# Sequence Diagrams — The Main Business Flows

> **Status:** Proposed runtime design. The sequences follow the three tiers and three layers: React web → ASP.NET Core presentation → business logic → data access/EF Core → PostgreSQL.
>
> **Scope:** the sequences below describe the leaf functions printed in bold under the **Recruitment** and **Core HR** pillars of the `topdown-approach.png` map (see section 2 of the [README](../README.md)), plus the sign-in flow of the `[ADM]` identity module ([ADR-011](adr/011-in-house-identity.md)). There is no sequence for headcount/budget validation during requisition approval, multi-channel posting management, the org chart, suspension and return to work, reporting and analytics, or system configuration — all of those are out of scope.
>
> The three Attendance & Leave sequences (leave request, attendance, period locking) were moved out of scope and are kept at [deferred/attendance_leave/sequence_diagrams_att.md](deferred/attendance_leave/sequence_diagrams_att.md).

## Conventions

- `Controller`: the presentation layer, handling HTTP, DTOs, authentication and Problem Details.
- `Service`: the business logic layer, enforcing the authorization policy, the workflow and the transaction intent.
- `Repository`: the data access layer, running EF Core queries and transactions.
- Every sensitive command writes its audit record in the same transaction as the business change.
- Side effects towards external systems are written to the outbox and sent after the business transaction commits.
- An `alt` branch marks a success or failure path that can be tested independently.

## 1. Requisition — create, approve and publish

**User Stories:** `REC-01.1`, `REC-01.2` · **Status:** Proposed.

```mermaid
sequenceDiagram
    autonumber
    actor HM as Hiring Manager
    actor HR as HR Manager
    actor R as Recruiter
    participant UI as React Recruitment UI
    participant C as RequisitionController
    participant S as RequisitionService
    participant Repo as RequisitionRepository
    participant DB as PostgreSQL
    participant W as Background Worker
    participant CP as Default careers portal

    HM->>UI: Fill in and save the requisition
    UI->>C: POST /api/v1/recruitment/requisitions
    C->>S: CreateDraft(actor, command)
    S->>S: Check permission and input constraints
    S->>Repo: Insert draft + audit
    Repo->>DB: BEGIN<br/>INSERT requisition, audit<br/>COMMIT
    DB-->>Repo: Committed requisition
    Repo-->>S: Draft + version
    S-->>C: Created result
    C-->>UI: 201 Created + ETag

    HM->>UI: Submit for approval
    UI->>C: POST /{id}/submit + If-Match
    C->>S: Submit(actor, id, version)
    S->>Repo: draft → pending_approval + audit
    Repo->>DB: Conditional UPDATE<br/>COMMIT

    HR->>UI: Approve the requisition
    UI->>C: POST /{id}/approve + If-Match
    C->>S: Approve(actor, id, version)
    S->>Repo: pending_approval → approved + audit
    Repo->>DB: Conditional UPDATE<br/>COMMIT

    R->>UI: Publish the posting
    UI->>C: POST /{id}/publish + If-Match
    C->>S: Publish(actor, id, version)
    S->>Repo: approved → active_recruiting + outbox + audit
    Repo->>DB: BEGIN<br/>UPDATE + INSERT outbox/audit<br/>COMMIT
    W->>DB: Claim publication message
    W->>CP: Display the posting with an idempotency key
    CP-->>W: Posting reference
    W->>DB: Mark delivery completed
```

## 2. Candidate résumé intake and data confirmation

**User Stories:** `REC-02.1`, `REC-02.2` · **Status:** Proposed.

