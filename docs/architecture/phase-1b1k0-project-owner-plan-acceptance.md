# Phase 1B.1-K0 Project Owner Plan Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-K0 PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan commit:**
d3007d7fd90c3e9055db451b32468a0d3315a8c5

**Accepted baseline:**
2e5d85cbe1aad8cdde6605db76b2b5bf85b292fd

**Accepted phase:**
Phase 1B.1-K0 — Account Management Discovery API

**Related blocked phase:**
Phase 1B.1-K — Security Account Management UI Foundation (PLAN ACCEPTED; IMPLEMENTATION NOT AUTHORIZED)

---

## Accepted reason

- Phase 1B.1-K identified Blocker B2 (UserId → accountId mapping) as a HIGH / hard blocker for the Account Management UI.
- Existing Account Management action APIs (Phase 1B.1-I) require `accountId` (`User_Auth_Accounts.id`) as the path parameter.
- No existing API returns `accountId` values. The organization user list returns `userId` (`Users.id`), which is a different identifier from a different table.
- The frontend Account Management UI cannot safely proceed without a SECURITY_ACCOUNT_MANAGE-scoped account discovery contract.
- Phase 1B.1-K implementation remains blocked until K0 is implemented, tested, accepted by the Project Owner, and formally closed.

---

## Accepted implementation shape

- Backend-only read-only Account Management discovery API.
- No frontend implementation in K0.
- No schema migration.
- No rollback migration.
- No new permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No permission model change.

---

## Accepted API contract

### GET /api/v2/security/accounts

- Returns `PagedResult<AccountSummaryDto>`.
- Supports query parameters: `search`, `status`, `providerType`, `page`, `pageSize`.
- Uses deterministic ordering (by `UserAuthAccount.Id` ascending).
- Uses read-only query behavior (EF Core `AsNoTracking()` with LINQ `.Select()` projection).
- Default page=1, pageSize=20, max pageSize=100 (following existing `PagedResult` conventions).

### GET /api/v2/security/accounts/by-user/{userId}

- Returns account summaries for a specific user (`AccountSummaryDto[]`).
- Returns 403 when caller lacks SECURITY_ACCOUNT_MANAGE.
- Returns 404 with `USER_NOT_FOUND` when `userId` does not exist.
- Returns empty array (200 with `[]`) when user exists but has no auth accounts.
- Uses sanitized `ProblemDetails` for all error responses.

---

## Accepted route safety

- `GET /api/v2/security/accounts/by-user/{userId:long}` uses the literal segment `by-user` — this cannot conflict with `GET /api/v2/security/accounts/{accountId:long}` because `by-user` is not parseable as `long`.
- `GET /api/v2/security/accounts` (no path parameter) is distinct from `GET /api/v2/security/accounts/{accountId:long}` (requires a path segment).
- Existing Account Management action routes (`{accountId:long}/activate`, etc.) are POST methods and are not affected.
- Explicit `:long` route constraint must be used on `by-user/{userId:long}`.

---

## Accepted permission and scope

- Use existing `SECURITY_ACCOUNT_MANAGE` permission.
- Use GLOBAL scope.
- Do not introduce a new permission code.
- `PermissionCodes.cs` remains unchanged.
- `permission-catalog.md` remains unchanged.
- Backend remains the authoritative authorization layer. No frontend permission gating.

---

## Accepted data exposure

### Allowed (available from current model and accepted as display-safe)

- accountId (`UserAuthAccount.Id`) — resolves Blocker B2
- userId (`UserAuthAccount.UserId`) — FK to Users
- username / providerSubject (`UserAuthAccount.ProviderSubject`) — login name; accepted as display-safe
- providerType (`UserAuthAccount.ProviderType`) — INTERNAL or external provider name
- status / accountStatus (`UserAuthAccount.AuthAccountStatus`) — ACTIVE/LOCKED/DISABLED
- mustChangePassword (`UserAuthAccount.MustChangePassword`)
- employeeCode (`User.EmployeeCode`) — from joined User record
- fullName (`User.FullName`) — from joined User record
- employmentStatus (`User.EmploymentStatus`) — from joined User record
- createdAt (`UserAuthAccount.CreatedAt`)
- updatedAt (`UserAuthAccount.UpdatedAt`)

