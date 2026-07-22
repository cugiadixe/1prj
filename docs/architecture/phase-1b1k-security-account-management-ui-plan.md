# Phase 1B.1-K Security Account Management UI Foundation Plan

**Status:** PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Baseline:** `f4dddc03250d69b54b657ff32a1183e2caaed1a0`

**Previous completed phase:** Phase 1B.1-J COMPLETE

---

## 1. Purpose

Phase 1B.1-K establishes the frontend Account Management UI foundation. The goal is to provide security administrators with a browser-based interface to manage user authentication accounts using the backend Account Management APIs implemented in Phase 1B.1-I.

After Phase J, the frontend has a working login flow, authentication state management, route guards, and a minimal authenticated shell. The next logical step is to build the first security administration screen on this foundation, starting with Account Management — the most operationally critical security function (lock, unlock, disable, reset password, revoke sessions).

### Implementation authorization

**Phase 1B.1-K frontend implementation is NOT authorized.** This document is a planning proposal only. Implementation may not begin until:

1. The Project Owner reviews and accepts this plan (DEC-1B-K-01 through DEC-1B-K-07).
2. Blocker B2 (UserId → accountId mapping) is resolved — either by accepting detail-by-known-ID-only UI (DEC-1B-K-02 Option A) or by completing Phase K0 account discovery API first.
3. A plan acceptance document is committed.

The Account Management UI cannot be implemented as a usable administration screen until account discovery is resolved. Without a way to navigate from users to their auth accounts, the UI would require administrators to know internal database IDs — this is not an acceptable default for a production administration tool.

---

## 2. Confirmed current state

### Backend — Account Management API (Phase 1B.1-I)

All endpoints require authentication and `SECURITY_ACCOUNT_MANAGE` at `PermissionScope.Global`.

| Endpoint | Method | Purpose | Request body |
|---|---|---|---|
| `/api/v2/security/accounts/{accountId}` | GET | View auth account detail | — |
| `/api/v2/security/accounts/{accountId}/activate` | POST | Activate account | — |
| `/api/v2/security/accounts/{accountId}/disable` | POST | Disable account | `AccountReasonRequest` (Reason) |
| `/api/v2/security/accounts/{accountId}/lock` | POST | Lock account | `AccountReasonRequest` (Reason) |
| `/api/v2/security/accounts/{accountId}/unlock` | POST | Unlock account | — |
| `/api/v2/security/accounts/{accountId}/reset-password` | POST | Admin reset password | `AccountReasonRequest` (Reason) |
| `/api/v2/security/accounts/{accountId}/revoke-sessions` | POST | Revoke all sessions | `AccountReasonRequest` (Reason) |

### AccountDetailDto fields

| Field | Type |
|---|---|
| Id | long |
| UserId | long |
| ProviderType | string |
| Username | string |
| Status | string (ACTIVE / LOCKED / DISABLED) |
| IsInternalProvider | bool |
| FailedAttemptCount | int |
| IsManualLock | bool |
| LockoutEnd | DateTime? |
| MustChangePassword | bool |
| TemporaryPasswordExpiresAt | DateTime? |
| CreatedAt | DateTime |
| UpdatedAt | DateTime? |

### AdminResetPasswordDto response

| Field | Type |
|---|---|
| TemporaryPassword | string |

### Reason validation rules (backend-enforced)

- Reason required for: disable, lock, reset-password, revoke-sessions.
- Reason must not be empty or whitespace.
- Reason must not exceed 500 characters.
- Reason must not contain sensitive terms (password, token, secret, hash, security_stamp, key material).

### User listing API (Organization)

| Endpoint | Method | Permission | Purpose |
|---|---|---|---|
| `GET /api/v2/organizations/users` | GET | ORGANIZATION_USER_MANAGE, GLOBAL | Returns list of UserDto |
| `GET /api/v2/organizations/users/{id}` | GET | ORGANIZATION_USER_MANAGE, GLOBAL | Returns single UserDto |

**UserDto fields:** Id, EmployeeCode, FullName, Email, EmploymentStatus, AccountStatus, RowVersion, CreatedAt, UpdatedAt.