```mermaid
sequenceDiagram
    autonumber
    actor U as Candidate / Recruiter
    participant UI as React Intake UI
    participant C as CandidateController
    participant S as CandidateIntakeService
    participant Repo as CandidateRepository
    participant Store as Private Object Storage
    participant Scan as Malware Scanner
    participant Parser as CV Parser
    participant DB as PostgreSQL

    U->>UI: Pick a CV and enter contact details
    UI->>C: POST /api/v1/recruitment/resumes (multipart)
    C->>S: StartIntake(actor, metadata, stream)
    S->>S: Check the content type and a size ≤ 10 MB
    S->>Scan: Scan the file

    alt File invalid or not clean
        Scan-->>S: infected / failed
        S-->>C: Validation or security failure
        C-->>UI: 422 Problem Details
    else File clean
        Scan-->>S: clean
        S->>Store: Put private object
        Store-->>S: object key
        S->>Repo: Save résumé metadata + outbox parse request
        Repo->>DB: BEGIN<br/>INSERT résumé/outbox<br/>COMMIT
        Parser->>Store: Read object
        Parser->>DB: Save parsed fields + confidence
        C-->>UI: 202 Accepted + intake ID
    end

    U->>UI: Review and correct the parsed data
    UI->>C: POST /api/v1/recruitment/intakes/{id}/confirm
    C->>S: ConfirmParsedData(actor, command)
    S->>Repo: Check for a duplicate identity

    alt A candidate with the same e-mail/phone exists
        Repo-->>S: Possible duplicate
        S-->>C: Duplicate confirmation required
        C-->>UI: 409 — the recruiter must confirm the merge
    else No duplicate
        S->>Repo: Create candidate + application + audit
        Repo->>DB: Atomic INSERT<br/>COMMIT
        C-->>UI: 201 Application created
    end
```

## 3. Advance an application to the next stage

**User Story:** `REC-03.2` · **Status:** Proposed. `RecruitmentPipelineService` in `src/backend` implements this flow at the code-complete level; there is no integration test yet verifying the conditional update against a real PostgreSQL.

```mermaid
sequenceDiagram
    autonumber
    actor R as Recruiter
    participant UI as React Pipeline Page
    participant C as RecruitmentApplicationsController
    participant S as RecruitmentPipelineService
    participant D as RecruitmentApplication
    participant Repo as RecruitmentApplicationRepository
    participant DB as PostgreSQL

    R->>UI: Choose “Advance stage”
    UI->>C: POST /applications/{id}/advance + JWT + If-Match "4"
    C->>C: Verify the permission and build the data scope
    C->>S: AdvanceAsync(command)
    S->>Repo: GetByIdAsync(id, dataScope)
    Repo->>DB: SELECT application JOIN job within scope
    DB-->>Repo: stage + version

    alt Not present within scope
        Repo-->>S: null
        S-->>C: ApplicationNotFoundException
        C-->>UI: 404 Problem Details
    else Version has changed
        S-->>C: ConcurrencyConflictException
        C-->>UI: 409 common.concurrency_conflict
        UI->>C: Reload application
    else Version current
        S->>Repo: GetAdvanceEligibilityAsync(id, target)
        Repo->>DB: Check the required interview/evaluation
        S->>D: AdvanceTo(target, eligibility)

        alt Skipping/reversing a stage, or ineligible
            D-->>S: BusinessRuleException
            S-->>C: Stable business error code
            C-->>UI: 409 Problem Details
        else Transition valid
            D-->>S: New stage + version 5
            S->>Repo: SaveAdvanceAsync(expectedVersion=4)
            Repo->>DB: BEGIN
            Repo->>DB: Conditional UPDATE applications WHERE version=4
            Repo->>DB: INSERT application_stage_events
            Repo->>DB: INSERT audit_logs
            Repo->>DB: COMMIT
            C-->>UI: 200 + ETag "5"
        end
    end
```

## 4. Candidate accepts an offer and moves into onboarding

**User Story:** `REC-06.2` · **Status:** Proposed.

The endpoint and token match `openapi.yaml`: `respondToRecruitmentOffer` with `X-Offer-Token`; the response is always `200 OfferResponseResult`, distinguishing the first call from a replay through `replayed`.

