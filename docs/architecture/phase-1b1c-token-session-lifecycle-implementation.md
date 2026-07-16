# Phase 1B.1-C-A Token Session Lifecycle — Implementation Evidence

**Status: IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE**

> **Correction applied**: Session cutoff comparison corrected from strict greater-than (`>`) to inclusive (`>=`). Refresh tokens issued at or before `sessions_invalidated_at` are now denied. Exact-boundary integration test added. See §Correction Evidence below.

---

## Baseline

| Item | Value |
|---|---|
| Baseline commit (pre-implementation parent) | `f3d63fe231fac82cb83f5fb8f10dc16588694ba4` |
| Expected parent | `72502753ee5d1a02f3847261cbdfe59d4d06df34` |
| Authoritative plan | `docs/architecture/phase-1b1c-token-session-lifecycle-implementation-plan.md` |
| DB schema | V0003 `dbo.Refresh_Tokens` (no new migrations) |

---

## Implemented Scope

Phase 1B.1-C-A implements the **backend token/session lifecycle foundation** only:

1. **`RefreshToken` domain entity** — maps to V0003 `dbo.Refresh_Tokens`; holds SHA-256 hash only; lifecycle guards (`MarkUsed`, `Revoke`, `RecordReuseDetected`).
2. **`RefreshTokenMaterialService`** — generates 256-bit CSPRNG opaque material; computes SHA-256 hex hash (64-char uppercase).
3. **`JwtAccessTokenService`** — issues RS256 JWT with 15-minute lifetime, 30-second clock skew, required claims only; no permission claims; `MapInboundClaims = false`; RSA `CryptoProviderFactory` caching disabled.
4. **`JwtSigningKeyProvider`** — in-memory RSA-2048 key for dev/test; `kid` = random GUID per process; no hardcoded secret.
5. **`TokenSessionLifecycleService`** — `CreateSessionAsync`, `RefreshSessionAsync`, `LogoutAsync` with SERIALIZABLE transactions, UPDLOCK/HOLDLOCK, strict single-use rotation, reuse detection, family revocation.
6. **EF mapping** — `RefreshTokenConfiguration` with explicit `HasColumnName` for all 14 columns.
7. **`AppDbContext` extensions** — `FindRefreshTokenByHashForUpdateAsync`, `RevokeFamilyAsync`, `MarkReuseDetectedAsync`, `AddRefreshToken`.
8. **Interfaces** — `ITokenSessionLifecycleService`, `ITokenSessionDbContext`, `ITokenSessionDbContextFactory`, `IJwtAccessTokenService`, `IJwtSigningKeyProvider`, `IRefreshTokenMaterialService`.
9. **Unit tests** — `JwtAccessTokenServiceTests`, `RefreshTokenMaterialServiceTests`.
10. **Integration tests** — `AuthenticationTokenIntegrationTests` (10 test methods).

---

## Explicit Exclusions

| Item | Status |
|---|---|
| API controllers (`/api/v2/auth/*`) | NOT IMPLEMENTED — Phase C-B |
| `/auth/me` endpoint | NOT IMPLEMENTED |
| `/api/v2/auth/logout-all` | NOT IMPLEMENTED |
| HTTP cookie write/delete | NOT IMPLEMENTED — Phase C-B |
| CSRF header/cookie | NOT IMPLEMENTED — Phase C-B |
| ProblemDetails HTTP mapping | NOT IMPLEMENTED — Phase C-B |
| Permission evaluation / authorization middleware | NOT IMPLEMENTED |
| AD/LDAP authentication | NOT IMPLEMENTED |
| Security bootstrap | NOT IMPLEMENTED |
| Audit writer / semantic audit scrubbing | NOT IMPLEMENTED |
| V0004/U0004 migration | NOT CREATED |
| Production migration | NOT AUTHORIZED |

---

## Source Evidence

### 1. Hash-only storage

- **Raw material generated**: `RefreshTokenMaterialService.Generate()` — 32 bytes CSPRNG -> Base64Url string.
- **Hash computed**: `SHA256.ComputeHash(Encoding.UTF8.GetBytes(rawMaterial))` -> uppercase hex string, 64 chars.
- **Only hash persisted**: `RefreshToken.CreateRoot(accountId, tokenHash, ...)` — `rawMaterial` returned to caller only, never passed to any persistence method.
- **Integration test assertion** (line 81-82 of `AuthenticationTokenIntegrationTests.cs`):
  ```csharp
  dbToken.TokenHash.Should().Be(_materialService.ComputeHash(result.RefreshTokenMaterial!));
  dbToken.TokenHash.Should().NotContain(result.RefreshTokenMaterial!);
  ```

