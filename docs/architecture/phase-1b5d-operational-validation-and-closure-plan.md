# Phase 1B.5-D Operational Validation and Closure Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE EXECUTION

## Authorization Source

Reference:
- Phase 1B.5-C PO frontend implementation acceptance commit:
  df419521942456a024c36451c1331e8c7494170b

State:
- Phase 1B.5-B backend/data implementation is accepted.
- Phase 1B.5-C frontend implementation is accepted.
- This document plans operational validation and closure only.
- This document does not authorize execution.

## Objective

Define the operational validation and closure approach for Phase 1B.5 Customer Merge and Duplicate Resolution.

## Source Documents Reviewed

- docs/architecture/phase-1b5c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b5c-frontend-implementation-acceptance-review.md
- docs/architecture/phase-1b5c-frontend-implementation-report.md
- docs/architecture/phase-1b5c-project-owner-frontend-plan-acceptance.md
- docs/architecture/phase-1b5c-frontend-scope-and-implementation-plan.md
- docs/architecture/phase-1b5b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b5b-backend-data-foundation-implementation-acceptance-review.md
- docs/architecture/phase-1b5b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b5b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b5b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b5-project-owner-plan-acceptance.md
- docs/architecture/phase-1b5-customer-merge-duplicate-resolution-discovery-and-detailed-plan.md
- docs/architecture/phase-1b4d-operational-validation-and-closure-report.md (pattern reference)

## Accepted Scope Summary

Phase 1B.5 Customer Merge and Duplicate Resolution accepted scope:

Backend/data foundation (Phase 1B.5-B):
- V0010 migration: Customer_Merge_Requests, Customer_Merge_Request_Candidates, Customer_Merge_History tables.
- U0010 rollback: soft-deactivates CUSTOMER_MERGE_* permissions, removes V0010 SchemaVersions record.
- Domain entities: CustomerMergeRequest, CustomerMergeRequestCandidate, CustomerMergeHistory.
- Application service: CustomerMergeService (ICustomerMergeService).
- Execution handler: CustomerMergeExecutionHandler for approved workflow execution boundary.
- API v2 controller: CustomerMergeController at /api/v2/customers with 4 endpoints.
- Permission enforcement: CUSTOMER_MERGE_REQUEST_CREATE, CUSTOMER_MERGE_REQUEST_VIEW, CUSTOMER_MERGE_REQUEST_ADMIN_VIEW, CUSTOMER_MERGE_EXECUTE.
- Unit tests, integration tests, API tests.
- MigrationRollbackTests for V0010/U0010.
- TestDatabaseFixture ResetToV0010.

Frontend (Phase 1B.5-C):
- Frontend API client (customerMergeApi.ts): findMergeDuplicates, createMergeRequest, getMergeRequestById, listMergeRequests.
- TypeScript types (customerMergeTypes.ts).
- Sanitized error mapping (customerMergeErrorMessages.ts).
- Duplicate customer search page (CustomerMergeDuplicateSearchPage.tsx).
- Merge request creation page (CustomerMergeRequestCreatePage.tsx) with source/survivor comparison.
- Merge request list page (CustomerMergeRequestsPage.tsx) with pagination and status tags.
- Merge request detail page (CustomerMergeRequestDetailPage.tsx) with candidates table and workflow link.
- App.tsx route wiring: 4 routes.
- AuthenticatedShell.tsx navigation: 2 permission-gated menu items.
- Frontend tests: 30 tests across 5 files.

## Automated Backend Validation Plan

Commands:

1. `dotnet build src/backend/PTKD-ERP.sln`
   Required: Build succeeded. 0 Error(s).

2. `dotnet test tests/backend/PTKD.UnitTests/`
   Required: All tests passed. 0 failed.

3. `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false`
   Required: All tests passed. 0 failed.

4. `dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false`
   Required: All tests passed. 0 failed.

Pass criteria: all 4 commands succeed with 0 failures. Test counts should be equal to or greater than Phase 1B.5-B acceptance evidence (158 unit, 196 integration, 267 API).

