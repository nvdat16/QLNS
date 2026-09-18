# ADR-006 — Explicit commands and state transitions

- **Status:** Proposed — **the code already follows this decision**, awaiting Project Owner confirmation
- **Owner:** none yet
- **Related:** [ADR-004](004-contract-first-openapi.md), quality goal Q3

## Context

Nearly every business entity in the system has a lifecycle with approval conditions: requisitions, applications, offers,
contracts, addenda, employee events, probation reviews and offboarding cases. A generic
`PATCH { "status": "approved" }` would erase every guard on those lifecycles at once.

## Decision

No endpoint lets a client set the `status` field directly. Each transition is its own action endpoint (`/approve`,
`/reject`, `/advance`, `/activate`, `/complete`, `/cancel`, …), requires `If-Match`, and is checked by the domain
against four things: the actor, the current state, the target state and the business guards.

A transition outside the state machine returns a business error with a stable `code`; a version conflict returns `409`.

## Alternatives considered

- **Plain REST with `PATCH` on the `status` field** — rejected: guards would have to be inferred from the old and new
  values, and every new DTO field would become another way around them.
- **A single `/transition` endpoint taking the target state** — rejected: it loses the ability to attach a distinct
  permission to each action (approving an offer and sending it are not the same right).

## Consequences

- There are more endpoints — this is the main reason the contract has 79 operations across 62 paths.
- The reserved out-of-scope values (`suspended`, `suspension`, `return_to_work`) are safe by construction: no action
  endpoint can set them, even though the schema permits them.
- An `EveryStateChangeUsesACommand` gate is needed; it is still `Planned`.
