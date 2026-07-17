# Phase 1B.1-D-A: Permission Evaluator Foundation — Completion Evidence

**Date:** 2026-07-17  
**Branch:** `feature/phase-1-organization`  
**Base Commit:** `f74c3f8b4445dd8b90f3b9b2dbd8b3c7d585cf06`  
**Plan Reference:** `docs/architecture/phase-1b1d-permission-evaluation-implementation-plan.md`

---

## 1. Summary of Implemented Behavior

Phase 1B.1-D-A delivers the core permission evaluation engine. The following capabilities are now in place:

| Capability | Status |
|---|---|
| `IPermissionEvaluator` interface with `EvaluateAsync` and `GetEffectivePermissionsAsync` | ✅ Implemented |
| `PermissionEvaluator` service implementing the five-layer cascade | ✅ Implemented |
| Individual DENY wins over all grants (OD-D-01) | ✅ Implemented |
| Union of active department baseline permissions (OD-D-02) | ✅ Implemented |
| Fail-closed on DB/cache error — returns `false` / empty list | ✅ Implemented |
| `IMemoryCache` with policy-version cache key (OD-D-04) | ✅ Implemented |
| Inactive permission catalog items return `false` (OD-D-05) | ✅ Implemented |
| `IAuthorizationDbContext` interface exposing all authorization DbSets | ✅ Implemented |
| Domain entities for all 10 authorization tables | ✅ Implemented |
| EF Core configuration for all authorization entities | ✅ Implemented |
| DI registration in `Program.cs` | ✅ Implemented |
| Unit tests — 11 evaluator scenarios | ✅ All pass |
| Integration tests — 2 SQL Server evaluator scenarios | ✅ All pass |

---

## 2. Evaluation Cascade (OD-D-01, OD-D-02)

`EvaluateAsync` executes the following steps in order. The first matching condition terminates evaluation:

1. **Permission catalog check** — if the `permission_code` is not in `Permissions` with `is_active = 1`, return `false`.
2. **Scope validation** — `COMPANY`-scoped permission requires `companyId ≠ null`; `GLOBAL`-scoped permission requires `companyId == null`.
3. **Individual DENY** — if any active, temporally valid `DENY` individual permission matches the user and scope, return `false` immediately.
4. **Admin Group grant** — if user has an active admin-group assignment and that group has the permission, return `true`.
5. **Individual ALLOW** — if user has an active individual `ALLOW` for the permission, return `true`.
6. **Role grant** — if user has an active role assignment and the role has the permission, return `true`.
7. **Department baseline** — if user has an active department assignment in the same company, and the department has the permission, return `true`.
8. **Default** — return `false`.

`GetEffectivePermissionsAsync` collects all grants from layers 1, 2, 3, and 4 (union), then removes all individual DENYs, and finally intersects with the active permission catalog.

---

## 3. Files Changed

### [MODIFIED] [Program.cs](file:///C:/Projects/PTKD-ERP/src/backend/PTKD.Api/Program.cs)
- Registered `IAuthorizationDbContext` (resolved from `AppDbContext`).
- Registered `IMemoryCache` via `AddMemoryCache()`.
- Registered `IPermissionEvaluator` → `PermissionEvaluator` (scoped).

### [MODIFIED] [AppDbContext.cs](file:///C:/Projects/PTKD-ERP/src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs)
- Implemented `IAuthorizationDbContext` interface.
- Added DbSets: `Permissions`, `Roles`, `AdminGroups`, `RolePermissions`, `AdminGroupPermissions`, `DepartmentPermissions`, `UserRoleAssignments`, `UserAdminGroupAssignments`, `UserIndividualPermissions`, `AuthorizationPolicyStates`.

### [NEW] Domain Entities (`src/backend/PTKD.Domain/Security/Authorization/`)
| File | Entity |
|---|---|
| `Permission.cs` | `Permissions` table |
| `Role.cs` | `Roles` table |
| `AdminGroup.cs` | `Admin_Groups` table |
| `RolePermission.cs` | `Role_Permissions` join |
| `AdminGroupPermission.cs` | `Admin_Group_Permissions` join |
| `DepartmentPermission.cs` | `Department_Permissions` join |
| `UserRoleAssignment.cs` | `User_Role_Assignments` table |
| `UserAdminGroupAssignment.cs` | `User_Admin_Group_Assignments` table |
| `UserIndividualPermission.cs` | `User_Individual_Permissions` table |
| `AuthorizationPolicyState.cs` | `Authorization_Policy_State` table |

### [NEW] Application Interfaces (`src/backend/PTKD.Application/Security/Authorization/Interfaces/`)
| File | Description |
|---|---|
| `IAuthorizationDbContext.cs` | Contracts for all authorization DbSets |
| `IPermissionEvaluator.cs` | `EvaluateAsync` and `GetEffectivePermissionsAsync` contracts |

