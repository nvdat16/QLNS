import { apiRequest } from "../../../api/apiClient";
import type { CandidateInput, CandidateSummary } from "./candidates";
import type { RecruitmentApplication } from "./recruitmentApplications";
import type { Requisition } from "./requisitionsApi";

export const intakeStatuses = [
  "scanning",
  "parsing",
  "awaiting_confirmation",
  "duplicate_review",
  "completed",
  "rejected",
  "failed",
] as const;
export type IntakeStatus = (typeof intakeStatuses)[number];

export interface CandidateIntake {
  id: string;
  requisitionId: number;
  status: IntakeStatus;
  parsedCandidate?: CandidateInput;
  confidence?: Record<string, number>;
  duplicateCandidates?: CandidateSummary[];
  uploadedAt: string;
}

export interface RecruitmentApplicationDetail extends RecruitmentApplication {
  candidate: CandidateSummary;
  requisition?: Requisition;
}

export async function uploadResume(
  file: File,
  requisitionId: number,
  privacyNoticeVersion: string,
): Promise<CandidateIntake> {
  const form = new FormData();
  form.set("file", file);
  form.set("requisitionId", String(requisitionId));
  form.set("privacyNoticeVersion", privacyNoticeVersion);
  form.set("consented", "true");

  const response = await apiRequest("/api/v1/recruitment/resumes", { method: "POST", body: form });
  return (await response.json()) as CandidateIntake;
}

export async function getIntake(intakeId: string): Promise<CandidateIntake> {
  const response = await apiRequest(`/api/v1/recruitment/intakes/${intakeId}`);
  return (await response.json()) as CandidateIntake;
}

export async function confirmIntake(
  intakeId: string,
  candidate: CandidateInput,
  existingCandidateId?: number,
): Promise<RecruitmentApplicationDetail> {
  const response = await apiRequest(`/api/v1/recruitment/intakes/${intakeId}/confirm`, {
    method: "POST",
    headers: { "Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify({ candidate, existingCandidateId, source: "direct" }),
  });
  return (await response.json()) as RecruitmentApplicationDetail;
}

export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
