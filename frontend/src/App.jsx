import { useEffect, useMemo, useState } from 'react';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:8000/api';

const employees = [
  { id: 'NV-2023-089', name: 'Nguyễn Văn Hùng', initials: 'NH', title: 'Senior Tech Lead', department: 'Kỹ thuật & Công nghệ', email: 'hung.nguyen@nexushr.vn', contract: 'Không xác định thời hạn', status: 'Đang làm việc', joined: '15/03/2023' },
  { id: 'NV-2022-142', name: 'Trần Thị Mai', initials: 'TM', title: 'HR Business Partner', department: 'Nhân sự & Đào tạo', email: 'mai.tran@nexushr.vn', contract: 'Xác định thời hạn', status: 'Đang làm việc', joined: '01/08/2022' },
  { id: 'NV-2021-003', name: 'Lê Hoàng Nam', initials: 'LN', title: 'Head of Finance', department: 'Tài chính & Kế toán', email: 'nam.le@nexushr.vn', contract: 'Không xác định thời hạn', status: 'Đang làm việc', joined: '12/01/2021' },
  { id: 'NV-2024-001', name: 'Phạm Thanh Sơn', initials: 'PS', title: 'Backend Engineer', department: 'Kỹ thuật & Công nghệ', email: 'son.pham@nexushr.vn', contract: 'Thử việc', status: 'Thử việc', joined: '02/09/2024' },
  { id: 'NV-2023-112', name: 'Nguyễn Hồng Đào', initials: 'NĐ', title: 'Product Designer', department: 'Sản phẩm & Trải nghiệm', email: 'dao.nguyen@nexushr.vn', contract: 'Xác định thời hạn', status: 'Đang làm việc', joined: '20/06/2023' },
];

const candidates = [
  { id: 1, name: 'Nguyễn Minh Anh', initials: 'MA', role: 'Senior Backend Engineer', stage: 'Ứng tuyển', score: 94, tags: ['Go', 'PostgreSQL'], source: 'TopCV' },
  { id: 2, name: 'Trần Quốc Bảo', initials: 'QB', role: 'Product Designer', stage: 'Ứng tuyển', score: 88, tags: ['Figma', 'UX'], source: 'LinkedIn' },
  { id: 3, name: 'Lê Gia Huy', initials: 'GH', role: 'DevOps Engineer', stage: 'Sàng lọc AI', score: 91, tags: ['AWS', 'Kubernetes'], source: 'Referral' },
  { id: 4, name: 'Phạm Thu Hà', initials: 'TH', role: 'Senior Backend Engineer', stage: 'Phỏng vấn kỹ thuật', score: 96, tags: ['Go', 'Redis'], source: 'TopCV' },
  { id: 5, name: 'Đỗ Anh Khoa', initials: 'AK', role: 'Product Designer', stage: 'Phỏng vấn vòng cuối', score: 90, tags: ['Research', 'Figma'], source: 'Careers' },
  { id: 6, name: 'Bùi Khánh Linh', initials: 'KL', role: 'QA Automation Engineer', stage: 'Đề nghị tuyển dụng', score: 93, tags: ['Playwright', 'CI/CD'], source: 'LinkedIn' },
];

const stages = ['Ứng tuyển', 'Sàng lọc AI', 'Phỏng vấn kỹ thuật', 'Phỏng vấn vòng cuối', 'Đề nghị tuyển dụng', 'Đã tuyển'];
const departments = ['Kỹ thuật & Công nghệ', 'Kinh doanh & Tiếp thị', 'Nhân sự & Đào tạo', 'Tài chính & Kế toán', 'Vận hành & Hỗ trợ', 'Sản phẩm & Trải nghiệm'];

