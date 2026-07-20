# Phase 1B.1-E-B Security API Shared Enforcement Plan

## 1. Status
DRAFT — AWAITING PROJECT OWNER REVIEW

## 2. Baseline
- Current accepted HEAD: 4b7d1561f008892dcf351b6a152f2f7efb7ca061
- E-A accepted foundation.
- D-B Security Administration APIs accepted.

## 3. Purpose
E-B migrates accepted D-B Security Administration API authorization from manual controller helper checks to the shared RequirePermission enforcement mechanism created in E-A, without changing business behavior.

## 4. In-scope
- Audit every D-B Security controller authorization check.
- Replace manual SecurityControllerHelper.EnforcePermissionAsync calls with shared RequirePermission metadata where behavior is equivalent.
- Preserve current SECURITY_ADMIN_MANAGE requirement.
- Preserve current service-level company-scope validation.
- Preserve all existing D-B API contracts and status codes.
- Keep existing D-B tests passing.
- Add/adjust tests to prove shared enforcement is active on real D-B controllers.
- No broad Organization API enforcement.

## 5. Out-of-scope
- UsersController, DepartmentsController, CompaniesController.
- New permission codes for Organization APIs.
- Business module enforcement.
- Phase F audit writer.
- Frontend.
- V0004/U0004.
- Production migration.
- Production seed/bootstrap.
- AD/LDAP.
- Multi-permission any-of/all-of behavior.

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

## 9. Open Project Owner decisions

**OD-E-B-01:**
Confirm E-B migrates only D-B Security Administration APIs to shared RequirePermission enforcement.

**OD-E-B-02:**
Confirm Organization APIs remain out of scope until canonical permission codes are added to permission-catalog.md.

**OD-E-B-03:**
Confirm migration must preserve existing D-B authorization semantics exactly, including GLOBAL vs COMPANY behavior discovered during equivalence audit.

**OD-E-B-04:**
Confirm SecurityAdminService company-scope validation remains in service layer and is not replaced by the filter.

**OD-E-B-05:**
Confirm E-B does not introduce multi-permission any-of/all-of behavior.

**OD-E-B-06:**
Confirm no new permission codes are created in E-B.

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
