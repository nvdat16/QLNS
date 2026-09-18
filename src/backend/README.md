# QLNS ASP.NET Core backend

Target: .NET 10, ASP.NET Core, Entity Framework Core and PostgreSQL.

**Status:** every one of the 79 operations in [`api/openapi.yaml`](../../docs/api/openapi.yaml) is `code-complete` — controller,
endpoint authorization policy, business workflow, transactional persistence (audit + outbox in the same transaction) and
unit tests (1 347 passing). What is still missing before an operation counts as `implemented` is listed in
[API_REFERENCE §7](../../docs/api/API_REFERENCE.md#7-trạng-thái-triển-khai): integration tests against PostgreSQL, EF Core
migrations and the background worker host.

## Modular 3-layer backend

- `Qlns.Api` — Presentation layer: HTTP, authentication/authorization, DTO and Problem Details mapping.
- `Qlns.BusinessLogic` — Business layer: use-case services, domain aggregates, workflow policy and repository contracts. It has no EF Core or ASP.NET dependency.
- `Qlns.DataAccess` — Data layer: EF Core mappings, PostgreSQL queries, transactions, audit/outbox persistence, development adapters.

Each layer groups code by `Modules/<Module>/<Feature>`; module names follow the OpenAPI tags and the set of folders is bounded by
the delivery scope (the bold leaf functions under Recruitment and Core HR in [`topdown-approach.png`](../../topdown-approach.png)):

```text
Modules/
├── Recruitment/
│   ├── Requisitions/     REC-01   RequisitionService
│   ├── Intake/           REC-02   CandidateIntakeService (+ IResumeParser port)
│   ├── Applications/     REC-03   RecruitmentPipelineService
│   ├── Interviews/       REC-04   InterviewService
│   ├── Evaluations/      REC-05   EvaluationService (+ IEvaluationScoringPolicy)
│   └── Offers/           REC-06   OfferService (+ IOfferResponseTokenService, candidate → employee handoff)
├── CoreHr/
│   ├── Shared/           shared kernel (actor, data scope, exceptions, paging, controller base, audit/outbox factories)
│   ├── Employees/        EMP-01   EmployeeDirectoryService
│   ├── Organization/     EMP-02   OrganizationService (departments + hierarchy rules, positions)
│   ├── Onboarding/       EMP-03   OnboardingTaskService
│   ├── EmployeeEvents/   EMP-04   EmployeeMovementService (+ ApplyDueEventsAsync for the worker)
│   ├── EmployeeDocuments/EMP-05   EmployeeDocumentService (+ IDocumentStorage, IMalwareScanner ports)
│   ├── Probation/        EMP-06   ProbationReviewService
│   └── Offboarding/      EMP-07   OffboardingCaseService, OffboardingTaskService
├── Contracts/
│   ├── Shared/           ContractPermissions / ContractPolicies
│   ├── Contracts/        CON-01, CON-02   ContractService (+ ExpireDueContractsAsync, ExpiryAlertPolicy)
│   └── Addenda/          CON-03   ContractAddendumService
├── Identity/
│   ├── Shared/           IdentityPermissions, RoleGrant / IdentityAuthorization, e-mail normalization, policies, controller base
│   ├── Authentication/   ADM-01   AuthenticationService (+ IPasswordHasher, IAccessTokenIssuer, IRefreshTokenGenerator ports)
│   └── Users/            ADM-02   UserAccountService (accounts, role grants, password resets, role catalogue)
└── Operations/           GET /health/live, GET /health/ready (Qlns.Api only)
```

There is no `Reports/` module and no Organizational Chart endpoint: those functions are out of scope. Of the System
Administration pillar only account and role management is delivered, as the `Identity/` module (ADR-011) — audit-log
browsing, outbox monitoring and integration configuration remain out of scope. Audit logging and the transactional outbox
are crosscutting mechanisms inside each feature's repository (`CoreHrAudit.Entry`, `CoreHrOutbox.Message`,
`IdentityAudit.Entry`), not modules of their own.

## The pattern every feature follows

`controller → business service → repository`, exactly as the Onboarding feature shows it:

| Layer | Piece | Responsibility |
|---|---|---|
| BusinessLogic | aggregate (`Requisition`, `Offer`, `Contract`, `ProbationReview`, …) | invariants and workflow transitions; every mutation bumps `Version` and `UpdatedAt`; invalid transitions throw `CoreHrBusinessRuleException` with a stable code |
| BusinessLogic | `<Feature>Service` | data scope (out of scope ⇒ 404), action-level permission (403), `If-Match` version check (409), orchestration |
| BusinessLogic | `I<Aggregate>Repository`, `<Feature>Permissions` | persistence contract; permission claim constants |
| DataAccess | `<Aggregate>Repository` | `AsNoTracking` reads filtered by scope in SQL; writes = one transaction: conditional `ExecuteUpdate … WHERE version = expected` → audit row → outbox row → commit; `false` when a concurrent write won |
| DataAccess | `<Feature>Registration.Add<Module><Feature>()` | composition of the feature, called from `DependencyInjection.AddDataAccess` |
| Api | `<Feature>Controller : CoreHrControllerBase` | `ExecuteAsync(actor => …)` maps the five kernel exceptions to RFC 9457 Problem Details; ETag/If-Match; `Location` on 201 |
| Api | `<Feature>Policies.Add<Feature>Policies()` | endpoint policies requiring the coarse `permission` claim, called from `Program.cs` |

The shared kernel lives under `Modules/CoreHr/Shared` in every layer and is used by all four modules (the `CoreHr` prefix is
historical). Token claims consumed by `CoreHrActorResolver` — and therefore the claims `JwtAccessTokenIssuer` must emit:
`qlns_user_id`, `qlns_employee_id`, `data_scope`
(`self` | `department` | `organization`), `department_id` (repeatable) and `permission` (repeatable). Permission values are the
`<module>.<feature>.<verb>` constants in each `*Permissions` class, e.g. `recruitment.offer.approve`, `contracts.contract.write`,
`corehr.probation.decide`.

## Cross-module writes that must stay atomic

Some commands write rows owned by another feature because the requirement demands one transaction. They go through the
owning feature's entities, never through its API:

| Command | Also writes | Why |
|---|---|---|
| `POST /recruitment/offers/{id}/response` (accept) | `employees`, `contracts` (draft probation), `onboarding_tasks`, `applications` → `hired_ready` | REC-06.2 handoff: at most one employee per `source_application_id`; replay returns `replayed: true` |
| `POST /contracts/{id}/activate` (probation contract) | `probation_reviews` (pending, due 7 days before `end_date`) | EMP-06 step 1 |
| `POST /contract-addenda/{id}/make-effective` | `employee_events` (approved) when salary/position/department change | CON-03: master data changes only via events |
| `POST /probation-reviews/{id}/decide` | `employee_events` (approved) and, for `terminated`, a draft `offboarding_cases` row | EMP-06.2 |
| `POST /offboarding/cases/{id}/complete` | `employee_events` (termination, approved, effective on the last working date) | EMP-07.2 |

`employees.status`, `department_id`, `position_id` and `manager_id` are still written in exactly one place,
`EmployeeEvent.ApplyTo`, when the Effective-Date Worker applies an approved event.

## Ports and development adapters

| Port (BusinessLogic) | Development adapter (DataAccess) | Configuration | Replace before any shared environment |
|---|---|---|---|
| `IDocumentStorage` | `FileSystemDocumentStorage` + `/dev/document-content` | `Documents:StorageRoot`, `Documents:PublicBaseUrl`, `Documents:SigningKey` | private object store with pre-signed URLs |
| `IMalwareScanner` | `DevelopmentOnlyMalwareScanner` (always clean) | — | real scanner |
| `IResumeParser` | `DevelopmentOnlyResumeParser` (extracts nothing; recruiter types the data at confirm) | — | CV parsing service |
| `IOfferResponseTokenService` | `HmacOfferResponseTokenService` + `/dev/offer-token?offerId=` | `Recruitment:OfferTokenSigningKey` (required) | keep, rotate the key |
| `IEvaluationScoringPolicy` | `EqualWeightScoringPolicy` | — | per-position weights |
| `IPasswordHasher` | `Pbkdf2PasswordHasher` (PBKDF2-HMAC-SHA512, 210 000 iterations) — production-grade, not a stub | — | keep; raise the iteration count over time (the hash carries its own parameters) |
| `IAccessTokenIssuer` | `JwtAccessTokenIssuer` (HS256) | `Authentication:Jwt:SigningKey` (**required**), `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays` | keep; move to an asymmetric key if other services must validate the token |
| `IRefreshTokenGenerator` | `RandomRefreshTokenGenerator` (256-bit CSPRNG, SHA-256 digest at rest) | — | keep |

