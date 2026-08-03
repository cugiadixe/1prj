# Phase 1B.6 Service Module Foundation Discovery and Detailed Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Post-Phase 1B.5 Project Owner next-work decision commit:
  154241b1c5805e471d5319690c49ea45816efe0f

State:
- Phase 1B.5 Customer Merge and Duplicate Resolution is closed.
- Phase 1B.6 Service Module Foundation is selected.
- This document is discovery and detailed planning only.
- This document does not authorize implementation.

## Objective

Define the discovery findings, scope boundaries, open decisions, and recommended implementation approach for Phase 1B.6 Service Module Foundation.

## Source Documents Reviewed

- docs/architecture/post-1b5-project-owner-next-work-decision.md
- docs/architecture/post-1b5-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b5-project-owner-closure-acceptance.md
- docs/architecture/phase-1b5-customer-merge-duplicate-resolution-discovery-and-detailed-plan.md
- docs/architecture/phase-1b4-project-owner-closure-acceptance.md
- docs/architecture/project-readiness-review.md
- docs/architecture/phase-1b0-security-discovery-decisions.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/business/process-catalog.md
- database/migrations/ (V0001 through V0010)
- database/rollbacks/ (U0001 through U0010)
- src/backend/PTKD.Api/Controllers/ (12 controllers)
- src/backend/PTKD.Domain/Entities/ (25 entity files)
- src/frontend/src/App.tsx (route structure)
- src/frontend/src/components/AuthenticatedShell.tsx (navigation/permission gating)

Missing sources:
- PTKD-ERP-Master-Context.md: file does not exist at repository root.
- docs/business/PTKD-Specification-v1.1.md: referenced in Phase 1B.5 plan source list; not independently read for this plan. Business rules, permission catalog, acceptance criteria, and process catalog serve as the primary specification sources.

## Completed Foundation Summary

### Security / Auth / Admin (Phase 1B.0, 1B.1)
- JWT authentication with refresh token rotation.
- IPermissionEvaluator with department/role/individual/DENY evaluation.
- Role, AdminGroup, IndividualPermission management.
- RequirePermission attribute enforcement on all controllers.
- Security audit controls.
- First-admin provisioning.
- 20 security architecture decisions approved (DEC-1B-001 through DEC-1B-021).

### Permission and Scope Model
- Permission catalog with GLOBAL and COMPANY scopes.
- Service-related permissions already cataloged: SERVICE_CREATE_STANDARD (COMPANY), SERVICE_RENEW_STANDARD (COMPANY), SERVICE_PRICE_OVERRIDE_REQUEST (COMPANY, sensitive), SERVICE_PRICE_OVERRIDE_APPROVE (COMPANY, sensitive, delegable).
- ADMIN_SERVICE_DATA admin group documented.
- PTKD_MANAGER role includes SERVICE_PRICE_OVERRIDE_APPROVE.

### Workflow / Approval Foundation (Phase 1B.3)
- Sequential approval workflow engine (DRAFT → PUBLISHED → ACTIVE → RETIRED).
- Workflow definitions, versions, steps, approver rules, conditions, bindings.
- Approval runtime: APPROVE, REJECT, RETURN, RESUBMIT.
- Execution handler framework with proven handlers: CUSTOMER_CREATE_FROM_APPROVAL, CUSTOMER_UPDATE_FROM_APPROVAL, CUSTOMER_MERGE_FROM_APPROVAL.
- SLA/reminder infrastructure.
- Delegation support.
- Frontend workflow admin UI (definitions, versions, bindings) and runtime UI (my-approvals, my-requests, instance detail).

### Customer Foundation (Phase 1B.2, 1B.4, 1B.5)
- Profiles (GLOBAL) and Customers (GLOBAL).
- Customer_Company_Context (unique by customer+company, COMPANY scoped).
- CustomerChangeRequest with target rowversion / concurrency.
- Customer Merge with duplicate detection, merge request/candidates/history, execution handler.
- Duplicate CCCD checking (filtered unique index).

### API v2 Pattern
- Base route: /api/v2.
- Controllers with RequirePermission attributes.
- Problem Details error responses.
- DTOs (no direct entity exposure).
- axiosClient baseURL: http://localhost:5057/api/v2.
- 12 controllers in production.

### SQL Server Migration / Rollback Pattern
- Forward migrations: V0001 through V0010 in database/migrations/.
- Rollback scripts: U0001 through U0010 in database/rollbacks/.
- bigint IDENTITY(1,1) PKs.
- rowversion for concurrency.
- datetime2(3) timestamps.
- DbMigrator owns SchemaVersions table.
- TestDatabaseFixture with sequential ResetToV000X() methods.
- SafeTestWebApplicationFactory uses ResetToV0010().
- Test database: PTKD_TEST_PHASE1A2.

