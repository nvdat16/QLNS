import { contractStatuses, contractTypes, type ContractFilters, type ContractStatus, type ContractType } from "../api/contractsApi";
import { contractTypeLabels } from "./contractStatusBadge";

const statusLabels: Record<ContractStatus, string> = {
  draft: "Nháp",
  approved: "Đã duyệt",
  executed: "Đã ký",
  active: "Hiệu lực",
  expired: "Hết hiệu lực",
  terminated: "Đã chấm dứt",
  cancelled: "Đã huỷ",
};

interface Props {
  onChange: (filters: Omit<ContractFilters, "page" | "pageSize">) => void;
}

export function ContractToolbar({ onChange }: Props) {
  return (
    <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4">
      <div className="relative min-w-[220px] flex-1">
        <span className="material-symbols-outlined icon-sm absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
          search
        </span>
        <input
          type="search"
          placeholder="Tìm theo mã nhân viên..."
          onChange={(event) =>
            onChange({ employeeId: event.target.value ? Number(event.target.value) : undefined })
          }
          className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </div>

      <select
        onChange={(event) => onChange({ type: (event.target.value || undefined) as ContractType | undefined })}
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Loại hợp đồng: Tất cả</option>
        {contractTypes.map((type) => (
          <option key={type} value={type}>
            {contractTypeLabels[type]}
          </option>
        ))}
      </select>

      <select
        onChange={(event) => onChange({ status: (event.target.value || undefined) as ContractStatus | undefined })}
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Tình trạng: Tất cả</option>
        {contractStatuses.map((status) => (
          <option key={status} value={status}>
            {statusLabels[status]}
          </option>
        ))}
      </select>
    </div>
  );
}
