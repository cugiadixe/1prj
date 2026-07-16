# Phase 1B.1-C Token and Session Lifecycle Implementation Plan

**Status**: ACCEPTED BY PROJECT OWNER — IMPLEMENTATION NOT AUTHORIZED

**Project Owner decisions**: RESOLVED FOR PLANNING
**Remaining blockers**: NONE FOR PLANNING
**Implementation**: NOT AUTHORIZED
**Phase 1B.1-D through I**: NOT AUTHORIZED
**Production migration**: NOT AUTHORIZED

## 1. Baseline Commit
- **HEAD**: `72502753ee5d1a02f3847261cbdfe59d4d06df34`
- **Parent**: `a2e381139bba61ddaf8d9097be7df0e0010d878f`

## 2. Accepted Prior Phases
- **Phase 1B.1-A**: Database foundation (ACCEPTED BY PROJECT OWNER).
- **Phase 1B.1-B**: Authentication account and password lifecycle (ACCEPTED BY PROJECT OWNER).

## 3. Scope
The scope of Phase 1B.1-C covers the implementation of the core token and session lifecycle logic:
1. **Login endpoint design**: Credential verification delegating to Phase 1B.1-B logic, returning JWT and Refresh Cookie.
2. **JWT access-token issuance**: Generating signed, short-lived JWTs.
3. **Refresh-token issuance**: Generating opaque 256-bit random tokens.
4. **Refresh-token rotation**: Exchanging valid refresh tokens for a new JWT and a new refresh token.
5. **Token family/session model**: Grouping linked refresh tokens by `family_id` to allow chain tracing.
6. **Hashed refresh-token storage only**: Storing only SHA-256 hashes of the opaque refresh tokens in `Refresh_Tokens`.
7. **Refresh-token reuse detection**: Detecting attempts to use an already `used_at` refresh token.
8. **Family revocation on reuse**: Instantly revoking all tokens sharing the same `family_id` upon reuse detection.
9. **Logout behavior**: Revoking the current token family and clearing the refresh cookie.
10. **Security-stamp validation**: Validating the token's embedded stamp against the database account's current `security_stamp`.
11. **`sessions_invalidated_at` cutoff validation**: Rejecting token issuance/refresh if the token was issued before the account's invalidation cutoff.
12. **Cookie transport**: Delivering the refresh token exclusively via an `HttpOnly`, `Secure`, `SameSite=Strict` cookie.
13. **CSRF strategy**: Enforcing Anti-Forgery (CSRF) protections on cookie-reliant endpoints (`/refresh`, `/logout`).
14. **Generic non-enumerating authentication responses**: Returning generic 401/403 responses without exposing internal account status or error reasons.
16. **Token signing-key provider abstraction**: `IJwtSigningKeyProvider` abstraction for locating RS256 keys.
17. **Key rotation and `kid` handling**: Injecting `kid` in JWT headers to support key rotation and the 24h old-key validation window.
18. **Clock skew handling**: Allowing a maximum of 30 seconds of clock skew for token expiry.
19. **Transaction and concurrency strategy**: Strictly serializing refresh rotation to prevent race conditions and lost updates.
20. **Test strategy**: Implementing comprehensive Unit, SQL Server Integration, and API Regression tests for token and cookie behaviors.

## 4. Explicit Exclusions
The following are NOT planned or implemented in this slice:
- Permission evaluation and role/admin-group authorization middleware (Phase 1B.1-D/E).
- Frontend login UI (Phase 1B.1-G).
- AD/LDAP external provider integration.
- First-admin bootstrap implementation (Phase 1B.1-F).
- Application audit writer implementation (Phase 1B.1-F).
- Semantic audit scrubbing (Phase 1B.1-F).
- Production secret provisioning.
- Production migration execution.
- Phase 1B.1-D through I.

