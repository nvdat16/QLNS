import { useMemo, useState } from 'react';

const employees = [
  { id: 'EMP-2048', name: 'Marcus Vance', role: 'Lead Design Systems Architect', department: 'Experience Design', location: 'London HQ', status: 'Active', type: 'Permanent', initials: 'MV' },
  { id: 'EMP-1092', name: 'Elena Rostova', role: 'VP of Product & Experience Design', department: 'Experience Design', location: 'New York HQ', status: 'Active', type: 'Permanent', initials: 'ER' },
  { id: 'EMP-3041', name: 'Sarah Lin', role: 'UX Researcher', department: 'Experience Design', location: 'San Francisco', status: 'Active', type: 'Permanent', initials: 'SL' },
  { id: 'EMP-4109', name: 'Tobias Thorne', role: 'Platform Infrastructure Engineer', department: 'Platform Engineering', location: 'Berlin Hub', status: 'In probation', type: 'Probation', initials: 'TT' },
  { id: 'EMP-1822', name: 'Claire Dupond', role: 'Senior HR Business Partner', department: 'People & Culture', location: 'Paris Operations', status: 'Active', type: 'Permanent', initials: 'CD' },
];

const candidates = [
  { id: 'CAND-01', name: 'Trần Hoàng Minh', role: 'Senior Backend Engineer', stage: 'Applied', score: 92, skills: ['Go', 'Postgres', 'Kafka'], source: 'TopCV', initials: 'TM' },
  { id: 'CAND-02', name: 'Jessica Sterling', role: 'Principal Product Designer', stage: 'Applied', score: 86, skills: ['Figma', 'Design systems'], source: 'LinkedIn', initials: 'JS' },
  { id: 'CAND-03', name: 'Nguyễn Văn Đức', role: 'Lead Systems Architect', stage: 'AI screening', score: 96, skills: ['Kubernetes', 'AWS', 'Microservices'], source: 'Careers', initials: 'NĐ' },
  { id: 'CAND-04', name: 'Sophia Martinez', role: 'DevOps & Cloud Lead', stage: 'AI screening', score: 89, skills: ['Terraform', 'Docker', 'CI/CD'], source: 'Referral', initials: 'SM' },
  { id: 'CAND-05', name: 'Lê Quốc Huy', role: 'Senior Backend Engineer', stage: 'Technical interview', score: 94, skills: ['Go', 'Redis', 'gRPC'], source: 'Careers', initials: 'LH' },
  { id: 'CAND-06', name: 'Phạm Thanh Thảo', role: 'Principal Product Designer', stage: 'Executive round', score: 91, skills: ['UX research', 'Figma', 'Strategy'], source: 'LinkedIn', initials: 'PT' },
  { id: 'CAND-07', name: 'Đặng Tuấn Anh', role: 'Lead Systems Architect', stage: 'Offer', score: 98, skills: ['Cloud', 'Architecture', 'Python'], source: 'LinkedIn', initials: 'ĐA' },
  { id: 'CAND-08', name: 'Bùi Thùy Dung', role: 'Senior QA Automation Engineer', stage: 'Hired', score: 93, skills: ['Playwright', 'TypeScript', 'CI/CD'], source: 'TopCV', initials: 'BD' },
];

const jobs = [
  { code: 'REQ-2026-08', title: 'Lead Systems Architect', team: 'Platform Engineering', candidates: 18, target: 2, status: 'Active recruiting' },
  { code: 'REQ-2026-11', title: 'Senior Backend Engineer (Go)', team: 'Platform Engineering', candidates: 42, target: 4, status: 'Active recruiting' },
  { code: 'REQ-2026-04', title: 'Principal Product Designer', team: 'Experience Design', candidates: 21, target: 1, status: 'Active recruiting' },
  { code: 'REQ-2026-15', title: 'DevOps & Cloud Lead', team: 'Platform Engineering', candidates: 17, target: 2, status: 'Draft' },
];

const stages = ['Applied', 'AI screening', 'Technical interview', 'Executive round', 'Offer', 'Hired'];

