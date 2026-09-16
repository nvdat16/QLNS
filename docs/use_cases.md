# Use Cases — Tổng Quan và Các Chức Năng Quản Lý Chính

> **Trạng thái:** Proposed business design. Các mã trong ngoặc vuông truy vết tới `user_stories.md` hoặc `functional_specifications.md`. Chưa use case nào được xem là implemented; source trong `src/` chỉ là skeleton cấu trúc.

## Quy ước

- Đường liền từ actor tới use case: actor trực tiếp khởi tạo hoặc tham gia.
- `<<include>>`: hành vi bắt buộc được dùng lại trong use case nguồn.
- `<<extend>>`: hành vi có điều kiện hoặc tùy chọn.
- Hệ thống ngoài được đặt ngoài biên QLNS.
- Kiểm tra quyền và ghi audit là hành vi dùng chung; chi tiết policy vẫn thuộc requirements và kiến trúc.

## 1. Use Case Tổng Quát

Sơ đồ dưới đây thể hiện các actor và nhóm chức năng quản lý chính trong toàn bộ QLNS. Các phần tiếp theo phân rã từng nhóm thành use case chi tiết.


```mermaid
flowchart LR
    Admin([Super Admin])
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
        Reports[View workforce reports]
        Access[Manage accounts & RBAC]
    end

    Recruiter --> Jobs
    Recruiter --> ATS
    Recruiter --> Interviews
    Interviewer --> Jobs
    Interviewer --> Interviews
    HRMgr --> Offers
    HRMgr --> Records
    HRMgr --> Reports
    HROfficer --> Records
    User --> ATS
    User --> Records
    Admin --> Access
    Admin --> Reports
```

---

## 2. Quản lý tuyển dụng ATS

```mermaid
flowchart LR
    Candidate(["👤 Ứng viên"])
    HiringManager(["👤 Trưởng bộ phận"])
    Recruiter(["👤 Recruiter"])
    HRManager(["👤 HR Manager"])
    Interviewer(["👤 Người phỏng vấn"])
    JobBoard["Hệ thống ngoài<br/>Job Board"]
    Communication["Hệ thống ngoài<br/>Email / Calendar"]

    subgraph QLNS_ATS["QLNS — Quản lý Tuyển dụng"]
        direction TB
        UC_REQ_DRAFT(["Tạo requisition nháp<br/>[REC-01.1]"])
        UC_REQ_SUBMIT(["Gửi requisition phê duyệt<br/>[REC-01.2]"])
        UC_REQ_DECIDE(["Phê duyệt / Từ chối requisition<br/>[REC-01.3]"])
        UC_PUBLISH(["Đăng tin tuyển dụng<br/>[REC-01.4]"])
        UC_APPROVED_CHECK(["Kiểm tra requisition đã Approved"])
        UC_UPLOAD(["Nộp / Upload CV an toàn<br/>[REC-02.1]"])
        UC_SCAN(["Kiểm tra file và malware"])
        UC_PARSE(["Bóc tách CV và xác nhận dữ liệu<br/>[REC-02.2]"])
        UC_PIPELINE(["Xem pipeline theo giai đoạn<br/>[REC-03.1]"])
        UC_ADVANCE(["Chuyển ứng viên một giai đoạn<br/>[REC-03.2]"])
        UC_REJECT(["Từ chối ứng viên có lý do<br/>[REC-03.3]"])
        UC_ELIGIBILITY(["Kiểm tra điều kiện chuyển vòng"])
        UC_INTERVIEW(["Xếp / Đổi / Hủy lịch phỏng vấn<br/>[REC-04.1]"])
        UC_SCORE(["Nộp scorecard<br/>[REC-05.1]"])
        UC_OFFER(["Lập và phê duyệt Offer<br/>[REC-06.1]"])
        UC_ACCEPT(["Phản hồi Offer<br/>[REC-06.2]"])
        UC_HANDOFF(["Khởi tạo hồ sơ và onboarding"])
        UC_AUTH(["Xác thực quyền và data scope"])
        UC_AUDIT(["Ghi lịch sử / Audit"])

        UC_REQ_SUBMIT -. "<<include>>" .-> UC_AUTH
        UC_REQ_DECIDE -. "<<include>>" .-> UC_AUTH
        UC_PUBLISH -. "<<include>>" .-> UC_APPROVED_CHECK
        UC_UPLOAD -. "<<include>>" .-> UC_SCAN
        UC_PARSE -. "<<extend>> khi file sạch" .-> UC_UPLOAD
        UC_ADVANCE -. "<<include>>" .-> UC_ELIGIBILITY
        UC_ADVANCE -. "<<include>>" .-> UC_AUTH
        UC_ADVANCE -. "<<include>>" .-> UC_AUDIT
        UC_REJECT -. "<<include>>" .-> UC_AUDIT
        UC_SCORE -. "<<include>>" .-> UC_AUTH
        UC_OFFER -. "<<include>>" .-> UC_AUTH
        UC_HANDOFF -. "<<extend>> khi Accepted" .-> UC_ACCEPT
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
    UC_PUBLISH --> JobBoard
    UC_INTERVIEW --> Communication
    UC_REJECT --> Communication
    UC_OFFER --> Communication
```