**Note:** No dedicated account list/search endpoint exists under `/api/v2/security/accounts`. Account detail requires a known `accountId` (the `User_Auth_Accounts.id` primary key). The organization user list (`GET /api/v2/organizations/users`) provides user listings including `AccountStatus`, but it returns `UserDto.Id` (the `Users.id` primary key), not the auth `accountId`. These are different identifiers from different tables. There is no API to map one to the other.

**Important:** `GET /api/v2/organizations/users` is gated by `ORGANIZATION_USER_MANAGE`, not `SECURITY_ACCOUNT_MANAGE`. It is not a complete account discovery solution for the Account Management UI because:
1. It requires a different permission than the account management actions themselves.
2. It does not return auth account IDs needed by the Account Management API.
3. It does not provide account-specific fields (ProviderType, IsManualLock, LockoutEnd, MustChangePassword, TemporaryPasswordExpiresAt, FailedAttemptCount).
4. Using it as a discovery mechanism creates a cross-permission dependency: an administrator with SECURITY_ACCOUNT_MANAGE but without ORGANIZATION_USER_MANAGE could not discover accounts.

The organization user list must not be treated as a substitute for a SECURITY_ACCOUNT_MANAGE-scoped account discovery API unless the Project Owner explicitly accepts that cross-permission dependency (see DEC-1B-K-02).

### No account list/search endpoint

There is no `GET /api/v2/security/accounts` list endpoint. The existing Account Management API is detail-and-action only, requiring a known `accountId`. To build a usable account management UI, the frontend needs a way to discover accounts.

### Backend — Authentication response

`LoginResponse` returns: `AccessToken`, `TokenType`, `ExpiresIn`, `ExpiresAtUtc`, `User` (UserId, Username, DisplayName), `MustChangePassword`.

**No permission data** is included in the login or refresh response. No `GET /api/v2/auth/my-permissions` endpoint exists.

### Backend — Effective permissions

`GET /api/v2/security/users/{userId}/effective-permissions?companyId={companyId}` exists but requires `SECURITY_ADMIN_MANAGE` — it is designed for administrators viewing other users' permissions, not for the authenticated user to query their own permissions.

### Frontend — Current state (Phase 1B.1-J)

| Component | Status |
|---|---|
| AuthProvider | Implemented — login, logout, refresh, change-password |
| ProtectedRoute | Implemented — redirects unauthenticated, forces change-password |
| AuthenticatedShell | Implemented — minimal layout with logout, Home, SystemHealth nav |
| axiosClient | Implemented — baseURL, Bearer token injection, 401 silent refresh |
| CSRF utility | Implemented — reads X-CSRF-TOKEN cookie, sends X-CSRF-Token header |
| In-memory auth state | Implemented — accessToken, mustChangePassword, user (userId, username, displayName) |
| Permission-aware routing | ABSENT — no permission data available to frontend |
| Permission-gated component visibility | ABSENT |
| Security admin pages | ABSENT |
| Account management UI | ABSENT |

### Frontend — Stack

React 19, TypeScript 6, Vite 8, Ant Design 6, TanStack Query v5, React Router DOM v7, React Hook Form 7, Zod 4, Vitest 4, React Testing Library 16.

---

## 3. Backend Account Management API discovery

### Endpoints confirmed

All 7 endpoints from Phase 1B.1-I are present and tested:
- `GET /api/v2/security/accounts/{accountId}` — returns `AccountDetailDto`
- `POST .../activate` — returns 204
- `POST .../disable` — requires reason, returns 204
- `POST .../lock` — requires reason, returns 204
- `POST .../unlock` — returns 204
- `POST .../reset-password` — requires reason, returns `AdminResetPasswordDto` (TemporaryPassword)
- `POST .../revoke-sessions` — requires reason, returns 204

### Error codes

The controller maps error codes to ProblemDetails responses:
- `AUTH_ACCOUNT_NOT_FOUND` → 404
- `AUTH_ACCOUNT_STATE_CONFLICT` → 409
- `AUTH_EXTERNAL_PASSWORD_MANAGED` → 409
- `AUTH_PASSWORD_REUSE` → 422
- `AUTH_PASSWORD_LENGTH_INVALID` → 422
- `AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT` → 422
- `AUTH_ACCOUNT_CONCURRENCY_CONFLICT` → 409
- Reason validation: `REASON_REQUIRED` → 400, `REASON_TOO_LONG` → 400, `REASON_CONTAINS_SENSITIVE_TERM` → 400

