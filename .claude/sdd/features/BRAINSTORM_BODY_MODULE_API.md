# BRAINSTORM: Body Module API

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_API |
| **Date** | 2026-08-25 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "Let's add these changes to the api: [summary of the completed BODY_MODULE_DATABASE build — all 8 migrations V13–V20 live-verified, FK/generated-column/UNIQUE/CHECK constraints and idempotency all confirmed against a real running Postgres instance]." — the explicitly-deferred API follow-up to BODY_MODULE_DATABASE, per that feature's own scope boundary ("Let's start per database and the move to api").

**Context Gathered:**
- The `body` schema and all 7 tables (`weekly_routines`, `workouts`, `personal_records`, `meals`, `water_intake`, `body_metrics`, `sleep_logs`) already exist and are live-verified in Postgres — this feature is pure API-layer work on top of an already-shipped, stable schema. No DB changes are in scope.
- FinPulse.Api has exactly one established convention across 6 existing resources (Goals, Bills, Budgets, Earnings, Expenses, Investments): a full Controller/Service/DTO/Model set per resource, nested under `/api/users/{userId}/{resource}`, `[Authorize]` + a `GetCurrentUserId()` ownership check on every action, EF Core models mapped via `[Table]`/`[Column]` to snake_case DB columns, and DTOs using only `DataAnnotations` — no business-rule validation duplicated from the DB.
- No shared generic CRUD base class exists anywhere in the API — every resource is independently written, even though the 6 existing resources are structurally near-identical. This has been a consistent choice through every prior phase, not an oversight.
- `[RequiresPlan(N)]` gates some resources (Goals, Investments) behind a paid tier; others (Bills, Budgets, Earnings, Expenses) are open to all authenticated users. No single rule — it's a per-resource business decision.
- `FinPulse.Tests` has 53 pre-existing compile errors, documented and left unfixed since the original PostgreSQL/.NET 10 migration — a known, unrelated, out-of-scope gap that persists into this feature.
- `personal_records` was deliberately designed as DB-level append-only history (new row each time a record is broken, never updated in place) — this constrains what the API should expose for that one resource.
- `sleep_logs.total_hours` is a Postgres generated column — the DB physically rejects direct writes to it, so any request DTO that included it would just produce a confusing runtime error.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `api/FinPulse.Api/{Controllers,Services,DTOs,Models}/` — 7 new files per folder (28 total), plus `ApplicationDbContext.cs` gets 7 new `DbSet<T>` registrations | Mirrors the exact file layout of every existing resource; no new folders needed |
| Relevant KB Domains | None — the KB is data-engineering-focused (dbt, Spark, Airflow, data-modeling, etc.); no domain covers ASP.NET Core/EF Core REST API design | Confidence 0.80 (codebase-pattern-only, per the Design Confidence Matrix) — `GoalsController`/`GoalDTOs`/`GoalService`/`Goal.cs` serve as the ground-truth pattern instead of a KB pattern |
| IaC Impact | None — no new services, no docker-compose changes; the API container and Postgres connection already exist | Existing `api/docker-compose.yml` / EF Core setup handles this without modification |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Should every one of the 7 tables get full CRUD, or does any table need different treatment given its DB-level semantics? | Full CRUD for all 7, except `personal_records` gets create+read only | Matches the DB's own append-only design intent for `personal_records`; the other 6 get GET/POST/PUT/DELETE like every existing resource |
| 2 | Should Body endpoints be gated behind a paid plan tier like Goals/Investments? | No — open to all authenticated users, `[Authorize]` only | Fitness/health tracking is a distinct product area from premium financial features; conflating the two gates would be arbitrary |
| 3 | Should this pass add computed/aggregation endpoints (daily nutrition summary, weekly training overview) beyond raw CRUD? | No — raw CRUD only | Mirrors the DB module's own YAGNI cut (`SUM(meals)` computed client-side, not stored); keeps this pass the same size/shape as the DB pass |
| 4 | How should routes be structured — nested under the user like Goals, or flat top-level? | Mirror existing convention: `/api/users/{userId}/body/{resource}` | Zero departure from established routing; every controller keeps the same ownership-check pattern |
| 5 | Any samples/Postman collection to ground the DTOs, or use the existing DTO shape as ground truth? | Use `GoalsController`/`GoalDTOs` as ground truth, no separate samples | The DTO fields are 1:1 derivable from the already-built, live-verified V13–V20 migrations plus the existing DTO convention — no additional grounding needed |
| 6 | Keep the existing one-controller-per-resource style, or introduce a shared generic CRUD base for the 7 similar new resources? | Mirror existing style — 7 independent Controller/Service/DTO/Model sets | Matches 100% of existing precedent (6/6 resources); a generic base would be the first abstraction of its kind in the API, working against the grain of every prior decision |
| 7 | Should DTOs duplicate DB CHECK-constraint validation (e.g. `day_of_week` range, `wake_time > bed_time`) for friendlier errors? | No — DB remains the source of truth, no duplicate validation | Matches `CreateGoalRequest`'s existing pattern exactly (DataAnnotations only, no business rules); would otherwise be the first instance of that pattern in the API |
| 8 | Is writing automated tests part of this feature, given `FinPulse.Tests` has 53 pre-existing, unrelated compile errors? | Yes — write Body module tests anyway; leave the 53 pre-existing errors unfixed | Tests get authored and are correct in isolation, but will not compile/run until someone separately fixes the pre-existing errors — an explicit, accepted limitation, not an oversight |
| 9 | Should `GET /weekly-routines` pad its response to always return all 7 days, or return only whatever rows exist? | Return only whatever rows exist (0–7), no padding | Consistent with "raw CRUD only, no computed convenience logic" from question 3 |

