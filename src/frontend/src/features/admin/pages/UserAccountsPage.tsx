import { useState } from "react";
import { Modal } from "../../../components/Modal";
import { PageHeader } from "../../../components/PageHeader";
import { Pagination } from "../../../components/Pagination";
import { StateBanner } from "../../../components/StateBanner";
import { IdentityPermissions } from "../../auth/api/permissions";
import { useAuth, usePermission } from "../../auth/context/AuthContext";
import {
  createUserAccount,
  replaceRoleGrants,
  resetUserPassword,
  setUserAccountStatus,
  type RoleGrant,
  type UserAccount,
} from "../api/userAccountsApi";
import { RoleGrantEditor } from "../components/RoleGrantEditor";
import { UserAccountTable } from "../components/UserAccountTable";
import { useUserAccounts } from "../hooks/useUserAccounts";

type Dialog =
  | { kind: "create" }
  | { kind: "roles"; account: UserAccount }
  | { kind: "password"; account: UserAccount }
  | undefined;

export function UserAccountsPage() {
  const { session } = useAuth();
  const canManage = usePermission(IdentityPermissions.userManage);
  const { filters, data, roles, loading, error, updateFilters, setPage, reload, replaceAccount } = useUserAccounts();

  const [dialog, setDialog] = useState<Dialog>();
  const [busyId, setBusyId] = useState<number>();
  const [actionError, setActionError] = useState<string>();

  async function run(account: UserAccount, action: () => Promise<UserAccount>) {
    setBusyId(account.id);
    setActionError(undefined);
    try {
      replaceAccount(await action());
    } catch (cause) {
      setActionError(cause instanceof Error ? cause.message : "Không thực hiện được yêu cầu.");
    } finally {
      setBusyId(undefined);
    }
  }

  return (
    <>
      <PageHeader
        breadcrumb="Quản trị hệ thống"
        title="Tài khoản & phân quyền"
        actions={
          canManage && (
            <button
              type="button"
              onClick={() => setDialog({ kind: "create" })}
              className="flex items-center gap-2 rounded-lg bg-brand-600 px-3.5 py-2 text-sm font-semibold text-white hover:bg-brand-700"
            >
              <span className="material-symbols-outlined icon-sm">person_add</span>
              Tạo tài khoản
            </button>
          )
        }
      />

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <label className="min-w-56 flex-1">
          <span className="mb-1.5 block text-xs font-medium text-slate-500">Tìm theo tên hoặc email</span>
          <input
            defaultValue={filters.search ?? ""}
            onBlur={(event) => updateFilters({ search: event.target.value.trim() || undefined })}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
        </label>
        <label>
          <span className="mb-1.5 block text-xs font-medium text-slate-500">Trạng thái</span>
          <select
            value={filters.status ?? ""}
            onChange={(event) =>
              updateFilters({ status: event.target.value === "" ? undefined : (event.target.value as "active" | "disabled") })
            }
            className="rounded-lg border border-slate-200 px-3 py-2 text-sm"
          >
            <option value="">Tất cả</option>
            <option value="active">Đang hoạt động</option>
            <option value="disabled">Đã vô hiệu hoá</option>
          </select>
        </label>
      </div>

      <StateBanner loading={loading} error={error} onRetry={reload} />
      {actionError && (
        <p role="alert" className="mb-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {actionError}
        </p>
      )}

      {data && (
        <div className="overflow-hidden rounded-2xl border border-slate-100 bg-white">
          <div className="overflow-x-auto hrm-scroll">
            <UserAccountTable
              accounts={data.items}
              busyId={busyId}
              canManage={canManage}
              currentUserId={session?.user.userId}
              onEditRoles={(account) => setDialog({ kind: "roles", account })}
              onResetPassword={(account) => setDialog({ kind: "password", account })}
              onToggleStatus={(account) =>
                void run(account, () =>
                  setUserAccountStatus(account, account.status === "active" ? "disabled" : "active"),
                )
              }
            />
          </div>
          <Pagination page={data.page} itemLabel="tài khoản" onPageChange={setPage} />
        </div>
      )}

      {dialog?.kind === "create" && (
        <CreateAccountDialog
          roles={roles}
          onClose={() => setDialog(undefined)}
          onCreated={() => {
            setDialog(undefined);
            void reload();
          }}
        />
      )}

      {dialog?.kind === "roles" && (
        <RoleGrantsDialog
          account={dialog.account}
          roles={roles}
          onClose={() => setDialog(undefined)}
          onSaved={(account) => {
            replaceAccount(account);
            setDialog(undefined);
          }}
        />
      )}

      {dialog?.kind === "password" && (
        <ResetPasswordDialog
          account={dialog.account}
          onClose={() => setDialog(undefined)}
          onSaved={(account) => {
            replaceAccount(account);
            setDialog(undefined);
          }}
        />
      )}
    </>
  );
}