### Missing: Account list/search

No endpoint returns a list of accounts. The frontend cannot discover accounts by browsing `/api/v2/security/accounts`.

### Missing: AccountId ↔ UserId mapping — HARD BLOCKER

The Account Management API uses `accountId` (the `User_Auth_Accounts.id` primary key), not `userId`. The UserDto from the organization API returns `UserId` (the `Users.id` primary key). There is no API that maps a UserId to its auth accountId(s), nor does the user list return auth account IDs.

This is **Blocker B2 — HIGH / HARD BLOCKER** for a usable Account Management UI. Without this mapping:
- The frontend cannot navigate from any user-facing identifier to the account management detail.
- The frontend cannot build a user-to-account navigation flow.
- Administrators would need to know internal database primary keys from the `User_Auth_Accounts` table.
- No existing API returns `accountId` values that could be used to call the account management endpoints.

This blocker must be resolved before Phase K can produce a usable Account Management screen. See DEC-1B-K-02 for resolution options.

---

## 4. Frontend auth/routing discovery

### Auth client structure

- `axiosClient.ts` — axios instance with `baseURL = /api/v2`, Content-Type JSON
- `authApi.ts` — apiLogin, apiLogout, apiRefresh, apiChangePassword functions
- `authState.ts` — in-memory state store (accessToken, mustChangePassword, user)
- `csrf.ts` — reads X-CSRF-TOKEN cookie, exports CSRF_HEADER_NAME
- `AuthProvider.tsx` — React context wrapping auth state, bootstrap refresh, axios interceptors

### Route guard behavior

`ProtectedRoute` checks `isAuthenticated` and `mustChangePassword`:
- Bootstrapping → spinner
- Not authenticated → redirect `/login`
- mustChangePassword=true → redirect `/change-password`
- Otherwise → render children

No permission-based guard exists.

### AuthenticatedShell layout

Ant Design Layout with Header (PTKD ERP title, Home/SystemHealth menu, user display, logout button), Content (Outlet), Footer. Admin navigation menus are deferred.

### API client/interceptor pattern

AuthProvider installs axios request interceptor (injects Bearer token from in-memory state) and response interceptor (on 401 from non-auth endpoints, attempts silent refresh once, then clears auth on failure).

### Ant Design usage

Ant Design 6 is used for Layout, Menu, Button, Typography, Form, Input, Spin, Alert. Standard component library usage.

### Frontend test structure

Vitest + React Testing Library + jsdom. Test files colocated with source. 7 test suites, 35 tests. Tests mock axios and render components in isolation.

### Permission data availability to frontend

**Not available.** No frontend-accessible current-user permissions exist. The login/refresh response (`LoginResponse`) returns only `AccessToken`, `TokenType`, `ExpiresIn`, `ExpiresAtUtc`, `User` (UserId, Username, DisplayName), and `MustChangePassword`. No permission codes are included.

No `GET /api/v2/auth/my-permissions` endpoint exists. The existing `GET /api/v2/security/users/{userId}/effective-permissions` endpoint requires `SECURITY_ADMIN_MANAGE` and is designed for administrators viewing other users' permissions, not for self-query.

The frontend has no way to know what permissions the current user holds.

### Permission-gated UI feasibility

**Cannot be implemented now.** Frontend permission-gated navigation requires the backend to expose the current user's permissions to the frontend. Without this:
- The frontend cannot conditionally show/hide navigation items based on permissions.
- The frontend cannot disable action buttons based on the current user's permission set.
- Phase K must not invent frontend authorization logic. Backend remains the authoritative enforcement layer (SEC-01).

Two future options exist (both out of Phase K scope):
1. Add a `GET /api/v2/auth/my-permissions` endpoint (backend change — separate phase required).
2. Include permission claims in the JWT access token payload (backend change — separate phase required).

For Phase K, frontend permission-gated navigation must be deferred entirely. The UI relies on backend 403 enforcement and displays a "Permission denied" message when the user lacks the required permission.

### SECURITY_ACCOUNT_MANAGE in frontend payload

**Not present.** The login response contains no permission information. `SECURITY_ACCOUNT_MANAGE` is not available to the frontend in any form.

---

## 5. Proposed scope

### Frontend-only by default

