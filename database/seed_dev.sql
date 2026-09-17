-- QLNS development seed — Core HR, Recruitment and Contracts smoke-test data. DEVELOPMENT ONLY, never run against a shared environment.
-- Ids are fixed so that the /dev/token personas in Qlns.Api can reference them.
BEGIN;

INSERT INTO users (id, external_subject, email, display_name, status) OVERRIDING SYSTEM VALUE VALUES
    (1, 'dev|hr-manager',   'hr.manager@qlns.local',   'Trần Thị HR Manager',  'active'),
    (2, 'dev|hr-officer',   'hr.officer@qlns.local',   'Lê Văn HR Officer',    'active'),
    (3, 'dev|line-manager', 'eng.manager@qlns.local',  'Nguyễn Văn Eng Lead',  'active'),
    (4, 'dev|employee',     'dev.nguyen@qlns.local',   'Nguyễn Văn Dev',       'active'),
    (5, 'dev|it-admin',     'it.admin@qlns.local',     'Phạm IT Admin',        'active'),
    (6, 'dev|ceo',          'ceo@qlns.local',          'Hoàng CEO',            'active'),
    (7, 'dev|recruiter',    'recruiter@qlns.local',    'Vũ Thị Recruiter',     'active');

INSERT INTO departments (id, code, name, parent_department_id, cost_center) OVERRIDING SYSTEM VALUE VALUES
    (1, 'BOD',  'Ban Giám đốc',      NULL, 'CC-000'),
    (2, 'ENG',  'Khối Kỹ thuật',     1,    'CC-100'),
    (3, 'PROD', 'Phòng Sản phẩm',    1,    'CC-200'),
    (4, 'BE',   'Đội Backend',       2,    'CC-110'),
    (5, 'HR',   'Phòng Nhân sự',     1,    'CC-300'),
    (6, 'EMPTY','Phòng trống (test xóa)', 1, NULL);

INSERT INTO positions (id, code, name, level) OVERRIDING SYSTEM VALUE VALUES
    (1, 'CEO',    'Tổng Giám đốc',        'L-9'),
    (2, 'ENG-MGR','Engineering Manager',  'L-5'),
    (3, 'SWE',    'Software Engineer',    'IC-2'),
    (4, 'PM',     'Product Manager',      'IC-4'),
    (5, 'HR-MGR', 'HR Manager',           'L-5'),
    (6, 'HR-OFF', 'HR Officer',           'IC-2');

INSERT INTO employees (id, employee_code, user_id, first_name, last_name, work_email, personal_email, phone,
    date_of_birth, gender, office_location, manager_id, department_id, position_id, hire_date, status) OVERRIDING SYSTEM VALUE VALUES
    (1, 'EMP-00001', 6, 'Hoàng',  'Nguyễn Minh', 'ceo@qlns.local',         NULL,                    '0900000001', '1975-03-01', 'male',   'Hà Nội', NULL, 1, 1, '2015-01-05', 'active'),
    (2, 'EMP-00002', 1, 'HR',     'Trần Thị',    'hr.manager@qlns.local',  NULL,                    '0900000002', '1985-07-12', 'female', 'Hà Nội', 1,    5, 5, '2018-03-01', 'active'),
    (3, 'EMP-00003', 3, 'Lead',   'Nguyễn Văn',  'eng.manager@qlns.local', NULL,                    '0900000003', '1988-11-20', 'male',   'Hà Nội', 1,    2, 2, '2019-06-10', 'active'),
    (4, 'EMP-00004', 4, 'Dev',    'Nguyễn Văn',  'dev.nguyen@qlns.local',  'dev.personal@gmail.com','0900000004', '1998-02-14', 'male',   'Hà Nội', 3,    4, 3, '2026-08-01', 'probation'),
    (5, 'EMP-00005', NULL,'PM',   'Lê Thị',      'pm.le@qlns.local',       NULL,                    '0900000005', '1992-09-09', 'female', 'TP.HCM', 1,    3, 4, '2021-04-01', 'active'),
    (6, 'EMP-00006', 2, 'Officer','Lê Văn',      'hr.officer@qlns.local',  NULL,                    '0900000006', '1995-05-05', 'male',   'Hà Nội', 2,    5, 6, '2022-02-01', 'active'),
    (7, 'EMP-00007', NULL,'Old',  'Phạm Văn',    NULL,                     NULL,                    NULL,         '1990-01-01', 'male',   'Hà Nội', 3,    4, 3, '2020-01-01', 'terminated');

INSERT INTO user_roles (user_id, role_code, data_scope_type, data_scope_id) VALUES
    (1, 'ROLE_HR_MGR',     'organization', 0),
    (2, 'ROLE_HR_OFFICER', 'organization', 0),
    (3, 'ROLE_LINE_MGR',   'department',   2),
    (3, 'ROLE_LINE_MGR',   'department',   4),
    (4, 'ROLE_EMPLOYEE',   'self',         0),
    (7, 'ROLE_RECRUITER',  'organization', 0);

