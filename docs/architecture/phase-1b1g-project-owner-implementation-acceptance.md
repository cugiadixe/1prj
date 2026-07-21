# Phase 1B.1-G Project Owner Implementation Acceptance

**Status**: ACCEPTED — PHASE 1B.1-G IMPLEMENTATION COMPLETE

**Accepted implementation commit**: `d2c932725d902539726df6562ac0294658657c07`

**Accepted parent**: `247447dcdf1423a3a49e536a69537feb7ab62258`

**Accepted phase**: Phase 1B.1-G — Initial Admin Login & Force Password Change Verification

## Accepted implementation scope

- `must_change_password` awareness in login/token/refresh flow: login response and JWT now include `must_change_password=true` when the authenticated account requires a password change; refresh preserves this claim until password change completes.
- Fail-closed guard for `must_change_password=true` tokens: `MustChangePasswordAuthorizationFilter` blocks all normal protected endpoints with HTTP 403 when the claim is present; only the three auth-safe paths are allowed through.
- `POST /api/v2/auth/change-password`: endpoint implemented in `AuthController`; requires authenticated user including those with `must_change_password=true`.
- Self-service password change only: endpoint operates exclusively on the authenticated user's own account; no admin override path.
- Side-effect-free current password verification: `VerifyCurrentPasswordAsync` reads and verifies the current password without creating login sessions, tokens, or audit side effects.
- Password policy enforcement: existing `AuthenticationAccountPolicy.ValidatePassword` applied on new password before acceptance.
- Password history enforcement: existing history depth check applied; password reuse rejected.
- Clear `must_change_password` after success: `account.ReplacePassword(…, mustChangePassword: false, …)` clears the flag atomically with the password change.
- Persistent refresh/session invalidation after password change: `ISessionInvalidationService.Invalidate` is called on successful password change; old refresh tokens are revoked.
- Fresh login required after successful password change: the old access token carries `must_change_password=true` which the guard blocks; a new clean token requires re-authentication.
- `PASSWORD_CHANGED` audit without secrets: audit record contains `EventCode`, `EntityType`, `EntityId`, `Outcome`, `CorrelationId`, `ActorUserId`, `TargetUserId` only; no password, hash, token, security stamp, or raw request body.
- Transaction-aware `PASSWORD_CHANGED` audit using same connection and transaction: `SqlTransactionalAuditWriter` reuses the EF `DbConnection` and `DbTransaction` active at the time of the password change; if the audit INSERT fails the transaction rolls back and the password change does not persist (fail-closed, OD-F-04).
- Audit atomicity tests: `PasswordChangeAuditAtomicityTests` (231 lines) covers rollback visibility, post-rollback absence, commit persistence, sensitive-data rejection, null-argument guards, and no-op sensitive-data validation.

## Accepted exclusions

- No admin reset password.
- No forgot password.
- No email/SMS OTP.
- No Security Audit Read.
- No `SECURITY_AUDIT_VIEW` enforcement.
- No Security Admin UI.
- No frontend UI.
- No business modules.
- No AD/LDAP.
- No production Key Vault / secret provider operationalization.
- No schema migration.

## Accepted test evidence

- UnitTests: 119/119 passed.
- IntegrationTests: 168/168 passed.
- DatabaseSafety: 17/17 passed.
- ApiTests: 153/153 passed.
- PasswordChangeAuditAtomicityTests: 8/8 passed.
- Grand total: 465/465 passed.

## Implementation spot-check summary

| Requirement | Verified in |
|---|---|
| `must_change_password` claim on login | `AuthController.cs`, `TokenSessionResult.cs`, `JwtAccessTokenService.cs` |
| Fail-closed guard | `MustChangePasswordAuthorizationFilter.cs` — 403 with Problem Details |
| Auth-safe paths only | `AllowedPaths`: `/api/v2/auth/change-password`, `/api/v2/auth/logout`, `/api/v2/auth/refresh` |
| `POST /api/v2/auth/change-password` | `AuthController.ChangePassword` action |
| Side-effect-free verification | `VerifyCurrentPasswordAsync` — read-only context, no `SaveChangesAsync` |
| Policy enforcement | `AuthenticationAccountPolicy.ValidatePassword` called before hash |
| History enforcement | `IsPasswordReused` checks current hash and history depth |
| `must_change_password` cleared | `account.ReplacePassword(…, false, null, …)` |
| Session invalidation | `_sessionInvalidationService.Invalidate(account, utcNow)` |
| Transaction-aware audit | `SqlTransactionalAuditWriter.WriteAsync` reuses caller's `DbConnection`/`DbTransaction` |
| No secrets in audit | `record.ThrowIfContainsSensitiveData()` guard + audit record fields inspected |
| Audit atomicity tests | `PasswordChangeAuditAtomicityTests.cs` — 8 tests, 231 lines |
| No migration | Zero migration/rollback files in committed file set |
| No `PermissionCodes.cs` change | Confirmed absent from committed file set |
| No `permission-catalog.md` change | Confirmed absent from committed file set |
| No `SECURITY_AUDIT_VIEW` | Confirmed absent from all committed diffs |
| No admin reset / forgot password endpoint | `AdministratorResetPasswordAsync` pre-existed in service; no HTTP endpoint exposed |

## Acceptance conclusion

Phase 1B.1-G implementation is accepted as complete.

Project may proceed to Phase 1B.1-G closure review or next approved security planning step.
