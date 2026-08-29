# BRAINSTORM: Resource List Enhancements

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | RESOURCE_LIST_ENHANCEMENTS |
| **Date** | 2026-08-29 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "A liked the way, let's just improve the UI of the tables now" — following positive feedback on `SECTION_DASHBOARDS`, the user asked to improve the UI of the 15 resource list screens (Goals, Meals, Expenses, etc.) — the row-list pattern shipped in `WEB_APP_UI`.

**Context Gathered:**
- `ResourceList.tsx` currently renders a flat, unstyled-beyond-hover list of every row the API returns, in API order, with no search/sort/grouping/count — confirmed by re-reading the file
- `ResourceConfig` (`resources.ts`) has `listPrimary`/`listSecondary`/`listValue` per resource but no notion of a canonical "date field," which any sort/group-by-date feature needs
- 13 of the 15 resources have exactly one clear date-typed field already present in their `fields` config (e.g. `expenseDate`, `mealDate`, `sessionDate`); `bills` (only `dueDay`, an integer 1-31, not a date) and `weekly-routines` (only `dayOfWeek`) have none

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `web/src/components/ResourceList.tsx`, `web/src/config/resources.ts` | Extends the existing shared component + config, same convention as prior features |
| Relevant KB Domains | None (no project KB configured) | Relying on codebase conventions + Meridian's pill/badge visual motif |
| IaC Patterns | N/A — frontend-only | No infrastructure change |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Does "tables" mean the resource list screens (the row-list pattern)? | Yes | Confirms scope is `ResourceList.tsx`, not a new component |
| 2 | What kind of improvement — visual polish, sorting, search/filter, grouping? | All four | Scope is broader than a pure styling pass; sort/search/group are real new interactive behavior on top of the existing list |
| 3 | Grouping granularity (day/week/month)? | Month | Groups stay meaningful even with today's low row counts (3-5/resource); `bills` and `weekly-routines` (no date field) simply won't offer grouping |

