import { Badge, type BadgeTone } from "../../../components/Badge";
import type { OfferStatus } from "../api/offersApi";

const config: Record<OfferStatus, { tone: BadgeTone; label: string }> = {
  draft: { tone: "slate", label: "Nháp" },
  approved: { tone: "blue", label: "Đã duyệt" },
  sent: { tone: "amber", label: "Đã gửi (chờ ký)" },
  accepted: { tone: "emerald", label: "Đã ký (Executed)" },
  declined: { tone: "rose", label: "Ứng viên từ chối" },
  expired: { tone: "slate", label: "Hết hạn" },
  cancelled: { tone: "rose", label: "Đã huỷ" },
};

export function OfferStatusBadge({ status }: { status: OfferStatus }) {
  const { tone, label } = config[status];
  return <Badge tone={tone}>{label}</Badge>;
}
