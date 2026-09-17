import { apiRequest } from "../../../api/apiClient";

export type Score = number; // 0-5 in 0.5 steps

export const recommendations = ["strong_hire", "hire", "hold", "no_hire", "strong_no_hire"] as const;
export type Recommendation = (typeof recommendations)[number];

export interface EvaluationWrite {
  technicalScore: Score;
  communicationScore: Score;
  problemSolvingScore: Score;
  teamworkScore: Score;
  recommendation: Recommendation;
  feedback: string;
}

export interface Evaluation extends EvaluationWrite {
  id: number;
  interviewId: number;
  evaluatorUserId: number;
  overallScore: number;
  submittedAt: string;
  unlockedAt?: string;
  version: number;
}

export async function listEvaluations(interviewId: number): Promise<Evaluation[]> {
  const response = await apiRequest(`/api/v1/recruitment/interviews/${interviewId}/evaluations`);
  return (await response.json()) as Evaluation[];
}

export async function submitEvaluation(interviewId: number, write: EvaluationWrite): Promise<Evaluation> {
  const response = await apiRequest(`/api/v1/recruitment/interviews/${interviewId}/evaluations`, {
    method: "POST",
    headers: { "Idempotency-Key": crypto.randomUUID() },
    body: JSON.stringify(write),
  });
  return (await response.json()) as Evaluation;
}
