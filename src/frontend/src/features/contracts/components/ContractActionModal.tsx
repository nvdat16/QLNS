import { ReasonModal } from "../../../components/ReasonModal";
import type { Contract, ContractLifecycleAction } from "../api/contractsApi";

const titles: Record<ContractLifecycleAction, string> = {
  approve: "Duyệt hợp đồng",
  activate: "Kích hoạt hợp đồng",
  terminate: "Chấm dứt hợp đồng",
  cancel: "Huỷ hợp đồng",
};

interface Props {
  pending?: { contract: Contract; action: ContractLifecycleAction };
  busy: boolean;
  onConfirm: (reason: string) => void;
  onClose: () => void;
}

export function ContractActionModal({ pending, busy, onConfirm, onClose }: Props) {
  return (
    <ReasonModal
      key={pending ? `${pending.contract.id}-${pending.action}` : "none"}
      open={pending !== undefined}
      title={pending ? titles[pending.action] : ""}
      subtitle={pending ? `Hợp đồng ${pending.contract.contractNumber}` : undefined}
      busy={busy}
      onConfirm={onConfirm}
      onClose={onClose}
    />
  );
}
