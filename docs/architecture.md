# Architecture (arc42 + C4)

## 1. Introduction and Goals

QLNS là hệ thống quản trị nguồn nhân lực (HRMS) kết hợp quản lý tuyển dụng (ATS), hướng tới một luồng dữ liệu xuyên suốt từ yêu cầu tuyển dụng, ứng viên, phỏng vấn và offer đến hồ sơ nhân viên, hợp đồng, onboarding, thử việc và thôi việc. Chấm công và nghỉ phép nằm ngoài phạm vi triển khai hiện tại.

Mục tiêu kiến trúc là tạo ranh giới rõ giữa giao diện, quy tắc nghiệp vụ và dữ liệu; bảo vệ dữ liệu nhân sự nhạy cảm; đồng thời cho phép phát triển từng phần mà không biến UI prototype thành nguồn business rule.

### 1.1 Stakeholders

| Role | Concern |
|---|---|
| Ban lãnh đạo / Nhà tài trợ | số liệu nhân sự đáng tin cậy, hiệu quả đầu tư, giảm rủi ro vận hành |
| HR Director / HR Manager | quy trình đúng thẩm quyền, truy vết quyết định, báo cáo nhất quán |
| Recruiter | pipeline ứng viên, lịch phỏng vấn, scorecard và offer trên một luồng thống nhất |
| Hiring / Line Manager | tác vụ phê duyệt rõ ràng, dữ liệu đúng phạm vi quản lý |
| HR Officer / C&B | hồ sơ, hợp đồng, onboarding, thử việc và thôi việc chính xác |
| Employee / Candidate | trải nghiệm dễ dùng, trạng thái minh bạch, dữ liệu cá nhân được bảo vệ |
| Application engineer | contract rõ, module độc lập, môi trường phát triển tái lập được |
| Architect / Reviewer | ngăn drift giữa yêu cầu, schema, API và implementation |
| Security / Legal | least privilege, audit, retention và tuân thủ pháp luật Việt Nam |
| SRE / Operations | triển khai, quan sát, sao lưu và phục hồi có thể kiểm chứng |

### 1.2 Quality goals (measurable — arc42 §1.2)

| # | Quality goal | Scenario | Measure | Priority |
|---|---|---|---|---|
| Q1 | **Bảo mật dữ liệu nhân sự** | người dùng yêu cầu hồ sơ, hợp đồng hoặc hành động ngoài phạm vi | 100% API nghiệp vụ yêu cầu authenticated actor; 100% test ngoài quyền trả `401/403`; không trả trường restricted | 1 |
| Q2 | **Toàn vẹn và truy vết** | lỗi xảy ra giữa một workflow nhiều bước | transaction rollback không để lại trạng thái dở dang; 100% command nhạy cảm có audit actor, time, target, result | 1 |
| Q3 | **Chính xác workflow** | client gửi transition, phê duyệt hoặc version không hợp lệ | 100% transition ngoài state machine bị từ chối; conflict đồng thời trả `409`; không cập nhật trực tiếp `status` | 1 |
| Q4 | **Khả dụng sử dụng** | người dùng hoàn thành tìm hồ sơ, chuyển vòng hoặc duyệt phép | các tác vụ ưu tiên đạt success rate ≥ 90% trong usability test; WCAG 2.1 AA cho luồng thiết yếu | 2 |
| Q5 | **Hiệu năng tương tác** | tải danh sách có filter/pagination trong tải mục tiêu | p95 API đọc ≤ 500 ms và command ≤ 800 ms, không tính provider ngoài; quy mô tải phải được chốt trước production | 2 |
| Q6 | **Dễ bảo trì** | thêm module hoặc thay provider tích hợp | không sửa domain module không liên quan; dependency fitness tests và contract tests đều pass | 2 |
| Q7 | **Khả năng phục hồi tích hợp** | email/calendar/e-signature timeout hoặc gửi callback lặp | business transaction vẫn nhất quán; event trùng không tạo side effect trùng; retry hữu hạn có đối soát | 2 |
| Q8 | **Phục hồi dữ liệu** | mất database node hoặc thao tác khôi phục | đạt RPO/RTO được phê duyệt và restore drill pass; giá trị cụ thể là Open Decision | 3 |

Q1–Q3 là các mục tiêu định hình kiến trúc. Mọi quyết định làm suy giảm chúng phải có ADR riêng.

---

## 2. Constraints

| # | Constraint | Type | Implication |
|---|---|---|---|
| C1 | Hiện trạng gồm UI/UX prototype và skeleton source (cấu trúc dự án + một module mẫu); chưa có runtime đã xác minh | Project | phân biệt skeleton với implemented, build-tested, integrated và production-ready |
| C2 | Frontend không truy cập database trực tiếp | Security | mọi query/command đi qua Backend API và server-side authorization |
| C3 | PostgreSQL là database chuẩn; `database/schema.sql` là canonical contract tạm thời | Technical | schema phải được review, chuyển thành EF Core migration và kiểm thử constraint trước khi dùng |
| C4 | React/Vite, ASP.NET Core .NET 10, EF Core và PostgreSQL | Technical | được Project Owner chấp thuận ngày 2026-09-15; package patch phải được pin trước release |
| C5 | Không dùng distributed transaction/2-phase commit với provider ngoài | Technical | business state commit độc lập; outbox, idempotency và reconciliation cho side effect |
| C6 | Dữ liệu nhân sự và ứng viên là confidential/restricted | Legal/Security | least privilege, encryption, audit, masking, retention và controlled export |
| C7 | Quy tắc lao động, hợp đồng, thuế và bảo hiểm cần HR/Legal phê duyệt | Legal | tài liệu kỹ thuật không tự suy diễn quy định pháp lý |
| C8 | Giao diện chính dùng tiếng Việt, thuật ngữ kỹ thuật có thể kèm tiếng Anh | Product | glossary và trạng thái nghiệp vụ phải nhất quán |
| C9 | Documentation-first | Organisational | thay đổi feature phải cập nhật SRS, kiến trúc, API/schema và ADR liên quan |
| C10 | SLA, tải, cloud/on-premises, RPO/RTO chưa được chốt | Organisational | deployment và capacity design giữ vendor-neutral; không cam kết production sớm |