```mermaid
sequenceDiagram
    autonumber
    actor Candidate
    participant UI as Candidate Portal
    participant C as RecruitmentOffersController
    participant S as OfferAcceptanceService
    participant Repo as OfferHandoffRepository
    participant DB as PostgreSQL
    participant W as Outbox Worker
    participant P as Provisioning / Notification

    Candidate->>UI: Accept the offer
    UI->>C: POST /api/v1/recruitment/offers/{offerId}/response<br/>{ decision: accept } + X-Offer-Token + Idempotency-Key
    C->>C: Verify X-Offer-Token matches offerId and is unexpired
    C->>S: Respond(command)
    S->>Repo: Lock the offer, application and candidate
    Repo->>DB: SELECT ... FOR UPDATE

    alt Token wrong or not matching the offer
        C-->>UI: 401 Problem Details
    else Offer not in state sent, or already expired
        S-->>C: recruitment.offer_not_open
        C-->>UI: 409 Problem Details
    else employees.source_application_id = application.id already exists
        Repo-->>S: Existing employee, contract and tasks
        C-->>UI: 200 OfferResponseResult { replayed: true }
    else Valid — first call
        S->>Repo: Accept and hand off in one transaction
        Repo->>DB: BEGIN<br/>UPDATE offers → accepted<br/>UPDATE applications → hired_ready (version+1)<br/>INSERT application_stage_events<br/>INSERT employees (source_application_id)<br/>INSERT contracts (probation, draft)<br/>INSERT onboarding_tasks × N<br/>INSERT audit_logs<br/>INSERT outbox_messages<br/>COMMIT
        C-->>UI: 200 OfferResponseResult { replayed: false }
        W->>DB: Claim outbox messages (after commit)
        W->>P: Provision accounts, notify the HR Officer and the manager
        P-->>W: Result
        W->>DB: Update delivery state / retry schedule
    end
```

## 5. Approve and apply an employee movement

**User Story:** `EMP-04.1` · **Status:** Proposed.

```mermaid
sequenceDiagram
    autonumber
    actor Officer as HR Officer
    actor Manager as HR Manager
    participant UI as React Employee UI
    participant C as EmployeeEventsController
    participant S as EmployeeMovementService
    participant Repo as EmployeeRepository
    participant DB as PostgreSQL
    participant W as Effective-Date Worker

    Officer->>UI: Enter the before/after data, effective date and reason
    UI->>C: POST /api/v1/employees/{id}/events
    C->>S: CreateProposal(actor, command)
    S->>Repo: Check the employee version and conflicting fields
    Repo->>DB: INSERT event pending_approval + audit
    C-->>UI: 201 Created + ETag

    Manager->>UI: Approve the proposal
    UI->>C: POST /events/{id}/approve + If-Match
    C->>S: Approve(actor, eventId, version)
    S->>Repo: Conditional approve + audit
    Repo->>DB: COMMIT

    W->>S: Apply the events that are now due
    S->>Repo: Lock the event and the employee

    alt Another event changes the same field on the same date
        Repo-->>S: Conflict
        S->>DB: Record a reconciliation task
    else No conflict
        S->>Repo: Apply the master-data change + mark applied + audit
        Repo->>DB: Atomic UPDATE<br/>COMMIT
    end
```

## 6. Contract lifecycle and expiry alerts

**User Stories:** `CON-01.1`, `CON-02.1`, `CON-03.1` · **Status:** Proposed.

> [!NOTE]
> Signing happens outside the system and the signed copy is uploaded through `POST /api/v1/contracts/{contractId}/signed-document`. The API contract has no callback or webhook from an e-signature provider: no provider has been chosen and that integration is out of scope for this delivery.

