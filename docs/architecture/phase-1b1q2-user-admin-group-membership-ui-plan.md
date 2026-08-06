# Phase 1B.1-Q2 User Admin Group Membership UI Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
PHASE 1B.1-Q2 PLAN ACCEPTED — SEE phase-1b1q2-project-owner-plan-acceptance.md

Baseline:
3121f7da6739ec080b62af8867bf8428316a0b84

Parent umbrella phase:
Phase 1B.1-Q — User Security Assignment UI

Previous completed slice:
Phase 1B.1-Q1 COMPLETE

## 1. Purpose
This phase details the planning for Phase 1B.1-Q2, which implements the frontend UI for User Admin Group Membership assignment.

## 2. Confirmed current state
- Phase 1B.1-Q1 (User Role Assignment UI) is successfully completed.
- Existing `UserAdminGroupAssignmentsController` handles list, assign, and deactivate operations.
- The UI must enforce standard authorization patterns as validated in Q1, utilizing `SECURITY_ADMIN_MANAGE GLOBAL` as the primary gate.

## 3. Backend UserAdminGroupAssignmentsController discovery
1. **List endpoint:** `GET /api/v2/security/users/{userId}/admin-group-assignments`
2. **Assign endpoint:** `POST /api/v2/security/users/{userId}/admin-group-assignments`
3. **Deactivate endpoint:** `DELETE /api/v2/security/users/{userId}/admin-group-assignments/{id}`
4. **Assign Request DTO:** `CreateUserAdminGroupAssignmentRequest(long AdminGroupId, DateTime EffectiveFrom, DateTime? EffectiveTo)`
5. **Deactivate Request DTO:** `DeactivateAssignmentRequest(string RowVersion)`
6. **Response DTO:** `UserAdminGroupAssignmentDto(long Id, long UserId, long AdminGroupId, string GroupCode, string GroupName, string AssignmentStatus, DateTime EffectiveFrom, DateTime? EffectiveTo, string RowVersion)`
7. **EffectiveFrom:** Required by backend.
8. **EffectiveTo:** Optional/Nullable.
9. **Status/Lifecycle:** Visible in `AssignmentStatus` and `EffectiveFrom`/`EffectiveTo`.
10. **Hard delete vs Soft deactivate:** Soft deactivate only (OD-D-B-07).
11. **ENTITY scope:** Admin groups do not currently support ENTITY scope.
12. **DENY support:** Not supported/exposed for admin group memberships.
13. **COMPANY assignment:** Requires selected active current company matching context where relevant.
14. **Audit behavior:** Wired automatically on backend state changes (incrementing `Authorization_Policy_State`).
15. **Protection:** Controller is protected by `SECURITY_ADMIN_MANAGE GLOBAL`.
16. **Sanitized errors:** Standard problem details are returned safely.

## 4. Admin group lookup discovery
1. **List API:** `GET /api/v2/security/admin-groups` is sufficient for selection dropdowns.
2. **AdminGroupDto:** Exposes `Id`, `GroupCode`, `Name`, `ScopeType`, `CompanyId`, `IsActive`, and `RowVersion`.
3. **Filtering:** Frontend will filter the list to only show active admin groups, matching the required scope/company.

## 5. Account/user discovery
- Existing user retrieval API from `/security/users/{userId}` can be reused to fetch basic user details.
- `AccountDetailPage` (if linked) already resolves the account->user relationship.

## 6. Q1 reuse and access-gate analysis
- `userRoleAssignmentsApi.ts` pattern for fetching, submitting, and error handling can be identically replicated for `userAdminGroupAssignmentsApi.ts`.
- Reusing `SECURITY_ADMIN_MANAGE GLOBAL` protects the route without requiring `SECURITY_ACCOUNT_MANAGE`.

## 7. Selected access-gate recommendation
Recommended route is `/security/users/:userId/admin-group-assignments`.
The component will explicitly check for `SECURITY_ADMIN_MANAGE GLOBAL`.
The `AccountDetailPage` will render a link to this route only if the user has `SECURITY_ADMIN_MANAGE GLOBAL`.

## 8. Proposed Q2 scope
- Implement a dedicated page for managing a user's Admin Group Assignments.
- Create new React components and API integrations under `src/frontend/src/userAdminGroupAssignments`.
- Allow administrators to select an admin group and assign it to a user.
- Enforce selected company context for COMPANY scoped admin groups.
- Display a list of current and past admin group assignments.
- Allow deactivation of active assignments using optimistic concurrency (`RowVersion`).

## 9. Proposed backend scope
- None. Ensure full reuse of existing `UserAdminGroupAssignmentsController` and `AdminGroupsController`.

## 10. Proposed frontend scope
- `userAdminGroupAssignmentsApi.ts`
- `errorMessages.ts` (local to domain)
- `UserAdminGroupAssignmentsPage.tsx`
- Component tests.
- Registration in `App.tsx`.
- Optional permission-aware link in `AccountDetailPage.tsx` and `UserRoleAssignmentsPage.tsx`.

## 11. Authorization and permission-gating strategy
- Component enforces `SECURITY_ADMIN_MANAGE GLOBAL`.
- Missing permission redirects or shows standard unauthorized view.

## 12. GLOBAL and COMPANY scope strategy
- When a user selects an admin group to assign, if the admin group has `ScopeType = COMPANY`, the UI will require the user to have a valid active company selected in their session.
- No silent fallback to GLOBAL is permitted.