Phase 1B.1-K is frontend-only, consuming existing Phase 1B.1-I Account Management APIs.

### In-scope

- Account Management page under authenticated shell at `/security/accounts/:accountId`.
- Account detail view displaying all safe AccountDetailDto fields.
- Account status display with color-coded badges (ACTIVE=green, LOCKED=orange, DISABLED=red).
- Warning banner when MustChangePassword=true.
- Warning banner when TemporaryPasswordExpiresAt is set and approaching/past expiry.
- Activate account action button.
- Disable account action with confirmation modal and required reason input.
- Lock account action with confirmation modal and required reason input.
- Unlock account action button.
- Admin reset password action with confirmation modal and required reason input.
- One-time temporary password display modal after successful reset (copy-and-close pattern).
- Revoke all sessions action with confirmation modal and required reason input.
- Sanitized error handling — map ProblemDetails error codes to user-friendly messages.
- Confirmation modals for all destructive/security-sensitive actions (disable, lock, reset-password, revoke-sessions).
- Route entry under AuthenticatedShell.
- API client functions for all 7 account management endpoints.
- Frontend tests for account detail rendering, action flows, reason validation, confirmation modals, temporary password display, error display, and route guard behavior.
- No logging of temporary passwords in browser console.
- No persisting of temporary passwords in any storage.

### Navigation placeholder

- Add a "Security" submenu or link in the AuthenticatedShell navigation bar for Account Management.
- This is a static link, not permission-gated (see DEC-1B-K-03).

---

## 6. Conditional scope

### Account navigation by User ID

If Blocker B2 (AccountId ↔ UserId mapping) is resolved by adding a backend endpoint or by adjusting the account detail endpoint to accept either accountId or userId, the UI can include a user-to-account navigation flow.

**Option A — Account detail by UserId:** If backend adds `GET /api/v2/security/accounts/by-user/{userId}`, the frontend can navigate from user list to account detail. This requires a small backend addition (out of Phase K frontend-only scope by default).

**Option B — Account detail by UserId lookup on existing endpoint:** If the existing `GET /api/v2/security/accounts/{accountId}` is modified to also accept userId, no new endpoint is needed. This requires backend change.

**Option C — Hardcoded navigation:** The frontend provides a manual account ID input field. The user must know the accountId to navigate. This is functional but poor UX.

**Option D — Mini-phase K0 backend addition:** A small Phase K0 adds `GET /api/v2/security/accounts/by-user/{userId}` and optionally `GET /api/v2/security/accounts` (list with pagination/filter). Phase K then builds UI on top.

### Account list page

Only if a backend list endpoint is added (Phase K0 or backend scope extension). Without it, the UI is detail-by-ID only.

---

## 7. Explicit out-of-scope

- Backend API changes unless blocker is accepted and approved.
- Account list/search page (unless backend list endpoint exists — see Blocker B1).
- Permission-gated navigation menus (no permission data available to frontend — see Blocker B3).
- Permission assignment UI.
- Role/group management UI.
- Audit viewer UI.
- Dynamic Approval Workflow.
- Business modules.
- AD/LDAP UI.
- Bulk import/export.
- Password forgot/self-service reset.
- Admin creation/bootstrap UI.
- Production dashboards.
- Audit export/reporting.
- Audit retention/archive/purge.
- Permission model redesign.
- SIEM integration.
- Schema migration.
- Rollback migration.
- PermissionCodes.cs change.
- permission-catalog.md change.

---

## 8. Account Management UI strategy

### Page structure

The Account Management UI consists of:

1. **Account Detail Page** (`/security/accounts/:accountId`) — displays account metadata and action buttons.
2. **Action buttons** — contextually enabled/disabled based on current account status:
   - ACTIVE account: Disable, Lock, Reset Password, Revoke Sessions available.
   - LOCKED account: Unlock, Disable, Reset Password, Revoke Sessions available.
   - DISABLED account: Activate available.
3. **Account ID input** — if no list endpoint exists, provide a text input for entering account ID directly (or integrate with user list navigation if Blocker B2 is resolved).

### Data flow

1. User navigates to `/security/accounts/:accountId`.
2. Frontend calls `GET /api/v2/security/accounts/{accountId}` with Bearer token.
3. On 200: render AccountDetailDto fields.
4. On 403: show "Permission denied" message.
5. On 404: show "Account not found" message.
6. On 401: axios interceptor handles refresh/redirect.

