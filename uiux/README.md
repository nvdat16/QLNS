# 🎨 UI/UX Prototypes

> This directory holds the interactive prototypes, the standalone UI/UX design and the screenshots of each functional module of the **QLNS** human resource management system.

> **Scope:** the HTML/CSS/JavaScript files here are **UI/UX prototypes**, not a frontend application. All data and interactions are simulated locally; there is no authentication, no Backend API, no persistence and no server-side business processing.

> [!NOTE]
> The prototypes were drawn before the delivery scope was settled, so this set is broader than the current scope. The delivery scope is the bold leaf functions under Recruitment and Core HR on [`topdown-approach.png`](../topdown-approach.png) — see [section 2 of the root README](../README.md). Out-of-scope screens that remain are kept as design reference and marked ⏸️ in place: the overview dashboard and the organizational chart. The attendance and leave module has been dropped from the prototypes entirely.

The interface uses the shared design system in [`hrm-theme.css`](./hrm-theme.css): a fixed white sidebar, a minimal topbar, a light-grey workspace, rounded cards, a blue accent and moderate data density.

---

## 📌 Contents

- [1. Information Architecture](#1-information-architecture)
  - [1.1. Sitemap](#11-sitemap)
  - [1.2. Screens hierarchy](#12-screens-hierarchy)
  - [1.3. Navigation model](#13-navigation-model)
- [2. Standalone Screens](#2-standalone-screens)
- [3. Visual Showcase](#3-visual-showcase)
  - [3.1. Overview dashboard (main.html)](#31-overview-dashboard-mainhtmlviewdashboard)
  - [3.2. Employee records & contracts (main.html)](#32-employee-records--contracts-mainhtml)
  - [3.3. Recruitment & onboarding ATS (recruitment.html)](#33-recruitment--onboarding-ats-recruitmenthtml)
- [4. Interactions Simulated in the Prototypes](#4-interactions-simulated-in-the-prototypes)
  - [4.1. Employee records & employment contracts](#41-employee-records--employment-contracts-mainhtml)
  - [4.2. Recruitment & the onboarding process](#42-recruitment--the-onboarding-process-recruitmenthtml)
- [5. UI/UX Design Principles](#5-uiux-design-principles)
- [6. How to Open the Prototypes](#6-how-to-open-the-prototypes)
- [🔗 Back to the project README](../README.md)

---

## 1. Information Architecture

This section maps how the delivery-scope functions (the bold leaf functions under Recruitment and Core HR on
[`topdown-approach.png`](../topdown-approach.png)) are organised into screens and navigated, independently of any one
prototype file. It is the missing link between the [28 backend features](../src/backend/README.md#modular-3-layer-backend),
[28 user stories](../docs/user_stories.md) and the standalone HTML prototypes below — read it before the prototypes
themselves.

### 1.1. Sitemap

```text
QLNS
├── Auth                                          (auth.html)
│   ├── Sign in
│   └── Register                                  ⏸️ out of scope — ADM-01 has no self-service sign-up
│
├── Overview dashboard                            (main.html?view=dashboard)  ⏸️ out of scope (Reports & Analytics)
│
├── Core HR
│   ├── Employee Records                          (main.html?view=profiles)   — EMP-01
│   │   └── Employee detail drawer                — EMP-01 (profile, contracts, onboarding tasks)
│   │       └── + Add employee (modal)             — EMP-01 (create)
│   ├── Organization Chart                        (main.html?view=org)        ⏸️ out of scope (tree rendering only; the
│   │                                                                            underlying department/position data is
│   │                                                                            in scope, at Employee Records level)
│   └── Contracts                                 (main.html?view=contracts)  — CON-01, CON-02, CON-03
│       └── Contract detail / addendum            — CON-03
│
├── Recruitment                                   (recruitment.html)
│   ├── Pipeline (Kanban / table)                 — REC-02, REC-03
│   │   └── Candidate detail
│   │       └── + Add candidate (modal)            — REC-02 (intake)
│   ├── Job Requisitions                          — REC-01
│   │   └── + Post job (modal)                     — REC-01 (create/publish)
│   ├── Interviews & Scorecards                   — REC-04, REC-05
│   │   └── Scorecard (modal)                      — REC-05 (evaluation)
│   └── Offers & Onboarding Handoff               — REC-06
│       └── Offer detail (modal)                   — REC-06 (approve/send/respond)
│           └── "Onboard" action → Employee Records  — REC-06.2 handoff into EMP-03 (creates the employee + onboarding
│                                                       tasks; not itself a screen)
│
└── System Administration                         ⏸️ prototype not drawn — served by the API only (ADM-01, ADM-02);
                                                      account/role management has no dedicated UI in this delivery
```

Not modelled as their own screens because they are modals/drawers reached from a parent screen, not independent
navigation destinations: **Add employee**, **Add candidate**, **Post job**, **Scorecard**, **Offer detail**. Probation
review (EMP-06) and Offboarding & handover (EMP-07) have no prototype screen at all — see the note under
[§1.3](#13-navigation-model).

### 1.2. Screens hierarchy

Depth reflects how a user reaches a screen, not the file that renders it — `main.html` alone renders four
depth-1 destinations by switching an internal view.

| Depth | Screen | Reached from | Delivery status |
| :---: | :--- | :--- | :--- |
| 0 | Sign in | entry point | ✅ in scope |
| 1 | Overview dashboard | sidebar → *Overview* | ⏸️ out of scope |
| 1 | Employee Records (list) | sidebar → *Employee Records* | ✅ in scope (EMP-01) |
| 2 | Employee detail drawer | row click in Employee Records | ✅ in scope (EMP-01) |
| 3 | Add employee (modal) | "+ Add" in Employee Records | ✅ in scope (EMP-01) |
| 1 | Organization Chart | sidebar → *Organization Chart* | ⏸️ out of scope (tree view only) |
| 1 | Contracts (list) | sidebar → *Contracts* | ✅ in scope (CON-01/02) |
| 2 | Contract detail / addendum | row click in Contracts | ✅ in scope (CON-03) |
| 1 | Recruitment Pipeline | top nav in `recruitment.html` → *Pipeline* | ✅ in scope (REC-02/03) |
| 2 | Candidate detail | card/row click in Pipeline | ✅ in scope (REC-03) |
| 3 | Add candidate (modal) | "+ Add" in Pipeline | ✅ in scope (REC-02) |
| 1 | Job Requisitions | top nav → *Job Requisitions* | ✅ in scope (REC-01) |
| 2 | Post job (modal) | "+ Post" in Job Requisitions | ✅ in scope (REC-01) |
| 1 | Interviews & Scorecards | top nav → *Interviews* | ✅ in scope (REC-04) |
| 2 | Scorecard (modal) | row action in Interviews | ✅ in scope (REC-05) |
| 1 | Offers & Onboarding Handoff | top nav → *Offers* | ✅ in scope (REC-06) |
| 2 | Offer detail (modal) | row click in Offers | ✅ in scope (REC-06) |
| — | Probation Review, Offboarding & Handover | no prototype screen | ✅ in scope in the API (EMP-06, EMP-07); UI not drawn |

### 1.3. Navigation model

- **Two independent shells, no shared chrome.** `main.html` (Core HR: dashboard, employee records, org chart,
  contracts) and `recruitment.html` (the ATS) each own a full-height sidebar/topbar and are reached from each other
  only through one static link (`main.html`'s sidebar → *Recruitment*) and one cross-flow action (the offer's
  **"Onboard"** button, which is a data handoff, not a navigation link back into `main.html`). There is no single
  top-level shell that hosts every module.
- **`main.html` is a single-page shell with four views**, not four HTML files: `switchMainView('dashboard' |
  'profiles' | 'org' | 'contracts')` toggles a `<div>` and the matching sidebar item, addressable directly with
  `?view=`.
- **`recruitment.html` is a single-page shell with four tab views**: `view-pipeline`, `view-jobs`,
  `view-interviews`, `view-offers`, switched by the top nav, each with its own "+" action that opens a modal
  (`addCandidateModal`, `postJobModal`, `scorecardModal`, `offerModal`).
- **`auth.html` is a third, unconnected shell** — it does not link forward into `main.html` on sign-in; wiring it up
  is frontend work, not a prototype gap.
- **Gaps against the backend's 28 delivered features:** Probation Review (EMP-06) and Offboarding & Handover
  (EMP-07) are implemented end-to-end in the API (see [src/backend/README.md](../src/backend/README.md)) but have no
  screen here — evaluate them against `docs/api/openapi.yaml` directly, not against these prototypes. System
  Administration (ADM-01/02: accounts, roles) is API-only in this delivery.

---

## 2. Standalone Screens

There are three self-contained HTML files that run straight in a browser with no backend:

| File | Purpose | Link |
| :--- | :--- | :--- |
| **`main.html`** | Employee records, employment contracts and the employee detail drawer. Two views are ⏸️ **out of the delivery scope** (kept as concepts): the overview dashboard, which belongs to the Reports & Analytics pillar, and the org chart, which is the Organizational Chart function. | [Open main.html](./main.html) |
| **`recruitment.html`** | The recruitment ATS module: candidate management (Kanban/table pipeline), job requisitions, interview scheduling and scorecards, and the onboarding handoff. | [Open recruitment.html](./recruitment.html) |
| **`auth.html`** | The authentication gateway: sign-in, registration, password visibility toggle, form validation and navigation into the prototype dashboard. | [Open auth.html](./auth.html) |

---

## 3. Visual Showcase

> [!NOTE]
> The screenshots below were captured before the interface was relabelled to English, so they still show the earlier Vietnamese labels. The dashboard screenshot also still shows the "Recent Leave Requests" card, which was removed together with the attendance module. Re-capture them when convenient.

### 3.1. Overview dashboard (`main.html?view=dashboard`)

⏸️ **Out of the delivery scope** (kept as a concept) — it belongs to the Reports & Analytics pillar. There is no reporting endpoint in `docs/api/openapi.yaml`.

The default page shows HR KPIs, a headcount-movement chart, recent leave requests, the status breakdown and a list of new hires. The business views are still reachable from the sidebar.

![Overview dashboard](dashboard/dashboard.png)

---

### 3.2. Employee records & contracts (`main.html`)

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

### 3.3. Recruitment & onboarding ATS (`recruitment.html`)

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
The table that manages offer outcomes and the intake of new hires. Its seven columns are laid out responsively so everything fits on screen **without horizontal scrolling**. The **"Onboard"** action transfers the data of a hired candidate straight into a QLNS employee record.
![Onboarding handoff](recruitment/onboard_handoff.png)

---

## 4. Interactions Simulated in the Prototypes

### 4.1. Employee records & employment contracts (`main.html`)
- **Sample data modelled on a Vietnamese company**: five core employee records across departments (Engineering Director, HR Manager, Senior Software Engineer, Chief Accountant, Recruiter) with employee code, full name, department, job title, work e-mail, phone number and employment status.
- **No-avatar standard**: personal photos are removed from the table and the drawer, as required by internal privacy rules.
- **Contract data kept separate**: the employee table shows profile data only; contract type, term and contract salary all live in the **contract management** table.
- **Responsive filter and search toolbar**: it scales with the viewport without wrapping or overlapping text on tablet or desktop.
- **Employee detail drawer**: clicking a row opens a right-hand drawer with the full personal and contact information, the employment history and the list of signed contracts.

### 4.2. Recruitment & the onboarding process (`recruitment.html`)
- **Top navigation over the four core functions**:
  1. *Pipeline*
  2. *Job requisitions*
  3. *Interviews and scorecards*
  4. *Offers and onboarding handoff*
- **No redundant counters**: navigation buttons and filter chips are kept minimal, with no count next to the label.
- **Optimised handoff table**:
  - The action button is labelled **"Onboard"**, short and true to the business term.
  - The seven columns fit a desktop viewport without a horizontal scrollbar.
- **Icon-only handoff action on the ATS page**: in the hired-candidate table the handoff button is rendered as an icon (`how_to_reg`) to save space.
- **Twelve mock candidates**: fully wired to the posting codes `TD-2024-01` through `TD-2024-05`.

- **Consistent sidebar navigation**: integrated across `main.html` and `recruitment.html`.

---

## 5. UI/UX Design Principles

1. **Enterprise clean and modern**: a refined Tailwind Slate and Indigo palette that feels professional for an ERP / HRMS.
2. **Data density and usability**: dense but readable tables, sensible cell spacing and clear status badges on a conventional colour code (green: active / approved; yellow: pending / expiring soon; purple: probation; blue: informational; red: rejected / expired).
3. **Mobile and tablet responsive**: modern CSS Grid and Flexbox so the layout adapts to any resolution.
4. **No-avatar standard**: minimal and privacy-conscious — a bold name, the identifier and the employment details instead.
5. **English interface, Vietnamese data**: every interface string is English — navigation, headings, buttons, tabs, table headers, KPI labels, form labels, placeholders, status badges, toasts. Only the sample records stay Vietnamese: person names, department names, job titles, free-text reasons and emails, together with Vietnamese number and date formatting (`35.000.000đ`, `dd/mm/yyyy`). Status and enum values count as interface vocabulary, not data, so they are translated (`Active`, `Probation`, `Expiring Soon`, `Awaiting Signature`).

---

## 6. How to Open the Prototypes

Open the HTML files directly in any web browser:

```bash
# on macOS
open uiux/auth.html
open uiux/main.html
open uiux/recruitment.html

# open a specific main.html view
open "uiux/main.html?view=dashboard"
open "uiux/main.html?view=profiles"
open "uiux/main.html?view=org"
open "uiux/main.html?view=contracts"
```

---

[⬅️ Back to the project README](../README.md)
