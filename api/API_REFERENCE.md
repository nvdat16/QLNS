# QLNS API Reference

Tài liệu này diễn giải các operation trong [`openapi.yaml`](openapi.yaml) cho ba phân hệ được chọn triển khai trước: **Core HR** (bao gồm nhánh con Contracts), **Recruitment (ATS)** và **Attendance & Leave**, cùng Reports và Administration.

> OpenAPI là nguồn contract chính thức. Khi nội dung mô tả ở đây khác OpenAPI, ưu tiên `openapi.yaml`.

## 1. Quy ước chung

- Base URL local: `http://localhost:5000`.
- Base path: `/api/v1`.
- API nội bộ yêu cầu `Authorization: Bearer <JWT>` và luôn kiểm tra permission, data scope và field scope ở backend.
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

## 2. Recruitment

### 2.1. Requisitions

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
- **Body:** `RequisitionAction`; `reject` bắt buộc `reason`, `publish` bắt buộc ít nhất một `channel`.
- **Workflow:** `draft → pending_approval → approved → active_recruiting → closed/cancelled`; reject trả requisition về trạng thái có thể chỉnh sửa.
- **Side effect:** Publish ghi outbox để gửi sang job board sau khi transaction thành công.

### 2.2. Candidate intake và CV

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

### 2.3. Recruitment pipeline

#### `GET /api/v1/recruitment/pipeline`

- **Mục đích:** Lấy dữ liệu Kanban theo requisition.
- **Query:** Bắt buộc `requisitionId`; tùy chọn `search`, `stage`, `minimumAiScore`, `page`, `pageSize`.
- **Kết quả:** Các `PipelineColumn`, tổng số card và AI score trung bình theo stage.
- **Yêu cầu:** `REC-03.1`.

#### `GET /api/v1/recruitment/applications/{applicationId}`

- **Mục đích:** Lấy trạng thái hiện tại của application.
- **Kết quả:** `RecruitmentApplication` và `ETag`.
- **Ghi chú:** Đây là query được backend hiện tại triển khai.

#### `POST /api/v1/recruitment/applications/{applicationId}/advance`

- **Mục đích:** Chuyển application đúng một stage tiến về phía trước.
- **Header:** Bắt buộc `If-Match`.
- **Body:** `targetStage`, tùy chọn `reason`.
- **Điều kiện:** Không nhảy/lùi stage; vào `tech_interview` cần lịch hợp lệ; vào `offer_letter` cần evaluation đạt policy.
- **Kết quả:** Application và `ETag` mới.
- **Tính nguyên tử:** Update application, stage event và audit log trong cùng transaction.
- **Ghi chú:** Đây là command được backend hiện tại triển khai.

#### `POST /api/v1/recruitment/applications/{applicationId}/{terminalAction}`

- **Mục đích:** Kết thúc application ngoài luồng advance.
- **Action:** `reject` hoặc `withdraw`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Bắt buộc `reason`.
- **Kết quả:** Application ở stage `rejected` hoặc `withdrawn` cùng `ETag` mới.
- **Side effect:** Có thể tạo outbox gửi email cảm ơn; lỗi provider không rollback trạng thái đã commit.

### 2.4. Interviews

#### `GET /api/v1/recruitment/interviews`

- **Mục đích:** Tra cứu lịch phỏng vấn theo phạm vi người dùng.
- **Query:** `page`, `pageSize`, `applicationId`, `interviewerUserId`, `from`, `to`, `status`.
- **Kết quả:** `InterviewPage`.

#### `POST /api/v1/recruitment/interviews`

- **Mục đích:** Xếp lịch phỏng vấn và tạo thông báo/lịch mời.
- **Body:** `applicationId`, `interviewType`, `startsAt`, `endsAt`, `timezone`, `interviewerUserIds`, `location` hoặc `meetingUrl`.
- **Kiểm tra:** `endsAt > startsAt`; không trùng interviewer hoặc phòng họp.
- **Kết quả:** `201`, `Interview`, `Location`, `ETag`.
- **Side effect:** Email/calendar invitation được xử lý qua outbox.

