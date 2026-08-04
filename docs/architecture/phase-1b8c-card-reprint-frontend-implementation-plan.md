# Phase 1B.8-C Card Reprint Frontend Implementation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER FRONTEND PLAN ACCEPTANCE

## Authorization Source

Reference:
- Phase 1B.8-B2 Project Owner workflow/payment acceptance commit:
  edda862664724dd4c65373a6280bfa1e8881e1e0

## Planning Scope

This document represents frontend planning only. No implementation code has been written or modified.

## Backend/API Inputs

The frontend will consume the following backend APIs via `/api/v2/card-reprint-requests`:
- `GET /` (List requests)
- `POST /` (Create draft)
- `GET /{id}` (Get details)
- `POST /{id}/submit` (Submit workflow)
- `POST /{id}/approve` (Workflow approval step)
- `POST /{id}/reject` (Workflow rejection step)
- `POST /{id}/create-payment` (Initiate payment)
- `GET /{id}/payment-status` (Check payment status)
- `POST /{id}/mark-printed` (Mark as printed)
- `POST /{id}/mark-released` (Mark as released)

## Proposed Frontend Scope

The Phase 1B.8-C frontend implementation will provide the user interface for end-to-end Card Reprint request handling. This includes request creation, viewing lists and details, taking workflow actions (submit, approve, reject), taking payment actions (drafting and viewing status), and logging physical handling events (printing and releasing the card).

## Routes / Pages

- `/cards/reprints`: The main list page showing tabular data for card reprint requests.
- `/cards/reprints/new`: The form page to create a new card reprint request.
- `/cards/reprints/:id`: The detail page showing request status, workflow history, payment status, and available lifecycle actions.

## Components

- `CardReprintList`: Data table component fetching and displaying paginated lists.
- `CardReprintForm`: Form for capturing request details (Card ID, Reason, Notes).
- `CardReprintDetail`: Main container for a single request.
- `CardReprintStatusBadge`: Visual indicator for domain status (DRAFT, PENDING_APPROVAL, PENDING_PAYMENT, PAID, PRINTED, RELEASED, REJECTED).
- `WorkflowActionGroup`: Buttons for Submit, Approve, and Reject (includes modal for comment/reason).
- `PaymentActionGroup`: Buttons/Displays for "Create Payment" and "Check Status".
- `PhysicalHandlingActionGroup`: Buttons for "Mark Printed" and "Mark Released".

## API Client / Hooks Plan

- `useCardReprintRequests`: Fetches the list with SWR or React Query.
- `useCardReprintRequest(id)`: Fetches individual request details.
- `cardReprintApiClient`: Contains typed Axios/fetch functions corresponding to the REST endpoints listed above. Includes appropriate request/response DTO typings matching `CardReprintRequestDto`.

## Permission-Gated UI Plan

Actions and UI elements will be conditionally rendered based on permissions:
- Creating/Submitting/Paying: Requires `CARD_REPRINT_REQUEST_CREATE`
- Viewing/Listing: Requires `CARD_REPRINT_REQUEST_VIEW`
- Approving: Requires `CARD_REPRINT_APPROVE`
- Rejecting: Requires `CARD_REPRINT_REQUEST_REJECT`
- Printing/Releasing: Requires `CARD_REPRINT_REQUEST_MARK_PRINTED`

## Lifecycle UI Plan

Buttons will be selectively enabled based on the request's current `Status`:
- `DRAFT`: Show "Submit"
- `PENDING_APPROVAL`: Show "Approve", "Reject"
- `APPROVED`: Show "Create Payment Draft"
- `PENDING_PAYMENT`: Show "Check Payment Status"
- `PAID`: Show "Mark Printed"
- `PRINTED`: Show "Mark Released"
- `RELEASED` / `REJECTED`: Terminal states (hide all actionable buttons)

## Workflow UI Plan

- **Submit**: A direct action button triggering a confirmation modal.
- **Approve/Reject**: Will open a modal requesting an optional comment or required reason code, which submits the `{ StepId, TargetVersion, Reason, Comment }` payload.

## Payment UI Plan

- **Create Payment Draft**: Action button visible on `APPROVED` status.
- **Payment Status**: While in `PENDING_PAYMENT`, a "Refresh Status" button will call the read-only payment-status API to update the view. If the API returns `CONFIRMED`, the local UI will prompt the user to refresh the overall request.
- **Payment Link**: If `paymentTransactionId` exists, display a deep link to the central Payment module.

## Print / Release UI Plan

- **Mark Printed**: Direct action button calling the backend API. It handles safe 400s if the payment is not confirmed.
- **Mark Released**: Direct action button confirming the handover of the physical card to the user.

## Error / Empty / Loading State Plan

- Loading spinners/skeletons during API calls.
- Empty state designs for the list page when no requests exist.
- Form/Action error messages caught and displayed via standard toast notifications (especially HTTP 400 `BusinessRuleValidationException`, HTTP 403 `Forbidden`, and HTTP 409 `Concurrency`).

## Frontend Test Plan

- **Component Tests (Vitest/RTL)**: Test rendering and conditional button display logic based on status and permissions.
- **Form Validation Tests**: Ensure required fields are met before calling API.
- **API Mocking**: Mock backend responses to test UI state transitions without network calls.
- **Error Handling Tests**: Verify toast notifications appear appropriately on 400/403 responses.

## Validation Plan

Validation commands will follow repository conventions:
- `npm run lint`
- `npm run type-check` (or `tsc --noEmit`)
- `npm run test` (Vitest unit testing)

## Boundaries / Non-Goals

- No backend changes or database migrations.
- No business rule or permission catalog changes.
- No dynamic PDF/template generation for the card.
- No generic Payment Print UI (handled by Payment Foundation).
- No refund, cancellation, or partial payment implementations.
- No physical inventory/stamp stock management.
- No Care Package Sales.
- Operational validation execution and production migration are deferred.

## Risks / Follow-Ups

- Frontend implementation deferred until Project Owner frontend plan acceptance.
- Operational validation deferred to Phase 1B.8-D.
- Dependency on exact routing paths in the existing Payment Foundation UI for generating deep links.

## Recommended Implementation Sequence

1. API client and Hook definitions.
2. Route registrations.
3. List page (`/cards/reprints`).
4. Create form (`/cards/reprints/new`).
5. Detail page foundation (`/cards/reprints/:id`).
6. Workflow action UI integration.
7. Payment status and action UI integration.
8. Physical handling (print/release) action UI.
9. Permission gating logic.
10. Vitest component tests.
11. Phase 1B.8-C implementation report creation.

## Recommended Next Gate

Project Owner Phase 1B.8-C frontend implementation plan acceptance.
