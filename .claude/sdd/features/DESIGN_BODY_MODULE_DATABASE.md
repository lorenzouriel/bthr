# DESIGN: Body Module Database

> Technical design for adding a new `body` schema (Training, Nutrition, Sleep) to the FinPulse database

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_DATABASE |
| **Date** | 2026-08-25 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_BODY_MODULE_DATABASE.md](./DEFINE_BODY_MODULE_DATABASE.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                    database/migrations/ (Flyway, sequential)             │
├───────────────────────────────────────────────────────────────────────────┤
│  Existing:  V1 (schemas) → V2..V8 (finance tables) → V12 (indexes)      │
│                                                                           │
│  New:       V13 ── CREATE SCHEMA IF NOT EXISTS body;                    │
│                       │                                                  │
│                       ▼                                                 │
│             V14 ── body.weekly_routines  (planned template)             │
│             V15 ── body.workouts         (actual logged sessions)       │
│             V16 ── body.personal_records (append-only PR history)       │
│             V17 ── body.meals            (per-meal macros)              │
│             V18 ── body.water_intake     (daily running total)          │
│             V19 ── body.body_metrics     (weight/height history)        │
│             V20 ── body.sleep_logs       (bed/wake, generated duration) │
│                                                                           │
│  Every body.* table: user_id INT NOT NULL REFERENCES users(id)          │
│                       ON DELETE CASCADE   (public.users, existing table)│
└───────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                 ┌─────────────────────────┐
                 │  PostgreSQL (database/) │
                 │  fin_pulse @ :5432      │
                 │  schemas: public,       │
                 │  finance, plan,         │
                 │  investment, reporting, │
                 │  body (NEW)             │
                 └─────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `body` schema | Namespace isolating Body-module tables from finance tables | PostgreSQL schema, `CREATE SCHEMA IF NOT EXISTS` |
| `body.weekly_routines` | Reusable weekly training plan template, one row per user per day-of-week | Table with `UNIQUE(user_id, day_of_week)` |
| `body.workouts` | Session-level logged training sessions | Table |
| `body.personal_records` | Append-only history of best results per exercise | Table |
| `body.meals` | Per-meal nutrition log (calories, protein, carbs, fat) | Table |
| `body.water_intake` | One row per user per day, running total | Table with `UNIQUE(user_id, intake_date)` |
| `body.body_metrics` | History of weight/height/body fat over time | Table with `UNIQUE(user_id, measured_date)` |
| `body.sleep_logs` | Bed/wake times with a generated total-hours column | Table, `GENERATED ALWAYS AS (...) STORED` |

---

## Key Decisions

### Decision 1: Sleep duration as a verified PostgreSQL generated column

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's Assumption A-001 flagged that `GENERATED ALWAYS AS (...) STORED` needed live verification for a `TIMESTAMPTZ` subtraction expression, since generated-column expressions have restrictions (must be immutable, cannot reference other generated columns).

**Choice:** `total_hours NUMERIC(4,2) GENERATED ALWAYS AS (ROUND(EXTRACT(EPOCH FROM (wake_time - bed_time)) / 3600.0, 2)) STORED`, plus a `CHECK (wake_time > bed_time)` constraint.

**Rationale:** Tested live against the running Postgres 17.11 instance during this Design phase (not assumed): a scratch table confirmed the expression computes correctly (`8.00` hours for an 8-hour span) and that direct writes to the column are rejected (`ERROR: cannot insert a non-DEFAULT value into column "total_hours"`) — exactly the correctness-by-construction behavior DEFINE required. The `CHECK` constraint additionally resolves DEFINE's Assumption A-002 (midnight-crossing sessions): since both columns are full `TIMESTAMPTZ` values (date+time, not time-of-day-only), a bed time of `23:00` and a wake time of `07:00` the next calendar day already compute correctly with no special-casing — verified in the same live test.

**Alternatives Rejected:**
1. Trigger-based computation — rejected: a generated column is simpler, verified to work, and guarantees the value can never drift out of sync (no trigger to forget to attach)
2. Plain `hours_slept` numeric column, set by the application — rejected in brainstorm; loses the bedtime-pattern data and reintroduces the sync-drift risk a generated column eliminates

**Consequences:**
- `total_hours` can only be read, never written directly — any future API/ORM code must not attempt to set it
- The computed value is `NUMERIC(4,2)`, capping at 99.99 hours — far beyond any realistic sleep duration, so no practical limitation

---

### Decision 2: Real FK constraints + additional UNIQUE constraints beyond DEFINE's explicit ask

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE required FK constraints on every `body.*` table (already decided in brainstorm) and flagged (Assumption A-004) whether `weekly_routines` needs `UNIQUE(user_id, day_of_week)` to actually enforce "one row per user per day." The same "one row per day" design intent also applies to `water_intake` (daily running total) and `body_metrics` (one entry per date logged), neither of which DEFINE explicitly called out.

**Choice:** All FK constraints use `REFERENCES users(id) ON DELETE CASCADE` (verified live: inserting a nonexistent `user_id` correctly fails with a foreign-key-violation error). Add `UNIQUE(user_id, day_of_week)` to `weekly_routines`, `UNIQUE(user_id, intake_date)` to `water_intake`, and `UNIQUE(user_id, measured_date)` to `body_metrics`.

**Rationale:** All three tables share the identical "at most one row per user per [period]" design intent that was explicitly validated with the user during brainstorm (for `weekly_routines`) and directly implied by "one row per day, running total" (`water_intake`) / "one row per user per date logged" (`body_metrics`). Leaving the constraint off two of the three tables just because DEFINE's assumption only named one would be an inconsistent application of the same already-approved design intent — the smallest correct change is to apply it uniformly.

**Alternatives Rejected:**
1. Leave all three unconstrained, matching DEFINE's literal text — rejected: `body_metrics`/`water_intake`'s "one row per day" intent is stated just as clearly as `weekly_routines`'s, just not called out as a numbered DEFINE assumption; enforcing it inconsistently would be arbitrary
2. `ON DELETE CASCADE` alternatives (`RESTRICT`, `SET NULL`) — rejected: a deleted user's Body data has no meaning without the user; cascade delete matches how a user-owned, no-orphans domain should behave

**Consequences:**
- Application/future-API code must use `UPSERT` (`INSERT ... ON CONFLICT (user_id, day) DO UPDATE`) for these three tables rather than blind `INSERT`, or handle the unique-violation error
- Deleting a `users` row now cascades to delete all of that user's Body data — a real behavior change from every existing finance table (which has no FK at all, so a user delete today silently orphans finance rows instead)

---

### Decision 3: 8 migration files, not 7 — correcting a DEFINE count inconsistency

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's Success Criteria stated "7 new migration files (V13–V19)" while its Goals table separately listed schema creation *and* 7 tables as distinct MUST items — 8 things, not 7.

**Choice:** 8 migration files: `V13` (schema creation only) through `V20` (7 tables).

**Rationale:** `V1__create_schemas.sql` already established the precedent that schema creation gets its own dedicated migration, separate from any table. Combining `body` schema creation into the first table's migration (`V13`) would break that precedent for no benefit; keeping them separate costs nothing and stays consistent with how this database already does it.

**Alternatives Rejected:**
1. Force exactly 7 files by combining schema-creation into `V14` alongside `weekly_routines` — rejected: inconsistent with `V1`'s precedent, and DEFINE's own Goals table already implies 8 separate things

**Consequences:**
- File manifest below lists 8 files, `V13`–`V20`, not `V13`–`V19` — Build should treat this DESIGN's file list as authoritative over DEFINE's success-criteria text on this point, per DEFINE's own Goals table

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `database/migrations/V13__create_body_schema.sql` | Create | `CREATE SCHEMA IF NOT EXISTS body;` | @schema-designer | None |
| 2 | `database/migrations/V14__create_weekly_routines_table.sql` | Create | Weekly training plan template | @schema-designer | 1 |
| 3 | `database/migrations/V15__create_workouts_table.sql` | Create | Session-level workout log | @schema-designer | 1 |
| 4 | `database/migrations/V16__create_personal_records_table.sql` | Create | Append-only PR history | @schema-designer | 1 |
| 5 | `database/migrations/V17__create_meals_table.sql` | Create | Per-meal nutrition log | @schema-designer | 1 |
| 6 | `database/migrations/V18__create_water_intake_table.sql` | Create | Daily water running total | @schema-designer | 1 |
| 7 | `database/migrations/V19__create_body_metrics_table.sql` | Create | Weight/height/body-fat history | @schema-designer | 1 |
| 8 | `database/migrations/V20__create_sleep_logs_table.sql` | Create | Bed/wake times, generated duration | @schema-designer | 1 |

**Total Files:** 8

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|-----------------|-----------------|
| @schema-designer | 1–8 (all 8 migration files) | Same agent used for the prior `POSTGRESQL_DATABASE_MIGRATION` feature — `kb_domains: [data-modeling, sql-patterns, data-quality]` matches DEFINE's identified KB domain (`data-modeling`) exactly; description covers "schema evolution" and "modeling decisions," the closest specialist to DDL authoring in this repo |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: KB domain overlap (primary signal, since this is pure SQL/DDL work with no app code)

---

## Code Patterns

### Pattern 1: Schema creation (`V13`)

```sql
------------------------------------------------------------
-- CREATE BODY SCHEMA
------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS body;
```

### Pattern 2: Weekly routine template with day-of-week + uniqueness (`V14`)

```sql
------------------------------------------------------------
-- WEEKLY_ROUTINES TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE body.weekly_routines (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    day_of_week   SMALLINT NOT NULL,
    routine_name  VARCHAR(100) NOT NULL,
    description   VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_weekly_routines_day_of_week CHECK (day_of_week BETWEEN 0 AND 6),
    CONSTRAINT uq_weekly_routines_user_day UNIQUE (user_id, day_of_week)
);

COMMENT ON TABLE body.weekly_routines IS 'Reusable weekly training plan template: what is normally planned for each day of the week, one row per user per day.';

COMMENT ON COLUMN body.weekly_routines.id IS 'Unique identifier for each weekly routine entry (primary key).';
COMMENT ON COLUMN body.weekly_routines.user_id IS 'References the user who owns this routine (foreign key to users.id).';
COMMENT ON COLUMN body.weekly_routines.day_of_week IS 'Day of week this routine applies to: 0=Sunday, 1=Monday, ..., 6=Saturday (matches PostgreSQL EXTRACT(DOW) convention).';
COMMENT ON COLUMN body.weekly_routines.routine_name IS 'Name of the planned routine for this day (e.g., Push Day, Rest Day, Leg Day).';
COMMENT ON COLUMN body.weekly_routines.description IS 'Optional details about what this routine involves.';
COMMENT ON COLUMN body.weekly_routines.status IS 'Status flag (1=Active, 0=Deleted).';
COMMENT ON COLUMN body.weekly_routines.created_at IS 'Timestamp when this routine entry was created.';
```

### Pattern 3: Sleep logs with generated column + check constraint (`V20`, verified live)

```sql
------------------------------------------------------------
-- SLEEP_LOGS TABLE DEFINITION
------------------------------------------------------------
CREATE TABLE body.sleep_logs (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id       INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    bed_time      TIMESTAMPTZ NOT NULL,
    wake_time     TIMESTAMPTZ NOT NULL,
    total_hours   NUMERIC(4,2) GENERATED ALWAYS AS (
                      ROUND(EXTRACT(EPOCH FROM (wake_time - bed_time)) / 3600.0, 2)
                  ) STORED,
    notes         VARCHAR(500),
    status        SMALLINT NOT NULL DEFAULT 1,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_sleep_logs_times CHECK (wake_time > bed_time)
);

COMMENT ON TABLE body.sleep_logs IS 'Nightly sleep log: bed time, wake time, and total hours slept (computed automatically, cannot be set directly).';

COMMENT ON COLUMN body.sleep_logs.id IS 'Unique identifier for each sleep log entry (primary key).';
COMMENT ON COLUMN body.sleep_logs.user_id IS 'References the user who owns this sleep log (foreign key to users.id).';
COMMENT ON COLUMN body.sleep_logs.bed_time IS 'Date and time the user went to bed.';
COMMENT ON COLUMN body.sleep_logs.wake_time IS 'Date and time the user woke up. Must be after bed_time (enforced by check constraint), correctly handles sessions that cross midnight since both are full timestamps.';
COMMENT ON COLUMN body.sleep_logs.total_hours IS 'Total hours slept, automatically computed from wake_time minus bed_time. Cannot be written directly (generated column).';
COMMENT ON COLUMN body.sleep_logs.notes IS 'Optional notes about sleep quality or disturbances.';
COMMENT ON COLUMN body.sleep_logs.status IS 'Status flag (1=Active, 0=Deleted).';
COMMENT ON COLUMN body.sleep_logs.created_at IS 'Timestamp when this sleep log entry was created.';
```

### Pattern 4: Remaining 4 tables — same conventions, summarized

All follow the identical style shown above (`INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY`, `user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE`, `status SMALLINT NOT NULL DEFAULT 1`, `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, full `COMMENT ON` coverage). Only the distinguishing columns are shown:

| Table | Distinguishing Columns |
|-------|------------------------|
| `body.workouts` | `workout_date DATE NOT NULL`, `routine_name VARCHAR(100) NOT NULL`, `duration_minutes INT`, `calories_burned NUMERIC(8,2)`, `notes VARCHAR(500)` |
| `body.personal_records` | `exercise_name VARCHAR(100) NOT NULL`, `metric_type VARCHAR(50) NOT NULL` (e.g. "Max Weight", "Max Reps", "Best Time"), `value NUMERIC(10,2) NOT NULL`, `unit VARCHAR(20) NOT NULL` (e.g. "kg", "reps", "seconds"), `achieved_date DATE NOT NULL`, `notes VARCHAR(500)` |
| `body.meals` | `meal_date DATE NOT NULL`, `meal_type VARCHAR(50) NOT NULL` (e.g. "Breakfast", "Lunch", "Dinner", "Snack"), `description VARCHAR(500)`, `calories NUMERIC(8,2) NOT NULL`, `protein_grams NUMERIC(6,2)`, `carbs_grams NUMERIC(6,2)`, `fat_grams NUMERIC(6,2)` |
| `body.water_intake` | `intake_date DATE NOT NULL`, `amount_ml INT NOT NULL DEFAULT 0`, `CONSTRAINT uq_water_intake_user_date UNIQUE (user_id, intake_date)` |
| `body.body_metrics` | `measured_date DATE NOT NULL`, `weight_kg NUMERIC(5,2)`, `height_cm NUMERIC(5,2)`, `body_fat_percent NUMERIC(4,2)`, `notes VARCHAR(500)`, `CONSTRAINT uq_body_metrics_user_date UNIQUE (user_id, measured_date)` |

---

## Data Flow

```text
1. Flyway detects 8 new pending migrations (V13-V20) on `docker compose up flyway`
   │
   ▼
2. V13 creates the `body` schema (idempotent via IF NOT EXISTS)
   │
   ▼
3. V14-V20 each create one table, in dependency order (all depend only on V13
   and the pre-existing `users` table — no inter-table dependencies among
   the 7 new tables themselves)
   │
   ▼
4. Each table's FK constraint is validated against the existing `users` table
   at creation time (trivially satisfied - FK constraints don't require
   existing data, only that the referenced table/column exists)
   │
   ▼
5. flyway_schema_history records 8 new successful entries, schema now at V20
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-----------------|------------------|
| PostgreSQL (`database/`, already running) | Flyway-applied DDL over the existing connection | Existing `postgres` superuser credentials, no new user needed |

No new external systems — this feature adds tables to the already-running database only.

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Migration apply | All 8 new files, fresh state (already at V12) | `database/migrations/V13-V20*.sql` | `docker compose up flyway` + `docker compose run --rm findatabase info` | AT-001: 8/8 report "Success" |
| FK enforcement | `body.*` tables | Manual `psql` insert with nonexistent `user_id` | `psql` | AT-002: insert fails with FK violation (already verified live during Design for the pattern) |
| Generated column correctness | `body.sleep_logs` | Manual `psql` insert + read-back, plus a direct-write attempt | `psql` | AT-003: correct computed value; direct write rejected (already verified live during Design) |
| Lint | All 8 new files | `database/.sqlfluff` (already `dialect = postgres`) | `sqlfluff lint database/migrations/` | AT-004: 0 violations |
| Idempotency | `V13`'s `CREATE SCHEMA IF NOT EXISTS` | Full migration set re-run | `docker compose run --rm findatabase migrate` (twice) | AT-005: second run reports "up to date" |
| Documentation completeness | All 8 tables | `psql \d+ body.<table>` for each | `psql`, manual inspection | AT-006: every column shows a non-empty description |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|--------------------|--------|
| FK violation (nonexistent `user_id`) | PostgreSQL rejects the insert/update at the constraint level — verified live | No — caller must supply a valid `user_id` |
| Direct write to `sleep_logs.total_hours` | PostgreSQL rejects with "cannot insert a non-DEFAULT value into column" — verified live | No — caller must not set this column |
| Duplicate `(user_id, day_of_week)` / `(user_id, intake_date)` / `(user_id, measured_date)` | `UNIQUE` constraint violation | No — caller must `UPDATE` the existing row or use `ON CONFLICT ... DO UPDATE` |
| `wake_time <= bed_time` | `CHECK` constraint violation | No — caller must supply a valid time range |
| Migration syntax error | Flyway aborts the run; PostgreSQL DDL is transactional per statement | No — fix and rerun `migrate` |

---

## Configuration

No new configuration — this feature uses the existing `database/.env`/`docker-compose.yml`/`flyway.toml` setup unchanged. `FLYWAY_SCHEMAS` (currently `public,finance,plan,investment,reporting`) does not need `body` added — Flyway only needs the *default* schema and migration-history-table schema configured; `CREATE SCHEMA IF NOT EXISTS body;` in `V13` creates the schema itself regardless of `FLYWAY_SCHEMAS`, exactly as `V1` already does for `finance`/`plan`/`investment`/`reporting` today.

---

## Security Considerations

- `ON DELETE CASCADE` on every `body.*` FK means deleting a `users` row now cascades to delete all of that user's Body data (see Decision 2's Consequences) — a real behavior change from existing finance tables, which have no FK and would silently orphan rows instead. This is more correct (no orphaned data) but means user deletion is more destructive than it is today for finance data — worth the future API-layer feature being aware of this when implementing account deletion.
- No new secrets, no new credentials, no new network exposure — pure DDL against the already-running, already-secured local Postgres instance.

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | Flyway console output during migration, unchanged from existing setup |
| Metrics | N/A — schema-only change; once the API layer exists (future feature), `postgres_exporter`'s existing `pg_stat_user_tables` collector will automatically pick up the new tables with zero configuration (already confirmed working for `finance` tables in the `OBSERVABILITY_STACK` feature) |
| Tracing | N/A |

---

## Pipeline Architecture (if applicable)

Not applicable. DEFINE confirmed this is an OLTP application schema addition, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | design-agent | Initial version, derived from `DEFINE_BODY_MODULE_DATABASE.md`. Generated-column and FK-constraint syntax verified live against the running Postgres instance (not assumed). Corrected DEFINE's file count (7→8, schema creation gets its own migration per `V1`'s precedent). Extended `UNIQUE` constraint pattern to `water_intake`/`body_metrics` beyond DEFINE's single explicit ask (`weekly_routines`), for consistency with the same validated "one row per day" design intent. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_BODY_MODULE_DATABASE.md`