#### `POST /api/v1/recruitment/interviews/{interviewId}/{action}`

- **Mục đích:** Thay đổi trạng thái/lịch phỏng vấn.
- **Action:** `reschedule`, `complete`, `cancel`.
- **Header:** Bắt buộc `If-Match`.
- **Body:** Reschedule dùng thời gian/timezone/location mới; cancel yêu cầu `reason` theo policy.
- **Kết quả:** Interview và `ETag` mới.
- **Side effect:** Gửi cập nhật hoặc hủy lịch sau khi commit.

### 2.5. Evaluations

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

### 2.6. Offers

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
- **Xác thực:** `X-Offer-Token`.
- **Header:** Bắt buộc `Idempotency-Key`.
- **Body:** `decision=accept|decline`; decline nên có `reason`.
- **Kết quả:** `OfferResponseResult`, gồm Offer status và các ID employee/contract/onboarding khi accept.
- **Tính nguyên tử:** Accept tạo tối đa một employee, một hợp đồng ban đầu và một bộ onboarding task.
- **Retry:** Cùng idempotency key trả lại kết quả trước với `replayed=true`.

## 3. Core HR

### 3.1. Employees

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

### 3.2. Organization

#### `GET /api/v1/organization/chart`

- **Mục đích:** Lấy cây tổ chức gồm phòng ban, quản lý và headcount.
- **Query:** `rootDepartmentId`, `depth` từ 1 đến 10.
- **Kết quả:** Mảng `OrganizationNode` phân cấp.

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

### 3.3. Onboarding

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

### 3.4. Employee events

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

### 3.5. Employee documents

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

### 3.6. Probation review

> Trạng thái: `proposed`.

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

### 3.7. Offboarding

> Trạng thái: `proposed`.

#### `GET /api/v1/offboarding/cases`

- **Mục đích:** Tra cứu hồ sơ thôi việc theo trạng thái và phòng ban.

#### `POST /api/v1/offboarding/cases`

- **Mục đích:** Mở hồ sơ thôi việc.
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

## 4. Contracts

### 4.1. Employment contracts

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

### 4.2. Contract addenda

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

## 5. Reports

#### `GET /api/v1/reports/headcount`

- Trả KPI quân số, active/probation, cơ cấu phòng ban, loại hợp đồng và tỷ lệ hoàn thiện hồ sơ.
- Bắt buộc `from`, `to`; hỗ trợ lọc phòng ban, vị trí và địa điểm.
- Mỗi metric có mã công thức, policy version và thời điểm read model được làm mới.

#### `GET /api/v1/reports/recruitment`

- Trả recruitment funnel, time-to-hire, hiệu quả nguồn và tỷ lệ chấp nhận Offer.
- Bắt buộc `from`, `to`; hỗ trợ lọc phòng ban, vị trí, địa điểm và requisition.
- Dữ liệu luôn được giới hạn theo data scope của người xem.

#### `POST /api/v1/reports/exports`

- Tạo export CSV, XLSX hoặc PDF bất đồng bộ cho báo cáo headcount/recruitment.
- Yêu cầu `Idempotency-Key` và quyền export riêng.
- Server áp dụng field allowlist, data scope, watermark người xuất và audit.

#### `GET /api/v1/reports/exports/{exportId}`

- Theo dõi trạng thái `queued`, `processing`, `completed`, `failed` hoặc `expired`.
- Chỉ người tạo hoặc actor có quyền quản lý export được xem.

#### `POST /api/v1/reports/exports/{exportId}/download-url`

- Tạo signed URL ngắn hạn cho export đã hoàn tất.
- Export chưa hoàn tất/hết hạn trả `409`; truy cập không đúng scope trả `403/404`.

## 6. Attendance và Leave

> **Trạng thái:** `x-implementation-status: discovery-required`. Canonical schema đã có đủ 13 bảng cho nhóm này, nhưng công thức lịch lễ, ca đêm, làm tròn phút, hệ số tăng ca, accrual/carry-over, số dư âm và chuỗi duyệt **phải được HR/Legal chốt** trước khi triển khai. Danh sách quyết định: [Open Decisions — Attendance & Leave](../docs/open_decisions_attendance_leave.md).
>
> Hai chốt chặn kỹ thuật: không tính được bảng công khi `attendance_policies.status = 'draft'`, và không gửi được đơn nghỉ với `leave_types.policy_status = 'draft'`.

