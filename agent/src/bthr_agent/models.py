"""Provider adapters: no implicit routing, fallback, retries or content tracing."""
import asyncio
import ipaddress
import json
import os
import time
from typing import Literal
from urllib.parse import urlparse

import yaml
from langchain_core.messages import HumanMessage, SystemMessage
from pydantic import BaseModel, ConfigDict, Field
from .telemetry import Accounting, tracer


class ModelFailure(Exception):
    """Sanitized exception; never exposes a provider request or response."""


class Profile(BaseModel):
    model_config = ConfigDict(extra="forbid", hide_input_in_errors=True)
    provider: Literal["openai", "anthropic", "ollama", "compatible"]
    model: str = Field(min_length=1, max_length=150)
    deployment: Literal["local", "cloud"]
    base_url: str | None = None
    key_env: str | None = None
    vision: bool = False
    timeout: int = Field(default=60, ge=1, le=180)
    max_tokens: int = Field(default=2048, ge=64, le=8192)


def local_endpoint(url: str | None) -> bool:
    parsed = urlparse(url or "")
    if parsed.scheme not in ("http", "https") or parsed.username or parsed.password:
        return False
    host = parsed.hostname or ""
    if host in ("localhost", "host.docker.internal", "ollama"):
        return True
    try:
        address = ipaddress.ip_address(host)
        return address.is_loopback or address in ipaddress.ip_network("10.0.0.0/8") or address in ipaddress.ip_network("172.16.0.0/12") or address in ipaddress.ip_network("192.168.0.0/16")
    except ValueError:
        return False


class Models:
    def __init__(self, settings, accounting: Accounting):
        # Explicitly prevent callbacks that export conversation contents.
        os.environ["LANGSMITH_TRACING"] = "false"
        os.environ["LANGCHAIN_TRACING_V2"] = "false"
        config = yaml.safe_load(settings.model_config_path.read_text())
        self.profiles = {key: Profile.model_validate(value) for key, value in config["profiles"].items()}
        self.tasks = config["tasks"]
        if set(self.tasks) != {"conversation", "insights", "vision"}:
            raise ValueError("Configure conversation, insights and vision profiles")
        for key in self.tasks.values():
            profile = self.profiles[key]
            if profile.deployment == "local" and (profile.provider not in ("ollama", "compatible") or not local_endpoint(profile.base_url)):
                raise ValueError("Local models require an explicit private/local endpoint")
            if settings.local_only and profile.deployment != "local":
                raise ValueError("Cloud profile selected in LOCAL_ONLY mode")
        self.rates = yaml.safe_load(settings.pricing_config_path.read_text()).get("rates", {})
        self.accounting = accounting
        self.clients = {}

    def client(self, profile: Profile):
        key = profile.model_dump_json()
        if key in self.clients:
            return self.clients[key]
        if profile.provider == "ollama":
            from langchain_ollama import ChatOllama
            client = ChatOllama(model=profile.model, base_url=profile.base_url, num_predict=profile.max_tokens,
                                client_kwargs={"timeout": profile.timeout, "follow_redirects": False, "trust_env": False})
        else:
            secret = os.environ.get(profile.key_env or "", "")
            if not secret and profile.deployment != "local":
                raise ModelFailure("Model credentials are not configured")
            if profile.provider == "anthropic":
                from langchain_anthropic import ChatAnthropic
                client = ChatAnthropic(model=profile.model, api_key=secret, max_tokens=profile.max_tokens,
                                       timeout=profile.timeout, max_retries=0)
                # Anthropic adapter manages its own HTTP client; no caller-supplied endpoint.
            else:
                from langchain_openai import ChatOpenAI
                import httpx
                transport = httpx.AsyncClient(timeout=profile.timeout, follow_redirects=False, trust_env=False)
                client = ChatOpenAI(model=profile.model, api_key=secret or "local", base_url=profile.base_url,
                                    max_tokens=profile.max_tokens, timeout=profile.timeout, max_retries=0,
                                    http_async_client=transport)
        self.clients[key] = client
        return client

    async def json(self, task: str, instruction: str, content, schema: type[BaseModel]):
        profile = self.profiles[self.tasks[task]]
        if task == "vision" and not profile.vision:
            raise ModelFailure("Selected model does not support images")
        system = instruction + "\nReturn ONLY a JSON object matching this schema. No markdown.\n" + json.dumps(schema.model_json_schema())
        usage = None
        outcome = "error"
        start = time.monotonic()
        with tracer.start_as_current_span("ai." + task, record_exception=False, set_status_on_exception=False) as span:
            span.set_attribute("gen_ai.provider.name", profile.provider)
            span.set_attribute("gen_ai.request.model", profile.model)
            span.set_attribute("task", task)
            try:
                response = await asyncio.wait_for(self.client(profile).ainvoke(
                    [SystemMessage(content=system), HumanMessage(content=content)], config={"callbacks": []}), profile.timeout)
                usage = response.usage_metadata
                metadata = response.response_metadata or {}
                resolved = metadata.get("model_name") or metadata.get("model") or profile.model
                span.set_attribute("gen_ai.response.model", str(resolved)[:150])
                text = response.content
                if isinstance(text, list):
                    text = "".join(block.get("text", "") for block in text if isinstance(block, dict) and block.get("type") == "text")
                result = schema.model_validate_json(text)
                outcome = "success"
                return result
            except Exception:
                span.set_attribute("error.type", "model_or_validation_error")
                raise ModelFailure("Model unavailable or returned invalid output; no changes were made") from None
            finally:
                # One accounting event per network attempt, even if parsing fails.
                rate = self.rates.get(profile.provider + "/" + str(locals().get("resolved", profile.model)))
                if rate and (not rate.get("version") or not rate.get("source") or
                             (task == "vision" and not rate.get("vision_token_billing", False)) or
                             (rate.get("max_input_tokens") is not None and (usage or {}).get("input_tokens", 0) > rate["max_input_tokens"])):
                    rate = None
                if rate:
                    span.set_attribute("bthr.ai.pricing.version", str(rate["version"]))
                self.accounting.record(profile, task, usage, rate, time.monotonic() - start, outcome)
