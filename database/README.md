# Fin Pulse Database

> PostgreSQL database schema for a personal finance assistant that helps users track expenses, earnings, investments, and financial goals.

## Overview

Fin Pulse Database provides the data layer for a personal finance application. It manages user financial data across multiple domains:

- **Expenses & Earnings** - Track daily transactions and income sources
- **Investments** - Monitor stocks, crypto, fixed income, and other assets
- **Budgets & Goals** - Set spending limits and savings targets
- **Bank Connections** - Open Banking integration for account syncing

## Quick Start

### Prerequisites

- Docker and Docker Compose (a local PostgreSQL instance is provisioned automatically — no separate install needed)

### Setup

1. Clone the repository and navigate to the database folder:

```bash
git clone <repository-url>
cd fin/database
```

2. Copy the environment template and configure your database connection:

```bash
cp .env.example .env
```

3. Edit `.env` with your PostgreSQL credentials:

```properties
POSTGRES_DB=fin_pulse
POSTGRES_USER=postgres
POSTGRES_PASSWORD=YourStrongPassword123!
FLYWAY_URL=jdbc:postgresql://postgres:5432/fin_pulse
FLYWAY_USER=postgres
FLYWAY_PASSWORD=YourStrongPassword123!
```

4. Run migrations:

```bash
docker compose up
```

### Verify Installation

Check migration status:

```bash
docker compose run --rm flyway info
```

## Database Schema

### Schemas

| Schema | Purpose |
| ------ | ------- |
| `public` | Core tables (users, budgets, goals, investments) |
| `finance` | Financial transactions |
| `plan` | Planning and budgeting |
| `investment` | Investment tracking |
| `reporting` | Reporting views and aggregations |

### Tables

#### Users

| Table | Description |
| ----- | ----------- |
| `users` | Registered application users and their authentication data |

#### Finances

| Table | Description |
| ----- | ----------- |
| `expenses` | User expenses with purchase details, amounts, and payment methods |
| `earnings` | User income from salary, bonuses, and other sources |
| `investments` | Investment records across multiple asset types and platforms |
| `bills` | Recurring payment obligations with due dates and status tracking |
| `bill_payments` | Individual payment records for bills |

#### Planning & Budgeting

| Table | Description |
| ----- | ----------- |
| `budgets` | User-defined financial budgets with spending limits |
| `budget_spending` | Spending records linked to budgets |
| `goals` | Financial goals with target amounts and progress tracking |

#### Banking Integration

| Table | Description |
| ----- | ----------- |
| `bank_connections` | Open Banking credentials and sync status |
| `bank_accounts` | User bank accounts from connected institutions |
| `bank_transactions` | Transactions imported from connected bank accounts |

### Architecture

```text
┌─────────────────────────────────────────────────────────────────────┐
│                              users                                   │
│                         (authentication)                             │
└─────────────────────────────────────────────────────────────────────┘
           │
           ├───────────────┬───────────────┬───────────────┐
           ▼               ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐
│    FINANCES     │ │   PLANNING  │ │ INVESTMENTS │ │     BANKING     │
├─────────────────┤ ├─────────────┤ ├─────────────┤ ├─────────────────┤
│ expenses        │ │ budgets     │ │ investments │ │ bank_connections│
│ earnings        │ │ goals       │ │             │ │ bank_accounts   │
│ bills           │ │ budget_     │ │             │ │ bank_           │
│ bill_payments   │ │  spending   │ │             │ │  transactions   │
└─────────────────┘ └─────────────┘ └─────────────┘ └─────────────────┘
```

## Entity Relationship Diagram

![ER Diagram](docs/schema/schema.svg)

> Full schema documentation: [docs/schema/README.md](docs/schema/README.md)

## Documentation

| Document | Description |
| -------- | ----------- |
| [Schema Reference](docs/schema/README.md) | Full table documentation with column details |
| [Flyway Guide](docs/FLYWAY_README.md) | Migration tool setup and usage |
| [Azure DevOps Setup](docs/AZURE_DEVOPS_SETUP.md) | CI/CD pipeline configuration |
| [tbls Configuration](docs/TBLS_DOCS.md) | Schema documentation generation |

## Migrations

