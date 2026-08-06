# Phase 1B.6-B Service Module Foundation Backend/Data Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER BACKEND/DATA SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.6 Project Owner scope acceptance commit:
  73a600687b884ce954d7b44e4eaa1f580a2ace0c

State:
- Phase 1B.6 Service Module Foundation scope is accepted.
- This document is backend/data scope and implementation planning only.
- This document does not authorize implementation.
- This document does not authorize migration/rollback creation.

## Objective

Define the backend/data implementation scope, database strategy, API v2 strategy, permission strategy, workflow integration boundary, validation approach, and blockers for Phase 1B.6-B.

## Source Documents Reviewed

- docs/architecture/phase-1b6-project-owner-scope-acceptance.md
- docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md
- docs/architecture/post-1b5-project-owner-next-work-decision.md
- docs/architecture/post-1b5-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b5-project-owner-closure-acceptance.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/business/process-catalog.md
- docs/architecture/project-readiness-review.md
- docs/architecture/phase-1b0-security-discovery-decisions.md
- database/migrations/ (V0001 through V0010)
- database/rollbacks/ (U0001 through U0010)
- src/backend/PTKD.Api/Controllers/ (12 controllers, patterns inspected)
- src/backend/PTKD.Domain/Entities/ (25 entities, patterns inspected)
- src/backend/PTKD.Infrastructure/Persistence/Configurations/ (EF configurations, patterns inspected)
- src/backend/PTKD.Application/ (services, handlers, DTOs, patterns inspected)
- tests/backend/ (TestDatabaseFixture, SafeTestWebApplicationFactory, MigrationRollbackTests, patterns inspected)

Missing sources:
- PTKD-ERP-Master-Context.md: does not exist at repository root.

## Accepted 1B.6 Scope Summary

- ServiceType catalog foundation.
- Service entity foundation.
- Service lifecycle/status model.
- Standard service price snapshot strategy.
- Service creation and renewal foundation for standard services.
- SERVICE_PRICE_OVERRIDE workflow integration planning.
- V0011/U0011 migration planning.
- API v2 planning.
- Backend/data test planning.
- Permission planning.

## Backend/Data Scope Decision Summary

### In Scope for 1B.6-B Backend/Data

1. **V0011/U0011 Migration and Rollback**: Create Service_Types, Service_Price_History, Services, Service_History tables. Seed service permissions. U0011 drops tables in dependency order and soft-deactivates permissions.

2. **Domain Entities**: ServiceType, ServicePriceHistory, Service, ServiceHistory. Private setters, behavior methods, status constants, rowversion concurrency.

3. **EF Configurations**: ServiceTypeConfiguration, ServicePriceHistoryConfiguration, ServiceConfiguration, ServiceHistoryConfiguration. Snake_case table/column names, explicit PK/FK/index names, DeleteBehavior.Restrict.

4. **DbContext Updates**: Add DbSet properties for new entities to IOrganizationDbContext and OrganizationDbContext.

5. **Application Services**: IServiceService / ServiceService for service catalog query, service creation (standard), service renewal (standard), price override request submission. IServiceTypeService / ServiceTypeService for service type catalog management.

6. **DTOs**: ServiceTypeDto, ServiceDto, CreateServiceRequest, RenewServiceRequest, RequestPriceOverrideRequest, ServiceListResponse, ServiceTypeListResponse.

7. **Execution Handler**: ServicePriceOverrideExecutionHandler implementing IWorkflowExecutionHandler with ProcessCode "SERVICE_PRICE_OVERRIDE".

8. **API v2 Controller**: ServiceController at /api/v2/services (service CRUD, renewal, price override request). ServiceTypeController at /api/v2/service-types (catalog query, admin management).

9. **Permission Seeding**: Seed SERVICE_VIEW, SERVICE_TYPE_MANAGE permissions via V0011. Existing SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE are already seeded.

10. **Tests**: Unit tests, integration tests, API tests, migration rollback tests.

11. **Test Fixture Updates**: TestDatabaseFixture.ResetToV0011(). SafeTestWebApplicationFactory updated to call ResetToV0011(). KnownTables updated with new table names.

