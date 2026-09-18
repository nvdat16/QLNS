# QLNS Documentation

> **Status:** Design/Prototype · **Audience:** product owners, architects, developers, reviewers

The `docs/` directory is the centre of the business and architecture specification for the **QLNS / NexusHR** human resource management system.

> [!IMPORTANT]
> The repository holds a **UI/UX prototype**, a canonical database/OpenAPI contract, a **code-complete .NET 10 backend** covering all 79 operations (three layers, audit and outbox in the same transaction, 1347 unit tests) and a React frontend with real sign-in. Integration tests against PostgreSQL, runtime migrations, the background worker and the deployment infrastructure are not verified. Only an artifact explicitly marked `Implemented` counts as finished; `code-complete` is not production-ready.

> **Delivery scope:** two modules selected from the function map [`topdown-approach.png`](../topdown-approach.png) — **Core HR** (including the Contracts branch) and **Recruitment (ATS)**. How to read the map: **only the leaf functions printed in bold under those two pillars are in scope**; everything else is out, including the four non-bold leaf functions sitting inside those same two pillars. The authoritative source for scope is [section 2 of the root README](../README.md#2-delivery-scope--seven-pillars-two-selected); the detail is in [section 4](#4-functional-coverage) below.

---

## 1. Documentation Map

| Document | Purpose | Status |
|---|---|---|
| [INVEST User Stories](user_stories.md) | 28 INVEST user stories covering the two business modules and the identity module, with Gherkin acceptance criteria and traceability | Proposed · Authoritative for delivery |
| [Deferred — Attendance & Leave](deferred/attendance_leave/README.md) | The complete design of the module taken out of scope: SRS, 13 stories, use cases, 3 sequences, DDL for 13 tables, an OpenAPI fragment and its open decisions | **Out of scope · Parked** |
| [Architecture Decision Records](adr/README.md) | 11 ADRs, one file per decision: context, alternatives considered, consequences and status | Mixed — 6 Accepted · 5 Proposed |
| [Architecture — arc42 + C4](architecture.md) | The primary design document: goals, constraints, C4 Level 1/2/3, runtime, deployment, crosscutting concepts, ADR index, quality scenarios, risks and fitness functions | Proposed · Authoritative |
| [Functional Specifications](functional_specifications.md) | Requirements, roles, permissions, preconditions, business flows and acceptance rules | Proposed · Authoritative |
| [Use Cases](use_cases.md) | The overall use case diagram and the detailed ones for the two in-scope modules: Core HR and Recruitment | Proposed · Supporting |
| [Sequence Diagrams](sequence_diagrams.md) | 8 sequences across the three tiers and three layers, with both success and failure branches | Proposed · Supporting |
| [Class Diagrams](class_diagrams.md) | A domain model of 28 classes and 13 enumerations per module, the design class diagram of the "advance application" vertical slice, and the 3-layer pattern every module follows | Proposed · Supporting |
| [Database Design](../database/database_design.md) | An ERD covering all 28 canonical v1.2 tables and the field-level specification per module | Design artifact |
| [Database README](../database/README.md) | An index of the canonical schema, the deprecated legacy DDL and how to check the design | Design artifact |
| [API Contract](api/README.md) | OpenAPI 3.0.3, 79 operations across 62 paths, with `x-implementation-status` on each operation | Design artifact |
| [UI/UX README](../uiux/README.md) | An index of the HTML prototypes and the screenshots of the designed functions | Prototype artifact |

Precedence when two documents disagree:

1. **Technical boundaries** → `architecture.md`.
2. **Business rules and acceptance criteria** → `functional_specifications.md` / `user_stories.md`.
3. **Data types, constraints and invariants** → `database/schema.sql` (canonical; it beats `database_design.md` too).
4. **The API contract** → `docs/api/openapi.yaml` (it beats `API_REFERENCE.md`).
5. **Anything under `deferred/`** carries no authority over the current scope; it is only a starting point should that module come back.

---

## 2. Current Project Scope

| Area | Existing artifacts | Not implemented | Blocked by |
|---|---|---|---|
| Core HR — Profile & Organization | UI prototype; canonical schema; OpenAPI; user stories with AC; **backend code-complete** (EMP-01, EMP-02) | integration tests | migrations |
| Core HR — Lifecycle (onboarding, movements, probation, offboarding) | UI prototype (onboarding); canonical schema; OpenAPI; user stories with AC; **backend code-complete** (EMP-03 … EMP-07) | integration tests; the Effective-Date Worker and the account-disabling worker | migrations, Payroll (final settlement) |
| Core HR — Contracts | UI prototype; canonical schema; OpenAPI; user stories with AC; **backend code-complete** (CON-01 … CON-03) | integration tests; the expiry/alert worker; a real object store | migrations |
| Recruitment ATS | UI prototype; canonical schema and OpenAPI; **backend code-complete** (REC-01 … REC-06) | integration tests; the outbox worker (e-mail, `.ics`, offer tokens); a real CV parser and malware scanner | migrations, a PostgreSQL runtime |
| Attendance & Leave | UI prototype; the full design moved to `deferred/` | — | **Out of scope** — not being built |
| Identity & Access (ADM) | Sign-in UI and the account administration screen; canonical schema (6 identity tables); OpenAPI; user stories with AC; **backend code-complete** (ADM-01, ADM-02) | integration tests for refresh-token rotation; rate limiting at the reverse proxy; a forgotten-password flow over e-mail | migrations, secret management for `Authentication:Jwt:SigningKey` |
| Audit & outbox (crosscutting) | `audit_logs` and `outbox_messages` are a **mandatory mechanism** of every command, written in the business transaction; both are in the canonical schema | the runtime persistence and outbox dispatcher | migrations, an e-mail/calendar provider |
| Deployment / Operations | The target topology in the architecture document; the two probes `/health/live` and `/health/ready` in the contract (tag `Operations`, part of the deployment view rather than a business function) | container image, Docker Compose, CI/CD, monitoring, backup runtime | an infrastructure ADR |

**Account and role administration are in scope** (the `[ADM]` module, [ADR-011](adr/011-in-house-identity.md)): QLNS issues and revokes its own sessions and uses no external Identity Provider. There is still no API for audit logs, delivery or integration: integration, notification and approval configuration lives in `appsettings` (no UI, no API). The requirement to check permission and data scope server-side on every request is unchanged — quality goal Q1 stands.

The HTML files under `uiux/` use illustrative data and simulated interactions. They are not a frontend application and must never be cited as evidence that a business rule works. Some prototypes (the Workforce Dashboard, for example) depict **out-of-scope** functionality and are kept purely as design reference.

---

## 3. Recommended Reading Paths

### Product owner / HR reviewer

1. [Functional Specifications](functional_specifications.md)
2. [Business context and stakeholders](architecture.md#1-introduction-and-goals)
3. [Quality requirements](architecture.md#10-quality-requirements-stimulus--response--measure)
4. [UI/UX prototypes](../uiux/README.md)

### Architect / technical reviewer

1. [Architecture — arc42 + C4](architecture.md)
2. [Constraints](architecture.md#2-constraints)
3. [C4 building blocks](architecture.md#5-building-block-view)
4. [Class diagrams](class_diagrams.md)
5. [Architecture decisions](adr/README.md)
6. [Risks and technical debt](architecture.md#11-risks-and-technical-debt)

### Developer starting implementation

1. [Current project scope](#2-current-project-scope)
2. [Target code structure](architecture.md#56-target-code-structure)
3. [Runtime view](architecture.md#6-runtime-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Architecture fitness functions](architecture.md#12-architecture-fitness-functions)
6. [Database design](../database/database_design.md)
7. [Class diagrams](class_diagrams.md)

### Security / Operations reviewer

1. [Quality goals](architecture.md#12-quality-goals-measurable--arc42-12)
2. [External interfaces](architecture.md#32-external-interfaces)
3. [Deployment view](architecture.md#7-deployment-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Risk register](architecture.md#11-risks-and-technical-debt)

---

## 4. Functional Coverage

The specification covers the two in-scope pillars, which amount to **34 bold leaf functions** on [`topdown-approach.png`](../topdown-approach.png):

1. **Core HR — 16 leaf functions** — employee profiles (personal and contact information, employment information, employee documents, profile updates); organization management (departments and the hierarchy, positions and job levels, assignments and reporting lines); contract management (drafting and approval, signing and effective dates, addenda and renewal, expiry and termination, contract documents); employee lifecycle (preboarding and the onboarding checklist, probation review and confirmation, promotion and internal transfer, offboarding and handover).
2. **Recruitment ATS — 18 leaf functions** — job requisition (create/edit, approval, status tracking); job posting (publish/update/close); candidate management (candidate profiles and résumé ingestion, application management, screening and matching, the recruitment pipeline, rejection/withdrawal); interview management (rounds and panel assignment, scheduling and invitations, scorecards and feedback, the hiring decision); offer management (drafting and compensation approval, offer sending, acceptance/signing/decline); onboarding handoff (transferring the data of an accepted candidate, linking the application to the employee record).

### Out of scope

**Four non-bold leaf functions sitting inside the two selected pillars:**

| Function | Parent | Consequence |
|---|---|---|
| **Headcount & Budget Validation** | Recruitment · Job Requisition | A requisition still has its approval flow, but there is **no** step where the system checks headcount and salary budget by itself; the HR Manager decides manually. `target_headcount`, `salary_min` and `salary_max` are declared data, not a control mechanism. |
| **Recruitment Channel Management** | Recruitment · Job Posting | Publish/update/close stays in scope; selecting and managing multiple posting channels does not. There is a single default careers channel and no sourcing-channel comparison. |
| **Organizational Chart** | Core HR · Organization Management | The **org-tree screen and endpoint** are dropped. *Departments & Organizational Hierarchy* stays **in** scope, so `departments.parent_department_id`, parent-child relationships, the cycle-prevention rule and the delete constraints all remain. Only the tree rendering is out. |
| **Suspension & Return to Work** | Core HR · Employee Lifecycle | Suspending an employee and returning them to work is not delivered. `employees.status = 'suspended'` and `employee_events.event_type IN ('suspension','return_to_work')` stay in the canonical schema as **reserved values that no endpoint can set** in this delivery. Offboarding therefore requires an employee who is `active` or `probation`. |

**The remaining pillars of the map:** Attendance & Leave Management (see the note below), Reports & Analytics, System Administration, Performance Management, Compensation & Benefits.

> [!NOTE]
> **Attendance & Leave** has been designed in full (SRS, 13 stories, DDL for 13 tables, 24 endpoints, 3 sequences, a list of HR/Legal decisions) but is **not part of the delivery scope**. All of it is kept intact under [deferred/attendance_leave/](deferred/attendance_leave/README.md) so it can be reattached when scope grows. Do not reference it from any authoritative document.

Performance Management and Compensation & Benefits (including payroll) are not part of the main design baseline.

---

## 5. Documentation Rules

- Distinguish `Design artifact`, `Proposed`, `Implemented` and `Accepted` clearly.
- Use `Implemented` only when the corresponding source code, migration and tests actually exist.
- Use `Accepted` for an ADR only when it has an owner and an explicit approval decision.
- Never describe prototype data as real data, or a UI prototype as a finished frontend.
- A feature change must update the SRS, the architecture, the API/schema and the affected diagrams together.
- A business rule needs a business owner and acceptance criteria; the engineering team does not infer labour or legal regulations on its own.
- The Mermaid source in `architecture.md` is the primary place to edit C4 diagrams; images and other diagrams are supplementary.
- Documentation is written in English. Vietnamese labour-law and HR terms may be kept in parentheses where the English term would lose a legal nuance.
- Internal links, Mermaid diagrams and tables of contents must be checked before merge.

---

## 6. Implementation Entry Criteria

Before frontend or backend work starts, at a minimum:

- Approve the frontend/backend stack and its versions with an ADR.
- Decide where the `Authentication:Jwt:SigningKey` secret is managed and settle the session lifetime policy (access token 30 minutes, refresh token 14 days). The authentication flow is settled in [ADR-011](adr/011-in-house-identity.md); the permission and data-scope matrix lives in `database/seed_roles.sql`.
- Review the canonical **28-table (v1.2)** schema and generate the first versioned EF Core migration.
- Settle the API conventions, the error model, pagination and the concurrency strategy. ✅ already covered in `docs/api/README.md`.
- Pick the first vertical slice together with its end-to-end acceptance test.
- Set up the architecture, security, migration and contract gates in CI.
- Create the integration test project running against a real PostgreSQL — mandatory, because the important Core HR invariants (partial unique indexes, conditional updates) cannot be verified with mocks.

The full set of open decisions is tracked in the [ADR index](adr/README.md); each decision has its own file under [`docs/adr/`](adr/README.md).

---

## 7. Related Resources

- [Project README](../README.md)
- [UI/UX prototypes](../uiux/README.md)
- [Database artifacts](../database/README.md)
- [Primary architecture](architecture.md)
- [Functional specifications](functional_specifications.md)