function App() {
  const [section, setSection] = useState('profiles');
  const [employeeView, setEmployeeView] = useState('profiles');
  const [recruitmentView, setRecruitmentView] = useState('pipeline');
  const [attendanceView, setAttendanceView] = useState('timesheet');
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [toast, setToast] = useState('');
  const [mobileNav, setMobileNav] = useState(false);
  const [employeeRows, setEmployeeRows] = useState(employees);
  const [candidateRows, setCandidateRows] = useState(candidates);

  useEffect(() => {
    async function loadLiveData() {
      try {
        const [employeeResponse, pipelineResponse] = await Promise.all([
          fetch(`${API_BASE}/employees`),
          fetch(`${API_BASE}/recruitment/pipeline`),
        ]);
        if (employeeResponse.ok) {
          const data = await employeeResponse.json();
          if (data.length) setEmployeeRows(data.map((employee) => ({
            id: employee.employee_code || `NV-${employee.id}`,
            name: employee.full_name || `${employee.first_name || ''} ${employee.last_name || ''}`.trim(),
            initials: initials(employee.full_name || `${employee.first_name || ''} ${employee.last_name || ''}`),
            title: employee.position_name || 'Nhân viên',
            department: employee.department_name || 'Chưa phân phòng ban',
            email: employee.email || 'Chưa cập nhật',
            contract: employee.contract_type || 'Chưa cập nhật',
            status: employee.status || 'Đang làm việc',
            joined: employee.join_date || 'Chưa cập nhật',
          })));
        }
        if (pipelineResponse.ok) {
          const data = await pipelineResponse.json();
          const liveCandidates = (data.stages || []).flatMap((stage) => (stage.cards || []).map((candidate) => ({
            id: candidate.candidate_id,
            name: candidate.name,
            initials: initials(candidate.name),
            role: candidate.role || 'Ứng viên',
            stage: stageLabel(stage.stage_id),
            score: candidate.ai_score || 0,
            tags: candidate.skills?.length ? candidate.skills : ['Chưa cập nhật'],
            source: candidate.source || 'Direct Web',
          })));
          if (liveCandidates.length) setCandidateRows(liveCandidates);
        }
      } catch {
        // Prototype data remains available when the API is not running.
      }
    }
    loadLiveData();
  }, []);

  const employeeResults = useMemo(() => employeeRows.filter((item) => `${item.name} ${item.id} ${item.department} ${item.title}`.toLowerCase().includes(query.toLowerCase())), [employeeRows, query]);
  const candidateResults = useMemo(() => candidateRows.filter((item) => `${item.name} ${item.role} ${item.tags.join(' ')}`.toLowerCase().includes(query.toLowerCase())), [candidateRows, query]);
  const notify = (message) => { setToast(message); window.setTimeout(() => setToast(''), 2800); };
  const go = (next) => { setSection(next); setQuery(''); setMobileNav(false); if (next === 'profiles') setEmployeeView('profiles'); };
  const title = section === 'recruitment' ? 'Quản lý tuyển dụng' : section === 'attendance' ? 'Chấm công & Nghỉ phép' : employeeView === 'contracts' ? 'Quản lý hợp đồng' : employeeView === 'org' ? 'Sơ đồ tổ chức' : 'Hồ sơ nhân viên';

  return <div className="app-shell">
    {mobileNav && <button className="nav-backdrop" aria-label="Đóng menu" onClick={() => setMobileNav(false)} />}
    <Sidebar section={section} employeeView={employeeView} go={go} setEmployeeView={setEmployeeView} mobileNav={mobileNav} />
    <main className="workspace">
      <header className="topbar">
        <div className="topbar-title"><button className="mobile-menu" onClick={() => setMobileNav(true)} aria-label="Mở menu">☰</button><div><span>HR Management /</span><strong>{title}</strong></div></div>
        <div className="top-actions"><label className="global-search"><span>⌕</span><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Tìm kiếm nhanh..." /></label><button className="icon-button" aria-label="Thông báo">♢<b>3</b></button><button className="help-button">?</button><div className="top-avatar">LT</div></div>
      </header>
      <div className="page-content">
        {section === 'profiles' && <PeopleArea view={employeeView} setView={setEmployeeView} rows={employeeResults} onOpen={(item) => { setSelected(item); setDrawerOpen(true); }} notify={notify} />}
        {section === 'recruitment' && <RecruitmentArea view={recruitmentView} setView={setRecruitmentView} rows={candidateResults} onOpen={(item) => { setSelected(item); setDrawerOpen(true); }} notify={notify} />}
        {section === 'attendance' && <AttendanceArea view={attendanceView} setView={setAttendanceView} notify={notify} />}
      </div>
    </main>
    {drawerOpen && <DetailDrawer item={selected} kind={section} onClose={() => setDrawerOpen(false)} notify={notify} />}
    {toast && <div className="toast" role="status">✓ {toast}</div>}
  </div>;
}