### Out of Scope / Deferred

- Frontend implementation (deferred to Phase 1B.6-C).
- Full Payment implementation.
- Billing/collection/reconciliation implementation.
- Card Reprint implementation.
- SELL_CARE_PACKAGE implementation (RESERVED / INACTIVE).
- Production payment workflow.
- Service-to-Plot/Location linkage.
- Reporting/reconciliation.
- Production migration.
- Release tag.
- Push.

## Open Decisions Review

| Decision ID | Topic | Repository Evidence | Recommended Planning Position | Status | Blocks Implementation? | Required PO Action |
|---|---|---|---|---|---|---|
| OD-1B6-001 | Service Type Taxonomy | No specific types listed in business-rules.md or process-catalog.md. Services referenced generically. | Design ServiceType as admin-manageable catalog entity. Seed 2 example types for testing (e.g. "CHAM_SOC_THUONG_NIEN" annual care, "BAO_TRI" maintenance). Production types configured by ADMIN_SERVICE_DATA. | RESOLVED FOR 1B.6-B PLANNING | No | None — catalog-driven design eliminates taxonomy blocker |
| OD-1B6-002 | Service Lifecycle Statuses | RENEW_SERVICE_STANDARD implies active/expired cycle. SERVICE_PRICE_OVERRIDE implies pending state. | Use ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE. No DRAFT status — service creation at standard price is immediate (no approval needed). | RESOLVED FOR 1B.6-B PLANNING | No | None — statuses supported by process catalog evidence |
| OD-1B6-003 | Renewal Model | PAY-008 references "service-cycle" implying distinct cycles. | New Service row per renewal cycle, linked via PreviousServiceId. Prior service transitions to EXPIRED. Each cycle is a distinct entity for PAY-008 payment-item reference. | RESOLVED FOR 1B.6-B PLANNING | No | None — new-row model best supports PAY-008 |
| OD-1B6-004 | Standard Price Scope | SERVICE_PRICE_OVERRIDE condition fields include company_id and standard_price. | Standard price on ServiceType entity (global per type). Phase 1B.6 implements global-per-type only. If per-company pricing is needed later, a ServiceTypeCompanyPrice table can be added without schema-breaking changes. | DEFERRED WITH SAFE DEFAULT | No | None — global-per-type is the safe default |
| OD-1B6-005 | Whether Service Sale Belongs in 1B.6 | PO decision references "Service Catalog, Standard Pricing, Service Sales." | Include service creation (standard) and renewal (standard) in Phase 1B.6-B backend. SERVICE_PRICE_OVERRIDE workflow integration also in scope. | RESOLVED FOR 1B.6-B PLANNING | No | None — PO decision explicitly includes service sales |
| OD-1B6-006 | SERVICE_VIEW Permission | permission-catalog.md has no SERVICE_VIEW. CUSTOMER module has CUSTOMER_VIEW_BASIC as separate read permission. | Add SERVICE_VIEW (SERVICE, VIEW, COMPANY) for consistency. Seed in V0011. Allows read-only access for users who should see services but not create them. | RESOLVED FOR 1B.6-B PLANNING | No | None — follows established CUSTOMER_VIEW_BASIC pattern |
| OD-1B6-007 | Service-to-Customer Linkage | DATA-002 defines Customer_Company_Context as (customer_id, company_id). DATA-003 says services are COMPANY-scoped. | Service references CustomerId + CompanyId (two FKs to Customers and Companies). Application validation ensures Customer_Company_Context exists before creation. | RESOLVED FOR 1B.6-B PLANNING | No | None — matches existing FK pattern |
| OD-1B6-008 | Service Type Admin Endpoints | ADMIN_SERVICE_DATA admin group exists. GOV-002 does not restrict catalog data management. | Include ServiceType admin endpoints (create, update, deactivate) gated by SERVICE_TYPE_MANAGE. Enables operational flexibility. | RESOLVED FOR 1B.6-B PLANNING | No | None — admin group exists, catalog management is standard |
| OD-1B6-009 | Migration Scope | V0010 created 3 tables in single migration. | Single V0011 for all service tables + permissions. Single U0011 for rollback. | RESOLVED FOR 1B.6-B PLANNING | No | None — follows V0010 precedent |
| OD-1B6-010 | Frontend Screen Scope | Not applicable to 1B.6-B backend scope. | Deferred to Phase 1B.6-C frontend planning. | DEFERRED | No | Deferred to 1B.6-C |

