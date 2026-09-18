import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function ProtectedRoute() {
  const { session, restoring } = useAuth();
  const location = useLocation();

  // While the stored refresh token is being exchanged there is no session yet; redirecting would throw the
  // user back to the sign-in screen on every full page load.
  if (restoring) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#f5f7fb] text-sm text-slate-500">
        Đang khôi phục phiên đăng nhập…
      </div>
    );
  }

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  // A session issued for a pending password change may do nothing else.
  if (session.user.passwordChangeRequired && location.pathname !== "/change-password") {
    return <Navigate to="/change-password" replace />;
  }

  return <Outlet />;
}
