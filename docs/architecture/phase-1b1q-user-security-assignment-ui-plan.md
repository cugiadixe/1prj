# Phase 1B.1-Q User Security Assignment UI Plan

**Status:**
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
PHASE 1B.1-Q PLAN ACCEPTED — SEE phase-1b1q-project-owner-plan-acceptance.md

**Baseline:**
376ad58d485bb5cd3cd1165e50e29daa581d2286

**Previous completed phase:**
Phase 1B.1-P2 COMPLETE

## 1. Purpose
Plan the frontend-only implementation of User Security Assignment UI (Phase 1B.1-Q), encompassing User Role Assignment and User Admin Group Membership. This allows security administrators to grant access to users by assigning them predefined Roles and Admin Groups.

## 2. Confirmed current state
- Role and Admin Group management UIs are complete.
- Account Management UI is complete.
- Backend APIs for assigning Roles (`UserRoleAssignmentsController`) and Admin Groups (`UserAdminGroupAssignmentsController`) are fully implemented and gated by `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- No backend changes, schema changes, or permission catalog updates are required.

## 3. Backend user-role assignment discovery
- **Endpoints:**
  - `GET /api/v2/security/users/{userId}/role-assignments`
  - `POST /api/v2/security/users/{userId}/role-assignments`
  - `DELETE /api/v2/security/users/{userId}/role-assignments/{id}`
- **DTOs:**
  - `CreateUserRoleAssignmentRequest(long RoleId, DateTime EffectiveFrom, DateTime? EffectiveTo)`
  - `DeactivateAssignmentRequest(string RowVersion)`
- **Scope & Behavior:** The assignment references a `RoleId`. The scope (`GLOBAL` or `COMPANY`) is inherent to the Role itself.

## 4. Backend user-admin-group assignment discovery
- **Endpoints:**
  - `GET /api/v2/security/users/{userId}/admin-group-assignments`
  - `POST /api/v2/security/users/{userId}/admin-group-assignments`
  - `DELETE /api/v2/security/users/{userId}/admin-group-assignments/{id}`
- **DTOs:**
  - `CreateUserAdminGroupAssignmentRequest(long AdminGroupId, DateTime EffectiveFrom, DateTime? EffectiveTo)`
  - `DeactivateAssignmentRequest(string RowVersion)`
- **Scope & Behavior:** The assignment references an `AdminGroupId`. The scope is inherent to the Admin Group itself.

## 5. Role/Admin Group lookup discovery
- Existing list APIs in `RolesController` and `AdminGroupsController` are sufficient for selecting target roles and admin groups in the assignment forms.

## 6. Account/User discovery
- Existing `AccountsController` provides `SearchAccounts` and `GetAccountDetail` which are sufficient for discovering users to manage assignments for.
- Note: Accessing Account Management APIs requires `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.

## 7. Frontend security UI discovery
- The `AccountDetailPage` (located at `/security/accounts/:accountId`) already exists.
- Adding "Role Assignments" and "Admin Group Assignments" as tabs within `AccountDetailPage` provides the most cohesive user experience.

## 8. Proposed phase shape
Phase 1B.1-Q is an umbrella planning phase only. The implementation will be executed in separate slices.

## 9. Proposed implementation split
- **Phase 1B.1-Q1** — User Role Assignment UI
- **Phase 1B.1-Q2** — User Admin Group Membership UI

## 10. Proposed backend scope
- **Zero changes.** Existing backend APIs, DTOs, authorization, and audit mechanisms will be used as-is.

## 11. Proposed frontend scope
- Add "Role Assignments" and "Admin Group Memberships" tabs/sections to the existing `AccountDetailPage`.
- Implement list views for active/inactive assignments.
- Implement forms to create assignments (selecting a Role/Admin Group, Effective From, and Effective To dates).
- Implement actions to deactivate assignments.
- Filter assignable Roles/Admin Groups based on `GLOBAL` or `COMPANY` selection (with company context enforcement).

## 12. Authorization and permission-gating strategy
- The assignment tabs/actions will be strictly gated by `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- The entry point (Account Detail) currently requires `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.

**AccountDetailPage access-gate issue:**
- `AccountDetailPage` (`/security/accounts/:accountId`) is the recommended UX entry point.
- `AccountDetailPage` may currently be protected by `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.
- Q1/Q2 assignment UI must not accidentally require both `SECURITY_ACCOUNT_MANAGE` `GLOBAL` and `SECURITY_ADMIN_MANAGE` `GLOBAL` unless the Project Owner explicitly approves that dual-permission requirement.
- `SECURITY_ADMIN_MANAGE` `GLOBAL` is the recommended gate for user security assignment actions.
- Backend authorization remains authoritative.
- Direct URL access must still rely on backend authorization and sanitized 403 handling.
- Implementation must choose one of these approaches during slice planning/acceptance:
  1. Allow `SECURITY_ADMIN_MANAGE` `GLOBAL` users to access the assignment section even if they do not have `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.
  2. Create separate `SECURITY_ADMIN_MANAGE` `GLOBAL` assignment route/components.
  3. Intentionally require both `SECURITY_ACCOUNT_MANAGE` `GLOBAL` and `SECURITY_ADMIN_MANAGE` `GLOBAL`, only if Project Owner explicitly approves.

