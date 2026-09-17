<div align="center">

# QLNS

**Human Resource Management System — Core HR and Smart ATS Recruitment**

*Specify the business. Freeze the contract. Then write the code.*

[![Status](https://img.shields.io/badge/status-code--complete%20%C2%B7%20integration%20pending-yellow)](docs/README.md#2-current-project-scope)
[![Spec](https://img.shields.io/badge/spec-arc42%20%2B%20C4%20%2B%20ADR-informational)](docs/architecture.md)
[![API](https://img.shields.io/badge/OpenAPI-3.0.3%20%C2%B7%2066%20ops%20%2F%2051%20paths-blue)](api/openapi.yaml)
[![Schema](https://img.shields.io/badge/schema-24%20canonical%20tables%20%C2%B7%20v1.1-blue)](database/schema.sql)
[![Stories](https://img.shields.io/badge/INVEST-24%20user%20stories-informational)](docs/user_stories.md)
[![Code complete](https://img.shields.io/badge/code--complete-66%20of%2066%20operations-brightgreen)](api/API_REFERENCE.md#6-trạng-thái-triển-khai)
[![Unit tests](https://img.shields.io/badge/unit%20tests-1248%20passing-brightgreen)](src/backend/README.md#local-testing)

</div>

```mermaid
flowchart LR
    S["Specification<br/><small>SRS · 24 stories · use cases</small>"]
    A["Architecture<br/><small>arc42 + C4 + 9 ADR</small>"]
    D["Data contract<br/><small>23 canonical tables</small>"]
    C["API contract<br/><small>66 operations</small>"]
    K["Backend code<br/><small>66 operations · 3 layers · 1248 unit tests</small>"]
    R["Runtime<br/><small>integration tests · migration · IdP · worker · deployment</small>"]

    S --> A --> D --> C --> K --> R

    classDef done fill:#dbeafe,stroke:#1d4ed8,color:#1e3a8a
    classDef partial fill:#fef3c7,stroke:#b45309,color:#78350f
    classDef todo fill:#f1f5f9,stroke:#94a3b8,color:#475569,stroke-dasharray: 4 4
    class S,A,D,C,K done
    class R todo
```

**Blue is written and reviewable. Grey does not exist yet.** Every operation is `code-complete` (controller, policy, workflow, transactional persistence with audit/outbox, unit tests); integration tests against PostgreSQL and the real Identity Provider are the remaining gate to `implemented`.

<div align="center">

### [See the prototypes →](#6-visual-showcase)

</div>

[![The QLNS workforce dashboard prototype. Four KPI tiles read 312 employees, 268 present today, 18 requests awaiting approval and 12 open vacancies, above a twelve-month headcount-movement chart, a recent-leave list, an employment-status breakdown and a table of new joiners.](uiux/dashboard/dashboard.png)](#61-workforce-dashboard)

---

## Table of contents

- [1. What QLNS is](#1-what-qlns-is)
- [2. Delivery scope — seven pillars, two selected](#2-delivery-scope--seven-pillars-two-selected)
- [3. Roles and use cases](#3-roles-and-use-cases)
- [4. Domain model — the classes behind those use cases](#4-domain-model--the-classes-behind-those-use-cases)
- [5. Architecture, from simple to detailed](#5-architecture-from-simple-to-detailed)
- [6. Visual showcase](#6-visual-showcase)
- [7. Documentation](#7-documentation)

---

## 1. What QLNS is

QLNS is a **documentation-first design baseline** for a Vietnamese HR management
system, built contract-first so that the schema, the API and the acceptance
criteria are agreed before any feature is coded.

> The requirement is the contract. The schema is the contract. The OpenAPI
> document is the contract. Code that contradicts them is a bug in the code —
> or an ADR nobody has written yet.

Once delivered, the system targets these outcomes:

- **Automated recurring tasks** — fewer manual errors in employee records, employment-contract expiry monitoring and onboarding checklists.
- **Optimised recruitment (ATS)** — a shorter time-to-hire with a Kanban pipeline, interview scheduling and standardised scorecards.
- **Seamless onboarding handoff** — an accepted offer becomes an employee record, an initial contract and a checklist, without re-entering data and without duplicating on retry.
- **Traceable decisions** — every requisition approval, stage move, offer and employee event is written with its actor, reason and timestamp in the same transaction as the change itself.
- **Legal compliance** — employment-contract, social-insurance and personal-income-tax processes aligned with Vietnamese labour law, with HR/Legal as the rule owner.

Full specification: [Functional Specifications (SRS)](docs/functional_specifications.md) ·
[INVEST user stories](docs/user_stories.md) ·
[Architecture (arc42 + C4)](docs/architecture.md)

---

## 2. Delivery scope — seven pillars, two selected

The system decomposes top-down into **seven functional pillars**. Two of them —
**Recruitment** and **Core HR** — are selected for this delivery.

![Top-down functional decomposition diagram](topdown-approach.png)

**How to read the map: a leaf function printed in bold is in scope. Everything
set in normal weight is out of scope**, including the four non-bold leaves that
sit inside the two selected pillars. This map and this section are the single
source of truth for scope; every other document follows them.

In the mind map, Contract Management sits **under Core HR** alongside employee
profiles, organization management and the employee lifecycle — so contracts are
part of the Core HR scope, not a pillar of its own.

### 2.1. In scope — Recruitment, 18 leaf functions

| Group | Leaf functions delivered |
|---|---|
| **Job Requisition** | Create / Edit Requisition · Requisition Approval · Requisition Status Tracking |
| **Job Posting** | Publish / Update / Close Posting |
| **Candidate Management** | Candidate Profiles & Resume Ingestion · Application Management · Screening & Matching · Recruitment Pipeline · Rejection / Withdrawal |
| **Interview Management** | Interview Rounds & Panel Assignment · Scheduling & Invitations · Scorecards & Feedback · Hiring Decision |
| **Offer Management** | Offer Drafting & Compensation Approval · Offer Sending · Acceptance / Signing / Decline |
| **Onboarding Handoff** | Transfer Accepted Candidate Data · Link Application to Employee Record |

### 2.2. In scope — Core HR, 16 leaf functions

| Group | Leaf functions delivered |
|---|---|
| **Employee Profiles** | Personal & Contact Information · Employment Information · Employee Documents · Profile Updates |
| **Organization Management** | Departments & Organizational Hierarchy · Positions & Job Levels · Employee Assignments & Reporting Lines |
| **Contract Management** | Contract Drafting & Approval · Signing & Effective Dates · Amendments & Renewal · Expiration & Termination · Contract Documents |
| **Employee Lifecycle** | Preboarding & Onboarding Checklist · Probation Review & Confirmation · Promotion & Internal Transfer · Offboarding & Handover |

### 2.3. Out of scope — four leaf functions inside the two selected pillars

These four are the easiest to assume are included, because their parent group is
delivered. They are not.

| Leaf function | Parent | What that means concretely |
|---|---|---|
| **Headcount & Budget Validation** | Recruitment · Job Requisition | A requisition still has its approval flow, but the system performs **no** automated headcount or salary-budget check. The HR Manager decides manually. `target_headcount`, `salary_min` and `salary_max` remain declared data, not a control mechanism. |
| **Recruitment Channel Management** | Recruitment · Job Posting | Publish / Update / Close stays in scope. Selecting and managing multiple posting channels does not: there is a single default careers channel, no channel array on a requisition action, and no sourcing-channel comparison. |
| **Organizational Chart** | Core HR · Organization Management | The tree *screen and endpoint* are dropped. **Departments & Organizational Hierarchy stays in scope**, so `departments.parent_department_id`, parent–child relationships, the cycle-prevention rule and the delete restrictions all remain. Only the tree rendering is out. |
| **Suspension & Return to Work** | Core HR · Employee Lifecycle | Suspending an employee and returning them to work is not delivered. `employees.status = 'suspended'` and `employee_events.event_type IN ('suspension', 'return_to_work')` stay in the canonical schema as **reserved values that no endpoint can set**. Offboarding therefore requires an employee who is `active` or `probation`. |

### 2.4. Out of scope — the five remaining pillars

| Pillar | Note |
|---|---|
| **Attendance & Leave Management** | Fully designed, then parked — kept intact at [docs/deferred/attendance_leave/](docs/deferred/attendance_leave/README.md). |
| **Reports & Analytics** | Workforce Dashboard, Recruitment Analytics, Attendance & Leave Reports, Payroll & Personnel Cost Reports, Performance & Training Reports, Authorized Report Export. No reporting or export endpoint is part of the contract. |
| **System Administration** | Account Management, Roles/Permissions & Data Access Scope, Approval Workflow & Delegation Configuration, Notifications & Reminder Configuration, Integration Configuration, Audit Trail. |
| **Performance Management** | Not part of the design baseline. |
| **Compensation & Benefits** | Not part of the design baseline. |

### 2.5. Crosscutting mechanisms stay — their administration screens do not

This is the distinction that is easiest to get wrong, so it is stated once,
explicitly:

| Concern | Status in this delivery |
|---|---|
| Writing an audit record in the same transaction as the business change | **Still mandatory.** It is a crosscutting concern of every command, and the `audit_logs` table stays canonical. What is out of scope is the *administration endpoint for browsing audit records*. |
| Transactional outbox for email and calendar delivery | **Still mandatory**, and `outbox_messages` stays canonical. What is out of scope is the *administration endpoint for inspecting and retrying deliveries*. |
| `users` and `user_roles` tables | **Still canonical** — they carry the identity reference and the data scope that authorization reads. |
| Creating accounts, roles and role grants | Out of scope as an API. Account and role provisioning is owned by the **external Identity Provider**; QLNS consumes the result, it does not administer it. |
| Server-side permission and data-scope check on every request | **Still mandatory** — quality goal Q1 is unchanged. |
| Integration, notification and approval-workflow configuration | Out of scope. Configuration lives in `appsettings`, with no UI and no API. |

Infrastructure probes (`GET /health/live`, `GET /health/ready`) are retained under
the `Operations` tag. They belong to the deployment view, not to the functional
map, and are not a business function in scope.

---

## 3. Roles and use cases

Detail: [Use Cases](docs/use_cases.md#1-use-case-tổng-quát) ·
[Role-to-story permission matrix](docs/user_stories.md#51-bảng-phân-quyền-role-to-story)

| Actor | Owns |
|---|---|
| **Super Admin** | **no function in the current delivery** — accounts and role grants come from the external Identity Provider, and the System Administration pillar is out of scope ([section 2.5](#25-crosscutting-mechanisms-stay--their-administration-screens-do-not)) |
| **HR Director / Manager** | hiring and offer approval, employee movements, contracts |
| **Recruiter** | vacancies, CV screening, the ATS pipeline, interview scheduling, offer preparation |
| **Hiring Manager / Interviewer** | hiring requests, interviews, candidate scorecards |
| **HR Officer (C&B / Records)** | employee records, employment contracts, onboarding checklists |
| **Employee / Candidate** | permitted profile fields, own contract and org information, applications |

```mermaid
flowchart LR
    HRMgr([HR Director / Manager])
    Recruiter([Recruiter])
    Interviewer([Hiring Manager / Interviewer])
    HROfficer([HR Officer])
    User([Employee / Candidate])

    subgraph HRMS["QLNS / HRMS"]
        Jobs[Create & publish job requisitions]
        ATS[Screen CVs & manage ATS pipeline]
        Interviews[Schedule interviews & submit scorecards]
        Offers[Approve offers & onboarding]
        Records[Manage employee records & contracts]
    end

    Recruiter --> Jobs
    Recruiter --> ATS
    Recruiter --> Interviews
    Interviewer --> Jobs
    Interviewer --> Interviews
    HRMgr --> Offers
    HRMgr --> Records
    HROfficer --> Records
    User --> ATS
    User --> Records
```

**Super Admin is deliberately absent from this diagram**: it owns no use case in
the current delivery. The role stays in the actor table because it exists in the
role model, but every function it used to own — account and role administration,
audit-trail browsing, configuration, organization-wide reporting — is out of
scope, and account provisioning belongs to the external Identity Provider.

Authorization is **permission plus data scope**, enforced server-side on every
request — `self`, `department` or `organization`. Hiding a button in the UI is not
authorization. See [Q1 quality goal](docs/architecture.md#12-quality-goals-measurable--arc42-12)
and [risk R5](docs/architecture.md#11-risks-and-technical-debt).

---

## 4. Domain model — the classes behind those use cases

One condensed view of the objects the use cases above act on, and of the single
seam between the two pillars. The authoritative version, with every attribute,
method and invariant, is [docs/class_diagrams.md](docs/class_diagrams.md); the
canonical field types live in [database/schema.sql](database/schema.sql).

```mermaid
classDiagram
    direction LR

    class JobPosting {
        +long id
        +string jobCode
        +JobPostingStatus status
        +approve(long actorUserId) void
        +publish(DateTimeOffset) void
    }
    class Candidate {
        +long id
        +string firstName
        +string lastName
        +string email
    }
    class Application {
        +long id
        +ApplicationStage stage
        +long version
        +advanceTo(ApplicationStage) ApplicationStage
    }
    class Interview {
        +long id
        +InterviewStatus status
        +DateTimeOffset startsAt
    }
    class Evaluation {
        +long id
        +Recommendation recommendation
        +decimal overallScore
    }
    class Offer {
        +long id
        +OfferStatus status
        +accept(DateTimeOffset) void
    }

    class Employee {
        +long id
        +string employeeCode
        +long sourceApplicationId
        +EmployeeStatus status
        +applyApprovedEvent(EmployeeEvent) void
    }
    class Department {
        +long id
        +long parentDepartmentId
        +isRootUnit() bool
    }
    class Position {
        +long id
        +string level
    }
    class EmployeeEvent {
        +long id
        +EmployeeEventType eventType
        +EmployeeEventStatus status
        +DateOnly effectiveDate
        +applyTo(Employee, DateOnly) void
    }
    class Contract {
        +long id
        +ContractType contractType
        +ContractStatus status
        +DateOnly endDate
    }
    class OnboardingTask {
        +long id
        +TaskStatus status
    }
    class EmployeeDocument {
        +long id
        +string documentType
        +int version
    }
    class ProbationReview {
        +long id
        +ProbationStatus status
        +decide(ProbationOutcome, DateOnly, long actorUserId) EmployeeEvent
    }
    class OffboardingCase {
        +long id
        +OffboardingStatus status
        +DateOnly lastWorkingDate
        +approve(long actorUserId) EmployeeEvent
    }

    JobPosting "1" o-- "0..*" Application : vacancy
    Candidate "1" o-- "0..*" Application : applies through
    Application "1" *-- "0..*" Interview
    Interview "1" *-- "0..*" Evaluation
    Application "1" *-- "0..1" Offer : at most one open

    Offer ..> Employee : accepted offer creates
    Application "0..1" <.. "0..1" Employee : sourceApplicationId (unique)

    Department "0..1" --> "0..*" Department : parent of
    Department "1" o-- "0..*" Employee : employs
    Position "1" o-- "0..*" Employee : classifies
    Employee "0..1" --> "0..*" Employee : manages
    Employee "1" *-- "0..*" EmployeeEvent
    Employee "1" *-- "0..*" Contract
    Employee "1" *-- "0..*" OnboardingTask
    Employee "1" *-- "0..*" EmployeeDocument
    Employee "1" *-- "0..*" ProbationReview
    Employee "1" *-- "0..*" OffboardingCase
    ProbationReview ..> EmployeeEvent : produces
    OffboardingCase ..> EmployeeEvent : produces
```

---

## 5. Architecture, from simple to detailed

Four views of the same system. Each adds one layer of detail; the authoritative
version of all of them is [docs/architecture.md](docs/architecture.md).

### View 1 — the three tiers

```mermaid
flowchart TD
    A["React 19 + Vite web client<br/><small>presentation tier</small>"]
    B["ASP.NET Core API · .NET 10<br/><small>application tier</small>"]
    C["PostgreSQL<br/><small>system of record · 24 tables</small>"]
    D["Providers<br/><small>IdP · email · calendar · object storage</small>"]

    A -->|HTTPS · /api/v1 · JWT| B
    B -->|EF Core| C
    B -.->|transactional outbox| D

    classDef tier fill:#dbeafe,stroke:#1d4ed8,color:#1e3a8a
    class B tier
```

The frontend never reaches the database ([constraint C2](docs/architecture.md#2-constraints)).
Every query and command goes through the API, which owns authorization and the
business rules. Tiers are deployment boundaries; the three layers below are
source-code boundaries inside the application tier.

### View 2 — one command, end to end

Advancing a candidate one stage, as specified in
[sequence 3](docs/sequence_diagrams.md#3-chuyển-application-sang-giai-đoạn-tiếp-theo)
and implemented in
[RecruitmentPipelineService.cs](src/backend/src/Qlns.BusinessLogic/Modules/Recruitment/Applications/RecruitmentPipelineService.cs).

```mermaid
sequenceDiagram
    autonumber
    actor R as Recruiter
    participant UI as React pipeline page
    participant C as Controller<br/>(Qlns.Api)
    participant S as Service<br/>(Qlns.BusinessLogic)
    participant Repo as Repository<br/>(Qlns.DataAccess)
    participant DB as PostgreSQL

    R->>UI: Advance stage
    UI->>C: POST /applications/{id}/advance · JWT · If-Match "4"
    C->>C: check permission, build data scope
    C->>S: AdvanceAsync(command)
    S->>Repo: load application within scope
    Repo->>DB: SELECT application JOIN job posting
    S->>S: AdvanceTo(target, eligibility)
    S->>Repo: SaveAdvanceAsync(expectedVersion = 4)
    Repo->>DB: BEGIN · conditional UPDATE WHERE version = 4
    Repo->>DB: INSERT application_stage_events
    Repo->>DB: INSERT audit_logs
    Repo->>DB: COMMIT
    C-->>UI: 200 · ETag "5"
```

The business change, its history row and its audit row commit in **one
transaction**. That is the rule for every command, not a detail of this one.

And its failure twin — the reason `If-Match` is mandatory:

```mermaid
sequenceDiagram
    autonumber
    participant C as Controller
    participant S as Service
    participant Repo as Repository

    C->>S: AdvanceAsync(If-Match "4")
    S->>Repo: load application
    Repo-->>S: version is already 5
    Note over S: another recruiter advanced it first
    S-->>C: ConcurrencyConflictException
    C-->>C: 409 · common.concurrency_conflict
    Note over C: nothing was written — the client reloads and retries
```

A stage jump, a backwards move or a missing interview/evaluation fails the same
way — a stable business error code in `application/problem+json`, never a silent
partial write. See [quality requirement QR2](docs/architecture.md#10-quality-requirements-stimulus--response--measure).

### View 3 — the backend, layered

```mermaid
flowchart TB
    subgraph L1["Qlns.Api — presentation layer"]
        direction LR
        CTRL[Controllers]
        AUTHZ[AuthN / AuthZ policies]
        PD[DTO + Problem Details]
    end

    subgraph L2["Qlns.BusinessLogic — business layer"]
        direction LR
        UC[Use-case services]
        WF[Workflow + state machines]
        PORT[Repository contracts]
    end

    subgraph L3["Qlns.DataAccess — data layer"]
        direction LR
        EF[EF Core mappings]
        TX[Transactions]
        AUD[Audit + outbox writes]
    end

    L1 --> L2
    L3 --> L2

    classDef core fill:#dbeafe,stroke:#1d4ed8,color:#1e3a8a
    class L2 core
```

Dependencies point **inward**: the business layer references neither EF Core nor
ASP.NET Core, so a use case can be unit-tested with `new`. Each layer groups code
by `Modules/<Module>/<Feature>`, and the module names match the OpenAPI tags, so
every tag has exactly one owner —
[target code structure](docs/architecture.md#56-target-code-structure).

### View 4 — the ATS pipeline as a state machine

```mermaid
stateDiagram-v2
    direction LR
    [*] --> SourcedApplied
    SourcedApplied --> AiScreening
    AiScreening --> TechInterview
    TechInterview --> ExecutiveRound
    ExecutiveRound --> OfferLetter
    OfferLetter --> HiredReady
    HiredReady --> [*]

    SourcedApplied --> Rejected
    AiScreening --> Rejected
    TechInterview --> Rejected
    ExecutiveRound --> Rejected
    OfferLetter --> Rejected
    Rejected --> [*]
    SourcedApplied --> Withdrawn
    Withdrawn --> [*]

    note right of TechInterview
        Six stages, two terminal states.
        No stage jumps, no backwards moves,
        and no direct write to `status` —
        every transition is an explicit command.
    end note
```

The enumeration is [ApplicationStage.cs](src/backend/src/Qlns.BusinessLogic/Modules/Recruitment/Applications/ApplicationStage.cs);
the contract names are `sourced_applied` … `withdrawn`, and the server owns the
next stage no matter what the client sends.

---

## 6. Visual showcase

These are **UI/UX prototypes** with illustrative data — standalone HTML on one
shared design system ([hrm-theme.css](uiux/hrm-theme.css)). They are not a
frontend application, they call no backend, and they are not evidence that any
business rule works. Full index: [uiux/README.md](uiux/README.md).

### 6.1. Workforce dashboard

[`uiux/main.html?view=dashboard`](uiux/main.html) — headcount KPIs, movement,
recent leave requests, employment-status breakdown and new joiners.

> [!NOTE]
> The Workforce Dashboard is a prototype of the **Reports & Analytics** pillar,
> which is **out of scope for this delivery** ([section 2.4](#24-out-of-scope--the-five-remaining-pillars)).
> The screen is kept as a design reference only: it draws on illustrative data,
> its leave panel belongs to the parked Attendance & Leave module, and no
> reporting or export endpoint exists in the API contract.

![Workforce dashboard](uiux/dashboard/dashboard.png)


### 6.2. Smart recruitment and onboarding (ATS)


**Pipeline — Kanban view.** The six stages of [view 4](#view-4--the-ats-pipeline-as-a-state-machine), with fixed-size cards and the primary action aligned across every column.

![ATS candidate pipeline Kanban](uiux/recruitment/candidate_kanban.png)

## 7. Documentation

Read in this order:

| # | Document | What it answers |
|---|---|---|
| 01 | [Docs index](docs/README.md) | The map, the authority order, the reading path for your role |
| 02 | [Functional Specifications (SRS)](docs/functional_specifications.md) | What the system must do, per module |
| 03 | [INVEST user stories](docs/user_stories.md) | **24 stories with Gherkin acceptance criteria** and traceability |
| 04 | [Use cases](docs/use_cases.md) | Actors, boundaries and the actor-to-function matrix |
| 05 | [Architecture (arc42 + C4)](docs/architecture.md) | **The main design document** — context, containers, components, runtime, deployment |
| 06 | [Sequence diagrams](docs/sequence_diagrams.md) | Six flows across the three layers, success *and* failure branches |
| 07 | [Class diagrams](docs/class_diagrams.md) | The domain model per module, the design model of the one slice that has code, and the 3-layer pattern for the rest |
| 08 | [Database design](database/database_design.md) · [schema](database/schema.sql) | The ERD, the field specification, the canonical DDL |
| 09 | [API contract](api/README.md) · [OpenAPI](api/openapi.yaml) · [reference](api/API_REFERENCE.md) | Conventions, 66 operations across 51 paths, per-operation status |
| 10 | [UI/UX prototypes](uiux/README.md) | Every prototype screen and the interactions it simulates |