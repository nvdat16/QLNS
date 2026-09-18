# 🎨 UI/UX Prototypes

> This directory holds the interactive prototypes, the standalone UI/UX design and the screenshots of each functional module of the **QLNS** human resource management system.

> **Scope:** the HTML/CSS/JavaScript files here are **UI/UX prototypes**, not a frontend application. All data and interactions are simulated locally; there is no authentication, no Backend API, no persistence and no server-side business processing.

> [!NOTE]
> The prototypes were drawn before the delivery scope was settled, so this set is broader than the current scope. The delivery scope is the bold leaf functions under Recruitment and Core HR on [`topdown-approach.png`](../topdown-approach.png) — see [section 2 of the root README](../README.md). Out-of-scope screens are kept as design reference and marked ⏸️ in place: the overview dashboard, the organizational chart, and the whole of `attendance.html`.

The interface uses the shared design system in [`hrm-theme.css`](./hrm-theme.css): a fixed white sidebar, a minimal topbar, a light-grey workspace, rounded cards, a blue accent and moderate data density.

---

## 📌 Contents

- [1. Standalone Screens](#1-standalone-screens)
- [2. Visual Showcase](#2-visual-showcase)
  - [2.1. Overview dashboard (main.html)](#21-overview-dashboard-mainhtmlviewdashboard)
  - [2.2. Employee records & contracts (main.html)](#22-employee-records--contracts-mainhtml)
  - [2.3. Recruitment & onboarding ATS (recruitment.html)](#23-recruitment--onboarding-ats-recruitmenthtml)
  - [2.4. Attendance & leave management (attendance.html)](#24-attendance--leave-management-attendancehtml)
- [3. Interactions Simulated in the Prototypes](#3-interactions-simulated-in-the-prototypes)
  - [3.1. Employee records & employment contracts](#31-employee-records--employment-contracts-mainhtml)
  - [3.2. Recruitment & the onboarding process](#32-recruitment--the-onboarding-process-recruitmenthtml)
  - [3.3. Attendance, work shifts & leave management](#33-attendance-work-shifts--leave-management-attendancehtml)
- [4. UI/UX Design Principles](#4-uiux-design-principles)
- [5. How to Open the Prototypes](#5-how-to-open-the-prototypes)
- [🔗 Back to the project README](../README.md)

---

## 1. Standalone Screens

There are four self-contained HTML files that run straight in a browser with no backend:

| File | Purpose | Link |
| :--- | :--- | :--- |
| **`main.html`** | Employee records, employment contracts and the employee detail drawer. Two views are ⏸️ **out of the delivery scope** (kept as concepts): the overview dashboard, which belongs to the Reports & Analytics pillar, and the org chart, which is the Organizational Chart function. | [Open main.html](./main.html) |
| **`recruitment.html`** | The recruitment ATS module: candidate management (Kanban/table pipeline), job requisitions, interview scheduling and scorecards, and the onboarding handoff. | [Open recruitment.html](./recruitment.html) |
| **`attendance.html`** | ⏸️ **Out of the delivery scope** (kept as a concept). The attendance and leave module: timesheet and check-in/out, shift definition and scheduling, leave requests and balances, and leave approval. | [Open attendance.html](./attendance.html) |
| **`auth.html`** | The authentication gateway: sign-in, registration, password visibility toggle, form validation and navigation into the prototype dashboard. | [Open auth.html](./auth.html) |

---

## 2. Visual Showcase

### 2.1. Overview dashboard (`main.html?view=dashboard`)

⏸️ **Out of the delivery scope** (kept as a concept) — it belongs to the Reports & Analytics pillar. There is no reporting endpoint in `docs/api/openapi.yaml`.

The default page shows HR KPIs, a headcount-movement chart, recent leave requests, the status breakdown and a list of new hires. The business views are still reachable from the sidebar.

![Overview dashboard](dashboard/dashboard.png)

---

### 2.2. Employee records & contracts (`main.html`)

#### 📷 Employee directory
An enterprise-standard employee list. Personal avatars are deliberately omitted for privacy and simplicity; the data table is optimised with a search bar, department and status filters, and a button that opens the detail drawer.
![Employee directory](profile/employee_profiles.png)

#### 📷 Contract management
The contract lifecycle: contract number, contract type (probation, fixed-term, indefinite), the salary used for social insurance, the effective and expiry dates, and the activation status.
![Contract management](profile/contracts.png)

#### 📷 Organizational chart
⏸️ **Out of the delivery scope** (kept as a concept). The department hierarchy stays in scope at the data and management level; only the tree rendering and its endpoint are dropped.

A visualisation of the corporate hierarchy: board of directors → executive board → divisions and functional departments → staff.
![Organizational chart](profile/organizational.png)

---

### 2.3. Recruitment & onboarding ATS (`recruitment.html`)

#### 📷 Candidate pipeline (ATS)
A professional ATS view with two display modes: a Kanban board over the funnel stages (newly applied, screening, first interview, second interview, offer proposed, hired) and a table mode. The handoff action is shown as a compact icon.
![Candidate pipeline](recruitment/candidate.png)

#### 📷 Job requisitions
The list of open positions: posting code, job title, requesting department, target headcount, indicative salary, application deadline and campaign status.
![Job requisitions](recruitment/job_requisitions.png)

#### 📷 Interviews & scorecards
Interview appointments per round, the format (on site / Google Meet), the evaluation panel and the scorecard covering technical skills and culture fit.
![Interviews and scorecards](recruitment/interviews.png)

#### 📷 Offers & onboarding handoff
The table that manages offer outcomes and the intake of new hires. Its seven columns are laid out responsively so everything fits on screen **without horizontal scrolling**. The **"Tiếp nhận"** (hand over) action transfers the data of a hired candidate straight into a QLNS employee record.
![Onboarding handoff](recruitment/onboard_handoff.png)

---

### 2.4. Attendance & leave management (`attendance.html`)

#### 📷 Timesheet & real-time check-in / check-out
A detailed daily attendance history: check-in and check-out times, on-time/late/early-leave classification, the authentication method (main-gate fingerprint, mobile GPS, FaceID, Wi-Fi), the actual hours worked, and a drawer with the event log. Quick clock-in and Excel export actions are available.
![Timesheet and attendance](attendance/timesheet_attendance.png)

#### 📷 Work shifts & weekly schedule
Two blocks, matching the mindmap:
- **Shift definition**: the office shift (08:30–17:30, 8.0 h, 60-minute lunch break), the part-time morning shift (08:00–12:00, 4.0 h) and the server/night shift (22:00–06:00, 1.5× rate, night allowance). A modal supports adding and editing shifts.
- **Weekly shift assignment**: a Monday-to-Sunday allocation matrix for the whole core team.
![Work shifts and schedule](attendance/work_shifts.png)

#### 📷 Leave requests & leave balances
- **Personal leave balance**: cards showing annual leave (8.5/12 days), sick leave covered by social insurance (29/30 days), paid personal leave (3/3 days) and unpaid leave.
- **Leave history & request status**: a table tracking the request code, requester, leave type, dates, number of days, reason, approver and status (approved, pending, rejected). The create-request modal computes the number of days automatically.
![Leave requests](attendance/leave_requests.png)

#### 📷 Leave approval & workflow
The list of pending requests together with the requester's remaining balance. A manager can:
- **Approve** a request directly, or approve them all at once.
- **Reject** with transparent feedback.
- Filter quickly by request code, employee and leave type.
![Leave approval](attendance/leave_approval.png)

---

## 3. Interactions Simulated in the Prototypes

### 3.1. Employee records & employment contracts (`main.html`)
- **Sample data modelled on a Vietnamese company**: five core employee records across departments (Engineering Director, HR Manager, Senior Software Engineer, Chief Accountant, Recruiter) with employee code, full name, department, job title, work e-mail, phone number and employment status.
- **No-avatar standard**: personal photos are removed from the table and the drawer, as required by internal privacy rules.
- **Contract data kept separate**: the employee table shows profile data only; contract type, term and contract salary all live in the **contract management** table.
- **Responsive filter and search toolbar**: it scales with the viewport without wrapping or overlapping text on tablet or desktop.
- **Employee detail drawer**: clicking a row opens a right-hand drawer with the full personal and contact information, the employment history and the list of signed contracts.

### 3.2. Recruitment & the onboarding process (`recruitment.html`)
- **Top navigation over the four core functions**:
  1. *Pipeline*
  2. *Job requisitions*
  3. *Interviews and scorecards*
  4. *Offers and onboarding handoff*
- **No redundant counters**: navigation buttons and filter chips are kept minimal, with no count next to the label.
- **Optimised handoff table**:
  - The action button is labelled **"Tiếp nhận"** (hand over), short and true to the business term.
  - The seven columns fit a desktop viewport without a horizontal scrollbar.
- **Icon-only handoff action on the ATS page**: in the hired-candidate table the handoff button is rendered as an icon (`how_to_reg`) to save space.
- **Twelve mock candidates**: fully wired to the posting codes `TD-2024-01` through `TD-2024-05`.

### 3.3. Attendance, work shifts & leave management (`attendance.html`)
- **A 100% match with the Attendance & Leave Management mindmap**:
  1. **Work shifts**: shift definition (start/end times, mid-shift break, 1.0× / 0.5× / 1.5× rates) and the Monday-to-Sunday weekly assignment matrix.
  2. **Check-in / check-out**: a real-time attendance log, late/early-leave monitoring, the recording device (fingerprint, GPS, FaceID, Wi-Fi) and a drawer with the intraday timeline.
  3. **Leave requests**: four balance types (annual leave, insured sick leave, personal leave, unpaid leave) and a create-request modal that computes the number of days from the leave form (full day, half day, multi-day).
  4. **Leave approval**: the pending queue, instant approve/reject with a toast notification, and batch approval.
- **Summary statistics at the top**: four KPI cards (present headcount, late/early leavers, currently on leave, requests awaiting approval) updating in real time.
- **Consistent sidebar navigation**: integrated across `main.html`, `recruitment.html` and `attendance.html`.

---

## 4. UI/UX Design Principles

1. **Enterprise clean and modern**: a refined Tailwind Slate and Indigo palette that feels professional for an ERP / HRMS.
2. **Data density and usability**: dense but readable tables, sensible cell spacing and clear status badges on a conventional colour code (green: on time / approved; yellow: late / pending; orange: early leave; purple: night shift; blue: on leave; red: rejected / absent).
3. **Mobile and tablet responsive**: modern CSS Grid and Flexbox so the layout adapts to any resolution.
4. **No-avatar standard**: minimal and privacy-conscious — a bold name, the identifier and the employment details instead.

---

## 5. How to Open the Prototypes

Open the HTML files directly in any web browser:

```bash
# on macOS
open uiux/main.html
open uiux/recruitment.html
open uiux/attendance.html

# open a specific attendance tab
open "uiux/attendance.html?view=timesheet"
open "uiux/attendance.html?view=shifts"
open "uiux/attendance.html?view=leaves"
open "uiux/attendance.html?view=approvals"
```

---

[⬅️ Back to the project README](../README.md)
