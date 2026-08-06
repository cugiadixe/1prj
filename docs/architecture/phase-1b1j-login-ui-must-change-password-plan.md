# Phase 1B.1-J Login UI and MustChangePassword UI Foundation Plan

**Status**: PHASE 1B.1-J PLAN ACCEPTED — SEE phase-1b1j-project-owner-plan-acceptance.md

**Baseline**: `10c7e6ff1cf138fab34c172ecf5e56722a175120`

**Previous completed phase**: Phase 1B.1-I COMPLETE

## 1. Purpose
The purpose of Phase 1B.1-J is to establish the foundational frontend authentication flows. This includes the Login UI, the forced Must-Change-Password flow, session state management, refresh token handling via cookies, CSRF integration, and basic route guarding for the single-page application (SPA).

## 2. Confirmed current state
The project currently has a backend API that securely issues and manages authentication sessions using a short-lived JWT access token and a long-lived HttpOnly refresh cookie. The frontend is a React 19 SPA using Vite, TypeScript, and Ant Design, but it lacks any authentication state, login UI, or route protection.

## 3. Backend auth API discovery
The backend provides the following endpoints under `/api/v2/auth`:
- `POST /login`: Accepts `Username` and `Password`. Returns `LoginResponse` containing a short-lived `AccessToken`, `ExpiresIn`, and `MustChangePassword` flag. Sets `RefreshToken` via an HttpOnly Secure cookie and issues a CSRF token.
- `POST /refresh`: Rotates the refresh token. Requires the `RefreshToken` cookie and a valid CSRF token. Returns a new `AccessToken` and `MustChangePassword` flag.
- `POST /logout`: Revokes the session. Requires the `RefreshToken` cookie and CSRF token.
- `POST /change-password`: Accepts `CurrentPassword` and `NewPassword`. Requires authentication (Bearer token). Revokes all sessions on success and clears cookies/CSRF.

No backend changes are required. The API contract fully supports the planned frontend foundation.

## 4. Frontend discovery
- **Stack**: React 19, TypeScript, Vite, Ant Design, React Query, React Router DOM, Axios.
- **Test Tooling**: Vitest, React Testing Library. `setupTests.ts` and `SystemHealth.test.tsx` currently exist.
- **Routing**: `App.tsx` contains basic routes (`/` and `/system-health`). No route guards exist.
- **Client**: `api/axiosClient.ts` exists but lacks authentication interceptors.
- **State**: No authentication state or token storage mechanism is currently implemented.

## 5. Proposed scope
- **Login UI**: A page for users to enter credentials and authenticate.
- **Must-Change-Password UI**: A page for users to set a new password if their account requires it.
- **Auth State Management**: In-memory storage for the access token and user context.
- **Axios Interceptors**: Injection of the Bearer token into requests and automatic CSRF header inclusion.
- **Refresh Flow**: Implementation of silent refresh logic when the access token expires.
- **Logout Flow**: UI trigger and API integration for ending the session.
- **Route Guard**: A wrapper component to protect authenticated routes and redirect unauthenticated users.

### Proposed Frontend Routes
- `/login`
- `/change-password`
- authenticated shell placeholder route

## 6. Explicit out-of-scope
- Security Admin UI
- Permission assignment UI
- Account Management UI
- Audit viewer UI
- Dynamic Approval Workflow
- Business modules
- AD/LDAP UI
- Forgot password/self-service reset
- Admin password reset UI
- Audit export/reporting
- Audit retention/archive/purge

## 7. Token/session handling strategy
- Access token must be held in memory only.
- Do not store access token in localStorage.
- Do not store access token in sessionStorage.
- Do not store access token in persistent cookies.
- On page reload/app bootstrap, frontend should call refresh endpoint to re-establish auth state if the refresh cookie exists.
- If refresh fails, clear auth state and redirect to login.

## 8. Must-change-password routing strategy
- Unauthenticated users go to `/login`.
- Authenticated users with `must_change_password=true` are allowed only:
  - `/change-password`
  - logout action
- Authenticated users with `must_change_password=true` must be blocked from normal protected app shell/routes.
- After successful change-password, backend requires fresh login according to Phase G; frontend should clear auth state and redirect to `/login` unless backend contract explicitly says otherwise.