Migrations are managed with [Flyway](https://flywaydb.org/) and follow the naming convention `V{number}__{description}.sql`.

### Current Migrations

| Version | Description |
| ------- | ----------- |
| V1 | Create database schemas |
| V2 | Create users table |
| V3 | Create budgets table |
| V4 | Create goals table |
| V5 | Create earnings table |
| V6 | Create expenses table |
| V7 | Create investments table |
| V8 | Create bills table |
| V9 | Create bank_connections table |
| V10 | Create bank_accounts table |
| V11 | Create bank_transactions table |
| V12 | Create bill_payments table |
| V13 | Create budget_spending table |
| V14 | Create indexes |

### Running Migrations

```bash
# Run all pending migrations
docker compose up flyway

# View migration status
docker compose run --rm flyway info

# Validate migrations
docker compose run --rm flyway validate

# Repair migration history (if needed)
docker compose run --rm flyway repair
```

## Configuration

### Environment Variables

| Variable | Description | Example |
| -------- | ----------- | ------- |
| `POSTGRES_DB` | Local PostgreSQL container database name | `fin_pulse` |
| `POSTGRES_USER` | Local PostgreSQL container user | `postgres` |
| `POSTGRES_PASSWORD` | Local PostgreSQL container password | `YourPassword123!` |
| `FLYWAY_URL` | JDBC connection string | `jdbc:postgresql://postgres:5432/fin_pulse` |
| `FLYWAY_USER` | Database username | `postgres` |
| `FLYWAY_PASSWORD` | Database password | `YourPassword123!` |
| `FLYWAY_SCHEMAS` | Schemas to manage | `public,finance,plan,investment,reporting` |

### Flyway Configuration

The `flyway.toml` file configures migration behavior:

- Migration prefix: `V` (versioned), `R` (repeatable)
- Separator: `__` (double underscore)
- Encoding: UTF-8
- Validation on migrate: enabled
- Clean disabled: true (safety)

## CI/CD

Migrations are automatically deployed via Azure DevOps Pipeline on pushes to the `dev` branch.

### Pipeline Stages

1. **Validate** - Check migration file count and naming
2. **Build** - Build Flyway Docker image
3. **Info** - Display current migration status
4. **Migrate** - Apply pending migrations
5. **Cleanup** - Remove Docker resources

### Pipeline Variables

Configure these in Azure DevOps variable group `FLYWAY-DEV`:

| Variable | Description |
| -------- | ----------- |
| `DEV_DB_URL` | Development database JDBC URL |
| `DEV_DB_USER` | Development database username |
| `DEV_DB_PASSWORD` | Development database password |

## Development

### Adding a New Migration

1. Create a new SQL file in `migrations/`:

```bash
touch migrations/V13__create_new_table.sql
```

2. Write your SQL migration following the existing patterns:

```sql
------------------------------------------------------------
-- TABLE DESCRIPTION
------------------------------------------------------------
CREATE TABLE new_table (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY NOT NULL,
    user_id INT NOT NULL,
    -- additional columns...
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Add a comment for documentation
COMMENT ON TABLE new_table IS 'Description of the table';
```

3. Test locally:

```bash
docker compose run --rm flyway validate
docker compose run --rm flyway migrate
```

### SQL Standards

- Use `INT GENERATED ALWAYS AS IDENTITY` for primary keys
- Include `user_id` foreign key for user-owned data
- Add `status` column (SMALLINT) for soft deletes
- Include `created_at` timestamp with `TIMESTAMPTZ NOT NULL DEFAULT now()`
- Document all tables and columns with `COMMENT ON TABLE` / `COMMENT ON COLUMN`

## Project Structure

```text
database/
├── migrations/              # Flyway SQL migration files
│   ├── V1__create_schemas.sql
│   ├── V2__create_users_table.sql
│   └── ...V14__create_indexes.sql
├── docs/
│   ├── schema/              # Generated schema docs (tbls)
│   │   ├── README.md        # Table index
│   │   ├── schema.svg       # ER diagram
│   │   └── *.md             # Per-table documentation
│   ├── AZURE_DEVOPS_SETUP.md
│   ├── FLYWAY_README.md
│   └── TBLS_DOCS.md
├── .env.example             # Environment template
├── .sqlfluff                # SQL linter config
├── azure-pipelines.yml      # CI/CD pipeline
├── docker-compose.yml       # Docker configuration
├── Dockerfile               # Flyway container
├── flyway.toml              # Flyway configuration
└── README.md
```
