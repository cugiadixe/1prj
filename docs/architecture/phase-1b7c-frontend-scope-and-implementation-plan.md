# Phase 1B.7-C Payment Frontend Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER FRONTEND SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.7-B PO backend/data implementation acceptance commit:
  e2568efa2233cb04751218ab310c1ccd82dc0dc1

State:
- Phase 1B.7-B backend/data implementation is accepted.
- This document is frontend scope and implementation planning only.
- This document does not authorize frontend implementation.

## Objective

Define the frontend scope, API client mapping, route/navigation strategy, permission-gated UI strategy, error handling, tests, and boundaries for Phase 1B.7-C Payment Frontend.

## Source Documents Reviewed

- docs/architecture/phase-1b7b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b7b-backend-data-foundation-updated-implementation-acceptance-review.md
- docs/architecture/phase-1b7b-backend-data-foundation-remediation-report.md
- docs/architecture/phase-1b7b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b7b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b7b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b7-project-owner-scope-acceptance.md
- docs/architecture/phase-1b7-payment-foundation-discovery-and-detailed-plan.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- PTKD-ERP-Master-Context.md
- src/backend/PTKD.Api/Controllers/PaymentTransactionController.cs
- src/backend/PTKD.Api/Controllers/ReconciliationController.cs

## Accepted Backend Contract Summary

The accepted backend/data implementation includes:

- V0012/U0012.
- Payment_Transactions.
- Payment_Transaction_Items.
- Payment_Correction_History.
- Reconciliation_Periods.
- PaymentTransactionService.
- ReconciliationService.
- PaymentTransactionController.
- ReconciliationController.
- PAYMENT_* and RECONCILIATION_* permissions.
- DRAFT to CONFIRMED lifecycle.
- Admin correction.
- manual reconciliation.
- remediation of Prepare/Confirm authorization bypass.
- backend test evidence: 219 UnitTests, 203 IntegrationTests, 299 ApiTests.

## Frontend Scope Decision Summary

### In Scope for 1B.7-C Frontend

The following frontend features are supported by the accepted backend API contract and are in scope:
- Payment list page with filtering (company, customer, status, dates).
- Payment detail page showing transaction information and items.
- Payment create/draft page.
- Payment confirm action/flow from draft state.
- Admin payment correction form/dialog.
- Payment soft-delete for draft records.
- Reconciliation daily report page.
- Reconciliation monthly report page.
- Reconciliation prepare and confirm controls.
- Payment status tags (e.g., Draft, Confirmed).
- VND amount display formatting.
- Payment item/service line display.

### Out of Scope / Deferred

Explicitly out of scope:
- backend changes.
- database migrations.
- rollbacks.
- Card Reprint frontend.
- Care Package Sales frontend.
- refund/cancellation/partial payment frontend.
- automated bank integration frontend.
- production migration.
- release tag.
- push.

## API Client and Type Strategy

Plan:
- **Frontend API client files**: paymentApi.ts, econciliationApi.ts.
- **TypeScript types**: DTOs will be mapped to exact TypeScript interfaces matching the backend.
- **Endpoint mapping**: Explicit mappings based on discovered controllers.
- **DTO mapping**: CreatePaymentDraftRequest, ConfirmPaymentRequest, CorrectPaymentRequest, SoftDeletePaymentRequest, PrepareReconciliationRequest, ConfirmReconciliationRequest mapped to TS types.
- **Error mapping**: Standardized API error interceptor catching 400 Validation Error, 403 Forbidden, 404 Not Found, 409 Conflict.
- **Concurrency/rowversion handling**: 409 Conflict will map to a user-friendly "Data has changed" message prompting a refresh.
- **Sanitized error display**: No raw stack traces; mapped to generic "server error" or specific validation title/detail.

