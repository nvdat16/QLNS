# Architecture Decision Records

Every architecture decision gets its own file, numbered sequentially, and a number is never reused. Once an ADR is
published its decision text is **not edited**; to change course, write a new ADR and mark the old one
`Superseded by ADR-xxx`.

## Status vocabulary

| Status | Meaning |
|---|---|
| `Proposed` | Written down, but without a business owner or an approval. **Not** settled, even if the code already follows it. |
| `Accepted` | Has an owner, an approval date, alternatives and consequences. |
| `Superseded` | Replaced by a later ADR; the header names which one. |
| `Rejected` | Considered and turned down; kept so the argument is not had twice. |

No ADR becomes `Accepted` merely because a technology turns up in a prototype, a diagram, a DDL file or the source code.

## Index

| ADR | Decision | Status |
|---|---|---|
| [ADR-001](001-three-tier-three-layer.md) | Three-tier React – ASP.NET Core API – PostgreSQL, with a three-layer backend | Accepted 2026-09-15 |
| [ADR-002](002-modular-monolith.md) | A modular monolith backend before microservices | Proposed |
| [ADR-003](003-backend-enforces-authorization.md) | The backend enforces authorization and business rules | Proposed |
| [ADR-004](004-contract-first-openapi.md) | REST/JSON, DTOs and a contract-first OpenAPI 3.0.3 document | Accepted 2026-09-15 |
| [ADR-005](005-postgresql-system-of-record.md) | PostgreSQL as the system of record, with versioned migrations | Proposed |
| [ADR-006](006-explicit-commands-and-state-machines.md) | Explicit commands and state transitions | Proposed |
| [ADR-007](007-ports-adapters-and-outbox.md) | Ports/adapters, a transactional outbox and reliable delivery | Proposed |
| [ADR-008](008-feature-based-react-frontend.md) | A feature-based React frontend with a shared API client | Accepted 2026-09-15 |
| [ADR-009](009-dotnet-10-efcore-postgresql.md) | .NET 10, ASP.NET Core, EF Core and PostgreSQL | Accepted 2026-09-15 |
| [ADR-010](010-delivery-scope-two-pillars.md) | Narrowing the delivery scope to the Recruitment and Core HR pillars | Accepted 2026-09-17 |
| [ADR-011](011-in-house-identity.md) | Authentication and account administration in house, with no external Identity Provider | Accepted 2026-09-18 |

## Open governance debt

The five ADRs below are still `Proposed`, yet **the code has been written on top of them**. This is real governance debt,
not a typo: if any one of them were rejected, the corresponding code would have to be rewritten rather than reconfigured.

| ADR | What the code depends on |
|---|---|
| ADR-002 | The whole `Modules/<Module>/<Feature>` tree across all three layers |
| ADR-003 | Every `*Policies` class in `Qlns.Api` and every data-scope check inside a service |
| ADR-005 | `schema.sql`, every EF Core repository, and the partial unique indexes that carry the invariants |
| ADR-006 | Every action endpoint (`/approve`, `/advance`, `/complete`, …) and the state machines in the domain |
| ADR-007 | `outbox_messages`, `CoreHrOutbox.Message` and the `IDocumentStorage` / `IResumeParser` / `IMalwareScanner` ports |

Each of these needs the Project Owner either to confirm it, or to record explicitly that approving the code implied
approving the decision.
