import { apiRequest } from "../../../api/apiClient";

export type DataScope = "self" | "department" | "organization";

/** Server-resolved identity of the caller. Permissions are authoritative only on the server; the UI uses
 *  them to hide what would be refused anyway, never as the access decision itself. */
export interface AuthenticatedIdentity {
  userId: number;
  employeeId?: number;
  email: string;
  displayName: string;
  roles: string[];
  permissions: string[];
  dataScope: DataScope;
  departmentIds: number[];
  passwordChangeRequired: boolean;
}

export interface Session {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  expiresAt: string;
  /** Absent while a password change is pending: such a session is deliberately not renewable. */
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
  user: AuthenticatedIdentity;
}

export const roleLabels: Record<string, string> = {
  ROLE_ADMIN: "Quản trị hệ thống",
  ROLE_HR_MGR: "Trưởng phòng Nhân sự",
  ROLE_HR_OFFICER: "Chuyên viên Nhân sự",
  ROLE_LINE_MGR: "Quản lý trực tiếp",
  ROLE_RECRUITER: "Chuyên viên tuyển dụng",
  ROLE_INTERVIEWER: "Thành viên hội đồng phỏng vấn",
  ROLE_EMPLOYEE: "Nhân viên",
  ROLE_IT_ADMIN: "Quản trị IT",
};

export function describeRoles(roles: string[]): string {
  if (roles.length === 0) return "Chưa được cấp vai trò";
  return roles.map((role) => roleLabels[role] ?? role).join(", ");
}

export async function signIn(email: string, password: string): Promise<Session> {
  const response = await apiRequest(
    "/api/v1/auth/login",
    { method: "POST", body: JSON.stringify({ email, password }) },
    { skipAuth: true },
  );
  return (await response.json()) as Session;
}

export async function refreshSession(refreshToken: string): Promise<Session> {
  const response = await apiRequest(
    "/api/v1/auth/refresh",
    { method: "POST", body: JSON.stringify({ refreshToken }) },
    { skipAuth: true },
  );
  return (await response.json()) as Session;
}

export async function signOut(refreshToken: string): Promise<void> {
  await apiRequest(
    "/api/v1/auth/logout",
    { method: "POST", body: JSON.stringify({ refreshToken }) },
    { skipAuth: true },
  );
}

export async function getCurrentIdentity(): Promise<AuthenticatedIdentity> {
  const response = await apiRequest("/api/v1/auth/me");
  return (await response.json()) as AuthenticatedIdentity;
}

export async function changeOwnPassword(currentPassword: string, newPassword: string): Promise<void> {
  await apiRequest("/api/v1/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
  });
}
