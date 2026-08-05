# Phase 1B.9-C Care Package Sales Frontend Implementation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER FRONTEND PLAN ACCEPTANCE

## Authorization Source

Reference:
- Phase 1B.9-B2 Project Owner workflow/payment acceptance commit:
  87b783b1f2b64c73fe67aff57016324c543c1003

## Planning Boundary

- Frontend implementation planning only.
- Frontend implementation is not authorized.
- Source changes are not authorized in this task.
- Backend changes are not authorized.
- Migrations are not authorized.
- Business docs and permission catalog changes are not authorized.
- Production migration/tag/push are not authorized.

## Accepted Backend / API Scope Summary

The following backend API endpoints are accepted and available under `/api/v2/care-packages`:

| Method | Path | Permission | Purpose |
|--------|------|-----------|---------|
| GET | `/care-packages` | CARE_PACKAGE_VIEW | List requests (paged, company-scoped via X-Company-Id) |
| GET | `/care-packages/{id}` | CARE_PACKAGE_VIEW | Detail with items |
| POST | `/care-packages` | CARE_PACKAGE_CREATE | Create request/draft |
| POST | `/care-packages/{id}/submit` | CARE_PACKAGE_CREATE | Submit for approval |
| POST | `/care-packages/{id}/approve` | CARE_PACKAGE_APPROVE | Approve workflow step |
| POST | `/care-packages/{id}/reject` | CARE_PACKAGE_REJECT | Reject workflow step |
| POST | `/care-packages/{id}/create-payment` | CARE_PACKAGE_CREATE_PAYMENT | Create payment draft |
| GET | `/care-packages/{id}/payment-status` | CARE_PACKAGE_VIEW | Read-only payment status |
| POST | `/care-packages/{id}/activate` | CARE_PACKAGE_CREATE | Activate after confirmed payment |

Backend response DTO (CarePackageRequestDto):
- `id`, `companyId`, `customerId`, `status`, `requiresApproval`, `workflowInstanceId`, `serviceId`, `saleDate`, `subtotalAmount`, `discountAmount`, `discountReason`, `totalAmount`, `paymentTransactionId`, `previousRequestId`, `createdAt`, `createdByUserId`, `updatedAt`, `updatedByUserId`, `items[]`.

Item DTO (CarePackageRequestItemDto):
- `id`, `carePackageRequestId`, `graveId`, `cotCountSnapshot`, `servicePeriodStartDate`, `servicePeriodEndDate`, `unitPriceSnapshot`, `lineSubtotal`, `notes`.

Create request DTO (CreateCarePackageRequest):
- `customerId`, `serviceId`, `saleDate`, `discountAmount`, `discountReason`, `item` (single item: `graveId`, `cotCount`, `servicePeriodStartDate`).

Error behavior:
- 400: validation errors (detail/title in body).
- 403: missing permission or company access.
- 404: not found.
- 409: invalid lifecycle transition, duplicate payment, not payment-eligible.

Company scope: `X-Company-Id` header required on all endpoints.

Backend-calculated pricing: unit price snapshot from Service Foundation effective-date pricing. Subtotal, discount, total calculated server-side. No hard-coded price.

## Accepted Workflow / Payment Scope Summary

Workflow:
- Process key: SELL_CARE_PACKAGE.
- Approval required for discount, price override, changed-price renewal, or configured approval rule.
- No approval required for configured-price/no-discount requests (status goes directly to PaymentEligible).
- Submit initiates workflow and sets PendingApproval.
- Approve/reject facades delegate to WorkflowRuntimeService.
- Domain state synchronization via CarePackageExecutionHandler after successful workflow action.
- Rejected requests are blocked from payment.

Payment:
- Payment created only when request is payment-eligible.
- Create-payment delegates to IPaymentTransactionService.
- Duplicate payment blocked.
- Payment-status is read-only.
- Active-status transitions request to Active after confirmed payment.
- Payment Foundation constraints: VND only, full payment, no partial/refund/cancellation.

## Proposed Frontend Routes

| Route | Page | Purpose |
|-------|------|---------|
| `/care-packages` | CarePackageRequestsPage | List/filter care package requests |
| `/care-packages/new` | CarePackageRequestCreatePage | Create new request |
| `/care-packages/:id` | CarePackageRequestDetailPage | Detail, actions, payment status |

These routes follow the existing repository pattern (e.g., `/cards/reprints`, `/cards/reprints/new`, `/cards/reprints/:id`).

