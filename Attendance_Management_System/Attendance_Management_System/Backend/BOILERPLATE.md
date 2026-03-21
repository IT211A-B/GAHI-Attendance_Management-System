Attendance Management System - Single-Project Layered Boilerplate
=================================================================

This project uses a single-project layered structure. All backend layers live
under `Backend/` inside the MVC web project. The goal is to keep things simple
while still separating concerns cleanly.

Folder Guide
------------
- `Backend/Entities`: Core domain entities.
- `Backend/ValueObjects`: Immutable value objects.
- `Backend/Enums`: Domain enums.
- `Backend/Exceptions`: Domain-specific exceptions.
- `Backend/Events`: Domain events (interfaces or marker types).

- `Backend/Interfaces/Repositories`: Repository contracts (e.g. `IRepository<T>`).
- `Backend/Interfaces/Services`: Application/service layer contracts.
- `Backend/DTOs`: Data transfer objects (request/response models).
- `Backend/UseCases`: Use case / application logic entry points.
- `Backend/Mappings`: Mapping configuration/helpers.
- `Backend/Validators`: Validation interfaces/helpers.

- `Backend/Persistence`: EF Core DbContext and persistence setup.
- `Backend/Repositories`: Repository implementations.
- `Backend/DependencyInjection.cs`: One place to register backend services.

How DI Is Wired
--------------
`Program.cs` calls:

```
builder.Services.AddBackend(builder.Configuration);
```

This method is defined in `Backend/DependencyInjection.cs` and registers:
- `AppDbContext` using the `ConnectionStrings:Default` setting
- `IRepository<T>` -> `GenericRepository<T>`
- `IUnitOfWork` -> `UnitOfWork`

Configuration
-------------
Connection string is stored in:
- `appsettings.json`
- `appsettings.Development.json`

Example:
```
Host=localhost;Port=5432;Database=attendance_db;Username=postgres;Password=postgres
```

How To Add A New Feature (Example Flow)
---------------------------------------
1. Create a domain entity in `Backend/Entities`.
2. Create a DTO in `Backend/DTOs` (if needed for UI/API).
3. Add a use case/service in `Backend/UseCases` or `Backend/Interfaces/Services`.
4. Add repository interface in `Backend/Interfaces/Repositories` (optional).
5. Implement repository in `Backend/Repositories` if custom logic is needed.
6. Register new services in `Backend/DependencyInjection.cs`.

Notes
-----
- This is a boilerplate only; no sample feature is included yet.
- EF Core migrations are not created by default.
