# Phase 1B.1-D-B Security Administration API Plan

## 1. Status

DRAFT — AWAITING PROJECT OWNER REVIEW

## 2. Baseline

5592dc5e7dce37ee2402efbb782db5225bfb49a0

## 3. Confirmed Prior Scope

Phase 1B.1-D-A (Permission Evaluator Foundation) has been implemented, audited, and accepted by the Project Owner.

## 4. Proposed D-B Scope

Phase 1B.1-D-B implements the Security Administration APIs for configuring authorization data modeled in V0003.

The proposed scope includes:
1. **Permission catalog read API**: `GET /api/v2/security/permissions`.
2. **Role read/create/update/deactivate APIs**: CRUD operations for Roles.
3. **Role-permission assignment APIs**: Assigning/removing permissions from roles.
4. **Admin group read/create/update/deactivate APIs**: CRUD operations for Admin Groups.
5. **Admin group permission assignment APIs**: Assigning/removing permissions from admin groups.
6. **User role assignment APIs**: Assigning roles to users with temporal scope.
7. **User admin group assignment APIs**: Assigning admin groups to users with temporal scope.
8. **User individual permission ALLOW/DENY APIs**: Individual overrides with temporal scope.
9. **Department baseline permission APIs**: Proposed to be included or deferred based on OD-D-B-09.
10. **Effective permissions read API**: Proposed to be included or deferred based on OD-D-B-10.
11. **Authorization policy version increment**: All mutations to roles, admin groups, and assignments must increment `Authorization_Policy_State.policy_version`.
12. **Cache invalidation behavior**: Incrementing the policy version automatically invalidates the `IPermissionEvaluator` cache.
13. **Validation behavior for inactive permissions**: Assigning an inactive permission returns HTTP 422.
14. **Company-scope validation behavior**: Enforcing `company_id` rules (GLOBAL vs COMPANY) at assignment time.
15. **RowVersion/concurrency behavior**: Enforcing optimistic concurrency for updates to roles/admin groups (HTTP 409).
16. **EffectiveFrom/EffectiveTo behavior**: Enforcing temporal overlap rules.
17. **Idempotency behavior**: Pending decision (OD-D-B-06).
18. **Soft deactivate vs hard delete**: Pending decision (OD-D-B-07).

## 5. Explicit Exclusions

- No Phase E middleware enforcement.
- No Phase F semantic audit writer (API mutations will use a no-op stub for now).
- No frontend implementation.
- No X-Company-Id global enforcement middleware.
- No V0004/U0004 migration (V0003 schema is sufficient).
- No production migration.

## 6. Open Decisions

**OD-D-B-01: Should D-B production APIs be exposed now, or should D-B first implement application services only?**
Reason: Phase E middleware enforcement is not authorized yet. Exposing security admin APIs without a permission enforcement strategy may be unsafe.
_Proposal:_ Implement controllers, but either disable them via a feature flag or enforce permissions manually inside the controller (see OD-D-B-02) until Phase E.

**OD-D-B-02: How are D-B APIs authorized before Phase E?**
Options:
A. Require authenticated JWT only and rely on environment access.
B. Add explicit manual permission checks inside each controller action using `IPermissionEvaluator`.
C. Implement only services/tests now, defer controllers until Phase E.
D. Another documented approach.
_Proposal:_ Option B, manual checks using `IPermissionEvaluator` to validate the authorization logic early.

**OD-D-B-03: Which permission code controls security administration?**
Candidate: `SECURITY_ADMIN_MANAGE` (Not in V0003 seed) or `SECURITY_ASSIGNMENT_MANAGE` (In V0003 seed).
_Proposal:_ Use `SECURITY_ASSIGNMENT_MANAGE`, `SECURITY_ROLE_MANAGE`, and `SECURITY_ADMIN_GROUP_MANAGE` as seeded in V0003.

