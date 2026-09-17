import { useCallback, useEffect, useState } from "react";
import { getPipeline, type RecruitmentPipeline } from "../api/pipelineApi";
import type { RecruitmentStage } from "../api/recruitmentApplications";

export interface PipelineFilterState {
  search?: string;
  stage?: RecruitmentStage;
}

export function usePipeline(requisitionId: number | undefined, filters: PipelineFilterState) {
  const [pipeline, setPipeline] = useState<RecruitmentPipeline>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    if (requisitionId === undefined) return;
    setLoading(true);
    setError(undefined);
    try {
      setPipeline(await getPipeline({ requisitionId, ...filters }));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải quy trình tuyển dụng.");
    } finally {
      setLoading(false);
    }
  }, [requisitionId, filters.search, filters.stage]);

  useEffect(() => {
    void load();
  }, [load]);

  return { pipeline, loading, error, reload: load };
}
