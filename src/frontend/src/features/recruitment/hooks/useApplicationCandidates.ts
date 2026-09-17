import { useEffect, useState } from "react";
import { getApplication, type RecruitmentApplication } from "../api/recruitmentApplications";
import { getPipeline } from "../api/pipelineApi";
import type { CandidateSummary } from "../api/candidates";

export interface ApplicationCandidate {
  application: RecruitmentApplication;
  candidate?: CandidateSummary;
}

// There is no standalone "get candidate by id" endpoint, so we resolve a candidate by
// looking the application up inside its requisition's pipeline board.
async function resolveOne(applicationId: number): Promise<ApplicationCandidate> {
  const application = await getApplication(applicationId);
  const pipeline = await getPipeline({ requisitionId: application.jobPostingId });
  const card = pipeline.columns.flatMap((column) => column.items).find((item) => item.application.id === applicationId);
  return { application, candidate: card?.candidate };
}

export function useApplicationCandidates(applicationIds: number[]) {
  const key = [...new Set(applicationIds)].sort((a, b) => a - b).join(",");
  const [entries, setEntries] = useState<Map<number, ApplicationCandidate>>(new Map());
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const ids = key ? key.split(",").map(Number) : [];
    if (ids.length === 0) return;

    let cancelled = false;
    setLoading(true);
    Promise.all(
      ids.map(async (id) => {
        try {
          return [id, await resolveOne(id)] as const;
        } catch {
          return undefined;
        }
      }),
    ).then((results) => {
      if (cancelled) return;
      setEntries(new Map(results.filter((entry): entry is readonly [number, ApplicationCandidate] => entry !== undefined)));
      setLoading(false);
    });

    return () => {
      cancelled = true;
    };
  }, [key]);

  return { entries, loading };
}
