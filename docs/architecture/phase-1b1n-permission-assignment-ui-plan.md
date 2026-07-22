Title:
Phase 1B.1-N Permission Assignment UI Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
PHASE 1B.1-N PLAN ACCEPTED — SEE phase-1b1n-project-owner-plan-acceptance.md

Baseline:
4d0266bc032c5affb06b11ab4203708a08f18702

Previous completed phase:
Phase 1B.1-M COMPLETE

Sections:
1. Purpose.
2. Confirmed current state.
3. Backend permission-assignment discovery.
4. Frontend security UI discovery.
5. Proposed backend scope.
6. Proposed frontend scope.
7. Authorization and permission-gating strategy.
8. GLOBAL and COMPANY scope strategy.
9. Current company context usage.
10. User/account selection strategy.
11. Permission catalog strategy.
12. Effective permissions display strategy.
13. Assignment write strategy.
14. Audit and security boundaries.
15. Error handling strategy.
16. Test strategy.
17. Explicit out-of-scope.
18. Required Project Owner decisions.
19. Blockers, if any.
20. Recommended implementation slices.
21. Acceptance criteria.

## 1. Purpose
Plan the implementation of a UI for individual user permission assignment.

## 2. Confirmed current state
Phase L and M are complete. The backend already contains endpoints for managing individual user permissions.

## 3. Backend permission-assignment discovery
- `UserIndividualPermissionsController.cs` exists and supports GET, POST, and DELETE.
- Permissions require `SECURITY_ADMIN_MANAGE` Global.
- `PermissionsController` exists for catalog dropdowns.
- `EffectivePermissionsController` exists for read-only effective permissions.

## 4. Frontend security UI discovery
- Account management and current-user permission UI exist.
- Current company selector exists.

## 5. Proposed backend scope
No new backend endpoints required; rely on existing security admin endpoints. No schema migration, no new permission codes.

## 6. Proposed frontend scope
- Add Permission Assignment page under Security/Admin area.
- Gate page with SECURITY_ADMIN_MANAGE GLOBAL.
- Allow selecting a user/account from existing APIs.
- Show assignable permission catalog.
- Support individual user permission assignments (ALLOW/DENY, GLOBAL/COMPANY scope).
- Refetch current-user permissions after assignment changes if affected.

## 7. Authorization and permission-gating strategy
Gate UI with SECURITY_ADMIN_MANAGE GLOBAL.

## 8. GLOBAL and COMPANY scope strategy
Support GLOBAL and COMPANY scopes. ENTITY remains deferred.

## 9. Current company context usage
COMPANY-scoped assignments require selected current company from Phase M.

## 10. User/account selection strategy
Use existing approved account discovery APIs.

## 11. Permission catalog strategy
Fetch from existing `GET /api/v2/security/permissions`.

## 12. Effective permissions display strategy
Fetch from existing `GET /api/v2/security/users/{userId}/effective-permissions`.

## 13. Assignment write strategy
Use `POST /api/v2/security/users/{userId}/individual-permissions` for grant, `DELETE` for revoke.

## 14. Audit and security boundaries
Backend remains authoritative. Use existing business audit events.

## 15. Error handling strategy
Show sanitized error messages.

## 16. Test strategy
Ensure frontend tests cover gating, scope selection, company context requirement, and error sanitization.

## 17. Explicit out-of-scope
- Role/Group/Department Permission Assignment UI.
- Bulk assignment.
- Schema migration.
- New permission codes.

## 18. Required Project Owner decisions
- DEC-1B-N-01: Phase shape (Recommended: Individual user UI only)
- DEC-1B-N-02: Authorization gate (Recommended: SECURITY_ADMIN_MANAGE GLOBAL)
- DEC-1B-N-03: Assignment target (Recommended: User-level individual only)
- DEC-1B-N-04: Scope support (Recommended: GLOBAL and COMPANY only)
- DEC-1B-N-05: Company context (Recommended: Yes, require selected company)
- DEC-1B-N-06: DENY behavior (Recommended: Expose DENY assignment)
- DEC-1B-N-07: Effective permissions display (Recommended: Yes, read-only)
- DEC-1B-N-08: Audit (Recommended: Yes for writes, no new read audit)
- DEC-1B-N-09: Permission catalog (Recommended: No new codes)
- DEC-1B-N-10: Backend changes (Recommended: None needed)
- DEC-1B-N-11: Account Management integration (Recommended: Yes, link if permitted)
- DEC-1B-N-12: Deferred items (Recommended: Keep Role/Group/Dept deferred)

## 19. Blockers, if any
None discovered.

## 20. Recommended implementation slices
- Slice 1: UI routing and list existing individual permissions.
- Slice 2: Add/Remove permissions and effective permissions display.

## 21. Acceptance criteria

Acceptance criteria must include:
- Permission Assignment UI is gated by accepted security permission.
- Backend authorization remains authoritative.
- User/account selection uses existing approved account discovery APIs when possible.
- Current company context is required for COMPANY-scoped assignments.
- No silent fallback to GLOBAL for COMPANY-scoped assignment.
- DENY-wins behavior remains backend-enforced.
- Assignment UI does not expose internal assignment lineage beyond approved fields.
- No role/group/department assignment UI unless explicitly approved.
- No ENTITY scope.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.
- Existing auth, account management, current permissions, current company, and mustChangePassword tests remain passing.
- Frontend tests cover permission gate, scope selection, company context requirement, sanitized errors, and no unauthorized UI exposure.
