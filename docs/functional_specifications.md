# 📘 SOFTWARE REQUIREMENTS SPECIFICATION (SRS)
## Centralised HR & Recruitment Management System (QLNS / NexusHR)

---

## 1. Introduction

> **Implementation status:** this document is a specification of target requirements. The project has UI/UX prototypes, a canonical database and OpenAPI contract, a React frontend and a **code-complete** .NET 10 backend covering all 79 operations (controller, authorization policy, workflow, persistence with audit and outbox, unit tests). There are no integration tests against PostgreSQL, no EF Core migrations and no background worker, so no behaviour has been verified end to end; `code-complete` is not production-ready.

### 1.1. Project goals
**QLNS / NexusHR** is a human resource management (HRMS) and smart recruitment (ATS) system whose purpose is to:
- Digitise the full employee lifecycle: from application, interview and probation intake through signing a permanent contract, employment changes (promotion, transfer) and finally separation.
- Automate CV screening, interview scoring and contract expiry alerts.
- Centralise recruitment, employee and contract data into a single source, replacing the scattered spreadsheets currently exchanged between departments.
- Comply with Vietnamese labour law on contracts, social insurance and the retention of personnel records.

### 1.2. Users and the permission & data scope matrix

| Role | Role code | Main responsibilities |
| :--- | :--- | :--- |
| **Super Admin** | `ROLE_ADMIN` | Administers accounts, roles and data scope in the `[ADM]` module: create and disable accounts, grant and revoke roles, reset passwords. Holds **no business permission at all** — cannot see profiles, contracts or recruitment data. Integration, notification and approval configuration remain in `appsettings`, out of scope. |
| **HR Director / Manager** | `ROLE_HR_MGR` | Approves hiring requests and offer letters, and signs off appointment, transfer and contract-termination decisions across the organisation. |
| **Talent Acquisition (Recruiter)** | `ROLE_RECRUITER` | Manages job postings, screens CVs, schedules interviews, tracks the ATS Kanban board, and sends interview invitations and offers. |
| **Hiring Manager / Interviewer** | `ROLE_INTERVIEWER` | Raises requisitions, sits on interview panels, scores candidates on the scorecard and makes hiring recommendations. |
| **HR Officer (C&B / Records)** | `ROLE_HR_OFFICER` | Manages the employee directory, drafts and tracks employment contracts, and follows the onboarding checklist. |
| **Line Manager** | `ROLE_LINE_MGR` | Runs probation reviews, confirms handovers when an employee leaves, and proposes employee movements for their own team. Data scope limited to `data_scope_type = 'department'`. |
| **Employee** | `ROLE_EMPLOYEE` | Views their own profile, sees their department and direct manager, looks up their own contract, and performs their handover when leaving. |

The authorization principle does not change: every request must have its role **permission** and its **data scope** (`data_scope_type`: organization / department / self) checked server-side; permissions sent by the client are never trusted.

Role codes are reference data in the `roles` table; the role → permission matrix lives in `role_permissions` and is deployed through [`database/seed_roles.sql`](../database/seed_roles.sql). Besides the seven roles above there is `ROLE_IT_ADMIN`, for whoever performs the IT tasks in the onboarding and offboarding checklists. At sign-in, permissions are resolved with `user_roles ⋈ role_permissions`, so **revoking a role takes effect at the next session refresh** with no redeployment. An account with no `user_roles` rows can still sign in but can call no business endpoint at all (deny by default).

### 1.3. Delivery scope and specification principles

