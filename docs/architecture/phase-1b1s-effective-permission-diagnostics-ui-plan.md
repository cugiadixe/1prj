# Phase 1B.1-S Effective Permission Diagnostics UI Plan

**Status:** PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
**Baseline:** 96ee586850ad67f65252ed0732cedf7f9cf40b90
**Previous completed phase:** Phase 1B.1-R COMPLETE

---

## 1. Purpose

Provide a dedicated read-only diagnostics page for security administrators to inspect the final effective permission set for any user, alongside contextual information from each authorization source. This completes the verification counterpart to the administration UIs delivered in Phases N through R.

---

## 2. Current authorization administration coverage

All components of the authorization formula are now administrable through UI:

| # | Capability | Phase | Status |
|---|-----------|-------|--------|
| 1 | Account Management | 1B.1-K (+ K0 discovery) | COMPLETE |
| 2 | Individual Permission Assignment | 1B.1-N | COMPLETE |
| 3 | Security Audit Viewer | 1B.1-O | COMPLETE |
| 4 | Role Permission Management | 1B.1-P1 | COMPLETE |
| 5 | Admin Group Permission Management | 1B.1-P2 | COMPLETE |
| 6 | User Role Assignment | 1B.1-Q1 | COMPLETE |
| 7 | User Admin Group Membership | 1B.1-Q2 | COMPLETE |
| 8 | Department Baseline Permission Management | 1B.1-R | COMPLETE |

Formula: `DepartmentBaseAllow ∪ RoleCompanyAllow ∪ EffectiveIndividualAllow - EffectiveIndividualDeny`

---

## 3. Remaining gaps

| # | Gap | Status |
|---|-----|--------|
| 1 | Effective Permission Diagnostics UI | This phase |
| 2 | Authorization Matrix / Security Overview | Deferred (requires backend) |
| 3 | ENTITY scope | Deferred |
| 4 | DENY on roles/departments | Not supported |
| 5 | Bulk assignment | Deferred |
| 6 | Export/download | Deferred |
| 7 | Workflow approval for security changes | Deferred |

---

## 4. Backend/API discovery findings

### 4.1 EffectivePermissionsController

- **Endpoint:** `GET /api/v2/security/users/{userId:long}/effective-permissions?companyId={companyId}`
- **Gate:** `SECURITY_ADMIN_MANAGE` GLOBAL
- **Response:** `EffectivePermissionsResponse(long UserId, long? CompanyId, IReadOnlyList<string> PermissionCodes)`
- **Source attribution:** NOT SUPPORTED. Returns flat permission codes only (per OD-D-B-11). No per-permission source breakdown. No indication of which source (department/role/admin-group/individual) contributed each code.
- **Denied permissions:** NOT INCLUDED. Only the final effective set after DENY subtraction is returned.
- **CompanyId behavior:** Optional query parameter. When provided, returns company-scoped effective permissions. When omitted, returns global effective permissions.

### 4.2 Reusable APIs for contextual side-by-side display

All require `SECURITY_ADMIN_MANAGE` GLOBAL:

| API | Path | Returns |
|-----|------|---------|
| Account search (K0) | `GET /api/v2/security/accounts?search=` | `PagedResult<AccountSummaryDto>` — user lookup with userId, fullName, employeeCode |
| Individual permissions | `GET /api/v2/security/users/{userId}/individual-permissions` | `UserIndividualPermissionDto[]` — includes grantType ALLOW/DENY, scopeType, companyId |
| User role assignments | `GET /api/v2/security/users/{userId}/role-assignments` | `UserRoleAssignmentDto[]` — includes roleId, roleCode, roleName |
| User admin group assignments | `GET /api/v2/security/users/{userId}/admin-group-assignments` | `UserAdminGroupAssignmentDto[]` — includes adminGroupId, groupCode, groupName |
| Role details | `GET /api/v2/security/roles/{id}` | `RoleDto` — includes permissionCodes[] |
| Admin group details | `GET /api/v2/security/admin-groups/{id}` | `AdminGroupDto` — includes permissionCodes[] |
| Department permissions | `GET /api/v2/security/departments/{departmentId}/permissions` | `DepartmentPermissionDto[]` — permissionCode per department |
| Permission catalog | `GET /api/v2/security/permissions` | `PermissionDto[]` — enrichment with description, scope, module |

### 4.3 User-to-department mapping

**NOT AVAILABLE via existing API.** The `UserDto` (from `GET /api/v2/organizations/users/{id}`) does not include department assignment. The `UserAssignmentsController` (`/api/v2/organizations/users/{userId}`) has only mutation endpoints (POST for assign, PUT for transfer) — no GET to retrieve current department assignments.

**Consequence:** The diagnostics page cannot show which department a user belongs to, and therefore cannot automatically load the department's baseline permissions. Department baseline context must either:
- Be omitted from Phase S, or
- Require manual department selection by the admin, or
- Require a backend discovery phase (similar to K0) to add a GET endpoint for user department assignments.

### 4.4 User lookup

The K0 Account Search API (`GET /api/v2/security/accounts?search=`) provides user lookup with search across username, employeeCode, and fullName. This pattern is already used by the Individual Permission Assignment page. The diagnostics page should reuse this same pattern.

