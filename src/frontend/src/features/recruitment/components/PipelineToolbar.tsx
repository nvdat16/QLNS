import type { Requisition } from "../api/requisitionsApi";
import type { RecruitmentStage } from "../api/recruitmentApplications";
import { stageFilterOptions, stageLabels } from "./stagePresentation";

export type PipelineView = "table" | "kanban";

interface Props {
  requisitions: Requisition[];
  requisitionId: number | undefined;
  onRequisitionChange: (id: number) => void;
  onSearchChange: (search: string | undefined) => void;
  onStageChange: (stage: RecruitmentStage | undefined) => void;
  view: PipelineView;
  onViewChange: (view: PipelineView) => void;
  onAddCandidate: () => void;
}

export function PipelineToolbar({
  requisitions,
  requisitionId,
  onRequisitionChange,
  onSearchChange,
  onStageChange,
  view,
  onViewChange,
  onAddCandidate,
}: Props) {
  return (
    <div className="flex flex-wrap items-center gap-3 border-b border-slate-100 px-5 py-4">
      <select
        value={requisitionId ?? ""}
        onChange={(event) => onRequisitionChange(Number(event.target.value))}
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="" disabled>
          Vị trí tuyển dụng
        </option>
        {requisitions.map((requisition) => (
          <option key={requisition.id} value={requisition.id}>
            {requisition.title} ({requisition.jobCode})
          </option>
        ))}
      </select>

      <div className="relative min-w-[200px] flex-1">
        <span className="material-symbols-outlined icon-sm absolute left-3 top-1/2 -translate-y-1/2 text-slate-400">
          search
        </span>
        <input
          type="search"
          placeholder="Tìm ứng viên..."
          onChange={(event) => onSearchChange(event.target.value || undefined)}
          className="w-full rounded-lg border border-slate-200 py-2 pl-9 pr-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </div>

      <select
        onChange={(event) => onStageChange((event.target.value || undefined) as RecruitmentStage | undefined)}
        className="rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-600 focus:border-brand-500 focus:outline-none"
      >
        <option value="">Giai đoạn: Tất cả</option>
        {stageFilterOptions.map((stage) => (
          <option key={stage} value={stage}>
            {stageLabels[stage]}
          </option>
        ))}
      </select>

      <div className="flex rounded-lg border border-slate-200 p-0.5">
        <button
          type="button"
          onClick={() => onViewChange("table")}
          className={`flex items-center gap-1 rounded-md px-3 py-1.5 text-xs font-semibold ${view === "table" ? "bg-brand-600 text-white" : "text-slate-500"}`}
        >
          <span className="material-symbols-outlined icon-sm">table_rows</span>
          Danh sách
        </button>
        <button
          type="button"
          onClick={() => onViewChange("kanban")}
          className={`flex items-center gap-1 rounded-md px-3 py-1.5 text-xs font-semibold ${view === "kanban" ? "bg-brand-600 text-white" : "text-slate-500"}`}
        >
          <span className="material-symbols-outlined icon-sm">view_kanban</span>
          Kanban
        </button>
      </div>

      <button
        type="button"
        onClick={onAddCandidate}
        className="ml-auto flex items-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
      >
        <span className="material-symbols-outlined icon-sm">person_add</span>
        Tiếp nhận
      </button>
    </div>
  );
}