---

## 3. Context and Scope

<a id="c4-level-1-system-context"></a>

### 3.1 Business context (C4 Level 1)

```mermaid
flowchart LR
    candidate(["👤 Candidate"])
    employee(["👤 Employee"])
    manager(["👤 Hiring / Line Manager"])
    hr(["👤 Recruiter / HR Officer / HR Manager"])
    admin(["👤 System Administrator"])

    subgraph boundary["QLNS System Boundary"]
        qlns["QLNS<br/><i>[Software System — Proposed]</i><br/>Recruitment and HR lifecycle management"]
    end

    jobboards["Job Boards<br/><i>[External System]</i>"]
    comms["Email / Calendar<br/><i>[External System]</i>"]
    esign["E-signature<br/><i>[External System]</i>"]
    storage["Document Storage<br/><i>[External System]</i>"]
    idp["Identity Provider<br/><i>[External System]</i>"]

    candidate -- "submits application; receives status" --> qlns
    employee -- "profile, contracts, handover" --> qlns
    manager -- "requisition, scorecard, approvals" --> qlns
    hr -- "recruitment and HR operations" --> qlns
    admin -- "accounts, roles, configuration" --> qlns
    qlns -- "publishes/receives recruitment data" --> jobboards
    qlns -- "notifications and schedules" --> comms
    qlns -- "documents and callbacks" --> esign
    qlns -- "private document objects" --> storage
    qlns -- "validates identity/tokens" --> idp

    style qlns fill:#1168bd,color:#fff
    style jobboards fill:#999,color:#fff
    style comms fill:#999,color:#fff
    style esign fill:#999,color:#fff
    style storage fill:#999,color:#fff
    style idp fill:#999,color:#fff
```

### 3.2 External interfaces

| Interface | Direction | Protocol / contract | Contract owner | Failure mode |
|---|---|---|---|---|
| Web API | in/out | HTTPS, REST/JSON, OpenAPI | QLNS Backend | RFC 7807-style error, `Retry-After` khi phù hợp |
| Identity | in | OIDC/OAuth2 candidate; chưa chọn provider | Security / Platform | fail closed; token invalid → `401` |
| Job boards | both | Provider API/webhook | Recruitment adapter | timeout/retry; duplicate → idempotent handling |
| Email/Calendar | out/both | Provider API/webhook | Notification/Calendar adapter | delivery state + bounded retry + reconciliation |
| E-signature | both | Provider API/webhook | Document/Contract adapter | callback verification; status reconciliation |
| Document storage | both | object API; signed/authorized download | Document adapter | unavailable → no metadata corruption |
| Database | both | PostgreSQL protocol | Persistence layer | transaction rollback; readiness degraded |

---

## 4. Solution Strategy

| Quality goal | Strategy | Where |
|---|---|---|
| Q1 security | backend-enforced authentication, RBAC + data scope, deny-by-default, restricted-field DTOs | §5.3, §8, ADR-003 |
| Q2 integrity | application-owned transaction, DB constraints, optimistic version/lock, immutable audit | §6, §8, ADR-005 |
| Q3 workflow | explicit commands and state machines; client cannot patch status directly | §6.2–6.4, ADR-006 |
| Q4 usability | feature-oriented UI, shared interaction states, accessibility and responsive prototype validation | §5.2, §8 |
| Q5 performance | server-side filter/sort/page, bounded queries, indexes derived from workloads, measurable budgets | §10 |
| Q6 maintainability | modular monolith, inward dependencies, ports/adapters, ownership per module | §5, ADR-002/007/008 |
| Q7 reliability | timeout, bounded retry, idempotency, outbox and reconciliation after commit | §6.5, §8, ADR-007 |
| Q8 recovery | versioned migration, encrypted backup, restore drill and documented RPO/RTO | §7, §10 |

**The one-sentence strategy:** *phát triển theo vertical slice trên một modular monolith, giữ business rules ở backend, PostgreSQL làm system of record và cô lập mọi hệ thống ngoài qua port/adapter.*

### 4.1 Strategy in one picture

```mermaid
flowchart TB
    UX["UI/UX prototypes<br/><i>Design input — existing</i>"]
    SRS["SRS + workflow + RBAC<br/><i>Specification — existing</i>"]
    DBDesign["ERD / DBML / DDL<br/><i>Database design — existing</i>"]
    Web["Feature-based Web Application<br/><i>Proposed</i>"]
    API["Modular Backend API<br/><i>Proposed</i>"]
    DB[("PostgreSQL + migrations<br/><i>Proposed runtime</i>")]
    Ports["Integration ports / adapters<br/><i>Proposed</i>"]

    UX --> Web
    SRS --> Web
    SRS --> API
    DBDesign --> DB
    Web --> API
    API --> DB
    API --> Ports
```

---

## 5. Building Block View

<a id="c4-level-2-containers"></a>

### 5.1 C4 Level 2 — containers