- **The source of scope**: the `topdown-approach.png` function decomposition map (see section 2 of [README.md](../README.md)). How to read it: **only the leaf functions printed in bold under the Recruitment and Core HR pillars are within the delivery scope**; the non-bold leaves and all remaining pillars are out of scope.
- **The two modules selected for delivery**: Recruitment (18 leaf functions) and Core HR (16 leaf functions). Contract Management is a group inside Core HR, not a pillar of its own; this document treats it as a separate module purely for presentation. This is the scope mapped in full down to user stories, the canonical schema and the API contract.
- **Already present at the data-design level**: both modules above, the `[ADM]` identity module (`users`, `user_credentials`, `user_roles`, `roles`, `role_permissions`, `refresh_tokens`), the audit log and the outbox. `database/schema.sql` is the canonical schema contract, v1.2, with **28 tables**; the older DDL (`init.sql`, `postgres_db.sql`, `dbml.txt`) is marked deprecated, and the schema is not yet managed by EF Core migrations at runtime.
- **Out of the delivery scope — designed, then paused**: attendance and leave. Its specification, user stories, DDL and API contract are kept at [docs/deferred/attendance_leave/](deferred/attendance_leave/README.md) for later reuse; none of it is part of the current canonical schema or OpenAPI document.
- Out-of-scope modules are **not** specified in this document, not even by name; the full boundary and the reasoning are in section [2.1](#21-scope-boundary--what-is-not-in-this-delivery).
- Every create, update, approve, reject and download operation must check the caller's role permission and record who acted and when.
- Business statuses must be constrained to a valid value set; direct updates and bypassing an approval step through the UI or the API are never allowed.

---

## 2. Functional Module Specification

The decomposition below contains only functions within the delivery scope, taken directly from the **bold** leaf functions under the Recruitment and Core HR pillars of `topdown-approach.png` (section 2 of [README.md](../README.md)). The other pillars of the map do not appear here, exactly as the settled scope requires.

```
QLNS / NexusHR
├── MODULE 1: SMART RECRUITMENT MANAGEMENT (ATS) — the Recruitment pillar
│   ├── [REC-01] Job requisitions & job postings
│   ├── [REC-02] CV intake & AI parsing
│   ├── [REC-03] The visual recruitment pipeline (Kanban ATS)
│   ├── [REC-04] Interview scheduling & invitations
│   ├── [REC-05] Interview scorecards
│   └── [REC-06] Offer management & onboarding handoff
├── MODULE 2: EMPLOYEE RECORDS & LIFECYCLE (CORE HR) — the Core HR pillar
│   ├── [EMP-01] Employee directory & master data
│   ├── [EMP-02] Organization management
│   ├── [EMP-03] Onboarding checklist
│   ├── [EMP-04] Internal mobility & employee events
│   ├── [EMP-05] Employee documents
│   ├── [EMP-06] Probation review & confirmation
│   └── [EMP-07] Offboarding & handover
├── MODULE 3: EMPLOYMENT CONTRACT MANAGEMENT (CONTRACTS) — the Contract Management group inside Core HR
│   ├── [CON-01] Contract drafting & storage
│   ├── [CON-02] Expiry monitoring & automatic alerts
│   └── [CON-03] Contract addenda
└── MODULE 4: IDENTITY & ACCESS — the Account Management + Roles/Permissions leaves of System Administration
    ├── [ADM-01] Password sign-in & session management
    └── [ADM-02] Account & role administration
```

> Module 4 was added after the decision **not** to use an external Identity Provider. The two leaf functions Account Management and Roles/Permissions & Data Access Scope of the System Administration pillar therefore moved into the delivery scope; the rest of that pillar (approval-workflow, notification and integration configuration, audit-trail lookup) stays out.

### 2.1. Scope boundary — what is not in this delivery

This is the official scope boundary, not a gap in the document.

**a) Four non-bold leaf functions sitting inside the two selected modules**

| Function | Group | Consequence for the specification |
| :--- | :--- | :--- |
| Headcount & Budget Validation | Recruitment · Job Requisition | A requisition still has its approval flow, but the system does **not** check headcount or salary budget by itself. `target_headcount`, `salary_min` and `salary_max` are declared data for the approver to consider, not a control mechanism; the decision belongs to the HR Manager. |
| Recruitment Channel Management | Recruitment · Job Posting | Publishing, updating and closing a posting stay in scope, but only through **a single default careers portal**. There is no selection or management of multiple channels, and no measurement of sourcing effectiveness. |
| Organizational Chart | Core HR · Organization Management | There is no screen and no endpoint rendering the org tree. The department hierarchy stays **in** scope: `departments.parent_department_id`, parent-child relationships, cycle prevention, the delete constraints, headcount rollup and cost centres are all retained under `[EMP-02]`. Only the tree rendering is dropped. |
| Suspension & Return to Work | Core HR · Employee Lifecycle | There is no suspend-and-return business flow. The values `employees.status = 'suspended'` and `employee_events.event_type IN ('suspension','return_to_work')` remain in the canonical schema but are **reserved: no flow and no endpoint can set them in this delivery**. |

**b) The remaining pillars of the function map**

Reports & Analytics, Performance Management, Compensation & Benefits and Attendance & Leave Management are all out of scope and are not specified here. System Administration **enters the scope only through the two leaf functions** Account Management and Roles/Permissions & Data Access Scope (the `[ADM]` module); the rest of that pillar — approval-workflow and delegation configuration, notification configuration, integration configuration and audit-trail lookup — stays out. The Attendance & Leave design is kept intact at [docs/deferred/attendance_leave/](deferred/attendance_leave/README.md).

