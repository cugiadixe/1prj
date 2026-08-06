# Phase 1B.6-B Service Module Foundation Backend/Data Implementation Acceptance Review

## Status

PASSED — READY FOR PROJECT OWNER BACKEND/DATA IMPLEMENTATION ACCEPTANCE

## Reviewed Commit

- Implementation commit: `4c49ab001713663cb218d20ea439e0075736fe14`
- Parent PO backend/data scope acceptance commit: `a2f9e2a4d30b2c65cc74e21a85fa05aea6539523`

## Committed Files Review

32 files changed (25 added, 7 modified).

Files from `git diff-tree --no-commit-id --name-status -r HEAD`:

| Status | File |
|--------|------|
| A | database/migrations/V0011__service_module_foundation.sql |
| A | database/rollbacks/U0011__service_module_foundation.sql |
| A | docs/architecture/phase-1b6b-backend-data-foundation-implementation-report.md |
| A | src/backend/PTKD.Api/Controllers/ServiceController.cs |
| A | src/backend/PTKD.Api/Controllers/ServiceTypeController.cs |
| M | src/backend/PTKD.Api/Program.cs |
| M | src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs |
| A | src/backend/PTKD.Application/ServiceManagement/DTOs/ServiceDtos.cs |
| A | src/backend/PTKD.Application/ServiceManagement/Handlers/ServicePriceOverrideExecutionHandler.cs |
| A | src/backend/PTKD.Application/ServiceManagement/Services/IServiceService.cs |
| A | src/backend/PTKD.Application/ServiceManagement/Services/IServiceTypeService.cs |
| A | src/backend/PTKD.Application/ServiceManagement/Services/ServiceService.cs |
| A | src/backend/PTKD.Application/ServiceManagement/Services/ServiceTypeService.cs |
| A | src/backend/PTKD.Domain/Entities/Service.cs |
| A | src/backend/PTKD.Domain/Entities/ServiceHistory.cs |
| A | src/backend/PTKD.Domain/Entities/ServicePriceHistory.cs |
| A | src/backend/PTKD.Domain/Entities/ServiceType.cs |
| M | src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceHistoryConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/ServicePriceHistoryConfiguration.cs |
| A | src/backend/PTKD.Infrastructure/Persistence/Configurations/ServiceTypeConfiguration.cs |
| M | tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs |
| A | tests/backend/PTKD.ApiTests/ServiceApiTests.cs |
| A | tests/backend/PTKD.ApiTests/ServiceTypeApiTests.cs |
| M | tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs |
| M | tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs |
| A | tests/backend/PTKD.IntegrationTests/ServiceSchemaTests.cs |
| M | tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs |
| A | tests/backend/PTKD.UnitTests/ServiceManagement/ServicePriceOverrideExecutionHandlerTests.cs |
| A | tests/backend/PTKD.UnitTests/ServiceManagement/ServiceTests.cs |
| A | tests/backend/PTKD.UnitTests/ServiceManagement/ServiceTypeTests.cs |

Confirmed:
- All 32 files are authorized backend/data/test/migration/report files.
- No frontend files.
- No business docs.
- No scratch/decompiled/FixStrategy/script/debug files.

## Database / Migration Review

Confirmed:
- V0011 migration reviewed. Uses SET XACT_ABORT ON / BEGIN TRANSACTION / COMMIT TRANSACTION pattern.
- U0011 rollback reviewed. Drops tables in correct reverse FK dependency order (Service_History, Services, Service_Price_History, Service_Types), all with IF OBJECT_ID guards.
- 4 service foundation tables reviewed: Service_Types, Service_Price_History, Services, Service_History.
- All tables use bigint IDENTITY(1,1) PKs, explicit constraint names, datetime2(3) timestamps.
- Service_Types has ROWVERSION, UQ_Service_Types_code unique index, IX_Service_Types_is_active index.
- Services has ROWVERSION, CK_Services_status CHECK constraint for 4 valid status values, FKs to Service_Types/Customers/Companies/Users/self with no cascade.
- Service_Price_History has composite index (service_type_id, effective_from).
- Service_History has composite index (service_id, created_at), correlation_id UNIQUEIDENTIFIER.
- SERVICE_* permission seeds reviewed: 6 permissions seeded (SERVICE_VIEW COMPANY, SERVICE_TYPE_MANAGE GLOBAL, SERVICE_CREATE_STANDARD COMPANY, SERVICE_RENEW_STANDARD COMPANY, SERVICE_PRICE_OVERRIDE_REQUEST COMPANY, SERVICE_PRICE_OVERRIDE_APPROVE COMPANY).
- Business process catalog entries reviewed: SERVICE_PRICE_OVERRIDE (approval required) and RENEW_SERVICE_STANDARD (no approval), both with IF NOT EXISTS guards.
- Rollback soft-deactivates permissions (UPDATE is_active = 0) respecting TR_Permissions_PreventDelete.
- Rollback soft-deactivates business process catalog entries.
- Rollback deletes V0011 from SchemaVersions.
- DbMigrator remains owner of SchemaVersions.
- No Payment tables created.
- No Card Reprint tables created.
- No Care Package Sales tables created.
- No production migration was run.

