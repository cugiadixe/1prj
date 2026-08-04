# Phase 1B.8-A Card Reprint Open Decisions and Detailed Scope Clarification

## Status

BLOCKED — REQUIRES PROJECT OWNER DECISIONS BEFORE IMPLEMENTATION PLANNING

## Authorization Source

Reference:
- Phase 1B.8 Project Owner scope acceptance commit:
  8f58c813f6475a03090dc7da24a6515d4a805611

State:
- Phase 1B.8 scope is accepted as working baseline.
- This document resolves or classifies open decisions.
- This document is detailed scope clarification only.
- This document does not authorize implementation.

## Source Documents Reviewed

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
| OD-1B8-001 | exact new print vs reprint terminology | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to define exact terms (First Issue, New Print, Reprint). | Yes | Not in source docs |
| OD-1B8-002 | first print approval requirement | RESOLVED BY SOURCE DOCUMENTS | First issue requires no approval. | No | `docs/business/process-catalog.md` |
| OD-1B8-003 | second-or-later print approval levels | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose conditional workflow using standard dynamic configuration. | No | `docs/business/process-catalog.md` states workflow may apply. |
| OD-1B8-004 | fee model and whether 50k is configurable price | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to define if 50k is hardcoded or relies on price tables. | Yes | Not in source docs |
| OD-1B8-005 | payment timing relative to approval | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to define if payment is before or after approval. | Yes | Not in source docs |
| OD-1B8-006 | physical stamp / card custody handling | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to define physical custody tracking steps in system. | Yes | Not in source docs |
| OD-1B8-007 | requester and approver role mapping | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose using standard permissions mapped via workflow engine. | No | `docs/business/process-catalog.md` |
| OD-1B8-008 | workflow snapshot/versioning rule | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose standard Phase 1B.3 behavior: running requests retain original snapshot. | No | Phase 1B.3 foundation |
| OD-1B8-009 | whether rejection ends or returns request | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose standard Phase 1B.3 behavior: rejection ends request. | No | Phase 1B.3 foundation |
| OD-1B8-010 | reporting requirements | DEFERRED — NOT REQUIRED FOR PHASE 1B.8 IMPLEMENTATION | Standard lists only, no custom reports yet. | No | Out of scope |
| OD-1B8-011 | notification requirements | DEFERRED — NOT REQUIRED FOR PHASE 1B.8 IMPLEMENTATION | Notifications deferred. | No | Out of scope |
| OD-1B8-012 | print output/PDF/template scope | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to define if system generates PDFs or just tracks status. | Yes | Not in source docs |
| OD-1B8-013 | whether Payment Print UI remains deferred or becomes part of Card Reprint | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Need PO to decide UI integration. | Yes | Not in source docs |
| OD-1B8-014 | database impact and migration need | PROPOSED FOR PROJECT OWNER CONFIRMATION | Propose `Cards` and `Card_Reprint_Requests` tables. | No | Based on standard design |
| OD-1B8-015 | acceptance criteria gaps | BLOCKING — REQUIRES PROJECT OWNER ANSWER | Missing AC for exact UI steps and validations. | Yes | Not in source docs |

## Confirmed Business Rules

- Card Reprint (`CARD_REPRINT`) is a `CONDITIONAL` approval process.
- Condition fields for approval include: `company_id`, `previous_print_count`, `reprint_number`, `fee_amount`, `reason_code`.
- First issue requires no approval.
- Workflow may apply from the second print onward.

## Proposed Rules Requiring PO Confirmation

- Conditional workflow configuration for second-or-later prints should use standard Phase 1B.3 dynamic rules.
- Requester and approver role mapping should use the standard permission-based workflow bindings.
- In-flight requests should retain their original workflow version (snapshot).
- Rejection should terminate the request; returns require a new submission round.
- New database tables `Cards` and `Card_Reprint_Requests` should be introduced.

## Blocking Decisions Remaining

- OD-1B8-001: What is the exact official terminology (First Issue, New Print, Reprint) to use in UI and DB?
- OD-1B8-004: Is the reprint fee (e.g. 50k) hardcoded, or does it require configurable price tables?
- OD-1B8-005: Must payment be completed before approval can begin, or after approval is granted?
- OD-1B8-006: How should physical stamp/custody be tracked? Is there a distinct "Stamp" step?
- OD-1B8-012: Does the system need to generate printable PDFs/templates for the card, or only track the print status?
- OD-1B8-013: Does the Phase 1B.7 Payment Print UI need to be built now to support Card Reprint receipts?
- OD-1B8-015: Acceptance criteria for the exact validation and UI flow are missing and must be provided.

## Refined Request Lifecycle