All 10 open decisions are resolved or safely deferred. No blockers before 1B.6-B implementation.

## Proposed Database Model

### Service_Types Table

- **Purpose**: Service type catalog with standard pricing.
- **Columns**:
  - id (bigint IDENTITY(1,1) PK)
  - code (nvarchar(50) NOT NULL, UNIQUE)
  - name (nvarchar(200) NOT NULL)
  - description (nvarchar(500) NULL)
  - standard_price (decimal(18,2) NOT NULL)
  - standard_price_currency (nvarchar(3) NOT NULL DEFAULT 'VND')
  - cycle_duration_months (int NULL — NULL for one-time services)
  - is_active (bit NOT NULL DEFAULT 1)
  - created_at (datetime2(3) NOT NULL)
  - updated_at (datetime2(3) NULL)
  - created_by_user_id (bigint NOT NULL FK → Users)
  - row_version (rowversion NOT NULL)
- **Indexes**:
  - PK_Service_Types on id
  - UQ_Service_Types_code on code
  - IX_Service_Types_is_active on is_active
- **FKs**:
  - FK_Service_Types_created_by_user_id → Users(id), RESTRICT
- **rowversion**: Yes — catalog updates require concurrency protection.
- **Rollback**: DROP TABLE.

### Service_Price_History Table

- **Purpose**: Audit trail of standard price changes per service type.
- **Columns**:
  - id (bigint IDENTITY(1,1) PK)
  - service_type_id (bigint NOT NULL FK → Service_Types)
  - price (decimal(18,2) NOT NULL)
  - effective_from (datetime2(3) NOT NULL)
  - effective_to (datetime2(3) NULL — NULL = current)
  - changed_by_user_id (bigint NOT NULL FK → Users)
  - change_reason (nvarchar(500) NOT NULL)
  - created_at (datetime2(3) NOT NULL)
- **Indexes**:
  - PK_Service_Price_History on id
  - IX_SPH_service_type_id on service_type_id
  - IX_SPH_service_type_effective on (service_type_id, effective_from)
- **FKs**:
  - FK_SPH_service_type_id → Service_Types(id), RESTRICT
  - FK_SPH_changed_by_user_id → Users(id), RESTRICT
- **rowversion**: No — append-only audit table.
- **Rollback**: DROP TABLE before Service_Types.

### Services Table

- **Purpose**: Core service instance record linking customer+company to a service type.
- **Columns**:
  - id (bigint IDENTITY(1,1) PK)
  - service_type_id (bigint NOT NULL FK → Service_Types)
  - customer_id (bigint NOT NULL FK → Customers)
  - company_id (bigint NOT NULL FK → Companies)
  - status (nvarchar(30) NOT NULL — ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE)
  - applied_price (decimal(18,2) NOT NULL)
  - standard_price_snapshot (decimal(18,2) NOT NULL)
  - is_override_price (bit NOT NULL DEFAULT 0)
  - override_approval_request_id (bigint NULL — FK → Workflow_Instances if override was approved)
  - valid_from (datetime2(3) NOT NULL)
  - valid_to (datetime2(3) NULL — NULL if perpetual)
  - cycle_number (int NOT NULL DEFAULT 1)
  - previous_service_id (bigint NULL FK → Services — renewal chain)
  - created_by_user_id (bigint NOT NULL FK → Users)
  - created_at (datetime2(3) NOT NULL)
  - updated_at (datetime2(3) NULL)
  - row_version (rowversion NOT NULL)
- **Indexes**:
  - PK_Services on id
  - IX_Services_customer_company on (customer_id, company_id)
  - IX_Services_company_status on (company_id, status)
  - IX_Services_service_type on service_type_id
  - IX_Services_previous on previous_service_id WHERE previous_service_id IS NOT NULL