```mermaid
sequenceDiagram
    autonumber
    actor Officer as HR Officer / C&B
    actor Manager as HR Manager
    actor Employee
    participant UI as React Contract UI
    participant C as ContractsController
    participant S as ContractService
    participant Repo as ContractRepository
    participant DB as PostgreSQL
    participant W as Background Worker
    participant Store as Private Object Storage

    Officer->>UI: Draft a contract or an addendum
    UI->>C: POST /api/v1/contracts
    C->>S: CreateDraft(actor, command)
    S->>Repo: Validate the number, the dates and any active-contract overlap
    Repo->>DB: INSERT draft + audit<br/>COMMIT

    Manager->>UI: Approve
    UI->>C: POST /contracts/{id}/approve + If-Match
    C->>S: Approve(actor, id, version)
    S->>Repo: approved + outbox notification + audit
    Repo->>DB: Atomic UPDATE/INSERT<br/>COMMIT
    W->>DB: Claim the notification after commit

    Employee->>Officer: Sign the paper or scanned copy
    Officer->>UI: Upload the signed copy
    UI->>C: POST /contracts/{id}/signed-document
    C->>S: AttachSignedDocument(actor, id, file)
    S->>Repo: Scan the file, store the object key, move to executed
    Repo->>Store: PUT the signed copy into private storage
    Repo->>DB: UPDATE executed + audit<br/>COMMIT

    Officer->>UI: Activate the contract
    UI->>C: POST /contracts/{id}/activate + If-Match
    C->>S: Activate(actor, id, version)
    S->>Repo: Close the previous primary contract, open the new one
    Repo->>DB: Atomic UPDATE + audit<br/>COMMIT

    W->>Repo: Find contracts at an alert threshold
    Repo->>DB: Bounded expiry query
    W->>DB: Insert a deduplicated notification outbox row
```

## 7. Offboarding and handover

**User Stories:** `EMP-07.1`, `EMP-07.2` · **Status:** Proposed.

This flow has many side effects, and the order between them is a business rule rather than a technical detail: the checklist is only generated once the case is approved, the termination `employee_events` row is only written on completion, and the account is disabled exactly on `lastWorkingDate` rather than when someone presses complete.

```mermaid
sequenceDiagram
    autonumber
    actor Officer as HR Officer
    actor Mgr as Line Manager / HR Manager
    participant UI as React Employee UI
    participant C as OffboardingController
    participant S as OffboardingCaseService
    participant TS as OffboardingTaskService
    participant Repo as OffboardingCaseRepository
    participant DB as PostgreSQL
    participant W as Worker (outbox + effective date)

    Officer->>UI: Open the case (type, last working date, handover recipient, reason)
    UI->>C: POST /api/v1/offboarding/cases
    C->>S: Open(actor, command)
    S->>S: Check the employee is active or probation<br/>handoverToEmployeeId ≠ employeeId

    alt The employee already has an open case
        Repo->>DB: INSERT violates ux_offboarding_open_case
        DB-->>Repo: unique violation
        S-->>C: Conflict
        C-->>UI: 409 Problem Details
    else Valid
        Repo->>DB: INSERT draft case + audit (same transaction)
        S->>S: Compute noticePeriodShortfallDays
        Note over S,C: A notice-period shortfall is a **warning**, not a block —<br/>the HR Manager decides and the warning is audited
        C-->>UI: 201 Created + ETag + noticePeriodShortfallDays
    end

    Mgr->>UI: Approve the case
    UI->>C: POST /offboarding/cases/{id}/approve + If-Match
    C->>S: Act(actor, Approve, version)
    S->>S: Generate the checklist from templates across it/admin/hr/manager/finance
    Repo->>DB: Conditional UPDATE WHERE version = expected<br/>+ INSERT tasks (skipping existing template_key) + audit
    Note over Repo,DB: ux_offboarding_task_template means re-approving<br/>creates no duplicate task
    C-->>UI: 200 + new ETag

    loop For each handover / recovery task
        Mgr->>UI: start / complete the task
        UI->>C: POST /offboarding/tasks/{taskId}/{action} + If-Match
        C->>TS: Act(actor, action, version)
        TS->>Repo: Conditional UPDATE + audit
    end

    Officer->>UI: Complete the case
    UI->>C: POST /offboarding/cases/{id}/complete + If-Match
    C->>S: Act(actor, Complete, version, reason?)
    S->>Repo: ListBlockingTasks(caseId)

    alt A blocks_last_working_day task is outstanding and the actor lacks the approve permission
        S-->>C: BusinessRule blocking_tasks_outstanding
        C-->>UI: 409 + the list of blocking tasks
    else Override the blocking tasks (needs the approve permission + a reason)
        S->>S: Mark as overridden; the reason is mandatory and audited
    end

    alt finalSettlementStatus is not paid/waived
        S-->>C: BusinessRule settlement_pending
        C-->>UI: 409 — there is **no** way around this check
        Note over S: finalSettlementStatus is owned by Payroll (out of scope)
    else Settlement resolved
        S->>Repo: SaveCompletion(case completed + termination event)
        Repo->>DB: UPDATE case + INSERT employee_events<br/>(termination, approved, effective = lastWorkingDate)<br/>+ audit + outbox corehr.offboarding.case_completed<br/>ONE transaction
        C-->>UI: 200 + new ETag
    end

    W->>DB: Claim outbox corehr.offboarding.case_completed
    W->>DB: On lastWorkingDate: apply employee_events → employees.status = resigned/terminated
    Note over W,DB: The account moves to disabled exactly on lastWorkingDate, not earlier.<br/>The worker host does not exist yet — see architecture.md §5.4
```

