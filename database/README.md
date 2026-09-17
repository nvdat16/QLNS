# 🗄️ Thiết Kế Cơ Sở Dữ Liệu (Database Architecture & Schema)

> Thư mục chứa cấu trúc lược đồ dữ liệu quan hệ (Relational Schema), sơ đồ thực thể liên kết (ERD), ảnh DBML trực quan, file DDL PostgreSQL và dữ liệu khởi tạo mẫu (Seed Data) cho hệ thống **QLNS**.

> **Trạng thái:** đây là **thiết kế cơ sở dữ liệu và DDL tham chiếu**. Source backend hiện chỉ là skeleton cấu trúc; EF Core migration pipeline và PostgreSQL runtime chưa được cấu hình/xác minh. Việc có file SQL không đồng nghĩa database đã được triển khai hoặc các luồng nghiệp vụ đã hoạt động.

> [!IMPORTANT]
> Chỉ [`schema.sql`](schema.sql) là canonical — **23 bảng**, bao phủ hai phân hệ trong phạm vi: Core HR (gồm Contracts) và Recruitment. DDL Attendance & Leave (13 bảng) được giữ ngoài phạm vi tại [docs/deferred/attendance_leave/schema_attendance_leave.sql](../docs/deferred/attendance_leave/schema_attendance_leave.sql). `init.sql` và `postgres_db.sql` đã được đánh dấu **DEPRECATED** trong chính file và không khớp canonical; không sinh migration từ chúng.

