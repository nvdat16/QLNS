# ADR-002 — Backend modular monolith trước microservices

- **Trạng thái:** Proposed — **code đã đi theo quyết định này**, chờ Project Owner xác nhận
- **Owner:** chưa có
- **Liên quan:** [ADR-001](001-three-tier-three-layer.md), [ADR-007](007-ports-adapters-and-outbox.md)

## Bối cảnh

Phạm vi giao hàng gồm bốn module (Recruitment, Core HR, Contracts, Identity & Access) có ràng buộc dữ liệu chặt với nhau:
chấp nhận offer tạo nhân viên và hợp đồng trong **một** transaction, quyết định thử việc sinh `employee_events`, hoàn tất
thôi việc sinh event termination. Đội phát triển là một nhóm nhỏ, chưa có hạ tầng vận hành phân tán.

## Quyết định

Một deployable duy nhất, chia module theo nghiệp vụ: `Modules/<Module>/<Feature>` lặp lại ở cả ba layer, tên module khớp
OpenAPI tag. Mỗi bảng có đúng một module sở hữu; module khác muốn đọc/ghi thì đi qua interface của module chủ, không dùng
bảng như API ngầm.

Ngoại lệ được ghi nhận tường minh: các command cần một transaction duy nhất xuyên module (offer accept → employee +
contract + onboarding_tasks) được phép ghi entity của module khác **qua entity của module đó**, không qua HTTP.

## Phương án đã cân nhắc

- **Microservices theo module** — loại bỏ: sẽ biến các invariant một-transaction ở trên thành saga, đổi lấy một bài toán
  nhất quán phân tán mà quy mô hiện tại không cần.
- **Monolith không chia module** — loại bỏ: mất khả năng tách về sau và mất ranh giới sở hữu dữ liệu.

## Hệ quả

- Tách microservices sau này vẫn khả thi vì ranh giới module và quyền sở hữu bảng đã rõ, nhưng sẽ phải xử lý đúng những
  transaction xuyên module đã liệt kê ở `src/backend/README.md`.
- Cần một fitness function `NoCrossModuleTableWrites` để ranh giới không trôi; hiện vẫn `Planned`.
