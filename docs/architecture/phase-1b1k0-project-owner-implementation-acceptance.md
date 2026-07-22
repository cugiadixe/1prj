# Phase 1B.1-K0 Project Owner Implementation Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-K0 IMPLEMENTATION ACCEPTED
PHASE 1B.1-K0 CLOSURE REVIEW PASSED — SEE phase-1b1k0-final-closure-review.md

**Accepted phase:**
Phase 1B.1-K0 — Account Management Discovery API

**Accepted plan commit:**
d3007d7fd90c3e9055db451b32468a0d3315a8c5

**Accepted plan acceptance commit:**
a7245512f575b7e5ad0ab8b7bf1318c66cf77b4b

**Accepted implementation commit:**
5426acd2809377c690545f96385e536756c8796d

**Implementation acceptance baseline:**
5426acd2809377c690545f96385e536756c8796d

---

## Accepted implementation files

- src/backend/PTKD.Application/Security/AccountManagement/DTOs/AccountSummaryDto.cs
- src/backend/PTKD.Application/Security/AccountManagement/AccountSearchParameters.cs
- src/backend/PTKD.Application/Security/AccountManagement/IAccountManagementService.cs
- src/backend/PTKD.Infrastructure/Security/AccountManagement/AccountManagementService.cs
- src/backend/PTKD.Api/Controllers/Security/AccountsController.cs
- tests/backend/PTKD.ApiTests/Security/AccountDiscoveryApiTests.cs

---

## Accepted API contract

### GET /api/v2/security/accounts

- Returns `PagedResult<AccountSummaryDto>`.
- Supports query parameters: `search`, `status`, `providerType`, `page`, `pageSize`.
- Uses deterministic ordering (by `UserAuthAccount.Id` ascending).
- Uses read-only query behavior (EF Core `AsNoTracking()` with LINQ `.Select()` projection).
- Default page=1, pageSize=20, max pageSize=100.
- Controller validates page >= 1 (returns 400 `PAGE_INVALID`) and 1 <= pageSize <= 100 (returns 400 `PAGE_SIZE_INVALID`).
- Search matches across ProviderSubject, EmployeeCode, and FullName using `.Contains()`.
- Status and ProviderType filters use exact match.

### GET /api/v2/security/accounts/by-user/{userId:long}

- Returns account summaries for a specific user (`AccountSummaryDto[]`).
- Returns 403 when caller lacks SECURITY_ACCOUNT_MANAGE.
- Returns 404 with `USER_NOT_FOUND` when `userId` does not exist.
- Returns 200 with empty array (`[]`) when user exists but has no auth accounts.
- Returns 200 with `AccountSummaryDto[]` when user has auth accounts.
- Uses sanitized `ProblemDetails` for all error responses.

---

## Accepted permission and scope

- Uses existing `SECURITY_ACCOUNT_MANAGE` permission.
- Uses `PermissionScope.Global` through existing `AccountsController` class-level `[RequirePermission]` attribute.
- No new permission code.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- Backend remains the authoritative authorization layer.

---

## Accepted data exposure

### Allowed response fields

- AccountId (`UserAuthAccount.Id`) — resolves Blocker B2
- UserId (`UserAuthAccount.UserId`) — FK to Users
- Username (`UserAuthAccount.ProviderSubject`) — login name
- ProviderType (`UserAuthAccount.ProviderType`) — INTERNAL or external provider name
- Status (`UserAuthAccount.AuthAccountStatus`) — ACTIVE/LOCKED/DISABLED
- MustChangePassword (`UserAuthAccount.MustChangePassword`)
- EmployeeCode (`User.EmployeeCode`) — from joined User record
- FullName (`User.FullName`) — from joined User record
- EmploymentStatus (`User.EmploymentStatus`) — from joined User record
- CreatedAt (`UserAuthAccount.CreatedAt`)
- UpdatedAt (`UserAuthAccount.UpdatedAt`)

### Forbidden fields confirmed excluded

- PasswordHash — credential material
- SecurityStamp — session invalidation secret
- RefreshToken material — session credential material
- PasswordHistories — credential history collection
- SessionsInvalidatedAt — internal session management
- RowVersion — concurrency token
- User.Email — potentially sensitive under NĐ 13/2023
- Raw exception details — never exposed in any API response
- SQL text — never exposed in any error response
- Stack traces — never exposed in any error response

---

## Accepted route safety

- `GET /api/v2/security/accounts/by-user/{userId:long}` uses the literal segment `by-user` — cannot conflict with `GET /api/v2/security/accounts/{accountId:long}` because `by-user` is not parseable as `long`.
- `GET /api/v2/security/accounts` (no path parameter) is distinct from `GET /api/v2/security/accounts/{accountId:long}` (requires a path segment).
- Existing account detail route (`GET /api/v2/security/accounts/{accountId:long}`) remains valid and functional.
- Existing account action routes (`POST {accountId:long}/activate`, `disable`, `lock`, `unlock`, `reset-password`, `revoke-sessions`) remain valid and unaffected.

---

## Accepted audit behavior

- No account-view or account-discovery audit event was added.
- No `SECURITY_ACCOUNT_VIEWED` event.
- No `ACCOUNT_LIST_ACCESSED` event.
- No new audit event type.
- This follows DEC-1B-K0-08.
- Consistent with the existing `GET /api/v2/security/accounts/{accountId}` detail endpoint (no read audit event).

---

## Accepted test evidence

- Build passed: 0 warnings, 0 errors.
- ApiTests passed: 229/229.
- UnitTests passed: 133/133.
- IntegrationTests passed: 196/196.
- DatabaseSafety passed: 17/17.
- Grand total: 558 tests, 0 failed, 0 skipped.
- 18 new AccountDiscovery API tests added in `AccountDiscoveryApiTests.cs`.
- Existing 49 `AccountsControllerApiTests` tests passed without regression.
- Response JSON verified to not contain: passwordHash, password_hash, securityStamp, security_stamp, rowVersion, sessionsInvalidatedAt, email.

---

## Accepted exclusions

- No frontend implementation.
- No Phase K UI implementation.
- No schema migration.
- No rollback migration.
- No schema change.
- No new permission code.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- No audit event addition.
- No Dapper introduction.
- No business module changes.

---

## Phase K status

- Phase 1B.1-K frontend implementation remains blocked until Phase 1B.1-K0 is closed and finally accepted.
- This implementation acceptance does not by itself authorize Phase K UI implementation.
- Phase K requires: K0 closure review → K0 final acceptance → Phase K implementation authorization.

---

## Implementation acceptance conclusion

PHASE 1B.1-K0 IMPLEMENTATION ACCEPTED — READY FOR CLOSURE REVIEW
