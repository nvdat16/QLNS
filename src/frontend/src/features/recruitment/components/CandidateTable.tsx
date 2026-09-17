import { IconButton } from "../../../components/IconButton";
import { candidateFullName } from "../api/candidates";
import { nextStage } from "../api/recruitmentApplications";
import type { PipelineCard } from "../api/pipelineApi";
import { StageBadge } from "./StageBadge";

interface Props {
  cards: PipelineCard[];
  busyApplicationId: number | undefined;
  onSelect: (card: PipelineCard) => void;
  onAdvance: (card: PipelineCard) => void;
  onReject: (card: PipelineCard) => void;
}

export function CandidateTable({ cards, busyApplicationId, onSelect, onAdvance, onReject }: Props) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 bg-[#fafbfc] text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Mã ứng viên</th>
          <th className="px-5 py-3">Họ và tên ứng viên</th>
          <th className="px-5 py-3">AI match score</th>
          <th className="px-5 py-3">Nguồn ứng tuyển</th>
          <th className="px-5 py-3 text-center">Giai đoạn</th>
          <th className="px-5 py-3 text-right">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        {cards.map(({ application, candidate }) => {
          const target = nextStage(application.stage);
          const busy = busyApplicationId === application.id;

          return (
            <tr
              key={application.id}
              onClick={() => onSelect({ application, candidate })}
              className="cursor-pointer border-b border-[#eef0f4] last:border-0 hover:bg-slate-50"
            >
              <td className="px-5 py-3.5 font-mono text-xs font-semibold text-brand-600">CAND-{candidate.id}</td>
              <td className="px-5 py-3.5">
                <p className="font-semibold text-slate-800">{candidateFullName(candidate)}</p>
                <p className="text-[11px] text-slate-400">{candidate.email}</p>
              </td>
              <td className="px-5 py-3.5">
                {application.aiScore !== undefined ? (
                  <span className={`inline-flex items-center gap-1 font-semibold ${application.aiScore >= 90 ? "text-brand-600" : "text-slate-700"}`}>
                    <span className="material-symbols-outlined icon-sm">auto_awesome</span>
                    {Math.round(application.aiScore)}% Match
                  </span>
                ) : (
                  <span className="text-slate-400">—</span>
                )}
              </td>
              <td className="px-5 py-3.5 text-slate-600">{application.source}</td>
              <td className="px-5 py-3.5 text-center">
                <StageBadge stage={application.stage} />
              </td>
              <td className="px-5 py-3.5">
                <div className="flex justify-end gap-1.5">
                  {target && (
                    <IconButton
                      icon="arrow_forward"
                      label={`Chuyển sang giai đoạn kế tiếp`}
                      tone="brand"
                      disabled={busy}
                      onClick={() => onAdvance({ application, candidate })}
                    />
                  )}
                  <IconButton
                    icon="close"
                    label="Từ chối ứng viên"
                    disabled={busy}
                    onClick={() => onReject({ application, candidate })}
                  />
                </div>
              </td>
            </tr>
          );
        })}
        {cards.length === 0 && (
          <tr>
            <td colSpan={6} className="px-5 py-10 text-center text-sm text-slate-400">
              Không có ứng viên nào phù hợp với bộ lọc.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
