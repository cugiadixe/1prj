# Phase 1B.7-B Payment Backend/Data Foundation Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER BACKEND/DATA SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.7 Project Owner scope acceptance commit:
  19222c0c329777b3bc081f259d3081d94f82f13f

State:
- Phase 1B.7 scope is accepted.
- This document is backend/data scope and implementation planning only.
- This document does not authorize implementation.
- This document does not authorize V0012/U0012 creation.

## Objective

Define exact proposed backend/data scope, database strategy, API v2 contract, permission strategy, testing strategy, migration/rollback strategy, and implementation boundaries for Phase 1B.7-B.

## Source Documents Reviewed

- docs/architecture/phase-1b7-project-owner-scope-acceptance.md
- docs/architecture/phase-1b7-payment-foundation-discovery-and-detailed-plan.md
- docs/architecture/post-1b6-project-owner-next-work-decision.md
- docs/architecture/phase-1b6-project-owner-closure-acceptance.md
- docs/business/business-rules.md (PAY-001 through PAY-012, GOV-007, DATA-003, DATA-006, AUTH-009, AUTH-010, SEC-001 through SEC-007)
- docs/business/permission-catalog.md (PAYMENT_*, RECONCILIATION_*)
- docs/business/acceptance-criteria.md (PAY-01 through PAY-08)
- docs/architecture/project-readiness-review.md
- database/migrations/V0011__service_module_foundation.sql (schema patterns, DECIMAL(18,2), permission seeding)
- database/rollbacks/U0011__service_module_foundation.sql (rollback patterns)
- src/backend/PTKD.Domain/Entities/Service.cs (entity patterns)
- src/backend/PTKD.Domain/Entities/ServiceType.cs
- src/backend/PTKD.Application/ServiceManagement/Services/ServiceService.cs (two-phase save, IOrganizationDbContextFactory)
- src/backend/PTKD.Application/ServiceManagement/DTOs/ServiceDtos.cs (DTO patterns)
- src/backend/PTKD.Api/Controllers/ServiceController.cs (controller patterns)
- src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs (EF config patterns)
- tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs (KnownTables, DropKnownSchema, ResetToV0011)
- tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs (ResetToV0011)
- tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs
- tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs (ExpectedPermissionCodes)

## Accepted Business Rule Baseline

### Rules Driving Backend/Data Design

| Rule | Backend/Data Impact |
|------|---------------------|
| PAY-001 | Cashier with PAYMENT_CONFIRM creates and confirms without approval workflow. No Approval_Requests integration. |
| PAY-002 | At least one item. Server calculates total_amount from items. total_amount > 0. |
| PAY-003 | One-way DRAFT → CONFIRMED. Domain entity must enforce. |
| PAY-004 | After CONFIRMED: no edit, no delete by cashier. Hard invariant in domain entity. |
| PAY-005 | Only ADMIN_PAYMENT + PAYMENT_CORRECT_CONFIRMED may correct. Mandatory reason. |
| PAY-006 | Correction cannot change id, bill_code, CONFIRMED status, VND currency. Hard invariant in domain entity. |
| PAY-007 | No cancel, refund, partial-payment states. Status CHECK constraint: DRAFT, CONFIRMED only. |
| PAY-008 | Correction preserves customer/company/service-cycle consistency. No duplicate cycle payment. |
| PAY-009 | Company/date correction marks affected daily/monthly reconciliation periods. |
| PAY-010 | Correction + items + aggregates + reconciliation + audit committed atomically. |
| PAY-011 | Post-commit notify: cashier, PTKD manager, reconciliation accounting group. |
| PAY-012 | Client cannot supply trusted totals/actor/auth. Server-side only. |
| AUTH-010 | Financial operations validated inside database transaction. |
| DATA-003 | Payment/reconciliation scoped by company_id. |
| DATA-006 | Customer spending from confirmed payments by company. |

### Acceptance Criteria Mapping

| AC | Backend/Data Coverage |
|----|----------------------|
| PAY-01 | CreateDraft + Confirm without Approval_Requests. |
| PAY-02 | Permission check on PAYMENT_CONFIRM. |
| PAY-03 | Server rejects empty items / mismatched total. |
| PAY-04 | Domain entity rejects edit/delete after CONFIRMED. |
| PAY-05 | CorrectConfirmed with permission + field restrictions. |
| PAY-06 | Hard invariant enforcement in domain entity. |
| PAY-07 | Reconciliation period marking on company/date correction. |
| PAY-08 | Correction audit with before/after + notifications. |

## Backend/Data Scope Summary

### In Scope for 1B.7-B Backend/Data Implementation

