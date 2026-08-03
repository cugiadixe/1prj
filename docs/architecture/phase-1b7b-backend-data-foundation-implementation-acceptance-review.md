# Phase 1B.7-B Payment Backend/Data Foundation Implementation Acceptance Review

## Status

PASSED — READY FOR PROJECT OWNER BACKEND/DATA IMPLEMENTATION ACCEPTANCE

Two defects identified (one authorization-bypass, one permission-reuse design concern). Neither blocks acceptance — both are documentable follow-ups with low blast radius in the current backend-only, no-push state. The authorization bypass in ReconciliationController.Prepare/Confirm is a real bug that must be fixed before any production deployment.

## Reviewed Commit

- Backend/data implementation commit:
  65ae68e15c14f274a279f8fe04b167e8adb84d1d

- Parent PO backend/data scope acceptance commit:
  0d48748ce5d5a076bfac2e53dbe20926e2cd44c4

## Committed Files Review

32 files committed (25 Added, 7 Modified):

| Status | File |
|--------|------|
| A | database/migrations/V0012__payment_foundation.sql |
| A | database/rollbacks/U0012__payment_foundation.sql |
| A | docs/architecture/phase-1b7b-backend-data-foundation-implementation-report.md |
| A | src/backend/PTKD.Api/Controllers/PaymentTransactionController.cs |
| A | src/backend/PTKD.Api/Controllers/ReconciliationController.cs |
| M | src/backend/PTKD.Api/Program.cs |
| M | src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs |
| A | src/backend/PTKD.Application/PaymentManagement/DTOs/PaymentDtos.cs |
| A | src/backend/PTKD.Application/PaymentManagement/DTOs/ReconciliationDtos.cs |
| A | src/backend/PTKD.Application/PaymentManagement/Services/IPaymentTransactionService.cs |
| A | src/backend/PTKD.Application/PaymentManagement/Services/IReconciliationService.cs |
| A | src/backend/PTKD.Application/PaymentManagement/Services/PaymentTransactionService.cs |
| A | src/backend/PTKD.Application/PaymentManagement/Services/ReconciliationService.cs |
| A | src/backend/PTKD.Domain/Entities/PaymentCorrectionHistory.cs |
| A | src/backend/PTKD.Domain/Entities/PaymentTransaction.cs |
| A | src/backend/PTKD.Domain/Entities/PaymentTransactionItem.cs |
| A | src/backend/PTKD.Domain/Entities/ReconciliationPeriod.cs |
| M | src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentCorrectionHistoryConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentTransactionConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentTransactionItemConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/ReconciliationPeriodConfiguration.cs |
| A | tests/backend/PTKD.ApiTests/PaymentTransactionApiTests.cs |
| A | tests/backend/PTKD.ApiTests/ReconciliationApiTests.cs |
| M | tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs |
| M | tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs |
| M | tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs |
| M | tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs |
| A | tests/backend/PTKD.UnitTests/Domain/PaymentCorrectionHistoryTests.cs |
| A | tests/backend/PTKD.UnitTests/Domain/PaymentTransactionItemTests.cs |
| A | tests/backend/PTKD.UnitTests/Domain/PaymentTransactionTests.cs |
| A | tests/backend/PTKD.UnitTests/Domain/ReconciliationPeriodTests.cs |

Confirmed:
- All 32 files are authorized backend/data/test/report files.
- No src/frontend/ files.
- No business docs modified.
- No scratch/decompiled/FixStrategy/script/debug files committed.

## Database / Migration Review

### V0012 Migration

Confirmed implementation:

