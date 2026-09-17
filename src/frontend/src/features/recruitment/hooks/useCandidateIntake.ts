import { useCallback, useState } from "react";
import type { CandidateInput } from "../api/candidates";
import { confirmIntake, getIntake, sleep, uploadResume, type CandidateIntake } from "../api/candidateIntake";

const MAX_POLL_ATTEMPTS = 10;
const POLL_INTERVAL_MS = 1200;

type Phase = "idle" | "uploading" | "confirming" | "done";

export function useCandidateIntake(requisitionId: number) {
  const [phase, setPhase] = useState<Phase>("idle");
  const [intake, setIntake] = useState<CandidateIntake>();
  const [error, setError] = useState<string>();
  const [createdApplicationId, setCreatedApplicationId] = useState<number>();

  const submit = useCallback(
    async (file: File, privacyNoticeVersion: string) => {
      setPhase("uploading");
      setError(undefined);
      try {
        let current = await uploadResume(file, requisitionId, privacyNoticeVersion);
        for (let attempt = 0; attempt < MAX_POLL_ATTEMPTS && (current.status === "scanning" || current.status === "parsing"); attempt++) {
          await sleep(POLL_INTERVAL_MS);
          current = await getIntake(current.id);
        }
        setIntake(current);
        if (current.status === "failed" || current.status === "rejected") {
          setError("Không thể xử lý hồ sơ CV. Vui lòng thử lại hoặc nhập thông tin thủ công.");
        }
        setPhase("idle");
      } catch (cause) {
        setError(cause instanceof Error ? cause.message : "Không thể tải lên hồ sơ CV.");
        setPhase("idle");
      }
    },
    [requisitionId],
  );

  const confirm = useCallback(
    async (candidate: CandidateInput, existingCandidateId?: number) => {
      if (!intake) return;
      setPhase("confirming");
      setError(undefined);
      try {
        const application = await confirmIntake(intake.id, candidate, existingCandidateId);
        setCreatedApplicationId(application.id);
        setPhase("done");
      } catch (cause) {
        setError(cause instanceof Error ? cause.message : "Không thể thêm ứng viên vào pipeline.");
        setPhase("idle");
      }
    },
    [intake],
  );

  const reset = useCallback(() => {
    setPhase("idle");
    setIntake(undefined);
    setError(undefined);
    setCreatedApplicationId(undefined);
  }, []);

  return { phase, intake, error, createdApplicationId, submit, confirm, reset };
}
