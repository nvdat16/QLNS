-- QLNS canonical PostgreSQL schema — baseline v1
-- Scope: Core HR (incl. Contracts), Recruitment and Attendance & Leave — 36 tables.
-- Status: Proposed. This file is the canonical schema contract until EF Core
-- migrations are generated and accepted. Times are stored in UTC (timestamptz).

BEGIN;

CREATE TABLE departments (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(50) NOT NULL UNIQUE,
    name varchar(255) NOT NULL,
    parent_department_id bigint REFERENCES departments(id),
    cost_center varchar(100),
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_departments_not_self_parent CHECK (parent_department_id IS NULL OR parent_department_id <> id)
);

CREATE TABLE positions (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(50) NOT NULL UNIQUE,
    name varchar(255) NOT NULL,
    level varchar(50),
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1
);

CREATE TABLE users (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    external_subject varchar(255) NOT NULL UNIQUE,
    email varchar(320) NOT NULL UNIQUE,
    display_name varchar(255) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_users_status CHECK (status IN ('active', 'disabled'))
);

CREATE TABLE user_roles (
    user_id bigint NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_code varchar(80) NOT NULL,
    data_scope_type varchar(30) NOT NULL DEFAULT 'organization',
    data_scope_id bigint NOT NULL DEFAULT 0,
    granted_at timestamptz NOT NULL DEFAULT now(),
    granted_by bigint REFERENCES users(id),
    PRIMARY KEY (user_id, role_code, data_scope_type, data_scope_id),
    CONSTRAINT ck_user_roles_scope CHECK (
        (data_scope_type = 'department' AND data_scope_id > 0) OR
        (data_scope_type IN ('self', 'organization') AND data_scope_id = 0)
    )
);

CREATE TABLE job_postings (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    job_code varchar(50) NOT NULL UNIQUE,
    title varchar(255) NOT NULL,
    department_id bigint NOT NULL REFERENCES departments(id),
    position_id bigint REFERENCES positions(id),
    description text,
    requirements text,
    location varchar(255),
    employment_type varchar(30) NOT NULL,
    salary_min numeric(15,2),
    salary_max numeric(15,2),
    target_headcount integer NOT NULL,
    status varchar(40) NOT NULL DEFAULT 'draft',
    closing_date date,
    published_at timestamptz,
    created_by bigint NOT NULL REFERENCES users(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_job_headcount CHECK (target_headcount > 0),
    CONSTRAINT ck_job_salary_range CHECK (salary_min IS NULL OR salary_max IS NULL OR salary_min <= salary_max),
    CONSTRAINT ck_job_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'active_recruiting', 'closed', 'cancelled'))
);

