# Use Cases — Overview and the Main Management Functions

> **Status:** Proposed business design. The codes in square brackets trace to `user_stories.md` or `functional_specifications.md`. The backend under `src/backend` is **code-complete** for every use case below, but no use case has reached `implemented`: integration tests against a real PostgreSQL are still missing.

## Conventions

- A solid line from an actor to a use case: the actor directly initiates or takes part in it.
- `<<include>>`: mandatory behaviour reused by the source use case.
- `<<extend>>`: conditional or optional behaviour.
- External systems are drawn outside the QLNS boundary.
- Permission checking and audit logging are shared behaviour; the policy detail stays in the requirements and the architecture.

## 1. Overall Use Case Diagram

The diagram below shows the actors and the management function groups **within the delivery scope** of QLNS: the two pillars Recruitment and Core HR (including Contract Management) per `topdown-approach.png`, section 2 of [README.md](../README.md). The following sections break each group into detailed use cases.


```mermaid
flowchart LR
    HRMgr([HR Director / Manager])
    Recruiter([Recruiter])
    Interviewer([Hiring Manager / Interviewer])
    HROfficer([HR Officer])
    LineMgr([Line Manager])
    User([Employee / Candidate])
    Admin([Super Admin])

    subgraph HRMS["QLNS / NexusHR"]
        Identity[Sign in & manage the session]
        Accounts[Administer accounts, roles & data scope]
        Jobs[Create, approve & publish job requisitions]
        ATS[Screen CVs & manage ATS pipeline]
        Interviews[Schedule interviews & submit scorecards]
        Offers[Approve offers & hand off to onboarding]
        Records[Manage employee records, org data & contracts]
        Lifecycle[Run probation, mobility & offboarding]
    end

    Admin --> Accounts
    Admin --> Identity
    HRMgr --> Identity
    Recruiter --> Identity
    Interviewer --> Identity
    HROfficer --> Identity
    LineMgr --> Identity
    User --> Identity

    Recruiter --> Jobs
    Recruiter --> ATS
    Recruiter --> Interviews
    Interviewer --> Jobs
    Interviewer --> Interviews
    HRMgr --> Jobs
    HRMgr --> Offers
    HRMgr --> Records
    HRMgr --> Lifecycle
    HROfficer --> Records
    HROfficer --> Lifecycle
    LineMgr --> Lifecycle
    User --> ATS
    User --> Records
    User --> Lifecycle
```

This delivery **does** include sign-in and account/role administration (section 6, the `[ADM]` module), but it has no use case for reporting, analytics, system configuration or audit-log lookup; nor for viewing the org chart or for suspending and returning an employee to work. Writing an audit record and sending notifications through the outbox remain mandatory behaviour in every business use case; only the corresponding screens and lookup APIs are out of scope. The **Super Admin** actor owns exactly the identity use cases and has **no** right to read any business data.

---

## 2. Recruitment (ATS)

