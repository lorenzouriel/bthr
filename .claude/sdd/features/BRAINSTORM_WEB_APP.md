# BRAINSTORM: Web App (Meridian SPA wired to FinPulse.Api)

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP |
| **Date** | 2026-08-26 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "I sent an app example here - C:\Users\Uriel\workspace\opensource\bthr\app\Meridian.dc.html — Let's start connecting the API, Database and add life to the app..."

**Context Gathered:**
- `app/Meridian.dc.html` (747 lines) + `app/support.js` (60KB) + `app/.thumbnail` is a **Claude Design canvas artifact** (`.dc.html` format) — a self-contained design/prototype runtime (`x-dc` custom element, `sc-for`/`sc-if` directives, a `DCLogic`-based `Component` class), not a deployable production app. Confirmed by grepping `support.js`: zero `fetch()` calls tied to real endpoints — all networking is internal to the design-canvas runtime itself (loading Babel/React from CDN, sibling-component fetching). All app data (`pillars()`, `coachFor()`, scores, workouts, meals, etc.) is hardcoded inline in the `Component` class.
- The mock models 3 "pillars" — **Body** (Training, Nutrition, Sleep), **Mind** (Focus, Projects, Knowledge, Learning, Finance), **Spirit** (Meditation, Journal, Sleep & Rest) — plus Home, Inbox, and Timeline top-level screens and an "AI coach" chat panel, all fully mocked.
- **Critical naming mismatch discovered**: the mock's "Mind" pillar (Focus/Projects/Knowledge/Learning/Finance) does **not** correspond to the backend's `mind` Postgres schema (Meditation/Journal, built in the prior `MIND_MODULE` feature). The backend's `mind` schema actually corresponds to the mock's **"Spirit"** pillar. This was resolved during discovery (see Q2 below).
- `api/FinPulse.Api/Program.cs` already has CORS configured for `http://localhost:5173` (Vite's default dev port) and `http://localhost:3000`, with `.AllowCredentials()` for cookie-based JWT — strong pre-existing evidence that a real SPA, not the design canvas file itself, was always the intended consumer of this API.
- No `web/`, `client/`, or `frontend/` folder exists yet anywhere in the repo; no `package.json` for a JS frontend framework exists. This is a greenfield frontend build.
- Backend inventory of what actually has a live, tested API today: **Finance** (`Goals`, `Bills`, `Budgets`, `Earnings`, `Expenses`, `Investments` — 6 resources), **Body** (`WeeklyRoutines`, `Workouts`, `PersonalRecords`, `Meals`, `WaterIntake`, `BodyMetrics`, `SleepLogs` — 7 resources), **Mind** (`MeditationSessions`, `JournalEntries` — 2 resources, just shipped). Auth is `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`, cookie-based JWT.
- Nothing exists in the backend for: Focus (tasks/Pomodoro/habits), Projects (kanban/milestones), Knowledge (notes/graph), Learning (courses/flashcards), Inbox (AI classification), Timeline (unified event feed), the AI coach chat, Garmin/Strava/Open-Finance integrations, or any pillar-level aggregate score (Body 84%/Mind 76%/Spirit 91% in the mock) — aggregation endpoints were explicitly deferred as out-of-scope in both the `BODY_MODULE_API` and `MIND_MODULE` DEFINE documents.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | New `web/` directory at repo root (sibling to `api/`, `database/`, `app/`, `monitor/`) | Standard monorepo placement, matches CORS port 5173 expectation |
| Relevant KB Domains | None — the KB is data-engineering-focused; no React/Vite/frontend domain exists | Confidence 0.75 — genuinely novel for this repo (no existing frontend code to pattern-match against, unlike the ASP.NET API work which had 6 prior resources as precedent) |
| IaC Patterns | N/A — local dev via `npm run dev` / Vite dev server; no containerization or deployment config requested yet | Design phase should scope only local dev, not production hosting |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Patch fetch() calls directly into Meridian.dc.html, or build a real Vite SPA using it as a visual reference? | Build a real Vite SPA | `app/Meridian.dc.html` stays as a design reference only; a new `web/` app is scaffolded and becomes the real, shipped frontend |
| 2 | How to resolve the mock's Mind/Spirit naming vs. the backend's actual `finance`/`body`/`mind` schemas? | Only wire up what's built — Finance, Body (Training/Nutrition/Sleep), Wellbeing (Meditation/Journal) | Defines the exact 3-section nav for this pass; resolves the naming collision by using "Wellbeing" instead of either mock label for the Meditation/Journal section |
| 3 | Meridian has no login screen (hardcodes "Daniel Costa"); how should auth work? | Build real login/register screens | Adds a `Login`/`Register` flow calling the real `/api/auth/*` endpoints before any dashboard route is reachable |
| 4 | Read-only display first, or full CRUD (matching what the API already supports)? | Full CRUD from the start | Every wired resource screen gets list + create + edit + delete UI, not just data display |
| 5 | Any additional design/data samples beyond Meridian.dc.html itself? | None — Meridian.dc.html is the only reference | Confirms the visual spec source; API response shapes come from the already-live-verified Body/Mind build reports |
| 6 | Tech stack for the new SPA? | React + Vite + TanStack Query + React Router | Query/cache/invalidation handled by a library across ~15 CRUD resources instead of hand-rolled per resource; routing matches Meridian's existing pillar/module page-switching model |

**Minimum Questions:** 3 (6 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Design reference | `app/Meridian.dc.html` | 1 file, 747 lines | Visual/UX spec: colors (Porcelain/Ink/Dusk themes), fonts (Newsreader/Instrument Sans), layout patterns (stat cards, bar charts, list rows, kanban), navigation model (sidebar → pillar → module) |
| API response shapes | `.claude/sdd/reports/BUILD_REPORT_BODY_MODULE_API.md`, `.claude/sdd/reports/BUILD_REPORT_MIND_MODULE.md` | 2 reports, live curl transcripts | Ground-truth JSON shapes for every Body/Mind resource, already live-verified against the real running API |
| Related code | `api/FinPulse.Api/DTOs/*.cs` (15 resources across Finance/Body/Mind) | 15 files | Exact field names/types/nullability for every request/response DTO the SPA will call |

**How samples will be used:**

- Meridian's exact inline styles (colors, spacing, border-radius, font stacks) are ported into React components — visual fidelity to the mock, not a redesign.
- DTO field names drive the SPA's TypeScript types and form fields directly — no guessing at shapes.
- The build reports' live curl transcripts serve as the manual test script for verifying each wired screen against the real API during Build.

---

## Approaches Explored

### Frontend delivery approach

#### Approach A: Build a real Vite SPA ⭐ Recommended

**Description:** Scaffold a new `web/` app (framework TBD in the tech-stack decision below) that reimplements Meridian's screens for real, fetching from `FinPulse.Api`. `Meridian.dc.html` remains the design reference, untouched.

**Pros:**
- Matches the pre-existing CORS configuration in `Program.cs` (ports 5173/3000, `AllowCredentials`) — this was clearly anticipated already.
- Design-canvas files are explicitly a prototyping/mockup format (per the `design` skill's own description: "DRAFT the design... Save publishes a new version" — a Claude Design artifact, not a deployable app).
- Clean separation: design reference vs. shipped code, matching how every other part of this repo separates spec (`.claude/sdd/`) from implementation.

**Cons:**
- More upfront scaffolding (new project, build tooling, dependencies) than editing one file.

**Why Recommended:** The CORS evidence alone (a port and credentials setup that only makes sense for a real SPA) shows this was the intended path. Confidence 0.90 — direct codebase evidence, not just design-tool convention.

---

#### Approach B: Patch fetch() calls directly into Meridian.dc.html

**Description:** Replace the hardcoded `pillars()`/`coachFor()` methods in the `Component` class with real `fetch()` calls into `FinPulse.Api`, keep shipping the single HTML file.

**Pros:**
- Zero new scaffolding — one file, no build step.

**Cons:**
- Fights the tool's purpose: `.dc.html` files are Claude Design's editable-canvas format (click-to-select, properties panel, Save-publishes-new-version) — hardcoding live network calls into it means every future design-canvas edit risks clobbering integration logic, and vice versa.
- No routing, no code-splitting, no real dependency management, no test tooling — everything the rest of this repo already has (`FinPulse.Api`, `FinPulse.Tests`) would be entirely absent on the frontend.
- Doesn't explain the pre-existing CORS setup for ports 5173/3000, which this approach wouldn't need (a design-canvas file isn't served from a Vite/CRA dev server).

**Why not recommended:** Directly contradicts existing evidence (CORS config) and the design-canvas tool's own purpose.

---

### Tech stack approach (within Approach A)

#### Approach A1: React + Vite + TanStack Query + React Router ⭐ Recommended

**Description:** Standard modern SPA stack. TanStack Query owns loading/error/cache/invalidation state for all ~15 CRUD resources; React Router owns the pillar → module nested routing Meridian already models via its `sc-if` page-switching.

**Pros:**
- No hand-rolled loading/error/refetch-after-mutation logic needed 15 times over.
- React Router's nested routes map naturally onto Meridian's existing `/pillar/module` navigation structure (already expressed as `page = 'body.training'`-style state in the mock's `go()` method).

**Cons:**
- More dependencies than a no-framework approach.

**Why Recommended:** Full CRUD across 15 resources (confirmed in scope, Q4) is exactly the case TanStack Query is built for — DEFINE/DESIGN would otherwise need to specify identical loading/error/cache-invalidation logic 15 separate times.

---

#### Approach A2: React + Vite, plain fetch/useState/useEffect

**Description:** Same React + Vite base, no query library — hand-write `fetch` + state for every screen.

**Pros:** Fewer dependencies.

**Cons:** 15 resources × (loading state + error state + refetch-after-mutation) hand-written is a lot of repeated, easy-to-get-subtly-wrong boilerplate.

**Why not recommended:** The repeated-boilerplate cost outweighs the dependency savings at this resource count.

---

#### Approach A3: Vanilla JS + Vite, no framework

**Description:** Closest in spirit to Meridian's own hand-rolled component style (`sc-for`/`sc-if` already mimic components without a framework).

**Pros:** Minimal dependencies, philosophically consistent with the mock's own approach.

**Cons:** No real component reuse across 15 near-identical CRUD screens (list + form + edit + delete) — would mean re-deriving what React already provides.

**Why not recommended:** Loses component reuse exactly where it matters most (15 structurally similar resource screens).

---

## Data Engineering Context (if applicable)

Not applicable — this is a frontend application feature consuming an existing REST API, not a data pipeline.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A (real Vite SPA) + Approach A1 (React + Vite + TanStack Query + React Router) |
| **User Confirmation** | 2026-08-26 |
| **Reasoning** | Matches pre-existing CORS evidence; respects the design-canvas tool's actual purpose; TanStack Query avoids 15x repeated boilerplate for the confirmed full-CRUD scope |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Build a new `web/` React+Vite SPA; `Meridian.dc.html` stays as design reference only | Pre-existing CORS config (5173/3000, `AllowCredentials`) already anticipated this; design-canvas files aren't meant to carry live integration logic | Patching `fetch()` calls directly into `Meridian.dc.html` |
| 2 | Nav scoped to 3 real sections this pass: Finance, Body (Training/Nutrition/Sleep), Wellbeing (Meditation/Journal) | Only wire up what has a live backend today; resolves the mock's Mind/Spirit naming collision by not reusing either ambiguous label | Matching the mock's exact 3-pillar (Body/Mind/Spirit) structure, which would mix real and permanently-mocked screens under the same nav labels |
| 3 | Real login/register screens calling `/api/auth/*`, not a hardcoded dev user | Matches this session's established discipline of live-verifying real auth flows, not bypassing them | Skipping auth, hardcoding a fixed `userId` |
| 4 | Full CRUD UI (list/create/edit/delete) for every wired resource | The API already supports full CRUD on all 15 resources in scope — no reason to ship a read-only shell first | Read-only display first, mutations deferred |
| 5 | React + Vite + TanStack Query + React Router | Full-CRUD-across-15-resources is exactly TanStack Query's designed use case; avoids repeated hand-rolled state logic | Plain fetch/useState (A2); vanilla JS no-framework (A3) |
| 6 | Keep Meridian's theme-switching (Porcelain/Ink/Dusk) mechanism | Nearly free — Meridian's `themeVars()` CSS-variable logic ports directly, no backend dependency | Dropping it as out-of-scope (considered, but cost is too low to justify cutting) |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Home's "AI coach" chat panel | No LLM/chat backend exists anywhere in this repo | Yes |
| Inbox screen (AI classification of incoming items) | No classification backend, no "inbox items" schema | Yes |
| Timeline screen (unified chronological event feed) | No unified events API; would require aggregating across all resources, which is a distinct feature | Yes |
| Focus module (tasks, Pomodoro, time blocking, habits) | Zero backend — no tasks/habits schema anywhere | Yes |
| Projects module (kanban, milestones, notes) | Zero backend — no projects/kanban schema anywhere | Yes |
| Knowledge module (notes, graph view, backlinks) | Zero backend — no notes schema anywhere | Yes |
| Learning module (courses, flashcards, books) | Zero backend — no learning schema anywhere | Yes |
| Garmin / Strava / Apple Health / Open Finance integrations | No OAuth/webhook infrastructure exists for any external integration | Yes |
| Pillar-level aggregate scores (e.g., "Body 84%") | Both `BODY_MODULE_API` and `MIND_MODULE` DEFINE documents explicitly deferred aggregation/computed endpoints as out-of-scope | Yes — needs a dedicated aggregation-endpoint feature first |
| Sleep & Rest's "dream notes" sub-feature | Distinct from `body.sleep_logs` (which IS in scope under Body/Sleep); dream notes have no backend field anywhere | Yes |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Overall shape (stack, nav sections, auth, CRUD scope, visual fidelity) | ✅ | "Looks good (Recommended)" | No — confirmed as drafted |
| YAGNI scope (10 exclusions + theme-switching keep) | ✅ | "Yes, looks right (Recommended)" | No — confirmed as drafted |

**Minimum Validations:** 2 (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)

FinPulse has a fully live-verified backend (finance, body, and mind domains — 15 CRUD resources total) and a visual design reference (`Meridian.dc.html`), but no real, deployable frontend application exists to connect the two — every screen in the design reference is hardcoded mock data with zero network calls.

### Target Users (Draft)

| User | Pain Point |
|------|------------|
| FinPulse user | Wants to actually use the app — log in, see real finance/body/wellbeing data, create and edit records — but today there is only a design mockup with fake data and a backend with no UI in front of it |

### Success Criteria (Draft)

- [ ] A new `web/` React + Vite SPA exists, runs on `localhost:5173`, and successfully authenticates against `/api/auth/*` via cookie-based JWT
- [ ] Finance, Body, and Wellbeing sections each display real data fetched live from `FinPulse.Api`, replacing every hardcoded array from `Meridian.dc.html`'s `pillars()` method for those sections
- [ ] Full CRUD (create/read/update/delete) works live, end-to-end, through the UI for all 15 in-scope resources
- [ ] Visual fidelity to `Meridian.dc.html`'s layout, colors, and typography is preserved (not a redesign)
- [ ] Every CRUD action live-verified against the real running API + Postgres (not mocked), matching this initiative's established verification discipline

### Constraints Identified

- Must consume the existing `FinPulse.Api` exactly as built — no backend changes as part of this feature (unless a genuine bug is discovered)
- Must use cookie-based JWT auth exactly as `AuthController` implements it today
- Must not implement Focus/Projects/Knowledge/Learning/Inbox/Timeline/AI-coach/aggregate-scores — no backend exists for any of them

### Out of Scope (Confirmed)

- AI coach chat panel, Inbox, Timeline
- Focus, Projects, Knowledge, Learning modules
- Garmin/Strava/Apple Health/Open Finance integrations
- Pillar-level aggregate scores
- Production deployment/hosting/CI for the new frontend (local dev only, this pass)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 6 |
| Approaches Explored | 5 (2 delivery-approach options + 3 tech-stack options) |
| Features Removed (YAGNI) | 10 |
| Validations Completed | 2 |
| Duration | ~20 min |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_WEB_APP.md`
