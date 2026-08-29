# DEFINE: Section Dashboards

> Add a real-data dashboard landing page for each of FinPulse's three sections (Finance, Body, Wellbeing), porting Meridian's stat-card/chart/progress-bar/dot-grid visual patterns

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | SECTION_DASHBOARDS |
| **Date** | 2026-08-29 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

Every screen in the FinPulse SPA is currently a flat list of raw rows for one resource (15 resources total); there is no view that summarizes a section's data the way Meridian's reference UI does (stat cards, trend charts, progress bars), so a user has no at-a-glance way to see totals, trends, or progress without manually scanning individual resource lists.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse end user | Any logged-in account holder | Must open each resource list individually (e.g. Expenses, then Earnings, then Goals) and mentally total/compare rows to answer basic questions like "how much did I spend this month" or "am I on track toward my goals" |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Finance section has a dashboard: spent/earned/net this month, expenses-last-7-days bar chart, goal progress bars, recent-transactions list |
| **MUST** | Body section has a dashboard: latest weight, calories today, water today, sleep last night, minutes-trained-last-7-days bar chart, workout-consistency dot grid (28 days) |
| **MUST** | Wellbeing section has a dashboard: meditation streak, journal entries this month, average mood (7-day), mood-last-7-days bar chart, meditation-consistency dot grid (28 days) |
| **MUST** | All three dashboards are reachable from the sidebar and compute every number from real data via existing endpoints — zero fabricated content, zero new backend code |
| **SHOULD** | Dashboards degrade gracefully with partial/empty data (e.g. a resource with 0 rows) rather than crashing or showing `NaN`/`undefined` |
| **COULD** | Visual polish pass to match Meridian's spacing/typography pixel-for-pixel (best-effort, not gated) |

---

## Success Criteria

