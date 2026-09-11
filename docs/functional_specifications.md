# 📘 TÀI LIỆU ĐẶC TẢ TÍNH NĂNG HỆ THỐNG (SOFTWARE REQUIREMENTS SPECIFICATION - SRS)
## Hệ Thống Quản Trị Nhân Sự & Tuyển Dụng Tập Trung (QLNS / NexusHR)

---

## 1. Giới Thiệu Tổng Quan

### 1.1. Mục tiêu Dự án
Hệ thống **QLNS / NexusHR** là giải pháp phần mềm quản trị nguồn nhân lực (HRMS) và tuyển dụng thông minh (ATS) toàn diện, nhằm:
- Số hóa 100% vòng đời của nhân sự: từ khi nộp hồ sơ ứng tuyển, phỏng vấn, tiếp nhận thử việc, ký hợp đồng chính thức, biến động công tác (thăng chức, điều chuyển) đến thôi việc.
- Tự động hóa các luồng xét duyệt hồ sơ, chấm điểm phỏng vấn và cảnh báo hợp đồng sắp hết hạn.
- Cung cấp bảng điều khiển (Dashboard) và báo cáo phân tích số liệu quân số, chi phí tuyển dụng theo thời gian thực.
- Đảm bảo tuân thủ các quy định pháp luật lao động Việt Nam về hợp đồng, bảo hiểm và lưu trữ hồ sơ nhân sự.

### 1.2. Đối tượng Người dùng & Ma trận Phân quyền (RBAC Matrix)

| Vai trò (Role) | Mã quyền | Quyền hạn & Trách nhiệm chính |
| :--- | :--- | :--- |
| **Super Admin** | `ROLE_ADMIN` | Quản trị toàn bộ cấu hình hệ thống, quản lý tài khoản, phân quyền bảo mật, tra cứu Audit Log. |
| **HR Director / Manager** | `ROLE_HR_MGR` | Phê duyệt đề xuất tuyển dụng, duyệt Offer letter, ký duyệt quyết định bổ nhiệm/điều chuyển/sa thải, xem toàn bộ báo cáo phân tích. |
| **Talent Acquisition (Recruiter)** | `ROLE_RECRUITER` | Quản lý tin tuyển dụng (Job Posting), sàng lọc CV, xếp lịch phỏng vấn, theo dõi bảng Kanban ATS, gửi thư mời phỏng vấn & Offer. |
| **Hiring Manager / Interviewer** | `ROLE_INTERVIEWER` | Tạo đề xuất tuyển dụng (Requisition), tham gia hội đồng phỏng vấn, chấm điểm ứng viên trên Scorecard, đưa ra khuyến nghị tuyển dụng. |
| **HR Officer (C&B / Records)** | `ROLE_HR_OFFICER` | Quản lý danh bạ hồ sơ nhân viên, soạn thảo và theo dõi hợp đồng lao động, theo dõi danh mục công việc tiếp nhận (Onboarding Checklist). |
| **Employee (Nhân viên)** | `ROLE_EMPLOYEE` | Xem thông tin hồ sơ cá nhân, xem sơ đồ tổ chức phòng ban, tra cứu thông tin hợp đồng của chính mình. |

---

## 2. Đặc Tả Chi Tiết Các Phân Hệ Chức Năng