**OD-D-B-04: Should SECURITY_AUDIT_VIEW remain read-only and separate from security administration management?**
_Proposal:_ Yes, audit viewing is a separate concern from administration and should be kept read-only under `SECURITY_AUDIT_VIEW`.

**OD-D-B-05: Should Role/AdminGroup/Permission assignment mutations increment Authorization_Policy_State in the same DB transaction?**
_Proposal:_ Yes, to ensure atomic cache invalidation, the transaction that modifies authorization data must also increment the policy version.

**OD-D-B-06: Should duplicate active assignments return:**
- 200/204 idempotent success;
- 409 conflict;
- or 422 validation error?
_Proposal:_ Return 409 Conflict, as it indicates the client's view of the state is out of sync with the server.

**OD-D-B-07: Should assignment removal be:**
- deactivate/end-date only;
- status change only;
- or hard delete?
_Proposal:_ End-date only (set `effective_to` = now and `assignment_status` = 'CLOSED') to preserve historical auditability without deleting rows.

**OD-D-B-08: Should effective_to be exclusive or inclusive?**
_Proposal:_ Exclusive `[effective_from, effective_to)`. A permission is active if `effective_from <= NOW < effective_to`.

**OD-D-B-09: Should department baseline permission APIs be included in D-B, or deferred?**
_Proposal:_ Included, as it completes the authorization management feature set.

**OD-D-B-10: Should effective-permissions read API be included in D-B, and what route shape should it use?**
_Proposal:_ Included. Route: `GET /api/v2/security/users/{userId}/effective-permissions`.

**OD-D-B-11: Should API responses expose source breakdown, or only final effective permission codes?**
_Proposal:_ Only final effective permission codes for simplicity and security. Source breakdown is an advanced diagnostic feature that can be deferred.

**OD-D-B-12: Should D-B create any seed permissions for SECURITY_ADMIN_MANAGE, or is seeding/bootstrap deferred?**
_Proposal:_ Seeding/bootstrap is Phase 1B.1-F. We will use the V0003 seeded permissions for now.

**OD-D-B-13: Can D-B proceed without audit writer, or must mutation APIs wait for Phase F audit?**
_Proposal:_ Proceed without it. Mutation APIs will use an injected stub/no-op interface that will be replaced in Phase F.

**OD-D-B-14: Should management APIs accept only active permissions, returning HTTP 422 for inactive permission usage?**
_Proposal:_ Yes. Assigning an inactive permission is a business logic error and should return 422 Unprocessable Entity.

**OD-D-B-15: Should company-scoped roles/admin groups be restricted to users with active company assignment?**
_Proposal:_ Yes. A user must have an active assignment to Company X to be granted a role scoped to Company X.

## 7. Proposed API Design

Base Path: `/api/v2/security`

