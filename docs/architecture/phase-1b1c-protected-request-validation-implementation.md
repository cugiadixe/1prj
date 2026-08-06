# Phase 1B.1-C-C Protected Request Validation — Implementation Evidence

## Status

ACCEPTED BY PROJECT OWNER

Approver: Đào Hải Bách
Role: Project Owner
Acceptance date: 2026-07-17
Confirmation method: Direct written authorization

## Baseline and Commits

| Field | Value |
|---|---|
| Baseline commit (parent) | `d37259c96b24166c0b4cd5201c83d7b8662cce51` — Record Project Owner acceptance of Phase 1B.1-C-B implementation |
| Implementation commit | `584f4001034efc0f96c71f6f519bc0f7b3139691` — Implement Phase 1B.1-C-C Protected Request Validation |
| Evidence correction commit | `50f5f494b8bbf8507025946f98905f093642b803` — Complete Phase 1B.1-C-C evidence documentation |

## Accepted Scope

The Project Owner has reviewed the implementation, testing, formal review, and evidence correction for Phase 1B.1-C-C and accepts the following:

### 1. JWT Bearer Validation

- Issuer validation: `ValidIssuer = "PTKD-ERP"`
- Audience validation: `ValidAudience = "PTKD-ERP-API"`
- Signing key validation with `kid`-based key resolver via `IJwtSigningKeyProvider`
- `exp`/`nbf` lifetime validation
- Clock skew: 30 seconds
- `BuildServiceProvider` anti-pattern removed; `JwtBearerConfigureOptions` wired via `ConfigureOptions<>` DI

### 2. Protected-Request Business Validation (server-side, after cryptographic pass)

The following conditions are accepted as implemented:

- Auth account must exist
- Auth account status must be `ACTIVE`
- Linked user must exist
- Employment status `ACTIVE` or `PROBATION` is eligible
- Employment status other than `ACTIVE`/`PROBATION` is rejected
- Token `security_stamp` must match current `UserAuthAccount.SecurityStamp`
- Token `issued_at` must be **strictly after** `sessions_invalidated_at` cutoff
- Token `issued_at` equal to cutoff is **denied**
- Token `issued_at` before cutoff is **denied**
- Validation **fails closed** when trusted server-side state cannot be checked (infrastructure exception)

### 3. Failure Mapping

- Protected request failure returns generic `401 Unauthorized`
- Response does not reveal: account disabled, employment inactive, stamp mismatch, session cutoff, or account existence
- No internal reason details in public response body

### 4. Explicit Exclusions — Accepted

The following were NOT implemented in C-C and are accepted as excluded:

- Permission evaluation: NOT IMPLEMENTED
- Role/admin-group authorization: NOT IMPLEMENTED
- Company-scope authorization by JWT alone: NOT IMPLEMENTED
- Production `/auth/me` endpoint: NOT IMPLEMENTED
- `/api/v2/auth/logout-all`: NOT IMPLEMENTED
- Frontend: NOT IMPLEMENTED
- AD/LDAP, bootstrap, audit writer, semantic audit scrubbing: NOT IMPLEMENTED
- Test-only protected endpoint (`ProtectedTestController`) is accepted as test infrastructure only, not production API behavior

## Build and Test Results — Accepted

| Command | Result |
|---|---|
| `dotnet build src/backend/PTKD-ERP.sln --configuration Debug --no-restore` | Build succeeded. 0 errors. 4 MSB3277 warnings (pre-existing, non-blocking). |
| `dotnet test PTKD.UnitTests` | Passed: 76, Failed: 0, Skipped: 0 |
| `dotnet test PTKD.IntegrationTests` | Passed: 138, Failed: 0, Skipped: 0 |
| `dotnet test PTKD.ApiTests` | Passed: 88, Failed: 0, Skipped: 0 |
| `dotnet test PTKD.IntegrationTests --filter FullyQualifiedName~DatabaseSafety` | Passed: 17, Failed: 0, Skipped: 0 |

## Database Safety — Accepted

- DB-writing tests use `PTKD_TEST_PHASE1A2` only
- `InitialCatalog` guard maintained
- `SELECT DB_NAME()` guard maintained
- `PTKD_DEV` was not connected during any test run
- Production migration was not executed
- `V0003`/`U0003`: unchanged
- `V0004`/`U0004`: do not exist

## Changed Files — Accepted

| File | Change |
|---|---|
| `docs/architecture/phase-1b1c-protected-request-validation-implementation.md` | NEW — this evidence document |
| `src/backend/PTKD.Api/Program.cs` | MODIFY — registered `IProtectedRequestValidator`, wired `JwtBearerConfigureOptions` via `ConfigureOptions<>` |
| `src/backend/PTKD.Api/Security/JwtBearerConfigureOptions.cs` | NEW |
| `src/backend/PTKD.Application/Security/Authentication/Interfaces/IProtectedRequestValidator.cs` | NEW |
| `src/backend/PTKD.Application/Security/Authentication/Services/JwtAccessTokenService.cs` | MODIFY — minor addition |
| `src/backend/PTKD.Application/Security/Authentication/Services/ProtectedRequestValidator.cs` | NEW |
| `tests/backend/PTKD.ApiTests/ProtectedEndpointIntegrationTests.cs` | NEW |
| `tests/backend/PTKD.ApiTests/ProtectedTestController.cs` | NEW — test host only |
| `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs` | MODIFY |
| `tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj` | MODIFY — added `Moq.EntityFrameworkCore` (test-only) |
| `tests/backend/PTKD.UnitTests/Security/Authentication/ProtectedRequestValidatorTests.cs` | NEW |

## Authorization Status for Subsequent Phases

| Phase | Status |
|---|---|
| Phase 1B.1-C-C | **ACCEPTED BY PROJECT OWNER — 2026-07-17** |
| Phase 1B.1-D through I | NOT AUTHORIZED BY THIS ACCEPTANCE |
| Production migration | NOT AUTHORIZED |
