# Phase 1B.5-B Backend/Data Foundation Implementation Acceptance Review

## Status

PASSED — READY FOR PROJECT OWNER IMPLEMENTATION ACCEPTANCE

## Reviewed Commits

- Implementation commit:
  dc6ebf6ea85b98d9ade2609c4e237fbb03d11916
- Remediation commit:
  ddab8b3397779672c7f4995888a1f9c2f952cfc5
- Parent scope authorization commit:
  8cdf94053ccf390811b38887950507f0db7fad06

## Scope Review

Exact committed files (implementation commit dc6ebf6):

| File | Status | In Scope |
|---|---|---|
| database/migrations/V0010__customer_merge_backend_data_foundation.sql | A | Yes |
| database/rollbacks/U0010__customer_merge_backend_data_foundation.sql | A | Yes |
| docs/architecture/phase-1b5b-backend-data-foundation-implementation-report.md | A | Yes |
| src/backend/PTKD.Api/Controllers/CustomerMergeController.cs | A | Yes |
| src/backend/PTKD.Api/Program.cs | M | Yes |
| src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs | M | Yes |
| src/backend/PTKD.Application/Customers/DTOs/CustomerMergeDtos.cs | A | Yes |
| src/backend/PTKD.Application/Customers/Handlers/CustomerMergeExecutionHandler.cs | A | Yes |
| src/backend/PTKD.Application/Customers/Services/CustomerMergeService.cs | A | Yes |
| src/backend/PTKD.Application/Customers/Services/ICustomerMergeService.cs | A | Yes |
| src/backend/PTKD.Domain/Entities/CustomerMergeHistory.cs | A | Yes |
| src/backend/PTKD.Domain/Entities/CustomerMergeRequest.cs | A | Yes |
| src/backend/PTKD.Domain/Entities/CustomerMergeRequestCandidate.cs | A | Yes |
| src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs | M | Yes |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerMergeHistoryConfiguration.cs | A | Yes |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerMergeRequestCandidateConfiguration.cs | A | Yes |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerMergeRequestConfiguration.cs | A | Yes |
| tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs | M | Yes |
| tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs | M | Yes |
| tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs | M | Yes |
| tests/backend/PTKD.UnitTests/Customers/CustomerMergeExecutionHandlerTests.cs | A | Yes |
| tests/backend/PTKD.UnitTests/Customers/CustomerMergeServiceTests.cs | A | Yes |

Remediation commit (ddab8b3):

| File | Status | In Scope |
|---|---|---|
| docs/architecture/phase-1b5b-backend-data-foundation-implementation-report.md | M | Yes |
| tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs | M | Yes |
| tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs | M | Yes |

All committed files are within the authorized Phase 1B.5-B backend/data scope. No frontend files. No business docs modified. No PermissionCodes.cs changed (permissions are seeded via V0010 migration SQL only).

## Implementation Scope vs Accepted Plan

| Accepted Scope Item | Implemented | Notes |
|---|---|---|
| Customer_Merge_Requests persistence | Yes | V0010 creates table with FK constraints, CHECK constraints, indexes |
| Duplicate candidate search boundaries | Yes | Customer_Merge_Request_Candidates table, DuplicateCheckRequest endpoint |
| Source and survivor customer linkage | Yes | source_customer_id/target_customer_id FK to Customers |
| Source customer MERGED marker | Yes | SetStatus("MERGED") in execution handler |
| SurvivorCustomerId / CanonicalCustomerId strategy | Yes | target_customer_id as survivor, source linked via SurvivorCustomerId |
| RowVersion/concurrency checks | Yes | source_rowversion_snapshot, target_rowversion_snapshot validated at execution |
| Merge request lifecycle/status tracking | Yes | DRAFT/SUBMITTED/APPROVED/EXECUTED/REJECTED/WITHDRAWN CHECK constraint |
| Before/after/survivorship snapshot persistence | Yes | survivorship_payload (NVARCHAR(MAX)), snapshot_payload on candidates |
| Append-only merge audit/history | Yes | Customer_Merge_History table with action_type, summary_payload |
| CUSTOMER_MERGE workflow execution boundary | Yes | CustomerMergeExecutionHandler implements IWorkflowExecutionHandler |
| Approved execution handler | Yes | Checks SUBMITTED/APPROVED status before execution |
| Idempotency/double-apply prevention | Yes | Returns silently if already EXECUTED |
| Rejected/non-approved request no mutation | Yes | Returns silently if REJECTED/WITHDRAWN, throws if invalid state |
| API v2 backend endpoints | Yes | CustomerMergeController: GET duplicates, POST/GET merge-requests |
| Backend permission enforcement | Yes | CUSTOMER_MERGE_REQUEST_CREATE, _VIEW, _ADMIN_VIEW, CUSTOMER_MERGE_EXECUTE |
| V0010/U0010 migration and rollback | Yes | V0010 creates, U0010 soft-deactivates permissions + drops tables |
| MigrationRollbackTests | Yes | Updated to cover V0010 apply and rollback |
| Unit/Integration/API tests | Yes | CustomerMergeServiceTests, CustomerMergeExecutionHandlerTests, SecuritySchemaTests updated |
| Overlapping company context blocking | Yes | Service checks overlap and throws validation error |

No unauthorized scope was introduced. No frontend implementation. No production migration. No release tag. No push.

