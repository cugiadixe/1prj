# Phase 1B.1-S Project Owner Plan Acceptance

**Status:** ACCEPTED — IMPLEMENTATION AUTHORIZED WITH USER LOOKUP CONSTRAINT
**Accepted phase:** Phase 1B.1-S — Effective Permission Diagnostics UI
**Accepted Phase R final acceptance commit:** 96ee586850ad67f65252ed0732cedf7f9cf40b90
**Accepted Phase S detailed plan commit:** 6508f4f51bee7397805b639dd00c1c4c78b7a878
**Plan acceptance baseline:** 6508f4f51bee7397805b639dd00c1c4c78b7a878
**Previous completed phase:** Phase 1B.1-R COMPLETE

---

## Approved decisions

**DEC-1B-S-01 — Proceed with Effective Permission Diagnostics UI:**
Accepted. Phase S implements a dedicated read-only Effective Permission Diagnostics UI.

**DEC-1B-S-02 — Frontend-only default:**
Accepted. Phase S is frontend-only by default. If a backend/API gap is found, stop and report before changing backend code.

**DEC-1B-S-03 — Route:**
Accepted. Phase S route is: /security/effective-permissions

**DEC-1B-S-04 — Gate:**
Accepted. Phase S route, menu, and actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-S-05 — Flat effective result:**
Accepted. The current backend-authoritative effective permission result is flat PermissionCodes[] only.

**DEC-1B-S-06 — Source-level attribution:**
Accepted as deferred. Do not claim per-permission source attribution unless backend explicitly provides it in a later approved phase.

**DEC-1B-S-07 — Contextual sections:**
Accepted. Side-by-side contextual sections may be shown using existing APIs where feasible, but they must be clearly labeled as context only, not authoritative source-level attribution.

**DEC-1B-S-08 — Department baseline context:**
Accepted. Department baseline context is omitted unless safe user-to-department mapping is available through an existing GET API. Do not invent user-department mapping.

**DEC-1B-S-09 — User lookup/search:**
Accepted with constraint. Phase S must not silently introduce a dual-permission requirement. Core Phase S must work with SECURITY_ADMIN_MANAGE GLOBAL only, using direct UserId entry or a confirmed SECURITY_ADMIN_MANAGE-compatible user lookup. Do not require SECURITY_ACCOUNT_MANAGE GLOBAL for the core Phase S diagnostics path. If implementation cannot provide a usable user selection path without SECURITY_ACCOUNT_MANAGE, stop and report as a backend/API gap before implementation continues.

**DEC-1B-S-10 — Backend changes:**
Accepted. No backend changes by default. Stop and report any backend gap.

**DEC-1B-S-11 — Schema/migration/rollback:**
Accepted. No schema changes, no migrations, and no rollbacks.

**DEC-1B-S-12 — Permission catalog/code:**
Accepted. Do not add new permission codes. Do not modify PermissionCodes.cs. Do not modify permission-catalog.md.

**DEC-1B-S-13 — Authorization Matrix:**
Accepted as deferred. Authorization Matrix / Security Overview remains out of scope.

**DEC-1B-S-14 — Deferred scope:**
Accepted. ENTITY, DENY outside existing individual-permission behavior, bulk, export/download, workflow, and business modules remain deferred.

**DEC-1B-S-15 — Audit:**
Accepted. No frontend-side audit events. No audit mutation/export/retention changes.

**DEC-1B-S-16 — Backend authority:**
Accepted. Backend remains authoritative. Frontend must not replace backend authorization or permission calculation.

---

## Accepted backend/API basis

- Use existing EffectivePermissionsController.
- Use existing GET /api/v2/security/users/{userId}/effective-permissions?companyId=.
- Treat EffectivePermissionsController response as backend-authoritative final result.
- Current response includes UserId, CompanyId, PermissionCodes[].
- Current response does not include source attribution.
- Current response does not include denied permission list.
- Use existing PermissionsController catalog API for enrichment where available.
- Use existing individual permission, role assignment, admin group assignment, role permission, and admin group permission APIs only as feasible contextual sections.
- Do not include department baseline context unless safe user-to-department mapping is available through existing API.
- Do not use K0/account search as a required path if it requires SECURITY_ACCOUNT_MANAGE GLOBAL.
- Do not add backend aggregation endpoint.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

---

## Accepted frontend scope

- Add read-only Effective Permission Diagnostics UI.
- Route: /security/effective-permissions
- Gate route/menu/actions with SECURITY_ADMIN_MANAGE GLOBAL.
- Core workflow must not require SECURITY_ACCOUNT_MANAGE GLOBAL.
- Show direct UserId entry or confirmed SECURITY_ADMIN_MANAGE-compatible user lookup.
- Select or provide company context where required.
- Fetch backend-authoritative effective permissions.
- Display effective permission codes.
- Enrich codes with catalog name, description, scope, and status where available.
- Show loading, empty, success, and sanitized error states.
- Show contextual sections only where existing APIs support them safely.
- Clearly label contextual sections as non-authoritative context, not source-level proof.
- Do not show source attribution unless backend provides it.
- Do not show denied permission list unless backend provides it.
- Do not implement mutation.
- Do not implement export/download.
- Do not implement Authorization Matrix.
- Do not expose ENTITY.
- Do not add non-individual DENY behavior.
- Do not persist diagnostics state in localStorage/sessionStorage/cookies.
- Do not add JWT permission/company arrays.
- Do not add console logging.
- Keep backend authoritative.

---

## Accepted out-of-scope

- Per-permission source attribution.
- Authorization Matrix / Security Overview.
- Department baseline source context without safe user-department GET mapping.
- Account Management changes.
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Individual Permission Assignment changes.
- User Role Assignment changes.
- User Admin Group Membership changes.
- Department Baseline Permission Management changes.
- Backend aggregation endpoint.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- ENTITY scope.
- DENY outside existing individual-permission behavior.
- Bulk assignment.
- Export/download.
- Workflow approval.
- Business modules.
- Frontend-side audit events.
- Frontend-only authorization enforcement.

---

## Implementation authorization

Phase 1B.1-S Effective Permission Diagnostics UI implementation is authorized only under the accepted constraints above.

**Important implementation stop condition:**
If a usable user selection path cannot be implemented without requiring SECURITY_ACCOUNT_MANAGE GLOBAL, stop and report:
PHASE 1B.1-S BLOCKED — USER LOOKUP API GAP DISCOVERED

PHASE 1B.1-S PLAN ACCEPTED — IMPLEMENTATION AUTHORIZED WITH USER LOOKUP CONSTRAINT
