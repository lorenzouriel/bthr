# DESIGN: Web App UI (visual fidelity fix)

> Technical design for rebuilding `ResourceList`, `ResourceForm`, and `Sidebar` to reproduce `Meridian.dc.html`'s actual component patterns, not just its color/font tokens.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP_UI |
| **Date** | 2026-08-27 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_WEB_APP_UI.md](./DEFINE_WEB_APP_UI.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                    web/ — RENDERING-LAYER FIX ONLY                        │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  [AppLayout: unchanged]                                                   │
│    ├─ [Sidebar]            ── restyled to match Meridian nav (lines 27-69)│
│    └─ <Outlet/>                                                           │
│         └─ [ResourceSectionPage: unchanged logic, restyled "+ New" button]│
│              ├─ [ResourceList] ── rewritten: Meridian `isList` row shape  │
│              │                    (lines 242-254) + CSS hover-reveal glyphs│
│              └─ [ResourceForm] ── rewritten: fixed-position slide-in panel│
│                                    matching Coach panel shell (lines 374-391)│
│                                                                             │
│  UNCHANGED (data layer, untouched by this feature):                      │
│  api/client.ts · api/auth.ts · auth/AuthContext.tsx ·                    │
│  TanStack Query useQuery/useMutation calls inside ResourceList/Form      │
│                                                                             │
└───────────────────────────────────────────────────────────────────────────┘
```

This is a rendering-layer fix: every `useQuery`/`useMutation`/`apiFetch` call inside `ResourceList`/`ResourceForm` is preserved verbatim — only the JSX/CSS around them changes.

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `config/resources.ts` (modified) | Add `listPrimary`/`listSecondary`/`listValue` per resource, replacing `listColumns` | TypeScript data |
| `components/ResourceList.tsx` (rewritten) | Render Meridian's `isList` row shape with CSS hover-reveal edit/delete glyphs | React + existing TanStack Query hooks |
| `components/ResourceForm.tsx` (rewritten) | Render as a fixed-position slide-in panel styled like Meridian's Coach panel | React + existing TanStack Query hooks |
| `components/Sidebar.tsx` (restyled) | Match Meridian's nav spacing/weight/section-header conventions exactly | React inline styles |
| `pages/ResourceSectionPage.tsx` (modified) | Restyle the "+ New" trigger button; logic unchanged | React |
| `theme/theme.css` (modified) | Remove now-dead `table`/`th`/`td` rules; add `.resource-row` hover-reveal rule and a `panelSlide` keyframe (matching Meridian's own `mfade` keyframe technique) | CSS |

---

## Key Decisions

### Decision 1: Slide-in panel uses `position: fixed`, not a modal/backdrop, and needs zero `AppLayout` changes

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-27 |

**Context:** DEFINE's Assumption A-002 flagged a risk: does a fixed-position panel conflict with `AppLayout`'s existing `display:flex; height:100vh; overflow:hidden` root? This needed resolving before committing to a positioning strategy.

**Choice:** The panel is `position: fixed; top: 0; right: 0; height: 100vh; width: 312px`, rendered conditionally (mounted only while `showForm` is true, exactly as `ResourceSectionPage.tsx` already gates it). No backdrop/dimming overlay — the list stays visible and scrollable behind the panel, matching Meridian's Coach panel which coexists with `<main>` content without ever dimming it.

**Rationale:** Read `AppLayout.tsx` directly during this Design session (per DEFINE's explicit instruction) — its root `<div>` has no `transform`/`filter`/`will-change`, so it does not create a new CSS containing block. `position: fixed` therefore anchors correctly to the viewport regardless of the root's `overflow: hidden`, which only clips normal-flow descendants, not fixed-positioned ones. **Zero changes to `AppLayout.tsx` are needed** — Assumption A-002 resolves cleanly in the "no conflict" direction.

**Alternatives Rejected:**
1. Make the panel a real flex sibling inside `AppLayout` (like `<Sidebar>` and `<main>`) — rejected: this would require passing `showForm`/`editing` state up through `AppLayout`/`ResourceSectionPage`, entangling layout-level and screen-level state for no visual benefit over `position: fixed`, which achieves the identical Coach-panel-like appearance more simply.
2. A modal with a dimmed backdrop — rejected in Brainstorm already (no precedent anywhere in Meridian); restated here because it would have sidestepped the `AppLayout` question entirely, but Decision 1 shows that dodge was unnecessary — `position: fixed` has no conflict to dodge.

**Consequences:**
- `ResourceForm.tsx` becomes fully self-contained: it can be dropped into `ResourceSectionPage.tsx` exactly where it already renders today, no prop drilling through `AppLayout`.

---

### Decision 2: `ResourceConfig`'s list-row fields are single field-name references, not composite templates

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-27 |

**Context:** Meridian's list rows sometimes show a composite right-hand value (e.g., `"PR +20 kg"`, `"5:24 /km"`) that isn't a single raw field. Supporting arbitrary composite formatting per resource would require either a template-string mini-language or per-resource render functions in the config.

**Choice:** `ResourceConfig` gains `listPrimary: string` (one field, the title), `listSecondary: string[]` (0-3 fields, joined with `' · '` for the subtitle line), and `listValue?: string` (one optional field, the right-aligned value — optional because some resources, like `WeeklyRoutine`, have no single natural "value" field). All three are plain field-name references into the already-fetched resource object; no template strings, no formatting functions.

**Rationale:** DEFINE's constraint requires preserving the config-driven architecture without reintroducing per-resource components; a template-string or function-based config would technically still be "config-driven" but adds a new sublanguage/escape-hatch that isn't needed here — every one of the 15 resources already has a suitable single field for each of the three roles (confirmed field-by-field below, resolving DEFINE's Assumption A-001). One deliberately accepted imperfection: `PersonalRecord`'s value (`140`) doesn't carry its `unit` (`kg`) inline — this is a minor, cosmetic simplification, not a functional gap, and matches DEFINE's Constraint against inventing new formatting mechanisms.

**Alternatives Rejected:**
1. A `format: (item) => string` function per field in the config — rejected: functions in a config array are harder to review/scan than plain field names, and no resource actually needs it badly enough to justify the added complexity (checked all 15 during this session).
2. Keep `listColumns: string[]` and have `ResourceList` guess which column is "primary" vs "secondary" vs "value" positionally — rejected: implicit, fragile, and doesn't actually change the row's visual *shape*, which is the entire point of this feature.

**Consequences:**
- `journal-entries`' `title` field is nullable — `ResourceList`'s row-primary rendering must fall back to a truncated `content` preview when `title` is empty (a small, resource-agnostic fallback rule in the component, not a config concern — see Pattern 1).

---

### Decision 3: Edit/Delete hover-reveal is pure CSS (`:hover` + `opacity`), not JS state

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-27 |

**Context:** DEFINE's AT-002 requires glyphs invisible at rest, visible on hover, without shifting row layout. DEFINE's Assumption A-003 already confirmed no touch/mobile requirement exists for this feature.

**Choice:** A `.resource-row` CSS class in `theme.css` with a nested `.row-actions { opacity: 0; transition: opacity .12s ease; }` and `.resource-row:hover .row-actions { opacity: 1; }`. The actions `<span>` is always in the DOM (reserving its layout space via `visibility`-safe `opacity`, not `display: none`), so hovering never causes the row to reflow/shift.

**Rationale:** No JS `onMouseEnter`/`onMouseLeave` state needed — CSS `:hover` is simpler, has zero re-render cost, and is the idiomatic tool for exactly this interaction. Matches DEFINE's explicit constraint (no new complexity beyond what's needed).

**Alternatives Rejected:**
1. JS-tracked hover state (`useState` + `onMouseEnter`/`onMouseLeave`) — rejected as unnecessary re-render overhead for a purely presentational effect CSS already handles natively.

**Consequences:**
- Per DEFINE's A-003, this means no tap-to-reveal fallback exists for touch devices — accepted, matching DEFINE's explicit scope.

---

### Decision 4: Reuse Meridian's own `mfade`-style keyframe technique for the panel's entrance, not a new animation library

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-27 |

**Context:** DEFINE's COULD goal: a subtle open transition for the slide-in panel, "matching the spirit of Meridian's existing `mfade` fade-in without inventing a new animation language."

**Choice:** A `@keyframes panelSlide { from { transform: translateX(100%); } to { transform: translateX(0); } }` rule in `theme.css`, applied as `animation: panelSlide .22s ease` on the panel element — directly modeled on Meridian's own `@keyframes mfade` (`app/Meridian.dc.html` line 20: `from{opacity:0;transform:translateY(6px)}to{opacity:1;transform:none}`), same technique (a CSS keyframe animation applied on mount via className), different transform axis (horizontal slide instead of vertical fade, appropriate for a side panel vs. a full-screen content swap).

**Rationale:** Zero new dependencies, zero new JS state, directly precedented by Meridian's own file rather than invented from scratch.

**Alternatives Rejected:**
1. A JS-driven transition (`useState` + a delayed class toggle, or a library like `framer-motion`) — rejected: adds a dependency and/or JS complexity for a COULD-priority, purely cosmetic effect that plain CSS `@keyframes` already covers.

**Consequences:**
- The animation runs once per mount (each time the panel opens); since `ResourceForm` unmounts on close (`{showForm && <ResourceForm/>}` in `ResourceSectionPage.tsx`), every open re-triggers the animation naturally, no manual reset logic needed.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `web/src/config/resources.ts` | Modify | Replace `listColumns` with `listPrimary`/`listSecondary`/`listValue` for all 15 resources (Decision 2) | (general) | None |
| 2 | `web/src/theme/theme.css` | Modify | Remove dead `table`/`th`/`td` rules; add `.resource-row`/`.row-actions` hover rule (Decision 3) and `panelSlide` keyframe (Decision 4) | (general) | None |
| 3 | `web/src/components/ResourceList.tsx` | Rewrite | Meridian `isList` row shape + hover-reveal glyphs | (general) | 1, 2 |
| 4 | `web/src/components/ResourceForm.tsx` | Rewrite | Fixed-position slide-in panel (Decision 1) with header/close, same field-driven body | (general) | 1, 2 |
| 5 | `web/src/components/Sidebar.tsx` | Modify | Match Meridian's nav spacing/weight conventions more precisely | (general) | None |
| 6 | `web/src/pages/ResourceSectionPage.tsx` | Modify | Restyle "+ New" trigger button only; show/hide logic unchanged | (general) | 3, 4 |

**Total Files:** 6 (0 create, 6 modify)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| (general) | All 6 | No specialist agent in `.claude/agents/` matches React/TypeScript/CSS frontend code — same conclusion reached for `WEB_APP`, `BODY_MODULE_API`, and `MIND_MODULE`. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: File type, purpose keywords, path patterns, KB domains — no match found for `.tsx`/`.css` frontend files

---

## Code Patterns

### Pattern 1: `resources.ts` — list-row field mapping (all 15 resources)

Field-by-field mapping decided by reading each resource's existing `fields`/`listColumns` (from `WEB_APP`) and choosing the most Meridian-row-appropriate primary/secondary/value:

```typescript
export interface ResourceConfig {
  key: string;
  section: 'finance' | 'body' | 'wellbeing';
  label: string;
  basePath: string;
  hasEdit: boolean;
  hasDelete: boolean;
  listPrimary: string;       // title field
  listSecondary: string[];   // 0-3 fields joined with ' · ' for the subtitle
  listValue?: string;        // optional right-aligned value field
  fields: FieldConfig[];     // unchanged from WEB_APP — still drives the form
}
```

Per-resource row mapping (replaces each entry's `listColumns` line; `fields` arrays are unchanged from `WEB_APP` and omitted here for brevity):

| Resource | `listPrimary` | `listSecondary` | `listValue` |
|----------|---------------|------------------|-------------|
| `goals` | `name` | `['currencyCode', 'dueDate']` | `currentAmount` |
| `bills` | `name` | `['category', 'dueDay']` | `amount` |
| `budgets` | `name` | `['startDate', 'endDate']` | `amountLimit` |
| `earnings` | `category` | `['paymentMethod', 'earningDate']` | `amount` |
| `expenses` | `category` | `['paymentMethod', 'expenseDate']` | `amount` |
| `investments` | `assetName` | `['investmentType', 'purchaseDate']` | `investedAmount` |
| `weekly-routines` | `routineName` | `['dayOfWeek']` | *(none)* |
| `workouts` | `routineName` | `['workoutDate']` | `durationMinutes` |
| `personal-records` | `exerciseName` | `['metricType', 'unit']` | `value` |
| `meals` | `mealType` | `['mealDate']` | `calories` |
| `water-intake` | `intakeDate` | `[]` | `amountMl` |
| `body-metrics` | `measuredDate` | `['bodyFatPercent']` | `weightKg` |
| `sleep-logs` | `bedTime` | `['wakeTime']` | `totalHours` |
| `meditation-sessions` | `meditationType` | `['sessionDate']` | `durationMinutes` |
| `journal-entries` | `title` | `['entryDate', 'category']` | `mood` |

Apply this by replacing each resource entry's `listColumns: [...]` line in the existing `RESOURCES` array with the three new fields shown above, keeping every other line (`key`, `section`, `label`, `basePath`, `hasEdit`, `hasDelete`, `fields`) exactly as `WEB_APP` built them.

---

### Pattern 2: `ResourceList.tsx` — Meridian's `isList` row shape

Directly ported from `app/Meridian.dc.html` lines 242-254 (row structure: flex row, baseline-aligned title+subtitle stack on the left, a `flex:none` value on the right, `border-bottom` separator, `padding: 11px 2px`):

```tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

interface ResourceListProps {
  config: ResourceConfig;
  onEdit: (item: Record<string, unknown>) => void;
}

function rowTitle(config: ResourceConfig, item: Record<string, unknown>): string {
  const primary = item[config.listPrimary];
  if (primary) return String(primary);
  // journal-entries fallback: nullable title -> truncated content preview (Decision 2)
  const content = item['content'];
  if (typeof content === 'string' && content.length > 0) {
    return content.length > 40 ? `${content.slice(0, 40)}…` : content;
  }
  return 'Untitled';
}

export function ResourceList({ config, onEdit }: ResourceListProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));

  const { data, isLoading, error } = useQuery({
    queryKey: [config.key],
    queryFn: () => apiFetch<Record<string, unknown>[]>(path),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apiFetch<void>(`${path}/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [config.key] }),
  });

  if (isLoading) return <div style={{ color: 'var(--m)' }}>Loading {config.label}…</div>;
  if (error) return <div style={{ color: 'crimson' }}>Failed to load {config.label}: {(error as Error).message}</div>;
  if (!data || data.length === 0) return <div style={{ color: 'var(--m)' }}>No {config.label.toLowerCase()} yet.</div>;

  return (
    <div>
      {data.map((item) => (
        <div
          key={item.id as number}
          className="resource-row"
          style={{ display: 'flex', alignItems: 'baseline', gap: 12, padding: '11px 2px', borderBottom: '1px solid var(--br)' }}
        >
          <div style={{ display: 'flex', flexDirection: 'column', gap: 1, flex: 1, minWidth: 0 }}>
            <span style={{ fontSize: 14, fontWeight: 500 }}>{rowTitle(config, item)}</span>
            {config.listSecondary.length > 0 && (
              <span style={{ fontSize: 11.5, color: 'var(--m)' }}>
                {config.listSecondary.map((f) => String(item[f] ?? '—')).join(' · ')}
              </span>
            )}
          </div>
          {config.listValue && (
            <span style={{ fontSize: 12, color: 'var(--m)', fontVariantNumeric: 'tabular-nums', flex: 'none' }}>
              {String(item[config.listValue] ?? '—')}
            </span>
          )}
          {(config.hasEdit || config.hasDelete) && (
            <span className="row-actions" style={{ display: 'flex', gap: 6, flex: 'none' }}>
              {config.hasEdit && (
                <button
                  onClick={() => onEdit(item)}
                  aria-label="Edit"
                  style={{ border: 'none', background: 'transparent', padding: 4, cursor: 'pointer', fontSize: 13 }}
                >
                  ✎
                </button>
              )}
              {config.hasDelete && (
                <button
                  onClick={() => deleteMutation.mutate(item.id as number)}
                  disabled={deleteMutation.isPending}
                  aria-label="Delete"
                  style={{ border: 'none', background: 'transparent', padding: 4, cursor: 'pointer', fontSize: 13, color: 'var(--m)' }}
                >
                  ✕
                </button>
              )}
            </span>
          )}
        </div>
      ))}
    </div>
  );
}
```

`PersonalRecords` (`hasEdit:false, hasDelete:false`) automatically renders no `.row-actions` span at all — satisfying AT-003 structurally, not via a special case.

---

### Pattern 3: `ResourceForm.tsx` — slide-in panel matching the Coach panel shell

Panel shell dimensions/surface/border directly ported from `app/Meridian.dc.html` lines 374-391 (`width:312px`, `border-left:1px solid var(--br)`, `background:var(--s)`, header `padding:20px 20px 14px` with `border-bottom`); positioning per Decision 1, entrance animation per Decision 4. The field-rendering body is unchanged from `WEB_APP`'s original `ResourceForm.tsx`:

```tsx
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apiFetch } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { ResourceConfig } from '../config/resources';

interface ResourceFormProps {
  config: ResourceConfig;
  editing: Record<string, unknown> | null;
  onDone: () => void;
}

export function ResourceForm({ config, editing, onDone }: ResourceFormProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const path = config.basePath.replace('{userId}', String(user!.id));
  const writableFields = config.fields.filter((f) => !f.readOnly);
  const readOnlyFields = config.fields.filter((f) => f.readOnly);

  const [values, setValues] = useState<Record<string, unknown>>(() =>
    Object.fromEntries(
      writableFields.map((f) => [f.name, editing?.[f.name] ?? (f.type === 'checkbox' ? false : '')])
    )
  );

  const mutation = useMutation({
    mutationFn: () =>
      editing
        ? apiFetch(`${path}/${editing.id}`, { method: 'PUT', body: JSON.stringify(values) })
        : apiFetch(path, { method: 'POST', body: JSON.stringify(values) }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [config.key] });
      onDone();
    },
  });

  return (
    <aside
      style={{
        position: 'fixed', top: 0, right: 0, height: '100vh', width: 312,
        display: 'flex', flexDirection: 'column',
        borderLeft: '1px solid var(--br)', background: 'var(--s)',
        boxShadow: '-4px 0 16px rgba(0,0,0,0.08)',
        animation: 'panelSlide .22s ease',
        zIndex: 10,
      }}
    >
      <div style={{ padding: '20px 20px 14px', borderBottom: '1px solid var(--br)', display: 'flex', alignItems: 'baseline', gap: 8 }}>
        <span style={{ fontFamily: "'Newsreader',serif", fontStyle: 'italic', fontSize: 17, flex: 1 }}>
          {editing ? `Edit ${config.label}` : `New ${config.label}`}
        </span>
        <button
          onClick={onDone}
          aria-label="Close"
          style={{ border: 'none', background: 'transparent', cursor: 'pointer', fontSize: 14, color: 'var(--m)' }}
        >
          ✕
        </button>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          mutation.mutate();
        }}
        style={{ flex: 1, overflowY: 'auto', padding: '18px 20px', margin: 0, border: 'none', maxWidth: 'none' }}
      >
        {writableFields.map((f) => (
          <label key={f.name}>
            {f.label}
            {f.type === 'textarea' ? (
              <textarea
                maxLength={f.maxLength}
                required={f.required}
                value={String(values[f.name] ?? '')}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            ) : f.type === 'checkbox' ? (
              <input
                type="checkbox"
                checked={Boolean(values[f.name])}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.checked }))}
              />
            ) : (
              <input
                type={f.type === 'datetime' ? 'datetime-local' : f.type}
                maxLength={f.maxLength}
                required={f.required}
                value={String(values[f.name] ?? '')}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            )}
          </label>
        ))}

        {readOnlyFields.length > 0 && editing && (
          <div style={{ marginTop: 4, marginBottom: 16 }}>
            {readOnlyFields.map((f) => (
              <div key={f.name} style={{ fontSize: 12.5, color: 'var(--m)' }}>
                {f.label}: {String((editing as Record<string, unknown>)[f.name] ?? '—')}
              </div>
            ))}
          </div>
        )}

        {mutation.isError && <div style={{ color: 'crimson', marginBottom: 12 }}>{(mutation.error as Error).message}</div>}
        <button type="submit" disabled={mutation.isPending}>{editing ? 'Save' : 'Create'}</button>
      </form>
    </aside>
  );
}
```

`SleepLogs`' `totalHours` (a `readOnly: true` field) is filtered out of `writableFields` exactly as `WEB_APP` already did — it now displays via the new `readOnlyFields` block when editing, satisfying AT-005, and is still never present in `values`/the submitted payload.

---

### Pattern 4: `theme.css` additions/removals

Remove (no longer used — `ResourceList` no longer renders a `<table>`):

```css
table { ... }
th, td { ... }
```

Add:

```css
.resource-row .row-actions {
  opacity: 0;
  transition: opacity .12s ease;
}
.resource-row:hover .row-actions {
  opacity: 1;
}

@keyframes panelSlide {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}
```

`form`'s existing rule (`max-width: 480px`, `margin-bottom: 20px`, etc.) is overridden inline on the panel's `<form>` (`maxWidth: 'none'`, `margin: 0`, `border: 'none'`) since the panel's `<aside>` now owns the card-like framing (border/background/shadow) that `form` used to provide standalone.

---

### Pattern 5: `Sidebar.tsx` — closer match to Meridian's nav conventions

The existing `Sidebar.tsx` (built during `WEB_APP`) already uses the right structural shape (logo row, section headers, `NavLink`s) — this pass only tightens spacing/weight/color values to match `app/Meridian.dc.html` lines 27-69 exactly:

- Section header: `fontSize: 11.5, fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--m)'` — **already correct**, no change needed here.
- Nav item active/inactive states — **already correct** (`navStyle`-equivalent logic already matches Meridian's `navStyle()` method).
- The one real gap: Meridian's section/pillar headers include a small leading accent dot (`p.dotStyle`, `this.dot(p.acc)`) before the label. Since `WEB_APP_UI`'s scope has no per-section accent color system (that was tied to Meridian's Body/Mind/Spirit pillar coloring, explicitly not carried over when Finance/Body/Wellbeing was chosen as the real nav in `WEB_APP`'s own Brainstorm), the dot is omitted rather than faked with an arbitrary color — a straightforward small increase in top/bottom spacing (`marginTop: 24` instead of `20`) is applied instead to give section groups the same visual breathing room Meridian's dot-plus-label row implies, without inventing a color-coding system DEFINE never asked for.

No code snippet needed — this is a targeted value tweak to the existing file, not a rewrite.

---

## Data Flow

Unchanged from `WEB_APP` — see `DESIGN_WEB_APP.md`'s Data Flow section. This feature only changes steps 3 ("ResourceList renders") and 5 ("ResourceForm renders fields") of that flow's visual output; the request/response/cache-invalidation mechanics are identical.

---

## Integration Points

Unchanged from `WEB_APP` — no new integration points, no backend changes.

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-----------------|-----------------|
| Live (manual, primary) | Every acceptance test in DEFINE, verified against the real running API + Postgres | `web/` via `npm run dev` | curl-based HTTP verification (no browser-automation tool available in this environment, matching `WEB_APP`'s own precedent) for the CRUD-still-works tests (AT-006/007/008); direct code/value inspection for the purely visual tests (AT-001/002/003/004/009) | All 11 acceptance tests |
| Type check | Compile-time correctness | Whole `web/` project | `tsc` via `npm run build` | 0 TypeScript errors (AT-011) |
| Dependency check | No new icon library | `web/package.json` | Manual diff | 0 new icon packages (AT-010) |

Because this feature changes only the rendering layer (Decision 1-4 all explicitly preserve the existing `apiFetch`/TanStack Query calls), Build does not need to re-run the full 15-resource CRUD matrix live — DEFINE's own Out of Scope explicitly limits live re-verification to one resource per section (AT-006/007/008), consistent with the risk actually being tested (rendering correctness, not data-layer correctness, which `WEB_APP`'s BUILD_REPORT already proved for all 15).

---

## Error Handling

Unchanged from `WEB_APP` — `mutation.isError`/`error` handling inside `ResourceList`/`ResourceForm` is preserved verbatim (see Pattern 2/3 above); only its visual placement within the new row/panel shapes changes.

---

## Configuration

No new configuration. No `package.json` dependency changes (DEFINE's AT-010 constraint).

---

## Security Considerations

No new security surface — this feature touches only presentation code. The panel's `position: fixed` + no-backdrop design does not introduce any new data exposure (it renders the same fields the old inline form rendered, just repositioned).

---

## Observability

Unchanged from `WEB_APP` — no frontend telemetry in scope, existing backend Serilog/OpenTelemetry instrumentation is unaffected since no API surface changes.

---

## Pipeline Architecture (if applicable)

Not applicable — frontend visual-fidelity fix, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-27 | design-agent | Initial version. Resolved DEFINE's Assumption A-002 by reading `AppLayout.tsx` directly (Decision 1) — confirmed `position: fixed` needs zero layout changes. Resolved A-001 by mapping all 15 resources' list-row fields explicitly (Pattern 1). |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_WEB_APP_UI.md`
