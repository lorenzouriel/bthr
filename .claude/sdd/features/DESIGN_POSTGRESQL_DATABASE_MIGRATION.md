# DESIGN: PostgreSQL Database Migration

> Technical design for translating the `database/` folder's SQL Server (T-SQL/Flyway) schema and tooling to native, idiomatic PostgreSQL

## Metadata

| Attribute | Value |
|-----------|-------|
| **Feature** | POSTGRESQL_DATABASE_MIGRATION |
| **Date** | 2026-08-24 |
| **Author** | design-agent |
| **DEFINE** | [DEFINE_POSTGRESQL_DATABASE_MIGRATION.md](./DEFINE_POSTGRESQL_DATABASE_MIGRATION.md) |
| **Status** | ✅ Complete (Built) |

---

## Architecture Overview

```text
┌───────────────────────────────────────────────────────────────────────────┐
│                    LOCAL DEV / CI — docker compose                        │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│   ┌─────────────────────┐  jdbc:postgresql://postgres:5432/fin_pulse    │
│   │  flyway container   │ ─────────────────────────────┐                │
│   │  (flyway/flyway,    │                               │                │
│   │   unchanged image)  │  depends_on: service_healthy  ▼                │
│   └─────────────────────┘                    ┌─────────────────────┐    │
│             ▲                                 │  postgres container │    │
│             │ mounts                          │  (postgres:17-alpine)│   │
│   ┌─────────────────────┐                     │  volume: pgdata      │   │
│   │ migrations/V1..V12  │                     └─────────────────────┘    │
│   │ (rewritten for PG)  │                               │                │
│   └─────────────────────┘                               │                │
│                                                          ▼                │
│                                          schemas: public, finance,       │
│                                          plan, investment, reporting     │
│                                          table: flyway_schema_history    │
│                                                                           │
├───────────────────────────────────────────────────────────────────────────┤
│  Supporting tooling (config-only changes, no runtime dependency):        │
│    .sqlfluff (dialect=postgres) ──lints──> migrations/*.sql             │
│    docs/tbls.yml (postgres:// DSN) ──(manual, deferred)──> docs/schema/*│
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| PostgreSQL container | Local database engine that migrations apply against | `postgres:17-alpine`, Docker Compose service, named volume for persistence |
| Flyway migration runner | Applies `migrations/*.sql` in version order, tracks state in `flyway_schema_history` | `flyway/flyway` official image (unchanged — already bundles the PostgreSQL JDBC driver) |
| Migration files (V1–V8, V12) | DDL definitions for schemas, tables, indexes | PostgreSQL DDL (rewritten from T-SQL) |
| sqlfluff linter | Static SQL style/dialect validation | `sqlfluff`, `dialect = postgres` |
| tbls doc generator | Schema documentation generator (regeneration deferred per DEFINE scope) | `tbls`, `postgres://` DSN |

---

## Key Decisions

### Decision 1: Identity columns via `GENERATED ALWAYS AS IDENTITY`

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** SQL Server's `INT IDENTITY(1,1) PRIMARY KEY` needs a PostgreSQL equivalent for all 6 tables with a surrogate integer primary key (`users`, `budgets`, `goals`, `earnings`, `expenses`, `investments`, `bills`).

**Choice:** `id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL`

**Rationale:** This is the SQL-standard identity-column syntax PostgreSQL has recommended since v10; it's the closest semantic match to SQL Server's `IDENTITY(1,1)` (auto-incrementing, not user-assignable by default) and keeps the schema idiomatic for anyone who works on it after this migration, including whoever eventually migrates the API's EF Core provider.

**Alternatives Rejected:**
1. `SERIAL` pseudo-type — rejected because PostgreSQL's own documentation steers users toward identity columns; `SERIAL` creates an implicit sequence with looser ownership semantics and is considered legacy.

**Consequences:**
- Sequence values are owned by the column (`GENERATED ALWAYS`), matching SQL Server's non-overridable identity behavior (`GENERATED ALWAYS` rejects explicit inserts into the identity column unless `OVERRIDING SYSTEM VALUE` is used — acceptable since no code inserts explicit IDs today).
- No functional gap versus the SQL Server behavior being replaced.

---

### Decision 2: `dbo` → `public`, no schema reorganization

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** The actual migrations put every table in SQL Server's `dbo` schema, while the `finance`/`plan`/`investment`/`reporting` schemas created in `V1` are never used by any `CREATE TABLE`. Stale tbls-generated docs (`docs/schema/*.md`) describe a different, more organized layout (`plan.budgets`, `finance.expenses`, etc.) that was never actually implemented.

**Choice:** Translate `dbo.*` → bare table names in PostgreSQL's default `public` schema. Keep creating `finance`/`plan`/`investment`/`reporting` via `CREATE SCHEMA IF NOT EXISTS` (unused placeholders, as today) but do not move any table into them.

**Rationale:** User explicitly confirmed (twice — once in brainstorm, once when this discrepancy surfaced in Define) that this migration translates current reality, not the aspirational docs. Reorganizing tables into semantic schemas is a real schema-restructuring decision with its own trade-offs and blast radius (would need review of every query, every EF Core mapping later) — out of scope for "swap the database engine."

**Alternatives Rejected:**
1. Reorganize into `finance`/`plan`/`investment` per the docs — rejected: scope creep beyond an engine migration, and the docs describing this layout are unverified/stale, not a validated target.
2. Keep a schema literally named `dbo` in Postgres — rejected in Brainstorm (Approach B) as unidiomatic and confusing.

**Consequences:**
- `public.users`, `public.budgets`, etc. — Postgres' default schema, so table names can be referenced unqualified.
- `finance`/`plan`/`investment`/`reporting` remain empty but present, ready for a future, separately-scoped reorganization if one is ever decided on.

---

### Decision 3: Local PostgreSQL via a new `docker-compose.yml` service

| Attribute | Value |
|-----------|-------|
| **Status** | Accepted |
| **Date** | 2026-08-24 |

**Context:** Today's `docker-compose.yml` only defines the `findatabase` (Flyway) service and expects an *external* SQL Server instance reachable via `host.docker.internal`. DEFINE requires the whole loop (`docker compose up`) to work with zero manual DB setup.

**Choice:** Add a `postgres` service (`postgres:17-alpine`, named volume `pgdata`, healthcheck via `pg_isready`) to `docker-compose.yml`; the `findatabase` (Flyway) service gets `depends_on: postgres: condition: service_healthy` and its `FLYWAY_URL` points at the in-network hostname `postgres` (container-to-container, not `localhost`).

**Rationale:** Matches DEFINE's "local Docker Postgres" decision and the "no manual setup beyond `docker compose up`" success criterion. A healthcheck-gated `depends_on` avoids the classic race condition where Flyway starts before Postgres is ready to accept connections.

**Alternatives Rejected:**
1. Keep expecting an external Postgres instance (mirroring today's SQL Server setup) — rejected because DEFINE explicitly chose local Docker Postgres over "managed cloud" or "both."

**Consequences:**
- First-time `docker compose up` now provisions the database itself; no separate SQL Server/Postgres install step.
- The named volume (`pgdata`) persists data across container restarts but is local-only — not a concern since there's no data to preserve (fresh rewrite, per DEFINE).

---

## File Manifest

| # | File | Action | Purpose | Agent | Dependencies |
|---|------|--------|---------|-------|--------------|
| 1 | `database/migrations/V1__create_schemas.sql` | Modify | `CREATE SCHEMA IF NOT EXISTS` for finance/plan/investment/reporting (native idempotent syntax, drop `sys.schemas` check) | @schema-designer | None |
| 2 | `database/migrations/V2__create_users_table.sql` | Modify | Translate `users` table: identity column, `VARCHAR`/`TIMESTAMPTZ`, native `COMMENT ON` | @schema-designer | 1 |
| 3 | `database/migrations/V3__create_budgets_table.sql` | Modify | Translate `budgets` table | @schema-designer | 1 |
| 4 | `database/migrations/V4__create_goals_table.sql` | Modify | Translate `goals` table | @schema-designer | 1 |
| 5 | `database/migrations/V5__create_earnings_table.sql` | Modify | Translate `earnings` table | @schema-designer | 1 |
| 6 | `database/migrations/V6__create_expenses_table.sql` | Modify | Translate `expenses` table | @schema-designer | 1 |
| 7 | `database/migrations/V7__create_investments_table.sql` | Modify | Translate `investments` table | @schema-designer | 1 |
| 8 | `database/migrations/V8__create_bills_table.sql` | Modify | Translate `bills` table: `BIT`→`BOOLEAN`, `CHECK` constraint carries over unchanged | @schema-designer | 1 |
| 9 | `database/migrations/V12__create_indexes.sql` | Modify | Translate 6 indexes, drop `GO` separators | @schema-designer | 2,3,4,5,6,7,8 |
| 10 | `database/docker-compose.yml` | Modify | Add `postgres` service + healthcheck + `depends_on`; point `FLYWAY_URL` at it | (general) | None |
| 11 | `database/.sqlfluff` | Modify | `dialect = tsql` → `dialect = postgres` | (general) | None |
| 12 | `database/docs/tbls.yml` | Modify | DSN → `postgres://user:password@host:5432/database?sslmode=disable` | (general) | None |
| 13 | `database/.env.example` | Modify | `FLYWAY_URL` → `jdbc:postgresql://localhost:5432/fin_pulse`; add `POSTGRES_DB`/`POSTGRES_USER`/`POSTGRES_PASSWORD` | (general) | 10 |
| 14 | `database/README.md` | Modify | Replace SQL Server prerequisites/connection examples/migration-authoring template with PostgreSQL equivalents | (general) | 1-13 |
| 15 | `database/docs/FLYWAY_README.md` | Modify | Remove SQL Server-specific wording | (general) | None |
| 16 | `database/docs/AZURE_DEVOPS_SETUP.md` | Modify | Remove SQL Server-specific wording | (general) | None |
| 17 | `database/docs/TBLS_DOCS.md` | Modify | Remove SQL Server-specific wording | (general) | None |
| 18 | `database/Dockerfile` | Modify | Fix stale comment ("SQL Server Migrations" → "PostgreSQL Migrations") | (general) | None |

**Total Files:** 18

---

## Agent Assignment Rationale

> Agents discovered from `.claude/agents/` — Build phase invokes matched specialists.

| Agent | Files Assigned | Why This Agent |
|-------|----------------|-----------------|
| @schema-designer | 1–9 (all 9 migration files) | `kb_domains: [data-modeling, sql-patterns, data-quality]` matches DEFINE's identified KB domains exactly; description explicitly covers "schema evolution" and "modeling decisions" — the closest specialist to DDL translation across dialects. `sql-optimizer` was considered (its description also mentions cross-dialect SQL) but its focus is query *performance* tuning on existing queries, not schema/DDL authoring, so it was not selected. |
| (general) | 10–18 | No agent in `.claude/agents/` covers Docker Compose service config, Flyway/sqlfluff/tbls tooling config, or markdown documentation specifically — `ci-cd-specialist` was considered but its scope is Terraform/Databricks Asset Bundles/Azure DevOps pipeline *code*, not Docker Compose service definitions or doc prose. These are mechanical, low-risk edits handled directly in Build. |

**Agent Discovery:**
- Scanned: `.claude/agents/**/*.md`
- Matched by: KB domain overlap (primary signal here, since this task is pure SQL/DDL translation with no app code), file type, purpose keywords

---

## Code Patterns

### Pattern 1: Idempotent schema creation (replaces `V1`'s `sys.schemas` check)

```sql
-- Pattern: native idempotent schema creation
-- Replaces: IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'x') BEGIN EXEC('CREATE SCHEMA [x]') END; GO

