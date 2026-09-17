import { useCallback, useEffect, useState } from "react";
import { listInterviews, type Interview } from "../api/interviewsApi";

export function useInterviews() {
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      const page = await listInterviews({ pageSize: 50 });
      setInterviews([...page.items].sort((a, b) => a.startsAt.localeCompare(b.startsAt)));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải lịch phỏng vấn.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const replace = useCallback((interview: Interview) => {
    setInterviews((prev) => prev.map((item) => (item.id === interview.id ? interview : item)));
  }, []);

  return { interviews, loading, error, reload: load, replace };
}
