# Phase 1B.1-P2 Admin Group Permission Management UI Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

Baseline:
cd8e0d7bcd7b8f2d76d9f78539c2b4181d85dcbf

Previous completed phase:
Phase 1B.1-P1 COMPLETE

## 1. Purpose
This document outlines the implementation plan for Phase 1B.1-P2 (Admin Group Permission Management UI). The goal is to build a frontend interface for managing admin groups and their permissions, similar to the Role Permission Management UI (Phase 1B.1-P1).

## 2. Confirmed current state
- Phase 1B.1-P1 is complete.
- Backend `AdminGroupsController` is fully implemented and protected by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Backend supports Admin Group CRUD and permission assignment/removal.
- Scope types "GLOBAL" and "COMPANY" are explicitly modeled in `CreateAdminGroupRequest`.

## 3. Backend admin group discovery
- `AdminGroupsController` exposes standard CRUD endpoints (`/api/v2/security/admin-groups`).
- Endpoints accept `CreateAdminGroupRequest`, `UpdateAdminGroupRequest`, and `DeactivateAdminGroupRequest`.
- Responses use `AdminGroupDto`.
- Permissions are managed via POST to `{id}/permissions` (`AddAdminGroupPermissionsRequest`) and DELETE to `{id}/permissions/{code}`.
- All endpoints are gated by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Existing backend audit behavior is reused.
- Admin group APIs use DTOs:
  - AdminGroupDto
  - CreateAdminGroupRequest
  - UpdateAdminGroupRequest
  - DeactivateAdminGroupRequest
  - AddAdminGroupPermissionsRequest

## 4. Backend membership discovery
- `UserAdminGroupAssignmentsController` exists.
- User-admin-group membership is separate from admin group permission management.
- User-admin-group membership UI remains out of scope for P2.

## 5. Frontend security UI discovery
- Role Management UI exists at `/security/roles` and is gated by `SECURITY_ADMIN_MANAGE` GLOBAL.
- `AuthenticatedShell.tsx` and `App.tsx` handle gating logic.
- We can reuse frontend patterns from Role Management for Admin Groups.

## 6. Proposed phase shape
- Phase P2 will implement the Admin Group Permission Management UI only.
- It will be a frontend-only implementation following plan acceptance.

## 7. Proposed backend scope
- Use existing `AdminGroupsController` endpoints.
- Use existing permission catalog API.
- Keep backend as authoritative.
- No backend source changes, schema migrations, or new permission codes.

## 8. Proposed frontend scope
- New route at `/security/admin-groups`.
- Admin Group list and detail views.
- Admin Group CRUD (create, update, deactivate).
- Admin Group permission assignment and removal.
- Sanitized loading, empty, success, and failure states.
- Reusable components/patterns from role management.

## 9. Authorization and permission-gating strategy
- The route and navigation menu will be gated by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Backend authorization remains authoritative.

## 10. GLOBAL and COMPANY scope strategy
- Support GLOBAL and COMPANY scopes as supported by the backend DTOs.
- Do not expose ENTITY scope (deferred).

## 11. Current company context strategy
- COMPANY assignment requires the selected current company from Phase M context.
- No silent fallback from COMPANY to GLOBAL.

## 12. DENY strategy
- Admin group permission assignment does not natively support DENY in its DTOs (`PermissionCodes` is a list of strings). Do not expose DENY for admin groups.

## 13. Membership exclusion strategy
- User-admin-group membership UI remains out of scope for Phase P2.

## 14. Audit strategy
- No frontend-side audit events. Use existing backend audit behavior only.

## 15. Error handling strategy
- Use standardized error handling.
- Graceful degradation on permission denial or API failure.

## 16. Test strategy
- Add frontend tests for the new page (`AdminGroupManagementPage.test.tsx`).
- Ensure existing gating tests (`AuthenticatedShell.test.tsx`) are updated as needed.
- No new backend tests are required since backend is untouched.

## 17. Explicit out-of-scope
- User-admin-group membership UI.
- User-role assignment UI.
- Role Management changes (unless navigation tests require it).
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Admin group DENY.
- Approval workflow.
- Business modules.
- Audit mutation/export/retention.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- `PermissionCodes.cs` change.
- `permission-catalog.md` change.
- Frontend-only authorization enforcement.

## 18. Required Project Owner decisions

**DEC-1B-P2-01 — Phase shape:**
Should P2 implement Admin Group Permission Management UI only?
Recommended: Yes.

**DEC-1B-P2-02 — Authorization gate:**
Which permission gates Admin Group Permission Management UI?
Recommended: SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-P2-03 — Backend basis:**
Should P2 use existing AdminGroupsController endpoints only?
Recommended: Yes. If a backend gap is found during implementation, stop and report.

**DEC-1B-P2-04 — Scope support:**
Should GLOBAL and COMPANY be supported?
Recommended: Support GLOBAL and COMPANY only where existing backend supports them. ENTITY remains deferred.

**DEC-1B-P2-05 — Company context:**
Should COMPANY-scoped admin group permission assignment require selected current company from Phase M?
Recommended: Yes. No silent fallback to GLOBAL.

**DEC-1B-P2-06 — DENY behavior:**
Should admin group permission assignment expose DENY?
Recommended: No, unless backend explicitly supports admin group DENY semantics.

**DEC-1B-P2-07 — Membership:**
Should user-admin-group membership UI be included?
Recommended: No. Keep membership out of scope for P2.

**DEC-1B-P2-08 — Role Management:**
Should P2 modify Role Management UI?
Recommended: No. P1 remains closed unless a shared navigation/test update is strictly necessary.

**DEC-1B-P2-09 — Audit:**
Should P2 create frontend-side audit events?
Recommended: No. Use existing backend audit behavior only.

**DEC-1B-P2-10 — Permission catalog:**
Should a new permission code be added?
Recommended: No. Use existing SECURITY_ADMIN_MANAGE.

**DEC-1B-P2-11 — Backend changes:**
Should backend changes be allowed in P2 implementation?
Recommended: No by default. Existing endpoints only. Any backend gap must be reported before implementation.

**DEC-1B-P2-12 — Deferred items:**
Should department baseline, bulk, ENTITY, membership UI, workflow, and business modules remain deferred?
Recommended: Yes.

## 19. Blockers, if any
- None identified. Existing APIs perfectly match frontend requirements.

## 20. Recommended implementation slices
1. Scaffold `AdminGroupManagementPage` and routing.
2. Implement Admin Group list viewing.
3. Implement Admin Group creation and modification (including deactivation).
4. Implement Admin Group permission assignment and removal UI.
5. Apply company context logic for COMPANY scope.
6. Write unit tests.

## 21. Acceptance criteria
- UI is gated by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Backend authorization remains authoritative.
- Existing `AdminGroupsController` APIs are used where possible.
- Existing permission catalog API is reused.
- No frontend-only authorization replacement.
- GLOBAL and COMPANY support only where backend supports them safely.
- COMPANY-scoped assignment requires selected current company.
- No silent fallback to GLOBAL.
- ENTITY scope remains deferred.
- Admin group DENY is exposed only if backend explicitly supports it.
- User-admin-group membership UI remains out of scope.
- Role Management UI remains unchanged unless shared shell route/menu tests require update.
- No department baseline UI unless separately approved.
- No bulk assignment unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No `PermissionCodes.cs` change unless separately approved.
- No `permission-catalog.md` change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, audit viewer, role management, and mustChangePassword tests remain passing.