### Frontend Pattern
- React 19, Vite, Ant Design v6.
- React Query (@tanstack/react-query) for data fetching.
- Feature-based folder structure (src/customers/, src/components/).
- Route registration in App.tsx.
- Permission-gated menu items in AuthenticatedShell.tsx using hasPermission(code, scope).
- Vitest + React Testing Library.
- Sanitized error handling with error message mapping modules.

### Current Test Baseline
- Backend: 158 unit, 196 integration, 267 API tests (621 total).
- Frontend: 53 test files, 417 tests.
- Database: V0010 is current migration ceiling.

## Confirmed Business Context

### Service / Payment Dependency
- DATA-003: "Service, payment, reconciliation, operational documents and approval requests are scoped by company_id."
- PAY-008: "Payment correction must preserve customer/company/service-cycle consistency and must not pay the same cycle twice."
- Service entities must exist before Payment can enforce service-cycle consistency. This is the primary reason Service Module precedes Payment.

### Service Processes (from process-catalog.md)
- **RENEW_SERVICE_STANDARD**: approval_mode NONE. "No approval when price equals the captured standard-price snapshot." Permission: implicit (SERVICE_RENEW_STANDARD in permission catalog).
- **SERVICE_PRICE_OVERRIDE**: approval_mode CONDITIONAL. "Required whenever requested price differs from standard snapshot." Submit permission: SERVICE_PRICE_OVERRIDE_REQUEST. Handler: SERVICE_PRICE_OVERRIDE_FROM_APPROVAL. Condition fields: company_id, standard_price, requested_price, discount_amount, discount_percent, service_type.
- **SELL_CARE_PACKAGE**: RESERVED / INACTIVE. "The business need is confirmed, but form fields, entity schema, execution handler and exact approval trigger require the service-sales module specification before activation." Must not be implemented or activated.

### Service Permissions (from permission-catalog.md)
- SERVICE_CREATE_STANDARD (SERVICE, CREATE, COMPANY): Create a service at standard terms.
- SERVICE_RENEW_STANDARD (SERVICE, RENEW, COMPANY): Renew at standard snapshot price.
- SERVICE_PRICE_OVERRIDE_REQUEST (SERVICE, REQUEST_PRICE, COMPANY, sensitive): Request non-standard service pricing.
- SERVICE_PRICE_OVERRIDE_APPROVE (SERVICE, APPROVE_PRICE, COMPANY, sensitive, delegable): Approve a non-standard service price.

### Customer-Service Linkage
- DATA-002: "Company-specific customer information is stored in Customer_Company_Context, unique by (customer_id, company_id)."
- DATA-003 confirms services are COMPANY-scoped.
- Services will link to customers through Customer_Company_Context (customer + company).

### Company Scope
- AUTH-007: "A COMPANY permission is effective only when the user has an ACTIVE company assignment for that company."
- All service permissions are COMPANY-scoped. Users can only manage services within companies where they have active assignments.

### Workflow / Approval Relevance
- APR-002: "Standard-price renewal creates no approval; price differing from snapshot requires SERVICE_PRICE_OVERRIDE."
- APR-003: "Unapproved exceptional price cannot be used to create/confirm a bill."
- The workflow foundation supports SERVICE_PRICE_OVERRIDE. The execution handler pattern is proven.

### Pricing / Versioning
- SERVICE_PRICE_OVERRIDE condition fields include standard_price, requested_price, discount_amount, discount_percent, service_type.
- This implies a standard-price snapshot mechanism: the system must capture the standard price at the time of service creation/renewal, and any deviation triggers the override workflow.

### Card Reprint Dependency
- CARD_REPRINT: approval_mode CONDITIONAL. Condition fields include company_id, previous_print_count, reprint_number, fee_amount, reason_code.
- Cards are service artifacts. Card Reprint depends on the service domain existing but is out of scope for Phase 1B.6.

### Care Package Dependency
- SELL_CARE_PACKAGE: RESERVED / INACTIVE. Depends on service-sales module specification. Out of scope for Phase 1B.6.

### Admin Group
- ADMIN_SERVICE_DATA: "Service/package catalog and service data." This admin group will manage service catalog and service data.

### Department Baseline
- PTKD / Kinh doanh: baseline includes "create/renew standard service."
- This confirms business staff need SERVICE_CREATE_STANDARD and SERVICE_RENEW_STANDARD.

### Role
- PTKD_MANAGER: includes SERVICE_PRICE_OVERRIDE_APPROVE (COMPANY, delegable).

## Proposed Scope Boundary

### In Scope for Phase 1B.6 Foundation

