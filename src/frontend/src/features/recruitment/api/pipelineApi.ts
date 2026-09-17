import { apiRequest, buildQuery } from "../../../api/apiClient";
import type { CandidateSummary } from "./candidates";
import type { RecruitmentApplication, RecruitmentStage } from "./recruitmentApplications";

export interface PipelineCard {
  application: RecruitmentApplication;
  candidate: CandidateSummary;
}

export interface PipelineColumn {
  stage: RecruitmentStage;
  totalItems: number;
  averageAiScore?: number;
  items: PipelineCard[];
}

export interface RecruitmentPipeline {
  requisitionId: number;
  columns: PipelineColumn[];
}

export interface PipelineFilters {
  requisitionId: number;
  search?: string;
  stage?: RecruitmentStage;
  minimumAiScore?: number;
}

export async function getPipeline(filters: PipelineFilters): Promise<RecruitmentPipeline> {
  const query = buildQuery({
    requisitionId: filters.requisitionId,
    search: filters.search,
    stage: filters.stage,
    minimumAiScore: filters.minimumAiScore,
    pageSize: 100,
  });
  const response = await apiRequest(`/api/v1/recruitment/pipeline${query}`);
  return (await response.json()) as RecruitmentPipeline;
}
