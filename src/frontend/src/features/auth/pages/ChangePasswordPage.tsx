import { useState, type FormEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { ApiProblem } from "../../../api/apiClient";
import { useAuth } from "../context/AuthContext";

/** Forced password change. Reached with the restricted session issued after an administrative reset, which
 *  can call nothing else, and from the sidebar for a voluntary change. */
export function ChangePasswordPage() {
  const { session, changePassword } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmation, setConfirmation] = useState("");
  const [error, setError] = useState<string>();
  const [saving, setSaving] = useState(false);
  const [done, setDone] = useState(false);

  // The order matters: a successful change revokes every token, so the session is already gone by the time
  // the confirmation renders. Checking `done` first keeps the user on a page that tells them what happened.
  if (!done && !session) return <Navigate to="/login" replace />;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (newPassword !== confirmation) {
      setError("Mật khẩu xác nhận không trùng khớp.");
      return;
    }

    setSaving(true);
    setError(undefined);
    try {
      await changePassword(currentPassword, newPassword);
      setDone(true);
    } catch (cause) {
      setError(
        cause instanceof ApiProblem && cause.code === "admin.auth.invalid_credentials"
          ? "Mật khẩu hiện tại không đúng."
          : cause instanceof Error
            ? cause.message
            : "Không thể đổi mật khẩu.",
      );
    } finally {
      setSaving(false);
    }
  }

  if (done) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#f5f7fb] px-6 py-12">
        <div className="w-full max-w-sm space-y-4 rounded-2xl bg-white p-8 text-center shadow-sm">
          <span className="mx-auto flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-600 text-white">
            <span className="material-symbols-outlined icon-md">check</span>
          </span>
          <h1 className="text-lg font-bold text-slate-900">Đã đổi mật khẩu</h1>
          <p className="text-sm text-slate-500">
            Mọi phiên đăng nhập của tài khoản đã bị chấm dứt. Hãy đăng nhập lại bằng mật khẩu mới.
          </p>
          <Link
            to="/login"
            className="inline-flex w-full items-center justify-center rounded-lg bg-brand-600 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            Đăng nhập lại
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#f5f7fb] px-6 py-12">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-4 rounded-2xl bg-white p-8 shadow-sm">
        <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600 text-white">
          <span className="material-symbols-outlined icon-md">key</span>
        </span>
        <h1 className="text-lg font-bold text-slate-900">Đổi mật khẩu</h1>
        <p className="text-sm text-slate-500">
          {session?.user.passwordChangeRequired
            ? "Mật khẩu hiện tại do quản trị viên cấp nên phải được thay thế trước khi sử dụng hệ thống."
            : "Mật khẩu mới tối thiểu 10 ký tự, kết hợp ít nhất 3 trong 4 nhóm: chữ thường, chữ hoa, số, ký tự đặc biệt."}
        </p>

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Mật khẩu hiện tại</span>
          <input
            type="password"
            autoComplete="current-password"
            required
            value={currentPassword}
            onChange={(event) => setCurrentPassword(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Mật khẩu mới</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={10}
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <label className="block">
          <span className="mb-1.5 block text-sm font-medium text-slate-700">Xác nhận mật khẩu mới</span>
          <input
            type="password"
            autoComplete="new-password"
            required
            value={confirmation}
            onChange={(event) => setConfirmation(event.target.value)}
            className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        {error && (
          <p role="alert" className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">
            {error}
          </p>
        )}

        <p className="text-xs text-slate-400">
          Sau khi đổi mật khẩu, mọi phiên đăng nhập trên các thiết bị khác sẽ bị chấm dứt.
        </p>

        <button
          type="submit"
          disabled={saving}
          className="w-full rounded-lg bg-brand-600 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? "Đang lưu…" : "Đổi mật khẩu"}
        </button>
      </form>
    </div>
  );
}