Worker entry points exposed by services (no worker host yet): `EmployeeMovementService.ApplyDueEventsAsync`,
`OfferService.ExpireDueOffersAsync`, `ContractService.ExpireDueContractsAsync`. Outbox message types written today:
`recruitment.requisition.published`, `recruitment.application.rejected`, `recruitment.interview.scheduled|rescheduled|cancelled`,
`recruitment.offer.sent|accepted`, `contracts.contract.approved`, `corehr.probation.review_submitted`,
`corehr.offboarding.case_completed`.

## Local testing

Ba mức, từ nhanh nhất tới gần production nhất.

**1. Unit test — không cần database, không cần token**

```bash
dotnet test Qlns.sln
dotnet test Qlns.sln --filter FullyQualifiedName~Recruitment.Offers    # một feature
```

**2. Gọi HTTP thật trên PostgreSQL với token phát triển**

```bash
# PostgreSQL (port 5433 để không đụng container sẵn có ở 5432)
docker run -d --name qlns-pg -e POSTGRES_USER=qlns_app -e POSTGRES_PASSWORD=change-me \
  -e POSTGRES_DB=qlns -p 5433:5432 postgres:17
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/schema.sql      # v1.2 — 28 bảng
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/seed_roles.sql  # dữ liệu tham chiếu, bắt buộc
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/seed_dev.sql

# API — launchSettings.json đã đặt sẵn Development và http://localhost:5080
dotnet run --project src/Qlns.Api

# Đăng nhập thật (ADM-01). Mọi tài khoản trong seed_dev.sql dùng mật khẩu Qlns@2026.
curl -s -X POST http://localhost:5080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"hr.manager@qlns.local","password":"Qlns@2026"}'

# Hoặc lối tắt persona chỉ có ở Development, bỏ qua mật khẩu:
curl "http://localhost:5080/dev/token?persona=hr-manager"
```

Tài khoản trong seed: `hr.manager@`, `hr.officer@`, `eng.manager@` (Line Manager, scope phòng ban 2 và 4), `dev.nguyen@`
(Employee), `it.admin@`, `recruiter@`, `admin@` (Super Admin) — tất cả ở `@qlns.local`. `ceo@qlns.local` được seed với
`must_change_password` và **không** có vai trò nào: dùng nó để kiểm chứng phiên hạn chế và ranh giới 401 / 403.

Persona khả dụng (khớp `seed_dev.sql` và ma trận vai trò trong `docs/user_stories.md` §6.1): `hr-manager`, `hr-officer`,
`line-manager` (kiêm Hiring Manager / Interviewer, scope phòng ban 2 và 4), `recruiter`, `employee`, `it-admin`.
Token phản hồi Offer của ứng viên lấy bằng `GET /dev/offer-token?offerId=` sau khi Offer đã `send`.

> Cổng local là **5080**, không phải 5000 như `servers` trong `api/openapi.yaml`, vì trên macOS cổng 5000 bị AirPlay Receiver chiếm và trả 403 cho mọi request. Đổi cổng tại `Properties/launchSettings.json`, nhớ đổi kèm `Documents:PublicBaseUrl` trong `appsettings.Development.json` để signed URL trỏ đúng.

Công cụ gửi request có sẵn: `CoreHr.http` (REST Client), `tests/smoke/corehr_smoke.py` và `tests/smoke/identity_smoke.py`
(phủ toàn bộ ADM-01/ADM-02: đăng nhập, luân chuyển refresh token, phát hiện replay, khoá tạm, buộc đổi mật khẩu, cấp/thu
hồi vai trò — tự tạo tài khoản dùng một lần nên chạy lại được nhiều lần). Các module Recruitment,
Contracts, Probation và Offboarding chưa có collection riêng; dùng `/openapi/v1.json` do `MapOpenApi` sinh ra hoặc Postman import
`api/openapi.yaml`.

