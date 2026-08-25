# BRAINSTORM: PostgreSQL API Migration

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_API_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "now let's apply the changes to the API, let's change all to PostgreSQL" — a direct follow-up to the already-shipped `database/` folder migration (SQL Server → PostgreSQL), now extending to `api/` (the FinPulse.Api .NET backend).

**Context Gathered:**
- `api/` is a .NET 8 ASP.NET Core Web API (`FinPulse.Api`) using EF Core with `Microsoft.EntityFrameworkCore.SqlServer` — the exact dependency the prior `database/` migration deliberately left untouched (out of scope at the time).
- Only **2 files** reference SQL Server directly at the code/config level: `Program.cs` (`options.UseSqlServer(...)`) and `FinPulse.Api.csproj` (the SqlServer package reference). Every `Model` maps through provider-agnostic `[Column]`/`[Table]` attributes.
- `ApplicationDbContext.cs` has 7 `HasDefaultValueSql("GETDATE()")` calls (one per entity: User, Expense, Earning, Bill, Budget, Goal, Investment) that mirror the `GETDATE()`→`now()` swap already done on the database side.
- No EF Core Migrations folder exists — the API is Code-First-without-migrations, mapping onto a schema Flyway (`database/`) already owns and manages independently. No EF migration regeneration is needed.
- `FinPulse.Tests` uses `Microsoft.EntityFrameworkCore.InMemory` exclusively — completely decoupled from the DB engine. **Zero test-project changes needed** for the engine swap itself.
- A real, non-obvious risk surfaced during discovery: the database's columns are now `TIMESTAMPTZ`, but the C# models use `DateTime` (not `DateTimeOffset`). Npgsql (EF Core's Postgres provider) strictly rejects writing a `DateTime` with `Kind=Unspecified` to a `timestamptz` column by default — and several date fields (`ExpenseDate`, `EarningDate`, `PurchaseDate`, `Budget.StartDate/EndDate`, `Goal.DueDate`, `Bill.EndDate`) arrive from user-submitted DTOs, which can carry `Kind=Unspecified`.
- Mid-session, the user expanded scope to bundle a **.NET 8 → .NET 10 upgrade** into this same migration (in response to a version-pinning question), confirmed explicitly after I flagged the full blast radius (both `.csproj` TargetFrameworks, `Dockerfile` base images, all ASP.NET-Core-aligned package versions).
- `api/README.md` documents a "Banking Integration" feature set (BankConnections/BankAccounts/BankTransactions controllers, services, models, 294 tests) that **does not exist in the codebase** — the same stale/aspirational-documentation pattern found in `database/README.md` during the prior migration (V9–V14 bank tables that were never built). Confirms Banking Integration stays out of scope here too.
- `api/docker-compose.yml` and `api/azure-pipelines.yml` are already engine/version-agnostic (env-var-driven, build via `Dockerfile`) — no changes anticipated there.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `api/FinPulse.Api/` (Program.cs, Data/, Models/ untouched structurally, Dockerfile, .csproj), `api/FinPulse.Tests/FinPulse.Tests.csproj`, `api/README.md`, `api/.env.example` | All changes scoped inside `api/` |
| Relevant KB Domains | None directly match .NET/C#/EF Core/Npgsql work in this repo's KB index (checked `.claude/kb/_index.yaml` — closest are `sql-patterns`/`data-modeling`, both SQL-dialect-focused, not ORM/framework-focused) | Confidence 0.70 — Design phase should validate Npgsql/EF Core 10 specifics directly against official docs (e.g. via context7 MCP) rather than relying on KB patterns |
| IaC Impact | Modify existing | `Dockerfile` base images bump (`sdk:8.0`→`10.0`, `aspnet:8.0`→`10.0`); `docker-compose.yml` and `azure-pipelines.yml` need no changes (already generic) |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | How should the API normalize DateTime values so Npgsql accepts writes to TIMESTAMPTZ columns — normalize at the boundary, or enable Npgsql's legacy timestamp behavior switch? | Normalize at the boundary | Rules out the deprecated legacy-behavior escape hatch; commits to an explicit, correct fix |
| 2 | How should the API reach the database for local dev, given `api/docker-compose.yml` and `database/docker-compose.yml` are separate compose projects? | Keep today's `host.docker.internal` pattern | No compose restructuring — `database/docker-compose.yml`'s Postgres (published on host port 5432) is already reachable exactly like the old external SQL Server was |
| 3 | What Npgsql/EF Core package version should be targeted? | (Answer evolved into:) "Let's use dotnet 10" | Expanded scope from a pure DB-provider swap to a bundled .NET 8→10 upgrade |
| 4 | Confirm: bundle the full .NET 10 upgrade (TargetFrameworks, Dockerfile base images, all ASP.NET-Core-aligned packages) into this same migration, or keep it as a separate follow-up? | Bundle both | Locks in the larger scope explicitly, after the full blast radius was surfaced |
| 5 | Which mechanism should normalize DateTime→UTC: a global EF Core value converter, or per-service `DateTime.SpecifyKind` calls? | Global EF Core value converter | Confines the DateTime fix to 1 file (`ApplicationDbContext.cs`) instead of 6 (every service) |

