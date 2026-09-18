import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { ApiProblem, setAccessToken, setReauthorizeHandler } from "../../../api/apiClient";
import {
  changeOwnPassword,
  refreshSession,
  signIn as requestSignIn,
  signOut as requestSignOut,
  type AuthenticatedIdentity,
  type Session,
} from "../api/authApi";

interface AuthContextValue {
  session: Session | undefined;
  /** True while the stored refresh token is being exchanged on start-up. */
  restoring: boolean;
  signingIn: boolean;
  error: string | undefined;
  passwordChangeRequired: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/** Only the refresh token is persisted. The access token stays in memory, so closing the tab drops it. */
const refreshTokenKey = "qlns.refreshToken";

function readStoredRefreshToken(): string | undefined {
  try {
    return window.localStorage.getItem(refreshTokenKey) ?? undefined;
  } catch {
    return undefined;
  }
}

function storeRefreshToken(token: string | undefined): void {
  try {
    if (token) window.localStorage.setItem(refreshTokenKey, token);
    else window.localStorage.removeItem(refreshTokenKey);
  } catch {
    // Private-browsing modes deny storage; the session then simply lasts until the tab is closed.
  }
}

function describe(cause: unknown): string {
  if (cause instanceof ApiProblem) {
    switch (cause.code) {
      case "admin.auth.invalid_credentials":
        return "Email hoặc mật khẩu không đúng.";
      case "admin.auth.account_locked":
        return "Tài khoản đang tạm khoá do nhập sai nhiều lần. Vui lòng thử lại sau ít phút.";
      case "admin.auth.account_disabled":
        return "Tài khoản đã bị vô hiệu hoá. Liên hệ quản trị viên hệ thống.";
      default:
        return cause.message;
    }
  }

  return cause instanceof Error ? cause.message : "Không thể kết nối tới máy chủ QLNS.";
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session>();
  const [restoring, setRestoring] = useState(() => readStoredRefreshToken() !== undefined);
  const [signingIn, setSigningIn] = useState(false);
  const [error, setError] = useState<string>();

  // The current refresh token is mirrored in a ref because the 401 recovery path in apiClient runs outside
  // React's render cycle and must always see the newest value.
  const refreshToken = useRef<string | undefined>(readStoredRefreshToken());
  const renewal = useRef<Promise<string | undefined> | undefined>(undefined);

  const apply = useCallback((next: Session) => {
    setAccessToken(next.accessToken);
    refreshToken.current = next.refreshToken;
    storeRefreshToken(next.refreshToken);
    setSession(next);
  }, []);

  const clear = useCallback(() => {
    setAccessToken(undefined);
    refreshToken.current = undefined;
    storeRefreshToken(undefined);
    setSession(undefined);
  }, []);

  /** Exchanges the stored refresh token. Concurrent callers share one in-flight exchange, because the
   *  token is single-use on the server: two parallel refreshes would look like a replay and end the session. */
  const renew = useCallback(async (): Promise<string | undefined> => {
    if (!refreshToken.current) return undefined;
    if (renewal.current) return renewal.current;

    const exchange = (async () => {
      try {
        const next = await refreshSession(refreshToken.current!);
        apply(next);
        return next.accessToken;
      } catch {
        clear();
        return undefined;
      } finally {
        renewal.current = undefined;
      }
    })();

    renewal.current = exchange;
    return exchange;
  }, [apply, clear]);

  useEffect(() => {
    setReauthorizeHandler(renew);
    return () => setReauthorizeHandler(undefined);
  }, [renew]);

  // Restore the session on a full page load.
  useEffect(() => {
    if (!restoring) return;
    void renew().finally(() => setRestoring(false));
  }, [renew, restoring]);

  // Renew shortly before the access token expires so long-lived screens never hit a 401.
  useEffect(() => {
    if (!session?.refreshToken) return;
    const aheadOfExpiry = Math.max(15, session.expiresIn - 60) * 1000;
    const timer = window.setTimeout(() => void renew(), aheadOfExpiry);
    return () => window.clearTimeout(timer);
  }, [renew, session]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      setSigningIn(true);
      setError(undefined);
      try {
        apply(await requestSignIn(email, password));
      } catch (cause) {
        clear();
        setError(describe(cause));
      } finally {
        setSigningIn(false);
      }
    },
    [apply, clear],
  );

  const signOut = useCallback(async () => {
    const token = refreshToken.current;
    clear();
    if (token) {
      // Best effort: the local session is already gone, a failing revocation must not block the user.
      await requestSignOut(token).catch(() => undefined);
    }
  }, [clear]);

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      await changeOwnPassword(currentPassword, newPassword);
      // Changing the password revokes every refresh token, this one included: sign in again.
      clear();
    },
    [clear],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      restoring,
      signingIn,
      error,
      passwordChangeRequired: session?.user.passwordChangeRequired ?? false,
      signIn,
      signOut,
      changePassword,
      clearError: () => setError(undefined),
    }),
    [session, restoring, signingIn, error, signIn, signOut, changePassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider");
  return context;
}

export function useIdentity(): AuthenticatedIdentity | undefined {
  return useAuth().session?.user;
}

/** True when the signed-in actor holds the permission; mirrors the server-side check for UI affordances. */
export function usePermission(permission: string): boolean {
  return useIdentity()?.permissions.includes(permission) ?? false;
}