1. **Service Type Catalog**: definition of available service types (e.g. burial care, maintenance). Entity for service type definitions with standard pricing.

2. **Service Entity**: core service record linking a customer (via Customer_Company_Context) to a service type, with lifecycle status, pricing, validity period, and company scope.

3. **Service Lifecycle / Status Model**: statuses for service lifecycle (e.g. DRAFT, ACTIVE, EXPIRED, CANCELLED). Transitions and rules.

4. **Standard Pricing Snapshot**: mechanism to capture the standard price at service creation/renewal time, enabling comparison for SERVICE_PRICE_OVERRIDE detection.

5. **Service Creation (Standard)**: API and frontend for creating a service at standard terms. Permission: SERVICE_CREATE_STANDARD. No approval required.

6. **Service Renewal (Standard)**: API and frontend for renewing a service at standard snapshot price. Permission: SERVICE_RENEW_STANDARD. No approval required per RENEW_SERVICE_STANDARD process.

7. **SERVICE_PRICE_OVERRIDE Workflow Integration**: execution handler (SERVICE_PRICE_OVERRIDE_FROM_APPROVAL) for approved non-standard pricing. Workflow binding, condition fields, approval flow.

8. **Service List / Detail / Search Frontend**: service catalog browsing, service detail view, service search by customer/company.

9. **Permission Enforcement**: backend RequirePermission for all service endpoints. Frontend hasPermission gating.

10. **V0011/U0011 Migration and Rollback**: service schema creation and rollback.

11. **Tests**: unit, integration, API, migration rollback, and frontend tests.

### Out of Scope / Deferred

- **Payment implementation**: full billing, collection, reconciliation. Deferred to Phase 1B.7 or later. Service module establishes the foundation Payment will reference.
- **Card Reprint implementation**: depends on services but is a separate workflow. Deferred.
- **Care Package Sales (SELL_CARE_PACKAGE)**: RESERVED / INACTIVE. Must not be activated without functional specification.
- **Service-to-Plot/Location linkage**: DATA-008 references Site-Company scope inheritance. Whether services link to physical locations (plots) is not confirmed. Deferred until Plot entity domain exists.
- **Reporting / Reconciliation**: deferred to Payment phase.
- **Production migration**: not authorized.
- **Release tag**: not authorized.
- **Push**: not authorized.

## Proposed Domain Model

### ServiceType (New Entity)

- **Purpose**: Define available service types with standard pricing. Acts as the service catalog.
- **Key fields (conceptual)**:
  - Id (bigint IDENTITY PK)
  - Code (nvarchar, unique, stable identifier for the service type)
  - Name (nvarchar, Vietnamese display name)
  - Description (nvarchar, optional)
  - StandardPrice (decimal, current standard price)
  - StandardPriceCurrency (nvarchar, default 'VND')
  - CycleDurationMonths (int, renewal cycle length in months, nullable if one-time)
  - IsActive (bit, only active types can be used for new services)
  - CreatedAt, UpdatedAt (datetime2(3))
  - RowVersion (rowversion)
- **Dependencies**: None. Root catalog entity.
- **Unresolved decisions**: exact service type taxonomy (what types exist), whether pricing tiers exist per company, whether cycle duration is fixed per type or configurable per service instance.

### ServicePriceHistory (New Entity)

- **Purpose**: Track standard price changes over time for audit and snapshot comparison.
- **Key fields (conceptual)**:
  - Id (bigint IDENTITY PK)
  - ServiceTypeId (bigint FK → ServiceType)
  - Price (decimal)
  - EffectiveFrom (datetime2(3))
  - EffectiveTo (datetime2(3), nullable, null = current)
  - ChangedByUserId (bigint FK → Users)
  - ChangeReason (nvarchar)
  - CreatedAt (datetime2(3))
- **Dependencies**: ServiceType.
- **Unresolved decisions**: whether price history is per-type globally or per-type-per-company.

### Service (New Entity)

- **Purpose**: Core service record representing a service instance for a customer at a company.
- **Key fields (conceptual)**:
  - Id (bigint IDENTITY PK)
  - ServiceTypeId (bigint FK → ServiceType)
  - CustomerId (bigint FK → Customers)
  - CompanyId (bigint FK → Companies)
  - Status (nvarchar: ACTIVE, EXPIRED, CANCELLED, PENDING_APPROVAL)
  - AppliedPrice (decimal, the price actually applied — standard or approved override)
  - StandardPriceSnapshot (decimal, the standard price at time of creation/renewal)
  - IsOverridePrice (bit, whether price differs from standard snapshot)
  - OverrideApprovalRequestId (bigint, nullable FK → workflow instance, if override was approved)
  - ValidFrom (datetime2(3))
  - ValidTo (datetime2(3), nullable if perpetual or until cancellation)
  - CycleNumber (int, which renewal cycle this represents)
  - PreviousServiceId (bigint, nullable FK → Service, for renewal chain)
  - CreatedByUserId (bigint FK → Users)
  - CreatedAt, UpdatedAt (datetime2(3))
  - RowVersion (rowversion)