```
QLNS / NexusHR
├── PHÂN HỆ 1: QUẢN LÝ TUYỂN DỤNG THÔNG MINH (ATS)
│   ├── [REC-01] Quản lý Yêu cầu & Tin Tuyển dụng (Job Requisitions)
│   ├── [REC-02] Tiếp nhận & Trích xuất Hồ sơ Ứng viên (CV Intake & AI Parsing)
│   ├── [REC-03] Đường ống Tuyển dụng Trực quan (Kanban ATS Pipeline)
│   ├── [REC-04] Lịch Phỏng vấn & Thư Mời (Interview Scheduling)
│   ├── [REC-05] Đánh giá Phỏng vấn (Interview Scorecard)
│   └── [REC-06] Quản lý Đề nghị Tuyển dụng (Offer Management)
├── PHÂN HỆ 2: QUẢN LÝ HỒ SƠ & VÒNG ĐỜI NHÂN SỰ (CORE HR)
│   ├── [EMP-01] Danh bạ & Hồ sơ Tổng thể Nhân viên (Employee Master Data)
│   ├── [EMP-02] Cơ cấu Tổ chức & Phòng Ban (Organizational Framework)
│   ├── [EMP-03] Quy trình Tiếp nhận Nhân viên Mới (Onboarding Checklist)
│   ├── [EMP-04] Quản lý Biến động Nhân sự (Internal Mobility & Events)
│   └── [EMP-05] Quản lý Tài liệu & Văn bản Nhân sự (Employee Documents)
├── PHÂN HỆ 3: QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (CONTRACTS)
│   ├── [CON-01] Soạn thảo & Lưu trữ Hợp đồng (Contract Drafting & Storage)
│   ├── [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
│   └── [CON-03] Quản lý Phụ lục Hợp đồng (Contract Addenda)
└── PHÂN HỆ 4: BÁO CÁO & PHÂN TÍCH NHÂN SỰ (HR ANALYTICS)
    ├── [REP-01] Thống kê Quân số & Biến động Cơ cấu (Headcount KPIs)
    └── [REP-02] Đo lường Hiệu suất Tuyển dụng (Hiring Velocity & Funnel)
```

---

## 3. Đặc Tả Chi Tiết Từng Tính Năng

### PHÂN HỆ 1: QUẢN LÝ TUYỂN DỤNG THÔNG MINH (ATS)

#### [REC-01] Quản lý Yêu cầu & Đăng tin Tuyển dụng (Job Requisitions)
- **Mục tiêu**: Cho phép Trưởng bộ phận gửi đề xuất tuyển người và Recruiter đăng tin tuyển dụng lên nhiều kênh.
- **Tác nhân**: Hiring Manager, HR Manager, Recruiter.
- **Tiền điều kiện**: Phòng ban tồn tại trong hệ thống.
- **Luồng xử lý chính**:
  1. Hiring Manager chọn phòng ban, chức danh, nhập số lượng cần tuyển (Target Headcount), lý do tuyển (thay thế/mở rộng), mức lương dự kiến và yêu cầu kỹ năng.
  2. Hệ thống kiểm tra ngân sách lương và chuyển trạng thái sang `Pending Approval`.
  3. HR Manager phê duyệt đề xuất.
  4. Recruiter lựa chọn kênh đăng tuyển (Cổng nội bộ Careers, LinkedIn, TopCV) và kích hoạt trạng thái `Active Recruiting`.
  5. Hệ thống sinh mã định danh công việc (ví dụ: `REQ-2026-08`).
- **Dữ liệu đầu vào**: Tiêu đề vị trí, phòng ban ID, số lượng tuyển, hình thức làm việc (Full-time/Part-time/Hybrid/Remote), dải lương (min - max), kênh phát hành, mô tả công việc (JD).
- **Hậu điều kiện**: Bản ghi được lưu vào bảng `job_postings`, sẵn sàng nhận hồ sơ ứng tuyển.

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
  - Hệ thống gọi API `POST /api/recruitment/applications/{id}/advance` để ghi nhận sự kiện chuyển bước và lưu thời điểm chuyển.
  - Nếu ứng viên không đạt: Chuyển sang trạng thái `Rejected` kèm lý do từ chối; hệ thống cho phép kích hoạt gửi email cảm ơn mẫu.

---

#### [REC-04] Lịch Phỏng vấn & Gửi Thư Mời (Interview Scheduling)
- **Mục tiêu**: Phối hợp thời gian phỏng vấn giữa ứng viên và hội đồng phỏng vấn, tránh trùng lịch.
- **Tác nhân**: Recruiter, Interviewer, Candidate.
- **Luồng xử lý**:
  1. Recruiter chọn ứng viên trong giai đoạn phỏng vấn, chọn thành phần phỏng vấn (Interviewer IDs), thời gian bắt đầu/kết thúc, hình thức (Trực tiếp: phòng họp / Trực tuyến: Google Meet, Zoom, MS Teams link).
  2. Hệ thống tạo bản ghi trong bảng `interviews` với trạng thái `Scheduled`.
  3. Hệ thống gửi email tự động kèm file lịch (`.ics`) đến ứng viên và người phỏng vấn.

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

