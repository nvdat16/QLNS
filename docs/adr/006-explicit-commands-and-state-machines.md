# ADR-006 — Explicit commands và state transitions

- **Trạng thái:** Proposed — **code đã đi theo quyết định này**, chờ Project Owner xác nhận
- **Owner:** chưa có
- **Liên quan:** [ADR-004](004-contract-first-openapi.md), quality goal Q3

## Bối cảnh

Gần như mọi thực thể nghiệp vụ trong hệ thống đều có vòng đời có điều kiện phê duyệt: requisition, application, offer,
contract, addendum, employee event, probation review, offboarding case. Một `PATCH { "status": "approved" }` tổng quát sẽ
xoá sạch mọi guard của các vòng đời đó.

## Quyết định

Không endpoint nào cho phép client đặt trực tiếp trường `status`. Mỗi chuyển trạng thái là một action endpoint riêng
(`/approve`, `/reject`, `/advance`, `/activate`, `/complete`, `/cancel`, …), bắt buộc `If-Match`, và được domain kiểm tra
theo bốn yếu tố: actor, trạng thái hiện tại, trạng thái đích và guard nghiệp vụ.

Transition ngoài state machine trả lỗi nghiệp vụ có `code` ổn định; xung đột phiên bản trả `409`.

## Phương án đã cân nhắc

- **REST thuần với PATCH trên trường `status`** — loại bỏ: guard sẽ phải suy diễn từ giá trị cũ và mới, và mọi trường mới
  thêm vào DTO đều trở thành một lối đi vòng.
- **Một endpoint `/transition` nhận trạng thái đích** — loại bỏ: mất khả năng gắn permission riêng cho từng hành động
  (duyệt offer và gửi offer không cùng một quyền).

## Hệ quả

- Số lượng endpoint lớn hơn — đây là phần chính khiến contract có 79 operation trên 62 path.
- Giá trị reserved ngoài phạm vi (`suspended`, `suspension`, `return_to_work`) an toàn theo thiết kế: không có action
  endpoint nào đặt được chúng, dù schema vẫn cho phép.
- Cần gate `EveryStateChangeUsesACommand`; hiện `Planned`.
