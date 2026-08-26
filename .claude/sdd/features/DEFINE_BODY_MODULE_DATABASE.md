# DEFINE: Body Module Database

> Add a new `body` schema to the FinPulse database with 7 tables covering Training, Nutrition, and Sleep tracking — database only, API is a separate follow-up feature.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_DATABASE |
| **Date** | 2026-08-25 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

FinPulse's database has no way to store Training, Nutrition, or Sleep data — the app currently tracks only financial data — even though the user wants a genuinely new, distinct "Body" module built with the same database-first discipline already proven in the recent PostgreSQL migration.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse user tracking fitness/health | End user of the eventual Body module | No database structure exists to persist workouts, meals, water intake, body metrics, or sleep — there is nothing yet for a future API or UI to build against |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Create a new `body` schema (`CREATE SCHEMA IF NOT EXISTS body;`) |
| **MUST** | Create `body.weekly_routines` — a reusable weekly training plan template (one row per user per day-of-week) |
| **MUST** | Create `body.workouts` — session-level logged training sessions (date, routine label, duration, calories burned, notes) |
| **MUST** | Create `body.personal_records` — append-only history of best results per exercise (new row each time a record is broken) |
| **MUST** | Create `body.meals` — per-meal nutrition log (calories, protein, carbs, fat, description, meal type) |
| **MUST** | Create `body.water_intake` — one row per user per day, running total in ml |
| **MUST** | Create `body.body_metrics` — history of weight/height/body fat over time (one row per date logged) |
| **MUST** | Create `body.sleep_logs` — `bed_time`/`wake_time` stored, total sleep hours as a PostgreSQL generated column |
| **MUST** | Every `body.*` table has a real `FOREIGN KEY (user_id) REFERENCES users(id)` constraint |
| **MUST** | Every table and column documented via native `COMMENT ON`, matching existing migration style |
| **SHOULD** | `sqlfluff lint` (dialect `postgres`, already configured) returns 0 violations against the new migration files |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] All 7 new migration files (`V13`–`V19`, sequential after the existing `V1`–`V8`, `V12`) apply cleanly via `docker compose up flyway` against the live local Postgres instance, with 0 errors
- [ ] `\dn` in `psql` shows the `body` schema exists alongside `finance`/`plan`/`investment`/`reporting`/`public`
- [ ] Inserting a `body.workouts` (or any `body.*` table) row with a `user_id` that doesn't exist in `users` fails with a foreign-key-violation error
- [ ] Inserting a `body.sleep_logs` row with `bed_time`/`wake_time` set produces a correctly computed total-hours value with no direct write to that column possible
- [ ] `sqlfluff lint database/migrations/` (dialect `postgres`) reports 0 violations across the 7 new files
- [ ] Re-running `docker compose run --rm findatabase migrate` against an already-migrated database is a no-op (idempotent), matching the behavior already proven for `V1`'s schema creation

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Fresh migration run applies all 7 tables | A local Postgres instance already at migration `V12` (current state) | `docker compose up flyway` is run | All 7 new migrations apply successfully; `flyway_schema_history` shows 7 new successful entries; `body` schema and all 7 tables exist |
| AT-002 | FK constraint enforcement | The `body.workouts` table exists | An `INSERT` is attempted with `user_id = 999999` (nonexistent) | The insert fails with a foreign-key-violation error, not a silent success |
| AT-003 | Sleep generated column correctness | A `body.sleep_logs` row is inserted with `bed_time = '2026-08-24 23:00:00+00'` and `wake_time = '2026-08-25 07:00:00+00'` | The row is read back | The computed total-hours value equals 8, and a direct `UPDATE ... SET total_hours = X` fails or is rejected (generated columns cannot be written directly) |
| AT-004 | Lint passes under Postgres dialect | `.sqlfluff` is already set to `dialect = postgres` (from the prior migration) | `sqlfluff lint database/migrations/` is run | 0 violations across all 7 new files |
| AT-005 | Idempotent re-run | All 7 migrations already applied | `docker compose run --rm findatabase migrate` is run again | Flyway reports "up to date, no migration necessary" — no errors, no duplicate schema/table creation attempts |
| AT-006 | Documentation completeness | Any `body.*` table | `psql -c "\d+ body.workouts"` (or equivalent for any of the 7 tables) is run | Every column shows a non-empty `Description` populated via `COMMENT ON COLUMN` |

---

## Out of Scope

Explicitly NOT included in this feature:

- **API layer** (DTOs, controllers, endpoints, EF Core models) — explicitly deferred to a separate follow-up feature, mirroring the database-then-API sequencing already used for the PostgreSQL migration.
- **Exercise catalog / per-set, per-rep workout tracking** — session-level tracking only; a full Strong/Hevy-style exercise-and-set structure was explicitly rejected as a larger, unrequested feature.
- **Food catalog / searchable nutrition database** — meals are logged as free-text description + macros, not looked up from a reference table.
- **User-configurable unit system** — fixed metric units (kg, cm, ml) only; no unit column on any table.
- **Per-drink water logging** — one running-total row per user per day, not per-entry.
- **Separate daily nutrition aggregate table** — daily totals are computed via `SUM(meals)`, not stored redundantly.
- **Weekly routine plan versioning/history** — one active weekly template per user; no history of past plans.
- **Sleep quality, naps, or sleep-stage tracking** — never requested, not invented.
- **Any changes to existing finance tables or schemas** (`public`, `finance`, `plan`, `investment`, `reporting`) — this feature only adds new files.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Must match existing migration conventions exactly (`INT GENERATED ALWAYS AS IDENTITY`, `TIMESTAMPTZ NOT NULL DEFAULT now()`, `SMALLINT` soft-delete `status`, native `COMMENT ON`) | Design must not introduce a different DDL style for this schema |
| Technical | Database-only — no API/DTO/controller work | Design must not touch anything under `api/` |
| Technical | Fixed metric units, no unit-conversion columns | Design must not add a `unit`/`unit_system` column anywhere in this feature |
| Scope | No changes to any existing finance table, schema, or migration file | Design must only add new, sequentially-numbered migration files |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `database/migrations/V13__*.sql` through `V19__*.sql` (7 new sequential files, following the existing `V1`–`V8`, `V12` numbering) | No changes to any existing migration file |
| **KB Domains** | `data-modeling` (general relational schema-design patterns) | Confidence 0.70 — no KB domain covers fitness/health-tracking schemas specifically; the design was fully validated through user discovery in the brainstorm session instead |
| **IaC Impact** | None | Pure DDL added to the already-running Postgres instance via the existing Flyway/`docker-compose.yml` setup — no infrastructure changes needed |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is an OLTP application schema addition, not a data pipeline. No source-system ingestion, ETL, or analytics-layer concerns apply.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | PostgreSQL's `GENERATED ALWAYS AS (...) STORED` computed-column syntax correctly supports an expression like `EXTRACT(EPOCH FROM (wake_time - bed_time)) / 3600` for `sleep_logs.total_hours` | Design would need a trigger-based or application-computed alternative instead of a generated column | [ ] |
| A-002 | Storing `bed_time`/`wake_time` as full `TIMESTAMPTZ` values (not time-of-day-only) correctly handles sleep sessions that cross midnight, with no special-casing needed | Would need explicit date-rollover logic in the schema or a documented convention for callers | [ ] |
| A-003 | `body.personal_records`'s `metric_type` and `exercise_name` are free-text `VARCHAR` (no lookup/enum table), consistent with the "no exercise catalog" YAGNI decision | No validation prevents typos/inconsistent naming across records for what should be the same exercise — acceptable for this pass, revisit if it becomes a real problem | [x] Confirmed intentional via brainstorm YAGNI decision |
| A-004 | `body.weekly_routines` does not need a `UNIQUE(user_id, day_of_week)` constraint, even though the design intent is "at most one row per user per day" | Without the constraint, duplicate/conflicting rows for the same day could be inserted; Design should decide whether to add this constraint explicitly | [ ] |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific, verifiable — no Body-tracking storage exists today |
| Users | 2 | One clear persona with a concrete pain point, but a single generic user type rather than multiple distinct personas |
| Goals | 3 | MoSCoW-prioritized, each traceable to one of 11 validated brainstorm decisions |
| Success | 3 | Every criterion is testable pass/fail (schema exists, FK enforced, generated column correct, lint clean, idempotent, documented) |
| Scope | 3 | Nine explicit out-of-scope items, each with a clear rationale traced back to a brainstorm YAGNI decision |
| **Total** | **14/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design. (Assumption A-004 — whether `weekly_routines` needs a `UNIQUE(user_id, day_of_week)` constraint — is flagged for Design to resolve, not a blocker to starting Design.)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | define-agent | Initial version, derived from `BRAINSTORM_BODY_MODULE_DATABASE.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_BODY_MODULE_DATABASE.md`
