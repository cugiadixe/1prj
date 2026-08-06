# Phase 1B.1-P2 Project Owner Plan Acceptance

Status:
ACCEPTED — IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-P2 — Admin Group Permission Management UI

Accepted plan commit:
170a708f2c66c5f6e6ac1702d37785a232072d18

Plan acceptance baseline:
170a708f2c66c5f6e6ac1702d37785a232072d18

Previous completed phase:
Phase 1B.1-P1 COMPLETE

## Approved decisions:

**DEC-1B-P2-01 — Phase shape:**
Accepted. Phase P2 implements Admin Group Permission Management UI only.

**DEC-1B-P2-02 — Authorization gate:**
Accepted. Admin Group Permission Management UI is gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-P2-03 — Backend basis:**
Accepted. Use existing AdminGroupsController endpoints only. If a backend gap is found during implementation, stop and report before changing backend code.

**DEC-1B-P2-04 — Scope support:**
Accepted. GLOBAL and COMPANY are supported where existing backend supports them safely. ENTITY remains deferred.

**DEC-1B-P2-05 — Company context:**
Accepted. COMPANY-scoped admin group permission assignment requires selected current company from Phase M. No silent fallback to GLOBAL.

**DEC-1B-P2-06 — DENY behavior:**
Accepted. Do not expose admin group DENY unless backend explicitly supports admin group DENY semantics.

**DEC-1B-P2-07 — Membership:**
Accepted. User-admin-group membership UI is out of scope for Phase P2.

**DEC-1B-P2-08 — Role Management:**
Accepted. Do not modify Role Management UI except for strictly necessary shared shell route/menu tests.

**DEC-1B-P2-09 — Audit:**
Accepted. Do not create frontend-side audit events. Use existing backend audit behavior only.

**DEC-1B-P2-10 — Permission catalog:**
Accepted. No new permission code is added. Use existing SECURITY_ADMIN_MANAGE.

**DEC-1B-P2-11 — Backend changes:**
Accepted. No backend changes are expected. Existing endpoints only. Any backend gap must be reported before implementation changes.

**DEC-1B-P2-12 — Deferred items:**
Accepted. Department baseline, bulk assignment, ENTITY, membership UI, workflow, and business modules remain deferred.

## Accepted backend basis:
- Use existing AdminGroupsController.
- Use existing admin group CRUD endpoints.
- Use existing admin group permission assignment endpoint.
- Use existing admin group permission removal endpoint.
- Use existing permission catalog API.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL.
- Preserve existing backend audit behavior.
- Do not use UserAdminGroupAssignmentsController in P2 implementation.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

## Accepted frontend scope:
- Add Admin Group Permission Management UI under Security/Admin area.
- Route:
  `/security/admin-groups`
- Gate route/menu with SECURITY_ADMIN_MANAGE GLOBAL.
- Reuse existing AdminGroupsController APIs.
- Show admin group list and admin group details using existing safe DTOs.
- Support admin group create/update/deactivate only through existing backend endpoints.
- Support admin group permission assignment using existing backend endpoint.
- Support admin group permission removal using existing backend endpoint.
- Support GLOBAL and COMPANY scopes only.
- Require selected current company for COMPANY assignment.
- Prevent silent fallback from COMPANY to GLOBAL.
- Do not expose ENTITY scope.
- Do not expose DENY for admin groups.
- Do not implement user-admin-group membership UI.
- Do not modify Role Management behavior.
- Show sanitized loading, empty, success, and failure states.
- Keep backend as authoritative.

## Accepted out-of-scope:
- User-admin-group membership UI.
- User-role assignment UI.
- Role Management changes except strictly necessary shared shell route/menu tests.
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Admin group DENY behavior unless backend explicitly supports it.
- Approval workflow.
- Business modules.
- Audit mutation/export/retention.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only authorization enforcement.

## Implementation authorization:
Phase 1B.1-P2 Admin Group Permission Management UI implementation is authorized under the accepted scope and decisions above.

PHASE 1B.1-P2 IMPLEMENTATION ACCEPTED � SEE phase-1b1p2-project-owner-implementation-acceptance.md