Additional fields from existing AccountDetailDto that may be added to AccountSummaryDto if the Project Owner amends DEC-1B-K0-04:
- temporaryPasswordExpiresAt — operational, non-sensitive
- lockoutEnd / lockedUntilUtc — operational, non-sensitive
- failedAttemptCount / failedAccessCount — operational, non-sensitive

If these fields are needed in the list/summary view, they may be included without schema change. This decision is deferred to implementation unless the Project Owner specifies inclusion here.

### Forbidden

- PasswordHash — credential material
- SecurityStamp — session invalidation secret
- SessionsInvalidatedAt — internal session management
- RowVersion — concurrency token (not approved for discovery)
- PasswordHistories — credential history collection
- User.Email — potentially sensitive under NĐ 13/2023
- Refresh tokens — session credential material
- Raw audit payload — not part of account discovery contract
- Raw exception details — never exposed in any API response
- SQL text, stack traces — never exposed in any error response

---

## Accepted audit decision

- Read-only discovery must not emit a new security audit event in K0.
- Do not add `SECURITY_ACCOUNT_VIEWED`, `ACCOUNT_LIST_ACCESSED`, or any similar event without separate Project Owner approval.
- This follows the same pattern as the existing `GET /api/v2/security/accounts/{accountId}` detail endpoint (no read audit event).

---

## Accepted decisions

**DEC-1B-K0-01 — Phase shape:**
Approved: Backend-only Account Management discovery API. No frontend. No schema change.

---

**DEC-1B-K0-02 — Discovery contract:**
Approved: Both list/search (`GET /api/v2/security/accounts`) and by-user lookup (`GET /api/v2/security/accounts/by-user/{userId}`), if possible without schema migration. Minimum acceptable is by-user lookup only if list/search becomes unexpectedly blocked during implementation.

---

**DEC-1B-K0-03 — Permission:**
Approved: Existing `SECURITY_ACCOUNT_MANAGE` at GLOBAL scope. No new permission code.

---

**DEC-1B-K0-04 — Data exposure:**
Approved: Non-sensitive operational account summary fields only. Credential and session internals are forbidden. Specific field set defined in "Accepted data exposure" above.

---

**DEC-1B-K0-05 — Pagination:**
Approved: Pagination for list/search endpoint using existing `PagedResult<T>` model. Default page=1, pageSize=20, max pageSize=100.

---

**DEC-1B-K0-06 — Schema:**
Approved: No schema migration. Stop and report blocker to the Project Owner if schema change becomes required during implementation.

---

**DEC-1B-K0-07 — Frontend:**
Approved: No frontend work in K0. Phase K UI resumes only after K0 implementation is complete, accepted by the Project Owner, and formally closed.

---

**DEC-1B-K0-08 — Audit:**
Approved: No read audit event in K0. No `SECURITY_ACCOUNT_VIEWED` or similar event without separate Project Owner approval.

---

## Implementation authorization

**Phase 1B.1-K0 backend-only implementation may begin after this Project Owner plan acceptance is committed.**

Implementation constraints:
- Stop and request Project Owner approval if schema change is required.
- Stop and request Project Owner approval if a new permission code is required.
- Stop and request Project Owner approval if backend changes outside the K0 discovery scope appear necessary.
- Do not begin Phase K frontend implementation. Phase K remains blocked.

**Phase 1B.1-K frontend implementation remains blocked until K0 is implemented, tested, accepted by the Project Owner, and formally closed.**

PHASE 1B.1-K0 PLAN ACCEPTED — IMPLEMENTATION AUTHORIZED FOR BACKEND DISCOVERY API
