# Phase 1B.6-B Project Owner Backend/Data Scope Acceptance

## Status

ACCEPTED — PHASE 1B.6-B SERVICE MODULE FOUNDATION BACKEND/DATA SCOPE APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b6b-backend-data-foundation-scope-and-implementation-plan.md

Planning commit:
4a58faf05af47cffba651c148789ac9f95dc4d49

## Accepted Backend/Data Scope

The Project Owner accepts the backend/data scope defined in the plan, including:

- ServiceType catalog foundation.
- Service entity foundation.
- Service lifecycle/status model.
- Standard service price snapshot strategy.
- Service creation and renewal foundation for standard services.
- SERVICE_PRICE_OVERRIDE workflow integration boundary.
- Company/customer linkage.
- Rowversion/concurrency handling.
- Audit/created/updated fields.
- API v2 backend scope.
- Backend permission scope.
- Backend validation rules.
- Backend tests.
- V0011 migration creation during implementation.
- U0011 rollback creation during implementation.
- TestDatabaseFixture reset target update to V0011 during implementation.
- SafeTestWebApplicationFactory reset target update to V0011 during implementation.
- MigrationRollbackTests extension for V0011/U0011.

## Accepted Proposed Migration / Rollback Scope

The Project Owner accepts:

- Expected migration number: V0011.
- Expected rollback number: U0011.
- DbMigrator remains owner of SchemaVersions.
- Rollback must be safe and ordered.
- Permission rollback must soft-deactivate protected permission rows where required.
- No production migration is authorized.

## Accepted API v2 Scope

The Project Owner accepts the planned backend API v2 scope from the plan, including:

- Service catalog query/read endpoints.
- Service detail endpoint.
- Service create endpoint within accepted standard-service scope.
- Service renewal endpoint within accepted standard-service scope.
- Service lifecycle/status handling within accepted scope.
- Service price snapshot/lookup behavior.
- Workflow submission boundary for SERVICE_PRICE_OVERRIDE.
- Permission checks.
- Sanitized validation/error mapping.
- Concurrency handling.

The implementation must not expand into Payment, Card Reprint, or Care Package Sales.

## Accepted Permission Scope

The Project Owner accepts permission planning from the backend/data plan as implementation scope, subject to exact permission codes in the plan.

- Permission additions must follow existing permission model and seed conventions.
- Rollback behavior must avoid hard-deleting protected permission rows where repository constraints require soft-deactivation.
- permission-catalog.md must not be modified unless separately authorized.

## Accepted Open Decision Handling

The Project Owner accepts the plan's handling of OD-1B6-001 through OD-1B6-010:

| Decision | Summary | Plan Position | Status |
|---|---|---|---|
| OD-1B6-001 | Service Type Taxonomy | Admin-manageable catalog entity, seed example types for testing | Resolved for 1B.6-B implementation |
| OD-1B6-002 | Service Lifecycle Statuses | ACTIVE, EXPIRED, CANCELLED, PENDING_PRICE_OVERRIDE | Resolved for 1B.6-B implementation |
| OD-1B6-003 | Renewal Model | New Service row per renewal cycle, linked via PreviousServiceId | Resolved for 1B.6-B implementation |
| OD-1B6-004 | Standard Price Scope | Global per ServiceType, per-company deferred | Deferred with safe default — no blocker |
| OD-1B6-005 | Service Sale in 1B.6 | Standard creation and renewal included | Resolved for 1B.6-B implementation |
| OD-1B6-006 | SERVICE_VIEW Permission | Add SERVICE_VIEW (COMPANY) following CUSTOMER_VIEW_BASIC pattern | Resolved for 1B.6-B implementation |
| OD-1B6-007 | Service-to-Customer Linkage | CustomerId + CompanyId FKs, application validates Customer_Company_Context | Resolved for 1B.6-B implementation |
| OD-1B6-008 | Service Type Admin Endpoints | Include create/update/deactivate gated by SERVICE_TYPE_MANAGE | Resolved for 1B.6-B implementation |
| OD-1B6-009 | Migration Scope | Single V0011 for all service tables + permissions, single U0011 | Resolved for 1B.6-B implementation |
| OD-1B6-010 | Frontend Screen Scope | Deferred to Phase 1B.6-C frontend planning | Deferred — no blocker |

Any blocker marked as requiring PO confirmation must be resolved before implementation expands beyond the accepted scope.

## Accepted Out-of-Scope Items

The following are not authorized in Phase 1B.6-B implementation:

- Frontend implementation.
- Full Payment implementation.
- Billing/collection/reconciliation implementation.
- Card Reprint implementation.
- SELL_CARE_PACKAGE implementation.
- Production payment workflow.
- Production migration.
- Release tag.
- Push.

## Implementation Boundaries

- Backend/data implementation is authorized only after this acceptance commit.
- V0011/U0011 creation is authorized only as part of the next backend/data implementation task.
- Frontend implementation is not authorized.
- Payment implementation is not authorized.
- Card Reprint implementation is not authorized.
- Care Package Sales implementation is not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Required Implementation Evidence

Future implementation must provide:

- V0011 migration.
- U0011 rollback.
- Domain entities/configurations.
- DbContext/interface updates.
- Permission seed updates.
- Application services/DTOs/handlers.
- API controller.
- Unit tests.
- Integration tests.
- API tests.
- Migration rollback tests.
- Fixture reset target update to V0011.
- Implementation report.
- Backend build result.
- UnitTests result.
- IntegrationTests result.
- ApiTests result.
- git diff --check result.
- Confirmation no frontend implementation.
- Confirmation no production migration/tag/push.

## Project Owner Decision

The Project Owner accepts the Phase 1B.6-B Service Module Foundation backend/data scope and implementation plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.6-B Service Module Foundation backend/data implementation only.

The implementation may create backend/data source files, backend tests, V0011 migration, and U0011 rollback only within the accepted scope.

Do not authorize:
- frontend implementation,
- Payment implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.
