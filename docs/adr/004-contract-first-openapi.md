# ADR-004 — REST/JSON, DTOs and a contract-first OpenAPI 3.0.3 document

- **Status:** Accepted 2026-09-15
- **Owner:** Architect
- **Related:** [ADR-006](006-explicit-commands-and-state-machines.md), [ADR-008](008-feature-based-react-frontend.md)

## Context

Frontend and backend are built in parallel, and the business documents must be traceable down to individual endpoints.
If the contract were generated **from** the code, every code change would silently become a contract change.

## Decision

`docs/api/openapi.yaml` is the contract, written first, and it is the source of truth that beats `API_REFERENCE.md`. The
application must preserve the contract's operation IDs, schemas, status codes and error codes. Each operation carries an
`x-requirement` pointing back to the requirements baseline and an `x-implementation-status` saying how far it has got.

Mandatory conventions: base path `/api/v1`; `camelCase` JSON; RFC 3339 UTC timestamps; errors as
`application/problem+json` with a stable `code` and a `correlationId`; mutable aggregates expose an `ETag` and require
`If-Match`; collections must use bounded pagination with an allowlist for filter and sort.

## Alternatives considered

- **Code-first, generating OpenAPI from the controllers** — rejected: the contract would be "correct" by definition and
  would lose its role as a gate.
- **GraphQL** — rejected: per-field data scope and bounded pagination are considerably harder to enforce than in REST
  for this problem.

## Consequences

- A breaking change requires a new version or an accepted ADR, plus a contract diff — the `OpenApiBreakingChangeGate`,
  still `Planned`.
- A complete contract is easily mistaken for a working API; that is risk R1b and the reason `x-implementation-status`
  exists.
