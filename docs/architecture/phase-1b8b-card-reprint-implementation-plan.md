# Phase 1B.8-B Card Reprint Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER IMPLEMENTATION PLAN ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.8-A Project Owner detailed scope acceptance commit:
  accac0eddff4eca889d545bd729b2d9109f4ce44

State:
- Phase 1B.8-A detailed scope is accepted.
- This document is implementation planning only.
- This document does not authorize implementation.

## Source Documents Reviewed

- docs/architecture/phase-1b8a-project-owner-detailed-scope-acceptance.md
- docs/architecture/phase-1b8a-detailed-scope-updated-acceptance-review.md
- docs/architecture/phase-1b8a-card-reprint-open-decisions-and-detailed-scope.md
- docs/architecture/phase-1b8a-project-owner-blocker-decision-response.md
- docs/architecture/phase-1b8-project-owner-scope-acceptance.md
- docs/architecture/phase-1b8-card-reprint-discovery-and-scope-plan.md
- docs/business/business-rules.md
- docs/business/process-catalog.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- PTKD-ERP-Master-Context.md
- docs/architecture/project-readiness-review.md
- docs/architecture/phase-1b3-project-owner-closure-acceptance.md
- docs/architecture/phase-1b6-project-owner-closure-acceptance.md
- docs/architecture/phase-1b7-project-owner-closure-acceptance.md

## Accepted Scope Summary

Card Reprint is a conditional workflow-enabled process that leverages Phase 1B.3 workflow and Phase 1B.7 payment foundations. 

Key constraints:
- **Terminology**: Initial Print (no approval) and Reprint (conditional approval). System must track print history.
- **Fee Model**: Reprint fee is 50,000 VND (configurable). Initial Print fee is deferred unless source-supported. No hard-coded fees.
- **Timing**: Approval must complete before payment draft creation. Payment must be CONFIRMED before physical print/release completion.
- **Physical Custody**: MVP tracks status only (printed, released, actor, timestamp).
- **Out of Scope**: Refunds, cancellations, partial payments, dynamic PDF/template generation, Care Package Sales, generic Payment Print UI, physical inventory management.

## Recommended Implementation Sequence

- **Phase 1B.8-B1: Backend/Data Foundation Implementation**: Create DB migration (V0013/U0013), domain models, application services, integration tests. Gate: PR/Validation.
- **Phase 1B.8-B2: Workflow & Payment Integration & APIs**: Integrate with workflow/payment foundations, expose API controllers, permission seeds, API tests. Gate: PR/Validation.
- **Phase 1B.8-C: Frontend Implementation**: Create UI pages (list, detail, create), permission gating, error handling, frontend tests. Gate: PR/Validation.
- **Phase 1B.8-D: Operational Validation and Closure**: End-to-end testing, PO acceptance. Gate: Project Owner Phase 1B.8 closure.

## Data / Database Plan

Proposed Migration: `V0013__card_reprint_foundation.sql` (and rollback `U0013__card_reprint_foundation.sql`).

Proposed Entities:
- `Cards` (Required for MVP). Key fields: `Id`, `CompanyId`, `GraveId`, `ServiceId`, `PrintCount`, `Status`, `RowVersion`.
- `Card_Reprint_Requests` (Required for MVP). Key fields: `Id`, `CompanyId`, `CardId`, `RequesterId`, `ReprintNumber`, `FeeAmount`, `ReasonCode`, `WorkflowInstanceId` (nullable), `PaymentTransactionId` (nullable), `Status`, `RowVersion`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
- `Card_Reprint_Request_Items` (Not needed for MVP, assumed 1 card per request unless otherwise specified).

Relationships:
- Links to `Workflow_Instances` (1:1 or 1:0 for Initial Prints).
- Links to `Payment_Transactions` (1:1 or 1:0 for free/Initial Prints).
- Links to `Service_Items` (for pricing via standard references).
- Links to `Customers`, `Graves`, `Companies` based on existing system patterns.

Concurrency: Uses `RowVersion` (optimistic concurrency).
Soft Delete: Not used (financial/workflow requests are permanent records).

## Backend / API Plan

**Module Location**: `src/backend/PTKD.CardReprint/` or similar slice.

**Controllers**: `CardReprintRequestsController`
**Application Services**: `CardReprintRequestService`
**Domain Services**: `CardReprintDomainService` (handles Initial Print vs Reprint condition logic)

**DTOs**: `CreateCardReprintRequestDto`, `CardReprintRequestDetailDto`, `CardReprintRequestListDto`

**Endpoints**:
- `POST /api/v2/cards/reprint-requests`
- `GET /api/v2/cards/reprint-requests`
- `GET /api/v2/cards/reprint-requests/{id}`
- `POST /api/v2/cards/reprint-requests/{id}/mark-printed`
- `POST /api/v2/cards/reprint-requests/{id}/mark-released`

