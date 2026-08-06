# Phase 1B.9-C Care Package Sales Frontend Implementation Report

## Status
IMPLEMENTED — READY FOR FRONTEND ACCEPTANCE REVIEW

## Authorization Source
Phase 1B.9-C Project Owner frontend plan acceptance commit: 4742aca08f5c95403c97a5dd165d0ee49f4db550

## Implemented Frontend Files

New files:
- src/frontend/src/care-packages/types.ts
- src/frontend/src/care-packages/carePackageApi.ts
- src/frontend/src/care-packages/hooks.ts
- src/frontend/src/care-packages/errorMessages.ts
- src/frontend/src/care-packages/CarePackageRequestsPage.tsx
- src/frontend/src/care-packages/CarePackageRequestsPage.test.tsx
- src/frontend/src/care-packages/CarePackageRequestCreatePage.tsx
- src/frontend/src/care-packages/CarePackageRequestCreatePage.test.tsx
- src/frontend/src/care-packages/CarePackageRequestDetailPage.tsx
- src/frontend/src/care-packages/CarePackageRequestDetailPage.test.tsx

Modified files:
- src/frontend/src/App.tsx (added 3 route imports and 3 route registrations)

Report:
- docs/architecture/phase-1b9c-care-package-sales-frontend-implementation-report.md

## Routes Added

| Route | Component | Purpose |
|-------|-----------|---------|
| `/care-packages` | CarePackageRequestsPage | List care package requests |
| `/care-packages/new` | CarePackageRequestCreatePage | Create new request |
| `/care-packages/:id` | CarePackageRequestDetailPage | Detail, lifecycle actions, payment status |

All routes are registered under `<ProtectedRoute><AuthenticatedShell /></ProtectedRoute>`.

## Pages / Components Summary

### CarePackageRequestsPage
- Table with columns: ID, Customer ID, Status, Total Amount, Sale Date, Created At.
- Status filter dropdown.
- Permission-gated "Create Request" button (CARE_PACKAGE_CREATE, COMPANY scope).
- Loading, empty, error, and 403 permission-denied states.
- Row click navigates to detail page.
- VND currency formatting.

### CarePackageRequestCreatePage
- Form fields: Customer ID, Service ID, Sale Date, Grave ID, Cot Count, Service Period Start Date, Discount Amount, Discount Reason.
- Discount reason required when discount amount > 0 (frontend validation).
- Defaults: Sale Date = today, Service Period Start Date = today, Discount Amount = 0.
- Permission gate: CARE_PACKAGE_CREATE, COMPANY scope.
- After creation: navigates to detail page with backend-calculated response.
- No frontend price calculation.

### CarePackageRequestDetailPage
- Summary panel: Status, Customer, Company, Service, Sale Date, Requires Approval, Previous Request, Workflow Instance link, Created/Updated audit.
- Line items table: Grave ID, Cot Count, Service Period, Unit Price, Line Subtotal, Notes.
- Pricing summary: Subtotal, Discount (with reason), Total — all from backend DTO.
- Payment status display (read-only, polling when payment exists).
- Lifecycle action buttons with permission and status gating.
- Approve modal with optional comment.
- Reject modal with reason.
- Create Payment modal with payment method selection (CASH / TRANSFER).
- View Payment link when paymentTransactionId exists.
- Workflow instance link when workflowInstanceId exists.

## API Client / Hooks / Types Summary

### Types (types.ts)
- CarePackageRequestDto — mirrors backend DTO (18 fields + items array).
- CarePackageRequestItemDto — mirrors backend item DTO (9 fields).
- CreateCarePackageRequest — create request body with nested item.
- CreateCarePackageRequestItem — item within create request.
- ApproveRejectRequest — stepId, targetVersion, reason, comment.
- CreatePaymentRequest — paymentMethod.
- CarePackagePaymentStatusDto — payment status response.

### API Client (carePackageApi.ts)
9 functions:
- listCarePackageRequests(params) — GET /care-packages
- getCarePackageRequest(id) — GET /care-packages/{id}
- createCarePackageRequest(data) — POST /care-packages
- submitCarePackageRequest(id) — POST /care-packages/{id}/submit
- approveCarePackageRequest(id, data) — POST /care-packages/{id}/approve
- rejectCarePackageRequest(id, data) — POST /care-packages/{id}/reject
- createCarePackagePayment(id, data) — POST /care-packages/{id}/create-payment
- getCarePackagePaymentStatus(id) — GET /care-packages/{id}/payment-status
- activateCarePackageRequest(id) — POST /care-packages/{id}/activate

All use axiosClient (X-Company-Id header handled by existing interceptor).

### Hooks (hooks.ts)
9 hooks:
- useCarePackageRequests(params) — useQuery, key ['carePackageRequests', params]
- useCarePackageRequest(id) — useQuery, key ['carePackageRequest', id], enabled when id truthy
- useCreateCarePackageRequest() — useMutation, invalidates carePackageRequests
- useSubmitCarePackageRequest() — useMutation, invalidates carePackageRequest + carePackageRequests
- useApproveCarePackageRequest() — useMutation, invalidates carePackageRequest + carePackageRequests
- useRejectCarePackageRequest() — useMutation, invalidates carePackageRequest + carePackageRequests
- useCreateCarePackagePayment() — useMutation, invalidates carePackageRequest
- useCarePackagePaymentStatus(id, enabled) — useQuery, refetchInterval 5000ms
- useActivateCarePackageRequest() — useMutation, invalidates carePackageRequest + carePackageRequests

