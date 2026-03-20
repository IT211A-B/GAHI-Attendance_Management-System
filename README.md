# DonBosco Attendance Management System (DBTC-IAS)

A .NET 9 ASP.NET Core Web API for managing student and staff attendance via QR code scanning at gate terminals. Built for Don Bosco Technical College.

---

## Tech Stack

| Layer     | Technology                       |
| --------- | -------------------------------- |
| Runtime   | .NET 9.0                         |
| Framework | ASP.NET Core Web API             |
| Database  | PostgreSQL                       |
| ORM       | Entity Framework Core 9 (Npgsql) |
| Auth      | JWT Bearer + BCrypt              |
| API Docs  | Scalar (OpenAPI)                 |
| Testing   | xUnit + Moq + EF Core InMemory   |

## Project Structure

```
SystemManagementSystem/
├── AttendanceManagementSystem.slnx          # Solution file
├── SystemManagementSystem/                  # Main API project
│   ├── Controllers/                         # 13 API controllers
│   ├── Data/
│   │   ├── Configurations/                  # EF Core Fluent API configs
│   │   ├── Migrations/                      # EF Core migrations
│   │   ├── ApplicationDbContext.cs
│   │   └── DbInitializer.cs                 # Seed data
│   ├── DTOs/                                # Request/Response DTOs
│   ├── Helpers/
│   │   └── JwtTokenHelper.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Models/
│   │   ├── Entities/                        # 14 domain entities
│   │   └── Enums/                           # 7 enums
│   ├── Services/
│   │   ├── Interfaces/                      # Service contracts
│   │   └── Implementations/                 # Service implementations
│   ├── Program.cs                           # App entry point & DI
│   └── appsettings.json
└── SystemManagementSystem.Tests/            # Unit test project (81 tests)
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) 14+

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/<your-org>/DonBosco-Attendance_Management-System.git
cd DonBosco-Attendance_Management-System/SystemManagementSystem/SystemManagementSystem
```

### 2. Configure user secrets

Sensitive configuration is stored via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (not in source control).

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=DBTC_AttendanceDB;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "JwtSettings:SecretKey" "YOUR_SECRET_KEY_MIN_32_CHARS"
```

### 3. Apply migrations

```bash
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

The API starts at:

- **HTTP:** `http://localhost:5001`
- **HTTPS:** `https://localhost:7145`

### 5. Open API documentation

Navigate to `http://localhost:5001/scalar/v1` for the interactive Scalar API docs.

## Running Tests

```bash
cd SystemManagementSystem.Tests
dotnet test
```

81 unit tests covering all service layer operations.

## Seed Data

On first run, `DbInitializer` populates the database with:

| Data           | Details                                                                                                               |
| -------------- | --------------------------------------------------------------------------------------------------------------------- |
| Roles          | `SuperAdmin`, `Admin`, `Registrar`, `Guard`                                                                           |
| Admin user     | `admin` / `Admin@123`                                                                                                 |
| Departments    | `CITE`, `CAHS`, `CAS`                                                                                                 |
| Business rules | Late threshold (15 min), absent threshold (60 min), scan cooldown (5 min), max daily scans (10), QR expiry (365 days) |
| Gate terminal  | Main Gate (entry type, active)                                                                                        |

## API Endpoints

### Authentication

| Method | Endpoint                    | Description                           |
| ------ | --------------------------- | ------------------------------------- |
| POST   | `/api/auth/login`           | Login, returns access + refresh token |
| POST   | `/api/auth/refresh`         | Rotate refresh token                  |
| PUT    | `/api/auth/change-password` | Change password                       |

### Students

| Method | Endpoint                           | Description                  |
| ------ | ---------------------------------- | ---------------------------- |
| GET    | `/api/students`                    | List (paginated, searchable) |
| GET    | `/api/students/{id}`               | Get by ID                    |
| POST   | `/api/students`                    | Create                       |
| PUT    | `/api/students/{id}`               | Update                       |
| DELETE | `/api/students/{id}`               | Soft delete                  |
| POST   | `/api/students/{id}/regenerate-qr` | Regenerate QR code           |

### Staff

| Method | Endpoint                        | Description                  |
| ------ | ------------------------------- | ---------------------------- |
| GET    | `/api/staff`                    | List (paginated, searchable) |
| GET    | `/api/staff/{id}`               | Get by ID                    |
| POST   | `/api/staff`                    | Create                       |
| PUT    | `/api/staff/{id}`               | Update                       |
| DELETE | `/api/staff/{id}`               | Soft delete                  |
| POST   | `/api/staff/{id}/regenerate-qr` | Regenerate QR code           |

### Attendance

| Method | Endpoint               | Description         |
| ------ | ---------------------- | ------------------- |
| POST   | `/api/attendance/scan` | Process QR scan     |
| GET    | `/api/attendance`      | Get logs (filtered) |
| GET    | `/api/attendance/{id}` | Get log by ID       |

### Other Resources

Full CRUD endpoints available for: **Users**, **Departments**, **Business Rules**, **Gate Terminals**, **Academic Periods**, **Academic Programs**, **Sections**, **Audit Logs** (read-only), **Dashboard** (stats).

See `/scalar/v1` for complete endpoint documentation.

## Key Features

- **QR Code Scanning** — Students and staff scan at gate terminals; system auto-detects entry/exit and computes on-time/late status
- **JWT Auth with Refresh Tokens** — 8-hour access tokens, 7-day single-use rotating refresh tokens
- **Audit Trail** — All create/update/delete operations logged with old/new JSON values
- **Soft Deletes** — No data permanently removed; `IsDeleted` flag on all entities
- **Configurable Business Rules** — Global defaults with per-department overrides
- **Global Exception Handling** — Consistent `ApiResponse<T>` envelope for all responses

## Configuration

Non-sensitive settings in `appsettings.json`:

```json
{
  "JwtSettings": {
    "Issuer": "DBTC-IAS",
    "Audience": "DBTC-IAS-Clients",
    "ExpirationInMinutes": 480
  }
}
```

Sensitive values (`ConnectionStrings:DefaultConnection`, `JwtSettings:SecretKey`) must be set via user secrets or environment variables.

## Team

| Role     | Member                 |
| -------- | ---------------------- |
| Backend  | Jake F. Sucgang        |
| Frontend | John Paolo M. Cabaluna |
| QA       | Clyde Z. Parba         |
