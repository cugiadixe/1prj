# Phase 1B.1-K0 Account Management Discovery API Plan

**Status:** PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Baseline:** `2e5d85cbe1aad8cdde6605db76b2b5bf85b292fd`

**Previous completed phase:** Phase 1B.1-J COMPLETE (Login UI and MustChangePassword UI Foundation)

**Related blocked phase:** Phase 1B.1-K — Security Account Management UI Foundation (PLAN ACCEPTED; IMPLEMENTATION NOT AUTHORIZED)

**Backend dependency:** Phase 1B.1-I — Account Management API Hardening (COMPLETE)

---

## 1. Purpose

Phase 1B.1-K0 adds backend-only read-only discovery endpoints to the Account Management API surface. These endpoints allow the frontend to discover which auth accounts exist and navigate from a user identifier to an account identifier, resolving Blocker B2 identified in the Phase 1B.1-K plan.

### Why K0 exists

The Phase 1B.1-K plan review identified Blocker B2 as a HARD BLOCKER: the Account Management UI cannot be built as a usable administration screen because no API exists to discover `accountId` values. The existing Phase 1B.1-I endpoints all require `accountId` (the `User_Auth_Accounts.id` primary key), but no existing API returns this value. The organization user list returns `UserId` (the `Users.id` primary key), which is a different identifier from a different table. Without K0, administrators would need to know internal database primary keys to manage accounts.

DEC-1B-K-02 accepted Option C: open Phase 1B.1-K0 before Phase K implementation begins.

### Implementation authorization

**Phase 1B.1-K0 implementation is NOT authorized.** This document is a planning proposal only. Implementation may not begin until:

1. The Project Owner reviews and accepts this plan (DEC-1B-K0-01 through DEC-1B-K0-08).
2. A plan acceptance document is committed.

---

## 2. Confirmed blocker from Phase K

**Blocker B2 — HARD BLOCKER (HIGH):**

The Account Management API (Phase 1B.1-I) provides 7 endpoints, all under `GET/POST /api/v2/security/accounts/{accountId}`. The `accountId` parameter is `User_Auth_Accounts.id` — the auto-increment primary key of the auth accounts table.

The organization user list (`GET /api/v2/organizations/users`) returns `UserDto` with `Id` = `Users.id`. This is a different primary key from a different table. No existing API maps between these two identifiers.

Additionally, the organization user list requires `ORGANIZATION_USER_MANAGE` permission, not `SECURITY_ACCOUNT_MANAGE`. Using it as an account discovery mechanism creates a cross-permission dependency: a security account administrator would need both `SECURITY_ACCOUNT_MANAGE` AND `ORGANIZATION_USER_MANAGE` to navigate to accounts — which is architecturally incorrect.

**Resolution:** K0 adds SECURITY_ACCOUNT_MANAGE-scoped discovery endpoints that return `accountId` values, enabling the frontend to navigate to the existing Phase 1B.1-I detail/action endpoints.

---

## 3. Current backend Account Management API discovery

