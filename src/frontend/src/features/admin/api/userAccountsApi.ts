import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";
import type { DataScope } from "../../auth/api/authApi";

export const accountStatuses = ["active", "disabled"] as const;
export type AccountStatus = (typeof accountStatuses)[number];

export interface RoleGrant {
  roleCode: string;
  dataScopeType: DataScope;
  dataScopeId: number;
}

export interface UserAccount {
  id: number;
  email: string;
  displayName: string;
  status: AccountStatus;
  externalSubject: string;
  employeeId?: number;
  roles: RoleGrant[];
  dataScope: DataScope;
  hasCredential: boolean;
  mustChangePassword: boolean;
  passwordUpdatedAt?: string;
  lastLoginAt?: string;
  lockedUntil?: string;
  createdAt: string;
  updatedAt: string;
  version: number;
  etag: string;
}

export interface UserAccountFilters {
  search?: string;
  status?: AccountStatus;
  roleCode?: string;
  page?: number;
  pageSize?: number;
}

export interface UserAccountPage {
  items: UserAccount[];
  page: PageMetadata;
}

export interface Role {
  code: string;
  name: string;
  description?: string;
  isAssignable: boolean;
  permissions: string[];
}

export interface CreateUserAccountInput {
  email: string;
  displayName: string;
  initialPassword: string;
  employeeId?: number;
  roles: RoleGrant[];
}

const basePath = "/api/v1/admin/users";

/** The list endpoint returns no per-row ETag, so the version column stands in for it. */
function fromList(item: Omit<UserAccount, "etag">): UserAccount {
  return { ...item, etag: `"${item.version}"` };
}

export async function listUserAccounts(filters: UserAccountFilters): Promise<UserAccountPage> {
  const query = buildQuery({
    search: filters.search,
    status: filters.status,
    roleCode: filters.roleCode,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 10,
  });
  const response = await apiRequest(`${basePath}${query}`);
  const body = (await response.json()) as { items: Omit<UserAccount, "etag">[]; page: PageMetadata };
  return { items: body.items.map(fromList), page: body.page };
}

export async function listRoles(): Promise<Role[]> {
  const response = await apiRequest("/api/v1/admin/roles");
  return (await response.json()) as Role[];
}

async function readAccount(response: Response): Promise<UserAccount> {
  const body = (await response.json()) as Omit<UserAccount, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function createUserAccount(input: CreateUserAccountInput): Promise<UserAccount> {
  const response = await apiRequest(basePath, { method: "POST", body: JSON.stringify(input) });
  return readAccount(response);
}

export async function updateUserAccount(
  account: UserAccount,
  input: { email: string; displayName: string },
): Promise<UserAccount> {
  const response = await apiRequest(`${basePath}/${account.id}`, {
    method: "PUT",
    headers: { "If-Match": account.etag },
    body: JSON.stringify(input),
  });
  return readAccount(response);
}

export async function setUserAccountStatus(account: UserAccount, status: AccountStatus): Promise<UserAccount> {
  const action = status === "disabled" ? "disable" : "enable";
  const response = await apiRequest(`${basePath}/${account.id}/${action}`, {
    method: "POST",
    headers: { "If-Match": account.etag },
  });
  return readAccount(response);
}

export async function resetUserPassword(account: UserAccount, newPassword: string): Promise<UserAccount> {
  const response = await apiRequest(`${basePath}/${account.id}/password-reset`, {
    method: "POST",
    body: JSON.stringify({ newPassword }),
  });
  return readAccount(response);
}

export async function replaceRoleGrants(account: UserAccount, roles: RoleGrant[]): Promise<UserAccount> {
  const response = await apiRequest(`${basePath}/${account.id}/roles`, {
    method: "PUT",
    headers: { "If-Match": account.etag },
    body: JSON.stringify({ roles }),
  });
  return readAccount(response);
}
