import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";

export const employmentTypes = ["full_time", "part_time", "hybrid", "remote", "internship", "service_contract"] as const;
export type EmploymentType = (typeof employmentTypes)[number];

export const requisitionStatuses = [
  "draft",
  "pending_approval",
  "approved",
  "active_recruiting",
  "closed",
  "cancelled",
] as const;
export type RequisitionStatus = (typeof requisitionStatuses)[number];

export interface Requisition {
  id: number;
  jobCode: string;
  title: string;
  departmentId: number;
  positionId?: number;
  description?: string;
  requirements?: string;
  location?: string;
  employmentType: EmploymentType;
  salaryMin?: number;
  salaryMax?: number;
  targetHeadcount: number;
  closingDate?: string;
  status: RequisitionStatus;
  publishedAt?: string;
  createdBy: number;
  version: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
}

export interface RequisitionWrite {
  title: string;
  departmentId: number;
  positionId?: number;
  description?: string;
  requirements?: string;
  location?: string;
  employmentType: EmploymentType;
  salaryMin?: number;
  salaryMax?: number;
  targetHeadcount: number;
  closingDate?: string;
}

export interface RequisitionFilters {
  search?: string;
  departmentId?: number;
  status?: RequisitionStatus;
  page?: number;
  pageSize?: number;
}

export interface RequisitionPage {
  items: Requisition[];
  page: PageMetadata;
}

export type RequisitionLifecycleAction = "submit" | "approve" | "reject" | "publish" | "close" | "cancel";

export async function listRequisitions(filters: RequisitionFilters): Promise<RequisitionPage> {
  const query = buildQuery({
    search: filters.search,
    departmentId: filters.departmentId,
    status: filters.status,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 20,
  });
  const response = await apiRequest(`/api/v1/recruitment/requisitions${query}`);
  return (await response.json()) as RequisitionPage;
}

export async function getRequisition(requisitionId: number): Promise<Requisition> {
  const response = await apiRequest(`/api/v1/recruitment/requisitions/${requisitionId}`);
  const body = (await response.json()) as Omit<Requisition, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function createRequisition(write: RequisitionWrite): Promise<Requisition> {
  const response = await apiRequest("/api/v1/recruitment/requisitions", {
    method: "POST",
    body: JSON.stringify(write),
  });
  const body = (await response.json()) as Omit<Requisition, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function performRequisitionAction(
  requisition: Requisition,
  action: RequisitionLifecycleAction,
  reason?: string,
): Promise<Requisition> {
  const response = await apiRequest(`/api/v1/recruitment/requisitions/${requisition.id}/${action}`, {
    method: "POST",
    headers: { "If-Match": requisition.etag },
    body: JSON.stringify(reason ? { reason } : {}),
  });
  const body = (await response.json()) as Omit<Requisition, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}
