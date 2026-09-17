import { Drawer } from "../../../components/Drawer";
import { candidateFullName } from "../api/candidates";
import type { PipelineCard } from "../api/pipelineApi";
import { StageBadge } from "./StageBadge";

interface Props {
  card: PipelineCard | undefined;
  onClose: () => void;
}

export function CandidateDetailDrawer({ card, onClose }: Props) {
  return (
    <Drawer open={card !== undefined} onClose={onClose}>
      {card && (
        <div className="space-y-6">
          <div>
            <p className="font-mono text-xs font-semibold text-brand-600">CAND-{card.candidate.id}</p>
            <h3 className="mt-1 text-lg font-bold text-slate-900">{candidateFullName(card.candidate)}</h3>
            <div className="mt-2">
              <StageBadge stage={card.application.stage} />
            </div>
          </div>

          <section className="rounded-xl bg-slate-50 p-4">
            <h4 className="mb-3 text-xs font-bold uppercase tracking-wide text-slate-400">Thông tin liên hệ</h4>
            <dl className="space-y-2 text-sm">
              <Row label="Email" value={card.candidate.email} mono />
              <Row label="Số điện thoại" value={card.candidate.phone ?? "—"} />
              {card.candidate.linkedinUrl && <Row label="LinkedIn" value={card.candidate.linkedinUrl} mono />}
              {card.candidate.portfolioUrl && <Row label="Portfolio" value={card.candidate.portfolioUrl} mono />}
            </dl>
          </section>

          <section className="rounded-xl border border-slate-100 p-4">
            <h4 className="mb-3 text-xs font-bold uppercase tracking-wide text-slate-400">Thông tin ứng tuyển</h4>
            <dl className="space-y-2 text-sm">
              <Row label="Nguồn ứng tuyển" value={card.application.source} />
              <Row
                label="AI Match Score"
                value={card.application.aiScore !== undefined ? `${Math.round(card.application.aiScore)}%` : "Chưa chấm điểm"}
              />
              <Row label="Ngày ứng tuyển" value={new Date(card.application.appliedAt).toLocaleDateString("vi-VN")} />
            </dl>
          </section>
        </div>
      )}
    </Drawer>
  );
}

function Row({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-500">{label}</dt>
      <dd className={`truncate text-right font-medium text-slate-800 ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}
