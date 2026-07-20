# Phase 1B.1-E-D User Assignments API Permission Enforcement Plan

## 1. Status
DRAFT — AWAITING PROJECT OWNER REVIEW

## 2. Baseline
- Current accepted HEAD: `b912492aef32690a9e30aaf4184a7f357451af0d`
- E-A accepted enforcement foundation.
- E-B accepted Security Administration APIs shared enforcement.
- E-C accepted Organization API enforcement (UsersController, DepartmentsController, CompaniesController).
- Post-E status review discovered `UserAssignmentsController` as remaining unprotected Organization mutation surface.

## 3. Purpose
E-D closes the remaining critical Phase E enforcement gap by applying `[Authorize]` and `[RequirePermission]` to `UserAssignmentsController`. All 7 actions in this controller are mutation-only endpoints that currently allow unauthenticated access to user organizational assignment operations, including company assignment, department assignment, primary assignment changes, company assignment closure, and intra/cross-company transfers.

The already-approved `ORGANIZATION_USER_MANAGE` permission (OD-E-C-02, phase-1b1e-c-organization-api-permission-plan.md) covers user management in Phase 1B and applies equally to assignment management. No new permission code is required.

## 4. UserAssignmentsController audit

Controller class route: `[Route("api/v2/organizations/users/{userId}")]`

| # | Action | Method / Route | `[Authorize]` | `[RequirePermission]` | Reads/Mutates | Business purpose | Risk if unauthenticated | Proposed permission | Proposed scope | X-Company-Id required in Phase 1B |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `AssignCompany` | `POST .../companies` | No | No | Mutates | Adds a company assignment for a user. | Unauthenticated caller can place any user into any company. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 2 | `AssignDepartment` | `POST .../departments` | No | No | Mutates | Adds a department assignment for a user within an existing company assignment. | Unauthenticated caller can assign any user to any department. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 3 | `ChangePrimaryCompany` | `PUT .../company-assignments/{id}/primary` | No | No | Mutates | Sets a specified company assignment as the user's primary. | Unauthenticated caller can alter any user's primary company designation. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 4 | `ChangePrimaryDepartment` | `PUT .../department-assignments/{id}/primary` | No | No | Mutates | Sets a specified department assignment as the user's primary. | Unauthenticated caller can alter any user's primary department designation. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 5 | `CloseCompanyAssignment` | `PUT .../company-assignments/{id}/close` | No | No | Mutates | Terminates a user's company assignment with an effective date. | Unauthenticated caller can close any active company assignment, removing user access. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 6 | `SameCompanyDepartmentTransfer` | `POST .../company-assignments/{id}/transfer/same-company` | No | No | Mutates | Transfers a user to a different department within the same company. | Unauthenticated caller can reassign any user's department within a company. | `ORGANIZATION_USER_MANAGE` | Global | No |
| 7 | `CrossCompanyTransfer` | `POST .../company-assignments/{id}/transfer/cross-company` | No | No | Mutates | Transfers a user from one company to another, closing the source and creating a destination assignment. | Unauthenticated caller can execute a cross-company transfer for any user — the highest-impact gap in this controller. | `ORGANIZATION_USER_MANAGE` | Global | No |

Observations:
- All 7 actions are pure mutations (all return `204 NoContent`). There are no read endpoints in this controller.
- No action currently carries `[Authorize]` or `[RequirePermission]`.
- No action requires `X-Company-Id` today; none is proposed for Phase 1B (consistent with OD-E-C-04).
- The controller is a sibling surface to `UsersController` (same organizational domain, same `{userId}` route segment prefix).

## 5. Proposed Project Owner decisions

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

## 6. Recommended implementation scope after plan acceptance

- Add `using` directives for `Microsoft.AspNetCore.Authorization`, `PTKD.Api.Security.Authorization`, `PTKD.Application.Security.Authorization.Attributes`, and `PTKD.Application.Security.Authorization.Models` to `UserAssignmentsController.cs`.
- Apply `[Authorize]` and `[RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)]` at the controller class level.
- Do not modify any action method, service call, DTO, or response.

Add API tests proving:
- Unauthenticated request to each representative endpoint returns 401.
- Authenticated user without `ORGANIZATION_USER_MANAGE` returns 403.
- Authenticated user with `ORGANIZATION_USER_MANAGE` succeeds on at least `AssignCompany` (the primary representative mutation).
- `SECURITY_ADMIN_MANAGE` alone is not accepted as a substitute (returns 403).
- Sending `X-Company-Id` does not cause a 400 response (Global scope ignores the header).
- Existing route and action contracts are unchanged.

Run full build and test suite:
- Build: 0 warnings, 0 errors.
- UnitTests: 0 failed.
- IntegrationTests: 0 failed.
- ApiTests: 0 failed.
- DatabaseSafety: 0 failed.

## 7. Explicit exclusions
- No new permission code.
- No `PermissionScope.Company`.
- No `X-Company-Id` requirement.
- No read/manage split.
- No JWT permission claims.
- No V0004/U0004 migration.
- No production migration.
- No production seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No `SystemController` implementation change.
- No line-ending normalization.
- No tag/push.

## 8. Risks
- Leaving `UserAssignmentsController` unauthenticated permits unauthorized user-company and user-department assignment mutations, including cross-company transfers, by any unauthenticated caller.
- Using `PermissionScope.Company` before entity-company ownership validation is designed may create a false sense of scoped security without actually enforcing company boundaries at the database level.
- Creating a new assignment-specific permission code in Phase 1B would expand the permission catalog unnecessarily before the full business authorization model is designed.
- Overloading `SECURITY_ADMIN_MANAGE` would blur Security Administration and Organization User management domain boundaries.
- Failing to add enforcement tests risks re-opening previously unprotected endpoints silently in future refactors.

## 9. Acceptance criteria
- Plan accepted by Project Owner.
- OD-E-D-01 through OD-E-D-09 recorded.
- No implementation is performed in this plan commit.
- No application code, tests, or migrations are changed.
