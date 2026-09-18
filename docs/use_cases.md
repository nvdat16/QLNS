# Use Cases — Tổng Quan và Các Chức Năng Quản Lý Chính

> **Trạng thái:** Proposed business design. Các mã trong ngoặc vuông truy vết tới `user_stories.md` hoặc `functional_specifications.md`. Backend trong `src/backend` đã **code-complete** cho mọi use case dưới đây, nhưng chưa use case nào đạt `implemented`: còn thiếu integration test trên PostgreSQL thật.

## Quy ước

- Đường liền từ actor tới use case: actor trực tiếp khởi tạo hoặc tham gia.
- `<<include>>`: hành vi bắt buộc được dùng lại trong use case nguồn.
- `<<extend>>`: hành vi có điều kiện hoặc tùy chọn.
- Hệ thống ngoài được đặt ngoài biên QLNS.
- Kiểm tra quyền và ghi audit là hành vi dùng chung; chi tiết policy vẫn thuộc requirements và kiến trúc.

## 1. Use Case Tổng Quát

Sơ đồ dưới đây thể hiện các actor và nhóm chức năng quản lý **thuộc phạm vi giao hàng** của QLNS: hai trụ cột Recruitment và Core HR (gồm Contract Management) theo `topdown-approach.png`, mục 2 của [README.md](../README.md). Các phần tiếp theo phân rã từng nhóm thành use case chi tiết.


```mermaid
flowchart LR
    HRMgr([HR Director / Manager])
    Recruiter([Recruiter])
    Interviewer([Hiring Manager / Interviewer])
    HROfficer([HR Officer])
    LineMgr([Line Manager])
    User([Employee / Candidate])
    Admin([Super Admin])

    subgraph HRMS["QLNS / HRMS"]
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

Phạm vi đợt này **có** use case đăng nhập và quản trị tài khoản/vai trò (mục 6, phân hệ `[ADM]`), nhưng không có use case báo cáo, phân tích, cấu hình hệ thống hay tra cứu nhật ký kiểm toán; cũng không có use case xem cây tổ chức trực quan và tạm hoãn/trở lại làm việc. Ghi nhật ký kiểm toán và gửi thông báo qua outbox vẫn là hành vi bắt buộc của mọi use case nghiệp vụ, chỉ các màn hình và API tra cứu tương ứng là ngoài phạm vi. Actor **Super Admin** sở hữu đúng nhóm use case định danh và **không** có quyền đọc dữ liệu nghiệp vụ nào.

---

## 2. Quản lý tuyển dụng ATS

```mermaid
flowchart LR
    Candidate(["👤 Ứng viên"])
    HiringManager(["👤 Trưởng bộ phận"])
    Recruiter(["👤 Recruiter"])
    HRManager(["👤 HR Manager"])
    Interviewer(["👤 Người phỏng vấn"])
    Careers["Hệ thống ngoài<br/>Cổng Careers (kênh mặc định)"]
    Communication["Hệ thống ngoài<br/>Email / Calendar"]

    subgraph QLNS_ATS["QLNS — Quản lý Tuyển dụng"]
        direction TB
        UC_REQ_DRAFT(["Tạo requisition nháp<br/>[REC-01.1]"])
        UC_REQ_SUBMIT(["Gửi requisition phê duyệt<br/>[REC-01.2]"])
        UC_REQ_DECIDE(["Phê duyệt / Từ chối requisition<br/>[REC-01.3]"])
        UC_PUBLISH(["Đăng / Cập nhật / Đóng tin tuyển dụng<br/>[REC-01.4]"])
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
    UC_PUBLISH --> Careers
    UC_INTERVIEW --> Communication
    UC_REJECT --> Communication
    UC_OFFER --> Communication
```

### Ranh giới nghiệp vụ chính

- Requisition chỉ được đăng sau khi HR Manager phê duyệt; phê duyệt là quyết định của người có thẩm quyền, hệ thống không tự kiểm tra định biên hay quỹ lương.
- Tin tuyển dụng chỉ phát hành trên một cổng careers mặc định; việc chọn và quản lý nhiều kênh đăng tin không thuộc phạm vi.
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
        UC_ORG_MGMT(["Quản lý phòng ban, chức danh, phân công và reporting line<br/>[EMP-02.1]"])
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

### Ranh giới nghiệp vụ chính

- Nhân viên chỉ xem/sửa trường được phép của chính mình; HR vẫn bị giới hạn bởi data scope và field allowlist.
- Phòng ban, chức danh, trạng thái và quản lý trực tiếp không được sửa thẳng trên hồ sơ; phải qua employee event.
- Phân cấp phòng ban cha – con, ràng buộc chống chu trình và ràng buộc xóa phòng ban vẫn được kiểm soát ở tầng dữ liệu, nhưng không có use case hiển thị cây tổ chức.
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

## 5. Theo dõi vận hành hệ thống

Đợt giao hàng này không có use case báo cáo hay phân tích. Quản trị tài khoản và vai trò được đặc tả ở mục 6; phần còn lại của trụ cột System Administration chỉ còn việc theo dõi tình trạng hoạt động của dịch vụ — đây là hạ tầng phục vụ triển khai và giám sát, không phải chức năng nghiệp vụ trên bản đồ chức năng.

```mermaid
flowchart LR
    Admin(["👤 System Administrator"])
    Monitor["Hệ thống ngoài<br/>Monitoring / Load Balancer"]

    subgraph QLNS_OPS["QLNS — Operations"]
        direction TB
        UC_HEALTH(["Theo dõi health / readiness"])
    end

    Admin --> UC_HEALTH
    Monitor --> UC_HEALTH
