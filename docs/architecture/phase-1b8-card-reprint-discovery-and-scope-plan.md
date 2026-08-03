# Phase 1B.8 Card Reprint / Grave Card Reprint Discovery and Scope Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER SCOPE ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Post-1B.7 Project Owner next-work decision commit:
  19011ef9cf8605cc94709d6bb29367290bc7715f

State:
- Phase 1B.7 is closed.
- Phase 1B.8 Card Reprint / Grave Card Reprint was selected as next work.
- This document is discovery and scope planning only.
- This document does not authorize implementation.

## Source Documents Reviewed

- docs/architecture/post-1b7-project-owner-next-work-decision.md
- docs/architecture/post-1b7-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b7-project-owner-closure-acceptance.md
- docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md
- docs/architecture/phase-1b6-project-owner-closure-acceptance.md
- docs/business/process-catalog.md
- docs/business/permission-catalog.md
- docs/business/business-rules.md
- docs/business/PTKD-Specification-v1.1.md

## Confirmed Business Context

- Card Reprint (`CARD_REPRINT`) is a defined business process in the process catalog.
- Approval mode is `CONDITIONAL`. Condition fields include `company_id`, `previous_print_count`, `reprint_number`, `fee_amount`, `reason_code`.
- First issue (first print) requires no approval.
- Workflow/approval may apply from the second print onward.
- A permission `CARD_REPRINT_APPROVE` (delegable) exists at the `COMPANY` scope.
- Cards are considered service artifacts that depend on the Service domain.
- The Service module was previously implemented to support reference by future Card entities.

## Assumptions Not Made

- No assumption is made about the exact cost/fee of the reprint (e.g., whether 50k is hardcoded or relies on a price configuration table).
- No assumption is made about whether payment must occur before or after approval.
- No assumption is made about how physical stamp / card custody logic must be tracked in the database.
- No assumption is made about whether a single request can include multiple grave cards simultaneously.
- No assumption is made about the final PDF or print template generation mechanism.

## Business Scope Discovery

The business purpose of this feature is to manage the lifecycle of issuing and re-issuing physical Grave Cards to customers.
- **New Print / First Print:** Documented as not requiring approval.
- **Reprint / Second-or-later Print:** Workflow may apply depending on conditions (reprint number, fee, etc.). Requires approval permission `CARD_REPRINT_APPROVE`.
- **Payment:** A fee applies. Integration with the Phase 1B.7 Payment Foundation is required, but timing relative to approval is an open decision.
- **Physical Stamp / Custody:** Handling physical cards or stamps is conceptually part of the process, but system tracking specifics require clarification.
- **Roles:** Requester role needs process-specific submit permission. Approvers are determined via workflow binding and `CARD_REPRINT_APPROVE` permission.
- **Audit/Notifications:** Standard audit for workflow actions and state transitions applies. Notification needs specific to Card Reprint must be defined.
- **Reporting:** Out of scope unless specified.

## Workflow and Approval Discovery

- `CARD_REPRINT` is a workflow-enabled process with `CONDITIONAL` approval mode.
- Approval flow should be dynamically configurable using whitelisted condition fields (`company_id`, `previous_print_count`, `reprint_number`, `fee_amount`, `reason_code`).
- Approval assignment can be dynamically bound to departments/roles using the Workflow Engine.
- If approval assignment changes mid-flight, workflow snapshot rules apply (request retains original workflow version).
- Delegation is supported (the `CARD_REPRINT_APPROVE` permission is delegable).
- Open decisions remain on whether rejection ends the request or returns it, and whether physical stamping requires a distinct approval/confirmation step.

## Payment and Service Dependency Discovery

- **Service Module (1B.6):** Cards reference the Service entity.
- **Payment Foundation (1B.7):** Fees for reprint must be processed as payment items. It's an open decision whether the fee uses the standard price override mechanism or is a specialized service type.
- Refund, cancellation, and partial payment are out of scope (as per existing rules).
- Payment correction/reconciliation flows from 1B.7 will naturally cover reprint payments once confirmed.

## Proposed Phase 1B.8 Scope

### In Scope

- Card entity schema and lifecycle tracking (first print vs reprint).
- Card Reprint request lifecycle and UI.
- Integration with Workflow Engine for `CARD_REPRINT` approvals.
- Integration with Payment Foundation to collect reprint fees.
- Execution handler `CARD_REPRINT_FROM_APPROVAL` to finalize card status.
- API endpoints for submitting, viewing, and acting on requests.

### Out of Scope

- production migration.
- release tag.
- push.
- Care Package Sales.
- unrelated payment changes.
- refunds.
- cancellation.
- partial payment.
- generic print module unless explicitly accepted.
- direct PDF/template generation if not confirmed.

## Proposed Request Lifecycle