## 8. Sign-in, session refresh and stolen-token detection

**User Stories:** `ADM-01.1`, `ADM-01.2` · **Status:** Proposed.

This flow is the precondition of the seven above: every `Controller` there assumes an authenticated actor already carrying permissions and a data scope. The three branches below are the three independently testable ones: a wrong password, a lockout, and replaying a consumed refresh token.

```mermaid
sequenceDiagram
    autonumber
    actor U as Internal user
    participant UI as React Auth UI
    participant C as AuthController
    participant S as AuthenticationService
    participant H as Pbkdf2PasswordHasher
    participant T as JwtAccessTokenIssuer
    participant Repo as AuthenticationRepository
    participant DB as PostgreSQL

    U->>UI: Enter e-mail and password
    UI->>C: POST /api/v1/auth/login
    C->>S: SignIn(email, password, clientContext)
    S->>Repo: FindByEmail(normalised e-mail)
    Repo->>DB: SELECT users ⋈ user_credentials ⋈ employees
    DB-->>Repo: Account + credential
    Repo-->>S: UserSignInRecord

    alt E-mail unknown, or no credential
        S->>H: Verify(DummyHash, password)
        Note over S,H: Takes exactly as long as a real verification,<br/>so which accounts exist cannot be probed
        S->>Repo: RecordFailedSignIn(invalid_credentials)
        Repo->>DB: BEGIN<br/>INSERT audit_logs (result='rejected')<br/>COMMIT
        S-->>C: AuthenticationFailedException
        C-->>UI: 401 invalid_credentials
    else Inside the lockout window
        S-->>C: AccountLocked(retryAfterSeconds)
        Note over S: The password is not verified and the counter is not incremented
        C-->>UI: 401 account_locked
    else Wrong password
        S->>H: Verify(storedHash, password) → Failed
        S->>Repo: RecordFailedSignIn(attempts+1, locked_until once 5 is reached)
        Repo->>DB: BEGIN<br/>UPDATE user_credentials<br/>INSERT audit_logs (rejected)<br/>COMMIT
        C-->>UI: 401 invalid_credentials
    else Password correct
        S->>H: Verify(storedHash, password) → Succeeded
        alt users.status = 'disabled'
            S->>Repo: RecordFailedSignIn(account_disabled)
            C-->>UI: 401 account_disabled
        else must_change_password
            S->>T: Issue(restricted identity: no permissions, 10 minutes)
            S->>Repo: RecordSuccessfulSignIn(no refresh token)
            C-->>UI: 200 Session (no refreshToken, passwordChangeRequired)
        else Normal
            S->>Repo: GetAuthorization(userId)
            Repo->>DB: SELECT user_roles ⋈ role_permissions
            DB-->>Repo: Grants + permissions
            S->>T: Issue(full identity)
            S->>Repo: RecordSuccessfulSignIn(new refresh token)
            Repo->>DB: BEGIN<br/>UPDATE user_credentials (reset the counter, last_login_at)<br/>INSERT refresh_tokens (SHA-256 digest only)<br/>INSERT audit_logs (succeeded)<br/>COMMIT
            C-->>UI: 200 Session (accessToken + refreshToken)
        end
    end

    Note over UI: ~1 minute before the access token expires
    UI->>C: POST /api/v1/auth/refresh
    C->>S: Refresh(refreshToken)
    S->>Repo: FindRefreshToken(SHA-256(token))
    alt The token was already revoked — a sign of theft
        S->>Repo: RevokeAllRefreshTokens(reuse_detected)
        Repo->>DB: BEGIN<br/>UPDATE refresh_tokens SET revoked_at<br/>INSERT audit_logs (rejected)<br/>COMMIT
        C-->>UI: 401 invalid_refresh_token
        Note over UI: Both the real user and the attacker must sign in again
    else Token still valid
        S->>Repo: GetAuthorization(userId)
        Note over S,Repo: Authority is re-read from the database, so a role revoked<br/>a moment ago is absent from the new token
        S->>Repo: RotateRefreshToken(tokenId, successor)
        Repo->>DB: BEGIN<br/>UPDATE ... WHERE id=@id AND revoked_at IS NULL<br/>INSERT refresh_tokens (successor)<br/>UPDATE replaced_by_token_id<br/>INSERT audit_logs<br/>COMMIT
        Note over Repo,DB: The revoked_at IS NULL condition means that of two parallel<br/>requests with the same token, only one wins
        C-->>UI: 200 New session
    end
```

