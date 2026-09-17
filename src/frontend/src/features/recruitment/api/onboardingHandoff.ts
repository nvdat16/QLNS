import { apiRequest } from "../../../api/apiClient";

export interface OfferResponseResult {
  offerId: number;
  status: "accepted" | "declined";
  employeeId?: number;
  initialContractId?: number;
  onboardingTaskIds?: number[];
  replayed: boolean;
}

// Real onboarding handoff is candidate-driven: accepting an offer via its X-Offer-Token
// atomically creates the Employee + initial Contract + onboarding tasks (no HR-side "convert" endpoint exists).
// The candidate token is only obtainable through the Development-only seam below, so this action
// is only usable while the API runs in the Development environment.
async function getDevOfferToken(offerId: number): Promise<string> {
  const response = await apiRequest(`/dev/offer-token?offerId=${offerId}`);
  const body = (await response.json()) as { token: string };
  return body.token;
}

export async function acceptOfferAsCandidate(offerId: number): Promise<OfferResponseResult> {
  const offerToken = await getDevOfferToken(offerId);
  const response = await apiRequest(
    `/api/v1/recruitment/offers/${offerId}/response`,
    {
      method: "POST",
      headers: { "X-Offer-Token": offerToken, "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({ decision: "accept" }),
    },
    { skipAuth: true },
  );
  return (await response.json()) as OfferResponseResult;
}