## 5. Authoritative Documents Read
- `AGENTS.md`
- `docs/business/business-rules.md`, `permission-catalog.md`, `acceptance-criteria.md`
- `docs/architecture/technical-decisions-v1.0.md`
- `docs/architecture/phase-1a2-application-api-implementation.md`
- `docs/architecture/phase-1b0-security-discovery-decisions.md`
- `docs/decisions/phase-1b0-open-decisions.md`
- `docs/architecture/phase-1b1-authentication-authorization-implementation-plan.md`
- `docs/architecture/phase-1b1a-security-database-foundation-implementation.md`
- `docs/architecture/phase-1b1b-authentication-account-password-implementation-plan.md`
- `docs/architecture/phase-1b1b-authentication-account-password-implementation.md`
- `database/migrations/V0003__create_security_schema.sql`, `U0003__drop_security_schema.sql`

## 6. Decision Compatibility Matrix

| Decision ID | Approved Behavior | Impact on Slice C | Implementation Dependency | Project Owner Clarification Needed? |
|---|---|---|---|---|
| **DEC-1B-003** | Access: 15m. Refresh: 7d. Skew: 30s. | Hardcode or configure these lifetimes during issuance and validation. | None. | No. |
| **DEC-1B-005** | Opaque single-use refresh tokens. Hash storage only. Atomic rotation. Reuse revokes family. No server-side grace period. | Needs strict `SERIALIZABLE` transaction during refresh to prevent race conditions. | None. | No. |
| **DEC-1B-010** | Bootstrap boundary. | N/A to this slice. | Deferred to Slice F. | No. |
| **DEC-1B-011** | Fail closed on cache/infrastructure error. | 503 response if JWT provider or DB fails. | None. | No. |
| **DEC-1B-013** | Employment status ACTIVE/PROBATION required. | Login endpoint must check `Users` employment status. | Depends on joining `Users` table. | No. |
| **DEC-1B-018** | Refresh cookie: HttpOnly, Secure, SameSite=Strict. CSRF controls on cookies. Access token in memory. | `/login` and `/refresh` must set `Set-Cookie`. `/refresh` and `/logout` require CSRF token validation. | ASP.NET Core Antiforgery integration required. | Yes (Exact CSRF mechanism and Cookie Path). |
| **DEC-1B-019** | RS256/2048-bit signing key. 20-min overlap window. `kid` required. Dev user secrets. | Implement `IJwtSigningKeyProvider` resolving keys by `kid`. Include `kid` in JWT header. | `Microsoft.IdentityModel.Tokens` required. | No. |
| **DEC-1B-020** | Generic failure behavior (401/403). Non-enumerating. | Login/Refresh must return generic `ProblemDetails` for failures. | None. | Yes (Exact error codes). |

## 7. V0003 Database Compatibility Matrix
Using V0003 only, without schema changes (`V0004` is NOT AUTHORIZED).

| Requirement | Evaluation Result | Notes |
|---|---|---|
| `Refresh_Tokens` table | AVAILABLE IN V0003 | |
| Token family/session identifiers | AVAILABLE IN V0003 | `family_id` and `session_id` are `uniqueidentifier`. |
| Token hash | AVAILABLE IN V0003 | `token_hash` is `char(64)` for SHA-256 hex string. |
| Token issue/expiry/revocation fields | AVAILABLE IN V0003 | `issued_at`, `expires_at`, `used_at`, `revoked_at`, `revoke_reason`. |
| Reuse detection fields | AVAILABLE IN V0003 | `replaced_by_token_id`, `reuse_detected_at`. |
| Linkage to `User_Auth_Accounts` | AVAILABLE IN V0003 | `account_id` foreign key. |
| Security stamp relationship | IMPLEMENTABLE WITHOUT SCHEMA CHANGE | Accessed via `User_Auth_Accounts` using `account_id`. |
| Rowversion/concurrency fields | AVAILABLE IN V0003 | `row_version` present. |
| Audit/security metadata fields | AVAILABLE IN V0003 | `created_ip_address`, `created_user_agent`. |
| Indexes for lookup and revocation | AVAILABLE IN V0003 | `IX_RefreshTokens_FamilyId`, `IX_RefreshTokens_SessionId` exist. |
| Cascade behavior | AVAILABLE IN V0003 | No cascade deletes enforced. |

