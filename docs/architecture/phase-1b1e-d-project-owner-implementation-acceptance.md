# Phase 1B.1-E-D Project Owner Implementation Acceptance

## Status
ACCEPTED — PHASE 1B.1-E-D IMPLEMENTATION COMPLETE

## Accepted implementation commit
43db8c55162571e19165ad288bc780e53e60e499

## Plan acceptance commit
20a59fc2771c38aa205b3a5bd5181945365232bf

## Plan commit
3653ceeb6311e663a58c283c87df0f1e91f0885d

## Authoritative chain
```
3653ceeb6311e663a58c283c87df0f1e91f0885d   Prepare Phase 1B.1-E-D user assignments enforcement plan
  → 20a59fc2771c38aa205b3a5bd5181945365232bf  Record Project Owner acceptance of Phase 1B.1-E-D plan
  → 43db8c55162571e19165ad288bc780e53e60e499  Implement Phase 1B.1-E-D user assignments enforcement
```

## Accepted scope
- Protected UserAssignmentsController with `[Authorize]`.
- Protected UserAssignmentsController with `[RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)]`.
- Applied protection at class level so all 7 mutation actions are covered:
  - AssignCompany
  - AssignDepartment
  - ChangePrimaryCompany
  - ChangePrimaryDepartment
  - CloseCompanyAssignment
  - SameCompanyDepartmentTransfer
  - CrossCompanyTransfer
- Reused existing ORGANIZATION_USER_MANAGE permission code (OD-E-D-01).
- Used PermissionScope.Global; no X-Company-Id requirement (OD-E-D-02).
- Added UserAssignmentsPermissionEnforcementTests covering all 7 actions unauthenticated (401), no-permission (403), SECURITY_ADMIN_MANAGE rejected (403), X-Company-Id ignored (not 400), representative mutation succeeds (204).
- Preserved existing route attributes unchanged.
- Preserved existing action names unchanged.
- Preserved existing request DTOs unchanged.
- Preserved existing response contracts unchanged.
- Preserved existing application service behavior after authorization succeeds.
- Preserved JWT behavior; no permissions added to JWT (OD-E-D-06).

## Accepted test evidence
- Targeted UserAssignments tests: 12 passed, 0 failed.
- Targeted Organization tests: 65 passed, 0 failed.
- Targeted Security tests: 41 passed, 0 failed.
- Build: 0 warnings, 0 errors.
- UnitTests: 97 passed, 0 failed.
- IntegrationTests: 147 passed, 0 failed.
- ApiTests: 148 passed, 0 failed.
- DatabaseSafety: 17 passed, 0 failed.

## Security/static review evidence
- No Exception.ToString exposure.
- No StackTrace exposure.
- No unauthorized AllowSelf bypass.
- UserAssignmentsController has RequirePermission.
- UserAssignmentsController does not use PermissionScope.Company.
- UserAssignmentsController does not use SECURITY_ADMIN_MANAGE.
- UserAssignmentsController does not require X-Company-Id.
- ORGANIZATION_USER_MANAGE exists in permission catalog and PermissionCodes.

## Explicit exclusions
- No new permission code.
- No PermissionScope.Company.
- No X-Company-Id requirement.
- No SECURITY_ADMIN_MANAGE for UserAssignmentsController.
- No read/manage split.
- No permission claims added to JWT.
- No route/action/DTO contract changes.
- No V0004/U0004 migration.
- No production migration.
- No production seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No SystemController change.
- No AD/LDAP.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Known deferred items
- SystemController `GET /api/v2/system/info` remains deferred under OD-E-D-08 and requires separate Project Owner decision.
- SECURITY_ADMIN_MANAGE and SECURITY_AUDIT_VIEW catalog backfill remains deferred under OD-E-D-09.
- SecurityControllerHelper.cs may appear as a CRLF/LF 0-content-diff working-tree artifact in some environments; it is not part of this acceptance.

## Known next step
Run a final Post Phase 1B.1-E completion review before planning Phase F or any additional enforcement/documentation hygiene slice.
