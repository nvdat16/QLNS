# 📘 TÀI LIỆU ĐẶC TẢ TÍNH NĂNG HỆ THỐNG (SOFTWARE REQUIREMENTS SPECIFICATION - SRS)
## Hệ Thống Quản Trị Nhân Sự & Tuyển Dụng Tập Trung (QLNS / NexusHR)

---

## 1. Giới Thiệu Tổng Quan

> **Trạng thái triển khai:** tài liệu này là đặc tả yêu cầu mục tiêu. Project có UI/UX prototype, canonical database/OpenAPI contract, frontend React và backend .NET 10 **code-complete** cho cả 79 operation (controller, authorization policy, workflow, persistence kèm audit/outbox, unit test). Chưa có integration test trên PostgreSQL, EF Core migration và background worker, nên chưa hành vi nào được xác minh end-to-end; `code-complete` chưa phải production-ready.

### 1.1. Mục tiêu Dự án
Hệ thống **QLNS / NexusHR** là giải pháp phần mềm quản trị nguồn nhân lực (HRMS) và tuyển dụng thông minh (ATS) toàn diện, nhằm:
- Số hóa 100% vòng đời của nhân sự: từ khi nộp hồ sơ ứng tuyển, phỏng vấn, tiếp nhận thử việc, ký hợp đồng chính thức, biến động công tác (thăng chức, điều chuyển) đến thôi việc.
- Tự động hóa các luồng xét duyệt hồ sơ, chấm điểm phỏng vấn và cảnh báo hợp đồng sắp hết hạn.
- Tập trung dữ liệu tuyển dụng, hồ sơ nhân sự và hợp đồng vào một nguồn dữ liệu duy nhất, thay cho các tệp bảng tính rời rạc giữa các bộ phận.
- Đảm bảo tuân thủ các quy định pháp luật lao động Việt Nam về hợp đồng, bảo hiểm và lưu trữ hồ sơ nhân sự.

### 1.2. Đối tượng Người dùng & Ma trận Phân quyền (Permission & Data Scope Matrix)

| Vai trò (Role) | Mã quyền | Quyền hạn & Trách nhiệm chính |
| :--- | :--- | :--- |
| **Super Admin** | `ROLE_ADMIN` | Quản trị tài khoản, vai trò và phạm vi dữ liệu trong phân hệ `[ADM]`: tạo/khoá tài khoản, cấp và thu hồi vai trò, đặt lại mật khẩu. **Không có quyền nghiệp vụ nào** — không xem được hồ sơ, hợp đồng hay dữ liệu tuyển dụng. Cấu hình tích hợp/thông báo/luồng phê duyệt vẫn nằm ở `appsettings`, ngoài phạm vi. |
| **HR Director / Manager** | `ROLE_HR_MGR` | Phê duyệt đề xuất tuyển dụng, duyệt Offer letter, ký duyệt quyết định bổ nhiệm/điều chuyển/chấm dứt hợp đồng trên toàn tổ chức. |
| **Talent Acquisition (Recruiter)** | `ROLE_RECRUITER` | Quản lý tin tuyển dụng (Job Posting), sàng lọc CV, xếp lịch phỏng vấn, theo dõi bảng Kanban ATS, gửi thư mời phỏng vấn & Offer. |
| **Hiring Manager / Interviewer** | `ROLE_INTERVIEWER` | Tạo đề xuất tuyển dụng (Requisition), tham gia hội đồng phỏng vấn, chấm điểm ứng viên trên Scorecard, đưa ra khuyến nghị tuyển dụng. |
| **HR Officer (C&B / Records)** | `ROLE_HR_OFFICER` | Quản lý danh bạ hồ sơ nhân viên, soạn thảo và theo dõi hợp đồng lao động, theo dõi danh mục công việc tiếp nhận (Onboarding Checklist). |
| **Line Manager (Quản lý trực tiếp)** | `ROLE_LINE_MGR` | Thực hiện đánh giá hết thử việc, xác nhận bàn giao khi nhân viên thôi việc, đề xuất biến động nhân sự cho đội mình. Phạm vi dữ liệu giới hạn theo `data_scope_type = 'department'`. |
| **Employee (Nhân viên)** | `ROLE_EMPLOYEE` | Xem thông tin hồ sơ cá nhân, xem phòng ban và quản lý trực tiếp của mình, tra cứu thông tin hợp đồng của chính mình, thực hiện bàn giao khi thôi việc. |

Nguyên tắc phân quyền không thay đổi: mọi request đều phải được kiểm tra **permission** của vai trò và **data scope** (`data_scope_type`: toàn tổ chức / phòng ban / chính mình) ở phía server; quyền do client gửi lên không được tin cậy.

Mã vai trò là dữ liệu tham chiếu trong bảng `roles`; ma trận vai trò → permission nằm ở `role_permissions` và được triển khai qua [`database/seed_roles.sql`](../database/seed_roles.sql). Ngoài bảy vai trò trên còn `ROLE_IT_ADMIN` dành cho người thực hiện các task IT trong checklist onboarding/offboarding. Khi đăng nhập, permission được resolve bằng `user_roles ⋈ role_permissions` nên **thu hồi một vai trò có hiệu lực ngay ở lần làm mới phiên kế tiếp**, không cần triển khai lại code. Một tài khoản không có dòng `user_roles` nào vẫn đăng nhập được nhưng không gọi được endpoint nghiệp vụ nào (deny by default).

### 1.3. Phạm vi triển khai và nguyên tắc đặc tả