| Method | Path | Frontend Function | Request Type | Response Type | Permission | Notes |
|---|---|---|---|---|---|---|
| POST | /api/v2/payments | createDraft | CreatePaymentDraftRequest | Payment DTO | PAYMENT_CREATE_DRAFT | Creates draft payment |
| POST | /api/v2/payments/{id}/confirm | confirmPayment | ConfirmPaymentRequest | Payment DTO | PAYMENT_CONFIRM | Confirms draft |
| GET | /api/v2/payments | listPayments | Query Params | Paginated Payment List | PAYMENT_CREATE_DRAFT | List with filters |
| GET | /api/v2/payments/{id} | getPaymentById | URL Param | Payment Detail DTO | PAYMENT_CREATE_DRAFT | Detail view |
| POST | /api/v2/payments/{id}/correct | correctConfirmed| CorrectPaymentRequest | Payment DTO | PAYMENT_CORRECT_CONFIRMED | Admin only |
| DELETE| /api/v2/payments/{id} | softDeleteDraft | SoftDeletePaymentRequest| void | PAYMENT_CREATE_DRAFT | Only for drafts |
| GET | /api/v2/reconciliation/daily | getDailyReport | Query Params | Reconciliation Report | RECONCILIATION_PREPARE | Daily view |
| GET | /api/v2/reconciliation/monthly| getMonthlyReport| Query Params | Reconciliation Report | RECONCILIATION_PREPARE | Monthly view |
| POST | /api/v2/reconciliation/periods/{id}/prepare | prepareReconciliation| PrepareReconciliationRequest| Period DTO | RECONCILIATION_PREPARE | Mutates period |
| POST | /api/v2/reconciliation/periods/{id}/confirm | confirmReconciliation| ConfirmReconciliationRequest| Period DTO | RECONCILIATION_CONFIRM | Finalizes period |

## Page / Component Strategy

- **Payment list page**:
  - Purpose: Browse and filter payments.
  - Data source: listPayments.
  - Permission gate: PAYMENT_CREATE_DRAFT.
  - Key UI fields: Date, Customer, Amount, Status, Actions.
  - Error behavior: Toast on failure.
  - Tests: Mock list rendering, pagination, filters.

- **Payment detail page**:
  - Purpose: View detailed payment information.
  - Data source: getPaymentById.
  - Permission gate: PAYMENT_CREATE_DRAFT.
  - Key UI fields: Header info, Item list, History, Action buttons (Confirm, Correct, Delete).
  - Tests: Conditional action rendering based on state and permissions.

- **Payment create/draft page**:
  - Purpose: Create new payment draft.
  - Data source: createDraft.
  - Permission gate: PAYMENT_CREATE_DRAFT.
  - Validation behavior: Prevent empty amounts/items. Error toast on 400.

- **Payment confirm flow**:
  - Purpose: Transition draft to confirmed.
  - Data source: confirmPayment.
  - Permission gate: PAYMENT_CONFIRM.
  - Error behavior: 409 Conflict triggers refresh dialog.

- **Admin payment correction form/dialog**:
  - Purpose: Fix confirmed payment values with reason.
  - Data source: correctConfirmed.
  - Permission gate: PAYMENT_CORRECT_CONFIRMED.
  - Validation behavior: Reason is mandatory.

- **Reconciliation daily/monthly report pages**:
  - Purpose: View reconciliation numbers for a date/month.
  - Data source: getDailyReport, getMonthlyReport.
  - Permission gate: RECONCILIATION_PREPARE.

- **Reconciliation prepare/confirm controls**:
  - Purpose: Process the reconciliation workflow.
  - Data source: prepareReconciliation, confirmReconciliation.
  - Permission gates: RECONCILIATION_PREPARE, RECONCILIATION_CONFIRM.

- **Displays**:
  - **Payment status tag**: Badge indicating Draft/Confirmed.
  - **Payment item/service line display**: Table within detail page.
  - **VND amount display**: Formatter showing decimal(18,2) in VND currency style.
  - **Correction history display**: Rendered on detail page if returned by API.

## Route and Navigation Strategy

- **Route paths**:
  - /payments -> Payment list
  - /payments/new -> Create draft
  - /payments/:id -> Payment detail
  - /reconciliation/daily -> Daily report
  - /reconciliation/monthly -> Monthly report

