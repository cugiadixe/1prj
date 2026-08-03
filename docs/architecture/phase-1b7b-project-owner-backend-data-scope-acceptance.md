# Phase 1B.7-B Project Owner Backend/Data Scope Acceptance

## Status

ACCEPTED — PHASE 1B.7-B PAYMENT BACKEND/DATA SCOPE APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b7b-backend-data-foundation-scope-and-implementation-plan.md

Planning commit:
b80b07add213597bc096f82e9cc51bbac80117cf

## Project Owner Decision

The Project Owner accepts the Phase 1B.7-B Payment Backend/Data Foundation scope and implementation plan.

This acceptance authorizes Phase 1B.7-B backend/data implementation only after this acceptance commit.

This acceptance does not authorize frontend implementation.

## Accepted Backend/Data Scope

The Project Owner accepts the backend/data scope defined in the plan:

- Payment / Billing / Collection / Reconciliation backend foundation.
- DRAFT to CONFIRMED lifecycle.
- One-time full payment.
- No partial payment.
- No refund.
- No cancellation.
- One bill/payment cannot be confirmed twice.
- One payment may cover multiple services.
- VND-only amount handling.
- Admin-only confirmed-payment correction.
- Mandatory correction reason.
- Append-only correction history.
- Reconciliation period marking after correction.
- Manual daily/monthly reconciliation support.
- Service-linked payment consistency.
- Auditability.
- Sanitized backend/API errors.

## Accepted Database Scope

The Project Owner accepts the proposed database scope:

- V0012 migration.
- U0012 rollback.
- Payment_Transactions.
- Payment_Transaction_Items.
- Payment_Correction_History.
- Reconciliation_Periods.

Accepted conventions:
- DECIMAL(18,2) amount handling for consistency with Service Module pricing.
- Rowversion/concurrency.
- Lifecycle/status constraints (DRAFT/CONFIRMED for payments; OPEN/DIRTY/PREPARED/CONFIRMED for reconciliation).
- FK relationships to Companies, Customers, Services, and Users as defined by the plan.
- Indexes and uniqueness constraints defined by the plan.
- CHECK constraints defined by the plan (status, currency_code = 'VND', period_type).
- is_deleted behavior only for DRAFT payments; confirmed payments cannot be deleted.
- Confirmed payment immutability and correction strategy.

V0012/U0012 creation is authorized only for the next Phase 1B.7-B backend/data implementation task.
Production migration is not authorized.
Exact implementation must stay within the accepted plan.

## Accepted Rollback Scope

The Project Owner accepts the proposed U0012 rollback direction:

- Structural rollback (DROP tables in reverse FK order with IF OBJECT_ID guards).
- SchemaVersions cleanup according to repository convention.
- Soft-deactivation of 6 seeded PAYMENT/RECONCILIATION permissions (UPDATE is_active = 0, per TR_Permissions_PreventDelete).
- SET XACT_ABORT ON / BEGIN TRANSACTION / COMMIT TRANSACTION pattern.
- Migration/rollback tests.

## Accepted Domain Model Scope

The Project Owner accepts implementation planning for:

- PaymentTransaction: DRAFT → CONFIRMED one-way lifecycle, hard invariants (PAY-004, PAY-006), Admin correction guards, server-calculated total, soft-delete DRAFT only.
- PaymentTransactionItem: service-linked, amount validation, immutable after creation.
- PaymentCorrectionHistory: append-only, mandatory reason, before/after JSON snapshots, correlation_id.
- ReconciliationPeriod: OPEN → DIRTY → PREPARED → CONFIRMED lifecycle, MarkDirty/Prepare/Confirm guards.

## Accepted Application Service Scope

The Project Owner accepts implementation planning for:

- PaymentTransactionService: create draft, confirm, get/list, correct confirmed, soft-delete draft.
- ReconciliationService: daily/monthly queries, prepare, confirm.
- Notification boundary for Admin correction (audit trail minimum; formal notifications deferred per OD-1B7-010).
- Audit boundary via ITransactionalAuditWriter / SecurityAuditEventRecord.
- Transaction boundaries: atomic commit for correction (PAY-010).
- Two-phase save for draft creation (parent entity, then items).
- Duplicate service-cycle prevention within confirmation transaction.
- Sanitized error handling.
- Permission checks via IPermissionEvaluator.

## Accepted API v2 Scope

The Project Owner accepts the proposed API v2 scope:

### PaymentTransactionController — api/v2/payments

- POST /payments — create draft (PAYMENT_CREATE_DRAFT).
- POST /payments/{id}/confirm — confirm (PAYMENT_CONFIRM).
- GET /payments?companyId&... — list (PAYMENT_CREATE_DRAFT).
- GET /payments/{id} — detail (PAYMENT_CREATE_DRAFT).
- POST /payments/{id}/correct — Admin correct (PAYMENT_CORRECT_CONFIRMED).
- DELETE /payments/{id} — soft-delete draft (PAYMENT_CREATE_DRAFT).