1. Requester selects a Service and initiates a Card Reprint request.
2. System determines `previous_print_count` and `reprint_number`.
3. If first print: System bypasses approval and moves to Execution/Payment (Open Decision on exact step order).
4. If second-or-later print: System evaluates Workflow conditions based on `company_id`, `fee_amount`, `reason_code`, etc.
5. System routes request to Approver(s).
6. Approver(s) Approve, Return, or Reject (Open Decision on Rejection flow).
7. If Approved, the execution handler is invoked.
8. Payment is captured (Open Decision on whether payment precedes step 4 or follows step 7).
9. Card is physically printed/stamped and system marked as PRINTED/DELIVERED (Open Decision on stamp tracking).

## Proposed Data Scope

- New table `Cards` mapping to `Services`.
- New table `Card_Reprint_Requests` (if not reusing generic workflow payload).
- Reuse existing workflow tables (`Approval_Request_Steps`, `Approval_Actions`).
- Reuse existing payment tables (`Payment_Transactions`, `Payment_Transaction_Items`).

## Proposed Backend/API Scope

- `POST /api/v2/cards/reprint-requests`
- `GET /api/v2/cards/reprint-requests`
- `GET /api/v2/cards/reprint-requests/{id}`
- Workflow execution handler `CardReprintExecutionHandler`.
- Domain rules validating service state and previous print counts.
- Permission enforcement for submit and approve actions.

## Proposed Frontend Scope

- Card Reprint Request List Page.
- Create Card Reprint Request Form (selecting service, entering reason).
- Card Reprint Request Detail Page (showing workflow status, previous prints).
- Payment link / status component integration.
- Print / physical release status toggle.

## Proposed Permission Scope

- `CARD_REPRINT_REQUEST_CREATE` (Submit request)
- `CARD_REPRINT_REQUEST_VIEW` (View requests)
- `CARD_REPRINT_APPROVE` (Existing, approve request)
- `CARD_REPRINT_REQUEST_REJECT` (Reject request)
- `CARD_REPRINT_REQUEST_MARK_PRINTED` (Confirm physical printing)
- `CARD_REPRINT_REQUEST_ADMIN` (Admin overrides/corrections)

## Proposed Test Strategy

- Unit tests for domain logic (first print vs reprint rules).
- Workflow integration tests for `CARD_REPRINT` conditions.
- Payment integration tests ensuring reprint fee correctly binds to transaction.
- API tests for permission checks.
- Frontend tests for form validation and workflow UI rendering.
- Migration/rollback scripts for `Cards` table.

## Open Decisions and Blockers

| ID | Decision Needed | Why It Matters | Blocking? | Proposed Owner |
|---|---|---|---|---|
| OD-1B8-001 | exact new print vs reprint terminology. | Ensures UI and data model align with business language. | Yes | PO / BA |
| OD-1B8-002 | first print approval requirement. | Confirming the process catalog note that first print bypasses workflow. | Yes | PO / BA |
| OD-1B8-003 | second-or-later print approval levels. | Defines baseline workflow configuration. | Yes | PO / BA |
| OD-1B8-004 | fee model and whether 50k is configurable price. | Impacts dependency on service pricing catalog vs hardcoded fee. | Yes | PO / BA |
| OD-1B8-005 | payment timing relative to approval. | Does a user pay before it goes to approval, or only after approved? | Yes | PO / BA |
| OD-1B8-006 | physical stamp / card custody handling. | Do we need a "Mark Stamped" step in the system? | No | PO / BA |
| OD-1B8-007 | requester and approver role mapping. | Needed to configure test/seed data. | No | PO / BA |
| OD-1B8-008 | workflow snapshot/versioning rule. | Confirms standard workflow versioning applies here. | No | DEV |
| OD-1B8-009 | whether rejection ends or returns request. | Dictates allowed state transitions for approvers. | Yes | PO / BA |
| OD-1B8-010 | reporting requirements. | May require additional audit tracking. | No | PO / BA |
| OD-1B8-011 | notification requirements. | Determines email/alert implementation scope. | No | PO / BA |
| OD-1B8-012 | print output/PDF/template scope. | Are we generating PDFs or just tracking state? | Yes | PO / BA |
| OD-1B8-013 | whether Payment Print UI remains deferred or becomes part of Card Reprint. | Potentially bundled feature from 1B.7 deferred items. | Yes | PO / BA |
| OD-1B8-014 | database impact and migration need. | Finalizing exact entity schema. | Yes | DEV |
| OD-1B8-015 | acceptance criteria gaps. | We need formal acceptance criteria to validate against. | Yes | PO / BA |

## Risks

- approval complexity based on missing rules.
- payment timing ambiguity (pre-pay vs post-pay).
- physical card/stamp process ambiguity could lead to incomplete tracking.
- print/PDF/template ambiguity could result in massive scope creep if PDF generation engine is required.
- scope creep into generic printing if not tightly bounded to Grave Cards.
- dependency on workflow configuration stability.
- dependency on price configuration if reprint fee is dynamic.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.

## Recommended Next Gate

Recommended next authorized task:
Project Owner Phase 1B.8 scope acceptance.

Do not authorize:
- implementation,
- source code changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- production migration,
- release tag,
- push.

## Non-Goals

Confirm this document does not:
- implement Card Reprint.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- run production migration.
- create release tag.
- push.