### Action flow

1. User clicks action button (e.g., "Disable Account").
2. Confirmation modal opens with reason textarea (for actions requiring reason).
3. User enters reason and confirms.
4. Frontend validates reason client-side (non-empty, under 500 chars).
5. Frontend calls the action endpoint with reason in request body.
6. On 204: show success notification, refetch account detail.
7. On error: map ProblemDetails to user-friendly message.

### Reset password flow

1. User clicks "Reset Password".
2. Confirmation modal opens with reason textarea and warning text.
3. User enters reason and confirms.
4. Frontend calls `POST .../reset-password`.
5. On 200: display `TemporaryPassword` in a one-time modal with copy button.
6. User clicks "Copy and Close" — modal closes, temporary password is cleared from component state.
7. Temporary password is never logged to console, never saved to storage.

---

## 9. Security-sensitive action strategy

### Confirmation modals

All destructive/security-sensitive actions require a confirmation modal before execution:

| Action | Requires reason | Confirmation text |
|---|---|---|
| Activate | No | "Are you sure you want to activate this account?" |
| Disable | Yes | "This will prevent the user from logging in. Enter a reason." |
| Lock | Yes | "This will lock the account. Enter a reason." |
| Unlock | No | "Are you sure you want to unlock this account?" |
| Reset Password | Yes | "This will generate a new temporary password and revoke all sessions. Enter a reason." |
| Revoke Sessions | Yes | "This will revoke all active sessions for this user. Enter a reason." |

### Reason validation (client-side)

- Non-empty, non-whitespace.
- Maximum 500 characters.
- Frontend validates before sending; backend re-validates.

### Self-action prevention

- Backend already prevents an admin from locking/disabling their own account (returns error).
- Frontend should display the backend error message if this occurs.
- Frontend does not need to prevent this client-side — backend is authoritative.

---

## 10. Temporary password display strategy

- After successful admin password reset, the backend returns `AdminResetPasswordDto` containing `TemporaryPassword`.
- The frontend displays the temporary password in a modal dialog exactly once.
- The modal provides a "Copy to clipboard" button and a "Close" button.
- On close, the temporary password is cleared from React component state.
- The temporary password is never written to:
  - `console.log` or any console method
  - `localStorage`
  - `sessionStorage`
  - cookies
  - URL parameters
  - browser history state
- Test must verify no console output containing the temporary password.

---

## 11. Permission/navigation strategy

### Current limitation

The frontend has no permission data. The login/refresh response does not include permission codes. No `my-permissions` endpoint exists.

### Recommended approach for Phase K

- **Do not implement permission-gated navigation.** Backend remains the sole enforcer of permissions.
- Add a static "Security" or "Account Management" link in the AuthenticatedShell navigation. All authenticated users can see the link.
- If a user without `SECURITY_ACCOUNT_MANAGE` navigates to the account management page, the API call will return 403. The UI should display a "Permission denied" message.
- This is consistent with SEC-01: "No endpoint relies only on UI visibility for authorization."

### Future improvement (not Phase K)

A future phase should add `GET /api/v2/auth/my-permissions` to allow the frontend to conditionally show/hide navigation items based on the current user's effective permissions. This is a backend change and requires a separate phase.

---

## 12. Error handling strategy

### ProblemDetails mapping

The frontend maps backend error codes to user-friendly messages:

| Error code | User-facing message |
|---|---|
| AUTH_ACCOUNT_NOT_FOUND | "Account not found." |
| AUTH_ACCOUNT_STATE_CONFLICT | "This action cannot be performed on the account in its current state." |
| AUTH_EXTERNAL_PASSWORD_MANAGED | "Password for this account is managed externally." |
| AUTH_PASSWORD_REUSE | "The generated password matches a recently used password. Please try again." |
| AUTH_PASSWORD_LENGTH_INVALID | "Password does not meet length requirements. Please try again." |
| AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT | "Password contains a disallowed pattern. Please try again." |
| AUTH_ACCOUNT_CONCURRENCY_CONFLICT | "The account was modified by another user. Please refresh and try again." |
| REASON_REQUIRED | "A reason is required for this action." |
| REASON_TOO_LONG | "Reason must not exceed 500 characters." |
| REASON_CONTAINS_SENSITIVE_TERM | "Reason must not contain sensitive terms." |