Note: Account search requires `SECURITY_ACCOUNT_MANAGE` GLOBAL (AccountsController-level gate). The diagnostics page requires `SECURITY_ADMIN_MANAGE` GLOBAL. An admin with `SECURITY_ADMIN_MANAGE` but without `SECURITY_ACCOUNT_MANAGE` cannot use the account search API for user lookup.

**Alternative:** `GET /api/v2/organizations/users` (UsersController) requires `ORGANIZATION_USER_MANAGE` GLOBAL and returns `UserDto[]` with userId, employeeCode, fullName. This is also a different permission gate.

**Consequence:** The diagnostics page must either:
- Require the admin to manually enter a userId (no search), or
- Accept that user lookup requires at least one additional permission beyond SECURITY_ADMIN_MANAGE, or
- Defer user search to a future phase that addresses cross-permission user lookup.

**Recommended approach:** Accept manual userId entry for Phase S. The admin can obtain the userId from the Individual Permission Assignment page or Account Management page (both already available). Alternatively, if the Project Owner accepts, reuse the account search API and document that the page functionally requires both SECURITY_ADMIN_MANAGE and SECURITY_ACCOUNT_MANAGE for the search feature, while the core diagnostics (once userId is known) requires only SECURITY_ADMIN_MANAGE.

---

## 5. Proposed Phase S scope

### 5.1 Phase name
Phase 1B.1-S — Effective Permission Diagnostics UI

### 5.2 Route
`/security/effective-permissions`

### 5.3 Permission gate
`SECURITY_ADMIN_MANAGE` GLOBAL (menu/route visibility)

### 5.4 Implementation folder
`src/frontend/src/effectivePermissionDiagnostics`

### 5.5 Proposed files
- `src/frontend/src/effectivePermissionDiagnostics/effectivePermissionDiagnosticsApi.ts`
- `src/frontend/src/effectivePermissionDiagnostics/errorMessages.ts`
- `src/frontend/src/effectivePermissionDiagnostics/EffectivePermissionDiagnosticsPage.tsx`
- `src/frontend/src/effectivePermissionDiagnostics/EffectivePermissionDiagnosticsPage.test.tsx`

Modified files:
- `src/frontend/src/App.tsx` — add route
- `src/frontend/src/components/AuthenticatedShell.tsx` — add menu item
- `src/frontend/src/components/AuthenticatedShell.test.tsx` — update test

### 5.6 UI behavior

1. **User selection:** Admin enters a userId manually (numeric input). Optionally, if Project Owner accepts dual-permission requirement, provide account search using the K0 API.
2. **Company selection:** Admin selects a company from the company selector (already available in AuthenticatedShell) or leaves it unselected for global effective permissions.
3. **Effective permissions display:** Show the flat list of effective permission codes returned by the backend. Enrich each code with catalog information (description, module, scope, active status) by cross-referencing the permission catalog API.
4. **Contextual side-by-side sections (read-only, non-authoritative):**
   - Individual permissions (ALLOW and DENY) for the selected user.
   - User's role assignments, with each role's permission codes expandable.
   - User's admin group assignments, with each group's permission codes expandable.
   - Department baseline permissions only if department context is available (see 4.3 limitation).
5. **Clear labeling:** The effective permissions section is labeled as "Backend-Authoritative Final Result." The side-by-side sections are labeled as "Contextual Information — Not Source-Level Attribution" to prevent misinterpretation.
6. **No mutation.** Entirely read-only.

### 5.7 API reuse

| Frontend action | Backend API | Permission required |
|----------------|-------------|-------------------|
| Effective permissions | `GET /api/v2/security/users/{userId}/effective-permissions` | SECURITY_ADMIN_MANAGE |
| Permission catalog enrichment | `GET /api/v2/security/permissions` | SECURITY_ADMIN_MANAGE |
| Individual permissions context | `GET /api/v2/security/users/{userId}/individual-permissions` | SECURITY_ADMIN_MANAGE |
| Role assignments context | `GET /api/v2/security/users/{userId}/role-assignments` | SECURITY_ADMIN_MANAGE |
| Admin group assignments context | `GET /api/v2/security/users/{userId}/admin-group-assignments` | SECURITY_ADMIN_MANAGE |
| Role permission details | `GET /api/v2/security/roles/{id}` | SECURITY_ADMIN_MANAGE |
| Admin group permission details | `GET /api/v2/security/admin-groups/{id}` | SECURITY_ADMIN_MANAGE |
| User search (optional) | `GET /api/v2/security/accounts?search=` | SECURITY_ACCOUNT_MANAGE |

---

## 6. Source attribution limitation

**The current EffectivePermissionsController returns only flat permission codes (per OD-D-B-11).** It does not provide per-permission source attribution (which department, role, admin group, or individual grant contributed each code). It does not indicate which codes were individually denied.

The contextual side-by-side sections show the admin what sources exist for the selected user, but they cannot prove which source caused a specific effective permission to appear. The admin must manually cross-reference if needed.

Source-level attribution would require a new backend endpoint that returns the evaluation breakdown. This is explicitly deferred from Phase S unless the Project Owner separately approves backend changes.

