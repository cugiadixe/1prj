# Phase 1B.7-C Project Owner Frontend Scope Acceptance

## Status

ACCEPTED — PHASE 1B.7-C PAYMENT FRONTEND SCOPE APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b7c-frontend-scope-and-implementation-plan.md

Planning commit:
563dfd4ea66054cebd3e73488587730381608396

## Project Owner Decision

The Project Owner accepts the Phase 1B.7-C Payment Frontend scope and implementation plan.

This acceptance authorizes Phase 1B.7-C Payment frontend implementation only after this acceptance commit.

This acceptance does not authorize backend changes, migration changes, rollback changes, production migration, release tag, or push.

## Accepted Frontend Scope

- Payment frontend API client.
- Reconciliation frontend API client.
- TypeScript payment/reconciliation DTO and type mapping.
- payment list page.
- payment detail page.
- payment create/draft page.
- payment confirm action/flow.
- Admin payment correction form/dialog.
- draft payment soft-delete UI, only without implying cancellation.
- reconciliation daily report page.
- reconciliation monthly report page.
- reconciliation prepare controls.
- reconciliation confirm controls.
- payment status tag.
- payment item/service line display.
- VND amount display.
- correction history display if supported by backend response.
- route wiring.
- navigation wiring.
- permission-gated UI.
- sanitized frontend error handling.
- frontend tests.

## Accepted Backend Contract Baseline

- V0012/U0012.
- Payment_Transactions.
- Payment_Transaction_Items.
- Payment_Correction_History.
- Reconciliation_Periods.
- PaymentTransactionController.
- ReconciliationController.
- DRAFT to CONFIRMED lifecycle.
- Admin correction.
- manual reconciliation.
- Prepare/Confirm authorization bypass remediation already accepted.
- backend validation evidence: 219 UnitTests, 203 IntegrationTests, 299 ApiTests.

- frontend must not change backend API contracts.
- frontend must not invent endpoints.
- frontend must not expand into Card Reprint or Care Package Sales.

## Accepted API Client / Type Scope

- paymentApi.ts.
- reconciliationApi.ts.
- payment/reconciliation TypeScript types.
- CreatePaymentDraftRequest.
- ConfirmPaymentRequest.
- CorrectPaymentRequest.
- SoftDeletePaymentRequest.
- PrepareReconciliationRequest.
- ConfirmReconciliationRequest.
- payment list/detail response types.
- reconciliation daily/monthly report response types.
- error mapping for 400, 403, 404, 409, and generic 500.

- API clients must consume existing /api/v2 endpoints only.
- rowversion/concurrency handling must map 409 to a safe refresh message.
- no raw SQL/internal errors may be displayed.

## Accepted Route / Navigation Scope

- /payments
- /payments/new
- /payments/:id
- /reconciliation/daily
- /reconciliation/monthly

- App route wiring.
- AuthenticatedShell navigation wiring.
- permission-gated navigation entries.
- direct URL fallback to backend authorization.

Do not add:
- Card Reprint navigation.
- Care Package Sales navigation.
- refund/cancellation/partial payment navigation.
- automated bank integration navigation.

## Accepted Permission Scope

- PAYMENT_CREATE_DRAFT.
- PAYMENT_CONFIRM.
- PAYMENT_PRINT.
- PAYMENT_CORRECT_CONFIRMED.
- RECONCILIATION_PREPARE.
- RECONCILIATION_CONFIRM.

- UI gating is convenience only.
- backend authorization remains authoritative.
- direct URL access must still rely on backend 403/404.
- missing permission should hide or disable unsafe actions.

- PAYMENT_PRINT may be visible in types/permission handling if backend exposes it, but print UI scope remains subject to the accepted plan’s risk/open-question handling.
- no new permission codes are authorized by this acceptance.

## Accepted Error Handling Scope

- permission denied.
- not found.
- validation failure.
- stale rowversion/concurrency.
- confirmed payment immutability.
- invalid payment lifecycle transition.
- invalid service/customer/company.
- reconciliation period not found.
- reconciliation period already prepared/confirmed where applicable.
- generic server failure.

- no raw SQL/internal exception display.
- no stack trace display.
- no raw sensitive payload exposure.

## Accepted Test Scope

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

- npm run lint.
- npx tsc -b.
- npm run test.
- targeted Payment frontend tests.
- git diff --check.

## Accepted Out-of-Scope Items

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

## Accepted Risks / Open Questions

- payment print UI scope may remain deferred unless clearly supported by backend and accepted frontend scope.
- reconciliation export scope remains deferred unless clearly supported.
- customer/service deep links must be safe and supported.
- correction history display depends on backend response support.
- draft soft-delete UI must not imply cancellation.
- no refund/cancellation/partial payment UI.
- Card Reprint remains deferred.
- Care Package Sales remains deferred.
- operational browser validation remains a future gate.

These are accepted as tracked frontend planning risks and must be handled safely during implementation.

## Required Implementation Evidence

- frontend API client files.
- frontend TypeScript types.
- payment pages/components.
- reconciliation pages/components.
- route wiring.
- navigation wiring.
- permission-gated UI.
- sanitized error handling.
- frontend tests.
- implementation report.
- npm run lint result.
- npx tsc -b result.
- npm run test result.
- targeted Payment frontend test result.
- git diff --check result.
- confirmation no backend changes.
- confirmation no migration/rollback changes.
- confirmation no Card Reprint/Care Package Sales implementation.
- confirmation no production migration/tag/push.

## Authorization for Next Step

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

- implement frontend in this commit.
- create source code in this commit.
- create tests in this commit.
- modify frontend/backend files in this commit.
- modify migrations/rollbacks.
- modify business docs.
- implement Card Reprint.
- implement Care Package Sales.
- run production migration.
- create release tag.
- push.

## Notes / Risks

- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- production release remains deferred.
- implementation requires a separate implementation commit after this acceptance.