**c) A necessary distinction: the crosscutting mechanisms stay, the administration API does not**

- Writing an audit record in the same transaction as the business change **remains mandatory**; the `audit_logs` table is kept. Only the audit-log lookup endpoint is out of scope.
- The transactional outbox for sending e-mail and calendar invitations **remains mandatory**; the `outbox_messages` table is kept. Only the endpoints that inspect and retry a delivery are out of scope.
- Checking permission and data scope server-side on every request **remains mandatory** (section 1.2).
- The identity tables (`users`, `user_credentials`, `user_roles`, `roles`, `role_permissions`, `refresh_tokens`) carry credentials, identity and data scope. The account and role management API **is in scope** (`[ADM-02]`); the role → permission matrix is reference data editable only through `seed_roles.sql`, never through the API.
- Integration, notification and approval-workflow configuration lives in `appsettings`, with no UI and no administration API.
- `GET /health/live` and `GET /health/ready` are kept as infrastructure endpoints serving deployment and monitoring, not as business functions on the map.

---

## 3. Detailed Feature Specification

### MODULE 1: SMART RECRUITMENT MANAGEMENT (ATS)

#### [REC-01] Job requisitions & job postings
- **Goal**: let a department head raise a hiring request, the HR Manager approve it, and the recruiter publish, update and close the posting on the careers portal.
- **Actors**: Hiring Manager, HR Manager, Recruiter.
- **Precondition**: the department exists in the system.
- **Main flow**:
  1. The Hiring Manager picks a department and a job title, and enters the target headcount, the reason for hiring (replacement/growth), the indicative salary and the skill requirements.
  2. The Hiring Manager submits the request; the system moves it to `Pending Approval` and creates an approval task for the HR Manager.
  3. The HR Manager approves or rejects. Balancing headcount and salary budget is a decision for the authorised person; the system neither checks nor blocks it.
  4. The Recruiter publishes the posting to the careers portal, activating `Active Recruiting`; a published posting can be updated or closed.
  5. The system generates the job identifier (for example `REQ-2026-08`).
- **Input data**: job title, department ID, number of hires, employment type (full-time / part-time / hybrid / remote), salary range (min–max), job description.
- **Postcondition**: the record is stored in `job_postings`, ready to receive applications.
- **Rules and exceptions**:
  - Valid statuses: `Draft` → `Pending Approval` → `Approved` → `Active Recruiting` → `Closed` or `Cancelled`; only the HR Manager may approve or reject.
  - A rejection requires a reason and returns the request to `Draft` for the Hiring Manager to revise; a posting cannot be published before `Approved`.
  - `closing_date` must be after the publication date; `salary_min` must not exceed `salary_max`; `target_headcount` must be greater than zero.
  - `target_headcount`, `salary_min` and `salary_max` are declared data for the approver's reference, not an automatic control; the system does not reconcile them against a departmental salary budget or headcount plan.
  - A requisition with an offer still in `Sent` cannot be closed until that offer is resolved; every status change writes an audit record.

---

#### [REC-02] CV intake & AI parsing
- **Goal**: collect candidate CVs from the portal or from an uploaded PDF/DOCX file and extract the core information.
- **Actors**: Recruiter, candidate (applying online).
- **Main flow**:
  1. A candidate submits a CV through the web, or a recruiter uploads the CV file.
  2. The system validates the file (under 10 MB, PDF/DOC/DOCX).
  3. The AI/OCR module extracts the data: full name, e-mail, phone number, key skills, years of experience, education.
  4. The system checks for duplicates against `candidates` on e-mail and phone number:
     - If one exists: update the candidate record and link it to the new posting.
     - If not: create a new candidate record in `candidates` and store the file in `resumes`.
  5. The system computes an initial match score (an AI match score from 0 to 100%).
  6. An application record is created in `applications` with the status `Sourced & Applied`.
- **Data protection and error rules**:
  - The file is only stored after a successful malware scan; a corrupt, oversized or wrongly formatted file must be rejected with a clear message.
  - E-mails are normalised before comparison, and phone numbers are normalised to their country code. A duplicate record is merged only after the recruiter confirms, so that the profile of a different person is never overwritten.
  - AI/OCR output is a suggestion: it must display a confidence indicator and let the recruiter correct it before it is used to reject a candidate.
  - Candidates must be told the purpose of the data processing; deletion and anonymisation requests must be supported under the retention policy.

---

