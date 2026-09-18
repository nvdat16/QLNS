# ADR-011 — Xác thực và quản trị tài khoản làm nội bộ

- **Trạng thái:** Accepted 2026-09-18
- **Owner:** Architect
- **Liên quan:** [ADR-003](003-backend-enforces-authorization.md), [ADR-010](010-delivery-scope-two-pillars.md)

## Bối cảnh

Baseline đầu tiên để trống Identity Provider ("chưa chọn provider"). Hệ quả là hệ thống có đầy đủ authorization nhưng
**không có đường đăng nhập nào** ngoài endpoint `/dev/token` chỉ chạy ở môi trường Development. Chọn một IdP bên ngoài lại
là một quyết định mua sắm chưa có.

## Quyết định

Đưa hai chức năng lá *Account Management* và *Roles/Permissions & Data Access Scope* của trụ cột System Administration vào
phạm vi, và tự phát hành phiên:

- Mật khẩu băm **PBKDF2-HMAC-SHA512**, 210 000 vòng, salt 128-bit mỗi mật khẩu, tham số nằm trong chính chuỗi hash
  (`user_credentials.password_hash`) để nâng số vòng không cần migration.
- Access token **JWT HS256** do chính API phát hành, ký bằng `Authentication:Jwt:SigningKey`, mặc định sống 30 phút.
- Refresh token **dùng một lần**, chỉ lưu bản băm SHA-256 trong `refresh_tokens`, có phát hiện replay.
- Ma trận vai trò → permission là **dữ liệu tham chiếu** trong `roles` / `role_permissions`, nạp từ `seed_roles.sql`.

`Authentication:Jwt:SigningKey` là bắt buộc: thiếu nó host dừng ngay khi khởi động chứ không chạy ở trạng thái nửa vời.
Cố tình **không** có nhánh dự phòng sang một OIDC authority bên ngoài.

## Phương án đã cân nhắc

- **Tích hợp Keycloak / Entra ID** — loại bỏ: cần thêm một thành phần vận hành và một quyết định mua sắm chưa có.
- **Chỉ làm đăng nhập, cấp tài khoản bằng SQL tay** — loại bỏ: không có vết audit cho hành vi nâng quyền.

## Hệ quả

1. Hệ thống nay tự chịu trách nhiệm lưu mật khẩu. Giảm rủi ro bằng PBKDF2 210 000 vòng, khoá tạm 15 phút sau 5 lần sai, và
   ghi audit mọi lần đăng nhập kể cả thất bại (`result = 'rejected'`).
2. Access token stateless **không thu hồi được**, nên vòng đời 30 phút chính là giới hạn trên của việc thu hồi quyền;
   refresh token là tạo tác thu hồi được.
3. `Authentication:Jwt:SigningKey` trở thành secret hạng nhất của hệ thống.
4. Claim trong token giữ **nguyên** hình dạng cũ (`qlns_user_id`, `qlns_employee_id`, `data_scope`, `department_id`,
   `permission`), nên không module nghiệp vụ nào phải sửa. Nếu sau này federation với IdP ngoài, chỉ cần provider phát hành
   đúng bộ claim đó và thay cụm `/api/v1/auth/*`; phần authorization không đổi. Vì federation thay cả cụm endpoint đăng
   nhập chứ không phải một dòng cấu hình, nó phải là một ADR mới.
5. Thêm bốn bảng vào canonical schema (delta v1.2): `roles`, `role_permissions`, `user_credentials`, `refresh_tokens`;
   `user_roles.role_code` trở thành khóa ngoại tới `roles(code)`.

## Chưa làm

MFA, quên mật khẩu qua email (hiện chỉ có admin đặt lại), rate limit theo IP trước `POST /auth/login` (thuộc reverse proxy).
