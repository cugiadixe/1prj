# Phase 1B.1-Q1 Project Owner Plan Acceptance

**Status:**
ACCEPTED — IMPLEMENTATION AUTHORIZED

**Accepted phase:**
Phase 1B.1-Q1 — User Role Assignment UI

**Accepted umbrella plan commit:**
cbf2cddb70000b16c020877632c3f300eaa7d027

**Accepted umbrella plan acceptance commit:**
20ad5b2fc4ff435b5bef1129e3cbebce5936476e

**Accepted Q1 plan commit:**
69cd3ec4eebc19c5c9a8e1def9fa7314a68d7007

**Plan acceptance baseline:**
69cd3ec4eebc19c5c9a8e1def9fa7314a68d7007

**Previous completed phase:**
Phase 1B.1-P2 COMPLETE

## Approved decisions

**DEC-1B-Q1-01 — Q1 phase shape:**
Accepted. Q1 implements User Role Assignment UI only.

**DEC-1B-Q1-02 — Authorization gate:**
Accepted. Q1 route and assignment actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-Q1-03 — Access-gate resolution:**
Accepted. Q1 uses a separate SECURITY_ADMIN_MANAGE GLOBAL route/component to avoid silently requiring both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL. Account Detail may include an optional permission-aware link only if safe.

**DEC-1B-Q1-04 — Entry route:**
Accepted. Q1 route is:
`/security/users/:userId/role-assignments`

**DEC-1B-Q1-05 — Backend basis:**
Accepted. Use existing UserRoleAssignmentsController endpoints only. If a backend gap is found during implementation, stop and report before changing backend code.

**DEC-1B-Q1-06 — Role lookup:**
Accepted. Use existing RolesController lookup/list APIs.

**DEC-1B-Q1-07 — Account/user lookup:**
Accepted. Use existing account/user discovery APIs only as needed.

**DEC-1B-Q1-08 — Scope behavior:**
Accepted. Support GLOBAL and COMPANY roles only where existing backend supports them safely. ENTITY remains deferred.

**DEC-1B-Q1-09 — Company context:**
Accepted. COMPANY role assignment requires selected current company where relevant. No silent fallback to GLOBAL.

**DEC-1B-Q1-10 — Lifecycle fields:**
Accepted. Expose EffectiveFrom and EffectiveTo only according to existing backend DTO and validation contracts.

**DEC-1B-Q1-11 — DENY behavior:**
Accepted. Do not expose DENY. Role assignment grants membership in a role; DENY remains out of scope unless backend explicitly supports it.

**DEC-1B-Q1-12 — Removal behavior:**
Accepted. Do not expose hard delete. Use existing deactivate/remove endpoint semantics only.

**DEC-1B-Q1-13 — Audit:**
Accepted. Do not create frontend-side audit events. Use existing backend audit behavior only.

**DEC-1B-Q1-14 — Backend changes:**
Accepted. No backend changes are expected. Existing endpoints only. Any backend gap must be reported before implementation changes.

**DEC-1B-Q1-15 — Deferred items:**
Accepted. Q2 admin group membership, bulk assignment, ENTITY, DENY, department baseline, workflow, and business modules remain deferred.

## Accepted backend basis
- Use existing UserRoleAssignmentsController.
- Use existing GET /api/v2/security/users/{userId}/role-assignments.
- Use existing POST /api/v2/security/users/{userId}/role-assignments.
- Use existing DELETE /api/v2/security/users/{userId}/role-assignments semantics.
- Use existing RolesController lookup/list APIs.
- Use existing account/user discovery APIs only as needed.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL.
- Preserve existing backend audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

## Accepted frontend scope
- Add User Role Assignment UI as standalone route/component.
- Route:
  `/security/users/:userId/role-assignments`
- Gate route/actions with SECURITY_ADMIN_MANAGE GLOBAL.
- Do not silently require SECURITY_ACCOUNT_MANAGE GLOBAL.
- Keep Account Management itself SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Optional Account Detail link may be added only when safe and permission-aware.
- Show role assignments for one user.
- Assign a selected role to one user.
- Remove/deactivate a user role assignment through existing backend endpoint semantics.
- Use existing RolesController lookup/list APIs for role selection.
- Use existing account/user discovery APIs only as needed.
- Support GLOBAL and COMPANY roles only where backend supports safely.
- COMPANY role assignment requires selected current company where relevant.
- No silent fallback from COMPANY to GLOBAL.
- EffectiveFrom and EffectiveTo follow backend contracts.
- Do not expose ENTITY.
- Do not expose DENY.
- Do not implement user admin group membership UI.
- Do not implement Q2.
- Do not change Role Permission Management.
- Do not change Admin Group Permission Management.
- Show sanitized loading, empty, success, and failure states.
- Keep backend as authoritative.

## Accepted out-of-scope
- Q2 User Admin Group Membership UI.
- User admin group membership API client/UI.
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Individual Permission Assignment changes except optional navigation consistency if explicitly safe.
- Department Baseline Permission UI.
- Bulk permission assignment.
- Export/download.
- ENTITY scope.
- DENY behavior.
- Approval workflow.
- Business modules.
- Audit mutation/export/retention.
- Frontend-side audit events.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only authorization enforcement.

## Implementation authorization
Phase 1B.1-Q1 User Role Assignment UI implementation is authorized under the accepted scope and decisions above.