function initials(name = '') {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return parts.length > 1 ? `${parts[0][0]}${parts.at(-1)[0]}`.toUpperCase() : (name.slice(0, 2) || 'HR').toUpperCase();
}

function stageLabel(id) {
  return ({ sourced: 'Ứng tuyển', screening: 'Sàng lọc AI', interview: 'Phỏng vấn kỹ thuật', executive: 'Phỏng vấn vòng cuối', offer: 'Đề nghị tuyển dụng', hired: 'Đã tuyển' })[id] || 'Ứng tuyển';
}

function Sidebar({ section, employeeView, go, setEmployeeView, mobileNav }) {
  const profileNav = (view, icon, label) => <button className={`nav-item ${section === 'profiles' && employeeView === view ? 'active' : ''}`} onClick={() => { go('profiles'); setEmployeeView(view); }}><span>{icon}</span>{label}</button>;
  return <aside className={`sidebar ${mobileNav ? 'open' : ''}`}>
    <div className="brand"><div className="brand-icon">▣</div><div><strong>Hệ Thống Doanh Nghiệp</strong><small>NexusHR Enterprise</small></div></div>
    <nav>
      {profileNav('profiles', '♙', 'Hồ sơ nhân viên')}
      {profileNav('org', '⌘', 'Sơ đồ tổ chức')}
      {profileNav('contracts', '▤', 'Quản lý hợp đồng')}
      <button className={`nav-item ${section === 'recruitment' ? 'active' : ''}`} onClick={() => go('recruitment')}><span>⌕</span>Quản lý tuyển dụng <em>ATS</em></button>
      <button className={`nav-item ${section === 'attendance' ? 'active' : ''}`} onClick={() => go('attendance')}><span>◷</span>Chấm công & Nghỉ phép</button>
    </nav>
    <div className="sidebar-user"><div className="avatar">LT</div><div><strong>Lê Minh Trí</strong><small>Trưởng phòng Nhân sự</small></div></div>
  </aside>;
}

function PeopleArea({ view, setView, rows, onOpen, notify }) {
  if (view === 'org') return <Organization />;
  if (view === 'contracts') return <Contracts rows={rows} onOpen={onOpen} notify={notify} />;
  return <>
    <PageHeading title="Hồ sơ nhân viên" description="Quản lý thông tin, vị trí công việc và vòng đời nhân sự." action={<button className="primary" onClick={() => notify('Đã mở biểu mẫu thêm nhân viên')}>＋ Thêm nhân viên</button>} />
    <MetricGrid metrics={[['Tổng nhân viên', '312', '+8 mới trong tháng'], ['Đang làm việc', '284', '91% tổng số'], ['Thử việc (30–60 ngày)', '18', 'Cần lưu ý'], ['Đã nghỉ việc / Tạm hoãn', '10', 'Đã xử lý']]} />
    <section className="panel"><Toolbar placeholder="Tìm nhân viên theo tên, mã NV..." /><EmployeeTable rows={rows} onOpen={onOpen} /></section>
  </>;
}

function Organization() { return <>
  <PageHeading title="Sơ đồ tổ chức" description="Cấu trúc phòng ban và số lượng nhân sự theo đơn vị." action={<button className="secondary">⇩ Xuất sơ đồ</button>} />
  <section className="org-summary"><div className="company-node"><span>▣</span><div><strong>NexusHR Enterprise</strong><small>312 nhân sự · Hà Nội</small></div></div><div className="org-line" /></section>
  <section className="department-grid">{departments.map((name, index) => <article className="department-card" key={name}><div className={`department-icon tone-${index}`} >{['⌘', '◉', '♙', '₫', '◌', '✦'][index]}</div><div><h3>{name}</h3><p>{[86, 52, 31, 24, 48, 71][index]} nhân sự</p></div><button aria-label={`Xem ${name}`}>→</button><footer><span>Trưởng phòng</span><strong>{['Nguyễn Văn Hùng', 'Vũ Minh Đức', 'Trần Thị Mai', 'Lê Hoàng Nam', 'Đặng Khánh An', 'Nguyễn Hồng Đào'][index]}</strong></footer></article>)}</section>
  </>;
}