### Existing endpoints (Phase 1B.1-I)

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/api/v2/security/accounts/{accountId}` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Account detail |
| POST | `/api/v2/security/accounts/{accountId}/activate` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Activate |
| POST | `/api/v2/security/accounts/{accountId}/disable` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Disable |
| POST | `/api/v2/security/accounts/{accountId}/lock` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Lock |
| POST | `/api/v2/security/accounts/{accountId}/unlock` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Unlock |
| POST | `/api/v2/security/accounts/{accountId}/reset-password` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Admin reset password |
| POST | `/api/v2/security/accounts/{accountId}/revoke-sessions` | SECURITY_ACCOUNT_MANAGE (GLOBAL) | Revoke all sessions |

### Existing AccountDetailDto fields

| Field | Type | Source |
|---|---|---|
| Id | long | UserAuthAccount.Id |
| UserId | long | UserAuthAccount.UserId |
| ProviderType | string | UserAuthAccount.ProviderType |
| Username | string | UserAuthAccount.ProviderSubject |
| Status | string | UserAuthAccount.AuthAccountStatus |
| IsInternalProvider | bool | Computed from ProviderType |
| FailedAttemptCount | int | UserAuthAccount.FailedAttemptCount |
| IsManualLock | bool | Computed from status + LockoutEnd |
| LockoutEnd | DateTime? | UserAuthAccount.LockoutEnd |
| MustChangePassword | bool | UserAuthAccount.MustChangePassword |
| TemporaryPasswordExpiresAt | DateTime? | UserAuthAccount.TemporaryPasswordExpiresAt |
| CreatedAt | DateTime | UserAuthAccount.CreatedAt |
| UpdatedAt | DateTime? | UserAuthAccount.UpdatedAt |

### What is missing

- No `GET /api/v2/security/accounts` (list/search).
- No `GET /api/v2/security/accounts/by-user/{userId}` (by-user lookup).
- No way for the frontend to discover `accountId` values without direct database access.

---

## 4. Current data model discovery

### UserAuthAccount entity

Located at `src/backend/PTKD.Domain/Entities/UserAuthAccount.cs`.

Key fields relevant to discovery:

| Property | Type | Notes |
|---|---|---|
| Id | long | PK (auto-increment). This is the `accountId` used by all Phase I endpoints. |
| UserId | long | FK to Users.Id. **The mapping that resolves B2.** |
| ProviderType | string | "INTERNAL" or external provider name. |
| ProviderSubject | string | Login name / provider subject. |
| AuthAccountStatus | string | ACTIVE, LOCKED, or DISABLED. |
| FailedAttemptCount | int | Current failed attempt count. |
| LockoutEnd | DateTime? | Timed lockout expiry. |
| MustChangePassword | bool | Forced password change flag. |
| TemporaryPasswordExpiresAt | DateTime? | Temp password expiry. |
| CreatedAt | DateTime | Creation timestamp. |
| UpdatedAt | DateTime? | Last update timestamp. |
| User | User (nav) | **Navigation property to Users table — already configured in EF.** |

Sensitive fields NOT exposed (and must remain unexposed):
- PasswordHash, SecurityStamp, SessionsInvalidatedAt, RowVersion, PasswordHistories.

### User entity

Located at `src/backend/PTKD.Domain/Entities/User.cs`.

Key fields for search/display:

| Property | Type | Notes |
|---|---|---|
| Id | long | PK. This is the `userId`. |
| EmployeeCode | string | Employee identifier (searchable). |
| FullName | string | Display name (searchable). |
| Email | string? | Email address. |
| EmploymentStatus | string | ACTIVE, PROBATION, SUSPENDED, etc. |
| AccountStatus | string | User-level account status. |

### EF relationship

`UserAuthAccountConfiguration.cs` line 81:
```
builder.HasOne(account => account.User)
    .WithMany()
    .HasForeignKey(account => account.UserId)
    .OnDelete(DeleteBehavior.Restrict)
    .HasConstraintName("FK_UserAuthAccounts_User");