1. V0012 migration: 4 tables, 6 permission seeds, no business process catalog (no approval workflow needed).
2. U0012 rollback.
3. Domain entities: PaymentTransaction, PaymentTransactionItem, PaymentCorrectionHistory, ReconciliationPeriod.
4. EF configurations for all 4 entities.
5. IOrganizationDbContext: 4 new DbSet properties.
6. AppDbContext: 4 new DbSet properties.
7. DTOs: request/response for payment CRUD, confirmation, correction, reconciliation.
8. PaymentTransactionService: create draft, confirm, get/list, correct confirmed.
9. ReconciliationService: daily/monthly queries, prepare, confirm periods.
10. PaymentTransactionController: api/v2/payments.
11. ReconciliationController: api/v2/reconciliation.
12. Program.cs: DI registrations.
13. Permission seeding in V0012.
14. SecuritySchemaTests: updated ExpectedPermissionCodes.
15. TestDatabaseFixture: KnownTables, DropKnownSchema, ResetToV0012.
16. SafeTestWebApplicationFactory: ResetToV0012.
17. MigrationRollbackTests: V0012/U0012 assertions.
18. Unit tests, integration tests, API tests.
19. Audit via ITransactionalAuditWriter / SecurityAuditEventRecord.
20. Notification boundary (create notification records, delivery channel deferred).

### Out of Scope / Deferred

- Frontend implementation.
- Card Reprint implementation.
- Care Package Sales implementation.
- Production migration.
- Release tag.
- Push.
- External accounting integration.
- Automated bank reconciliation.
- Bank reference code.
- Refund/cancellation flows.
- Partial payment.
- Print template/PDF generation.
- Multi-currency.
- Notification delivery channel implementation (records created, delivery deferred).

## Proposed Database Schema Strategy

V0012 migration is proposed only. V0012/U0012 are not created in this task.

### Payment_Transactions

| Column | Type | Required | Description | Notes |
|--------|------|----------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | Primary key | |
| bill_code | NVARCHAR(50) | NOT NULL | Server-generated unique bill code | Immutable after creation (PAY-006) |
| company_id | BIGINT | NOT NULL | FK to Companies | Company scope (DATA-003) |
| customer_id | BIGINT | NOT NULL | FK to Customers | |
| payment_method | NVARCHAR(20) | NOT NULL | CASH or TRANSFER | OD-1B7-001 default |
| payment_date | DATETIME2(3) | NOT NULL | Date of payment | |
| total_amount | DECIMAL(18,2) | NOT NULL | Server-calculated from items | Consistent with Service DECIMAL(18,2) |
| currency_code | NVARCHAR(3) | NOT NULL | Always VND | DEFAULT 'VND', CHECK = 'VND' |
| status | NVARCHAR(20) | NOT NULL | DRAFT or CONFIRMED | CHECK constraint |
| notes | NVARCHAR(500) | NULL | Optional payment notes | |
| confirmed_at | DATETIME2(3) | NULL | Confirmation timestamp | |
| confirmed_by_user_id | BIGINT | NULL | FK to Users | Set on confirmation |
| created_by_user_id | BIGINT | NOT NULL | FK to Users | |
| created_at | DATETIME2(3) | NOT NULL | | |
| updated_at | DATETIME2(3) | NULL | | |
| is_deleted | BIT | NOT NULL | Soft-delete for DRAFT only | DEFAULT 0; confirmed records: application enforces is_deleted=0 |
| row_version | ROWVERSION | NOT NULL | Concurrency | |

FKs:
- FK_PT_company_id → Companies (id), no cascade.
- FK_PT_customer_id → Customers (id), no cascade.
- FK_PT_confirmed_by_user_id → Users (id), no cascade.
- FK_PT_created_by_user_id → Users (id), no cascade.

Indexes:
- UQ_Payment_Transactions_bill_code UNIQUE (bill_code).
- IX_PT_company_id (company_id).
- IX_PT_customer_id (customer_id).
- IX_PT_company_status (company_id, status).
- IX_PT_company_payment_date (company_id, payment_date).

CHECK constraints:
- CK_PT_status CHECK (status IN ('DRAFT', 'CONFIRMED')).
- CK_PT_currency CHECK (currency_code = 'VND').

### Payment_Transaction_Items

| Column | Type | Required | Description | Notes |
|--------|------|----------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | Primary key | |
| payment_transaction_id | BIGINT | NOT NULL | FK to Payment_Transactions | |
| service_id | BIGINT | NOT NULL | FK to Services | |
| service_type_code | NVARCHAR(50) | NOT NULL | Denormalized for reporting | Snapshot from Service.ServiceType at payment time |
| service_cycle_number | INT | NOT NULL | Denormalized for duplicate prevention | Snapshot from Service.CycleNumber |
| amount | DECIMAL(18,2) | NOT NULL | Item amount in VND | |
| description | NVARCHAR(500) | NULL | Optional description | |
| created_at | DATETIME2(3) | NOT NULL | | |

