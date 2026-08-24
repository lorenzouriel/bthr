# Fin API

> A comprehensive personal finance management REST API built with ASP.NET Core 8.0, Entity Framework Core, and SQL Server.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-294%20passing-brightgreen)](#testing)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Overview

FinPulse API provides a complete backend solution for personal finance applications. It enables users to track expenses, earnings, bills, budgets, financial goals, and investments. The API also supports Open Banking integration with bank connections, accounts, and transaction syncing.

**Key capabilities:**
- User authentication with JWT tokens stored in HTTP-only cookies
- Full financial tracking (expenses, earnings, bills, budgets, goals, investments)
- Open Banking integration (bank connections, accounts, transactions)
- Secure password hashing with BCrypt
- Interactive Swagger documentation
- 294 unit tests with 100% pass rate

## Features

| Category | Features |
|----------|----------|
| **Authentication** | JWT-based auth, HTTP-only cookie tokens, BCrypt password hashing |
| **Financial Tracking** | Expenses, Earnings, Bills, Budgets, Goals, Investments |
| **Banking Integration** | Bank Connections, Bank Accounts, Bank Transactions |
| **Infrastructure** | Docker support, Azure Container Apps deployment, CI/CD pipelines |

## Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 8.0 |
| Database | SQL Server |
| Authentication | JWT Bearer Tokens (HTTP-only cookies) |
| Password Hashing | BCrypt.Net |
| API Documentation | Swagger/OpenAPI |
| Containerization | Docker |
| CI/CD | Azure DevOps Pipelines |
| Cloud Hosting | Azure Container Apps |

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local or remote instance)
- [Docker](https://www.docker.com/get-started) (optional)

### Local Development

1. **Clone and configure**
   ```bash
   git clone <repository-url>
   cd api
   ```

2. **Set up environment** — copy `.env.example` to `.env` and fill in your values, or configure `FinPulse.Api/appsettings.json` directly:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost,1433;Database=fin_pulse;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
     }
   }
   ```

3. **Run the application**
   ```bash
   cd FinPulse.Api
   dotnet restore
   dotnet run
   ```

4. **Access the API**
   - API Base URL: `http://localhost:5026`
   - Swagger UI: `http://localhost:5026/swagger`
   - Health Check: `http://localhost:5026/health`

### Docker Deployment

```bash
# Create environment file
cp .env.example .env
# Edit .env with your configuration

# Start services
docker-compose up -d

# Verify
curl http://localhost:5026/health
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and receive JWT cookie |
| POST | `/api/auth/logout` | Logout (clear cookie) |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users/{id}` | Get user by ID |
| PUT | `/api/users/{id}` | Update user profile |
| DELETE | `/api/users/{id}` | Soft-delete user |

### Financial Tracking

All financial endpoints follow the pattern `/api/users/{userId}/{resource}`.

| Resource | Base Path | Supports |
|----------|-----------|----------|
| **Expenses** | `/expenses` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |
| **Earnings** | `/earnings` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |
| **Bills** | `/bills` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |
| **Budgets** | `/budgets` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |
| **Goals** | `/goals` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |
| **Investments** | `/investments` | GET (filters), POST, PUT `/{id}`, DELETE `/{id}` |

### Banking Integration

| Resource | Base Path | Supports |
|----------|-----------|----------|
| **Bank Connections** | `/bank-connections` | GET, GET `/{id}`, POST, PUT `/{id}`, DELETE `/{id}` |
| **Bank Accounts** | `/bank-accounts` | GET, GET `/{id}`, POST, PUT `/{id}`, DELETE `/{id}` |
| **Bank Transactions** | `/bank-transactions` | GET (filters), GET `/{id}`, POST, PUT `/{id}`, DELETE `/{id}` |

## Authentication Flow

All protected endpoints require a valid JWT token. The token is set as an HTTP-only cookie on login.

```bash
# 1. Register a new user
curl -X POST http://localhost:5026/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "phoneNumber": "+1234567890",
    "email": "john@example.com",
    "password": "SecurePass@123"
  }'

# 2. Login — JWT token stored in HTTP-only cookie automatically
curl -c cookies.txt -X POST http://localhost:5026/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass@123"
  }'

# 3. Use the cookie for subsequent requests
curl -b cookies.txt http://localhost:5026/api/users/1/expenses
```

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Yes |
| `Jwt__SecretKey` | JWT signing key (min 32 chars) | Yes |
| `Jwt__Issuer` | JWT token issuer | No (default: `FinPulse.Api`) |
| `Jwt__Audience` | JWT token audience | No (default: `FinPulse.Api`) |
| `Jwt__ExpirationMinutes` | Token lifetime in minutes | No (default: `60`) |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | No (default: `Production`) |

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Base configuration |
| `appsettings.Development.json` | Development overrides |
| `.env` | Docker environment variables |
| `.env.example` | Environment variable template |

## Project Structure

