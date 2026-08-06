# Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot Project Owner Implementation Authorization

## Status

AUTHORIZED — PHASE 1B.3-B4 CREATE_CUSTOMER PILOT IMPLEMENTATION MAY PROCEED

## Authorized Implementation

Phase 1B.3-B4 — CREATE_CUSTOMER Workflow Pilot

## Authorization Baseline

f118636cc0184e237273a13894d63d75d84924a0

## Accepted Commits

| Role | Hash |
|---|---|
| B4 CREATE_CUSTOMER implementation plan commit | f118636cc0184e237273a13894d63d75d84924a0 |
| B4 plan acceptance commit | 94912ee14c94240b9be8c50a4c807d3f8b31d0e6 |
| B4 plan commit | 93607eb57c4a4aee3f2dd0ecba8a00135f3db87e |
| B3 final acceptance commit | bd451869b83dd9716422bdcc53d3f628c363232e |
| B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |

---

## Project Owner Decision

The Project Owner authorizes implementation of the limited Phase 1B.3-B4 CREATE_CUSTOMER workflow pilot under the accepted implementation plan and constraints below.

---

## Authorized Pilot

CREATE_CUSTOMER only.

---

## Authorized Direct-Create Coexistence

- Option A — alternate proposal path is approved.
- Existing direct customer create remains unchanged.
- CREATE_CUSTOMER pilot adds a separate approval proposal path.
- No existing direct-create authorization behavior may be removed in B4.
- Future replacement or configuration-gated direct-create behavior remains deferred.

---

## Authorized Backend Scope

- CustomerProposalController or equivalent business-specific proposal controller.
- CustomerProposalService or equivalent application service.
- CreateCustomerExecutionHandler or equivalent approved execution handler.
- IWorkflowExecutionHandler interface/factory or equivalent execution dispatch mechanism.
- CustomerChangeRequest entity or equivalent stable proposal/linkage entity.
- DTOs and validators required for proposal creation/status.
- Backend audit events required for proposal, workflow start, execution, failure, and linkage.
- Backend sanitized error handling.
- Backend tests required by the implementation plan.

---

## Authorized Frontend Scope

- Customer proposal entry point.
- Proposal submit UX.
- Proposal status/detail UX.
- Link to existing workflow instance detail.
- Existing My Approvals UI reused for approver actions.
- Existing Workflow Admin UI reused for configuration/binding.
- Frontend tests required by the implementation plan.

---

## Authorized Database/Migration Scope

- V0007 migration and U0007 rollback are authorized if required for CustomerChangeRequest or equivalent proposal/linkage table.
- Migration must be limited to CREATE_CUSTOMER pilot support.
- Migration must include required indexes, constraints, rowVersion/concurrency fields, and rollback.
- Production migration/release remains deferred.

---

## Authorized PermissionCodes.cs Wiring

- PermissionCodes.cs may be updated only to wire the already documented permission-catalog entry: CUSTOMER_CHANGE_REQUEST_CREATE.
- This authorization does not permit adding unrelated permission codes.
- This authorization does not permit changing permission-catalog.md.
- If implementation requires permission-catalog.md changes, stop and request separate Project Owner approval.
- If repository seed/mapping code must include CUSTOMER_CHANGE_REQUEST_CREATE to align with existing catalog entry, that limited seed/mapping change is authorized.
- DENY wins remains backend-enforced.
- Backend remains authoritative.

---

## Authorized Safe Payload Strategy

- Metadata-only payload summary.
- Allowed safe summary fields only, such as CustomerCode, FullName, CompanyId, and proposal metadata.
- No raw PayloadJson display.
- No BeforeDataJson display.
- No CCCD/identity number, phone, address, sensitive customer fields, or raw proposal JSON in UI.
- No raw sensitive data logging.
- payload_hash and workflow_snapshot_json remain backend-owned.

---

## Authorized Execution Strategy

- Workflow final approval must move to PENDING_EXECUTION or equivalent accepted execution boundary.
- Execution handler must be idempotent.
- Execution handler creates the customer only after final approval.
- Execution success/failure must be recorded separately from approval status.
- APR-008 approval status and execution status separation must be preserved.
- APR-009 idempotent execution requirement must be preserved.
- Execution failures must not silently create duplicate customers.
- Retry/failure state must be explicit and auditable.

---

## Authorized Entity-to-Instance Linkage

- CustomerChangeRequest or equivalent stable proposal entity is authorized as BusinessEntityType/BusinessEntityId target.
- WorkflowInstance.BusinessEntityType should identify the proposal entity, not a non-existent Customer record.
- Final created CustomerId must be linked back to the proposal after successful execution.
- No ambiguous duplicate workflow/business state is allowed.

---

## Authorized Tests

- Backend unit tests for proposal service, execution handler, permission checks, safe payload summary, and idempotency.
- Backend API/integration tests for proposal creation/status/linkage.
- Workflow runtime integration tests for final approval and execution transition.
- Frontend tests for proposal entry/status/link UX.
- Regression tests for existing direct customer create path.
- Regression tests for B2 Workflow Admin routes.
- Regression tests for B3 My Approvals routes/actions.
- Deferred behavior tests confirming no My Requests, no action history, and no reject UI/API.

---

## Explicitly Deferred

- My Requests UI/API.
- Action history/timeline UI/API.
- Reject UI/API.
- Active instance migration.
- Generic workflow instance creation UI.
- CUSTOMER_MASTER_CHANGE implementation.
- Service/Payment/Merge/Card/Plot/ENTITY implementation.
- Production migration/release.
- Broad workflow engine redesign.
- New permission-catalog entries.
- Unrelated PermissionCodes.cs additions.

---

## Implementation Stop Conditions

Stop before implementation commit and request Project Owner clarification if:

- CUSTOMER_CHANGE_REQUEST_CREATE cannot be wired without changing permission-catalog.md.
- CustomerChangeRequest linkage cannot be implemented safely.
- Execution handler cannot be made idempotent.
- Final approval cannot trigger execution without broad workflow engine redesign.
- Existing direct customer create would need to be broken or replaced.
- Raw sensitive payload would need to be exposed.
- New permission codes beyond CUSTOMER_CHANGE_REQUEST_CREATE are required.
- Scope expands into CUSTOMER_MASTER_CHANGE, Service, Payment, Merge, Card, Plot, ENTITY, or production release.

---

## Authorized Next Task

Implement Phase 1B.3-B4 CREATE_CUSTOMER workflow pilot under this authorization.

---

## Conclusion

PHASE 1B.3-B4 CREATE_CUSTOMER PILOT IMPLEMENTATION AUTHORIZED