```

Index on UserId: `IX_UserAuthAccounts_UserId` (line 79).

**Conclusion: The join from UserAuthAccount to User is already configured.** No schema change is needed. Discovery queries can use `.Include(a => a.User)` or LINQ projection to access user fields.

### IAuthenticationDbContext

Located at `src/backend/PTKD.Application/Security/Authentication/Interfaces/IAuthenticationDbContext.cs`.

Exposes:
- `DbSet<UserAuthAccount> UserAuthAccounts`
- `DbSet<User> Users`

Both tables are accessible from the same context. Discovery queries can join across them without a new DbContext.

### Existing pagination model

`PagedResult<T>` at `src/backend/PTKD.Application/Common/Models/PagedResult.cs`:
- `Page` (int)
- `PageSize` (int)
- `TotalCount` (long)
- `Items` (IEnumerable<T>)

Used by `SecurityAuditController` with `SecurityAuditQueryParameters` (Page default=1, PageSize default=50). This is the established pagination pattern.

### Query style

AccountManagementService uses EF Core with `IAuthenticationDbContextFactory`:
- Read operations: `AsNoTracking()` + `FirstOrDefaultAsync()`
- Write operations: `SERIALIZABLE` transaction with `UPDLOCK, HOLDLOCK` via raw SQL

Discovery is read-only and should follow the `AsNoTracking()` pattern.

---

## 5. Proposed API contract

### Route disambiguation

The existing `AccountsController` is routed at `api/v2/security/accounts` with existing endpoints using `{accountId:long}` route constraints. The new K0 endpoints must not conflict:

- `GET /api/v2/security/accounts` (list/search) — no path parameter; distinguished from `GET /api/v2/security/accounts/{accountId:long}` by the absence of a path segment.
- `GET /api/v2/security/accounts/by-user/{userId:long}` — uses the literal segment `by-user` before the parameter; cannot conflict with `{accountId:long}` because `by-user` is not parseable as `long`.
- Existing action routes (`{accountId:long}/activate`, `{accountId:long}/disable`, etc.) are POST methods with additional path segments — no conflict with GET endpoints.

Both `accountId` and `userId` are `long` (BIGINT). The `:long` route constraint is already used on existing endpoints and must be preserved on the new `by-user/{userId:long}` route.

Implementation must verify that no existing account management action route is broken by the addition of the new endpoints. The existing `[HttpGet("{accountId:long}")]` detail endpoint must continue to function unchanged.

### Endpoint 1: Account list/search

```
GET /api/v2/security/accounts
```

**Permission:** SECURITY_ACCOUNT_MANAGE at GLOBAL scope (same controller-level attribute as existing endpoints).

**Query parameters:**

| Parameter | Type | Default | Notes |
|---|---|---|---|
| search | string? | null | Searches across username (ProviderSubject), employee code, and full name. Case-insensitive. |
| status | string? | null | Filter by AuthAccountStatus: ACTIVE, LOCKED, DISABLED. |
| providerType | string? | null | Filter by ProviderType: INTERNAL, or external provider names. |
| page | int | 1 | 1-based page number. |
| pageSize | int | 20 | Items per page. Max 100. |

**Response:** `PagedResult<AccountSummaryDto>`

**AccountSummaryDto fields:**

| Field | Type | Source | Notes |
|---|---|---|---|
| AccountId | long | UserAuthAccount.Id | The key that resolves B2 — frontend can use this to call detail/action endpoints. |
| UserId | long | UserAuthAccount.UserId | FK to Users. |
| Username | string | UserAuthAccount.ProviderSubject | Login name. |
| ProviderType | string | UserAuthAccount.ProviderType | INTERNAL or external. |
| Status | string | UserAuthAccount.AuthAccountStatus | ACTIVE/LOCKED/DISABLED. |
| MustChangePassword | bool | UserAuthAccount.MustChangePassword | Flag. |
| EmployeeCode | string | User.EmployeeCode | From joined User record. |
| FullName | string | User.FullName | From joined User record. |
| EmploymentStatus | string | User.EmploymentStatus | From joined User record. |
| CreatedAt | DateTime | UserAuthAccount.CreatedAt | Account creation. |
| UpdatedAt | DateTime? | UserAuthAccount.UpdatedAt | Last update. |

**Not included in AccountSummaryDto (sensitive — same exclusions as AccountDetailDto):**
- PasswordHash, SecurityStamp, SessionsInvalidatedAt, RowVersion, PasswordHistories, FailedAttemptCount, LockoutEnd, TemporaryPasswordExpiresAt, IsManualLock, IsInternalProvider.

Note: FailedAttemptCount, LockoutEnd, TemporaryPasswordExpiresAt, IsManualLock, and IsInternalProvider are available on the existing AccountDetailDto. They are omitted from the summary to keep the list endpoint lightweight. The frontend navigates to the detail endpoint for full account information.

**Ordering:** Default order by `UserAuthAccount.Id` ascending (stable, indexed).

**Error responses:**

| Status | Error code | Condition |
|---|---|---|
| 400 | INVALID_PAGE | page < 1 |
| 400 | INVALID_PAGE_SIZE | pageSize < 1 or pageSize > 100 |
| 400 | INVALID_STATUS_FILTER | status value is not ACTIVE/LOCKED/DISABLED |
| 403 | AUTH_PERMISSION_DENIED | Missing SECURITY_ACCOUNT_MANAGE |
| 401 | (standard) | Unauthenticated |

### Endpoint 2: By-user lookup

```
GET /api/v2/security/accounts/by-user/{userId}
```

**Permission:** SECURITY_ACCOUNT_MANAGE at GLOBAL scope.

**Path parameter:** `userId` (long) — the `Users.id` value.

**Response:** `AccountSummaryDto[]` (array, not paged — a user typically has 1-2 auth accounts: one internal, possibly one external).

**Rationale for array:** A user may have multiple auth accounts (e.g., one INTERNAL and one external provider). The frontend should show all accounts for the given user.

**Error responses:**

| Status | Error code | Condition |
|---|---|---|
| 404 | USER_NOT_FOUND | No User record exists with the given userId |
| 403 | AUTH_PERMISSION_DENIED | Missing SECURITY_ACCOUNT_MANAGE |
| 401 | (standard) | Unauthenticated |

Note: If the user exists but has no auth accounts, return an empty array (200 with `[]`), not 404. 404 is reserved for "the user itself does not exist."

All error responses use sanitized `ProblemDetails` format consistent with the existing `AccountsController.BuildProblem()` helper. No raw exception details, SQL text, or stack traces are exposed in any error response.

---

## 6. Permission and scope strategy

- Both discovery endpoints use `SECURITY_ACCOUNT_MANAGE` at `GLOBAL` scope.
- This is the same permission and scope used by the existing 7 endpoints on `AccountsController`.
- No new permission code is introduced. `PermissionCodes.cs` is unchanged.
- `permission-catalog.md` is unchanged.
- No COMPANY-scoped discovery in K0. All account management remains GLOBAL-scoped, consistent with Phase 1B.1-I (DEC-1B-I-04).
- Backend remains the authoritative permission enforcement layer. No frontend permission gating.

### Permission consistency

The discovery endpoints join to the `Users` table for display fields (EmployeeCode, FullName, EmploymentStatus). This is a read projection, not an organization user management operation. It does NOT require `ORGANIZATION_USER_MANAGE` — the security administrator is reading user identity fields as part of account management, not managing the user record. This avoids the cross-permission dependency identified in Phase K Blocker B2.

---

## 7. Data exposure strategy

### Allowed fields

Account fields already accepted as safe in Phase 1B.1-I AccountDetailDto:
- Id, UserId, ProviderType, Username (ProviderSubject), Status (AuthAccountStatus), MustChangePassword, CreatedAt, UpdatedAt.

User fields that are non-sensitive display/search data:
- EmployeeCode, FullName, EmploymentStatus.

### Detail-only fields (available on AccountDetailDto, omitted from summary)

These fields exist on the current model and are accepted as non-sensitive in Phase 1B.1-I AccountDetailDto, but are omitted from AccountSummaryDto to keep the list endpoint lightweight. The frontend navigates to `GET /api/v2/security/accounts/{accountId}` for full information:

- FailedAttemptCount — operational detail, not needed for list browsing.
- LockoutEnd — operational detail, relevant only when viewing a specific locked account.
- TemporaryPasswordExpiresAt — operational detail, relevant only when viewing a specific account after admin reset.
- IsManualLock — computed from status + LockoutEnd, meaningful only in detail context.
- IsInternalProvider — computable from ProviderType, meaningful only in detail context.

If the Project Owner determines that any of these fields should appear in the summary list (e.g., to enable filtering locked accounts by lockout expiry), this can be added to AccountSummaryDto without schema change. This would be a DEC-1B-K0-04 amendment.

### Absolutely forbidden (never exposed in any discovery response)

Enforced by LINQ `.Select()` projection (not `.Include()`):
- PasswordHash — credential material. Must never leave the database layer except for verification.
- SecurityStamp — session invalidation secret. Exposure would allow session forgery.
- SessionsInvalidatedAt — internal session management timestamp.
- RowVersion — concurrency token (not needed for read-only discovery).
- PasswordHistories — credential history collection.
- User.Email — not needed for account discovery; potentially sensitive under NĐ 13/2023.
- User.AccountStatus — user-level status; distinct from auth account status and would create confusion.
- Raw audit payload — not part of the account discovery contract.
- Raw exception details — never exposed in any API response.

### Projection strategy

Use LINQ `.Select()` projection directly in the query, not `.Include()`. This ensures:
1. Only specified fields are loaded from the database.
2. Sensitive fields never enter application memory.
3. The generated SQL only selects projected columns.

---

## 8. Pagination/filtering strategy

### List endpoint

- Use the existing `PagedResult<T>` model.
- Page is 1-based (consistent with SecurityAuditQueryParameters).
- Default PageSize = 20. Maximum PageSize = 100.
- Search is case-insensitive and matches across ProviderSubject, User.EmployeeCode, and User.FullName using `EF.Functions.Like` or `.Contains()` with case-insensitive collation (SQL Server default collation is case-insensitive).
- Status filter is exact match on AuthAccountStatus.
- ProviderType filter is exact match on ProviderType.
- Filters are AND-combined (search AND status AND providerType).
- TotalCount is computed with `.CountAsync()` before paging.
- Results are ordered by `UserAuthAccount.Id` ascending (stable, avoids secondary sort).
- Paging uses `.Skip((page - 1) * pageSize).Take(pageSize)`.

### By-user endpoint

- Not paginated (users typically have 1-2 auth accounts).
- Returns all accounts for the given userId.
- Ordered by `UserAuthAccount.Id` ascending.

### Query parameters DTO

Create `AccountSearchParameters` following the `SecurityAuditQueryParameters` pattern:

```csharp
public class AccountSearchParameters
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? ProviderType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