function CreateAccountDialog({
  roles,
  onClose,
  onCreated,
}: {
  roles: Parameters<typeof RoleGrantEditor>[0]["roles"];
  onClose: () => void;
  onCreated: () => void;
}) {
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [initialPassword, setInitialPassword] = useState("");
  const [employeeId, setEmployeeId] = useState("");
  const [grants, setGrants] = useState<RoleGrant[]>([]);
  const [error, setError] = useState<string>();
  const [saving, setSaving] = useState(false);

  async function submit() {
    setSaving(true);
    setError(undefined);
    try {
      await createUserAccount({
        email: email.trim(),
        displayName: displayName.trim(),
        initialPassword,
        employeeId: employeeId.trim() === "" ? undefined : Number(employeeId),
        roles: grants,
      });
      onCreated();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không tạo được tài khoản.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      width="lg"
      title="Tạo tài khoản"
      subtitle="Mật khẩu ban đầu phải được đổi ở lần đăng nhập đầu tiên."
      onClose={onClose}
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg border border-slate-200 px-3.5 py-2 text-sm font-semibold text-slate-600">
            Huỷ
          </button>
          <button
            type="button"
            disabled={saving}
            onClick={() => void submit()}
            className="rounded-lg bg-brand-600 px-3.5 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {saving ? "Đang lưu…" : "Tạo tài khoản"}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Email công vụ</span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Tên hiển thị</span>
          <input
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
        </label>
        <div className="flex flex-wrap gap-4">
          <label className="min-w-56 flex-1">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Mật khẩu ban đầu</span>
            <input
              type="password"
              autoComplete="new-password"
              value={initialPassword}
              onChange={(event) => setInitialPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </label>
          <label className="w-40">
            <span className="mb-1.5 block text-sm font-medium text-slate-700">Mã nhân viên</span>
            <input
              inputMode="numeric"
              placeholder="không bắt buộc"
              value={employeeId}
              onChange={(event) => setEmployeeId(event.target.value)}
              className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
            />
          </label>
        </div>

        <div>
          <p className="mb-2 text-sm font-semibold text-slate-800">Vai trò &amp; phạm vi dữ liệu</p>
          <RoleGrantEditor roles={roles} value={grants} onChange={setGrants} />
        </div>

        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}
      </div>
    </Modal>
  );
}

function RoleGrantsDialog({
  account,
  roles,
  onClose,
  onSaved,
}: {
  account: UserAccount;
  roles: Parameters<typeof RoleGrantEditor>[0]["roles"];
  onClose: () => void;
  onSaved: (account: UserAccount) => void;
}) {
  const [grants, setGrants] = useState<RoleGrant[]>(account.roles);
  const [error, setError] = useState<string>();
  const [saving, setSaving] = useState(false);

  async function submit() {
    setSaving(true);
    setError(undefined);
    try {
      onSaved(await replaceRoleGrants(account, grants));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không cập nhật được vai trò.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      width="lg"
      title={`Vai trò của ${account.displayName}`}
      subtitle="Danh sách này thay thế toàn bộ vai trò hiện có của tài khoản."
      onClose={onClose}
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg border border-slate-200 px-3.5 py-2 text-sm font-semibold text-slate-600">
            Huỷ
          </button>
          <button
            type="button"
            disabled={saving}
            onClick={() => void submit()}
            className="rounded-lg bg-brand-600 px-3.5 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {saving ? "Đang lưu…" : "Lưu vai trò"}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <RoleGrantEditor roles={roles} value={grants} onChange={setGrants} />
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}
      </div>
    </Modal>
  );
}

function ResetPasswordDialog({
  account,
  onClose,
  onSaved,
}: {
  account: UserAccount;
  onClose: () => void;
  onSaved: (account: UserAccount) => void;
}) {
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string>();
  const [saving, setSaving] = useState(false);

  async function submit() {
    setSaving(true);
    setError(undefined);
    try {
      onSaved(await resetUserPassword(account, newPassword));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không đặt lại được mật khẩu.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      title={`Đặt lại mật khẩu cho ${account.displayName}`}
      subtitle="Mọi phiên đăng nhập của tài khoản sẽ bị chấm dứt và người dùng phải đổi mật khẩu khi đăng nhập lại."
      onClose={onClose}
      footer={
        <>
          <button type="button" onClick={onClose} className="rounded-lg border border-slate-200 px-3.5 py-2 text-sm font-semibold text-slate-600">
            Huỷ
          </button>
          <button
            type="button"
            disabled={saving}
            onClick={() => void submit()}
            className="rounded-lg bg-brand-600 px-3.5 py-2 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {saving ? "Đang lưu…" : "Đặt lại mật khẩu"}
          </button>
        </>
      }
    >
      <div className="space-y-4">
        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Mật khẩu tạm thời</span>
          <input
            type="password"
            autoComplete="new-password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
          />
        </label>
        <p className="text-xs text-slate-500">
          Tối thiểu 10 ký tự, kết hợp ít nhất 3 trong 4 nhóm ký tự. Hãy chuyển mật khẩu này cho người dùng qua
          kênh an toàn, ngoài hệ thống.
        </p>
        {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}
      </div>
    </Modal>
  );
}
