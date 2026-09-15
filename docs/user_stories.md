# 📋 INVEST User Stories — 3 Phân Hệ Quản Lý Cốt Lõi (QLNS / NexusHR)

> **Tài liệu chuẩn yêu cầu nghiệp vụ (Authoritative Requirements Baseline)**  
> **Ánh xạ kiến trúc:** [Functional Specifications](functional_specifications.md) · [Use Cases](use_cases.md) · [Architecture (arc42 + C4)](architecture.md) · [Database Schema](../database/schema.sql)  
> **Nguyên tắc thiết kế Story:** Tuân thủ tiêu chuẩn **INVEST** (*Independent, Negotiable, Valuable, Estimable, Small, Testable*).

---

## Mục lục

- [1. Quy ước & Cấu trúc User Story](#1-quy-ước--cấu-trúc-user-story)
- [2. Phân Hệ 1: Tuyển Dụng Thông Minh (Smart ATS Recruitment)](#2-phân-hệ-1-tuyển-dụng-thông-minh-smart-ats-recruitment)
  - [REC-01: Quản lý Đề xuất & Tin Tuyển dụng](#rec-01-quản-lý-đề-xuất--tin-tuyển-dụng)
  - [REC-02: Tiếp nhận & Sàng lọc CV Ứng viên](#rec-02-tiếp-nhận--sàng-lọc-cv-ứng-viên)
  - [REC-03: Đường ống Tuyển dụng Kanban & Chuyển bước](#rec-03-đường-ống-tuyển-dụng-kanban--chuyển-bước)
  - [REC-04: Điều phối Lịch Phỏng vấn](#rec-04-điều-phối-lịch-phỏng-vấn)
  - [REC-05: Đánh giá Ứng viên qua Scorecard](#rec-05-đánh-giá-ứng-viên-qua-scorecard)
  - [REC-06: Đề nghị Tuyển dụng & Bàn giao Onboarding](#rec-06-đề-nghị-tuyển-dụng--bàn-giao-onboarding)
- [3. Phân Hệ 2: Hồ Sơ & Vòng Đời Nhân Sự (Core HR & Employee Lifecycle)](#3-phân-hệ-2-hồ-sơ--vòng-đời-nhân-sự-core-hr--employee-lifecycle)
  - [EMP-01: Danh bạ & Hồ sơ Định danh Nhân viên](#emp-01-danh-bạ--hồ-sơ-định-danh-nhân-viên)
  - [EMP-02: Cơ cấu Tổ chức & Sơ đồ Phòng ban](#emp-02-cơ-cấu-tổ-chức--sơ-đồ-phòng-ban)
  - [EMP-03: Quy trình Tiếp nhận Nhân viên Mới (Onboarding)](#emp-03-quy-trình-tiếp-nhận-nhân-viên-mới-onboarding)
  - [EMP-04: Biến động Nhân sự & Quản lý Sự kiện Công tác](#emp-04-biến-động-nhân-sự--quản-lý-sự-kiện-công-tác)
  - [EMP-05: Quản lý Hồ sơ Tài liệu Điện tử An toàn](#emp-05-quản-lý-hồ-sơ-tài-liệu-điện-tử-an-toàn)
- [4. Phân Hệ 3: Quản Lý Hợp Đồng Lao Động (Contract Management)](#4-phân-hệ-3-quản-lý-hợp-đồng-lao-động-contract-management)
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

## 2. Phân Hệ 1: Tuyển Dụng Thông Minh (Smart ATS Recruitment)

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

## 3. Phân Hệ 2: Hồ Sơ & Vòng Đời Nhân Sự (Core HR & Employee Lifecycle)

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

## 4. Phân Hệ 3: Quản Lý Hợp Đồng Lao Động (Contract Management)

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

| Mã User Story | Tiêu đề tóm tắt | Employee | Recruiter | Interviewer / Manager | HR Officer | HR Manager | Super Admin |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **REC-01.1** | Tạo đề xuất tuyển dụng | — | — | **Tạo/Sửa** | — | Xem | Quản trị |
| **REC-01.2** | Duyệt & Đăng tin tuyển | — | **Đăng tin** | — | — | **Phê duyệt** | Quản trị |
| **REC-02.1** | Tiếp nhận CV & Quét an toàn | Nộp CV | **Upload** | — | — | Xem | Giám sát |
| **REC-02.2** | Nhận diện trùng lặp & AI parse | — | **Kiểm tra** | — | — | Xem | — |
| **REC-03.1** | Xem bảng Kanban ATS | — | **Toàn quyền** | Xem vòng phỏng vấn | — | Xem | — |
| **REC-03.2** | Chuyển giai đoạn Kanban | — | **Thực hiện** | — | — | Giám sát | — |
| **REC-04.1** | Xếp lịch phỏng vấn | Xem lịch | **Tạo lịch** | Tham gia | — | Giám sát | — |
| **REC-05.1** | Chấm điểm Scorecard | — | Xem tổng hợp | **Chấm điểm** | — | Quản lý | — |
| **REC-06.1** | Tạo & Duyệt Offer Letter | Phản hồi | **Soạn thảo** | Xem | — | **Phê duyệt** | — |
| **REC-06.2** | Tự động chuyển Onboarding | — | — | — | **Tiếp nhận** | Giám sát | — |
| **EMP-01.1** | Tra cứu danh bạ nhân sự | Xem cơ bản | — | Xem phòng ban | **Toàn quyền** | **Toàn quyền** | Quản trị |
| **EMP-01.2** | Cập nhật hồ sơ cá nhân | **Cập nhật** | — | — | Kiểm tra | Phê duyệt | — |
| **EMP-02.1** | Xem sơ đồ tổ chức | Xem | Xem | Xem | Xem | Xem/Sửa | Quản trị |
| **EMP-03.1** | Theo dõi việc Onboarding | Nhận việc | — | Nhận việc | **Điều phối** | Giám sát | — |
| **EMP-04.1** | Khởi tạo & Duyệt biến động | Xem của mình | — | Đề xuất | Soạn thảo | **Phê duyệt** | Quản trị |
| **EMP-05.1** | Lưu trữ hồ sơ điện tử | Xem của mình | — | — | **Quản lý** | **Quản lý** | Quản trị |
| **CON-01.1** | Soạn thảo hợp đồng | — | — | — | **Soạn thảo** | Phê duyệt | — |
| **CON-01.2** | Ký kết & Kích hoạt HĐ | Xem/Ký | — | — | **Thực hiện** | Giám sát | — |
| **CON-02.1** | Cảnh báo hạn hợp đồng | — | — | Nhận thông báo | **Xử lý** | Giám sát | — |
| **CON-03.1** | Quản lý phụ lục hợp đồng | Xem của mình | — | — | **Soạn thảo** | **Phê duyệt** | — |

---

### 5.2. Ánh xạ Cơ sở Dữ liệu & Use Cases (Traceability Matrix)

| User Story ID | Use Case ID | Bảng Cơ sở Dữ liệu liên quan (`schema.sql`) | API Endpoints (`openapi.yaml`) |
| :--- | :--- | :--- | :--- |
| **REC-01.1/.2** | `UC_REQ_DRAFT`, `UC_REQ_DECIDE` | `job_postings`, `departments`, `positions` | `POST /api/recruitment/jobs`, `PUT /api/recruitment/jobs/{id}/status` |
| **REC-02.1/.2** | `UC_UPLOAD`, `UC_PARSE` | `candidates`, `resumes`, `applications` | `POST /api/recruitment/candidates`, `POST /api/recruitment/resumes` |
| **REC-03.1/.2** | `UC_PIPELINE`, `UC_ADVANCE` | `applications`, `application_stages` | `GET /api/recruitment/pipeline`, `POST /api/recruitment/applications/{id}/advance` |
| **REC-04.1** | `UC_INTERVIEW` | `interviews`, `interview_participants` | `POST /api/recruitment/interviews` |
| **REC-05.1** | `UC_SCORE` | `evaluations`, `evaluation_criteria` | `POST /api/recruitment/evaluations` |
| **REC-06.1/.2** | `UC_OFFER`, `UC_ACCEPT`, `UC_HANDOFF` | `offers`, `employees`, `onboarding_tasks`, `contracts` | `POST /api/recruitment/offers`, `POST /api/recruitment/offers/{id}/accept` |
| **EMP-01.1/.2** | `UC_SEARCH`, `UC_VIEW`, `UC_SELF_CHANGE` | `employees`, `departments`, `positions` | `GET /api/employees`, `GET /api/employees/{id}`, `PATCH /api/employees/{id}/profile` |
| **EMP-02.1** | `UC_ORG`, `UC_ORG_MGMT` | `departments` | `GET /api/organization/chart` |
| **EMP-03.1** | `UC_ONBOARD`, `UC_TASK` | `onboarding_tasks`, `employees` | `GET /api/onboarding/tasks`, `PATCH /api/onboarding/tasks/{id}` |
| **EMP-04.1** | `UC_MOVEMENT`, `UC_MOVEMENT_DECIDE` | `employee_events`, `employees` | `POST /api/employees/{id}/events`, `PUT /api/events/{id}/approve` |
| **EMP-05.1** | `UC_DOCUMENT`, `UC_DOWNLOAD` | `employee_documents`, `employees` | `POST /api/employees/{id}/documents`, `GET /api/documents/{id}/signed-url` |
| **CON-01.1/.2** | `UC_DRAFT`, `UC_APPROVE`, `UC_SIGN` | `contracts`, `employees` | `POST /api/contracts`, `PUT /api/contracts/{id}/activate` |
| **CON-02.1** | `UC_MONITOR`, `UC_ALERT` | `contracts`, `contract_alerts` | `GET /api/contracts/expiring`, `POST /api/contracts/alerts/trigger` |
| **CON-03.1** | `UC_ADDENDUM`, `UC_ADDENDUM_EFFECT` | `contract_addenda`, `contracts`, `employee_events` | `POST /api/contracts/{id}/addenda` |
