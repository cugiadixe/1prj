# Phase 1B.6-C Project Owner Frontend Scope Acceptance

## Status

ACCEPTED — PHASE 1B.6-C SERVICE MODULE FOUNDATION FRONTEND SCOPE APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b6c-frontend-scope-and-implementation-plan.md

Planning commit:
d9e547f77c3af6c5b476f74a77708ae41ad842d4

## Accepted Frontend Scope

The Project Owner accepts the frontend scope defined in the plan, including:

- Service Module frontend API client.
- Service Module TypeScript types.
- frontend error message mapping.
- service type API module.
- service API module.
- service type catalog/list UI.
- service type detail UI.
- service type create/edit UI if defined by the plan and supported by backend.
- service list UI.
- service detail UI.
- standard service create UI.
- standard service renewal UI.
- price snapshot display.
- lifecycle/status display.
- SERVICE_PRICE_OVERRIDE workflow boundary display/submission if defined by the accepted backend contract.
- App route wiring.
- AuthenticatedShell navigation wiring.
- permission-gated UI.
- sanitized frontend error handling.
- frontend tests.

## Accepted API Client / Type Scope

The Project Owner accepts API client and type mapping for the actual accepted backend API v2 contract.

- frontend must map to existing backend endpoints only.
- frontend must not change backend API contracts.
- frontend must not invent endpoints.
- frontend must not expand into Payment, Card Reprint, or Care Package Sales.

## Accepted Route / Navigation Scope

The Project Owner accepts the route/navigation scope from the plan, including:

- planned service type routes.
- planned service routes.
- permission-gated navigation entries.
- direct URL fallback to backend authorization.
- no Payment/Card Reprint/Care Package navigation.

## Accepted Permission Scope

The Project Owner accepts frontend permission usage based on backend implementation:

- GLOBAL permission handling for service type management.
- COMPANY-scoped permission handling for services.
- frontend gating is convenience only.
- backend authorization remains authoritative.

Exact permission codes from the plan:
- SERVICE_TYPE_MANAGE
- SERVICE_VIEW
- SERVICE_CREATE_STANDARD
- SERVICE_RENEW_STANDARD
- SERVICE_PRICE_OVERRIDE_REQUEST
- SERVICE_PRICE_OVERRIDE_APPROVE

## Accepted Error Handling Scope

The Project Owner accepts sanitized frontend handling for:

- permission denied,
- not found,
- validation failure,
- stale rowversion/concurrency,
- inactive service type,
- invalid customer/company,
- invalid lifecycle transition,
- price override workflow required,
- generic server failure.

Confirmation:
- no raw SQL/internal exception display.
- no stack trace display.
- no raw sensitive payload exposure.

## Accepted Test Scope

The Project Owner accepts planned frontend tests, including:

- API client tests.
- service type page tests.
- service list/detail tests.
- service create/renew tests if in scope.
- permission-gated UI tests.
- route/navigation tests.
- error mapping tests.
- regression tests preventing raw internal error display.

## Accepted Open Questions / Risks

The Project Owner carries forward the plan’s open questions and risks, including:

- UX for SERVICE_PRICE_OVERRIDE workflow boundary.
- whether service type management UI is admin-only.
- whether service create/renew should be entry points from Customer pages.
- frontend display of standard price snapshots.
- lifecycle transition UI limitations.
- deferred Payment/Card Reprint/Care Package UI dependencies.
- operational browser validation remains future gate.

These are non-blocking for frontend implementation if implemented safely and documented.

## Accepted Out-of-Scope Items

The following are confirmed as not authorized in Phase 1B.6-C frontend implementation:

- backend changes.
- database migrations.
- rollbacks.
- Payment frontend.
- billing/collection/reconciliation frontend.
- Card Reprint frontend.
- SELL_CARE_PACKAGE / Care Package Sales frontend.
- production migration.
- release tag.
- push.

## Implementation Boundaries

- Frontend implementation is authorized only after this acceptance commit.
- Backend changes are not authorized.
- Database migration is not authorized.
- Rollback creation is not authorized.
- Business rule changes are not authorized.
- Payment implementation is not authorized.
- Card Reprint implementation is not authorized.
- Care Package Sales implementation is not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Required Implementation Evidence

Future frontend implementation must provide:

- frontend API client files.
- frontend TypeScript types.
- frontend error mapping.
- service type pages/components.
- service pages/components.
- route wiring.
- navigation wiring.
- permission-gated UI.
- sanitized error handling.
- frontend tests.
- implementation report.
- npm run lint result.
- npx tsc -b result.
- npm run test result.
- targeted Service Module frontend test result.
- git diff --check result.
- confirmation no backend changes.
- confirmation no migration/rollback changes.
- confirmation no Payment/Card Reprint/Care Package implementation.
- confirmation no production migration/tag/push.

## Project Owner Decision

The Project Owner accepts the Phase 1B.6-C Service Module Foundation frontend scope and implementation plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.6-C Service Module Foundation frontend implementation only.

Implementation must stay within the accepted frontend scope.

Do not authorize:
- backend changes,
- database migration,
- rollback creation,
- Payment implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.
