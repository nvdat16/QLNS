# QLNS Documentation

> **Status:** Design/Prototype · **Audience:** product owners, architects, developers, reviewers

Thư mục `docs/` là trung tâm đặc tả nghiệp vụ và kiến trúc của hệ thống Quản lý Nhân sự **QLNS / HRMS**.

> [!IMPORTANT]
> Repository có **UI/UX prototype**, canonical database/OpenAPI contract và **skeleton source** React/.NET 10 (cấu trúc dự án 3-tier/3-layer và một module mẫu, chưa phải code chính thức). Migration runtime, tích hợp PostgreSQL/IdP và hạ tầng triển khai chưa được xác minh. Chỉ artifact được ghi rõ `Implemented` mới được xem là source hiện có; không suy diễn thành production-ready.

> **Phạm vi triển khai trước:** hai phân hệ được chọn theo bản đồ chức năng — **Core HR** (bao gồm nhánh con Contracts) và **Recruitment (ATS)**. Attendance & Leave đã có thiết kế nhưng được **tách ra ngoài phạm vi** và giữ tại [deferred/attendance_leave/](deferred/attendance_leave/README.md). Payroll, Performance và Learning chưa thuộc baseline thiết kế.

---

## 1. Documentation Map

| Document | Purpose | Status |
|---|---|---|
| [INVEST User Stories](user_stories.md) | 24 user story theo INVEST cho hai phân hệ triển khai trước, có acceptance criteria Gherkin và traceability | Proposed · Authoritative for delivery |
| [Deferred — Attendance & Leave](deferred/attendance_leave/README.md) | Toàn bộ thiết kế của phân hệ đã tách khỏi phạm vi: SRS, 13 story, use case, 3 sequence, DDL 13 bảng, OpenAPI fragment, Open Decisions | **Out of scope · Parked** |
| [Architecture — arc42 + C4](architecture.md) | Tài liệu thiết kế chính: goals, constraints, C4 Level 1/2/3, runtime, deployment, crosscutting concepts, ADR, quality scenarios, risks và fitness functions | Proposed · Authoritative |
| [Functional Specifications](functional_specifications.md) | Đặc tả yêu cầu, vai trò, quyền hạn, precondition, business flow và acceptance rules | Proposed · Authoritative |
| [Use Cases](use_cases.md) | Use case tổng quát và chi tiết cho hai phân hệ, cùng Reporting/Administration | Proposed · Supporting |
| [Sequence Diagrams](sequence_diagrams.md) | 6 sequence theo 3-tier/3-layer, gồm success và failure branches | Proposed · Supporting |
| [Class Diagrams](class_diagrams.md) | Domain model 23 class theo module, design class diagram của vertical slice đã có code, và pattern 3-layer cho module còn lại | Proposed · Supporting |
| [Database Design](../database/database_design.md) | ERD và đặc tả canonical 23 bảng | Design artifact |
| [Database README](../database/README.md) | Chỉ mục schema canonical, DDL legacy đã deprecated và hướng dẫn kiểm tra | Design artifact |
| [API Contract](../api/README.md) | OpenAPI 3.0.3, 87 operation trên 70 path, kèm `x-implementation-status` từng operation | Design artifact |
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
| Core HR — Profile & Organization | UI prototype; canonical schema; OpenAPI; user story có AC | API, authorization, workflow, persistence runtime | IdP/RBAC, migration |
| Core HR — Lifecycle (onboarding, biến động, thử việc, thôi việc) | UI prototype (onboarding); canonical schema; OpenAPI; user story có AC | toàn bộ implementation; sequence cho offboarding chưa vẽ | IdP/RBAC, migration, template checklist |
| Core HR — Contracts | UI prototype; canonical schema; OpenAPI; user story có AC | toàn bộ implementation | IdP/RBAC, migration |
| Recruitment ATS | UI prototype; canonical schema/OpenAPI; skeleton source (module mẫu) | toàn bộ implementation | IdP/RBAC, migration, PostgreSQL runtime |
| Attendance & Leave | UI prototype; thiết kế đầy đủ đã tách sang `deferred/` | — | **Ngoài phạm vi** — không triển khai |
| Identity / RBAC / Audit | vai trò và policy được đặc tả; `users`, `user_roles`, `audit_logs` trong canonical schema | IdP, server-side enforcement, audit storage runtime | chọn IdP |
| Deployment / Operations | topology mục tiêu trong tài liệu kiến trúc | container image, Docker Compose, CI/CD, monitoring, backup runtime | ADR hạ tầng |

Các file HTML trong `uiux/` sử dụng dữ liệu minh họa và tương tác mô phỏng. Chúng không phải frontend application và không được dùng làm bằng chứng rằng business rule đã hoạt động.

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

Đặc tả bao phủ hai phân hệ triển khai trước:

1. **Core HR** — employee master data, organization, onboarding, employee events, documents, probation review, offboarding & handover, và nhánh con Contracts (lifecycle, cảnh báo hết hạn, phụ lục).
2. **Recruitment ATS** — requisition, CV intake, candidate pipeline, interview, scorecard, offer và onboarding handoff.

Cùng hai nhóm hỗ trợ: **HR Analytics** (headcount, recruitment funnel) và **System Administration** (tài khoản, RBAC, audit, integration).

> [!NOTE]
> **Attendance & Leave** đã được thiết kế đầy đủ (SRS, 13 story, DDL 13 bảng, 24 endpoint, 3 sequence, danh sách quyết định HR/Legal) nhưng **không thuộc phạm vi triển khai**. Toàn bộ được giữ nguyên tại [deferred/attendance_leave/](deferred/attendance_leave/README.md) để có thể ghép lại khi phạm vi mở rộng. Không tham chiếu tới nó từ tài liệu authoritative.

Payroll, Performance và Learning chưa thuộc baseline thiết kế chính.

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
- Chốt Identity Provider, authentication flow, RBAC và data-scope policy.
- Review canonical schema **23 bảng** và sinh EF Core migration đầu tiên có version.
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
