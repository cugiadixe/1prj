# Phase 1B.5-B Project Owner Backend/Data Implementation Acceptance

## Status

ACCEPTED — PHASE 1B.5-B BACKEND/DATA FOUNDATION IMPLEMENTATION COMPLETE

## Accepted Implementation

The Project Owner accepts Phase 1B.5-B Customer Merge backend/data foundation implementation as complete.

## Accepted Commits

- Acceptance review commit:
  b2d96935f82dd5fba9feff31649d9382e7b7427a

- Remediation commit:
  ddab8b3397779672c7f4995888a1f9c2f952cfc5

- Initial backend/data implementation commit:
  dc6ebf6ea85b98d9ade2609c4e237fbb03d11916

- Backend/data scope acceptance commit:
  8cdf94053ccf390811b38887950507f0db7fad06

## Accepted Scope

The Project Owner accepts the following backend/data scope as implemented:

- V0010 customer merge backend/data migration.
- U0010 rollback.
- Customer_Merge_Requests persistence.
- Customer_Merge_Request_Candidates persistence.
- Customer_Merge_History / audit persistence.
- Customer merge domain entities (CustomerMergeRequest, CustomerMergeRequestCandidate, CustomerMergeHistory).
- EF configurations (CustomerMergeRequestConfiguration, CustomerMergeRequestCandidateConfiguration, CustomerMergeHistoryConfiguration).
- AppDbContext updates (DbSet registrations for customer merge entities).
- IOrganizationDbContext updates (interface extension for customer merge DbSets).
- CustomerMergeService (create, get, search merge requests with overlapping company context blocking).
- ICustomerMergeService (service interface).
- CustomerMergeExecutionHandler (workflow execution boundary with idempotency, concurrency validation, Serializable isolation).
- CustomerMergeController (API v2 endpoints: GET duplicates, POST merge-requests, GET merge-requests, GET merge-requests/{id}).
- API v2 backend endpoints under /api/v2/customers.
- CUSTOMER_MERGE_REQUEST_CREATE, CUSTOMER_MERGE_REQUEST_VIEW, CUSTOMER_MERGE_REQUEST_ADMIN_VIEW, CUSTOMER_MERGE_EXECUTE permission handling.
- Unit tests (CustomerMergeServiceTests, CustomerMergeExecutionHandlerTests).
- Integration tests (MigrationRollbackTests updated for V0010/U0010, SecuritySchemaTests updated with 4 new permission codes).
- API tests (267 passed after remediation).
- MigrationRollbackTests (V0010 apply and U0010 rollback coverage).
- TestDatabaseFixture V0010 support (ResetToV0010 method added).
- SafeTestWebApplicationFactory ResetToV0010 remediation.

## Evidence Accepted

- Backend build passed: 0 errors, 0 warnings.
- UnitTests passed: 158 passed, 0 failed.
- IntegrationTests passed: 196 passed, 0 failed.
- ApiTests passed: 267 passed, 0 failed.
- git diff --check: clean.

## Remediation Accepted

The Project Owner accepts the remediation of the initial ApiTests blocker.

- Initial implementation commit dc6ebf6 was PARTIAL / BLOCKED.
- ApiTests initially failed 267/267 due to test database reset mismatch and deadlock risk.
- Root cause: SafeTestWebApplicationFactory reset the test database to V0009 while the backend application EF context now requires V0010 Customer_Merge_* schema tables. The factory dropped all tables then only rebuilt to V0009, leaving Customer_Merge_* tables missing and causing SQL deadlocks and failures during EF model validation.
- Fix: ResetToV0010() was added to TestDatabaseFixture following the established sequential reset pattern.
- SafeTestWebApplicationFactory was updated to call ResetToV0010() instead of ResetToV0009().
- Final ApiTests passed: 267 passed, 0 failed.

## Database / Migration Acceptance

- V0010 migration is accepted.
- U0010 rollback is accepted.
- MigrationRollbackTests cover V0010/U0010.
- DbMigrator owns SchemaVersions.
- U0010 removes V0010 SchemaVersions record.
- U0010 soft-deactivates CUSTOMER_MERGE_* permissions (sets is_active=0) rather than hard-deleting them because the TR_Permissions_PreventDelete trigger blocks hard deletes.
- No production migration was executed.

## Security and Boundary Acceptance

- Backend authorization remains authoritative via IPermissionEvaluator.
- Frontend gating is not implemented in this phase.
- DENY-wins behavior remains unchanged.
- No raw SQL or internal exception exposure is accepted.
- No stack traces are exposed.
- Sanitized errors are required (InvalidOperationException messages are generic validation errors).
- No frontend files changed.
- No business docs changed.
- No production migration.
- No release tag.
- No push.

## Known Follow-Ups

- Frontend Customer Merge UI remains future work (Phase 1B.5-C).
- Future service/payment/document linked-module merge handling remains deferred.
- Future migrations must update test fixture reset target beyond V0010.
- Execution handler ActorId=0 placeholder should be resolved in future phases.
- Survivorship payload field-level application to target profile is deferred to future phases.
- Untracked scratch/decompiled/FixStrategy files remain and must not be staged.

## Project Owner Decision

The Project Owner accepts Phase 1B.5-B Customer Merge backend/data foundation implementation as complete.

## Authorization for Next Step

Authorized next task:
Phase 1B.5-C Customer Merge frontend scope and implementation planning only.

Frontend implementation requires separate Project Owner approval after the Phase 1B.5-C frontend scope and implementation plan is reviewed.

Do not authorize:
- frontend implementation,
- production migration,
- release tag,
- push.
