# 📋 INVEST User Stories — the Two Modules Delivered First (QLNS / NexusHR)

> **The authoritative requirements baseline**
> **Scope:** the two modules selected for the first delivery per the function map — **Core HR** (including the Contracts branch) and **Recruitment (ATS)**. Attendance & Leave has been taken out of scope; its stories are kept at [deferred/attendance_leave/user_stories_att.md](deferred/attendance_leave/user_stories_att.md).
> **Architecture mapping:** [Functional Specifications](functional_specifications.md) · [Use Cases](use_cases.md) · [Architecture (arc42 + C4)](architecture.md) · [Database Schema](../database/schema.sql) · [API Contract](api/openapi.yaml)
> **Story design principle:** the **INVEST** standard (*Independent, Negotiable, Valuable, Estimable, Small, Testable*).

> [!NOTE]
> On the function map, **Contract Management is a branch of Core HR** (a sibling of Employee Profiles, Organization Management and Employee Lifecycle). This document keeps the separate `CON-*` codes for traceability, but for delivery scope `EMP-*` and `CON-*` belong to the same module.

---

## Scope of This Delivery

The scope is settled from the `topdown-approach.png` function decomposition map and section 2 of the [README](../README.md): **only the leaf functions printed in bold under the Recruitment and Core HR pillars** (Employee Profiles, Organization Management, Contract Management, Employee Lifecycle) are in this delivery. Every story below sits inside exactly that set.

What is **not** in scope, and therefore has no story here:

- **Headcount & Budget Validation** — a requisition still has its approval flow, but the system checks neither headcount nor salary budget; the decision belongs to the HR Manager. `target_headcount`, `salary_min` and `salary_max` are declared data only.
- **Recruitment Channel Management** — publishing, updating and closing a posting remain, but only on a single default careers channel; there is no selection or management of multiple channels.
- **Organizational Chart** — there is no org-tree screen. The department hierarchy (`parent_department_id`), the parent-child relationship and the delete constraints all stay in scope (see `EMP-02.1`); only the tree rendering is dropped.
- **Suspension & Return to Work** — there is no suspend-and-return business flow. The `suspended` status and the corresponding `employee_events.event_type` values remain in the schema as *reserved* values that no endpoint can set in this delivery.
- **Reports & Analytics, Performance Management, Compensation & Benefits, Attendance & Leave Management** — these pillars are entirely out of scope.
- **System Administration** — only the two leaf functions Account Management and Roles/Permissions & Data Access Scope are in scope, under the `ADM-*` codes (section 5 of this document and [ADR-011](adr/011-in-house-identity.md)). Workflow, notification and integration configuration and the audit-trail screen remain out of scope; writing the audit log and the transactional outbox remain crosscutting requirements of every story.

---

## Status per Module

| Module | Story codes | Status |
| :--- | :--- | :--- |
| **Recruitment (ATS)** | `REC-01` … `REC-06` — 10 stories | Proposed |
| **Core HR — Profile, Organization, Lifecycle** | `EMP-01` … `EMP-07` — 10 stories | Proposed |
| **Core HR — Contracts** | `CON-01` … `CON-03` — 4 stories | Proposed |
| **Identity & Access** | `ADM-01` … `ADM-02` — 4 stories | Proposed |
| **Total** | **28 stories** | Proposed |

---

## Contents

