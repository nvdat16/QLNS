# ADR-010 — Narrowing the delivery scope to the Recruitment and Core HR pillars

- **Status:** Accepted 2026-09-17 — **the owner and the full alternatives/consequences still await Project Owner confirmation**
- **Owner:** none yet
- **Related:** [ADR-011](011-in-house-identity.md)

## Context

The `topdown-approach.png` function map (updated 2026-09-17) has seven pillars. The original design spread wider than
this delivery can carry, and Attendance & Leave in particular had already been specified in full (SRS, 13 stories, DDL
for 13 tables, 24 endpoints).

## Decision

The delivery scope is **exactly the leaf functions printed in bold** under the two pillars Recruitment (18 leaves) and
Core HR (16 leaves, including the Contract Management branch).

Out of scope: Reports & Analytics, Performance Management, Compensation & Benefits, Attendance & Leave Management, the
remainder of System Administration, and the **four non-bold leaf functions sitting inside the two selected pillars** —
Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart, and Suspension & Return to Work.

Authorization, audit logging and the transactional outbox **remain mandatory** for every command; only the screens and
endpoints that administer them are out of scope.

## Alternatives considered

- **Delete the Attendance & Leave design outright** — rejected: it would waste a complete specification. It was moved
  intact to `docs/deferred/attendance_leave/` with a warning that authoritative documents must not reference it.
- **Keep the four non-bold leaf functions because "they are nearly done anyway"** — rejected: each brings its own
  screens, endpoints and business rules.

## Consequences

- Narrowing the scope **does not change the DDL**: what was dropped is screens and endpoints, not data structures.
- `employees.status = 'suspended'` and `employee_events.event_type IN ('suspension','return_to_work')` become **reserved
  values**: present in the schema, settable by no action endpoint
  ([ADR-006](006-explicit-commands-and-state-machines.md)).
- Offboarding therefore requires an employee who is `active` or `probation`.
- Risk R2b: the design under `deferred/` will drift from the canonical schema if that module ever returns.