- **FKs**:
  - FK_Services_service_type_id → Service_Types(id), RESTRICT
  - FK_Services_customer_id → Customers(id), RESTRICT
  - FK_Services_company_id → Companies(id), RESTRICT
  - FK_Services_previous_service_id → Services(id), RESTRICT
  - FK_Services_created_by_user_id → Users(id), RESTRICT
- **rowversion**: Yes — concurrency on lifecycle changes.
- **Application validation**: Customer_Company_Context must exist for (customer_id, company_id) before service creation.
- **Rollback**: DROP TABLE before Service_Price_History and Service_Types.

### Service_History Table

- **Purpose**: Audit trail for service lifecycle events.
- **Columns**:
  - id (bigint IDENTITY(1,1) PK)
  - service_id (bigint NOT NULL FK → Services)
  - action_code (nvarchar(30) NOT NULL — CREATED, RENEWED, PRICE_OVERRIDDEN, CANCELLED, EXPIRED)
  - before_data (nvarchar(max) NULL — JSON snapshot)
  - after_data (nvarchar(max) NULL — JSON snapshot)
  - acted_by_user_id (bigint NOT NULL FK → Users)
  - reason (nvarchar(500) NULL)
  - correlation_id (uniqueidentifier NOT NULL)
  - created_at (datetime2(3) NOT NULL)
- **Indexes**:
  - PK_Service_History on id
  - IX_SH_service_id on service_id
  - IX_SH_service_created on (service_id, created_at)
- **FKs**:
  - FK_SH_service_id → Services(id), RESTRICT
  - FK_SH_acted_by_user_id → Users(id), RESTRICT
- **rowversion**: No — append-only audit table.
- **Rollback**: DROP TABLE first (depends on Services).

### Permission Seeds

V0011 will seed the following permissions into the Permissions table:

| permission_code | module_code | action_code | data_scope | is_sensitive | is_delegable | is_active |
|---|---|---|---|---|---|---|
| SERVICE_VIEW | SERVICE | VIEW | COMPANY | 0 | 0 | 1 |
| SERVICE_TYPE_MANAGE | SERVICE | MANAGE_CATALOG | GLOBAL | 1 | 0 | 1 |

The following permissions already exist (seeded in earlier migrations): SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE.

U0011 rollback will UPDATE IsActive = 0 for SERVICE_VIEW and SERVICE_TYPE_MANAGE (soft-deactivation per TR_Permissions_PreventDelete pattern).

### Business Process Catalog Seed

V0011 will seed the SERVICE_PRICE_OVERRIDE and RENEW_SERVICE_STANDARD process codes into Business_Process_Catalog if not already present.

U0011 will UPDATE is_active = 0 for these process codes (soft-deactivation).

## Migration / Rollback Strategy

- **Forward migration**: V0011__create_service_schema.sql
  1. CREATE TABLE Service_Types.
  2. CREATE TABLE Service_Price_History (FK → Service_Types).
  3. CREATE TABLE Services (FK → Service_Types, Customers, Companies).
  4. CREATE TABLE Service_History (FK → Services).
  5. INSERT permissions (SERVICE_VIEW, SERVICE_TYPE_MANAGE).
  6. INSERT business process catalog entries (SERVICE_PRICE_OVERRIDE, RENEW_SERVICE_STANDARD) if not present.
  7. INSERT SchemaVersions record for V0011.

- **Rollback**: U0011__drop_service_schema.sql
  1. DROP TABLE Service_History (depends on Services).
  2. DROP TABLE Services (depends on Service_Types, Customers, Companies).
  3. DROP TABLE Service_Price_History (depends on Service_Types).
  4. DROP TABLE Service_Types.
  5. UPDATE Permissions SET is_active = 0 WHERE permission_code IN ('SERVICE_VIEW', 'SERVICE_TYPE_MANAGE').
  6. UPDATE Business_Process_Catalog SET is_active = 0 WHERE process_code IN ('SERVICE_PRICE_OVERRIDE', 'RENEW_SERVICE_STANDARD').
  7. DELETE FROM SchemaVersions WHERE Version = 'V0011'.

