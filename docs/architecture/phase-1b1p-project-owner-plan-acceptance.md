# Phase 1B.1-P Project Owner Plan Acceptance

Status:
ACCEPTED — PHASE 1B.1-P PLAN ACCEPTED; PHASE 1B.1-P1 IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-P — Role and Admin Group Permission Management UI Discovery and Plan

Accepted plan commit:
46868a8866fe619abf8ac62b2cd5c2411d1af095

Plan acceptance baseline:
46868a8866fe619abf8ac62b2cd5c2411d1af095

Previous completed phase:
Phase 1B.1-O COMPLETE

## Approved decisions

**DEC-1B-P-01 — Phase shape:**
Accepted. Phase P remains discovery/plan for Role and Admin Group Permission Management UI. Implementation must be split into smaller phases.

**DEC-1B-P-02 — Management target:**
Accepted. Split implementation into:
- Phase 1B.1-P1 — Role Permission Management UI.
- Phase 1B.1-P2 — Admin Group Permission Management UI.
Phase 1B.1-P1 Role Permission Management UI is authorized first. Phase 1B.1-P2 remains deferred until P1 is completed and accepted.

**DEC-1B-P-03 — Authorization gate:**
Accepted. Role/Admin Group Permission Management UI is gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-P-04 — Scope support:**
Accepted. GLOBAL and COMPANY are supported. ENTITY remains deferred.

**DEC-1B-P-05 — Company context:**
Accepted. COMPANY-scoped role/admin group permission assignment requires selected current company from Phase M. No silent fallback to GLOBAL.

**DEC-1B-P-06 — DENY behavior:**
Accepted. Do not expose DENY for roles/admin groups because backend does not explicitly support role/group deny semantics. Role/admin group permission assignment is allow-style only.

**DEC-1B-P-07 — Assignment lineage:**
Accepted. Show only safe backend-supported fields. Do not invent lineage or source explanation beyond existing backend DTOs.

**DEC-1B-P-08 — Audit:**
Accepted. Use existing backend audit behavior for role/admin group writes. Do not create frontend-side audit events.

**DEC-1B-P-09 — Backend changes:**
Accepted. No backend changes are expected for Phase 1B.1-P1 because existing role endpoints are sufficient. If a gap is discovered, stop and report blocker before implementing backend changes.

**DEC-1B-P-10 — Permission catalog:**
Accepted. No new permission code is added.

**DEC-1B-P-11 — Split strategy:**
Accepted. Role Permission Management UI and Admin Group Permission Management UI are split. Implement Role Permission Management UI first.

**DEC-1B-P-12 — Deferred items:**
Accepted. Department baseline, bulk assignment, ENTITY scope, workflow, and Admin Group UI remain deferred from P1.

## Accepted backend basis
- Use existing RolesController for Phase 1B.1-P1.
- Use existing role CRUD endpoints.
- Use existing role permission assignment endpoint.
- Use existing permission catalog API.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL.
- Preserve existing backend audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

## Accepted Phase 1B.1-P1 frontend scope
- Add Role Permission Management UI under Security/Admin area.
- Gate route/menu with SECURITY_ADMIN_MANAGE GLOBAL.
- Reuse existing backend role APIs.
- Show role list and role details using existing safe DTOs.
- Support role permission assignment using existing role permission endpoint.
- Support GLOBAL and COMPANY scopes only.
- Require selected current company for COMPANY assignment.
- Prevent silent fallback from COMPANY to GLOBAL.
- Do not expose ENTITY scope.
- Do not expose DENY for roles.
- Show sanitized success/failure messages.
- Keep backend as authoritative.

## Accepted out-of-scope for Phase 1B.1-P1
- Admin Group Permission Management UI.
- User-role assignment UI unless already required by role details and explicitly supported safely.
- User-admin-group assignment UI.
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Role/group DENY.
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

## Implementation authorization
Phase 1B.1-P1 Role Permission Management UI implementation is authorized under the accepted scope and decisions above.

Phase 1B.1-P2 Admin Group Permission Management UI is not yet authorized for implementation and remains deferred until Phase 1B.1-P1 is completed and accepted.
