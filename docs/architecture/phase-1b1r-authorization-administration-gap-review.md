# Phase 1B.1-R Authorization Administration Gap Review

**Status:** PHASE 1B.1-R GAP REVIEW ACCEPTED — DETAILED PLANNING AUTHORIZED; SEE phase-1b1r-project-owner-gap-review-acceptance.md
**Baseline:** 87fdb4d4509b6b43cfbb2f1ed0bf4ccd7987a3a6
**Previous completed phase:** Phase 1B.1-Q COMPLETE

## 1. Purpose
This document captures the remaining authorization administration gaps after the completion of Phase 1B.1-Q. It discovers the existing coverage and recommends the next logical phase to complete the security administration component.

## 2. Baseline and completed authorization phases
The repository baseline is currently at `87fdb4d4509b6b43cfbb2f1ed0bf4ccd7987a3a6` with no uncommitted modifications.
The following authorization administration UI phases have been confirmed as completed:
- Phase 1B.1-N: Individual Permission Assignment UI complete.
- Phase 1B.1-O: Security Audit Viewer UI complete.
- Phase 1B.1-P1: Role Permission Management UI complete.
- Phase 1B.1-P2: Admin Group Permission Management UI complete.
- Phase 1B.1-Q1: User Role Assignment UI complete.
- Phase 1B.1-Q2: User Admin Group Membership UI complete.

## 3. Current authorization administration coverage
The system currently provides UI tools to manage:
- Account Management.
- Individual Permission Assignment UI.
- Security Audit Viewer UI.
- Role Permission Management UI.
- Admin Group Permission Management UI.
- User Role Assignment UI.
- User Admin Group Membership UI.

## 4. Backend capability discovery
An inspection of the API and domain layers reveals the following implemented capabilities:
- **Department Baseline Permissions:** Fully supported via `DepartmentPermissionsController` (`GET`, `PUT`, `DELETE` at `/api/v2/security/departments/{id}/permissions`).
- **Effective Permissions:** Supported via `EffectivePermissionsController` (`GET /api/v2/security/users/{id}/effective-permissions`).
- **Permission Evaluator:** Implements the formula `DepartmentBaseAllow ∪ RoleCompanyAllow ∪ EffectiveIndividualAllow - EffectiveIndividualDeny`.

## 5. Frontend coverage discovery
An inspection of the frontend implementation (`src/frontend/src`) reveals:
- No UI exists for mapping permissions to Departments (no screens targeting `DepartmentPermissionsController`).
- No dedicated "Effective Permissions Diagnostics" view exists, though the `fetchEffectivePermissions` API is used internally in the Permission Assignment UI.
- No Authorization Matrix or holistic security overview screen exists.

## 6. Remaining confirmed gaps
Based on the completed phases and existing backend capabilities, the primary remaining gaps are:
1. **Department Baseline Permission Management UI:** The backend API exists but there is no UI for administrators to define baseline permissions for a Department.
2. **Effective Permission Diagnostics UI:** A dedicated tool for a security admin to view the final calculated permissions for a given user across all sources.
3. **Security Context / Matrix Overviews:** Broad reports showing matrixes of who has what across the company.

## 7. Deferred or unsupported concepts
Per existing governance and baseline documentation:
- `ENTITY` scope remains deferred.
- Explicit `DENY` is restricted to individual permissions and is not supported on roles or departments.
- Bulk assignment, export/download of authorization rules, and workflow approval for security administration changes remain deferred.

## 8. Candidate next phases
1. **Department Baseline Permission Management UI**
   - *Business purpose:* Completes the final piece of the evaluated authorization formula (department baselines).
   - *Backend support:* Complete (`DepartmentPermissionsController`).
   - *Frontend support:* None.
   - *Risk/Dependencies:* Low risk. Blocked by nothing.
2. **Effective Permission Diagnostics UI**
   - *Business purpose:* Helps admins troubleshoot access issues.
   - *Backend support:* Complete (`EffectivePermissionsController`).
   - *Frontend support:* API bound, but no dedicated page.
   - *Risk/Dependencies:* Low risk.
3. **Authorization Matrix / Security Administration Overview**
   - *Business purpose:* High-level reporting.
   - *Backend support:* Partial/Missing (no dedicated aggregated matrix endpoint).
   - *Risk/Dependencies:* Blocked/Requires backend changes to aggregate data efficiently.

## 9. Recommended next phase
**Phase 1B.1-R — Department Baseline Permission Management UI**

*Reasoning:* The accepted permission formula includes department baseline permissions: `department baseline + role company + admin group + individual allow - individual deny`. Individual, role, admin group, user-role, and user-admin-group administration are now complete. Department baseline is the remaining major authorization administration component and has full backend support ready to be consumed.

## 10. Proposed scope for the recommended phase
- Create frontend routes, pages, and components for viewing and managing department permissions.
- Integrate with existing `GET`, `PUT`, `DELETE` endpoints on `DepartmentPermissionsController`.
- Maintain consistency with existing Role and Admin Group permission management UI patterns.
- No backend code changes required.

## 11. Proposed out-of-scope
- Backend schema or API modifications.
- `ENTITY` scope rules.
- Bulk updates or export functionality.
- Security matrices.

## 12. Required Project Owner decisions
- **DEC-1B-R-01:** Proceed with the recommended next phase (Phase 1B.1-R)?
- **DEC-1B-R-02:** Must the next phase be frontend-only? (Recommended: Yes, backend support already exists).
- **DEC-1B-R-03:** Are backend changes allowed if backend support is found missing during implementation? (Recommended: Stop and report).
- **DEC-1B-R-04:** Are schema/migration changes allowed? (Recommended: No).
- **DEC-1B-R-05:** Are new permission codes allowed? (Recommended: No).
- **DEC-1B-R-06:** Can `permission-catalog.md` be updated if needed? (Recommended: No).
- **DEC-1B-R-07:** Do ENTITY, DENY (non-individual), bulk, export/download, workflow, and business modules remain deferred? (Recommended: Yes).

## 13. Blockers or uncertainties
There are no current blockers. The backend infrastructure is ready to support the Department Baseline Permission Management UI.

## 14. Recommended next action
Await Project Owner review and decisions on the proposed phase. Upon approval, proceed to generate the implementation plan for Phase 1B.1-R.
