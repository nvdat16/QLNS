# ADR-007 — Ports/adapters, transactional outbox và reliable delivery

- **Trạng thái:** Proposed — **code đã đi theo quyết định này**, chờ Project Owner xác nhận
- **Owner:** chưa có
- **Liên quan:** [ADR-002](002-modular-monolith.md), [ADR-005](005-postgresql-system-of-record.md), constraint C5, quality goal Q7

## Bối cảnh

Hệ thống phải gửi email, lời mời lịch (`.ics`), thư mời nhận việc và thông báo onboarding; phải lưu CV cùng tài liệu hợp
đồng vào object storage; phải quét mã độc và bóc tách CV. Không được dùng distributed transaction với các provider này (C5).

## Quyết định

Mọi hệ thống ngoài nằm sau một **port** khai báo ở `Qlns.BusinessLogic` (`IDocumentStorage`, `IMalwareScanner`,
`IResumeParser`, `IOfferResponseTokenService`, `IAccessTokenIssuer`, …), adapter nằm ở `Qlns.DataAccess`.

Side effect ra ngoài **không** được gọi trong transaction nghiệp vụ. Thay vào đó, command ghi một dòng `outbox_messages`
cùng transaction với thay đổi nghiệp vụ; worker claim và gửi sau khi đã commit, có idempotency key, timeout, retry hữu hạn
và dead-letter để đối soát.

## Phương án đã cân nhắc

- **Gọi provider trực tiếp trong service** — loại bỏ: provider timeout sẽ hoặc rollback một thay đổi nghiệp vụ đã hợp lệ,
  hoặc để lại trạng thái đã commit mà thông báo không bao giờ được gửi.
- **Message broker riêng (RabbitMQ/Kafka) ngay từ đầu** — loại bỏ ở giai đoạn này: thêm một thành phần vận hành trong khi
  outbox trên chính PostgreSQL đã đủ cho tải mục tiêu; vẫn để ngỏ nếu tách microservices.

## Hệ quả

- Business state và side effect nhất quán cuối cùng, không nhất quán tức thời — UI phải thể hiện đúng điều đó.
- Toàn bộ adapter hiện tại là **development adapter** (`FileSystemDocumentStorage`, `DevelopmentOnlyMalwareScanner`,
  `DevelopmentOnlyResumeParser`); phải thay trước bất kỳ môi trường dùng chung nào.
- `Qlns.Worker` chưa tồn tại, nên ba điểm vào worker và outbox dispatcher hiện chỉ gọi được từ test.
