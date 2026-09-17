const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export interface PageMetadata {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export class ApiProblem extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string | undefined,
    message: string,
  ) {
    super(message);
  }
}

// Runtime token seam: set by the auth context after a successful sign-in.
// Falls back to VITE_DEV_ACCESS_TOKEN so the app still works without going through /login locally.
let accessToken: string | undefined = import.meta.env.VITE_DEV_ACCESS_TOKEN;

export function setAccessToken(token: string | undefined): void {
  accessToken = token;
}

export function getAccessToken(): string | undefined {
  return accessToken;
}

export async function apiRequest(
  path: string,
  init?: RequestInit,
  options?: { skipAuth?: boolean },
): Promise<Response> {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json, application/problem+json");
  if (init?.body && !(init.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (accessToken && !options?.skipAuth) headers.set("Authorization", `Bearer ${accessToken}`);

  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers,
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as {
      title?: string;
      detail?: string;
      code?: string;
    };
    throw new ApiProblem(response.status, problem.code, problem.detail ?? problem.title ?? "API request failed");
  }

  return response;
}

export function buildQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    search.set(key, String(value));
  }
  const query = search.toString();
  return query ? `?${query}` : "";
}

export function withEtag<T extends { version: number }>(body: T, etag: string | null): T & { etag: string } {
  return { ...body, etag: etag ?? `"${body.version}"` };
}