```mermaid
flowchart LR
    Candidate(["👤 Candidate"])
    HiringManager(["👤 Hiring Manager"])
    Recruiter(["👤 Recruiter"])
    HRManager(["👤 HR Manager"])
    Interviewer(["👤 Interviewer"])
    Careers["External system<br/>Careers portal (default channel)"]
    Communication["External system<br/>E-mail / Calendar"]

    subgraph QLNS_ATS["QLNS — Recruitment"]
        direction TB
        UC_REQ_DRAFT(["Draft a requisition<br/>[REC-01.1]"])
        UC_REQ_SUBMIT(["Submit a requisition for approval<br/>[REC-01.2]"])
        UC_REQ_DECIDE(["Approve / reject a requisition<br/>[REC-01.3]"])
        UC_PUBLISH(["Publish / update / close a posting<br/>[REC-01.4]"])
        UC_APPROVED_CHECK(["Check the requisition is approved"])
        UC_UPLOAD(["Submit / upload a CV safely<br/>[REC-02.1]"])
        UC_SCAN(["Check the file and scan for malware"])
        UC_PARSE(["Parse the CV and confirm the data<br/>[REC-02.2]"])
        UC_PIPELINE(["View the pipeline by stage<br/>[REC-03.1]"])
        UC_ADVANCE(["Advance a candidate one stage<br/>[REC-03.2]"])
        UC_REJECT(["Reject a candidate with a reason<br/>[REC-03.3]"])
        UC_ELIGIBILITY(["Check the transition is eligible"])
        UC_INTERVIEW(["Schedule / reschedule / cancel an interview<br/>[REC-04.1]"])
        UC_SCORE(["Submit a scorecard<br/>[REC-05.1]"])
        UC_OFFER(["Draft and approve an offer<br/>[REC-06.1]"])
        UC_ACCEPT(["Respond to an offer<br/>[REC-06.2]"])
        UC_HANDOFF(["Create the employee record and onboarding"])
        UC_AUTH(["Verify permission and data scope"])
        UC_AUDIT(["Write the history / audit record"])

        UC_REQ_SUBMIT -. "<<include>>" .-> UC_AUTH
        UC_REQ_DECIDE -. "<<include>>" .-> UC_AUTH
        UC_PUBLISH -. "<<include>>" .-> UC_APPROVED_CHECK
        UC_UPLOAD -. "<<include>>" .-> UC_SCAN
        UC_PARSE -. "<<extend>> when the file is clean" .-> UC_UPLOAD
        UC_ADVANCE -. "<<include>>" .-> UC_ELIGIBILITY
        UC_ADVANCE -. "<<include>>" .-> UC_AUTH
        UC_ADVANCE -. "<<include>>" .-> UC_AUDIT
        UC_REJECT -. "<<include>>" .-> UC_AUDIT
        UC_SCORE -. "<<include>>" .-> UC_AUTH
        UC_OFFER -. "<<include>>" .-> UC_AUTH
        UC_HANDOFF -. "<<extend>> when accepted" .-> UC_ACCEPT
    end

    HiringManager --> UC_REQ_DRAFT
    HiringManager --> UC_REQ_SUBMIT
    HRManager --> UC_REQ_DECIDE
    Recruiter --> UC_PUBLISH
    Recruiter --> UC_UPLOAD
    Candidate --> UC_UPLOAD
    Recruiter --> UC_PARSE
    Recruiter --> UC_PIPELINE
    Recruiter --> UC_ADVANCE
    Recruiter --> UC_REJECT
    Recruiter --> UC_INTERVIEW
    Interviewer --> UC_SCORE
    Recruiter --> UC_OFFER
    HRManager --> UC_OFFER
    Candidate --> UC_ACCEPT
    UC_PUBLISH --> Careers
    UC_INTERVIEW --> Communication
    UC_REJECT --> Communication
    UC_OFFER --> Communication
```

### Key business boundaries

- A requisition can only be published after the HR Manager approves it; approval is a human decision, and the system does not check headcount or salary budget by itself.
- A posting is published to a single default careers portal; selecting and managing multiple channels is out of scope.
- An application advances exactly one stage at a time; skipping, going backwards and overwriting an older version are all rejected.
- Entering the interview round requires a valid schedule; reaching the offer stage requires an eligible evaluation.
- The candidate-to-employee handoff must be idempotent: no duplicate employee, contract or checklist.

## 3. Employee Records and Lifecycle