---

## 9. Error handling strategy

Follow the existing `AccountsController` pattern:
- Use `ProblemDetails` for all error responses.
- Use `BuildProblem()` helper for consistent error format.
- Error codes use stable string constants.
- No raw exception details in responses.
- No SQL text in responses.
- No stack traces in responses.

New error codes for K0:

| Error code | HTTP status | Condition |
|---|---|---|
| INVALID_PAGE | 400 | Page < 1 |
| INVALID_PAGE_SIZE | 400 | PageSize < 1 or > 100 |
| INVALID_STATUS_FILTER | 400 | Status is not ACTIVE/LOCKED/DISABLED |
| USER_NOT_FOUND | 404 | By-user lookup: no User record with given userId |

Existing error codes reused:
- AUTH_PERMISSION_DENIED (403) — handled by RequirePermission attribute.
- 401 — handled by [Authorize] attribute.

---

## 10. Test strategy

### API tests (PTKD.ApiTests)

Add `AccountDiscoveryApiTests.cs` covering:

**Authorization:**
- List: unauthenticated returns 401.
- List: without SECURITY_ACCOUNT_MANAGE returns 403.
- By-user: unauthenticated returns 401.
- By-user: without SECURITY_ACCOUNT_MANAGE returns 403.

**List endpoint:**
- Returns 200 with PagedResult containing known test accounts.
- Default pagination (page=1, pageSize=20) works.
- Explicit page/pageSize parameters work.
- Search by username filters correctly.
- Search by employee code filters correctly.
- Search by full name filters correctly.
- Status filter returns only matching status.
- ProviderType filter returns only matching type.
- Combined filters (search + status) work.
- Empty result set returns 200 with empty Items and TotalCount=0.
- Invalid page returns 400 with INVALID_PAGE.
- Invalid pageSize returns 400 with INVALID_PAGE_SIZE.
- Invalid status filter returns 400 with INVALID_STATUS_FILTER.
- Response JSON does not contain passwordHash, securityStamp, rowVersion, sessionsInvalidatedAt.
- AccountId in response matches actual UserAuthAccount.Id.
- UserId in response matches actual UserAuthAccount.UserId.
- EmployeeCode and FullName in response match the joined User record.