```mermaid
flowchart LR
    candidate(["👤 Candidate"])
    employee(["👤 Employee"])
    manager(["👤 Hiring / Line Manager"])
    hr(["👤 Recruiter / HR Officer / HR Manager"])
    admin(["👤 System Administrator / Auditor"])

    subgraph system["QLNS System Boundary"]
        direction TB
        web["React Web Application<br/><i>[Container · Presentation Tier — Skeleton]</i><br/>Candidate portal and internal HR workspace"]
        api["ASP.NET Core Backend API<br/><i>[Container · Application Tier — Skeleton]</i><br/>Authorization, use cases, workflow and transactions"]
        worker[".NET Background Worker<br/><i>[Container · Application Tier — Proposed]</i><br/>Scheduled jobs, outbox delivery and reconciliation"]
        db[("PostgreSQL HRMS Database<br/><i>[Container · Data Tier — Schema contract]</i><br/>Transactional system of record")]
    end

    idp["Identity Provider<br/><i>[External System]</i>"]
    jobboards["Job Boards<br/><i>[External System]</i>"]
    comms["Email / Calendar<br/><i>[External System]</i>"]
    esign["E-signature<br/><i>[External System]</i>"]
    objects[("Private Object Storage<br/><i>[External System]</i>")]
    observe["Observability Platform<br/><i>[External System]</i>"]

    candidate -->|"HTTPS: application and status"| web
    employee -->|"HTTPS: profile, contracts and handover"| web
    manager -->|"HTTPS: requisition, review and approval"| web
    hr -->|"HTTPS: recruitment and HR operations"| web
    admin -->|"HTTPS: account, role and audit"| web

    web -->|"HTTPS REST/JSON; OpenAPI contract"| api
    web -->|"OIDC Authorization Code + PKCE"| idp
    api -->|"validate token metadata / JWKS"| idp
    api -->|"EF Core / Npgsql; ACID transaction"| db
    api -->|"object metadata and signed access"| objects
    api -->|"transactional outbox"| db
    jobboards -->|"signed webhook / polling result"| api
    esign -->|"signed callback"| api

    worker -->|"claim jobs/outbox; write delivery state"| db
    worker -->|"publish jobs and reconcile status"| jobboards
    worker -->|"notification and calendar API"| comms
    worker -->|"send documents and reconcile signature"| esign
    worker -->|"read/write document objects"| objects
    api -->|"logs, metrics and traces"| observe
    worker -->|"logs, metrics and traces"| observe

    classDef partial fill:#1168bd,color:#fff,stroke:#0b4884
    classDef proposed fill:#6b4f9b,color:#fff,stroke:#463267
    classDef contract fill:#2f855a,color:#fff,stroke:#1f5b3d
    classDef external fill:#777,color:#fff,stroke:#555
    class web,api partial
    class worker proposed
    class db contract
    class idp,jobboards,comms,esign,objects,observe external
```

**Container responsibilities and dependency direction**

```text
Presentation Tier       Application Tier                         Data Tier
React Web Application → ASP.NET Core API ─┬→ Business/Data Layer → PostgreSQL
                                          └→ durable outbox
                                             ↓
                         .NET Background Worker → Provider adapters
```

- **React Web Application:** trình bày giao diện, điều hướng, local UI state và gọi API; không phải security boundary và không sở hữu business invariant.
- **ASP.NET Core Backend API:** entry point duy nhất cho dữ liệu nghiệp vụ; xác thực/ủy quyền, thực thi use case, workflow và transaction.
- **.NET Background Worker:** xử lý tác vụ bất đồng bộ hoặc theo lịch sau khi business state đã được commit; không nhận request trực tiếp từ người dùng.
- **PostgreSQL:** system of record. `database/schema.sql` hiện là canonical contract; migration/runtime database chưa được xác minh.
- **Private Object Storage:** giữ nội dung file; PostgreSQL chỉ giữ metadata và quyền tham chiếu.
- Màu xanh dương là skeleton source (cấu trúc, chưa implement), tím là thiết kế đề xuất, xanh lá là contract dữ liệu, xám là hệ thống ngoài.

<a id="c4-level-3-web"></a>

### 5.2 C4 Level 3 — inside Web Application

```mermaid
flowchart TB
    user(["👤 Browser User"])
    api["ASP.NET Core Backend API<br/><i>[Container]</i>"]
    idp["Identity Provider<br/><i>[External System]</i>"]
    observe["Observability Platform<br/><i>[External System]</i>"]

    subgraph web["React Web Application [Container · Presentation Tier]"]
        direction TB
        shell["Application Shell & Router<br/><i>[Component — Partial]</i><br/>layout, routes, navigation and error boundary"]
        auth["Session & Route Guards<br/><i>[Component — Proposed]</i><br/>OIDC session, claims and route access"]

        subgraph features["Feature components"]
            direction LR
            recruitment["Recruitment<br/><i>[Partial]</i><br/>jobs, candidates, interviews, offers"]
            corehr["Core HR<br/><i>[Proposed]</i><br/>employees, organization, contracts, onboarding"]
            reporting["Dashboard & Reporting<br/><i>[Proposed]</i><br/>authorized KPIs and export"]
            administration["Administration<br/><i>[Proposed]</i><br/>users, roles, configuration and audit"]
        end

        shared["Shared UI & Accessibility<br/><i>[Component — Partial]</i><br/>design tokens, forms, tables, feedback and WCAG states"]
        client["Typed API Client<br/><i>[Component — Partial]</i><br/>DTO, token, Problem Details, concurrency and correlation"]
        telemetry["Client Telemetry<br/><i>[Component — Proposed]</i><br/>diagnostics without sensitive payloads"]

        shell --> auth
        shell --> recruitment
        shell --> corehr
        shell --> reporting
        shell --> administration

        recruitment --> shared
        corehr --> shared
        reporting --> shared
        administration --> shared

        recruitment --> client
        corehr --> client
        reporting --> client
        administration --> client
        shell --> telemetry
    end

    user -->|HTTPS| shell
    auth -->|"Authorization Code + PKCE"| idp
    client -->|"REST/JSON generated from OpenAPI"| api
    telemetry -->|"logs, traces and web vitals"| observe

    classDef partial fill:#1168bd,color:#fff,stroke:#0b4884
    classDef proposed fill:#6b4f9b,color:#fff,stroke:#463267
    classDef external fill:#777,color:#fff,stroke:#555
    class shell,recruitment,shared,client partial
    class auth,corehr,reporting,administration,telemetry proposed
    class api,idp,observe external
```

Prototype trong `uiux/` là nguồn tham khảo cho các feature/component trên, không phải frontend implementation. Mỗi feature chỉ phụ thuộc `shared` và `client`; feature không import trực tiếp internals của feature khác. Route guard giúp trải nghiệm người dùng, nhưng Backend API vẫn phải kiểm tra quyền cho mọi request.