### 403 handling

If the API returns 403 (no `SECURITY_ACCOUNT_MANAGE`), the page displays a "You do not have permission to manage accounts" message instead of the account detail.

### Network/server errors

Generic "An error occurred. Please try again." for unexpected 500 errors. No raw exception details displayed.

---

## 13. Test strategy

| Layer | Coverage target |
|---|---|
| Component | Account detail page renders all AccountDetailDto fields correctly |
| Component | Status badges show correct colors for ACTIVE/LOCKED/DISABLED |
| Component | MustChangePassword warning banner appears when true |
| Component | Action buttons are contextually enabled/disabled by account status |
| Component | Confirmation modal opens on action click |
| Component | Reason textarea validates non-empty and max length |
| Component | Temporary password modal displays once and clears on close |
| Component | No console.log of temporary password (spy test) |
| Component | Error messages render correctly for each ProblemDetails error code |
| Component | 403 renders permission denied message |
| Component | 404 renders account not found message |
| Integration | Account detail page loads and displays data from mocked API |
| Integration | Action flow: click → confirm → API call → success notification → refetch |
| Integration | Route guard: unauthenticated user redirected to /login |
| Regression | All existing 35 frontend tests remain passing |

---

## 14. Security risks

| Risk | Severity | Mitigation |
|---|---|---|
| Temporary password displayed in browser | Medium | Display once in modal, clear from state on close, no console logging, no storage |
| Temporary password copied to clipboard | Low | Clipboard is user-initiated action; acceptable UX pattern; same as bootstrap flow |
| Account management link visible to non-admins | Low | Backend enforces 403; frontend shows permission denied; no data exposed |
| CSRF on account management actions | None | All actions use Bearer token (not cookie-reliant); CSRF not required for Bearer-only endpoints |
| XSS in reason field | Low | Ant Design components escape output by default; reason is sent to backend, not rendered as HTML |
| Browser console exposes account detail | Low | AccountDetailDto contains only safe fields (no password hash, no security stamp); acceptable |

---

## 15. Required Project Owner decisions

**DEC-1B-K-01 — Phase shape:**
Should Phase K be frontend-only Account Management UI using existing Phase 1B.1-I APIs?

Recommended: Yes, frontend-only. Phase K remains planning-only until account discovery (Blocker B2) is resolved. Backend changes only if a blocker is accepted and approved. Implementation is not authorized until this plan is accepted and B2 resolution path is decided.

---

**DEC-1B-K-02 — Account discovery/navigation:**
How should the frontend discover accounts to manage? This decision gates whether Phase K can produce a usable administration screen.

- Option A: Detail-by-known-accountId only. User enters the internal `User_Auth_Accounts.id` manually. No list page. Functional but operationally limited — administrators must obtain account IDs through direct database access or API tooling. Not recommended as the primary admin UI, but acceptable as an explicitly approved interim if no backend change is authorized.
- Option B: Navigate from organization user list (`GET /api/v2/organizations/users`). Requires resolving Blocker B2 (UserId → accountId mapping) via a new backend endpoint. Also introduces a cross-permission dependency (ORGANIZATION_USER_MANAGE + SECURITY_ACCOUNT_MANAGE). Small backend addition needed.
- Option C: Open Phase 1B.1-K0 — Account Management Discovery API. K0 adds a SECURITY_ACCOUNT_MANAGE-scoped discovery contract, such as `GET /api/v2/security/accounts/by-user/{userId}` and/or `GET /api/v2/security/accounts` (list with pagination/filter). Phase K then builds full UI on top of a proper discovery API.

Recommended: Option C (Phase K0 first). This provides the cleanest permission boundary, avoids cross-permission dependency, and delivers a usable account discovery contract scoped to SECURITY_ACCOUNT_MANAGE. K0 should decide and implement one of:
- `GET /api/v2/security/accounts?search=...` (list/search with pagination)
- `GET /api/v2/security/accounts/by-user/{userId}` (lookup by user ID)
- Another approved SECURITY_ACCOUNT_MANAGE-scoped discovery contract

