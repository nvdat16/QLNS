import { useState } from "react";
import type { DataScope } from "../../auth/api/authApi";
import { roleLabels } from "../../auth/api/authApi";
import type { Role, RoleGrant } from "../api/userAccountsApi";

interface Props {
  roles: Role[];
  value: RoleGrant[];
  onChange: (grants: RoleGrant[]) => void;
}

const scopeLabels: Record<DataScope, string> = {
  self: "Chính mình",
  department: "Phòng ban",
  organization: "Toàn tổ chức",
};

function sameGrant(a: RoleGrant, b: RoleGrant): boolean {
  return a.roleCode === b.roleCode && a.dataScopeType === b.dataScopeType && a.dataScopeId === b.dataScopeId;
}

/** Builds the list of user_roles rows. Department scope is the only one that carries an id, mirroring the
 *  ck_user_roles_scope constraint, so the id field only appears for that choice. */
export function RoleGrantEditor({ roles, value, onChange }: Props) {
  const assignable = roles.filter((role) => role.isAssignable);
  const [roleCode, setRoleCode] = useState(assignable[0]?.code ?? "");
  const [scope, setScope] = useState<DataScope>("organization");
  const [departmentId, setDepartmentId] = useState("");
  const [error, setError] = useState<string>();

  function add() {
    if (!roleCode) return;
    const id = scope === "department" ? Number(departmentId) : 0;
    if (scope === "department" && (!Number.isInteger(id) || id <= 0)) {
      setError("Nhập mã phòng ban hợp lệ cho phạm vi phòng ban.");
      return;
    }

    const grant: RoleGrant = { roleCode, dataScopeType: scope, dataScopeId: id };
    if (value.some((existing) => sameGrant(existing, grant))) {
      setError("Vai trò này đã được cấp với cùng phạm vi.");
      return;
    }

    setError(undefined);
    setDepartmentId("");
    onChange([...value, grant]);
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-end gap-2">
        <label className="min-w-40 flex-1">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Vai trò</span>
          <select
            value={roleCode}
            onChange={(event) => setRoleCode(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          >
            {assignable.map((role) => (
              <option key={role.code} value={role.code}>
                {roleLabels[role.code] ?? role.name}
              </option>
            ))}
          </select>
        </label>

        <label className="min-w-32">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Phạm vi dữ liệu</span>
          <select
            value={scope}
            onChange={(event) => setScope(event.target.value as DataScope)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          >
            {(Object.keys(scopeLabels) as DataScope[]).map((option) => (
              <option key={option} value={option}>
                {scopeLabels[option]}
              </option>
            ))}
          </select>
        </label>

        {scope === "department" && (
          <label className="w-28">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Mã phòng ban</span>
            <input
              inputMode="numeric"
              value={departmentId}
              onChange={(event) => setDepartmentId(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </label>
        )}

        <button
          type="button"
          onClick={add}
          className="rounded-lg border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
        >
          Thêm
        </button>
      </div>

      {error && <p className="text-sm text-rose-600">{error}</p>}

      {value.length === 0 ? (
        <p className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800">
          Chưa cấp vai trò nào — tài khoản đăng nhập được nhưng không truy cập được chức năng nào.
        </p>
      ) : (
        <ul className="space-y-2">
          {value.map((grant) => (
            <li
              key={`${grant.roleCode}-${grant.dataScopeType}-${grant.dataScopeId}`}
              className="flex items-center justify-between gap-3 rounded-lg bg-slate-50 px-3 py-2 text-sm"
            >
              <span className="text-slate-700">
                <span className="font-semibold">{roleLabels[grant.roleCode] ?? grant.roleCode}</span>
                {" · "}
                {scopeLabels[grant.dataScopeType]}
                {grant.dataScopeType === "department" && ` #${grant.dataScopeId}`}
              </span>
              <button
                type="button"
                onClick={() => onChange(value.filter((item) => !sameGrant(item, grant)))}
                aria-label="Bỏ vai trò"
                className="rounded-lg p-1 text-slate-400 hover:bg-white hover:text-rose-600"
              >
                <span className="material-symbols-outlined icon-sm">delete</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
