# Flyway Database Migration Setup
This project uses [Flyway](https://flywaydb.org/) for managing SQL Server database migrations in a version-controlled, automated manner.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Best Practices](#best-practices)

## Prerequisites

You need ONE of the following:

### Option 1: Flyway CLI (Recommended for local development)
- Download from: https://flywaydb.org/download
- Or install via package manager:
  ```bash
  # Windows (Chocolatey)
  choco install flyway

  # macOS (Homebrew)
  brew install flyway

  # Linux (apt)
  wget -qO- https://repo1.maven.org/maven2/org/flywaydb/flyway-commandline/10.10.0/flyway-commandline-10.10.0-linux-x64.tar.gz | tar xvz
  sudo ln -s `pwd`/flyway-10.10.0/flyway /usr/local/bin
  ```

### Option 2: Docker (Recommended for CI/CD)
- Docker Desktop or Docker Engine
- Docker Compose (optional, but recommended)

### Database Requirements
- SQL Server 2019+ (running externally, not in Docker)
- Database user with appropriate permissions (CREATE, ALTER, DROP, SELECT, INSERT, UPDATE, DELETE)

## Quick Start

### 1. Clone and Setup

```bash
cd C:\WAVES\fin\database

# Copy environment template
copy .env.example .env

# Edit .env with your SQL Server credentials
notepad .env
```

### 2. Configure Database Connection

Edit `.env` file:

```env
FLYWAY_URL=jdbc:sqlserver://localhost:1433;databaseName=FinPulse;encrypt=true;trustServerCertificate=true
FLYWAY_USER=sa
FLYWAY_PASSWORD=YourActualPassword
```

### 3. Run Migrations

**Using Flyway CLI:**
```bash
# View migration status
flyway info

# Run pending migrations
flyway migrate

# Validate migrations
flyway validate
```

**Using Docker:**
```bash
# Run migrations using Docker Compose
docker-compose up flyway

# Or build and run manually
docker build -t finpulse-flyway .
docker run --rm \
  -e FLYWAY_URL="jdbc:sqlserver://host.docker.internal:1433;databaseName=FinPulse;encrypt=true;trustServerCertificate=true" \
  -e FLYWAY_USER="sa" \
  -e FLYWAY_PASSWORD="YourPassword" \
  finpulse-flyway migrate
```

## Configuration

### flyway.toml

The main configuration file. Key settings:

```toml
[flyway]
url = "${FLYWAY_URL}"                   # Database JDBC URL
user = "${FLYWAY_USER}"                 # Database user
password = "${FLYWAY_PASSWORD}"         # Database password
locations = ["filesystem:migrations"]   # Migration files location
schemas = ["dbo", "finance", "plan", "investment", "reporting"]
defaultSchema = "dbo"
createSchemas = true                    # Auto-create schemas
table = "flyway_schema_history"        # Migration history table
```

### Environment Variables

All sensitive data is stored in `.env` (never committed to git):

```env
FLYWAY_URL=jdbc:sqlserver://<host>:<port>;databaseName=<db>;encrypt=true;trustServerCertificate=true
FLYWAY_USER=<username>
FLYWAY_PASSWORD=<password>
```

## Usage

### Common Flyway Commands

```bash
# View migration status
flyway info

# Run all pending migrations
flyway migrate

# Validate applied migrations against available ones
flyway validate

# Generate baseline (for existing databases)
flyway baseline -baselineVersion=0 -baselineDescription="Initial baseline"

# Clean database (DANGEROUS - only for dev!)
flyway clean  # Disabled by default in flyway.toml

# Repair migration history
flyway repair
```

### Creating New Migrations

**Naming Convention:**
- Versioned: `V<version>__<description>.sql`
  - Example: `V11__add_user_email_index.sql`
- Repeatable: `R__<description>.sql`
  - Example: `R__user_spending_view.sql`

**Example Migration:**

```sql
-- V11__add_user_email_index.sql
------------------------------------------------------------
-- ADD EMAIL INDEX TO USERS TABLE
------------------------------------------------------------
CREATE INDEX IX_users_email ON dbo.users(email);
GO
```

**Versioning Strategy:**
- Use sequential integers: V1, V2, V3...
- Or semantic versions: V1.0.0, V1.0.1, V1.1.0
- Or timestamps: V20251106_1, V20251106_2

### Migration Best Practices

1. **One change per migration** - Don't combine unrelated changes
2. **Test locally first** - Always test on local database before committing
3. **Use transactions** - Wrap statements in transactions where possible
4. **Idempotent scripts** - Use `IF NOT EXISTS` checks
5. **Never modify applied migrations** - Create a new migration instead
6. **Document changes** - Add clear comments in migration files

## Best Practices

### Development Workflow

1. **Create migration locally**
   ```bash
   # Create new file: migrations/V11__add_feature.sql
   # Write your SQL
   ```

2. **Test locally**
   ```bash
   flyway info
   flyway migrate
   ```

3. **Commit and push**
   ```bash
   git add migrations/V11__add_feature.sql
   git commit -m "feat: add new feature migration"
   git push
   ```

4. **CI/CD runs automatically**
   - Validates migration
   - Tests on temporary database
   - Deploys to staging/production

### Migration Guidelines

**DO:**
- ✅ Use explicit schema names: `[dbo].[users]`
- ✅ Include GO statements for batching
- ✅ Add extended properties for documentation
- ✅ Use idempotent checks where possible
- ✅ Test rollback strategies

**DON'T:**
- ❌ Modify existing migrations
- ❌ Skip version numbers
- ❌ Use dynamic SQL in migrations
- ❌ Forget to test before committing
- ❌ Commit `.env` file

### Debugging
Enable detailed logging:

```bash
# CLI
flyway -X migrate

# Environment variable
export FLYWAY_LOG_LEVEL=DEBUG
flyway migrate
```
