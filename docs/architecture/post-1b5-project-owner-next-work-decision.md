# Post-Phase 1B.5 Project Owner Next-Work Decision

## Status

ACCEPTED — PHASE 1B.6 SERVICE MODULE FOUNDATION SELECTED FOR DISCOVERY AND DETAILED PLANNING

## Decision

The Project Owner selects Phase 1B.6 Service Module Foundation as the next work item after Phase 1B.5 Customer Merge and Duplicate Resolution.

## Accepted Recommendation

Reference:

docs/architecture/post-1b5-next-work-selection-discovery-and-recommendation.md

- Recommendation commit:
  dbdb3fa5fcd1d029c2baaa204cb682db88b1c5aa

- Parent Phase 1B.5 closure acceptance commit:
  22040fb2767ebbb1882c061b212767a257490dc0

## Rationale

- Service foundation is a prerequisite for Payment. PAY-008 requires service-cycle consistency — payment correction must not pay the same cycle twice, which implies service entities must exist before payment can be fully implemented.
- Card Reprint depends on service definitions. Cards are printed for services; CARD_REPRINT cannot be built without the service domain.
- Care Package Sales (SELL_CARE_PACKAGE) depends on service lifecycle. It is currently RESERVED/INACTIVE and must not be activated without a functional specification.
- Customer Master (Phase 1B.2/1B.4), Workflow/Approval (Phase 1B.3), Security (Phase 1B.1), and Customer Merge (Phase 1B.5) foundations are now complete and provide the necessary base for service module discovery.
- Building service module first reduces downstream ambiguity before billing/payment implementation. The dependency order Services then Payment is clearer than Payment then Services.

## Selected Next Work

Selected phase:

Phase 1B.6 Service Module Foundation

Initial authorized task:

Phase 1B.6 discovery and detailed planning only.

## Expected Discovery / Planning Scope

Discovery and detailed planning is authorized for:

- Service catalog boundaries (service types, categories, lifecycle states).
- Service lifecycle (creation, activation, renewal, suspension, termination).
- Service pricing and versioning dependency analysis (standard pricing snapshots, override triggers).
- Service sale and request boundaries (RENEW_SERVICE_STANDARD, SERVICE_PRICE_OVERRIDE).
- Customer-to-service linkage (service-to-Customer_Company_Context relationship).
- Approval workflow needs for service operations (SERVICE_PRICE_OVERRIDE conditional approval).
- Dependencies for Payment, Card Reprint, and Care Package Sales.
- Required API v2 scope (service endpoints).
- Required SQL Server schema and migration strategy (V0011/U0011).
- Required frontend scope (service pages, forms, lists).
- Required test strategy (unit, integration, API, frontend tests).
- Blockers and open decisions.

Implementation is not authorized in this decision.

## Alternatives Considered

1. **Payment / Billing / Collection / Reconciliation**: Not selected first because PAY-008 requires service-cycle consistency. Building payment before services would require placeholder service references or rework when services are added. Payment is the expected next candidate after services.

2. **Card Reprint Approval Flow**: Not selected first because cards are service artifacts. CARD_REPRINT depends on the service domain existing.

3. **Care Package Sales**: Not selected first because SELL_CARE_PACKAGE is RESERVED/INACTIVE in process-catalog.md. It requires a separate functional module specification before activation and depends on service lifecycle.

4. **Change Owner (Plot Ownership Transfer)**: Not selected first because it introduces a new entity domain (plots/sites/zones/blocks) that is not a prerequisite for services or payments. Lower business urgency.

5. **Import Rollback**: Not selected first because import infrastructure is not yet defined. Lower priority than transactional modules.

6. **Sensitive Export**: Not selected first because export controls are more meaningful after transactional modules (services, payments) exist.

7. **Production Release Readiness**: Not selected because go-live is blocked until PAY criteria pass per acceptance-criteria.md. Insufficient functional scope without services and payments.

These alternatives are not selected first because Service Module Foundation is the prerequisite or lower-risk foundation for them, according to the recommendation.

## Boundaries

- This decision authorizes discovery and detailed planning only.
- Source code changes are not authorized.
- Test changes are not authorized.
- Frontend/backend implementation changes are not authorized.
- Database migrations are not authorized.
- Rollbacks are not authorized.
- Business rule changes are not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Project Owner Decision

The Project Owner accepts the recommendation to proceed with Phase 1B.6 Service Module Foundation.

## Authorization for Next Step

Authorized next task:
Phase 1B.6 Service Module Foundation discovery and detailed planning only.

Implementation requires separate Project Owner scope acceptance.

Do not authorize:
- implementation,
- database migration,
- production migration,
- release tag,
- push.
