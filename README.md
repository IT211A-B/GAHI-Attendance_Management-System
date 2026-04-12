# DonBosco Attendance Management System

Attendance Management System for Don Bosco Technical College, implemented as a pure ASP.NET MVC web application with server-rendered Razor views.

## Project Status

- Active app mode: Pure MVC
- Authentication: Cookie-based ASP.NET Identity
- Data access: Entity Framework Core + PostgreSQL
- Health endpoint: `/health`

Cutover notes are documented in [Attendance_Management_System/Attendance_Management_System/PURE_MVC_CUTOVER.md](Attendance_Management_System/Attendance_Management_System/PURE_MVC_CUTOVER.md).

## Tech Stack

- .NET 9.0
- ASP.NET Core MVC + Razor Views
- EF Core 9 + Npgsql
- ASP.NET Core Identity (int keys)
- Frontend assets: CSS/JS in `wwwroot`, jQuery + Bootstrap libs

## Repository Structure

- Main web app: [Attendance_Management_System/Attendance_Management_System](Attendance_Management_System/Attendance_Management_System)
- Solution file: [Attendance_Management_System/Attendance_Management_System.slnx](Attendance_Management_System/Attendance_Management_System.slnx)
- Backend domain/services/controllers: [Attendance_Management_System/Attendance_Management_System/Backend](Attendance_Management_System/Attendance_Management_System/Backend)
- Frontend Razor views and static assets: [Attendance_Management_System/Attendance_Management_System/Frontend](Attendance_Management_System/Attendance_Management_System/Frontend)
- Tests project: [Attendance_Management_System/tests/Attendance_Management_System.Tests](Attendance_Management_System/tests/Attendance_Management_System.Tests)

## Prerequisites

- .NET SDK 9.0+
- PostgreSQL running locally
- Git

## Quick Start (Local)

1. Restore/build:

```powershell
dotnet build Attendance_Management_System/Attendance_Management_System.slnx
```

2. Ensure DB connection string is valid.

Default is in [Attendance_Management_System/Attendance_Management_System/appsettings.json](Attendance_Management_System/Attendance_Management_System/appsettings.json), but for local testing prefer runtime override:

```powershell
$env:ConnectionStrings__Default='Host=localhost;Port=5432;Database=attendance_db;Username=postgres;Password=YOUR_PASSWORD'
dotnet run --project Attendance_Management_System/Attendance_Management_System/Attendance_Management_System.csproj --launch-profile manual-qa
```

3. Open:

- Login: `http://localhost:5003/login`
- Signup: `http://localhost:5003/signup`
- Health: `http://localhost:5003/health`

## Seeded Accounts (Fresh Database Only)

Seed runs only when there are no users yet.

- Admin:
	- Email: `admin@dbtc-cebu.edu.ph`
	- Password: `Admin123!`
- Teacher (seeded set):
	- Example email: `it.faculty@dbtc-cebu.edu.ph`
	- Password: `Teacher123!`
- Student pattern:
	- Email: `student01@dbtc-cebu.edu.ph` (and more)
	- Password: `Student123!`

Seed implementation is in [Attendance_Management_System/Attendance_Management_System/Backend/Data/SeedData.cs](Attendance_Management_System/Attendance_Management_System/Backend/Data/SeedData.cs).

## Testing

Build:

```powershell
dotnet build Attendance_Management_System/Attendance_Management_System.slnx -v minimal
```

Run tests:

```powershell
dotnet test Attendance_Management_System/tests/Attendance_Management_System.Tests/Attendance_Management_System.Tests.csproj -v minimal
```

## Manual QA Notes

- For QR flows, test both routes:
	- Student scan page: `/attendance/scan`
	- Teacher/admin QR page: `/attendance/qr`
- If browser shows `chrome-error://chromewebdata`, open a new tab and load `http://localhost:5003/login` directly.
- Keep the app run terminal active while testing.

## Branch and PR Workflow

- Work on feature branch (for this repo, often `JP`).
- Push branch updates to origin.
- Create PR into `main`.

If direct push to `main` is blocked, follow PR-only policy and use GitHub compare/PR flow.
