# Human Resource Management System (QLNS / HRMS)

> **Status:** Design/Prototype. Repository hiện chỉ có UI/UX prototype và database design; frontend application, Backend API và deployment runtime chưa được triển khai.

**Primary architecture:** [05 — Architecture (arc42 + C4)](docs/architecture.md) · **Requirements:** [Functional specifications](docs/functional_specifications.md) · **Database:** [Database design](database/database_design.md)

## Table of Contents

- [1. System Overview](#1-system-overview)
- [2. Functional Architecture (Top-Down Mind Map)](#2-functional-architecture-top-down-mind-map)
- [3. Roles & Use Case Diagram](#3-roles--use-case-diagram)
- [4. Visual Showcase](#4-visual-showcase)
  - [4.1. Workforce Dashboard](#41-workforce-dashboard)
  - [4.2. Employee Records and Employment Contracts (Core HR)](#42-employee-records-and-employment-contracts-core-hr)
  - [4.3. Smart Recruitment and Onboarding (ATS)](#43-smart-recruitment-and-onboarding-ats)
  - [4.4. Attendance and Leave Management](#44-attendance-and-leave-management)
  - [4.5. Database Design Diagram (DBML)](#45-database-design-diagram-dbml)

---

## 1. System Overview

Khi được triển khai, **QLNS** hướng tới số hóa các nghiệp vụ nhân sự sau:

- **Automated recurring tasks:** Reduces manual errors in attendance tracking, payroll, employee record management, and employment-contract expiry monitoring.
- **Optimized recruitment experience (ATS):** Shortens time-to-hire with a visual Kanban pipeline, interview scheduling, and standardized scorecards.
- **Seamless onboarding handoff:** Converts successful candidates into official employee records with one click, without re-entering data.
- **End-to-end attendance and leave management:** Supports multiple attendance methods (fingerprint, GPS, Face ID, and Wi-Fi), weekly work schedules, and intelligent leave-approval workflows.
- **Decision support and reporting:** Cung cấp dữ liệu phục vụ phân tích biến động nhân sự, cơ cấu phòng ban và chi phí nhân sự.
- **Legal compliance:** Standardizes employment-contract, social-insurance, and personal-income-tax processes in accordance with Vietnamese labor law.

---

## 2. Functional Architecture (Top-Down Mind Map)

The system follows a top-down decomposition approach with **eight functional pillars**:

![Top-down functional decomposition diagram](topdown-approach.png)

1. **Recruitment Management (Smart ATS Recruitment)**
2. **Employee Records and Lifecycle**
3. **Attendance and Leave Management**
4. **Compensation, Benefits, and Payroll**
5. **Performance Management (KPI / OKR)**
6. **Training and Development**
7. **Reports and Analytics**
8. **System Administration and Access Control (RBAC)**

---

## 3. Roles & Use Case Diagram

Detailed documentation: [docs/system_diagrams.md](docs/system_diagrams.md#2-sơ-đồ-use-case-tổng-quan-overall-use-case-diagram)

### Actors

- **Super Admin:** Manages system accounts, role-based access control, configuration, audit trails, and organization-wide reporting.
- **HR Director / Manager:** Approves hiring requests and offers, manages employee movements and contracts, and monitors workforce analytics.
- **Recruiter (Talent Acquisition):** Publishes vacancies, screens CVs, manages the ATS pipeline, schedules interviews, and prepares offers.
- **Hiring Manager / Interviewer:** Creates hiring requests, participates in interviews, and completes candidate scorecards.
- **HR Officer (C&B / Records):** Maintains employee records, employment contracts, and onboarding checklists.
- **Employee / Candidate:** Updates permitted profile information, views organization and contract information, or submits an application.

### Use Case Diagram (Mermaid)

```mermaid
flowchart LR
    Admin([Super Admin])
    HRMgr([HR Director / Manager])
    Recruiter([Recruiter])
    Interviewer([Hiring Manager / Interviewer])
    HROfficer([HR Officer])
    User([Employee / Candidate])

    subgraph HRMS["QLNS / HRMS"]
        Jobs[Create & publish job requisitions]
        ATS[Screen CVs & manage ATS pipeline]
        Interviews[Schedule interviews & submit scorecards]
        Offers[Approve offers & onboarding]
        Records[Manage employee records & contracts]
        Attendance[Manage attendance & leave]
        Reports[View workforce reports]
        Access[Manage accounts & RBAC]
    end

    Recruiter --> Jobs
    Recruiter --> ATS
    Recruiter --> Interviews
    Interviewer --> Jobs
    Interviewer --> Interviews
    HRMgr --> Offers
    HRMgr --> Records
    HRMgr --> Reports
    HROfficer --> Records
    HROfficer --> Attendance
    User --> ATS
    User --> Records
    Admin --> Access
    Admin --> Reports
```

---

## 4. Visual Showcase

Các module dưới đây có UI/UX prototype với dữ liệu minh họa; chưa kết nối frontend/backend hoặc database thực.

### 4.1. Workforce Dashboard

#### 📷 HR Overview (`uiux/main.html?view=dashboard`)

> Cung cấp cái nhìn tổng quan về KPI nhân sự, biến động quân số, đơn nghỉ gần đây, cơ cấu trạng thái và danh sách nhân sự mới.

![Workforce Dashboard](uiux/dashboard/dashboard.png)

---

### 4.2. Employee Records and Employment Contracts (Core HR)

#### 📷 Employee Profile List (`uiux/main.html`)

> Enterprise-ready design with no-avatar privacy standards, plus an optimized data table with responsive search and filters.

![Employee Profile List](uiux/profile/employee_profiles.png)

#### 📷 Employment Contract Management (`uiux/main.html`)

> Manage contract duration, contract types (probationary, fixed-term, and indefinite-term), insurance salary, and contract validity.

![Employment Contract Management](uiux/profile/contracts.png)

#### 📷 Organizational Structure Tree (`uiux/main.html`)

> Visualizes the company's hierarchy, from the board of directors to departments and their employees.

![Organizational Structure Tree](uiux/profile/organizational.png)

---

### 4.3. Smart Recruitment and Onboarding (ATS)

#### Candidate Recruitment Pipeline

> ATS recruitment workflow with a six-stage Kanban board and candidate list, including an intuitive icon-based onboarding action.

![ATS Recruitment Pipeline](uiux/recruitment/candidate.png)

#### Job Requisition Management

> Manage open positions, hiring targets, requesting departments, and application deadlines.

![Job Requisition Management](uiux/recruitment/job_requisitions.png)

#### Interview Schedule and Scorecards

> Coordinate multi-round interview schedules, interview formats, and candidate competency scorecards.

![Interview Schedule and Scorecards](uiux/recruitment/interviews.png)

#### New-Hire Offer and Onboarding

> A seven-column onboarding table fully visible on desktop **without horizontal scrolling**, with an “Onboard” action that quickly transfers a candidate into QLNS.

![New-Hire Onboarding](uiux/recruitment/onboard_handoff.png)

---

### 4.4. Attendance and Leave Management

#### Timesheet and Real-Time Attendance

> Monitor detailed attendance logs: check-in/check-out times, on-time/late/early-leave status, authentication methods (fingerprint, GPS, Face ID, and Wi-Fi), actual work hours, and an event-detail drawer.

![Timesheet and Real-Time Attendance](uiux/attendance/timesheet_attendance.png)

#### Work Shifts and Weekly Scheduling

> Manage shift definitions (standard office, morning, and night shifts with coefficients) and a Monday-to-Sunday weekly scheduling matrix.

![Work Shifts and Weekly Scheduling](uiux/attendance/work_shifts.png)

#### Leave Requests and Personal Leave Balance

> Track leave-request history and personal leave balances (annual leave, social-insurance sick leave, personal leave, and unpaid leave), with a request form that automatically calculates the number of days.

![Leave Requests and Personal Leave Balance](uiux/attendance/leave_requests.png)

#### Leave Approval

> Process pending leave requests, with support for individual approval, batch approval, and rejection with feedback.

![Leave Approval](uiux/attendance/leave_approval.png)

---

### 4.5. Database Design Diagram (DBML)

#### 14-Table Entity and Foreign-Key Relationship Diagram

> A normalized relational data model connecting recruitment candidates, employment contracts, and official employee profiles end to end.

![Database Diagram](database/dbml.png)
