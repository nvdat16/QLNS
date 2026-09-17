import { apiRequest, buildQuery, withEtag, type PageMetadata } from "../../../api/apiClient";

export const interviewStatuses = ["scheduled", "completed", "cancelled", "no_show"] as const;
export type InterviewStatus = (typeof interviewStatuses)[number];

export interface Interview {
  id: number;
  applicationId: number;
  interviewType: string;
  startsAt: string;
  endsAt: string;
  timezone: string;
  interviewerUserIds: number[];
  location?: string;
  meetingUrl?: string;
  status: InterviewStatus;
  cancellationReason?: string;
  version: number;
  etag: string;
}

export interface InterviewWrite {
  applicationId: number;
  interviewType: string;
  startsAt: string;
  endsAt: string;
  timezone: string;
  interviewerUserIds: number[];
  location?: string;
  meetingUrl?: string;
}

export interface InterviewFilters {
  applicationId?: number;
  interviewerUserId?: number;
  from?: string;
  to?: string;
  status?: InterviewStatus;
  page?: number;
  pageSize?: number;
}

export interface InterviewPage {
  items: Interview[];
  page: PageMetadata;
}

export type InterviewLifecycleAction = "reschedule" | "complete" | "cancel";

export async function listInterviews(filters: InterviewFilters): Promise<InterviewPage> {
  const query = buildQuery({
    applicationId: filters.applicationId,
    interviewerUserId: filters.interviewerUserId,
    from: filters.from,
    to: filters.to,
    status: filters.status,
    page: filters.page ?? 1,
    pageSize: filters.pageSize ?? 20,
  });
  const response = await apiRequest(`/api/v1/recruitment/interviews${query}`);
  return (await response.json()) as InterviewPage;
}

export async function createInterview(write: InterviewWrite): Promise<Interview> {
  const response = await apiRequest("/api/v1/recruitment/interviews", {
    method: "POST",
    body: JSON.stringify(write),
  });
  const body = (await response.json()) as Omit<Interview, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}

export async function performInterviewAction(
  interview: Interview,
  action: InterviewLifecycleAction,
  input: { reason?: string; startsAt?: string; endsAt?: string } = {},
): Promise<Interview> {
  const response = await apiRequest(`/api/v1/recruitment/interviews/${interview.id}/${action}`, {
    method: "POST",
    headers: { "If-Match": interview.etag },
    body: JSON.stringify(input),
  });
  const body = (await response.json()) as Omit<Interview, "etag">;
  return withEtag(body, response.headers.get("ETag"));
}