**3. Integration test** — `tests/Qlns.IntegrationTests` chưa tồn tại. `docs/architecture.md` §5.6 coi đây là điều kiện bắt buộc trước khi một operation được tính là `implemented`, vì các invariant mạnh nhất (partial unique index `ux_offers_one_open_per_application`, `ux_contracts_primary_active`, `ux_offboarding_open_case`, `ux_probation_review_contract`; conditional update theo version; các truy vấn EF phức tạp như cửa sổ cảnh báo hết hạn hay đếm task chặn) chỉ có thể kiểm chứng trên PostgreSQL thật. Ngăn xếp đề xuất: `WebApplicationFactory` cộng Testcontainers.

**Xác thực.** Token do chính API phát hành: `Authentication:Jwt:SigningKey` là **bắt buộc** — thiếu nó `AddIdentityAccess`
ném lỗi ngay khi khởi động thay vì chạy với một API nửa vời. `appsettings.Development.json` đã có khóa phát triển sẵn;
môi trường thật phải đặt qua user secrets hoặc biến môi trường `Authentication__Jwt__SigningKey` (tối thiểu 32 ký tự).

Lối tắt persona `/dev/token` vẫn được giữ cho smoke test: nó chỉ hoạt động khi môi trường là Development **và**
`Authentication:DevelopmentSigningKey` có giá trị, và bộ xử lý bearer khi đó nhận **cả hai** issuer. Ngoài Development,
`/dev/token`, `/dev/document-content` và `/dev/offer-token` không được đăng ký. Không có nhánh dự phòng sang OIDC
authority bên ngoài: federation sẽ thay cả cụm endpoint đăng nhập, nên nó cần một ADR mới chứ không phải một `if`.

## Decisions taken while implementing (not in the contract)

- Schema v1.1 deltas (`offers.currency`, `contracts.currency`, `interview_panelists`, `contract_addenda.version` as ETag) — see [database/README §2.6](../../database/README.md#26-delta-v11--phát-hiện-khi-triển-khai).
- Schema v1.2 deltas (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`, and `user_roles.role_code` now a foreign key) — see [database/README §2.7](../../database/README.md#27-delta-v12--đưa-xác-thực-về-nội-bộ).
- The role → permission matrix is data, not code: `seed_roles.sql` owns it, sign-in resolves it with `user_roles ⋈ role_permissions`. The persona map in `DevelopmentAuthentication` is a Development-only duplicate of the same matrix.
- An administrator cannot disable, reset or re-grant their own account — it would either lock the organization out of administration or let someone escalate without a second pair of eyes.
- Sign-in with an unknown e-mail still runs one password verification (against `IPasswordHasher.DummyHash`) so that response time cannot be used to enumerate accounts; a disabled account is reported only after the password verified.
- `Idempotency-Key` is validated (16–128 chars) but idempotency itself comes from natural keys: a completed intake replays its application, an evaluator's locked scorecard is immutable (409), an accepted offer replays the handoff ids.
- Requisition reject returns the requisition to `draft`; `jobCode` is `REQ-{yyyy}-{id:D5}`.
- Interview scheduling requires the application to be in `ai_screening`, `tech_interview` or `executive_round` (advancing into `tech_interview` needs a scheduled interview, so the interview comes first).
- Evaluation unlock inserts a new version copying the scores with the unlock metadata; the evaluator's next submission inserts the following version. `overallScore` is an equal-weight mean rounded to one decimal.
- Offers expire lazily (a `sent` offer past `expirationDate` is persisted as `expired` when next touched) in addition to the worker method.
- Contract activation auto-expires a predecessor primary contract whose `endDate` precedes the new `startDate`; a true overlap needs `allowPrimaryOverlap` plus the approve permission and terminates the predecessor. Probation contracts must be ≤ 60 days.
- Probation review stores the reviewer's recommendation in `outcome` while `in_review`; `decide` may override it. Offboarding `complete` may bypass blocking tasks only with the approve permission and a reason (audited), never the final-settlement check.