**Minimum Questions:** 3 ✅ (3 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Ground truth | Live Postgres (`fin_pulse`, user `uiverify`, id=2) | 15 resources, 3-5 rows each | Same seeded data used for `SECTION_DASHBOARDS` verification — reused here for search/sort/group verification |
| Related code | `web/src/components/ResourceList.tsx`, `web/src/config/resources.ts` | 2 files | The component and config being extended |
| Related code | `app/Meridian.dc.html` — badge (line 40) and capability-pill (line 292) markup | 1 file | Source of the pill/chip visual motif reused for sort-toggle and group-toggle controls, since Meridian's own mockup has no search/sort/group UI to copy literally |

**How samples will be used:**

- Live seeded rows drive the same hybrid live-verification method used for `SECTION_DASHBOARDS`: real API responses run through the actual shipped filter/sort/group logic, cross-checked by direct inspection of the known seed values.

---

## Approaches Explored

### Approach A: Extend `ResourceConfig` + `ResourceList` directly ⭐ Recommended

**Description:** Add one optional field to `ResourceConfig` (`dateField?: string`, set for the 13 resources that have one) and add local component state to `ResourceList` for search text, active sort key/direction, and a group-by-month toggle. All filtering/sorting/grouping logic lives inside `ResourceList`, driven by the existing `listPrimary`/`listSecondary`/`listValue`/`dateField` config — no per-resource custom code.

**Pros:**
- One implementation covers all 15 resources automatically; new resources added later get the same controls for free
- Small, additive change to already-shipped, already-live-verified files — same low-blast-radius principle used for `useResourceList` in the previous feature
- Search/sort/group state resets naturally on resource navigation (confirmed: `ResourceSectionPage` already keys its wrapper `<div>` by `config.key`, so React remounts `ResourceList` on every resource switch)

**Cons:**
- Sort/group semantics are necessarily generic (alphabetic on `listPrimary`, chronological on `dateField`, numeric on `listValue`) — a resource with unusual sort needs would have to live with the generic behavior or be special-cased later

**Why Recommended:** Exactly the same reasoning `WEB_APP_UI` used for the CRUD screens in the first place — all 15 resources share one shape (rows + a primary/secondary/value/date field), so one config-driven implementation is the right amount of abstraction, not a per-resource rebuild.

---

### Approach B: Per-resource custom list components for sort/search/group

**Description:** Give each resource (or at least the ones with more complex needs, like Investments' multiple numeric fields) its own list component with bespoke controls.

**Pros:**
- Full flexibility per resource type

**Cons:**
- 15x the code for a need that's actually uniform (every resource already reduces to the same primary/secondary/value/date shape); directly contradicts the config-driven precedent this session has followed since `WEB_APP`

**Why Not Recommended:** No resource's actual data shape demands anything beyond alphabetic/date/numeric sort and a text-substring search — inventing per-resource components for a uniform need is the over-engineering this project's conventions have repeatedly rejected (most recently in `SECTION_DASHBOARDS`'s Approach A vs. B comparison, for the opposite reason — there, a shared config didn't fit; here, it does).

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — extend `ResourceConfig` + `ResourceList` |
| **User Confirmation** | 2026-08-29 — confirmed target (resource lists) and desired improvements (polish + sort + search + group) via discovery questions |
| **Reasoning** | Matches the uniform shape of the 15 resources; consistent with this project's established config-driven precedent |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|------------------------|
| 1 | Add `dateField?: string` to `ResourceConfig`, populated for 13/15 resources | `bills` (`dueDay` is a 1-31 integer, not a date) and `weekly-routines` (`dayOfWeek` only) genuinely have no date to sort/group by — leaving the field `undefined` for them and hiding the Date/Group controls accordingly is honest, not a workaround | Fabricating a fake date field for `bills`/`weekly-routines` |
| 2 | Group-by-month bucket keys and labels are derived by string-slicing the ISO date (`value.slice(0,7)`), never by parsing into a local-timezone `Date` object | `SECTION_DASHBOARDS`'s build surfaced a real bug from exactly this class of mistake (UTC-midnight strings read via local `getMonth()`) — applying that lesson here from the start rather than re-discovering it | Using `new Date(value).toLocaleDateString(...)` without forcing UTC |
| 3 | Sort/search/group are pill-shaped toggle controls (`border-radius: 99px`), not a new dropdown/select component | Reuses an existing Meridian visual motif (badge/capability-pill, `border-radius:99px`) already present in the mockup, rather than inventing a new control style with no precedent in the reference UI | A `<select>` dropdown (no visual precedent in Meridian; less consistent with the app's built-so-far aesthetic) |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|------------------|------------------|
| Server-side/paginated search or sort | Current data volumes (3-5 rows/resource) make client-side filtering of the already-fetched full list instant; no pagination exists anywhere in the app today | Yes, if row counts grow substantially |
| Multi-field/compound sort (e.g. sort by date, then by value) | No use case surfaced; a single active sort key (toggle-to-clear) covers every request made | Yes |
| Day- or week-level grouping | Explicitly decided against in favor of month, given current row counts | Yes — the grouping key is a simple string-slice change if finer granularity is wanted later |
| Persisting search/sort/group state across navigation (e.g. in the URL or localStorage) | Not requested; state resetting when switching resources (a side effect of the existing `key={config.key}` remount) matches how every other piece of UI state in this app already behaves | Yes |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|----------------|-----------|
| Target confirmation (resource lists vs. something else) | ✅ | Confirmed resource lists | No |
| Improvement scope (polish/sort/search/group, multi-select) | ✅ | All four selected | No — scope expanded from initial "just improve the UI" phrasing to explicit interactive features |
| Grouping granularity | ✅ | Month | No |

**Minimum Validations:** 2 ✅ (3 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
The 15 resource list screens render every row in raw API order with no way to search, sort, or group them, so a user with more than a handful of rows in any resource has no way to find, order, or make sense of their data beyond scrolling.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| FinPulse end user | Cannot search, sort, or group any resource list; must scan the full unordered list to find a specific row |

### Success Criteria (Draft)
- [ ] Every resource list has a working search box (substring match against its primary + secondary display fields)
- [ ] Every resource list can be sorted by name (always), by date (13/15 resources), and by value (resources with a `listValue`) — ascending/descending toggle
- [ ] The 13 resources with a `dateField` can be grouped by month, with a Meridian-style uppercase month header per group
- [ ] Zero new npm dependencies, zero backend changes

### Constraints Identified
- No backend changes — all filtering/sorting/grouping is client-side over data already fetched by the existing `GET` list endpoints
- No new npm dependencies
- Must reuse existing `theme.css` tokens/rules (`input`, `button` global styles) rather than introducing new ones

### Out of Scope (Confirmed)
- Server-side pagination/search/sort
- Multi-field compound sort
- Day/week grouping granularity
- Cross-navigation persistence of search/sort/group state

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 3 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 4 |
| Validations Completed | 3 |
| Duration | ~10 minutes |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_RESOURCE_LIST_ENHANCEMENTS.md`
