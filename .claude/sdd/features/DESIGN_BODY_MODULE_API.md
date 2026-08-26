# DESIGN: Body Module API

> Technical design for implementing the REST API layer over the already-built, live-verified `body` schema (7 resources: Training, Nutrition, Sleep)

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | BODY_MODULE_API |
| **Date** | 2026-08-25 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_BODY_MODULE_API.md](./DEFINE_BODY_MODULE_API.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                    FINPULSE.API — BODY MODULE (7 RESOURCES)              │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  [Client] ──JWT Bearer──▶ [Controller]  (Authorize, ownership check)     │
│                                │                                          │
│                                ▼                                          │
│                          [Service]  (business logic, soft-delete)        │
│                                │                                          │
│                                ▼                                          │
│                    [ApplicationDbContext]  (EF Core / Npgsql)            │
│                                │                                          │
│                                ▼                                          │
│                    [Postgres: body.* tables]  (V13–V20, already live)    │
│                                                                            │
│  7 parallel resource stacks, each independent (no shared base class):    │
│  WeeklyRoutines · Workouts · PersonalRecords · Meals ·                   │
│  WaterIntake · BodyMetrics · SleepLogs                                   │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| Controllers (×7) | Route handling, `[Authorize]`, ownership check via `GetCurrentUserId()` | ASP.NET Core MVC (`[ApiController]`) |
| Services (×7) | Business logic, DB queries, DTO mapping, soft-delete | EF Core `DbContext` queries |
| DTOs (×7 files) | Request/response shapes with `DataAnnotations` | Plain C# classes |
| Models (×7) | EF Core entity mapping to live `body.*` tables | `[Table]`/`[Column]` attributes |
| `ApplicationDbContext` (modified) | 7 new `DbSet<T>` + `OnModelCreating` FK/default configs | EF Core |
| `User.cs` (modified) | 7 new navigation collection properties | EF Core |
| `Program.cs` (modified) | 7 new DI registrations (`AddScoped`) | ASP.NET Core DI |
| Tests (×21 files) | Builder + ServiceTests + ControllerTests per resource | xUnit, Moq, FluentAssertions, Bogus |

---

## Key Decisions

### Decision 1: `sleep_logs.total_hours` mapped via `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]`, response-only

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE flagged assumption A-001 — whether EF Core correctly handles a Postgres `GENERATED ALWAYS AS (...) STORED` column without attempting to write to it (which would fail at the DB level, since Postgres physically rejects direct writes to generated columns).

**Choice:** `SleepLog.TotalHours` is mapped with `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]`. `CreateSleepLogRequest`/`UpdateSleepLogRequest` never include a `TotalHours` property; only `SleepLogResponse` does.

**Rationale:** **Live-verified during this Design session** against the real running Postgres instance (`findatabase-postgres`, `body.sleep_logs`) using a throwaway EF Core console scratch project referencing the same `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 package as `FinPulse.Api`:
- `INSERT` with `bed_time`/`wake_time` set, `TotalHours` left at its CLR default → succeeded, EF read back `TotalHours = 7.50` (same bed/wake times as the DB module's own midnight-crossing test case: 23:00 → 06:30 next day)
- `UPDATE` of an unrelated field (`Notes`) on the same row → succeeded, EF did not attempt to write `TotalHours`, value remained correctly `7.50`
- Confirms EF Core + Npgsql never emits `total_hours` in the generated `INSERT`/`UPDATE` SQL for a `Computed` property — it is fetched via `RETURNING` after the write instead

**Alternatives Rejected:**
1. `[DatabaseGenerated(DatabaseGeneratedOption.None)]` with the property simply omitted from all write paths manually — rejected because `None` doesn't tell EF the DB owns this value, so EF would still send whatever the entity's current in-memory value is on `INSERT`, which is `0` by default — silently storing a wrong value would be worse than an explicit error.
2. A raw-SQL query for reads, bypassing EF's change tracking for this one property — rejected as unnecessary complexity; `Computed` is the standard, correct EF Core pattern for this exact scenario and it works, as verified above.

**Consequences:**
- Response DTOs, not request DTOs, are the only place `TotalHours` appears — matches AT-004 from DEFINE.
- Unit tests against EF Core's **InMemory** provider (see Decision 4) cannot exercise this Postgres-side computation — that guarantee is proven only by the live verification above and by Build's own live re-verification against the real database.

---

### Decision 2: `PersonalRecordsController` omits `PUT`/`DELETE` actions entirely (no route exists, not a 405 handler)

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** `body.personal_records` is deliberately append-only at the DB level (a new row per broken record, never updated/deleted in place). DEFINE's AT-003 requires this to surface as `404 Not Found` for `PUT`/`DELETE` requests.

**Choice:** `PersonalRecordsController` has only `[HttpGet]` and `[HttpPost]` action methods. No `[HttpPut]`/`[HttpDelete]` methods exist anywhere in the class — ASP.NET Core's routing naturally returns `404` for a verb+route combination with no matching action, with zero extra code.

**Rationale:** Simplest possible implementation of the DB's own constraint; matches how DEFINE's AT-003 is worded ("no such route exists"). Also matches `PersonalRecordDTOs.cs` having no `UpdatePersonalRecordRequest` class at all — the absence is structural, not enforced by a runtime check.

**Alternatives Rejected:**
1. Implement `PUT`/`DELETE` actions that immediately return `405 Method Not Allowed` or `403 Forbidden` — rejected as unnecessary code for a route that should simply not exist; also `405` isn't what DEFINE's AT-003 specifies.

**Consequences:**
- `PersonalRecordService` has no `UpdatePersonalRecordAsync`/`DeletePersonalRecordAsync` methods — its interface is intentionally smaller than the other 6 services.

---

### Decision 3: DTOs carry only `DataAnnotations`; DB constraint violations surface via the existing unhandled-exception behavior

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's constraint: no duplicated DB-constraint validation (e.g., `day_of_week` range, `wake_time > bed_time`, `UNIQUE(user_id, day_of_week)`) in C#.

**Choice:** Every Create/Update DTO uses only `[Required]`/`[MaxLength]` (matching `CreateGoalRequest` exactly). No custom `IValidatableObject`, no FluentValidation, no manual range/comparison checks. A constraint violation at the DB (e.g., a duplicate `weekly_routines` day) throws an unhandled `DbUpdateException`, which ASP.NET Core's default behavior turns into a `500` — identical to how every existing controller behaves today, since no global exception-handling middleware exists in `Program.cs`.

**Rationale:** Confirmed directly in DEFINE/BRAINSTORM discovery. Adding custom validation middleware or per-DTO business rules would be new architecture introduced solely for this feature, working against 6/6 existing resources' precedent.

**Alternatives Rejected:**
1. Add `IValidatableObject` implementations mirroring each CHECK constraint — rejected, explicitly out of scope per DEFINE.
2. Add global exception-handling middleware to turn `DbUpdateException` into a friendly `400` — rejected as a larger, unrequested architectural change affecting all 13 resources, not just the 7 new ones.

**Consequences:**
- AT-006 ("DB constraint violations surface, not swallowed") is satisfied by doing nothing extra — the default ASP.NET Core behavior already doesn't swallow them.
- A future feature could add global exception handling if `500`s for constraint violations prove too unfriendly in practice.

---

### Decision 4: `SleepLogServiceTests` do not assert the real computed value of `TotalHours`

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** `ServiceTestBase` (existing test infrastructure, unchanged) uses EF Core's **InMemory** provider (`UseInMemoryDatabase`), not real Postgres. The InMemory provider does not execute the database's generated-column SQL expression — a property marked `DatabaseGeneratedOption.Computed` is simply left at its CLR default (`0m`) on InMemory, because there is no real "database" computing it.

**Choice:** `SleepLogServiceTests` test that `CreateSleepLogAsync` correctly persists `BedTime`/`WakeTime` and that the returned `SleepLogResponse.TotalHours` property exists and round-trips whatever the InMemory context returns — but do **not** assert `TotalHours` equals a specific computed value like `7.50m`. That guarantee is covered by Decision 1's live Postgres verification (during Design) and Build's own live re-verification (matching the DB module's own AT-003 discipline).

**Rationale:** Discovered by reading `ServiceTestBase.cs` during this Design session — asserting a specific computed value in an InMemory-backed unit test would either always fail (since InMemory won't compute it) or require manually setting `TotalHours` in the test arrange step, which would test nothing real (the test would just be checking that EF returns what was already put in). Being explicit about this limitation is more honest than writing a test that looks like it verifies Postgres behavior but doesn't.

**Alternatives Rejected:**
1. Switch `ServiceTestBase` to a real (test) Postgres instance via Testcontainers — rejected as a large, unrequested change to shared test infrastructure used by all 13 resources, not scoped to this feature.
2. Manually set `TotalHours` in `SleepLogBuilder` and assert it round-trips — rejected as testing EF's basic property-get/set, not the actual computed-column guarantee; would give false confidence.

**Consequences:**
- `SleepLogServiceTests` is slightly thinner than the other 6 resources' service tests on this one property.
- The BUILD_REPORT must explicitly document this as a known, deliberate test-coverage boundary, not an oversight.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `api/FinPulse.Api/Models/WeeklyRoutine.cs` | Create | EF Core model → `body.weekly_routines` | (general) | None |
| 2 | `api/FinPulse.Api/Models/Workout.cs` | Create | EF Core model → `body.workouts` | (general) | None |
| 3 | `api/FinPulse.Api/Models/PersonalRecord.cs` | Create | EF Core model → `body.personal_records` | (general) | None |
| 4 | `api/FinPulse.Api/Models/Meal.cs` | Create | EF Core model → `body.meals` | (general) | None |
| 5 | `api/FinPulse.Api/Models/WaterIntake.cs` | Create | EF Core model → `body.water_intake` | (general) | None |
| 6 | `api/FinPulse.Api/Models/BodyMetric.cs` | Create | EF Core model → `body.body_metrics` | (general) | None |
| 7 | `api/FinPulse.Api/Models/SleepLog.cs` | Create | EF Core model → `body.sleep_logs`, generated-column mapping (Decision 1) | (general) | None |
| 8 | `api/FinPulse.Api/Models/User.cs` | Modify | Add 7 navigation collection properties | (general) | 1–7 |
| 9 | `api/FinPulse.Api/Data/ApplicationDbContext.cs` | Modify | Add 7 `DbSet<T>` + `OnModelCreating` FK/default configs | (general) | 1–8 |
| 10 | `api/FinPulse.Api/DTOs/WeeklyRoutineDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 1 |
| 11 | `api/FinPulse.Api/DTOs/WorkoutDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 2 |
| 12 | `api/FinPulse.Api/DTOs/PersonalRecordDTOs.cs` | Create | Create/Response DTOs only (Decision 2) | (general) | 3 |
| 13 | `api/FinPulse.Api/DTOs/MealDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 4 |
| 14 | `api/FinPulse.Api/DTOs/WaterIntakeDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 5 |
| 15 | `api/FinPulse.Api/DTOs/BodyMetricDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 6 |
| 16 | `api/FinPulse.Api/DTOs/SleepLogDTOs.cs` | Create | Create/Update/Response DTOs, `TotalHours` response-only (Decision 1) | (general) | 7 |
| 17 | `api/FinPulse.Api/Services/WeeklyRoutineService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 10 |
| 18 | `api/FinPulse.Api/Services/WorkoutService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 11 |
| 19 | `api/FinPulse.Api/Services/PersonalRecordService.cs` | Create | Interface + implementation, create+read only (Decision 2) | (general) | 9, 12 |
| 20 | `api/FinPulse.Api/Services/MealService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 13 |
| 21 | `api/FinPulse.Api/Services/WaterIntakeService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 14 |
| 22 | `api/FinPulse.Api/Services/BodyMetricService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 15 |
| 23 | `api/FinPulse.Api/Services/SleepLogService.cs` | Create | Interface + implementation, full CRUD | (general) | 9, 16 |
| 24 | `api/FinPulse.Api/Controllers/WeeklyRoutinesController.cs` | Create | Full CRUD at `/api/users/{userId}/body/weekly-routines` | (general) | 17 |
| 25 | `api/FinPulse.Api/Controllers/WorkoutsController.cs` | Create | Full CRUD at `/api/users/{userId}/body/workouts` | (general) | 18 |
| 26 | `api/FinPulse.Api/Controllers/PersonalRecordsController.cs` | Create | `GET`/`POST` only at `/api/users/{userId}/body/personal-records` (Decision 2) | (general) | 19 |
| 27 | `api/FinPulse.Api/Controllers/MealsController.cs` | Create | Full CRUD at `/api/users/{userId}/body/meals` | (general) | 20 |
| 28 | `api/FinPulse.Api/Controllers/WaterIntakeController.cs` | Create | Full CRUD at `/api/users/{userId}/body/water-intake` | (general) | 21 |
| 29 | `api/FinPulse.Api/Controllers/BodyMetricsController.cs` | Create | Full CRUD at `/api/users/{userId}/body/body-metrics` | (general) | 22 |
| 30 | `api/FinPulse.Api/Controllers/SleepLogsController.cs` | Create | Full CRUD at `/api/users/{userId}/body/sleep-logs` | (general) | 23 |
| 31 | `api/FinPulse.Api/Program.cs` | Modify | Register 7 new services via `AddScoped` | (general) | 17–23 |
| 32 | `api/FinPulse.Tests/Helpers/Builders/WeeklyRoutineBuilder.cs` | Create | Fluent test-data builder | (general) | 1 |
| 33 | `api/FinPulse.Tests/Helpers/Builders/WorkoutBuilder.cs` | Create | Fluent test-data builder | (general) | 2 |
| 34 | `api/FinPulse.Tests/Helpers/Builders/PersonalRecordBuilder.cs` | Create | Fluent test-data builder | (general) | 3 |
| 35 | `api/FinPulse.Tests/Helpers/Builders/MealBuilder.cs` | Create | Fluent test-data builder | (general) | 4 |
| 36 | `api/FinPulse.Tests/Helpers/Builders/WaterIntakeBuilder.cs` | Create | Fluent test-data builder | (general) | 5 |
| 37 | `api/FinPulse.Tests/Helpers/Builders/BodyMetricBuilder.cs` | Create | Fluent test-data builder | (general) | 6 |
| 38 | `api/FinPulse.Tests/Helpers/Builders/SleepLogBuilder.cs` | Create | Fluent test-data builder | (general) | 7 |
| 39 | `api/FinPulse.Tests/UnitTests/Services/WeeklyRoutineServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 17, 32 |
| 40 | `api/FinPulse.Tests/UnitTests/Services/WorkoutServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 18, 33 |
| 41 | `api/FinPulse.Tests/UnitTests/Services/PersonalRecordServiceTests.cs` | Create | Service unit tests (InMemory), create+read only | (general) | 19, 34 |
| 42 | `api/FinPulse.Tests/UnitTests/Services/MealServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 20, 35 |
| 43 | `api/FinPulse.Tests/UnitTests/Services/WaterIntakeServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 21, 36 |
| 44 | `api/FinPulse.Tests/UnitTests/Services/BodyMetricServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 22, 37 |
| 45 | `api/FinPulse.Tests/UnitTests/Services/SleepLogServiceTests.cs` | Create | Service unit tests (InMemory), no computed-value assertion (Decision 4) | (general) | 23, 38 |
| 46 | `api/FinPulse.Tests/UnitTests/Controllers/WeeklyRoutinesControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 24 |
| 47 | `api/FinPulse.Tests/UnitTests/Controllers/WorkoutsControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 25 |
| 48 | `api/FinPulse.Tests/UnitTests/Controllers/PersonalRecordsControllerTests.cs` | Create | Controller unit tests (mocked service), no PUT/DELETE tests | (general) | 26 |
| 49 | `api/FinPulse.Tests/UnitTests/Controllers/MealsControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 27 |
| 50 | `api/FinPulse.Tests/UnitTests/Controllers/WaterIntakeControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 28 |
| 51 | `api/FinPulse.Tests/UnitTests/Controllers/BodyMetricsControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 29 |
| 52 | `api/FinPulse.Tests/UnitTests/Controllers/SleepLogsControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 30 |

**Total Files:** 52 (49 create, 3 modify)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| (general) | All 52 | No specialist agent in `.claude/agents/` matches ASP.NET Core/EF Core REST API code (the roster is data-engineering-focused: `schema-designer`, `dbt-specialist`, `airflow-specialist`, etc. — none cover C#/.NET application code). Build handles all 52 files directly, following the code patterns below exactly, the same way `POSTGRESQL_API_MIGRATION` was originally built. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: File type, purpose keywords, path patterns, KB domains — no match found for `.cs` REST API files

---

## Code Patterns

### Pattern 1: Standard full-CRUD resource (applies to `Workout`, `Meal`, `WaterIntake`, `BodyMetric`, `WeeklyRoutine`)

**Model** (`Models/Workout.cs`):

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("workouts", Schema = "body")]
public class Workout
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("workout_date")]
    public DateTime WorkoutDate { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("routine_name")]
    public string RoutineName { get; set; } = string.Empty;

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("calories_burned", TypeName = "decimal(8,2)")]
    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