function Contracts({ rows, onOpen, notify }) { return <>
  <PageHeading title="Quản lý hợp đồng" description="Theo dõi tình trạng, thời hạn và các mốc gia hạn hợp đồng lao động." action={<button className="primary" onClick={() => notify('Đã mở biểu mẫu tạo hợp đồng')}>＋ Tạo hợp đồng</button>} />
  <MetricGrid metrics={[['Đang hiệu lực', '284', '91% tổng số'], ['Sắp hết hạn', '12', 'Trong 60 ngày tới'], ['Cần gia hạn', '7', 'Cần xử lý'], ['Thử việc', '18', 'Theo dõi đánh giá']]} />
  <section className="panel"><Toolbar placeholder="Tìm theo mã hợp đồng, nhân viên..." /><div className="table-wrap"><table><thead><tr><th>Nhân viên</th><th>Mã hợp đồng</th><th>Loại hợp đồng</th><th>Ngày hiệu lực</th><th>Trạng thái</th><th /></tr></thead><tbody>{rows.map((row) => <tr key={row.id} onClick={() => onOpen(row)}><td><Person item={row} /></td><td><code>HĐ-{row.id.slice(3)}</code></td><td>{row.contract}</td><td>{row.joined} — 14/03/2026</td><td><Badge text={row.status === 'Thử việc' ? 'Thử việc' : 'Đang hiệu lực'} tone={row.status === 'Thử việc' ? 'amber' : 'green'} /></td><td><button className="more" aria-label="Xem chi tiết">⋮</button></td></tr>)}</tbody></table></div><Pagination /></section>
  </>;
}

function RecruitmentArea({ view, setView, rows, onOpen, notify }) { return <>
  <PageHeading title="Quản lý tuyển dụng" description="Theo dõi tuyển dụng từ yêu cầu nhân sự đến tiếp nhận nhân viên mới." action={<button className="primary" onClick={() => notify('Đã mở biểu mẫu tạo yêu cầu tuyển dụng')}>＋ Tạo yêu cầu tuyển dụng</button>} />
  <MetricGrid metrics={[['Vị trí đang tuyển', '18', '+3 trong tháng'], ['Tổng ứng viên', '126', '34 hồ sơ mới'], ['Phỏng vấn tuần này', '16', '6 lịch hôm nay'], ['Tỷ lệ tuyển dụng', '72%', '+8% so với tháng trước']]} />
  <Tabs tabs={[['pipeline', 'Đường ống ứng viên'], ['jobs', 'Yêu cầu tuyển dụng'], ['interviews', 'Lịch phỏng vấn'], ['onboarding', 'Tiếp nhận nhân sự']]} active={view} setActive={setView} />
  {view === 'pipeline' && <section className="panel pipeline-panel"><div className="panel-header"><div><h2>Đường ống tuyển dụng</h2><p>Kéo thả ứng viên qua từng giai đoạn của quy trình tuyển dụng.</p></div><button className="secondary" onClick={() => notify('Bộ lọc vị trí đã được mở')}>☷ Lọc vị trí</button></div><div className="kanban">{stages.map((stage) => <KanbanColumn key={stage} stage={stage} candidates={rows.filter((candidate) => candidate.stage === stage)} onOpen={onOpen} />)}</div></section>}
  {view === 'jobs' && <JobTable notify={notify} />}
  {view === 'interviews' && <Interviews notify={notify} />}
  {view === 'onboarding' && <Onboarding notify={notify} />}
  </>;
}

