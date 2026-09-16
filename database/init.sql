-- =============================================================================
-- DEPRECATED — reference seed/DDL script for eyeballing the design only.
-- The canonical schema contract is database/schema.sql (36 tables).
-- This file predates identity/RBAC, audit, outbox and Attendance & Leave.
-- Do not generate EF Core migrations or seed production data from this file.
-- =============================================================================

-- ========================================================
-- QLNS Database Schema & Seed Data
-- ========================================================

-- Drop tables if they exist to allow clean recreations
DROP TABLE IF EXISTS "offers" CASCADE;
DROP TABLE IF EXISTS "evaluations" CASCADE;
DROP TABLE IF EXISTS "interviews" CASCADE;
DROP TABLE IF EXISTS "applications" CASCADE;
DROP TABLE IF EXISTS "resumes" CASCADE;
DROP TABLE IF EXISTS "candidates" CASCADE;
DROP TABLE IF EXISTS "job_postings" CASCADE;
DROP TABLE IF EXISTS "employee_documents" CASCADE;
DROP TABLE IF EXISTS "onboarding_tasks" CASCADE;
DROP TABLE IF EXISTS "employee_events" CASCADE;
DROP TABLE IF EXISTS "contracts" CASCADE;
DROP TABLE IF EXISTS "employees" CASCADE;
DROP TABLE IF EXISTS "positions" CASCADE;
DROP TABLE IF EXISTS "departments" CASCADE;

