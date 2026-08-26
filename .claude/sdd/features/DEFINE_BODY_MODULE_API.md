# DEFINE: Body Module API

> Add the REST API layer (Controllers, Services, DTOs, EF Core Models) for the already-built, live-verified `body` schema — 7 resources for Training, Nutrition, and Sleep tracking.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_API |
| **Date** | 2026-08-25 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

The `body` schema and its 7 tables (`weekly_routines`, `workouts`, `personal_records`, `meals`, `water_intake`, `body_metrics`, `sleep_logs`) are live-verified in Postgres, but no client can read from or write to them — there is no Controller, Service, DTO, or EF Core Model for any of the 7 tables, even though the database was deliberately built first with the API as its explicit next step.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| FinPulse user tracking their fitness/health | End user of the eventual Body module UI | The database can store their Training/Nutrition/Sleep data, but nothing can create, read, update, or delete it yet — no API surface exists for a future frontend to build against |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Create `WeeklyRoutinesController` — full CRUD at `/api/users/{userId}/body/weekly-routines` |
| **MUST** | Create `WorkoutsController` — full CRUD at `/api/users/{userId}/body/workouts` |
| **MUST** | Create `PersonalRecordsController` — create + read only (`GET`, `POST`) at `/api/users/{userId}/body/personal-records`, no `PUT`/`DELETE` |
| **MUST** | Create `MealsController` — full CRUD at `/api/users/{userId}/body/meals` |
| **MUST** | Create `WaterIntakeController` — full CRUD at `/api/users/{userId}/body/water-intake` |
| **MUST** | Create `BodyMetricsController` — full CRUD at `/api/users/{userId}/body/body-metrics` |
| **MUST** | Create `SleepLogsController` — full CRUD at `/api/users/{userId}/body/sleep-logs` |
| **MUST** | Every controller follows the existing convention exactly: `[ApiController]`, `[Authorize]` (no `[RequiresPlan]`), a `GetCurrentUserId()` ownership check on every action, returning `Forbid()` on mismatch |
| **MUST** | Every table gets a matching EF Core model (`[Table]`/`[Column]` attributes) and a `DbSet<T>` registered in `ApplicationDbContext` |
| **MUST** | Every resource gets Request/Response DTOs following `GoalDTOs`' shape (`Create{X}Request`, `Update{X}Request`, `{X}Response`, DataAnnotations only — no duplicated DB-constraint validation) |
| **MUST** | `sleep_logs.total_hours` (generated column) appears only in `SleepLogResponse`, never in any request DTO |
| **MUST** | Soft-delete via `Status` (1=Active, 0=Deleted), matching every existing resource — `DELETE` sets `Status = 0`, never a hard delete |
| **MUST** | All 7 resources are live-verified end-to-end (Swagger/curl) against the running API + Postgres, not just compiled |
| **SHOULD** | New xUnit test files exist in `FinPulse.Tests` for the 7 new controllers/services (acknowledged: won't compile/run until the project's 53 pre-existing, unrelated errors are separately fixed) |
| **SHOULD** | Optional `start_date`/`end_date` query filtering on resources with a natural date field (`workouts`, `meals`, `water_intake`, `body_metrics`, `sleep_logs`, `personal_records`), mirroring `GoalsController`'s existing filter pattern |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] 7 new Controllers exist, each returning `403 Forbidden` when the route `userId` doesn't match the authenticated user's claim
- [ ] 6 resources (all except `personal_records`) expose `GET`, `POST`, `PUT`, `DELETE`; `personal_records` exposes only `GET`, `POST`
- [ ] 7 new EF Core models exist, each mapped 1:1 to its `body.*` table's live schema (column names, types, nullability match V14–V20 exactly)
- [ ] `ApplicationDbContext` has 7 new `DbSet<T>` properties, one per model
- [ ] Every resource has `Create{X}Request`/`Update{X}Request`/`{X}Response` DTOs (personal_records has no `Update` DTO), using only `DataAnnotations`
- [ ] `SleepLogResponse.TotalHours` is present and populated; no request DTO for `sleep_logs` contains a `TotalHours` field
- [ ] A live `POST` to each of the 6 full-CRUD resources with an invalid/foreign `user_id` in the route (not matching the JWT claim) returns `403`, not a DB-level error
- [ ] A live `DELETE` on any resource sets `Status = 0` in the DB and the row no longer appears in a subsequent `GET`, but still exists in the table
- [ ] Full CRUD lifecycle (`POST` → `GET` → `PUT` → `GET` → `DELETE` → `GET`) succeeds live against the running API + Postgres for all 6 full-CRUD resources; `POST` → `GET` succeeds for `personal_records`
- [ ] `dotnet build` succeeds with 0 errors for `FinPulse.Api` (the new code compiles cleanly, independent of `FinPulse.Tests`' pre-existing state)

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Full CRUD lifecycle on a standard resource | An authenticated user with no existing `body.workouts` rows | `POST` a new workout, `GET` it back, `PUT` an update, `GET` again, `DELETE` it, `GET` again | Each step returns the expected status (201/200/200/200/200/200) and the final `GET` list no longer includes the deleted workout |
| AT-002 | Ownership enforcement | Two authenticated users, A and B | User A calls any Body endpoint with User B's `userId` in the route | The API returns `403 Forbidden` without touching the database, matching `GoalsController`'s existing behavior |
| AT-003 | `personal_records` has no Update/Delete | The `body.personal_records` table exists with a record | A client sends `PUT` or `DELETE` to `/api/users/{userId}/body/personal-records/{id}` | The API returns `404 Not Found` (no such route exists), confirming those actions were never implemented |
| AT-004 | Generated column is response-only | A `POST` to `/api/users/{userId}/body/sleep-logs` with a JSON body | The request DTO is inspected/serialized | `CreateSleepLogRequest` has no `TotalHours` property; the resulting `SleepLogResponse` after creation has a correctly computed `TotalHours` |
| AT-005 | Soft delete, not hard delete | An existing `body.meals` row | `DELETE /api/users/{userId}/body/meals/{id}` is called, then the row is queried directly via `psql` | The row still exists in the table with `status = 0`; it no longer appears in the `GET` list endpoint (which filters `status != 0`) |
| AT-006 | DB constraint violations surface, not swallowed | The `body.weekly_routines` UNIQUE(user_id, day_of_week) constraint | A `POST` is made for a day-of-week that already has a routine for that user | The API returns an error reflecting the constraint violation (not a silent 200 or a generic 500 with no detail), matching how existing controllers handle DB errors |
| AT-007 | Full build succeeds | All 7 Controllers/Services/DTOs/Models are added | `dotnet build` is run on `FinPulse.Api` | 0 compile errors |

---

## Out of Scope

Explicitly NOT included in this feature:

- **Any changes to the `body` schema or its migrations** — V13–V20 are final, shipped, and live-verified; this feature is pure API code on top of them.
- **Daily nutrition summary / weekly training overview (computed/aggregation) endpoints** — raw CRUD only, mirroring the DB module's own deferral of computed daily totals.
- **Plan-tier gating (`[RequiresPlan]`)** — Body endpoints are open to all authenticated users, unlike Goals/Investments.
- **A generic/shared `BaseCrudController<T>`/`BaseCrudService<T>` abstraction** — each of the 7 resources gets its own independent file set, matching all 6 existing resources.
- **Duplicated DB-constraint validation in DTOs** (e.g., re-checking `day_of_week` range or `wake_time > bed_time` in C#) — the DB remains the sole source of truth for business rules, matching every existing DTO.
- **`GET /weekly-routines` response padding** to always return 7 entries — returns only whatever rows exist (0–7).
- **Fixing `FinPulse.Tests`' 53 pre-existing, unrelated compile errors** — explicitly deferred; new Body module tests are written despite this and will not run until that separate repair happens.
- **Any frontend/UI work** — API only.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Must match existing API conventions exactly (route nesting under `/api/users/{userId}/...`, `GetCurrentUserId()` ownership check, DTO/DataAnnotations style, soft-delete via `Status`, no shared generic base) | Design must not introduce a different controller/service style for this module |
| Technical | No changes to `database/migrations/` — the `body` schema is complete and out of scope | Design must treat V13–V20 as a fixed, read-only contract |
| Technical | No `[RequiresPlan]` attribute on any Body controller | Design must not apply plan-gating middleware to these routes |
| Scope | No aggregation/computed endpoints this pass | Design must limit each controller to CRUD actions mapped 1:1 to its table |
| Scope | `FinPulse.Tests` remains in its current (broken) state aside from additive new test files | Design must not attempt to fix the 53 pre-existing errors as part of this feature |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `api/FinPulse.Api/{Controllers,Services,DTOs,Models}/` — 7 new files per folder (28 total), plus `Data/ApplicationDbContext.cs` gets 7 new `DbSet<T>` registrations; `FinPulse.Tests/` gets new test files | No changes to any existing controller/service/DTO/model file |
| **KB Domains** | None — the KB is data-engineering-focused (dbt, Spark, Airflow, data-modeling, etc.); no domain covers ASP.NET Core/EF Core REST API design | Confidence 0.80 (codebase-pattern-only) — `GoalsController`/`GoalDTOs`/`GoalService`/`Goal.cs` are the ground-truth pattern for Design to follow |
| **IaC Impact** | None | No new services, no docker-compose changes; the existing API container and Postgres connection are reused as-is |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is a standard REST API layer over an existing OLTP schema, not a data pipeline. No source-system ingestion, ETL, or analytics-layer concerns apply.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | EF Core can map a Postgres `GENERATED ALWAYS AS (...) STORED` column (`sleep_logs.total_hours`) as a read-only property without EF attempting to write to it on `SaveChanges()` | Design would need an explicit `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` or a raw-SQL/ignored-property workaround | [ ] |
| A-002 | Returning raw Postgres constraint-violation exceptions (e.g., `UNIQUE`, `CHECK` violations) to the client via the existing unhandled-exception behavior is acceptable, matching how existing controllers handle DB errors today (no custom exception middleware exists) | Would need Design to add explicit try/catch + friendly-error mapping, a new pattern not used elsewhere in the API | [x] Confirmed acceptable via brainstorm discovery (Q7 — DB remains source of truth, no duplicate validation layer) |
| A-003 | `FinPulse.Tests` project references (xUnit, Moq/test doubles if any) are still resolvable enough to add new test files as valid C# source, even though the project doesn't currently compile as a whole | If the 53 errors are severe enough (e.g., missing project references), new test files may not even be parseable/addable in a meaningful way | [ ] |
| A-004 | `PersonalRecordsController` omitting `PUT`/`DELETE` entirely (rather than implementing them and returning `405`/`403`) is the correct way to express "no update/delete allowed," consistent with ASP.NET routing conventions | If wrong, tests/clients expecting a `405 Method Not Allowed` instead of `404 Not Found` would need Design to add explicit disabled-action handlers | [x] Confirmed via brainstorm — AT-003 above treats "no such route" (404) as the expected behavior |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific, verifiable — the schema exists and is live, but zero API surface exists on top of it |
| Users | 2 | One clear persona with a concrete pain point, but a single generic user type rather than multiple distinct personas |
| Goals | 3 | MoSCoW-prioritized, each traceable to one of 9 validated brainstorm decisions |
| Success | 3 | Every criterion is testable pass/fail (build succeeds, CRUD lifecycle works live, ownership enforced, soft-delete verified, generated column response-only) |
| Scope | 3 | Seven explicit out-of-scope items, each with a clear rationale traced back to a brainstorm YAGNI decision |
| **Total** | **14/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design. (Assumptions A-001 and A-003 are flagged for Design to resolve/validate — particularly A-001, since EF Core's handling of Postgres generated columns should be verified live during Design, the same discipline used for `sleep_logs.total_hours` during the DB build.)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | define-agent | Initial version, derived from `BRAINSTORM_BODY_MODULE_API.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_BODY_MODULE_API.md`
