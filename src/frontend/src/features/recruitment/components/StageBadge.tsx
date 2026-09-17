import { Badge } from "../../../components/Badge";
import type { RecruitmentStage } from "../api/recruitmentApplications";
import { stageLabels, stageTones } from "./stagePresentation";

export function StageBadge({ stage }: { stage: RecruitmentStage }) {
  return <Badge tone={stageTones[stage]}>{stageLabels[stage]}</Badge>;
}
