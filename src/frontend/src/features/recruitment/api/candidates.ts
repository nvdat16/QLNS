export interface CandidateInput {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  linkedinUrl?: string;
  portfolioUrl?: string;
}

export interface CandidateSummary {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  linkedinUrl?: string;
  portfolioUrl?: string;
}

export function candidateFullName(candidate: CandidateSummary): string {
  return `${candidate.lastName} ${candidate.firstName}`;
}
