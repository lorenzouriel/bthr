import json
import httpx
from opentelemetry.propagate import inject
from .telemetry import tracer


class ApiFailure(Exception):
    pass


class UnknownMutation(ApiFailure):
    pass


class Api:
    def __init__(self, settings):
        self.client = httpx.AsyncClient(base_url=settings.api_url.rstrip("/"), timeout=20, follow_redirects=False, trust_env=False)
        self.key = settings.bot_api_key.get_secret_value()

    async def request(self, sender, method, path, *, data=None, params=None, etag=None, content=None):
        headers = {"X-Bot-Api-Key": self.key, "X-Telegram-User": str(sender)}
        if etag:
            headers["If-Match"] = etag
        if content is not None:
            headers["Content-Type"] = "image/jpeg"
        # No URL, path IDs, response payload or exception message is recorded.
        with tracer.start_as_current_span("bthr.api", record_exception=False, set_status_on_exception=False) as span:
            span.set_attribute("http.request.method", method)
            inject(headers)
            try:
                response = await self.client.request(method, path, headers=headers, json=data if content is None else None,
                                                     params=params, content=content)
            except httpx.HTTPError:
                if method != "GET":
                    raise UnknownMutation("The API result is unknown. Check your records before retrying.") from None
                raise ApiFailure("API is unavailable") from None
            span.set_attribute("http.response.status_code", response.status_code)
            if response.status_code in (401, 403):
                raise ApiFailure("Account is unlinked or access was denied. Connect in BTHR settings.")
            if response.status_code in (409, 412, 428):
                raise ApiFailure("Record changed or conflicts with existing data. Read it again before editing.")
            if response.status_code >= 500 and method != "GET":
                raise UnknownMutation("The API result is unknown. Check your records before retrying.")
            if response.status_code >= 400:
                raise ApiFailure("API rejected the request. Check required fields, values and record ID.")
            return response.json() if response.content else None

    async def identity(self, sender):
        return await self.request(sender, "GET", "/api/telegram/status")

    async def records(self, sender, resource, **params):
        return await self.request(sender, "GET", "/api/agent/records/" + resource, params=params)

    async def close(self):
        await self.client.aclose()


def receipt(value):
    return json.dumps(value, ensure_ascii=False, default=str)[:3000]
