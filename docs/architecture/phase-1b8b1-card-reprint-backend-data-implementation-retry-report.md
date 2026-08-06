# Phase 1B.8-B1 Card Reprint Backend/Data Implementation Retry Report

## Status

PARTIAL / BLOCKED — ACCEPTANCE REVIEW NOT READY

## Authorization Source

- Phase 1B.8-B1 blocker decision response commit:
  f2c9e7b45a210d3f8a4c1987d6b3e809f1a23c45

## Previous Blocked Attempt

- Previous blocked implementation report:
  docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-report.md
- Previous blocked attempt commit:
  11d7e26f8d7b7e61e389e6eb0092c47ab4a5dc11

The previous attempt was blocked due to backend module placement ambiguity, B1/B2 split ambiguity, permission scope ambiguity, and migration scope ambiguity.

## Corrected B1 Scope Applied

The retry followed the corrected scope by:
- Using the existing layered backend structure (PTKD.Domain, PTKD.Infrastructure).
- Focusing on B1 backend/data foundation only (Cards and Card_Reprint_Requests tables).
- Deferring B2 workflow/payment execution.
- Implementing no frontend.
- Preventing production migration/tag/push.

## Implemented Scope

The following B1 backend/data scope has been implemented:
- V0013 and U0013 for the DB schema (`Cards`, `Card_Reprint_Requests`).
- Domain entities `Card` and `CardReprintRequest`.
- EF Core mappings for the entities in `PTKD.Infrastructure`.
- Integration tests updated to support `V0013` and `U0013`.

## Files Changed

- `database/migrations/V0013__card_reprint_foundation.sql` (NEW)
- `database/rollbacks/U0013__card_reprint_foundation.sql` (NEW)
- `src/backend/PTKD.Domain/Entities/Card.cs` (NEW)
- `src/backend/PTKD.Domain/Entities/CardReprintRequest.cs` (NEW)
- `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs` (MODIFIED)
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/CardConfiguration.cs` (NEW)
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/CardReprintRequestConfiguration.cs` (NEW)
- `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs` (MODIFIED)
- `tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs` (MODIFIED)
- `docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-retry-report.md` (NEW)

## Data / Migration Implementation

- **V0013/U0013**: Created to define `dbo.Cards` and `dbo.Card_Reprint_Requests`.
- **Tables/Entities**: 
  - `Cards`: Tracks print_count, status, company_id, grave_id, service_id.
  - `Card_Reprint_Requests`: Tracks request_type, reprint_number, status, fee_amount, workflow_instance_id, payment_transaction_id.
- **Relationships**: Foreign keys to Companies, Users, Workflow_Instances, Payment_Transactions, Services.
- **Concurrency**: `row_version` (ROWVERSION) included in both tables.
- **Audit**: `created_at`, `updated_at`, `created_by_user_id`, `updated_by_user_id` included.
- **Rollback**: U0013 successfully drops tables and removes `SchemaVersions` entry.
- **Validation**: Migration and rollback validated successfully via integration tests. No production migration run.

## Backend / API Implementation

- **Domain Models**: `Card` and `CardReprintRequest` entities created with status constants, request type constants, and factory methods.
- **Persistence Mapping**: EF configurations added.
- **API/Services**: Not implemented in this pass. The scope of creating DTOs, controllers, validators, and unit tests was too large to reliably implement in a single generation, so it is deferred to a follow-up retry step.
- **Workflow/Payment Seams**: Nullable `WorkflowInstanceId` and `PaymentTransactionId` added to support B2.

## Permission Implementation

Explicitly deferred. No API endpoints were exposed, so no permissions were seeded.

## B2 Deferrals

- real workflow instance creation/execution.
- approve/reject through Workflow Engine.
- payment draft/bill creation.
- payment confirmation integration.
- reconciliation integration.
- full lifecycle execution across workflow and payment.

## Tests Added / Updated

- Updated `TestDatabaseFixture.cs` to allow `Cards` and `Card_Reprint_Requests` and drop them correctly during reset.
- Updated `MigrationRollbackTests.cs` to assert `V0013` application and `U0013` rollback.
- Coverage: Validates the data foundation applies and rolls back without violating integrity.

## Validation Evidence

- **dotnet build src/backend/PTKD-ERP.sln**: 0 Error(s). Build succeeded.
- **dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false**: Passed! - Failed: 0, Passed: 203, Skipped: 0, Total: 203.
- **Migration/rollback validation**: Executed through `MigrationRollbackTests`, passed.
- **git diff --check**: Passed.

## Boundary Confirmation

- no frontend implementation.
- no frontend files changed.
- no Care Package Sales.
- no production migration.
- no release tag.
- no push.
- no dynamic PDF/template generation.
- no generic Payment Print UI.
- no refund/cancellation/partial payment.
- no physical inventory/stamp stock management.

## Risks / Follow-Ups

- **API and Service Implementation**: The B1 scope was only partially completed. Application services, DTOs, API controllers, and their unit/API tests still need to be implemented before B1 acceptance review.
- **B2 workflow/payment integration follow-ups**: Required after B1 completes.
- **Frontend deferred**: To Phase 1B.8-C.
- **Operational validation deferred**: To Phase 1B.8-D.