## Automated Frontend Validation Plan

Commands:

1. `npm run lint` (from src/frontend/)
   Required: exit 0. Only pre-existing non-Customer-Merge warnings allowed.

2. `npx tsc -b` (from src/frontend/)
   Required: 0 errors.

3. `npm run test` (from src/frontend/)
   Required: All test files passed. 0 failed. Test count should be >= 417 (Phase 1B.5-C acceptance evidence).

4. Targeted Customer Merge frontend tests:
   `npm run test -- src/customers/customerMergeApi.test.ts src/customers/customerMergeErrorMessages.test.ts src/customers/CustomerMergeDuplicateSearchPage.test.tsx src/customers/CustomerMergeRequestsPage.test.tsx src/customers/CustomerMergeRequestDetailPage.test.tsx`
   Required: 5 test files, 30 tests passed, 0 failed.

Pass criteria: all 4 commands succeed with 0 failures.

## Database / Migration Validation Plan

Validation approach (via automated test evidence, not production migration):

1. V0010 migration applies correctly.
   Evidence: MigrationRollbackTests and IntegrationTests pass — V0010 is applied as part of test fixture setup.

2. U0010 rollback works correctly.
   Evidence: MigrationRollbackTests for V0010/U0010 pass — U0010 is tested as part of rollback coverage.

3. U0010 soft-deactivates CUSTOMER_MERGE_* permissions.
   Evidence: U0010 rollback script uses UPDATE (not DELETE) because TR_Permissions_PreventDelete blocks hard delete. Confirmed in backend implementation report.

4. U0010 removes V0010 SchemaVersions record.
   Evidence: MigrationRollbackTests validate SchemaVersions cleanup. DbMigrator owns SchemaVersions.

5. API test fixture uses ResetToV0010.
   Evidence: SafeTestWebApplicationFactory uses TestDatabaseFixture.ResetToV0010(). API tests pass.

6. Test database is PTKD_TEST_PHASE1A2 only.
   Evidence: Connection string in test configuration. No production database connection.

7. No production migration.
   Evidence: git status, no production deployment artifacts, no release tag.

## Manual / Operational Checklist

Each item requires a result status during execution:
- PASSED
- NOT EXECUTED (with reason)
- FAILED (with reason)

### Duplicate Search and Candidate Display

1. Duplicate customer search form renders and accepts CCCD and phone input.
2. Search returns candidate list when duplicates exist.
3. Search shows "No duplicate customers found" when no duplicates exist.
4. Candidate list displays customer ID, code, name, CCCD, phone, status safely.
5. "Select as Source" link navigates to merge request creation with pre-selected source.

### Merge Request Creation

6. Merge request creation form renders with source and target customer ID inputs.
7. Source and target customers load and display side-by-side comparison (name, CCCD, phone, status).
8. Source equals target is blocked with client-side validation error.
9. Already merged source is blocked with sanitized backend error.
10. Invalid/inactive target is blocked with sanitized backend error.
11. Overlapping CustomerCompanyContext conflict shows sanitized validation error (not raw SQL).
12. Stale rowversion/concurrency error shows sanitized error ("Data has changed since you started").
13. Survivorship payload is generated programmatically and not displayed as raw JSON.
14. Rowversion snapshots are captured from loaded customer data.
15. Successful submit navigates to merge request detail page.

### Merge Request List and Detail

16. Merge request list page renders with paginated table.
17. Status tags display with correct color coding (DRAFT, SUBMITTED, APPROVED, EXECUTED, REJECTED, WITHDRAWN).
18. View link navigates to merge request detail page.
19. Workflow link navigates to workflow instance detail page when workflowInstanceId exists.
20. Merge request detail page displays request metadata, status, candidates table.
21. Detail page does not expose raw survivorshipPayload JSON.

### Error Handling and Security

