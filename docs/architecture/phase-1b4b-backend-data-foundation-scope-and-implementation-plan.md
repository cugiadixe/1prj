# Phase 1B.4-B Customer Master Backend/Data Foundation Scope and Implementation Plan

## Status

PROPOSED — AWAITING PROJECT OWNER BACKEND/DATA SCOPE ACCEPTANCE
PHASE 1B.4-B BACKEND/DATA FOUNDATION SCOPE ACCEPTED — SEE phase-1b4b-project-owner-backend-data-scope-acceptance.md

## Planning Baseline

- Phase 1B.4 Project Owner plan acceptance commit:
  06cb36218503f6f4c01a05b05e8fb077a16a767d
- Phase 1B.4 discovery and detailed plan commit:
  4dec520d41fc1ad6de9ec4b25a50415b179f2d0c
- Phase 1B.4 selection commit:
  420f76df3d37218c47d98168923b5fa559fc78d9
- B5-D Project Owner closure acceptance commit:
  0a4149fb233c516210acba197a8b2977cbc39170
- B5-B Project Owner backend acceptance commit:
  c42734e351404d9788b82e2049c92f6de09baf18

## Purpose

Define the exact backend/data foundation scope for Phase 1B.4 Customer Master Expansion before any implementation begins.

## Source Documents Reviewed

- `docs/architecture/phase-1b4-project-owner-plan-acceptance.md`
- `docs/architecture/phase-1b4-customer-master-expansion-discovery-and-detailed-plan.md`
- `docs/business/process-catalog.md`
- `docs/business/business-rules.md`
- `src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerChangeRequestConfiguration.cs`

## Confirmed Implementation Basis

- **Shared customer master governance**: Updates to existing customers require strict control.
- **Proposal/approval model**: Staff propose changes; approval handles execution.
- **Customer data-admin authority**: Only designated individuals can officially alter data without approval (or they are the designated approvers).
- **Protected/critical field restrictions**: Certain fields cannot be edited directly by regular staff.
- **B5 workflow runtime reuse**: Full reliance on existing Approval engine for routing and state management.
- **Backend-authoritative permissions**: Authorization must be strictly enforced on backend endpoints.
- **Rowversion/concurrency expectations**: `RowVersion` checking is required to avoid lost updates.
- **Audit/history expectations**: Comprehensive logging of what changed, by whom, and when.

## Proposed Phase 1B.4-B Backend/Data Scope

- **Customer master change request data model**: Extend existing `CustomerChangeRequest` to track the `TargetCustomerId` for updates.
- **Official customer data update boundary**: Only the workflow execution handler or specific authorized endpoint can save to `Customers`.
- **Before/after value storage strategy**: Store structured before/after diffs in the `PayloadJson` column.
- **Workflow instance linkage**: Reuse `WorkflowInstanceId` on the request table to track approval lifecycle.
- **Status lifecycle**: Reuse existing states (`DRAFT`, `SUBMITTED`, `APPROVED`, `EXECUTED`, `FAILED`, `WITHDRAWN`).
- **Protected field handling**: Backend validation ensures only allowed fields are altered based on permissions.
- **Duplicate detection planning**: Implement backend validation to warn/block overlapping CCCDs during update, where supported.
- **Audit/event recording**: Write standard domain events for customer updates.
- **API v2 backend surface**: Expose endpoints for `POST` (create), `GET` (detail), etc.
- **Application service boundaries**: Isolate proposal logic from official customer write logic.
- **Validation rules**: Ensure target row version matches upon final apply.
- **Permission enforcement**: Implement `CUSTOMER_CHANGE_REQUEST_CREATE` checks.
- **Test coverage**: Unit/Integration tests for handler idempotency and concurrency.

## Explicit Non-Scope for 1B.4-B

- frontend implementation,
- production migration,
- production release,
- customer merge implementation unless separately approved,
- Service module,
- Payment module,
- Card print/reprint flow,
- Plot/cemetery location flow,
- ENTITY expansion,
- export/download,
- safe user lookup/reassign expansion,
- broad workflow engine rewrite,
- broad frontend redesign,
- any business behavior not supported by current docs.

## Proposed Database Design

*Note: Proposed and subject to Project Owner acceptance.*

- **Proposed new tables**: None. Use existing `Customer_Change_Requests`.
- **Proposed columns**: 
  - Add `target_customer_id` (bigint, null) to `Customer_Change_Requests` to distinguish updates from creations.
  - Add `target_row_version` (binary, null) to capture the base version.
