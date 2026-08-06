# Phase 1B.1-Q1 User Role Assignment UI Plan

**Status:**
PHASE 1B.1-Q1 PLAN ACCEPTED — SEE phase-1b1q1-project-owner-plan-acceptance.md

**Baseline:**
20ad5b2fc4ff435b5bef1129e3cbebce5936476e

**Parent umbrella phase:**
Phase 1B.1-Q — User Security Assignment UI

**Previous completed phase:**
Phase 1B.1-P2 COMPLETE

## 1. Purpose
Plan the frontend-only implementation of Phase 1B.1-Q1, focusing exclusively on the User Role Assignment UI. This enables security administrators to view, assign, and deactivate roles for specific users without affecting other permission modules.

## 2. Confirmed current state
- Phase 1B.1-Q (umbrella) is accepted.
- `SECURITY_ADMIN_MANAGE` `GLOBAL` is confirmed as the assignment gate.
- Existing role management UI and account management UI are complete.
- Backend APIs for User Role Assignments are fully implemented.

## 3. Backend UserRoleAssignmentsController discovery
- **Endpoints:**
  - `GET /api/v2/security/users/{userId}/role-assignments`
  - `POST /api/v2/security/users/{userId}/role-assignments`
  - `DELETE /api/v2/security/users/{userId}/role-assignments/{id}`
- **DTOs:**
  - Assignment creation uses `CreateUserRoleAssignmentRequest(long RoleId, DateTime EffectiveFrom, DateTime? EffectiveTo)`.
  - Removal uses `DeactivateAssignmentRequest(string RowVersion)`.
  - Response is `UserRoleAssignmentDto`.
- **Scope Behavior:** Scope (`GLOBAL`/`COMPANY`) is inherited from the selected `RoleId`. The endpoint does not explicitly require `CompanyId` in the DTO, but `COMPANY` scoped roles require the target user to have an active company assignment matching the role's context.
- **Lifecycle:** `EffectiveFrom` and `EffectiveTo` are fully supported according to backend contracts.
- **Security:** Fully protected by `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- **Delete Behavior:** Deactivation is a soft-delete (terminating assignment), not a hard delete.

## 4. Role lookup discovery
- Existing `RolesController` list APIs are fully sufficient for populating role selection dropdowns.
- `RoleDto` exposes `roleId`, `roleCode`/name, `scope`, `companyId`, and `status`.

## 5. Account/user discovery
- Existing `AccountsController` APIs (`SearchAccounts`, `GetAccountDetail`) are sufficient to discover the `userId` for the URL path and to display the user's name on the assignment page.

## 6. Frontend Account Detail access-gate analysis
- `AccountDetailPage` (`/security/accounts/:accountId`) currently requires `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.
- Embedding User Role Assignment deeply into `AccountDetailPage` would silently force administrators to need both `SECURITY_ACCOUNT_MANAGE` and `SECURITY_ADMIN_MANAGE` to manage role assignments, which violates the principle of separation of duties unless explicitly desired.

## 7. Selected access-gate recommendation
- **Option 2** is selected: Create a separate `SECURITY_ADMIN_MANAGE` `GLOBAL` route/component specifically for Q1.
- This keeps the access paths clean. An optional link from `AccountDetailPage` may be provided *only* if the user has both permissions, but the core functionality will live on a dedicated assignment route.

## 8. Proposed Q1 scope
- Frontend-only User Role Assignment UI.
- Use existing `UserRoleAssignmentsController` endpoints only.
- Use existing `RolesController` lookup API.
- Use existing account/user lookup APIs only as needed.
- Gate Q1 route/actions with `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- Do not silently require `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.
- Backend remains authoritative.
- Show assigned roles for one user.
- Assign a selected role to one user.
- Remove/deactivate a user role assignment through existing backend endpoint.
- Show `EffectiveFrom` and `EffectiveTo` only according to existing backend DTO/validation.
- Support `GLOBAL` and `COMPANY` role assignment only where existing backend supports safely.
- `COMPANY` role assignment requires selected current company where relevant.
- No silent fallback from `COMPANY` to `GLOBAL`.

## 9. Proposed backend scope
- **Zero changes.** If any gap is discovered, implementation will stop and report.

## 10. Proposed frontend scope
- Create a dedicated `UserRoleAssignmentsPage` (e.g., at `/security/users/:userId/role-assignments`).
- Implement list view of active/inactive role assignments.
- Implement assignment creation form.
- Implement deactivation action.

## 11. Authorization and permission-gating strategy
- The Q1 UI and actions will be strictly gated by `SECURITY_ADMIN_MANAGE` `GLOBAL`.

## 12. GLOBAL and COMPANY scope strategy
- `GLOBAL` and `COMPANY` scope roles will be assignable.
- `ENTITY` scope remains deferred.

## 13. Current company context strategy
- When assigning `COMPANY` scoped roles, the selection will require the current company context where relevant. No silent fallback to `GLOBAL`.

## 14. Lifecycle fields strategy
- Expose `EffectiveFrom` and `EffectiveTo` as driven by the backend DTOs.

## 15. DENY strategy
- `DENY` is **not** exposed, as the role assignment purely grants access and backend does not support `DENY` on this construct.

## 16. Removal/deactivation strategy
- Do not attempt hard deletes. Use the existing `DELETE` (deactivate) endpoint semantics.

## 17. Audit strategy
- No frontend-side audit events. Rely entirely on the backend audit trails.