#### [REC-03] The visual recruitment pipeline (Kanban ATS)
- **Goal**: visualise the interview process across six standard stages, supporting drag-and-drop and a quick profile view.
- **Standard Kanban stages**:
  1. `Sourced & Applied`
  2. `AI Screening`
  3. `Tech Interview` (first technical round)
  4. `Executive Round` (management / culture fit)
  5. `Offer Letter` (offer and negotiation)
  6. `Hired & Ready` (accepted and handed off to onboarding)
- **Stage transition rules**:
  - When a candidate passes the current round, the recruiter or interviewer presses **"Advance Stage →"** or drags the card to the next column.
  - The system calls `POST /api/v1/recruitment/applications/{applicationId}/advance` to record the transition and its timestamp.
  - If the candidate does not pass: move to `Rejected` with a rejection reason; the system can trigger a templated thank-you e-mail.
- **Transition controls**:
  - Only a single forward step along the standard flow is allowed. Going backwards, skipping a step, or restoring from `Rejected`/`Withdrawn` requires the Recruiter or HR Manager permission plus a reason.
  - An application may only enter an interview round once a `Scheduled` interview exists, and may only reach `Offer Letter` after a valid evaluation under the position's policy.
  - The API must check the current version or state so that two users cannot advance the same application at once.

---

#### [REC-04] Interview scheduling & invitations
- **Goal**: coordinate interview times between the candidate and the panel without clashes.
- **Actors**: Recruiter, interviewer, candidate.
- **Flow**:
  1. The recruiter picks a candidate in an interview stage, chooses the panel (interviewer IDs), the start and end time, and the format (on site in a meeting room, or online via Google Meet, Zoom or MS Teams).
  2. The system creates a row in `interviews` with the status `Scheduled`.
  3. The system sends an automatic e-mail with a calendar file (`.ics`) to the candidate and the interviewers.
- **Rules and exceptions**:
  - Overlapping bookings for the same interviewer or meeting room are rejected; displayed times must carry an explicit timezone.
  - On a reschedule or cancellation the system keeps the history, notifies every participant of the change, and requires a reason if the cancellation falls inside the notice window.
  - Reminders go out 24 hours and 1 hour beforehand by default; if an e-mail fails, the recruiter is offered a resend task.

---

#### [REC-05] Interview scorecards
- **Goal**: standardise candidate scoring so it is objective and transparent.
- **Actor**: interviewer.
- **Flow**:
  1. After the interview, the interviewer opens the scorecard template for the position.
  2. They score each competency group out of 5.0:
     - Technical competency
     - Problem solving
     - Communication and teamwork
     - Culture fit
  3. They choose an overall recommendation: `Strong Hire`, `Hire`, `Hold`, `No Hire`, `Strong No Hire`.
  4. They write detailed comments (strengths and areas for improvement).
  5. The data is stored in `evaluations`.
- **Data rules**:
  - Each criterion takes a value from 0 to 5 in steps of 0.5; `overall_score` is computed automatically from the position's configurable weights.
  - Only an interviewer assigned to the schedule may submit a scorecard; once submitted, only the HR Manager may unlock and edit it, and a reason must be recorded.
  - Another interviewer's evaluation is not shown before the user submits their own, or before the publication point defined by policy.

---

#### [REC-06] Offer management & onboarding handoff
- **Goal**: draft and approve the offer letter before sending it to the successful candidate.
- **Actors**: Recruiter, HR Manager, candidate.
- **Flow**:
  1. The recruiter creates the offer from the posting: base salary, sign-on bonus, allowances, expected start date and response deadline (expiration date).
  2. The record is stored in `offers` with the status `Draft`.
  3. The HR Manager approves the offer → it moves to `Sent`.
  4. The candidate responds:
     - **Accepted**: the system moves the application to `Hired & Ready` and triggers the creation of the employee record `[EMP-01]` and the onboarding checklist `[EMP-03]`.
     - **Declined**: a decline reason is captured (salary not competitive, chose another company, …).
- **Rules and exceptions**:
  - An offer is only sent after approval; its content is generated from an approved template version, and the URL of the sent document is stored.
  - Past `expiration_date` an offer moves to `Expired` automatically; extending it requires a new version and a recorded re-approval.
  - Acceptance must be idempotent: one offer yields at most one `employees` record, one initial contract and one set of onboarding tasks; if a background task fails, retrying must be safe.

---

### MODULE 2: EMPLOYEE RECORDS & LIFECYCLE (CORE HR)