- **PK/FK strategy**: `target_customer_id` references `Customers(id)`.
- **Rowversion strategy**: Maintain `RowVersion` on customer. Execution fails if it doesn't match `target_row_version`.
- **Company scope handling**: Restrict visibility using `company_id`.
- **Workflow_instance_id linkage**: Existing column.
- **Requester/customer/data-admin references**: Existing relations.
- **Status values**: Existing logic.
- **Before/after snapshot approach**: Kept in `payload_json`.
- **Sensitive field redaction/storage rules**: Avoid dumping raw CCCD strings into generic logs; ensure `payload_json` access requires permissions.
- **Duplicate candidate storage or read-only detection approach**: Dynamic query at submission and execution.
- **Created/updated/audit columns**: Existing standard columns.
- **Indexes**: Add index for `target_customer_id`.
- **Constraints**: Enforce validity of target ID when `process_code == 'CUSTOMER_MASTER_CHANGE'`.
- **Rollback considerations**: Drop added columns safely in rollback.

## Proposed Migration and Rollback Strategy

- **Next migration number/name**: Next sequential EF migration (e.g., `AddCustomerMasterChangeFields`).
- **Rollback script name**: Matching sequential rollback script.
- **Dependency order**: Applies after all previous B5 migrations.
- **SchemaVersions behavior**: Inserts record on up, deletes on down.
- **SQL Server constraints**: Standard foreign key handling.
- **No production migration**: Strictly local/test environments.
- **Test DB only validation**: Run `MigrationRollbackTests` to ensure schema downgrades correctly.
- **MigrationRollbackTests update plan**: Validate new columns exist/dropped.

## Proposed Domain/Application Design

- **Domain entities or value objects**: Extend `CustomerChangeRequest`.
- **Application service interfaces**: Add `ICustomerMasterChangeService`.
- **Commands/DTOs**: `CreateCustomerMasterChangeRequest`, `CustomerMasterChangeDetailDto`.
- **Validation rules**: Cannot update an inactive customer; required fields checking.
- **Workflow handler/executor boundaries**: Implement `CUSTOMER_UPDATE_FROM_APPROVAL` execution handler.
- **Idempotency rules**: Handler tracks completion state; duplicate invocations return success if already executed.
- **Concurrency handling**: Catch `DbUpdateConcurrencyException` and mark request `FAILED`.
- **Audit behavior**: Emit `CustomerUpdatedEvent`.
- **Sanitized error behavior**: Standard JSON problem details.

## Proposed API v2 Design

- **Create customer master change request**: `POST /api/v2/customers/{id}/change-requests`
- **List my customer master change requests**: `GET /api/v2/customers/my-change-requests`
- **Get request detail**: `GET /api/v2/customers/change-requests/{id}`
- **Submit/start workflow**: If separate, `POST /api/v2/customers/change-requests/{id}/submit`
- **Get safe before/after diff**: Returned via detail endpoint mapping.
- **Data-admin apply approved change**: Internal workflow execution.
- **Duplicate check endpoint**: Only if supported (e.g. `POST /api/v2/customers/duplicate-check`).
- **Error model**: Standard problem details (400, 403, 404, 409).
- **Permission requirements per endpoint**: `CUSTOMER_CHANGE_REQUEST_CREATE` or data admin roles.
- **Company scope behavior**: Constrained via authorization handlers.

## Proposed Workflow Integration

- **CUSTOMER_MASTER_CHANGE process code**: Required configuration.
- **Workflow definition requirement**: A valid workflow definition bound to this code.
- **How B5 My Requests and Action History are reused**: Integrated via the standard request ID and `Approval_Requests` bridge.
- **Approve/reject semantics**: Approved triggers execution handler; Rejected sets state to terminal and cleans up.
- **Retry semantics for failed execution**: Allowed via standard B5 `RETRY` action.
- **Terminal states**: `EXECUTED`, `FAILED`, `WITHDRAWN`, `REJECTED`.
- **Official update timing**: Strictly within the execution handler transaction.
- **Prevention of double-apply**: Linkage check before updating.
- **Behavior if customer rowversion changed before apply**: Transaction rolls back, execution is marked `FAILED`.

## Proposed Permission Codes