function KanbanColumn({ stage, candidates: cards, onOpen }) { return <div className="kanban-column"><div className="kanban-head"><span>{stage}</span><b>{cards.length}</b></div>{cards.length ? cards.map((card) => <button className="candidate-card" onClick={() => onOpen(card)} key={card.id}><div><Avatar initials={card.initials} /><span className="score">AI {card.score}</span></div><strong>{card.name}</strong><small>{card.role}</small><p>{card.tags.map((tag) => <i key={tag}>{tag}</i>)}</p><footer><span>{card.source}</span><span>→</span></footer></button>) : <div className="empty-column">Chưa có ứng viên</div>}</div>; }

function JobTable({ notify }) { const jobs = [['REQ-2026-08', 'Lead Systems Architect', 'Kỹ thuật & Công nghệ', '18', '2'], ['REQ-2026-11', 'Senior Backend Engineer', 'Kỹ thuật & Công nghệ', '42', '4'], ['REQ-2026-04', 'Principal Product Designer', 'Sản phẩm & Trải nghiệm', '21', '1'], ['REQ-2026-15', 'DevOps & Cloud Lead', 'Kỹ thuật & Công nghệ', '17', '2']]; return <section className="panel"><div className="panel-header"><div><h2>Yêu cầu tuyển dụng</h2><p>Các vị trí đang mở và tiến độ tuyển dụng.</p></div><button className="secondary" onClick={() => notify('Đã xuất danh sách yêu cầu')}>⇩ Xuất danh sách</button></div><div className="table-wrap"><table><thead><tr><th>Mã yêu cầu</th><th>Vị trí</th><th>Phòng ban</th><th>Ứng viên</th><th>Chỉ tiêu</th><th>Trạng thái</th><th /></tr></thead><tbody>{jobs.map((job) => <tr key={job[0]}><td><code>{job[0]}</code></td><td><strong>{job[1]}</strong></td><td>{job[2]}</td><td>{job[3]} hồ sơ</td><td>{job[4]} người</td><td><Badge text="Đang tuyển" tone="blue" /></td><td><button className="more">⋮</button></td></tr>)}</tbody></table></div><Pagination /></section>; }

function Interviews({ notify }) { const items = [['Hôm nay, 15:30', 'Lê Gia Huy', 'DevOps Engineer', 'Technical interview', 'Google Meet'], ['13/09, 10:00', 'Phạm Thu Hà', 'Senior Backend Engineer', 'Technical interview', 'Phòng họp 3B'], ['14/09, 14:30', 'Đỗ Anh Khoa', 'Product Designer', 'Final interview', 'Google Meet']]; return <section className="panel"><div className="panel-header"><div><h2>Lịch phỏng vấn sắp tới</h2><p>Điều phối lịch hẹn và phiếu đánh giá cho từng ứng viên.</p></div><button className="primary" onClick={() => notify('Đã mở lịch phỏng vấn')}>＋ Lên lịch phỏng vấn</button></div><div className="schedule-list">{items.map(([time, name, role, type, place]) => <article className="schedule-row" key={name}><div className="calendar-icon">▣</div><div><strong>{name}</strong><p>{role} · {type}</p></div><time>{time}<small>{place}</small></time><button className="secondary" onClick={() => notify(`Đã mở scorecard của ${name}`)}>Xem scorecard</button></article>)}</div></section>; }

function Onboarding({ notify }) { const hires = [['Bùi Khánh Linh', 'QA Automation Engineer', 'Kỹ thuật & Công nghệ', '01/10/2026', 'Đã ký offer'], ['Ngô Minh Quân', 'Sales Executive', 'Kinh doanh & Tiếp thị', '15/10/2026', 'Chờ giấy tờ'], ['Lý Bảo Ngọc', 'HR Specialist', 'Nhân sự & Đào tạo', '20/10/2026', 'Đã ký offer']]; return <section className="panel"><div className="panel-header"><div><h2>Tiếp nhận nhân sự mới</h2><p>Hoàn thiện các bước trước ngày nhận việc.</p></div><button className="secondary" onClick={() => notify('Đã gửi lời nhắc onboarding')}>Gửi lời nhắc</button></div><div className="table-wrap"><table><thead><tr><th>Ứng viên</th><th>Vị trí</th><th>Phòng ban</th><th>Ngày nhận việc</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>{hires.map((hire) => <tr key={hire[0]}><td><strong>{hire[0]}</strong></td><td>{hire[1]}</td><td>{hire[2]}</td><td>{hire[3]}</td><td><Badge text={hire[4]} tone={hire[4] === 'Đã ký offer' ? 'green' : 'amber'} /></td><td><button className="secondary small" onClick={() => notify(`${hire[0]} đã được tiếp nhận`)}>Tiếp nhận</button></td></tr>)}</tbody></table></div></section>; }

