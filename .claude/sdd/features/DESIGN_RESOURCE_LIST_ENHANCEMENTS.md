# DESIGN: Resource List Enhancements

> Technical design for implementing Resource List Enhancements

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | RESOURCE_LIST_ENHANCEMENTS |
| **Date** | 2026-08-29 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_RESOURCE_LIST_ENHANCEMENTS.md](./DEFINE_RESOURCE_LIST_ENHANCEMENTS.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────┐
│                     ResourceList.tsx (extended)                   │
├───────────────────────────────────────────────────────────────────┤
│  useQuery(...) → raw rows (unchanged)                             │
│         │                                                          │
│         ▼                                                          │
│  1. filter(rows, searchText)        ← listPrimary + listSecondary  │
│         │                                                          │
│         ▼                                                          │
│  2. sort(filtered, sortKey, sortDir)← name | date | value          │
│         │                                                          │
│         ▼                                                          │
│  3. groupByMonth(sorted)?           ← only if dateField exists     │
│      ├─ yes → [{label, rows}] buckets, month-desc order            │
│      └─ no  → flat row list                                       │
│         │                                                          │
│         ▼                                                          │
│  Control bar (search input, sort pills, group toggle, count)       │
│  + existing row markup (title/subtitle/value/hover actions)        │
└───────────────────────────────────────────────────────────────────┘
```

No new files, no new endpoints. `resources.ts` gains one optional config field (`dateField`); everything else is contained inside `ResourceList.tsx`.

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `ResourceConfig.dateField` (new, optional) | Names the one date-typed field a resource can sort/group by, if any | TypeScript config |
| `ResourceList` control bar (new, inline JSX) | Search input, Name/Date/Value sort pills, Group-by-month pill, row-count label | React, inline styles, existing `theme.css` `input`/`button` rules |
| `ResourceList` filter/sort/group pipeline (new, local functions) | Pure functions transforming the fetched row array before render | Plain TypeScript |

---

## Key Decisions

### Decision 1: `dateField` is config, not inferred

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** Sort-by-date and group-by-month both need to know which field on a resource's row represents "the" date. Some resources have multiple date-typed fields (e.g. `budgets` has both `startDate` and `endDate`; `investments` has `purchaseDate` and `maturityDate`).

**Choice:** Add `dateField?: string` directly to `ResourceConfig`, set explicitly per resource (not inferred from `FieldConfig.type === 'date'`).

**Rationale:** Inference would be ambiguous exactly where it matters most (`budgets`, `investments` both have 2 date fields) — explicit config is unambiguous and self-documenting. Follows the same pattern already used for `listPrimary`/`listSecondary`/`listValue`.

**Alternatives Rejected:**
1. Infer from the first `date`-typed entry in `fields` — rejected: silently picks the wrong field for `budgets`/`investments` (would pick `startDate`/`purchaseDate` by field-array order, which happens to be correct here, but the correctness is accidental, not designed — future resources could break it).

**Consequences:**
- 13 of 15 `RESOURCES` entries get one new line (`dateField: '...'`); `bills` and `weekly-routines` are left without it, and every date-dependent control checks for its presence

---

### Decision 2: Month bucketing via string-slicing, never `Date` object local-time parsing

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** `SECTION_DASHBOARDS`'s build produced a real, confirmed bug: `new Date(isoString).getMonth()` reads the *local* calendar month, but the API's `date`-typed fields are always serialized as UTC midnight (`T00:00:00Z`) — on a negative-UTC-offset host, the 1st of a month reads as the last day of the *previous* month.

**Choice:** Group keys are derived by slicing the raw ISO string (`value.slice(0, 7)` → `'2026-08'`), exactly like `dateKey()` in `dashboardMath.ts`. Month display labels are built from that same sliced string via a hardcoded month-name array indexed by the numeric month — never via `Date`/`toLocaleDateString`.

**Rationale:** Applies the lesson from the previous feature's bug from the start, rather than re-discovering it. Fully string-based logic is immune to timezone bugs by construction — there is no `Date` object local-time read anywhere in the grouping path.

**Alternatives Rejected:**
1. `new Date(value).toLocaleDateString('en-US', { month: 'long', year: 'numeric', timeZone: 'UTC' })` — technically also correct (forcing UTC), but keeps a `Date`-object dependency in a code path that has already produced one timezone bug this session; the pure string approach removes the entire bug class instead of carefully avoiding it.

**Consequences:**
- Month labels are in English regardless of locale (acceptable — matches the rest of the app, which has no i18n)

---

### Decision 3: Sort/group controls are pill-shaped toggles, reusing Meridian's badge/capability-pill motif

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** Meridian's own mockup has no search/sort/group UI to copy literally (it's a static, read-only demo) — but it does use a `border-radius: 99px` pill shape twice: the Inbox badge (`app/Meridian.dc.html` line 40) and the module Capabilities pills (line 292).

**Choice:** Sort-by-Name/Date/Value and Group-by-month are rendered as small pill buttons (`border-radius: 99px`, active state = filled `var(--t)` background / `var(--b)` text, inactive = outlined). The search input reuses the existing global `input` rule from `theme.css` with an explicit narrower `width` override (the global rule defaults to `width: 100%`).

**Rationale:** Extending an existing Meridian visual motif rather than inventing a new control style with zero precedent in the reference UI, consistent with this session's overall approach of porting concrete Meridian patterns rather than generic UI conventions.

**Consequences:**
- No new CSS classes or custom properties — every new visual element is inline styles referencing existing `var(--t)`/`var(--s)`/`var(--m)`/`var(--br)`/`var(--hl)` tokens, consistent with `ResourceList`/`ResourceForm`/the dashboard components

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|---------------|
| 1 | `web/src/config/resources.ts` | Modify | Add `dateField?: string` to `ResourceConfig`; set it for 13/15 resources | (general) | None |
| 2 | `web/src/components/ResourceList.tsx` | Modify | Add search/sort/group state, filter/sort/group pipeline, and the control bar | (general) | 1 |

**Total Files:** 2 (0 create, 2 modify)

---

## Agent Assignment Rationale

| Agent | Files Assigned | Why This Agent |
|-------|------------------|-------------------|
| (general) | 1, 2 | No frontend/React specialist agent exists in `.claude/agents/`; same conclusion as every prior file in this initiative |

---

## Code Patterns

### Pattern 1: `resources.ts` — `dateField` additions

```tsx
// web/src/config/resources.ts
export interface ResourceConfig {
  key: string;
  section: 'finance' | 'body' | 'wellbeing';
  label: string;
  basePath: string;
  hasEdit: boolean;
  hasDelete: boolean;
  listPrimary: string;
  listSecondary: string[];
  listValue?: string;
  dateField?: string;   // NEW — the one date-typed field this resource sorts/groups by, if any
  fields: FieldConfig[];
}

// Per-resource additions (one line each, added to the existing object literals):
//   goals:                dateField: 'dueDate'
//   bills:                (none — dueDay is a 1-31 integer, not a date)
//   budgets:               dateField: 'startDate'
//   earnings:               dateField: 'earningDate'
//   expenses:               dateField: 'expenseDate'
//   investments:            dateField: 'purchaseDate'
//   weekly-routines:        (none — dayOfWeek only, no calendar date)
//   workouts:               dateField: 'workoutDate'
//   personal-records:       dateField: 'achievedDate'
//   meals:                  dateField: 'mealDate'
//   water-intake:           dateField: 'intakeDate'
//   body-metrics:           dateField: 'measuredDate'
//   sleep-logs:             dateField: 'bedTime'
//   meditation-sessions:    dateField: 'sessionDate'
//   journal-entries:        dateField: 'entryDate'
```

### Pattern 2: `ResourceList.tsx` — filter/sort/group pipeline (pure, local to the component)

```tsx
type SortKey = 'primary' | 'date' | 'value';

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'];

function monthLabel(yearMonth: string): string {
  const [year, month] = yearMonth.split('-');
  return `${MONTH_NAMES[Number(month) - 1]} ${year}`;
}

function applyFilterSortGroup(
  data: Record<string, unknown>[],
  config: ResourceConfig,
  search: string,
  sortKey: SortKey | null,
  sortDir: 'asc' | 'desc',
  groupByMonth: boolean,
) {
  const q = search.trim().toLowerCase();
  const filtered = !q ? data : data.filter((item) => {
    const primary = String(item[config.listPrimary] ?? '').toLowerCase();
    const secondary = config.listSecondary.map((f) => String(item[f] ?? '')).join(' ').toLowerCase();
    return primary.includes(q) || secondary.includes(q);
  });

  const sorted = !sortKey ? filtered : [...filtered].sort((a, b) => {
    let cmp = 0;
    if (sortKey === 'primary') cmp = String(a[config.listPrimary] ?? '').localeCompare(String(b[config.listPrimary] ?? ''));
    if (sortKey === 'date' && config.dateField) cmp = String(a[config.dateField] ?? '').localeCompare(String(b[config.dateField] ?? ''));
    if (sortKey === 'value' && config.listValue) cmp = Number(a[config.listValue] ?? 0) - Number(b[config.listValue] ?? 0);
    return sortDir === 'asc' ? cmp : -cmp;
  });

  if (!groupByMonth || !config.dateField) {
    return { flat: sorted, groups: null as null | { key: string; label: string; rows: typeof sorted }[] };
  }

  const byKey = new Map<string, typeof sorted>();
  for (const item of sorted) {
    const raw = String(item[config.dateField] ?? '');
    const key = raw.slice(0, 7) || 'unknown';
    if (!byKey.has(key)) byKey.set(key, []);
    byKey.get(key)!.push(item);
  }
  const keys = [...byKey.keys()].sort((a, b) => b.localeCompare(a));
  const groups = keys.map((key) => ({
    key, label: key === 'unknown' ? 'Unknown date' : monthLabel(key), rows: byKey.get(key)!,
  }));
  return { flat: sorted, groups };
}
```

### Pattern 3: `ResourceList.tsx` — control bar JSX

```tsx
function pillStyle(active: boolean): React.CSSProperties {
  return {
    fontSize: 12, fontWeight: 600, padding: '5px 11px', borderRadius: 99,
    border: '1px solid var(--br)', background: active ? 'var(--t)' : 'var(--s)',
    color: active ? 'var(--b)' : 'var(--m)', cursor: 'pointer',
  };
}

// Inside the component, after data has loaded and data.length > 0:
<div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16, flexWrap: 'wrap' }}>
  <input
    type="text"
    placeholder={`Search ${config.label.toLowerCase()}…`}
    value={search}
    onChange={(e) => setSearch(e.target.value)}
    style={{ width: 220 }}
  />
  <button type="button" onClick={() => toggleSort('primary')} style={pillStyle(sortKey === 'primary')}>
    Name{sortKey === 'primary' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
  </button>
  {config.dateField && (
    <button type="button" onClick={() => toggleSort('date')} style={pillStyle(sortKey === 'date')}>
      Date{sortKey === 'date' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
    </button>
  )}
  {config.listValue && (
    <button type="button" onClick={() => toggleSort('value')} style={pillStyle(sortKey === 'value')}>
      Value{sortKey === 'value' ? (sortDir === 'asc' ? ' ↑' : ' ↓') : ''}
    </button>
  )}
  {config.dateField && (
    <button type="button" onClick={() => setGroupByMonth((g) => !g)} style={pillStyle(groupByMonth)}>
      Group by month
    </button>
  )}
  <span style={{ marginLeft: 'auto', fontSize: 11.5, color: 'var(--m)' }}>
    {filteredCount === data.length ? `${data.length} ${config.label.toLowerCase()}` : `${filteredCount} of ${data.length}`}
  </span>
</div>
```

`toggleSort(key)`: if `sortKey !== key` → set `sortKey = key, sortDir = 'asc'`; else if `sortDir === 'asc'` → `sortDir = 'desc'`; else → `sortKey = null` (third click clears, satisfying AT-003).

### Pattern 4: Group header markup (reuses Meridian's Timeline day-header style)

```tsx
<div style={{ fontSize: 12, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--m)', padding: '18px 2px 8px' }}>
  {group.label}
</div>
```

---

## Data Flow

```text
1. useQuery fetches the resource's full row list (unchanged from before this feature)
   │
   ▼
2. applyFilterSortGroup(data, config, search, sortKey, sortDir, groupByMonth) runs on every render
   (cheap at current row counts — no memoization needed, matches DEFINE's Assumption A-001)
   │
   ▼
3a. groups === null → render the existing flat row markup over `flat`
3b. groups !== null → render one month-header + its rows, per group, in month-descending order
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|--------------------|------------------|
| None new — no additional calls to `FinPulse.Api` beyond the existing `GET` list request `ResourceList` already makes | — | — |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Unit | None — no test framework configured, consistent with prior features | N/A | N/A | N/A |
| Live data-correctness verification | AT-001 through AT-008 | Manual, via curl + the actual shipped filter/sort/group logic run against real fetched JSON | `curl`, Node (mirroring the exact pattern used for `SECTION_DASHBOARDS`) | All 8 data-behavior acceptance tests |
| Type check | All modified files | N/A | `tsc -b` via `npm run build` | 0 errors — AT-010 |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|----------------------|--------|
| Search matches zero rows | Distinct "No results for “{search}”." message (AT-002) | N/A — expected state |
| A resource has zero rows at all | Existing "No {label} yet." message, control bar not rendered (nothing to search/sort/group) | N/A |
| `dateField`/`listValue` absent | Corresponding pill simply isn't rendered — no error, no disabled/greyed control | N/A |

---

## Configuration

No new configuration. Reuses `RESOURCES` (`web/src/config/resources.ts`) exactly as it exists today, plus the one new optional field.

---

## Security Considerations

- No new endpoints, no new data exposure — all filtering/sorting/grouping happens client-side over data already returned to the already-authenticated user by the existing endpoint

---

## Observability

| Aspect | Implementation |
|--------|-------------------|
| Logging | None new |
| Metrics | None new |
| Tracing | None new |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-29 | design-agent | Initial version. Decision 2 directly carries forward the timezone lesson from `BUILD_REPORT_SECTION_DASHBOARDS.md`. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_RESOURCE_LIST_ENHANCEMENTS.md`
