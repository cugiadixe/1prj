# Phase 1B.1-E-B Security API Shared Enforcement Plan

## 1. Status
ACCEPTED PLAN — PHASE 1B.1-E-B IMPLEMENTATION MAY BE AUTHORIZED SEPARATELY

## 2. Baseline
- Current accepted HEAD: 4b7d1561f008892dcf351b6a152f2f7efb7ca061
- E-A accepted foundation.
- D-B Security Administration APIs accepted.

## 3. Purpose
E-B migrates accepted D-B Security Administration API authorization from manual controller helper checks to the shared RequirePermission enforcement mechanism created in E-A, without changing business behavior.

## 4. In-scope
- Audit all D-B Security Administration controller manual authorization checks.
- Replace equivalent manual SecurityControllerHelper.EnforcePermissionAsync checks with shared RequirePermission metadata.
- Preserve SECURITY_ADMIN_MANAGE authorization semantics.
- Preserve existing D-B API behavior and status codes.
- Preserve service-level company-scope validation.
- Keep D-B regression tests passing.
- Add or adjust tests proving D-B APIs now use shared enforcement.

## 5. Out-of-scope
- Do not touch UsersController, DepartmentsController, or CompaniesController.
- Do not create new permission codes.
- Do not introduce multi-permission behavior.
- No V0004/U0004.
- No production migration.
- No production seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.

## 6. Required equivalence audit
Before implementation, E-B must inspect and document:
- Which D-B endpoints currently call SecurityControllerHelper.
- Which permission code is used.
- Whether current manual check evaluates GLOBAL or COMPANY scope.
- Whether companyId is used in the authorization check or only in service validation.
- Which responses are currently 401, 403, 400, 422, 409.
- Which behavior must remain unchanged.

Important:
Do not blindly set all D-B endpoints to PermissionScope.Global unless the audit proves current manual enforcement is GLOBAL-equivalent.

## 7. Proposed technical design
- **Attribute placement strategy:** We will decorate individual actions (or the controller class, if all actions identically use the same permission and scope) with `[RequirePermission(PermissionCodes.SecurityAdminManage, Scope = ...)]`.
- **Safe removal:** We will safely remove the duplicated manual calls to `SecurityControllerHelper.EnforcePermissionAsync` inside each controller action.
- **Service layer validation:** Service layer company validation logic within `SecurityAdminService` will remain completely untouched.
- **Avoiding double enforcement:** Using only the attribute prevents double enforcement while keeping the exact same authorization evaluation path via `IPermissionEvaluator`.
- **Response preservation:** The `PermissionAuthorizationFilter` returns standard `ProblemDetails` for 403 and relies on the default framework for 401, preserving exact API contract compatibility.
- **Testing:** We will verify all existing Phase D-B tests still pass exactly as they do now, proving equivalence.

## 8. Testing strategy
Tests must cover:
- unauthenticated request returns 401;
- authenticated without SECURITY_ADMIN_MANAGE returns 403;
- authenticated with SECURITY_ADMIN_MANAGE succeeds;
- D-B mutation behavior unchanged;
- inactive permission still returns 422;
- duplicate/idempotent behavior unchanged;
- overlap conflict still returns 409;
- policy version increment tests still pass;
- effective-permissions self-query remains forbidden;
- full Unit, Integration, Api, DatabaseSafety suites.

## 9. Recorded Project Owner decisions

OD-E-B-01:
E-B migrates only D-B Security Administration APIs to shared RequirePermission enforcement.

OD-E-B-02:
Organization APIs remain out of scope until canonical permission codes are added to permission-catalog.md.

OD-E-B-03:
Migration must preserve existing D-B authorization semantics exactly, including GLOBAL vs COMPANY behavior discovered during equivalence audit.

OD-E-B-04:
SecurityAdminService company-scope validation remains in the service layer and is not replaced by the filter.

OD-E-B-05:
E-B does not introduce multi-permission any-of/all-of behavior.

OD-E-B-06:
No new permission codes are created in E-B.

## 10. Risks
- Accidentally changing D-B 401/403 behavior.
- Accidentally removing service-level company validation.
- Double-enforcement if manual helper is not removed cleanly.
- Misclassifying SECURITY_ADMIN_MANAGE as GLOBAL or COMPANY without audit.
- Breaking accepted D-B tests.

## 11. Acceptance criteria
- Plan acceptance required before implementation.
- No code changed in this plan commit.
- No tests changed in this plan commit.
- No migrations.