- [1. Conventions & Story Structure](#1-conventions--story-structure)
- [2. Smart ATS Recruitment](#2-smart-ats-recruitment)
  - [REC-01: Job requisitions & postings](#rec-01-job-requisitions--postings)
  - [REC-02: CV intake & candidate screening](#rec-02-cv-intake--candidate-screening)
  - [REC-03: The Kanban pipeline & stage transitions](#rec-03-the-kanban-pipeline--stage-transitions)
  - [REC-04: Interview scheduling](#rec-04-interview-scheduling)
  - [REC-05: Candidate evaluation through scorecards](#rec-05-candidate-evaluation-through-scorecards)
  - [REC-06: Offers & the onboarding handoff](#rec-06-offers--the-onboarding-handoff)
- [3. Core HR — Employee Records & Lifecycle](#3-core-hr--employee-records--lifecycle)
  - [EMP-01: Employee directory & identity records](#emp-01-employee-directory--identity-records)
  - [EMP-02: Organization structure, positions & assignments](#emp-02-organization-structure-positions--assignments)
  - [EMP-03: Onboarding](#emp-03-onboarding)
  - [EMP-04: Employee movements & event management](#emp-04-employee-movements--event-management)
  - [EMP-05: Secure electronic document records](#emp-05-secure-electronic-document-records)
  - [EMP-06: Probation review & confirmation](#emp-06-probation-review--confirmation)
  - [EMP-07: Offboarding & handover](#emp-07-offboarding--handover)
- [4. Core HR — the Contract Management Branch](#4-core-hr--the-contract-management-branch)
  - [CON-01: Contract drafting, signing & lifecycle](#con-01-contract-drafting-signing--lifecycle)
  - [CON-02: Contract expiry monitoring & automatic alerts](#con-02-contract-expiry-monitoring--automatic-alerts)
  - [CON-03: Contract addenda](#con-03-contract-addenda)
- [5. Identity & Access](#5-identity--access)
  - [ADM-01: Sign-in & session management](#adm-01-sign-in--session-management)
  - [ADM-02: Account & role administration](#adm-02-account--role-administration)
- [6. Permission Matrix & Traceability](#6-permission-matrix--traceability)

---

## 1. Conventions & Story Structure

Every user story in this document has the same structure:
1. **Identifier (ID)**: matching the module code (`REC`, `EMP`, `CON`, `ADM`).
2. **Title**: a summary of the business action.
3. **User story statement**, in the form:
   > **As a** `[role/actor]`,
   > **I want** `[the action or capability]`,
   > **so that** `[the business value]`.
4. **Preconditions**: what must be true before the action can happen.
5. **Acceptance criteria (AC)**: written as behaviour scenarios in **Given – When – Then** (*Gherkin syntax*), covering both the success path and the exceptions.
6. **Technical & data constraints**: rules on security, RBAC, duplicate detection and data integrity.

---

## 2. Smart ATS Recruitment

### REC-01: Job requisitions & postings

#### [REC-01.1] Create a job requisition
- **Statement**:
  > **As a** hiring manager,
  > **I want** to raise a hiring request for my department with the headcount, job description and indicative salary,
  > **so that** HR management can approve the hiring plan.
- **Precondition**: the hiring manager is signed in, belongs to a valid department and holds `ROLE_INTERVIEWER` or `ROLE_DEPT_HEAD`.
- **Acceptance criteria**:
  - **Scenario 1: a draft requisition is created successfully**
    - **Given** the user has entered the job title, the department, a headcount > 0, a valid salary range (`salary_min <= salary_max`) and a hiring reason,
    - **When** the user chooses "Save draft",
    - **Then** the system stores the record in `job_postings` with status `draft`, generates a unique `job_code`, and confirms the save.
  - **Scenario 2: an error is raised on invalid data**
    - **Given** the user leaves the headcount empty, or enters `target_headcount <= 0` or `salary_min > salary_max`,
    - **When** the user saves or submits for approval,
    - **Then** the system blocks the save, outlines the offending fields in red and shows a clear warning.
- **Technical constraints**: `created_by` records the current user; the target table is `job_postings`. `target_headcount`, `salary_min` and `salary_max` are **declared data** for the approver to read and compare; the system validates only their internal consistency (`target_headcount > 0`, `salary_min <= salary_max`) and reconciles them against no headcount plan or budget.

---

#### [REC-01.2] Approve & publish a job requisition
- **Statement**:
  > **As an** HR Manager,
  > **I want** to review the pending requisitions and hand them to a recruiter to publish,
  > **so that** a position only reaches the market after an authorised person agrees, with that decision clearly recorded.
- **Precondition**: the requisition is in `pending_approval`; the user holds `ROLE_HR_MGR`.
- **Scope note**: approval is a **business decision of the HR Manager**. The system checks neither headcount nor salary budget; it presents the requisition data, records the decision and the reason.
- **Acceptance criteria**:
  - **Scenario 1: the requisition is approved**
    - **Given** the requisition is in `pending_approval`,
    - **When** the HR Manager presses "Approve",
    - **Then** the status becomes `approved`, the system records `approved_by` and notifies the responsible recruiter.
  - **Scenario 2: rejection requires a reason**
    - **Given** the HR Manager reviews the requisition and decides not to open the position,
    - **When** they press "Reject" without entering a reason,
    - **Then** the system blocks the action and requires: *"A rejection reason is mandatory"*.
    - **When** a reason has been entered and the rejection confirmed,
    - **Then** the status becomes `rejected` (or returns to `draft`), with the reason stored in the audit log.
  - **Scenario 3: publishing**
    - **Given** the requisition is `approved`,
    - **When** the recruiter presses "Publish",
    - **Then** the status becomes `active`, the posting appears on the company's **default careers channel** and starts accepting applications; there is no channel selection or configuration step.

---

### REC-02: CV intake & candidate screening

#### [REC-02.1] CV intake & file safety
- **Statement**:
  > **As a** recruiter (or a candidate applying online),
  > **I want** to upload a CV file (PDF/DOCX),
  > **so that** the candidate's application enters secure storage and is ready for review.
- **Acceptance criteria**:
  - **Scenario 1: a valid file is uploaded**
    - **Given** the CV file is `.pdf`, `.doc` or `.docx` and is at most 10 MB,
    - **When** the user uploads it,
    - **Then** the system scans it for malware, stores it in private object storage, creates a `resumes` row and links it to `candidates`.
  - **Scenario 2: an oversized or wrongly typed file is rejected**
    - **Given** the uploaded file has an `.exe` or `.zip` extension, or exceeds 10 MB,
    - **When** the user presses upload,
    - **Then** the system refuses the file, reports the specific error and creates no database record at all.

---

#### [REC-02.2] Deduplication & parsing
- **Statement**:
  > **As a** recruiter,
  > **I want** the system to check automatically for duplicate candidate details (e-mail/phone) and extract the basic fields,
  > **so that** one candidate does not end up with several fragmented records in the database.
- **Acceptance criteria**:
  - **Scenario 1: an entirely new candidate**
    - **Given** the candidate's e-mail and phone have never appeared in `candidates`,
    - **When** a new application is created,
    - **Then** the system creates a new `candidates` row and a new `application` linked to the posting.
  - **Scenario 2: the candidate already exists**
    - **Given** the e-mail or the phone matches an existing candidate,
    - **When** an application is submitted for a new posting,
    - **Then** the system creates no new candidate record but attaches the new `application` to the existing `candidate_id`, and shows the previous application history.

---

### REC-03: The Kanban pipeline & stage transitions

#### [REC-03.1] Manage applications on a visual Kanban board
- **Statement**:
  > **As a** recruiter,
  > **I want** to see candidates across the six standard recruitment columns: `Applied`, `Screening`, `Interview 1`, `Interview 2`, `Offer`, `Hired`,
  > **so that** I can grasp progress and the distribution of candidates per stage at a glance.
- **Acceptance criteria**:
  - **Scenario 1: stages are distributed correctly**
    - **Given** a position has 20 applications spread across the stages,
    - **When** the recruiter opens that position's pipeline page,
    - **Then** the candidate cards are grouped into the right columns by `current_stage`, and each column shows the total count and the average match score.
  - **Scenario 2: quick filtering**
    - **Given** a large candidate list,
    - **When** the recruiter searches by name, skill keyword or score range,
    - **Then** the Kanban board keeps only the matching cards, in under a second.

---

#### [REC-03.2] Advance an application safely
- **Statement**:
  > **As a** recruiter,
  > **I want** to move a candidate forward one stage, or mark them rejected with a reason,
  > **so that** the interview progress is recorded accurately and the data stays consistent.
- **Acceptance criteria**:
  - **Scenario 1: advancing one stage (happy path)**
    - **Given** the candidate is in stage `applied` and has passed screening,
    - **When** the recruiter issues the advance command (`POST /api/v1/recruitment/applications/{applicationId}/advance`),
    - **Then** the candidate moves to `screening`, and the system records the transition time and actor and increments `version` to guard against concurrent overwrites.
  - **Scenario 2: skipping a stage is blocked**
    - **Given** the candidate is in stage `applied`,
    - **When** a request tries to move them straight to `offer` without the interview rounds,
    - **Then** the system refuses, returning `400 Bad Request` with a message stating the valid transition flow was violated.
  - **Scenario 3: rejecting with a reason**
    - **Given** the candidate did not pass one of the rounds,
    - **When** the recruiter chooses "Reject" and picks a reason (for example `Failed Technical` or `Salary Expectation Mismatch`),
    - **Then** the candidate's status becomes `rejected`, an audit record is written and the templated thank-you e-mail is triggered.

---

### REC-04: Interview scheduling

#### [REC-04.1] Schedule an interview and detect conflicts
- **Statement**:
  > **As a** recruiter,
  > **I want** to schedule an interview between the candidate and the panel,
  > **so that** the time is arranged accurately and the calendar invitation goes out automatically, without clashing on a room or an interviewer's time.
- **Acceptance criteria**:
  - **Scenario 1: booking succeeds when there is no conflict**
    - **Given** the interviewer and the candidate are both free in the intended window,
    - **When** the recruiter picks the start and end time and the format (an online meeting link or an on-site room),
    - **Then** the system stores a row in `interviews` with status `scheduled` and sends an e-mail with an `.ics` calendar file to each party.
  - **Scenario 2: a scheduling conflict is detected**
    - **Given** interviewer A already has another interview from 14:00 to 15:00,
    - **When** the recruiter tries to book interviewer A at 14:30 the same day,
    - **Then** the system warns about the conflict: *"The interviewer already has a clashing booking: 14:00 – 15:00"*, and refuses to save until a different person or time is chosen.

---

### REC-05: Candidate evaluation through scorecards

#### [REC-05.1] Submit a standardised interview scorecard
- **Statement**:
  > **As an** interviewer,
  > **I want** to score the candidate against quantified criteria (1–5) with detailed comments after the interview,
  > **so that** the hiring recommendation is transparent, objective and evidence-based.
- **Acceptance criteria**:
  - **Scenario 1: a valid evaluation is submitted**
    - **Given** the interviewer is assigned to a completed interview,
    - **When** they enter scores (1–5) for technical competency, soft skills, problem solving and culture fit, and pick a recommendation (`Strong Hire`, `Hire`, `Hold`, `No Hire`),
    - **Then** the system stores the result in `evaluations`, computes the weighted `overall_score` automatically, and locks the record against casual editing.
  - **Scenario 2: blind evaluation**
    - **Given** two interviewers took part in the same interview,
    - **When** interviewer 1 has not yet submitted their scorecard,
    - **Then** the system hides interviewer 2's scores and comments entirely, so the judgement stays independent and unbiased.

---

### REC-06: Offers & the onboarding handoff

#### [REC-06.1] Draft & approve an offer letter
- **Statement**:
  > **As a** recruiter,
  > **I want** to create an offer letter with the salary, allowances and start date, and send it to the HR Manager for approval before it reaches the candidate,
  > **so that** the compensation package stays within the company's salary bands and policy.
- **Acceptance criteria**:
  - **Scenario 1: the offer is created and approved**
    - **Given** the candidate has passed the final interview round,
    - **When** the recruiter drafts the offer in `draft` and submits it, and the HR Manager confirms "Approve",
    - **Then** the offer moves to `approved` (or `sent`), and the system generates the confirmation link sent to the candidate's e-mail with a response deadline.
  - **Scenario 2: the offer expires automatically**
    - **Given** the offer has an `expiration_date` of 20 September,
    - **When** 20 September passes with no response from the candidate,
    - **Then** the system moves the offer to `expired`, and the candidate cannot accept unless HR extends it.

---

#### [REC-06.2] Convert a hired candidate into an employee record
- **Statement**:
  > **As an** HR Officer,
  > **I want** the system to create the new employee record and the onboarding checklist automatically as soon as the candidate accepts the offer,
  > **so that** manual data entry is avoided and no onboarding task is ever missed.
- **Acceptance criteria**:
  - **Scenario 1: automatic, idempotent conversion (happy path)**
    - **Given** the candidate confirms the offer online,
    - **When** the system records the status `accepted`,
    - **Then** a single safe database transaction:
      1. creates a new `employees` row with an auto-generated employee code (for example `EMP-00128`),
      2. transfers the information from `candidates` to `employees`,
      3. creates a draft probation contract in `contracts`,
      4. creates the onboarding checklist in `onboarding_tasks`.
  - **Scenario 2: no duplicates on retry (idempotency check)**
    - **Given** the candidate has already been converted successfully,
    - **When** the acceptance event is processed again (through network lag or an API retry) — whether with the same `Idempotency-Key` or a different one,
    - **Then** the system recognises the existing record through `employees.source_application_id`, returns an `OfferResponseResult` with `replayed = true`, and creates no further employee, contract or checklist.
  - **Scenario 3: two concurrent acceptances**
    - **Given** two `accept` requests for the same offer arrive at the server almost simultaneously,
    - **When** both are processed,
    - **Then** exactly one creates the data; the other receives the same result with `replayed = true`; two `employees` rows with the same `source_application_id` never exist.
  - **Scenario 4: the offer is no longer open**
    - **Given** the offer is `expired`, `declined` or `cancelled`,
    - **When** the candidate presses accept,
    - **Then** the system returns `409` with the code `recruitment.offer_not_open` and writes nothing.
  - **Scenario 5: the token does not match the offer**
    - **Given** a valid `X-Offer-Token` belonging to a different offer,
    - **When** the response endpoint is called,
    - **Then** the system returns `401` without revealing whether the target offer exists.
  - **Scenario 6: the new employee has no work e-mail yet**
    - **Given** the candidate has only a personal e-mail,
    - **When** the handoff creates the `employees` row,
    - **Then** `work_email` is left empty, `personal_email` is copied from the candidate and `status = 'probation'`; the system blocks the move to `active` until IT issues a work e-mail (`ck_employee_active_requires_work_email`).
- **Technical constraints**: every write to `offers`, `applications`, `application_stage_events`, `employees`, `contracts`, `onboarding_tasks`, `audit_logs` and `outbox_messages` happens in **one transaction**. `employee_code` comes from `employee_code_seq` in the format `EMP-00001`. Notifications and account provisioning go through the outbox and are only dispatched after the commit.

---

## 3. Core HR — Employee Records & Lifecycle

### EMP-01: Employee directory & identity records

#### [EMP-01.1] Search & filter the employee directory
- **Statement**:
  > **As an** HR Officer or a manager,
  > **I want** to search employees by name, employee code, e-mail, department, position and employment status,
  > **so that** I can look up personnel information quickly.
- **Acceptance criteria**:
  - **Scenario 1: full-text search**
    - **Given** the database holds thousands of employees,
    - **When** the user types "Nguyễn Văn" or "EMP-0012",
    - **Then** the system returns results in under 500 ms, showing the name, job title, department and work e-mail.
  - **Scenario 2: sensitive information is scoped**
    - **Given** an ordinary employee browses the directory,
    - **When** they open a colleague's profile,
    - **Then** the system shows only public employment information (department, job title, work e-mail) and absolutely hides the national ID, salary, bank details and home address.

---

#### [EMP-01.2] Controlled self-service profile update
- **Statement**:
  > **As an** employee,
  > **I want** to update my own personal details (phone number, temporary address, emergency contact),
  > **so that** my contact information stays accurate without troubling the HR department.
- **Acceptance criteria**:
  - **Scenario 1: permitted fields are updated**
    - **Given** the employee is signed in on their own profile page,
    - **When** they edit their mobile number or temporary address and press save,
    - **Then** the system validates the format, updates `employees` and writes an audit record.
  - **Scenario 2: core employment fields cannot be self-edited**
    - **Given** the employee is on their personal information screen,
    - **When** they look at salary, department, job title, employee code and start date,
    - **Then** those fields are read-only; any attempt to change them through the API is refused with `403 Forbidden`.

---

### EMP-02: Organization structure, positions & assignments

#### [EMP-02.1] Manage the organization structure, positions & assignments
- **Statement**:
  > **As an** HR Manager (with an HR Officer to help maintain the catalogues),
  > **I want** to declare and maintain the department catalogue as a parent-child hierarchy and the job title catalogue with grades, and then assign employees to a department, a position and a direct manager,
  > **so that** all HR, contract and recruitment data references a single consistent organisation structure with a clear data scope.
- **Precondition**: the user holds `ROLE_HR_MGR` (to maintain the catalogues) or `ROLE_HR_OFFICER` (to update assignments within their granted data scope).
- **Acceptance criteria**:
  - **Scenario 1: declaring a department in the hierarchy**
    - **Given** the HR Manager enters the department code, name, parent department (`parent_department_id`) and cost centre,
    - **When** they save,
    - **Then** the system creates a `departments` row with a system-wide unique code linked to the right parent; leaving the parent empty means a top-level unit.
  - **Scenario 2: integrity when managing departments**
    - **Given** a department still has employees, child departments or open postings,
    - **When** an administrator tries to delete it,
    - **Then** the system refuses with `409 Conflict` and asks for the employees to be moved, the child departments handled and the postings closed first.
  - **Scenario 3: cycles in the hierarchy are blocked**
    - **Given** department A is a parent (directly or indirectly) of department B,
    - **When** a user sets A's `parent_department_id` to B,
    - **Then** the system refuses, citing the cycle-prevention rule; the department hierarchy is always acyclic.
  - **Scenario 4: declaring a position and its grade**
    - **Given** the HR Manager declares a new job title with a code, a name and a `level`,
    - **When** they save,
    - **Then** the position is created in `positions` with a unique code and is ready to be assigned to employees, contracts and requisitions; renaming a position does not break the records referencing it.
  - **Scenario 5: assigning an employee and setting the reporting line**
    - **Given** an employee must be assigned to a department, a position and a direct manager,
    - **When** HR updates the assignment through a dated movement decision (see `[EMP-04.1]`),
    - **Then** the employee record references the right `department_id`, `position_id` and `manager_id`; the direct manager must not be the employee themselves, and the reporting chain must not form a cycle.
  - **Scenario 6: a data scope derived from the organisation structure**
    - **Given** a user with `data_scope_type = 'department'`,
    - **When** they look up the employee list or the organisation catalogues,
    - **Then** the system returns only their own department and its children; anything outside that scope is filtered out server-side.
- **Technical constraints**: `departments` (`parent_department_id`, a unique code, the cycle-prevention and delete constraints), `positions` and `employees`; every catalogue change uses optimistic concurrency through `If-Match`/ETag and writes an audit record in the same transaction. This delivery has **no** org-tree screen or endpoint.

---

### EMP-03: Onboarding

#### [EMP-03.1] Track & assign onboarding tasks
- **Statement**:
  > **As an** HR Officer,
  > **I want** to track the progress of onboarding tasks for a new hire (laptop, e-mail account, desk setup, contract signing, induction training),
  > **so that** their first day is smooth and professional.
- **Acceptance criteria**:
  - **Scenario 1: work is distributed to the responsible teams automatically**
    - **Given** a new employee record has been created from a successful offer,
    - **When** the system triggers the onboarding process,
    - **Then** the corresponding tasks are created in `onboarding_tasks` and assigned to the right owner (IT: create the e-mail and issue the machine; facilities: issue the access badge; HR: prepare the contract).
  - **Scenario 2: an overdue onboarding task is flagged**
    - **Given** the task "Prepare laptop" was due one day before the start date but is still `pending`,
    - **When** the automatic scan runs,
    - **Then** a red warning flag appears on the onboarding dashboard and a reminder e-mail is sent to the responsible IT owner.

---

### EMP-04: Employee movements & event management

#### [EMP-04.1] Create & approve an employee movement
- **Statement**:
  > **As an** HR Manager,
  > **I want** to create and approve employee movement decisions (promotion, departmental transfer, salary increase, termination) with an explicit effective date,
  > **so that** the employment history stays transparent and the new position or department applies automatically on the right date.
- **Acceptance criteria**:
  - **Scenario 1: creating a transfer decision**
    - **Given** employee A currently belongs to Engineering,
    - **When** HR proposes a move to Product effective 1 October 2026 and the authorised person approves it,
    - **Then** a row is created in `employee_events` with status `approved`, storing `old_department_id`, `new_department_id` and `effective_date`.
  - **Scenario 2: applied automatically on the effective date**
    - **Given** the transfer has been approved with an effective date of today,
    - **When** the background worker runs at the start of the day,
    - **Then** the employee's row in `employees` is updated to the new department with no manual step.
  - **Scenario 3: movement history is never edited or deleted**
    - **Given** a movement event already took effect in the past,
    - **When** a user tries to edit or delete it,
    - **Then** the system blocks the action; any change must be made through a new compensating event.

---

### EMP-05: Secure electronic document records

#### [EMP-05.1] Upload & control access to HR documents
- **Statement**:
  > **As an** HR Officer,
  > **I want** to store scanned HR documents securely (national ID, degree certificates, professional certificates, NDAs),
  > **so that** paper records are fully digitised and retrievable quickly during an audit.
- **Acceptance criteria**:
  - **Scenario 1: private storage**
    - **Given** HR uploads a scan of an employee's degree certificate,
    - **When** the file is stored in `employee_documents`,
    - **Then** the file is private and the download link is a signed URL with a short lifetime (15 minutes, say), never a fixed public path.
  - **Scenario 2: access control on sensitive documents**
    - **Given** the document is of type `Contract` or `Disciplinary Record`,
    - **When** a user without `ROLE_HR_OFFICER` or `ROLE_HR_MGR` tries to reach the document link,
    - **Then** the system returns `403 Forbidden` and logs the unauthorised access attempt.

---

### EMP-06: Probation review & confirmation

#### [EMP-06.1] Submit a probation review
- **Statement**:
  > **As a** line manager,
  > **I want** to enter the employee's probation review with their strengths, areas for improvement and a recommended outcome,
  > **so that** HR has a basis for signing a permanent contract, extending probation or terminating before the probation contract expires.
- **Precondition**: the employee has `status = 'probation'`; a `probation_reviews` row exists in `pending` or `in_review`; the user is that review's `reviewer_user_id`.
- **Acceptance criteria**:
  - **Scenario 1: the review is entered successfully**
    - **Given** the review is `pending` and the user is the assigned reviewer,
    - **When** the line manager enters an `overall_score` between 0 and 5, the strengths, the areas for improvement and a recommended outcome,
    - **Then** the review moves to `in_review`, the system records who entered it and when, and notifies the HR Manager.
  - **Scenario 2: an unassigned user is blocked**
    - **Given** the user is neither the review's `reviewer_user_id` nor an HR role,
    - **When** they call the review API,
    - **Then** the system returns `403 Forbidden` and writes nothing.
  - **Scenario 3: a termination recommendation requires comments**
    - **Given** the line manager recommends `terminated`,
    - **When** they leave the areas-for-improvement field empty,
    - **Then** the system blocks the save and requires comments as supporting evidence.
  - **Scenario 4: an overdue review is flagged**
    - **Given** today is past `review_due_date` and the review is still `pending` or `in_review`,
    - **When** HR opens the employee lifecycle dashboard,
    - **Then** the review appears in the red warning block with the number of days overdue, classified as a legal risk rather than a late process step.
- **Technical constraints**: `probation_reviews`; `ux_probation_review_contract` guarantees one review per probation contract; `overall_score` is bounded to 0–5 by a CHECK constraint.

---

#### [EMP-06.2] Decide the probation outcome and sync the employee status
- **Statement**:
  > **As an** HR Manager,
  > **I want** to approve the probation outcome and have the system create the corresponding employee event,
  > **so that** the employee's status and contract always match the approved decision, without editing several places by hand.
- **Precondition**: the review is `in_review`; the user holds `ROLE_HR_MGR`.
- **Acceptance criteria**:
  - **Scenario 1: confirming the employee**
    - **Given** the review is `in_review` with a `confirmed` recommendation,
    - **When** the HR Manager approves it and enters the effective date,
    - **Then** the review moves to `decided` with `outcome`, `decided_by`, `decided_at` and `effective_date`; the system creates an `employee_events` row with `event_type = 'probation_confirmation'` and links it through `probation_reviews.employee_event_id`.
  - **Scenario 2: a decision missing mandatory data is blocked**
    - **Given** the HR Manager approves without entering an effective date,
    - **When** the request is sent,
    - **Then** the system refuses with `422`; the `ck_probation_decided` constraint guarantees no `decided` review exists without `outcome`, `decided_by`, `decided_at` and `effective_date`.
  - **Scenario 3: the employee status does not change before the effective date**
    - **Given** a `confirmed` decision with a future `effective_date`,
    - **When** the employee record is inspected immediately after approval,
    - **Then** `employees.status` is still `probation`; it becomes `active` only when the effective date arrives and the event is applied.
  - **Scenario 4: a termination outcome triggers the offboarding flow**
    - **Given** the outcome is `terminated`,
    - **When** the review moves to `decided`,
    - **Then** the system creates an `employee_events` row with `event_type = 'termination'` and opens the corresponding `offboarding_cases` per `[EMP-07.1]`.
- **Technical constraints**: creating the event and updating the review happen in one transaction together with the audit record; repeating the approval must be idempotent — a review produces at most one `employee_events` row.

---

### EMP-07: Offboarding & handover

#### [EMP-07.1] Create an offboarding case and generate the handover checklist
- **Statement**:
  > **As an** HR Officer,
  > **I want** to create an offboarding case with the last working date and the handover recipient, and have the system generate the task list,
  > **so that** nothing is missed in recovering assets, locking accounts and settling what is owed when someone leaves.
- **Precondition**: the employee is `active` or `probation`; the user holds `ROLE_HR_OFFICER` or `ROLE_HR_MGR`.
- **Acceptance criteria**:
  - **Scenario 1: the case is created and the checklist generated**
    - **Given** the HR Officer enters `separation_type`, `last_working_date`, the handover recipient and the reason,
    - **When** the HR Manager approves the case,
    - **Then** the case moves to `approved` and the system generates `offboarding_tasks` from templates across the five groups `it`, `admin`, `hr`, `manager` and `finance`, each with an owner and a due date.
  - **Scenario 2: a second case is blocked while one is open**
    - **Given** the employee already has a case in `draft`, `pending_approval`, `approved` or `in_progress`,
    - **When** a user creates another case for the same employee,
    - **Then** the system returns `409 Conflict`; `ux_offboarding_open_case` guarantees two open cases cannot coexist.
  - **Scenario 3: a short notice period is flagged**
    - **Given** `notice_received_on` is fewer than `contracts.notice_period_days` before `last_working_date`,
    - **When** the HR Officer saves the case,
    - **Then** the system shows a clear notice-period warning but **still saves**, because the decision belongs to the HR Manager; the warning is written to the audit log.
  - **Scenario 4: handing over to oneself is blocked**
    - **Given** the HR Officer picks the leaving employee as `handover_to_employee_id`,
    - **When** they save the case,
    - **Then** the system refuses; `ck_offboarding_handover_not_self` blocks it at the database layer.
  - **Scenario 5: regenerating the checklist creates no duplicates**
    - **Given** a case is re-approved after being cancelled and restored,
    - **When** the checklist generation runs a second time,
    - **Then** no task is duplicated; `ux_offboarding_task_template` guarantees each `template_key` appears once per case.
- **Technical constraints**: `offboarding_cases`, `offboarding_tasks` and `employee_events`; creating the case and generating the tasks happen in one transaction.

---

#### [EMP-07.2] Complete the handover and close the case
- **Statement**:
  > **As an** HR Officer,
  > **I want** the offboarding case to be closeable only once every blocking task is done and the settlement is resolved,
  > **so that** the company loses no assets, leaves no active account behind, and the employee receives everything they are owed.
- **Precondition**: the case is `in_progress`.
- **Acceptance criteria**:
  - **Scenario 1: closing is blocked while a blocking task is outstanding**
    - **Given** at least one `offboarding_tasks` row has `blocks_last_working_day = true` and `status <> 'completed'`,
    - **When** the HR Officer presses close,
    - **Then** the system refuses and lists the specific blocking tasks.
  - **Scenario 2: closing is blocked until the settlement is resolved**
    - **Given** every blocking task is complete but `final_settlement_status` is still `pending` or `calculated`,
    - **When** the HR Officer presses close,
    - **Then** the system refuses and asks for the settlement status to be moved to `paid` or `waived`.
  - **Scenario 3: the case closes and the account is disabled**
    - **Given** every condition is met,
    - **When** the HR Officer closes the case,
    - **Then** the case moves to `completed` with a `completed_at`; on `last_working_date`, `employees.status` becomes `terminated` and `users.status` becomes `disabled`.
  - **Scenario 4: the account is never locked before the last working date**
    - **Given** the case is `completed` but `last_working_date` is still in the future,
    - **When** the employee signs in during the remaining days,
    - **Then** they still have access to finish the handover; the account is disabled exactly on the last working date.
  - **Scenario 5: records are retained after departure**
    - **Given** the employee is `terminated`,
    - **When** the HR Officer looks up their documents,
    - **Then** the documents remain accessible until `retention_until`; the employee no longer appears in the active directory.
- **Technical constraints**: `ck_offboarding_completed` guarantees a `completed` case always has a `completed_at`; overriding a blocking task requires `ROLE_HR_MGR` and a reason written to the audit log.

---

## 4. Core HR — the Contract Management Branch

> On the function map this group of stories is a **branch of Core HR**, not a module of its own. The `CON-*` codes are kept separate to trace back to `contracts` and `contract_addenda`.

### CON-01: Contract drafting, signing & lifecycle

#### [CON-01.1] Draft & create an employment contract
- **Statement**:
  > **As an** HR Officer (C&B, responsible for contracts),
  > **I want** to create an employment contract from the standard templates of the Vietnamese Labour Code (probation, fixed-term, indefinite),
  > **so that** the legal employment relationship with the worker is established correctly.
- **Acceptance criteria**:
  - **Scenario 1: drafting a valid fixed-term contract**
    - **Given** the employee is working and has no active permanent contract,
    - **When** HR picks the `fixed_term` type and enters the contract number (`contract_number`), start date, end date (`start_date < end_date`) and salary,
    - **Then** the system stores the contract in `contracts` with status `draft`, checking that the contract number is unique system-wide.
  - **Scenario 2: a fixed-term contract requires an end date**
    - **Given** HR creates a `probation` or `fixed_term` contract,
    - **When** they leave `end_date` empty or enter `end_date <= start_date`,
    - **Then** the system reports: *"A fixed-term contract must have an end date later than its start date"*.
  - **Scenario 3: two overlapping primary contracts are blocked**
    - **Given** employee B already has a contract in `active`,
    - **When** HR tries to activate another contract whose effective period overlaps,
    - **Then** the system warns about the overlap and requires the previous contract to be ended first.

---

#### [CON-01.2] Execute & activate a contract
- **Statement**:
  > **As an** HR Officer or HR Manager,
  > **I want** to upload the contract file signed by both parties, or record that it was signed electronically,
  > **so that** the contract formally enters force (`active`).
- **Acceptance criteria**:
  - **Scenario 1: activation with valid signature evidence**
    - **Given** the contract is in `approved`,
    - **When** the signed scan is uploaded or the e-signature flow completes,
    - **Then** the contract moves to `active`, recording who activated it and when.
  - **Scenario 2: an employee views their own contract**
    - **Given** the employee is signed in,
    - **When** they open "My contracts",
    - **Then** they see only their own contracts and can view and download the signed scan.

---

### CON-02: Contract expiry monitoring & automatic alerts

#### [CON-02.1] Multi-threshold automated expiry alerts
- **Statement**:
  > **As an** HR Officer responsible for C&B,
  > **I want** the system to scan for contracts nearing expiry and alert automatically (7 and 15 days ahead for probation; 30 and 45 days ahead for permanent contracts),
  > **so that** the probation review, the renewal or the statutory termination notice happens on time.
- **Acceptance criteria**:
  - **Scenario 1: the scan surfaces alerts on the dashboard**
    - **Given** three probation contracts will expire within the next 7 days,
    - **When** HR opens the contract management dashboard,
    - **Then** the system lists those three contracts in the "Expiry alert (amber/red)" block with the days remaining.
  - **Scenario 2: automatic notification by e-mail and in-app**
    - **Given** the daily scan runs at 06:00,
    - **When** it finds a contract reaching an alert threshold (30 days before expiry),
    - **Then** the system e-mails the responsible HR officer and the employee's direct manager, attaching the renewal review form.
  - **Scenario 3: no duplicate alert on the same day**
    - **Given** the 30-day alert was already sent that morning,
    - **When** the scan runs again in the afternoon,
    - **Then** the system checks the notification log and does not resend for the same threshold.

---

### CON-03: Contract addenda

#### [CON-03.1] Create an addendum adjusting contract terms
- **Statement**:
  > **As an** HR Officer,
  > **I want** to create a contract addendum when the salary, allowance, job title or work location changes, without compromising the integrity of the original contract,
  > **so that** the change of legal terms is traceable and the employment record stays legally sound.
- **Acceptance criteria**:
  - **Scenario 1: a salary adjustment addendum is created**
    - **Given** the original contract is `active`,
    - **When** HR creates an addendum with its number, effective date, change type `Salary_Adjustment` and the new salary,
    - **Then** the system creates a `contract_addenda` row linked to the `contract_id`, storing the values before and after the change.
  - **Scenario 2: the original contract is immutable**
    - **Given** the addendum has been approved and is effective,
    - **When** the original contract is inspected,
    - **Then** its original terms are unchanged; the system records only the extension relationship to the addendum.
  - **Scenario 3: the employee movement is synchronised when the addendum takes effect**
    - **Given** an addendum adjusting the job title has been signed,
    - **When** it moves to `effective`,
    - **Then** the system creates the matching `employee_event` to synchronise the employee's new job title.

---

## 5. Identity & Access

> This module was added when the decision was taken **not** to use an external Identity Provider ([ADR-011](adr/011-in-house-identity.md)). It is the precondition of every other story: with no authenticated actor, no story can check a permission or a data scope.

### ADM-01: Sign-in & session management

#### [ADM-01.1] Sign in with e-mail & password
- **Statement**:
  > **As an** internal QLNS user,
  > **I want** to sign in with my work e-mail and password,
  > **so that** I reach exactly the functions and data scope my role has been granted.
- **Precondition**: the account exists in `users` with `status = 'active'` and has a `user_credentials` row.
- **Acceptance criteria**:
  - **Scenario 1: signing in successfully**
    - **Given** the account `hr.manager@qlns.local` is `active` and holds `ROLE_HR_MGR` at organization scope,
    - **When** the user submits the correct e-mail and password,
    - **Then** the system returns an access token with the role's full permissions, `dataScope = 'organization'` and a refresh token,
    - **And** `user_credentials.failed_attempts` is reset to 0 and `last_login_at` is recorded,
    - **And** an `audit_logs` row with `action = 'admin.auth.sign_in'` and `result = 'succeeded'` is written in the same transaction.
  - **Scenario 2: a wrong password reveals nothing about which accounts exist**
    - **Given** someone submits the right e-mail with a wrong password, or an e-mail that does not exist at all,
    - **When** the system handles the request,
    - **Then** both cases return `401` with the same `code = 'admin.auth.invalid_credentials'` and the same message,
    - **And** for an unknown e-mail the system **still** performs a password verification against a dummy hash, so the response time does not differ,
    - **And** an `audit_logs` row with `result = 'rejected'` is written, including the attempted e-mail and the client trace.
  - **Scenario 3: lockout after repeated failures**
    - **Given** the account has had 4 consecutive wrong passwords,
    - **When** the user gets it wrong a fifth time,
    - **Then** `failed_attempts = 5` and `locked_until = now + 15 minutes`,
    - **And** every sign-in inside that window returns `401` with `code = 'admin.auth.account_locked'` and a `retryAfterSeconds`, **without** verifying the password,
    - **And** further failures during the lockout do **not** extend the window.
  - **Scenario 4: the account has been disabled**
    - **Given** the account has `status = 'disabled'`,
    - **When** the user submits the **correct** password,
    - **Then** the system returns `401` with `code = 'admin.auth.account_disabled'`,
    - **But** if the password is wrong it returns `invalid_credentials` like any other case — that the account is disabled is only disclosed to someone who knows the password.
  - **Scenario 5: an account with no roles**
    - **Given** an `active` account with no `user_roles` rows,
    - **When** the user signs in,
    - **Then** sign-in **succeeds** but the permission list is empty,
    - **And** every business endpoint returns `403` (deny by default), not `401`.
- **Technical constraints**: passwords are stored with PBKDF2-HMAC-SHA512, 210 000 iterations and a 128-bit per-password salt; the parameters live inside the hash string. Comparison is constant-time. Permissions and data scope are resolved from `user_roles ⋈ role_permissions` and are **never** taken from the client. The audit record never contains a password, a hash or a token value.

#### [ADM-01.2] Keep & end a session safely
- **Statement**:
  > **As a** user working in the system,
  > **I want** my session to continue without signing in again every 30 minutes,
  > **so that** my work is not interrupted, while I can still sign out to end the session immediately when I need to.
- **Precondition**: the user holds a refresh token that has neither expired nor been revoked.
- **Acceptance criteria**:
  - **Scenario 1: refreshing the session re-reads the authority**
    - **Given** a valid refresh token,
    - **When** the client calls refresh,
    - **Then** the old token is revoked with `revoked_reason = 'rotated'` and linked to its successor,
    - **And** the new access token is issued with permissions **re-read from the database**, so a role revoked a moment ago no longer appears.
  - **Scenario 2: detecting a stolen token**
    - **Given** a refresh token that has already been used (revoked with the reason `rotated`),
    - **When** someone presents that exact token again,
    - **Then** the system revokes **all** of that account's active refresh tokens with `revoked_reason = 'reuse_detected'`,
    - **And** returns `401`; both the real user and the attacker must sign in again.
  - **Scenario 3: sign-out is not a token-probing tool**
    - **Given** any refresh token — valid, revoked or non-existent,
    - **When** the client calls sign-out,
    - **Then** the system always returns `204`,
    - **And** only a valid token is actually revoked (`revoked_reason = 'logout'`).
  - **Scenario 4: the account is disabled mid-session**
    - **Given** an administrator disables the account while the user has an open session,
    - **When** the client calls refresh,
    - **Then** all of the account's refresh tokens are revoked and the request returns `401 admin.auth.account_disabled`,
    - **And** the access token in hand remains usable **until it expires** — the known limitation of stateless bearer tokens, accepted with a 30-minute lifetime.
- **Technical constraints**: the refresh token is a 256-bit random string from a CSPRNG; the database stores only the SHA-256 digest in `refresh_tokens.token_hash` (UNIQUE). Rotation is protected by a `WHERE revoked_at IS NULL` condition, so of two parallel requests with the same token only one wins.

### ADM-02: Account & role administration

#### [ADM-02.1] Provision a user account
- **Statement**:
  > **As a** Super Admin,
  > **I want** to create an account with its roles and data scope, optionally linked to an employee record,
  > **so that** a new user reaches the system with the right authority from day one.
- **Precondition**: the operator holds the `admin.user.manage` permission.
- **Acceptance criteria**:
  - **Scenario 1: the account is created successfully**
    - **Given** the e-mail is unused and every role code exists with `is_assignable = true`,
    - **When** the Super Admin creates the account with an initial password and a role list,
    - **Then** the system writes `users`, `user_credentials` and `user_roles` in **one transaction**, together with an `admin.user.create` audit record,
    - **And** `must_change_password = true` is **always** set, because someone else knows the password,
    - **And** `external_subject` is generated as `local|<normalised e-mail>`.
  - **Scenario 2: linking an employee record**
    - **Given** an `employee_id` not yet linked to any account,
    - **When** the Super Admin creates the account with that `employeeId`,
    - **Then** `employees.user_id` is updated in the same transaction,
    - **But** if the employee already has an account, the system returns `409 admin.user.employee_already_linked` and writes **nothing**.
  - **Scenario 3: invalid data is rejected before anything is written**
    - **Given** the request has a malformed e-mail, a weak password, a non-existent role, or a department-scoped grant with no `dataScopeId`,
    - **When** the system processes it,
    - **Then** it returns `422` for format errors and `409 admin.user.unknown_role` (with the `unknownRoles`/`unknownDepartments` lists) for reference errors,
    - **And** no row is written to any table.
- **Technical constraints**: `users.email` is UNIQUE after lowercasing; a race between the pre-check and the insert is caught as a unique violation and also returns `409`. The role → permission matrix is reference data in `roles`/`role_permissions` and is **not** editable through the API.

#### [ADM-02.2] Revoke & adjust access
- **Statement**:
  > **As a** Super Admin,
  > **I want** to disable an account, change its roles and reset its password when someone moves team, leaves, or loses control of their account,
  > **so that** access always matches the organisational reality and an incident is handled within minutes.
- **Precondition**: the operator holds `admin.user.manage` and the account's current `ETag`.
- **Acceptance criteria**:
  - **Scenario 1: disabling an account ends its open sessions**
    - **Given** an `active` account with live refresh tokens,
    - **When** the Super Admin disables it with the correct `If-Match`,
    - **Then** `users.status = 'disabled'` and **all** of the account's refresh tokens are revoked (`account_disabled`) in the same transaction,
    - **And** repeating the disable returns the current state (idempotent), bypassing `If-Match`.
  - **Scenario 2: changing roles replaces the whole target state**
    - **Given** the account has two `ROLE_LINE_MGR` grants, scoped to departments 2 and 4,
    - **When** the Super Admin sends a list containing only department 2,
    - **Then** the department 4 grant is removed and `audit_logs` records both the before and the after state,
    - **And** sending an empty list leaves an account that can sign in but has no authority.
  - **Scenario 3: a password reset forces a change at the next sign-in**
    - **Given** a user reports losing control of their account,
    - **When** the Super Admin sets a temporary password,
    - **Then** `must_change_password = true`, the lockout counters are cleared and all refresh tokens are revoked,
    - **And** the temporary password does **not** appear in the response — it must be handed over through a secure channel outside the system,
    - **And** the next sign-in issues a **restricted** session: no refresh token, no permissions, and only the change-password endpoint reachable.
  - **Scenario 4: nobody administers themselves**
    - **Given** a Super Admin signed in with their own account,
    - **When** they try to disable, reset the password of, or change the roles of **that same account**,
    - **Then** the system returns `403 admin.user.self_management_forbidden` and writes nothing,
    - **And** to change their own password they use `ADM-01` (`POST /auth/change-password`), which verifies the current password.
  - **Scenario 5: two administrators edit the same account**
    - **Given** two Super Admins both opened the account at `version = 5`,
    - **When** the first saves successfully and the second saves with `If-Match: "5"`,
    - **Then** the second request returns `409` and writes nothing — even when the operation is a role change, because the concurrency anchor is `users.version`.
- **Technical constraints**: every write uses `ExecuteUpdate … WHERE id = @id AND version = @expected`, with the audit record in the same transaction. An issued access token **cannot be revoked**; its 30-minute lifetime is the upper bound on revoking authority and is documented in [ADR-011](adr/011-in-house-identity.md).

---

## 6. Permission Matrix & Traceability

### 6.1. Role-to-story permission matrix

| Story ID | Summary | Employee | Recruiter | Interviewer / Hiring Mgr | Line Manager | HR Officer | HR Manager | Super Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **REC-01.1** | Create a requisition | — | — | **Create/edit** | — | — | View | — |
| **REC-01.2** | Approve & publish a posting | — | **Publish** | — | — | — | **Approve** | — |
| **REC-02.1** | CV intake & safety scan | Submit a CV | **Upload** | — | — | — | View | — |
| **REC-02.2** | Deduplication & AI parsing | — | **Review** | — | — | — | View | — |
| **REC-03.1** | View the ATS Kanban board | — | **Full** | View their rounds | — | — | View | — |
| **REC-03.2** | Advance a Kanban stage | — | **Perform** | — | — | — | Oversee | — |
| **REC-04.1** | Schedule interviews | View schedule | **Create** | Attend | — | — | Oversee | — |
| **REC-05.1** | Submit a scorecard | — | View the summary | **Score** | — | — | Manage | — |
| **REC-06.1** | Create & approve an offer letter | Respond | **Draft** | View | — | — | **Approve** | — |
| **REC-06.2** | Automatic onboarding handoff | — | — | — | — | **Receive** | Oversee | — |
| **EMP-01.1** | Search the employee directory | Basic view | — | Department view | Scoped view | **Full** | **Full** | — |
| **EMP-01.2** | Update own profile | **Update** | — | — | — | Review | Approve | — |
| **EMP-02.1** | Manage structure, positions & assignments | Scoped view | View | View | Scoped view | **Declare/assign** | **Full** | — |
| **EMP-03.1** | Track onboarding | Receive tasks | — | Receive tasks | Receive tasks | **Coordinate** | Oversee | — |
| **EMP-04.1** | Create & approve movements | View own | — | Propose | Propose | Draft | **Approve** | — |
| **EMP-05.1** | Store electronic records | View own | — | — | — | **Manage** | **Manage** | — |
| **EMP-06.1** | Submit a probation review | View own | — | — | **Review** | Coordinate | Oversee | — |
| **EMP-06.2** | Decide the probation outcome | Receive the result | — | — | Recommend | Draft | **Approve** | — |
| **EMP-07.1** | Create an offboarding case | View own | — | — | Confirm handover | **Draft** | **Approve** | — |
| **EMP-07.2** | Complete handover & close the case | Perform the handover | — | — | Confirm | **Close** | Approve exceptions | — |
| **CON-01.1** | Draft a contract | — | — | — | — | **Draft** | Approve | — |
| **CON-01.2** | Execute & activate a contract | View/sign | — | — | — | **Perform** | Oversee | — |
| **CON-02.1** | Contract expiry alerts | — | — | Receive the notice | Receive the notice | **Act** | Oversee | — |
| **CON-03.1** | Manage contract addenda | View own | — | — | — | **Draft** | **Approve** | — |
| **ADM-01.1/.2** | Sign in & keep the session | Every role | Every role | Every role | Every role | Every role | Every role | Every role |
| **ADM-02.1** | Provision accounts & roles | — | — | — | — | — | — | **Full** |
| **ADM-02.2** | Revoke & adjust access | — | — | — | — | — | — | **Full** |

The data scope is checked server-side against `user_roles.data_scope_type`. The labels "scoped view" and "team view" correspond to `data_scope_type = 'department'`; "view own" corresponds to `'self'`.

> [!NOTE]
> **The Super Admin owns only the `ADM-*` stories and holds no business permission.** The role manages accounts, roles and data scope (`ADM-02`) but cannot read employee records, contracts or recruitment data — see the last column above. The remaining System Administration functions (workflow, notification and integration configuration, the audit-trail screen) stay out of scope. Checking permission and data scope server-side is a mandatory requirement of every story, and the audit record is still written in the same transaction as the business change.
>
> `ADM-01` (sign-in, session refresh, self-service password change) applies to **every role**, which is why it is not broken down per column.

---

### 6.2. Database & use case traceability matrix

> The table below has been checked against [`schema.sql`](../database/schema.sql) and [`openapi.yaml`](api/openapi.yaml). Every table name and endpoint exists in the canonical artifacts; the base path is `/api/v1`.

#### Recruitment

| Story ID | Use Case ID | Database tables (`schema.sql`) | API endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **REC-01.1/.2** | `UC_REQ_DRAFT`, `UC_REQ_DECIDE` | `job_postings`, `departments`, `positions` | `POST /api/v1/recruitment/requisitions`, `POST /api/v1/recruitment/requisitions/{requisitionId}/{action}` |
| **REC-02.1/.2** | `UC_UPLOAD`, `UC_PARSE` | `candidates`, `resumes`, `applications` | `POST /api/v1/recruitment/resumes`, `GET /api/v1/recruitment/intakes/{intakeId}`, `POST /api/v1/recruitment/intakes/{intakeId}/confirm` |
| **REC-03.1** | `UC_PIPELINE` | `applications`, `job_postings` | `GET /api/v1/recruitment/pipeline` |
| **REC-03.2** | `UC_ADVANCE` | `applications`, `application_stage_events`, `audit_logs` | `GET /api/v1/recruitment/applications/{applicationId}`, `POST /api/v1/recruitment/applications/{applicationId}/advance`, `POST /api/v1/recruitment/applications/{applicationId}/{terminalAction}` |
| **REC-04.1** | `UC_INTERVIEW` | `interviews` | `POST /api/v1/recruitment/interviews`, `POST /api/v1/recruitment/interviews/{interviewId}/{action}` |
| **REC-05.1** | `UC_SCORE` | `evaluations` | `POST /api/v1/recruitment/interviews/{interviewId}/evaluations`, `POST /api/v1/recruitment/evaluations/{evaluationId}/unlock` |
| **REC-06.1/.2** | `UC_OFFER`, `UC_ACCEPT`, `UC_HANDOFF` | `offers`, `employees`, `onboarding_tasks`, `contracts` | `POST /api/v1/recruitment/offers`, `POST /api/v1/recruitment/offers/{offerId}/{action}`, `POST /api/v1/recruitment/offers/{offerId}/response` |

#### Core HR

| Story ID | Use Case ID | Database tables (`schema.sql`) | API endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **EMP-01.1/.2** | `UC_SEARCH`, `UC_VIEW`, `UC_SELF_CHANGE` | `employees`, `departments`, `positions` | `GET /api/v1/employees`, `GET /api/v1/employees/{employeeId}`, `PATCH /api/v1/employees/{employeeId}/profile` |
| **EMP-02.1** | `UC_ORG_MGMT` | `departments`, `positions`, `employees` | `GET\|POST /api/v1/organization/departments`, `PUT\|DELETE /api/v1/organization/departments/{departmentId}`, `GET\|POST /api/v1/organization/positions`, `PUT /api/v1/organization/positions/{positionId}` |
| **EMP-03.1** | `UC_ONBOARD`, `UC_TASK` | `onboarding_tasks`, `employees` | `GET /api/v1/onboarding/tasks`, `POST /api/v1/onboarding/tasks/{taskId}/{action}` |
| **EMP-04.1** | `UC_MOVEMENT`, `UC_MOVEMENT_DECIDE` | `employee_events`, `employees` | `POST /api/v1/employees/{employeeId}/events`, `POST /api/v1/employee-events/{eventId}/{action}` |
| **EMP-05.1** | `UC_DOCUMENT`, `UC_DOWNLOAD` | `employee_documents`, `employees` | `POST /api/v1/employees/{employeeId}/documents`, `POST /api/v1/employee-documents/{documentId}/download-url` |
| **EMP-06.1/.2** | `UC_PROBATION`, `UC_PROBATION_DECIDE` | `probation_reviews`, `employee_events`, `contracts`, `employees` | `GET\|POST /api/v1/employees/{employeeId}/probation-review`, `POST /api/v1/probation-reviews/{reviewId}/{action}` |
| **EMP-07.1/.2** | `UC_OFFBOARD`, `UC_OFFBOARD_TASK` | `offboarding_cases`, `offboarding_tasks`, `employee_events`, `users` | `GET\|POST /api/v1/offboarding/cases`, `POST /api/v1/offboarding/cases/{caseId}/{action}`, `GET /api/v1/offboarding/cases/{caseId}/tasks`, `POST /api/v1/offboarding/tasks/{taskId}/{action}` |
| **CON-01.1/.2** | `UC_DRAFT`, `UC_APPROVE`, `UC_SIGN` | `contracts`, `employees` | `POST /api/v1/contracts`, `POST /api/v1/contracts/{contractId}/{action}`, `PUT /api/v1/contracts/{contractId}/signed-document` |
| **CON-02.1** | `UC_MONITOR`, `UC_ALERT` | `contracts`, `outbox_messages` | `GET /api/v1/contracts/expiring` |
| **CON-03.1** | `UC_ADDENDUM`, `UC_ADDENDUM_EFFECT` | `contract_addenda`, `contracts`, `employee_events` | `POST /api/v1/contracts/{contractId}/addenda`, `POST /api/v1/contract-addenda/{addendumId}/{action}` |

#### Identity & Access

| Story ID | Use Case ID | Database tables (`schema.sql`) | API endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **ADM-01.1** | `UC_SIGN_IN` | `users`, `user_credentials`, `user_roles`, `role_permissions`, `employees`, `audit_logs` | `POST /api/v1/auth/login`, `GET /api/v1/auth/me` |
| **ADM-01.2** | `UC_SESSION` | `refresh_tokens`, `users`, `user_credentials`, `audit_logs` | `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, `POST /api/v1/auth/change-password` |
| **ADM-02.1** | `UC_ACCOUNT_PROVISION` | `users`, `user_credentials`, `user_roles`, `roles`, `departments`, `employees`, `audit_logs` | `GET\|POST /api/v1/admin/users`, `GET /api/v1/admin/users/{userId}`, `GET /api/v1/admin/roles` |
| **ADM-02.2** | `UC_ACCOUNT_REVOKE` | `users`, `user_credentials`, `user_roles`, `refresh_tokens`, `audit_logs` | `PUT /api/v1/admin/users/{userId}`, `POST /api/v1/admin/users/{userId}/{enable\|disable}`, `POST /api/v1/admin/users/{userId}/password-reset`, `PUT /api/v1/admin/users/{userId}/roles` |

---

### 6.3. Entry criteria for taking a story into a sprint

| Story group | Mandatory prerequisites |
| :--- | :--- |
| `REC-*`, `EMP-01`…`EMP-05`, `CON-*` | `ADM-01`/`ADM-02` must come first, because every other story needs an authenticated actor with permissions and a data scope; and the first EF Core migration must be generated from the canonical schema. |
| `ADM-01`, `ADM-02` | Settle the password policy and session lifetimes with Security (recorded in [ADR-011](adr/011-in-house-identity.md)); decide where the `Authentication:Jwt:SigningKey` secret is managed; put per-IP rate limiting at the reverse proxy in front of `POST /auth/login`. |
| `EMP-06`, `EMP-07` | Settle the offboarding checklist templates per unit; settle the list of payments due on termination — calculating and paying them belongs to Compensation & Benefits and is out of scope, so the in-scope part is only the settlement status on the offboarding case. |