```

### Ranh giới nghiệp vụ chính

- `GET /health/live` và `GET /health/ready` là endpoint hạ tầng, không trả dữ liệu nghiệp vụ và không yêu cầu quyền nghiệp vụ.
- Quản lý tài khoản, vai trò và phạm vi dữ liệu **thuộc phạm vi** và được đặc tả riêng ở mục 6. Cấu hình tích hợp/thông báo (nằm trong `appsettings`) và màn hình tra cứu nhật ký kiểm toán vẫn ngoài phạm vi.
- Ghi nhật ký kiểm toán trong cùng transaction với thay đổi nghiệp vụ và gửi thông báo qua transactional outbox **vẫn bắt buộc** với mọi use case ở các mục 2, 3 và 4; chỉ màn hình và API tra cứu/retry tương ứng là ngoài phạm vi.

## 6. Định danh & phân quyền

```mermaid
flowchart LR
    AnyUser(["👤 Người dùng nội bộ<br/>(mọi vai trò)"])
    Admin(["👤 Super Admin"])

    subgraph QLNS_ADM["QLNS — Identity & Access"]
        direction TB
        UC_SIGN_IN(["Đăng nhập bằng email & mật khẩu<br/>[ADM-01.1]"])
        UC_SESSION(["Duy trì & kết thúc phiên<br/>[ADM-01.2]"])
        UC_CHANGE_PWD(["Tự đổi mật khẩu<br/>[ADM-01.2]"])
        UC_ACCOUNT_PROVISION(["Cấp tài khoản & vai trò<br/>[ADM-02.1]"])
        UC_ACCOUNT_REVOKE(["Thu hồi & điều chỉnh quyền<br/>[ADM-02.2]"])
        UC_AUDIT_WRITE(["Ghi nhật ký kiểm toán"])
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

### Ranh giới nghiệp vụ chính

- `UC_SIGN_IN` là **tiền điều kiện của mọi use case** ở các mục 2, 3 và 4: những use case đó đều `<<include>>` `UC_AUTH`, và `UC_AUTH` giờ được hiện thực bởi chính hệ thống chứ không bởi Identity Provider bên ngoài.
- Mọi lần đăng nhập, kể cả thất bại, đều ghi nhật ký kiểm toán — đây là use case duy nhất mà một lần **thất bại** cũng phải để lại vết.
- `UC_ACCOUNT_REVOKE` `<<extend>>` `UC_SESSION`: vô hiệu hoá tài khoản hoặc đặt lại mật khẩu sẽ chấm dứt các phiên đang mở của tài khoản đó.
- Super Admin **không** có quyền đọc hồ sơ, hợp đồng hay dữ liệu tuyển dụng; và không được tự vô hiệu hoá, đặt lại mật khẩu hay đổi vai trò của chính mình.
- Ngoài phạm vi: tự đăng ký tài khoản, quên mật khẩu qua email, SSO/OIDC federation, xác thực hai yếu tố, uỷ quyền tạm thời.

## 7. Ma trận actor — nhóm chức năng

| Actor | Định danh | Tuyển dụng | Core HR | Hợp đồng |
|---|---|---|---|---|
| Candidate | — (dùng `X-Offer-Token`, không có tài khoản) | Nộp CV, phản hồi Offer | — | — |
| Employee | Đăng nhập, đổi mật khẩu của mình | — | Hồ sơ cá nhân, thông tin phòng ban và quản lý trực tiếp, bàn giao khi thôi việc | Xem/ký hợp đồng |
| Hiring/Line Manager | Đăng nhập, đổi mật khẩu của mình | Requisition, phỏng vấn | Cơ cấu đội ngũ, onboarding, đánh giá thử việc, xác nhận bàn giao | — |
| Recruiter | Đăng nhập, đổi mật khẩu của mình | Pipeline, lịch, scorecard, Offer | — | — |
| HR Officer / C&B | Đăng nhập, đổi mật khẩu của mình | Hỗ trợ tiếp nhận | Hồ sơ, onboarding, biến động, tài liệu, khởi tạo & đóng case thôi việc | Soạn hợp đồng/phụ lục |
| HR Manager | Đăng nhập, đổi mật khẩu của mình | Phê duyệt requisition/Offer | Phê duyệt biến động, quyết định hết thử việc, phê duyệt case thôi việc, quản lý phòng ban/chức danh | Phê duyệt hợp đồng/phụ lục |
| Super Admin | **Cấp/thu hồi tài khoản, vai trò và phạm vi dữ liệu, đặt lại mật khẩu** | — | Vô hiệu hoá tài khoản đúng ngày làm việc cuối; không mặc định xem dữ liệu HR | — |

Ma trận có bốn nhóm chức năng: ba nhóm nghiệp vụ trong phạm vi, cộng nhóm định danh được bổ sung theo [ADR-011](adr/011-in-house-identity.md). Cột báo cáo vẫn không có (trụ cột Reports & Analytics ngoài phạm vi), và vai trò **Auditor** không có use case nào trong đợt này — nhật ký kiểm toán vẫn được ghi đầy đủ, nhưng không có màn hình hay API tra cứu.

Chấm công / nghỉ phép không còn là một cột ở đây vì nhóm chức năng đó nằm ngoài phạm vi triển khai; use case của nó được giữ tại [deferred/attendance_leave/use_cases_att.md](deferred/attendance_leave/use_cases_att.md).

Super Admin không mặc nhiên có quyền đọc hồ sơ, lương hoặc hợp đồng: `ROLE_ADMIN` chỉ mang `admin.user.*`, `admin.role.read` và `corehr.organization.read`. Quyền quản trị và quyền dữ liệu nghiệp vụ phải tách biệt.