### Ranh giới nghiệp vụ chính

- Requisition chỉ được đăng sau khi HR Manager phê duyệt.
- Application chỉ tiến đúng một stage; nhảy/lùi stage và ghi đè version cũ bị từ chối.
- Vào vòng phỏng vấn cần lịch hợp lệ; vào Offer cần đánh giá đủ điều kiện.
- Candidate-to-employee handoff phải idempotent, không tạo trùng nhân viên, hợp đồng hoặc checklist.

## 3. Quản lý hồ sơ và vòng đời nhân sự

```mermaid
flowchart LR
    Employee(["👤 Nhân viên"])
    HROfficer(["👤 HR Officer"])
    HRManager(["👤 HR Manager"])
    LineManager(["👤 Line Manager"])
    TaskOwner(["👤 IT / Admin / Task Owner"])
    ObjectStorage["Hệ thống ngoài<br/>Private Object Storage"]

    subgraph QLNS_CORE["QLNS — Core HR & Employee Lifecycle"]
        direction TB
        UC_SEARCH(["Tìm kiếm / Lọc danh bạ<br/>[EMP-01.1]"])
        UC_VIEW(["Xem hồ sơ theo phạm vi<br/>[EMP-01.2]"])
        UC_SELF_CHANGE(["Đề nghị sửa thông tin cá nhân"])
        UC_ORG(["Xem cơ cấu tổ chức<br/>[EMP-02.1]"])
        UC_ORG_MGMT(["Quản lý phòng ban / vị trí"])
        UC_ONBOARD(["Theo dõi onboarding checklist<br/>[EMP-03.1]"])
        UC_TASK(["Nhận và hoàn thành onboarding task"])
        UC_MOVEMENT(["Tạo đề xuất biến động nhân sự<br/>[EMP-04.1]"])
        UC_MOVEMENT_DECIDE(["Phê duyệt / Hủy biến động"])
        UC_APPLY(["Áp dụng biến động đúng ngày hiệu lực"])
        UC_DOCUMENT(["Upload / Phiên bản hóa tài liệu<br/>[EMP-05.1]"])
        UC_DOWNLOAD(["Truy cập tài liệu bằng URL có hạn"])
        UC_PROBATION(["Đánh giá kết quả thử việc<br/>[EMP-06.1]"])
        UC_PROBATION_DECIDE(["Quyết định hết thử việc<br/>[EMP-06.2]"])
        UC_OFFBOARD(["Khởi tạo hồ sơ thôi việc<br/>[EMP-07.1]"])
        UC_OFFBOARD_TASK(["Bàn giao, thu hồi tài sản / tài khoản<br/>[EMP-07.2]"])
        UC_OFFBOARD_CLOSE(["Đóng case & vô hiệu hóa tài khoản"])
        UC_AUTH(["Xác thực quyền, field và data scope"])
        UC_AUDIT(["Ghi audit trước / sau"])

        UC_SEARCH -. "<<include>>" .-> UC_AUTH
        UC_VIEW -. "<<include>>" .-> UC_AUTH
        UC_SELF_CHANGE -. "<<extend>>" .-> UC_VIEW
        UC_ORG_MGMT -. "<<include>>" .-> UC_AUTH
        UC_ONBOARD -. "<<include>>" .-> UC_TASK
        UC_TASK -. "<<include>>" .-> UC_AUDIT
        UC_MOVEMENT -. "<<include>>" .-> UC_AUTH
        UC_MOVEMENT_DECIDE -. "<<include>>" .-> UC_AUDIT
        UC_APPLY -. "<<extend>> khi Approved và đến hạn" .-> UC_MOVEMENT_DECIDE
        UC_DOCUMENT -. "<<include>>" .-> UC_AUTH
        UC_DOWNLOAD -. "<<extend>>" .-> UC_DOCUMENT
        UC_DOWNLOAD -. "<<include>>" .-> UC_AUDIT
        UC_PROBATION -. "<<include>>" .-> UC_AUTH
        UC_PROBATION_DECIDE -. "<<include>>" .-> UC_PROBATION
        UC_PROBATION_DECIDE -. "<<include>>" .-> UC_AUDIT
        UC_MOVEMENT -. "<<extend>> sinh sự kiện từ kết quả" .-> UC_PROBATION_DECIDE
        UC_OFFBOARD -. "<<include>>" .-> UC_AUTH
        UC_OFFBOARD -. "<<include>>" .-> UC_OFFBOARD_TASK
        UC_OFFBOARD_CLOSE -. "<<extend>> khi task chặn đã xong" .-> UC_OFFBOARD_TASK
        UC_OFFBOARD_CLOSE -. "<<include>>" .-> UC_AUDIT
    end

    Employee --> UC_VIEW
    Employee --> UC_SELF_CHANGE
    Employee --> UC_ORG
    HROfficer --> UC_SEARCH
    HROfficer --> UC_VIEW
    HROfficer --> UC_ONBOARD
    HROfficer --> UC_MOVEMENT
    HROfficer --> UC_DOCUMENT
    HRManager --> UC_MOVEMENT_DECIDE
    HRManager --> UC_ORG_MGMT
    LineManager --> UC_ORG
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

### Ranh giới nghiệp vụ chính

- Nhân viên chỉ xem/sửa trường được phép của chính mình; HR vẫn bị giới hạn bởi data scope và field allowlist.
- Phòng ban, chức danh, trạng thái và quản lý trực tiếp không được sửa thẳng trên hồ sơ; phải qua employee event.
- Event đã áp dụng không bị xóa/sửa lịch sử; thay đổi ngược dùng compensating event.
- Tài liệu luôn private, được kiểm tra an toàn và chỉ tải qua quyền truy cập có thời hạn.
- Mỗi hợp đồng thử việc có đúng một phiếu đánh giá; kết quả chỉ vào hồ sơ qua employee event đã phê duyệt.
- Phiếu đánh giá thử việc quá hạn là rủi ro pháp lý, không chỉ là trễ quy trình — phải cảnh báo riêng.
- Mỗi nhân viên chỉ có một case thôi việc đang mở; không đóng case khi còn task chặn hoặc chưa chốt công nợ.
- Tài khoản chỉ bị vô hiệu hóa đúng ngày làm việc cuối, không sớm hơn, để nhân viên còn hoàn thành bàn giao.

## 4. Quản lý hợp đồng lao động

```mermaid
flowchart LR
    Employee(["👤 Nhân viên"])
    HROfficer(["👤 HR Officer / C&B"])
    HRManager(["👤 HR Manager"])
    Worker(["⚙️ Background Worker"])
    ESign["Hệ thống ngoài<br/>E-signature"]
    Notification["Hệ thống ngoài<br/>Email / Notification"]

    subgraph QLNS_CONTRACT["QLNS — Contract Management"]
        direction TB
        UC_VIEW(["Xem hợp đồng được phép"])
        UC_DRAFT(["Soạn hợp đồng nháp<br/>[CON-01.1]"])
        UC_VALIDATE(["Kiểm tra số, thời hạn và overlap"])
        UC_APPROVE(["Phê duyệt hợp đồng"])
        UC_SIGN(["Gửi ký / Đối soát chữ ký"])
        UC_ACTIVATE(["Thực thi / Kích hoạt hợp đồng"])
        UC_MONITOR(["Quét hợp đồng sắp hết hạn<br/>[CON-02.1]"])
        UC_ALERT(["Tạo cảnh báo không trùng"])
        UC_ADDENDUM(["Soạn phụ lục hợp đồng<br/>[CON-03.1]"])
        UC_ADDENDUM_EFFECT(["Áp dụng phụ lục và tạo employee event"])
        UC_AUTH(["Kiểm tra quyền và field scope"])
        UC_AUDIT(["Ghi audit và phiên bản"])

        UC_VIEW -. "<<include>>" .-> UC_AUTH
        UC_DRAFT -. "<<include>>" .-> UC_VALIDATE
        UC_APPROVE -. "<<include>>" .-> UC_AUTH
        UC_APPROVE -. "<<include>>" .-> UC_AUDIT
        UC_SIGN -. "<<extend>> sau Approved" .-> UC_APPROVE
        UC_ACTIVATE -. "<<extend>> khi có bằng chứng ký" .-> UC_SIGN
        UC_MONITOR -. "<<include>>" .-> UC_ALERT
        UC_ADDENDUM -. "<<include>>" .-> UC_VALIDATE
        UC_ADDENDUM_EFFECT -. "<<extend>> khi Approved/Signed" .-> UC_ADDENDUM
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

