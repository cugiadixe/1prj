# Phase 1B.9 Care Package Sales Closure Review

## Status

READY FOR PROJECT OWNER CLOSURE ACCEPTANCE WITH DEPLOYMENT READINESS NOTES

## Review Target

Reference:

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

## Confirmed Business Scope

Phase 1B.9 Care Package Sales / Gói chăm sóc delivers:

- Sale unit: cốt-year (one cốt, one year).
- One-year packages only (no multi-year, no partial-year).
- Pricing from Service Foundation effective-date pricing.
- Price formula: unit price per cốt per year × cốt count × 1 year.
- Request total: subtotal minus approved discount.
- Backend-calculated pricing snapshots (unit price, cốt count, subtotal, discount, total) preserved and immutable after creation.
- Sale date determines applied effective-date price.
- Historical records not rewritten by later price changes.
- Renewal creates a new one-year request for the next service period.
- Duplicate active/paid overlapping care packages for same care target prevented.
- Discount: VND amount only, requires reason, cannot reduce total below zero.
- Approval required for: discount, price override, changed-price renewal, or configured approval-required rule.
- No approval required for configured-price request with no discount.
- SELL_CARE_PACKAGE workflow used when approval is required.
- Payment only when payment-eligible (approved or no-approval-required with valid price).
- Confirmed payment required before active status.
- Payment Foundation constraints: VND only, full payment only, no partial payment, no refund, no cancellation, one bill cannot be paid multiple times.
- Frontend relies on backend-calculated totals and status.
- No hard-coded care package price.

## Gate Completion Summary

### A — Scope and Detailed Scope

| Gate | Document | Status |
|------|----------|--------|
| Discovery/scope plan | phase-1b9-care-package-sales-discovery-and-scope-plan.md | Completed |
| PO scope acceptance | phase-1b9-project-owner-scope-acceptance.md | ACCEPTED |
| Open decisions / detailed scope | phase-1b9a-care-package-sales-open-decisions-and-detailed-scope.md | Completed |
| PO blocker decisions | phase-1b9a-project-owner-blocker-decision-response.md | Decided |
| Updated detailed scope | phase-1b9a-care-package-sales-updated-detailed-scope.md | Completed |
| PO detailed scope acceptance | phase-1b9a-project-owner-detailed-scope-acceptance.md | ACCEPTED |

### B — Implementation Planning

| Gate | Document | Status |
|------|----------|--------|
| Implementation plan | phase-1b9b-care-package-sales-implementation-plan.md | Completed |
| PO implementation plan acceptance | phase-1b9b-project-owner-implementation-plan-acceptance.md | ACCEPTED |

### B1 — Backend/Data Foundation

| Gate | Document | Status |
|------|----------|--------|
| Implementation report | phase-1b9b1-care-package-sales-backend-data-implementation-report.md | IMPLEMENTED |
| Acceptance review | phase-1b9b1-care-package-sales-backend-data-acceptance-review.md | PASSED WITH NOTES |
| PO backend/data acceptance | phase-1b9b1-project-owner-backend-data-acceptance.md | ACCEPTED |

### B2 — Workflow/Payment Integration

| Gate | Document | Status |
|------|----------|--------|
| Implementation report | phase-1b9b2-care-package-sales-workflow-payment-implementation-report.md | IMPLEMENTED |
| Acceptance review | phase-1b9b2-care-package-sales-workflow-payment-acceptance-review.md | PASSED WITH NOTES |
| PO workflow/payment acceptance | phase-1b9b2-project-owner-workflow-payment-acceptance.md | ACCEPTED |

### C — Frontend

| Gate | Document | Status |
|------|----------|--------|
| Frontend implementation plan | phase-1b9c-care-package-sales-frontend-implementation-plan.md | Completed |
| PO frontend plan acceptance | phase-1b9c-project-owner-frontend-plan-acceptance.md | ACCEPTED |
| Implementation report | phase-1b9c-care-package-sales-frontend-implementation-report.md | IMPLEMENTED |
| Acceptance review | phase-1b9c-care-package-sales-frontend-acceptance-review.md | PASSED WITH NOTES |
| PO frontend acceptance | phase-1b9c-project-owner-frontend-acceptance.md | ACCEPTED |

### D — Operational Validation

| Gate | Document | Status |
|------|----------|--------|
| Operational validation plan | phase-1b9d-care-package-sales-operational-validation-plan.md | PROPOSED |
| PO validation plan acceptance | phase-1b9d-project-owner-operational-validation-plan-acceptance.md | ACCEPTED |
| Operational validation report | phase-1b9d-care-package-sales-operational-validation-report.md | PASSED WITH DEPLOYMENT READINESS NOTES |
| PO validation acceptance | phase-1b9d-project-owner-operational-validation-acceptance.md | ACCEPTED WITH DEPLOYMENT READINESS NOTES |

All 1B.9 gates are accepted.

## Accepted B1 Backend/Data Scope

- V0014/U0014 Care Package Sales foundation migration/rollback.
- CarePackageRequest and CarePackageRequestItem domain entities.
- EF Core configurations with snake_case mappings.
- AppDbContext integration.
- CarePackageRequestDto and CarePackageRequestItemDto.
- CreateCarePackageRequest and CreateCarePackageRequestItem DTOs.
- ICarePackageRequestService and CarePackageRequestService.
- CarePackageRequestsController with /api/v2/care-packages list/detail/create endpoints.
- Backend-calculated pricing snapshot foundation via Service Foundation effective-date pricing.
- Company-scope authorization via RequirePermission with X-Company-Id.
- Permission constants: CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE.
- Backend unit tests, API tests, integration tests.

