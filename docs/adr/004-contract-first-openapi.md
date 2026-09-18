# ADR-004 — REST/JSON, DTO và contract-first OpenAPI 3.0.3

- **Trạng thái:** Accepted 2026-09-15
- **Owner:** Architect
- **Liên quan:** [ADR-006](006-explicit-commands-and-state-machines.md), [ADR-008](008-feature-based-react-frontend.md)

## Bối cảnh

Frontend và backend được phát triển song song, và tài liệu nghiệp vụ phải truy vết được xuống từng endpoint. Nếu contract
sinh ra **từ** code thì mọi thay đổi code đều âm thầm trở thành thay đổi contract.

## Quyết định

`docs/api/openapi.yaml` là contract, viết trước, và là nguồn chuẩn thắng `API_REFERENCE.md`. Ứng dụng phải giữ nguyên
operationId, schema, status code và error code của contract. Mỗi operation mang `x-requirement` trỏ về requirements
baseline và `x-implementation-status` cho biết đã làm tới đâu.

Quy ước bắt buộc: base path `/api/v1`; JSON `camelCase`; thời gian RFC 3339 UTC; lỗi dùng `application/problem+json` có
`code` ổn định và `correlationId`; aggregate sửa được thì expose `ETag` và yêu cầu `If-Match`; collection phải phân trang
có giới hạn kèm allowlist cho filter/sort.

## Phương án đã cân nhắc

- **Code-first, sinh OpenAPI từ controller** — loại bỏ: contract sẽ luôn "đúng" theo định nghĩa và mất vai trò gate.
- **GraphQL** — loại bỏ: data scope theo từng trường và phân trang có giới hạn khó siết hơn nhiều so với REST ở bài toán này.

## Hệ quả

- Breaking change cần version mới hoặc một ADR được chấp nhận, cộng contract diff — gate `OpenApiBreakingChangeGate`, hiện `Planned`.
- Có contract đầy đủ dễ bị hiểu nhầm là API đã chạy; đó là rủi ro R1b và là lý do tồn tại của `x-implementation-status`.