## Backend Implementation Review

Confirmed:

**A. Domain / EF / DbContext**
- ServiceType entity: private setters, private parameterless EF ctor, public ctor with validation (code length, name length, price > 0, cycle > 0), Update/SetStandardPrice/Deactivate/Activate methods, RowVersion byte[].
- ServicePriceHistory entity: append-only, CloseEffectivePeriod method, price and reason validation.
- Service entity: status constants (ACTIVE/EXPIRED/CANCELLED/PENDING_PRICE_OVERRIDE), static factories CreateStandard/CreateRenewal, behavior methods Expire/Cancel/SetPendingPriceOverride/ApplyPriceOverride/RevertPendingOverride with state guards.
- ServiceHistory entity: append-only, action constants, CorrelationId for audit tracing.
- EF configurations follow existing conventions: IEntityTypeConfiguration<T>, snake_case columns, explicit PK/FK/index names, DeleteBehavior.Restrict, filtered index on PreviousServiceId.
- IOrganizationDbContext updated with 4 DbSet properties and Entry<TEntity> method.
- Entry<T> addition is justified: required for rowversion concurrency in service update/deactivate/renew operations. AppDbContext inherits from DbContext which already implements Entry<T>, so no new implementation required.
- AppDbContext updated with 4 expression-bodied DbSet properties.
- No unauthorized broad architectural change.

**B. Application services / handlers**
- ServiceTypeService: ListAsync (paged), GetByIdAsync, CreateAsync (with duplicate code check), UpdateAsync (rowversion concurrency), DeactivateAsync (rowversion concurrency).
- ServiceService: ListAsync (company-scoped with optional filters), GetByIdAsync, CreateStandardAsync (validates ServiceType active, Customer exists, Company exists, CustomerCompanyContext exists), RenewStandardAsync (validates status, expires previous, increments cycle), RequestPriceOverrideAsync (validates ACTIVE status, price differs, calls IWorkflowRuntimeService.CreateInstanceAsync).
- ServicePriceOverrideExecutionHandler: ProcessCode "SERVICE_PRICE_OVERRIDE", Serializable transaction, idempotency check, payload parsing, ApplyPriceOverride call, ServiceHistory creation, SecurityAuditEventRecord with ThrowIfContainsSensitiveData, ITransactionalAuditWriter.
- Standard price snapshot logic implemented: Service stores StandardPriceSnapshot at creation/renewal time from ServiceType.StandardPrice.
- Two-phase SaveChanges: parent entity saved before child history records to resolve FK dependency on IDENTITY Id. Correct approach.
- Validation rules sanitized: InvalidOperationException for business rule violations, ArgumentException for input validation.
- Concurrency/rowversion: Entry<T>.Property.OriginalValue set from base64-encoded RowVersion in requests.

**C. API v2**
- ServiceTypeController at api/v2/service-types: GET /, GET /{id}, POST /, PUT /{id}, POST /{id}/deactivate. All use SERVICE_TYPE_MANAGE (GLOBAL-scoped) — correct because service types are org-wide catalog items.
- ServiceController at api/v2/services: GET / (company-scoped), GET /{id} (company-scoped post-load), POST / (SERVICE_CREATE_STANDARD with CompanyId), POST /{id}/renew (SERVICE_RENEW_STANDARD with entity CompanyId), POST /{id}/request-price-override (SERVICE_PRICE_OVERRIDE_REQUEST with entity CompanyId).
- Permission denied returns Forbid() (sanitized).
- Not found returns NotFound with generic Title/Detail (sanitized).
- Validation errors caught as InvalidOperationException/ArgumentException → BadRequest with Title/Detail (sanitized).
- Concurrency errors caught as DbUpdateConcurrencyException → Conflict with generic message (sanitized).
- No raw SQL or internal exception exposure.

**D. Boundaries**
- No frontend implementation.
- No Payment implementation.
- No billing/collection/reconciliation implementation.
- No Card Reprint implementation.
- No SELL_CARE_PACKAGE / Care Package Sales implementation.
- No business docs changed.

## API v2 Review

Implemented API areas:
- Service type endpoints: GET /api/v2/service-types, GET /api/v2/service-types/{id}, POST /api/v2/service-types, PUT /api/v2/service-types/{id}, POST /api/v2/service-types/{id}/deactivate.
- Service endpoints: GET /api/v2/services?companyId={id}, GET /api/v2/services/{id}, POST /api/v2/services, POST /api/v2/services/{id}/renew, POST /api/v2/services/{id}/request-price-override.

