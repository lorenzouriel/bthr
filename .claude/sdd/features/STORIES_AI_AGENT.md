# STORIES: bthr AI Agent (Web + Telegram)

> Rebuilt on 2026-10-07 against the working tree, after the migration reorganization and bthr rename.
> Status: implementation plan; no feature stories completed. Story 0 contains a product decision that must be settled before schema-dependent implementation.
> Sources: [BRAINSTORM](./BRAINSTORM_AI_AGENT.md), [DEFINE](./DEFINE_AI_AGENT.md), [DESIGN](./DESIGN_AI_AGENT.md).
> This revision supersedes conflicting implementation details and acceptance counts in those earlier documents. Source code and the verified API contract take precedence over old line-number references.

## Outcome and scope

Add a separate Python service in `agent/` that exposes one persistent conversation per user through web chat and private Telegram messages. Domain reads and writes go through the existing API with that user's identity. Creates run directly; supported updates and deletes require explicit confirmation of server-stored arguments. Support Portuguese and English, configurable model providers, and existing resource permissions.

Keep the existing API capabilities: 19 resources, of which 16 support edits/deletes and three are append-only. No new account mutation tools, direct domain SQL, proactive notifications, voice/image ingestion, or market-data integration in this feature.

## Audit findings and resolutions

| Finding | Evidence in this checkout | Resolution |
| --- | --- | --- |
| Migration references are obsolete | `database/migrations/` now contains V1-V22, ending in indexes | Reserve V23 for integration and V24 for agent state; apply in numerical order. |
| API and database are incompatible | `Models/User.cs` maps `plan`; V2 omits it. `Models/Investment.cs` maps three columns absent from V7. `Models/Bill.cs` and bill DTOs/services still use the template model while V8 defines bill occurrences | Story 0 is a prerequisite, with an explicit contract decision and real PostgreSQL tests. Passing EF InMemory tests does not establish SQL compatibility. |
| Plan-based access is unresolved | Budgets, goals and investments have `[RequiresPlan(1)]`; login reads `User.Plan` | Resolve entitlements with the schema decision. Do not silently default every user to plan 0 or grant admin access to make tests pass. |
| Not all resources support CRUD | PersonalRecordsController, SubstanceLogsController and SymptomLogsController expose only GET collection and POST | Registry capabilities drive tool authorization and eval coverage; update/delete attempts for these resources are rejected without HTTP calls. |
| Claimed GET-by-id endpoint does not exist | None of the 19 resource controllers currently exposes item GET | `get_record` is a collection lookup, with completeness rules below. Do not invent routes. |
| Date/filter assumptions differ by resource | Bills use year/month; habits and weekly routines have no date filters; sleep DTO uses bedTime/wakeTime | Derive request fields and filters from the reconciled contract; date field is optional and sleep uses bedTime, not sleepDate. |
| Current token has no iat claim | `Services/JwtService.cs` supplies sub, plan, admin, jti and exp | Preserve existing signature; test new token expiry against the issuance clock, or deliberately add iat with compatibility tests. |
| Approval persistence is incomplete | Old plan used per-action claimed rows without durable execution outcomes | Introduce a batch plus action state machine, atomic whole-batch claims and explicit unknown outcomes. No claim of exactly-once writes across API and agent DB. |
| Confirmation history can be invalid after restart/denial | Old plan truncates runs independently and can leave unanswered tool calls | Persist resumable history and close all deferred calls on denial/expiry; truncate complete conversation segments. |
| Cached Telegram tokens can outlive unlink/relink | Old plan caches chat identity for 15 minutes | Resolve current link on every message/callback and enforce link-version revocation for bot JWTs at API authorization time. |
| Web cookie auth needs CSRF handling | Secure API cookies currently use SameSite=None | Explicit trusted-origin and CSRF protection for browser mutation endpoints, including link/unlink and chat/action submission. |
| Deployment networking is underspecified | API compose uses project-scoped fin-network and service finapi | Story 8 supplies a shared network/routing configuration; identical compose network keys in separate projects are insufficient. |
| Eval isolation is insufficient | Existing seed script uses :5173; clearing conversations does not reset changed domain data | Use an isolated fixture database/users per case or reset all affected domain fixtures. Never truncate shared/demo production state. |
| Export/dependency assumptions are unproven | No agent directory/lock exists; Program startup requires JWT and OTel settings | Verify the export path and SDK APIs with a compatibility spike, then pin tested versions. Require AGENT_MODEL; remove the unverified hardcoded model identifier. |
| Story dependency graph was inconsistent | V24 follows V23, and deployment was not gated by evals | Explicit dependency table below; release follows both channel acceptance and eval gates. |

