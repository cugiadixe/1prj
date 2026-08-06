# Phase 1B.8-A Project Owner Detailed Scope Acceptance

## Status

ACCEPTED — PHASE 1B.8-A CARD REPRINT DETAILED SCOPE ACCEPTED

## Project Owner Decision

State:

The Project Owner accepts the corrected Phase 1B.8-A Card Reprint / Grave Card Reprint detailed scope.

This acceptance is based on the corrected detailed scope document and the updated detailed scope acceptance review.

This acceptance does not authorize implementation.

## Accepted Review

Reference:

- Updated detailed scope acceptance review commit:
  e87394786f7d77645efd78ebb075cc37cd2c40b6

- Corrected detailed scope commit:
  c29468426d6a99d01e3db49164541712bb6ed403

- Project Owner blocker decision response commit:
  97c802d96202131b975f02b07c6c8ab3a77f2905

## Accepted Detailed Scope Baseline

Confirm accepted baseline includes:

- Initial Print = first issuance/printing of a grave card for a grave/service record.
- Reprint = every print after Initial Print.
- system must track print count or enough request history.
- Reprint fee is currently 50,000 VND per card/reprint.
- Reprint fee must be configurable through existing service price/effective-date pattern where applicable.
- no hard-coded application fee.
- Initial Print fee remains deferred/unresolved unless source-supported.
- request is created first.
- required approval completes before payment draft/bill creation.
- payment must be CONFIRMED before physical print/release completion.
- no partial payment.
- no refund.
- no cancellation.
- physical stamp/card custody is status-only in MVP.
- dynamic PDF/template generation is deferred from MVP.
- generic Payment Print UI remains deferred.
- Card Reprint may display payment status and payment reference/link only.

## Accepted MVP Lifecycle

Accept the lifecycle:

1. create Card Reprint request.
2. validate customer/grave/card/service references.
3. classify Initial Print vs Reprint.
4. evaluate approval requirement.
5. create workflow instance when required.
6. use workflow snapshot/versioning for in-flight requests.
7. approve or reject request.
8. after required approval, create payment draft/bill.
9. require CONFIRMED payment before physical print/release.
10. mark printed.
11. mark released/handed over.
12. record audit trail.
13. include payment in reconciliation where applicable.
14. close request.

## Accepted Approval / Workflow Scope

Confirm:

- Card Reprint is workflow-enabled where approval is required.
- approval flow is not hard-coded to individual users.
- workflow assignment aligns with existing dynamic approval foundation.
- department/title/position approver resolution is considered.
- delegation behavior is considered.
- workflow snapshot/versioning is required for in-flight requests.
- rejection handling is part of the MVP scope.
- approval must complete before payment draft/bill creation for paid Reprint.
- workflow implementation is not authorized by this acceptance.

## Accepted Payment / Service Scope

Confirm:

- Card Reprint aligns with Service Module Foundation.
- Reprint fee uses configurable service price/effective-date pattern where applicable.
- Payment Foundation is used for payment draft/bill and payment confirmation.
- CONFIRMED payment is required before print/release completion.
- payment correction and reconciliation rules remain applicable.
- daily/monthly reconciliation includes Card Reprint payments where paid.
- no refund, cancellation, or partial payment is introduced.
- generic Payment Print UI remains deferred.

## Accepted Data Scope Baseline

Accept planning baseline only:

- Card / Grave Card master entity or equivalent.
- Card_Reprint_Requests or equivalent request tracking.
- print count or request history tracking.
- workflow instance link.
- payment transaction link.
- service item link.
- customer link.
- company scope.
- grave/site/zone/lot reference needs.
- audit fields.
- rowversion/concurrency fields.
- soft-delete policy to be finalized during implementation planning if needed.
- reuse existing workflow/payment/service/pricing tables where possible.

State:
- migration need remains planning-only and must be addressed in implementation planning.
- no migration is authorized by this acceptance.

## Accepted Backend/API Scope Baseline

Accept planning baseline only:

- Card Reprint controller/API analysis.
- application service analysis.
- domain rules analysis.
- DTO analysis.
- validation rules.
- authorization checks.
- workflow integration.
- payment integration.
- audit/notification points.
- safe error handling.
- concurrency behavior.
- unit/integration/API test strategy.

State:
- no endpoints are authorized by this acceptance.

## Accepted Frontend Scope Baseline

Accept planning baseline only:

- Card Reprint routes/navigation analysis.
- list page analysis.
- create request page analysis.
- detail page analysis.
- approval/rejection UI analysis.
- payment status/link UI analysis.
- mark printed/released UI analysis.
- permission-gated UI analysis.
- safe error handling.
- frontend test strategy.

State:
- no frontend pages are authorized by this acceptance.
- dynamic PDF/template UI is deferred.
- generic Payment Print UI is deferred.

## Accepted Permission Scope Baseline

Accept candidate permission planning only:

- CARD_REPRINT_REQUEST_CREATE.
- CARD_REPRINT_REQUEST_VIEW.
- CARD_REPRINT_APPROVE.
- CARD_REPRINT_REQUEST_REJECT.
- CARD_REPRINT_REQUEST_MARK_PRINTED.
- CARD_REPRINT_REQUEST_ADMIN.

Confirm:
- permission catalog was not modified.
- permission seeding is not authorized.
- final permission changes must be handled in implementation planning and PO authorization gate.
- backend authorization remains authoritative.
- frontend gating remains convenience only.

## Accepted Test Strategy Baseline

Accept planning baseline for:

Backend:
- domain unit tests.
- application service tests.
- workflow integration tests.
- payment integration tests.
- authorization tests.
- concurrency tests.
- audit tests.
- migration/rollback tests if DB changes are later approved.

API:
- create request.
- get/list/detail.
- approve/reject.
- mark printed/released.
- payment link/status.
- unauthorized/forbidden.
- invalid lifecycle.
- 409 concurrency.
- sanitized errors.

Frontend:
- list page.
- create page.
- detail page.
- approval/rejection UI.
- payment status UI.
- mark printed/released UI.
- permission-gated rendering.
- safe error rendering.

Operational:
- end-to-end Card Reprint checklist.

## Deferred Items

Document:

- Initial Print fee remains deferred unless source-supported.
- dynamic PDF/template generation remains deferred.
- generic Payment Print UI remains deferred.
- physical inventory/stamp stock management remains deferred.
- Care Package Sales remains deferred.
- production rollout remains deferred.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-B Card Reprint implementation planning only.

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

Future Phase 1B.8-B implementation planning must produce:

docs/architecture/phase-1b8b-card-reprint-implementation-plan.md

It must include:
- proposed implementation sequence.
- database/migration strategy if needed.
- rollback strategy if migration is proposed.
- backend/API implementation plan.
- workflow integration plan.
- payment integration plan.
- frontend implementation plan.
- permission catalog/seed plan.
- test strategy.
- validation strategy.
- exact boundaries.
- open risks.
- readiness recommendation.
- clear statement that implementation still requires PO implementation scope acceptance.

## Boundaries

Confirm:

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
- Card Reprint implementation must wait for implementation planning and PO implementation authorization gates.

## Non-Goals

Confirm this document does not:

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

- Phase 1B.8 remains pre-implementation.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- Care Package Sales remains deferred.
- production rollout remains deferred.
