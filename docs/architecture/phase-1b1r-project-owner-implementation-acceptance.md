# Phase 1B.1-R Project Owner Implementation Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-R IMPLEMENTATION ACCEPTED

**Accepted phase:**
Phase 1B.1-R — Department Baseline Permission Management UI

**Accepted Phase Q completion review commit:**
87fdb4d4509b6b43cfbb2f1ed0bf4ccd7987a3a6

**Accepted Phase R gap review commit:**
f9b86db45d8d720dd3d556e60853d883941c544e

**Accepted Phase R gap review acceptance commit:**
ed1ae18edd8a2fb364b9b8acf3e21fd7bb208d5f

**Accepted Phase R detailed plan commit:**
75218cd0af431d57178a40e29a7356ed749c152c

**Accepted Phase R plan acceptance commit:**
8d8c3656b6c9b3b77fe542b997766be40d930a38

**Accepted Phase R implementation commit:**
1f895b23942c47cb868d0153e11ca47a2bb074a7

**Implementation acceptance baseline:**
1f895b23942c47cb868d0153e11ca47a2bb074a7

**Previous completed phase:**
Phase 1B.1-Q COMPLETE

---

## Accepted implementation summary

- Frontend-only Department Baseline Permission Management UI implemented.
- Route implemented: /security/departments/permissions
- Route, menu, and actions are gated by SECURITY_ADMIN_MANAGE GLOBAL.
- Phase R does not require SECURITY_ACCOUNT_MANAGE GLOBAL.
- Phase R does not require SECURITY_AUDIT_VIEW GLOBAL.
- Account Management remains SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Audit Viewer remains SECURITY_AUDIT_VIEW GLOBAL gated.
- Existing DepartmentPermissionsController endpoints are reused.
- Existing PermissionsController catalog API is reused.
- Existing organization company/department lookup APIs are reused only as needed.
- Department baseline permission list is implemented.
- Department baseline permission replace-all PUT is implemented.
- UI does not treat PUT as append-only single-permission add.
- Existing intended permissions are preserved when using PUT.
- Department baseline permission DELETE/remove is implemented.
- GLOBAL and COMPANY are handled safely.
- COMPANY baseline permission requires selected current company where relevant.
- No silent fallback to GLOBAL.
- ENTITY is not exposed except filter/test assertions.
- DENY is not exposed.
- Effective Permission Diagnostics UI remains deferred.
- Authorization Matrix UI remains deferred.
- No bulk/export/download controls.
- No frontend-side audit events.
- Backend remains authoritative.
- No localStorage/sessionStorage/cookie persistence.
- No console logging.
- No JWT permission/company arrays.

---

## Accepted committed files

- M src/frontend/src/App.tsx
- M src/frontend/src/components/AuthenticatedShell.tsx
- M src/frontend/src/components/AuthenticatedShell.test.tsx
- A src/frontend/src/departmentPermissions/DepartmentPermissionsPage.test.tsx
- A src/frontend/src/departmentPermissions/DepartmentPermissionsPage.tsx
- A src/frontend/src/departmentPermissions/departmentPermissionsApi.ts
- A src/frontend/src/departmentPermissions/errorMessages.ts

---

## Accepted test evidence

- Backend build: 0 warnings, 0 errors.
- UnitTests: 133 passed, 0 failed, 0 skipped.
- IntegrationTests: 196 passed, 0 failed, 0 skipped.
- ApiTests: 239 passed, 0 failed, 0 skipped.
- Frontend lint: 0 errors.
- Frontend typecheck: 0 errors.
- Frontend tests: 156 passed, 0 failed, 0 skipped.

---

## Accepted constraints

- No backend source/test changes.
- No database changes.
- No migrations.
- No rollbacks.
- No new production permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No Account Management behavior change.
- No Audit Viewer behavior change.
- No Role Permission Management behavior change.
- No Admin Group Permission Management behavior change.
- No Individual Permission Assignment behavior change.
- No User Role Assignment behavior change.
- No User Admin Group Membership behavior change.
- No Effective Permission Diagnostics UI.
- No Authorization Matrix UI.
- No bulk assignment.
- No export/download.
- No ENTITY scope exposure.
- No DENY behavior exposure.
- No frontend-side audit events.
- No frontend-only authorization replacement.

---

## Project Owner acceptance

The Project Owner accepts the Phase 1B.1-R implementation as complete under the accepted Phase R scope and authorizes closure review.

PHASE 1B.1-R IMPLEMENTATION ACCEPTED — READY FOR CLOSURE REVIEW
