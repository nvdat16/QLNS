import { useState } from "react";
import { Modal } from "../../../components/Modal";
import { recommendations, type EvaluationWrite, type Recommendation, type Score } from "../api/evaluationsApi";

const recommendationLabels: Record<Recommendation, string> = {
  strong_hire: "Rất nên tuyển (Strong Hire)",
  hire: "Nên tuyển (Hire)",
  hold: "Cân nhắc thêm (Hold)",
  no_hire: "Không tuyển (No Hire)",
  strong_no_hire: "Rất không nên tuyển (Strong No Hire)",
};

const criteria: { key: keyof Omit<EvaluationWrite, "recommendation" | "feedback">; label: string }[] = [
  { key: "technicalScore", label: "Năng lực chuyên môn & kỹ thuật" },
  { key: "communicationScore", label: "Kỹ năng giao tiếp" },
  { key: "problemSolvingScore", label: "Tư duy giải quyết vấn đề" },
  { key: "teamworkScore", label: "Tinh thần đồng đội & văn hoá" },
];

interface Props {
  open: boolean;
  subtitle?: string;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSubmit: (write: EvaluationWrite) => void;
}

export function ScorecardModal({ open, subtitle, busy, error, onClose, onSubmit }: Props) {
  const [scores, setScores] = useState<Record<string, Score>>({
    technicalScore: 4,
    communicationScore: 4,
    problemSolvingScore: 4,
    teamworkScore: 4,
  });
  const [recommendation, setRecommendation] = useState<Recommendation>("hire");
  const [feedback, setFeedback] = useState("");

  if (!open) return null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Chấm điểm phỏng vấn (Scorecard)"
      subtitle={subtitle}
      width="lg"
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100">
            Đóng
          </button>
          <button
            type="button"
            disabled={busy || feedback.trim().length === 0}
            onClick={() =>
              onSubmit({
                technicalScore: scores.technicalScore,
                communicationScore: scores.communicationScore,
                problemSolvingScore: scores.problemSolvingScore,
                teamworkScore: scores.teamworkScore,
                recommendation,
                feedback: feedback.trim(),
              })
            }
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {busy ? "Đang lưu…" : "Lưu đánh giá"}
          </button>
        </>
      }
    >
      <div className="space-y-5">
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}

        {criteria.map(({ key, label }) => (
          <div key={key}>
            <div className="mb-1 flex items-center justify-between">
              <span className="text-sm font-medium text-slate-700">{label}</span>
              <span className="rounded-full bg-slate-900 px-2 py-0.5 text-xs font-bold text-white">
                {scores[key].toFixed(1)}
              </span>
            </div>
            <input
              type="range"
              min={0}
              max={5}
              step={0.5}
              value={scores[key]}
              onChange={(event) => setScores((prev) => ({ ...prev, [key]: Number(event.target.value) }))}
              className="w-full accent-brand-600"
            />
            <div className="flex justify-between text-[11px] text-slate-400">
              <span>0.0 · Chưa đạt</span>
              <span>3.0 · Đạt yêu cầu</span>
              <span>5.0 · Xuất sắc</span>
            </div>
          </div>
        ))}

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Khuyến nghị</span>
          <select
            value={recommendation}
            onChange={(event) => setRecommendation(event.target.value as Recommendation)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
          >
            {recommendations.map((option) => (
              <option key={option} value={option}>
                {recommendationLabels[option]}
              </option>
            ))}
          </select>
        </label>

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Nhận xét chi tiết *</span>
          <textarea
            value={feedback}
            onChange={(event) => setFeedback(event.target.value)}
            rows={4}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>
      </div>
    </Modal>
  );
}
