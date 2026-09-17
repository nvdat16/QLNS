import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";

export const employeeStatuses = ["probation", "active", "suspended", "terminated"] as const;
export type EmployeeStatus = (typeof employeeStatuses)[number];

export interface EmployeeSummary {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  departmentId: number;
  positionId: number;
  managerId?: number;
  officeLocation?: string;
  status: EmployeeStatus;
}

export interface EmployeeDetail extends EmployeeSummary {
  personalEmail?: string;
  phone?: string;
  dateOfBirth?: string;
  gender?: string;
  permanentAddress?: string;
  temporaryAddress?: string;
  emergencyContact?: Record<string, string>;
  hireDate: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
}

export interface EmployeeFilters {
  search?: string;
  departmentId?: number;
  status?: EmployeeStatus;
  page?: number;
  pageSize?: number;
  sort?: "name" | "-name" | "employeeCode" | "hireDate";
}

export interface EmployeePage {
  items: EmployeeSummary[];
  page: PageMetadata;
}

export interface PersonalProfilePatch {
  personalEmail?: string;
  phone?: string;
  temporaryAddress?: string;
  emergencyContact?: Record<string, string>;
}

export async function listEmployees(filters: EmployeeFilters): Promise<EmployeePage> {
  const query = buildQuery({
    search: filters.search,
    departmentId: filters.departmentId,
    status: filters.status,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 20,
    sort: filters.sort,
  });
  const response = await apiRequest(`/api/v1/employees${query}`);
  return (await response.json()) as EmployeePage;
}

export async function getEmployee(employeeId: number): Promise<EmployeeDetail> {
  const response = await apiRequest(`/api/v1/employees/${employeeId}`);
  const body = (await response.json()) as Omit<EmployeeDetail, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function patchEmployeeProfile(
  employee: EmployeeDetail,
  patch: PersonalProfilePatch,
): Promise<EmployeeDetail> {
  const response = await apiRequest(`/api/v1/employees/${employee.id}/profile`, {
    method: "PATCH",
    headers: { "If-Match": employee.etag, "Content-Type": "application/merge-patch+json" },
    body: JSON.stringify(patch),
  });
  const body = (await response.json()) as Omit<EmployeeDetail, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}
