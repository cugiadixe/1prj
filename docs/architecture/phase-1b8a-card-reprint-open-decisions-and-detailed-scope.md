# Phase 1B.8-A Card Reprint Open Decisions and Detailed Scope Clarification

## Status

PROPOSED — READY FOR PROJECT OWNER DETAILED SCOPE ACCEPTANCE REVIEW

## Authorization Source

Reference:
- Phase 1B.8 Project Owner scope acceptance commit:
  8f58c813f6475a03090dc7da24a6515d4a805611

State:
- Phase 1B.8 scope is accepted as working baseline.
- This document resolves or classifies open decisions based on Project Owner blocker decisions.
- This document is detailed scope clarification only.
- This document does not authorize implementation.

## Source Documents Reviewed

- docs/architecture/phase-1b8a-project-owner-blocker-decision-response.md
- docs/architecture/phase-1b8-project-owner-scope-acceptance.md
- docs/architecture/phase-1b8-card-reprint-discovery-and-scope-plan.md
- docs/architecture/post-1b7-project-owner-next-work-decision.md
- docs/architecture/post-1b7-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b7-project-owner-closure-acceptance.md
- docs/business/business-rules.md
- docs/business/process-catalog.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- PTKD-ERP-Master-Context.md
- docs/architecture/project-readiness-review.md
- docs/architecture/phase-1b3-project-owner-closure-acceptance.md
- docs/architecture/phase-1b6-project-owner-closure-acceptance.md

## Decision Resolution Summary

| ID | Decision | Classification | Resolution / Proposed Answer | Blocking? | Evidence / Source |
|---|---|---|---|---|---|
| OD-1B8-001 | exact new print vs reprint terminology | RESOLVED BY PO DECISION | Initial Print = first issuance/printing of a grave card for a grave/service record. Reprint = every print after Initial Print. System must track print count or enough request history. | No | PO Blocker Decision |
| OD-1B8-002 | first print approval requirement | RESOLVED BY SOURCE DOCUMENTS | First issue requires no approval. | No | `docs/business/process-catalog.md` |
| OD-1B8-003 | second-or-later print approval levels | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose conditional workflow using standard dynamic configuration. | No | `docs/business/process-catalog.md` states workflow may apply. |
| OD-1B8-004 | fee model and whether 50k is configurable price | RESOLVED BY PO DECISION | Reprint fee is 50,000 VND per card/reprint, must be configurable using existing service price/effective-date pattern where applicable. No hard-coded application fee. Initial Print fee remains deferred/unresolved unless source-supported. | No | PO Blocker Decision |
| OD-1B8-005 | payment timing relative to approval | RESOLVED BY PO DECISION | Payment draft/bill created after approval. Payment CONFIRMED before physical print/release completion. | No | PO Blocker Decision |
| OD-1B8-006 | physical stamp / card custody handling | RESOLVED BY PO DECISION | MVP tracks status only: approved/rejected, payment confirmed, card printed, card released/handed over, actor, timestamp, optional note. No physical inventory, stamp stock, or advanced custody logistics in MVP. | No | PO Blocker Decision |
| OD-1B8-007 | requester and approver role mapping | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose using standard permissions mapped via workflow engine. | No | `docs/business/process-catalog.md` |
| OD-1B8-008 | workflow snapshot/versioning rule | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose standard Phase 1B.3 behavior: running requests retain original snapshot. | No | Phase 1B.3 foundation |
| OD-1B8-009 | whether rejection ends or returns request | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose standard Phase 1B.3 behavior: rejection ends request. | No | Phase 1B.3 foundation |
| OD-1B8-010 | reporting requirements | DEFERRED — NOT REQUIRED FOR PHASE 1B.8 IMPLEMENTATION | Standard lists only, no custom reports yet. | No | Out of scope |
| OD-1B8-011 | notification requirements | DEFERRED — NOT REQUIRED FOR PHASE 1B.8 IMPLEMENTATION | Notifications deferred. | No | Out of scope |
| OD-1B8-012 | print output/PDF/template scope | RESOLVED BY PO DECISION | Dynamic PDF/template generation is deferred from MVP. MVP may record request, approve/reject, show payment status, mark printed, mark released. | No | PO Blocker Decision |
| OD-1B8-013 | whether Payment Print UI remains deferred or becomes part of Card Reprint | RESOLVED BY PO DECISION | Generic Payment Print UI remains deferred. Card Reprint may display payment status and payment reference/link only. Do not include generic bill/payment print UI in Phase 1B.8 MVP. | No | PO Blocker Decision |
| OD-1B8-014 | database impact and migration need | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose `Cards` and `Card_Reprint_Requests` tables. | No | Based on standard design |
| OD-1B8-015 | acceptance criteria gaps | RESOLVED BY PO DECISION | MVP acceptance criteria baseline has been established. | No | PO Blocker Decision |

