# Phase 1B.1-M Project Owner Plan Acceptance

Status:
ACCEPTED — IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-M — Current Company Context and X-Company-Id Frontend Foundation

Accepted plan commit:
2a49dcf75766f5635c9871fa63e20e03fe593a21

Plan acceptance baseline:
2a49dcf75766f5635c9871fa63e20e03fe593a21

Previous completed phase:
Phase 1B.1-L COMPLETE

Approved decisions:

DEC-1B-M-01 — Phase shape:
Accepted. Phase M will include a minimal backend current-user companies endpoint plus frontend company context foundation.

DEC-1B-M-02 — Company source:
Accepted. Use GET /api/v2/auth/me/companies as the source of selectable companies for the authenticated user.

DEC-1B-M-03 — Current company storage:
Accepted. Current company context is memory-only in Phase M.

DEC-1B-M-04 — X-Company-Id attachment:
Accepted. Attach X-Company-Id only for company-scoped API clients/requests, not as a global axios default.

DEC-1B-M-05 — Company selector UX:
Accepted. Authenticated shell may show a company selector when selectable companies exist.

DEC-1B-M-06 — Permission refresh on company change:
Accepted. Refetch GET /api/v2/auth/me/permissions with X-Company-Id after company selection/switch.

DEC-1B-M-07 — COMPANY-scoped UI gating:
Accepted as advisory only for current-company context. Backend remains authoritative.

DEC-1B-M-08 — Persistence:
Accepted. No localStorage, sessionStorage, or cookie persistence for current company in Phase M.

DEC-1B-M-09 — Audit:
Accepted. No read/switch audit event in Phase M.

DEC-1B-M-10 — Permission catalog:
Accepted. No new production permission code, no PermissionCodes.cs change, and no permission-catalog.md change.

Accepted backend scope:
- Implement GET /api/v2/auth/me/companies.
- Endpoint requires authenticated user.
- Endpoint returns 401 when unauthenticated.
- Endpoint does not require a separate permission code.
- Endpoint returns only companies the current user may select.
- Endpoint does not expose assignment internals.
- Endpoint does not expose role/group/department internals.
- Endpoint does not emit read audit or switch audit event in Phase M.
- Endpoint does not require schema migration.
- Endpoint does not require a new production permission code.

Accepted response shape:
- companies: array
  - companyId
  - companyCode
  - companyName
  - isDefault optional or false if no existing default source exists

Accepted frontend scope:
- Add memory-only current company state/provider.
- Fetch selectable companies after login/refresh when mustChangePassword=false.
- Show safe selector UX in authenticated shell.
- Auto-select single company in memory if exactly one selectable company exists.
- Require manual selection when multiple selectable companies exist.
- Show safe empty state when no selectable companies exist.
- Attach X-Company-Id only to company-scoped API requests.
- Refetch current-user permissions with X-Company-Id after company switch.
- Clear company context on logout, refresh failure, auth clear, and password change.
- Keep Account Management GLOBAL-only.

Accepted security boundaries:
- Backend remains authoritative.
- Frontend company context and permission gating are advisory only.
- No localStorage company persistence.
- No sessionStorage company persistence.
- No cookie company persistence.
- No JWT company array.
- No JWT permission array.
- No read/switch audit event in Phase M.
- No secret console logging.
- Sanitized 400/401/403 handling must be preserved.

Accepted out-of-scope:
- Permission Assignment UI.
- Role/group management UI.
- Audit Viewer UI.
- Dynamic Approval Workflow.
- Business modules.
- Company administration CRUD.
- Organization structure redesign.
- Schema migration.
- Rollback migration.
- New production permission codes.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Persistent company context storage.
- Global axios X-Company-Id default.
- Company context as security enforcement.

Implementation authorization:
Phase 1B.1-M implementation is authorized under the accepted scope and decisions above.