- **SchemaVersions**: DbMigrator inserts V0011 record. U0011 removes it.
- **Test DB reset**: TestDatabaseFixture.ResetToV0011() chains from ResetToV0010(), executes V0011, inserts SchemaVersions record.
- **SafeTestWebApplicationFactory**: Updated to call ResetToV0011().
- **KnownTables**: Add Service_Types, Service_Price_History, Services, Service_History to KnownTables set.
- **MigrationRollbackTests**: Add V0011/U0011 test case.
- **No production migration**.

## Domain and Application Strategy

### Entities

| Entity | File | Status Constants | Behavior Methods |
|---|---|---|---|
| ServiceType | ServiceType.cs | N/A (IsActive flag) | SetStandardPrice(price, reason, userId), Deactivate(), Activate() |
| ServicePriceHistory | ServicePriceHistory.cs | N/A (append-only) | Constructor only |
| Service | Service.cs | ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE | Expire(), Cancel(reason), ApplyPriceOverride(price, approvalRequestId), static CreateStandard(...), static CreateRenewal(...) |
| ServiceHistory | ServiceHistory.cs | CREATED, RENEWED, PRICE_OVERRIDDEN, CANCELLED, EXPIRED | Constructor only |

All entities follow established conventions: private setters, private parameterless EF constructor, public domain constructor with validation, behavior methods for state transitions, byte[] RowVersion where applicable.

### Application Services

| Interface | Implementation | Purpose |
|---|---|---|
| IServiceTypeService | ServiceTypeService | Service type catalog CRUD, price management |
| IServiceService | ServiceService | Service creation (standard), renewal (standard), price override request, list/detail queries |

Services use IOrganizationDbContextFactory → CreateDbContext() pattern. Return DTOs, not entities. Async with CancellationToken.

### Execution Handler

| Handler | ProcessCode | Purpose |
|---|---|---|
| ServicePriceOverrideExecutionHandler | SERVICE_PRICE_OVERRIDE | Apply approved override price to service. Validate service exists and is in valid state. Update price fields. Create ServiceHistory. Serializable transaction. Idempotency via status check. |

Follows CustomerMergeExecutionHandler pattern: IWorkflowExecutionHandler interface, IOrganizationDbContextFactory + ITransactionalAuditWriter dependencies.

### DTOs

| DTO | Purpose |
|---|---|
| ServiceTypeDto | Service type catalog item response |
| CreateServiceTypeRequest | Admin: create new service type |
| UpdateServiceTypeRequest | Admin: update service type |
| ServiceDto | Service instance response |
| CreateServiceRequest | Create service at standard price |
| RenewServiceRequest | Renew service at standard price |
| RequestPriceOverrideRequest | Request non-standard pricing (triggers workflow) |
| ServiceListResponse | Paginated service list |

### Transaction Boundaries

- Service creation: single transaction — create Service, create ServiceHistory (CREATED), validate Customer_Company_Context exists.
- Service renewal: single transaction — create new Service (linked via PreviousServiceId), create ServiceHistory (RENEWED), expire previous Service.
- Price override execution: Serializable transaction — validate service, apply price, create ServiceHistory (PRICE_OVERRIDDEN).

### Concurrency

- ServiceType: rowversion for catalog updates.
- Service: rowversion for lifecycle transitions (cancel, expire, price override).
- DbUpdateConcurrencyException → 409 Conflict with sanitized message.

### Audit

- ServiceHistory captures all lifecycle events with before/after JSON snapshots, actor, reason, correlation_id.
- ServicePriceHistory captures standard price changes.
- Both are append-only (SEC-001).

## API v2 Strategy

### ServiceType Controller

Route: /api/v2/service-types

| Method | Path | Purpose | Permission | Request DTO | Response DTO | Key Validation |
|---|---|---|---|---|---|---|
| GET | / | List active service types | SERVICE_VIEW (COMPANY) | Query params: page, pageSize | ServiceTypeListResponse | None |
| GET | /{id} | Service type detail | SERVICE_VIEW (COMPANY) | N/A | ServiceTypeDto | 404 if not found |
| POST | / | Create service type | SERVICE_TYPE_MANAGE (GLOBAL) | CreateServiceTypeRequest | ServiceTypeDto | Code uniqueness, non-empty name, price > 0 |
| PUT | /{id} | Update service type | SERVICE_TYPE_MANAGE (GLOBAL) | UpdateServiceTypeRequest | ServiceTypeDto | 409 concurrency, code immutability |
| POST | /{id}/deactivate | Deactivate service type | SERVICE_TYPE_MANAGE (GLOBAL) | N/A | ServiceTypeDto | 409 concurrency |

