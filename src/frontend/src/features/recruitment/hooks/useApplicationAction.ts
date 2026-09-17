import { useCallback, useState } from "react";
import { ApiProblem } from "../../../api/apiClient";
import {
  advanceApplication,
  nextStage,
  rejectApplication,
  type ActiveStage,
  type RecruitmentApplication,
} from "../api/recruitmentApplications";

export function useApplicationAction(onSettled: () => void) {
  const [busyApplicationId, setBusyApplicationId] = useState<number>();
  const [error, setError] = useState<string>();

  const advance = useCallback(
    async (application: RecruitmentApplication, targetStage?: ActiveStage) => {
      const target = targetStage ?? nextStage(application.stage);
      if (!target) return;

      setBusyApplicationId(application.id);
      setError(undefined);
      try {
        await advanceApplication(application, target);
        onSettled();
      } catch (cause) {
        setError(
          cause instanceof ApiProblem
            ? cause.message
            : cause instanceof Error
              ? cause.message
              : "Không thể chuyển giai đoạn ứng viên.",
        );
      } finally {
        setBusyApplicationId(undefined);
      }
    },
    [onSettled],
  );

  const reject = useCallback(
    async (application: RecruitmentApplication, reason: string) => {
      setBusyApplicationId(application.id);
      setError(undefined);
      try {
        await rejectApplication(application, reason);
        onSettled();
      } catch (cause) {
        setError(cause instanceof Error ? cause.message : "Không thể từ chối ứng viên.");
      } finally {
        setBusyApplicationId(undefined);
      }
    },
    [onSettled],
  );

  return { advance, reject, busyApplicationId, error };
}
