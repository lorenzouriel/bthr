# BRAINSTORM: Section Dashboards

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | SECTION_DASHBOARDS |
| **Date** | 2026-08-29 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "Liked but it's still different from the UI, the UI has dashboards and a lot of stuff. Describe the project and the UI in a prompt" — feedback that even after `WEB_APP_UI` (which ported Meridian's list-row/slide-in-panel visual language into the CRUD screens), the app is still structurally flat: every screen is "a list of raw rows for one resource." Meridian's actual UI is dashboard-driven — score cards, charts, progress bars, metric grids — none of which exist yet.

**Context Gathered:**
- Re-read `app/Meridian.dc.html` end to end (all 748 lines, both the JSX-like template and the `Component` class's mock data / render logic) to catalog every screen type and content-block type it defines.
- Reviewed the current SPA (`web/src/config/resources.ts`, `AppLayout.tsx`, `App.tsx`) — confirmed there is no dashboard/landing page today; `/` redirects straight to `/finance/goals`, and every route beyond auth is a single resource's list+form.
- Inspected the live Postgres schema for all 15 resources (column names, types, check constraints) to ground any proposed metric in fields that actually exist.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `web/src/pages/`, `web/src/components/` | New dashboard pages + shared presentational block components, same conventions as `WEB_APP`/`WEB_APP_UI` |
| Relevant KB Domains | None (no project KB configured for this repo) | Rely on Meridian's own patterns + codebase conventions, as prior features did |
| IaC Patterns | N/A — frontend-only feature, no infrastructure change | Confirmed by the aggregation-location decision below |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Meridian's 3 pillars (Body/Mind/Spirit) don't match FinPulse's 3 sections (Finance/Body/Wellbeing) — should each FinPulse section become a "pillar dashboard" with resources as "modules"? | "Not the pillars, more the UI itself, the panels, graphs" | Drop the pillar/module *conceptual* reframing — port Meridian's visual *components* (stat cards, bar charts, progress bars, metric grids) onto FinPulse's existing three sections as-is, not its Body/Mind/Spirit taxonomy |
| 2 | Meridian's scores, AI insights, and coach chat are all hardcoded/fake — how should this build handle them? | "Skip AI coach and focus only on the graphs" | No AI Coach panel, no insight quotes, no percentage "scores." Only real, computed metrics and charts from actual data |
| 3 | Which Meridian screens should this build target? | "Section dashboards" (of Home / Section dashboards / resource-level blocks / Timeline) | Scope is exactly one new screen type: a landing dashboard per section (Finance, Body, Wellbeing). Home page, per-resource embedded charts, and Timeline are explicitly out of scope for this round |
| 4 | Dashboards need aggregated numbers (totals, sums, streaks) that don't exist as single API fields — where should that computation happen? | "Client-side, from existing endpoints" | No backend changes. SPA fetches the same list endpoints it already calls and aggregates with TanStack Query, consistent with `WEB_APP`/`WEB_APP_UI`'s "no backend changes" constraint |

**Minimum Questions:** 3 ✅ (4 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Ground truth | Live Postgres (`fin_pulse` DB, user `uiverify`, id=2) | 15 resources, 3-5 rows each | Seeded this session specifically so every dashboard metric has real data to compute against and display |
| Related code | `app/Meridian.dc.html` lines 158-294 (pillar dashboard + module blocks), 432-452 (`buildBlocks` renderer) | 1 file | Source of every visual pattern being ported: stat card, 7-column bar chart, progress bar row, habit-dot grid |
| Related code | `web/src/config/resources.ts`, `web/src/components/ResourceList.tsx` | 2 files | Existing field names/types per resource; row-list visual pattern already ported and to be reused for a "recent transactions" list |

**How samples will be used:**

- The live seeded rows are what the dashboards will actually render against during build verification (same live-curl-based verification method used for every prior feature — no browser automation available in this environment).
- Meridian's block markup/styles (padding, border-radius, colors via CSS vars) are copied directly into the Design phase's code patterns, not reinvented.

---

## Approaches Explored

### Approach A: Three dedicated dashboard components ⭐ Recommended

**Description:** One purpose-built component per section (`FinanceDashboard`, `BodyDashboard`, `WellbeingDashboard`), each fetching only the resources it needs and computing its own specific metrics (spent-this-month, latest-weight, meditation-streak, etc.). A thin `SectionDashboardPage` reads the `:section` route param and renders the matching component. Shared presentational pieces (`StatCard`, `BarChart`, `ProgressBars`, `DotGrid`) extracted once and reused across all three.

**Pros:**
- Each section's metrics are genuinely different formulas over different resources — no artificial abstraction forcing them into one shape
- Shared visual building blocks (4 small components) still eliminate the real duplication (Meridian's exact stat-card/bar-chart/dot-grid markup)
- Easy to verify and reason about — each dashboard is independently readable

**Cons:**
- 3 new page-level components instead of 1 generic one (more files)
- Adding a 4th section later means writing a 4th dedicated component, not just a config entry

**Why Recommended:** The resource-config-driven pattern works great for the CRUD screens (15 resources, identical shape: list of rows + form). It does NOT fit here — there are exactly 3 dashboards, each with hand-picked, domain-specific metrics that don't reduce to a shared schema. Forcing a "dashboard config" abstraction over 3 non-uniform screens would be exactly the kind of premature abstraction this project's conventions warn against.

---

### Approach B: Single generic `SectionDashboard` driven by a metrics config

**Description:** Extend `resources.ts` (or a parallel config) with a declarative list of "widgets" per section — e.g. `{ type: 'stat', resource: 'expenses', filter: 'thisMonth', agg: 'sum', field: 'amount' }` — and one generic renderer interprets the config at runtime.

**Pros:**
- Adding/editing a metric later is a config change, no new code
- Consistent with the CRUD screens' config-driven philosophy

**Cons:**
- The metrics aren't uniform: goal progress bars need `current/target`, mood charts need "average per day across possibly-multiple entries," meditation streak needs a "consecutive days" algorithm — a generic `{agg: sum/avg/count}` vocabulary can't express a streak or a per-day-bucketed chart without becoming its own mini query language
- Over-engineered for exactly 3 fixed dashboards; the config DSL would end up more complex than the 3 plain components it replaces

**Why Not Recommended:** This is the trap the `WEB_APP_UI` DESIGN doc explicitly reasoned through for the CRUD side (config-driven made sense there because all 15 resources share one shape). Dashboards don't share a shape — Approach B pays a real complexity cost to simulate uniformity that isn't there.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — three dedicated dashboard components + shared visual blocks |
| **User Confirmation** | 2026-08-29, "Looks good, build" (confirming the proposed per-section metric/chart set, which presupposes Approach A's shape) |
| **Reasoning** | Matches the real shape of the problem (3 non-uniform dashboards) without inventing an abstraction the data doesn't support |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Port Meridian's visual components (stat card, bar chart, progress bars, dot grid), not its Body/Mind/Spirit pillar taxonomy | FinPulse's real domain is Finance/Body/Wellbeing; Meridian's pillar structure is a fictional demo and doesn't map 1:1 (its own "Finance" is a module nested under "Mind") | Reshaping FinPulse's navigation/domain model to match Meridian's fictional pillars |
| 2 | No AI Coach panel, no insight quotes, no percentage "scores" | Meridian's AI content is 100% hardcoded fake data; FinPulse has no scoring algorithm or AI backend, and building one is far outside this feature's scope | Static/placeholder AI text purely for visual completeness (considered, explicitly rejected by user) |
| 3 | Client-side aggregation only, via existing list endpoints | Zero backend changes, consistent with `WEB_APP`/`WEB_APP_UI`'s established constraint; data volumes here (single-digit-to-low-hundreds rows per user) don't justify new summary endpoints | New `GET /summary` backend endpoints per section |
| 4 | Finance dashboard has no "budget usage" bar (present in Meridian's own Finance-module example) | `budgets` has no FK or shared category convention linking it to `expenses` in the live schema — computing this would mean guessing a category-string match, i.e. fabricating a relationship that doesn't exist in the data | Loosely matching `expenses.category` to `budgets.name` as a heuristic |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Global Home landing page (greeting, cross-section activity feed, upcoming list) | User explicitly scoped this round to section dashboards only | Yes — natural next iteration once section dashboards exist to aggregate |
| Per-resource embedded charts (e.g. a bar chart inside the Meals list page itself) | Out of scope per user's answer to the Scope question; the list+form CRUD screens stay as they are | Yes |
| Timeline (unified chronological feed across all 15 resources) | Explicitly deferred; no equivalent exists in the API today and it's the most build-heavy item of the four scope options | Yes |
| AI Coach chat panel | No AI backend exists; user explicitly said to skip it | Yes, if/when an AI backend is built — separate initiative |
| Percentage "scores" per section (Meridian's 84%/76%/91% pillar scores) | No scoring algorithm exists or was requested; would be fabricated content | Only if a real scoring formula is defined first |
| Budget-usage progress bar on the Finance dashboard | No data relationship between `budgets` and `expenses` in the live schema | Yes, if a `budgetId` FK is added to `expenses` in a future backend change |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Project + target-UI description (Meridian's 5 screen types, the pillar-taxonomy mismatch) | ✅ | Confirmed the framing; clarified they want the panels/graphs, not the pillar restructuring | Yes — scoped discovery questions accordingly |
| Per-section metric/chart proposal (concrete stat cards, charts, bars, list per section, grounded in real schema columns) | ✅ | "Looks good, build" | No — approved as proposed |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
Every screen in the FinPulse SPA today is a flat list of raw rows for one resource; there is no view that summarizes a section's data the way Meridian's mockup demonstrates (stat cards, trend charts, progress bars), so users can't see totals, trends, or progress at a glance.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| FinPulse end user (any logged-in account) | Has to open each of the 15 resource lists individually and mentally total/compare rows to understand "how am I doing this month/week" |

### Success Criteria (Draft)
- [ ] Each of the 3 sections (Finance, Body, Wellbeing) has a dashboard page reachable from the sidebar, showing only real, computed data (no fabricated content)
- [ ] All computation happens client-side against existing endpoints — zero new backend endpoints, zero backend file changes
- [ ] Dashboards render correctly with the live seeded data (3-5 rows per resource) with no runtime errors, including on empty/partial data (e.g. a section with 0 rows in one resource)

### Constraints Identified
- No backend changes (endpoints, schema, or DTOs) — everything computed in the SPA from data already returned by existing `GET` list endpoints
- No new npm dependencies — charts/bars/grids are hand-built with CSS (Div/flex), following Meridian's own approach (no charting library in the mockup either)
- Must visually match Meridian's block styling (padding, border-radius, CSS custom properties already defined in `theme.css`)

### Out of Scope (Confirmed)
- Global Home landing page
- Per-resource embedded charts on the existing CRUD list screens
- Timeline (unified activity feed)
- AI Coach panel, insight quotes, percentage scores
- Budget-usage bar on the Finance dashboard (no data relationship exists)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 4 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 6 |
| Validations Completed | 2 |
| Duration | ~25 minutes |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_SECTION_DASHBOARDS.md`
