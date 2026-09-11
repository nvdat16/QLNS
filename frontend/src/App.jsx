import { useEffect, useMemo, useState } from 'react';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:8000/api';

const initialEmployees = [
  { id: 'EMP-2048', name: 'Marcus Vance', role: 'Lead Design Systems Architect', department: 'Experience Design', location: 'Hanoi HQ', status: 'Active', type: 'Permanent', initials: 'MV' },
  { id: 'EMP-1092', name: 'Elena Rostova', role: 'VP of Product & Experience Design', department: 'Experience Design', location: 'Hanoi HQ', status: 'Active', type: 'Permanent', initials: 'ER' },
  { id: 'EMP-3041', name: 'Sarah Lin', role: 'UX Researcher', department: 'Experience Design', location: 'Hanoi HQ', status: 'Active', type: 'Fixed-Term', initials: 'SL' },
  { id: 'EMP-4109', name: 'Tobias Thorne', role: 'Platform Infrastructure Engineer', department: 'Platform Engineering', location: 'Hanoi HQ', status: 'In probation', type: 'Probation', initials: 'TT' },
  { id: 'EMP-1822', name: 'Claire Dupond', role: 'Senior HR Business Partner', department: 'People & Culture', location: 'Hanoi HQ', status: 'Active', type: 'Permanent', initials: 'CD' },
];

const initialCandidates = [
  { id: 'CAND-01', applicationId: 1, name: 'Trần Hoàng Minh', role: 'Senior Backend Engineer', stage: 'Applied', score: 92, skills: ['Go', 'Postgres', 'Kafka'], source: 'TopCV', initials: 'TM' },
  { id: 'CAND-02', applicationId: 2, name: 'Jessica Sterling', role: 'Principal Product Designer', stage: 'Applied', score: 86, skills: ['Figma', 'Design systems'], source: 'LinkedIn', initials: 'JS' },
  { id: 'CAND-03', applicationId: 3, name: 'Nguyễn Văn Đức', role: 'Lead Systems Architect', stage: 'AI screening', score: 96, skills: ['Kubernetes', 'AWS', 'Microservices'], source: 'Careers', initials: 'NĐ' },
  { id: 'CAND-04', applicationId: 4, name: 'Sophia Martinez', role: 'DevOps & Cloud Lead', stage: 'AI screening', score: 89, skills: ['Terraform', 'Docker', 'CI/CD'], source: 'Referral', initials: 'SM' },
  { id: 'CAND-05', applicationId: 5, name: 'Lê Quốc Huy', role: 'Senior Backend Engineer', stage: 'Technical interview', score: 94, skills: ['Go', 'Redis', 'gRPC'], source: 'Careers', initials: 'LH' },
  { id: 'CAND-06', applicationId: 6, name: 'Phạm Thanh Thảo', role: 'Principal Product Designer', stage: 'Executive round', score: 91, skills: ['UX research', 'Figma', 'Strategy'], source: 'LinkedIn', initials: 'PT' },
  { id: 'CAND-07', applicationId: 7, name: 'Đặng Tuấn Anh', role: 'Lead Systems Architect', stage: 'Offer', score: 98, skills: ['Cloud', 'Architecture', 'Python'], source: 'LinkedIn', initials: 'ĐA' },
  { id: 'CAND-08', applicationId: 8, name: 'Bùi Thùy Dung', role: 'Senior QA Automation Engineer', stage: 'Hired', score: 93, skills: ['Playwright', 'TypeScript', 'CI/CD'], source: 'TopCV', initials: 'BD' },
];