## Decisions to settle before implementation

1. **Schema versus app contract (required for Story 0):**
   - Preserve the current database: update API DTOs/models/services, web forms/reports and seeds for bill occurrences; remove writable stored investment value/return fields; define subscription entitlement behavior independently of the removed column. Missing market values stay unavailable, never invented.
   - Preserve current app behavior: revise the undeployed initial migrations to retain user plan, stored investment fields and bill templates; verify the resulting SQL with the app.
   - Do not choose by whichever makes tests easiest. Record the user's decision here and in DEFINE/DESIGN before implementing it.
2. **Model:** operator supplies a provider/model identifier available to their account; local tests use scripted models without credentials. Python 3.12 is the initial runtime target, subject to dependency resolution.
3. **Web authentication default:** retain local HS256 validation from the design, explicitly recognizing that possession of the signing secret also permits signing. Require issuer, audience, exp and positive integer sub, and pin HS256. Never expose tokens to browser JS, persisted transcripts or model context.
4. **Operational defaults:** America/Sao_Paulo, last 20 complete conversation segments, 60 model turns/hour/user, 15-minute pending TTL, one agent worker/replica. These are configurable; multi-worker deployment is out of scope until locking/limiting is distributed.
5. **Deployment inputs:** bot username/token, model credentials, public origin, webhook URL, DB credentials, shared Docker network and Azure variable group are operator configuration. Missing inputs block the affected deployment, not offline implementation.

## Verified resource inventory

Paths are relative to `/api/users/{userId}/`. The authenticated user supplies userId; tools cannot override it. All resources support collection list and create; get is a client-side lookup. Revalidate filters and DTOs after Story 0.

| Resource | Path suffix | Update/delete | List filters today |
| --- | --- | --- | --- |
| bills | bills | Yes | year, month |
| budgets | budgets | Yes | start_date, end_date |
| goals | goals | Yes | start_date, end_date |
| earnings | earnings | Yes | start_date, end_date, category |
| expenses | expenses | Yes | start_date, end_date, category |
| investments | investments | Yes | start_date, end_date, investment_type, category |
| weekly_routines | body/weekly-routines | Yes | none |
| workouts | body/workouts | Yes | start_date, end_date |
| personal_records | body/personal-records | No | start_date, end_date |
| meals | body/meals | Yes | start_date, end_date |
| water_intake | body/water-intake | Yes | start_date, end_date |
| body_metrics | body/body-metrics | Yes | start_date, end_date |
| sleep_logs | body/sleep-logs | Yes | start_date, end_date |
| habits | body/habits | Yes | none |
| habit_logs | body/habit-logs | Yes | start_date, end_date, habit_id |
| substance_logs | body/substance-logs | No | start_date, end_date |
| symptom_logs | body/symptom-logs | No | start_date, end_date |
| meditation_sessions | mind/meditation-sessions | Yes | start_date, end_date |
| journal_entries | mind/journal-entries | Yes | start_date, end_date |

Reports use `GET /api/reports/review`; domain is finance/body/mind/all and all requires combine=true. Preserve its date-range validation and obtain all report numbers through API responses.

## Story 0: Reconcile the deployable API contract

