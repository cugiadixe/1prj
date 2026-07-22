# Phase 1B.1-Q Project Owner Plan Acceptance

**Status:**
ACCEPTED — UMBRELLA PLAN ACCEPTED, Q1 DETAILED PLANNING AUTHORIZED

**Accepted phase:**
Phase 1B.1-Q — User Security Assignment UI

**Accepted plan commit:**
cbf2cddb70000b16c020877632c3f300eaa7d027

**Plan acceptance baseline:**
cbf2cddb70000b16c020877632c3f300eaa7d027

**Previous completed phase:**
Phase 1B.1-P2 COMPLETE

## Approved decisions

**DEC-1B-Q-01 — Phase shape:**
Accepted. Phase Q is an umbrella plan split into Q1 User Role Assignment UI and Q2 User Admin Group Membership UI.

**DEC-1B-Q-02 — First implementation slice:**
Accepted. Q1 User Role Assignment UI is the first slice to plan in detail. Q2 remains deferred until Q1 is separately reviewed.

**DEC-1B-Q-03 — Authorization gate:**
Accepted. User security assignment actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.

**DEC-1B-Q-04 — Entry point and Account Detail access gate:**
Accepted with constraint. AccountDetailPage /security/accounts/:accountId is the preferred UX entry point, but Q1/Q2 must not silently require both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL.
Assignment actions must use SECURITY_ADMIN_MANAGE GLOBAL.
Account Management itself remains SECURITY_ACCOUNT_MANAGE GLOBAL gated.
Q1 detailed planning must choose and document one of these approaches before implementation:
1. Allow SECURITY_ADMIN_MANAGE GLOBAL users to access the assignment section without requiring SECURITY_ACCOUNT_MANAGE GLOBAL.
2. Create separate SECURITY_ADMIN_MANAGE GLOBAL assignment route/components.
3. Intentionally require both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL only if Project Owner explicitly approves that dual-permission requirement.
Default accepted constraint: do not silently require both permissions.

**DEC-1B-Q-05 — Backend basis:**
Accepted. Q1 and Q2 use existing backend assignment controllers only. If a backend gap is found during detailed planning or implementation, stop and report before changing backend code.

**DEC-1B-Q-06 — Scope support:**
Accepted. GLOBAL and COMPANY are supported only where existing backend supports them safely. ENTITY remains deferred.

**DEC-1B-Q-07 — Company context:**
Accepted. COMPANY-scoped assignment requires selected current company from Phase M where relevant. No silent fallback to GLOBAL.

**DEC-1B-Q-08 — DENY behavior:**
Accepted. Do not expose DENY unless backend explicitly supports it for the relevant assignment type.

**DEC-1B-Q-09 — Lifecycle behavior:**
Accepted. EffectiveFrom and EffectiveTo may be exposed only according to existing backend DTOs and validation contracts.

**DEC-1B-Q-10 — Audit:**
Accepted. Do not create frontend-side audit events. Use existing backend audit behavior only.

**DEC-1B-Q-11 — Permission catalog:**
Accepted. No new permission code is added. Use existing SECURITY_ADMIN_MANAGE.

**DEC-1B-Q-12 — Backend changes:**
Accepted. No backend changes are expected by default. Existing endpoints only. Any backend gap must be reported before implementation changes.

**DEC-1B-Q-13 — Deferred items:**
Accepted. Department baseline, bulk assignment, ENTITY, unsupported DENY, workflow, business modules, and permission formula redesign remain deferred.

## Accepted backend basis:
- Use existing UserRoleAssignmentsController for Q1 discovery and later implementation if Q1 is separately accepted.
- Use existing UserAdminGroupAssignmentsController for Q2 discovery and later implementation if Q2 is separately accepted.
- Use existing RolesController lookup APIs where needed.
- Use existing AdminGroupsController lookup APIs where needed.
- Use existing account/user discovery APIs subject to DEC-1B-Q-04.
- Preserve backend authorization with SECURITY_ADMIN_MANAGE GLOBAL for assignment actions.
- Preserve existing backend audit behavior.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

## Accepted frontend planning scope:
- Phase Q is not a direct source implementation authorization.
- Q1 detailed planning is authorized next.
- Q1 will cover User Role Assignment UI only.
- Q2 User Admin Group Membership UI remains deferred to a separate slice.
- AccountDetailPage is the preferred UX entry point, but access-gate handling must be resolved in Q1 plan.
- Assignment UI must not silently require both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL.
- Backend remains authoritative.
- No frontend-only authorization replacement.

## Accepted out-of-scope:
- Implementing Q1 source code in this acceptance commit.
- Implementing Q2 source code.
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Individual Permission Assignment changes unless separately approved.
- Department Baseline Permission UI.
- Bulk permission assignment.
- ENTITY scope.
- Unsupported DENY behavior.
- Approval workflow.
- Business modules.
- Organization structure redesign.
- Permission formula redesign.
- Permission catalog redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only authorization enforcement.

## Authorization after this acceptance:
- Phase 1B.1-Q umbrella plan is accepted.
- Phase 1B.1-Q1 detailed planning is authorized.
- Source implementation is not yet authorized until Q1 detailed plan is reviewed and accepted.
