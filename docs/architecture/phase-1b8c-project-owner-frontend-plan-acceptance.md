# Phase 1B.8-C Project Owner Frontend Plan Acceptance

## Status

ACCEPTED — PHASE 1B.8-C CARD REPRINT FRONTEND IMPLEMENTATION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.8-C Card Reprint frontend implementation plan.

This acceptance is based on the proposed frontend implementation plan.

This acceptance authorizes only the next implementation task:
Phase 1B.8-C Card Reprint frontend implementation.

This acceptance does not authorize backend implementation, operational validation execution, production migration, release tag, or push.

## Accepted Plan

- Phase 1B.8-C frontend implementation plan commit:
  f31d332d03b0c2a297a7eb7f1747833a69480112

- Phase 1B.8-B2 Project Owner workflow/payment acceptance commit:
  edda862664724dd4c65373a6280bfa1e8881e1e0

## Accepted Frontend Scope

- Card Reprint request list page.
- Card Reprint request create page/form.
- Card Reprint request detail page.
- lifecycle/status display.
- submit action UI.
- approve action UI.
- reject action UI.
- create payment draft/bill action UI.
- read-only payment status display.
- payment link behavior if existing Payment Foundation route is available.
- mark printed action UI if backend-supported.
- mark released action UI if backend-supported.
- permission-gated UI actions.
- company scope behavior through existing frontend conventions.
- loading states.
- empty states.
- safe error handling for 400/403/404/409 responses.
- API client/hooks for Card Reprint endpoints.
- frontend tests for Card Reprint UI behavior.
- frontend implementation report.

## Accepted Routes / Pages

- /cards/reprints
- /cards/reprints/new
- /cards/reprints/:id

If existing frontend route conventions require adjusted paths, implementation may adapt to existing conventions while preserving the accepted functional scope.

## Accepted UI Boundaries

- display fee/payment data returned by backend/payment APIs only.
- not hard-code 50,000 VND.
- not infer paid status locally.
- rely on backend payment-status and backend lifecycle guards.
- treat frontend permission gating as usability only.
- keep backend authorization authoritative.

## Accepted Validation Plan

- lint command used by the repo.
- TypeScript check command used by the repo.
- Vitest command used by the repo.
- targeted Card Reprint frontend tests.
- Playwright only if already used by repo conventions for similar flows.

## Accepted Boundaries

- backend implementation.
- database migrations.
- business rule changes.
- permission catalog changes.
- dynamic PDF/template generation.
- generic Payment Print UI.
- refund.
- cancellation.
- partial payment.
- physical inventory/stamp stock management.
- Care Package Sales.
- operational validation execution.
- production migration.
- release tag.
- push.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-C Card Reprint frontend implementation only.

The next task may include only frontend implementation scope accepted by this plan:
- frontend API client/hooks.
- frontend route registration.
- Card Reprint list page.
- Card Reprint create form.
- Card Reprint detail page.
- workflow action UI.
- payment status/action UI.
- print/release action UI if backend-supported.
- permission-gated UI behavior.
- frontend tests.
- frontend implementation report.

Do not authorize:
- backend implementation,
- database migrations,
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

Future Phase 1B.8-C frontend implementation task must produce:

docs/architecture/phase-1b8c-card-reprint-frontend-implementation-report.md

It must include:
- implementation summary.
- files changed.
- routes/pages implemented.
- components implemented.
- API client/hooks implemented.
- permission-gated UI evidence.
- lifecycle/workflow/payment UI evidence.
- tests added/updated.
- validation evidence.
- boundary confirmation.
- risks/follow-ups.

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

- frontend plan is accepted.
- Phase 1B.8 remains not closed.
- frontend implementation may proceed only within accepted 1B.8-C scope.
- operational validation remains deferred to Phase 1B.8-D.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
