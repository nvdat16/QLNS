import { useState } from "react";
import { Modal } from "../../../components/Modal";
import type { OfferWrite } from "../api/offersApi";
import { employmentTypes } from "../api/requisitionsApi";
import { employmentTypeLabels } from "./requisitionStatusBadge";

interface Props {
  open: boolean;
  busy: boolean;
  error?: string;
  onClose: () => void;
  onSubmit: (write: OfferWrite) => void;
}

export function NewOfferModal({ open, busy, error, onClose, onSubmit }: Props) {
  const [applicationId, setApplicationId] = useState("");
  const [baseSalary, setBaseSalary] = useState("");
  const [bonusAmount, setBonusAmount] = useState("");
  const [employmentType, setEmploymentType] = useState<OfferWrite["employmentType"]>("full_time");
  const [startDate, setStartDate] = useState("");
  const [expirationDate, setExpirationDate] = useState("");

  const canSubmit = applicationId && baseSalary && startDate && expirationDate;

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Soạn thảo thư mời nhận việc (Offer)"
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 hover:bg-slate-100">
            Huỷ
          </button>
          <button
            type="button"
            disabled={!canSubmit || busy}
            onClick={() =>
              onSubmit({
                applicationId: Number(applicationId),
                baseSalary: Number(baseSalary),
                bonusAmount: bonusAmount ? Number(bonusAmount) : undefined,
                employmentType,
                startDate,
                expirationDate,
                templateVersion: "v1",
              })
            }
            className="rounded-lg bg-brand-600 py-2 px-4 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {busy ? "Đang tạo…" : "Tạo Offer"}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Mã hồ sơ ứng tuyển (Application ID) *</span>
          <input
            type="number"
            value={applicationId}
            onChange={(event) => setApplicationId(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:border-brand-500 focus:outline-none"
          />
        </label>

        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Lương cơ bản đề xuất (VNĐ) *</span>
            <input
              type="number"
              value={baseSalary}
              onChange={(event) => setBaseSalary(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Thưởng gia nhập (VNĐ)</span>
            <input
              type="number"
              value={bonusAmount}
              onChange={(event) => setBonusAmount(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
        </div>

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Hình thức làm việc</span>
          <select
            value={employmentType}
            onChange={(event) => setEmploymentType(event.target.value as OfferWrite["employmentType"])}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
          >
            {employmentTypes.map((type) => (
              <option key={type} value={type}>
                {employmentTypeLabels[type]}
              </option>
            ))}
          </select>
        </label>

        <div className="grid grid-cols-2 gap-4">
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Ngày bắt đầu dự kiến *</span>
            <input
              type="date"
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Hạn phản hồi *</span>
            <input
              type="date"
              value={expirationDate}
              onChange={(event) => setExpirationDate(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            />
          </label>
        </div>
      </div>
    </Modal>
  );
}
