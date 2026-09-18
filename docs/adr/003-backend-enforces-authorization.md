# ADR-003 — Backend thực thi authorization và business rules

- **Trạng thái:** Proposed — **code đã đi theo quyết định này**, chờ Project Owner xác nhận
- **Owner:** chưa có
- **Liên quan:** [ADR-001](001-three-tier-three-layer.md), [ADR-011](011-in-house-identity.md), quality goal Q1

## Bối cảnh

Dữ liệu nhân sự là confidential/restricted (C6). UI prototype có sẵn các màn hình ẩn/hiện nút theo vai trò, và rủi ro R5
trong sổ rủi ro chính là "RBAC chỉ ẩn nút, thiếu data scope server-side".

## Quyết định

Deny-by-default tại application boundary. Mỗi endpoint nghiệp vụ gắn một policy yêu cầu permission dạng
`<module>.<feature>.<verb>`; mỗi service kiểm tra thêm **data scope** (`self` / `department` / `organization`) trên chính
tài nguyên được yêu cầu. Tài nguyên ngoài scope trả `404`, không phải `403`, để không tiết lộ sự tồn tại của bản ghi.

Quyền do client gửi lên không bao giờ được tin. Route guard ở frontend chỉ phục vụ trải nghiệm.

## Phương án đã cân nhắc

- **Chỉ kiểm tra permission, bỏ data scope** — loại bỏ: một Line Manager sẽ đọc được hồ sơ toàn công ty.
- **Row-level security của PostgreSQL** — loại bỏ ở giai đoạn này: scope phụ thuộc actor và quan hệ quản lý, biểu diễn
  trong policy ứng dụng dễ test hơn; có thể xem lại như lớp phòng thủ thứ hai.

## Hệ quả

- Mọi service nhận `CoreHrActor` chứ không nhận `userId` trần.
- Cần `EveryBusinessEndpointRequiresAuthorization` và bộ test deny-by-default theo từng vai trò/scope; hiện vẫn `Planned`.
- Trả `404` cho tài nguyên ngoài scope khiến log khó đọc hơn một chút; bù lại bằng `correlationId` trong audit.
