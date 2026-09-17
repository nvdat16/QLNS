import { NavLink } from "react-router-dom";
import { useAuth } from "../../features/auth/context/AuthContext";
import { personaLabels } from "../../features/auth/api/authApi";

const navItems = [
  { to: "/employees", icon: "group", label: "Hồ sơ nhân viên" },
  { to: "/contracts", icon: "description", label: "Quản lý hợp đồng" },
  { to: "/recruitment", icon: "person_search", label: "Quản lý tuyển dụng" },
];

export function Sidebar() {
  const { session, signOut } = useAuth();

  return (
    <aside className="fixed inset-y-0 left-0 z-30 hidden w-[260px] flex-col border-r border-[#e8ebf1] bg-white/98 lg:flex">
      <div className="flex h-[74px] items-center gap-3 border-b border-[#e8ebf1] px-5">
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-white">
          <span className="material-symbols-outlined icon-md">business_center</span>
        </span>
        <div>
          <p className="text-sm font-bold leading-tight text-slate-900">NexusHR</p>
          <p className="text-[11px] leading-tight text-slate-400">Human Resources</p>
        </div>
      </div>

      <nav className="flex-1 overflow-y-auto hrm-scroll px-3.5 py-4.5">
        <p className="nav-section-label">Menu chính</p>
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) => `nav-item ${isActive ? "nav-item-active" : ""}`}
          >
            <span className="material-symbols-outlined icon-md">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>

      {session && (
        <div className="border-t border-[#e8ebf1] p-4">
          <div className="flex items-center gap-3 rounded-xl bg-slate-50 px-3 py-2.5">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-700">
              {session.persona.slice(0, 2).toUpperCase()}
            </span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-slate-800">{personaLabels[session.persona]}</p>
              <p className="truncate text-[11px] text-slate-400">Môi trường phát triển</p>
            </div>
            <button
              type="button"
              onClick={signOut}
              title="Đăng xuất"
              aria-label="Đăng xuất"
              className="shrink-0 rounded-lg p-1.5 text-slate-400 hover:bg-white hover:text-rose-600"
            >
              <span className="material-symbols-outlined icon-sm">logout</span>
            </button>
          </div>
        </div>
      )}
    </aside>
  );
}
