import { useEffect, useState } from "react";
import { listDepartments, listPositions, type Department, type Position } from "../api/organizationApi";

export function useOrganizationDirectory() {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    Promise.all([listDepartments(), listPositions()])
      .then(([departmentList, positionList]) => {
        if (cancelled) return;
        setDepartments(departmentList);
        setPositions(positionList);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const departmentsById = new Map(departments.map((department) => [department.id, department]));
  const positionsById = new Map(positions.map((position) => [position.id, position]));

  return {
    departments,
    positions,
    loading,
    departmentName: (id: number) => departmentsById.get(id)?.name ?? `Phòng ban #${id}`,
    positionName: (id: number) => positionsById.get(id)?.name ?? `Vị trí #${id}`,
  };
}
