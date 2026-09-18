# QLNS API Reference

Tài liệu này diễn giải các operation trong [`openapi.yaml`](openapi.yaml) cho hai phân hệ được chọn triển khai trước: **Recruitment (ATS)** và **Core HR** (bao gồm nhánh con Contracts).

**Phạm vi:** các chức năng lá được **in đậm** dưới Recruitment và Core HR của [`topdown-approach.png`](../../topdown-approach.png) — xem thêm [README · Functional architecture](../../README.md#2-delivery-scope--seven-pillars-two-selected) — cộng phân hệ **Identity & Access (ADM)**: đăng nhập bằng mật khẩu, làm mới phiên và quản trị tài khoản/vai trò do chính API này đảm nhiệm, không dùng Identity Provider bên ngoài. Nằm ngoài phạm vi và **không** có endpoint trong tài liệu này: Reports & Analytics (kèm xuất báo cáo), các chức năng System Administration còn lại (tra cứu audit log, theo dõi delivery, cấu hình integration/notification/approval), Performance Management, Compensation & Benefits, Attendance & Leave Management, cùng bốn chức năng không in đậm nằm trong hai phân hệ được chọn: Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart và Suspension & Return to Work. Contract của Attendance & Leave được giữ tại [docs/deferred/attendance_leave/openapi_attendance_leave.yaml](../deferred/attendance_leave/openapi_attendance_leave.yaml).

> OpenAPI là nguồn contract chính thức. Khi nội dung mô tả ở đây khác OpenAPI, ưu tiên `openapi.yaml`.

## 1. Quy ước chung

- Base URL local: `http://localhost:5000`.
- Base path: `/api/v1`.
- API nội bộ yêu cầu `Authorization: Bearer <JWT>` và luôn kiểm tra permission, data scope và field scope ở backend.
- Access token do `POST /api/v1/auth/login` phát hành, mặc định sống 30 phút; làm mới bằng `POST /api/v1/auth/refresh` với refresh token dùng một lần. Ba endpoint `login`, `refresh`, `logout` là endpoint công khai (không cần bearer).
- API ứng viên phản hồi Offer dùng `X-Offer-Token` thay cho JWT.
- Resource có thể thay đổi trả `ETag`, ví dụ `"4"`. Lệnh cập nhật phải gửi lại `If-Match: "4"`.
- Command có khả năng retry tạo dữ liệu yêu cầu `Idempotency-Key`, dài từ 16 đến 128 ký tự.
- Danh sách dùng `page`, `pageSize`; `pageSize` tối đa 100.
- Lỗi trả `application/problem+json`, gồm `type`, `title`, `status`, `correlationId` và có thể có `code`, `detail`, `errors`.

### Mã lỗi HTTP chung

| Mã | Ý nghĩa |
|---|---|
| `400` | Request sai cú pháp, action không hỗ trợ hoặc `If-Match` sai định dạng. |
| `401` | Thiếu hoặc sai thông tin xác thực. |
| `403` | Không có permission, data scope hoặc field access cần thiết. |
| `404` | Resource không tồn tại hoặc nằm ngoài phạm vi được phép xem. |
| `409` | Xung đột version, workflow, dữ liệu trùng hoặc thiếu điều kiện nghiệp vụ. |
| `413` | File vượt giới hạn dung lượng. |
| `415` | Định dạng file không được hỗ trợ. |
| `422` | Dữ liệu đúng cú pháp nhưng không hợp lệ về ngữ nghĩa. |

## 2. Identity & Access (ADM)

Phân hệ định danh do hệ thống tự quản: mật khẩu lưu dưới dạng băm PBKDF2-HMAC-SHA512 (210.000 vòng, salt riêng cho từng mật khẩu), refresh token chỉ lưu bản băm SHA-256 và dùng **một lần**. Mọi lần đăng nhập — thành công hay thất bại — đều ghi `audit_logs` trong cùng transaction với thay đổi trạng thái.

### 2.1. Phiên đăng nhập (ADM-01)

#### `POST /api/v1/auth/login`

- **Mục đích:** Đổi email + mật khẩu lấy access token và refresh token.
- **Body:** `LoginRequest` — `email`, `password`.
- **Kết quả:** `Session` gồm `accessToken`, `expiresIn`, `refreshToken` và `user` (`AuthenticatedIdentity`: vai trò, permission, data scope).
- **Công khai:** không cần bearer token.
- **Lỗi 401 kèm `code`:**
  - `admin.auth.invalid_credentials` — email không tồn tại, không có credential nội bộ, hoặc sai mật khẩu. Ba trường hợp trả về **giống nhau**; email lạ vẫn được verify với một hash giả để thời gian phản hồi không tiết lộ tài khoản nào tồn tại.
  - `admin.auth.account_locked` — sai 5 lần liên tiếp, khoá 15 phút; problem details kèm `retryAfterSeconds`.
  - `admin.auth.account_disabled` — tài khoản bị vô hiệu hoá; chỉ báo **sau khi** mật khẩu đã đúng.
- **Trường hợp phải đổi mật khẩu:** nếu `must_change_password` đang bật, response **không có** `refreshToken` và access token **không mang permission nào**, `user.passwordChangeRequired = true`. Phiên đó chỉ gọi được `change-password`.
- **Yêu cầu:** `ADM-01.1`.

#### `POST /api/v1/auth/refresh`

- **Mục đích:** Luân chuyển refresh token thành phiên mới.
- **Body:** `RefreshTokenRequest` — `refreshToken`.
- **Kết quả:** `Session` mới. Permission và data scope được **đọc lại từ database**, nên vai trò bị thu hồi hết hiệu lực trong vòng tối đa một chu kỳ access token.
- **Quy tắc:** token cũ bị thu hồi (`rotated`) và liên kết tới token kế nhiệm. Nếu trình một token **đã bị thu hồi**, hệ thống coi là token bị đánh cắp và thu hồi **toàn bộ** refresh token của tài khoản đó (`reuse_detected`).
- **Lỗi:** `401 admin.auth.invalid_refresh_token`, `401 admin.auth.account_disabled`, `401 admin.auth.password_change_required`.
- **Yêu cầu:** `ADM-01.2`.

#### `POST /api/v1/auth/logout`

- **Mục đích:** Thu hồi refresh token đang giữ.
- **Kết quả:** luôn `204`, dù token có tồn tại hay không — không để endpoint này trở thành công cụ dò token hợp lệ.
- **Lưu ý:** access token vẫn dùng được cho tới khi hết hạn; đây là cái giá đã biết của bearer token stateless.
- **Yêu cầu:** `ADM-01.2`.

#### `GET /api/v1/auth/me`

- **Mục đích:** Trả về danh tính hiện tại, **đọc lại từ database** thay vì tin vào claim trong token.
- **Kết quả:** `AuthenticatedIdentity`.
- **Yêu cầu:** `ADM-01.1`.

#### `POST /api/v1/auth/change-password`

- **Mục đích:** Người dùng tự đổi mật khẩu.
- **Body:** `ChangePasswordRequest` — `currentPassword`, `newPassword`.
- **Quy tắc mật khẩu:** tối thiểu 10 ký tự, tối đa 128, kết hợp ít nhất 3 trong 4 nhóm (chữ thường, chữ hoa, số, ký tự đặc biệt), không chứa phần trước `@` của email, không trùng mật khẩu hiện tại.
- **Bắt buộc nhập mật khẩu hiện tại** dù đã authenticated: access token bị đánh cắp một mình không được phép chiếm tài khoản.
- **Kết quả:** `204`. Toàn bộ refresh token của người dùng bị thu hồi (`password_changed`) nên các thiết bị khác phải đăng nhập lại.
- **Lỗi:** `401 admin.auth.invalid_credentials`, `422` khi mật khẩu mới không hợp lệ, `409` khi `user_credentials.version` đã đổi.
- **Yêu cầu:** `ADM-01.3`.

### 2.2. Quản trị tài khoản & vai trò (ADM-02)

Yêu cầu permission `admin.user.read` để đọc, `admin.user.manage` để thay đổi, `admin.role.read` để đọc danh mục vai trò.

#### `GET /api/v1/admin/users`

- **Query:** `page`, `pageSize`, `search` (tên hoặc email), `status` (`active` | `disabled`), `roleCode`.
- **Kết quả:** `UserAccountPage`. Mỗi `UserAccount` gồm vai trò kèm phạm vi, `hasCredential`, `mustChangePassword`, `lastLoginAt`, `lockedUntil` — **không bao giờ** trả password hash hay refresh token.
- **Yêu cầu:** `ADM-02.1`.

#### `POST /api/v1/admin/users`

- **Body:** `CreateUserAccountRequest` — `email`, `displayName`, `initialPassword`, `employeeId` (không bắt buộc), `roles`.
- **Quy tắc:** email được chuẩn hoá về chữ thường và phải chưa được dùng; `external_subject` sinh theo mẫu `local|<email>`; mật khẩu do admin đặt nên **luôn** bật `must_change_password`; `employeeId` liên kết `employees.user_id` và bị từ chối nếu nhân viên đã có tài khoản.
- **Kết quả:** `201` kèm `ETag` và `Location`.
- **Lỗi:** `409 admin.user.email_taken`, `409 admin.user.unknown_role` (kèm `unknownRoles` / `unknownDepartments`), `409 admin.user.employee_already_linked`, `422` cho email/mật khẩu/vai trò sai định dạng.
- **Yêu cầu:** `ADM-02.1`.

#### `GET /api/v1/admin/users/{userId}` · `PUT /api/v1/admin/users/{userId}`

- `PUT` cần `If-Match` theo `users.version`, body `UserAccountWrite` (`email`, `displayName`). Không có thay đổi thực tế thì không ghi gì và trả lại trạng thái hiện tại.
- **Yêu cầu:** `ADM-02.1`.

#### `POST /api/v1/admin/users/{userId}/enable` · `/disable`

- Cần `If-Match`. `disable` thu hồi toàn bộ refresh token của tài khoản.
- **Idempotent:** tài khoản đã ở trạng thái đích được trả về nguyên trạng, bỏ qua `If-Match`.
- **Lỗi:** `403 admin.user.self_management_forbidden` — admin không được đổi trạng thái tài khoản của chính mình.
- **Yêu cầu:** `ADM-02.2`.

#### `POST /api/v1/admin/users/{userId}/password-reset`

- **Body:** `ResetUserPasswordRequest` — `newPassword`.
- Đặt mật khẩu tạm, bật `must_change_password`, xoá bộ đếm khoá và thu hồi toàn bộ refresh token. Mật khẩu **không** được trả lại trong response; phải chuyển cho người dùng qua kênh an toàn ngoài hệ thống.
- **Lỗi:** `403 admin.user.self_management_forbidden` — tự đặt lại mật khẩu thì dùng `change-password`.
- **Yêu cầu:** `ADM-02.3`.

#### `PUT /api/v1/admin/users/{userId}/roles`

- **Body:** `RoleGrantsRequest` — danh sách `RoleGrant` (`roleCode`, `dataScopeType`, `dataScopeId`). Đây là **trạng thái đích đầy đủ**: vai trò không nằm trong danh sách sẽ bị xoá; danh sách rỗng để lại tài khoản đăng nhập được nhưng không có quyền nào.
- **Quy tắc:** `dataScopeType = department` bắt buộc `dataScopeId > 0` và phòng ban phải tồn tại; hai phạm vi còn lại bắt buộc `dataScopeId = 0` (đúng theo `ck_user_roles_scope`). Vai trò phải có trong `roles` và `is_assignable = true`.
- Cần `If-Match` theo `users.version` — chính version này tuần tự hoá hai admin sửa cùng tài khoản dù dữ liệu thay đổi nằm ở `user_roles`.
- **Lỗi:** `403 admin.user.self_management_forbidden` — admin không tự sửa vai trò của mình.
- **Yêu cầu:** `ADM-02.2`.

#### `GET /api/v1/admin/roles`

- Trả danh mục vai trò kèm permission của từng vai trò. **Chỉ đọc**: ma trận vai trò → permission là dữ liệu tham chiếu triển khai qua [`database/seed_roles.sql`](../../database/seed_roles.sql), sửa đổi phải đi qua review chứ không qua API.
- **Yêu cầu:** `ADM-02.2`.

## 3. Recruitment

### 3.1. Requisitions

#### `GET /api/v1/recruitment/requisitions`

- **Mục đích:** Tìm kiếm requisition trong data scope của người dùng.
- **Query:** `page`, `pageSize`, `search`, `departmentId`, `status`, `sort`.
- **Sort hợp lệ:** `createdAt`, `-createdAt`, `closingDate`, `-closingDate`.
- **Kết quả:** `RequisitionPage` gồm danh sách và metadata phân trang.
- **Yêu cầu:** `REC-01.1`, `REC-01.2`.

#### `POST /api/v1/recruitment/requisitions`

- **Mục đích:** Tạo requisition ở trạng thái `draft`.
- **Body:** `RequisitionWrite`; bắt buộc `title`, `departmentId`, `employmentType`, `targetHeadcount`.
- **Kiểm tra:** headcount lớn hơn 0, salary min không lớn hơn salary max, phòng ban/vị trí phải hợp lệ.
- **Kết quả:** `201`, `Location`, `ETag` và `Requisition` vừa tạo.
- **Yêu cầu:** `REC-01.1`.

#### `GET /api/v1/recruitment/requisitions/{requisitionId}`

- **Mục đích:** Lấy chi tiết một requisition.
- **Kết quả:** `Requisition` và `ETag` hiện tại.
- **Bảo mật:** Resource ngoài data scope được xử lý như không tìm thấy.
- **Yêu cầu:** `REC-01.1`, `REC-01.2`.

#### `PUT /api/v1/recruitment/requisitions/{requisitionId}`

- **Mục đích:** Thay thế dữ liệu có thể sửa của requisition `draft` hoặc requisition đã bị trả lại.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `RequisitionWrite` đầy đủ.
- **Kết quả:** Requisition mới và `ETag` mới.
- **Xung đột:** Trạng thái không cho sửa hoặc version đã thay đổi trả `409`.

#### `POST /api/v1/recruitment/requisitions/{requisitionId}/{action}`

- **Mục đích:** Thực hiện command workflow cho requisition.
- **Action:** `submit`, `approve`, `reject`, `publish`, `close`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `RequisitionAction`; `reject` bắt buộc `reason`. `publish` không nhận danh sách kênh — bài đăng chỉ hiển thị trên kênh careers mặc định.
- **Workflow:** `draft → pending_approval → approved → active_recruiting → closed/cancelled`; reject trả requisition về trạng thái có thể chỉnh sửa.
- **Side effect:** Publish ghi outbox để phát hành bài đăng sau khi transaction thành công.
- **Phê duyệt:** Là quyết định của HR Manager. `targetHeadcount`, `salaryMin`, `salaryMax` là dữ liệu khai báo của requisition, server không dùng chúng làm điều kiện chặn.

### 3.2. Candidate intake và CV

#### `POST /api/v1/recruitment/resumes`

- **Mục đích:** Tiếp nhận CV, quét mã độc và bắt đầu bóc tách bất đồng bộ.
- **Content-Type:** `multipart/form-data`.
- **Body:** `file`, `requisitionId`, `privacyNoticeVersion`, `consented=true`.
- **Giới hạn:** PDF, DOC hoặc DOCX; tối đa 10 MiB.
- **Kết quả:** `202 Accepted`, `Location` và `CandidateIntake`.
- **An toàn:** File không được lưu chính thức nếu chưa qua kiểm tra an toàn.

#### `GET /api/v1/recruitment/intakes/{intakeId}`

- **Mục đích:** Theo dõi trạng thái scan, parse và phát hiện trùng.
- **Trạng thái:** `scanning`, `parsing`, `awaiting_confirmation`, `duplicate_review`, `completed`, `rejected`, `failed`.
- **Kết quả:** Dữ liệu candidate gợi ý, confidence theo trường và danh sách candidate có khả năng trùng.

#### `POST /api/v1/recruitment/intakes/{intakeId}/confirm`

- **Mục đích:** Xác nhận dữ liệu bóc tách và tạo application.
- **Header:** Bắt buộc `Idempotency-Key`.
- **Body:** `candidate`, `source`; gửi `existingCandidateId` khi chọn liên kết với candidate đã tồn tại.
- **Kết quả:** `201`, application mới, `Location` và `ETag`.
- **Xung đột:** Nếu có candidate nghi trùng nhưng chưa được xử lý, API trả `409`.
- **Tính nguyên tử:** Candidate/resume/application phải được tạo hoặc liên kết trong cùng một transaction.

### 3.3. Recruitment pipeline

#### `GET /api/v1/recruitment/pipeline`

- **Mục đích:** Lấy dữ liệu Kanban theo requisition.
- **Query:** Bắt buộc `requisitionId`; tùy chọn `search`, `stage`, `minimumAiScore`, `page`, `pageSize`.
- **Kết quả:** Các `PipelineColumn`, tổng số card và AI score trung bình theo stage.
- **Yêu cầu:** `REC-03.1`.

#### `GET /api/v1/recruitment/applications/{applicationId}`

- **Mục đích:** Lấy trạng thái hiện tại của application.
- **Kết quả:** `RecruitmentApplication` và `ETag`.

#### `POST /api/v1/recruitment/applications/{applicationId}/advance`

- **Mục đích:** Chuyển application đúng một stage tiến về phía trước.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `targetStage`, tùy chọn `reason`.
- **Điều kiện:** Không nhảy/lùi stage; vào `tech_interview` cần lịch hợp lệ; vào `offer_letter` cần evaluation đạt policy.
- **Kết quả:** Application và `ETag` mới.
- **Tính nguyên tử:** Update application, stage event và audit log trong cùng transaction.

#### `POST /api/v1/recruitment/applications/{applicationId}/{terminalAction}`

- **Mục đích:** Kết thúc application ngoài luồng advance.
- **Action:** `reject` hoặc `withdraw`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Bắt buộc `reason`.
- **Kết quả:** Application ở stage `rejected` hoặc `withdrawn` cùng `ETag` mới.
- **Side effect:** Có thể tạo outbox gửi email cảm ơn; lỗi provider không rollback trạng thái đã commit.

### 3.4. Interviews

#### `GET /api/v1/recruitment/interviews`

- **Mục đích:** Tra cứu lịch phỏng vấn theo phạm vi người dùng.
- **Query:** `page`, `pageSize`, `applicationId`, `interviewerUserId`, `from`, `to`, `status`.
- **Kết quả:** `InterviewPage`.

#### `POST /api/v1/recruitment/interviews`

- **Mục đích:** Xếp lịch phỏng vấn và tạo thông báo/lịch mời.
- **Body:** `applicationId`, `interviewType`, `startsAt`, `endsAt`, `timezone` (IANA), `interviewerUserIds` (hội đồng, lưu ở `interview_panelists`; người đầu tiên là lead), `location` hoặc `meetingUrl`.
- **Kiểm tra:** `endsAt > startsAt`; application đang ở `ai_screening`, `tech_interview` hoặc `executive_round`; không trùng giờ interviewer (`recruitment.interview.interviewer_conflict`) hoặc phòng họp (`recruitment.interview.location_conflict`).
- **Kết quả:** `201`, `Interview`, `Location`, `ETag`.
- **Side effect:** Email/calendar invitation được xử lý qua outbox.

#### `POST /api/v1/recruitment/interviews/{interviewId}/{action}`

- **Mục đích:** Thay đổi trạng thái/lịch phỏng vấn.
- **Action:** `reschedule`, `complete`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Reschedule dùng thời gian/timezone/location mới; cancel yêu cầu `reason` theo policy.
- **Kết quả:** Interview và `ETag` mới.
- **Side effect:** Gửi cập nhật hoặc hủy lịch sau khi commit.

### 3.5. Evaluations

#### `GET /api/v1/recruitment/interviews/{interviewId}/evaluations`

- **Mục đích:** Lấy các scorecard được phép xem.
- **Bảo mật:** Áp dụng blind-evaluation; evaluator chưa nộp không được thấy đánh giá của người khác.
- **Kết quả:** Mảng `Evaluation`.

#### `POST /api/v1/recruitment/interviews/{interviewId}/evaluations`

- **Mục đích:** Nộp scorecard bất biến cho một buổi phỏng vấn.
- **Header:** Bắt buộc `Idempotency-Key`.
- **Body:** Điểm technical, communication, problem solving, teamwork; recommendation và feedback.
- **Điểm:** Từ 0 đến 5, bước 0.5. `overallScore` do server tính.
- **Kết quả:** `201`, `Evaluation`, `Location`, `ETag`.
- **Phân quyền:** Chỉ interviewer được gán vào buổi phỏng vấn mới được nộp.

#### `POST /api/v1/recruitment/evaluations/{evaluationId}/unlock`

- **Mục đích:** Mở khóa bằng cách tạo version đánh giá mới có audit, không sửa lịch sử cũ.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Bắt buộc `reason`.
- **Phân quyền:** Chỉ HR Manager hoặc quyền tương đương.
- **Kết quả:** Evaluation version mới và `ETag` mới.

### 3.6. Offers

#### `GET /api/v1/recruitment/offers`

- **Mục đích:** Tra cứu Offer trong data scope.
- **Query:** `page`, `pageSize`, `applicationId`, `status`.
- **Kết quả:** `OfferPage`.

#### `POST /api/v1/recruitment/offers`

- **Mục đích:** Tạo Offer `draft`.
- **Body:** Application, lương cơ bản, bonus, allowance, currency, loại việc làm, ngày bắt đầu, hạn phản hồi và template version.
- **Điều kiện:** Application đã qua vòng phỏng vấn; chỉ một Offer mở cho mỗi application.
- **Kết quả:** `201`, `Offer`, `Location`, `ETag`.

#### `GET /api/v1/recruitment/offers/{offerId}`

- **Mục đích:** Lấy một Offer được phép xem.
- **Kết quả:** `Offer` và `ETag`.

#### `POST /api/v1/recruitment/offers/{offerId}/{action}`

- **Mục đích:** Điều khiển workflow Offer nội bộ.
- **Action:** `approve`, `send`, `extend`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `extend` dùng `expirationDate`; cancel yêu cầu `reason`.
- **Workflow:** `draft → approved → sent`; sau đó candidate phản hồi hoặc hệ thống chuyển `expired`.
- **Side effect:** Send tạo token phản hồi và outbox gửi email.

#### `POST /api/v1/recruitment/offers/{offerId}/response`

- **Mục đích:** Candidate chấp nhận hoặc từ chối Offer.
- **Xác thực:** `X-Offer-Token` — token HMAC do action `send` phát hành và gửi qua outbox; ở môi trường Development có thể lấy bằng `GET /dev/offer-token?offerId=`.
- **Header:** Bắt buộc `Idempotency-Key`.
- **Body:** `decision=accept|decline`; decline nên có `reason`.
- **Kết quả:** `OfferResponseResult`, gồm Offer status và các ID employee/contract/onboarding khi accept.
- **Tính nguyên tử:** Accept tạo tối đa một employee, một hợp đồng ban đầu và một bộ onboarding task.
- **Retry:** Cùng idempotency key trả lại kết quả trước với `replayed=true`.

## 4. Core HR

### 4.1. Employees

#### `GET /api/v1/employees`

- **Mục đích:** Tìm kiếm danh bạ nhân viên theo data scope.
- **Query:** `page`, `pageSize`, `search`, `departmentId`, `positionId`, `status`, `sort`.
- **Sort hợp lệ:** `name`, `-name`, `employeeCode`, `hireDate`.
- **Kết quả:** `EmployeePage`; không trả trường nhạy cảm trong danh sách.

#### `GET /api/v1/employees/{employeeId}`

- **Mục đích:** Xem hồ sơ nhân viên.
- **Kết quả:** `EmployeeDetail` và `ETag`.
- **Field policy:** Đồng nghiệp chỉ thấy thông tin công việc công khai; trường cá nhân/nhạy cảm được ẩn hoặc mask theo quyền.

#### `PATCH /api/v1/employees/{employeeId}/profile`

- **Mục đích:** Cập nhật các trường hồ sơ cá nhân được phép.
- **Content-Type:** `application/merge-patch+json`.
- **Header:** Bắt buộc `If-Match`.
- **Trường cho phép:** personal email, phone, temporary address, emergency contact.
- **Trường bị cấm:** work email, department, position, manager, status, salary và employee code.
- **Kết quả:** `EmployeeDetail` và `ETag` mới.

### 4.2. Organization

#### `GET /api/v1/organization/departments`

- **Mục đích:** Lấy danh mục phòng ban.
- **Kết quả:** Mảng `Department`.

#### `POST /api/v1/organization/departments`

- **Mục đích:** Tạo phòng ban.
- **Body:** `code`, `name`, tùy chọn parent, cost center, description.
- **Kiểm tra:** Code duy nhất; parent hợp lệ; không tạo vòng lặp phân cấp.
- **Kết quả:** `201`, Department, `Location`, `ETag`.

#### `PUT /api/v1/organization/departments/{departmentId}`

- **Mục đích:** Thay thế metadata phòng ban.
- **Header:** Bắt buộc `If-Match`.
- **Kết quả:** Department và `ETag` mới.

#### `DELETE /api/v1/organization/departments/{departmentId}`

- **Mục đích:** Xóa phòng ban rỗng.
- **Header:** Bắt buộc `If-Match`.
- **Điều kiện:** Không còn phòng ban con, nhân viên hoặc requisition đang mở.
- **Kết quả:** `204 No Content`; vi phạm điều kiện trả `409`.

#### `GET /api/v1/organization/positions`

- **Mục đích:** Lấy danh mục chức danh/vị trí.
- **Kết quả:** Mảng `Position`.

#### `POST /api/v1/organization/positions`

- **Mục đích:** Tạo position definition.
- **Body:** `code`, `name`, tùy chọn `level`, `description`.
- **Kết quả:** `201`, Position, `Location`, `ETag`.

#### `PUT /api/v1/organization/positions/{positionId}`

- **Mục đích:** Thay thế metadata position.
- **Header:** Bắt buộc `If-Match`.
- **Kết quả:** Position và `ETag` mới.

### 4.3. Onboarding

#### `GET /api/v1/onboarding/tasks`

- **Mục đích:** Tra cứu task onboarding, gồm task quá hạn.
- **Query:** `page`, `pageSize`, `employeeId`, `assignedToUserId`, `status`, `overdue`.
- **Kết quả:** `OnboardingTaskPage`.

#### `PUT /api/v1/onboarding/tasks/{taskId}`

- **Mục đích:** Cập nhật tên, mô tả, người phụ trách và hạn task.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `OnboardingTaskWrite`.
- **Kết quả:** Task và `ETag` mới.

#### `POST /api/v1/onboarding/tasks/{taskId}/{action}`

- **Mục đích:** Chuyển trạng thái task.
- **Action:** `start`, `complete`, `reopen`.
- **Header:** Bắt buộc `If-Match`.
- **Workflow:** `pending → in_progress → completed`.
- **Quyền:** Reopen chỉ dành cho HR Officer/HR Manager và phải có lý do.

### 4.4. Employee events

#### `GET /api/v1/employees/{employeeId}/events`

- **Mục đích:** Lấy lịch sử biến động bất biến của nhân viên.
- **Query:** `page`, `pageSize`.
- **Kết quả:** `EmployeeEventPage`.

#### `POST /api/v1/employees/{employeeId}/events`

- **Mục đích:** Tạo đề xuất biến động ở trạng thái `draft`.
- **Body:** `eventType`, `effectiveDate`, `beforeData`, `afterData`, `reason`; có thể chỉ ra event được bù bằng `compensatesEventId`.
- **Loại:** promotion, transfer, demotion, salary adjustment, termination, correction.
- **Kiểm tra:** Không có hai event cùng ngày hiệu lực thay đổi cùng một trường.
- **Kết quả:** `201`, EmployeeEvent, `Location`, `ETag`.

#### `POST /api/v1/employee-events/{eventId}/{action}`

- **Mục đích:** Điều khiển workflow biến động.
- **Action:** `submit`, `approve`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Workflow:** `draft → pending_approval → approved → applied`.
- **Bất biến:** Event đã applied không được sửa/xóa; phải tạo compensating event.

### 4.5. Employee documents

#### `GET /api/v1/employees/{employeeId}/documents`

- **Mục đích:** Lấy metadata tài liệu được phép xem.
- **Kết quả:** Mảng `EmployeeDocument`; không trả object key hoặc URL storage công khai.
- **Bảo mật:** Quyền xem phụ thuộc document type và quan hệ với nhân viên.

#### `POST /api/v1/employees/{employeeId}/documents`

- **Mục đích:** Upload và tạo version mới của tài liệu nhân sự.
- **Content-Type:** `multipart/form-data`.
- **Body:** `file`, `documentType`, tùy chọn `retentionUntil`.
- **An toàn:** File phải được kiểm tra type, size, nội dung và malware trước khi lưu private.
- **Kết quả:** `201`, metadata tài liệu và `Location`.

#### `POST /api/v1/employee-documents/{documentId}/download-url`

- **Mục đích:** Tạo signed URL tải tài liệu sau khi kiểm tra quyền.
- **Kết quả:** `SignedDownload` gồm `url` và `expiresAt`, tối đa khoảng 15 phút.
- **Audit:** Truy cập tài liệu nhạy cảm phải được ghi log.

### 4.6. Probation review

#### `GET /api/v1/employees/{employeeId}/probation-review`

- **Mục đích:** Lấy phiếu đánh giá thử việc của hợp đồng thử việc đang hiệu lực.
- **Kết quả:** `ProbationReview` kèm `overdue` và `ETag`.
- **Ràng buộc:** Một hợp đồng thử việc có đúng một phiếu (`ux_probation_review_contract`).

#### `PUT /api/v1/employees/{employeeId}/probation-review`

- **Mục đích:** Nhập hoặc cập nhật nội dung đánh giá; bắt buộc `If-Match`.
- **Body:** `overallScore` (0–5), `strengths`, `improvements`, `recommendedOutcome`.
- **Quy tắc:** Chỉ `reviewerUserId` được gán mới nhập được, người khác trả `403`. Đề xuất `terminated` bắt buộc có `improvements`, thiếu thì trả `422`.

#### `GET /api/v1/probation-reviews`

- **Mục đích:** Tra cứu phiếu đánh giá, hỗ trợ lọc `overdue=true` để lấy danh sách quá hạn.
- **Lưu ý:** Phiếu quá `reviewDueDate` là rủi ro pháp lý, phải được hiển thị riêng trên dashboard.

#### `POST /api/v1/probation-reviews/{reviewId}/{action}`

- **Action:** `decide`, `cancel`, `unlock`; bắt buộc `If-Match`.
- **`decide`:** Yêu cầu `outcome` và `effectiveDate`; thiếu thì trả `422` (`ck_probation_decided`).
- **Tính nguyên tử:** Quyết định và việc tạo `employee_events` tương ứng nằm trong cùng một transaction. Thao tác lặp lại là idempotent — một phiếu chỉ sinh tối đa một sự kiện.
- **Hệ quả:** `confirmed` → `probation_confirmation`; `extended` → `probation_extension`; `terminated` → `termination` và mở một offboarding case.
- **Lưu ý:** `employees.status` **không** đổi ngay khi phê duyệt; chỉ đổi khi sự kiện được áp dụng vào `effectiveDate`.

### 4.7. Offboarding

#### `GET /api/v1/offboarding/cases`

- **Mục đích:** Tra cứu hồ sơ thôi việc theo trạng thái và phòng ban.

#### `POST /api/v1/offboarding/cases`

- **Mục đích:** Mở hồ sơ thôi việc.
- **Tiền điều kiện:** Nhân viên phải đang ở trạng thái `active` hoặc `probation`.
- **Body:** `separationType`, `lastWorkingDate`, `handoverToEmployeeId`, `reason`, tùy chọn `noticeReceivedOn`.
- **Quy tắc:** Nhân viên đã có case đang mở → `409` (`ux_offboarding_open_case`). `handoverToEmployeeId` không được là chính nhân viên thôi việc → `422`.
- **Cảnh báo, không chặn:** Thiếu thời hạn báo trước được trả về ở `noticePeriodShortfallDays` như một cảnh báo, không chặn việc lưu — quyết định thuộc HR Manager và cảnh báo được ghi audit log.

#### `GET /api/v1/offboarding/cases/{caseId}`

- **Kết quả:** Case kèm `blockingTasksOutstanding` và `ETag`.

#### `POST /api/v1/offboarding/cases/{caseId}/{action}`

- **Action:** `approve`, `start`, `complete`, `cancel`; bắt buộc `If-Match`.
- **`approve`:** Sinh `offboarding_tasks` từ template theo 5 nhóm `it`, `admin`, `hr`, `manager`, `finance`. Sinh lại không tạo task trùng (`ux_offboarding_task_template`).
- **`complete`:** Bị từ chối khi còn task `blocksLastWorkingDay` chưa hoàn thành, hoặc `finalSettlementStatus` chưa đạt `paid`/`waived` → `409` kèm danh sách task đang chặn.
- **Hệ quả:** Hủy các đơn nghỉ `approved` bắt đầu sau `lastWorkingDate` và trả lại quỹ. Tài khoản chỉ bị `disabled` đúng `lastWorkingDate`, không sớm hơn.

#### `GET /api/v1/offboarding/cases/{caseId}/tasks`

- **Mục đích:** Lấy checklist bàn giao và thu hồi; hỗ trợ `category` và `blockingOnly`.

#### `POST /api/v1/offboarding/tasks/{taskId}/{action}`

- **Action:** `start`, `complete`, `reopen`; bắt buộc `If-Match`.
- **Quy tắc:** Bỏ qua task chặn cần quyền HR Manager và bắt buộc ghi lý do vào audit log.

## 5. Contracts

### 5.1. Employment contracts

#### `GET /api/v1/contracts`

- **Mục đích:** Tra cứu hợp đồng theo data scope.
- **Query:** `page`, `pageSize`, `employeeId`, `type`, `status`.
- **Bảo mật:** Employee chỉ được xem hợp đồng của chính mình.
- **Kết quả:** `ContractPage`.

#### `POST /api/v1/contracts`

- **Mục đích:** Tạo hợp đồng `draft`.
- **Body:** Employee, số hợp đồng, loại, ngày bắt đầu/kết thúc, salary, currency, notice period và `isPrimary`.
- **Kiểm tra:** Số hợp đồng duy nhất; hợp đồng có thời hạn phải có `endDate > startDate`; không overlap hợp đồng chính đang hiệu lực.
- **Kết quả:** `201`, Contract, `Location`, `ETag`.

#### `GET /api/v1/contracts/expiring`

- **Mục đích:** Lấy các hợp đồng chạm ngưỡng cảnh báo hết hạn.
- **Query:** `asOf`, `withinDays` từ 1 đến 365, `page`, `pageSize`.
- **Kết quả:** `ExpiringContractPage` với `daysRemaining` và `alertLevel`.
- **Ngưỡng mặc định:** Probation 15/7 ngày; fixed-term 45/30 ngày.

#### `GET /api/v1/contracts/{contractId}`

- **Mục đích:** Lấy chi tiết một hợp đồng được phép xem.
- **Kết quả:** `Contract` và `ETag`.

#### `PUT /api/v1/contracts/{contractId}`

- **Mục đích:** Thay thế trường có thể chỉnh sửa của hợp đồng `draft`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `ContractWrite` đầy đủ.
- **Kết quả:** Contract và `ETag` mới.

#### `POST /api/v1/contracts/{contractId}/{action}`

- **Mục đích:** Điều khiển vòng đời hợp đồng.
- **Action:** `approve`, `activate`, `terminate`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Terminate/cancel yêu cầu `reason`; activate có thể nhận `signedAt`; ngoại lệ overlap dùng `allowPrimaryOverlap` và cần quyền HR Manager.
- **Điều kiện activate:** Hợp đồng đã approved và có bằng chứng ký hợp lệ.
- **Kết quả:** Contract và `ETag` mới.

#### `POST /api/v1/contracts/{contractId}/signed-document`

- **Mục đích:** Upload bản PDF đã ký trước khi activate.
- **Content-Type:** `multipart/form-data`.
- **Header:** Bắt buộc `If-Match`.
- **Kết quả:** Contract cập nhật trạng thái tài liệu và `ETag` mới.

#### `POST /api/v1/contracts/{contractId}/download-url`

- **Mục đích:** Tạo signed URL tải hợp đồng đã ký.
- **Bảo mật:** Employee chỉ tải hợp đồng của chính mình; HR vẫn chịu data scope.
- **Kết quả:** `SignedDownload` với thời hạn ngắn.

### 5.2. Contract addenda

#### `GET /api/v1/contracts/{contractId}/addenda`

- **Mục đích:** Lấy các phụ lục của hợp đồng mà không thay đổi nội dung hợp đồng gốc.
- **Kết quả:** Mảng `ContractAddendum`.

#### `POST /api/v1/contracts/{contractId}/addenda`

- **Mục đích:** Tạo phụ lục `draft`.
- **Body:** Số phụ lục, ngày hiệu lực, `beforeTerms`, `afterTerms`, lý do.
- **Kiểm tra:** Hợp đồng gốc tồn tại/còn phù hợp; số phụ lục duy nhất; before/after phải phản ánh thay đổi được phép.
- **Kết quả:** `201`, ContractAddendum, `Location`, `ETag`.

#### `POST /api/v1/contract-addenda/{addendumId}/{action}`

- **Mục đích:** Điều khiển workflow phụ lục.
- **Action:** `submit`, `approve`, `mark-signed`, `make-effective`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Workflow:** `draft → pending_approval → approved → effective`; bản cũ có thể chuyển `superseded`.
- **Side effect:** Khi effective, thay đổi chức danh/lương/phòng ban tạo employee event tương ứng thay vì sửa lịch sử trực tiếp.

#### `POST /api/v1/contract-addenda/{addendumId}/signed-document`

- **Mục đích:** Upload PDF phụ lục đã ký trước khi chuyển `effective`.
- **Content-Type:** `multipart/form-data`.
- **Header:** Bắt buộc `If-Match`.
- **Kết quả:** ContractAddendum và `ETag` mới.

## 6. Operations

Hai endpoint dưới đây là endpoint hạ tầng phục vụ deployment (liveness/readiness probe), không phải chức năng nghiệp vụ trên bản đồ phân rã chức năng.

#### `GET /health/live`

- Probe công khai tối giản cho biết process còn sống; không kiểm tra dependency và không lộ chi tiết nội bộ.

#### `GET /health/ready`

- Probe công khai tối giản cho biết service sẵn sàng nhận traffic.
- Trả `200` khi ready hoặc `503` khi chưa ready; không liệt kê secret/dependency detail.

## 7. Trạng thái triển khai

Mọi operation trong `openapi.yaml` hiện mang `x-implementation-status: code-complete`: đã có controller, authorization policy ở endpoint, business workflow (service + domain), persistence transaction ghi kèm audit và outbox, và unit test cho tầng nghiệp vụ. Source nằm tại `src/backend`, tổ chức theo `Modules/<Module>/<Feature>`; xem [src/backend/README.md](../../src/backend/README.md) để biết feature nào chứa operation nào.

### Ý nghĩa các giá trị `x-implementation-status`

| Giá trị | Ý nghĩa |
| :--- | :--- |
| `proposed` | Chỉ có contract. |
| `code-complete` | Đủ 5/6 phần: controller, policy, workflow, persistence (audit + outbox cùng transaction), unit test. **Chưa có** integration/contract test trên PostgreSQL. |
| `implemented` | `code-complete` cộng integration/contract test chạy trên PostgreSQL thật. |

### Điều kiện còn thiếu để lên `implemented`

| Nhóm | Còn thiếu |
| :--- | :--- |
| Toàn bộ | `tests/Qlns.IntegrationTests` (WebApplicationFactory + Testcontainers) để kiểm chứng partial unique index, conditional update và các truy vấn EF phức tạp (cửa sổ cảnh báo hết hạn, đếm task chặn, subquery user của quản lý); sinh EF Core migration từ `schema.sql` v1.2. |
| Identity & Access | Integration test cho luân chuyển refresh token (bao gồm hai request đồng thời cùng token) và cho thu hồi phiên khi vô hiệu hoá tài khoản; rate limit ở reverse proxy trước `POST /auth/login`; luồng quên mật khẩu qua email (hiện chỉ có admin đặt lại). |
| Recruitment | Worker gửi outbox (email/`.ics`, offer token), worker `ExpireDueOffersAsync`; bộ parser CV thật thay `DevelopmentOnlyResumeParser`; scanner thật thay `DevelopmentOnlyMalwareScanner`. |
| Core HR — Probation, Offboarding | Worker vô hiệu hóa tài khoản đúng `lastWorkingDate` (nhận từ outbox `corehr.offboarding.case_completed`); nguồn cập nhật `finalSettlementStatus` thuộc Payroll (ngoài phạm vi). |
| Contracts | Worker `ExpireDueContractsAsync` và outbox cảnh báo hết hạn có chống trùng theo mốc; object store thật thay `FileSystemDocumentStorage`. |

### Nguyên tắc triển khai từng operation

Mỗi operation phải được triển khai đầy đủ cả sáu phần, không tách rời: controller, authorization policy (permission + data scope), business workflow, persistence transaction (kèm audit và outbox trong cùng transaction), unit test và contract/integration test. Một endpoint trả đúng JSON nhưng chưa có kiểm tra quyền phía server hoặc chưa ghi audit **không được tính là đã triển khai**.