function AttendanceArea({ view, setView, notify }) { return <>
  <PageHeading title="Chấm công & Quản lý nghỉ phép" description="Theo dõi thời gian làm việc, lịch ca và đơn nghỉ phép của nhân sự." action={<div className="heading-actions"><button className="secondary" onClick={() => notify('Đã ghi nhận chấm công')}>◷ Chấm công nhanh</button><button className="primary" onClick={() => notify('Đã mở biểu mẫu nghỉ phép')}>＋ Tạo đơn nghỉ phép</button></div>} />
  <MetricGrid metrics={[['Có mặt hôm nay', '268', '86% nhân sự'], ['Đi muộn / Về sớm', '12', 'Cần theo dõi'], ['Đang nghỉ phép', '9', '3 đơn có lương'], ['Chờ phê duyệt', '7', 'Cần xử lý']]} />
  <Tabs tabs={[['timesheet', 'Bảng công & điểm danh'], ['shifts', 'Ca làm việc'], ['leaves', 'Đơn nghỉ phép'], ['approvals', 'Xét duyệt nghỉ phép']]} active={view} setActive={setView} />
  {view === 'timesheet' && <Timesheet notify={notify} />}{view === 'shifts' && <Shifts notify={notify} />}{view === 'leaves' && <Leaves notify={notify} />}{view === 'approvals' && <Approvals notify={notify} />}
  </>;
}

function Timesheet({ notify }) { const records = [['Nguyễn Văn Hùng', 'Kỹ thuật & Công nghệ', '08:02', '17:34', '8h 32m', 'Đúng giờ'], ['Trần Thị Mai', 'Nhân sự & Đào tạo', '07:58', '17:28', '8h 30m', 'Đúng giờ'], ['Lê Hoàng Nam', 'Tài chính & Kế toán', '08:18', '17:47', '8h 29m', 'Đi muộn'], ['Phạm Thanh Sơn', 'Kỹ thuật & Công nghệ', '08:07', '17:19', '8h 12m', 'Đúng giờ'], ['Nguyễn Hồng Đào', 'Sản phẩm & Trải nghiệm', '08:23', '17:52', '8h 29m', 'Đi muộn']]; return <section className="panel"><div className="panel-header"><div><h2>Bảng công hôm nay</h2><p>Thứ Bảy, 12 tháng 09, 2026 · Dữ liệu được cập nhật theo thời gian thực.</p></div><button className="secondary" onClick={() => notify('Đã xuất bảng công')}>⇩ Xuất bảng công</button></div><div className="table-wrap"><table><thead><tr><th>Nhân viên</th><th>Phòng ban</th><th>Check-in</th><th>Check-out</th><th>Giờ làm</th><th>Trạng thái</th><th /></tr></thead><tbody>{records.map((record, index) => <tr key={record[0]}><td><Person item={{ name: record[0], initials: record[0].split(' ').map(x => x[0]).slice(-2).join(''), title: index === 0 ? 'Senior Tech Lead' : 'Nhân viên' }} /></td><td>{record[1]}</td><td><strong>{record[2]}</strong></td><td><strong>{record[3]}</strong></td><td>{record[4]}</td><td><Badge text={record[5]} tone={record[5] === 'Đúng giờ' ? 'green' : 'amber'} /></td><td><button className="more">⋮</button></td></tr>)}</tbody></table></div><Pagination /></section>; }