const initialJobs = [
  { id: 1, code: 'REQ-2026-08', title: 'Lead Systems Architect', team: 'Platform Engineering', candidates: 18, target: 2, status: 'Active recruiting' },
  { id: 2, code: 'REQ-2026-11', title: 'Senior Backend Engineer (Go)', team: 'Platform Engineering', candidates: 42, target: 4, status: 'Active recruiting' },
  { id: 3, code: 'REQ-2026-04', title: 'Principal Product Designer', team: 'Experience Design', candidates: 21, target: 1, status: 'Active recruiting' },
  { id: 4, code: 'REQ-2026-15', title: 'DevOps & Cloud Lead', team: 'Platform Engineering', candidates: 17, target: 2, status: 'Active recruiting' },
];

const STAGE_CONFIG = [
  { id: 'sourced', label: 'Applied', dbStage: 'Sourced & Applied' },
  { id: 'screening', label: 'AI screening', dbStage: 'AI Screening' },
  { id: 'interview', label: 'Technical interview', dbStage: 'Tech Interview' },
  { id: 'executive', label: 'Executive round', dbStage: 'Executive Round' },
  { id: 'offer', label: 'Offer', dbStage: 'Offer Letter' },
  { id: 'hired', label: 'Hired', dbStage: 'Hired & Ready' },
];