**Depends on:** schema/app decision above. **Files:** relevant existing migrations, API models/DTOs/services/controllers, web resources/reporting, seed scripts and tests.

- Inventory EF mappings against a database built by Flyway V1-V22, including generated columns, date types, required fields, constraints and relationship behavior.
- Implement the selected contract consistently through API, UI, authentication/entitlements and fixture scripts. Keep the earlier migration reorganization and bthr naming.
- Add PostgreSQL integration coverage using Flyway-created tables, not EnsureCreated. Exercise registration/login, plan-gated access, bills, investments, reports and representative body/mind writes.
- Capture the resulting route, operation, request and response contract for Story 3. Update DEFINE/DESIGN to reflect the selected behavior.

**Done when:** a fresh migrated PostgreSQL instance supports real authenticated API/UI workflows; missing-column errors and entitlement ambiguity are resolved; existing and new tests pass. No later agent story is called release-ready before this gate.

## Story 1: Telegram account linking

**Depends on:** 0. **Files:** `V23__create_integration_schema.sql`, new Telegram models/DTOs/link service/controller, ApplicationDbContext, Program, API configuration/tests.

- Create `integration.telegram_link_codes` (identity id, user_id FK cascade, unique code_hash CHAR(64), expires_at, used_at, created_at) and `integration.telegram_links` (identity id, unique user_id FK cascade, unique chat_id BIGINT, optional telegram_username, linked_at, link_version UUID). Add indexes and comments for every table/column.
- Authenticated user endpoints: POST `/api/integrations/telegram/link-code`, GET `/link`, DELETE `/link`. User identity comes only from claims. Missing bot username returns 503 from code creation; unrelated API operations still boot.
- Generate a cryptographically random 10-character base32 code, persist only SHA-256, expire at 10 minutes, invalidate older unused codes. Use TimeProvider in tests.
- Serialize code creation/redeeming/unlinking for the same user in PostgreSQL transactions. Redeem only an unused/unexpired code, and consume it atomically with link creation/update. Unique chat conflicts roll back; one code cannot succeed twice under concurrency. Relink rotates link_version; unlink invalidates outstanding codes.
- Apply bounded link-code issuance/redemption limits to resist guessing and exhaustion. Protect browser write endpoints with the chosen CSRF mechanism.

**Done when:** real PostgreSQL race tests prove single-use and uniqueness; expiry/conflict/unlink tests pass; no raw codes or tokens appear in logs. InMemory tests cover service/controller branches only, not transactional guarantees.

## Story 2: Chat-scoped bot authentication

**Depends on:** 1. **Files:** AuthController, JwtService, AuthDTOs/TelegramDTOs, Program authentication events, API compose/pipeline/env examples and tests.

- Remove POST `/api/auth/bot/token` and BotTokenRequest; assert the old route returns 404.
- Add bot-key-protected POST `/api/auth/bot/telegram/link` and `/api/auth/bot/token-for-chat`. Compare nonempty configured/provided keys in constant time; reject malformed chat IDs. Never accept a target user ID.
- Token issuance resolves the link and active user, derives entitlements from Story 0 and caps lifetime at 15 minutes. Retain existing GenerateToken signature for ordinary callers; use a separate bot-token method or overload for lifetime and bot claims.
- Include source=telegram, chat identifier and link_version in bot tokens. API token validation checks the current link/version and active user, so unlink/relink invalidates issued bot tokens. Existing browser tokens retain their current validation behavior.
- Agent obtains a new chat identity for each message and callback; do not cache authorization across turns. Do not rerun an entire turn after an API 401 because earlier creates may already have succeeded.
- Wire bot username, key and TTL consistently through compose, env examples and pipelines; record the removed impersonation endpoint in BACKLOG only after tests pass.

**Done when:** link -> token -> resource flow passes on PostgreSQL; foreign/unlinked/inactive users cannot obtain data; old tokens fail after unlink/relink; expiry is checked with a controlled clock; logs exclude credentials.

## Story 3: Agent scaffold and verified API registry