1. (Source-confirmed) Requester selects a Service and initiates a Card Reprint request.
2. (Source-confirmed) System determines `previous_print_count` and `reprint_number`.
3. (Source-confirmed) System validates customer/grave/service references.
4. (Source-confirmed) If first print, bypass approval and proceed to next step.
5. (Proposed) If second-or-later print, create workflow instance based on condition fields.
6. (Proposed) Workflow captures snapshot of configuration.
7. (Proposed) Approvers approve or reject (rejection terminates request).
8. (Blocking) Payment draft creation timing (before or after approval?).
9. (Blocking) Payment confirmation.
10. (Blocking) Physical stamp/card printing step and tracking.
11. (Proposed) System marks request as COMPLETED.
12. (Source-confirmed) Audit trail records all transitions.
13. (Deferred) Notifications.

## Refined Approval / Workflow Scope

- Process Key: `CARD_REPRINT`.
- Approval Rule: First print requires no approval. Second-or-later prints require conditional approval.
- Assignment: Dynamic Admin-configured approval flow assignment based on `CARD_REPRINT_APPROVE` permission.
- Delegation: Standard Phase 1B.3 delegation applies.
- Snapshot/Versioning: Running requests use the workflow version active at creation time.
- Rejection: Rejection terminates the request workflow.
- Re-submit: Requires a new request.
- Audit: Phase 1B.3 standard audit logs for state changes.
- Permissions: Execution requires `CARD_REPRINT_APPROVE` matching the company scope.

## Refined Payment / Service Scope

- Service Modeling: Card Reprint is modeled as a service action related to the Grave/Card.
- Fee Model: (Blocking) Pending PO decision on configurable pricing vs fixed fee.
- Payment Timing: (Blocking) Pending PO decision on timing relative to approval.
- Payment Correction: Phase 1B.7 payment correction rules apply.
- Reconciliation: Payments will be included in daily/monthly reconciliation.
- Out of Scope: Refunds, cancellations, partial payments remain out of scope. Confirmed payment remains non-deletable.

## Refined Data Scope

- Cards: (Proposed) Tracks the current card state, references Grave/Customer/Service. Key fields: `Id`, `CompanyId`, `GraveId`, `ServiceId`, `PrintCount`, `Status`, `RowVersion`. Required for MVP.
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
  - `POST /api/v2/cards/reprint-requests/{id}/mark-printed` (Deferred/Blocking)
- Validation: Ensure valid service/grave, enforce permission scope.
- Workflow/Payment: Integration points based on pending PO decisions.
- Tests: Domain unit tests, integration tests for workflow/payment, API tests for authorization and concurrency. (Required)

## Refined Frontend Scope

- Routes: `/cards/reprint-requests`, `/cards/reprint-requests/:id`. (Required)
- List Page: Data grid showing requests, statuses, payment links. (Required)
- Create Page: Form to select grave/service, show calculated fee, submit. (Required)
- Detail Page: Shows workflow timeline, payment status, print toggle. (Required)
- Approval UI: Standard approve/reject action buttons. (Required)
- Error Handling: Safe rendering of validation and 409 errors. (Required)
- Tests: Component and page-level frontend tests. (Required)

## Refined Permission Scope

- `CARD_REPRINT_REQUEST_CREATE`: (Proposed) Allows submitting requests. Company scope. Assigned to staff. Required for MVP.
- `CARD_REPRINT_REQUEST_VIEW`: (Proposed) Allows viewing requests. Company scope. Assigned to staff/managers. Required for MVP.
- `CARD_REPRINT_APPROVE`: (Source-supported) Allows approving requests. Company scope. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_REJECT`: (Proposed) Allows rejecting requests. Company scope. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_MARK_PRINTED`: (Proposed) Allows marking card as printed/delivered. Company scope. Assigned to admin/printing staff. Required for MVP.
- `CARD_REPRINT_REQUEST_ADMIN`: (Proposed) Allows administrative overrides. Global scope. Assigned to system admins. Deferred.

## Refined Test Strategy

- Backend:
  - Unit tests for domain logic (first print vs reprint evaluation).
  - Workflow integration tests for condition evaluation and state changes.
  - Payment integration tests ensuring fee creation.
  - Authorization tests for all endpoints.
  - Concurrency tests (409) for parallel request processing.
- API:
  - Full lifecycle tests from creation to completion.
  - Invalid state transition tests.
  - Forbidden/unauthorized tests.
- Frontend:
  - List/detail page rendering tests.
  - Permission-gated UI elements test (buttons hidden if no permission).
  - Validation error display tests.
- Operational:
  - Manual checklist covering headless flow and UI flow.

## Implementation Readiness Assessment

NOT READY — BLOCKING DECISIONS REMAIN

Implementation cannot begin because fundamental workflow (payment timing, fee modeling, physical custody tracking, output generation) remains undefined by the Project Owner.

## Recommended Next Gate

Recommended next authorized task:
Project Owner Phase 1B.8-A blocker decision response.

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

- approval complexity.
- payment timing ambiguity.
- physical card/stamp ambiguity.
- print/PDF/template ambiguity.
- scope creep into generic printing.
- workflow configuration dependency.
- price configuration dependency.
- migration risk if data impact is confirmed.
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