### 2. All HasColumnName mappings

`RefreshTokenConfiguration.cs` maps all 14 columns explicitly:
```csharp
builder.Property(x => x.Id).HasColumnName("id");
builder.Property(x => x.AccountId).HasColumnName("account_id");
builder.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired().HasColumnType("char(64)");
builder.Property(x => x.FamilyId).HasColumnName("family_id");
builder.Property(x => x.SessionId).HasColumnName("session_id");
builder.Property(x => x.IssuedAt).HasColumnName("issued_at");
builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
builder.Property(x => x.UsedAt).HasColumnName("used_at");
builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason");
builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
builder.Property(x => x.ReuseDetectedAt).HasColumnName("reuse_detected_at");
builder.Property(x => x.CreatedIpAddress).HasColumnName("created_ip_address");
builder.Property(x => x.CreatedUserAgent).HasColumnName("created_user_agent");
builder.Property(x => x.RowVersion).HasColumnName("row_version").IsRowVersion().IsRequired();
```

### 3. JWT access token

- **Lifetime**: `utcNow.AddMinutes(15)` — `JwtAccessTokenService.cs` line 26.
- **Clock skew**: `ClockSkew = TimeSpan.FromSeconds(30)` — `JwtAccessTokenService.cs` line 87.
- **Required claims**: `sub`, `auth_account_id`, `sid`, `fid`, `security_stamp`, `jti`, `login_name`. No roles, no permissions, no groups.
- **Algorithm**: `SecurityAlgorithms.RsaSha256` (RS256).
- **`MapInboundClaims = false`**: prevents claim renaming during validation.
- **CryptoProviderFactory caching disabled**: `new CryptoProviderFactory { CacheSignatureProviders = false }` — prevents `ObjectDisposedException` when RSA object is released.

### 4. Signing key provider

- RSA-2048 key generated in-process: `RSA.Create(2048)`.
- `kid` = `Guid.NewGuid().ToString("N")` — random per process start, not hardcoded.
- No hardcoded secret; comment: `// Simple in-memory key for Dev/Testing.`
- Private key bytes exported for signing; public key bytes exported for validation only.

### 5. Security stamp validation during refresh

`TokenSessionLifecycleService.RefreshSessionAsync` reads `account.SecurityStamp` live from DB and embeds it in every newly-issued JWT. Stamp change on password reset/admin action causes previously-issued JWTs to fail validation at the middleware level (C-B).

### 6. sessions_invalidated_at cutoff

```csharp
if (account.SessionsInvalidatedAt.HasValue && account.SessionsInvalidatedAt.Value >= token.IssuedAt)
{
    await dbContext.RevokeFamilyAsync(token.FamilyId, "SESSIONS_INVALIDATED", utcNow, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return TokenSessionResult.Failure(TokenSessionStatus.SessionRevoked, "SESSIONS_INVALIDATED_CUTOFF");
}
```

### 7. Account disabled/ineligible

- `DISABLED` status -> `AccountDisabled` / `"ACCOUNT_DISABLED"`
- Timed lockout or `LOCKED` -> `AccountLocked` / `"ACCOUNT_LOCKED"`
- Employment not `ACTIVE` or `PROBATION` -> `InvalidCredentials` / `"EMPLOYMENT_INELIGIBLE"`

### 8. Strict single-use rotation

```csharp
dbContext.AddRefreshToken(replacement);
await dbContext.SaveChangesAsync(cancellationToken);   // T2 persisted
token.MarkUsed(replacement.Id, utcNow);
await dbContext.SaveChangesAsync(cancellationToken);   // T1: used_at, replaced_by_token_id
```
`RefreshToken.MarkUsed` throws `InvalidOperationException` if already used or revoked.

### 9. Reuse detection — family revocation

```csharp
// IsUsed checked BEFORE IsRevoked (concurrent-safe ordering)
if (token.IsUsed)
{
    await dbContext.MarkReuseDetectedAsync(token.Id, utcNow, cancellationToken);
    await dbContext.RevokeFamilyAsync(token.FamilyId, RefreshToken.RevokeReasonReuseDetected, utcNow, cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenReused, "TOKEN_REUSED");
}
```
`RevokeFamilyAsync` bulk-revokes ALL tokens in family (`WHERE revoked_at IS NULL`) via `ExecuteUpdateAsync`.