<a id="c4-level-3-backend"></a>

### 5.3 C4 Level 3 — inside ASP.NET Core Backend API

```mermaid
flowchart TB
    web["React Web Application<br/><i>[Container]</i>"]
    callbacks["Provider / Device Callbacks<br/><i>[External Systems]</i>"]
    idp["Identity Provider<br/><i>[External System]</i>"]
    db[("PostgreSQL<br/><i>[Container]</i>")]
    objects[("Private Object Storage<br/><i>[External System]</i>")]

    subgraph api["ASP.NET Core Backend API [Application Tier]"]
        direction TB

        subgraph presentation["Qlns.Api — Presentation Layer"]
            direction LR
            pipeline["HTTP Pipeline<br/><i>[Component — Partial]</i><br/>auth, correlation, validation and Problem Details"]
            recApi["Recruitment API<br/><i>[Partial]</i>"]
            hrApi["Core HR API<br/><i>[Proposed]</i>"]
            contractApi["Contract & Onboarding API<br/><i>[Proposed]</i>"]
            reportApi["Reporting API<br/><i>[Proposed]</i>"]
            adminApi["Administration API<br/><i>[Proposed]</i>"]
            webhookApi["Integration Webhook API<br/><i>[Proposed]</i>"]
        end

        subgraph business["Qlns.BusinessLogic — Business Layer"]
            direction LR
            authorization["Authorization Policies<br/><i>[Component — Proposed]</i><br/>RBAC + data scope + field policy"]
            recLogic["Recruitment Services & Domain<br/><i>[Partial]</i>"]
            hrLogic["Core HR Services & Domain<br/><i>[Proposed]</i>"]
            contractLogic["Contract & Onboarding Services<br/><i>[Proposed]</i>"]
            reportLogic["Reporting Query Services<br/><i>[Proposed]</i>"]
            adminLogic["Identity Administration Services<br/><i>[Proposed]</i>"]
            integrationLogic["Webhook Verification & Mapping<br/><i>[Proposed]</i>"]
            auditOutbox["Audit & Outbox Policies<br/><i>[Component — Proposed]</i>"]
        end

        subgraph data["Qlns.DataAccess — Data Layer"]
            direction LR
            recRepo["Recruitment Repositories<br/><i>[Partial]</i>"]
            hrRepo["Core HR Repositories<br/><i>[Proposed]</i>"]
            contractRepo["Contract Repositories<br/><i>[Proposed]</i>"]
            reportRepo["Reporting Read Repositories<br/><i>[Proposed]</i>"]
            identityRepo["Identity Repositories<br/><i>[Proposed]</i>"]
            integrationRepo["Integration & Idempotency Store<br/><i>[Proposed]</i>"]
            auditRepo["Audit & Outbox Repositories<br/><i>[Proposed]</i>"]
            uow["EF Core DbContext & Unit of Work<br/><i>[Component — Partial]</i>"]
            objectAdapter["Object Storage Adapter<br/><i>[Component — Proposed]</i>"]
        end
    end

    web -->|"REST/JSON"| pipeline
    callbacks -->|"authenticated/signed webhook"| pipeline
    pipeline -->|"validate token / obtain claims"| idp
    pipeline --> recApi
    pipeline --> hrApi
    pipeline --> contractApi
    pipeline --> reportApi
    pipeline --> adminApi
    pipeline --> webhookApi
    pipeline --> authorization

    recApi --> recLogic
    hrApi --> hrLogic
    contractApi --> contractLogic
    reportApi --> reportLogic
    adminApi --> adminLogic
    webhookApi --> integrationLogic

    recLogic --> recRepo
    hrLogic --> hrRepo
    contractLogic --> contractRepo
    reportLogic --> reportRepo
    adminLogic --> identityRepo
    integrationLogic --> integrationRepo

    recLogic --> auditOutbox
    hrLogic --> auditOutbox
    contractLogic --> auditOutbox
    adminLogic --> auditOutbox
    integrationLogic --> auditOutbox
    auditOutbox --> auditRepo

    recRepo --> uow
    hrRepo --> uow
    contractRepo --> uow
    reportRepo --> uow
    identityRepo --> uow
    integrationRepo --> uow
    auditRepo --> uow
    uow -->|"EF Core / Npgsql"| db
    contractLogic --> objectAdapter
    recLogic --> objectAdapter
    objectAdapter -->|"authorized object API"| objects

    classDef partial fill:#1168bd,color:#fff,stroke:#0b4884
    classDef proposed fill:#6b4f9b,color:#fff,stroke:#463267
    classDef external fill:#777,color:#fff,stroke:#555
    class pipeline,recApi,recLogic,recRepo,uow partial
    class authorization,hrApi,contractApi,reportApi,adminApi,webhookApi,hrLogic,contractLogic,reportLogic,adminLogic,integrationLogic,auditOutbox,hrRepo,contractRepo,reportRepo,identityRepo,integrationRepo,auditRepo,objectAdapter proposed
    class web,callbacks,idp,db,objects external
```

Ba **tier runtime** là: Presentation Tier (React Web), Application Tier (ASP.NET Core API + .NET Worker) và Data Tier (PostgreSQL). Chúng là ranh giới triển khai/mạng; Worker không tạo tier thứ tư. Ba **layer source code** bên trong ASP.NET Core application tier là Presentation, Business Logic và Data Access:

- Presentation chỉ chuyển HTTP contract thành command/query, gọi Business Logic và map kết quả sang DTO/Problem Details.
- Business Logic sở hữu use case, domain workflow, authorization theo tài nguyên và các repository/adapter contract; không phụ thuộc ASP.NET Core hoặc EF Core.
- Data Access triển khai contract của Business Logic bằng EF Core/provider adapter. `Qlns.Api` chỉ tham chiếu Data Access tại composition root để đăng ký dependency.
- Các module ghi dữ liệu phải đi qua Unit of Work và cùng transaction ghi audit/outbox; Reporting chỉ dùng read model đã áp dụng data scope.