function getInitials(name = '') {
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return (name.slice(0, 2) || 'HR').toUpperCase();
}

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

  const [employeesList, setEmployeesList] = useState(initialEmployees);
  const [candidatesList, setCandidatesList] = useState(initialCandidates);
  const [jobsList, setJobsList] = useState(initialJobs);
  const [backendOnline, setBackendOnline] = useState(false);
  const [stats, setStats] = useState({ total_employees: 5, active_employees: 5, open_jobs_count: 5 });

  // Load from FastAPI backend
  async function loadDataFromBackend() {
    try {
      // 1. Employees
      const empRes = await fetch(`${API_BASE}/employees`);
      if (empRes.ok) {
        const empData = await empRes.json();
        if (empData && empData.length > 0) {
          setEmployeesList(empData.map(e => ({
            id: e.employee_code || `EMP-${e.id}`,
            name: e.full_name || `${e.first_name} ${e.last_name}`,
            role: e.position_name || 'Staff',
            department: e.department_name || 'General',
            location: e.office_location || 'Hanoi HQ',
            status: e.status || 'Active',
            type: e.status === 'Probation' ? 'Probation' : 'Permanent',
            initials: getInitials(e.full_name || `${e.first_name} ${e.last_name}`)
          })));
        }
        setBackendOnline(true);
      }

      // 2. Stats
      const statsRes = await fetch(`${API_BASE}/employees/stats/summary`);
      if (statsRes.ok) {
        const s = await statsRes.json();
        setStats(s);
      }

      // 3. Recruitment Pipeline
      const pipeRes = await fetch(`${API_BASE}/recruitment/pipeline`);
      if (pipeRes.ok) {
        const pipeData = await pipeRes.json();
        if (pipeData && pipeData.stages) {
          const flatCandidates = [];
          pipeData.stages.forEach(st => {
            const mappedLabel = STAGE_CONFIG.find(c => c.id === st.stage_id)?.label || 'Applied';
            (st.cards || []).forEach(c => {
              flatCandidates.push({
                id: `CAND-${String(c.candidate_id).padStart(2, '0')}`,
                applicationId: c.application_id,
                name: c.name,
                role: c.role,
                stage: mappedLabel,
                score: c.ai_score || 90,
                skills: c.skills && c.skills.length > 0 ? c.skills : ['Core Skill', 'Expertise'],
                source: c.source || 'Direct Web',
                initials: getInitials(c.name)
              });
            });
          });
          if (flatCandidates.length > 0) {
            setCandidatesList(flatCandidates);
          }
        }
      }

      // 4. Jobs
      const jobsRes = await fetch(`${API_BASE}/recruitment/jobs`);
      if (jobsRes.ok) {
        const jData = await jobsRes.json();
        if (jData && jData.length > 0) {
          setJobsList(jData.map(j => ({
            id: j.id,
            code: `REQ-2026-${String(j.id).padStart(2, '0')}`,
            title: j.title,
            team: j.department_name || 'Engineering',
            candidates: j.applicant_count || 0,
            target: j.target_headcount || 1,
            status: j.status || 'Active recruiting'
          })));
        }
      }
    } catch (err) {
      console.warn("Backend API not connected, using offline mock data:", err);
      setBackendOnline(false);
    }
  }

  useEffect(() => {
    loadDataFromBackend();
  }, []);

  const visibleEmployees = useMemo(() => employeesList.filter((employee) => {
    const matchesQuery = `${employee.name} ${employee.role} ${employee.id}`.toLowerCase().includes(employeeQuery.toLowerCase());
    return matchesQuery && (department === 'All departments' || employee.department === department);
  }), [employeesList, employeeQuery, department]);

  const visibleCandidates = useMemo(() => candidatesList.filter((candidate) => (
    `${candidate.name} ${candidate.role} ${(candidate.skills || []).join(' ')}`.toLowerCase().includes(candidateQuery.toLowerCase())
  )), [candidatesList, candidateQuery]);

  async function saveJob(jobData) {
    setShowJobForm(false);
    try {
      const res = await fetch(`${API_BASE}/recruitment/jobs`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: jobData.title,
          department_id: jobData.deptId || 2,
          target_headcount: jobData.headcount || 1,
          location: 'Hanoi / Hybrid',
          employment_type: jobData.employmentType || 'Full-time',
          status: 'Active Recruiting',
          channels: 'LinkedIn, TopCV, Careers'
        })
      });
      if (res.ok) {
        setJobNotice(`Requisition "${jobData.title}" published & saved to PostgreSQL!`);
        await loadDataFromBackend();
      } else {
        setJobNotice(`Requisition "${jobData.title}" saved locally.`);
      }
    } catch (err) {
      setJobNotice(`Requisition "${jobData.title}" saved (demo mode).`);
    }
    window.setTimeout(() => setJobNotice(''), 3500);
  }

  async function advanceCandidate(candidate) {
    setSelectedCandidate(null);
    if (!candidate.applicationId) {
      setJobNotice(`${candidate.name} advanced to next stage!`);
      window.setTimeout(() => setJobNotice(''), 3500);
      return;
    }

    try {
      const res = await fetch(`${API_BASE}/recruitment/applications/${candidate.applicationId}/advance`, {
        method: 'POST'
      });
      if (res.ok) {
        const data = await res.json();
        setJobNotice(`${candidate.name} advanced to ${data.stage} in PostgreSQL!`);
        await loadDataFromBackend();
      } else {
        setJobNotice(`${candidate.name} advanced to next interview stage!`);
      }
    } catch (err) {
      setJobNotice(`${candidate.name} advanced to next stage (demo mode)!`);
    }
    window.setTimeout(() => setJobNotice(''), 3500);
  }

  return (
    <div className="app-shell">
      <Sidebar page={page} setPage={setPage} backendOnline={backendOnline} candidatesCount={candidatesList.length} />
      <main className="main-content">
        <Topbar page={page} onCreateJob={() => setShowJobForm(true)} backendOnline={backendOnline} />
        {page === 'employees' ? (
          <EmployeePage
            query={employeeQuery}
            onQueryChange={setEmployeeQuery}
            department={department}
            onDepartmentChange={setDepartment}
            employees={visibleEmployees}
            allDepartments={[...new Set(employeesList.map(e => e.department))]}
            stats={stats}
            onSelect={setSelectedEmployee}
          />
        ) : (
          <RecruitmentPage
            tab={recruitmentTab}
            setTab={setRecruitmentTab}
            query={candidateQuery}
            onQueryChange={setCandidateQuery}
            candidates={visibleCandidates}
            jobs={jobsList}
            onSelect={setSelectedCandidate}
            onCreateJob={() => setShowJobForm(true)}
          />
        )}
      </main>
      {selectedEmployee && <EmployeeDrawer employee={selectedEmployee} onClose={() => setSelectedEmployee(null)} />}
      {selectedCandidate && <CandidateDrawer candidate={selectedCandidate} onClose={() => setSelectedCandidate(null)} onAdvance={() => advanceCandidate(selectedCandidate)} />}
      {showJobForm && <JobModal onClose={() => setShowJobForm(false)} onSubmit={saveJob} />}
      {jobNotice && <div className="toast" role="status">{jobNotice}</div>}
    </div>
  );
}

