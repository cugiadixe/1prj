Title:
Phase 1B.1-L Current User Permissions API and Frontend Permission Awareness Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

Baseline:
aa728fe4634c7db36820e82355bf73cbfb449a3a

Previous completed phase:
Phase 1B.1-K COMPLETE

Sections:

1. Endpoint contract
Endpoint: GET /api/v2/auth/me/permissions
Authentication:
- Requires authenticated user.
- Returns 401 when unauthenticated.
- Does not require a separate permission code because the user is reading their own effective permissions.
- Backend remains authoritative for all protected APIs.

Recommended response shape:
- permissions: array
  - permissionCode
  - scope
  - companyId nullable

Forbidden response content:
- role assignment internals
- admin group assignment internals
- department override internals
- deny/allow assignment lineage
- raw SQL
- audit payloads
- raw exception details
- security stamps
- token/session material

2. Permission calculation strategy
- Endpoint must use existing effective-permission calculation.
- DENY-wins behavior must be preserved.
- GLOBAL and COMPANY scopes are supported.
- ENTITY scope remains out of scope.
- No permission model redesign.
- No new permission code.
- No schema migration.

3. COMPANY scope strategy
Recommended:
- Return GLOBAL permissions.
- Return COMPANY-scoped permissions with companyId when available and authorized.
- Do not invent company permissions on the frontend.
- Do not infer company access from UI state.
- If implementation discovers current backend requires X-Company-Id for COMPANY evaluation, stop and document whether endpoint should return only current-company permissions or whether a broader account-context endpoint is needed.

4. mustChangePassword behavior
- Frontend must not use permissions to unlock navigation for users with mustChangePassword=true.
- AuthProvider or permission provider should skip or ignore permission loading while mustChangePassword=true.
- Existing mustChangePassword route guard remains authoritative for UI navigation.
- On successful password change, auth and permission state must be cleared and user redirected to login, consistent with Phase G/J behavior.

5. Frontend permission state lifecycle
- Permission state is memory-only.
- Clear permission state on logout.
- Clear permission state on refresh failure.
- Clear permission state on auth clear.
- Clear permission state after password change.
- Do not persist permissions in localStorage/sessionStorage.
- Do not persist permissions in cookies.
- Do not encode permissions into URLs.

6. UI gating behavior
- UI gating is advisory only.
- Backend 403 remains mandatory.
- Account Management nav link should be hidden unless SECURITY_ACCOUNT_MANAGE GLOBAL is present.
- While permission loading is pending, avoid flashing unauthorized security links.
- Deep links must still call backend and safely handle 403.
- Do not invent frontend authorization logic beyond checking backend-provided permission codes/scopes.

7. Error handling
- 401 should clear auth/permission state according to existing auth behavior.
- 403 should display sanitized unauthorized message.
- Backend raw details must not be shown.
- Permission endpoint errors must not expose internal assignment logic.

8. Test strategy additions

Backend:
- authenticated user can read own permissions
- unauthenticated request returns 401
- effective permissions preserve DENY-wins behavior
- GLOBAL permission returned correctly
- COMPANY permission includes companyId when applicable
- no assignment internals exposed
- no audit event emitted unless separately approved

Frontend:
- permissions fetched after login/refresh when mustChangePassword=false
- permissions not used while mustChangePassword=true
- Account Management nav hidden without SECURITY_ACCOUNT_MANAGE GLOBAL
- Account Management nav shown with SECURITY_ACCOUNT_MANAGE GLOBAL
- permission state clears on logout
- permission state clears on refresh failure/auth clear
- permission state clears after password change
- no localStorage/sessionStorage permission persistence
- deep-link 403 remains sanitized

9. Required Project Owner decisions

DEC-1B-L-01 — Phase shape:
Recommended: backend current-user permissions API plus frontend permission awareness.

DEC-1B-L-02 — Endpoint shape:
Recommended: GET /api/v2/auth/me/permissions.

DEC-1B-L-03 — Permission data source:
Recommended: fetch from backend after login/refresh, do not embed in JWT.

DEC-1B-L-04 — Frontend storage:
Recommended: memory-only, clear with auth state.

DEC-1B-L-05 — UI gating:
Recommended: advisory only; backend remains authoritative.

DEC-1B-L-06 — Company scope:
Recommended: include scope and companyId for COMPANY-scoped permissions when available; no ENTITY scope.

DEC-1B-L-07 — Audit:
Recommended: no read audit event.

DEC-1B-L-08 — Permission catalog:
Recommended: no new permission code and no permission-catalog.md change.
