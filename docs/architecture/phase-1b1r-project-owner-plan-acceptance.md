# Phase 1B.1-R Project Owner Plan Acceptance

**Status:** ACCEPTED — IMPLEMENTATION AUTHORIZED
PHASE 1B.1-R IMPLEMENTATION ACCEPTED — SEE phase-1b1r-project-owner-implementation-acceptance.md
**Accepted phase:** Phase 1B.1-R — Department Baseline Permission Management UI
**Accepted Phase Q completion review commit:** 87fdb4d4509b6b43cfbb2f1ed0bf4ccd7987a3a6
**Accepted Phase R gap review commit:** f9b86db45d8d720dd3d556e60853d883941c544e
**Accepted Phase R gap review acceptance commit:** ed1ae18edd8a2fb364b9b8acf3e21fd7bb208d5f
**Accepted Phase R detailed plan commit:** 75218cd0af431d57178a40e29a7356ed749c152c
**Plan acceptance baseline:** 75218cd0af431d57178a40e29a7356ed749c152c
**Previous completed phase:** Phase 1B.1-Q COMPLETE

## Approved decisions

**DEC-1B-R-01 — Phase R shape:**
Accepted. Phase R implements Department Baseline Permission Management UI only.

**DEC-1B-R-02 — Authorization gate:**
Accepted. Phase R route, menu, and actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-R-03 — Route:**
Accepted. Phase R route is:
`/security/departments/permissions`

**DEC-1B-R-04 — Backend basis:**
Accepted. Use existing DepartmentPermissionsController endpoints only. If a backend gap is found during implementation, stop and report before changing backend code.

**DEC-1B-R-05 — Permission catalog lookup:**
Accepted. Use existing PermissionsController catalog API.

**DEC-1B-R-06 — Department/company lookup:**
Accepted. Use existing department/company APIs only as needed.

**DEC-1B-R-07 — Scope behavior:**
Accepted. Support GLOBAL and COMPANY only where existing backend supports safely. ENTITY remains deferred.

**DEC-1B-R-08 — Company context:**
Accepted. COMPANY department baseline permission assignment requires selected current company where relevant. No silent fallback to GLOBAL.

**DEC-1B-R-09 — DENY behavior:**
Accepted. Do not expose DENY unless existing DepartmentPermissionsController explicitly supports DENY and Project Owner separately approves it.

**DEC-1B-R-10 — Removal behavior:**
Accepted. Do not expose hard delete beyond existing backend semantics. Use existing backend delete/remove semantics only.

**DEC-1B-R-11 — Audit:**
Accepted. Do not create frontend-side audit events. Use existing backend audit behavior only.

**DEC-1B-R-12 — Backend changes:**
Accepted. No backend changes are expected. Existing endpoints only. Any backend gap must be reported before implementation changes.

**DEC-1B-R-13 — Deferred items:**
Accepted. Effective Permission Diagnostics UI, Authorization Matrix UI, ENTITY, DENY outside accepted backend behavior, bulk, export/download, workflow, and business modules remain deferred unless separately approved.

**DEC-1B-R-14 — Permission catalog/code changes:**
Accepted. Do not add new permission codes or modify permission-catalog.md by default.

**DEC-1B-R-15 — Frontend navigation:**
Accepted. Add a Security Administration menu entry for Phase R if consistent with existing navigation patterns, gated by SECURITY_ADMIN_MANAGE GLOBAL.

## Accepted backend basis
- Use existing DepartmentPermissionsController.
- Use existing `GET /api/v2/security/departments/{departmentId}/permissions`.
- Use existing `PUT /api/v2/security/departments/{departmentId}/permissions`.
- Use existing DELETE endpoint semantics for department permission removal.
- Treat PUT as replace-all for the department baseline permission set.
- Do not treat PUT as append-only single-permission add.
- Any add/update flow must preserve existing intended permissions when using PUT.
- Use existing PermissionsController catalog API.
- Use existing organization company/department lookup APIs only as needed.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL.
- Preserve existing backend audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify `PermissionCodes.cs`.
- Do not modify `permission-catalog.md`.

## Accepted frontend scope
- Add Department Baseline Permission Management UI.
- Route: `/security/departments/permissions`
- Gate route/menu/actions with SECURITY_ADMIN_MANAGE GLOBAL.
- Do not require SECURITY_ACCOUNT_MANAGE GLOBAL.
- Do not require SECURITY_AUDIT_VIEW GLOBAL.
- Keep Account Management SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Keep Audit Viewer SECURITY_AUDIT_VIEW GLOBAL gated.
- Show departments using existing organization APIs.
- Show baseline permissions for selected department.
- Assign/update baseline permissions through existing backend replace-all PUT semantics.
- Remove baseline permission through existing backend DELETE/remove semantics.
- Use existing permission catalog for selectable permissions.
- Support GLOBAL and COMPANY only where backend supports safely.
- COMPANY baseline assignment requires selected current company where relevant.
- No silent fallback from COMPANY to GLOBAL.
- Do not expose ENTITY.
- Do not expose DENY unless separately approved.
- Show sanitized loading, empty, success, and failure states.
- Keep backend as authoritative.
- Do not create frontend-side audit events.
- Do not persist department permission state in localStorage/sessionStorage/cookies.
- Do not add JWT permission/company arrays.

## Accepted out-of-scope
- Effective Permission Diagnostics UI.
- Authorization Matrix UI.
- Account Management changes.
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Individual Permission Assignment changes.
- User Role Assignment changes.
- User Admin Group Membership changes.
- Department baseline bulk assignment.
- Export/download.
- Workflow approval.
- Business modules.
- ENTITY scope.
- DENY behavior unless separately approved.
- Audit mutation/export/retention.
- Frontend-side audit events.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- `PermissionCodes.cs` change.
- `permission-catalog.md` change.
- Frontend-only authorization enforcement.

## Implementation authorization
Phase 1B.1-R Department Baseline Permission Management UI implementation is authorized under the accepted scope and decisions above.