CREATE SCHEMA IF NOT EXISTS finance;
CREATE SCHEMA IF NOT EXISTS plan;
CREATE SCHEMA IF NOT EXISTS reporting;
CREATE SCHEMA IF NOT EXISTS investment;
```

### Pattern 2: Table translation (canonical example — `users`, applies to all 6 tables)

```sql
-- Pattern: table + identity PK + native comments
-- Replaces: CREATE TABLE dbo.users (... IDENTITY(1,1) ...) + EXEC sp_addextendedproperty (per column)

CREATE TABLE users
(
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    username      VARCHAR(100) NOT NULL,
    phone_number  VARCHAR(15),
    email         VARCHAR(100) NOT NULL,
    password      VARCHAR(1024),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    plan          SMALLINT NOT NULL DEFAULT 0,
    status        SMALLINT NOT NULL DEFAULT 1
);

COMMENT ON TABLE users IS 'Stores registered application users and their authentication data.';

COMMENT ON COLUMN users.id IS 'Unique identifier for each user (primary key).';
COMMENT ON COLUMN users.username IS 'Public username chosen by the user (unique within the system).';
COMMENT ON COLUMN users.phone_number IS 'Optional phone number used for contact or verification.';
COMMENT ON COLUMN users.email IS 'Primary email address of the user (used for login and notifications).';
COMMENT ON COLUMN users.password IS 'Hashed password for authentication (never stored in plain text).';
COMMENT ON COLUMN users.created_at IS 'Date and time when the user record was created.';
COMMENT ON COLUMN users.plan IS 'Subscription plan (0=Freemium, 1=Basic).';
COMMENT ON COLUMN users.status IS 'User status flag (1 = active, 0 = inactive, others for future states).';
```

> Note: `password`, `plan`, and `status` are **not** reserved words in PostgreSQL — the SQL Server `[bracket]` escaping used for them is dropped rather than translated to `"double quotes"`.

### Pattern 3: `BIT`/`CHECK` translation (`bills` table specifics from `V8`)

```sql
-- Pattern: BIT -> BOOLEAN, CHECK constraint carries over unchanged, DATE stays DATE
is_recurrent    BOOLEAN NOT NULL DEFAULT true,
end_date        DATE,
...
CONSTRAINT ck_bills_due_day CHECK (due_day BETWEEN 1 AND 31)
```

### Pattern 4: Index translation (`V12`, no `GO` needed)

```sql
-- Pattern: index creation, unchanged syntax minus GO batch separators
CREATE INDEX ix_bills_user_id ON bills (user_id);
CREATE INDEX ix_budgets_user_id ON budgets (user_id);
CREATE INDEX ix_goals_user_id ON goals (user_id);
CREATE INDEX ix_investments_user_id ON investments (user_id);
CREATE INDEX ix_expenses_user_id ON expenses (user_id);
CREATE INDEX ix_earnings_user_id ON earnings (user_id);
```

### Pattern 5: `docker-compose.yml` — local Postgres service

```yaml
# config.yaml structure — new service added to database/docker-compose.yml
services:
  postgres:
    image: postgres:17-alpine
    container_name: findatabase-postgres
    environment:
      - POSTGRES_DB=${POSTGRES_DB}
      - POSTGRES_USER=${POSTGRES_USER}
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 5

  findatabase:
    # ...existing build/image/volumes unchanged...
    environment:
      - FLYWAY_URL=jdbc:postgresql://postgres:5432/${POSTGRES_DB}
      - FLYWAY_USER=${POSTGRES_USER}
      - FLYWAY_PASSWORD=${POSTGRES_PASSWORD}
      - FLYWAY_SCHEMAS=public,finance,plan,investment,reporting
      - FLYWAY_DEFAULT_SCHEMA=public
      - FLYWAY_CREATE_SCHEMAS=true
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  pgdata:
```

---

## Data Flow

```text
1. Developer runs `docker compose up` (or `up -d postgres` then `up flyway`)
   │
   ▼