```

**DTOs** (`DTOs/WorkoutDTOs.cs`):

```csharp
using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateWorkoutRequest
{
    [Required]
    public DateTime WorkoutDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoutineName { get; set; } = string.Empty;

    public int? DurationMinutes { get; set; }

    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateWorkoutRequest
{
    public DateTime? WorkoutDate { get; set; }

    [MaxLength(100)]
    public string? RoutineName { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? CaloriesBurned { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class WorkoutResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime WorkoutDate { get; set; }
    public string RoutineName { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public decimal? CaloriesBurned { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Service** (`Services/WorkoutService.cs`):

```csharp
using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IWorkoutService
{
    Task<List<WorkoutResponse>> GetUserWorkoutsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<WorkoutResponse> CreateWorkoutAsync(int userId, CreateWorkoutRequest request);
    Task<WorkoutResponse?> UpdateWorkoutAsync(int userId, int workoutId, UpdateWorkoutRequest request);
    Task<bool> DeleteWorkoutAsync(int userId, int workoutId);
}

public class WorkoutService : IWorkoutService
{
    private readonly ApplicationDbContext _context;

    public WorkoutService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutResponse>> GetUserWorkoutsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Workouts.Where(w => w.UserId == userId && w.Status != 0);

        if (startDate.HasValue)
            query = query.Where(w => w.WorkoutDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(w => w.WorkoutDate <= endDate.Value);

        return await query
            .OrderByDescending(w => w.WorkoutDate)
            .Select(w => new WorkoutResponse
            {
                Id = w.Id,
                UserId = w.UserId,
                WorkoutDate = w.WorkoutDate,
                RoutineName = w.RoutineName,
                DurationMinutes = w.DurationMinutes,
                CaloriesBurned = w.CaloriesBurned,
                Notes = w.Notes,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<WorkoutResponse> CreateWorkoutAsync(int userId, CreateWorkoutRequest request)
    {
        var workout = new Workout
        {
            UserId = userId,
            WorkoutDate = request.WorkoutDate,
            RoutineName = request.RoutineName,
            DurationMinutes = request.DurationMinutes,
            CaloriesBurned = request.CaloriesBurned,
            Notes = request.Notes,
            Status = 1
        };

        _context.Workouts.Add(workout);
        await _context.SaveChangesAsync();

        return new WorkoutResponse
        {
            Id = workout.Id,
            UserId = workout.UserId,
            WorkoutDate = workout.WorkoutDate,
            RoutineName = workout.RoutineName,
            DurationMinutes = workout.DurationMinutes,
            CaloriesBurned = workout.CaloriesBurned,
            Notes = workout.Notes,
            Status = workout.Status,
            CreatedAt = workout.CreatedAt
        };
    }

    public async Task<WorkoutResponse?> UpdateWorkoutAsync(int userId, int workoutId, UpdateWorkoutRequest request)
    {
        var workout = await _context.Workouts.FirstOrDefaultAsync(w => w.Id == workoutId && w.Status != 0);

        if (workout == null)
            return null;

        if (workout.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this workout");

        if (request.WorkoutDate.HasValue) workout.WorkoutDate = request.WorkoutDate.Value;
        if (request.RoutineName != null) workout.RoutineName = request.RoutineName;
        if (request.DurationMinutes.HasValue) workout.DurationMinutes = request.DurationMinutes.Value;
        if (request.CaloriesBurned.HasValue) workout.CaloriesBurned = request.CaloriesBurned.Value;
        if (request.Notes != null) workout.Notes = request.Notes;
        if (request.Status.HasValue) workout.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new WorkoutResponse
        {
            Id = workout.Id,
            UserId = workout.UserId,
            WorkoutDate = workout.WorkoutDate,
            RoutineName = workout.RoutineName,
            DurationMinutes = workout.DurationMinutes,
            CaloriesBurned = workout.CaloriesBurned,
            Notes = workout.Notes,
            Status = workout.Status,
            CreatedAt = workout.CreatedAt
        };
    }

    public async Task<bool> DeleteWorkoutAsync(int userId, int workoutId)
    {
        var workout = await _context.Workouts.FirstOrDefaultAsync(w => w.Id == workoutId && w.Status != 0);

        if (workout == null)
            return false;

        if (workout.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this workout");

        workout.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
```

**Controller** (`Controllers/WorkoutsController.cs`):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/body/workouts")]
[Authorize]
public class WorkoutsController : ControllerBase
{
    private readonly IWorkoutService _workoutService;

    public WorkoutsController(IWorkoutService workoutService)
    {
        _workoutService = workoutService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<WorkoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkouts(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var workouts = await _workoutService.GetUserWorkoutsAsync(userId, start_date, end_date);
        return Ok(workouts);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateWorkout(int userId, [FromBody] CreateWorkoutRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var workout = await _workoutService.CreateWorkoutAsync(userId, request);
        return StatusCode(201, workout);
    }

    [HttpPut("{workoutId}")]
    [ProducesResponseType(typeof(WorkoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateWorkout(int userId, int workoutId, [FromBody] UpdateWorkoutRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var workout = await _workoutService.UpdateWorkoutAsync(userId, workoutId, request);
            if (workout == null)
                return NotFound(new { message = "Workout not found" });

            return Ok(workout);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{workoutId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteWorkout(int userId, int workoutId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _workoutService.DeleteWorkoutAsync(userId, workoutId);
            if (!success)
                return NotFound(new { message = "Workout not found" });

            return Ok(new { message = "Workout deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
```

**Field mapping for the other 4 Pattern-1 resources** (apply the same 4-file structure, swapping fields per V14/V17/V18/V19):

| Resource | Route | Distinguishing fields (beyond `id`/`user_id`/`status`/`created_at`) | Notable constraint |
|----------|-------|----------------------------------------------------------------------|---------------------|
| `WeeklyRoutine` | `/body/weekly-routines` | `DayOfWeek` (short, 0–6), `RoutineName` (string, required, max100), `Description` (string?, max500) | `UNIQUE(user_id, day_of_week)` — surfaces as `500` per Decision 3, no C# check |
| `Meal` | `/body/meals` | `MealDate` (DateTime, required), `MealType` (string, required, max50), `Description` (string?, max500), `Calories` (decimal, required), `ProteinGrams`/`CarbsGrams`/`FatGrams` (decimal?) | None beyond FK |
| `WaterIntake` | `/body/water-intake` | `IntakeDate` (DateTime, required), `AmountMl` (int, required, default 0) | `UNIQUE(user_id, intake_date)` — same as above |
| `BodyMetric` | `/body/body-metrics` | `MeasuredDate` (DateTime, required), `WeightKg`/`HeightCm` (decimal?), `BodyFatPercent` (decimal?), `Notes` (string?, max500) | `UNIQUE(user_id, measured_date)` — same as above |

---

### Pattern 2: Create+read-only resource (`PersonalRecord`)

**DTOs** (`DTOs/PersonalRecordDTOs.cs`) — no `Update` class:

```csharp
using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreatePersonalRecordRequest
{
    [Required]
    [MaxLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string MetricType { get; set; } = string.Empty;

    [Required]
    public decimal Value { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public DateTime AchievedDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class PersonalRecordResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime AchievedDate { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Service interface** (`Services/PersonalRecordService.cs`) — only 2 methods, matching Decision 2:

```csharp
public interface IPersonalRecordService
{
    Task<List<PersonalRecordResponse>> GetUserPersonalRecordsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<PersonalRecordResponse> CreatePersonalRecordAsync(int userId, CreatePersonalRecordRequest request);
}
```

**Controller** (`Controllers/PersonalRecordsController.cs`) — only `GetPersonalRecords`/`CreatePersonalRecord` actions, same ownership-check shape as `WorkoutsController`'s `GetWorkouts`/`CreateWorkout`. No `[HttpPut]`/`[HttpDelete]` methods exist in this class (Decision 2).

---

### Pattern 3: Generated-column resource (`SleepLog`)

**Model** (`Models/SleepLog.cs`) — live-verified mapping from Decision 1:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("sleep_logs", Schema = "body")]
public class SleepLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("bed_time")]
    public DateTime BedTime { get; set; }

    [Required]
    [Column("wake_time")]
    public DateTime WakeTime { get; set; }

    [Column("total_hours", TypeName = "decimal(4,2)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalHours { get; set; }

    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
```

**DTOs** (`DTOs/SleepLogDTOs.cs`) — `TotalHours` appears only in the Response:

```csharp
using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateSleepLogRequest
{
    [Required]
    public DateTime BedTime { get; set; }

    [Required]
    public DateTime WakeTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateSleepLogRequest
{
    public DateTime? BedTime { get; set; }
    public DateTime? WakeTime { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class SleepLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime BedTime { get; set; }
    public DateTime WakeTime { get; set; }
    public decimal TotalHours { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

The Service/Controller for `SleepLog` otherwise follow Pattern 1 exactly (full CRUD), with `CreateSleepLogAsync`/`UpdateSleepLogAsync` never assigning `TotalHours` — EF Core populates it automatically on `SaveChangesAsync()` per Decision 1.

---

### Pattern 4: Shared infrastructure modifications

**`ApplicationDbContext.cs`** — add alongside the existing 6 `DbSet<T>` declarations:

```csharp
public DbSet<WeeklyRoutine> WeeklyRoutines { get; set; }
public DbSet<Workout> Workouts { get; set; }
public DbSet<PersonalRecord> PersonalRecords { get; set; }
public DbSet<Meal> Meals { get; set; }
public DbSet<WaterIntake> WaterIntakes { get; set; }
public DbSet<BodyMetric> BodyMetrics { get; set; }
public DbSet<SleepLog> SleepLogs { get; set; }
```

And inside `OnModelCreating`, one block per entity following the exact existing shape (shown for `Workout`; repeat for the other 6):

```csharp
modelBuilder.Entity<Workout>(entity =>
{
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
    entity.HasOne(e => e.User)
          .WithMany(u => u.Workouts)
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

**`User.cs`** — add alongside the existing 6 navigation properties:

```csharp
public virtual ICollection<WeeklyRoutine> WeeklyRoutines { get; set; } = new List<WeeklyRoutine>();
public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
public virtual ICollection<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();
public virtual ICollection<WaterIntake> WaterIntakes { get; set; } = new List<WaterIntake>();
public virtual ICollection<BodyMetric> BodyMetrics { get; set; } = new List<BodyMetric>();
public virtual ICollection<SleepLog> SleepLogs { get; set; } = new List<SleepLog>();
```

**`Program.cs`** — add alongside the existing 6 `AddScoped` registrations:

```csharp
builder.Services.AddScoped<IWeeklyRoutineService, WeeklyRoutineService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IPersonalRecordService, PersonalRecordService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IWaterIntakeService, WaterIntakeService>();
builder.Services.AddScoped<IBodyMetricService, BodyMetricService>();
builder.Services.AddScoped<ISleepLogService, SleepLogService>();
```

No `[RequiresPlan]` filter is applied anywhere (Body is open to all authenticated users per DEFINE).

---

### Pattern 5: Test patterns

**Builder** (`Helpers/Builders/WorkoutBuilder.cs`) — same fluent-builder shape as `GoalBuilder`:

```csharp
using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class WorkoutBuilder
{
    private readonly Workout _workout;
    private static readonly Faker _faker = new Faker();

    public WorkoutBuilder()
    {
        _workout = new Workout
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            WorkoutDate = _faker.Date.Recent(30),
            RoutineName = _faker.PickRandom(new[] { "Push Day", "Pull Day", "Leg Day", "Rest Day" }),
            DurationMinutes = _faker.Random.Int(20, 90),
            CaloriesBurned = _faker.Random.Decimal(150, 600),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public WorkoutBuilder WithUserId(int userId) { _workout.UserId = userId; return this; }
    public WorkoutBuilder WithWorkoutDate(DateTime date) { _workout.WorkoutDate = date; return this; }
    public WorkoutBuilder AsActive() { _workout.Status = 1; return this; }
    public WorkoutBuilder AsDeleted() { _workout.Status = 0; return this; }
    public Workout Build() => _workout;
}
```

**ServiceTests** (`UnitTests/Services/WorkoutServiceTests.cs`) — same structure as `GoalServiceTests`: `CreateXAsync_WithValidRequest_CreatesSuccessfully`, `GetUserXAsync_ReturnsOnlyUserX`, `GetUserXAsync_FiltersOutDeletedX`, `GetUserXAsync_FiltersByDateRange`, `UpdateXAsync_WithValidRequest_UpdatesSuccessfully`, `UpdateXAsync_WithWrongUserId_ThrowsUnauthorizedAccessException`, `DeleteXAsync_SoftDeletesX`, `DeleteXAsync_WithWrongUserId_ThrowsUnauthorizedAccessException` — one `[Fact]` per scenario, using `ServiceTestBase`'s InMemory `Context` exactly like `GoalServiceTests` does.

**ControllerTests** (`UnitTests/Controllers/WorkoutsControllerTests.cs`) — same structure as `GoalsControllerTests`: `Mock<IWorkoutService>`, `SetupControllerContext(_sut, userId)` from `ControllerTestBase`, one `[Fact]` per action × {ownership-ok, ownership-forbidden, not-found-where-applicable}.

**`PersonalRecordServiceTests`/`PersonalRecordsControllerTests`** cover only `Create`/`GetUser` scenarios — no `Update`/`Delete` test methods exist, matching Decision 2's structural omission.

**`SleepLogServiceTests`** follows Pattern 5 but per Decision 4 omits any assertion pinning `TotalHours` to a specific computed value.

---

## Data Flow

```text
1. Client sends HTTP request with JWT bearer cookie/header to
   /api/users/{userId}/body/{resource}[/{id}]
   │
   ▼
2. [Authorize] validates the JWT; controller extracts the authenticated
   user's id via GetCurrentUserId() and compares it to the route {userId}
   │
   ▼
3. On mismatch → 403 Forbidden (no DB call made)
   On match → controller calls the matching IXService method
   │
   ▼
4. Service builds/executes an EF Core LINQ query against
   ApplicationDbContext, scoped to Status != 0 for reads
   │
   ▼
5. EF Core translates to SQL, Npgsql executes against Postgres
   (body.* tables, V13–V20 — already live, unchanged by this feature)
   │
   ▼
6. Service maps the resulting entity/entities to Response DTOs,
   returns to controller, controller returns the appropriate
   IActionResult (200/201/404) — OpenTelemetry auto-instruments
   the whole request via existing AddAspNetCoreInstrumentation()/AddNpgsql()
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-------------------|------------------|
| PostgreSQL (`body` schema, V13–V20) | EF Core / Npgsql, direct connection | Existing `DefaultConnection` string, unchanged |
| OpenTelemetry Collector | Auto-instrumented traces/metrics (existing `AddAspNetCoreInstrumentation`/`AddNpgsql`) | None — same OTLP endpoint already configured |

No new external systems are introduced by this feature.

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Unit — Service | Business logic, soft-delete, ownership, date filtering | 7 files, `UnitTests/Services/*ServiceTests.cs` | xUnit, FluentAssertions, EF Core InMemory (`ServiceTestBase`) | Every public service method, happy + error path |
| Unit — Controller | Route/ownership/status-code behavior | 7 files, `UnitTests/Controllers/*ControllerTests.cs` | xUnit, Moq, FluentAssertions (`ControllerTestBase`) | Every action, ownership-ok + ownership-forbidden |
| Live (manual) | Full CRUD lifecycle against real Postgres, matching AT-001–AT-007 | Swagger UI / curl against running `dotnet run` + `findatabase-postgres` | Manual, same discipline as every prior build in this initiative | All 7 acceptance tests from DEFINE |

**Known, deliberate boundary:** the new xUnit test files (39–52 in the manifest) are written and structurally correct but **will not compile or run** as part of `dotnet test FinPulse.Tests` until the 53 pre-existing, unrelated errors in `BillBuilder.cs`/`BillsControllerTests.cs`/`BillServiceTests.cs`/`AuthControllerTests.cs`/`UsersControllerTests.cs`/`UserServiceTests.cs` are separately fixed (confirmed live during this Design session via `dotnet build FinPulse.Tests` — the error count and file list are unchanged from the DEFINE phase's citation, and none of the errors touch the Goal-pattern test infrastructure these new files build on). This is DEFINE's explicit, accepted decision, not a Build-phase surprise.

Live verification during Build must cover every acceptance test in DEFINE (AT-001–AT-007), the same "test against the real running system" discipline used for every prior feature in this initiative.

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|---------------------|--------|
| Route `userId` ≠ authenticated user | `Forbid()` → `403`, no DB call | No |
| Resource not found (`GET`/`PUT`/`DELETE` by id) | `NotFound()` → `404` with `{ message: "... not found" }` | No |
| Service throws `UnauthorizedAccessException` (defense-in-depth ownership check inside `Update`/`Delete`) | Controller catches, returns `Forbid()` → `403` | No |
| DB constraint violation (`UNIQUE`, `CHECK`, FK) | Unhandled `DbUpdateException` → ASP.NET Core default `500` (Decision 3 — matches existing behavior, no new middleware) | No |
| `PUT`/`DELETE` on `personal_records` | No matching route → framework default `404` (Decision 2) | No |

---

## Configuration

No new configuration keys — reuses the existing `ConnectionStrings:DefaultConnection`, `Jwt:*`, and `Otel:ExporterEndpoint` settings from `appsettings.json` unchanged.

---

## Security Considerations

- Every controller requires `[Authorize]` — no anonymous access, matching every existing controller.
- Ownership is enforced on every action via `GetCurrentUserId()` compared against the route `{userId}`, before any DB call — identical to `GoalsController`'s proven pattern.
- No `[RequiresPlan]` gate is applied (explicit DEFINE decision) — this is a deliberate business choice, not an oversight; Body data is still protected by authentication + ownership like every other resource.
- Soft-delete only (`Status = 0`) — no hard deletes, so no risk of accidental permanent data loss via this API layer; matches existing convention.
- DTOs use `[Required]`/`[MaxLength]` to prevent oversized/missing input from reaching the DB layer; the DB's own CHECK/UNIQUE/FK constraints remain the final authority (Decision 3).

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | Serilog structured logging via existing `UseSerilogRequestLogging` — no changes needed, applies automatically to the 7 new controllers |
| Metrics | Existing `AddAspNetCoreInstrumentation()`/`AddNpgsql()` OpenTelemetry metrics apply automatically to the new routes/queries |
| Tracing | Existing `AddAspNetCoreInstrumentation()` OpenTelemetry tracing applies automatically — new spans appear in the existing Tempo/Grafana stack (`monitor/`) with no configuration changes |

---

## Pipeline Architecture (if applicable)

Not applicable — this is a REST API feature, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | design-agent | Initial version. Live-verified Decision 1 (EF Core `Computed` generated-column mapping) against real Postgres during this session; confirmed Decision 4 (InMemory test-provider limitation) by reading `ServiceTestBase.cs`; confirmed A-003 (test project compiles for Goal-pattern infrastructure despite 53 pre-existing, unrelated errors) via a live `dotnet build FinPulse.Tests` run. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_BODY_MODULE_API.md`
