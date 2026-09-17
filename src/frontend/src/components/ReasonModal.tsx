import { useState } from "react";
import { Modal } from "./Modal";

interface Props {
  open: boolean;
  title: string;
  subtitle?: string;
  busy: boolean;
  confirmLabel?: string;
  onConfirm: (reason: string) => void;
  onClose: () => void;
}

export function ReasonModal({ open, title, subtitle, busy, confirmLabel = "Xác nhận", onConfirm, onClose }: Props) {
  const [reason, setReason] = useState("");

  if (!open) return null;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      subtitle={subtitle}
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100">
            Huỷ
          </button>
          <button
            type="button"
            disabled={busy || reason.trim().length === 0}
            onClick={() => onConfirm(reason.trim())}
            className="rounded-lg bg-rose-600 px-4 py-2 text-sm font-semibold text-white hover:bg-rose-700 disabled:opacity-60"
          >
            {busy ? "Đang xử lý…" : confirmLabel}
          </button>
        </>
      }
    >
      <label className="block">
        <span className="mb-1.5 block text-sm font-medium text-slate-700">Lý do (bắt buộc)</span>
        <textarea
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          rows={3}
          className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </label>
    </Modal>
  );
}