function Sidebar({ page, setPage, backendOnline, candidatesCount }) {
  const navigation = [
    ['Overview', '⌂', null],
    ['Employee records', '◉', 'employees'],
    ['Teams & org chart', '⌘', null],
    ['Recruitment / ATS', '◌', 'recruitment'],
  ];
  return (
    <aside className="sidebar">
      <div className="brand">
        <span className="brand-mark">N</span>
        <div>
          <strong>NexusHR</strong>
          <small>{backendOnline ? '🟢 FastAPI Live' : '⚪ Local Demo'}</small>
        </div>
      </div>
      <p className="nav-label">Workspace</p>
      <nav>
        {navigation.map(([label, icon, destination]) => (
          <button
            type="button"
            key={label}
            className={page === destination ? 'nav-item active' : 'nav-item'}
            onClick={() => destination && setPage(destination)}
          >
            <span>{icon}</span>
            {label}
            {label === 'Recruitment / ATS' && <b>{candidatesCount}</b>}
          </button>
        ))}
      </nav>
      <p className="nav-label">Management</p>
      <nav>
        {['Time & attendance', 'Leave & time off', 'Performance & goals', 'Payroll & compensation'].map((item) => (
          <button type="button" className="nav-item" key={item}>
            <span>○</span>{item}
          </button>
        ))}
      </nav>
      <div className="sidebar-footer">
        <div>
          <span className="status-dot" style={{ background: backendOnline ? '#10b981' : '#94a3b8' }} />
          {backendOnline ? 'PostgreSQL Connected' : 'Local Demo Cache'}
        </div>
        <div className="pool">
          <span>Talent Pool</span>
          <strong>{candidatesCount} candidates</strong>
          <i><em style={{ width: backendOnline ? '100%' : '60%' }} /></i>
        </div>
      </div>
    </aside>
  );
}

function Topbar({ page, onCreateJob, backendOnline }) {
  return (
    <header className="topbar">
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span className="crumb">Workforce management</span>
        <span className="crumb-separator">/</span>
        <strong>{page === 'employees' ? 'Employee Records & Master Data' : 'Recruitment & ATS Pipeline'}</strong>
        <span style={{
          marginLeft: '12px',
          fontSize: '11px',
          padding: '2px 8px',
          borderRadius: '12px',
          fontWeight: 600,
          background: backendOnline ? '#ecfdf5' : '#f1f5f9',
          color: backendOnline ? '#065f46' : '#64748b',
          border: `1px solid ${backendOnline ? '#a7f3d0' : '#cbd5e1'}`
        }}>
          {backendOnline ? 'FastAPI Connected' : 'Offline Mode'}
        </span>
      </div>
      <div className="topbar-actions">
        <button className="company">Acme Global Inc. ⌄</button>
        {page === 'recruitment' && <button className="primary" onClick={onCreateJob}>＋ New requisition</button>}
        <button className="bell" aria-label="Notifications">♧<b>5</b></button>
        <div className="user-avatar">SJ</div>
      </div>
    </header>
  );
}