#### [EMP-01] Employee directory & master data
- **Goal**: manage the central identity information of every employee.
- **Data managed**:
  - Personal: full name, employee code (`EMP-xxxx`), date of birth, gender, national ID, nationality, marital status.
  - Contact: work e-mail, personal e-mail, mobile number, permanent address, temporary address, emergency contact.
  - Employment: department ID, position ID, band/grade (for example IC-1 through L-9), direct manager ID, work location (Hanoi office, Ho Chi Minh City, remote), hire date.
  - Employment statuses used in this delivery: `Active`, `Probation`, `Terminated`. The value `Suspended` remains in the canonical schema as a **reserved** value — out of scope in this delivery, settable by no flow and no endpoint (see section 2.1).
- **System requirements**:
  - Full-text search by name, employee code, e-mail or job title.
  - Multi-dimensional filtering by department, grade and status.
  - An employee may only view and request changes to their own profile fields; HR Officers and HR Managers may edit business data within their granted scope.
  - Work e-mail and employee code must be unique. Changing the e-mail, department, job title, status or direct manager must go through the movement flow `[EMP-04]`; it can never be edited directly on the profile screen.
  - Sensitive identity fields (national ID, bank details, emergency contact) must be encrypted at rest, partially masked on display, and excluded from default data exports.

---

#### [EMP-02] Organization management
- **Goal**: manage the master data of the organisational hierarchy (department → unit/pod → position → employee), the job title and grade catalogue, and the reporting lines that underpin employee records, requisitions and contracts.
- **Business rules**:
  - Each department has a unique department code (`code`) and a name.
  - Departments are organised in multiple levels through `parent_department_id`; a department has at most one parent.
  - The system computes the headcount underneath a department automatically and attaches a cost centre to each one, including its sub-departments.
  - The job title and grade catalogue (`positions`) is managed centrally; employee assignments and reporting lines come from `employees.department_id`, `employees.position_id` and `employees.manager_id`, and every change must go through the movement flow `[EMP-04]`.
  - A department that still has employees or an active requisition cannot be deleted; the related records must be moved or closed first.
- **Data dependencies**: the canonical schema already has `departments(id, code, name, parent_department_id, cost_center, description, version)` together with the `ck_departments_not_self_parent` constraint, which is enough to express a multi-level hierarchy and compute cost centres. Reporting lines come from `employees.manager_id`.
  - **Missing**: a `manager_id` column at the department level (the formal head of the unit). Today it can only be inferred from `employees.manager_id`, so a department that temporarily has no staff cannot be expressed. A dedicated migration is needed if the business requires it.
  - The parent-child relationship must prevent cycles at the service layer (A is the parent of B, B is the parent of A); the current constraint only stops a department pointing at itself.

---

#### [EMP-03] Onboarding checklist
- **Goal**: have the equipment, system accounts and induction training ready before the new hire's first day.
- **Actors**: HR Officer, IT Admin, facilities admin, the new hire.
- **Flow**:
  1. When a candidate accepts the offer, the system generates the task list in `onboarding_tasks`:
     - IT: create the work e-mail, grant Slack/Git/ERP access, prepare the laptop.
     - Facilities: prepare the employee badge, the desk and the parking pass.
     - HR: prepare the probation contract, guide the submission of personal documents (photo, diplomas, tax details).
     - Line manager: assign a buddy or mentor.
  2. Each task has a due date (`due_date`) and an owner (`assigned_to`).
  3. The owner marks it done → the status becomes `Completed` with a completion timestamp.
- **Business rules**:
  - The checklist must be generated from a template per unit, position, location and contract type; reprocessing the offer must not create duplicate tasks.
  - Valid task statuses: `Pending` → `In Progress` → `Completed`; a completed task may only be reopened by an HR Officer or HR Manager, with a recorded reason.
  - The onboarding tracking screen must flag overdue tasks and tasks that block the start date (for example, an account not yet provisioned or a probation contract not yet signed).

---

#### [EMP-04] Internal mobility & employee events
- **Goal**: keep a full trace of departmental transfers, promotions, job-title adjustments and disciplinary or recognition decisions throughout employment.
- **Actors**: HR Manager, Director.
- **Flow**:
  1. When a personnel decision is made, HR creates a movement record in `employee_events`.
  2. The event types (`event_type`, constrained by `ck_employee_event_type` in the canonical schema):
     - `probation_confirmation` (probation passed → permanent) — generated by `[EMP-06]`
     - `probation_extension` — generated by `[EMP-06]`
     - `promotion`
     - `transfer` (between departments or branches)
     - `demotion`
     - `salary_adjustment`
     - `termination` — generated by `[EMP-07]`
  3. The system stores the old values (`old_department_id`, `old_position_id`), the new values (`new_department_id`, `new_position_id`), the effective date (`effective_date`) and the reason.
  4. On the effective date the system updates the employee's master record in `employees` automatically.
