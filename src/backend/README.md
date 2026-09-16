# QLNS ASP.NET Core backend

Target: .NET 10, ASP.NET Core, Entity Framework Core and PostgreSQL.

## Modular 3-layer backend

- `Qlns.Api` — Presentation layer: HTTP, authentication/authorization, DTO and Problem Details mapping.
- `Qlns.BusinessLogic` — Business layer: use-case service, workflow policy and repository contracts. It has no EF Core or ASP.NET dependency.
- `Qlns.DataAccess` — Data layer: EF Core mappings, PostgreSQL queries, transactions, audit/history persistence.

Each layer groups code by `Modules/<Module>/<Feature>`. The current implemented slice is under `Modules/Recruitment/Applications`; future code must use the same module names as the OpenAPI tags:

```text
Modules/
├── Recruitment/
│   ├── Requisitions/
│   ├── Intake/
│   ├── Applications/
│   ├── Interviews/
│   ├── Evaluations/
│   └── Offers/
├── CoreHr/
│   ├── Employees/
│   ├── Organization/
│   ├── Onboarding/
│   ├── EmployeeEvents/
│   └── EmployeeDocuments/
├── Contracts/
│   ├── Contracts/
│   └── Addenda/
├── Reports/
│   ├── Headcount/
│   ├── Recruitment/
│   └── Exports/
├── Attendance/
│   ├── Shifts/
│   ├── Events/
│   ├── Timesheets/
│   └── Corrections/
├── Leave/
│   ├── Balances/
│   └── Requests/
└── Administration/
    ├── Users/
    ├── Authorization/
    ├── Audit/
    ├── Deliveries/
    └── Integrations/
```

Do not add empty controllers for target operations. A feature folder is added when its use case, authorization policy, persistence and contract tests are implemented together.

The deployment tiers are React Web, ASP.NET Core API and PostgreSQL. Layers are source-code boundaries inside the application tier; tiers are independently deployed/runtime boundaries.

## Vertical slice

REC-03.2 is implemented by `POST /api/v1/recruitment/applications/{applicationId}/advance`. `If-Match` protects against lost updates. A successful Data Access transaction writes the new stage, stage history and audit record atomically.

After the .NET 10 SDK is installed:

```bash
dotnet restore backend/Qlns.sln
dotnet build backend/Qlns.sln --no-restore
dotnet test backend/Qlns.sln --no-build
```

Database migrations are intentionally not generated without the selected SDK. [`database/schema.sql`](../../database/schema.sql) is the proposed canonical contract until the first reviewed EF Core migration is created.