## Proposed Frontend File / Module Structure

Following the existing `cards/` module pattern:

```
src/frontend/src/care-packages/
  types.ts                              — TypeScript interfaces
  carePackageApi.ts                     — API client functions
  hooks.ts                              — React Query hooks
  errorMessages.ts                      — Error message helpers
  CarePackageRequestsPage.tsx           — List page
  CarePackageRequestsPage.test.tsx      — List page tests
  CarePackageRequestCreatePage.tsx      — Create page
  CarePackageRequestCreatePage.test.tsx — Create page tests
  CarePackageRequestDetailPage.tsx      — Detail page
  CarePackageRequestDetailPage.test.tsx — Detail page tests
```

Additionally:
- `src/frontend/src/App.tsx` — add 3 routes under authenticated shell.

## Proposed API Client / Hooks / Types

### Types (types.ts)

```typescript
CarePackageRequestDto          — mirrors backend DTO
CarePackageRequestItemDto      — mirrors backend item DTO
CreateCarePackageRequest       — create request body
CreateCarePackageRequestItem   — item within create request
ApproveRejectRequest           — stepId, targetVersion, reason, comment
CreatePaymentRequest           — paymentMethod
CarePackagePaymentStatusDto    — payment status response
```

### API Functions (carePackageApi.ts)

| Function | Method | Path | Returns |
|----------|--------|------|---------|
| `listCarePackageRequests(params)` | GET | `/care-packages` | `PaginatedResult<CarePackageRequestDto>` |
| `getCarePackageRequest(id)` | GET | `/care-packages/{id}` | `CarePackageRequestDto` |
| `createCarePackageRequest(data)` | POST | `/care-packages` | `CarePackageRequestDto` |
| `submitCarePackageRequest(id)` | POST | `/care-packages/{id}/submit` | `CarePackageRequestDto` |
| `approveCarePackageRequest(id, data)` | POST | `/care-packages/{id}/approve` | `CarePackageRequestDto` |
| `rejectCarePackageRequest(id, data)` | POST | `/care-packages/{id}/reject` | `CarePackageRequestDto` |
| `createCarePackagePayment(id, data)` | POST | `/care-packages/{id}/create-payment` | response |
| `getCarePackagePaymentStatus(id)` | GET | `/care-packages/{id}/payment-status` | payment status |
| `activateCarePackageRequest(id)` | POST | `/care-packages/{id}/activate` | `CarePackageRequestDto` |

All functions pass `X-Company-Id` header via axiosClient interceptor (existing pattern).

### Hooks (hooks.ts)

| Hook | Type | Query Key | Notes |
|------|------|-----------|-------|
| `useCarePackageRequests(params)` | useQuery | `['carePackageRequests', params]` | List with filters |
| `useCarePackageRequest(id)` | useQuery | `['carePackageRequest', id]` | Detail, enabled when id truthy |
| `useCreateCarePackageRequest()` | useMutation | invalidates `carePackageRequests` | Navigate to detail on success |
| `useSubmitCarePackageRequest()` | useMutation | invalidates `carePackageRequest` | |
| `useApproveCarePackageRequest()` | useMutation | invalidates `carePackageRequest` | |
| `useRejectCarePackageRequest()` | useMutation | invalidates `carePackageRequest` | |
| `useCreateCarePackagePayment()` | useMutation | invalidates `carePackageRequest` | |
| `useCarePackagePaymentStatus(id)` | useQuery | `['carePackagePaymentStatus', id]` | Read-only, enabled conditionally |
| `useActivateCarePackageRequest()` | useMutation | invalidates `carePackageRequest` | |

Pattern follows existing `cards/hooks.ts` convention using `@tanstack/react-query`.

## Proposed Pages

### List Page (CarePackageRequestsPage)

- Table displaying: status, customer, sale date, total amount, payment status, created date.
- Filters: status, customer name/ID, service period, payment status.
- Pagination following existing pattern (page/pageSize query params).
- "Create" button visible only if user has CARE_PACKAGE_CREATE permission.
- Row click navigates to detail page.
- Loading, empty, and error states.

### Create Page (CarePackageRequestCreatePage)

- Customer selection field (customer ID or search, following existing customer selector pattern).
- Service ID / service code reference field.
- Sale date field (defaults to today).
- Care target item:
  - Grave ID / care target reference.
  - Cốt count input.
  - Service period start date.
- Discount fields (visible/enabled as needed):
  - Discount amount (VND).
  - Discount reason (required when discount > 0).
