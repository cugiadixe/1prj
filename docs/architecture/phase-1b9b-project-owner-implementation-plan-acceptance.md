# Phase 1B.9-B Project Owner Implementation Plan Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-B CARE PACKAGE SALES IMPLEMENTATION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-B Care Package Sales implementation plan.

This acceptance is based on the accepted Phase 1B.9-A detailed scope and the proposed implementation plan.

This acceptance authorizes only the first implementation slice:
Phase 1B.9-B1 Care Package Sales backend/data foundation.

This acceptance does not authorize frontend implementation, full workflow/payment integration, production migration, release tag, or push.

## Accepted Implementation Plan

- Phase 1B.9-B implementation plan commit:
  b02c3392f78922d6f649941e6d1431cf52f86a65

- Phase 1B.9-A Project Owner detailed scope acceptance commit:
  4ecf849e8d90b3066c9fa09bc0dab0031ba455b9

- Phase 1B.9-A updated detailed scope commit:
  6f3bc245e6fdf342a7b6e4128f55e3a68db891a1

- Phase 1B.9-A Project Owner blocker decision response commit:
  9d8d168fd6e8a33c30c97eb7b8656361bbd0ec4c

## Accepted Business Rule Baseline

- Business process name is Care Package Sales / Gói chăm sóc.
- Sale unit is cốt-year.
- Phase 1B.9 supports one-year packages only.
- Pricing comes from Service Foundation effective-date pricing.
- Price formula is unit price per cốt per year × cốt count × 1 year.
- Request total equals subtotal minus approved discount.
- Pricing snapshots must be preserved.
- Sale date determines effective-date price.
- Historical records must not be rewritten by later price changes.
- Renewal creates a new one-year request for the next service period.
- Duplicate active/paid overlapping care packages for the same care target must be prevented.
- Approval is required for discount, price override, changed-price renewal, or configured approval-required rule.
- No approval is required for configured-price request with no discount.
- `SELL_CARE_PACKAGE` workflow is used when approval is required.
- Payment may be created only when payment-eligible.
- Confirmed payment is required before active status.
- Payment Foundation constraints apply.
- Frontend must rely on backend-calculated totals/status.
- No hard-coded care package price is allowed.

## Accepted Implementation Sequence

1. Phase 1B.9-B1 backend/data foundation.
2. Phase 1B.9-B2 workflow/payment integration.
3. Phase 1B.9-C frontend implementation.
4. Phase 1B.9-D operational validation.
5. Phase 1B.9 closure review and Project Owner closure acceptance.

## Authorized First Implementation Slice

Authorized next task:
Phase 1B.9-B1 Care Package Sales backend/data foundation implementation only.

The next task may implement only the backend/data foundation slice.

Authorized B1 scope:
- create candidate migration/rollback for Care Package Sales foundation using the next repository migration number, expected V0014/U0014 if V0013/U0013 remains latest.
- create `CarePackageRequest` domain entity.
- create `CarePackageRequestItem` domain entity.
- create EF mappings/configurations.
- integrate entities into `AppDbContext`.
- create application DTOs for list/detail/create.
- create application service foundation.
- implement list/detail/create APIs under `/api/v2/care-packages`.
- implement backend-calculated pricing snapshot foundation using Service Foundation effective-date pricing where feasible for B1.
- preserve company scope.
- preserve customer requirement.
- preserve cốt count snapshot.
- preserve one-year period fields.
- preserve subtotal/discount/total fields.
- include rowversion/audit fields according to repository conventions.
- add B1 backend/domain/integration/API tests.
- add permission constants only as required for B1 backend authorization, but do not modify business permission catalog unless explicitly necessary and justified in the B1 report.

B1 must not implement:
- frontend pages/components.
- Phase 1B.9-C frontend work.
- full `SELL_CARE_PACKAGE` workflow runtime integration.
- approve/reject workflow facades.
- full payment draft/bill creation.
- payment-status endpoint beyond what the B1 plan explicitly allows.
- active status after confirmed payment.
- production migration.
- release tag.
- push.
- dynamic PDF/template generation.
- generic Payment Print UI.
- refund.
- cancellation.
- partial payment.
- physical inventory/stamp stock management.
- multi-year packages.
- partial-year packages.
- discount percent UI.
- dedicated report/export UI.

## Required B1 Implementation Report

The next task must produce:

docs/architecture/phase-1b9b1-care-package-sales-backend-data-implementation-report.md

The report must include:
- implemented files.
- migration/rollback names.
- schema summary.
- backend/domain/application/API summary.
- authorization summary.
- pricing snapshot summary.
- tests added/updated.
- validation evidence.
- boundary confirmation.
- known risks/follow-ups.
- explicit statement of any deferred B2/C/D work.

## Required B1 Validation

Future B1 implementation must run and record:

Backend:
- `dotnet build src/backend/PTKD-ERP.sln`
- `dotnet test tests/backend/PTKD.UnitTests/`
- `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false`
- `dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false`

Repository:
- `git diff --check`

Frontend validation is not required for B1 unless frontend files are changed, which they should not be.

## Non-Goals

This acceptance task does not:
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

- Phase 1B.9 implementation has not started in this acceptance task.
- implementation may begin only in the next B1 task and only within the accepted B1 scope.
- local branch may be ahead of origin; no push is authorized.
- production migration and release tagging require separate explicit authorization.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