Option A is acceptable only if the Project Owner explicitly approves detail-by-known-ID-only UI as a deliberate interim measure. This must be a conscious PO decision, not a default.

---

**DEC-1B-K-03 — Permission-gated UI:**
Should Phase K implement frontend permission-gated navigation?

- Option A: No permission gating. All authenticated users see the "Account Management" link. Backend enforces 403 on API calls. Frontend displays "Permission denied" when 403 is returned.
- Option B: Defer Phase K until a `GET /api/v2/auth/my-permissions` endpoint is added, then implement permission-gated navigation.

Recommended: Option A. No frontend-accessible current-user permissions exist. No `/api/v2/auth/my-permissions` endpoint exists. Frontend permission-gated navigation must be deferred. Backend remains the authoritative enforcement layer (SEC-01). Phase K must not invent frontend authorization logic.

---

**DEC-1B-K-04 — Security-sensitive action UX:**
Should disable, lock, reset password, and revoke sessions require confirmation modal and reason input?

Recommended: Yes. Confirmation modal plus required reason input for disable, lock, reset password, and revoke sessions. Consistent with backend reason validation (DEC-1B-I-07) and SEC-003.

---

**DEC-1B-K-05 — Temporary password display:**
Should temporary password be displayed once after admin reset password?

Recommended: Yes. Display once in modal with copy button. Do not log to console. Do not persist to any storage. Clear from component state on modal close. Consistent with DEC-1B-I-03.

---

**DEC-1B-K-06 — Audit visibility:**
Should the Account Management UI show audit event links or audit history?

Recommended: No. Defer audit history/links to Audit Viewer UI phase.

---

**DEC-1B-K-07 — Backend changes:**
Should backend changes be allowed in Phase K?

Recommended: No backend changes in Phase K. Use Phase K0 for the account discovery API backend addition. Phase K is frontend-only, consuming APIs that exist after K0 (or after I if Option A detail-by-ID is accepted). Stop and request PO approval before any backend change.

---

## 16. Blockers, if any

| ID | Blocker | Severity | Options |
|---|---|---|---|
| B1 | No account list/search endpoint exists under `/api/v2/security/accounts` | Medium | (a) Detail-by-ID only UI. (b) Phase K0 adds list endpoint. |
| B2 | **HARD BLOCKER.** No UserId → accountId mapping API exists. The account detail endpoint requires `accountId` (User_Auth_Accounts.id), but the user list returns `UserId` (Users.id). No existing API returns accountId values. Frontend cannot navigate from any user-facing identifier to account management. Administrators would need to know internal database primary keys. | **HIGH** | (a) Phase K0 adds `GET /api/v2/security/accounts/by-user/{userId}` (recommended). (b) Modify existing detail endpoint to accept userId as alternative. (c) Manual accountId entry only (not recommended as primary admin UI — must be explicitly approved by PO as DEC-1B-K-02 Option A). |
| B3 | No `my-permissions` endpoint exists. Frontend cannot know the current user's permissions. Permission-gated navigation is impossible without backend change. | Low for Phase K | Defer permission-gated navigation. Backend enforces 403. |

**B2 is the critical hard blocker for a usable Account Management UI.** Without it, the Account Management UI requires administrators to know the internal `User_Auth_Accounts.id` primary key, which is not discoverable through any existing frontend flow or API. The organization user list shows `UserId` (from the `Users` table), but the account management API requires `accountId` (from the `User_Auth_Accounts` table). These are different identifiers from different tables with no existing mapping API.

**Recommended resolution:** Open Phase 1B.1-K0 — Account Management Discovery API before Phase K implementation begins. K0 should add a SECURITY_ACCOUNT_MANAGE-scoped discovery contract (e.g., `GET /api/v2/security/accounts/by-user/{userId}` and/or `GET /api/v2/security/accounts` with search/pagination). This is a small, well-bounded backend addition that follows the existing AccountManagementService pattern.

**Alternative resolution:** The Project Owner may explicitly accept detail-by-known-accountId-only UI (DEC-1B-K-02 Option A). This is operationally limited and not recommended as the primary administration interface, but it is functional and avoids any backend change. This must be a conscious PO decision recorded in DEC-1B-K-02.

---

## 17. Recommended implementation slices

### If Blocker B2 is resolved (Phase K0 completed first):

