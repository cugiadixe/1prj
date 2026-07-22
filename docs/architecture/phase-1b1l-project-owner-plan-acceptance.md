Title:
Phase 1B.1-L Project Owner Plan Acceptance

Status:
ACCEPTED — PHASE 1B.1-L PLAN APPROVED FOR IMPLEMENTATION

Accepted phase:
Phase 1B.1-L — Current User Permissions API and Frontend Permission Awareness

Accepted plan commit:
72621f69a45bed406b40f3d4249cc5c2cdaefd0b

Accepted baseline:
aa728fe4634c7db36820e82355bf73cbfb449a3a

Accepted previous completed phase:
Phase 1B.1-K — Security Account Management UI Foundation

Accepted purpose:
- Provide backend-supported current-user permission awareness for the frontend.
- Enable advisory UI gating such as hiding Account Management navigation when the user lacks SECURITY_ACCOUNT_MANAGE GLOBAL.
- Preserve backend authorization as the only authoritative security boundary.

Accepted backend contract:
- Implement GET /api/v2/auth/me/permissions.
- Requires authenticated user.
- Returns 401 when unauthenticated.
- Does not require a separate permission code because the user reads their own effective permissions.
- Uses existing effective-permission calculation.
- Preserves DENY-wins behavior.
- Supports GLOBAL and COMPANY scopes.
- ENTITY scope remains out of scope.
- Does not expose role assignment internals, admin group assignment internals, department override internals, deny/allow lineage, raw SQL, audit payloads, raw exception details, security stamps, token material, or session material.
- Does not emit a read audit event in this phase.

Accepted response shape:
- permissions: array
  - permissionCode
  - scope
  - companyId nullable

Accepted frontend behavior:
- Fetch permissions from backend after login and refresh when mustChangePassword=false.
- Do not embed permissions in JWT.
- Store permission state in memory only.
- Clear permission state on logout.
- Clear permission state on refresh failure.
- Clear permission state on auth clear.
- Clear permission state after password change.
- Do not persist permissions in localStorage.
- Do not persist permissions in sessionStorage.
- Do not persist permissions in cookies.
- Do not encode permissions into URLs.
- Hide Account Management nav link unless SECURITY_ACCOUNT_MANAGE GLOBAL is present.
- Avoid flashing unauthorized security links while permissions are loading.
- Deep links must still call backend and safely handle 403.
- Existing mustChangePassword route guard remains authoritative.

Accepted security boundaries:
- Frontend permission gating is advisory only.
- Backend remains authoritative for every protected API.
- 403 handling remains mandatory.
- Raw backend details must not be shown.

Accepted decisions:

DEC-1B-L-01 — Phase shape:
- Approved backend current-user permissions API plus frontend permission awareness.

DEC-1B-L-02 — Endpoint shape:
- Approved GET /api/v2/auth/me/permissions.

DEC-1B-L-03 — Permission data source:
- Approved fetching permissions from backend after login/refresh.
- Do not embed permission arrays in JWT in this phase.

DEC-1B-L-04 — Frontend storage:
- Approved memory-only permission state.
- Clear permission state with auth state.

DEC-1B-L-05 — UI gating:
- Approved advisory-only UI gating.
- Backend remains authoritative.

DEC-1B-L-06 — Company scope:
- Approved scope and companyId for COMPANY-scoped permissions when available.
- ENTITY scope remains out of scope.

DEC-1B-L-07 — Audit:
- Approved no read audit event for current-user permission reads.

DEC-1B-L-08 — Permission catalog:
- Approved no new permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.

Implementation authorization:
- Phase 1B.1-L implementation may begin after this Project Owner plan acceptance is committed.
- Implementation must stop and report if schema migration, new permission code, PermissionCodes.cs change, or permission-catalog.md change becomes required.

PHASE 1B.1-L COMPANY-SCOPE BLOCKER RESOLVED — SEE phase-1b1l-company-scope-blocker-decision.md