- Submit/create button.
- After successful creation: display backend-calculated response (unit price snapshot, subtotal, discount, total) and navigate to detail page.
- Validation error display for 400 responses.
- No frontend price calculation — all pricing is backend-calculated and displayed from the response DTO.

### Detail Page (CarePackageRequestDetailPage)

Summary panel:
- Status badge.
- Customer reference.
- Company.
- Service reference.
- Sale date.
- Previous request reference (for renewals).

Line items table:
- Grave/care target.
- Cốt count snapshot.
- Service period (start — end).
- Unit price snapshot.
- Line subtotal.

Pricing summary:
- Subtotal amount.
- Discount amount (with reason if present).
- Total amount.
- All values displayed from backend DTO — no frontend calculation.

Workflow status:
- Workflow instance link (if workflowInstanceId exists).
- Approval status display.

Payment status:
- Payment transaction reference (if paymentTransactionId exists).
- Read-only payment status from `/payment-status` endpoint.
- Link to Payment Foundation detail if applicable.

Lifecycle action buttons (see next section).

Audit info:
- Created by / created at.
- Updated by / updated at.

## Permission-Gated UI Plan

Frontend permission gating is UX convenience only. Backend remains authoritative for all actions.

| Permission | UI Effect |
|-----------|-----------|
| CARE_PACKAGE_VIEW | Access to list and detail pages |
| CARE_PACKAGE_CREATE | Show "Create" button on list page; access to create page; show "Submit" button on detail |
| CARE_PACKAGE_APPROVE | Show "Approve" button on detail page when pending approval |
| CARE_PACKAGE_REJECT | Show "Reject" button on detail page when pending approval |
| CARE_PACKAGE_CREATE_PAYMENT | Show "Create Payment" button on detail page when payment-eligible |

Hidden buttons do not replace backend authorization. All action failures display safe error messages.

The accepted non-blocking note remains: SQL permission seed alignment for CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, and CARE_PACKAGE_CREATE_PAYMENT must be addressed before deployment/operational validation. Frontend permission gating depends on these permissions being properly seeded and granted at runtime.

## Lifecycle Action Plan

| Action | Visible When | Permission | Backend Endpoint |
|--------|-------------|-----------|------------------|
| Submit | Draft status AND requiresApproval is true | CARE_PACKAGE_CREATE | POST `/{id}/submit` |
| Approve | PendingApproval status | CARE_PACKAGE_APPROVE | POST `/{id}/approve` |
| Reject | PendingApproval status | CARE_PACKAGE_REJECT | POST `/{id}/reject` |
| Create Payment | PaymentEligible status | CARE_PACKAGE_CREATE_PAYMENT | POST `/{id}/create-payment` |
| Activate | Paid/PendingPayment with confirmed payment | CARE_PACKAGE_CREATE | POST `/{id}/activate` |
| Payment Status | Any status with paymentTransactionId | CARE_PACKAGE_VIEW | GET `/{id}/payment-status` |

Action button behavior:
- Approve shows a dialog for step ID, target version, and optional comment (following existing WorkflowRejectDialog pattern).
- Reject shows a dialog requiring a reason (following existing WorkflowRejectDialog pattern).
- Create Payment shows a dialog for payment method selection (CASH / TRANSFER).
- All mutations invalidate the detail query on success.
- All mutations display safe error messages on failure.

## Pricing / Status Display Plan

- All pricing values (unit price snapshot, cốt count snapshot, line subtotal, subtotal, discount, total) are displayed directly from backend DTO fields.
- No frontend price calculation or hard-coded price.
- Frontend is not a financial source of truth.
- Status values are displayed as-is from backend `status` field.
- Payment status is fetched from the read-only `/payment-status` endpoint.
- Currency display: VND only, formatted according to existing frontend conventions.

## Error Handling Plan

Following existing `cards/errorMessages.ts` pattern:

| HTTP Status | User Message | Source |
|-------------|-------------|--------|
| 400 | `response.data.detail` or `response.data.title` or "Bad Request" | Validation errors, missing fields, invalid input |
| 403 | "You do not have permission to perform this action" | Missing permission or company access |
| 404 | "Care package request not found" | Invalid ID or deleted request |
| 409 | `response.data.detail` or "Invalid state transition" | Invalid lifecycle action, not payment-eligible, duplicate payment |
| Network error | "Unable to connect to the server" | Connection failure |
| Other | "An unexpected error occurred" | Fallback |

