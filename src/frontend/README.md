# QLNS React frontend

Feature-oriented React/Vite application covering sign-in, employee records, contracts, the recruitment pipeline and
account administration. It does not modify the standalone prototypes in `uiux/`.

- `src/app`: composition, routing and global presentation (shell, sidebar).
- `src/api`: shared HTTP client, Problem Details mapping and the token seam.
- `src/features/<feature>/api`: typed API contract adapter.
- `src/features/<feature>/hooks`: server interaction state.
- `src/features/<feature>/components`: feature UI.
- `src/features/<feature>/pages`: routed screen boundaries.

Features: `auth` (ADM-01), `admin` (ADM-02 accounts and role grants), `employees`, `contracts`, `organization`,
`recruitment`.

## How the session works

Sign-in posts to `POST /api/v1/auth/login` and the app then holds two tokens with deliberately different lifetimes:

| Token | Where it lives | Why |
|---|---|---|
| Access token | in memory only (`setAccessToken` in `src/api/apiClient.ts`) | closing the tab drops it; it is never written to storage where another script could read it |
| Refresh token | `localStorage` under `qlns.refreshToken` | survives a page reload, which is what makes the session feel persistent |

`AuthContext` renews the session in three situations: on a full page load, about a minute before the access token
expires, and when a request comes back `401`. Concurrent renewals share one in-flight exchange, because the server
treats a refresh token as single-use — two parallel refreshes would look like a replay and end the session.

A password reset by an administrator yields a **restricted session**: no refresh token and no permissions, so
`ProtectedRoute` sends the user to `/change-password` and nothing else is reachable until the password is replaced.

Permissions in `session.user.permissions` decide which affordances are rendered (`usePermission`). They are a UI
convenience only — the server re-checks permission and data scope on every request.

## Local development

```bash
npm install
npm run dev     # http://localhost:5173, expects the API on http://localhost:5000
npm run build   # tsc -b && vite build
```

`VITE_API_BASE_URL` points at the API; the backend's local port is **5080** (see `src/backend/README.md`), so set it
accordingly. `VITE_DEV_ACCESS_TOKEN` is a temporary local-development seam that pre-seeds the in-memory access token —
it lets you open a screen without going through `/login`. Never commit a token or use that variable in a deployed
environment.

Seeded development accounts all use the password `Qlns@2026`: `admin@qlns.local` (Super Admin — the only one that sees
*Tài khoản & phân quyền*), `hr.manager@qlns.local`, `hr.officer@qlns.local`, `eng.manager@qlns.local`,
`recruiter@qlns.local`, `dev.nguyen@qlns.local`, `it.admin@qlns.local`.