FKs:
- FK_PTI_payment_transaction_id → Payment_Transactions (id), no cascade.
- FK_PTI_service_id → Services (id), no cascade.

Indexes:
- IX_PTI_payment_transaction_id (payment_transaction_id).
- IX_PTI_service_id (service_id).

Unique constraint for duplicate cycle prevention:
- UQ_PTI_confirmed_service_cycle: filtered unique index on (service_id, service_cycle_number) across items that belong to CONFIRMED payments. Implementation approach: application-level check within confirmation transaction (unique filtered index across a JOIN is not straightforward in SQL Server; the domain service validates no existing confirmed payment item for the same service_id + service_cycle_number before confirming).

### Payment_Correction_History

| Column | Type | Required | Description | Notes |
|--------|------|----------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | Primary key | |
| payment_transaction_id | BIGINT | NOT NULL | FK to Payment_Transactions | |
| corrected_by_user_id | BIGINT | NOT NULL | FK to Users | |
| reason | NVARCHAR(1000) | NOT NULL | Mandatory correction reason | PAY-005, SEC-003 |
| before_data | NVARCHAR(MAX) | NOT NULL | JSON snapshot before correction | |
| after_data | NVARCHAR(MAX) | NOT NULL | JSON snapshot after correction | |
| corrected_fields | NVARCHAR(500) | NOT NULL | Comma-separated list of changed field names | |
| correlation_id | UNIQUEIDENTIFIER | NOT NULL | Audit correlation | |
| affected_reconciliation_periods | NVARCHAR(MAX) | NULL | JSON of affected periods | |
| created_at | DATETIME2(3) | NOT NULL | | |

FKs:
- FK_PCH_payment_transaction_id → Payment_Transactions (id), no cascade.
- FK_PCH_corrected_by_user_id → Users (id), no cascade.

Indexes:
- IX_PCH_payment_transaction_id (payment_transaction_id).
- IX_PCH_created_at (created_at).

### Reconciliation_Periods

| Column | Type | Required | Description | Notes |
|--------|------|----------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | Primary key | |
| company_id | BIGINT | NOT NULL | FK to Companies | |
| period_type | NVARCHAR(10) | NOT NULL | DAILY or MONTHLY | |
| period_date | DATE | NOT NULL | DAILY: the date; MONTHLY: first day of month | |
| status | NVARCHAR(20) | NOT NULL | OPEN, DIRTY, PREPARED, CONFIRMED | |
| total_amount | DECIMAL(18,2) | NOT NULL | Aggregate of confirmed payments | DEFAULT 0 |
| transaction_count | INT | NOT NULL | Count of confirmed payments | DEFAULT 0 |
| prepared_by_user_id | BIGINT | NULL | FK to Users | |
| prepared_at | DATETIME2(3) | NULL | | |
| confirmed_by_user_id | BIGINT | NULL | FK to Users | |
| confirmed_at | DATETIME2(3) | NULL | | |
| notes | NVARCHAR(500) | NULL | | |
| created_at | DATETIME2(3) | NOT NULL | | |
| updated_at | DATETIME2(3) | NULL | | |
| row_version | ROWVERSION | NOT NULL | Concurrency | |

FKs:
- FK_RP_company_id → Companies (id), no cascade.
- FK_RP_prepared_by_user_id → Users (id), no cascade.
- FK_RP_confirmed_by_user_id → Users (id), no cascade.

Indexes:
- UQ_RP_company_period_type_date UNIQUE (company_id, period_type, period_date).
- IX_RP_status (status).

CHECK constraints:
- CK_RP_period_type CHECK (period_type IN ('DAILY', 'MONTHLY')).
- CK_RP_status CHECK (status IN ('OPEN', 'DIRTY', 'PREPARED', 'CONFIRMED')).

### Permission Seeding in V0012

6 permissions following V0011 pattern:

```sql
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('PAYMENT_CREATE_DRAFT', 'PAYMENT', 'CREATE_DRAFT', 'COMPANY', 1, 0, 0, 1, N'Create draft payment/bill.'),
    ('PAYMENT_CONFIRM', 'PAYMENT', 'CONFIRM', 'COMPANY', 1, 0, 0, 1, N'Confirm a valid draft payment.'),
    ('PAYMENT_PRINT', 'PAYMENT', 'PRINT', 'COMPANY', 1, 0, 0, 1, N'Print a confirmed payment/bill.'),
    ('PAYMENT_CORRECT_CONFIRMED', 'PAYMENT', 'CORRECT', 'COMPANY', 1, 0, 0, 1, N'Correct a confirmed payment under hard invariants.'),
    ('RECONCILIATION_PREPARE', 'RECONCILIATION', 'PREPARE', 'COMPANY', 1, 0, 0, 1, N'Prepare reconciliation periods/data.'),
    ('RECONCILIATION_CONFIRM', 'RECONCILIATION', 'CONFIRM', 'COMPANY', 1, 0, 0, 1, N'Confirm reconciliation.');
```

