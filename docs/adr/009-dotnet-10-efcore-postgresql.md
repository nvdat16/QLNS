# ADR-009 — .NET 10, ASP.NET Core, EF Core và PostgreSQL

- **Trạng thái:** Accepted 2026-09-15
- **Owner:** Project Owner
- **Liên quan:** [ADR-001](001-three-tier-three-layer.md), [ADR-005](005-postgresql-system-of-record.md), constraint C4

## Bối cảnh

Rủi ro R3 ghi nhận nguy cơ "stack được chọn theo sơ đồ mà không qua decision process": một vài công nghệ đã xuất hiện
trong prototype và file DDL trước khi có ai quyết định chính thức.

## Quyết định

Backend dùng .NET 10 với ASP.NET Core và EF Core; database là PostgreSQL; frontend dùng React với Vite. Được Project Owner
chấp thuận ngày 2026-09-15. Phiên bản patch của package phải được pin trước release.

## Phương án đã cân nhắc

- **Node.js/NestJS** — loại bỏ: đội hiện có kinh nghiệm .NET sâu hơn, và mô hình transaction/unit-of-work của EF Core khớp
  với yêu cầu atomic của [ADR-005](005-postgresql-system-of-record.md).
- **Dapper thay EF Core** — loại bỏ như lựa chọn mặc định: cần change tracking và transaction thuận tiện; vẫn dùng SQL thô
  ở những truy vấn EF diễn đạt kém.
- **SQL Server** — loại bỏ: partial (filtered) index, `jsonb` và exclusion constraint của PostgreSQL được dùng trực tiếp
  trong canonical schema.

## Hệ quả

- Migration đầu tiên chỉ sinh được sau khi cài .NET 10 SDK và review model/schema drift.
- Phụ thuộc `jsonb`, partial unique index và `timestamptz` khiến việc đổi sang database khác là một thay đổi lớn, không
  phải đổi connection string.
