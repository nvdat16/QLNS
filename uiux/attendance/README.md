# ⏱️ Phân Hệ Chấm Công & Quản Lý Nghỉ Phép (Attendance & Leave Management)

> Thư mục chứa hình ảnh chụp màn hình và tài liệu giao diện nguyên mẫu của phân hệ Chấm công & Quản lý Nghỉ phép ([attendance.html](../attendance.html)).

---

## 📌 Mục Lục
1. [Tổng Quan & Cải Tiến Cân Bằng Giao Diện](#1-tổng-quan--cải-tiến-cân-bằng-giao-diện)
2. [Hình Ảnh Giao Diện & Bố Cục Chuẩn](#2-hình-ảnh-giao-diện--bố-cục-chuẩn)
   - [Bảng công & Điểm danh (Timesheet)](#21-bảng-công--điểm-danh-check-in--check-out)
   - [Ca làm việc & Lịch trực tuần (Work Shifts)](#22-ca-làm-việc--lịch-trực-tuần-work-shifts)
   - [Đơn xin nghỉ phép & Quỹ phép (Leave Requests)](#23-đơn-xin-nghỉ-phép--hạn-mức-quỹ-phép)
   - [Xét duyệt nghỉ phép (Leave Approval)](#24-xét-duyệt-nghỉ-phép-leave-approval)
3. [Quy Chuẩn Căn Chỉnh Bảng Dữ Liệu (Balanced Layout Standard)](#3-quy-chuẩn-căn-chỉnh-bảng-dữ-liệu-balanced-layout-standard)
4. [Hướng Dẫn Mở Trực Tiếp](#4-hướng-dẫn-mở-trực-tiếp)

---

## 1. Tổng Quan & Cải Tiến Cân Bằng Giao Diện

Giao diện Chấm công & Nghỉ phép được thiết kế chuẩn Enterprise, tuân thủ các quy tắc:
- **Hiển thị trọn vẹn 100% cột không cuộn ngang**: Toàn bộ các cột dữ liệu vừa vặn trên màn hình laptop/desktop tiêu chuẩn (1440px / 1280px).
- **Tiêu đề cột không bị gãy dòng (No Header Wrapping)**: Toàn bộ tiêu đề như `CHECK-IN`, `CHECK-OUT`, `THIẾT BỊ / ĐỊA ĐIỂM`, `GIỜ CÔNG`, `TRẠNG THÁI`, `THAO TÁC`, `HÀNH ĐỘNG` nằm gọn trên 1 dòng.
- **Cân đối nút hành động (Balanced Action Buttons)**: Hai nút **Duyệt** (`check`) và **Từ chối** (`close`) có cùng kích thước (`h-8`, `min-w-[72px]`), bố trí cạnh nhau cân xứng, không bị ngắt dòng thành "Từ \n chối".
- **Định dạng thời gian 2 tầng trực quan**: Khoảng thời gian nghỉ phép hiển thị ngày bắt đầu và ngày kết thúc trên 2 dòng gọn gàng, giảm độ rộng cột và tăng độ dễ đọc.

---

## 2. Hình Ảnh Giao Diện & Bố Cục Chuẩn

### 2.1. Bảng công & Điểm danh (Check-in / Check-out)
Giám sát chi tiết điểm danh hàng ngày với đầy đủ 9 cột: Nhân viên, Phòng ban & Chức danh, Ca làm việc, Check-in, Check-out, Thiết bị/Địa điểm, Giờ công, Trạng thái, Thao tác.
![Bảng công & Điểm danh](timesheet_attendance.png)

---

### 2.2. Ca làm việc & Lịch trực tuần (Work Shifts)
Cấu hình danh mục ca làm việc (Ca Hành chính, Ca Sáng, Ca Trực Server Đêm) và ma trận lịch trực từ Thứ Hai đến Chủ Nhật.
![Ca làm việc & Lịch trực](work_shifts.png)

---

### 2.3. Đơn xin nghỉ phép & Hạn mức quỹ phép
4 thẻ thống kê hạn mức quỹ phép cá nhân (Phép năm, Nghỉ ốm BHXH, Việc riêng, Nghỉ không lương) kèm bảng lịch sử 9 cột được căn chỉnh đều đặn, không tràn lề.
![Đơn xin nghỉ phép](leave_requests.png)

---

### 2.4. Xét duyệt nghỉ phép (Leave Approval)
Bảng duyệt đơn 8 cột tích hợp thông tin nhân viên và phòng ban thành một cột gọn gàng; 2 nút hành động "Duyệt" và "Từ chối" cân xứng hoàn hảo.
![Xét duyệt nghỉ phép](leave_approval.png)

---

## 3. Quy Chuẩn Căn Chỉnh Bảng Dữ Liệu (Balanced Layout Standard)

| Phân Vùng | Trước Khi Tinh Chỉnh | Sau Khi Tinh Chỉnh | Lợi Ích Trực Quan |
| :--- | :--- | :--- | :--- |
| **Tiêu đề cột bảng công** | Bị xuống dòng vụn vặt (`CHECK-` / `OUT`, `THIẾT BỊ / ĐỊA` / `ĐIỂM`) | Header nằm trên 1 dòng duy nhất (`whitespace-nowrap`), padding `py-3 px-3` | Bảng chuyên nghiệp, tinh gọn, không tạo khoảng trống thừa |
| **Bảng xét duyệt nghỉ phép** | 9 cột, nút "Từ chối" bị gãy thành 2 dòng ("Từ" / "chối"), cột hành động bị đẩy ra ngoài cuộn ngang | Gom thành 8 cột (`NHÂN VIÊN & PHÒNG BAN`), nút cố định `min-w-[72px]` trên 1 dòng | Hiển thị 100% cột không cần cuộn ngang, nút bấm đều đẹp |
| **Cột thời gian nghỉ** | Trải dài trên một dòng (`12/09/2026 → 13/09/2026`) chiếm nhiều chiều rộng | Hiển thị 2 tầng: Ngày bắt đầu ở trên, ngày kết thúc / "Trong ngày" ở dưới | Tiết kiệm 70px chiều ngang, cân đối dòng dữ liệu |
| **Mật độ thông tin nhân sự** | Tên kèm email dài gây ép dòng | Hiển thị tên in đậm + Mã NV (hoặc kèm phòng ban ngắn gọn) | Không tràn lề, đồng bộ với tiêu chuẩn No-Avatar |

---

## 4. Hướng Dẫn Mở Trực Tiếp

```bash
# Mở trực tiếp trang Chấm công & Nghỉ phép
open uiux/attendance.html

# Mở nhanh từng tab cụ thể
open "uiux/attendance.html?view=timesheet"
open "uiux/attendance.html?view=shifts"
open "uiux/attendance.html?view=leaves"
open "uiux/attendance.html?view=approvals"
```

---
[⬅️ Quay lại README UI/UX](../README.md)