- **Dependencies**: ServiceType, Customers, Companies. Customer_Company_Context must exist for the (CustomerId, CompanyId) pair.
- **Unresolved decisions**: whether Service links to Customer_Company_Context directly or via (CustomerId, CompanyId) FK pair; whether CycleNumber is auto-incremented; whether renewal creates a new Service row or updates the existing one.

### ServiceHistory (New Entity)

- **Purpose**: Audit trail for service lifecycle changes (creation, renewal, cancellation, price override application).
- **Key fields (conceptual)**:
  - Id (bigint IDENTITY PK)
  - ServiceId (bigint FK → Service)
  - ActionCode (nvarchar: CREATED, RENEWED, PRICE_OVERRIDDEN, CANCELLED, EXPIRED)
  - BeforeData (nvarchar(max), JSON snapshot before change)
  - AfterData (nvarchar(max), JSON snapshot after change)
  - ActedByUserId (bigint FK → Users)
  - Reason (nvarchar, nullable)
  - CorrelationId (uniqueidentifier)
  - CreatedAt (datetime2(3))
- **Dependencies**: Service.
- **Unresolved decisions**: none significant.

## SQL Server / Migration Strategy

Plan only. No migration files to be created.

- **Next migration**: V0011 / U0011.
- **Naming convention**: V0011__create_service_schema.sql / U0011__drop_service_schema.sql (following established pattern).
- **Schema**: Tables will be created in the default schema (dbo), consistent with existing entities (Customers, Customer_Company_Context, etc.).
- **rowversion**: Required on ServiceType (catalog updates), Service (concurrency on lifecycle changes). ServicePriceHistory and ServiceHistory are append-only and do not need rowversion.
- **FK / Index strategy**:
  - ServiceType: unique index on Code.
  - Service: FK to ServiceType, Customers, Companies. Index on (CustomerId, CompanyId). Index on (CompanyId, Status) for company-scoped queries. Unique constraint on (CustomerId, CompanyId, ServiceTypeId, CycleNumber) if cycle-based renewal model is confirmed.
  - ServicePriceHistory: FK to ServiceType, index on (ServiceTypeId, EffectiveFrom).
  - ServiceHistory: FK to Service, index on (ServiceId, CreatedAt).
- **SchemaVersions**: DbMigrator will add V0011 record on forward migration. U0011 will remove V0011 record.
- **Rollback safety**: U0011 must drop Service-related tables in dependency order (ServiceHistory → Service → ServicePriceHistory → ServiceType). If service permissions are seeded via V0011, U0011 must soft-deactivate them (UPDATE, not DELETE) due to TR_Permissions_PreventDelete.
- **Test DB impact**: TestDatabaseFixture must add ResetToV0011(). SafeTestWebApplicationFactory must be updated to call ResetToV0011().
- **Migration rollback tests**: MigrationRollbackTests must cover V0011/U0011.
- **No production migration**: confirmed.

## API v2 Strategy

Plan only. No controllers or DTOs to be created.

### Service Type Catalog Endpoints

| Endpoint | Method | Purpose | Permission | Notes |
|---|---|---|---|---|
| /api/v2/service-types | GET | List active service types | SERVICE_CREATE_STANDARD or SERVICE_RENEW_STANDARD | Public catalog query for authorized users |
| /api/v2/service-types/{id} | GET | Service type detail with current standard price | SERVICE_CREATE_STANDARD or SERVICE_RENEW_STANDARD | Includes pricing info |

Service type management (create/update/deactivate) requires ADMIN_SERVICE_DATA permissions. Whether admin endpoints are in Phase 1B.6 scope is an open decision.

### Service Endpoints

| Endpoint | Method | Purpose | Permission | Notes |
|---|---|---|---|---|
| /api/v2/services | GET | List services filtered by company, customer, status | SERVICE_CREATE_STANDARD (COMPANY scoped) | Company-scoped query |
| /api/v2/services/{id} | GET | Service detail | SERVICE_CREATE_STANDARD (COMPANY scoped) | Includes pricing, lifecycle, cycle info |
| /api/v2/services | POST | Create a new service at standard price | SERVICE_CREATE_STANDARD (COMPANY) | Validates Customer_Company_Context exists, captures standard price snapshot |
| /api/v2/services/{id}/renew | POST | Renew at standard price | SERVICE_RENEW_STANDARD (COMPANY) | Creates new cycle, captures price snapshot, no approval if price matches |
| /api/v2/services/{id}/request-price-override | POST | Request non-standard pricing | SERVICE_PRICE_OVERRIDE_REQUEST (COMPANY) | Creates workflow request for SERVICE_PRICE_OVERRIDE process |