**Depends on:** 0; export the final snapshot again after 2. **Files:** new `agent/` package, `pyproject.toml`, `uv.lock`, `.env.example`, configuration/telemetry/main, registry, client, snapshot/export tooling, offline tests.

- Resolve Python 3.12 dependencies for Pydantic AI slim provider extras, FastAPI, httpx, asyncpg, settings, YAML/schema validation, OTel and tests. Pin a tested compatible release set in uv.lock; do not copy unverified event imports or version claims from the old design.
- Add a small scripted-model spike proving deferred approval/resumption and streamed events using the pinned SDK. Keep this as a regression test.
- Export OpenAPI with a pinned compatible Swashbuckle tool and deterministic test-only startup configuration (JWT/OTel/DB settings). Normalize irrelevant output differences; `--check` fails on actual contract drift. Provide a platform-neutral entry point usable on Windows and CI.
- Package the YAML and JSON snapshot as Python package data; resolve by package location, not current directory. Build/install the package in a clean environment to verify this.
- Registry validates all 19 paths, HTTP operations, path/query parameters, request schema dialect, references, nullability and writable fields. Use the exported OpenAPI dialect correctly; do not feed OpenAPI 3.0 nullable schemas directly to Draft202012Validator.
- Unsupported update/delete schemas are absent for append-only resources. Reject unknown fields, foreign user identifiers and response-only/generated fields. Preserve API partial-update/null semantics; do not imply that null clears a field unless supported.
- BthrClient uses one shared AsyncClient with the fixed API base URL. Bearer token/user ID come from trusted dependencies. Only allow registry routes/filters. Map 400, 401, 403, 404, 409, 429 and 5xx distinctly; never describe a permission failure as missing data.
- Retry bounded transient GET failures only. Never automatically retry mutations. Apply request/response limits and timeouts without treating truncated results as complete.
- AGENT_MODEL and selected provider credentials are required when enabling model execution; Telegram config is required only for enabled Telegram modes. Provide liveness/readiness separately.

**Done when:** registry/client/SDK spike tests pass without model credentials; clean package install includes assets; deterministic export detects DTO changes; no unsupported route or mutation retry is possible.

## Story 4: Seven tools with enforceable approvals

**Depends on:** 3. **Files:** agent core prompts, dependencies, tool definitions, tests.

- Expose describe_resource, list_records, get_record, create_record, update_record, delete_record and get_review. Cap model calls/tool calls per turn and validation retries.
- Tools get user ID, token, API URL and registry from server dependencies; none are model arguments. Reject unsupported operations before generating pending actions or HTTP calls.
- List responses include completeness/truncation information. Get-by-id searches a complete relevant collection before output truncation. Bills require an explicit applicable period under the current API; if completeness cannot be established, return an incomplete-search result and ask for a period rather than claiming not-found.
- Read and resolve an exact target before proposing a mutation. Ambiguity requires clarification. Show a useful server-rendered preview with record identity and changed values, not just a tool name/ID.
- Update/delete defer with immutable validated arguments. Execution verifies a server-side authorization record matching owner, batch, tool call and argument hash; SDK approval alone is insufficient. Never accept client-supplied tool history/arguments.
- New or altered update/delete calls on a resumed run require a new confirmation. Creates remain direct as specified in DEFINE; transport duplicates are controlled by Story 5.
- Bilingual instructions resolve dates in the configured timezone, separate record content from instructions, and never invent missing amounts or market values. Enforce essential permissions in code, not prompt substring tests.
- Instrument with content capture disabled; exclude tokens, link codes, message text, payloads and sensitive query attributes from logs/traces.

**Done when:** scripted tests prove zero PUT/DELETE before authorization, no append-only edits, no cross-user route substitution, correct schema/error handling and new approval for changed arguments.

## Story 5: Durable conversation and execution state

**Depends on:** 1 (V23 ordering), 4. **Files:** `V24__create_agent_schema.sql`, agent store/runner/rate limiter and PostgreSQL tests.

