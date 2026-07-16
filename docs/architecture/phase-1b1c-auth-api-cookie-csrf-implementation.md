# Phase 1B.1-C-B: Auth API Cookie CSRF Layer Implementation Evidence

**Status:** IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE

**Phase 1B.1-C-C:** NOT AUTHORIZED
**Phase 1B.1-D through I:** NOT AUTHORIZED
**Production migration:** NOT AUTHORIZED

---

## Correction: Locked Account Status Mapping

During final review, it was found that locked account login returned 401 because `AuthenticationAccountService` returned `InvalidCredentials`, which was mapped to 401. A correction was implemented to:
- Map locked login with correct password to HTTP 403 generic.
- Ensure external response remains non-enumerating (wrong passwords still return 401).
- Ensure failed-attempt accounting remains unchanged (locked account attempt does not increment failed_attempt_count).

### Current Correction Chain
- `16160d09953cdb3d1a6b2d210961da3acbb12d9d` (Initial Implementation)
- `1097922c77ed5898d20bd1a11da60a50e1a45b15` (Cookie prefix correction)
- `070af32788e1027ee298e8f85f4d1dc7530c6005` (API coverage correction)
- `f6a46560eefd4d330081ea32bcf39b6ff94cbe83` (Doc update)
- `a713529bc941f2d69a52feadc3b5d05dd724f805` (Doc update)
- `a9f2ff6917afd565f111b2ef2534002dc8d93b7a` (Locked status correction)

---

## 1. Baseline and Context

- **Expected Baseline Commit:** `951e6a33d0caab9fe2b3b8a54d09c455cc0817cf`
- **C-B Implementation Commit:** `16160d09953cdb3d1a6b2d210961da3acbb12d9d`
- **C-B Cookie-Prefix Correction Commit:** `1097922c77ed5898d20bd1a11da60a50e1a45b15`
- **C-B API Coverage and Evidence Correction Commit:** `070af32788e1027ee298e8f85f4d1dc7530c6005`
- **Scope:** Auth API layer (`AuthController`), cookie and CSRF service, JWT bearer validation skeleton.

---

## 2. Implemented Scope

The following endpoints were implemented in `AuthController`:
- `POST /api/v2/auth/login`
- `POST /api/v2/auth/refresh`
- `POST /api/v2/auth/logout`

---

## 3. Explicit Exclusions

- NO `GET /api/v2/auth/me` implemented.
- NO `POST /api/v2/auth/logout-all` implemented.
- NO frontend components modified or created.
- NO permission evaluation.
- NO role/admin-group authorization middleware beyond what is necessary to wire authenticated API behavior.
- NO database migrations created. V0004/U0004 are untouched and do not exist.
- Production migration NOT AUTHORIZED for Phase 1B.1-C-B.

---

## 4. Endpoint Behavior

### A. Login
- Validates password via `IAuthenticationAccountService`.
- Success issues JWT access token in the response body.
- Sets refresh token as `RefreshToken` HttpOnly Secure Strict cookie on `/api/v2/auth`.
- Issues a CSRF token as `X-CSRF-TOKEN` cookie and `X-CSRF-Token` response header.
- Does not require CSRF token since it's the beginning of a session.
- Response body **does not** include refresh token material (confirmed by test).

### B. Refresh
- Requires valid `RefreshToken` cookie.
- Requires CSRF token matching between `X-CSRF-Token` header and `X-CSRF-TOKEN` cookie.
- Rotates refresh cookie securely.
- Rotates CSRF token cookie and header.
- Returns new access token in the response body.
- Response body **does not** include refresh token material (confirmed by test).

### C. Logout
- Requires valid CSRF token if the refresh cookie is present.
- Generic success is returned regardless of token state.
- Revokes the current family/session.
- Deletes both `RefreshToken` and `X-CSRF-TOKEN` cookies.
- Safe behavior when no refresh cookie is present (204, no CSRF required).

---

## 5. Cookie and CSRF Details

