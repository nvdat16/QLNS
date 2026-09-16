# QLNS ASP.NET Core backend

Target: .NET 10, ASP.NET Core, Entity Framework Core and PostgreSQL.

## Modular 3-layer backend

- `Qlns.Api` — Presentation layer: HTTP, authentication/authorization, DTO and Problem Details mapping.
- `Qlns.BusinessLogic` — Business layer: use-case service, workflow policy and repository contracts. It has no EF Core or ASP.NET dependency.
- `Qlns.DataAccess` — Data layer: EF Core mappings, PostgreSQL queries, transactions, audit/history persistence.

Each layer groups code by `Modules/<Module>/<Feature>`. `Modules/Recruitment/Applications` is a **sample module** that shows the intended layout; it is skeleton code, not a finished feature. Future code must use the same module names as the OpenAPI tags:

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
│   ├── Shifts/
│   ├── Events/
│   ├── Timesheets/
│   └── Corrections/
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

## Sample module

`Modules/Recruitment/Applications` demonstrates the pattern every feature must follow: controller → business service → repository, `If-Match` against lost updates, and one Data Access transaction that writes the business change, its history row and its audit row together. It is a structural sample only; the corresponding OpenAPI operations remain `x-implementation-status: proposed` until the module is completed with migration, authorization against a real identity provider, and integration/contract tests.

After the .NET 10 SDK is installed:

```bash
dotnet restore backend/Qlns.sln
dotnet build backend/Qlns.sln --no-restore
dotnet test backend/Qlns.sln --no-build
```

Database migrations are intentionally not generated without the selected SDK. [`database/schema.sql`](../../database/schema.sql) is the proposed canonical contract until the first reviewed EF Core migration is created.
