import { ApplicationStageCard } from "../components/ApplicationStageCard";
import { useAdvanceApplication } from "../hooks/useAdvanceApplication";

interface Props {
  applicationId: number;
}

export function RecruitmentPipelinePage({ applicationId }: Props) {
  const { application, loading, error, advance, reload } = useAdvanceApplication(applicationId);

  return (
    <main className="page">
      <h1>Quy trình tuyển dụng</h1>
      {loading && !application && <p role="status">Đang tải hồ sơ…</p>}
      {error && (
        <div className="error" role="alert">
          <p>{error}</p>
          <button type="button" onClick={() => void reload()}>Thử lại</button>
        </div>
      )}
      {application && (
        <ApplicationStageCard application={application} busy={loading} onAdvance={() => void advance()} />
      )}
    </main>
  );
}