### 6.1. Ca làm việc và lịch tuần

#### `GET /api/v1/attendance/shifts`

- Lấy danh mục ca, có thể lọc ca đang active.
- Trả giờ bắt đầu/kết thúc, timezone, phút nghỉ, phút công chuẩn và hệ số công.

#### `POST /api/v1/attendance/shifts`

- Tạo định nghĩa ca làm việc; code phải duy nhất.
- Server xác định ca qua đêm từ giờ bắt đầu/kết thúc và trả `ETag`.

#### `GET /api/v1/attendance/shifts/{shiftId}`

- Lấy một định nghĩa ca cùng `ETag` hiện tại.

#### `PUT /api/v1/attendance/shifts/{shiftId}`

- Thay thế định nghĩa ca; bắt buộc `If-Match`.
- Thay đổi không được làm sai lịch đã khóa hoặc dữ liệu bảng công lịch sử.

#### `GET /api/v1/attendance/schedules`

- Lấy lịch phân ca theo tuần; bắt buộc `weekStart` là thứ Hai.
- Hỗ trợ lọc employee/phòng ban và phân trang.

#### `PUT /api/v1/attendance/schedules`

- Thay thế một tập assignment tuần trong một transaction.
- Yêu cầu `weekStart`, `Idempotency-Key`; kiểm tra employee, ca, ngày và xung đột lịch.
- Một nhân viên chỉ có một ca mỗi ngày (`ux_schedule_employee_date`); phân ca vào kỳ công đã khóa trả `409`.

### 6.2. Lịch nghỉ lễ

#### `GET /api/v1/attendance/holidays`

- Lấy lịch nghỉ lễ theo `calendarCode` (mặc định `VN`) và `year`.
- Trả cờ hưởng lương và hệ số công nếu phải làm việc trong ngày lễ.

#### `POST /api/v1/attendance/holidays`

- Thêm một ngày lễ vào lịch; trùng `calendarCode` + `holidayDate` trả `409`.

#### `PUT /api/v1/attendance/holidays/{holidayId}`

- Thay thế một mục lịch lễ; bắt buộc `If-Match`.

#### `DELETE /api/v1/attendance/holidays/{holidayId}`

- Xóa mục lịch lễ; bắt buộc `If-Match`.
- Trả `409` nếu ngày lễ đang được tham chiếu bởi bảng công đã tính.

### 6.3. Điểm danh và bảng công

#### `POST /api/v1/attendance/events`

- Nhân viên hoặc HR được phép ghi nhận check-in/check-out.
- Body gồm thời điểm, timezone, phương thức và dữ liệu vị trí/thiết bị khi áp dụng.
- Yêu cầu `Idempotency-Key`; event trùng không tạo bản ghi lần hai.

#### `POST /api/v1/integrations/attendance/events`

- Nhận event từ máy chấm công/GPS/Face ID qua `X-Device-Signature`.
- Yêu cầu external event ID, device ID, employee external key và `Idempotency-Key`.
- Chữ ký/thiết bị sai trả `401`; event hợp lệ được nhận bằng `202`.

#### `GET /api/v1/attendance/timesheets`

- Lấy bảng công cá nhân hoặc đội nhóm theo khoảng `from`–`to`.
- Hỗ trợ employee, phòng ban, trạng thái và phân trang có giới hạn trên.
- Các giá trị worked/late/early-leave/overtime **do server tính** và kèm `policyVersion` của từng dòng. Client không được tính lại.
- Truy vấn ngoài data scope trả `403`, không trả danh sách rỗng.

### 6.4. Hiệu chỉnh công

#### `GET /api/v1/attendance/corrections`

- Tra cứu đề nghị hiệu chỉnh theo employee, trạng thái và data scope.

#### `POST /api/v1/attendance/corrections`

