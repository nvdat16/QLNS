import { Badge, type BadgeTone } from "../../../components/Badge";
import type { EmployeeStatus } from "../api/employeesApi";

const config: Record<EmployeeStatus, { tone: BadgeTone; label: string }> = {
  probation: { tone: "amber", label: "Thử việc" },
  active: { tone: "emerald", label: "Đang làm việc" },
  suspended: { tone: "slate", label: "Tạm hoãn" },
  terminated: { tone: "rose", label: "Đã nghỉ việc" },
};

export function EmployeeStatusBadge({ status }: { status: EmployeeStatus }) {
  const { tone, label } = config[status];
  return <Badge tone={tone}>{label}</Badge>;
}
