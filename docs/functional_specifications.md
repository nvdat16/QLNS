# 📘 TÀI LIỆU ĐẶC TẢ TÍNH NĂNG HỆ THỐNG (SOFTWARE REQUIREMENTS SPECIFICATION - SRS)
## Hệ Thống Quản Trị Nhân Sự & Tuyển Dụng Tập Trung (QLNS / NexusHR)

---

## 1. Giới Thiệu Tổng Quan

> **Trạng thái triển khai:** tài liệu này là đặc tả yêu cầu mục tiêu. Project có UI/UX prototype, canonical database/OpenAPI contract và source baseline REC-03.2; runtime chưa được build/tích hợp. Các hành vi ngoài slice được ghi rõ trong `user_stories.md` vẫn là yêu cầu tương lai, không phải chức năng đang chạy.

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
| **Line Manager (Quản lý trực tiếp)** | `ROLE_LINE_MANAGER` | Duyệt/từ chối đơn nghỉ phép, đơn hiệu chỉnh công và đơn tăng ca của nhân viên trong phạm vi quản lý; soát bảng công đội nhóm; thực hiện đánh giá hết thử việc. Phạm vi dữ liệu giới hạn theo `data_scope_type = 'department'`. |
| **Employee (Nhân viên)** | `ROLE_EMPLOYEE` | Xem thông tin hồ sơ cá nhân, xem sơ đồ tổ chức phòng ban, tra cứu thông tin hợp đồng của chính mình; chấm công, xem bảng công và quỹ phép của chính mình, gửi đơn nghỉ/hiệu chỉnh/tăng ca. |

### 1.3. Phạm vi triển khai và nguyên tắc đặc tả