CREATE TABLE candidates (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    first_name varchar(100) NOT NULL,
    last_name varchar(100) NOT NULL,
    email varchar(320) NOT NULL,
    normalized_email varchar(320) NOT NULL,
    phone varchar(30),
    normalized_phone varchar(30),
    linkedin_url varchar(2048),
    portfolio_url varchar(2048),
    privacy_notice_version varchar(50) NOT NULL,
    consented_at timestamptz NOT NULL,
    retention_until date,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX ux_candidates_normalized_email ON candidates(normalized_email);
CREATE INDEX ix_candidates_normalized_phone ON candidates(normalized_phone) WHERE normalized_phone IS NOT NULL;

CREATE TABLE resumes (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    candidate_id bigint NOT NULL REFERENCES candidates(id),
    object_key varchar(1024) NOT NULL UNIQUE,
    original_file_name varchar(255) NOT NULL,
    content_type varchar(100) NOT NULL,
    size_bytes bigint NOT NULL,
    malware_scan_status varchar(30) NOT NULL,
    parser_status varchar(30) NOT NULL DEFAULT 'pending',
    parsed_data jsonb,
    parser_version varchar(100),
    uploaded_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_resume_size CHECK (size_bytes > 0 AND size_bytes <= 10485760),
    CONSTRAINT ck_resume_scan CHECK (malware_scan_status IN ('pending', 'clean', 'infected', 'failed')),
    CONSTRAINT ck_resume_parser CHECK (parser_status IN ('pending', 'processing', 'completed', 'failed', 'confirmed'))
);

CREATE TABLE applications (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    candidate_id bigint NOT NULL REFERENCES candidates(id),
    job_posting_id bigint NOT NULL REFERENCES job_postings(id),
    resume_id bigint REFERENCES resumes(id),
    stage varchar(40) NOT NULL DEFAULT 'sourced_applied',
    ai_score numeric(5,2),
    source varchar(80) NOT NULL DEFAULT 'direct',
    applied_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_applications_candidate_job UNIQUE (candidate_id, job_posting_id),
    CONSTRAINT ck_applications_stage CHECK (stage IN ('sourced_applied', 'ai_screening', 'tech_interview', 'executive_round', 'offer_letter', 'hired_ready', 'rejected', 'withdrawn')),
    CONSTRAINT ck_applications_ai_score CHECK (ai_score IS NULL OR (ai_score >= 0 AND ai_score <= 100))
);

CREATE INDEX ix_applications_pipeline ON applications(job_posting_id, stage, applied_at DESC);

CREATE TABLE interviews (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    application_id bigint NOT NULL REFERENCES applications(id),
    interview_type varchar(50) NOT NULL,
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    timezone varchar(100) NOT NULL,
    interviewer_user_id bigint NOT NULL REFERENCES users(id),
    location varchar(255),
    meeting_url varchar(2048),
    status varchar(30) NOT NULL DEFAULT 'scheduled',
    cancellation_reason text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_interview_time CHECK (ends_at > starts_at),
    CONSTRAINT ck_interview_status CHECK (status IN ('scheduled', 'completed', 'cancelled', 'no_show'))
);

CREATE INDEX ix_interviews_application_status ON interviews(application_id, status);
CREATE INDEX ix_interviews_interviewer_time ON interviews(interviewer_user_id, starts_at, ends_at);

CREATE TABLE evaluations (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    interview_id bigint NOT NULL REFERENCES interviews(id),
    evaluator_user_id bigint NOT NULL REFERENCES users(id),
    technical_score numeric(2,1) NOT NULL,
    communication_score numeric(2,1) NOT NULL,
    problem_solving_score numeric(2,1) NOT NULL,
    teamwork_score numeric(2,1) NOT NULL,
    overall_score numeric(2,1) NOT NULL,
    recommendation varchar(30) NOT NULL,
    feedback text NOT NULL,
    submitted_at timestamptz NOT NULL DEFAULT now(),
    unlocked_at timestamptz,
    unlocked_by bigint REFERENCES users(id),
    unlock_reason text,
    version integer NOT NULL DEFAULT 1,
    CONSTRAINT ux_evaluation_interviewer_version UNIQUE (interview_id, evaluator_user_id, version),
    CONSTRAINT ck_evaluation_scores CHECK (
        technical_score BETWEEN 0 AND 5 AND mod(technical_score, 0.5) = 0 AND
        communication_score BETWEEN 0 AND 5 AND mod(communication_score, 0.5) = 0 AND
        problem_solving_score BETWEEN 0 AND 5 AND mod(problem_solving_score, 0.5) = 0 AND
        teamwork_score BETWEEN 0 AND 5 AND mod(teamwork_score, 0.5) = 0 AND
        overall_score BETWEEN 0 AND 5
    ),
    CONSTRAINT ck_evaluation_recommendation CHECK (recommendation IN ('strong_hire', 'hire', 'hold', 'no_hire', 'strong_no_hire'))
);

CREATE TABLE offers (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    application_id bigint NOT NULL REFERENCES applications(id),
    base_salary numeric(15,2) NOT NULL,
    bonus_amount numeric(15,2),
    allowance_amount numeric(15,2),
    employment_type varchar(50) NOT NULL,
    start_date date NOT NULL,
    expiration_date date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'draft',
    template_version varchar(50) NOT NULL,
    document_object_key varchar(1024),
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    sent_at timestamptz,
    responded_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_offer_dates CHECK (expiration_date <= start_date),
    CONSTRAINT ck_offer_status CHECK (status IN ('draft', 'approved', 'sent', 'accepted', 'declined', 'expired', 'cancelled'))
);

CREATE UNIQUE INDEX ux_offers_one_open_per_application ON offers(application_id)
WHERE status IN ('draft', 'approved', 'sent', 'accepted');

CREATE TABLE employees (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_code varchar(50) NOT NULL UNIQUE,
    source_application_id bigint UNIQUE REFERENCES applications(id),
    user_id bigint UNIQUE REFERENCES users(id),
    first_name varchar(100) NOT NULL,
    last_name varchar(100) NOT NULL,
    work_email varchar(320) NOT NULL UNIQUE,
    personal_email varchar(320),
    phone varchar(30),
    date_of_birth date,
    gender varchar(30),
    office_location varchar(255),
    manager_id bigint REFERENCES employees(id),
    department_id bigint NOT NULL REFERENCES departments(id),
    position_id bigint NOT NULL REFERENCES positions(id),
    hire_date date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'probation',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_employee_status CHECK (status IN ('probation', 'active', 'suspended', 'terminated')),
    CONSTRAINT ck_employee_not_self_manager CHECK (manager_id IS NULL OR manager_id <> id)
);

CREATE INDEX ix_employees_directory ON employees(department_id, status, last_name, first_name);

CREATE TABLE contracts (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    contract_number varchar(100) NOT NULL UNIQUE,
    contract_type varchar(40) NOT NULL,
    start_date date NOT NULL,
    end_date date,
    salary numeric(15,2) NOT NULL,
    notice_period_days integer,
    status varchar(30) NOT NULL DEFAULT 'draft',
    is_primary boolean NOT NULL DEFAULT true,
    document_object_key varchar(1024),
    signed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_contract_dates CHECK (end_date IS NULL OR end_date > start_date),
    CONSTRAINT ck_contract_status CHECK (status IN ('draft', 'approved', 'executed', 'active', 'expired', 'terminated', 'cancelled'))
);

CREATE UNIQUE INDEX ux_contracts_primary_active ON contracts(employee_id)
WHERE is_primary AND status IN ('executed', 'active');

CREATE TABLE contract_addenda (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    contract_id bigint NOT NULL REFERENCES contracts(id),
    addendum_number varchar(100) NOT NULL UNIQUE,
    version integer NOT NULL DEFAULT 1,
    status varchar(30) NOT NULL DEFAULT 'draft',
    effective_date date NOT NULL,
    before_terms jsonb NOT NULL,
    after_terms jsonb NOT NULL,
    reason text NOT NULL,
    document_object_key varchar(1024),
    created_by bigint NOT NULL REFERENCES users(id),
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    signed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ux_contract_addendum_version UNIQUE (contract_id, version),
    CONSTRAINT ck_contract_addendum_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'effective', 'superseded', 'cancelled'))
);

