# Phase 1B.1-D Permission Evaluation — Implementation Plan

## Document Status

PROPOSED — PROJECT OWNER DECISIONS RECORDED — AWAITING PROJECT OWNER PLAN ACCEPTANCE

Phase 1B.1-D implementation:
NOT AUTHORIZED BY THIS PLAN

Phase 1B.1-E through I:
NOT AUTHORIZED

Production migration:
NOT AUTHORIZED

---

## 1. Purpose

This document defines the technical implementation plan for Phase 1B.1-D: Permission, Role, Department, and Admin Group evaluation.

Phase 1B.1-D implements the server-side permission evaluation engine that translates a user's assigned roles, department baseline permissions, admin group memberships, and individual grants/denies into an effective permission set for a given company scope. It does not implement company-scope header enforcement (that is Phase 1B.1-E) and does not implement frontend (Phase 1B.1-G/H).

Authentication (C-A/C-B/C-C) is complete and accepted. Phase 1B.1-D builds on the V0003 schema and the existing authentication infrastructure to provide the first layer of authorization that controllers can call.

---

## 2. Current Accepted Baseline

| Field | Value |
|---|---|
| Phase 1B.1-A (DB foundation) | ACCEPTED BY PROJECT OWNER |
| Phase 1B.1-B (Auth account / password lifecycle) | ACCEPTED BY PROJECT OWNER |
| Phase 1B.1-C-A (Token lifecycle) | ACCEPTED BY PROJECT OWNER |
| Phase 1B.1-C-B (Auth API / cookie / CSRF) | ACCEPTED BY PROJECT OWNER |
| Phase 1B.1-C-C (Protected request validation) | ACCEPTED BY PROJECT OWNER — `e51e98e50011f2005c8ffea59bbffc6eba3d752a` |
| Planning parent HEAD | `e51e98e50011f2005c8ffea59bbffc6eba3d752a` |
| Phase 1B.1-D through I | NOT AUTHORIZED |
| Production migration | NOT AUTHORIZED |

---

## 3. Confirmed Scope for Phase 1B.1-D

From `phase-1b1-authentication-authorization-implementation-plan.md` (Section 25, Slice D):

> **Phase 1B.1-D: Permission, Role, Department and Admin Group evaluation.**
> - Files: Permission natural codes, assignment logic, evaluation union engine.
> - Impact: API endpoints for role management.
> - Completion: DENY precedence and hierarchical rule tests pass.

This plan covers:

1. **Permission evaluation service** — given `(userId, permissionCode, companyId?)`, compute effective grant/deny.
2. **Evaluation algorithm** — department baseline + role (company-scoped) + individual allow − individual deny, with DENY winning.
3. **Admin Group evaluation** — Admin Group membership provides permission grants that override the standard hierarchy (see Section 12).
4. **Policy version / cache invalidation** — read `Authorization_Policy_State.policy_version` on every evaluation; cache keyed on version; fail closed on DB error.
5. **Role management APIs** — CRUD for Roles and their permission assignments (`SECURITY_ROLE_VIEW`, `SECURITY_ROLE_MANAGE`).
6. **Department permission assignment APIs** — assign/replace department baseline permissions.
7. **Admin Group management APIs** — CRUD for Admin Groups and their permission assignments.
8. **Individual permission grant/revoke APIs** — temporal grant/deny for individual users.
9. **User role assignment APIs** — temporal role assignment per company scope.
10. **User admin group assignment APIs** — temporal admin group assignment.
11. **Effective permission query API** — `GET /api/v2/security/users/{id}/effective-permissions`.
12. **Permission catalog query API** — `GET /api/v2/security/permissions`.
13. **Unit and integration tests** for evaluation algorithm, DENY precedence, scope rules, and temporal overlaps.

---

## 4. Explicit Exclusions

| Item | Status |
|---|---|
| Company-scope header enforcement middleware (`X-Company-Id`) | Phase 1B.1-E — NOT AUTHORIZED |
| Frontend administration UI | Phase 1B.1-G/H — NOT AUTHORIZED |
| First-admin bootstrap | Phase 1B.1-F — NOT AUTHORIZED |
| Security audit writer (semantic audit events) | Phase 1B.1-F — NOT AUTHORIZED |
| Semantic audit scrubbing | Phase 1B.1-F — NOT AUTHORIZED |
| AD/LDAP integration | Out of scope |
| ENTITY scope authorization | Explicitly deferred per DEC-1B-006 |
| Permission list in JWT | Explicitly prohibited — DEC-1B-003, C plan §8 |
| Super-admin bypass | Explicitly prohibited — DEC-1B-007 |
| V0004/U0004 schema migration | NOT AUTHORIZED — see Section 19 |
| Production migration | NOT AUTHORIZED |
| `/auth/me` endpoint | Not in Phase 1B.1-D scope |
| `/auth/logout-all` endpoint | Not in Phase 1B.1-D scope |