```mermaid
flowchart LR
    Employee(["👤 Employee"])
    HROfficer(["👤 HR Officer"])
    HRManager(["👤 HR Manager"])
    LineManager(["👤 Line Manager"])
    TaskOwner(["👤 IT / Admin / task owner"])
    ObjectStorage["External system<br/>Private object storage"]

    subgraph QLNS_CORE["QLNS — Core HR & Employee Lifecycle"]
        direction TB
        UC_SEARCH(["Search / filter the directory<br/>[EMP-01.1]"])
        UC_VIEW(["View a profile within scope<br/>[EMP-01.2]"])
        UC_SELF_CHANGE(["Request a change to own details"])
        UC_ORG_MGMT(["Manage departments, positions, assignments and reporting lines<br/>[EMP-02.1]"])
        UC_ONBOARD(["Track the onboarding checklist<br/>[EMP-03.1]"])
        UC_TASK(["Receive and complete an onboarding task"])
        UC_MOVEMENT(["Propose an employee movement<br/>[EMP-04.1]"])
        UC_MOVEMENT_DECIDE(["Approve / cancel a movement"])
        UC_APPLY(["Apply the movement on its effective date"])
        UC_DOCUMENT(["Upload / version a document<br/>[EMP-05.1]"])
        UC_DOWNLOAD(["Access a document through a time-limited URL"])
        UC_PROBATION(["Review the probation outcome<br/>[EMP-06.1]"])
        UC_PROBATION_DECIDE(["Decide the probation outcome<br/>[EMP-06.2]"])
        UC_OFFBOARD(["Open an offboarding case<br/>[EMP-07.1]"])
        UC_OFFBOARD_TASK(["Hand over, recover assets and accounts<br/>[EMP-07.2]"])
        UC_OFFBOARD_CLOSE(["Close the case & disable the account"])
        UC_AUTH(["Verify permission, field and data scope"])
        UC_AUDIT(["Write the before/after audit record"])

        UC_SEARCH -. "<<include>>" .-> UC_AUTH
        UC_VIEW -. "<<include>>" .-> UC_AUTH
        UC_SELF_CHANGE -. "<<extend>>" .-> UC_VIEW
        UC_ORG_MGMT -. "<<include>>" .-> UC_AUTH
        UC_ONBOARD -. "<<include>>" .-> UC_TASK
        UC_TASK -. "<<include>>" .-> UC_AUDIT
        UC_MOVEMENT -. "<<include>>" .-> UC_AUTH
        UC_MOVEMENT_DECIDE -. "<<include>>" .-> UC_AUDIT
        UC_APPLY -. "<<extend>> when approved and due" .-> UC_MOVEMENT_DECIDE
        UC_DOCUMENT -. "<<include>>" .-> UC_AUTH
        UC_DOWNLOAD -. "<<extend>>" .-> UC_DOCUMENT
        UC_DOWNLOAD -. "<<include>>" .-> UC_AUDIT
        UC_PROBATION -. "<<include>>" .-> UC_AUTH
        UC_PROBATION_DECIDE -. "<<include>>" .-> UC_PROBATION
        UC_PROBATION_DECIDE -. "<<include>>" .-> UC_AUDIT
        UC_MOVEMENT -. "<<extend>> event created from the outcome" .-> UC_PROBATION_DECIDE
        UC_OFFBOARD -. "<<include>>" .-> UC_AUTH
        UC_OFFBOARD -. "<<include>>" .-> UC_OFFBOARD_TASK
        UC_OFFBOARD_CLOSE -. "<<extend>> when blocking tasks are done" .-> UC_OFFBOARD_TASK
        UC_OFFBOARD_CLOSE -. "<<include>>" .-> UC_AUDIT
    end

    Employee --> UC_VIEW
    Employee --> UC_SELF_CHANGE
    HROfficer --> UC_SEARCH
    HROfficer --> UC_VIEW
    HROfficer --> UC_ONBOARD
    HROfficer --> UC_MOVEMENT
    HROfficer --> UC_DOCUMENT
    HRManager --> UC_MOVEMENT_DECIDE
    HRManager --> UC_ORG_MGMT
    LineManager --> UC_ONBOARD
    LineManager --> UC_PROBATION
    HRManager --> UC_PROBATION_DECIDE
    HROfficer --> UC_OFFBOARD
    HRManager --> UC_OFFBOARD
    HROfficer --> UC_OFFBOARD_CLOSE
    LineManager --> UC_OFFBOARD_TASK
    Employee --> UC_OFFBOARD_TASK
    TaskOwner --> UC_TASK
    TaskOwner --> UC_OFFBOARD_TASK
    UC_DOCUMENT --> ObjectStorage
    UC_DOWNLOAD --> ObjectStorage
```

### Key business boundaries

- An employee may view and edit only the permitted fields of their own record; HR is still bounded by data scope and a field allowlist.
- Department, position, status and direct manager are never edited on the profile itself; they change through an employee event.
- The parent-child department hierarchy, the cycle-prevention rule and the delete constraints are still enforced at the data layer, but there is no use case that renders the org tree.
- An applied event is never deleted or rewritten; a reversal uses a compensating event.
- Documents are always private, safety-checked and downloaded only through time-limited access.
- Each probation contract has exactly one review; the outcome reaches the employee record only through an approved employee event.
- An overdue probation review is a legal risk, not merely a late process step — it needs its own alert.
- An employee has at most one open offboarding case; a case cannot be closed while a blocking task is outstanding or the final settlement is unresolved.
- The account is disabled exactly on the last working date and not earlier, so the employee can finish the handover.

