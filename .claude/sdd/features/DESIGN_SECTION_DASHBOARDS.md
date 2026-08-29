# DESIGN: Section Dashboards

> Technical design for implementing Section Dashboards

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | SECTION_DASHBOARDS |
| **Date** | 2026-08-29 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_SECTION_DASHBOARDS.md](./DEFINE_SECTION_DASHBOARDS.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────┐
│                          SPA (unchanged shell)                        │
├───────────────────────────────────────────────────────────────────────┤
│  Sidebar                                                               │
│   ├─ section label → NOW a link to /:section  (NEW)                   │
│   └─ resource item  → link to /:section/:resourceKey (unchanged)      │
│                                                                        │
│  App.tsx routes                                                       │
│   /                     → redirect to /finance          (CHANGED)     │
│   /:section             → SectionDashboardPage           (NEW)        │
│   /:section/:resourceKey→ ResourceSectionPage             (unchanged) │
│                                                                        │
│  SectionDashboardPage ─ dispatches on :section param                  │
│    ├─ FinanceDashboard    ─┐                                          │
│    ├─ BodyDashboard        ├─ each: useResourceList(key) × N          │
│    └─ WellbeingDashboard  ─┘        ↓                                 │
│                                dashboardMath.ts (pure aggregation)     │
│                                      ↓                                │
│                            DashboardBlocks.tsx (StatCard, BarChart,   │
│                                    ProgressBars, DotGrid)              │
│                                      ↓                                │
│  useResourceList(key) → apiFetch(existing GET list endpoint)          │
│                          (same endpoints ResourceList already calls)  │
└───────────────────────────────────────────────────────────────────────┘
```

No backend component — every box above is inside `web/`. Data still terminates at the same `FinPulse.Api` list endpoints already in production use.

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `useResourceList` hook | Fetch one resource's full list by key, sharing the TanStack Query cache with `ResourceList` | React + TanStack Query |
| `dashboardMath.ts` | Pure functions: date-key extraction, this-month/last-N-days filtering, summing, streak counting | Plain TypeScript, no dependencies |
| `DashboardBlocks.tsx` | 4 presentational components porting Meridian's exact block markup: `StatCard`, `BarChart`, `ProgressBars`, `DotGrid` | React, inline styles (matches `ResourceList`/`ResourceForm` convention) |
| `FinanceDashboard` / `BodyDashboard` / `WellbeingDashboard` | One dedicated page component per section, computing that section's specific metrics and composing the shared blocks | React |
| `SectionDashboardPage` | Route-level dispatcher, reads `:section` param and renders the matching dashboard | React Router |

---

## Key Decisions

### Decision 1: Three dedicated dashboard components, not a generic config-driven renderer

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** The CRUD side of this SPA (`ResourceList`/`ResourceForm`) is entirely config-driven — one component interprets `ResourceConfig` for all 15 resources because they share one shape (list of rows + form). Dashboards do not share a shape: Finance needs a this-month sum and goal-progress percentages, Body needs "most recent row" lookups and a 28-day existence grid, Wellbeing needs a streak algorithm. A generic `{agg: sum/avg/count}` config vocabulary cannot express "consecutive days" or "most recent row per resource" without becoming a small query language of its own.

**Choice:** Write `FinanceDashboard.tsx`, `BodyDashboard.tsx`, `WellbeingDashboard.tsx` as three separate, plainly-readable components. Extract only what is genuinely identical across all three into shared pieces: the visual blocks (`DashboardBlocks.tsx`) and the date/aggregation primitives (`dashboardMath.ts`).

**Rationale:** Matches this project's established convention (seen in the CRUD side too) of using config-driven abstraction exactly where the shape is uniform, and plain code where it isn't. Reasoned through explicitly in `BRAINSTORM_SECTION_DASHBOARDS.md`'s Approach A vs. B comparison.

**Alternatives Rejected:**
1. Single generic `SectionDashboard` driven by a metrics config (Approach B) — rejected because the metrics aren't uniform (streak, most-recent-row, and per-day-bucketed-average all need different algorithms), so the config DSL needed to express them would end up more complex than the 3 plain components it replaces.

**Consequences:**
- 3 new page files instead of 1, but each is short (~60-90 lines) and independently readable/testable
- A 4th section later means a 4th dedicated component — an accepted, explicit trade-off, not an oversight

---

### Decision 2: Client-side aggregation via a shared `useResourceList` hook, no backend changes

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** Dashboards need totals/sums/streaks that don't exist as single API fields. `ResourceList.tsx` already fetches a resource's full list via an inline `useQuery` + `apiFetch` call; that exact same pattern is what every dashboard needs, repeated across 2-5 resources per dashboard.

**Choice:** Extract the fetch into `useResourceList<T>(resourceKey: string)`, a thin hook resolving the resource's `basePath` from `RESOURCES` config (same as `ResourceList` does inline) and calling `useQuery({ queryKey: [resourceKey], queryFn: () => apiFetch<T[]>(path) })`. `ResourceList.tsx` itself is left untouched — this hook is net-new, used only by the new dashboard components, to keep this build's blast radius on already-shipped, live-verified code at zero.

**Rationale:** Confirmed during brainstorm discovery — user explicitly chose client-side aggregation over new backend summary endpoints, consistent with `WEB_APP`/`WEB_APP_UI`'s "no backend changes" constraint.

**Alternatives Rejected:**
1. New backend `GET /summary` endpoints per section — rejected: more correct at scale, but out of scope for this feature and unnecessary at current data volumes (single-digit-to-low-hundreds rows/resource).
2. Refactor `ResourceList.tsx` to use the new shared hook too — rejected for this pass: would touch already-shipped, already-live-verified code for a cosmetic DRY gain, adding regression risk with no user-facing benefit.

**Consequences:**
- Because `useResourceList` and `ResourceList`'s inline query both use `queryKey: [resourceKey]`, TanStack Query's cache is naturally shared between a dashboard and that resource's list page within the same session — an incidental benefit, not a requirement
- Any future resource added to `RESOURCES` is automatically fetchable by any dashboard with zero additional plumbing

---

### Decision 3: No Meridian accent colors (`--ab`/`--am`/`--as`) — single accent, `var(--t)`

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-29 |

**Context:** Meridian colors each pillar's bars/dots/chart fills with a distinct accent variable (`--ab` for Body, `--am` for Mind, `--as` for Spirit) sourced from its per-pillar `acc` field. This session's DEFINE explicitly dropped the pillar/module conceptual reframing per user feedback ("not the pillars, more the UI itself, the panels, graphs").

**Choice:** All three dashboards use the single existing `var(--t)` (primary text/ink color, already defined in `theme.css`) for every bar fill, "on" dot, and progress-bar fill. No new CSS custom properties introduced.

**Rationale:** Keeps the constraint from `DEFINE_SECTION_DASHBOARDS.md` ("reuse existing CSS custom properties, don't introduce new color tokens") satisfied exactly, and avoids re-introducing the pillar-accent concept the user asked not to port.

**Alternatives Rejected:**
1. Three distinct accent colors, one per section — rejected: reintroduces the pillar-accent-color concept the user asked to leave out; also would require adding 3 new CSS variables, violating the "no new color tokens" constraint.

**Consequences:**
- Visually simpler/more monochrome than Meridian's mockup — an accepted trade-off in exchange for staying inside the existing token set

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `web/src/hooks/useResourceList.ts` | Create | Shared resource-list fetch hook | (general) | None |
| 2 | `web/src/utils/dashboardMath.ts` | Create | Pure date/aggregation helpers | (general) | None |
| 3 | `web/src/components/DashboardBlocks.tsx` | Create | Shared visual primitives (StatCard, BarChart, ProgressBars, DotGrid) | (general) | None |
| 4 | `web/src/pages/FinanceDashboard.tsx` | Create | Finance section dashboard | (general) | 1, 2, 3 |
| 5 | `web/src/pages/BodyDashboard.tsx` | Create | Body section dashboard | (general) | 1, 2, 3 |
| 6 | `web/src/pages/WellbeingDashboard.tsx` | Create | Wellbeing section dashboard | (general) | 1, 2, 3 |
| 7 | `web/src/pages/SectionDashboardPage.tsx` | Create | Route-level dispatcher by `:section` | (general) | 4, 5, 6 |
| 8 | `web/src/App.tsx` | Modify | Add `/:section` route, change default redirect to `/finance` | (general) | 7 |
| 9 | `web/src/components/Sidebar.tsx` | Modify | Make section header a link to `/:section` | (general) | None |

**Total Files:** 9 (7 create, 2 modify)

---

## Agent Assignment Rationale

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| (general) | 1-9 | No frontend/React specialist agent exists in `.claude/agents/`; same conclusion reached for every prior file in `WEB_APP`/`WEB_APP_UI` |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: File type, purpose keywords, path patterns — no match for React/TypeScript SPA work

---

## Code Patterns

### Pattern 1: `useResourceList` hook

```tsx
// web/src/hooks/useResourceList.ts
import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { RESOURCES } from '../config/resources';

export function useResourceList<T = Record<string, unknown>>(resourceKey: string) {
  const { user } = useAuth();
  const config = RESOURCES.find((r) => r.key === resourceKey)!;
  const path = config.basePath.replace('{userId}', String(user!.id));

  return useQuery({
    queryKey: [resourceKey],
    queryFn: () => apiFetch<T[]>(path),
  });
}
```

### Pattern 2: `dashboardMath` pure helpers

```tsx
// web/src/utils/dashboardMath.ts

export function dateKey(value: string): string {
  return value.slice(0, 10); // 'YYYY-MM-DD' prefix of an ISO date/datetime string
}

export function isThisMonth(value: string): boolean {
  const d = new Date(value);
  const now = new Date();
  return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
}

export function lastNDates(n: number): string[] {
  const out: string[] = [];
  const now = new Date();
  for (let i = n - 1; i >= 0; i--) {
    const d = new Date(now);
    d.setDate(d.getDate() - i);
    out.push(d.toISOString().slice(0, 10));
  }
  return out;
}

export function sumBy<T>(items: T[], pick: (item: T) => number | null | undefined): number {
  return items.reduce((acc, item) => acc + (pick(item) ?? 0), 0);
}

export function mostRecentBy<T>(items: T[], pick: (item: T) => string): T | undefined {
  return [...items].sort((a, b) => pick(b).localeCompare(pick(a)))[0];
}

export function consecutiveStreak(dateStrings: string[]): number {
  const set = new Set(dateStrings.map(dateKey));
  let streak = 0;
  const cursor = new Date();
  while (set.has(cursor.toISOString().slice(0, 10))) {
    streak += 1;
    cursor.setDate(cursor.getDate() - 1);
  }
  return streak;
}
```

### Pattern 3: `DashboardBlocks` shared visual primitives

```tsx
// web/src/components/DashboardBlocks.tsx

const sectionTitleStyle: React.CSSProperties = {
  margin: '0 0 14px', fontSize: 12, fontWeight: 600, letterSpacing: '0.12em',
  textTransform: 'uppercase', color: 'var(--m)',
};

export function StatCard({ label, value, note }: { label: string; value: string; note?: string }) {
  return (
    <div style={{ padding: '16px 18px', borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)' }}>
      <div style={{ fontSize: 11.5, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--m)' }}>{label}</div>
      <div style={{ fontFamily: "'Newsreader',serif", fontWeight: 300, fontSize: 26, marginTop: 6 }}>{value}</div>
      {note && <div style={{ fontSize: 11.5, color: 'var(--m)', marginTop: 2 }}>{note}</div>}
    </div>
  );
}

export function BarChart({ title, columns }: { title: string; columns: { label: string; value: number; display: string }[] }) {
  const max = Math.max(1, ...columns.map((c) => c.value));
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div style={{ display: 'flex', gap: 12, padding: '20px 20px 14px', borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)' }}>
        {columns.map((c, i) => (
          <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 7, height: 150 }}>
            <div style={{ flex: 1, display: 'flex', alignItems: 'flex-end' }}>
              <div style={{ height: `${Math.max(4, Math.round((c.value / max) * 100))}%`, width: '100%', maxWidth: 30, margin: '0 auto', borderRadius: 4, background: c.value ? 'var(--t)' : 'var(--hl)' }} />
            </div>
            <div style={{ textAlign: 'center', fontSize: 11 }}>{c.display}</div>
            <div style={{ textAlign: 'center', fontSize: 10.5, color: 'var(--m)' }}>{c.label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function ProgressBars({ title, rows }: { title: string; rows: { label: string; value: string; percent: number }[] }) {
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div>
        {rows.map((r, i) => (
          <div key={i} style={{ display: 'flex', flexDirection: 'column', gap: 7, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 12 }}>
              <span style={{ fontSize: 13.5, fontWeight: 500 }}>{r.label}</span>
              <span style={{ fontSize: 12, color: 'var(--m)' }}>{r.value}</span>
            </div>
            <div style={{ height: 4, borderRadius: 2, background: 'var(--hl)', overflow: 'hidden' }}>
              <div style={{ width: `${Math.min(100, Math.max(0, r.percent))}%`, height: '100%', borderRadius: 2, background: 'var(--t)' }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export function DotGrid({ title, dots }: { title: string; dots: boolean[] }) {
  return (
    <div>
      <h2 style={sectionTitleStyle}>{title}</h2>
      <div style={{ display: 'inline-flex', flexWrap: 'wrap', gap: 6, padding: 18, borderRadius: 12, background: 'var(--s)', border: '1px solid var(--br)', maxWidth: 322 }}>
        {dots.map((on, i) => (
          <span key={i} style={{ width: 15, height: 15, borderRadius: 4, background: on ? 'var(--t)' : 'var(--hl)', opacity: on ? 0.85 : 1, flex: 'none' }} />
        ))}
      </div>
    </div>
  );
}
```

### Pattern 4: `FinanceDashboard` (concrete instance — Body/Wellbeing follow the same shape, see the formula table below)

```tsx
// web/src/pages/FinanceDashboard.tsx
import { useResourceList } from '../hooks/useResourceList';
import { StatCard, BarChart, ProgressBars } from '../components/DashboardBlocks';
import { dateKey, isThisMonth, lastNDates, sumBy } from '../utils/dashboardMath';

interface Expense { id: number; amount: number; category: string; expenseDate: string; }
interface Earning { id: number; amount: number; category: string; earningDate: string; }
interface Goal { id: number; name: string; currentAmount: number; targetAmount: number; currencyCode: string; }

export function FinanceDashboard() {
  const expenses = useResourceList<Expense>('expenses');
  const earnings = useResourceList<Earning>('earnings');
  const goals = useResourceList<Goal>('goals');

  if (expenses.isLoading || earnings.isLoading || goals.isLoading) {
    return <div style={{ color: 'var(--m)' }}>Loading dashboard…</div>;
  }
  if (expenses.error || earnings.error || goals.error) {
    return <div style={{ color: 'crimson' }}>Failed to load dashboard.</div>;
  }

  const expenseRows = expenses.data ?? [];
  const earningRows = earnings.data ?? [];
  const goalRows = goals.data ?? [];

  const spentThisMonth = sumBy(expenseRows.filter((e) => isThisMonth(e.expenseDate)), (e) => e.amount);
  const earnedThisMonth = sumBy(earningRows.filter((e) => isThisMonth(e.earningDate)), (e) => e.amount);
  const net = earnedThisMonth - spentThisMonth;

  const chartColumns = lastNDates(7).map((day) => {
    const total = sumBy(expenseRows.filter((e) => dateKey(e.expenseDate) === day), (e) => e.amount);
    return { label: new Date(day).toLocaleDateString(undefined, { weekday: 'short' }), value: total, display: total ? total.toFixed(0) : '—' };
  });

  const progressRows = goalRows.map((g) => ({
    label: g.name,
    value: `${g.currentAmount.toFixed(0)} / ${g.targetAmount.toFixed(0)} ${g.currencyCode}`,
    percent: g.targetAmount > 0 ? (g.currentAmount / g.targetAmount) * 100 : 0,
  }));

  const transactions = [
    ...expenseRows.map((e) => ({ t: e.category, s: e.expenseDate.slice(0, 10), r: `-${e.amount.toFixed(2)}`, date: e.expenseDate })),
    ...earningRows.map((e) => ({ t: e.category, s: e.earningDate.slice(0, 10), r: `+${e.amount.toFixed(2)}`, date: e.earningDate })),
  ].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 8);

  return (
    <div>
      <h1 style={{ margin: 0, fontFamily: "'Newsreader',serif", fontWeight: 400, fontSize: 34 }}>Finance</h1>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 14, marginTop: 30 }}>
        <StatCard label="Spent this month" value={spentThisMonth.toFixed(2)} />
        <StatCard label="Earned this month" value={earnedThisMonth.toFixed(2)} />
        <StatCard label="Net this month" value={net.toFixed(2)} />
      </div>

      <div style={{ marginTop: 30 }}>
        <BarChart title="Expenses — last 7 days" columns={chartColumns} />
      </div>

      {progressRows.length > 0 && (
        <div style={{ marginTop: 30 }}>
          <ProgressBars title="Goals" rows={progressRows} />
        </div>
      )}

      <div style={{ marginTop: 30 }}>
        <h2 style={{ margin: '0 0 14px', fontSize: 12, fontWeight: 600, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--m)' }}>Recent transactions</h2>
        {transactions.length === 0 ? (
          <div style={{ color: 'var(--m)' }}>No transactions yet.</div>
        ) : (
          transactions.map((tx, i) => (
            <div key={i} style={{ display: 'flex', alignItems: 'baseline', gap: 12, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 1, flex: 1, minWidth: 0 }}>
                <span style={{ fontSize: 14, fontWeight: 500 }}>{tx.t}</span>
                <span style={{ fontSize: 11.5, color: 'var(--m)' }}>{tx.s}</span>
              </div>
              <span style={{ fontSize: 12, color: 'var(--m)', flex: 'none' }}>{tx.r}</span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
```

### Pattern 5: Routing + Sidebar changes

```tsx
// web/src/App.tsx — diff summary
// - <Route path="/" element={<Navigate to="/finance/goals" replace />} />
// + <Route path="/" element={<Navigate to="/finance" replace />} />
// + <Route path="/:section" element={<SectionDashboardPage />} />
//   <Route path="/:section/:resourceKey" element={<ResourceSectionPage />} />   (unchanged, still below the new route)
```

```tsx
// web/src/pages/SectionDashboardPage.tsx
import { useParams } from 'react-router-dom';
import { FinanceDashboard } from './FinanceDashboard';
import { BodyDashboard } from './BodyDashboard';
import { WellbeingDashboard } from './WellbeingDashboard';

export function SectionDashboardPage() {
  const { section } = useParams();
  if (section === 'finance') return <FinanceDashboard />;
  if (section === 'body') return <BodyDashboard />;
  if (section === 'wellbeing') return <WellbeingDashboard />;
  return <div>Unknown section.</div>;
}
```

```tsx
// web/src/components/Sidebar.tsx — the section-header <div> becomes a <NavLink>
// Before:
//   <div style={{ fontSize: 11.5, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--m)', padding: '4px 10px' }}>
//     {section.label}
//   </div>
// After:
//   <NavLink to={`/${section.key}`} style={{ fontSize: 11.5, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--m)', padding: '4px 10px', textDecoration: 'none', cursor: 'pointer' }}>
//     {section.label}
//   </NavLink>
```

---

## Per-Dashboard Metric Formulas (Body & Wellbeing — same shape as Pattern 4, different resources/fields)

| Dashboard | Resources Fetched | Stat Cards | Chart | Extra Block |
|-----------|--------------------|-----------|-------|--------------|
| **Body** (`BodyDashboard.tsx`) | `body-metrics`, `meals`, `water-intake`, `sleep-logs`, `workouts` | Weight = `mostRecentBy(bodyMetrics, m => m.measuredDate).weightKg`; Calories today = `sumBy(meals.filter(dateKey(m.mealDate) === today), m => m.calories)`; Water today = `sumBy(waterIntake.filter(dateKey(w.intakeDate) === today), w => w.amountMl)`; Sleep last night = `mostRecentBy(sleepLogs, s => s.wakeTime).totalHours` | `BarChart`: "Minutes trained — last 7 days" — `lastNDates(7)` × `sumBy(workouts.filter(dateKey(w.workoutDate) === day), w => w.durationMinutes)` | `DotGrid`: "Workout consistency — last 28 days" — `lastNDates(28).map(day => workouts.some(w => dateKey(w.workoutDate) === day))` |
| **Wellbeing** (`WellbeingDashboard.tsx`) | `journal-entries`, `meditation-sessions` | Meditation streak = `consecutiveStreak(meditationSessions.map(s => s.sessionDate))`; Journal entries this month = `journalEntries.filter(isThisMonth).length`; Avg mood (7-day) = mean of `journalEntries.filter(e => lastNDates(7).includes(dateKey(e.entryDate)) && e.mood != null).map(e => e.mood)` (0 if no entries with mood in range) | `BarChart`: "Mood — last 7 days" — for each of `lastNDates(7)`, average `mood` of entries on that day (0/`—` if none) | `DotGrid`: "Meditation consistency — last 28 days" — `lastNDates(28).map(day => meditationSessions.some(s => dateKey(s.sessionDate) === day))` |

All empty-data cases (AT-006) resolve naturally from the helpers: `sumBy([])` → `0`, `mostRecentBy([])` → `undefined` (dashboards must fall back to a placeholder string like `'—'` when this happens, rather than reading a property off `undefined`), `consecutiveStreak([])` → `0`, `.filter([]).length` → `0`.

---

## Data Flow

```text
1. User clicks a section label in the Sidebar (or lands on "/" → redirected to /finance)
   │
   ▼
2. Router renders SectionDashboardPage, which reads :section and picks the matching dashboard component
   │
   ▼
3. The dashboard component calls useResourceList() 2-5 times (one per resource it needs)
   │
   ▼
4. Each hook call fires a GET against the existing FinPulse.Api list endpoint (cookie-authed, same as ResourceList)
   │
   ▼
5. Once all queries resolve, the dashboard runs dashboardMath.ts helpers over the raw rows to compute
   this-month sums, last-N-day buckets, most-recent lookups, and streaks
   │
   ▼
6. Computed values are handed to StatCard / BarChart / ProgressBars / DotGrid for rendering
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-------------------|-----------------|
| `FinPulse.Api` (existing list endpoints only — no new routes) | REST, `GET` | Cookie-based JWT (`access_token`, `credentials: 'include'`) — identical to every existing call, no change |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Unit | None — no test framework configured in `web/`, consistent with `WEB_APP`/`WEB_APP_UI` | N/A | N/A | N/A |
| Live data-correctness verification | Every AT in DEFINE | Manual, via curl + direct Postgres queries | `curl` (cookie jar), `psql` | All 9 acceptance tests |
| Type check | All new/modified files | N/A | `tsc -b` (via `npm run build`) | 0 errors — AT-009 |

**How live verification works (no browser automation available):** for each computed metric (e.g. "spent this month"), independently compute the expected value with a direct `psql` query (e.g. `SELECT SUM(amount) FROM expenses WHERE user_id=2 AND date_trunc('month', expense_date) = date_trunc('month', now())`), then confirm the documented client-side formula (Pattern 4 / the formula table) applied to the same rows the `GET` endpoint actually returns produces the identical number — verified by code inspection of the exact formula plus a matching `psql` cross-check, the same hybrid method this session has used throughout.

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|---------------------|--------|
| Any of a dashboard's `useResourceList` queries fails (network, 401, 500) | Render a single `"Failed to load dashboard."` message (mirrors `ResourceList`'s existing error-display pattern) | No — TanStack Query's default retry behavior is left at its default (already the case for every existing query in the app) |
| A resource has zero rows | Every `dashboardMath` helper returns a safe zero/empty value (`0`, `undefined`, `[]`) — components must render a placeholder (`'—'`, `'No data yet.'`) instead of reading a property off `undefined` | N/A — not an error, an expected empty state (AT-006) |
| Unknown `:section` param | `SectionDashboardPage` renders `"Unknown section."` rather than crashing | N/A |

---

## Configuration

No new configuration. Reuses `API_BASE` (`web/src/api/client.ts`) and `RESOURCES`/`SECTIONS` (`web/src/config/resources.ts`) exactly as they exist today.

---

## Security Considerations

- No new endpoints, no new authentication surface — every request is the same cookie-authed `GET` call already used by `ResourceList`, against data the logged-in user already has access to
- All aggregation happens client-side over data the API already authorized the user to see; no new data exposure

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | None new — matches `WEB_APP`/`WEB_APP_UI` (no frontend logging infrastructure exists) |
| Metrics | None new |
| Tracing | None new |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-29 | design-agent | Initial version. Decision 1 resolves brainstorm's Approach A vs. B choice; Decision 2 resolves the client-side-aggregation discovery answer; Decision 3 resolves the "not the pillars" feedback into a concrete no-new-color-tokens rule. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_SECTION_DASHBOARDS.md`
