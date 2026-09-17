import { useState } from "react";
import { PageHeader } from "../../../components/PageHeader";
import { StateBanner } from "../../../components/StateBanner";
import { createOffer, performOfferAction, type Offer, type OfferLifecycleAction, type OfferWrite } from "../api/offersApi";
import { acceptOfferAsCandidate } from "../api/onboardingHandoff";
import { NewOfferModal } from "../components/NewOfferModal";
import { OfferTable } from "../components/OfferTable";
import { useApplicationCandidates } from "../hooks/useApplicationCandidates";
import { useOffers } from "../hooks/useOffers";
import { useRequisitionTitles } from "../hooks/useRequisitionTitles";

export function OffersPage() {
  const { data, loading, error, reload, upsert } = useOffers();
  const { entries: candidates } = useApplicationCandidates(data?.items.map((offer) => offer.applicationId) ?? []);
  const { titleFor } = useRequisitionTitles(
    [...candidates.values()].map((entry) => entry.application.jobPostingId),
  );

  const [createOpen, setCreateOpen] = useState(false);
  const [createBusy, setCreateBusy] = useState(false);
  const [createError, setCreateError] = useState<string>();
  const [busyOfferId, setBusyOfferId] = useState<number>();
  const [actionError, setActionError] = useState<string>();
  const [handoffResult, setHandoffResult] = useState<{ employeeId?: number; initialContractId?: number }>();

  function handleAction(offer: Offer, action: OfferLifecycleAction) {
    setBusyOfferId(offer.id);
    setActionError(undefined);
    performOfferAction(offer, action)
      .then(upsert)
      .catch((cause) => setActionError(cause instanceof Error ? cause.message : "Không thể cập nhật thư mời."))
      .finally(() => setBusyOfferId(undefined));
  }

  function handleHandoff(offer: Offer) {
    setBusyOfferId(offer.id);
    setActionError(undefined);
    acceptOfferAsCandidate(offer.id)
      .then((result) => {
        upsert({ ...offer, status: "accepted" });
        setHandoffResult({ employeeId: result.employeeId, initialContractId: result.initialContractId });
      })
      .catch((cause) => setActionError(cause instanceof Error ? cause.message : "Không thể mô phỏng phản hồi ứng viên."))
      .finally(() => setBusyOfferId(undefined));
  }

  return (
    <div>
      <PageHeader
        breadcrumb="Tuyển dụng / Offers & Onboarding"
        title="Quản lý thư mời nhận việc & tiếp nhận nhân sự"
        actions={
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="flex items-center gap-1.5 rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white hover:bg-brand-700"
          >
            <span className="material-symbols-outlined icon-sm">add</span>
            Soạn Offer mới
          </button>
        }
      />

      {actionError && (
        <div className="mb-4">
          <StateBanner error={actionError} />
        </div>
      )}

      {handoffResult && (
        <div className="mb-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          Đã tiếp nhận thành công — tạo hồ sơ nhân viên #{handoffResult.employeeId} và hợp đồng ban đầu #
          {handoffResult.initialContractId}. Xem chi tiết tại trang Hồ sơ nhân viên.
        </div>
      )}

      <div className="overflow-hidden rounded-2xl border border-[#edf0f4] bg-white shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
        {(loading && !data) || error ? (
          <div className="p-5">
            <StateBanner loading={loading && !data} error={error} onRetry={reload} loadingLabel="Đang tải danh sách thư mời…" />
          </div>
        ) : (
          data && (
            <div className="overflow-x-auto hrm-scroll">
              <OfferTable
                offers={data.items}
                candidates={candidates}
                titleFor={titleFor}
                busyOfferId={busyOfferId}
                onAction={handleAction}
                onHandoff={handleHandoff}
              />
            </div>
          )
        )}
      </div>

      <NewOfferModal
        open={createOpen}
        busy={createBusy}
        error={createError}
        onClose={() => setCreateOpen(false)}
        onSubmit={(write: OfferWrite) => {
          setCreateBusy(true);
          setCreateError(undefined);
          createOffer(write)
            .then((offer) => {
              upsert(offer);
              setCreateOpen(false);
            })
            .catch((cause) => setCreateError(cause instanceof Error ? cause.message : "Không thể tạo thư mời nhận việc."))
            .finally(() => setCreateBusy(false));
        }}
      />
    </div>
  );
}