**Conclusion**: No V0004 is required. Slice C is fully implementable with V0003.

## 8. Token Model Plan

**Access Token (JWT)**
- **Claims Included**: `sub` (user_id), `sid` (session_id), `login_name` (provider_subject), `security_stamp`, `iat`, `exp`, `jti`.
- **Claims NOT Included**: Permissions, roles, active company status.
- **Company Context**: Ignored at the JWT level. Handled via `X-Company-Id` header on APIs.
- **Expiry**: 15 minutes.
- **Header**: Includes `kid` for key identification, `alg` = RS256.
- **Clock Skew**: 30 seconds during validation.
- **Validation Pipeline**: Checks signature, expiry, issuer/audience (if defined), and `kid`. After crypto validation, the `security_stamp` and `sessions_invalidated_at` are verified against the database.

**Refresh Token (Opaque)**
- **Material**: 256-bit secure random string, Base64Url encoded.
- **Storage**: Only SHA-256 hash stored in `Refresh_Tokens.token_hash`.
- **Transport**: Set as an `HttpOnly`, `Secure`, `SameSite=Strict` cookie. Not returned in JSON payload.
- **Rotation**: On `/refresh`, the token is marked `used_at`. A new token is issued linking back via `replaced_by_token_id`. `session_id` remains the same; `family_id` remains the same.
- **Reuse Detection**: If a request uses a token that already has `used_at` != null, a reuse event is triggered.
- **Revocation**: On reuse detection, all tokens with the same `family_id` are marked `revoked_at` with `revoke_reason = 'REUSE_DETECTED'`. This updates `Refresh_Tokens` state only; emitting a semantic audit event is deferred to Slice F.
- **Expiry**: 7 days.
- **Logout Behavior**: Marks the current `family_id` as revoked and clears the cookie.

## 9. Security-Stamp Integration
- **Embedded in JWT**: The active `User_Auth_Accounts.security_stamp` at issuance time is embedded as a claim in the JWT.
- **Refresh Token Storage**: `Refresh_Tokens` does not store the stamp.
- **Validation on Refresh**: The system reads the current account state. If the account `security_stamp` does not match the expected state (or if `sessions_invalidated_at` > token `issued_at`), the refresh is denied (`AUTH_SESSION_REVOKED` / `AUTH_SECURITY_STAMP_CHANGED`).
- **Account Disablement**: Triggers an immediate 403 `AUTH_ACCOUNT_DISABLED` on next token use or refresh.
- **Password Change / Admin Reset**: `sessions_invalidated_at` is updated (implemented in Slice B). Next refresh attempt fails.
- **Non-enumeration**: All failures return generic 401/403 responses.

## 10. Cookie and CSRF Plan
- **Cookie Attributes**:
  - `HttpOnly`: true
  - `Secure`: true (Requires HTTPS even in Dev)
  - `SameSite`: Strict
  - `Path`: `/api/v2/auth` (To restrict cookie transmission only to auth endpoints)
  - `Domain`: Omitted (defaults to current host)
  - `Expiry`: 7 days
- **CSRF Protection**:
  - Requires ASP.NET Core Antiforgery.
  - Endpoints reliant on the cookie (`/refresh`, `/logout`) must require a valid CSRF token.
  - The CSRF token is provided to the client via an endpoint or header upon successful login.
- **Logout**: The `Set-Cookie` header will overwrite the refresh cookie with an empty value and an expiration date in the past.

## 11. API Plan
- **`POST /api/v2/auth/login`**:
  - Request Body: `LoginRequest` (username, password).
  - Success Response: 200 OK with `LoginResponse` (accessToken, expiresIn). Writes Refresh Cookie.
  - Generic Failure: 401 `AUTH_INVALID_CREDENTIALS`.
  - Transaction: Yes, to record failed attempts/lockout (delegated to Slice B) and insert new `Refresh_Tokens` row.