## Confirmed Business Rules

- Card Reprint (`CARD_REPRINT`) is a `CONDITIONAL` approval process.
- Condition fields for approval include: `company_id`, `previous_print_count`, `reprint_number`, `fee_amount`, `reason_code`.
- First issue (Initial Print) requires no approval.
- Workflow may apply from the second print onward (Reprint).

## Proposed Rules Requiring PO Confirmation

- Conditional workflow configuration for Reprints should use standard Phase 1B.3 dynamic rules.
- Requester and approver role mapping should use the standard permission-based workflow bindings.
- In-flight requests should retain their original workflow version (snapshot).
- Rejection should terminate the request; returns require a new submission round.
- New database tables `Cards` and `Card_Reprint_Requests` should be introduced.

## Blocking Decisions Remaining

No blocking decisions remain for MVP. The Project Owner has provided all required decisions in the blocker decision response.

## Refined Request Lifecycle

1. Requester selects a Service and initiates a Card Reprint request.
2. System determines print count and classifies as Initial Print or Reprint.
3. System validates customer/grave/card/service references.
4. If Initial Print, bypass approval and proceed to payment/print steps (Initial Print fee deferred).
5. If Reprint, evaluate approval requirement based on condition fields. Create workflow instance if required.
6. Workflow captures snapshot of configuration.
7. Approvers approve or reject (rejection terminates request).
8. Payment draft/bill creation occurs **after** required approval.
9. Payment confirmation is required **before** physical print/release completion.
10. Authorized user marks card as printed.
11. Authorized user marks card as released/handed over.
12. System marks request as COMPLETED.
13. Audit trail records all transitions.
14. (Deferred) Notifications.
15. Reconciliation impact includes Card Reprint payment if paid.

*Note: No partial payment, no refund, and no cancellation are permitted.*

## Refined Approval / Workflow Scope

- Process Key: `CARD_REPRINT`.
- Approval Rule: Initial Print requires no approval. Reprints require conditional approval.
- Assignment: Dynamic Admin-configured approval flow assignment based on `CARD_REPRINT_APPROVE` permission.
- Delegation: Standard Phase 1B.3 delegation applies.
- Snapshot/Versioning: Running requests use the workflow version active at creation time.
- Rejection: Rejection terminates the request workflow.
- Re-submit: Requires a new request.
- Audit: Phase 1B.3 standard audit logs for state changes.
- Permissions: Execution requires `CARD_REPRINT_APPROVE` matching the company scope.

## Refined Payment / Service Scope

- Service Modeling: Card Reprint is modeled as a service action related to the Grave/Card.
- Fee Model: Reprint fee is 50,000 VND per card/reprint, configurable using the existing service price/effective-date pattern where applicable. No hard-coded application fee. Initial Print fee remains deferred/unresolved unless source-supported.
- Payment Timing: Payment draft/bill is created after approval. Payment must be CONFIRMED before physical print/release completion.
- Payment Correction: Phase 1B.7 payment correction rules apply.
- Reconciliation: Payments will be included in daily/monthly reconciliation.
- Out of Scope: Refunds, cancellations, partial payments remain out of scope. Confirmed payment remains non-deletable.

## Refined Data Scope

- Cards: (Proposed) Tracks the current card state, references Grave/Customer/Service. Key fields: `Id`, `CompanyId`, `GraveId`, `ServiceId`, `PrintCount`, `Status`, `RowVersion`. Required for MVP to distinguish Initial Print vs Reprint.
- Card_Reprint_Requests: (Proposed) Tracks individual reprint requests. Key fields: `Id`, `CompanyId`, `CardId`, `RequesterId`, `ReprintNumber`, `FeeAmount`, `ReasonCode`, `WorkflowInstanceId`, `PaymentTransactionId`, `Status`, `RowVersion`. Required for MVP.
- Workflow / Payment links: Reuse existing `Workflow_Instances`, `Approval_Actions`, `Payment_Transactions`, `Payment_Transaction_Items`.
- Audit: Standard audit fields (`CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`).
- Soft Delete: Not recommended for financial/workflow requests.

## Refined Backend/API Scope