---

#### [REC-06] Quản lý Đề nghị Tuyển dụng (Offer Management)
- **Mục tiêu**: Lập và phê duyệt thư mời nhận việc trước khi gửi cho ứng viên trúng tuyển.
- **Tác nhân**: Recruiter, HR Manager, Candidate.
- **Luồng xử lý**:
  1. Recruiter khởi tạo Offer dựa trên vị trí tuyển dụng: nhập Lương cơ bản (Base Salary), Thưởng gia nhập (Sign-on Bonus), Phụ cấp, Ngày bắt đầu làm việc dự kiến (Start Date), Hạn phản hồi (Expiration Date).
  2. Bản ghi được lưu vào bảng `offers` ở trạng thái `Draft`.
  3. HR Manager phê duyệt Offer -> Chuyển sang `Sent`.
  4. Ứng viên phản hồi:
     - **Chấp thuận (Accepted)**: Hệ thống tự động chuyển trạng thái hồ sơ sang `Hired & Ready`, đồng thời kích hoạt quy trình tạo hồ sơ nhân viên `[EMP-01]` và danh mục tiếp nhận `[EMP-03]`.
     - **Từ chối (Declined)**: Nhập lý do từ chối (lương chưa phù hợp, chọn công ty khác...).

---

### PHÂN HỆ 2: QUẢN LÝ HỒ SƠ & VÒNG ĐỜI NHÂN SỰ (CORE HR)

#### [EMP-01] Danh bạ & Hồ sơ Tổng thể Nhân viên (Employee Master Data)
- **Mục tiêu**: Quản lý thông tin định danh tập trung của toàn bộ cán bộ công nhân viên.
- **Dữ liệu quản lý**:
  - Thông tin cá nhân: Họ tên, Mã nhân viên (`EMP-xxxx`), Ngày sinh, Giới tính, CMND/CCCD, Quốc tịch, Tình trạng hôn nhân.
  - Thông tin liên hệ: Email công vụ, Email cá nhân, Số điện thoại di động, Địa chỉ thường trú, Địa chỉ tạm trú, Thông tin liên hệ khẩn cấp.
  - Thông tin công việc: Phòng ban ID, Chức vụ ID, Cấp bậc (Band/Grade: ví dụ IC-1 đến L-9), Quản lý trực tiếp (Manager ID), Địa điểm làm việc (Văn phòng Hà Nội, TP.HCM, Remote), Ngày vào công ty.
  - Trạng thái công tác: `Active` (Đang làm việc), `Probation` (Thử việc), `Suspended` (Tạm hoãn), `Terminated` (Đã nghỉ việc).
- **Yêu cầu hệ thống**:
  - Tìm kiếm toàn văn (Full-text search) theo Tên, Mã NV, Email hoặc Chức danh.
  - Lọc đa chiều theo Phòng ban, Cấp bậc, Trạng thái.

---

#### [EMP-02] Cơ cấu Tổ chức & Phòng Ban (Organizational Framework)
- **Mục tiêu**: Thể hiện trực quan mô hình phân cấp tổ chức (Phòng ban -> Bộ phận/Pod -> Vị trí -> Nhân viên).
- **Quy tắc nghiệp vụ**:
  - Mỗi phòng ban có Mã phòng ban duy nhất (`code`), Tên phòng ban, Trưởng phòng (Manager ID).
  - Phòng ban có thể có quan hệ cha-con (Parent Department) phục vụ mô hình tập đoàn đa chi nhánh.
  - Hệ thống tính toán tự động số lượng nhân sự trực thuộc (Headcount), Trung tâm chi phí (Cost Center) và Tỷ lệ kiểm soát (Span of Control) của từng quản lý.

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

---

