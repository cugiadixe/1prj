# Phase 1B.9 Project Owner Closure Acceptance — Care Package Sales

## Status

ACCEPTED WITH DEPLOYMENT READINESS NOTES — PHASE 1B.9 CARE PACKAGE SALES CLOSED

## Project Owner Decision

The Project Owner accepts closure of Phase 1B.9 Care Package Sales with deployment readiness notes.

This acceptance is based on the Phase 1B.9 closure review and all accepted Phase 1B.9 gates.

All implementation and validation gates for Phase 1B.9 are accepted.

This acceptance closes Phase 1B.9 for project delivery tracking.

This acceptance does not claim production readiness.

This acceptance does not authorize production migration, release tag, or push.

This acceptance authorizes only the next planning/recommendation task:
Post-Phase 1B.9 next-work recommendation.

## Accepted Closure Review

Reference:

- Phase 1B.9 closure review commit:
  68c09f2a747f7e97809012af87db3fab072db0f4

- Phase 1B.9-D Project Owner operational validation acceptance commit:
  b4fd5c07b1dcd0c490ae3b1e925cd48606c3059d

- Phase 1B.9-D operational validation report commit:
  022857b7e0017708deb961c1c65a0af27bb66b9c

- Phase 1B.9-C frontend implementation commit:
  aae57bd1dd3479f757e1a8173061bce5616f5190

- Phase 1B.9-B2 workflow/payment implementation commit:
  fd58d92391ece74be9680a8c8aa8504c6c5e2c0a

- Phase 1B.9-B1 backend/data implementation commit:
  c28e7d5b65ac902f80a51c92121352e5ec1fc70c

## Accepted Business Scope

Care Package Sales / Gói chăm sóc:

- Sale unit: cốt-year (one cốt, one year).
- One-year packages only.
- Pricing from Service Foundation effective-date pricing.
- Price formula: unit price per cốt per year × cốt count × 1 year.
- Request total: subtotal minus approved discount.
- Backend-calculated pricing snapshots preserved and immutable.
- Sale date determines applied effective-date price.
- Historical records not rewritten by later price changes.
- Renewal creates a new one-year request for the next service period.
- Duplicate active/paid overlapping care packages for same care target prevented.
- Discount: VND amount only, requires reason, cannot reduce total below zero.
- Approval required for: discount, price override, changed-price renewal, or configured approval-required rule.
- No approval required for configured-price request with no discount.
- SELL_CARE_PACKAGE workflow used when approval is required.
- Payment only when payment-eligible.
- Payment Foundation constraints: VND only, full payment only, no partial payment, no refund, no cancellation, one bill cannot be paid multiple times.
- Frontend relies on backend-calculated totals and status.
- No hard-coded care package price.

## Accepted Gate Completion

- Phase 1B.9 scope: accepted.
- Phase 1B.9 detailed scope: accepted.
- Phase 1B.9 implementation plan: accepted.
- Phase 1B.9-B1 backend/data: accepted.
- Phase 1B.9-B2 workflow/payment: accepted.
- Phase 1B.9-C frontend: accepted.
- Phase 1B.9-D operational validation: accepted with deployment readiness notes.
- Phase 1B.9 closure review: completed.

## Accepted Delivered Scope

### B1 Backend/Data
- V0014/U0014 migration/rollback.
- CarePackageRequest and CarePackageRequestItem domain entities.
- EF Core configurations, AppDbContext integration.
- DTOs, service, controller with /api/v2/care-packages list/detail/create.
- Backend-calculated pricing snapshot foundation.
- Company-scope authorization.
- Permission constants: CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE.
- Backend unit tests, API tests, integration tests.

### B2 Workflow/Payment
- SELL_CARE_PACKAGE workflow integration.
- Approval-required and no-approval paths.
- Domain state synchronization via CarePackageExecutionHandler.
- Payment eligibility guard, duplicate payment guard.
- Create-payment delegation to IPaymentTransactionService.
- Payment-status read-only endpoint, active-status transition.
- Permission constants: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT.

