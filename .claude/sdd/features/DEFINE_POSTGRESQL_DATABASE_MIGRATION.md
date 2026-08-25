# DEFINE: PostgreSQL Database Migration

> Migrate the Fin Pulse `database/` folder's schema, migrations, and tooling from SQL Server (T-SQL/Flyway) to native PostgreSQL, with no data to preserve and the .NET API explicitly out of scope.

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_DATABASE_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | define-agent |
| **Status** | ✅ Complete (Built) |
| **Clarity Score** | 14/15 |

---

## Problem Statement

The Fin Pulse `database/` folder is built exclusively for SQL Server (T-SQL syntax, `sys.schemas` checks, `sp_addextendedproperty`, `GETDATE()`, `GO` batch separators) via Flyway migrations, which forces every developer and CI job to run SQL Server locally even though the target engine for this project going forward is PostgreSQL.

---

## Target Users

| User | Role | Pain Point |
|------|------|------------|
| Developers running `database/` locally | Backend/database engineer | Must provision and run a SQL Server instance today just to apply migrations, even though PostgreSQL is the intended engine |
| Future API-migration engineer | Backend engineer (downstream, not in this feature) | Needs a clean, idiomatic PostgreSQL schema to target once `api/FinPulse.Api`'s EF Core provider is swapped in a later, separate effort |

---

## Goals

| Priority | Goal |
|----------|------|
| **MUST** | Translate all 9 existing migration files (`V1__create_schemas.sql` through `V8__create_bills_table.sql`, `V12__create_indexes.sql`) into idiomatic PostgreSQL syntax |
| **MUST** | Provide a local Docker Postgres service (`docker-compose.yml`) so `docker compose up flyway` works out of the box against Postgres, mirroring today's SQL Server workflow |
| **SHOULD** | Update supporting tooling config (`.sqlfluff` dialect, `docs/tbls.yml` DSN) and documentation (`README.md`, `docs/FLYWAY_README.md`, `docs/AZURE_DEVOPS_SETUP.md`, `docs/TBLS_DOCS.md`, `.env.example`) to remove SQL Server references |
| **COULD** | Regenerate `docs/schema/*` (tbls-generated ER diagrams/docs) against the new Postgres instance — deferred as a manual follow-up since it requires a live DB connection |

**Priority Guide:**
- **MUST** = MVP fails without this
- **SHOULD** = Important, but workaround exists
- **COULD** = Nice-to-have, cut first if needed

---

## Success Criteria

- [ ] All 9 migration files apply cleanly via `docker compose up flyway` against a fresh local PostgreSQL container, with 0 errors
- [ ] `docker compose run --rm flyway info` reports all 9 migrations as "Success"
- [ ] `docker compose run --rm flyway validate` passes with 0 validation errors
- [ ] `.sqlfluff` lint (dialect `postgres`) run against `database/migrations/` returns 0 violations
- [ ] 0 remaining SQL Server-specific references (T-SQL types, `GETDATE()`, `sp_addextendedproperty`, `GO`, `sqlserver://` DSNs) across `database/` (migrations, config, docs)
- [ ] `docker-compose.yml` brings up a working local PostgreSQL instance with no manual setup beyond `docker compose up`

---

## Acceptance Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AT-001 | Fresh migration run | A clean local environment with only `docker-compose.yml`, `.env` configured for Postgres, and no existing database | `docker compose up flyway` is run | All 9 migrations apply successfully; `flyway_schema_history` shows 9 successful entries; no SQL Server-specific errors occur |
| AT-002 | Idempotent schema creation | The `finance`/`plan`/`investment`/`reporting` schemas already exist from a prior run | `V1__create_schemas.sql` is re-applied (e.g. via `flyway repair` + `migrate` on a fresh but partially-initialized DB) | `CREATE SCHEMA IF NOT EXISTS` succeeds without error, matching the idempotent behavior of the original `IF NOT EXISTS (SELECT * FROM sys.schemas ...)` check |
| AT-003 | Lint passes under Postgres dialect | `.sqlfluff` is set to `dialect = postgres` | `sqlfluff lint database/migrations/` is run | 0 violations reported |
| AT-004 | Table/column documentation preserved | A migration includes a table or column comment (e.g. `bills.due_day`) | The migration is translated | The same descriptive text is present via native `COMMENT ON TABLE` / `COMMENT ON COLUMN`, not lost in translation |

---

## Out of Scope

Explicitly NOT included in this feature:

- **`api/FinPulse.Api` (.NET API) migration** — `Microsoft.EntityFrameworkCore.SqlServer` package, `GETDATE()` calls in `ApplicationDbContext.cs`, SQL Server connection string in `appsettings.json`. Separate, future effort.
- **Data export/import from any existing SQL Server instance** — no production data exists; this is a fresh schema rewrite.
- **Creating the missing migrations documented in the stale README** (V9–V11 bank tables, V13 budget_spending, V14 bill_payments) — these files don't exist today; authoring them is unrelated to an engine migration.
- **Reorganizing tables into the `finance`/`plan`/`investment` schemas** described in the stale `docs/schema/*.md` — the actual migrations put every table in `dbo` today; the Postgres rewrite preserves that reality (`dbo` → `public`) rather than performing a schema restructuring. The `finance`/`plan`/`investment`/`reporting` schemas are still created (for future use) but no table is forced into them.
- **Managed cloud PostgreSQL provisioning** (Azure Database for PostgreSQL, Supabase, RDS, etc.) — local Docker Postgres only.
- **Regenerating `docs/schema/*` tbls output** — requires a live DB connection; tracked as a manual follow-up, not part of this deliverable.
- **Changes to `azure-pipelines*.yml`** — verified via grep that these files contain no SQL Server-specific strings (they consume `FLYWAY_URL`/`DEV_DB_URL` etc. as opaque env vars from Azure DevOps variable groups), so no pipeline YAML changes are anticipated.

---

## Constraints

| Type | Constraint | Impact |
|------|------------|--------|
| Technical | Flyway remains the migration tool (no tool swap) | Design must stay within Flyway's PostgreSQL support (already bundled in the official `flyway/flyway` image) |
| Technical | No production data exists | No data export/import/backfill design needed |
| Technical | Only the 9 existing migration files are in scope | Design must not introduce new tables beyond what V1–V8, V12 already define |
| Scope | `dbo` → `public`, tables not redistributed into `finance`/`plan`/`investment` | Design should not "fix" the schema organization as part of this engine swap |

---

## Technical Context

> Essential context for Design phase - prevents misplaced files and missed infrastructure needs.

| Aspect | Value | Notes |
|--------|-------|-------|
| **Deployment Location** | `database/migrations/`, `database/.sqlfluff`, `database/docs/tbls.yml`, `database/docker-compose.yml`, `database/.env.example`, `database/README.md`, `database/docs/FLYWAY_README.md`, `database/docs/AZURE_DEVOPS_SETUP.md`, `database/docs/TBLS_DOCS.md` | All changes scoped inside `database/`; no changes outside it |
| **KB Domains** | `sql-patterns` (cross-dialect SQL translation, confidence 0.80), `data-modeling` (schema-migration pattern, confidence 0.80) | No KB domain covers SQL Server→PostgreSQL specifically — codebase-pattern-only confidence, Design phase should validate translation choices directly against PostgreSQL docs where the KB is silent |
| **IaC Impact** | Modify existing | `docker-compose.yml` gains a `postgres` service (new local container); `Dockerfile` and `flyway.toml` need no functional changes (official Flyway image already bundles the PostgreSQL JDBC driver) — only a stale comment in `Dockerfile` ("SQL Server Migrations") needs a wording fix |

**Why This Matters:**

- **Location** → Design phase uses correct project structure, prevents misplaced files
- **KB Domains** → Design phase pulls correct patterns from `.claude/kb/`
- **IaC Impact** → Triggers infrastructure planning, avoids "works locally" failures

---

## Data Contract (if applicable)

Not applicable — this is a schema/DDL engine migration, not a data pipeline. No source-system ingestion, volume, or freshness concerns apply; there is no data to move.

---

## Assumptions

| ID | Assumption | If Wrong, Impact | Validated? |
|----|------------|------------------|------------|
| A-001 | `azure-pipelines*.yml` files are engine-agnostic (consume `FLYWAY_URL`/`DEV_DB_URL` as opaque env vars, no hardcoded SQL Server syntax) | Pipeline YAML would need direct edits too, expanding scope | [x] Verified via grep across `database/` — no `sqlserver`/`jdbc` matches in the pipeline files |
| A-002 | A recent PostgreSQL major version (e.g. 17) is acceptable for the local Docker image with no specific version constraint from the user | Would need to pin a different major version in `docker-compose.yml` | [ ] |
| A-003 | The installed/CI `sqlfluff` version supports `dialect = postgres` | Lint step would fail to run entirely rather than reporting violations | [ ] |

**Note:** Validate critical assumptions before DESIGN phase. Unvalidated assumptions become risks.

---

## Clarity Score Breakdown

| Element | Score (0-3) | Notes |
|---------|-------------|-------|
| Problem | 3 | Specific engine, specific folder, specific pain (forced SQL Server dependency) |
| Users | 2 | One clear, present-tense persona (local dev); the second (future API-migration engineer) is a downstream beneficiary rather than an active user of this feature |
| Goals | 3 | MoSCoW-prioritized, each goal traceable to a validated brainstorm decision |
| Success | 3 | Every criterion is testable pass/fail (migration count, lint violations, reference counts) |
| Scope | 3 | Two real discrepancies surfaced and explicitly resolved (missing V9–V14 files; stale schema-organization docs vs. actual `dbo`-only reality) |
| **Total** | **14/15** | |

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
| 1.0 | 2026-08-24 | define-agent | Initial version, derived from `BRAINSTORM_POSTGRESQL_DATABASE_MIGRATION.md`. Resolved one new discrepancy during Define: actual migrations put every table in `dbo` while `docs/schema/*.md` describes an unimplemented `finance`/`plan`/`investment` table layout — user confirmed translating current reality (`dbo` → `public`), not reorganizing. |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_POSTGRESQL_DATABASE_MIGRATION.md`