- Create conversations (one per user), turns/messages, pending_batches, pending_actions and inbound_events. Include FK cascades, indexes and comments. Channels are web/telegram; inbound event identity is unique within its channel. Never persist bearer tokens.
- A turn stores request ID, ordered model messages, display text, outcome and execution checkpoints. Persist completed tool results even when a later model call fails; otherwise a repeated request could recreate completed writes.
- A batch stores owner/conversation, originating turn, expiry and decision. Actions store tool_call_id, validated args/hash, preview, execution status/result and timestamps. Statuses distinguish pending, executing, succeeded, failed, denied, expired and unknown.
- Claim the whole pending/unexpired owner-matched batch once in a transaction; claim/deny/new-message invalidation share the same serialization boundary. Duplicate clicks cannot partially claim separate actions. Each action executes once per claim and records its own outcome; partial batch success is reported explicitly.
- A crash or timeout after sending a mutation can leave an unknown outcome. Do not replay it automatically: reconcile via API reads or report the uncertainty. Exactly-once effects require future API idempotency support and are not promised here.
- Resume using the original pending run's server-stored messages plus the pinned SDK's DeferredToolResults. Denial, expiry and new-message invalidation close every outstanding tool call. Retain the pending run even if it is outside the display history window.
- Truncate history at complete conversation-segment boundaries (including approval and resolution). Return active pending cards in display history so reload/restart remains usable.
- Serialize turns per user across web/Telegram in the single worker. Check the message limit before model invocation; confirmation continuations have a separate bounded allowance so a pending decision can still be resolved. Bound lock/rate-limit memory and clean inactive entries.
- Record durable inbound event IDs before execution. Duplicate web request IDs or Telegram update IDs return existing status/results and do not invoke the model or mutations again. Recover abandoned processing events as unknown/failed, never blind replay.
- Emit tool/delta/confirm plus exactly one terminal done/error for connected clients. Persist outcome on disconnect; reconnect reads history by request ID. Shutdown closes clients/pools and records interrupted turns.

**Done when:** real PostgreSQL tests cover concurrent claims, double clicks, wrong owners, expiry, restart, denial, new-message races, duplicate delivery, partial failure and crash uncertainty. Complete history can resume without orphaned tool calls.

## Story 6: Web Assistant

**Depends on:** 5. **Files:** agent auth/web routes; web agent client, Assistant.tsx, App.tsx, index.css, vite config/env/tests.

- Cookie-authenticated GET/POST `/api/agent/messages` and POST `/api/agent/actions/{batch_id}`. Validate user identity, text length and request ID; clients supply only text or approval decisions, never execution payloads.
- Enforce configured trusted origins and a session-bound CSRF mechanism for browser mutations. Validate requests before SSE begins; use 401/403 for rejected authentication/origin. Production uses the same HTTPS origin.
- Stream SSE with no buffering/cache and heartbeat handling. Parse LF/CRLF, multiline data, split UTF-8, multiple frames, comments, malformed events and EOF without a terminal event. Do not reconnect/retry a POST automatically.
- Assistant page supports persisted transcript/pending cards, stream cancellation, expiry, confirm/cancel, disabled duplicate submissions, errors and authentication expiry. Render model text safely as text. Refresh affected existing data through the app's normal fetch mechanism.
- Add /assistant desktop/mobile navigation using existing styling. Put `/api/agent` Vite proxy before `/api`; use AGENT_PROXY_TARGET and keep API_PROXY_TARGET. Public runtime base URLs and proxy routes must agree.

**Done when:** web build/tests pass; browser smoke test covers streaming, reload with pending action, confirmation, data refresh and session expiry; auth/CSRF/channel tests pass with no model key.

## Story 7: Telegram and account settings

**Depends on:** 2, 5; web settings integrate with 6. **Files:** BotClient, Telegram channel/lifecycle, Account.tsx, agent configuration/tests.