- Tạo đề nghị sửa check-in/check-out của một ngày làm việc.
- Body gồm employee, ngày công, thời điểm đề xuất và lý do; trả request `pending` cùng `ETag`.

#### `POST /api/v1/attendance/corrections/{correctionId}/{action}`

- Action: `approve`, `reject`, `cancel`; bắt buộc `If-Match`.
- Reject/cancel phải có lý do theo policy.
- Approve ghi một `attendance_events` mới với `source = 'correction'`, tính lại bảng công, ghi audit và outbox **trong cùng transaction**. Sự kiện gốc từ thiết bị không bị xóa.
- Mỗi nhân viên chỉ có một đề nghị `pending` cho một ngày (`ux_corrections_one_pending_per_day`).

### 6.5. Tăng ca

#### `GET /api/v1/attendance/overtime-requests`

- Tra cứu đơn tăng ca theo employee, trạng thái, khoảng ngày và `pendingMyDecision`.

#### `POST /api/v1/attendance/overtime-requests`

- Đăng ký tăng ca cho một ngày công; yêu cầu `Idempotency-Key`.
- **Server tự xác định** `overtimeCategory` (`weekday` / `weekly_rest` / `holiday` / `night`) và `workCoefficient` từ lịch ca, lịch lễ và policy đang active. Giá trị client gửi lên bị bỏ qua.
- Hai đơn `pending`/`approved` của cùng nhân viên không được giao nhau về thời gian (`ex_overtime_requests_no_overlap`) → `409`.

#### `POST /api/v1/attendance/overtime-requests/{overtimeRequestId}/{action}`

- Action: `approve`, `reject`, `cancel`; bắt buộc `If-Match`.
- `approvedMinutes` có thể nhỏ hơn số đăng ký nhưng **không bao giờ lớn hơn** (`ck_overtime_minutes`) → vượt trả `422`.
- Số phút được duyệt được cộng vào `overtime_minutes` của ngày công tương ứng.

> [!WARNING]
> **Giới hạn giờ tăng ca theo ngày/tháng/năm hiện chưa được hệ thống chặn** — chưa có bảng hạn mức. Đây là khoảng trống tuân thủ đã ghi nhận tại `[OD-5.4]` và phải được xử lý trước khi go-live.

### 6.6. Nghỉ phép

#### `GET /api/v1/leave/types`

- Lấy loại phép, paid/unpaid, cho phép số dư âm hay không và policy version.

#### `GET /api/v1/leave/balances`

- Lấy quỹ phép theo năm; employee mặc định xem chính mình, HR xem theo scope.
- Trả entitlement, used, reserved và available theo đơn vị ngày/giờ.

#### `GET /api/v1/leave/requests`

- Tìm đơn của cá nhân, đội nhóm hoặc hàng đợi cần chính người dùng duyệt.
- Hỗ trợ employee, status, khoảng ngày, `pendingMyDecision` và phân trang.

#### `POST /api/v1/leave/requests`

- Gửi đơn và reserve số dư nguyên tử; yêu cầu `Idempotency-Key`.
- Client gửi khoảng thời gian, timezone và hình thức nghỉ; server tự tính requested units theo lịch, ngày lễ và policy.
- Không đủ quỹ hoặc trùng đơn trả `409`.

#### `GET /api/v1/leave/requests/{leaveRequestId}`

- Lấy đơn, số lượng đã tính, policy version, lịch sử quyết định và `ETag`.

#### `POST /api/v1/leave/requests/{leaveRequestId}/{action}`

- Action: `approve`, `reject`, `cancel`; bắt buộc `If-Match`.
- Backend kiểm tra đúng approver chain/data scope rồi cập nhật đơn, balance, audit và outbox nguyên tử.

#### `POST /api/v1/leave/requests/batch-decision`

- Duyệt hoặc từ chối tối đa 100 đơn, yêu cầu `Idempotency-Key`.
- Mỗi item mang `leaveRequestId` và `expectedVersion`.
- Response trả kết quả riêng từng đơn; một conflict không che kết quả của các đơn khác.

### 6.7. Cấu hình loại phép

#### `POST /api/v1/leave/types`