-- 1. DEPARTMENTS
CREATE TABLE "departments" (
  "id" SERIAL PRIMARY KEY,
  "name" varchar NOT NULL,
  "code" varchar UNIQUE NOT NULL,
  "description" text,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 2. POSITIONS
CREATE TABLE "positions" (
  "id" SERIAL PRIMARY KEY,
  "name" varchar NOT NULL,
  "code" varchar UNIQUE NOT NULL,
  "level" varchar,
  "description" text,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 3. EMPLOYEES
CREATE TABLE "employees" (
  "id" SERIAL PRIMARY KEY,
  "employee_code" varchar UNIQUE NOT NULL,
  "first_name" varchar NOT NULL,
  "last_name" varchar NOT NULL,
  "date_of_birth" date,
  "gender" varchar,
  "email" varchar UNIQUE NOT NULL,
  "phone" varchar,
  "avatar_url" varchar,
  "office_location" varchar,
  "work_auth" varchar,
  "emergency_contact" varchar,
  "cost_center" varchar,
  "manager_id" integer,
  "hire_date" date DEFAULT CURRENT_DATE,
  "status" varchar DEFAULT 'Active',
  "department_id" integer NOT NULL,
  "position_id" integer NOT NULL,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "updated_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 4. CONTRACTS
CREATE TABLE "contracts" (
  "id" SERIAL PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "protocol_number" varchar,
  "contract_type" varchar NOT NULL,
  "start_date" date NOT NULL,
  "end_date" date,
  "notice_period" varchar,
  "salary" decimal(15, 2),
  "status" varchar DEFAULT 'Active',
  "document_url" varchar,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 5. EMPLOYEE EVENTS
CREATE TABLE "employee_events" (
  "id" SERIAL PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "event_type" varchar NOT NULL,
  "effective_date" date NOT NULL,
  "old_department_id" integer,
  "new_department_id" integer,
  "old_position_id" integer,
  "new_position_id" integer,
  "description" text,
  "created_by" integer,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 6. ONBOARDING TASKS
CREATE TABLE "onboarding_tasks" (
  "id" SERIAL PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "task_name" varchar NOT NULL,
  "description" text,
  "assigned_to" integer,
  "due_date" date,
  "status" varchar DEFAULT 'Pending',
  "completed_at" timestamp,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 7. EMPLOYEE DOCUMENTS
CREATE TABLE "employee_documents" (
  "id" SERIAL PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "document_type" varchar NOT NULL,
  "file_name" varchar NOT NULL,
  "file_url" varchar,
  "uploaded_by" integer,
  "uploaded_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 8. JOB POSTINGS
CREATE TABLE "job_postings" (
  "id" SERIAL PRIMARY KEY,
  "job_code" varchar UNIQUE,
  "title" varchar NOT NULL,
  "department_id" integer NOT NULL,
  "description" text,
  "requirements" text,
  "location" varchar,
  "employment_type" varchar DEFAULT 'Full-time',
  "salary_min" decimal(15, 2),
  "salary_max" decimal(15, 2),
  "target_headcount" integer DEFAULT 1,
  "status" varchar DEFAULT 'Active Recruiting',
  "channels" varchar DEFAULT 'LinkedIn, TopCV, Careers',
  "published_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "closing_date" date,
  "created_by" integer,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "updated_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 9. CANDIDATES
CREATE TABLE "candidates" (
  "id" SERIAL PRIMARY KEY,
  "first_name" varchar NOT NULL,
  "last_name" varchar NOT NULL,
  "email" varchar UNIQUE NOT NULL,
  "phone" varchar,
  "avatar_url" varchar,
  "address" text,
  "linkedin_url" varchar,
  "portfolio_url" varchar,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "updated_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 10. RESUMES
CREATE TABLE "resumes" (
  "id" SERIAL PRIMARY KEY,
  "candidate_id" integer NOT NULL,
  "file_name" varchar NOT NULL,
  "file_url" varchar,
  "parsed_text" text,
  "skills" text,
  "parsed_data" jsonb,
  "uploaded_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 11. APPLICATIONS
CREATE TABLE "applications" (
  "id" SERIAL PRIMARY KEY,
  "candidate_id" integer NOT NULL,
  "job_posting_id" integer NOT NULL,
  "resume_id" integer,
  "stage" varchar NOT NULL DEFAULT 'Sourced & Applied',
  "ai_score" integer,
  "source" varchar DEFAULT 'Direct',
  "stage_metadata" jsonb,
  "applied_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "updated_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 12. INTERVIEWS
CREATE TABLE "interviews" (
  "id" SERIAL PRIMARY KEY,
  "application_id" integer NOT NULL,
  "interview_type" varchar NOT NULL,
  "scheduled_at" timestamp NOT NULL,
  "location" varchar,
  "meeting_url" varchar,
  "interviewer_id" integer,
  "status" varchar DEFAULT 'Scheduled',
  "notes" text,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 13. EVALUATIONS
CREATE TABLE "evaluations" (
  "id" SERIAL PRIMARY KEY,
  "interview_id" integer NOT NULL,
  "evaluator_id" integer NOT NULL,
  "technical_score" integer,
  "communication_score" integer,
  "problem_solving_score" integer,
  "teamwork_score" integer,
  "overall_score" decimal(3, 1),
  "recommendation" varchar,
  "feedback" text,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- 14. OFFERS
CREATE TABLE "offers" (
  "id" SERIAL PRIMARY KEY,
  "application_id" integer NOT NULL,
  "base_salary" decimal(15, 2) NOT NULL,
  "bonus_amount" decimal(15, 2),
  "employment_type" varchar DEFAULT 'Permanent Full-time',
  "start_date" date NOT NULL,
  "expiration_date" date NOT NULL,
  "status" varchar DEFAULT 'Sent',
  "offer_letter_url" varchar,
  "created_at" timestamp DEFAULT CURRENT_TIMESTAMP,
  "updated_at" timestamp DEFAULT CURRENT_TIMESTAMP
);

-- FOREIGN KEYS
ALTER TABLE "employees" ADD CONSTRAINT "fk_emp_dept" FOREIGN KEY ("department_id") REFERENCES "departments" ("id");
ALTER TABLE "employees" ADD CONSTRAINT "fk_emp_pos" FOREIGN KEY ("position_id") REFERENCES "positions" ("id");
ALTER TABLE "contracts" ADD CONSTRAINT "fk_contracts_emp" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id");
ALTER TABLE "employee_events" ADD CONSTRAINT "fk_events_emp" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id");
ALTER TABLE "onboarding_tasks" ADD CONSTRAINT "fk_tasks_emp" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id");
ALTER TABLE "employee_documents" ADD CONSTRAINT "fk_docs_emp" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id");
ALTER TABLE "job_postings" ADD CONSTRAINT "fk_jobs_dept" FOREIGN KEY ("department_id") REFERENCES "departments" ("id");
ALTER TABLE "resumes" ADD CONSTRAINT "fk_resumes_cand" FOREIGN KEY ("candidate_id") REFERENCES "candidates" ("id");
ALTER TABLE "applications" ADD CONSTRAINT "fk_apps_cand" FOREIGN KEY ("candidate_id") REFERENCES "candidates" ("id");
ALTER TABLE "applications" ADD CONSTRAINT "fk_apps_job" FOREIGN KEY ("job_posting_id") REFERENCES "job_postings" ("id");
ALTER TABLE "interviews" ADD CONSTRAINT "fk_interviews_app" FOREIGN KEY ("application_id") REFERENCES "applications" ("id");
ALTER TABLE "evaluations" ADD CONSTRAINT "fk_evaluations_int" FOREIGN KEY ("interview_id") REFERENCES "interviews" ("id");
ALTER TABLE "offers" ADD CONSTRAINT "fk_offers_app" FOREIGN KEY ("application_id") REFERENCES "applications" ("id");

-- ========================================================
-- INITIAL SEED DATA
-- ========================================================

-- Departments
INSERT INTO "departments" ("id", "name", "code", "description") VALUES
(1, 'Experience Design', 'CC-ENG-402', 'UI/UX design, design systems, UX research and digital product experience.'),
(2, 'Platform Engineering', 'CC-PLAT-108', 'Distributed infrastructure, cloud platform, databases, and microservices.'),
(3, 'People & Culture', 'CC-HR-001', 'Talent acquisition, HR business partnering, people operations, and culture.'),
(4, 'Global Enterprise Sales', 'CC-REV-901', 'Enterprise revenue operations, account executive management, and partner relations.'),
(5, 'Product & Strategy', 'CC-PROD-201', 'Product discovery, strategy roadmap, monetization, and analytics.'),
(6, 'Finance & Legal Compliance', 'CC-FIN-305', 'Corporate treasury, financial auditing, statutory labor compliance, and payroll.');

-- Positions
INSERT INTO "positions" ("id", "name", "code", "level", "description") VALUES
(1, 'Lead Design Systems Architect', 'POS-ARCH-DS', 'IC-5 Senior Staff', 'Architects enterprise multi-brand design tokens and cross-platform UI infrastructure.'),
(2, 'VP of Product & Experience Design', 'POS-EXEC-VP', 'L9 Executive VP', 'Leads product design strategy, executive reviews, and design leadership organization.'),
(3, 'UX Researcher', 'POS-UXR-01', 'IC-3 Mid Specialist', 'Executes qualitative and quantitative usability studies and design research.'),
(4, 'Design Systems Engineer', 'POS-ENG-DS', 'IC-3 Mid Specialist', 'Codes design components across React, Vue, and native mobile apps.'),
(5, 'Senior Platform Infrastructure Engineer', 'POS-ENG-PLAT', 'IC-2 Junior Assoc', 'Cloud Kubernetes clusters, observability, and container CI/CD pipelines.'),
(6, 'Senior HR Business Partner', 'POS-HRBP-04', 'IC-4 Senior HRBP', 'People ops, performance calibrations, employee relations, and labor compliance.');

-- Employees
INSERT INTO "employees" ("id", "employee_code", "first_name", "last_name", "email", "phone", "avatar_url", "office_location", "work_auth", "emergency_contact", "cost_center", "manager_id", "department_id", "position_id", "status") VALUES
(1, 'EMP-2048', 'Marcus', 'Vance', 'm.vance@acmeglobal.com', '+44 20 7946 0912', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwFBEjCj76V5bSoiuPVWmmxS4yQovKscOeaLsxTi1y4VvfLcivBJ8b6z1Y0hLgNbWq4qBxsalaEjuJLmEnKWKNbxg6FPsRVtuGLCTs40GkFcn-XJ0zrAJxvqHR2eGDUSgBRAXZRMGBQX7Gnlfu13LGPv455wqZOBOLpxYFAvwgBdgEOkHekwRWjaH3P1-lWr6eX2dtrLbAfEggqWEhUgWBp40ahh848mVGTLJYv5KJwSL_eeNGDj1sRQ', 'London HQ (Pod B)', 'UK Citizen (Settled)', 'Elena Vance (Spouse) • +44 7700 900341', 'CC-ENG-402', 2, 1, 1, 'Active'),
(2, 'EMP-1092', 'Elena', 'Rostova', 'e.rostova@acmeglobal.com', '+1 212 555 0199', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCITlndezrdQSlUWyKqKNuu7GZGNeiH4XVbkKtdnnNBIQcoLPI7R0cM2u_L6QQCZwkk9GQ9aq84lj3kkfEungP358TLg8BDDixc5LlcCKVGbll0dHxAtb19KTl7yDVpwr3jdM5UKGft757hhFpUWCYcjBV2iHo__xJORuY35g3d9kDFutCepEu0rMtvStPR7AGKucFPfXU2jDIoI5Puzau__v3evNMPNmvnAuRtST6W1onkwiqsXszz-w', 'New York HQ (Floor 9)', 'US Citizen', 'Alexander Rostov • +1 212 555 0120', 'CC-EXEC-101', NULL, 1, 2, 'Active'),
(3, 'EMP-3041', 'Sarah', 'Lin', 's.lin@acmeglobal.com', '+1 415 555 0184', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCITlndezrdQSlUWyKqKNuu7GZGNeiH4XVbkKtdnnNBIQcoLPI7R0cM2u_L6QQCZwkk9GQ9aq84lj3kkfEungP358TLg8BDDixc5LlcCKVGbll0dHxAtb19KTl7yDVpwr3jdM5UKGft757hhFpUWCYcjBV2iHo__xJORuY35g3d9kDFutCepEu0rMtvStPR7AGKucFPfXU2jDIoI5Puzau__v3evNMPNmvnAuRtST6W1onkwiqsXszz-w', 'San Francisco Tech Hub', 'US Resident', 'David Lin • +1 415 555 0199', 'CC-ENG-402', 1, 1, 3, 'Active'),
(4, 'EMP-4109', 'Tobias', 'Thorne', 't.thorne@acmeglobal.com', '+49 30 1234 5678', 'https://lh3.googleusercontent.com/aida-public/AB6AXuBHKe-EpnMvaqdpuYXWJ4APhecVvfkMlMMOkqtRvDb2YLhDTxc0pJmtjf4i4Gby0YP2RLgHQ7fgWKSXQes887yktL6zfMUez31OJryutc9HplUrL-yaB-tfIh9ilqBxwTEUgwOHem37s_6piG5x9_Om-YJR34eehL0Nn0Ce27Y-4a6HE_FyQtZ_5WNu7vDbKF867TYIKaNYur5VAfrYWSgVwFUeExFT4auuEJ9FQ441IXuKPIKzXoeikg', 'Berlin Hub (Tiergarten)', 'EU Citizen (Germany)', 'Klara Thorne • +49 30 1234 5670', 'CC-PLAT-108', 2, 2, 5, 'In Probation'),
(5, 'EMP-1822', 'Claire', 'Dupond', 'c.dupond@acmeglobal.com', '+33 1 4268 5555', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwFBEjCj76V5bSoiuPVWmmxS4yQovKscOeaLsxTi1y4VvfLcivBJ8b6z1Y0hLgNbWq4qBxsalaEjuJLmEnKWKNbxg6FPsRVtuGLCTs40GkFcn-XJ0zrAJxvqHR2eGDUSgBRAXZRMGBQX7Gnlfu13LGPv455wqZOBOLpxYFAvwgBdgEOkHekwRWjaH3P1-lWr6eX2dtrLbAfEggqWEhUgWBp40ahh848mVGTLJYv5KJwSL_eeNGDj1sRQ', 'Paris Operations Center', 'EU Citizen (France)', 'Henri Dupond • +33 1 4268 5550', 'CC-HR-001', 2, 3, 6, 'Active');

-- Contracts
INSERT INTO "contracts" ("id", "employee_id", "protocol_number", "contract_type", "start_date", "end_date", "notice_period", "salary", "status", "document_url") VALUES
(1, 1, '#UK-2021-089-IND', 'Permanent Indefinite', '2021-03-15', NULL, '3 Calendar Months', 95000.00, 'Executed', 'Master_Employment_Contract.pdf'),
(2, 2, '#US-2019-012-EXEC', 'Executive Indefinite', '2019-01-10', NULL, '6 Months', 185000.00, 'Executed', 'Executive_Agreement_Elena.pdf'),
(3, 3, '#US-2024-401-FIX', 'Fixed-Term 2-Yr', '2024-04-01', '2026-10-01', '1 Calendar Month', 68000.00, 'Expiring in 21 Days', 'Contract_SarahLin.pdf'),
(4, 4, '#DE-2024-899-PROB', 'Standard 60-Day Probation', '2026-08-25', '2026-10-24', '14 Days Notice', 52000.00, 'Review Due Oct 24', 'Probation_Tobias.pdf'),
(5, 5, '#FR-2022-771-IND', 'Permanent Indefinite (CDI)', '2022-06-01', NULL, '2 Calendar Months', 72000.00, 'Executed', 'CDI_ClaireDupond.pdf');

-- Job Postings (Requisitions)
INSERT INTO "job_postings" ("id", "job_code", "title", "department_id", "location", "employment_type", "salary_min", "salary_max", "target_headcount", "status", "channels") VALUES
(1, 'REQ-2026-08', 'Lead Systems Architect', 2, 'Hanoi / Remote', 'Full-time', 75000000.00, 95000000.00, 2, 'Active Recruiting', 'LinkedIn, TopCV, Careers'),
(2, 'REQ-2026-11', 'Senior Backend Engineer (Go)', 2, 'Ho Chi Minh / Hybrid', 'Hybrid', 45000000.00, 65000000.00, 4, 'Active Recruiting', 'LinkedIn, TopCV'),
(3, 'REQ-2026-04', 'Principal Product Designer', 1, 'Hanoi / On-site', 'On-site', 50000000.00, 70000000.00, 1, 'Active Recruiting', 'LinkedIn, TopCV'),
(4, 'REQ-2026-15', 'DevOps & Cloud Lead', 2, 'Remote', 'Full-time', 60000000.00, 85000000.00, 2, 'Active Recruiting', 'LinkedIn, TopCV, Referral'),
(5, 'REQ-2026-02', 'Senior QA Automation Engineer', 2, 'Hanoi / Hybrid', 'Hybrid', 35000000.00, 50000000.00, 2, 'Active Recruiting', 'TopCV, Careers');

-- Candidates (12 Candidates matching the 6 Kanban columns)
INSERT INTO "candidates" ("id", "first_name", "last_name", "email", "phone", "avatar_url") VALUES
(1, 'Trần Hoàng', 'Minh', 'minh.tran@email.com', '+84 912 345 678', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwFBEjCj76V5bSoiuPVWmmxS4yQovKscOeaLsxTi1y4VvfLcivBJ8b6z1Y0hLgNbWq4qBxsalaEjuJLmEnKWKNbxg6FPsRVtuGLCTs40GkFcn-XJ0zrAJxvqHR2eGDUSgBRAXZRMGBQX7Gnlfu13LGPv455wqZOBOLpxYFAvwgBdgEOkHekwRWjaH3P1-lWr6eX2dtrLbAfEggqWEhUgWBp40ahh848mVGTLJYv5KJwSL_eeNGDj1sRQ'),
(2, 'Jessica', 'Sterling', 'jessica.sterling@design.co', '+1 415 892 1029', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCITlndezrdQSlUWyKqKNuu7GZGNeiH4XVbkKtdnnNBIQcoLPI7R0cM2u_L6QQCZwkk9GQ9aq84lj3kkfEungP358TLg8BDDixc5LlcCKVGbll0dHxAtb19KTl7yDVpwr3jdM5UKGft757hhFpUWCYcjBV2iHo__xJORuY35g3d9kDFutCepEu0rMtvStPR7AGKucFPfXU2jDIoI5Puzau__v3evNMPNmvnAuRtST6W1onkwiqsXszz-w'),
(3, 'Nguyễn Văn', 'Đức', 'duc.nguyen@cloudtech.vn', '+84 988 776 554', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDa3IHO3fHVUUX220_CWZ-2wbm1w-0FLS8MgF7fhIuUrs-nvJzz0_tpDmifqOgZyrPlpyoaQPANaPr_VVyfmnUoWSjbWd_TxQzHUwkSYie_PGbgLlInzd0XRGD6hW0O-8V3Z97iQ8fZnanD3k-5OMNKYbTnApRoUrmp3QPB2hPiRv4w0_vu6PQu88t1jpRxzO9T0-JkalR4LUSEdBz_GeVESFrlEh38HV-K-JtTk8BzE2FU2FFROqutSQ'),
(4, 'Sophia', 'Martinez', 'sophia.martinez@cloudops.org', '+1 408 555 0192', 'https://lh3.googleusercontent.com/aida-public/AB6AXuBHKe-EpnMvaqdpuYXWJ4APhecVvfkMlMMOkqtRvDb2YLhDTxc0pJmtjf4i4Gby0YP2RLgHQ7fgWKSXQes887yktL6zfMUez31OJryutc9HplUrL-yaB-tfIh9ilqBxwTEUgwOHem37s_6piG5x9_Om-YJR34eehL0Nn0Ce27Y-4a6HE_FyQtZ_5WNu7vDbKF867TYIKaNYur5VAfrYWSgVwFUeExFT4auuEJ9FQ441IXuKPIKzXoeikg'),
(5, 'Lê Quốc', 'Huy', 'huy.le@gotech.io', '+84 903 881 229', 'https://lh3.googleusercontent.com/aida-public/AB6AXuB0VEJCyMITv4AsRHFexn5B9LzNrgXl5FwEFEvAfuHiOkqRBJ_jhezX_j4HtTFpS40iXrdY7jNNvu1k14pF1-h4Go86XgO-yeLtUDX-qtiluAZpKrRI1AhfUxxzyhAlboFDsAoRj43lFjEir8KsURU08-qDXr6bm2YjYkA8SdS1fVMsqxIcjn9fc2BdkOoCza8oPodm4RvI5fInzInX4hF8hQTcMbwdqgWCnqwLN4ub8knV3ZAAEXuFhg'),
(6, 'Đặng Minh', 'Khôi', 'khoi.dang@uiuxdev.vn', '+84 918 223 344', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwFBEjCj76V5bSoiuPVWmmxS4yQovKscOeaLsxTi1y4VvfLcivBJ8b6z1Y0hLgNbWq4qBxsalaEjuJLmEnKWKNbxg6FPsRVtuGLCTs40GkFcn-XJ0zrAJxvqHR2eGDUSgBRAXZRMGBQX7Gnlfu13LGPv455wqZOBOLpxYFAvwgBdgEOkHekwRWjaH3P1-lWr6eX2dtrLbAfEggqWEhUgWBp40ahh848mVGTLJYv5KJwSL_eeNGDj1sRQ'),
(7, 'Phạm Thanh', 'Thảo', 'thao.pham@creativelab.com', '+84 934 556 677', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDsYZJVXQz9kHiVce4tGh2l7pQh-vIEPPOWWPjsKLwhkiZVnbD_z3UwuVM3JBbJBJkzU2Zo-hrChD5upWaBCl1wkw6DJdyzCygfdSCu0a1dWcwfMeO5TPTyuTVTgOuqxq595APdDxMLXU6eZtUrErwNJx1ysXUh9x1F2--9iRQt9YNneZ7m7G1-WJOfo3ck2agLT2LxjEMmUorJAHc7PdYbiqu4_TJSpsLt0OGVe8Id4ojk8ig_cDHu5w'),
(8, 'Hoàng Minh', 'Tuấn', 'tuan.hoang@techleader.org', '+84 977 123 889', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDwFBEjCj76V5bSoiuPVWmmxS4yQovKscOeaLsxTi1y4VvfLcivBJ8b6z1Y0hLgNbWq4qBxsalaEjuJLmEnKWKNbxg6FPsRVtuGLCTs40GkFcn-XJ0zrAJxvqHR2eGDUSgBRAXZRMGBQX7Gnlfu13LGPv455wqZOBOLpxYFAvwgBdgEOkHekwRWjaH3P1-lWr6eX2dtrLbAfEggqWEhUgWBp40ahh848mVGTLJYv5KJwSL_eeNGDj1sRQ'),
(9, 'Đặng Tuấn', 'Anh', 'tuananh.dang@fintechcorp.vn', '+84 909 998 877', 'https://lh3.googleusercontent.com/aida-public/AB6AXuDa3IHO3fHVUUX220_CWZ-2wbm1w-0FLS8MgF7fhIuUrs-nvJzz0_tpDmifqOgZyrPlpyoaQPANaPr_VVyfmnUoWSjbWd_TxQzHUwkSYie_PGbgLlInzd0XRGD6hW0O-8V3Z97iQ8fZnanD3k-5OMNKYbTnApRoUrmp3QPB2hPiRv4w0_vu6PQu88t1jpRxzO9T0-JkalR4LUSEdBz_GeVESFrlEh38HV-K-JtTk8BzE2FU2FFROqutSQ'),
(10, 'Bùi Thùy', 'Dung', 'dung.bui@testqa.io', '+84 945 678 123', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCITlndezrdQSlUWyKqKNuu7GZGNeiH4XVbkKtdnnNBIQcoLPI7R0cM2u_L6QQCZwkk9GQ9aq84lj3kkfEungP358TLg8BDDixc5LlcCKVGbll0dHxAtb19KTl7yDVpwr3jdM5UKGft757hhFpUWCYcjBV2iHo__xJORuY35g3d9kDFutCepEu0rMtvStPR7AGKucFPfXU2jDIoI5Puzau__v3evNMPNmvnAuRtST6W1onkwiqsXszz-w'),
(11, 'Vũ Mai', 'Chi', 'chi.vu@uxstudio.design', '+84 962 110 998', 'https://lh3.googleusercontent.com/aida-public/AB6AXuCITlndezrdQSlUWyKqKNuu7GZGNeiH4XVbkKtdnnNBIQcoLPI7R0cM2u_L6QQCZwkk9GQ9aq84lj3kkfEungP358TLg8BDDixc5LlcCKVGbll0dHxAtb19KTl7yDVpwr3jdM5UKGft757hhFpUWCYcjBV2iHo__xJORuY35g3d9kDFutCepEu0rMtvStPR7AGKucFPfXU2jDIoI5Puzau__v3evNMPNmvnAuRtST6W1onkwiqsXszz-w'),
(12, 'Cao Thanh', 'Tùng', 'tung.cao@cloudnative.dev', '+84 983 456 789', 'https://lh3.googleusercontent.com/aida-public/AB6AXuB0VEJCyMITv4AsRHFexn5B9LzNrgXl5FwEFEvAfuHiOkqRBJ_jhezX_j4HtTFpS40iXrdY7jNNvu1k14pF1-h4Go86XgO-yeLtUDX-qtiluAZpKrRI1AhfUxxzyhAlboFDsAoRj43lFjEir8KsURU08-qDXr6bm2YjYkA8SdS1fVMsqxIcjn9fc2BdkOoCza8oPodm4RvI5fInzInX4hF8hQTcMbwdqgWCnqwLN4ub8knV3ZAAEXuFhg');

-- Applications in 6 Stages
INSERT INTO "applications" ("id", "candidate_id", "job_posting_id", "stage", "ai_score", "source", "stage_metadata") VALUES
-- Stage 1: Sourced & Applied
(1, 1, 2, 'Sourced & Applied', 92, 'TopCV Direct', '{"skills": ["Golang", "Postgres", "Kafka"], "applied_time": "Applied 2d ago"}'),
(2, 2, 3, 'Sourced & Applied', 86, 'LinkedIn Jobs', '{"skills": ["Figma", "Design System"], "applied_time": "Applied 1d ago"}'),
-- Stage 2: AI Screening
(3, 3, 1, 'AI Screening', 96, 'Direct Web', '{"skills": ["Kubernetes", "AWS", "Microservices"], "status_note": "CV Verified", "action_label": "Schedule R1 →"}'),
(4, 4, 4, 'AI Screening', 89, 'Referral (David Chen)', '{"skills": ["Terraform", "CI/CD", "Docker"], "status_note": "3d in stage"}'),
-- Stage 3: Tech Interview
(5, 5, 2, 'Tech Interview', 94, 'Direct Web', '{"schedule": "Today @ 15:30", "location": "Google Meet", "interviewer": "Marcus V.", "status_badge": "Ready"}'),
(6, 6, 1, 'Tech Interview', 91, 'TopCV', '{"schedule": "Tomorrow @ 14:00", "location": "Room 3B", "interviewer": "Panel: Elena & Sarah", "status_badge": "Confirmed"}'),
-- Stage 4: Executive Round
(7, 7, 3, 'Executive Round', 91, 'LinkedIn', '{"score": "4.8 / 5.0", "interviewer": "VP: Elena Rostova", "schedule": "Tomorrow 10:00"}'),
(8, 8, 2, 'Executive Round', 93, 'Referral', '{"score": "4.7 / 5.0", "interviewer": "Dir: David Chen", "schedule": "Friday 15:00"}'),
-- Stage 5: Offer Letter
(9, 9, 1, 'Offer Letter', 98, 'LinkedIn', '{"base_salary": "85,000,000 VND", "bonus": "10,000,000 VND", "expiry": "Expires in 3d", "status_label": "Awaiting Sign"}'),
(10, 10, 5, 'Offer Letter', 93, 'TopCV', '{"base_salary": "42,000,000 VND", "bonus": "1.5 Months", "expiry": "Expires in 5d", "status_label": "Under Review"}'),
-- Stage 6: Hired & Ready
(11, 11, 3, 'Hired & Ready', 95, 'LinkedIn', '{"start_date": "Oct 01, 2026", "onboarding": "View Checklist →"}'),
(12, 12, 4, 'Hired & Ready', 96, 'Direct Web', '{"start_date": "Oct 15, 2026", "equipment": "IT Ready"}');

-- Reset sequences to prevent key collision
SELECT setval('departments_id_seq', (SELECT MAX(id) FROM departments));
SELECT setval('positions_id_seq', (SELECT MAX(id) FROM positions));
SELECT setval('employees_id_seq', (SELECT MAX(id) FROM employees));
SELECT setval('contracts_id_seq', (SELECT MAX(id) FROM contracts));
SELECT setval('job_postings_id_seq', (SELECT MAX(id) FROM job_postings));
SELECT setval('candidates_id_seq', (SELECT MAX(id) FROM candidates));
SELECT setval('applications_id_seq', (SELECT MAX(id) FROM applications));
