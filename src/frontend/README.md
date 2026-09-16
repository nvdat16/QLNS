# QLNS React frontend

This feature-oriented React/Vite shell is a structural skeleton with one sample feature module (recruitment applications) and does not modify the standalone prototypes in `uiux/`.

- `src/app`: composition and global presentation.
- `src/api`: shared HTTP/error handling.
- `src/features/recruitment/api`: typed API contract adapter.
- `src/features/recruitment/hooks`: server interaction state.
- `src/features/recruitment/components`: feature UI.
- `src/features/recruitment/pages`: routed screen boundary.

`VITE_DEV_ACCESS_TOKEN` is a temporary local-development seam only. Replace it with the selected OIDC client's in-memory token flow before any deployed environment; never commit a token or use this variable in production.
