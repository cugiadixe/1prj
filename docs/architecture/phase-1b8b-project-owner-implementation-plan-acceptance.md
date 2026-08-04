# Phase 1B.8-B Project Owner Implementation Plan Acceptance

## Status

ACCEPTED — PHASE 1B.8-B CARD REPRINT IMPLEMENTATION PLAN APPROVED

## Project Owner Decision

State:

The Project Owner accepts the Phase 1B.8-B Card Reprint implementation plan.

This acceptance authorizes only the first implementation sub-phase after this commit:
Phase 1B.8-B1 Card Reprint backend/data foundation implementation.

This acceptance does not authorize frontend implementation, operational validation, production migration, release tag, or push.

## Accepted Plan

Reference:

- Phase 1B.8-B implementation plan commit:
  87931c7993823be0784281b1694064dee92e323d

- Implementation plan document:
  docs/architecture/phase-1b8b-card-reprint-implementation-plan.md

## Accepted Scope Baseline

Confirm accepted baseline:

- Phase 1B.8-A detailed scope is accepted.
- Initial Print means first issuance/printing of a grave card for a grave/service record.
- Reprint means every print after Initial Print.
- system must track print count or enough request history.
- Reprint fee is currently 50,000 VND per card/reprint.
- Reprint fee must be configurable through existing service price/effective-date pattern where applicable.
- no hard-coded application fee.
- required approval completes before payment draft/bill creation.
- payment must be CONFIRMED before physical print/release completion.
- no partial payment.
- no refund.
- no cancellation.
- physical stamp/card custody is status-only in MVP.
- dynamic PDF/template generation is deferred from MVP.
- generic Payment Print UI remains deferred.
- Card Reprint may display payment status and payment reference/link only.

## Accepted Implementation Sequence

Accept implementation sequence as the working plan:

- Phase 1B.8-B1: Backend/Data Foundation.
- Phase 1B.8-B2: Workflow/Payment/API integration.
- Phase 1B.8-C: Frontend implementation.
- Phase 1B.8-D: Operational validation and closure.

State:
- only Phase 1B.8-B1 is authorized after this acceptance.
- later sub-phases require their own acceptance/review gates.

## Accepted Data / Database Plan

Accept as implementation planning baseline:

- proposed V0013/U0013 migration strategy.
- proposed Cards or Grave Card master entity.
- proposed Card_Reprint_Requests table.
- rowversion/concurrency.
- audit fields.
- workflow instance relationship.
- payment transaction relationship.
- service item relationship.
- customer/company/grave references.
- print count or request-history strategy.
- status tracking strategy.
- rollback strategy.

State:
- migration creation is authorized only in the next B1 implementation task, not in this acceptance task.
- production migration remains unauthorized.

## Accepted Backend / API Plan

Accept as planning baseline:

- CardReprintRequestsController or equivalent.
- application services.
- domain rules.
- DTOs.
- lifecycle validation.
- authorization checks.
- workflow integration points.
- payment integration points.
- pricing/service integration points.
- audit integration.
- safe error handling.
- concurrency behavior.
- backend unit/integration/API tests.

State:
- backend implementation is authorized only for the next B1 task after this acceptance.
- frontend implementation is not authorized.

## Accepted Workflow / Approval Integration Plan

Accept as planning baseline:

- process key CARD_REPRINT.
- dynamic approval flow alignment.
- approval flow assignment without hard-coded individual users.
- workflow snapshot/versioning.
- department/title/position approver resolution.
- delegation behavior.
- approval/rejection outcomes.
- no mutation before authorization.
- audit trail.
- workflow tests.

## Accepted Payment / Service Integration Plan

Accept as planning baseline:

- uses Service Module Foundation.
- uses configurable service price/effective-date pattern for Reprint fee.
- creates payment draft/bill after required approval.
- links payment transaction to Card Reprint request.
- requires CONFIRMED payment before print/release.
- applies Payment Foundation correction/reconciliation rules.
- enforces no refund/cancellation/partial payment.
- keeps generic Payment Print UI deferred.

## Accepted Frontend Plan

Accept as later-phase planning baseline only:

- frontend module/folder plan.
- API client/types plan.
- route/navigation plan.
- list page plan.
- create request page plan.
- detail page plan.
- approval/rejection UI plan.
- payment status/link UI plan.
- mark printed/released UI plan.
- permission-gated rendering.
- safe error handling.
- frontend tests.

State:
- frontend implementation is not authorized by this acceptance.
- Phase 1B.8-C requires later gate.

## Accepted Permission Plan

Accept candidate permission plan as implementation baseline:

- CARD_REPRINT_REQUEST_CREATE.
- CARD_REPRINT_REQUEST_VIEW.
- CARD_REPRINT_APPROVE.
- CARD_REPRINT_REQUEST_REJECT.
- CARD_REPRINT_REQUEST_MARK_PRINTED.
- CARD_REPRINT_REQUEST_ADMIN.

Confirm:
- final permission catalog/seed changes are authorized only in the next B1 implementation task if included in B1 scope.
- permission catalog was not modified in this acceptance task.
- backend authorization remains authoritative.
- frontend gating remains convenience only.

## Accepted Test / Validation Plan

Accept test plan covering:

Backend:
- domain unit tests.
- application service tests.
- lifecycle tests.
- fee resolution tests.
- authorization tests.
- concurrency tests.
- audit tests.

Integration:
- database persistence.
- migration/rollback if DB changes are implemented.
- workflow integration.
- payment integration.
- reconciliation integration.

API:
- create request.
- list/detail.
- approve/reject.
- payment link/status.
- mark printed.
- mark released.
- unauthorized/forbidden.
- invalid lifecycle.
- 409 concurrency.
- sanitized errors.

Frontend:
- later Phase 1B.8-C frontend tests.

Operational:
- later Phase 1B.8-D end-to-end validation.

## Accepted Boundaries

Confirm implementation must not include unless later authorized:

- Care Package Sales.
- production rollout.
- dynamic PDF/template generation.
- generic Payment Print UI.
- physical inventory/stamp stock management.
- refunds.
- cancellation.
- partial payment.
- production migration.
- release tag.
- push.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-B1 Card Reprint backend/data foundation implementation only.

The next task may include only the B1 backend/data scope accepted in the implementation plan, including:
- V0013/U0013 if required by the accepted B1 data plan,
- backend/domain/application/API foundation,
- permission seed/catalog changes only if explicitly required by the B1 plan,
- backend unit/integration/API tests,
- implementation report.

Do not authorize:
- frontend implementation,
- operational validation execution,
- Care Package Sales,
- production migration,
- release tag,
- push.

## Required Next Task Output

Future Phase 1B.8-B1 implementation task must produce:

docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-report.md

It must include:
- implementation summary.
- files changed.
- database/migration evidence if created.
- rollback evidence if created.
- backend/API implementation evidence.
- workflow/payment/service integration evidence.
- permission seed evidence if changed.
- test evidence.
- boundary confirmation.
- risks/follow-ups.

## Non-Goals

Confirm this acceptance does not:

- implement Card Reprint.
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

Include:

- Phase 1B.8 remains pre-implementation until this acceptance is committed.
- after this acceptance commit, only B1 backend/data foundation implementation is authorized.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- Care Package Sales remains deferred.
- production rollout remains deferred.
