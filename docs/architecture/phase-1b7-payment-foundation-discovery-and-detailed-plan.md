# Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation Discovery and Detailed Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Post-1B.6 Project Owner next-work decision commit:
  bb93d86665152ec5d1a2ab1c082a9f3704b7feb3

State:
- Phase 1B.6 Service Module Foundation is closed.
- Project Owner selected Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation.
- This document is discovery and detailed planning only.
- This document does not authorize implementation.
- This document does not authorize database migration creation.

## Objective

Define proposed business scope, technical scope, database/API/frontend/workflow strategy, testing strategy, open decisions, and gated implementation phases for Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation.

## Source Documents Reviewed

- docs/architecture/post-1b6-project-owner-next-work-decision.md
- docs/architecture/post-1b6-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b6-project-owner-closure-acceptance.md
- docs/architecture/phase-1b6d-operational-validation-and-closure-acceptance-review.md
- docs/architecture/phase-1b6d-operational-validation-and-closure-report.md
- docs/architecture/phase-1b6c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b6b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md
- docs/business/business-rules.md (PAY-001 through PAY-012, GOV-007, DATA-003, DATA-006, AUTH-009, AUTH-010, SEC-001 through SEC-007)
- docs/business/permission-catalog.md (PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT, PAYMENT_CORRECT_CONFIRMED, RECONCILIATION_PREPARE, RECONCILIATION_CONFIRM)
- docs/business/acceptance-criteria.md (PAY-01 through PAY-08)
- docs/architecture/project-readiness-review.md

Missing sources:
- PTKD-ERP-Master-Context.md: file does not exist at repository root.

## Confirmed Business Rules

The following rules are confirmed by source documents (business-rules.md, permission-catalog.md, acceptance-criteria.md).

### Payment Creation and Confirmation

| Source | Rule |
|--------|------|
| PAY-001 | A user with role CASHIER and permission PAYMENT_CONFIRM may create and confirm the same valid payment. No approval request is created for normal confirmation. |
| PAY-002 | A payment must contain at least one item. `total_amount` is calculated by the server from item amounts and must be greater than zero. |
| PAY-003 | Confirmation moves payment one-way from DRAFT to CONFIRMED. |
| PAY-012 | The client may not supply trusted totals, actor fields or authorization decisions. |
| PAY-01 (AC) | An authorized cashier creates and confirms a valid bill without Approval_Requests. |
| PAY-02 (AC) | A user without PAYMENT_CONFIRM cannot confirm a bill. |
| PAY-03 (AC) | A bill with no items or mismatched total is rejected. |

### Post-Confirmation Immutability

| Source | Rule |
|--------|------|
| PAY-004 | After confirmation, the cashier may view/print but may not edit or delete the payment. **Hard invariant.** |
| PAY-04 (AC) | A cashier cannot edit/delete a payment after CONFIRMED. |

### Admin Correction of Confirmed Payment

| Source | Rule |
|--------|------|
| PAY-005 | Only ADMIN_PAYMENT with PAYMENT_CORRECT_CONFIRMED may correct a confirmed payment. A non-empty correction reason is mandatory. |
| PAY-006 | Confirmed payment correction may not change `id`, `bill_code`, status from CONFIRMED, or currency from VND. **Hard invariant.** |
| PAY-008 | Payment correction must preserve customer/company/service-cycle consistency and must not pay the same cycle twice. |
| PAY-010 | Payment correction, items, aggregates, reconciliation flags and audit are committed atomically. |
| PAY-05 (AC) | ADMIN_PAYMENT may correct allowed company/customer/service/amount/date/method fields with a valid correction package. |
| PAY-06 (AC) | Admin cannot change id, bill_code, CONFIRMED status or VND currency. |
| SEC-003 | Reasons are mandatory for confirmed-payment corrections. |

### Notification After Correction

| Source | Rule |
|--------|------|
| PAY-011 | After commit, notify the cashier/confirming user, PTKD manager and reconciliation accounting group; include old/new company recipients where applicable. |
| PAY-08 (AC) | Correction creates before/after audit, reason and notifications to all required recipients. |

### Reconciliation Impact

| Source | Rule |
|--------|------|
| PAY-009 | When company or payment date changes, all affected old/new daily and monthly reconciliation periods must be marked/recalculated. |
| PAY-07 (AC) | Company/date correction marks/recalculates all affected old/new day/month periods. |

### No Refund / No Cancel / No Partial

| Source | Rule |
|--------|------|
| PAY-007 | No cancel, refund or partial-payment state is introduced by this specification. |

### VND Only

| Source | Rule |
|--------|------|
| PAY-006 | Currency may not be changed from VND. Hard invariant. |

### Company Scope

