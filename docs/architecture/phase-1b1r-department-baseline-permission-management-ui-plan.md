# Phase 1B.1-R Department Baseline Permission Management UI Plan

**Status:** PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW
**Baseline:** ed1ae18edd8a2fb364b9b8acf3e21fd7bb208d5f
**Previous completed phase:** Phase 1B.1-Q COMPLETE
**Parent discovery:** Phase 1B.1-R Authorization Administration Gap Review accepted

## 1. Purpose
This document details the frontend implementation plan for the Department Baseline Permission Management UI (Phase 1B.1-R), addressing the confirmed authorization administration gap while reusing existing backend capabilities.

## 2. Confirmed current state
- The backend `DepartmentPermissionsController` provides full support for managing baseline permissions for a given department.
- The authorization evaluator already factors in department baseline permissions.
- No frontend UI currently exists to manage these department baseline permissions.
- Phase 1B.1-Q (User Role and Admin Group Assignment UI) is complete.

## 3. DepartmentPermissionsController discovery
1. **Exact route prefix:** `api/v2/security/departments/{departmentId:long}/permissions`
2. **Exact GET endpoint path:** `/api/v2/security/departments/{departmentId}/permissions`
3. **Exact PUT endpoint path:** `/api/v2/security/departments/{departmentId}/permissions`
4. **Exact DELETE endpoint path:** `/api/v2/security/departments/{departmentId}/permissions/{code}`
5. **Endpoint scope:** Department-based.
6. **Exact route parameters:** `departmentId` (long), `code` (string).
7. **Exact query parameters:** None.
8. **Exact request DTO for create/update:** `SetDepartmentPermissionsRequest(IReadOnlyList<string> PermissionCodes)`.
9. **Exact request DTO for delete/remove:** None (uses route parameters).
10. **Exact response DTO:** `IReadOnlyList<DepartmentPermissionDto>`.
11. **rowVersion required:** No.
12. **EffectiveFrom supported:** No.
13. **EffectiveTo supported:** No.
14. **Important backend contract:**
    - PUT replaces the full department baseline permission set.
    - UI must not treat PUT as append-only single-permission add.
    - Any add/update flow must preserve existing intended permissions when using PUT.
    - DELETE uses existing backend remove semantics only.
