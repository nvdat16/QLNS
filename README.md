# Human Resource Management System (QLNS / HRMS)

> A comprehensive, modern **Human Resource Management System (HRMS)** & **Applicant Tracking System (ATS)** designed with an enterprise top-down architecture—streamlining the entire employee lifecycle from job requisition, CV parsing, interview evaluation, and onboarding handoff to personnel records, labor contracts, organization charts, attendance, shift scheduling, and leave management.

---

## Bảng Điều Hướng Tài Liệu Chi Tiết (Documentation Hub)

Mỗi thư mục trong hệ thống đều có file `README.md` riêng biệt đặc tả chi tiết kiến trúc, hình ảnh và hướng dẫn thực thi:

| Phân Hệ / Thư Mục | Nội Dung Đặc Tả | Liên Kết Trực Tiếp |
| :--- | :--- | :--- |
| **`uiux/`** | Giao diện nguyên mẫu tương tác (`main.html`, `recruitment.html`, `attendance.html`), thư viện ảnh chụp màn hình đầy đủ 11 màn hình thực tế, tiêu chuẩn thiết kế no-avatar, responsive layout không cuộn ngang. | [Xem uiux/README.md](uiux/README.md) |
| **`docs/`** | Tài liệu đặc tả yêu cầu phần mềm (SRS), ma trận phân quyền RBAC 6 vai trò, 8 sơ đồ Mermaid (Kiến trúc 3 tầng, Use Case, State Machine ATS, 3 Sequence Diagrams, Flowchart, Docker). | [Xem docs/README.md](docs/README.md) |
| **`database/`** | Lược đồ cơ sở dữ liệu quan hệ 14 bảng, ảnh sơ đồ DBML trực quan, file DDL PostgreSQL `init.sql` và dữ liệu mẫu khởi tạo. | [Xem database/README.md](database/README.md) |
| **`backend/`** | Dịch vụ RESTful API xây dựng bằng FastAPI (Python 3.12), SQLAlchemy 2.0 ORM, Pydantic v2, danh mục endpoints, tài liệu tương tác Swagger UI & ReDoc. | [Xem backend/README.md](backend/README.md) |
| **`frontend/`** | Ứng dụng Single Page Application (SPA) xây dựng bằng React 18 và Vite 5, tích hợp gọi API thời gian thực và đồng bộ dữ liệu. | [Xem frontend/README.md](frontend/README.md) |

