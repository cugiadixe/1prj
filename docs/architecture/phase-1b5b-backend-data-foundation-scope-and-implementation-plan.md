# Phase 1B.5-B Customer Merge Backend/Data Foundation Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.5 PO plan acceptance commit: da00b9b02d4fd0a3e921f63c8e95bf0033e8f25d

State:
- This document is backend/data foundation scope and implementation planning only.
- It does not authorize implementation.
- It does not authorize migration creation.

## Objective

Define the backend/data foundation implementation scope for Phase 1B.5 Customer Merge and Duplicate Resolution.

## Source Documents Reviewed

- docs/architecture/phase-1b5-project-owner-plan-acceptance.md
- docs/architecture/phase-1b5-customer-merge-duplicate-resolution-discovery-and-detailed-plan.md
- docs/architecture/post-1b4-project-owner-next-work-decision.md
- docs/architecture/post-1b4-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b4-project-owner-closure-acceptance.md
- PTKD-ERP-Master-Context.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/business/process-catalog.md
- docs/business/PTKD-Specification-v1.1.md
- src/backend/PTKD.Domain/Entities/Customer.cs
- src/backend/PTKD.Domain/Entities/Profile.cs
- src/backend/PTKD.Domain/Entities/CustomerCompanyContext.cs
- src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerCompanyContextConfiguration.cs
- src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerChangeRequestConfiguration.cs
- src/backend/PTKD.Application/Customers/
- src/backend/PTKD.Api/Controllers/CustomersController.cs
- src/backend/PTKD.Api/Controllers/CustomerMasterChangeController.cs
- src/backend/PTKD.Application/Workflow/
- src/backend/PTKD.Domain/Entities/WorkflowInstance.cs
- database/migrations/
- database/rollbacks/
- tests/backend/

## Confirmed Existing Foundation

- Customer entity/profile/company context foundation: Separate Profile (identity) and Customer (status).
- CustomerMasterChange foundation: Exists to handle process-based state changes (Draft -> Submitted -> Executed).
- Workflow runtime/execution handler pattern: Exists.
- API v2 conventions: Base path `/api/v2`, ProblemDetails errors.
- Migration/rollback conventions: Versioned `Vxxxx` and `Uxxxx` SQL scripts, SchemaVersions via DbMigrator.
- Test patterns: UnitTests, IntegrationTests, ApiTests, MigrationRollbackTests against `PTKD_TEST_PHASE1A2`.

## Proposed Backend/Data Scope

- merge request persistence (reuse CustomerChangeRequest or dedicated table Customer_Merge_Requests).
- duplicate candidate snapshot for review.
- survivor/source linkage (update Customer with SurvivorCustomerId).
- source customer merged status (update Customer status to MERGED).
- audit/history (append-only history of the merge event).
- rowversion/concurrency (strict checking on target and source).
- workflow execution boundary (handler logic applied upon final approval).
- permissions (read/create/admin execution).
- API v2 backend endpoints (search duplicates, create merge request, execute).
- migration/rollback plan (V0010 schema change).
- backend tests (idempotency, security, validation).

## Proposed Database Design

### Customer Extension
- `Status`: Enforce support for `MERGED` status.
- `SurvivorCustomerId`: `UNIQUEIDENTIFIER NULL`. FK to `Customer.Id`. Links merged source to canonical target.
- `RowVersion`: existing concurrency check.

### Customer_Merge_Requests (or Extension of CustomerChangeRequest)
- `Id`: `UNIQUEIDENTIFIER PK`
- `TargetCustomerId`: `UNIQUEIDENTIFIER FK` (Survivor)
- `SourceCustomerId`: `UNIQUEIDENTIFIER FK` (Duplicate to merge)
- `Status`: `VARCHAR` (Draft, Submitted, Approved, Rejected, Executed)
- `RequestedBy`, `RequestedAt`, `CompanyId`
- `SurvivorshipPayload`: `NVARCHAR(MAX)` JSON snapshot of which fields to keep.
- `RowVersion`: `ROWVERSION`

### Migration Impact
- V0010: Add SurvivorCustomerId, Add Customer_Merge_Requests table.
- U0010: Drop table, drop column.

## Proposed Domain/Application Design

- Domain Entities: `CustomerMergeRequest` root.
- Application Service: `ICustomerMergeService` containing business rules and concurrency checks.
- DTOs: `CustomerMergeRequestDto`, `CreateCustomerMergeCommand`.
- Duplicate detection query boundary: Look for exact CCCD or Phone matches where status is ACTIVE.
- Merge request creation: Validates source/target are active, no pending requests exist for either.
- Approved execution handler: Modifies source status to MERGED, updates SurvivorCustomerId, updates Profile fields based on survivorship, creates Audit log, completes workflow.
- Idempotency: Execution handler checks if request is already EXECUTED.
- Rejected/non-approved no mutation: Request transitions to REJECTED, no customer data is changed.
- Concurrency: Ensure rowversions of both source and target match at execution time.