- **Approval and effectiveness rules**:
  - A movement record needs a `Draft` / `Pending Approval` / `Approved` / `Cancelled` status in the table or a supporting workflow; only an `Approved` event is applied to `employees`.
  - Two movements effective on the same day may not change the same employee field. Cancelling after the event has been applied creates a new corrective event rather than editing or deleting history.
  - A salary adjustment must be linked to an approved contract addendum or salary decision.
  - The two values `suspension` and `return_to_work` remain within `ck_employee_event_type` in the canonical schema but are **reserved**: in this delivery no flow and no endpoint can create an event of either type (see section 2.1).

---

#### [EMP-05] Employee documents
- **Goal**: securely store scanned certificates, CVs, diplomas and non-disclosure agreements.
- **Data**: the `employee_documents` table stores the document type (`document_type`), the file name, the secure storage path (`file_url`), the uploader and the upload date.
- **Security rules**:
  - Only files whose type, size and content have been checked are accepted; files are stored privately and access is granted through a time-limited URL, never a fixed public URL.
  - View and download rights are granted per document type; identity documents and contracts are accessible only to authorised HR staff or to the employee themselves.
  - Versions, the retention period and the expiry status of a document are recorded; physical deletion happens only after the retention period ends and the deletion is approved.

---

#### [EMP-06] Probation review & confirmation
- **Goal**: ensure every employee on probation is reviewed and formally decided upon before the probation contract expires, avoiding the legal risk of someone working without a contractual basis.
- **Actors**: line manager (the reviewer), HR Officer (coordination), HR Manager (approves the outcome).
- **Precondition**: the employee has `status = 'probation'` and an active contract with `contract_type = 'Probation'`.
- **Main flow**:
  1. When the probation contract is activated, the system creates a `probation_reviews` row with the status `pending` and a `review_due_date` set far enough before the contract expiry to complete the paperwork.
  2. The system reminds the line manager at the alert thresholds of `[CON-02]` (7 and 15 days before expiry).
  3. The line manager enters the overall score (`overall_score`), the strengths, the areas for improvement and a recommended outcome; the record moves to `in_review`.
  4. The HR Manager approves the outcome; the record moves to `decided` with `outcome`, `effective_date`, `decided_by` and `decided_at`.
  5. The system creates the matching `employee_events` row and links back through `probation_reviews.employee_event_id`:
     - `confirmed` → `event_type = 'probation_confirmation'`, together with a new permanent contract in `[CON-01]`.
     - `extended` → `event_type = 'probation_extension'`, with an addendum or a new probation contract.
     - `terminated` → `event_type = 'termination'`, triggering `[EMP-07]`.
- **Postcondition**: `employees.status` is only updated once the corresponding `employee_events` row is `approved` and its `effective_date` arrives; it is never edited from the review screen.
- **Rules and exceptions**:
  - A probation contract has exactly one review (`ux_probation_review_contract`). Extending probation creates a new contract or addendum and therefore a new review; the old one is never overwritten.
  - The `decided` status requires `outcome`, `decided_by`, `decided_at` and `effective_date` together (`ck_probation_decided`).
  - Only the line manager assigned as `reviewer_user_id` may enter the review; after `decided`, only the HR Manager may reopen it, with a reason recorded in the audit log.
  - `overall_score` takes a value from 0 to 5. A `terminated` outcome requires comments in `improvements` as supporting evidence.
  - The review list must flag in red any review past its `review_due_date` that is still `pending` or `in_review`. This is a legal risk, not merely a late process step.

---

#### [EMP-07] Offboarding & handover
- **Goal**: manage the full separation procedure: handing over work, recovering assets and accounts, settling what is owed, and retaining records for the required period.
- **Actors**: employee (submits notice), line manager (confirms the handover), HR Officer (coordination), HR Manager (approval), IT Admin, facilities admin, Finance.
- **Precondition**: the employee is `active` or `probation`.
- **Main flow**:
  1. The HR Officer creates an `offboarding_cases` row with `separation_type`, `notice_received_on`, `last_working_date`, the handover recipient (`handover_to_employee_id`) and the reason.
  2. The system compares `notice_received_on` against the notice period (`contracts.notice_period_days`) and warns if it is short, but does not block — the decision belongs to the HR Manager.
  3. The HR Manager approves; the case moves to `approved` and the system generates `offboarding_tasks` from templates by department, position and separation type:
     - `it`: recover the laptop and devices, lock the e-mail/Slack/Git/ERP accounts, transfer ownership of repositories and documents.
     - `admin`: collect the employee badge and parking pass, hand back the workspace.
     - `hr`: run the exit interview, issue the termination decision, close the insurance book, return original documents.
     - `manager`: confirm the handover of work, documents and contacts to the recipient.
     - `finance`: settle debts, advances and any remaining payments due on termination.
  4. Each owner marks their task complete; the case moves to `in_progress`.
  5. Once every task with `blocks_last_working_day = true` is `completed` and `final_settlement_status` reaches `paid` or `waived`, the HR Officer closes the case (`completed`).
  6. The system creates an `employee_events` row with `event_type = 'termination'` and `effective_date = last_working_date`; on that date `employees.status` becomes `terminated`.
