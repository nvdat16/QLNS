-- =============================================================================
-- DEPRECATED — DO NOT USE AS A SOURCE OF TRUTH.
--
-- This file is a legacy reference DDL kept for historical comparison only.
-- The canonical schema contract is database/schema.sql (23 tables).
--
-- Known divergences from canonical v1:
--   * Missing: users, user_roles, audit_logs, outbox_messages,
--     contract_addenda, application_stage_events, probation_reviews,
--     offboarding_cases, offboarding_tasks.
--   * Attendance/Leave tables here (work_shifts, employee_shifts,
--     work_schedules, attendance_records, leave_types, leave_requests,
--     leave_approvals) are OUT OF SCOPE. A newer, non-canonical design for
--     that module is parked in docs/deferred/attendance_leave/; neither this
--     file nor that one may be used to generate migrations.
--   * Uses integer PKs, naive timestamps and no version/ETag column.
--
-- Do not generate EF Core migrations from this file.
-- =============================================================================

CREATE TABLE "employees" (
  "id" integer PRIMARY KEY,
  "employee_code" varchar UNIQUE,
  "first_name" varchar,
  "last_name" varchar,
  "date_of_birth" date,
  "gender" varchar,
  "email" varchar UNIQUE,
  "phone" varchar,
  "address" text,
  "hire_date" date,
  "status" varchar,
  "department_id" integer NOT NULL,
  "position_id" integer NOT NULL,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "departments" (
  "id" integer PRIMARY KEY,
  "name" varchar,
  "code" varchar UNIQUE,
  "description" text,
  "created_at" timestamp
);

CREATE TABLE "positions" (
  "id" integer PRIMARY KEY,
  "name" varchar,
  "code" varchar UNIQUE,
  "level" varchar,
  "description" text,
  "created_at" timestamp
);

CREATE TABLE "contracts" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "contract_type" varchar,
  "start_date" date,
  "end_date" date,
  "salary" decimal,
  "status" varchar,
  "document_url" varchar,
  "created_at" timestamp
);

CREATE TABLE "employee_events" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "event_type" varchar,
  "effective_date" date,
  "old_department_id" integer,
  "new_department_id" integer,
  "old_position_id" integer,
  "new_position_id" integer,
  "description" text,
  "created_by" integer,
  "created_at" timestamp
);

CREATE TABLE "onboarding_tasks" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "task_name" varchar,
  "description" text,
  "assigned_to" integer,
  "due_date" date,
  "status" varchar,
  "completed_at" timestamp,
  "created_at" timestamp
);

CREATE TABLE "employee_documents" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "document_type" varchar,
  "file_name" varchar,
  "file_url" varchar,
  "uploaded_by" integer,
  "uploaded_at" timestamp
);