- `CUSTOMER_CHANGE_REQUEST_CREATE` (Likely exists, scope: COMPANY/GLOBAL)
- `CUSTOMER_CHANGE_REQUEST_VIEW` (For tracking)
- `CUSTOMER_CHANGE_REQUEST_ADMIN_VIEW` (For review)
- `CUSTOMER_CHANGE_REQUEST_APPLY` (If manual data-admin override is supported)
- `CUSTOMER_DUPLICATE_CHECK` (Only if needed)
- **Scope**: COMPANY overrides GLOBAL.
- **Receivers**: Regular staff for create; Managers/Data Admins for review/apply.
- **Deny-wins rule**: Active across all new endpoints.
- **Backend enforcement**: Decorate controllers with policies.

## Security and Data Exposure Rules

- **Protected field restrictions**: Hardcoded backend whitelist of modifiable fields via proposal.
- **Before/after value redaction**: Do not expose sensitive JSON payloads in unauthorized API calls.
- **Raw payload restrictions**: Read restricted to requester and workflow participants.
- **No sensitive data in logs**: Exclude PII from `ILogger`.
- **No SQL/internal exception leakage**: Standard middleware.
- **Audit completeness**: Mandatory event generation.
- **Backend-authoritative authorization**: Claims inspection.
- **Company-scope enforcement**: Verified on target customer.
- **Immutable action/history rules**: Append-only audit.

## Proposed Test Strategy

- **Unit tests**: Handler logic, payload serialization.
- **Integration tests**: Concurrency (RowVersion) simulation, double execution protection.
- **API tests**: End-to-end 403 Forbidden checks, JSON response formats.
- **Migration/rollback tests**: Validate table structural changes.
- **Permission/security tests**: `EffectivePermissions` simulated checks.
- **Workflow runtime tests**: End-to-end processing with mock approvals.
- **Concurrency/rowversion tests**: Specifically test race conditions.
- **Idempotency/double-apply tests**: Repeated execution tests.
- **Regression tests for existing customer proposal APIs**: Ensure `CREATE_CUSTOMER` remains unaffected.

## Proposed Implementation File List

**Database migration/rollback files**:
- `database/migrations/xxx_AddCustomerChangeRequestTargetFields.sql`
- `database/rollbacks/xxx_AddCustomerChangeRequestTargetFields.sql`

**Backend domain files**:
- `src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs` (Modify)

**Backend application files**:
- `src/backend/PTKD.Application/Customers/Handlers/CustomerMasterChangeExecutionHandler.cs` (New)
- `src/backend/PTKD.Application/Customers/DTOs/CustomerMasterChangeRequestDto.cs` (New)
- `src/backend/PTKD.Application/Customers/Services/CustomerChangeRequestService.cs` (Modify/New)

**Backend API/controller files**:
- `src/backend/PTKD.Api/Controllers/CustomerChangeRequestsController.cs` (Modify/New)

**Permission files**:
- `src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs` (Modify)

**Test files**:
- `tests/backend/PTKD.IntegrationTests/Customers/CustomerMasterChangeExecutionTests.cs`
- `tests/backend/PTKD.ApiTests/Customers/CustomerChangeRequestApiTests.cs`

**Documentation files**:
- `docs/architecture/...`

## Acceptance Criteria for Future 1B.4-B Implementation

- build passes,
- unit/integration/API tests pass,
- migration/rollback passes,
- permissions enforced backend-side,
- protected fields handled safely,
- workflow linkage works,
- approved change can be applied once,
- rejected change does not alter official data,
- stale rowversion blocks or requires controlled retry,
- no raw sensitive data exposure,
- git diff/check clean.

## Open Decisions Requiring Project Owner Approval

- exact protected field list,
- exact process trigger,
- exact workflow definition assignment,
- duplicate detection behavior,
- whether duplicate detection blocks submission,
- whether merge remains deferred,
- exact permission names,
- exact audit payload fields,
- exact before/after redaction model,
- whether data-admin apply is manual or automatic after approval,
- handling stale official customer rowversion,
- manual validation data setup.

## Risks and Stop Conditions

- unsupported business requirements discovered,
- protected field ambiguity,
- customer merge scope creep,
- sensitive payload exposure risk,
- migration rollback uncertainty,
- permission model conflict,
- workflow semantics conflict with B5,
- concurrency/idempotency uncertainty,
- missing PO decisions.

## Recommended Next Step

Recommend Project Owner acceptance of this backend/data scope before implementation.

## Conclusion

PHASE 1B.4-B BACKEND/DATA FOUNDATION PLAN PROPOSED — AWAITING PROJECT OWNER SCOPE ACCEPTANCE
