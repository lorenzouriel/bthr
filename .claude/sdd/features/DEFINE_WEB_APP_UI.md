# DEFINE: Web App UI (visual fidelity fix)

> Rebuild `ResourceList`, `ResourceForm`, and `Sidebar` so the shipped `web/` SPA actually reproduces `Meridian.dc.html`'s component patterns, not just its color/font tokens — with zero functional regression to the CRUD behavior `WEB_APP` already live-verified.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP_UI |
| **Date** | 2026-08-27 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

`WEB_APP` shipped a functionally correct SPA (11/11 acceptance tests passed live), but its `ResourceList` renders a bare HTML `<table>` and `ResourceForm` renders a stacked plain form — neither reproduces `Meridian.dc.html`'s actual component patterns (list-row shape, panel treatment), even though `theme.css` correctly ported Meridian's color and font tokens; the user has confirmed this visual gap directly ("respect the UI of the app, not build from scratch a new app").

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse user | End user of the app | Sees a functionally-correct but visually generic CRUD app instead of the actual Meridian design they were shown and expect |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Rebuild `ResourceList.tsx` to render Meridian's exact `isList` block shape (`app/Meridian.dc.html` lines 242-254): border-separated rows, no `<table>`, no card wrapper, each row a stacked title+subtitle on the left with a single right-aligned value |
| **MUST** | Extend `ResourceConfig` (`web/src/config/resources.ts`) with the fields needed to drive that row shape — a primary (title) field, one or more secondary fields joined into a subtitle, and one value field — for all 15 resources, replacing the current flat `listColumns: string[]` |
| **MUST** | Rebuild `ResourceForm.tsx` as a slide-in side panel matching Meridian's Coach panel treatment (`app/Meridian.dc.html` lines 374-391): fixed ~312px width, white/`--s` surface, `border-left`, consistent internal padding — replacing the current stacked-inline form |
| **MUST** | Edit/Delete controls appear only on row hover, rendered as unicode glyphs (no icon library dependency), keeping rows visually identical to Meridian's action-free pattern at rest |
| **MUST** | Restyle `Sidebar.tsx` to match Meridian's real nav structure (spacing, weight, section-header treatment, `app/Meridian.dc.html` lines 27-69), scoped to the 3 real sections (Finance/Body/Wellbeing) — no new nav items |
| **MUST** | Every resource's create/edit/delete flow still works — re-verify full CRUD live against the real running API + Postgres after the rebuild, matching `WEB_APP`'s own verification discipline; zero functional regression from the 11 acceptance tests `WEB_APP` already passed |
| **MUST** | `npm run build` succeeds with 0 TypeScript errors after the change |
| **SHOULD** | `PersonalRecords`' row (the one resource with no edit/delete) shows no hover glyphs at all, matching its `hasEdit:false, hasDelete:false` config exactly |
| **SHOULD** | `SleepLogs`' read-only `totalHours` field displays in the list/panel but is never part of the submitted form payload, consistent with `WEB_APP`'s existing `readOnly` field handling |
| **COULD** | A subtle open/close transition on the slide-in panel (CSS transition on `transform`/`right`), matching the spirit of Meridian's existing `mfade` fade-in without inventing a new animation language |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] `ResourceList` renders Meridian's list-row shape (title/subtitle/right-aligned value, border-separated, no table) for all 15 resources
- [ ] `ResourceForm` renders as a slide-in panel matching Meridian's Coach panel dimensions/surface/border for all 15 resources
- [ ] Edit/Delete glyphs are invisible at rest and appear only on row `:hover`, for the 13 resources that support them; `PersonalRecords` shows neither
- [ ] `Sidebar` visually matches Meridian's nav spacing/weight/section-header conventions for Finance/Body/Wellbeing
- [ ] A full live create → list → update → delete cycle succeeds through the rebuilt UI for at least one resource per section (Finance/Body/Wellbeing), verified against the real running API + Postgres
- [ ] `npm run build` reports 0 TypeScript errors
- [ ] No new npm dependencies are added for icons (`package.json` diff contains no icon-library package)

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | List row matches Meridian's shape | A resource with existing rows (e.g., `Goals`) | The list screen is rendered | Each row shows a title line, a subtitle line beneath it, and a right-aligned value — no `<table>`/`<th>` elements, no card border wrapping the row, a `border-bottom` separator between rows |
| AT-002 | Hover reveals actions, rest state doesn't | A resource row for `Meals` (has edit+delete) | The mouse is not over the row vs. is over the row | At rest, no edit/delete glyphs are visible; on hover, both glyphs appear without shifting the row's layout |
| AT-003 | PersonalRecords shows no action glyphs | A `PersonalRecords` row | The row is hovered | No edit or delete glyph appears at any time, matching its `hasEdit:false, hasDelete:false` config |
| AT-004 | Create/edit panel slides in | A resource list screen | "+ New" is clicked, or a row's edit glyph is clicked | A panel appears from the right, styled with the Coach panel's dimensions/surface/border (not a centered modal, not an inline expansion) |
| AT-005 | Read-only field displays but isn't submitted | A `SleepLogs` create/edit panel | The panel is opened | `totalHours` is visible (read-only) in the panel but has no editable input and is not included in the `POST`/`PUT` payload |
| AT-006 | Full CRUD still works after rebuild — Finance | An authenticated user with plan sufficient for `Goals` | A Goal is created via the new panel, appears in the new list row shape, is edited, is deleted | Each step succeeds live against the real API; the deleted Goal's row disappears from the list |
| AT-007 | Full CRUD still works after rebuild — Body | An authenticated user | A `Meals` row is created, listed, edited, deleted via the rebuilt UI | Same live-verified cycle as AT-006 |
| AT-008 | Full CRUD still works after rebuild — Wellbeing | An authenticated user | A `JournalEntries` row is created with mood omitted, listed, edited, deleted via the rebuilt UI | Same live-verified cycle; nullable `mood` still handled correctly in the new panel's form |
| AT-009 | Sidebar visually matches Meridian's nav conventions | The app is loaded, authenticated | The sidebar is inspected | Section headers and resource links use Meridian's spacing/weight/typography conventions (`app/Meridian.dc.html` lines 27-69), not the prior generic nav styling |
| AT-010 | No new icon-library dependency | The rebuilt code | `web/package.json` is inspected | No icon-library package (e.g., no `lucide-react`, `react-icons`, `@heroicons/*`) appears in `dependencies`/`devDependencies` |
| AT-011 | Build succeeds | All rebuilt files | `npm run build` is run in `web/` | 0 TypeScript errors, build succeeds |