| Source | Rule |
|--------|------|
| DATA-003 | Service, payment, reconciliation, operational documents and approval requests are scoped by company_id. |
| DATA-006 | Do not use Customers.total_spent as a universal financial source. Calculate spending from confirmed payments by company. |

### Audit and Security

| Source | Rule |
|--------|------|
| GOV-007 | All material payment changes require immutable audit records. |
| SEC-001 | Audit and Approval_Actions are append-only; business users may not update/delete them. |
| SEC-002 | Audit includes actor/acting-as, entity, company, stable action code, changed fields, selected before/after, reason, correlation ID and time. |
| SEC-007 | Notifications are created only after the related transaction commits. |
| AUTH-009 | Every endpoint must re-check permission and data scope at the server. |
| AUTH-010 | Financial stored procedures must validate actor, record state and invariants inside the database transaction. |

### Permission Catalog (Confirmed)

| Permission Code | DataScope | IsSensitive | Purpose |
|---|---|---|---|
| PAYMENT_CREATE_DRAFT | COMPANY | Yes | Create draft payment/bill |
| PAYMENT_CONFIRM | COMPANY | Yes | Confirm a valid draft payment |
| PAYMENT_PRINT | COMPANY | Yes | Print a confirmed payment/bill |
| PAYMENT_CORRECT_CONFIRMED | COMPANY | Yes | Correct a confirmed payment under hard invariants |
| RECONCILIATION_PREPARE | COMPANY | Yes | Prepare reconciliation periods/data |
| RECONCILIATION_CONFIRM | COMPANY | Yes | Confirm reconciliation |

### Business Roles (Confirmed)

| Role | Permissions | Scope |
|---|---|---|
| CASHIER | PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT | COMPANY |
| ACCOUNTANT_RECONCILER | RECONCILIATION_CONFIRM plus approved report/export | COMPANY |
| ADMIN_PAYMENT | PAYMENT_CORRECT_CONFIRMED | Admin group |

## Not Supported / Ambiguous Rules

The following expected rules are NOT fully supported or are ambiguous in reviewed source documents:

1. **Exact payment method values** — business-rules.md does not enumerate allowed payment methods (cash, transfer, etc.). project-readiness-review.md mentions "customer may pay by cash or transfer" but this is in the readiness review, not the formal business rules.

2. **One payment covering multiple services** — No explicit rule in PAY-001 through PAY-012 states that a single payment may cover multiple services. PAY-002 says "at least one item" implying multiple items are allowed, but the item-to-service relationship is not specified.

3. **No deposit** — Not explicitly stated in business-rules.md. PAY-007 says no partial payment, which implies no deposit, but "deposit" is not named.

4. **Bill code generation** — PAY-006 references `bill_code` as immutable after confirmation but does not specify generation format.

5. **Manual reconciliation only** — PAY-009 describes reconciliation period recalculation but does not explicitly state whether reconciliation is manual or automated.

6. **No bank reference code** — Not stated in business-rules.md.

7. **Cashier self-confirm** — PAY-001 confirms a CASHIER with PAYMENT_CONFIRM may create and confirm the same payment. Self-confirm is supported.

8. **Whether draft bills expire or can be deleted** — Not specified. PAY-003 only describes DRAFT → CONFIRMED transition.

9. **Exact fields Admin may correct** — PAY-05 (AC) lists "company/customer/service/amount/date/method" but business-rules.md PAY-005 does not enumerate exact fields beyond hard invariants in PAY-006.

10. **Notification mechanism** — PAY-011 specifies recipients but not channel (in-app, email, SMS).

11. **Service status change on payment** — No rule states whether a Service status changes upon payment confirmation.

12. **Print format/template** — PAYMENT_PRINT permission exists but no specification for print output format.

## Closed Phase 1B.6 Dependency Summary

Phase 1B.6 Service Module Foundation closed with:

- **Services table**: Core entity linking ServiceType to Customer within a Company. Status lifecycle (ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE). Each Service has `applied_price`, `standard_price_snapshot`, `is_override_price`, `cycle_number`.
- **Service_Types table**: Catalog of service types with `standard_price` and `cycle_duration_months`.
- **Service_Price_History table**: Append-only audit of price changes per ServiceType.
- **Service_History table**: Append-only audit of service lifecycle events.
- **ServiceService / ServiceTypeService**: Application services for CRUD and lifecycle management.
- **API v2 controllers**: `api/v2/services` (company-scoped) and `api/v2/service-types` (global).
- **Frontend**: Service Type and Service management pages, API clients, permission-gated navigation.
- **Price snapshots**: Service stores `StandardPriceSnapshot` and `AppliedPrice` at creation/renewal time.
- **Permissions**: SERVICE_VIEW (COMPANY), SERVICE_TYPE_MANAGE (GLOBAL), SERVICE_CREATE_STANDARD (COMPANY), SERVICE_RENEW_STANDARD (COMPANY), SERVICE_PRICE_OVERRIDE_REQUEST (COMPANY), SERVICE_PRICE_OVERRIDE_APPROVE (COMPANY).

