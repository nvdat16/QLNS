# 🎨 Phân Hệ Giao Diện & Trải Nghiệm Người Dùng (UI/UX)

> Thư mục chứa toàn bộ giao diện nguyên mẫu (Interactive Prototypes), thiết kế giao diện người dùng (UI/UX) độc lập và hình ảnh chụp màn hình các phân hệ chức năng của Hệ thống Quản lý Nhân sự (**QLNS**).

---

## 📌 Mục Lục

- [1. Danh Sách Giao Diện Độc Lập](#1-danh-sách-giao-diện-độc-lập)
- [2. Thư Viện Hình Ảnh Giao Diện (Visual Showcase)](#2-thư-viện-hình-ảnh-giao-diện-visual-showcase)
  - [2.1. Phân hệ Hồ sơ Nhân sự & Hợp đồng (main.html)](#21-phân-hệ-hồ-sơ-nhân-sự--hợp-đồng-mainhtml)
  - [2.2. Phân hệ Tuyển dụng & Onboarding ATS (recruitment.html)](#22-phân-hệ-tuyển-dụng--onboarding-ats-recruitmenthtml)
  - [2.3. Phân hệ Chấm công & Quản lý Nghỉ phép (attendance.html)](#23-phân-hệ-chấm-công--quản-lý-nghỉ-phép-attendancehtml)
- [3. Chi Tiết Tính Năng Đã Hoàn Thiện](#3-chi-tiết-tính-năng-đã-hoàn-thiện)
  - [3.1. Quản lý Hồ sơ & Hợp đồng lao động](#31-quản-lý-hồ-sơ--hợp-đồng-lao-động)
  - [3.2. Quản lý Tuyển dụng & Quy trình Onboarding](#32-quản-lý-tuyển-dụng--quy-trình-onboarding)
  - [3.3. Chấm công, Ca làm việc & Quản lý Nghỉ phép](#33-chấm-công-ca-làm-việc--quản-lý-nghỉ-phép)
- [4. Nguyên Tắc Thiết Kế UI/UX](#4-nguyên-tắc-thiết-kế-uiux)
- [5. Hướng Dẫn Xem & Trải Nghiệm Giao Diện](#5-hướng-dẫn-xem--trải-nghiệm-giao-diện)
- [🔗 Quay lại README Tổng Quan](../README.md)

---

## 1. Danh Sách Giao Diện Độc Lập

Hệ thống cung cấp 4 file HTML độc lập hoàn chỉnh, chạy trực tiếp trên trình duyệt mà không cần cài đặt backend:

| Tên File | Chức Năng Chính | Đường Dẫn Trực Tiếp |
| :--- | :--- | :--- |
| **`main.html`** | Quản lý Hồ sơ Nhân sự, Quản lý Hợp đồng lao động, Sơ đồ tổ chức doanh nghiệp (Org Chart), Drawer chi tiết nhân sự | [Xem file main.html](./main.html) |
| **`recruitment.html`** | Phân hệ Tuyển dụng ATS: Quản lý Ứng viên (Pipeline Kanban/Table), Yêu cầu tuyển dụng, Lịch phỏng vấn & Đánh giá, Chuyển tiếp Tiếp nhận (Onboarding Handoff) | [Xem file recruitment.html](./recruitment.html) |
| **`attendance.html`** | Phân hệ Chấm công & Nghỉ phép: Bảng công & Điểm danh (Check-in/out), Định nghĩa & Lịch phân ca (Work Shifts), Đơn xin nghỉ phép & Quỹ phép (Leave Requests), Xét duyệt nghỉ phép (Leave Approval) | [Xem file attendance.html](./attendance.html) |
| **`auth.html`** | Cổng xác thực: đăng nhập, đăng ký tài khoản, hiện/ẩn mật khẩu, kiểm tra biểu mẫu và điều hướng vào dashboard prototype. | [Xem file auth.html](./auth.html) |

---

## 2. Thư Viện Hình Ảnh Giao Diện (Visual Showcase)

### 2.1. Phân hệ Hồ sơ Nhân sự & Hợp đồng (`main.html`)

#### 📷 Danh sách Hồ sơ Nhân sự (Employee Directory)
Giao diện quản lý danh sách nhân viên chuẩn doanh nghiệp, loại bỏ hình ảnh đại diện cá nhân theo quy chuẩn bảo mật/tối giản, tối ưu bảng dữ liệu với thanh công cụ tìm kiếm, bộ lọc phòng ban, trạng thái và nút xem chi tiết Drawer.
![Danh sách Hồ sơ Nhân sự](profile/employee_profiles.png)

#### 📷 Quản lý Hợp đồng Lao động (Contracts Management)
Quản lý vòng đời hợp đồng: Số hợp đồng, loại hợp đồng (Thử việc, Xác định thời hạn, Không xác định thời hạn), mức lương đóng BH, ngày hiệu lực/hết hạn và trạng thái kích hoạt.
![Quản lý Hợp đồng Lao động](profile/contracts.png)

#### 📷 Sơ đồ Cây Cơ cấu Tổ chức (Organizational Chart)
Trực quan hóa cây phả hệ doanh nghiệp: Hội đồng Quản trị -> Ban Giám đốc -> Các Khối & Phòng ban chức năng -> Đội ngũ nhân viên.
![Sơ đồ Cơ cấu Tổ chức](profile/organizational.png)

---

### 2.2. Phân hệ Tuyển dụng & Onboarding ATS (`recruitment.html`)

#### 📷 Quy trình Tuyển dụng Ứng viên (Candidate Pipeline ATS)
Giao diện ATS chuyên nghiệp với 2 chế độ hiển thị (Kanban Board theo các phễu: Mới ứng tuyển, Sơ loại, Phỏng vấn vòng 1, Phỏng vấn vòng 2, Đề xuất Offer, Đã trúng tuyển; và Chế độ Bảng danh sách). Nút thao tác tiếp nhận hiển thị dạng icon trực quan, gọn gàng.
![Quy trình Tuyển dụng Ứng viên](recruitment/candidate.png)

#### 📷 Quản lý Yêu cầu Tuyển dụng (Job Requisitions)
Danh sách các vị trí đang tuyển dụng: Mã tin tuyển dụng, chức danh, phòng ban yêu cầu, chỉ tiêu số lượng, mức lương dự kiến, hạn nộp hồ sơ và trạng thái chiến dịch.
![Quản lý Yêu cầu Tuyển dụng](recruitment/job_requisitions.png)

#### 📷 Lịch Phỏng vấn & Phiếu Đánh giá (Interviews & Scorecards)
Lịch hẹn phỏng vấn các vòng, hình thức (Trực tiếp / Google Meet), hội đồng đánh giá và phiếu chấm điểm (Scorecard) năng lực chuyên môn, văn hóa.
![Lịch Phỏng vấn & Đánh giá](recruitment/interviews.png)

#### 📷 Đề xuất & Tiếp nhận Onboarding (Offers & Onboarding Handoff)
Bảng quản lý kết quả Offer và tiếp nhận nhân sự mới. Bảng 7 cột được dàn trang responsive chuẩn xác, hiển thị đầy đủ thông tin trên màn hình mà **không cần thanh cuộn ngang (no horizontal scrolling)**. Nút hành động **"Tiếp nhận"** hỗ trợ chuyển giao trực tiếp dữ liệu từ ứng viên trúng tuyển sang hồ sơ nhân viên chính thức trong QLNS.
![Tiếp nhận Onboarding](recruitment/onboard_handoff.png)

---

### 2.3. Phân hệ Chấm công & Quản lý Nghỉ phép (`attendance.html`)

#### 📷 Bảng công & Điểm danh Thời gian thực (Check-in / Check-out & Timesheet)
Giám sát chi tiết lịch sử điểm danh hàng ngày: Giờ Check-in, Check-out, phân loại đúng giờ/đi muộn/về sớm, phương thức xác thực (Vân tay Cổng chính, GPS Mobile, FaceID, Wifi), số giờ công thực tế và Drawer xem chi tiết nhật ký sự kiện. Nút hành động chấm công nhanh và xuất báo cáo Excel tiện lợi.
![Bảng công & Điểm danh](attendance/timesheet_attendance.png)

#### 📷 Ca làm việc & Lịch trực Tuần (Work Shifts & Weekly Schedule)
Bao gồm 2 khối chức năng theo đúng sơ đồ mindmap:
- **Định nghĩa Ca làm việc (Shift Definition)**: Ca Hành chính (08:30-17:30, 8.0h công, nghỉ trưa 60p), Ca Sáng Part-time (08:00-12:00, 4.0h công), Ca Trực Server & Đêm (22:00-06:00, hệ số công 1.5x, phụ cấp ca đêm). Hỗ trợ modal thêm/sửa ca làm việc.
- **Phân ca làm việc tuần (Employee Shift Assignment & Work Schedule)**: Ma trận phân bổ ca làm việc từ Thứ Hai đến Chủ Nhật cho toàn bộ đội ngũ nhân sự nòng cốt.
![Ca làm việc & Lịch trực](attendance/work_shifts.png)

#### 📷 Đơn xin Nghỉ phép & Hạn mức Quỹ phép (Leave Requests & Leave Quota)
- **Hạn mức Quỹ phép Cá nhân (Leave Balance Quota)**: Thẻ trực quan thể hiện Phép năm (AL - 8.5/12 ngày), Nghỉ ốm hưởng BHXH (29/30 ngày), Việc riêng có lương (3/3 ngày), Nghỉ không lương.
- **Lịch sử & Trạng thái Đơn nghỉ phép (Leave History & Request Status)**: Bảng theo dõi mã đơn, người tạo, loại phép, thời gian, số ngày, lý do, người duyệt và trạng thái (Đã duyệt, Chờ duyệt, Từ chối). Modal tạo đơn hỗ trợ tự động tính số ngày nghỉ.
![Đơn xin Nghỉ phép](attendance/leave_requests.png)

#### 📷 Xét duyệt Nghỉ phép (Leave Approval & Workflow)
Danh sách các đơn nghỉ phép đang chờ phê duyệt (Pending Requests) với thông tin quỹ phép còn lại của nhân sự. Người quản lý có thể thao tác:
- **Duyệt nhanh (Approve)** trực tiếp từng đơn hoặc duyệt hàng loạt (**Duyệt tất cả**).
- **Từ chối (Reject)** với phản hồi minh bạch.
- Bộ lọc tìm kiếm nhanh theo mã đơn, nhân viên và loại phép.
![Xét duyệt Nghỉ phép](attendance/leave_approval.png)

---

## 3. Chi Tiết Tính Năng Đã Hoàn Thiện

### 3.1. Quản lý Hồ sơ & Hợp đồng lao động (`main.html`)
- **Dữ liệu mẫu chuẩn hóa doanh nghiệp Việt Nam**: Đầy đủ 5 hồ sơ nhân sự nòng cốt đa phòng ban (Giám đốc Kỹ thuật, Trưởng phòng Nhân sự, Kỹ sư phần mềm Senior, Kế toán trưởng, Quản viên tuyển dụng) với các trường thông tin: Mã NV, Họ tên, Phòng ban, Chức danh, Email công vụ, Số điện thoại, Trạng thái hoạt động.
- **Tiêu chuẩn thiết kế không avatar (No-Avatar Standard)**: Loại bỏ các ảnh đại diện cá nhân trên bảng dữ liệu và Drawer theo đúng yêu cầu bảo mật thông tin nội bộ.
- **Tách bạch thông tin Hợp đồng**: Bảng hồ sơ nhân sự chỉ hiển thị thông tin hồ sơ; toàn bộ thông tin loại hợp đồng, thời hạn, mức lương hợp đồng được quy tụ tại bảng **Quản lý Hợp đồng**.
- **Thanh công cụ lọc & tìm kiếm linh hoạt (Responsive Toolbar)**: Tự động co giãn theo tỉ lệ màn hình, không bị tràn dòng hay chồng chéo chữ trên tablet/desktop.
- **Drawer Chi tiết Nhân sự**: Bấm vào từng hàng nhân viên để mở bảng trượt bên phải hiển thị đầy đủ thông tin cá nhân, liên hệ, quá trình công tác và danh sách hợp đồng đã ký.

### 3.2. Quản lý Tuyển dụng & Quy trình Onboarding (`recruitment.html`)
- **Thanh điều hướng 4 chức năng cốt lõi (Top Navigation)**:
  1. *Quy trình Tuyển dụng (Pipeline)*
  2. *Tin Tuyển dụng (Job Requisitions)*
  3. *Lịch Phỏng vấn & Đánh giá (Interviews & Scorecards)*
  4. *Đề xuất & Tiếp nhận (Offers & Onboarding Handoff)*
- **Loại bỏ số đếm dư thừa**: Tối giản giao diện các nút điều hướng và ô chọn lọc, không hiển thị số lượng cạnh tên chức năng.
- **Tối ưu bảng Tiếp nhận Onboarding**:
  - Đổi tên nút hành động thành **"Tiếp nhận"** ngắn gọn, chuẩn nghiệp vụ.
  - Căn chỉnh layout bảng 7 cột gọn gàng, vừa vặn toàn bộ khung hình desktop mà không phát sinh thanh cuộn ngang gây khó khăn khi thao tác.
- **Nút thao tác tiếp nhận icon-only tại trang ATS**: Tại bảng ứng viên đã trúng tuyển, nút "Tiếp nhận" được hiển thị dạng icon trực quan (`how_to_reg`), tiết kiệm không gian bảng.
- **Dữ liệu 12 Ứng viên Mock Data**: Khớp nối hoàn toàn với các mã vị trí tuyển dụng từ `TD-2024-01` đến `TD-2024-05`.

### 3.3. Chấm công, Ca làm việc & Quản lý Nghỉ phép (`attendance.html`)
- **Khớp 100% sơ đồ Mindmap Attendance & Leave Management**:
  1. **Work Shifts**: Cấu hình Shift Definition (giờ vào/ra, nghỉ giữa ca, hệ số công 1.0x / 0.5x / 1.5x) và ma trận phân ca hàng tuần (Work Schedule) Thứ Hai → Chủ Nhật.
  2. **Check-in / Check-out**: Nhật ký điểm danh thời gian thực, giám sát Đi muộn / Về sớm (Late / Early Leave), thiết bị ghi nhận (Vân tay, GPS, FaceID, Wifi), Drawer chi tiết timeline trong ngày.
  3. **Leave Requests**: Quỹ phép 4 loại (Phép năm, Nghỉ ốm BHXH, Việc riêng, Nghỉ không lương), Modal tạo đơn có chức năng tự tính số ngày theo hình thức nghỉ (Cả ngày, Nửa ngày, Nhiều ngày).
  4. **Leave Approval**: Danh sách chờ duyệt (Pending Requests), chức năng Duyệt / Từ chối tức thì kèm Toast thông báo và nút Phê duyệt tất cả (Batch Approval).
- **Thống kê tổng quan đầu trang**: 4 thẻ KPI (Quân số có mặt, Đi muộn/Về sớm, Đang nghỉ phép, Đơn chờ xét duyệt) cập nhật theo thời gian thực.
- **Điều hướng Sidebar đồng bộ**: Tích hợp xuyên suốt giữa `main.html`, `recruitment.html` và `attendance.html`.

---

## 4. Nguyên Tắc Thiết Kế UI/UX

1. **Enterprise Clean & Modern**: Sử dụng hệ màu Tailwind Slate & Indigo sang trọng, thanh lịch, tạo cảm giác chuyên nghiệp cho hệ thống ERP / HRMS.
2. **Data Density & Usability**: Tối ưu mật độ hiển thị dữ liệu bảng, giãn cách ô hợp lý, nhãn trạng thái (Badge) rõ ràng theo mã màu quy ước (Xanh lá: Đúng giờ/Đã duyệt, Vàng: Đi muộn/Chờ duyệt, Cam: Về sớm, Tím: Trực đêm, Xanh dương: Nghỉ phép, Đỏ: Từ chối/Vắng mặt).
3. **Mobile & Tablet Responsive**: Sử dụng CSS Grid, Flexbox hiện đại để tự thích ứng trên mọi độ phân giải màn hình.
4. **No-Avatar Standard**: Tối giản và bảo mật, sử dụng tên in đậm, mã định danh và thông tin công tác.

---

## 5. Hướng Dẫn Xem & Trải Nghiệm Giao Diện

Bạn có thể mở trực tiếp các file HTML bằng bất kỳ trình duyệt web nào:

```bash
# Sử dụng lệnh mở trên macOS
open uiux/main.html
open uiux/recruitment.html
open uiux/attendance.html

# Mở trực tiếp từng tab cụ thể của Chấm công:
open "uiux/attendance.html?view=timesheet"
open "uiux/attendance.html?view=shifts"
open "uiux/attendance.html?view=leaves"
open "uiux/attendance.html?view=approvals"
```

---

[⬅️ Trở về Trang Chủ Tài Liệu](../README.md)