2. `postgres` container starts, initializes `fin_pulse` DB, healthcheck begins polling `pg_isready`
   │
   ▼
3. `findatabase` (Flyway) container waits on `depends_on: service_healthy`, then connects via
   `jdbc:postgresql://postgres:5432/fin_pulse`
   │
   ▼
4. Flyway applies migrations/ in version order: V1 → V2 → V3 → V4 → V5 → V6 → V7 → V8 → V12,
   recording each in `flyway_schema_history`
   │
   ▼
5. (Dev-time, independent of the above) `sqlfluff lint database/migrations/` validates syntax
   against `dialect = postgres`
   │
   ▼
6. (Manual, deferred per DEFINE) `tbls doc` connects live to the running Postgres instance to
   regenerate `docs/schema/*`
```

---

## Integration Points

| External System | Integration Type | Authentication |
|-----------------|-----------------|-----------------|
| PostgreSQL (local Docker container) | Native Postgres wire protocol / JDBC | `POSTGRES_USER`/`POSTGRES_PASSWORD` via `.env` (local dev only) |
| Flyway CLI (Docker image, unchanged) | JDBC (`org.postgresql.Driver`, bundled in `flyway/flyway`) | `FLYWAY_USER`/`FLYWAY_PASSWORD` env vars |
| sqlfluff (dev/CI) | CLI static analysis, no live connection | N/A |
| tbls (manual/CI, deferred) | DSN reflection against a live Postgres instance | `postgres://` DSN with credentials |