**By-user endpoint:**
- Returns 200 with array of accounts for known user.
- Returns 200 with empty array if user exists but has no auth accounts.
- Returns 404 with USER_NOT_FOUND if userId does not exist.
- Response JSON does not contain sensitive fields.
- Returns all accounts for a user with multiple accounts (if test data supports it).

**Data safety:**
- Response JSON for both endpoints does not contain passwordHash, password_hash, securityStamp, security_stamp, rowVersion, sessionsInvalidatedAt.
- No error response exposes StackTrace, SELECT, or SqlException.

### Integration tests (PTKD.IntegrationTests)

- AccountSummaryDto projection does not load sensitive fields.
- Pagination TotalCount is accurate.
- Search is case-insensitive.

### Unit tests (PTKD.UnitTests)

- AccountSearchParameters validation logic.
- Query builder logic if extracted to a separate method.

### Regression

- All existing 35 frontend tests remain passing (npm test).
- All existing backend tests remain passing.
- Build passes with 0 errors.

---

## 11. Security risks

| Risk | Mitigation |
|---|---|
| Sensitive account fields in discovery response | LINQ projection selects only allowed fields. API test verifies no sensitive field names in JSON. |
| Enumeration of all auth accounts | SECURITY_ACCOUNT_MANAGE at GLOBAL scope is required — only authorized security administrators can list accounts. This is consistent with the existing detail endpoint. |
| Cross-permission information leak (User fields) | EmployeeCode, FullName, EmploymentStatus are non-sensitive display fields. Email is excluded. The security administrator needs these to identify which account belongs to which person. |
| SQL injection via search parameter | EF Core parameterized queries. No raw SQL for search. |
| Denial of service via large page sizes | PageSize capped at 100. |
| Timing attacks on by-user endpoint | 404 for non-existent user is acceptable because the endpoint requires SECURITY_ACCOUNT_MANAGE — the caller is already a trusted administrator. |