function Shifts({ notify }) { const shifts = [['HC', 'Ca hành chính', '08:00 — 17:30', 'Thứ Hai — Thứ Sáu', '242 nhân sự'], ['S1', 'Ca sáng', '06:00 — 14:00', 'Thứ Hai — Chủ Nhật', '38 nhân sự'], ['S2', 'Ca chiều', '14:00 — 22:00', 'Thứ Hai — Chủ Nhật', '21 nhân sự'], ['N1', 'Ca trực đêm', '22:00 — 06:00', 'Thứ Hai — Chủ Nhật', '11 nhân sự']]; return <section className="panel"><div className="panel-header"><div><h2>Danh mục ca làm việc</h2><p>Thiết lập giờ làm, ngày áp dụng và nhân sự thuộc từng ca.</p></div><button className="primary" onClick={() => notify('Đã mở biểu mẫu thêm ca')}>＋ Thêm ca làm việc</button></div><div className="shift-grid">{shifts.map((shift) => <article className="shift-card" key={shift[0]}><span className="shift-code">{shift[0]}</span><h3>{shift[1]}</h3><strong>{shift[2]}</strong><p>{shift[3]}</p><footer><span>♙ {shift[4]}</span><button onClick={() => notify(`Đã mở chỉnh sửa ${shift[1]}`)}>Chỉnh sửa</button></footer></article>)}</div></section>; }

function Leaves({ notify }) { const rows = [['Nguyễn Hồng Đào', 'Nghỉ phép năm', '15/09/2026 — 16/09/2026', '2 ngày', 'Chờ phê duyệt'], ['Phạm Thanh Sơn', 'Nghỉ ốm', '12/09/2026', '1 ngày', 'Đã phê duyệt'], ['Trần Thị Mai', 'Việc riêng', '20/09/2026', '1 ngày', 'Đã phê duyệt'], ['Vũ Minh Đức', 'Nghỉ không lương', '22/09/2026 — 24/09/2026', '3 ngày', 'Chờ phê duyệt']]; return <section className="panel"><div className="panel-header"><div><h2>Đơn nghỉ phép</h2><p>Lịch sử đơn nghỉ và quỹ phép cá nhân của nhân sự.</p></div><button className="primary" onClick={() => notify('Đã mở biểu mẫu tạo đơn')}>＋ Tạo đơn nghỉ phép</button></div><div className="table-wrap"><table><thead><tr><th>Nhân viên</th><th>Loại nghỉ</th><th>Thời gian</th><th>Số ngày</th><th>Trạng thái</th><th /></tr></thead><tbody>{rows.map((row) => <tr key={row[0]}><td><strong>{row[0]}</strong></td><td>{row[1]}</td><td>{row[2]}</td><td>{row[3]}</td><td><Badge text={row[4]} tone={row[4] === 'Đã phê duyệt' ? 'green' : 'amber'} /></td><td><button className="more">⋮</button></td></tr>)}</tbody></table></div><Pagination /></section>; }

function Approvals({ notify }) { const pending = [['Nguyễn Hồng Đào', 'Nghỉ phép năm', '15/09 — 16/09/2026', '2 ngày'], ['Vũ Minh Đức', 'Nghỉ không lương', '22/09 — 24/09/2026', '3 ngày'], ['Đỗ Phương Linh', 'Việc riêng', '18/09/2026', '1 ngày']]; return <section className="panel"><div className="panel-header"><div><h2>Yêu cầu chờ phê duyệt</h2><p>Xử lý các đơn nghỉ phép cần phản hồi từ quản lý.</p></div><button className="primary" onClick={() => notify('Đã phê duyệt tất cả đơn hợp lệ')}>✓ Duyệt tất cả</button></div><div className="approval-list">{pending.map((request) => <article className="approval-row" key={request[0]}><Avatar initials={request[0].split(' ').map(x => x[0]).slice(-2).join('')} /><div><strong>{request[0]}</strong><p>{request[1]} · {request[2]} · {request[3]}</p></div><div><button className="secondary reject" onClick={() => notify(`Đã từ chối đơn của ${request[0]}`)}>Từ chối</button><button className="primary small" onClick={() => notify(`Đã phê duyệt đơn của ${request[0]}`)}>Duyệt đơn</button></div></article>)}</div></section>; }

