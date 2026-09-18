# QLNS API Reference

This document explains the operations in [`openapi.yaml`](openapi.yaml) for the two modules selected for the first delivery: **Recruitment (ATS)** and **Core HR** (including the Contracts branch).

**Scope:** the leaf functions printed in **bold** under Recruitment and Core HR on [`topdown-approach.png`](../../topdown-approach.png) — see also [README · Functional architecture](../../README.md#2-delivery-scope--seven-pillars-two-selected) — plus the **Identity & Access (ADM)** module: password sign-in, session refresh and account/role administration are served by this API itself, with no external Identity Provider. Out of scope and therefore **absent** from this document: Reports & Analytics (including report export), the remaining System Administration functions (audit-log lookup, delivery monitoring, integration/notification/approval configuration), Performance Management, Compensation & Benefits, Attendance & Leave Management, and the four non-bold functions inside the two selected modules: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart and Suspension & Return to Work. The Attendance & Leave contract is kept at [docs/deferred/attendance_leave/openapi_attendance_leave.yaml](../deferred/attendance_leave/openapi_attendance_leave.yaml).

> OpenAPI is the official contract. Where a description here differs from OpenAPI, `openapi.yaml` wins.

## 1. General Conventions

- Local base URL: `http://localhost:5080`.
- Base path: `/api/v1`.
- Internal APIs require `Authorization: Bearer <JWT>` and always check permission, data scope and field scope in the backend.
- The access token is issued by `POST /api/v1/auth/login` and lives 30 minutes by default; it is renewed through `POST /api/v1/auth/refresh` with a single-use refresh token. The three endpoints `login`, `refresh` and `logout` are public (no bearer needed).
- The candidate's offer-response API uses `X-Offer-Token` instead of a JWT.
- A mutable resource returns an `ETag`, for example `"4"`. An update command must send it back as `If-Match: "4"`.
- A retryable creating command requires an `Idempotency-Key` of 16 to 128 characters.
- Lists use `page` and `pageSize`; `pageSize` is capped at 100.
- Errors return `application/problem+json` with `type`, `title`, `status` and `correlationId`, and possibly `code`, `detail` and `errors`.

### Common HTTP status codes

| Code | Meaning |
|---|---|
| `400` | Malformed request, unsupported action, or an `If-Match` in the wrong format. |
| `401` | Missing or invalid authentication. |
| `403` | The required permission, data scope or field access is missing. |
| `404` | The resource does not exist, or lies outside the caller's visible scope. |
| `409` | A version, workflow or duplicate-data conflict, or an unmet business precondition. |
| `413` | The file exceeds the size limit. |
| `415` | The file format is not supported. |
| `422` | The payload is syntactically valid but semantically invalid. |

## 2. Identity & Access (ADM)

The identity module is managed by the system itself: passwords are stored as PBKDF2-HMAC-SHA512 hashes (210 000 iterations, a per-password salt), and refresh tokens are stored only as a SHA-256 digest and are **single-use**. Every sign-in — successful or not — writes to `audit_logs` in the same transaction as the state change.

### 2.1. Sessions (ADM-01)

#### `POST /api/v1/auth/login`

- **Purpose:** exchange an e-mail and password for an access token and a refresh token.
- **Body:** `LoginRequest` — `email`, `password`.
- **Result:** a `Session` with `accessToken`, `expiresIn`, `refreshToken` and `user` (`AuthenticatedIdentity`: roles, permissions, data scope).
- **Public:** no bearer token required.
- **401 errors with a `code`:**
  - `admin.auth.invalid_credentials` — the e-mail does not exist, has no local credential, or the password is wrong. All three return **the same** response; an unknown e-mail is still verified against a dummy hash so the response time does not reveal which accounts exist.
  - `admin.auth.account_locked` — 5 consecutive failures lock the account for 15 minutes; the problem details carry `retryAfterSeconds`.
  - `admin.auth.account_disabled` — the account is disabled; reported only **after** the password has been verified.
- **When a password change is required:** if `must_change_password` is set, the response carries **no** `refreshToken`, the access token carries **no permissions**, and `user.passwordChangeRequired = true`. Such a session can only call `change-password`.
- **Requirement:** `ADM-01.1`.

#### `POST /api/v1/auth/refresh`

- **Purpose:** rotate a refresh token into a new session.
- **Body:** `RefreshTokenRequest` — `refreshToken`.
- **Result:** a new `Session`. Permissions and data scope are **re-read from the database**, so a revoked role stops taking effect within at most one access-token lifetime.
- **Rules:** the old token is revoked (`rotated`) and linked to its successor. Presenting an **already revoked** token is treated as theft and revokes **all** of that account's refresh tokens (`reuse_detected`).
- **Errors:** `401 admin.auth.invalid_refresh_token`, `401 admin.auth.account_disabled`, `401 admin.auth.password_change_required`.
- **Requirement:** `ADM-01.2`.

#### `POST /api/v1/auth/logout`

- **Purpose:** revoke the refresh token being held.
- **Result:** always `204`, whether or not the token exists — so this endpoint cannot be used to probe for valid tokens.
- **Note:** the access token remains usable until it expires; this is the known cost of a stateless bearer token.
- **Requirement:** `ADM-01.2`.

#### `GET /api/v1/auth/me`

- **Purpose:** return the current identity, **re-read from the database** rather than trusted from the token claims.
- **Result:** `AuthenticatedIdentity`.
- **Requirement:** `ADM-01.1`.

#### `POST /api/v1/auth/change-password`

- **Purpose:** let a user change their own password.
- **Body:** `ChangePasswordRequest` — `currentPassword`, `newPassword`.
- **Password rules:** at least 10 characters and at most 128, combining at least 3 of the 4 classes (lowercase, uppercase, digit, special), not containing the local part of the e-mail, and different from the current password.
- **The current password is required** even though the caller is authenticated: a stolen access token alone must not be enough to take over the account.
- **Result:** `204`. All of that user's refresh tokens are revoked (`password_changed`), so other devices must sign in again.
- **Errors:** `401 admin.auth.invalid_credentials`, `422` if the new password is invalid, `409` if `user_credentials.version` has changed.
- **Requirement:** `ADM-01.3`.

### 2.2. Account & role administration (ADM-02)

Requires `admin.user.read` to read, `admin.user.manage` to change, and `admin.role.read` to read the role catalogue.

#### `GET /api/v1/admin/users`

- **Query:** `page`, `pageSize`, `search` (name or e-mail), `status` (`active` | `disabled`), `roleCode`.
- **Result:** a `UserAccountPage`. Each `UserAccount` carries its roles with their scope, plus `hasCredential`, `mustChangePassword`, `lastLoginAt` and `lockedUntil` — **never** a password hash or a refresh token.
- **Requirement:** `ADM-02.1`.

#### `POST /api/v1/admin/users`

- **Body:** `CreateUserAccountRequest` — `email`, `displayName`, `initialPassword`, `employeeId` (optional), `roles`.
- **Rules:** the e-mail is lowercased and must be unused; `external_subject` is generated as `local|<email>`; because an administrator chose the password, `must_change_password` is **always** set; `employeeId` links `employees.user_id` and is rejected if the employee already has an account.
- **Result:** `201` with an `ETag` and a `Location`.
- **Errors:** `409 admin.user.email_taken`, `409 admin.user.unknown_role` (with `unknownRoles` / `unknownDepartments`), `409 admin.user.employee_already_linked`, and `422` for a malformed e-mail, password or role.
- **Requirement:** `ADM-02.1`.

#### `GET /api/v1/admin/users/{userId}` · `PUT /api/v1/admin/users/{userId}`

- `PUT` requires `If-Match` on `users.version`, with a `UserAccountWrite` body (`email`, `displayName`). If nothing actually changes, nothing is written and the current state is returned.
- **Requirement:** `ADM-02.1`.

#### `POST /api/v1/admin/users/{userId}/enable` · `/disable`

- Requires `If-Match`. `disable` revokes all of that account's refresh tokens.
- **Idempotent:** an account already in the target state is returned unchanged, bypassing `If-Match`.
- **Error:** `403 admin.user.self_management_forbidden` — an administrator may not change the status of their own account.
- **Requirement:** `ADM-02.2`.

#### `POST /api/v1/admin/users/{userId}/password-reset`

- **Body:** `ResetUserPasswordRequest` — `newPassword`.
- Sets a temporary password, sets `must_change_password`, clears the lockout counters and revokes all refresh tokens. The password is **not** returned in the response; it must be handed to the user through a secure channel outside the system.
- **Error:** `403 admin.user.self_management_forbidden` — to reset your own password, use `change-password`.
- **Requirement:** `ADM-02.3`.

#### `PUT /api/v1/admin/users/{userId}/roles`

- **Body:** `RoleGrantsRequest` — a list of `RoleGrant` (`roleCode`, `dataScopeType`, `dataScopeId`). This is the **complete target state**: a role absent from the list is removed; an empty list leaves an account that can sign in but has no authority.
- **Rules:** `dataScopeType = department` requires `dataScopeId > 0` and an existing department; the other two scopes require `dataScopeId = 0` (exactly as `ck_user_roles_scope` demands). A role must exist in `roles` with `is_assignable = true`.
- Requires `If-Match` on `users.version` — that version serialises two administrators editing the same account, even though the data being changed lives in `user_roles`.
- **Error:** `403 admin.user.self_management_forbidden` — an administrator may not change their own roles.
- **Requirement:** `ADM-02.2`.

#### `GET /api/v1/admin/roles`

- Returns the role catalogue with each role's permissions. **Read-only**: the role → permission matrix is reference data deployed through [`database/seed_roles.sql`](../../database/seed_roles.sql), so a change goes through review rather than the API.
- **Requirement:** `ADM-02.2`.

## 3. Recruitment

### 3.1. Requisitions

#### `GET /api/v1/recruitment/requisitions`

- **Purpose:** search requisitions within the caller's data scope.
- **Query:** `page`, `pageSize`, `search`, `departmentId`, `status`, `sort`.
- **Valid sorts:** `createdAt`, `-createdAt`, `closingDate`, `-closingDate`.
- **Result:** a `RequisitionPage` with the list and its pagination metadata.
- **Requirements:** `REC-01.1`, `REC-01.2`.

#### `POST /api/v1/recruitment/requisitions`

- **Purpose:** create a requisition in `draft`.
- **Body:** `RequisitionWrite`; `title`, `departmentId`, `employmentType` and `targetHeadcount` are required.
- **Validation:** headcount greater than zero, salary min not greater than salary max, and a valid department and position.
- **Result:** `201` with a `Location`, an `ETag` and the new `Requisition`.
- **Requirement:** `REC-01.1`.

#### `GET /api/v1/recruitment/requisitions/{requisitionId}`

- **Purpose:** read one requisition.
- **Result:** the `Requisition` and its current `ETag`.
- **Security:** a resource outside the data scope is treated as not found.
- **Requirements:** `REC-01.1`, `REC-01.2`.

#### `PUT /api/v1/recruitment/requisitions/{requisitionId}`

- **Purpose:** replace the editable data of a `draft` requisition, or one that has been returned for revision.
- **Header:** `If-Match` required.
- **Body:** a complete `RequisitionWrite`.
- **Result:** the updated requisition and a new `ETag`.
- **Conflict:** a status that forbids editing, or a changed version, returns `409`.

#### `POST /api/v1/recruitment/requisitions/{requisitionId}/{action}`

- **Purpose:** run a workflow command on the requisition.
- **Actions:** `submit`, `approve`, `reject`, `publish`, `close`, `cancel`.
- **Header:** `If-Match` required.
- **Body:** `RequisitionAction`; `reject` requires a `reason`. `publish` takes no channel list — a posting appears only on the default careers channel.
- **Workflow:** `draft → pending_approval → approved → active_recruiting → closed/cancelled`; a rejection returns the requisition to an editable state.
- **Side effect:** publishing writes an outbox row so the posting is released after the transaction succeeds.
- **Approval:** a decision made by the HR Manager. `targetHeadcount`, `salaryMin` and `salaryMax` are declared data on the requisition; the server does not use them as a blocking condition.

### 3.2. Candidate intake and CVs

#### `POST /api/v1/recruitment/resumes`

- **Purpose:** accept a CV, scan it for malware and start parsing asynchronously.
- **Content-Type:** `multipart/form-data`.
- **Body:** `file`, `requisitionId`, `privacyNoticeVersion`, `consented=true`.
- **Limits:** PDF, DOC or DOCX; at most 10 MiB.
- **Result:** `202 Accepted` with a `Location` and a `CandidateIntake`.
- **Safety:** the file is not stored permanently until it has passed the safety checks.

#### `GET /api/v1/recruitment/intakes/{intakeId}`

- **Purpose:** track the scan, parse and duplicate-detection status.
- **Statuses:** `scanning`, `parsing`, `awaiting_confirmation`, `duplicate_review`, `completed`, `rejected`, `failed`.
- **Result:** the suggested candidate data, per-field confidence, and the list of possible duplicate candidates.

#### `POST /api/v1/recruitment/intakes/{intakeId}/confirm`

- **Purpose:** confirm the parsed data and create the application.
- **Header:** `Idempotency-Key` required.
- **Body:** `candidate`, `source`; send `existingCandidateId` to link to a candidate that already exists.
- **Result:** `201` with the new application, a `Location` and an `ETag`.
- **Conflict:** if a suspected duplicate has not been resolved, the API returns `409`.
- **Atomicity:** the candidate, résumé and application are created or linked in a single transaction.

### 3.3. The recruitment pipeline

#### `GET /api/v1/recruitment/pipeline`

- **Purpose:** get the Kanban data for one requisition.
- **Query:** `requisitionId` required; `search`, `stage`, `minimumAiScore`, `page` and `pageSize` optional.
- **Result:** the `PipelineColumn` entries, the total card count and the average AI score per stage.
- **Requirement:** `REC-03.1`.

#### `GET /api/v1/recruitment/applications/{applicationId}`

- **Purpose:** get the current state of an application.
- **Result:** a `RecruitmentApplication` and an `ETag`.

#### `POST /api/v1/recruitment/applications/{applicationId}/advance`

- **Purpose:** move an application exactly one stage forward.
- **Header:** `If-Match` required.
- **Body:** `targetStage`, plus an optional `reason`.
- **Preconditions:** no skipping or reversing a stage; entering `tech_interview` requires a valid schedule; entering `offer_letter` requires an evaluation meeting the policy.
- **Result:** the application and a new `ETag`.
- **Atomicity:** the application update, the stage event and the audit record share one transaction.

#### `POST /api/v1/recruitment/applications/{applicationId}/{terminalAction}`

- **Purpose:** end an application outside the advance flow.
- **Actions:** `reject` or `withdraw`.
- **Header:** `If-Match` required.
- **Body:** a `reason` is required.
- **Result:** the application in the `rejected` or `withdrawn` stage, with a new `ETag`.
- **Side effect:** may write an outbox row for a thank-you e-mail; a provider failure never rolls back committed state.

### 3.4. Interviews

#### `GET /api/v1/recruitment/interviews`

- **Purpose:** look up interviews within the caller's scope.
- **Query:** `page`, `pageSize`, `applicationId`, `interviewerUserId`, `from`, `to`, `status`.
- **Result:** an `InterviewPage`.

#### `POST /api/v1/recruitment/interviews`

- **Purpose:** schedule an interview and create the notification and calendar invitation.
- **Body:** `applicationId`, `interviewType`, `startsAt`, `endsAt`, `timezone` (IANA), `interviewerUserIds` (the panel, stored in `interview_panelists`; the first is the lead), and `location` or `meetingUrl`.
- **Validation:** `endsAt > startsAt`; the application is in `ai_screening`, `tech_interview` or `executive_round`; no clash for the interviewer (`recruitment.interview.interviewer_conflict`) or the room (`recruitment.interview.location_conflict`).
- **Result:** `201` with the `Interview`, a `Location` and an `ETag`.
- **Side effect:** the e-mail and calendar invitation go through the outbox.

#### `POST /api/v1/recruitment/interviews/{interviewId}/{action}`

- **Purpose:** change the interview's status or schedule.
- **Actions:** `reschedule`, `complete`, `cancel`.
- **Header:** `If-Match` required.
- **Body:** a reschedule carries the new time, timezone and location; a cancellation requires a `reason` under the policy.
- **Result:** the interview and a new `ETag`.
- **Side effect:** the update or cancellation is sent after the commit.

### 3.5. Evaluations

#### `GET /api/v1/recruitment/interviews/{interviewId}/evaluations`

- **Purpose:** read the scorecards the caller is allowed to see.
- **Security:** blind evaluation applies; an evaluator who has not submitted cannot see anyone else's.
- **Result:** an array of `Evaluation`.

#### `POST /api/v1/recruitment/interviews/{interviewId}/evaluations`

- **Purpose:** submit an immutable scorecard for one interview.
- **Header:** `Idempotency-Key` required.
- **Body:** the technical, communication, problem-solving and teamwork scores, plus a recommendation and feedback.
- **Scoring:** 0 to 5 in steps of 0.5. `overallScore` is computed by the server.
- **Result:** `201` with the `Evaluation`, a `Location` and an `ETag`.
- **Authorization:** only an interviewer assigned to that interview may submit.

#### `POST /api/v1/recruitment/evaluations/{evaluationId}/unlock`

- **Purpose:** unlock by creating a new, audited evaluation version rather than editing history.
- **Header:** `If-Match` required.
- **Body:** a `reason` is required.
- **Authorization:** HR Manager or an equivalent right.
- **Result:** the new evaluation version and a new `ETag`.

### 3.6. Offers

#### `GET /api/v1/recruitment/offers`

- **Purpose:** look up offers within the data scope.
- **Query:** `page`, `pageSize`, `applicationId`, `status`.
- **Result:** an `OfferPage`.

#### `POST /api/v1/recruitment/offers`

- **Purpose:** create a `draft` offer.
- **Body:** the application, base salary, bonus, allowance, currency, employment type, start date, response deadline and template version.
- **Preconditions:** the application has passed the interview rounds; at most one open offer per application.
- **Result:** `201` with the `Offer`, a `Location` and an `ETag`.

#### `GET /api/v1/recruitment/offers/{offerId}`

- **Purpose:** read one offer the caller is allowed to see.
- **Result:** the `Offer` and an `ETag`.

#### `POST /api/v1/recruitment/offers/{offerId}/{action}`

- **Purpose:** drive the internal offer workflow.
- **Actions:** `approve`, `send`, `extend`, `cancel`.
- **Header:** `If-Match` required.
- **Body:** `extend` carries an `expirationDate`; `cancel` requires a `reason`.
- **Workflow:** `draft → approved → sent`; after that the candidate responds, or the system moves it to `expired`.
- **Side effect:** sending creates the response token and an outbox row for the e-mail.

#### `POST /api/v1/recruitment/offers/{offerId}/response`

- **Purpose:** let the candidate accept or decline the offer.
- **Authentication:** `X-Offer-Token` — an HMAC token issued by the `send` action and delivered through the outbox; in Development it can be fetched with `GET /dev/offer-token?offerId=`.
- **Header:** `Idempotency-Key` required.
- **Body:** `decision=accept|decline`; a decline should carry a `reason`.
- **Result:** an `OfferResponseResult` with the offer status and, on acceptance, the employee, contract and onboarding IDs.
- **Atomicity:** acceptance creates at most one employee, one initial contract and one set of onboarding tasks.
- **Retry:** the same idempotency key returns the previous result with `replayed=true`.

## 4. Core HR

### 4.1. Employees

#### `GET /api/v1/employees`

- **Purpose:** search the employee directory within the data scope.
- **Query:** `page`, `pageSize`, `search`, `departmentId`, `positionId`, `status`, `sort`.
- **Valid sorts:** `name`, `-name`, `employeeCode`, `hireDate`.
- **Result:** an `EmployeePage`; no sensitive field is returned in a list.

#### `GET /api/v1/employees/{employeeId}`

- **Purpose:** view an employee profile.
- **Result:** an `EmployeeDetail` and an `ETag`.
- **Field policy:** a colleague sees only public employment information; personal and sensitive fields are hidden or masked according to authority.

#### `PATCH /api/v1/employees/{employeeId}/profile`

- **Purpose:** update the permitted personal profile fields.
- **Content-Type:** `application/merge-patch+json`.
- **Header:** `If-Match` required.
- **Permitted fields:** personal e-mail, phone, temporary address, emergency contact.
- **Forbidden fields:** work e-mail, department, position, manager, status, salary and employee code.
- **Result:** an `EmployeeDetail` and a new `ETag`.

### 4.2. Organization

#### `GET /api/v1/organization/departments`

- **Purpose:** read the department catalogue.
- **Result:** an array of `Department`.

#### `POST /api/v1/organization/departments`

- **Purpose:** create a department.
- **Body:** `code`, `name`, and optionally a parent, cost centre and description.
- **Validation:** the code is unique, the parent is valid, and no hierarchy cycle is created.
- **Result:** `201` with the department, a `Location` and an `ETag`.

#### `PUT /api/v1/organization/departments/{departmentId}`

- **Purpose:** replace a department's metadata.
- **Header:** `If-Match` required.
- **Result:** the department and a new `ETag`.

#### `DELETE /api/v1/organization/departments/{departmentId}`

- **Purpose:** delete an empty department.
- **Header:** `If-Match` required.
- **Preconditions:** no child departments, no employees and no open requisitions remain.
- **Result:** `204 No Content`; a violated precondition returns `409`.

#### `GET /api/v1/organization/positions`

- **Purpose:** read the job title and position catalogue.
- **Result:** an array of `Position`.

#### `POST /api/v1/organization/positions`

- **Purpose:** create a position definition.
- **Body:** `code`, `name`, and optionally `level` and `description`.
- **Result:** `201` with the position, a `Location` and an `ETag`.

#### `PUT /api/v1/organization/positions/{positionId}`

- **Purpose:** replace a position's metadata.
- **Header:** `If-Match` required.
- **Result:** the position and a new `ETag`.

### 4.3. Onboarding

#### `GET /api/v1/onboarding/tasks`

- **Purpose:** look up onboarding tasks, including the overdue ones.
- **Query:** `page`, `pageSize`, `employeeId`, `assignedToUserId`, `status`, `overdue`.
- **Result:** an `OnboardingTaskPage`.

#### `PUT /api/v1/onboarding/tasks/{taskId}`

- **Purpose:** update a task's name, description, owner and due date.
- **Header:** `If-Match` required.
- **Body:** `OnboardingTaskWrite`.
- **Result:** the task and a new `ETag`.

#### `POST /api/v1/onboarding/tasks/{taskId}/{action}`

- **Purpose:** move the task through its states.
- **Actions:** `start`, `complete`, `reopen`.
- **Header:** `If-Match` required.
- **Workflow:** `pending → in_progress → completed`.
- **Authorization:** reopening is restricted to an HR Officer or HR Manager and requires a reason.

### 4.4. Employee events

#### `GET /api/v1/employees/{employeeId}/events`

- **Purpose:** read the employee's immutable movement history.
- **Query:** `page`, `pageSize`.
- **Result:** an `EmployeeEventPage`.

#### `POST /api/v1/employees/{employeeId}/events`

- **Purpose:** create a movement proposal in `draft`.
- **Body:** `eventType`, `effectiveDate`, `beforeData`, `afterData`, `reason`; a compensated event can be named through `compensatesEventId`.
- **Types:** promotion, transfer, demotion, salary adjustment, termination, correction.
- **Validation:** two events effective on the same date may not change the same field.
- **Result:** `201` with the `EmployeeEvent`, a `Location` and an `ETag`.

#### `POST /api/v1/employee-events/{eventId}/{action}`

- **Purpose:** drive the movement workflow.
- **Actions:** `submit`, `approve`, `cancel`.
- **Header:** `If-Match` required.
- **Workflow:** `draft → pending_approval → approved → applied`.
- **Immutability:** an applied event is never edited or deleted; a compensating event must be created instead.

### 4.5. Employee documents

#### `GET /api/v1/employees/{employeeId}/documents`

- **Purpose:** read the metadata of the documents the caller may see.
- **Result:** an array of `EmployeeDocument`; no object key and no public storage URL are returned.
- **Security:** visibility depends on the document type and the caller's relationship to the employee.

#### `POST /api/v1/employees/{employeeId}/documents`

- **Purpose:** upload a document and create a new version of it.
- **Content-Type:** `multipart/form-data`.
- **Body:** `file`, `documentType`, and optionally `retentionUntil`.
- **Safety:** the file's type, size, content and malware status are checked before it is stored privately.
- **Result:** `201` with the document metadata and a `Location`.

#### `POST /api/v1/employee-documents/{documentId}/download-url`

- **Purpose:** mint a signed download URL after checking authority.
- **Result:** a `SignedDownload` with a `url` and an `expiresAt`, at most about 15 minutes away.
- **Audit:** access to a sensitive document must be logged.

### 4.6. Probation review

#### `GET /api/v1/employees/{employeeId}/probation-review`

- **Purpose:** read the probation review of the active probation contract.
- **Result:** a `ProbationReview` with `overdue` and an `ETag`.
- **Constraint:** a probation contract has exactly one review (`ux_probation_review_contract`).

#### `PUT /api/v1/employees/{employeeId}/probation-review`

- **Purpose:** enter or update the review content; `If-Match` required.
- **Body:** `overallScore` (0–5), `strengths`, `improvements`, `recommendedOutcome`.
- **Rules:** only the assigned `reviewerUserId` may enter it; anyone else gets `403`. A `terminated` recommendation requires `improvements`, otherwise `422`.

#### `GET /api/v1/probation-reviews`

- **Purpose:** look up reviews, with `overdue=true` to list the overdue ones.
- **Note:** a review past its `reviewDueDate` is a legal risk and must be surfaced separately on the dashboard.

#### `POST /api/v1/probation-reviews/{reviewId}/{action}`

- **Actions:** `decide`, `cancel`, `unlock`; `If-Match` required.
- **`decide`:** requires `outcome` and `effectiveDate`; without them it returns `422` (`ck_probation_decided`).
- **Atomicity:** the decision and the creation of the matching `employee_events` row share one transaction. Repeating the call is idempotent — one review produces at most one event.
- **Consequences:** `confirmed` → `probation_confirmation`; `extended` → `probation_extension`; `terminated` → `termination` plus a new offboarding case.
- **Note:** `employees.status` does **not** change on approval; it changes only when the event is applied on its `effectiveDate`.

### 4.7. Offboarding

#### `GET /api/v1/offboarding/cases`

- **Purpose:** look up offboarding cases by status and department.

#### `POST /api/v1/offboarding/cases`

- **Purpose:** open an offboarding case.
- **Precondition:** the employee must be `active` or `probation`.
- **Body:** `separationType`, `lastWorkingDate`, `handoverToEmployeeId`, `reason`, and optionally `noticeReceivedOn`.
- **Rules:** an employee who already has an open case → `409` (`ux_offboarding_open_case`). `handoverToEmployeeId` must not be the leaving employee → `422`.
- **A warning, not a block:** a notice-period shortfall is returned in `noticePeriodShortfallDays` as a warning and does not prevent saving — the decision belongs to the HR Manager and the warning is written to the audit log.

#### `GET /api/v1/offboarding/cases/{caseId}`

- **Result:** the case with `blockingTasksOutstanding` and an `ETag`.

#### `POST /api/v1/offboarding/cases/{caseId}/{action}`

- **Actions:** `approve`, `start`, `complete`, `cancel`; `If-Match` required.
- **`approve`:** generates `offboarding_tasks` from templates across the five groups `it`, `admin`, `hr`, `manager` and `finance`. Re-approving creates no duplicate task (`ux_offboarding_task_template`).
- **`complete`:** rejected while a `blocksLastWorkingDay` task is outstanding, or while `finalSettlementStatus` is not `paid`/`waived` → `409` with the list of blocking tasks.
- **Consequences:** writes one approved termination `employee_events` row effective on `lastWorkingDate`, in the same transaction that closes the case. The account is only `disabled` on `lastWorkingDate` and not earlier — see [sequence 7](../sequence_diagrams.md#7-offboarding-and-handover).

#### `GET /api/v1/offboarding/cases/{caseId}/tasks`

- **Purpose:** read the handover and recovery checklist; supports `category` and `blockingOnly`.

#### `POST /api/v1/offboarding/tasks/{taskId}/{action}`

- **Actions:** `start`, `complete`, `reopen`; `If-Match` required.
- **Rules:** overriding a blocking task requires the HR Manager permission and a reason written to the audit log.

## 5. Contracts

### 5.1. Employment contracts

#### `GET /api/v1/contracts`

- **Purpose:** look up contracts within the data scope.
- **Query:** `page`, `pageSize`, `employeeId`, `type`, `status`.
- **Security:** an employee may only see their own contracts.
- **Result:** a `ContractPage`.

#### `POST /api/v1/contracts`

- **Purpose:** create a `draft` contract.
- **Body:** the employee, contract number, type, start and end dates, salary, currency, notice period and `isPrimary`.
- **Validation:** the contract number is unique; a fixed-term contract needs `endDate > startDate`; no overlap with an active primary contract.
- **Result:** `201` with the contract, a `Location` and an `ETag`.

#### `GET /api/v1/contracts/expiring`

- **Purpose:** list the contracts that have reached an expiry alert threshold.
- **Query:** `asOf`, `withinDays` from 1 to 365, `page`, `pageSize`.
- **Result:** an `ExpiringContractPage` with `daysRemaining` and `alertLevel`.
- **Default thresholds:** probation at 15 and 7 days; fixed-term at 45 and 30 days.

#### `GET /api/v1/contracts/{contractId}`

- **Purpose:** read one contract the caller may see.
- **Result:** the `Contract` and an `ETag`.

#### `PUT /api/v1/contracts/{contractId}`

- **Purpose:** replace the editable fields of a `draft` contract.
- **Header:** `If-Match` required.
- **Body:** a complete `ContractWrite`.
- **Result:** the contract and a new `ETag`.

#### `POST /api/v1/contracts/{contractId}/{action}`

- **Purpose:** drive the contract lifecycle.
- **Actions:** `approve`, `activate`, `terminate`, `cancel`.
- **Header:** `If-Match` required.
- **Body:** `terminate` and `cancel` require a `reason`; `activate` may carry `signedAt`; an overlap exception uses `allowPrimaryOverlap` and needs the HR Manager permission.
- **Activation precondition:** the contract is approved and has valid signature evidence.
- **Result:** the contract and a new `ETag`.

#### `POST /api/v1/contracts/{contractId}/signed-document`

- **Purpose:** upload the signed PDF before activation.
- **Content-Type:** `multipart/form-data`.
- **Header:** `If-Match` required.
- **Result:** the contract with an updated document status, and a new `ETag`.

#### `POST /api/v1/contracts/{contractId}/download-url`

- **Purpose:** mint a signed URL to download the signed contract.
- **Security:** an employee may only download their own contract; HR is still bound by the data scope.
- **Result:** a short-lived `SignedDownload`.

### 5.2. Contract addenda

#### `GET /api/v1/contracts/{contractId}/addenda`

- **Purpose:** read a contract's addenda without changing the original contract.
- **Result:** an array of `ContractAddendum`.

#### `POST /api/v1/contracts/{contractId}/addenda`

- **Purpose:** create a `draft` addendum.
- **Body:** the addendum number, the effective date, `beforeTerms`, `afterTerms` and a reason.
- **Validation:** the original contract exists and is still eligible; the addendum number is unique; the before/after terms must reflect a permitted change.
- **Result:** `201` with the `ContractAddendum`, a `Location` and an `ETag`.

#### `POST /api/v1/contract-addenda/{addendumId}/{action}`

- **Purpose:** drive the addendum workflow.
- **Actions:** `submit`, `approve`, `mark-signed`, `make-effective`, `cancel`.
- **Header:** `If-Match` required.
- **Workflow:** `draft → pending_approval → approved → effective`; an older version may move to `superseded`.
- **Side effect:** on becoming effective, a change of job title, salary or department creates the matching employee event rather than editing history directly.

#### `POST /api/v1/contract-addenda/{addendumId}/signed-document`

- **Purpose:** upload the signed addendum PDF before it becomes `effective`.
- **Content-Type:** `multipart/form-data`.
- **Header:** `If-Match` required.
- **Result:** the `ContractAddendum` and a new `ETag`.

## 6. Operations

The two endpoints below are infrastructure endpoints serving deployment (liveness and readiness probes), not business functions on the function decomposition map.

#### `GET /health/live`

- A minimal public probe reporting that the process is alive; it checks no dependency and exposes no internal detail.

#### `GET /health/ready`

- A minimal public probe reporting whether the service is ready to take traffic.
- Returns `200` when ready and `503` when not; it lists no secrets and no dependency details.

## 7. Implementation Status

Every operation in `openapi.yaml` currently carries `x-implementation-status: code-complete`: a controller, an endpoint authorization policy, a business workflow (service plus domain), a persistence transaction that also writes audit and outbox rows, and unit tests for the business layer. The source lives under `src/backend`, organised as `Modules/<Module>/<Feature>`; see [src/backend/README.md](../../src/backend/README.md) for which feature holds which operation.

### What the `x-implementation-status` values mean

| Value | Meaning |
| :--- | :--- |
| `proposed` | The contract only. |
| `code-complete` | Five of six parts present: controller, policy, workflow, persistence (audit and outbox in the same transaction), unit tests. **No** integration or contract test against PostgreSQL. |
| `implemented` | `code-complete` plus integration and contract tests running against a real PostgreSQL. |

### What is still missing before `implemented`

| Area | Missing |
| :--- | :--- |
| Everything | `tests/Qlns.IntegrationTests` (WebApplicationFactory plus Testcontainers) to verify the partial unique indexes, the conditional updates and the more complex EF queries (the expiry alert window, counting blocking tasks, the manager's user subquery); and generating the EF Core migration from `schema.sql` v1.2. |
| Identity & Access | Integration tests for refresh-token rotation (including two concurrent requests with the same token) and for session revocation when an account is disabled; rate limiting at the reverse proxy in front of `POST /auth/login`; a forgotten-password flow over e-mail (today only an administrator can reset). |
| Recruitment | The outbox worker (e-mail, `.ics`, offer tokens) and the `ExpireDueOffersAsync` worker; a real CV parser replacing `DevelopmentOnlyResumeParser`; a real scanner replacing `DevelopmentOnlyMalwareScanner`. |
| Core HR — Probation, Offboarding | The worker that disables the account on `lastWorkingDate` (driven by the `corehr.offboarding.case_completed` outbox message); the source that updates `finalSettlementStatus` belongs to Payroll (out of scope). |
| Contracts | The `ExpireDueContractsAsync` worker and the expiry-alert outbox with per-threshold deduplication; a real object store replacing `FileSystemDocumentStorage`. |

### The rule for implementing an operation

An operation must be implemented with all six parts together, never separately: the controller, the authorization policy (permission plus data scope), the business workflow, the persistence transaction (with audit and outbox in the same transaction), unit tests, and contract/integration tests. An endpoint that returns the right JSON but has no server-side permission check, or writes no audit record, **does not count as implemented**.
