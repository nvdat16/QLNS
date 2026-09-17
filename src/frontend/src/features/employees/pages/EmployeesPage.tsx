import { useState } from "react";
import { Pagination } from "../../../components/Pagination";
import { PageHeader } from "../../../components/PageHeader";
import { StateBanner } from "../../../components/StateBanner";
import { useOrganizationDirectory } from "../../organization/hooks/useOrganizationDirectory";
import { EmployeeDetailDrawer } from "../components/EmployeeDetailDrawer";
import { EmployeeTable } from "../components/EmployeeTable";
import { EmployeeToolbar } from "../components/EmployeeToolbar";
import { useEmployees } from "../hooks/useEmployees";

export function EmployeesPage() {
  const { data, loading, error, updateFilters, setPage, reload } = useEmployees();
  const { departments, departmentName, positionName } = useOrganizationDirectory();
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number>();

  return (
    <div>
      <PageHeader breadcrumb="Không gian làm việc / Hồ sơ nhân viên" title="Quản lý hồ sơ nhân viên" />

      <div className="overflow-hidden rounded-2xl border border-[#edf0f4] bg-white shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
        <EmployeeToolbar departments={departments} onChange={updateFilters} />

        {(loading && !data) || error ? (
          <div className="p-5">
            <StateBanner loading={loading && !data} error={error} onRetry={reload} loadingLabel="Đang tải danh sách nhân viên…" />
          </div>
        ) : (
          data && (
            <>
              <div className="overflow-x-auto hrm-scroll">
                <EmployeeTable
                  employees={data.items}
                  departmentName={departmentName}
                  positionName={positionName}
                  onSelect={setSelectedEmployeeId}
                />
              </div>
              <Pagination page={data.page} itemLabel="nhân viên" onPageChange={setPage} />
            </>
          )
        )}
      </div>

      <EmployeeDetailDrawer
        employeeId={selectedEmployeeId}
        departmentName={departmentName}
        positionName={positionName}
        onClose={() => setSelectedEmployeeId(undefined)}
      />
    </div>
  );
}
