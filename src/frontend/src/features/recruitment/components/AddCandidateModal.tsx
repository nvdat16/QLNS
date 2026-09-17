import { useEffect, useState } from "react";
import { Modal } from "../../../components/Modal";
import type { CandidateInput } from "../api/candidates";
import { useCandidateIntake } from "../hooks/useCandidateIntake";

interface Props {
  open: boolean;
  requisitionId: number;
  onClose: () => void;
  onCreated: () => void;
}

export function AddCandidateModal({ open, requisitionId, onClose, onCreated }: Props) {
  const { phase, intake, error, createdApplicationId, submit, confirm, reset } = useCandidateIntake(requisitionId);
  const [file, setFile] = useState<File>();
  const [consented, setConsented] = useState(false);
  const [candidate, setCandidate] = useState<CandidateInput>({ firstName: "", lastName: "", email: "" });

  useEffect(() => {
    if (!open) reset();
  }, [open, reset]);

  useEffect(() => {
    if (intake?.parsedCandidate) setCandidate(intake.parsedCandidate);
  }, [intake]);

  useEffect(() => {
    if (phase === "done") {
      onCreated();
      onClose();
    }
  }, [phase, onCreated, onClose]);

  if (!open) return null;

  const showConfirmationForm = intake && intake.status !== "scanning" && intake.status !== "parsing";

  return (
    <Modal open={open} onClose={onClose} title="Tiếp nhận hồ sơ ứng viên mới" width="lg">
      <div className="space-y-4">
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}

        {!intake && (
          <>
            <label className="block">
              <span className="mb-1.5 block text-sm font-medium text-slate-700">CV ứng viên (PDF/DOC/DOCX) *</span>
              <input
                type="file"
                accept=".pdf,.doc,.docx"
                onChange={(event) => setFile(event.target.files?.[0])}
                className="w-full rounded-lg border border-dashed border-slate-300 px-3 py-6 text-sm text-slate-500"
              />
            </label>

            <label className="flex items-start gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                checked={consented}
                onChange={(event) => setConsented(event.target.checked)}
                className="mt-0.5"
              />
              Ứng viên đã đồng ý với chính sách xử lý dữ liệu cá nhân (privacy notice) của công ty.
            </label>

            <button
              type="button"
              disabled={!file || !consented || phase === "uploading"}
              onClick={() => file && void submit(file, "v1")}
              className="w-full rounded-lg bg-brand-600 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
            >
              {phase === "uploading" ? "Đang tải lên & phân tích CV (AI Parsing)…" : "Tải lên & phân tích CV"}
            </button>
          </>
        )}

        {showConfirmationForm && (
          <div className="space-y-3 rounded-xl border border-slate-100 p-4">
            <p className="text-sm font-semibold text-slate-700">Xác nhận thông tin đã trích xuất từ CV</p>

            {intake.duplicateCandidates && intake.duplicateCandidates.length > 0 && (
              <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
                Phát hiện {intake.duplicateCandidates.length} hồ sơ ứng viên trùng khớp trong hệ thống. Vui lòng kiểm
                tra trước khi xác nhận.
              </p>
            )}

            <div className="grid grid-cols-2 gap-3">
              <Field label="Họ" value={candidate.lastName} onChange={(v) => setCandidate((c) => ({ ...c, lastName: v }))} />
              <Field label="Tên" value={candidate.firstName} onChange={(v) => setCandidate((c) => ({ ...c, firstName: v }))} />
            </div>
            <Field label="Email" value={candidate.email} onChange={(v) => setCandidate((c) => ({ ...c, email: v }))} type="email" />
            <Field label="Số điện thoại" value={candidate.phone ?? ""} onChange={(v) => setCandidate((c) => ({ ...c, phone: v }))} />

            <button
              type="button"
              disabled={phase === "confirming" || !candidate.firstName || !candidate.lastName || !candidate.email}
              onClick={() => void confirm(candidate)}
              className="w-full rounded-lg bg-brand-600 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
            >
              {phase === "confirming" ? "Đang thêm vào pipeline…" : "Lưu & thêm vào Pipeline"}
            </button>
          </div>
        )}

        {createdApplicationId && (
          <p className="rounded-lg bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
            Đã thêm ứng viên vào pipeline (hồ sơ #{createdApplicationId}).
          </p>
        )}
      </div>
    </Modal>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-slate-500">{label}</span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
      />
    </label>
  );
}