function App() {
  const [page, setPage] = useState('employees');
  const [employeeQuery, setEmployeeQuery] = useState('');
  const [department, setDepartment] = useState('All departments');
  const [selectedEmployee, setSelectedEmployee] = useState(null);
  const [recruitmentTab, setRecruitmentTab] = useState('Pipeline');
  const [candidateQuery, setCandidateQuery] = useState('');
  const [selectedCandidate, setSelectedCandidate] = useState(null);
  const [showJobForm, setShowJobForm] = useState(false);
  const [jobNotice, setJobNotice] = useState('');

  const visibleEmployees = useMemo(() => employees.filter((employee) => {
    const matchesQuery = `${employee.name} ${employee.role} ${employee.id}`.toLowerCase().includes(employeeQuery.toLowerCase());
    return matchesQuery && (department === 'All departments' || employee.department === department);
  }), [employeeQuery, department]);

  const visibleCandidates = useMemo(() => candidates.filter((candidate) => (
    `${candidate.name} ${candidate.role} ${candidate.skills.join(' ')}`.toLowerCase().includes(candidateQuery.toLowerCase())
  )), [candidateQuery]);

  function saveJob(event) {
    event.preventDefault();
    setShowJobForm(false);
    setJobNotice('New requisition saved as a draft.');
    window.setTimeout(() => setJobNotice(''), 3500);
  }

  return (
    <div className="app-shell">
      <Sidebar page={page} setPage={setPage} />
      <main className="main-content">
        <Topbar page={page} onCreateJob={() => setShowJobForm(true)} />
        {page === 'employees' ? (
          <EmployeePage
            query={employeeQuery}
            onQueryChange={setEmployeeQuery}
            department={department}
            onDepartmentChange={setDepartment}
            employees={visibleEmployees}
            onSelect={setSelectedEmployee}
          />
        ) : (
          <RecruitmentPage
            tab={recruitmentTab}
            setTab={setRecruitmentTab}
            query={candidateQuery}
            onQueryChange={setCandidateQuery}
            candidates={visibleCandidates}
            onSelect={setSelectedCandidate}
            onCreateJob={() => setShowJobForm(true)}
          />
        )}
      </main>
      {selectedEmployee && <EmployeeDrawer employee={selectedEmployee} onClose={() => setSelectedEmployee(null)} />}
      {selectedCandidate && <CandidateDrawer candidate={selectedCandidate} onClose={() => setSelectedCandidate(null)} />}
      {showJobForm && <JobModal onClose={() => setShowJobForm(false)} onSubmit={saveJob} />}
      {jobNotice && <div className="toast" role="status">{jobNotice}</div>}
    </div>
  );
}

function Sidebar({ page, setPage }) {
  const navigation = [
    ['Overview', '⌂', null], ['Employee records', '◉', 'employees'], ['Teams & org chart', '⌘', null], ['Recruitment / ATS', '◌', 'recruitment'],
  ];
  return <aside className="sidebar">
    <div className="brand"><span className="brand-mark">N</span><div><strong>NexusHR</strong><small>Enterprise plan</small></div></div>
    <p className="nav-label">Workspace</p>
    <nav>{navigation.map(([label, icon, destination]) => <button type="button" key={label} className={page === destination ? 'nav-item active' : 'nav-item'} onClick={() => destination && setPage(destination)}><span>{icon}</span>{label}{label === 'Recruitment / ATS' && <b>14</b>}</button>)}</nav>
    <p className="nav-label">Management</p>
    <nav>{['Time & attendance', 'Leave & time off', 'Performance & goals', 'Payroll & compensation'].map((item) => <button type="button" className="nav-item" key={item}><span>○</span>{item}</button>)}</nav>
    <div className="sidebar-footer"><div><span className="status-dot" /> ATS AI engine active</div><div className="pool"><span>Talent pool</span><strong>342 candidates</strong><i><em /></i></div></div>
  </aside>;
}

function Topbar({ page, onCreateJob }) {
  return <header className="topbar"><div><span className="crumb">Workforce management</span><span className="crumb-separator">/</span><strong>{page === 'employees' ? 'Employee records' : 'Recruitment & ATS pipeline'}</strong></div><div className="topbar-actions"><button className="company">Acme Global Inc.⌄</button>{page === 'recruitment' && <button className="primary" onClick={onCreateJob}>＋ New requisition</button>}<button className="bell" aria-label="Notifications">♧<b>5</b></button><div className="user-avatar">SJ</div></div></header>;
}