- **Postcondition**: a `terminated` employee has no system access (their account is `users.status = 'disabled'`) and no longer appears in the active directory, but their records and documents are retained per `employee_documents.retention_until`.
- **Rules and exceptions**:
  - Each employee has at most one open case (`ux_offboarding_open_case`). Leaving and being rehired creates a new case; the old one is never reopened.
  - `handover_to_employee_id` must not be the leaving employee themselves (`ck_offboarding_handover_not_self`) and must be an `active` employee.
  - A case cannot be closed while a blocking task is outstanding. Overriding a blocking task requires HR Manager approval and a reason in the audit log.
  - The account must be locked exactly on `last_working_date`, not earlier, so the employee can finish the handover.
  - Remaining payments on termination (debts, advances, contractual entitlements) must be settled and reflected in `final_settlement_status` before the case is closed; the calculation belongs to the Payroll module and is out of scope.
  - Physical deletion of records only happens after the retention period ends and with approval, per `[EMP-05]`.

---

### MODULE 3: EMPLOYMENT CONTRACT MANAGEMENT (CONTRACTS)

#### [CON-01] Contract drafting & lifecycle
- **Goal**: manage every contract type recognised by the Vietnamese Labour Code.
- **Contract types (`contract_type`)**:
  - `Probation` (30 or 60 days)
  - `Fixed-Term` (12, 24 or 36 months)
  - `Indefinite`
  - `Internship`
  - `Service_Contract` (contractor / collaborator)
- **Business rules**:
  - Each contract has a unique contract/protocol number. The current schema uses `protocol_number`; standardising it to `contract_number` requires a migration and a unique constraint.
  - Record the start date (`start_date`), end date (`end_date`), salary (`salary`), notice period (`notice_period`) and contract status. The social-insurance salary, fixed allowances and the digital signature are data that require a schema extension before use.
  - Support storing the signed digitised file (PDF) or the DocuSign / VNPT-CA signature status.
  - An employee has one primary `Active`/`Executed` contract at a time, unless the HR Manager flags an exception; `end_date` must be after `start_date` for a fixed-term contract.

---

#### [CON-02] Expiry monitoring & automatic alerts
- **Goal**: prevent the legal risk of missing a statutory renewal deadline.
- **Alert rules**:
  - Probation contracts: alert **7 days** and **15 days** before expiry, so the manager can complete the probation review.
  - Fixed-term contracts: alert **30 days** and **45 days** before expiry.
- **System actions**:
  - Show an amber/red badge in the contract list of the responsible HR officer.
  - Notify the HR owner of the contract so they can issue a termination notice or prepare a new contract.

---

#### [CON-03] Contract addenda
- **Goal**: manage addenda that adjust contract terms without compromising the integrity of the original contract.
- **Actors**: HR Officer, HR Manager, employee (confirms or signs where applicable).
- **Flow**:
  1. The HR Officer picks an active original contract, chooses the type of change (salary, allowance, job title, location, term or another clause) and enters the effective date.
  2. The system creates the addendum in `Draft`, records the before/after data and submits it for approval.
  3. Once `Approved` and signed per policy, the addendum becomes `Effective`; the system creates the matching `employee_event` and updates the master data when the effective date arrives.
- **Rules**:
  - An addendum never edits the original contract; a change after it takes effect requires a new addendum version or a replacement addendum.
  - Each addendum needs a reference number, an author, an approver, the signed file and an audit trail in the canonical `contract_addenda` table; the runtime migration must still be created and reviewed before deployment.

---

### MODULE 4: IDENTITY & ACCESS

