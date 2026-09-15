# QLNS Documentation

> **Status:** Design/Prototype · **Audience:** product owners, architects, developers, reviewers

Thư mục `docs/` là trung tâm đặc tả nghiệp vụ và kiến trúc của hệ thống Quản lý Nhân sự **QLNS / HRMS**.

> [!IMPORTANT]
> Repository có **UI/UX prototype**, canonical database/OpenAPI contract và source baseline React/.NET 10 cho vertical slice `REC-03.2`. Migration runtime, tích hợp PostgreSQL/IdP và hạ tầng triển khai chưa được xác minh. Chỉ artifact được ghi rõ `Implemented` mới được xem là source hiện có; không suy diễn thành production-ready.

---

## 1. Documentation Map

| Document | Purpose | Status |
|---|---|---|
| [INVEST User Stories](user_stories.md) | User Story được tách nhỏ theo INVEST, có mẫu chuẩn, acceptance criteria và traceability | Proposed · Authoritative for delivery |
| [REC-03.2 Vertical Slice](vertical_slice_rec_03_2.md) | Traceability, 3-tier/3-layer mapping, class and sequence diagrams for the first end-to-end slice | Source implemented · Runtime verification pending |
| [Architecture — arc42 + C4](architecture.md) | Tài liệu thiết kế chính: goals, constraints, C4 Level 1/2/3, runtime, deployment, crosscutting concepts, ADR, quality scenarios, risks và fitness functions | Proposed · Authoritative |
| [Functional Specifications](functional_specifications.md) | Đặc tả yêu cầu, vai trò, quyền hạn, precondition, business flow và acceptance rules cho các phân hệ chính | Proposed · Authoritative |
| [Use Cases](use_cases.md) | Use case tổng quát và chi tiết cho Tuyển dụng, Core HR, Hợp đồng, Attendance/Leave và Reporting/Administration | Proposed · Supporting |
| [Sequence Diagrams](sequence_diagrams.md) | Sequence theo 3-tier/3-layer cho các workflow quản lý chính, gồm success và failure branches | Proposed · Supporting |
| [Database Design](../database/database_design.md) | ERD và đặc tả 14 bảng cho Core HR, Contracts, Recruitment và Onboarding | Design artifact |
| [Database README](../database/README.md) | Chỉ mục DBML, PostgreSQL DDL và hướng dẫn kiểm tra schema tham chiếu | Design artifact |
| [UI/UX README](../uiux/README.md) | Chỉ mục HTML prototype và ảnh giao diện cho các chức năng đã thiết kế | Prototype artifact |

`architecture.md` là nguồn chuẩn cho quyết định kiến trúc và C4; `use_cases.md` là nguồn sơ đồ use case nghiệp vụ. Nếu hai tài liệu mâu thuẫn, ưu tiên ranh giới kỹ thuật trong `architecture.md` và business rule trong `functional_specifications.md`/`user_stories.md`.

---

## 2. Current Project Scope

| Area | Existing artifacts | Not implemented |
|---|---|---|
| Core HR | UI prototype; canonical schema contract cho nhân viên, phòng ban, vị trí, hợp đồng, tài liệu và biến động | API, authorization, workflow, persistence runtime |
| Recruitment ATS | UI prototype; canonical schema/OpenAPI; source REC-03.2 React/.NET 10 | runtime verification, IdP, provider integrations và các story còn lại |
| Onboarding | UI prototype và bảng checklist nền tảng | handoff transaction, account provisioning, notification |
| Attendance | UI prototype | database schema, work-calendar rules, API, device integration |
| Leave | UI prototype | database schema, balance policy, approval API và audit |
| Identity / RBAC / Audit | vai trò và policy được đặc tả | IdP, account schema, server-side enforcement, audit storage |
| Deployment / Operations | topology mục tiêu trong tài liệu kiến trúc | container image, Docker Compose, CI/CD, monitoring, backup runtime |

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
4. [Architecture decisions](architecture.md#architecture-decisions)
5. [Risks and technical debt](architecture.md#11-risks-and-technical-debt)

### Developer starting implementation

1. [Current project scope](#2-current-project-scope)
2. [Target code structure](architecture.md#56-target-code-structure)
3. [Runtime view](architecture.md#6-runtime-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Architecture fitness functions](architecture.md#12-architecture-fitness-functions)
6. [Database design](../database/database_design.md)

### Security / Operations reviewer

1. [Quality goals](architecture.md#12-quality-goals-measurable--arc42-12)
2. [External interfaces](architecture.md#32-external-interfaces)
3. [Deployment view](architecture.md#7-deployment-view)
4. [Crosscutting concepts](architecture.md#8-crosscutting-concepts)
5. [Risk register](architecture.md#11-risks-and-technical-debt)

---

## 4. Functional Coverage

Đặc tả hiện tập trung vào bốn nhóm nghiệp vụ:

1. **Recruitment ATS** — requisition, CV intake, candidate pipeline, interview, scorecard và offer.
2. **Core HR** — employee master data, organization, onboarding, employee events và documents.
3. **Contracts** — contract lifecycle, expiration alert và addendum.
4. **HR Analytics** — headcount và recruitment funnel.

Attendance và Leave hiện mới có UI/UX concept và mô tả kiến trúc mục tiêu; schema và business rules cần được phê duyệt trước implementation. Payroll, Performance và Learning chưa thuộc baseline thiết kế chính.

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
- Review canonical schema 20 bảng và sinh EF Core migration đầu tiên có version.
- Bổ sung schema cho identity, audit và module được chọn làm vertical slice.
- Chốt API convention, error model, pagination và concurrency strategy.
- Chọn vertical slice đầu tiên cùng acceptance test end-to-end.
- Thiết lập architecture, security, migration và contract gates trong CI.

Các open decision đầy đủ được theo dõi tại [Architecture Decisions](architecture.md#architecture-decisions).

---

## 7. Related Resources

- [Project README](../README.md)
- [UI/UX prototypes](../uiux/README.md)
- [Database artifacts](../database/README.md)
- [Primary architecture](architecture.md)
- [Functional specifications](functional_specifications.md)