function EmployeePage({ query, onQueryChange, department, onDepartmentChange, employees: visibleEmployees, onSelect }) {
  return <section className="page"><PageTitle eyebrow="Workforce management" title="Employee records & profile management" description="Keep people data, reporting structure, and contracts in one workspace." />
    <div className="metric-grid"><Metric label="Total headcount" value="248" detail="+12 this quarter" /><Metric label="Active employees" value="231" detail="93% of workforce" /><Metric label="Contracts to review" value="8" detail="3 expire this month" /></div>
    <section className="panel"><div className="panel-header"><div><h2>Employee directory</h2><p>Search and review employee records.</p></div><button className="secondary">⇩ Export CSV</button></div><div className="filters"><input value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search employees, roles, or ID…" /><select value={department} onChange={(event) => onDepartmentChange(event.target.value)}><option>All departments</option>{[...new Set(employees.map((employee) => employee.department))].map((name) => <option key={name}>{name}</option>)}</select></div><div className="table-wrap"><table><thead><tr><th>Employee</th><th>Department</th><th>Location</th><th>Employment</th><th>Status</th></tr></thead><tbody>{visibleEmployees.map((employee) => <tr key={employee.id} onClick={() => onSelect(employee)}><td><Person name={employee.name} initials={employee.initials} subtext={`${employee.role} · ${employee.id}`} /></td><td>{employee.department}</td><td>{employee.location}</td><td>{employee.type}</td><td><Status label={employee.status} /></td></tr>)}</tbody></table></div></section>
  </section>;
}

function RecruitmentPage({ tab, setTab, query, onQueryChange, candidates: visibleCandidates, onSelect, onCreateJob }) {
  return <section className="page"><PageTitle eyebrow="Talent acquisition" title="Recruitment & applicant tracking" description="Move candidates from sourcing to a signed offer with a shared pipeline." action={<button className="primary" onClick={onCreateJob}>＋ Post new job</button>} />
    <div className="metric-grid"><Metric label="Active requisitions" value="14" detail="42 headcount target" /><Metric label="Candidate pipeline" value="328" detail="46 added this week" /><Metric label="Offers in progress" value="6" detail="3 expire in 7 days" /></div>
    <div className="tabs">{['Pipeline', 'Jobs', 'Interviews', 'Offers'].map((item) => <button type="button" key={item} className={tab === item ? 'tab active' : 'tab'} onClick={() => setTab(item)}>{item}</button>)}</div>
    {tab === 'Pipeline' && <Pipeline query={query} onQueryChange={onQueryChange} candidates={visibleCandidates} onSelect={onSelect} />}
    {tab === 'Jobs' && <Jobs onCreateJob={onCreateJob} />}
    {tab === 'Interviews' && <Interviews />}
    {tab === 'Offers' && <Offers />}
  </section>;
}

function Pipeline({ query, onQueryChange, candidates: visibleCandidates, onSelect }) { return <section className="panel pipeline-panel"><div className="panel-header"><div><h2>Candidate pipeline</h2><p>Drag-and-drop stages are represented by the workflow columns.</p></div><input className="compact-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search candidates or skills…" /></div><div className="kanban">{stages.map((stage) => { const inStage = visibleCandidates.filter((candidate) => candidate.stage === stage); return <div className="kanban-column" key={stage}><div className="kanban-title"><span>{stage}</span><b>{inStage.length}</b></div>{inStage.length ? inStage.map((candidate) => <button type="button" className="candidate-card" key={candidate.id} onClick={() => onSelect(candidate)}><div className="candidate-card-top"><Avatar initials={candidate.initials} /><span className="score">{candidate.score}</span></div><strong>{candidate.name}</strong><small>{candidate.role}</small><div className="chips">{candidate.skills.slice(0, 2).map((skill) => <span key={skill}>{skill}</span>)}</div><footer>{candidate.source}<span>→</span></footer></button>) : <div className="empty-stage">No candidates</div>}</div>; })}</div></section>; }

