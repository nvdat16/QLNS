import { NavLink, Outlet } from "react-router-dom";

const tabs = [
  { to: "/recruitment/pipeline", icon: "view_kanban", label: "Candidate Pipeline (ATS)" },
  { to: "/recruitment/requisitions", icon: "work_outline", label: "Job Requisitions" },
  { to: "/recruitment/interviews", icon: "event_note", label: "Interviews & Scorecards" },
  { to: "/recruitment/offers", icon: "task_alt", label: "Offers & Onboarding Handoff" },
];

export function RecruitmentLayout() {
  return (
    <div>
      <div className="mb-6 flex flex-wrap gap-1 rounded-xl bg-slate-100 p-1">
        {tabs.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              `flex items-center gap-1.5 rounded-lg px-3.5 py-2 text-sm font-semibold transition-colors ${
                isActive ? "bg-brand-600 text-white" : "text-slate-500 hover:text-slate-700"
              }`
            }
          >
            <span className="material-symbols-outlined icon-sm">{tab.icon}</span>
            {tab.label}
          </NavLink>
        ))}
      </div>
      <Outlet />
    </div>
  );
}
