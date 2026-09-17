import { Link } from "react-router-dom";
import { candidateFullName } from "../api/candidates";
import { nextStage } from "../api/recruitmentApplications";
import type { PipelineCard, PipelineColumn } from "../api/pipelineApi";
import { stageLabels } from "./stagePresentation";

const dotClasses: Record<string, string> = {
  sourced_applied: "bg-slate-400",
  ai_screening: "bg-blue-500",
  tech_interview: "bg-amber-500",
  executive_round: "bg-purple-500",
  offer_letter: "bg-rose-500",
  hired_ready: "bg-emerald-500",
};

interface Props {
  columns: PipelineColumn[];
  busyApplicationId: number | undefined;
  onSelect: (card: PipelineCard) => void;
  onAdvance: (card: PipelineCard) => void;
}

export function CandidateKanbanBoard({ columns, busyApplicationId, onSelect, onAdvance }: Props) {
  return (
    <div className="flex gap-4 overflow-x-auto hrm-scroll p-5">
      {columns.map((column) => (
        <div key={column.stage} className="w-72 shrink-0 rounded-xl bg-slate-50 p-3">
          <div className="mb-3 flex items-center gap-2 px-1">
            <span className={`h-2 w-2 rounded-full ${dotClasses[column.stage]}`} />
            <h3 className="text-xs font-bold uppercase tracking-wide text-slate-600">{stageLabels[column.stage]}</h3>
            <span className="ml-auto rounded-full bg-slate-200 px-2 py-0.5 text-[11px] font-bold text-slate-700">
              {column.totalItems}
            </span>
          </div>

          <div className="space-y-2">
            {column.items.map((card) => {
              const target = nextStage(card.application.stage);
              const busy = busyApplicationId === card.application.id;

              return (
                <div
                  key={card.application.id}
                  onClick={() => onSelect(card)}
                  className="cursor-pointer rounded-lg border border-slate-100 bg-white p-3 shadow-sm hover:border-brand-200"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-mono text-[11px] font-semibold text-brand-600">CAND-{card.candidate.id}</span>
                    {card.application.aiScore !== undefined && (
                      <span className="text-[11px] font-semibold text-slate-500">{Math.round(card.application.aiScore)}%</span>
                    )}
                  </div>
                  <p className="mt-1 text-sm font-semibold text-slate-800">{candidateFullName(card.candidate)}</p>
                  <p className="text-[11px] text-slate-400">{card.application.source}</p>

                  {column.stage === "hired_ready" ? (
                    <Link
                      to="/employees"
                      onClick={(event) => event.stopPropagation()}
                      className="mt-2 flex items-center justify-center gap-1.5 rounded-lg bg-emerald-600 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700"
                    >
                      <span className="material-symbols-outlined icon-sm">how_to_reg</span>
                      Tiếp nhận
                    </Link>
                  ) : (
                    target && (
                      <button
                        type="button"
                        disabled={busy}
                        onClick={(event) => {
                          event.stopPropagation();
                          onAdvance(card);
                        }}
                        className="mt-2 w-full rounded-lg bg-brand-50 py-1.5 text-xs font-semibold text-brand-700 hover:bg-brand-100 disabled:opacity-50"
                      >
                        Chuyển sang {stageLabels[target]} →
                      </button>
                    )
                  )}
                </div>
              );
            })}
            {column.items.length === 0 && <p className="px-2 py-3 text-center text-xs text-slate-400">Trống</p>}
          </div>
        </div>
      ))}
    </div>
  );
}