<a id="c4-level-3-worker"></a>

### 5.4 C4 Level 3 — inside .NET Background Worker

```mermaid
flowchart LR
    db[("PostgreSQL<br/><i>[Container]</i>")]
    jobboards["Job Boards<br/><i>[External System]</i>"]
    comms["Email / Calendar<br/><i>[External System]</i>"]
    esign["E-signature<br/><i>[External System]</i>"]
    observe["Observability Platform<br/><i>[External System]</i>"]

    subgraph worker[".NET Background Worker [Container · Application Tier — Proposed]"]
        direction TB
        scheduler["Job Scheduler<br/><i>[Component]</i><br/>bounded cadence and distributed lock"]
        outbox["Outbox Poller<br/><i>[Component]</i><br/>claim committed messages"]
        dispatcher["Event Dispatcher<br/><i>[Component]</i><br/>route event to handler"]
        contractExpiry["Contract Expiry Scanner<br/><i>[Component]</i>"]
        offerExpiry["Offer Expiry Processor<br/><i>[Component]</i>"]
        effectiveEvents["Effective-date Employee Processor<br/><i>[Component]</i>"]
        notification["Notification & Calendar Handler<br/><i>[Component]</i>"]
        providerSync["Provider Sync Handlers<br/><i>[Component]</i><br/>job board and e-signature reconciliation"]
        retry["Retry, Dead-letter & Reconciliation<br/><i>[Component]</i>"]
        adapters["Provider Adapters<br/><i>[Component]</i><br/>timeout, idempotency and signature validation"]
        telemetry["Worker Telemetry<br/><i>[Component]</i>"]

        scheduler --> contractExpiry
        scheduler --> offerExpiry
        scheduler --> effectiveEvents
        scheduler --> providerSync
        outbox --> dispatcher
        dispatcher --> notification
        dispatcher --> providerSync
        notification --> adapters
        providerSync --> adapters
        notification --> retry
        providerSync --> retry
        contractExpiry --> retry
        offerExpiry --> retry
        effectiveEvents --> retry
        retry --> telemetry
    end

    db -->|"claim pending jobs/outbox"| outbox
    scheduler -->|"read due work"| db
    contractExpiry -->|"transactional state + outbox"| db
    offerExpiry -->|"transactional state + outbox"| db
    effectiveEvents -->|"transactional state + outbox"| db
    dispatcher -->|"delivery status"| db
    retry -->|"attempt/dead-letter/reconciliation state"| db
    adapters -->|HTTPS| jobboards
    adapters -->|HTTPS| comms
    adapters -->|HTTPS| esign
    telemetry -->|"logs, metrics and traces"| observe

    classDef proposed fill:#6b4f9b,color:#fff,stroke:#463267
    classDef external fill:#777,color:#fff,stroke:#555
    class scheduler,outbox,dispatcher,contractExpiry,offerExpiry,effectiveEvents,notification,providerSync,retry,adapters,telemetry proposed
    class db,jobboards,comms,esign,observe external
```

Worker chưa có implementation đã xác minh. Mọi handler phải idempotent, claim công việc an toàn khi chạy nhiều instance, retry hữu hạn và chuyển dead-letter để đối soát; không giữ database transaction trong khi gọi provider ngoài.

### 5.5 Business modules and data ownership

Hai module nghiệp vụ được chọn triển khai trước — **Core HR** (gồm Contracts) và **Recruitment** — đều đã có bảng trong canonical schema v1 (23 bảng). Attendance & Leave nằm ngoài phạm vi; thiết kế của nó được giữ tại [deferred/attendance_leave/](deferred/attendance_leave/README.md).

| Module | Responsibilities | Canonical tables | Current evidence |
|---|---|---|---|
| Core HR — Profile & Organization | employee, department, position | `employees`, `departments`, `positions` | UI prototype + canonical schema + OpenAPI + story có AC |
| Core HR — Lifecycle | onboarding, events, documents, probation, offboarding | `onboarding_tasks`, `employee_events`, `employee_documents`, `probation_reviews`, `offboarding_cases`, `offboarding_tasks` | UI prototype (onboarding) + canonical schema + OpenAPI + story có AC |
| Core HR — Contracts | contract lifecycle, expiry alert, addendum | `contracts`, `contract_addenda` | UI prototype + canonical schema + OpenAPI + story có AC |
| Recruitment | job, candidate, application, interview, evaluation, offer | `job_postings`, `candidates`, `resumes`, `applications`, `application_stage_events`, `interviews`, `evaluations`, `offers` | UI prototype + canonical schema + OpenAPI + module mẫu trong skeleton |
| Identity/Audit/Notification | actor, roles/data scope, audit, delivery state | `users`, `user_roles`, `audit_logs`, `outbox_messages` | canonical v1 design |
| Reporting | authorized read models and export | read model trên bảng của các module trên; chưa có bảng riêng | UI/SRS concept |

**Ownership rule:** Recruitment sở hữu dữ liệu ứng viên tới thời điểm offer được chấp nhận; từ đó Core HR sở hữu `employees` và mọi thứ phái sinh. Liên kết ngược duy nhất là `employees.source_application_id` (unique), dùng để đảm bảo một offer chỉ tạo một nhân viên. Chiều phụ thuộc là Core HR → Recruitment (đọc), một chiều.

**Trạng thái nhân sự chỉ đổi qua sự kiện:** `employees.status`, phòng ban, chức danh và quản lý trực tiếp không được sửa thẳng; mọi thay đổi đi qua `employee_events` đã `approved` và được áp dụng đúng `effective_date`. Probation review và offboarding case đều kết thúc bằng việc sinh một `employee_events`, không ghi trực tiếp vào hồ sơ.

Database không có C4 Component diagram riêng vì đây là data-store container, không phải executable container. Thành phần bên trong được mô hình hóa bằng ownership ở bảng trên và ERD/DDL trong `database/`.

### 5.6 Target code structure

