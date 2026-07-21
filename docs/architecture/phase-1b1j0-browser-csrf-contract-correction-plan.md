# Phase 1B.1-J0 Browser CSRF Contract Correction Plan

**Status**: PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Baseline**: `536915b49741e4881d774fcf134c4cc0d1f70a1a`

**Related blocked phase**: Phase 1B.1-J — Login UI and MustChangePassword UI Foundation

## 1. Purpose
The purpose of Phase 1B.1-J0 is to correct a backend CSRF contract blocker discovered during Phase J implementation discovery. The current backend CSRF configuration prevents the frontend Single-Page Application (SPA) from reading the CSRF token, making it impossible to perform authenticated actions like refresh, logout, and change-password safely.

## 2. Blocker summary
The backend `CsrfTokenService` generates a CSRF token in an `X-CSRF-TOKEN` cookie restricted to the `/api/v2/auth` path. Because the SPA runs at the root path `/`, browser security prevents `document.cookie` from reading this cookie. Additionally, while the backend sends the token in an `X-CSRF-Token` response header, it is not exposed to the client via CORS (`Access-Control-Expose-Headers`). Consequently, the frontend cannot obtain the CSRF token to echo it back in the `X-CSRF-Token` request header, completely blocking the implementation of state-changing endpoints in Phase J.

## 3. Confirmed evidence
1. **CsrfTokenService cookie name**: `X-CSRF-TOKEN`.
2. **CsrfTokenService response header name**: `X-CSRF-Token`.
3. **Current CSRF cookie Path**: `/api/v2/auth` (hardcoded in `CsrfTokenService.cs`).
4. **Whether CSRF cookie is HttpOnly or JS-readable**: It is non-HttpOnly (`HttpOnly = false`), but unreadable due to path mismatch.
5. **Whether CORS exposes X-CSRF-Token**: No. `Program.cs` CORS policy uses `.AllowAnyHeader()` but lacks `.WithExposedHeaders("X-CSRF-Token")`.
6. **Which auth endpoints require CSRF**: `/refresh` and `/logout` require CSRF validation (and Phase J implementation extends this logic to session management).
7. **Why frontend at / cannot read a cookie scoped to /api/v2/auth**: `document.cookie` only exposes cookies that match the document's current path (i.e., `/`). It inherently filters out cookies restricted to subpaths like `/api/v2/auth`.
8. **Why refresh-on-bootstrap cannot work after page reload**: The frontend relies on the in-memory access token. On a full reload, the access token is lost. To restore the session, the frontend must call `/refresh`. The `/refresh` endpoint requires the CSRF token. Since the CSRF token is unreadable from cookies or headers, the request fails with 403 Forbidden, forcing the user to log in again.
9. **Whether any frontend-only workaround exists**: No reliable browser-based workaround exists. The SPA cannot intercept the backend's Set-Cookie headers, nor read the cookie path natively.
10. **Whether changing backend contract is unavoidable**: Yes. The backend must provide a browser-consumable CSRF token.

## 4. Current backend CSRF behavior
- Login issues an access token, an HttpOnly refresh token cookie, and a non-HttpOnly CSRF cookie (`X-CSRF-TOKEN`). It also sends an `X-CSRF-Token` header.
- The CSRF cookie is scoped to `/api/v2/auth`.
- Validation checks `X-CSRF-Token` request header against `X-CSRF-TOKEN` cookie using double-submit cookie pattern.

## 5. Why frontend-only implementation is blocked
Without backend modifications, the frontend is physically unable to extract the CSRF token from the browser due to path and CORS restrictions. Implementing Phase J frontend routes without a readable CSRF token would result in a broken application where `/refresh`, `/logout`, and `/change-password` endpoints fail immediately.

## 6. Correction options

### Option A — Root-path readable CSRF cookie
- Change CSRF token cookie `Path` from `/api/v2/auth` to `/`.
- Keep CSRF cookie non-HttpOnly if current double-submit pattern requires JavaScript to read it.
- Keep Secure and SameSite settings consistent with current auth cookie policy.
- Frontend can read CSRF cookie from `document.cookie` or axios XSRF convention.
- Refresh/logout/change-password can send `X-CSRF-Token` header.

### Option B — Expose X-CSRF-Token response header via CORS
- Add `.WithExposedHeaders("X-CSRF-Token")` in `Program.cs`.
- Frontend can read token from login/refresh response headers in cross-origin dev.
- But this alone may not solve refresh-on-bootstrap after page reload unless a token can be obtained before refresh (which is impossible on a fresh page load without a readable cookie).
- Therefore, likely insufficient alone.

### Option C — Dedicated CSRF bootstrap endpoint
- Add `GET /api/v2/auth/csrf` or equivalent.
- Endpoint issues CSRF cookie/header before refresh.
- More explicit but adds new endpoint and tests.
- May be more work than needed.

