import { formatCurrency, formatDate } from "../../../components/format";
import { IconButton } from "../../../components/IconButton";
import type { Contract, ContractLifecycleAction } from "../api/contractsApi";
import { ContractStatusBadge, contractTypeLabels } from "./contractStatusBadge";

const actionsByStatus: Record<Contract["status"], { action: ContractLifecycleAction; icon: string; label: string }[]> = {
  draft: [
    { action: "approve", icon: "task_alt", label: "Duyệt hợp đồng" },
    { action: "cancel", icon: "block", label: "Huỷ hợp đồng" },
  ],
  approved: [
    { action: "activate", icon: "play_arrow", label: "Kích hoạt hợp đồng" },
    { action: "cancel", icon: "block", label: "Huỷ hợp đồng" },
  ],
  executed: [{ action: "activate", icon: "play_arrow", label: "Kích hoạt hợp đồng" }],
  active: [{ action: "terminate", icon: "stop_circle", label: "Chấm dứt hợp đồng" }],
  expired: [],
  terminated: [],
  cancelled: [],
};

interface Props {
  contracts: Contract[];
  nameFor: (employeeId: number) => string;
  busyContractId: number | undefined;
  onSelect: (contract: Contract) => void;
  onAction: (contract: Contract, action: ContractLifecycleAction) => void;
}

export function ContractTable({ contracts, nameFor, busyContractId, onSelect, onAction }: Props) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 bg-[#fafbfc] text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Số hợp đồng</th>
          <th className="px-5 py-3">Nhân viên</th>
          <th className="px-5 py-3">Loại hợp đồng</th>
          <th className="px-5 py-3">Lương cơ bản</th>
          <th className="px-5 py-3">Bắt đầu</th>
          <th className="px-5 py-3">Hết hạn</th>
          <th className="px-5 py-3 text-center">Hiệu lực</th>
          <th className="px-5 py-3 text-right">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        {contracts.map((contract) => (
          <tr
            key={contract.id}
            onClick={() => onSelect(contract)}
            className="cursor-pointer border-b border-[#eef0f4] last:border-0 hover:bg-slate-50"
          >
            <td className="px-5 py-3.5 font-mono text-xs font-semibold text-brand-600">{contract.contractNumber}</td>
            <td className="px-5 py-3.5 font-medium text-slate-800">{nameFor(contract.employeeId)}</td>
            <td className="px-5 py-3.5 text-slate-600">{contractTypeLabels[contract.contractType]}</td>
            <td className="px-5 py-3.5 font-semibold text-slate-800">
              {formatCurrency(contract.salary, contract.currency)}
            </td>
            <td className="px-5 py-3.5 text-slate-600">{formatDate(contract.startDate)}</td>
            <td className={`px-5 py-3.5 ${contract.endDate ? "font-semibold text-rose-600" : "text-slate-400"}`}>
              {contract.endDate ? formatDate(contract.endDate) : "Vô thời hạn"}
            </td>
            <td className="px-5 py-3.5 text-center">
              <ContractStatusBadge status={contract.status} />
            </td>
            <td className="px-5 py-3.5">
              <div className="flex justify-end gap-1.5">
                {actionsByStatus[contract.status].map(({ action, icon, label }) => (
                  <IconButton
                    key={action}
                    icon={icon}
                    label={label}
                    tone="brand"
                    disabled={busyContractId === contract.id}
                    onClick={() => onAction(contract, action)}
                  />
                ))}
              </div>
            </td>
          </tr>
        ))}
        {contracts.length === 0 && (
          <tr>
            <td colSpan={8} className="px-5 py-10 text-center text-sm text-slate-400">
              Không tìm thấy hợp đồng phù hợp với bộ lọc.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