## 9. Traceability

| # | Sequence | User Story / Requirement | Module | Implementation status |
|---|---|---|---|---|
| 1 | Requisition lifecycle | `REC-01.1`–`REC-01.2` | Recruitment | Proposed |
| 2 | Résumé intake | `REC-02.1`–`REC-02.2` | Recruitment | Proposed |
| 3 | Advance application | `REC-03.2` | Recruitment | Proposed |
| 4 | Offer acceptance | `REC-06.1`–`REC-06.2` | Recruitment → Core HR | Proposed |
| 5 | Employee movement | `EMP-04.1` | Core HR | Proposed |
| 6 | Contract lifecycle | `CON-01.1`–`CON-03.1` | Core HR / Contracts | Proposed |
| 7 | Offboarding and handover | `EMP-07.1`–`EMP-07.2` | Core HR | Proposed |
| 8 | Sign-in and refresh rotation | `ADM-01.1`–`ADM-01.2` | Identity & Access | Proposed |

### Flows without a sequence diagram

The stories below have acceptance criteria but no sequence diagram. This is a known gap, not an oversight:

| Story | Why it is not drawn |
|---|---|
| `REC-03.1`, `REC-04.1`, `REC-05.1` | Reading the pipeline, scheduling an interview and submitting a scorecard are clear enough from the acceptance criteria; a diagram is only worth drawing if a dispute arises about invitation ordering or locking an evaluation record |
| `EMP-01.1`–`EMP-01.2` | Looking up and self-updating a profile is single-step CRUD; the rules that matter live in the field policy and the data scope, not in the interaction order |
| `EMP-02.1` | Managing departments, positions and assignments is CRUD with `If-Match`; a diagram will follow if bulk departmental restructuring is added |
| `EMP-03.1` | Generating the onboarding checklist is already covered by sequence 4; tracking and reminders are straightforward background work |
| `EMP-05.1` | The upload and signed-URL flow will be drawn together with the document storage standard |
| `EMP-06.1`–`EMP-06.2` | To be drawn once the probation review template is settled |
| `ADM-02.1`–`ADM-02.2` | Provisioning an account and changing roles is CRUD with `If-Match`; the rules that matter are reference validation and the self-administration guard, not the interaction order. The one ordered consequence — disabling an account revokes its refresh tokens — is already shown in sequence 8 |
