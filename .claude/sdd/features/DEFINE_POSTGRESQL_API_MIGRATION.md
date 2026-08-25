# DEFINE: PostgreSQL API Migration

> Migrate `api/FinPulse.Api` off SQL Server (EF Core `Microsoft.EntityFrameworkCore.SqlServer`) onto PostgreSQL (Npgsql), bundled with a .NET 8 → .NET 10 upgrade, so the API can connect to the already-migrated `database/` schema.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_API_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 15/15 |

---

## Problem Statement

`api/FinPulse.Api` still targets SQL Server (via `Microsoft.EntityFrameworkCore.SqlServer`) and .NET 8, but the database it depends on (`database/`) has already been migrated to PostgreSQL — the API cannot connect to its own database at all until it catches up, and the user has bundled a .NET 8 → .NET 10 upgrade into the same effort.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| Developers running `api/` locally against the now-Postgres `database/` | Backend engineer | `UseSqlServer` against a PostgreSQL instance fails outright — the API is currently non-functional against its real database |
| Future maintainers adding new date-bearing fields/endpoints | Backend engineer (downstream) | Need a structural guarantee (not per-call-site discipline) that `DateTime` values won't silently fail against `TIMESTAMPTZ` columns |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Swap the EF Core provider from `Microsoft.EntityFrameworkCore.SqlServer` to `Npgsql.EntityFrameworkCore.PostgreSQL`, update `Program.cs` (`UseSqlServer` → `UseNpgsql`) and connection-string formats (`appsettings.json`, `.env.example`) |
| **MUST** | Translate `ApplicationDbContext.cs`'s 7 `HasDefaultValueSql("GETDATE()")` calls to `"now()"` |
| **MUST** | Add a global EF Core value converter (`ConfigureConventions`) that normalizes every `DateTime` property to `Kind=Utc`, so writes to `TIMESTAMPTZ` columns never fail regardless of the incoming `Kind` |
| **MUST** | Upgrade both `.csproj` files (`FinPulse.Api`, `FinPulse.Tests`) to `TargetFramework net10.0`, and the `Dockerfile`'s base images (`sdk:8.0`→`10.0`, `aspnet:8.0`→`10.0`) |
| **SHOULD** | Bump ASP.NET-Core-aligned NuGet packages (Serilog.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.OpenApi, Swashbuckle.AspNetCore, Microsoft.AspNetCore.Mvc.Testing) to their .NET 10-aligned versions |
| **SHOULD** | Update `api/README.md`'s tech-stack table, badges, prerequisites, and connection-string example (SQL Server → PostgreSQL, .NET 8.0 → .NET 10.0) — leaving the pre-existing, unrelated "Banking Integration" content untouched |
| **COULD** | Re-verify non-ASP.NET-Core-aligned test packages (Bogus, FluentAssertions, Moq, xunit, BCrypt.Net-Next) still resolve cleanly on .NET 10, bumping only if a compatibility issue actually surfaces |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] API successfully performs CRUD operations against the PostgreSQL database (`database/`'s already-shipped schema) with 0 connection or type-mapping errors
- [ ] All existing tests in `FinPulse.Tests` continue to pass unmodified — 0 test regressions from this migration (the InMemory provider itself is untouched)
- [ ] Both `.csproj` files target `net10.0`; `dotnet build`/`dotnet publish` succeed; `Dockerfile` builds successfully on .NET 10 SDK/runtime base images
- [ ] 0 remaining SQL Server-specific references (`UseSqlServer`, `GETDATE()`, SQL Server ADO.NET connection-string syntax, `Microsoft.EntityFrameworkCore.SqlServer` package reference) anywhere in `api/`
- [ ] A `DateTime` value with `Kind=Unspecified` submitted via any date-bearing endpoint (expense, earning, budget, goal, investment, bill) is written to its `TIMESTAMPTZ` column without throwing

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | API connects and performs CRUD against Postgres | The `database/` PostgreSQL instance is running (per the already-shipped migration) and the API's connection string points at it | The API starts and a client calls any resource endpoint (e.g. `POST /api/users/{id}/expenses`) | The request succeeds; the row is persisted and readable back with correct values |
| AT-002 | Unspecified-Kind DateTime write succeeds | A client submits an expense with `expenseDate` lacking a UTC offset (producing `Kind=Unspecified` after JSON deserialization) | The service saves the entity via `ApplicationDbContext` | No `Npgsql`/`InvalidCastException` is thrown; the value is persisted correctly in the `TIMESTAMPTZ` column |
| AT-003 | Existing test suite unaffected | `FinPulse.Tests` (InMemory-provider-backed) as it exists today | `dotnet test` is run after the migration | All previously-passing tests still pass; 0 regressions |
| AT-004 | .NET 10 build and container build succeed | The migrated `.csproj` files and `Dockerfile` | `dotnet build`, `dotnet publish`, and `docker build -f FinPulse.Api/Dockerfile .` are run | All three succeed with 0 errors |
| AT-005 | No leftover SQL Server references | The full `api/` directory after migration | `grep -ri "UseSqlServer\|GETDATE()\|SqlServer" api/` (excluding unrelated matches) is run | 0 matches outside of intentionally-unchanged, unrelated content |

---

## Out of Scope

Explicitly NOT included in this feature:

- **Fixing the phantom "Banking Integration" controllers/services/models/tests** documented in `api/README.md` (`BankConnectionsController`, `BankAccountsController`, `BankTransactionsController`, and related services/models) — this code was never actually built; the README is stale/aspirational, the same pattern found in `database/README.md` during the prior migration. Not something this migration should "fix" as a side effect.
- **`DateTime` → `DateTimeOffset` migration** across Models/DTOs — a breaking API-contract change (alters JSON response shapes) unrelated to swapping the DB engine. The global value-converter approach achieves correctness without touching the public API.
- **Bridging `api/docker-compose.yml` and `database/docker-compose.yml`** into a shared Docker network — the existing `host.docker.internal` pattern already reaches the new local Postgres exactly as it reached the old external SQL Server.
- **Any changes to `database/`** — already shipped in the prior `POSTGRESQL_DATABASE_MIGRATION` feature.
- **Data migration/backfill** — no production data exists.
- **EF Core Migrations authoring** — none exist today and none are needed; the API maps onto a schema Flyway (`database/`) already owns independently.
- **Pinning exact NuGet package version numbers in this document** — Design phase validates precise `Npgsql.EntityFrameworkCore.PostgreSQL`/ASP.NET-Core-aligned versions against live NuGet/docs.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | No EF Core Migrations exist or are needed | Design must not introduce a migrations workflow — the API maps onto Flyway's schema |
| Technical | Public API JSON contract (request/response shapes) must not change | Rules out `DateTime`→`DateTimeOffset` and any DTO field renames |
| Technical | `FinPulse.Tests` uses `Microsoft.EntityFrameworkCore.InMemory`, decoupled from the DB engine | Only framework-version-alignment package bumps apply to the test project — no engine-related test changes |
| Scope | Bundled .NET 8→10 upgrade, confirmed explicitly by the user after the full blast radius was surfaced | Design must cover both `.csproj` TargetFrameworks, `Dockerfile` base images, and ASP.NET-Core-aligned package versions — not just Npgsql |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `api/FinPulse.Api/FinPulse.Api.csproj`, `api/FinPulse.Api/Program.cs`, `api/FinPulse.Api/Data/ApplicationDbContext.cs`, `api/FinPulse.Api/appsettings.json`, `api/FinPulse.Api/Dockerfile`, `api/FinPulse.Tests/FinPulse.Tests.csproj`, `api/.env.example`, `api/README.md` | All changes scoped inside `api/`; no changes to `database/` |
| **KB Domains** | None in `.claude/kb/_index.yaml` directly cover .NET/C#/EF Core/Npgsql (closest are `sql-patterns`/`data-modeling`, both SQL-dialect-focused, not ORM/framework-focused) | Confidence 0.70 — Design phase should validate Npgsql/EF Core 10/.NET 10 specifics directly against official docs (e.g. context7 MCP) rather than relying on KB patterns |
| **IaC Impact** | Modify existing | `Dockerfile` base images bump (`sdk:8.0`→`10.0`, `aspnet:8.0`→`10.0`); `docker-compose.yml` and `azure-pipelines.yml` (both in `api/`) need no changes — already env-var-driven and version-agnostic |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is an application-layer ORM/framework migration (EF Core provider swap + .NET version upgrade), not a data pipeline. No source-system ingestion, volume, or freshness concerns apply.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | ASP.NET-Core-aligned packages (Serilog.AspNetCore, JwtBearer, OpenApi, Swashbuckle.AspNetCore, Mvc.Testing) have stable .NET-10-compatible releases available on NuGet | Design would need to find alternative packages or temporarily pin mixed-target versions | [ ] |
| A-002 | `Npgsql.EntityFrameworkCore.PostgreSQL` has a stable release compatible with EF Core 10 / .NET 10 (GA was ~9 months before this document's date) | Design would need to pin a preview Npgsql package or delay the .NET 10 bundling for this package specifically | [ ] |
| A-003 | Non-ASP.NET-Core-aligned packages (BCrypt.Net-Next, Bogus, FluentAssertions, Moq, xunit) work on .NET 10 without version bumps, since they're framework-version-agnostic utility/testing libraries | Would need version bumps for these too, expanding the file/package footprint slightly | [ ] |
| A-004 | Swashbuckle.AspNetCore's Swagger generation works correctly with `TIMESTAMPTZ`-mapped `DateTime` properties without additional schema-filter configuration | Swagger UI might show incorrect type hints for date fields (cosmetic risk, not a correctness/functional risk) | [ ] |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific, verifiable (API literally cannot connect to its own database today), directly continues the already-shipped DB migration |
| Users | 3 | Two personas with concrete, present-tense pain points |
| Goals | 3 | MoSCoW-prioritized, each traceable to a validated brainstorm decision (approach comparison + explicit user confirmations) |
| Success | 3 | Every criterion is testable pass/fail (connection success, test-suite pass rate, build success, reference counts, exception-free write) |
| Scope | 3 | Seven explicit out-of-scope items, including two real discrepancies (phantom Banking Integration docs, DateTimeOffset temptation) explicitly resolved |
| **Total** | **15/15** | |

**Scoring Guide:**
- 0 = Missing entirely
- 1 = Vague or incomplete
- 2 = Clear but missing details
- 3 = Crystal clear, actionable

**Minimum to proceed: 12/15**

---

## Open Questions

None - ready for Design.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-24 | define-agent | Initial version, derived from `BRAINSTORM_POSTGRESQL_API_MIGRATION.md`. Continues directly from the already-shipped `POSTGRESQL_DATABASE_MIGRATION` feature. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_POSTGRESQL_API_MIGRATION.md`
