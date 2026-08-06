# Post Phase 1B.1-E Owner Decisions Acceptance

## Status
ACCEPTED — POST PHASE 1B.1-E OWNER DECISIONS APPROVED

## Accepted decision plan commit
ec43267fc73d4f976be46fe319943d7f3b855140

## Current accepted Phase E completion commit
a3e7a5ce2ae0e5b9219376215feb081bba5f17d4

## Accepted decisions

**OD-POST-E-01:**
Protect SystemController GET /api/v2/system/info with `[Authorize]` and `[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]`.

Accepted implementation direction:
- Add `[Authorize]` to `SystemController`.
- Add `[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]` to `SystemController`.
- Do not change route.
- Do not change response contract.
- Do not create a new permission code.
- Do not use `PermissionScope.Company`.
- Do not require `X-Company-Id`.
- Add API tests proving 401, 403, 200, and `X-Company-Id` does not cause 400.

**OD-POST-E-02:**
Backfill `docs/business/permission-catalog.md` with `SECURITY_ADMIN_MANAGE` and `SECURITY_AUDIT_VIEW`.

Accepted implementation direction:
- Update `permission-catalog.md` only.
- Do not change `PermissionCodes.cs`.
- Do not create migration.
- Do not seed production data.
- Do not implement audit endpoints.

**OD-POST-E-03:**
`SecurityControllerHelper.cs` CRLF/LF artifact remains deferred to a separate repository hygiene task.

Accepted implementation direction:
- Do not normalize line endings in SystemController or catalog backfill tasks.
- Do not stage `SecurityControllerHelper.cs` unless a separate hygiene task is explicitly authorized.

**OD-POST-E-04:**
Phase F planning may begin only after OD-POST-E-01 and OD-POST-E-02 are completed, or explicitly deferred again by Project Owner.

Accepted sequencing:
- First complete the small post-E implementation slice for SystemController protection and catalog backfill.
- Then run acceptance review.
- Then record completion.
- Then plan Phase F.

## Accepted next implementation slice
Post Phase 1B.1-E SystemController and permission catalog backfill.

## Implementation scope to authorize separately
- Modify `SystemController` only for `[Authorize]` and `[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]`.
- Modify `docs/business/permission-catalog.md` only to add `SECURITY_ADMIN_MANAGE` and `SECURITY_AUDIT_VIEW`.
- Add API tests for SystemController authorization behavior.

## Explicit non-authorization
- No implementation in this commit.
- No application code changes.
- No tests changed.
- No permission-catalog.md change in this commit.
- No PermissionCodes.cs change.
- No migration.
- No seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Next step
A separate implementation authorization prompt is required for SystemController protection and permission catalog backfill.