Additional safe failures:
- Missing/inactive service price: backend returns 400 with descriptive detail. Frontend displays the backend message.
- Workflow configuration missing: backend returns 400/409. Frontend displays the backend message.
- No raw backend internals, stack traces, or SQL exposed.

## Frontend Test / Validation Plan

### Test Coverage

| Test | Page | Validates |
|------|------|-----------|
| List page renders rows | CarePackageRequestsPage | Table rendering, column display |
| List page filters | CarePackageRequestsPage | Status/customer filter behavior |
| Create button permission-gated | CarePackageRequestsPage | CARE_PACKAGE_CREATE gate |
| Create page submits valid request | CarePackageRequestCreatePage | Form submission, API call |
| Create page validation errors | CarePackageRequestCreatePage | 400 error display |
| Detail page renders summary | CarePackageRequestDetailPage | Summary panel, line items, pricing |
| Detail page renders pricing snapshot | CarePackageRequestDetailPage | Backend-calculated values displayed |
| Detail page renders workflow status | CarePackageRequestDetailPage | Workflow instance reference |
| Detail page renders payment status | CarePackageRequestDetailPage | Payment status from API |
| Submit action visibility | CarePackageRequestDetailPage | Draft + requiresApproval + permission |
| Approve/reject action visibility | CarePackageRequestDetailPage | PendingApproval + permission |
| Create payment action visibility | CarePackageRequestDetailPage | PaymentEligible + permission |
| 400/403/404/409 handling | All pages | Error message display |
| No frontend hard-coded pricing | CarePackageRequestCreatePage | Backend-calculated totals |

### Validation Commands

```bash
cd src/frontend && npm run lint
cd src/frontend && npm run build
cd src/frontend && npm run test -- --run
cd src/frontend && npx vitest run src/care-packages
```

Backend validation is not required for Phase 1B.9-C frontend implementation unless backend files are changed, which should not happen.

## Risks / Dependencies

1. **Dependency on accepted backend API**: frontend relies on `/api/v2/care-packages` endpoints being available and returning documented DTOs.
2. **Dependency on B2 workflow/payment behavior**: submit/approve/reject/create-payment/activate actions depend on backend state machine behavior.
3. **Dependency on company context**: `X-Company-Id` header must be provided by existing CompanyProvider/axiosClient interceptor.
4. **Dependency on frontend permission context**: permission gating depends on existing permission evaluation pattern used by other modules.
5. **Dependency on backend-calculated pricing**: no frontend price calculation. If backend pricing behavior changes, frontend must adapt.
6. **SQL permission seed alignment**: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT permission constants were added without SQL seed rows. Database permission seeds must be addressed before deployment/operational validation.
7. **SELL_CARE_PACKAGE workflow runtime configuration**: workflow process configuration must be administratively established before runtime workflow operations function.
8. **Stale status risk**: frontend may display action buttons for a status that is no longer current if the request was modified by another user. Backend 409 responses handle this safely.
9. **Care target selector UX**: the create form requires graveId input. If existing UI does not have a grave/care-target selector component, one may need to be created or a manual ID input used.
10. **Report/export UI**: may be requested later but is out of Phase 1B.9-C scope.

## Out of Scope / Non-Goals

- Implementation in this planning task.
- Backend source changes.
- Database migrations.
- Business docs changes.
- Permission catalog changes.
- Production migration.
- Release tag.
- Push.
- Dynamic PDF/template generation.
- Generic Payment Print UI.
- Dedicated report/export UI.
- Refund.
- Cancellation.
- Partial payment.
- Physical inventory/stamp stock management.
- Multi-year packages.
- Partial-year packages.
- Discount percent UI.
- Frontend hard-coded price.
- Frontend as financial source of truth.

## Recommended Next Gate

Project Owner Phase 1B.9-C frontend plan acceptance.

No frontend implementation may begin until Project Owner frontend plan acceptance is recorded.

After PO frontend plan acceptance, the recommended implementation slice is:

Phase 1B.9-C Care Package Sales frontend implementation:
- Frontend routes (3 routes in App.tsx).
- `care-packages/` module (types, API client, hooks, error messages).
- List, create, and detail pages.
- Permission-gated lifecycle actions.
- Payment-status display.
- Frontend tests.
- Frontend implementation report.

Exclusions from implementation:
- Backend changes unless a blocking API mismatch is discovered and separately authorized.
- Production migration.
- Tag/push.
- Export/report UI.
- Dynamic PDF/template.
- Generic Payment Print UI.
