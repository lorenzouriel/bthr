# BRAINSTORM: PostgreSQL Database Migration

> Exploratory session to clarify intent and approach before requirements capture

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_DATABASE_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | brainstorm-agent |
| **Status** | ✅ Complete (Defined) |

---

## Initial Idea

**Raw Input:** "Let's change the whole database to PostgreSQL" (pointed at `database/`)

**Context Gathered:**
- `database/` is a Flyway-managed SQL Server (T-SQL) schema for "Fin Pulse", a personal finance app — schemas `dbo`, `finance`, `plan`, `investment`, `reporting`.
- Only 9 migration files actually exist: `V1__create_schemas.sql`, `V2`–`V8` (users, budgets, goals, earnings, expenses, investments, bills), `V12__create_indexes.sql`.
- `database/README.md` documents V9–V11, V13, V14 (bank_connections, bank_accounts, bank_transactions, bill_payments, budget_spending) — these migration files **do not exist**. The README is stale/aspirational, unrelated to this migration.
- A sibling `api/` folder (`FinPulse.Api`, .NET 8) has SQL Server baked in separately: `Microsoft.EntityFrameworkCore.SqlServer` package, `GETDATE()` defaults in `ApplicationDbContext.cs`, SQL Server connection string in `appsettings.json`. This is a **separate concern**, explicitly out of scope for this pass.
- Tooling around the schema: `.sqlfluff` (dialect `tsql`), `docs/tbls.yml` (tbls schema-doc generator, `sqlserver://` DSN), `docker-compose.yml` (external SQL Server via `FLYWAY_URL`), Azure DevOps pipelines (`azure-pipelines*.yml`).
- No production data exists — this is a fresh schema, not a live system with data to preserve.

**Technical Context Observed (for Define):**

| Aspect | Observation | Implication |
|--------|-------------|-------------|
| Likely Location | `database/migrations/`, `database/.sqlfluff`, `database/docs/tbls.yml`, `database/docker-compose.yml`, `database/.env.example`, `database/README.md` | All changes are scoped inside `database/` |
| Relevant KB Domains | `sql-patterns` (cross-dialect SQL translation), `data-modeling` (schema-migration pattern) | No KB domain covers SQL Server→Postgres specifically (confidence 0.80, codebase-pattern-only) |
| IaC Patterns | Flyway + Docker Compose, Azure DevOps pipelines | Flyway is engine-agnostic (bundles the Postgres JDBC driver already) — no migration-tool change needed |

---

## Discovery Questions & Answers

| # | Question | Answer | Impact |
|---|----------|--------|--------|
| 1 | Scope: `database/` folder only, or full stack including the .NET API (which has SQL Server EF Core baked in)? | Database folder only | API (`FinPulse.Api`) stays on SQL Server / `Microsoft.EntityFrameworkCore.SqlServer` for now — it will need its own follow-up migration later. This is a known, accepted consequence, not an oversight. |
| 2 | Where will PostgreSQL run (local Docker vs managed cloud vs both)? | Local Docker Postgres | Add a `postgres` service to `docker-compose.yml`; connection strings target `localhost:5432`; no cloud-provider-specific config (Azure Database for PostgreSQL, etc.) in this pass. |
| 3 | Is there existing data to migrate, or is this a fresh schema rewrite? | Fresh rewrite, no data | No ETL/backfill/data-export step needed — purely a schema/migration-file rewrite. |
| 4 | Reference samples: existing Postgres style to follow, or derive from the current SQL Server migrations? | Derive from current V1–V8, V12 T-SQL files | Existing migrations are the source of truth for column names, constraints, and comments; only syntax/types translate. |
| 5 | Do the missing migrations (V9–V11, V13, V14 — bank tables, bill_payments, budget_spending) get created as part of this? | No — only translate what exists (V1–V8, V12) | Creating new tables is out of scope; the README discrepancy is a separate, unrelated cleanup. |

**Minimum Questions:** 3 ✅ (5 asked)

---

## Sample Data Inventory