This enables Payment Foundation because:
- Payment items reference Services (service_id, applied_price).
- Payment must validate that the service cycle has not already been paid (PAY-008).
- Price consistency requires using the Service's `applied_price` snapshot.
- Company scope for payments aligns with company scope for services.

## Proposed Phase 1B.7 Scope

### In Scope

1. **Payment/Bill entity and data model** — tables for payments, payment items, payment correction history.
2. **DRAFT → CONFIRMED lifecycle** — one-way confirmation with hard invariant enforcement.
3. **Payment items** — each item references a Service; server calculates total from item amounts.
4. **Cashier self-create-and-confirm** — CASHIER with PAYMENT_CONFIRM may create and confirm without approval workflow.
5. **Admin confirmed-payment correction** — ADMIN_PAYMENT with PAYMENT_CORRECT_CONFIRMED, mandatory reason, hard invariant enforcement, atomic commit with audit.
6. **Prevent duplicate service-cycle payment** — a service cycle cannot be paid twice.
7. **VND-only currency** — hard invariant.
8. **Bill code generation** — server-generated, immutable after creation.
9. **Payment method capture** — capture method on each payment (proposed: CASH, TRANSFER pending OD-1B7-001).
10. **Reconciliation period marking** — when company or payment date changes via Admin correction, mark affected daily/monthly periods as dirty/needing recalculation.
11. **Daily/monthly reconciliation report endpoints** — read-only report data for manual reconciliation.
12. **Notification after Admin correction** — notify cashier/confirming user, PTKD manager, reconciliation accounting group.
13. **Audit trail** — before/after snapshots, reason, actor, correlation ID for all payment mutations.
14. **Backend permission enforcement** — all PAYMENT_* and RECONCILIATION_* permissions.
15. **Frontend** — payment list, detail, creation, confirmation, Admin correction, reconciliation report view.
16. **Permission seeding** — PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT, PAYMENT_CORRECT_CONFIRMED, RECONCILIATION_PREPARE, RECONCILIATION_CONFIRM.
17. **Comprehensive tests** — unit, integration, API, frontend.

### Out of Scope / Deferred

- Card Reprint implementation.
- Care Package Sales implementation.
- Production migration.
- Release tag.
- Push.
- External accounting system integration.
- Online bank reconciliation integration.
- Bank reference code.
- Refund flows.
- Cancellation flows.
- Partial payment.
- Deposit.
- Print template/PDF generation (permission exists but print output format is unspecified; mark as deferred to OD-1B7-011).
- Automatic reconciliation confirmation.
- Service status change on payment (not specified by business rules).
- SELL_CARE_PACKAGE workflow.
- Multi-currency support.

## Proposed Data Model Strategy

All entities below are proposed only. V0012/U0012 will be created only after PO implementation authorization.

### Proposed Tables

#### Payment_Transactions

Primary payment/bill entity.

| Column | Type | Notes |
|--------|------|-------|
| id | bigint IDENTITY(1,1) | PK |
| bill_code | nvarchar(50) | Unique, server-generated, immutable after creation |
| company_id | bigint | FK to Companies, NOT NULL |
| customer_id | bigint | FK to Customers, NOT NULL |
| payment_method | nvarchar(20) | CASH / TRANSFER (pending OD-1B7-001) |
| payment_date | datetime2(3) | Date of payment |
| total_amount | decimal(18,0) | Server-calculated from items, VND (no decimals) |
| currency_code | nvarchar(3) | Always "VND", hard invariant |
| status | nvarchar(20) | DRAFT / CONFIRMED |
| confirmed_at | datetime2(3) | NULL until confirmation |
| confirmed_by_user_id | bigint | FK to Users, NULL until confirmation |
| created_by_user_id | bigint | FK to Users, NOT NULL |
| created_at | datetime2(3) | NOT NULL |
| updated_at | datetime2(3) | NULL |
| row_version | rowversion | Concurrency |
| is_deleted | bit | Default 0; soft-delete for DRAFT only (pending OD-1B7-007) |

Indexes:
- UQ_Payment_Transactions_bill_code (unique)
- IX_Payment_Transactions_company_id
- IX_Payment_Transactions_customer_id
- IX_Payment_Transactions_status
- IX_Payment_Transactions_payment_date
- CK_Payment_Transactions_status CHECK (status IN ('DRAFT', 'CONFIRMED'))
- CK_Payment_Transactions_currency CHECK (currency_code = 'VND')

#### Payment_Transaction_Items

Line items linking payments to services.

