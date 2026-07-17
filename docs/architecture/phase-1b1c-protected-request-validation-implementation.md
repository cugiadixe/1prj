# Phase 1B.1-C-C Protected Request Validation — Implementation Evidence

## Status

IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE

## Baseline and Commits

| Field | Value |
|---|---|
| Baseline commit (parent) | `d37259c96b24166c0b4cd5201c83d7b8662cce51` — Record Project Owner acceptance of Phase 1B.1-C-B implementation |
| Implementation commit | `584f4001034efc0f96c71f6f519bc0f7b3139691` — Implement Phase 1B.1-C-C Protected Request Validation |

## Scope Implemented

Phase 1B.1-C-C implements cryptographic and business-level protected-request validation only.
No permission evaluation, company-scope authorization, or RBAC was implemented in this phase.

### Changed Files

| File | Change |
|---|---|
| `docs/architecture/phase-1b1c-protected-request-validation-implementation.md` | NEW — this evidence document |
| `src/backend/PTKD.Api/Program.cs` | MODIFY — registered `IProtectedRequestValidator`, wired `JwtBearerConfigureOptions` via `ConfigureOptions<>` (no `BuildServiceProvider`) |
| `src/backend/PTKD.Api/Security/JwtBearerConfigureOptions.cs` | NEW — `IConfigureNamedOptions<JwtBearerOptions>` implementation |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/IProtectedRequestValidator.cs` | NEW — interface |
| `src/backend/PTKD.Application/Security/Authentication/Services/JwtAccessTokenService.cs` | MODIFY — minor addition |
| `src/backend/PTKD.Application/Security/Authentication/Services/ProtectedRequestValidator.cs` | NEW — implementation |
| `tests/backend/PTKD.ApiTests/ProtectedEndpointIntegrationTests.cs` | NEW — API integration tests |
| `tests/backend/PTKD.ApiTests/ProtectedTestController.cs` | NEW — test-only protected endpoint |
| `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs` | MODIFY — loads `ProtectedTestController` assembly part into test host |
| `tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj` | MODIFY — added `Moq.EntityFrameworkCore` test-only package |
| `tests/backend/PTKD.UnitTests/Security/Authentication/ProtectedRequestValidatorTests.cs` | NEW — unit tests |

## Cryptographic Validation

Implemented in `JwtBearerConfigureOptions` via `IConfigureNamedOptions<JwtBearerOptions>`:

- **Issuer**: `ValidateIssuer = true`, `ValidIssuer = "PTKD-ERP"`
- **Audience**: `ValidateAudience = true`, `ValidAudience = "PTKD-ERP-API"`
- **Lifetime**: `ValidateLifetime = true`
- **Clock skew**: `ClockSkew = TimeSpan.FromSeconds(30)`
- **Signing key**: `ValidateIssuerSigningKey = true`; key resolved dynamically from `IJwtSigningKeyProvider` using `kid` header claim
- **DI wiring**: `builder.Services.ConfigureOptions<JwtBearerConfigureOptions>()` — no `BuildServiceProvider` anti-pattern

## JWT Bearer Wiring — BuildServiceProvider Anti-Pattern Removed

The previous implementation used `BuildServiceProvider` inside `Program.cs` to resolve the signing key at configuration time.
This has been removed.

`JwtBearerConfigureOptions` now receives `IJwtSigningKeyProvider` via constructor DI and is registered as `IConfigureNamedOptions<JwtBearerOptions>`.
The key resolver runs per-request inside `IssuerSigningKeyResolver`, resolving the correct RSA key by `kid` at validation time without any service-locator anti-pattern.

## Business Rule Validation

Implemented in `ProtectedRequestValidator` (`IProtectedRequestValidator`).
Invoked from `JwtBearerEvents.OnTokenValidated` using scoped DI (`context.HttpContext.RequestServices`).

Validation sequence:

1. **Auth account exists**: `UserAuthAccount` must exist for the `sub` claim user ID. If not found → fail.
2. **Auth account status ACTIVE**: `AuthAccountStatus` must equal `"ACTIVE"` (case-insensitive). If not → fail.
3. **Linked user exists**: `account.User` navigation property must not be null. If null → fail.
4. **Employment eligibility**: Employment status **ACTIVE or PROBATION** is eligible. Any other employment status (e.g., `TERMINATED`, `RESIGNED`, `ON_LEAVE`) is rejected → fail.
5. **Security stamp matches**: JWT `security_stamp` claim must exactly match `UserAuthAccount.SecurityStamp`. If mismatch → fail.
6. **Session cutoff**: If `SessionsInvalidatedAt` is set, token `iat` must be **strictly after** the cutoff (`issuedAtUtc > SessionsInvalidatedAt`). Token issued **at** the cutoff (`iat == cutoff`) is **denied**. Token issued **before** the cutoff is denied. Token issued **after** the cutoff is allowed when all other conditions pass.

## Fail-Closed Behavior

Any infrastructure exception (e.g., database unreachable) inside `ProtectedRequestValidator.ValidateAsync` is caught, logged as an error, and returns `false`.
This causes `OnTokenValidated` to call `context.Fail("Unauthorized")`, resulting in a `401 Unauthorized` response.
The system never grants access when the trusted validation store cannot be checked.

## Failure Mapping

| Condition | External Response |
|---|---|
| Invalid or expired JWT | 401 Unauthorized |
| Missing bearer token | 401 Unauthorized |
| Invalid signature / tampered token | 401 Unauthorized |
| Account not found | 401 Unauthorized (generic) |
| Account disabled | 401 Unauthorized (generic) |
| Linked user missing | 401 Unauthorized (generic) |
| Employment not ACTIVE or PROBATION | 401 Unauthorized (generic) |
| Security stamp mismatch | 401 Unauthorized (generic) |
| Token issued at or before session cutoff | 401 Unauthorized (generic) |
| Infrastructure exception | 401 Unauthorized (generic) |

No internal reason (account status, employment status, stamp mismatch, cutoff detail, or account existence) is revealed in the public response body.

## Test Commands and Results

All commands run on commit `584f4001034efc0f96c71f6f519bc0f7b3139691` with `--no-restore`.

```
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --no-restore
```
Result: **Build succeeded. 0 errors. 4 MSB3277 warnings (pre-existing, non-blocking).**

```
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore
```
Result: **Passed: 76, Failed: 0, Skipped: 0.**

```
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore
```
Result: **Passed: 138, Failed: 0, Skipped: 0.**

```
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore
```
Result: **Passed: 88, Failed: 0, Skipped: 0.**

```
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DatabaseSafety"
```
Result: **Passed: 17, Failed: 0, Skipped: 0.**

## Unit Test Coverage

Tests in `PTKD.UnitTests/Security/Authentication/ProtectedRequestValidatorTests.cs`:

| Test | Validates |
|---|---|
| `ValidateAsync_ActiveAccount_EligibleUser_MatchingStamp_AfterCutoff_Passes` | Happy path — all conditions pass |
| `ValidateAsync_AccountNotFound_Fails` | Account not found |
| `ValidateAsync_AccountDisabled_Fails` | Account status not ACTIVE |
| `ValidateAsync_LinkedUserMissing_Fails` | User navigation null |
| `ValidateAsync_EmploymentStatusNotActiveOrProbation_Fails` | Employment status TERMINATED |
| `ValidateAsync_SecurityStampMismatches_Fails` | Stamp mismatch |
| `ValidateAsync_TokenIssuedAtCutoff_Fails` | `iat == cutoff` (inclusive deny) |
| `ValidateAsync_TokenIssuedBeforeCutoff_Fails` | `iat < cutoff` |
| *(covered by happy path)* | `iat > cutoff` passes |
| `ValidateAsync_InfrastructureException_FailsClosed` | DB exception → fail closed |

## API Test Coverage

Tests in `PTKD.ApiTests/ProtectedEndpointIntegrationTests.cs` (HTTP boundary via `SafeTestWebApplicationFactory`):

| Test | Validates |
|---|---|
| `ProtectedEndpoint_ValidToken_Succeeds` | Valid token → 200 OK |
| `ProtectedEndpoint_MissingToken_Returns401` | No bearer token → 401 |
| `ProtectedEndpoint_InvalidSignature_Returns401` | Tampered token → 401 |
| `ProtectedEndpoint_ExpiredToken_Returns401` | Expired token → 401 |
| `ProtectedEndpoint_SecurityStampMismatch_Returns401` | Stamp mismatch → 401, body does not contain "stamp" |
| `ProtectedEndpoint_AccountDisabled_Returns401` | Account disabled → 401, body does not contain "disabled" |
| `ProtectedEndpoint_EmploymentTerminated_Returns401` | Employment TERMINATED → 401, body does not contain "employment" |
| `ProtectedEndpoint_SessionCutoff_Returns401` | Cutoff in future of token issue → 401 |

Test-only endpoint: `ProtectedTestController` lives in namespace `PTKD.ApiTests`, route `api/v2/test/ProtectedTest`.
It is loaded into the test host only via `AddApplicationPart`. It is not part of the production API.

## Database Safety Evidence

- All DB-writing API tests use `SafeTestWebApplicationFactory` with `ConnectionStrings:DefaultConnection = PTKD_TEST_PHASE1A2`.
- `InitialCatalog` guard runs before writes: tests assert they are connected to `PTKD_TEST_PHASE1A2`.
- `DatabaseSafety` filter passed: 17 tests, 0 failures.
- `PTKD_DEV` was not connected during any test run.
- No production migration was executed.
- `V0003/U0003` are unchanged.
- `V0004/U0004` do not exist.

## Explicit Exclusions

| Item | Status |
|---|---|
| Phase 1B.1-C-C | IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE |
| Phase 1B.1-D through I (authorization, permission evaluation, etc.) | NOT AUTHORIZED |
| Permission evaluation | NOT IMPLEMENTED — not authorized in C-C |
| Role/admin-group authorization | NOT IMPLEMENTED — not authorized in C-C |
| Company-scope authorization by JWT alone | NOT IMPLEMENTED — not authorized in C-C |
| Production `/auth/me` endpoint | NOT IMPLEMENTED |
| `/api/v2/auth/logout-all` endpoint | NOT IMPLEMENTED |
| Frontend | NOT IMPLEMENTED |
| AD/LDAP | NOT IMPLEMENTED |
| Bootstrap | NOT IMPLEMENTED |
| Audit writer | NOT IMPLEMENTED |
| Semantic audit scrubbing | NOT IMPLEMENTED |
| V0004/U0004 migration | NOT CREATED |
| V0003/U0003 | UNCHANGED |
| Production migration | NOT AUTHORIZED — NOT EXECUTED |

## Remaining Work After Phase 1B.1-C-C

- Phase 1B.1-D and later (authorization, permission evaluation, RBAC, company-scope enforcement) remain **NOT AUTHORIZED**.
- No role/admin-group permission evaluation was implemented in C-C.
- No company-scope authorization by JWT alone was implemented.
- No audit writer or semantic audit scrubbing was implemented.
- No frontend was implemented.
- No production migration was executed.
- All subsequent phases require explicit Project Owner authorization before implementation begins.
