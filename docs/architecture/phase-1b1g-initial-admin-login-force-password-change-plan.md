# Phase 1B.1-G — Initial Admin Login & Force Password Change Plan

## 1. Goal
**Status**: PHASE 1B.1-G PLAN ACCEPTED — SEE [phase-1b1g-project-owner-plan-acceptance.md](phase-1b1g-project-owner-plan-acceptance.md)

Implement the Initial Admin Login and Force Password Change flow. This ensures that the bootstrapped administrative account must change its temporary password before accessing any other business endpoints, fulfilling the security requirement that `must_change_password` blocks non-password operations.

## 2. Existing Implementation Status
- `AuthController.cs` handles `/api/v2/auth/login`, issuing a full JWT and Refresh Token.
- `AuthenticationAccountService.AuthenticateAsync` evaluates and returns the `MustChangePassword` flag in its `AuthenticationAttemptResult`.
- `AuthenticationAccountService.ChangePasswordAsync` implements the business rules for password change (history check, policy validation, rehash, session invalidation).
- `PTKD.Bootstrap` successfully provisions the initial admin with `must_change_password = true`.

## 3. Gaps to Address
- **Login Enforcement**: `AuthController.cs` currently ignores the `MustChangePassword` flag and issues a standard, unrestricted JWT. 
- **Change Password Endpoint**: A `POST /api/v2/auth/change-password` endpoint is missing from `AuthController.cs`.
- **Authorization Block**: There is no middleware or policy ensuring that a user with `must_change_password = true` cannot access business endpoints.
- **Audit Integration**: `AuthenticationAccountService` currently does not integrate `IAuditWriter` to record `PASSWORD_CHANGED` events.

## 4. Proposed Scope and Boundaries

### In Scope
- Login response/claims update to include `must_change_password=true`.
- Authorization guard to block `must_change_password=true` tokens from accessing normal protected endpoints.
- Change-password endpoint (`POST /api/v2/auth/change-password`) and service integration.
- Password policy and password history enforcement.
- Refresh token / session revocation after successful password change.
- `PASSWORD_CHANGED` audit record on success.
- Unit, integration, and API tests for all added behavior.

### Out of Scope
- Admin reset password functionality.
- Forgot password functionality.
- Email/SMS OTP generation or verification.
- Security Audit Read endpoint (`SECURITY_AUDIT_VIEW`).
- Security Admin UI (Permission assignment screens).
- Frontend UI implementations.
- Business modules.
- AD/LDAP integrations.
- Production Key Vault integration.

## 5. Decisions Required from Project Owner

### DEC-1B-G-01 Token / authorization strategy
**Recommended Decision:**
- Use JWT claim `must_change_password=true`.
- Enforce fail-closed guard for normal protected endpoints.
- Login is public and is not a protected endpoint exemption.
- Allow `must_change_password=true` authenticated user to call:
  - `POST /api/v2/auth/change-password`
  - `POST /api/v2/auth/logout`
  - `POST /api/v2/auth/refresh` only if the refreshed access token preserves `must_change_password=true` until password change completes.
- Do not allow `must_change_password=true` token to access business endpoints.
- Do not allow `must_change_password=true` token to access security management endpoints.
- Do not silently upgrade `must_change_password=true` token into a normal token.
- Do not implement a separate restricted token type unless Project Owner explicitly changes this decision later.

### DEC-1B-G-02 Audit behavior
**Recommended Decision:**
- `PASSWORD_CHANGED` audit event is required on successful password change.
- Audit payload must not contain plaintext password, hash, token, secret, or current/new password.
- Failed password change audit is optional only if it can be implemented without expanding scope; otherwise defer.

### DEC-1B-G-03 Refresh token / session behavior after password change
**Recommended Decision:**
- After successful password change, revoke current refresh token family or all current sessions created before password change.
- Require login again to receive a new token without `must_change_password=true`.
- Do not silently upgrade the old `must_change_password` token into a normal business token.

### DEC-1B-G-04 Change password endpoint behavior
**Recommended Decision:**
- **Endpoint**: `POST /api/v2/auth/change-password`.
- Requires authenticated user.
- Allows authenticated user with `must_change_password=true`.
- Only changes the current authenticated user's own password.
- Not an admin reset-password endpoint.
- Requires current/temporary password verification.
- Applies existing password policy.
- Applies password history.
- Clears `must_change_password` on success.
- Uses sanitized errors.
- Does not leak account existence, password validity details, hashes, tokens, or secrets.

## 6. Required Tests

**Explicit tests to be added:**
- Bootstrap admin can login with temporary password.
- Login response/token includes `must_change_password=true`.
- `must_change_password=true` token is blocked from normal protected endpoints.
- `must_change_password=true` token can call change-password endpoint.
- Wrong current password fails safely.
- Password policy violation fails safely.
- Password history reuse fails safely.
- Successful change clears `must_change_password`.
- Successful change revokes old refresh/session state or requires re-login.
- Old `must_change_password` token cannot access business endpoints after password change.
- `PASSWORD_CHANGED` audit exists and contains no secrets.
- No `SECURITY_AUDIT_VIEW` enforcement added.
- No admin reset/forgot password scope.