### Validation Concerns

- Customer_Company_Context must exist for (customerId, companyId) before service creation.
- Standard price snapshot must be captured at creation/renewal time.
- Price override request must include requested_price, and it must differ from standard_price.
- Company scope must be enforced — user can only create/view services for companies where they have active assignments.
- rowversion concurrency on service updates.

### Error Handling

- 400: invalid input, missing Customer_Company_Context, price validation failures.
- 403: permission denied (sanitized).
- 404: service or service type not found (sanitized).
- 409: concurrency conflict (sanitized, same pattern as customer merge).
- Sanitized error mapping module (serviceErrorMessages.ts) following customerMergeErrorMessages.ts pattern.

## Workflow / Approval Strategy

Plan only.

### SERVICE_PRICE_OVERRIDE Workflow

- **Process code**: SERVICE_PRICE_OVERRIDE (already in process-catalog.md).
- **Approval mode**: CONDITIONAL. Required when requested price differs from standard snapshot.
- **Execution handler**: SERVICE_PRICE_OVERRIDE_FROM_APPROVAL. This handler will:
  - Validate the service still exists and is in a valid state.
  - Apply the approved override price to the service.
  - Update IsOverridePrice = true, OverrideApprovalRequestId = workflow instance ID.
  - Create ServiceHistory record with PRICE_OVERRIDDEN action.
  - All within a Serializable transaction (same pattern as CustomerMergeExecutionHandler).
- **Condition fields**: company_id, standard_price, requested_price, discount_amount, discount_percent, service_type. These are already defined in process-catalog.md.
- **Approver**: PTKD_MANAGER role has SERVICE_PRICE_OVERRIDE_APPROVE (delegable).

### RENEW_SERVICE_STANDARD (No Approval)

- No workflow integration needed. Standard-price renewal is a direct operation.
- The system must verify that the renewal price equals the current standard-price snapshot. If it differs, the operation must be rejected and the user directed to submit a SERVICE_PRICE_OVERRIDE request instead.

### Card Reprint and Care Package Sales

- CARD_REPRINT: requires service domain (cards are service artifacts). Separate later workflow. Phase 1B.6 should ensure the Service entity supports card-related references but should not implement CARD_REPRINT.
- SELL_CARE_PACKAGE: RESERVED / INACTIVE. Phase 1B.6 must not activate or implement this process.

### What Phase 1B.6 Should Prepare But Not Implement

- Service entity schema that Payment can reference (service_id FK from future payment items).
- Service cycle numbering that PAY-008 can use for service-cycle consistency checks.
- ServiceType catalog that Card Reprint can reference for card generation rules.

## Permission Strategy

Plan only. Permissions will not be modified until implementation is authorized.

### Confirmed Permissions (already in permission-catalog.md)

| Permission Code | Module | Scope | Purpose |
|---|---|---|---|
| SERVICE_CREATE_STANDARD | SERVICE | COMPANY | Create a service at standard terms |
| SERVICE_RENEW_STANDARD | SERVICE | COMPANY | Renew at standard snapshot price |
| SERVICE_PRICE_OVERRIDE_REQUEST | SERVICE | COMPANY | Request non-standard pricing |
| SERVICE_PRICE_OVERRIDE_APPROVE | SERVICE | COMPANY | Approve non-standard price (delegable) |

### Proposed Additional Permissions

| Permission Code | Module | Scope | Purpose | Status |
|---|---|---|---|---|
| SERVICE_VIEW | SERVICE | COMPANY | View services within assigned company | Proposed — requires PO decision |
| SERVICE_MANAGE | SERVICE | COMPANY | Manage service lifecycle (cancel, update metadata) | Proposed — requires PO decision |
| SERVICE_TYPE_MANAGE | SERVICE | GLOBAL | Manage service type catalog (ADMIN_SERVICE_DATA) | Proposed — requires PO decision |

Whether SERVICE_VIEW is a separate permission or combined with SERVICE_CREATE_STANDARD read access is an open decision.

### Permission Seeding

- New permissions will be seeded via V0011 migration (INSERT into Permissions table).
- U0011 rollback will soft-deactivate new permissions (UPDATE IsActive = 0, not DELETE) per TR_Permissions_PreventDelete pattern.

## Frontend Strategy

Plan only. No frontend files to be created.

### Candidate Screens