### C Frontend
- Routes: /care-packages, /care-packages/new, /care-packages/:id.
- care-packages module: types, API client (9 functions), hooks (9 hooks), error messages.
- List, create, detail pages with permission-gated lifecycle actions.
- Safe error handling: 400/403/404/409.
- 19 frontend tests across 3 test files.

### D Operational Validation
- Automated backend/frontend/repository validation passed.
- Manual API/UI/lifecycle validation not executed (environment unavailable), covered by automated tests.

## Accepted Validation Evidence

- Backend build: 0 errors, 9 pre-existing warnings.
- UnitTests: 236/236 passed.
- IntegrationTests: 203/203 passed.
- ApiTests: 308/308 passed.
- Frontend lint: passed (pre-existing auth/ warnings only).
- Frontend build: succeeded.
- Full Vitest: 71/71 files, 500/500 tests passed.
- Targeted care-packages: 3/3 files, 19/19 tests passed.
- Repository: clean, git diff --check clean, no production migration/tag/push.
- Manual validation limitation: live API/UI/lifecycle validation not executed due to environment unavailability; automated tests covered relevant paths; accepted by PO.

## Deployment Readiness Notes Carried Forward

The Project Owner accepts the following deployment readiness blockers as carried-forward items:

1. SQL permission seed alignment for Care Package permissions.
2. Runtime permission rows for:
   - CARE_PACKAGE_VIEW
   - CARE_PACKAGE_CREATE
   - CARE_PACKAGE_APPROVE
   - CARE_PACKAGE_REJECT
   - CARE_PACKAGE_CREATE_PAYMENT
3. SELL_CARE_PACKAGE workflow runtime configuration.

These items do not block Phase 1B.9 project closure.

These items block any claim of production/deployment readiness until resolved or separately accepted.

## Non-Blocking Follow-Ups Carried Forward

The Project Owner accepts the following non-blocking follow-ups:

- Manual ID selector UX for customer/grave.
- Stale frontend status / backend 409 safe handling follow-up.
- Care target selector/search UX improvement.
- Live manual API/UI/lifecycle validation in a suitable environment before deployment readiness.
- No report/export UI in Phase 1B.9.
- No generic Payment Print UI in Phase 1B.9.
- No dynamic PDF/template generation in Phase 1B.9.

## Boundary Confirmation

- No production migration was performed.
- No release tag was created.
- No push was performed.
- Production/deployment readiness is not claimed.
- Refund is not implemented.
- Cancellation is not implemented.
- Partial payment is not implemented.
- Dynamic PDF/template generation is not implemented.
- Generic Payment Print UI is not implemented.
- Dedicated report/export UI is not implemented.
- Physical inventory/stamp stock management is not implemented.
- Multi-year packages are not implemented.
- Partial-year packages are not implemented.
- Discount percent UI is not implemented.
- Permission catalog was not changed in implementation slices.

## Authorization for Next Step

Authorized next task:
Post-Phase 1B.9 next-work recommendation only.

The next task may create only a next-work recommendation document.

The next task must produce:

docs/architecture/post-phase-1b9-next-work-recommendation.md

The next task must:
- Review current project status after Phase 1B.9 closure.
- Identify candidate next work items from accepted backlog/specification only.
- Carry forward deployment readiness notes from Phase 1B.9.
- Recommend the next authorized phase/slice.
- Avoid inventing business requirements.
- Avoid modifying source code.
- Avoid production migration, tag, or push.

The next task must not:
- Implement code.
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.

## Non-Goals

This acceptance task does not:
- Implement code.
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.
- Claim production readiness.

## Notes

- Phase 1B.9 Care Package Sales is closed for project delivery tracking.
- Deployment readiness blockers remain carried forward.
- Post-phase next-work recommendation has not started.
- Local branch may be ahead of origin; no push is authorized.
- Production migration and release tagging require separate explicit authorization.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