- Controller: `CardReprintRequestsController`. (Required)
- Application Service: `CardReprintRequestService`. (Required)
- DTOs: `CreateCardReprintRequestDto`, `CardReprintRequestDetailDto`, `CardReprintRequestListDto`. (Required)
- Endpoints: (Required)
  - `POST /api/v2/cards/reprint-requests`
  - `GET /api/v2/cards/reprint-requests`
  - `GET /api/v2/cards/reprint-requests/{id}`
  - `POST /api/v2/cards/reprint-requests/{id}/mark-printed`
  - `POST /api/v2/cards/reprint-requests/{id}/mark-released`
- Validation: Ensure valid service/grave, enforce permission scope. Ensure payment is confirmed before printed/released.
- Workflow/Payment: Integration points aligned with the refined lifecycle.
- Tests: Domain unit tests, integration tests for workflow/payment, API tests for authorization and concurrency. (Required)

## Refined Frontend Scope

- Routes: `/cards/reprint-requests`, `/cards/reprint-requests/:id`. (Required)
- List Page: Data grid showing requests, statuses, payment links. (Required)
- Create Page: Form to select grave/service, show calculated configurable fee, submit. (Required)
- Detail Page: Shows workflow timeline, payment status/link only, mark printed/released toggles. (Required)
- Approval UI: Standard approve/reject action buttons. (Required)
- Print/PDF output: Dynamic PDF/template generation is deferred from MVP. (Deferred)
- Payment Print UI: Generic Payment Print UI remains deferred. Do not include generic bill/payment print UI. (Deferred)
- Error Handling: Safe rendering of validation and 409 errors. (Required)
- Tests: Component and page-level frontend tests. (Required)

## Refined Permission Scope

- `CARD_REPRINT_REQUEST_CREATE`: (Proposed) Allows submitting requests. Company scope. Assigned to staff. Required for MVP.
- `CARD_REPRINT_REQUEST_VIEW`: (Proposed) Allows viewing requests. Company scope. Assigned to staff/managers. Required for MVP.
- `CARD_REPRINT_APPROVE`: (Source-supported) Allows approving requests. Company scope. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_REJECT`: (Proposed) Allows rejecting requests. Company scope. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_MARK_PRINTED`: (Proposed) Allows marking card as printed/released. Company scope. Assigned to admin/printing staff. Required for MVP.
- `CARD_REPRINT_REQUEST_ADMIN`: (Proposed) Allows administrative overrides. Global scope. Assigned to system admins. Deferred.

## Refined Test Strategy

- Backend:
  - Unit tests for domain logic (Initial Print vs Reprint evaluation).
  - Workflow integration tests for condition evaluation and state changes.
  - Payment integration tests ensuring fee creation occurs after approval.
  - Authorization tests for all endpoints.
  - Concurrency tests (409) for parallel request processing.
- API:
  - Full lifecycle tests from creation to completion.
  - Invalid state transition tests (e.g. attempting to mark printed before payment).
  - Forbidden/unauthorized tests.
- Frontend:
  - List/detail page rendering tests.
  - Permission-gated UI elements test (buttons hidden if no permission).
  - Validation error display tests.
- Operational:
  - Manual checklist covering headless flow and UI flow.

## MVP Acceptance Criteria Baseline

- create Card Reprint request.
- distinguish Initial Print vs Reprint where supported.
- require configured approval for Reprint when applicable.
- prevent print/release before required approval.
- prevent print/release before CONFIRMED payment for paid Reprint.
- calculate Reprint fee from configurable price/effective-date source.
- create/link payment transaction through Payment Foundation.
- show request status and payment status.
- allow authorized user to mark printed.
- allow authorized user to mark released.
- enforce permission-gated actions.
- record audit trail.
- handle rejection safely.
- show safe errors for 400/403/404/409/500.
- exclude refund/cancellation/partial payment.
- exclude Care Package Sales.
- exclude production migration/tag/push.

## Implementation Readiness Assessment

READY FOR DETAILED SCOPE ACCEPTANCE REVIEW

All blockers have been resolved by the Project Owner decision response. The detailed scope is now aligned with those decisions. Implementation remains unauthorized until the detailed scope acceptance is explicitly passed by the Project Owner.

## Recommended Next Gate

Authorized next task:
Project Owner Phase 1B.8-A detailed scope acceptance review.

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

## Risks

- Initial Print fee deferred unless source-supported.
- dynamic PDF/template generation deferred.
- generic Payment Print UI deferred.
- physical inventory/stamp stock management deferred.
- Care Package Sales deferred.
- production rollout deferred.
- migration risk remains for implementation planning.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.

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
