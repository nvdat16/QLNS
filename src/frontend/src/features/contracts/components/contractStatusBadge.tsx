import { Badge, type BadgeTone } from "../../../components/Badge";
import type { ContractStatus, ContractType } from "../api/contractsApi";

const statusConfig: Record<ContractStatus, { tone: BadgeTone; label: string }> = {
  draft: { tone: "slate", label: "Nháp" },
  approved: { tone: "blue", label: "Đã duyệt" },
  executed: { tone: "blue", label: "Đã ký" },
  active: { tone: "emerald", label: "Hiệu lực" },
  expired: { tone: "amber", label: "Hết hiệu lực" },
  terminated: { tone: "rose", label: "Đã chấm dứt" },
  cancelled: { tone: "rose", label: "Đã huỷ" },
};

export const contractTypeLabels: Record<ContractType, string> = {
  probation: "Thử việc",
  fixed_term: "Xác định thời hạn",
  indefinite: "Không xác định thời hạn",
  internship: "Thực tập",
  service_contract: "Hợp đồng dịch vụ",
};

export function ContractStatusBadge({ status }: { status: ContractStatus }) {
  const { tone, label } = statusConfig[status];
  return <Badge tone={tone}>{label}</Badge>;
}
