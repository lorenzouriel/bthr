# DESIGN: AI Agent (Web + Telegram)

> Review update (2026-10-07): [STORIES_AI_AGENT.md](./STORIES_AI_AGENT.md) supersedes conflicting implementation details in this document. Its audit identifies unresolved API/database contract drift, V23/V24 migration numbering, three append-only resources, and required execution/authorization safeguards. This document is historical design context, not an independently ready-to-build specification.

> Technical design for a provider-agnostic Python agent service that operates every bthr resource on behalf of the user, from the web app and Telegram.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | AI_AGENT |
| **Date** | 2026-10-06 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_AI_AGENT.md](./DEFINE_AI_AGENT.md) |
| **Status** | Ready for Build |
| **Confidence** | 0.90 — KB patterns (`genai/tool-calling`, `genai/chatbot-architecture`, `genai/guardrails`, `genai/evaluation-framework`) + specialist agents matched; Pydantic AI 2.0 deferred-tool approval API validated via Context7 (`/pydantic/pydantic-ai/v2.0.0`) |

---

## Architecture Overview

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│                               bthr + AI Agent                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  Browser (web/)                              Telegram servers                 │
│   Assistant.tsx ── fetch POST (SSE) ──┐        │ HTTPS webhook                │
│   Settings: Connect Telegram          │        │ X-Telegram-Bot-Api-Secret-Token
│        │ cookie access_token          │        ▼                              │
│        │                       ┌──────┴───────── Traefik (same host) ───────┐ │
│        │                       │ /api/agent/*  → agent:8000                 │ │
│        └──────────────────────►│ /telegram/*   → agent:8000                 │ │
│                                │ /api/*        → finapi:8080              │ │
│                                └──────┬──────────────────────────┬──────────┘ │
│                                       ▼                          │            │
│  ┌──────────────────────── agent/ (Python 3.12, FastAPI) ─────┐  │            │
│  │ channels/web.py          channels/telegram.py (aiogram 3)  │  │            │
│  │   (JWT from cookie)        (chat_id → chat token via API)  │  │            │
│  │            └───────────┬──────────────┘                    │  │            │
│  │                 runner.py  AgentRunner                     │  │            │
│  │   per-user lock · rate limit · history load/save ·         │  │            │
│  │   pending-action (deferred approval) lifecycle             │  │            │
│  │                        │                                   │  │            │
│  │          core/agent.py  Pydantic AI Agent (model from env) │  │            │
│  │   tools: describe_resource · list_records · create_record  │  │            │
│  │          update_record* · delete_record* · get_review      │  │            │
│  │          (* requires_approval → DeferredToolRequests)      │  │            │
│  │                        │                                   │  │            │
│  │   registry/ (resources.yaml + openapi snapshot)            │  │            │
│  │   api_client.py (httpx, Bearer <user JWT>, OTel traceparent)│ │            │
│  │   store/ (asyncpg → schema `agent`)                        │  │            │
│  └────────────┬────────────────────────────┬─────────────────┘  │            │
│               │ REST as the user           │ SQL (agent.* only)  │            │
│               ▼                            ▼                     ▼            │
│  ┌──────── bthr.Api (.NET 10) ────────┐   ┌──────── PostgreSQL ────────┐  │
│  │ 19 resource controllers, reports       │──►│ finance / body / mind      │  │
│  │ TelegramIntegrationController (new)    │   │ integration.* (new, API)   │  │
│  │ AuthController: bot/telegram/link,     │   │ agent.* (new, agent svc)   │  │
│  │   bot/token-for-chat (new)             │   └────────────────────────────┘  │
│  └────────────────────────────────────────┘                                   │
│            OTel (traces/logs/metrics) ───► monitor/ collector → Tempo/Loki    │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `agent/` service | Hosts the agent, both channels, conversation store | Python 3.12, FastAPI, uvicorn, Pydantic AI 2.x (`pydantic-ai-slim[anthropic,openai]`), aiogram 3, httpx, asyncpg, PyJWT, pydantic-settings |
| Resource registry | Maps the 19 resource names → API routes, id param, supported list filters, create/update JSON schemas | `resources.yaml` + committed OpenAPI snapshot `agent/openapi/bthr.openapi.json` |
| Agent core | System instructions, persona, dynamic date/timezone context, generic tools | Pydantic AI `Agent(deps_type=AgentDeps, output_type=[str, DeferredToolRequests])` |
| AgentRunner | Channel-agnostic turn executor: lock, rate limit, history, deferred approvals, event stream | asyncio, Pydantic AI `run_stream_events` |
| Web channel | `POST /api/agent/messages` (SSE), `POST /api/agent/actions/{id}`, `GET /api/agent/messages` | FastAPI `StreamingResponse` |
| Telegram channel | Webhook (prod) / polling (dev), `/start <code>` linking, inline-keyboard confirmations | aiogram 3 |
| Conversation store | `agent.conversations`, `agent.messages`, `agent.pending_actions` | PostgreSQL via asyncpg, Flyway-managed DDL |
| Telegram integration (API) | Link codes, chat links, chat-scoped short-lived tokens | ASP.NET Core controller + service + EF Core, `integration` schema |
| Web Assistant panel | Chat UI with streamed replies and confirm cards; Settings → Connect Telegram | React 19 + TS, `fetch` ReadableStream SSE parser |
| Eval suite | Release gate on seeded data | `pydantic-evals`, seed-demo dataset |

---

## Key Decisions

### Decision 1: Confirmation via Pydantic AI deferred tools + persisted pending actions

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Context:** Updates/deletes must never execute without explicit confirmation (DEFINE: 100% of cases, 0 premature mutations), confirmation can arrive minutes later from a different HTTP request or a Telegram callback, must expire after 15 min, and must be bound to its owner.

**Choice:** `update_record` and `delete_record` are registered with `requires_approval=True`. A run that calls them ends with `DeferredToolRequests`; the runner persists the run's messages (including the unanswered `ToolCallPart`s) and inserts one `agent.pending_actions` row per approval (shared `batch_id`) with owner, preview and `expires_at = now() + 15 min`. On Confirm/Cancel the runner validates owner + status + expiry **server-side**, then resumes with `agent.run(message_history=..., deferred_tool_results=DeferredToolResults(approvals={tool_call_id: True | ToolDenied(...)}))`. If the user sends a new message while a batch is pending, the batch is auto-denied (`ToolDenied("User moved on without confirming")`) in the same run as the new prompt, keeping the history valid.

**Rationale:** The mutation is physically unreachable until a deferred result with `True` is supplied — the guarantee comes from the framework's control flow, not from prompt instructions. Persisting the batch makes confirmation stateless across processes/channels and lets the API-facing checks (owner, TTL) live in one SQL predicate.

**Alternatives Rejected:**
1. Prompt-only ("ask before deleting") — rejected: not enforceable; fails the 100% safety gate.
2. Two-step tool (`prepare_delete` → `commit_delete(token)`) — rejected: the LLM could call commit itself; reinvents what deferred tools already provide.
3. LangGraph interrupts — rejected: framework already chosen (Pydantic AI) in Brainstorm.

**Consequences:**
- Pending state is coupled to Pydantic AI's message format (pinned major version `>=2,<3`).
- Uniform UX on both channels: a "confirm card" event the channel renders as buttons.

---

### Decision 2: Agent acts as the user with the user's JWT; Telegram gets chat-scoped short-lived tokens

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Context:** The existing `POST /api/auth/bot/token {userId}` lets any holder of `BOT_API_KEY` impersonate any user. DEFINE requires 0 cross-user access and removal of that endpoint.

**Choice:**
- **Web:** the agent reads the `access_token` cookie (same-site via Traefik path routing), validates it locally with PyJWT (HS256, same `Jwt:SecretKey`/`Issuer`/`Audience`, `exp`), extracts `sub`, and forwards it to the API as `Authorization: Bearer`. (The API's `OnMessageReceived` sets `context.Token` from the cookie; when the cookie is absent the token is `null` and `JwtBearerHandler` falls back to the `Authorization` header.)
- **Telegram:** new API endpoints, both gated by `X-Bot-Api-Key`:
  - `POST /api/auth/bot/telegram/link {code, chatId, telegramUsername?}` — redeems a single-use, 10-minute code (stored as SHA-256 hash) and upserts `integration.telegram_links`.
  - `POST /api/auth/bot/token-for-chat {chatId}` — returns `{token, userId, expiresAt}` **only** if the chat is linked; token lifetime 15 min (`JwtService.GenerateToken(..., lifetime)`).
  - `POST /api/auth/bot/token` is **deleted**.
- **User-facing (cookie auth):** `POST /api/integrations/telegram/link-code`, `GET /api/integrations/telegram/link`, `DELETE /api/integrations/telegram/link`.

**Rationale:** A leaked bot key now only grants access to users who linked a chat, and only via that chat id; link codes are single-use, hashed, and short-lived. The API's existing `userId == claim` checks remain the sole authorization boundary.

**Alternatives Rejected:**
1. Keep `bot/token {userId}` — rejected: key leak = full account takeover for every user.
2. Agent stores long-lived user tokens — rejected: token theft from agent DB; no rotation.
3. Agent calls `/api/auth/me` per web message instead of local JWT validation — rejected: +1 round trip per turn against the < 3 s first-token SLO; kept as fallback if the secret must not be shared.

**Consequences:**
- The agent holds `JWT_SECRET_KEY` (verification only). Documented in Security Considerations.
- Link tables are owned by the API (`integration` schema, EF Core); the agent never touches them directly.

---

### Decision 3: Generic tools driven by a registry + committed OpenAPI snapshot

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Context:** 19 resources × 4 operations ≈ 76 endpoints. Swagger is only served when `ASPNETCORE_ENVIRONMENT=Development` (`Program.cs`), so the agent cannot fetch it in production (A-001). Most resources have **no** `GET /{id}` endpoint (only list).

**Choice:**
- `agent/openapi/bthr.openapi.json` is a committed snapshot exported with Swashbuckle CLI (`agent/scripts/export_openapi.sh`); CI regenerates it and fails on diff.
- `agent/src/bthr_agent/registry/resources.yaml` lists each resource: `name`, `domain`, `path`, `id_param`, `create_schema`, `update_schema`, `list_filters`, `date_field`, `description`.
- At startup the registry resolves `$ref`s from the snapshot into JSON Schemas and builds a `Literal[...]` enum of resource names.
- Tools (7): `describe_resource(resource)` (returns field schema — keeps per-resource schemas out of the base prompt), `list_records(resource, start_date?, end_date?, filters?, limit?)`, `get_record(resource, id)` (list + filter by id, since no GET-by-id exists), `create_record(resource, data)`, `update_record(resource, id, changes)` *, `delete_record(resource, id)` *, `get_review(start_date, end_date, domain, combine?)`.
- `data`/`changes` are validated against the resource's JSON Schema with `jsonschema`; failures raise `ModelRetry` with the error list so the model self-corrects (AT-015).

**Rationale:** Seven tools keep tool selection accurate; schemas loaded on demand keep the context small; adding a resource = one YAML entry + snapshot refresh (DEFINE SHOULD goal).

**Alternatives Rejected:**
1. Enable Swagger in production and fetch at runtime — rejected: exposes API surface publicly; startup dependency on API.
2. Hand-written Pydantic models per resource — rejected: drift with DTOs; 38 models to maintain.
3. One tool per endpoint — rejected in Brainstorm (context bloat).

**Consequences:**
- Snapshot drift is caught in CI, not at runtime.
- Model needs one extra `describe_resource` call before the first create of a resource type in a conversation (mitigated: schemas of the 6 most-used resources — expenses, earnings, meals, water-intake, workouts, sleep-logs — are inlined in instructions).

---

### Decision 4: One conversation per user, stored as per-run Pydantic AI message JSON, serialized by a per-user lock

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Context:** Shared thread across channels (AT-010); messages from both channels can arrive concurrently; tool call/return pairs must never be split when truncating history.

**Choice:** `agent.messages` stores one row per run: `messages_json` (`result.new_messages_json()`), plus `channel`, `user_text`, `assistant_text` for display. History = last `AGENT_HISTORY_RUNS` (default 20) runs concatenated in order — run boundaries guarantee no orphaned tool returns. An in-process `asyncio.Lock` per user serializes turns (single instance, A-007). Rows cascade on `users` delete.

**Rationale:** Run-granular rows make truncation safe and cheap, and display text avoids re-parsing message JSON for the web history view.

**Alternatives Rejected:**
1. One row per `ModelMessage` — rejected: truncation can split tool pairs.
2. Summarization memory — deferred (YAGNI); revisit if 20 runs exceed context budget.

**Consequences:**
- Multi-instance deployment would need a Postgres advisory lock (`pg_advisory_xact_lock(user_id)`) — noted for later.

---

### Decision 5: Transport — SSE over POST for web, webhook (prod) / polling (dev) for Telegram, Traefik path routing for same-site cookies

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Context:** A-002 (cookie must reach the agent), A-003 (public HTTPS for webhook), < 3 s first token.

**Choice:** Production Traefik routes `/api/agent/*` and `/telegram/*` on the API's host to the agent container (same origin → `SameSite` cookie sent). Web uses `fetch` + `ReadableStream` to parse `text/event-stream` (EventSource cannot POST). Events: `delta`, `tool`, `confirm`, `done`, `error`. Dev: Vite `server.proxy` forwards `/api/agent` to `localhost:8000`. Telegram: `TELEGRAM_MODE=webhook|polling`; webhook validates `X-Telegram-Bot-Api-Secret-Token`.

**Rationale:** No CORS-with-credentials surface in prod; polling makes local dev work behind NAT.

**Alternatives Rejected:**
1. WebSockets — rejected: bidirectional not needed; harder through proxies.
2. Credentialed CORS to a separate agent host — rejected: larger attack surface, cookie `SameSite=None` already required by API config.

**Consequences:**
- Traefik config lives outside this repo → documented deployment step in `agent/README.md`.

---

### Decision 6: Provider-agnostic model selection by env + per-user rate limit before any LLM call

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-10-06 |

**Choice:** `AGENT_MODEL` holds a Pydantic AI model string (default `anthropic:claude-sonnet-5-5`; e.g. `openai:gpt-5.2`, `ollama:qwen3`), passed to `Agent(...)`; provider keys via standard env vars (`ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, `OLLAMA_BASE_URL`). In-memory sliding-window limiter (`AGENT_RATE_LIMIT_PER_HOUR=60`) checked before history load; `UsageLimits(request_limit=8, tool_calls_limit=12)` caps runaway loops.

**Rationale:** Meets "switch provider by env only" and cost control with zero extra infra.

**Alternatives Rejected:** LiteLLM proxy — extra hop/service; Redis limiter — unnecessary for single instance.

---

## File Manifest

### Database (Flyway)

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `database/migrations/V31__create_integration_schema.sql` | Create | `integration.telegram_link_codes`, `integration.telegram_links` (+ comments, indexes) | @schema-designer | None |
| 2 | `database/migrations/V32__create_agent_schema.sql` | Create | `agent.conversations`, `agent.messages`, `agent.pending_actions` (+ comments, indexes, FKs cascade to `users`) | @schema-designer | None |

### API (.NET)

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 3 | `api/bthr.Api/Models/TelegramLink.cs` | Create | EF entity → `integration.telegram_links` | @dotnet-developer | 1 |
| 4 | `api/bthr.Api/Models/TelegramLinkCode.cs` | Create | EF entity → `integration.telegram_link_codes` | @dotnet-developer | 1 |
| 5 | `api/bthr.Api/Data/ApplicationDbContext.cs` | Modify | DbSets + mappings for 3, 4 | @dotnet-developer | 3, 4 |
| 6 | `api/bthr.Api/DTOs/TelegramDTOs.cs` | Create | `LinkCodeResponse`, `TelegramLinkStatusResponse`, `BotTelegramLinkRequest`, `BotChatTokenRequest`, `BotChatTokenResponse` | @dotnet-developer | None |
| 7 | `api/bthr.Api/DTOs/AuthDTOs.cs` | Modify | Remove `BotTokenRequest` | @dotnet-developer | None |
| 8 | `api/bthr.Api/Services/JwtService.cs` | Modify | `GenerateToken(int userId, int plan = 0, bool isAdmin = false, TimeSpan? lifetime = null)` | @dotnet-developer | None |
| 9 | `api/bthr.Api/Services/TelegramLinkService.cs` | Create | Create code (hash, TTL 10 min, invalidates previous), redeem (single-use), status, unlink, resolve chat → user | @dotnet-developer | 5, 6 |
| 10 | `api/bthr.Api/Controllers/TelegramIntegrationController.cs` | Create | `api/integrations/telegram/link-code` (POST), `api/integrations/telegram/link` (GET/DELETE) | @dotnet-developer | 9 |
| 11 | `api/bthr.Api/Controllers/AuthController.cs` | Modify | Delete `bot/token`; add `bot/telegram/link`, `bot/token-for-chat` (constant-time key compare) | @dotnet-developer | 8, 9 |
| 12 | `api/bthr.Api/Program.cs` | Modify | Register `ITelegramLinkService`; `Telegram:BotUsername`, `Bot:ChatTokenMinutes` config | @dotnet-developer | 9 |
| 13 | `api/docker-compose.yml` / `api/.env.example` | Modify | `Telegram__BotUsername`, `Bot__ChatTokenMinutes` | (general) | 12 |
| 14 | `.config/dotnet-tools.json` | Create | Pin `swashbuckle.aspnetcore.cli` for OpenAPI export | @dotnet-specialist | None |
| 15 | `api/bthr.Tests/Helpers/Builders/TelegramLinkBuilder.cs` | Create | Test data builder (matches existing builder pattern) | @dotnet-developer | 3, 4 |
| 16 | `api/bthr.Tests/UnitTests/Services/TelegramLinkServiceTests.cs` | Create | Code TTL, single-use, hash, relink, unlink | @dotnet-developer | 9, 15 |
| 17 | `api/bthr.Tests/UnitTests/Controllers/TelegramIntegrationControllerTests.cs` | Create | Auth required, status/unlink | @dotnet-developer | 10 |
| 18 | `api/bthr.Tests/UnitTests/Controllers/AuthControllerTests.cs` | Modify | Remove bot/token tests; add link + token-for-chat (bad key, unlinked chat, TTL ≤ 15 min) | @dotnet-developer | 11 |
| 19 | `api/bthr.Tests/UnitTests/Services/JwtServiceTests.cs` | Modify | Custom lifetime test | @dotnet-developer | 8 |
| 20 | `api/bthr.Tests/IntegrationTests/TelegramBotEndpointTests.cs` | Create | AT-007/008/009/014 end-to-end against `CustomWebApplicationFactory` | @dotnet-developer | 11 |

### Agent service (Python)

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 21 | `agent/pyproject.toml` | Create | Deps (pinned majors), ruff, pytest config, `uv` lock | @python-developer | None |
| 22 | `agent/.env.example` | Create | All config keys (see Configuration) | (general) | None |
| 23 | `agent/src/bthr_agent/config.py` | Create | `Settings(BaseSettings)` | @python-developer | 21 |
| 24 | `agent/src/bthr_agent/telemetry.py` | Create | OTLP tracer/logger, `Agent.instrument_all()`, httpx + FastAPI instrumentation | @python-developer | 23 |
| 25 | `agent/openapi/bthr.openapi.json` | Create | Committed API OpenAPI snapshot | @dotnet-specialist | 14 |
| 26 | `agent/scripts/export_openapi.sh` | Create | Build API, `dotnet swagger tofile`, write 25 | @shell-script-specialist | 14 |
| 27 | `agent/src/bthr_agent/registry/resources.yaml` | Create | 19 resource entries | @python-developer | 25 |
| 28 | `agent/src/bthr_agent/registry/registry.py` | Create | Load YAML + resolve schemas, `ResourceName` Literal, validation | @python-developer | 25, 27 |
| 29 | `agent/src/bthr_agent/api_client.py` | Create | `bthrClient` (httpx.AsyncClient, Bearer, error mapping) + `BotClient` (link, chat token, cached until 60 s before expiry) | @python-developer | 23 |
| 30 | `agent/src/bthr_agent/auth.py` | Create | Web cookie JWT validation → `UserContext(user_id, token)` | @python-developer | 23 |
| 31 | `agent/src/bthr_agent/store/db.py` | Create | asyncpg pool lifecycle | @python-developer | 23 |
| 32 | `agent/src/bthr_agent/store/conversations.py` | Create | get-or-create conversation, load last N runs, append run | @python-developer | 31 |
| 33 | `agent/src/bthr_agent/store/pending.py` | Create | create batch, claim batch (owner+status+expiry in one UPDATE … RETURNING), deny open batches | @python-developer | 31 |
| 34 | `agent/src/bthr_agent/core/prompts.py` | Create | Persona + rules (PT/EN, list-before-mutate, ambiguity handling, no fabricated numbers) | @llm-specialist | 28 |
| 35 | `agent/src/bthr_agent/core/agent.py` | Create | `AgentDeps`, `build_agent(settings, registry)`, dynamic instructions (today, tz) | @genai-architect | 28, 29, 34 |
| 36 | `agent/src/bthr_agent/core/tools.py` | Create | 7 generic tools, approval flags, `ModelRetry` on validation/API 4xx | @genai-architect | 28, 29, 35 |
| 37 | `agent/src/bthr_agent/ratelimit.py` | Create | Sliding-window limiter | @python-developer | 23 |
| 38 | `agent/src/bthr_agent/runner.py` | Create | `AgentRunner.handle_message` / `resolve_action` → async iterator of `TurnEvent` | @genai-architect | 32, 33, 35, 36, 37 |
| 39 | `agent/src/bthr_agent/channels/web.py` | Create | FastAPI router: SSE messages, actions, history | @python-developer | 30, 38 |
| 40 | `agent/src/bthr_agent/channels/telegram.py` | Create | aiogram router: `/start <code>`, text, callbacks `pa:<batch>:<y|n>`, unlinked reply | @python-developer | 29, 38 |
| 41 | `agent/src/bthr_agent/main.py` | Create | App factory, lifespan (pool, registry, bot webhook/polling), `/health` (DB + API reachability) | @python-developer | 24, 31, 39, 40 |
| 42 | `agent/Dockerfile` | Create | Multi-stage uv build, non-root user | @aws-container-ops | 21 |
| 43 | `agent/docker-compose.yml` | Create | `bthr-agent` service on `fin-network` | @aws-container-ops | 42 |
| 44 | `agent/azure-pipelines.yml` | Create | Lint, tests, OpenAPI drift check, build/deploy on self-hosted agent (mirrors `api/`) | @ci-cd-specialist | 26, 42 |
| 45 | `agent/README.md` | Create | Run, config, Traefik routes, Telegram setup, DB role grants | @code-documenter | 41, 43 |

### Agent tests & evals

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 46 | `agent/tests/conftest.py` | Create | Settings override, `TestModel`/`FunctionModel` fixtures, respx API mock, test Postgres (testcontainers) | @test-generator | 38 |
| 47 | `agent/tests/test_registry.py` | Create | All 19 resources resolve schemas; enum matches YAML; validation errors | @test-generator | 28 |
| 48 | `agent/tests/test_tools.py` | Create | Each tool → correct HTTP call; 4xx → `ModelRetry`; list truncation | @test-generator | 36 |
| 49 | `agent/tests/test_runner_confirm.py` | Create | AT-003/004/005, auto-deny on new message, no HTTP mutation before approval | @test-generator | 38 |
| 50 | `agent/tests/test_pending_security.py` | Create | AT-006 foreign batch, replay, expired | @test-generator | 33 |
| 51 | `agent/tests/test_web_channel.py` | Create | Missing/invalid/expired cookie → 401; SSE event order; history endpoint | @test-generator | 39 |
| 52 | `agent/tests/test_telegram_channel.py` | Create | AT-007 unlinked (no API data call), `/start` link, callback routing, webhook secret check | @test-generator | 40 |
| 53 | `agent/tests/test_ratelimit.py` | Create | AT-013 61st message rejected, no LLM call | @test-generator | 37, 38 |
| 54 | `agent/evals/cases.yaml` | Create | ≥ 96 operation cases (19×5 + review) + ~50 PT/EN utterances + safety cases | @llm-specialist | 27 |
| 55 | `agent/evals/run_evals.py` | Create | `pydantic_evals` Dataset; evaluators: tool/resource/payload match, numeric vs ground truth, safety; exits non-zero below gates | @llm-specialist | 38, 54 |
| 56 | `agent/evals/README.md` | Create | Seeding (`scripts/seed-demo.py`), running, thresholds | @code-documenter | 55 |

### Web

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 57 | `web/src/agent.ts` | Create | `streamMessage()` SSE parser over fetch, `resolveAction()`, `history()` | @javascript-developer | 39 |
| 58 | `web/src/Assistant.tsx` | Create | Chat panel: transcript, streaming bubble, confirm card, error/rate-limit states | @javascript-developer | 57 |
| 59 | `web/src/App.tsx` | Modify | "Assistant" page/route + sidebar/mobile nav entry | @javascript-developer | 58 |
| 60 | `web/src/Account.tsx` | Modify | Settings → Telegram: status, "Connect" (code + deep link + expiry countdown), "Disconnect" | @javascript-developer | 10 |
| 61 | `web/src/index.css` | Modify | Assistant + confirm card styles (existing tokens) | @javascript-developer | 58 |
| 62 | `web/vite.config.ts` | Modify | Dev `server.proxy['/api/agent']` → `VITE_AGENT_PROXY_TARGET` (default `http://localhost:8000`) | @javascript-developer | None |
| 63 | `web/.env.example` | Modify | `VITE_AGENT_PROXY_TARGET` | (general) | 62 |
| 64 | `web/tests/agent.test.mjs` | Create | SSE chunk parser (split frames, multi-line data, error event) | @javascript-developer | 57 |

### Docs

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 65 | `BACKLOG.md` | Modify | Mark bot-token P0 risk resolved; add deferred agent items (YAGNI list) | (general) | 11 |

**Total Files:** 65 (50 create, 15 modify)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|----------------|
| @schema-designer | 1, 2 | Postgres DDL, FK/index design, schema-per-domain convention |
| @dotnet-developer | 3–12, 15–20 | C# services/controllers/EF + xUnit following existing Builder/Service/Controller test pattern |
| @dotnet-specialist | 14, 25 | Swashbuckle CLI / ASP.NET Core tooling specifics |
| @python-developer | 21, 23, 24, 27–33, 37, 39–41 | Typed async Python services, pydantic-settings, httpx, asyncpg |
| @genai-architect | 35, 36, 38 | Agent loop, tool design, approval/guardrail flow (KB `genai`) |
| @llm-specialist | 34, 54, 55 | System prompt, bilingual behaviour, eval design (KB `prompt-engineering`, `genai/evaluation-framework`) |
| @test-generator | 46–53 | pytest fixtures, respx mocks, Pydantic AI `TestModel` |
| @javascript-developer | 57–62, 64 | React 19 + TS, fetch streaming, Vite config |
| @aws-container-ops | 42, 43 | Multi-stage Dockerfile, non-root runtime, compose |
| @ci-cd-specialist | 44 | Azure DevOps pipeline matching `api/azure-pipelines.yml` |
| @shell-script-specialist | 26 | Robust export/drift script |
| @code-documenter | 45, 56 | README/runbooks |
| (general) | 13, 22, 63, 65 | Small config/doc edits |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md` (architect, cloud, data-engineering, dev, dotnet, javascript, platform, python, test)
- Matched by: file type (`.cs`, `.py`, `.tsx`, `.sql`, `Dockerfile`, `azure-pipelines.yml`), purpose keywords (agent/tools/prompt/eval), KB domains (`genai`, `dotnet`, `javascript`, `python`, `pydantic`, `testing`)

---

## Code Patterns

> Pin `pydantic-ai-slim[anthropic,openai]>=2.0,<3`; snippets follow the v2.0.0 docs (deferred tools, `run_stream_events`, `ModelMessagesTypeAdapter`).

### Pattern 1: Agent, deps and approval-gated tools (`core/agent.py`, `core/tools.py`)

```python
# Generic tools over the registry; mutations that change existing data need approval.
from __future__ import annotations

from dataclasses import dataclass
from datetime import date, datetime
from typing import Any
from zoneinfo import ZoneInfo

from pydantic_ai import Agent, DeferredToolRequests, ModelRetry, RunContext

from bthr_agent.api_client import ApiValidationError, bthrClient
from bthr_agent.core.prompts import BASE_INSTRUCTIONS
from bthr_agent.registry.registry import Registry, ResourceName


@dataclass
class AgentDeps:
    user_id: int
    api: bthrClient
    registry: Registry
    timezone: str
    channel: str  # "web" | "telegram"


def build_agent(model: str) -> Agent[AgentDeps, str | DeferredToolRequests]:
    agent = Agent(
        model,
        deps_type=AgentDeps,
        output_type=[str, DeferredToolRequests],
        instructions=BASE_INSTRUCTIONS,
    )

    @agent.instructions
    def now_context(ctx: RunContext[AgentDeps]) -> str:
        now = datetime.now(ZoneInfo(ctx.deps.timezone))
        return f"Current local date/time: {now:%Y-%m-%d %H:%M} ({ctx.deps.timezone}). Channel: {ctx.deps.channel}."

    @agent.tool
    async def describe_resource(ctx: RunContext[AgentDeps], resource: ResourceName) -> dict[str, Any]:
        """Return the fields accepted when creating/updating a resource and its list filters."""
        return ctx.deps.registry.describe(resource)

    @agent.tool
    async def list_records(
        ctx: RunContext[AgentDeps],
        resource: ResourceName,
        start_date: date | None = None,
        end_date: date | None = None,
        filters: dict[str, str] | None = None,
        limit: int = 50,
    ) -> dict[str, Any]:
        """List the user's records of one resource, newest first. Use before updating or deleting."""
        spec = ctx.deps.registry.get(resource)
        rows = await ctx.deps.api.list(spec, ctx.deps.user_id, start_date, end_date, filters or {})
        return {"count": len(rows), "truncated": len(rows) > limit, "records": rows[:limit]}

    @agent.tool
    async def create_record(ctx: RunContext[AgentDeps], resource: ResourceName, data: dict[str, Any]) -> dict[str, Any]:
        """Create a record. Call describe_resource first if unsure about required fields."""
        spec = ctx.deps.registry.get(resource)
        if errors := spec.validate_create(data):
            raise ModelRetry(f"Invalid fields for {resource}: {errors}")
        try:
            return await ctx.deps.api.create(spec, ctx.deps.user_id, data)
        except ApiValidationError as exc:
            raise ModelRetry(f"API rejected the record: {exc.detail}") from exc

    @agent.tool(requires_approval=True)
    async def update_record(
        ctx: RunContext[AgentDeps], resource: ResourceName, record_id: int, changes: dict[str, Any]
    ) -> dict[str, Any]:
        """Change fields of an existing record. The user must confirm before this runs."""
        spec = ctx.deps.registry.get(resource)
        if errors := spec.validate_update(changes):
            raise ModelRetry(f"Invalid fields for {resource}: {errors}")
        return await ctx.deps.api.update(spec, ctx.deps.user_id, record_id, changes)

    @agent.tool(requires_approval=True)
    async def delete_record(ctx: RunContext[AgentDeps], resource: ResourceName, record_id: int) -> str:
        """Delete an existing record. The user must confirm before this runs."""
        spec = ctx.deps.registry.get(resource)
        await ctx.deps.api.delete(spec, ctx.deps.user_id, record_id)
        return f"Deleted {resource} #{record_id}."

    # get_record and get_review follow the same shape (omitted for brevity).
    return agent
```

### Pattern 2: Runner — history, auto-deny, deferred approvals, events (`runner.py`)

```python
# Channel-agnostic turn execution. Channels render TurnEvents; they never call the agent directly.
from __future__ import annotations

import asyncio
from collections import defaultdict
from collections.abc import AsyncIterator
from dataclasses import dataclass
from typing import Literal

from pydantic_ai import DeferredToolRequests, DeferredToolResults, ToolDenied, UsageLimits
from pydantic_ai.messages import PartDeltaEvent, TextPartDelta, FunctionToolCallEvent
from pydantic_ai.run import AgentRunResultEvent


@dataclass
class TurnEvent:
    kind: Literal["delta", "tool", "confirm", "done", "error"]
    data: dict


class AgentRunner:
    def __init__(self, agent, conversations, pending, limiter, settings):
        self._agent, self._conv, self._pending = agent, conversations, pending
        self._limiter, self._settings = limiter, settings
        self._locks: defaultdict[int, asyncio.Lock] = defaultdict(asyncio.Lock)

    async def handle_message(self, deps, text: str) -> AsyncIterator[TurnEvent]:
        if not self._limiter.allow(deps.user_id):
            yield TurnEvent("error", {"code": "rate_limited"})
            return
        async with self._locks[deps.user_id]:
            denied = await self._pending.deny_open(deps.user_id)  # {tool_call_id: ToolDenied}
            results = DeferredToolResults(approvals=denied) if denied else None
            async for event in self._run(deps, prompt=text, deferred=results):
                yield event

    async def resolve_action(self, deps, batch_id: str, approve: bool) -> AsyncIterator[TurnEvent]:
        async with self._locks[deps.user_id]:
            claimed = await self._pending.claim(batch_id, user_id=deps.user_id)  # owner+pending+not expired
            if claimed is None:
                yield TurnEvent("error", {"code": "action_unavailable"})
                return
            decision = True if approve else ToolDenied("The user cancelled this change.")
            results = DeferredToolResults(approvals={c.tool_call_id: decision for c in claimed})
            async for event in self._run(deps, prompt=None, deferred=results):
                yield event

    async def _run(self, deps, prompt, deferred) -> AsyncIterator[TurnEvent]:
        conversation_id, history = await self._conv.load(deps.user_id, runs=self._settings.history_runs)
        limits = UsageLimits(request_limit=8, tool_calls_limit=12)
        async with self._agent.run_stream_events(
            prompt, deps=deps, message_history=history, deferred_tool_results=deferred, usage_limits=limits
        ) as events:
            async for event in events:
                if isinstance(event, PartDeltaEvent) and isinstance(event.delta, TextPartDelta):
                    yield TurnEvent("delta", {"text": event.delta.content_delta})
                elif isinstance(event, FunctionToolCallEvent):
                    yield TurnEvent("tool", {"name": event.part.tool_name})
                elif isinstance(event, AgentRunResultEvent):
                    result = event.result
        await self._conv.append_run(conversation_id, deps.channel, prompt, result)
        if isinstance(result.output, DeferredToolRequests):
            batch = await self._pending.create_batch(deps.user_id, conversation_id, deps.channel,
                                                     result.output.approvals, ttl_minutes=15)
            yield TurnEvent("confirm", {"batch_id": batch.id, "items": batch.previews, "expires_at": batch.expires_at})
        yield TurnEvent("done", {})
```

> Build note: verify event class import paths against the pinned 2.x release (`pydantic_ai.messages` / `pydantic_ai.run`); behaviour above is from the v2.0.0 docs.

### Pattern 3: Atomic pending-action claim (`store/pending.py`)

```python
# One statement enforces owner, status and expiry — no check-then-act race.
CLAIM_SQL = """
UPDATE agent.pending_actions
   SET status = 'claimed', resolved_at = now()
 WHERE batch_id = $1 AND user_id = $2 AND status = 'pending' AND expires_at > now()
RETURNING id, tool_call_id, tool_name, args
"""

EXPIRE_SQL = """
UPDATE agent.pending_actions SET status = 'expired', resolved_at = now()
 WHERE batch_id = $1 AND status = 'pending' AND expires_at <= now()
"""


async def claim(pool, batch_id: str, user_id: int) -> list[PendingCall] | None:
    async with pool.acquire() as conn:
        rows = await conn.fetch(CLAIM_SQL, batch_id, user_id)
        if not rows:
            await conn.execute(EXPIRE_SQL, batch_id)
            return None
        return [PendingCall(**dict(r)) for r in rows]
```

> An expired batch is still answered to the model as denied ("expired") on the user's next message via `deny_open`, so history stays consistent.

### Pattern 4: Chat-scoped bot token endpoint (`AuthController.cs`)

```csharp
[HttpPost("bot/token-for-chat")]
[ProducesResponseType(typeof(BotChatTokenResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> TokenForChat([FromBody] BotChatTokenRequest request)
{
    if (!IsValidBotKey())
        return Unauthorized(new { message = "Invalid bot API key" });

    var userId = await _telegramLinkService.GetLinkedUserIdAsync(request.ChatId);
    if (userId is null)
        return NotFound(new { message = "Chat is not linked" });

    var user = await _userService.GetUserByIdAsync(userId.Value);
    if (user == null)
        return NotFound(new { message = "User not found" });

    var lifetime = TimeSpan.FromMinutes(_configuration.GetValue("Bot:ChatTokenMinutes", 15));
    var isAdmin = await _userService.IsUserAdminAsync(userId.Value);
    var token = _jwtService.GenerateToken(userId.Value, user.Plan, isAdmin, lifetime);
    return Ok(new BotChatTokenResponse(token, userId.Value, DateTimeOffset.UtcNow.Add(lifetime)));
}

private bool IsValidBotKey()
{
    var expected = _configuration["Bot:ApiKey"];
    var provided = Request.Headers["X-Bot-Api-Key"].FirstOrDefault();
    return !string.IsNullOrEmpty(expected) && provided is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided));
}
```

### Pattern 5: Agent schema migration (`V32__create_agent_schema.sql`, house style)

```sql
------------------------------------------------------------
-- AGENT SCHEMA
-- Conversation state owned by the agent service.
------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS agent;

CREATE TABLE agent.conversations (
    id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id     INT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE agent.messages (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    conversation_id  INT NOT NULL REFERENCES agent.conversations(id) ON DELETE CASCADE,
    channel          VARCHAR(16) NOT NULL,
    user_text        TEXT,
    assistant_text   TEXT,
    messages_json    JSONB NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_messages_channel CHECK (channel IN ('web', 'telegram'))
);
CREATE INDEX ix_agent_messages_conversation ON agent.messages (conversation_id, id DESC);

CREATE TABLE agent.pending_actions (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id         UUID NOT NULL,
    user_id          INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    conversation_id  INT NOT NULL REFERENCES agent.conversations(id) ON DELETE CASCADE,
    channel          VARCHAR(16) NOT NULL,
    tool_call_id     VARCHAR(128) NOT NULL,
    tool_name        VARCHAR(64) NOT NULL,
    args             JSONB NOT NULL,
    preview          VARCHAR(500) NOT NULL,
    status           VARCHAR(16) NOT NULL DEFAULT 'pending',
    expires_at       TIMESTAMPTZ NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    resolved_at      TIMESTAMPTZ,
    CONSTRAINT ck_pending_status CHECK (status IN ('pending', 'claimed', 'denied', 'expired'))
);
CREATE INDEX ix_agent_pending_user_open ON agent.pending_actions (user_id) WHERE status = 'pending';
CREATE INDEX ix_agent_pending_batch ON agent.pending_actions (batch_id);

COMMENT ON SCHEMA agent IS 'State of the bthr AI agent service (conversations, history, pending confirmations).';
COMMENT ON TABLE agent.messages IS 'One row per agent run; messages_json holds the Pydantic AI message list for that run.';
COMMENT ON TABLE agent.pending_actions IS 'Update/delete tool calls awaiting user confirmation; expire after 15 minutes.';
```

`V31__create_integration_schema.sql`: `integration.telegram_link_codes (id, user_id FK cascade, code_hash CHAR(64) UNIQUE, expires_at, used_at, created_at)` and `integration.telegram_links (id, user_id FK cascade UNIQUE, chat_id BIGINT UNIQUE, telegram_username, linked_at)`.

### Pattern 6: Resource registry entry (`registry/resources.yaml`)

```yaml
# One entry per user-scoped resource. Schemas are resolved from openapi/bthr.openapi.json.
- name: expenses
  domain: finance
  path: /api/users/{userId}/expenses
  id_param: expenseId
  create_schema: CreateExpenseRequest
  update_schema: UpdateExpenseRequest
  date_field: expenseDate
  list_filters: [start_date, end_date, category]
  description: Money spent. Amount is positive; currencyCode e.g. BRL.
  inline_schema: true   # included in base instructions (frequent)

- name: sleep_logs
  domain: body
  path: /api/users/{userId}/body/sleep-logs
  id_param: sleepLogId
  create_schema: CreateSleepLogRequest
  update_schema: UpdateSleepLogRequest
  date_field: sleepDate
  list_filters: [start_date, end_date]
  description: One night of sleep per entry.
  inline_schema: true
```

### Pattern 7: Configuration (`config.py`)

```python
from pydantic import SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    agent_model: str = "anthropic:claude-sonnet-5-5"
    api_base_url: str = "http://finapi:8080"
    database_url: SecretStr
    jwt_secret_key: SecretStr
    jwt_issuer: str = "bthr.Api"
    jwt_audience: str = "bthr.Api"
    bot_api_key: SecretStr
    telegram_bot_token: SecretStr | None = None
    telegram_mode: str = "webhook"  # webhook | polling | disabled
    telegram_webhook_url: str | None = None
    telegram_webhook_secret: SecretStr | None = None
    default_timezone: str = "America/Sao_Paulo"
    history_runs: int = 20
    rate_limit_per_hour: int = 60
    pending_ttl_minutes: int = 15
    otel_exporter_otlp_endpoint: str | None = None
```

### Pattern 8: Web SSE consumption (`web/src/agent.ts`)

```ts
export type AgentEvent =
  | { kind: "delta"; text: string }
  | { kind: "tool"; name: string }
  | { kind: "confirm"; batch_id: string; items: { preview: string }[]; expires_at: string }
  | { kind: "done" }
  | { kind: "error"; code: string; message?: string }

export async function* streamMessage(text: string, signal?: AbortSignal): AsyncGenerator<AgentEvent> {
  const response = await fetch("/api/agent/messages", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json", Accept: "text/event-stream" },
    body: JSON.stringify({ text }),
    signal,
  })
  if (!response.ok || !response.body) throw new Error(`Agent unavailable (${response.status})`)

  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()
  let buffer = ""
  for (;;) {
    const { value, done } = await reader.read()
    if (done) break
    buffer += value
    let boundary: number
    while ((boundary = buffer.indexOf("\n\n")) !== -1) {
      const frame = buffer.slice(0, boundary)
      buffer = buffer.slice(boundary + 2)
      const data = frame
        .split("\n")
        .filter((line) => line.startsWith("data:"))
        .map((line) => line.slice(5).trimStart())
        .join("\n")
      if (data) yield JSON.parse(data) as AgentEvent
    }
  }
}
```

---

## Data Flow

```text
A. Create via Telegram (AT-001)
1. Telegram → POST /telegram/webhook (secret header verified) → aiogram handler
   │
2. BotClient.token_for_chat(chat_id) → API returns {token, userId} (404 → reply "link your account", stop: AT-007)
   │
3. AgentRunner.handle_message: rate limit → per-user lock → deny_open() → load last 20 runs
   │
4. Agent run: create_record("expenses", {...}) → schema validate → POST /api/users/{id}/expenses (Bearer token)
   │
5. Final text → append run to agent.messages → bot replies (Markdown), "typing" action while running

B. Update with confirmation via web (AT-003)
1. Browser POST /api/agent/messages (cookie) → auth.py validates JWT → UserContext
   │
2. Agent: list_records("expenses", filters) → identifies record → update_record(...) [requires_approval]
   │
3. Run ends with DeferredToolRequests → pending_actions batch (TTL 15 min) → SSE `confirm` event → card
   │
4. User clicks Confirm → POST /api/agent/actions/{batch_id} {approve:true}
   │
5. claim() (owner+pending+unexpired, atomic) → run(deferred_tool_results={id: True})
   │
6. update_record executes → PUT /api/users/{id}/expenses/{expenseId} → SSE `delta`… `done`

C. Linking Telegram (AT-008)
1. Web Settings → POST /api/integrations/telegram/link-code → {code, deepLink t.me/<bot>?start=<code>, expiresAt}
   │
2. User opens deep link → Telegram sends "/start <code>" → bot → POST /api/auth/bot/telegram/link {code, chatId}
   │
3. API: hash(code) match, unused, unexpired → mark used, upsert link → bot confirms; Settings polls GET …/link → "Connected"
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-----------------|----------------|
| bthr.Api (resource + report endpoints) | REST (httpx) | User JWT as `Authorization: Bearer` (web: from cookie; Telegram: chat-scoped 15-min token) |
| bthr.Api (bot endpoints) | REST | `X-Bot-Api-Key` (constant-time compare) |
| LLM providers (Anthropic / OpenAI / Ollama) | Pydantic AI model adapters | Provider API key env vars |
| Telegram Bot API | aiogram 3 (webhook or polling) | `TELEGRAM_BOT_TOKEN`; inbound webhook verified by `X-Telegram-Bot-Api-Secret-Token` |
| PostgreSQL | asyncpg | Dedicated role `bthr_agent` with privileges on schema `agent` only |
| OTel collector (`monitor/`) | OTLP gRPC | Network-internal |
| Traefik (outside repo) | Path routing `/api/agent/*`, `/telegram/*` | TLS at edge |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|---------------|
| Unit (API) | Link service, JWT lifetime, controllers | `TelegramLinkServiceTests.cs`, `TelegramIntegrationControllerTests.cs`, `AuthControllerTests.cs`, `JwtServiceTests.cs` | xUnit + existing builders/`ServiceTestBase` | Keep suite green (294+ tests) + new paths 100% |
| Integration (API) | Bot endpoints end-to-end | `TelegramBotEndpointTests.cs` | `CustomWebApplicationFactory` | AT-007/008/009/014 |
| Unit (agent) | Registry, tools, rate limit | `test_registry.py`, `test_tools.py`, `test_ratelimit.py` | pytest, respx, Pydantic AI `TestModel`/`FunctionModel` | 85% line coverage of `bthr_agent` |
| Component (agent) | Runner confirm/deny/expire, pending security, channels | `test_runner_confirm.py`, `test_pending_security.py`, `test_web_channel.py`, `test_telegram_channel.py` | pytest-asyncio, testcontainers Postgres (+ Flyway migrations applied), `FunctionModel` scripted tool calls | All safety ATs |
| Unit (web) | SSE parser | `web/tests/agent.test.mjs` | `node --test` | Frame splitting, errors |
| Evals (release gate) | Real model vs seeded API | `agent/evals/run_evals.py`, `cases.yaml` | `pydantic-evals`, docker-compose stack + `scripts/seed-demo.py` | ≥ 90% accuracy; 100% safety; manual/nightly (costs tokens) |
| E2E (manual) | Full web + Telegram flows on staging | Checklist in `agent/README.md` | Browser + Telegram app | Happy paths + AT-010/011 |

### Acceptance Test Coverage

| AT | Covered by |
|----|-----------|
| AT-001 create via Telegram | `test_telegram_channel.py` (scripted model) + eval cases |
| AT-002 cross-domain read | Eval cases vs `demo-data-summary.json` |
| AT-003 update requires confirmation | `test_runner_confirm.py` (asserts no PUT before approve) |
| AT-004 delete cancelled | `test_runner_confirm.py` |
| AT-005 pending expiry | `test_runner_confirm.py`, `test_pending_security.py` (clock/`expires_at` in past) |
| AT-006 foreign pending action | `test_pending_security.py` |
| AT-007 unlinked chat | `test_telegram_channel.py` (respx asserts no resource call), `TelegramBotEndpointTests.cs` |
| AT-008 linking flow + code reuse | `TelegramLinkServiceTests.cs`, `TelegramBotEndpointTests.cs` |
| AT-009 expired link code | `TelegramLinkServiceTests.cs` |
| AT-010 shared thread | `test_runner_confirm.py::test_history_shared_across_channels` + manual E2E |
| AT-011 web ↔ data sync | Manual E2E (same API/DB by construction) |
| AT-012 provider switch | Eval run with `AGENT_MODEL=openai:…` |
| AT-013 rate limit | `test_ratelimit.py` (asserts model not invoked) |
| AT-014 old endpoint removed | `TelegramBotEndpointTests.cs` (404) |
| AT-015 API error surfaced | `test_tools.py` (400 → `ModelRetry`) + eval case |
| AT-016 ambiguous target | Eval cases (expects `list_records` then clarification, no pending batch) |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|-------------------|--------|
| Schema validation fails on tool args | `ModelRetry` with field errors → model fixes or asks user | Yes (model, max 2 via tool retries) |
| API 400 (validation) | `ModelRetry` with API message | Yes (model) |
| API 401 on web | SSE `error {code: "session_expired"}` → web shows re-login | No |
| API 401 on Telegram | Refresh chat token once, then reply "please try again" | Yes (1×) |
| API 403 / 404 on record | Tool returns `{"error": "not_found"}` → model tells user | No |
| API 5xx / timeout (10 s) | httpx retry with backoff for idempotent GET (2×); mutations not retried; user-facing apology | GET only |
| LLM provider error / timeout | Error event / Telegram reply; run not persisted | No |
| `UsageLimitExceeded` | Polite "couldn't finish" reply; run persisted | No |
| Pending action expired/foreign/claimed | `action_unavailable` error; UI disables card | No |
| Rate limited | `rate_limited` event/reply; no LLM call | No |
| Telegram webhook bad secret | 401, logged with source IP | No |
| DB unavailable | `/health` 503; request 503 | No |

---

## Configuration

| Config Key | Type | Default | Description |
|------------|------|---------|-------------|
| `AGENT_MODEL` | string | `anthropic:claude-sonnet-5-5` | Pydantic AI model string; switching provider = changing this + key |
| `ANTHROPIC_API_KEY` / `OPENAI_API_KEY` / `OLLAMA_BASE_URL` | secret/string | — | Provider credentials |
| `API_BASE_URL` | string | `http://finapi:8080` | Internal API URL |
| `DATABASE_URL` | secret | — | Postgres DSN for role `bthr_agent` |
| `JWT_SECRET_KEY` / `JWT_ISSUER` / `JWT_AUDIENCE` | secret/string | — / `bthr.Api` / `bthr.Api` | Web cookie JWT verification |
| `BOT_API_KEY` | secret | — | Calls to `/api/auth/bot/*` |
| `TELEGRAM_BOT_TOKEN` | secret | — | Bot credentials |
| `TELEGRAM_MODE` | string | `webhook` | `webhook` \| `polling` \| `disabled` |
| `TELEGRAM_WEBHOOK_URL` / `TELEGRAM_WEBHOOK_SECRET` | string/secret | — | Public webhook + verification secret |
| `DEFAULT_TIMEZONE` | string | `America/Sao_Paulo` | Resolving "today", "last month" |
| `HISTORY_RUNS` | int | `20` | Runs loaded as context |
| `RATE_LIMIT_PER_HOUR` | int | `60` | Per-user message cap |
| `PENDING_TTL_MINUTES` | int | `15` | Confirmation window |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | string | — | Collector endpoint |
| API `Bot__ChatTokenMinutes` | int | `15` | Chat token lifetime |
| API `Telegram__BotUsername` | string | — | Builds `t.me/<bot>?start=<code>` deep link |
| Web `VITE_AGENT_PROXY_TARGET` | string | `http://localhost:8000` | Dev proxy target |

---

## Security Considerations

- **Authorization boundary unchanged:** every data access goes through the API as the user; the agent's DB role can only touch schema `agent`.
- **Bot key blast radius reduced:** `bot/token {userId}` removed; chat tokens issued only for linked chats, 15-min lifetime; key compared in constant time.
- **Link codes:** 8+ char random (base32, ~40 bits), stored as SHA-256, single-use, 10-min TTL, creating a new code invalidates prior ones.
- **Confirmation integrity:** mutations unreachable without a deferred approval; claim is a single atomic `UPDATE … WHERE user_id = $2 AND status='pending' AND expires_at > now()`; Telegram callbacks resolve user from `chat_id`, never from callback payload.
- **Prompt injection (OWASP LLM01/LLM06):** tool results may contain user-authored text (journal entries). Mitigations: the agent can only act as that same user; destructive tools need human approval; instructions tell the model to treat record contents as data. No cross-user data is ever in context.
- **Shared JWT secret:** the agent holds `JWT_SECRET_KEY` for verification only; documented; fallback is `/api/auth/me` validation if the secret must not be shared.
- **PII:** `agent.messages` contains financial/health/journal content → cascades on user deletion; logs never include message text (only ids, tool names, latency); OTel span attributes exclude prompt/response content (`include_content=False`).
- **Webhook:** secret-token header required; Telegram group chats ignored (`chat.type == "private"` only).
- **Supply chain:** dependencies pinned via `uv.lock`; container runs as non-root.

---

## Observability

| Aspect | Implementation |
|--------|----------------|
| Logging | Structured JSON (stdlib logging → OTLP log exporter to Loki); fields: `user_id`, `channel`, `run_id`, `tool`, `latency_ms`; no message content |
| Metrics | OTel metrics: `agent_turns_total{channel,outcome}`, `agent_first_token_seconds` (histogram, SLO < 3 s p95), `agent_turn_seconds`, `agent_tool_calls_total{tool,resource,status}`, `agent_pending_total{outcome}`, `agent_rate_limited_total`, LLM token usage |
| Tracing | `Agent.instrument_all()` + httpx/FastAPI instrumentation; W3C `traceparent` propagated to bthr.Api (already instrumented) → single trace LLM → API → Npgsql in Tempo |

---

## Pipeline Architecture (if applicable)

Not applicable — no data pipeline; operational tables only.

---

## Assumption Resolution (from DEFINE)

| ID | Resolution in this design |
|----|---------------------------|
| A-001 | Swagger is dev-only → committed OpenAPI snapshot + CI drift check (Decision 3) |
| A-002 | Traefik path routing on the same host (Decision 5); dev uses Vite proxy |
| A-003 | Webhook via Traefik `/telegram/*`; `TELEGRAM_MODE=polling` fallback |
| A-004 | Pydantic AI 2.x supports tool calling + streaming for Anthropic/OpenAI/Ollama; verified by AT-012 eval |
| A-005 | List endpoints + `start_date`/`end_date` filters used; aggregates via `get_review`; evals will reveal if new endpoints are needed |
| A-006 | Evals seed with `seed-demo.py` and assert against `demo-data-summary.json` |
| A-007 | Single instance: in-memory locks/limiter; multi-instance path noted (advisory locks) |
| A-008 | Chat tokens use the same `GenerateToken` claims (`sub`, `plan`, `admin`) → accepted by all controllers |

## Open Questions Resolved

| DEFINE Open Question | Decision |
|----------------------|----------|
| Message retention | Keep until user deletion (FK cascade); time-based purge deferred |
| Telegram delivery mode | Webhook in prod, polling in dev |
| Web → agent auth | Same-site cookie via Traefik + local JWT validation |
| Context window | Last 20 runs; summarization deferred |
| Default model | `anthropic:claude-sonnet-5-5`; eval gate run per configured model |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-06 | design-agent | Initial version |

---

## Next Step

**Ready for:** `/build .claude/sdd/features/DESIGN_AI_AGENT.md`