### Ranh giới nghiệp vụ chính

- Số hợp đồng/phụ lục là duy nhất; hợp đồng có thời hạn phải có ngày kết thúc sau ngày bắt đầu.
- Mặc định một nhân viên chỉ có một hợp đồng chính đang hiệu lực.
- Phụ lục không sửa nội dung hợp đồng gốc và phải giữ before/after, phê duyệt, chữ ký, phiên bản.
- Lỗi gửi cảnh báo không rollback trạng thái hợp đồng; delivery được retry hữu hạn.

## 5. Báo cáo, quản trị và dịch vụ dùng chung

```mermaid
flowchart LR
    HRManager(["👤 HR Manager"])
    Recruiter(["👤 Recruiter"])
    Admin(["👤 System Administrator"])
    Auditor(["👤 Auditor"])
    Worker(["⚙️ Background Worker"])
    IdP["Hệ thống ngoài<br/>Identity Provider"]
    Provider["Hệ thống ngoài<br/>Email / Calendar / Job Board"]

    subgraph QLNS_SHARED["QLNS — Reporting & Administration"]
        direction TB
        UC_HEADCOUNT(["Xem dashboard quân số<br/>[REP-01]"])
        UC_FUNNEL(["Xem recruitment funnel<br/>[REP-02]"])
        UC_FILTER(["Lọc theo thời gian / đơn vị / vị trí"])
        UC_EXPORT(["Xuất CSV / Excel / PDF"])
        UC_PROTECT(["Mask field / Watermark / Data scope"])
        UC_ACCOUNT(["Ánh xạ tài khoản từ IdP<br/>[SYS-01]"])
        UC_ROLE(["Cấp / Thu hồi role và data scope<br/>[SYS-01]"])
        UC_AUDIT(["Tra cứu audit log<br/>[SYS-03]"])
        UC_DELIVERY(["Theo dõi / Retry delivery<br/>[SYS-02]"])
        UC_INTEGRATION(["Quản lý cấu hình tích hợp<br/>[SYS-04]"])
        UC_HEALTH(["Theo dõi health / readiness"])

        UC_HEADCOUNT -. "<<include>>" .-> UC_FILTER
        UC_FUNNEL -. "<<include>>" .-> UC_FILTER
        UC_EXPORT -. "<<extend>>" .-> UC_HEADCOUNT
        UC_EXPORT -. "<<extend>>" .-> UC_FUNNEL
        UC_EXPORT -. "<<include>>" .-> UC_PROTECT
        UC_ROLE -. "<<include>>" .-> UC_AUDIT
        UC_DELIVERY -. "<<include>>" .-> UC_AUDIT
        UC_INTEGRATION -. "<<include>>" .-> UC_AUDIT
    end

    HRManager --> UC_HEADCOUNT
    HRManager --> UC_FUNNEL
    HRManager --> UC_EXPORT
    Recruiter --> UC_FUNNEL
    Admin --> UC_ACCOUNT
    Admin --> UC_ROLE
    Admin --> UC_DELIVERY
    Admin --> UC_INTEGRATION
    Admin --> UC_HEALTH
    Auditor --> UC_AUDIT
    IdP --> UC_ACCOUNT
    Worker --> UC_DELIVERY
    UC_DELIVERY --> Provider
    UC_INTEGRATION --> Provider
```

