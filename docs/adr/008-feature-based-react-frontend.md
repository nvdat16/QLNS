# ADR-008 — A feature-based React frontend with a shared API client

- **Status:** Accepted 2026-09-15
- **Owner:** Frontend lead
- **Related:** [ADR-004](004-contract-first-openapi.md), quality goals Q4 and Q6

## Context

The prototypes under `uiux/` are organised by *screen*: each HTML file carries its own data and simulated interactions.
Porting that structure straight into React would produce pages that share nothing.

## Decision

Organise by feature rather than by file type: `src/features/<feature>/{api,hooks,components,pages}`. A feature may
depend only on `src/shared` (the design system) and `src/api` (the HTTP client, Problem Details mapping, token handling
and correlation); a feature never imports another feature's internals.

The access token is held **in memory**; the refresh token lives in `localStorage`. Concurrent renewals share a single
in-flight exchange, because the server treats a refresh token as single-use.

## Alternatives considered

- **Organise by type (`components/`, `pages/`, `hooks/` at the root)** — rejected: one business change would then be
  spread across four directories.
- **Keep the access token in `localStorage` for convenience** — rejected: every script on the page could read it.

## Consequences

- The permissions in `session.user.permissions` decide rendering only; the server re-checks every request
  ([ADR-003](003-backend-enforces-authorization.md)).
- Closing the tab drops the access token — by design; the session is restored from the refresh token on reload.
- The HTML prototypes under `uiux/` remain design reference, not a code source.