| Column | Type | Notes |
|--------|------|-------|
| id | bigint IDENTITY(1,1) | PK |
| payment_transaction_id | bigint | FK to Payment_Transactions, NOT NULL |
| service_id | bigint | FK to Services, NOT NULL |
| service_type_code | nvarchar(50) | Denormalized for reporting |
| service_cycle_number | int | Denormalized for duplicate-cycle prevention |
| amount | decimal(18,0) | Item amount in VND |
| description | nvarchar(500) | Optional description |
| created_at | datetime2(3) | NOT NULL |

Indexes:
- IX_Payment_Transaction_Items_payment_transaction_id
- IX_Payment_Transaction_Items_service_id
- UQ_Payment_Transaction_Items_service_cycle (unique filtered: service_id, service_cycle_number WHERE payment confirmed — prevents paying same cycle twice, pending OD-1B7-004)

#### Payment_Correction_History

Append-only audit of Admin corrections to confirmed payments.

| Column | Type | Notes |
|--------|------|-------|
| id | bigint IDENTITY(1,1) | PK |
| payment_transaction_id | bigint | FK to Payment_Transactions, NOT NULL |
| corrected_by_user_id | bigint | FK to Users, NOT NULL |
| reason | nvarchar(1000) | NOT NULL (mandatory per PAY-005) |
| before_data | nvarchar(max) | JSON snapshot of fields before correction |
| after_data | nvarchar(max) | JSON snapshot of fields after correction |
| correlation_id | uniqueidentifier | NOT NULL |
| affected_reconciliation_periods | nvarchar(max) | JSON list of affected day/month periods |
| created_at | datetime2(3) | NOT NULL |

Indexes:
- IX_Payment_Correction_History_payment_transaction_id
- IX_Payment_Correction_History_created_at

#### Reconciliation_Periods

Tracks daily/monthly reconciliation status per company.

| Column | Type | Notes |
|--------|------|-------|
| id | bigint IDENTITY(1,1) | PK |
| company_id | bigint | FK to Companies, NOT NULL |
| period_type | nvarchar(10) | DAILY / MONTHLY |
| period_date | date | For DAILY: the date; for MONTHLY: first day of month |
| status | nvarchar(20) | OPEN / DIRTY / PREPARED / CONFIRMED |
| total_amount | decimal(18,0) | Aggregate of confirmed payments in period |
| transaction_count | int | Count of confirmed payments in period |
| prepared_by_user_id | bigint | FK to Users, NULL |
| prepared_at | datetime2(3) | NULL |
| confirmed_by_user_id | bigint | FK to Users, NULL |
| confirmed_at | datetime2(3) | NULL |
| created_at | datetime2(3) | NOT NULL |
| updated_at | datetime2(3) | NULL |
| row_version | rowversion | Concurrency |

Indexes:
- UQ_Reconciliation_Periods_company_period (unique: company_id, period_type, period_date)
- IX_Reconciliation_Periods_status

### Relationships

- Payment_Transactions → Companies (FK, Restrict)
- Payment_Transactions → Customers (FK, Restrict)
- Payment_Transactions → Users (created_by, confirmed_by; FK, Restrict)
- Payment_Transaction_Items → Payment_Transactions (FK, Restrict)
- Payment_Transaction_Items → Services (FK, Restrict)
- Payment_Correction_History → Payment_Transactions (FK, Restrict)
- Payment_Correction_History → Users (corrected_by; FK, Restrict)
- Reconciliation_Periods → Companies (FK, Restrict)

### Key Design Decisions

- **VND amounts use decimal(18,0)** — no decimal places for Vietnamese Dong.
- **Bill code is server-generated** — format to be decided (OD-1B7-002).
- **Duplicate cycle prevention** — a unique filtered index on (service_id, service_cycle_number) across confirmed payment items prevents paying the same service cycle twice (PAY-008).
- **Reconciliation periods are created/updated lazily** — a period record is created when the first confirmed payment falls in that date/month, or when an Admin correction affects that period.
- **Soft-delete for DRAFT only** — is_deleted applies only to DRAFT payments; confirmed payments cannot be deleted (PAY-004 hard invariant).
- **Correction history is append-only** — each Admin correction creates a new record; previous records are immutable.

### Migration Notes

- Future migration would be V0012__payment_foundation.sql / U0012__payment_foundation.sql.
- V0012 must not be created in this planning task.
- Test fixture reset targets would update from ResetToV0011 to ResetToV0012 only during implementation.
- Permission seeding: 6 PAYMENT/RECONCILIATION permissions in V0012.
- Business_Process_Catalog: no new workflow process required (cashier self-confirms; no approval workflow for normal payment).

## Proposed Backend/API v2 Strategy

### Application Services