### Service Controller

Route: /api/v2/services

| Method | Path | Purpose | Permission | Request DTO | Response DTO | Key Validation |
|---|---|---|---|---|---|---|
| GET | / | List services by company | SERVICE_VIEW (COMPANY) | Query: companyId, customerId, status, page, pageSize | ServiceListResponse | Company scope enforcement |
| GET | /{id} | Service detail | SERVICE_VIEW (COMPANY) | N/A | ServiceDto | 404, company scope |
| POST | / | Create service (standard) | SERVICE_CREATE_STANDARD (COMPANY) | CreateServiceRequest | ServiceDto | Customer_Company_Context exists, service type active, price = standard |
| POST | /{id}/renew | Renew (standard) | SERVICE_RENEW_STANDARD (COMPANY) | RenewServiceRequest | ServiceDto | Service exists and is ACTIVE or EXPIRED, price = standard snapshot, creates new cycle |
| POST | /{id}/request-price-override | Request override | SERVICE_PRICE_OVERRIDE_REQUEST (COMPANY) | RequestPriceOverrideRequest | WorkflowRequestResponse | Price differs from standard, creates workflow request |

### Error Handling

All endpoints follow established sanitized error patterns:

| HTTP Status | Scenario | Sanitized Message |
|---|---|---|
| 400 | Invalid input, missing Customer_Company_Context, price validation, inactive service type | Specific validation message per scenario |
| 403 | Permission denied | "You do not have permission to perform this action." |
| 404 | Service or service type not found | "Service not found." / "Service type not found." |
| 409 | Concurrency conflict | "Data has changed since you started. Please refresh and try again." |
| 500 | Internal error | "An unexpected error occurred. Please try again." |

No raw SQL, stack traces, or internal exception details in any response.

## Workflow Integration Strategy

### SERVICE_PRICE_OVERRIDE

- **Trigger**: POST /api/v2/services/{id}/request-price-override.
- **Workflow creation**: ServiceService creates a workflow request using the existing workflow submission infrastructure with process_code "SERVICE_PRICE_OVERRIDE" and condition fields (company_id, standard_price, requested_price, discount_amount, discount_percent, service_type).
- **Service status**: Service transitions to PENDING_PRICE_OVERRIDE while the workflow is pending.
- **Approval result**: On approval, WorkflowRuntimeService invokes ServicePriceOverrideExecutionHandler which applies the override price.
- **Rejection result**: On rejection, the service reverts to its previous status (ACTIVE). ServiceHistory records the rejection.

### RENEW_SERVICE_STANDARD

- **No workflow**: Standard-price renewal is a direct operation.
- **Price validation**: The system verifies that the applied price equals the current standard-price snapshot on the ServiceType. If it differs, the operation is rejected with a validation error directing the user to request a SERVICE_PRICE_OVERRIDE.

### Deferred

- CARD_REPRINT: separate later phase. Service entity will be referenceable by future card entities.
- SELL_CARE_PACKAGE: RESERVED / INACTIVE. Must not be activated.

## Permission Strategy

### Permissions to Seed in V0011

| Code | Module | Action | Scope | Sensitive | Delegable | Purpose |
|---|---|---|---|---|---|---|
| SERVICE_VIEW | SERVICE | VIEW | COMPANY | No | No | View services within assigned company |
| SERVICE_TYPE_MANAGE | SERVICE | MANAGE_CATALOG | GLOBAL | Yes | No | Manage service type catalog (ADMIN_SERVICE_DATA) |

### Pre-existing Permissions (already seeded)