22. Permission denied (403) shows "You do not have permission to perform this action."
23. Not found (404) shows "Merge request not found."
24. Generic server failure shows "An unexpected error occurred. Please try again."
25. No raw SQL or internal exception details are displayed for any error scenario.
26. No stack traces are displayed.
27. No raw sensitive payload is displayed.

### Backend Authorization and Merge Execution

28. Backend authorization remains authoritative for all merge operations.
29. Frontend permission gating is convenience only — does not replace backend checks.
30. Approved workflow execution applies merge once via CustomerMergeExecutionHandler.
31. Rejected or non-approved request does not mutate customer data.
32. Retry/idempotency does not double-apply merge (execution handler checks request status).
33. Source customer remains traceable after merge (not hard-deleted).
34. Survivor/canonical customer remains active after merge.
35. No destructive customer deletion UI exists.
36. No automatic fuzzy merge UI exists.

## Security and Data Exposure Validation Plan

Planned validation during execution:

- Backend authorization is authoritative: all CustomerMergeController endpoints enforce RequirePermission attributes. Frontend hasPermission() is convenience only.
- No raw SQL/internal exception display: getMergeErrorMessage maps all errors to sanitized strings. Unknown Detail values fall through to generic error. Tested: SQL deadlock detail returns generic error.
- No stack traces: error mapping returns only mapped string constants, never raw exception data.
- No raw sensitive payload exposure: survivorshipPayload is not rendered as raw text on detail page. Confirmed by test (queryByText returns null).
- Sanitized errors only: all error scenarios mapped in customerMergeErrorMessages.ts.
- No destructive merge UI: no "Execute Merge" or "Delete Customer" buttons. Create page only creates DRAFT requests.
- No automatic fuzzy merge: all merges require manual source/target selection and explicit submit.

## Repository Hygiene Validation Plan

During execution, verify:

- `git diff --check`: clean.
- `git status --short --untracked-files=all`: no tracked modifications except closure report.
- No staged scratch/decompiled/FixStrategy/script/debug files.
- No tag at HEAD.
- No push performed.
- No production migration applied.

## Closure Criteria

Phase 1B.5 may proceed to closure acceptance review only if:

- Backend build passes (0 errors).
- UnitTests pass (0 failed).
- IntegrationTests pass (0 failed).
- ApiTests pass (0 failed).
- Frontend lint passes (exit 0, only pre-existing warnings).
- TypeScript passes (0 errors).
- Full Vitest passes (0 failed, >= 417 tests).
- Targeted Customer Merge frontend tests pass (30 tests across 5 files).
- git diff --check clean.
- Operational checklist has no blocking FAILED item.
- No production migration/tag/push.
- Closure report is created.

## Risks / Follow-Ups

1. **Workflow approval UI integration limits**: Merge request detail links to existing WorkflowInstanceDetailPage via workflowInstanceId. No dedicated merge approval screen. Operational validation should confirm the link works when a workflow instance exists.

2. **Future service/payment/document linked-module display**: Deferred. Not in Phase 1B.5 scope. Merge request detail page has no linked-module impact preview.

3. **Future migrations must update test fixture reset target**: When V0011+ is added, TestDatabaseFixture must add ResetToV0011() and SafeTestWebApplicationFactory must be updated. ResetToV0010 is the current ceiling.

4. **DuplicateCheckResult type reuse**: Frontend reuses existing DuplicateCheckResult from types.ts. If backend /customers/duplicates response shape differs from /customers/duplicate-check, the type may need adjustment during operational validation.

5. **CustomerMergeRequestCreatePage test gap**: No dedicated test file was committed. Core behavior covered by API client tests. This is a minor gap noted in the acceptance review.

6. **Untracked scratch/decompiled/FixStrategy files**: Remain in working tree. Must not be staged or committed.

7. **Production release**: Remains deferred. No release tag or push is authorized in Phase 1B.5-D.

## Recommended Next Gate

Project Owner acceptance of this Phase 1B.5-D operational validation and closure plan.

## Project Owner Approval Required

This plan does not authorize operational validation execution.
Operational validation execution may begin only after Project Owner accepts this Phase 1B.5-D plan.