### Ranh giới nghiệp vụ chính

- KPI phải công bố công thức, thời điểm làm mới và filter đang áp dụng.
- Báo cáo và tổng số không được làm lộ dữ liệu ngoài data scope.
- Export dữ liệu nhạy cảm cần permission riêng, watermark và audit.
- QLNS lưu ánh xạ actor/role/data scope; IdP chịu trách nhiệm xác thực danh tính và phát token.

## 6. Ma trận actor — nhóm chức năng

| Actor | Tuyển dụng | Core HR | Hợp đồng | Báo cáo / Quản trị |
|---|---|---|---|---|
| Candidate | Nộp CV, phản hồi Offer | — | — | — |
| Employee | — | Hồ sơ cá nhân, sơ đồ tổ chức, bàn giao khi thôi việc | Xem/ký hợp đồng | — |
| Hiring/Line Manager | Requisition, phỏng vấn | Cơ cấu đội ngũ, onboarding, đánh giá thử việc, xác nhận bàn giao | — | Báo cáo theo scope |
| Recruiter | Pipeline, lịch, scorecard, Offer | — | — | Recruitment analytics |
| HR Officer / C&B | Hỗ trợ tiếp nhận | Hồ sơ, onboarding, biến động, tài liệu, khởi tạo & đóng case thôi việc | Soạn hợp đồng/phụ lục | Export theo quyền |
| HR Manager | Phê duyệt requisition/Offer | Phê duyệt biến động, quyết định hết thử việc, phê duyệt case thôi việc | Phê duyệt hợp đồng/phụ lục | Dashboard toàn quyền HR |
| System Admin | — | Không mặc định xem dữ liệu HR; vô hiệu hóa tài khoản khi thôi việc | — | Account, role, integration, health |
| Auditor | — | — | — | Audit read-only theo mandate |

Chấm công / nghỉ phép không còn là một cột ở đây vì nhóm chức năng đó nằm ngoài phạm vi triển khai; use case của nó được giữ tại [deferred/attendance_leave/use_cases_att.md](deferred/attendance_leave/use_cases_att.md).

System Admin không mặc nhiên có quyền đọc hồ sơ, lương hoặc hợp đồng; quyền vận hành và quyền dữ liệu nghiệp vụ phải tách biệt.