| Code | Scope | Purpose |
|---|---|---|
| SERVICE_CREATE_STANDARD | COMPANY | Create service at standard terms |
| SERVICE_RENEW_STANDARD | COMPANY | Renew at standard snapshot price |
| SERVICE_PRICE_OVERRIDE_REQUEST | COMPANY | Request non-standard pricing |
| SERVICE_PRICE_OVERRIDE_APPROVE | COMPANY | Approve non-standard price |

### Rollback Behavior

U0011 soft-deactivates SERVICE_VIEW and SERVICE_TYPE_MANAGE (UPDATE IsActive = 0). Pre-existing permissions are not touched by U0011.

### Permission-catalog.md

This plan proposes SERVICE_VIEW and SERVICE_TYPE_MANAGE. permission-catalog.md is not modified by this plan. If PO accepts, these permissions will be seeded in V0011 and documented post-implementation.

## Validation and Business Rules

### Service Type Validation

- Code: non-empty, unique, max 50 characters.
- Name: non-empty, max 200 characters.
- StandardPrice: > 0.
- StandardPriceCurrency: must be 'VND' (per PAY-006 currency invariant pattern).
- CycleDurationMonths: null or > 0.
- IsActive: only active types can be used for new service creation.

### Service Creation Validation

- ServiceType must exist and be active.
- Customer must exist and be active (not MERGED, not inactive).
- Company must exist and be active.
- Customer_Company_Context must exist for (customerId, companyId) — application validation.
- CompanyId must match the user's active company assignment (AUTH-007).
- AppliedPrice must equal ServiceType.StandardPrice for standard creation.
- ValidFrom is required. ValidTo is optional (null = until cancellation/expiry).
- CycleNumber defaults to 1 for initial creation.
- StandardPriceSnapshot captures ServiceType.StandardPrice at creation time.

### Service Renewal Validation

- Service must exist.
- Service must be ACTIVE or EXPIRED (renewable states).
- Company scope enforcement.
- New Service row created with PreviousServiceId = current service ID.
- CycleNumber = previous CycleNumber + 1.
- AppliedPrice must equal current ServiceType.StandardPrice. If different, reject with guidance to request SERVICE_PRICE_OVERRIDE.
- Previous service transitions to EXPIRED.

### Price Override Validation

- Service must exist and be in ACTIVE state.
- Requested price must differ from StandardPriceSnapshot.
- Creates workflow request with SERVICE_PRICE_OVERRIDE process code.
- Service transitions to PENDING_PRICE_OVERRIDE.

### Concurrency

- rowversion checked on ServiceType updates.
- rowversion checked on Service lifecycle transitions.
- DbUpdateConcurrencyException → 409 Conflict.

### Permission Denied

- 403 with sanitized message. No internal details.

### Not Found

- 404 with sanitized message. No entity type or ID in response.

## Test Strategy

### Unit Tests

- ServiceType: constructor validation, SetStandardPrice behavior, Activate/Deactivate.
- Service: CreateStandard factory validation, CreateRenewal factory validation, Expire/Cancel/ApplyPriceOverride behavior, status transition guards.
- ServiceService: creation validation (Customer_Company_Context check, price snapshot capture, active type check). Renewal validation (price comparison, cycle increment, previous service expiry). Price override validation (price differs, workflow request creation).
- ServicePriceOverrideExecutionHandler: idempotency check, price application, ServiceHistory creation, concurrency handling.

### Integration Tests

- V0011/U0011 migration rollback (MigrationRollbackTests).
- ServiceType EF persistence: insert, query, update, rowversion.
- ServicePriceHistory EF persistence: insert, query by service type.
- Service EF persistence: insert, query by company, FK enforcement, rowversion.
- ServiceHistory EF persistence: insert, query by service.
- FK constraint enforcement: cannot create Service with invalid ServiceTypeId, CustomerId, CompanyId.
- Cascade behavior: RESTRICT prevents deletion of referenced entities.

### API Tests