| Route | Page | Permission Gate | Purpose |
|---|---|---|---|
| /services | ServiceListPage | SERVICE_VIEW or SERVICE_CREATE_STANDARD | List services filtered by company/customer/status |
| /services/{id} | ServiceDetailPage | SERVICE_VIEW or SERVICE_CREATE_STANDARD | Service detail with pricing, lifecycle, history |
| /services/new | ServiceCreatePage | SERVICE_CREATE_STANDARD | Create service at standard price with customer/company selection |
| /services/{id}/renew | ServiceRenewPage | SERVICE_RENEW_STANDARD | Renew service at standard price |
| /service-types | ServiceTypeCatalogPage | SERVICE_TYPE_MANAGE | Admin: manage service type catalog |

### Route / Navigation Planning

- Routes added to App.tsx under the ProtectedRoute wrapper, following existing pattern.
- Navigation items added to AuthenticatedShell.tsx with hasPermission gating.
- Service menu items grouped under a "Services" section, similar to "Customers" section.

### Sanitized Error Handling

- serviceErrorMessages.ts following customerMergeErrorMessages.ts pattern.
- Map backend error details to user-facing Vietnamese messages.
- No raw SQL, stack traces, or internal exception details exposed.

### Frontend API Client

- serviceApi.ts following customerMergeApi.ts pattern.
- Functions: listServiceTypes, getServiceType, listServices, getService, createService, renewService, requestPriceOverride.
- Uses axiosClient with /api/v2 base URL.

### Frontend Test Plan

- ServiceListPage.test.tsx: renders, empty state, error state, service list display.
- ServiceDetailPage.test.tsx: renders, loading, error, detail display, lifecycle info.
- ServiceCreatePage.test.tsx: form renders, validation, submit, error handling.
- serviceApi.test.ts: API client function tests.
- serviceErrorMessages.test.ts: error mapping tests.

## Test Strategy

Plan only.

### Backend Unit Tests

- ServiceType validation logic.
- Service creation validation (Customer_Company_Context existence, price snapshot).
- Service renewal validation (price comparison, cycle increment).
- Price override request validation.
- ServicePriceOverrideExecutionHandler logic.

### Backend Integration Tests

- V0011/U0011 migration rollback tests (MigrationRollbackTests).
- Service entity persistence via EF.
- ServiceType CRUD via EF.
- Service lifecycle transitions.
- ServiceHistory append-only behavior.

### Backend API Tests

- Service type catalog endpoints (GET list, GET detail).
- Service CRUD endpoints with permission enforcement.
- Service renewal endpoint with standard price validation.
- Price override request endpoint with workflow creation.
- Company scope enforcement (cannot access services in unauthorized company).
- Concurrency (409 Conflict) on service operations.
- Error sanitization (no raw SQL/exceptions in responses).

### Migration Rollback Tests

- V0011 forward: tables created, permissions seeded.
- U0011 rollback: tables dropped, permissions deactivated.
- SchemaVersions record management.

### Frontend Tests

- Component tests (list, detail, create, renew pages).
- API client tests.
- Error message mapping tests.
- Permission-gated navigation tests.

## Dependencies

### Payment / Billing / Collection / Reconciliation
- **Dependency direction**: Payment depends on Service (not reverse).
- **PAY-008**: Payment correction must preserve service-cycle consistency. Service entity (with CycleNumber) must exist first.
- **Phase 1B.6 responsibility**: establish Service entity schema that Payment can reference via FK. Define CycleNumber for PAY-008 enforcement.
- **Not in Phase 1B.6**: payment tables, billing logic, reconciliation.

### Card Reprint
- **Dependency direction**: Card Reprint depends on Service (cards are service artifacts).
- **Phase 1B.6 responsibility**: establish Service entity that Card Reprint can reference.
- **Not in Phase 1B.6**: card entity, print tracking, CARD_REPRINT workflow.

### Care Package Sales / Renewal
- **Dependency direction**: SELL_CARE_PACKAGE depends on service-sales module specification.
- **Phase 1B.6 responsibility**: none. SELL_CARE_PACKAGE is RESERVED / INACTIVE. Must not be activated.

### Customer Master
- **Dependency direction**: Service depends on Customer Master (customer must exist to receive services).
- **Linkage**: Service → (CustomerId, CompanyId) → Customer_Company_Context must exist.
- **Complete**: Customer Master foundation is closed (Phases 1B.2, 1B.4, 1B.5).

### Workflow / Approval
- **Dependency direction**: SERVICE_PRICE_OVERRIDE uses existing workflow foundation.
- **Reuse**: execution handler pattern, binding resolution, approval runtime.
- **Complete**: Workflow foundation is closed (Phase 1B.3).

### Permission / Security
- **Dependency direction**: Service module uses existing permission system.
- **Service permissions already cataloged**: SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE.
- **Complete**: Security foundation is closed (Phase 1B.1).