- [ ] 3 new dashboard pages exist, one per section, each rendering only real computed values (no hardcoded/fake numbers)
- [ ] 0 new backend files or endpoints — 100% of aggregation happens in the SPA against existing `GET` list endpoints
- [ ] 0 new npm dependencies (charts/bars/grids hand-built with CSS, matching Meridian's own no-library approach)
- [ ] All 3 dashboards render without runtime errors against the live seeded data (3-5 rows/resource) and are live-verified via curl-driven data assertions (no browser automation available in this environment)
- [ ] Sidebar lets a user reach each section's dashboard in exactly 1 click

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Finance dashboard totals are correct | Live data has known expenses/earnings for the current month | User navigates to `/finance` | Spent/Earned/Net stat cards equal the actual sum of this-month expenses/earnings rows (verified against a direct DB query) |
| AT-002 | Finance goal progress bars | Live data has goals with `currentAmount`/`targetAmount` | User navigates to `/finance` | Each goal renders a progress bar whose percent equals `currentAmount/targetAmount` (clamped 0-100) and label matches the goal name |
| AT-003 | Body dashboard latest values | Live data has multiple `body-metrics`/`sleep-logs` rows | User navigates to `/body` | Weight and Sleep stat cards show the row with the most recent date, not an arbitrary or first-inserted row |
| AT-004 | Body workout consistency grid | Live data has workouts on some but not all of the last 28 days | User navigates to `/body` | The dot grid has exactly 28 dots; each is "on" iff at least one workout exists on that calendar date |
| AT-005 | Wellbeing meditation streak | Live data has meditation sessions on some consecutive recent days, with a gap further back | User navigates to `/wellbeing` | Streak counts only the unbroken run of days ending today/most-recent, not the total session count |
| AT-006 | Empty-resource handling | A resource a dashboard depends on has zero rows for the current user | User navigates to that dashboard | The affected stat/chart/list renders a neutral empty state (e.g. "0" or "No data yet"), not `NaN`, `undefined`, or a crash |
| AT-007 | Sidebar navigation | User is logged in | User clicks a section header/label in the sidebar | Browser navigates to that section's dashboard route (`/finance`, `/body`, or `/wellbeing`) in one click, distinct from any individual resource route |
| AT-008 | No backend changes | Build is complete | Reviewer inspects the diff | Zero files under `api/FinPulse.Api/` are created or modified |
| AT-009 | Build succeeds | Code is complete | `npm run build` is run in `web/` | Exits 0 with no TypeScript errors |

---

## Out of Scope

- Global Home landing page (greeting, cross-section activity feed, "upcoming" list) — deferred, natural next iteration
- Per-resource embedded charts on the existing CRUD list/form screens — those stay exactly as `WEB_APP_UI` left them
- Timeline (unified chronological feed across all 15 resources)
- AI Coach chat panel, AI insight quotes, and percentage "scores" per section — no scoring algorithm or AI backend exists; would be fabricated content
- Budget-usage progress bar on the Finance dashboard — `budgets` has no data relationship (FK or shared category) to `expenses` in the live schema; faking one via string-matching category names was explicitly rejected during brainstorm

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | No backend changes — all 3 dashboards compute metrics client-side from existing `GET /api/users/{userId}/...` list endpoints already used by `ResourceList` | Design must specify exact client-side aggregation logic (date filtering, grouping, streak calculation) per dashboard |
| Technical | No new npm dependencies (no charting library) | Bar charts, progress bars, and dot grids are hand-built with plain CSS/flex, mirroring Meridian's own approach exactly |
| Technical | Must reuse `theme.css`'s existing CSS custom properties (`--s`, `--br`, `--m`, `--t`, `--hl`) rather than introducing new color tokens | Keeps the new dashboards visually consistent with the already-ported list/panel/sidebar styling from `WEB_APP_UI` |
| Verification | No browser-automation tool available in this environment | Live verification is data-correctness-focused (curl against the API + direct Postgres queries to confirm computed numbers match), not pixel-level visual verification |

---

## Technical Context

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `web/src/pages/` (3 new dashboard page components + `SectionDashboardPage.tsx` router), `web/src/components/DashboardBlocks.tsx` (shared visual primitives), `web/src/hooks/useResourceList.ts` (shared data-fetching hook) | Matches `WEB_APP`/`WEB_APP_UI`'s existing directory conventions |
| **KB Domains** | None — no project KB configured in this repo | Relying on Meridian's own patterns + this session's established codebase conventions, as prior features did |
| **IaC Impact** | None | Frontend-only feature; no infrastructure, no deployment config changes |

**Why This Matters:**

- **Location** → Keeps new files inside the existing `pages/`/`components/`/(new) `hooks/` structure rather than inventing a new one
- **KB Domains** → N/A this repo
- **IaC Impact** → Confirms this is a pure frontend addition, consistent with the "no backend changes" constraint

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | Fetching all rows of 2-5 resources per dashboard (rather than a paginated/limited query) is fast enough given current data volume (single-digit-to-low-hundreds rows per user) | Would need pagination or a backend summary endpoint if a user accumulates thousands of rows per resource — out of scope for now | [x] Reasonable for current seeded data (3-5 rows/resource); no pagination exists anywhere in the SPA today, so this matches existing precedent |
| A-002 | "This month" / "last 7 days" / "last 28 days" windows are computed from the browser's local clock (`new Date()`), matching how Meridian's own hardcoded mock date logic works | If a user's browser clock/timezone is wrong, computed windows would be off — same class of issue as any client-side date logic; not addressed by this feature | [ ] Accepted as a known limitation, not blocking |
| A-003 | A "day" for date-bucketing (chart columns, streak calculation, dot grids) is defined by calendar date string equality (`YYYY-MM-DD`), not full timestamp — matching how `date`-typed columns (`meal_date`, `workout_date`, etc.) are already stored | If this doesn't match user expectation across timezones, dates could look off by one — no different from existing CRUD date fields today | [x] Consistent with existing `date`-typed columns already used unmodified throughout the SPA |

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Concrete, specific: flat lists vs. Meridian's dashboard patterns, grounded in a re-read of the actual mockup file |
| Users | 2 | Single user persona (any FinPulse account) — accurate for this app's scope, but not multiple distinct personas |
| Goals | 3 | Each goal specifies exact stat cards/charts/lists per section, MoSCoW-prioritized |
| Success | 3 | Every criterion is measurable/verifiable (0 backend files changed, 0 new deps, exit 0 build, exact numeric assertions) |
| Scope | 3 | Explicit, detailed Out of Scope list carried directly from brainstorm's YAGNI section |
| **Total** | **14/15** | |

**Minimum to proceed: 12/15** ✅

---

## Open Questions

None — ready for Design.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-29 | define-agent | Initial version, derived from `BRAINSTORM_SECTION_DASHBOARDS.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_SECTION_DASHBOARDS.md`
