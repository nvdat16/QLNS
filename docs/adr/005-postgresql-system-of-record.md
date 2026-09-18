# ADR-005 — PostgreSQL as the system of record, with versioned migrations

- **Status:** Proposed — **the code already follows this decision**, awaiting Project Owner confirmation
- **Owner:** none yet
- **Related:** [ADR-009](009-dotnet-10-efcore-postgresql.md), constraint C3, quality goal Q2

## Context

HR data needs genuine ACID guarantees: an accepted offer must create the employee, the contract and the onboarding
checklist, or create nothing at all. Several of the most important invariants are of the *"only one open record"* kind —
one open offer per application, one active primary contract, one open offboarding case.

## Decision

PostgreSQL is the single source of truth for business data. The invariants above are enforced by **partial unique
indexes** in the database (`ux_offers_one_open_per_application`, `ux_contracts_primary_active`,
`ux_offboarding_open_case`, `ux_probation_review_contract`), not only by application-level checks. Contended updates use
a conditional update on `version`.

`database/schema.sql` is the **interim** canonical contract until the first EF Core migration is generated and approved.
At that point the migration becomes the deployment source and `schema.sql` must be drift-checked in CI. Migrations run
as a separate release step; ORM auto-create is never used in a shared environment.

## Alternatives considered

- **Enforce the invariants in the application only** — rejected: two concurrent requests would both pass the check and
  both write.
- **Keep `schema.sql` as the permanent source and skip migrations** — rejected: it offers no way to upgrade a database
  that already holds data.

## Consequences

- The strongest invariants **cannot** be verified against a faked repository, so `tests/Qlns.IntegrationTests` against a
  real PostgreSQL is a precondition for any operation reaching `implemented`. That project does not exist yet — this is
  risk R2.
- The four v1.1 deltas and the four v1.2 deltas must be carried intact into the first migration (see
  `database/README.md` §2.6 and §2.7).
