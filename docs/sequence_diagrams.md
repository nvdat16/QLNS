# Sequence Diagrams — Các Luồng Nghiệp Vụ Chính

> **Trạng thái:** Proposed runtime design, ngoại trừ `REC-03.2` đã có source baseline. Các sequence tuân theo 3-tier và 3-layer: React Web → ASP.NET Core Presentation → Business Logic → Data Access/EF Core → PostgreSQL.
>
> Sequence 7, 8 và 9 thuộc phân hệ Attendance & Leave và ở trạng thái **`Blocked — policy pending`**: cấu trúc luồng đã chốt, nhưng công thức tính toán bên trong phụ thuộc [Open Decisions](open_decisions_attendance_leave.md).

## Quy ước

- `Controller`: Presentation Layer, xử lý HTTP, DTO, authentication và Problem Details.
- `Service`: Business Logic Layer, thực thi authorization policy, workflow và transaction intent.
- `Repository`: Data Access Layer, thực thi EF Core query/transaction.
- Mọi command nhạy cảm phải ghi audit trong cùng transaction với thay đổi nghiệp vụ.
- Side effect đến hệ thống ngoài được ghi Outbox và gửi sau khi business transaction commit.
- Nhánh `alt` thể hiện success/failure có thể kiểm thử độc lập.

## 1. Requisition — tạo, phê duyệt và đăng tuyển

**User Stories:** `REC-01.1` → `REC-01.4` · **Trạng thái:** Proposed.

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
    participant JB as Job Board

    HM->>UI: Nhập và lưu requisition
    UI->>C: POST /api/v1/recruitment/requisitions
    C->>S: CreateDraft(actor, command)
    S->>S: Kiểm tra quyền, headcount và salary range
    S->>Repo: Insert draft + audit
    Repo->>DB: BEGIN<br/>INSERT requisition, audit<br/>COMMIT
    DB-->>Repo: Committed requisition
    Repo-->>S: Draft + version
    S-->>C: Created result
    C-->>UI: 201 Created + ETag

    HM->>UI: Gửi phê duyệt
    UI->>C: POST /{id}/submit + If-Match
    C->>S: Submit(actor, id, version)
    S->>Repo: draft → pending_approval + audit
    Repo->>DB: Conditional UPDATE<br/>COMMIT

    HR->>UI: Phê duyệt requisition
    UI->>C: POST /{id}/approve + If-Match
    C->>S: Approve(actor, id, version)
    S->>Repo: pending_approval → approved + audit
    Repo->>DB: Conditional UPDATE<br/>COMMIT

    R->>UI: Chọn kênh và đăng tuyển
    UI->>C: POST /{id}/publish + If-Match
    C->>S: Publish(actor, id, channels)
    S->>Repo: approved → active_recruiting + outbox + audit
    Repo->>DB: BEGIN<br/>UPDATE + INSERT outbox/audit<br/>COMMIT
    W->>DB: Claim publication message
    W->>JB: Publish with idempotency key
    JB-->>W: Provider posting ID
    W->>DB: Mark delivery completed
```

## 2. Candidate résumé intake và xác nhận dữ liệu

**User Stories:** `REC-02.1`, `REC-02.2` · **Trạng thái:** Proposed.

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

    U->>UI: Chọn CV và nhập thông tin liên hệ
    UI->>C: POST /api/v1/recruitment/resumes (multipart)
    C->>S: StartIntake(actor, metadata, stream)
    S->>S: Kiểm tra type và dung lượng ≤ 10 MB
    S->>Scan: Scan file

    alt File không hợp lệ hoặc không sạch
        Scan-->>S: infected / failed
        S-->>C: Validation or security failure
        C-->>UI: 422 Problem Details
    else File sạch
        Scan-->>S: clean
        S->>Store: Put private object
        Store-->>S: object key
        S->>Repo: Save résumé metadata + outbox parse request
        Repo->>DB: BEGIN<br/>INSERT résumé/outbox<br/>COMMIT
        Parser->>Store: Read object
        Parser->>DB: Save parsed fields + confidence
        C-->>UI: 202 Accepted + intake ID
    end

    U->>UI: Kiểm tra và sửa dữ liệu bóc tách
    UI->>C: POST /api/v1/recruitment/intakes/{id}/confirm
    C->>S: ConfirmParsedData(actor, command)
    S->>Repo: Check duplicate identity

    alt Có candidate trùng email/phone
        Repo-->>S: Possible duplicate
        S-->>C: Duplicate confirmation required
        C-->>UI: 409 yêu cầu Recruiter xác nhận merge
    else Không trùng
        S->>Repo: Create candidate + application + audit
        Repo->>DB: Atomic INSERT<br/>COMMIT
        C-->>UI: 201 Application created
    end
```