## Remediation Review

Root cause: SafeTestWebApplicationFactory called ResetToV0009() but application EF context expected V0010 tables. API tests failed 267/267 with SQL deadlock/missing table errors.

Fix: Added ResetToV0010() to TestDatabaseFixture following the established sequential reset pattern. Updated SafeTestWebApplicationFactory to call ResetToV0010(). Fix is minimal, correct, and follows the existing pattern exactly.

## Database and Migration Review

### V0010 Migration

- Customer_Merge_Requests: UNIQUEIDENTIFIER PK, bigint FKs to Customers/Users/Workflow_Instances, CHECK constraint on request_status, CHECK constraint source <> target, ROWVERSION, datetime2(3) timestamps. Correct.
- Customer_Merge_Request_Candidates: UNIQUEIDENTIFIER PK, FK to merge request and customer, match_type/match_confidence/snapshot_payload. Correct.
- Customer_Merge_History: UNIQUEIDENTIFIER PK, nullable FK to merge request, FKs to source/target customers and actor user, action_type/summary_payload. Correct.
- Permission seeding: 4 permission codes inserted with is_sensitive=1, module_code='CUSTOMER'. Correct.
- Indexes: Nonclustered indexes on source/target customer and merge_request FK columns. Correct.
- Transaction: SET XACT_ABORT ON, BEGIN/COMMIT TRANSACTION. Correct.

### U0010 Rollback

- Soft-deactivates 4 CUSTOMER_MERGE_* permissions (is_active=0) — correct, avoids TR_Permissions_PreventDelete trigger.
- Drops tables in dependency-safe order: History first, then Candidates, then Requests. Correct.
- Uses IF OBJECT_ID checks for safety. Correct.
- Removes V0010 from SchemaVersions. Correct.
- Transaction-wrapped. Correct.

## Domain Entity Review

- CustomerMergeRequest: Private setters, constructor validates source != target, null checks on required fields, status transition methods (SetSubmitted/SetApproved/SetExecuted/SetRejected/SetWithdrawn), private parameterless ctor for EF. Correct.
- CustomerMergeRequestCandidate: Private setters, FK to merge request, null check on matchType, private parameterless ctor for EF. Correct.
- CustomerMergeHistory: Private setters, nullable merge request FK (allows orphan history), null checks on actionType/summaryPayload, private parameterless ctor for EF. Correct.

## Security and Permission Review

- API endpoints enforce permission via IPermissionEvaluator before data access. Correct.
- FindDuplicates: requires CUSTOMER_MERGE_REQUEST_CREATE. Correct.
- CreateMergeRequest: requires CUSTOMER_MERGE_REQUEST_CREATE. Correct.
- GetMergeRequest: requires CUSTOMER_MERGE_REQUEST_VIEW or CUSTOMER_MERGE_REQUEST_ADMIN_VIEW. Correct.
- ListMergeRequests: requires CUSTOMER_MERGE_REQUEST_VIEW or CUSTOMER_MERGE_REQUEST_ADMIN_VIEW. Correct.
- Execution handler: invoked via workflow execution boundary (IWorkflowExecutionHandler), not directly via API. Correct.
- SecuritySchemaTests updated with 4 new permission codes. Verified: CUSTOMER_MERGE_EXECUTE, CUSTOMER_MERGE_REQUEST_ADMIN_VIEW, CUSTOMER_MERGE_REQUEST_CREATE, CUSTOMER_MERGE_REQUEST_VIEW.
- No raw SQL/internal exception exposure. InvalidOperationException messages are generic validation errors. Correct.
- No sensitive customer fields (CCCD, phone, address, DOB) exposed in merge DTOs — only IDs, status, and survivorship payload. Correct.
- Execution handler uses Serializable isolation level for merge execution transaction. Correct.
- Concurrency validation compares rowversion snapshots before execution. Correct.

## Test Evidence

From implementation report (post-remediation):

- dotnet build: 0 errors, 0 warnings.
- UnitTests: 158 passed, 0 failed.
- IntegrationTests: 196 passed, 0 failed.
- ApiTests: 267 passed, 0 failed.
- git diff --check: clean.

## Boundary Compliance

- No frontend changes. Confirmed.
- No business-rules.md changes. Confirmed.
- No permission-catalog.md changes. Confirmed.
- No acceptance-criteria.md changes. Confirmed.
- No PermissionCodes.cs changes. Confirmed.
- No production migration executed. Confirmed.
- No release tag created. Confirmed.
- No push executed. Confirmed.
- No destructive customer deletion. Confirmed.
- No service/payment/document module implementation. Confirmed.
- No automatic fuzzy merge without review. Confirmed.

## Risks / Follow-Ups

- Execution handler uses ActorId=0 as placeholder — future work should propagate the actual actor from the workflow instance context.
- Survivorship payload application to target profile fields is deferred ("For foundation scope, simply mark source as MERGED and link survivor"). Future phases must implement field-level survivorship merge.
- Frontend implementation for customer merge is not authorized in this scope.
- Service/payment/document linked-module cascade handling remains deferred.
- The `_customerService` dependency in CustomerMergeController is injected but only used for duplicate checks — verify DI registration.

## Review Decision

PASSED — B5-B BACKEND/DATA FOUNDATION IMPLEMENTATION MAY PROCEED TO PROJECT OWNER ACCEPTANCE