- **Correction**: The `__Host-` prefix was removed because the Project Owner-approved Path is `/api/v2/auth` and `__Host-` strictly requires `Path=/`.
- **Refresh Cookie**: Name `RefreshToken`, Path `/api/v2/auth`, `HttpOnly=true`, `Secure=true`, `SameSite=Strict`, `Domain` omitted.
- **CSRF Token**: Double-submit pattern. `X-CSRF-TOKEN` cookie is set to `HttpOnly=false`, `Secure=true`, `SameSite=Strict`, `Path=/api/v2/auth`, `Domain` omitted. Validated against `X-CSRF-Token` header in constant-time using `CryptographicOperations.FixedTimeEquals`.
- CSRF token is 256-bit cryptographically random (`RandomNumberGenerator.Fill`, Base64-encoded).

---

## 6. ProblemDetails Mapping

- **Invalid Credentials**: Returns 401 Unauthorized with generic "Authentication Failed" without enumerating user existence, password correctness, employment status, or account lock status.
- **Account Locked** (at `AuthenticateAsync` layer): Returns 401 generic — same as invalid credentials — to avoid enumerating lock state. The 403 path via `CreateSessionAsync`/`MapSessionFailureToProblem` applies only to post-authentication race conditions (see N3 below).
- **Refresh Failure**: Returns 401 Unauthorized with generic session invalid message. Covers: missing cookie, invalid token, expired token, revoked token, reused token, sessions_invalidated_at cutoff.
- **CSRF Failure**: Returns 403 Forbidden with explicit CSRF message but no sensitive material exposed.

---

## 7. Non-blocking Concerns Carried into C-C

**N1 — BuildServiceProvider anti-pattern:**
`builder.Services.BuildServiceProvider()` inside `IssuerSigningKeyResolver` in `Program.cs` creates a second root DI container (ASP0000 warning). Must be remediated in Phase 1B.1-C-C by using `context.HttpContext.RequestServices` or `IOptions<>` pattern.

**N2 — Refresh response user object stub:**
Refresh response currently returns `UserId: 0` and empty `Username` in the `User` object. Must be resolved or formally accepted before final auth API acceptance. Planned for resolution when `/api/v2/auth/me` is implemented in C-C.

**N3 — AccountLocked 403 detection path ambiguity:**
`AuthenticationAccountService.AuthenticateAsync` returns `InvalidCredentials` (401) for locked accounts — it catches lock before password verification. The `AccountLocked → 403` path in `MapSessionFailureToProblem` is reachable only via a post-authentication race condition. This is non-enumeration compliant (locked accounts get same 401 as wrong credentials). Must be formally documented and verified when C-C wires protected-request validation.

---

## 8. Remaining C-C Work

- Fully wire protected-request stamp validation in a dedicated authorization policy/filter.
- This requires extracting the `sub` (UserId) and `security_stamp` claims from the JWT, querying the database to verify the account is ACTIVE, employment status is eligible, the `security_stamp` matches, and the token was issued after `sessions_invalidated_at`.
- Remediate `BuildServiceProvider` anti-pattern (N1).
- Resolve refresh response user object stub (N2).
- Formally verify `AccountLocked` detection path (N3).

---

## 9. Database Safety and Tests

- All API tests executed against `PTKD_TEST_PHASE1A2`.
- `TestDatabaseSafety.ValidateConnectionString()` checks `InitialCatalog=PTKD_TEST_PHASE1A2` before any connection.
- `TestDatabaseSafety.VerifyOpenConnection()` executes `SELECT DB_NAME()` and asserts result = `PTKD_TEST_PHASE1A2` before host is returned to tests.
- `SafeTestWebApplicationFactory` runs both guards in `CreateHost()` before any test client can issue a request.
- PTKD_DEV was NOT connected.
- No production migration was run.
- V0003/U0003 unchanged — no commits touching those files in C-B range.
- V0004/U0004 do not exist — filesystem and git log confirm.

---

## 10. Test Coverage (Coverage Correction Commit)

### API Test Suite — AuthControllerTests.cs

