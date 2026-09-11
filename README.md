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
- [4. Proposed Technology Stack](#4-proposed-technology-stack)
- [5. Suggested Project Structure](#5-suggested-project-structure)

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