---

## 12. Explicit out-of-scope

- Frontend Account Management UI (Phase K). **Phase K UI implementation remains blocked until K0 is implemented, tested, accepted by the Project Owner, and formally closed. K0 completion does not automatically authorize Phase K implementation — Phase K requires its own separate implementation authorization after K0 closure.**
- Permission Assignment UI.
- Audit Viewer UI.
- Dynamic Approval Workflow.
- Business modules.
- Schema migration.
- Rollback migration.
- New permission code in PermissionCodes.cs.
- permission-catalog.md modification.
- Write operations (create/update/delete accounts).
- Account creation API.
- Bulk import/export.
- AD/LDAP integration.
- Password forgot/self-service reset.
- Audit events for read operations.
- COMPANY-scoped discovery.
- Frontend permission-gated navigation.
- `my-permissions` endpoint.

---

## 13. Required Project Owner decisions

**DEC-1B-K0-01 — Phase shape:**
Should K0 be backend-only Account Management discovery API?

Recommended: Yes. K0 adds read-only discovery endpoints only. No frontend. No schema change. Resolves Blocker B2 from Phase K.

---

**DEC-1B-K0-02 — Discovery contract:**
Should K0 implement list/search, by-user lookup, or both?

- Option A: List/search only (`GET /api/v2/security/accounts`). Frontend can search and browse accounts. Does not provide direct userId-to-accountId navigation.
- Option B: By-user lookup only (`GET /api/v2/security/accounts/by-user/{userId}`). Frontend can navigate from user to account. Does not provide browsable account list.
- Option C: Both list/search and by-user lookup.