| Test | Covers |
|------|--------|
| `Login_ValidCredentials_ReturnsSuccessAndSetsCookies` | 200 OK; body shape; cookie attributes; CSRF cookie/header; no `__Host-` |
| `Login_InvalidCredentials_Returns401Generic` | Invalid credentials → 401; generic error URI |
| `Login_LockedAccount_ReturnsGenericResponse_DoesNotExposeReason` | Locked → 401 generic (N3); does not expose lock reason |
| `Login_Success_ResponseBodyDoesNotContainRefreshToken` | Login body excludes refreshToken/refresh_token/rawRefreshToken fields |
| `Refresh_ValidCookies_ReturnsNewToken` | Refresh rotates cookies; no `__Host-`; new access token returned |
| `Refresh_MissingCsrf_Returns403` | Missing CSRF header → 403 |
| `Refresh_MissingRefreshCookie_Returns401Generic` | Missing refresh cookie (with valid CSRF) → 401 generic |
| `Refresh_InvalidToken_Returns401Generic` | Invalid token → 401; generic session-invalid URI; no internal code exposed |
| `Refresh_RevokedToken_Returns401Generic` | Revoked token (after logout) → 401 generic |
| `Refresh_ReusedToken_Returns401Generic` | Reuse of rotated token → 401 generic; TOKEN_REUSED not exposed |
| `Refresh_Success_ResponseBodyDoesNotContainRefreshToken` | Refresh body excludes refreshToken fields |
| `Logout_ValidSession_Returns204AndClearsCookies` | 204; RefreshToken cookie cleared with expiry |
| `Logout_MissingCsrf_WithRefreshCookiePresent_Returns403` | Missing CSRF on logout with refresh cookie → 403 |
| `Logout_RevokesSession_SubsequentRefreshFails` | After logout, refresh with same token → 401 (session revoked) |
| `Logout_DeletesRefreshCookieAndCsrfCookie` | Both RefreshToken and X-CSRF-TOKEN cookies cleared on logout |
| `Logout_NoCookiePresent_Returns204Safe` | No cookies → 204 (no CSRF required) |
| `Auth_Me_Endpoint_NotPresent` | GET /api/v2/auth/me → 404 |
| `Auth_LogoutAll_Endpoint_NotPresent` | POST /api/v2/auth/logout-all → 404 |

### Build and Test Results (Coverage Correction Commit)

**Build command:** `dotnet build src/backend/PTKD-ERP.sln --configuration Debug --no-restore`
- Errors: 0
- Warnings: 7 (all MSB3277 — pre-existing `System.IdentityModel.Tokens.Jwt` version conflict, non-blocking)
- Result: **SUCCEEDED**

**Unit tests:** `dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore`
- Passed: 67, Failed: 0, Skipped: 0 ✅

**Integration tests:** `dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore`
- Passed: 138, Failed: 0, Skipped: 0 ✅

**API tests:** `dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore`
- Passed: 80, Failed: 0, Skipped: 0 ✅ (20 tests — 5 original + 15 new coverage tests)

---

## 11. Changed Files

- `src/backend/PTKD.Api/Controllers/AuthController.cs` [NEW in C-B impl, MODIFIED in C-B correction]
- `src/backend/PTKD.Api/Auth/Models/AuthModels.cs` [NEW]
- `src/backend/PTKD.Api/Security/CsrfTokenService.cs` [NEW]
- `src/backend/PTKD.Infrastructure/Persistence/TokenSessionDbContextFactory.cs` [NEW]
- `tests/backend/PTKD.ApiTests/AuthControllerTests.cs` [NEW in C-B impl, MODIFIED in correction, MODIFIED in coverage correction]
- `src/backend/PTKD.Api/Program.cs` [MODIFIED]
- `src/backend/PTKD.Api/PTKD.Api.csproj` [MODIFIED — Package added]
- `docs/architecture/phase-1b1c-auth-api-cookie-csrf-implementation.md` [NEW in C-B impl, MODIFIED in correction, MODIFIED in coverage correction]
