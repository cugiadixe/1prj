# Phase 1B.9-C Care Package Sales Frontend Acceptance Review

## Status

PASSED WITH NOTES — READY FOR PROJECT OWNER FRONTEND ACCEPTANCE

## Review Target

Reference:
- Phase 1B.9-C frontend implementation commit:
  aae57bd1dd3479f757e1a8173061bce5616f5190

- Phase 1B.9-C Project Owner frontend plan acceptance commit:
  4742aca08f5c95403c97a5dd165d0ee49f4db550

## Authorization Review

The implementation stayed entirely within the authorized C frontend scope. Only frontend source files, frontend test files, and the implementation report were committed. No backend files, backend tests, migrations, rollbacks, business docs, or permission catalog files were modified. No production migration, release tag, or push occurred.

## Committed File Review

Committed files from `git diff-tree --no-commit-id --name-status -r HEAD`:

- A docs/architecture/phase-1b9c-care-package-sales-frontend-implementation-report.md
- M src/frontend/src/App.tsx
- A src/frontend/src/care-packages/CarePackageRequestCreatePage.test.tsx
- A src/frontend/src/care-packages/CarePackageRequestCreatePage.tsx
- A src/frontend/src/care-packages/CarePackageRequestDetailPage.test.tsx
- A src/frontend/src/care-packages/CarePackageRequestDetailPage.tsx
- A src/frontend/src/care-packages/CarePackageRequestsPage.test.tsx
- A src/frontend/src/care-packages/CarePackageRequestsPage.tsx
- A src/frontend/src/care-packages/carePackageApi.ts
- A src/frontend/src/care-packages/errorMessages.ts
- A src/frontend/src/care-packages/hooks.ts
- A src/frontend/src/care-packages/types.ts

12 files total: 1 report, 1 modified route file, 7 new frontend source files, 3 new frontend test files.

- No backend files were modified.
- No backend tests were modified.
- No database migration/rollback files were committed.
- No business docs were committed.
- No permission catalog changes were committed.
- No scratch/decompiled/FixStrategy/script/debug files were committed.
- No production migration/tag/push.

## Route / Module Review

Three routes registered in App.tsx under `<ProtectedRoute><AuthenticatedShell /></ProtectedRoute>`:

| Route | Component |
|-------|-----------|
| `/care-packages` | CarePackageRequestsPage |
| `/care-packages/new` | CarePackageRequestCreatePage |
| `/care-packages/:id` | CarePackageRequestDetailPage |

App.tsx changes are limited to 3 import statements and 3 route registrations with a phase comment. No unrelated routes or modules were added. Module structure follows the existing `cards/` pattern.

## API Client / Hooks / Types Review

### Types (types.ts)
7 interfaces defined:
- CarePackageRequestDto — 18 fields + items array, mirrors accepted backend DTO.
- CarePackageRequestItemDto — 9 fields, mirrors backend item DTO.
- CreateCarePackageRequest — matches backend create request shape with nested item.
- CreateCarePackageRequestItem — graveId, cotCount, servicePeriodStartDate.
- ApproveRejectRequest — stepId, targetVersion, reason, comment.
- CreatePaymentRequest — paymentMethod.
- CarePackagePaymentStatusDto — status field.

Types correctly mirror accepted backend DTO shapes. No invented fields.

### API Client (carePackageApi.ts)
9 functions mapping to the 9 accepted backend endpoints:
- listCarePackageRequests — GET /care-packages
- getCarePackageRequest — GET /care-packages/{id}
- createCarePackageRequest — POST /care-packages
- submitCarePackageRequest — POST /care-packages/{id}/submit
- approveCarePackageRequest — POST /care-packages/{id}/approve
- rejectCarePackageRequest — POST /care-packages/{id}/reject
- createCarePackagePayment — POST /care-packages/{id}/create-payment
- getCarePackagePaymentStatus — GET /care-packages/{id}/payment-status
- activateCarePackageRequest — POST /care-packages/{id}/activate

All use axiosClient (X-Company-Id header handled by existing interceptor). No invented endpoints. No hard-coded price. Frontend does not calculate financial totals. PaginatedResult interface follows existing pattern.

