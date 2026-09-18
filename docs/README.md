# QLNS Documentation

> **Status:** Design/Prototype · **Audience:** product owners, architects, developers, reviewers

Thư mục `docs/` là trung tâm đặc tả nghiệp vụ và kiến trúc của hệ thống Quản lý Nhân sự **QLNS / HRMS**.

> [!IMPORTANT]
> Repository có **UI/UX prototype**, canonical database/OpenAPI contract và **backend .NET 10 code-complete** cho toàn bộ 79 operation (3 layer, audit/outbox cùng transaction, 1347 unit test) cùng frontend React có đăng nhập thật. Integration test trên PostgreSQL, migration runtime, background worker và hạ tầng triển khai chưa được xác minh. Chỉ artifact được ghi rõ `Implemented` mới được xem là hoàn tất; `code-complete` chưa phải production-ready.

> **Phạm vi giao hàng:** hai phân hệ được chọn theo bản đồ chức năng [`topdown-approach.png`](../topdown-approach.png) — **Core HR** (bao gồm nhánh con Contracts) và **Recruitment (ATS)**. Quy ước đọc bản đồ: **chỉ các chức năng lá in đậm dưới hai phân hệ này thuộc phạm vi**; mọi thứ khác ngoài phạm vi, kể cả bốn chức năng lá không in đậm nằm ngay trong hai phân hệ đó. Nguồn chuẩn về phạm vi là [mục 2 của README gốc](../README.md#2-delivery-scope--seven-pillars-two-selected); chi tiết ở [mục 4](#4-functional-coverage) dưới đây.

---

## 1. Documentation Map

| Document | Purpose | Status |
|---|---|---|
| [INVEST User Stories](user_stories.md) | 28 user story theo INVEST cho hai phân hệ nghiệp vụ và phân hệ định danh, có acceptance criteria Gherkin và traceability | Proposed · Authoritative for delivery |
| [Deferred — Attendance & Leave](deferred/attendance_leave/README.md) | Toàn bộ thiết kế của phân hệ đã tách khỏi phạm vi: SRS, 13 story, use case, 3 sequence, DDL 13 bảng, OpenAPI fragment, Open Decisions | **Out of scope · Parked** |
| [Architecture — arc42 + C4](architecture.md) | Tài liệu thiết kế chính: goals, constraints, C4 Level 1/2/3, runtime, deployment, crosscutting concepts, ADR, quality scenarios, risks và fitness functions | Proposed · Authoritative |
| [Functional Specifications](functional_specifications.md) | Đặc tả yêu cầu, vai trò, quyền hạn, precondition, business flow và acceptance rules | Proposed · Authoritative |
| [Use Cases](use_cases.md) | Use case tổng quát và chi tiết cho hai phân hệ trong phạm vi: Core HR và Recruitment | Proposed · Supporting |
| [Sequence Diagrams](sequence_diagrams.md) | 6 sequence theo 3-tier/3-layer, gồm success và failure branches | Proposed · Supporting |
| [Class Diagrams](class_diagrams.md) | Domain model 23 class theo module, design class diagram của vertical slice đã có code, và pattern 3-layer cho module còn lại | Proposed · Supporting |
| [Database Design](../database/database_design.md) | ERD và đặc tả canonical 23 bảng v1; bảng thứ 24 (delta v1.1) và bốn bảng định danh (delta v1.2) ở Database README | Design artifact |
| [Database README](../database/README.md) | Chỉ mục schema canonical, DDL legacy đã deprecated và hướng dẫn kiểm tra | Design artifact |
| [API Contract](api/README.md) | OpenAPI 3.0.3, 79 operation trên 62 path, kèm `x-implementation-status` từng operation | Design artifact |
| [UI/UX README](../uiux/README.md) | Chỉ mục HTML prototype và ảnh giao diện cho các chức năng đã thiết kế | Prototype artifact |

Thứ tự ưu tiên khi hai tài liệu mâu thuẫn:

1. **Ranh giới kỹ thuật** → `architecture.md`.
2. **Business rule và acceptance criteria** → `functional_specifications.md` / `user_stories.md`.
3. **Kiểu dữ liệu, constraint và invariant** → `database/schema.sql` (canonical, thắng cả `database_design.md`).
4. **Hợp đồng API** → `api/openapi.yaml` (thắng `API_REFERENCE.md`).
5. **Nội dung trong `deferred/`** không có thẩm quyền với phạm vi hiện tại; nó chỉ là điểm khởi đầu nếu module tương ứng quay lại phạm vi.

---

## 2. Current Project Scope

| Area | Existing artifacts | Not implemented | Chặn bởi |
|---|---|---|---|
| Core HR — Profile & Organization | UI prototype; canonical schema; OpenAPI; user story có AC; **backend code-complete** (EMP-01, EMP-02) | integration test | migration |
| Core HR — Lifecycle (onboarding, biến động, thử việc, thôi việc) | UI prototype (onboarding); canonical schema; OpenAPI; user story có AC; **backend code-complete** (EMP-03 … EMP-07) | integration test; Effective-Date Worker và worker khóa tài khoản; sequence cho offboarding chưa vẽ | migration, Payroll (final settlement) |
| Core HR — Contracts | UI prototype; canonical schema; OpenAPI; user story có AC; **backend code-complete** (CON-01 … CON-03) | integration test; worker hết hạn/cảnh báo; object store thật | migration |
| Recruitment ATS | UI prototype; canonical schema/OpenAPI; **backend code-complete** (REC-01 … REC-06) | integration test; outbox worker (email/.ics/offer token); CV parser và malware scanner thật | migration, PostgreSQL runtime |
| Attendance & Leave | UI prototype; thiết kế đầy đủ đã tách sang `deferred/` | — | **Ngoài phạm vi** — không triển khai |
| Identity & Access (ADM) | UI đăng nhập + màn hình quản trị tài khoản; canonical schema (6 bảng định danh); OpenAPI; user story có AC; **backend code-complete** (ADM-01, ADM-02) | integration test cho luân chuyển refresh token; rate limit ở reverse proxy; luồng quên mật khẩu qua email | migration, quản lý secret cho `Authentication:Jwt:SigningKey` |
| Audit & outbox (crosscutting) | `audit_logs` và `outbox_messages` là **cơ chế bắt buộc** của mọi command, ghi cùng transaction nghiệp vụ; đã có trong canonical schema | persistence và outbox dispatcher runtime | migration, email/calendar provider |
| Deployment / Operations | topology mục tiêu trong tài liệu kiến trúc; hai probe `/health/live`, `/health/ready` trong hợp đồng (tag `Operations`, thuộc deployment view chứ không phải chức năng nghiệp vụ) | container image, Docker Compose, CI/CD, monitoring, backup runtime | ADR hạ tầng |

**Quản trị tài khoản và vai trò thuộc phạm vi** (phân hệ `[ADM]`, [ADR-011](architecture.md#9-architecture-decisions-adr-index)): QLNS tự phát hành và thu hồi phiên, không dùng Identity Provider bên ngoài. Vẫn không có API cho audit log, delivery hay integration: cấu hình integration/notification/approval nằm trong `appsettings` (không UI, không API). Yêu cầu kiểm tra permission và data scope phía server trên mọi request không đổi — quality goal Q1 giữ nguyên.

Các file HTML trong `uiux/` sử dụng dữ liệu minh họa và tương tác mô phỏng. Chúng không phải frontend application và không được dùng làm bằng chứng rằng business rule đã hoạt động. Một số prototype (ví dụ Workforce Dashboard) mô tả chức năng **ngoài phạm vi** và chỉ được giữ làm tham chiếu thiết kế.

---

## 3. Recommended Reading Paths

### Product owner / HR reviewer

1. [Functional Specifications](functional_specifications.md)
2. [Business context và stakeholders](architecture.md#1-introduction-and-goals)
3. [Quality requirements](architecture.md#10-quality-requirements-stimulus--response--measure)
4. [UI/UX prototypes](../uiux/README.md)

### Architect / technical reviewer

1. [Architecture — arc42 + C4](architecture.md)
2. [Constraints](architecture.md#2-constraints)
3. [C4 building blocks](architecture.md#5-building-block-view)
4. [Class diagrams](class_diagrams.md)
5. [Architecture decisions](architecture.md#9-architecture-decisions-adr-index)
6. [Risks and technical debt](architecture.md#11-risks-and-technical-debt)

### Developer starting implementation

1. [Current project scope](#2-current-project-scope)
2. [Target code structure](architecture.md#56-target-code-structure)
3. [Runtime view](architecture.md#6-runtime-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Architecture fitness functions](architecture.md#12-architecture-fitness-functions)
6. [Database design](../database/database_design.md)
7. [Class diagrams](class_diagrams.md)

### Security / Operations reviewer

1. [Quality goals](architecture.md#12-quality-goals-measurable--arc42-12)
2. [External interfaces](architecture.md#32-external-interfaces)
3. [Deployment view](architecture.md#7-deployment-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Risk register](architecture.md#11-risks-and-technical-debt)

---

## 4. Functional Coverage

Đặc tả bao phủ hai phân hệ trong phạm vi, tương ứng **34 chức năng lá in đậm** trên [`topdown-approach.png`](../topdown-approach.png):

1. **Core HR — 16 chức năng lá** — employee profiles (thông tin cá nhân/liên hệ, thông tin công tác, employee documents, profile updates); organization management (phòng ban & cây phân cấp, positions & job levels, phân công & reporting line); Contract Management (drafting & approval, signing & effective date, phụ lục & gia hạn, hết hạn & chấm dứt, contract documents); employee lifecycle (preboarding & onboarding checklist, probation review & confirmation, promotion & internal transfer, offboarding & handover).
2. **Recruitment ATS — 18 chức năng lá** — job requisition (tạo/sửa, phê duyệt, theo dõi trạng thái); job posting (publish/update/close); candidate management (hồ sơ ứng viên & resume ingestion, application management, screening & matching, recruitment pipeline, rejection/withdrawal); interview management (vòng phỏng vấn & panel, scheduling & invitation, scorecard & feedback, hiring decision); offer management (drafting & compensation approval, offer sending, acceptance/signing/decline); onboarding handoff (chuyển dữ liệu ứng viên đã nhận việc, liên kết application với employee record).

### Ngoài phạm vi

**Bốn chức năng lá không in đậm, nằm ngay trong hai phân hệ được chọn:**

| Chức năng | Thuộc | Hệ quả |
|---|---|---|
| **Headcount & Budget Validation** | Recruitment · Job Requisition | Requisition vẫn có luồng phê duyệt, nhưng **không** có bước hệ thống tự kiểm tra định biên và ngân sách lương; HR Manager quyết định thủ công. `target_headcount`, `salary_min`, `salary_max` chỉ là dữ liệu khai báo, không phải cơ chế kiểm soát. |
| **Recruitment Channel Management** | Recruitment · Job Posting | Publish/Update/Close vẫn trong phạm vi; chọn và quản lý nhiều kênh đăng tin thì không. Chỉ còn một kênh careers mặc định, không so sánh hiệu quả kênh. |
| **Organizational Chart** | Core HR · Organization Management | Bỏ **màn hình và endpoint cây tổ chức**. *Departments & Organizational Hierarchy* vẫn **trong** phạm vi, nên `departments.parent_department_id`, quan hệ cha con, quy tắc chống vòng lặp và ràng buộc khi xóa phòng ban đều được giữ. Chỉ phần trình bày dạng cây bị loại. |
| **Suspension & Return to Work** | Core HR · Employee Lifecycle | Bỏ nghiệp vụ tạm hoãn và trở lại làm việc. `employees.status = 'suspended'` và `employee_events.event_type IN ('suspension','return_to_work')` được giữ trong canonical schema như **giá trị reserved, không endpoint nào đặt được** trong đợt này. Tiền điều kiện của offboarding là nhân viên đang `active` hoặc `probation`. |

**Các trụ cột còn lại của bản đồ:** Attendance & Leave Management (xem note bên dưới), Reports & Analytics, System Administration, Performance Management, Compensation & Benefits.

> [!NOTE]
> **Attendance & Leave** đã được thiết kế đầy đủ (SRS, 13 story, DDL 13 bảng, 24 endpoint, 3 sequence, danh sách quyết định HR/Legal) nhưng **không thuộc phạm vi triển khai**. Toàn bộ được giữ nguyên tại [deferred/attendance_leave/](deferred/attendance_leave/README.md) để có thể ghép lại khi phạm vi mở rộng. Không tham chiếu tới nó từ tài liệu authoritative.

Performance Management và Compensation & Benefits (gồm payroll) chưa thuộc baseline thiết kế chính.

---

## 5. Documentation Rules

- Phân biệt rõ `Design artifact`, `Proposed`, `Implemented` và `Accepted`.
- Chỉ dùng `Implemented` khi source code, migration và test tương ứng thực sự tồn tại.
- Chỉ dùng `Accepted` cho ADR khi có owner và quyết định phê duyệt rõ ràng.
- Không mô tả dữ liệu prototype là dữ liệu thật hoặc UI prototype là frontend hoàn chỉnh.
- Thay đổi feature phải cập nhật đồng thời SRS, architecture, API/schema và diagram liên quan.
- Business rule phải có owner nghiệp vụ và acceptance criteria; nhóm kỹ thuật không tự suy diễn quy định lao động/pháp lý.
- Mermaid trong `architecture.md` là nguồn chỉnh sửa chính cho C4; ảnh hoặc sơ đồ khác chỉ mang tính bổ sung.
- Link nội bộ, Mermaid và bảng mục lục phải được kiểm tra trước khi merge.

---

## 6. Implementation Entry Criteria

Trước khi bắt đầu frontend/backend, tối thiểu cần:

- Chấp thuận stack frontend/backend và phiên bản bằng ADR.
- Chốt nơi quản lý secret `Authentication:Jwt:SigningKey` và chính sách vòng đời phiên (access token 30 phút, refresh token 14 ngày). Authentication flow đã chốt ở [ADR-011](architecture.md#9-architecture-decisions-adr-index); ma trận permission và data scope nằm ở `database/seed_roles.sql`.
- Review canonical schema **28 bảng (v1.2)** và sinh EF Core migration đầu tiên có version.
- Chốt API convention, error model, pagination và concurrency strategy. ✅ đã có trong `api/README.md`.
- Chọn vertical slice đầu tiên cùng acceptance test end-to-end.
- Thiết lập architecture, security, migration và contract gates trong CI.
- Tạo integration test project chạy trên PostgreSQL thật — bắt buộc, vì các invariant quan trọng của Core HR (partial unique index, conditional update) không thể verify bằng mock.

Các open decision đầy đủ được theo dõi tại [Architecture Decisions](architecture.md#9-architecture-decisions-adr-index).

---

## 7. Related Resources

- [Project README](../README.md)
- [UI/UX prototypes](../uiux/README.md)
- [Database artifacts](../database/README.md)
- [Primary architecture](architecture.md)
- [Functional specifications](functional_specifications.md)
