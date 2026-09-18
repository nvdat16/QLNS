# 📑 HRMS Database Design

> **Status:** a design artifact describing the **canonical schema — 28 tables (v1.2)**, covering the two business modules (Core HR including Contracts, and Recruitment) plus the Identity & Access module. The Mermaid ERD in section 1 covers all 28 v1.2 tables, including `interview_panelists` (delta v1.1) and the four identity tables of delta v1.2 — see [README §2.6](README.md#26-delta-v11--surfaced-during-implementation) and [§2.7](README.md#27-delta-v12--moving-authentication-in-house). The backend is code-complete across all of these tables, but the EF Core migration pipeline does not exist and the database runtime has not been verified by an integration test.

> [!IMPORTANT]
> [`schema.sql`](schema.sql) is the **canonical schema contract** and the single source of truth for data types, constraints, indexes and exclusion constraints. This document describes the business purpose and the invariants of each table. Where the two disagree, `schema.sql` wins and this document must be corrected.

> [!NOTE]
> Attendance & Leave is **not part of the delivery scope**. Its 13-table DDL is kept at [docs/deferred/attendance_leave/](../docs/deferred/attendance_leave/README.md) for later reuse and is not part of the canonical schema.

> [!NOTE]
> **Delivery scope** follows the bold leaf functions under Recruitment and Core HR on the [`topdown-approach.png`](../topdown-approach.png) map; the authoritative source is [section 2 of the root README](../README.md#2-delivery-scope--seven-pillars-two-selected). Out of scope for this delivery: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart and Suspension & Return to Work. Note that *Departments & Organizational Hierarchy* stays **in** scope, so `departments.parent_department_id`, the parent-child relationship and the cycle-prevention rule are all kept — only the tree screen and endpoint are dropped.

## Table Map by Module

| Module | Tables | Count |
| :--- | :--- | :---: |
| **Core HR — Organization & Profile** | `departments`, `positions`, `employees` | 3 |
| **Core HR — Lifecycle** | `onboarding_tasks`, `employee_events`, `employee_documents`, `probation_reviews`, `offboarding_cases`, `offboarding_tasks` | 6 |
| **Core HR — Contracts** | `contracts`, `contract_addenda` | 2 |
| **Recruitment (ATS)** | `job_postings`, `candidates`, `resumes`, `applications`, `application_stage_events`, `interviews`, `evaluations`, `offers` | 8 |
| **Recruitment — delta v1.1** | `interview_panelists` | 1 |
| **Identity & Access (ADM)** | `users`, `user_credentials`, `user_roles`, `roles`, `role_permissions`, `refresh_tokens` | 6 |
| **Platform (audit, outbox)** | `audit_logs`, `outbox_messages` | 2 |
| **Total** | | **28** |

The six Identity & Access tables are an **aggregate owned by this system**: `users` plus `user_credentials` carry the identity and the password, `user_roles` plus `roles` plus `role_permissions` carry authorization and data scope, and `refresh_tokens` carries the revocable long-lived session. They **do have an administration API**, at `/api/v1/auth/*` and `/api/v1/admin/*` ([ADR-011](../docs/adr/011-in-house-identity.md)).

The two remaining platform tables are a **mandatory crosscutting mechanism**, written in the same transaction as the business change, and have **no** API: no endpoint browses the audit log, and none inspects or retries a delivery.

## Canonical Schema Conventions

- Primary keys: `bigint GENERATED ALWAYS AS IDENTITY`, except `outbox_messages` (`uuid`).
- Time: `timestamptz`, stored in UTC. A field that means a calendar date uses `date`.
- Every mutable aggregate has a `version bigint` column as the source of its `ETag` / `If-Match`.
- Money: `numeric(15,2)`.
- Status value sets are enforced by `CHECK`, not only by application-layer validation.
- The "only one open record" rule is enforced by a partial `UNIQUE INDEX` (for example `ux_offboarding_open_case`, `ux_offers_one_open_per_application`).

---

## 1. Overall Entity–Relationship Diagram (Mermaid ERD)

The diagram below covers **all 28 canonical v1.2 tables** and is derived directly from `schema.sql`. To stay readable, each entity lists only
its primary key, foreign keys, unique keys and the columns that carry meaning (status, lifecycle timestamps, and the `version` used as an ETag);
the full per-field specification is in [§2](#2-employee-records-module--field-level-view)–[§5](#5-identity--access--platform-tables).
The `created_at` / `updated_at` columns exist on most tables and are omitted from the diagram.

```mermaid
erDiagram
    %% ---------- Identity & Access (ADM) ----------
    roles ||--o{ role_permissions : "grants"
    roles ||--o{ user_roles : "assigned through"
    users ||--o| user_credentials : "signs in with"
    users ||--o{ user_roles : "holds"
    users ||--o{ refresh_tokens : "owns session"
    refresh_tokens |o--o| refresh_tokens : "replaced by"
    users ||--o{ audit_logs : "acts in"

    %% ---------- Organization & Profile ----------
    departments |o--o{ departments : "parent of"
    departments ||--o{ employees : "employs"
    positions ||--o{ employees : "classifies"
    employees |o--o{ employees : "manages"
    users |o--o| employees : "is"

    %% ---------- Recruitment (ATS) ----------
    departments ||--o{ job_postings : "owns"
    positions |o--o{ job_postings : "classifies"
    job_postings ||--o{ resumes : "receives intake"
    candidates |o--o{ resumes : "owns after confirmation"
    candidates ||--o{ applications : "submits"
    job_postings ||--o{ applications : "receives"
    resumes |o--o{ applications : "supports"
    applications ||--o{ application_stage_events : "logs"
    applications ||--o{ interviews : "includes"
    interviews ||--o{ interview_panelists : "panel of"
    users ||--o{ interview_panelists : "sits on"
    interviews ||--o{ evaluations : "scored by"
    applications ||--o{ offers : "receives"
    applications |o--o| employees : "hired as"

    %% ---------- Contracts ----------
    employees ||--o{ contracts : "signs"
    contracts ||--o{ contract_addenda : "amended by"

    %% ---------- Employee Lifecycle ----------
    employees ||--o{ onboarding_tasks : "onboards through"
    employees ||--o{ employee_documents : "files"
    employees ||--o{ employee_events : "changes through"
    employee_events |o--o| employee_events : "compensated by"
    employees ||--o{ probation_reviews : "reviewed in"
    contracts ||--o{ probation_reviews : "probation of"
    probation_reviews |o--o| employee_events : "results in"
    employees ||--o{ offboarding_cases : "exits via"
    offboarding_cases ||--o{ offboarding_tasks : "handed over by"
    offboarding_cases |o--o| employee_events : "results in"

    %% ================= entities =================
    roles {
        varchar code PK
        varchar name
        boolean is_assignable
    }

    role_permissions {
        varchar role_code PK_FK
        varchar permission PK
    }

    users {
        bigint id PK
        varchar external_subject UK "local| prefix for in-house accounts"
        varchar email UK
        varchar display_name
        varchar status "active | disabled"
        bigint version
    }

    user_credentials {
        bigint user_id PK_FK
        varchar password_hash "pbkdf2-sha512$iter$salt$hash"
        boolean must_change_password
        integer failed_attempts
        timestamptz locked_until
        timestamptz last_login_at
        bigint version
    }

    user_roles {
        bigint user_id PK_FK
        varchar role_code PK_FK
        varchar data_scope_type PK "self | department | organization"
        bigint data_scope_id PK
        bigint granted_by FK
    }

    refresh_tokens {
        bigint id PK
        bigint user_id FK
        varchar token_hash UK "SHA-256 digest only"
        timestamptz expires_at
        timestamptz revoked_at
        varchar revoked_reason "rotated | logout | reuse_detected | ..."
        bigint replaced_by_token_id FK
    }

    %% ---------------------------------------------
    departments {
        bigint id PK
        varchar code UK
        varchar name
        bigint parent_department_id FK
        varchar cost_center
        bigint version
    }

    positions {
        bigint id PK
        varchar code UK
        varchar name
        varchar level
        bigint version
    }

    employees {
        bigint id PK
        varchar employee_code UK
        bigint source_application_id FK_UK "handoff idempotency key"
        bigint user_id FK_UK
        varchar work_email "null until activation"
        varchar personal_email
        bigint manager_id FK
        bigint department_id FK
        bigint position_id FK
        date hire_date
        varchar status "probation | active | suspended* | resigned | terminated"
        bigint version
    }

    %% ---------------------------------------------
    job_postings {
        bigint id PK
        varchar job_code UK "REQ-{yyyy}-{id:D5}"
        bigint department_id FK
        bigint position_id FK
        varchar employment_type
        numeric salary_min
        numeric salary_max
        integer target_headcount
        varchar status "draft | pending_approval | approved | active | closed"
        bigint created_by FK
        bigint version
    }

    candidates {
        bigint id PK
        varchar email
        varchar normalized_email "duplicate detection"
        varchar normalized_phone
        varchar privacy_notice_version
        timestamptz consented_at
        date retention_until
        bigint version
    }

    resumes {
        bigint id PK
        uuid intake_id UK
        bigint job_posting_id FK
        bigint candidate_id FK "set at confirmation"
        varchar object_key UK
        varchar intake_status "scanning | parsing | awaiting_confirmation | ..."
        varchar malware_scan_status
        varchar parser_status
        jsonb parsed_data
        jsonb parse_confidence
        bigint uploaded_by FK
        bigint confirmed_by FK
    }

    applications {
        bigint id PK
        bigint candidate_id FK
        bigint job_posting_id FK
        bigint resume_id FK
        varchar stage "sourced_applied ... hired_ready"
        numeric ai_score
        varchar source
        bigint version
    }

    application_stage_events {
        bigint id PK
        bigint application_id FK
        varchar from_stage
        varchar to_stage
        bigint changed_by FK
        bigint application_version "lost-update guard"
    }

    interviews {
        bigint id PK
        bigint application_id FK
        varchar interview_type
        timestamptz starts_at
        timestamptz ends_at
        varchar timezone
        bigint interviewer_user_id FK "panel lead"
        varchar status "scheduled | completed | cancelled"
        bigint version
    }

    interview_panelists {
        bigint interview_id PK_FK
        bigint user_id PK_FK
    }

    evaluations {
        bigint id PK
        bigint interview_id FK
        bigint evaluator_user_id FK
        numeric technical_score "0-5 step 0.5"
        numeric communication_score
        numeric problem_solving_score
        numeric teamwork_score
        numeric overall_score "server-computed"
        varchar recommendation
        bigint unlocked_by FK
        integer version "scorecard revision"
    }

    offers {
        bigint id PK
        bigint application_id FK
        numeric base_salary
        char currency
        date start_date
        date expiration_date
        varchar status "draft | pending_approval | approved | sent | accepted | ..."
        varchar document_object_key
        bigint approved_by FK
        bigint version
    }

    %% ---------------------------------------------
    contracts {
        bigint id PK
        bigint employee_id FK
        varchar contract_number UK
        varchar contract_type "probation | fixed_term | indefinite | ..."
        date start_date
        date end_date
        numeric salary
        char currency
        varchar status "draft | pending_approval | active | expired | terminated"
        boolean is_primary "one active primary per employee"
        bigint version
    }

    contract_addenda {
        bigint id PK
        bigint contract_id FK
        varchar addendum_number UK
        varchar status
        date effective_date
        jsonb before_terms
        jsonb after_terms
        bigint created_by FK
        bigint approved_by FK
        bigint version "ETag, not sibling ordering"
    }

    %% ---------------------------------------------
    onboarding_tasks {
        bigint id PK
        bigint employee_id FK
        varchar template_key
        bigint assigned_to_user_id FK
        timestamptz due_at
        varchar status "pending | in_progress | done | skipped"
        bigint version
    }

    employee_documents {
        bigint id PK
        bigint employee_id FK
        varchar document_type
        integer version "document revision"
        varchar object_key UK
        bigint uploaded_by FK
        date retention_until
        timestamptz deleted_at
    }

    employee_events {
        bigint id PK
        bigint employee_id FK
        varchar event_type "promotion | transfer | termination | suspension* | ..."
        varchar status "draft | pending_approval | approved | applied | rejected"
        date effective_date
        jsonb before_data
        jsonb after_data
        bigint compensates_event_id FK
        bigint created_by FK
        bigint approved_by FK
        timestamptz applied_at "set by the Effective-Date Worker"
        bigint version
    }

    probation_reviews {
        bigint id PK
        bigint employee_id FK
        bigint contract_id FK
        date review_due_date
        bigint reviewer_user_id FK
        varchar status "pending | in_review | decided"
        varchar outcome "confirmed | extended | terminated"
        bigint decided_by FK
        bigint employee_event_id FK "one-to-one, replay guard"
        bigint version
    }

    offboarding_cases {
        bigint id PK
        bigint employee_id FK
        bigint employee_event_id FK "one-to-one, replay guard"
        varchar separation_type
        date last_working_date
        bigint handover_to_employee_id FK
        varchar final_settlement_status "owned by Payroll, out of scope"
        varchar status "draft | approved | in_progress | completed"
        bigint created_by FK
        bigint approved_by FK
        bigint version
    }

    offboarding_tasks {
        bigint id PK
        bigint offboarding_case_id FK
        varchar template_key
        varchar category
        bigint assigned_to_user_id FK
        boolean blocks_last_working_day
        varchar status
        bigint version
    }

    %% ---------------------------------------------
    audit_logs {
        bigint id PK
        bigint actor_user_id FK "null for anonymous sign-in attempts"
        varchar action
        varchar entity_type
        varchar entity_id
        jsonb before_data
        jsonb after_data
        varchar result "succeeded | rejected | failed"
        varchar correlation_id
        timestamptz occurred_at
    }

    outbox_messages {
        uuid id PK
        varchar message_type
        varchar aggregate_type
        varchar aggregate_id
        jsonb payload
        timestamptz available_at
        timestamptz processed_at
        integer attempts
        text last_error
    }
```

> [!NOTE]
> `audit_logs` and `outbox_messages` have no foreign key to a business aggregate — they reference it as strings through
> `entity_type` + `entity_id` and `aggregate_type` + `aggregate_id`, so an audit record outlives the row it describes.
> The relationships drawn above are only the real foreign keys in `schema.sql`; `users ||--o{ audit_logs` in particular is
> the foreign key of `actor_user_id`, not of the affected object.
>
> `*` marks a value that is **reserved and out of scope in this delivery**: `employees.status = 'suspended'` and
> `employee_events.event_type IN ('suspension','return_to_work')` exist in the schema but no endpoint can set them.
---

## 2. Employee Records Module — field-level view

> [!NOTE]
> The column tables in §2 and §3 are a **historical conceptual description** (`INTEGER`/`TIMESTAMP`/`SERIAL`). Canonical v1 uses `bigint identity` and `timestamptz`, and adds `version`, `created_by`/`approved_by`, and an object-storage key in place of a public `file_url`. Always check [`schema.sql`](schema.sql) before generating a migration.

### 2.1. `departments` Table (Departments)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `name` | `VARCHAR(255)` | `NOT NULL` | Department name |
| `code` | `VARCHAR(50)` | `UNIQUE`, `NOT NULL` | Department identifier (e.g., IT, HR, FIN) |
| `description` | `TEXT` | `NULL` | Detailed description of the department's responsibilities |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 2.2. `positions` Table (Job Positions)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `name` | `VARCHAR(255)` | `NOT NULL` | Position name (e.g., Backend Developer, HR Specialist) |
| `code` | `VARCHAR(50)` | `UNIQUE`, `NOT NULL` | Position code (e.g., DEV_BE, HR_SPEC) |
| `level` | `VARCHAR(50)` | `NULL` | Seniority level (Junior, Mid, Senior, Lead, Director) |
| `description` | `TEXT` | `NULL` | Description of job responsibilities |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 2.3. `employees` Table (Employee Profiles)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `employee_code` | `VARCHAR(50)` | `UNIQUE`, `NOT NULL` | Employee code (e.g., EMP001) |
| `first_name` | `VARCHAR(100)` | `NOT NULL` | Given name |
| `last_name` | `VARCHAR(100)` | `NOT NULL` | Family and middle name(s) |
| `date_of_birth` | `DATE` | `NULL` | Date of birth |
| `gender` | `VARCHAR(20)` | `NULL` | Gender (MALE, FEMALE, OTHER) |
| `email` | `VARCHAR(255)` | `UNIQUE`, `NOT NULL` | Work email address |
| `phone` | `VARCHAR(20)` | `NULL` | Contact phone number |
| `address` | `TEXT` | `NULL` | Permanent or temporary address |
| `hire_date` | `DATE` | `NOT NULL` | Employment start date |
| `status` | `VARCHAR(30)` | `NOT NULL`, `DEFAULT 'probation'`, `CHECK ck_employee_status` | Employment status: `probation`, `active`, `terminated`. The value `suspended` is still inside the CHECK but is **reserved** — Suspension & Return to Work is out of scope and no endpoint can set it. |
| `department_id` | `INTEGER` | `FOREIGN KEY` -> `departments(id)`, `NOT NULL` | Assigned department |
| `position_id` | `INTEGER` | `FOREIGN KEY` -> `positions(id)`, `NOT NULL` | Assigned position |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |
| `updated_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Last updated timestamp |

> [!NOTE]
> The canonical value set of `employees.status` is `ck_employee_status` in [`schema.sql`](schema.sql): `probation`, `active`, `suspended`, `terminated` (the table above is a historical description — see [section 6, point 1](#6-preconditions-before-generating-a-migration)). Of these, **`suspended` is a reserved value**: the *Suspension & Return to Work* business function is **out of scope** for this delivery, so **no endpoint can set it**. The value is kept in the DDL so the status set stays stable when the scope grows. Offboarding therefore requires an employee who is `active` or `probation`.

### 2.4. `onboarding_tasks` Table
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `employee_id` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NOT NULL` | New employee associated with the task |
| `task_name` | `VARCHAR(255)` | `NOT NULL` | Task name (e.g., issue laptop, create email account) |
| `description` | `TEXT` | `NULL` | Detailed instructions |
| `assigned_to` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NULL` | Employee assigned to the task |
| `due_date` | `DATE` | `NULL` | Completion deadline |
| `status` | `VARCHAR(50)` | `DEFAULT 'PENDING'` | Task status (PENDING, IN_PROGRESS, COMPLETED) |
| `completed_at` | `TIMESTAMP` | `NULL` | Actual completion timestamp |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 2.5. `employee_documents` Table (Employee Records & Documents)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `employee_id` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NOT NULL` | Employee to whom the document belongs |
| `document_type` | `VARCHAR(100)` | `NOT NULL` | Document type (ID_CARD, DEGREE, CERTIFICATE, CV) |
| `file_name` | `VARCHAR(255)` | `NOT NULL` | Original file name |
| `file_url` | `VARCHAR(500)` | `NOT NULL` | Storage location (S3 / cloud) |
| `uploaded_by` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NULL` | Employee who uploaded the document |
| `uploaded_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Upload timestamp |

### 2.6. `contracts` Table (Employment Contracts)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `employee_id` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NOT NULL` | Employee who signed the contract |
| `contract_type` | `VARCHAR(50)` | `NOT NULL` | Contract type (PROBATION, 1_YEAR, 3_YEAR, INDEFINITE) |
| `start_date` | `DATE` | `NOT NULL` | Effective start date |
| `end_date` | `DATE` | `NULL` | Expiration date |
| `salary` | `DECIMAL(15, 2)`| `NOT NULL` | Agreed salary |
| `status` | `VARCHAR(50)` | `DEFAULT 'ACTIVE'` | Contract status (ACTIVE, EXPIRED, TERMINATED) |
| `document_url` | `VARCHAR(500)` | `NULL` | URL of the scanned contract |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 2.7. `employee_events` Table (Employee Change History)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `INTEGER` | `PRIMARY KEY`, Auto Increment | Primary key |
| `employee_id` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NOT NULL` | Employee associated with the event |
| `event_type` | `VARCHAR(50)` | `NOT NULL`, `CHECK` | One of `promotion`, `transfer`, `demotion`, `salary_adjustment`, `termination`, `correction` (client-writable via `EmployeeEventWrite`) or `probation_confirmation`, `probation_extension` (system-generated). `suspension` and `return_to_work` are **reserved values, out of scope for this delivery** — see the note below. |
| `effective_date` | `DATE` | `NULL` | Date on which the decision takes effect |
| `old_department_id` | `INTEGER` | `FOREIGN KEY` -> `departments(id)`, `NULL` | Previous department, if transferred |
| `new_department_id` | `INTEGER` | `FOREIGN KEY` -> `departments(id)`, `NULL` | New department |
| `old_position_id` | `INTEGER` | `FOREIGN KEY` -> `positions(id)`, `NULL` | Previous position, if appointed or changed |
| `new_position_id` | `INTEGER` | `FOREIGN KEY` -> `positions(id)`, `NULL` | New position |
| `description` | `TEXT` | `NULL` | Reason or decision details |
| `created_by` | `INTEGER` | `FOREIGN KEY` -> `employees(id)`, `NULL` | Employee who created the record |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Record creation timestamp |

> [!NOTE]
> `ck_employee_event_type` in [`schema.sql`](schema.sql) still permits `suspension` and `return_to_work`, but these are **reserved, out-of-scope values** for this delivery: *Suspension & Return to Work* is not implemented and **no endpoint can set** either event type. There is therefore no reconciliation rule requiring `suspension`/`return_to_work` to come in balanced pairs.

---

## 3. Recruitment Management Module

### 3.1. `job_postings` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `job_code` | `VARCHAR` | `UNIQUE`, `NULL` | Human-readable job requisition code |
| `title` | `VARCHAR` | `NOT NULL` | Job title |
| `department_id` | `INTEGER` | `FOREIGN KEY` -> `departments(id)`, `NOT NULL` | Department requesting the role |
| `description` | `TEXT` | `NULL` | Job description |
| `requirements` | `TEXT` | `NULL` | Required qualifications and skills |
| `location` | `VARCHAR` | `NULL` | Work location |
| `employment_type` | `VARCHAR` | `DEFAULT 'Full-time'` | Employment arrangement |
| `salary_min` | `DECIMAL(15, 2)` | `NULL` | Minimum salary range |
| `salary_max` | `DECIMAL(15, 2)` | `NULL` | Maximum salary range |
| `target_headcount` | `INTEGER` | `DEFAULT 1` | Number of positions to fill |
| `status` | `VARCHAR` | `DEFAULT 'Active Recruiting'` | Recruitment status |
| `published_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Publication timestamp |
| `closing_date` | `DATE` | `NULL` | Application deadline |
| `created_by` | `INTEGER` | `NULL` | User who created the job posting |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |
| `updated_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Last updated timestamp |

> [!NOTE]
> `target_headcount`, `salary_min` and `salary_max` are **declared data**, not a control mechanism: *Headcount & Budget Validation* is out of scope for this delivery, so the system does not check headcount or salary budget when a requisition is approved — the HR Manager decides manually. Likewise *Recruitment Channel Management* is out of scope: there is no channel column or list, only the single default careers channel.

### 3.2. `candidates` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `first_name` | `VARCHAR` | `NOT NULL` | Given name |
| `last_name` | `VARCHAR` | `NOT NULL` | Family and middle name(s) |
| `email` | `VARCHAR` | `UNIQUE`, `NOT NULL` | Candidate email address |
| `phone` | `VARCHAR` | `NULL` | Contact phone number |
| `avatar_url` | `VARCHAR` | `NULL` | Profile image URL |
| `address` | `TEXT` | `NULL` | Candidate address |
| `linkedin_url` | `VARCHAR` | `NULL` | LinkedIn profile URL |
| `portfolio_url` | `VARCHAR` | `NULL` | Portfolio URL |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |
| `updated_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Last updated timestamp |

### 3.3. `resumes` Table (Résumé file + intake workflow)

A row is created when a résumé is uploaded, **before** any `candidates` row exists. The same row is the backing store of the `CandidateIntake` API resource (`POST /recruitment/resumes`, `GET/POST /recruitment/intakes/{intakeId}`). `candidate_id` is filled only when the intake is confirmed.

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `bigint` | `PRIMARY KEY`, identity | Internal primary key |
| `intake_id` | `uuid` | `NOT NULL`, `UNIQUE`, default `gen_random_uuid()` | Public identifier returned by the intake API (`CandidateIntake.id`) |
| `job_posting_id` | `bigint` | `FK job_postings(id)`, `NOT NULL` | Requisition the résumé was submitted for (`CandidateIntake.requisitionId`) |
| `candidate_id` | `bigint` | `FK candidates(id)`, `NULL` | Owner candidate; set on confirmation, required when `intake_status = 'completed'` |
| `object_key` | `varchar(1024)` | `NOT NULL`, `UNIQUE` | Private object-storage key |
| `original_file_name` | `varchar(255)` | `NOT NULL` | Original uploaded file name |
| `content_type` | `varchar(100)` | `NOT NULL` | PDF, DOC or DOCX |
| `size_bytes` | `bigint` | `NOT NULL`, `CHECK ≤ 10 MiB` | File size |
| `intake_status` | `varchar(30)` | `NOT NULL`, `CHECK`, default `scanning` | Aggregate workflow state: `scanning` → `parsing` → `awaiting_confirmation` / `duplicate_review` → `completed`; terminal `rejected` (infected or unsupported) / `failed` (parser error). Matches `CandidateIntake.status` |
| `malware_scan_status` | `varchar(30)` | `NOT NULL`, `CHECK` | `pending`, `clean`, `infected`, `failed` |
| `parser_status` | `varchar(30)` | `NOT NULL`, `CHECK` | `pending`, `processing`, `completed`, `failed`, `confirmed` |
| `parsed_data` | `jsonb` | `NULL` | Structured fields extracted by the CV parser (`CandidateIntake.parsedCandidate`) |
| `parse_confidence` | `jsonb` | `NULL` | Per-field confidence 0–1 (`CandidateIntake.confidence`) |
| `parser_version` | `varchar(100)` | `NULL` | Parser build that produced `parsed_data` |
| `duplicate_candidate_ids` | `bigint[]` | `NOT NULL`, default `{}` | Existing candidates matched by normalized email/phone (`CandidateIntake.duplicateCandidates`) |
| `uploaded_by` | `bigint` | `FK users(id)`, `NULL` | Recruiter who uploaded; `NULL` for candidate-portal uploads |
| `uploaded_at` | `timestamptz` | `NOT NULL`, default `now()` | Upload timestamp (`CandidateIntake.uploadedAt`) |
| `confirmed_by` | `bigint` | `FK users(id)`, `NULL` | Recruiter who confirmed the parsed data |
| `confirmed_at` | `timestamptz` | `NULL` | Confirmation timestamp; required when `intake_status = 'completed'` |

Invariant `ck_resume_completed_has_candidate`: a `completed` intake always has a `candidate_id` and `confirmed_at`. Partial index `ix_resumes_open_intakes` serves the recruiter's "pending intakes" list per requisition.

### 3.4. `applications` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `candidate_id` | `INTEGER` | `FOREIGN KEY` -> `candidates(id)`, `NOT NULL` | Applicant |
| `job_posting_id` | `INTEGER` | `FOREIGN KEY` -> `job_postings(id)`, `NOT NULL` | Job posting applied for |
| `resume_id` | `INTEGER` | `FOREIGN KEY` -> `resumes(id)`, `NULL` | Resume submitted with the application |
| `stage` | `VARCHAR` | `NOT NULL`, `DEFAULT 'Sourced & Applied'` | Current recruitment stage |
| `ai_score` | `INTEGER` | `NULL` | Automated screening score |
| `source` | `VARCHAR` | `DEFAULT 'Direct'` | Application source or channel |
| `stage_metadata` | `JSONB` | `NULL` | Stage-specific data for the recruitment workflow |
| `applied_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Application timestamp |
| `updated_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Last updated timestamp |

### 3.5. `interviews` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `application_id` | `INTEGER` | `FOREIGN KEY` -> `applications(id)`, `NOT NULL` | Application being interviewed |
| `interview_type` | `VARCHAR` | `NOT NULL` | Interview round or format |
| `scheduled_at` | `TIMESTAMP` | `NOT NULL` | Scheduled interview time |
| `location` | `VARCHAR` | `NULL` | In-person location |
| `meeting_url` | `VARCHAR` | `NULL` | Online-meeting link |
| `interviewer_id` | `INTEGER` | `NULL` | Employee assigned as interviewer |
| `status` | `VARCHAR` | `DEFAULT 'Scheduled'` | Interview status |
| `notes` | `TEXT` | `NULL` | Scheduling or interview notes |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 3.6. `evaluations` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `interview_id` | `INTEGER` | `FOREIGN KEY` -> `interviews(id)`, `NOT NULL` | Interview being evaluated |
| `evaluator_id` | `INTEGER` | `NOT NULL` | Employee who completed the scorecard |
| `technical_score` | `INTEGER` | `NULL` | Technical competency score |
| `communication_score` | `INTEGER` | `NULL` | Communication score |
| `problem_solving_score` | `INTEGER` | `NULL` | Problem-solving score |
| `teamwork_score` | `INTEGER` | `NULL` | Teamwork score |
| `overall_score` | `DECIMAL(3, 1)` | `NULL` | Overall evaluation score |
| `recommendation` | `VARCHAR` | `NULL` | Hiring recommendation |
| `feedback` | `TEXT` | `NULL` | Qualitative feedback |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |

### 3.7. `offers` Table

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `SERIAL` | `PRIMARY KEY` | Primary key |
| `application_id` | `INTEGER` | `FOREIGN KEY` -> `applications(id)`, `NOT NULL` | Application receiving the offer |
| `base_salary` | `DECIMAL(15, 2)` | `NOT NULL` | Offered base salary |
| `bonus_amount` | `DECIMAL(15, 2)` | `NULL` | Offered bonus amount |
| `employment_type` | `VARCHAR` | `DEFAULT 'Permanent Full-time'` | Proposed employment arrangement |
| `start_date` | `DATE` | `NOT NULL` | Proposed employment start date |
| `expiration_date` | `DATE` | `NOT NULL` | Offer acceptance deadline |
| `status` | `VARCHAR` | `DEFAULT 'Sent'` | Offer status |
| `offer_letter_url` | `VARCHAR` | `NULL` | URL of the generated offer letter |
| `created_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Creation timestamp |
| `updated_at` | `TIMESTAMP` | `DEFAULT CURRENT_TIMESTAMP` | Last updated timestamp |

---

## 4. Core HR Lifecycle Module (canonical)

### 4.1. `probation_reviews` — probation review & confirmation (EMP-06)

A probation contract has exactly one review (`ux_probation_review_contract`). The outcome only reaches `employees.status` through an `approved` `employee_events` row; it is never written directly.

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `bigint` | `PK identity` | Primary key |
| `employee_id` | `bigint` | `FK employees(id)`, `NOT NULL` | The employee on probation |
| `contract_id` | `bigint` | `FK contracts(id)`, `UNIQUE`, `NOT NULL` | The probation contract being reviewed |
| `review_due_date` | `date` | `NOT NULL` | The deadline for completing the review (before the contract expires) |
| `reviewer_user_id` | `bigint` | `FK users(id)`, `NULL` | The line manager performing the review |
| `status` | `varchar(30)` | `CHECK`, default `pending` | `pending` → `in_review` → `decided` \| `cancelled` |
| `outcome` | `varchar(30)` | `CHECK`, `NULL` | `confirmed` \| `extended` \| `terminated` |
| `overall_score` | `numeric(3,1)` | `CHECK 0..5`, `NULL` | The overall review score |
| `strengths` / `improvements` | `text` | `NULL` | Qualitative comments |
| `effective_date` | `date` | `NULL` | The effective date of the outcome |
| `decided_by` / `decided_at` | `bigint` / `timestamptz` | `NULL` | Who decided, and when |
| `employee_event_id` | `bigint` | `FK employee_events(id)`, `NULL` | The employee event created from the outcome |
| `version` | `bigint` | `NOT NULL` default 1 | Optimistic concurrency |

**Invariant:** `ck_probation_decided` — when `status = 'decided'`, `outcome`, `decided_by`, `decided_at` and `effective_date` are all required.

### 4.2. `offboarding_cases` — the separation case & handover (EMP-07)

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `bigint` | `PK identity` | Primary key |
| `employee_id` | `bigint` | `FK employees(id)`, `NOT NULL` | The leaving employee |
| `employee_event_id` | `bigint` | `FK employee_events(id)`, `NULL` | The matching `termination` event |
| `separation_type` | `varchar(40)` | `CHECK`, `NOT NULL` | `resignation` \| `mutual_agreement` \| `dismissal` \| `contract_expiry` \| `retirement` |
| `notice_received_on` | `date` | `NULL` | When the notice was received, used to check the notice period |
| `last_working_date` | `date` | `NOT NULL` | The last working date |
| `handover_to_employee_id` | `bigint` | `FK employees(id)`, `CHECK <> employee_id` | Who receives the handover |
| `exit_interview_at` | `timestamptz` | `NULL` | When the exit interview takes place |
| `final_settlement_status` | `varchar(30)` | `CHECK`, default `pending` | `pending` \| `calculated` \| `paid` \| `waived` |
| `status` | `varchar(30)` | `CHECK`, default `draft` | `draft` → `pending_approval` → `approved` → `in_progress` → `completed` \| `cancelled` |
| `reason` | `text` | `NOT NULL` | The reason for leaving |
| `created_by` / `approved_by` / `approved_at` / `completed_at` | | | The approval and completion trail |

**Invariant:** `ux_offboarding_open_case` — an employee has at most one open case (`draft`/`pending_approval`/`approved`/`in_progress`).

### 4.3. `offboarding_tasks` — the handover & recovery checklist

| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `bigint` | `PK identity` | Primary key |
| `offboarding_case_id` | `bigint` | `FK offboarding_cases(id) ON DELETE CASCADE` | The offboarding case |
| `template_key` | `varchar(100)` | `UNIQUE(case, template_key)` | Prevents duplicate tasks when the case is reprocessed |
| `category` | `varchar(30)` | `CHECK` | `it` \| `admin` \| `hr` \| `manager` \| `finance` |
| `task_name` / `description` | `varchar(255)` / `text` | | For example: recover the laptop, lock the AD/Git accounts, collect the badge, settle debts |
| `assigned_to_user_id` | `bigint` | `FK users(id)`, `NULL` | The owner |
| `due_at` | `timestamptz` | `NULL` | The due date |
| `blocks_last_working_day` | `boolean` | default `false` | A blocking task: the case cannot be closed while it is outstanding |
| `status` | `varchar(30)` | `CHECK`, default `pending` | `pending` → `in_progress` → `completed` |

---

## 5. Identity & Access + Platform Tables

These eight tables belong to no single business module, yet every module depends on them. The full DDL specification is in [`schema.sql`](schema.sql).

| Table | Purpose | Key invariant |
| :--- | :--- | :--- |
| `users` | The application identity. `external_subject` carries a `local|` prefix for accounts issued by QLNS | `external_subject` and `email` are `UNIQUE`; `status IN (active, disabled)`; `version` is the ETag of every administrative command |
| `user_credentials` | The local password and the lockout counters | 1–0..1 with `users` (an account may have no password); `failed_attempts >= 0`; `password_hash` carries its own algorithm parameters |
| `user_roles` | Role grants with their **data scope** | `ck_user_roles_scope`: a `department` scope requires `data_scope_id > 0`; `self`/`organization` require `= 0`; `role_code` references `roles(code)` |
| `roles` | The role catalogue (reference data) | Loaded from [`seed_roles.sql`](seed_roles.sql); `is_assignable = false` retires a role without losing its history |
| `role_permissions` | The role → permission matrix (reference data) | PK `(role_code, permission)`; sign-in resolves permissions with `user_roles ⋈ role_permissions` |
| `refresh_tokens` | The **revocable** long-lived session | `token_hash` is the SHA-256 of the token and is `UNIQUE` — the original token is never stored; `expires_at > issued_at`; `revoked_at` and `revoked_reason` are always both set or both null |
| `audit_logs` | The log of every create, update, approve and reject, written in the business transaction | `result IN (succeeded, rejected, failed)`; indexed by entity and by actor; a failed sign-in must also leave a row |
| `outbox_messages` | The transactional outbox for e-mail, notifications and integrations | Written in the business transaction; a partial index over the unprocessed rows |

### 5.1. `user_credentials` Table (Local sign-in secret)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `user_id` | `BIGINT` | `PRIMARY KEY`, `FK users(id) ON DELETE CASCADE` | One row per account that can sign in with a password |
| `password_hash` | `VARCHAR(255)` | `NOT NULL` | Of the form `pbkdf2-sha512$<iterations>$<salt>$<hash>` — the parameters live in the string, so raising the work factor needs no migration |
| `password_algorithm` | `VARCHAR(40)` | `NOT NULL`, default `pbkdf2-sha512` | Used to filter and size the work when the algorithm changes |
| `must_change_password` | `BOOLEAN` | `NOT NULL`, default `false` | Set when an administrator chose the password; the session issued then carries no permissions |
| `password_updated_at` | `TIMESTAMPTZ` | `NOT NULL`, default `now()` | Supports a password-age policy later |
| `failed_attempts` | `INTEGER` | `NOT NULL`, default 0, `>= 0` | Consecutive failures; reset on a successful sign-in |
| `locked_until` | `TIMESTAMPTZ` | `NULL` | The end of the lockout window (5 failures ⇒ 15 minutes) |
| `last_login_at` | `TIMESTAMPTZ` | `NULL` | The most recent successful sign-in |
| `version` | `BIGINT` | `NOT NULL`, default 1 | The concurrency anchor of the change-password command |

### 5.2. `refresh_tokens` Table (Rotating session tokens)
| Column | Data Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `id` | `BIGINT` | `PRIMARY KEY`, Auto Increment | Primary key |
| `user_id` | `BIGINT` | `NOT NULL`, `FK users(id) ON DELETE CASCADE` | The owner of the session |
| `token_hash` | `VARCHAR(64)` | `NOT NULL`, `UNIQUE` | The SHA-256 hex of the token; the original is **not** stored, so a dump of this table cannot be replayed. `varchar` rather than `char`, so a `text` parameter matches the unique index directly on the hot sign-in path |
| `issued_at` | `TIMESTAMPTZ` | `NOT NULL`, default `now()` | When it was issued |
| `expires_at` | `TIMESTAMPTZ` | `NOT NULL`, `> issued_at` | 14 days by default |
| `revoked_at` | `TIMESTAMPTZ` | `NULL` | Always accompanied by `revoked_reason` |
| `revoked_reason` | `VARCHAR(40)` | `NULL`, CHECK enum | `rotated`, `logout`, `password_changed`, `reuse_detected`, `revoked_by_admin`, `account_disabled` |
| `replaced_by_token_id` | `BIGINT` | `NULL`, `FK refresh_tokens(id)` | Links the rotation chain; used to investigate a detected replay |
| `client_ip` | `VARCHAR(45)` | `NULL` | A client trace, wide enough for IPv6 |
| `user_agent` | `VARCHAR(255)` | `NULL` | A client trace, truncated if longer |

`CREATE INDEX ix_refresh_tokens_active ON refresh_tokens(user_id, expires_at) WHERE revoked_at IS NULL` — the hot operation is "revoke every valid token of one account", so the partial index matches the shape of that statement.

---

## 6. Preconditions Before Generating a Migration

1. `employees.work_email` is **nullable**, with the case-insensitive partial unique index `ux_employees_work_email`: an employee created by the offer-acceptance handoff has no work e-mail yet, and `ck_employee_active_requires_work_email` blocks the move to `active` while it is empty. `employee_code` comes from the `employee_code_seq` sequence. §2.3 above is a historical description and still says `email NOT NULL` — the canonical schema wins.
2. ~~Choose an Identity Provider~~ — settled: authentication is in house ([ADR-011](../docs/adr/011-in-house-identity.md)). What remains is deciding where the `Authentication:Jwt:SigningKey` secret is managed, and putting `seed_roles.sql` into the deployment procedure of every environment.
3. Integration tests against a real PostgreSQL are needed for each write-blocking invariant: one open offer per application, one active primary contract, one probation review per contract, one open offboarding case, and uniqueness of `application_stage_events` per version.
4. Tests are needed for applying `employee_events` on the right `effective_date`, and for the idempotency of the offer-acceptance handoff.
