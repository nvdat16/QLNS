import { apiRequest } from "../../../api/apiClient";
import type { EmployeeSummary } from "../../employees/api/employeesApi";

export interface Department {
  id: number;
  code: string;
  name: string;
  parentDepartmentId?: number;
  costCenter?: string;
  description?: string;
  manager?: EmployeeSummary;
  headcount: number;
  version: number;
}

export interface Position {
  id: number;
  code: string;
  name: string;
  level?: string;
  description?: string;
  version: number;
}

export async function listDepartments(): Promise<Department[]> {
  const response = await apiRequest("/api/v1/organization/departments");
  return (await response.json()) as Department[];
}

export async function listPositions(): Promise<Position[]> {
  const response = await apiRequest("/api/v1/organization/positions");
  return (await response.json()) as Position[];
}
