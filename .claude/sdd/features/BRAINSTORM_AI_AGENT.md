# BRAINSTORM: AI Agent (Web + Telegram)

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | AI_AGENT |
| **Date** | 2026-10-06 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "Let's plan the AI Agent to live in this repository, the same agent needs to talk with the UI and Telegram. What is the best architecture we can add for this project? The agent needs to be able to do everything that we can do now in the project."

**Context Gathered:**
- bthr is a personal life-tracking platform: ASP.NET Core 10 + EF Core + PostgreSQL (`api/`), React + Vite SPA (`web/`), Flyway migrations with `finance` / `body` / `mind` schemas (`database/`), OTel → Tempo/Loki/Prometheus/Grafana (`monitor/`).
- "Everything we can do now" = 19 user-scoped CRUD resources under `api/users/{userId}/...` (Bills, Budgets, Earnings, Expenses, Goals, Investments, WeeklyRoutines, Workouts, PersonalRecords, Meals, WaterIntake, BodyMetrics, SleepLogs, Habits, HabitLogs, SubstanceLogs, SymptomLogs, MeditationSessions, JournalEntries) + `GET /api/reports/review` + account ops (`/api/auth/me`, change-password, `UsersController`).
- Auth is a JWT in an HTTP-only `access_token` cookie; every controller checks `GetCurrentUserId() == userId`.
- A bot integration was anticipated: `POST /api/auth/bot/token` ([AuthController.cs:140](../../../api/FinPulse.Api/Controllers/AuthController.cs)) mints a JWT for **any** `userId` given `X-Bot-Api-Key`; `BOT_API_KEY` is wired in `api/docker-compose.yml` and `azure-pipelines.yml`. No bot code exists.
- [UI_PROMPT.md](../../../UI_PROMPT.md) defines the product vision: a Cleo-style assistant where "users manage their information through either conversation or conventional screens. Both experiences must use the same records and remain synchronized."
- Python is already used in the repo (`scripts/seed-demo.py`, `scripts/seed-random.py`).

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | New top-level `agent/` (Python service); small changes in `api/` (auth endpoints, Telegram link), `database/migrations/` (new `agent` schema), `web/` (Assistant screen + Connect Telegram) | Agent is a 5th sibling subproject with its own Dockerfile/compose |
| Relevant KB Domains | `genai` (tool-calling, chatbot-architecture, guardrails, evaluation-framework, state-machines), `pydantic`, `python`, `prompt-engineering`, `dotnet` (auth endpoint changes), `testing` | Patterns for tool design, confirm flow, evals |
| IaC Patterns | docker-compose per subproject; Azure Pipelines on a self-hosted agent; no Terraform | Add `agent/docker-compose.yml` + pipeline stage mirroring `api/` |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Which LLM provider should power the agent? | Provider-agnostic | Use a framework with pluggable model backends; model chosen by env var |
| 2 | How should the agent behave on data mutations? | Creates execute directly; updates/deletes require confirmation | Need a pending-action store + confirm UX on both channels |
| 3 | How should a Telegram chat be linked to a bthr user? | One-time link code generated from the web UI | New link-code + chat-link endpoints; replace the arbitrary-userId bot token |
| 4 | Should conversation history be shared across channels? | One thread per user across all channels | Conversation persistence keyed by user, not by channel |
| 5 | Which language/framework for the agent service? | Python + Pydantic AI | FastAPI for web SSE, aiogram for Telegram, Pydantic models for tool schemas |
| 6 | How should tools map to ~85 endpoints? | Generic per-resource tools driven by a registry | ~6 tools with a `resource` enum; schemas derived from the API's OpenAPI document |

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `scripts/seed-demo.py`, `scripts/seed-random.py` | 2 | Seed a test user with realistic finance/body/mind data |
| Output examples | `scripts/demo-data-summary.json`, `scripts/random-data-summary.json` | 2 | Expected record counts/values after seeding — ground truth for read-question evals |
| Ground truth | Same summaries + seeded DB | — | Verify answers like "how much did I spend on food last month?" |
| Related code | `api/FinPulse.Api/DTOs/*.cs`, Swagger `/swagger/v1/swagger.json`, `web/src/resources.ts` | 19 resources | Source for tool schemas and resource field descriptions |

