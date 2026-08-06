# Phase 1B.7 Project Owner Scope Acceptance

## Status

ACCEPTED — PHASE 1B.7 PAYMENT / BILLING / COLLECTION / RECONCILIATION FOUNDATION SCOPE APPROVED FOR BACKEND/DATA PLANNING

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b7-payment-foundation-discovery-and-detailed-plan.md

Planning commit:
e57ed13757b7b6b711a6ed5b0a7ec816f1291979

## Project Owner Decision

The Project Owner accepts the Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation discovery and detailed plan as the approved scope baseline.

This acceptance authorizes backend/data scope and implementation planning only.

This acceptance does not authorize implementation.

## Accepted Business Rules

The Project Owner accepts the confirmed business rules from the plan:

- PAY-001: Cashier with PAYMENT_CONFIRM may create and confirm the same valid payment without approval request.
- PAY-002: Payment must contain at least one item; total_amount server-calculated and greater than zero.
- PAY-003: Confirmation moves payment one-way from DRAFT to CONFIRMED.
- PAY-004: After confirmation, cashier may view/print but may not edit or delete. Hard invariant.
- PAY-005: Only ADMIN_PAYMENT with PAYMENT_CORRECT_CONFIRMED may correct confirmed payment; non-empty reason mandatory.
- PAY-006: Confirmed payment correction may not change id, bill_code, status from CONFIRMED, or currency from VND. Hard invariant.
- PAY-007: No cancel, refund or partial-payment state.
- PAY-008: Payment correction must preserve customer/company/service-cycle consistency; must not pay same cycle twice.
- PAY-009: When company or payment date changes, all affected old/new daily and monthly reconciliation periods must be marked/recalculated.
- PAY-010: Payment correction, items, aggregates, reconciliation flags and audit committed atomically.
- PAY-011: After commit, notify cashier/confirming user, PTKD manager and reconciliation accounting group.
- PAY-012: Client may not supply trusted totals, actor fields or authorization decisions.

Additional confirmed rules:
- One payment may cover multiple services (PAY-002 allows multiple items).
- VND-only currency (PAY-006 hard invariant).
- Audit trail required (GOV-007, SEC-001, SEC-002, SEC-003).
- Notifications only after transaction commits (SEC-007).
- Backend authorization authoritative (AUTH-009).
- Financial operations require database-level validation (AUTH-010).
- Payment data company-scoped (DATA-003).
- Customer spending calculated from confirmed payments by company (DATA-006).

No new business rules were added or invented.

## Accepted Acceptance Criteria Baseline

The Project Owner accepts the payment acceptance criteria referenced by the plan:

- PAY-01 through PAY-08.

Backend/data planning must map future implementation evidence to these criteria.

## Accepted Proposed Scope

The Project Owner accepts the proposed Phase 1B.7 scope baseline:

- Payment / Billing / Collection / Reconciliation Foundation.
- DRAFT to CONFIRMED payment lifecycle.
- One-time full-payment rules.
- Multi-service payment itemization.
- Admin confirmed-payment correction.
- Append-only correction history.
- Manual daily/monthly reconciliation support.
- Service-linked payment consistency.
- Reporting support for PTKD and Accounting reconciliation.
- Permission/security enforcement.
- Auditability.
- Sanitized errors.
- Frontend and backend implementation to be planned in later gated steps.

## Accepted Proposed Data Model Direction

The Project Owner accepts the proposed data model direction for later backend/data planning:

- Payment_Transactions.
- Payment_Transaction_Items.
- Payment_Correction_History.
- Reconciliation_Periods.
- Relationship to Services.
- Relationship to Customers.
- Relationship to Companies.
- Rowversion/concurrency.
- VND amount handling.
- Payment lifecycle/status.
- Confirmed payment immutability and correction strategy.
- Reconciliation period support.

These are approved as planning direction only.
- V0012 migration creation is not authorized by this acceptance.
- U0012 rollback creation is not authorized by this acceptance.
- Exact schema remains subject to Phase 1B.7-B backend/data scope planning and PO acceptance.

## Accepted Proposed Backend/API Direction

The Project Owner accepts the proposed backend/API direction for later planning:

- PaymentTransactionController.
- ReconciliationController.
- API v2 endpoints described in the plan.
- Application service boundaries.
- Permission checks.
- Audit trail.
- Concurrency checks.
- Sanitized error handling.
- No raw internal error exposure.
- No production/accounting integration unless separately authorized.

Backend implementation is not authorized by this acceptance.

## Accepted Proposed Frontend Direction

The Project Owner accepts the proposed frontend direction for later planning:

- Payment list page.
- Payment detail page.
- Payment creation page.
- Payment confirmation flow.
- Admin confirmed-payment correction page/form.
- Reconciliation report view.
- Service/customer entry points if supported.
- Permission-gated navigation.
- Sanitized error display.
- Frontend tests.

Frontend implementation is not authorized by this acceptance.

## Accepted Permission and Security Direction

The Project Owner accepts the proposed PAYMENT_* and RECONCILIATION_* permission baseline from the plan:

- PAYMENT_CREATE_DRAFT (COMPANY).
- PAYMENT_CONFIRM (COMPANY).
- PAYMENT_PRINT (COMPANY).
- PAYMENT_CORRECT_CONFIRMED (COMPANY).
- RECONCILIATION_PREPARE (COMPANY).
- RECONCILIATION_CONFIRM (COMPANY).

Final permission codes and scopes must be confirmed during Phase 1B.7-B planning.
- Backend authorization remains authoritative.
- Frontend gating is convenience only.
- Admin-only confirmed-payment correction is accepted.
- Cashier/payment confirmation role mapping must follow the plan and accepted business rules.
- Audit/security event requirements must be included in backend/data planning.

## Accepted Reconciliation and Reporting Direction

The Project Owner accepts the proposed reconciliation/reporting direction:

- Daily PTKD report.
- Monthly PTKD report.
- Accounting reconciliation support.
- Manual reconciliation only.
- No bank reference code if confirmed by business docs.
- No automated bank integration unless separately documented and approved.
- Reconciliation period marking on correction.
- Report/export scope to be resolved during planning if still open.

## Accepted Open Questions

The Project Owner carries forward the plan's open decisions:

- OD-1B7-001 through OD-1B7-020.

The open decisions are accepted as tracked planning questions.
- They are not blockers to starting Phase 1B.7-B backend/data scope and implementation planning.
- Any decision that becomes implementation-blocking must be surfaced before implementation authorization.

## Accepted Proposed Implementation Phases

The Project Owner accepts the gated implementation sequence from the plan:

1. 1B.7-A Project Owner scope acceptance (this document).
2. 1B.7-B backend/data scope and implementation planning.
3. 1B.7-B Project Owner backend/data scope acceptance.
4. 1B.7-B backend/data implementation.
5. 1B.7-B backend/data acceptance review.
6. 1B.7-B Project Owner backend/data implementation acceptance.
7. 1B.7-C frontend scope and implementation planning.
8. 1B.7-C Project Owner frontend scope acceptance.
9. 1B.7-C frontend implementation.
10. 1B.7-C frontend acceptance review.
11. 1B.7-C Project Owner frontend implementation acceptance.
12. 1B.7-D operational validation and closure plan.
13. 1B.7-D Project Owner plan acceptance.
14. 1B.7-D operational validation execution.
15. 1B.7-D closure acceptance review.
16. 1B.7 Project Owner closure acceptance.

## Boundaries

- Implementation is not authorized.
- Database migration creation is not authorized.
- Rollback creation is not authorized.
- Backend implementation is not authorized.
- Frontend implementation is not authorized.
- Source/test changes are not authorized.
- Business docs changes are not authorized.
- Card Reprint implementation is not authorized.
- Care Package Sales implementation is not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Required Evidence for Next Planning Step

Future Phase 1B.7-B backend/data scope and implementation plan must define:

- Exact database schema proposal.
- V0012/U0012 strategy, but no creation unless later authorized.
- Exact permissions and scopes.
- Exact API v2 contract.
- Exact application service boundaries.
- Domain invariants.
- Audit/correction strategy.
- Reconciliation report strategy.
- Test strategy.
- Migration/rollback test strategy.
- Implementation boundaries.
- Open decision handling.

## Authorization for Next Step

Authorized next task:
Phase 1B.7-B backend/data scope and implementation planning only.

Do not authorize:
- implementation,
- database migration creation,
- rollback creation,
- backend implementation,
- frontend implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

This acceptance does not:

- implement Payment,
- create migrations,
- create rollbacks,
- modify source code,
- modify tests,
- modify frontend/backend files,
- modify business docs,
- implement Card Reprint,
- implement Care Package Sales,
- run production migration,
- create release tag,
- push.

## Notes / Risks

- 12 ambiguous/open rules remain carried forward from the plan.
- OD-1B7-001 through OD-1B7-020 remain tracked.
- Future V0012 migration would require reset target updates after authorization.
- Production release remains deferred.
- Local branch may be ahead of origin/main; no push is authorized.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