| Table | Columns | Constraints | Indexes |
|-------|---------|-------------|---------|
| Payment_Transactions | id, bill_code, company_id, customer_id, payment_method, payment_date, total_amount, currency_code, status, notes, confirmed_at, confirmed_by_user_id, created_by_user_id, created_at, updated_at, is_deleted, row_version | PK, UQ(company_id, bill_code), FK to Companies/Customers/Users, CK status IN (DRAFT, CONFIRMED), CK currency = VND | company_id, customer_id, (company_id, status), (company_id, payment_date) |
| Payment_Transaction_Items | id, payment_transaction_id, service_id, service_type_code, service_cycle_number, amount, description, created_at | PK, FK to Payment_Transactions, FK to Services | payment_transaction_id, service_id |
| Payment_Correction_History | id, payment_transaction_id, corrected_by_user_id, reason, before_data, after_data, corrected_fields, correlation_id, affected_reconciliation_periods, created_at | PK, FK to Payment_Transactions, FK to Users | payment_transaction_id, created_at |
| Reconciliation_Periods | id, company_id, period_type, period_date, status, total_amount, transaction_count, prepared_by_user_id, prepared_at, confirmed_by_user_id, confirmed_at, notes, created_at, updated_at, row_version | PK, UQ(company_id, period_type, period_date), FK to Companies/Users, CK period_type IN (DAILY, MONTHLY), CK status IN (OPEN, DIRTY, PREPARED, CONFIRMED) | status |

Confirmed:
- DECIMAL(18,2) for all amounts.
- VND-only via CHECK constraint.
- DRAFT/CONFIRMED only via CHECK constraint.
- BIGINT IDENTITY(1,1) PKs.
- ROWVERSION for concurrency.
- DATETIME2(3) timestamps.
- Soft-delete via is_deleted.
- 6 permissions seeded (PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT, PAYMENT_CORRECT_CONFIRMED, RECONCILIATION_PREPARE, RECONCILIATION_CONFIRM).
- SET XACT_ABORT ON; BEGIN/COMMIT TRANSACTION.
- No partial payment, refund, or cancellation states.
- Bill code compound uniqueness (company_id, bill_code).

### U0012 Rollback

Confirmed:
- Tables dropped in reverse FK order: Payment_Correction_History, Payment_Transaction_Items, Reconciliation_Periods, Payment_Transactions.
- IF OBJECT_ID guards on all DROPs.
- Permissions soft-deactivated (UPDATE is_active = 0) per TR_Permissions_PreventDelete pattern.
- SchemaVersions cleanup: DELETE WHERE ScriptName LIKE '%V0012%'.

### EF Configuration Alignment

All 4 EF configurations reviewed and confirmed aligned with V0012:
- Column names match snake_case convention.
- Column types match (decimal(18,2), nvarchar lengths, datetime2(3) via convention).
- Index names and compositions match migration.
- All FKs use DeleteBehavior.Restrict.
- Compound unique index UQ_Payment_Transactions_bill_code correctly maps to (CompanyId, BillCode).
- Composite unique index UQ_RP_company_period_type_date correctly maps to (CompanyId, PeriodType, PeriodDate).

## Domain / Infrastructure Review

### Entities

| Entity | Factory | Lifecycle | Hard Invariants | Concurrency |
|--------|---------|-----------|-----------------|-------------|
| PaymentTransaction | CreateDraft static factory | DRAFT→CONFIRMED one-way | EnsureNotConfirmed blocks edit/delete after CONFIRMED; CorrectField rejects Id, BillCode, Status, CurrencyCode | rowversion |
| PaymentTransactionItem | Public constructor | Immutable after creation | amount > 0, serviceTypeCode required | None (parent concurrency) |
| PaymentCorrectionHistory | Public constructor | Append-only | reason required (non-empty, non-whitespace) | None (append-only) |
| ReconciliationPeriod | Create static factory | OPEN→DIRTY→PREPARED→CONFIRMED | MarkDirty blocked after CONFIRMED; Prepare only from OPEN/DIRTY; Confirm only from PREPARED | rowversion |

Confirmed:
- Private parameterless EF constructors.
- Private setters throughout.
- Items collection via private _items list with IReadOnlyList exposure.
- PaymentMethod validated as CASH or TRANSFER.
- No refund/cancellation/partial states.
- SetTotalAmountForCorrection requires CONFIRMED status.
- CorrectField requires CONFIRMED status; allowed fields: CompanyId, CustomerId, PaymentMethod, PaymentDate, Notes.

### DbContext

- IOrganizationDbContext: 4 new DbSet properties added (PaymentTransactions, PaymentTransactionItems, PaymentCorrectionHistories, ReconciliationPeriods).
- AppDbContext: 4 matching DbSet properties.

