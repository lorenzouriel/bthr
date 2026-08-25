# DESIGN: PostgreSQL API Migration

> Technical design for migrating `api/FinPulse.Api` from SQL Server to PostgreSQL (Npgsql), bundled with a .NET 8 → .NET 10 upgrade

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_API_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_POSTGRESQL_API_MIGRATION.md](./DEFINE_POSTGRESQL_API_MIGRATION.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                         api/FinPulse.Api (.NET 10)                       │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│   Program.cs                                                             │
│     builder.Services.AddDbContext<ApplicationDbContext>(o =>             │
│         o.UseNpgsql(ConnectionStrings:DefaultConnection))                │
│             │                                                            │
│             ▼                                                            │
│   ApplicationDbContext : DbContext                                       │
│     ├─ ConfigureConventions: DateTime/DateTime? → UTC value converters   │
│     ├─ OnModelCreating: HasDefaultValueSql("now()") x7 (was GETDATE())   │
│     └─ DbSet<User/Expense/Earning/Bill/Budget/Goal/Investment>           │
│             │                                                            │
│             │  Npgsql wire protocol                                     │
│             ▼                                                            │
│   ┌─────────────────────────────┐                                       │
│   │  PostgreSQL (database/)     │  ← already migrated, host.docker      │
│   │  fin_pulse @ :5432          │    .internal:5432 in Docker, or       │
│   │  public schema, TIMESTAMPTZ │    localhost:5432 for `dotnet run`    │
│   └─────────────────────────────┘                                       │
│                                                                           │
├───────────────────────────────────────────────────────────────────────────┤
│  api/FinPulse.Tests (.NET 10, unaffected by the DB engine):              │
│    CustomWebApplicationFactory → UseInMemoryDatabase (unchanged)         │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| `ApplicationDbContext` | EF Core context; now targets Postgres via Npgsql, normalizes all `DateTime`/`DateTime?` to UTC | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 |
| `Program.cs` DI wiring | Registers the DbContext with the Npgsql provider | EF Core 10 (pulled in transitively by Npgsql 10.0.3) |
| Connection configuration | `appsettings.json` + `.env` supply the Npgsql-format connection string | ADO.NET Npgsql connection string syntax |
| Container build | `.NET 10` SDK/runtime base images | Docker, `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0` |
| Test project | Fully decoupled from the DB engine; only needs framework-version alignment | `Microsoft.EntityFrameworkCore.InMemory` 10.0.4, `Microsoft.AspNetCore.Mvc.Testing` 10.0.0 |

---

## Key Decisions

### Decision 1: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 as the EF Core provider

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** `FinPulse.Api.csproj` references `Microsoft.EntityFrameworkCore.SqlServer` 8.0.11; `Program.cs` calls `options.UseSqlServer(...)`. The database this API talks to is now PostgreSQL.

**Choice:** Replace with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (requires `Microsoft.EntityFrameworkCore` >= 10.0.4, < 11.0.0 — pulled in transitively) and `options.UseNpgsql(...)`.

**Rationale:** Npgsql is the de facto standard, Microsoft-recommended EF Core provider for PostgreSQL. Version 10.0.3 is the current stable release aligned with EF Core 10 / .NET 10, verified live via NuGet search on 2026-08-24 (not assumed from training data) — resolves DEFINE's Assumption A-002.

**Alternatives Rejected:**
1. Pin to an older Npgsql major version compatible with .NET 8, then upgrade later — rejected because the .NET 10 bundling was already confirmed in scope; no reason to do this in two passes.

**Consequences:**
- No direct `Microsoft.EntityFrameworkCore` package reference is needed in the `.csproj` — it arrives transitively via the Npgsql provider package, matching how `Microsoft.EntityFrameworkCore.SqlServer` worked today.
- Connection string syntax changes from SQL Server ADO.NET format (`Server=...;User Id=...`) to Npgsql format (`Host=...;Username=...`).

---

### Decision 2: Global UTC `DateTime` normalization via two `ConfigureConventions` registrations

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** DEFINE (and the brainstorm before it) selected a global EF Core value converter over per-service normalization. Researching the exact API during Design surfaced a correctness gotcha: EF Core's `ConfigureConventions` treats `DateTime` and `DateTime?` as distinct CLR types — registering a converter for `Properties<DateTime>()` does **not** automatically apply it to `Properties<DateTime?>()`. Several fields are nullable (`Investment.MaturityDate`, `Bill.EndDate`), so a single registration would silently leave those unconverted.

**Choice:** Register **two** conversions in `ApplicationDbContext.ConfigureConventions`, one for `DateTime` and one for `DateTime?`, each backed by its own small `ValueConverter` class (nested inside `ApplicationDbContext.cs` to keep the file manifest at 8 files, matching what was already validated with the user in brainstorm/define):

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
}

