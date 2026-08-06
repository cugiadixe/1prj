# Phase 1B.8-B2 Project Owner Workflow/Payment Acceptance

## Status

ACCEPTED — PHASE 1B.8-B2 CARD REPRINT WORKFLOW/PAYMENT ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.8-B2 Card Reprint workflow/payment integration implementation.

This acceptance is based on the passed workflow/payment acceptance review.

This acceptance authorizes only the next planning gate:
Phase 1B.8-C Card Reprint frontend implementation planning.

This acceptance does not authorize frontend implementation, operational validation execution, production migration, release tag, or push.

## Accepted Review

- Phase 1B.8-B2 workflow/payment acceptance review commit:
  b78e6c6b245ff0337c86a1112467cc7a659ccbfb

- Phase 1B.8-B2 workflow/payment implementation commit:
  67f480f2d4808c160a22ce6ec4ce2d4a51e604d5

- Phase 1B.8-B1 Project Owner backend/data acceptance commit:
  16819c724efeaaf832f7332c93a0d87f22701cf8

## Accepted B2 Scope

- Card Reprint workflow integration.
- CARD_REPRINT process integration.
- Workflow Engine remains source of truth.
- approve/reject facades delegate to WorkflowRuntimeService.
- CardReprintExecutionHandler integration.
- domain status synchronization after successful workflow execution.
- payment draft creation after approved workflow state.
- configurable CARD_REPRINT service/price lookup.
- safe failure if CARD_REPRINT service/price config is missing or inactive.
- no hard-coded 50,000 VND fallback.
- payment transaction link.
- read-only payment-status endpoint.
- confirmed payment guard before mark printed.
- printed-before-released guard.
- B2 backend/API permission enforcement.
- B2 domain unit tests.
- full backend validation.

## Accepted Validation Evidence

- dotnet build src/backend/PTKD-ERP.sln:
  Succeeded, 0 errors, 9 warnings.

- dotnet test tests/backend/PTKD.UnitTests/:
  Passed 226, Failed 0.

- dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false:
  Passed 203, Failed 0.

- dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false:
  Passed 305, Failed 0.

- git diff --check:
  Passed.

## Accepted Boundaries

- frontend implementation.
- frontend files.
- Care Package Sales.
- production migration.
- release tag.
- push.
- dynamic PDF/template generation.
- generic Payment Print UI.
- refund.
- cancellation.
- partial payment.
- physical inventory/stamp stock management.
- implementation_plan.md.
- task.md.
- scratch/decompiled/FixStrategy/script/debug files.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-C Card Reprint frontend implementation planning only.

The next task may create only a frontend implementation plan document.

The frontend planning task must define:
- pages/routes to add.
- component boundaries.
- API client usage.
- permission-gated UI behavior.
- Card Reprint request lifecycle UI.
- workflow approval/rejection UI.
- payment status UI.
- print/release action UI if backend-supported.
- frontend tests to be added.
- out-of-scope items.
- implementation sequence.
- validation strategy.

Do not authorize:
- frontend implementation,
- backend implementation,
- operational validation execution,
- Care Package Sales,
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

Future Phase 1B.8-C frontend planning task must produce:

docs/architecture/phase-1b8c-card-reprint-frontend-implementation-plan.md

It must include:
- proposed frontend scope.
- routes/pages.
- components.
- API integration plan.
- permission/UI gating plan.
- test plan.
- validation plan.
- boundaries and deferrals.
- recommended frontend implementation steps.

## Non-Goals

- implement frontend.
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

- B2 workflow/payment integration is accepted.
- Phase 1B.8 remains not closed.
- frontend remains deferred to Phase 1B.8-C after frontend plan acceptance.
- operational validation remains deferred to Phase 1B.8-D.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