| Type | Location | Count | Notes |
|------|----------|-------|-------|
| Input files | `database/migrations/V1__create_schemas.sql` through `V8__create_bills_table.sql`, `V12__create_indexes.sql` | 9 files | Source-of-truth T-SQL to translate 1:1 into idiomatic PostgreSQL |
| Output examples | N/A | — | No existing Postgres schema to mirror; approach and type mapping validated directly with the user instead (see Incremental Validations) |
| Ground truth | N/A | — | Fresh rewrite, no data to preserve |
| Related code | `database/.sqlfluff`, `database/docs/tbls.yml`, `database/docker-compose.yml`, `database/flyway.toml`, `database/Dockerfile`, `database/.env.example`, `database/README.md` | 7 files | Supporting tooling/config that must be updated alongside the migrations for the engine swap to actually work |

**How samples will be used:**

- The 9 existing migration files are translated line-by-line using the validated type-mapping table below.
- `flyway.toml` and `Dockerfile` need **no changes** — Flyway's official image already bundles the PostgreSQL JDBC driver; only the connection URL (env var) changes.

---

## Approaches Explored

### Approach A: Idiomatic PostgreSQL ⭐ Recommended

**Description:** Rewrite each migration as native, idiomatic PostgreSQL: `dbo` schema renamed to `public` (Postgres' default/idiomatic schema; `finance`/`plan`/`investment`/`reporting` schemas kept as-is), `IDENTITY(1,1)` → `GENERATED ALWAYS AS IDENTITY` (SQL-standard identity columns, Postgres' recommended syntax since v10), `sp_addextendedproperty` → native `COMMENT ON TABLE/COLUMN`, and standard type translations (see mapping table). Also updates `.sqlfluff` dialect, `docs/tbls.yml` DSN, and adds a `postgres` service to `docker-compose.yml`.

**Pros:**
- Clean, idiomatic schema — matches what any Postgres-experienced developer expects
- Best long-term tooling support: sqlfluff, tbls, and Flyway all treat it as native Postgres rather than a ported T-SQL schema
- No SQL Server naming baggage (`dbo`, bracketed reserved-word escaping) to explain or maintain later

**Cons:**
- `dbo.*` → `public.*`/bare-name is a bigger textual diff from the current docs/ER diagrams than a strict 1:1 port

**Why Recommended:** No data to preserve and the API is explicitly out of scope for this pass, so nothing depends on the literal `dbo` name today. This is the cheapest point in the project's life to land on idiomatic Postgres — doing it later (once the API is wired to the DB) would be far more disruptive.

---

### Approach B: Minimal-diff port

**Description:** Keep the schema literally named `dbo` (Postgres permits arbitrary schema names), use the legacy `SERIAL` pseudo-type instead of identity columns for closer syntactic resemblance to `IDENTITY(1,1)`. All other translations same as Approach A.

**Pros:**
- Smaller textual diff — schema/table names stay identical everywhere, easier line-by-line comparison against the old T-SQL during review

**Cons:**
- `SERIAL` is a legacy pseudo-type that PostgreSQL's own documentation steers users away from in favor of identity columns
- Keeping `dbo` in Postgres reads as an unintentional leftover rather than a deliberate choice, and will need renaming later anyway once the API migration happens

---

## Data Engineering Context

Not applicable — this is a schema/DDL engine migration, not a data pipeline. No source-system ingestion, volume, or freshness concerns apply; there is no data to move (confirmed fresh rewrite, no existing rows).

---

## Selected Approach

| Attribute | Value |
|-----------|-------|
| **Chosen** | Approach A — Idiomatic PostgreSQL |
| **User Confirmation** | 2026-08-24, via direct selection |
| **Reasoning** | No data or API dependency ties the schema to `dbo`/`SERIAL` conventions today; better to land on idiomatic Postgres now than retrofit later. |

---

## Key Decisions Made

| # | Decision | Rationale | Alternative Rejected |
|---|----------|-----------|----------------------|
| 1 | Scope limited to `database/` folder | User explicitly excluded the .NET API's SQL Server/EF Core layer | Full-stack migration (database + API) |
| 2 | Local Docker PostgreSQL (`postgres:17` service in docker-compose) | Matches current local-dev pattern; no cloud provisioning needed yet | Managed cloud Postgres (Azure DB for PostgreSQL, Supabase, RDS) |
| 3 | Fresh schema rewrite, no data migration/ETL | No production data exists yet | Data export/import pipeline from SQL Server |
| 4 | Translate only existing files (V1–V8, V12) | README's V9–V11/V13/V14 don't correspond to real files; out of scope | Also authoring the missing bank/bill_payments/budget_spending tables |
| 5 | `dbo` → `public`, `IDENTITY` → `GENERATED ALWAYS AS IDENTITY` (Approach A) | Idiomatic, no dependents on old naming yet | Minimal-diff port keeping `dbo` + `SERIAL` (Approach B) |
| 6 | Flyway retained as migration tool, no Dockerfile/flyway.toml changes | Flyway's official image already bundles the PostgreSQL JDBC driver | Switching migration tooling |