**Minimum Questions:** 3 ✅ (9 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `api/FinPulse.Api/Controllers/GoalsController.cs`, `DTOs/GoalDTOs.cs`, `Models/Goal.cs`, `Services/GoalService.cs` | 4 files | The canonical ground-truth pattern this feature mirrors exactly — route nesting, ownership check, DTO shape, soft-delete via `Status`, service-layer mapping |
| Output examples | N/A | — | No separate example payloads; response DTO shapes are derived directly from each table's live-verified column list (V13–V20) |
| Ground truth | `database/migrations/V14__create_weekly_routines_table.sql` through `V20__create_sleep_logs_table.sql` | 7 files | Source of truth for every field, type, constraint, and generated column each Model/DTO must reflect |
| Related code | `api/FinPulse.Api/Data/ApplicationDbContext.cs` | 1 file | Shows the existing `DbSet<T>` registration pattern; 7 new registrations follow the same shape |

**How samples will be used:**

- Every new Controller/Service/DTO/Model is a structural copy of the Goals set, with fields swapped to match each `body.*` table.
- Generated/computed columns (`sleep_logs.total_hours`) are mirrored as response-only DTO properties, never present in request DTOs — matching how the DB itself rejects direct writes.
- `personal_records`' DTOs omit Update entirely (no `UpdatePersonalRecordRequest`), and its controller omits the `PUT`/`DELETE` actions — matching the DB's append-only design.

---

## Approaches Explored

### Approach A: One independent Controller/Service/DTO/Model set per resource ⭐ Recommended

**Description:** Each of the 7 tables gets its own full file set (`WorkoutsController.cs`, `WorkoutDTOs.cs`, `Workout.cs`, `WorkoutService.cs`, etc.), structurally identical to `GoalsController`/`GoalDTOs`/`Goal.cs`/`GoalService.cs` — no shared base class or generic abstraction.

**Pros:**
- Matches 100% of existing precedent — every one of the 6 current resources (Goals, Bills, Budgets, Earnings, Expenses, Investments) is built this way
- Every file is self-contained and easy to find/reason about in isolation — no indirection through a generic base to understand what one endpoint does
- Zero risk of a shared abstraction leaking resource-specific quirks (e.g. `personal_records`' missing Update, `sleep_logs`' generated column) into unrelated resources

**Cons:**
- ~7x duplication of near-identical CRUD boilerplate (route setup, ownership check, try/catch-Forbid pattern, soft-delete-via-Status)

**Why Recommended:** Confirmed directly by the user. The codebase has consistently chosen explicit duplication over shared base classes through every one of its 6 existing resources — introducing a generic abstraction now, for reasons of convenience on this one feature, would create the only inconsistent corner of the entire API.

---

### Approach B: Shared generic CRUD base (`BaseCrudController<T>`/`BaseCrudService<T>`)

**Description:** A generic base controller and service parameterized by entity type, with the 7 new resources as thin subclasses overriding only what differs (e.g. `personal_records`' missing Update).

**Pros:**
- Meaningfully less boilerplate across 7 near-identical resources
- Centralizes the ownership-check and soft-delete logic in one place

**Cons:**
- The first generic abstraction of its kind anywhere in the API — a new pattern introduced for this feature alone, not reused by (or retrofitted onto) any of the 6 existing resources
- Generic bases tend to accumulate special-case overrides as resource-specific quirks pile up (exactly what `personal_records` and `sleep_logs` already require) — the abstraction's benefit erodes fast in a domain this heterogeneous

**Why Not Recommended:** The user chose Approach A. A generic base is a legitimate future refactor if a *third* wave of similar resources arrives and the duplication becomes a real maintenance burden — but retrofitting it onto 13 resources at once (6 existing + 7 new) is a much larger, unrequested change.

---

## Data Engineering Context

Not applicable — this is a standard REST API layer over an existing OLTP schema, not a data pipeline. No source-system ingestion, ETL, or analytics-layer concerns apply.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A (one independent Controller/Service/DTO/Model set per resource) |
| **User Confirmation** | 2026-08-25, via direct selection |
| **Reasoning** | Total consistency with all 6 existing resources; a generic base would be the first architectural inconsistency introduced anywhere in the API for the sake of this one feature |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Full CRUD for 6 resources; `personal_records` gets POST+GET only | Matches the DB's own append-only design for `personal_records`; everything else needs full lifecycle management like every existing resource | Full CRUD uniformly, including PUT/DELETE on `personal_records` |
| 2 | No `[RequiresPlan]` gating — open to all authenticated users | Fitness/health tracking is a distinct product area from premium financial features; gating it the same way conflates two unrelated value props | Gating Body behind a plan tier like Goals/Investments |
| 3 | Raw CRUD only, no aggregation/summary endpoints this pass | Mirrors the DB module's own YAGNI cut (client computes `SUM(meals)`, nothing is pre-aggregated server-side) | Adding daily-nutrition-summary / weekly-training-overview endpoints now |
| 4 | Routes nested as `/api/users/{userId}/body/{resource}` | Zero departure from the existing convention; keeps the same ownership-check shape everywhere | Flat top-level routes (`/api/body/{resource}`) with no route-level user scoping |
| 5 | One independent Controller/Service/DTO/Model set per resource (7x), no generic base | Matches 100% of existing precedent across all 6 current resources | Introducing a generic `BaseCrudController<T>`/`BaseCrudService<T>` abstraction |
| 6 | No duplicated DB-constraint validation in DTOs (day_of_week range, wake_time>bed_time, etc.) | Matches `CreateGoalRequest`'s existing pattern — DataAnnotations only; DB remains the single source of truth for business rules | Adding explicit C#-side validation for the known CHECK constraints |
| 7 | Generated/computed DB columns (`sleep_logs.total_hours`) appear only in response DTOs, never request DTOs | The DB physically rejects direct writes to generated columns; including them in a request DTO would only produce a confusing runtime error | Including `total_hours` as an optional field on `CreateSleepLogRequest`/`UpdateSleepLogRequest` |
| 8 | `GET /weekly-routines` returns only existing rows (0–7), no padding to a fixed 7-day shape | Consistent with the "raw CRUD only" decision — no computed/convenience logic added server-side | Always returning exactly 7 entries (placeholder rows for missing days) |
| 9 | Write Body module tests into `FinPulse.Tests` despite its 53 pre-existing, unrelated compile errors | User's explicit choice; the new tests are correct and ready the moment the pre-existing errors are eventually fixed, rather than deferred and forgotten | Fixing the 53 errors first (larger, unrequested scope) or skipping tests entirely (falls back to live-only verification) |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Daily nutrition summary / weekly training overview endpoints | Confirmed out of scope — raw CRUD only, matching the DB module's own aggregation deferral | Yes — a natural, small follow-up once a client actually needs pre-aggregated data |
| Plan-tier gating (`[RequiresPlan]`) on Body endpoints | Confirmed out of scope — Body is a distinct product area from premium financial features | Yes — a business decision that can be layered on later without a schema/API shape change |
| Generic `BaseCrudController<T>`/`BaseCrudService<T>` abstraction | Confirmed out of scope — would be the first such abstraction in the API, against 6/6 existing precedent | Yes — a legitimate future refactor if a third wave of similar resources arrives |
| Duplicated DB-constraint validation in DTOs | Confirmed out of scope — DB remains the source of truth, matching every existing DTO | Yes — could be added per-resource later if raw Postgres error messages prove too unfriendly |
| `weekly-routines` response padding to a fixed 7-day shape | Confirmed out of scope — no computed/convenience logic, consistent with raw-CRUD-only scope | Yes — a small, isolated addition to `WeeklyRoutinesController` if a future client wants it |
| Fixing `FinPulse.Tests`' 53 pre-existing compile errors | Explicitly deferred — user chose to write new tests anyway rather than expand scope to include the pre-existing repair | Yes — a separate, standalone feature whenever prioritized |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| Architecture + scope (7 controllers, route shape, plan gating, no aggregation, generated-column handling) | ✅ | "looks good" | No |
| Testing strategy, YAGNI cuts, and `weekly_routines` response-shape default | ✅ | "yes" | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
The live-verified `body` schema (V13–V20) has no API layer — there is no way for a client to create, read, update, or delete Training, Nutrition, or Sleep data, even though the database was explicitly built first with the API as its planned next step.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| FinPulse user tracking their fitness/health | The database can store their data, but nothing can write to or read from it yet — no controller, service, or DTO exists for any of the 7 `body.*` tables |

### Success Criteria (Draft)
- [ ] 7 new Controllers exist under `/api/users/{userId}/body/{resource}`, each `[Authorize]`-protected with the same `GetCurrentUserId()` ownership check as every existing controller
- [ ] 6 resources support full CRUD (`GET`, `POST`, `PUT`, `DELETE`); `personal_records` supports only `GET`/`POST`
- [ ] 7 new EF Core models exist in `ApplicationDbContext`, mapped 1:1 to the live `body.*` tables via `[Table]`/`[Column]` attributes
- [ ] Request/response DTOs exist for each resource, following `GoalDTOs`' shape; generated columns (`sleep_logs.total_hours`) appear only in response DTOs
- [ ] All 7 resources are live-verified end-to-end against the running API + Postgres (Swagger/curl), matching the verification discipline used for every prior build in this initiative
- [ ] New xUnit test files exist in `FinPulse.Tests` for the 7 new controllers/services (acknowledged: will not compile/run until the project's 53 pre-existing, unrelated errors are separately fixed)

### Constraints Identified
- Must match existing API conventions exactly (route nesting, ownership-check pattern, DTO/DataAnnotations style, soft-delete via `Status`, no shared generic base)
- No changes to the `body` schema or any existing migration file — DB layer is already complete and out of scope
- No plan-tier gating on any Body endpoint
- No aggregation/computed endpoints this pass
- `FinPulse.Tests`' 53 pre-existing compile errors remain unfixed and out of scope

### Out of Scope (Confirmed)
- Daily nutrition summary / weekly training overview (computed) endpoints
- Plan-tier gating
- Generic/shared CRUD base abstraction
- Duplicated DB-constraint validation in DTOs
- `weekly-routines` response padding to a fixed 7-day shape
- Fixing `FinPulse.Tests`' pre-existing, unrelated compile errors
- Any changes to the `body` schema or its migrations (V13–V20 are final, shipped)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 9 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 6 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_BODY_MODULE_API.md`