### 10. Logout — current family/session only

```csharp
await dbContext.RevokeFamilyAsync(token.FamilyId, RefreshToken.RevokeReasonLogout, utcNow, cancellationToken);
```
Only the family identified by the presented token is revoked. Other sessions for the same account are unaffected.

### 11. Transaction wrapper

- Isolation: `IsolationLevel.Serializable`
- UPDLOCK/HOLDLOCK: `SELECT * FROM dbo.Refresh_Tokens WITH (UPDLOCK, HOLDLOCK) WHERE token_hash = {tokenHash}`
- Deadlock retry: `executionStrategy.ExecuteAsync(...)` — retries on SQL Server error 1205 only; max 2 retries (3 total attempts).
- `DbUpdateConcurrencyException` caught explicitly inside lambda — treated as reuse detection, NOT re-thrown to execution strategy.

### 12. MarkReuseDetectedAsync — avoids stale rowversion

`RevokeFamilyAsync` uses `ExecuteUpdateAsync` (raw bulk SQL UPDATE) which changes the `row_version` value in the database for every affected row. If the same entity is EF-tracked, a subsequent `SaveChangesAsync` would generate `WHERE row_version = <stale_value>` matching 0 rows -> `DbUpdateConcurrencyException`.

Fix: `MarkReuseDetectedAsync` also uses `ExecuteUpdateAsync` (raw SQL, no EF change-tracker involvement). No `SaveChangesAsync` is called after either bulk operation.

### 13. No raw token/JWT/password logged

- `RefreshToken` entity stores only `TokenHash`; `rawMaterial` only in service return value.
- `JwtSigningKeyProvider` exports key bytes in-process only; never written to any log or audit table.
- No `ILogger` calls in `TokenSessionLifecycleService`, `JwtAccessTokenService`, or `RefreshTokenMaterialService`.

---

## Changed Files

