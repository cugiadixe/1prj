# Phase 1B.1-G Project Owner Plan Acceptance

**Status**: ACCEPTED — PHASE 1B.1-G PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan commit**: `aa53ce32e650bc78bb51d9e7c2db1eb6fdb67bb9`
**Accepted baseline**: `70f9300a6eec956271c6670a0d927c3ff5c13b28`
**Accepted phase**: Phase 1B.1-G — Initial Admin Login & Force Password Change Verification

## Accepted scope
- Add `must_change_password` awareness to login/token flow.
- Enforce fail-closed guard for users whose token has `must_change_password=true`.
- Implement `POST /api/v2/auth/change-password`.
- Allow `must_change_password=true` users to change their own password.
- Apply existing password policy.
- Apply password history.
- Clear `must_change_password` after successful change.
- Revoke current refresh token family or current sessions after successful password change.
- Require fresh login after successful password change.
- Record `PASSWORD_CHANGED` audit event without secrets.
- Add unit, integration, and API tests.

## Accepted out-of-scope
- Admin reset password.
- Forgot password.
- Email/SMS OTP.
- Security Audit Read.
- `SECURITY_AUDIT_VIEW` enforcement.
- Security Admin UI.
- Frontend UI.
- Business modules.
- AD/LDAP.
- Production Key Vault / secret provider operationalization.
- Any schema migration unless implementation discovery proves it is absolutely required and separately approved.

## Accepted decisions

### DEC-1B-G-01 — Token and authorization strategy
- Use JWT claim `must_change_password=true`.
- Enforce a fail-closed guard for normal protected endpoints.
- Login is public and is not a protected endpoint exemption.
- Allow `must_change_password=true` authenticated users to call only auth-safe endpoints:
  - `POST /api/v2/auth/change-password`
  - `POST /api/v2/auth/logout`
  - `POST /api/v2/auth/refresh` only if the refreshed access token preserves `must_change_password=true` until password change completes.
- Do not allow `must_change_password=true` tokens to access business endpoints.
- Do not allow `must_change_password=true` tokens to access security management endpoints.
- Do not silently upgrade a `must_change_password=true` token into a normal token.
- Do not implement a separate restricted token type in this phase.

### DEC-1B-G-02 — Audit behavior
- `PASSWORD_CHANGED` audit event is required on successful password change.
- Audit payload must not contain plaintext password, password hash, token, secret, current password, or new password.
- Failed password-change audit may be deferred unless it can be implemented safely without expanding scope.

### DEC-1B-G-03 — Refresh/session behavior after password change
- After successful password change, revoke the current refresh token family or all current sessions created before the password change.
- Require the user to login again to receive a new access token without `must_change_password=true`.
- Do not silently upgrade old `must_change_password` tokens.

### DEC-1B-G-04 — Change-password endpoint behavior
- Endpoint: `POST /api/v2/auth/change-password`.
- Requires authenticated user.
- Allows an authenticated user with `must_change_password=true`.
- Only changes the current authenticated user's own password.
- Not an admin reset-password endpoint.
- Requires current/temporary password verification.
- Applies existing password policy.
- Applies password history.
- Clears `must_change_password` on success.
- Uses sanitized errors.
- Does not leak account existence, password validity details, hashes, tokens, or secrets.

## Required implementation tests
- Bootstrap admin can login with temporary password.
- Login response/token includes `must_change_password=true`.
- `must_change_password=true` token is blocked from normal protected endpoints.
- `must_change_password=true` token can call change-password endpoint.
- `must_change_password=true` token can logout.
- refresh preserves `must_change_password=true` until password change completes.
- Wrong current password fails safely.
- Password policy violation fails safely.
- Password history reuse fails safely.
- Successful password change clears `must_change_password`.
- Successful password change revokes current refresh/session state.
- Fresh login after successful password change returns normal access token.
- Old `must_change_password` token cannot access business endpoints.
- `PASSWORD_CHANGED` audit exists and contains no secrets.
- No `SECURITY_AUDIT_VIEW` enforcement is added.
- No admin reset/forgot-password scope is implemented.

## Implementation authorization
Phase 1B.1-G implementation may begin after this Project Owner plan acceptance is committed.
