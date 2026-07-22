Title:
Phase 1B.1-N Project Owner Plan Acceptance

Status:
ACCEPTED — IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-N — Permission Assignment UI

Accepted plan commit:
db6938a729f7d98aed44d79f4af8f36cd7ee8ac5

Plan acceptance baseline:
db6938a729f7d98aed44d79f4af8f36cd7ee8ac5

Previous completed phase:
Phase 1B.1-M COMPLETE

Approved decisions:

DEC-1B-N-01 — Phase shape:
Accepted. Phase N will focus on frontend UI for individual user permission assignment only.

DEC-1B-N-02 — Authorization gate:
Accepted. Permission Assignment UI is gated by SECURITY_ADMIN_MANAGE GLOBAL.

DEC-1B-N-03 — Assignment target:
Accepted. Phase N supports user-level individual permission assignments only.

DEC-1B-N-04 — Scope support:
Accepted. Phase N supports GLOBAL and COMPANY assignments only. ENTITY remains deferred.

DEC-1B-N-05 — Company context:
Accepted. COMPANY-scoped assignment requires selected current company from Phase M. No silent fallback to GLOBAL.

DEC-1B-N-06 — DENY behavior:
Accepted. UI may expose DENY assignment because backend supports DENY-wins behavior.

DEC-1B-N-07 — Effective permissions display:
Accepted. UI should show effective permissions as read-only using existing effective permission APIs.

DEC-1B-N-08 — Audit:
Accepted. Permission assignment writes use existing backend assignment audit behavior. No new read audit event is required in Phase N.

DEC-1B-N-09 — Permission catalog:
Accepted. No new permission code is added.

DEC-1B-N-10 — Backend changes:
Accepted. No backend changes are expected because existing controllers are sufficient. If a gap is discovered, stop and report blocker before implementing backend changes.

DEC-1B-N-11 — Account Management integration:
Accepted. Account Detail may link to Permission Assignment UI when gated by SECURITY_ADMIN_MANAGE GLOBAL and backend authorization remains authoritative.

DEC-1B-N-12 — Deferred items:
Accepted. Role/group/department/bulk assignment remain deferred.

Accepted backend basis:
- Use existing UserIndividualPermissionsController for GET, POST, and DELETE.
- Use existing PermissionsController for permission catalog.
- Use existing EffectivePermissionsController for read-only effective permissions.
- Preserve backend authorization and DENY-wins behavior.
- Preserve existing assignment write audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

Accepted frontend scope:
- Add Permission Assignment UI page under Security/Admin area.
- Gate route/menu with SECURITY_ADMIN_MANAGE GLOBAL.
- Use existing account discovery APIs to select a user/account.
- Show permission catalog for assignment selection.
- Show read-only effective permissions when supported by existing endpoint.
- Support individual user ALLOW and DENY assignments.
- Support GLOBAL and COMPANY scopes only.
- Require current company selection for COMPANY assignment.
- Prevent silent fallback from COMPANY to GLOBAL.
- Show sanitized success/failure messages.
- Keep backend as authoritative.

Accepted out-of-scope:
- Role Permission Assignment UI.
- Admin Group Permission Assignment UI.
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Approval workflow.
- Audit Viewer UI.
- Organization structure redesign.
- Business modules.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only enforcement.

Implementation authorization:
Phase 1B.1-N implementation is authorized under the accepted scope and decisions above.

PHASE 1B.1-N IMPLEMENTATION ACCEPTED � SEE phase-1b1n-project-owner-implementation-acceptance.md