## 4. Employment Contract Management

```mermaid
flowchart LR
    Employee(["👤 Employee"])
    HROfficer(["👤 HR Officer / C&B"])
    HRManager(["👤 HR Manager"])
    Worker(["⚙️ Background worker"])
    ESign["External system<br/>E-signature"]
    Notification["External system<br/>E-mail / notification"]

    subgraph QLNS_CONTRACT["QLNS — Contract Management"]
        direction TB
        UC_VIEW(["View a permitted contract"])
        UC_DRAFT(["Draft a contract<br/>[CON-01.1]"])
        UC_VALIDATE(["Validate the number, term and overlap"])
        UC_APPROVE(["Approve the contract"])
        UC_SIGN(["Send for signature / reconcile the signature"])
        UC_ACTIVATE(["Execute / activate the contract"])
        UC_MONITOR(["Scan for contracts nearing expiry<br/>[CON-02.1]"])
        UC_ALERT(["Raise a non-duplicated alert"])
        UC_ADDENDUM(["Draft a contract addendum<br/>[CON-03.1]"])
        UC_ADDENDUM_EFFECT(["Make the addendum effective and create an employee event"])
        UC_AUTH(["Check permission and field scope"])
        UC_AUDIT(["Write the audit record and version"])

        UC_VIEW -. "<<include>>" .-> UC_AUTH
        UC_DRAFT -. "<<include>>" .-> UC_VALIDATE
        UC_APPROVE -. "<<include>>" .-> UC_AUTH
        UC_APPROVE -. "<<include>>" .-> UC_AUDIT
        UC_SIGN -. "<<extend>> after approval" .-> UC_APPROVE
        UC_ACTIVATE -. "<<extend>> once signature evidence exists" .-> UC_SIGN
        UC_MONITOR -. "<<include>>" .-> UC_ALERT
        UC_ADDENDUM -. "<<include>>" .-> UC_VALIDATE
        UC_ADDENDUM_EFFECT -. "<<extend>> when approved/signed" .-> UC_ADDENDUM
        UC_ADDENDUM_EFFECT -. "<<include>>" .-> UC_AUDIT
    end

    Employee --> UC_VIEW
    Employee --> UC_SIGN
    HROfficer --> UC_DRAFT
    HROfficer --> UC_ADDENDUM
    HRManager --> UC_APPROVE
    HRManager --> UC_ADDENDUM
    Worker --> UC_MONITOR
    UC_SIGN --> ESign
    ESign --> UC_SIGN
    UC_ALERT --> Notification
```

### Key business boundaries

- Contract and addendum numbers are unique; a fixed-term contract must end after it starts.
- By default an employee has exactly one active primary contract.
- An addendum never edits the original contract text and must preserve the before/after terms, the approval, the signature and the version.
- A failed alert delivery never rolls back the contract state; delivery is retried a bounded number of times.

## 5. Operational Monitoring

This delivery has no reporting or analytics use case. Account and role administration is specified in section 6; what remains of the System Administration pillar is monitoring service health — infrastructure for deployment and observability, not a business function on the function map.

```mermaid
flowchart LR
    Admin(["👤 System administrator"])
    Monitor["External system<br/>Monitoring / load balancer"]

    subgraph QLNS_OPS["QLNS — Operations"]
        direction TB
        UC_HEALTH(["Monitor health / readiness"])
    end

    Admin --> UC_HEALTH
    Monitor --> UC_HEALTH
```

### Key business boundaries

- `GET /health/live` and `GET /health/ready` are infrastructure endpoints: they return no business data and require no business permission.
- Managing accounts, roles and data scope **is in scope** and is specified separately in section 6. Integration and notification configuration (which lives in `appsettings`) and the audit-log lookup screen remain out of scope.
- Writing an audit record in the same transaction as the business change, and sending notifications through the transactional outbox, **remain mandatory** for every use case in sections 2, 3 and 4; only the corresponding lookup and retry screens and APIs are out of scope.

## 6. Identity & Access