- ServiceType endpoints: list, detail, create, update, deactivate. Permission enforcement (SERVICE_VIEW, SERVICE_TYPE_MANAGE).
- Service endpoints: list (company-scoped), detail, create (standard), renew (standard), request-price-override. Permission enforcement (SERVICE_VIEW, SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST).
- Company scope enforcement: user cannot access services in unauthorized company.
- Concurrency: 409 on stale rowversion.
- Validation: 400 on invalid input, missing Customer_Company_Context, inactive service type, price mismatch on renewal.
- Error sanitization: no raw SQL, no stack traces, no internal exception details.
- Workflow integration: price override request creates workflow instance.
- Execution handler: ServicePriceOverrideExecutionHandler applies price on approval.

### Migration Rollback Tests

- V0011 forward: tables exist, permissions seeded, SchemaVersions record present.
- U0011 rollback: tables dropped, permissions deactivated, SchemaVersions record removed.

### No Frontend Tests in 1B.6-B

Frontend tests are deferred to Phase 1B.6-C.

## Implementation Sequence if Accepted

1. V0011/U0011 migration and rollback scripts.
2. Domain entities (ServiceType, ServicePriceHistory, Service, ServiceHistory).
3. EF configurations (4 configuration files).
4. IOrganizationDbContext / OrganizationDbContext DbSet updates.
5. Permission seed verification (SERVICE_VIEW, SERVICE_TYPE_MANAGE in V0011).
6. DTOs (ServiceTypeDto, ServiceDto, request/response DTOs).
7. Application services (IServiceTypeService/ServiceTypeService, IServiceService/ServiceService).
8. ServicePriceOverrideExecutionHandler.
9. API controllers (ServiceTypeController, ServiceController).
10. DI registration in Program.cs.
11. TestDatabaseFixture updates (KnownTables, ResetToV0011, DropKnownSchema).
12. SafeTestWebApplicationFactory update (ResetToV0011).
13. MigrationRollbackTests extension (V0011/U0011).
14. Unit tests.
15. Integration tests.
16. API tests.
17. Implementation report.
18. Full backend validation (build, all test suites).

## Validation Commands for Future Implementation

```
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
git diff --check
```

## Risks

1. **Service/payment boundary ambiguity**: Service includes CycleNumber and pricing fields for future Payment reference, but the exact payment-item → service FK is deferred. Risk: Payment phase may require Service schema additions.

2. **Pricing/versioning ambiguity**: Global-per-type standard pricing may need per-company extension later. ServicePriceHistory entity mitigates by tracking price changes independently.

3. **Workflow scope creep**: Only SERVICE_PRICE_OVERRIDE is in scope. SELL_CARE_PACKAGE must not be activated. CARD_REPRINT is deferred.

4. **Customer-service linkage**: Service uses (CustomerId, CompanyId) FKs with application-level Customer_Company_Context validation. Risk: no database-level enforcement of Customer_Company_Context existence. Mitigation: application validation in ServiceService before any insert.

5. **Permission catalog drift**: SERVICE_VIEW and SERVICE_TYPE_MANAGE are proposed new permissions. If PO does not accept them, the plan must be adjusted (e.g. reuse SERVICE_CREATE_STANDARD for read access).

6. **Migration rollback safety**: U0011 must drop 4 tables in correct dependency order and soft-deactivate 2 permissions. Risk: incorrect drop order causes FK violation. Mitigation: explicit drop order in plan.

7. **Test DB reset target drift**: TestDatabaseFixture must add ResetToV0011() and SafeTestWebApplicationFactory must call it. Mechanical change following established pattern.

8. **Overbuilding beyond foundation scope**: Service module must be minimal and correct. No speculative fields for unconfirmed requirements (e.g. plot linkage, document attachments).

9. **Pre-existing permission seed verification**: SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE must already exist in the Permissions table. V0011 must not re-insert them. Implementation must verify they are present.

## Recommended Next Gate

Project Owner backend/data scope acceptance for Phase 1B.6-B.

## Recommended Authorization Wording

Authorized next task:
Phase 1B.6-B Service Module Foundation backend/data implementation only.

Implementation must stay within the accepted backend/data scope.

Do not authorize:
- frontend implementation,
- Payment implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

This document does not:
- modify business requirements,
- create source code,
- create tests,
- create frontend files,
- create backend files,
- create migrations or rollbacks,
- modify permission-catalog.md,
- authorize implementation,
- authorize production migration,
- authorize release tag,
- authorize push.
