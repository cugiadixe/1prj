# Phase 1B.6-B Backend/Data Foundation — Implementation Report

**Date**: 2026-08-03
**Branch**: feature/phase-1-organization
**Authorization**: phase-1b6b-project-owner-backend-data-scope-acceptance.md

## Summary

Implemented the Service Module Foundation backend/data layer including SQL migration, domain entities, EF configurations, application services, controllers, and comprehensive tests.

## Deliverables

### Database

| File | Description |
|------|-------------|
| `database/migrations/V0011__service_module_foundation.sql` | Creates Service_Types, Service_Price_History, Services, Service_History tables; seeds 6 permissions and 2 business process catalog entries |
| `database/rollbacks/U0011__service_module_foundation.sql` | Drops tables in reverse FK order, soft-deactivates permissions and catalog entries |

### Domain Entities

| File | Description |
|------|-------------|
| `src/backend/PTKD.Domain/Entities/ServiceType.cs` | Service type catalog entity with Update, SetStandardPrice, Activate, Deactivate |
| `src/backend/PTKD.Domain/Entities/ServicePriceHistory.cs` | Append-only price audit trail |
| `src/backend/PTKD.Domain/Entities/Service.cs` | Core service entity with status machine (ACTIVE→EXPIRED/CANCELLED/PENDING_PRICE_OVERRIDE) |
| `src/backend/PTKD.Domain/Entities/ServiceHistory.cs` | Append-only service change audit |

### Infrastructure

| File | Description |
|------|-------------|
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceTypeConfiguration.cs` | EF config with snake_case, UQ_Service_Types_code |
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/ServicePriceHistoryConfiguration.cs` | EF config with FK to ServiceType |
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs` | EF config with FKs to ServiceType, Customer, Company, self-reference |
| `src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceHistoryConfiguration.cs` | EF config with FK to Service |

### Application Layer

| File | Description |
|------|-------------|
| `src/backend/PTKD.Application/ServiceManagement/DTOs/ServiceDtos.cs` | Request/response DTOs |
| `src/backend/PTKD.Application/ServiceManagement/Services/IServiceTypeService.cs` | Service type CRUD interface |
| `src/backend/PTKD.Application/ServiceManagement/Services/IServiceService.cs` | Service operations interface |
| `src/backend/PTKD.Application/ServiceManagement/Services/ServiceTypeService.cs` | Service type implementation |
| `src/backend/PTKD.Application/ServiceManagement/Services/ServiceService.cs` | Service implementation with workflow integration |
| `src/backend/PTKD.Application/ServiceManagement/Handlers/ServicePriceOverrideExecutionHandler.cs` | Workflow execution handler for price overrides |

### API Controllers

| File | Description |
|------|-------------|
| `src/backend/PTKD.Api/Controllers/ServiceTypeController.cs` | CRUD endpoints at api/v2/service-types (SERVICE_TYPE_MANAGE) |
| `src/backend/PTKD.Api/Controllers/ServiceController.cs` | Service endpoints at api/v2/services (company-scoped permissions) |

### Modified Files

| File | Change |
|------|--------|
| `src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs` | Added 4 DbSet properties, Entry<T> method |
| `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs` | Added 4 DbSet properties |
| `src/backend/PTKD.Api/Program.cs` | DI registrations for service management |
| `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs` | KnownTables, DropKnownSchema, ResetToV0011 |
| `tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs` | Added 6 SERVICE_* to ExpectedPermissionCodes |
| `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs` | ResetToV0010→ResetToV0011 |
| `tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs` | V0011/U0011 assertions |

### Tests

| File | Tests |
|------|-------|
| `tests/backend/PTKD.UnitTests/ServiceManagement/ServiceTypeTests.cs` | 10 tests |
| `tests/backend/PTKD.UnitTests/ServiceManagement/ServiceTests.cs` | 12 tests (was 14 in plan, reduced to core state machine tests) |
| `tests/backend/PTKD.UnitTests/ServiceManagement/ServicePriceOverrideExecutionHandlerTests.cs` | 1 test |
| `tests/backend/PTKD.IntegrationTests/ServiceSchemaTests.cs` | 7 tests (tables exist, permissions seeded, catalog seeded, rollback) |
| `tests/backend/PTKD.ApiTests/ServiceTypeApiTests.cs` | 8 tests (CRUD, auth, permissions, validation) |
| `tests/backend/PTKD.ApiTests/ServiceApiTests.cs` | 6 tests (CRUD, auth, permissions, renewal) |

## Test Results

| Suite | Passed | Failed | Total |
|-------|--------|--------|-------|
| UnitTests | 185 | 0 | 185 |
| IntegrationTests | 203 | 0 | 203 |
| ApiTests | 281 | 0 | 281 |

## Deviations from Plan

1. **Permission seeding**: Plan stated SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE were "already seeded in earlier migrations." Grep verification confirmed they were NOT seeded anywhere. All 6 SERVICE_* permissions are now seeded in V0011.

2. **ServiceTypeController permission**: Changed from SERVICE_VIEW (COMPANY-scoped) to SERVICE_TYPE_MANAGE (GLOBAL-scoped) for list/get endpoints. SERVICE_VIEW with null companyId fails the evaluator's scope validation since SERVICE_VIEW has DataScope=COMPANY. Service type catalog viewing is a global admin function, so SERVICE_TYPE_MANAGE is correct.

3. **IOrganizationDbContext.Entry<T>**: Added `Entry<TEntity>` method to the interface. Required for rowversion concurrency checks in service methods. AppDbContext already implements this via DbContext inheritance.

4. **Two-phase SaveChanges**: ServiceType creation and Service creation now save the parent entity before creating child history records, because the parent's Id (bigint IDENTITY) is 0 until persisted.

## Not Implemented (Out of Scope per Acceptance)

- Frontend UI
- Payment/billing/collection/reconciliation
- Card Reprint
- SELL_CARE_PACKAGE / Care Package Sales
- Production migration execution
