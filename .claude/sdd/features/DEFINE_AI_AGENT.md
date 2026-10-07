# DEFINE: AI Agent (Web + Telegram)

> A single conversational agent, reachable from the bthr web app and Telegram, that can read, create, update and delete every user record and produce reviews — using the same API and data as the existing screens.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | AI_AGENT |
| **Date** | 2026-10-06 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Designed) |
| **Clarity Score** | 14/15 |
| **Source** | [BRAINSTORM_AI_AGENT.md](BRAINSTORM_AI_AGENT.md) |

---

## Problem Statement

bthr users can manage their 19 finance, body and mind resources only through per-resource forms and dashboards in the web app, which makes quick logging on the go (an expense, a meal, water) and cross-domain questions ("how did I sleep and spend this week?") slow; they need one conversational agent, available in the web app and in Telegram, that can do everything the app can do today against the same records.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| Mobile logger | bthr user away from the computer | Opening the web app and finding the right form to log a R$40 lunch or 500 ml of water takes too long; wants to text it to Telegram |
| Web reviewer | bthr user on the web app | Answering cross-domain questions means navigating many separate screens; wants to just ask |
| Operator | Repo owner running the self-hosted stack | Needs the agent to be safe (no cross-user access, no silent destructive edits), observable, and swappable between LLM providers |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Agent can list, get, create, update and delete records for all 19 user resources (Bills, Budgets, Earnings, Expenses, Goals, Investments, WeeklyRoutines, Workouts, PersonalRecords, Meals, WaterIntake, BodyMetrics, SleepLogs, Habits, HabitLogs, SubstanceLogs, SymptomLogs, MeditationSessions, JournalEntries) and fetch the weekly review (`/api/reports/review`) |
| **MUST** | Same agent reachable from the web app (chat panel with streamed responses) and from a Telegram bot |
| **MUST** | Agent acts strictly as the authenticated end user via the existing REST API; the API remains the sole writer of domain data |
| **MUST** | Creates execute immediately; updates and deletes require explicit user confirmation (web button / Telegram inline keyboard), with pending actions expiring after 15 min and bound to their owner |
| **MUST** | Telegram chats are linked to a bthr account via a single-use, 10-minute code generated from web Settings; unlinked chats cannot read or write any data |
| **MUST** | Replace the existing `POST /api/auth/bot/token {userId}` with a chat-scoped, short-lived token endpoint usable only for linked chats |
| **MUST** | LLM provider selectable by configuration only (no code change) |
| **MUST** | One persistent conversation thread per user, shared across web and Telegram |
| **MUST** | Eval suite on seeded demo data that gates release (see Success Criteria) |
| **SHOULD** | Per-user message rate limit (default 60 messages/hour, configurable) |
| **SHOULD** | OTel traces from the agent exported to the existing collector, linking LLM call → API call → DB in one trace |
| **SHOULD** | New API resources become available to the agent by adding a registry entry only (schemas derived from the API's OpenAPI document) |
| **SHOULD** | Web Settings shows Telegram link status and allows unlinking |
| **COULD** | Agent supports read-only account info (`/api/auth/me`) for "who am I / what plan am I on" questions |

---

## Success Criteria

- [ ] 100% of the 19 resources support list/get/create/update/delete through the agent, and the weekly review is retrievable — verified by at least one eval case per resource × operation (≥ 96 cases, including review)
- [ ] Eval suite (~50+ PT/EN utterances on `seed-demo` data) reaches **≥ 90%** correct tool, resource and payload selection
- [ ] **100%** of update/delete eval cases produce a pending confirmation and perform **0** mutations before confirmation
- [ ] **0** successful cross-user reads or writes in security tests (forged userId, unlinked chat, expired/foreign pending action, reused link code)
- [ ] A record created via Telegram is visible in the web app on the next fetch (same API/DB, no sync step) — 100% of synchronization test cases
- [ ] Web chat first streamed token **< 3 s p95**; single-tool create turn completes **< 8 s p95** (measured via OTel on the reference deployment)
- [ ] Switching provider (e.g., Anthropic → OpenAI) requires changing **only** environment variables; eval suite still ≥ 90% on the default model
- [ ] Rate limit rejects the 61st message within one hour for a user (default config)
- [ ] Old `bot/token {userId}` endpoint returns 404 / is removed; new chat-token TTL ≤ 15 min

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Create via Telegram | Linked Telegram chat for user U | U sends "gastei 40 no almoço hoje" | An Expense (amount 40, food-like category, today) is created for U and the bot replies with a confirmation summary; no confirm prompt |
| AT-002 | Read across domains (web) | U logged in on web, seed-demo data loaded | U asks "how much did I spend on food last month and how many hours did I sleep on average?" | Answer matches values computed from seeded data (ground truth summary) within rounding |
| AT-003 | Update requires confirmation | Existing expense E for U | U says "change that lunch to 45" | Agent shows a preview with Confirm/Cancel; E is unchanged until Confirm; after Confirm, E.amount = 45 |
| AT-004 | Delete cancelled | Existing workout W for U | U asks to delete W, then presses Cancel | W still exists; pending action marked cancelled |
| AT-005 | Pending action expiry | Pending delete created 16 min ago | U presses Confirm | Action is rejected as expired; no mutation |
| AT-006 | Foreign pending action | Pending action P owned by user A | User B attempts to confirm P (forged callback/id) | Rejected; no mutation |
| AT-007 | Unlinked Telegram chat | Chat C not linked | C sends any message | Bot replies with linking instructions; no API data call is made |
| AT-008 | Linking flow | U logged in on web | U generates link code, opens `t.me/<bot>?start=<code>` within 10 min | Chat linked to U; Settings shows "Connected"; reusing the same code fails |
| AT-009 | Expired link code | Code generated 11 min ago | Chat sends `/start <code>` | Linking rejected |
| AT-010 | Shared thread | U chatted on Telegram earlier ("I logged a 5k run") | U opens web Assistant and asks "what did I just log?" | Agent answers using the Telegram conversation history |
| AT-011 | Web ↔ data sync | U creates a meal via the agent | U opens the Meals screen | New meal is listed without manual refresh steps beyond a normal fetch |
| AT-012 | Provider switch | Agent configured for provider X | Operator changes model env var to provider Y and restarts | Agent works; eval suite ≥ 90% |
| AT-013 | Rate limit | U has sent 60 messages in the past hour | U sends another | Polite rate-limit reply; no LLM call made |
| AT-014 | Old bot endpoint removed | Valid `X-Bot-Api-Key` | Call `POST /api/auth/bot/token {userId}` | 404 (endpoint no longer exists) |
| AT-015 | API error surfaced | API returns 400 validation error on create | Agent attempts create with invalid data | Agent explains the problem and asks for the missing/invalid field; no crash |
| AT-016 | Ambiguous target | U has 3 expenses named "lunch" this week | U says "delete the lunch" | Agent asks which one (or lists candidates) before creating a pending delete |

---

## Out of Scope

- Proactive messages: bill-due reminders, habit nudges, scheduled summaries
- Telegram voice notes (speech-to-text) and receipt/photo OCR
- RAG / semantic search over journal entries
- MCP server exposure of the agent's tools
- Cross-domain pattern/insights engine beyond `/api/reports/review`
- Undo log for executed actions
- Agent personality / preference settings (one fixed persona)
- Hand-written domain-specific tools (generic tools only, unless evals show need)
- Full UI_PROMPT redesign (Today/Explore/Insights navigation, glass theme) — only a minimal Assistant chat panel and Settings "Connect Telegram" are in scope
- Account mutations via agent (change password, delete account)
- Group chats in Telegram (private chats only)
- Fixes to unrelated BACKLOG items (refresh tokens, global API rate limiting, health check)

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Agent is a separate Python service (Pydantic AI) in a new top-level `agent/` folder (Approach B from brainstorm) | Second runtime; needs its own Dockerfile, compose, pipeline stage |
| Technical | Agent must call the REST API with the end user's JWT; never a cross-user service account | API `userId == claim` checks remain the authorization boundary |
| Technical | API is the only writer of `finance`/`body`/`mind` data; the agent writes only its own `agent` schema | No direct DB access to domain tables from the agent |
| Technical | All DB schema changes via Flyway migrations in `database/migrations/` (next version V31+) | Agent tables created by migration, not by ORM auto-create |
| Technical | Web auth uses the HTTP-only `access_token` cookie | Web → agent requires same-site deployment (reverse proxy) or credentialed CORS |
| Technical | Must run under existing docker-compose and self-hosted Azure Pipelines setup | Mirror `api/` deploy patterns; secrets via pipeline variables |
| Product | Bilingual input (Portuguese and English) | Prompts and evals must cover both |
| Resource | LLM cost borne by the operator | Per-user rate limit; model configurable to cheaper tiers |

---

## Technical Context

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `agent/` (new Python service); `api/FinPulse.Api/` (auth/link endpoints, removal of old bot token); `database/migrations/` (V31+ `agent` schema); `web/src/` (Assistant panel, Settings link) | Fifth sibling subproject alongside api/web/database/monitor |
| **KB Domains** | `genai` (tool-calling, chatbot-architecture, guardrails, state-machines, evaluation-framework), `pydantic`, `python`, `prompt-engineering`, `dotnet`, `javascript`, `testing` | genai patterns drive tool + confirm design; dotnet for API endpoints; javascript for web panel |
| **IaC Impact** | Modify existing | New `agent/Dockerfile` + `agent/docker-compose.yml`; new pipeline stage in `azure-pipelines.yml`; new secrets (LLM API key(s), `TELEGRAM_BOT_TOKEN`, `TELEGRAM_WEBHOOK_SECRET`, reuse `BOT_API_KEY`); public HTTPS route for the Telegram webhook; OTel exporter pointed at existing collector |

---

## Data Contract (if applicable)

Not a data pipeline. The feature adds operational tables only (new `agent` schema), detailed in Design:

| Entity | Purpose | PII? |
|--------|---------|------|
| conversations | One thread per user | No (user_id FK) |
| messages | Conversation history (user/assistant/tool turns) | **Yes** — may contain financial/health/journal content |
| pending_actions | Unconfirmed update/delete payloads, owner, expiry, status | Yes (payload) |
| telegram_links | chat_id ↔ user_id, linked_at | Yes (Telegram chat id) |
| link_codes | Single-use code, user_id, expires_at, used_at | No |

Requirement: message history is deleted when the owning user is deleted (follows `UsersController` soft-delete semantics — exact retention decided in Design).

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | The API's Swagger/OpenAPI JSON is reachable by the agent (runtime or build-time export) and DTOs carry enough field info to build tool schemas | Must hand-maintain schemas per resource or add an export step | [ ] |
| A-002 | Web app and agent can be served same-site (reverse proxy) so the `access_token` cookie reaches the agent | Need a token-exchange endpoint or credentialed CORS setup | [ ] |
| A-003 | The deployment can expose a public HTTPS endpoint for the Telegram webhook | Fall back to long polling (works behind NAT, one instance only) | [ ] |
| A-004 | Pydantic AI supports the target providers' tool calling and streaming with sufficient parity | Model switch may degrade eval scores; may need per-provider prompt tweaks | [ ] |
| A-005 | Existing per-resource list endpoints' filters (date range, category) are enough for agent reads; no aggregation endpoints needed for MVP | Agent must page through records and aggregate client-side, hurting latency/accuracy; may require new query endpoints | [ ] |
| A-006 | `scripts/seed-demo.py` data + `demo-data-summary.json` are deterministic enough to serve as eval ground truth | Need a fixed eval fixture/snapshot | [ ] |
| A-007 | Single agent instance is sufficient (personal/small user base) | Need distributed rate limiting and Telegram update dedup | [ ] |
| A-008 | Short-lived JWTs minted for linked chats are accepted by all existing controllers (same claims as web tokens) | Need claim mapping changes in `JwtService` | [ ] |

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific users, specific friction, explicit "do everything the app does" scope |
| Users | 3 | Three personas with concrete pain points, incl. operator safety needs |
| Goals | 3 | MoSCoW-prioritized, each traceable to a brainstorm decision |
| Success | 3 | Numeric gates: ≥90% eval, 100% confirm safety, 0 cross-user, <3 s / <8 s p95, 60/h limit |
| Scope | 2 | Out-of-scope explicit; minimal web UI boundary vs. UI_PROMPT redesign may need refinement in Design |
| **Total** | **14/15** | |

**Minimum to proceed: 12/15** ✅

---

## Open Questions

Non-blocking — resolve during Design:

1. Retention policy for `agent.messages` (keep forever vs. N days; behaviour on account soft-delete).
2. Telegram delivery mode: webhook (preferred) vs. long polling, depending on A-003.
3. Web → agent auth transport: reverse-proxy same-site cookie vs. a short-lived token exchange, depending on A-002.
4. Context window strategy for the shared thread (last N turns vs. summarization).
5. Default model per provider for the ≥ 90% eval bar.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-06 | define-agent | Initial version from BRAINSTORM_AI_AGENT.md + eval and latency targets |

---

## Next Step

**Next Step:** `/build .claude/sdd/features/DESIGN_AI_AGENT.md`
