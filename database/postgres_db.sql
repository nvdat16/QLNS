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

ALTER TABLE "employees" ADD CONSTRAINT "employee_department" FOREIGN KEY ("department_id") REFERENCES "departments" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employees" ADD CONSTRAINT "employee_position" FOREIGN KEY ("position_id") REFERENCES "positions" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "contracts" ADD CONSTRAINT "employee_contracts" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_events" ADD CONSTRAINT "employee_events" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "onboarding_tasks" ADD CONSTRAINT "employee_onboarding" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "employee_documents" ADD CONSTRAINT "employee_documents" FOREIGN KEY ("employee_id") REFERENCES "employees" ("id") DEFERRABLE INITIALLY IMMEDIATE;
