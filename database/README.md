# 🗄️ Thiết Kế Cơ Sở Dữ Liệu (Database Architecture & Schema)

> Thư mục chứa cấu trúc lược đồ dữ liệu quan hệ (Relational Schema), sơ đồ thực thể liên kết (ERD), ảnh DBML trực quan, file DDL PostgreSQL và dữ liệu khởi tạo mẫu (Seed Data) cho hệ thống **QLNS**.

> **Trạng thái:** đây là **thiết kế cơ sở dữ liệu và DDL tham chiếu**. Backend đã code-complete và đọc/ghi đúng các bảng này, nhưng schema vẫn được nạp thủ công bằng `psql`: EF Core migration pipeline chưa tồn tại và chưa có integration test nào chạy trên PostgreSQL thật. Việc có file SQL không đồng nghĩa database đã được triển khai hoặc các invariant đã được kiểm chứng.

> [!IMPORTANT]
> Chỉ [`schema.sql`](schema.sql) là canonical — **28 bảng** (baseline v1.2), bao phủ hai phân hệ nghiệp vụ trong phạm vi (Core HR gồm Contracts, và Recruitment) cùng phân hệ định danh Identity & Access (ADM). DDL Attendance & Leave (13 bảng) được giữ ngoài phạm vi tại [docs/deferred/attendance_leave/schema_attendance_leave.sql](../docs/deferred/attendance_leave/schema_attendance_leave.sql). `init.sql` và `postgres_db.sql` đã được đánh dấu **DEPRECATED** trong chính file và không khớp canonical; không sinh migration từ chúng.

