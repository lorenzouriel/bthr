"""Explicit operator commands; never invoked automatically by service startup."""
import argparse
import asyncio
from urllib.parse import urlparse
from aiogram import Bot
from .settings import Settings


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("command", choices=["set-webhook", "poll"])
    parser.add_argument("url", nargs="?")
    args = parser.parse_args()
    settings = Settings()
    if args.command == "set-webhook":
        if not args.url or urlparse(args.url).scheme != "https":
            parser.error("Provide the public HTTPS /telegram/webhook URL")
        async with Bot(settings.telegram_bot_token.get_secret_value()) as bot:
            await bot.set_webhook(args.url, secret_token=settings.telegram_webhook_secret.get_secret_value(),
                                  allowed_updates=["message"], drop_pending_updates=False)
        print("Webhook configured.")
    else:
        from .app import app, lifespan
        async with lifespan(app):
            runtime = app.state.runtime
            # Explicit poll mode disables the webhook, without dropping pending messages.
            await runtime.bot.delete_webhook(drop_pending_updates=False)
            offset = None
            while True:
                updates = await runtime.bot.get_updates(offset=offset, timeout=30, allowed_updates=["message"])
                for update in updates:
                    while runtime.queue.full():
                        await asyncio.sleep(1)
                    try:
                        await runtime.enqueue(update)
                    except Exception:
                        await asyncio.sleep(3)
                        break
                    offset = update.update_id + 1


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
    except Exception:
        print("Telegram command failed. Check connectivity and configuration; secrets are not printed.")
        raise SystemExit(1) from None