- **Nguồn xác định phạm vi**: bản đồ phân rã chức năng `topdown-approach.png` (xem mục 2 của [README.md](../README.md)). Quy tắc đọc bản đồ: **chỉ những chức năng lá được in đậm dưới hai trụ cột Recruitment và Core HR thuộc phạm vi giao hàng**; các lá không in đậm và toàn bộ các trụ cột còn lại đều nằm ngoài phạm vi.
- **Hai phân hệ được chọn triển khai**: Recruitment (18 chức năng lá) và Core HR (16 chức năng lá). Contract Management là một nhóm chức năng thuộc Core HR, không phải một trụ cột riêng; tài liệu này tách thành phân hệ riêng chỉ để trình bày chi tiết. Đây là phạm vi được ánh xạ đầy đủ xuống user story, canonical schema và API contract.
- **Đã có ở mức thiết kế dữ liệu**: toàn bộ hai phân hệ trên, phân hệ định danh `[ADM]` (`users`, `user_credentials`, `user_roles`, `roles`, `role_permissions`, `refresh_tokens`), audit log và outbox. `database/schema.sql` là canonical schema contract v1.2 với **28 bảng**; DDL cũ (`init.sql`, `postgres_db.sql`, `dbml.txt`) đã được đánh dấu deprecated và schema chưa được quản lý bởi EF Core migration/runtime.
- **Ngoài phạm vi triển khai — đã có thiết kế, đang tạm dừng**: chấm công và nghỉ phép. Đặc tả, user story, DDL và API contract của nhóm này được giữ tại [docs/deferred/attendance_leave/](deferred/attendance_leave/README.md) để dùng lại sau; không nằm trong canonical schema hay OpenAPI hiện hành.
- Các phân hệ ngoài phạm vi **không** được đặc tả ở tài liệu này, kể cả ở mức tên gọi; ranh giới đầy đủ và lý do nằm ở mục [2.1](#21-ranh-giới-phạm-vi--những-gì-không-thuộc-đợt-này).
- Mọi thao tác tạo, cập nhật, phê duyệt, từ chối và tải dữ liệu phải kiểm tra quyền theo vai trò, lưu người thực hiện và thời điểm thực hiện.
- Các trạng thái nghiệp vụ phải được kiểm soát bằng tập giá trị hợp lệ; không cho phép cập nhật trực tiếp hoặc bỏ qua bước phê duyệt qua giao diện/API.

---

## 2. Đặc Tả Chi Tiết Các Phân Hệ Chức Năng

Cây phân rã dưới đây chỉ chứa các chức năng thuộc phạm vi giao hàng, lấy trực tiếp từ các chức năng lá **in đậm** dưới hai trụ cột Recruitment và Core HR trên `topdown-approach.png` (mục 2 của [README.md](../README.md)). Các trụ cột khác của bản đồ không xuất hiện ở đây theo đúng phạm vi đã chốt.

```
QLNS / NexusHR
├── PHÂN HỆ 1: QUẢN LÝ TUYỂN DỤNG THÔNG MINH (ATS) — trụ cột Recruitment
│   ├── [REC-01] Quản lý Yêu cầu & Tin Tuyển dụng (Job Requisitions & Job Posting)
│   ├── [REC-02] Tiếp nhận & Trích xuất Hồ sơ Ứng viên (CV Intake & AI Parsing)
│   ├── [REC-03] Đường ống Tuyển dụng Trực quan (Kanban ATS Pipeline)
│   ├── [REC-04] Lịch Phỏng vấn & Thư Mời (Interview Scheduling)
│   ├── [REC-05] Đánh giá Phỏng vấn (Interview Scorecard)
│   └── [REC-06] Quản lý Đề nghị Tuyển dụng & Chuyển giao Onboarding (Offer Management & Handoff)
├── PHÂN HỆ 2: QUẢN LÝ HỒ SƠ & VÒNG ĐỜI NHÂN SỰ (CORE HR) — trụ cột Core HR
│   ├── [EMP-01] Danh bạ & Hồ sơ Tổng thể Nhân viên (Employee Master Data)
│   ├── [EMP-02] Cơ cấu Tổ chức & Phòng Ban (Organization Management)
│   ├── [EMP-03] Quy trình Tiếp nhận Nhân viên Mới (Onboarding Checklist)
│   ├── [EMP-04] Quản lý Biến động Nhân sự (Internal Mobility & Events)
│   ├── [EMP-05] Quản lý Tài liệu & Văn bản Nhân sự (Employee Documents)
│   ├── [EMP-06] Đánh giá & Xác nhận Hết Thử việc (Probation Review)
│   └── [EMP-07] Thôi việc & Bàn giao (Offboarding & Handover)
├── PHÂN HỆ 3: QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (CONTRACTS) — nhóm Contract Management thuộc Core HR
│   ├── [CON-01] Soạn thảo & Lưu trữ Hợp đồng (Contract Drafting & Storage)
│   ├── [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
│   └── [CON-03] Quản lý Phụ lục Hợp đồng (Contract Addenda)
└── PHÂN HỆ 4: ĐỊNH DANH & PHÂN QUYỀN (IDENTITY & ACCESS) — nhóm Account Management + Roles/Permissions thuộc System Administration
    ├── [ADM-01] Đăng nhập & Quản lý Phiên (Password Sign-in & Session Management)
    └── [ADM-02] Quản trị Tài khoản & Vai trò (Account & Role Administration)
```

> Phân hệ 4 được bổ sung sau khi quyết định **không** dùng Identity Provider bên ngoài. Hai chức năng lá Account Management và Roles/Permissions & Data Access Scope của trụ cột System Administration vì thế chuyển vào phạm vi giao hàng; các chức năng còn lại của trụ cột đó (cấu hình luồng phê duyệt, thông báo, tích hợp, tra cứu audit trail) vẫn ngoài phạm vi.

### 2.1. Ranh giới phạm vi — những gì không thuộc đợt này

Đây là ranh giới phạm vi chính thức, không phải phần còn thiếu của tài liệu.

**a) Bốn chức năng lá không in đậm, nằm ngay trong hai phân hệ được chọn**

| Chức năng | Thuộc nhóm | Hệ quả đối với đặc tả |
| :--- | :--- | :--- |
| Headcount & Budget Validation | Recruitment · Job Requisition | Requisition vẫn có luồng phê duyệt, nhưng hệ thống **không** tự kiểm tra định biên và ngân sách lương. `target_headcount`, `salary_min`, `salary_max` là dữ liệu khai báo phục vụ người phê duyệt, không phải cơ chế kiểm soát; quyết định thuộc HR Manager. |
| Recruitment Channel Management | Recruitment · Job Posting | Publish/Update/Close tin tuyển dụng vẫn trong phạm vi, nhưng chỉ qua **một cổng careers mặc định**. Không chọn và quản lý nhiều kênh đăng tin, không đo hiệu quả nguồn tuyển. |
| Organizational Chart | Core HR · Organization Management | Không có màn hình và endpoint hiển thị cây tổ chức. Phân cấp phòng ban vẫn **trong** phạm vi: `departments.parent_department_id`, quan hệ cha–con, quy tắc chống chu trình, ràng buộc xóa phòng ban, tính headcount và cost center đều được giữ ở `[EMP-02]`. Chỉ phần trình bày dạng cây bị loại. |
| Suspension & Return to Work | Core HR · Employee Lifecycle | Không có nghiệp vụ tạm hoãn và trở lại làm việc. Giá trị `employees.status = 'suspended'` và `employee_events.event_type IN ('suspension','return_to_work')` vẫn tồn tại trong canonical schema nhưng là **reserved, không luồng hoặc endpoint nào đặt được trong đợt này**. |

**b) Các trụ cột còn lại trên bản đồ chức năng**

Reports & Analytics, Performance Management, Compensation & Benefits và Attendance & Leave Management đều ngoài phạm vi và không được đặc tả ở tài liệu này. System Administration **chỉ vào phạm vi ở hai chức năng lá** Account Management và Roles/Permissions & Data Access Scope (phân hệ `[ADM]`); phần còn lại của trụ cột — cấu hình luồng phê duyệt và uỷ quyền, cấu hình thông báo, cấu hình tích hợp, tra cứu audit trail — vẫn ngoài phạm vi. Thiết kế của Attendance & Leave được giữ nguyên tại [docs/deferred/attendance_leave/](deferred/attendance_leave/README.md).

**c) Phân biệt bắt buộc: cơ chế xuyên suốt vẫn còn, API quản trị thì không**

