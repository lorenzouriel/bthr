import asyncio
import hmac
import io
import time
from contextlib import asynccontextmanager

from aiogram import Bot
from aiogram.types import Update, ReplyKeyboardMarkup, KeyboardButton
from fastapi import FastAPI, HTTPException, Request

from .api import Api, ApiFailure
from .models import Models, ModelFailure
from .photos import MAX_BYTES, normalize
from .settings import Settings
from .telemetry import Accounting, configure, log, tracer
from .workflow import Workflow


class Runtime:
    def __init__(self, settings):
        self.settings = settings
        self.api = Api(settings)
        self.bot = Bot(settings.telegram_bot_token.get_secret_value())
        self.accounting = Accounting()
        self.workflow = Workflow(self.api, Models(settings, self.accounting), settings, self.accounting)
        self.queue: asyncio.Queue = asyncio.Queue(maxsize=100)
        self.accounting.queue_size = self.queue.qsize
        self.seen: dict[int, float] = {}
        self.pending: dict[int, int] = {}
        self.rate: dict[int, tuple[float, int]] = {}
        self.albums_seen: dict[tuple, float] = {}
        self.worker_task = None

    async def enqueue(self, update: Update):
        now = time.monotonic()
        self.seen = {key: expires for key, expires in self.seen.items() if expires > now}
        self.rate = {key: value for key, value in self.rate.items() if value[0] > now}
        if update.update_id in self.seen:
            return
        message = update.message
        if not message or message.chat.type != "private" or not message.from_user or message.from_user.is_bot:
            return
        sender = message.from_user.id
        window, count = self.rate.get(sender, (now + 60, 0))
        if count >= 20 or len(self.seen) >= 10000 or self.queue.full() or self.pending.get(sender, 0) >= 10:
            raise HTTPException(429, "Agent at capacity; retry later")
        self.queue.put_nowait(update)
        self.pending[sender] = self.pending.get(sender, 0) + 1
        self.seen[update.update_id] = now + 3600
        self.rate[sender] = (window, count + 1)

    async def send(self, chat_id, text, markup=None):
        # Plain text: never evaluate model-generated HTML/Markdown or Telegram links.
        await self.bot.send_message(chat_id, text[:4000], reply_markup=markup)

    async def image(self, message):
        item = message.photo[-1] if message.photo else message.document
        if not item or (item.file_size or 0) > MAX_BYTES:
            raise ValueError("Use an image smaller than 10 MB")
        class Bounded(io.BytesIO):
            def write(self, data):
                if self.tell() + len(data) > MAX_BYTES:
                    raise ValueError("Use an image smaller than 10 MB")
                return super().write(data)
        destination = Bounded()
        await self.bot.download(item, destination=destination)
        return normalize(destination.getvalue())

    async def handle(self, update):
        message = update.message
        sender, chat = message.from_user.id, message.chat.id
        text = message.text or message.caption or ""
        if text.startswith("/start "):
            await self.api.request(sender, "POST", "/api/telegram/claim", data={"token": text.split(maxsplit=1)[1], "sender": sender})
            await self.send(chat, f"Return to BTHR settings and approve Telegram user ID {sender}. No access is granted until you approve.")
            return
        if text in ("/start", "/link", "/phone"):
            await self.send(chat, f"Connect in {self.settings.web_url}/account using the QR/link, or share your own phone contact below. "
                            "You must approve the connection in your signed-in BTHR account. Photos and requested data are processed by the configured AI provider.",
                            ReplyKeyboardMarkup(keyboard=[[KeyboardButton(text="Share my phone", request_contact=True)]], resize_keyboard=True, one_time_keyboard=True))
            return
        if message.contact:
            if message.contact.user_id != sender or message.forward_origin:
                await self.send(chat, "Use the Share my phone button to share your own contact.")
                return
            result = await self.api.request(sender, "POST", "/api/telegram/phone", data={"sender": sender, "phone": message.contact.phone_number})
            await self.send(chat, f"Open {self.settings.web_url}/account and enter this phone-link code: {result['token']}\n"
                            f"Then approve Telegram user ID {sender}. The code expires in five minutes.")
            return
        if text == "/unlink":
            await self.api.request(sender, "POST", "/api/telegram/unlink")
            self.workflow.clear(sender)
            await self.send(chat, "Disconnected. Pending conversation state has been cleared.")
            return
        if text == "/help":
            await self.send(chat, "Ask to add, read, edit or delete your records, or request a dated review. Send a meal photo to log nutrition. "
                                  "Use /cancel, /retry_photo or /unlink. Pending work is lost if the service restarts.")
            return
        await self.api.identity(sender)  # Check before downloading or sending images to a provider.
        messages = [message]
        if message.media_group_id:
            key = (sender, message.media_group_id)
            now = time.monotonic()
            self.albums_seen = {k: v for k, v in self.albums_seen.items() if v > now}
            if key in self.albums_seen:
                await self.send(chat, "A late album image was not processed. Send the full meal again only if it was not already saved.")
                return
            # Let subsequent webhook updates reach the queue, then drain matching album members.
            await asyncio.sleep(2)
            other = []
            while not self.queue.empty():
                candidate = self.queue.get_nowait()
                self.queue.task_done()
                if candidate.message.from_user.id == sender and candidate.message.media_group_id == message.media_group_id:
                    messages.append(candidate.message)
                    self.pending[sender] -= 1
                else:
                    other.append(candidate)
            for candidate in other:
                self.queue.put_nowait(candidate)
            self.albums_seen[key] = now + 3600
        images = [await self.image(m) for m in messages[:10] if m.photo or m.document]
        if images:
            # Bound retained decoded upload data across all in-memory sessions.
            retained = sum(sum(len(image) for image in s.images) for s in self.workflow.sessions.values())
            if retained + sum(map(len, images)) > 64 * 1024 * 1024:
                raise ApiFailure("Photo processing is at capacity; try later")
        answer = await self.workflow.run(sender, text, images)
        await self.send(chat, answer)

    async def worker(self):
        while True:
            update = await self.queue.get()
            sender = update.message.from_user.id
            try:
                with tracer.start_as_current_span("telegram.update", record_exception=False, set_status_on_exception=False):
                    await asyncio.wait_for(self.handle(update), timeout=240)
            except (ApiFailure, ModelFailure) as exc:
                try:
                    await self.send(update.message.chat.id, str(exc))
                except Exception:
                    log.warning("Telegram reply delivery failed")
            except Exception:
                # Do not retry possible writes or leak provider/Telegram URLs in exceptions.
                log.warning("Agent operation failed")
                try:
                    await self.send(update.message.chat.id, "Could not finish this request. Check your records before retrying a write. For images, use JPEG/PNG/WebP under 10 MB.")
                except Exception:
                    log.warning("Telegram reply delivery failed")
            finally:
                self.pending[sender] -= 1
                if self.pending[sender] == 0:
                    self.pending.pop(sender)
                self.queue.task_done()


