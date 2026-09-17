import type { Department } from "../../organization/api/organizationApi";
import { employeeStatuses, type EmployeeFilters, type EmployeeStatus } from "../api/employeesApi";

const statusLabels: Record<EmployeeStatus, string> = {
  probation: "Thử việc",
  active: "Đang làm việc",
  suspended: "Tạm hoãn",
  terminated: "Đã nghỉ việc",
};

interface Props {
  departments: Department[];
  onChange: (filters: Omit<EmployeeFilters, "page" | "pageSize">) => void;
}

export function EmployeeToolbar({ departments, onChange }: Props) {
  return (
    <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4">
      <div className="relative min-w-[240px] flex-1">
        <span className="material-symbols-outlined icon-sm absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
          search
        </span>
        <input
          type="search"
          placeholder="Tìm mã nhân viên, họ tên, email..."
          onChange={(event) => onChange({ search: event.target.value || undefined })}
          className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </div>

      <select
        onChange={(event) =>
          onChange({ departmentId: event.target.value ? Number(event.target.value) : undefined })
        }
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Phòng ban: Tất cả</option>
        {departments.map((department) => (
          <option key={department.id} value={department.id}>
            {department.name}
          </option>
        ))}
      </select>

      <select
        onChange={(event) => onChange({ status: (event.target.value || undefined) as EmployeeStatus | undefined })}
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Tình trạng: Tất cả</option>
        {employeeStatuses.map((status) => (
          <option key={status} value={status}>
            {statusLabels[status]}
          </option>
        ))}
      </select>
    </div>
  );
}
