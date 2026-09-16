# 🗄️ Thiết Kế Cơ Sở Dữ Liệu (Database Architecture & Schema)

> Thư mục chứa cấu trúc lược đồ dữ liệu quan hệ (Relational Schema), sơ đồ thực thể liên kết (ERD), ảnh DBML trực quan, file DDL PostgreSQL và dữ liệu khởi tạo mẫu (Seed Data) cho hệ thống **QLNS**.

> **Trạng thái:** đây là **thiết kế cơ sở dữ liệu và DDL tham chiếu**. Source backend REC-03.2 đã tồn tại; EF Core migration pipeline và PostgreSQL runtime chưa được cấu hình/xác minh. Việc có file SQL không đồng nghĩa database đã được triển khai hoặc các luồng nghiệp vụ đã hoạt động.

> [!IMPORTANT]
> Chỉ [`schema.sql`](schema.sql) là canonical — **36 bảng**, bao phủ ba phân hệ triển khai trước: Core HR (gồm Contracts), Recruitment và Attendance & Leave. `init.sql`, `postgres_db.sql` và `dbml.txt` đã được đánh dấu **DEPRECATED** trong chính file và không khớp canonical; không sinh migration từ chúng. `dbml.png` render từ `dbml.txt` nên cũng là sơ đồ cũ.

---

## 📌 Mục Lục

