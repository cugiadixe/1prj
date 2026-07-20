# Phase 1B.1-D-B Security Administration API Plan

## 1. Status

ACCEPTED PLAN — D-B IMPLEMENTATION MAY BE AUTHORIZED SEPARATELY

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
9. **Department baseline permission APIs**: Included (OD-D-B-09 accepted).
10. **Effective permissions read API**: Included. Route: `GET /api/v2/security/users/{userId}/effective-permissions?companyId={companyId}` (OD-D-B-10 accepted).
11. **Authorization policy version increment**: All mutations to roles, admin groups, and assignments must increment `Authorization_Policy_State.policy_version` in the same DB transaction (OD-D-B-05 accepted).
12. **Cache invalidation behavior**: Incrementing the policy version automatically invalidates the `IPermissionEvaluator` cache.
13. **Validation behavior for inactive permissions**: Assigning an inactive permission returns HTTP 422 (OD-D-B-14 accepted).
14. **Company-scope validation behavior**: Enforcing `company_id` rules (GLOBAL vs COMPANY) at assignment time via `IAuthorizationDbContext.UserCompanyAssignments` (OD-D-B-15 accepted).
15. **RowVersion/concurrency behavior**: Enforcing optimistic concurrency for updates to roles/admin groups (HTTP 409).
16. **EffectiveFrom/EffectiveTo behavior**: `effective_from` inclusive, `effective_to` exclusive. Active logic: `EffectiveFrom <= now AND (EffectiveTo IS NULL OR EffectiveTo > now)` (OD-D-B-08 accepted).
17. **Idempotency behavior**: Exact duplicate active assignment returns idempotent 200/204. Conflicting overlap returns 409 Conflict (OD-D-B-06 accepted).
18. **Soft deactivate vs hard delete**: Assignment removal must use deactivate/end-date behavior to preserve history. No hard delete (OD-D-B-07 accepted).

## 5. Explicit Exclusions

- No Phase E middleware enforcement.
- No X-Company-Id middleware enforcement.
- No Phase F semantic audit writer (API mutations will use a no-op stub for now).
- No frontend implementation.
- No V0004/U0004 migration (V0003 schema is sufficient).
- No production migration.
- No seed/bootstrap for SECURITY_ADMIN_MANAGE (deferred to Phase F).
- No tag/push until explicitly authorized.

## 6. Project Owner Decisions

**OD-D-B-01:** D-B may implement production API routes, but only as dev/test implementation. No production deployment or production migration is authorized.

**OD-D-B-02:** D-B APIs require authenticated JWT plus manual per-action authorization checks using `IPermissionEvaluator`. Do not rely on environment access only. Do not implement Phase E middleware.

**OD-D-B-03:** Security administration management permission code is `SECURITY_ADMIN_MANAGE`.

**OD-D-B-04:** `SECURITY_AUDIT_VIEW` remains read-only and separate from security administration management.

**OD-D-B-05:** All Role/AdminGroup/Permission/Assignment/DepartmentBaseline/IndividualPermission mutations must increment `Authorization_Policy_State` in the same DB transaction.

**OD-D-B-06:** Exact duplicate active assignment returns idempotent success, 200 or 204. Exact duplicate means same `userId`, same `roleId`/`adminGroupId`/`permissionCode`, same `companyId`/scope, same `effectiveFrom`, same `effectiveTo`, and ACTIVE status. Conflicting overlapping assignment returns 409 Conflict.

**OD-D-B-07:** Assignment removal must not hard delete. Use deactivate/status change/end-date behavior to preserve history.

**OD-D-B-08:** `effective_from` is inclusive. `effective_to` is exclusive. Active logic: `EffectiveFrom <= now AND (EffectiveTo IS NULL OR EffectiveTo > now)`.

**OD-D-B-09:** Department baseline permission APIs are included in D-B.

**OD-D-B-10:** Effective-permissions read API is included in D-B: `GET /api/v2/security/users/{userId}/effective-permissions?companyId={companyId}`.

**OD-D-B-11:** D-B effective-permissions response returns final effective permission codes only. Source breakdown is deferred. Self-query is not authorized in D-B. Endpoint requires `SECURITY_ADMIN_MANAGE`.

**OD-D-B-12:** D-B must not create production seed/bootstrap permissions for `SECURITY_ADMIN_MANAGE`. Tests may seed `SECURITY_ADMIN_MANAGE` directly into `PTKD_TEST_PHASE1A2` only. Bootstrap/seeding remains separate and not authorized here.

**OD-D-B-13:** D-B may proceed without audit writer for dev/test implementation only. Production enablement of mutation APIs requires Phase F audit/bootstrap or separate acceptance.

**OD-D-B-14:** Management APIs must accept only active permissions. Inactive permission usage/assignment returns HTTP 422.

**OD-D-B-15:** Company-scoped roles/admin groups/assignments must be restricted to users with active assignment to that company. Implement this through `IAuthorizationDbContext` exposing `UserCompanyAssignments`. Do not inject concrete `AppDbContext` directly into Application services. Do not cast `IAuthorizationDbContext` to `AppDbContext`.

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
