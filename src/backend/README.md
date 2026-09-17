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

## Core HR module

`Modules/CoreHr/` implements requirements EMP-01 … EMP-05 with the same controller → business service → repository pattern as the sample module. Shared plumbing lives in `Modules/CoreHr/Shared` of each layer:

| Layer | Shared piece | Purpose |
|---|---|---|
| BusinessLogic | `CoreHrActor`, `CoreHrDataScope`, `CoreHrPermissions` | server-established actor, `self` / `department` / `organization` scope, permission constants |
| BusinessLogic | `CoreHrNotFoundException` (404), `CoreHrForbiddenException` (403), `CoreHrConcurrencyConflictException` (409), `CoreHrBusinessRuleException` (409 + stable `code`), `CoreHrValidationException` (422) | the only exceptions services throw |
| BusinessLogic | `PageRequest`, `PagedResult<T>` | paging contract shared with `PageMetadata` |
| DataAccess | `Entities.cs`, `CoreHrEntityConfigurations.cs`, `CoreHrAudit` | EF Core mappings for the Core HR tables and the audit-row factory |
| Api | `CoreHrControllerBase`, `CoreHrPolicies`, `CoreHrActorResolver` | ETag/If-Match, Problem Details mapping, claim → actor mapping, authorization policies |

| Feature folder | Requirement | Service | Endpoints |
|---|---|---|---|
| `Employees` | EMP-01 | `EmployeeDirectoryService` | `GET /employees`, `GET /employees/{id}`, `PATCH /employees/{id}/profile` |
| `Organization` | EMP-02 | `OrganizationService` | `GET /organization/chart`, departments `GET/POST/PUT/DELETE`, positions `GET/POST/PUT` |
| `Onboarding` | EMP-03 | `OnboardingTaskService` | `GET /onboarding/tasks`, `PUT /onboarding/tasks/{id}`, `POST /onboarding/tasks/{id}/{start|complete|reopen}` |
| `EmployeeEvents` | EMP-04 | `EmployeeMovementService` | `GET/POST /employees/{id}/events`, `POST /employee-events/{id}/{submit|approve|cancel}`, `ApplyDueEventsAsync` for the effective-date worker |
| `EmployeeDocuments` | EMP-05 | `EmployeeDocumentService` | `GET/POST /employees/{id}/documents`, `POST /employee-documents/{id}/download-url` |

Token claims consumed by `CoreHrActorResolver`: `qlns_user_id`, `qlns_employee_id`, `data_scope` (`self` | `department` | `organization`), `department_id` (repeatable) and `permission` (repeatable, values in `CoreHrPermissions`). Endpoint policies check the permission; services check data scope, field policy and action-level permissions (for example `corehr.event.approve`, `corehr.onboarding.reopen`, `corehr.document.read_sensitive`).

Employee master data (`status`, `department_id`, `position_id`, `manager_id`) is written in exactly one place, `EmployeeEvent.ApplyTo`, when an approved event reaches its effective date. `PATCH /employees/{id}/profile` accepts only personal fields; any other property in the merge-patch body is rejected with `403`.

Document storage and malware scanning are ports (`IDocumentStorage`, `IMalwareScanner`). The registered adapters — `FileSystemDocumentStorage` and `DevelopmentOnlyMalwareScanner`, configured by the `Documents` section of `appsettings.json` — are development placeholders and must be replaced by the selected object store and scanner before any shared environment.

### Local testing

Ba mức, từ nhanh nhất tới gần production nhất.

**1. Unit test — không cần database, không cần token**

```bash
dotnet test Qlns.sln
dotnet test Qlns.sln --filter FullyQualifiedName~CoreHr.Onboarding    # một feature
```

**2. Gọi HTTP thật trên PostgreSQL với token phát triển**

```bash
# PostgreSQL (port 5433 để không đụng container sẵn có ở 5432)
docker run -d --name qlns-pg -e POSTGRES_USER=qlns_app -e POSTGRES_PASSWORD=change-me \
  -e POSTGRES_DB=qlns -p 5433:5432 postgres:17
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/schema.sql
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/seed_dev.sql

# API — launchSettings.json đã đặt sẵn Development và http://localhost:5080
dotnet run --project src/Qlns.Api
curl "http://localhost:5080/dev/token?persona=hr-manager"
```

Persona khả dụng: `hr-manager`, `hr-officer`, `line-manager`, `employee`, `it-admin`.

> Cổng local là **5080**, không phải 5000 như `servers` trong `api/openapi.yaml`, vì trên macOS cổng 5000 bị AirPlay Receiver chiếm và trả 403 cho mọi request. Đổi cổng tại `Properties/launchSettings.json`, nhớ đổi kèm `Documents:PublicBaseUrl` trong `appsettings.Development.json` để signed URL trỏ đúng.

Ba cách gửi request, dùng cùng dữ liệu seed:

| Công cụ | Tệp | Lệnh |
|---|---|---|
| Postman / newman | `tests/postman/QLNS-CoreHR.postman_collection.json` + `QLNS-Local.postman_environment.json` | `./tests/postman/run.sh` |
| Script Python | `tests/smoke/corehr_smoke.py` | `python3 tests/smoke/corehr_smoke.py http://localhost:5080` |
| REST Client (VS Code) | `CoreHr.http` | mở tệp, bấm *Send Request* |

Trong Postman: import cả hai tệp JSON, chọn environment **QLNS · Local (Development)**, chạy thư mục `00 · Lấy token` trước để nạp token vào biến collection, rồi dùng Collection Runner cho toàn bộ. Token, ETag và id được tự động lưu qua test script nên không phải copy tay. Các request upload tài liệu tham chiếu `tests/postman/sample-degree.pdf`; nếu Postman báo thiếu tệp thì chọn lại ở tab *Body → form-data*.

Bộ collection gồm 68 request với 105 assertion, phủ cả đường thành công lẫn 401, 403, 404, 409, 415, 422.

**3. Integration test** — `tests/Qlns.IntegrationTests` chưa tồn tại. `docs/architecture.md` §5.6 coi đây là điều kiện bắt buộc trước slice đầu tiên, vì các invariant mạnh nhất là partial unique index và conditional update nằm ở PostgreSQL. Ngăn xếp đề xuất: `WebApplicationFactory` cộng Testcontainers.

**Xác thực phát triển.** `Qlns.Api/Development/DevelopmentAuthentication.cs` chỉ thay Identity Provider bằng khóa đối xứng khi môi trường là Development **và** `Authentication:DevelopmentSigningKey` có giá trị. Ngoài Development, endpoint `/dev/token` và `/dev/document-content` không được đăng ký.

Probation review (EMP-06) and offboarding (EMP-07) are not implemented yet: their sprint entry conditions in `docs/user_stories.md` §5.3 (offboarding checklist template, final-settlement catalogue) are still open and both depend on the Contracts module.

After the .NET 10 SDK is installed:

```bash
dotnet restore backend/Qlns.sln
dotnet build backend/Qlns.sln --no-restore
dotnet test backend/Qlns.sln --no-build
```

Database migrations are intentionally not generated without the selected SDK. [`database/schema.sql`](../../database/schema.sql) is the proposed canonical contract until the first reviewed EF Core migration is created.
