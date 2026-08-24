# Azure DevOps Setup Guide

## Quick Setup for Flyway CI/CD Pipeline (Dev Environment)

### 1. Create Variable Group

In Azure DevOps:
1. Go to **Pipelines** → **Library**
2. Click **+ Variable group**
3. Name it: `FLYWAY-DEV`
4. Add these variables:

**Development Environment:**
- `DEV_DB_URL` = `jdbc:sqlserver://your-dev-server:1433;databaseName=FinPulse_Dev;encrypt=true;trustServerCertificate=true`
- `DEV_DB_USER` = `your-db-user`
- `DEV_DB_PASSWORD` = `your-password` (🔒 click lock icon to mark as secret)

**Database Schemas (configured in pipeline):**
The pipeline automatically creates and manages these schemas:
- `dbo` (default schema)
- `finance`
- `plan`
- `investment`
- `reporting`

### 2. Create Pipeline
1. Go to **Pipelines** → **Pipelines**
2. Click **New pipeline**
3. Select **Azure Repos Git**
4. Select your repository
5. Choose **Existing Azure Pipelines YAML file**
6. Select `/azure-pipelines.yml`
7. Click **Run**

### 3. Agent Requirements
Your agent `10.0.0.xxx` must have:
- ✅ Docker installed and running
- ✅ Docker Compose installed
- ✅ Python 3.x (for SQLFluff)
- ✅ Network access to SQL Server dev instance
- ✅ Sufficient disk space for Docker images

### 4. How It Works

**Pipeline Trigger:**
The pipeline runs when:
- Changes are pushed to the branch
- Changes affect `migrations/*` or `flyway.toml`

**Pipeline Flow:**
```
Checkout → Validate Files → Install SQLFluff → Lint SQL →
Build Docker Image → Flyway Info → Flyway Migrate → Cleanup
```

## Pipeline Steps
### Step 1: Validate Migration Files
- Counts and validates migration files in `migrations/` directory
- Checks for `V*.sql` files

### Step 2: Install SQLFluff
- Installs Python 3.x
- Installs SQLFluff linter
- Validates SQLFluff installation

### Step 3: SQLFluff Linting
- Lints all migration files in `migrations/` directory
- Uses `.sqlfluff` configuration file
- Fails pipeline if linting errors are found
- Outputs results in GitHub annotation format

### Step 4: Build Flyway Docker Image
- Builds Flyway Docker image using `docker-compose build flyway`
- Uses configuration from `docker-compose.yml` and `Dockerfile`

### Step 5: Flyway Info
- Runs `flyway info` to display migration status
- Shows pending and applied migrations
- Uses database credentials from variable group

### Step 6: Flyway Migrate
- Runs `flyway migrate` to apply pending migrations
- Automatically creates schemas if they don't exist:
  - `dbo` (default)
  - `finance`
  - `plan`
  - `investment`
  - `reporting`
- Uses dev database credentials from variable group

### Step 7: Cleanup
- Runs always (even if previous steps fail)
- Removes Docker containers and volumes
- Cleans up temporary resources

## Quick Reference

### Manual Testing Commands

```bash
# Test SQLFluff locally
pip install sqlfluff
sqlfluff lint migrations/

# Build Docker image locally
docker-compose build flyway

# Run Flyway info locally
docker-compose run --rm \
  -e FLYWAY_URL="jdbc:sqlserver://..." \
  -e FLYWAY_USER="..." \
  -e FLYWAY_PASSWORD="..." \
  -e FLYWAY_SCHEMAS=dbo,finance,plan,investment,reporting \
  -e FLYWAY_DEFAULT_SCHEMA=dbo \
  -e FLYWAY_CREATE_SCHEMAS=true \
  flyway info

# Run Flyway migrate locally
docker-compose run --rm \
  -e FLYWAY_URL="jdbc:sqlserver://..." \
  -e FLYWAY_USER="..." \
  -e FLYWAY_PASSWORD="..." \
  -e FLYWAY_SCHEMAS=dbo,finance,plan,investment,reporting \
  -e FLYWAY_DEFAULT_SCHEMA=dbo \
  -e FLYWAY_CREATE_SCHEMAS=true \
  flyway migrate
```

### Environment Variables Used

The pipeline uses these environment variables from the variable group:
- `$(DEV_DB_URL)` - JDBC connection string
- `$(DEV_DB_USER)` - Database username
- `$(DEV_DB_PASSWORD)` - Database password (secret)

And these hardcoded in the pipeline:
- `FLYWAY_SCHEMAS=dbo,finance,plan,investment,reporting`
- `FLYWAY_DEFAULT_SCHEMA=dbo`
- `FLYWAY_CREATE_SCHEMAS=true`

## Pipeline Configuration Summary
**File:** [azure-pipelines.yml](../azure-pipelines.yml)

**Trigger:**
- Branch: `xxx`
- Paths: `migrations/*`, `flyway.toml`

**Agent Pool:**
- Pool: `Default`
- Agent: `10.0.0.xxx`

**Variable Group:**
- `FLYWAY-xxx`

**Database Schemas:**
- dbo (default)
- finance
- plan
- investment
- reporting

Done! Your Flyway CI/CD pipeline for your environment is ready!