15. **Allow/Deny support:** Allow only.
16. **GLOBAL scope support:** Yes (baseline for the department).
17. **COMPANY scope support:** No separate payload scope (inherently bounded by department's company, if applicable).
18. **ENTITY scope support:** No.
19. **X-Company-Id requirement:** Not explicitly required on the controller endpoint.
20. **Department-Company validation:** Backend handles ownership/validation natively.
21. **Protected by SECURITY_ADMIN_MANAGE GLOBAL:** Yes.
22. **Backend audit behavior:** Wired.
23. **Sanitized errors:** Yes, returns standard Problem Details.
24. **Sufficient for frontend-only:** Yes.

## 4. Department/company lookup discovery
25. **List companies:** `GET /api/v2/organizations/companies`
26. **List departments:** `GET /api/v2/organizations/departments`
27. **Filter by company:** Yes, via `[FromQuery] long companyId`.
28. **Department DTO:** Exposes `Id`, `DepartmentCode`, `Name`, `CompanyId`, `IsActive`.
29. **Inactive departments:** Displayed read-only.
30. **Company context:** Should be used to filter departments down to the current company context where appropriate, though security admins may operate globally.

## 5. Permission catalog discovery
31. **Catalog API sufficient:** Yes (`GET /api/v2/security/permissions`).
32. **Exposed fields:** `PermissionCode`, `Name`, `Description`, `Scope`, `Status`.
33. **Scope filtering:** Done client-side.
34. **ENTITY permissions:** Must be hidden/filtered out in UI.
35. **Authorization:** `SECURITY_ADMIN_MANAGE GLOBAL` is sufficient.

## 6. Frontend reuse analysis
36. **Permission Assignment UI:** We can reuse the split-pane layout (departments on left, assigned permissions on right).
37. **Role/Admin Group UI:** We can reuse the "Set Permissions" modal/drawer with checkbox selection and PUT replacement pattern.
38. **Company provider:** `useCurrentCompany` can be used to filter available departments.
39. **Error mapping:** Standard Axios interceptors and Problem Details mapping.
40. **Route/Menu:** Placed under Security Administration navigation.
41. **Tests:** Will mirror Role Permission Management testing patterns.

## 7. Selected route and access-gate recommendation
**Recommended route:** `/security/departments/permissions`
*Reasoning:* It aligns perfectly with REST paths and existing frontend routing standards for security modules.
**Recommended access-gate:** `SECURITY_ADMIN_MANAGE GLOBAL`

## 8. Proposed Phase R scope
- Frontend-only implementation of the Department Baseline Permission Management UI.
- Use existing `DepartmentPermissionsController` and `PermissionsController`.
- Gate the route with `SECURITY_ADMIN_MANAGE GLOBAL`.
- Support viewing, setting (PUT), and removing (DELETE) baseline permissions for departments.
- Deferred items remain deferred (Effective Permission Diagnostics, Matrices, ENTITY scope, DENY).

## 9. Proposed backend scope
- **None.** The existing backend fully supports the requirements.

## 10. Proposed frontend scope
- **New Page:** `DepartmentPermissionsPage.tsx`
- **Integration:** React Query hooks for fetching and updating department permissions.
- **UI Components:** Department selector/list, permission list view, "Manage Permissions" modal.
- **Navigation:** Add entry in `AuthenticatedShell` if accepted by Project Owner.

## 11. Authorization and permission-gating strategy
- The route and actions will be strictly gated by `SECURITY_ADMIN_MANAGE GLOBAL`.
- Backend remains authoritative.
- No dependency on `SECURITY_ACCOUNT_MANAGE` or `SECURITY_AUDIT_VIEW`.

## 12. GLOBAL and COMPANY scope strategy
- Department baseline permissions apply to the department itself. The UI will only expose permissions valid for this baseline level (filtering out ENTITY).

## 13. Current company context strategy
- The department list will be filtered by the currently selected company in the header, ensuring administrators only manage departments within their active context.
- No silent fallback to GLOBAL context.

## 14. DENY strategy
- DENY is not supported for department baselines by the backend, so the UI will not expose it.

## 15. Removal/deactivation strategy
- Uses existing `DELETE` semantics provided by `DepartmentPermissionsController` (hard delete of the assignment).

## 16. Audit strategy
- Relies completely on existing backend audit trail generation. No custom frontend audit events.

## 17. Error handling strategy
- Reuse standard `ProblemDetails` components and toast notifications for user-friendly error display.

## 18. Test strategy
- Unit tests for new components and page: `DepartmentPermissionsPage.test.tsx`.
- Integration tests ensuring proper routing and permission gating in `App.test.tsx` and `AuthenticatedShell.test.tsx`.

## 19. Explicit out-of-scope
- Backend changes, schema migrations, rollback migrations.
- New permission codes, changes to `PermissionCodes.cs` or `permission-catalog.md`.
- ENTITY scope permissions.
- Explicit DENY assignments for departments.
- Bulk assignment, export/download functionality.
- Effective Permission Diagnostics UI or Authorization Matrix UI.
- Account Management, Role Permission Management, Admin Group Permission Management, User Role Assignment, or User Admin Group Membership changes.

## 20. Required Project Owner decisions
- **DEC-1B-R-01 — Phase R shape:** Should Phase R implement Department Baseline Permission Management UI only? (Recommended: Yes)
- **DEC-1B-R-02 — Authorization gate:** Which permission gates Phase R? (Recommended: `SECURITY_ADMIN_MANAGE GLOBAL`)
- **DEC-1B-R-03 — Route:** Which route should Phase R use? (Recommended: `/security/departments/permissions`)
- **DEC-1B-R-04 — Backend basis:** Should Phase R use existing `DepartmentPermissionsController` only? (Recommended: Yes)
- **DEC-1B-R-05 — Permission catalog lookup:** Should Phase R use existing `PermissionsController` catalog API? (Recommended: Yes)
- **DEC-1B-R-06 — Department/company lookup:** Should Phase R use existing department/company APIs only as needed? (Recommended: Yes)
- **DEC-1B-R-07 — Scope behavior:** Should Phase R support GLOBAL and COMPANY only where backend supports safely? (Recommended: Yes)
- **DEC-1B-R-08 — Company context:** Should COMPANY department baseline assignment require selected current company? (Recommended: Yes)
- **DEC-1B-R-09 — DENY behavior:** Should Phase R expose DENY? (Recommended: No)
- **DEC-1B-R-10 — Removal behavior:** Should Phase R expose hard delete? (Recommended: No frontend difference, use existing backend delete semantics only)
- **DEC-1B-R-11 — Audit:** Should Phase R create frontend-side audit events? (Recommended: No)
- **DEC-1B-R-12 — Backend changes:** Should backend changes be allowed in Phase R implementation? (Recommended: No by default)
- **DEC-1B-R-13 — Deferred items:** Should Effective Permission Diagnostics UI, Authorization Matrix UI, ENTITY, DENY outside accepted backend behavior, bulk, export/download, workflow, and business modules remain deferred? (Recommended: Yes)
- **DEC-1B-R-14 — Permission catalog/code changes:** Should Phase R add new permission codes or modify `permission-catalog.md`? (Recommended: No by default)
- **DEC-1B-R-15 — Frontend navigation:** Should Phase R add a Security Administration menu entry? (Recommended: Yes)

## 21. Blockers, if any
- None.

## 22. Recommended implementation files
- `src/frontend/src/departmentPermissions/departmentPermissionsApi.ts`
- `src/frontend/src/departmentPermissions/errorMessages.ts`
- `src/frontend/src/departmentPermissions/DepartmentPermissionsPage.tsx`
- `src/frontend/src/departmentPermissions/DepartmentPermissionsPage.test.tsx`
- `src/frontend/src/App.tsx` (Route registration)
- `src/frontend/src/components/AuthenticatedShell.tsx` (Navigation entry)
- `src/frontend/src/components/AuthenticatedShell.test.tsx`

## 23. Acceptance criteria
- Phase R is Department Baseline Permission Management UI only.
- UI is gated by `SECURITY_ADMIN_MANAGE GLOBAL`.
- Phase R does not require `SECURITY_ACCOUNT_MANAGE GLOBAL`.
- Phase R does not require `SECURITY_AUDIT_VIEW GLOBAL`.
- Account Management remains `SECURITY_ACCOUNT_MANAGE GLOBAL` gated.
- Audit Viewer remains `SECURITY_AUDIT_VIEW GLOBAL` gated.
- Backend authorization remains authoritative.
- Existing `DepartmentPermissionsController` APIs are used where possible.
- Existing `PermissionsController` catalog APIs are reused where possible.
- Existing department/company lookup APIs are reused only as needed.
- No frontend-only authorization replacement.
- GLOBAL and COMPANY support only where backend supports safely.
- COMPANY baseline permission assignment requires selected current company where relevant.
- No silent fallback to GLOBAL.
- ENTITY scope remains deferred.
- DENY is not exposed unless backend explicitly supports it and Project Owner approves it.
- Removal uses existing backend endpoint semantics only.
- Effective Permission Diagnostics UI remains deferred.
- Authorization Matrix UI remains deferred.
- No account management changes unless separately approved.
- No role permission management changes unless separately approved.
- No admin group permission management changes unless separately approved.
- No individual permission assignment changes unless separately approved.
- No user role assignment changes unless separately approved.
- No user admin group membership changes unless separately approved.
- No department baseline bulk assignment unless separately approved.
- No export/download unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No `PermissionCodes.cs` change unless separately approved.
- No `permission-catalog.md` change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, role management, admin group management, user role assignment, user admin group membership, audit viewer, and mustChangePassword tests remain passing.
