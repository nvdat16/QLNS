import { useEffect, useState } from "react";
import { Drawer } from "../../../components/Drawer";
import { StateBanner } from "../../../components/StateBanner";
import { useEmployeeDetail } from "../hooks/useEmployeeDetail";
import { EmployeeStatusBadge } from "./employeeStatusBadge";

interface Props {
  employeeId: number | undefined;
  departmentName: (id: number) => string;
  positionName: (id: number) => string;
  onClose: () => void;
}

export function EmployeeDetailDrawer({ employeeId, departmentName, positionName, onClose }: Props) {
  const { employee, loading, error, updateProfile, reload } = useEmployeeDetail(employeeId);
  const [phone, setPhone] = useState("");
  const [personalEmail, setPersonalEmail] = useState("");
  const [temporaryAddress, setTemporaryAddress] = useState("");

  useEffect(() => {
    setPhone(employee?.phone ?? "");
    setPersonalEmail(employee?.personalEmail ?? "");
    setTemporaryAddress(employee?.temporaryAddress ?? "");
  }, [employee]);

  return (
    <Drawer open={employeeId !== undefined} onClose={onClose}>
      <StateBanner loading={loading && !employee} error={error} onRetry={reload} loadingLabel="Đang tải hồ sơ nhân viên…" />

      {employee && (
        <div className="space-y-6">
          <div>
            <p className="font-mono text-xs font-semibold text-brand-600">{employee.employeeCode}</p>
            <h3 className="mt-1 text-lg font-bold text-slate-900">
              {employee.lastName} {employee.firstName}
            </h3>
            <p className="text-sm text-slate-500">{positionName(employee.positionId)}</p>
            <div className="mt-2">
              <EmployeeStatusBadge status={employee.status} />
            </div>
          </div>

          <section className="rounded-xl bg-slate-50 p-4">
            <h4 className="mb-3 text-xs font-bold uppercase tracking-wide text-slate-400">Thông tin công tác</h4>
            <dl className="space-y-2 text-sm">
              <Row label="Email công vụ" value={employee.workEmail} mono />
              <Row label="Phòng ban" value={departmentName(employee.departmentId)} />
              <Row label="Địa điểm làm việc" value={employee.officeLocation ?? "—"} />
              <Row label="Ngày vào làm" value={new Date(employee.hireDate).toLocaleDateString("vi-VN")} />
            </dl>
          </section>

          <section className="rounded-xl border border-slate-100 p-4">
            <h4 className="mb-3 text-xs font-bold uppercase tracking-wide text-slate-400">
              Thông tin cá nhân (có thể cập nhật)
            </h4>
            <form
              className="space-y-3"
              onSubmit={(event) => {
                event.preventDefault();
                void updateProfile({
                  phone: phone || undefined,
                  personalEmail: personalEmail || undefined,
                  temporaryAddress: temporaryAddress || undefined,
                });
              }}
            >
              <Field label="Số điện thoại" value={phone} onChange={setPhone} />
              <Field label="Email cá nhân" value={personalEmail} onChange={setPersonalEmail} type="email" />
              <Field label="Địa chỉ tạm trú" value={temporaryAddress} onChange={setTemporaryAddress} />
              <button
                type="submit"
                disabled={loading}
                className="w-full rounded-lg bg-brand-600 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
              >
                Lưu thay đổi
              </button>
            </form>
          </section>
        </div>
      )}
    </Drawer>
  );
}

function Row({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-500">{label}</dt>
      <dd className={`text-right font-medium text-slate-800 ${mono ? "font-mono" : ""}`}>{value}</dd>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-slate-500">{label}</span>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
      />
    </label>
  );
}
