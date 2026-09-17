import { useEffect, useState } from "react";
import { getRequisition } from "../api/requisitionsApi";

export function useRequisitionTitles(requisitionIds: number[]) {
  const key = [...new Set(requisitionIds)].sort((a, b) => a - b).join(",");
  const [titles, setTitles] = useState<Map<number, string>>(new Map());

  useEffect(() => {
    const ids = key ? key.split(",").map(Number) : [];
    if (ids.length === 0) return;

    let cancelled = false;
    Promise.all(
      ids.map(async (id) => {
        try {
          const requisition = await getRequisition(id);
          return [id, `${requisition.title} (${requisition.jobCode})`] as const;
        } catch {
          return [id, `Vị trí #${id}`] as const;
        }
      }),
    ).then((entries) => {
      if (!cancelled) setTitles(new Map(entries));
    });

    return () => {
      cancelled = true;
    };
  }, [key]);

  return { titleFor: (id: number) => titles.get(id) ?? `Vị trí #${id}` };
}