### Amount Type Decision

Service Module uses DECIMAL(18,2) for prices (standard_price, applied_price, standard_price_snapshot). Payment amounts will use DECIMAL(18,2) for consistency with the Service Module, even though VND has no subunit. This avoids type mismatch when copying Service.AppliedPrice to PaymentTransactionItem.Amount.

### Bill Code Generation

Server-generated on draft creation. Proposed format: `PAY-{YYYYMMDD}-{sequence}` (e.g., PAY-20260803-0001). Sequence per company per day. Implementation detail; format may be adjusted during implementation.

## Proposed Rollback Strategy

U0012 rollback follows V0011/U0011 pattern. Not created in this task.

Proposed sequence:
1. DROP TABLE Payment_Correction_History (depends on Payment_Transactions).
2. DROP TABLE Payment_Transaction_Items (depends on Payment_Transactions, Services).
3. DROP TABLE Reconciliation_Periods (depends on Companies).
4. DROP TABLE Payment_Transactions (depends on Customers, Companies, Users).
5. Soft-deactivate 6 PAYMENT/RECONCILIATION permissions (UPDATE is_active = 0, per TR_Permissions_PreventDelete).
6. DELETE FROM SchemaVersions WHERE ScriptName LIKE '%V0012%'.

All with IF OBJECT_ID guards and SET XACT_ABORT ON / BEGIN TRANSACTION / COMMIT TRANSACTION.

## Proposed Domain Model Strategy

### PaymentTransaction

- Private setters, private parameterless EF constructor, public static factory.
- Status constants: StatusDraft = "DRAFT", StatusConfirmed = "CONFIRMED".
- Factory: `CreateDraft(companyId, customerId, billCode, paymentMethod, paymentDate, createdByUserId)`.
- `Confirm(userId)`: guards Status == DRAFT, sets Status = CONFIRMED, ConfirmedAt, ConfirmedByUserId.
- `CorrectField(fieldName, newValue)`: guards Status == CONFIRMED, validates hard invariants (cannot change Id, BillCode, Status, CurrencyCode). Returns changed field name for audit.
- `SetTotalAmount(decimal total)`: server-only; validates total > 0.
- `SoftDelete()`: guards Status == DRAFT; sets IsDeleted = true.
- Hard invariant methods: `EnsureNotConfirmed()` for draft-only mutations, `EnsureConfirmed()` for correction-only mutations.
- RowVersion: byte[].

### PaymentTransactionItem

- Private setters, private parameterless EF constructor.
- Constructor: `(paymentTransactionId, serviceId, serviceTypeCode, serviceCycleNumber, amount, description)`.
- Amount validation: > 0.
- Immutable after creation (items are replaced during Admin correction via delete-and-recreate within the same transaction).

### PaymentCorrectionHistory

- Append-only. Private setters, private parameterless EF constructor.
- Constructor: `(paymentTransactionId, correctedByUserId, reason, beforeData, afterData, correctedFields, correlationId, affectedReconciliationPeriods)`.
- Reason validation: not null or empty.

### ReconciliationPeriod

- Private setters, private parameterless EF constructor.
- Status constants: StatusOpen = "OPEN", StatusDirty = "DIRTY", StatusPrepared = "PREPARED", StatusConfirmed = "CONFIRMED".
- `MarkDirty()`: sets Status = DIRTY, resets aggregates.
- `Prepare(userId, totalAmount, transactionCount)`: guards Status IN (OPEN, DIRTY), sets Status = PREPARED.
- `Confirm(userId)`: guards Status == PREPARED, sets Status = CONFIRMED.
- RowVersion: byte[].

## Proposed Application Service Strategy

### PaymentTransactionService

Uses IOrganizationDbContextFactory → CreateDbContext() per method.

**CreateDraftAsync(CreatePaymentDraftRequest, long actorUserId)**
1. Validate customer exists.
2. Validate company exists.
3. Validate CustomerCompanyContext exists.
4. Validate each item's service exists, is ACTIVE, belongs to same company/customer.
5. Validate no duplicate service-cycle in items.
6. Generate bill_code.
7. Calculate total from item amounts.
8. Create PaymentTransaction (two-phase save: parent first, items second).
9. Return PaymentTransactionDto.

**ConfirmAsync(long id, ConfirmPaymentRequest, long actorUserId)**
1. Load PaymentTransaction with items.
2. Validate Status == DRAFT.
3. Validate items non-empty, total > 0.
4. Validate no already-confirmed payment for the same service_id + cycle_number (query existing confirmed items).
5. Call entity.Confirm(actorUserId).
6. Ensure/create affected Reconciliation_Periods (OPEN).
7. Update reconciliation period aggregates.
8. SaveChanges.
9. Write SecurityAuditEventRecord.
10. Return PaymentTransactionDto.

