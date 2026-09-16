# 📋 INVEST User Stories — 3 Phân Hệ Triển Khai Trước (QLNS / NexusHR)

> **Tài liệu chuẩn yêu cầu nghiệp vụ (Authoritative Requirements Baseline)**
> **Phạm vi:** ba phân hệ được chọn triển khai trước theo bản đồ chức năng — **Core HR** (bao gồm nhánh con Contracts), **Recruitment (ATS)** và **Attendance & Leave**.
> **Ánh xạ kiến trúc:** [Functional Specifications](functional_specifications.md) · [Use Cases](use_cases.md) · [Architecture (arc42 + C4)](architecture.md) · [Database Schema](../database/schema.sql) · [API Contract](../api/openapi.yaml)
> **Nguyên tắc thiết kế Story:** Tuân thủ tiêu chuẩn **INVEST** (*Independent, Negotiable, Valuable, Estimable, Small, Testable*).

> [!NOTE]
> Theo bản đồ chức năng, **Contract Management là nhánh con của Core HR** (cùng cấp với Employee Profiles, Organization Management và Employee Lifecycle). Tài liệu này giữ mã `CON-*` riêng cho dễ truy vết, nhưng về phạm vi triển khai thì `EMP-*` và `CON-*` cùng thuộc một phân hệ.

> [!IMPORTANT]
> Toàn bộ story `ATT-*` ở trạng thái **`Blocked — policy pending`**. Acceptance criteria của chúng đã được viết đầy đủ và có thể estimate, nhưng **các giá trị ngưỡng, hệ số và công thức còn là đề xuất chưa được HR/Legal phê duyệt**. Mã `[OD-x.y]` trong AC trỏ tới [Open Decisions — Attendance & Leave](open_decisions_attendance_leave.md). Không đưa story `ATT-*` vào sprint trước khi các mục tương ứng ở đó được chốt.

---

## Trạng thái theo phân hệ

| Phân hệ | Mã story | Trạng thái |
| :--- | :--- | :--- |
| **Recruitment (ATS)** | `REC-01` … `REC-06` — 10 story | Proposed · `REC-03.2` đã có source baseline |
| **Core HR — Profile, Organization, Lifecycle** | `EMP-01` … `EMP-07` — 10 story | Proposed |
| **Core HR — Contracts** | `CON-01` … `CON-03` — 4 story | Proposed |
| **Attendance & Leave** | `ATT-01` … `ATT-04` — 13 story | **Blocked — policy pending** |

---

## Mục lục

