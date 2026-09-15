# QLNS Documentation

> **Status:** Design/Prototype · **Audience:** product owners, architects, developers, reviewers

Thư mục `docs/` là trung tâm đặc tả nghiệp vụ và kiến trúc của hệ thống Quản lý Nhân sự **QLNS / HRMS**.

> [!IMPORTANT]
> Repository hiện chỉ có **UI/UX prototype** và **thiết kế cơ sở dữ liệu** cho các chức năng chính. Frontend application, Backend API, migration runtime, tích hợp ngoài và hạ tầng triển khai chưa được xây dựng. Mọi API, container, component và runtime flow trong tài liệu đều là **Proposed/Target**, trừ khi được ghi rõ là artifact hiện có.

---

## 1. Documentation Map

| Document | Purpose | Status |
|---|---|---|
| [Architecture — arc42 + C4](architecture.md) | Tài liệu thiết kế chính: goals, constraints, C4 Level 1/2/3, runtime, deployment, crosscutting concepts, ADR, quality scenarios, risks và fitness functions | Proposed · Authoritative |
| [Functional Specifications](functional_specifications.md) | Đặc tả yêu cầu, vai trò, quyền hạn, precondition, business flow và acceptance rules cho các phân hệ chính | Proposed · Authoritative |
| [System Diagrams](system_diagrams.md) | Use case, ATS state machine, sequence và activity diagrams bổ sung | Proposed · Supporting |
| [Database Design](../database/database_design.md) | ERD và đặc tả 14 bảng cho Core HR, Contracts, Recruitment và Onboarding | Design artifact |
| [Database README](../database/README.md) | Chỉ mục DBML, PostgreSQL DDL và hướng dẫn kiểm tra schema tham chiếu | Design artifact |
| [UI/UX README](../uiux/README.md) | Chỉ mục HTML prototype và ảnh giao diện cho các chức năng đã thiết kế | Prototype artifact |

`architecture.md` là nguồn chuẩn cho quyết định kiến trúc và C4. `system_diagrams.md` chỉ bổ sung góc nhìn nghiệp vụ; nếu hai tài liệu mâu thuẫn, ưu tiên `architecture.md` và cập nhật tài liệu còn lại trong cùng thay đổi.

---

## 2. Current Project Scope

| Area | Existing artifacts | Not implemented |
|---|---|---|
| Core HR | UI prototype; thiết kế bảng nhân viên, phòng ban, vị trí, hợp đồng, tài liệu và biến động | frontend, API, authorization, workflow, persistence runtime |
| Recruitment ATS | UI prototype; thiết kế job, candidate, resume, application, interview, evaluation và offer | frontend, API, state-transition engine, provider integrations |
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
2. [Target code structure](architecture.md#55-target-code-structure)
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
- Review schema 14 bảng và chọn công cụ migration có version.
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