---

## Testing Strategy

| Test Type | Scope | Files | Tools | Coverage Goal |
|-----------|-------|-------|-------|-----------------|
| Migration apply | All 9 migration files, fresh DB | `database/migrations/*.sql` | `docker compose up flyway` + `docker compose run --rm flyway info` | AT-001: 9/9 migrations report "Success" |
| Idempotent schema creation | `V1` re-applied against a partially-initialized DB | `database/migrations/V1__create_schemas.sql` | `docker compose run --rm flyway migrate` run twice | AT-002: no error on second run |
| Lint | All migration files under the new dialect | `database/.sqlfluff` | `sqlfluff lint database/migrations/` | AT-003: 0 violations |
| Comment preservation | Spot-check translated `COMMENT ON` output | `database/migrations/V8__create_bills_table.sql` | `psql -c "\d+ bills"` or `docker compose run --rm flyway info` + manual `psql` inspection | AT-004: descriptive text matches the original `sp_addextendedproperty` values |
| Reference sweep | Confirm no SQL Server terms remain | All 18 manifest files | `grep -ri "sqlserver\|sql server\|jdbc:sqlserver\|GETDATE\|sp_addextendedproperty" database/` | DEFINE success criterion: 0 matches |

---

## Error Handling

| Error Type | Handling Strategy | Retry? |
|------------|--------------------|--------|
| Migration SQL syntax error | Flyway aborts the run; PostgreSQL DDL is transactional per statement group, so a failed migration leaves `flyway_schema_history` clean for that version | No — fix the SQL, rerun `migrate` |
| Postgres not yet accepting connections | `depends_on: condition: service_healthy` on the `findatabase` service blocks Flyway from starting until `pg_isready` succeeds | Yes — Docker Compose itself retries the healthcheck per its configured interval |
| Partial `V1` re-run (schemas already exist) | `CREATE SCHEMA IF NOT EXISTS` is a no-op on existing schemas | No retry needed — idempotent by design |
| sqlfluff reports a violation | Build/CI step fails visibly; fix the flagged file | No — human fixes and reruns lint |

