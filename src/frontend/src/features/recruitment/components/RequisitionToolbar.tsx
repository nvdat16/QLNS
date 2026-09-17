import type { Department } from "../../organization/api/organizationApi";
import { requisitionStatuses, type RequisitionFilters, type RequisitionStatus } from "../api/requisitionsApi";

const statusLabels: Record<RequisitionStatus, string> = {
  draft: "Nháp",
  pending_approval: "Chờ duyệt",
  approved: "Đã duyệt",
  active_recruiting: "Đang tuyển dụng",
  closed: "Đã đóng",
  cancelled: "Đã huỷ",
};

interface Props {
  departments: Department[];
  onChange: (filters: Omit<RequisitionFilters, "page" | "pageSize">) => void;
  onCreate: () => void;
}

export function RequisitionToolbar({ departments, onChange, onCreate }: Props) {
  return (
    <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4">
      <div className="relative min-w-[220px] flex-1">
        <span className="material-symbols-outlined icon-sm absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
          search
        </span>
        <input
          type="search"
          placeholder="Tìm chức danh, mã vị trí..."
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
        onChange={(event) =>
          onChange({ status: (event.target.value || undefined) as RequisitionStatus | undefined })
        }
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Trạng thái: Tất cả</option>
        {requisitionStatuses.map((status) => (
          <option key={status} value={status}>
            {statusLabels[status]}
          </option>
        ))}
      </select>

      <button
        type="button"
        onClick={onCreate}
        className="ml-auto flex items-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
      >
        <span className="material-symbols-outlined icon-sm">add</span>
        Đăng tin tuyển dụng mới
      </button>
    </div>
  );
}
