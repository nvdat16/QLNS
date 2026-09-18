# Architecture Decision Records

Mỗi quyết định kiến trúc có một file riêng, đánh số tăng dần và không bao giờ tái sử dụng số. Một ADR đã publish thì
**không sửa nội dung quyết định**; muốn đổi thì viết ADR mới và đặt ADR cũ sang `Superseded by ADR-xxx`.

## Quy ước trạng thái

| Trạng thái | Nghĩa |
|---|---|
| `Proposed` | Đã viết ra, chưa có owner nghiệp vụ hoặc chưa được phê duyệt. **Không** được coi là đã chốt, kể cả khi code đã đi theo. |
| `Accepted` | Có owner, ngày phê duyệt, alternatives và consequences rõ ràng. |
| `Superseded` | Bị thay bởi một ADR sau; header ghi rõ ADR nào. |
| `Rejected` | Đã cân nhắc và bác bỏ; giữ lại để không phải tranh luận lại. |

Không ADR nào được chuyển sang `Accepted` chỉ vì công nghệ đã xuất hiện trong prototype, sơ đồ, file DDL hay source code.

## Chỉ mục

| ADR | Quyết định | Trạng thái |
|---|---|---|
| [ADR-001](001-three-tier-three-layer.md) | Kiến trúc 3-tier React – ASP.NET Core API – PostgreSQL và backend 3-layer | Accepted 2026-09-15 |
| [ADR-002](002-modular-monolith.md) | Backend modular monolith trước microservices | Proposed |
| [ADR-003](003-backend-enforces-authorization.md) | Backend thực thi authorization và business rules | Proposed |
| [ADR-004](004-contract-first-openapi.md) | REST/JSON, DTO và contract-first OpenAPI 3.0.3 | Accepted 2026-09-15 |
| [ADR-005](005-postgresql-system-of-record.md) | PostgreSQL system of record và versioned migration | Proposed |
| [ADR-006](006-explicit-commands-and-state-machines.md) | Explicit commands và state transitions | Proposed |
| [ADR-007](007-ports-adapters-and-outbox.md) | Ports/adapters, outbox và reliable delivery | Proposed |
| [ADR-008](008-feature-based-react-frontend.md) | Feature-based React frontend và shared API client | Accepted 2026-09-15 |
| [ADR-009](009-dotnet-10-efcore-postgresql.md) | .NET 10, ASP.NET Core, EF Core và PostgreSQL | Accepted 2026-09-15 |
| [ADR-010](010-delivery-scope-two-pillars.md) | Thu hẹp phạm vi giao hàng về hai trụ cột Recruitment và Core HR | Accepted 2026-09-17 |
| [ADR-011](011-in-house-identity.md) | Xác thực và quản trị tài khoản làm nội bộ, không dùng Identity Provider ngoài | Accepted 2026-09-18 |

## Nợ quản trị đang mở

Năm ADR dưới đây vẫn `Proposed` nhưng **code đã được viết dựa trên chúng**. Đây là nợ quản trị có thật, không phải lỗi
đánh máy: nếu một trong số này bị bác bỏ, phần code tương ứng phải viết lại chứ không chỉ sửa cấu hình.

| ADR | Code đang phụ thuộc |
|---|---|
| ADR-002 | Toàn bộ cây `Modules/<Module>/<Feature>` ở cả ba layer |
| ADR-003 | Mọi `*Policies` ở `Qlns.Api` và mọi kiểm tra data scope trong service |
| ADR-005 | `schema.sql`, mọi repository EF Core, các partial unique index làm invariant |
| ADR-006 | Mọi action endpoint (`/approve`, `/advance`, `/complete`, …) và các state machine trong domain |
| ADR-007 | `outbox_messages`, `CoreHrOutbox.Message` và các port `IDocumentStorage` / `IResumeParser` / `IMalwareScanner` |

Cần Project Owner xác nhận từng ADR trên, hoặc ghi nhận rõ rằng chúng được chấp nhận ngầm khi duyệt code.