---

## 5. Business Rules Confirmed from Existing Docs

The following rules are confirmed from `phase-1b0-security-discovery-decisions.md` and `phase-1b1-authentication-authorization-implementation-plan.md`:

### 5.1 Permission Code (DEC-1B-006, DEC-1B-016)

- Natural primary key `permission_code VARCHAR(100)` in `Permissions` table.
- Permission codes are immutable; managed by development, not by administrators.
- 15 approved codes seeded by V0003 (ORGANIZATION_*, SECURITY_*).
- No new permission codes are created without a development-controlled migration.

### 5.2 Scope Types (DEC-1B-006, DEC-1B-007)

- Scopes: `GLOBAL` and `COMPANY`. `ENTITY` is **deferred** and not implemented in Phase 1B.1-D.
- `GLOBAL` permissions: `company_id IS NULL` in scope context.
- `COMPANY` permissions: require `company_id IS NOT NULL` in scope context.
- Roles with `scope_type = GLOBAL` must have `company_id IS NULL` in `User_Role_Company`.
- Roles with `scope_type = COMPANY` must have `company_id IS NOT NULL` in `User_Role_Company`.
- Same scope constraint applies to Admin Groups.

### 5.3 Permission Evaluation Formula (DEC-1B-007, §12 of phase-1b1 plan)

The effective permission for `(userId, permissionCode, companyId?)` is computed as:

```
effective = (
    [department baseline permissions for user's current department(s)]
  + [role-granted permissions via active User_Role_Company assignments]
  + [individual ALLOW grants via active User_Individual_Permissions]
  - [individual DENY grants via active User_Individual_Permissions where is_deny = 1]
)
```

Admin Group grants supersede the standard role/department hierarchy (see Section 12).

### 5.4 DENY Wins (DEC-1B-007)

An individual `DENY` (`is_deny = 1`) in `User_Individual_Permissions` overrides any ALLOW from any source (department, role, admin group, or individual grant). There is no bypass.

### 5.5 No Super-Admin Bypass (DEC-1B-007, DEC-1B-009)

No hardcoded super-admin role bypasses permission evaluation. Even Admin Group members must hold explicit permission grants. The system has no "all permissions" shortcut.

### 5.6 GLOBAL Requires company_id NULL; COMPANY Requires company_id Non-Null (DEC-1B-007, DEC-1B-009)

Server-side validation enforces scope consistency at assignment time and at evaluation time. A COMPANY-scoped permission evaluation must supply a non-null `companyId`.

### 5.7 Server-Side Hard Rules Override Grants (§12 of phase-1b1 plan)

Business hard rules (e.g., self-approval prohibition, cross-company data access) are enforced at the application/controller level independently of the permission evaluation result. Permission grants cannot override hard rules.

### 5.8 No Permission List in JWT (DEC-1B-003, C plan §8)

JWT claims do not contain permissions, roles, or admin groups. Permission evaluation is always server-side on each protected request using the current database state.

### 5.9 Server-Side Trusted Evaluation (DEC-1B-011)

Permission evaluation is server-side and trusted. The JWT provides identity only (`sub`, `security_stamp`). The authorization result is never accepted from the client.

### 5.10 Policy Version and Cache (DEC-1B-011)

- `Authorization_Policy_State.policy_version` is read on every permission evaluation.
- Cache key includes `(userId, companyId, policyVersion)`.
- Cache failure must **fail closed** — return 503, never serve a stale authorization result.
- Account, session, and company checks occur before cache use (already handled by C-C).
- Immediate permission changes must be effective on the next protected request after the policy version changes.

### 5.11 Temporal Assignments (DEC-1B-014)

- `User_Role_Company`, `User_Individual_Permissions`, `User_Admin_Group_Assignments` all use half-open `[effective_from, effective_to)` date ranges.
- Only assignments where `effective_from <= NOW < effective_to` (or `effective_to IS NULL`) and `is_active = 1` are considered.
- Overlap prevention at assignment time: `SERIALIZABLE` transaction with `UPDLOCK`/`HOLDLOCK`, plus DB trigger and filtered unique index as defense in depth.
- Deadlock retry (SQL error 1205): maximum 3 attempts, bounded jitter, using existing `DeadlockRetryPolicy`.

