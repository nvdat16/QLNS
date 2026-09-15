# 📘 TÀI LIỆU ĐẶC TẢ TÍNH NĂNG HỆ THỐNG (SOFTWARE REQUIREMENTS SPECIFICATION - SRS)
## Hệ Thống Quản Trị Nhân Sự & Tuyển Dụng Tập Trung (QLNS / NexusHR)

---

## 1. Giới Thiệu Tổng Quan

> **Trạng thái triển khai:** tài liệu này là đặc tả yêu cầu mục tiêu. Project hiện chỉ có UI/UX prototype và thiết kế database cho các chức năng chính; chưa có frontend/backend. Các câu mô tả hành vi hệ thống bên dưới là yêu cầu để phát triển và nghiệm thu trong tương lai, không phải mô tả chức năng đang chạy.

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

### 1.3. Phạm vi triển khai và nguyên tắc đặc tả

- **Đã có ở mức thiết kế dữ liệu**: ATS, hồ sơ nhân sự, onboarding và hợp đồng (14 bảng trong `database/init.sql`). DDL chưa được quản lý bởi migration/runtime của backend.
- **Cần mở rộng mô hình dữ liệu trước khi triển khai**: chấm công, nghỉ phép, lương thưởng, hiệu suất, đào tạo, tài khoản/RBAC, thông báo và audit log. Các tính năng này được đặc tả trong tài liệu để làm baseline cho các giai đoạn tiếp theo; không được giả định là đã có bảng dữ liệu hoặc API sản xuất.
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
│   └── [EMP-05] Quản lý Tài liệu & Văn bản Nhân sự (Employee Documents)
├── PHÂN HỆ 3: QUẢN LÝ HỢP ĐỒNG LAO ĐỘNG (CONTRACTS)
│   ├── [CON-01] Soạn thảo & Lưu trữ Hợp đồng (Contract Drafting & Storage)
│   ├── [CON-02] Giám sát Thời hạn & Cảnh báo Tự động (Expiration Alerts)
│   └── [CON-03] Quản lý Phụ lục Hợp đồng (Contract Addenda)
├── PHÂN HỆ 4: BÁO CÁO & PHÂN TÍCH NHÂN SỰ (HR ANALYTICS)
│   ├── [REP-01] Thống kê Quân số & Biến động Cơ cấu (Headcount KPIs)
│   └── [REP-02] Đo lường Hiệu suất Tuyển dụng (Hiring Velocity & Funnel)
├── PHÂN HỆ 5: CHẤM CÔNG & NGHỈ PHÉP (ATTENDANCE & LEAVE)
│   ├── [ATT-01] Danh mục ca làm việc & Lịch phân ca
│   ├── [ATT-02] Ghi nhận & Hiệu chỉnh chấm công
│   └── [ATT-03] Quản lý quỹ phép, đơn nghỉ & phê duyệt
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
  - Hệ thống gọi API `POST /api/recruitment/applications/{id}/advance` để ghi nhận sự kiện chuyển bước và lưu thời điểm chuyển.
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
- **Phụ thuộc dữ liệu**: Schema hiện tại chỉ có `departments(id, name, code, description)`; các trường `parent_department_id`, `manager_id` và `cost_center` cần migration riêng trước khi hỗ trợ cây tổ chức nhiều cấp và quản lý trưởng phòng ở cấp phòng ban.

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
  2. Các loại sự kiện (`event_type`):
     - `Promotion` (Thăng chức / Nâng bậc)
     - `Transfer` (Điều chuyển phòng ban / Chi nhánh)
     - `Demotion` (Giáng chức)
     - `Salary_Adjustment` (Điều chỉnh bậc lương)
     - `Termination` (Chấm dứt hợp đồng)
  3. Hệ thống lưu lại giá trị cũ (`old_department_id`, `old_position_id`) và giá trị mới (`new_department_id`, `new_position_id`), ngày có hiệu lực (`effective_date`) và lý do.
  4. Khi đến ngày hiệu lực, hệ thống tự động cập nhật bản ghi chính của nhân viên trong bảng `employees`.
- **Quy tắc phê duyệt và hiệu lực**:
  - Bản ghi biến động cần trạng thái `Draft`/`Pending Approval`/`Approved`/`Cancelled` trong bảng hoặc workflow hỗ trợ; chỉ sự kiện `Approved` mới được áp dụng vào `employees`.
  - Không cho phép hai biến động hiệu lực cùng ngày làm thay đổi cùng một trường của nhân viên. Khi hủy sau khi áp dụng, tạo sự kiện điều chỉnh mới thay vì sửa hoặc xóa lịch sử.
  - Điều chỉnh lương phải liên kết với phụ lục hợp đồng hoặc quyết định lương đã được phê duyệt.

---

#### [EMP-05] Quản lý Tài liệu & Văn bản Hồ sơ (Employee Documents)
- **Mục tiêu**: Lưu trữ an toàn bản scan các giấy tờ chứng chỉ, sơ yếu lý lịch, bằng cấp, cam kết bảo mật (NDA).
- **Dữ liệu**: Bảng `employee_documents` lưu loại tài liệu (`document_type`), tên file, đường dẫn lưu trữ an toàn (`file_url`), người tải lên và ngày tải lên.
- **Quy tắc bảo mật**:
  - Chỉ chấp nhận loại tệp, dung lượng và nội dung đã được kiểm tra; file được lưu private và cấp quyền truy cập bằng URL có thời hạn, không dùng URL công khai cố định.
  - Phân quyền xem/tải theo loại tài liệu; các giấy tờ định danh và hợp đồng chỉ HR có thẩm quyền hoặc chính nhân viên được truy cập.
  - Ghi nhận phiên bản, thời hạn lưu trữ và trạng thái hết hạn của tài liệu; xóa vật lý chỉ sau khi hết thời hạn lưu trữ và đã được phê duyệt.

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
  - Mỗi phụ lục cần số tham chiếu, người lập, người phê duyệt, file đã ký và audit trail. Schema hiện tại chưa có bảng `contract_addenda`; cần bổ sung bảng trước khi triển khai.

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