**GetByIdAsync(long id)**
- Load with items. Permission check. Return DTO.

**ListAsync(PaymentSearchParams)**
- Filter by companyId (required), optional: customerId, status, dateFrom, dateTo, page, pageSize.
- Permission check. Return paged result.

**CorrectConfirmedAsync(long id, CorrectPaymentRequest, long actorUserId)**
1. Load PaymentTransaction with items.
2. Validate Status == CONFIRMED.
3. Validate hard invariants: id, bill_code, status, currency_code unchanged.
4. Validate correction reason non-empty.
5. Capture before_data snapshot.
6. Apply field corrections (company, customer, payment_date, payment_method, amount/items).
7. If items changed: validate service/cycle consistency, recalculate total.
8. If company_id or payment_date changed: identify affected old and new reconciliation periods, mark all as DIRTY.
9. Capture after_data snapshot.
10. Create PaymentCorrectionHistory record.
11. SaveChanges atomically (PAY-010).
12. Write SecurityAuditEventRecord.
13. Create notification records for: confirming user, PTKD manager, reconciliation accounting group (PAY-011).
14. Return PaymentTransactionDto.

**SoftDeleteDraftAsync(long id, long actorUserId)**
- Validate Status == DRAFT. Set IsDeleted = true. SaveChanges.

### ReconciliationService

**GetDailyReportAsync(long companyId, DateOnly date)**
- Query confirmed payments for company on date.
- Return daily report DTO with payment list and aggregates.

**GetMonthlyReportAsync(long companyId, int year, int month)**
- Query daily periods for company in month.
- Return monthly report DTO with daily breakdown.

**PrepareAsync(long periodId, long actorUserId)**
- Load period. Validate OPEN or DIRTY.
- Recalculate aggregates from confirmed payments.
- Set status PREPARED.
- SaveChanges.

**ConfirmAsync(long periodId, long actorUserId)**
- Load period. Validate PREPARED.
- Set status CONFIRMED.
- SaveChanges.

### Transaction Boundaries

- CorrectConfirmedAsync: single explicit transaction wrapping correction + items + reconciliation + audit + notification records (PAY-010).
- ConfirmAsync: single transaction wrapping status change + reconciliation period update.
- CreateDraftAsync: two-phase save (parent entity, then items) within implicit transaction.

### Notification Boundary

- Create in-app notification records within the correction transaction.
- Notification delivery (email, push, etc.) is deferred to future infrastructure.
- Notification records contain: recipient_user_id, notification_type, reference_id, message.
- If no Notification table exists, use SecurityAuditEventRecord as minimum audit trail and defer formal notification records to OD-1B7-010.

### Error Handling

- InvalidOperationException → BadRequest (business rule violations).
- ArgumentException → BadRequest (input validation).
- DbUpdateConcurrencyException → Conflict (rowversion mismatch).
- Entity not found → NotFound.
- Permission denied → Forbid.
- No raw SQL or internal exception exposure.

## Proposed API v2 Contract

### PaymentTransactionController — `api/v2/payments`

| Method | Path | Purpose | Permission | Request DTO | Response DTO | Notes |
|--------|------|---------|-----------|-------------|-------------|-------|
| POST | `/payments` | Create draft | PAYMENT_CREATE_DRAFT (COMPANY) | CreatePaymentDraftRequest | PaymentTransactionDto (201) | |
| POST | `/payments/{id}/confirm` | Confirm draft | PAYMENT_CONFIRM (COMPANY) | ConfirmPaymentRequest | PaymentTransactionDto | |
| GET | `/payments?companyId&...` | List | PAYMENT_CREATE_DRAFT (COMPANY) | query params | PagedResult<PaymentTransactionListDto> | companyId required |
| GET | `/payments/{id}` | Detail | PAYMENT_CREATE_DRAFT (COMPANY) | — | PaymentTransactionDto | Includes items |
| POST | `/payments/{id}/correct` | Admin correct | PAYMENT_CORRECT_CONFIRMED (COMPANY) | CorrectPaymentRequest | PaymentTransactionDto | |
| DELETE | `/payments/{id}` | Soft-delete draft | PAYMENT_CREATE_DRAFT (COMPANY) | RowVersion header/body | 204 | DRAFT only |

### ReconciliationController — `api/v2/reconciliation`