- Support disabled, polling and webhook modes; default disabled until configured. Validate settings by mode. Polling removes the webhook before starting; webhook mode verifies its secret header and does not run polling concurrently.
- Ignore non-private chats. Link via /start code; derive identity from the Telegram update, never callback payloads. For every text/callback resolve the active linked user before history, model or data access.
- Use durable update_id deduplication from Story 5. Webhook acknowledges after durable acceptance; process through the bounded worker so long model calls do not trigger duplicate processing. Polling uses the same deduplication path.
- Callback payload contains only batch ID and decision within Telegram's size limit. Reauthorize ownership, acknowledge callback promptly, and disable the keyboard after resolution/expiry.
- Send plain text (parse_mode=None), split long replies within Telegram limits, and handle outbound failures without replaying successful domain writes. Text typing indicators are bounded to active work.
- Settings shows current link status, generates a deep link/code with expiry and offers disconnect. Cancel status polling when leaving; preserve password/logout behavior.

**Done when:** linked create/confirmation works end-to-end; unlinked/group messages invoke no domain tools; old callbacks/tokens fail after unlink; duplicate updates cause one turn; wrong webhook secrets are rejected.

## Story 8: Packaging and deployable environment

**Depends on:** 6, 7 for deployment; prepare packaging earlier. **Files:** agent Dockerfile/compose/pipeline/runbook plus API/database compose or pipeline changes required for connectivity/configuration.

- Build a locked non-root image with one worker, installed package assets, readiness and graceful shutdown. Use bthr-agent naming; the existing API service is still finapi, so do not assume it was renamed by the earlier fin_pulse replacement.
- Provide explicit shared network configuration for separate compose projects and test agent -> API -> PostgreSQL and agent -> collector resolution. Document deployment from the repository root with correct compose/env paths; repair affected pipeline working-directory assumptions.
- Apply Flyway V1-V24 before API/agent startup. A migration-owner role creates FKs; the agent runtime role gets only required agent-table/sequence privileges, not public/body/mind/integration reads or writes. Verify inherited/PUBLIC grants with actual permission tests. API role owns integration access.
- Supply production HTTPS routing for `/api/agent/*`, `/telegram/*` and `/api/*`, correct priority and SSE buffering/timeouts. Store sample proxy configuration in the repo and document how to apply externally managed routes.
- CI gates: API tests, web tests/build, Python lint/tests, PostgreSQL concurrency/security tests, OpenAPI drift and container startup. Clean temporary secret files on every outcome; never echo secrets.
- Export metrics/traces/logs with explicit exporters and content redaction. Measure token latency and turn duration; report SLO results rather than asserting unmeasured performance.

**Done when:** clean-checkout container smoke test passes; least-privilege tests pass; rollout order/configuration are reproducible. Actual production release additionally requires Story 9 and operator secrets/routes. No production deployment is implied by completing the plan.

## Story 9: Evaluations and release gate

**Depends on:** 5 for harness, 6/7/8 for final channel/release checks. **Files:** isolated eval fixtures, cases, harness, tests, runbook.

- Generate cases from registry capabilities: 16 x 5 + 3 x 3 + 1 review = **90 supported-operation cases** (get is logical lookup), plus **6 unsupported update/delete rejection cases**. Add at least 50 PT/EN utterances and separate security cases. This replaces the incorrect 96-supported-operations claim.
- Use frozen dates/timezone and per-case domain fixtures. Extend seed tooling with an explicit API base URL or create dedicated fixtures; the web dev server must not be an implicit prerequisite. Confirm IDs, entitlements and expected numeric totals from the fixture API data.
- Use a dedicated disposable database and at least two users for cross-user tests. Reset both domain and agent state between mutation cases through an authorized fixture owner, not the restricted agent role; never truncate a shared environment.
- Evaluate tool/resource/payload accuracy >=90%, 100% update/delete approval safety and zero successful cross-user access. Verify writes with actual API state, not only tool text or event hooks. Unsupported operations must make zero mutation requests.
- Test happy approval, deny/expiry, ambiguity, foreign IDs, injected record text, transport duplicates, unlink revocation, model errors after successful creates and restart recovery.
- Scripted harness tests run offline in CI. Live provider evals run with explicit credentials/cost budget and save model/version/commit/configuration with results. Provider switching changes config only and requires its own gate result.

