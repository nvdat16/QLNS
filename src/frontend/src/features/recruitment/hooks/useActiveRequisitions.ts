import { useEffect, useState } from "react";
import { listRequisitions, type Requisition } from "../api/requisitionsApi";

export function useActiveRequisitions() {
  const [requisitions, setRequisitions] = useState<Requisition[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    listRequisitions({ status: "active_recruiting", pageSize: 100 })
      .then((page) => {
        if (!cancelled) setRequisitions(page.items);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return { requisitions, loading };
}