## 3. Chuyển application sang giai đoạn tiếp theo

**User Story:** `REC-03.2` · **Trạng thái:** Source baseline implemented, runtime verification pending.

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

    R->>UI: Chọn “Chuyển giai đoạn”
    UI->>C: POST /applications/{id}/advance + JWT + If-Match "4"
    C->>C: Xác thực permission và tạo data scope
    C->>S: AdvanceAsync(command)
    S->>Repo: GetByIdAsync(id, dataScope)
    Repo->>DB: SELECT application JOIN job trong scope
    DB-->>Repo: stage + version

    alt Không tồn tại trong scope
        Repo-->>S: null
        S-->>C: ApplicationNotFoundException
        C-->>UI: 404 Problem Details
    else Version đã thay đổi
        S-->>C: ConcurrencyConflictException
        C-->>UI: 409 common.concurrency_conflict
        UI->>C: Reload application
    else Version hiện tại
        S->>Repo: GetAdvanceEligibilityAsync(id, target)
        Repo->>DB: Kiểm tra interview/evaluation bắt buộc
        S->>D: AdvanceTo(target, eligibility)

        alt Nhảy/lùi stage hoặc thiếu điều kiện
            D-->>S: BusinessRuleException
            S-->>C: Stable business error code
            C-->>UI: 409 Problem Details
        else Transition hợp lệ
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

## 4. Candidate chấp nhận Offer và chuyển sang onboarding

**User Story:** `REC-06.2` · **Trạng thái:** Proposed.

```mermaid
sequenceDiagram
    autonumber
    actor Candidate
    participant UI as Candidate Portal
    participant C as OfferController
    participant S as OfferAcceptanceService
    participant Repo as OnboardingRepository
    participant DB as PostgreSQL
    participant W as Background Worker
    participant P as Provisioning / Notification

    Candidate->>UI: Chấp nhận Offer
    UI->>C: POST /api/v1/offers/{id}/accept + acceptance token + Idempotency-Key
    C->>S: AcceptOffer(command)
    S->>Repo: Lock offer, application và source candidate
    Repo->>DB: SELECT FOR UPDATE

    alt Offer hết hạn hoặc không ở trạng thái sent
        S-->>C: Invalid offer state
        C-->>UI: 409 Problem Details
    else Idempotency key đã xử lý
        Repo-->>S: Existing employee result
        C-->>UI: 200 existing result
    else Hợp lệ
        S->>Repo: Accept + create employee/contract/tasks/audit/outbox
        Repo->>DB: BEGIN<br/>atomic INSERT/UPDATE<br/>COMMIT
        C-->>UI: 201 Employee onboarding created
        W->>DB: Claim onboarding messages
        W->>P: Provision accounts and send notifications
        P-->>W: Result
        W->>DB: Update delivery state / retry schedule
    end
```

## 5. Phê duyệt và áp dụng biến động nhân sự

**User Story:** `EMP-04.1` · **Trạng thái:** Proposed.

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

    Officer->>UI: Nhập before/after, ngày hiệu lực và lý do
    UI->>C: POST /api/v1/employees/{id}/events
    C->>S: CreateProposal(actor, command)
    S->>Repo: Check employee version và conflicting fields
    Repo->>DB: INSERT event pending_approval + audit
    C-->>UI: 201 Created + ETag

    Manager->>UI: Phê duyệt đề xuất
    UI->>C: POST /events/{id}/approve + If-Match
    C->>S: Approve(actor, eventId, version)
    S->>Repo: Conditional approve + audit
    Repo->>DB: COMMIT

    W->>S: Apply events đến ngày hiệu lực
    S->>Repo: Lock event và employee

    alt Có event khác thay đổi cùng field/ngày
        Repo-->>S: Conflict
        S->>DB: Record reconciliation task
    else Không xung đột
        S->>Repo: Apply master-data change + mark applied + audit
        Repo->>DB: Atomic UPDATE<br/>COMMIT
    end
