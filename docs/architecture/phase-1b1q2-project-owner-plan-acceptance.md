Title:
Phase 1B.1-Q2 Project Owner Plan Acceptance

Status:
ACCEPTED — IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-Q2 — User Admin Group Membership UI

Accepted umbrella plan commit:
cbf2cddb70000b16c020877632c3f300eaa7d027

Accepted umbrella plan acceptance commit:
20ad5b2fc4ff435b5bef1129e3cbebce5936476e

Accepted Q1 final acceptance commit:
3121f7da6739ec080b62af8867bf8428316a0b84

Accepted Q2 plan commit:
d97b3a5a23fbb91c88d86b7f8e20ad1f141cecd8

Plan acceptance baseline:
d97b3a5a23fbb91c88d86b7f8e20ad1f141cecd8

Previous completed slice:
Phase 1B.1-Q1 COMPLETE

Approved decisions:

DEC-1B-Q2-01 — Q2 phase shape:
Accepted. Q2 implements User Admin Group Membership UI only.

DEC-1B-Q2-02 — Authorization gate:
Accepted. Q2 route and assignment actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.

DEC-1B-Q2-03 — Access-gate resolution:
Accepted. Q2 uses a separate SECURITY_ADMIN_MANAGE GLOBAL route/component to avoid silently requiring both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL. Account Detail may include an optional permission-aware link only if safe.

DEC-1B-Q2-04 — Entry route:
Accepted. Q2 route is:
/security/users/:userId/admin-group-assignments

DEC-1B-Q2-05 — Backend basis:
Accepted. Use existing UserAdminGroupAssignmentsController endpoints only. If a backend gap is found during implementation, stop and report before changing backend code.

DEC-1B-Q2-06 — Admin group lookup:
Accepted. Use existing AdminGroupsController lookup/list APIs.

DEC-1B-Q2-07 — Account/user lookup:
Accepted. Use existing account/user discovery APIs only as needed.

DEC-1B-Q2-08 — Scope behavior:
Accepted. Support GLOBAL and COMPANY admin groups only where existing backend supports them safely. ENTITY remains deferred.

DEC-1B-Q2-09 — Company context:
Accepted. COMPANY admin group assignment requires selected current company where relevant. No silent fallback to GLOBAL.

DEC-1B-Q2-10 — Lifecycle fields:
Accepted. Expose EffectiveFrom and EffectiveTo only according to existing backend DTO and validation contracts.

DEC-1B-Q2-11 — DENY behavior:
Accepted. Do not expose DENY. Admin group assignment grants membership in an admin group; DENY remains out of scope unless backend explicitly supports it.

DEC-1B-Q2-12 — Removal behavior:
Accepted. Do not expose hard delete. Use existing deactivate/remove endpoint semantics only.

DEC-1B-Q2-13 — Audit:
Accepted. Do not create frontend-side audit events. Use existing backend audit behavior only.

DEC-1B-Q2-14 — Backend changes:
Accepted. No backend changes are expected. Existing endpoints only. Any backend gap must be reported before implementation changes.

DEC-1B-Q2-15 — Deferred items:
Accepted. User role assignment changes, bulk assignment, ENTITY, DENY, department baseline, workflow, and business modules remain deferred unless separately approved.

Accepted backend basis:
- Use existing UserAdminGroupAssignmentsController.
- Use existing GET /api/v2/security/users/{userId}/admin-group-assignments.
- Use existing POST /api/v2/security/users/{userId}/admin-group-assignments.
- Use existing DELETE /api/v2/security/users/{userId}/admin-group-assignments/{id} semantics.
- Use existing AdminGroupsController lookup/list APIs.
- Use existing account/user discovery APIs only as needed.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL.
- Preserve existing backend audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

Accepted frontend scope:
- Add User Admin Group Membership UI as standalone route/component.
- Route:
  /security/users/:userId/admin-group-assignments
- Gate route/actions with SECURITY_ADMIN_MANAGE GLOBAL.
- Do not silently require SECURITY_ACCOUNT_MANAGE GLOBAL.
- Keep Account Management itself SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Optional Account Detail link may be added only when safe and permission-aware.
- Show admin group memberships for one user.
- Assign a selected admin group to one user.
- Remove/deactivate a user admin group assignment through existing backend endpoint semantics.
- Use existing AdminGroupsController lookup/list APIs for admin group selection.
- Use existing account/user discovery APIs only as needed.
- Reuse Q1 implementation patterns where safe.
- Support GLOBAL and COMPANY admin groups only where backend supports safely.
- COMPANY admin group assignment requires selected current company where relevant.
- No silent fallback from COMPANY to GLOBAL.
- EffectiveFrom and EffectiveTo follow backend contracts.
- Do not expose ENTITY.
- Do not expose DENY.
- Do not change Q1 User Role Assignment UI unless separately approved.
- Do not change Role Permission Management.
- Do not change Admin Group Permission Management.
- Show sanitized loading, empty, success, and failure states.
- Keep backend as authoritative.

Accepted out-of-scope:
- Q1 User Role Assignment changes unless separately approved.
- User role assignment API/client behavior changes.
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

Implementation authorization:
Phase 1B.1-Q2 User Admin Group Membership UI implementation is authorized under the accepted scope and decisions above.

PHASE 1B.1-Q2 IMPLEMENTATION ACCEPTED — SEE phase-1b1q2-project-owner-implementation-acceptance.md