## 9. Route guard strategy
- A `ProtectedRoute` component will wrap all authenticated routes.
- If no access token exists and a refresh attempt fails, the user is redirected to `/login`.
- If `must_change_password` is true, the user is restricted to the `/change-password` route.

## 10. Error handling strategy
- Axios interceptors will catch `401 Unauthorized` responses. If a 401 occurs and a refresh attempt also fails, the auth state is cleared, and the user is redirected to `/login`.
- Form validation errors and API `ProblemDetails` responses will be mapped to user-friendly UI alerts.

## 11. CSRF/browser-cookie considerations
- Frontend must follow the existing backend cookie/CSRF contract.
- Do not invent a new CSRF mechanism.
- Do not modify backend CSRF behavior in Phase J.
- If implementation discovery cannot find a reliable CSRF token source/header/cookie convention, stop and report a blocker before changing backend.
- Refresh/logout/change-password must use credentials/cookie-compatible browser requests according to the existing backend contract.

## 12. Backend-change boundary
- Phase J is frontend-only by default.
- Backend changes are out-of-scope unless discovery proves an accepted auth contract is impossible to consume.
- If backend changes appear required, implementation must stop and request Project Owner approval.

## 13. Test strategy
- **Unit Tests**: Test the auth state reducer/context and utility functions.
- **Component Tests**: Use React Testing Library to verify rendering and form validation for the Login and MustChangePassword components.
- **Integration Tests**: Mock the Axios client to verify the route guard logic (e.g., redirect to login when unauthenticated, redirect to change-password when required).

## 14. Required Project Owner decisions

**DEC-1B-J-01 — Phase shape**
Should Phase J be frontend-only foundation, or mixed frontend/backend?
*Recommended: Frontend-only, unless discovery finds backend blockers.*

**DEC-1B-J-02 — Token storage**
Should access token be held in memory only, with refresh token remaining HttpOnly Secure cookie?
*Recommended: Yes, consistent with DEC-1B-G/DEC-1B-H security direction.*

**DEC-1B-J-03 — Must-change-password routing**
Should users with `must_change_password=true` be forced only to change-password/logout routes?
*Recommended: Yes.*

**DEC-1B-J-04 — Authenticated shell**
Should Phase J include only a minimal authenticated shell placeholder, deferring admin menus?
*Recommended: Yes.*

**DEC-1B-J-05 — Permission-gated navigation**
Should Phase J implement permission-gated navigation now, or defer until Security Admin UI phase?
*Recommended: Implement route-guard foundation only; defer full permission-gated navigation menus.*

**DEC-1B-J-06 — CSRF/browser behavior**
Confirm frontend must follow existing refresh/logout/change-password cookie/CSRF expectations.
*Recommended: Yes, no backend contract change unless blocker found.*

**DEC-1B-J-07 — UI library and layout**
Confirm Ant Design is accepted for Login and MustChangePassword UI.
*Recommended: Yes, use existing stack.*

## 15. Blockers, if any
None discovered. The backend API is fully prepared to support the frontend implementation.

## 16. Recommended implementation slices
1. **Slice 1: Auth State & API Client**: Implement React Context for auth state, Axios interceptors for token injection, and silent refresh logic.
2. **Slice 2: Login UI & Route Guards**: Build the Login page, integrate it with the auth API, and implement the `ProtectedRoute` wrapper.
3. **Slice 3: Must-Change-Password Flow**: Build the Change Password page and implement the routing enforcement for the `must_change_password` flag.

## 17. Acceptance criteria
- [ ] Users can log in using valid credentials via the UI.
- [ ] Users are redirected to `/login` if attempting to access a protected route without authentication.
- [ ] Users with `must_change_password = true` are forced to the change-password screen and cannot bypass it.
- [ ] Successful password change redirects the user to log in again.
- [ ] Access token is stored only in memory; refresh token relies on HttpOnly cookies.
- [ ] Axios automatically handles silent refresh when the access token expires.
- [ ] Users can successfully log out via the UI.
- [ ] CSRF tokens are correctly passed to the backend for refresh, logout, and change-password requests.
- [ ] Automated tests cover Login, Logout, Route Guard, and Must-Change-Password flows.