---

## Out of Scope

Explicitly NOT included in this feature:

- **Home dashboard** (visual-only or otherwise) — no backend for aggregate scores/focus/upcoming/quote; already out of scope in `WEB_APP`'s own DEFINE.
- **AI Coach panel as a functional or shell feature** — no AI backend exists; only its *dimensions/surface/border treatment* are borrowed as a template for the create/edit panel, the Coach panel itself is not built.
- **Inbox, Timeline** — no backend, already out of scope in `WEB_APP`.
- **Icon library dependencies** — unicode glyphs only, matching Meridian's own approach.
- **Drag-and-drop, custom animations beyond a simple panel-slide transition** — nothing in Meridian's file uses either beyond its existing `mfade` fade-in.
- **Per-resource summary/stat-card tiles** — aggregation/computed-value territory, already explicitly deferred in `BODY_MODULE_API` and `MIND_MODULE`.
- **Any backend changes** — this is a pure frontend visual fix; `FinPulse.Api` is untouched.
- **Theme switching UI** (Porcelain/Ink/Dusk selector) — `ThemeContext`/`theme.css` already exist from `WEB_APP` and are unaffected; no new theme-switcher control is being added.
- **Re-verifying all 15 resources' full CRUD live** — AT-006/007/008 spot-check one resource per section; DEFINE does not require re-running the entire 15-resource matrix live, since the underlying `apiFetch`/TanStack Query logic is unchanged, only the rendering layer around it.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Must preserve `WEB_APP`'s config-driven `ResourceList`/`ResourceForm` architecture (`DESIGN_WEB_APP.md` Decision 2) — one component pair for all 15 resources, not per-resource components | Design must extend `ResourceConfig`, not replace the config-driven pattern with hand-written screens |
| Technical | No new npm dependencies for icons | Design must specify literal unicode glyph characters, not an icon component import |
| Technical | `apiFetch`/TanStack Query data-fetching logic (`client.ts`, the `useQuery`/`useMutation` calls) is unchanged — this is a rendering-layer fix only | Design must not touch `api/client.ts`, `api/auth.ts`, or the query/mutation hook logic beyond what's needed to wire the new components |
| Scope | No new screens, no backend changes | Design must limit file changes to `Sidebar.tsx`, `ResourceList.tsx`, `ResourceForm.tsx`, `resources.ts`, and `theme.css` |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `web/src/components/{Sidebar,ResourceList,ResourceForm}.tsx` (rewrite), `web/src/config/resources.ts` (modify — new list-row config fields), `web/src/theme/theme.css` (modify — list-row/panel/hover-glyph styles); possibly one new small wrapper component for the slide-in panel shell if Design finds that cleaner than folding it into `ResourceForm.tsx` directly | No new top-level pages/routes; `App.tsx`, `AppLayout.tsx`, auth files, and all 15 resources' API integration are unaffected |
| **KB Domains** | None — same gap as `WEB_APP` | Confidence 0.85 (up from `WEB_APP`'s 0.75) — `Meridian.dc.html` is now a re-read, line-numbered 1:1 spec for the exact patterns being ported, not just a general stylistic reference |
| **IaC Impact** | None | Purely a frontend visual change |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — frontend visual-fidelity fix, not a data pipeline.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | For each of the 15 resources, a sensible `{primary, secondary[], value}` field mapping exists among its already-defined fields (no resource needs a field that doesn't already exist in its DTO/config) | Design would need to compute a derived display value (e.g., a formatted string) not already present, adding complexity beyond a config extension | [x] Confirmed by re-reading `resources.ts`'s existing 15 field lists during Define — every resource has at least 3 fields suitable for title/subtitle/value roles |
| A-002 | A CSS `position: fixed` (or `absolute` within a positioned ancestor) slide-in panel does not conflict with `AppLayout`'s existing `overflow-y: auto` main content area or the `ProtectedRoute`/`AppLayout` structure `WEB_APP` already built | If wrong, Design would need to adjust `AppLayout.tsx`'s layout/positioning context | [ ] |
| A-003 | Hover-only visibility (`:hover` CSS, no JS state) is sufficient for revealing edit/delete glyphs, with no separate touch/mobile requirement | If touch-device support is needed, `:hover` alone doesn't work on touch screens and Design would need a tap-to-reveal fallback | [x] Confirmed acceptable via brainstorm discovery (Q4) — no mobile/touch requirement was raised, and `WEB_APP`'s own DEFINE never mentioned mobile support |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific, user-confirmed, and traceable to an exact code/reference-file gap (bare `<table>`/`<input>` vs. Meridian's real component patterns) |
| Users | 2 | One clear persona with a concrete pain point, same single-persona pattern as `WEB_APP`'s own DEFINE |
| Goals | 3 | MoSCoW-prioritized, each traceable to one of 5 validated brainstorm discovery answers plus 2 validation checkpoints |
| Success | 3 | Every criterion is testable pass/fail (row shape matches, hover behavior correct, panel dimensions match, CRUD still works live, build succeeds, no new dependency) |
| Scope | 3 | Nine explicit out-of-scope items, each traced back to a brainstorm YAGNI decision |
| **Total** | **14/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design. (Assumption A-002 — whether the slide-in panel's positioning conflicts with `AppLayout`'s existing structure — should be resolved by Design reading `AppLayout.tsx` directly before finalizing the panel's CSS positioning strategy.)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-27 | define-agent | Initial version, derived from `BRAINSTORM_WEB_APP_UI.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_WEB_APP_UI.md`