- [1. Quy ước & Cấu trúc User Story](#1-quy-ước--cấu-trúc-user-story)
- [2. Phân Hệ Tuyển Dụng Thông Minh (Smart ATS Recruitment)](#2-phân-hệ-tuyển-dụng-thông-minh-smart-ats-recruitment)
  - [REC-01: Quản lý Đề xuất & Tin Tuyển dụng](#rec-01-quản-lý-đề-xuất--tin-tuyển-dụng)
  - [REC-02: Tiếp nhận & Sàng lọc CV Ứng viên](#rec-02-tiếp-nhận--sàng-lọc-cv-ứng-viên)
  - [REC-03: Đường ống Tuyển dụng Kanban & Chuyển bước](#rec-03-đường-ống-tuyển-dụng-kanban--chuyển-bước)
  - [REC-04: Điều phối Lịch Phỏng vấn](#rec-04-điều-phối-lịch-phỏng-vấn)
  - [REC-05: Đánh giá Ứng viên qua Scorecard](#rec-05-đánh-giá-ứng-viên-qua-scorecard)
  - [REC-06: Đề nghị Tuyển dụng & Bàn giao Onboarding](#rec-06-đề-nghị-tuyển-dụng--bàn-giao-onboarding)
- [3. Phân Hệ Core HR — Hồ Sơ & Vòng Đời Nhân Sự](#3-phân-hệ-core-hr--hồ-sơ--vòng-đời-nhân-sự)
  - [EMP-01: Danh bạ & Hồ sơ Định danh Nhân viên](#emp-01-danh-bạ--hồ-sơ-định-danh-nhân-viên)
  - [EMP-02: Cơ cấu Tổ chức & Sơ đồ Phòng ban](#emp-02-cơ-cấu-tổ-chức--sơ-đồ-phòng-ban)
  - [EMP-03: Quy trình Tiếp nhận Nhân viên Mới (Onboarding)](#emp-03-quy-trình-tiếp-nhận-nhân-viên-mới-onboarding)
  - [EMP-04: Biến động Nhân sự & Quản lý Sự kiện Công tác](#emp-04-biến-động-nhân-sự--quản-lý-sự-kiện-công-tác)
  - [EMP-05: Quản lý Hồ sơ Tài liệu Điện tử An toàn](#emp-05-quản-lý-hồ-sơ-tài-liệu-điện-tử-an-toàn)
  - [EMP-06: Đánh giá & Xác nhận Hết Thử việc](#emp-06-đánh-giá--xác-nhận-hết-thử-việc)
  - [EMP-07: Thôi việc & Bàn giao](#emp-07-thôi-việc--bàn-giao)
- [4. Core HR — Nhánh Quản Lý Hợp Đồng Lao Động](#4-core-hr--nhánh-quản-lý-hợp-đồng-lao-động-contract-management)
  - [CON-01: Soạn thảo, Ký kết & Vòng đời Hợp đồng](#con-01-soạn-thảo-ký-kết--vòng-đời-hợp-đồng)
  - [CON-02: Giám sát Hạn Hợp đồng & Cảnh báo Tự động](#con-02-giám-sát-hạn-hợp-đồng--cảnh-báo-tự-động)
  - [CON-03: Quản lý Phụ lục Hợp đồng Lao động](#con-03-quản-lý-phụ-lục-hợp-đồng-lao-động)
- [5. Phân Hệ Chấm Công & Nghỉ Phép (Attendance & Leave)](#5-phân-hệ-chấm-công--nghỉ-phép-attendance--leave)
  - [ATT-01: Ca làm việc, Phân ca & Lịch lễ](#att-01-ca-làm-việc-phân-ca--lịch-lễ)
  - [ATT-02: Ghi nhận, Hiệu chỉnh Chấm công & Tăng ca](#att-02-ghi-nhận-hiệu-chỉnh-chấm-công--tăng-ca)
  - [ATT-03: Quỹ phép, Đơn nghỉ & Phê duyệt](#att-03-quỹ-phép-đơn-nghỉ--phê-duyệt)
  - [ATT-04: Bảng công, Duyệt kỳ & Khóa kỳ](#att-04-bảng-công-duyệt-kỳ--khóa-kỳ)
- [6. Ma trận Phân quyền & Traceability](#6-ma-trận-phân-quyền--traceability)

---

## 1. Quy ước & Cấu trúc User Story

Mỗi User Story trong tài liệu này được cấu trúc nhất quán gồm:
1. **Mã định danh (ID)**: Tương ứng với mã định danh phân hệ (`REC`, `EMP`, `CON`).
2. **Tiêu đề**: Tóm tắt hành động nghiệp vụ.
3. **Mô tả (User Story Statement)**: Theo cú pháp:
   > **Là một** `[Vai trò/Actor]`,  
   > **Tôi muốn** `[Hành động/Tính năng mong muốn]`,  
   > **Để** `[Giá trị nghiệp vụ đem lại]`.
4. **Tiền điều kiện (Preconditions)**: Các điều kiện bắt buộc trước khi thực hiện.
5. **Tiêu chí nghiệm thu (Acceptance Criteria - AC)**: Viết theo định dạng kịch bản hành vi **Given - When - Then** (*Gherkin syntax*), bao gồm kịch bản thành công và các trường hợp ngoại lệ.
6. **Ràng buộc kỹ thuật & Dữ liệu (Technical & Data Constraints)**: Quy định về bảo mật, RBAC, kiểm tra trùng lặp và tính toàn vẹn dữ liệu.

---

## 2. Phân Hệ Tuyển Dụng Thông Minh (Smart ATS Recruitment)

### REC-01: Quản lý Đề xuất & Tin Tuyển dụng

#### [REC-01.1] Khởi tạo Đề xuất Tuyển dụng mới (Create Job Requisition)
- **Mô tả**:
  > **Là một** Hiring Manager (Trưởng bộ phận),  
  > **Tôi muốn** tạo đề xuất tuyển dụng nhân sự cho phòng ban của mình kèm theo số lượng, mô tả công việc và mức lương dự kiến,  
  > **Để** trình cấp quản lý nhân sự phê duyệt kế hoạch tuyển dụng.
- **Tiền điều kiện**: Hiring Manager đã đăng nhập, thuộc về phòng ban hợp lệ và có quyền `ROLE_INTERVIEWER` hoặc `ROLE_DEPT_HEAD`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo đề xuất nháp thành công**
    - **Given** người dùng nhập đầy đủ: Tên vị trí, Phòng ban, Số lượng tuyển > 0, Dải lương hợp lệ (`salary_min <= salary_max`), Lý do tuyển dụng,
    - **When** người dùng chọn hành động "Lưu nháp",
    - **Then** hệ thống lưu bản ghi vào `job_postings` với trạng thái `draft`, sinh mã `job_code` duy nhất, và thông báo lưu thành công.
  - **Kịch bản 2: Báo lỗi khi dữ liệu không hợp lệ**
    - **Given** người dùng để trống Số lượng tuyển hoặc nhập `target_headcount <= 0` hoặc `salary_min > salary_max`,
    - **When** người dùng nhấn lưu hoặc gửi duyệt,
    - **Then** hệ thống chặn lưu, viền đỏ các trường lỗi và hiển thị thông điệp cảnh báo rõ ràng.
- **Ràng buộc kỹ thuật**: Lưu vết `created_by` là user hiện tại; bảng dữ liệu đích `job_postings`.

---

#### [REC-01.2] Phê duyệt & Đăng Tin Tuyển dụng (Approve & Publish Job Requisition)
- **Mô tả**:
  > **Là một** HR Manager,  
  > **Tôi muốn** xét duyệt các đề xuất tuyển dụng đang chờ và chuyển cho Recruiter kích hoạt đăng tin,  
  > **Để** kiểm soát định biên nhân sự và ngân sách chi trả trước khi công khai ra thị trường.
- **Tiền điều kiện**: Bản ghi đề xuất ở trạng thái `pending_approval`; người dùng có vai trò `ROLE_HR_MGR`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Phê duyệt đề xuất thành công**
    - **Given** đề xuất đang ở trạng thái `pending_approval`,
    - **When** HR Manager nhấn "Phê duyệt (Approve)",
    - **Then** trạng thái chuyển thành `approved`, hệ thống ghi nhận `approved_by` và gửi thông báo cho Recruiter phụ trách.
  - **Kịch bản 2: Từ chối đề xuất kèm lý do bắt buộc**
    - **Given** HR Manager xem xét đề xuất nhưng ngân sách không đáp ứng,
    - **When** nhấn "Từ chối (Reject)" nhưng không nhập lý do từ chối,
    - **Then** hệ thống chặn hành động và yêu cầu: *"Bắt buộc nhập lý do từ chối"*.
    - **When** đã nhập lý do và xác nhận từ chối,
    - **Then** trạng thái chuyển về `rejected` (hoặc trả lại `draft`), kèm lý do được lưu trong audit log.
  - **Kịch bản 3: Đăng tin tuyển dụng (Publishing)**
    - **Given** đề xuất đã ở trạng thái `approved`,
    - **When** Recruiter cấu hình kênh tuyển dụng và nhấn "Đăng tin (Publish)",
    - **Then** trạng thái chuyển sang `active`, tin hiển thị trên Careers Portal và cho phép tiếp nhận hồ sơ.

---

### REC-02: Tiếp nhận & Sàng lọc CV Ứng viên

#### [REC-02.1] Tiếp nhận Hồ sơ & Kiểm tra An toàn File (CV Intake & File Safety)
- **Mô tả**:
  > **Là một** Recruiter (hoặc Ứng viên nộp hồ sơ trực tuyến),  
  > **Tôi muốn** tải lên tệp CV (định dạng PDF/DOCX),  
  > **Để** hồ sơ ứng viên được đưa vào hệ thống lưu trữ an toàn và sẵn sàng xét duyệt.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Upload file hợp lệ**
    - **Given** tệp CV có định dạng `.pdf`, `.doc`, hoặc `.docx`, dung lượng <= 10MB,
    - **When** người dùng tải lên,
    - **Then** hệ thống quét mã độc, lưu trữ file vào private object storage, tạo bản ghi trong bảng `resumes` và liên kết với bảng `candidates`.
  - **Kịch bản 2: Chặn file vượt quá dung lượng hoặc sai định dạng**
    - **Given** tệp tải lên có đuôi `.exe`, `.zip` hoặc dung lượng > 10MB,
    - **When** người dùng nhấn upload,
    - **Then** hệ thống từ chối nhận file, thông báo lỗi cụ thể và không tạo bất kỳ bản ghi nào trong cơ sở dữ liệu.

---

#### [REC-02.2] Nhận diện Trùng lặp & Bóc tách Dữ liệu (Deduplication & Parsing)
- **Mô tả**:
  > **Là một** Recruiter,  
  > **Tôi muốn** hệ thống tự động kiểm tra trùng lặp thông tin ứng viên (Email/Số điện thoại) và bóc tách các trường cơ bản,  
  > **Để** tránh tình trạng một ứng viên có nhiều hồ sơ phân mảnh trong cơ sở dữ liệu.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Ứng viên mới hoàn toàn**
    - **Given** email và số điện thoại của ứng viên chưa từng xuất hiện trong bảng `candidates`,
    - **When** tạo hồ sơ ứng tuyển mới,
    - **Then** hệ thống tạo bản ghi mới trong `candidates` và tạo một `application` mới liên kết tới vị trí tuyển dụng.
  - **Kịch bản 2: Ứng viên đã có trong hệ thống**
    - **Given** email hoặc số điện thoại trùng khớp với một ứng viên đã tồn tại,
    - **When** nộp hồ sơ vào tin tuyển dụng mới,
    - **Then** hệ thống không tạo bản ghi candidate mới mà gắn `application` mới vào hồ sơ `candidate_id` sẵn có, đồng thời hiển thị lịch sử ứng tuyển trước đó.

---

### REC-03: Đường ống Tuyển dụng Kanban & Chuyển bước

#### [REC-03.1] Quản lý Hồ sơ trên Bảng Kanban Trực quan (Kanban Pipeline View)
- **Mô tả**:
  > **Là một** Recruiter,  
  > **Tôi muốn** xem danh sách ứng viên theo 6 cột trạng thái tuyển dụng chuẩn quốc tế: `Applied`, `Screening`, `Interview 1`, `Interview 2`, `Offer`, `Hired`,  
  > **Để** nắm bắt tức thì tiến độ và phân bố số lượng ứng viên theo từng giai đoạn.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Hiển thị đúng phân bổ giai đoạn**
    - **Given** một vị trí tuyển dụng có 20 hồ sơ ứng tuyển ở các giai đoạn khác nhau,
    - **When** Recruiter mở trang quản trị pipeline của vị trí đó,
    - **Then** các thẻ ứng viên được nhóm chính xác vào từng cột theo `current_stage`, mỗi cột hiển thị tổng số hồ sơ và điểm match score trung bình.
  - **Kịch bản 2: Lọc ứng viên nhanh chóng**
    - **Given** danh sách ứng viên phong phú,
    - **When** Recruiter tìm kiếm theo tên, từ khóa kỹ năng hoặc dải điểm đánh giá,
    - **Then** bảng Kanban chỉ giữ lại các thẻ thỏa mãn điều kiện lọc trong thời gian dưới 1 giây.

---

#### [REC-03.2] Chuyển Giai đoạn Tuyển dụng An toàn (Advance Application Stage)
- **Mô tả**:
  > **Là một** Recruiter,  
  > **Tôi muốn** chuyển ứng viên tiến một bước sang giai đoạn tiếp theo hoặc đánh dấu từ chối kèm lý do,  
  > **Để** cập nhật đúng tiến trình phỏng vấn và giữ tính toàn vẹn dữ liệu.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Chuyển tiến một bước (Happy Path)**
    - **Given** ứng viên đang ở stage `applied` và đã có kết quả sơ loại đạt,
    - **When** Recruiter thực hiện lệnh Advance (`POST /api/recruitment/applications/{id}/advance`),
    - **Then** ứng viên được chuyển sang stage `screening`, hệ thống ghi nhận thời điểm chuyển, người thực hiện và tăng trường `version` để chống ghi đè đồng thời.
  - **Kịch bản 2: Chặn nhảy cóc giai đoạn bất hợp lệ**
    - **Given** ứng viên đang ở stage `applied`,
    - **When** có yêu cầu chuyển thẳng sang `offer` mà chưa qua các vòng phỏng vấn,
    - **Then** hệ thống từ chối thao tác, trả về mã lỗi `400 Bad Request` kèm thông báo vi phạm luồng chuyển trạng thái hợp lệ.
  - **Kịch bản 3: Từ chối hồ sơ ứng viên (Reject with Reason)**
    - **Given** ứng viên không đạt tiêu chuẩn ở bất kỳ vòng nào,
    - **When** Recruiter chọn "Từ chối (Reject)" và chọn lý do (ví dụ: `Failed Technical`, `Salary Expectation Mismatch`),
    - **Then** trạng thái ứng viên chuyển sang `rejected`, ghi nhận audit log và kích hoạt mẫu email cảm ơn tự động.

---

### REC-04: Điều phối Lịch Phỏng vấn

#### [REC-04.1] Xếp Lịch Phỏng vấn Tránh Trùng Lịch (Schedule Interview & Conflict Detection)
- **Mô tả**:
  > **Là một** Recruiter,  
  > **Tôi muốn** xếp lịch phỏng vấn giữa ứng viên và hội đồng phỏng vấn (Interviewer),  
  > **Để** sắp xếp thời gian làm việc chuẩn xác, tự động gửi lời mời lịch mà không bị trùng phòng họp hoặc trùng giờ người phỏng vấn.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Đặt lịch thành công khi không có xung đột**
    - **Given** người phỏng vấn và ứng viên còn trống lịch trong khung giờ dự kiến,
    - **When** Recruiter chọn ngày giờ bắt đầu, kết thúc, hình thức (Online meeting link hoặc Offline phòng họp),
    - **Then** hệ thống lưu bản ghi vào bảng `interviews` với trạng thái `scheduled`, đồng thời gửi email kèm file lịch `.ics` cho các bên.
  - **Kịch bản 2: Phát hiện xung đột lịch**
    - **Given** người phỏng vấn A đã có lịch phỏng vấn khác từ 14:00 - 15:00,
    - **When** Recruiter cố gắng đặt thêm lịch cho Interviewer A vào lúc 14:30 cùng ngày,
    - **Then** hệ thống cảnh báo xung đột: *"Người phỏng vấn đã có lịch trùng: 14:00 - 15:00"*, không cho phép lưu nếu chưa chọn người khác hoặc đổi giờ.

---

### REC-05: Đánh giá Ứng viên qua Scorecard

#### [REC-05.1] Chấm điểm Phỏng vấn Tiêu chuẩn Hóa (Submit Interview Scorecard)
- **Mô tả**:
  > **Là một** Interviewer (Người phỏng vấn),  
  > **Tôi muốn** chấm điểm ứng viên theo bộ tiêu chí định lượng (1 - 5 điểm) kèm nhận xét chi tiết sau buổi phỏng vấn,  
  > **Để** đưa ra khuyến nghị tuyển dụng minh bạch, khách quan và có căn cứ dữ liệu.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Nộp đánh giá hợp lệ**
    - **Given** Interviewer được gán vào cuộc phỏng vấn đã hoàn tất,
    - **When** Interviewer nhập điểm (thang 1 - 5) cho các tiêu chí: Chuyên môn, Kỹ năng mềm, Giải quyết vấn đề, Văn hóa và chọn Khuyến nghị (`Strong Hire`, `Hire`, `Hold`, `No Hire`),
    - **Then** hệ thống lưu kết quả vào bảng `evaluations`, tự động tính điểm trung bình trọng số `overall_score` và khóa bản ghi để chống sửa đổi tùy tiện.
  - **Kịch bản 2: Bảo mật đánh giá độc lập (Blind Evaluation)**
    - **Given** một buổi phỏng vấn có 2 người phỏng vấn cùng tham gia,
    - **When** Interviewer 1 chưa hoàn thành nộp Scorecard của mình,
    - **Then** hệ thống ẩn toàn bộ điểm số và nhận xét của Interviewer 2 nhằm đảm bảo tính độc lập, không bị thiên vị.

---

### REC-06: Đề nghị Tuyển dụng & Bàn giao Onboarding

#### [REC-06.1] Lập & Phê duyệt Thư mời Nhận việc (Offer Management)
- **Mô tả**:
  > **Là một** Recruiter,  
  > **Tôi muốn** khởi tạo thư mời nhận việc (Offer Letter) kèm mức lương, phụ cấp, ngày nhận việc và gửi HR Manager duyệt trước khi gửi ứng viên,  
  > **Để** đảm bảo chế độ đãi ngộ tuân thủ đúng khung lương và chính sách của công ty.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo và duyệt Offer thành công**
    - **Given** ứng viên đã vượt qua vòng phỏng vấn cuối,
    - **When** Recruiter soạn thảo Offer ở trạng thái `draft` và gửi duyệt, HR Manager xác nhận "Phê duyệt",
    - **Then** Offer chuyển sang trạng thái `approved` (hoặc `sent`), hệ thống tự động sinh link xác nhận gửi tới email của ứng viên kèm hạn phản hồi.
  - **Kịch bản 2: Tự động hết hạn khi quá ngày phản hồi**
    - **Given** Offer có `expiration_date` là ngày 20/09,
    - **When** hết ngày 20/09 mà ứng viên chưa phản hồi,
    - **Then** hệ thống tự động chuyển trạng thái Offer sang `expired`, không cho phép ứng viên bấm chấp thuận nếu không được HR gia hạn.

---

#### [REC-06.2] Tự động Chuyển đổi Ứng viên trúng tuyển sang Hồ sơ Nhân viên (Handoff to Employee)
- **Mô tả**:
  > **Là một** HR Officer,  
  > **Tôi muốn** khi ứng viên bấm Chấp thuận (Accepted) Offer, hệ thống tự động khởi tạo hồ sơ nhân viên mới và bảng checklist tiếp nhận,  
  > **Để** tiết kiệm thời gian nhập liệu thủ công và không bao giờ bị sót công việc tiếp nhận.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Chuyển đổi tự động Idempotent (Happy Path)**
    - **Given** ứng viên xác nhận đồng ý Offer trực tuyến,
    - **When** hệ thống ghi nhận trạng thái `accepted`,
    - **Then** một giao dịch an toàn (database transaction) tự động:
      1. Tạo bản ghi mới trong bảng `employees` với mã nhân viên sinh tự động (ví dụ: `EMP-00128`),
      2. Chuyển thông tin từ `candidates` sang `employees`,
      3. Khởi tạo hợp đồng thử việc nháp trong bảng `contracts`,
      4. Tạo bộ checklist công việc tiếp nhận trong `onboarding_tasks`.
  - **Kịch bản 2: Chống tạo trùng lặp khi retry (Idempotency Check)**
    - **Given** ứng viên đã được chuyển đổi thành nhân viên thành công,
    - **When** có yêu cầu xử lý lại sự kiện chấp nhận Offer (do mạng lag hoặc retry API),
    - **Then** hệ thống nhận diện hồ sơ đã tồn tại, trả về kết quả hiện tại và không tạo thêm bản ghi nhân viên hay checklist trùng lặp.

---

## 3. Phân Hệ Core HR — Hồ Sơ & Vòng Đời Nhân Sự

### EMP-01: Danh bạ & Hồ sơ Định danh Nhân viên

#### [EMP-01.1] Tìm kiếm & Lọc Danh bạ Nhân viên Nhanh (Employee Directory Search & Filter)
- **Mô tả**:
  > **Là một** HR Officer hoặc Quản lý,  
  > **Tôi muốn** tìm kiếm nhân viên theo Tên, Mã nhân viên, Email, Phòng ban, Chức vụ và Trạng thái làm việc,  
  > **Để** tra cứu nhanh chóng thông tin nhân sự trong công ty.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tìm kiếm toàn văn (Full-Text Search)**
    - **Given** cơ sở dữ liệu có hàng nghìn nhân viên,
    - **When** người dùng gõ từ khóa "Nguyễn Văn" hoặc "EMP-0012",
    - **Then** hệ thống trả về kết quả trong vòng dưới 500ms, hiển thị ảnh đại diện, họ tên, chức danh, phòng ban và email công vụ.
  - **Kịch bản 2: Phân quyền xem thông tin nhạy cảm**
    - **Given** một nhân viên thông thường xem danh bạ đồng nghiệp,
    - **When** mở chi tiết hồ sơ của đồng nghiệp,
    - **Then** hệ thống chỉ hiển thị thông tin công việc công khai (Phòng ban, Chức danh, Email công vụ), tuyệt đối ẩn số CCCD, Lương, Tài khoản ngân hàng và Địa chỉ nhà.

---

#### [EMP-01.2] Cập nhật Hồ sơ Cá nhân có Kiểm soát (Employee Self-Service Profile Update)
- **Mô tả**:
  > **Là một** Nhân viên (Employee),  
  > **Tôi muốn** tự cập nhật thông tin cá nhân của mình (Số điện thoại, Địa chỉ tạm trú, Thông tin liên hệ khẩn cấp),  
  > **Để** đảm bảo thông tin liên lạc luôn chính xác mà không làm phiền phòng Nhân sự.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Cập nhật các trường được phép**
    - **Given** nhân viên đăng nhập vào trang hồ sơ cá nhân của mình,
    - **When** chỉnh sửa số điện thoại di động hoặc địa chỉ tạm trú và bấm Lưu,
    - **Then** hệ thống kiểm tra định dạng hợp lệ, cập nhật vào bảng `employees` và ghi nhận nhật ký cập nhật (Audit Log).
  - **Kịch bản 2: Chặn tự ý sửa đổi trường thông tin công việc cốt lõi**
    - **Given** nhân viên đang ở màn hình thông tin cá nhân,
    - **When** xem các trường: Mức lương, Phòng ban, Chức danh, Mã nhân viên, Ngày bắt đầu,
    - **Then** các trường này ở chế độ chỉ đọc (Read-only); mọi nỗ lực can thiệp gửi request sửa đổi qua API đều bị từ chối với mã `403 Forbidden`.

---

### EMP-02: Cơ cấu Tổ chức & Sơ đồ Phòng ban

#### [EMP-02.1] Xem Sơ đồ Cơ cấu Tổ chức Cây Phân cấp (Organizational Chart View)
- **Mô tả**:
  > **Là một** Nhân viên hoặc Quản lý,  
  > **Tôi muốn** xem sơ đồ hình cây của toàn bộ công ty từ Ban Giám Đốc xuống các Khối, Phòng ban và Đội nhóm,  
  > **Để** hiểu rõ cấu trúc tổ chức và mối quan hệ báo cáo công việc.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Hiển thị sơ đồ cây chuẩn xác**
    - **Given** hệ thống đã thiết lập các mối quan hệ `parent_department_id` trong bảng `departments`,
    - **When** người dùng mở màn hình Sơ đồ tổ chức,
    - **Then** hệ thống hiển thị cây phân cấp trực quan, mỗi nút thể hiện tên phòng ban, trưởng bộ phận, và tổng số lượng nhân sự trực thuộc (Headcount).
  - **Kịch bản 2: Bảo đảm toàn vẹn khi quản lý phòng ban**
    - **Given** một phòng ban đang có nhân viên trực thuộc hoặc tin tuyển dụng đang mở,
    - **When** người quản trị cố gắng thực hiện hành động xóa phòng ban này,
    - **Then** hệ thống từ chối xóa và hiển thị thông báo yêu cầu điều chuyển toàn bộ nhân viên và đóng tin tuyển dụng trước.

---

### EMP-03: Quy trình Tiếp nhận Nhân viên Mới (Onboarding)

#### [EMP-03.1] Theo dõi & Phân công Nhiệm vụ Tiếp nhận (Onboarding Task Checklist)
- **Mô tả**:
  > **Là một** HR Officer,  
  > **Tôi muốn** theo dõi tiến độ hoàn thành các nhiệm vụ tiếp nhận nhân viên mới (cấp laptop, tạo email, chuẩn bị bàn ghế, ký hợp đồng, đào tạo hội nhập),  
  > **Để** đảm bảo ngày làm việc đầu tiên của nhân viên mới diễn ra chỉn chu, chuyên nghiệp.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tự động phân chia đầu việc cho các phòng ban liên quan**
    - **Given** một hồ sơ nhân viên mới được tạo từ Offer trúng tuyển,
    - **When** hệ thống kích hoạt quy trình Onboarding,
    - **Then** các nhiệm vụ tương ứng được tạo trong `onboarding_tasks` và gán đúng người phụ trách (IT: tạo email & cấp máy; Hành chính: cấp thẻ ra vào; HR: chuẩn bị hợp đồng).
  - **Kịch bản 2: Cảnh báo nhiệm vụ tiếp nhận trễ hạn**
    - **Given** nhiệm vụ "Chuẩn bị Laptop" có hạn hoàn thành trước ngày nhận việc 1 ngày nhưng trạng thái vẫn là `pending`,
    - **When** đến hạn quét tự động của hệ thống,
    - **Then** hiển thị cờ cảnh báo đỏ trên Onboarding Dashboard và gửi email nhắc nhở IT phụ trách.

---

### EMP-04: Biến động Nhân sự & Quản lý Sự kiện Công tác

#### [EMP-04.1] Khởi tạo & Phê duyệt Biến động Công tác (Internal Mobility & Promotion)
- **Mô tả**:
  > **Là một** HR Manager,  
  > **Tôi muốn** tạo và phê duyệt quyết định biến động nhân sự (Thăng chức, Điều chuyển phòng ban, Tăng lương, Nghỉ việc) có ngày hiệu lực rõ ràng,  
  > **Để** lưu giữ lịch sử công tác minh bạch và tự động cập nhật chức vụ/phòng ban mới đúng ngày chỉ định.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo quyết định điều chuyển phòng ban**
    - **Given** nhân viên A đang thuộc phòng Kỹ thuật,
    - **When** HR lập đề xuất chuyển sang phòng Sản phẩm từ ngày 01/10/2026 và được cấp thẩm quyền duyệt,
    - **Then** một bản ghi được tạo trong bảng `employee_events` với trạng thái `approved`, lưu rõ `old_department_id`, `new_department_id` và `effective_date`.
  - **Kịch bản 2: Tự động áp dụng vào đúng ngày hiệu lực**
    - **Given** quyết định điều chuyển đã được duyệt với ngày hiệu lực là hôm nay,
    - **When** tiến trình công việc nền (Background Worker) chạy vào đầu ngày,
    - **Then** bản ghi của nhân viên trong bảng `employees` được tự động cập nhật sang phòng ban mới mà không cần thao tác tay.
  - **Kịch bản 3: Không xóa sửa lịch sử biến động**
    - **Given** một sự kiện biến động đã có hiệu lực trong quá khứ,
    - **When** người dùng cố gắng chỉnh sửa hoặc xóa sự kiện,
    - **Then** hệ thống chặn hành động; mọi thay đổi phải thực hiện bằng một sự kiện điều chỉnh bù (Compensating Event) mới.

---

### EMP-05: Quản lý Hồ sơ Tài liệu Điện tử An toàn

#### [EMP-05.1] Tải lên & Phân quyền Truy cập Tài liệu Nhân sự (Secure Document Storage)
- **Mô tả**:
  > **Là một** HR Officer,  
  > **Tôi muốn** lưu trữ an toàn các bản scan tài liệu nhân sự (CCCD, Bằng đại học, Chứng chỉ, Cam kết bảo mật NDA),  
  > **Để** số hóa 100% hồ sơ giấy tờ và truy xuất nhanh chóng khi thanh kiểm tra.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Lưu trữ tài liệu riêng tư (Private Storage)**
    - **Given** HR tải lên bản scan Bằng tốt nghiệp của nhân viên,
    - **When** tệp được lưu vào bảng `employee_documents`,
    - **Then** tệp được đặt ở chế độ private; đường link tải xuống là Signed URL có thời hạn (ví dụ: hết hạn sau 15 phút), không sử dụng đường dẫn công khai cố định.
  - **Kịch bản 2: Kiểm soát quyền truy cập tài liệu nhạy cảm**
    - **Given** tài liệu thuộc loại `Contract` hoặc `Disciplinary Record` (Hồ sơ kỷ luật),
    - **When** một người dùng không có vai trò `ROLE_HR_OFFICER` hoặc `ROLE_HR_MGR` cố gắng truy cập link tài liệu,
    - **Then** hệ thống trả về lỗi `403 Forbidden` và ghi lại nhật ký truy cập trái phép.

---

### EMP-06: Đánh giá & Xác nhận Hết Thử việc

#### [EMP-06.1] Đánh giá kết quả thử việc (Submit Probation Review)
- **Mô tả**:
  > **Là một** Line Manager (Quản lý trực tiếp),
  > **Tôi muốn** nhập đánh giá kết quả thử việc của nhân viên kèm điểm mạnh, điểm cần cải thiện và đề xuất kết quả,
  > **Để** HR có căn cứ ra quyết định ký hợp đồng chính thức, gia hạn thử việc hoặc chấm dứt trước khi hợp đồng thử việc hết hạn.
- **Tiền điều kiện**: Nhân viên có `status = 'probation'`; tồn tại một `probation_reviews` ở trạng thái `pending` hoặc `in_review`; người dùng là `reviewer_user_id` của phiếu đó.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Nhập đánh giá thành công**
    - **Given** phiếu đánh giá đang ở trạng thái `pending` và người dùng là người đánh giá được gán,
    - **When** Line Manager nhập `overall_score` trong khoảng 0–5, điểm mạnh, điểm cần cải thiện và chọn đề xuất kết quả,
    - **Then** phiếu chuyển `in_review`, hệ thống lưu vết người nhập và thời điểm nhập, và gửi thông báo cho HR Manager.
  - **Kịch bản 2: Chặn người không được gán đánh giá**
    - **Given** người dùng không phải `reviewer_user_id` của phiếu và cũng không có quyền HR,
    - **When** gọi API nhập đánh giá,
    - **Then** hệ thống trả `403 Forbidden` và không ghi bất kỳ thay đổi nào.
  - **Kịch bản 3: Bắt buộc nhận xét khi đề xuất chấm dứt**
    - **Given** Line Manager chọn đề xuất kết quả là `terminated`,
    - **When** để trống trường điểm cần cải thiện,
    - **Then** hệ thống chặn lưu và yêu cầu nhập nhận xét làm căn cứ.
  - **Kịch bản 4: Cảnh báo phiếu quá hạn**
    - **Given** hôm nay đã vượt `review_due_date` mà phiếu còn `pending` hoặc `in_review`,
    - **When** HR mở dashboard vòng đời nhân sự,
    - **Then** phiếu hiển thị trong khối cảnh báo đỏ kèm số ngày quá hạn, được phân loại là rủi ro pháp lý chứ không phải trễ quy trình.
- **Ràng buộc kỹ thuật**: `probation_reviews`; `ux_probation_review_contract` đảm bảo một hợp đồng thử việc chỉ có một phiếu; `overall_score` bị chặn trong khoảng 0–5 bằng CHECK constraint.

---

#### [EMP-06.2] Ra quyết định hết thử việc và đồng bộ trạng thái nhân sự (Decide Probation Outcome)
- **Mô tả**:
  > **Là một** HR Manager,
  > **Tôi muốn** phê duyệt kết quả thử việc và để hệ thống tự sinh sự kiện nhân sự tương ứng,
  > **Để** trạng thái nhân viên và hợp đồng luôn khớp với quyết định đã phê duyệt, không phải sửa tay ở nhiều nơi.
- **Tiền điều kiện**: Phiếu ở trạng thái `in_review`; người dùng có vai trò `ROLE_HR_MGR`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Xác nhận chính thức**
    - **Given** phiếu ở `in_review` với đề xuất `confirmed`,
    - **When** HR Manager phê duyệt và nhập ngày hiệu lực,
    - **Then** phiếu chuyển `decided` với đầy đủ `outcome`, `decided_by`, `decided_at`, `effective_date`; hệ thống tạo `employee_events` với `event_type = 'probation_confirmation'` và liên kết vào `probation_reviews.employee_event_id`.
  - **Kịch bản 2: Chặn quyết định thiếu dữ liệu bắt buộc**
    - **Given** HR Manager phê duyệt nhưng không nhập ngày hiệu lực,
    - **When** gửi yêu cầu,
    - **Then** hệ thống từ chối với `422`; constraint `ck_probation_decided` đảm bảo không tồn tại phiếu `decided` mà thiếu `outcome`/`decided_by`/`decided_at`/`effective_date`.
  - **Kịch bản 3: Trạng thái nhân viên không đổi trước ngày hiệu lực**
    - **Given** quyết định `confirmed` có `effective_date` trong tương lai,
    - **When** kiểm tra hồ sơ nhân viên ngay sau khi phê duyệt,
    - **Then** `employees.status` vẫn là `probation`; chỉ khi đến `effective_date` và sự kiện được áp dụng thì trạng thái mới chuyển `active`.
  - **Kịch bản 4: Kết quả chấm dứt kích hoạt luồng thôi việc**
    - **Given** quyết định là `terminated`,
    - **When** phiếu chuyển `decided`,
    - **Then** hệ thống tạo `employee_events` với `event_type = 'termination'` và mở một `offboarding_cases` tương ứng theo `[EMP-07.1]`.
- **Ràng buộc kỹ thuật**: Sinh sự kiện và cập nhật phiếu phải nằm trong cùng một transaction kèm audit log; thao tác phê duyệt lặp lại phải idempotent — một phiếu chỉ sinh tối đa một `employee_events`.

---

### EMP-07: Thôi việc & Bàn giao

#### [EMP-07.1] Khởi tạo hồ sơ thôi việc và sinh checklist bàn giao (Create Offboarding Case)
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** tạo hồ sơ thôi việc với ngày làm việc cuối và người nhận bàn giao, rồi để hệ thống sinh sẵn danh mục việc cần làm,
  > **Để** không bỏ sót việc thu hồi tài sản, khóa tài khoản và chốt công nợ khi nhân viên rời công ty.
- **Tiền điều kiện**: Nhân viên ở trạng thái `active`, `probation` hoặc `suspended`; người dùng có vai trò `ROLE_HR_OFFICER` hoặc `ROLE_HR_MGR`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo case và sinh checklist thành công**
    - **Given** HR Officer nhập `separation_type`, `last_working_date`, người nhận bàn giao và lý do,
    - **When** HR Manager phê duyệt case,
    - **Then** case chuyển `approved` và hệ thống sinh `offboarding_tasks` từ template theo 5 nhóm `it`, `admin`, `hr`, `manager`, `finance`, mỗi task có người chịu trách nhiệm và hạn hoàn thành.
  - **Kịch bản 2: Chặn tạo case thứ hai khi đã có case đang mở**
    - **Given** nhân viên đã có một case ở trạng thái `draft`, `pending_approval`, `approved` hoặc `in_progress`,
    - **When** người dùng tạo case mới cho cùng nhân viên,
    - **Then** hệ thống trả `409 Conflict`; `ux_offboarding_open_case` đảm bảo không tồn tại hai case mở song song.
  - **Kịch bản 3: Cảnh báo không đủ thời hạn báo trước**
    - **Given** `notice_received_on` cách `last_working_date` ít hơn `contracts.notice_period_days`,
    - **When** HR Officer lưu case,
    - **Then** hệ thống hiển thị cảnh báo rõ ràng về thời hạn báo trước nhưng **vẫn cho lưu**, vì quyết định thuộc HR Manager; cảnh báo được ghi vào audit log.
  - **Kịch bản 4: Chặn bàn giao cho chính người thôi việc**
    - **Given** HR Officer chọn `handover_to_employee_id` là chính nhân viên đang thôi việc,
    - **When** lưu case,
    - **Then** hệ thống từ chối; `ck_offboarding_handover_not_self` chặn ở tầng database.
  - **Kịch bản 5: Sinh lại checklist không tạo task trùng**
    - **Given** case được phê duyệt lại sau khi bị hủy rồi khôi phục,
    - **When** tiến trình sinh checklist chạy lần thứ hai,
    - **Then** không có task nào bị nhân đôi; `ux_offboarding_task_template` đảm bảo mỗi `template_key` chỉ tồn tại một lần trong một case.
- **Ràng buộc kỹ thuật**: `offboarding_cases`, `offboarding_tasks`, `employee_events`; tạo case và sinh task nằm trong một transaction.

---

#### [EMP-07.2] Hoàn tất bàn giao và đóng hồ sơ thôi việc (Complete Offboarding)
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** chỉ đóng được hồ sơ thôi việc khi mọi việc chặn đã hoàn thành và công nợ đã chốt,
  > **Để** công ty không mất tài sản, không để lại tài khoản còn hoạt động và nhân viên nhận đủ quyền lợi.
- **Tiền điều kiện**: Case ở trạng thái `in_progress`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Chặn đóng case khi còn việc chặn**
    - **Given** còn ít nhất một `offboarding_tasks` có `blocks_last_working_day = true` và `status <> 'completed'`,
    - **When** HR Officer bấm đóng case,
    - **Then** hệ thống từ chối và liệt kê cụ thể các task đang chặn.
  - **Kịch bản 2: Chặn đóng case khi chưa chốt công nợ**
    - **Given** toàn bộ task chặn đã hoàn thành nhưng `final_settlement_status` vẫn là `pending` hoặc `calculated`,
    - **When** HR Officer bấm đóng case,
    - **Then** hệ thống từ chối và yêu cầu chuyển trạng thái chốt công nợ sang `paid` hoặc `waived`.
  - **Kịch bản 3: Đóng case thành công và vô hiệu hóa tài khoản**
    - **Given** toàn bộ điều kiện đã thỏa,
    - **When** HR Officer đóng case,
    - **Then** case chuyển `completed` với `completed_at`; đến `last_working_date`, `employees.status` chuyển `terminated` và `users.status` chuyển `disabled`.
  - **Kịch bản 4: Khóa tài khoản không được sớm hơn ngày làm việc cuối**
    - **Given** case đã `completed` nhưng `last_working_date` còn ở tương lai,
    - **When** nhân viên đăng nhập trong khoảng thời gian còn lại,
    - **Then** nhân viên vẫn truy cập được để hoàn thành bàn giao; tài khoản chỉ bị vô hiệu hóa đúng ngày làm việc cuối.
  - **Kịch bản 5: Hủy đơn nghỉ phép sau ngày làm việc cuối**
    - **Given** nhân viên có đơn nghỉ `approved` với ngày bắt đầu sau `last_working_date`,
    - **When** HR Officer đóng case,
    - **Then** hệ thống hủy các đơn đó, trả lại quỹ phép tương ứng và ghi audit log; nếu không hủy được thì chặn việc đóng case.
  - **Kịch bản 6: Hồ sơ vẫn được lưu trữ sau khi thôi việc**
    - **Given** nhân viên đã `terminated`,
    - **When** HR Officer tra cứu hồ sơ tài liệu của nhân viên đó,
    - **Then** tài liệu vẫn truy cập được tới `retention_until`; nhân viên không còn xuất hiện trong danh bạ đang làm việc.
- **Ràng buộc kỹ thuật**: `ck_offboarding_completed` đảm bảo `completed` luôn có `completed_at`; bỏ qua task chặn cần quyền `ROLE_HR_MGR` và bắt buộc ghi lý do vào audit log.

---

## 4. Core HR — Nhánh Quản Lý Hợp Đồng Lao Động (Contract Management)

> Theo bản đồ chức năng, nhóm story này là **nhánh con của Core HR**, không phải một phân hệ độc lập. Mã `CON-*` được giữ riêng để truy vết tới `contracts` và `contract_addenda`.

### CON-01: Soạn thảo, Ký kết & Vòng đời Hợp đồng

#### [CON-01.1] Soạn thảo & Tạo mới Hợp đồng Lao động (Contract Drafting)
- **Mô tả**:
  > **Là một** HR Officer (C&B phụ trách hợp đồng),  
  > **Tôi muốn** tạo mới hợp đồng lao động theo mẫu chuẩn quy định của Bộ luật Lao động Việt Nam (Thử việc, Xác định thời hạn, Không xác định thời hạn),  
  > **Để** thiết lập quan hệ lao động pháp lý chính xác với người lao động.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Soạn hợp đồng xác định thời hạn hợp lệ**
    - **Given** nhân viên đang làm việc và chưa có hợp đồng chính thức nào còn hiệu lực,
    - **When** HR chọn loại hợp đồng `fixed_term`, nhập Số hợp đồng (`contract_number`), Ngày bắt đầu, Ngày kết thúc (`start_date < end_date`), Mức lương,
    - **Then** hệ thống lưu hợp đồng vào bảng `contracts` ở trạng thái `draft`, kiểm tra không có trùng số hợp đồng trong toàn hệ thống.
  - **Kịch bản 2: Bắt buộc ngày kết thúc đối với hợp đồng có thời hạn**
    - **Given** HR tạo hợp đồng loại `probation` hoặc `fixed_term`,
    - **When** để trống trường `end_date` hoặc nhập `end_date <= start_date`,
    - **Then** hệ thống báo lỗi: *"Hợp đồng có thời hạn bắt buộc phải có ngày kết thúc lớn hơn ngày bắt đầu"*.
  - **Kịch bản 3: Chặn một nhân viên có 2 hợp đồng chính cùng hiệu lực**
    - **Given** nhân viên B đang có một hợp đồng trạng thái `active`,
    - **When** HR cố gắng kích hoạt một hợp đồng khác có khoảng thời gian hiệu lực giao nhau,
    - **Then** hệ thống đưa ra cảnh báo trùng lặp hiệu lực và yêu cầu kết thúc hợp đồng cũ trước.

---

#### [CON-01.2] Ký kết & Kích hoạt Hiệu lực Hợp đồng (Contract Execution & Activation)
- **Mô tả**:
  > **Là một** HR Officer hoặc HR Manager,  
  > **Tôi muốn** tải lên tệp hợp đồng đã có đầy đủ chữ ký của hai bên hoặc cập nhật trạng thái đã ký điện tử,  
  > **Để** chính thức đưa hợp đồng vào trạng thái có hiệu lực thi hành (`active`).
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Kích hoạt khi có file ký hợp lệ**
    - **Given** hợp đồng ở trạng thái `approved`,
    - **When** tải lên bản scan hợp đồng có chữ ký hoặc hoàn tất luồng ký số,
    - **Then** trạng thái hợp đồng chuyển thành `active`, lưu vết người kích hoạt và thời gian kích hoạt.
  - **Kịch bản 2: Nhân viên xem hợp đồng của chính mình**
    - **Given** nhân viên đăng nhập vào hệ thống,
    - **When** vào mục "Hợp đồng của tôi",
    - **Then** nhân viên chỉ nhìn thấy danh sách các hợp đồng của chính mình, có thể xem và tải bản scan hợp đồng đã ký.

---

### CON-02: Giám sát Hạn Hợp đồng & Cảnh báo Tự động

#### [CON-02.1] Cảnh báo Hợp đồng Sắp Hết hạn Đa tầng (Automated Expiration Alert Engine)
- **Mô tả**:
  > **Là một** HR Officer phụ trách mảng C&B,  
  > **Tôi muốn** hệ thống tự động quét và gửi cảnh báo các hợp đồng sắp đến ngày đáo hạn (trước 7 ngày, 15 ngày cho thử việc; trước 30 ngày, 45 ngày cho hợp đồng chính thức),  
  > **Để** kịp thời tiến hành thủ tục đánh giá hết thử việc, tái ký hợp đồng mới hoặc lập thông báo chấm dứt đúng thời hạn luật định.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Quét và hiển thị cảnh báo trên Dashboard**
    - **Given** có 3 hợp đồng thử việc sẽ hết hạn trong vòng 7 ngày tới,
    - **When** HR mở Dashboard quản trị Hợp đồng,
    - **Then** hệ thống hiển thị danh sách 3 hợp đồng này trong khối "Cảnh báo Hết hạn (Amber/Red Alert)" kèm số ngày còn lại.
  - **Kịch bản 2: Tự động gửi thông báo qua Email/Chuông thông báo**
    - **Given** tiến trình quét định kỳ hàng ngày lúc 06:00 sáng,
    - **When** phát hiện hợp đồng chạm mốc mốc cảnh báo (30 ngày trước ngày hết hạn),
    - **Then** hệ thống gửi email tự động tới HR phụ trách và Quản lý trực tiếp của nhân viên đó kèm theo mẫu phiếu đánh giá tái ký.
  - **Kịch bản 3: Không gửi cảnh báo trùng lặp trong cùng một ngày**
    - **Given** thông báo cảnh báo mốc 30 ngày đã được gửi vào lúc sáng,
    - **When** tiến trình quét chạy lại vào buổi chiều,
    - **Then** hệ thống kiểm tra bảng nhật ký thông báo và không gửi lặp lại email cho cùng một mốc cảnh báo.

---

### CON-03: Quản lý Phụ lục Hợp đồng Lao động

#### [CON-03.1] Tạo Phụ lục Điều chỉnh Điều khoản Hợp đồng (Contract Addendum Management)
- **Mô tả**:
  > **Là một** HR Officer,  
  > **Tôi muốn** tạo phụ lục hợp đồng khi có sự thay đổi về mức lương, phụ cấp, chức danh hoặc địa điểm làm việc mà không làm thay đổi tính toàn vẹn của hợp đồng gốc,  
  > **Để** lưu vết biến động điều khoản pháp lý và bảo vệ tính pháp lý của hồ sơ lao động.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo phụ lục điều chỉnh mức lương thành công**
    - **Given** hợp đồng gốc đang ở trạng thái `active`,
    - **When** HR chọn tạo phụ lục, nhập Số phụ lục, Ngày hiệu lực, Loại thay đổi là `Salary_Adjustment`, Mức lương mới,
    - **Then** hệ thống tạo bản ghi trong bảng `contract_addenda` liên kết với `contract_id` tương ứng, lưu lại giá trị trước và sau thay đổi.
  - **Kịch bản 2: Tính bất biến của hợp đồng gốc**
    - **Given** phụ lục đã được duyệt và có hiệu lực,
    - **When** kiểm tra hợp đồng gốc,
    - **Then** nội dung điều khoản gốc ban đầu vẫn giữ nguyên vẹn; hệ thống chỉ ghi nhận mối quan hệ mở rộng tới phụ lục này.
  - **Kịch bản 3: Đồng bộ biến động nhân sự khi phụ lục có hiệu lực**
    - **Given** phụ lục điều chỉnh chức danh công việc được ký kết thành công,
    - **When** phụ lục chuyển sang trạng thái `effective`,
    - **Then** hệ thống tự động sinh một bản ghi `employee_event` tương ứng để đồng bộ chức danh mới của nhân viên.

---

## 5. Phân Hệ Chấm Công & Nghỉ Phép (Attendance & Leave)

> [!IMPORTANT]
> **Trạng thái nhóm story: `Blocked — policy pending`.**
> Các story dưới đây đã đủ chi tiết để estimate và viết test, nhưng **mọi ngưỡng, hệ số và công thức là giá trị đề xuất**. Mã `[OD-x.y]` trỏ tới mục tương ứng trong [Open Decisions — Attendance & Leave](open_decisions_attendance_leave.md). Một story chỉ được đưa vào sprint khi tất cả mã `[OD-*]` mà nó tham chiếu đã được chốt và có người phê duyệt.
> Canonical schema chặn kỹ thuật: không thể ghi bảng công theo `attendance_policies` còn `draft`, không thể dùng `leave_types` có `policy_status = 'draft'`.

### ATT-01: Ca làm việc, Phân ca & Lịch lễ

#### [ATT-01.1] Định nghĩa ca làm việc (Define Work Shift)
- **Phụ thuộc quyết định**: `[OD-1.1]` múi giờ chuẩn, `[OD-1.3]` số phút công chuẩn, `[OD-1.8]` khung giờ và hệ số làm đêm.
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** định nghĩa các ca làm việc với giờ bắt đầu, giờ kết thúc, thời gian nghỉ giữa ca và số phút công chuẩn,
  > **Để** hệ thống có mốc chuẩn đối chiếu khi tính đi muộn, về sớm và giờ công thực tế.
- **Tiền điều kiện**: Người dùng có vai trò `ROLE_HR_OFFICER` hoặc `ROLE_HR_MGR`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo ca hành chính thành công**
    - **Given** HR Officer nhập mã ca `SHIFT_ADMIN`, giờ 08:30–17:30, múi giờ `Asia/Ho_Chi_Minh`, nghỉ giữa ca 60 phút, số phút công chuẩn 480,
    - **When** bấm lưu,
    - **Then** hệ thống tạo bản ghi trong `work_shifts` với `crosses_midnight = false` và trả về `ETag` cho lần sửa sau.
  - **Kịch bản 2: Tự suy ra ca qua đêm, không cho nhập mâu thuẫn**
    - **Given** HR Officer nhập ca 22:00–06:00,
    - **When** bấm lưu,
    - **Then** hệ thống tự đặt `crosses_midnight = true`; nếu client gửi `crosses_midnight = false` thì bị từ chối bởi `ck_shift_crosses_midnight`.
  - **Kịch bản 3: Chặn giá trị ngoài miền hợp lệ**
    - **Given** HR Officer nhập `standard_work_minutes = 0` hoặc `break_minutes = 2000` hoặc `work_coefficient = 9`,
    - **When** bấm lưu,
    - **Then** hệ thống trả `422` và nêu rõ trường vi phạm; không bản ghi nào được tạo.
  - **Kịch bản 4: Không cho xóa ca đang được sử dụng**
    - **Given** ca đang được tham chiếu bởi `work_schedule_assignments` hoặc `attendance_daily_records`,
    - **When** HR Officer cố xóa ca,
    - **Then** hệ thống từ chối và đề xuất chuyển `active = false` thay vì xóa.
  - **Kịch bản 5: Sửa đồng thời không ghi đè lẫn nhau**
    - **Given** hai HR Officer cùng mở một ca để sửa,
    - **When** người thứ hai gửi `PUT` với `If-Match` là `ETag` đã cũ,
    - **Then** hệ thống trả `409 Conflict` và không ghi thay đổi.
- **Ràng buộc kỹ thuật**: `work_shifts`; `code` là `UNIQUE`; `version` là nguồn `ETag`; API `GET|POST /api/v1/attendance/shifts`, `GET|PUT /api/v1/attendance/shifts/{shiftId}`.

---

#### [ATT-01.2] Phân ca theo tuần (Assign Weekly Work Schedule)
- **Phụ thuộc quyết định**: `[OD-1.2]` ca qua đêm thuộc ngày nào, `[OD-1.4]` ngày nghỉ hằng tuần.
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** phân ca cho nhân viên theo tuần và cập nhật nhiều phân ca trong một lần,
  > **Để** lịch làm việc của cả đội được thiết lập nhanh và luôn là một lịch nhất quán.
- **Tiền điều kiện**: Đã có ít nhất một ca `active`; nhân viên đang ở trạng thái làm việc.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Phân ca cả tuần thành công**
    - **Given** HR Officer chọn tuần bắt đầu và gán ca cho từng nhân viên từng ngày,
    - **When** gửi `PUT /api/v1/attendance/schedules` kèm `Idempotency-Key`,
    - **Then** toàn bộ phân ca được ghi **trong một transaction**; nếu một phần tử lỗi thì không phần tử nào được ghi.
  - **Kịch bản 2: Chặn hai ca trong cùng một ngày**
    - **Given** nhân viên đã có ca cho ngày 2026-10-05,
    - **When** HR Officer gán thêm một ca khác cho cùng ngày đó,
    - **Then** hệ thống trả `409 Conflict`; `ux_schedule_employee_date` chặn ở tầng database.
  - **Kịch bản 3: Ngày không phân ca là ngày nghỉ, không suy diễn ca mặc định**
    - **Given** nhân viên không có bản ghi phân ca cho Chủ Nhật,
    - **When** hệ thống tính bảng công cho ngày đó,
    - **Then** ngày đó có `status = 'day_off'` với `scheduled_minutes = 0`; hệ thống **không** tự gán ca hành chính.
  - **Kịch bản 4: Chặn phân ca vào kỳ công đã khóa**
    - **Given** kỳ công chứa ngày cần phân ca đang ở `status = 'locked'`,
    - **When** HR Officer gửi phân ca cho ngày đó,
    - **Then** hệ thống trả `409` với mã lỗi nghiệp vụ nêu rõ kỳ công đã khóa.
  - **Kịch bản 5: Gửi lại do timeout không tạo dữ liệu trùng**
    - **Given** request phân ca bị timeout ở client nhưng đã thành công ở server,
    - **When** client gửi lại cùng `Idempotency-Key`,
    - **Then** hệ thống trả về chính kết quả lần đầu, không tạo bản ghi thứ hai.
- **Ràng buộc kỹ thuật**: `work_schedule_assignments`; `assigned_by` lưu người thực hiện; API `GET|PUT /api/v1/attendance/schedules`.

---

#### [ATT-01.3] Thiết lập lịch nghỉ lễ (Maintain Holiday Calendar)
- **Phụ thuộc quyết định**: `[OD-2.1]` danh sách ngày lễ, `[OD-2.2]` người duyệt và thời hạn nhập, `[OD-2.3]` hệ số làm việc ngày lễ, `[OD-2.4]` nghỉ bù.
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** nhập lịch nghỉ lễ của năm và trình HR Manager phê duyệt,
  > **Để** hệ thống không trừ quỹ phép và không tính vắng vào những ngày toàn công ty được nghỉ.
- **Tiền điều kiện**: Người dùng có vai trò `ROLE_HR_OFFICER`; HR Manager phê duyệt.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Nhập lịch lễ năm sau thành công**
    - **Given** HR Officer nhập danh sách ngày lễ với `calendar_code = 'VN'`, tên và cờ hưởng lương,
    - **When** HR Manager phê duyệt,
    - **Then** các bản ghi được lưu vào `holidays` và có hiệu lực cho mọi phép tính công từ thời điểm đó.
  - **Kịch bản 2: Chặn ngày lễ trùng trong cùng một lịch**
    - **Given** ngày 2027-01-01 đã tồn tại trong lịch `VN`,
    - **When** HR Officer nhập lại cùng ngày đó,
    - **Then** hệ thống trả `409`; `ux_holidays_calendar_date` chặn ở tầng database.
  - **Kịch bản 3: Ngày lễ không bị tính là nghỉ không phép**
    - **Given** một ngày lễ đã được duyệt và nhân viên không chấm công ngày đó,
    - **When** hệ thống tính bảng công,
    - **Then** ngày đó có `status = 'holiday'` và trỏ `holiday_id`, **không** phải `absent`.
  - **Kịch bản 4: Cảnh báo khi năm tới chưa có lịch lễ**
    - **Given** đã qua 31/12 mà năm kế tiếp chưa có bản ghi `holidays` nào,
    - **When** HR mở dashboard chấm công,
    - **Then** hệ thống hiển thị cảnh báo yêu cầu nhập lịch lễ, vì thiếu lịch lễ sẽ làm sai toàn bộ số ngày nghỉ phép được trừ.
- **Ràng buộc kỹ thuật**: `holidays`; `work_coefficient` bị chặn trong khoảng 0–5; API `GET|POST /api/v1/attendance/holidays`, `PUT|DELETE /api/v1/attendance/holidays/{holidayId}`.

---

### ATT-02: Ghi nhận, Hiệu chỉnh Chấm công & Tăng ca

#### [ATT-02.1] Chấm công vào / ra (Record Check-in and Check-out)
- **Phụ thuộc quyết định**: `[OD-1.2]` ca qua đêm, `[OD-1.6]` ân hạn muộn/về sớm, `[OD-1.7]` ngưỡng vắng, `[OD-3.1]` phương thức được chấp nhận, `[OD-3.2]` giới hạn vị trí, `[OD-3.4]` xử lý thiếu check-out, `[OD-3.5]` nhiều lần vào/ra.
- **Mô tả**:
  > **Là một** Nhân viên,
  > **Tôi muốn** chấm công vào và ra trên ứng dụng và thấy ngay kết quả ngày công của mình,
  > **Để** giờ làm việc của tôi được ghi nhận chính xác và tôi phát hiện sớm nếu có sai sót.
- **Tiền điều kiện**: Nhân viên đã đăng nhập; đã có `attendance_policies` ở trạng thái `active`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Chấm công đúng giờ**
    - **Given** nhân viên có ca 08:30–17:30 và chấm công vào lúc 08:25,
    - **When** gửi `POST /api/v1/attendance/events` với `eventType = check_in`,
    - **Then** hệ thống ghi `attendance_events`, tính lại ngày công và trả về `status = 'on_time'` với `lateMinutes = 0`.
  - **Kịch bản 2: Ghi nhận đi muộn**
    - **Given** nhân viên có ca bắt đầu 08:30 và chấm công vào lúc 08:52,
    - **When** ghi nhận check-in,
    - **Then** ngày công có `status = 'late'` và `lateMinutes` bằng chênh lệch sau khi trừ `late_grace_minutes` của policy đang áp dụng.
  - **Kịch bản 3: Gửi trùng không tạo bản ghi thứ hai**
    - **Given** nhân viên bấm chấm công hai lần do mạng chậm, cùng một `Idempotency-Key`,
    - **When** request thứ hai đến server,
    - **Then** hệ thống trả `duplicate = true` với mã 2xx và **không** tạo bản ghi thứ hai; `ux_attendance_events_idempotency` là chốt chặn cuối.
  - **Kịch bản 4: Chỉ có check-in, không suy diễn giờ ra**
    - **Given** nhân viên chấm công vào nhưng quên chấm công ra,
    - **When** hệ thống tính bảng công cuối ngày,
    - **Then** ngày công có `status = 'incomplete'`, `worked_minutes = 0` và **không** tự điền giờ ra theo ca; hệ thống hiển thị nhắc nhở tạo đơn hiệu chỉnh.
  - **Kịch bản 5: Ca qua đêm thuộc đúng ngày công**
    - **Given** nhân viên có ca 22:00 ngày 05/10 đến 06:00 ngày 06/10,
    - **When** chấm công ra lúc 06:05 ngày 06/10,
    - **Then** cả hai sự kiện có `work_date = 2026-10-05` (ngày bắt đầu ca), và ngày công được tính trên một dòng duy nhất.
  - **Kịch bản 6: Chặn chấm công vào kỳ đã khóa**
    - **Given** kỳ công chứa thời điểm chấm công đang `locked`,
    - **When** nhân viên hoặc thiết bị gửi sự kiện,
    - **Then** hệ thống trả `409` với mã lỗi nghiệp vụ và không ghi sự kiện.
  - **Kịch bản 7: Toạ độ GPS phải đầy đủ**
    - **Given** client gửi latitude mà không gửi longitude,
    - **When** ghi nhận sự kiện,
    - **Then** hệ thống trả `422`; `ck_attendance_event_geo` đảm bảo hai trường luôn cùng có hoặc cùng không.
- **Ràng buộc kỹ thuật**: `attendance_events` là append-only; `attendance_daily_records` là dữ liệu dẫn xuất luôn ghi kèm `policy_version`; client không được tự tính `lateMinutes`/`workedMinutes`.

---

#### [ATT-02.2] Tiếp nhận dữ liệu từ thiết bị chấm công (Ingest Device Attendance Events)
- **Phụ thuộc quyết định**: `[OD-3.3]` cửa sổ nhận dữ liệu gửi bù.
- **Mô tả**:
  > **Là một** hệ thống thiết bị chấm công (máy vân tay / Face ID),
  > **Tôi muốn** đẩy sự kiện chấm công vào QLNS một cách có xác thực và an toàn khi gửi lại,
  > **Để** dữ liệu chấm công không bị mất khi thiết bị mất mạng và cũng không bị nhân đôi khi gửi bù.
- **Tiền điều kiện**: Thiết bị đã được cấp thông tin xác thực; `employeeExternalKey` map được tới một nhân viên.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Nhận sự kiện hợp lệ**
    - **Given** thiết bị gửi sự kiện với `externalEventId` chưa từng xuất hiện,
    - **When** gọi `POST /api/v1/integrations/attendance/events`,
    - **Then** hệ thống trả `202 Accepted`, ghi `attendance_events` với `source = 'device'`.
  - **Kịch bản 2: Gửi bù sau khi mất mạng không tạo bản ghi trùng**
    - **Given** thiết bị offline 8 giờ rồi gửi lại toàn bộ 200 sự kiện, trong đó 150 sự kiện đã từng gửi thành công,
    - **When** batch được đẩy lên,
    - **Then** 150 sự kiện cũ trả `duplicate = true` và bị bỏ qua; chỉ 50 sự kiện mới được ghi; `ux_attendance_events_device` trên cặp `device_id` + `external_event_id` là chốt chặn.
  - **Kịch bản 3: Chặn sự kiện thiếu định danh thiết bị**
    - **Given** payload có `source = 'device'` nhưng thiếu `deviceId` hoặc `externalEventId`,
    - **When** gọi API,
    - **Then** hệ thống trả `422`; `ck_attendance_event_device` chặn ở tầng database.
  - **Kịch bản 4: Chặn thiết bị không được xác thực**
    - **Given** request không mang thông tin xác thực thiết bị hợp lệ,
    - **When** gọi API,
    - **Then** hệ thống trả `401` và không ghi bất kỳ dữ liệu nào.
  - **Kịch bản 5: Nhân viên không map được**
    - **Given** `employeeExternalKey` không khớp nhân viên nào,
    - **When** gọi API,
    - **Then** hệ thống trả `422` và đưa sự kiện vào danh sách cần xử lý thủ công, **không** im lặng bỏ qua.
- **Ràng buộc kỹ thuật**: Endpoint dùng `deviceAuth` riêng, không dùng token người dùng; sự kiện quá cửa sổ gửi bù bị từ chối và phải đi qua luồng hiệu chỉnh `[ATT-02.3]`.

---

#### [ATT-02.3] Đề nghị và duyệt hiệu chỉnh công (Request and Decide Attendance Correction)
- **Phụ thuộc quyết định**: `[OD-4.1]` người duyệt, `[OD-4.2]` thời hạn gửi đơn, `[OD-4.3]` yêu cầu minh chứng, `[OD-4.4]` hiệu chỉnh sau khóa kỳ.
- **Mô tả**:
  > **Là một** Nhân viên,
  > **Tôi muốn** gửi đề nghị sửa giờ vào/ra của một ngày kèm lý do và được quản lý duyệt,
  > **Để** ngày công bị sai do quên chấm công hoặc lỗi thiết bị không làm tôi bị trừ lương oan.
- **Tiền điều kiện**: Ngày công tồn tại; kỳ công chứa ngày đó chưa `locked`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Gửi đơn hiệu chỉnh thành công**
    - **Given** nhân viên chọn ngày công `incomplete` và nhập giờ vào/ra đề nghị kèm lý do,
    - **When** gửi `POST /api/v1/attendance/corrections`,
    - **Then** hệ thống tạo bản ghi `pending`, lưu snapshot giá trị hiện tại vào `current_check_in_at`/`current_check_out_at` để đối chiếu, và trả `ETag`.
  - **Kịch bản 2: Bắt buộc nhập lý do**
    - **Given** nhân viên để trống lý do,
    - **When** gửi đơn,
    - **Then** hệ thống trả `422`; `reason` là `NOT NULL` ở tầng database.
  - **Kịch bản 3: Chặn đơn thứ hai cho cùng một ngày**
    - **Given** đã có một đơn `pending` cho ngày 2026-10-05,
    - **When** nhân viên gửi đơn thứ hai cho cùng ngày,
    - **Then** hệ thống trả `409`; `ux_corrections_one_pending_per_day` chặn ở tầng database.
  - **Kịch bản 4: Duyệt đơn và tái tính ngày công**
    - **Given** Line Manager duyệt đơn với `If-Match` đúng phiên bản,
    - **When** gửi `POST /api/v1/attendance/corrections/{correctionId}/approve`,
    - **Then** hệ thống ghi một `attendance_events` mới với `source = 'correction'`, tính lại ngày công, đặt `status = 'corrected'`, trỏ `correction_id` và ghi `applied_at`. **Sự kiện gốc từ thiết bị không bị xóa.**
  - **Kịch bản 5: Từ chối phải có lý do**
    - **Given** Line Manager từ chối đơn mà không nhập lý do,
    - **When** gửi request,
    - **Then** hệ thống chặn và yêu cầu nhập lý do; quyết định được lưu kèm `decided_by` và `decided_at`.
  - **Kịch bản 6: Duyệt đồng thời không ghi đè**
    - **Given** hai người duyệt cùng mở một đơn,
    - **When** người thứ hai gửi quyết định với `ETag` cũ,
    - **Then** hệ thống trả `409` và giữ nguyên quyết định đầu tiên.
  - **Kịch bản 7: Không có trạng thái "đã áp dụng nhưng chưa duyệt"**
    - **Given** một đơn ở trạng thái `rejected` hoặc `cancelled`,
    - **When** kiểm tra dữ liệu,
    - **Then** `applied_at` luôn `NULL`; `ck_correction_applied` đảm bảo chỉ đơn `approved` mới có `applied_at`.
- **Ràng buộc kỹ thuật**: `attendance_corrections`; `ck_correction_range` đảm bảo giờ ra sau giờ vào; `ck_correction_decided` đảm bảo mọi quyết định đều có người và thời điểm.

---

#### [ATT-02.4] Đăng ký và duyệt tăng ca (Request and Approve Overtime)
- **Phụ thuộc quyết định**: `[OD-5.1]` đăng ký trước hay sau, `[OD-5.2]` ngưỡng tối thiểu, `[OD-5.3]` hệ số theo loại, `[OD-5.4]` **giới hạn giờ tăng ca theo luật**, `[OD-5.5]` đổi OT thành nghỉ bù.
- **Mô tả**:
  > **Là một** Nhân viên,
  > **Tôi muốn** đăng ký tăng ca trước khi làm và được quản lý duyệt số giờ cụ thể,
  > **Để** giờ làm thêm của tôi được trả đúng hệ số và công ty kiểm soát được chi phí làm thêm.
- **Tiền điều kiện**: Nhân viên có ca được phân cho ngày đó hoặc ngày đó là ngày nghỉ/lễ; policy đang `active`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Đăng ký tăng ca ngày thường thành công**
    - **Given** nhân viên đăng ký tăng ca 18:00–20:30 của một ngày làm việc,
    - **When** gửi `POST /api/v1/attendance/overtime-requests`,
    - **Then** hệ thống tạo đơn `pending`, tự xác định `overtimeCategory = 'weekday'` và gán `workCoefficient` theo policy; client không được tự gửi hệ số.
  - **Kịch bản 2: Tự phân loại tăng ca ngày lễ**
    - **Given** ngày đăng ký là một ngày trong `holidays`,
    - **When** tạo đơn,
    - **Then** hệ thống đặt `overtimeCategory = 'holiday'` và áp hệ số của ngày lễ, không phải hệ số ngày thường.
  - **Kịch bản 3: Chặn đơn tăng ca trùng khoảng thời gian**
    - **Given** nhân viên đã có đơn `approved` từ 18:00–20:00 ngày 05/10,
    - **When** gửi đơn mới 19:00–21:00 cùng ngày,
    - **Then** hệ thống trả `409`; `ex_overtime_requests_no_overlap` chặn ở tầng database.
  - **Kịch bản 4: Duyệt ít hơn số đăng ký**
    - **Given** nhân viên đăng ký 150 phút,
    - **When** Line Manager duyệt 120 phút,
    - **Then** đơn chuyển `approved` với `approvedMinutes = 120`; ngày công cộng đúng 120 phút vào `overtime_minutes`.
  - **Kịch bản 5: Chặn duyệt vượt số đăng ký**
    - **Given** nhân viên đăng ký 120 phút,
    - **When** Line Manager cố duyệt 180 phút,
    - **Then** hệ thống trả `422`; `ck_overtime_minutes` chặn `approved_minutes > requested_minutes`.
  - **Kịch bản 6: Không ghi nhận tăng ca dưới ngưỡng**
    - **Given** policy có `min_overtime_minutes = 30` và nhân viên chỉ làm thêm 20 phút,
    - **When** hệ thống tính bảng công,
    - **Then** `overtime_minutes = 0` cho ngày đó và giao diện nêu rõ lý do không đạt ngưỡng.
  - **Kịch bản 7: Đơn đã duyệt nhưng không có dữ liệu chấm công**
    - **Given** đơn tăng ca `approved` nhưng không có sự kiện chấm công phủ khoảng thời gian đã duyệt,
    - **When** HR soát kỳ công,
    - **Then** trường hợp này xuất hiện trong danh sách ngoại lệ cần xử lý và **chặn việc duyệt kỳ công** cho tới khi được giải quyết.
- **Ràng buộc kỹ thuật**: `overtime_requests`; `ck_overtime_coefficient` giới hạn hệ số 1–5. **Giới hạn giờ tăng ca theo ngày/tháng/năm hiện chưa có bảng hạn mức** — đây là khoảng trống tuân thủ đã ghi nhận tại `[OD-5.4]` và [Open Decisions §10](open_decisions_attendance_leave.md#10-hạng-mục-còn-thiếu-trong-canonical-schema).

---

### ATT-03: Quỹ phép, Đơn nghỉ & Phê duyệt

#### [ATT-03.1] Cấu hình loại phép và chính sách (Configure Leave Types)
- **Phụ thuộc quyết định**: `[OD-6.1]` danh mục loại phép, `[OD-6.5]` quỹ âm, `[OD-6.6]` đơn vị nhỏ nhất, `[OD-6.7]` ngày lễ trong khoảng nghỉ, `[OD-6.8]` số ngày báo trước, `[OD-6.9]` minh chứng, `[OD-7.1]` số cấp duyệt.
- **Mô tả**:
  > **Là một** HR Officer,
  > **Tôi muốn** cấu hình từng loại phép với chính sách riêng và trình HR Manager phê duyệt,
  > **Để** không loại phép nào được đưa vào sử dụng khi chính sách của nó chưa được phê duyệt chính thức.
- **Tiền điều kiện**: Người dùng có vai trò `ROLE_HR_OFFICER`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Tạo loại phép ở trạng thái nháp**
    - **Given** HR Officer nhập mã `ANNUAL`, đơn vị `days`, cho phép nửa ngày, không tính ngày lễ, số ngày báo trước 3, số cấp duyệt 1,
    - **When** bấm lưu,
    - **Then** bản ghi được tạo với `policy_status = 'draft'` và **chưa** dùng được để gửi đơn.
  - **Kịch bản 2: Chặn gửi đơn với loại phép còn nháp**
    - **Given** loại phép có `policy_status = 'draft'`,
    - **When** nhân viên gửi đơn nghỉ với loại phép đó,
    - **Then** hệ thống trả `422` với mã lỗi nêu rõ chính sách chưa được phê duyệt.
  - **Kịch bản 3: Phê duyệt chính sách**
    - **Given** HR Manager xem xét và chấp thuận,
    - **When** phê duyệt loại phép,
    - **Then** `policy_status` chuyển `approved` cùng `approved_by` và `approved_at`; `ck_leave_type_approved` đảm bảo không tồn tại loại phép `approved` mà thiếu hai trường này.
  - **Kịch bản 4: Chặn số cấp duyệt ngoài miền hợp lệ**
    - **Given** HR Officer nhập `approvalLevels = 5`,
    - **When** bấm lưu,
    - **Then** hệ thống trả `422`; `ck_leave_type_levels` giới hạn 1–3.
- **Ràng buộc kỹ thuật**: `leave_types`; API `GET /api/v1/leave/types` và các endpoint quản trị tương ứng.

---

#### [ATT-03.2] Gửi đơn nghỉ phép và giữ chỗ quỹ (Submit Leave Request)
- **Phụ thuộc quyết định**: `[OD-6.2]` … `[OD-6.9]`, đặc biệt `[OD-6.7]` cách tính ngày nghỉ khi có ngày lễ / ngày nghỉ tuần.
- **Mô tả**:
  > **Là một** Nhân viên,
  > **Tôi muốn** gửi đơn nghỉ phép và thấy ngay số ngày bị trừ cùng số dư còn lại,
  > **Để** tôi biết chắc mình còn đủ phép và không phải tự tính tay số ngày nghỉ.
- **Tiền điều kiện**: Loại phép có `policy_status = 'approved'`; nhân viên có bản ghi `leave_balances` cho loại phép và năm tương ứng.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Gửi đơn hợp lệ và giữ chỗ quỹ**
    - **Given** nhân viên còn `availableUnits = 8` ngày phép năm và xin nghỉ 3 ngày làm việc,
    - **When** gửi `POST /api/v1/leave/requests`,
    - **Then** hệ thống trả `201` với đơn `pending`, `requestedUnits = 3`; `reserved_units` tăng 3 và `available_units` giảm còn 5 — **tất cả trong một transaction** cùng audit log và outbox message.
  - **Kịch bản 2: Server tính số ngày, bỏ qua giá trị client gửi**
    - **Given** khoảng nghỉ từ thứ Năm đến thứ Ba tuần sau, trong đó có 2 ngày nghỉ cuối tuần và 1 ngày lễ, loại phép có `counts_holidays = false`,
    - **When** gửi đơn,
    - **Then** `requestedUnits` bằng 3 (chỉ đếm ngày có ca làm việc), **không** phải 6; nếu client gửi kèm số ngày tự tính thì giá trị đó bị bỏ qua.
  - **Kịch bản 3: Không đủ quỹ thì không giữ chỗ**
    - **Given** nhân viên còn 1 ngày phép và loại phép có `allow_negative_balance = false`,
    - **When** xin nghỉ 3 ngày,
    - **Then** hệ thống trả `409` với mã lỗi nghiệp vụ ổn định; **không** tạo đơn và **không** thay đổi `reserved_units`.
  - **Kịch bản 4: Chặn trùng đơn ở tầng database**
    - **Given** nhân viên đã có đơn `approved` từ 10/10 đến 12/10,
    - **When** gửi đơn mới từ 11/10 đến 13/10,
    - **Then** hệ thống trả `409`; `ex_leave_requests_no_overlap` chặn, không phụ thuộc kiểm tra đọc-rồi-ghi ở application.
  - **Kịch bản 5: Gửi lại do retry không tạo đơn thứ hai**
    - **Given** request bị timeout ở client nhưng server đã tạo đơn,
    - **When** client gửi lại cùng `Idempotency-Key`,
    - **Then** hệ thống trả về chính đơn đã tạo; `ux_leave_requests_idempotency` là chốt chặn cuối.
  - **Kịch bản 6: Chặn đơn không đủ ngày báo trước**
    - **Given** loại phép có `min_notice_days = 3` và nhân viên xin nghỉ vào ngày mai,
    - **When** gửi đơn,
    - **Then** hệ thống trả `422` nêu rõ số ngày báo trước tối thiểu.
  - **Kịch bản 7: Bắt buộc minh chứng khi chính sách yêu cầu**
    - **Given** loại phép có `requires_attachment = true`,
    - **When** nhân viên gửi đơn mà không đính kèm tệp,
    - **Then** hệ thống trả `422`; tệp đính kèm được lưu vào private object storage, không phải URL công khai.
  - **Kịch bản 8: Hai đơn gửi đồng thời không làm âm quỹ**
    - **Given** nhân viên còn 3 ngày phép và gửi đồng thời hai đơn, mỗi đơn 2 ngày,
    - **When** cả hai request được xử lý song song,
    - **Then** chỉ một đơn thành công, đơn còn lại trả `409`; tổng `reserved_units` không bao giờ vượt quỹ khả dụng.
- **Ràng buộc kỹ thuật**: `leave_requests`, `leave_balances`; `available_units` là generated column nên không thể bị client tính lệch; `requested_units` bị chặn phải lớn hơn 0.

---

#### [ATT-03.3] Duyệt hoặc từ chối đơn nghỉ phép (Decide Leave Request)
- **Phụ thuộc quyết định**: `[OD-7.1]` số cấp duyệt, `[OD-7.2]` người duyệt thay, `[OD-7.3]` thời điểm trừ quỹ, `[OD-7.4]` hủy đơn đã duyệt.
- **Mô tả**:
  > **Là một** Line Manager,
  > **Tôi muốn** duyệt hoặc từ chối đơn nghỉ của nhân viên trong phạm vi quản lý, kèm lý do khi từ chối,
  > **Để** quỹ phép được cập nhật đúng và nhân viên nhận kết quả rõ ràng, có thể truy vết.
- **Tiền điều kiện**: Đơn ở trạng thái `pending`; người dùng nằm trong chuỗi phê duyệt hợp lệ và trong phạm vi dữ liệu được phân quyền.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Duyệt đơn một cấp**
    - **Given** đơn `pending` của loại phép có `approvalLevels = 1`,
    - **When** Line Manager gửi `POST /api/v1/leave/requests/{leaveRequestId}/approve` kèm `If-Match` đúng phiên bản,
    - **Then** đơn chuyển `approved`; `reserved_units` giảm và `used_units` tăng đúng `requestedUnits`; hệ thống ghi `leave_request_decisions` với `sequence = 1` và `request_version` tại thời điểm quyết định; các ngày liên quan trong bảng công được đánh dấu `on_leave`.
  - **Kịch bản 2: Từ chối và trả lại quỹ**
    - **Given** đơn `pending` đang giữ chỗ 3 ngày,
    - **When** Line Manager từ chối kèm lý do,
    - **Then** đơn chuyển `rejected`; `reserved_units` giảm 3 và `used_units` **không** đổi; `available_units` trở về giá trị trước khi gửi đơn.
  - **Kịch bản 3: Từ chối không có lý do bị chặn**
    - **Given** Line Manager chọn từ chối,
    - **When** không nhập lý do,
    - **Then** hệ thống chặn và yêu cầu nhập lý do trước khi lưu.
  - **Kịch bản 4: Đơn nhiều cấp chuyển tiếp thay vì kết thúc**
    - **Given** loại phép có `approvalLevels = 2` và đơn đang ở `current_approval_level = 1`,
    - **When** Line Manager duyệt,
    - **Then** đơn **vẫn** ở `pending` với `current_approval_level = 2` và chuyển vào hàng chờ của HR Manager; chỉ khi cấp cuối duyệt thì đơn mới chuyển `approved`.
  - **Kịch bản 5: Sai người duyệt trả 403, không phải 409**
    - **Given** người dùng không nằm trong chuỗi phê duyệt của đơn hoặc ngoài phạm vi dữ liệu,
    - **When** gửi quyết định,
    - **Then** hệ thống trả `403 Forbidden` và không ghi thay đổi; phân biệt rõ với `409` của xung đột phiên bản.
  - **Kịch bản 6: Quyết định đồng thời không ghi đè**
    - **Given** hai người duyệt cùng mở một đơn,
    - **When** người thứ hai gửi quyết định với `If-Match` là phiên bản đã cũ,
    - **Then** hệ thống trả `409` và giữ nguyên quyết định đầu tiên.
  - **Kịch bản 7: Duyệt hàng loạt, mỗi phần tử độc lập**
    - **Given** HR Manager chọn 10 đơn để duyệt, trong đó 2 đơn đã bị người khác xử lý,
    - **When** gửi `POST /api/v1/leave/requests/batch-decision`,
    - **Then** phản hồi trả kết quả **cho từng phần tử**: 8 đơn thành công, 2 đơn trả `409`; một xung đột **không** che kết quả của các phần tử còn lại.
  - **Kịch bản 8: Không có quỹ bị giữ chỗ bởi đơn đã kết thúc**
    - **Given** một tập đơn ở các trạng thái `approved`, `rejected`, `cancelled`,
    - **When** đối soát quỹ phép,
    - **Then** `reserved_units` chỉ phản ánh các đơn còn `pending`; không đơn nào đã kết thúc mà còn giữ chỗ quỹ.
  - **Kịch bản 9: Thông báo chỉ gửi sau khi commit**
    - **Given** transaction duyệt đơn thất bại ở bước cuối,
    - **When** kiểm tra hộp thư nhân viên,
    - **Then** không có email nào được gửi; thông báo đi qua outbox và chỉ được worker phát sau khi transaction commit thành công.
- **Ràng buộc kỹ thuật**: `leave_requests`, `leave_request_decisions`, `leave_balances`, `outbox_messages`; `ux_leave_decision_sequence` đảm bảo mỗi cấp duyệt chỉ có một quyết định.

---

#### [ATT-03.4] Xem quỹ phép và lịch sử đơn (View Leave Balance and History)
- **Phụ thuộc quyết định**: `[OD-6.3]` cách cấp quỹ, `[OD-6.4]` chuyển quỹ sang năm sau, `[OD-9.2]` quyền xem lý do nghỉ.
- **Mô tả**:
  > **Là một** Nhân viên,
  > **Tôi muốn** xem số phép được hưởng, đã dùng, đang giữ chỗ và còn lại cùng lịch sử đơn của mình,
  > **Để** tôi tự lập kế hoạch nghỉ mà không phải hỏi HR.
- **Tiền điều kiện**: Nhân viên đã đăng nhập.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Xem quỹ phép của chính mình**
    - **Given** nhân viên có quỹ phép năm hiện tại,
    - **When** gọi `GET /api/v1/leave/balances?year=2026`,
    - **Then** hệ thống trả `entitledUnits`, `carriedOverUnits`, `usedUnits`, `reservedUnits` và `availableUnits` kèm `policyVersion` đã áp dụng.
  - **Kịch bản 2: Chặn xem quỹ người khác**
    - **Given** nhân viên không có quyền quản lý,
    - **When** gọi API với `employeeId` của người khác,
    - **Then** hệ thống trả `403 Forbidden`, **không** trả danh sách rỗng — tránh để người dùng suy ra sự tồn tại của dữ liệu ngoài phạm vi.
  - **Kịch bản 3: Quản lý xem trong phạm vi phòng ban**
    - **Given** Line Manager có `data_scope_type = 'department'`,
    - **When** xem quỹ phép của đội mình,
    - **Then** chỉ nhân viên thuộc phạm vi được trả về; nhân viên ngoài phạm vi không xuất hiện.
  - **Kịch bản 4: Hiển thị hạn dùng quỹ chuyển tiếp**
    - **Given** nhân viên có `carriedOverUnits = 5` với `carry_over_expires_on = 2026-03-31`,
    - **When** xem quỹ phép,
    - **Then** giao diện nêu rõ số quỹ chuyển tiếp và ngày hết hạn, cảnh báo khi còn dưới 30 ngày.
  - **Kịch bản 5: Lịch đội nhóm không tiết lộ lý do nghỉ**
    - **Given** nhân viên xem lịch nghỉ của đồng nghiệp trong phòng,
    - **When** mở lịch đội nhóm,
    - **Then** chỉ thấy trạng thái nghỉ và khoảng thời gian; **không** thấy loại phép chi tiết và lý do nghỉ.
- **Ràng buộc kỹ thuật**: `leave_balances`, `leave_requests`; `availableUnits` lấy từ generated column ở database, không tính lại ở frontend.

---

### ATT-04: Bảng công, Duyệt kỳ & Khóa kỳ

#### [ATT-04.1] Xem bảng công cá nhân và đội nhóm (View Timesheet)
- **Phụ thuộc quyết định**: `[OD-1.5]` làm tròn, `[OD-1.6]` ân hạn, `[OD-1.7]` ngưỡng vắng, `[OD-9.1]` phạm vi xem.
- **Mô tả**:
  > **Là một** Nhân viên hoặc Line Manager,
  > **Tôi muốn** xem bảng công theo khoảng ngày với đầy đủ chỉ số và biết số liệu được tính theo quy tắc nào,
  > **Để** phát hiện sai sót sớm và tin được con số trước khi nó đi vào lương.
- **Tiền điều kiện**: Có `attendance_policies` ở trạng thái `active`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Xem bảng công cá nhân**
    - **Given** nhân viên chọn khoảng từ 01/10 đến 31/10,
    - **When** gọi `GET /api/v1/attendance/timesheets?from=2026-10-01&to=2026-10-31`,
    - **Then** hệ thống trả từng ngày với `status`, `scheduledMinutes`, `workedMinutes`, `lateMinutes`, `earlyLeaveMinutes`, `overtimeMinutes` và `policyVersion`.
  - **Kịch bản 2: Công khai quy tắc đã dùng để tính**
    - **Given** hai ngày công được tính theo hai phiên bản policy khác nhau,
    - **When** xem bảng công,
    - **Then** mỗi dòng hiển thị `policyVersion` riêng của nó; giao diện không gộp thành một quy tắc duy nhất.
  - **Kịch bản 3: Quản lý chỉ xem được trong phạm vi**
    - **Given** Line Manager có phạm vi `department`,
    - **When** truy vấn bảng công với `departmentId` ngoài phạm vi,
    - **Then** hệ thống trả `403`, không trả dữ liệu.
  - **Kịch bản 4: Phân trang có giới hạn**
    - **Given** truy vấn một phòng ban lớn trong 3 tháng,
    - **When** gọi API không truyền `pageSize`,
    - **Then** hệ thống áp `pageSize` mặc định có giới hạn trên và trả `page` metadata; không bao giờ trả toàn bộ tập dữ liệu không giới hạn.
  - **Kịch bản 5: Client không được tính lại số liệu**
    - **Given** frontend nhận về `workedMinutes`,
    - **When** hiển thị tổng giờ công của kỳ,
    - **Then** tổng được lấy từ phản hồi của server; frontend **không** tự cộng trừ theo giờ vào/ra để tránh lệch với số liệu lương.
- **Ràng buộc kỹ thuật**: `attendance_daily_records`; `ux_attendance_daily_employee_date` đảm bảo một dòng công mỗi nhân viên mỗi ngày.

---

#### [ATT-04.2] Duyệt và khóa kỳ công (Review, Approve and Lock Timesheet Period)
- **Phụ thuộc quyết định**: `[OD-8.1]` chu kỳ kỳ công, `[OD-8.2]` hạn chốt, `[OD-8.3]` người khóa/mở lại, `[OD-8.4]` hình thức bàn giao Payroll, `[OD-8.5]` **điều chỉnh sau bàn giao**.
- **Mô tả**:
  > **Là một** HR Manager,
  > **Tôi muốn** duyệt rồi khóa kỳ công sau khi mọi ngoại lệ đã được xử lý,
  > **Để** số liệu bàn giao cho Payroll là bất biến và mọi thay đổi sau đó đều có dấu vết.
- **Tiền điều kiện**: Kỳ công tồn tại; người dùng có vai trò `ROLE_HR_MGR`.
- **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  - **Kịch bản 1: Chặn duyệt kỳ khi còn ngoại lệ**
    - **Given** trong kỳ còn ít nhất một đơn hiệu chỉnh `pending`, hoặc một ngày `incomplete`, hoặc một đơn tăng ca đã duyệt mà không có dữ liệu chấm công,
    - **When** HR Manager bấm duyệt kỳ,
    - **Then** hệ thống từ chối và liệt kê cụ thể từng ngoại lệ kèm nhân viên và ngày tương ứng.
  - **Kịch bản 2: Duyệt và khóa kỳ thành công**
    - **Given** toàn bộ ngoại lệ đã được xử lý,
    - **When** HR Manager duyệt rồi khóa kỳ,
    - **Then** kỳ chuyển `approved` rồi `locked` với `locked_by` và `locked_at`; `ck_timesheet_period_locked` đảm bảo không tồn tại kỳ `locked` mà thiếu hai trường này.
  - **Kịch bản 3: Kỳ đã khóa là bất biến**
    - **Given** kỳ đang ở `locked`,
    - **When** có sự kiện chấm công, đơn hiệu chỉnh, phân ca hoặc đơn tăng ca nhắm vào ngày trong kỳ,
    - **Then** mọi thao tác đó bị từ chối với `409` và mã lỗi nghiệp vụ nêu rõ kỳ công đã khóa.
  - **Kịch bản 4: Mở lại kỳ bắt buộc có lý do**
    - **Given** HR Manager cần sửa số liệu của kỳ đã khóa,
    - **When** mở lại kỳ mà không nhập lý do,
    - **Then** hệ thống từ chối; `ck_timesheet_period_reopened` đảm bảo kỳ `reopened` luôn có `reopened_by` và `reopen_reason`; hành động được ghi audit log.
  - **Kịch bản 5: Chặn tạo kỳ công giao nhau**
    - **Given** đã có kỳ 01/10–31/10,
    - **When** HR Officer tạo kỳ 15/10–15/11,
    - **Then** hệ thống trả `409`; `ex_timesheet_periods_no_overlap` chặn ở tầng database.
  - **Kịch bản 6: Không bàn giao Payroll khi chưa khóa kỳ**
    - **Given** kỳ đang ở `approved` nhưng chưa `locked`,
    - **When** HR Officer thực hiện bàn giao Payroll,
    - **Then** hệ thống từ chối; `ck_timesheet_period_handoff` đảm bảo `payroll_handoff_at` chỉ tồn tại khi đã có `locked_at`.
  - **Kịch bản 7: Thay đổi policy không âm thầm tái tính số cũ**
    - **Given** một `attendance_policies` mới được kích hoạt giữa kỳ,
    - **When** kiểm tra các ngày đã tính trước đó,
    - **Then** các ngày đó **giữ nguyên** `policy_version` cũ và số liệu cũ; tái tính là hành động tường minh, có audit log.
  - **Kịch bản 8: Nhân viên tạm hoãn không sinh dòng công tính lương**
    - **Given** nhân viên có `employee_events` loại `suspension` đang hiệu lực,
    - **When** hệ thống tính bảng công cho khoảng thời gian tạm hoãn,
    - **Then** không sinh dòng công tính lương cho nhân viên đó trong khoảng đó.
- **Ràng buộc kỹ thuật**: `timesheet_periods`, `attendance_daily_records`. **Việc mở lại kỳ đã bàn giao Payroll hiện chưa có cơ chế điều chỉnh** — rủi ro đã ghi nhận tại `[OD-8.5]`; phải chốt trước khi nối phân hệ Payroll.

---

## 6. Ma trận Phân quyền & Traceability

### 6.1. Bảng phân quyền Role-to-Story

| Mã User Story | Tiêu đề tóm tắt | Employee | Recruiter | Interviewer / Hiring Mgr | Line Manager | HR Officer | HR Manager | Super Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **REC-01.1** | Tạo đề xuất tuyển dụng | — | — | **Tạo/Sửa** | — | — | Xem | Quản trị |
| **REC-01.2** | Duyệt & Đăng tin tuyển | — | **Đăng tin** | — | — | — | **Phê duyệt** | Quản trị |
| **REC-02.1** | Tiếp nhận CV & Quét an toàn | Nộp CV | **Upload** | — | — | — | Xem | Giám sát |
| **REC-02.2** | Nhận diện trùng lặp & AI parse | — | **Kiểm tra** | — | — | — | Xem | — |
| **REC-03.1** | Xem bảng Kanban ATS | — | **Toàn quyền** | Xem vòng phỏng vấn | — | — | Xem | — |
| **REC-03.2** | Chuyển giai đoạn Kanban | — | **Thực hiện** | — | — | — | Giám sát | — |
| **REC-04.1** | Xếp lịch phỏng vấn | Xem lịch | **Tạo lịch** | Tham gia | — | — | Giám sát | — |
| **REC-05.1** | Chấm điểm Scorecard | — | Xem tổng hợp | **Chấm điểm** | — | — | Quản lý | — |
| **REC-06.1** | Tạo & Duyệt Offer Letter | Phản hồi | **Soạn thảo** | Xem | — | — | **Phê duyệt** | — |
| **REC-06.2** | Tự động chuyển Onboarding | — | — | — | — | **Tiếp nhận** | Giám sát | — |
| **EMP-01.1** | Tra cứu danh bạ nhân sự | Xem cơ bản | — | Xem phòng ban | Xem phạm vi | **Toàn quyền** | **Toàn quyền** | Quản trị |
| **EMP-01.2** | Cập nhật hồ sơ cá nhân | **Cập nhật** | — | — | — | Kiểm tra | Phê duyệt | — |
| **EMP-02.1** | Xem sơ đồ tổ chức | Xem | Xem | Xem | Xem | Xem | Xem/Sửa | Quản trị |
| **EMP-03.1** | Theo dõi việc Onboarding | Nhận việc | — | Nhận việc | Nhận việc | **Điều phối** | Giám sát | — |
| **EMP-04.1** | Khởi tạo & Duyệt biến động | Xem của mình | — | Đề xuất | Đề xuất | Soạn thảo | **Phê duyệt** | Quản trị |
| **EMP-05.1** | Lưu trữ hồ sơ điện tử | Xem của mình | — | — | — | **Quản lý** | **Quản lý** | Quản trị |
| **EMP-06.1** | Đánh giá kết quả thử việc | Xem của mình | — | — | **Đánh giá** | Điều phối | Giám sát | — |
| **EMP-06.2** | Quyết định hết thử việc | Nhận kết quả | — | — | Đề xuất | Soạn thảo | **Phê duyệt** | — |
| **EMP-07.1** | Khởi tạo hồ sơ thôi việc | Xem của mình | — | — | Xác nhận bàn giao | **Soạn thảo** | **Phê duyệt** | — |
| **EMP-07.2** | Hoàn tất bàn giao & đóng case | Thực hiện bàn giao | — | — | Xác nhận | **Đóng case** | Duyệt ngoại lệ | Quản trị tài khoản |
| **CON-01.1** | Soạn thảo hợp đồng | — | — | — | — | **Soạn thảo** | Phê duyệt | — |
| **CON-01.2** | Ký kết & Kích hoạt HĐ | Xem/Ký | — | — | — | **Thực hiện** | Giám sát | — |
| **CON-02.1** | Cảnh báo hạn hợp đồng | — | — | Nhận thông báo | Nhận thông báo | **Xử lý** | Giám sát | — |
| **CON-03.1** | Quản lý phụ lục hợp đồng | Xem của mình | — | — | — | **Soạn thảo** | **Phê duyệt** | — |
| **ATT-01.1** | Định nghĩa ca làm việc | Xem ca của mình | — | — | Xem đội nhóm | **Tạo/Sửa** | Phê duyệt | Quản trị |
| **ATT-01.2** | Phân ca theo tuần | Xem của mình | — | — | Xem đội nhóm | **Phân ca** | Giám sát | — |
| **ATT-01.3** | Thiết lập lịch nghỉ lễ | Xem | Xem | Xem | Xem | **Nhập** | **Phê duyệt** | — |
| **ATT-02.1** | Chấm công vào / ra | **Chấm công** | — | — | Xem đội nhóm | Xem toàn bộ | Giám sát | — |
| **ATT-02.2** | Tiếp nhận dữ liệu thiết bị | — | — | — | — | Xử lý ngoại lệ | Giám sát | **Cấu hình thiết bị** |
| **ATT-02.3** | Hiệu chỉnh công | **Gửi đơn** | — | — | **Phê duyệt** | Duyệt thay | Giám sát | — |
| **ATT-02.4** | Đăng ký & duyệt tăng ca | **Gửi đơn** | — | — | **Phê duyệt** | Đối soát | Giám sát | — |
| **ATT-03.1** | Cấu hình loại phép | Xem | — | — | Xem | **Cấu hình** | **Phê duyệt** | — |
| **ATT-03.2** | Gửi đơn nghỉ phép | **Gửi đơn** | — | — | Xem đội nhóm | Xem toàn bộ | Giám sát | — |
| **ATT-03.3** | Duyệt / từ chối đơn nghỉ | Nhận kết quả | — | — | **Phê duyệt cấp 1** | Hủy đơn đã qua | **Phê duyệt cấp 2** | — |
| **ATT-03.4** | Xem quỹ phép & lịch sử | **Xem của mình** | — | — | Xem phạm vi | Xem toàn bộ | Xem toàn bộ | — |
| **ATT-04.1** | Xem bảng công | **Xem của mình** | — | — | **Soát đội nhóm** | Đối soát | Xem toàn bộ | — |
| **ATT-04.2** | Duyệt & khóa kỳ công | — | — | — | Soát đội nhóm | Chuẩn bị kỳ | **Duyệt & Khóa** | — |

Phạm vi dữ liệu được kiểm tra phía server theo `user_roles.data_scope_type`. Nhãn "Xem phạm vi" và "Xem đội nhóm" tương ứng `data_scope_type = 'department'`; "Xem của mình" tương ứng `'self'`.

---

### 6.2. Ánh xạ Cơ sở Dữ liệu & Use Cases (Traceability Matrix)

> Bảng dưới đây đã được đối chiếu với [`schema.sql`](../database/schema.sql) và [`openapi.yaml`](../api/openapi.yaml). Tất cả tên bảng và endpoint đều tồn tại trong canonical artifact; base path là `/api/v1`.

#### Recruitment

| User Story ID | Use Case ID | Bảng Cơ sở Dữ liệu (`schema.sql`) | API Endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **REC-01.1/.2** | `UC_REQ_DRAFT`, `UC_REQ_DECIDE` | `job_postings`, `departments`, `positions` | `POST /api/v1/recruitment/requisitions`, `POST /api/v1/recruitment/requisitions/{requisitionId}/{action}` |
| **REC-02.1/.2** | `UC_UPLOAD`, `UC_PARSE` | `candidates`, `resumes`, `applications` | `POST /api/v1/recruitment/resumes`, `GET /api/v1/recruitment/intakes/{intakeId}`, `POST /api/v1/recruitment/intakes/{intakeId}/confirm` |
| **REC-03.1** | `UC_PIPELINE` | `applications`, `job_postings` | `GET /api/v1/recruitment/pipeline` |
| **REC-03.2** | `UC_ADVANCE` | `applications`, `application_stage_events`, `audit_logs` | `GET /api/v1/recruitment/applications/{applicationId}`, `POST /api/v1/recruitment/applications/{applicationId}/advance`, `POST /api/v1/recruitment/applications/{applicationId}/{terminalAction}` |
| **REC-04.1** | `UC_INTERVIEW` | `interviews` | `POST /api/v1/recruitment/interviews`, `POST /api/v1/recruitment/interviews/{interviewId}/{action}` |
| **REC-05.1** | `UC_SCORE` | `evaluations` | `POST /api/v1/recruitment/interviews/{interviewId}/evaluations`, `POST /api/v1/recruitment/evaluations/{evaluationId}/unlock` |
| **REC-06.1/.2** | `UC_OFFER`, `UC_ACCEPT`, `UC_HANDOFF` | `offers`, `employees`, `onboarding_tasks`, `contracts` | `POST /api/v1/recruitment/offers`, `POST /api/v1/recruitment/offers/{offerId}/{action}`, `POST /api/v1/recruitment/offers/{offerId}/response` |

#### Core HR

| User Story ID | Use Case ID | Bảng Cơ sở Dữ liệu (`schema.sql`) | API Endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **EMP-01.1/.2** | `UC_SEARCH`, `UC_VIEW`, `UC_SELF_CHANGE` | `employees`, `departments`, `positions` | `GET /api/v1/employees`, `GET /api/v1/employees/{employeeId}`, `PATCH /api/v1/employees/{employeeId}/profile` |
| **EMP-02.1** | `UC_ORG`, `UC_ORG_MGMT` | `departments`, `positions`, `employees` | `GET /api/v1/organization/chart`, `GET\|POST /api/v1/organization/departments`, `GET\|POST /api/v1/organization/positions` |
| **EMP-03.1** | `UC_ONBOARD`, `UC_TASK` | `onboarding_tasks`, `employees` | `GET /api/v1/onboarding/tasks`, `POST /api/v1/onboarding/tasks/{taskId}/{action}` |
| **EMP-04.1** | `UC_MOVEMENT`, `UC_MOVEMENT_DECIDE` | `employee_events`, `employees` | `POST /api/v1/employees/{employeeId}/events`, `POST /api/v1/employee-events/{eventId}/{action}` |
| **EMP-05.1** | `UC_DOCUMENT`, `UC_DOWNLOAD` | `employee_documents`, `employees` | `POST /api/v1/employees/{employeeId}/documents`, `POST /api/v1/employee-documents/{documentId}/download-url` |
| **EMP-06.1/.2** | `UC_PROBATION`, `UC_PROBATION_DECIDE` | `probation_reviews`, `employee_events`, `contracts`, `employees` | `GET\|POST /api/v1/employees/{employeeId}/probation-review`, `POST /api/v1/probation-reviews/{reviewId}/{action}` |
| **EMP-07.1/.2** | `UC_OFFBOARD`, `UC_OFFBOARD_TASK` | `offboarding_cases`, `offboarding_tasks`, `employee_events`, `users` | `GET\|POST /api/v1/offboarding/cases`, `POST /api/v1/offboarding/cases/{caseId}/{action}`, `GET /api/v1/offboarding/cases/{caseId}/tasks`, `POST /api/v1/offboarding/tasks/{taskId}/{action}` |
| **CON-01.1/.2** | `UC_DRAFT`, `UC_APPROVE`, `UC_SIGN` | `contracts`, `employees` | `POST /api/v1/contracts`, `POST /api/v1/contracts/{contractId}/{action}`, `PUT /api/v1/contracts/{contractId}/signed-document` |
| **CON-02.1** | `UC_MONITOR`, `UC_ALERT` | `contracts`, `outbox_messages` | `GET /api/v1/contracts/expiring` |
| **CON-03.1** | `UC_ADDENDUM`, `UC_ADDENDUM_EFFECT` | `contract_addenda`, `contracts`, `employee_events` | `POST /api/v1/contracts/{contractId}/addenda`, `POST /api/v1/contract-addenda/{addendumId}/{action}` |

#### Attendance & Leave

| User Story ID | Use Case ID | Bảng Cơ sở Dữ liệu (`schema.sql`) | API Endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **ATT-01.1** | `UC_SHIFT` | `work_shifts` | `GET\|POST /api/v1/attendance/shifts`, `GET\|PUT /api/v1/attendance/shifts/{shiftId}` |
| **ATT-01.2** | `UC_ASSIGN` | `work_schedule_assignments`, `work_shifts` | `GET\|PUT /api/v1/attendance/schedules` |
| **ATT-01.3** | `UC_HOLIDAY` | `holidays` | `GET\|POST /api/v1/attendance/holidays`, `PUT\|DELETE /api/v1/attendance/holidays/{holidayId}` |
| **ATT-02.1** | `UC_CAPTURE`, `UC_CALCULATE` | `attendance_events`, `attendance_daily_records`, `attendance_policies` | `POST /api/v1/attendance/events` |
| **ATT-02.2** | `UC_CAPTURE` | `attendance_events` | `POST /api/v1/integrations/attendance/events` |
| **ATT-02.3** | `UC_CORRECT` | `attendance_corrections`, `attendance_events`, `attendance_daily_records` | `GET\|POST /api/v1/attendance/corrections`, `POST /api/v1/attendance/corrections/{correctionId}/{action}` |
| **ATT-02.4** | `UC_OVERTIME` | `overtime_requests`, `attendance_daily_records`, `holidays` | `GET\|POST /api/v1/attendance/overtime-requests`, `POST /api/v1/attendance/overtime-requests/{overtimeRequestId}/{action}` |
| **ATT-03.1** | `UC_LEAVE_POLICY` | `leave_types` | `GET /api/v1/leave/types`, `POST /api/v1/leave/types`, `POST /api/v1/leave/types/{leaveTypeId}/{action}` |
| **ATT-03.2** | `UC_LEAVE`, `UC_VALIDATE` | `leave_requests`, `leave_balances`, `leave_types`, `holidays`, `work_schedule_assignments`, `outbox_messages` | `POST /api/v1/leave/requests` |
| **ATT-03.3** | `UC_APPROVE`, `UC_AUTH`, `UC_AUDIT` | `leave_requests`, `leave_request_decisions`, `leave_balances`, `audit_logs`, `outbox_messages` | `POST /api/v1/leave/requests/{leaveRequestId}/{action}`, `POST /api/v1/leave/requests/batch-decision` |
| **ATT-03.4** | `UC_BALANCE` | `leave_balances`, `leave_requests` | `GET /api/v1/leave/balances`, `GET /api/v1/leave/requests`, `GET /api/v1/leave/requests/{leaveRequestId}` |
| **ATT-04.1** | `UC_TIMESHEET` | `attendance_daily_records`, `attendance_policies` | `GET /api/v1/attendance/timesheets` |
| **ATT-04.2** | `UC_PERIOD_LOCK` | `timesheet_periods`, `attendance_daily_records`, `audit_logs` | `GET\|POST /api/v1/attendance/timesheet-periods`, `POST /api/v1/attendance/timesheet-periods/{periodId}/{action}` |

---

### 6.3. Điều kiện đưa story vào sprint

| Nhóm story | Điều kiện bắt buộc |
| :--- | :--- |
| `REC-*`, `EMP-01`…`EMP-05`, `CON-*` | Chốt Identity Provider và RBAC; sinh EF Core migration đầu tiên từ canonical schema. |
| `EMP-06`, `EMP-07` | Chốt template checklist offboarding theo đơn vị; chốt công thức quy đổi phép chưa dùng khi thôi việc `[OD-6.4]`. |
| `ATT-01`, `ATT-02` | Chốt toàn bộ Mục 1–5 của [Open Decisions](open_decisions_attendance_leave.md); có `attendance_policies` ở trạng thái `active`. |
| `ATT-03` | Chốt Mục 6 và 7 của Open Decisions; có `leave_types` với `policy_status = 'approved'`. |
| `ATT-04` | Chốt Mục 8 của Open Decisions, **bao gồm `[OD-8.5]`** trước khi nối Payroll. |