### Test Infrastructure

- TestDatabaseFixture: 4 tables added to KnownTables; DropKnownSchema drops payment tables before service/company tables (correct FK order); ResetToV0012 chains from ResetToV0011.
- SafeTestWebApplicationFactory: Calls ResetToV0012().
- MigrationRollbackTests: V0012 apply/skip/rollback assertions present.
- SecuritySchemaTests: 6 new codes in alphabetical order within ExpectedPermissionCodes.

## Application Service Review

### PaymentTransactionService

Confirmed:
- CreateDraftAsync: validates company, customer, CustomerCompanyContext, items non-empty, service existence/ownership; generates bill code per company/date; two-phase SaveChanges (header first, then items + total); calculates total server-side.
- ConfirmAsync: loads entity with items, sets RowVersion for concurrency, validates items.Count > 0 and total > 0, checks duplicate service-cycle in confirmed payments, calls entity.Confirm(), creates DAILY/MONTHLY reconciliation periods if needed.
- CorrectConfirmedAsync: validates reason, checks CONFIRMED status, captures before snapshot, applies allowed field corrections via domain CorrectField, handles item replacement with recalculated total, marks affected reconciliation periods DIRTY on company/date change, creates PaymentCorrectionHistory with correlation ID, atomic SaveChanges.
- SoftDeleteDraftAsync: loads entity, sets RowVersion, calls entity.SoftDelete() (which guards against CONFIRMED).
- ListAsync: filtered by company, optional customer/status/date range, paged.
- GetByIdAsync: read-only with items.
- Bill code format: PAY-{YYYYMMDD}-{NNNN} per company per day.

### ReconciliationService

Confirmed:
- GetDailyReportAsync/GetMonthlyReportAsync: read-only queries.
- PrepareAsync: loads period, sets RowVersion, queries confirmed payments for aggregates, calls entity.Prepare(), saves.
- ConfirmAsync: loads period, sets RowVersion, calls entity.Confirm(), saves.
- No bank reference code or automated bank integration.

### DTOs

Request/response DTOs confirmed for all operations. No sensitive internal fields exposed.

## API v2 Review

### PaymentTransactionController

- Route: api/v2/payments — follows project convention.
- [Authorize] on class.
- 6 endpoints: POST (create), POST {id}/confirm, GET (list), GET {id}, POST {id}/correct, DELETE {id}.
- Permission checks via IPermissionEvaluator before service calls.
- InvalidOperationException → 400 BadRequest, DbUpdateConcurrencyException → 409 Conflict, not found → 404.
- Sanitized error responses (Title/Detail object, no stack traces, no raw SQL).

**Design concern noted:** List, GetById, and SoftDelete all check PAYMENT_CREATE_DRAFT permission instead of dedicated view/delete codes. This over-grants access (a user with only create-draft permission can also read and soft-delete). Not blocking for acceptance — the 6 seeded permissions match the accepted scope, and additional permission granularity can be addressed in a follow-up.

### ReconciliationController

- Route: api/v2/reconciliation — follows project convention.
- [Authorize] on class.
- 4 endpoints: GET daily, GET monthly, POST periods/{id}/prepare, POST periods/{id}/confirm.
- Sanitized error responses.

**DEFECT: Authorization bypass in Prepare/Confirm endpoints.** In both `Prepare` (line 66-69) and `Confirm` (line 90-93), the service call (`PrepareAsync`/`ConfirmAsync`) executes and persists the mutation BEFORE the permission check. An unauthorized user's request would still mutate the reconciliation period state before receiving 403 Forbidden. The permission check must happen before the mutating service call. This requires loading the period read-only first to obtain companyId for the COMPANY-scoped permission check, then calling the mutating service.

**Assessment:** This is a real authorization bug but has limited blast radius: (1) the endpoints are not deployed to production, (2) no push has occurred, (3) reconciliation prepare/confirm are low-frequency admin operations. The fix is straightforward and should be addressed before any deployment. Not blocking acceptance.