```

## 6. Vòng đời hợp đồng và cảnh báo hết hạn

**User Stories:** `CON-01.1`, `CON-02.1`, `CON-03.1` · **Trạng thái:** Proposed.

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
    participant Sign as E-signature Provider

    Officer->>UI: Soạn hợp đồng hoặc phụ lục
    UI->>C: POST /api/v1/contracts
    C->>S: CreateDraft(actor, command)
    S->>Repo: Validate number, dates và active-contract overlap
    Repo->>DB: INSERT draft + audit<br/>COMMIT

    Manager->>UI: Phê duyệt
    UI->>C: POST /contracts/{id}/approve + If-Match
    C->>S: Approve(actor, id, version)
    S->>Repo: approved + outbox signature request + audit
    Repo->>DB: Atomic UPDATE/INSERT<br/>COMMIT
    W->>DB: Claim signature request after commit
    W->>Sign: Send document with idempotency key

    Employee->>Sign: Ký tài liệu
    Sign->>C: Signed callback + provider event ID
    C->>S: ReconcileSignature(callback)
    S->>Repo: Verify idempotency and activate contract
    Repo->>DB: UPDATE signed/active + audit<br/>COMMIT

    W->>Repo: Find contracts at alert thresholds
    Repo->>DB: Bounded expiry query
    W->>DB: Insert deduplicated notification outbox
```

## 7. Gửi và xét duyệt đơn nghỉ phép

**User Stories:** `ATT-03.2`, `ATT-03.3` · **Trạng thái:** Proposed · **Blocked — policy pending** (`[OD-6.*]`, `[OD-7.*]`).

```mermaid
sequenceDiagram
    autonumber
    actor E as Employee
    actor M as Line Manager
    participant UI as React Leave UI
    participant C as LeaveRequestsController
    participant S as LeaveService
    participant Repo as LeaveRepository
    participant DB as PostgreSQL
    participant W as Notification Worker

    E->>UI: Nhập loại phép, thời gian và lý do
    UI->>C: POST /api/v1/leave-requests
    C->>S: Submit(actor, command)
    S->>Repo: Get schedule, holidays, overlap và balance
    Repo->>DB: Read scoped leave data

    alt Không đủ quỹ phép hoặc trùng đơn
        S-->>C: Stable business conflict
        C-->>UI: 409 Problem Details
    else Hợp lệ
        S->>Repo: Create pending request + reserve balance + audit/outbox
        Repo->>DB: BEGIN<br/>INSERT/UPDATE<br/>COMMIT
        C-->>UI: 201 Pending request
    end

    M->>UI: Duyệt hoặc từ chối
    UI->>C: POST /leave-requests/{id}/decision + If-Match
    C->>S: Decide(actor, command)
    S->>S: Kiểm tra approver chain và data scope
    S->>Repo: Conditional decision + balance + audit/outbox

    alt Version/approver không hợp lệ
        Repo-->>S: Conflict / forbidden
        C-->>UI: 409 hoặc 403 Problem Details
    else Quyết định hợp lệ
        Repo->>DB: Atomic UPDATE<br/>COMMIT
        C-->>UI: 200 decision result
        W->>DB: Claim notification
        W-->>E: Gửi kết quả sau commit
    end
```

## 8. Ghi nhận chấm công và tính bảng công

**User Stories:** `ATT-02.1`, `ATT-02.2` · **Trạng thái:** Proposed · **Blocked — policy pending** (`[OD-1.*]`, `[OD-3.*]`).

Điểm cần chú ý: sự kiện trùng trả về **2xx với `duplicate = true`**, không phải lỗi. Thiết bị offline gửi bù phải là thao tác an toàn, nếu trả lỗi thì thiết bị sẽ retry vô hạn hoặc mất dữ liệu.