```
api/
├── FinPulse.Api/
│   ├── Controllers/           # 11 API controllers
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── ExpensesController.cs
│   │   ├── EarningsController.cs
│   │   ├── BillsController.cs
│   │   ├── BudgetsController.cs
│   │   ├── GoalsController.cs
│   │   ├── InvestmentsController.cs
│   │   ├── BankConnectionsController.cs
│   │   ├── BankAccountsController.cs
│   │   └── BankTransactionsController.cs
│   ├── Services/              # 11 service classes + interfaces
│   ├── Models/                # 10 Entity Framework models
│   ├── DTOs/                  # Request / response DTOs
│   ├── Data/                  # ApplicationDbContext
│   ├── Program.cs             # Application entry point & DI setup
│   ├── Dockerfile
│   └── appsettings.json
│
├── FinPulse.Tests/
│   ├── UnitTests/
│   │   ├── Controllers/       # 11 controller test classes
│   │   └── Services/          # 11 service test classes
│   └── Helpers/
│       ├── ServiceTestBase.cs         # In-memory DB base class
│       ├── ControllerTestBase.cs      # Auth context base class
│       ├── Builders/                  # 10 fluent test-data builders
│       ├── Factories/                 # WebApplicationFactory (integration test ready)
│       └── Extensions/                # HttpClient helpers (integration test ready)
│
├── docker-compose.yml
├── azure-pipelines.yml
├── .env.example
└── api-csharp.sln
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Filter by layer
dotnet test --filter "FullyQualifiedName~Services"
dotnet test --filter "FullyQualifiedName~Controllers"

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage — 294 Tests (100% Pass Rate)

#### Service Tests (132 total)

| Test Class | Tests |
|---|---|
| `JwtServiceTests` | 9 |
| `UserServiceTests` | 24 |
| `ExpenseServiceTests` | 8 |
| `EarningServiceTests` | 11 |
| `BillServiceTests` | 15 |
| `BudgetServiceTests` | 14 |
| `GoalServiceTests` | 14 |
| `InvestmentServiceTests` | 15 |
| `BankAccountServiceTests` | 15 |
| `BankConnectionServiceTests` | 14 |
| `BankTransactionServiceTests` | 18 |

Each service test class covers: `Create`, `GetList` (with filters and soft-delete), `GetById`, `Update`, and `Delete` — including not-found and unauthorized scenarios.

#### Controller Tests (162 total)

| Test Class | Tests |
|---|---|
| `AuthControllerTests` | 8 |
| `UsersControllerTests` | 13 |
| `ExpensesControllerTests` | 10 |
| `EarningsControllerTests` | 10 |
| `BillsControllerTests` | 10 |
| `BudgetsControllerTests` | 10 |
| `GoalsControllerTests` | 10 |
| `InvestmentsControllerTests` | 10 |
| `BankConnectionsControllerTests` | 16 |
| `BankAccountsControllerTests` | 16 |
| `BankTransactionsControllerTests` | 16 |

Each controller test class covers: 200/201/404/403 responses and unauthorized access scenarios.

### Test Stack

| Package | Purpose |
|---------|---------|
| xUnit | Test framework |
| Moq | Mocking framework |
| FluentAssertions | Assertion library |
| Bogus | Fake data generation |
| EF Core InMemory | In-memory database for service tests |

### Test Conventions

**Naming:** `[MethodName]_[Scenario]_[ExpectedResult]`

```
RegisterAsync_WithValidRequest_CreatesUserSuccessfully
LoginAsync_WithInvalidPassword_ReturnsNull
GetExpenses_WhenUserDoesNotOwnResource_Returns403Forbidden
```

**Pattern:** All tests follow AAA (Arrange / Act / Assert) and use the fluent builder pattern:

```csharp
var expense = new ExpenseBuilder()
    .WithUserId(userId)
    .WithCategory("Food")
    .WithAmount(50.00m)
    .AsActive()
    .Build();
```

## Deployment

### Azure Container Apps (via Azure Pipelines)

The project includes an Azure DevOps pipeline (`azure-pipelines.yml`) that:
1. Builds and pushes the Docker image to Azure Container Registry
2. Deploys to Azure Container Apps with zero-downtime updates
3. Triggers on pushes to `dev` and `main` branches

### Manual Docker Deployment

```bash
# Build image
docker build -t finpulse-api -f FinPulse.Api/Dockerfile .

# Run container
docker run -d \
  -p 5026:8080 \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  -e Jwt__SecretKey="your-32-char-secret-key" \
  finpulse-api
```

## Data Models

### Core Entities

| Entity | Description |
|--------|-------------|
| `User` | User account with hashed password and JWT auth |
| `Expense` | Spending record with category, payment method, date |
| `Earning` | Income record with category and earning date |
| `Bill` | Recurring or one-time payment obligation |
| `Budget` | Monthly spending limit by category |
| `Goal` | Savings target with current/target amount and due date |
| `Investment` | Investment holding with value, yield, and P&L tracking |

### Banking Entities

| Entity | Description |
|--------|-------------|
| `BankConnection` | Open Banking provider link with consent/token management |
| `BankAccount` | Linked bank account with balance tracking |
| `BankTransaction` | Synced transaction with category and pending status |

All entities use soft-delete (`Status = 0`) rather than hard deletes.
