import { apiRequest } from "../../../api/apiClient";

export const personas = ["hr-manager", "hr-officer", "line-manager", "recruiter", "employee", "it-admin"] as const;
export type Persona = (typeof personas)[number];

export const personaLabels: Record<Persona, string> = {
  "hr-manager": "Trưởng phòng Nhân sự",
  "hr-officer": "Chuyên viên Nhân sự",
  "line-manager": "Quản lý trực tiếp",
  recruiter: "Chuyên viên tuyển dụng",
  employee: "Nhân viên",
  "it-admin": "Quản trị hệ thống",
};

export interface DevSession {
  persona: Persona;
  token: string;
  expiresInSeconds: number;
  issuedAt: number;
}

// Backed by the Development-only `/dev/token` seam (see DevelopmentAuthentication.cs).
// There is no production login/register endpoint yet — real sign-in is delegated to an external OIDC IdP.
export async function signInWithPersona(persona: Persona): Promise<DevSession> {
  const response = await apiRequest(`/dev/token?persona=${persona}`);
  const body = (await response.json()) as { persona: Persona; token: string; expiresInSeconds: number };
  return { ...body, issuedAt: Date.now() };
}