- [1. Sơ Đồ Thực Thể Quan Hệ (Visual DBML & ERD)](#1-sơ-đồ-thực-thể-quan-hệ-visual-dbml--erd)
- [2. Danh Sách 36 Bảng Canonical](#2-danh-sách-36-bảng-canonical)
- [3. Danh Mục Tệp Lược Đồ Dữ Liệu](#3-danh-mục-tệp-lược-đồ-dữ-liệu)
- [4. Kiểm Tra Thiết Kế Schema](#4-kiểm-tra-thiết-kế-schema-tùy-chọn)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Sơ Đồ Thực Thể Quan Hệ (Visual DBML & ERD)

### 📷 Sơ đồ Cấu trúc Bảng & Khóa ngoại (DBML Schema Diagram)

> [!WARNING]
> Ảnh dưới đây render từ `dbml.txt` theo mô hình **14 bảng cũ** và chưa phản ánh canonical 36 bảng. Nguồn ERD hiện hành là Mermaid trong [`database_design.md`](./database_design.md#1-overall-entityrelationship-diagram-mermaid-erd).

![Sơ đồ Cấu trúc Bảng DBML](dbml.png)

---

## 2. Danh Sách 36 Bảng Canonical

Canonical v1 (`schema.sql`) gồm 36 bảng thuộc bảy nhóm. Cột **Trạng thái** cho biết mức độ sẵn sàng triển khai, không phải mức độ tồn tại của file SQL.

### 2.1. Core HR — Organization & Profile

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 1 | **`departments`** | Danh mục phòng ban, có `parent_department_id` cho cây tổ chức nhiều cấp và `cost_center`. | Proposed |
| 2 | **`positions`** | Danh mục chức danh, vị trí công việc và cấp bậc. | Proposed |
| 3 | **`employees`** | Bảng nhân viên trung tâm: định danh, liên hệ, phòng ban, chức vụ, `manager_id`, trạng thái công tác. | Proposed |

### 2.2. Core HR — Employee Lifecycle

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 4 | **`onboarding_tasks`** | Checklist tiếp nhận nhân sự mới, sinh từ template theo đơn vị/vị trí. | Proposed |
| 5 | **`employee_events`** | Lịch sử biến động nhân sự với before/after JSON và luồng phê duyệt. | Proposed |
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
| 14 | **`resumes`** | Tệp CV, trạng thái quét mã độc và dữ liệu bóc tách. | Proposed |
| 15 | **`applications`** | Đơn ứng tuyển liên kết ứng viên với tin tuyển dụng và giai đoạn pipeline. | **Implemented** (read + advance) |
| 16 | **`application_stage_events`** | Lịch sử chuyển giai đoạn, chống ghi đè bằng `application_version`. | **Implemented** |
| 17 | **`interviews`** | Lịch phỏng vấn kèm múi giờ, người phỏng vấn và trạng thái. | Proposed |
| 18 | **`evaluations`** | Scorecard chấm điểm với thang 0–5 bước 0.5 và cơ chế unlock có lý do. | Proposed |
| 19 | **`offers`** | Thư mời nhận việc; index partial đảm bảo mỗi đơn chỉ có một offer đang mở. | Proposed |

### 2.5. Attendance

> Toàn bộ nhóm này phụ thuộc policy chưa được HR/Legal phê duyệt — xem [Open Decisions](../docs/open_decisions_attendance_leave.md).

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 20 | **`attendance_policies`** | Phiên bản chính sách tính công: ân hạn, làm tròn, ngưỡng vắng, khung giờ đêm. | Proposed · policy pending |
| 21 | **`holidays`** | Lịch nghỉ lễ theo `calendar_code`, cờ hưởng lương và hệ số làm việc ngày lễ. | Proposed · policy pending |
| 22 | **`work_shifts`** | Định nghĩa ca làm việc, số phút công chuẩn, hệ số và cờ ca qua đêm. | Proposed |
| 23 | **`work_schedule_assignments`** | Phân ca theo ngày; unique đảm bảo một nhân viên một ca mỗi ngày. | Proposed |
| 24 | **`attendance_events`** | Bản ghi check-in/check-out thô, append-only, idempotent theo thiết bị. | Proposed |
| 25 | **`attendance_daily_records`** | Bảng công theo ngày (dữ liệu dẫn xuất), luôn ghi kèm `policy_version`. | Proposed · policy pending |
| 26 | **`attendance_corrections`** | Đề nghị và phê duyệt hiệu chỉnh công, một đơn `pending` mỗi ngày. | Proposed |
| 27 | **`overtime_requests`** | Đăng ký và duyệt tăng ca theo loại OT và hệ số; chống trùng khoảng thời gian. | Proposed · policy pending |
| 28 | **`timesheet_periods`** | Kỳ công, soát–duyệt–khóa kỳ và bàn giao sang Payroll; các kỳ không giao nhau. | Proposed |

### 2.6. Leave

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 29 | **`leave_types`** | Loại phép và chính sách: hưởng lương, nửa ngày, số ngày báo trước, số cấp duyệt. | Proposed · policy pending |
| 30 | **`leave_balances`** | Quỹ phép theo năm; `available_units` là generated column, client không tự tính. | Proposed · policy pending |
| 31 | **`leave_requests`** | Đơn nghỉ phép; exclusion constraint chặn trùng đơn ở tầng database. | Proposed |
| 32 | **`leave_request_decisions`** | Lịch sử phê duyệt nhiều cấp, lưu `request_version` tại thời điểm quyết định. | Proposed |

### 2.7. Platform — Identity, Audit, Integration

| STT | Tên Bảng | Chức Năng Chính | Trạng thái |
| :---: | :--- | :--- | :--- |
| 33 | **`users`** | Danh tính ứng dụng liên kết IdP qua `external_subject`. | Proposed |
| 34 | **`user_roles`** | Gán vai trò kèm data scope (`self`/`department`/`organization`). | Proposed |
| 35 | **`audit_logs`** | Nhật ký hành động với before/after và `correlation_id`. | **Implemented** (advance stage) |
| 36 | **`outbox_messages`** | Transactional outbox cho email, notification và integration. | Proposed |

---

## 3. Danh Mục Tệp Lược Đồ Dữ Liệu

> [!IMPORTANT]
> [`schema.sql`](schema.sql) là **canonical schema contract cho baseline v1**. `init.sql`, `postgres_db.sql` và `dbml.txt` là artifact prototype/legacy để đối chiếu và không được dùng làm nguồn tạo migration mới. Khi backend có EF Core migration được phê duyệt, migration trở thành nguồn triển khai và `schema.sql` phải được kiểm tra drift trong CI.

| Tệp | Mô Tả Chi Tiết | Liên Kết |
| :--- | :--- | :--- |
| **`database_design.md`** | Tài liệu đặc tả kỹ thuật chi tiết từng trường, kiểu dữ liệu, ràng buộc khóa chính/khóa ngoại và Mermaid ERD. | [Xem database_design.md](./database_design.md) |
| **`schema.sql`** | **Canonical schema contract v1 — 36 bảng.** Nguồn chuẩn duy nhất cho kiểu dữ liệu, constraint, index và exclusion constraint. | [Xem schema.sql](./schema.sql) |
| **`init.sql`** | ⚠️ **DEPRECATED.** Script seed/DDL cũ, chưa có identity/RBAC, audit, outbox và Attendance & Leave. | [Xem init.sql](./init.sql) |
| **`postgres_db.sql`** | ⚠️ **DEPRECATED.** DDL legacy; các bảng chấm công/nghỉ phép ở đây đã bị canonical thay thế. | [Xem postgres_db.sql](./postgres_db.sql) |
| **`dbml.txt`** | ⚠️ **DEPRECATED.** Nguồn render `dbml.png` theo mô hình 14 bảng cũ, không khớp canonical. | [Xem dbml.txt](./dbml.txt) |

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
