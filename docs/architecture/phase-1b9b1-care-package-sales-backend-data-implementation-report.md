# Phase 1B.9-B1 Care Package Sales Backend/Data Implementation Report

## Status

IMPLEMENTED — READY FOR BACKEND/DATA ACCEPTANCE REVIEW

## Authorization Source

- Phase 1B.9-B Project Owner implementation plan acceptance commit:
  e3d8beddd656c4ce2d2846f91e6a3531083b202e

## Implemented Scope

Phase 1B.9-B1 implements the core backend and data foundation for Care Package Sales. This includes database schemas for requests and request items, entity models, EF Core configurations with snake_case mappings, API controllers (`CarePackageRequestsController`), application services, and role-based permissions (`CARE_PACKAGE_VIEW`, `CARE_PACKAGE_CREATE`). It does not include workflow integration, payment draft creation, or frontend components.

## Implemented Files

- database/migrations/V0014__care_package_sales_foundation.sql
- database/rollbacks/U0014__care_package_sales_foundation.sql
- src/backend/PTKD.Domain/Entities/CarePackageRequest.cs
- src/backend/PTKD.Domain/Entities/CarePackageRequestItem.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/CarePackageRequestConfiguration.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/CarePackageRequestItemConfiguration.cs
- src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs
- src/backend/PTKD.Infrastructure/DependencyInjection.cs
- src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs
- src/backend/PTKD.Application/CarePackages/DTOs/CarePackageRequestDto.cs
- src/backend/PTKD.Application/CarePackages/DTOs/CarePackageRequestItemDto.cs
- src/backend/PTKD.Application/CarePackages/DTOs/CreateCarePackageRequest.cs
- src/backend/PTKD.Application/CarePackages/DTOs/CreateCarePackageRequestItem.cs
- src/backend/PTKD.Application/CarePackages/Services/ICarePackageRequestService.cs
- src/backend/PTKD.Application/CarePackages/Services/CarePackageRequestService.cs
- src/backend/PTKD.Api/Controllers/CarePackageRequestsController.cs
- src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs
- src/backend/PTKD.Api/Program.cs
- tests/backend/PTKD.UnitTests/Domain/Entities/CarePackageRequestTests.cs
- tests/backend/PTKD.ApiTests/CarePackageRequestApiTests.cs
- tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs
- tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs
- tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs
- tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs
- docs/architecture/phase-1b9b1-care-package-sales-backend-data-implementation-report.md

## Migration / Rollback

- V0014__care_package_sales_foundation.sql
- U0014__care_package_sales_foundation.sql

Schema includes `Care_Package_Requests` and `Care_Package_Request_Items` tables with company scope, customer tracking, pricing snapshots, discount tracking, and constraints (including row versioning).

## Domain Model Summary

- `CarePackageRequest`: Tracks the root request (company, customer, service, status, pricing, discounts, audit, row version).
- `CarePackageRequestItem`: Tracks individual graves in the request, snapshotting the cot count, unit price, and service periods.
- Invariants: Subtotal and Total calculations are maintained as decimals. Items are tied closely to the root request. Pricing is snapshotted to maintain historical consistency.

## EF / Persistence Summary

- `CarePackageRequestConfiguration` and `CarePackageRequestItemConfiguration` define explicit snake_case column mappings for SQL tables.
- DbContext integrates `CarePackageRequests` and `CarePackageRequestItems` DB sets.
- Row versions are configured for optimistic concurrency.
- `TestDatabaseFixture` is updated to drop constraints (`FK_CPR_created_by_user_id` and `FK_CPR_updated_by_user_id`) to avoid deadlocks on tear down, resetting effectively to `V0014`.

## Application / API Summary

- Data Transfer Objects (DTOs) for incoming creations and outgoing reads.
- `ICarePackageRequestService` foundational service handling company scoped request creation, listing, and retrieving.
- `CarePackageRequestsController` with `List`, `GetById`, and `Create` endpoints.
- Authorization integrated using `RequirePermission` and `PermissionCodes.CarePackageCreate`/`CarePackageView`.
- Validations block mismatching service constraints.

## Pricing Snapshot Summary

- Backend foundation is built for pricing snapshots (`unit_price_snapshot`, `line_subtotal`, `subtotal_amount`, `discount_amount`, `total_amount`).
- No hard-coded prices; fields support real calculated price fetching when integrating fully with the Service domain.

## Tests Added / Updated

- tests/backend/PTKD.ApiTests/CarePackageRequestApiTests.cs
- tests/backend/PTKD.UnitTests/Domain/Entities/CarePackageRequestTests.cs
- tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs
- tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs
- tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs
- tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs

## Validation Evidence

Validation completed successfully on August 4, 2026.
- `dotnet build src/backend/PTKD-ERP.sln`: 0 errors, 9 warnings (obsolete attributes in unrelated files).
- `dotnet test tests/backend/PTKD.UnitTests/`: Passed: 235/235
- `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false`: Passed: 203/203
- `dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false`: Passed: 308/308
- `git diff --check`: Clean (no whitespace errors).

## Boundary Confirmation

- no frontend implementation.
- no full workflow/payment integration.
- no approve/reject workflow facades.
- no full payment draft/bill creation.
- no production migration.
- no release tag.
- no push.
- no dynamic PDF/template generation.
- no generic Payment Print UI.
- no refund.
- no cancellation.
- no partial payment.
- no physical inventory/stamp stock management.
- no multi-year packages.
- no partial-year packages.
- no discount percent UI.
- no dedicated report/export UI.
- no business docs changed.
- no permission catalog changed.
- implementation_plan.md not committed.
- task.md not committed.
- frontend debug/test output not committed.
- scratch/decompiled/FixStrategy/script/debug files not committed.

## Known Risks / Follow-Ups

- B2 workflow/payment integration deferred.
- frontend deferred to C.
- operational validation deferred to D.
- any overlap prevention limitations if not fully implemented in B1.
- pricing snapshot calculations may need further tuning alongside Service lookup refinements in B2.

## Recommended Next Gate

Phase 1B.9-B1 backend/data acceptance review.