**IPaymentTransactionService**
- `CreateDraftAsync(CreatePaymentDraftRequest)` — validate customer, company, services, items, calculate total. Permission: PAYMENT_CREATE_DRAFT (COMPANY).
- `ConfirmAsync(long id, ConfirmPaymentRequest)` — validate DRAFT status, items non-empty, total > 0, no duplicate cycles. Transition to CONFIRMED. Permission: PAYMENT_CONFIRM (COMPANY).
- `GetByIdAsync(long id)` — return payment with items. Permission: PAYMENT_CREATE_DRAFT or PAYMENT_CONFIRM (COMPANY, read access).
- `ListAsync(PaymentSearchParams)` — list by company, optional filters (customer, status, date range, page/pageSize). Permission: PAYMENT_CREATE_DRAFT or PAYMENT_CONFIRM (COMPANY).
- `CorrectConfirmedAsync(long id, CorrectPaymentRequest)` — validate CONFIRMED, enforce hard invariants (id, bill_code, status, currency immutable), apply corrections atomically with audit, mark affected reconciliation periods, send notifications. Permission: PAYMENT_CORRECT_CONFIRMED (COMPANY).

**IReconciliationService**
- `GetDailyReportAsync(long companyId, date)` — aggregate confirmed payments for a single day. Permission: RECONCILIATION_PREPARE (COMPANY).
- `GetMonthlyReportAsync(long companyId, year, month)` — aggregate confirmed payments for a month. Permission: RECONCILIATION_PREPARE (COMPANY).
- `PrepareAsync(long periodId)` — mark period as PREPARED with aggregates. Permission: RECONCILIATION_PREPARE (COMPANY).
- `ConfirmAsync(long periodId)` — mark period as CONFIRMED. Permission: RECONCILIATION_CONFIRM (COMPANY).

### API v2 Endpoints

**PaymentTransactionController** — `api/v2/payments`

| Method | Path | Permission | Notes |
|--------|------|-----------|-------|
| POST | `/payments` | PAYMENT_CREATE_DRAFT (COMPANY) | Create draft |
| POST | `/payments/{id}/confirm` | PAYMENT_CONFIRM (COMPANY) | Confirm draft |
| GET | `/payments?companyId&customerId&status&dateFrom&dateTo&page&pageSize` | PAYMENT_CREATE_DRAFT or PAYMENT_CONFIRM (COMPANY) | List |
| GET | `/payments/{id}` | PAYMENT_CREATE_DRAFT or PAYMENT_CONFIRM (COMPANY) | Detail with items |
| POST | `/payments/{id}/correct` | PAYMENT_CORRECT_CONFIRMED (COMPANY) | Admin correction |

**ReconciliationController** — `api/v2/reconciliation`

| Method | Path | Permission | Notes |
|--------|------|-----------|-------|
| GET | `/reconciliation/daily?companyId&date` | RECONCILIATION_PREPARE (COMPANY) | Daily report |
| GET | `/reconciliation/monthly?companyId&year&month` | RECONCILIATION_PREPARE (COMPANY) | Monthly report |
| POST | `/reconciliation/periods/{id}/prepare` | RECONCILIATION_PREPARE (COMPANY) | Prepare period |
| POST | `/reconciliation/periods/{id}/confirm` | RECONCILIATION_CONFIRM (COMPANY) | Confirm period |

### Error Handling

- Sanitized errors: BadRequest(Title/Detail), NotFound(Title/Detail), Forbid(), Conflict(Title/Detail).
- No raw SQL or internal exception exposure.
- InvalidOperationException → BadRequest for business rule violations.
- DbUpdateConcurrencyException → Conflict for rowversion mismatch.

### Audit Trail

- Payment confirmation creates a SecurityAuditEventRecord via ITransactionalAuditWriter.
- Admin correction creates Payment_Correction_History record AND SecurityAuditEventRecord, atomically.
- Audit includes actor, entity, company, action code, before/after data, reason, correlation ID.

### Concurrency

- rowversion on Payment_Transactions and Reconciliation_Periods.
- Admin correction requires current rowVersion to prevent concurrent modifications.
- Reconciliation prepare/confirm requires current rowVersion.

## Proposed Frontend Strategy

### Pages

1. **PaymentsPage** — List payments by current company. Filters: customer, status, date range. Columns: Bill Code, Customer, Amount, Status, Payment Date, Method. Permission: PAYMENT_CREATE_DRAFT or PAYMENT_CONFIRM (COMPANY).

2. **PaymentCreatePage** — Form: Customer selector, add service items (search active services for selected customer within company), payment method, payment date. Server calculates total. Permission: PAYMENT_CREATE_DRAFT (COMPANY).

3. **PaymentDetailPage** — Read-only detail with items. Actions:
   - Confirm button (DRAFT only, PAYMENT_CONFIRM).
   - Correct button (CONFIRMED only, PAYMENT_CORRECT_CONFIRMED).
   - Print button (CONFIRMED only, PAYMENT_PRINT).

