# Phase 1B.7-B Backend/Data Foundation Implementation Report

## Status

COMPLETE — ALL AUTHORIZED SCOPE IMPLEMENTED AND VALIDATED

## Authorization Chain

- Scope document: docs/architecture/phase-1b7b-backend-data-foundation-scope-and-implementation-plan.md
- Acceptance commit: 0d48748ce5d5a076bfac2e53dbe20926e2cd44c4
- Parent commit: b80b07add213597bc096f82e9cc51bbac80117cf
- Branch: feature/phase-1-organization

## Implemented Artifacts

### Database (2 files)

| File | Purpose |
|------|---------|
| database/migrations/V0012__payment_foundation.sql | 4 tables, 6 permissions, indexes, FKs, CHECK constraints |
| database/rollbacks/U0012__payment_foundation.sql | Reverse-FK-order DROP, permission soft-deactivate |

### Domain Entities (4 files)

| File | Purpose |
|------|---------|
| src/backend/PTKD.Domain/Entities/PaymentTransaction.cs | DRAFT→CONFIRMED lifecycle, hard invariants PAY-004/PAY-006 |
| src/backend/PTKD.Domain/Entities/PaymentTransactionItem.cs | Immutable item with amount > 0 validation |
| src/backend/PTKD.Domain/Entities/PaymentCorrectionHistory.cs | Append-only audit with mandatory reason (PAY-005) |
| src/backend/PTKD.Domain/Entities/ReconciliationPeriod.cs | OPEN→DIRTY→PREPARED→CONFIRMED lifecycle |

### EF Configurations (4 files)

| File | Purpose |
|------|---------|
| src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentTransactionConfiguration.cs | Table mapping, compound unique (company_id, bill_code), FKs |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentTransactionItemConfiguration.cs | FK to PaymentTransaction and Service |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/PaymentCorrectionHistoryConfiguration.cs | FKs to PaymentTransaction and User |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/ReconciliationPeriodConfiguration.cs | Composite unique index (CompanyId, PeriodType, PeriodDate) |

### Application Services and DTOs (6 files)

| File | Purpose |
|------|---------|
| src/backend/PTKD.Application/PaymentManagement/DTOs/PaymentDtos.cs | Request/response DTOs for payment operations |
| src/backend/PTKD.Application/PaymentManagement/DTOs/ReconciliationDtos.cs | Request/response DTOs for reconciliation |
| src/backend/PTKD.Application/PaymentManagement/Services/IPaymentTransactionService.cs | Service interface |
| src/backend/PTKD.Application/PaymentManagement/Services/IReconciliationService.cs | Service interface |
| src/backend/PTKD.Application/PaymentManagement/Services/PaymentTransactionService.cs | Full implementation: bill code gen, two-phase draft, confirm with duplicate check, correction with audit |
| src/backend/PTKD.Application/PaymentManagement/Services/ReconciliationService.cs | Daily/monthly reports, prepare (recalculate), confirm |

### API Controllers (2 files)

| File | Purpose |
|------|---------|
| src/backend/PTKD.Api/Controllers/PaymentTransactionController.cs | 6 endpoints: create, confirm, list, get, correct, soft-delete |
| src/backend/PTKD.Api/Controllers/ReconciliationController.cs | 4 endpoints: daily report, monthly report, prepare, confirm |

### Modified Files (4 files)

| File | Change |
|------|--------|
| src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs | Added 4 DbSet properties |
| src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs | Added 4 DbSet properties |
| src/backend/PTKD.Api/Program.cs | DI registrations for IPaymentTransactionService, IReconciliationService |
| tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs | 4 tables in KnownTables, DROP statements, ResetToV0012 |

### Test Files (8 files)

| File | Tests |
|------|-------|
| tests/backend/PTKD.UnitTests/Domain/PaymentTransactionTests.cs | 17 tests: CreateDraft, SetTotalAmount, Confirm, SoftDelete, CorrectField hard invariants |
| tests/backend/PTKD.UnitTests/Domain/PaymentTransactionItemTests.cs | 4 tests: constructor validation |
| tests/backend/PTKD.UnitTests/Domain/PaymentCorrectionHistoryTests.cs | 3 tests: reason validation |
| tests/backend/PTKD.UnitTests/Domain/ReconciliationPeriodTests.cs | 11 tests: lifecycle guards |
| tests/backend/PTKD.ApiTests/PaymentTransactionApiTests.cs | 10 tests: 401, 403, CRUD, confirm, correct, soft-delete |
| tests/backend/PTKD.ApiTests/ReconciliationApiTests.cs | 4 tests: 401, 403, daily/monthly reports |
| tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs | 6 new permission codes in ExpectedPermissionCodes |
| tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs | V0012 apply/rollback assertions |

### Modified Test Infrastructure (2 files)

| File | Change |
|------|--------|
| tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs | ResetToV0012 |
| tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs | KnownTables, DropKnownSchema, ResetToV0012 |

## Business Rules Coverage