- Ghi audit log trong cùng transaction với thay đổi nghiệp vụ **vẫn bắt buộc**; bảng `audit_logs` được giữ. Chỉ endpoint tra cứu audit log là ngoài phạm vi.
- Transactional outbox để gửi email và lịch **vẫn bắt buộc**; bảng `outbox_messages` được giữ. Chỉ endpoint xem và retry delivery là ngoài phạm vi.
- Kiểm tra permission và data scope phía server trên mọi request **vẫn bắt buộc** (mục 1.2).
- Các bảng định danh (`users`, `user_credentials`, `user_roles`, `roles`, `role_permissions`, `refresh_tokens`) mang thông tin đăng nhập, định danh và data scope. API quản lý tài khoản và vai trò **thuộc phạm vi** (`[ADM-02]`); ma trận vai trò → permission là dữ liệu tham chiếu chỉ sửa được qua `seed_roles.sql`, không qua API.
- Cấu hình tích hợp, thông báo và luồng phê duyệt nằm trong `appsettings`, không có giao diện hay API quản trị.
- `GET /health/live` và `GET /health/ready` được giữ như endpoint hạ tầng phục vụ triển khai và giám sát, không phải chức năng nghiệp vụ trên bản đồ.

---

## 3. Đặc Tả Chi Tiết Từng Tính Năng

### PHÂN HỆ 1: QUẢN LÝ TUYỂN DỤNG THÔNG MINH (ATS)

#### [REC-01] Quản lý Yêu cầu & Tin Tuyển dụng (Job Requisitions & Job Posting)
- **Mục tiêu**: Cho phép Trưởng bộ phận gửi đề xuất tuyển người, HR Manager phê duyệt và Recruiter đăng, cập nhật, đóng tin tuyển dụng trên cổng careers.
- **Tác nhân**: Hiring Manager, HR Manager, Recruiter.
- **Tiền điều kiện**: Phòng ban tồn tại trong hệ thống.
- **Luồng xử lý chính**:
  1. Hiring Manager chọn phòng ban, chức danh, nhập số lượng cần tuyển (Target Headcount), lý do tuyển (thay thế/mở rộng), mức lương dự kiến và yêu cầu kỹ năng.
  2. Hiring Manager gửi đề xuất; hệ thống chuyển trạng thái sang `Pending Approval` và tạo tác vụ phê duyệt cho HR Manager.
  3. HR Manager phê duyệt hoặc từ chối đề xuất. Việc cân đối định biên và ngân sách lương là quyết định của người có thẩm quyền, hệ thống không tự kiểm tra và không tự chặn.
  4. Recruiter đăng tin tuyển dụng lên cổng careers, kích hoạt trạng thái `Active Recruiting`; tin đã đăng có thể được cập nhật hoặc đóng.
  5. Hệ thống sinh mã định danh công việc (ví dụ: `REQ-2026-08`).
- **Dữ liệu đầu vào**: Tiêu đề vị trí, phòng ban ID, số lượng tuyển, hình thức làm việc (Full-time/Part-time/Hybrid/Remote), dải lương (min - max), mô tả công việc (JD).
- **Hậu điều kiện**: Bản ghi được lưu vào bảng `job_postings`, sẵn sàng nhận hồ sơ ứng tuyển.
- **Quy tắc và ngoại lệ**:
  - Trạng thái hợp lệ: `Draft` → `Pending Approval` → `Approved` → `Active Recruiting` → `Closed` hoặc `Cancelled`; chỉ HR Manager được phê duyệt/từ chối.
  - Khi từ chối, bắt buộc nhập lý do và trả yêu cầu về `Draft` để Hiring Manager chỉnh sửa; không được đăng tin khi chưa `Approved`.
  - `closing_date` phải sau ngày đăng; `salary_min` không được lớn hơn `salary_max`; `target_headcount` phải lớn hơn 0.
  - `target_headcount`, `salary_min` và `salary_max` là dữ liệu khai báo để người phê duyệt tham chiếu, không phải cơ chế kiểm soát tự động; hệ thống không đối chiếu với quỹ lương hay định biên của phòng ban.
  - Không cho đóng requisition còn Offer `Sent` nếu chưa có quyết định xử lý Offer; mọi thay đổi trạng thái phải ghi audit log.

---

#### [REC-02] Tiếp nhận & Trích xuất Hồ sơ Ứng viên (CV Intake & AI Parsing)
- **Mục tiêu**: Tự động thu thập hồ sơ ứng viên từ cổng thông tin hoặc tải tệp PDF/Docx lên và bóc tách thông tin cốt lõi.
- **Tác nhân**: Recruiter, Ứng viên (nộp online).
- **Luồng xử lý chính**:
  1. Ứng viên nộp CV qua web hoặc Recruiter upload tệp CV lên hệ thống.
  2. Hệ thống kiểm tra tính hợp lệ của tệp (dung lượng < 10MB, định dạng PDF/DOC/DOCX).
  3. Module AI/OCR bóc tách dữ liệu: Họ và tên, Email, Số điện thoại, Kỹ năng chính, Số năm kinh nghiệm, Học vấn.
  4. Hệ thống kiểm tra trùng lặp (Duplicate Detection) dựa trên Email và Số điện thoại trong bảng `candidates`:
     - Nếu đã tồn tại: Cập nhật bản ghi ứng viên và liên kết với tin tuyển dụng mới.
     - Nếu chưa tồn tại: Tạo mới bản ghi ứng viên trong bảng `candidates` và lưu tệp vào bảng `resumes`.
  5. Hệ thống tự động tính điểm độ phù hợp ban đầu (AI Match Score từ 0 - 100%).
  6. Tạo bản ghi ứng tuyển trong bảng `applications` với trạng thái `Sourced & Applied`.
- **Quy tắc bảo vệ dữ liệu và lỗi**:
  - Chỉ lưu tệp sau khi quét mã độc thành công; tệp lỗi, vượt dung lượng hoặc không đúng định dạng phải bị từ chối và trả thông báo rõ ràng.
  - Email được chuẩn hóa trước khi so trùng; số điện thoại được chuẩn hóa theo mã quốc gia. Bản ghi trùng chỉ được hợp nhất sau khi Recruiter xác nhận để tránh ghi đè hồ sơ khác người.
  - Kết quả AI/OCR là dữ liệu gợi ý, phải hiển thị trạng thái độ tin cậy và cho phép Recruiter sửa trước khi sử dụng để loại ứng viên.
  - Ứng viên phải được thông báo mục đích xử lý dữ liệu; hỗ trợ yêu cầu xóa/ẩn danh hồ sơ theo chính sách lưu trữ.

---

#### [REC-03] Đường ống Tuyển dụng Trực quan (Kanban ATS Pipeline)
- **Mục tiêu**: Trực quan hóa quy trình phỏng vấn theo 6 giai đoạn chuẩn quốc tế, hỗ trợ kéo thả và xem nhanh hồ sơ.
- **Các giai đoạn chuẩn (Kanban Stages)**:
  1. `Sourced & Applied` (Mới ứng tuyển)
  2. `AI Screening` (Sơ loại hồ sơ & kiểm tra tự động)
  3. `Tech Interview` (Phỏng vấn chuyên môn vòng 1)
  4. `Executive Round` (Phỏng vấn quản trị / Văn hóa)
  5. `Offer Letter` (Thư mời nhận việc & Đàm phán)
  6. `Hired & Ready` (Đã tiếp nhận & Chuyển sang Onboarding)
