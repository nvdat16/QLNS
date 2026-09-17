import { IconButton } from "../../../components/IconButton";
import type { EmployeeSummary } from "../api/employeesApi";
import { EmployeeStatusBadge } from "./employeeStatusBadge";

interface Props {
  employees: EmployeeSummary[];
  departmentName: (id: number) => string;
  positionName: (id: number) => string;
  onSelect: (employeeId: number) => void;
}

export function EmployeeTable({ employees, departmentName, positionName, onSelect }: Props) {
  return (
    <table className="w-full text-left text-sm">
      <thead>
        <tr className="border-b border-slate-100 bg-[#fafbfc] text-[11px] font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Mã nhân viên</th>
          <th className="px-5 py-3">Nhân viên</th>
          <th className="px-5 py-3">Phòng ban &amp; chức vụ</th>
          <th className="px-5 py-3">Địa điểm</th>
          <th className="px-5 py-3 text-center">Trạng thái</th>
          <th className="px-5 py-3 text-right">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        {employees.map((employee) => (
          <tr
            key={employee.id}
            onClick={() => onSelect(employee.id)}
            className="cursor-pointer border-b border-[#eef0f4] last:border-0 hover:bg-slate-50"
          >
            <td className="px-5 py-3.5 font-mono text-xs font-semibold text-brand-600">{employee.employeeCode}</td>
            <td className="px-5 py-3.5">
              <p className="font-semibold text-slate-800">
                {employee.lastName} {employee.firstName}
              </p>
              <p className="text-[11px] text-slate-400">{employee.workEmail}</p>
            </td>
            <td className="px-5 py-3.5">
              <p className="font-medium text-slate-700">{departmentName(employee.departmentId)}</p>
              <p className="text-[11px] text-slate-400">{positionName(employee.positionId)}</p>
            </td>
            <td className="px-5 py-3.5 text-slate-600">{employee.officeLocation ?? "—"}</td>
            <td className="px-5 py-3.5 text-center">
              <EmployeeStatusBadge status={employee.status} />
            </td>
            <td className="px-5 py-3.5 text-right">
              <IconButton icon="visibility" label="Xem chi tiết" onClick={() => onSelect(employee.id)} />
            </td>
          </tr>
        ))}
        {employees.length === 0 && (
          <tr>
            <td colSpan={6} className="px-5 py-10 text-center text-sm text-slate-400">
              Không tìm thấy nhân viên phù hợp với bộ lọc.
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