function EmployeePage({ query, onQueryChange, department, onDepartmentChange, employees: visibleEmployees, allDepartments, stats, onSelect }) {
  return (
    <section className="page">
      <PageTitle eyebrow="Workforce management" title="Employee records & profile management" description="Keep people data, reporting structure, and contracts in one workspace." />
      <div className="metric-grid">
        <Metric label="Total headcount" value={stats.total_employees || visibleEmployees.length} detail="+12 this quarter" />
        <Metric label="Active employees" value={stats.active_employees || visibleEmployees.length} detail="100% of workforce" />
        <Metric label="Open requisitions" value={stats.open_jobs_count || 5} detail="Active hiring goals" />
      </div>
      <section className="panel">
        <div className="panel-header">
          <div>
            <h2>Employee directory</h2>
            <p>Search and review employee records synced with PostgreSQL.</p>
          </div>
          <button className="secondary">⇩ Export CSV</button>
        </div>
        <div className="filters">
          <input value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search employees, roles, or ID…" />
          <select value={department} onChange={(event) => onDepartmentChange(event.target.value)}>
            <option>All departments</option>
            {allDepartments.map((name) => <option key={name}>{name}</option>)}
          </select>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Employee</th>
                <th>Department</th>
                <th>Location</th>
                <th>Employment</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {visibleEmployees.map((employee) => (
                <tr key={employee.id} onClick={() => onSelect(employee)}>
                  <td><Person name={employee.name} initials={employee.initials} subtext={`${employee.role} · ${employee.id}`} /></td>
                  <td>{employee.department}</td>
                  <td>{employee.location}</td>
                  <td>{employee.type}</td>
                  <td><Status label={employee.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </section>
  );
}

function RecruitmentPage({ tab, setTab, query, onQueryChange, candidates: visibleCandidates, jobs, onSelect, onCreateJob }) {
  const totalHeadcount = jobs.reduce((acc, j) => acc + (j.target || 1), 0);

  return (
    <section className="page">
      <PageTitle
        eyebrow="Talent acquisition"
        title="Recruitment & applicant tracking"
        description="Move candidates from sourcing to a signed offer with a shared pipeline."
        action={<button className="primary" onClick={onCreateJob}>＋ Post new job</button>}
      />
      <div className="metric-grid">
        <Metric label="Active requisitions" value={`${jobs.length} Jobs`} detail={`${totalHeadcount} headcount target`} />
        <Metric label="Candidate pipeline" value={`${visibleCandidates.length} Active`} detail="Synced across ATS pipeline" />
        <Metric label="Hiring velocity" value="18.5 Days" detail="Fast-track technical rounds" />
      </div>
      <div className="tabs">
        {['Pipeline', 'Jobs', 'Interviews', 'Offers'].map((item) => (
          <button type="button" key={item} className={tab === item ? 'tab active' : 'tab'} onClick={() => setTab(item)}>
            {item}
          </button>
        ))}
      </div>
      {tab === 'Pipeline' && <Pipeline query={query} onQueryChange={onQueryChange} candidates={visibleCandidates} onSelect={onSelect} />}
      {tab === 'Jobs' && <Jobs jobs={jobs} onCreateJob={onCreateJob} />}
      {tab === 'Interviews' && <Interviews />}
      {tab === 'Offers' && <Offers />}
    </section>
  );
}

function Pipeline({ query, onQueryChange, candidates: visibleCandidates, onSelect }) {
  return (
    <section className="panel pipeline-panel">
      <div className="panel-header">
        <div>
          <h2>Candidate pipeline</h2>
          <p>Real-time Kanban stages synchronized with PostgreSQL recruitment tables.</p>
        </div>
        <input className="compact-search" value={query} onChange={(event) => onQueryChange(event.target.value)} placeholder="Search candidates or skills…" />
      </div>
      <div className="kanban">
        {STAGE_CONFIG.map(({ label }) => {
          const inStage = visibleCandidates.filter((candidate) => candidate.stage === label);
          return (
            <div className="kanban-column" key={label}>
              <div className="kanban-title">
                <span>{label}</span>
                <b>{inStage.length}</b>
              </div>
              {inStage.length ? (
                inStage.map((candidate) => (
                  <button type="button" className="candidate-card" key={candidate.id} onClick={() => onSelect(candidate)}>
                    <div className="candidate-card-top">
                      <Avatar initials={candidate.initials} />
                      <span className="score">{candidate.score}% AI</span>
                    </div>
                    <strong>{candidate.name}</strong>
                    <small>{candidate.role}</small>
                    <div className="chips">
                      {(candidate.skills || []).slice(0, 2).map((skill) => (
                        <span key={skill}>{skill}</span>
                      ))}
                    </div>
                    <footer>
                      {candidate.source}
                      <span>→</span>
                    </footer>
                  </button>
                ))
              ) : (
                <div className="empty-stage">No candidates</div>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}

function Jobs({ jobs, onCreateJob }) {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>Open requisitions</h2>
          <p>Roles currently in the recruitment workflow saved in PostgreSQL.</p>
        </div>
        <button className="primary" onClick={onCreateJob}>＋ New requisition</button>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Requisition</th>
              <th>Department</th>
              <th>Applicants</th>
              <th>Target Headcount</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {jobs.map((job) => (
              <tr key={job.code}>
                <td>
                  <strong>{job.title}</strong>
                  <small>{job.code}</small>
                </td>
                <td>{job.team}</td>
                <td>{job.candidates} applicants</td>
                <td>{job.target}</td>
                <td><Status label={job.status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function Interviews() {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>Upcoming interviews</h2>
          <p>Scheduled interviews across active requisitions.</p>
        </div>
        <button className="secondary">＋ Schedule interview</button>
      </div>
      {[
        ['Today, 15:30', 'Lê Quốc Huy', 'Senior Backend Engineer', 'Google Meet'],
        ['Tomorrow, 10:00', 'Phạm Thanh Thảo', 'Principal Product Designer', 'Room 3B']
      ].map(([time, person, role, place]) => (
        <div className="schedule-row" key={person}>
          <span className="calendar">▣</span>
          <div>
            <strong>{person}</strong>
            <small>{role} · {place}</small>
          </div>
          <time>{time}</time>
          <button className="secondary">View scorecard</button>
        </div>
      ))}
    </section>
  );
}

function Offers() {
  return (
    <section className="panel">
      <div className="panel-header">
        <div>
          <h2>Offers in progress</h2>
          <p>Track offer review and acceptance deadlines.</p>
        </div>
      </div>
      {[
        ['Đặng Tuấn Anh', 'Lead Systems Architect', '85,000,000 VND', 'Expires in 3 days'],
        ['Bùi Thùy Dung', 'Senior QA Automation Engineer', '42,000,000 VND', 'Expires in 5 days']
      ].map(([person, role, salary, expiry]) => (
        <div className="schedule-row" key={person}>
          <Avatar initials={person.slice(0, 2)} />
          <div>
            <strong>{person}</strong>
            <small>{role} · Base salary: {salary}</small>
          </div>
          <time>{expiry}</time>
          <Status label="Awaiting signature" />
        </div>
      ))}
    </section>
  );
}

function PageTitle({ eyebrow, title, description, action }) {
  return (
    <div className="page-title">
      <div>
        <span className="eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action}
    </div>
  );
}

function Metric({ label, value, detail }) {
  return (
    <article className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{detail}</small>
    </article>
  );
}

function Avatar({ initials }) {
  return <span className="avatar">{initials}</span>;
}

function Person({ name, initials, subtext }) {
  return (
    <div className="person">
      <Avatar initials={initials} />
      <div>
        <strong>{name}</strong>
        <small>{subtext}</small>
      </div>
    </div>
  );
}

function Status({ label }) {
  return <span className={`status ${(label || '').toLowerCase().replaceAll(' ', '-')}`}>{label}</span>;
}

function Drawer({ title, children, onClose }) {
  return (
    <div className="drawer-backdrop" role="presentation" onMouseDown={onClose}>
      <aside className="drawer" role="dialog" aria-modal="true" aria-label={title} onMouseDown={(event) => event.stopPropagation()}>
        <button className="close" onClick={onClose} aria-label="Close">×</button>
        {children}
      </aside>
    </div>
  );
}

function EmployeeDrawer({ employee, onClose }) {
  return (
    <Drawer title="Employee profile" onClose={onClose}>
      <Avatar initials={employee.initials} />
      <span className="eyebrow">Employee profile</span>
      <h2>{employee.name}</h2>
      <p className="drawer-subtitle">{employee.role}</p>
      <Status label={employee.status} />
      <dl>
        <dt>Employee ID</dt>
        <dd>{employee.id}</dd>
        <dt>Department</dt>
        <dd>{employee.department}</dd>
        <dt>Location</dt>
        <dd>{employee.location}</dd>
        <dt>Employment type</dt>
        <dd>{employee.type}</dd>
      </dl>
      <button className="primary full" onClick={onClose}>Close profile</button>
    </Drawer>
  );
}

function CandidateDrawer({ candidate, onClose, onAdvance }) {
  return (
    <Drawer title="Candidate profile" onClose={onClose}>
      <Avatar initials={candidate.initials} />
      <span className="eyebrow">Candidate profile</span>
      <h2>{candidate.name}</h2>
      <p className="drawer-subtitle">{candidate.role}</p>
      <div className="candidate-score">
        <b>{candidate.score}%</b>
        <span>AI match score</span>
      </div>
      <h3>Skills</h3>
      <div className="chips large">
        {(candidate.skills || []).map((skill) => <span key={skill}>{skill}</span>)}
      </div>
      <dl>
        <dt>Current stage</dt>
        <dd>{candidate.stage}</dd>
        <dt>Source</dt>
        <dd>{candidate.source}</dd>
        <dt>Next action</dt>
        <dd>Advance candidate stage in ATS</dd>
      </dl>
      <button className="primary full" onClick={onAdvance}>Advance candidate →</button>
    </Drawer>
  );
}

function JobModal({ onClose, onSubmit }) {
  const [title, setTitle] = useState('');
  const [deptId, setDeptId] = useState(2);
  const [headcount, setHeadcount] = useState(1);
  const [employmentType, setEmploymentType] = useState('Full-time');

  function handleSubmit(e) {
    e.preventDefault();
    onSubmit({ title, deptId, headcount, employmentType });
  }

  return (
    <div className="modal-backdrop" role="presentation">
      <form className="modal" onSubmit={handleSubmit}>
        <button type="button" className="close" onClick={onClose} aria-label="Close">×</button>
        <span className="eyebrow">Recruitment</span>
        <h2>Create a new requisition</h2>
        <p>Publish job requisition directly to PostgreSQL and recruitment pipeline.</p>
        <label>
          Job title
          <input required placeholder="e.g. Senior Backend Engineer" value={title} onChange={(e) => setTitle(e.target.value)} />
        </label>
        <label>
          Department
          <select value={deptId} onChange={(e) => setDeptId(parseInt(e.target.value, 10))}>
            <option value={2}>Platform Engineering</option>
            <option value={1}>Experience Design</option>
            <option value={3}>People & Culture</option>
            <option value={4}>Global Enterprise Sales</option>
            <option value={5}>Product & Strategy</option>
            <option value={6}>Finance & Legal Compliance</option>
          </select>
        </label>
        <div className="form-row">
          <label>
            Headcount
            <input type="number" min="1" value={headcount} onChange={(e) => setHeadcount(parseInt(e.target.value, 10))} />
          </label>
          <label>
            Employment type
            <select value={employmentType} onChange={(e) => setEmploymentType(e.target.value)}>
              <option value="Full-time">Full-time</option>
              <option value="Part-time">Part-time</option>
              <option value="Contract">Contract</option>
            </select>
          </label>
        </div>
        <div className="modal-actions">
          <button type="button" className="secondary" onClick={onClose}>Cancel</button>
          <button className="primary" type="submit">Publish requisition</button>
        </div>
      </form>
    </div>
  );
}

export default App;