> [!NOTE]
> **Phạm vi giao hàng** lấy theo các chức năng lá in đậm dưới Recruitment và Core HR trên bản đồ [`topdown-approach.png`](../topdown-approach.png); nguồn chuẩn là [mục 2 của README gốc](../README.md#2-delivery-scope--seven-pillars-two-selected). Ngoài phạm vi đợt này: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart và Suspension & Return to Work. Việc thu hẹp phạm vi **không đổi DDL**: phần bị loại là màn hình và endpoint, không phải cấu trúc dữ liệu. Bảng `interview_panelists` và các cột `currency` được bổ sung ở v1.1 khi triển khai code — xem [mục 2.6](#26-delta-v11--phát-hiện-khi-triển-khai). Bốn bảng định danh của v1.2 (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`) được thêm khi chuyển xác thực về nội bộ — xem [mục 2.7](#27-delta-v12--đưa-xác-thực-về-nội-bộ).

---

## 📌 Mục Lục

- [1. Sơ Đồ Thực Thể Quan Hệ (ERD)](#1-sơ-đồ-thực-thể-quan-hệ-erd)
- [2. Danh Sách 28 Bảng Canonical](#2-danh-sách-28-bảng-canonical)
- [3. Danh Mục Tệp Lược Đồ Dữ Liệu](#3-danh-mục-tệp-lược-đồ-dữ-liệu)
- [4. Kiểm Tra Thiết Kế Schema](#4-kiểm-tra-thiết-kế-schema-tùy-chọn)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Sơ Đồ Thực Thể Quan Hệ (ERD)

Nguồn ERD hiện hành là sơ đồ Mermaid trong [`database_design.md`](./database_design.md#1-overall-entityrelationship-diagram-mermaid-erd), render từ canonical 23 bảng của v1; bảng `interview_panelists` (v1.1) và bốn bảng định danh của v1.2 (`roles`, `role_permissions`, `user_credentials`, `refresh_tokens`) chưa được vẽ.

> [!NOTE]
> Sơ đồ DBML cũ (`dbml.txt` và ảnh `dbml.png`, theo mô hình 14 bảng) đã được xóa khỏi repository vì không khớp canonical schema. Đừng dựng lại nó song song với Mermaid ERD: hai nguồn sơ đồ sẽ lệch nhau.

---

## 2. Danh Sách 28 Bảng Canonical

Canonical v1.2 (`schema.sql`) gồm 28 bảng thuộc năm nhóm, đánh số liên tục 1–28. Cột **Trạng thái** dùng đúng thang của
[API_REFERENCE §7](../docs/api/API_REFERENCE.md#7-trạng-thái-triển-khai): `Code-complete` nghĩa là backend đã đọc/ghi bảng này
qua repository có transaction kèm audit/outbox và có unit test. **Chưa bảng nào đạt `Implemented`**, vì điều đó đòi hỏi
integration test chạy trên PostgreSQL thật (`tests/Qlns.IntegrationTests` chưa tồn tại) và một EF Core migration có version.

### 2.1. Core HR — Organization & Profile

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 1 | **`departments`** | Danh mục phòng ban, có `parent_department_id` cho phân cấp nhiều tầng và `cost_center`. *Departments & Organizational Hierarchy* trong phạm vi nên quan hệ cha con, quy tắc chống vòng lặp và ràng buộc khi xóa được giữ; chỉ màn hình/endpoint *Organizational Chart* nằm ngoài phạm vi. | Code-complete |
| 2 | **`positions`** | Danh mục chức danh, vị trí công việc và cấp bậc. | Code-complete |
| 3 | **`employees`** | Bảng nhân viên trung tâm: định danh, liên hệ, phòng ban, chức vụ, `manager_id`, trạng thái công tác. `work_email` nullable tới khi kích hoạt; `source_application_id` unique là khóa idempotency của handoff. `status = 'suspended'` là **giá trị reserved, ngoài phạm vi** đợt này — không endpoint nào đặt được. | Code-complete · target of REC-06.2 |

### 2.2. Core HR — Employee Lifecycle

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 4 | **`onboarding_tasks`** | Checklist tiếp nhận nhân sự mới, sinh từ template theo đơn vị/vị trí. | Code-complete |
| 5 | **`employee_events`** | Lịch sử biến động nhân sự với before/after JSON và luồng phê duyệt. `event_type` là `'suspension'` hoặc `'return_to_work'` là **giá trị reserved, ngoài phạm vi** đợt này — không endpoint nào đặt được. | Code-complete |
| 6 | **`employee_documents`** | Hồ sơ, bằng cấp, chứng chỉ; lưu `object_key` private kèm thời hạn lưu trữ. | Code-complete |
| 7 | **`probation_reviews`** | Đánh giá và xác nhận hết thử việc (`confirmed`/`extended`/`terminated`). | Code-complete |
| 8 | **`offboarding_cases`** | Hồ sơ thôi việc: loại chấm dứt, ngày làm việc cuối, người nhận bàn giao, chốt công nợ. | Code-complete |
| 9 | **`offboarding_tasks`** | Checklist bàn giao và thu hồi tài sản/tài khoản, có cờ task chặn. | Code-complete |

### 2.3. Core HR — Contracts

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 10 | **`contracts`** | Hợp đồng lao động: loại, mức lương và `currency` (v1.1), ngày hiệu lực/hết hạn, trạng thái, bản ký số hóa. | Code-complete |
| 11 | **`contract_addenda`** | Phụ lục hợp đồng với before/after terms và lịch sử phê duyệt. `version` là phiên bản đồng thời (ETag) từ v1.1; phụ lục được định danh bằng `addendum_number`. | Code-complete |

### 2.4. Recruitment (ATS)

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 12 | **`job_postings`** | Đề xuất và tin tuyển dụng, ngân sách lương, số lượng cần tuyển. | Code-complete |
| 13 | **`candidates`** | Hồ sơ ứng viên, email/điện thoại đã chuẩn hóa để chống trùng, mốc đồng ý xử lý dữ liệu. | Code-complete |
| 14 | **`resumes`** | Tệp CV kèm vòng đời intake (`intake_id`, `intake_status`), trạng thái quét mã độc, dữ liệu bóc tách và độ tin cậy. Tạo trước khi có `candidates`. | Code-complete |
| 15 | **`applications`** | Đơn ứng tuyển liên kết ứng viên với tin tuyển dụng và giai đoạn pipeline. | Code-complete |
| 16 | **`application_stage_events`** | Lịch sử chuyển giai đoạn, chống ghi đè bằng `application_version`. | Code-complete |
| 17 | **`interviews`** | Lịch phỏng vấn kèm múi giờ, người phỏng vấn chính (`interviewer_user_id`) và trạng thái. | Code-complete |
| 18 | **`interview_panelists`** *(v1.1)* | Hội đồng phỏng vấn — một dòng cho mỗi `interviewerUserIds` của `InterviewWrite`; `interviewer_user_id` của `interviews` luôn có mặt trong bảng này. | Code-complete |
| 19 | **`evaluations`** | Scorecard chấm điểm với thang 0–5 bước 0.5 và cơ chế unlock có lý do. | Code-complete |
| 20 | **`offers`** | Thư mời nhận việc; index partial đảm bảo mỗi đơn chỉ có một offer đang mở. Cột `currency` (v1.1) khớp `OfferWrite.currency`. | Code-complete |

### 2.5. Platform — định danh, phân quyền, audit, outbox

Tám bảng này **thuộc canonical schema**. Sáu bảng đầu mang dữ liệu định danh, thông tin đăng nhập và phân quyền của phân hệ Identity & Access (ADM) — **có API quản trị** tại `/api/v1/auth/*` và `/api/v1/admin/*`. Hai bảng cuối là cơ chế xuyên suốt bắt buộc và **không** có API: không endpoint tra cứu audit log, không endpoint xem/retry delivery.

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 21 | **`users`** | Dữ liệu định danh của ứng dụng. `external_subject` mang tiền tố `local|` cho tài khoản do QLNS cấp, để dành không gian tên riêng nếu sau này federation với IdP ngoài. Quản trị qua `/api/v1/admin/users`. | Code-complete |
| 22 | **`user_credentials`** | Mật khẩu băm PBKDF2-HMAC-SHA512 kèm tham số trong chính chuỗi hash, cờ buộc đổi mật khẩu, bộ đếm sai và mốc hết khoá. Một dòng cho mỗi tài khoản đăng nhập được bằng mật khẩu. | Code-complete |
| 23 | **`user_roles`** | Gán vai trò kèm data scope (`self`/`department`/`organization`) — nguồn cho việc kiểm tra quyền phía server, bắt buộc trên mọi request. Quản trị qua `PUT /api/v1/admin/users/{userId}/roles`. | Code-complete |
| 24 | **`roles`** | Danh mục vai trò. Dữ liệu tham chiếu, nạp từ `seed_roles.sql`; `user_roles.role_code` tham chiếu tới đây. | Code-complete |
| 25 | **`role_permissions`** | Ma trận vai trò → permission. Đăng nhập resolve permission bằng `user_roles ⋈ role_permissions`, nên đổi ma trận không cần build lại code. Chỉ đọc qua API. | Code-complete |
| 26 | **`refresh_tokens`** | Refresh token dùng một lần: chỉ lưu bản băm SHA-256, ghi nhận token kế nhiệm và lý do thu hồi. Trình lại token đã thu hồi ⇒ thu hồi cả họ token của tài khoản. | Code-complete |
| 27 | **`audit_logs`** | Nhật ký hành động với before/after và `correlation_id`. **Cơ chế bắt buộc**: ghi cùng transaction với thay đổi nghiệp vụ — kể cả lần đăng nhập thất bại (`result = 'rejected'`). Không có endpoint tra cứu trong đợt này. | Code-complete |
| 28 | **`outbox_messages`** | Transactional outbox cho email và lịch. **Cơ chế bắt buộc**: ghi cùng transaction nghiệp vụ. Không có endpoint xem/retry delivery trong đợt này. | Code-complete |

### 2.6. Delta v1.1 — phát hiện khi triển khai

Bốn khác biệt giữa contract OpenAPI và DDL v1 chỉ lộ ra khi viết code. Chúng được sửa trong chính `schema.sql` (script tạo mới) thay vì thêm `ALTER` rời, và phải được mang sang EF Core migration đầu tiên:

| Delta | Lý do |
|---|---|
| `offers.currency char(3) NOT NULL DEFAULT 'VND'` | `OfferWrite.currency` có trong contract nhưng không có cột lưu. |
| `contracts.currency char(3) NOT NULL DEFAULT 'VND'` | `ContractWrite.currency` tương tự. |
| Bảng mới `interview_panelists(interview_id, user_id)` | `InterviewWrite.interviewerUserIds` là mảng; `interviews.interviewer_user_id` chỉ lưu một người. Người đầu tiên của hội đồng được ghi vào `interviewer_user_id` làm lead; `evaluations` chấm theo từng panelist. |
| `contract_addenda.version bigint` là **phiên bản đồng thời**, bỏ `ux_contract_addendum_version (contract_id, version)` | Contract dùng `version` làm ETag/`If-Match` cho phụ lục; một unique theo `(contract_id, version)` sẽ va chạm khi hai phụ lục cùng hợp đồng được sửa. Thứ tự/định danh phụ lục dựa vào `addendum_number` (UNIQUE) và trạng thái `superseded`. |

### 2.7. Delta v1.2 — đưa xác thực về nội bộ

Quyết định chuyển đăng nhập và quản trị tài khoản từ Identity Provider bên ngoài về chính hệ thống kéo theo bốn bảng mới và một khoá ngoại:

| Delta | Lý do |
|---|---|
| Bảng mới `roles(code, name, description, is_assignable)` | Cần danh mục vai trò để API `GET /api/v1/admin/roles` trả về và để validate `role_code` khi cấp quyền. |
| Bảng mới `role_permissions(role_code, permission)` | Ma trận vai trò → permission trước đây chỉ tồn tại trong code (`DevelopmentAuthentication` personas). Đưa vào dữ liệu để đăng nhập resolve permission bằng SQL, và để đổi ma trận không phải build lại. |
| Bảng mới `user_credentials` | `users` không có chỗ lưu mật khẩu; tách bảng 1–1 giữ nguyên `users` là dữ liệu định danh và cho phép tồn tại tài khoản chưa có mật khẩu nội bộ. |
| Bảng mới `refresh_tokens` | Phiên dài hạn cần một tạo tác **thu hồi được**, điều mà access token stateless không làm được. Chỉ lưu băm SHA-256 nên dump bảng này không replay được. |
| `user_roles.role_code` giờ `REFERENCES roles(code)` | Trước đây là chuỗi tự do; một mã sai chính tả sẽ âm thầm không cấp quyền nào. |

Hai lưu ý khi triển khai: (1) `seed_roles.sql` là **dữ liệu tham chiếu bắt buộc ở mọi môi trường**, phải chạy ngay sau `schema.sql`; (2) mọi tài khoản trong `seed_dev.sql` dùng mật khẩu phát triển `Qlns@2026` và **không được** mang sang môi trường dùng chung.

---

## 3. Danh Mục Tệp Lược Đồ Dữ Liệu

> [!IMPORTANT]
> [`schema.sql`](schema.sql) là **canonical schema contract cho baseline v1**. `init.sql` và `postgres_db.sql` là artifact prototype/legacy để đối chiếu và không được dùng làm nguồn tạo migration mới. Khi backend có EF Core migration được phê duyệt, migration trở thành nguồn triển khai và `schema.sql` phải được kiểm tra drift trong CI.

| Tệp | Mô Tả Chi Tiết | Liên Kết |
| :--- | :--- | :--- |
| **`database_design.md`** | Tài liệu đặc tả kỹ thuật chi tiết từng trường, kiểu dữ liệu, ràng buộc khóa chính/khóa ngoại và Mermaid ERD. | [Xem database_design.md](./database_design.md) |
| **`schema.sql`** | **Canonical schema contract v1.2 — 28 bảng.** Nguồn chuẩn duy nhất cho kiểu dữ liệu, constraint, index và exclusion constraint. | [Xem schema.sql](./schema.sql) |
| **`seed_roles.sql`** | **Dữ liệu tham chiếu bắt buộc:** danh mục 8 vai trò và ma trận vai trò → permission. Chạy sau `schema.sql`, trước `seed_dev.sql`; idempotent (`ON CONFLICT`). | [Xem seed_roles.sql](./seed_roles.sql) |
| **`init.sql`** | ⚠️ **DEPRECATED.** Script seed/DDL cũ, chưa có bảng định danh/phân quyền, audit, outbox và các bảng lifecycle. | [Xem init.sql](./init.sql) |
| **`postgres_db.sql`** | ⚠️ **DEPRECATED.** DDL legacy; các bảng chấm công/nghỉ phép ở đây nằm ngoài phạm vi và không khớp bản thiết kế deferred. | [Xem postgres_db.sql](./postgres_db.sql) |
| **`seed_dev.sql`** | Dữ liệu mẫu **chỉ dùng cho môi trường phát triển**: 8 user (đều đăng nhập bằng mật khẩu `Qlns@2026`), 6 phòng ban, 6 chức danh, 7 nhân viên, 5 task onboarding, 2 requisition, 2 ứng viên/đơn ứng tuyển, 1 phỏng vấn đã chấm, 3 hợp đồng và 1 phiếu thử việc. Không thuộc hợp đồng schema và không được chạy trên môi trường dùng chung. | [Xem seed_dev.sql](./seed_dev.sql) |

---

## 4. Kiểm Tra Thiết Kế Schema (Tùy chọn)

Project chưa cung cấp môi trường database chạy sẵn. Nếu đã có một PostgreSQL test do bạn tự quản lý, có thể nạp DDL tham chiếu bằng `psql`:

```bash
psql -v ON_ERROR_STOP=1 -d <test_database> -f database/schema.sql

# Sau khi kết nối vào database test
\dt
SELECT id, employee_code, first_name, last_name, work_email, status FROM employees;
```

Không dùng thông tin xác thực mặc định hoặc dữ liệu mẫu này cho production. Khi bắt đầu backend, cần chọn công cụ migration có version, tách seed theo môi trường và bổ sung kiểm thử constraint/rollback.

---

[⬅️ Trở về Trang Chủ Tài Liệu](../README.md)