CREATE TABLE onboarding_tasks (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    template_key varchar(100) NOT NULL,
    task_name varchar(255) NOT NULL,
    description text,
    assigned_to_user_id bigint REFERENCES users(id),
    due_at timestamptz,
    status varchar(30) NOT NULL DEFAULT 'pending',
    completed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_onboarding_task_template UNIQUE (employee_id, template_key),
    CONSTRAINT ck_onboarding_status CHECK (status IN ('pending', 'in_progress', 'completed'))
);

CREATE TABLE employee_documents (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    document_type varchar(80) NOT NULL,
    version integer NOT NULL DEFAULT 1,
    original_file_name varchar(255) NOT NULL,
    object_key varchar(1024) NOT NULL UNIQUE,
    content_type varchar(100) NOT NULL,
    size_bytes bigint NOT NULL,
    uploaded_by bigint NOT NULL REFERENCES users(id),
    uploaded_at timestamptz NOT NULL DEFAULT now(),
    retention_until date,
    deleted_at timestamptz,
    CONSTRAINT ux_employee_document_version UNIQUE (employee_id, document_type, version),
    CONSTRAINT ck_employee_document_size CHECK (size_bytes > 0)
);

CREATE TABLE employee_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    event_type varchar(50) NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'draft',
    effective_date date NOT NULL,
    before_data jsonb NOT NULL,
    after_data jsonb NOT NULL,
    reason text NOT NULL,
    created_by bigint NOT NULL REFERENCES users(id),
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    applied_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_employee_event_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'applied', 'cancelled')),
    CONSTRAINT ck_employee_event_type CHECK (event_type IN (
        'probation_confirmation', 'probation_extension', 'promotion', 'demotion', 'transfer',
        'salary_adjustment', 'suspension', 'return_to_work', 'termination'
    ))
);

CREATE TABLE application_stage_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    application_id bigint NOT NULL REFERENCES applications(id),
    from_stage varchar(40),
    to_stage varchar(40) NOT NULL,
    reason text,
    changed_by bigint NOT NULL REFERENCES users(id),
    changed_at timestamptz NOT NULL DEFAULT now(),
    application_version bigint NOT NULL,
    CONSTRAINT ux_application_stage_version UNIQUE (application_id, application_version),
    CONSTRAINT ck_stage_event_from CHECK (from_stage IS NULL OR from_stage IN ('sourced_applied', 'ai_screening', 'tech_interview', 'executive_round', 'offer_letter', 'hired_ready', 'rejected', 'withdrawn')),
    CONSTRAINT ck_stage_event_to CHECK (to_stage IN ('sourced_applied', 'ai_screening', 'tech_interview', 'executive_round', 'offer_letter', 'hired_ready', 'rejected', 'withdrawn'))
);

