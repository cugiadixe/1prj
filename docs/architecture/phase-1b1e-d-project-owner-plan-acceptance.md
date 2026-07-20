# Phase 1B.1-E-D Project Owner Plan Acceptance

## Status
PHASE 1B.1-E-D PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## Accepted plan commit
3653ceeb6311e663a58c283c87df0f1e91f0885d

## Current accepted baseline
b912492aef32690a9e30aaf4184a7f357451af0d

## Accepted next slice
Phase 1B.1-E-D — UserAssignmentsController Permission Enforcement

## Project Owner Decisions

**OD-E-D-01:**
`UserAssignmentsController` uses the existing `ORGANIZATION_USER_MANAGE` permission in Phase 1B.
No new `ORGANIZATION_USER_ASSIGNMENT_MANAGE` permission code is created.
Assignment management is subordinate to user management and shares the same managing role in Phase 1B.

**OD-E-D-02:**
`UserAssignmentsController` uses `PermissionScope.Global` in Phase 1B and does not require `X-Company-Id`.
Company-scoped assignment enforcement is deferred until entity-company ownership validation is explicitly designed, consistent with OD-E-C-04.

**OD-E-D-03:**
All 7 actions in `UserAssignmentsController` require `[Authorize]` and `[RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)]` applied at the controller class level.
No per-action override is needed.

**OD-E-D-04:**
Existing route, action, and DTO contracts must remain unchanged.
Only the authorization attributes are added; no business logic, service calls, or response shapes are modified.

**OD-E-D-05:**
No read/manage split is introduced in E-D.
All endpoints use a single manage permission, consistent with OD-E-C-07.

**OD-E-D-06:**
No permission claims are added to the JWT in E-D.
Enforcement continues via `IPermissionEvaluator` database lookup, consistent with the foundation established in E-A.

**OD-E-D-07:**
No migration, production seed/bootstrap, Phase F audit writer, frontend, or business module implementation is authorized in E-D.

**OD-E-D-08:**
`SystemController` `GET /api/v2/system/info` is not part of the E-D critical path.
A separate Project Owner decision is required to determine whether to: keep it public; reduce the information it returns (e.g., omit the environment variable); or protect it with `[Authorize]`.
E-D does not block on this decision.

**OD-E-D-09:**
`SECURITY_ADMIN_MANAGE` and `SECURITY_AUDIT_VIEW` catalog backfill (adding them to `permission-catalog.md`) is documentation hygiene from Phase D-B and is not part of E-D enforcement implementation unless separately authorized.

## Accepted implementation direction
- Add `using` directives for `Microsoft.AspNetCore.Authorization`, `PTKD.Api.Security.Authorization`, `PTKD.Application.Security.Authorization.Attributes`, and `PTKD.Application.Security.Authorization.Models` to `UserAssignmentsController.cs`.
- Apply `[Authorize]` and `[RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)]` at the controller class level.
- Do not modify any action method, service call, DTO, or response.
- Add API tests proving: unauthenticated request to each representative endpoint returns 401; authenticated user without `ORGANIZATION_USER_MANAGE` returns 403; authenticated user with `ORGANIZATION_USER_MANAGE` succeeds on at least `AssignCompany`; `SECURITY_ADMIN_MANAGE` alone returns 403; sending `X-Company-Id` does not cause a 400 (Global scope ignores the header); existing route and action contracts are unchanged.
- Run full build and test suite: 0 warnings, 0 errors; 0 failed in UnitTests, IntegrationTests, ApiTests, DatabaseSafety.

## Explicit non-authorization
- No implementation in this commit.
- No application code changes.
- No tests changed.
- No new permission code.
- No `PermissionScope.Company`.
- No `X-Company-Id` requirement.
- No migration.
- No seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No `SystemController` implementation change.
- No line-ending normalization.
- No tag/push.

## Next step
E-D implementation requires a separate implementation authorization prompt.