| Route | Method | Purpose | Required Permission | Transactional | Increments Policy Version |
|---|---|---|---|---|---|
| `/permissions` | GET | List active permission catalog | `SECURITY_PERMISSION_VIEW` | No | No |
| `/roles` | GET | List roles | `SECURITY_ROLE_VIEW` | No | No |
| `/roles` | POST | Create role | `SECURITY_ROLE_MANAGE` | Yes | Yes |
| `/roles/{id}` | PUT | Update role (name/desc/status) | `SECURITY_ROLE_MANAGE` | Yes | Yes |
| `/roles/{id}/permissions` | PUT | Replace role permissions | `SECURITY_ROLE_MANAGE` | Yes | Yes |
| `/admin-groups` | GET | List admin groups | `SECURITY_ADMIN_GROUP_VIEW` | No | No |
| `/admin-groups` | POST | Create admin group | `SECURITY_ADMIN_GROUP_MANAGE` | Yes | Yes |
| `/admin-groups/{id}` | PUT | Update admin group | `SECURITY_ADMIN_GROUP_MANAGE` | Yes | Yes |
| `/admin-groups/{id}/permissions` | PUT | Replace admin group permissions | `SECURITY_ADMIN_GROUP_MANAGE` | Yes | Yes |
| `/users/{id}/roles` | POST | Assign role to user | `SECURITY_ASSIGNMENT_MANAGE` | Yes | Yes |
| `/users/{id}/roles` | DELETE | End-date role assignment | `SECURITY_ASSIGNMENT_MANAGE` | Yes | Yes |
| `/users/{id}/admin-groups` | POST | Assign admin group | `SECURITY_ACCOUNT_MANAGE` | Yes | Yes |
| `/users/{id}/admin-groups` | DELETE | End-date admin group assignment | `SECURITY_ACCOUNT_MANAGE` | Yes | Yes |
| `/users/{id}/individual-permissions` | POST | Grant/Deny individual permission | `SECURITY_ASSIGNMENT_MANAGE` | Yes | Yes |
| `/users/{id}/individual-permissions` | DELETE | End-date individual permission | `SECURITY_ASSIGNMENT_MANAGE` | Yes | Yes |
| `/departments/{id}/permissions` | PUT | Replace department permissions | `SECURITY_ROLE_MANAGE` | Yes | Yes |
| `/users/{id}/effective-permissions` | GET | Read computed effective permissions | `SECURITY_ASSIGNMENT_MANAGE` | No | No |

## 8. Proposed Application Structure

**Interfaces:**
- `IPermissionCatalogService`
- `IRoleManagementService`
- `IAdminGroupManagementService`
- `IUserSecurityAssignmentService`
- `IDepartmentSecurityService`

**Implementations:**
- Placed in `src/backend/PTKD.Application/Security/Authorization/Services/`.
- Repositories/EF interactions handled via `IAuthorizationDbContext`.

**DTOs:**
- Located in `src/backend/PTKD.Application/Security/Authorization/DTOs/`.
- Validators using FluentValidation.

**Controllers:**
- `SecurityController` (for Roles, Admin Groups, Permissions)
- `UserSecurityController` (for User assignments)

## 9. Database Strategy

- **Schema**: Strictly use existing V0003 schema. No V0004 required.
- **Transactions**: Use EF Core `IDbContextTransaction` for mutations involving `Authorization_Policy_State` increment.
- **Concurrency**: Use `row_version` for `Roles` and `Admin_Groups`. Handle `DbUpdateConcurrencyException`.
- **Temporal Overlap**: Handled natively by DB triggers, but application logic will explicitly check and prevent overlaps using `SERIALIZABLE` isolation if needed.

## 10. Test Strategy

- **Unit Tests**:
  - Validation of request DTOs.
  - Service logic for scope validation (GLOBAL vs COMPANY).
  - Exceptions thrown for inactive permission assignments.
- **Integration Tests**:
  - Verify DB writes.
  - Verify `Authorization_Policy_State` increments atomically.
  - Verify trigger behavior for temporal overlaps.
  - Run against `PTKD_TEST_PHASE1A2`.
- **API Tests**:
  - Validate endpoints return correct status codes (200, 201, 400, 401, 403, 404, 409, 422).
  - Verify manual authorization (if OD-D-B-02 Option B is chosen).

## 11. Risks and Boundaries

- **Risk of exposing APIs before Phase E**: Mitigated by adopting manual authorization checks (OD-D-B-02 Option B).
- **Risk of mutation before audit writer**: Low, as long as stub is injected.
- **Risk of duplicate assignments**: Addressed by explicit DB overlap triggers and unique constraints.
- **Risk of modifying V0003**: Acknowledged. No V0003 structure changes will be made.

## 12. Recommended Implementation Slices

- **D-B-1**: Application Services (Interfaces, implementations, tests) for Roles and Admin Groups.
- **D-B-2**: Application Services for User Assignments and Department Permissions.
- **D-B-3**: Controllers, DTOs, API Tests, and Authorization wiring.

## 13. Final Recommendation

Do not implement D-B until Project Owner accepts this plan and resolves open decisions.