**How samples will be used:**
- Seeded DB is the fixture for an agent eval suite (read accuracy, correct tool/resource selection, correct create payloads).
- Summary JSONs are ground truth for numeric answers.
- A starter set of PT/EN user utterances will be authored during `/define` (none exist today).

---

## Approaches Explored

### Approach A: Agent module inside the .NET API (originally recommended)

**Description:** `Agent/` module in `FinPulse.Api` using `Microsoft.Extensions.AI`; tools call existing service interfaces in-process; web SSE + Telegram webhook controllers in the API.

**Pros:**
- In-process reuse of services, auth, OTel, test builders, pipeline
- No service-to-service auth needed

**Cons:**
- Long LLM calls inside the API process
- Thinner .NET agent ecosystem

---

### Approach B: Separate Python agent service calling the REST API ⭐ Selected

**Description:** New `agent/` service (Python, Pydantic AI, FastAPI, aiogram). Calls the existing REST API **as the end user** with that user's JWT. Generic tools driven by a resource registry + OpenAPI-derived schemas.

**Pros:**
- Rich, provider-agnostic agent framework (Pydantic AI)
- API remains the single source of business rules and authorization
- Agent scales/deploys independently; LLM latency isolated from the API
- Tool schemas generated from OpenAPI → new resources need only a registry entry

**Cons:**
- Second runtime in the repo
- Extra network hop per tool call
- Depends on a service-to-service token path → must harden `bot/token` (see Key Decisions)

---

### Approach C: MCP server + separate agent host

