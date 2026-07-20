# Phase 1B.1-E-C Project Owner Implementation Acceptance

## Status
ACCEPTED — PHASE 1B.1-E-C IMPLEMENTATION COMPLETE

## Lineage correction commit
555ac9140eef81274a12adadcfeca136a4024e9f

## Accepted implementation commit
b97fbe1c92899b8d7539088cfaed32ebf98136c6

## Plan acceptance commit
0e11cd3e9ef763055f4a668aa4815425894761f8

## Plan commit
173b2215eac3bfc8077c716b7f5e7f009aa00e24

## Current reachable E-B acceptance anchor
4251e763617d97b97c868f89427a0cb762393f78

## Authoritative chain
```
4251e763617d97b97c868f89427a0cb762393f78   Record Project Owner acceptance of Phase 1B.1-E-B implementation
  → 173b2215eac3bfc8077c716b7f5e7f009aa00e24  Prepare Phase 1B.1-E-C organization API permission plan
  → 0e11cd3e9ef763055f4a668aa4815425894761f8  Record Project Owner acceptance of Phase 1B.1-E-C plan
  → b97fbe1c92899b8d7539088cfaed32ebf98136c6  Implement Phase 1B.1-E-C organization API enforcement
  → 555ac9140eef81274a12adadcfeca136a4024e9f  Record Phase 1B.1-E lineage correction
```

## Accepted scope
- Added canonical Organization permission codes:
  - ORGANIZATION_USER_MANAGE
  - ORGANIZATION_DEPARTMENT_MANAGE
  - ORGANIZATION_COMPANY_MANAGE
- Added approved permission codes to docs/business/permission-catalog.md.
- Added permission constants to src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs.
- Moved PermissionCodes.cs from the D-B Security controller namespace to the shared API security authorization namespace.
- Protected UsersController with ORGANIZATION_USER_MANAGE and PermissionScope.Global.
- Protected DepartmentsController with ORGANIZATION_DEPARTMENT_MANAGE and PermissionScope.Global.
- Protected CompaniesController with ORGANIZATION_COMPANY_MANAGE and PermissionScope.Global.
- Updated Organization API tests to authenticate with the approved Organization permissions.
- Added Organization permission enforcement tests covering 401, 403, and 200 behavior.
- Preserved D-B Security Administration behavior; D-B controller changes were only PermissionCodes namespace updates.
- Preserved JWT behavior; no permissions added to JWT.

## Accepted test evidence
- Targeted Organization tests: 65 passed, 0 failed.
- Targeted Users tests: 3 passed, 0 failed.
- Targeted Departments tests: 4 passed, 0 failed.
- Targeted Companies tests: 3 passed, 0 failed.
- Targeted Security tests: 40 passed, 0 failed.
- Build: 0 warnings, 0 errors.
- UnitTests: 97 passed, 0 failed.
- IntegrationTests: 147 passed, 0 failed.
- ApiTests: 136 passed, 0 failed.
- DatabaseSafety: 17 passed, 0 failed.

## Security/static review evidence
- No Exception.ToString exposure.
- No StackTrace exposure.
- No unauthorized AllowSelf bypass.
- Organization controllers have RequirePermission.
- Organization controllers do not use PermissionScope.Company.
- Organization controllers do not use SECURITY_ADMIN_MANAGE.
- Organization controllers do not require X-Company-Id.
- Security controllers do not use ORGANIZATION_* permissions.

## Explicit exclusions
- No X-Company-Id requirement introduced for Organization APIs.
- No PermissionScope.Company used for Organization APIs.
- No SECURITY_ADMIN_MANAGE used for Organization APIs.
- No read/manage split introduced.
- No permissions added to JWT.
- No V0004/U0004 migration.
- No production migration.
- No production seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No tag/push.

## Known notes
- PTKD-ERP-Master-Context.md referenced in external handoff was not found in the repository during review; this is not an implementation defect.
- A pre-existing CRLF/LF line-ending artifact may appear for SecurityControllerHelper.cs in some environments. It has zero content diff and is not part of this acceptance.

## Known next step
Post Phase 1B.1-E status review is required before planning the next slice. Do not start Phase F or additional enforcement without separate Project Owner decision.
