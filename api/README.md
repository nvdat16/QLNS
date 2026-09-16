# QLNS API contract

[`openapi.yaml`](openapi.yaml) is the contract-first OpenAPI 3.0.3 target for Recruitment, Core HR and Contract Management. The ASP.NET Core application must preserve operation IDs, schemas, status codes and error codes from this document. `x-requirement` links each operation to the requirements baseline; presence in the contract does not by itself mean the operation is implemented.

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

`POST /api/v1/recruitment/applications/{applicationId}/advance` advances exactly one canonical stage. A success atomically writes the application update, `application_stage_events` row and `audit_logs` row. Invalid workflow prerequisites and stale versions return `409` without partial writes.

All other operations in the current contract are target contracts for phased implementation. Backend code is organized by `Modules/<Module>/<Feature>` so each OpenAPI tag has an explicit owner.
