# ADR-001 — Three-tier architecture with a three-layer backend

- **Status:** Accepted 2026-09-15
- **Owner:** Architect
- **Related:** [ADR-002](002-modular-monolith.md), [ADR-003](003-backend-enforces-authorization.md), [ADR-009](009-dotnet-10-efcore-postgresql.md)

## Context

The system serves three very different audiences — external candidates, internal employees and administrators — over the
same body of sensitive HR data. The starting point was a set of static HTML prototypes with simulated data, so the
biggest risk was business rules drifting into the presentation layer and data being reached without a control point.

## Decision

Three **runtime tiers**: the React web application (presentation), the ASP.NET Core API plus the .NET worker
(application), and PostgreSQL (data). Inside the application tier there are three **source layers**: `Qlns.Api`
(presentation), `Qlns.BusinessLogic` (business) and `Qlns.DataAccess` (data access). Dependencies always point inward:
the business layer references neither ASP.NET Core nor EF Core, and `Qlns.Api` touches the data layer only at the
composition root to register dependencies.

The worker does **not** form a fourth tier — it sits in the application tier and differs only in that it takes no direct
user requests.

## Alternatives considered

- **Two tiers, with the client reaching the database** — rejected: permission and data scope cannot be enforced
  server-side, which violates Q1.
- **Full hexagonal/clean architecture with a separate domain project** — rejected for now: it adds another project
  boundary without a need to replace persistence. The ports/adapters boundary that is genuinely needed is covered by
  [ADR-007](007-ports-adapters-and-outbox.md).

## Consequences

- Every query and command must go through the backend API; the frontend has no connection string.
- The business layer is testable with plain unit tests and no database — which is why the current 1347 unit tests run
  without PostgreSQL.
- The cost: even a trivial use case passes through three layers. Accepted, because this is the boundary that protects
  Q1–Q3.
