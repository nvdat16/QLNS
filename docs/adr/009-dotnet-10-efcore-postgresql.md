# ADR-009 — .NET 10, ASP.NET Core, EF Core and PostgreSQL

- **Status:** Accepted 2026-09-15
- **Owner:** Project Owner
- **Related:** [ADR-001](001-three-tier-three-layer.md), [ADR-005](005-postgresql-system-of-record.md), constraint C4

## Context

Risk R3 records the danger of "a stack chosen from a diagram rather than through a decision process": several
technologies had appeared in the prototypes and the DDL files before anyone had formally decided on them.

## Decision

The backend uses .NET 10 with ASP.NET Core and EF Core; the database is PostgreSQL; the frontend uses React with Vite.
Approved by the Project Owner on 2026-09-15. Package patch versions must be pinned before release.

## Alternatives considered

- **Node.js/NestJS** — rejected: the team has deeper .NET experience, and EF Core's transaction and unit-of-work model
  matches the atomicity requirements of [ADR-005](005-postgresql-system-of-record.md).
- **Dapper instead of EF Core** — rejected as the default: change tracking and convenient transactions are wanted. Raw
  SQL is still used where EF expresses a query poorly.
- **SQL Server** — rejected: the canonical schema uses PostgreSQL partial (filtered) indexes, `jsonb` and exclusion
  constraints directly.

## Consequences

- The first migration can only be generated once the .NET 10 SDK is installed and the model/schema drift reviewed.
- Depending on `jsonb`, partial unique indexes and `timestamptz` makes switching database a major change rather than a
  connection-string edit.
