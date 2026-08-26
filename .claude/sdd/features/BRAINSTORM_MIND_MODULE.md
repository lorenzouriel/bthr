# BRAINSTORM: Mind Module (Meditation & Journaling)

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | MIND_MODULE |
| **Date** | 2026-08-25 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "Let's add now Meditation and Journaling"

**Context Gathered:**
- The Body module (Training/Nutrition/Sleep) is the direct precedent: it was split into two features — `BODY_MODULE_DATABASE` (schema, migrations V13–V20, all live-verified against Postgres) then `BODY_MODULE_API` (REST layer on top, 7 resources, 306/306 tests passing after a later fix pass).
- No `mind`, `meditation`, or `journal`-related tables, models, or endpoints exist anywhere in the codebase today (confirmed via grep across the repo).
- Existing schema namespaces: `finance`, `plan`, `reporting`, `investment` (V1) and `body` (V13). Each domain gets its own Postgres schema.
- Body module table conventions (e.g. `body.meals`, V17): `id GENERATED ALWAYS AS IDENTITY PRIMARY KEY`, `user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE`, a domain-specific date column, `status SMALLINT NOT NULL DEFAULT 1` for soft-delete, `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, and full `COMMENT ON TABLE`/`COMMENT ON COLUMN` documentation for every table and column.
- Body module API conventions: per-resource `Controller`/`Service`/DTOs/`Model` quadruplet (explicitly no shared generic base — rejected during `BODY_MODULE_API` design), route pattern `/api/users/{userId}/{resource}`, `[Authorize]` + manual `GetCurrentUserId()` ownership check returning `Forbid()`, soft-delete via `Status`, DTOs with `DataAnnotations` only.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `database/migrations/` (next: V21, V22) and `api/FinPulse.Api/` (Models/DTOs/Services/Controllers) + `api/FinPulse.Tests/` | Mirrors the exact layout used for the Body module |
| Relevant KB Domains | None found for ASP.NET Core/EF Core/Postgres REST design — KB is data-engineering-focused | Codebase-precedent-only design, confidence 0.80 (same gap noted and accepted during `BODY_MODULE_API`) |
| IaC Patterns | N/A — plain SQL migration files (Flyway-style `V{n}__description.sql`), no IaC tooling involved | New migrations follow the same numbered-file convention |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Split into DB-first then API-later (like Body module), or build DB+API together in one pass? | DB + API together | Single BRAINSTORM/DEFINE/DESIGN/BUILD cycle covers schema and REST layer; no deferred-API follow-up needed |
| 2 | What should a Meditation session log capture? | Duration + type + mood (before/after) | Drives `mind.meditation_sessions` column set |
| 3 | What should a Journal entry capture? | Title + body + mood + tags/category | Drives `mind.journal_entries` column set |
| 4 | Where should the new tables live — new `mind` schema or inside existing `body` schema? | New `mind` schema | Keeps physical vs. mental wellness cleanly separated, consistent with how `body` was split out from `finance`/`plan`/`reporting`/`investment` |
| 5 | How should "mood" be represented (meditation before/after, journal mood)? | Numeric scale 1–5 | `SMALLINT CHECK (mood BETWEEN 1 AND 5)` on all three mood columns, consistent with existing CHECK-constrained columns in `body` schema |
| 6 | How should journal tags/category be modeled? | Single category column | `category VARCHAR(50)`, mirrors the existing single-value pattern (`meal_type`, `meditation_type`) rather than introducing arrays/join tables |

**Minimum Questions:** 3 (6 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | N/A | 0 | None available — user confirmed "None yet" |
| Output examples | N/A | 0 | — |
| Ground truth | N/A | 0 | — |
| Related code | `api/FinPulse.Api/{Models,DTOs,Services,Controllers}/{Meal,SleepLog}*.cs`, `database/migrations/V17__create_meals_table.sql`, `V20__create_sleep_logs_table.sql` | 8 files | Used as the direct structural template for both new resources |

**How samples will be used:**

- The Body module's `Meal` (simple full-CRUD resource with a date + numeric fields) and `SleepLog` (resource with nullable optional fields) files serve as the copy-adapt templates for `MeditationSession` and `JournalEntry` respectively.
- No ground truth or few-shot data needed — this is schema/API scaffolding, not an extraction or ML task.

---

## Approaches Explored

### Approach A: Mirror Body Module exactly ⭐ Recommended

**Description:** Two independent tables (`mind.meditation_sessions`, `mind.journal_entries`), full CRUD per resource, per-resource `Controller`/`Service`/DTOs/`Model` quadruplet — no shared base class.

**Pros:**
- Zero new architectural risk — this exact pattern is proven across 13 resources already (6 finance resources + 7 Body module resources)
- Consistent developer experience: any engineer familiar with `MealsController` immediately understands `JournalEntriesController`
- Independent tables mean independent CHECK constraints tuned per resource (no nullable-column compromises)

**Cons:**
- Some boilerplate duplication across the two resources (same as every other resource pair in this codebase)

**Why Recommended:** Direct precedent with 0.80 confidence (codebase-pattern match, no KB domain available). This is the same reasoning that drove every decision in `BODY_MODULE_API` — proven consistency beats novel abstraction.

---

### Approach B: Single polymorphic `mind.wellness_logs` table

**Description:** One table with an `entry_type` discriminator column (`'meditation'` / `'journal'`) covering both concepts, with nullable columns for fields that don't apply to both (e.g. `duration_minutes` null for journal rows, `title`/`content` null for meditation rows).

**Pros:**
- Fewer tables and fewer files overall

**Cons:**
- Nullable columns for fields that don't apply to both types weakens the schema's self-documentation
- CHECK constraints become harder to express correctly per discriminator value (Postgres CHECK doesn't easily do "required if type=X")
- No precedent anywhere in this codebase — every existing resource pair (e.g. `Meal` vs `WaterIntake`) uses independent tables even when conceptually related

**Why not recommended:** Trades schema clarity and constraint safety for a marginal reduction in file count. Not worth it.

---

### Approach C: Two tables with a shared generic "MindEntry" base Controller/Service

**Description:** Same two tables as Approach A, but `MeditationSessionsController`/`JournalEntriesController` inherit from a shared generic base to reduce duplication.

**Pros:**
- Less duplicated CRUD boilerplate

**Cons:**
- This exact idea (generic base vs. per-resource mirror) was explicitly evaluated and rejected during `BODY_MODULE_API`'s design phase in favor of per-resource mirroring, for the same codebase-consistency reasons
- Introduces a new architectural pattern not used anywhere else in the API layer

**Why not recommended:** Reintroducing a pattern already rejected once in this codebase, without new justification, is scope creep.

---

## Data Engineering Context (if applicable)

Not applicable — this is application schema/API scaffolding (OLTP tables behind a REST API), not a data pipeline, ETL process, or analytics workload.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — Mirror Body Module exactly |
| **User Confirmation** | 2026-08-25 |
| **Reasoning** | Zero new architectural risk; proven 13 times already in this codebase; user explicitly selected the Recommended option |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | New `mind` Postgres schema, separate from `body` | Keeps physical vs. mental wellness domains cleanly separated, matching the existing `finance`/`plan`/`reporting`/`investment`/`body` schema-per-domain pattern | Adding `meditation_sessions`/`journal_entries` into the existing `body` schema |
| 2 | Two independent tables, full CRUD, no shared base class | Direct precedent from 13 existing resources; codebase-consistency confidence 0.80 (no KB domain available) | Polymorphic single-table design (Approach B); shared generic base (Approach C, previously rejected in `BODY_MODULE_API`) |
| 3 | Mood represented as `SMALLINT CHECK (mood BETWEEN 1 AND 5)`, nullable on all three mood columns | Numeric scale is simple, sortable, chartable; nullable avoids forcing a mood rating as friction on every log/entry | Required (`NOT NULL`) mood fields; free-text mood labels |
| 4 | Journal category as single `VARCHAR(50)` column, no enforced vocabulary, no tag arrays | Mirrors the existing single-value pattern (`meal_type`, `weekly_routines.day_of_week`-style simplicity) already used everywhere in this codebase | `TEXT[]` array (no precedent, adds EF Core/Npgsql array-mapping complexity) |
| 5 | Build DB + API together in one BRAINSTORM→DEFINE→DESIGN→BUILD cycle | User explicitly chose this over repeating the Body module's DB-then-API split | Splitting into `MIND_MODULE_DATABASE` + `MIND_MODULE_API` as two separate features |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Aggregation endpoints (e.g. "average mood this week", "total meditation minutes this month") | Same YAGNI reasoning as `BODY_MODULE_API` — computable client-side from list endpoints for MVP; no aggregation endpoints exist anywhere else in this API either | Yes |
| Reminders/notifications for meditation or journaling | No notification infrastructure exists in this codebase at all; out of scope for a CRUD API | Yes |
| Streak / gamification tracking | Derived/computed concept, not raw data; adds stateful logic beyond simple logging | Yes |
| Audio/media attachments on meditation sessions | No file/blob storage infrastructure exists in this codebase | Yes |
| Journal entry sharing (multi-user visibility) | Every existing resource in this codebase is single-owner (`user_id` + ownership check); sharing would be a new cross-cutting concern | Yes |
| Full-text search on journal content | No search infrastructure (e.g. Postgres `tsvector`, external search) exists anywhere in this codebase yet | Yes |
| Tag arrays / multi-tag journal entries | No precedent for array columns in this codebase; single `category` column keeps EF Core mapping simple | Yes — could layer a join table later without breaking the base schema |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Schema design (both tables, all columns, mood nullability) | ✅ | "Looks good, mood optional (Recommended)" | No — confirmed as drafted |
| YAGNI scope (6 exclusions + full-CRUD confirmation) | ✅ | "Yes, looks right (Recommended)" | No — confirmed as drafted |

**Minimum Validations:** 2 (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)

Users have no way to log meditation sessions or journal entries in FinPulse today — the app covers finance and physical wellness (training/nutrition/sleep) but has no mental-wellness tracking.

### Target Users (Draft)

| User | Pain Point |
|------|------------|
| FinPulse user tracking personal wellness | Wants to log meditation sessions and journal entries alongside their existing finance/body tracking, in one app, without external tools |

### Success Criteria (Draft)

- [ ] `mind` schema with `meditation_sessions` and `journal_entries` tables created via numbered migrations, applied cleanly to a live Postgres instance
- [ ] All CHECK/FK constraints (duration > 0, mood 1–5, user_id FK) live-verified to correctly accept valid and reject invalid data
- [ ] Full CRUD REST API (`GET` list, `GET` by id, `POST`, `PUT`, `DELETE`) live-verified for both `MeditationSession` and `JournalEntry` resources against the running API + Postgres
- [ ] Unit tests written for both resources' Services and Controllers, and the full test suite passes (building on the now-fixed 306/306 baseline)

### Constraints Identified

- Must follow existing per-resource Controller/Service/DTO/Model file convention exactly (no shared base class)
- Must follow existing route pattern `/api/users/{userId}/{resource}` with `[Authorize]` + `GetCurrentUserId()` ownership checks
- Must use soft-delete via `Status` column, not hard delete
- Every table and column needs a real `COMMENT ON` description, matching Body module documentation standard

### Out of Scope (Confirmed)

- Aggregation/analytics endpoints
- Reminders/notifications
- Streak/gamification tracking
- Audio/media attachments
- Journal entry sharing between users
- Full-text search
- Multi-tag journal entries (array-based tags)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 6 |
| Approaches Explored | 3 |
| Features Removed (YAGNI) | 7 |
| Validations Completed | 2 |
| Duration | ~15 min |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_MIND_MODULE.md`
