-- QLNS development seed — Core HR smoke-test data. DEVELOPMENT ONLY, never run against a shared environment.
-- Ids are fixed so that the /dev/token personas in Qlns.Api can reference them.
BEGIN;

INSERT INTO users (id, external_subject, email, display_name, status) OVERRIDING SYSTEM VALUE VALUES
    (1, 'dev|hr-manager',   'hr.manager@qlns.local',   'Trần Thị HR Manager',  'active'),
    (2, 'dev|hr-officer',   'hr.officer@qlns.local',   'Lê Văn HR Officer',    'active'),
    (3, 'dev|line-manager', 'eng.manager@qlns.local',  'Nguyễn Văn Eng Lead',  'active'),
    (4, 'dev|employee',     'dev.nguyen@qlns.local',   'Nguyễn Văn Dev',       'active'),
    (5, 'dev|it-admin',     'it.admin@qlns.local',     'Phạm IT Admin',        'active'),
    (6, 'dev|ceo',          'ceo@qlns.local',          'Hoàng CEO',            'active');

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
    (4, 'ROLE_EMPLOYEE',   'self',         0);

INSERT INTO onboarding_tasks (id, employee_id, template_key, task_name, description, assigned_to_user_id, due_at, status, completed_at) OVERRIDING SYSTEM VALUE VALUES
    (1, 4, 'it.email',      'Tạo email công vụ',          'Tạo mailbox và cấp quyền Slack/Git', 5, '2026-07-31T10:00:00Z', 'pending',     NULL),
    (2, 4, 'it.laptop',     'Chuẩn bị laptop',            NULL,                                5, '2026-07-31T10:00:00Z', 'in_progress', NULL),
    (3, 4, 'admin.badge',   'Cấp thẻ nhân viên',          NULL,                                2, '2026-08-01T02:00:00Z', 'pending',     NULL),
    (4, 4, 'hr.contract',   'Chuẩn bị hợp đồng thử việc', NULL,                                2, '2026-07-30T10:00:00Z', 'completed',   '2026-07-29T08:00:00Z'),
    (5, 4, 'manager.buddy', 'Bố trí buddy/mentor',        NULL,                                3, '2026-08-05T10:00:00Z', 'pending',     NULL);

INSERT INTO employee_events (id, employee_id, event_type, status, effective_date, before_data, after_data, reason, created_by) OVERRIDING SYSTEM VALUE VALUES
    (1, 5, 'transfer', 'draft', '2026-10-01', '{"departmentId": 3}', '{"departmentId": 2}', 'Điều chuyển sang Khối Kỹ thuật theo quyết định 12/QĐ', 2);

-- Realign identity sequences with the fixed ids above.
SELECT setval(pg_get_serial_sequence('users', 'id'),            (SELECT max(id) FROM users));
SELECT setval(pg_get_serial_sequence('departments', 'id'),      (SELECT max(id) FROM departments));
SELECT setval(pg_get_serial_sequence('positions', 'id'),        (SELECT max(id) FROM positions));
SELECT setval(pg_get_serial_sequence('employees', 'id'),        (SELECT max(id) FROM employees));
SELECT setval(pg_get_serial_sequence('onboarding_tasks', 'id'), (SELECT max(id) FROM onboarding_tasks));
SELECT setval(pg_get_serial_sequence('employee_events', 'id'),  (SELECT max(id) FROM employee_events));
SELECT setval('employee_code_seq', 7);

COMMIT;
