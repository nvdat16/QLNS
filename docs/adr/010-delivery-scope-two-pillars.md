# ADR-010 — Thu hẹp phạm vi giao hàng về hai trụ cột Recruitment và Core HR

- **Trạng thái:** Accepted 2026-09-17 — **owner và phần alternatives/consequences đầy đủ vẫn chờ Project Owner xác nhận**
- **Owner:** chưa có
- **Liên quan:** [ADR-011](011-in-house-identity.md)

## Bối cảnh

Bản đồ phân rã chức năng `topdown-approach.png` (cập nhật 2026-09-17) có bảy trụ cột. Thiết kế ban đầu trải rộng hơn năng
lực giao hàng của đợt này, trong đó Attendance & Leave đã được đặc tả đầy đủ (SRS, 13 story, DDL 13 bảng, 24 endpoint).

## Quyết định

Phạm vi giao hàng là **đúng các chức năng lá in đậm** dưới hai trụ cột Recruitment (18 lá) và Core HR (16 lá, gồm nhánh
Contract Management).

Ngoài phạm vi: Reports & Analytics, Performance Management, Compensation & Benefits, Attendance & Leave Management, phần
còn lại của System Administration, cộng **bốn chức năng lá không in đậm nằm ngay trong hai trụ cột được chọn** — Headcount
& Budget Validation, Recruitment Channel Management, Organizational Chart, Suspension & Return to Work.

Authorization, audit log và transactional outbox **vẫn là cơ chế bắt buộc** của mọi command; chỉ các màn hình/endpoint
quản trị chúng mới nằm ngoài phạm vi.

## Phương án đã cân nhắc

- **Xoá hẳn thiết kế Attendance & Leave** — loại bỏ: phí phạm một đặc tả hoàn chỉnh. Thay vào đó đưa nguyên vẹn vào
  `docs/deferred/attendance_leave/` kèm cảnh báo không được tham chiếu từ tài liệu authoritative.
- **Giữ bốn chức năng lá không in đậm vì "đằng nào cũng gần xong"** — loại bỏ: mỗi cái kéo theo màn hình, endpoint và
  quy tắc nghiệp vụ riêng.

## Hệ quả

- Việc thu hẹp phạm vi **không đổi DDL**: phần bị loại là màn hình và endpoint, không phải cấu trúc dữ liệu.
- `employees.status = 'suspended'` và `employee_events.event_type IN ('suspension','return_to_work')` trở thành **giá trị
  reserved**: tồn tại trong schema, không action endpoint nào đặt được ([ADR-006](006-explicit-commands-and-state-machines.md)).
- Tiền điều kiện của offboarding vì thế là nhân viên đang `active` hoặc `probation`.
- Rủi ro R2b: thiết kế trong `deferred/` sẽ drift khỏi canonical nếu module đó quay lại phạm vi.