### Hooks (hooks.ts)
9 hooks following existing cards/hooks.ts pattern:
- useCarePackageRequests — useQuery with params.
- useCarePackageRequest — useQuery, enabled when id truthy.
- useCreateCarePackageRequest — useMutation, invalidates list.
- useSubmitCarePackageRequest — useMutation, invalidates detail + list.
- useApproveCarePackageRequest — useMutation, invalidates detail + list.
- useRejectCarePackageRequest — useMutation, invalidates detail + list.
- useCreateCarePackagePayment — useMutation, invalidates detail.
- useCarePackagePaymentStatus — useQuery with 5s polling, conditionally enabled.
- useActivateCarePackageRequest — useMutation, invalidates detail + list.

Query key naming follows existing conventions.

## Pages / Components Review

### List Page (CarePackageRequestsPage)
- Table with columns: ID, Customer ID, Status, Total Amount, Sale Date, Created At.
- Status filter dropdown with all lifecycle states.
- VND currency formatting (vi-VN locale).
- Permission-gated "Create Request" button (CARE_PACKAGE_CREATE, COMPANY scope).
- Row click navigates to detail page.
- Loading, empty, error, and 403 permission-denied states present.
- Status color tags for all lifecycle states.

### Create Page (CarePackageRequestCreatePage)
- Form fields: Customer ID, Service ID, Sale Date, Grave ID, Cot Count, Service Period Start Date, Discount Amount, Discount Reason.
- Discount reason required when discount > 0 (frontend validation).
- Defaults: Sale Date = today, Service Period Start Date = today, Discount Amount = 0.
- Permission gate: CARE_PACKAGE_CREATE, COMPANY scope.
- After creation: navigates to detail page with backend-calculated response.
- No frontend price calculation. No hard-coded price.
- Error display for backend validation errors.
- No multi-year or partial-year UI. No discount percent UI.

### Detail Page (CarePackageRequestDetailPage)
- Summary panel: Status, Customer, Company, Service, Sale Date, Requires Approval, Previous Request, Workflow Instance link, Created/Updated audit.
- Line items table: Grave ID, Cot Count, Service Period, Unit Price, Line Subtotal, Notes.
- Pricing summary: Subtotal, Discount (with reason), Total — all from backend DTO.
- Payment status display (read-only, polling when paymentTransactionId exists).
- Lifecycle action buttons with permission and status gating.
- Approve modal with optional comment. Reject modal with reason. Create Payment modal with method selection.
- View Payment link to existing Payment UI. Workflow instance link to existing Workflow UI.
- No Payment Print UI. No report/export UI.

## Permission-Gated UI Review

| Permission | Scope | UI Effect |
|-----------|-------|-----------|
| CARE_PACKAGE_VIEW | COMPANY | List and detail access (handled by backend 403) |
| CARE_PACKAGE_CREATE | COMPANY | Create button, create page, submit button, activate button |
| CARE_PACKAGE_APPROVE | COMPANY | Approve button when PendingApproval |
| CARE_PACKAGE_REJECT | COMPANY | Reject button when PendingApproval |
| CARE_PACKAGE_CREATE_PAYMENT | COMPANY | Create Payment button when PaymentEligible |

- CARE_PACKAGE_REPORT_VIEW is not used in this slice.
- All permission gates use `hasPermission(code, 'COMPANY')` following existing service module pattern.
- Frontend gates are UX-only. Backend remains authoritative.
- 403 errors are handled safely with permission-denied alerts.

## Lifecycle Action Review

| Action | Visible When | Permission | Endpoint |
|--------|-------------|-----------|----------|
| Submit for Approval | Draft + requiresApproval | CARE_PACKAGE_CREATE | POST /{id}/submit |
| Approve | PendingApproval | CARE_PACKAGE_APPROVE | POST /{id}/approve |
| Reject | PendingApproval | CARE_PACKAGE_REJECT | POST /{id}/reject |
| Create Payment | PaymentEligible | CARE_PACKAGE_CREATE_PAYMENT | POST /{id}/create-payment |
| Activate | Paid | CARE_PACKAGE_CREATE | POST /{id}/activate |
| Payment Status | paymentTransactionId exists | (read-only display) | GET /{id}/payment-status |

