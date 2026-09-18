# ADR-007 — Ports/adapters, a transactional outbox and reliable delivery

- **Status:** Proposed — **the code already follows this decision**, awaiting Project Owner confirmation
- **Owner:** none yet
- **Related:** [ADR-002](002-modular-monolith.md), [ADR-005](005-postgresql-system-of-record.md), constraint C5, quality goal Q7

## Context

The system has to send e-mails, calendar invitations (`.ics`), offer letters and onboarding notifications; store CVs and
contract documents in object storage; and scan for malware and parse CVs. Distributed transactions with those providers
are ruled out by constraint C5.

## Decision

Every external system sits behind a **port** declared in `Qlns.BusinessLogic` (`IDocumentStorage`, `IMalwareScanner`,
`IResumeParser`, `IOfferResponseTokenService`, `IAccessTokenIssuer`, …), with the adapter in `Qlns.DataAccess`.

Outbound side effects are **never** invoked inside the business transaction. Instead, the command writes an
`outbox_messages` row in the same transaction as the business change; the worker claims and sends it after the commit,
with an idempotency key, a timeout, bounded retries and a dead-letter path for reconciliation.

## Alternatives considered

- **Call the provider directly from the service** — rejected: a provider timeout would either roll back a perfectly
  valid business change, or leave committed state whose notification is never sent.
- **A dedicated message broker (RabbitMQ/Kafka) from the start** — rejected for now: it adds an operational component
  while an outbox on PostgreSQL itself is sufficient for the target load. Still open should the system split into
  services.

## Consequences

- Business state and side effects are eventually consistent, not immediately consistent — the UI has to reflect that.
- Every adapter today is a **development adapter** (`FileSystemDocumentStorage`, `DevelopmentOnlyMalwareScanner`,
  `DevelopmentOnlyResumeParser`) and must be replaced before any shared environment.
- `Qlns.Worker` does not exist yet, so the three worker entry points and the outbox dispatcher are currently callable
  only from tests.
