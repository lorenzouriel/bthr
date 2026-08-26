# DEFINE: Mind Module (Meditation & Journaling)

> Add a new `mind` Postgres schema (meditation sessions, journal entries) plus its full REST API layer, built together in one pass and mirroring the Body module's proven schema and API conventions exactly.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | MIND_MODULE |
| **Date** | 2026-08-25 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Designed) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

FinPulse users can track finances and physical wellness (training, nutrition, sleep via the `body` schema), but there is no way to log meditation sessions or journal entries — no table, no API, no way to persist mental-wellness data at all.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse user tracking personal wellness | End user of the eventual Mind module UI | Wants to log meditation sessions and journal entries alongside their existing finance/body tracking, in one app, without external tools — but no schema or API exists to store this data |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Create `mind` Postgres schema via a numbered migration (`CREATE SCHEMA IF NOT EXISTS mind;`), following the exact pattern of `V13__create_body_schema.sql` |
| **MUST** | Create `mind.meditation_sessions` table: `id`, `user_id` (FK → users, cascade delete), `session_date` (DATE), `duration_minutes` (SMALLINT, `CHECK > 0`), `meditation_type` (VARCHAR(50)), `mood_before`/`mood_after` (SMALLINT, nullable, `CHECK BETWEEN 1 AND 5`), `notes` (VARCHAR(500), nullable), `status` (SMALLINT, soft-delete), `created_at` (TIMESTAMPTZ) |
| **MUST** | Create `mind.journal_entries` table: `id`, `user_id` (FK → users, cascade delete), `entry_date` (DATE), `title` (VARCHAR(200), nullable), `content` (TEXT, required), `mood` (SMALLINT, nullable, `CHECK BETWEEN 1 AND 5`), `category` (VARCHAR(50), nullable), `status` (SMALLINT, soft-delete), `created_at` (TIMESTAMPTZ) |
| **MUST** | Every table and column gets a real `COMMENT ON TABLE`/`COMMENT ON COLUMN` description, matching the Body module documentation standard |
| **MUST** | Both migrations live-verified against a real running Postgres instance: FK rejection, CHECK constraint rejection (invalid mood, non-positive duration), successful valid inserts, idempotent re-run |
| **MUST** | Create `MeditationSessionsController` — full CRUD at `/api/users/{userId}/mind/meditation-sessions` |
| **MUST** | Create `JournalEntriesController` — full CRUD at `/api/users/{userId}/mind/journal-entries` |
| **MUST** | Every controller follows the existing convention exactly: `[ApiController]`, `[Authorize]` (no `[RequiresPlan]`), a `GetCurrentUserId()` ownership check on every action, returning `Forbid()` on mismatch |
| **MUST** | Each table gets a matching EF Core model (`[Table]`/`[Column]` attributes) and a `DbSet<T>` registered in `ApplicationDbContext` |
| **MUST** | Each resource gets Request/Response DTOs following `GoalDTOs`' shape (`Create{X}Request`, `Update{X}Request`, `{X}Response`, DataAnnotations only — no duplicated DB-constraint validation) |
| **MUST** | Soft-delete via `Status` (1=Active, 0=Deleted), matching every existing resource — `DELETE` sets `Status = 0`, never a hard delete |
| **MUST** | Both resources are live-verified end-to-end (Swagger/curl) against the running API + Postgres, not just compiled |
| **SHOULD** | New xUnit test files for both new controllers/services in `FinPulse.Tests`, following the now-fixed, passing baseline (306/306) established after the Body module's test-infrastructure repair |
| **SHOULD** | Optional `start_date`/`end_date` query filtering on both resources (they both have a natural date field), mirroring `GoalsController`'s and the Body module's existing filter pattern |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] `mind` schema plus `meditation_sessions` and `journal_entries` tables exist in a live Postgres instance after running the new migrations
- [ ] A live insert with an invalid `mood_before`/`mood_after`/`mood` (outside 1–5) is rejected by the CHECK constraint
- [ ] A live insert with `duration_minutes <= 0` is rejected by the CHECK constraint
- [ ] A live insert with a nonexistent `user_id` is rejected by the FK constraint
- [ ] Re-running the migration tool reports "up to date, no migration necessary" (idempotency)
- [ ] Both new Controllers return `403 Forbidden` when the route `userId` doesn't match the authenticated user's claim
- [ ] Both resources expose full CRUD (`GET` list, `GET` by id, `POST`, `PUT`, `DELETE`)
- [ ] Both new EF Core models are mapped 1:1 to their live table schema (column names, types, nullability match the migrations exactly)
- [ ] `ApplicationDbContext` has 2 new `DbSet<T>` properties
- [ ] Each resource has `Create{X}Request`/`Update{X}Request`/`{X}Response` DTOs using only `DataAnnotations`
- [ ] A live `DELETE` sets `Status = 0` in the DB and the row no longer appears in a subsequent `GET`, but still exists in the table
- [ ] Full CRUD lifecycle (`POST` → `GET` → `PUT` → `GET` → `DELETE` → `GET`) succeeds live against the running API + Postgres for both resources
- [ ] `dotnet build` succeeds with 0 errors for `FinPulse.Api`

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Schema and constraints live-verified | A running Postgres instance with migrations up to V20 applied | The new `mind` schema migrations are run | `mind.meditation_sessions` and `mind.journal_entries` exist with all documented columns, FK, and CHECK constraints |
| AT-002 | CHECK constraints reject invalid data | The `mind.journal_entries` table exists | An `INSERT` is attempted with `mood = 9` | Postgres rejects the insert with a CHECK constraint violation |
| AT-003 | FK constraint rejects orphan rows | The `mind.meditation_sessions` table exists | An `INSERT` is attempted with a `user_id` that doesn't exist in `users` | Postgres rejects the insert with a FK violation |
| AT-004 | Idempotent migration re-run | Migrations already applied once | The migration tool is re-run | It reports "up to date, no migration necessary" — no duplicate schema objects created |
| AT-005 | Full CRUD lifecycle on both resources | An authenticated user with no existing `mind.*` rows | `POST` a new meditation session (and separately a journal entry), `GET` it back, `PUT` an update, `GET` again, `DELETE` it, `GET` again | Each step returns the expected status (201/200/200/200/200/200) and the final `GET` list no longer includes the deleted row |
| AT-006 | Ownership enforcement | Two authenticated users, A and B | User A calls any Mind endpoint with User B's `userId` in the route | The API returns `403 Forbidden` without touching the database |
| AT-007 | Soft delete, not hard delete | An existing `mind.journal_entries` row | `DELETE /api/users/{userId}/mind/journal-entries/{id}` is called, then the row is queried directly via `psql` | The row still exists in the table with `status = 0`; it no longer appears in the `GET` list endpoint |
| AT-008 | Nullable mood fields accepted | The `mind.meditation_sessions` table exists | A `POST` is made with `mood_before`/`mood_after` omitted | The insert succeeds; the response shows `null` for the omitted mood fields |
| AT-009 | Full build succeeds | Both Controllers/Services/DTOs/Models are added | `dotnet build` is run on `FinPulse.Api` | 0 compile errors |

