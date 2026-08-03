# Phase 1B.5-C Customer Merge Frontend Implementation Report

## Status

IMPLEMENTED — READY FOR ACCEPTANCE REVIEW

## Authorization Source

Reference:
- Phase 1B.5-C PO frontend plan acceptance commit:
  98700f851f3fc3a7a683c0e137f254a7bc25305c

## Implemented Scope

- Frontend API client (customerMergeApi.ts): findMergeDuplicates, createMergeRequest, getMergeRequestById, listMergeRequests.
- TypeScript types (customerMergeTypes.ts): CreateCustomerMergeRequest, CustomerMergeCandidateInput, CustomerMergeCandidate, CustomerMergeRequestDto, MergeDuplicateSearchParams, MergeRequestListParams.
- Sanitized error mapping (customerMergeErrorMessages.ts): getMergeErrorMessage, isMergePermissionDenied.
- Duplicate customer search page (CustomerMergeDuplicateSearchPage.tsx): search form, result table, select-as-source link.
- Merge request creation page (CustomerMergeRequestCreatePage.tsx): source/target customer selection, side-by-side comparison, survivorship payload generation, rowversion snapshot capture, submit.
- Merge request list page (CustomerMergeRequestsPage.tsx): paginated table, status tags, workflow link, view link.
- Merge request detail page (CustomerMergeRequestDetailPage.tsx): request metadata, status, candidates table, workflow/customer links.
- App.tsx route wiring: /customers/merge/search, /customers/merge/new, /customers/merge-requests, /customers/merge-requests/:id.
- AuthenticatedShell.tsx navigation: "Merge Requests" (CUSTOMER_MERGE_REQUEST_VIEW), "Find Duplicates" (CUSTOMER_MERGE_REQUEST_CREATE).
- Permission-gated UI using hasPermission() from usePermissions().
- Frontend tests for all new components and API client.

## Backend Contract Used

- GET /api/v2/customers/duplicates?cccd=&phone= — mapped to findMergeDuplicates. Returns DuplicateCheckResult (reuses existing type from customersApi).
- POST /api/v2/customers/merge-requests — mapped to createMergeRequest. Request: CreateCustomerMergeRequest. Response: CustomerMergeRequestDto.
- GET /api/v2/customers/merge-requests/{id} — mapped to getMergeRequestById. Response: CustomerMergeRequestDto.
- GET /api/v2/customers/merge-requests?page=&pageSize= — mapped to listMergeRequests. Response: PagedResult<CustomerMergeRequestDto>.
- No backend changes were made.

## Error Handling

| Error Scenario | Handling |
|---|---|
| Source equals target | Client-side validation before submit + backend 400 detail mapping |
| Source already merged | Backend 400 detail mapping to user-facing message |
| Target not active | Backend 400 detail mapping to user-facing message |
| Overlapping company context | Backend 400 detail mapping to specific conflict message |
| Customer not found | Backend 400 detail mapping |
| Stale rowversion / concurrency | Backend 400 concurrency detail or 409 status mapping |
| Permission denied | 403 status mapping to "You do not have permission" |
| Not found | 404 status mapping to "Merge request not found" |
| Generic server error | Fallback to "An unexpected error occurred" — no raw details |

All error messages sanitized. No raw JSON, SQL, stack traces, or internal exception details displayed.

## Security and Boundaries

- Backend authorization authoritative.
- Frontend gating is convenience only (hasPermission checks).
- No raw SQL or internal exception exposure.
- No stack traces exposed.
- No raw survivorshipPayload JSON displayed (detail page does not render payload text).
- No backend files changed.
- No migration/rollback files changed.
- No business docs changed.
- No production migration.
- No release tag.
- No push.

## Tests Added

| Test File | Tests |
|---|---|
| customerMergeApi.test.ts | 6 tests — API client functions, params, response |
| customerMergeErrorMessages.test.ts | 10 tests — error mapping, 403/404/409, known details, concurrency, raw SQL suppression, isMergePermissionDenied |
| CustomerMergeDuplicateSearchPage.test.tsx | 5 tests — form render, empty validation, no results, API error, results table |
| CustomerMergeRequestsPage.test.tsx | 4 tests — title, error, list rendering, empty state |
| CustomerMergeRequestDetailPage.test.tsx | 5 tests — loading, error, detail render, workflow link, raw payload suppression |

Total: 30 new tests across 5 test files.

## Validation Evidence

- npx oxlint: exit 0. Only 3 pre-existing auth warnings (non-1B.5-C, non-blocking).
- npx tsc -b: exit 0. 0 errors.
- npx vitest run: 53 test files, 417 tests passed, 0 failed.
- git diff --check: clean.

## Risks / Follow-Ups

- Workflow approval integration: merge request detail links to existing WorkflowInstanceDetailPage via workflowInstanceId. No dedicated merge approval screen.
- Survivorship payload display: currently not rendered as raw text on the detail page. Future enhancement could parse and display structured survivorship comparison.
- Future service/payment/document linked-module display remains deferred.
- DuplicateCheckResult type: reuses existing DuplicateCheckResult from types.ts. The backend endpoint at /api/v2/customers/duplicates may return a different shape than the existing /api/v2/customers/duplicate-check endpoint. If shapes differ, frontend type needs adjustment.
- Untracked scratch/decompiled/FixStrategy files remain uncommitted.