| Method | Path | Purpose | Permission | Request DTO | Response DTO | Notes |
|--------|------|---------|-----------|-------------|-------------|-------|
| GET | `/reconciliation/daily?companyId&date` | Daily report | RECONCILIATION_PREPARE (COMPANY) | query params | DailyReconciliationReportDto | |
| GET | `/reconciliation/monthly?companyId&year&month` | Monthly report | RECONCILIATION_PREPARE (COMPANY) | query params | MonthlyReconciliationReportDto | |
| POST | `/reconciliation/periods/{id}/prepare` | Prepare period | RECONCILIATION_PREPARE (COMPANY) | PrepareReconciliationRequest | ReconciliationPeriodDto | |
| POST | `/reconciliation/periods/{id}/confirm` | Confirm period | RECONCILIATION_CONFIRM (COMPANY) | ConfirmReconciliationRequest | ReconciliationPeriodDto | |

## Proposed DTO Strategy

### Payment DTOs

**CreatePaymentDraftRequest**
- CustomerId: long
- CompanyId: long
- PaymentMethod: string (CASH / TRANSFER)
- PaymentDate: DateTime
- Notes: string?
- Items: List<CreatePaymentItemRequest>

**CreatePaymentItemRequest**
- ServiceId: long
- Amount: decimal
- Description: string?

**ConfirmPaymentRequest**
- RowVersion: string (base64)

**CorrectPaymentRequest**
- CustomerId: long?
- CompanyId: long?
- PaymentMethod: string?
- PaymentDate: DateTime?
- Notes: string?
- Items: List<CreatePaymentItemRequest>? (null = no item changes; non-null = replace all items)
- Reason: string (mandatory)
- RowVersion: string (base64)

**PaymentTransactionDto** (response)
- Id, BillCode, CompanyId, CustomerId, PaymentMethod, PaymentDate, TotalAmount, CurrencyCode, Status, Notes, ConfirmedAt, ConfirmedByUserId, CreatedByUserId, CreatedAt, UpdatedAt, RowVersion
- Items: List<PaymentTransactionItemDto>

**PaymentTransactionItemDto**
- Id, PaymentTransactionId, ServiceId, ServiceTypeCode, ServiceCycleNumber, Amount, Description, CreatedAt

**PaymentTransactionListDto** (list item, no nested items)
- Id, BillCode, CompanyId, CustomerId, PaymentMethod, PaymentDate, TotalAmount, Status, CreatedAt

### Reconciliation DTOs

**DailyReconciliationReportDto**
- CompanyId, Date, Period (ReconciliationPeriodDto?), Payments: List<PaymentTransactionListDto>, TotalAmount, TransactionCount

**MonthlyReconciliationReportDto**
- CompanyId, Year, Month, DailySummaries: List<DailySummaryDto>, MonthlyTotalAmount, MonthlyTransactionCount

**DailySummaryDto**
- Date, TotalAmount, TransactionCount, PeriodStatus

**ReconciliationPeriodDto**
- Id, CompanyId, PeriodType, PeriodDate, Status, TotalAmount, TransactionCount, PreparedByUserId, PreparedAt, ConfirmedByUserId, ConfirmedAt, RowVersion

**PrepareReconciliationRequest** / **ConfirmReconciliationRequest**
- RowVersion: string (base64)

## Proposed Permission and Security Strategy

### Exact Permission Codes

All 6 permissions from the accepted permission catalog:

| Permission Code | Module | Action | DataScope | Sensitive | Delegable | Usage |
|---|---|---|---|---|---|---|
| PAYMENT_CREATE_DRAFT | PAYMENT | CREATE_DRAFT | COMPANY | Yes | No | Create draft; also used for read access (list/detail) |
| PAYMENT_CONFIRM | PAYMENT | CONFIRM | COMPANY | Yes | No | Confirm draft → CONFIRMED |
| PAYMENT_PRINT | PAYMENT | PRINT | COMPANY | Yes | No | Print confirmed (deferred to frontend/future) |
| PAYMENT_CORRECT_CONFIRMED | PAYMENT | CORRECT | COMPANY | Yes | No | Admin correct confirmed payment |
| RECONCILIATION_PREPARE | RECONCILIATION | PREPARE | COMPANY | Yes | No | Prepare reconciliation; read reports |
| RECONCILIATION_CONFIRM | RECONCILIATION | CONFIRM | COMPANY | Yes | No | Confirm reconciliation period |

### Security Enforcement

- Backend authorization is authoritative. Frontend gating is convenience only.
- All permissions are COMPANY-scoped; IPermissionEvaluator.EvaluateAsync requires companyId.
- PAYMENT_CREATE_DRAFT implies read access to payments within the company (list/detail).
- PAYMENT_CORRECT_CONFIRMED restricted to ADMIN_PAYMENT group members via permission assignment.
- Hard invariants (PAY-004, PAY-006) enforced at domain entity level, independent of permissions.
- Sanitized errors: no raw SQL, no internal exceptions, no stack traces, no sensitive payload exposure.
- Audit: SecurityAuditEventRecord via ITransactionalAuditWriter for confirmation and correction operations.