- **Navigation labels**: "Payments", "Daily Reconciliation", "Monthly Reconciliation".
- **App route wiring**: Added to standard router configuration.
- **AuthenticatedShell navigation wiring**: Added to sidebar menu.
- **Permission-gated menu behavior**: Links hidden if user lacks minimum permission (e.g., PAYMENT_CREATE_DRAFT for Payments).
- **Direct URL behavior**: Will attempt to load, but API calls will 403 Forbid and trigger standard unauthorized redirect/error if permissions are missing.

## Permission Strategy

Exact permission codes used:
- PAYMENT_CREATE_DRAFT.
- PAYMENT_CONFIRM.
- PAYMENT_PRINT.
- PAYMENT_CORRECT_CONFIRMED.
- RECONCILIATION_PREPARE.
- RECONCILIATION_CONFIRM.

For each:
- **UI use**: Hiding/showing buttons and navigation links.
- **Backend authority**: Always relies on API 403 responses to prevent unauthorized actions.
- **Menu/action behavior**: Hidden or disabled if permission is absent.
- **Direct URL fallback behavior**: Page may render skeleton, but API call will fail with 403, and frontend will catch this to display an error or redirect.
- **State**: frontend permission gating is convenience only. backend authorization remains authoritative.

## Error Handling Strategy

Sanitized frontend handling for:
- **permission denied**: Catch 403 and show standard "Access Denied" toast or page.
- **not found**: Catch 404 and show generic "Not Found" placeholder.
- **validation failure**: Map 400 Bad Request to form field errors or a notification toast using the Detail property from backend.
- **stale rowversion/concurrency**: Map 409 Conflict to a specific warning asking the user to refresh the record.
- **confirmed payment immutability / invalid lifecycle**: Handled gracefully via API 400 errors with sanitized messages.
- **invalid service/customer/company**: Mapped from 400 Validation Error.
- **reconciliation period not found**: Handle 404 cleanly.
- **reconciliation period already prepared/confirmed**: Handled via 400 validation from API.
- **generic server failure**: Catch 500 and show a generic "An unexpected error occurred" message.

Confirm:
- no raw SQL/internal exception display.
- no stack trace display.
- no raw sensitive payload exposure.

## Test Strategy

Plan frontend tests:
- API client tests.
- payment list/detail tests.
- payment create/draft tests.
- payment confirm tests.
- Admin correction tests.
- reconciliation daily/monthly report tests.
- reconciliation prepare/confirm tests.
- permission-gated UI tests.
- route/navigation tests.
- error mapping tests.
- regression tests for no raw SQL/internal error/stack trace display.

Expected validation commands for future implementation:
npm run lint
npx tsc -b
npm run test
targeted Payment frontend tests
git diff --check

## Implementation Sequence if Accepted

1. TypeScript payment/reconciliation types.
2. API clients and error mapping.
3. Payment list/detail pages.
4. Payment create/draft and confirm UI.
5. Admin correction UI.
6. Reconciliation report and prepare/confirm UI.
7. route wiring.
8. navigation wiring.
9. tests.
10. implementation report.
11. frontend validation.

Do not implement in this task.

## Risks / Open Questions

- whether payment print UI is in 1B.7-C scope or deferred.
- whether reconciliation export is in scope or deferred.
- whether customer/service deep links are safe in this phase.
- how to display correction history if API response supports it.
- how to handle soft-delete draft UI without implying cancellation.
- no refund/cancellation/partial payment UI.
- Card Reprint remains deferred.
- Care Package Sales remains deferred.
- operational browser validation remains future gate.

## Recommended Next Gate

Project Owner frontend scope acceptance for Phase 1B.7-C.

## Recommended Authorization Wording

Authorized next task:
Phase 1B.7-C Payment frontend implementation only.

Implementation must stay within the accepted frontend scope.

Do not authorize:
- backend changes,
- database migration,
- rollback creation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

This document does not:
- modify business requirements.
- create source code.
- create tests.
- create frontend files.
- create backend files.
- create migrations/rollbacks.
- authorize frontend implementation.
- authorize production migration.
- authorize release tag.
- authorize push.
