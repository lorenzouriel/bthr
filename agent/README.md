# BTHR Telegram agent

Python/LangGraph agent with LangChain adapters for Claude, GPT and local models. Domain records stay behind the existing API. Only `users.telegram_user_id` is added by migration V23; the meals schema is unchanged.

## Configuration

Create the bot through Telegram BotFather. Set these variables in `monitor/.env` alongside the existing monitor credentials:

```dotenv
TELEGRAM_BOT_TOKEN=<BotFather token>
TELEGRAM_BOT_USERNAME=<username without @>
TELEGRAM_WEBHOOK_SECRET=<at least 32 random characters>
BOT_API_KEY=<different random secret of at least 32 characters>
WEB_PUBLIC_URL=https://your-bthr-site.example
AI_LOCAL_ONLY=true
AGENT_TIMEZONE=America/Sao_Paulo
```

Edit `config/models.yml` to select installed/available model profiles for conversation, insights and vision. Defaults reference local Ollama models; install those models on your host yourself. A vision profile must support image input. To use Claude/GPT, set `AI_LOCAL_ONLY=false`, choose the cloud profiles, and set the referenced API-key environment variables. There is no automatic cloud fallback. Local profiles accept only private/local endpoints and redirects are disabled for compatible/Ollama clients.

The model receives the requested content and bounded recent conversation context. Photos are resized and metadata is stripped before analysis. No AI prompts, responses, photos or account identifiers are exported to telemetry.

## Docker commands

From `monitor/`:

```sh
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml config --quiet
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml up -d --build
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml ps
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml logs --tail=100 agent api
```

The overlay exposes the agent only at `127.0.0.1:8090`. Configure your HTTPS reverse proxy to forward **only** `/telegram/webhook` to the agent. Keep one agent process and one API replica; in-memory state is not shared. Then explicitly register the externally reachable HTTPS endpoint:

```sh
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml exec agent python -m bthr_agent.telegram set-webhook https://your-agent.example/telegram/webhook
```

For local development without public HTTPS, stop the webhook agent and run polling explicitly:

```sh
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml stop agent
docker compose -f docker-compose.yml -f compose.local.yml -f compose.agent.yml run --rm --no-deps agent python -m bthr_agent.telegram poll
```

Polling disables the registered webhook without dropping pending updates. Do not run polling and webhook consumers together. `down` preserves named photo/database/monitor volumes; do not use `down -v` unless intentionally deleting data.

## Using the bot

In BTHR Settings, create a Telegram QR/link, open it in Telegram, and approve the displayed numeric Telegram ID back in the signed-in web account. Alternatively share your own contact with the bot, paste its phone-link code into Settings, and approve. Phone matching alone grants no access. Codes expire after five minutes and disappear on API restart. `/unlink` revokes access immediately.

Ask to create/read/edit/delete any supported domain record, including finance. Reads are paginated; follow the returned offset for more. Three previously read/create-only resources (personal records, substance logs and symptom logs) now support owner-scoped full replacement PUT and soft-delete. Other update contracts remain unchanged. Destructive actions require a short-lived `/confirm <code>` reply; `/cancel` clears pending state. User ID, URLs and SQL are never model-controlled tools.

Ask a dated question for deterministic domain reports or numerical summaries; currencies/units are kept separate. The agent cannot answer every arbitrary analytical question: unavailable fields require a follow-up rather than fabricated data.

Send a meal photo or album. The model estimates foods/portions and the API saves directly into existing calorie/macro columns. The reply identifies approximate nutrition; no estimation column is stored. Ambiguous photos trigger questions. Saved images live in the private `meal-photos` volume under existing owner/meal IDs. API image reads check ownership and active status. If a meal saves but upload fails, `/retry_photo` retries only the same photo keys, never meal creation. Completed photos survive restarts; unsaved photo bytes and pending processing exist only in memory and may need resubmission.

Deleted/missing meal photos are reconciled hourly. Images are immediately inaccessible through the API after meal deletion. A single upload is limited to 10 MB, decoded images to 20 million pixels, and retained agent image bytes to 64 MB. No general permanent non-meal attachment library is provided.

## Monitoring and pricing

Grafana: `/d/bthr-agent-observability` on the existing monitor host. Metrics go to Prometheus, sanitized correlated logs to Loki, and workflow/model/API traces to Tempo. Provider/model filters apply to model panels; logs show the selected agent instances. Alerts are visible in Prometheus; notifications require separately configured alert delivery.

`config/model-pricing.yml` intentionally starts with no rates. Add reviewed rates keyed by **actual billed provider/model** from official provider pricing, including `version`, `source`, and non-overlapping USD-per-million-token `input`, `output`, `cache_read`, and `cache_write` rates as applicable. For vision, explicitly set `vision_token_billing: true` only when the selected model bills image usage in the returned token counts; otherwise costs remain unknown. Use `max_input_tokens` to bound a simple rate to its supported context tier. Complex non-token charges are not modeled and must remain unpriced.

Unknown usage/pricing is shown separately, never as zero. Local provider API charges are zero; hardware, electricity and hosting costs are excluded. No automatic retries or provider fallbacks are enabled; each actual attempt is accounted once, including invalid-output failures. Costs are monitoring estimates, not invoices. Export loss, process crashes, counter scraping and retention can undercount. Alert thresholds (including USD 10/hour) are examples to tune.

## Checks and limitations

From `agent/`, after dependency-install approval/access is available:

```sh
uv sync --python 3.12
uv run pytest
uv run ruff check src tests
uv run mypy src
```

Dependency resolution and a committed `uv.lock` remain pending while installation is blocked; the current Dockerfile uses pyproject ranges and is not yet a reproducible release build. Replace its installation step with locked `uv sync --frozen --no-dev` once resolution and tests pass. See `VALIDATION.md` for actual completed checks; source presence is not live acceptance.

No durable inbox/outbox/checkpoints or operation ledger exists. Restarts cancel sessions, pending confirmations and in-memory deduplication. An acknowledged update may be lost; redelivery after restart can duplicate a write. Ambiguous API writes are not automatically retried. Inspect records before retrying. Multi-instance operation, reliable background processing, permanent audit trails, scheduled coaching and a durable spend cap are outside this v1.