- **Quy tắc chuyển giai đoạn**:
  - Khi ứng viên đạt vòng hiện tại, Recruiter hoặc người phỏng vấn bấm **"Advance Stage →"** hoặc kéo thẻ sang cột tiếp theo.
  - Hệ thống gọi API `POST /api/v1/recruitment/applications/{applicationId}/advance` để ghi nhận sự kiện chuyển bước và lưu thời điểm chuyển.
  - Nếu ứng viên không đạt: Chuyển sang trạng thái `Rejected` kèm lý do từ chối; hệ thống cho phép kích hoạt gửi email cảm ơn mẫu.
- **Kiểm soát chuyển trạng thái**:
  - Chỉ cho phép chuyển tiến một bước theo luồng chuẩn. Chuyển lùi, nhảy bước hoặc khôi phục từ `Rejected`/`Withdrawn` yêu cầu quyền Recruiter hoặc HR Manager và lý do.
  - Chỉ được chuyển vào vòng phỏng vấn khi có lịch `Scheduled`; chỉ được chuyển `Offer Letter` sau khi có đánh giá hợp lệ theo policy của vị trí.
  - API phải kiểm tra phiên bản cập nhật hoặc trạng thái hiện tại để tránh hai người dùng đồng thời chuyển cùng một hồ sơ.

---

#### [REC-04] Lịch Phỏng vấn & Gửi Thư Mời (Interview Scheduling)
- **Mục tiêu**: Phối hợp thời gian phỏng vấn giữa ứng viên và hội đồng phỏng vấn, tránh trùng lịch.
- **Tác nhân**: Recruiter, Interviewer, Candidate.
- **Luồng xử lý**:
  1. Recruiter chọn ứng viên trong giai đoạn phỏng vấn, chọn thành phần phỏng vấn (Interviewer IDs), thời gian bắt đầu/kết thúc, hình thức (Trực tiếp: phòng họp / Trực tuyến: Google Meet, Zoom, MS Teams link).
  2. Hệ thống tạo bản ghi trong bảng `interviews` với trạng thái `Scheduled`.
  3. Hệ thống gửi email tự động kèm file lịch (`.ics`) đến ứng viên và người phỏng vấn.
- **Quy tắc và ngoại lệ**:
  - Không cho phép xếp lịch trùng giờ với cùng Interviewer hoặc phòng họp; thời gian hiển thị phải có múi giờ rõ ràng.
  - Khi đổi lịch hoặc hủy, hệ thống giữ lịch sử, gửi thông báo hủy/cập nhật cho tất cả người tham gia và yêu cầu nhập lý do nếu hủy trong thời gian quy định.
  - Gửi nhắc lịch mặc định trước 24 giờ và 1 giờ; nếu gửi email thất bại, hiển thị tác vụ gửi lại cho Recruiter.

---

#### [REC-05] Đánh giá Phỏng vấn (Interview Scorecard)
- **Mục tiêu**: Chuẩn hóa tiêu chí chấm điểm ứng viên một cách khách quan, minh bạch.
- **Tác nhân**: Interviewer.
- **Luồng xử lý**:
  1. Sau buổi phỏng vấn, người phỏng vấn mở mẫu Scorecard tương ứng với vị trí.
  2. Nhập điểm theo thang điểm 5.0 cho từng nhóm kỹ năng:
     - Kỹ năng chuyên môn kỹ thuật (Technical Competency)
     - Khả năng giải quyết vấn đề (Problem Solving)
     - Kỹ năng giao tiếp & phối hợp (Communication & Teamwork)
     - Mức độ phù hợp văn hóa doanh nghiệp (Culture Fit)
  3. Chọn đề xuất chung: `Strong Hire`, `Hire`, `Hold`, `No Hire`, `Strong No Hire`.
  4. Nhập nhận xét chi tiết (Strengths & Areas of Improvement).
  5. Lưu thông tin vào bảng `evaluations`.
- **Quy tắc dữ liệu**:
  - Điểm từng tiêu chí chỉ nhận giá trị từ 0 đến 5, cho phép bước nhảy 0.5; `overall_score` được tính tự động theo trọng số của vị trí và có thể cấu hình.
  - Chỉ Interviewer được gán vào lịch mới có thể nộp scorecard; sau khi nộp, chỉ HR Manager được mở khóa/chỉnh sửa và phải ghi lý do.
  - Không hiển thị đánh giá của người phỏng vấn khác trước khi người dùng nộp bản đánh giá của mình hoặc trước thời điểm công bố theo policy.

---

#### [REC-06] Quản lý Đề nghị Tuyển dụng & Chuyển giao Onboarding (Offer Management & Handoff)
- **Mục tiêu**: Lập và phê duyệt thư mời nhận việc trước khi gửi cho ứng viên trúng tuyển.
- **Tác nhân**: Recruiter, HR Manager, Candidate.
- **Luồng xử lý**:
  1. Recruiter khởi tạo Offer dựa trên vị trí tuyển dụng: nhập Lương cơ bản (Base Salary), Thưởng gia nhập (Sign-on Bonus), Phụ cấp, Ngày bắt đầu làm việc dự kiến (Start Date), Hạn phản hồi (Expiration Date).
  2. Bản ghi được lưu vào bảng `offers` ở trạng thái `Draft`.
  3. HR Manager phê duyệt Offer -> Chuyển sang `Sent`.
  4. Ứng viên phản hồi:
     - **Chấp thuận (Accepted)**: Hệ thống tự động chuyển trạng thái hồ sơ sang `Hired & Ready`, đồng thời kích hoạt quy trình tạo hồ sơ nhân viên `[EMP-01]` và danh mục tiếp nhận `[EMP-03]`.
     - **Từ chối (Declined)**: Nhập lý do từ chối (lương chưa phù hợp, chọn công ty khác...).
- **Quy tắc và ngoại lệ**:
  - Offer chỉ được gửi sau khi có phê duyệt; nội dung được sinh từ phiên bản template đã được phê duyệt và lưu URL tài liệu đã gửi.
  - Khi quá `expiration_date`, Offer tự chuyển `Expired`; việc gia hạn cần tạo lịch sử phiên bản/ghi nhận phê duyệt lại.
  - Hành động chấp thuận phải idempotent: một Offer chỉ tạo tối đa một hồ sơ `employees`, một hợp đồng ban đầu và một bộ onboarding task; nếu tác vụ nền thất bại, phải có cơ chế retry an toàn.

---

### PHÂN HỆ 2: QUẢN LÝ HỒ SƠ & VÒNG ĐỜI NHÂN SỰ (CORE HR)

