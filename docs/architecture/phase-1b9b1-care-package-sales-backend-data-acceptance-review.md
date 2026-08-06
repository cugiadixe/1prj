# Phase 1B.9-B1 Care Package Sales Backend/Data Acceptance Review

## Status

PASSED WITH NOTES — READY FOR PROJECT OWNER BACKEND/DATA ACCEPTANCE

## Review Target

- Phase 1B.9-B1 implementation commit:
  c28e7d5b65ac902f80a51c92121352e5ec1fc70c

- Phase 1B.9-B Project Owner implementation plan acceptance commit:
  e3d8beddd656c4ce2d2846f91e6a3531083b202e

## Authorization Review

The implementation stayed within the B1 authorization. It focused strictly on the backend/data foundation. No frontend was implemented, no full workflow/payment integration was done, no approve/reject facades or payment draft/bill creation were implemented.

## Committed File Review

Committed files:
A	database/migrations/V0014__care_package_sales_foundation.sql
A	database/rollbacks/U0014__care_package_sales_foundation.sql
A	docs/architecture/phase-1b9b1-care-package-sales-backend-data-implementation-report.md
A	src/backend/PTKD.Api/Controllers/CarePackageRequestsController.cs
M	src/backend/PTKD.Api/Program.cs
M	src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs
A	src/backend/PTKD.Application/CarePackages/DTOs/CarePackageRequestDto.cs
A	src/backend/PTKD.Application/CarePackages/DTOs/CreateCarePackageRequest.cs
A	src/backend/PTKD.Application/CarePackages/Services/CarePackageRequestService.cs
M	src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs
A	src/backend/PTKD.Domain/Entities/CarePackageRequest.cs
A	src/backend/PTKD.Domain/Entities/CarePackageRequestItem.cs
M	src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs
A	src/backend/PTKD.Infrastructure/Persistence/Configurations/CarePackageRequestConfiguration.cs
A	src/backend/PTKD.Infrastructure/Persistence/Configurations/CarePackageRequestItemConfiguration.cs
A	tests/backend/PTKD.ApiTests/CarePackageRequestApiTests.cs
M	tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs
M	tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs
M	tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs
M	tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs
A	tests/backend/PTKD.UnitTests/Domain/Entities/CarePackageRequestTests.cs

- The required report path was committed.
- The wrong report path was not committed.
- No frontend files were included.
- No business docs were modified.
- No permission catalog changes were made.

## Migration / Rollback Review

The V0014 migration adds `Care_Package_Requests` and `Care_Package_Request_Items` and sets up the correct relationships, optimistic concurrency, and company scopes. The U0014 provides a safe rollback script in reverse dependency order.

**Note on Authorization**: The decision to seed `CARE_PACKAGE_VIEW` and `CARE_PACKAGE_CREATE` permissions inside V0014 is consistent with B1 backend authorization needs. ACCEPTED WITH NOTE.

## Domain / EF / Persistence Review

Domain entities `CarePackageRequest` and `CarePackageRequestItem` correctly model the data invariants with snake_case EF configuration and snapshot fields. Invariants, relationships, and constraints are strictly modeled without violating scope.

## Application / API Review

Application DTOs and `CarePackageRequestService` implement list, detail, and create properly scoped to the company ID. `CarePackageRequestsController` exposes `/api/v2/care-packages` using standard authorization permission checks `CARE_PACKAGE_CREATE` and `CARE_PACKAGE_VIEW`. Error handling avoids unsafe exposure.

## Pricing Snapshot Review

Pricing snapshot fields such as `unit_price_snapshot` and `line_subtotal` are present in `Care_Package_Request_Items`. No hard-coded care package price exists, ensuring that the integration relies on genuine service data snapshots.

## Authorization / Company Scope Review

Company scope is enforced within the application layer for list and read models. Permission checks on endpoints restrict access appropriately based on the user's effective permissions.

## Test Coverage Review

Tests added in ApiTests, IntegrationTests, and UnitTests properly cover migration rollback, domain entities, API error responses, valid operations, and company scoping rules.

## Acceptance Validation Evidence

Validation successfully executed.
- build: 0 errors, 9 warnings.
- unit tests: Passed 235/235.
- integration tests: Passed 203/203.
- API tests: Passed 308/308.
- git diff --check: Clean.

## Non-Blocking Notes

- `CARE_PACKAGE_VIEW` / `CARE_PACKAGE_CREATE` permission seed in V0014 is accepted.

## Blockers

No blocking issues found.

## Boundary Confirmation

- no frontend implementation.
- no full workflow/payment integration.
- no approve/reject workflow facades.
- no full payment draft/bill creation.
- no production migration.
- no release tag.
- no push.
- no business docs changed.
- no permission catalog changed.
- no refund/cancellation/partial payment.
- no dynamic PDF/template generation.
- no generic Payment Print UI.

## Recommended Next Gate

Project Owner Phase 1B.9-B1 backend/data acceptance.