### 5.12 Audit Requirement

- Security-sensitive assignment mutations (`ROLE_CREATE`, `ROLE_UPDATE`, `ROLE_PERMS`, `DEPT_PERMS`, `USER_ROLE_ASSIGN`, `USER_PERM_GRANT`, etc.) require audit records.
- **Semantic audit writer is Phase 1B.1-F** and is NOT implemented in Phase 1B.1-D.
- Phase 1B.1-D mutating endpoints must be structured so that the audit call is a no-op stub or deferred; the business transaction must succeed independently. The audit writer will be wired in Phase 1B.1-F.

---

## 6. Resolved Decisions (Project Owner)

The following items have been resolved by the Project Owner. There are no longer any blocking open decisions for this plan.

| # | Decision |
|---|---|
| **OD-D-01** | Individual DENY always wins over Admin Group grants. |
| **OD-D-02** | For multiple active department assignments, use union of all active department baseline permissions. |
| **OD-D-03** | If user has no active assignment to requested company, evaluator returns DENY. |
| **OD-D-04** | Use IMemoryCache for Phase 1B.1-D. Distributed cache is deferred. |
| **OD-D-05** | GET /api/v2/security/permissions returns active permissions only, where is_active = 1. |
| **OD-D-06** | Effective-permissions API requires explicit company scope for COMPANY evaluation. Result may include GLOBAL permissions plus permissions effective for the requested company. |
| **OD-D-07** | Inactive permission catalog usage/assignment returns HTTP 422 Unprocessable Entity. |
| **OD-D-08** | Continue using `PTKD_TEST_PHASE1A2` for Phase 1B.1-D integration/API tests. |


---

## 7. Proposed Architecture

Phase 1B.1-D follows the same vertical-slice modular pattern as existing slices:

```
PTKD.Domain
  └── Security/Authorization/
        ├── Permission.cs           (entity)
        ├── Role.cs                 (entity)
        ├── AdminGroup.cs           (entity)
        ├── UserRoleAssignment.cs   (entity, temporal)
        ├── UserIndividualPermission.cs (entity, temporal)
        ├── UserAdminGroupAssignment.cs (entity, temporal)
        ├── DepartmentPermission.cs (entity)
        └── RolePermission.cs       (entity)

PTKD.Application
  └── Security/Authorization/
        ├── Interfaces/
        │     ├── IPermissionEvaluator.cs
        │     ├── IPermissionService.cs
        │     ├── IRoleService.cs
        │     ├── IAdminGroupService.cs
        │     └── IAuthorizationDbContext.cs
        ├── Services/
        │     ├── PermissionEvaluator.cs
        │     ├── PermissionService.cs (catalog query)
        │     ├── RoleService.cs
        │     └── AdminGroupService.cs
        └── DTOs/
              ├── EffectivePermissionsResponse.cs
              ├── RoleRequest/Response DTOs
              ├── AdminGroupRequest/Response DTOs
              └── PermissionAssignmentRequest DTOs

PTKD.Infrastructure
  └── Security/Authorization/
        └── AuthorizationDbContext.cs (or extension of AppDbContext)

PTKD.Api
  └── Controllers/
        └── SecurityController.cs   (NEW or extend existing)
```

---

## 8. Proposed Files to Add or Change

### New Files