#### [EMP-01] Danh bạ & Hồ sơ Tổng thể Nhân viên (Employee Master Data)
- **Mục tiêu**: Quản lý thông tin định danh tập trung của toàn bộ cán bộ công nhân viên.
- **Dữ liệu quản lý**:
  - Thông tin cá nhân: Họ tên, Mã nhân viên (`EMP-xxxx`), Ngày sinh, Giới tính, CMND/CCCD, Quốc tịch, Tình trạng hôn nhân.
  - Thông tin liên hệ: Email công vụ, Email cá nhân, Số điện thoại di động, Địa chỉ thường trú, Địa chỉ tạm trú, Thông tin liên hệ khẩn cấp.
  - Thông tin công việc: Phòng ban ID, Chức vụ ID, Cấp bậc (Band/Grade: ví dụ IC-1 đến L-9), Quản lý trực tiếp (Manager ID), Địa điểm làm việc (Văn phòng Hà Nội, TP.HCM, Remote), Ngày vào công ty.
  - Trạng thái công tác được dùng trong đợt này: `Active` (Đang làm việc), `Probation` (Thử việc), `Terminated` (Đã nghỉ việc). Giá trị `Suspended` vẫn được giữ trong canonical schema như giá trị **reserved** — ngoài phạm vi đợt này, không luồng nghiệp vụ hay endpoint nào đặt được (xem mục 2.1).
- **Yêu cầu hệ thống**:
  - Tìm kiếm toàn văn (Full-text search) theo Tên, Mã NV, Email hoặc Chức danh.
  - Lọc đa chiều theo Phòng ban, Cấp bậc, Trạng thái.
  - Nhân viên chỉ được xem và đề nghị chỉnh sửa các trường hồ sơ của chính mình; HR Officer/HR Manager được sửa dữ liệu nghiệp vụ theo phạm vi được phân quyền.
  - Email công vụ và mã nhân viên phải duy nhất. Thay đổi email, phòng ban, chức danh, trạng thái hoặc quản lý trực tiếp phải đi qua luồng biến động `[EMP-04]`, không được sửa trực tiếp từ màn hình hồ sơ.
  - Các trường định danh nhạy cảm (CCCD, thông tin ngân hàng, liên hệ khẩn cấp) phải được mã hóa khi lưu trữ, che một phần khi hiển thị và không đưa vào các bản xuất dữ liệu mặc định.

---

#### [EMP-02] Cơ cấu Tổ chức & Phòng Ban (Organization Management)
- **Mục tiêu**: Quản lý dữ liệu chủ về phân cấp tổ chức (Phòng ban -> Bộ phận/Pod -> Vị trí -> Nhân viên), danh mục chức danh/cấp bậc và reporting line, làm nền cho hồ sơ nhân viên, requisition và hợp đồng.
- **Quy tắc nghiệp vụ**:
  - Mỗi phòng ban có Mã phòng ban duy nhất (`code`) và tên phòng ban.
  - Phòng ban được tổ chức nhiều cấp qua `parent_department_id`; một phòng ban có tối đa một phòng ban cha.
  - Hệ thống tính toán tự động số lượng nhân sự trực thuộc (Headcount) và gắn Trung tâm chi phí (Cost Center) cho từng phòng ban, tính cả các phòng ban con.
  - Danh mục chức danh và cấp bậc (`positions`) được quản lý tập trung; phân công nhân viên và reporting line lấy từ `employees.department_id`, `employees.position_id` và `employees.manager_id`, mọi thay đổi phải đi qua luồng biến động `[EMP-04]`.
  - Không cho phép xóa phòng ban còn nhân viên hoặc requisition đang hiệu lực; phải điều chuyển hoặc đóng các bản ghi liên quan trước.
- **Phụ thuộc dữ liệu**: Canonical schema đã có `departments(id, code, name, parent_department_id, cost_center, description, version)` cùng constraint `ck_departments_not_self_parent`, đủ để biểu diễn phân cấp phòng ban nhiều cấp và tính cost center. Reporting line được lấy từ `employees.manager_id`.
  - **Còn thiếu**: cột `manager_id` ở cấp phòng ban (trưởng phòng chính danh của đơn vị). Hiện chỉ suy ra được qua `employees.manager_id`, nên chưa biểu diễn được trường hợp phòng ban tạm thời không có nhân sự trực thuộc. Cần migration riêng nếu nghiệp vụ yêu cầu.
  - Quan hệ phòng ban cha – con phải chặn chu trình ở tầng service (A là cha của B, B là cha của A); constraint hiện chỉ chặn trường hợp phòng ban tự trỏ chính nó.

---

#### [EMP-03] Quy trình Tiếp nhận Nhân viên Mới (Onboarding Checklist)
- **Mục tiêu**: Chuẩn bị đầy đủ trang thiết bị, tài khoản hệ thống và đào tạo nhập môn cho nhân viên mới ngay trước ngày đi làm đầu tiên.
- **Tác nhân**: HR Officer, IT Admin, Admin Logistics, Nhân viên mới.
- **Luồng xử lý**:
  1. Khi ứng viên chấp nhận Offer, hệ thống tự động sinh danh mục công việc trong bảng `onboarding_tasks`:
     - IT: Tạo email công vụ, cấp quyền Slack/Git/ERP, chuẩn bị máy tính/laptop.
     - Hành chính: Chuẩn bị thẻ nhân viên, bàn làm việc, thẻ gửi xe.
     - HR: Chuẩn bị hợp đồng thử việc, hướng dẫn nộp hồ sơ cá nhân (ảnh, bằng cấp, thuế).
     - Quản lý bộ phận: Bố trí người hướng dẫn (Buddy/Mentor).
  2. Mỗi công việc có Hạn hoàn thành (`due_date`) và Người chịu trách nhiệm (`assigned_to`).
  3. Người phụ trách đánh dấu hoàn thành -> Cập nhật trạng thái `Completed` và thời điểm hoàn thành.
- **Quy tắc nghiệp vụ**:
  - Checklist phải được sinh từ template theo đơn vị, vị trí, địa điểm và loại hợp đồng; không tạo trùng task khi Offer được xử lý lại.
  - Trạng thái task hợp lệ: `Pending` → `In Progress` → `Completed`; task hoàn thành chỉ được mở lại bởi HR Officer/HR Manager và phải ghi lý do.
  - Màn hình theo dõi onboarding phải cảnh báo task quá hạn và task chặn ngày nhận việc (ví dụ: chưa cấp tài khoản hoặc chưa ký hợp đồng thử việc).

---

