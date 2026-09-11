# 🏢 Human Resource Management System (QLNS / HRMS)

> A comprehensive **Human Resource Management System (HRMS)** designed with a top-down approach to streamline HR operations—from recruitment and employee records to attendance, payroll, training, and development.

---

## 📌 Table of Contents

- [1. Overview](#1-overview)
- [2. Functional Architecture (Top-down Mind Map)](#2-functional-architecture-top-down-mind-map)
- [3. Functional Modules](#3-functional-modules)
  - [3.1. Recruitment Management](#31-recruitment-management)
  - [3.2. Employee Records & Lifecycle Management](#32-employee-records--lifecycle-management)
  - [3.3. Attendance & Leave Management](#33-attendance--leave-management)
  - [3.4. Compensation, Benefits & Payroll Management](#34-compensation-benefits--payroll-management)
  - [3.5. Performance Management (KPI / OKR)](#35-performance-management-kpi--okr)
  - [3.6. Training & Development Management](#36-training--development-management)
  - [3.7. Reports & Analytics](#37-reports--analytics)
  - [3.8. System Administration & Access Control](#38-system-administration--access-control)
- [4. Technology Stack](#4-technology-stack)
- [5. Detailed Documentation & Diagrams](#5-detailed-documentation--diagrams)
- [6. Project Structure](#6-project-structure)
- [7. Getting Started (Docker Compose)](#7-getting-started-docker-compose)

---

## 1. Overview

**QLNS** aims to digitize end-to-end HR processes within an organization, helping teams:

- **Automate routine work:** Minimize manual errors in attendance tracking, payroll, and employee-record management.
- **Improve the employee experience:** Offer an Employee Self-Service portal for viewing payslips, submitting requests, and tracking individual goals.
- **Support decision-making:** Provide multidimensional HR analytics, including workforce changes, payroll costs, and productivity.
- **Maintain legal compliance:** Support insurance contributions, personal income tax, and employment-contract management in accordance with Vietnamese labor law.

---

## 2. Functional Architecture (Top-down Mind Map)

The system is organized into **eight core functional pillars**:

![Top-down functional decomposition diagram](topdown-approach.png)

---

## 3. Functional Modules

### 3.1. Recruitment Management

- **Job posting:** Manage job requisitions, create job descriptions, and publish openings to the internal website and external recruitment channels.
- **Application intake & data extraction:** Maintain a talent pool and automatically parse key CV details—such as name, skills, experience, and contact information—using AI/OCR.
- **Interview management:**
  - **Scheduling:** Coordinate interview times between candidates and interview panels, with Google Calendar / Outlook integration.
  - **Invitations:** Automatically send interview invitation emails with the location or online-meeting link.
  - **Evaluation:** Use predefined scorecards for candidate assessment.
  - **Offer letters:** Generate offer letters from templates and send them to candidates.

### 3.2. Employee Records & Lifecycle Management

- **Employee profile (master data):** Store complete personal, contact, employment-history, education, dependent, and bank-account information.
- **Organization chart:** Visualize the company structure, departments, teams, and direct reporting relationships.
- **Contract management:** Manage probationary, fixed-term, indefinite-term, and addendum contracts, with expiration reminders.
- **Employee lifecycle:**
  - **Onboarding:** Checklists for equipment, accounts, and orientation.
  - **Internal mobility:** Workflows for department transfers, promotions, and new appointments.
  - **Offboarding:** Resignation requests, work handover records, asset returns, outstanding-balance settlement, and contract termination.

### 3.3. Attendance & Leave Management

- **Work shifts:** Configure regular, rotating, split, and night shifts, as well as overtime (OT) rules.
- **Check-in / check-out:** Support multiple attendance methods, including fingerprint/facial-recognition devices, GPS check-ins through mobile apps, and office Wi-Fi connectivity.
- **Leave requests:** Employees can submit requests for annual leave, sick leave, maternity leave, or unpaid leave, with real-time leave-balance visibility.
- **Leave approval:** Multi-level approval workflows (Direct Manager → Department Head → HR) with automatic notifications.

### 3.4. Compensation, Benefits & Payroll Management

- **Automated payroll:** A flexible, formula-based payroll engine that aggregates attendance, allowances, bonuses, and deductions.
- **Insurance & tax:** Automatically calculate social, health, and unemployment insurance contributions, along with personal income tax under the latest progressive tax brackets.
- **Bonuses & allowances:** Manage fixed and flexible allowances, KPI bonuses, project bonuses, and holiday bonuses.
- **Salary adjustments & history:** Track salary increases and decreases, and retain salary decisions for future reference.
- **Payslip generation & delivery:** Generate detailed payslips and securely distribute them by email or through the employee portal.
- **Payment integration:** Export bank-standard payment files (e.g., Vietcombank, Techcombank, BIDV) or connect to payment gateways.

### 3.5. Performance Management (KPI / OKR)

- **Goal setting:** Define OKRs or KPIs for individuals, departments, and the entire organization.
- **Review process:** Run recurring review cycles (monthly, quarterly, or annually), supporting employee self-reviews and manager reviews.
- **360-degree feedback:** Enable peer and subordinate feedback on attitude, collaboration, and professional competency.

### 3.6. Training & Development Management

- **Course management:** Maintain a catalog of internal and external courses, learning materials, and instructors.
- **Progress tracking:** Record attendance, learning time, and lesson-completion status.
- **Post-training assessment:** Deliver quizzes and assessments to evaluate knowledge after training.
- **Certification:** Automatically generate course-completion certificates and record them in employee competency profiles.

### 3.7. Reports & Analytics

- **Visual dashboards:** Display high-level metrics such as headcount, employee turnover rate, and today's attendance/leave rate.
- **Analytics:** Analyze payroll funds, performance, employee tenure, and average recruitment cost per hire.
- **Export:** Export data in Excel, CSV, and PDF formats for audits, inspections, and management reporting.

### 3.8. System Administration & Access Control

- **Account management:** Provide identity authentication, two-factor authentication (2FA), and single sign-on (SSO) through Google Workspace / Microsoft 365.
- **Role & permission management (RBAC):** Apply granular permissions by role, such as Super Admin, HR Manager, HR Officer, Team Lead, and Employee.
- **Audit logs:** Record all system activities—including sign-ins, payroll-data edits, and data exports—to improve information security and transparency.

---

## 4. Technology Stack

- **Backend API**: [FastAPI](https://fastapi.tiangolo.com/) (Python 3.12, Uvicorn, SQLAlchemy 2.0 ORM, Pydantic v2)
- **Database**: [PostgreSQL 16](https://www.postgresql.org/) (Official Docker Alpine image)
- **Containerization**: [Docker](https://www.docker.com/) & [Docker Compose](https://docs.docker.com/compose/)
- **Frontend**: 
  - **React SPA**: [React 18](https://react.dev/) + [Vite 5](https://vitejs.dev/) with hot-reloading on port `5173`.
  - **Static Mockups**: Standalone HTML5 / Tailwind pages in `uiux/` and `frontend/*.html`.

---

## 5. Detailed Documentation & Diagrams

Tài liệu đặc tả chi tiết và bộ sơ đồ luồng hệ thống đã được chuẩn bị đầy đủ:

- 📘 **[Tài Liệu Đặc Tả Tính Năng Chi Tiết (SRS)](docs/functional_specifications.md)**:
  - Ma trận phân quyền (RBAC Matrix) cho 6 vai trò: `ROLE_ADMIN`, `ROLE_HR_MGR`, `ROLE_RECRUITER`, `ROLE_INTERVIEWER`, `ROLE_HR_OFFICER`, `ROLE_EMPLOYEE`.
  - Đặc tả 4 phân hệ cốt lõi: Tuyển dụng thông minh (ATS), Hồ sơ & Vòng đời nhân sự (Core HR), Quản lý hợp đồng lao động, Báo cáo phân tích số liệu.
  - Chi tiết từng tính năng với Tiền điều kiện, Luồng xử lý chính, Dữ liệu vào/ra và Hậu điều kiện.

- 📊 **[Hệ Thống Sơ Đồ Kiến Trúc & Luồng Nghiệp Vụ (Mermaid Diagrams)](docs/system_diagrams.md)**:
  - **Sơ đồ kiến trúc hệ thống 3 tầng** (Client React, Docker Bridge Network, FastAPI Backend, PostgreSQL 16 DB).
  - **Sơ đồ Use Case tổng quan** thể hiện tương tác giữa các tác nhân và chức năng.
  - **Sơ đồ Máy trạng thái (State Machine)** cho chu trình tuyển dụng ATS qua 6 giai đoạn.
  - **3 Sơ đồ Tuần tự (Sequence Diagrams)**: ATS Hiring & Scorecard, Tiếp nhận Onboarding chuyển đổi ứng viên thành nhân viên, Quản lý biến động nhân sự (Promotion / Transfer).
  - **Sơ đồ Luồng hoạt động (Activity Flowchart)** toàn trình tuyển dụng.
  - **Sơ đồ Triển khai Docker Container** với cấu hình mạng nội bộ và volume mount.

- 🗄️ **[Thiết Kế Cơ Sở Dữ Liệu & Sơ Đồ ERD](database/database_design.md)**: Chi tiết 14 bảng quan hệ và cấu trúc DDL PostgreSQL.

---

## 6. Project Structure

```text
QLNS/
├── backend/                  # FastAPI Application (Python 3.12, SQLAlchemy 2.0)
│   ├── app/
│   │   ├── models/           # Declarative ORM models (Employee & Recruitment)
│   │   ├── routers/          # REST API endpoints
│   │   ├── schemas/          # Pydantic validation schemas
│   │   └── main.py           # FastAPI entrypoint, CORS & healthcheck
│   ├── Dockerfile            # Python container image
│   └── requirements.txt      # Python dependencies
├── frontend/                 # React 18 + Vite 5 Application
│   ├── src/
│   │   ├── App.jsx           # Main React App with live backend integration
│   │   ├── main.jsx          # React DOM entrypoint
│   │   └── styles.css        # App styling & responsive design tokens
│   ├── Dockerfile            # Node.js 20 Alpine container image
│   ├── vite.config.js        # Vite config with React plugin
│   └── package.json          # Frontend dependencies & scripts
├── database/
│   ├── init.sql              # PostgreSQL DDL schema & seed data
│   ├── postgres_db.sql       # Original DDL schema reference
│   └── database_design.md    # Mermaid ERD diagrams & schema docs
├── docs/                     # System Specifications & Diagrams
│   ├── functional_specifications.md  # Detailed SRS & Feature Specifications
│   └── system_diagrams.md    # Full Mermaid System & Workflow Diagrams
├── uiux/                     # Standalone HTML previews & design assets
│   ├── main.html             # Employee records standalone page
│   └── recruitment.html      # Recruitment ATS standalone page
├── docker-compose.yml        # Orchestrates PostgreSQL + FastAPI + React Frontend
├── package.json              # Root npm scripts (npm run dev)
└── README.md                 # System overview and operational guide
```

---

## 7. Getting Started (Docker Compose)

### 6.1. Prerequisites
- [Docker](https://www.docker.com/) and [Docker Compose](https://docs.docker.com/compose/) installed on your machine.

### 6.2. Start the Full System

From the project root directory, run:

```bash
docker compose up -d --build
```

This command will:
1. Launch **PostgreSQL 16** container (`qlns_postgres`) on port `5432` with auto-seeded demo data.
2. Build & start **FastAPI** container (`qlns_backend`) on port `8000`.
3. Build & start **React Frontend** container (`qlns_frontend`) on port `5173`.

### 6.3. Service URLs & Interactive API Docs

- **React Web Application**: [http://localhost:5173](http://localhost:5173)
- **Swagger UI Interactive Documentation**: [http://localhost:8000/docs](http://localhost:8000/docs)
- **ReDoc Interactive Documentation**: [http://localhost:8000/redoc](http://localhost:8000/redoc)
- **Health Check Endpoint**: [http://localhost:8000/api/health](http://localhost:8000/api/health)

### 6.4. Running Frontend Locally without Docker

If you prefer running the frontend directly with Node.js on your Mac:

```bash
# From project root:
npm run dev

# Or from the frontend directory:
cd frontend
npm install
npm run dev
```

Then open [http://localhost:5173](http://localhost:5173) in your browser.
