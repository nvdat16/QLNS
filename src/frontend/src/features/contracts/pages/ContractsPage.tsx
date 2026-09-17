import { useState } from "react";
import { PageHeader } from "../../../components/PageHeader";
import { Pagination } from "../../../components/Pagination";
import { StateBanner } from "../../../components/StateBanner";
import { EmployeeDetailDrawer } from "../../employees/components/EmployeeDetailDrawer";
import { useEmployeeNames } from "../../employees/hooks/useEmployeeNames";
import { useOrganizationDirectory } from "../../organization/hooks/useOrganizationDirectory";
import type { Contract, ContractLifecycleAction } from "../api/contractsApi";
import { ContractActionModal } from "../components/ContractActionModal";
import { ContractTable } from "../components/ContractTable";
import { ContractToolbar } from "../components/ContractToolbar";
import { useContractAction } from "../hooks/useContractAction";
import { useContracts } from "../hooks/useContracts";

export function ContractsPage() {
  const { data, loading, error, updateFilters, setPage, reload, replaceContract } = useContracts();
  const { nameFor } = useEmployeeNames(data?.items.map((contract) => contract.employeeId) ?? []);
  const { run, busyContractId, error: actionError } = useContractAction(replaceContract);
  const { departmentName, positionName } = useOrganizationDirectory();
  const [pending, setPending] = useState<{ contract: Contract; action: ContractLifecycleAction }>();
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number>();

  function handleAction(contract: Contract, action: ContractLifecycleAction) {
    if (action === "terminate" || action === "cancel") {
      setPending({ contract, action });
      return;
    }
    void run(contract, action);
  }

  return (
    <div>
      <PageHeader breadcrumb="Không gian làm việc / Quản lý hợp đồng" title="Quản lý hợp đồng lao động" />

      {actionError && (
        <div className="mb-4">
          <StateBanner error={actionError} />
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-[#edf0f4] bg-white shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
        <ContractToolbar onChange={updateFilters} />

        {(loading && !data) || error ? (
          <div className="p-5">
            <StateBanner loading={loading && !data} error={error} onRetry={reload} loadingLabel="Đang tải danh sách hợp đồng…" />
          </div>
        ) : (
          data && (
            <>
              <div className="overflow-x-auto hrm-scroll">
                <ContractTable
                  contracts={data.items}
                  nameFor={nameFor}
                  busyContractId={busyContractId}
                  onSelect={(contract) => setSelectedEmployeeId(contract.employeeId)}
                  onAction={handleAction}
                />
              </div>
              <Pagination page={data.page} itemLabel="hợp đồng" onPageChange={setPage} />
            </>
          )
        )}
      </div>

      <ContractActionModal
        pending={pending}
        busy={busyContractId === pending?.contract.id}
        onClose={() => setPending(undefined)}
        onConfirm={(reason) => {
          if (!pending) return;
          void run(pending.contract, pending.action, { reason }).then((ok) => {
            if (ok) setPending(undefined);
          });
        }}
      />

      <EmployeeDetailDrawer
        employeeId={selectedEmployeeId}
        departmentName={departmentName}
        positionName={positionName}
        onClose={() => setSelectedEmployeeId(undefined)}
      />
    </div>
  );
}