Recommended: Option C (both). The data model supports both without schema change. List/search enables browsing the full account list. By-user lookup enables direct navigation from user context. Both use the same AccountSummaryDto projection. Implementation cost is marginal — both are read-only EF queries on the same tables.

---

**DEC-1B-K0-03 — Permission:**
Should discovery endpoints use existing SECURITY_ACCOUNT_MANAGE at GLOBAL scope?

Recommended: Yes. This is the same permission used by all existing account management endpoints. No new permission code is needed. PermissionCodes.cs remains unchanged. permission-catalog.md remains unchanged.

---

**DEC-1B-K0-04 — Data exposure:**
Which fields should AccountSummaryDto expose?

Recommended: AccountId, UserId, Username, ProviderType, Status, MustChangePassword, EmployeeCode, FullName, EmploymentStatus, CreatedAt, UpdatedAt. Never expose PasswordHash, SecurityStamp, SessionsInvalidatedAt, RowVersion, PasswordHistories, or User.Email.

---

**DEC-1B-K0-05 — Pagination:**
Should the account list/search endpoint be paginated?

Recommended: Yes. Use the existing `PagedResult<T>` model. Default page=1, pageSize=20, max pageSize=100. Consistent with SecurityAuditController.

---

**DEC-1B-K0-06 — Schema:**
Should K0 allow schema migration?

Recommended: No. The existing schema supports discovery without modification. UserAuthAccount already has UserId FK with index. User entity already has EmployeeCode, FullName, EmploymentStatus. Stop and report blocker if schema change is discovered during implementation.

---

**DEC-1B-K0-07 — Frontend:**
Should K0 include frontend UI work?

Recommended: No. K0 is backend-only. Phase K UI resumes after K0 acceptance/closure. Frontend implementation is separately authorized.

---

**DEC-1B-K0-08 — Audit:**
Should read-only discovery endpoints emit security audit events?

Recommended: No. Read-only discovery is not a security-sensitive action. The existing `GET /api/v2/security/accounts/{accountId}` detail endpoint does not emit audit events. Discovery follows the same pattern. Audit events are reserved for state-changing operations (activate, disable, lock, unlock, reset-password, revoke-sessions) as established in Phase 1B.1-I.

Do not add `SECURITY_ACCOUNT_VIEWED`, `ACCOUNT_LIST_ACCESSED`, or any similar read-audit event without separate Project Owner approval. Read-audit for account discovery is not required by any existing decision and would create noise in the Security_Audit_Events table.

---

## 14. Blockers, if any

| ID | Blocker | Severity | Notes |
|---|---|---|---|
| (none) | — | — | No blockers identified. The existing data model, EF configuration, pagination model, and permission infrastructure support K0 without schema change, new permissions, or new dependencies. |

Discovery findings that confirm no blockers:
1. `UserAuthAccount.User` navigation property is already configured (`UserAuthAccountConfiguration.cs` line 81).
2. `IX_UserAuthAccounts_UserId` index already exists (`UserAuthAccountConfiguration.cs` line 79).
3. `IAuthenticationDbContext` exposes both `UserAuthAccounts` and `Users` DbSets.
4. `PagedResult<T>` pagination model already exists at `PTKD.Application.Common.Models`.
5. `SECURITY_ACCOUNT_MANAGE` already exists in `PermissionCodes.cs` (added in Phase 1B.1-I per DEC-1B-I-04).
6. Seeded in V0003 migration.
7. AccountsController already uses controller-level `[RequirePermission]` — new endpoints on the same controller inherit this.

---

## 15. Recommended implementation slices

