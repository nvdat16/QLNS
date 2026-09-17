import { apiRequest, withEtag } from "../../../api/apiClient";

export const activeStages = [
  "sourced_applied",
  "ai_screening",
  "tech_interview",
  "executive_round",
  "offer_letter",
  "hired_ready",
] as const;

export type ActiveStage = (typeof activeStages)[number];
export type RecruitmentStage = ActiveStage | "rejected" | "withdrawn";

export interface RecruitmentApplication {
  id: number;
  candidateId: number;
  jobPostingId: number;
  resumeId?: number;
  stage: RecruitmentStage;
  aiScore?: number;
  source: string;
  appliedAt: string;
  version: number;
  updatedAt: string;
  etag: string;
}

export async function getApplication(applicationId: number): Promise<RecruitmentApplication> {
  const response = await apiRequest(`/api/v1/recruitment/applications/${applicationId}`);
  const body = (await response.json()) as Omit<RecruitmentApplication, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function advanceApplication(
  application: RecruitmentApplication,
  targetStage: ActiveStage,
): Promise<RecruitmentApplication> {
  const response = await apiRequest(`/api/v1/recruitment/applications/${application.id}/advance`, {
    method: "POST",
    headers: { "If-Match": application.etag },
    body: JSON.stringify({ targetStage }),
  });
  const body = (await response.json()) as Omit<RecruitmentApplication, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function rejectApplication(
  application: RecruitmentApplication,
  reason: string,
): Promise<RecruitmentApplication> {
  const response = await apiRequest(`/api/v1/recruitment/applications/${application.id}/reject`, {
    method: "POST",
    headers: { "If-Match": application.etag },
    body: JSON.stringify({ reason }),
  });
  const body = (await response.json()) as Omit<RecruitmentApplication, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export function nextStage(stage: RecruitmentStage): ActiveStage | undefined {
  const index = activeStages.indexOf(stage as ActiveStage);
  return index >= 0 ? activeStages[index + 1] : undefined;
}