### Reporting
- **Not in Phase 1B.6**: reporting depends on Payment which depends on Service. Reporting is downstream.

## Open Decisions / Blockers

### OD-1B6-001: Service Type Taxonomy
- **Question**: What service types exist? (e.g. burial care annual, maintenance one-time, etc.)
- **Status**: Not defined in repository documents. business-rules.md and process-catalog.md reference services generically without listing specific types.
- **Blocker**: Yes, for schema design. Implementation needs at least one concrete service type to validate the model.
- **Recommended default**: Design the ServiceType entity to be catalog-driven (admin-manageable). Seed 1-2 example types for testing. Exact production types can be configured by ADMIN_SERVICE_DATA after deployment.
- **Owner**: Requires PO decision.

### OD-1B6-002: Service Lifecycle Statuses
- **Question**: What statuses does a service go through? (DRAFT → ACTIVE → EXPIRED → CANCELLED? Or simpler?)
- **Status**: Not explicitly defined. RENEW_SERVICE_STANDARD implies an active/expired cycle. PENDING_APPROVAL may be needed for override flow.
- **Blocker**: Yes, for schema design.
- **Recommended default**: ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE. DRAFT only if the business requires a draft/confirm flow for service creation.
- **Owner**: Requires PO decision.

### OD-1B6-003: Renewal Model
- **Question**: Does renewal create a new Service row (linked via PreviousServiceId) or update the existing Service with new validity dates?
- **Status**: Not specified in repository documents. PAY-008 references "service-cycle" which implies distinct cycles.
- **Blocker**: Non-blocker for schema design (can be designed to support both).
- **Recommended default**: New Service row per renewal cycle (linked via PreviousServiceId). This supports PAY-008 service-cycle consistency (each cycle is a distinct entity that payment items reference). The prior service row transitions to EXPIRED.
- **Owner**: Requires PO decision.

### OD-1B6-004: Standard Price Scope
- **Question**: Is the standard price global per ServiceType, or can it vary per company?
- **Status**: process-catalog.md SERVICE_PRICE_OVERRIDE condition fields include company_id and standard_price, suggesting the standard price may vary or at least be evaluated per company context.
- **Blocker**: Non-blocker (can design for per-type with company override layer later).
- **Recommended default**: Standard price on ServiceType entity (global per type). If per-company pricing is needed later, a ServiceTypeCompanyPrice override table can be added. Phase 1B.6 implements global-per-type only.
- **Owner**: Requires PO decision.

### OD-1B6-005: Whether Service Sale Belongs in Phase 1B.6 or Later
- **Question**: Should Phase 1B.6 include the full service creation/renewal flow, or only the catalog and schema?
- **Status**: Post-1B.5 recommendation and PO decision both reference "Service Module Foundation" including "Service Catalog, Standard Pricing, Service Sales."
- **Blocker**: No. The PO decision authorizes discovery to determine scope.
- **Recommended default**: Include service creation (standard) and renewal (standard) in Phase 1B.6 alongside the catalog. SERVICE_PRICE_OVERRIDE workflow integration is also in scope. This provides a complete foundation for Payment.
- **Owner**: Requires PO decision (via scope acceptance).

### OD-1B6-006: SERVICE_VIEW Permission
- **Question**: Is a separate SERVICE_VIEW permission needed, or is read access implied by SERVICE_CREATE_STANDARD?
- **Status**: permission-catalog.md does not list a SERVICE_VIEW permission. CUSTOMER module has CUSTOMER_VIEW_BASIC as a separate read permission.
- **Blocker**: Non-blocker.
- **Recommended default**: Add SERVICE_VIEW (COMPANY) for consistency with CUSTOMER_VIEW_BASIC pattern. This allows read-only access for users who should see services but not create/renew them.
- **Owner**: Requires PO decision.

### OD-1B6-007: Service-to-Customer Linkage
- **Question**: Does Service reference CustomerId + CompanyId directly, or reference CustomerCompanyContextId?
- **Status**: DATA-002 defines Customer_Company_Context as (customer_id, company_id). Services are company-scoped (DATA-003).
- **Blocker**: Non-blocker.
- **Recommended default**: Service references CustomerId + CompanyId (two FKs). This is simpler and matches the Customers/Companies FK pattern used elsewhere. A check constraint or application validation ensures the Customer_Company_Context record exists.
- **Owner**: Requires PO decision.