CREATE TABLE audit_logs (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    actor_user_id bigint REFERENCES users(id),
    action varchar(120) NOT NULL,
    entity_type varchar(100) NOT NULL,
    entity_id varchar(100) NOT NULL,
    before_data jsonb,
    after_data jsonb,
    result varchar(30) NOT NULL,
    correlation_id varchar(100) NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_audit_result CHECK (result IN ('succeeded', 'rejected', 'failed'))
);

CREATE INDEX ix_audit_entity ON audit_logs(entity_type, entity_id, occurred_at DESC);
CREATE INDEX ix_audit_actor ON audit_logs(actor_user_id, occurred_at DESC);

CREATE TABLE outbox_messages (
    id uuid PRIMARY KEY,
    message_type varchar(200) NOT NULL,
    aggregate_type varchar(100) NOT NULL,
    aggregate_id varchar(100) NOT NULL,
    payload jsonb NOT NULL,
    occurred_at timestamptz NOT NULL,
    available_at timestamptz NOT NULL DEFAULT now(),
    processed_at timestamptz,
    attempts integer NOT NULL DEFAULT 0,
    last_error text,
    CONSTRAINT ck_outbox_attempts CHECK (attempts >= 0)
);

CREATE INDEX ix_outbox_pending ON outbox_messages(available_at)
WHERE processed_at IS NULL;

-- ===========================================================================
-- Core HR — employee lifecycle extensions
-- Requirements: EMP-06 (Probation Review), EMP-07 (Offboarding & Handover)
-- Status: Proposed. Business rules pending HR/Legal sign-off — see
-- docs/open_decisions_attendance_leave.md.
-- ===========================================================================

