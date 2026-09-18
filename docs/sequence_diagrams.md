# Sequence Diagrams — Các Luồng Nghiệp Vụ Chính

> **Trạng thái:** Proposed runtime design. Các sequence tuân theo 3-tier và 3-layer: React Web → ASP.NET Core Presentation → Business Logic → Data Access/EF Core → PostgreSQL.
>
> **Phạm vi:** các sequence dưới đây mô tả những chức năng lá được in đậm dưới hai trụ cột **Recruitment** và **Core HR** trên bản đồ `topdown-approach.png` (xem mục 2 của [README](../README.md)), cộng luồng đăng nhập của phân hệ định danh `[ADM]` ([ADR-011](architecture.md#9-architecture-decisions-adr-index)). Không có sequence cho kiểm tra định biên/ngân sách khi phê duyệt requisition, quản lý nhiều kênh đăng tin, sơ đồ cây tổ chức, tạm hoãn/trở lại làm việc, báo cáo & phân tích hay cấu hình hệ thống — tất cả đều ngoài phạm vi.
>
> Ba sequence của phân hệ Attendance & Leave (đơn nghỉ, chấm công, khóa kỳ công) đã được tách ra ngoài phạm vi và giữ tại [deferred/attendance_leave/sequence_diagrams_att.md](deferred/attendance_leave/sequence_diagrams_att.md).

## Quy ước

- `Controller`: Presentation Layer, xử lý HTTP, DTO, authentication và Problem Details.
- `Service`: Business Logic Layer, thực thi authorization policy, workflow và transaction intent.
- `Repository`: Data Access Layer, thực thi EF Core query/transaction.
- Mọi command nhạy cảm phải ghi audit trong cùng transaction với thay đổi nghiệp vụ.
- Side effect đến hệ thống ngoài được ghi Outbox và gửi sau khi business transaction commit.
- Nhánh `alt` thể hiện success/failure có thể kiểm thử độc lập.

## 1. Requisition — tạo, phê duyệt và đăng tuyển

**User Stories:** `REC-01.1`, `REC-01.2` · **Trạng thái:** Proposed.

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
    participant CP as Trang tuyển dụng careers mặc định

    HM->>UI: Nhập và lưu requisition
    UI->>C: POST /api/v1/recruitment/requisitions
    C->>S: CreateDraft(actor, command)
    S->>S: Kiểm tra quyền và ràng buộc dữ liệu đầu vào
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

    R->>UI: Đăng tuyển
    UI->>C: POST /{id}/publish + If-Match
    C->>S: Publish(actor, id, version)
    S->>Repo: approved → active_recruiting + outbox + audit
    Repo->>DB: BEGIN<br/>UPDATE + INSERT outbox/audit<br/>COMMIT
    W->>DB: Claim publication message
    W->>CP: Hiển thị tin với idempotency key
    CP-->>W: Posting reference
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

**User Story:** `REC-03.2` · **Trạng thái:** Proposed. `RecruitmentPipelineService` trong `src/backend` đã hiện thực luồng này ở mức code-complete; chưa có integration test xác minh conditional update trên PostgreSQL thật.

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

Endpoint và token khớp `openapi.yaml`: `respondToRecruitmentOffer` với `X-Offer-Token`; phản hồi luôn là `200 OfferResponseResult`, phân biệt lần đầu và lần replay bằng `replayed`.

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

    Candidate->>UI: Chấp nhận Offer
    UI->>C: POST /api/v1/recruitment/offers/{offerId}/response<br/>{ decision: accept } + X-Offer-Token + Idempotency-Key
    C->>C: Xác thực X-Offer-Token khớp offerId, còn hạn
    C->>S: Respond(command)
    S->>Repo: Lock offer, application và candidate
    Repo->>DB: SELECT ... FOR UPDATE

    alt Token sai hoặc không khớp offer
        C-->>UI: 401 Problem Details
    else Offer không ở trạng thái sent hoặc đã expired
        S-->>C: recruitment.offer_not_open
        C-->>UI: 409 Problem Details
    else Đã có employees.source_application_id = application.id
        Repo-->>S: Existing employee, contract và tasks
        C-->>UI: 200 OfferResponseResult { replayed: true }
    else Hợp lệ — lần đầu
        S->>Repo: Accept và handoff trong một transaction
        Repo->>DB: BEGIN<br/>UPDATE offers → accepted<br/>UPDATE applications → hired_ready (version+1)<br/>INSERT application_stage_events<br/>INSERT employees (source_application_id)<br/>INSERT contracts (probation, draft)<br/>INSERT onboarding_tasks × N<br/>INSERT audit_logs<br/>INSERT outbox_messages<br/>COMMIT
        C-->>UI: 200 OfferResponseResult { replayed: false }
        W->>DB: Claim outbox messages (sau commit)
        W->>P: Provision accounts, notify HR Officer và người quản lý
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

> [!NOTE]
> Việc ký được thực hiện ngoài hệ thống rồi tải bản đã ký lên qua `POST /api/v1/contracts/{contractId}/signed-document`. Hợp đồng API không có callback hay webhook từ nhà cung cấp chữ ký số: nhà cung cấp e-signature chưa được chốt và tích hợp đó nằm ngoài phạm vi đợt này.

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

    Officer->>UI: Soạn hợp đồng hoặc phụ lục
    UI->>C: POST /api/v1/contracts
    C->>S: CreateDraft(actor, command)
    S->>Repo: Validate number, dates và active-contract overlap
    Repo->>DB: INSERT draft + audit<br/>COMMIT

    Manager->>UI: Phê duyệt
    UI->>C: POST /contracts/{id}/approve + If-Match
    C->>S: Approve(actor, id, version)
    S->>Repo: approved + outbox notification + audit
    Repo->>DB: Atomic UPDATE/INSERT<br/>COMMIT
    W->>DB: Claim notification after commit

    Employee->>Officer: Ký bản giấy hoặc bản scan
    Officer->>UI: Tải lên bản đã ký
    UI->>C: POST /contracts/{id}/signed-document
    C->>S: AttachSignedDocument(actor, id, file)
    S->>Repo: Scan file, lưu object key, chuyển executed
    Repo->>Store: PUT bản đã ký vào private storage
    Repo->>DB: UPDATE executed + audit<br/>COMMIT

    Officer->>UI: Kích hoạt hợp đồng
    UI->>C: POST /contracts/{id}/activate + If-Match
    C->>S: Activate(actor, id, version)
    S->>Repo: Đóng hợp đồng chính cũ, mở hợp đồng mới
    Repo->>DB: Atomic UPDATE + audit<br/>COMMIT

    W->>Repo: Find contracts at alert thresholds
    Repo->>DB: Bounded expiry query
    W->>DB: Insert deduplicated notification outbox
```

## 7. Thôi việc và bàn giao

**User Stories:** `EMP-07.1`, `EMP-07.2` · **Trạng thái:** Proposed.

Luồng này có nhiều side effect và thứ tự giữa chúng là quy tắc nghiệp vụ, không phải chi tiết kỹ thuật: checklist chỉ sinh sau khi
case được duyệt, `employee_events` termination chỉ được ghi khi hoàn tất, và tài khoản chỉ bị vô hiệu hoá đúng `lastWorkingDate`
chứ không phải lúc bấm hoàn tất.

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

    Officer->>UI: Mở hồ sơ thôi việc (loại, ngày làm việc cuối, người nhận bàn giao, lý do)
    UI->>C: POST /api/v1/offboarding/cases
    C->>S: Open(actor, command)
    S->>S: Kiểm tra employee đang active hoặc probation<br/>handoverToEmployeeId ≠ employeeId

    alt Nhân viên đã có case đang mở
        Repo->>DB: INSERT vi phạm ux_offboarding_open_case
        DB-->>Repo: unique violation
        S-->>C: Conflict
        C-->>UI: 409 Problem Details
    else Hợp lệ
        Repo->>DB: INSERT case draft + audit (cùng transaction)
        S->>S: Tính noticePeriodShortfallDays
        Note over S,C: Thiếu thời hạn báo trước là **cảnh báo**, không chặn —<br/>quyết định thuộc HR Manager, cảnh báo được ghi audit
        C-->>UI: 201 Created + ETag + noticePeriodShortfallDays
    end

    Mgr->>UI: Phê duyệt hồ sơ
    UI->>C: POST /offboarding/cases/{id}/approve + If-Match
    C->>S: Act(actor, Approve, version)
    S->>S: Sinh checklist 5 nhóm it/admin/hr/manager/finance từ template
    Repo->>DB: Conditional UPDATE WHERE version = expected<br/>+ INSERT tasks (bỏ qua template_key đã có) + audit
    Note over Repo,DB: ux_offboarding_task_template khiến việc duyệt lại<br/>không sinh task trùng
    C-->>UI: 200 + ETag mới

    loop Từng task bàn giao / thu hồi
        Mgr->>UI: start / complete task
        UI->>C: POST /offboarding/tasks/{taskId}/{action} + If-Match
        C->>TS: Act(actor, action, version)
        TS->>Repo: Conditional UPDATE + audit
    end

    Officer->>UI: Hoàn tất hồ sơ
    UI->>C: POST /offboarding/cases/{id}/complete + If-Match
    C->>S: Act(actor, Complete, version, reason?)
    S->>Repo: ListBlockingTasks(caseId)

    alt Còn task blocks_last_working_day chưa xong và actor không có quyền approve
        S-->>C: BusinessRule blocking_tasks_outstanding
        C-->>UI: 409 + danh sách task đang chặn
    else Bỏ qua task chặn (cần quyền approve + lý do)
        S->>S: Đánh dấu overridden, lý do bắt buộc ghi audit
    end

    alt finalSettlementStatus chưa paid/waived
        S-->>C: BusinessRule settlement_pending
        C-->>UI: 409 — **không** có đường bỏ qua kiểm tra này
        Note over S: Nguồn cập nhật finalSettlementStatus thuộc Payroll (ngoài phạm vi)
    else Đã chốt công nợ
        S->>Repo: SaveCompletion(case completed + termination event)
        Repo->>DB: UPDATE case + INSERT employee_events<br/>(termination, approved, effective = lastWorkingDate)<br/>+ audit + outbox corehr.offboarding.case_completed<br/>MỘT transaction
        C-->>UI: 200 + ETag mới
    end

    W->>DB: Claim outbox corehr.offboarding.case_completed
    W->>DB: Đến lastWorkingDate: apply employee_events → employees.status = resigned/terminated
    Note over W,DB: Tài khoản chỉ chuyển disabled đúng lastWorkingDate, không sớm hơn.<br/>Worker host chưa tồn tại — xem architecture.md §5.4
```

## 8. Đăng nhập, làm mới phiên và phát hiện token bị đánh cắp

**User Stories:** `ADM-01.1`, `ADM-01.2` · **Trạng thái:** Proposed.

Luồng này là tiền đề của cả sáu sequence trên: mọi `Controller` ở đó đều giả định đã có một actor đã xác thực kèm permission và data scope. Ba nhánh dưới đây là ba nhánh có thể kiểm thử độc lập: sai mật khẩu, khoá tạm, và trình lại refresh token đã dùng.

```mermaid
sequenceDiagram
    autonumber
    actor U as Người dùng nội bộ
    participant UI as React Auth UI
    participant C as AuthController
    participant S as AuthenticationService
    participant H as Pbkdf2PasswordHasher
    participant T as JwtAccessTokenIssuer
    participant Repo as AuthenticationRepository
    participant DB as PostgreSQL

    U->>UI: Nhập email và mật khẩu
    UI->>C: POST /api/v1/auth/login
    C->>S: SignIn(email, password, clientContext)
    S->>Repo: FindByEmail(email đã chuẩn hoá)
    Repo->>DB: SELECT users ⋈ user_credentials ⋈ employees
    DB-->>Repo: Account + credential
    Repo-->>S: UserSignInRecord

    alt Email không tồn tại hoặc không có credential
        S->>H: Verify(DummyHash, password)
        Note over S,H: Vẫn tốn đúng thời gian như một lần verify thật<br/>nên không dò được tài khoản nào tồn tại
        S->>Repo: RecordFailedSignIn(invalid_credentials)
        Repo->>DB: BEGIN<br/>INSERT audit_logs (result='rejected')<br/>COMMIT
        S-->>C: AuthenticationFailedException
        C-->>UI: 401 invalid_credentials
    else Đang trong cửa sổ khoá
        S-->>C: AccountLocked(retryAfterSeconds)
        Note over S: Không verify mật khẩu và không tăng bộ đếm
        C-->>UI: 401 account_locked
    else Sai mật khẩu
        S->>H: Verify(storedHash, password) → Failed
        S->>Repo: RecordFailedSignIn(attempts+1, locked_until nếu đạt 5)
        Repo->>DB: BEGIN<br/>UPDATE user_credentials<br/>INSERT audit_logs (rejected)<br/>COMMIT
        C-->>UI: 401 invalid_credentials
    else Mật khẩu đúng
        S->>H: Verify(storedHash, password) → Succeeded
        alt users.status = 'disabled'
            S->>Repo: RecordFailedSignIn(account_disabled)
            C-->>UI: 401 account_disabled
        else must_change_password
            S->>T: Issue(identity hạn chế: không permission, 10 phút)
            S->>Repo: RecordSuccessfulSignIn(không refresh token)
            C-->>UI: 200 Session (không refreshToken, passwordChangeRequired)
        else Bình thường
            S->>Repo: GetAuthorization(userId)
            Repo->>DB: SELECT user_roles ⋈ role_permissions
            DB-->>Repo: Grants + permissions
            S->>T: Issue(identity đầy đủ)
            S->>Repo: RecordSuccessfulSignIn(refresh token mới)
            Repo->>DB: BEGIN<br/>UPDATE user_credentials (reset bộ đếm, last_login_at)<br/>INSERT refresh_tokens (chỉ lưu SHA-256)<br/>INSERT audit_logs (succeeded)<br/>COMMIT
            C-->>UI: 200 Session (accessToken + refreshToken)
        end
    end

    Note over UI: ~1 phút trước khi access token hết hạn
    UI->>C: POST /api/v1/auth/refresh
    C->>S: Refresh(refreshToken)
    S->>Repo: FindRefreshToken(SHA-256(token))
    alt Token đã bị thu hồi — dấu hiệu bị đánh cắp
        S->>Repo: RevokeAllRefreshTokens(reuse_detected)
        Repo->>DB: BEGIN<br/>UPDATE refresh_tokens SET revoked_at<br/>INSERT audit_logs (rejected)<br/>COMMIT
        C-->>UI: 401 invalid_refresh_token
        Note over UI: Cả người dùng thật và kẻ tấn công đều phải đăng nhập lại
    else Token còn hiệu lực
        S->>Repo: GetAuthorization(userId)
        Note over S,Repo: Đọc lại quyền từ DB nên vai trò vừa bị thu hồi<br/>không còn trong token mới
        S->>Repo: RotateRefreshToken(tokenId, successor)
        Repo->>DB: BEGIN<br/>UPDATE ... WHERE id=@id AND revoked_at IS NULL<br/>INSERT refresh_tokens (successor)<br/>UPDATE replaced_by_token_id<br/>INSERT audit_logs<br/>COMMIT
        Note over Repo,DB: Điều kiện revoked_at IS NULL khiến hai request<br/>song song cùng token chỉ một cái thắng
        C-->>UI: 200 Session mới
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
| 7 | Thôi việc và bàn giao | `EMP-07.1`–`EMP-07.2` | Core HR | Proposed |
| 8 | Sign-in và refresh rotation | `ADM-01.1`–`ADM-01.2` | Identity & Access | Proposed |

### Luồng chưa có sequence diagram

Các story sau đã có acceptance criteria nhưng chưa được vẽ sequence. Đây là khoảng trống đã biết, không phải thiếu sót bị bỏ qua:

| Story | Lý do chưa vẽ |
|---|---|
| `REC-03.1`, `REC-04.1`, `REC-05.1` | Luồng đọc pipeline, xếp lịch phỏng vấn và nộp scorecard đã đủ rõ trong acceptance criteria; chỉ vẽ nếu phát sinh tranh chấp về thứ tự gửi lời mời và khóa bản ghi đánh giá |
| `EMP-01.1`–`EMP-01.2` | Luồng tra cứu và tự cập nhật hồ sơ là CRUD một bước, quy tắc quan trọng nằm ở field policy và data scope chứ không ở thứ tự tương tác |
| `EMP-02.1` | Quản lý danh mục phòng ban, chức danh và phân công là CRUD có `If-Match`; sẽ vẽ nếu bổ sung luồng tái cấu trúc phòng ban hàng loạt |
| `EMP-03.1` | Việc sinh checklist onboarding đã nằm trong sequence 4; phần theo dõi và nhắc hạn là công việc nền đơn giản |
| `EMP-05.1` | Luồng upload và phát Signed URL sẽ vẽ chung với chuẩn lưu trữ tài liệu |
| `EMP-06.1`–`EMP-06.2` | Sẽ vẽ khi chốt template đánh giá thử việc |
| `ADM-02.1`–`ADM-02.2` | Cấp tài khoản và thay vai trò là CRUD có `If-Match`; quy tắc quan trọng nằm ở kiểm tra tham chiếu và rào tự-quản-trị, không ở thứ tự tương tác. Hệ quả duy nhất có thứ tự — vô hiệu hoá tài khoản thu hồi refresh token — đã thể hiện ở sequence 8 |