**Minimum Questions:** 3 ✅ (5 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `api/FinPulse.Api/Program.cs`, `Data/ApplicationDbContext.cs`, `FinPulse.Api.csproj`, `appsettings.json`, `.env.example`, `Dockerfile`, `FinPulse.Tests.csproj`, `README.md` | 8 files | Source-of-truth for the translation; each file's current SQL-Server/.NET-8 content was read in full before this document was written |
| Output examples | N/A | — | No existing .NET-10-on-Postgres reference project to mirror; the DateTime-handling and package-version approach were validated directly with the user instead (see Incremental Validations) |
| Ground truth | N/A | — | No data migration involved (mirrors the `database/` migration's "fresh rewrite, no data" decision) |
| Related code | `database/migrations/*.sql` (already-shipped Postgres schema this API must map onto), `.claude/sdd/features/DEFINE_POSTGRESQL_DATABASE_MIGRATION.md` and `DESIGN_POSTGRESQL_DATABASE_MIGRATION.md` (prior migration's decisions this one must stay consistent with) | 2 docs + 9 SQL files | Schema shape (types, nullability, identity columns) the EF models must correctly map to |

**How samples will be used:**

- The already-shipped `database/migrations/*.sql` files are the authoritative schema this API's EF models must correctly read/write against (e.g. confirming `TIMESTAMPTZ` columns, which is exactly what surfaced the DateTime-handling risk).
- The prior DB migration's DEFINE/DESIGN documents establish the connection-string host/port/credentials convention (`localhost:5432`, `fin_pulse` database) the API's `appsettings.json`/`.env.example` should match.

---

## Approaches Explored

### Approach A: Global EF Core value converter for DateTime normalization ⭐ Recommended

**Description:** Override `ConfigureConventions` in `ApplicationDbContext.cs` to apply a model-wide `DateTime` → UTC conversion (EF Core 8+'s `configurationBuilder.Properties<DateTime>().HaveConversion<...>()`), normalizing every `DateTime` property on both read and write, in one place.

**Pros:**
- Touches exactly one file (`ApplicationDbContext.cs`, plus a small converter class)
- Cannot be forgotten when a new service/endpoint is added later — it's structural, not per-call-site discipline
- Matches the pattern the EF Core/Npgsql community has converged on for this exact problem

**Cons:**
- Less visible at the call site than an explicit per-service call — a reader has to know to look in `ConfigureConventions`

**Why Recommended:** Smaller footprint (1 file vs. 6), and structurally impossible to miss for future services — the alternative relies on every future contributor remembering to normalize manually.

---

### Approach B: Per-service `DateTime.SpecifyKind` normalization

**Description:** In each of the 6 services that map a DTO date onto a model (Expense, Earning, Budget, Goal, Investment, Bill), explicitly call `DateTime.SpecifyKind(x, DateTimeKind.Utc)` before assignment.

**Pros:**
- Explicit and visible at the exact call site

**Cons:**
- Touches 6 files instead of 1
- Easy to miss when a new service or endpoint is added later — no structural guarantee
- Duplicated logic across services

---

## Data Engineering Context

Not applicable — this is an application-layer ORM/framework migration (EF Core provider swap + .NET version upgrade), not a data pipeline. No source-system ingestion, volume, or freshness concerns apply.

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — Global EF Core value converter |
| **User Confirmation** | 2026-08-24, via direct selection |
| **Reasoning** | Smaller, structurally-guaranteed fix (1 file) vs. a larger, easy-to-miss fix scattered across 6 services |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Scope bundles PostgreSQL provider swap **and** .NET 8→10 upgrade in one pass | User explicitly confirmed after the full blast radius (csproj TargetFrameworks, Dockerfile base images, ASP.NET-Core-aligned package versions) was surfaced | PostgreSQL-only on .NET 8, with .NET 10 as a separate follow-up cycle |
| 2 | DateTime→UTC normalization via a global EF Core value converter | Structural, 1-file fix that can't be forgotten later | Per-service `DateTime.SpecifyKind` calls (6 files, easy to miss) |
| 3 | Keep `api/docker-compose.yml`'s existing `host.docker.internal` pattern to reach the database's Postgres container | Zero compose restructuring; `database/docker-compose.yml` already publishes Postgres on host port 5432, reachable exactly like the old external SQL Server was | Bridging the two compose projects onto a shared Docker network |
| 4 | Exact Npgsql/ASP.NET-Core-aligned package versions deferred to Design phase | Brainstorm fixes scope and approach, not precise version pins — Design validates against live NuGet/docs | Pinning exact version numbers now, without MCP/docs validation |
| 5 | Banking Integration content in `api/README.md` (and the corresponding stale test-count claims) left untouched | Pre-existing, unrelated stale documentation — same pattern as the `database/README.md` discrepancy found in the prior migration; not something an engine/framework migration should silently "fix" | Correcting the stale Banking Integration docs as part of this migration |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| Migrate all `DateTime` properties to `DateTimeOffset` across Models/DTOs | Breaking API-contract change (changes JSON shape returned to clients), unrelated to swapping the DB engine — the value-converter approach achieves correctness without touching the public API | Yes — separate, deliberately-scoped feature if ever needed |
| Fixing the phantom "Banking Integration" controllers/services/models/tests documented in `api/README.md` | Pre-existing, unrelated inaccuracy (the code was never built) — same category as the `database/README.md` V9–V14 discrepancy from the prior migration | Yes — separate feature, if Banking Integration is ever actually built |
| Bridging `api/docker-compose.yml` and `database/docker-compose.yml` into a shared Docker network | Not needed — the existing `host.docker.internal` pattern already works for the new local Postgres exactly as it did for the old external SQL Server | Yes — if local dev ergonomics ever demand it |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| File-by-file manifest (8 files, what changes in each) | ✅ | Confirmed correct | No |
| Full decision summary (scope, DateTime mechanism, connectivity, versioning, YAGNI) | ✅ | Confirmed correct | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
`api/FinPulse.Api` still targets SQL Server (via `Microsoft.EntityFrameworkCore.SqlServer`) and .NET 8, even though the database it depends on (`database/`) has already been migrated to PostgreSQL — the API needs to catch up to both the new database engine and a bundled .NET 10 upgrade, without breaking its public API contract or its already-passing (InMemory-backed) test suite.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| Developers running `api/` locally against the now-Postgres `database/` | API currently can't connect to the migrated database at all — `UseSqlServer` against a Postgres instance fails outright |
| Future maintainers adding new date-bearing fields/endpoints | Need a structural guarantee (not per-call-site discipline) that DateTime values won't silently fail against `TIMESTAMPTZ` columns |

### Success Criteria (Draft)
- [ ] API successfully connects to and performs CRUD operations against the PostgreSQL database (`database/`'s already-shipped schema) with 0 connection/type-mapping errors
- [ ] All existing tests in `FinPulse.Tests` continue to pass unmodified (InMemory provider is untouched by this migration)
- [ ] Both `.csproj` files target `net10.0`; `Dockerfile` builds successfully on `.NET 10` SDK/runtime base images
- [ ] 0 remaining SQL Server-specific references (`UseSqlServer`, `GETDATE()`, SQL Server connection-string syntax) in `api/`
- [ ] A DateTime value submitted via any date-bearing endpoint (expense, earning, budget, goal, investment, bill) with `Kind=Unspecified` is written to the corresponding `TIMESTAMPTZ` column without throwing

### Constraints Identified
- No EF Core Migrations exist or are needed — the API maps onto a schema Flyway (`database/`) already owns
- Public API contract (JSON request/response shapes) must not change — rules out a `DateTime`→`DateTimeOffset` migration
- Test suite (`Microsoft.EntityFrameworkCore.InMemory`) is out of scope for engine-related changes — only framework-version-alignment package bumps apply

### Out of Scope (Confirmed)
- Fixing the phantom "Banking Integration" controllers/services/models/tests documented in `api/README.md` (pre-existing, unrelated, never built)
- `DateTime`→`DateTimeOffset` migration across Models/DTOs
- Bridging `api/docker-compose.yml` and `database/docker-compose.yml` into a shared Docker network
- Any changes to `database/` (already shipped in the prior migration)
- Data migration/backfill (no production data exists)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 5 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 3 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_POSTGRESQL_API_MIGRATION.md`