private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

private sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter() : base(
        v => v.HasValue
            ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
            : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
```

**Rationale:** EF Core does not pass `null` through value converters (a `null` entity value maps straight to a `null` database value and back), so the nullable converter's lambdas only ever run on non-null values in practice — the `HasValue` checks are defensive, matching documented EF Core behavior, not working around it. This was verified against EF Core's value-conversion docs and a known GitHub issue (dotnet/efcore#28085) discussing exactly this nullable-type limitation, rather than assumed.

**Alternatives Rejected:**
1. Register only `Properties<DateTime>()` and hope it cascades to nullable properties — rejected: verified via research that it does not, which would have silently reintroduced the exact bug this feature exists to prevent for `MaturityDate` and `EndDate`.
2. Per-property `HasConversion(...)` calls in `OnModelCreating` for every date column — rejected in brainstorm as the "6 files" alternative; a global convention is still the smaller, structurally-guaranteed fix even with two registrations instead of one.

**Consequences:**
- Any future `DateTime`/`DateTime?` property added to any entity is automatically UTC-normalized — no per-property wiring needed.
- Values already `Kind=Utc` pass through unchanged (idempotent); `Kind=Unspecified` or `Kind=Local` are coerced to `Utc` without shifting the clock value (matches the "trust the caller meant UTC" semantics implied by DEFINE's AT-002).

---

### Decision 3: Keep `Swashbuckle.AspNetCore` (bumped to 10.2.3), do not migrate to native OpenAPI + Scalar

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** Research surfaced that .NET 9+ introduced a Microsoft-recommended native OpenAPI stack (`Microsoft.AspNetCore.OpenApi` for generation + `Scalar.AspNetCore` for the UI), and that Swashbuckle's latest version (10.2.3) is now described as limited to `Microsoft.OpenApi` v2.x. `Program.cs` currently wires Swashbuckle explicitly (`AddEndpointsApiExplorer()` + `AddSwaggerGen(...)` + `UseSwagger()`/`UseSwaggerUI()`).

**Choice:** Bump `Swashbuckle.AspNetCore` to 10.2.3 (confirmed compatible with .NET 10) and leave `Program.cs`'s Swagger wiring structurally unchanged.

**Rationale:** DEFINE scoped this feature as "bump ASP.NET-Core-aligned NuGet packages to their .NET-10-aligned versions" — not as a Swagger-to-Scalar UI migration, which is a distinct, un-scoped feature (different package, different `Program.cs` wiring, different frontend developer experience). Swashbuckle 10.2.3 is confirmed to still work on .NET 10; picking the smallest change consistent with DEFINE's actual scope avoids an unplanned, unreviewed UI-layer change riding along with a backend engine swap.

**Alternatives Rejected:**
1. Migrate to native `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` — rejected as out of DEFINE's scope; a legitimate future feature on its own.

**Consequences:**
- `Microsoft.AspNetCore.OpenApi` (already referenced today for `AddEndpointsApiExplorer()` support) bumps to 10.0.0 alongside Swashbuckle, but its native `AddOpenApi()`/`MapOpenApi()` surface is not adopted in this pass.

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `api/FinPulse.Api/FinPulse.Api.csproj` | Modify | `TargetFramework` → `net10.0`; swap `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3; bump ASP.NET-Core-aligned packages (see Pattern 5) | (general) | None |
| 2 | `api/FinPulse.Api/Program.cs` | Modify | `UseSqlServer` → `UseNpgsql` | (general) | 1 |
| 3 | `api/FinPulse.Api/Data/ApplicationDbContext.cs` | Modify | 7× `GETDATE()`→`now()`; add `ConfigureConventions` + 2 nested UTC value-converter classes (Decision 2) | (general) | 1 |
| 4 | `api/FinPulse.Api/appsettings.json` | Modify | `DefaultConnection` → Npgsql connection-string format | (general) | 1 |
| 5 | `api/.env.example` | Modify | `DB_CONNECTION_STRING` → Npgsql format | (general) | None |
| 6 | `api/FinPulse.Api/Dockerfile` | Modify | `sdk:8.0`/`aspnet:8.0` → `sdk:10.0`/`aspnet:10.0` | (general) | 1 |
| 7 | `api/FinPulse.Tests/FinPulse.Tests.csproj` | Modify | `TargetFramework` → `net10.0`; bump `Microsoft.AspNetCore.Mvc.Testing` → 10.0.0, `Microsoft.EntityFrameworkCore.InMemory` → 10.0.4 (matches the EF Core version Npgsql 10.0.3 pulls in, avoiding NU1605 downgrade warnings) | (general) | 1 |
| 8 | `api/README.md` | Modify | Tech-stack table, badges, prerequisites, connection-string example: SQL Server→PostgreSQL, .NET 8.0→.NET 10.0. Leave the pre-existing "Banking Integration" content untouched | (general) | 1-7 |

**Total Files:** 8

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|-----------------|-----------------|
| (general) | 1–8 | No agent in `.claude/agents/` covers .NET/C#/EF Core/Npgsql work — the 58 available agents are data-engineering, cloud-infra, and Python-focused. Per the Design Confidence Matrix this is a "No KB, no agent match → 0.70 → Research first" case; that research (live NuGet version verification, EF Core `ConfigureConventions` nullable-type behavior) was performed directly in this Design phase (see Decisions 1–3) rather than deferred to an agent that doesn't exist. All 8 files are mechanical, well-specified C#/JSON/Dockerfile/Markdown edits handled directly in Build. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: KB domain overlap (none), file type (`.cs`/`.csproj`/`.json`/Dockerfile/Markdown — no C#-specialized agent exists), purpose keywords

---

## Code Patterns

### Pattern 1: Npgsql provider registration (`Program.cs`)

```csharp
// Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Pattern 2: `ApplicationDbContext.cs` — default-value translation + UTC conversion

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FinPulse.Api.Models;

namespace FinPulse.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Earning> Earnings { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<Investment> Investments { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Expenses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Earning, Bill, Budget, Goal, Investment: same pattern —
        // entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()"); (was "GETDATE()")
        // ...
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcNullableDateTimeConverter() : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
```

### Pattern 3: `appsettings.json` — Npgsql connection string

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fin_pulse;Username=postgres;Password=CHANGE_ME;"
  }
}
```

### Pattern 4: `.env.example` — Npgsql connection string

```env
# Database Connection
DB_CONNECTION_STRING=Host=your-server;Port=5432;Database=fin_pulse;Username=postgres;Password=your-password;
```

### Pattern 5: `FinPulse.Api.csproj` — final package set (versions verified live via NuGet on 2026-08-24)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.2.3" />
  </ItemGroup>

</Project>
```