---

## Features Removed (YAGNI)

| Feature Suggested | Reason Removed | Can Add Later? |
|-------------------|----------------|----------------|
| .NET API migration (EF Core provider, connection strings, `GETDATE()` calls in `ApplicationDbContext.cs`) | User explicitly scoped this out; separate concern with its own blast radius | Yes — natural follow-up feature |
| Data export/import from SQL Server | No data exists to preserve | Yes, if data appears before this ships |
| Creating missing bank_connections/bank_accounts/bank_transactions/bill_payments/budget_spending tables | Not part of an engine migration; README discrepancy is unrelated cleanup | Yes — separate feature, in Postgres syntax directly |
| Managed cloud Postgres provisioning (Azure DB for PostgreSQL, etc.) | User chose local Docker only | Yes, when a deployment target is decided |
| Regenerating `docs/schema/*` (tbls-generated ER diagrams/docs) | tbls needs a live DB connection to regenerate; not something to hand-edit | Yes — run `tbls doc` once the new Postgres instance is up, as a manual follow-up |

---

## Incremental Validations

| Section | Presented | User Feedback | Adjusted? |
|---------|-----------|---------------|-----------|
| File-by-file translation plan (which files change, which don't, docs-regen caveat) | ✅ | Confirmed correct; surfaced the V9–V14 file-existence discrepancy, which the user then resolved | No further change needed |
| Type/syntax mapping table (IDENTITY, NVARCHAR, TINYINT, DATETIME, extended properties, GO, schema creation) | ✅ | Approved as-is | No |

**Minimum Validations:** 2 ✅ (2 completed)

---

## Suggested Requirements for /define

### Problem Statement (Draft)
The `database/` folder's schema, migrations, and tooling are built for SQL Server (T-SQL/Flyway) and need to run natively on PostgreSQL instead, with no data to preserve and the API layer explicitly out of scope.

### Target Users (Draft)
| User | Pain Point |
|------|------------|
| Developers running `database/` locally | Need a working Postgres-based local dev loop (docker-compose, Flyway, `.env.example`) instead of SQL Server |
| Future API migration work | Needs a clean, idiomatic Postgres schema to target once the API's EF Core provider is swapped later |

### Success Criteria (Draft)
- [ ] `V1__create_schemas.sql` through `V8__create_bills_table.sql` and `V12__create_indexes.sql` run cleanly against PostgreSQL via `docker compose up flyway`
- [ ] `docker-compose.yml` includes a local `postgres` service that Flyway can connect to out of the box
- [ ] `.sqlfluff` lints the migrations under the `postgres` dialect with no errors
- [ ] `docs/tbls.yml` DSN targets Postgres (regeneration itself is a manual follow-up, not part of this deliverable)
- [ ] `.env.example` and `README.md` reflect PostgreSQL connection strings, prerequisites, and terminology (no more "SQL Server" references)
- [ ] Schema uses `public` (not `dbo`) plus the existing `finance`/`plan`/`investment`/`reporting` schemas, identity columns via `GENERATED ALWAYS AS IDENTITY`, and native `COMMENT ON` for documentation

### Constraints Identified
- No data migration/backfill — fresh schema only
- Flyway remains the migration tool (no tool swap)
- Only the 9 existing migration files are in scope — no new tables authored

### Out of Scope (Confirmed)
- `.NET API` (`api/FinPulse.Api`) — EF Core provider, connection strings, `GETDATE()` usage — separate future migration
- Bank integration tables and `bill_payments`/`budget_spending` (README-documented but never implemented)
- Data export/import from any existing SQL Server instance
- Managed cloud PostgreSQL provisioning
- Regenerating tbls-generated schema docs/diagrams (manual follow-up after the new DB is live)

---

## Session Summary

| Metric | Value |
|--------|-------|
| Questions Asked | 5 |
| Approaches Explored | 2 |
| Features Removed (YAGNI) | 5 |
| Validations Completed | 2 |
| Duration | Single session |

---

## Next Step

**Ready for:** `/define .claude/sdd/features/BRAINSTORM_POSTGRESQL_DATABASE_MIGRATION.md`
