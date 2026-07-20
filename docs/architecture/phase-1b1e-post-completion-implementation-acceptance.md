# Post Phase 1B.1-E SystemController and Catalog Backfill Implementation Acceptance

## Status
ACCEPTED — POST PHASE 1B.1-E SYSTEMCONTROLLER PROTECTION AND CATALOG BACKFILL COMPLETE

## Accepted implementation commit
0346ecb032e3847aace1508139d265a6e79e1979

## Decision acceptance commit
2f73584cccf121d501a593987f4a1dd87883d24d

## Decision plan commit
ec43267fc73d4f976be46fe319943d7f3b855140

## Phase E completion acceptance commit
a3e7a5ce2ae0e5b9219376215feb081bba5f17d4

## Accepted scope

- Protected `SystemController` `GET /api/v2/system/info` with `[Authorize]`.
- Protected `SystemController` with `[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]`.
- Preserved `SystemController` route `api/v2/system`.
- Preserved `GetInfo` action behavior after successful authorization.
- Preserved response contract: `appName`, `version`, `environment`.
- Backfilled `docs/business/permission-catalog.md` with `SECURITY_ADMIN_MANAGE`.
- Backfilled `docs/business/permission-catalog.md` with `SECURITY_AUDIT_VIEW`.
- Documented `SECURITY_AUDIT_VIEW` as reserved read-only audit view permission with endpoint enforcement deferred.
- Added `SystemControllerPermissionEnforcementTests`.
- Updated `HealthCheckTests` so correlation-id middleware tests use `/api/v2/health` instead of now-protected `/api/v2/system/info`.

## Accepted test evidence

- Targeted System tests: 5 passed, 0 failed.
- Targeted HealthCheck tests: 4 passed, 0 failed.
- Targeted Security tests: 44 passed, 0 failed.
- Targeted UserAssignments tests: 12 passed, 0 failed.
- Targeted Organization tests: 65 passed, 0 failed.
- Build: 0 warnings, 0 errors.
- UnitTests: 97 passed, 0 failed.
- IntegrationTests: 147 passed, 0 failed.
- ApiTests: 153 passed, 0 failed.
- DatabaseSafety: 17 passed, 0 failed.

## Accepted security and static review evidence

- `SystemController` has `[RequirePermission]`.
- `SystemController` uses `PermissionCodes.SecurityAdminManage`.
- `SystemController` uses `PermissionScope.Global`.
- `SystemController` does not use `PermissionScope.Company`.
- `SystemController` does not require or parse `X-Company-Id`.
- `SECURITY_ADMIN_MANAGE` exists in `PermissionCodes.cs` and `permission-catalog.md`.
- `SECURITY_AUDIT_VIEW` exists in `PermissionCodes.cs` and `permission-catalog.md`.
- `HealthCheckTests` no longer depend on `/api/v2/system/info` for public middleware checks.
- No `Exception.ToString()` exposure.
- No `StackTrace` exposure.
- No unauthorized `AllowSelf` bypass.

## Explicit exclusions

- No `PermissionCodes.cs` change.
- No new permission code.
- No `PermissionScope.Company`.
- No `X-Company-Id` requirement.
- No permissions added to JWT.
- No `AuthController` behavior change.
- No `SystemController` response contract change.
- No migration.
- No production seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Known deferred items

- `SECURITY_AUDIT_VIEW` is cataloged but not yet enforced by a live endpoint; endpoint enforcement remains deferred.
- Phase F Audit Writer / Initial Admin Bootstrap is not implemented and still requires a separate plan and Project Owner authorization.
- `SecurityControllerHelper.cs` may appear as a CRLF/LF 0-content-diff working-tree artifact; deferred to a separate repository hygiene task per OD-POST-E-03.

## Next step
Run a final post-E closure check, then prepare Phase F planning only after Project Owner authorization.
