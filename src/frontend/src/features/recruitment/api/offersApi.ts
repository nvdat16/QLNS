import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";
import type { EmploymentType } from "./requisitionsApi";

export const offerStatuses = ["draft", "approved", "sent", "accepted", "declined", "expired", "cancelled"] as const;
export type OfferStatus = (typeof offerStatuses)[number];

export interface Offer {
  id: number;
  applicationId: number;
  baseSalary: number;
  bonusAmount?: number;
  allowanceAmount?: number;
  currency: string;
  employmentType: EmploymentType;
  startDate: string;
  expirationDate: string;
  templateVersion: string;
  status: OfferStatus;
  documentAvailable: boolean;
  approvedBy?: number;
  approvedAt?: string;
  sentAt?: string;
  respondedAt?: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
}

export interface OfferWrite {
  applicationId: number;
  baseSalary: number;
  bonusAmount?: number;
  allowanceAmount?: number;
  currency?: string;
  employmentType: EmploymentType;
  startDate: string;
  expirationDate: string;
  templateVersion: string;
}

export interface OfferFilters {
  applicationId?: number;
  status?: OfferStatus;
  page?: number;
  pageSize?: number;
}

export interface OfferPage {
  items: Offer[];
  page: PageMetadata;
}

export type OfferLifecycleAction = "approve" | "send" | "extend" | "cancel";

export async function listOffers(filters: OfferFilters): Promise<OfferPage> {
  const query = buildQuery({
    applicationId: filters.applicationId,
    status: filters.status,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 20,
  });
  const response = await apiRequest(`/api/v1/recruitment/offers${query}`);
  return (await response.json()) as OfferPage;
}

export async function createOffer(write: OfferWrite): Promise<Offer> {
  const response = await apiRequest("/api/v1/recruitment/offers", {
    method: "POST",
    body: JSON.stringify(write),
  });
  const body = (await response.json()) as Omit<Offer, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function performOfferAction(
  offer: Offer,
  action: OfferLifecycleAction,
  input: { reason?: string; expirationDate?: string } = {},
): Promise<Offer> {
  const response = await apiRequest(`/api/v1/recruitment/offers/${offer.id}/${action}`, {
    method: "POST",
    headers: { "If-Match": offer.etag },
    body: JSON.stringify(input),
  });
  const body = (await response.json()) as Omit<Offer, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}
