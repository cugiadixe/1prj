Title:
Phase 1B.1-M Project Owner Implementation Acceptance

Status:
ACCEPTED — PHASE 1B.1-M IMPLEMENTATION ACCEPTED
PHASE 1B.1-M CLOSURE REVIEW PASSED — SEE phase-1b1m-final-closure-review.md

Accepted phase:
Phase 1B.1-M — Current Company Context and X-Company-Id Frontend Foundation

Accepted plan commit:
2a49dcf75766f5635c9871fa63e20e03fe593a21

Accepted plan acceptance commit:
7efcc169148e14d18d1047c13497895a162d3d82

Accepted implementation commit:
41accfe41b7d8ce8dea9cf907b8a38d6e283bf74

Implementation acceptance baseline:
41accfe41b7d8ce8dea9cf907b8a38d6e283bf74

Note:
The Phase 1B.1-M implementation acceptance baseline uses the actual committed implementation hash:
41accfe41b7d8ce8dea9cf907b8a38d6e283bf74.
A previously reported hash, 41accfe5427b3780ef46f2c3d596bb0566270634, was superseded by the actual committed hash recorded as the parent of this acceptance chain.

Accepted implementation files:
- src/backend/PTKD.Api/Controllers/AuthController.cs
- src/backend/PTKD.Application/Security/Authorization/DTOs/UserCompanyDto.cs
- src/backend/PTKD.Application/Security/Authorization/Interfaces/ISecurityAdminService.cs
- src/backend/PTKD.Application/Security/Authorization/Services/SecurityAdminService.cs
- tests/backend/PTKD.ApiTests/MeCompaniesTests.cs
- src/frontend/src/App.tsx
- src/frontend/src/auth/AuthProvider.tsx
- src/frontend/src/auth/authApi.ts
- src/frontend/src/auth/CompanyProvider.tsx
- src/frontend/src/auth/CompanyProvider.test.tsx
- src/frontend/src/components/AuthenticatedShell.tsx

Accepted backend behavior:
- GET /api/v2/auth/me/companies implemented.
- Endpoint requires authenticated user.
- Endpoint returns 401 when unauthenticated.
- Endpoint does not require a separate permission code.
- Endpoint returns only selectable companies for the current user.
- Selectable companies are based on active user-company assignments and active companies.
- Inactive assignments are excluded.
- Inactive companies are excluded.
- Future assignments are excluded.
- Expired assignments are excluded.
- Empty selectable companies return a safe empty array.
- Stable ordering is tested.
- No read/switch audit event is emitted.

Accepted response shape:
- companies: array
  - companyId
  - companyCode
  - companyName
  - isDefault

Accepted response exclusions:
- No assignmentStatus.
- No effectiveFrom/effectiveTo.
- No role internals.
- No group internals.
- No department internals.
- No token/session material.
- No security stamp material.
- No password/hash material.
- No audit payloads.
- No raw SQL.
- No raw exception details.

Accepted frontend behavior:
- Current company context is memory-only.
- Selectable companies are fetched after authenticated login/refresh when mustChangePassword=false.
- Companies are not fetched when mustChangePassword=true.
- Zero selectable companies produce safe empty state.
- Exactly one selectable company is auto-selected in memory.
- Multiple selectable companies do not auto-select.
- User must manually select when multiple selectable companies exist.
- Manual company selection refetches current-user permissions with X-Company-Id.
- Account Management remains SECURITY_ACCOUNT_MANAGE GLOBAL gated.
- Account Management does not require selected company.
- X-Company-Id is not configured as a global axios default.
- X-Company-Id is used only for explicit permissions refetch or future company-scoped request helpers.
- Company context clears on logout.
- Company context clears on refresh failure/auth clear.
- Company context clears after password change.
- Company context is not persisted in localStorage.
- Company context is not persisted in sessionStorage.
- Company context is not persisted in cookies.
- Selected company is not encoded in URL.
- Backend remains authoritative.

Accepted security behavior:
- No schema migration.
- No rollback migration.
- No new production permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No JWT company array.
- No JWT permission array.
- No localStorage company persistence.
- No sessionStorage company persistence.
- No cookie company persistence.
- No token persistence introduced.
- RefreshToken is not read from document.cookie.
- document.cookie usage remains limited to CSRF utility.
- No read/switch audit event.
- No console logging of company/auth/permission payloads.

Accepted test evidence:
- Backend build Release passed with 0 errors.
- Full UnitTests passed: 133/133.
- Full IntegrationTests passed: 196/196.
- Full ApiTests passed: 239/239.
- Frontend typecheck passed with 0 errors.
- Frontend tests passed: 103/103.
- Frontend lint passed with 0 errors and 3 known React Refresh warnings.

Accepted exclusions:
- No Permission Assignment UI.
- No Role/Group Management UI.
- No Audit Viewer UI.
- No Dynamic Approval Workflow.
- No business module changes.
- No Company Administration CRUD.
- No organization structure redesign.
- No schema migration.
- No rollback migration.
- No new production permission code.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No persistent company context storage.
- No global axios X-Company-Id default.
- No company context as backend security enforcement replacement.
- No implementation_plan.md committed.
- No task.md committed.
- No walkthrough.md committed.
- No scratch files committed.

Implementation acceptance conclusion:
PHASE 1B.1-M IMPLEMENTATION ACCEPTED — READY FOR CLOSURE REVIEW
