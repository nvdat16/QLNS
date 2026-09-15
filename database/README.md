# 🗄️ Thiết Kế Cơ Sở Dữ Liệu (Database Architecture & Schema)

> Thư mục chứa cấu trúc lược đồ dữ liệu quan hệ (Relational Schema), sơ đồ thực thể liên kết (ERD), ảnh DBML trực quan, file DDL PostgreSQL và dữ liệu khởi tạo mẫu (Seed Data) cho hệ thống **QLNS**.

> **Trạng thái:** đây là **thiết kế cơ sở dữ liệu và DDL tham chiếu**. Chưa có Backend API, migration pipeline hay PostgreSQL runtime được cấu hình trong project. Việc có file SQL không đồng nghĩa database đã được triển khai hoặc các luồng nghiệp vụ đã hoạt động.

---

## 📌 Mục Lục

- [1. Sơ Đồ Thực Thể Quan Hệ (Visual DBML & ERD)](#1-sơ-đồ-thực-thể-quan-hệ-visual-dbml--erd)
- [2. Danh Sách 14 Bảng Dữ Liệu](#2-danh-sách-14-bảng-dữ-liệu)
- [3. Danh Mục Tệp Lược Đồ Dữ Liệu](#3-danh-mục-tệp-lược-đồ-dữ-liệu)
- [4. Kiểm Tra Thiết Kế Schema](#4-kiểm-tra-thiết-kế-schema-tùy-chọn)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Sơ Đồ Thực Thể Quan Hệ (Visual DBML & ERD)

### 📷 Sơ đồ Cấu trúc Bảng & Khóa ngoại (DBML Schema Diagram)
Dưới đây là sơ đồ chi tiết các bảng, trường thông tin và các mối quan hệ khóa ngoại (1-N) giữa các thực thể trong hệ thống:

![Sơ đồ Cấu trúc Bảng DBML](dbml.png)

---

## 2. Danh Sách 14 Bảng Dữ Liệu

Hệ thống được chuẩn hóa thành 14 bảng quan hệ, phân thành 2 nhóm nghiệp vụ cốt lõi:

| STT | Tên Bảng | Phân Nhóm | Chức Năng Chính |
| :---: | :--- | :--- | :--- |
| 1 | **`departments`** | Core HR | Danh mục phòng ban, khối chuyên môn trong doanh nghiệp. |
| 2 | **`positions`** | Core HR | Danh mục chức danh, vị trí công việc và cấp bậc. |
| 3 | **`employees`** | Core HR | Bảng dữ liệu nhân viên trung tâm (Thông tin cá nhân, liên hệ, phòng ban, chức vụ, trạng thái). |
| 4 | **`contracts`** | Core HR | Quản lý hợp đồng lao động (Loại HĐ, mức lương đóng BH, ngày hiệu lực/hết hạn, trạng thái). |
| 5 | **`onboarding_tasks`** | Core HR | Danh mục checklist các đầu việc tiếp nhận nhân sự mới. |
| 6 | **`employee_documents`**| Core HR | Lưu trữ hồ sơ, bằng cấp, chứng chỉ, scan hợp đồng của nhân viên. |
| 7 | **`employee_events`** | Core HR | Lịch sử biến động nhân sự (Thử việc -> Chính thức, thăng chức, luân chuyển phòng ban, khen thưởng/kỷ luật). |
| 8 | **`job_postings`** | Recruitment | Tin tuyển dụng, yêu cầu vị trí, mức lương, số lượng cần tuyển. |
| 9 | **`candidates`** | Recruitment | Hồ sơ ứng viên (Họ tên, email, điện thoại, nguồn ứng tuyển). |
| 10 | **`resumes`** | Recruitment | Tệp CV ứng viên, đường dẫn file và dữ liệu trích xuất kỹ năng. |
| 11 | **`applications`** | Recruitment | Đơn ứng tuyển liên kết Ứng viên với Tin tuyển dụng, trạng thái vòng tuyển dụng (Applied, Screening, Interview, Offered, Hired, Rejected). |
| 12 | **`interviews`** | Recruitment | Lịch hẹn phỏng vấn (Thời gian, hình thức trực tiếp/online, vòng phỏng vấn). |
| 13 | **`evaluations`** | Recruitment | Phiếu chấm điểm và đánh giá của hội đồng phỏng vấn (Scorecard). |
| 14 | **`offers`** | Recruitment | Thư đề nghị nhận việc (Mức lương, ngày bắt đầu dự kiến, hạn phản hồi, trạng thái chấp nhận). |

---

## 3. Danh Mục Tệp Lược Đồ Dữ Liệu

| Tệp | Mô Tả Chi Tiết | Liên Kết |
| :--- | :--- | :--- |
| **`database_design.md`** | Tài liệu đặc tả kỹ thuật chi tiết từng trường, kiểu dữ liệu, ràng buộc khóa chính/khóa ngoại và Mermaid ERD. | [Xem database_design.md](./database_design.md) |
| **`init.sql`** | Script PostgreSQL tham chiếu để tạo schema và dữ liệu mẫu khi cần kiểm tra thiết kế. | [Xem init.sql](./init.sql) |
| **`postgres_db.sql`** | File tham chiếu DDL schema chuẩn. | [Xem postgres_db.sql](./postgres_db.sql) |
| **`dbml.txt`** | Định nghĩa mã nguồn chuẩn DBML dùng để render sơ đồ trên dbdocs / dbdiagram.io. | [Xem dbml.txt](./dbml.txt) |

---

## 4. Kiểm Tra Thiết Kế Schema (Tùy chọn)

Project chưa cung cấp môi trường database chạy sẵn. Nếu đã có một PostgreSQL test do bạn tự quản lý, có thể nạp DDL tham chiếu bằng `psql`:

```bash
psql -v ON_ERROR_STOP=1 -d <test_database> -f database/init.sql

# Sau khi kết nối vào database test
\dt
SELECT id, employee_code, first_name, last_name, email, status FROM employees;
```

Không dùng thông tin xác thực mặc định hoặc dữ liệu mẫu này cho production. Khi bắt đầu backend, cần chọn công cụ migration có version, tách seed theo môi trường và bổ sung kiểm thử constraint/rollback.

---

[⬅️ Trở về Trang Chủ Tài Liệu](../README.md)
