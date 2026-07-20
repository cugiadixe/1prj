# Phase 1B.1-E Project Owner Plan Acceptance

## Status
PHASE 1B.1-E PLAN ACCEPTED — E-A IMPLEMENTATION NOT YET STARTED

## Accepted plan commit
e40edb0e5170d70ebbeea6a8a3068e1ab0463cf4

## Current accepted baseline
7eb6f427f7dbfbbe2f88460077a824165e162526

## Accepted next slice
Phase 1B.1-E-A — Company Context and Permission Enforcement Foundation

## Project Owner Decisions

**OD-E-01:**
Use custom endpoint attributes / metadata, for example [RequirePermission("CODE", Scope = COMPANY/GLOBAL)], with one shared enforcement filter/handler.
Do not use a centralized route-permission registry in Phase E-A.

**OD-E-02:**
Require X-Company-Id only for endpoints explicitly marked as COMPANY-scoped.
Do not require X-Company-Id for all authenticated non-auth endpoints.

**OD-E-03:**
Keep D-B Security Administration APIs manually enforced in Phase E-A.
Do not migrate D-B APIs to the shared enforcement mechanism yet.
Migration can be evaluated in a later E-B slice after the foundation is proven.

**OD-E-04:**
For GLOBAL permission endpoints, X-Company-Id is optional and ignored.
Do not reject it in Phase E-A.

**OD-E-05:**
Initial endpoint set for Phase E-A is enforcement foundation plus dedicated test endpoints only.
Do not apply enforcement broadly to existing organization/security APIs in E-A.
Existing C/D tests must not regress.

**OD-E-06:**
Malformed or missing required X-Company-Id returns 400 with a sanitized specific error code.
No raw internal details.

**OD-E-07:**
Introduce reusable permission metadata/attribute:
[RequirePermission("PERMISSION_CODE", Scope = PermissionScope.COMPANY/GLOBAL)]
or equivalent strongly typed metadata.
Permission codes must remain repo-controlled constants.

**OD-E-08:**
Defer multi-permission support.
Phase E-A supports exactly one required permission per endpoint.
Any-of/all-of behavior requires a later decision.

## Authorized Scope for Phase E-A
- Implement company context extraction for endpoints marked COMPANY-scoped.
- Implement reusable permission metadata/attribute.
- Implement shared enforcement filter/handler using IPermissionEvaluator.
- Return 401 for missing/invalid JWT through existing auth pipeline.
- Return 400 for missing/malformed required X-Company-Id.
- Return 403 for missing company access or missing permission.
- Add dedicated test-only endpoints for enforcement validation if needed.
- Keep auth endpoints excluded.
- Keep D-B APIs manually enforced in E-A.
- No production migration.
- No V0004/U0004.
- No frontend.
- No Phase F audit writer.
- No bootstrap/seed.
- No business module implementation.

## Explicit non-authorization
- No implementation in this commit.
- No broad migration of D-B APIs to shared enforcement in E-A.
- No Phase F audit writer.
- No frontend.
- No production seed/bootstrap.
- No V0004/U0004.
- No production migration.
- No business module implementation.

## Next step
E-A implementation requires a separate implementation authorization prompt.