- Tạo loại phép ở trạng thái `policy_status = 'draft'`.
- Loại phép `draft` **không dùng được để gửi đơn** — trả `422`.

#### `PUT /api/v1/leave/types/{leaveTypeId}`

- Thay thế policy còn `draft`; bắt buộc `If-Match`.
- Policy đã `approved` không được sửa; phải tạo `policyVersion` mới.

#### `POST /api/v1/leave/types/{leaveTypeId}/{action}`

- Action: `approve`, `deactivate`, `reactivate`; bắt buộc `If-Match`.
- `approve` ghi `approvedBy` và `approvedAt`; `ck_leave_type_approved` chặn trạng thái `approved` thiếu hai trường này.

### 6.8. Kỳ công và khóa kỳ

#### `GET /api/v1/attendance/timesheet-periods`

- Liệt kê kỳ công, lọc theo trạng thái, kèm `openExceptionCount`.

#### `POST /api/v1/attendance/timesheet-periods`

- Tạo kỳ công với `periodCode`, `startsOn`, `endsOn`.
- Các kỳ **không được giao nhau** (`ex_timesheet_periods_no_overlap`) → `409`.

#### `GET /api/v1/attendance/timesheet-periods/{periodId}/exceptions`

- Liệt kê ngoại lệ đang chặn việc duyệt kỳ: `incomplete_workday`, `unexplained_absence`, `pending_correction`, `overtime_without_attendance`, `missing_schedule`.
- Mỗi ngoại lệ nêu rõ nhân viên, ngày và chi tiết.

#### `POST /api/v1/attendance/timesheet-periods/{periodId}/{action}`

- Action: `submit`, `approve`, `lock`, `reopen`; bắt buộc `If-Match`.
- `approve` bị từ chối khi còn ngoại lệ chưa xử lý → `409` kèm danh sách.
- `lock` làm mọi bản ghi trong kỳ trở nên bất biến: chấm công, hiệu chỉnh, phân ca và tăng ca nhắm vào kỳ đã khóa đều trả `409`.
- `reopen` bắt buộc có lý do (`ck_timesheet_period_reopened`) và được ghi audit log.

#### `POST /api/v1/attendance/timesheet-periods/{periodId}/payroll-handoff`

- Bàn giao kỳ công sang Payroll; yêu cầu `Idempotency-Key` và `reference`.
- Bị từ chối nếu kỳ chưa `locked` (`ck_timesheet_period_handoff`).

> [!WARNING]
> **Việc mở lại kỳ đã bàn giao Payroll hiện chưa có cơ chế điều chỉnh** (`[OD-8.5]`). Phải chốt trước khi nối phân hệ Payroll, nếu không sẽ tạo sai lệch giữa số đã bàn giao và số hiện tại.

## 7. Administration và Operations

### 7.1. User và RBAC

#### `GET /api/v1/administration/users`

- Tìm local actor mapping theo search/status; không truy cập mật khẩu hoặc credential của IdP.

#### `POST /api/v1/administration/users`

- Ánh xạ một `externalSubject` đã tồn tại tại IdP với user QLNS.
- Kiểm tra subject/email duy nhất; trả user cùng `ETag`.

#### `GET /api/v1/administration/users/{userId}`

- Lấy mapping, trạng thái, role grants và data scopes của một user.

#### `POST /api/v1/administration/users/{userId}/{action}`

- Action: `enable`, `disable`, `synchronize`; bắt buộc `If-Match`.
- Disable/synchronize không quản lý password; IdP vẫn sở hữu xác thực danh tính.

#### `GET /api/v1/administration/roles`

- Lấy catalog role, permission và loại data scope có thể cấp.

#### `GET /api/v1/administration/users/{userId}/role-grants`

- Lấy toàn bộ role/data-scope grants của user.

#### `POST /api/v1/administration/users/{userId}/role-grants`

- Cấp role với scope `self`, `department` hoặc `organization`.
- Yêu cầu `Idempotency-Key`; mọi grant được audit.

#### `DELETE /api/v1/administration/users/{userId}/role-grants/{roleCode}`

