# Phase 1B.5-C Project Owner Frontend Plan Acceptance

## Status

ACCEPTED — PHASE 1B.5-C CUSTOMER MERGE FRONTEND PLAN APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b5c-frontend-scope-and-implementation-plan.md

Planning commit:
32395eb15cefbf4ff8d04d196926f5dcb0569970

## Accepted Frontend Scope

The Project Owner accepts only the frontend implementation scope defined in the plan, including:

- Customer Merge frontend API client (customerMergeApi.ts).
- TypeScript DTO/type mapping (customerMergeTypes.ts).
- Duplicate customer search page (CustomerMergeDuplicateSearchPage.tsx).
- Duplicate candidate result list.
- Merge request creation form (CustomerMergeRequestCreatePage.tsx).
- Source vs survivor comparison UI.
- Survivorship review UI.
- Merge request list page (CustomerMergeRequestsPage.tsx).
- Merge request detail page (CustomerMergeRequestDetailPage.tsx).
- Customer detail entry point if appropriate (gated by CUSTOMER_MERGE_REQUEST_CREATE).
- App.tsx route wiring for /customers/merge/search, /customers/merge/new, /customers/merge-requests, /customers/merge-requests/:id.
- AuthenticatedShell.tsx navigation wiring (Merge Requests, Find Duplicates).
- Permission-gated UI using usePermissions() / hasPermission().
- Sanitized frontend error handling (customerMergeErrorMessages.ts).
- Frontend tests for all new components and API client.

## Accepted API Client Scope

The Project Owner accepts the frontend client mapping to backend Customer Merge API v2 endpoints:

- findDuplicates → GET /api/v2/customers/duplicates
- createMergeRequest → POST /api/v2/customers/merge-requests
- getMergeRequest → GET /api/v2/customers/merge-requests/{id}
- listMergeRequests → GET /api/v2/customers/merge-requests

The frontend must not change backend API contracts unless a blocker is found and separately approved.

## Accepted Error Handling Scope

The Project Owner accepts frontend handling for:

- source equals target customer,
- source already merged,
- target not active / invalid survivor,
- overlapping CustomerCompanyContext conflict,
- stale rowversion / concurrency error,
- permission denied (403),
- not found (404),
- sanitized generic server failure.

All errors must be displayed as sanitized user-facing messages. No raw JSON, SQL, stack traces, or internal exception details.

## Accepted Test Scope

The Project Owner accepts planned frontend tests:

- API client tests (customerMergeApi.test.ts).
- Duplicate search/list tests (CustomerMergeDuplicateSearchPage.test.tsx).
- Merge request form tests (CustomerMergeRequestCreatePage.test.tsx).
- Merge request list tests (CustomerMergeRequestsPage.test.tsx).
- Merge request detail tests (CustomerMergeRequestDetailPage.test.tsx).
- Error mapping tests (customerMergeErrorMessages.test.ts).
- Permission-gated UI tests.
- Route/navigation tests.
- Regression tests preventing raw internal error display.

## Accepted Open Questions / Risks

The following are carried forward as non-blocking implementation constraints:

- Survivorship payload display UX: must parse and render safely, no raw JSON display.
- Admin vs requester view split: single page filtered by backend authorization is acceptable.
- Workflow approval reuse: reuse existing WorkflowInstanceDetailPage via workflowInstanceId link.
- Duplicate result pagination: implement as simple list initially; add pagination if needed.
- DuplicateCheckResult type: verify actual backend response shape during implementation.
- Future linked-module display: out of scope for 1B.5-C; may note as placeholder.

These are non-blocking for frontend implementation if implemented safely and documented.

## Boundaries

- Frontend implementation is authorized only after this acceptance commit.
- Backend changes are not authorized.
- Database migration is not authorized.
- Migration/rollback changes are not authorized.
- Business requirement changes are not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.
- Automatic fuzzy merge is not authorized.
- Destructive customer deletion is not authorized.
- Service/payment/document module implementation is not authorized.

## Implementation Evidence Required

Future frontend implementation must provide:

- Frontend API client implementation.
- Frontend pages/components.
- Route/navigation wiring.
- Permission-gated UI.
- Sanitized error handling.
- Frontend test coverage.
- npx oxlint result.
- npx tsc -b result.
- npx vitest run result.
- Targeted Customer Merge frontend test result.
- git diff --check clean.
- Implementation report.
- No backend changes unless separately approved.
- No production migration/tag/push.

## Project Owner Decision

The Project Owner accepts the Phase 1B.5-C Customer Merge frontend scope and implementation plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.5-C Customer Merge frontend implementation only.

Do not authorize:
- backend changes,
- database migration,
- production migration,
- release tag,
- push.

After implementation, a separate Phase 1B.5-C frontend implementation report and acceptance review are required before Project Owner frontend implementation acceptance.