### [NEW] Application Service (`src/backend/PTKD.Application/Security/Authorization/Services/`)
| File | Description |
|---|---|
| `PermissionEvaluator.cs` | Full cascade implementation with cache and fail-closed error handling |

### [NEW] EF Core Configuration (`src/backend/PTKD.Infrastructure/Persistence/Configurations/`)
| File | Description |
|---|---|
| `AuthorizationConfigurations.cs` | IEntityTypeConfiguration for all 10 authorization entities mapping to V0003 tables |

---

## 4. Database Migration

No new migration script was created. This phase maps entirely to V0003 (`V0003__create_security_schema.sql`), which was delivered in Phase 1A.2 and is already applied to `PTKD_DEV` and `PTKD_TEST_PHASE1A2`.

**Rollback script exists:** `database/rollbacks/U0003__drop_security_schema.sql` (delivered in Phase 1A.2, unchanged).

---

## 5. API Endpoints Changed

None. Phase 1B.1-D-A is a foundational service layer only. No new endpoints are exposed.

---

## 6. Tests Added / Updated

### Unit Tests — `tests/backend/PTKD.UnitTests/Security/Authorization/PermissionEvaluatorTests.cs`

| Test | Covers |
|---|---|
| `Evaluate_InactivePermission_ReturnsFalse` | Inactive catalog → DENY |
| `Evaluate_NoAssignments_ReturnsFalse` | No assignments → DENY |
| `Evaluate_RoleGrant_GrantsPermission` | Role grant → ALLOW |
| `Evaluate_DepartmentGrant_GrantsPermission` | Dept baseline → ALLOW |
| `Evaluate_AdminGroupGrant_GrantsPermission` | Admin group → ALLOW |
| `Evaluate_IndividualAllow_GrantsPermission` | Individual ALLOW → ALLOW |
| `Evaluate_IndividualDeny_OverridesRoleGrant` | DENY beats Role (OD-D-01) |
| `Evaluate_IndividualDeny_OverridesDepartmentGrant` | DENY beats Dept (OD-D-01) |
| `Evaluate_IndividualDeny_OverridesAdminGroupGrant` | DENY beats AdminGroup (OD-D-01) |
| `Evaluate_DataAccessException_ReturnsFailClosedDeny` | DB failure → `false` (fail-closed) |
| `GetEffectivePermissions_*` | Union + DENY subtraction |

### Integration Tests — `tests/backend/PTKD.IntegrationTests/Security/Authorization/PermissionEvaluatorIntegrationTests.cs`

| Test | Covers |
|---|---|
| `EvaluateAsync_WithIndividualDeny_ReturnsFalse_EvenWithRoleGrant` | Full SQL Server DENY-wins path |
| `EvaluateAsync_WithRoleGrant_ReturnsTrue` | Full SQL Server role grant path |

Both tests use `PTKD_TEST_PHASE1A2` verified by `TestDatabaseSafety` guard.

---

## 7. Build and Test Results

### Build
```
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --no-restore
→ 4 Warning(s), 0 Error(s)
```
Warnings are pre-existing MSB3277 assembly version conflicts in `PTKD.ApiTests` (transitive JWT version mismatch between test project and application — pre-existing, not introduced by this phase).

### Unit Tests
```
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore
→ Passed! Failed: 0, Passed: 92, Skipped: 0, Total: 92
```

### Integration Tests
```
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore
→ Passed! Failed: 0, Passed: 140, Skipped: 0, Total: 140
```

### API Tests
```
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore
→ Passed! Failed: 0, Passed: 88, Skipped: 0, Total: 88
```

---

## 8. Unresolved Risks and Decisions

| Item | Status |
|---|---|
| `EvaluateAsync` will evaluate to `false` for `GLOBAL` scope if `companyId` is passed — OD-D-06 constraint. Callers must pass `null` for global checks. | Documented, enforced |
| Inactive permission usage returns `false` — OD-D-07 (422 on write operations is out of scope for D-A). | Out of scope for this phase |
| `TimeProvider` is `TimeProvider.System` in all registrations — tests override via constructor injection. | Confirmed |
| Cache key includes `policyVersion` read from `Authorization_Policy_State` row `id=1`. If the row does not exist, version defaults to `1` (stable). | Confirmed safe |

---

## 9. Manual Verification Steps

1. Confirm HEAD is at the commit produced by this phase:
   ```
   git log --oneline -1
   ```

2. Run the full test suite and confirm all pass:
   ```
   dotnet test src/backend/PTKD-ERP.sln --configuration Debug
   ```

3. Verify `PTKD_TEST_PHASE1A2` guard is active by inspecting `TestDatabaseSafety.cs` and confirming integration tests refuse any other database name.

4. Confirm no new IIS or production configuration was added.

5. Confirm no V0004 migration script exists under `database/migrations/`.
