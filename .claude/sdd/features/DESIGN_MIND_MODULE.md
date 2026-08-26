# DESIGN: Mind Module (Meditation & Journaling)

> Technical design for a new `mind` Postgres schema (meditation sessions, journal entries) plus its full REST API layer, built in one pass and mirroring the Body module's proven schema and API conventions exactly.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | MIND_MODULE |
| **Date** | 2026-08-25 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_MIND_MODULE.md](./DEFINE_MIND_MODULE.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                    FINPULSE.API — MIND MODULE (2 RESOURCES)              │
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
│                    [Postgres: mind.* tables]  (V21–V23, new)             │
│                                                                            │
│  2 parallel resource stacks, each independent (no shared base class):    │
│  MeditationSessions · JournalEntries                                     │
│                                                                            │
│  New schema, same pattern as `body` (V13) → `finance`/`plan`/            │
│  `reporting`/`investment` (V1)                                           │
│                                                                            │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| Migrations (×3) | Create `mind` schema + 2 tables, live-verified against Postgres | Flyway-style numbered SQL (`V21`–`V23`) |
| Controllers (×2) | Route handling, `[Authorize]`, ownership check via `GetCurrentUserId()` | ASP.NET Core MVC (`[ApiController]`) |
| Services (×2) | Business logic, DB queries, DTO mapping, soft-delete | EF Core `DbContext` queries |
| DTOs (×2 files) | Request/response shapes with `DataAnnotations` | Plain C# classes |
| Models (×2) | EF Core entity mapping to new `mind.*` tables | `[Table]`/`[Column]` attributes |
| `ApplicationDbContext` (modified) | 2 new `DbSet<T>` + `OnModelCreating` FK/default configs | EF Core |
| `User.cs` (modified) | 2 new navigation collection properties | EF Core |
| `Program.cs` (modified) | 2 new DI registrations (`AddScoped`) | ASP.NET Core DI |
| Tests (×6 files) | Builder + ServiceTests + ControllerTests per resource | xUnit, Moq, FluentAssertions, Bogus |

---

## Key Decisions

### Decision 1: New `mind` schema created via its own migration (V21), before the table migrations

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE requires the new tables to live in a new `mind` schema, separate from `body`, matching the existing schema-per-domain convention (`finance`/`plan`/`reporting`/`investment` in V1, `body` in V13).

**Choice:** `V21__create_mind_schema.sql` contains only `CREATE SCHEMA IF NOT EXISTS mind;`, identical in shape to `V13__create_body_schema.sql`. `V22`/`V23` create the two tables inside it.

**Rationale:** Confirmed by reading `V13__create_body_schema.sql` directly during this Design session — it is a single-statement file with no table definitions, and every table migration since (V14–V20) references `body.*` by schema-qualified name. Mirroring this exactly keeps schema creation and table creation as separately reviewable, separately revertable migration steps.

**Alternatives Rejected:**
1. Fold `CREATE SCHEMA IF NOT EXISTS mind;` into `V22` (the first table migration) — rejected because it breaks the established one-concern-per-migration pattern seen in V1/V13.

**Consequences:**
- Migration numbering for this feature is V21 (schema), V22 (`meditation_sessions`), V23 (`journal_entries`) — confirmed via `Glob` during this Design session that V20 (`sleep_logs`) is the current head and no V21+ exists yet, resolving DEFINE's open assumption A-004.

---

### Decision 2: Mood CHECK constraints use explicit `IS NULL OR ... BETWEEN` form

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's assumption A-001 — whether a nullable `SMALLINT` column with a `CHECK (col BETWEEN 1 AND 5)` constraint correctly allows `NULL`. Under standard SQL three-valued logic, `NULL BETWEEN 1 AND 5` evaluates to `UNKNOWN`, and a Postgres `CHECK` constraint is satisfied whenever its expression is `TRUE` or `UNKNOWN` — so a bare `CHECK (mood BETWEEN 1 AND 5)` already permits `NULL` without any special handling. This is documented, standard Postgres behavior (SQL standard three-valued logic), not something that needs a scratch-project live experiment the way the Body module's generated-column mapping did.

**Choice:** All three mood columns (`meditation_sessions.mood_before`, `.mood_after`, `journal_entries.mood`) use the explicit form: `CHECK (mood_before IS NULL OR mood_before BETWEEN 1 AND 5)`.

**Rationale:** Functionally identical to the bare form, but self-documenting — a reader doesn't need to know SQL's `NULL`-in-`CHECK` semantics to understand that the column is intentionally optional. This matches the codebase's `no inline comments, self-documenting names` standard extended to SQL constraint expressions.

**Alternatives Rejected:**
1. Bare `CHECK (mood BETWEEN 1 AND 5)` — functionally correct but relies on the reader already knowing three-valued-logic CHECK semantics; rejected for clarity, not correctness.

**Consequences:**
- Build must still live-verify both directions per AT-002 (mood=9 rejected) and AT-008 (mood omitted/NULL accepted) against the real running Postgres instance — this decision only fixes the SQL text, not the verification discipline.

---

### Decision 3: Nullable mood columns mapped to C# `short?`, no special EF Core configuration

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** DEFINE's assumption A-002 — whether EF Core maps a nullable Postgres `SMALLINT` to a nullable C# `short?` without extra configuration.

**Choice:** `MeditationSession.MoodBefore`/`MoodAfter` and `JournalEntry.Mood` are typed `short?` with a plain `[Column("mood_before")]` attribute, no `[DatabaseGenerated]` or `TypeName` override.

**Rationale:** This is standard EF Core nullable value-type mapping — already proven in this exact codebase via `Meal.ProteinGrams`/`CarbsGrams`/`FatGrams` (`decimal?`), `Workout.DurationMinutes` (`int?`), and `Workout.CaloriesBurned` (`decimal?`), none of which needed special configuration. Unlike Decision 1 in `BODY_MODULE_API` (the `sleep_logs.total_hours` generated column), there is no Postgres-side computed-value magic here — this is a plain nullable column, so no live scratch-project verification is warranted; Build's live CRUD verification (AT-005, AT-008) is sufficient.

**Alternatives Rejected:**
1. Live-verify via a throwaway EF Core console project, matching `BODY_MODULE_API` Decision 1's discipline — rejected as unnecessary ceremony for a well-understood, already-proven-in-this-codebase mapping pattern; that discipline is reserved for genuinely novel/risky mappings (generated columns), not routine nullable scalars.

**Consequences:**
- None beyond the standard nullable-property boilerplate already used throughout the API.

---

### Decision 4: `journal_entries.content` mapped as Postgres `TEXT` / C# `string`, `[Required]` only (no `[MaxLength]`)

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-25 |

**Context:** Every existing text column in this codebase (`Description`, `Notes`, etc.) uses `VARCHAR(n)` + `[MaxLength(n)]`. Journal content is free-form long-form writing, so a bounded `VARCHAR` would be an artificial constraint not requested anywhere in DEFINE/BRAINSTORM.

**Choice:** `content TEXT NOT NULL` in Postgres, mapped to `public string Content { get; set; } = string.Empty;` in C# with `[Required]` but no `[MaxLength]` in either the model or the DTOs.

**Rationale:** Postgres `TEXT` has no length limit and EF Core/Npgsql maps unbounded `string` to it without any `[Column(TypeName=...)]` override needed (the default `string` → Npgsql mapping is `text` when no `[MaxLength]`/`[StringLength]` constrains it). This is the correct, idiomatic choice for free-form content and introduces no new pattern beyond "don't add `[MaxLength]` when there isn't a real bound."

**Alternatives Rejected:**
1. `VARCHAR(5000)` or similar arbitrary cap — rejected; no such limit was requested, and an arbitrary cap would just be a future support ticket.

**Consequences:**
- `journal_entries` is the first table in this codebase to use an unbounded `TEXT` column; Build should confirm during live verification that a long (multi-paragraph) `content` value round-trips correctly.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `database/migrations/V21__create_mind_schema.sql` | Create | `CREATE SCHEMA IF NOT EXISTS mind;` (Decision 1) | (general) | None |
| 2 | `database/migrations/V22__create_meditation_sessions_table.sql` | Create | `mind.meditation_sessions` table + CHECK/FK constraints + `COMMENT ON` docs | (general) | 1 |
| 3 | `database/migrations/V23__create_journal_entries_table.sql` | Create | `mind.journal_entries` table + CHECK/FK constraints + `COMMENT ON` docs | (general) | 1 |
| 4 | `api/FinPulse.Api/Models/MeditationSession.cs` | Create | EF Core model → `mind.meditation_sessions` | (general) | 2 |
| 5 | `api/FinPulse.Api/Models/JournalEntry.cs` | Create | EF Core model → `mind.journal_entries` | (general) | 3 |
| 6 | `api/FinPulse.Api/Models/User.cs` | Modify | Add 2 navigation collection properties | (general) | 4, 5 |
| 7 | `api/FinPulse.Api/Data/ApplicationDbContext.cs` | Modify | Add 2 `DbSet<T>` + `OnModelCreating` FK/default configs | (general) | 4, 5, 6 |
| 8 | `api/FinPulse.Api/DTOs/MeditationSessionDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 4 |
| 9 | `api/FinPulse.Api/DTOs/JournalEntryDTOs.cs` | Create | Create/Update/Response DTOs | (general) | 5 |
| 10 | `api/FinPulse.Api/Services/MeditationSessionService.cs` | Create | Interface + implementation, full CRUD | (general) | 7, 8 |
| 11 | `api/FinPulse.Api/Services/JournalEntryService.cs` | Create | Interface + implementation, full CRUD | (general) | 7, 9 |
| 12 | `api/FinPulse.Api/Controllers/MeditationSessionsController.cs` | Create | Full CRUD at `/api/users/{userId}/mind/meditation-sessions` | (general) | 10 |
| 13 | `api/FinPulse.Api/Controllers/JournalEntriesController.cs` | Create | Full CRUD at `/api/users/{userId}/mind/journal-entries` | (general) | 11 |
| 14 | `api/FinPulse.Api/Program.cs` | Modify | Register 2 new services via `AddScoped` | (general) | 10, 11 |
| 15 | `api/FinPulse.Tests/Helpers/Builders/MeditationSessionBuilder.cs` | Create | Fluent test-data builder | (general) | 4 |
| 16 | `api/FinPulse.Tests/Helpers/Builders/JournalEntryBuilder.cs` | Create | Fluent test-data builder | (general) | 5 |
| 17 | `api/FinPulse.Tests/UnitTests/Services/MeditationSessionServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 10, 15 |
| 18 | `api/FinPulse.Tests/UnitTests/Services/JournalEntryServiceTests.cs` | Create | Service unit tests (InMemory) | (general) | 11, 16 |
| 19 | `api/FinPulse.Tests/UnitTests/Controllers/MeditationSessionsControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 12 |
| 20 | `api/FinPulse.Tests/UnitTests/Controllers/JournalEntriesControllerTests.cs` | Create | Controller unit tests (mocked service) | (general) | 13 |

**Total Files:** 20 (17 create, 3 modify)

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| (general) | All 20 | No specialist agent in `.claude/agents/` matches ASP.NET Core/EF Core REST API or plain SQL migration files (the roster is data-engineering-focused: `schema-designer`, `dbt-specialist`, `airflow-specialist`, etc. — none cover C#/.NET application code or Flyway-style OLTP migrations). Build handles all 20 files directly, following the code patterns below exactly — same conclusion reached during `BODY_MODULE_API`'s design. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: File type, purpose keywords, path patterns, KB domains — no match found for `.cs` REST API files or `.sql` OLTP migration files

---

## Code Patterns

### Pattern 1: Migration — schema creation (`V21__create_mind_schema.sql`)

```sql
------------------------------------------------------------
-- CREATE MIND SCHEMA
------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS mind;
```

### Pattern 2: Migration — `meditation_sessions` table (`V22__create_meditation_sessions_table.sql`)

```sql
------------------------------------------------------------
-- MEDITATION SESSIONS TABLE DEFINITION
-- Per-session meditation log with optional before/after mood.
------------------------------------------------------------
CREATE TABLE mind.meditation_sessions (
    id                 INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id            INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_date       DATE NOT NULL,
    duration_minutes   SMALLINT NOT NULL CHECK (duration_minutes > 0),
    meditation_type    VARCHAR(50) NOT NULL,
    mood_before        SMALLINT CHECK (mood_before IS NULL OR mood_before BETWEEN 1 AND 5),
    mood_after         SMALLINT CHECK (mood_after IS NULL OR mood_after BETWEEN 1 AND 5),
    notes              VARCHAR(500),
    status             SMALLINT NOT NULL DEFAULT 1,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE mind.meditation_sessions
IS 'Per-session meditation log with optional before/after mood ratings.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN mind.meditation_sessions.id IS 'Unique identifier for each meditation session record (primary key).';

COMMENT ON COLUMN mind.meditation_sessions.user_id IS 'References the user who logged this session (foreign key to users.id).';

COMMENT ON COLUMN mind.meditation_sessions.session_date IS 'Date the meditation session took place.';

COMMENT ON COLUMN mind.meditation_sessions.duration_minutes IS 'Length of the session in minutes; must be greater than zero.';

COMMENT ON COLUMN mind.meditation_sessions.meditation_type
IS 'Type of meditation practiced (e.g., Guided, Breathing, Body Scan).';

COMMENT ON COLUMN mind.meditation_sessions.mood_before IS 'Optional self-reported mood before the session, on a 1-5 scale.';

COMMENT ON COLUMN mind.meditation_sessions.mood_after IS 'Optional self-reported mood after the session, on a 1-5 scale.';

COMMENT ON COLUMN mind.meditation_sessions.notes IS 'Optional free-text notes about the session.';

COMMENT ON COLUMN mind.meditation_sessions.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN mind.meditation_sessions.created_at IS 'Timestamp when this session record was created.';
```

### Pattern 3: Migration — `journal_entries` table (`V23__create_journal_entries_table.sql`)

```sql
------------------------------------------------------------
-- JOURNAL ENTRIES TABLE DEFINITION
-- Free-form journal entries with optional mood and category.
------------------------------------------------------------
CREATE TABLE mind.journal_entries (
    id             INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id        INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    entry_date     DATE NOT NULL,
    title          VARCHAR(200),
    content        TEXT NOT NULL,
    mood           SMALLINT CHECK (mood IS NULL OR mood BETWEEN 1 AND 5),
    category       VARCHAR(50),
    status         SMALLINT NOT NULL DEFAULT 1,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
COMMENT ON TABLE mind.journal_entries
IS 'Free-form journal entries with an optional title, mood rating, and category label.';

------------------------------------------------------------
-- COLUMN DESCRIPTIONS
------------------------------------------------------------

COMMENT ON COLUMN mind.journal_entries.id IS 'Unique identifier for each journal entry record (primary key).';

COMMENT ON COLUMN mind.journal_entries.user_id IS 'References the user who wrote this entry (foreign key to users.id).';

COMMENT ON COLUMN mind.journal_entries.entry_date IS 'Date the journal entry was written for.';

COMMENT ON COLUMN mind.journal_entries.title IS 'Optional short title for the entry.';

COMMENT ON COLUMN mind.journal_entries.content IS 'Full text of the journal entry.';

COMMENT ON COLUMN mind.journal_entries.mood IS 'Optional self-reported mood at the time of writing, on a 1-5 scale.';

COMMENT ON COLUMN mind.journal_entries.category
IS 'Optional free-text category label (e.g., Gratitude, Reflection, Goals).';

COMMENT ON COLUMN mind.journal_entries.status IS 'Status flag (1=Active, 0=Deleted).';

COMMENT ON COLUMN mind.journal_entries.created_at IS 'Timestamp when this entry record was created.';
```

### Pattern 4: `MeditationSession` — Model / DTOs / Service / Controller

**Model** (`Models/MeditationSession.cs`):

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("meditation_sessions", Schema = "mind")]
public class MeditationSession
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("session_date")]
    public DateTime SessionDate { get; set; }

    [Required]
    [Column("duration_minutes")]
    public short DurationMinutes { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("meditation_type")]
    public string MeditationType { get; set; } = string.Empty;

    [Column("mood_before")]
    public short? MoodBefore { get; set; }

    [Column("mood_after")]
    public short? MoodAfter { get; set; }

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

**DTOs** (`DTOs/MeditationSessionDTOs.cs`):

```csharp
using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateMeditationSessionRequest
{
    [Required]
    public DateTime SessionDate { get; set; }

    [Required]
    public short DurationMinutes { get; set; }

    [Required]
    [MaxLength(50)]
    public string MeditationType { get; set; } = string.Empty;

    public short? MoodBefore { get; set; }

    public short? MoodAfter { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateMeditationSessionRequest
{
    public DateTime? SessionDate { get; set; }

    public short? DurationMinutes { get; set; }

    [MaxLength(50)]
    public string? MeditationType { get; set; }

    public short? MoodBefore { get; set; }

    public short? MoodAfter { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public short? Status { get; set; }
}

public class MeditationSessionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime SessionDate { get; set; }
    public short DurationMinutes { get; set; }
    public string MeditationType { get; set; } = string.Empty;
    public short? MoodBefore { get; set; }
    public short? MoodAfter { get; set; }
    public string? Notes { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Service** (`Services/MeditationSessionService.cs`):

```csharp
using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IMeditationSessionService
{
    Task<List<MeditationSessionResponse>> GetUserMeditationSessionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<MeditationSessionResponse> CreateMeditationSessionAsync(int userId, CreateMeditationSessionRequest request);
    Task<MeditationSessionResponse?> UpdateMeditationSessionAsync(int userId, int sessionId, UpdateMeditationSessionRequest request);
    Task<bool> DeleteMeditationSessionAsync(int userId, int sessionId);
}

public class MeditationSessionService : IMeditationSessionService
{
    private readonly ApplicationDbContext _context;

    public MeditationSessionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MeditationSessionResponse>> GetUserMeditationSessionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.MeditationSessions.Where(s => s.UserId == userId && s.Status != 0);

        if (startDate.HasValue)
            query = query.Where(s => s.SessionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SessionDate <= endDate.Value);

        return await query
            .OrderByDescending(s => s.SessionDate)
            .Select(s => new MeditationSessionResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                SessionDate = s.SessionDate,
                DurationMinutes = s.DurationMinutes,
                MeditationType = s.MeditationType,
                MoodBefore = s.MoodBefore,
                MoodAfter = s.MoodAfter,
                Notes = s.Notes,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MeditationSessionResponse> CreateMeditationSessionAsync(int userId, CreateMeditationSessionRequest request)
    {
        var session = new MeditationSession
        {
            UserId = userId,
            SessionDate = request.SessionDate,
            DurationMinutes = request.DurationMinutes,
            MeditationType = request.MeditationType,
            MoodBefore = request.MoodBefore,
            MoodAfter = request.MoodAfter,
            Notes = request.Notes,
            Status = 1
        };

        _context.MeditationSessions.Add(session);
        await _context.SaveChangesAsync();

        return new MeditationSessionResponse
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionDate = session.SessionDate,
            DurationMinutes = session.DurationMinutes,
            MeditationType = session.MeditationType,
            MoodBefore = session.MoodBefore,
            MoodAfter = session.MoodAfter,
            Notes = session.Notes,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<MeditationSessionResponse?> UpdateMeditationSessionAsync(int userId, int sessionId, UpdateMeditationSessionRequest request)
    {
        var session = await _context.MeditationSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Status != 0);

        if (session == null)
            return null;

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this meditation session");

        if (request.SessionDate.HasValue) session.SessionDate = request.SessionDate.Value;
        if (request.DurationMinutes.HasValue) session.DurationMinutes = request.DurationMinutes.Value;
        if (request.MeditationType != null) session.MeditationType = request.MeditationType;
        if (request.MoodBefore.HasValue) session.MoodBefore = request.MoodBefore.Value;
        if (request.MoodAfter.HasValue) session.MoodAfter = request.MoodAfter.Value;
        if (request.Notes != null) session.Notes = request.Notes;
        if (request.Status.HasValue) session.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new MeditationSessionResponse
        {
            Id = session.Id,
            UserId = session.UserId,
            SessionDate = session.SessionDate,
            DurationMinutes = session.DurationMinutes,
            MeditationType = session.MeditationType,
            MoodBefore = session.MoodBefore,
            MoodAfter = session.MoodAfter,
            Notes = session.Notes,
            Status = session.Status,
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<bool> DeleteMeditationSessionAsync(int userId, int sessionId)
    {
        var session = await _context.MeditationSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.Status != 0);

        if (session == null)
            return false;

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this meditation session");

        session.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
```

**Controller** (`Controllers/MeditationSessionsController.cs`):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/mind/meditation-sessions")]
[Authorize]
public class MeditationSessionsController : ControllerBase
{
    private readonly IMeditationSessionService _meditationSessionService;

    public MeditationSessionsController(IMeditationSessionService meditationSessionService)
    {
        _meditationSessionService = meditationSessionService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MeditationSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMeditationSessions(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var sessions = await _meditationSessionService.GetUserMeditationSessionsAsync(userId, start_date, end_date);
        return Ok(sessions);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MeditationSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMeditationSession(int userId, [FromBody] CreateMeditationSessionRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var session = await _meditationSessionService.CreateMeditationSessionAsync(userId, request);
        return StatusCode(201, session);
    }

    [HttpPut("{sessionId}")]
    [ProducesResponseType(typeof(MeditationSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMeditationSession(int userId, int sessionId, [FromBody] UpdateMeditationSessionRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var session = await _meditationSessionService.UpdateMeditationSessionAsync(userId, sessionId, request);
            if (session == null)
                return NotFound(new { message = "Meditation session not found" });

            return Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMeditationSession(int userId, int sessionId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _meditationSessionService.DeleteMeditationSessionAsync(userId, sessionId);
            if (!success)
                return NotFound(new { message = "Meditation session not found" });

            return Ok(new { message = "Meditation session deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
```

### Pattern 5: `JournalEntry` — Model / DTOs / Service / Controller

**Model** (`Models/JournalEntry.cs`):

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinPulse.Api.Models;

[Table("journal_entries", Schema = "mind")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("entry_date")]
    public DateTime EntryDate { get; set; }

    [MaxLength(200)]
    [Column("title")]
    public string? Title { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("mood")]
    public short? Mood { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("status")]
    public short Status { get; set; } = 1;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
```

**DTOs** (`DTOs/JournalEntryDTOs.cs`):

```csharp
using System.ComponentModel.DataAnnotations;

namespace FinPulse.Api.DTOs;

public class CreateJournalEntryRequest
{
    [Required]
    public DateTime EntryDate { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public short? Mood { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }
}

public class UpdateJournalEntryRequest
{
    public DateTime? EntryDate { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public short? Mood { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    public short? Status { get; set; }
}

public class JournalEntryResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public short? Mood { get; set; }
    public string? Category { get; set; }
    public short Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Service** (`Services/JournalEntryService.cs`):

```csharp
using Microsoft.EntityFrameworkCore;
using FinPulse.Api.Data;
using FinPulse.Api.DTOs;
using FinPulse.Api.Models;

namespace FinPulse.Api.Services;

public interface IJournalEntryService
{
    Task<List<JournalEntryResponse>> GetUserJournalEntriesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<JournalEntryResponse> CreateJournalEntryAsync(int userId, CreateJournalEntryRequest request);
    Task<JournalEntryResponse?> UpdateJournalEntryAsync(int userId, int entryId, UpdateJournalEntryRequest request);
    Task<bool> DeleteJournalEntryAsync(int userId, int entryId);
}

public class JournalEntryService : IJournalEntryService
{
    private readonly ApplicationDbContext _context;

    public JournalEntryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JournalEntryResponse>> GetUserJournalEntriesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.JournalEntries.Where(e => e.UserId == userId && e.Status != 0);

        if (startDate.HasValue)
            query = query.Where(e => e.EntryDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.EntryDate <= endDate.Value);

        return await query
            .OrderByDescending(e => e.EntryDate)
            .Select(e => new JournalEntryResponse
            {
                Id = e.Id,
                UserId = e.UserId,
                EntryDate = e.EntryDate,
                Title = e.Title,
                Content = e.Content,
                Mood = e.Mood,
                Category = e.Category,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<JournalEntryResponse> CreateJournalEntryAsync(int userId, CreateJournalEntryRequest request)
    {
        var entry = new JournalEntry
        {
            UserId = userId,
            EntryDate = request.EntryDate,
            Title = request.Title,
            Content = request.Content,
            Mood = request.Mood,
            Category = request.Category,
            Status = 1
        };

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();

        return new JournalEntryResponse
        {
            Id = entry.Id,
            UserId = entry.UserId,
            EntryDate = entry.EntryDate,
            Title = entry.Title,
            Content = entry.Content,
            Mood = entry.Mood,
            Category = entry.Category,
            Status = entry.Status,
            CreatedAt = entry.CreatedAt
        };
    }

    public async Task<JournalEntryResponse?> UpdateJournalEntryAsync(int userId, int entryId, UpdateJournalEntryRequest request)
    {
        var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.Status != 0);

        if (entry == null)
            return null;

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to update this journal entry");

        if (request.EntryDate.HasValue) entry.EntryDate = request.EntryDate.Value;
        if (request.Title != null) entry.Title = request.Title;
        if (request.Content != null) entry.Content = request.Content;
        if (request.Mood.HasValue) entry.Mood = request.Mood.Value;
        if (request.Category != null) entry.Category = request.Category;
        if (request.Status.HasValue) entry.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new JournalEntryResponse
        {
            Id = entry.Id,
            UserId = entry.UserId,
            EntryDate = entry.EntryDate,
            Title = entry.Title,
            Content = entry.Content,
            Mood = entry.Mood,
            Category = entry.Category,
            Status = entry.Status,
            CreatedAt = entry.CreatedAt
        };
    }

    public async Task<bool> DeleteJournalEntryAsync(int userId, int entryId)
    {
        var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.Status != 0);

        if (entry == null)
            return false;

        if (entry.UserId != userId)
            throw new UnauthorizedAccessException("Not authorized to delete this journal entry");

        entry.Status = 0;
        await _context.SaveChangesAsync();

        return true;
    }
}
```

**Controller** (`Controllers/JournalEntriesController.cs`):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinPulse.Api.DTOs;
using FinPulse.Api.Services;

namespace FinPulse.Api.Controllers;

[ApiController]
[Route("api/users/{userId}/mind/journal-entries")]
[Authorize]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntryService _journalEntryService;

    public JournalEntriesController(IJournalEntryService journalEntryService)
    {
        _journalEntryService = journalEntryService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : -1;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<JournalEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetJournalEntries(
        int userId,
        [FromQuery] DateTime? start_date = null,
        [FromQuery] DateTime? end_date = null)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var entries = await _journalEntryService.GetUserJournalEntriesAsync(userId, start_date, end_date);
        return Ok(entries);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JournalEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateJournalEntry(int userId, [FromBody] CreateJournalEntryRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        var entry = await _journalEntryService.CreateJournalEntryAsync(userId, request);
        return StatusCode(201, entry);
    }

    [HttpPut("{entryId}")]
    [ProducesResponseType(typeof(JournalEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateJournalEntry(int userId, int entryId, [FromBody] UpdateJournalEntryRequest request)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var entry = await _journalEntryService.UpdateJournalEntryAsync(userId, entryId, request);
            if (entry == null)
                return NotFound(new { message = "Journal entry not found" });

            return Ok(entry);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{entryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteJournalEntry(int userId, int entryId)
    {
        if (GetCurrentUserId() != userId)
            return Forbid();

        try
        {
            var success = await _journalEntryService.DeleteJournalEntryAsync(userId, entryId);
            if (!success)
                return NotFound(new { message = "Journal entry not found" });

            return Ok(new { message = "Journal entry deleted successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
```

### Pattern 6: Shared infrastructure modifications

**`ApplicationDbContext.cs`** — add alongside the existing 13 `DbSet<T>` declarations:

```csharp
public DbSet<MeditationSession> MeditationSessions { get; set; }
public DbSet<JournalEntry> JournalEntries { get; set; }
```

And inside `OnModelCreating`, one block per entity following the exact existing shape:

```csharp
modelBuilder.Entity<MeditationSession>(entity =>
{
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
    entity.HasOne(e => e.User)
          .WithMany(u => u.MeditationSessions)
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<JournalEntry>(entity =>
{
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
    entity.HasOne(e => e.User)
          .WithMany(u => u.JournalEntries)
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

**`User.cs`** — add alongside the existing 13 navigation properties:

```csharp
public virtual ICollection<MeditationSession> MeditationSessions { get; set; } = new List<MeditationSession>();
public virtual ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
```

**`Program.cs`** — add alongside the existing 14 `AddScoped` registrations:

```csharp
builder.Services.AddScoped<IMeditationSessionService, MeditationSessionService>();
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
```

No `[RequiresPlan]` filter is applied anywhere (Mind is open to all authenticated users per DEFINE, matching Body).

---

### Pattern 7: Test patterns

**Builder** (`Helpers/Builders/MeditationSessionBuilder.cs`) — same fluent-builder shape as `MealBuilder`:

```csharp
using Bogus;
using FinPulse.Api.Models;

namespace FinPulse.Tests.Helpers.Builders;

public class MeditationSessionBuilder
{
    private readonly MeditationSession _session;
    private static readonly Faker _faker = new Faker();

    public MeditationSessionBuilder()
    {
        _session = new MeditationSession
        {
            Id = 0,
            UserId = _faker.Random.Int(1, 100),
            SessionDate = _faker.Date.Recent(30),
            DurationMinutes = (short)_faker.Random.Int(5, 60),
            MeditationType = _faker.PickRandom(new[] { "Guided", "Breathing", "Body Scan", "Silent" }),
            MoodBefore = (short)_faker.Random.Int(1, 5),
            MoodAfter = (short)_faker.Random.Int(1, 5),
            CreatedAt = DateTime.UtcNow,
            Status = 1
        };
    }

    public MeditationSessionBuilder WithId(int id) { _session.Id = id; return this; }
    public MeditationSessionBuilder WithUserId(int userId) { _session.UserId = userId; return this; }
    public MeditationSessionBuilder WithSessionDate(DateTime date) { _session.SessionDate = date; return this; }
    public MeditationSessionBuilder AsActive() { _session.Status = 1; return this; }
    public MeditationSessionBuilder AsDeleted() { _session.Status = 0; return this; }
    public MeditationSession Build() => _session;
}
```

`JournalEntryBuilder` follows the identical shape, seeding `EntryDate`, `Title`, `Content` (via `_faker.Lorem.Paragraph()`), `Mood`, and `Category` (via `_faker.PickRandom(new[] { "Gratitude", "Reflection", "Goals" })`).

**ServiceTests** (`UnitTests/Services/MeditationSessionServiceTests.cs` / `JournalEntryServiceTests.cs`) — same structure as `MealServiceTests`: `CreateXAsync_WithValidRequest_CreatesSuccessfully`, `GetUserXAsync_ReturnsOnlyUserX`, `GetUserXAsync_FiltersOutDeletedX`, `GetUserXAsync_FiltersByDateRange`, `UpdateXAsync_WithValidRequest_UpdatesSuccessfully`, `UpdateXAsync_WithWrongUserId_ThrowsUnauthorizedAccessException`, `DeleteXAsync_SoftDeletesX`, `DeleteXAsync_WithWrongUserId_ThrowsUnauthorizedAccessException` — one `[Fact]` per scenario, using `ServiceTestBase`'s InMemory `Context` exactly like `MealServiceTests` does. Add one extra `[Fact]` per resource verifying a `null` mood round-trips correctly (`CreateMeditationSessionAsync_WithNullMood_Succeeds` / `CreateJournalEntryAsync_WithNullMood_Succeeds`), matching AT-008.

**ControllerTests** (`UnitTests/Controllers/MeditationSessionsControllerTests.cs` / `JournalEntriesControllerTests.cs`) — same structure as `MealsControllerTests`: `Mock<IMeditationSessionService>`/`Mock<IJournalEntryService>`, `SetupControllerContext(_sut, userId)` from `ControllerTestBase`, one `[Fact]` per action × {ownership-ok, ownership-forbidden, not-found-where-applicable}.

---

## Data Flow

```text
1. Client sends HTTP request with JWT bearer cookie/header to
   /api/users/{userId}/mind/{resource}[/{id}]
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
   (mind.* tables, V21–V23 — new, created by this feature)
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
| PostgreSQL (`mind` schema, V21–V23, new) | EF Core / Npgsql, direct connection | Existing `DefaultConnection` string, unchanged |
| OpenTelemetry Collector | Auto-instrumented traces/metrics (existing `AddAspNetCoreInstrumentation`/`AddNpgsql`) | None — same OTLP endpoint already configured |

No new external systems are introduced by this feature.

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-----------------|-----------------|
| Migration (live) | Schema, FK, CHECK constraints, idempotency, matching AT-001–AT-004 | `V21`–`V23` applied to running Postgres | `psql`/migration tool, manual, same discipline as `BODY_MODULE_DATABASE` | All 4 schema acceptance tests |
| Unit — Service | Business logic, soft-delete, ownership, date filtering, null-mood handling | 2 files, `UnitTests/Services/*ServiceTests.cs` | xUnit, FluentAssertions, EF Core InMemory (`ServiceTestBase`) | Every public service method, happy + error path, plus null-mood path |
| Unit — Controller | Route/ownership/status-code behavior | 2 files, `UnitTests/Controllers/*ControllerTests.cs` | xUnit, Moq, FluentAssertions (`ControllerTestBase`) | Every action, ownership-ok + ownership-forbidden |
| Live (manual) | Full CRUD lifecycle against real Postgres, matching AT-005–AT-009 | Swagger UI / curl against running `dotnet run` + Postgres | Manual, same discipline as every prior build in this initiative | All 9 acceptance tests from DEFINE |

Unlike `BODY_MODULE_API`, this feature's new xUnit test files are expected to compile and run cleanly as part of `dotnet test FinPulse.Tests` — the 53 pre-existing, unrelated `FinPulse.Tests` errors were fixed in a follow-up pass after `BODY_MODULE_API`'s build (confirmed: 306/306 passing), and DEFINE's assumption A-003 records this as validated. Build should re-confirm with a full `dotnet test` run after adding these files.

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|---------------------|--------|
| Route `userId` ≠ authenticated user | `Forbid()` → `403`, no DB call | No |
| Resource not found (`GET`/`PUT`/`DELETE` by id) | `NotFound()` → `404` with `{ message: "... not found" }` | No |
| Service throws `UnauthorizedAccessException` (defense-in-depth ownership check inside `Update`/`Delete`) | Controller catches, returns `Forbid()` → `403` | No |
| DB constraint violation (`CHECK`, FK) | Unhandled `DbUpdateException` → ASP.NET Core default `500`, matching existing Body/finance resource behavior (no new middleware) | No |

---

## Configuration

No new configuration keys — reuses the existing `ConnectionStrings:DefaultConnection`, `Jwt:*`, and `Otel:ExporterEndpoint` settings from `appsettings.json` unchanged.

---

## Security Considerations

- Every controller requires `[Authorize]` — no anonymous access, matching every existing controller.
- Ownership is enforced on every action via `GetCurrentUserId()` compared against the route `{userId}`, before any DB call — identical to `MealsController`'s proven pattern.
- No `[RequiresPlan]` gate is applied (explicit DEFINE decision) — Mind data is still protected by authentication + ownership like every other resource.
- Soft-delete only (`Status = 0`) — no hard deletes, so no risk of accidental permanent data loss via this API layer.
- DTOs use `[Required]`/`[MaxLength]` to prevent oversized/missing input from reaching the DB layer; the DB's own CHECK/FK constraints remain the final authority.
- Journal `content` is unbounded `TEXT` with no length cap at the DTO level — acceptable since it's user-owned free text behind authentication, no different in risk profile from any other unbounded user-supplied field already accepted elsewhere in the API pipeline.

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | Serilog structured logging via existing `UseSerilogRequestLogging` — no changes needed, applies automatically to the 2 new controllers |
| Metrics | Existing `AddAspNetCoreInstrumentation()`/`AddNpgsql()` OpenTelemetry metrics apply automatically to the new routes/queries |
| Tracing | Existing `AddAspNetCoreInstrumentation()` OpenTelemetry tracing applies automatically — new spans appear in the existing Tempo/Grafana stack (`monitor/`) with no configuration changes |

---

## Pipeline Architecture (if applicable)

Not applicable — this is a REST API + OLTP schema feature, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-25 | design-agent | Initial version. Confirmed migration numbering (V21–V23, resolving DEFINE's A-004) via a live directory listing during this session; confirmed A-001 (nullable CHECK semantics) and A-002 (nullable `short?` mapping) by reasoning from documented SQL/EF Core behavior and existing codebase precedent (`Meal.ProteinGrams`, `Workout.DurationMinutes`) rather than a scratch-project experiment, since neither involves Postgres-computed-value behavior like the Body module's generated column did. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_MIND_MODULE.md`