#### [EMP-04] Quản lý Biến động Nhân sự (Internal Mobility & Events)
- **Mục tiêu**: Lưu vết toàn bộ lịch sử thuyên chuyển phòng ban, thăng chức, điều chỉnh chức danh hoặc kỷ luật/khen thưởng trong suốt quá trình công tác.
- **Tác nhân**: HR Manager, Director.
- **Luồng xử lý**:
  1. Khi có quyết định nhân sự, HR tạo bản ghi biến động trong bảng `employee_events`.
  2. Các loại sự kiện (`event_type`, được chặn bằng `ck_employee_event_type` trong canonical schema):
     - `probation_confirmation` (Xác nhận hết thử việc → chính thức) — sinh từ `[EMP-06]`
     - `probation_extension` (Gia hạn thử việc) — sinh từ `[EMP-06]`
     - `promotion` (Thăng chức / Nâng bậc)
     - `transfer` (Điều chuyển phòng ban / Chi nhánh)
     - `demotion` (Giáng chức)
     - `salary_adjustment` (Điều chỉnh bậc lương)
     - `termination` (Chấm dứt hợp đồng) — sinh từ `[EMP-07]`
  3. Hệ thống lưu lại giá trị cũ (`old_department_id`, `old_position_id`) và giá trị mới (`new_department_id`, `new_position_id`), ngày có hiệu lực (`effective_date`) và lý do.
  4. Khi đến ngày hiệu lực, hệ thống tự động cập nhật bản ghi chính của nhân viên trong bảng `employees`.
- **Quy tắc phê duyệt và hiệu lực**:
  - Bản ghi biến động cần trạng thái `Draft`/`Pending Approval`/`Approved`/`Cancelled` trong bảng hoặc workflow hỗ trợ; chỉ sự kiện `Approved` mới được áp dụng vào `employees`.
  - Không cho phép hai biến động hiệu lực cùng ngày làm thay đổi cùng một trường của nhân viên. Khi hủy sau khi áp dụng, tạo sự kiện điều chỉnh mới thay vì sửa hoặc xóa lịch sử.
  - Điều chỉnh lương phải liên kết với phụ lục hợp đồng hoặc quyết định lương đã được phê duyệt.
  - Hai giá trị `suspension` và `return_to_work` vẫn nằm trong `ck_employee_event_type` của canonical schema nhưng là giá trị **reserved**: đợt này không có luồng nghiệp vụ hay endpoint nào tạo được sự kiện thuộc hai loại đó (xem mục 2.1).

---

#### [EMP-05] Quản lý Tài liệu & Văn bản Hồ sơ (Employee Documents)
- **Mục tiêu**: Lưu trữ an toàn bản scan các giấy tờ chứng chỉ, sơ yếu lý lịch, bằng cấp, cam kết bảo mật (NDA).
- **Dữ liệu**: Bảng `employee_documents` lưu loại tài liệu (`document_type`), tên file, đường dẫn lưu trữ an toàn (`file_url`), người tải lên và ngày tải lên.
- **Quy tắc bảo mật**:
  - Chỉ chấp nhận loại tệp, dung lượng và nội dung đã được kiểm tra; file được lưu private và cấp quyền truy cập bằng URL có thời hạn, không dùng URL công khai cố định.
  - Phân quyền xem/tải theo loại tài liệu; các giấy tờ định danh và hợp đồng chỉ HR có thẩm quyền hoặc chính nhân viên được truy cập.
  - Ghi nhận phiên bản, thời hạn lưu trữ và trạng thái hết hạn của tài liệu; xóa vật lý chỉ sau khi hết thời hạn lưu trữ và đã được phê duyệt.

---

#### [EMP-06] Đánh giá & Xác nhận Hết Thử việc (Probation Review)
- **Mục tiêu**: Đảm bảo mọi nhân viên thử việc đều được đánh giá và có quyết định chính thức trước khi hợp đồng thử việc hết hạn, tránh rủi ro pháp lý do để nhân viên làm việc không có căn cứ hợp đồng.
- **Tác nhân**: Line Manager (người đánh giá), HR Officer (điều phối), HR Manager (phê duyệt kết quả).
- **Tiền điều kiện**: Nhân viên có `status = 'probation'` và một hợp đồng `contract_type = 'Probation'` đang hiệu lực.
- **Luồng xử lý chính**:
  1. Khi hợp đồng thử việc được kích hoạt, hệ thống tạo một bản ghi `probation_reviews` ở trạng thái `pending` với `review_due_date` đặt trước ngày hết hạn hợp đồng một khoảng đủ để xử lý thủ tục.
  2. Hệ thống gửi nhắc việc cho Line Manager theo mốc cảnh báo của `[CON-02]` (7 ngày và 15 ngày trước khi hết hạn).
  3. Line Manager nhập điểm đánh giá tổng (`overall_score`), điểm mạnh, điểm cần cải thiện và đề xuất kết quả; bản ghi chuyển `in_review`.
  4. HR Manager phê duyệt kết quả; bản ghi chuyển `decided` kèm `outcome`, `effective_date`, `decided_by` và `decided_at`.
  5. Hệ thống sinh một `employee_events` tương ứng với `outcome` và liên kết ngược lại qua `probation_reviews.employee_event_id`:
     - `confirmed` → `event_type = 'probation_confirmation'`, kèm hợp đồng chính thức mới ở `[CON-01]`.
     - `extended` → `event_type = 'probation_extension'`, kèm phụ lục hoặc hợp đồng thử việc mới.
     - `terminated` → `event_type = 'termination'`, kích hoạt `[EMP-07]`.
- **Hậu điều kiện**: `employees.status` chỉ được cập nhật khi `employee_events` tương ứng đã `approved` và đến `effective_date`; không sửa trực tiếp từ màn hình đánh giá.
- **Quy tắc và ngoại lệ**:
  - Một hợp đồng thử việc có đúng một phiếu đánh giá (`ux_probation_review_contract`). Gia hạn thử việc tạo hợp đồng/phụ lục mới và do đó tạo phiếu đánh giá mới, không ghi đè phiếu cũ.
  - Trạng thái `decided` bắt buộc có đồng thời `outcome`, `decided_by`, `decided_at` và `effective_date` (`ck_probation_decided`).
  - Chỉ Line Manager được gán là `reviewer_user_id` mới nhập được đánh giá; sau khi `decided`, chỉ HR Manager được mở lại và phải ghi lý do vào audit log.
  - `overall_score` nhận giá trị từ 0 đến 5. Kết quả `terminated` bắt buộc có nhận xét ở `improvements` để làm căn cứ.
  - Danh sách phiếu đánh giá thử việc phải cảnh báo đỏ các phiếu quá `review_due_date` mà còn `pending`/`in_review`. Đây là rủi ro pháp lý, không chỉ là trễ quy trình.

---

