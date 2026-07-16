# Phase 1B.1-C-C Protected Request Validation Implementation

## Overview
This document records the implementation of cryptographic and business-level request validation for protected endpoints. The authorization model enforces token validation without performing domain permission evaluation.

## Cryptographic Validation
- **Middleware**: Integrated `JwtBearerDefaults.AuthenticationScheme` in `Program.cs`.
- **Options**: Created `JwtBearerConfigureOptions` validating `ValidIssuer`, `ValidAudience`, `ValidateLifetime`, and resolving the active signature key dynamically using `IJwtSigningKeyProvider`.
- **Claims Mapping**: Prevented inbound claim remapping by checking both natively mapped and raw JWT subject (`sub`) claims inside the event handler.
- **Fail-Closed**: Non-compliant claims automatically force an `Unauthorized` context fail.

## Business Rule Validation
- **Validator**: Implemented `IProtectedRequestValidator` and `ProtectedRequestValidator`.
- **Rules Verified**:
  - Requires the `UserAuthAccount` to exist for the subject claim ID.
  - Requires `AuthAccountStatus` to equal `ACTIVE`.
  - Requires linked `User` `AccountStatus` to equal `ACTIVE`.
  - Requires linked `User` `EmploymentStatus` to equal `ACTIVE`.
  - Requires JWT `security_stamp` claim to perfectly match the database `SecurityStamp`.
  - Requires JWT `iat` (Issued At) claim to occur *after* the account's `SessionsInvalidatedAt` (if populated).
- **Enforcement Event**: Executed within `JwtBearerEvents.OnTokenValidated` directly against the scoped database context.
- **Infrastructure Safety**: Fails closed natively. Caught exceptions inside the validation layer log warnings and return `false`, resulting in an implicit `401 Unauthorized`.

## Testing Evidence
- **Integration Tests**: Added full test suite `ProtectedEndpointIntegrationTests` using `SafeTestWebApplicationFactory`. Tests execute over an HTTP boundary.
- **Scenarios Handled**:
  - `ValidToken_Succeeds`: 200 OK
  - `ExpiredToken_Returns401`: 401 Unauthorized
  - `SecurityStampMismatch_Returns401`: 401 Unauthorized
  - `AccountDisabled_Returns401`: 401 Unauthorized
  - `EmploymentTerminated_Returns401`: 401 Unauthorized
  - `SessionCutoff_Returns401`: 401 Unauthorized
  - `NoToken_Returns401`: 401 Unauthorized
- **Test Integrity**: Utilized unique test identities per test function within `ProtectedEndpointIntegrationTests` to evade database race conditions and Foreign/Unique constraints during parallel test teardown.

## Exclusions Maintained
- Permission evaluation remains unimplemented.
- Admin-group mapping or RBAC remains unimplemented.
- The V0004 migration script was not created.
- The `auth/me` and `auth/logout-all` endpoints remain unimplemented.
- No historical changes were rewritten.