### Error Messages (errorMessages.ts)
- isPermissionDenied(error) — checks for 403.
- getErrorMessage(error) — extracts detail/title from 400, 404 ("Care package request not found"), 409 ("Invalid state transition"), generic fallback.

## Permission-Gated UI Summary

| Permission | Scope | UI Effect |
|-----------|-------|-----------|
| CARE_PACKAGE_VIEW | COMPANY | List and detail page access (handled by backend 403) |
| CARE_PACKAGE_CREATE | COMPANY | Create button on list, create page access, submit button on detail |
| CARE_PACKAGE_APPROVE | COMPANY | Approve button when PendingApproval |
| CARE_PACKAGE_REJECT | COMPANY | Reject button when PendingApproval |
| CARE_PACKAGE_CREATE_PAYMENT | COMPANY | Create Payment button when PaymentEligible |

All permission gates are UX convenience only. Backend remains authoritative.

## Lifecycle Action Summary

| Action | Visible When | Permission |
|--------|-------------|-----------|
| Submit for Approval | Draft + requiresApproval | CARE_PACKAGE_CREATE |
| Approve | PendingApproval | CARE_PACKAGE_APPROVE |
| Reject | PendingApproval | CARE_PACKAGE_REJECT |
| Create Payment | PaymentEligible | CARE_PACKAGE_CREATE_PAYMENT |
| Activate | Paid | CARE_PACKAGE_CREATE |
| View Payment | paymentTransactionId exists | (link, no permission gate) |

## Pricing / Status Display Summary

- All pricing values displayed from backend DTO: subtotalAmount, discountAmount, discountReason, totalAmount, unitPriceSnapshot, lineSubtotal.
- No frontend price calculation.
- No frontend hard-coded price.
- Currency formatted as VND using vi-VN locale.
- Payment status fetched from read-only /payment-status endpoint.

## Error Handling Summary

| HTTP Status | Behavior |
|-------------|----------|
| 400 | Display response.data.detail or response.data.title or "Bad Request" |
| 403 | Permission denied alert on pages; safe error on actions |
| 404 | "Care package request not found" |
| 409 | Display response.data.detail or "Invalid state transition" |
| Network error | Display error.message |
| Other | "An unexpected error occurred." |

No raw backend internals, stack traces, or SQL exposed.

## Tests Added

3 test files, 19 test cases:

### CarePackageRequestsPage.test.tsx (6 tests)
- Renders loading state
- Renders error state
- Renders empty state
- Renders list with data
- Hides create button if missing permission
- Renders permission denied if API returns 403

### CarePackageRequestCreatePage.test.tsx (3 tests)
- Renders create form
- Renders permission denied if missing CARE_PACKAGE_CREATE
- Renders form fields

### CarePackageRequestDetailPage.test.tsx (10 tests)
- Renders loading state
- Renders detail page with data
- Shows submit button for Draft status with requiresApproval
- Shows approve/reject buttons for PendingApproval status
- Shows create payment button for PaymentEligible status
- Shows activate button for Paid status
- Hides action buttons when permissions missing
- Renders permission denied on 403
- Renders error state on non-403 error
- Displays payment status when paymentTransactionId exists

## Validation Evidence

- `npm run lint` (oxlint): only pre-existing warnings in auth/ files. No new warnings.
- `npm run build` (tsc -b && vite build): Build succeeded. 3275 modules transformed.
- `npm run test -- --run`: 71 test files passed, 500 tests passed.
- `npx vitest run src/care-packages`: 3 test files passed, 19 tests passed.
- `git diff --check`: clean.

## Boundary Confirmation

- No backend files modified.
- No backend tests modified.
- No database migrations modified.
- No rollbacks modified.
- No business docs modified.
- No docs/business/permission-catalog.md modified.
- No production migration run.
- No release tag created.
- No push performed.
- No scratch/decompiled/FixStrategy files staged.
- No frontend hard-coded price.
- No frontend financial source-of-truth calculation.
- No refund/cancellation/partial payment.
- No dynamic PDF/template generation.
- No generic Payment Print UI.
- No dedicated report/export UI.

## Known Risks / Follow-Ups

1. SQL permission seed alignment for CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT must be addressed before deployment/operational validation.
2. SELL_CARE_PACKAGE workflow runtime configuration must be administratively established before runtime operations.
3. Care target selector UX uses manual Grave ID input; a searchable selector component may be desired in a future iteration.
4. Customer selector uses manual Customer ID input; integration with existing customer search may be desired.
5. Stale status risk: frontend may show action buttons for status no longer current; backend 409 handles this safely.
6. Frontend depends on existing company context / X-Company-Id interceptor behavior.

## Recommended Next Gate

Phase 1B.9-C frontend acceptance review.
