# Phase 1B.4-D Operational Validation and Closure Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE EXECUTION

## Authorization Source

Reference:
- Phase 1B.4-C PO frontend implementation acceptance commit:
  5541e6f1178d318340b98863903e43e7e188a002

State:
- This document is planning only.
- Operational validation execution is not authorized by this document.
- Execution requires separate Project Owner approval.

## Objective

Define the operational validation and closure approach for Phase 1B.4 Customer Master Expansion.

## Accepted Phase 1B.4 Scope to Validate

Summarize accepted components:
- backend/data foundation from 1B.4-B,
- Customer_Change_Requests target linkage,
- TargetRowVersion/concurrency,
- V0009/U0009 migration and rollback,
- CustomerMasterChange API v2,
- CUSTOMER_UPDATE_FROM_APPROVAL execution handler,
- frontend API client,
- customer change request form,
- my requests page,
- detail page,
- route/navigation wiring,
- permission-gated UI.

## Proposed Validation Evidence

Plan to run:

Backend:
- dotnet build src/backend/PTKD-ERP.sln
- dotnet test tests/backend/PTKD.UnitTests/
- dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
- dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false

Frontend:
From src/frontend:
- npm run lint
- npx tsc -b
- npm run test
- targeted CustomerMasterChange tests

Repository hygiene:
- git diff --check
- git status --short --untracked-files=all
- verify no tag
- verify no push
- verify no production migration

## Manual / Operational Validation Plan

Define manual validation checklist without executing it yet:

1. Create customer master change request from customer detail UI.
2. Verify RowVersion is submitted.
3. Verify duplicate CCCD error is sanitized.
4. Verify stale rowversion/concurrency error is sanitized.
5. Verify My Requests page displays submitted request.
6. Verify detail page shows safe data only.
7. Verify raw PayloadJson / BeforeDataJson are not displayed.
8. Verify SQL/internal exception/stack trace are not displayed.
9. Verify permission-gated UI follows existing pattern.
10. Verify backend remains authoritative for permission enforcement.
11. Verify workflow approval apply boundary using CUSTOMER_UPDATE_FROM_APPROVAL.
12. Verify rejected/non-approved request does not mutate official customer data.
13. Verify retry/idempotency does not double-apply official changes.

## Database / Migration Validation Plan

Plan to confirm:
- V0009 applies in test DB.
- U0009 rollback is covered by MigrationRollbackTests.
- SchemaVersions is owned by DbMigrator.
- No manual production migration is executed.
- Test database is PTKD_TEST_PHASE1A2.

## Security Validation Plan

Plan to confirm:
- no raw PayloadJson exposure,
- no raw BeforeDataJson exposure,
- no SQL/internal exception exposure,
- no stack trace exposure,
- sanitized frontend/backend errors,
- backend authorization authoritative,
- frontend gating convenience only,
- no new permission code introduced outside approved scope,
- no permission catalog change.

## Closure Criteria

Phase 1B.4 can be closed only if:
- backend build/tests pass,
- frontend lint/TypeScript/Vitest pass,
- targeted CustomerMasterChange tests pass,
- migration rollback evidence passes,
- manual/operational checklist is completed or explicitly deferred,
- no backend/frontend tracked modifications remain,
- no production migration,
- no release tag,
- no push,
- closure report is created,
- closure acceptance review is created,
- Project Owner closure acceptance is created.

## Explicitly Out of Scope

State:
- production migration,
- release tag,
- push,
- deployment,
- customer merge,
- payment/service modules,
- new permissions,
- business requirement changes,
- backend/frontend feature expansion beyond accepted 1B.4 scope.

## Risks / Watch Items

Document:
- local history rewrite/hash mismatch previously verified non-blocking,
- untracked scratch files remain and must not be staged,
- shared test database must not be used by overlapping test runs,
- PTKD_TEST_PHASE1A2 must remain the test DB,
- manual UI validation may require seeded users/permissions/workflow definitions,
- production release remains deferred.

## Recommended Execution Steps After PO Approval

Propose:
1. Run full backend validation.
2. Run full frontend validation.
3. Run targeted CustomerMasterChange frontend tests.
4. Run repository hygiene checks.
5. Perform manual operational validation checklist.
6. Create Phase 1B.4-D operational validation and closure report.
7. Create acceptance review.
8. Create Project Owner closure acceptance.
9. Only after separate approval, decide next-work selection.

## Project Owner Approval Required

This plan does not authorize operational validation execution.
Execution may begin only after Project Owner accepts this Phase 1B.4-D operational validation and closure plan.
