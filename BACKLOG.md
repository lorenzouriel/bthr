# bthr — Backlog

Personal life-tracking platform (finance + body + mind) built on ASP.NET Core 10 / EF Core / PostgreSQL (`api`), a React + Vite SPA (`web`), Flyway-managed schema (`database`), and an OTel/Prometheus/Loki/Tempo/Grafana stack (`monitor`).

This backlog reflects gaps found by reading the actual code, migrations, tests, and configs as of 2026-09-23 — not aspirational scope.

## P0 — Correctness & security

- **Banking integration is documented but not implemented.** `api/README.md` advertises `bank-connections`, `bank-accounts`, and `bank-transactions` endpoints with full CRUD, but there is no `BankConnectionsController`, no corresponding DTOs/models, and no Flyway migration for those tables. Either build the feature or strip it from the README so the public API surface isn't misrepresented.
- **No refresh tokens.** `AuthController` issues a single 7-day JWT in an HTTP-only cookie with no refresh/rotation mechanism (`api/bthr.Api/Controllers/AuthController.cs`). A stolen or expired token means a full re-login; there's no revocation path either (e.g. on logout the cookie is cleared client-side but the JWT itself remains valid until expiry).
- **No rate limiting anywhere in the API.** `Program.cs` has no `AddRateLimiter`/throttling middleware. `/api/auth/login` and `/api/auth/register` are open to brute-force and enumeration.
- **Health check is a stub.** `app.MapGet("/health", () => new { status = "ok" })` never checks the database connection, so the container can report healthy while Postgres is unreachable — a real risk for the Azure Container Apps deployment referenced in the API README.
- **No password reset / email verification flow.** Registration and login exist; there's no way for a user to recover a forgotten password or verify an email address.

## P1 — Product gaps

- **Web app has no automated tests.** `web/package.json` defines only `dev`/`build`/`preview` — no Vitest, Jest, Playwright, or Testing Library. The API has 294 passing tests (`api/bthr.Tests`, one Builder + Service + Controller test file per resource); the frontend has zero, despite driving all the same CRUD flows plus auth.
- **No CI for the web app.** `azure-pipelines.yml` exists for `api` and `database`, and there's no `.github/workflows` anywhere in the repo. `web` has no pipeline at all — a broken build or type error only surfaces locally.
- **No linting/formatting configured in `web`.** No ESLint or Prettier config despite React + TypeScript; `tsconfig.json` is the only guardrail.
- **No root-level orchestration.** Four independently docker-composed services (`api`, `database`, `monitor`, and implicitly `web`) with no top-level `docker-compose.yml` or `Makefile` tying them together for local end-to-end spin-up. New contributors have to read four sets of docs to run the whole stack.
- **No root README or LICENSE.** The repo root has nothing describing what `bthr` is, how the four subprojects relate, or how to run them together — only `api/README.md` and `database/README.md` exist as scoped docs.
- **Dashboards cover 3 of 3 sections but only via generic CRUD forms.** `FinanceDashboard`, `BodyDashboard`, `WellbeingDashboard` and `DashboardBlocks` exist, but there's no cross-section view (e.g. a unified "today" view combining a bill due, a workout logged, and a journal entry) — every resource is siloed behind its own list/form pair (`web/src/config/resources.ts`).
- **No data export or account deletion flow surfaced in the UI.** `UsersController` supports soft-delete server-side, but there's no user-facing settings page to export personal data or delete an account — relevant given the app stores financial, health, and journal data together.

## P2 — Hardening & DX

- **No API-level input rate/size limits or request validation middleware** beyond per-DTO data annotations — worth a pass once the DTO set grows further (17 controllers already).
- **`monitor/` stack has no alerting rules** — Prometheus and Grafana are provisioned (`monitor/config/prometheus.yml`, `monitor/config/grafana/provisioning`) but there's no evidence of alert rules or notification channels, so the observability stack is currently dashboards-only, not alerting.
- **No versioning strategy for the API.** All routes are unversioned `/api/...`; fine today, but the banking-integration gap above suggests the surface will keep growing — worth deciding on `/api/v1` before more consumers depend on it.
- **`web/app/` (Meridian design-tool export) is tracked but undocumented** — `web/app/Meridian.dc.html` and `support.js` look like a design-mockup export sitting next to real app code; worth confirming it's still needed or moving it out of the source tree.

## Notably solid (no action needed)

- API test coverage is consistent and thorough: every resource (finance, body, mind) has a Builder + Service test + Controller test, including the newer body/mind modules (meals, meditation, sleep, workouts, journal entries) added alongside the Postgres migration.
- OTel tracing/metrics/logging wired through Serilog + OpenTelemetry exporters in `Program.cs`, feeding the `monitor` stack (Tempo/Loki/Prometheus/Grafana) — observability plumbing itself is in good shape, just missing alerting (see P2).
- Flyway migrations are cleanly incremental (V1–V23) with a clear schema-per-domain split (`finance`, `body`, `mind`).
