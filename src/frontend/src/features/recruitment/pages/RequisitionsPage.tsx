import { useState } from "react";
import { PageHeader } from "../../../components/PageHeader";
import { Pagination } from "../../../components/Pagination";
import { ReasonModal } from "../../../components/ReasonModal";
import { StateBanner } from "../../../components/StateBanner";
import { useOrganizationDirectory } from "../../organization/hooks/useOrganizationDirectory";
import { createRequisition, performRequisitionAction, type Requisition, type RequisitionLifecycleAction } from "../api/requisitionsApi";
import { NewRequisitionModal } from "../components/NewRequisitionModal";
import { RequisitionTable } from "../components/RequisitionTable";
import { RequisitionToolbar } from "../components/RequisitionToolbar";
import { useRequisitions } from "../hooks/useRequisitions";

export function RequisitionsPage() {
  const { data, loading, error, updateFilters, setPage, reload, upsert } = useRequisitions();
  const { departments, departmentName } = useOrganizationDirectory();
  const [createOpen, setCreateOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);
  const [createError, setCreateError] = useState<string>();
  const [busyRequisitionId, setBusyRequisitionId] = useState<number>();
  const [actionError, setActionError] = useState<string>();
  const [pendingRejection, setPendingRejection] = useState<Requisition>();

  async function handleAction(requisition: Requisition, action: RequisitionLifecycleAction) {
    if (action === "reject") {
      setPendingRejection(requisition);
      return;
    }
    setBusyRequisitionId(requisition.id);
    setActionError(undefined);
    try {
      upsert(await performRequisitionAction(requisition, action));
    } catch (cause) {
      setActionError(cause instanceof Error ? cause.message : "Không thể cập nhật tin tuyển dụng.");
    } finally {
      setBusyRequisitionId(undefined);
    }
  }

  return (
    <div>
      <PageHeader breadcrumb="Tuyển dụng / Yêu cầu tuyển dụng" title="Yêu cầu tuyển dụng & đăng tin (Job Requisitions)" />

      {actionError && (
        <div className="mb-4">
          <StateBanner error={actionError} />
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-[#edf0f4] bg-white shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
        <RequisitionToolbar departments={departments} onChange={updateFilters} onCreate={() => setCreateOpen(true)} />

        {(loading && !data) || error ? (
          <div className="p-5">
            <StateBanner loading={loading && !data} error={error} onRetry={reload} loadingLabel="Đang tải danh sách tin tuyển dụng…" />
          </div>
        ) : (
          data && (
            <>
              <div className="overflow-x-auto hrm-scroll">
                <RequisitionTable
                  requisitions={data.items}
                  departmentName={departmentName}
                  busyRequisitionId={busyRequisitionId}
                  onAction={(requisition, action) => void handleAction(requisition, action)}
                />
              </div>
              <Pagination page={data.page} itemLabel="tin tuyển dụng" onPageChange={setPage} />
            </>
          )
        )}
      </div>

      <NewRequisitionModal
        open={createOpen}
        departments={departments}
        busy={createBusy}
        error={createError}
        onClose={() => setCreateOpen(false)}
        onSubmit={(write) => {
          setCreateBusy(true);
          setCreateError(undefined);
          createRequisition(write)
            .then((requisition) => {
              upsert(requisition);
              setCreateOpen(false);
            })
            .catch((cause) => setCreateError(cause instanceof Error ? cause.message : "Không thể tạo tin tuyển dụng."))
            .finally(() => setCreateBusy(false));
        }}
      />

      <ReasonModal
        key={pendingRejection?.id ?? "none"}
        open={pendingRejection !== undefined}
        title="Từ chối tin tuyển dụng"
        subtitle={pendingRejection ? `${pendingRejection.title} (${pendingRejection.jobCode})` : undefined}
        busy={busyRequisitionId === pendingRejection?.id}
        onClose={() => setPendingRejection(undefined)}
        onConfirm={(reason) => {
          if (!pendingRejection) return;
          setBusyRequisitionId(pendingRejection.id);
          performRequisitionAction(pendingRejection, "reject", reason)
            .then((requisition) => {
              upsert(requisition);
              setPendingRejection(undefined);
            })
            .catch((cause) => setActionError(cause instanceof Error ? cause.message : "Không thể từ chối tin tuyển dụng."))
            .finally(() => setBusyRequisitionId(undefined));
        }}
      />
    </div>
  );
}