---

## Configuration

| Config Key | Type | Default | Description |
|------------|------|---------|--------------|
| `POSTGRES_DB` | string | `fin_pulse` | Local Postgres container database name |
| `POSTGRES_USER` | string | `postgres` | Local Postgres container user |
| `POSTGRES_PASSWORD` | string | *(set in `.env`, not committed)* | Local Postgres container password |
| `FLYWAY_URL` | string | `jdbc:postgresql://postgres:5432/fin_pulse` | Flyway JDBC connection string (container-network hostname) |
| `FLYWAY_SCHEMAS` | string | `public,finance,plan,investment,reporting` | Schemas Flyway manages (replaces the old `dbo,finance,plan,investment,reporting` list) |
| `FLYWAY_DEFAULT_SCHEMA` | string | `public` | Replaces the old `dbo` default |

---

## Security Considerations

- `.env` continues to hold real credentials and stays git-ignored (already the case in `database/.gitignore`) — only `.env.example` (placeholder values) is committed.
- The local `postgres` container's port `5432` is published to the host for developer convenience; this is a local-dev-only compose file, not a production deployment, so this is an accepted trade-off, not a hardening gap.
- No PII/production data is migrated — DEFINE confirmed this is a fresh schema rewrite, so there is no data-in-transit or data-at-rest exposure to design for.
- `flyway.toml`'s `cleanDisabled = true` (destructive `flyway clean` disabled) carries over unchanged — no regression in migration safety.

---

## Observability

| Aspect | Implementation |
|--------|-----------------|
| Logging | Flyway console output (`loggers = "console"` in `flyway.toml`, unchanged) |
| Metrics | N/A — local dev/CI tooling, no metrics pipeline |
| Tracing | N/A |

---

## Pipeline Architecture (if applicable)

Not applicable. DEFINE's Data Contract section confirmed this is a schema/DDL engine migration, not a data pipeline — no DAG, partitioning, incremental-load, or data-quality-gate design is needed.

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-08-24 | design-agent | Initial version, derived from `DEFINE_POSTGRESQL_DATABASE_MIGRATION.md` |

---

## Next Step

**Ready for:** `/ship .claude/sdd/features/DEFINE_POSTGRESQL_DATABASE_MIGRATION.md`