```mermaid
sequenceDiagram
    autonumber
    actor E as Employee
    participant Dev as Attendance Device
    participant UI as React Attendance UI
    participant C as AttendanceEventsController
    participant S as AttendanceService
    participant Calc as TimesheetCalculator
    participant Repo as AttendanceRepository
    participant DB as PostgreSQL

    alt Chấm công từ ứng dụng
        E->>UI: Bấm check-in
        UI->>C: POST /api/v1/attendance/events + Idempotency-Key
    else Thiết bị đẩy sự kiện
        Dev->>C: POST /api/v1/integrations/attendance/events<br/>(deviceAuth + externalEventId)
    end

    C->>S: Record(actor, command)
    S->>S: Kiểm tra policy active và map employee

    alt Không có attendance_policies active
        S-->>C: Configuration conflict
        C-->>UI: 409 Problem Details<br/>"policy chưa được phê duyệt"
    else Policy hợp lệ
        S->>Repo: Resolve shift, timezone và work_date
        Repo->>DB: Read schedule + holiday + policy

        alt Kỳ công đã locked
            Repo-->>S: Period locked
            S-->>C: Stable business conflict
            C-->>UI: 409 Problem Details
        else Kỳ công còn mở
            S->>Repo: Insert attendance_event (append-only)

            alt Sự kiện đã tồn tại
                Repo-->>S: Unique violation<br/>(device hoặc idempotency key)
                S-->>C: Existing event
                C-->>Dev: 202 duplicate = true<br/>(không tạo bản ghi thứ hai)
            else Sự kiện mới
                Repo->>DB: BEGIN<br/>INSERT attendance_events<br/>UPSERT attendance_daily_records<br/>COMMIT
                S->>Calc: Recompute(employee, work_date, policy_version)
                Calc->>Calc: Áp shift, holiday, leave, overtime<br/>và rounding theo policy
                Calc-->>S: status, workedMinutes, lateMinutes
                C-->>UI: 201 + ngày công đã tính<br/>kèm policyVersion
            end
        end
    end
```

Nhánh thất bại đáng kiểm thử riêng:

| Nhánh | Kết quả mong đợi |
|---|---|
| Không có policy `active` | `409`, không ghi sự kiện — chặn tính công theo policy `draft` |
| Kỳ công `locked` | `409` với mã lỗi nghiệp vụ ổn định |
| Sự kiện trùng từ thiết bị | `202` với `duplicate = true`, **không** phải `409` |
| Chỉ có check-in đến cuối ngày | Ngày công `incomplete`, `workedMinutes = 0`, không suy diễn giờ ra |
| `employeeExternalKey` không map được | `422` và đưa vào danh sách xử lý thủ công, không im lặng bỏ qua |

## 9. Soát, duyệt và khóa kỳ công

**User Stories:** `ATT-04.2` · **Trạng thái:** Proposed · **Blocked — policy pending** (`[OD-8.*]`).

Điểm cần chú ý: kỳ công chỉ được duyệt khi **không còn ngoại lệ**, và chỉ được bàn giao Payroll khi **đã khóa**. Cả hai điều kiện đều được enforce bằng constraint, không chỉ bằng kiểm tra ở service.