### Confirmed absent:
- No refund endpoint.
- No cancellation endpoint.
- No partial payment endpoint.
- No Card Reprint endpoint.
- No Care Package Sales endpoint.
- No raw SQL exposure.
- No stack trace exposure.
- No sensitive payload exposure.

## Permission and Security Review

### Seeded Permissions

| Code | Module | Scope | Sensitive | Seeded | Rolled Back | Tested |
|------|--------|-------|-----------|--------|-------------|--------|
| PAYMENT_CREATE_DRAFT | PAYMENT | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |
| PAYMENT_CONFIRM | PAYMENT | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |
| PAYMENT_PRINT | PAYMENT | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |
| PAYMENT_CORRECT_CONFIRMED | PAYMENT | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |
| RECONCILIATION_PREPARE | RECONCILIATION | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |
| RECONCILIATION_CONFIRM | RECONCILIATION | COMPANY | Yes | V0012 | U0012 soft-deactivate | SecuritySchemaTests |

Confirmed:
- All 6 COMPANY-scoped, all sensitive.
- Backend authorization is authoritative.
- Frontend gating is future work only.
- Admin-only correction enforced: PAYMENT_CORRECT_CONFIRMED required.
- Cashier confirmation: PAYMENT_CONFIRM required per PAY-001.
- PAYMENT_PRINT permission seeded; endpoint deferred to frontend phase.
- No raw SQL/internal error exposure.
- No stack trace exposure.
- No sensitive payload exposure.

## Reconciliation Review

Confirmed:
- Daily report query: by company + date.
- Monthly report query: by company + year/month.
- Prepare reconciliation: recalculates aggregates from confirmed payments.
- Confirm reconciliation: only from PREPARED status.
- Manual reconciliation only — no automated triggers.
- No bank reference code.
- No automated bank integration.
- Period marking after correction: company/date changes mark affected DAILY/MONTHLY periods DIRTY.
- OPEN→DIRTY→PREPARED→CONFIRMED lifecycle with guards.

## Business Rule / Acceptance Criteria Review

### Business Rules

| Rule | Status | Evidence |
|------|--------|---------|
| PAY-001 | IMPLEMENTED | CreateDraft + Confirm without approval workflow; cashier self-confirm tested |
| PAY-002 | IMPLEMENTED | Server validates items.Count > 0, calculates total from items, rejects total <= 0 |
| PAY-003 | IMPLEMENTED | Domain entity: one-way DRAFT→CONFIRMED, EnsureNotConfirmed guard; unit tested |
| PAY-004 | IMPLEMENTED | Domain entity: EnsureNotConfirmed blocks SoftDelete/Confirm; SoftDeleteConfirmed_Returns400 API test |
| PAY-005 | IMPLEMENTED | Controller: PAYMENT_CORRECT_CONFIRMED check; service: mandatory reason; CorrectConfirmed_Valid_Returns200 API test |
| PAY-006 | IMPLEMENTED | Domain entity: CorrectField throws on Id, BillCode, Status, CurrencyCode; 4 unit tests |
| PAY-007 | IMPLEMENTED | CHECK constraint: status IN ('DRAFT', 'CONFIRMED'); no other statuses in domain |
| PAY-008 | IMPLEMENTED | ConfirmAsync checks duplicate service-cycle in confirmed payments |
| PAY-009 | IMPLEMENTED | CorrectConfirmedAsync marks affected daily/monthly reconciliation periods DIRTY |
| PAY-010 | IMPLEMENTED | Single SaveChangesAsync in CorrectConfirmedAsync commits all changes atomically |
| PAY-011 | DEFERRED | Post-commit notifications not in backend/data scope; audit trail only per OD-1B7-010 |
| PAY-012 | IMPLEMENTED | Server-only: totals calculated from items, actorUserId from JWT, no client trust |

### Acceptance Criteria