---

## 7. Test strategy

- Render tests: page loads, displays user input, shows effective permissions.
- User selection: entering userId triggers data fetch.
- Effective permissions display: shows permission codes with catalog enrichment.
- Empty state: no permissions message when user has none.
- Error handling: API error messages displayed safely.
- Contextual sections: individual permissions, role assignments, admin group assignments display correctly.
- Company selector interaction: changing company refetches effective permissions.
- No mutation tests (page is read-only).
- No skip/only/debug in tests.

---

## 8. Security constraints

- No localStorage/sessionStorage/cookie persistence.
- No console logging.
- No JWT permission/company arrays.
- No frontend-only authorization replacement.
- Backend remains authoritative.
- No sensitive data exposure beyond what existing APIs return.
- Error messages sanitized (reuse existing errorMessages pattern).

---

## 9. Out of scope

- Per-permission source-level attribution (requires backend changes).
- Authorization Matrix / Security Overview.
- ENTITY scope.
- DENY outside existing individual-permission behavior.
- Bulk assignment.
- Export/download.
- Workflow approval.
- Business modules.
- Permission formula redesign.
- Organization structure redesign.
- Audit mutation/export/retention.
- Backend aggregation endpoint.
- User-department mapping endpoint (no GET exists).
- New permission codes.
- PermissionCodes.cs changes.
- permission-catalog.md changes.
- Schema/migration/rollback changes.

---

## 10. Required Project Owner decisions

**DEC-1B-S-01 — Phase selection:**
Proceed with Effective Permission Diagnostics UI as Phase 1B.1-S?
Recommended: Yes.

**DEC-1B-S-02 — Frontend-only:**
Must Phase S be frontend-only?
Recommended: Yes. All required APIs exist.

**DEC-1B-S-03 — Route:**
Accept `/security/effective-permissions` as the route?
Recommended: Yes.

**DEC-1B-S-04 — Permission gate:**
Accept `SECURITY_ADMIN_MANAGE` GLOBAL as the menu/route gate?
Recommended: Yes.

**DEC-1B-S-05 — Flat effective result:**
Accept flat effective permission result without source-level attribution?
Recommended: Yes. Backend only returns flat codes (OD-D-B-11).

**DEC-1B-S-06 — Source attribution deferral:**
Defer source-level attribution unless backend changes are separately approved?
Recommended: Yes.

**DEC-1B-S-07 — Contextual side-by-side sections:**
Allow side-by-side contextual sections using existing APIs, clearly labeled as context not attribution?
Recommended: Yes.

**DEC-1B-S-08 — Department baseline context:**
Omit department baseline context from side-by-side sections because no user-to-department GET API exists?
Recommended: Yes. Include department baseline only if user-department mapping becomes available.

**DEC-1B-S-09 — User lookup approach:**
Accept manual userId entry as the primary user selection method?
Recommended: Yes. If the Project Owner prefers search, accept that the page functionally requires SECURITY_ACCOUNT_MANAGE in addition to SECURITY_ADMIN_MANAGE for the search feature only.

**DEC-1B-S-10 — No backend changes:**
No backend changes by default; stop and report backend gaps?
Recommended: Yes.

**DEC-1B-S-11 — No schema/migration/rollback:**
No schema, migration, or rollback changes?
Recommended: Yes.

**DEC-1B-S-12 — No new permission codes:**
No new permission codes or permission catalog changes?
Recommended: Yes.

**DEC-1B-S-13 — Authorization Matrix deferred:**
Keep Authorization Matrix deferred?
Recommended: Yes.

**DEC-1B-S-14 — Deferred items:**
Keep ENTITY, non-individual DENY, bulk, export/download, workflow, and business modules deferred?
Recommended: Yes.

**DEC-1B-S-15 — No frontend-side audit events:**
No frontend-side audit events?
Recommended: Yes.

**DEC-1B-S-16 — Backend authority:**
Backend remains authoritative?
Recommended: Yes.

---

## 11. Risks and blockers

- **No blockers.** All required APIs for core diagnostics exist.
- **Risk: User-department mapping gap.** No GET endpoint exists to retrieve a user's department assignments. Department baseline context cannot be shown automatically. This is documented and accepted as a limitation.
- **Risk: User lookup permission crossover.** If account search is used for user lookup, the page requires SECURITY_ACCOUNT_MANAGE in addition to SECURITY_ADMIN_MANAGE. Manual userId entry avoids this but is less user-friendly.
- **Risk: Multiple API calls.** Composing 5+ API calls for a single user diagnostic view may be slow. Acceptable for admin diagnostic use.
- **Risk: Misleading side-by-side.** Admins may interpret contextual sections as source-level proof. Clear labeling mitigates this.

---

## 12. Recommendation

Proceed with Phase 1B.1-S as a frontend-only Effective Permission Diagnostics UI. The phase delivers immediate value by providing a dedicated verification tool for security administrators. Source-level attribution and authorization matrix remain deferred for separate backend-supported phases.

PHASE 1B.1-S PLAN PROPOSED — AWAITING PROJECT OWNER REVIEW
