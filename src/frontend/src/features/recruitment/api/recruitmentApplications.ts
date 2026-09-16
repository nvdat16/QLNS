import { apiRequest } from "../../../api/apiClient";

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
  stage: RecruitmentStage;
  version: number;
  updatedAt: string;
  etag: string;
}

export async function getApplication(applicationId: number): Promise<RecruitmentApplication> {
  const response = await apiRequest(`/api/v1/recruitment/applications/${applicationId}`);
  const body = (await response.json()) as Omit<RecruitmentApplication, "etag">;
  return { ...body, etag: response.headers.get("ETag") ?? `"${body.version}"` };
}

export async function advanceApplication(
  application: RecruitmentApplication,
  targetStage: RecruitmentStage,
): Promise<RecruitmentApplication> {
  const response = await apiRequest(
    `/api/v1/recruitment/applications/${application.id}/advance`,
    {
      method: "POST",
      headers: { "If-Match": application.etag },
      body: JSON.stringify({ targetStage }),
    },
  );
  const body = (await response.json()) as Omit<RecruitmentApplication, "etag">;
  return { ...body, etag: response.headers.get("ETag") ?? `"${body.version}"` };
}

export function nextStage(stage: RecruitmentStage): ActiveStage | undefined {
  const index = activeStages.indexOf(stage as ActiveStage);
  return index >= 0 ? activeStages[index + 1] : undefined;
}