#### [ADM-01] Password sign-in & session management
- **Goal**: give a user an authenticated identity carrying the right permissions and data scope, without depending on an external identity provider.
- **Actors**: every internal user with an `active` account in `users`.
- **Flow**:
  1. The user enters an e-mail and a password. The e-mail is lowercased and trimmed before lookup, because `users.email` is UNIQUE.
  2. The system verifies the password against the hash in `user_credentials`. If the e-mail does not exist, the system **still** verifies against a dummy hash, so the response time does not reveal which accounts exist.
  3. On success: clear the failure counter, record `last_login_at`, resolve permissions and data scope with `user_roles ⋈ role_permissions`, and issue an access token (JWT HS256, 30 minutes by default) and a refresh token (14 days by default).
  4. The refresh token is a **single-use 256-bit random string**; the database stores only its SHA-256 digest. Refreshing revokes the old token with the reason `rotated`, links the successor, and **re-reads the authority from the database**.
  5. Signing out revokes the held refresh token; the access token stays alive until it expires — a known and accepted limitation of stateless bearer tokens.
- **Business rules**:
  - **Per-account lockout**: **5 consecutive failures** lock the account for **15 minutes** (`user_credentials.failed_attempts`, `locked_until`). The failure that reaches the threshold starts the window; further failures during the lockout do not extend it.
  - **No account disclosure**: an unknown e-mail, a missing credential and a wrong password all return `401` with the same `code`. A disabled account is only reported **after** the password has been verified.
  - **Token theft detection**: presenting an already revoked refresh token revokes **all** of that account's refresh tokens (`reuse_detected`) and forces a fresh sign-in.
  - **Forced password change**: when `must_change_password` is set, the issued session is **restricted** — no refresh token, an access token carrying no permissions, and only the change-password endpoint reachable.
  - **Self-service password change** requires the current password (a stolen access token alone must not be enough to take over the account); the new password must be at least 10 characters, combine at least 3 of the 4 character classes, not contain the local part of the e-mail, and differ from the old one. Changing it revokes all of that user's refresh tokens.
  - **Password storage**: PBKDF2-HMAC-SHA512, 210 000 iterations, a 128-bit per-password salt; the parameters live in the hash string, so raising the work factor needs no migration. Comparison is constant-time.
  - **Audit**: every sign-in, refresh, sign-out and password change — including the failures (`result = 'rejected'`) — writes to `audit_logs` in the same transaction as the state change. Passwords, hashes and token values are never written to the audit record.
- **Out of scope**: self-registration, forgotten password over e-mail, single sign-on (SSO/OIDC federation) and multi-factor authentication. Per-IP rate limiting belongs at the reverse proxy in front of the API, not in the application.

---

#### [ADM-02] Account & role administration
- **Goal**: let the Super Admin grant, revoke and bound access without touching the database directly.
- **Actors**: Super Admin (`admin.user.manage`); read-only roles use `admin.user.read` / `admin.role.read`.
- **Flow**:
  1. Create an account: enter the e-mail, display name and initial password, optionally link an `employee_id`, and give the list of roles with their data scope. The system generates `external_subject = local|<email>`, hashes the password and **always** sets `must_change_password`, because someone else knows the password.
  2. Grant and revoke roles: send the **complete target state**; any role absent from the list is removed. An empty list leaves an account that can sign in but has no authority.
  3. Disable an account: set `users.status = 'disabled'` and revoke all of that account's refresh tokens.
  4. Reset a password: set a temporary password, set `must_change_password`, clear the lockout counters and revoke all refresh tokens. The temporary password does **not** appear in the response; it must be handed over through a secure channel outside the system.
- **Business rules**:
  - **No self-administration**: a Super Admin may not disable, reset the password of, or change the roles of their own account. This is a double guard — against locking the whole organisation out of administration, and against escalating privileges without a second pair of eyes.
  - **Roles must exist in the catalogue**: `role_code` must exist in `roles` with `is_assignable = true`. A `department` scope requires `data_scope_id` to be an existing department; `self` and `organization` require `data_scope_id = 0` (exactly as `ck_user_roles_scope` demands).
  - **E-mail is unique** after lowercasing; a conflict returns `409` whether it is caught by the pre-check or by losing the race at the unique index.
  - **Employee linkage**: an `employee` is linked to exactly one account; an employee who already has one is rejected.
  - **Concurrency**: every modifying command requires `If-Match` on `users.version`. Changing roles uses that same version as the anchor, even though the data being changed lives in `user_roles`.
  - **The role → permission matrix is reference data** and is not editable through the API: it is deployed through [`database/seed_roles.sql`](../database/seed_roles.sql), so every change to authority goes through review and leaves a trace in version control.
- **Out of scope**: creating or editing roles and permissions through the API, temporary delegation, bulk account import, and synchronising accounts from HR to external systems.