INSERT INTO onboarding_tasks (id, employee_id, template_key, task_name, description, assigned_to_user_id, due_at, status, completed_at) OVERRIDING SYSTEM VALUE VALUES
    (1, 4, 'it.email',      'Tạo email công vụ',          'Tạo mailbox và cấp quyền Slack/Git', 5, '2026-07-31T10:00:00Z', 'pending',     NULL),
    (2, 4, 'it.laptop',     'Chuẩn bị laptop',            NULL,                                5, '2026-07-31T10:00:00Z', 'in_progress', NULL),
    (3, 4, 'admin.badge',   'Cấp thẻ nhân viên',          NULL,                                2, '2026-08-01T02:00:00Z', 'pending',     NULL),
    (4, 4, 'hr.contract',   'Chuẩn bị hợp đồng thử việc', NULL,                                2, '2026-07-30T10:00:00Z', 'completed',   '2026-07-29T08:00:00Z'),
    (5, 4, 'manager.buddy', 'Bố trí buddy/mentor',        NULL,                                3, '2026-08-05T10:00:00Z', 'pending',     NULL);

INSERT INTO employee_events (id, employee_id, event_type, status, effective_date, before_data, after_data, reason, created_by) OVERRIDING SYSTEM VALUE VALUES
    (1, 5, 'transfer', 'draft', '2026-10-01', '{"departmentId": 3}', '{"departmentId": 2}', 'Điều chuyển sang Khối Kỹ thuật theo quyết định 12/QĐ', 2);

-- ---------------------------------------------------------------------------------------------
-- Recruitment (REC-01 … REC-06): one published requisition with two applications at different stages,
-- one draft requisition owned by the line manager, one completed interview with a submitted scorecard.
-- ---------------------------------------------------------------------------------------------
INSERT INTO job_postings (id, job_code, title, department_id, position_id, description, location, employment_type,
    salary_min, salary_max, target_headcount, status, closing_date, published_at, created_by) OVERRIDING SYSTEM VALUE VALUES
    (1, 'REQ-2026-00001', 'Senior Backend Engineer', 4, 3, 'Thiết kế và phát triển dịch vụ .NET/PostgreSQL', 'Hà Nội', 'full_time',
        30000000, 45000000, 2, 'active_recruiting', '2026-12-31', '2026-08-15T02:00:00Z', 3),
    (2, 'REQ-2026-00002', 'Engineering Manager (Platform)', 2, 2, NULL, 'Hà Nội', 'hybrid',
        NULL, NULL, 1, 'draft', NULL, NULL, 3);

INSERT INTO candidates (id, first_name, last_name, email, normalized_email, phone, normalized_phone, privacy_notice_version, consented_at) OVERRIDING SYSTEM VALUE VALUES
    (1, 'Ứng', 'Trần Văn', 'ung.tran@example.com', 'ung.tran@example.com', '0912345678', '84912345678', 'v1', '2026-08-20T03:00:00Z'),
    (2, 'Viên', 'Lê Thị',  'vien.le@example.com',  'vien.le@example.com',  '0987654321', '84987654321', 'v1', '2026-08-22T03:00:00Z');

INSERT INTO resumes (id, intake_id, job_posting_id, candidate_id, object_key, original_file_name, content_type, size_bytes,
    intake_status, malware_scan_status, parser_status, parsed_data, uploaded_by, uploaded_at, confirmed_by, confirmed_at) OVERRIDING SYSTEM VALUE VALUES
    (1, '11111111-1111-4111-8111-111111111111', 1, 1, 'resumes/1/seed-ung-tran', 'ung-tran-cv.pdf', 'application/pdf', 204800,
        'completed', 'clean', 'confirmed', '{"candidate":{"firstName":"Ứng","lastName":"Trần Văn","email":"ung.tran@example.com"},"privacyNoticeVersion":"v1","consentedAt":"2026-08-20T03:00:00Z"}', 7, '2026-08-20T03:00:00Z', 7, '2026-08-20T03:30:00Z'),
    (2, '22222222-2222-4222-8222-222222222222', 1, 2, 'resumes/1/seed-vien-le',  'vien-le-cv.pdf',  'application/pdf', 189000,
        'completed', 'clean', 'confirmed', '{"candidate":{"firstName":"Viên","lastName":"Lê Thị","email":"vien.le@example.com"},"privacyNoticeVersion":"v1","consentedAt":"2026-08-22T03:00:00Z"}', 7, '2026-08-22T03:00:00Z', 7, '2026-08-22T03:30:00Z');

INSERT INTO applications (id, candidate_id, job_posting_id, resume_id, stage, ai_score, source, applied_at, version) OVERRIDING SYSTEM VALUE VALUES
    (1, 1, 1, 1, 'ai_screening', 72.50, 'careers', '2026-08-20T03:30:00Z', 2),
    (2, 2, 1, 2, 'offer_letter', 88.00, 'referral', '2026-08-22T03:30:00Z', 5);