4. **PaymentCorrectPage** or **PaymentCorrectDialog** — Edit form for Admin correction: amount, services, customer, company, payment date, payment method. Reason field (mandatory). Shows immutable fields (id, bill_code, status, currency). Permission: PAYMENT_CORRECT_CONFIRMED (COMPANY).

5. **ReconciliationDailyPage** — Daily report for selected company and date. Shows list of confirmed payments, total, transaction count. Actions: Prepare, Confirm. Permission: RECONCILIATION_PREPARE / RECONCILIATION_CONFIRM (COMPANY).

6. **ReconciliationMonthlyPage** — Monthly report for selected company, year, month. Aggregated view by day. Actions: Prepare, Confirm at month level. Permission: RECONCILIATION_PREPARE / RECONCILIATION_CONFIRM (COMPANY).

### Routes

```
payments                              → PaymentsPage
payments/new                          → PaymentCreatePage
payments/:paymentId                   → PaymentDetailPage
payments/:paymentId/correct           → PaymentCorrectPage
reconciliation/daily                  → ReconciliationDailyPage
reconciliation/monthly                → ReconciliationMonthlyPage
```

### Navigation

```tsx
{hasPermission('PAYMENT_CREATE_DRAFT', 'COMPANY') && (
  <Menu.Item key="payments">
    <Link to="/payments">Payments</Link>
  </Menu.Item>
)}
{hasPermission('RECONCILIATION_PREPARE', 'COMPANY') && (
  <Menu.Item key="reconciliation">
    <Link to="/reconciliation/daily">Reconciliation</Link>
  </Menu.Item>
)}
```

### API Clients

- `src/frontend/src/payments/types.ts` — TypeScript interfaces
- `src/frontend/src/payments/paymentsApi.ts` — API client functions
- `src/frontend/src/payments/errorMessages.ts` — Error message mapping
- `src/frontend/src/reconciliation/types.ts`
- `src/frontend/src/reconciliation/reconciliationApi.ts`

### Tests

- API client tests (mock axiosClient)
- Page tests (render, permission denied, form submission, confirmation flow)
- Error message tests

## Proposed Permission and Security Strategy

### Proposed Permissions (Already in Catalog)

All 6 PAYMENT/RECONCILIATION permissions are already defined in permission-catalog.md. They will be seeded in V0012.

| Permission | DataScope | Sensitive | Purpose |
|---|---|---|---|
| PAYMENT_CREATE_DRAFT | COMPANY | Yes | Create draft payment |
| PAYMENT_CONFIRM | COMPANY | Yes | Confirm draft → CONFIRMED |
| PAYMENT_PRINT | COMPANY | Yes | Print confirmed payment |
| PAYMENT_CORRECT_CONFIRMED | COMPANY | Yes | Admin correction of confirmed payment |
| RECONCILIATION_PREPARE | COMPANY | Yes | Prepare reconciliation data |
| RECONCILIATION_CONFIRM | COMPANY | Yes | Confirm reconciliation |

### Security Enforcement

- Backend authorization is authoritative. Frontend permission gating is convenience only.
- All permissions are COMPANY-scoped; evaluator requires matching companyId.
- PAYMENT_CORRECT_CONFIRMED is restricted to ADMIN_PAYMENT group members.
- Hard invariants (PAY-004, PAY-006) enforced at domain entity level, not just permission.
- Financial operations use explicit transactions (PAY-010, AUTH-010).
- Audit events written via ITransactionalAuditWriter within the same transaction.

### Notification Strategy

- After Admin correction commit: notify confirming user, PTKD manager, reconciliation accounting group (PAY-011).
- Notifications created only after transaction commits (SEC-007).
- Notification mechanism: proposed in-app notification (channel TBD per OD-1B7-010).

## Reconciliation and Reporting Strategy

### Daily PTKD Report

- Lists all confirmed payments for a company on a specific date.
- Shows: bill code, customer, amount, payment method, confirmed by, confirmed at.
- Aggregate: total amount, transaction count.
- Status: OPEN (new/dirty), PREPARED (aggregates frozen), CONFIRMED (signed off).

### Monthly PTKD Report

- Aggregates daily totals for a company in a specific month.
- Shows: day-by-day breakdown, monthly totals.
- Status follows same lifecycle as daily.

### Accounting Reconciliation Support

- Manual reconciliation: PTKD prepares period, Accounting reviews and confirms.
- No automated bank integration.
- No bank reference code.
- No automated accounting system export (deferred).
- When Admin corrects a payment that changes company or payment date, all affected old and new daily/monthly periods are marked DIRTY for re-preparation (PAY-009).

### Report Fields (Proposed)

Daily report: company, date, list of payments (bill_code, customer_name, amount, method, confirmed_by, confirmed_at), total_amount, transaction_count, period_status.