#### [EMP-07] Thôi việc & Bàn giao (Offboarding & Handover)
- **Mục tiêu**: Quản lý toàn bộ thủ tục chấm dứt quan hệ lao động: bàn giao công việc, thu hồi tài sản và tài khoản, chốt công nợ và lưu trữ hồ sơ theo thời hạn.
- **Tác nhân**: Employee (gửi đơn), Line Manager (xác nhận bàn giao), HR Officer (điều phối), HR Manager (phê duyệt), IT Admin, Admin Logistics, Finance.
- **Tiền điều kiện**: Nhân viên đang ở trạng thái `active` hoặc `probation`.
- **Luồng xử lý chính**:
  1. HR Officer tạo `offboarding_cases` với `separation_type`, `notice_received_on`, `last_working_date`, người nhận bàn giao (`handover_to_employee_id`) và lý do.
  2. Hệ thống đối chiếu `notice_received_on` với thời hạn báo trước (`contracts.notice_period_days`) và cảnh báo nếu không đủ, nhưng không tự chặn — quyết định thuộc HR Manager.
  3. HR Manager phê duyệt; case chuyển `approved` và hệ thống sinh `offboarding_tasks` từ template theo phòng ban, chức danh và loại chấm dứt:
     - `it`: thu hồi laptop và thiết bị, khóa tài khoản email/Slack/Git/ERP, chuyển quyền sở hữu repository và tài liệu.
     - `admin`: thu thẻ nhân viên, thẻ gửi xe, bàn giao chỗ làm việc.
     - `hr`: phỏng vấn thôi việc, lập quyết định chấm dứt, chốt sổ bảo hiểm, trả hồ sơ gốc.
     - `manager`: xác nhận bàn giao công việc, tài liệu và đầu mối liên hệ cho người nhận.
     - `finance`: chốt công nợ, tạm ứng và các khoản thanh toán còn lại khi chấm dứt.
  4. Người phụ trách đánh dấu hoàn thành từng task; case chuyển `in_progress`.
  5. Khi toàn bộ task có `blocks_last_working_day = true` đã `completed` và `final_settlement_status` đạt `paid` hoặc `waived`, HR Officer đóng case (`completed`).
  6. Hệ thống tạo `employee_events` với `event_type = 'termination'` và `effective_date = last_working_date`; đến ngày hiệu lực, `employees.status` chuyển `terminated`.
- **Hậu điều kiện**: Nhân viên `terminated` không còn quyền truy cập hệ thống (tài khoản `users.status = 'disabled'`), không xuất hiện trong danh bạ đang làm việc, nhưng hồ sơ và tài liệu vẫn được giữ theo `employee_documents.retention_until`.
- **Quy tắc và ngoại lệ**:
  - Mỗi nhân viên chỉ có tối đa một case đang mở (`ux_offboarding_open_case`). Thôi việc rồi tái tuyển dụng tạo case mới, không mở lại case cũ.
  - `handover_to_employee_id` không được là chính nhân viên thôi việc (`ck_offboarding_handover_not_self`) và phải là nhân viên `active`.
  - Không được đóng case khi còn task chặn chưa hoàn thành. Bỏ qua task chặn cần HR Manager duyệt và ghi lý do vào audit log.
  - Khóa tài khoản phải thực hiện đúng `last_working_date`, không sớm hơn, để nhân viên còn hoàn thành bàn giao.
  - Các khoản thanh toán còn lại khi chấm dứt (công nợ, tạm ứng, quyền lợi theo hợp đồng) phải được chốt và phản ánh vào `final_settlement_status` trước khi đóng case; công thức tính thuộc phân hệ Payroll và chưa nằm trong phạm vi.
  - Xóa vật lý hồ sơ chỉ được thực hiện sau khi hết thời hạn lưu trữ và có phê duyệt, theo `[EMP-05]`.

---

### PHÂN HỆ 3: QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (CONTRACTS)

#### [CON-01] Soạn thảo & Lưu trữ Hợp đồng (Contract Drafting & Lifecycle)
- **Mục tiêu**: Quản lý đầy đủ các loại hợp đồng theo Bộ luật Lao động Việt Nam.
- **Các loại hợp đồng (`contract_type`)**:
  - `Probation` (Hợp đồng thử việc: 30 hoặc 60 ngày)
  - `Fixed-Term` (Hợp đồng xác định thời hạn: 12 tháng, 24 tháng, 36 tháng)
  - `Indefinite` (Hợp đồng không xác định thời hạn)
  - `Internship` (Hợp đồng thực tập)
  - `Service_Contract` (Hợp đồng khoán / Cộng tác viên)
- **Quy tắc nghiệp vụ**:
  - Mỗi hợp đồng có Số hợp đồng/Protocol Number duy nhất. Schema hiện có đang dùng trường `protocol_number`; nếu chuẩn hóa thành `contract_number` cần migration và unique constraint.
  - Ghi nhận Ngày bắt đầu (`start_date`), Ngày kết thúc (`end_date`), Mức lương (`salary`), thời hạn báo trước (`notice_period`) và trạng thái hợp đồng. Tiền lương đóng BHXH, phụ cấp cố định và chữ ký số là dữ liệu cần mở rộng schema trước khi sử dụng.
  - Hỗ trợ lưu trữ file số hóa đã ký (PDF) hoặc trạng thái chữ ký số DocuSign/VNPT-CA.
  - Một nhân viên chỉ có một hợp đồng `Active`/`Executed` chính tại một thời điểm, trừ khi HR Manager đánh dấu ngoại lệ; `end_date` phải sau `start_date` đối với hợp đồng có thời hạn.

---

#### [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
- **Mục tiêu**: Ngăn ngừa rủi ro pháp lý do quên gia hạn hợp đồng đúng thời hạn luật định.
- **Quy tắc cảnh báo**:
  - Hợp đồng thử việc: Cảnh báo trước **7 ngày** và **15 ngày** trước khi hết hạn để quản lý hoàn thành đánh giá thử việc.
  - Hợp đồng xác định thời hạn: Cảnh báo trước **30 ngày** và **45 ngày** trước khi hết hạn.
- **Hành động hệ thống**:
  - Hiển thị badge màu hổ phách/đỏ trên danh sách hợp đồng của HR phụ trách.
  - Gửi thông báo đến HR phụ trách hợp đồng để lập thông báo chấm dứt hoặc ký hợp đồng mới.

---

#### [CON-03] Quản lý Phụ lục Hợp đồng (Contract Addenda)
- **Mục tiêu**: Quản lý phụ lục điều chỉnh điều khoản hợp đồng mà không làm mất tính toàn vẹn của hợp đồng gốc.
- **Tác nhân**: HR Officer, HR Manager, Employee (xác nhận/ký nếu áp dụng).
- **Luồng xử lý**:
  1. HR Officer chọn hợp đồng gốc còn hiệu lực, chọn loại thay đổi (lương, phụ cấp, chức danh, địa điểm, thời hạn hoặc điều khoản khác) và nhập ngày hiệu lực.
  2. Hệ thống tạo phụ lục ở trạng thái `Draft`, đối chiếu dữ liệu trước/sau và gửi phê duyệt.
  3. Sau khi `Approved` và được ký theo policy, phụ lục chuyển `Effective`; hệ thống tạo `employee_event` tương ứng và cập nhật dữ liệu chủ nếu đến ngày hiệu lực.
- **Quy tắc**:
  - Phụ lục không được sửa hợp đồng gốc; thay đổi sau khi hiệu lực phải tạo phiên bản phụ lục mới hoặc phụ lục thay thế.
  - Mỗi phụ lục cần số tham chiếu, người lập, người phê duyệt, file đã ký và vết ghi audit trong bảng canonical `contract_addenda`; migration runtime vẫn phải được tạo và review trước khi triển khai.

---

### PHÂN HỆ 4: ĐỊNH DANH & PHÂN QUYỀN (IDENTITY & ACCESS)

