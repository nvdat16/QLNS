# 📋 INVEST User Stories — 2 Phân Hệ Triển Khai Trước (QLNS / NexusHR)

> **Tài liệu chuẩn yêu cầu nghiệp vụ (Authoritative Requirements Baseline)**
> **Phạm vi:** hai phân hệ được chọn triển khai trước theo bản đồ chức năng — **Core HR** (bao gồm nhánh con Contracts) và **Recruitment (ATS)**. Attendance & Leave đã được tách ra ngoài phạm vi; story của nhóm đó được giữ tại [deferred/attendance_leave/user_stories_att.md](deferred/attendance_leave/user_stories_att.md).
> **Ánh xạ kiến trúc:** [Functional Specifications](functional_specifications.md) · [Use Cases](use_cases.md) · [Architecture (arc42 + C4)](architecture.md) · [Database Schema](../database/schema.sql) · [API Contract](../api/openapi.yaml)
> **Nguyên tắc thiết kế Story:** Tuân thủ tiêu chuẩn **INVEST** (*Independent, Negotiable, Valuable, Estimable, Small, Testable*).

> [!NOTE]
> Theo bản đồ chức năng, **Contract Management là nhánh con của Core HR** (cùng cấp với Employee Profiles, Organization Management và Employee Lifecycle). Tài liệu này giữ mã `CON-*` riêng cho dễ truy vết, nhưng về phạm vi triển khai thì `EMP-*` và `CON-*` cùng thuộc một phân hệ.

---

## Trạng thái theo phân hệ

| Phân hệ | Mã story | Trạng thái |
| :--- | :--- | :--- |
| **Recruitment (ATS)** | `REC-01` … `REC-06` — 10 story | Proposed |
| **Core HR — Profile, Organization, Lifecycle** | `EMP-01` … `EMP-07` — 10 story | Proposed |
| **Core HR — Contracts** | `CON-01` … `CON-03` — 4 story | Proposed |

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
- [5. Ma trận Phân quyền & Traceability](#5-ma-trận-phân-quyền--traceability)

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
    - **When** có yêu cầu xử lý lại sự kiện chấp nhận Offer (do mạng lag hoặc retry API) — dù cùng hay khác `Idempotency-Key`,
    - **Then** hệ thống nhận diện hồ sơ đã tồn tại qua `employees.source_application_id`, trả về `OfferResponseResult` với `replayed = true` và không tạo thêm bản ghi nhân viên, hợp đồng hay checklist.
  - **Kịch bản 3: Hai yêu cầu chấp nhận song song**
    - **Given** hai request `accept` cho cùng một Offer đến server gần như đồng thời,
    - **When** cả hai được xử lý,
    - **Then** đúng một request tạo dữ liệu; request còn lại nhận cùng kết quả với `replayed = true`; không bao giờ tồn tại hai `employees` có cùng `source_application_id`.
  - **Kịch bản 4: Offer không còn mở**
    - **Given** Offer đã `expired`, `declined` hoặc `cancelled`,
    - **When** ứng viên bấm chấp nhận,
    - **Then** hệ thống trả `409` với mã `recruitment.offer_not_open` và không ghi gì.
  - **Kịch bản 5: Token không khớp Offer**
    - **Given** `X-Offer-Token` hợp lệ nhưng thuộc một Offer khác,
    - **When** gọi endpoint phản hồi,
    - **Then** hệ thống trả `401` và không tiết lộ Offer đích có tồn tại hay không.
  - **Kịch bản 6: Nhân viên mới chưa có email công vụ**
    - **Given** ứng viên chỉ có email cá nhân,
    - **When** handoff tạo bản ghi `employees`,
    - **Then** `work_email` được để trống, `personal_email` được copy từ ứng viên, `status = 'probation'`; hệ thống chặn chuyển sang `active` cho tới khi IT cấp email công vụ (`ck_employee_active_requires_work_email`).
- **Ràng buộc kỹ thuật**: Toàn bộ ghi vào `offers`, `applications`, `application_stage_events`, `employees`, `contracts`, `onboarding_tasks`, `audit_logs`, `outbox_messages` nằm trong **một transaction**. `employee_code` sinh từ `employee_code_seq` theo định dạng `EMP-00001`. Thông báo và cấp tài khoản đi qua outbox, chỉ phát sau commit.

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
  - **Kịch bản 5: Hồ sơ vẫn được lưu trữ sau khi thôi việc**
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

## 5. Ma trận Phân quyền & Traceability

### 5.1. Bảng phân quyền Role-to-Story

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

Phạm vi dữ liệu được kiểm tra phía server theo `user_roles.data_scope_type`. Nhãn "Xem phạm vi" và "Xem đội nhóm" tương ứng `data_scope_type = 'department'`; "Xem của mình" tương ứng `'self'`.

---

### 5.2. Ánh xạ Cơ sở Dữ liệu & Use Cases (Traceability Matrix)

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

---

### 5.3. Điều kiện đưa story vào sprint

| Nhóm story | Điều kiện bắt buộc |
| :--- | :--- |
| `REC-*`, `EMP-01`…`EMP-05`, `CON-*` | Chốt Identity Provider và RBAC; sinh EF Core migration đầu tiên từ canonical schema. |
| `EMP-06`, `EMP-07` | Chốt template checklist offboarding theo đơn vị; chốt danh mục khoản thanh toán khi chấm dứt (thuộc Payroll, hiện ngoài phạm vi). |