```mermaid
sequenceDiagram
    autonumber
    actor HRO as HR Officer
    actor M as Line Manager
    actor HRM as HR Manager
    participant UI as React Timesheet UI
    participant C as TimesheetPeriodsController
    participant S as TimesheetPeriodService
    participant Repo as TimesheetRepository
    participant DB as PostgreSQL
    participant W as Payroll Handoff Worker

    HRO->>UI: Chuyển kỳ sang pending_approval
    UI->>C: POST /timesheet-periods/{id}/submit + If-Match
    C->>S: Submit(actor, command)
    S->>Repo: Đếm ngoại lệ trong kỳ
    Repo->>DB: Query incomplete / absent không đơn /<br/>correction pending / OT không có chấm công

    alt Còn ngoại lệ chưa xử lý
        Repo-->>S: Exception list
        S-->>C: Stable business conflict
        C-->>UI: 409 + danh sách ngoại lệ<br/>kèm nhân viên và ngày
    else Sạch ngoại lệ
        Repo->>DB: UPDATE status = pending_approval
        C-->>UI: 200 pending_approval
    end

    M->>UI: Soát bảng công đội nhóm
    UI->>C: GET /attendance/timesheets?departmentId=...
    C-->>UI: 200 (chỉ trong data scope, có phân trang)

    HRM->>UI: Duyệt rồi khóa kỳ
    UI->>C: POST /timesheet-periods/{id}/approve + If-Match
    C->>S: Approve(actor, command)
    S->>Repo: Conditional update + audit
    Repo->>DB: UPDATE status = approved<br/>SET approved_by, approved_at

    UI->>C: POST /timesheet-periods/{id}/lock + If-Match
    C->>S: Lock(actor, command)

    alt Version không khớp hoặc sai vai trò
        Repo-->>S: Conflict / forbidden
        C-->>UI: 409 hoặc 403 Problem Details
    else Khóa hợp lệ
        Repo->>DB: BEGIN<br/>UPDATE status = locked<br/>SET locked_by, locked_at<br/>INSERT audit_logs<br/>COMMIT
        C-->>UI: 200 locked
        W->>DB: Đọc kỳ đã locked
        W->>DB: SET payroll_handoff_at,<br/>payroll_handoff_reference
        Note over W,DB: ck_timesheet_period_handoff chặn<br/>bàn giao khi chưa có locked_at
    end
```

Nhánh thất bại đáng kiểm thử riêng:

| Nhánh | Kết quả mong đợi |
|---|---|
| Còn `attendance_corrections` `pending` trong kỳ | `409` kèm danh sách cụ thể, không cho duyệt |
| Khóa kỳ với `If-Match` cũ | `409`, giữ nguyên trạng thái |
| Người không phải HR Manager khóa kỳ | `403`, phân biệt rõ với `409` |
| Bàn giao Payroll khi kỳ mới `approved` | Bị chặn bởi `ck_timesheet_period_handoff` |
| Mở lại kỳ không nhập lý do | Bị chặn bởi `ck_timesheet_period_reopened` |
| Ghi nhận chấm công vào kỳ `locked` | `409` từ `[ATT-02.1]` |

## 10. Traceability

| # | Sequence | User Story / Requirement | Module | Implementation status |
|---|---|---|---|---|
| 1 | Requisition lifecycle | `REC-01.1`–`REC-01.2` | Recruitment | Proposed |
| 2 | Résumé intake | `REC-02.1`–`REC-02.2` | Recruitment | Proposed |
| 3 | Advance application | `REC-03.2` | Recruitment | **Source baseline implemented** |
| 4 | Offer acceptance | `REC-06.1`–`REC-06.2` | Recruitment → Core HR | Proposed |
| 5 | Employee movement | `EMP-04.1` | Core HR | Proposed |
| 6 | Contract lifecycle | `CON-01.1`–`CON-03.1` | Core HR / Contracts | Proposed |
| 7 | Leave submit & approval | `ATT-03.2`, `ATT-03.3` | Attendance & Leave | Proposed · **policy pending** |
| 8 | Attendance capture & timesheet calculation | `ATT-02.1`, `ATT-02.2` | Attendance & Leave | Proposed · **policy pending** |
| 9 | Timesheet review, approval & period lock | `ATT-04.2` | Attendance & Leave | Proposed · **policy pending** |

### Luồng chưa có sequence diagram

Các story sau đã có acceptance criteria nhưng chưa được vẽ sequence. Đây là khoảng trống đã biết, không phải thiếu sót bị bỏ qua:

| Story | Lý do chưa vẽ |
|---|---|
| `ATT-01.1`–`ATT-01.3` | CRUD có phê duyệt, không có nhánh runtime phức tạp; AC đã đủ để implement |
| `ATT-02.3` hiệu chỉnh công | Cùng khuôn mẫu với sequence 7 (submit → decide với `If-Match`) |
| `ATT-02.4` tăng ca | Cùng khuôn mẫu với sequence 7 |
| `EMP-06.1`–`EMP-06.2` | Sẽ vẽ khi chốt template đánh giá thử việc |
| `EMP-07.1`–`EMP-07.2` | Cần vẽ trước khi implement: có nhiều side effect (khóa tài khoản, hủy đơn nghỉ, chốt công nợ) và thứ tự thực hiện quan trọng |
