# 📊 HỆ THỐNG SƠ ĐỒ KIẾN TRÚC & LUỒNG NGHIỆP VỤ (SYSTEM DIAGRAMS)
## Hệ Thống Quản Trị Nhân Sự & Tuyển Dụng Tập Trung (QLNS / NexusHR)

> **Trạng thái:** toàn bộ sơ đồ trong tài liệu này là **thiết kế Proposed/Target**. Project hiện chưa có frontend application, Backend API, Docker Compose hoặc runtime deployment. Tên công nghệ, endpoint, container và chuỗi tương tác dưới đây là phương án tham chiếu cần được phê duyệt trước implementation.

> **Nguồn kiến trúc authoritative:** [05 — Architecture (arc42 + C4)](./05_architecture.md). Tài liệu này chỉ bổ sung các sơ đồ nghiệp vụ chi tiết.

---

## Danh Mục Sơ Đồ

1. [Sơ đồ Kiến trúc Hệ thống Tổng thể (System Architecture Diagram)](#1-sơ-đồ-kiến-trúc-hệ-thống-tổng-thể-system-architecture-diagram)
2. [Sơ đồ Use Case Tổng quan (Overall Use Case Diagram)](#2-sơ-đồ-use-case-tổng-quan-overall-use-case-diagram)
3. [Sơ đồ Máy Trạng thái Quy trình Tuyển dụng (Recruitment ATS State Machine)](#3-sơ-đồ-máy-trạng-thái-quy-trình-tuyển-dụng-recruitment-ats-state-machine)
4. [Sơ đồ Tuần tự: Quy trình Tuyển dụng & Đánh giá (ATS Hiring Sequence Diagram)](#4-sơ-đồ-tuần-tự-quy-trình-tuyển-dụng--đánh-giá-ats-hiring-sequence-diagram)
5. [Sơ đồ Tuần tự: Tiếp nhận Onboarding & Chuyển đổi Nhân viên (Onboarding Sequence Diagram)](#5-sơ-đồ-tuần-tự-tiếp-nhận-onboarding--chuyển-đổi-nhân-viên-onboarding-sequence-diagram)
6. [Sơ đồ Tuần tự: Quản lý Biến động Nhân sự (Internal Mobility Sequence Diagram)](#6-sơ-đồ-tuần-tự-quản-lý-biến-động-nhân-sự-internal-mobility-sequence-diagram)
7. [Sơ đồ Luồng Hoạt động Tuyển dụng Toàn trình (Recruitment Activity Flowchart)](#7-sơ-đồ-luồng-hoạt-động-tuyển-dụng-toàn-trình-recruitment-activity-flowchart)
8. [Sơ đồ Triển khai Container Docker Đề xuất (Proposed Docker Deployment Diagram)](#8-sơ-đồ-triển-khai-container-docker-đề-xuất-proposed-docker-deployment-diagram)

---

## 1. Sơ đồ Kiến trúc Hệ thống Tổng thể (System Architecture Diagram)

Phương án tham chiếu sử dụng kiến trúc nhiều tầng và Docker Compose. Sơ đồ không phản ánh môi trường đang chạy:

```mermaid
flowchart TB
    subgraph CLIENT_LAYER["1. TẦNG GIAO DIỆN (CLIENT LAYER)"]
        direction LR
        A1["React 18 SPA\n(Vite 5 / Port 5173)"]
        A2["Standalone UI Mockup\n(Tailwind CSS / HTML5)"]
        A3["Mobile Browser / Tablet\n(Responsive Viewport)"]
    end

    subgraph API_GATEWAY["2. ĐIỀU PHỐI & MÔI TRƯỜNG DOCKER"]
        direction TB
        B1["Docker Bridge Network: qlns_network"]
        B2["CORS Middleware\n(Allow Origins: localhost:5173, file://)"]
    end

    subgraph BACKEND_LAYER["3. TẦNG DỊCH VỤ BACKEND (FASTAPI APP / PORT 8000)"]
        direction TB
        C1["FastAPI Application Core (Python 3.12 / Uvicorn)"]
        
        subgraph ROUTERS["APIRouters"]
            R1["/api/employees\n(Hồ sơ, Phòng ban, Chức vụ, Thống kê)"]
            R2["/api/recruitment\n(Pipeline Kanban, Jobs, Candidates, Advance)"]
            R3["/api/health\n(Liveness & DB Health Probe)"]
        end

        subgraph DATA_ACCESS["Data Access & Validation Layer"]
            D1["Pydantic v2 Schemas\n(Input Validation & Serialization)"]
            D2["SQLAlchemy 2.0 ORM Engine\n(Connection Pooling & Sessionmaker)"]
        end

        C1 --> ROUTERS
        ROUTERS --> DATA_ACCESS
    end

    subgraph DATABASE_LAYER["4. TẦNG CƠ SỞ DỮ LIỆU (POSTGRESQL 16 ALPINE / PORT 5432)"]
        direction TB
        E1[("Database: qlns_db")]
        
        subgraph CORE_HR_TABLES["Nhóm Bảng Core HR"]
            T1["departments"]
            T2["positions"]
            T3["employees"]
            T4["contracts"]
            T5["employee_events"]
            T6["onboarding_tasks"]
            T7["employee_documents"]
        end

        subgraph ATS_TABLES["Nhóm Bảng ATS Tuyển Dụng"]
            T8["job_postings"]
            T9["candidates"]
            T10["resumes"]
            T11["applications"]
            T12["interviews"]
            T13["evaluations"]
            T14["offers"]
        end

        E1 --- CORE_HR_TABLES
        E1 --- ATS_TABLES
    end

    CLIENT_LAYER -->|HTTP / JSON REST API| API_GATEWAY
    API_GATEWAY --> BACKEND_LAYER
    DATA_ACCESS -->|psycopg2 / SQL Queries| DATABASE_LAYER
```

---

## 2. Sơ đồ Use Case Tổng quan (Overall Use Case Diagram)

Sơ đồ thể hiện quyền hạn và các hành vi tương tác của từng nhóm người dùng với các phân hệ chính:

```mermaid
flowchart LR
    %% Actors
    Admin(["Super Admin"])
    HRMgr(["HR Director / Manager"])
    Recruiter(["Recruiter (TA)"])
    Interviewer(["Hiring Manager / Interviewer"])
    Employee(["Nhân Viên (Employee)"])
    Candidate(["Ứng Viên (Candidate)"])

    %% Subsystems
    subgraph SUB_ATS["Phân Hệ Tuyển Dụng Thông Minh (ATS)"]
        UC_JOB_REQ["Tạo yêu cầu tuyển dụng"]
        UC_JOB_PUB["Đăng tin tuyển dụng (Job Posting)"]
        UC_CV_SUBMIT["Nộp hồ sơ ứng tuyển"]
        UC_AI_PARSE["Bóc tách CV tự động (AI Parsing)"]
        UC_KANBAN["Theo dõi & Kéo thả Kanban Pipeline"]
        UC_SCHEDULE["Xếp lịch phỏng vấn"]
        UC_SCORECARD["Chấm điểm đánh giá (Scorecard)"]
        UC_OFFER["Lập & Phê duyệt Offer Letter"]
    end

    subgraph SUB_CORE["Phân Hệ Hồ Sơ & Vòng Đời Nhân Sự"]
        UC_PROFILE_MGMT["Quản lý Danh bạ Hồ sơ Master Data"]
        UC_ORG_CHART["Xem & Quản lý Sơ đồ Tổ chức"]
        UC_ONBOARDING["Theo dõi Onboarding Checklist"]
        UC_EVENTS["Ghi nhận Biến động (Thăng chức, Điều chuyển)"]
        UC_DOCS["Lưu trữ Văn bản & Bằng cấp"]
    end

    subgraph SUB_CONTRACT["Phân Hệ Quản Lý Hợp Đồng"]
        UC_CONT_CREATE["Lập hợp đồng (Thử việc, Chính thức)"]
        UC_CONT_ALERT["Nhận cảnh báo hết hạn hợp đồng"]
    end

    subgraph SUB_SYS["Hệ Thống & Báo Cáo"]
        UC_DASHBOARD["Xem Dashboard & Báo cáo Tuyển dụng/Quân số"]
        UC_USER_MGMT["Quản lý tài khoản & Phân quyền"]
    end

    %% Interactions
    Candidate --> UC_CV_SUBMIT
    Interviewer --> UC_JOB_REQ
    Interviewer --> UC_SCORECARD

    Recruiter --> UC_JOB_PUB
    Recruiter --> UC_AI_PARSE
    Recruiter --> UC_KANBAN
    Recruiter --> UC_SCHEDULE
    Recruiter --> UC_OFFER

    HRMgr --> UC_JOB_PUB
    HRMgr --> UC_OFFER
    HRMgr --> UC_EVENTS
    HRMgr --> UC_CONT_CREATE
    HRMgr --> UC_DASHBOARD

    Employee --> UC_ORG_CHART
    Employee --> UC_PROFILE_MGMT

    Admin --> UC_USER_MGMT
    Admin --> UC_DASHBOARD
    Admin --> UC_PROFILE_MGMT
    Admin --> UC_CONT_ALERT
```

---

## 3. Sơ đồ Máy Trạng thái Quy trình Tuyển dụng (Recruitment ATS State Machine)

Mỗi hồ sơ ứng tuyển (`Application`) di chuyển tuần tự qua 6 giai đoạn chuẩn, với các khả năng rẽ nhánh:

```mermaid
stateDiagram-v2
    [*] --> Sourced_Applied : Ứng viên nộp CV / Recruiter import

    Sourced_Applied --> AI_Screening : AI Parser bóc tách & Chấm điểm phù hợp
    Sourced_Applied --> Rejected : Không đủ điều kiện cơ bản

    AI_Screening --> Tech_Interview : Đạt tiêu chí sơ loại (AI Match >= 80%)
    AI_Screening --> Talent_Pool : Giữ lại làm nguồn ứng viên tiềm năng
    AI_Screening --> Rejected : Không phù hợp

    Tech_Interview --> Executive_Round : Vượt qua vòng phỏng vấn chuyên môn (Scorecard >= 4.0)
    Tech_Interview --> Tech_Interview_R2 : Yêu cầu phỏng vấn bổ sung
    Tech_Interview --> Rejected : Đánh giá No Hire

    Executive_Round --> Offer_Letter : Ban Lãnh đạo phê duyệt tuyển dụng
    Executive_Round --> Rejected : Không phù hợp văn hóa / Yêu cầu

    Offer_Letter --> Hired_Ready : Ứng viên đồng ý & Ký thư mời (Accepted)
    Offer_Letter --> Withdrawn_Declined : Ứng viên từ chối / Đàm phán không thành

    Hired_Ready --> Onboarding_Active : Kích hoạt hồ sơ nhân viên & Bàn giao thiết bị
    Onboarding_Active --> [*] : Hoàn tất chu trình tuyển dụng

    Rejected --> Archive_TalentPool : Lưu kho tài năng phục vụ đợt sau
    Withdrawn_Declined --> Archive_TalentPool
```

---

## 4. Sơ đồ Tuần tự: Quy trình Tuyển dụng & Đánh giá (ATS Hiring Sequence Diagram)

Quy trình tuần tự từ lúc đăng tin tuyển dụng đến khi phê duyệt Offer:

```mermaid
sequenceDiagram
    autonumber
    actor Recruiter as Chuyên Viên Tuyển Dụng
    actor Candidate as Ứng Viên
    actor Interviewer as Người Phỏng Vấn (Hiring Lead)
    participant WebUI as Frontend (React / Web UI)
    participant API as FastAPI Backend (/api/recruitment)
    participant DB as PostgreSQL Database

    Note over Recruiter, DB: 1. Đăng tin & Nhận hồ sơ
    Recruiter->>WebUI: Nhập thông tin việc làm mới (Title, Dept, Headcount)
    WebUI->>API: POST /api/recruitment/jobs
    API->>DB: INSERT INTO job_postings
    DB-->>API: Trả về Job ID mới
    API-->>WebUI: 201 Created (Thông báo thành công)

    Candidate->>WebUI: Nộp CV ứng tuyển trực tuyến
    WebUI->>API: Gửi tệp CV & thông tin ứng tuyển
    API->>DB: INSERT INTO candidates & resumes & applications
    DB-->>API: Bản ghi ứng tuyển mới (Stage: 'Sourced & Applied')

    Note over Recruiter, Interviewer: 2. Sàng lọc & Xếp lịch phỏng vấn
    Recruiter->>WebUI: Kéo thẻ ứng viên sang 'Tech Interview'
    WebUI->>API: POST /api/recruitment/applications/{id}/advance
    API->>DB: UPDATE applications SET stage = 'Tech Interview'
    
    Recruiter->>WebUI: Chọn thời gian & gán Interviewer
    WebUI->>API: Tạo lịch phỏng vấn
    API->>DB: INSERT INTO interviews (status: 'Scheduled')
    API-->>Candidate: Tự động gửi Email mời phỏng vấn & Meeting link
    API-->>Interviewer: Gửi thông báo lịch & tài liệu ứng viên

    Note over Interviewer, DB: 3. Đánh giá chuyên môn (Scorecard)
    Interviewer->>WebUI: Mở form Scorecard chấm điểm (Kỹ thuật, Problem Solving, Culture)
    WebUI->>API: Gửi bảng đánh giá Scorecard
    API->>DB: INSERT INTO evaluations (score: 4.8/5.0, recommendation: 'Strong Hire')
    DB-->>API: Xác nhận lưu Scorecard
    API-->>WebUI: Cập nhật điểm trên thẻ ứng viên

    Note over Recruiter, DB: 4. Gửi đề nghị tuyển dụng (Offer Letter)
    Recruiter->>WebUI: Khởi tạo Offer (Lương: 85,000,000 VND, Thưởng, Ngày bắt đầu)
    WebUI->>API: Tạo đề nghị nhận việc
    API->>DB: INSERT INTO offers (status: 'Sent')
    API-->>Candidate: Gửi thư mời nhận việc chính thức
```

---

## 5. Sơ đồ Tuần tự: Tiếp nhận Onboarding & Chuyển đổi Nhân viên (Onboarding Sequence Diagram)

Khi ứng viên chấp nhận Offer, hệ thống tự động khởi tạo hồ sơ nhân viên chính thức:

```mermaid
sequenceDiagram
    autonumber
    actor Candidate as Ứng Viên
    actor HR as HR Officer (Tiếp Nhận)
    actor IT as Bộ Phận IT / Admin
    participant WebUI as Frontend (React / Web UI)
    participant API as FastAPI Backend
    participant DB as PostgreSQL Database

    Candidate->>WebUI: Bấm nút "Chấp thuận Thư mời" (Offer Accepted)
    WebUI->>API: Cập nhật trạng thái Offer
    API->>DB: UPDATE offers SET status = 'Accepted'
    API->>DB: UPDATE applications SET stage = 'Hired & Ready'

    Note over API, DB: Chuyển đổi dữ liệu tự động sang Nhân viên chính thức
    API->>DB: Tạo mã nhân viên tự động (ví dụ: EMP-5002)
    API->>DB: INSERT INTO employees (code, name, email, department_id, position_id, status = 'Probation')
    DB-->>API: Employee ID mới đã tạo

    Note over HR, IT: Khởi tạo Danh mục công việc Onboarding Checklist
    API->>DB: INSERT INTO onboarding_tasks (employee_id, task: 'Cấp phát máy tính & Email', assigned_to: IT)
    API->>DB: INSERT INTO onboarding_tasks (employee_id, task: 'Soạn Hợp đồng thử việc', assigned_to: HR)
    API->>DB: INSERT INTO onboarding_tasks (employee_id, task: 'Chuẩn bị thẻ ra vào & bàn làm việc', assigned_to: Admin)
    
    API->>DB: INSERT INTO contracts (employee_id, type: 'Probation', start_date, end_date, salary)
    
    IT->>WebUI: Đánh dấu đã hoàn thành chuẩn bị máy tính & email
    WebUI->>API: Cập nhật trạng thái task
    API->>DB: UPDATE onboarding_tasks SET status = 'Completed'

    HR->>WebUI: Nhân viên đến ngày làm việc đầu tiên -> Kích hoạt hồ sơ
    WebUI->>API: Xác nhận nhân viên đã nhận việc
    API->>DB: UPDATE employees SET status = 'Active'
    API-->>WebUI: Cập nhật danh bạ nhân viên thành công
```

---

## 6. Sơ đồ Tuần tự: Quản lý Biến động Nhân sự (Internal Mobility Sequence Diagram)

Quy trình thăng chức, điều chuyển phòng ban và lưu vết lịch sử:

```mermaid
sequenceDiagram
    autonumber
    actor HRDirector as Giám Đốc Nhân Sự
    actor HROfficer as Chuyên Viên Quản Lý Hồ Sơ
    participant WebUI as Frontend Workspace
    participant API as FastAPI Backend (/api/employees)
    participant DB as PostgreSQL Database

    HROfficer->>WebUI: Chọn nhân viên EMP-2048 -> Chọn "Điều Chuyển / Thăng Chức"
    WebUI->>WebUI: Mở form nhập: Phòng ban mới, Chức vụ mới, Ngày hiệu lực, Lý do
    HROfficer->>WebUI: Xác nhận gửi phê duyệt
    WebUI->>API: Gửi quyết định biến động
    API->>DB: Lưu quyết định tạm thời (status: 'Pending Approval')

    HRDirector->>WebUI: Xem xét và bấm "Ký duyệt quyết định"
    WebUI->>API: Phê duyệt quyết định biến động
    API->>DB: INSERT INTO employee_events (employee_id, event_type = 'Promotion', old_dept_id, new_dept_id, old_pos_id, new_pos_id, effective_date)
    
    Note over API, DB: Tự động cập nhật bảng chính
    API->>DB: UPDATE employees SET department_id = new_dept_id, position_id = new_pos_id
    API->>DB: INSERT INTO contracts (loại: Phụ lục điều chỉnh chức danh/lương)
    
    DB-->>API: Xác nhận hoàn tất cập nhật
    API-->>WebUI: Phản hồi thành công
    WebUI-->>HROfficer: Hiển thị dòng lịch sử sự kiện mới trên Timeline nhân viên
```

---

## 7. Sơ đồ Luồng Hoạt động Tuyển dụng Toàn trình (Recruitment Activity Flowchart)

```mermaid
flowchart TD
    Start([Bắt đầu: Nhu cầu nhân sự mới]) --> A1[Trưởng bộ phận tạo Job Requisition]
    A1 --> A2{HR Manager duyệt ngân sách?}
    A2 -- Không duyệt --> A3[Trả về điều chỉnh hoặc Hủy]
    A3 --> EndFail([Kết thúc: Hủy yêu cầu])

    A2 -- Phê duyệt --> B1[Recruiter đăng tin tuyển dụng lên Portal / LinkedIn / TopCV]
    B1 --> B2[Tiếp nhận CV của ứng viên]
    B2 --> B3[AI Parser phân tích, bóc tách kỹ năng & chấm điểm AI Match]
    
    B3 --> B4{Điểm AI Match >= 80%?}
    B4 -- Không đạt --> C1[Gửi email từ chối / Lưu vào Talent Pool]
    C1 --> EndFail
    
    B4 -- Đạt sơ tuyển --> D1[Xếp lịch phỏng vấn Chuyên môn Vòng 1]
    D1 --> D2[Hội đồng thực hiện phỏng vấn & chấm Scorecard]
    
    D2 --> D3{Đánh giá Scorecard >= 4.0?}
    D3 -- Không đạt --> C1
    D3 -- Đạt --> E1[Phỏng vấn Quản trị / Ban Giám Đốc]
    
    E1 --> E2{Ban Giám Đốc đồng ý tuyển?}
    E2 -- Không --> C1
    E2 -- Đồng ý --> F1[Khởi tạo Offer Letter & Trình ký HR Director]
    
    F1 --> F2[Gửi Thư mời nhận việc cho ứng viên]
    F2 --> F3{Ứng viên phản hồi?}
    F3 -- Từ chối Offer --> C1
    F3 -- Đồng ý nhận việc --> G1[Tạo hồ sơ nhân sự mới trong bảng employees]
    
    G1 --> G2[Tự động tạo Onboarding Checklist cho IT, HR, Hành chính]
    G2 --> G3[Chuẩn bị Hợp đồng thử việc & Trang thiết bị]
    G3 --> G4[Nhân viên chính thức làm việc ngày đầu tiên]
    G4 --> EndSuccess([Kết thúc: Tuyển dụng thành công!])
```

---

## 8. Sơ đồ Triển khai Container Docker Đề xuất (Proposed Docker Deployment Diagram)

Topology dưới đây chỉ là ý tưởng development environment; các container, bind mount và cổng mạng này chưa tồn tại trong project.

```mermaid
flowchart TB
    subgraph DOCKER_HOST["HỆ ĐIỀU HÀNH MÁY CHỦ (DOCKER ENGINE / HOST MAC)"]
        direction TB

        subgraph BRIDGE_NET["Mạng Nội Bộ Docker (qlns_default bridge)"]
            direction LR

            subgraph CONT_FRONTEND["Container: qlns_frontend"]
                FE_APP["Node 20 Alpine\nVite 5 Dev Server\nReact 18 SPA\nHot Module Replacement (HMR)"]
            end

            subgraph CONT_BACKEND["Container: qlns_backend"]
                BE_APP["Python 3.12-slim\nFastAPI Framework\nUvicorn Server\nSQLAlchemy 2.0 ORM"]
            end

            subgraph CONT_DB["Container: qlns_postgres"]
                DB_ENGINE["PostgreSQL 16 Alpine\nDatabase: qlns_db\nUser: qlns_user\nHealthcheck: pg_isready"]
            end
        end

        subgraph VOLUMES["Dữ Liệu Bền Vững (Docker Volumes & Mounts)"]
            V1[("Volume: postgres_data\n/var/lib/postgresql/data")]
            V2["Host Bind: ./database/init.sql\n-> /docker-entrypoint-initdb.d/"]
            V3["Host Bind: ./backend\n-> /app (Live Code Reload)"]
            V4["Host Bind: ./frontend\n-> /app (Live UI Reload)"]
        end
    end

    %% External Access
    USER_BROWSER(("Trình Duyệt Người Dùng\n(Google Chrome / Safari)"))
    DEV_CLIENT(("Database Client\n(DBeaver / DataGrip / TablePlus)"))

    USER_BROWSER -->|Port 5173: Xem giao diện React UI| CONT_FRONTEND
    USER_BROWSER -->|Port 8000: Gọi API & Xem Swagger UI /docs| CONT_BACKEND
    DEV_CLIENT -->|Port 5432: Truy vấn PostgreSQL trực tiếp| CONT_DB

    CONT_FRONTEND -->|Internal DNS: http://backend:8000/api| CONT_BACKEND
    CONT_BACKEND -->|Internal DNS: postgresql://db:5432/qlns_db| CONT_DB

    CONT_DB --- V1
    CONT_DB --- V2
    CONT_BACKEND --- V3
    CONT_FRONTEND --- V4
```