## Reconciliation Strategy

### Daily Report
- Confirmed payments for a company on a specific date.
- Fields: bill_code, customer (name/code), total_amount, payment_method, confirmed_by, confirmed_at.
- Aggregate: sum of amounts, count of transactions.
- Period status: OPEN → PREPARED → CONFIRMED.

### Monthly Report
- Daily summaries for a company in a month.
- Each day: date, total_amount, transaction_count, period_status.
- Monthly aggregate: sum of daily totals.

### Reconciliation Period Lifecycle
- OPEN: period exists with initial/recalculated aggregates.
- DIRTY: Admin correction affected this period; aggregates need recalculation.
- PREPARED: PTKD user prepared/froze aggregates.
- CONFIRMED: Accounting confirmed reconciliation.

### Period Marking on Correction
When Admin changes company_id or payment_date on a confirmed payment:
1. Identify old company+date daily period → mark DIRTY.
2. Identify old company+month monthly period → mark DIRTY.
3. Identify new company+date daily period → mark DIRTY (or create as OPEN if not exists).
4. Identify new company+month monthly period → mark DIRTY (or create as OPEN if not exists).
5. All within the same transaction (PAY-010).

### No Bank Reference Code
Confirmed by accepted plan. No bank_reference_code field.

### No Automated Integration
Manual reconciliation only. No external system integration.

## Audit and Notification Strategy

### Append-Only Correction History
- Every Admin correction creates a PaymentCorrectionHistory record.
- before_data / after_data: JSON snapshots of the full PaymentTransaction state.
- corrected_fields: explicit list of what changed.
- correlation_id: links to SecurityAuditEventRecord.
- Immutable: no update/delete of history records.

### Mandatory Correction Reason
- PAY-005 and SEC-003 require non-empty reason.
- Domain entity validates reason before applying correction.
- Reason stored in PaymentCorrectionHistory and SecurityAuditEventRecord.

### SecurityAuditEventRecord
- Written via ITransactionalAuditWriter within the correction/confirmation transaction.
- Action codes: PAYMENT_CONFIRMED, PAYMENT_CORRECTED.
- Includes actor, entity, company, before/after, reason, correlation_id.

### Notification After Correction
- PAY-011 requires notifying: confirming user, PTKD manager, reconciliation accounting group.
- SEC-007: notifications created only after transaction commits.
- Implementation approach: if notification infrastructure exists, create records post-commit. If not, audit trail via SecurityAuditEventRecord serves as minimum trace; formal notifications deferred (OD-1B7-010).

## Test Strategy

### Domain Unit Tests
- PaymentTransaction: CreateDraft validation, Confirm guards, CorrectField hard invariants, SoftDelete DRAFT-only, SetTotalAmount.
- PaymentTransactionItem: constructor validation, amount > 0.
- PaymentCorrectionHistory: constructor, reason validation.
- ReconciliationPeriod: MarkDirty, Prepare guards, Confirm guards.
- Estimated: ~20 tests.

### Application Service Tests (if mockable patterns exist)
- PaymentTransactionService: create draft, confirm with duplicate cycle check, correct with audit.
- Estimated: ~5 tests (handler process code, key logic).

### Integration Tests
- PaymentSchemaTests: 4 table existence, permission seeding (6 codes), rollback.
- MigrationRollbackTests: V0012/U0012 assertions.
- SecuritySchemaTests: 6 PAYMENT/RECONCILIATION codes added to ExpectedPermissionCodes.
- Estimated: ~8 tests.

### API Tests
- PaymentTransactionApiTests: 401, 403, create draft, confirm, list, detail, correct confirmed, soft-delete draft, concurrency.
- ReconciliationApiTests: 401, 403, daily report, monthly report, prepare, confirm.
- Estimated: ~14 tests.

### No Production Migration
All tests run against PTKD_TEST_PHASE1A2 or equivalent test database.

## Open Decisions Carried Forward

