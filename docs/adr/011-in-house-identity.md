# ADR-011 — Authentication and account administration in house

- **Status:** Accepted 2026-09-18
- **Owner:** Architect
- **Related:** [ADR-003](003-backend-enforces-authorization.md), [ADR-010](010-delivery-scope-two-pillars.md)

## Context

The first baseline left the Identity Provider blank ("provider not yet chosen"). The consequence was a system with full
authorization but **no way to sign in at all**, apart from a `/dev/token` endpoint that only exists in the Development
environment. Choosing an external IdP, however, is a procurement decision nobody has made.

## Decision

Bring the two leaf functions *Account Management* and *Roles/Permissions & Data Access Scope* of the System
Administration pillar into scope, and issue our own sessions:

- Passwords hashed with **PBKDF2-HMAC-SHA512**, 210 000 iterations, a 128-bit per-password salt, with the parameters
  encoded inside the hash string (`user_credentials.password_hash`) so the iteration count can be raised without a
  migration.
- The access token is a **JWT signed with HS256** issued by the API itself using `Authentication:Jwt:SigningKey`, valid
  for 30 minutes by default.
- The refresh token is **single-use**, stored only as a SHA-256 digest in `refresh_tokens`, with replay detection.
- The role → permission matrix is **reference data** in `roles` / `role_permissions`, loaded from `seed_roles.sql`.

`Authentication:Jwt:SigningKey` is required: without it the host stops at startup rather than running half-configured.
There is deliberately **no** silent fallback to an external OIDC authority.

## Alternatives considered

- **Integrate Keycloak or Entra ID** — rejected: it adds an operational component and requires a procurement decision
  that has not been made.
- **Build sign-in only and provision accounts by hand in SQL** — rejected: it leaves no audit trail for privilege
  escalation.

## Consequences

1. The system now stores passwords itself. The risk is reduced by PBKDF2 at 210 000 iterations, a 15-minute lockout
   after 5 failures, and an audit record for every sign-in attempt including the failures (`result = 'rejected'`).
2. A stateless access token **cannot be revoked**, so its 30-minute lifetime is the upper bound on revoking authority;
   the refresh token is the revocable artifact.
3. `Authentication:Jwt:SigningKey` becomes a first-class secret of the system.
4. The token claims keep **exactly** their previous shape (`qlns_user_id`, `qlns_employee_id`, `data_scope`,
   `department_id`, `permission`), so no business module had to change. Should the system federate with an external IdP
   later, that provider need only issue the same claim set and replace the `/api/v1/auth/*` group; the authorization
   code stays as it is. Because federation replaces the whole sign-in endpoint group rather than one configuration
   value, it must be its own ADR.
5. Four tables are added to the canonical schema (delta v1.2): `roles`, `role_permissions`, `user_credentials` and
   `refresh_tokens`; `user_roles.role_code` becomes a foreign key to `roles(code)`.

## Not done

MFA, a forgotten-password flow over e-mail (today only an administrator can reset), and per-IP rate limiting in front of
`POST /auth/login` (a reverse-proxy concern).
