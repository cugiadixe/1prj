# Phase 1B.1-P Role and Admin Group Permission Management UI Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
PHASE 1B.1-P PLAN ACCEPTED — SEE phase-1b1p-project-owner-plan-acceptance.md

Baseline:
00ee4f8dbd4e5706fc640ac355f7a06b7f8e67b6

Previous completed phase:
Phase 1B.1-O COMPLETE

## 1. Purpose
To propose the frontend-only implementation plan for Role and Admin Group Permission Management UI, leveraging existing backend APIs, and establishing the exact UI capabilities supported by the backend without requiring backend changes.

## 2. Confirmed current state
- Phase 1B.1-O Audit Viewer UI is complete.
- Backend APIs for roles and admin groups exist and are fully functional.
- The schema, authorization gates, and audit integrations for these entities are in place.

## 3. Backend role permission discovery
- `RolesController` exists under `/api/v2/security/roles`.
- Supports GET (list/detail), POST (create), PUT (update), DELETE (soft deactivate).
- Supports POST `/{id}/permissions` and DELETE `/{id}/permissions/{code}`.
- Roles support `ScopeType` ("GLOBAL" or "COMPANY").
- Protected by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Audit events are wired for `ROLE_CREATE`, `ROLE_UPDATE`, `ROLE_STATUS`, `ROLE_PERMS`.

## 4. Backend admin group permission discovery
- `AdminGroupsController` exists under `/api/v2/security/admin-groups`.
- Supports GET (list/detail), POST (create), PUT (update), DELETE (soft deactivate).
- Supports POST `/{id}/permissions` and DELETE `/{id}/permissions/{code}`.
- Admin Groups support `ScopeType` ("GLOBAL" or "COMPANY").
- Protected by `SECURITY_ADMIN_MANAGE` GLOBAL.
- Audit events are wired for `ADMINGROUP_CREATE`, `ADMINGROUP_UPDATE`, `ADMINGROUP_STATUS`, `ADMINGROUP_PERMS`.

## 5. Frontend security UI discovery
- Existing layout and menu structures from Phase 1B.1-N (Permission Assignment) and 1B.1-O (Audit Viewer) are reusable.
- The current company context selector is available.
- Existing frontend API clients for auth, account, and permissions can serve as templates.

## 6. Proposed phase shape
Discovery and plan for Role/Admin Group Permission Management UI. Implementation only proceeds later after Project Owner accepts exact backend-supported scope. We recommend splitting the implementation into Role Permission UI first, followed by Admin Group Permission UI, to minimize risk.

## 7. Proposed backend scope
- Use existing endpoints only.
- Do not create backend endpoints in planning.
- No schema migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.

## 8. Proposed frontend scope
- Role Permission Management page under Security/Admin area.
- Admin Group Permission Management page under Security/Admin area.
- Gate pages with `SECURITY_ADMIN_MANAGE` GLOBAL.
- Use existing permission catalog.
- Support GLOBAL and COMPANY assignment.
- Require current company context for COMPANY assignments.
- No silent fallback from COMPANY to GLOBAL.
- Show assignment list and safe effective/preview summary only using existing backend fields.
- Show sanitized success/failure messages.
- Preserve backend as authoritative.

## 9. Authorization and permission-gating strategy
- Gated exclusively by `SECURITY_ADMIN_MANAGE` GLOBAL.
- No frontend-only authorization replacement. Backend remains authoritative.

## 10. GLOBAL and COMPANY scope strategy
- Expose both GLOBAL and COMPANY scopes since the backend supports both safely.
- ENTITY scope remains deferred.

## 11. Current company context strategy
- COMPANY-scoped assignments strictly require the selected current company from the frontend state.
- No silent fallback to GLOBAL is permitted. Missing company context fails the action.

## 12. DENY and DENY-wins strategy
- DENY is not supported by the backend for Roles or Admin Groups (only for individual assignments).
- Do not expose DENY in the Role/Group UI.

## 13. Assignment lineage and effective permissions strategy
- `EffectivePermissionsResponse` explicitly returns final codes only (no source breakdown).
- Do not invent lineage. Show only safe fields supported by backend DTOs.