INSERT INTO application_stage_events (application_id, from_stage, to_stage, changed_by, changed_at, application_version) VALUES
    (1, NULL,              'sourced_applied', 7, '2026-08-20T03:30:00Z', 1),
    (1, 'sourced_applied', 'ai_screening',    7, '2026-08-21T03:00:00Z', 2),
    (2, NULL,              'sourced_applied', 7, '2026-08-22T03:30:00Z', 1),
    (2, 'sourced_applied', 'ai_screening',    7, '2026-08-23T03:00:00Z', 2),
    (2, 'ai_screening',    'tech_interview',  7, '2026-08-25T03:00:00Z', 3),
    (2, 'tech_interview',  'executive_round', 7, '2026-09-02T03:00:00Z', 4),
    (2, 'executive_round', 'offer_letter',    7, '2026-09-08T03:00:00Z', 5);

INSERT INTO interviews (id, application_id, interview_type, starts_at, ends_at, timezone, interviewer_user_id, location, status) OVERRIDING SYSTEM VALUE VALUES
    (1, 2, 'technical', '2026-08-28T02:00:00Z', '2026-08-28T03:00:00Z', 'Asia/Ho_Chi_Minh', 3, 'Phòng họp A1', 'completed');

INSERT INTO interview_panelists (interview_id, user_id) VALUES (1, 3);

INSERT INTO evaluations (id, interview_id, evaluator_user_id, technical_score, communication_score, problem_solving_score, teamwork_score,
    overall_score, recommendation, feedback, submitted_at, version) OVERRIDING SYSTEM VALUE VALUES
    (1, 1, 3, 4.5, 4.0, 4.5, 4.0, 4.3, 'hire', 'Nền tảng .NET tốt, cần cải thiện thiết kế hệ thống phân tán.', '2026-08-28T05:00:00Z', 1);

-- ---------------------------------------------------------------------------------------------
-- Contracts (CON-01 … CON-03) and probation review (EMP-06): the probation employee (4) has an active
-- probation contract with a pending review; employee 5 has a fixed-term contract inside the alert window.
-- ---------------------------------------------------------------------------------------------
INSERT INTO contracts (id, employee_id, contract_number, contract_type, start_date, end_date, salary, currency, notice_period_days, status, is_primary, signed_at) OVERRIDING SYSTEM VALUE VALUES
    (1, 4, 'HD-TV-EMP-00004', 'probation',  '2026-08-01', '2026-09-30', 15000000, 'VND', 3,  'active', true, '2026-07-30T08:00:00Z'),
    (2, 3, 'HD-CT-EMP-00003', 'indefinite', '2020-06-10', NULL,         60000000, 'VND', 45, 'active', true, '2020-06-08T08:00:00Z'),
    (3, 5, 'HD-XD-EMP-00005', 'fixed_term', '2025-10-16', '2026-10-15', 35000000, 'VND', 30, 'active', true, '2025-10-14T08:00:00Z');

INSERT INTO probation_reviews (id, employee_id, contract_id, review_due_date, reviewer_user_id, status) OVERRIDING SYSTEM VALUE VALUES
    (1, 4, 1, '2026-09-23', 3, 'pending');

-- Realign identity sequences with the fixed ids above.
SELECT setval(pg_get_serial_sequence('users', 'id'),            (SELECT max(id) FROM users));
SELECT setval(pg_get_serial_sequence('departments', 'id'),      (SELECT max(id) FROM departments));
SELECT setval(pg_get_serial_sequence('positions', 'id'),        (SELECT max(id) FROM positions));
SELECT setval(pg_get_serial_sequence('employees', 'id'),        (SELECT max(id) FROM employees));
SELECT setval(pg_get_serial_sequence('onboarding_tasks', 'id'), (SELECT max(id) FROM onboarding_tasks));
SELECT setval(pg_get_serial_sequence('employee_events', 'id'),  (SELECT max(id) FROM employee_events));
SELECT setval(pg_get_serial_sequence('job_postings', 'id'),     (SELECT max(id) FROM job_postings));
SELECT setval(pg_get_serial_sequence('candidates', 'id'),       (SELECT max(id) FROM candidates));
SELECT setval(pg_get_serial_sequence('resumes', 'id'),          (SELECT max(id) FROM resumes));
SELECT setval(pg_get_serial_sequence('applications', 'id'),     (SELECT max(id) FROM applications));
SELECT setval(pg_get_serial_sequence('interviews', 'id'),       (SELECT max(id) FROM interviews));
SELECT setval(pg_get_serial_sequence('evaluations', 'id'),      (SELECT max(id) FROM evaluations));
SELECT setval(pg_get_serial_sequence('contracts', 'id'),        (SELECT max(id) FROM contracts));
SELECT setval(pg_get_serial_sequence('probation_reviews', 'id'),(SELECT max(id) FROM probation_reviews));
SELECT setval('employee_code_seq', 7);

COMMIT;