@asynccontextmanager
async def lifespan(app):
    settings = Settings()
    providers = configure(settings)
    runtime = Runtime(settings)
    app.state.runtime = runtime
    runtime.worker_task = asyncio.create_task(runtime.worker())
    try:
        yield
    finally:
        runtime.worker_task.cancel()
        await asyncio.gather(runtime.worker_task, return_exceptions=True)
        await runtime.api.close()
        await runtime.bot.session.close()
        for provider in providers:
            provider.shutdown()


app = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None, openapi_url=None)


@app.get("/health/live")
async def live():
    return {"status": "ok"}


@app.get("/health/ready")
async def ready(request: Request):
    runtime = request.app.state.runtime
    if runtime.worker_task.done():
        raise HTTPException(503, "Worker unavailable")
    try:
        response = await runtime.api.client.get("/health/ready")
        response.raise_for_status()
    except Exception:
        raise HTTPException(503, "API unavailable") from None
    return {"status": "ready", "queued": runtime.queue.qsize()}


@app.post("/telegram/webhook")
async def webhook(request: Request):
    runtime = request.app.state.runtime
    supplied = request.headers.get("X-Telegram-Bot-Api-Secret-Token", "")
    if not hmac.compare_digest(supplied, runtime.settings.telegram_webhook_secret.get_secret_value()):
        raise HTTPException(403, "Invalid webhook secret")
    body = bytearray()
    async for part in request.stream():
        body.extend(part)
        if len(body) > 256 * 1024:
            raise HTTPException(413, "Update too large")
    try:
        update = Update.model_validate_json(bytes(body))
    except Exception:
        raise HTTPException(400, "Invalid update") from None
    await runtime.enqueue(update)
    return {"ok": True}