> `BCrypt.Net-Next` and `Serilog.Sinks.Console` are left at their current pinned versions — framework-agnostic packages, no forced bump per DEFINE's Assumption A-003. Bump only if Build hits an actual restore/compile error.

### Pattern 6: `FinPulse.Tests.csproj` — final package set

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Bogus" Version="35.4.1" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FinPulse.Api\FinPulse.Api.csproj" />
  </ItemGroup>

</Project>
```

> `Bogus`, `coverlet.collector`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`, `Moq`, `xunit`, `xunit.runner.visualstudio` are left unchanged — framework-agnostic, no forced bump per DEFINE's COULD priority and Assumption A-003.

### Pattern 7: `Dockerfile` — .NET 10 base images

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# ...unchanged below this line...

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# ...unchanged below this line...
```

---

## Data Flow

```text
1. Client sends a request with a date field (e.g. POST /api/users/{id}/expenses,
   expenseDate in the JSON body)
   │
   ▼
2. ASP.NET Core model binding deserializes the JSON into the DTO's DateTime
   property (Kind may be Utc, Local, or Unspecified depending on the JSON's
   offset format)
   │
   ▼
3. Service layer maps the DTO onto the EF Core model (Expense.ExpenseDate),
   unchanged from today — no per-service normalization code added
   │
   ▼
4. ApplicationDbContext.SaveChangesAsync() triggers EF Core's value-conversion
   pipeline: UtcDateTimeConverter / UtcNullableDateTimeConverter normalize the
   value to Kind=Utc before Npgsql serializes it
   │
   ▼
5. Npgsql writes the value to the TIMESTAMPTZ column — no InvalidCastException,
   regardless of the original Kind
   │
   ▼