#### [ADM-01] Đăng nhập & Quản lý Phiên (Password Sign-in & Session Management)
- **Mục tiêu**: Cấp cho người dùng một danh tính đã được xác thực, kèm đúng permission và data scope, mà không phụ thuộc nhà cung cấp định danh bên ngoài.
- **Tác nhân**: mọi người dùng nội bộ có tài khoản `active` trong bảng `users`.
- **Luồng xử lý**:
  1. Người dùng nhập email và mật khẩu. Email được chuẩn hoá về chữ thường và bỏ khoảng trắng trước khi tra cứu, vì `users.email` là UNIQUE.
  2. Hệ thống verify mật khẩu với bản băm trong `user_credentials`. Nếu email không tồn tại, hệ thống **vẫn** verify với một hash giả để thời gian phản hồi không tiết lộ tài khoản nào tồn tại.
  3. Đăng nhập đúng: xoá bộ đếm sai, ghi `last_login_at`, resolve permission + data scope bằng `user_roles ⋈ role_permissions`, phát access token (JWT HS256, mặc định 30 phút) và refresh token (mặc định 14 ngày).
  4. Refresh token là **chuỗi ngẫu nhiên 256-bit dùng một lần**; database chỉ lưu bản băm SHA-256. Làm mới phiên sẽ thu hồi token cũ với lý do `rotated`, lưu liên kết tới token kế nhiệm và **đọc lại quyền từ database**.
  5. Đăng xuất thu hồi refresh token đang giữ; access token vẫn sống tới khi hết hạn — đây là giới hạn đã biết và được chấp nhận của bearer token stateless.
- **Quy tắc nghiệp vụ**:
  - **Khoá tạm theo tài khoản**: sai **5 lần liên tiếp** khoá **15 phút** (`user_credentials.failed_attempts`, `locked_until`). Lần sai đạt ngưỡng mới bắt đầu cửa sổ khoá; sai thêm trong lúc đang khoá không kéo dài cửa sổ.
  - **Không tiết lộ tài khoản**: email sai, không có credential và sai mật khẩu đều trả `401` với cùng một `code`. Tài khoản bị vô hiệu hoá chỉ được báo **sau khi** mật khẩu đã đúng.
  - **Phát hiện đánh cắp token**: trình lại một refresh token đã bị thu hồi ⇒ thu hồi **toàn bộ** refresh token của tài khoản đó (`reuse_detected`) và buộc đăng nhập lại.
  - **Buộc đổi mật khẩu**: khi `must_change_password` bật, phiên được cấp là phiên **hạn chế** — không có refresh token, access token không mang permission nào, chỉ gọi được endpoint đổi mật khẩu.
  - **Tự đổi mật khẩu** bắt buộc nhập mật khẩu hiện tại (access token bị đánh cắp một mình không được phép chiếm tài khoản); mật khẩu mới tối thiểu 10 ký tự, kết hợp ít nhất 3 trong 4 nhóm ký tự, không chứa phần trước `@` của email và không trùng mật khẩu cũ. Đổi xong thu hồi mọi refresh token của người dùng.
  - **Lưu trữ mật khẩu**: PBKDF2-HMAC-SHA512, 210.000 vòng, salt 128-bit riêng cho từng mật khẩu; tham số nằm trong chính chuỗi hash nên nâng work factor không cần migration. So sánh bằng thuật toán constant-time.
  - **Audit**: mọi lần đăng nhập, làm mới, đăng xuất và đổi mật khẩu — kể cả thất bại (`result = 'rejected'`) — ghi `audit_logs` trong cùng transaction với thay đổi trạng thái. Không bao giờ ghi mật khẩu, hash hay giá trị token vào audit.
- **Ngoài phạm vi**: tự đăng ký tài khoản, quên mật khẩu qua email, đăng nhập một lần (SSO/OIDC federation), xác thực hai yếu tố (MFA). Rate limit theo IP đặt ở reverse proxy trước API, không phải trong ứng dụng.

---

#### [ADM-02] Quản trị Tài khoản & Vai trò (Account & Role Administration)
- **Mục tiêu**: Cho phép Super Admin cấp, thu hồi và giới hạn quyền truy cập mà không cần can thiệp trực tiếp vào database.
- **Tác nhân**: Super Admin (`admin.user.manage`); vai trò chỉ đọc dùng `admin.user.read` / `admin.role.read`.
- **Luồng xử lý**:
  1. Tạo tài khoản: nhập email, tên hiển thị, mật khẩu ban đầu, tuỳ chọn liên kết `employee_id`, và danh sách vai trò kèm phạm vi dữ liệu. Hệ thống sinh `external_subject = local|<email>`, băm mật khẩu và **luôn** bật `must_change_password` vì mật khẩu do người khác biết.
  2. Cấp/thu hồi vai trò: gửi **trạng thái đích đầy đủ**; vai trò không có trong danh sách sẽ bị xoá. Danh sách rỗng để lại tài khoản đăng nhập được nhưng không có quyền nào.
  3. Vô hiệu hoá tài khoản: đổi `users.status = 'disabled'` và thu hồi toàn bộ refresh token của tài khoản.
  4. Đặt lại mật khẩu: đặt mật khẩu tạm, bật `must_change_password`, xoá bộ đếm khoá, thu hồi toàn bộ refresh token. Mật khẩu tạm **không** xuất hiện trong response; phải chuyển cho người dùng qua kênh an toàn ngoài hệ thống.
- **Quy tắc nghiệp vụ**:
  - **Không tự quản trị chính mình**: Super Admin không được vô hiệu hoá, đặt lại mật khẩu hay sửa vai trò của tài khoản của chính mình. Đây là rào chắn kép — chống tự khoá cả tổ chức khỏi quyền quản trị, và chống nâng quyền không qua người thứ hai.
  - **Vai trò phải có trong danh mục**: `role_code` phải tồn tại trong `roles` và `is_assignable = true`. Phạm vi `department` bắt buộc `data_scope_id` là phòng ban tồn tại; `self` và `organization` bắt buộc `data_scope_id = 0` (đúng theo `ck_user_roles_scope`).
  - **Email là duy nhất** sau khi chuẩn hoá chữ thường; xung đột trả `409` cả khi phát hiện ở bước kiểm tra trước lẫn khi thua race ở unique index.
  - **Liên kết nhân viên**: một `employee` chỉ gắn với một tài khoản; nhân viên đã có tài khoản thì bị từ chối.
  - **Đồng thời**: mọi lệnh sửa yêu cầu `If-Match` theo `users.version`. Việc thay vai trò cũng dùng chính version này làm chốt, dù dữ liệu thay đổi nằm ở bảng `user_roles`.
  - **Ma trận vai trò → permission là dữ liệu tham chiếu**, không sửa được qua API: nó được triển khai bằng [`database/seed_roles.sql`](../database/seed_roles.sql) để mọi thay đổi quyền hạn đều đi qua review và có vết trong version control.
- **Ngoài phạm vi**: tạo/sửa vai trò và permission qua API, uỷ quyền tạm thời (delegation), nhập khẩu tài khoản theo lô, đồng bộ tài khoản từ HR sang hệ thống ngoài.
