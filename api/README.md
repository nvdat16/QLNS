# QLNS API contract

[`openapi.yaml`](openapi.yaml) is the contract-first OpenAPI 3.0.3 target for the three modules selected for first delivery — **Core HR** (including Contracts), **Recruitment (ATS)** and **Attendance & Leave** — plus Reports and Administration. It currently defines **121 operations across 94 paths**.

The ASP.NET Core application must preserve operation IDs, schemas, status codes and error codes from this document. `x-requirement` links each operation to the requirements baseline; **presence in the contract does not by itself mean the operation is implemented** — check `x-implementation-status` on each operation.

Attendance/Leave operations stay `discovery-required` until business policy is approved, even though the canonical database schema for them now exists. The blocking decisions are tracked in [Open Decisions — Attendance & Leave](../docs/open_decisions_attendance_leave.md).

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

`POST /api/v1/recruitment/applications/{applicationId}/advance` advances exactly one canonical stage. A success atomically writes the application update, `application_stage_events` row and `audit_logs` row. Invalid workflow prerequisites and stale versions return `409` without partial writes. `GET /api/v1/recruitment/applications/{applicationId}` reads one application within the caller's data scope and returns its `ETag`.

Those two operations are the whole of the implemented surface. What exactly was built, and the patterns it establishes for later slices, is documented in [Vertical Slice `REC-03.2`](../docs/vertical_slice_rec_03_2.md).

All other operations in the current contract are target contracts for phased implementation. Backend code is organized by `Modules/<Module>/<Feature>` so each OpenAPI tag has an explicit owner.

## Where server-side calculation is mandatory

Several operations deliberately ignore client-supplied values because the server owns the formula. Clients must display what the server returns rather than recomputing it — otherwise the number on screen can diverge from the number that reaches payroll.

| Operation | Server-owned value | Ignored if sent by client |
|---|---|---|
| `POST /api/v1/leave/requests` | `requestedUnits` — derived from work schedule, holiday calendar and leave policy | any client-side day count |
| `GET /api/v1/leave/balances` | `availableUnits` — a generated column in PostgreSQL | any client-side subtraction |
| `POST /api/v1/attendance/events` | `status`, `workedMinutes`, `lateMinutes`, `earlyLeaveMinutes` | — |
| `POST /api/v1/attendance/overtime-requests` | `overtimeCategory`, `workCoefficient` | both |
| `GET /api/v1/attendance/timesheets` | every minute total, plus the `policyVersion` used | any client-side aggregation |

## Idempotency and duplicate handling

Device attendance ingestion is the one place where a duplicate is **not** an error: `POST /api/v1/integrations/attendance/events` returns `202` with `duplicate: true` for an event it has already stored. An offline device replaying its buffer must be a safe operation — returning `409` there would make the device either retry forever or drop data.

Everywhere else, `Idempotency-Key` makes a retried create return the original resource instead of creating a second one.
