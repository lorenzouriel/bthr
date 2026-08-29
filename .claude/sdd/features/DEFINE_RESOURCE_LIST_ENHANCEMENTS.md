# DEFINE: Resource List Enhancements

> Add search, sort, month-grouping, and visual polish to the 15 resource list screens

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | RESOURCE_LIST_ENHANCEMENTS |
| **Date** | 2026-08-29 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

The 15 resource list screens render every row in raw API order with no way to search, sort, or group them, so a user with more than a handful of rows in any resource must manually scan the full unordered list to find, compare, or make sense of their data.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse end user | Any logged-in account holder | Cannot search, sort, or group any resource list; the only affordance today is scrolling |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Every resource list has a working search box filtering by substring match against `listPrimary` + `listSecondary` display values |
| **MUST** | Every resource list can sort by name (always available) and toggle ascending/descending; click again clears back to default order |
| **MUST** | Resources with a `listValue` (12/15) can additionally sort by value; resources with a `dateField` (13/15) can additionally sort by date |
| **MUST** | Resources with a `dateField` (13/15) can be grouped by month, with an uppercase month-label header per group (Meridian-style), ordered most-recent-first |
| **MUST** | `bills` and `weekly-routines` (no `dateField`) simply don't render Date-sort or Group controls — no fabricated date |
| **SHOULD** | A row-count indicator ("N {resource}" or "N of M" when filtered) is visible next to the controls |
| **SHOULD** | An empty search result shows a distinct message from a genuinely empty resource ("No results for “x”." vs. "No {resource} yet.") |

---

## Success Criteria

- [ ] Search, sort, and group state is component-local and correctly resets when navigating to a different resource (verified via the existing `key={config.key}` remount)
- [ ] 0 new backend files/endpoints, 0 new npm dependencies
- [ ] All computed groupings use month keys derived from date-string slicing, never local-timezone `Date` parsing (lesson carried forward from `SECTION_DASHBOARDS`'s timezone bug)
- [ ] `npm run build` exits 0 with no TypeScript errors
- [ ] Live-verified against real seeded data: search/sort/group results cross-checked against the known seed values for at least one resource per section (Goals, Meals, Journal Entries)

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Search filters correctly | A resource list with rows whose `listPrimary`/`listSecondary` values are known | User types a substring matching one row | Only matching rows render; others are hidden, no API refetch occurs |
| AT-002 | Search with no matches | Same as AT-001 | User types a substring matching nothing | A "No results for…" message renders, distinct from the generic empty-resource message |
| AT-003 | Sort by name toggles asc → desc → cleared | A resource list with 3+ rows | User clicks the Name sort control 3 times | 1st click: ascending alphabetical; 2nd click: descending; 3rd click: back to original API order |
| AT-004 | Sort by date only appears where a `dateField` exists | Two resources, one with a `dateField` (e.g. Meals) and one without (Bills) | User opens each resource's list | Meals shows a Date sort control; Bills does not |
| AT-005 | Sort by value only appears where `listValue` exists | A resource with `listValue` (e.g. Expenses) | User opens the list | A Value sort control is present and numerically orders rows (not lexically — e.g. `9` sorts before `10`) |
| AT-006 | Group by month buckets rows correctly | A resource with rows spanning 2+ calendar months (e.g. Earnings: 1 July row, 3 August rows) | User enables "Group by month" | Two groups render, "August 2026" before "July 2026" (most recent first), each containing exactly its rows |
| AT-007 | Group by month uses UTC-consistent bucketing | A row whose date field is exactly `T00:00:00Z` on the 1st of a month | User enables grouping on a host with a negative UTC offset | The row appears in the correct calendar month's group, not the previous month's (regression guard for the `SECTION_DASHBOARDS` timezone bug) |
| AT-008 | Group toggle only appears where a `dateField` exists | Weekly Routines (no `dateField`) | User opens the list | No "Group by month" control renders |
| AT-009 | No backend changes | Build is complete | Reviewer inspects the diff | Zero files under `api/FinPulse.Api/` are created or modified |
| AT-010 | Build succeeds | Code is complete | `npm run build` is run in `web/` | Exits 0 with no TypeScript errors |

---

## Out of Scope

- Server-side pagination/search/sort
- Multi-field compound sort (e.g. sort by date then by value)
- Day- or week-level grouping granularity
- Persisting search/sort/group state across navigation or reloads

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | No backend changes — filtering/sorting/grouping operate entirely on data the existing `GET` list endpoint already returned | Pure client-side logic inside `ResourceList.tsx` |
| Technical | No new npm dependencies | Hand-built controls, reusing existing `theme.css` `input`/`button` rules with inline-style overrides for pill shapes, matching the `ResourceList`/`ResourceForm`/dashboard convention already established |
| Technical | Grouping/date logic must be UTC-consistent (string-slice based, not local-`Date`-based) | Directly avoids re-introducing the exact bug class fixed in `BUILD_REPORT_SECTION_DASHBOARDS.md` |
| Verification | No browser-automation tool available | Live verification is data-correctness-focused (real API responses run through the actual shipped logic), not pixel-level visual verification |

---

## Technical Context

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `web/src/components/ResourceList.tsx` (modify), `web/src/config/resources.ts` (modify) | Extends already-shipped files rather than creating new ones |
| **KB Domains** | None — no project KB configured | Relying on codebase conventions + Meridian's pill/badge visual motif |
| **IaC Impact** | None | Frontend-only |

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|---------------------|------------|
| A-001 | Client-side filtering/sorting of the already-fully-fetched list is fast enough at current row counts (3-5/resource) | Would need debounced search or virtualization at much higher row counts — out of scope | [x] Consistent with `SECTION_DASHBOARDS`'s identical assumption, still valid at these volumes |
| A-002 | A single active sort key (not compound) is sufficient | A user wanting "sort by date, then by value" would need a workaround (re-sort by value within a visually-scanned date range) | [ ] Accepted as a known limitation, not blocking |
| A-003 | Grouping only by month (not day/week) is sufficient given current row counts | If row counts grow substantially, month buckets could become large; day/week grouping could be added later without restructuring the grouping mechanism (same string-slice approach, different slice length) | [x] Confirmed with user during brainstorm |

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific: no search/sort/group on 15 concrete screens, grounded in re-reading the actual component |
| Users | 2 | Single persona, accurate for this app's scope |
| Goals | 3 | Each goal specifies exact behavior per resource-config-shape (with/without dateField, with/without listValue) |
| Success | 3 | Every criterion is measurable (0 backend files, 0 new deps, exit 0 build, exact cross-checked seed values) |
| Scope | 3 | Explicit Out of Scope list carried directly from brainstorm's YAGNI section |
| **Total** | **14/15** | |

**Minimum to proceed: 12/15** ✅

---

## Open Questions

None — ready for Design.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-29 | define-agent | Initial version, derived from `BRAINSTORM_RESOURCE_LIST_ENHANCEMENTS.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_RESOURCE_LIST_ENHANCEMENTS.md`