## Accepted B2 Workflow/Payment Scope

- SELL_CARE_PACKAGE workflow integration.
- Approval-required path: submit initiates workflow, PendingApproval, approve/reject via WorkflowRuntimeService.
- No-approval path: configured-price/no-discount requests skip to PaymentEligible.
- Domain state synchronization via CarePackageExecutionHandler after successful workflow action.
- Rejected requests blocked from payment.
- Payment eligibility guard before payment draft creation.
- Create-payment delegates to IPaymentTransactionService.
- Duplicate payment blocked when pending/paid transaction exists.
- Payment-status endpoint read-only.
- Active-status transition after confirmed payment.
- Payment Foundation constraints enforced.
- Permission constants: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT.

## Accepted C Frontend Scope

- Routes: /care-packages, /care-packages/new, /care-packages/:id.
- care-packages module: types.ts, carePackageApi.ts, hooks.ts, errorMessages.ts.
- 9 API client functions, 9 React Query hooks.
- List page: table, status filter, permission-gated create button, row navigation.
- Create page: form with customer/service/sale date/grave/cot count/service period/discount, backend-calculated response.
- Detail page: summary, line items, pricing snapshots, workflow/payment status, lifecycle action buttons, approve/reject/payment modals.
- Permission-gated UI: CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT (COMPANY scope, UX-only).
- Safe error handling: 400/403/404/409.
- 19 frontend tests across 3 test files.

## Accepted D Operational Validation Result

- Validation status: PASSED WITH DEPLOYMENT READINESS NOTES.
- All automated backend validation passed.
- All automated frontend validation passed.
- Repository validation passed.
- Manual API/UI/lifecycle validation was NOT EXECUTED due to environment unavailability.
- Automated tests covered relevant paths.
- PO accepted validation with deployment readiness notes.

## Validation Evidence Summary

### Backend
- Build: 0 errors, 9 pre-existing warnings.
- UnitTests: 236/236 passed.
- IntegrationTests: 203/203 passed.
- ApiTests: 308/308 passed.

### Frontend
- Lint: passed (only pre-existing auth/ warnings).
- Build: succeeded (3275 modules transformed).
- Full Vitest: 71/71 files, 500/500 tests passed.
- Targeted care-packages: 3/3 files, 19/19 tests passed.

### Repository
- Clean tracked working tree at validation.
- git diff --check: clean.
- No production migration.
- No tag.
- No push.

## Manual Validation Limitation

Live manual API validation, live manual UI validation, and live workflow/payment lifecycle validation were not executed because the required live environment (running server, database, authenticated session, browser) was not available during validation.

All manual validation items are demonstrably covered by automated test suites (236 UnitTests + 203 IntegrationTests + 308 ApiTests + 500 frontend tests including 19 targeted care-packages tests).

This limitation was accepted by the Project Owner in the D operational validation acceptance. It remains visible here and does not authorize production deployment without live environment validation.

## Deployment Readiness Blockers Carried Forward

The following items block production/deployment readiness but do not block phase closure if accepted by the Project Owner:

1. **SQL permission seed alignment** — All 5 care package permission codes (CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT) exist as code constants. Database permission seed rows must be confirmed or added before runtime permission gating functions in production.

2. **Runtime permission rows** — All 5 permission codes must be grantable to users/roles at runtime. Depends on SQL permission seed alignment.

3. **SELL_CARE_PACKAGE workflow runtime configuration** — Workflow process configuration must be administratively established via workflow admin UI before approval-required path operations (submit/approve/reject) function at runtime. The no-approval path does not require workflow configuration.

These blockers must be resolved or separately accepted before any claim of production/deployment readiness.

## Non-Blocking Follow-Ups Carried Forward

- Manual ID selector UX for customer/grave (manual numeric input; searchable selector may improve UX in a future slice).
- Stale frontend status / backend 409 safe handling (frontend displays safe error message; behavior is correct but may confuse users in concurrent editing scenarios).
- Care target selector/search UX improvement.
- Live manual API/UI/lifecycle validation should be performed in a suitable environment before deployment readiness.
- No report/export UI in 1B.9 scope.
- No generic Payment Print UI in 1B.9 scope.
- No dynamic PDF/template generation in 1B.9 scope.

These follow-ups do not block phase closure.

## Boundary / Non-Goal Confirmation

- No production migration was performed.
- No release tag was created.
- No push was performed.
- No production/deployment readiness is claimed.
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
- Unrelated business docs were not changed.
- Permission catalog (docs/business/permission-catalog.md) was not changed in 1B.9 implementation slices.

## Production Readiness Statement

Phase 1B.9 closure review does not claim production readiness.

Production migration, release tag, and push remain unauthorized.

Production/deployment readiness requires:
- Resolution of SQL permission seed alignment.
- Resolution of runtime permission row confirmation.
- Resolution of SELL_CARE_PACKAGE workflow runtime configuration.
- Live manual API/UI/lifecycle validation in a suitable environment.
- Separate explicit Project Owner authorization.

## Closure Review Decision

Phase 1B.9 Care Package Sales is ready for Project Owner closure acceptance with deployment readiness notes.

All required gates (A scope, A detailed scope, B implementation plan, B1 backend/data, B2 workflow/payment, C frontend, D operational validation) are accepted by the Project Owner. Automated validation passed. Deployment readiness blockers are identified and carried forward. Non-blocking follow-ups are documented.

## Recommended Next Gate

Project Owner Phase 1B.9 closure acceptance.