---

## Out of Scope

Explicitly NOT included in this feature:

- **Aggregation/analytics endpoints** (e.g., "average mood this week", "total meditation minutes this month") — raw CRUD only.
- **Reminders/notifications** for meditation or journaling — no notification infrastructure exists in this codebase.
- **Streak/gamification tracking** — a derived/computed concept, not raw data.
- **Audio/media attachments on meditation sessions** — no file/blob storage infrastructure exists in this codebase.
- **Journal entry sharing between users** — every existing resource in this codebase is single-owner; sharing is a new cross-cutting concern.
- **Full-text search on journal content** — no search infrastructure (e.g., Postgres `tsvector`) exists anywhere in this codebase yet.
- **Tag arrays / multi-tag journal entries** — a single `category VARCHAR(50)` column is used instead, matching the existing single-value pattern (`meal_type`).
- **Plan-tier gating (`[RequiresPlan]`)** — Mind endpoints are open to all authenticated users, unlike Goals/Investments.
- **A generic/shared `BaseCrudController<T>`/`BaseCrudService<T>` abstraction** — each resource gets its own independent file set, matching all 13 existing resources.
- **Duplicated DB-constraint validation in DTOs** — the DB remains the sole source of truth for business rules.
- **Any frontend/UI work** — API and schema only.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Must match existing schema conventions exactly (`id GENERATED ALWAYS AS IDENTITY PRIMARY KEY`, `user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE`, `status SMALLINT NOT NULL DEFAULT 1`, `created_at TIMESTAMPTZ NOT NULL DEFAULT now()`, full `COMMENT ON` documentation) | Design must not deviate from the Body module's migration file shape |
| Technical | Must match existing API conventions exactly (route nesting under `/api/users/{userId}/mind/...`, `GetCurrentUserId()` ownership check, DTO/DataAnnotations style, soft-delete via `Status`, no shared generic base) | Design must not introduce a different controller/service style for this module |
| Technical | New tables live in a new `mind` schema, not inside `body` | Design must add a new schema-creation migration before the table migrations |
| Scope | No aggregation/computed endpoints this pass | Design must limit each controller to CRUD actions mapped 1:1 to its table |
| Scope | Mood fields (`mood_before`, `mood_after`, `mood`) are nullable, not required | Design must not add `NOT NULL` or DTO-level "required" validation on these fields |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `database/migrations/` (new `V21__create_mind_schema.sql`, `V22__create_meditation_sessions_table.sql`, `V23__create_journal_entries_table.sql` — exact numbering to be confirmed in Design against the current migration head) and `api/FinPulse.Api/{Controllers,Services,DTOs,Models}/` (2 new files per folder, 8 total), plus `Data/ApplicationDbContext.cs` gets 2 new `DbSet<T>` registrations and `Models/User.cs` gets 2 new navigation properties; `FinPulse.Tests/` gets new test files | No changes to any existing migration, controller, service, DTO, or model file |
| **KB Domains** | None — the KB is data-engineering-focused (dbt, Spark, Airflow, data-modeling, etc.); no domain covers ASP.NET Core/EF Core/Postgres REST API design | Confidence 0.80 (codebase-pattern-only) — `body.meals`/`MealsController`/`MealDTOs`/`MealService`/`Meal.cs` and `body.sleep_logs`'s nullable-column handling are the ground-truth patterns for Design to follow |
| **IaC Impact** | None | No new services, no docker-compose changes; the existing API container and Postgres connection are reused as-is |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is a standard OLTP schema plus REST API layer, not a data pipeline. No source-system ingestion, ETL, or analytics-layer concerns apply.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | Nullable `SMALLINT` columns with `CHECK (col BETWEEN 1 AND 5)` correctly allow `NULL` in Postgres (CHECK constraints pass on `NULL` by SQL three-valued-logic semantics, only enforcing the range when a value is present) | If wrong, Design would need `CHECK (col IS NULL OR col BETWEEN 1 AND 5)` explicit null-handling | [ ] |
| A-002 | EF Core maps nullable Postgres `SMALLINT` columns to nullable C# `short?` without special configuration, consistent with how existing nullable columns (e.g., `body.meals.protein_grams`) are already mapped | Design would need explicit `[Column(TypeName=...)]` overrides | [ ] |
| A-003 | `FinPulse.Tests` remains in its fixed, passing state (306/306, confirmed after the post-Body-module repair) and adding new test files won't reintroduce the prior compile-error class | If wrong, new Mind module tests could fail to compile alongside the rest of the suite | [x] Confirmed — `dotnet test` was run and passed 306/306 after the most recent fix pass, immediately before this brainstorm |
| A-004 | The next available migration version numbers are V21+ (last applied is V20, `sleep_logs`) | If a migration was added elsewhere between V20 and now, Design must re-check the actual head before numbering | [ ] |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific and verifiable — no meditation/journal schema or API exists anywhere in the codebase, confirmed by grep |
| Users | 2 | One clear persona with a concrete pain point, but a single generic user type rather than multiple distinct personas |
| Goals | 3 | MoSCoW-prioritized, each traceable to one of the 6 validated brainstorm discovery answers plus 2 validation checkpoints |
| Success | 3 | Every criterion is testable pass/fail (constraints reject invalid data, CRUD lifecycle works live, ownership enforced, soft-delete verified, build succeeds) |
| Scope | 3 | Eleven explicit out-of-scope items, each traced back to a brainstorm YAGNI decision or explicit approach rejection |
| **Total** | **14/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design. (Assumption A-004 — exact next migration version numbers — should be re-confirmed by Design against the current `database/migrations/` directory state at design time, since this DEFINE was written from a point-in-time observation of V1–V20.)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | define-agent | Initial version, derived from `BRAINSTORM_MIND_MODULE.md` |

---

## Next Step

**Ready for:** `/design .claude/sdd/features/DEFINE_MIND_MODULE.md`