Confirmed:
- API v2 route conventions followed.
- [ApiController] and [Authorize] attributes present.
- Permissions enforced per-endpoint via IPermissionEvaluator.EvaluateAsync.
- Company scope handling correct: ServiceController passes companyId from request or loaded entity to evaluator.
- ServiceType GLOBAL permission handling correct: SERVICE_TYPE_MANAGE with null companyId.
- Sanitized errors: no stack traces, no internal exception messages in responses.
- No scope creep beyond accepted service foundation.

## Permission Review

SERVICE_* permission codes added in V0011:

| Permission Code | DataScope | IsSensitive |
|---|---|---|
| SERVICE_VIEW | COMPANY | No |
| SERVICE_TYPE_MANAGE | GLOBAL | Yes |
| SERVICE_CREATE_STANDARD | COMPANY | No |
| SERVICE_RENEW_STANDARD | COMPANY | No |
| SERVICE_PRICE_OVERRIDE_REQUEST | COMPANY | Yes |
| SERVICE_PRICE_OVERRIDE_APPROVE | COMPANY | Yes |

Confirmed:
- Implementation report documents that none of these 6 permissions existed before V0011. Grep verification was performed during implementation. This is a deviation from the plan (which stated 4 were "already seeded") — correctly identified and resolved.
- All 6 SERVICE_* permissions seeded in V0011.
- SecuritySchemaTests updated with all 6 codes in ExpectedPermissionCodes (alphabetical order verified).
- Rollback behavior is safe: UPDATE is_active = 0 (soft-deactivation), not DELETE, respecting TR_Permissions_PreventDelete.

## Test and Validation Review

Per implementation report:
- dotnet build src/backend/PTKD-ERP.sln: passed (0 warnings, 0 errors).
- UnitTests: 185 passed.
- IntegrationTests: 203 passed.
- ApiTests: 281 passed.
- Total backend tests: 669 passed.
- git diff --check: clean.

Test changes reviewed:
- Unit tests: ServiceTypeTests (10 tests — constructor validation, SetStandardPrice, Deactivate/Activate, Update), ServiceTests (12 tests — CreateStandard/CreateRenewal factories, Expire, Cancel, SetPendingPriceOverride, ApplyPriceOverride, RevertPendingOverride, state transition guards), ServicePriceOverrideExecutionHandlerTests (1 test — ProcessCode).
- Integration tests: ServiceSchemaTests (7 tests — 4 table existence, permissions seeded, catalog seeded, U0011 rollback).
- API tests: ServiceTypeApiTests (8 tests — 401, 403, CRUD, duplicate code, not found, update, deactivate), ServiceApiTests (6 tests — 401, 403, create, invalid customer, not found, renew).
- SecuritySchemaTests: ExpectedPermissionCodes updated with 6 SERVICE_* codes.
- TestDatabaseFixture: KnownTables updated, DropKnownSchema updated, ResetToV0011 added.
- SafeTestWebApplicationFactory: reset target updated to ResetToV0011.
- MigrationRollbackTests: V0011/U0011 assertions added.

No evidence of regression — all 669 tests passing includes pre-existing test suites.

## Boundary Review

Confirmed:
- No frontend implementation.
- No Payment implementation.
- No billing/collection/reconciliation implementation.
- No Card Reprint implementation.
- No Care Package Sales implementation.
- No business docs changed.
- No production migration.
- No release tag.
- No push.

## Risks / Follow-Ups

- Frontend remains future Phase 1B.6-C.
- Payment remains deferred.
- Card Reprint remains deferred.
- Care Package Sales remains deferred.
- Production release remains deferred.
- Untracked scratch/decompiled/FixStrategy files remain in working tree and must not be staged.
- OD-1B6-004 (Standard Price Scope — per-company pricing) carried forward as deferred with safe default (global per ServiceType).
- OD-1B6-010 (Frontend Screen Scope) carried forward as deferred to Phase 1B.6-C.
- Implementation deviation: ServiceTypeController uses SERVICE_TYPE_MANAGE instead of SERVICE_VIEW for list/get — documented and justified (SERVICE_VIEW is COMPANY-scoped, incompatible with global catalog access pattern). Future Phase 1B.6-C may introduce a separate read-only permission if needed.
- Implementation deviation: Entry<T> added to IOrganizationDbContext interface — justified for rowversion concurrency, implemented via DbContext inheritance with no new code.

## Review Decision

PASSED — PHASE 1B.6-B SERVICE MODULE FOUNDATION BACKEND/DATA IMPLEMENTATION MAY PROCEED TO PROJECT OWNER ACCEPTANCE
