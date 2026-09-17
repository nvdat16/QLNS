import { Link } from "react-router-dom";
import { formatCurrency, formatDate } from "../../../components/format";
import { candidateFullName } from "../api/candidates";
import type { Offer, OfferLifecycleAction } from "../api/offersApi";
import type { ApplicationCandidate } from "../hooks/useApplicationCandidates";
import { OfferStatusBadge } from "./offerStatusBadge";

interface Props {
  offers: Offer[];
  candidates: Map<number, ApplicationCandidate>;
  titleFor: (jobPostingId: number) => string;
  busyOfferId: number | undefined;
  onAction: (offer: Offer, action: OfferLifecycleAction) => void;
  onHandoff: (offer: Offer) => void;
}

export function OfferTable({ offers, candidates, titleFor, busyOfferId, onAction, onHandoff }: Props) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 bg-[#fafbfc] text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Ứng viên</th>
          <th className="px-5 py-3">Vị trí</th>
          <th className="px-5 py-3">Lương &amp; đãi ngộ</th>
          <th className="px-5 py-3">Ngày bắt đầu</th>
          <th className="px-5 py-3">Hạn phản hồi</th>
          <th className="px-5 py-3 text-center">Trạng thái</th>
          <th className="px-5 py-3 text-right">Tiếp nhận</th>
        </tr>
      </thead>
      <tbody>
        {offers.map((offer) => {
          const resolved = candidates.get(offer.applicationId);
          const busy = busyOfferId === offer.id;

          return (
            <tr key={offer.id} className={`border-b border-[#eef0f4] last:border-0 hover:bg-slate-50 ${offer.status === "accepted" ? "bg-emerald-50/20" : ""}`}>
              <td className="px-5 py-3.5">
                <p className="flex items-center gap-1.5 font-semibold text-slate-800">
                  {offer.status === "accepted" && <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />}
                  {resolved?.candidate ? candidateFullName(resolved.candidate) : `Ứng viên #${offer.applicationId}`}
                </p>
              </td>
              <td className="px-5 py-3.5 text-slate-600">{titleFor(resolved?.application.jobPostingId ?? 0)}</td>
              <td className="px-5 py-3.5">
                <p className="font-semibold text-slate-800">{formatCurrency(offer.baseSalary, offer.currency)}</p>
                {offer.bonusAmount && (
                  <p className={`text-[11px] ${offer.status === "accepted" ? "text-emerald-600" : "text-blue-600"}`}>
                    +{formatCurrency(offer.bonusAmount, offer.currency)} thưởng
                  </p>
                )}
              </td>
              <td className="px-5 py-3.5 text-slate-600">{formatDate(offer.startDate)}</td>
              <td className="px-5 py-3.5 text-slate-600">{formatDate(offer.expirationDate)}</td>
              <td className="px-5 py-3.5 text-center">
                <OfferStatusBadge status={offer.status} />
              </td>
              <td className="px-5 py-3.5 text-right">
                {offer.status === "accepted" ? (
                  <Link
                    to="/employees"
                    className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-emerald-700"
                  >
                    <span className="material-symbols-outlined icon-sm">how_to_reg</span>
                    Tiếp nhận
                  </Link>
                ) : offer.status === "sent" ? (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => onHandoff(offer)}
                    className="rounded-lg border border-emerald-200 px-3 py-1.5 text-xs font-semibold text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                  >
                    {busy ? "Đang xử lý…" : "Mô phỏng ứng viên ký"}
                  </button>
                ) : (
                  <div className="flex justify-end gap-1.5">
                    {offer.status === "draft" && (
                      <ActionButton label="Duyệt" busy={busy} onClick={() => onAction(offer, "approve")} />
                    )}
                    {offer.status === "approved" && (
                      <ActionButton label="Gửi Offer" busy={busy} onClick={() => onAction(offer, "send")} />
                    )}
                    {(offer.status === "draft" || offer.status === "approved") && (
                      <ActionButton label="Huỷ" tone="rose" busy={busy} onClick={() => onAction(offer, "cancel")} />
                    )}
                  </div>
                )}
              </td>
            </tr>
          );
        })}
        {offers.length === 0 && (
          <tr>
            <td colSpan={7} className="px-5 py-10 text-center text-sm text-slate-400">
              Chưa có thư mời nhận việc nào.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}

function ActionButton({
  label,
  onClick,
  busy,
  tone = "brand",
}: {
  label: string;
  onClick: () => void;
  busy: boolean;
  tone?: "brand" | "rose";
}) {
  const toneClass = tone === "rose" ? "border-rose-200 text-rose-700 hover:bg-rose-50" : "border-brand-200 text-brand-600 hover:bg-brand-50";
  return (
    <button
      type="button"
      disabled={busy}
      onClick={onClick}
      className={`rounded-lg border px-3 py-1.5 text-xs font-semibold disabled:opacity-50 ${toneClass}`}
    >
      {label}
    </button>
  );
}