> [Xem sơ đồ Use Case tổng quát](docs/system_diagrams.md#2-sơ-đồ-use-case-tổng-quan-overall-use-case-diagram)

---

## Mục Lục (Table of Contents)

- [1. Tổng Quan Hệ Thống](#1-tổng-quan-hệ-thống)
- [2. Kiến Trúc Chức Năng (Top-down Mind Map)](#2-kiến-trúc-chức-năng-top-down-mind-map)
- [3. Thư Viện Hình Ảnh & Giao Diện Thực Tế (Visual Showcase)](#3-thư-viện-hình-ảnh--giao-diện-thực-tế-visual-showcase)
  - [3.1. Phân hệ Quản lý Hồ sơ & Hợp đồng Lao động (Core HR)](#31-phân-hệ-quản-lý-hồ-sơ--hợp-đồng-lao-động-core-hr)
  - [3.2. Phân hệ Tuyển dụng Thông minh & Onboarding (ATS)](#32-phân-hệ-tuyển-dụng-thông-minh--onboarding-ats)
  - [3.3. Phân hệ Chấm công & Quản lý Nghỉ phép (Attendance & Leave Management)](#33-phân-hệ-chấm-công--quản-lý-nghỉ-phép-attendance--leave-management)
  - [3.4. Sơ đồ Thiết kế Cơ sở Dữ liệu (Database DBML)](#34-sơ-đồ-thiết-kế-cơ-sở-dữ-liệu-database-dbml)

---

## 1. Tổng Quan Hệ Thống

**QLNS** số hóa toàn diện quy trình quản trị nguồn nhân lực trong doanh nghiệp:

- **Tự động hóa tác vụ định kỳ:** Loại bỏ sai sót thủ công trong chấm công, tính lương, quản lý hồ sơ và theo dõi thời hạn hợp đồng lao động.
- **Tối ưu trải nghiệm tuyển dụng (ATS):** Rút ngắn thời gian tuyển dụng (Time-to-Hire) với đường ống Kanban trực quan, lịch phỏng vấn và phiếu chấm điểm (Scorecard) chuẩn hóa.
- **Tiếp nhận Onboarding liền mạch:** Chuyển đổi trực tiếp ứng viên trúng tuyển thành nhân viên chính thức trong hệ thống chỉ với một cú nhấp chuột mà không phải nhập lại dữ liệu.
- **Chấm công & Quản lý Nghỉ phép toàn trình:** Tích hợp đa phương thức điểm danh (Vân tay, GPS, FaceID, Wifi), ma trận phân ca hàng tuần (Work Schedule) và quy trình xét duyệt nghỉ phép thông minh.
- **Hỗ trợ ra quyết định & Báo cáo:** Cung cấp số liệu phân tích đa chiều về biến động nhân sự, cơ cấu phòng ban và chi phí nhân sự theo thời gian thực.
- **Tuân thủ pháp luật:** Chuẩn hóa quy trình quản lý hợp đồng, bảo hiểm xã hội, thuế TNCN theo Luật Lao động Việt Nam.

---

## 2. Kiến Trúc Chức Năng (Top-down Mind Map)

Hệ thống được thiết kế theo phương pháp phân rã từ trên xuống (Top-down decomposition) gồm **8 trụ cột chức năng**:

![Top-down functional decomposition diagram](topdown-approach.png)

1. **Recruitment Management (Tuyển dụng thông minh ATS)**
2. **Employee Records & Lifecycle (Hồ sơ & Vòng đời nhân sự)**
3. **Attendance & Leave Management (Chấm công & Quản lý nghỉ phép)**
4. **Compensation, Benefits & Payroll (Tiền lương, Thưởng & Phúc lợi)**
5. **Performance Management (Đánh giá hiệu suất KPI / OKR)**
6. **Training & Development (Đào tạo & Phát triển năng lực)**
7. **Reports & Analytics (Báo cáo & Phân tích nhân sự)**
8. **System Administration & Access Control (Quản trị hệ thống & Phân quyền RBAC)**

---

## 3. Thư Viện Hình Ảnh & Giao Diện Thực Tế (Visual Showcase)

Toàn bộ các phân hệ đã được thiết kế và xây dựng giao diện hoàn chỉnh với dữ liệu mẫu doanh nghiệp chuẩn xác:

### 3.1. Phân hệ Quản lý Hồ sơ & Hợp đồng Lao động (Core HR)

#### 📷 Danh sách Hồ sơ Nhân viên (`uiux/main.html`)
> Thiết kế chuẩn doanh nghiệp, áp dụng tiêu chuẩn bảo mật loại bỏ avatar, bảng dữ liệu tối ưu với thanh tìm kiếm và bộ lọc responsive.
![Danh sách Hồ sơ Nhân viên](uiux/profile/employee_profiles.png)

#### 📷 Quản lý Hợp đồng Lao động (`uiux/main.html`)
> Quản lý thời hạn hợp đồng, loại hợp đồng (Thử việc, Xác định thời hạn, Vô thời hạn), mức lương đóng BH và trạng thái hiệu lực.
![Quản lý Hợp đồng](uiux/profile/contracts.png)

#### 📷 Sơ đồ Cây Cơ cấu Tổ chức (`uiux/main.html`)
> Trực quan hóa cấu trúc phân cấp công ty từ Ban Giám đốc đến các phòng ban và nhân viên trực thuộc.
![Sơ đồ Cơ cấu Tổ chức](uiux/profile/organizational.png)

---

### 3.2. Phân hệ Tuyển dụng Thông minh & Onboarding (ATS)

#### Đường ống Tuyển dụng Ứng viên
> Quy trình tuyển dụng ATS với bảng Kanban 6 giai đoạn và bảng danh sách ứng viên; nút thao tác tiếp nhận ứng viên dạng icon trực quan.
![Quy trình Tuyển dụng ATS](uiux/recruitment/candidate.png)

#### Quản lý Yêu cầu Tuyển dụng
> Quản lý danh sách các vị trí đang tuyển, chỉ tiêu tuyển dụng, phòng ban yêu cầu và thời hạn nộp hồ sơ.
![Quản lý Yêu cầu Tuyển dụng](uiux/recruitment/job_requisitions.png)

#### Lịch Phỏng vấn & Đánh giá Scorecard
> Điều phối lịch phỏng vấn các vòng, hình thức phỏng vấn và bảng chấm điểm năng lực ứng viên.
![Lịch Phỏng vấn & Đánh giá](uiux/recruitment/interviews.png)

#### Đề xuất & Tiếp nhận Nhân sự Mới
> Bảng tiếp nhận Onboarding 7 cột hiển thị trọn vẹn trên màn hình desktop **không cần cuộn ngang**, nút hành động "Tiếp nhận" chuyển đổi nhanh vào QLNS.
![Tiếp nhận Onboarding](uiux/recruitment/onboard_handoff.png)

---

### 3.3. Phân hệ Chấm công & Quản lý Nghỉ phép (Attendance & Leave Management)

#### Bảng công & Điểm danh Thời gian thực
> Giám sát chi tiết nhật ký điểm danh: Giờ Check-in/Check-out, phân loại đúng giờ/đi muộn/về sớm, phương thức xác thực (Vân tay, GPS, FaceID, Wifi), số giờ công thực tế và Drawer xem chi tiết sự kiện.
![Bảng công & Điểm danh](uiux/attendance/timesheet_attendance.png)

#### Ca làm việc & Lịch phân ca Tuần
> Quản lý danh mục định nghĩa ca làm việc (Ca Hành chính, Ca Sáng, Ca Trực Đêm có hệ số) và ma trận phân ca hàng tuần (Work Schedule) Thứ Hai → Chủ Nhật.
![Ca làm việc & Lịch phân ca](uiux/attendance/work_shifts.png)

#### Đơn xin Nghỉ phép & Quỹ phép Cá nhân
> Bảng theo dõi lịch sử đơn nghỉ phép, hạn mức quỹ phép (Phép năm, Nghỉ ốm BHXH, Việc riêng, Nghỉ không lương) và modal tạo đơn tự động tính số ngày nghỉ.
![Đơn xin Nghỉ phép](uiux/attendance/leave_requests.png)

#### Xét duyệt Nghỉ phép
> Quy trình xét duyệt các yêu cầu nghỉ phép đang chờ xử lý (Pending Requests), hỗ trợ duyệt nhanh từng đơn, duyệt tất cả (Batch Approve) hoặc từ chối kèm phản hồi.
![Xét duyệt Nghỉ phép](uiux/attendance/leave_approval.png)

---

### 3.4. Sơ đồ Thiết kế Cơ sở Dữ liệu (Database DBML)

#### Lược đồ 14 Bảng Thực thể & Quan hệ Khóa ngoại
> Mô hình dữ liệu quan hệ chuẩn 3NF kết nối xuyên suốt giữa ứng viên tuyển dụng, hợp đồng và hồ sơ nhân sự chính thức.
![Sơ đồ Cơ sở Dữ liệu](database/dbml.png)