| ID | Question | Proposed Handling | Status | Blocks Implementation? |
|---|---|---|---|---|
| OD-1B7-001 | Exact payment methods | Default CASH/TRANSFER via CHECK constraint; extensible | Proposed | No |
| OD-1B7-002 | Bill code format | PAY-{YYYYMMDD}-{sequence} per company per day; adjustable | Proposed | No |
| OD-1B7-003 | Multi-service-type mix in one payment | Allow by default; no restriction on service type mix | Proposed | No |
| OD-1B7-004 | Duplicate cycle prevention mechanism | Application-level check within confirmation transaction | Proposed | No |
| OD-1B7-005 | Draft bill deletion | Soft-delete DRAFT via is_deleted=1; confirmed cannot be deleted | Proposed | No |
| OD-1B7-006 | Admin-correctable fields | company, customer, payment_date, payment_method, amount/items, notes; hard invariants enforced | Confirmed from PAY-05/PAY-06 | No |
| OD-1B7-007 | is_deleted scope | DRAFT only; confirmed records always is_deleted=0 | Proposed | No |
| OD-1B7-008 | Service status on payment | No status change; Service remains in current status | Proposed | No |
| OD-1B7-009 | PAYMENT_VIEW permission | Not needed; PAYMENT_CREATE_DRAFT implies read access | Proposed | No |
| OD-1B7-010 | Notification channel | Audit trail via SecurityAuditEventRecord; formal notifications deferred | Carried Forward | No |
| OD-1B7-011 | Print format | Deferred to frontend/future phase | Deferred | No |
| OD-1B7-012 | Reconciliation export | Deferred to future phase | Deferred | No |
| OD-1B7-013 | Accounting confirm reconciliation | Confirmed: RECONCILIATION_CONFIRM permission allows it | Confirmed | No |
| OD-1B7-014 | payment_date semantics | Date of payment entry in system | Proposed | No |
| OD-1B7-015 | Correction of items | Replace-all strategy: correction may replace item set entirely | Proposed | No |
| OD-1B7-016 | bill_code company prefix | Included in proposed format PAY-{date}-{seq} | Proposed | No |
| OD-1B7-017 | is_deleted enforcement | Application-level + domain entity guard; no CHECK constraint (cross-column CHECK on status+is_deleted is fragile) | Proposed | No |
| OD-1B7-018 | Card Reprint/Care Package reuse | Design service_id FK as required now; future item types may need nullable FK or separate table | Carried Forward | No |
| OD-1B7-019 | Reconciliation period granularity | Both DAILY and MONTHLY supported | Proposed | No |
| OD-1B7-020 | Amount decimal precision | DECIMAL(18,2) consistent with Service Module | Confirmed | No |

No open decision blocks implementation.

## Implementation Sequence if Accepted

1. V0012 migration: 4 tables, 6 permission seeds.
2. U0012 rollback.
3. Domain entities: PaymentTransaction, PaymentTransactionItem, PaymentCorrectionHistory, ReconciliationPeriod.
4. EF configurations: 4 IEntityTypeConfiguration<T>.
5. IOrganizationDbContext: 4 DbSet properties.
6. AppDbContext: 4 DbSet properties.
7. DTOs: request/response DTOs.
8. PaymentTransactionService + IPaymentTransactionService.
9. ReconciliationService + IReconciliationService.
10. PaymentTransactionController.
11. ReconciliationController.
12. Program.cs: DI registrations.
13. TestDatabaseFixture: KnownTables, DropKnownSchema, ResetToV0012.
14. SafeTestWebApplicationFactory: ResetToV0012.
15. SecuritySchemaTests: 6 PAYMENT/RECONCILIATION codes.
16. PaymentSchemaTests: table existence, permissions, rollback.
17. MigrationRollbackTests: V0012/U0012 assertions.
18. Unit tests: domain entity tests.
19. API tests: PaymentTransactionApiTests, ReconciliationApiTests.
20. Implementation report.
21. Build + test validation.

Do not implement in this task.

## Validation Plan for Future Implementation

```
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
git diff --check
```

## Risks

1. **Payment/service lifecycle coupling** — Payment items reference Services; confirmation must validate service state and cycle uniqueness within the transaction.
2. **Confirmed payment correction complexity** — Correction of company/date triggers reconciliation period marking across up to 4 periods atomically (PAY-009, PAY-010).
3. **Reconciliation period ambiguity** — Exact report fields and export format remain partially open.
4. **Notification channel** — No notification delivery infrastructure exists; correction audit trail is the minimum trace.
5. **Future Card Reprint/Care Package dependency** — Payment_Transaction_Items.service_id is NOT NULL; future non-service items would need schema change.
6. **Migration/reset target update** — V0012 requires updating TestDatabaseFixture and SafeTestWebApplicationFactory reset targets.
7. **Bill code generation** — Proposed format needs sequence management per company per day; must be concurrency-safe.
8. **Local branch ahead of origin/main** — No push authorized.
9. **Scratch/decompiled/FixStrategy files** — Must not be staged.
10. **Production release** — Deferred.

## Recommended Next Gate

Recommended next authorized task:
Project Owner backend/data scope acceptance for Phase 1B.7-B.

After Project Owner backend/data scope acceptance, authorize Phase 1B.7-B backend/data implementation only.

Do not authorize:
- frontend implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

This document does not:
- implement Payment,
- create V0012,
- create U0012,
- modify source code,
- modify tests,
- modify frontend/backend files,
- modify business docs,
- implement Card Reprint,
- implement Care Package Sales,
- run production migration,
- create release tag,
- push.
