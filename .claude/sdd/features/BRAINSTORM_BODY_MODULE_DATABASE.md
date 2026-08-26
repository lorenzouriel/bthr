# BRAINSTORM: Body Module Database

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_DATABASE |
| **Date** | 2026-08-25 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "let's add a new module. The body module. The goal is to track: Training (calories, train of the day, week routine, personal records), Nutrition (Calories, Protein, Water, Carbs, Fat, User Information (weight, height, etc), Meals), Sleep (last night total, bed time, hours sleep). Let's start per database and the move to api"

**Context Gathered:**
- FinPulse's database (`database/`, already migrated to PostgreSQL) has exactly one convention so far: every table lives in the `public` schema — the `finance`/`plan`/`investment`/`reporting` schemas created in `V1` have never been used. This was a deliberate, confirmed decision during the PostgreSQL migration (preserve existing reality, don't reorganize).
- Every existing table has **zero foreign key constraints** — `user_id` is a plain `INT` column everywhere, unconstrained. This was never a deliberate design choice; it's simply how the SQL Server-era migrations were written and got carried forward.
- Existing conventions confirmed from `database/migrations/V2__create_users_table.sql` and siblings: `INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL`, `TIMESTAMPTZ NOT NULL DEFAULT now()` for `created_at`, `SMALLINT` soft-delete `status` column (1=active, 0=inactive), native `COMMENT ON TABLE`/`COMMENT ON COLUMN` for documentation instead of external docs.
- The user explicitly requested a two-phase approach — database first, API as a separate follow-up — mirroring exactly how the `POSTGRESQL_DATABASE_MIGRATION` and `POSTGRESQL_API_MIGRATION` features were sequenced.
- Body is a genuinely new bounded context (fitness/health tracking) with no relationship to the existing finance domain beyond sharing the same `users` table.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `database/migrations/V13__*.sql` onward (next available version after the existing V1–V8, V12) | New migration files only; no changes to existing finance tables |
| Relevant KB Domains | `data-modeling` (dimensional modeling, schema-evolution patterns) — general relational-modeling guidance applies, though no KB domain covers fitness/health-tracking schemas specifically | Confidence 0.70 — no direct precedent, multiple options presented and resolved via user discovery throughout this session |
| IaC Impact | None — no new services, no docker-compose changes; this is pure DDL added to the already-running Postgres instance | Existing `database/docker-compose.yml`/Flyway setup handles this without modification |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Should this brainstorm cover database only, with API as a separate follow-up feature? | Database-only this round | Scopes out all DTOs/controllers/endpoints; purely Flyway migrations |
| 2 | Should Body's tables live in a new `body` schema, or `public` alongside everything else? | New `body` schema | First real use of PostgreSQL schema namespacing in this database |
| 3 | How detailed should workout tracking be — session-level or exercise-and-set-level? | Session-level | No exercise catalog, no per-set/per-rep tables; a workout is one row |
| 4 | Should "week routine" be a separate weekly plan template distinct from actual logged workouts? | Yes, separate template table | Enables planned-vs-actual comparison; adds one more table |
| 5 | Should nutrition macros live per-meal (totals computed) or as a separate daily aggregate? | Per-meal, totals computed | No duplicate/driftable daily-summary table |
| 6 | How should water intake be logged — daily running total, or per-drink entries? | One row per day, running total | Simpler table, matches how most people think about a water goal |
| 7 | Should body metrics (weight/height) be a history over time, or a single current-value profile? | History over time | Enables trend charting; one row per user per date logged |
| 8 | Should sleep store bed_time/wake_time (duration derived), or just a duration value? | Bed time + wake time, duration derived | Enables bedtime-pattern insight; duration becomes a generated column |
| 9 | Fixed metric units, or a user-configurable unit system (mirroring finance's `currency_code` pattern)? | Fixed metric (kg, cm, ml) | No unit column needed anywhere; conversion (if ever wanted) is a display/API concern |
| 10 | Should `body.*` tables get real FK constraints to `users(id)` — a first for this database? | Yes, add real FK constraints | Genuine data-integrity improvement; this is a fresh area, not bound by matching the existing (accidental) no-FK convention |

**Minimum Questions:** 3 ✅ (10 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `database/migrations/V2__create_users_table.sql` and siblings (V3–V8) | 7 files | Source of the exact conventions this new schema must match (identity columns, TIMESTAMPTZ, status soft-delete, COMMENT ON) |
| Output examples | N/A | — | No existing Body-module schema to mirror; the full table design was validated directly with the user across two checkpoints instead |
| Ground truth | N/A | — | Fresh schema, no existing data to migrate or match |
| Related code | `database/migrations/V1__create_schemas.sql` (shows the `CREATE SCHEMA IF NOT EXISTS` pattern already established for `finance`/`plan`/`investment`/`reporting`) | 1 file | Directly reusable pattern for creating the new `body` schema |

**How samples will be used:**

- Every new table in this feature follows the exact column/constraint style already established in `V2`–`V8` — same identity-column syntax, same `TIMESTAMPTZ DEFAULT now()` pattern, same `status SMALLINT` soft-delete convention, same native `COMMENT ON` documentation approach.
- `V1`'s `CREATE SCHEMA IF NOT EXISTS` pattern is reused verbatim for `CREATE SCHEMA IF NOT EXISTS body;`.

---

## Approaches Explored

### Approach A: New `body` schema ⭐ Recommended

**Description:** `CREATE SCHEMA IF NOT EXISTS body;` — all Training/Nutrition/Sleep tables live schema-qualified (`body.workouts`, `body.meals`, etc.) instead of unqualified in `public`.

**Pros:**
- Clean separation for a genuinely distinct bounded context — Body has no relationship to finance beyond sharing `users`
- Finally puts PostgreSQL's schema namespacing to real use, rather than leaving `finance`/`plan`/`investment`/`reporting` as permanently-empty placeholders while everything piles into `public`
- Makes the domain boundary visible in every query (`body.workouts` vs. bare `expenses`) without needing a naming-prefix convention instead

**Cons:**
- Every existing table lives in `public` today — this introduces the first schema-qualified tables in the actual data, a stylistic inconsistency with current reality (though not with the schema *infrastructure*, which has always supported this)

**Why Recommended:** Confirmed directly by the user. Body is not a continuation of the finance domain in any sense — it's the right moment to start actually using schema namespacing, especially since the empty `finance`/`plan`/`investment`/`reporting` schemas already prove the infrastructure was intended for exactly this kind of separation.

---

### Approach B: `public` schema, alongside everything else

**Description:** Keep all Body tables unqualified in `public`, matching the current de facto convention for every existing table.

**Pros:**
- Maximum consistency with today's actual data layout

**Cons:**
- Continues piling unrelated domains into one namespace indefinitely, with no natural point to ever start separating them
- Wastes the schema infrastructure that already exists specifically for this purpose

**Why Not Recommended:** The user chose Approach A — a fresh, unrelated domain is the natural place to establish the separation this database's own schema list already anticipated.

---

### Approach C: Session-level workout tracking ⭐ Recommended

**Description:** A workout is logged as a single row (date, routine label, duration, calories burned, notes). Personal records are a separate, simple history table (exercise name, metric type, value, date) — not derived from per-set data.

**Pros:**
- Matches the "calories, train of the day" framing in the original request — a daily summary, not a set-by-set log
- Dramatically simpler schema (3 training tables vs. 5+ for exercise/set-level tracking) and far less data entry per workout
- Personal records are meaningful and trackable without needing the full exercise/set structure to derive them from

**Cons:**
- Can't answer "how many sets of bench press did I do on March 3rd" — only "I did a Push workout and burned 400 calories"

**Why Recommended:** Confirmed directly by the user. The original request's own phrasing ("train of the day," not "sets and reps") signals session-level intent; exercise/set-level tracking is a distinctly larger, Strong/Hevy-style feature that wasn't asked for.

---

### Approach D: Exercise-and-set-level workout tracking

**Description:** An exercise catalog table (name, muscle group) plus a workout_exercises/sets structure recording individual sets, reps, and weight per exercise per workout.

**Pros:**
- Enables precise progressive-overload tracking, per-exercise history, volume calculations

**Cons:**
- 2-3 additional tables (exercise catalog, workout-exercise join, sets) for a feature not requested
- Significantly more data entry burden per workout, which raises the risk of the whole module going unused

**Why Not Recommended:** The user chose session-level tracking (Approach C) — this remains available as a natural future iteration if session-level tracking proves insufficient.

---

## Data Engineering Context

Not applicable — this is an OLTP application schema addition, not a data pipeline. No source-system ingestion, ETL, or analytics-layer concerns apply.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A (new `body` schema) + Approach C (session-level workout tracking) |
| **User Confirmation** | 2026-08-25, via direct selection for both |
| **Reasoning** | Clean domain separation using infrastructure that already exists for this purpose, combined with the simplest schema that satisfies exactly what was asked for — no more, no less |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Database-only scope; API is a separate follow-up feature | User-requested, mirrors the proven two-phase pattern from the PostgreSQL migration | Building DB + API together in one pass |
| 2 | New `body` schema | Genuinely distinct domain; reuses schema infrastructure that already exists but has never been used | Adding to `public` |
| 3 | Session-level workout tracking (not exercise/set-level) | Matches the original request's own framing; far simpler | Full Strong/Hevy-style exercise-and-set tracking |
| 4 | Separate `weekly_routines` template table, distinct from logged `workouts` | User explicitly distinguished "train of the day" from "week routine" — enables planned-vs-actual | Routine as a free-text label on each workout only |
| 5 | Nutrition macros on individual `meals`, no separate daily aggregate | Avoids duplicate/driftable data; daily totals are `SUM(meals)` for the day | A separate daily-nutrition-summary table |
| 6 | Water intake as one row per user per day (running total) | Matches how most people think about a daily water goal; simpler than per-drink logging | One row per drink logged |
| 7 | Body metrics (weight/height/body fat) as a history table, one row per date logged | The entire point of tracking weight in a fitness context is seeing the trend | Single current-value profile row |
| 8 | Sleep stores `bed_time`/`wake_time`, total hours as a **generated column** (`GENERATED ALWAYS AS ... STORED`) | Correctness-by-construction — duration can never drift out of sync with the two timestamps it's derived from; also enables bedtime-pattern analysis later | Storing a plain `hours_slept` value with no clock times |
| 9 | Fixed metric units (kg, cm, ml) everywhere, no unit column | Simplest schema; matches common fitness-tracking convention; unit conversion (if ever needed) is a display/API concern, not a storage concern | User-configurable unit system mirroring finance's `currency_code` pattern |
| 10 | Real `REFERENCES users(id)` FK constraints on all `body.*` tables | Genuine data-integrity improvement; this is a fresh area, not bound by matching the existing (never deliberately chosen) no-FK convention | Matching existing tables' lack of FK constraints |
| 11 | Personal records as an append-only history (new row each time a record is broken), consistent with the body-metrics history pattern | A PR is inherently date-stamped ("I hit a new bench PR on this date") — matches decision #7's reasoning exactly | Upsert-in-place, keeping only the current best value per exercise |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Exercise catalog + per-set/per-rep tracking | Confirmed out of scope — session-level tracking chosen instead | Yes — a natural future iteration (Approach D) if session-level proves insufficient |
| Food catalog with searchable nutrition database | Never requested — meals are logged as free-text description + macros, not looked up from a reference table | Yes — a distinct, larger feature (nutrition database + search) |
| User-configurable unit system | Confirmed out of scope — fixed metric units chosen | Yes — would need a unit column added to every measurement table |
| Per-drink water logging | Confirmed out of scope — daily running total chosen | Yes — would need a table-structure change from update-in-place to insert-per-entry |
| Separate daily nutrition aggregate table | Confirmed out of scope — computed via `SUM(meals)` instead | Unlikely to be needed — recomputing is cheap at this data volume |
| Weekly routine plan versioning/history | Not requested — one active weekly template per user, no history of past plans | Yes — would need an effective-date range on `weekly_routines` |
| Sleep quality, naps, or sleep-stage tracking | Never mentioned in the original request — not invented | Yes — a natural extension of `sleep_logs` |
| API layer (DTOs, controllers, endpoints) | Explicitly scoped to a separate follow-up feature by the user | Yes — the next SDD cycle, once this schema ships |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Full 7-table schema design + FK constraint question | ✅ | Confirmed correct, FK constraints approved | No |
| Full decision summary (scope, per-domain structure, cross-cutting choices, YAGNI) | ✅ | Confirmed correct | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
FinPulse currently tracks only financial data; there is no way to track Training, Nutrition, or Sleep, even though the user wants these as a genuinely new, distinct module built with the same database-first discipline used for the recent PostgreSQL migration.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| FinPulse user tracking their fitness/health | No database structure exists yet to store workouts, meals, water, body metrics, or sleep — nothing to build an API or UI against |

### Success Criteria (Draft)
- [ ] `CREATE SCHEMA IF NOT EXISTS body;` and 7 new tables (`weekly_routines`, `workouts`, `personal_records`, `meals`, `water_intake`, `body_metrics`, `sleep_logs`) are created via new Flyway migrations (`V13` onward)
- [ ] Every new table has a real `FOREIGN KEY (user_id) REFERENCES users(id)` constraint
- [ ] `sleep_logs.total_hours` (or equivalent) is a PostgreSQL generated column derived from `bed_time`/`wake_time`, never independently settable
- [ ] Every table and column has a native `COMMENT ON` description, matching existing migration style
- [ ] All 7 migrations apply cleanly via `docker compose up flyway` against the already-running local Postgres instance, with 0 errors
- [ ] `sqlfluff lint` (dialect `postgres`, already configured) returns 0 violations against the new migration files

### Constraints Identified
- Database-only — no API/DTO/controller work in this feature
- Must match existing migration conventions exactly (identity columns, `TIMESTAMPTZ`, `status` soft-delete, native `COMMENT ON`)
- Fixed metric units only, no unit-conversion columns
- No changes to any existing finance table or schema

### Out of Scope (Confirmed)
- API layer (separate follow-up feature)
- Exercise catalog / per-set workout tracking
- Food catalog / nutrition database lookup
- Configurable unit systems
- Per-drink water entries
- Separate daily nutrition aggregate table
- Weekly routine plan versioning
- Sleep quality/naps/stages

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 10 |
| Approaches Explored | 4 (2 decision points, A/B and C/D) |
| Features Removed (YAGNI) | 8 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_BODY_MODULE_DATABASE.md`