| File | Purpose |
|---|---|
| `src/backend/PTKD.Domain/Security/Authorization/Permission.cs` | Permission entity (natural key `permission_code`) |
| `src/backend/PTKD.Domain/Security/Authorization/Role.cs` | Role entity |
| `src/backend/PTKD.Domain/Security/Authorization/AdminGroup.cs` | Admin Group entity |
| `src/backend/PTKD.Domain/Security/Authorization/RolePermission.cs` | Role–Permission join |
| `src/backend/PTKD.Domain/Security/Authorization/DepartmentPermission.cs` | Department–Permission join |
| `src/backend/PTKD.Domain/Security/Authorization/AdminGroupPermission.cs` | Admin Group–Permission join |
| `src/backend/PTKD.Domain/Security/Authorization/UserRoleAssignment.cs` | Temporal user-role assignment |
| `src/backend/PTKD.Domain/Security/Authorization/UserIndividualPermission.cs` | Temporal individual grant/deny |
| `src/backend/PTKD.Domain/Security/Authorization/UserAdminGroupAssignment.cs` | Temporal admin group assignment |
| `src/backend/PTKD.Application/Security/Authorization/Interfaces/IPermissionEvaluator.cs` | Evaluation service interface |
| `src/backend/PTKD.Application/Security/Authorization/Interfaces/IAuthorizationDbContext.cs` | Authorization DB context interface |
| `src/backend/PTKD.Application/Security/Authorization/Interfaces/IRoleService.cs` | Role management interface |
| `src/backend/PTKD.Application/Security/Authorization/Interfaces/IAdminGroupService.cs` | Admin Group management interface |
| `src/backend/PTKD.Application/Security/Authorization/Services/PermissionEvaluator.cs` | Core evaluation engine |
| `src/backend/PTKD.Application/Security/Authorization/Services/RoleService.cs` | Role CRUD + permission assignment |
| `src/backend/PTKD.Application/Security/Authorization/Services/AdminGroupService.cs` | Admin Group CRUD + permission assignment |
| `src/backend/PTKD.Application/Security/Authorization/DTOs/` | Request/response DTOs |
| `src/backend/PTKD.Infrastructure/Security/Authorization/AuthorizationDbContextFactory.cs` | DB context factory |
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/` | EF configurations for new entities |
| `src/backend/PTKD.Api/Controllers/SecurityController.cs` | Security administration endpoints |
| `tests/backend/PTKD.UnitTests/Security/Authorization/PermissionEvaluatorTests.cs` | Unit tests for evaluator |
| `tests/backend/PTKD.UnitTests/Security/Authorization/RoleServiceTests.cs` | Unit tests for role service |
| `tests/backend/PTKD.IntegrationTests/Security/` | Integration tests (temporal overlap, DB constraints) |
| `tests/backend/PTKD.ApiTests/Security/` | API-boundary tests |

### Modified Files

| File | Change |
|---|---|
| `src/backend/PTKD.Api/Program.cs` | Register `IPermissionEvaluator`, `IRoleService`, `IAdminGroupService`, `IAuthorizationDbContext` |
| `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs` | Add DbSets for authorization entities |
| `docs/architecture/phase-1b1d-permission-evaluation-implementation-plan.md` | This file |

---

## 9. Domain and Application Design

### Domain Entities

All entities must:
- Use `bigint` PKs (except `Permission` which uses natural key `permission_code VARCHAR(100)`).
- Have `row_version` (rowversion) for optimistic concurrency.
- Have standard audit columns (`created_at`, `created_by_user_id`, `updated_at`, `updated_by_user_id`) where applicable.
- Have no cascade deletes (per V0003 design).

Temporal assignment entities (`UserRoleAssignment`, `UserIndividualPermission`, `UserAdminGroupAssignment`) additionally require:
- `effective_from DATETIME2(3) NOT NULL`
- `effective_to DATETIME2(3) NULL` (null = open-ended / infinity)
- `is_active BIT NOT NULL`

Scope validation business rules must be enforced in the Application service layer (not only in DB constraints):
- GLOBAL role → assignment `company_id` must be `null`
- COMPANY role → assignment `company_id` must be non-null

### Application Services

**`IPermissionEvaluator`**:

```csharp
Task<bool> EvaluateAsync(
    long userId,
    string permissionCode,
    long? companyId,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
    long userId,
    long? companyId,
    CancellationToken cancellationToken = default);
