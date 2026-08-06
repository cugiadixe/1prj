# Phase 1B.4-C Customer Master Expansion Frontend Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.4-B PO acceptance commit:
  c8945470257f389c0d037661291270079e4a4fc5
- Authorized next task:
  Phase 1B.4-C frontend scope and implementation planning only.

frontend implementation is not authorized by this document.
implementation requires separate Project Owner approval.

## Objective

Define the frontend scope needed to expose the accepted 1B.4-B backend/data Customer Master Change capability.

## Confirmed Backend Foundation

- CustomerMasterChange API v2 controller exists.
- CustomerMasterChange service exists.
- CustomerMasterChange execution handler exists.
- target customer linkage exists.
- target rowversion/concurrency exists.
- workflow apply boundary exists.
- backend tests pass.

## Proposed Frontend Scope

1. Customer detail change request entry point
- From customer detail/profile screen.
- User can request official customer master data change.
- Must use safe form fields only.
- Must include target rowversion.

2. Customer master change request form
- Input fields based on accepted DTOs.
- Validate required fields client-side where appropriate.
- Submit to backend API.
- Show sanitized backend error messages.

3. My customer change requests
- List requests created by current user.
- Status display.
- Link to detail.

4. Customer change request detail
- Show requested before/after data safely.
- Show status.
- Show workflow/request identifiers only where useful.
- Do not expose raw JSON payloads.

5. Permission-gated admin/review visibility
- Respect backend authorization.
- UI gating is convenience only, not security boundary.
- Do not introduce new permissions unless already accepted.

6. Workflow integration
- Link or navigate to existing workflow approval/runtime views where supported.
- Do not duplicate workflow approval UI unless necessary.

## Explicitly Out of Scope

- backend changes
- new migrations
- production migration
- new permission catalog changes unless separately approved
- frontend implementation in this planning task
- Phase 1B.4-D operational validation
- customer merge
- duplicate resolution workflow beyond existing duplicate check
- service/payment modules
- release tag/push

## Proposed Frontend Files

- src/frontend/src/customers/CustomerMasterChangeRequestForm.tsx
- src/frontend/src/customers/CustomerMasterChangeRequestForm.test.tsx
- src/frontend/src/customers/CustomerMasterChangeRequestsPage.tsx
- src/frontend/src/customers/CustomerMasterChangeRequestsPage.test.tsx
- src/frontend/src/customers/CustomerMasterChangeRequestDetailPage.tsx
- src/frontend/src/customers/CustomerMasterChangeRequestDetailPage.test.tsx
- src/frontend/src/customers/customerMasterChangeApi.ts
- src/frontend/src/customers/customerMasterChangeApi.test.ts
- src/frontend/src/customers/customerMasterChangeTypes.ts
- src/frontend/src/App.tsx
- src/frontend/src/AuthenticatedShell.tsx

## API Client Plan

- endpoint wrappers for `POST /api/v2/customers/{id}/change-requests`, `GET /api/v2/customers/my-change-requests`, `GET /api/v2/customers/change-requests/{requestId}`
- typed request/response DTOs using TypeScript interfaces matching backend models
- rowversion transport included in payload
- sanitized error mapping using existing error handling patterns
- no raw JSON exposure
- test mocking strategy using vitest and standard mock data

## UX Rules

- loading states for form submission and data fetching
- empty states for "My requests" list
- forbidden/unauthorized states for restricted pages
- stale rowversion/concurrency error specific alerts
- duplicate CCCD error specific alerts
- submit success behavior navigates back to list or detail view
- link to workflow/my approvals if applicable
- no raw/internal error display

## Permission and Security Plan

- backend remains authoritative
- frontend permission gating is not a security boundary
- no raw PayloadJson/BeforeDataJson exposure
- no SQL/internal exception exposure
- no stack traces
- no sensitive logs
- company context handling if used by existing frontend pattern

## Test Plan

- npm run lint
- tsc -b
- vitest run
- targeted tests for:
  - API client
  - form validation
  - submit success
  - duplicate/stale/sanitized errors
  - my requests page
  - detail page safe rendering
  - permission-gated visibility
  - navigation route/shell links

## Acceptance Criteria for 1B.4-C Implementation

- frontend compiles
- lint passes
- TypeScript passes
- Vitest passes
- customer change request form works
- my requests page works
- detail page works
- backend errors are sanitized
- raw JSON/internal details are not displayed
- permission-gated UI follows existing pattern
- no backend/business/migration changes are made unless separately approved

## Risks / Open Questions

- exact field set for customer change form requires mapping to all optional fields
- whether admin/review list is needed in 1B.4-C or deferred
- whether workflow detail link is enough or a dedicated review screen is needed
- permission-code/catalog mismatch if discovered
- UI copy/labels requiring PO confirmation

## Recommended Implementation Steps

1. Add typed API client and tests.
2. Add form component and tests.
3. Add my requests page and tests.
4. Add detail page and tests.
5. Wire route/navigation/entry point.
6. Run frontend validation.
7. Create implementation report.
8. Submit for implementation acceptance review.

## Project Owner Approval Required

This plan does not authorize implementation.
Implementation may begin only after Project Owner accepts this Phase 1B.4-C frontend scope and implementation plan.