## 18. Error handling strategy
- Use existing API client patterns and standardized, sanitized error messages (mapping 409 Conflicts and 422 Unprocessable Entities gracefully).

## 19. Test strategy
- Add unit tests for `UserRoleAssignmentsPage` and its API hooks.
- Ensure existing routing and authentication tests pass.

## 20. Explicit out-of-scope
- User Admin Group Membership UI (Phase 1B.1-Q2).
- Role Permission Management changes.
- Admin Group Permission Management changes.
- Individual Permission Assignment changes (unless navigation consistency requires a link).
- Bulk assignment.
- Export/download.
- Department Baseline Permission UI.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-side audit events.

## 21. Required Project Owner decisions
- **DEC-1B-Q1-01 — Q1 phase shape:** Should Q1 implement User Role Assignment UI only? *(Recommended: Yes)*
- **DEC-1B-Q1-02 — Authorization gate:** Which permission gates Q1? *(Recommended: SECURITY_ADMIN_MANAGE GLOBAL)*
- **DEC-1B-Q1-03 — Access-gate resolution:** How should Q1 avoid silently requiring both SECURITY_ACCOUNT_MANAGE GLOBAL and SECURITY_ADMIN_MANAGE GLOBAL? *(Recommended: Use a separate SECURITY_ADMIN_MANAGE GLOBAL route/component for Q1. Account Detail may include an optional link only when safe, but Account Management remains SECURITY_ACCOUNT_MANAGE GLOBAL gated)*
- **DEC-1B-Q1-04 — Entry route:** Which route should Q1 use? *(Recommended: /security/users/:userId/role-assignments)*
- **DEC-1B-Q1-05 — Backend basis:** Should Q1 use existing UserRoleAssignmentsController only? *(Recommended: Yes. If a backend gap is found during implementation, stop and report)*
- **DEC-1B-Q1-06 — Role lookup:** Should Q1 use existing RolesController lookup/list APIs? *(Recommended: Yes)*
- **DEC-1B-Q1-07 — Account/user lookup:** Should Q1 use existing account/user discovery APIs only as needed? *(Recommended: Yes)*
- **DEC-1B-Q1-08 — Scope behavior:** Should Q1 support GLOBAL and COMPANY roles only? *(Recommended: Yes, only where existing backend supports them safely. ENTITY remains deferred)*
- **DEC-1B-Q1-09 — Company context:** Should COMPANY role assignment require selected current company? *(Recommended: Yes where relevant. No silent fallback to GLOBAL)*
- **DEC-1B-Q1-10 — Lifecycle fields:** Should Q1 expose EffectiveFrom and EffectiveTo? *(Recommended: Yes, only according to existing backend DTO and validation contracts)*
- **DEC-1B-Q1-11 — DENY behavior:** Should Q1 expose DENY? *(Recommended: No. Role assignment grants membership in role; DENY is not exposed unless backend explicitly supports it)*
- **DEC-1B-Q1-12 — Removal behavior:** Should Q1 expose hard delete? *(Recommended: No. Use existing deactivate/remove endpoint semantics only)*
- **DEC-1B-Q1-13 — Audit:** Should Q1 create frontend-side audit events? *(Recommended: No. Use existing backend audit behavior only)*
- **DEC-1B-Q1-14 — Backend changes:** Should backend changes be allowed in Q1 implementation? *(Recommended: No by default. Existing endpoints only. Any backend gap must be reported before implementation changes)*
- **DEC-1B-Q1-15 — Deferred items:** Should Q2 admin group membership, bulk, ENTITY, DENY, department baseline, workflow, and business modules remain deferred? *(Recommended: Yes)*

## 22. Blockers, if any
- None identified.

## 23. Recommended implementation files
- `src/frontend/src/userRoleAssignments/userRoleAssignmentsApi.ts`
- `src/frontend/src/userRoleAssignments/errorMessages.ts`
- `src/frontend/src/userRoleAssignments/UserRoleAssignmentsPage.tsx`
- `src/frontend/src/userRoleAssignments/UserRoleAssignmentsPage.test.tsx`
- Updates to `src/frontend/src/App.tsx` and potentially `src/frontend/src/components/AuthenticatedShell.tsx` (if a direct nav entry is approved).

## 24. Acceptance criteria
- Q1 is User Role Assignment UI only.
- UI is gated by `SECURITY_ADMIN_MANAGE` `GLOBAL`.
- Q1 does not silently require `SECURITY_ACCOUNT_MANAGE` `GLOBAL`.
- Account Management itself remains `SECURITY_ACCOUNT_MANAGE` `GLOBAL` gated.
- Backend authorization remains authoritative.
- Existing `UserRoleAssignmentsController` APIs are used where possible.
- Existing `RolesController` lookup APIs are reused where possible.
- Existing account/user discovery APIs are reused only as needed.
- No frontend-only authorization replacement.
- `GLOBAL` and `COMPANY` support only where backend supports safely.
- `COMPANY` role assignment requires selected current company where relevant.
- No silent fallback to `GLOBAL`.
- `ENTITY` scope remains deferred.
- `DENY` is not exposed unless backend explicitly supports it.
- `EffectiveFrom`/`EffectiveTo` follow backend DTO and validation contracts.
- Removal uses existing backend endpoint semantics only.
- No user admin group membership UI.
- No Q2 implementation.
- No role permission management changes unless separately approved.
- No admin group permission management changes unless separately approved.
- No department baseline UI unless separately approved.
- No bulk assignment unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, audit viewer, role management, admin group management, and mustChangePassword tests remain passing.
