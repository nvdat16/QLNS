# ADR-005 — PostgreSQL là system of record, migration có version

- **Trạng thái:** Proposed — **code đã đi theo quyết định này**, chờ Project Owner xác nhận
- **Owner:** chưa có
- **Liên quan:** [ADR-009](009-dotnet-10-efcore-postgresql.md), constraint C3, quality goal Q2

## Bối cảnh

Dữ liệu nhân sự cần ACID thật: một offer được chấp nhận phải tạo nhân viên, hợp đồng và checklist onboarding hoặc không
tạo gì cả. Nhiều invariant quan trọng nhất là *"chỉ một bản ghi đang mở"* — một offer mở mỗi đơn, một hợp đồng chính đang
hiệu lực, một case thôi việc đang mở.

## Quyết định

PostgreSQL là nguồn sự thật duy nhất cho dữ liệu nghiệp vụ. Các invariant trên được thực thi bằng **partial unique index**
ở database (`ux_offers_one_open_per_application`, `ux_contracts_primary_active`, `ux_offboarding_open_case`,
`ux_probation_review_contract`), không chỉ bằng kiểm tra ở tầng ứng dụng. Cập nhật có tranh chấp dùng conditional update
theo `version`.

`database/schema.sql` là canonical contract **tạm thời**, cho tới khi EF Core migration đầu tiên được sinh và phê duyệt.
Khi đó migration trở thành nguồn triển khai và `schema.sql` phải được kiểm tra drift trong CI. Migration chạy như một
release step riêng; không dùng ORM auto-create ở môi trường dùng chung.

## Phương án đã cân nhắc

- **Chỉ kiểm tra invariant ở tầng ứng dụng** — loại bỏ: hai request đồng thời sẽ cùng vượt qua kiểm tra rồi cùng ghi.
- **Giữ `schema.sql` làm nguồn vĩnh viễn, không dùng migration** — loại bỏ: không nâng cấp được database đang có dữ liệu.

## Hệ quả

- Các invariant mạnh nhất **không thể** verify bằng repository giả lập, nên `tests/Qlns.IntegrationTests` trên PostgreSQL
  thật là điều kiện bắt buộc để một operation đạt `implemented`. Project này chưa tồn tại — đó là rủi ro R2.
- Bốn delta v1.1 và bốn delta v1.2 phải được mang nguyên vẹn sang migration đầu tiên (xem `database/README.md` §2.6, §2.7).
