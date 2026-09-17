import { Badge } from "../../../components/Badge";
import { candidateFullName } from "../api/candidates";
import type { ApplicationCandidate } from "../hooks/useApplicationCandidates";
import type { Interview, InterviewLifecycleAction } from "../api/interviewsApi";

const statusLabels: Record<Interview["status"], string> = {
  scheduled: "Đã lên lịch",
  completed: "Hoàn tất",
  cancelled: "Đã huỷ",
  no_show: "Vắng mặt",
};

interface Props {
  interviews: Interview[];
  candidates: Map<number, ApplicationCandidate>;
  interviewerName: (userId: number) => string;
  busyInterviewId: number | undefined;
  onAction: (interview: Interview, action: InterviewLifecycleAction) => void;
  onScore: (interview: Interview) => void;
}

export function InterviewList({ interviews, candidates, interviewerName, busyInterviewId, onAction, onScore }: Props) {
  if (interviews.length === 0) {
    return <p className="px-1 py-10 text-center text-sm text-slate-400">Chưa có lịch phỏng vấn nào.</p>;
  }

  return (
    <div className="space-y-3">
      {interviews.map((interview) => {
        const resolved = candidates.get(interview.applicationId);
        const isMeeting = Boolean(interview.meetingUrl);

        return (
          <div key={interview.id} className="flex items-start gap-4 rounded-xl border border-slate-100 p-4">
            <span
              className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${isMeeting ? "bg-blue-50 text-blue-600" : "bg-indigo-50 text-indigo-600"}`}
            >
              <span className="material-symbols-outlined icon-md">{isMeeting ? "videocam" : "meeting_room"}</span>
            </span>

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="font-semibold text-slate-800">
                  {resolved?.candidate ? candidateFullName(resolved.candidate) : `Ứng viên #${interview.applicationId}`}
                </p>
                <Badge tone={interview.status === "cancelled" ? "rose" : "blue"}>{statusLabels[interview.status]}</Badge>
              </div>
              <p className="mt-0.5 text-sm text-slate-500">
                {interview.interviewType} • {new Date(interview.startsAt).toLocaleString("vi-VN")} –{" "}
                {new Date(interview.endsAt).toLocaleTimeString("vi-VN")}
              </p>
              <p className="mt-0.5 text-xs text-slate-400">
                {isMeeting ? interview.meetingUrl : interview.location} • Hội đồng:{" "}
                {interview.interviewerUserIds.map(interviewerName).join(", ")}
              </p>

              <div className="mt-3 flex flex-wrap gap-2">
                {interview.meetingUrl && (
                  <a
                    href={interview.meetingUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="rounded-lg bg-blue-50 px-3 py-1.5 text-xs font-semibold text-blue-700 hover:bg-blue-100"
                  >
                    Vào Meet
                  </a>
                )}
                <button
                  type="button"
                  onClick={() => onScore(interview)}
                  className="rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
                >
                  Scorecard
                </button>
                {interview.status === "scheduled" && (
                  <>
                    <button
                      type="button"
                      disabled={busyInterviewId === interview.id}
                      onClick={() => onAction(interview, "complete")}
                      className="rounded-lg border border-emerald-200 px-3 py-1.5 text-xs font-semibold text-emerald-700 hover:bg-emerald-50 disabled:opacity-50"
                    >
                      Hoàn tất
                    </button>
                    <button
                      type="button"
                      disabled={busyInterviewId === interview.id}
                      onClick={() => onAction(interview, "cancel")}
                      className="rounded-lg border border-rose-200 px-3 py-1.5 text-xs font-semibold text-rose-700 hover:bg-rose-50 disabled:opacity-50"
                    >
                      Huỷ lịch
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