**Done when:** offline gate tests reject 89% accuracy or any safety failure; live evaluation evidence for the configured release model passes; web/Telegram manual smoke cases and measured SLOs are recorded.

## Execution order and acceptance mapping

| Story | Dependency | Main gate |
| --- | --- | --- |
| 0 | schema decision | API works on Flyway-created PostgreSQL |
| 1 | 0 | transactional linking |
| 2 | 1 | scoped/revocable bot auth; AT-007/008/009/014 |
| 3 | 0 | verified registry/client/SDK contract |
| 4 | 3 | tool capabilities and approval boundary; AT-015/016 |
| 5 | 1, 4 | durable history/confirmations; AT-003/004/005/006/010/013 |
| 6 | 5 | web channel and UI; AT-002/003/010/011 |
| 7 | 2, 5, 6 integration | Telegram and Settings; AT-001/007/008/009 |
| 8 | 6, 7 | reproducible deployment candidate |
| 9 | 5; 6-8 for final release | AT-012 provider switching; all AT-001 through AT-016 plus new failure/race cases |

Recommended implementation sequence: 0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9. No story is complete merely because files exist. Keep completion checkboxes and verification evidence in the implementing change; do not mark unresolved assumptions as approved.

### Original acceptance tests

| ID | Scenario | Stories |
| --- | --- | --- |
| AT-001 | Telegram create | 7, 9 |
| AT-002 | Cross-domain reads | 6, 9 |
| AT-003 | Update confirmation | 4, 5, 6 |
| AT-004 | Delete cancellation | 5, 9 |
| AT-005 | Pending expiry | 5, 6, 7 |
| AT-006 | Foreign pending action | 5, 7 |
| AT-007 | Unlinked chat | 2, 7 |
| AT-008 | Linking and code reuse | 1, 2, 7 |
| AT-009 | Expired code | 1, 7 |
| AT-010 | Shared thread | 5, 6, 7 |
| AT-011 | Agent-to-web data visibility | 6, 7, 9 |
| AT-012 | Provider switch | 3, 9 |
| AT-013 | Rate limit | 5 |
| AT-014 | Old bot endpoint removed | 2 |
| AT-015 | API errors | 3, 4, 9 |
| AT-016 | Ambiguous target | 4, 9 |

## Verification commands and external references

Run commands from their stated directory. Container tests use disposable databases with migrations applied, never the user's development database.

- Repository root: `dotnet test api/api-csharp.sln`.
- `web/`: `npm test` and `npm run build`.
- `agent/` once created: `uv sync --frozen`, `uv run ruff check .`, `uv run pytest`.
- OpenAPI export/check command is established by Story 3's verified cross-platform script.
- Prior session baseline: 401 .NET tests and 5 web tests passed. These were not fresh PostgreSQL compatibility tests; rerun after implementation changes.
- [Pydantic AI deferred tools](https://pydantic.dev/docs/ai/tools-toolsets/deferred-tools/): server-side ownership/argument authorization remains necessary; resume with stored history and deferred results. Verify concrete APIs against the locked release.
- [Pydantic AI installation](https://github.com/pydantic/pydantic-ai/blob/main/docs/install.md): use slim extras for the selected providers; lock the resolved dependency set.

## Revision history

| Date | Revision |
| --- | --- |
| 2026-10-06 | Original nine-story decomposition |
| 2026-10-07 | Rebuilt against bthr working tree: prerequisite schema audit, V23/V24 ordering, append-only capability matrix, durable confirmation/retry rules, Telegram revocation/deduplication, CSRF, deploy networking and isolated eval gates |
