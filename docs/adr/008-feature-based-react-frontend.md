# ADR-008 — Feature-based React frontend và shared API client

- **Trạng thái:** Accepted 2026-09-15
- **Owner:** Frontend lead
- **Liên quan:** [ADR-004](004-contract-first-openapi.md), quality goal Q4, Q6

## Bối cảnh

Prototype trong `uiux/` được tổ chức theo *màn hình*, mỗi file HTML tự chứa dữ liệu và tương tác mô phỏng. Chuyển thẳng
cách tổ chức đó sang React sẽ tạo ra các trang không dùng lại được gì của nhau.

## Quyết định

Tổ chức theo feature, không theo loại file: `src/features/<feature>/{api,hooks,components,pages}`. Mỗi feature chỉ được
phụ thuộc `src/shared` (design system) và `src/api` (HTTP client, map Problem Details, xử lý token và correlation);
feature không import internals của feature khác.

Access token giữ **trong bộ nhớ**, refresh token giữ ở `localStorage`. Các lần renew đồng thời dùng chung một exchange
đang bay, vì server coi refresh token là dùng một lần.

## Phương án đã cân nhắc

- **Tổ chức theo loại (`components/`, `pages/`, `hooks/` ở gốc)** — loại bỏ: một thay đổi nghiệp vụ sẽ rải khắp bốn thư mục.
- **Giữ access token trong `localStorage` cho tiện** — loại bỏ: mọi script trong trang đọc được nó.

## Hệ quả

- Permission trong `session.user.permissions` chỉ quyết định việc render; server vẫn kiểm tra lại mọi request ([ADR-003](003-backend-enforces-authorization.md)).
- Đóng tab là mất access token — đúng ý đồ; phiên được khôi phục bằng refresh token khi tải lại trang.
- Prototype HTML trong `uiux/` giữ vai trò tham chiếu thiết kế, không phải nguồn code.