- Thu hồi đúng một grant, xác định thêm bằng `dataScopeType` và `dataScopeId`.
- Không được tự thu hồi quyền cuối cùng nếu làm hệ thống mất khả năng quản trị theo policy.

### 7.2. Audit và delivery

#### `GET /api/v1/administration/audit-logs`

- Tìm audit theo actor, action, entity, result và khoảng thời gian.
- Payload before/after được redacted và giới hạn theo mandate của auditor.

#### `GET /api/v1/administration/audit-logs/{auditLogId}`

- Lấy một audit record bất biến; không trả secret hoặc trường nhạy cảm ngoài quyền.

#### `GET /api/v1/administration/deliveries`

- Theo dõi outbox/provider delivery theo status, message type và aggregate.
- Không trả payload chứa dữ liệu nhạy cảm.

#### `POST /api/v1/administration/deliveries/{deliveryId}/retry`

- Lập lịch retry hữu hạn cho delivery failed/dead-letter.
- Yêu cầu `Idempotency-Key` và lý do; không gọi provider trong transaction HTTP.

### 7.3. Integration và health

#### `GET /api/v1/administration/integrations`

- Lấy cấu hình provider đã mask; chỉ cho biết secret đã được cấu hình hay chưa.

#### `PUT /api/v1/administration/integrations/{integrationKey}`

- Thay thế setting và có thể rotate `rotatedSecret` dạng write-only.
- Bắt buộc `If-Match`; response tuyệt đối không echo secret.

#### `POST /api/v1/administration/integrations/{integrationKey}/test`

- Khởi chạy connectivity test bất đồng bộ, không ghi business state.
- Yêu cầu `Idempotency-Key`; trả test ID và trạng thái.

#### `GET /health/live`

- Probe công khai tối giản cho biết process còn sống; không kiểm tra dependency và không lộ chi tiết nội bộ.

#### `GET /health/ready`

- Probe công khai tối giản cho biết service sẵn sàng nhận traffic.
- Trả `200` khi ready hoặc `503` khi chưa ready; không liệt kê secret/dependency detail.

## 8. Trạng thái triển khai

Contract trên mô tả API mục tiêu. Source backend hiện mới triển khai đầy đủ **hai operation**:

- `GET /api/v1/recruitment/applications/{applicationId}`
- `POST /api/v1/recruitment/applications/{applicationId}/advance`

Chi tiết những gì đã được viết: [Vertical Slice `REC-03.2`](../docs/vertical_slice_rec_03_2.md).

### Trạng thái theo nhóm

| Nhóm | `x-implementation-status` | Điều kiện để triển khai |
| :--- | :--- | :--- |
| Recruitment pipeline (advance/read) | **implemented** | Còn thiếu migration, runtime, IdP và integration/contract test |
| Recruitment (phần còn lại) | `proposed` | Chốt IdP/RBAC; sinh EF Core migration |
| Core HR — Employees, Organization, Onboarding, Events, Documents | `proposed` | Chốt IdP/RBAC; sinh EF Core migration |
| Core HR — Probation, Offboarding | `proposed` | Chốt template checklist offboarding; công thức quy đổi phép chưa dùng |
| Contracts | `proposed` | Chốt nhà cung cấp chữ ký số nếu dùng |
| Reports | `proposed` | Chốt quy ước tính toán và quyền xuất dữ liệu |
| **Attendance & Leave** | `discovery-required` | **Chốt [Open Decisions](../docs/open_decisions_attendance_leave.md)**; có `attendance_policies` `active` và `leave_types` `approved` |
| Administration | `proposed` | Chốt IdP và chính sách audit |

### Nguyên tắc triển khai từng operation

Mỗi operation phải được triển khai đầy đủ cả sáu phần, không tách rời: controller, authorization policy (permission + data scope), business workflow, persistence transaction (kèm audit và outbox trong cùng transaction), và contract/integration test. Một endpoint trả đúng JSON nhưng chưa có kiểm tra quyền phía server hoặc chưa ghi audit **không được tính là đã triển khai**.

Slice tiếp theo được đề xuất: [`ATT-03` Đơn nghỉ phép](../docs/vertical_slice_leave_01.md).
