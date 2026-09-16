import { useCallback, useEffect, useState } from "react";
import { ApiProblem } from "../../../api/apiClient";
import {
  advanceApplication,
  getApplication,
  nextStage,
  type RecruitmentApplication,
} from "../api/recruitmentApplications";

export function useAdvanceApplication(applicationId: number) {
  const [application, setApplication] = useState<RecruitmentApplication>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setApplication(await getApplication(applicationId));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải hồ sơ ứng tuyển.");
    } finally {
      setLoading(false);
    }
  }, [applicationId]);

  useEffect(() => {
    void load();
  }, [load]);

  const advance = useCallback(async () => {
    if (!application) return;
    const target = nextStage(application.stage);
    if (!target) return;

    setLoading(true);
    setError(undefined);
    try {
      setApplication(await advanceApplication(application, target));
    } catch (cause) {
      if (cause instanceof ApiProblem && cause.status === 409) {
        await load();
        setError(`${cause.message} Dữ liệu đã được tải lại.`);
      } else {
        setError(cause instanceof Error ? cause.message : "Không thể chuyển giai đoạn.");
      }
    } finally {
      setLoading(false);
    }
  }, [application, load]);

  return { application, loading, error, advance, reload: load };
}