## 14. Audit strategy
- Backend already emits `ROLE_CREATE`, `ROLE_PERMS`, `ADMINGROUP_CREATE`, `ADMINGROUP_PERMS`, etc.
- Rely entirely on existing backend audit behavior. No frontend audit event creation.

## 15. Error handling strategy
- Use standardized API error formats.
- Provide clear user-facing error messages for conflicts, overlaps, and unauthorized access.

## 16. Test strategy
- Add full frontend unit and integration tests for the new UI pages.
- Ensure existing auth, current permissions, current company, account management, permission assignment, audit viewer, and mustChangePassword tests remain passing.

## 17. Explicit out-of-scope
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Approval workflow.
- Business modules.
- Audit mutation/export/retention.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- Frontend-only authorization enforcement.

## 18. Required Project Owner decisions

**DEC-1B-P-01 — Phase shape:**
Should Phase P remain discovery/plan only until backend API coverage is confirmed?
Recommended: Yes.

**DEC-1B-P-02 — Management target:**
Should Phase P target Role Permission UI, Admin Group Permission UI, or both?
Recommended: Split if backend surface is incomplete. Prefer Role Permission UI first only if existing backend APIs are complete. (Backend APIs *are* complete, but splitting is still safer).

**DEC-1B-P-03 — Authorization gate:**
Which permission gates Role/Admin Group Permission Management UI?
Recommended: `SECURITY_ADMIN_MANAGE` GLOBAL.

**DEC-1B-P-04 — Scope support:**
Should GLOBAL and COMPANY be supported?
Recommended: Support only scopes that existing backend safely supports. ENTITY remains deferred.

**DEC-1B-P-05 — Company context:**
Should COMPANY-scoped role/group assignment require selected current company from Phase M?
Recommended: Yes. No silent fallback.

**DEC-1B-P-06 — DENY behavior:**
Should role/group assignment support DENY?
Recommended: Only if backend explicitly supports role/group deny semantics. Otherwise do not expose DENY for role/group.

**DEC-1B-P-07 — Assignment lineage:**
Should UI show source/lineage of permissions?
Recommended: Show only safe fields supported by backend DTOs. Do not invent lineage.

**DEC-1B-P-08 — Audit:**
Should role/group permission assignment writes emit audit events?
Recommended: Yes if writes are implemented; use existing backend audit behavior only.

**DEC-1B-P-09 — Backend changes:**
Should backend changes be allowed in Phase P implementation?
Recommended: No during planning. Later implementation may use existing APIs only unless Project Owner approves minimal backend additions.

**DEC-1B-P-10 — Permission catalog:**
Should a new permission code be added?
Recommended: No.

**DEC-1B-P-11 — Split strategy:**
Should Role Permission UI and Admin Group Permission UI be split into separate phases if both are not equally supported?
Recommended: Yes. (Or even if supported, splitting reduces commit risk).

**DEC-1B-P-12 — Deferred items:**
Should department baseline, bulk, ENTITY, and workflow remain deferred?
Recommended: Yes.

## 19. Blockers, if any
None. Existing backend APIs provide complete coverage for the required UI scope without schema or code changes.

## 20. Recommended implementation slices
1. Phase 1B.1-P1: Role Permission Management UI.
2. Phase 1B.1-P2: Admin Group Permission Management UI.

## 21. Acceptance criteria
- UI is gated by SECURITY_ADMIN_MANAGE GLOBAL.
- Backend authorization remains authoritative.
- Existing backend APIs are used where possible.
- No frontend-only authorization replacement.
- GLOBAL and COMPANY support only if backend supports them safely.
- COMPANY-scoped assignment requires selected current company.
- No silent fallback to GLOBAL.
- ENTITY scope remains deferred.
- Role/group DENY is exposed only if backend explicitly supports it.
- Assignment lineage is shown only if backend safely exposes it.
- No department baseline UI unless separately approved.
- No bulk assignment unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, audit viewer, and mustChangePassword tests remain passing.
