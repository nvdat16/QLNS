import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";
import { setAccessToken } from "../../../api/apiClient";
import { signInWithPersona, type DevSession, type Persona } from "../api/authApi";

interface AuthContextValue {
  session: DevSession | undefined;
  signingIn: boolean;
  error: string | undefined;
  signIn: (persona: Persona) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<DevSession>();
  const [signingIn, setSigningIn] = useState(false);
  const [error, setError] = useState<string>();

  const signIn = useCallback(async (persona: Persona) => {
    setSigningIn(true);
    setError(undefined);
    try {
      const next = await signInWithPersona(persona);
      setAccessToken(next.token);
      setSession(next);
    } catch (cause) {
      setError(
        cause instanceof Error
          ? cause.message
          : "Không thể đăng nhập. Đảm bảo backend đang chạy ở môi trường Development.",
      );
    } finally {
      setSigningIn(false);
    }
  }, []);

  const signOut = useCallback(() => {
    setAccessToken(undefined);
    setSession(undefined);
  }, []);

  const value = useMemo(
    () => ({ session, signingIn, error, signIn, signOut }),
    [session, signingIn, error, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider");
  return context;
}
