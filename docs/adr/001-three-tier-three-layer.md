# ADR-001 — Kiến trúc 3-tier và backend 3-layer

- **Trạng thái:** Accepted 2026-09-15
- **Owner:** Architect
- **Liên quan:** [ADR-002](002-modular-monolith.md), [ADR-003](003-backend-enforces-authorization.md), [ADR-009](009-dotnet-10-efcore-postgresql.md)

## Bối cảnh

Hệ thống phải phục vụ ba nhóm người dùng rất khác nhau (ứng viên bên ngoài, nhân viên nội bộ, quản trị) trên cùng một tập
dữ liệu nhân sự nhạy cảm. Prototype ban đầu là HTML tĩnh với dữ liệu mô phỏng, nên nguy cơ lớn nhất là business rule trôi
vào tầng trình bày và dữ liệu bị truy cập không qua kiểm soát.

## Quyết định

Ba **runtime tier**: React Web (Presentation), ASP.NET Core API cộng .NET Worker (Application), PostgreSQL (Data). Bên
trong Application Tier là ba **source layer**: `Qlns.Api` (Presentation), `Qlns.BusinessLogic` (Business), `Qlns.DataAccess`
(Data Access). Dependency luôn hướng vào trong: Business Logic không tham chiếu ASP.NET Core hay EF Core; `Qlns.Api` chỉ
chạm Data Access tại composition root để đăng ký dependency.

Worker **không** tạo tier thứ tư — nó nằm cùng Application Tier và chỉ khác ở chỗ không nhận request trực tiếp.

## Phương án đã cân nhắc

- **Hai tier (client gọi thẳng database)** — loại bỏ: không thể thực thi permission và data scope phía server, vi phạm Q1.
- **Hexagonal/Clean Architecture đầy đủ với project riêng cho domain** — loại bỏ ở giai đoạn này: thêm một ranh giới project
  nữa mà chưa có nhu cầu thay thế persistence; ranh giới ports/adapters cần thiết đã có ở [ADR-007](007-ports-adapters-and-outbox.md).

## Hệ quả

- Mọi query và command bắt buộc đi qua Backend API; frontend không có connection string.
- Business Logic test được bằng unit test thuần, không cần database — đây là lý do 1347 unit test hiện tại chạy không cần PostgreSQL.
- Cái giá: một use case đơn giản vẫn phải đi qua ba lớp. Chấp nhận, vì đây là ranh giới bảo vệ Q1–Q3.