function PageHeading({ title, description, action }) { return <div className="page-heading"><div><p className="breadcrumb">HR MANAGEMENT</p><h1>{title}</h1><p className="description">{description}</p></div>{action}</div>; }
function MetricGrid({ metrics }) { return <section className="metric-grid">{metrics.map(([label, value, detail], index) => <article className={`metric metric-${index}`} key={label}><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>)}</section>; }
function Toolbar({ placeholder }) { return <div className="toolbar"><label><span>⌕</span><input placeholder={placeholder} /></label><select defaultValue="all"><option value="all">Tất cả phòng ban</option>{departments.map((department) => <option key={department}>{department}</option>)}</select><select defaultValue="status"><option value="status">Trạng thái hợp đồng</option><option>Đang làm việc</option><option>Thử việc</option></select><button className="secondary">☷ Bộ lọc</button></div>; }
function EmployeeTable({ rows, onOpen }) { return <div className="table-wrap"><table><thead><tr><th>Nhân viên</th><th>Mã nhân viên</th><th>Phòng ban</th><th>Chức danh</th><th>Ngày vào làm</th><th>Trạng thái</th><th /></tr></thead><tbody>{rows.map((row) => <tr key={row.id} onClick={() => onOpen(row)}><td><Person item={row} /></td><td><code>{row.id}</code></td><td>{row.department}</td><td>{row.title}</td><td>{row.joined}</td><td><Badge text={row.status} tone={row.status === 'Thử việc' ? 'amber' : 'green'} /></td><td><button className="more">⋮</button></td></tr>)}</tbody></table>{!rows.length && <div className="empty-state">Không tìm thấy nhân viên phù hợp.</div>}</div>; }
function Tabs({ tabs, active, setActive }) { return <div className="tabs">{tabs.map(([id, label]) => <button className={active === id ? 'active' : ''} onClick={() => setActive(id)} key={id}>{label}</button>)}</div>; }
function Person({ item }) { return <div className="person"><Avatar initials={item.initials} /><div><strong>{item.name}</strong><small>{item.email || item.title}</small></div></div>; }
function Avatar({ initials }) { return <span className="avatar">{initials}</span>; }
function Badge({ text, tone = 'blue' }) { return <span className={`badge ${tone}`}>{text}</span>; }
function Pagination() { return <div className="pagination"><span>Hiển thị 1–5 trong tổng số 312 kết quả</span><div><button disabled>‹</button><button className="current">1</button><button>2</button><button>3</button><button>›</button></div></div>; }
function DetailDrawer({ item, kind, onClose, notify }) { const isCandidate = kind === 'recruitment'; return <div className="drawer-backdrop" onMouseDown={onClose}><aside className="drawer" onMouseDown={(event) => event.stopPropagation()} role="dialog" aria-modal="true"><button className="drawer-close" onClick={onClose}>×</button><Avatar initials={item.initials} /><p className="breadcrumb">{isCandidate ? 'HỒ SƠ ỨNG VIÊN' : 'HỒ SƠ NHÂN VIÊN'}</p><h2>{item.name}</h2><p className="drawer-role">{item.role || item.title}</p>{isCandidate ? <><div className="ai-score"><strong>{item.score}</strong><span>AI matching score</span></div><h3>Kỹ năng</h3><div className="tag-list">{item.tags.map((tag) => <span key={tag}>{tag}</span>)}</div><InfoList items={[['Vòng hiện tại', item.stage], ['Nguồn ứng tuyển', item.source], ['Hành động tiếp theo', 'Chuyển sang vòng kế tiếp']]} /><button className="primary full" onClick={() => { onClose(); notify(`${item.name} đã được chuyển sang vòng tiếp theo`); }}>Chuyển vòng ứng viên →</button></> : <><InfoList items={[['Mã nhân viên', item.id], ['Phòng ban', item.department], ['Email công việc', item.email], ['Loại hợp đồng', item.contract], ['Ngày vào làm', item.joined], ['Trạng thái', item.status]]} /><button className="primary full" onClick={() => notify(`Đã mở chỉnh sửa hồ sơ ${item.name}`)}>Chỉnh sửa hồ sơ</button></>}</aside></div>; }
function InfoList({ items }) { return <dl className="info-list">{items.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>; }

export default App;
