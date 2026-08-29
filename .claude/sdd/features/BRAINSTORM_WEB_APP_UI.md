# BRAINSTORM: Web App UI (visual fidelity fix)

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | WEB_APP_UI |
| **Date** | 2026-08-27 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "You need to respect the UI of the app, not build from scratch a new app"

**Context Gathered:**
- `WEB_APP` (shipped-code-complete, `BUILD_REPORT_WEB_APP.md`) built `ResourceList.tsx` as a bare, unstyled HTML `<table>` and `ResourceForm.tsx` as a stacked plain-`<input>` form. `DESIGN_WEB_APP.md`'s Pattern 4 ported Meridian's CSS *variable values* (colors, fonts) into `theme.css`, but the actual *component patterns* (list-row shape, card treatments, spacing conventions) were never ported — `theme.css` styled bare `<table>`/`<input>` elements generically rather than reproducing Meridian's specific layouts. This was flagged honestly in the prior BUILD_REPORT's AT-010 as "value-level, not rendered" verification, but the user's feedback confirms the gap is real and matters.
- Re-read all of `app/Meridian.dc.html` (all 748 lines, including lines 625-748 not previously read in full during `WEB_APP`'s design). Confirmed: Meridian is a **read-only dashboard mock** — it has zero CRUD form patterns anywhere (no `<input>`, no create/edit UI at all, except the AI Coach chat box and an "Inbox reclassify" button). The one directly reusable pattern for a resource list is the `isList` block (`app/Meridian.dc.html` lines 242-254): border-separated rows, no card wrapper, each row is `{title, subtitle, right-aligned value}` — no action buttons, nothing clickable in the row itself.
- Confirmed Meridian's Coach panel (lines 374-391) is the only existing "panel" visual pattern in the whole file: 312px fixed width, white surface (`--s`), `border-left`, consistent internal padding — a natural template for a slide-in create/edit panel, since Meridian has no modal/overlay pattern to draw from instead.
- Confirmed Meridian's nav icons are plain unicode glyphs (`⌂`, `◩`, `≡` for Home/Inbox/Timeline) — no icon library is used anywhere in the file.
- The current `Sidebar.tsx` groups resources under "Finance"/"Body"/"Wellbeing" headers, structurally similar to Meridian's pillar→module nav, but doesn't visually match (missing the accent-dot treatment, spacing, weight conventions Meridian actually uses).

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `web/src/components/{Sidebar,ResourceList,ResourceForm}.tsx` (modify), `web/src/config/resources.ts` (modify — config shape needs new list-row fields), `web/src/theme/theme.css` (modify — add list-row/panel/hover-glyph styles) | No new files needed beyond possibly a new panel wrapper component; this is a visual refinement of `WEB_APP`'s existing architecture, not a rebuild |
| Relevant KB Domains | None — same gap as `WEB_APP` (no frontend domain in `.claude/kb/`) | Confidence 0.85 (up from `WEB_APP`'s 0.75) — `Meridian.dc.html` is now a literal, line-numbered 1:1 visual spec for every pattern being ported, not just a stylistic reference |
| IaC Impact | None | Purely a frontend visual change, no backend/infra involved |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | How deep should this visual-fidelity pass go? | Full visual port — list, form, and sidebar all rebuilt to match Meridian | Both `ResourceList` (list-row pattern) and `ResourceForm` (new panel design) are in scope, not just the list |
| 2 | Where should the create/edit form live visually, given Meridian has no form pattern to copy? | Slide-in side panel, styled like Meridian's existing Coach panel | Reuses Meridian's one real "panel" pattern (312px, white surface, border-left) rather than inventing an unrelated modal/overlay style |
| 3 | Should a visual-only Home dashboard or AI Coach panel shell be added for layout completeness? | Skip both — focus purely on the 15 resource screens | No new screens; scope stays tightly on fixing what's broken, not adding new (fake) surface area |
| 4 | How should Edit/Delete actions fit into Meridian's action-free list-row pattern? | Reveal icon buttons on row hover | Rows stay visually clean at rest (matching Meridian exactly); hover state is a small, contained departure rather than permanent button chrome |
| 5 | Any additional design references beyond Meridian.dc.html itself? | None — Meridian.dc.html is the only reference; use unicode glyphs (not an icon library) for the hover actions, matching Meridian's own nav-icon approach | Confirms scope of "ground truth" and rules out adding a new dependency (icon library) |

**Minimum Questions:** 3 (5 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Design reference | `app/Meridian.dc.html`, specifically lines 242-254 (list block), 374-391 (Coach panel), 27-69 (sidebar nav) | 1 file, 3 specific patterns | Now the literal, line-numbered source for every visual pattern being ported — re-read in full (all 748 lines) during this brainstorm, not just referenced from memory |
| Related code (current, to be fixed) | `web/src/components/{Sidebar,ResourceList,AppLayout}.tsx`, `web/src/config/resources.ts` | 4 files | The actual gap being closed — read in full during this session to identify exactly what doesn't match Meridian |
| API response shapes | Unchanged from `WEB_APP` — no backend involvement this pass | — | — |

**How samples will be used:**

- Meridian's exact list-row DOM/style shape (border-bottom separator, baseline-aligned title/subtitle stack + right-aligned value, no card) is reproduced structurally in the new `ResourceList`, not just color-matched.
- Meridian's Coach panel's exact dimensions/surface/border treatment are reused as the create/edit panel's shell.
- Meridian's unicode-glyph nav icons are the direct precedent for the hover-reveal edit/delete glyphs (e.g., a pencil-like glyph and `✕`), avoiding a new icon-library dependency.

---

## Approaches Explored

### Approach A: Extend `resources.ts` config with list-row fields; one reusable Meridian-styled row/panel component pair ⭐ Recommended

**Description:** Replace `ResourceConfig.listColumns: string[]` with `listPrimary` (title field), `listSecondary` (fields joined into a subtitle string), and `listValue` (single right-aligned field) per resource. `ResourceList` becomes one component that renders Meridian's exact row shape for any resource, driven by this config. `ResourceForm` becomes a slide-in panel component, still config-driven from `fields`, styled like Meridian's Coach panel.

**Pros:**
- Preserves `WEB_APP`'s Decision 2 (one config-driven pair for 15 resources) — this is a refinement of that architecture, not an abandonment of it; the config just needs richer fields to describe *how* to lay out each resource's row, not a new per-resource component.
- Small, additive, mechanical change: 15 config entries get 3 new fields each, applied against 2 rewritten components.
- Directly matches Meridian's real DOM/style shape, not an approximation.

**Cons:**
- Choosing which 2-3 fields become `listSecondary`/`listValue` per resource requires a small editorial judgment call for each of the 15 resources (e.g., is a Bill's "value" its `amount` or its `paidThisMonth` status?) — mechanical but not fully automatic.

**Why Recommended:** Confidence 0.85 — Meridian's list-row pattern is now a literal, re-read source; keeping the config-driven architecture avoids reintroducing the 15x-duplication problem `WEB_APP`'s Decision 2 already solved once.

---

### Approach B: CSS-only restyle of the existing `<table>`/`<input>` elements

**Description:** Keep the current `ResourceList`/`ResourceForm` component structure exactly as built; only change `theme.css` to make the table/form look more polished.

**Pros:** Smallest possible change — no component rewrites, no config changes.

**Cons:** Cannot actually achieve Meridian's visual pattern — a `<table>` is fundamentally a grid of columns, while Meridian's list-row is a two-line stacked flex row (title over subtitle, value at the right). CSS alone cannot restructure a `<table>` into that shape without fighting the underlying markup. This is exactly the trap that produced the current, user-flagged gap: `theme.css` already tried "style the generic elements" and it wasn't enough.

**Why not recommended:** Would repeat the same category of mistake the user is now correcting — visual tokens without visual structure.

---

### Approach C: Per-resource custom list-row/form components (abandon the config-driven pattern)

**Description:** Write 15 hand-tuned row renderers and 15 hand-tuned forms, one per resource, to allow maximum per-resource visual customization.

**Pros:** Maximum flexibility per resource.

**Cons:** Reintroduces the exact 15x-duplication problem `WEB_APP`'s Design explicitly rejected (Decision 2) for good reason — any shared fix (e.g., adjusting the hover-glyph spacing) would need to be repeated in up to 15 places. Nothing about matching Meridian's visual pattern requires abandoning the config-driven architecture; the two are orthogonal.

**Why not recommended:** Solves a problem (per-resource customization) that doesn't exist here — every resource needs the *same* row/panel shape, just with different field mappings, which Approach A already handles via config.

---

## Data Engineering Context (if applicable)

Not applicable — this is a frontend visual-fidelity fix, not a data pipeline.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — extend `resources.ts` with list-row config fields; one reusable Meridian-styled `ResourceList`/`ResourceForm` (as a slide-in panel) pair |
| **User Confirmation** | 2026-08-27 |
| **Reasoning** | Matches Meridian's literal, re-read visual patterns; preserves the config-driven architecture from `WEB_APP` rather than reintroducing per-resource duplication |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | `ResourceList` rebuilt to match Meridian's `isList` block exactly (border-separated rows, title/subtitle/value, no table, no card wrapper) | Direct 1:1 port of a real, re-read pattern (`app/Meridian.dc.html` lines 242-254), not an approximation | CSS-only restyle of the existing `<table>` (Approach B) — structurally incapable of matching the pattern |
| 2 | Create/edit form becomes a slide-in side panel styled like Meridian's Coach panel (312px, white surface, border-left) | Meridian has zero CRUD form patterns; the Coach panel is the only existing "panel" shape in the whole file, so reusing its shell is the least-invented option | A modal/overlay — zero precedent anywhere in Meridian; would be the most "invented" choice |
| 3 | Edit/Delete exposed as hover-reveal glyphs, not always-visible buttons or click-to-open | Keeps rows visually identical to Meridian's clean, action-free pattern at rest | Always-visible icon buttons or a full clickable row — both add permanent visual chrome Meridian's list pattern doesn't have |
| 4 | No Home dashboard, no AI Coach panel shell added | Both are non-functional in this app (no aggregate scores, no AI backend) and were explicitly out of scope in `WEB_APP`'s own DEFINE; adding fake UI for non-existent features isn't "respecting the UI," it's adding new invented surface area | Adding a static Coach panel shell "for layout completeness" — rejected as scope creep with no functional payoff |
| 5 | Unicode glyphs for hover actions, no icon library | Matches Meridian's own nav-icon approach (`⌂`/`◩`/`≡`) exactly; avoids a new dependency | Pulling in an icon library (e.g., lucide-react) — unnecessary weight for 2 glyphs, and inconsistent with Meridian's own zero-icon-library approach |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Home dashboard (visual-only) | No backend for aggregate scores/focus/upcoming/quote; already out of scope in `WEB_APP` DEFINE | Yes — once a Home-worthy backend exists |
| AI Coach panel shell (visual-only) | No AI backend; adding a fake panel is invented surface area, not a UI fix | Yes — once AI coach features are built |
| Icon library dependency | Meridian itself uses zero icon libraries (unicode glyphs only) | Yes, if a future need outgrows unicode glyphs |
| Drag-and-drop, animations beyond Meridian's existing `mfade` fade-in | Not present anywhere in Meridian; would be inventing new interaction patterns, not porting existing ones | Yes |
| Per-resource summary/stat-card tiles (e.g., "12 goals, $48k total") | Aggregation/computed-value territory, already explicitly deferred in both `BODY_MODULE_API` and `MIND_MODULE` DEFINE documents | Yes — needs a dedicated aggregation-endpoint feature first |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Overall shape (config extension, list-row rebuild, slide-in panel, sidebar restyle) | ✅ | "Looks good (Recommended)" | No — confirmed as drafted |
| YAGNI scope (5 exclusions) | ✅ | "Yes, looks right (Recommended)" | No — confirmed as drafted |

**Minimum Validations:** 2 (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)

`WEB_APP`'s shipped code is functionally correct (11/11 acceptance tests passed live) but visually does not respect `Meridian.dc.html`'s actual design — `ResourceList` renders a bare HTML table and `ResourceForm` renders a stacked plain form, neither of which reproduce Meridian's real component patterns, even though `theme.css` correctly ported Meridian's color/font tokens.

### Target Users (Draft)

| User | Pain Point |
|------|------------|
| FinPulse user | Sees a functionally-correct but visually generic CRUD app instead of the actual Meridian design they were shown and expect |

### Success Criteria (Draft)

- [ ] `ResourceList` renders Meridian's exact list-row shape (title/subtitle/right-aligned value, border-separated, no table, no card) for all 15 resources
- [ ] `ResourceForm` renders as a slide-in side panel matching Meridian's Coach panel dimensions/surface/border treatment, for all 15 resources
- [ ] Edit/Delete controls appear only on row hover, as unicode glyphs, not always-visible buttons
- [ ] `Sidebar` visually matches Meridian's real nav structure (spacing, weight, section-header treatment) for the 3 real sections (Finance/Body/Wellbeing)
- [ ] Full CRUD still works live end-to-end after the visual rebuild (no functional regression) — re-verified against the real running API + Postgres
- [ ] `npm run build` succeeds with 0 TypeScript errors after the change

### Constraints Identified

- No backend changes — this is a pure frontend visual fix
- No new screens (Home, Coach panel, Inbox, Timeline stay out of scope)
- No new npm dependencies for icons — unicode glyphs only
- Must preserve `WEB_APP`'s config-driven `ResourceList`/`ResourceForm` architecture (Decision 2 from `DESIGN_WEB_APP.md`) rather than reverting to per-resource components

### Out of Scope (Confirmed)

- Home dashboard, AI Coach panel (even as visual shells)
- Icon library dependencies
- Drag-and-drop, custom animations beyond Meridian's existing fade-in
- Aggregate/summary stat tiles per resource

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 5 |
| Approaches Explored | 3 |
| Features Removed (YAGNI) | 5 |
| Validations Completed | 2 |
| Duration | ~15 min |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_WEB_APP_UI.md`