6. On read, the same converters' reverse lambdas mark the value Kind=Utc when
   EF Core materializes the entity
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-----------------|------------------|
| PostgreSQL (`database/`'s already-shipped instance) | Npgsql wire protocol / ADO.NET | `Username`/`Password` via `ConnectionStrings:DefaultConnection` (local dev) or `DB_CONNECTION_STRING` (Docker) |
| NuGet (build-time only) | Package restore | N/A |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Build verification | Both `.csproj` files compile on `net10.0` | `FinPulse.Api.csproj`, `FinPulse.Tests.csproj` | `dotnet build`, `dotnet restore` | AT-004: 0 errors |
| Existing unit/integration suite | All current tests, unmodified | `FinPulse.Tests/**` (InMemory-backed, untouched) | `dotnet test` | AT-003: 0 regressions from today's passing baseline |
| Live DB connectivity | CRUD against the real Postgres instance | Manual/`curl` against a running API + `docker compose up` (database/) | `curl`, Swagger UI | AT-001: successful create/read round-trip |
| DateTime edge case | Submit a date without a UTC offset | Manual/`curl` against `POST /api/users/{id}/expenses` with `"expenseDate": "2026-08-24T10:00:00"` (no `Z`/offset) | `curl` | AT-002: 201 Created, no 500 error |
| Reference sweep | No leftover SQL Server syntax | All of `api/` | `grep -ri "UseSqlServer\|GETDATE()\|SqlServer" api/` | AT-005: 0 matches outside unrelated content |
| Container build | `Dockerfile` builds on .NET 10 base images | `Dockerfile` | `docker build -f FinPulse.Api/Dockerfile .` | AT-004: 0 errors |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|--------------------|--------|
| Npgsql connection failure (wrong host/port/credentials) | Standard EF Core/Npgsql exception surfaces at first DB access; ASP.NET Core's default exception handling returns 500 with the error logged via Serilog | No — fix connection string, restart |
| `DateTime.Kind` mismatch on write | Eliminated by Decision 2's global converters — no longer a runtime error class after this migration | N/A |
| NuGet restore failure (a bumped package has no compatible version) | `dotnet restore` fails at build time, caught before any code runs | No — pin to the last known-compatible version, documented as a blocker |
| Docker build failure (base image pull) | `docker build` fails immediately at the `FROM` layer | No — verify the `10.0` tag exists on the configured registry |

---

## Configuration

| Config Key | Type | Default | Description |
|------------|------|---------|--------------|
| `ConnectionStrings:DefaultConnection` | string | `Host=localhost;Port=5432;Database=fin_pulse;Username=postgres;Password=CHANGE_ME;` | Npgsql connection string (local `dotnet run`) |
| `DB_CONNECTION_STRING` | string | *(set in `.env`, not committed)* | Same, for the Docker Compose path (`ConnectionStrings__DefaultConnection` env var override) |
| `TargetFramework` | string | `net10.0` | Both `.csproj` files |

---

## Security Considerations

- Connection-string credentials continue to flow through `.env` (git-ignored) and `appsettings.json` (placeholder `CHANGE_ME` only) — no change in secret-handling posture from today.
- No TLS/`sslmode` is configured in the Npgsql connection string, matching the local-dev-only scope of this migration (mirrors the DB migration's own local-Docker-only decision) — production hardening (e.g. `sslmode=require`) is a separate, future concern tied to the also-out-of-scope managed-cloud-Postgres decision.
- .NET 10 carries the current LTS/STS security patch baseline, an incidental benefit of the bundled upgrade, not a goal in itself.
- The global UTC value converter (Decision 2) removes a class of silent data-correctness bugs (ambiguous local-time writes) as a side effect of fixing the type-mapping error — a security-adjacent data-integrity improvement.

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | Serilog console logging, unchanged wiring in `Program.cs`; `Microsoft.EntityFrameworkCore` log level stays overridden to `Warning` |
| Metrics | N/A — no metrics pipeline exists today; out of scope for this migration |
| Tracing | N/A |

---

## Pipeline Architecture (if applicable)

Not applicable. DEFINE confirmed this is an application-layer ORM/framework migration, not a data pipeline.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-24 | design-agent | Initial version, derived from `DEFINE_POSTGRESQL_API_MIGRATION.md`. Package versions verified live via NuGet search (not assumed); EF Core nullable-`DateTime?` conversion gotcha discovered and resolved during research (Decision 2); Swashbuckle-vs-native-OpenAPI fork surfaced and resolved in favor of the smaller, in-scope change (Decision 3). |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_POSTGRESQL_API_MIGRATION.md`