- **`POST /api/v2/auth/refresh`**:
  - CSRF required.
  - Request: Reads Refresh Cookie.
  - Success Response: 200 OK with new JWT. Writes new Refresh Cookie.
  - Generic Failure: 401 `AUTH_REFRESH_TOKEN_INVALID` / `AUTH_REFRESH_TOKEN_REUSED` / `AUTH_SESSION_REVOKED`.
  - Transaction: Yes, `SERIALIZABLE` rotation.
- **`POST /api/v2/auth/logout`**:
  - CSRF required.
  - Request: Reads Refresh Cookie.
  - Success Response: 204 No Content. Clears Refresh Cookie.
  - Transaction: Yes, updates `Refresh_Tokens` family to revoked.

*(Note: `/api/v2/auth/me` and logout-all functionality are deferred to later slices as per the Phase 1B.1-A planning document endpoints list).*

## 12. Transaction and Concurrency Plan
- **Strict Single-Use Rotation**: No server-side grace period for concurrent refresh requests.
- **Row-Level Locking**: `SERIALIZABLE` isolation level with `UPDLOCK, HOLDLOCK` on the `User_Auth_Accounts` and `Refresh_Tokens` rows during the transaction.
- **Concurrent Requests with Same Refresh Token**: The first request locks the row, updates `used_at`, and commits. The second request blocks until the lock is released, then reads the row, sees `used_at != null`, triggers the **Family Revocation on Reuse** logic, and returns 401. Both concurrent requests deterministically resolve.
- **Race Conditions (Refresh vs Logout)**: Safe due to `UPDLOCK, HOLDLOCK`. One will succeed, the subsequent one will see the updated state (used or revoked) and act accordingly.
- **Deadlock Retry Policy**: Re-use existing `DeadlockRetryPolicy` targeting SQL Server error 1205. Maximum 3 attempts. Non-deadlock concurrency conflicts (e.g. `DbUpdateConcurrencyException`) fail fast without retry.

## 13. Test Strategy
*Plan only; no tests will be written in this slice.*

**Unit Tests**:
- Token issuance constructs valid JWTs with expected claims.
- `IJwtSigningKeyProvider` correctly identifies and rotates keys via `kid`.
- Refresh token hash generation validates SHA-256 output.
- Token validation fails on 30s+ expired tokens, wrong issuer/audience, or invalid signature.
- CSRF validation logic enforces required tokens.

**Integration Tests**:
- Requires `PTKD_TEST_PHASE1A2` guard.
- Login creates `Refresh_Tokens` row and returns valid cookie.
- Valid refresh rotates token, updates `used_at` and `replaced_by_token_id`.
- Old refresh reuse revokes entire family.
- Concurrent refresh requests guarantee strict single-use and family revocation without lost updates.
- Logout revokes current family.
- Refresh denied after password change (invalidated_at cutoff).
- Refresh denied after account disable.
- No raw token material persisted or logged.

**API Regression Tests**:
- No changes to existing Phase 1A.2 endpoints.
- `ProblemDetails` formatting strictly followed for all 401/403 responses.

## 14. File Manifest
- `docs/architecture/phase-1b1c-token-session-lifecycle-implementation-plan.md` (EXISTING - MODIFY / CREATE)
- `src/backend/PTKD.Domain/Security/Authentication/RefreshToken.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IJwtTokenGenerator.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IRefreshTokenService.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Application/Security/Authentication/Services/JwtTokenGenerator.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Application/Security/Authentication/Services/RefreshTokenService.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Application/Security/Authentication/Models/TokenResponses.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Api/Controllers/AuthController.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` (PROPOSED NEW FILE)
- `src/backend/PTKD.Infrastructure/Security/Cryptography/JwtSigningKeyProvider.cs` (PROPOSED NEW FILE)
- `tests/backend/PTKD.UnitTests/Security/Authentication/JwtTokenGeneratorTests.cs` (PROPOSED NEW FILE)
- `tests/backend/PTKD.UnitTests/Security/Authentication/RefreshTokenServiceTests.cs` (PROPOSED NEW FILE)
- `tests/backend/PTKD.IntegrationTests/AuthenticationTokenIntegrationTests.cs` (PROPOSED NEW FILE)
- `tests/backend/PTKD.ApiTests/Controllers/AuthControllerTests.cs` (PROPOSED NEW FILE)

