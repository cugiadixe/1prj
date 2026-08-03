# Phase 1B.6 Project Owner Scope Acceptance

## Status

ACCEPTED — PHASE 1B.6 SERVICE MODULE FOUNDATION SCOPE APPROVED FOR BACKEND/DATA PLANNING

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md

Planning commit:
fe0d7ebe16bcf5218d6eb7fe63369288ff0991b8

## Accepted Scope Direction

The Project Owner accepts the Phase 1B.6 Service Module Foundation direction as a foundation for later Payment, Card Reprint, and Care Package Sales work.

Accepted scope direction includes:

- ServiceType catalog foundation.
- Service entity foundation.
- Service lifecycle/status model.
- Standard service price snapshot strategy.
- Service creation and renewal foundation for standard services.
- SERVICE_PRICE_OVERRIDE workflow integration planning.
- V0011/U0011 migration planning.
- API v2 planning.
- Frontend planning.
- Test planning.
- Permission planning.

## Accepted Out-of-Scope Items

The following remain out of scope for Phase 1B.6 implementation unless separately approved:

- Full Payment implementation.
- Billing/collection/reconciliation implementation.
- Card Reprint implementation.
- SELL_CARE_PACKAGE implementation (RESERVED / INACTIVE — must not be activated without functional specification).
- Production payment workflow.
- Service-to-Plot/Location linkage (deferred until Plot entity domain exists).
- Reporting/reconciliation.
- Production migration.
- Release tag.
- Push.

## Open Decisions Accepted for Carry-Forward

The Project Owner acknowledges the 10 open decisions documented in the plan:

- OD-1B6-001: Service Type Taxonomy.
- OD-1B6-002: Service Lifecycle Statuses.
- OD-1B6-003: Renewal Model.
- OD-1B6-004: Standard Price Scope.
- OD-1B6-005: Whether Service Sale Belongs in Phase 1B.6 or Later.
- OD-1B6-006: SERVICE_VIEW Permission.
- OD-1B6-007: Service-to-Customer Linkage.
- OD-1B6-008: Service Type Admin Endpoints.
- OD-1B6-009: Migration Scope.
- OD-1B6-010: Frontend Screen Scope.

These decisions must be carried into the next backend/data scope planning task and resolved or explicitly deferred before implementation authorization.

## Boundaries

- This acceptance authorizes backend/data scope and implementation planning only.
- Source code changes are not authorized.
- Test changes are not authorized.
- Frontend/backend implementation changes are not authorized.
- Database migration files are not authorized.
- Rollback files are not authorized.
- Business rule changes are not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Project Owner Decision

The Project Owner accepts the Phase 1B.6 Service Module Foundation discovery and detailed plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.6-B Service Module Foundation backend/data scope and implementation planning only.

Implementation requires separate Project Owner backend/data scope acceptance.

Do not authorize:
- implementation,
- database migration,
- rollback creation,
- frontend implementation,
- production migration,
- release tag,
- push.