**Recommended decision:**
- Do not silently require both permissions.
- Prefer `SECURITY_ADMIN_MANAGE` `GLOBAL` for Q1/Q2 assignment actions.
- Keep Account Management itself gated by `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.

## 13. GLOBAL and COMPANY scope strategy
- When assigning `COMPANY` scoped roles/admin groups, the UI will require a company to be selected.
- `GLOBAL` scoped roles/admin groups will not require a company.

## 14. Current company context strategy
- `COMPANY`-scoped assignments will require the selected current company from the Phase M company selector context.
- There will be no silent fallback to `GLOBAL` if a company is not selected.

## 15. DENY strategy
- `DENY` behavior is not exposed for user role or admin group assignments, as the backend does not support `DENY` on these assignment types.

## 16. Assignment lifecycle strategy
- The UI will expose `EffectiveFrom` and `EffectiveTo` lifecycle fields, as they are explicitly required/supported by the backend DTOs.

## 17. Audit strategy
- Frontend will not create audit events. It will rely entirely on the existing backend audit behavior.

## 18. Error handling strategy
- Handle 409 Conflict (overlap/duplicate) and 422 Unprocessable Entity (business validation) by displaying standardized, sanitized error messages.
- No sensitive data will be logged to the console.

## 19. Test strategy
- Add unit tests for the new assignment components and hooks.
- Ensure existing Account Management, Role Management, and Admin Group Management tests continue to pass.
- Verify shared shell gating remains intact.

## 20. Explicit out-of-scope
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Permission catalog redesign.
- Department Baseline Permission UI.
- Individual Permission Assignment changes unless strictly needed for navigation consistency.
- Bulk assignment.
- `ENTITY` scope.
- `DENY` behavior (backend does not support it).
- Approval workflow.
- Business modules.
- Organization structure redesign.
- Permission formula redesign.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only authorization enforcement.

## 21. Required Project Owner decisions
- **DEC-1B-Q-01 — Phase shape:** Should Phase Q be an umbrella plan split into Q1 User Role Assignment UI and Q2 User Admin Group Membership UI? *(Recommended: Yes)*
- **DEC-1B-Q-02 — First implementation slice:** Which slice should be implemented first? *(Recommended: Q1 User Role Assignment UI first)*
- **DEC-1B-Q-03 — Authorization gate:** Which permission gates user security assignment UI? *(Recommended: SECURITY_ADMIN_MANAGE GLOBAL)*
- **DEC-1B-Q-04 — Entry point and Account Detail access gate:**
  *(Recommended: Use AccountDetailPage/User detail as the primary UX entry point, but do not silently require both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL. Project Owner must approve whether assignment UI is exposed inside Account Detail with SECURITY_ADMIN_MANAGE GLOBAL access, moved to separate SECURITY_ADMIN_MANAGE GLOBAL routes/components, or intentionally requires both permissions.)*
- **DEC-1B-Q-05 — Backend basis:** Should Q1 and Q2 use existing backend assignment controllers only? *(Recommended: Yes)*
- **DEC-1B-Q-06 — Scope support:** Should GLOBAL and COMPANY be supported? *(Recommended: Support only what existing backend safely supports. ENTITY remains deferred)*
- **DEC-1B-Q-07 — Company context:** Should COMPANY-scoped assignment require selected current company from Phase M? *(Recommended: Yes. No silent fallback to GLOBAL)*
- **DEC-1B-Q-08 — DENY behavior:** Should user role/admin group assignment expose DENY? *(Recommended: No, as backend does not support it)*
- **DEC-1B-Q-09 — Lifecycle behavior:** Should UI expose assignment start/end/lifecycle fields? *(Recommended: Yes, as backend requires/supports EffectiveFrom and EffectiveTo)*
- **DEC-1B-Q-10 — Audit:** Should Q create frontend-side audit events? *(Recommended: No)*
- **DEC-1B-Q-11 — Permission catalog:** Should a new permission code be added? *(Recommended: No. Use existing SECURITY_ADMIN_MANAGE)*
- **DEC-1B-Q-12 — Backend changes:** Should backend changes be allowed in Q implementation? *(Recommended: No by default)*
- **DEC-1B-Q-13 — Deferred items:** Should department baseline, bulk, ENTITY, DENY, workflow, and business modules remain deferred? *(Recommended: Yes)*

## 22. Blockers, if any
- The `AccountDetailPage` access-gate issue is recorded as a Project Owner decision/implementation constraint (see DEC-1B-Q-04).

## 23. Recommended implementation slices
- Proceed with **Phase 1B.1-Q1 — User Role Assignment UI**.

## 24. Acceptance criteria
- UI is gated by `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- Backend authorization remains authoritative.
- Existing assignment APIs are used where possible.
- Existing role/admin group lookup APIs are reused where possible.
- Existing account/user discovery APIs are reused where possible.
- No frontend-only authorization replacement.
- `GLOBAL` and `COMPANY` support only where backend supports them safely.
- `COMPANY`-scoped assignment requires selected current company.
- No silent fallback to `GLOBAL`.
- `ENTITY` scope remains deferred.
- `DENY` is exposed only if backend explicitly supports it (currently No).
- Role Permission Management UI remains unchanged unless separately approved.
- Admin Group Permission Management UI remains unchanged unless separately approved.
- No department baseline UI unless separately approved.
- No bulk assignment unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, audit viewer, role management, admin group management, and mustChangePassword tests remain passing.