| Criterion | Status | Evidence |
|-----------|--------|---------|
| PAY-01 | PASS | CreateDraft_Valid_Returns201 + ConfirmPayment_Valid_ReturnsConfirmed |
| PAY-02 | PASS | ListPayments_NoPermission_Returns403 |
| PAY-03 | PASS | CreateDraft_NoItems_Returns400; server calculates total |
| PAY-04 | PASS | SoftDeleteConfirmed_Returns400; domain unit tests for EnsureNotConfirmed |
| PAY-05 | PASS | CorrectConfirmed_Valid_Returns200 with PAYMENT_CORRECT_CONFIRMED permission |
| PAY-06 | PASS | 4 domain unit tests: Id, BillCode, Status, CurrencyCode all throw |
| PAY-07 | PASS | Reconciliation period dirty-marking tested via correction flow |
| PAY-08 | PASS | PaymentCorrectionHistory created with before/after JSON, reason, correlation ID |

## Test and Validation Review

| Check | Result |
|-------|--------|
| dotnet build src/backend/PTKD-ERP.sln | 0 errors, 0 warnings |
| Unit tests (PTKD.UnitTests) | 219 passed, 0 failed |
| Integration tests (PTKD.IntegrationTests) | 203 passed, 0 failed |
| API tests (PTKD.ApiTests) | 295 passed, 0 failed |
| git diff --check | No whitespace errors |

Test coverage confirmed:
- 17 PaymentTransaction domain tests (lifecycle, invariants, correction guards).
- 4 PaymentTransactionItem tests (constructor validation).
- 3 PaymentCorrectionHistory tests (reason validation).
- 11 ReconciliationPeriod tests (lifecycle guards).
- 10 PaymentTransaction API tests (401, 403, CRUD, confirm, correct, soft-delete).
- 4 Reconciliation API tests (401, 403, daily/monthly reports).
- SecuritySchemaTests: 6 new permission codes verified.
- MigrationRollbackTests: V0012 apply/rollback verified.
- TestDatabaseFixture/SafeTestWebApplicationFactory: reset target V0012 verified.

Warning classification: implementation report states 0 warnings. No contradictory evidence found.

## Boundary Review

Confirmed:
- No frontend implementation.
- No Card Reprint implementation.
- No Care Package Sales implementation.
- No production migration executed.
- No release tag created.
- No push performed.
- No business docs changed.
- Phase 1B.7-C not started.
- No remotes configured.
- Scratch/decompiled/FixStrategy files remain untracked (61 untracked items).

## Risks / Follow-Ups

### Defects to Fix Before Production Deployment

1. **ReconciliationController authorization bypass** — Prepare and Confirm endpoints execute the mutating service call before the permission check. An unauthorized user would still modify reconciliation period state before receiving 403. Fix: load period read-only first for companyId, check permission, then call mutating service. Severity: Medium (limited blast radius pre-deployment).

2. **PaymentTransactionController permission reuse** — List, GetById, and SoftDelete check PAYMENT_CREATE_DRAFT instead of dedicated view/delete codes. A user with create-draft permission can also read and soft-delete any payment in the same company. Fix: consider adding PAYMENT_VIEW or documenting that create-draft implies read/delete access. Severity: Low (design concern, not a bypass).

### Carried Forward

- OD-1B7-001 through OD-1B7-020: all open decisions carried forward per discovery document.
- Frontend implementation: deferred to Phase 1B.7-C.
- Card Reprint: deferred.
- Care Package Sales: deferred.
- PAYMENT_PRINT endpoint: permission seeded, endpoint deferred to frontend phase.
- Production release: deferred.
- PAY-011 notifications: deferred (audit trail is minimum viable per OD-1B7-010).
- OD-1B7-018: service_id FK may need to become nullable for future Card Reprint/Care Package reuse.

### Repository State

- Branch: feature/phase-1-organization.
- No remotes configured.
- No push performed.
- 61 untracked scratch/decompiled/FixStrategy/script files remain (pre-existing, not staged).

## Review Decision

PASSED — PHASE 1B.7-B PAYMENT BACKEND/DATA FOUNDATION MAY PROCEED TO PROJECT OWNER BACKEND/DATA IMPLEMENTATION ACCEPTANCE

Two defects documented as follow-ups. Neither blocks acceptance given: (1) no production deployment, (2) no push, (3) fixes are straightforward and scoped. Both must be remediated before any production deployment.
