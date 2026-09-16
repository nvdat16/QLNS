const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export class ApiProblem extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string | undefined,
    message: string,
  ) {
    super(message);
  }
}

export async function apiRequest(path: string, init?: RequestInit): Promise<Response> {
  // Temporary development seam. Replace with the selected OIDC client's in-memory token provider.
  const token = import.meta.env.VITE_DEV_ACCESS_TOKEN as string | undefined;
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json, application/problem+json");
  if (init?.body) headers.set("Content-Type", "application/json");
  if (token) headers.set("Authorization", `Bearer ${token}`);

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