### OD-1B6-008: Service Type Admin Endpoints
- **Question**: Should Phase 1B.6 include admin endpoints for managing the service type catalog, or is the catalog seed-only?
- **Status**: ADMIN_SERVICE_DATA admin group exists. GOV-002 states "Admin may not create a new business process" but service types are not business processes — they are catalog data.
- **Blocker**: Non-blocker.
- **Recommended default**: Include basic ServiceType admin endpoints (list, create, update, deactivate) gated by SERVICE_TYPE_MANAGE permission, accessible to ADMIN_SERVICE_DATA group. This enables operational flexibility without code changes for new service types.
- **Owner**: Requires PO decision.

### OD-1B6-009: Migration Scope
- **Question**: Should V0011 include all service tables (ServiceType, ServicePriceHistory, Service, ServiceHistory) or split across multiple migrations?
- **Status**: V0010 created three tables in a single migration (Customer_Merge_Requests, Candidates, History).
- **Blocker**: Non-blocker.
- **Recommended default**: Single V0011 migration for all service schema, following V0010 precedent.
- **Owner**: Can be decided at implementation planning.

### OD-1B6-010: Frontend Screen Scope
- **Question**: Which frontend screens are in Phase 1B.6 scope?
- **Status**: The recommendation references "Service frontend (list, detail, renewal form)."
- **Blocker**: Non-blocker.
- **Recommended default**: Service list, service detail, service create, service renew, and service type catalog admin. Price override request triggers workflow (uses existing workflow UI for approval).
- **Owner**: Requires PO decision (via scope acceptance).

## Recommended Implementation Phases

Following repository conventions from Phase 1B.5:

1. **Phase 1B.6-A**: Project Owner scope acceptance of this discovery and detailed plan.

2. **Phase 1B.6-B**: Backend/data foundation.
   - Scope planning and PO acceptance.
   - Implementation: ServiceType, ServicePriceHistory, Service, ServiceHistory entities. V0011/U0011 migration. EF configurations. ServiceService (IServiceService). ServiceController. SERVICE_PRICE_OVERRIDE_FROM_APPROVAL execution handler. Permission seeding. Unit/integration/API tests.
   - Implementation review.
   - PO backend/data implementation acceptance.

3. **Phase 1B.6-C**: Frontend implementation.
   - Scope planning and PO acceptance.
   - Implementation: serviceApi.ts, serviceTypes.ts, serviceErrorMessages.ts, ServiceListPage, ServiceDetailPage, ServiceCreatePage, ServiceRenewPage, ServiceTypeCatalogPage. Routes, navigation, permission gating. Frontend tests.
   - Implementation review.
   - PO frontend implementation acceptance.

4. **Phase 1B.6-D**: Operational validation and closure.
   - Validation plan and PO acceptance.
   - Validation execution: build, all test suites, manual/operational checklist.
   - Closure report.
   - Acceptance review.
   - PO closure acceptance.

## Risks

1. **Service/payment boundary ambiguity**: The exact interface between Service and future Payment entities needs clear definition. Service should include CycleNumber and pricing fields that Payment can reference, but the payment item → service FK design should be deferred to Payment phase.

2. **Pricing/versioning ambiguity**: Whether standard pricing is global-per-type or per-company is not confirmed. The recommended default (global-per-type) may need revision if business requirements demand per-company pricing. Designing ServicePriceHistory as a separate entity mitigates this risk.

3. **Workflow scope creep**: SERVICE_PRICE_OVERRIDE is the only workflow integration in scope. SELL_CARE_PACKAGE must not be activated. CARD_REPRINT is explicitly deferred.

4. **Overbuilding before payment requirements**: Service schema should be minimal and correct, not speculative. Fields should be added only when supported by documented business rules.

5. **Customer-service linkage ambiguity**: Whether Service references Customer_Company_Context directly or via (CustomerId, CompanyId) pair affects FK design. Recommendation is (CustomerId, CompanyId) for simplicity.

6. **Report/reconciliation dependency**: Service module does not include reporting. Future reporting depends on both Service and Payment existing.

7. **Migration rollback safety**: U0011 must safely drop all service tables and deactivate permissions without affecting existing V0010 schema. Table drop order must respect FK dependencies.

8. **Test DB reset target update**: TestDatabaseFixture must add ResetToV0011() and SafeTestWebApplicationFactory must call it. This is a mechanical change following established pattern.

9. **Permission catalog drift**: Four service permissions already exist in permission-catalog.md. If additional permissions are proposed (SERVICE_VIEW, SERVICE_MANAGE, SERVICE_TYPE_MANAGE), they must be accepted before seeding.

## Recommended Next Gate

Project Owner acceptance of this Phase 1B.6 discovery and detailed plan.

## Recommended Authorization Wording

If the plan is accepted:

Authorized next task:
Phase 1B.6 Service Module Foundation backend/data scope and implementation planning only.

Implementation requires separate Project Owner backend/data scope acceptance.

Do not authorize:
- implementation,
- database migration,
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
