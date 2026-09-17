import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";

export const contractTypes = ["probation", "fixed_term", "indefinite", "internship", "service_contract"] as const;
export type ContractType = (typeof contractTypes)[number];

export const contractStatuses = [
  "draft",
  "approved",
  "executed",
  "active",
  "expired",
  "terminated",
  "cancelled",
] as const;
export type ContractStatus = (typeof contractStatuses)[number];

export interface Contract {
  id: number;
  employeeId: number;
  contractNumber: string;
  contractType: ContractType;
  startDate: string;
  endDate?: string;
  salary: number;
  currency: string;
  noticePeriodDays?: number;
  isPrimary: boolean;
  status: ContractStatus;
  signedDocumentAvailable: boolean;
  signedAt?: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
}

export interface ContractFilters {
  employeeId?: number;
  type?: ContractType;
  status?: ContractStatus;
  page?: number;
  pageSize?: number;
}

export interface ContractPage {
  items: Contract[];
  page: PageMetadata;
}

export type ContractLifecycleAction = "approve" | "activate" | "terminate" | "cancel";

export interface ContractActionInput {
  reason?: string;
  signedAt?: string;
}

export async function listContracts(filters: ContractFilters): Promise<ContractPage> {
  const query = buildQuery({
    employeeId: filters.employeeId,
    type: filters.type,
    status: filters.status,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 10,
  });
  const response = await apiRequest(`/api/v1/contracts${query}`);
  return (await response.json()) as ContractPage;
}

export async function getContract(contractId: number): Promise<Contract> {
  const response = await apiRequest(`/api/v1/contracts/${contractId}`);
  const body = (await response.json()) as Omit<Contract, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function performContractAction(
  contract: Contract,
  action: ContractLifecycleAction,
  input: ContractActionInput = {},
): Promise<Contract> {
  const response = await apiRequest(`/api/v1/contracts/${contract.id}/${action}`, {
    method: "POST",
    headers: { "If-Match": contract.etag },
    body: JSON.stringify(input),
  });
  const body = (await response.json()) as Omit<Contract, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}
