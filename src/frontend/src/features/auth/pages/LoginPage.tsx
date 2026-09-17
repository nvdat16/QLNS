import { useState, type FormEvent } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { personaLabels, personas, type Persona } from "../api/authApi";

type Tab = "login" | "register";

const features = [
  { icon: "badge", label: "Hồ sơ nhân sự" },
  { icon: "person_search", label: "Tuyển dụng ATS" },
  { icon: "description", label: "Hợp đồng lao động" },
];

export function LoginPage() {
  const { session, signIn, signingIn, error } = useAuth();
  const location = useLocation();
  const [tab, setTab] = useState<Tab>("login");
  const [persona, setPersona] = useState<Persona>("hr-manager");

  if (session) {
    const from = (location.state as { from?: Location })?.from;
    return <Navigate to={from?.pathname ?? "/employees"} replace />;
  }

  function handleLogin(event: FormEvent) {
    event.preventDefault();
    void signIn(persona);
  }

  return (
    <div className="flex min-h-screen bg-[#f5f7fb]">
      <div className="relative hidden w-1/2 flex-col justify-between overflow-hidden bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 p-12 text-white lg:flex">
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600">
            <span className="material-symbols-outlined icon-md">business_center</span>
          </span>
          <div>
            <p className="text-base font-bold">NexusHR Enterprise</p>
            <p className="text-xs text-slate-400">Human Resource Management System</p>
          </div>
        </div>

        <div>
          <span className="inline-block rounded-full border border-white/15 bg-white/5 px-3 py-1 text-xs font-medium text-slate-300">
            Không gian làm việc bảo mật
          </span>
          <h1 className="mt-5 max-w-md text-3xl font-bold leading-tight">
            Quản trị nhân sự tập trung, hiệu quả.
          </h1>
          <div className="mt-8 flex flex-wrap gap-3">
            {features.map((feature) => (
              <span
                key={feature.label}
                className="inline-flex items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-3 py-2 text-sm text-slate-200"
              >
                <span className="material-symbols-outlined icon-sm">{feature.icon}</span>
                {feature.label}
              </span>
            ))}
          </div>
        </div>

        <p className="text-xs text-slate-500">© 2026 NexusHR Enterprise. Internal use only.</p>
      </div>

      <div className="flex w-full items-center justify-center px-6 py-12 lg:w-1/2">
        <div className="w-full max-w-sm">
          <div role="tablist" className="mb-8 flex gap-1 rounded-xl bg-slate-100 p-1">
            <button
              type="button"
              role="tab"
              aria-selected={tab === "login"}
              onClick={() => setTab("login")}
              className={`flex-1 rounded-lg py-2 text-sm font-semibold transition-colors ${tab === "login" ? "bg-white text-brand-700 shadow-sm" : "text-slate-500"}`}
            >
              Đăng nhập
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={tab === "register"}
              onClick={() => setTab("register")}
              className={`flex-1 rounded-lg py-2 text-sm font-semibold transition-colors ${tab === "register" ? "bg-white text-brand-700 shadow-sm" : "text-slate-500"}`}
            >
              Đăng ký
            </button>
          </div>

          {tab === "login" ? (
            <form onSubmit={handleLogin} className="space-y-4">
              <h2 className="text-lg font-bold text-slate-900">Đăng nhập môi trường phát triển</h2>
              <p className="text-sm text-slate-500">
                Chọn vai trò để lấy token phát triển từ backend (không có luồng email/mật khẩu vì hệ thống dùng
                nhà cung cấp định danh (IdP) doanh nghiệp cho môi trường thật).
              </p>

              <label className="block">
                <span className="mb-1.5 block text-sm font-medium text-slate-700">Vai trò đăng nhập</span>
                <select
                  value={persona}
                  onChange={(event) => setPersona(event.target.value as Persona)}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2.5 text-sm text-slate-800 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                >
                  {personas.map((option) => (
                    <option key={option} value={option}>
                      {personaLabels[option]}
                    </option>
                  ))}
                </select>
              </label>

              {error && (
                <p role="alert" className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">
                  {error}
                </p>
              )}

              <button
                type="submit"
                disabled={signingIn}
                className="flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {signingIn ? "Đang đăng nhập…" : "Đăng nhập"}
                {!signingIn && <span className="material-symbols-outlined icon-sm">arrow_forward</span>}
              </button>
            </form>
          ) : (
            <div className="space-y-4">
              <h2 className="text-lg font-bold text-slate-900">Đăng ký tài khoản</h2>
              <p className="text-sm text-slate-500">
                Việc cấp tài khoản mới do nhà cung cấp định danh (IdP) doanh nghiệp quản lý và nằm ngoài phạm vi
                triển khai hiện tại của QLNS.
              </p>
              <p className="rounded-lg bg-slate-50 px-3 py-3 text-sm text-slate-600">
                Vui lòng liên hệ quản trị viên hệ thống để được cấp tài khoản và phân quyền truy cập.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