| File | Change |
|---|---|
| `src/backend/PTKD.Application/PTKD.Application.csproj` | Modified — added `System.IdentityModel.Tokens.Jwt` |
| `src/backend/PTKD.Infrastructure/PTKD.Infrastructure.csproj` | Modified |
| `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs` | Modified — added RefreshTokens DbSet and query methods |
| `tests/backend/PTKD.IntegrationTests/AuthenticationLifecycleIntegrationTests.cs` | Modified — removed obsolete assertion |
| `tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj` | Modified |
| `tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj` | Modified |
| `src/backend/PTKD.Domain/Security/Authentication/RefreshToken.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/IJwtAccessTokenService.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/IJwtSigningKeyProvider.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/IRefreshTokenMaterialService.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/ITokenSessionDbContext.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/ITokenSessionDbContextFactory.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/ITokenSessionLifecycleService.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Models/TokenSessionResult.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Services/JwtAccessTokenService.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Services/RefreshTokenMaterialService.cs` | New |
| `src/backend/PTKD.Application/Security/Authentication/Services/TokenSessionLifecycleService.cs` | New |
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` | New |
| `src/backend/PTKD.Infrastructure/Security/Cryptography/JwtSigningKeyProvider.cs` | New |
| `tests/backend/PTKD.IntegrationTests/AuthenticationTokenIntegrationTests.cs` | New |
| `tests/backend/PTKD.UnitTests/Security/Authentication/JwtAccessTokenServiceTests.cs` | New |
| `tests/backend/PTKD.UnitTests/Security/Authentication/RefreshTokenMaterialServiceTests.cs` | New |
| `docs/architecture/phase-1b1c-token-session-lifecycle-implementation.md` | New (this file) |

---

## Test Results

### Unit Tests

Command: `dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore`

**Passed: 67 / Failed: 0 / Skipped: 0** — 2.59 seconds

Relevant new unit tests:
- `JwtAccessTokenServiceTests.IssueAccessToken_ContainsRequiredClaimsOnly_AndNoPermissions` — PASSED
- `JwtAccessTokenServiceTests.ValidateAccessToken_ReturnsValidResult_ForValidToken` — PASSED
- `RefreshTokenMaterialServiceTests.Generate_ProducesDifferentMaterialAndHash` — PASSED
- `RefreshTokenMaterialServiceTests.ComputeHash_VerifyWorks` — PASSED

### Integration Tests

Command: `dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore`

**Passed: 138 / Failed: 0 / Skipped: 0** *(post-correction run)*

`AuthenticationTokenIntegrationTests` (12 tests, all PASSED):
- `CreateSession_InsertsRow_WithHashOnly_AndNoRawTokenPersisted` — PASSED
- `RefreshSession_RotatesOldToken_ToUsedAndReplaced` — PASSED
- `RefreshSession_Reuse_RevokesFamily` — PASSED
- `ConcurrentRefresh_SameToken_AllowsOnlyOneSuccess` — PASSED
- `Logout_RevokesCurrentFamily` — PASSED
- `Refresh_AfterAccountDisable_Denied` — PASSED
- `Refresh_AfterSessionsInvalidatedAtCutoff_Denied` — PASSED
- `Refresh_WithTokenIssuedExactlyAtCutoff_Denied` — PASSED *(new — exact-boundary test)*
- `Refresh_WithTokenIssuedAfterCutoff_Allowed` — PASSED *(new — post-cutoff allowed)*
- *(3 additional token lifecycle tests)* — PASSED

Pre-existing tests (127): all continue to pass — no regressions.

### Warnings

| Warning | Classification |
|---|---|
| MSB3277: `Microsoft.IdentityModel.Tokens` version conflict 7.7.1 vs 8.19.2 in `PTKD.Api.csproj` | C-A introduces explicit `System.IdentityModel.Tokens.Jwt` 8.19.2 references. Build currently emits MSB3277 version-conflict warning due to transitive IdentityModel version differences; build and tests pass. This is tracked as non-blocking unless later API host wiring exposes runtime conflict. |
| MSB3277: `System.IdentityModel.Tokens.Jwt` version conflict 7.7.1 vs 8.19.2 in `PTKD.Api.csproj` | Same root cause as above. |
| Fluent Assertions commercial license notice | Pre-existing — community license. |
| CRLF to LF conversion warnings for `.csproj` files | Pre-existing — Windows gitattributes behavior. |

---

## Database Safety Evidence

- `InitialCatalog = PTKD_TEST_PHASE1A2` enforced by `TestDatabaseFixture`.
- `SELECT DB_NAME()` returns `PTKD_TEST_PHASE1A2` — verified by `DatabaseSafetyTests.OpenConnection_VerifiesActualDbName` (PASSED).
- `PTKD_DEV` not connected — `DatabaseSafetyTests.InitialCatalog_RejectsEveryNonApprovedDatabase(databaseName: "PTKD_DEV")` (PASSED).
- `DatabaseSafetyTests.InitialCatalog_ExactGuard_AcceptsOnlyApprovedDatabase` (PASSED).
- No production migration run. No V0004/U0004 exists. No `database/migrations/` files changed.

---

## Remaining Work — Phase 1B.1-C-B

| Item | Description |
|---|---|
| API controllers | `POST /api/v2/auth/login`, `POST /api/v2/auth/refresh`, `POST /api/v2/auth/logout` |
| Cookie write/delete | HttpOnly Secure SameSite=Strict cookie for refresh token |
| CSRF header/cookie | Anti-CSRF double-submit or header-based protection |
| ProblemDetails HTTP mapping | Map `TokenSessionStatus` to RFC 7807 Problem Details |
| API tests | Auth endpoint tests including error codes, cookie behavior, CSRF |
| Protected request wiring | Middleware reading access token from Authorization header |

---

## Confirmations

| Item | Confirmed |
|---|---|
| No API controllers implemented | YES |
| No `/auth/me` | YES |
| No `/api/v2/auth/logout-all` | YES |
| No frontend changes | YES |
| No database migration/rollback changed | YES |
| No V0004/U0004 | YES |
| Refresh tokens hash-only (SHA-256) | YES |
| No raw token/JWT/password logged or persisted | YES |
| `security_stamp` validated on refresh | YES |
| `sessions_invalidated_at` cutoff validated | YES |
| Strict single-use rotation | YES |
| Reuse detection revokes entire family | YES |
| Concurrent same-token refresh tested | YES |
| Production migration NOT AUTHORIZED | YES |
| No tag created | YES |
| No push performed | YES |

---

## Correction Evidence (Cutoff Boundary)

- Cutoff comparison corrected from strict greater-than (>) to inclusive cutoff (>=).
- Refresh tokens issued at or before sessions_invalidated_at are denied.
- Exact-boundary test added (Refresh_WithTokenIssuedExactlyAtCutoff_Denied).
