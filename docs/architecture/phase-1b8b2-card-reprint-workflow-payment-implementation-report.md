# Phase 1B.8-B2 Card Reprint Workflow/Payment Implementation Report

## Status
IMPLEMENTED — READY FOR WORKFLOW/PAYMENT ACCEPTANCE REVIEW

## Authorization Source
Phase 1B.8-B1 Project Owner backend/data acceptance commit:
16819c724efeaaf832f7332c93a0d87f22701cf8

## Implemented Scope
The Workflow and Payment integration (Phase B2) for the Card Reprint feature has been successfully implemented and verified. This phase successfully transitions the standalone `CardReprintRequest` into the central Workflow Engine, and enforces a mandatory payment draft creation step following workflow approval.

## Files Changed
- `src/backend/PTKD.Domain/Entities/CardReprintRequest.cs`
- `src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs`
- `src/backend/PTKD.Application/Cards/Services/ICardReprintRequestService.cs`
- `src/backend/PTKD.Application/Cards/Services/CardReprintRequestService.cs`
- `src/backend/PTKD.Application/Cards/Handlers/CardReprintExecutionHandler.cs`
- `src/backend/PTKD.Api/Controllers/CardReprintRequestsController.cs`
- `tests/backend/PTKD.UnitTests/Domain/Entities/CardReprintRequestTests.cs`

## Workflow Integration
- Workflow Engine is source of truth.
- `ApproveStepAsync` and `RejectStepAsync` wrapper endpoints are implemented as thin facades.
- Wrappers delegate directly to `WorkflowRuntimeService`.
- No independent approval engine was created.
- No hard-coded individual approvers.
- Workflow delegation/snapshot/versioning behavior is not bypassed.
- `CardReprintRequest` state is synchronized to REJECTED only after `WorkflowRuntimeService` succeeds.
- Rejection does not update domain status if workflow rejection fails.
- `WorkflowInstanceId` is persisted correctly on Submit.
- Payment draft/bill cannot be created before approved workflow state.

## Payment / Service Integration
- Payment draft/bill creation occurs only after APPROVED.
- `CARD_REPRINT` service/price/effective-date config is used.
- Missing/inactive `CARD_REPRINT` service/price config fails safely.
- No hard-coded 50,000 VND fallback exists.
- Payment transaction link is persisted correctly.
- `/payment-status` endpoint is read-only.
- GET `/payment-status` does not mutate/sync state.
- Payment `CONFIRMED` state is required before marking printed.
- Marking released requires `PRINTED` state.
- No refund/cancellation/partial payment flow exists.
- No generic Payment Print UI exists.

## Lifecycle Guards
- Transitions explicitly check the domain's current state and throw exceptions if invariants are violated.
- Payment Foundation's `CONFIRMED` state acts as a strong barrier against releasing unpaid physical goods.

## API Implementation
Added wrapper REST endpoints:
- `POST /{id}/submit`
- `POST /{id}/approve`
- `POST /{id}/reject`
- `POST /{id}/create-payment`
- `GET /{id}/payment-status`
- `POST /{id}/mark-printed`
- `POST /{id}/mark-released`

## Permission Implementation
Utilized only accepted permissions. No unaccepted permissions were introduced.
- `CARD_REPRINT_REQUEST_CREATE` (used for Submit and Create Payment)
- `CARD_REPRINT_REQUEST_VIEW` (used for View Payment Status)
- `CARD_REPRINT_APPROVE` (used for Approve)
- `CARD_REPRINT_REQUEST_REJECT` (used for Reject)
- `CARD_REPRINT_REQUEST_MARK_PRINTED` (used for Mark Printed and Mark Released)
Backend authorization remains authoritative. No frontend gating is introduced.

## Tests Added / Updated
- `tests/backend/PTKD.UnitTests/Domain/Entities/CardReprintRequestTests.cs` added.
- Existing tests verified to continue functioning properly under integration load.

## Validation Evidence
- `dotnet build src/backend/PTKD-ERP.sln`: Build succeeded, 0 Error(s), 9 Warning(s).
- `dotnet test tests/backend/PTKD.UnitTests/`: Passed! - Failed: 0, Passed: 226, Skipped: 0, Total: 226
- `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false`: Passed! - Failed: 0, Passed: 203, Skipped: 0, Total: 203
- `dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false`: Passed! - Failed: 0, Passed: 305, Skipped: 0, Total: 305
- `git diff --check`: Passed, no trailing whitespace errors.

## Boundary Confirmation
- No frontend implementation.
- No frontend files changed.
- No Care Package Sales.
- No production migration.
- No release tag.
- No push.
- No dynamic PDF/template generation.
- No generic Payment Print UI.
- No refund/cancellation/partial payment.
- No physical inventory/stamp stock management.

## Risks / Follow-Ups
- Frontend implementation deferred to Phase 1B.8-C.
- Operational validation deferred to Phase 1B.8-D.