**Rules**:
- Enforce strict validation of Grave/Card/Service references.
- Track print count by checking `Cards.PrintCount` or evaluating `Card_Reprint_Requests` history.
- Ensure CONFIRMED payment is present before allowing `mark-printed` or `mark-released` if a fee applies.
- Use ADO.NET SQL Server transactions for atomic status changes (approval -> payment draft).

## Workflow / Approval Integration Plan

- **Process Key**: `CARD_REPRINT`
- **Assignment**: Dynamic Admin-configured approval flow assignment based on `CARD_REPRINT_APPROVE` permission.
- **Snapshot/Versioning**: Request binds to the workflow definition active at creation time.
- **Delegation/Resolution**: Standard Phase 1B.3 foundation capabilities apply (department/title/position resolution).
- **Rejection**: Rejection safely terminates the request. No mutation permitted after rejection. Resubmission requires a new request.
- **Audit**: Phase 1B.3 standard logging.
- **Tests**: Workflow integration tests mapped to conditional rules.

## Payment / Service Integration Plan

- **Pricing**: System looks up Reprint service ID in standard price tables to determine the effective fee (50,000 VND configuration) and effective date.
- **Payment Timing**: After required approval completes (or immediately for Initial Print, if fee applies), system creates a `Payment_Transactions` draft via Phase 1B.7 Payment Foundation.
- **Link**: Request `PaymentTransactionId` is set to the generated transaction.
- **Gating**: API endpoints for marking as printed/released will check if the linked transaction status is `CONFIRMED`.
- **Reconciliation**: Seamlessly handled via standard `Payment_Transactions` integration.
- **Exclusions**: Payment mutation (refund, partial, cancellation) is strictly forbidden for reprints.

## Frontend Plan

**Module Structure**: `src/frontend/src/cards/reprint-requests/`
**API Client**: Axios-based client for Card Reprint.
**Types**: DTO-mapped TypeScript interfaces.
**Routes**: 
- `/cards/reprint-requests`
- `/cards/reprint-requests/:id`
- `/cards/reprint-requests/create`

**Pages**:
- **List Page**: Data grid showing requests, statuses, payment status, reprint number.
- **Create Page**: Grave/service selector. Automatic fee calculation based on configured price/effective-date.
- **Detail Page**: Timeline of request, workflow approval/rejection UI, payment reference/link, mark printed UI, mark released UI.

**Behaviors**:
- Permission-gated rendering.
- Safe error handling (400, 403, 404, 409).
- Excludes: Generic Payment Print UI and dynamic PDF template generation UI.

## Permission Plan

Candidate permissions (all proposed as `COMPANY` scope):
- `CARD_REPRINT_REQUEST_CREATE`: Allows submitting requests. Assigned to staff. Required for MVP.
- `CARD_REPRINT_REQUEST_VIEW`: Allows viewing requests. Assigned to staff/managers. Required for MVP.
- `CARD_REPRINT_APPROVE`: Allows approving requests. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_REJECT`: Allows rejecting requests. Assigned to managers/directors. Required for MVP.
- `CARD_REPRINT_REQUEST_MARK_PRINTED`: Allows marking card as printed/released. Assigned to admin/printing staff. Required for MVP.
- `CARD_REPRINT_REQUEST_ADMIN`: Allows administrative overrides. (Deferred)

Changes to permission catalog are not authorized in this planning task and must be part of implementation planning execution.

## Test Strategy

- **Backend Unit Tests**: Test classification of Initial vs Reprint, fee evaluation, payment timing gate, and rejection.
- **Integration Tests**: Test DB insertion (optimistic concurrency), workflow condition evaluation, payment draft generation, migration execution.
- **API Tests**: Tests covering full request lifecycle (`create -> approve -> payment -> mark-printed -> mark-released`), unauthorized responses, and invalid lifecycle transitions.
- **Frontend Tests**: Component rendering, conditional UI (buttons hidden if unapproved or unpaid), form validation.
- **Operational**: End-to-end manual QA checklist.

## Validation Strategy

At completion of each sub-phase, validation evidence must include:
- exact commands run.
- `git diff --name-status` summary of changes.
- Test execution outputs (`dotnet test`, `npm test`).
- API endpoint invocation logs or Swagger evidence.
- DB migration verification scripts.

## Boundaries / Non-Goals

Explicitly excluded:
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

## Risks / Open Questions

- **Non-blocking risk**: Whether `Cards` table strictly exists in production or relies on `Grave` properties. If missing, `V0013` will create it.
- **Non-blocking risk**: Exact `ServiceItem` ID mapping for the 50,000 VND reprint fee. Can be seed-driven.
- **Deferred risk**: Notification implementation remains deferred.

These questions do not block implementation planning acceptance.

## Implementation Readiness Recommendation

READY FOR PROJECT OWNER IMPLEMENTATION PLAN ACCEPTANCE

## Recommended Next Gate

Recommended next authorized task:
Project Owner Phase 1B.8-B implementation plan acceptance.

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
