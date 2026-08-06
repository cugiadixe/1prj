# Phase 1B.1-M Current Company Context and X-Company-Id Frontend Foundation Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

Baseline:
24660c7e7b6e959a9f336ede406e56cf3becd834

Previous completed phase:
Phase 1B.1-L COMPLETE

## 1. Purpose
The purpose of Phase 1B.1-M is to establish the frontend foundation for handling the current company context and passing `X-Company-Id` to company-scoped backend endpoints. This enables users to select their active company and allows the UI to render COMPANY-scoped permissions securely.

## 2. Confirmed current state
- Backend API endpoints requiring `PermissionScope.Company` expect an `X-Company-Id` header.
- `PermissionAuthorizationFilter` safely rejects missing (400), malformed (400), and unauthorized (403) `X-Company-Id` requests.
- `GET /api/v2/auth/me/permissions` successfully returns combined GLOBAL and COMPANY permissions when an `X-Company-Id` is provided.
- Frontend permission state is memory-only.
- Account Management navigation is gated by the `SECURITY_ACCOUNT_MANAGE` GLOBAL permission.

## 3. Backend company-context discovery
- **Requires X-Company-Id:** Any endpoint annotated with `[RequirePermission(..., PermissionScope.Company)]`.
- **Missing Header:** Returns 400 Bad Request ("Missing Company Context").
- **Malformed Header:** Returns 400 Bad Request ("Malformed Company Context").
- **Unauthorized Context:** Returns 403 Forbidden ("You do not have the required permissions or company access.") via `PermissionEvaluator`.
- **Current User Companies Endpoint:** There is currently NO backend endpoint that lists the selectable companies for the authenticated user. The existing `GET /api/v2/organizations/companies` requires `ORGANIZATION_COMPANY_MANAGE` and is for system-wide administration.
- **Login/Refresh Context:** `LoginResponse` does not include company access information.

## 4. Frontend company-context discovery
- **Company State:** The frontend currently lacks any active company context state.
- **Axios Client:** The Axios client in `api.ts` can be configured to attach `X-Company-Id`, but currently does not.

## 5. Proposed backend scope
Because discovery found no current-user accessible companies endpoint, the plan proposes a minimal backend read-only endpoint:

`GET /api/v2/auth/me/companies`

Accepted proposed behavior:
- Requires authenticated user.
- Returns 401 when unauthenticated.
- Does not require a separate permission code because the user reads their own selectable company context.
- Returns only companies the current user may select.
- Does not expose assignment internals.
- Does not expose role/group/department internals.
- Does not expose raw SQL, audit payloads, raw exceptions, token/session/security stamp material.
- Does not emit read audit or switch audit event in Phase M.
- Does not require schema migration.
- Does not require new permission code.
- Does not require PermissionCodes.cs change.
- Does not require permission-catalog.md change.

Recommended response shape:
```json
{
  "companies": [
    {
      "companyId": 1,
      "companyCode": "...",
      "companyName": "...",
      "isDefault": false
    }
  ]
}
```
*(If `isDefault` cannot be determined from existing data, it should be omitted or always false rather than inventing business meaning.)*

## 6. Proposed frontend scope
- Add memory-only current company state/provider.
- Store selected company object or selected companyId in memory only.
- Fetch the list of selectable companies after a successful login or refresh (when `mustChangePassword = false`).
- Clear selected company on logout.
- Clear selected company on refresh failure.
- Clear selected company on auth clear.
- Clear selected company after password change.
- Do not store selected company in localStorage.
- Do not store selected company in sessionStorage.
- Do not store selected company in cookies.
- Do not encode selected company in URL in Phase M.

## 7. X-Company-Id attachment strategy
- Attach `X-Company-Id` only for company-scoped API clients/requests.
- Do not attach `X-Company-Id` globally to all axios requests.
- Do not attach `X-Company-Id` to auth/global endpoints unless that endpoint explicitly accepts company context, such as `GET /api/v2/auth/me/permissions` after company selection.
- Missing company context for company-scoped actions should produce safe UI handling, not silent fallback.
- Backend remains authoritative.

## 8. Company selector UX
- AuthenticatedShell may show a company selector when the user has one or more selectable companies.
- If exactly one company is returned, the app may auto-select it in memory.
- If multiple companies are returned, the user selects the current company.
- If no selectable companies are returned, company-scoped UI should remain unavailable or show a safe empty state.
- Account Management remains GLOBAL-only and should not depend on selected company.

## 9. Permission refresh on company change
- After selecting/switching company, frontend refetches `GET /api/v2/auth/me/permissions` with `X-Company-Id`.
- Permission state remains memory-only.
- GLOBAL permissions remain valid.
- COMPANY-scoped advisory gating may be enabled only for current-company context.
- Backend 403 remains mandatory.

## 10. Error handling strategy
- 401 from company endpoint clears auth/company/permission state according to existing auth behavior.
- 400 missing/malformed company context from company-scoped APIs is shown as sanitized company-context error.
- 403 unauthorized company context is shown as sanitized unauthorized message.
- Raw backend details are not displayed.

## 11. Required Project Owner decisions

**DEC-1B-M-01 — Phase shape:**
*Recommended: backend minimal current-user companies endpoint plus frontend company context foundation.*

**DEC-1B-M-02 — Company source:**
*Recommended: GET /api/v2/auth/me/companies.*

**DEC-1B-M-03 — Current company storage:**
*Recommended: memory-only.*

**DEC-1B-M-04 — X-Company-Id attachment:**
*Recommended: only for company-scoped API clients/requests; not global axios default.*

**DEC-1B-M-05 — Company selector UX:**
*Recommended: show selector in authenticated shell when selectable companies exist.*

**DEC-1B-M-06 — Permission refresh on company change:**
*Recommended: refetch GET /api/v2/auth/me/permissions with X-Company-Id.*

**DEC-1B-M-07 — COMPANY-scoped UI gating:**
*Recommended: advisory only for current-company context; backend remains authoritative.*

**DEC-1B-M-08 — Persistence:**
*Recommended: no localStorage/sessionStorage/cookie persistence in Phase M.*

**DEC-1B-M-09 — Audit:**
*Recommended: no read/switch audit event in Phase M.*

**DEC-1B-M-10 — Permission catalog:**
*Recommended: no new permission code, no PermissionCodes.cs change, no permission-catalog.md change.*

## 12. Blockers, if any
None. No schema changes, no PermissionCodes.cs modifications, and no permission-catalog.md modifications are required.

## 13. Recommended implementation slices
1. **Backend Foundation:** Implement `GET /api/v2/auth/me/companies` and its accompanying tests.
2. **Frontend State:** Introduce `CompanyProvider` and logic to fetch companies post-login.
3. **Frontend Selector:** Build the company selector in the `AuthenticatedShell` and wire up the permission refetch logic.
4. **Client Interceptor:** Configure API clients to attach `X-Company-Id` based on the selected company state.

## 14. Acceptance criteria
- GET /api/v2/auth/me/companies is implemented only if accepted by Project Owner.
- Endpoint is authenticated and returns only selectable companies for the current user.
- Endpoint does not expose assignment internals.
- Current company state is memory-only.
- X-Company-Id is attached only to company-scoped API requests.
- GET /api/v2/auth/me/permissions can be refetched with X-Company-Id after company switch.
- Account Management remains SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Company context clears with auth state.
- No localStorage/sessionStorage/cookie persistence.
- Backend remains authoritative.
- No schema migration.
- No rollback migration.
- No new permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.

PHASE 1B.1-M PLAN ACCEPTED � SEE phase-1b1m-project-owner-plan-acceptance.md
