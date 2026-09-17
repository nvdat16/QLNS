-- QLNS canonical PostgreSQL schema — baseline v1
-- Scope: Core HR (incl. Contracts) and Recruitment — 23 tables.
-- Delivery scope follows the bold leaf functions under Recruitment and Core HR in
-- topdown-approach.png; see section 2 of README.md. Out of scope for this delivery:
-- Headcount & Budget Validation, Recruitment Channel Management, Organizational Chart
-- and Suspension & Return to Work. The users / user_roles / audit_logs / outbox_messages
-- tables stay canonical: identity data plus the crosscutting audit and outbox mechanisms
-- are still mandatory, only the administration endpoints that managed them are out of scope.
-- Attendance & Leave DDL is parked out of scope in docs/deferred/attendance_leave/schema_attendance_leave.sql.
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

-- A resume row is created at intake time, before any candidate exists.
-- intake_id is the public identifier exposed by the Recruitment Intake API;
-- intake_status is the aggregate workflow state, while malware_scan_status and
-- parser_status remain the two technical sub-states that drive it.
CREATE TABLE resumes (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    intake_id uuid NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    job_posting_id bigint NOT NULL REFERENCES job_postings(id),
    candidate_id bigint REFERENCES candidates(id),
    object_key varchar(1024) NOT NULL UNIQUE,
    original_file_name varchar(255) NOT NULL,
    content_type varchar(100) NOT NULL,
    size_bytes bigint NOT NULL,
    intake_status varchar(30) NOT NULL DEFAULT 'scanning',
    malware_scan_status varchar(30) NOT NULL DEFAULT 'pending',
    parser_status varchar(30) NOT NULL DEFAULT 'pending',
    parsed_data jsonb,
    parse_confidence jsonb,
    parser_version varchar(100),
    duplicate_candidate_ids bigint[] NOT NULL DEFAULT '{}',
    uploaded_by bigint REFERENCES users(id),
    uploaded_at timestamptz NOT NULL DEFAULT now(),
    confirmed_by bigint REFERENCES users(id),
    confirmed_at timestamptz,
    CONSTRAINT ck_resume_size CHECK (size_bytes > 0 AND size_bytes <= 10485760),
    CONSTRAINT ck_resume_intake_status CHECK (intake_status IN (
        'scanning', 'parsing', 'awaiting_confirmation', 'duplicate_review', 'completed', 'rejected', 'failed'
    )),
    CONSTRAINT ck_resume_scan CHECK (malware_scan_status IN ('pending', 'clean', 'infected', 'failed')),
    CONSTRAINT ck_resume_parser CHECK (parser_status IN ('pending', 'processing', 'completed', 'failed', 'confirmed')),
    CONSTRAINT ck_resume_completed_has_candidate CHECK (
        intake_status <> 'completed' OR (candidate_id IS NOT NULL AND confirmed_at IS NOT NULL)
    )
);

CREATE INDEX ix_resumes_open_intakes ON resumes(job_posting_id, uploaded_at)
    WHERE intake_status IN ('scanning', 'parsing', 'awaiting_confirmation', 'duplicate_review');

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
    work_email varchar(320),
    personal_email varchar(320),
    phone varchar(30),
    date_of_birth date,
    gender varchar(30),
    office_location varchar(255),
    -- Personal fields required by SRS EMP-01 and PersonalProfilePatch in api/openapi.yaml.
    -- Restricted data: encrypt at rest, mask in UI, never include in default exports.
    permanent_address text,
    temporary_address text,
    emergency_contact jsonb,
    manager_id bigint REFERENCES employees(id),
    department_id bigint NOT NULL REFERENCES departments(id),
    position_id bigint NOT NULL REFERENCES positions(id),
    hire_date date NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'probation',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    -- 'suspended' is reserved: Suspension & Return to Work is out of scope for the current
    -- delivery, so no endpoint can set it. Kept so the value set stays stable.
    CONSTRAINT ck_employee_status CHECK (status IN ('probation', 'active', 'suspended', 'terminated')),
    CONSTRAINT ck_employee_not_self_manager CHECK (manager_id IS NULL OR manager_id <> id),
    -- Created by offer-acceptance handoff before IT provisions a mailbox; must exist once active.
    CONSTRAINT ck_employee_active_requires_work_email CHECK (status <> 'active' OR work_email IS NOT NULL)
);

CREATE UNIQUE INDEX ux_employees_work_email ON employees(lower(work_email)) WHERE work_email IS NOT NULL;
CREATE INDEX ix_employees_directory ON employees(department_id, status, last_name, first_name);

-- Source for employee_code; the domain formats it as EMP-<zero-padded 5 digits>.
CREATE SEQUENCE employee_code_seq START WITH 1 INCREMENT BY 1;


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

-- Offer-acceptance handoff dedupe: at most one draft probation contract per employee.
CREATE UNIQUE INDEX ux_contracts_one_draft_probation ON contracts(employee_id)
WHERE contract_type = 'probation' AND status = 'draft';

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
    -- Applied events are immutable; a correction points at the event it compensates (EmployeeEventWrite.compensatesEventId).
    compensates_event_id bigint REFERENCES employee_events(id),
    created_by bigint NOT NULL REFERENCES users(id),
    approved_by bigint REFERENCES users(id),
    approved_at timestamptz,
    applied_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1,
    CONSTRAINT ck_employee_event_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'applied', 'cancelled')),
    -- 'suspension' and 'return_to_work' are reserved: Suspension & Return to Work is out of
    -- scope for the current delivery, so no endpoint can set them.
    CONSTRAINT ck_employee_event_type CHECK (event_type IN (
        'probation_confirmation', 'probation_extension', 'promotion', 'demotion', 'transfer',
        'salary_adjustment', 'suspension', 'return_to_work', 'termination', 'correction'
    )),
    CONSTRAINT ck_employee_event_not_self_compensation CHECK (compensates_event_id IS NULL OR compensates_event_id <> id)
);

CREATE INDEX ix_employee_events_due ON employee_events(effective_date, status) WHERE status = 'approved';

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
-- Status: Proposed. Offboarding final-settlement rules belong to Payroll (out of scope).
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

COMMIT;