CREATE TABLE "job_postings" (
  "id" integer PRIMARY KEY,
  "title" varchar,
  "department_id" integer NOT NULL,
  "description" text,
  "requirements" text,
  "location" varchar,
  "employment_type" varchar,
  "salary_min" decimal,
  "salary_max" decimal,
  "status" varchar,
  "published_at" timestamp,
  "closing_date" date,
  "created_by" integer,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "candidates" (
  "id" integer PRIMARY KEY,
  "first_name" varchar,
  "last_name" varchar,
  "email" varchar UNIQUE,
  "phone" varchar,
  "address" text,
  "linkedin_url" varchar,
  "portfolio_url" varchar,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "resumes" (
  "id" integer PRIMARY KEY,
  "candidate_id" integer NOT NULL,
  "file_name" varchar,
  "file_url" varchar,
  "parsed_text" text,
  "parsed_data" json,
  "uploaded_at" timestamp
);

CREATE TABLE "applications" (
  "id" integer PRIMARY KEY,
  "candidate_id" integer NOT NULL,
  "job_posting_id" integer NOT NULL,
  "resume_id" integer,
  "status" varchar,
  "applied_at" timestamp,
  "source" varchar,
  "notes" text,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "interviews" (
  "id" integer PRIMARY KEY,
  "application_id" integer NOT NULL,
  "interview_type" varchar,
  "scheduled_at" timestamp,
  "location" varchar,
  "meeting_url" varchar,
  "interviewer_id" integer,
  "status" varchar,
  "notes" text,
  "created_at" timestamp
);

CREATE TABLE "evaluations" (
  "id" integer PRIMARY KEY,
  "interview_id" integer NOT NULL,
  "evaluator_id" integer NOT NULL,
  "technical_score" integer,
  "communication_score" integer,
  "problem_solving_score" integer,
  "teamwork_score" integer,
  "overall_score" decimal,
  "recommendation" varchar,
  "feedback" text,
  "created_at" timestamp
);

CREATE TABLE "offers" (
  "id" integer PRIMARY KEY,
  "application_id" integer NOT NULL,
  "salary" decimal,
  "employment_type" varchar,
  "start_date" date,
  "expiration_date" date,
  "status" varchar,
  "offer_letter_url" varchar,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "work_shifts" (
  "id" integer PRIMARY KEY,
  "name" varchar,
  "code" varchar UNIQUE,
  "start_time" time,
  "end_time" time,
  "break_duration" integer,
  "status" varchar,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "employee_shifts" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "shift_id" integer NOT NULL,
  "start_date" date,
  "end_date" date,
  "status" varchar,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "work_schedules" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "shift_id" integer NOT NULL,
  "work_date" date,
  "status" varchar,
  "created_at" timestamp
);

CREATE TABLE "attendance_records" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "work_date" date,
  "check_in" timestamp,
  "check_out" timestamp,
  "late_minutes" integer,
  "early_leave_minutes" integer,
  "total_work_hours" decimal,
  "status" varchar,
  "notes" text,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "leave_types" (
  "id" integer PRIMARY KEY,
  "name" varchar,
  "code" varchar UNIQUE,
  "description" text,
  "max_days" integer,
  "status" varchar,
  "created_at" timestamp
);

CREATE TABLE "leave_requests" (
  "id" integer PRIMARY KEY,
  "employee_id" integer NOT NULL,
  "leave_type_id" integer NOT NULL,
  "start_date" date,
  "end_date" date,
  "total_days" decimal,
  "reason" text,
  "status" varchar,
  "created_at" timestamp,
  "updated_at" timestamp
);

CREATE TABLE "leave_approvals" (
  "id" integer PRIMARY KEY,
  "leave_request_id" integer NOT NULL,
  "approver_id" integer NOT NULL,
  "action" varchar,
  "comment" text,
  "approved_at" timestamp,
  "created_at" timestamp
);

ALTER TABLE "job_postings" ADD CONSTRAINT "job_department" FOREIGN KEY ("department_id") REFERENCES "departments" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "resumes" ADD CONSTRAINT "candidate_resumes" FOREIGN KEY ("candidate_id") REFERENCES "candidates" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "applications" ADD CONSTRAINT "candidate_applications" FOREIGN KEY ("candidate_id") REFERENCES "candidates" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "applications" ADD CONSTRAINT "job_applications" FOREIGN KEY ("job_posting_id") REFERENCES "job_postings" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "applications" ADD CONSTRAINT "application_resume" FOREIGN KEY ("resume_id") REFERENCES "resumes" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "interviews" ADD CONSTRAINT "application_interviews" FOREIGN KEY ("application_id") REFERENCES "applications" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "evaluations" ADD CONSTRAINT "interview_evaluations" FOREIGN KEY ("interview_id") REFERENCES "interviews" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "offers" ADD CONSTRAINT "application_offers" FOREIGN KEY ("application_id") REFERENCES "applications" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employees" ADD CONSTRAINT "employee_department" FOREIGN KEY ("department_id") REFERENCES "departments" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employees" ADD CONSTRAINT "employee_position" FOREIGN KEY ("position_id") REFERENCES "positions" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "contracts" ADD CONSTRAINT "employee_contracts" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_events" ADD CONSTRAINT "employee_events" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "onboarding_tasks" ADD CONSTRAINT "employee_onboarding" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_documents" ADD CONSTRAINT "employee_documents" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_shifts" ADD CONSTRAINT "employee_shift_employee" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_shifts" ADD CONSTRAINT "employee_shift_shift" FOREIGN KEY ("shift_id") REFERENCES "work_shifts" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "work_schedules" ADD CONSTRAINT "schedule_employee" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "work_schedules" ADD CONSTRAINT "schedule_shift" FOREIGN KEY ("shift_id") REFERENCES "work_shifts" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attendance_records" ADD CONSTRAINT "attendance_employee" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "leave_requests" ADD CONSTRAINT "leave_type" FOREIGN KEY ("leave_type_id") REFERENCES "leave_types" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "leave_requests" ADD CONSTRAINT "leave_employee" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "leave_approvals" ADD CONSTRAINT "leave_approval_request" FOREIGN KEY ("leave_request_id") REFERENCES "leave_requests" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "leave_approvals" ADD CONSTRAINT "leave_approval_approver" FOREIGN KEY ("approver_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;
