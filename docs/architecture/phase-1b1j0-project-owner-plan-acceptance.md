# Phase 1B.1-J0 Project Owner Plan Acceptance

**Status**: ACCEPTED — PHASE 1B.1-J0 PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan commit**: `e056bd3178c788647d9ad63a6e355c70c1fc161c`

**Accepted baseline**: `536915b49741e4881d774fcf134c4cc0d1f70a1a`

**Accepted phase**:
Phase 1B.1-J0 — Browser CSRF Contract Correction

**Related blocked phase**:
Phase 1B.1-J — Login UI and MustChangePassword UI Foundation

## Accepted blocker
- Frontend cannot reliably obtain the CSRF token required for refresh, logout, and change-password.
- Current CSRF cookie Path is `/api/v2/auth`.
- SPA runs from root path `/`.
- Browser JavaScript at `/` cannot reliably read a cookie scoped to `/api/v2/auth`.
- Backend returns `X-CSRF-Token` response header, but CORS does not currently expose that header.
- Refresh-on-bootstrap is blocked because access token is memory-only and refresh requires CSRF.

## Accepted corrective scope
- Backend-only CSRF/browser contract correction.
- No frontend implementation in J0.
- No schema migration.
- No rollback migration.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No permission model change.

## Accepted correction
- Change CSRF token cookie Path from `/api/v2/auth` to `/`.
- Keep CSRF cookie JavaScript-readable only because the backend uses a double-submit style browser CSRF pattern.
- Keep refresh token HttpOnly and Secure.
- Keep access token memory-only.
- Do not store access token in localStorage.
- Do not store access token in sessionStorage.
- Do not store access token in persistent cookies.
- Do not relax refresh token cookie security.

## Accepted CORS boundary
- If exposing `X-CSRF-Token` through CORS is required for browser/dev clients, expose only `X-CSRF-Token`.
- Do not broadly expose sensitive headers.
- Do not use wildcard origins with credentialed browser requests.
- Keep CORS restricted to explicitly approved frontend origins.
- Keep credentials-compatible behavior aligned with current backend/frontend dev setup.

## Accepted deferred option
- Do not add a dedicated CSRF bootstrap endpoint in J0 unless implementation proves Path=/ plus narrow CORS exposure cannot satisfy refresh-on-bootstrap.
- If a new endpoint appears required, stop and request Project Owner approval before implementing it.

## Accepted decisions

**DEC-1B-J0-01 — Corrective shape:**
- Approved backend-only CSRF/browser contract correction before resuming Phase J frontend.

**DEC-1B-J0-02 — CSRF cookie path:**
- Approved changing CSRF token cookie Path from `/api/v2/auth` to `/`.

**DEC-1B-J0-03 — CORS exposed header:**
- Approved exposing `X-CSRF-Token` via CORS only if required for browser/dev clients.
- Exposure must be narrow.
- No wildcard origins with credentialed requests.

**DEC-1B-J0-04 — CSRF bootstrap endpoint:**
- Defer dedicated CSRF bootstrap endpoint.
- Add only if Path=/ plus narrow CORS exposure cannot satisfy the browser contract, and only after separate approval.

**DEC-1B-J0-05 — Security boundary:**
- Refresh token remains HttpOnly Secure cookie.
- Access token remains memory-only.
- No persistent access token storage.

**DEC-1B-J0-06 — Phase J dependency:**
- Phase J frontend implementation may resume only after J0 is implemented, tested, accepted, and closed.

## Implementation authorization
Phase 1B.1-J0 implementation may begin only after this Project Owner plan acceptance is committed.

PHASE 1B.1-J0 IMPLEMENTATION ACCEPTED � SEE phase-1b1j0-project-owner-implementation-acceptance.md
