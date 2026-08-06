# Phase 1B.5-B Backend/Data Foundation Implementation Report

## Status

IMPLEMENTED — READY FOR ACCEPTANCE REVIEW

## Authorization Source

Reference:
- Phase 1B.5-B PO backend/data scope acceptance commit:
  8cdf94053ccf390811b38887950507f0db7fad06

## Implemented Scope

- V0010 migration for Customer Merge Request backend storage.
- U0010 rollback.
- Customer merge persistence.
- Customer merge domain entities and application service.
- Customer merge execution handler for approved workflow execution boundary.
- Customer merge API v2 backend controllers/endpoints (`CustomerMergeController`).
- Permission enforcement for CUSTOMER_MERGE_READ, CUSTOMER_MERGE_CREATE, CUSTOMER_MERGE_MANAGE.
- Unit tests, Integration tests, and ApiTests coverage.
- MigrationRollbackTests coverage for V0010/U0010.

## Database Changes

- **V0010 migration**: Creates `Customer_Merge_Requests`, `Customer_Merge_Request_Candidates`, `Customer_Merge_History` tables with related status/linkage fields.
- **Permission seed changes**: Inserts new codes in `Permissions` table for customer merge actions.
- **SchemaVersions**: Handled by DbMigrator.

## Rollback Notes

- U0010 does not hard-delete Permissions because `TR_Permissions_PreventDelete` blocks hard delete.
- U0010 uses soft-deactivate for new `CUSTOMER_MERGE_*` permissions.
- U0010 removes the V0010 SchemaVersions record as required by MigrationRollbackTests.

## Backend/API Changes

- **Domain entities**: `CustomerMergeRequest`, `CustomerMergeRequestCandidate`, `CustomerMergeHistory`.
- **EF configurations**: Mappings in `PTKD.Infrastructure.Persistence.Configurations`.
- **Application services**: `CustomerMergeService` and interface `ICustomerMergeService`.
- **DTOs**: Customer merge data transfer objects in `PTKD.Application.Customers.DTOs.CustomerMergeDtos`.
- **Execution handler**: `CustomerMergeExecutionHandler`.
- **API v2 controllers/endpoints**: `CustomerMergeController` under `/api/v2/customers`.
- **DI pattern**: Using `IOrganizationDbContextFactory` for context resolution.

## Test Fixes and Fixture Updates

- `TestDatabaseFixture` KnownTables update for new `Customer_Merge_*` tables.
- DropKnownSchema dependency order update.
- `SecuritySchemaTests` expected permission codes update.
- `CustomerMergeServiceTests` factory mock update.

## Validation Evidence

- **dotnet build result**: Build succeeded. 0 Error(s), 0 Warning(s).
- **UnitTests result**: Passed! - Failed: 0, Passed: 158, Skipped: 0, Total: 158.
- **IntegrationTests result**: Passed! - Failed: 0, Passed: 196, Skipped: 0, Total: 196.
- **ApiTests result**: Passed! - Failed: 0, Passed: 267, Skipped: 0, Total: 267.
- **git diff --check result**: Clean (no trailing whitespace/conflict markers).

## Security and Boundaries

- Backend authorization is authoritative via policy evaluation.
- No raw SQL/internal exception exposure.
- No stack traces; sanitized errors via standard Problem Details.
- No frontend changes included in this scope.
- No business docs changed.
- No production migration executed.
- No release tag created.
- No push executed.

## Remediation After Initial Blocked Commit

- Previous commit dc6ebf6ea85b98d9ade2609c4e237fbb03d11916 was PARTIAL / BLOCKED.
- ApiTests initially failed 267/267 due to SQL deadlock in TestDatabaseFixture.
- Root cause: `SafeTestWebApplicationFactory` called `ResetToV0009()` but the application EF context now expects V0010 tables (Customer_Merge_*). The factory dropped all tables then only rebuilt to V0009, leaving the Customer_Merge_* tables missing. When the schema was already at V0010 from a prior integration test run, API tests passed by luck; when run independently or concurrently, the missing tables caused deadlocks and failures during EF model validation.
- Fix applied:
  - Added `ResetToV0010()` method to `TestDatabaseFixture.cs` following the established pattern.
  - Updated `SafeTestWebApplicationFactory.cs` to call `ResetToV0010()` instead of `ResetToV0009()`.
- Files changed in remediation:
  - `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs` — added `ResetToV0010()`.
  - `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs` — updated schema init to V0010.
  - `docs/architecture/phase-1b5b-backend-data-foundation-implementation-report.md` — status updated.

## Risks / Follow-Ups

- Frontend remains future work and is not authorized in this commit.
- Future service/payment/document linked-module handling remains deferred.
- Untracked scratch/decompiled/FixStrategy files remain uncommitted.
