# Phase 1B.1-C-B: Auth API Cookie CSRF Layer Implementation Evidence

**Status:** IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE

## 1. Baseline and Context

- **Expected Baseline Commit:** `951e6a33d0caab9fe2b3b8a54d09c455cc0817cf`
- **Scope:** Auth API layer (`AuthController`), cookie and CSRF service, JWT bearer validation skeleton.

## 2. Implemented Scope

The following endpoints were implemented in `AuthController`:
- `POST /api/v2/auth/login`
- `POST /api/v2/auth/refresh`
- `POST /api/v2/auth/logout`

## 3. Explicit Exclusions
- NO `GET /api/v2/auth/me` implemented.
- NO `POST /api/v2/auth/logout-all` implemented.
- NO frontend components modified or created.
- NO permission evaluation.
- NO role/admin-group authorization middleware beyond what is necessary to wire authenticated API behavior.
- NO database migrations created (V0004/U0004 are untouched).

## 4. Endpoint Behavior

### A. Login
- Validates password via `IAuthenticationAccountService`.
- Success issues JWT access token in the response body.
- Sets refresh token as `RefreshToken` HttpOnly Secure Strict cookie on `/api/v2/auth`.
- Issues a CSRF token as `X-CSRF-TOKEN` cookie and `X-CSRF-Token` response header.
- Does not require CSRF token since it's the beginning of a session.

### B. Refresh
- Requires valid `RefreshToken` cookie.
- Requires CSRF token matching between `X-CSRF-Token` header and `X-CSRF-TOKEN` cookie.
- Rotates refresh cookie securely.
- Rotates CSRF token cookie and header.
- Returns new access token in the response body.

### C. Logout
- Requires valid CSRF token if the refresh cookie is present.
- Generic success is returned regardless of token state.
- Revokes the current family/session.
- Deletes both `RefreshToken` and `X-CSRF-TOKEN` cookies.

## 5. Cookie and CSRF Details
- **Correction**: The `__Host-` prefix was removed because the Project Owner-approved Path is `/api/v2/auth` and `__Host-` strictly requires `Path=/`.
- **Refresh Cookie**: Name `RefreshToken`, Path `/api/v2/auth`, `HttpOnly=true`, `Secure=true`, `SameSite=Strict`, `Domain` omitted.
- **CSRF Token**: Double-submit pattern. `X-CSRF-TOKEN` cookie is set to `HttpOnly=false`, `Secure=true`, `SameSite=Strict`, `Path=/api/v2/auth`, `Domain` omitted. Validated against `X-CSRF-Token` header in constant-time.

## 6. ProblemDetails Mapping
- **Invalid Credentials**: Returns 401 Unauthorized with generic "Authentication Failed" without enumerating user existence.
- **Refresh Failure**: Returns 401 Unauthorized with generic session invalid message.
- **Account Locked**: Returns 403 Forbidden with generic Access Denied message.
- **CSRF Failure**: Returns 403 Forbidden with explicit CSRF message but no sensitive material exposed.

## 7. Database Safety and Tests
- All API tests executed against `PTKD_TEST_PHASE1A2`.
- DB queries for account lookup during tests are strictly isolated.
- PTKD_DEV was NOT connected.
- Tests confirm expected cookie issuance, generic problem mapping, and CSRF protection correctness.

## 8. Remaining C-C Work
- Fully wire protected-request stamp validation in a dedicated authorization policy/filter.
- This requires extracting the `sub` (UserId) and `security_stamp` claims from the JWT, querying the database to verify the account is ACTIVE, employment status is eligible, the `security_stamp` matches, and the token was issued after `sessions_invalidated_at`.

## 9. Changed Files
- `src/backend/PTKD.Api/Controllers/AuthController.cs` [NEW]
- `src/backend/PTKD.Api/Auth/Models/AuthModels.cs` [NEW]
- `src/backend/PTKD.Api/Security/CsrfTokenService.cs` [NEW]
- `src/backend/PTKD.Infrastructure/Persistence/TokenSessionDbContextFactory.cs` [NEW]
- `tests/backend/PTKD.ApiTests/AuthControllerTests.cs` [NEW]
- `src/backend/PTKD.Api/Program.cs` [MODIFIED]
- `src/backend/PTKD.Api/PTKD.Api.csproj` [MODIFIED - Package added]
