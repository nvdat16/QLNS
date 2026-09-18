# 🗄️ Database Architecture & Schema

> This directory holds the relational schema, the entity–relationship diagram (ERD), the PostgreSQL DDL files and the sample seed data for the **QLNS** system.

> **Status:** this is a **database design and reference DDL**. The backend is code-complete and reads and writes these tables correctly, but the schema is still loaded by hand with `psql`: there is no EF Core migration pipeline and no integration test has run against a real PostgreSQL. The presence of a SQL file does not mean the database has been deployed or that the invariants have been verified.

> [!IMPORTANT]
> Only [`schema.sql`](schema.sql) is canonical — **28 tables** (baseline v1.2), covering the two in-scope business modules (Core HR including Contracts, and Recruitment) plus the Identity & Access (ADM) module. The Attendance & Leave DDL (13 tables) is parked out of scope at [docs/deferred/attendance_leave/schema_attendance_leave.sql](../docs/deferred/attendance_leave/schema_attendance_leave.sql). `init.sql` and `postgres_db.sql` are marked **DEPRECATED** in the files themselves and do not match the canonical schema; do not generate migrations from them.

> [!NOTE]
> **Delivery scope** follows the bold leaf functions under Recruitment and Core HR on the [`topdown-approach.png`](../topdown-approach.png) map; the authoritative source is [section 2 of the root README](../README.md#2-delivery-scope--seven-pillars-two-selected). Out of scope for this delivery: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart and Suspension & Return to Work. Narrowing the scope **does not change the DDL**: what was dropped is screens and endpoints, not data structures. The `interview_panelists` table and the `currency` columns were added in v1.1 while writing the code — see [section 2.6](#26-delta-v11--surfaced-during-implementation). The four identity tables of v1.2 (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`) were added when authentication moved in-house — see [section 2.7](#27-delta-v12--moving-authentication-in-house).

---

## 📌 Contents

- [1. Entity–Relationship Diagram (ERD)](#1-entityrelationship-diagram-erd)
- [2. The 28 Canonical Tables](#2-the-28-canonical-tables)
- [3. Schema File Index](#3-schema-file-index)
- [4. Checking the Schema Design](#4-checking-the-schema-design-optional)
- [🔗 Back to the project README](../README.md)

---

## 1. Entity–Relationship Diagram (ERD)

The current ERD is the Mermaid diagram in [`database_design.md`](./database_design.md#1-overall-entityrelationship-diagram-mermaid-erd), covering all **28 canonical v1.2 tables** — including `interview_panelists` (delta v1.1) and the four identity tables of delta v1.2 (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`). The diagram shows keys and discriminating columns only; the field-level specification is in the later sections of `database_design.md`.

> [!NOTE]
> The old DBML diagram (`dbml.txt` and the `dbml.png` image, a 14-table model) was deleted from the repository because it did not match the canonical schema. Do not rebuild it alongside the Mermaid ERD: two diagram sources will drift apart.

---

## 2. The 28 Canonical Tables

Canonical v1.2 (`schema.sql`) has 28 tables in five groups, numbered 1–28 without gaps. The **Status** column uses the same scale as
[API_REFERENCE §7](../docs/api/API_REFERENCE.md#7-implementation-status): `Code-complete` means the backend reads and writes the
table through a repository whose transaction also writes audit and outbox rows, and that unit tests exist. **No table has reached
`Implemented`**, because that requires integration tests against a real PostgreSQL (`tests/Qlns.IntegrationTests` does not exist yet)
and a versioned EF Core migration.

### 2.1. Core HR — Organization & Profile

| # | Table | Purpose | Status |
| :---: | :--- | :--- | :--- |
| 1 | **`departments`** | The department catalogue, with `parent_department_id` for a multi-level hierarchy and a `cost_center`. *Departments & Organizational Hierarchy* is in scope, so parent-child relationships, the cycle-prevention rule and the delete constraints are all kept; only the *Organizational Chart* screen and endpoint are out of scope. | Code-complete |
| 2 | **`positions`** | The catalogue of job titles, positions and levels. | Code-complete |
| 3 | **`employees`** | The central employee table: identity, contact details, department, position, `manager_id` and employment status. `work_email` is nullable until activation; the unique `source_application_id` is the idempotency key of the recruitment handoff. `status = 'suspended'` is a **reserved, out-of-scope value** in this delivery — no endpoint can set it. | Code-complete · target of REC-06.2 |

### 2.2. Core HR — Employee Lifecycle

| # | Table | Purpose | Status |
| :---: | :--- | :--- | :--- |
| 4 | **`onboarding_tasks`** | The onboarding checklist for a new hire, generated from a template per unit and position. | Code-complete |
| 5 | **`employee_events`** | The history of employee changes, with before/after JSON and an approval flow. An `event_type` of `'suspension'` or `'return_to_work'` is a **reserved, out-of-scope value** in this delivery — no endpoint can set it. | Code-complete |
| 6 | **`employee_documents`** | Personal records, diplomas and certificates; stores a private `object_key` together with a retention deadline. | Code-complete |
| 7 | **`probation_reviews`** | Probation review and confirmation (`confirmed` / `extended` / `terminated`). | Code-complete |
| 8 | **`offboarding_cases`** | The separation case: separation type, last working date, the person receiving the handover, and the final settlement. | Code-complete |
| 9 | **`offboarding_tasks`** | The handover checklist and the recovery of assets and accounts, with a flag for blocking tasks. | Code-complete |

### 2.3. Core HR — Contracts

| # | Table | Purpose | Status |
| :---: | :--- | :--- | :--- |
| 10 | **`contracts`** | The employment contract: type, salary and `currency` (v1.1), effective and expiry dates, status, and the digitised signed copy. | Code-complete |
| 11 | **`contract_addenda`** | Contract addenda with before/after terms and the approval history. Since v1.1 `version` is the optimistic concurrency version (ETag); an addendum is identified by `addendum_number`. | Code-complete |

### 2.4. Recruitment (ATS)

| # | Table | Purpose | Status |
| :---: | :--- | :--- | :--- |
| 12 | **`job_postings`** | Requisitions and job postings, the salary budget and the number of hires needed. | Code-complete |
| 13 | **`candidates`** | Candidate profiles, normalised e-mail and phone for duplicate detection, and the data-processing consent timestamp. | Code-complete |
| 14 | **`resumes`** | The CV file with its intake lifecycle (`intake_id`, `intake_status`), malware scan status, parsed data and parse confidence. Created before a `candidates` row exists. | Code-complete |
| 15 | **`applications`** | The application linking a candidate to a posting and to a pipeline stage. | Code-complete |
| 16 | **`application_stage_events`** | The stage transition history, guarded against lost updates by `application_version`. | Code-complete |
| 17 | **`interviews`** | The interview schedule with its timezone, the lead interviewer (`interviewer_user_id`) and a status. | Code-complete |
| 18 | **`interview_panelists`** *(v1.1)* | The interview panel — one row per entry of `InterviewWrite.interviewerUserIds`; the `interviewer_user_id` of `interviews` is always present in this table. | Code-complete |
| 19 | **`evaluations`** | The scorecard, scored 0–5 in steps of 0.5, with an unlock mechanism that requires a reason. | Code-complete |
| 20 | **`offers`** | The job offer; a partial index guarantees at most one open offer per application. The `currency` column (v1.1) matches `OfferWrite.currency`. | Code-complete |

### 2.5. Platform — identity, authorization, audit, outbox

These eight tables are **part of the canonical schema**. The first six carry the identity, credential and authorization data of the Identity & Access (ADM) module and **do have an administration API**, at `/api/v1/auth/*` and `/api/v1/admin/*`. The last two are mandatory crosscutting mechanisms with **no** API: no endpoint browses audit logs and none inspects or retries a delivery.

| # | Table | Purpose | Status |
| :---: | :--- | :--- | :--- |
| 21 | **`users`** | The application's identity data. `external_subject` carries a `local|` prefix for accounts issued by QLNS, reserving the namespace in case of federation with an external IdP later. Administered through `/api/v1/admin/users`. | Code-complete |
| 22 | **`user_credentials`** | A PBKDF2-HMAC-SHA512 password hash whose parameters live inside the hash string, plus a force-password-change flag, a failure counter and a lockout deadline. One row per account that can sign in with a password. | Code-complete |
| 23 | **`user_roles`** | Role grants with their data scope (`self` / `department` / `organization`) — the source for the server-side permission check required on every request. Administered through `PUT /api/v1/admin/users/{userId}/roles`. | Code-complete |
| 24 | **`roles`** | The role catalogue. Reference data loaded from `seed_roles.sql`; `user_roles.role_code` points here. | Code-complete |
| 25 | **`role_permissions`** | The role → permission matrix. Sign-in resolves permissions with `user_roles ⋈ role_permissions`, so changing the matrix needs no rebuild. Read-only through the API. | Code-complete |
| 26 | **`refresh_tokens`** | Single-use refresh tokens: only the SHA-256 digest is stored, together with the successor token and the revocation reason. Presenting an already revoked token revokes the whole token family of that account. | Code-complete |
| 27 | **`audit_logs`** | The action log with before/after data and a `correlation_id`. A **mandatory mechanism**: written in the same transaction as the business change — including failed sign-in attempts (`result = 'rejected'`). No lookup endpoint in this delivery. | Code-complete |
| 28 | **`outbox_messages`** | The transactional outbox for e-mail and calendar. A **mandatory mechanism**: written in the business transaction. No delivery inspection or retry endpoint in this delivery. | Code-complete |

### 2.6. Delta v1.1 — surfaced during implementation

Four differences between the OpenAPI contract and the v1 DDL only became visible while writing the code. They were fixed in `schema.sql` itself (a create-from-scratch script) rather than as separate `ALTER` statements, and must be carried into the first EF Core migration:

| Delta | Reason |
|---|---|
| `offers.currency char(3) NOT NULL DEFAULT 'VND'` | `OfferWrite.currency` exists in the contract but had no column to store it. |
| `contracts.currency char(3) NOT NULL DEFAULT 'VND'` | Same for `ContractWrite.currency`. |
| New table `interview_panelists(interview_id, user_id)` | `InterviewWrite.interviewerUserIds` is an array; `interviews.interviewer_user_id` holds only one person. The first member of the panel is written to `interviewer_user_id` as the lead; `evaluations` are scored per panelist. |
| `contract_addenda.version bigint` reinterpreted as the **optimistic concurrency version**, dropping `ux_contract_addendum_version (contract_id, version)` | The contract uses `version` as the ETag / `If-Match` value for an addendum; a unique constraint on `(contract_id, version)` would collide as soon as two addenda of the same contract are edited. Ordering and identity of an addendum rely on `addendum_number` (UNIQUE) and the `superseded` status. |

### 2.7. Delta v1.2 — moving authentication in-house

The decision to move sign-in and account administration from an external Identity Provider into the system itself brought four new tables and one foreign key:

| Delta | Reason |
|---|---|
| New table `roles(code, name, description, is_assignable)` | A role catalogue is needed for `GET /api/v1/admin/roles` to return, and to validate `role_code` when granting a role. |
| New table `role_permissions(role_code, permission)` | The role → permission matrix previously existed only in code (the `DevelopmentAuthentication` personas). Moving it into data lets sign-in resolve permissions in SQL, and lets the matrix change without a rebuild. |
| New table `user_credentials` | `users` had nowhere to store a password; a 1–1 table keeps `users` as pure identity data and allows an account with no local password to exist. |
| New table `refresh_tokens` | A long-lived session needs a **revocable** artifact, which a stateless access token cannot be. Only the SHA-256 digest is stored, so a dump of this table cannot be replayed. |
| `user_roles.role_code` is now `REFERENCES roles(code)` | It used to be free text; a misspelled code would silently grant nothing. |

Two implementation notes: (1) `seed_roles.sql` is **mandatory reference data in every environment** and must run immediately after `schema.sql`; (2) every account in `seed_dev.sql` uses the development password `Qlns@2026` and **must never** be carried into a shared environment.

---

## 3. Schema File Index

> [!IMPORTANT]
> [`schema.sql`](schema.sql) is the **canonical schema contract for baseline v1.2**. `init.sql` and `postgres_db.sql` are prototype/legacy artifacts kept for comparison and must not be used as the source of a new migration. Once the backend has an approved EF Core migration, the migration becomes the deployment source and `schema.sql` must be drift-checked in CI.

| File | Description | Link |
| :--- | :--- | :--- |
| **`database_design.md`** | The technical specification of every field, data type, primary and foreign key constraint, plus the Mermaid ERD. | [Open database_design.md](./database_design.md) |
| **`schema.sql`** | **The canonical schema contract, v1.2 — 28 tables.** The single source of truth for data types, constraints, indexes and exclusion constraints. | [Open schema.sql](./schema.sql) |
| **`seed_roles.sql`** | **Mandatory reference data:** the catalogue of 8 roles and the role → permission matrix. Runs after `schema.sql` and before `seed_dev.sql`; idempotent (`ON CONFLICT`). | [Open seed_roles.sql](./seed_roles.sql) |
| **`init.sql`** | ⚠️ **DEPRECATED.** An old seed/DDL script with no identity, authorization, audit, outbox or lifecycle tables. | [Open init.sql](./init.sql) |
| **`postgres_db.sql`** | ⚠️ **DEPRECATED.** Legacy DDL; its attendance and leave tables are out of scope and do not match the deferred design. | [Open postgres_db.sql](./postgres_db.sql) |
| **`seed_dev.sql`** | Sample data **for the development environment only**: 8 users (all signing in with `Qlns@2026`), 6 departments, 6 positions, 7 employees, 5 onboarding tasks, 2 requisitions, 2 candidates and applications, 1 scored interview, 3 contracts and 1 probation review. Not part of the schema contract and never to be run in a shared environment. | [Open seed_dev.sql](./seed_dev.sql) |

---

## 4. Checking the Schema Design (optional)

The project ships no ready-made database environment. If you already have a PostgreSQL instance you manage yourself, you can load the reference DDL with `psql`:

```bash
psql -v ON_ERROR_STOP=1 -d <test_database> -f database/schema.sql

# once connected to the test database
\dt
SELECT id, employee_code, first_name, last_name, work_email, status FROM employees;
```

Never use these default credentials or sample data in production. When backend work starts, pick a versioned migration tool, split the seed data per environment and add constraint and rollback tests.

---

[⬅️ Back to the project README](../README.md)