**Description:** Expose services as an MCP server (C# MCP SDK); separate agent host and channel adapters consume it.

**Pros:**
- Tools reusable by Claude Desktop / other MCP clients

**Cons:**
- Three moving parts for an MVP that needs one; deferred (see YAGNI)

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach B — separate Python (Pydantic AI) agent service |
| **User Confirmation** | 2026-10-06 |
| **Reasoning** | User preference for a dedicated agent service with a mature Python agent framework; API stays the authority for data and authorization |

### Architecture Sketch

```text
 Web (React)                          Telegram
    │ POST /agent/chat (SSE)              │ webhook (secret token header)
    │ cookie: access_token                ▼
    ▼
 ┌───────────── agent/ (Python, FastAPI) ────────────────┐
 │ channels/web.py      channels/telegram.py (aiogram)   │
 │        └──────────┬──────────┘                        │
 │            AgentRunner (Pydantic AI, model from env)  │
 │   tools: list_records · get_record · create_record    │
 │          update_record* · delete_record* · get_review │
 │          (* → pending action → confirm button)        │
 │   registry: resources.yaml + schemas from /swagger    │
 │   api_client.py (httpx, per-request user JWT)         │
 └──────────────┬────────────────────────────────────────┘
                │ REST /api/users/{id}/...  (user's JWT)
                ▼
        FinPulse.Api (.NET)  ──►  Postgres
                                   └ schema `agent`: conversations, messages,
                                     pending_actions, telegram_links
```

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Agent always calls the API with the **end user's** JWT | API's `userId == claim` checks remain the authorization boundary; agent can't exceed user privileges | Service account with cross-user access |
| 2 | Web channel forwards the user's existing `access_token` cookie | No second login; requires same-site deployment or credentialed CORS | Separate agent login |
| 3 | Replace `POST /api/auth/bot/token {userId}` with `POST /api/auth/bot/token-for-chat {chatId}` — returns a short-lived (~15 min) JWT **only for linked chats**, still gated by `X-Bot-Api-Key` | Current endpoint lets a leaked key impersonate any user | Keep arbitrary-userId token minting |
| 4 | Telegram linking via single-use, 10-min code from web Settings → `t.me/<bot>?start=<code>` | Proves chat ownership by an authenticated web user | Password entry in Telegram; single-user whitelist |
| 5 | Provider-agnostic via Pydantic AI; model selected by env (e.g. `anthropic:…`, `openai:…`, `ollama:…`) | User requirement | Hard-coding one provider SDK |
| 6 | ~6 generic tools with `resource` enum; per-resource schemas from the API OpenAPI doc | Small tool surface keeps tool selection accurate; new resources auto-exposed | One tool per endpoint (~85); hand-written domain tools |
| 7 | Creates execute immediately; updates/deletes create a `pending_action` (15-min TTL, owner-bound) confirmed via UI button / Telegram inline keyboard | Cleo-like speed with a safety net on destructive ops | Always-confirm; fully autonomous |
| 8 | One conversation thread per user shared across channels, stored in a new Flyway-managed `agent` schema (owned/written by the agent service) | Continuity across interactions (UI_PROMPT); consistent with existing schema-per-domain migrations | Per-channel threads; stateless |
| 9 | OTel instrumentation exported to the existing collector | Single trace from LLM call → API → Postgres in Tempo | Separate observability for the agent |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Proactive reminders (bill due, habit nudges) | Needs scheduler + notification preferences; MVP is user-initiated | Yes |
| Telegram voice notes / receipt photo OCR | Separate STT / vision pipelines | Yes |
| RAG / semantic search over journal entries | List/filter tools cover recent history | Yes |
| MCP server exposure of tools (Approach C) | No second consumer yet; registry can be wrapped later | Yes |
| Cross-domain pattern insights | Belongs to the Insights area; `reports/review` suffices for MVP | Yes |
| Undo log | Confirm step covers destructive writes | Yes |
| Agent personality / preference settings | Ship one fixed persona first | Yes |
| Hand-written domain tools (`log_expense`, …) | Generic tools cover it; add only where evals show misuse | Yes |
| One tool per endpoint auto-generated | Context bloat, worse tool selection | No |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Architecture concept & components | ✅ | "Yes, looks right" | No |
| Auth flows, confirm mechanism, MVP scope / YAGNI | ✅ | "Yes, looks right" | No |

---

## Suggested Requirements for /define

### Problem Statement (Draft)
bthr users can only manage their finance, body and mind records through forms and dashboards; they need a single conversational agent, reachable from the web app and Telegram, that can read, create, update and delete all of their records and produce reviews, while staying synchronized with the existing UI.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| bthr user on the go | Logging an expense/meal/water/workout through forms is slow; wants to text it to Telegram |
| bthr user on web | Wants to ask questions across domains ("how did I sleep and spend this week?") without navigating 19 screens |

### Success Criteria (Draft)
- [ ] Agent can list/get/create/update/delete records for all 19 resources and fetch the weekly review, from both web and Telegram
- [ ] A record created via Telegram appears in the web UI without any sync step (same API/DB)
- [ ] Updates/deletes never execute without explicit confirmation; pending actions expire after 15 min
- [ ] A Telegram chat can only act on behalf of the user who linked it; old `bot/token {userId}` endpoint removed
- [ ] Switching provider requires only an env var change
- [ ] Eval suite on seeded data (seed-demo) reaches an agreed pass rate for tool/resource selection and numeric answers
- [ ] Each agent turn produces one OTel trace spanning LLM + API calls

### Constraints Identified
- API remains the sole writer of domain data (`finance`/`body`/`mind` schemas); the agent writes only the `agent` schema
- All DB schema changes via Flyway migrations
- Must run in the existing docker-compose / Azure Pipelines (self-hosted) setup
- Web channel requires same-site deployment or credentialed CORS for cookie forwarding
- Assumes Swagger JSON is reachable by the agent (at build time or runtime) for schema generation
- Bilingual input (PT/EN) expected

### Out of Scope (Confirmed)
- Proactive notifications, voice/photo input, RAG over journals, MCP server, insights engine, undo log, persona settings (see YAGNI table)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 6 (+1 sample question) |
| Approaches Explored | 3 |
| Features Removed (YAGNI) | 9 |
| Validations Completed | 2 |
| Duration | ~1 session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_AI_AGENT.md`