#### [EMP-04] Quản lý Biến động Nhân sự (Internal Mobility & Events)
- **Mục tiêu**: Lưu vết toàn bộ lịch sử thuyên chuyển phòng ban, thăng chức, điều chỉnh chức danh hoặc kỷ luật/khen thưởng trong suốt quá trình công tác.
- **Tác nhân**: HR Manager, Director.
- **Luồng xử lý**:
  1. Khi có quyết định nhân sự, HR tạo bản ghi biến động trong bảng `employee_events`.
  2. Các loại sự kiện (`event_type`):
     - `Promotion` (Thăng chức / Nâng bậc)
     - `Transfer` (Điều chuyển phòng ban / Chi nhánh)
     - `Demotion` (Giáng chức)
     - `Salary_Adjustment` (Điều chỉnh bậc lương)
     - `Termination` (Chấm dứt hợp đồng)
  3. Hệ thống lưu lại giá trị cũ (`old_department_id`, `old_position_id`) và giá trị mới (`new_department_id`, `new_position_id`), ngày có hiệu lực (`effective_date`) và lý do.
  4. Khi đến ngày hiệu lực, hệ thống tự động cập nhật bản ghi chính của nhân viên trong bảng `employees`.

---

#### [EMP-05] Quản lý Tài liệu & Văn bản Hồ sơ (Employee Documents)
- **Mục tiêu**: Lưu trữ an toàn bản scan các giấy tờ chứng chỉ, sơ yếu lý lịch, bằng cấp, cam kết bảo mật (NDA).
- **Dữ liệu**: Bảng `employee_documents` lưu loại tài liệu (`document_type`), tên file, đường dẫn lưu trữ an toàn (`file_path`) và ngày tải lên.

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
  - Mỗi hợp đồng có Số hợp đồng (`contract_number`) duy nhất.
  - Ghi nhận Ngày bắt đầu (`start_date`), Ngày kết thúc (`end_date`), Mức lương cơ bản (`base_salary`), Tiền lương đóng BHXH, Các khoản phụ cấp cố định.
  - Hỗ trợ lưu trữ file số hóa đã ký (PDF) hoặc trạng thái chữ ký số DocuSign/VNPT-CA.

---

#### [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
- **Mục tiêu**: Ngăn ngừa rủi ro pháp lý do quên gia hạn hợp đồng đúng thời hạn luật định.
- **Quy tắc cảnh báo**:
  - Hợp đồng thử việc: Cảnh báo trước **7 ngày** và **15 ngày** trước khi hết hạn để quản lý hoàn thành đánh giá thử việc.
  - Hợp đồng xác định thời hạn: Cảnh báo trước **30 ngày** và **45 ngày** trước khi hết hạn.
- **Hành động hệ thống**:
  - Hiển thị badge màu hổ phách/đỏ trên giao diện Dashboard.
  - Gửi thông báo đến HR phụ trách hợp đồng để lập thông báo chấm dứt hoặc ký hợp đồng mới.

---

### PHÂN HỆ 4: BÁO CÁO & PHÂN TÍCH NHÂN SỰ (HR ANALYTICS)

#### [REP-01] Bảng Điều Khiển Tổng Hợp Quân Số (Headcount Dashboard)
- **Các chỉ số KPI**:
  - Tổng số nhân viên đang làm việc (Total Headcount & Active Employees).
  - Số lượng nhân viên đang thử việc (Probation Employees).
  - Cơ cấu nhân sự theo phòng ban (Headcount Distribution by Department).
  - Tỷ lệ hợp đồng theo loại hình (Permanent vs. Fixed-term vs. Probation).
  - Tỷ lệ hoàn thiện hồ sơ gốc (Dossier Completion Rate: mục tiêu > 95%).

#### [REP-02] Đo lường Hiệu Suất Tuyển Dụng (Hiring Analytics)
- **Các chỉ số KPI**:
  - Phễu chuyển đổi tuyển dụng (Recruitment Funnel Conversion Rates qua 6 giai đoạn).
  - Tốc độ tuyển dụng trung bình (Time-to-Hire: số ngày từ lúc ứng tuyển đến khi nhận việc).
  - Hiệu quả kênh nguồn (Sourcing Channel ROI: LinkedIn vs. TopCV vs. Careers Portal vs. Referral).
  - Tỷ lệ chấp thuận thư mời nhận việc (Offer Acceptance Rate: mục tiêu > 85%).
