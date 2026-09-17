import { useCallback, useEffect, useState } from "react";
import { ApiProblem } from "../../../api/apiClient";
import {
  getEmployee,
  patchEmployeeProfile,
  type EmployeeDetail,
  type PersonalProfilePatch,
} from "../api/employeesApi";

export function useEmployeeDetail(employeeId: number | undefined) {
  const [employee, setEmployee] = useState<EmployeeDetail>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    if (employeeId === undefined) return;
    setLoading(true);
    setError(undefined);
    try {
      setEmployee(await getEmployee(employeeId));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải hồ sơ nhân viên.");
    } finally {
      setLoading(false);
    }
  }, [employeeId]);

  useEffect(() => {
    setEmployee(undefined);
    void load();
  }, [load]);

  const updateProfile = useCallback(
    async (patch: PersonalProfilePatch) => {
      if (!employee) return;
      setLoading(true);
      setError(undefined);
      try {
        setEmployee(await patchEmployeeProfile(employee, patch));
      } catch (cause) {
        if (cause instanceof ApiProblem && cause.status === 409) {
          await load();
          setError(`${cause.message} Dữ liệu đã được tải lại.`);
        } else {
          setError(cause instanceof Error ? cause.message : "Không thể cập nhật hồ sơ.");
        }
      } finally {
        setLoading(false);
      }
    },
    [employee, load],
  );

  return { employee, loading, error, updateProfile, reload: load };
}