```mermaid
flowchart LR
    AnyUser(["👤 Internal user<br/>(any role)"])
    Admin(["👤 Super Admin"])

    subgraph QLNS_ADM["QLNS — Identity & Access"]
        direction TB
        UC_SIGN_IN(["Sign in with e-mail and password<br/>[ADM-01.1]"])
        UC_SESSION(["Maintain & end the session<br/>[ADM-01.2]"])
        UC_CHANGE_PWD(["Change own password<br/>[ADM-01.2]"])
        UC_ACCOUNT_PROVISION(["Provision an account & grant roles<br/>[ADM-02.1]"])
        UC_ACCOUNT_REVOKE(["Revoke & adjust authority<br/>[ADM-02.2]"])
        UC_AUDIT_WRITE(["Write the audit record"])
    end

    AnyUser --> UC_SIGN_IN
    AnyUser --> UC_SESSION
    AnyUser --> UC_CHANGE_PWD
    Admin --> UC_ACCOUNT_PROVISION
    Admin --> UC_ACCOUNT_REVOKE

    UC_SIGN_IN -. "<<include>>" .-> UC_AUDIT_WRITE
    UC_SESSION -. "<<include>>" .-> UC_AUDIT_WRITE
    UC_CHANGE_PWD -. "<<include>>" .-> UC_AUDIT_WRITE
    UC_ACCOUNT_PROVISION -. "<<include>>" .-> UC_AUDIT_WRITE
    UC_ACCOUNT_REVOKE -. "<<include>>" .-> UC_AUDIT_WRITE
    UC_ACCOUNT_REVOKE -. "<<extend>>" .-> UC_SESSION
```

### Key business boundaries

- `UC_SIGN_IN` is a **precondition of every use case** in sections 2, 3 and 4: they all `<<include>>` `UC_AUTH`, and `UC_AUTH` is now implemented by the system itself rather than by an external Identity Provider.
- Every sign-in attempt, including the failures, writes an audit record — this is the only use case where a **failure** must also leave a trace.
- `UC_ACCOUNT_REVOKE` `<<extend>>` `UC_SESSION`: disabling an account or resetting its password terminates that account's open sessions.
- The Super Admin has **no** right to read profiles, contracts or recruitment data, and may not disable, reset or re-grant their own account.
- Out of scope: self-registration, forgotten password over e-mail, SSO/OIDC federation, two-factor authentication, temporary delegation.

## 7. Actor — Function Group Matrix

| Actor | Identity | Recruitment | Core HR | Contracts |
|---|---|---|---|---|
| Candidate | — (uses `X-Offer-Token`, has no account) | Submit a CV, respond to an offer | — | — |
| Employee | Sign in, change own password | — | Own profile, department and manager information, handover on leaving | View/sign own contract |
| Hiring / Line Manager | Sign in, change own password | Requisitions, interviews | Team structure, onboarding, probation review, handover sign-off | — |
| Recruiter | Sign in, change own password | Pipeline, scheduling, scorecards, offers | — | — |
| HR Officer / C&B | Sign in, change own password | Support the handoff | Records, onboarding, movements, documents, opening and closing offboarding cases | Draft contracts and addenda |
| HR Manager | Sign in, change own password | Approve requisitions and offers | Approve movements, decide probation outcomes, approve offboarding cases, manage departments and positions | Approve contracts and addenda |
| Super Admin | **Grant and revoke accounts, roles and data scope; reset passwords** | — | Disable an account on the last working date; no default access to HR data | — |

The matrix has four function groups: the three in-scope business groups plus the identity group added by [ADR-011](adr/011-in-house-identity.md). There is still no reporting column (the Reports & Analytics pillar is out of scope), and the **Auditor** role has no use case in this delivery — audit records are still written in full, but there is no screen or API to read them.

Attendance and leave is no longer a column here, because that function group is out of the delivery scope; its use cases are kept at [deferred/attendance_leave/use_cases_att.md](deferred/attendance_leave/use_cases_att.md).

The Super Admin does not implicitly gain the right to read profiles, salaries or contracts: `ROLE_ADMIN` carries only `admin.user.*`, `admin.role.read` and `corehr.organization.read`. Administrative authority and business-data authority are kept separate.
