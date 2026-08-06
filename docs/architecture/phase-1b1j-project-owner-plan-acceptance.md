# Phase 1B.1-J Project Owner Plan Acceptance

**Status**: ACCEPTED — PHASE 1B.1-J PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan commit**: `117466e1470e9a5c81d89b1de38e8ec8891dc4d7`

**Accepted baseline**: `10c7e6ff1cf138fab34c172ecf5e56722a175120`

**Accepted phase**:
Phase 1B.1-J — Login UI and MustChangePassword UI Foundation

## Accepted shape
- Frontend-only foundation phase.
- No backend changes by default.
- Backend changes are out-of-scope unless implementation discovery proves the accepted auth contract cannot be consumed.
- If backend changes appear required, implementation must stop and request Project Owner approval.

## Accepted scope
- Login page.
- MustChangePassword page.
- Auth client/API wrapper.
- In-memory access token handling.
- Refresh-on-bootstrap flow using the existing refresh endpoint and refresh cookie.
- Logout flow.
- Route guard for authenticated pages.
- Forced routing for must_change_password=true.
- Minimal authenticated shell placeholder.
- Minimal frontend tests for login, logout, route guard, and must-change-password flow.

## Accepted token/session strategy
- Access token is held in memory only.
- Do not store access token in localStorage.
- Do not store access token in sessionStorage.
- Do not store access token in persistent cookies.
- Refresh token remains backend-managed via HttpOnly Secure cookie.
- On page reload/app bootstrap, frontend calls refresh endpoint to re-establish auth state if refresh cookie exists.
- If refresh fails, frontend clears auth state and redirects to login.

## Accepted must-change-password routing
- Unauthenticated users go to `/login`.
- Authenticated users with `must_change_password=true` are allowed only:
  - `/change-password`
  - logout action
- Authenticated users with `must_change_password=true` are blocked from normal protected shell/routes.
- After successful change-password, frontend clears auth state and redirects to `/login` because Phase G requires fresh login.

## Accepted routes
- `/login`
- `/change-password`
- authenticated shell placeholder route

## Accepted CSRF/browser-cookie strategy
- Frontend follows existing backend cookie/CSRF contract.
- Do not invent a new CSRF mechanism.
- Do not modify backend CSRF behavior in Phase J.
- Refresh, logout, and change-password must use credentials/cookie-compatible browser requests according to existing backend behavior.
- If implementation cannot find a reliable CSRF token source/header/cookie convention, stop and report blocker before changing backend.

## Accepted route guard strategy
- Implement route-guard foundation only.
- Defer full permission-gated navigation menus.
- Defer Security Admin UI and permission assignment UI.

## Accepted UI library
- Use existing React/Vite/TypeScript stack.
- Use Ant Design for Login and MustChangePassword UI.

## Accepted out-of-scope
- Backend API changes unless blocker is found and approved.
- Security Admin UI.
- Permission assignment UI.
- Account Management UI.
- Audit viewer UI.
- Dynamic Approval Workflow.
- Business modules.
- AD/LDAP UI.
- Forgot password / self-service reset.
- Admin password reset UI.
- Audit export/reporting.
- Audit retention/archive/purge.
- SIEM integration.
- Production dashboards.

## Accepted decisions

**DEC-1B-J-01 — Phase shape:**
- Approved frontend-only foundation phase.
- No backend changes unless implementation discovery finds a blocker.

**DEC-1B-J-02 — Token storage:**
- Approved in-memory access token only.
- Refresh token remains HttpOnly Secure cookie.
- No localStorage, sessionStorage, or persistent-cookie access token storage.

**DEC-1B-J-03 — Must-change-password routing:**
- Approved strict forced routing.
- `must_change_password=true` users may only access `/change-password` or logout.
- After successful change-password, clear auth state and redirect to `/login` for fresh login.

**DEC-1B-J-04 — Authenticated shell:**
- Approved minimal authenticated shell placeholder.
- Full admin navigation is deferred.

**DEC-1B-J-05 — Permission-gated navigation:**
- Implement route-guard foundation only.
- Defer full permission-gated navigation menus until later Security Admin UI phase.

**DEC-1B-J-06 — CSRF/browser behavior:**
- Approved following existing backend cookie/CSRF contract only.
- No new CSRF mechanism.
- No backend CSRF change in Phase J.

**DEC-1B-J-07 — UI library and layout:**
- Approved Ant Design for Login and MustChangePassword UI.

## Implementation authorization
Phase 1B.1-J implementation may begin only after this Project Owner plan acceptance is committed.

PHASE 1B.1-J IMPLEMENTATION ACCEPTED � SEE phase-1b1j-project-owner-implementation-acceptance.md
