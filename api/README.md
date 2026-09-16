# QLNS API contract

[`openapi.yaml`](openapi.yaml) is the contract-first OpenAPI 3.0.3 target for the two modules selected for first delivery — **Core HR** (including Contracts) and **Recruitment (ATS)** — plus Reports and Administration. It currently defines **87 operations across 70 paths**.

The ASP.NET Core application must preserve operation IDs, schemas, status codes and error codes from this document. `x-requirement` links each operation to the requirements baseline; **presence in the contract does not by itself mean the operation is implemented** — check `x-implementation-status` on each operation.

Attendance & Leave is out of scope. Its contract fragment (24 paths, 46 schemas) is parked in [docs/deferred/attendance_leave/openapi_attendance_leave.yaml](../docs/deferred/attendance_leave/openapi_attendance_leave.yaml) and is not part of this document.

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

No operation in this contract is implemented yet; every operation carries `x-implementation-status: proposed`. The source under `src/backend` is a structural skeleton that fixes the project layout (`Qlns.Api` → `Qlns.BusinessLogic` ← `Qlns.DataAccess`) and includes one sample module to show where controller, service, repository and unit tests belong. It is not production code.

All operations in the current contract are target contracts for phased implementation. Backend code is organized by `Modules/<Module>/<Feature>` so each OpenAPI tag has an explicit owner.

## Where server-side calculation is mandatory

Several operations deliberately ignore client-supplied values because the server owns the rule. Clients must display what the server returns rather than recomputing it.

| Operation | Server-owned value | Ignored if sent by client |
|---|---|---|
| `POST /api/v1/recruitment/applications/{applicationId}/advance` | next `stage`, new `version` | any client-side stage arithmetic |
| `POST /api/v1/recruitment/interviews/{interviewId}/evaluations` | `overallScore` — weighted per position policy | any client-side average |
| `POST /api/v1/offboarding/cases` | `noticePeriodShortfallDays`, `blockingTasksOutstanding` | both |
| `POST /api/v1/probation-reviews/{reviewId}/decide` | the `employee_events` row created for the outcome | — |
| `GET /api/v1/reports/*` | every KPI value and the refresh timestamp | any client-side aggregation |

## Idempotency and duplicate handling

`Idempotency-Key` makes a retried create return the original resource instead of creating a second one. Offer acceptance is additionally idempotent by design: one accepted offer yields at most one employee, one initial contract and one onboarding checklist, regardless of retries.
