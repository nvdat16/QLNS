import { Link } from "react-router-dom";
import { formatCurrency } from "../../../components/format";
import { IconButton } from "../../../components/IconButton";
import type { Requisition, RequisitionLifecycleAction } from "../api/requisitionsApi";
import { employmentTypeLabels, RequisitionStatusBadge } from "./requisitionStatusBadge";

const actionsByStatus: Record<
  Requisition["status"],
  { action: RequisitionLifecycleAction; icon: string; label: string }[]
> = {
  draft: [
    { action: "submit", icon: "send", label: "Gửi duyệt" },
    { action: "cancel", icon: "block", label: "Huỷ tin" },
  ],
  pending_approval: [
    { action: "approve", icon: "task_alt", label: "Duyệt" },
    { action: "reject", icon: "close", label: "Từ chối" },
  ],
  approved: [
    { action: "publish", icon: "campaign", label: "Đăng tin" },
    { action: "cancel", icon: "block", label: "Huỷ tin" },
  ],
  active_recruiting: [{ action: "close", icon: "check_circle", label: "Đóng tin (đã tuyển đủ)" }],
  closed: [],
  cancelled: [],
};

interface Props {
  requisitions: Requisition[];
  departmentName: (id: number) => string;
  busyRequisitionId: number | undefined;
  onAction: (requisition: Requisition, action: RequisitionLifecycleAction) => void;
}

export function RequisitionTable({ requisitions, departmentName, busyRequisitionId, onAction }: Props) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 bg-[#fafbfc] text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Mã vị trí &amp; chức danh</th>
          <th className="px-5 py-3">Phòng ban</th>
          <th className="px-5 py-3">Chỉ tiêu</th>
          <th className="px-5 py-3">Mức lương dự toán</th>
          <th className="px-5 py-3">Địa điểm &amp; hình thức</th>
          <th className="px-5 py-3 text-center">Trạng thái</th>
          <th className="px-5 py-3 text-right">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        {requisitions.map((requisition) => (
          <tr key={requisition.id} className="border-b border-[#eef0f4] last:border-0 hover:bg-slate-50">
            <td className="px-5 py-3.5">
              <p className="font-semibold text-slate-800">{requisition.title}</p>
              <p className="font-mono text-[11px] text-brand-600">{requisition.jobCode}</p>
            </td>
            <td className="px-5 py-3.5 text-slate-600">{departmentName(requisition.departmentId)}</td>
            <td className="px-5 py-3.5 font-semibold text-slate-700">{requisition.targetHeadcount} vị trí</td>
            <td className="px-5 py-3.5 font-semibold text-slate-800">
              {requisition.salaryMin && requisition.salaryMax
                ? `${formatCurrency(requisition.salaryMin)} - ${formatCurrency(requisition.salaryMax)}`
                : "Thoả thuận"}
            </td>
            <td className="px-5 py-3.5 text-slate-600">
              {requisition.location ?? "—"} • {employmentTypeLabels[requisition.employmentType]}
            </td>
            <td className="px-5 py-3.5 text-center">
              <RequisitionStatusBadge status={requisition.status} />
            </td>
            <td className="px-5 py-3.5">
              <div className="flex items-center justify-end gap-1.5">
                {requisition.status === "active_recruiting" && (
                  <Link
                    to={`/recruitment/pipeline?requisitionId=${requisition.id}`}
                    className="rounded-lg border border-brand-200 px-2.5 py-1.5 text-xs font-semibold text-brand-600 hover:bg-brand-50"
                  >
                    Pipeline →
                  </Link>
                )}
                {actionsByStatus[requisition.status].map(({ action, icon, label }) => (
                  <IconButton
                    key={action}
                    icon={icon}
                    label={label}
                    tone="brand"
                    disabled={busyRequisitionId === requisition.id}
                    onClick={() => onAction(requisition, action)}
                  />
                ))}
              </div>
            </td>
          </tr>
        ))}
        {requisitions.length === 0 && (
          <tr>
            <td colSpan={7} className="px-5 py-10 text-center text-sm text-slate-400">
              Chưa có tin tuyển dụng nào phù hợp với bộ lọc.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
