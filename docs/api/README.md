# QLNS API contract

[`openapi.yaml`](openapi.yaml) is the contract-first OpenAPI 3.0.3 target for the two modules selected for first delivery — **Recruitment (ATS)** and **Core HR** (including Contracts). Its scope is exactly the leaf functions printed in bold under those two pillars in [`topdown-approach.png`](../../topdown-approach.png); see [README · Functional architecture](../../README.md#2-delivery-scope--seven-pillars-two-selected). It currently defines **79 operations across 62 paths**, including the **Identity & Access (ADM)** module: password sign-in, refresh-token rotation and account/role administration are served by this API itself.

The ASP.NET Core application must preserve operation IDs, schemas, status codes and error codes from this document. `x-requirement` links each operation to the requirements baseline; **presence in the contract does not by itself mean the operation is implemented** — check `x-implementation-status` on each operation.

Attendance & Leave is out of scope. Its contract fragment (24 paths, 46 schemas) is parked in [docs/deferred/attendance_leave/openapi_attendance_leave.yaml](../deferred/attendance_leave/openapi_attendance_leave.yaml) and is not part of this document. Everything else left out is listed in [Out of scope](#out-of-scope).

Human-readable endpoint documentation: [`API_REFERENCE.md`](API_REFERENCE.md).

## Conventions

- Base path: `/api/v1`.
- JSON properties: `camelCase`; timestamps: RFC 3339 UTC.
- Authentication: JWT bearer token issued by `POST /api/v1/auth/login` and signed with `Authentication:Jwt:SigningKey`. Short-lived (30 min by default); renewed through `POST /api/v1/auth/refresh` with a single-use refresh token.
- Authorization: permission plus data scope, enforced server-side.
- Errors: `application/problem+json` with a stable `code` for business conflicts and `correlationId` for support.
- Commands use explicit action endpoints; workflow status is never changed by a generic `PATCH`.
- Mutable aggregates expose an `ETag`; commands require `If-Match` to prevent lost updates.
- Collection endpoints must use bounded pagination and documented filter/sort allowlists.
- Breaking changes require a new API version or an accepted ADR and contract-diff approval.

## Current implementation

Every operation now carries `x-implementation-status: code-complete`: controller, endpoint authorization policy, business workflow (service + domain), transactional persistence with audit and outbox rows, and unit tests exist under `src/backend` for all 79 operations, organized by `Modules/<Module>/<Feature>` so each OpenAPI tag has an explicit owner (see [src/backend/README.md](../../src/backend/README.md)).

`code-complete` is deliberately not `implemented`: the repository's definition of *implemented* also requires integration/contract tests against PostgreSQL (`tests/Qlns.IntegrationTests`, still to be created). Until then the strongest invariants — partial unique indexes and conditional updates — are only exercised by the database itself. The ADM module has not yet been exercised end-to-end against a live PostgreSQL instance either; its unit tests cover the decisions, not the SQL.

Two schema deltas surfaced while implementing and were folded into `database/schema.sql` v1.1: `offers.currency` / `contracts.currency`, the `interview_panelists` table behind `InterviewWrite.interviewerUserIds`, and `contract_addenda.version` as the addendum ETag (see [database/README.md §2.6](../../database/README.md#26-delta-v11--phát-hiện-khi-triển-khai)).

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
| System Administration — audit-log lookup, delivery monitoring, integration / notification / approval-workflow configuration | Still out of scope as API surface; those settings live in `appsettings`. **Account management, roles and role grants are now in scope** under the `Identity & Access` tag (`/api/v1/auth/*`, `/api/v1/admin/users`, `/api/v1/admin/roles`). |
| Performance Management, Compensation & Benefits | Separate pillars, not selected for this delivery. |
| Attendance & Leave Management | Parked in `docs/deferred/attendance_leave/`. |
| Headcount & Budget Validation (Recruitment) | Requisition approval stays, but the server performs no automatic headcount or salary-budget check. `targetHeadcount`, `salaryMin` and `salaryMax` are declared data, not a control; the HR Manager decides. |
| Recruitment Channel Management (Recruitment) | Publish / update / close a posting stay in scope, but there is no channel list — a posting goes to the single built-in careers channel. |
| Organizational Chart (Core HR) | The tree view and its endpoint are out. Departments & organizational hierarchy stay in scope, so `parent_department_id`, parent-child rules, cycle prevention and delete constraints are unchanged. |
| Suspension & Return to Work (Core HR) | No endpoint sets a suspension or a return to work. The `suspended` status and the `suspension` / `return_to_work` event types remain reserved values in the schema. Offboarding requires an `active` or `probation` employee. |

Two distinctions matter, because dropping the admin API does **not** drop the crosscutting mechanisms:

- **Audit log and transactional outbox are still mandatory.** Every business change writes its `audit_logs` row in the same transaction, and every email or calendar notification goes through `outbox_messages`. Only the endpoints that browse audit logs or retry deliveries are out of scope.
- **Identity is served in-house.** `users`, `user_credentials`, `user_roles`, `roles`, `role_permissions` and `refresh_tokens` carry identity, credentials and data scope; server-side permission plus data-scope checks still apply to every request. Provisioning accounts and assigning roles happens through `/api/v1/admin/*` — there is no external Identity Provider, and `Authentication:Jwt:SigningKey` is required for the API to start. There is deliberately no silent fallback to an external authority: federating would replace the sign-in endpoints, not just a configuration value, so it needs its own ADR.

### Security properties of the ADM module

| Property | How it is achieved |
|---|---|
| Passwords are never recoverable | PBKDF2-HMAC-SHA512, 210 000 iterations, 128-bit per-password salt; parameters encoded in `user_credentials.password_hash` so they can be raised without a migration. |
| Sign-in does not disclose which e-mails exist | An unknown e-mail is verified against a dummy hash, so timing and response are the same as for a real account; a disabled account is only reported after the password was verified. |
| Online guessing is bounded | 5 consecutive failures lock the credential for 15 minutes; every attempt, successful or not, writes an `audit_logs` row. |
| A stolen refresh token is detectable | Tokens are single-use and stored only as a SHA-256 digest; replaying a consumed token revokes the whole family of that account. |
| Withdrawn authority takes effect quickly | Permissions and data scope are re-read on every sign-in and refresh; disabling an account revokes its refresh tokens. The access-token lifetime (30 min) is the documented upper bound. |
| No administrator can silently escalate | An administrator cannot disable, reset or re-grant their own account; the role → permission matrix is reference data in `database/seed_roles.sql`, not an API-writable table. |

`GET /health/live` and `GET /health/ready` are kept under the `Operations` tag. They are infrastructure probes for the deployment view, not business functions on the function map.

## Idempotency and duplicate handling

`Idempotency-Key` makes a retried create return the original resource instead of creating a second one. Offer acceptance is additionally idempotent by design: one accepted offer yields at most one employee, one initial contract and one onboarding checklist, regardless of retries.
