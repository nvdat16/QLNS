import { useCallback, useEffect, useState } from "react";
import { listContracts, type Contract, type ContractFilters, type ContractPage } from "../api/contractsApi";

const defaultFilters: ContractFilters = { page: 1, pageSize: 10 };

export function useContracts() {
  const [filters, setFilters] = useState<ContractFilters>(defaultFilters);
  const [data, setData] = useState<ContractPage>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setData(await listContracts(filters));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải danh sách hợp đồng.");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    void load();
  }, [load]);

  const updateFilters = useCallback((patch: Omit<ContractFilters, "page" | "pageSize">) => {
    setFilters((prev) => ({ ...prev, ...patch, page: 1 }));
  }, []);

  const setPage = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const replaceContract = useCallback((contract: Contract) => {
    setData((prev) =>
      prev ? { ...prev, items: prev.items.map((item) => (item.id === contract.id ? contract : item)) } : prev,
    );
  }, []);

  return { filters, data, loading, error, updateFilters, setPage, reload: load, replaceContract };
}