| Slice | Deliverable |
|---|---|
| K0-1 | AccountSummaryDto and AccountSearchParameters: new DTO for list/search results, query parameters DTO. Place in `PTKD.Application.Security.AccountManagement.DTOs`. |
| K0-2 | IAccountManagementService extension: add `SearchAccountsAsync(AccountSearchParameters, CancellationToken)` returning `PagedResult<AccountSummaryDto>`, and `GetAccountsByUserIdAsync(long userId, CancellationToken)` returning `IReadOnlyList<AccountSummaryDto>`. |
| K0-3 | AccountManagementService implementation: EF Core `AsNoTracking()` queries with LINQ `.Select()` projection joining UserAuthAccount to User. Pagination using `.Skip()/.Take()`. Search using `.Contains()` or `EF.Functions.Like()`. Status and ProviderType exact match filters. |
| K0-4 | AccountsController endpoints: `[HttpGet]` for list/search (returning `PagedResult<AccountSummaryDto>`), `[HttpGet("by-user/{userId:long}")]` for by-user lookup (returning `AccountSummaryDto[]`). Input validation for page, pageSize, status filter. |
| K0-5 | API tests: `AccountDiscoveryApiTests.cs` covering authorization (401/403), list/search (pagination, search, filters, empty results, validation errors), by-user lookup (found, empty, not found), data safety (no sensitive fields in JSON). |
| K0-6 | Integration tests: projection safety, pagination accuracy, case-insensitive search. |
| K0-7 | Regression and final: all existing backend tests pass. Build passes. No frontend change. |

---

## 16. Acceptance criteria

| ID | Criterion |
|---|---|
| K0-AC-01 | `GET /api/v2/security/accounts` returns `PagedResult<AccountSummaryDto>` with paginated account list. |
| K0-AC-02 | `GET /api/v2/security/accounts/by-user/{userId}` returns `AccountSummaryDto[]` for the given user. |
| K0-AC-03 | Both endpoints require SECURITY_ACCOUNT_MANAGE at GLOBAL scope. |
| K0-AC-04 | Unauthenticated requests return 401. |
| K0-AC-05 | Requests without SECURITY_ACCOUNT_MANAGE return 403. |
| K0-AC-06 | AccountSummaryDto contains AccountId (resolving B2), UserId, Username, ProviderType, Status, MustChangePassword, EmployeeCode, FullName, EmploymentStatus, CreatedAt, UpdatedAt. |
| K0-AC-07 | AccountSummaryDto does NOT contain PasswordHash, SecurityStamp, SessionsInvalidatedAt, RowVersion, PasswordHistories, or User.Email. |
| K0-AC-08 | API test verifies response JSON does not contain sensitive field names (passwordHash, securityStamp, rowVersion, sessionsInvalidatedAt). |
| K0-AC-09 | Search is case-insensitive across Username, EmployeeCode, and FullName. |
| K0-AC-10 | Status filter accepts ACTIVE/LOCKED/DISABLED and rejects invalid values with 400. |
| K0-AC-11 | Pagination uses existing `PagedResult<T>` model. Default page=1, pageSize=20, max pageSize=100. |
| K0-AC-12 | Invalid page (<1) returns 400 with INVALID_PAGE error code. |
| K0-AC-13 | Invalid pageSize (<1 or >100) returns 400 with INVALID_PAGE_SIZE error code. |
| K0-AC-14 | By-user lookup returns 404 with USER_NOT_FOUND if userId does not exist. |
| K0-AC-15 | By-user lookup returns 200 with empty array if user exists but has no auth accounts. |
| K0-AC-16 | No schema migration. |
| K0-AC-17 | No PermissionCodes.cs change. |
| K0-AC-18 | No permission-catalog.md change. |
| K0-AC-19 | No frontend implementation. |
| K0-AC-20 | No audit events emitted by read-only discovery endpoints. |
| K0-AC-21 | No error response exposes StackTrace, SELECT, or SqlException. |
| K0-AC-22 | Backend build passes with 0 errors and 0 warnings (excluding existing warnings). |
| K0-AC-23 | All existing backend tests remain passing. |
| K0-AC-24 | No rollback migration. |
| K0-AC-25 | EF Core queries use AsNoTracking() and LINQ projection (not .Include()). |

---

*Document prepared from direct code inspection of HEAD `2e5d85cbe1aad8cdde6605db76b2b5bf85b292fd` on 2026-07-22.*
*No source code, tests, migrations, or committed documents were modified during the preparation of this plan.*
