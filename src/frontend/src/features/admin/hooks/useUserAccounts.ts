import { useCallback, useEffect, useState } from "react";
import {
  listRoles,
  listUserAccounts,
  type Role,
  type UserAccount,
  type UserAccountFilters,
  type UserAccountPage,
} from "../api/userAccountsApi";

const defaultFilters: UserAccountFilters = { page: 1, pageSize: 10 };

export function useUserAccounts() {
  const [filters, setFilters] = useState<UserAccountFilters>(defaultFilters);
  const [data, setData] = useState<UserAccountPage>();
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setData(await listUserAccounts(filters));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải danh sách tài khoản.");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    void load();
  }, [load]);

  // The role catalogue is reference data: load it once and keep it.
  useEffect(() => {
    void listRoles()
      .then(setRoles)
      .catch(() => setRoles([]));
  }, []);

  const updateFilters = useCallback((patch: Omit<UserAccountFilters, "page" | "pageSize">) => {
    setFilters((prev) => ({ ...prev, ...patch, page: 1 }));
  }, []);

  const setPage = useCallback((page: number) => setFilters((prev) => ({ ...prev, page })), []);

  const replaceAccount = useCallback((account: UserAccount) => {
    setData((prev) =>
      prev ? { ...prev, items: prev.items.map((item) => (item.id === account.id ? account : item)) } : prev,
    );
  }, []);

  return { filters, data, roles, loading, error, updateFilters, setPage, reload: load, replaceAccount };
}
