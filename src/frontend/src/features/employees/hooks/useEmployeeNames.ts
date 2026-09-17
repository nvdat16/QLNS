import { useEffect, useState } from "react";
import { getEmployee } from "../api/employeesApi";

export function useEmployeeNames(employeeIds: number[]) {
  const key = [...new Set(employeeIds)].sort((a, b) => a - b).join(",");
  const [names, setNames] = useState<Map<number, string>>(new Map());

  useEffect(() => {
    const ids = key ? key.split(",").map(Number) : [];
    if (ids.length === 0) return;

    let cancelled = false;
    Promise.all(
      ids.map(async (id) => {
        try {
          const employee = await getEmployee(id);
          return [id, `${employee.lastName} ${employee.firstName}`] as const;
        } catch {
          return [id, `NV #${id}`] as const;
        }
      }),
    ).then((entries) => {
      if (!cancelled) setNames(new Map(entries));
    });

    return () => {
      cancelled = true;
    };
  }, [key]);

  return {
    nameFor: (id: number) => names.get(id) ?? `NV #${id}`,
  };
}
