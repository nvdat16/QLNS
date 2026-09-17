# QLNS API contract

[`openapi.yaml`](openapi.yaml) is the contract-first OpenAPI 3.0.3 target for the two modules selected for first delivery — **Recruitment (ATS)** and **Core HR** (including Contracts). Its scope is exactly the leaf functions printed in bold under those two pillars in [`topdown-approach.png`](../topdown-approach.png); see [README · Functional architecture](../README.md#2-delivery-scope--seven-pillars-two-selected). It currently defines **66 operations across 51 paths**.

The ASP.NET Core application must preserve operation IDs, schemas, status codes and error codes from this document. `x-requirement` links each operation to the requirements baseline; **presence in the contract does not by itself mean the operation is implemented** — check `x-implementation-status` on each operation.

Attendance & Leave is out of scope. Its contract fragment (24 paths, 46 schemas) is parked in [docs/deferred/attendance_leave/openapi_attendance_leave.yaml](../docs/deferred/attendance_leave/openapi_attendance_leave.yaml) and is not part of this document. Everything else left out is listed in [Out of scope](#out-of-scope).

Human-readable endpoint documentation: [`API_REFERENCE.md`](API_REFERENCE.md).

## Conventions

- Base path: `/api/v1`.
- JSON properties: `camelCase`; timestamps: RFC 3339 UTC.
- Authentication: OIDC-issued JWT bearer token.
- Authorization: permission plus data scope, enforced server-side.
- Errors: `application/problem+json` with a stable `code` for business conflicts and `correlationId` for support.
- Commands use explicit action endpoints; workflow status is never changed by a generic `PATCH`.
- Mutable aggregates expose an `ETag`; commands require `If-Match` to prevent lost updates.
- Collection endpoints must use bounded pagination and documented filter/sort allowlists.
- Breaking changes require a new API version or an accepted ADR and contract-diff approval.

## Current implementation

Every operation now carries `x-implementation-status: code-complete`: controller, endpoint authorization policy, business workflow (service + domain), transactional persistence with audit and outbox rows, and unit tests exist under `src/backend` for all 66 operations, organized by `Modules/<Module>/<Feature>` so each OpenAPI tag has an explicit owner (see [src/backend/README.md](../src/backend/README.md)).

`code-complete` is deliberately not `implemented`: the repository's definition of *implemented* also requires integration/contract tests against PostgreSQL (`tests/Qlns.IntegrationTests`, still to be created) and authorization against the real Identity Provider. Until then the strongest invariants — partial unique indexes and conditional updates — are only exercised by the database itself.

Two schema deltas surfaced while implementing and were folded into `database/schema.sql` v1.1: `offers.currency` / `contracts.currency`, the `interview_panelists` table behind `InterviewWrite.interviewerUserIds`, and `contract_addenda.version` as the addendum ETag (see [database/README.md §2.6](../database/README.md#26-delta-v11--phát-hiện-khi-triển-khai)).

## Where server-side calculation is mandatory

Several operations deliberately ignore client-supplied values because the server owns the rule. Clients must display what the server returns rather than recomputing it.

| Operation | Server-owned value | Ignored if sent by client |
|---|---|---|
| `POST /api/v1/recruitment/applications/{applicationId}/advance` | next `stage`, new `version` | any client-side stage arithmetic |
| `POST /api/v1/recruitment/interviews/{interviewId}/evaluations` | `overallScore` — weighted per position policy | any client-side average |
| `POST /api/v1/offboarding/cases` | `noticePeriodShortfallDays`, `blockingTasksOutstanding` | both |
| `POST /api/v1/probation-reviews/{reviewId}/decide` | the `employee_events` row created for the outcome | — |

## Out of scope

| Left out | Why / what remains |
|---|---|
| Reports & Analytics, including authorized report export | Not a bold leaf under the two selected pillars. No `/reports` path and no export/download contract in this document. |
| System Administration endpoints — account management, roles and role grants, audit-log lookup, delivery monitoring, integration / notification / approval-workflow configuration | Out of scope as API surface. Integration, notification and approval settings live in `appsettings`; there is no UI or API for them. |
| Performance Management, Compensation & Benefits | Separate pillars, not selected for this delivery. |
| Attendance & Leave Management | Parked in `docs/deferred/attendance_leave/`. |
| Headcount & Budget Validation (Recruitment) | Requisition approval stays, but the server performs no automatic headcount or salary-budget check. `targetHeadcount`, `salaryMin` and `salaryMax` are declared data, not a control; the HR Manager decides. |
| Recruitment Channel Management (Recruitment) | Publish / update / close a posting stay in scope, but there is no channel list — a posting goes to the single built-in careers channel. |
| Organizational Chart (Core HR) | The tree view and its endpoint are out. Departments & organizational hierarchy stay in scope, so `parent_department_id`, parent-child rules, cycle prevention and delete constraints are unchanged. |
| Suspension & Return to Work (Core HR) | No endpoint sets a suspension or a return to work. The `suspended` status and the `suspension` / `return_to_work` event types remain reserved values in the schema. Offboarding requires an `active` or `probation` employee. |

Two distinctions matter, because dropping the admin API does **not** drop the crosscutting mechanisms:

- **Audit log and transactional outbox are still mandatory.** Every business change writes its `audit_logs` row in the same transaction, and every email or calendar notification goes through `outbox_messages`. Only the endpoints that browse audit logs or retry deliveries are out of scope.
- **`users` and `user_roles` still exist** in the canonical schema as identity and data-scope data, and server-side permission plus data-scope checks apply to every request. What is out of scope is managing those accounts and grants through this API: provisioning users and assigning roles is the external Identity Provider's job.

`GET /health/live` and `GET /health/ready` are kept under the `Operations` tag. They are infrastructure probes for the deployment view, not business functions on the function map.

## Idempotency and duplicate handling

`Idempotency-Key` makes a retried create return the original resource instead of creating a second one. Offer acceptance is additionally idempotent by design: one accepted offer yields at most one employee, one initial contract and one onboarding checklist, regardless of retries.
