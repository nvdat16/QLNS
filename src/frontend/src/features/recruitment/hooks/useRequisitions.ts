import { useCallback, useEffect, useState } from "react";
import {
  listRequisitions,
  type Requisition,
  type RequisitionFilters,
  type RequisitionPage,
} from "../api/requisitionsApi";

const defaultFilters: RequisitionFilters = { page: 1, pageSize: 20 };

export function useRequisitions() {
  const [filters, setFilters] = useState<RequisitionFilters>(defaultFilters);
  const [data, setData] = useState<RequisitionPage>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setData(await listRequisitions(filters));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải danh sách tin tuyển dụng.");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    void load();
  }, [load]);

  const updateFilters = useCallback((patch: Omit<RequisitionFilters, "page" | "pageSize">) => {
    setFilters((prev) => ({ ...prev, ...patch, page: 1 }));
  }, []);

  const setPage = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const upsert = useCallback((requisition: Requisition) => {
    setData((prev) =>
      prev
        ? prev.items.some((item) => item.id === requisition.id)
          ? { ...prev, items: prev.items.map((item) => (item.id === requisition.id ? requisition : item)) }
          : { ...prev, items: [requisition, ...prev.items] }
        : prev,
    );
  }, []);

  return { filters, data, loading, error, updateFilters, setPage, reload: load, upsert };
}