> [!NOTE]
> **Phạm vi giao hàng** lấy theo các chức năng lá in đậm dưới Recruitment và Core HR trên bản đồ [`topdown-approach.png`](../topdown-approach.png); nguồn chuẩn là [mục 2 của README gốc](../README.md#2-delivery-scope--seven-pillars-two-selected). Ngoài phạm vi đợt này: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart và Suspension & Return to Work. Việc thu hẹp phạm vi **không đổi DDL**: số bảng canonical vẫn là **23**, vì phần bị loại là màn hình và endpoint, không phải cấu trúc dữ liệu.

---

## 📌 Mục Lục

- [1. Sơ Đồ Thực Thể Quan Hệ (ERD)](#1-sơ-đồ-thực-thể-quan-hệ-erd)
- [2. Danh Sách 23 Bảng Canonical](#2-danh-sách-23-bảng-canonical)
- [3. Danh Mục Tệp Lược Đồ Dữ Liệu](#3-danh-mục-tệp-lược-đồ-dữ-liệu)
- [4. Kiểm Tra Thiết Kế Schema](#4-kiểm-tra-thiết-kế-schema-tùy-chọn)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Sơ Đồ Thực Thể Quan Hệ (ERD)

Nguồn ERD hiện hành là sơ đồ Mermaid trong [`database_design.md`](./database_design.md#1-overall-entityrelationship-diagram-mermaid-erd), render trực tiếp từ canonical 23 bảng.

> [!NOTE]
> Sơ đồ DBML cũ (`dbml.txt` và ảnh `dbml.png`, theo mô hình 14 bảng) đã được xóa khỏi repository vì không khớp canonical schema. Đừng dựng lại nó song song với Mermaid ERD: hai nguồn sơ đồ sẽ lệch nhau.

---

## 2. Danh Sách 23 Bảng Canonical

Canonical v1 (`schema.sql`) gồm 23 bảng thuộc năm nhóm. Cột **Trạng thái** cho biết mức độ sẵn sàng triển khai, không phải mức độ tồn tại của file SQL.

### 2.1. Core HR — Organization & Profile

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 1 | **`departments`** | Danh mục phòng ban, có `parent_department_id` cho phân cấp nhiều tầng và `cost_center`. *Departments & Organizational Hierarchy* trong phạm vi nên quan hệ cha con, quy tắc chống vòng lặp và ràng buộc khi xóa được giữ; chỉ màn hình/endpoint *Organizational Chart* nằm ngoài phạm vi. | Proposed |
| 2 | **`positions`** | Danh mục chức danh, vị trí công việc và cấp bậc. | Proposed |
| 3 | **`employees`** | Bảng nhân viên trung tâm: định danh, liên hệ, phòng ban, chức vụ, `manager_id`, trạng thái công tác. `work_email` nullable tới khi kích hoạt; `source_application_id` unique là khóa idempotency của handoff. `status = 'suspended'` là **giá trị reserved, ngoài phạm vi** đợt này — không endpoint nào đặt được. | Proposed · target of REC-06.2 |

### 2.2. Core HR — Employee Lifecycle

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 4 | **`onboarding_tasks`** | Checklist tiếp nhận nhân sự mới, sinh từ template theo đơn vị/vị trí. | Proposed |
| 5 | **`employee_events`** | Lịch sử biến động nhân sự với before/after JSON và luồng phê duyệt. `event_type` là `'suspension'` hoặc `'return_to_work'` là **giá trị reserved, ngoài phạm vi** đợt này — không endpoint nào đặt được. | Proposed |
| 6 | **`employee_documents`** | Hồ sơ, bằng cấp, chứng chỉ; lưu `object_key` private kèm thời hạn lưu trữ. | Proposed |
| 7 | **`probation_reviews`** | Đánh giá và xác nhận hết thử việc (`confirmed`/`extended`/`terminated`). | Proposed |
| 8 | **`offboarding_cases`** | Hồ sơ thôi việc: loại chấm dứt, ngày làm việc cuối, người nhận bàn giao, chốt công nợ. | Proposed |
| 9 | **`offboarding_tasks`** | Checklist bàn giao và thu hồi tài sản/tài khoản, có cờ task chặn. | Proposed |

### 2.3. Core HR — Contracts

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 10 | **`contracts`** | Hợp đồng lao động: loại, mức lương, ngày hiệu lực/hết hạn, trạng thái, bản ký số hóa. | Proposed |
| 11 | **`contract_addenda`** | Phụ lục hợp đồng với before/after terms, phiên bản và lịch sử phê duyệt. | Proposed |

### 2.4. Recruitment (ATS)

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 12 | **`job_postings`** | Đề xuất và tin tuyển dụng, ngân sách lương, số lượng cần tuyển. | Proposed |
| 13 | **`candidates`** | Hồ sơ ứng viên, email/điện thoại đã chuẩn hóa để chống trùng, mốc đồng ý xử lý dữ liệu. | Proposed |
| 14 | **`resumes`** | Tệp CV kèm vòng đời intake (`intake_id`, `intake_status`), trạng thái quét mã độc, dữ liệu bóc tách và độ tin cậy. Tạo trước khi có `candidates`. | Proposed |
| 15 | **`applications`** | Đơn ứng tuyển liên kết ứng viên với tin tuyển dụng và giai đoạn pipeline. | Proposed |
| 16 | **`application_stage_events`** | Lịch sử chuyển giai đoạn, chống ghi đè bằng `application_version`. | Proposed |
| 17 | **`interviews`** | Lịch phỏng vấn kèm múi giờ, người phỏng vấn và trạng thái. | Proposed |
| 18 | **`evaluations`** | Scorecard chấm điểm với thang 0–5 bước 0.5 và cơ chế unlock có lý do. | Proposed |
| 19 | **`offers`** | Thư mời nhận việc; index partial đảm bảo mỗi đơn chỉ có một offer đang mở. | Proposed |

### 2.5. Platform — định danh, audit, outbox

Bốn bảng này **vẫn thuộc canonical schema**. Chúng mang dữ liệu định danh và hai cơ chế xuyên suốt bắt buộc, chứ không phải một phân hệ nghiệp vụ. Đợt giao hàng này **không có API quản trị** cho chúng: không endpoint quản lý user/role/role-grant, không endpoint tra cứu audit log, không endpoint xem và retry delivery. Việc cấp tài khoản và vai trò do **Identity Provider bên ngoài** đảm nhiệm.

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 20 | **`users`** | Dữ liệu định danh của ứng dụng, liên kết IdP qua `external_subject`. Không có API quản lý tài khoản trong đợt này. | Proposed |
| 21 | **`user_roles`** | Gán vai trò kèm data scope (`self`/`department`/`organization`) — nguồn cho việc kiểm tra quyền phía server, vẫn bắt buộc trên mọi request. Không có API cấp vai trò trong đợt này. | Proposed |
| 22 | **`audit_logs`** | Nhật ký hành động với before/after và `correlation_id`. **Cơ chế bắt buộc**: ghi cùng transaction với thay đổi nghiệp vụ. Không có endpoint tra cứu trong đợt này. | Proposed |
| 23 | **`outbox_messages`** | Transactional outbox cho email và lịch. **Cơ chế bắt buộc**: ghi cùng transaction nghiệp vụ. Không có endpoint xem/retry delivery trong đợt này. | Proposed |

---

## 3. Danh Mục Tệp Lược Đồ Dữ Liệu

> [!IMPORTANT]
> [`schema.sql`](schema.sql) là **canonical schema contract cho baseline v1**. `init.sql` và `postgres_db.sql` là artifact prototype/legacy để đối chiếu và không được dùng làm nguồn tạo migration mới. Khi backend có EF Core migration được phê duyệt, migration trở thành nguồn triển khai và `schema.sql` phải được kiểm tra drift trong CI.

| Tệp | Mô Tả Chi Tiết | Liên Kết |
| :--- | :--- | :--- |
| **`database_design.md`** | Tài liệu đặc tả kỹ thuật chi tiết từng trường, kiểu dữ liệu, ràng buộc khóa chính/khóa ngoại và Mermaid ERD. | [Xem database_design.md](./database_design.md) |
| **`schema.sql`** | **Canonical schema contract v1 — 23 bảng.** Nguồn chuẩn duy nhất cho kiểu dữ liệu, constraint, index và exclusion constraint. | [Xem schema.sql](./schema.sql) |
| **`init.sql`** | ⚠️ **DEPRECATED.** Script seed/DDL cũ, chưa có bảng định danh/phân quyền, audit, outbox và các bảng lifecycle. | [Xem init.sql](./init.sql) |
| **`postgres_db.sql`** | ⚠️ **DEPRECATED.** DDL legacy; các bảng chấm công/nghỉ phép ở đây nằm ngoài phạm vi và không khớp bản thiết kế deferred. | [Xem postgres_db.sql](./postgres_db.sql) |
| **`seed_dev.sql`** | Dữ liệu mẫu **chỉ dùng cho môi trường phát triển**: 6 user, 6 phòng ban, 6 chức danh, 7 nhân viên, 5 task onboarding. Không thuộc hợp đồng schema và không được chạy trên môi trường dùng chung. | [Xem seed_dev.sql](./seed_dev.sql) |

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
