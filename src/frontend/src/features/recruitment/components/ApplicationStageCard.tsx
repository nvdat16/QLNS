import { nextStage, type RecruitmentApplication } from "../api/recruitmentApplications";

interface Props {
  application: RecruitmentApplication;
  busy: boolean;
  onAdvance: () => void;
}

export function ApplicationStageCard({ application, busy, onAdvance }: Props) {
  const target = nextStage(application.stage);

  return (
    <article className="card" aria-busy={busy}>
      <h2>Hồ sơ #{application.id}</h2>
      <div className="stage">{application.stage}</div>
      <p>Phiên bản: {application.version}</p>
      <p>Cập nhật: {new Date(application.updatedAt).toLocaleString("vi-VN")}</p>
      <button type="button" disabled={busy || !target} onClick={onAdvance}>
        {target ? `Chuyển sang ${target}` : "Đã ở giai đoạn cuối"}
      </button>
    </article>
  );
}