```text
frontend/
├── src/app/                      # composition, routing, session
├── src/features/
│   ├── recruitment/              # api, components, hooks, pages — sample module (skeleton)
│   ├── core-hr/                  # proposed
│   ├── contracts/                # proposed
│   ├── reporting/                # proposed
│   └── administration/           # proposed
├── src/shared/                   # design system and generic UI
└── src/api/                      # shared client and Problem Details mapping

backend/
├── src/Qlns.Api/                 # Presentation layer
├── src/Qlns.BusinessLogic/       # module services/domain + repository contracts
├── src/Qlns.DataAccess/          # module repositories + EF Core/adapters
├── src/Qlns.Worker/              # proposed background processing container
├── tests/Qlns.BusinessLogic.UnitTests/
└── tests/Qlns.IntegrationTests/  # proposed — required before the first slice

api/openapi.yaml                  # contract-first OpenAPI 3.0.3 (87 operations)
database/schema.sql               # canonical schema contract before EF migrations (23 tables)
```

`tests/Qlns.IntegrationTests/` chưa tồn tại nhưng là điều kiện bắt buộc trước slice đầu tiên: các invariant quan trọng nhất của Core HR (một offer đang mở mỗi đơn, một hợp đồng chính đang hiệu lực, một case thôi việc đang mở, áp dụng biến động đúng ngày hiệu lực) là partial unique index và conditional update ở database, không thể verify bằng repository giả lập.

Một use case mới nằm trong module sở hữu nghiệp vụ, cùng command/query, policy và test. Không đặt business rule trong route, component UI hoặc database trigger tổng quát.

---

## 6. Runtime View

Các sequence dưới đây mô tả các runtime scenario có ý nghĩa kiến trúc. Sequence nghiệp vụ chi tiết theo từng User Story được quản lý tại [Sequence Diagrams](sequence_diagrams.md).

### 6.1 Read employee list — happy path

```mermaid
sequenceDiagram
    autonumber
    actor U as HR User
    participant W as Web Application
    participant A as Backend API
    participant Z as Authorization
    participant D as PostgreSQL

    U->>W: Mở danh sách nhân viên
    W->>A: GET /api/employees?filter&page
    A->>Z: authorize(actor, employee.read, scope)
    Z-->>A: allowed + data scope
    A->>D: SELECT bounded fields + scope + page
    D-->>A: rows + total
    A-->>W: 200 EmployeeList DTO
    W-->>U: Render success/empty state
```

### 6.2 Read employee list — authorization failure twin

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant W as Web Application
    participant A as Backend API
    participant Z as Authorization
    participant D as PostgreSQL

    U->>W: Mở hồ sơ ngoài phạm vi
    W->>A: GET /api/employees/{id}
    A->>Z: authorize(actor, employee.read, target)
    Z-->>A: denied
    A-->>W: 403 Problem Details + correlationId
    W-->>U: Forbidden state
    Note over A,D: Database không trả dữ liệu nghiệp vụ cho request bị từ chối
```

### 6.3 Advance recruitment stage — success and conflict

```mermaid
sequenceDiagram
    autonumber
    actor R as Recruiter
    participant W as Recruitment UI
    participant A as Backend API
    participant S as Pipeline Service
    participant D as PostgreSQL
    participant O as Outbox

    R->>W: Advance candidate
    W->>A: POST /applications/{id}/advance {target, version}
    A->>S: advance(actor, id, target, version)
    S->>S: authorize + validate transition
    S->>D: lock/read application
    alt version/state changed
        D-->>S: current version differs
        S-->>A: Conflict
        A-->>W: 409 + current state reference
    else valid
        S->>D: update stage + transition + audit + outbox
        D-->>S: commit
        S-->>A: updated application
        A-->>W: 200 DTO
        O-->>O: delivered asynchronously after commit
    end
```

### 6.4 Candidate-to-employee handoff

```mermaid
sequenceDiagram
    autonumber
    actor H as HR Officer
    participant A as Backend API
    participant S as Onboarding Service
    participant D as PostgreSQL

    H->>A: POST /applications/{id}/onboard (Idempotency-Key)
    A->>S: onboard(actor, applicationId, key)
    S->>D: lock application + candidate + accepted offer
    S->>S: validate Hired/Accepted + duplicate policy
    alt employee already linked
        S-->>A: existing employee result / 409 by contract
    else valid
        S->>D: insert employee + source link + tasks + audit
        D-->>S: atomic commit
        S-->>A: Employee DTO
    end
```

### 6.5 External notification after transaction

```mermaid
sequenceDiagram
    autonumber
    participant S as Application Service
    participant D as PostgreSQL
    participant W as Worker
    participant P as Provider

    S->>D: business change + outbox row (one transaction)
    D-->>S: commit
    W->>D: claim pending outbox
    W->>P: send with idempotency key
    alt provider unavailable
        P--xW: timeout/5xx
        W->>D: bounded retry schedule + last error
    else accepted
        P-->>W: provider message id
        W->>D: mark delivered
    end
```

---

## 7. Deployment View

```mermaid
flowchart TB
    subgraph edge["Public Edge — Proposed"]
        ingress["DNS / TLS / WAF / Rate limit"]
        web["Static Web Hosting / CDN"]
    end

    subgraph app["Private Application Zone — Proposed"]
        api["Backend API<br/><i>replica 1..N</i>"]
        worker["Background Worker<br/><i>replica 1..N</i>"]
    end

    subgraph data["Private Data Zone — Proposed"]
        pg[("PostgreSQL<br/>HA + encrypted backup")]
        objects[("Private Object Storage")]
    end

    providers["External Providers"]
    ops["CI/CD + Secrets + Observability"]

    ingress --> web
    ingress --> api
    api --> pg
    api --> objects
    worker --> pg
    worker --> providers
    ops -.-> web
    ops -.-> api
    ops -.-> worker