Monthly report: company, year, month, list of daily summaries (date, total_amount, transaction_count, status), monthly_total_amount, monthly_transaction_count.

## Testing Strategy

### Unit Tests
- Payment_Transaction entity: constructor validation, Confirm(), correct() state guards, hard invariant enforcement.
- Payment_Transaction_Item: amount validation, service reference.
- Reconciliation_Period: status transitions, aggregate updates.
- PaymentTransactionService: draft creation, confirmation logic, duplicate cycle prevention, Admin correction with audit.
- ReconciliationService: period creation, preparation, confirmation.

### Integration Tests
- V0012 migration: table existence, FK constraints, unique indexes, CHECK constraints.
- U0012 rollback: table drops, permission soft-deactivation.
- Permission seeding: 6 PAYMENT/RECONCILIATION permission codes.
- SecuritySchemaTests: updated ExpectedPermissionCodes.

### API Tests
- PaymentTransactionController: 401/403, CRUD, confirmation, Admin correction, concurrency.
- ReconciliationController: 401/403, daily/monthly reports, prepare, confirm.
- Sanitized error responses.

### Frontend Tests
- API client tests (mock axiosClient).
- Page render tests.
- Permission denied tests.
- Form submission tests.
- Confirmation flow tests.
- Error message mapping tests.

### Migration/Rollback Tests
- Only after V0012/U0012 creation is authorized.
- MigrationRollbackTests: V0012/U0012 assertions.
- TestDatabaseFixture: ResetToV0012.
- SafeTestWebApplicationFactory: ResetToV0012.

### No Production Migration

No production migration in any testing phase.

## Open Questions / Decisions Required

| ID | Question | Current Evidence | Status | Blocks Implementation? | Recommended Owner |
|---|---|---|---|---|---|
| OD-1B7-001 | Exact allowed payment methods (CASH, TRANSFER, or others?) | project-readiness-review.md mentions "cash or transfer"; business-rules.md does not enumerate | Open | No (default to CASH/TRANSFER, expandable) | PO / Business |
| OD-1B7-002 | Bill code generation format (prefix, sequence, date-based?) | PAY-006 references bill_code but no format specified | Open | No (server-generated, format TBD at implementation) | PO / Business |
| OD-1B7-003 | Whether one payment may cover multiple services from different service types | PAY-002 says "at least one item"; no restriction on service type mix | Open | No (default: allow mix) | PO / Business |
| OD-1B7-004 | Exact mechanism to prevent paying same service cycle twice | PAY-008 requires this; proposed unique filtered index on (service_id, cycle_number) across confirmed items | Proposed | No | Technical |
| OD-1B7-005 | Whether unpaid DRAFT bills can be deleted or only soft-deleted | Not specified in PAY-001 through PAY-012 | Open | No (default: soft-delete DRAFT allowed) | PO / Business |
| OD-1B7-006 | Exact Admin-correctable fields after confirmation | PAY-05 (AC) lists "company/customer/service/amount/date/method"; PAY-006 lists hard-invariant immutable fields | Partially Confirmed | No | PO / Business |
| OD-1B7-007 | Whether is_deleted applies to DRAFT only or also to confirmed (hard invariant says no delete of confirmed) | PAY-004 hard invariant prevents delete of confirmed; DRAFT treatment unclear | Proposed (DRAFT-only) | No | Technical |
| OD-1B7-008 | Whether Service status should change upon payment (e.g., ACTIVE → PAID) | Not specified in any reviewed document | Open | No (default: no status change) | PO / Business |
| OD-1B7-009 | Whether PAYMENT_VIEW permission is needed or PAYMENT_CREATE_DRAFT/PAYMENT_CONFIRM imply read access | permission-catalog.md has no PAYMENT_VIEW; PTKD baseline includes "view company bills" | Open | No (default: CREATE_DRAFT implies read) | PO / Business |
| OD-1B7-010 | Notification channel for Admin correction (in-app, email, or both) | PAY-011 specifies recipients but not channel; SEC-008 mentions notification links | Open | No (default: in-app) | PO / Business |
| OD-1B7-011 | Print output format and template | PAYMENT_PRINT permission exists but no template specification | Deferred | No (defer to future phase) | PO / Business |
| OD-1B7-012 | Whether reconciliation export is in Phase 1B.7 scope | SENSITIVE_EXPORT permission exists in catalog; no specific reconciliation export rule | Open | No (default: defer) | PO |
| OD-1B7-013 | Whether accounting users can only view or also confirm reconciliation | RECONCILIATION_CONFIRM permission exists; ACCOUNTANT_RECONCILER role has it | Confirmed (can confirm) | No | — |
| OD-1B7-014 | Whether payment_date is the date money was received or the date entered in system | Not specified | Open | No (default: date of payment entry) | PO / Business |
| OD-1B7-015 | Whether correction of payment items (adding/removing services) is allowed or only field-level corrections | PAY-05 (AC) includes "service" in correctable fields; PAY-008 requires consistency | Open | No | PO / Business |
| OD-1B7-016 | Whether bill_code format includes company prefix | Not specified | Open | No | PO / Business |
| OD-1B7-017 | Whether confirmed transaction has is_deleted=0 enforced by CHECK constraint or by application logic only | PAY-004 hard invariant; AUTH-010 suggests database-level enforcement | Proposed (application + CHECK) | No | Technical |
| OD-1B7-018 | Whether future Card Reprint/Care Package payments reuse same Payment_Transactions foundation | Recommendation doc suggests yes; not formally decided | Open | No (design for extensibility) | PO |
| OD-1B7-019 | Reconciliation period granularity: is DAILY always required or can some companies use MONTHLY only | Not specified | Open | No (default: both) | PO / Business |
| OD-1B7-020 | Whether total_amount should be decimal(18,0) or decimal(18,2) for VND | VND has no subunit; standard practice is integer-like | Proposed (18,0) | No | Technical |