### ReconciliationController — api/v2/reconciliation

- GET /reconciliation/daily?companyId&date — daily report (RECONCILIATION_PREPARE).
- GET /reconciliation/monthly?companyId&year&month — monthly report (RECONCILIATION_PREPARE).
- POST /reconciliation/periods/{id}/prepare — prepare (RECONCILIATION_PREPARE).
- POST /reconciliation/periods/{id}/confirm — confirm (RECONCILIATION_CONFIRM).

API implementation must not expose refund, cancellation, partial payment, Card Reprint, or Care Package Sales behavior.
API implementation must not expose raw SQL/internal exceptions or stack traces.

## Accepted Permission and Security Scope

The Project Owner accepts the six proposed COMPANY-scoped permission codes:

| Permission Code | DataScope | Sensitive | Purpose |
|---|---|---|---|
| PAYMENT_CREATE_DRAFT | COMPANY | Yes | Create draft; also read access |
| PAYMENT_CONFIRM | COMPANY | Yes | Confirm draft → CONFIRMED |
| PAYMENT_PRINT | COMPANY | Yes | Print confirmed (deferred to frontend) |
| PAYMENT_CORRECT_CONFIRMED | COMPANY | Yes | Admin correct confirmed payment |
| RECONCILIATION_PREPARE | COMPANY | Yes | Prepare reconciliation; read reports |
| RECONCILIATION_CONFIRM | COMPANY | Yes | Confirm reconciliation period |

- Backend authorization remains authoritative.
- Frontend gating is convenience only.
- Admin-only confirmed-payment correction must be enforced.
- Cashier confirmation rules must follow accepted business rules (PAY-001).
- Sanitized errors required.
- No raw SQL/internal exception exposure.
- No stack traces.
- No raw sensitive payload exposure.

## Accepted Reconciliation Scope

- Manual reconciliation only.
- Daily reconciliation support.
- Monthly reconciliation support.
- PTKD and Accounting reconciliation reporting support.
- No bank reference code.
- No automated bank integration.
- Reconciliation period marking after Admin correction (PAY-009).
- Period lifecycle: OPEN → DIRTY → PREPARED → CONFIRMED.

## Accepted Test Scope

The Project Owner accepts planned backend/data tests:

- Domain unit tests (~20).
- Application service tests (~5).
- Integration tests (~8): table existence, permission seeding, rollback.
- API tests (~14): auth, CRUD, confirmation, correction, reconciliation.
- Migration/rollback tests (V0012/U0012).
- Permission/security tests (SecuritySchemaTests updated).
- Concurrency tests.
- Audit/correction tests.
- No production migration.

## Accepted Open Decisions

Carried forward: OD-1B7-001 through OD-1B7-020.

- Accepted as tracked decisions.
- None currently block backend/data implementation authorization according to the accepted plan.
- Any decision that becomes blocking during implementation must stop implementation and be documented.

## Implementation Boundaries

Authorized next task:
Phase 1B.7-B backend/data implementation only.

Authorized in the next task:
- V0012 migration creation.
- U0012 rollback creation.
- Backend/domain/application/infrastructure/API implementation within accepted scope.
- Backend tests.
- Migration/rollback tests.
- Backend/data implementation report.

Not authorized:
- Frontend implementation.
- Card Reprint implementation.
- Care Package Sales implementation.
- Production migration.
- Release tag.
- Push.

## Required Implementation Evidence

Future backend/data implementation must provide:

- V0012 migration.
- U0012 rollback.
- Backend entities/configurations.
- Application services.
- API v2 controllers.
- Permission seeding/tests.
- Migration/rollback tests.
- Domain unit tests.
- Integration tests.
- API tests.
- Implementation report.
- dotnet build result.
- UnitTests result.
- IntegrationTests result.
- ApiTests result.
- git diff --check result.
- Confirmation no frontend implementation.
- Confirmation no Card Reprint/Care Package Sales implementation.
- Confirmation no production migration/tag/push.

## Non-Goals

This acceptance does not:

- Implement Payment in this commit.
- Create V0012 in this commit.
- Create U0012 in this commit.
- Modify source code in this commit.
- Modify tests in this commit.
- Modify frontend/backend files in this commit.
- Modify business docs.
- Implement frontend.
- Implement Card Reprint.
- Implement Care Package Sales.
- Run production migration.
- Create release tag.
- Push.

## Notes / Risks

- OD-1B7-001 through OD-1B7-020 remain tracked.
- Payment/service lifecycle coupling risk.
- Confirmed payment correction audit risk.
- Reconciliation period/reporting ambiguity risk.
- Notification channel ambiguity risk (OD-1B7-010).
- Future Card Reprint/Care Package dependency risk (OD-1B7-018).
- V0012 migration will require reset target updates during authorized implementation.
- Local branch may be ahead of origin/main; no push is authorized.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
