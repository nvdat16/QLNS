# QLNS ASP.NET Core backend

Target: .NET 10, ASP.NET Core, Entity Framework Core and PostgreSQL.

**Status:** every one of the 79 operations in [`docs/api/openapi.yaml`](../../docs/api/openapi.yaml) is `code-complete` — controller,
endpoint authorization policy, business workflow, transactional persistence (audit + outbox in the same transaction) and
unit tests (1 347 passing). What is still missing before an operation counts as `implemented` is listed in
[API_REFERENCE §7](../../docs/api/API_REFERENCE.md#7-implementation-status): integration tests against PostgreSQL, EF Core
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

Three levels, from the fastest to the closest to production.

**1. Unit tests — no database, no token**

```bash
dotnet test Qlns.sln
dotnet test Qlns.sln --filter FullyQualifiedName~Recruitment.Offers    # a single feature
```

**2. Real HTTP against PostgreSQL with a development token**

```bash
# PostgreSQL (port 5433, so it does not clash with an existing container on 5432)
docker run -d --name qlns-pg -e POSTGRES_USER=qlns_app -e POSTGRES_PASSWORD=change-me \
  -e POSTGRES_DB=qlns -p 5433:5432 postgres:17
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/schema.sql      # v1.2 — 28 tables
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/seed_roles.sql  # reference data, mandatory
docker exec -i qlns-pg psql -U qlns_app -d qlns < ../../database/seed_dev.sql

# API — launchSettings.json already sets Development and http://localhost:5080
dotnet run --project src/Qlns.Api

# Real sign-in (ADM-01). Every account in seed_dev.sql uses the password Qlns@2026.
curl -s -X POST http://localhost:5080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"hr.manager@qlns.local","password":"Qlns@2026"}'

# Or the Development-only persona shortcut, which skips the password:
curl "http://localhost:5080/dev/token?persona=hr-manager"
```

Seeded accounts: `hr.manager@`, `hr.officer@`, `eng.manager@` (Line Manager, scoped to departments 2 and 4), `dev.nguyen@`
(Employee), `it.admin@`, `recruiter@` and `admin@` (Super Admin) — all at `@qlns.local`. `ceo@qlns.local` is seeded with
`must_change_password` and **no** roles: use it to exercise the restricted session and the 401 / 403 boundaries.

Available personas (matching `seed_dev.sql` and the role matrix in `docs/user_stories.md` §6.1): `hr-manager`, `hr-officer`,
`line-manager` (also acting as Hiring Manager / Interviewer, scoped to departments 2 and 4), `recruiter`, `employee`, `it-admin`.
The candidate's offer-response token comes from `GET /dev/offer-token?offerId=` once the offer has been sent.

> The local port is **5080**, not .NET's default 5000, because on macOS AirPlay Receiver occupies 5000 and answers 403 to every request. The `servers` entry in `docs/api/openapi.yaml` already points at 5080. Change the port in `Properties/launchSettings.json`, and remember to change `Documents:PublicBaseUrl` in `appsettings.Development.json` too, so signed URLs still resolve.

Request tooling available: `CoreHr.http` (REST Client), plus `tests/smoke/corehr_smoke.py` and `tests/smoke/identity_smoke.py`
(which cover all of ADM-01/ADM-02: sign-in, refresh-token rotation, replay detection, lockout, forced password change, and
granting and revoking roles — they create their own throwaway accounts, so they can be re-run freely). The Recruitment,
Contracts, Probation and Offboarding modules have no collection of their own; use the `/openapi/v1.json` document produced by `MapOpenApi`, or import
`docs/api/openapi.yaml` into Postman.

**3. Integration tests** — `tests/Qlns.IntegrationTests` does not exist yet. `docs/architecture.md` §5.6 treats it as a precondition for an operation counting as `implemented`, because the strongest invariants (the partial unique indexes `ux_offers_one_open_per_application`, `ux_contracts_primary_active`, `ux_offboarding_open_case` and `ux_probation_review_contract`; conditional updates on version; and the more complex EF queries such as the expiry alert window or counting blocking tasks) can only be verified against a real PostgreSQL. Proposed stack: `WebApplicationFactory` plus Testcontainers.

**Authentication.** The token is issued by the API itself: `Authentication:Jwt:SigningKey` is **mandatory** — without it `AddIdentityAccess`
throws at startup rather than running a half-configured API. `appsettings.Development.json` already carries a development key;
a real environment must set it through user secrets or the `Authentication__Jwt__SigningKey` environment variable (at least 32 characters).

The `/dev/token` persona shortcut is kept for the smoke tests: it only works when the environment is Development **and**
`Authentication:DevelopmentSigningKey` has a value, in which case the bearer handler accepts **both** issuers. Outside Development,
`/dev/token`, `/dev/document-content` and `/dev/offer-token` are not registered. There is no fallback to an external OIDC
authority: federation would replace the whole sign-in endpoint group, so it needs a new ADR rather than an `if`.

## Decisions taken while implementing (not in the contract)

- Schema v1.1 deltas (`offers.currency`, `contracts.currency`, `interview_panelists`, `contract_addenda.version` as ETag) — see [database/README §2.6](../../database/README.md#26-delta-v11--surfaced-during-implementation).
- Schema v1.2 deltas (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`, and `user_roles.role_code` now a foreign key) — see [database/README §2.7](../../database/README.md#27-delta-v12--moving-authentication-in-house).
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