```

**Deployment rules**

| Rule | Reason |
|---|---|
| Browser chỉ truy cập Public Edge; database/object storage không public | giảm attack surface và ngăn client bypass API |
| Web, API và worker là artifact versioned/immutable | rollback và trace release rõ ràng |
| Migration chạy như release step riêng, không dùng ORM auto-create production | kiểm soát compatibility và rollback/roll-forward |
| Business commit và outbox write nằm trong cùng DB transaction | không mất side effect sau commit |
| Secret đến từ secret manager/reference theo môi trường | không đóng gói credential trong source/image |
| Readiness kiểm tra dependency thiết yếu; liveness chỉ kiểm tra process | tránh route traffic vào instance chưa sẵn sàng |
| Backup phải có restore drill; RPO/RTO do ADR phê duyệt | backup không được xem là hữu ích nếu chưa phục hồi thử |

Docker Compose ba service có thể dùng cho local development sau này, nhưng hiện không tồn tại và không phải production topology.

---

## 8. Crosscutting Concepts

| Concept | Rule | Detail |
|---|---|---|
| **Identity** | mọi business request có authenticated actor do server xác lập | actor gồm user/employee ID, roles, permissions, data scope, correlation ID |
| **Authorization** | deny-by-default tại application boundary; UI hiding không phải security | RBAC kết hợp own/direct-report/department/organization scope |
| **Validation** | DTO validation ở delivery; invariant/state rule ở domain/application | lỗi field dùng `422`; conflict state/version dùng `409` |
| **Error handling** | error envelope/Problem Details nhất quán; không lộ stack, SQL, secret | lỗi có stable code, safe message, fields và correlation ID |
| **Workflow** | command tường minh; không patch `status` tùy ý | transition kiểm tra actor, current state, target, guards và version |
| **Transaction** | application service sở hữu transaction boundary | update aggregate, audit và outbox liên quan phải nguyên tử |
| **Audit** | mọi thay đổi nhạy cảm ghi actor, action, target, server time, result | audit khác operational log và không chứa toàn payload nhạy cảm |
| **Persistence** | owner module là writer duy nhất cho bảng của mình | module khác dùng application interface/reference, không dùng bảng như API ngầm |
| **Time** | instant lưu UTC; business date giữ semantic riêng; UI theo organization timezone | timezone mặc định đề xuất `Asia/Ho_Chi_Minh`, cần xác nhận |
| **Money** | dùng decimal/numeric và currency; không dùng floating point | calculation/rounding ở backend |
| **Integration** | timeout, bounded retry, idempotency và reconciliation | lỗi gửi không rollback business state đã commit |
| **Logging** | structured log, redaction bắt buộc | không log token, CV, hợp đồng, salary hoặc payload restricted |
| **UI state** | Loading, Empty, Forbidden, Validation, Conflict, Unavailable, Success | không fallback im lặng sang demo data khi API lỗi |
| **Accessibility** | keyboard, label, focus, contrast và lỗi gắn field | kiểm chứng WCAG 2.1 AA cho luồng thiết yếu |

---

<a id="architecture-decisions"></a>

## 9. Architecture Decisions (ADR index)

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Kiến trúc 3-tier React – ASP.NET Core API – PostgreSQL và backend 3-layer | Accepted 2026-09-15 |
| ADR-002 | Backend modular monolith trước microservices | Proposed |
| ADR-003 | Backend thực thi authorization và business rules | Proposed |
| ADR-004 | REST/JSON, DTO và contract-first OpenAPI 3.0.3 | Accepted 2026-09-15 |
| ADR-005 | PostgreSQL system of record và versioned migration | Proposed |
| ADR-006 | Explicit commands và state transitions | Proposed |
| ADR-007 | Ports/adapters, outbox và reliable delivery | Proposed |
| ADR-008 | Feature-based React frontend và shared API client | Accepted 2026-09-15 |
| ADR-009 | .NET 10, ASP.NET Core, EF Core và PostgreSQL | Accepted 2026-09-15 |

**Open decisions:** Identity Provider; object storage; worker/queue; hosting platform; SLA; RPO/RTO; retention và data residency. EF Core migration là công cụ migration mục tiêu nhưng migration đầu tiên chỉ được sinh sau khi cài .NET 10 SDK và review model/schema drift.

Không ADR nào chuyển sang Accepted chỉ vì công nghệ xuất hiện trong prototype, sơ đồ hoặc file DDL. ADR Accepted phải có owner, ngày phê duyệt, alternatives và consequences.

---

## 10. Quality Requirements (stimulus → response → measure)

| # | Source | Stimulus | Environment | Response | Measure |
|---|---|---|---|---|---|
| QR1 | Người dùng ngoài quyền | đọc hồ sơ/hợp đồng restricted | production | request bị từ chối trước khi trả dữ liệu | 100% authorization tests trả `401/403`; không rò field restricted |
| QR2 | Hai recruiter | cùng chuyển một application | concurrent requests | đúng một transition commit | request còn lại trả `409` hoặc idempotent result; không có transition trùng |
| QR3 | HR Officer | onboard lại cùng application | retry sau timeout | trả cùng employee hoặc conflict xác định | không tạo employee/task trùng |
| QR4 | Provider | email/calendar timeout | sau business commit | retry hữu hạn, business state giữ nguyên | không rollback trạng thái đã commit; có delivery/reconciliation record |
| QR5 | HR User | tải danh sách nhân viên | tải mục tiêu, warm service | trả page được scope/filter | p95 ≤ 500 ms; query bounded; không N+1 |
| QR6 | Auditor | truy vết thay đổi hợp đồng | retention window | nhận actor, time, before/after reference và result | 100% command hợp đồng có audit link |
| QR7 | Operations | database unavailable | runtime | readiness fail, request không ghi dở dang | rollback hoàn toàn; `5xx` an toàn + correlation ID |
| QR8 | Operations | restore từ backup | recovery drill | hệ thống phục hồi nhất quán | đạt RPO/RTO sau khi ADR tương ứng được Accepted |
| QR9 | Keyboard user | hoàn thành một luồng ưu tiên | desktop/tablet | thao tác không cần chuột | 100% control thiết yếu keyboard-accessible, focus visible |

Các budget chưa có dữ liệu tải hoặc hạ tầng được coi là **provisional** và phải được benchmark lại trước production.

---

## 11. Risks and Technical Debt

| # | Risk | Impact | Likelihood | Mitigation | Owner |
|---|---|---|---|---|---|
| R1 | UI prototype bị hiểu nhầm là frontend đã hoàn thành | High | High | nhãn Design-only, acceptance criteria và không dùng mock data fallback production | Product + Architecture |
| R1b | Có OpenAPI contract đầy đủ bị hiểu nhầm là API đã hoạt động | High | High | `x-implementation-status` trên từng operation; hiện **chưa operation nào** ở trạng thái implemented — source trong `src/` chỉ là skeleton cấu trúc | Architecture |
| R2 | Canonical schema chưa được chuyển thành EF migration có version | High | High | migration plan và constraint/invariant integration tests trên PostgreSQL thật | Data + Backend |
| R2b | Thiết kế Attendance & Leave đã tách ra `deferred/` có thể drift khỏi canonical (bảng `employees`, `users`, error model) nếu module đó quay lại phạm vi | Medium | Medium | ghi rõ phụ thuộc trong `deferred/attendance_leave/README.md`; review lại toàn bộ fragment trước khi ghép về | Architecture |
| R3 | Stack được chọn theo sơ đồ mà không qua decision process | Medium | High | ADR framework/version và proof-of-concept vertical slice | Architecture |
| R4 | Business rule rò vào UI/router | High | Medium | application/domain boundary, code review và architecture fitness tests | Backend lead |
| R5 | RBAC chỉ ẩn nút, thiếu data scope server-side | Critical | Medium | deny-by-default policy tests cho từng role/scope | Security |
| R6 | Candidate-to-employee handoff tạo dữ liệu trùng | High | Medium | source link, unique/business key, lock/version và idempotency test | Core HR + Recruitment |
| R6b | Kết quả thử việc / đóng case thôi việc sinh trùng `employee_events` khi retry | High | Medium | liên kết một-một (`probation_reviews.employee_event_id`, `offboarding_cases.employee_event_id`) và idempotency test | Core HR |
| R7 | Provider failure làm sai trạng thái nghiệp vụ | High | Medium | outbox, delivery state, bounded retry và reconciliation | Integration owner |
| R8 | Dữ liệu nhạy cảm xuất hiện trong log/export/test | Critical | Medium | classification, DTO allowlist, redaction, synthetic test data, export audit | Security + Data |
| R9 | Mermaid/C4/ADR drift khỏi implementation tương lai | Medium | High | docs-first PR checklist và traceability/fitness gates | Architecture |
| R10 | SLA/RPO/RTO không có owner | High | Medium | business impact analysis và ADR trước production design | Sponsor + Operations |

**Accepted technical debt:** chưa có. Mọi technical debt chỉ được Accepted khi có owner, impact, expiry/revisit condition và quyết định phê duyệt.

---

## 12. Architecture Fitness Functions

Các gate dưới đây là target bắt buộc. Skeleton source chỉ mới có unit test cho module mẫu; những gate chưa có executable job vẫn phải giữ trạng thái Planned.

| Test / Gate | Rule enforced | Fails when | Status / planned location |
|---|---|---|---|
| `FrontendCannotAccessDatabase` | C2, §5.1 | frontend dependency/import chứa DB driver hoặc connection | Planned — `tests/architecture` |
| `LayersPointInward` | §5.3 | domain phụ thuộc API, ORM hoặc provider SDK | Planned — `tests/architecture` |
| `NoCrossModuleTableWrites` | §5.5 | module ghi trực tiếp bảng do module khác sở hữu | Planned — architecture/integration tests |
| `EveryBusinessEndpointRequiresAuthorization` | Q1 | endpoint nghiệp vụ thiếu policy/actor | Planned — security fitness tests |
| `RestrictedFieldsAreAllowlisted` | Q1, §8 | response DTO vô tình expose salary/document/private field | Planned — contract tests |
| `EveryStateChangeUsesACommand` | Q3 | API cho phép generic patch trạng thái | Planned — route/contract tests |
| `EveryCommandWritesAudit` | Q2 | command nhạy cảm commit mà không có audit record | Planned — integration tests |
| `OutboxIsAtomicWithBusinessChange` | Q2/Q7 | commit business state nhưng thiếu outbox hoặc ngược lại | Planned — DB integration tests |
| `IdempotentWebhookConformance` | Q7 | cùng external event tạo side effect lần hai | Planned — integration conformance tests |
| `MigrationsUpgradeFromPreviousRelease` | C3 | migration fail hoặc schema không tương thích | Planned — CI database job |
| `OpenApiBreakingChangeGate` | ADR-004 | contract breaking change không có version/ADR | Planned — CI contract diff |
| `NoSensitiveDataInLogs` | §8 | log fixture chứa token, CV, salary hoặc restricted payload | Planned — security tests |
| `CriticalFlowsMeetAccessibilityGate` | Q4 | axe/keyboard checks fail ở luồng ưu tiên | Planned — frontend CI |
| `ReadPerformanceBudget` | Q5 | employee/recruitment list vượt provisional p95 budget | Planned — performance job |
| `MarkdownLinksAndMermaidAreValid` | C9 | tài liệu có link hỏng hoặc Mermaid không parse | Partially available — repository validation |

CI tương lai phải chạy các gate phù hợp trên mọi pull request. Một rule chỉ được đánh dấu **Enforced** khi test/job thực sự tồn tại, có thể fail và được required trong CI.

---

**Requirements:** [Functional specifications](functional_specifications.md) · **User Stories:** [INVEST backlog](user_stories.md) · **Use Cases:** [Use case diagrams](use_cases.md) · **Sequences:** [Sequence diagrams](sequence_diagrams.md) · **Database:** [Database design](../database/database_design.md)