## 13. Current company context strategy
- Utilize `useCompany()` hook to provide the active company ID when validating COMPANY assignments.

## 14. Lifecycle fields strategy
- Display `EffectiveFrom` and `EffectiveTo` in the UI list.
- Submit `EffectiveFrom` (defaulting to current date if not specified by user) and `EffectiveTo` (optional) in the POST request.

## 15. DENY strategy
- Do not expose.

## 16. Removal/deactivation strategy
- Render a "Deactivate" or "Remove" button next to active assignments.
- Prompt for confirmation.
- Submit DELETE request with the corresponding `RowVersion`.

## 17. Audit strategy
- No frontend-side audit event creation. Rely completely on backend.

## 18. Error handling strategy
- Catch standard `ProblemDetails` and map specific error codes (e.g., temporal overlaps, missing company) to user-friendly messages using `errorMessages.ts`.

## 19. Test strategy
- Unit tests for API client mapping.
- Component tests mocking `useAuth` and `useCompany`.
- Validate permission rejection (403).
- Validate successful assignment, listing, and deactivation rendering.

## 20. Explicit out-of-scope
- Backend modification.
- Q3 components (if any).
- Admin Group Permission Management UI (completed in P2).
- Role assignments (completed in Q1).
- Bulk assignment processing.
- Report downloading.

## 21. Required Project Owner decisions
- **DEC-1B-Q2-01 — Q2 phase shape:** Should Q2 implement User Admin Group Membership UI only? (Recommended: Yes)
- **DEC-1B-Q2-02 — Authorization gate:** Which permission gates Q2? (Recommended: SECURITY_ADMIN_MANAGE GLOBAL)
- **DEC-1B-Q2-03 — Access-gate resolution:** How should Q2 avoid silently requiring SECURITY_ACCOUNT_MANAGE GLOBAL? (Recommended: Standalone route)
- **DEC-1B-Q2-04 — Entry route:** Which route? (Recommended: `/security/users/:userId/admin-group-assignments`)
- **DEC-1B-Q2-05 — Backend basis:** Use existing controller? (Recommended: Yes)
- **DEC-1B-Q2-06 — Admin group lookup:** Use existing list API? (Recommended: Yes)
- **DEC-1B-Q2-07 — Account/user lookup:** Use existing APIs? (Recommended: Yes)
- **DEC-1B-Q2-08 — Scope behavior:** Support GLOBAL and COMPANY only? (Recommended: Yes)
- **DEC-1B-Q2-09 — Company context:** Require selected company for COMPANY assignments? (Recommended: Yes)
- **DEC-1B-Q2-10 — Lifecycle fields:** Expose EffectiveFrom/To? (Recommended: Yes)
- **DEC-1B-Q2-11 — DENY behavior:** Expose DENY? (Recommended: No)
- **DEC-1B-Q2-12 — Removal behavior:** Expose hard delete? (Recommended: No)
- **DEC-1B-Q2-13 — Audit:** Create frontend audit? (Recommended: No)
- **DEC-1B-Q2-14 — Backend changes:** Allowed? (Recommended: No)
- **DEC-1B-Q2-15 — Deferred items:** Keep deferred? (Recommended: Yes)

## 22. Blockers, if any
None identified.

## 23. Recommended implementation files
- `src/frontend/src/userAdminGroupAssignments/userAdminGroupAssignmentsApi.ts`
- `src/frontend/src/userAdminGroupAssignments/errorMessages.ts`
- `src/frontend/src/userAdminGroupAssignments/UserAdminGroupAssignmentsPage.tsx`
- `src/frontend/src/userAdminGroupAssignments/UserAdminGroupAssignmentsPage.test.tsx`
- `src/frontend/src/App.tsx` (Route registration)
- `src/frontend/src/pages/AccountDetailPage.tsx` (Optional link)
- `src/frontend/src/userRoleAssignments/UserRoleAssignmentsPage.tsx` (Optional link for horizontal navigation)

## 24. Acceptance criteria
- Q2 is User Admin Group Membership UI only.
- UI is gated by `SECURITY_ADMIN_MANAGE GLOBAL`.
- Q2 does not silently require `SECURITY_ACCOUNT_MANAGE GLOBAL`.
- Account Management itself remains `SECURITY_ACCOUNT_MANAGE GLOBAL` gated.
- Backend authorization remains authoritative.
- Existing `UserAdminGroupAssignmentsController` APIs are used where possible.
- Existing `AdminGroupsController` lookup APIs are reused where possible.
- Existing account/user discovery APIs are reused only as needed.
- Q1 User Role Assignment UI remains unchanged unless separately approved (e.g. adding a navigation tab).
- No frontend-only authorization replacement.
- GLOBAL and COMPANY support only where backend supports safely.
- COMPANY admin group assignment requires selected current company where relevant.
- No silent fallback to GLOBAL.
- ENTITY scope remains deferred.
- DENY is not exposed unless backend explicitly supports it.
- EffectiveFrom/EffectiveTo follow backend DTO and validation contracts.
- Removal uses existing backend endpoint semantics only.
- No role permission management changes unless separately approved.
- No admin group permission management changes unless separately approved.
- No individual permission assignment changes unless separately approved.
- No department baseline UI unless separately approved.
- No bulk assignment unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No `PermissionCodes.cs` change unless separately approved.
- No `permission-catalog.md` change unless separately approved.
- Existing auth, current permissions, current company, account management, user role assignments, permission assignment, audit viewer, role management, admin group management, and mustChangePassword tests remain passing.
