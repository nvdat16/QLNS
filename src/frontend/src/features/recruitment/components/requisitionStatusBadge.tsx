import { Badge, type BadgeTone } from "../../../components/Badge";
import type { EmploymentType, RequisitionStatus } from "../api/requisitionsApi";

const statusConfig: Record<RequisitionStatus, { tone: BadgeTone; label: string }> = {
  draft: { tone: "slate", label: "Nháp" },
  pending_approval: { tone: "amber", label: "Chờ duyệt" },
  approved: { tone: "blue", label: "Đã duyệt" },
  active_recruiting: { tone: "emerald", label: "Đang tuyển dụng" },
  closed: { tone: "slate", label: "Đã đóng" },
  cancelled: { tone: "rose", label: "Đã huỷ" },
};

export const employmentTypeLabels: Record<EmploymentType, string> = {
  full_time: "Toàn thời gian",
  part_time: "Bán thời gian",
  hybrid: "Hybrid",
  remote: "Từ xa",
  internship: "Thực tập",
  service_contract: "Hợp đồng dịch vụ",
};

export function RequisitionStatusBadge({ status }: { status: RequisitionStatus }) {
  const { tone, label } = statusConfig[status];
  return <Badge tone={tone}>{label}</Badge>;
}