```

- Must read `Authorization_Policy_State.policy_version` before cache lookup.
- Must fail closed on DB or cache infrastructure exception (return deny / 503).
- Must not be called before account/session/employment validation (C-C handles that).

---

## 10. Infrastructure and Data Access Design

- EF Core for ordinary CRUD on roles, admin groups, assignments, and permissions catalog.
- For the temporal overlap check (user role/individual permission/admin group assignment), use:
  - `SERIALIZABLE` transaction
  - `UPDLOCK`, `HOLDLOCK` on queried rows
  - Deadlock retry via existing `DeadlockRetryPolicy` (max 3 attempts)
- The DB trigger and filtered unique index in V0003 remain as defense-in-depth.
- `AppDbContext` is extended to expose `DbSet<>` for all new authorization entities.
- A separate `IAuthorizationDbContext` interface (or extension of `IOrganizationDbContext`) will expose only the sets needed by the authorization application layer.

---

## 11. API and Middleware Integration Design

Phase 1B.1-D does **not** implement `X-Company-Id` middleware enforcement (Phase 1B.1-E). Controllers may call `IPermissionEvaluator` directly when needed for the management endpoints themselves.

Proposed controller: `SecurityController` at `/api/v2/security/`.

All endpoints per phase-1b0 §9 API contract (subset for Phase 1B.1-D):

| Method | Path | Required Permission | Scope | Tx | Audit stub |
|---|---|---|---|---|---|
| GET | `/api/v2/security/permissions` | `SECURITY_PERMISSION_VIEW` | GLOBAL | No | No |
| GET | `/api/v2/security/roles` | `SECURITY_ROLE_VIEW` | GLOBAL | No | No |
| GET | `/api/v2/security/roles/{id}` | `SECURITY_ROLE_VIEW` | GLOBAL | No | No |
| POST | `/api/v2/security/roles` | `SECURITY_ROLE_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/roles/{id}` | `SECURITY_ROLE_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/roles/{id}/status` | `SECURITY_ROLE_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/roles/{id}/permissions` | `SECURITY_ROLE_MANAGE` | GLOBAL | Yes | Stub |
| GET | `/api/v2/security/departments/{id}/permissions` | `SECURITY_ROLE_VIEW` | GLOBAL | No | No |
| PUT | `/api/v2/security/departments/{id}/permissions` | `SECURITY_ROLE_MANAGE` | GLOBAL | Yes | Stub |
| POST | `/api/v2/security/users/{id}/roles/assign` | `SECURITY_ASSIGNMENT_MANAGE` | COMPANY/GLOBAL | Yes | Stub |
| POST | `/api/v2/security/users/{id}/roles/close` | `SECURITY_ASSIGNMENT_MANAGE` | COMPANY/GLOBAL | Yes | Stub |
| POST | `/api/v2/security/users/{id}/individual-permissions/grant` | `SECURITY_ASSIGNMENT_MANAGE` | COMPANY/GLOBAL | Yes | Stub |
| POST | `/api/v2/security/users/{id}/individual-permissions/revoke` | `SECURITY_ASSIGNMENT_MANAGE` | COMPANY/GLOBAL | Yes | Stub |
| GET | `/api/v2/security/admin-groups` | `SECURITY_ADMIN_GROUP_VIEW` | GLOBAL | No | No |
| GET | `/api/v2/security/admin-groups/{id}` | `SECURITY_ADMIN_GROUP_VIEW` | GLOBAL | No | No |
| POST | `/api/v2/security/admin-groups` | `SECURITY_ADMIN_GROUP_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/admin-groups/{id}` | `SECURITY_ADMIN_GROUP_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/admin-groups/{id}/status` | `SECURITY_ADMIN_GROUP_MANAGE` | GLOBAL | Yes | Stub |
| PUT | `/api/v2/security/admin-groups/{id}/permissions` | `SECURITY_ADMIN_GROUP_MANAGE` | GLOBAL | Yes | Stub |
| POST | `/api/v2/security/admin-groups/{id}/users/assign` | `SECURITY_ACCOUNT_MANAGE` | GLOBAL/COMPANY | Yes | Stub |
| POST | `/api/v2/security/admin-groups/{id}/users/close` | `SECURITY_ACCOUNT_MANAGE` | GLOBAL/COMPANY | Yes | Stub |
| GET | `/api/v2/security/users/{id}/effective-permissions` | `SECURITY_ASSIGNMENT_MANAGE` | COMPANY | No | No |

> **Note**: All mutating endpoints include a no-op audit stub that will be wired in Phase 1B.1-F. The transaction must succeed independently of the audit stub.

---

## 12. Permission Evaluation Algorithm

Subject to resolution of OD-D-01 and OD-D-02, the proposed algorithm is:

```
function evaluate(userId, permissionCode, companyId):

  1. Read policy_version from Authorization_Policy_State.
  2. Check cache(userId, companyId, policyVersion, permissionCode).
     - Cache HIT: return cached result.
     - Cache MISS or infrastructure error: fail closed → deny (503).

  3. (Cache MISS only) Load from DB:
     a. Load active Admin Group assignments for user (company-scoped or GLOBAL).
     b. Load Admin Group permission grants for those groups that include permissionCode.
     c. Load active individual permissions for user (is_deny=0 ALLOW, is_deny=1 DENY).
     d. Load active role assignments for user (company-scoped or GLOBAL).
     e. Load role permission grants for those roles that include permissionCode.
     f. Load department permissions for user's current active department(s).

  4. Apply evaluation:
     - If any individual DENY (is_deny=1) for this permission → DENY (subject to OD-D-01).
     - Else if Admin Group grant exists → ALLOW.
     - Else if individual ALLOW exists → ALLOW.
     - Else if role grant exists → ALLOW.
     - Else if department grant exists → ALLOW.
     - Else → DENY.

  5. Cache result keyed on (userId, companyId, policyVersion, permissionCode).
  6. Return result.
