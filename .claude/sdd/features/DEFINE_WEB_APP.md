# DEFINE: Web App (Meridian SPA wired to FinPulse.Api)

> Build a real React + Vite SPA — modeled visually on the `Meridian.dc.html` design reference — that authenticates against and performs full CRUD through the already-live `FinPulse.Api` for all 15 built resources across Finance, Body, and Wellbeing.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP |
| **Date** | 2026-08-26 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

FinPulse has a fully live-verified backend (15 CRUD resources across finance, body, and mind domains) and a visual design reference (`app/Meridian.dc.html`), but no real, running frontend application exists — every screen in the design reference is hardcoded mock data with zero network calls, so no user can actually log in or use the product.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse user | End user of the app | Wants to log in and see/manage their real finance, body, and wellbeing data, but today there is only a design mockup with fake data sitting in front of a fully working backend with no UI |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Scaffold a new `web/` React + Vite app (TypeScript), running on `localhost:5173` — matching the API's existing CORS allowlist exactly, no `Program.cs` changes required |
| **MUST** | Implement real `Login`/`Register` screens calling `POST /api/auth/login` / `POST /api/auth/register`; on success, route into the authenticated app; on failure, show the API's error message |
| **MUST** | Restore session on page load via `GET /api/auth/me` (returns `{id, email, username, plan}`) — if it 401s, redirect to `/login`; if it succeeds, populate the current user across the app |
| **MUST** | Implement `Logout` calling `POST /api/auth/logout`, clearing local session state and redirecting to `/login` |
| **MUST** | Finance section: full CRUD (list/create/edit/delete) UI for all 6 resources — `Goals`, `Bills`, `Budgets`, `Earnings`, `Expenses`, `Investments` — each hitting its real `/api/users/{userId}/{resource}` route |
| **MUST** | Body section: full CRUD UI for all 7 resources — `WeeklyRoutines`, `Workouts`, `PersonalRecords` (create+read only, matching the API's own PUT/DELETE omission), `Meals`, `WaterIntake`, `BodyMetrics`, `SleepLogs` (`TotalHours` displayed read-only, never submitted in a form) |
| **MUST** | Wellbeing section: full CRUD UI for both `mind` schema resources — `MeditationSessions`, `JournalEntries` — including nullable mood fields rendered as optional, not required, form inputs |
| **MUST** | Every list screen calls its resource's `GET` endpoint live; every create/edit form calls the matching `POST`/`PUT` live; every delete action calls `DELETE` live and removes the row from the visible list afterward (soft-delete, matching the API) |
| **MUST** | Visual layout, color themes (Porcelain/Ink/Dusk), typography (Newsreader/Instrument Sans), and component patterns (stat cards, bar charts, list rows) are ported from `Meridian.dc.html`, not redesigned from scratch |
| **MUST** | Every wired screen is live-verified end-to-end in a real browser against the real running API + Postgres — not just "compiles" or unit-tested in isolation — matching this initiative's established verification discipline |
| **SHOULD** | Client-side form validation mirrors each DTO's `DataAnnotations` constraints (`[Required]`, `[MaxLength]`, ranges) so obviously-invalid submissions are caught before hitting the network |
| **SHOULD** | A shared API client module centralizes `fetch` config (base URL, `credentials: 'include'` for the cookie, JSON parsing, 401-redirect-to-login handling) rather than each screen reimplementing it |
| **COULD** | Loading and empty states per list screen (skeleton/spinner while fetching, a friendly message when a list is empty) |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] `web/` app starts with `npm run dev` and serves on `http://localhost:5173`
- [ ] A real browser session can register a new user, log in, and land on an authenticated dashboard — verified live, not mocked
- [ ] Refreshing the browser mid-session keeps the user logged in (via `GET /api/auth/me` + the `access_token` cookie), not bounced to `/login`
- [ ] All 15 resources across Finance/Body/Wellbeing support a full live create → list → update → delete cycle through the UI, each verified against the real running Postgres database (row appears, updates, and soft-deletes as expected)
- [ ] `PersonalRecords` UI has no edit/delete controls (matches the API having no `PUT`/`DELETE` routes for this resource)
- [ ] `SleepLogs`' `TotalHours` and journal/meditation mood fields render correctly as computed/optional, never as required form inputs
- [ ] Logging out clears the session and blocks access to authenticated routes until logging back in
- [ ] `npm run build` succeeds with 0 TypeScript errors

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Register + auto-login | A fresh browser, no session | User fills the Register form and submits | `POST /api/auth/register` succeeds (201), the `access_token` cookie is set, and the user lands on the authenticated dashboard without a separate login step |
| AT-002 | Login with valid credentials | A registered user, logged out | User submits the Login form with correct credentials | `POST /api/auth/login` succeeds (200), cookie is set, user reaches the dashboard |
| AT-003 | Login with invalid credentials | A registered user, logged out | User submits the Login form with a wrong password | `POST /api/auth/login` returns 401; the UI shows an error message and does not navigate away from `/login` |
| AT-004 | Session restore on refresh | A logged-in user mid-session | Browser page is refreshed (F5) | `GET /api/auth/me` succeeds, user remains on the authenticated app, no redirect to `/login` |
| AT-005 | Session lost redirects to login | A logged-in user | The `access_token` cookie is cleared/expired and any authenticated API call is made | The call returns 401; the UI redirects to `/login` |
| AT-006 | Full CRUD lifecycle — Finance resource | An authenticated user with no existing `Bills` | User creates a Bill via the UI form, sees it in the list, edits its amount, sees the update reflected, deletes it | Each step succeeds live against the real API; the deleted Bill no longer appears in the list but still exists in Postgres with `status = 0` |
| AT-007 | Full CRUD lifecycle — Body resource | An authenticated user with no existing `Meals` | User creates a Meal, sees it listed, edits it, deletes it | Same live-verified cycle as AT-006, against `body.meals` |
| AT-008 | Full CRUD lifecycle — Wellbeing resource with nullable mood | An authenticated user | User creates a Journal Entry leaving the mood field blank | The entry is created successfully with `mood: null`, and the UI does not block submission for the missing optional field |
| AT-009 | PersonalRecords has no edit/delete UI | An authenticated user viewing their Personal Records list | User views a Personal Record row | No edit or delete button/action is rendered for that row, matching the API's `GET`/`POST`-only routes |
| AT-010 | Visual fidelity spot-check | The design reference `Meridian.dc.html` open side-by-side | The Home/Finance/Body/Wellbeing screens are compared | Colors, fonts, spacing, and card/list layout patterns visibly match the reference (not a pixel-perfect requirement, but a clear, recognizable port) |
| AT-011 | Logout clears session | A logged-in user | User clicks Logout | `POST /api/auth/logout` is called, the cookie is cleared, and the user is redirected to `/login`; navigating back to a dashboard route without logging in again redirects to `/login` |