- **Ba phân hệ được chọn triển khai trước**: Core HR (bao gồm Contracts), Recruitment và Attendance & Leave. Đây là phạm vi được ánh xạ đầy đủ xuống user story, canonical schema và API contract.
- **Đã có ở mức thiết kế dữ liệu**: toàn bộ ba phân hệ trên, cùng identity/RBAC, audit log và outbox. `database/schema.sql` là canonical schema contract v1 với **36 bảng**; DDL cũ (`init.sql`, `postgres_db.sql`, `dbml.txt`) đã được đánh dấu deprecated và schema chưa được quản lý bởi EF Core migration/runtime.
- **Đã có schema nhưng bị chặn bởi policy**: Attendance & Leave. Bảng dữ liệu đã được thiết kế, nhưng mọi công thức tính công và quỹ phép phụ thuộc chính sách chưa được HR/Legal phê duyệt — xem [Open Decisions — Attendance & Leave](open_decisions_attendance_leave.md). Canonical schema chặn kỹ thuật việc tính công theo policy còn ở trạng thái `draft`.
- **Chưa thuộc phạm vi triển khai trước**: lương thưởng, hiệu suất, đào tạo. Các phân hệ này được đặc tả ở mức tên gọi để giữ tính toàn vẹn của bản đồ chức năng; không được giả định là đã có bảng dữ liệu hoặc API.
- Mọi thao tác tạo, cập nhật, phê duyệt, từ chối và xuất dữ liệu phải kiểm tra quyền theo vai trò, lưu người thực hiện và thời điểm thực hiện.
- Các trạng thái nghiệp vụ phải được kiểm soát bằng tập giá trị hợp lệ; không cho phép cập nhật trực tiếp hoặc bỏ qua bước phê duyệt qua giao diện/API.

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
│   ├── [EMP-05] Quản lý Tài liệu & Văn bản Nhân sự (Employee Documents)
│   ├── [EMP-06] Đánh giá & Xác nhận Hết Thử việc (Probation Review)
│   └── [EMP-07] Thôi việc & Bàn giao (Offboarding & Handover)
├── PHÂN HỆ 3: QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (CONTRACTS)
│   ├── [CON-01] Soạn thảo & Lưu trữ Hợp đồng (Contract Drafting & Storage)
│   ├── [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
│   └── [CON-03] Quản lý Phụ lục Hợp đồng (Contract Addenda)
├── PHÂN HỆ 4: BÁO CÁO & PHÂN TÍCH NHÂN SỰ (HR ANALYTICS)
│   ├── [REP-01] Thống kê Quân số & Biến động Cơ cấu (Headcount KPIs)
│   └── [REP-02] Đo lường Hiệu suất Tuyển dụng (Hiring Velocity & Funnel)
├── PHÂN HỆ 5: CHẤM CÔNG & NGHỈ PHÉP (ATTENDANCE & LEAVE)
│   ├── [ATT-01] Danh mục ca làm việc, Lịch phân ca & Lịch lễ
│   ├── [ATT-02] Ghi nhận, Hiệu chỉnh chấm công & Tăng ca
│   ├── [ATT-03] Quản lý quỹ phép, đơn nghỉ & phê duyệt
│   └── [ATT-04] Bảng công, Duyệt kỳ công & Khóa kỳ (Timesheet Processing)
├── PHÂN HỆ 6: LƯƠNG THƯỞNG & PHÚC LỢI (PAYROLL & BENEFITS)
│   ├── [PAY-01] Cấu hình kỳ lương & tính lương
│   ├── [PAY-02] Phiếu lương, phê duyệt & chi trả
│   └── [PAY-03] Bảo hiểm, thuế & đối soát
├── PHÂN HỆ 7: HIỆU SUẤT, ĐÀO TẠO & PHÁT TRIỂN
│   ├── [PFT-01] Mục tiêu & đánh giá KPI/OKR
│   ├── [PFT-02] Phản hồi 360 độ
│   └── [LRN-01] Khóa học, tiến độ & chứng chỉ
└── PHÂN HỆ 8: QUẢN TRỊ HỆ THỐNG & DỊCH VỤ DÙNG CHUNG
    ├── [SYS-01] Tài khoản, xác thực & phân quyền
    ├── [SYS-02] Thông báo & tác vụ cần xử lý
    ├── [SYS-03] Nhật ký kiểm toán & bảo vệ dữ liệu cá nhân
    └── [SYS-04] Tích hợp, xuất dữ liệu & vận hành hệ thống
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
- **Quy tắc và ngoại lệ**:
  - Trạng thái hợp lệ: `Draft` → `Pending Approval` → `Approved` → `Active Recruiting` → `Closed` hoặc `Cancelled`; chỉ HR Manager được phê duyệt/từ chối.
  - Khi từ chối, bắt buộc nhập lý do và trả yêu cầu về `Draft` để Hiring Manager chỉnh sửa; không được đăng tin khi chưa `Approved`.
  - `closing_date` phải sau ngày đăng; `salary_min` không được lớn hơn `salary_max`; `target_headcount` phải lớn hơn 0.
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
  - Trạng thái công tác: `Active` (Đang làm việc), `Probation` (Thử việc), `Suspended` (Tạm hoãn), `Terminated` (Đã nghỉ việc).
- **Yêu cầu hệ thống**:
  - Tìm kiếm toàn văn (Full-text search) theo Tên, Mã NV, Email hoặc Chức danh.
  - Lọc đa chiều theo Phòng ban, Cấp bậc, Trạng thái.
  - Nhân viên chỉ được xem và đề nghị chỉnh sửa các trường hồ sơ của chính mình; HR Officer/HR Manager được sửa dữ liệu nghiệp vụ theo phạm vi được phân quyền.
  - Email công vụ và mã nhân viên phải duy nhất. Thay đổi email, phòng ban, chức danh, trạng thái hoặc quản lý trực tiếp phải đi qua luồng biến động `[EMP-04]`, không được sửa trực tiếp từ màn hình hồ sơ.
  - Các trường định danh nhạy cảm (CCCD, thông tin ngân hàng, liên hệ khẩn cấp) phải được mã hóa khi lưu trữ, che một phần khi hiển thị và không xuất trong báo cáo mặc định.

---

#### [EMP-02] Cơ cấu Tổ chức & Phòng Ban (Organizational Framework)
- **Mục tiêu**: Thể hiện trực quan mô hình phân cấp tổ chức (Phòng ban -> Bộ phận/Pod -> Vị trí -> Nhân viên).
- **Quy tắc nghiệp vụ**:
  - Mỗi phòng ban có Mã phòng ban duy nhất (`code`) và tên phòng ban.
  - Hệ thống tính toán tự động số lượng nhân sự trực thuộc (Headcount), Trung tâm chi phí (Cost Center) và Tỷ lệ kiểm soát (Span of Control) của từng quản lý.
  - Không cho phép xóa phòng ban còn nhân viên hoặc requisition đang hiệu lực; phải điều chuyển hoặc đóng các bản ghi liên quan trước.
- **Phụ thuộc dữ liệu**: Canonical schema đã có `departments(id, code, name, parent_department_id, cost_center, description, version)` cùng constraint `ck_departments_not_self_parent`, đủ để dựng cây tổ chức nhiều cấp và tính cost center. Reporting line được lấy từ `employees.manager_id`.
  - **Còn thiếu**: cột `manager_id` ở cấp phòng ban (trưởng phòng chính danh của đơn vị). Hiện chỉ suy ra được qua `employees.manager_id`, nên chưa biểu diễn được trường hợp phòng ban tạm thời không có nhân sự trực thuộc. Cần migration riêng nếu nghiệp vụ yêu cầu.
  - Cây tổ chức phải chặn chu trình ở tầng service (A là cha của B, B là cha của A); constraint hiện chỉ chặn tự trỏ chính nó.

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
  - Dashboard phải cảnh báo task quá hạn và task chặn ngày nhận việc (ví dụ: chưa cấp tài khoản hoặc chưa ký hợp đồng thử việc).

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
     - `suspension` (Tạm hoãn thực hiện hợp đồng / Tạm đình chỉ công tác)
     - `return_to_work` (Trở lại làm việc sau tạm hoãn)
     - `termination` (Chấm dứt hợp đồng) — sinh từ `[EMP-07]`
  3. Hệ thống lưu lại giá trị cũ (`old_department_id`, `old_position_id`) và giá trị mới (`new_department_id`, `new_position_id`), ngày có hiệu lực (`effective_date`) và lý do.
  4. Khi đến ngày hiệu lực, hệ thống tự động cập nhật bản ghi chính của nhân viên trong bảng `employees`.
- **Quy tắc phê duyệt và hiệu lực**:
  - Bản ghi biến động cần trạng thái `Draft`/`Pending Approval`/`Approved`/`Cancelled` trong bảng hoặc workflow hỗ trợ; chỉ sự kiện `Approved` mới được áp dụng vào `employees`.
  - Không cho phép hai biến động hiệu lực cùng ngày làm thay đổi cùng một trường của nhân viên. Khi hủy sau khi áp dụng, tạo sự kiện điều chỉnh mới thay vì sửa hoặc xóa lịch sử.
  - Điều chỉnh lương phải liên kết với phụ lục hợp đồng hoặc quyết định lương đã được phê duyệt.
  - Cặp `suspension` / `return_to_work` phải cân: không được tạo `return_to_work` khi nhân viên không ở trạng thái `suspended`, và không được tạo `suspension` thứ hai khi chưa có `return_to_work`. Trong thời gian `suspended`, hệ thống không sinh dòng bảng công tính lương và không cộng quỹ phép.

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
  - Dashboard phải cảnh báo đỏ các phiếu quá `review_due_date` mà còn `pending`/`in_review`. Đây là rủi ro pháp lý, không chỉ là trễ quy trình.

---

#### [EMP-07] Thôi việc & Bàn giao (Offboarding & Handover)
- **Mục tiêu**: Quản lý toàn bộ thủ tục chấm dứt quan hệ lao động: bàn giao công việc, thu hồi tài sản và tài khoản, chốt công nợ và lưu trữ hồ sơ theo thời hạn.
- **Tác nhân**: Employee (gửi đơn), Line Manager (xác nhận bàn giao), HR Officer (điều phối), HR Manager (phê duyệt), IT Admin, Admin Logistics, Finance.
- **Tiền điều kiện**: Nhân viên đang ở trạng thái `active`, `probation` hoặc `suspended`.
- **Luồng xử lý chính**:
  1. HR Officer tạo `offboarding_cases` với `separation_type`, `notice_received_on`, `last_working_date`, người nhận bàn giao (`handover_to_employee_id`) và lý do.
  2. Hệ thống đối chiếu `notice_received_on` với thời hạn báo trước (`contracts.notice_period_days`) và cảnh báo nếu không đủ, nhưng không tự chặn — quyết định thuộc HR Manager.
  3. HR Manager phê duyệt; case chuyển `approved` và hệ thống sinh `offboarding_tasks` từ template theo phòng ban, chức danh và loại chấm dứt:
     - `it`: thu hồi laptop và thiết bị, khóa tài khoản email/Slack/Git/ERP, chuyển quyền sở hữu repository và tài liệu.
     - `admin`: thu thẻ nhân viên, thẻ gửi xe, bàn giao chỗ làm việc.
     - `hr`: phỏng vấn thôi việc, lập quyết định chấm dứt, chốt sổ bảo hiểm, trả hồ sơ gốc.
     - `manager`: xác nhận bàn giao công việc, tài liệu và đầu mối liên hệ cho người nhận.
     - `finance`: chốt công nợ, tạm ứng, thanh toán phép chưa dùng.
  4. Người phụ trách đánh dấu hoàn thành từng task; case chuyển `in_progress`.
  5. Khi toàn bộ task có `blocks_last_working_day = true` đã `completed` và `final_settlement_status` đạt `paid` hoặc `waived`, HR Officer đóng case (`completed`).
  6. Hệ thống tạo `employee_events` với `event_type = 'termination'` và `effective_date = last_working_date`; đến ngày hiệu lực, `employees.status` chuyển `terminated`.
- **Hậu điều kiện**: Nhân viên `terminated` không còn quyền truy cập hệ thống (tài khoản `users.status = 'disabled'`), không xuất hiện trong danh bạ đang làm việc, nhưng hồ sơ và tài liệu vẫn được giữ theo `employee_documents.retention_until`.
- **Quy tắc và ngoại lệ**:
  - Mỗi nhân viên chỉ có tối đa một case đang mở (`ux_offboarding_open_case`). Thôi việc rồi tái tuyển dụng tạo case mới, không mở lại case cũ.
  - `handover_to_employee_id` không được là chính nhân viên thôi việc (`ck_offboarding_handover_not_self`) và phải là nhân viên `active`.
  - Không được đóng case khi còn task chặn chưa hoàn thành. Bỏ qua task chặn cần HR Manager duyệt và ghi lý do vào audit log.
  - Khóa tài khoản phải thực hiện đúng `last_working_date`, không sớm hơn, để nhân viên còn hoàn thành bàn giao.
  - Nhân viên đang có đơn nghỉ phép `approved` sau `last_working_date` thì các đơn đó phải được hủy và trả lại quỹ trước khi đóng case.
  - Phép năm chưa dùng phải được chốt và đưa vào `final_settlement_status`; công thức quy đổi thuộc chính sách chưa phê duyệt — xem [Open Decisions](open_decisions_attendance_leave.md).
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
  - Hiển thị badge màu hổ phách/đỏ trên giao diện Dashboard.
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
  - Mỗi phụ lục cần số tham chiếu, người lập, người phê duyệt, file đã ký và audit trail trong bảng canonical `contract_addenda`; migration runtime vẫn phải được tạo và review trước khi triển khai.

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
- **Yêu cầu chung cho báo cáo**:
  - Mọi chỉ số phải hỗ trợ lọc theo khoảng thời gian, phòng ban, vị trí và địa điểm khi dữ liệu có sẵn; giao diện phải hiển thị thời điểm làm mới dữ liệu.
  - Quy ước tính toán phải được công bố trong tooltip/tài liệu: `Time-to-Hire = accepted_at - applied_at`; Offer Acceptance Rate chỉ tính Offer có phản hồi trong khoảng lọc.
  - Chỉ vai trò được cấp quyền mới được xem lương, dữ liệu định danh hoặc xuất Excel/CSV/PDF. Bản xuất phải có watermark người xuất, thời điểm xuất và phạm vi dữ liệu.

---

### PHÂN HỆ 5: CHẤM CÔNG & NGHỈ PHÉP (ATTENDANCE & LEAVE)

> [!IMPORTANT]
> **Trạng thái:** `Proposed`. Canonical schema đã có đủ bảng (xem [Database Design §5–§6](../database/database_design.md#5-attendance-module-canonical-proposed)), nhưng **mọi con số và công thức trong phần này là giá trị đề xuất, chưa được HR/Legal phê duyệt**. Mã quyết định dạng `[OD-x.y]` tham chiếu tới [Open Decisions — Attendance & Leave](open_decisions_attendance_leave.md); khi quyết định được chốt, giá trị ở đó thắng và phần này phải được cập nhật.
> Nhóm kỹ thuật không tự suy diễn quy định lao động. Hệ thống chặn kỹ thuật việc tính công theo `attendance_policies` còn ở trạng thái `draft` và việc dùng `leave_types` có `policy_status = 'draft'`.

#### Nguyên tắc chung của phân hệ

- **Tách dữ liệu thô và dữ liệu dẫn xuất.** `attendance_events` là append-only, không sửa không xóa. Mọi con số công (`attendance_daily_records`) là dữ liệu tính lại được, luôn ghi kèm `policy_version` đã dùng.
- **Server tính, client không tính.** Số đơn vị phép (`requested_units`), số phút công, số phút muộn/về sớm và số dư quỹ khả dụng (`available_units`) đều do server tính. Client chỉ hiển thị. Đây là điều kiện để con số trên UI và con số đi vào lương luôn khớp nhau.
- **Múi giờ là bắt buộc, không mặc định.** Mọi mốc thời gian lưu `timestamptz` (UTC) và mọi bản ghi mang `timezone` tường minh. "Ngày công" (`work_date`) là giá trị được quy đổi, không phải cắt chuỗi từ timestamp.
- **Quy tắc trùng lặp được chặn ở tầng database.** Trùng đơn nghỉ, trùng đơn tăng ca, trùng kỳ công, trùng sự kiện thiết bị và hai ca trong một ngày đều là `UNIQUE`/`EXCLUDE` constraint, không phải kiểm tra đọc-rồi-ghi ở application.
- **Kỳ công đã khóa là bất biến.** Sau `timesheet_periods.status = 'locked'`, không bản ghi nào trong kỳ được thay đổi. Sửa số liệu buộc phải mở lại kỳ, có lý do và có audit log.

---

#### [ATT-01] Danh mục Ca làm việc, Lịch phân ca & Lịch lễ
- **Mục tiêu**: Định nghĩa thời gian làm việc chuẩn của doanh nghiệp làm cơ sở đối chiếu cho mọi phép tính công, và thiết lập lịch nghỉ lễ áp dụng.
- **Tác nhân**: HR Officer (định nghĩa và phân ca), HR Manager (phê duyệt lịch lễ), Line Manager (xem lịch đội nhóm), Employee (xem lịch của mình).
- **Tiền điều kiện**: Nhân viên tồn tại và ở trạng thái `active`, `probation` hoặc `suspended`.
- **Luồng xử lý chính**:
  1. HR Officer tạo định nghĩa ca trong `work_shifts`: mã ca, giờ bắt đầu/kết thúc, múi giờ, thời gian nghỉ giữa ca, số phút công chuẩn và hệ số công.
  2. Hệ thống tự suy ra `crosses_midnight` từ giờ bắt đầu/kết thúc, không cho người dùng nhập giá trị mâu thuẫn (`ck_shift_crosses_midnight`).
  3. HR Officer phân ca theo tuần cho từng nhân viên hoặc theo phòng ban, ghi vào `work_schedule_assignments`.
  4. HR Officer nhập lịch nghỉ lễ của năm vào `holidays`; HR Manager phê duyệt.
  5. Nhân viên và Line Manager xem lịch ca và lịch lễ trên giao diện lịch tuần.
- **Dữ liệu đầu vào**: Mã/tên ca, giờ bắt đầu, giờ kết thúc, múi giờ, phút nghỉ giữa ca, phút công chuẩn, hệ số công, cờ đang sử dụng; danh sách ngày lễ kèm cờ hưởng lương và hệ số làm việc ngày lễ.
- **Hậu điều kiện**: Mỗi nhân viên có tối đa một ca cho mỗi ngày (`ux_schedule_employee_date`). Ngày không có bản ghi phân ca được hiểu là ngày nghỉ (`status = 'day_off'`), hệ thống **không suy diễn ca mặc định**.
- **Quy tắc và ngoại lệ**:
  - `standard_work_minutes` trong khoảng 1–1440; `break_minutes` trong khoảng 0–1440; `work_coefficient` trong khoảng 0–5.
  - Không được xóa ca đang được tham chiếu bởi lịch phân ca hoặc bởi bảng công đã tính; chỉ được chuyển `active = false`.
  - Phân ca vào ngày thuộc kỳ công đã `locked` bị từ chối.
  - Ca qua đêm thuộc ngày công của giờ bắt đầu `[OD-1.2]`. Toàn bộ phép tính muộn/về sớm của ca qua đêm phải dùng cùng quy ước này.
  - Lịch lễ của năm sau phải được chốt trước 31/12; hệ thống cảnh báo nếu năm tới chưa có bản ghi `[OD-2.2]`.
  - Ngày lễ trùng ngày nghỉ hằng tuần và quy tắc nghỉ bù: `[OD-2.4]`.
- **Giá trị cần HR/Legal chốt**: múi giờ chuẩn `[OD-1.1]`, số phút công chuẩn một ngày `[OD-1.3]`, ngày nghỉ hằng tuần `[OD-1.4]`, danh sách ngày lễ `[OD-2.1]`, hệ số làm việc ngày lễ `[OD-2.3]`, khung giờ và hệ số làm đêm `[OD-1.8]`.

---

#### [ATT-02] Ghi nhận Chấm công, Hiệu chỉnh & Tăng ca
- **Mục tiêu**: Ghi nhận chính xác thời điểm vào/ra của nhân viên từ nhiều nguồn, cho phép sửa sai qua luồng có phê duyệt, và quản lý tăng ca có kiểm soát.
- **Tác nhân**: Employee, Line Manager, HR Officer, Thiết bị chấm công (hệ thống ngoài).
- **Tiền điều kiện**: Nhân viên có ca được phân cho ngày tương ứng, hoặc chấm công ngoài ca được đánh dấu để xem xét.

##### ATT-02.a — Ghi nhận check-in / check-out
- **Luồng xử lý chính**:
  1. Nhân viên chấm công qua ứng dụng web hoặc thiết bị gửi sự kiện qua webhook có xác thực.
  2. Hệ thống xác định `work_date` bằng cách quy đổi `occurred_at` theo `timezone` và theo ca được phân, xử lý đúng trường hợp ca qua đêm.
  3. Hệ thống kiểm tra idempotency:
     - Nguồn thiết bị: chống trùng theo cặp `device_id` + `external_event_id` (`ux_attendance_events_device`).
     - Nguồn ứng dụng: chống trùng theo `employee_id` + `idempotency_key`.
  4. Sự kiện được ghi vào `attendance_events` với `source`, `method`, và toạ độ nếu chấm công bằng GPS.
  5. Hệ thống tính lại `attendance_daily_records` cho ngày đó theo `policy_version` đang `active`.
- **Quy tắc và ngoại lệ**:
  - Sự kiện trùng được trả về `duplicate = true` với mã 2xx, **không tạo bản ghi thứ hai và không báo lỗi** — thiết bị offline gửi bù phải an toàn.
  - Toạ độ GPS phải có đồng thời latitude và longitude hoặc không có cả hai (`ck_attendance_event_geo`).
  - Sự kiện có `source = 'device'` bắt buộc có `device_id` và `external_event_id` (`ck_attendance_event_device`).
  - Chỉ có check-in mà không có check-out: ngày công ở `status = 'incomplete'`, hệ thống **không tự suy ra giờ ra** `[OD-3.4]`. Nhân viên phải tạo đơn hiệu chỉnh.
  - Nhiều lần vào/ra trong ngày: lấy `first_check_in_at` và `last_check_out_at`; các mốc giữa vẫn được lưu đầy đủ `[OD-3.5]`.
  - Sự kiện có `occurred_at` thuộc kỳ công đã `locked` bị từ chối và phải đi qua luồng mở lại kỳ.
  - Cửa sổ nhận dữ liệu gửi bù từ thiết bị offline: `[OD-3.3]`.
- **Giá trị cần HR/Legal chốt**: phương thức chấm công được chấp nhận `[OD-3.1]`, giới hạn vị trí khi chấm công GPS/web `[OD-3.2]`, ân hạn muộn/về sớm `[OD-1.6]`, ngưỡng tính vắng cả ngày `[OD-1.7]`, quy tắc làm tròn phút công `[OD-1.5]`.

##### ATT-02.b — Đề nghị và phê duyệt hiệu chỉnh công
- **Luồng xử lý chính**:
  1. Nhân viên mở ngày công cần sửa, xem giá trị hiện tại và nhập giờ vào/ra đề nghị kèm lý do bắt buộc.
  2. Hệ thống tạo `attendance_corrections` ở `status = 'pending'`, lưu snapshot giá trị trước (`current_check_in_at`, `current_check_out_at`) để đối chiếu.
  3. Line Manager duyệt hoặc từ chối, gửi kèm `If-Match` theo `version` để chống ghi đè đồng thời.
  4. Khi được duyệt, hệ thống ghi một `attendance_events` với `source = 'correction'`, tính lại ngày công, đặt `attendance_daily_records.status = 'corrected'`, trỏ `correction_id` và ghi `applied_at`.
- **Quy tắc và ngoại lệ**:
  - Mỗi nhân viên chỉ có một đơn `pending` cho một ngày (`ux_corrections_one_pending_per_day`).
  - `proposed_check_out_at` phải sau `proposed_check_in_at` (`ck_correction_range`).
  - Quyết định bắt buộc có `decided_by` và `decided_at` (`ck_correction_decided`); từ chối bắt buộc nhập lý do.
  - `applied_at` chỉ được ghi khi đơn ở `approved` (`ck_correction_applied`) — không tồn tại trạng thái "đã áp dụng nhưng chưa duyệt".
  - Sự kiện gốc từ hiệu chỉnh **không xóa sự kiện cũ**; lịch sử giữ đầy đủ cả giá trị thiết bị ghi và giá trị đã duyệt.
  - Không cho gửi hoặc duyệt đơn thuộc kỳ công đã `locked` `[OD-4.4]`.
- **Giá trị cần HR/Legal chốt**: người có quyền duyệt `[OD-4.1]`, thời hạn được gửi đơn `[OD-4.2]`, yêu cầu minh chứng đính kèm `[OD-4.3]`.

##### ATT-02.c — Đăng ký và phê duyệt tăng ca
- **Luồng xử lý chính**:
  1. Nhân viên hoặc Line Manager tạo `overtime_requests` với ngày, khoảng thời gian, loại tăng ca và lý do.
  2. Hệ thống xác định `overtime_category` (`weekday` / `weekly_rest` / `holiday` / `night`) từ lịch ca, lịch lễ và khung giờ đêm, rồi gán `work_coefficient` tương ứng.
  3. Line Manager duyệt; có thể duyệt số phút ít hơn số đăng ký (`approved_minutes <= requested_minutes`).
  4. Số phút được duyệt được cộng vào `attendance_daily_records.overtime_minutes` của ngày tương ứng.
- **Quy tắc và ngoại lệ**:
  - Hai đơn tăng ca `pending`/`approved` của cùng nhân viên không được giao nhau về thời gian (`ex_overtime_requests_no_overlap`).
  - `approved_minutes` không được lớn hơn `requested_minutes` (`ck_overtime_minutes`) — không cho duyệt vượt số đăng ký.
  - Chỉ ghi nhận tăng ca khi vượt `min_overtime_minutes` của policy `[OD-5.2]`.
  - `work_coefficient` trong khoảng 1–5; giá trị cụ thể theo từng loại là quyết định pháp lý `[OD-5.3]`.
  - Tăng ca chỉ được tính khi có bản ghi chấm công thực tế phủ khoảng thời gian đã duyệt; đơn đã duyệt mà không có dữ liệu chấm công tương ứng phải được hiển thị như một ngoại lệ cần xử lý.
- **Giá trị cần HR/Legal chốt**: đăng ký trước hay ghi nhận sau `[OD-5.1]`, hệ số theo loại tăng ca `[OD-5.3]`, **giới hạn giờ tăng ca theo ngày/tháng/năm `[OD-5.4]`** — hiện chưa có bảng hạn mức nên hệ thống chưa chặn được vượt trần, đây là rủi ro tuân thủ cần xử lý trước khi go-live.

---

#### [ATT-03] Quản lý Quỹ phép, Đơn nghỉ & Phê duyệt
- **Mục tiêu**: Quản lý chính xác quỹ phép của từng nhân viên và xử lý đơn nghỉ qua luồng phê duyệt có kiểm soát, đảm bảo số dư quỹ luôn nhất quán ngay cả khi có nhiều thao tác đồng thời.
- **Tác nhân**: Employee, Line Manager, HR Officer, HR Manager.
- **Tiền điều kiện**: Loại phép có `policy_status = 'approved'`; nhân viên có bản ghi `leave_balances` cho loại phép và năm tương ứng.
- **Luồng xử lý chính**:
  1. HR Officer cấu hình loại phép trong `leave_types`; HR Manager phê duyệt để `policy_status` chuyển `approved`.
  2. Hệ thống sinh quỹ phép năm cho từng nhân viên vào `leave_balances`.
  3. Nhân viên chọn loại phép, khoảng thời gian và đơn vị nghỉ (cả ngày / nửa ngày đầu / nửa ngày sau / theo giờ), nhập lý do.
  4. **Server tính `requested_units`** từ lịch phân ca, lịch lễ, múi giờ và policy — loại bỏ ngày không có ca và ngày lễ nếu `counts_holidays = false`. Giá trị client gửi lên (nếu có) bị bỏ qua.
  5. Hệ thống kiểm tra trong cùng một transaction: số ngày báo trước, giới hạn nghỉ liên tiếp, yêu cầu minh chứng, số dư khả dụng và trùng đơn.
  6. Nếu hợp lệ: tạo `leave_requests` ở `pending`, **cộng `reserved_units`** vào quỹ, ghi audit log và ghi outbox message để gửi thông báo — tất cả trong một transaction.
  7. Line Manager duyệt hoặc từ chối, gửi kèm `If-Match` theo `version`:
     - `approved`: chuyển `reserved_units` → `used_units`, ghi `leave_request_decisions`, đánh dấu các ngày liên quan trong `attendance_daily_records` là `on_leave` và trỏ `leave_request_id`.
     - `rejected`: trả lại `reserved_units`, bắt buộc nhập lý do.
  8. Với loại phép có `approval_levels > 1`, đơn chuyển sang cấp duyệt tiếp theo (`current_approval_level + 1`) thay vì kết thúc.
- **Hậu điều kiện**: `available_units` là generated column `entitled + carried_over - used - reserved`, luôn nhất quán và không thể bị client tính lệch.
- **Quy tắc và ngoại lệ**:
  - **Trùng đơn bị chặn ở tầng database** bằng `ex_leave_requests_no_overlap`: hai đơn `pending`/`approved` của cùng nhân viên không được giao nhau về thời gian. Trả `409` kèm mã lỗi ổn định.
  - Không đủ quỹ: trả `409` với mã lỗi nghiệp vụ, **không tạo bản ghi và không giữ chỗ quỹ**. Nếu `allow_negative_balance = true` cho loại phép đó thì cho phép vượt và hiển thị cảnh báo.
  - Gửi đơn trùng do retry được chống bằng `idempotency_key`; lần gửi lại trả về chính đơn đã tạo, không tạo đơn thứ hai.
  - Từ chối hoặc hủy đơn phải trả lại quỹ đúng số đã giữ chỗ. Không được có trạng thái quỹ bị giữ chỗ bởi một đơn đã kết thúc.
  - Hủy đơn đã duyệt: được phép khi ngày bắt đầu còn ở tương lai; hủy sau khi đã nghỉ phải qua HR Officer `[OD-7.4]`.
  - Quyết định trên đơn đã ở trạng thái kết thúc, hoặc với `version` đã cũ, trả `409` — không ghi đè.
  - Người duyệt phải nằm trong chuỗi phê duyệt hợp lệ và trong phạm vi dữ liệu được phân quyền; sai người duyệt trả `403`, không phải `409`.
  - Nhân viên không được xem lý do nghỉ của đồng nghiệp; lịch đội nhóm chỉ hiển thị trạng thái nghỉ `[OD-9.2]`.
  - Duyệt hàng loạt xử lý từng phần tử độc lập: một phần tử xung đột không được che kết quả của các phần tử khác.
  - Đơn nghỉ nằm sau `last_working_date` của một case thôi việc phải được hủy và trả quỹ trước khi đóng case (`[EMP-07]`).
- **Giá trị cần HR/Legal chốt**: danh mục loại phép `[OD-6.1]`, số ngày phép năm và thâm niên `[OD-6.2]`, cách cấp quỹ theo tháng hay đầu năm `[OD-6.3]`, quy tắc chuyển quỹ sang năm sau `[OD-6.4]`, cho phép quỹ âm `[OD-6.5]`, đơn vị nhỏ nhất `[OD-6.6]`, **ngày lễ/ngày nghỉ tuần trong khoảng nghỉ có bị trừ quỹ `[OD-6.7]`** (công thức quan trọng nhất của ATT-03), số ngày báo trước `[OD-6.8]`, yêu cầu minh chứng `[OD-6.9]`, số cấp phê duyệt `[OD-7.1]`, người duyệt thay khi quản lý vắng `[OD-7.2]`.

---

#### [ATT-04] Bảng công, Duyệt kỳ công & Khóa kỳ (Timesheet Processing)
- **Mục tiêu**: Đối soát chấm công với đơn nghỉ và đơn tăng ca thành một bảng công đã được xác nhận, khóa kỳ để số liệu bất biến, rồi bàn giao sang Payroll.
- **Tác nhân**: Employee (xem bảng công cá nhân), Line Manager (soát bảng công đội nhóm), HR Officer (đối soát), HR Manager (duyệt và khóa kỳ).
- **Tiền điều kiện**: Kỳ công tồn tại trong `timesheet_periods`; có `attendance_policies` ở trạng thái `active`.
- **Luồng xử lý chính**:
  1. HR Officer tạo kỳ công với `period_code`, ngày bắt đầu và ngày kết thúc. Các kỳ không được giao nhau (`ex_timesheet_periods_no_overlap`).
  2. Trong kỳ, hệ thống liên tục tính `attendance_daily_records` cho từng nhân viên từng ngày, hợp nhất bốn nguồn: sự kiện chấm công, ca được phân, đơn nghỉ đã duyệt và đơn tăng ca đã duyệt; ghi kèm `policy_version`.
  3. Cuối kỳ, HR Officer chuyển kỳ sang `pending_approval`. Hệ thống hiển thị danh sách ngoại lệ cần xử lý trước khi duyệt: ngày `incomplete`, ngày `absent` không có đơn, đơn hiệu chỉnh còn `pending`, đơn tăng ca đã duyệt nhưng không có dữ liệu chấm công.
  4. Line Manager soát bảng công của đội mình; HR Manager duyệt kỳ (`approved`).
  5. HR Manager khóa kỳ (`locked`), ghi `locked_by` và `locked_at`. Từ thời điểm này mọi ghi nhận và hiệu chỉnh vào kỳ đều bị từ chối.
  6. HR Officer bàn giao sang Payroll, ghi `payroll_handoff_at` và `payroll_handoff_reference`.
- **Hậu điều kiện**: Mỗi nhân viên có đúng một dòng công mỗi ngày (`ux_attendance_daily_employee_date`), mỗi dòng thuộc đúng một kỳ và công khai `policy_version` đã dùng để tính.
- **Quy tắc và ngoại lệ**:
  - Trạng thái kỳ: `open` → `pending_approval` → `approved` → `locked`; `reopened` khi mở lại.
  - Khóa kỳ bắt buộc có `locked_by` và `locked_at` (`ck_timesheet_period_locked`).
  - Mở lại kỳ bắt buộc có `reopened_by` và `reopen_reason` (`ck_timesheet_period_reopened`); chỉ HR Manager được thực hiện và hành động được ghi audit log `[OD-8.3]`.
  - `payroll_handoff_at` chỉ được ghi khi kỳ đã khóa (`ck_timesheet_period_handoff`) — không bàn giao số liệu chưa chốt.
  - Không được duyệt kỳ khi còn đơn hiệu chỉnh `pending` trong kỳ.
  - Khi policy thay đổi giữa kỳ, các ngày đã tính giữ nguyên `policy_version` cũ. Tái tính theo policy mới là hành động tường minh, có audit log, không tự động.
  - Nhân viên ở trạng thái `suspended` không sinh dòng công tính lương trong thời gian tạm hoãn.
  - Mở lại kỳ đã bàn giao Payroll: **chưa có cơ chế điều chỉnh** `[OD-8.5]`. Đây là khoảng trống phải chốt trước khi nối Payroll, nếu không việc mở lại kỳ sẽ tạo sai lệch giữa số đã bàn giao và số hiện tại.
- **Chỉ số hiển thị trên bảng công**: `scheduled_minutes`, `worked_minutes`, `late_minutes`, `early_leave_minutes`, `overtime_minutes`, `leave_units` và `status` (`on_time` / `late` / `early_leave` / `absent` / `incomplete` / `corrected` / `on_leave` / `holiday` / `day_off`).
- **Giá trị cần HR/Legal chốt**: chu kỳ kỳ công `[OD-8.1]`, hạn chốt kỳ `[OD-8.2]`, người khóa và mở lại kỳ `[OD-8.3]`, hình thức bàn giao Payroll `[OD-8.4]`, cơ chế điều chỉnh sau bàn giao `[OD-8.5]`.

---

### Phụ lục: Phạm vi dữ liệu của phân hệ Chấm công & Nghỉ phép

| Vai trò | Phạm vi xem bảng công | Phạm vi duyệt |
| :--- | :--- | :--- |
| Employee | Chỉ của chính mình (`data_scope_type = 'self'`) | — |
| Line Manager | Nhân viên trong phạm vi quản lý (`department`) | Đơn nghỉ, hiệu chỉnh công và tăng ca của nhân viên thuộc phạm vi |
| HR Officer | Toàn tổ chức | Hiệu chỉnh công khi nhân viên không có quản lý trực tiếp; hủy đơn nghỉ đã qua ngày nghỉ |
| HR Manager | Toàn tổ chức | Cấp duyệt thứ hai; duyệt và khóa kỳ công; mở lại kỳ đã khóa |
| Super Admin | Toàn tổ chức | Không duyệt nghiệp vụ; chỉ quản trị cấu hình và tra cứu audit log |

Quyền được kiểm tra phía server cho mọi request. Vi phạm phạm vi dữ liệu trả `403`, không trả danh sách rỗng — tránh việc người dùng suy ra sự tồn tại của dữ liệu ngoài phạm vi.
