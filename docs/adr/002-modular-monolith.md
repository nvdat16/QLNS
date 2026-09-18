# ADR-002 — A modular monolith backend before microservices

- **Status:** Proposed — **the code already follows this decision**, awaiting Project Owner confirmation
- **Owner:** none yet
- **Related:** [ADR-001](001-three-tier-three-layer.md), [ADR-007](007-ports-adapters-and-outbox.md)

## Context

The delivery scope contains four modules (Recruitment, Core HR, Contracts, Identity & Access) whose data is tightly
coupled: accepting an offer creates an employee and a contract in **one** transaction, a probation decision writes an
`employee_events` row, and completing an offboarding case writes a termination event. The team is small and has no
distributed operations platform.

## Decision

A single deployable, divided into business modules: `Modules/<Module>/<Feature>` repeated across all three layers, with
module names matching the OpenAPI tags. Each table has exactly one owning module; another module that needs to read or
write it goes through the owner's interface rather than treating the table as an implicit API.

One exception is recorded explicitly: commands that require a single transaction across modules (offer accept →
employee + contract + onboarding_tasks) may write another module's entities **through that module's own entity types**,
never over HTTP.

## Alternatives considered

- **Microservices per module** — rejected: it would turn the single-transaction invariants above into sagas, buying a
  distributed-consistency problem that the current scale does not require.
- **A monolith with no module structure** — rejected: it forfeits both the option to split later and the notion of data
  ownership.

## Consequences

- Splitting into services later remains feasible, because module boundaries and table ownership are already explicit —
  but it will have to address exactly the cross-module transactions listed in `src/backend/README.md`.
- A `NoCrossModuleTableWrites` fitness function is needed so the boundary does not erode; it is still `Planned`.