| Rule | Status | Implementation |
|------|--------|---------------|
| PAY-001 | IMPLEMENTED | Cashier self-confirm: CreateDraft + Confirm without approval workflow |
| PAY-002 | IMPLEMENTED | Server validates items.Count > 0, calculates total from items |
| PAY-003 | IMPLEMENTED | Domain entity: one-way DRAFT→CONFIRMED, EnsureNotConfirmed guard |
| PAY-004 | IMPLEMENTED | Domain entity: EnsureNotConfirmed blocks edit/delete after CONFIRMED |
| PAY-005 | IMPLEMENTED | Controller: PAYMENT_CORRECT_CONFIRMED permission check; service: mandatory reason |
| PAY-006 | IMPLEMENTED | Domain entity: CorrectField throws on Id, BillCode, Status, CurrencyCode |
| PAY-007 | IMPLEMENTED | CHECK constraint: status IN ('DRAFT', 'CONFIRMED'); no other statuses |
| PAY-008 | IMPLEMENTED | Confirm checks duplicate service-cycle in confirmed payments |
| PAY-009 | IMPLEMENTED | Correction marks affected daily/monthly reconciliation periods DIRTY |
| PAY-010 | IMPLEMENTED | Single SaveChangesAsync commits correction + items + audit atomically |
| PAY-011 | DEFERRED | Post-commit notifications: not in backend/data scope |
| PAY-012 | IMPLEMENTED | Server-only: totals calculated, actorUserId from JWT, no client trust |

## Acceptance Criteria Coverage

| Criterion | Status | Verification |
|-----------|--------|-------------|
| PAY-01 | PASS | CreateDraft_Valid_Returns201, ConfirmPayment_Valid_ReturnsConfirmed |
| PAY-02 | PASS | ListPayments_NoPermission_Returns403 |
| PAY-03 | PASS | CreateDraft_NoItems_Returns400 |
| PAY-04 | PASS | SoftDeleteConfirmed_Returns400, domain unit tests |
| PAY-05 | PASS | CorrectConfirmed_Valid_Returns200 |
| PAY-06 | PASS | 4 domain unit tests for hard invariant fields |
| PAY-07 | PASS | Reconciliation period dirty-marking tested via correction flow |
| PAY-08 | PASS | Correction audit with before/after JSON snapshots |

## Permissions Seeded

| Code | Module | DataScope | IsSensitive |
|------|--------|-----------|-------------|
| PAYMENT_CREATE_DRAFT | PAYMENT | COMPANY | true |
| PAYMENT_CONFIRM | PAYMENT | COMPANY | true |
| PAYMENT_PRINT | PAYMENT | COMPANY | true |
| PAYMENT_CORRECT_CONFIRMED | PAYMENT | COMPANY | true |
| RECONCILIATION_PREPARE | RECONCILIATION | COMPANY | true |
| RECONCILIATION_CONFIRM | RECONCILIATION | COMPANY | true |

## Validation Results

| Check | Result |
|-------|--------|
| dotnet build | 0 errors, 0 warnings |
| Unit tests (PTKD.UnitTests) | 219 passed, 0 failed |
| Integration tests (PTKD.IntegrationTests) | 203 passed, 0 failed |
| API tests (PTKD.ApiTests) | 295 passed, 0 failed |
| git diff --check | No whitespace errors |

## Bugs Found and Fixed During Implementation

1. **Bill code unique constraint scope**: Original migration had `UNIQUE (bill_code)` globally. Bill code format `PAY-{YYYYMMDD}-{seq}` sequences per company, so different companies on the same date generated identical codes. Fixed to `UNIQUE (company_id, bill_code)`.

2. **SecuritySchemaTests alphabetical ordering**: Initially inserted PAYMENT_* codes before ORGANIZATION_* in ExpectedPermissionCodes. The array must be strictly alphabetically sorted. Fixed ordering.

3. **API test Collection attribute**: New PaymentTransactionApiTests and ReconciliationApiTests were missing `[Collection("Sequential")]`, causing parallel execution with other API tests and DB state interference. Fixed by adding the attribute.

4. **API test InitializeAsync ordering**: `SeedServiceTypeAndServiceAsync` (which calls API) was called before `GrantPermissionAsync` for SERVICE_TYPE_MANAGE, causing 403 failures. Fixed by granting permissions before seeding via API.

## What Was NOT Implemented (Out of Scope)

- Frontend (explicitly excluded by constraints)
- Card Reprint (explicitly excluded)
- Care Package Sales (explicitly excluded)
- Post-commit notifications (PAY-011, not backend/data scope)
- PAYMENT_PRINT endpoint (permission seeded, endpoint deferred to frontend phase)
- Production migration execution (explicitly excluded)

## Conclusion

All authorized backend/data scope from Phase 1B.7-B has been implemented. No blockers encountered. No unsupported business rules were invented. The implementation follows existing patterns (IOrganizationDbContextFactory, IEntityTypeConfiguration, IPermissionEvaluator, snake_case columns, DECIMAL(18,2) amounts, two-phase SaveChanges).
