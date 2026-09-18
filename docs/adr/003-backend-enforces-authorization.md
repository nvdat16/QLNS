# ADR-003 — The backend enforces authorization and business rules

- **Status:** Proposed — **the code already follows this decision**, awaiting Project Owner confirmation
- **Owner:** none yet
- **Related:** [ADR-001](001-three-tier-three-layer.md), [ADR-011](011-in-house-identity.md), quality goal Q1

## Context

HR data is classified confidential/restricted (constraint C6). The UI prototypes already show and hide buttons by role,
and risk R5 in the register is precisely "RBAC only hides buttons, no server-side data scope".

## Decision

Deny by default at the application boundary. Every business endpoint carries a policy requiring a permission of the form
`<module>.<feature>.<verb>`; every service additionally checks the **data scope** (`self` / `department` /
`organization`) against the requested resource. A resource outside the caller's scope returns `404`, not `403`, so that
the existence of the record is not disclosed.

Permissions sent by the client are never trusted. Frontend route guards exist for user experience only.

## Alternatives considered

- **Check permissions only, drop data scope** — rejected: a line manager would be able to read every profile in the
  company.
- **PostgreSQL row-level security** — rejected for now: scope depends on the actor and on reporting relationships, which
  is easier to express and test in an application policy. Worth revisiting as a second line of defence.

## Consequences

- Every service takes a `CoreHrActor` rather than a bare `userId`.
- `EveryBusinessEndpointRequiresAuthorization` and a deny-by-default test suite per role and scope are needed; both are
  still `Planned`.
- Returning `404` for out-of-scope resources makes logs slightly harder to read; the `correlationId` in the audit record
  compensates.
