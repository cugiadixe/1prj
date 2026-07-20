# Post Phase 1B.1-E Owner Decisions Before Phase F

## Status
ACCEPTED — POST PHASE 1B.1-E OWNER DECISIONS APPROVED

## Baseline
- Current accepted HEAD: a3e7a5ce2ae0e5b9219376215feb081bba5f17d4
- Phase 1B.1-E completion acceptance recorded.
- Phase E functionally complete.
- No Phase F implementation started.

## Purpose
Record remaining owner decisions before Phase F planning or implementation.

## Decision group 1 — SystemController disposition

**OD-POST-E-01:**
SystemController GET /api/v2/system/info should be protected with:
`[Authorize]`
`[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]`

### Rationale
- The endpoint exposes system/environment information including `ASPNETCORE_ENVIRONMENT`.
- It is not part of the public auth flow (login/refresh/logout).
- Reusing `SECURITY_ADMIN_MANAGE` avoids creating a new permission code.
- Global scope is consistent with existing Security Administration enforcement.
- No `X-Company-Id` should be required.

### Current state
```csharp
[ApiController]
[Route("api/v2/system")]
public class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            appName = "PTKD ERP",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }
}
```

### Proposed state after acceptance
```csharp
[ApiController]
[Route("api/v2/system")]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
public class SystemController : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            appName = "PTKD ERP",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        });
    }
}
```

### Explicit implementation direction after acceptance
- Add `[Authorize]` and `[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]` to `SystemController`.
- Do not change route.
- Do not change response contract unless separately authorized.
- Do not create a new permission code.
- Do not use `PermissionScope.Company`.
- Do not require `X-Company-Id`.
- Add API tests proving:
  - unauthenticated request returns 401;
  - authenticated user without `SECURITY_ADMIN_MANAGE` returns 403;
  - authenticated user with `SECURITY_ADMIN_MANAGE` returns 200;
  - `X-Company-Id` is not required and does not cause 400.

---

## Decision group 2 — Permission catalog backfill

**OD-POST-E-02:**
Backfill `docs/business/permission-catalog.md` with:
- `SECURITY_ADMIN_MANAGE`
- `SECURITY_AUDIT_VIEW`

### Rationale
- Both constants exist in `PermissionCodes.cs` and are in active use or reserved.
- `SECURITY_ADMIN_MANAGE` is actively used by all 8 Security Administration controllers.
- `SECURITY_AUDIT_VIEW` exists as a reserved constant but is not yet used for enforcement on any endpoint.
- The catalog is the authoritative record of approved permission codes.
- This is documentation hygiene only — no code change, no migration, no seed.

### Proposed catalog additions after acceptance

| permission_code | module_code | action_code | data_scope | sensitive | delegable | Purpose |
|---|---|---|---|---:|---:|---|
| SECURITY_ADMIN_MANAGE | SECURITY | ADMIN_MANAGE | GLOBAL | Yes | No | Authorize Security Administration API access: Roles, AdminGroups, Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions. |
| SECURITY_AUDIT_VIEW | SECURITY | AUDIT_VIEW | GLOBAL | Yes | No | Reserved read-only audit view permission. Must NOT be used to authorize mutations. Enforcement deferred to Phase F or separately authorized audit endpoints. |

### Explicit implementation direction after acceptance
- Update `docs/business/permission-catalog.md` only.
- Do not change `PermissionCodes.cs`.
- Do not create migrations.
- Do not seed production data.
- Do not implement audit endpoints.

---

## Decision group 3 — Line-ending hygiene

**OD-POST-E-03:**
`SecurityControllerHelper.cs` CRLF/LF artifact is deferred to a separate repository hygiene task.

### Rationale
- It is a 0-content-diff local artifact (working-tree CRLF warning, no real line changes).
- It should not be mixed with security implementation or documentation decisions.
- No line-ending normalization is authorized in the SystemController or catalog backfill tasks.

---

## Decision group 4 — Phase F sequencing

**OD-POST-E-04:**
Phase F planning may begin only after the owner either:
- accepts and completes the SystemController/catalog backfill slice; or
- explicitly defers them again and authorizes Phase F planning independently.

### Rationale
- SystemController exposes environment information and is the only unenforced non-auth endpoint.
- Catalog gaps are a documentation integrity concern for an authorized auditor.
- Resolving both before Phase F avoids carrying forward known hygiene items into a new phase.

---

## Explicit non-authorization
- This decision plan does not authorize implementation.
- This decision plan does not authorize Phase F.
- This decision plan does not authorize migrations.
- This decision plan does not authorize seed/bootstrap.
- This decision plan does not authorize frontend or business module work.
- This decision plan does not authorize production deployment.
- This decision plan does not authorize tag or push.

## Recommended next step
Project Owner review and acceptance of OD-POST-E-01 through OD-POST-E-04.