- All actions use handleAction wrapper with safe error display.
- Stale status / backend 409 handled via getErrorMessage extracting detail/title.
- Approve sends stepId + targetVersion + optional comment.
- Reject sends stepId + targetVersion + reason.
- Create Payment sends paymentMethod (CASH / TRANSFER).
- Payment status polls at 5s interval when active.

## Pricing / Status Display Review

- All pricing values displayed from backend DTO: subtotalAmount, discountAmount, discountReason, totalAmount, unitPriceSnapshot, lineSubtotal.
- No frontend price calculation.
- No frontend hard-coded price.
- No frontend financial source-of-truth calculation.
- Currency formatted as VND using vi-VN locale.
- No refund/cancellation/partial payment UI.
- No dynamic PDF/template UI.
- No generic Payment Print UI.
- No dedicated report/export UI.

## Error Handling Review

errorMessages.ts provides:
- `isPermissionDenied(error)` — checks for 403 via axios.isAxiosError.
- `getErrorMessage(error)` — extracts safe messages:
  - 400: response.data.detail or response.data.title or "Bad Request".
  - 404: "Care package request not found".
  - 409: response.data.detail or response.data.title or "Invalid state transition".
  - Generic axios: response.data.message or error.message.
  - Error instance: error.message.
  - Fallback: "An unexpected error occurred."

This covers validation errors, missing permission, not found, invalid lifecycle transition, duplicate payment, payment not eligible, missing price, workflow configuration missing, and generic failures. All produce safe user-facing messages. No raw backend internals, stack traces, or SQL exposed.

## Test Coverage Review

3 test files, 19 test cases:

### CarePackageRequestsPage.test.tsx (6 tests)
- Renders loading state.
- Renders error state.
- Renders empty state.
- Renders list with data.
- Hides create button if missing permission.
- Renders permission denied if API returns 403.

### CarePackageRequestCreatePage.test.tsx (3 tests)
- Renders create form.
- Renders permission denied if missing CARE_PACKAGE_CREATE.
- Renders form fields.

### CarePackageRequestDetailPage.test.tsx (10 tests)
- Renders loading state.
- Renders detail page with data (status badge, total amount, line items table).
- Shows submit button for Draft + requiresApproval.
- Shows approve/reject buttons for PendingApproval.
- Shows create payment button for PaymentEligible.
- Shows activate button for Paid.
- Hides action buttons when permissions missing.
- Renders permission denied on 403.
- Renders error state on non-403 error.
- Displays payment status when paymentTransactionId exists.

Coverage is adequate for the acceptance criteria. Tests cover rendering, permission gating, lifecycle action visibility, payment status display, error handling, and backend-calculated value display. No export/report/PDF/print UI tested (out of scope).

## Acceptance Validation Evidence

- Lint (oxlint): only pre-existing warnings in auth/ files. No new warnings. No care-packages warnings.
- Build (tsc -b && vite build): Build succeeded. 3275 modules transformed.
- Full Vitest: 71 test files passed, 500 tests passed.
- Targeted care-packages Vitest: 3 test files passed, 19 tests passed.
- git diff --check: clean.

## Non-Blocking Notes

1. SQL permission seed alignment for CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT remains deferred before deployment/operational validation. Frontend permission gating depends on these permissions being properly seeded and granted at runtime.
2. SELL_CARE_PACKAGE workflow runtime configuration remains deferred before deployment/operational validation.
3. Care target selector uses manual Grave ID input. A searchable selector component may be desired in a future iteration.
4. Customer selector uses manual Customer ID input. Integration with existing customer search may be desired in a future iteration.
5. Stale status risk: frontend may display action buttons for a status that is no longer current if another user modified the request. Backend 409 responses handle this safely and errors are displayed to the user.

## Blockers

No blocking issues found.

## Boundary Confirmation

- No backend changes.
- No backend tests changed.
- No database migrations/rollbacks.
- No business docs changed.
- No permission catalog changed.
- No production migration.
- No release tag.
- No push.
- No dynamic PDF/template generation.
- No generic Payment Print UI.
- No dedicated report/export UI.
- No refund/cancellation/partial payment UI.
- No frontend hard-coded price.
- No frontend financial source-of-truth calculation.

## Recommended Next Gate

Project Owner Phase 1B.9-C frontend acceptance.