---

## Out of Scope

Explicitly NOT included in this feature:

- **AI coach chat panel** — no LLM/chat backend exists anywhere in this repo.
- **Inbox screen (AI classification)** — no classification backend or inbox-items schema.
- **Timeline screen (unified chronological feed)** — no unified events API.
- **Focus, Projects, Knowledge, Learning modules** — zero backend support for any of these.
- **Garmin / Strava / Apple Health / Open Finance integrations** — no OAuth/webhook infrastructure exists.
- **Pillar-level aggregate scores** (e.g., "Body 84%") — both `BODY_MODULE_API` and `MIND_MODULE` explicitly deferred aggregation endpoints.
- **"Sleep & Rest" dream notes** — distinct from `body.sleep_logs` (in scope); no backend field exists for dream notes.
- **Production deployment, hosting, or CI/CD for the new frontend** — this feature is local dev only (`npm run dev`); deployment is a separate future feature.
- **Backend/API changes of any kind** — this feature consumes `FinPulse.Api` exactly as it exists today; any bug found is logged, not silently worked around by changing backend code as part of this feature.
- **Automated frontend test suite** (Jest/Vitest/Playwright) — this pass relies on live manual browser verification, matching every prior feature's discipline; automated frontend tests are a candidate follow-up, not required here.
- **Editing `Meridian.dc.html` itself** — it remains the untouched design reference; the SPA is a new, separate codebase.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Must consume `FinPulse.Api` exactly as built (no backend changes) | Design must work within the API's existing routes, DTOs, and auth model as-is |
| Technical | Auth is cookie-only — the JWT middleware reads the token exclusively from the `access_token` cookie (`OnMessageReceived` → `context.Request.Cookies["access_token"]`), never an `Authorization` header | Every API call from the SPA must use `fetch(..., { credentials: 'include' })`; there is no header-based fallback |
| Technical | **`Cookie:Secure` defaults to `true`** (no override in `appsettings.json`, no `appsettings.Development.json`, no HTTPS `launchSettings.json` profile) — a `Secure` cookie will not be set/sent by a browser over the plain `http://localhost:5026`/`:5080` the API currently runs on in dev | This is a real, load-bearing gap: unresolved, login would appear to succeed (200) while the browser silently drops the cookie, breaking every subsequent authenticated call. Design MUST resolve this (most likely: add `appsettings.Development.json` with `"Cookie": {"Secure": false}`) before any other frontend work can be verified live |
| Technical | Visual fidelity to `Meridian.dc.html` (colors, fonts, layout patterns), not a redesign | Design must port the reference's inline style values, not invent a new visual language |
| Scope | Only Finance, Body, and Wellbeing sections get real screens this pass | Design must not build UI shells for Focus/Projects/Knowledge/Learning/Inbox/Timeline |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | New `web/` directory at repo root, sibling to `api/`, `database/`, `app/`, `monitor/` | `app/Meridian.dc.html` stays untouched as the design reference |
| **KB Domains** | None — the KB is data-engineering-focused; no React/Vite/frontend domain exists in `.claude/kb/` | Confidence 0.75 — genuinely novel for this repo, no existing frontend code to pattern-match; DESIGN should ground code patterns in `Meridian.dc.html`'s own conventions and standard React/TanStack Query/React Router documentation instead |
| **IaC Impact** | None | Local dev only (`npm run dev`), no containerization or deployment config in scope |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is a frontend application feature consuming an existing REST API, not a data pipeline.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | The `Cookie:Secure=true` default + no HTTPS dev profile will actually block the auth cookie from persisting in a real browser during local dev, unless Design adds a `Cookie:Secure=false` override for Development | If somehow browsers tolerate it (they won't, per spec — `Secure` cookies require HTTPS), this constraint is moot; if confirmed (expected), Design must add the config fix as its first, blocking step | [ ] — flagged for Design to live-verify first, before any other frontend work |
| A-002 | `GET /api/auth/me`'s response shape (`{id, email, username, plan}`) is stable and sufficient for the SPA's "current user" needs (no additional profile fields needed for this pass) | Design would need to call an additional endpoint (e.g., `GET /api/users/{id}`) to enrich the profile | [x] Confirmed by reading `AuthController.Me()` directly during this Define session |
| A-003 | React + Vite + TanStack Query + React Router (chosen in Brainstorm) have no version-compatibility issues with each other or with a fresh scaffold — no existing `package.json` precedent in this repo to validate against | If a compatibility issue surfaces, Design/Build would need to pin specific versions or adjust the stack slightly | [ ] |
| A-004 | The API's CORS policy (`WithOrigins("http://localhost:5173", "http://localhost:3000")`, dev-only per `if (app.Environment.IsDevelopment())`) is sufficient — no additional CORS configuration is needed for a Vite dev server on the default port | If Vite is configured to run on a non-default port, CORS would reject the SPA's requests | [x] Confirmed by reading `Program.cs` directly — Vite's default port (5173) is already in the allowlist |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific and verifiable — confirmed via direct code inspection that zero networking exists in the design reference and zero frontend code exists in the repo |
| Users | 2 | One clear persona with a concrete pain point, but a single generic user type rather than multiple distinct personas |
| Goals | 3 | MoSCoW-prioritized, each traceable to one of 6 validated brainstorm discovery answers plus 2 validation checkpoints |
| Success | 3 | Every criterion is testable pass/fail (build succeeds, live browser session survives refresh, full CRUD lifecycle works live per section, visual fidelity spot-check, PersonalRecords has no edit UI) |
| Scope | 3 | Ten explicit out-of-scope items, each traced back to a brainstorm YAGNI decision |
| **Total** | **14/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design. (Constraint on `Cookie:Secure` and Assumption A-001 are flagged as the first thing Design must resolve and live-verify — the entire feature is blocked on the auth cookie actually persisting in a real browser.)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-26 | define-agent | Initial version, derived from `BRAINSTORM_WEB_APP.md`. Discovered and documented the `Cookie:Secure` dev-environment risk by reading `AuthController.cs`, `Program.cs`, and `launchSettings.json` directly during this session — not present in the brainstorm. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_WEB_APP.md`
