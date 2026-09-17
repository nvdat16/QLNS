import { useState } from "react";
import { PageHeader } from "../../../components/PageHeader";
import { ReasonModal } from "../../../components/ReasonModal";
import { StateBanner } from "../../../components/StateBanner";
import { useEmployeeNames } from "../../employees/hooks/useEmployeeNames";
import { performInterviewAction, type Interview, type InterviewLifecycleAction } from "../api/interviewsApi";
import { submitEvaluation, type EvaluationWrite } from "../api/evaluationsApi";
import { candidateFullName } from "../api/candidates";
import { InterviewList } from "../components/InterviewList";
import { ScorecardModal } from "../components/ScorecardModal";
import { useApplicationCandidates } from "../hooks/useApplicationCandidates";
import { useInterviews } from "../hooks/useInterviews";

const rubric = [
  { title: "1. Năng lực kỹ thuật cốt lõi", detail: "Đánh giá độ sâu kiến thức chuyên môn và chất lượng code/giải pháp." },
  { title: "2. Tư duy & giải quyết vấn đề", detail: "Khả năng phân tích, tiếp cận vấn đề và đưa ra giải pháp hợp lý." },
  { title: "3. Văn hoá & kỹ năng giao tiếp", detail: "Mức độ phù hợp văn hoá đội ngũ và khả năng trình bày, phối hợp." },
];

export function InterviewsPage() {
  const { interviews, loading, error, reload, replace } = useInterviews();
  const { entries: candidates } = useApplicationCandidates(interviews.map((interview) => interview.applicationId));
  const { nameFor: interviewerName } = useEmployeeNames(interviews.flatMap((interview) => interview.interviewerUserIds));

  const [busyInterviewId, setBusyInterviewId] = useState<number>();
  const [actionError, setActionError] = useState<string>();
  const [pendingCancel, setPendingCancel] = useState<Interview>();
  const [scoring, setScoring] = useState<Interview>();
  const [scoreBusy, setScoreBusy] = useState(false);
  const [scoreError, setScoreError] = useState<string>();

  function handleAction(interview: Interview, action: InterviewLifecycleAction) {
    if (action === "cancel") {
      setPendingCancel(interview);
      return;
    }
    setBusyInterviewId(interview.id);
    setActionError(undefined);
    performInterviewAction(interview, action)
      .then(replace)
      .catch((cause) => setActionError(cause instanceof Error ? cause.message : "Không thể cập nhật lịch phỏng vấn."))
      .finally(() => setBusyInterviewId(undefined));
  }

  function handleScoreSubmit(write: EvaluationWrite) {
    if (!scoring) return;
    setScoreBusy(true);
    setScoreError(undefined);
    submitEvaluation(scoring.id, write)
      .then(() => setScoring(undefined))
      .catch((cause) => setScoreError(cause instanceof Error ? cause.message : "Không thể lưu đánh giá."))
      .finally(() => setScoreBusy(false));
  }

  const scoringCandidate = scoring ? candidates.get(scoring.applicationId)?.candidate : undefined;

  return (
    <div>
      <PageHeader breadcrumb="Tuyển dụng / Phỏng vấn & Đánh giá" title="Lịch phỏng vấn & phiếu đánh giá" />

      {actionError && (
        <div className="mb-4">
          <StateBanner error={actionError} />
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <div className="lg:col-span-8">
          <div className="rounded-2xl border border-[#edf0f4] bg-white p-5 shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
            <h2 className="mb-4 text-sm font-bold text-slate-800">Lịch phỏng vấn sắp diễn ra</h2>
            <StateBanner loading={loading} error={error} onRetry={reload} loadingLabel="Đang tải lịch phỏng vấn…" />
            {!loading && !error && (
              <InterviewList
                interviews={interviews}
                candidates={candidates}
                interviewerName={interviewerName}
                busyInterviewId={busyInterviewId}
                onAction={handleAction}
                onScore={setScoring}
              />
            )}
          </div>
        </div>

        <div className="lg:col-span-4">
          <div className="rounded-2xl border border-[#edf0f4] bg-white p-5 shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
            <h2 className="mb-4 text-sm font-bold text-slate-800">Tiêu chuẩn đánh giá chuẩn hoá</h2>
            <div className="space-y-4">
              {rubric.map((item) => (
                <div key={item.title}>
                  <p className="text-sm font-semibold text-slate-700">{item.title}</p>
                  <p className="mt-0.5 text-xs text-slate-500">{item.detail}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      <ScorecardModal
        open={scoring !== undefined}
        subtitle={scoringCandidate ? candidateFullName(scoringCandidate) : undefined}
        busy={scoreBusy}
        error={scoreError}
        onClose={() => setScoring(undefined)}
        onSubmit={handleScoreSubmit}
      />

      <ReasonModal
        key={pendingCancel?.id ?? "none"}
        open={pendingCancel !== undefined}
        title="Huỷ lịch phỏng vấn"
        busy={busyInterviewId === pendingCancel?.id}
        onClose={() => setPendingCancel(undefined)}
        onConfirm={(reason) => {
          if (!pendingCancel) return;
          setBusyInterviewId(pendingCancel.id);
          performInterviewAction(pendingCancel, "cancel", { reason })
            .then((interview) => {
              replace(interview);
              setPendingCancel(undefined);
            })
            .catch((cause) => setActionError(cause instanceof Error ? cause.message : "Không thể huỷ lịch phỏng vấn."))
            .finally(() => setBusyInterviewId(undefined));
        }}
      />
    </div>
  );
}