## 7. Recommended option
**Option A is required**: change CSRF token cookie Path from `/api/v2/auth` to `/`.
**Option B is accepted only as needed** for browser/dev cross-origin clients: expose `X-CSRF-Token` via CORS.
**Option C dedicated CSRF bootstrap endpoint is deferred** unless A+B cannot satisfy refresh-on-bootstrap.
**No schema migration is required.**

**Rationale**:
- Maintains HttpOnly refresh token.
- Keeps access token memory-only.
- Enables refresh-on-bootstrap via the browser's automatic sending of cookies and the SPA's ability to read the CSRF cookie on load.
- Minimal backend change (just changing `CookiePath`).
- Does not require schema/migration.
- Does not weaken auth endpoint permissions.
- Keeps CSRF double-submit/browser pattern consumable by SPA as intended.

## 8. Security boundary

### CORS safety
- If exposing `X-CSRF-Token` through CORS, expose only the required CSRF response header.
- Do not broadly expose sensitive headers.
- Do not use wildcard origins with credentialed browser requests.
- Keep CORS restricted to explicitly approved frontend origins.
- Keep credentials-compatible behavior aligned with the existing backend/frontend dev setup.

### Cookie safety
- CSRF cookie may be JS-readable only because the backend uses a double-submit style browser CSRF pattern.
- Refresh token cookie must remain HttpOnly and Secure.
- Access token must remain memory-only.
- Do not store access token in localStorage, sessionStorage, or persistent cookies.
- Changing CSRF cookie Path to `/` must not change refresh token cookie path/security behavior unless separately approved.

## 9. Explicit out-of-scope
- Login UI implementation.
- MustChangePassword UI implementation.
- Security Admin UI.
- Permission assignment UI.
- Account Management UI.
- Audit viewer UI.
- Dynamic Approval Workflow.
- Business modules.
- Schema migration.
- Permission model changes.
- Relaxing refresh token HttpOnly.
- Persistent access token storage.
- Broad CORS wildcard configuration.

## 10. Test strategy
- Update existing `CsrfTokenService` unit tests if paths are asserted.
- Add or update integration tests in `AuthControllerTests` to verify `Path = "/"` is correctly set for the `X-CSRF-TOKEN` cookie.
- Verify existing auth backend tests remain green.

## 11. Required Project Owner decisions

**DEC-1B-J0-01 — Corrective shape:**
Should Phase J0 be backend-only CSRF/browser contract correction before resuming Phase J frontend?
*Recommended: Yes.*

**DEC-1B-J0-02 — CSRF cookie path:**
Should CSRF token cookie Path be changed from `/api/v2/auth` to `/`?
*Recommended: Yes.*

**DEC-1B-J0-03 — CORS exposed header:**
Should backend expose `X-CSRF-Token` via CORS for browser clients?
*Recommended: Yes, if frontend/dev origin is cross-origin.*

**DEC-1B-J0-04 — CSRF bootstrap endpoint:**
Should a dedicated CSRF bootstrap endpoint be added?
*Recommended: Defer unless Option A+B cannot satisfy refresh-on-bootstrap.*

**DEC-1B-J0-05 — Security boundary:**
Confirm refresh token remains HttpOnly Secure cookie and access token remains memory-only.
*Recommended: Yes.*

**DEC-1B-J0-06 — Phase J dependency:**
Should Phase J frontend implementation resume only after J0 correction is implemented, tested, accepted, and closed?
*Recommended: Yes.*

## 12. Phase J dependency
- Phase J frontend implementation remains blocked until J0 is implemented, tested, accepted, and closed.
- Do not resume Login UI implementation before J0 closure.

## 13. Decisions vs blockers
- No blocker to planning or to the proposed corrective path.
- Phase J implementation is blocked until J0 correction is approved and completed.
- J0 implementation is blocked until Project Owner accepts DEC-1B-J0-01 through DEC-1B-J0-06.

## 14. Recommended implementation slices
1. Modify `CsrfTokenService.cs` to set `CookiePath = "/"`.
2. Update `Program.cs` CORS configuration to expose the `X-CSRF-Token` header securely (no wildcard origins with credentials).
3. Update and run backend integration and unit tests to verify the new cookie path and header exposure.

## 15. Acceptance criteria
- [ ] Frontend can reliably obtain CSRF token from browser-consumable source.
- [ ] Refresh-on-bootstrap can be implemented without persistent access token storage.
- [ ] Refresh/logout/change-password can send required CSRF header.
- [ ] Refresh token remains HttpOnly Secure cookie.
- [ ] Access token remains memory-only.
- [ ] Existing backend auth tests remain green.
- [ ] New/updated API tests cover browser-consumable CSRF behavior.
- [ ] No migration/rollback.
- [ ] No permission catalog change.
