import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { PageHeader } from "../../../components/PageHeader";
import { ReasonModal } from "../../../components/ReasonModal";
import { StateBanner } from "../../../components/StateBanner";
import type { PipelineCard } from "../api/pipelineApi";
import { AddCandidateModal } from "../components/AddCandidateModal";
import { CandidateDetailDrawer } from "../components/CandidateDetailDrawer";
import { CandidateKanbanBoard } from "../components/CandidateKanbanBoard";
import { CandidateTable } from "../components/CandidateTable";
import { PipelineToolbar, type PipelineView } from "../components/PipelineToolbar";
import { useActiveRequisitions } from "../hooks/useActiveRequisitions";
import { useApplicationAction } from "../hooks/useApplicationAction";
import { usePipeline, type PipelineFilterState } from "../hooks/usePipeline";

export function PipelinePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { requisitions, loading: loadingRequisitions } = useActiveRequisitions();
  const [filters, setFilters] = useState<PipelineFilterState>({});
  const [view, setView] = useState<PipelineView>("table");
  const [selectedCard, setSelectedCard] = useState<PipelineCard>();
  const [rejecting, setRejecting] = useState<PipelineCard>();
  const [addCandidateOpen, setAddCandidateOpen] = useState(false);

  const requisitionId = searchParams.get("requisitionId") ? Number(searchParams.get("requisitionId")) : requisitions[0]?.id;

  useEffect(() => {
    if (!searchParams.get("requisitionId") && requisitions[0]) {
      setSearchParams({ requisitionId: String(requisitions[0].id) }, { replace: true });
    }
  }, [requisitions, searchParams, setSearchParams]);

  const { pipeline, loading, error, reload } = usePipeline(requisitionId, filters);
  const { advance, reject, busyApplicationId, error: actionError } = useApplicationAction(reload);

  const allCards = pipeline?.columns.flatMap((column) => column.items) ?? [];

  return (
    <div>
      <PageHeader breadcrumb="Tuyển dụng / Quy trình tuyển dụng" title="Quản lý tuyển dụng & hồ sơ ứng viên" />

      {actionError && (
        <div className="mb-4">
          <StateBanner error={actionError} />
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-[#edf0f4] bg-white shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
        <PipelineToolbar
          requisitions={requisitions}
          requisitionId={requisitionId}
          onRequisitionChange={(id) => setSearchParams({ requisitionId: String(id) })}
          onSearchChange={(search) => setFilters((prev) => ({ ...prev, search }))}
          onStageChange={(stage) => setFilters((prev) => ({ ...prev, stage }))}
          view={view}
          onViewChange={setView}
          onAddCandidate={() => setAddCandidateOpen(true)}
        />

        {(loading && !pipeline) || error || loadingRequisitions ? (
          <div className="p-5">
            <StateBanner
              loading={(loading && !pipeline) || loadingRequisitions}
              error={error}
              onRetry={reload}
              loadingLabel="Đang tải quy trình tuyển dụng…"
            />
          </div>
        ) : pipeline ? (
          view === "table" ? (
            <div className="overflow-x-auto hrm-scroll">
              <CandidateTable
                cards={allCards}
                busyApplicationId={busyApplicationId}
                onSelect={setSelectedCard}
                onAdvance={(card) => void advance(card.application)}
                onReject={setRejecting}
              />
            </div>
          ) : (
            <CandidateKanbanBoard
              columns={pipeline.columns}
              busyApplicationId={busyApplicationId}
              onSelect={setSelectedCard}
              onAdvance={(card) => void advance(card.application)}
            />
          )
        ) : (
          <p className="p-5 text-sm text-slate-400">Chưa có vị trí tuyển dụng nào đang mở.</p>
        )}
      </div>

      <CandidateDetailDrawer card={selectedCard} onClose={() => setSelectedCard(undefined)} />

      {requisitionId !== undefined && (
        <AddCandidateModal
          open={addCandidateOpen}
          requisitionId={requisitionId}
          onClose={() => setAddCandidateOpen(false)}
          onCreated={reload}
        />
      )}

      <ReasonModal
        key={rejecting?.application.id ?? "none"}
        open={rejecting !== undefined}
        title="Từ chối ứng viên"
        subtitle={rejecting ? rejecting.candidate.email : undefined}
        busy={busyApplicationId === rejecting?.application.id}
        onClose={() => setRejecting(undefined)}
        onConfirm={(reason) => {
          if (!rejecting) return;
          void reject(rejecting.application, reason).then(() => setRejecting(undefined));
        }}
      />
    </div>
  );
}