## Proposed Implementation Phases

1. **Phase 1B.7-A** — Project Owner scope acceptance for this Payment Foundation plan.
2. **Phase 1B.7-B Scope** — Backend/data scope and implementation planning.
3. **Phase 1B.7-B PO Scope Acceptance** — Project Owner acceptance of backend/data scope.
4. **Phase 1B.7-B Implementation** — Backend/data implementation (V0012/U0012, domain entities, EF configs, application services, controllers, backend tests).
5. **Phase 1B.7-B Review** — Backend/data implementation acceptance review.
6. **Phase 1B.7-B PO Acceptance** — Project Owner backend/data implementation acceptance.
7. **Phase 1B.7-C Scope** — Frontend scope and implementation planning.
8. **Phase 1B.7-C PO Scope Acceptance** — Project Owner acceptance of frontend scope.
9. **Phase 1B.7-C Implementation** — Frontend implementation (API clients, pages, routes, tests).
10. **Phase 1B.7-C Review** — Frontend implementation acceptance review.
11. **Phase 1B.7-C PO Acceptance** — Project Owner frontend implementation acceptance.
12. **Phase 1B.7-D Plan** — Operational validation and closure plan.
13. **Phase 1B.7-D PO Plan Acceptance** — Project Owner plan acceptance.
14. **Phase 1B.7-D Execution** — Operational validation execution.
15. **Phase 1B.7-D Review** — Closure acceptance review.
16. **Phase 1B.7 PO Closure** — Project Owner closure acceptance.

The immediate next gate is Project Owner scope acceptance for this plan.

## Risks

1. **Incomplete payment business wording** — Several expected rules (payment methods, bill code format, draft deletion, service status on payment) are not fully specified in business-rules.md. Open questions must be resolved before or during implementation.

2. **Service/payment lifecycle coupling** — Payment references Services; care needed to prevent paying cancelled/expired services or double-paying the same cycle. Domain-level guards and unique indexes mitigate this.

3. **Reconciliation report ambiguity** — The exact fields, format, and granularity of reconciliation reports are not fully specified. Proposed design covers daily and monthly, but details may need PO/Business clarification.

4. **Admin correction audit risk** — Admin correction of confirmed payments is a high-risk operation affecting financial data and reconciliation. Atomic commit, before/after snapshots, mandatory reason, and notification are required (PAY-010, PAY-011). Implementation must be thoroughly tested.

5. **No-refund/no-cancel impact** — PAY-007 means incorrect payments can only be corrected by Admin, never reversed. This increases the importance of validation before confirmation.

6. **Future Card Reprint/Care Package dependency** — Payment foundation should be designed to accommodate future non-service payment items (reprint fees, care package purchases) without reimplementing the core. Proposed design uses Payment_Transaction_Items with service_id FK, which may need to become nullable for future item types.

7. **Migration/reset target risk for V0012** — V0012 migration must update TestDatabaseFixture and SafeTestWebApplicationFactory reset targets. This is a known pattern from V0011 (Phase 1B.6).

8. **Local branch ahead of origin/main** — No push is authorized. Branch may diverge further during implementation.

9. **Scratch/decompiled/FixStrategy untracked files** — Must not be staged in any commit.

10. **Production release deferred** — No production migration, release tag, or push authorized.

11. **Notification infrastructure** — PAY-011 requires notifications but no notification delivery system exists yet. Implementation may need to create notification records only, with delivery channel deferred.

## Recommended Next Gate

Recommended next authorized task:
Project Owner scope acceptance for Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation.

After Project Owner scope acceptance, authorize Phase 1B.7-B backend/data scope and implementation planning only.

Do not authorize implementation yet.

## Non-Goals

This document does not:
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