```

> [!IMPORTANT]
> The order is finalized as: individual DENY wins first, then Admin Group, then individual ALLOW, then role, then department.

---

## 13. Scope Handling

- **GLOBAL permission evaluation**: `companyId` is `null`. Only assignments with `company_id IS NULL` are included.
- **COMPANY permission evaluation**: `companyId` is non-null. Assignments with matching `company_id IS NOT NULL` are included. GLOBAL assignments with `company_id IS NULL` are also included (GLOBAL grants apply to all companies for the holding user). Subject to OD-D-03.
- **Assignment creation**: Server enforces `scope_type = GLOBAL ↔ company_id IS NULL` and `scope_type = COMPANY ↔ company_id IS NOT NULL` at the application layer.
- **ENTITY scope**: Not implemented in Phase 1B.1-D.

---

## 14. DENY Precedence Handling

Per DEC-1B-007: Individual DENY overrides all ALLOW sources. There is no path through the evaluation that grants access once an individual DENY exists for the evaluated `(userId, permissionCode, companyId)`.

Implementation:
- `User_Individual_Permissions.is_deny = 1` rows are loaded first.
- A match immediately short-circuits to DENY before checking any ALLOW source.
- Subject to resolution of OD-D-01 regarding Admin Group vs. individual DENY.

---

## 15. Fail-Closed Behavior

Per DEC-1B-011:

- If the DB cannot be reached during `policy_version` read → return 503 `AUTH_UNEXPECTED_DATABASE_ERROR`.
- If the cache returns an infrastructure error → fail closed, return 503.
- Under no circumstances is a stale or assumed authorization result returned.
- If the evaluator throws an unhandled exception → catch at controller level, return 503 (sanitized ProblemDetails).

---

## 16. Caching Strategy

- **Technology**: `IMemoryCache` (subject to OD-D-04 for distributed cache preference).
- **Cache key**: `$"perm:{userId}:{companyId}:{policyVersion}:{permissionCode}"` for point checks.
  Or: `$"perms:{userId}:{companyId}:{policyVersion}"` for the full effective-permission set.
- **Invalidation**: When `Authorization_Policy_State.policy_version` is incremented by any mutating security operation, the new version key produces a cache miss. Old entries expire naturally (TTL) or are evicted.
- **TTL**: Short TTL (e.g., 5 minutes) as defense against cache server staleness; policy version is the primary invalidation signal.
- **Cache failure**: Fail closed — do not serve permission data if cache backend is unavailable. Never use a stale result.

---

## 17. Audit and Logging Considerations

- Security-sensitive mutating operations (role create/update, assignment grant/revoke) must produce audit records.
- **Phase 1B.1-D does not implement the audit writer** (deferred to Phase 1B.1-F).
- Controllers must accept an injected `ISecurityAuditWriter` (or no-op stub) to be wired in Phase 1B.1-F.
- Technical logging (Serilog) of evaluation decisions must not log permission grants/denies in a way that reveals user data. Log only the evaluation outcome and correlation ID.
- Do not write passwords, tokens, signing keys, or user-identifying data beyond actor user ID and correlation ID to logs.

---

## 18. Test Strategy

### Unit Tests

| Test | Validates | DEC |
|---|---|---|
| `Evaluate_DepartmentBaseline_GrantsPermission` | Department permissions included in evaluation | AUTH-001 |
| `Evaluate_RoleGrant_GrantsPermission` | Role permission included in evaluation | AUTH-002, DEC-1B-007 |
| `Evaluate_IndividualDeny_OverridesRoleGrant` | DENY wins over ALLOW | AUTH-004, DEC-1B-007 |
| `Evaluate_IndividualDeny_OverridesDepartmentGrant` | DENY wins over department baseline | DEC-1B-007 |
| `Evaluate_AdminGroupGrant_GrantsPermission` | Admin Group membership grants | DEC-1B-007 |
| `Evaluate_NoAssignment_Denies` | Default deny with no assignment | DEC-1B-007 |
| `Evaluate_GlobalPermission_NullCompany` | GLOBAL scope with null company | DEC-1B-007, DEC-1B-009 |
| `Evaluate_CompanyPermission_WrongCompany_Denies` | COMPANY scope mismatch | DEC-1B-012 |
| `Evaluate_PolicyVersion_CacheInvalidatesOnVersionChange` | Cache miss on policy version change | AUTH-012, DEC-1B-011 |
| `Evaluate_InfrastructureException_FailsClosed` | Fail closed on DB error | DEC-1B-011 |
| `RoleService_GlobalRole_RequiresNullCompany` | Scope enforcement at assignment | DEC-1B-007 |
| `RoleService_CompanyRole_RequiresNonNullCompany` | Scope enforcement at assignment | DEC-1B-007 |
| `AdminGroup_ScopeValidatesCompanyId` | Admin Group scope enforcement | DEC-1B-007, DEC-1B-009 |
| `UserRoleAssignment_TemporalOverlap_Rejected` | Overlap prevention logic | DEC-1B-014 |
| `IndividualPermission_TemporalOverlap_Rejected` | Overlap prevention logic | DEC-1B-014 |

### Integration Tests (DB = PTKD_TEST_PHASE1A2 or resolved OD-D-08)

| Test | Validates |
|---|---|
| `DB_Reject_PTKD_DEV_BeforeAnyWrite` | Database safety guard |
| `UserRoleAssignment_TemporalOverlap_BlockedByTrigger` | V0003 trigger enforcement |
| `UserIndividualPermission_TemporalOverlap_BlockedByTrigger` | V0003 trigger enforcement |
| `UserAdminGroupAssignment_TemporalOverlap_BlockedByTrigger` | V0003 trigger enforcement |
| `PolicyVersion_Increment_InvalidatesCache` | Policy version–cache invalidation |
| `Evaluate_FullHierarchy_EndToEnd` | Full evaluation pipeline against real DB |
| `Evaluate_DENY_Wins_EndToEnd` | DENY precedence verified at DB level |
| `ConcurrentAssignment_Deadlock1205_RetryAndSucceeds` | Deadlock retry behavior |

### API Tests

| Test | Validates |
|---|---|
| `CreateRole_WithValidData_Returns201` | Role creation |
| `CreateRole_WithDuplicateName_Returns409` | Uniqueness enforcement |
| `AssignRoleToUser_WithOverlap_Returns409` | Temporal overlap error mapping |
| `GrantIndividualPermission_InactivePermission_Returns409or422` | Inactive permission handling (OD-D-07) |
| `EffectivePermissions_MatchesEvaluatorResult` | API ↔ evaluator consistency |
| `SecurityController_RequiresPermission_Returns403` | Permission enforcement on management APIs |
| `SecurityController_NoToken_Returns401` | Authentication enforcement |
| `ResponseBody_DoesNotRevealInternalReason` | Non-enumerating responses |

### Negative and Security Tests

| Test | Validates |
|---|---|
| `Evaluate_MissingCompanyId_ForCompanyScopedPermission_Denies` | Missing scope → deny |
| `Evaluate_CrossCompany_Denies` | Company isolation |
| `Evaluate_InactivRole_NotApplied` | Inactive role excluded |
| `Evaluate_InactivePermission_NotGranted` | Inactive permission excluded |
| `Regression_Phase1A2_Tests_StillPass` | Phase 1A.2 regression |

---

## 19. Database and Migration Impact

### V0003 Status

All tables required for Phase 1B.1-D already exist in V0003:

| Table | Status |
|---|---|
| `Permissions` | ✅ EXISTS in V0003 |
| `Roles` | ✅ EXISTS in V0003 |
| `Role_Permissions` | ✅ EXISTS in V0003 |
| `Department_Permissions` | ✅ EXISTS in V0003 |
| `Admin_Groups` | ✅ EXISTS in V0003 |
| `Admin_Group_Permissions` | ✅ EXISTS in V0003 |
| `User_Role_Company` | ✅ EXISTS in V0003 |
| `User_Individual_Permissions` | ✅ EXISTS in V0003 |
| `User_Admin_Group_Assignments` | ✅ EXISTS in V0003 |
| `Authorization_Policy_State` | ✅ EXISTS in V0003 |
| Temporal overlap triggers | ✅ EXISTS in V0003 |
| Filtered unique indexes | ✅ EXISTS in V0003 |

### V0004 Assessment

**V0004 is not required for Phase 1B.1-D.** All required schema is present in V0003.

If implementation discovers that V0003 is structurally insufficient, work must stop and report:
`PHASE 1B.1-D IMPLEMENTATION BLOCKED — V0004 DECISION REQUIRED`

### Production Migration

Production migration remains **NOT AUTHORIZED**.

---

## 20. Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| OD-D-01 (Admin Group vs. DENY precedence) is unresolved | Medium | High — incorrect evaluation algorithm | Block implementation until Project Owner resolves |
| OD-D-02 (multi-department user) is unresolved | Medium | Medium — incorrect effective permission for edge case | Block if multi-department users exist in production data model |
| Cache serves stale data after policy version increment | Low | High — incorrect authorization | Enforce cache key includes policy_version; fail closed on cache error |
| Temporal overlap not caught by application logic | Low | Medium | V0003 trigger + filtered index provide defense-in-depth |
| EF query generates incorrect JOIN for temporal active records | Medium | High | Integration tests against real SQL Server required |
| MSB3277 assembly conflict in PTKD.ApiTests worsens | Low | Low — pre-existing, non-blocking | Monitor; do not introduce new conflicting packages |
| Phase 1B.1-D controller endpoints used without company scope enforcement | High | High | Phase 1B.1-E must follow promptly; interim risk acknowledged |

---

## 21. Contradictions and Missing Decisions

1. **§12 of phase-1b1 plan** lists hierarchy as "Admin Group → Explicit Deny → Individual Grant → Role Grant" — this implies Admin Group wins over DENY. However, **DEC-1B-007** says "No hardcoded super-admin bypass" and individual DENY winning over all grants is a fundamental security principle. These are in tension. → **OD-D-01**.

2. **Phase-1b0 §9** shows `AUTH_CURRENT_COMPANY_REQUIRED` as "400 or 403 (explicitly decide and document)" — not yet decided. This affects Phase 1B.1-E more directly, but D's API contract must be consistent. → Record as forward dependency.

3. **`PTKD_TEST_PHASE1B`** appears in phase-1b0 §10 test traceability but **`PTKD_TEST_PHASE1A2`** is the confirmed test database used by C-A/C-B/C-C. → **OD-D-08**: confirm which database name to use.

---

## 22. Recommended Implementation Slices

Phase 1B.1-D is large and should be implemented in three sub-slices to allow incremental verification:

| Sub-slice | Content |
|---|---|
| **D-1** | Domain entities, EF configurations, IAuthorizationDbContext, DB wiring in Program.cs, seed data verification against V0003 |
| **D-2** | `IPermissionEvaluator` / `PermissionEvaluator`, policy version cache, unit tests for evaluation algorithm |
| **D-3** | Security administration APIs (roles, admin groups, assignments, individual permissions), temporal overlap enforcement, API tests |

Each sub-slice requires passing tests before proceeding. The Project Owner may authorize all three together or separately.

---

## 23. Acceptance Criteria

Phase 1B.1-D is complete when:

1. All unit tests for the permission evaluator pass, including DENY precedence and fail-closed.
2. All integration tests pass against the authorized test database (no `PTKD_DEV` connection).
3. All API tests for security administration endpoints pass.
4. `dotnet build` succeeds with 0 errors (existing MSB3277 non-blocking warnings acceptable).
5. Phase 1A.2 regression suite (138 IntegrationTests, 88 ApiTests) still passes.
6. No permission data in JWT.
7. No super-admin bypass path exists in code.
8. Evaluation returns 503 (not deny-silently) on infrastructure failure.
9. `Authorization_Policy_State.policy_version` increment causes cache miss on next evaluation.
10. Implementation evidence document created with all required fields.

---

## 24. Project Owner Approval Checklist

Before authorizing Phase 1B.1-D implementation, the Project Owner must confirm:

- [x] The Admin Group vs. individual DENY precedence (OD-D-01) is resolved.
- [x] Multi-department user handling (OD-D-02) is resolved.
- [x] Company scope boundary during evaluation (OD-D-03) is resolved.
- [x] Cache technology (OD-D-04) is acceptable (in-memory or distributed).
- [x] Permission catalog filtering (OD-D-05) — active-only or all.
- [x] Effective-permissions endpoint scope behavior (OD-D-06) is resolved.
- [x] `AUTH_PERMISSION_CATALOG_INACTIVE` HTTP status (OD-D-07: 409 or 422) is decided.
- [x] Test database name (OD-D-08: `PTKD_TEST_PHASE1A2` or `PTKD_TEST_PHASE1B`) is confirmed.
- [ ] The three sub-slice structure (D-1, D-2, D-3) or an alternative is approved.
- [ ] Phase 1B.1-D implementation is explicitly authorized (this plan acceptance does NOT authorize implementation).

---

## Authorization Status

| Phase | Status |
|---|---|
| Phase 1B.1-D implementation | **NOT AUTHORIZED BY THIS PLAN** |
| Phase 1B.1-E through I | NOT AUTHORIZED |
| Production migration | NOT AUTHORIZED |

Authorizer: _Pending Project Owner review_
