# Phase 1B.1-D-B Project Owner Plan Acceptance

## 1. Status

D-B PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## 2. Accepted Plan Commit

690f89f293f5e947a6164be5937badcf05ef6aa2

## 3. Baseline Accepted D-A Commit

5592dc5e7dce37ee2402efbb782db5225bfb49a0

## 4. Accepted D-B Plan Scope

- Permission catalog read API.
- Role management APIs (create, read, update, deactivate).
- Role-permission assignment APIs.
- Admin group management APIs (create, read, update, deactivate).
- Admin group permission assignment APIs.
- User role assignment APIs.
- User admin group assignment APIs.
- User individual ALLOW/DENY permission APIs.
- Department baseline permission APIs.
- Effective permissions read API.
- Authorization_Policy_State increment/cache invalidation after all mutations.
- Active permission validation with HTTP 422 for inactive permission usage.
- Manual per-action authorization checks using IPermissionEvaluator and SECURITY_ADMIN_MANAGE.

## 5. Project Owner Decisions

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

## 6. Explicit Non-Authorization

- D-B implementation has not started in this commit.
- Phase E middleware enforcement is not authorized.
- X-Company-Id middleware enforcement is not authorized.
- Phase F audit/bootstrap is not authorized.
- Frontend is not authorized.
- Seed/bootstrap is not authorized.
- V0004/U0004 is not authorized.
- Production migration is not authorized.
- Production deployment/enablement is not authorized.

## 7. Preserved WIP Note

A prior premature D-B WIP is preserved in git stash (`stash@{0}: WIP Phase 1B.1-D-B implementation before plan acceptance`) and is not part of this acceptance commit. The stash must not be popped or applied without a separate implementation authorization prompt.

## 8. Next Step

D-B implementation requires a separate implementation authorization prompt.
