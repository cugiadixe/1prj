# Phase 1B.9-A Project Owner Detailed Scope Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-A CARE PACKAGE SALES DETAILED SCOPE ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-A Care Package Sales updated detailed scope.

This acceptance is based on the updated detailed scope created after Project Owner blocker decisions.

This acceptance authorizes only the next planning task:
Phase 1B.9-B Care Package Sales implementation planning.

This acceptance does not authorize implementation, source code changes, database migrations, permission catalog changes, production migration, release tag, or push.

## Accepted Detailed Scope

- Phase 1B.9-A updated detailed scope commit:
  6f3bc245e6fdf342a7b6e4128f55e3a68db891a1

- Phase 1B.9-A Project Owner blocker decision response commit:
  9d8d168fd6e8a33c30c97eb7b8656361bbd0ec4c

- Phase 1B.9 Project Owner scope acceptance commit:
  f5e61a09718d55aa9d9287e6d88b4ff35a9adfc7

- Phase 1B.9 discovery/scope plan commit:
  2c9c6955474d899ef7274d880c7ce140f48a32f9

## Accepted Business Rules

- Business process name is Care Package Sales / Gói chăm sóc.
- Sale unit is cốt-year.
- Phase 1B.9 supports one-year packages only.
- Pricing comes from Service Foundation effective-date pricing.
- Price formula is unit price per cốt per year × cốt count × 1 year.
- Request total equals subtotal minus approved discount.
- Unit price snapshot, cốt count snapshot, subtotal, discount, and total must be preserved.
- Sale date determines applied effective-date price.
- Historical records must not be rewritten by later price changes.
- Renewal creates a new one-year request for the next service period.
- Duplicate active/paid overlapping care packages for the same care target must be prevented.
- Approval is required for discount, price override, changed-price renewal, or later configured approval-required rule.
- No approval is required for configured-price request with no discount.
- `SELL_CARE_PACKAGE` workflow is used when approval is required.
- Workflow Engine remains the source of truth.
- Discount is VND amount only, requires reason, and cannot reduce total below zero.
- Payment may be created only when payment-eligible.
- Payment-eligible means either no approval required with valid configured price, or approval required and approved.
- Confirmed payment is required before active status.
- Payment Foundation constraints apply: VND only, full payment only, no partial payment, no refund, no cancellation, one bill cannot be paid multiple times.
- Care Package Sales participates in existing daily/monthly manual reconciliation through Payment Foundation.
- Frontend must rely on backend-calculated totals and status.
- No hard-coded care package price is allowed.

## Accepted Candidate Scope for Implementation Planning

Backend/data:
- `CarePackageRequests`.
- `CarePackageRequestItems`.
- company/customer/service/workflow/payment references.
- lifecycle/status fields.
- pricing snapshots.
- discount fields.
- renewal traceability.
- rowversion and audit fields.

API:
- `/api/v2/care-packages` list/detail/create.
- `submit`.
- `approve`.
- `reject`.
- `create-payment`.
- `payment-status`.
- `activate` only after confirmed payment if accepted in implementation plan.

Workflow/payment:
- `SELL_CARE_PACKAGE` process.
- approval-required and no-approval paths.
- WorkflowRuntimeService source-of-truth.
- Service Foundation effective-date price lookup.
- safe failure for missing/inactive price.
- Payment Foundation full-payment constraints.

Frontend:
- `/care-packages`.
- `/care-packages/new`.
- `/care-packages/:id`.
- list/create/detail pages.
- lifecycle/status display.
- permission-gated actions.
- backend-calculated price/status display.
- safe 400/403/404/409 handling.

Permissions:
- `CARE_PACKAGE_VIEW`.
- `CARE_PACKAGE_CREATE`.
- `CARE_PACKAGE_APPROVE`.
- `CARE_PACKAGE_REJECT`.
- `CARE_PACKAGE_CREATE_PAYMENT`.
- `CARE_PACKAGE_REPORT_VIEW` as future/reporting candidate.
- reuse existing Payment Foundation permission for payment confirmation/correction.

Validation:
- backend build/unit/integration/API tests.
- frontend lint/build/Vitest.
- pricing/effective-date tests.
- cốt count calculation tests.
- one-year period validation.
- renewal/overlap validation.
- approval/no-approval paths.
- payment eligibility guards.
- company-scope and permission validation.
- no refund/cancellation/partial payment boundary validation.

## Accepted Exclusions / Non-Goals

The following remain out of scope unless later separately accepted:

- implementation in this acceptance task.
- source code changes in this acceptance task.
- database migrations in this acceptance task.
- permission catalog changes in this acceptance task.
- production migration.
- release tag.
- push.
- refunds.
- cancellation.
- partial payment.
- dynamic PDF/template generation.
- generic Payment Print UI.
- physical inventory/stamp stock management.
- unrelated service modules.
- multi-year packages.
- partial-year packages.
- discount percent UI.
- dedicated report/export UI.
- undocumented business rule changes.

## Authorization for Next Step

Authorized next task:
Phase 1B.9-B Care Package Sales implementation planning only.

The next task may create only an implementation plan document.

The next task must produce:

docs/architecture/phase-1b9b-care-package-sales-implementation-plan.md

The implementation plan must:
- translate the accepted detailed scope into a gated implementation plan.
- define backend/data implementation sequence.
- define migration/rollback candidates but not create them.
- define API/service implementation sequence.
- define workflow/payment implementation sequence.
- define frontend implementation sequence.
- define permission catalog impact.
- define validation/test strategy.
- define implementation boundaries and non-goals.
- recommend the first implementation slice.
- explicitly state that implementation remains unauthorized until Project Owner implementation plan acceptance.

Do not authorize:
- Care Package Sales implementation,
- source code changes,
- backend implementation,
- frontend implementation,
- database migrations,
- business docs changes,
- permission catalog changes,
- production migration,
- release tag,
- push,
- dynamic PDF/template generation,
- generic Payment Print UI,
- refund,
- cancellation,
- partial payment,
- physical inventory/stamp stock management.

## Required Next Task Output

Future Phase 1B.9-B implementation planning task must produce:

docs/architecture/phase-1b9b-care-package-sales-implementation-plan.md

It must include:
- accepted scope summary.
- proposed implementation phases.
- candidate migration/rollback plan.
- candidate backend/data plan.
- candidate API/application service plan.
- candidate workflow/payment plan.
- candidate frontend plan.
- candidate permission plan.
- test/validation plan.
- risks/dependencies.
- first authorized implementation slice recommendation.
- explicit non-authorization of implementation until PO implementation plan acceptance.

## Non-Goals

This acceptance does not:
- implement code.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- run production migration.
- create release tag.
- push.

## Notes

- Phase 1B.9-A detailed scope is accepted.
- Phase 1B.9 implementation has not started.
- local branch may be ahead of origin; no push is authorized.
- production migration and release tagging require separate explicit authorization.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