## Proposed Workflow Design

- Process Code: `CUSTOMER_MERGE_DUPLICATE`
- Approval flow usage: Follows standard company-specific or global approval bindings.
- Execution handler boundary: Runs atomically inside a transaction after the final approval step.
- Retry/idempotency: Safe to retry if transient database failure occurs during execution.
- Audit: System generates standard workflow audit records.

## Proposed API v2 Design

### `GET /api/v2/customers/duplicates`
- Purpose: Find potential duplicate candidates.
- Permission: `CUSTOMER_MERGE_REQUEST_CREATE`
- Request: CCCD, Phone, Name (optional fuzzy)
- Response: List of `CustomerDto` matches.

### `POST /api/v2/customers/merge-requests`
- Purpose: Create a new merge request.
- Permission: `CUSTOMER_MERGE_REQUEST_CREATE`
- Request: `CreateCustomerMergeRequestDto` (SourceId, TargetId, Survivorship definition)
- Response: `CustomerMergeRequestDto`

### `GET /api/v2/customers/merge-requests/{id}`
- Purpose: View merge request details for review.
- Permission: `CUSTOMER_MERGE_REQUEST_VIEW` or `CUSTOMER_MERGE_REQUEST_ADMIN_VIEW`
- Response: `CustomerMergeRequestDto`

### `POST /api/v2/customers/merge-requests/{id}/execute` (Internal/Workflow)
- Purpose: Execute the approved merge.
- Handled by workflow engine, but may expose explicit endpoint if workflow dictates.

## Permission and Security Plan

Proposed Permission Codes (require PO approval):
- `CUSTOMER_MERGE_REQUEST_CREATE`
- `CUSTOMER_MERGE_REQUEST_VIEW`
- `CUSTOMER_MERGE_REQUEST_ADMIN_VIEW`
- `CUSTOMER_MERGE_EXECUTE`

Confirm:
- Backend authorization is authoritative.
- Frontend gating is for convenience only.
- DENY wins.
- No raw sensitive payload exposure in errors/logs.
- No SQL/internal exception exposure (use ProblemDetails).
- Append-only audit.

## Migration and Rollback Strategy

- V0010_AddCustomerMergeFoundation.sql
- U0010_RemoveCustomerMergeFoundation.sql
- SchemaVersions via DbMigrator only.
- Run MigrationRollbackTests against `PTKD_TEST_PHASE1A2` to ensure safe up/down.
- No production migration authorized.

## Test Strategy

- UnitTests: Domain rules (cannot merge with self, cannot merge inactive).
- IntegrationTests: Database constraints, FKs, EF Core mapping.
- ApiTests: Endpoint status codes, validation formatting.
- MigrationRollbackTests: V0010 up and U0010 down.
- Permission/security tests: Unauthenticated and unauthorized access denied (401/403).
- Concurrency tests: Simulating concurrent updates to target/source during execution.
- Idempotency tests: Executing the same approved request twice.
- Rejected/no mutation tests: Ensure rejected request leaves Customer records untouched.

## Explicitly Out of Scope

- implementation in this task,
- migration creation in this task,
- frontend implementation,
- production migration,
- release tag,
- push,
- destructive delete,
- automatic fuzzy merge without review,
- service/payment module implementation,
- business requirement changes.

## Open Decisions / Blockers

1. **Exact survivorship rules for conflicting single-value fields**: Does not block backend foundation (we store JSON).
2. **Merge reversal policy**: Does not block backend foundation. Reversal can be manual SQL for now.
3. **Overlapping CustomerCompanyContext handling**: Blocks implementation. Must decide if contexts combine or error. Recommendation: Combine contexts (UPSERT).
4. **Fuzzy matching for names**: Does not block backend foundation. Can start with exact match.
5. **Approval flow details**: Does not block backend foundation. We plug into existing workflow.
6. **Permission catalog changes**: Blocks implementation. Need final permission codes.
7. **Future linked service/payment/document impact**: Blocks implementation. Need to ensure FK cascade logic or manual reassignment logic is defined before data goes into prod.

## Recommended Implementation Boundaries

Phase 1B.5-B backend/data foundation implementation only:
- no frontend,
- no production migration,
- no tag/push.

## Project Owner Approval Required

This plan does not authorize implementation.
Implementation may begin only after Project Owner accepts this Phase 1B.5-B backend/data scope and implementation plan.