## 15. Project Owner Decisions (RESOLVED FOR PLANNING)

1. **Login success response body**:
Approved:
`POST /api/v2/auth/login` returns:
```json
{
  "accessToken": "<jwt>",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "expiresAtUtc": "<utc datetime>",
  "user": {
    "userId": <number>,
    "username": "<string>",
    "displayName": "<string|null>"
  }
}
```
Refresh token must not be returned in the response body.
Refresh token is delivered only by HttpOnly Secure cookie.
Do not include permission list in JWT or login response in Slice C.

2. **Refresh cookie Path and SameSite**:
Approved:
- HttpOnly: true
- Secure: true
- SameSite: Strict
- Path: /api/v2/auth
- Domain: omitted by default
- Expiry: aligned with refresh token expiry, 7 days
Do not loosen SameSite for local dev in Slice C.

3. **CSRF mechanism**:
Approved:
Do not create `/auth/csrf` in Slice C.
Issue CSRF token on successful login and successful refresh.
Mechanism:
- separate non-HttpOnly Secure SameSite Strict CSRF cookie;
- also return CSRF token via response header;
- client sends X-CSRF-Token for `POST /api/v2/auth/refresh` and `POST /api/v2/auth/logout`.
Refresh token remains HttpOnly cookie.

4. **Active access token after password change / admin reset / account disable**:
Approved:
Slice C must validate account status and security stamp/cutoff server-side for authenticated API requests.
Do not rely only on 15-minute stateless JWT lifetime.
JWT access token must include session/family reference and security_stamp or equivalent stamp-version.
Protected API validation must verify:
- account is ACTIVE;
- user/employment remains eligible;
- token security stamp matches current account state;
- token/session was not issued at or before sessions_invalidated_at cutoff.
If DB or trusted validation store cannot be checked, fail closed.

5. **HTTP status for AUTH_ACCOUNT_LOCKED**:
Approved:
`AUTH_ACCOUNT_LOCKED` uses HTTP 403.
Do not use HTTP 423 in Phase 1B.1-C.
External response remains generic/non-enumerating.

6. **V0004 requirement**:
Approved:
No V0004/U0004 is authorized for Phase 1B.1-C.
Slice C must use V0003 as-is.
If implementation discovers V0003 is insufficient, stop and report:
`PHASE 1B.1-C IMPLEMENTATION BLOCKED — V0004 DECISION REQUIRED`
Do not modify V0003. Do not create V0004.

7. **`/auth/me`**:
Approved:
`/auth/me` is not authorized in Phase 1B.1-C.

8. **logout-all**:
Approved:
`/api/v2/auth/logout-all` is not authorized in Phase 1B.1-C.
Phase 1B.1-C logout revokes only the current refresh-token family/session represented by the refresh cookie.
Revoke-all-sessions belongs to Security Administration scope or a later explicitly approved slice.

## 16. Implementation Authorization

- **Project Owner result:** ACCEPTED BY PROJECT OWNER
- **Project Owner name:** Đào Hải Bách
- **Role:** Project Owner
- **Authorization date:** 2026-07-16
- **Confirmation method:** Direct written authorization
- **Conditions or residual risks accepted:** Phase 1B.1-C implementation remains NOT AUTHORIZED by this plan acceptance commit. Phase 1B.1-D through I remain NOT AUTHORIZED. Production migration remains NOT AUTHORIZED. Source/test/migration implementation remains NOT AUTHORIZED.

PHASE 1B.1-C PLAN ACCEPTED BY PROJECT OWNER — IMPLEMENTATION NOT AUTHORIZED
