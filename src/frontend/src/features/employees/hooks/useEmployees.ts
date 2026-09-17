import { useCallback, useEffect, useState } from "react";
import { listEmployees, type EmployeeFilters, type EmployeePage } from "../api/employeesApi";

const defaultFilters: EmployeeFilters = { page: 1, pageSize: 10 };

export function useEmployees() {
  const [filters, setFilters] = useState<EmployeeFilters>(defaultFilters);
  const [data, setData] = useState<EmployeePage>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setData(await listEmployees(filters));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải danh sách nhân viên.");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    void load();
  }, [load]);

  const updateFilters = useCallback((patch: Omit<EmployeeFilters, "page" | "pageSize">) => {
    setFilters((prev) => ({ ...prev, ...patch, page: 1 }));
  }, []);

  const setPage = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  return { filters, data, loading, error, updateFilters, setPage, reload: load };
}
