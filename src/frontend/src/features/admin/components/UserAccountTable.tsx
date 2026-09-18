import { Badge } from "../../../components/Badge";
import { formatDateTime } from "../../../components/format";
import { roleLabels } from "../../auth/api/authApi";
import type { UserAccount } from "../api/userAccountsApi";

interface Props {
  accounts: UserAccount[];
  busyId?: number;
  canManage: boolean;
  currentUserId?: number;
  onEditRoles: (account: UserAccount) => void;
  onResetPassword: (account: UserAccount) => void;
  onToggleStatus: (account: UserAccount) => void;
}

export function UserAccountTable({
  accounts,
  busyId,
  canManage,
  currentUserId,
  onEditRoles,
  onResetPassword,
  onToggleStatus,
}: Props) {
  return (
    <table className="w-full border-collapse text-sm">
      <thead>
        <tr className="border-b border-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">
          <th className="px-5 py-3">Người dùng</th>
          <th className="px-5 py-3">Vai trò &amp; phạm vi</th>
          <th className="px-5 py-3">Trạng thái</th>
          <th className="px-5 py-3">Đăng nhập gần nhất</th>
          <th className="px-5 py-3 text-right">Hành động</th>
        </tr>
      </thead>
      <tbody>
        {accounts.map((account) => {
          const isSelf = account.id === currentUserId;
          const busy = busyId === account.id;

          return (
            <tr key={account.id} className="border-b border-slate-50 last:border-0">
              <td className="px-5 py-3">
                <p className="font-semibold text-slate-800">{account.displayName}</p>
                <p className="text-xs text-slate-400">{account.email}</p>
              </td>
              <td className="px-5 py-3">
                {account.roles.length === 0 ? (
                  <span className="text-xs text-slate-400">Chưa cấp vai trò</span>
                ) : (
                  <div className="flex flex-wrap gap-1.5">
                    {account.roles.map((grant) => (
                      <Badge
                        key={`${grant.roleCode}-${grant.dataScopeType}-${grant.dataScopeId}`}
                        tone={grant.roleCode === "ROLE_ADMIN" ? "purple" : "blue"}
                      >
                        {roleLabels[grant.roleCode] ?? grant.roleCode}
                        {grant.dataScopeType === "department" && ` #${grant.dataScopeId}`}
                      </Badge>
                    ))}
                  </div>
                )}
              </td>
              <td className="px-5 py-3">
                <div className="flex flex-wrap gap-1.5">
                  <Badge tone={account.status === "active" ? "emerald" : "slate"} dot>
                    {account.status === "active" ? "Đang hoạt động" : "Đã vô hiệu hoá"}
                  </Badge>
                  {account.mustChangePassword && <Badge tone="amber">Phải đổi mật khẩu</Badge>}
                  {account.lockedUntil && <Badge tone="rose">Đang tạm khoá</Badge>}
                </div>
              </td>
              <td className="px-5 py-3 text-slate-500">
                {account.lastLoginAt ? formatDateTime(account.lastLoginAt) : "Chưa đăng nhập"}
              </td>
              <td className="px-5 py-3">
                <div className="flex items-center justify-end gap-1.5">
                  {canManage && !isSelf && (
                    <>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => onEditRoles(account)}
                        className="rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50"
                      >
                        Vai trò
                      </button>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => onResetPassword(account)}
                        className="rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-50 disabled:opacity-50"
                      >
                        Đặt lại mật khẩu
                      </button>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => onToggleStatus(account)}
                        className={`rounded-lg border px-2.5 py-1.5 text-xs font-semibold disabled:opacity-50 ${
                          account.status === "active"
                            ? "border-rose-200 text-rose-600 hover:bg-rose-50"
                            : "border-emerald-200 text-emerald-700 hover:bg-emerald-50"
                        }`}
                      >
                        {account.status === "active" ? "Vô hiệu hoá" : "Kích hoạt"}
                      </button>
                    </>
                  )}
                  {isSelf && <span className="text-xs text-slate-400">Tài khoản của bạn</span>}
                </div>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