| Slice | Deliverable |
|---|---|
| K-1 | Account Management API client: TypeScript functions for all 7 account management endpoints + the by-user lookup. Types for AccountDetailDto, AdminResetPasswordDto, AccountReasonRequest, ProblemDetails error codes. |
| K-2 | Account detail page: route at `/security/accounts/:accountId`, renders all AccountDetailDto fields, status badges, warning banners. Error handling for 403/404. Tests. |
| K-3 | Account actions — activate and unlock: action buttons, confirmation modals (no reason required), API integration, success notification, refetch. Tests. |
| K-4 | Account actions — disable, lock, revoke-sessions: confirmation modals with reason textarea, client-side reason validation, API integration, error mapping. Tests. |
| K-5 | Admin reset password: confirmation modal with reason, API integration, one-time temporary password display modal, copy button, state cleanup on close. Console spy test verifying no password logging. Tests. |
| K-6 | Navigation and routing integration: add Security/Account Management link to AuthenticatedShell. Add user-to-account navigation flow (user list → account detail via by-user lookup). Route setup. Tests. |
| K-7 | Regression and final: all existing 35 frontend tests remain passing. Build passes. Lint passes. |

### If Blocker B2 is NOT resolved (detail-by-ID only):

| Slice | Deliverable |
|---|---|
| K-1 | Account Management API client: TypeScript functions for all 7 endpoints. Types. |
| K-2 | Account detail page with manual account ID input: route at `/security/accounts`, user enters accountId, fetches detail. Status badges, warning banners, error handling. Tests. |
| K-3 | Account actions — activate and unlock. Tests. |
| K-4 | Account actions — disable, lock, revoke-sessions with reason. Tests. |
| K-5 | Admin reset password with temporary password display. Tests. |
| K-6 | Navigation link in AuthenticatedShell. Route setup. Tests. |
| K-7 | Regression and final. |

---

## 18. Acceptance criteria

| ID | Criterion |
|---|---|
| K-AC-01 | Account detail page at `/security/accounts/:accountId` renders all AccountDetailDto fields. |
| K-AC-02 | Account status is displayed with color-coded badge (ACTIVE=green, LOCKED=orange, DISABLED=red). |
| K-AC-03 | Warning banner appears when MustChangePassword=true. |
| K-AC-04 | Warning banner appears when TemporaryPasswordExpiresAt is set. |
| K-AC-05 | Activate action is available when account status is DISABLED. Returns to ACTIVE on success. |
| K-AC-06 | Disable action requires confirmation modal and reason input. Available when account status is ACTIVE or LOCKED. |
| K-AC-07 | Lock action requires confirmation modal and reason input. Available when account status is ACTIVE. |
| K-AC-08 | Unlock action is available when account status is LOCKED. Returns to ACTIVE on success. |
| K-AC-09 | Admin reset password requires confirmation modal and reason input. Displays temporary password exactly once in a modal with copy button. |
| K-AC-10 | Temporary password is never logged to console, never saved to localStorage/sessionStorage/cookies. |
| K-AC-11 | Revoke all sessions requires confirmation modal and reason input. |
| K-AC-12 | Reason validation: non-empty, max 500 characters, validated client-side before submission. |
| K-AC-13 | After successful action, account detail is refetched and UI reflects updated status. |
| K-AC-14 | ProblemDetails error codes are mapped to user-friendly messages. |
| K-AC-15 | 403 response renders "Permission denied" message. |
| K-AC-16 | 404 response renders "Account not found" message. |
| K-AC-17 | Account Management link is visible in authenticated navigation. |
| K-AC-18 | npm run build passes with 0 errors and 0 TypeScript errors. |
| K-AC-19 | npm test passes with 0 failures. All existing tests remain passing. |
| K-AC-20 | npm run lint passes with 0 errors. |
| K-AC-21 | No backend changes unless explicitly approved via blocker resolution. |
| K-AC-22 | No PermissionCodes.cs change. |
| K-AC-23 | No permission-catalog.md change. |
| K-AC-24 | No database migration. |
| K-AC-25 | No rollback migration. |

---

*Document prepared from direct code inspection of HEAD `f4dddc03250d69b54b657ff32a1183e2caaed1a0` on 2026-07-22.*
*No source code, tests, migrations, or committed documents were modified during the preparation of this plan.*
