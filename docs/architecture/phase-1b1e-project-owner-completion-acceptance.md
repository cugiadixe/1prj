# Phase 1B.1-E Project Owner Completion Acceptance

## Status
ACCEPTED — PHASE 1B.1-E FUNCTIONALLY COMPLETE

## Current accepted HEAD
15c78e70096dcdcc3534c123165cbb04a9a2da3e

## Accepted scope summary
Phase 1B.1-E connected authenticated requests to shared permission enforcement for protected Security Administration and Organization APIs.

## Accepted slice summary
- E-A accepted: shared enforcement foundation.
- E-B accepted: Security Administration APIs migrated to shared RequirePermission enforcement.
- E-C accepted: UsersController, DepartmentsController, CompaniesController protected with approved Organization permissions.
- E-D accepted: UserAssignmentsController protected with ORGANIZATION_USER_MANAGE and PermissionScope.Global.

## Controller coverage accepted
- AuthController remains public by accepted design for login/refresh/logout.
- Security Administration controllers are protected by [Authorize] and [RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)].
- Organization controllers are protected by [Authorize] and approved ORGANIZATION_* permissions.
- UserAssignmentsController is protected by [Authorize] and [RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)].
- SystemController GET /api/v2/system/info remains explicitly deferred under OD-E-D-08 and is not a Phase E blocker.

## Accepted completion findings
- No unauthenticated business mutation endpoints remain in Phase E scope.
- No authenticated-only but permission-unprotected business endpoints remain in Phase E scope.
- No controller uses PermissionScope.Company.
- No Organization controller uses SECURITY_ADMIN_MANAGE.
- No Security controller uses ORGANIZATION_* permissions.
- No X-Company-Id parsing exists in controllers.
- X-Company-Id remains infrastructure/foundation behavior only for COMPANY-scoped enforcement.
- No permissions are added to JWT.
- No Phase F audit writer was implemented.
- No migration or seed/bootstrap was added.
- No frontend or business module implementation was added.

## Accepted smoke review evidence
- UserAssignments, Organization, and Security targeted smoke tests: 117 passed, 0 failed.
- Static checks clean:
  - No Exception.ToString exposure.
  - No StackTrace exposure.
  - No unauthorized AllowSelf bypass.
  - No unexpected X-Company-Id controller parsing.
  - No PermissionScope.Company in controllers.

## Known deferred items
- OD-E-D-08 follow-up: SystemController GET /api/v2/system/info disposition requires separate Project Owner decision.
- OD-E-D-09 follow-up: SECURITY_ADMIN_MANAGE and SECURITY_AUDIT_VIEW catalog backfill is documentation hygiene and requires separate authorization.
- Company-scoped enforcement remains deferred until entity-company ownership validation is explicitly designed.
- SecurityControllerHelper.cs may appear as a CRLF/LF 0-content-diff working-tree artifact in some environments; it is not part of Phase E completion.

## Explicit non-authorization
- This completion acceptance does not authorize Phase F implementation.
- This completion acceptance does not authorize SystemController changes.
- This completion acceptance does not authorize permission catalog backfill.
- This completion acceptance does not authorize company-scoped enforcement.
- This completion acceptance does not authorize migration, seed/bootstrap, frontend, business module, AD/LDAP, production deployment, tag, or push.

## Next required owner decisions before implementation
1. Decide SystemController GET /api/v2/system/info disposition:
   - keep public;
   - remove environment information and keep public;
   - protect with [Authorize];
   - or protect with [Authorize] + SECURITY_ADMIN_MANAGE.
2. Decide whether to authorize documentation-only catalog backfill for SECURITY_ADMIN_MANAGE and SECURITY_AUDIT_VIEW.
3. Decide whether to proceed to Phase F planning for Audit Writer / Initial Admin Bootstrap.