function Jobs({ onCreateJob }) { return <section className="panel"><div className="panel-header"><div><h2>Open requisitions</h2><p>Roles currently in the recruitment workflow.</p></div><button className="primary" onClick={onCreateJob}>＋ New requisition</button></div><div className="table-wrap"><table><thead><tr><th>Requisition</th><th>Department</th><th>Pipeline</th><th>Headcount</th><th>Status</th></tr></thead><tbody>{jobs.map((job) => <tr key={job.code}><td><strong>{job.title}</strong><small>{job.code}</small></td><td>{job.team}</td><td>{job.candidates} candidates</td><td>{job.target}</td><td><Status label={job.status} /></td></tr>)}</tbody></table></div></section>; }
function Interviews() { return <section className="panel"><div className="panel-header"><div><h2>Upcoming interviews</h2><p>Scheduled interviews across active requisitions.</p></div><button className="secondary">＋ Schedule interview</button></div>{[['Today, 15:30', 'Lê Quốc Huy', 'Senior Backend Engineer', 'Google Meet'], ['Tomorrow, 10:00', 'Phạm Thanh Thảo', 'Principal Product Designer', 'Room 3B']].map(([time, person, role, place]) => <div className="schedule-row" key={person}><span className="calendar">▣</span><div><strong>{person}</strong><small>{role} · {place}</small></div><time>{time}</time><button className="secondary">View scorecard</button></div>)}</section>; }
function Offers() { return <section className="panel"><div className="panel-header"><div><h2>Offers in progress</h2><p>Track offer review and acceptance deadlines.</p></div></div>{[['Đặng Tuấn Anh', 'Lead Systems Architect', '85,000,000 VND', 'Expires in 3 days'], ['Bùi Thùy Dung', 'Senior QA Automation Engineer', '42,000,000 VND', 'Expires in 5 days']].map(([person, role, salary, expiry]) => <div className="schedule-row" key={person}><Avatar initials={person.slice(0, 2)} /><div><strong>{person}</strong><small>{role} · Base salary: {salary}</small></div><time>{expiry}</time><Status label="Awaiting signature" /></div>)}</section>; }

function PageTitle({ eyebrow, title, description, action }) { return <div className="page-title"><div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{action}</div>; }
function Metric({ label, value, detail }) { return <article className="metric"><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>; }
function Avatar({ initials }) { return <span className="avatar">{initials}</span>; }
function Person({ name, initials, subtext }) { return <div className="person"><Avatar initials={initials} /><div><strong>{name}</strong><small>{subtext}</small></div></div>; }
function Status({ label }) { return <span className={`status ${label.toLowerCase().replaceAll(' ', '-')}`}>{label}</span>; }
function Drawer({ title, children, onClose }) { return <div className="drawer-backdrop" role="presentation" onMouseDown={onClose}><aside className="drawer" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(event) => event.stopPropagation()}><button className="close" onClick={onClose} aria-label="Close">×</button>{children}</aside></div>; }
function EmployeeDrawer({ employee, onClose }) { return <Drawer title="Employee profile" onClose={onClose}><Avatar initials={employee.initials} /><span className="eyebrow">Employee profile</span><h2>{employee.name}</h2><p className="drawer-subtitle">{employee.role}</p><Status label={employee.status} /><dl><dt>Employee ID</dt><dd>{employee.id}</dd><dt>Department</dt><dd>{employee.department}</dd><dt>Location</dt><dd>{employee.location}</dd><dt>Employment type</dt><dd>{employee.type}</dd></dl><button className="primary full">Open full profile</button></Drawer>; }
function CandidateDrawer({ candidate, onClose }) { return <Drawer title="Candidate profile" onClose={onClose}><Avatar initials={candidate.initials} /><span className="eyebrow">Candidate profile</span><h2>{candidate.name}</h2><p className="drawer-subtitle">{candidate.role}</p><div className="candidate-score"><b>{candidate.score}</b><span>AI match score</span></div><h3>Skills</h3><div className="chips large">{candidate.skills.map((skill) => <span key={skill}>{skill}</span>)}</div><dl><dt>Current stage</dt><dd>{candidate.stage}</dd><dt>Source</dt><dd>{candidate.source}</dd><dt>Next action</dt><dd>Review with hiring team</dd></dl><button className="primary full">Advance candidate →</button></Drawer>; }
function JobModal({ onClose, onSubmit }) { return <div className="modal-backdrop" role="presentation"><form className="modal" onSubmit={onSubmit}><button type="button" className="close" onClick={onClose} aria-label="Close">×</button><span className="eyebrow">Recruitment</span><h2>Create a new requisition</h2><p>Save a draft to start a recruitment workflow.</p><label>Job title<input required placeholder="e.g. Senior Backend Engineer" /></label><label>Department<select defaultValue=""><option value="" disabled>Select a department</option><option>Platform Engineering</option><option>Experience Design</option><option>People & Culture</option></select></label><div className="form-row"><label>Headcount<input type="number" min="1" defaultValue="1" /></label><label>Employment type<select><option>Full-time</option><option>Part-time</option><option>Contract</option></select></label></div><div className="modal-actions"><button type="button" className="secondary" onClick={onClose}>Cancel</button><button className="primary" type="submit">Save draft</button></div></form></div>; }

export default App;
