import type { BadgeTone } from "../../../components/Badge";
import type { RecruitmentStage } from "../api/recruitmentApplications";

export const stageLabels: Record<RecruitmentStage, string> = {
  sourced_applied: "Sourced & Applied",
  ai_screening: "AI Screening",
  tech_interview: "Tech Interview",
  executive_round: "Executive Round",
  offer_letter: "Offer Letter",
  hired_ready: "Hired & Ready",
  rejected: "Từ chối",
  withdrawn: "Rút hồ sơ",
};

export const stageTones: Record<RecruitmentStage, BadgeTone> = {
  sourced_applied: "slate",
  ai_screening: "blue",
  tech_interview: "amber",
  executive_round: "purple",
  offer_letter: "rose",
  hired_ready: "emerald",
  rejected: "rose",
  withdrawn: "slate",
};

export const stageFilterOptions: RecruitmentStage[] = [
  "sourced_applied",
  "ai_screening",
  "tech_interview",
  "executive_round",
  "offer_letter",
  "hired_ready",
];
