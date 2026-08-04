# Phase 1B.8 Project Owner Scope Acceptance

## Status

ACCEPTED — PHASE 1B.8 CARD REPRINT DISCOVERY/SCOPE ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.8 Card Reprint / Grave Card Reprint Discovery and Scope Plan as the working scope baseline.

This acceptance does not authorize implementation.

Because open decisions/blockers remain, the next authorized task is open-decision resolution and detailed scope clarification only.

## Accepted Scope Plan

Reference:

- Phase 1B.8 discovery/scope plan commit:
  d56ab47131b6137dcb3c4269a0e70f01c96f926c

- Scope plan document:
  docs/architecture/phase-1b8-card-reprint-discovery-and-scope-plan.md

## Accepted Business Context

- Card Reprint / Grave Card Reprint is the selected next work after Phase 1B.7.
- First print / reprint terminology requires clarification where still open.
- Card Reprint depends on Service Module Foundation and Payment Foundation.
- Card Reprint is expected to integrate with workflow/approval where business rules require approval.
- Reprint fee/payment handling must use accepted payment foundation patterns where applicable.
- Refunds, cancellation, partial payment, Care Package Sales, and production rollout remain out of scope.

## Accepted Proposed Scope

Accept the proposed planning scope as a baseline, including:

- Card Reprint request lifecycle discovery.
- approval lifecycle discovery.
- payment lifecycle discovery.
- print/release lifecycle discovery.
- audit/notification lifecycle discovery.
- proposed data impact analysis.
- proposed backend/API scope analysis.
- proposed frontend scope analysis.
- proposed permission scope analysis.
- proposed test strategy.
- open decision/blocker tracking.

## Accepted Workflow / Approval Scope Baseline

- Card Reprint should be evaluated as a workflow-enabled process.
- approval flow must not be hard-coded to individual users.
- approval should align with existing workflow/approval foundation.
- dynamic approval configuration remains in scope for analysis.
- workflow snapshot/versioning must be clarified.
- delegation impact must be clarified.
- rejection behavior must be clarified.
- approval timing relative to payment must be clarified.

## Accepted Payment / Service Dependency Baseline

- Card Reprint payment behavior must align with Phase 1B.7 Payment Foundation.
- service linkage must align with Phase 1B.6 Service Module Foundation.
- reprint fee model must be clarified.
- configurable price/effective-date behavior must be clarified.
- daily/monthly reconciliation impact must be clarified.
- no refund, cancellation, or partial payment flow is authorized.

## Accepted Proposed Data Scope Baseline

Accept as planning baseline only:

- proposed Card / Grave Card entity impact.
- proposed Card_Reprint_Requests or equivalent request tracking.
- reuse of existing Workflow entities where possible.
- reuse of existing Payment_Transactions / Payment_Transaction_Items where possible.
- reuse of pricing/versioning mechanisms where possible.
- migration need remains an open decision and is not authorized yet.

## Accepted Proposed Backend/API Scope Baseline

Accept as planning baseline only:

- request management API analysis.
- approval/workflow integration analysis.
- payment integration analysis.
- print/release status analysis.
- validation/audit/notification analysis.
- authorization/API test strategy analysis.

Do not authorize creating endpoints.

## Accepted Proposed Frontend Scope Baseline

Accept as planning baseline only:

- Card Reprint list page analysis.
- create request form analysis.
- detail page analysis.
- approval action analysis.
- payment status display analysis.
- print/release status display analysis.
- permission-gated UI analysis.
- error handling and test strategy analysis.

Do not authorize creating frontend pages.

## Accepted Proposed Permission Scope Baseline

Candidate permission analysis only.

State:
- permission codes are planning candidates only.
- permission catalog is not modified by this acceptance.
- no permission seeding is authorized.

Candidate permissions may include:
- CARD_REPRINT_REQUEST_CREATE.
- CARD_REPRINT_REQUEST_VIEW.
- CARD_REPRINT_REQUEST_APPROVE.
- CARD_REPRINT_REQUEST_REJECT.
- CARD_REPRINT_REQUEST_MARK_PRINTED.
- CARD_REPRINT_REQUEST_ADMIN.
- existing CARD_REPRINT_APPROVE if already present in repository docs.

## Accepted Open Decisions / Blockers

Carry forward OD-1B8-001 through OD-1B8-015.

Explicitly state that these must be resolved or formally classified before implementation planning can be accepted:

- OD-1B8-001: exact new print vs reprint terminology.
- OD-1B8-002: first print approval requirement.
- OD-1B8-003: second-or-later print approval levels.
- OD-1B8-004: fee model and whether 50k is configurable price.
- OD-1B8-005: payment timing relative to approval.
- OD-1B8-006: physical stamp / card custody handling.
- OD-1B8-007: requester and approver role mapping.
- OD-1B8-008: workflow snapshot/versioning rule.
- OD-1B8-009: whether rejection ends or returns request.
- OD-1B8-010: reporting requirements.
- OD-1B8-011: notification requirements.
- OD-1B8-012: print output/PDF/template scope.
- OD-1B8-013: whether Payment Print UI remains deferred or becomes part of Card Reprint.
- OD-1B8-014: database impact and migration need.
- OD-1B8-015: acceptance criteria gaps.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-A Card Reprint open-decision resolution and detailed scope clarification only.

Do not authorize:
- implementation,
- source code changes,
- test changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- permission catalog changes,
- production migration,
- release tag,
- push.

## Required Next Task Output

Future Phase 1B.8-A task must produce:

docs/architecture/phase-1b8a-card-reprint-open-decisions-and-detailed-scope.md

It must include:
- resolved decisions or explicit unresolved blockers.
- refined request lifecycle.
- refined approval lifecycle.
- refined payment timing.
- refined data impact.
- refined backend/API scope.
- refined frontend scope.
- refined permission scope.
- refined test strategy.
- implementation readiness recommendation.
- clear statement whether implementation planning may proceed.

## Boundaries

- no implementation is authorized by this decision.
- no source code changes are authorized.
- no test changes are authorized.
- no backend changes are authorized.
- no frontend changes are authorized.
- no migrations/rollbacks are authorized.
- no permission catalog changes are authorized.
- no production migration is authorized.
- no release tag is authorized.
- no push is authorized.
- Card Reprint implementation must wait for open-decision resolution, detailed scope acceptance, and Project Owner implementation authorization gates.

## Non-Goals

This document does not:

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

- Phase 1B.7 is closed.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- Care Package Sales remains deferred.
- production rollout remains deferred.