CREATE TABLE probation_reviews (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    contract_id bigint NOT NULL REFERENCES contracts(id),
    review_due_date date NOT NULL,
    reviewer_user_id bigint REFERENCES users(id),
    status varchar(30) NOT NULL DEFAULT 'pending',
    outcome varchar(30),
    overall_score numeric(3,1),
    strengths text,
    improvements text,
    effective_date date,
    decided_by bigint REFERENCES users(id),
    decided_at timestamptz,
    employee_event_id bigint REFERENCES employee_events(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_probation_review_contract UNIQUE (contract_id),
    CONSTRAINT ck_probation_status CHECK (status IN ('pending', 'in_review', 'decided', 'cancelled')),
    CONSTRAINT ck_probation_outcome CHECK (outcome IS NULL OR outcome IN ('confirmed', 'extended', 'terminated')),
    CONSTRAINT ck_probation_score CHECK (overall_score IS NULL OR overall_score BETWEEN 0 AND 5),
    CONSTRAINT ck_probation_decided CHECK (
        status <> 'decided' OR
        (outcome IS NOT NULL AND decided_by IS NOT NULL AND decided_at IS NOT NULL AND effective_date IS NOT NULL)
    )
);

CREATE INDEX ix_probation_reviews_due ON probation_reviews(review_due_date, status);

CREATE TABLE offboarding_cases (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    employee_event_id bigint REFERENCES employee_events(id),
    separation_type varchar(40) NOT NULL,
    notice_received_on date,
    last_working_date date NOT NULL,
    handover_to_employee_id bigint REFERENCES employees(id),
    exit_interview_at timestamptz,
    final_settlement_status varchar(30) NOT NULL DEFAULT 'pending',
    status varchar(30) NOT NULL DEFAULT 'draft',
    reason text NOT NULL,
    created_by bigint NOT NULL REFERENCES users(id),
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    completed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_offboarding_separation CHECK (separation_type IN ('resignation', 'mutual_agreement', 'dismissal', 'contract_expiry', 'retirement')),
    CONSTRAINT ck_offboarding_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_offboarding_settlement CHECK (final_settlement_status IN ('pending', 'calculated', 'paid', 'waived')),
    CONSTRAINT ck_offboarding_handover_not_self CHECK (handover_to_employee_id IS NULL OR handover_to_employee_id <> employee_id),
    CONSTRAINT ck_offboarding_completed CHECK (status <> 'completed' OR completed_at IS NOT NULL)
);

CREATE UNIQUE INDEX ux_offboarding_open_case ON offboarding_cases(employee_id)
WHERE status IN ('draft', 'pending_approval', 'approved', 'in_progress');

CREATE TABLE offboarding_tasks (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    offboarding_case_id bigint NOT NULL REFERENCES offboarding_cases(id) ON DELETE CASCADE,
    template_key varchar(100) NOT NULL,
    category varchar(30) NOT NULL,
    task_name varchar(255) NOT NULL,
    description text,
    assigned_to_user_id bigint REFERENCES users(id),
    due_at timestamptz,
    blocks_last_working_day boolean NOT NULL DEFAULT false,
    status varchar(30) NOT NULL DEFAULT 'pending',
    completed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_offboarding_task_template UNIQUE (offboarding_case_id, template_key),
    CONSTRAINT ck_offboarding_task_category CHECK (category IN ('it', 'admin', 'hr', 'manager', 'finance')),
    CONSTRAINT ck_offboarding_task_status CHECK (status IN ('pending', 'in_progress', 'completed')),
    CONSTRAINT ck_offboarding_task_completed CHECK (status <> 'completed' OR completed_at IS NOT NULL)
);

-- ===========================================================================
-- Attendance & Leave — ATT-01..ATT-04
-- Status: Proposed. Every calculation rule is bound to an approved
-- attendance_policies row; no timesheet may be computed against a draft policy.
-- ===========================================================================

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE attendance_policies (
    policy_version varchar(50) PRIMARY KEY,
    effective_from date NOT NULL,
    effective_to date,
    timezone varchar(100) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    late_grace_minutes integer NOT NULL DEFAULT 0,
    early_leave_grace_minutes integer NOT NULL DEFAULT 0,
    rounding_rule varchar(30) NOT NULL DEFAULT 'none',
    rounding_step_minutes integer NOT NULL DEFAULT 1,
    absence_threshold_minutes integer NOT NULL DEFAULT 240,
    min_overtime_minutes integer NOT NULL DEFAULT 30,
    night_shift_starts_at time NOT NULL DEFAULT '22:00',
    night_shift_ends_at time NOT NULL DEFAULT '06:00',
    status varchar(20) NOT NULL DEFAULT 'draft',
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_attendance_policy_range CHECK (effective_to IS NULL OR effective_to >= effective_from),
    CONSTRAINT ck_attendance_policy_status CHECK (status IN ('draft', 'active', 'superseded')),
    CONSTRAINT ck_attendance_policy_rounding CHECK (rounding_rule IN ('none', 'nearest', 'floor', 'ceil')),
    CONSTRAINT ck_attendance_policy_step CHECK (rounding_step_minutes BETWEEN 1 AND 60),
    CONSTRAINT ck_attendance_policy_grace CHECK (late_grace_minutes >= 0 AND early_leave_grace_minutes >= 0),
    CONSTRAINT ck_attendance_policy_approved CHECK (
        status = 'draft' OR (approved_by IS NOT NULL AND approved_at IS NOT NULL)
    )
);

CREATE TABLE holidays (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    calendar_code varchar(50) NOT NULL DEFAULT 'VN',
    holiday_date date NOT NULL,
    name varchar(255) NOT NULL,
    paid boolean NOT NULL DEFAULT true,
    work_coefficient numeric(4,2) NOT NULL DEFAULT 3.00,
    created_by bigint NOT NULL REFERENCES users(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_holidays_calendar_date UNIQUE (calendar_code, holiday_date),
    CONSTRAINT ck_holiday_coefficient CHECK (work_coefficient >= 0 AND work_coefficient <= 5)
);

CREATE TABLE work_shifts (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(50) NOT NULL UNIQUE,
    name varchar(255) NOT NULL,
    starts_at time NOT NULL,
    ends_at time NOT NULL,
    timezone varchar(100) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    break_minutes integer NOT NULL DEFAULT 0,
    standard_work_minutes integer NOT NULL,
    work_coefficient numeric(4,2) NOT NULL DEFAULT 1.00,
    crosses_midnight boolean NOT NULL DEFAULT false,
    active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_shift_break CHECK (break_minutes BETWEEN 0 AND 1440),
    CONSTRAINT ck_shift_standard_minutes CHECK (standard_work_minutes BETWEEN 1 AND 1440),
    CONSTRAINT ck_shift_coefficient CHECK (work_coefficient >= 0 AND work_coefficient <= 5),
    CONSTRAINT ck_shift_crosses_midnight CHECK (crosses_midnight = (ends_at <= starts_at))
);

CREATE TABLE work_schedule_assignments (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    work_date date NOT NULL,
    shift_id bigint NOT NULL REFERENCES work_shifts(id),
    timezone varchar(100) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    assigned_by bigint NOT NULL REFERENCES users(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_schedule_employee_date UNIQUE (employee_id, work_date)
);

CREATE INDEX ix_schedule_date_employee ON work_schedule_assignments(work_date, employee_id);

CREATE TABLE attendance_events (
    id uuid PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    work_date date NOT NULL,
    event_type varchar(20) NOT NULL,
    occurred_at timestamptz NOT NULL,
    timezone varchar(100) NOT NULL,
    method varchar(20) NOT NULL,
    source varchar(20) NOT NULL DEFAULT 'self_service',
    latitude numeric(9,6),
    longitude numeric(9,6),
    device_id varchar(200),
    external_event_id varchar(200),
    idempotency_key varchar(200),
    recorded_by bigint REFERENCES users(id),
    recorded_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_attendance_event_type CHECK (event_type IN ('check_in', 'check_out')),
    CONSTRAINT ck_attendance_event_method CHECK (method IN ('fingerprint', 'gps', 'face_id', 'wifi', 'web', 'manual')),
    CONSTRAINT ck_attendance_event_source CHECK (source IN ('self_service', 'device', 'import', 'correction')),
    CONSTRAINT ck_attendance_event_geo CHECK ((latitude IS NULL) = (longitude IS NULL)),
    CONSTRAINT ck_attendance_event_device CHECK (
        source <> 'device' OR (device_id IS NOT NULL AND external_event_id IS NOT NULL)
    )
);

CREATE UNIQUE INDEX ux_attendance_events_device ON attendance_events(device_id, external_event_id)
WHERE external_event_id IS NOT NULL;

CREATE UNIQUE INDEX ux_attendance_events_idempotency ON attendance_events(employee_id, idempotency_key)
WHERE idempotency_key IS NOT NULL;

CREATE INDEX ix_attendance_events_employee_day ON attendance_events(employee_id, work_date, occurred_at);

CREATE TABLE timesheet_periods (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    period_code varchar(50) NOT NULL UNIQUE,
    starts_on date NOT NULL,
    ends_on date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'open',
    reviewed_by bigint REFERENCES users(id),
    reviewed_at timestamptz,
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    locked_by bigint REFERENCES users(id),
    locked_at timestamptz,
    reopened_by bigint REFERENCES users(id),
    reopen_reason text,
    payroll_handoff_at timestamptz,
    payroll_handoff_reference varchar(200),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_timesheet_period_range CHECK (ends_on >= starts_on),
    CONSTRAINT ck_timesheet_period_status CHECK (status IN ('open', 'pending_approval', 'approved', 'locked', 'reopened')),
    CONSTRAINT ck_timesheet_period_locked CHECK (status <> 'locked' OR (locked_by IS NOT NULL AND locked_at IS NOT NULL)),
    CONSTRAINT ck_timesheet_period_reopened CHECK (status <> 'reopened' OR (reopened_by IS NOT NULL AND reopen_reason IS NOT NULL)),
    CONSTRAINT ck_timesheet_period_handoff CHECK (payroll_handoff_at IS NULL OR locked_at IS NOT NULL),
    CONSTRAINT ex_timesheet_periods_no_overlap EXCLUDE USING gist (daterange(starts_on, ends_on, '[]') WITH &&)
);

CREATE TABLE leave_types (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(50) NOT NULL UNIQUE,
    name varchar(255) NOT NULL,
    paid boolean NOT NULL DEFAULT true,
    unit_of_balance varchar(10) NOT NULL DEFAULT 'days',
    allow_negative_balance boolean NOT NULL DEFAULT false,
    allow_half_day boolean NOT NULL DEFAULT true,
    counts_holidays boolean NOT NULL DEFAULT false,
    requires_attachment boolean NOT NULL DEFAULT false,
    min_notice_days integer NOT NULL DEFAULT 0,
    max_consecutive_units numeric(7,2),
    approval_levels smallint NOT NULL DEFAULT 1,
    policy_version varchar(50) NOT NULL,
    policy_status varchar(20) NOT NULL DEFAULT 'draft',
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_leave_type_unit CHECK (unit_of_balance IN ('days', 'hours')),
    CONSTRAINT ck_leave_type_notice CHECK (min_notice_days >= 0),
    CONSTRAINT ck_leave_type_levels CHECK (approval_levels BETWEEN 1 AND 3),
    CONSTRAINT ck_leave_type_policy_status CHECK (policy_status IN ('draft', 'approved')),
    CONSTRAINT ck_leave_type_approved CHECK (
        policy_status <> 'approved' OR (approved_by IS NOT NULL AND approved_at IS NOT NULL)
    )
);

CREATE TABLE leave_balances (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    leave_type_id bigint NOT NULL REFERENCES leave_types(id),
    year integer NOT NULL,
    entitled_units numeric(7,2) NOT NULL DEFAULT 0,
    carried_over_units numeric(7,2) NOT NULL DEFAULT 0,
    carry_over_expires_on date,
    used_units numeric(7,2) NOT NULL DEFAULT 0,
    reserved_units numeric(7,2) NOT NULL DEFAULT 0,
    available_units numeric(7,2) GENERATED ALWAYS AS
        (entitled_units + carried_over_units - used_units - reserved_units) STORED,
    unit_of_balance varchar(10) NOT NULL DEFAULT 'days',
    policy_version varchar(50) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_leave_balance_employee_type_year UNIQUE (employee_id, leave_type_id, year),
    CONSTRAINT ck_leave_balance_year CHECK (year BETWEEN 2000 AND 2100),
    CONSTRAINT ck_leave_balance_unit CHECK (unit_of_balance IN ('days', 'hours')),
    CONSTRAINT ck_leave_balance_non_negative CHECK (
        entitled_units >= 0 AND carried_over_units >= 0 AND used_units >= 0 AND reserved_units >= 0
    )
);

CREATE TABLE leave_requests (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_code varchar(50) NOT NULL UNIQUE,
    employee_id bigint NOT NULL REFERENCES employees(id),
    leave_type_id bigint NOT NULL REFERENCES leave_types(id),
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    timezone varchar(100) NOT NULL,
    unit varchar(20) NOT NULL,
    requested_units numeric(7,2) NOT NULL,
    unit_of_balance varchar(10) NOT NULL,
    balance_year integer NOT NULL,
    reason text NOT NULL,
    attachment_object_key varchar(1024),
    status varchar(20) NOT NULL DEFAULT 'pending',
    current_approval_level smallint NOT NULL DEFAULT 1,
    policy_version varchar(50) NOT NULL,
    idempotency_key varchar(200),
    cancellation_reason text,
    created_by bigint NOT NULL REFERENCES users(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_leave_request_range CHECK (ends_at > starts_at),
    CONSTRAINT ck_leave_request_units CHECK (requested_units > 0),
    CONSTRAINT ck_leave_request_unit CHECK (unit IN ('full_day', 'first_half', 'second_half', 'hours')),
    CONSTRAINT ck_leave_request_balance_unit CHECK (unit_of_balance IN ('days', 'hours')),
    CONSTRAINT ck_leave_request_status CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled')),
    CONSTRAINT ck_leave_request_level CHECK (current_approval_level BETWEEN 1 AND 3),
    CONSTRAINT ex_leave_requests_no_overlap EXCLUDE USING gist (
        employee_id WITH =,
        tstzrange(starts_at, ends_at, '[)') WITH &&
    ) WHERE (status IN ('pending', 'approved'))
);

CREATE UNIQUE INDEX ux_leave_requests_idempotency ON leave_requests(employee_id, idempotency_key)
WHERE idempotency_key IS NOT NULL;

CREATE INDEX ix_leave_requests_approval_queue ON leave_requests(status, starts_at);
CREATE INDEX ix_leave_requests_employee ON leave_requests(employee_id, starts_at DESC);

CREATE TABLE leave_request_decisions (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    leave_request_id bigint NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
    sequence smallint NOT NULL,
    approver_user_id bigint NOT NULL REFERENCES users(id),
    decision varchar(20) NOT NULL,
    reason text,
    decided_at timestamptz NOT NULL DEFAULT now(),
    request_version bigint NOT NULL,
    CONSTRAINT ux_leave_decision_sequence UNIQUE (leave_request_id, sequence),
    CONSTRAINT ck_leave_decision CHECK (decision IN ('approved', 'rejected')),
    CONSTRAINT ck_leave_decision_sequence CHECK (sequence BETWEEN 1 AND 3)
);

CREATE TABLE attendance_corrections (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    work_date date NOT NULL,
    current_check_in_at timestamptz,
    current_check_out_at timestamptz,
    proposed_check_in_at timestamptz NOT NULL,
    proposed_check_out_at timestamptz NOT NULL,
    reason text NOT NULL,
    attachment_object_key varchar(1024),
    status varchar(20) NOT NULL DEFAULT 'pending',
    requested_by bigint NOT NULL REFERENCES users(id),
    decided_by bigint REFERENCES users(id),
    decision_reason text,
    decided_at timestamptz,
    applied_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_correction_range CHECK (proposed_check_out_at > proposed_check_in_at),
    CONSTRAINT ck_correction_status CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled')),
    CONSTRAINT ck_correction_decided CHECK (
        status = 'pending' OR (decided_by IS NOT NULL AND decided_at IS NOT NULL)
    ),
    CONSTRAINT ck_correction_applied CHECK (applied_at IS NULL OR status = 'approved')
);

CREATE UNIQUE INDEX ux_corrections_one_pending_per_day ON attendance_corrections(employee_id, work_date)
WHERE status = 'pending';

CREATE INDEX ix_corrections_queue ON attendance_corrections(status, work_date);

CREATE TABLE overtime_requests (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_code varchar(50) NOT NULL UNIQUE,
    employee_id bigint NOT NULL REFERENCES employees(id),
    work_date date NOT NULL,
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    timezone varchar(100) NOT NULL,
    overtime_category varchar(30) NOT NULL,
    requested_minutes integer NOT NULL,
    approved_minutes integer,
    work_coefficient numeric(4,2) NOT NULL,
    reason text NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'pending',
    requested_by bigint NOT NULL REFERENCES users(id),
    decided_by bigint REFERENCES users(id),
    decision_reason text,
    decided_at timestamptz,
    policy_version varchar(50) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_overtime_range CHECK (ends_at > starts_at),
    CONSTRAINT ck_overtime_minutes CHECK (
        requested_minutes > 0 AND
        (approved_minutes IS NULL OR (approved_minutes >= 0 AND approved_minutes <= requested_minutes))
    ),
    CONSTRAINT ck_overtime_category CHECK (overtime_category IN ('weekday', 'weekly_rest', 'holiday', 'night')),
    CONSTRAINT ck_overtime_coefficient CHECK (work_coefficient >= 1 AND work_coefficient <= 5),
    CONSTRAINT ck_overtime_status CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled')),
    CONSTRAINT ck_overtime_decided CHECK (
        status = 'pending' OR (decided_by IS NOT NULL AND decided_at IS NOT NULL)
    ),
    CONSTRAINT ex_overtime_requests_no_overlap EXCLUDE USING gist (
        employee_id WITH =,
        tstzrange(starts_at, ends_at, '[)') WITH &&
    ) WHERE (status IN ('pending', 'approved'))
);

CREATE INDEX ix_overtime_requests_queue ON overtime_requests(status, work_date);

CREATE TABLE attendance_daily_records (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    employee_id bigint NOT NULL REFERENCES employees(id),
    work_date date NOT NULL,
    timesheet_period_id bigint NOT NULL REFERENCES timesheet_periods(id),
    shift_id bigint REFERENCES work_shifts(id),
    holiday_id bigint REFERENCES holidays(id),
    leave_request_id bigint REFERENCES leave_requests(id),
    first_check_in_at timestamptz,
    last_check_out_at timestamptz,
    status varchar(30) NOT NULL,
    scheduled_minutes integer NOT NULL DEFAULT 0,
    worked_minutes integer NOT NULL DEFAULT 0,
    late_minutes integer NOT NULL DEFAULT 0,
    early_leave_minutes integer NOT NULL DEFAULT 0,
    overtime_minutes integer NOT NULL DEFAULT 0,
    leave_units numeric(7,2) NOT NULL DEFAULT 0,
    correction_id bigint REFERENCES attendance_corrections(id),
    policy_version varchar(50) NOT NULL REFERENCES attendance_policies(policy_version),
    computed_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ux_attendance_daily_employee_date UNIQUE (employee_id, work_date),
    CONSTRAINT ck_attendance_daily_status CHECK (status IN ('on_time', 'late', 'early_leave', 'absent', 'incomplete', 'corrected', 'on_leave', 'holiday', 'day_off')),
    CONSTRAINT ck_attendance_daily_minutes CHECK (
        scheduled_minutes >= 0 AND worked_minutes >= 0 AND late_minutes >= 0 AND
        early_leave_minutes >= 0 AND overtime_minutes >= 0 AND leave_units >= 0
    ),
    CONSTRAINT ck_attendance_daily_checkpair CHECK (
        last_check_out_at IS NULL OR first_check_in_at IS NULL OR last_check_out_at > first_check_in_at
    )
);

CREATE INDEX ix_attendance_daily_period ON attendance_daily_records(timesheet_period_id, employee_id);
CREATE INDEX ix_attendance_daily_range ON attendance_daily_records(work_date, employee_id);

COMMIT;
