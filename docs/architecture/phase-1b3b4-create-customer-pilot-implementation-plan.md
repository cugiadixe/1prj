# Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot Implementation Plan

## Status

PHASE 1B.3-B4 CREATE_CUSTOMER PILOT IMPLEMENTATION AUTHORIZED — SEE phase-1b3b4-project-owner-implementation-authorization.md

## Baseline

94912ee14c94240b9be8c50a4c807d3f8b31d0e6

## Accepted Plan

93607eb57c4a4aee3f2dd0ecba8a00135f3db87e

## Accepted Plan Acceptance

94912ee14c94240b9be8c50a4c807d3f8b31d0e6

## Selected Pilot

CREATE_CUSTOMER

---

## Implementation Nature

- Limited backend integration required.
- Limited frontend integration required.
- Database migration required (V0007) — new CustomerChangeRequest table.
- No permission-catalog.md update.
- CUSTOMER_CHANGE_REQUEST_CREATE must be added to PermissionCodes.cs (exists in permission catalog but not in code — see Blockers).

---

## Direct-Create Coexistence Decision

**Selected model: Option A — Alternate proposal path.**

- Existing direct customer create route (POST /customers with CUSTOMER_CREATE_FINAL) remains unchanged. No code modification to CustomersController.Create.
- CREATE_CUSTOMER pilot adds a separate proposal submission endpoint (POST /customers/proposals).
- Staff with CUSTOMER_CHANGE_REQUEST_CREATE but without CUSTOMER_CREATE_FINAL use the proposal path.
- Staff/admins with CUSTOMER_CREATE_FINAL continue using the existing direct-create path unchanged.
- No existing direct-create permission behavior is removed in B4.
- Future replacement or configuration-gated switching remains deferred.
- Both paths coexist: the CustomerCreatePage renders either "Create Customer" (direct) or "Submit for Approval" (proposal) based on the user's effective permissions.

**Coexistence rules:**

| User has CUSTOMER_CREATE_FINAL | User has CUSTOMER_CHANGE_REQUEST_CREATE | Path |
|---|---|---|
| Yes | Any | Direct create (existing POST /customers). Proposal path also available if user chooses. |
| No | Yes | Proposal path only (POST /customers/proposals → workflow). |
| No | No | No create access. "New Customer" button hidden. |

---

## Design Rationale: CustomerChangeRequest Entity

### Problem

CreateWorkflowInstanceRequest requires a `long BusinessEntityId`. For CREATE_CUSTOMER, the customer does not exist yet — there is no Customer record to reference. Using BusinessEntityId=0 for all proposals would prevent distinguishing multiple concurrent proposals and break entity-to-instance linkage.

### Solution

Introduce a `CustomerChangeRequest` table to represent the proposal lifecycle:

1. Staff submits a customer proposal → a CustomerChangeRequest record is created with a DB-generated Id.
2. A WorkflowInstance is created with BusinessEntityType="CustomerChangeRequest" and BusinessEntityId=changeRequest.Id.
3. The CustomerChangeRequest stores the proposed data and tracks execution state independently from workflow status.
4. After final approval and execution, the CustomerChangeRequest links to the created Customer.Id.
5. This design also supports future CUSTOMER_MASTER_CHANGE proposals (TargetCustomerId set, BeforeDataJson populated).

### Why not derived-only linkage

Querying WorkflowInstance by processCode+businessEntityId requires a stable businessEntityId. Without a proposal record, there is no stable ID for a not-yet-created customer. Derived-only linkage (DEC-1B3B4-13) is not viable for CREATE_CUSTOMER without a proposal entity.

---

## Proposed Backend Scope

### New Entity: CustomerChangeRequest

File: `src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs`

| Property | Type | Notes |
|---|---|---|
| Id | long | BIGINT IDENTITY(1,1) PK |
| RequestType | string | CREATE_CUSTOMER or CUSTOMER_MASTER_CHANGE |
| TargetCustomerId | long? | null for CREATE, set for future CHANGE |
| WorkflowInstanceId | long? | FK to Workflow_Instances, set after workflow instance created |
| RequestStatus | string | DRAFT, PENDING_APPROVAL, APPROVED, EXECUTING, EXECUTED, FAILED, WITHDRAWN, CANCELLED |
| ProposalPayloadJson | string | JSON of proposed customer fields |
| ResultCustomerId | long? | set after execution creates the Customer |
| FailureReason | string? | set if execution fails |
| CreatedByUserId | long | requester |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |
| RowVersion | byte[] | SQL ROWVERSION |

### New EF Configuration: CustomerChangeRequestConfiguration

File: `src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerChangeRequestConfiguration.cs`

- Table: `Customer_Change_Requests`
- PK: Id (bigint identity)
- Index on WorkflowInstanceId
- Index on TargetCustomerId (filtered, non-null)
- Index on RequestType + RequestStatus (for listing)
- FK to Workflow_Instances (optional, no cascade)
- CHECK constraint on RequestType: CREATE_CUSTOMER, CUSTOMER_MASTER_CHANGE
- CHECK constraint on RequestStatus: DRAFT, PENDING_APPROVAL, APPROVED, EXECUTING, EXECUTED, FAILED, WITHDRAWN, CANCELLED

### New Endpoint: POST /customers/proposals

File: `src/backend/PTKD.Api/Controllers/CustomerProposalController.cs`

Route: `api/v2/customers/proposals`

| Method | HTTP | Route | Permission | Request DTO | Response |
|---|---|---|---|---|---|
| CreateProposal | POST | `/` | CUSTOMER_CHANGE_REQUEST_CREATE (Global) | CreateCustomerProposalRequest | CustomerProposalDto (201) |
| GetProposal | GET | `/{proposalId}` | CUSTOMER_CHANGE_REQUEST_CREATE (Global) | long | CustomerProposalDto |
| ListMyProposals | GET | `/my-proposals` | [Authorize] (self-scoped) | query params | CustomerProposalListItemDto[] |

Note: ListMyProposals is a lightweight alternative to My Requests (GAP-1 deferred). It returns only the current user's CustomerChangeRequest records with workflow status, not a generic "my workflow requests" list.

### New DTOs

File: `src/backend/PTKD.Application/Customers/DTOs/CustomerProposalDtos.cs`

**CreateCustomerProposalRequest**: Same fields as CreateCustomerRequest (CustomerCode, FullName, Cccd, Dob, etc.) plus CompanyId (required — determines workflow binding scope).

**CustomerProposalDto**: Id, RequestType, RequestStatus, WorkflowInstanceId, ProposalPayloadJson (metadata-only summary, not raw), CreatedByUserId, CreatedAt, UpdatedAt, RowVersion. Plus safe metadata fields: ProposedCustomerCode, ProposedFullName, ProposedCompanyId.

**CustomerProposalListItemDto**: Id, RequestType, RequestStatus, WorkflowInstanceId, ProposedCustomerCode, ProposedFullName, CreatedAt.

### New Validator

File: `src/backend/PTKD.Application/Customers/Validations/CustomerProposalValidator.cs`

CreateCustomerProposalRequestValidator: Same field-level rules as CreateCustomerRequestValidator (CustomerCode max 50, FullName required max 200, Cccd max 20, etc.) plus CompanyId > 0.

### New Service: ICustomerProposalService

File: `src/backend/PTKD.Application/Customers/Services/ICustomerProposalService.cs`
File: `src/backend/PTKD.Application/Customers/Services/CustomerProposalService.cs`

Methods:

| Method | Description |
|---|---|
| CreateProposalAsync | Validates proposal, runs duplicate check (CUS-005 pre-submit), creates CustomerChangeRequest, calls WorkflowRuntimeService.CreateInstanceAsync to create workflow instance, links WorkflowInstanceId back to request, writes audit event. All in a Serializable transaction. |
| GetProposalByIdAsync | Returns proposal with safe metadata summary. |
| GetMyProposalsAsync | Returns current user's proposals with workflow status. |
| ExecuteApprovedProposalAsync | Called by execution handler. Deserializes ProposalPayloadJson, runs final duplicate check (CUS-005 pre-execution), creates Customer+Profile via existing CustomerService.CreateCustomerAsync logic, updates CustomerChangeRequest with ResultCustomerId, writes audit. Idempotent via CorrelationId. |

### CreateProposalAsync detailed flow

1. Validate CreateCustomerProposalRequest (FluentValidation).
2. Run duplicate check: call existing CheckDuplicatesAsync with Cccd/Phone. If duplicates found and blocking, throw BusinessRuleValidationException with CUS_DUPLICATE_CCCD.
3. Serialize proposal fields to ProposalPayloadJson (same shape as CreateCustomerRequest JSON).
4. Create CustomerChangeRequest record (RequestType=CREATE_CUSTOMER, RequestStatus=PENDING_APPROVAL).
5. Save to get DB-generated Id.
6. Call WorkflowRuntimeService.CreateInstanceAsync with:
   - ProcessCode = "CREATE_CUSTOMER"
   - BusinessEntityType = "CustomerChangeRequest"
   - BusinessEntityId = changeRequest.Id
   - CompanyId = request.CompanyId
   - PayloadJson = proposalPayloadJson
   - BeforeDataJson = null (create, not change)
7. Update changeRequest.WorkflowInstanceId = instance.Id.
8. Write security audit event CUSTOMER_PROPOSAL_CREATED.
9. Commit transaction.

If WorkflowRuntimeService.CreateInstanceAsync fails (no binding, no assignees — WFD-007/WFD-008), the entire transaction rolls back including the CustomerChangeRequest.

### Execution Handler

File: `src/backend/PTKD.Application/Workflows/Services/IWorkflowExecutionHandler.cs`
File: `src/backend/PTKD.Application/Workflows/Handlers/CreateCustomerExecutionHandler.cs`

**IWorkflowExecutionHandler interface:**

```
Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct)
```

Registered by ProcessCode. The workflow runtime service calls the matching handler when an instance transitions to PENDING_EXECUTION.

**CreateCustomerExecutionHandler:**

1. Load CustomerChangeRequest by Id (from instance.BusinessEntityId).
2. Check idempotency: if changeRequest.RequestStatus is already EXECUTED, return (APR-009).
3. Set instance status to EXECUTING.
4. Set changeRequest.RequestStatus to EXECUTING.
5. Deserialize ProposalPayloadJson to CreateCustomerRequest.
6. Run final duplicate check (CUS-005 pre-execution). If duplicate found, set FAILED + FailureReason.
7. Call CustomerService.CreateCustomerAsync (reuse existing logic) with the deserialized request and the original requester's userId.
8. Set changeRequest.ResultCustomerId = created customer Id.
9. Set changeRequest.RequestStatus = EXECUTED.
10. Set instance to EXECUTED with afterDataJson = JSON of created customer summary.
11. Write audit event CUSTOMER_PROPOSAL_EXECUTED.
12. Commit.

On failure: set instance to FAILED, set changeRequest to FAILED + FailureReason. Write audit. Transaction commits the failure state.

### Workflow Runtime Service Modification

File: `src/backend/PTKD.Application/Workflows/Services/WorkflowRuntimeService.cs`

In ApproveStepAsync, after setting instance to PENDING_EXECUTION (line ~198), invoke the registered IWorkflowExecutionHandler for the instance's ProcessCode. This is the only modification to existing workflow runtime code.

Implementation options:
- (a) Inject `IEnumerable<IWorkflowExecutionHandler>` and match by ProcessCode.
- (b) Use a `IWorkflowExecutionHandlerFactory` that resolves by ProcessCode.
- (c) Dispatch via MediatR notification.

Recommended: option (b) — factory pattern. Simple, explicit, testable.

### Audit Events

| Event | When |
|---|---|
| CUSTOMER_PROPOSAL_CREATED | Proposal submitted, workflow instance created |
| CUSTOMER_PROPOSAL_EXECUTED | Execution handler created customer from approved proposal |
| CUSTOMER_PROPOSAL_FAILED | Execution handler failed (duplicate found, validation error) |
| CUSTOMER_PROPOSAL_WITHDRAWN | Requester withdrew workflow instance |

### Concurrency Behavior

- CustomerChangeRequest uses RowVersion for optimistic concurrency.
- WorkflowInstance uses RowVersion (existing).
- Execution handler checks CorrelationId for idempotency (APR-009).
- Final duplicate check uses Serializable transaction (existing CustomerService pattern).
- target_version recheck before execution (CUS-009): the CustomerChangeRequest RowVersion serves this role — if another process modified the request, concurrency conflict is raised.

### Error Handling

- No binding for CREATE_CUSTOMER: BusinessRuleValidationException WF_NO_BINDING → 422.
- No assignees resolved: BusinessRuleValidationException WF_NO_ASSIGNEES → 422.
- Duplicate CCCD at proposal time: BusinessRuleValidationException CUS_DUPLICATE_CCCD → 422.
- Duplicate CCCD at execution time: execution fails, instance set to FAILED.
- Concurrency conflict: ConcurrencyException → 409.
- Permission denied: 403 (RequirePermission attribute).
- Proposal not found: 404.

### Backend Tests

File locations under `tests/backend/`:

| Test File | Coverage |
|---|---|
| CustomerProposalServiceTests | CreateProposal happy path, duplicate check at submit, workflow instance creation, missing binding, missing assignees |
| CreateCustomerExecutionHandlerTests | Execute happy path, idempotency (already executed), duplicate at execution, failure handling, afterDataJson written |
| CustomerProposalControllerTests | POST /proposals 201, GET /proposals/:id 200/404, GET /my-proposals, permission denied 403 |
| WorkflowRuntimeService integration | ApproveStep triggers execution handler for CREATE_CUSTOMER, PENDING_EXECUTION → EXECUTING → EXECUTED flow |

---

## Proposed Frontend Scope

### Modified: CustomerCreatePage

File: `src/frontend/src/customers/CustomerCreatePage.tsx`

Changes:
- Check both `hasPermission('CUSTOMER_CREATE_FINAL', 'GLOBAL')` and `hasPermission('CUSTOMER_CHANGE_REQUEST_CREATE', 'GLOBAL')`.
- If user has CUSTOMER_CREATE_FINAL: existing behavior unchanged. Submit button says "Create Customer". Calls POST /customers.
- If user has CUSTOMER_CHANGE_REQUEST_CREATE but NOT CUSTOMER_CREATE_FINAL: submit button says "Submit for Approval". Calls POST /customers/proposals. On success, navigates to proposal detail or workflow instance detail.
- If user has both: show both buttons, or default to direct create with an option to submit for approval. Recommended: show "Create Customer" as primary (direct) since the user has final authority.
- Add CompanyId field (required for proposal path — determines workflow binding scope). Use company selector dropdown.

### Modified: CustomersPage (list)

File: `src/frontend/src/customers/CustomersPage.tsx`

Changes:
- "New Customer" button visibility: show if user has CUSTOMER_CREATE_FINAL OR CUSTOMER_CHANGE_REQUEST_CREATE (currently only checks CUSTOMER_CREATE_FINAL).

### New: CustomerProposalDetailPage

File: `src/frontend/src/customers/CustomerProposalDetailPage.tsx`

Displays:
- Proposal metadata: Id, RequestType, RequestStatus (color-coded tag), CreatedAt.
- Safe metadata summary: ProposedCustomerCode, ProposedFullName, ProposedCompanyId.
- No raw ProposalPayloadJson display.
- Link to workflow instance detail (/workflow/instances/:workflowInstanceId) if WorkflowInstanceId is set.
- If RequestStatus=EXECUTED: link to created customer (/customers/:resultCustomerId).
- If RequestStatus=FAILED: display FailureReason.

### New: CustomerMyProposalsPage (optional, lightweight)

File: `src/frontend/src/customers/CustomerMyProposalsPage.tsx`

Lightweight list of current user's proposals with status. Not a generic "My Requests" — scoped to CustomerChangeRequest only. This avoids depending on the deferred GAP-1 (My Requests backend).

### New Route and Menu

Routes to add in App.tsx:
- `/customers/proposals/new` → redirect to CustomerCreatePage (proposal mode via query param or context).
- `/customers/proposals/:proposalId` → CustomerProposalDetailPage.
- `/customers/my-proposals` → CustomerMyProposalsPage (optional).

Menu changes in AuthenticatedShell.tsx:
- Under Customers section: add "My Proposals" item, visible if user has CUSTOMER_CHANGE_REQUEST_CREATE.

### New: customerProposalApi.ts

File: `src/frontend/src/customers/customerProposalApi.ts`

| Function | HTTP | Endpoint |
|---|---|---|
| createProposal | POST | /customers/proposals |
| getProposal | GET | /customers/proposals/:id |
| getMyProposals | GET | /customers/proposals/my-proposals |

### Modified: Customer types.ts

Add:
- CreateCustomerProposalRequest (same fields as CreateCustomerRequest + companyId).
- CustomerProposal (id, requestType, requestStatus, workflowInstanceId, proposedCustomerCode, proposedFullName, proposedCompanyId, resultCustomerId, failureReason, createdAt, updatedAt, rowVersion).
- CustomerProposalListItem (id, requestType, requestStatus, workflowInstanceId, proposedCustomerCode, proposedFullName, createdAt).

### Validation and Error Handling

- Reuse existing customer errorMessages.ts patterns.
- Add proposal-specific error codes: CUS_PROPOSAL_NOT_FOUND, CUS_PROPOSAL_ALREADY_EXECUTED, WF_NO_BINDING, WF_NO_ASSIGNEES.
- Concurrency error handling: reuse existing isConcurrencyError + refresh pattern.
- Permission denied: reuse existing isPermissionDenied pattern.

### Safe Payload Display

- CustomerProposalDetailPage shows only safe metadata fields: ProposedCustomerCode, ProposedFullName, ProposedCompanyId.
- No raw ProposalPayloadJson, PayloadJson, or BeforeDataJson display anywhere.
- No CCCD, Phone, DOB, PermanentAddress, or other sensitive fields in the summary.
- WorkflowInstanceDetailPage continues showing only existing metadata (processCode, businessEntityType, businessEntityId, status) — no payload.

### Frontend Tests

| Test File | Coverage |
|---|---|
| CustomerCreatePage.test.tsx | Add: proposal submission path, permission-based button rendering, companyId field for proposals |
| CustomerProposalDetailPage.test.tsx | New: proposal detail rendering, status tags, workflow link, customer link after execution, failure display, no raw payload, 403/404 states |
| CustomerMyProposalsPage.test.tsx | New: list rendering, empty state, error state |
| customerProposalApi.test.ts | New: endpoint verification for createProposal, getProposal, getMyProposals |
| CustomersPage.test.tsx | Add: "New Customer" button visible with CUSTOMER_CHANGE_REQUEST_CREATE |

---

## Proposed Database Strategy

### New Migration: V0007__create_customer_change_request.sql

File: `database/migrations/V0007__create_customer_change_request.sql`

```sql
CREATE TABLE Customer_Change_Requests (
    id                    BIGINT IDENTITY(1,1) NOT NULL,
    request_type          VARCHAR(50)   NOT NULL,
    target_customer_id    BIGINT        NULL,
    workflow_instance_id  BIGINT        NULL,
    request_status        VARCHAR(30)   NOT NULL,
    proposal_payload_json NVARCHAR(MAX) NOT NULL,
    result_customer_id    BIGINT        NULL,
    failure_reason        NVARCHAR(2000) NULL,
    created_by_user_id    BIGINT        NOT NULL,
    created_at            DATETIME2(3)  NOT NULL,
    updated_at            DATETIME2(3)  NULL,
    row_version           ROWVERSION    NOT NULL,

    CONSTRAINT PK_Customer_Change_Requests PRIMARY KEY (id),
    CONSTRAINT FK_CCR_workflow_instance FOREIGN KEY (workflow_instance_id)
        REFERENCES Workflow_Instances(id),
    CONSTRAINT FK_CCR_target_customer FOREIGN KEY (target_customer_id)
        REFERENCES Customers(id),
    CONSTRAINT FK_CCR_result_customer FOREIGN KEY (result_customer_id)
        REFERENCES Customers(id),
    CONSTRAINT CK_CCR_request_type CHECK (request_type IN ('CREATE_CUSTOMER', 'CUSTOMER_MASTER_CHANGE')),
    CONSTRAINT CK_CCR_request_status CHECK (request_status IN ('DRAFT', 'PENDING_APPROVAL', 'APPROVED', 'EXECUTING', 'EXECUTED', 'FAILED', 'WITHDRAWN', 'CANCELLED'))
);

CREATE INDEX IX_CCR_workflow_instance_id ON Customer_Change_Requests(workflow_instance_id)
    WHERE workflow_instance_id IS NOT NULL;
CREATE INDEX IX_CCR_target_customer_id ON Customer_Change_Requests(target_customer_id)
    WHERE target_customer_id IS NOT NULL;
CREATE INDEX IX_CCR_created_by_status ON Customer_Change_Requests(created_by_user_id, request_status);
CREATE INDEX IX_CCR_request_type_status ON Customer_Change_Requests(request_type, request_status);
```

### Rollback: U0007__drop_customer_change_request.sql

File: `database/migrations/U0007__drop_customer_change_request.sql`

```sql
DROP TABLE IF EXISTS Customer_Change_Requests;
```

### PermissionCodes.cs Addition

File: `src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs`

Add constant:
```csharp
public const string CustomerChangeRequestCreate = "CUSTOMER_CHANGE_REQUEST_CREATE";
```

This is wiring an existing permission-catalog entry to code. No permission-catalog.md modification required.

### No Production Migration

V0007 is for development/sandbox only. Production migration requires separate approval (DEC-1B3B4-09).

### RowVersion/Concurrency

- Customer_Change_Requests uses ROWVERSION for optimistic concurrency (same pattern as Customers and Workflow_Instances).
- All update operations check RowVersion.

---

## Proposed Permission Strategy

| Permission | Scope | Usage |
|---|---|---|
| CUSTOMER_CHANGE_REQUEST_CREATE | GLOBAL | Gate proposal submission endpoint. Already in permission-catalog.md. Must be added to PermissionCodes.cs. |
| CUSTOMER_CREATE_FINAL | GLOBAL | Gate existing direct customer creation. No change. |
| CUSTOMER_VIEW_BASIC | GLOBAL | Gate customer search/view. No change. |
| CUSTOMER_VIEW_SENSITIVE | GLOBAL | Gate sensitive field visibility. No change. |
| CUSTOMER_MASTER_UPDATE | GLOBAL | Gate customer edit. No change. |
| WORKFLOW_VIEW | GLOBAL | Gate workflow instance detail view. No change. |
| WORKFLOW_REASSIGN_PENDING | COMPANY | Gate reassignment. No change. |

- No new permission codes added to permission-catalog.md.
- CUSTOMER_CHANGE_REQUEST_CREATE already exists in permission-catalog.md (line 26: module=CUSTOMER, action=PROPOSE_CHANGE, scope=GLOBAL, not sensitive, not delegable).
- DENY wins — backend enforced. No change to authorization evaluation.
- Backend remains authoritative for all authorization decisions.

---

## Proposed Safe Payload Strategy

- ProposalPayloadJson is stored as NVARCHAR(MAX) in Customer_Change_Requests — backend-owned.
- PayloadJson is stored on WorkflowInstance — backend-owned, SHA-256 hashed.
- BeforeDataJson is null for CREATE_CUSTOMER.
- Frontend never displays raw ProposalPayloadJson, PayloadJson, or BeforeDataJson.
- Safe metadata summary returned by backend DTO: ProposedCustomerCode, ProposedFullName, ProposedCompanyId only.
- No CCCD, Phone, DOB, PermanentAddress, TaxCode, or other sensitive fields in summary display.
- Safe-field list: CustomerCode, FullName, CompanyId. Expandable by future decision.
- workflow_snapshot_json and payload_hash remain backend-computed and backend-owned.
- No sensitive data in audit event payloads beyond what existing audit patterns allow (SEC-005).

---

## Proposed Workflow Lifecycle

### Happy path: Propose → Approve → Execute

1. Staff fills customer creation form on CustomerCreatePage.
2. Staff clicks "Submit for Approval" → POST /customers/proposals.
3. Backend creates CustomerChangeRequest (PENDING_APPROVAL) + WorkflowInstance (PENDING_APPROVAL).
4. First approval step is PENDING, assignees resolved from workflow binding configuration.
5. Approver sees proposal in My Approvals inbox (existing B3 UI).
6. Approver clicks into instance detail (existing B3 UI) — sees processCode=CREATE_CUSTOMER, businessEntityType=CustomerChangeRequest, safe metadata.
7. Approver approves step → next step becomes PENDING (if multi-step), or instance becomes PENDING_EXECUTION (if final step).
8. On PENDING_EXECUTION: execution handler fires automatically.
9. Execution handler runs final duplicate check, creates Customer via existing CustomerService, updates CustomerChangeRequest with ResultCustomerId.
10. Instance status → EXECUTED. CustomerChangeRequest status → EXECUTED.
11. Customer record now exists and is accessible via /customers/:id.

### Return flow

1. Approver returns step with reason.
2. Instance status → RETURNED. CustomerChangeRequest status remains PENDING_APPROVAL (workflow is still active).
3. Requester sees RETURNED status. Can resubmit (increments round, recreates steps with original workflow version per APR-006/APR-007).
4. On resubmit, workflow restarts from step 1 with new round.

### Withdraw flow

1. Requester withdraws instance.
2. Instance status → WITHDRAWN. CustomerChangeRequest status → WITHDRAWN.
3. No customer created. Proposal is terminal.

### Failure/Retry

1. Execution handler fails (duplicate CCCD found at execution time, or other error).
2. Instance status → FAILED. CustomerChangeRequest status → FAILED, FailureReason set.
3. Retry: future mechanism. Execution handler is idempotent (APR-009) — checks CorrelationId and RequestStatus before re-executing.
4. Manual retry endpoint deferred unless separately authorized.

### Audit

- Every state transition writes an immutable audit event (SEC-001, GOV-007).
- Proposal creation, approval actions, execution, failure all audited.
- Audit records include actor, entity, action code, correlation_id, timestamp.

---

## Proposed API Contracts

### POST /customers/proposals

Request:
```json
{
  "customerCode": "KH-2026-001",
  "fullName": "Nguyễn Văn A",
  "cccd": "012345678901",
  "phone": "0901234567",
  "gender": "MALE",
  "dob": "1990-01-15",
  "dobPrecision": "FULL",
  "permanentAddress": "123 Đường ABC, Quận 1, TP.HCM",
  "companyId": 1
}
```

Response (201):
```json
{
  "id": 1,
  "requestType": "CREATE_CUSTOMER",
  "requestStatus": "PENDING_APPROVAL",
  "workflowInstanceId": 42,
  "proposedCustomerCode": "KH-2026-001",
  "proposedFullName": "Nguyễn Văn A",
  "proposedCompanyId": 1,
  "createdByUserId": 5,
  "createdAt": "2026-08-01T10:00:00.000Z",
  "rowVersion": "AAAAAAAAB9E="
}
```

### GET /customers/proposals/{proposalId}

Response (200): Same shape as POST response, plus resultCustomerId and failureReason if applicable.

### GET /customers/proposals/my-proposals

Response (200):
```json
[
  {
    "id": 1,
    "requestType": "CREATE_CUSTOMER",
    "requestStatus": "PENDING_APPROVAL",
    "workflowInstanceId": 42,
    "proposedCustomerCode": "KH-2026-001",
    "proposedFullName": "Nguyễn Văn A",
    "createdAt": "2026-08-01T10:00:00.000Z"
  }
]
```

### Execution handler behavior (internal, no public endpoint)

Triggered automatically when WorkflowInstance transitions to PENDING_EXECUTION after final approval step.

Input: WorkflowInstance (with BusinessEntityId pointing to CustomerChangeRequest.Id).

Behavior:
1. Load CustomerChangeRequest.
2. If already EXECUTED → no-op (idempotent).
3. Deserialize ProposalPayloadJson → CreateCustomerRequest.
4. Run duplicate check.
5. Call CustomerService.CreateCustomerAsync.
6. Update CustomerChangeRequest (EXECUTED, ResultCustomerId).
7. Update WorkflowInstance (EXECUTED, AfterDataJson).

---

## Proposed File Impact

### Backend source (new)

| File | Type |
|---|---|
| src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs | Entity |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerChangeRequestConfiguration.cs | EF config |
| src/backend/PTKD.Api/Controllers/CustomerProposalController.cs | Controller |
| src/backend/PTKD.Application/Customers/DTOs/CustomerProposalDtos.cs | DTOs |
| src/backend/PTKD.Application/Customers/Validations/CustomerProposalValidator.cs | Validator |
| src/backend/PTKD.Application/Customers/Services/ICustomerProposalService.cs | Interface |
| src/backend/PTKD.Application/Customers/Services/CustomerProposalService.cs | Implementation |
| src/backend/PTKD.Application/Workflows/Services/IWorkflowExecutionHandler.cs | Interface |
| src/backend/PTKD.Application/Workflows/Handlers/CreateCustomerExecutionHandler.cs | Handler |
| src/backend/PTKD.Application/Workflows/Services/IWorkflowExecutionHandlerFactory.cs | Factory interface |
| src/backend/PTKD.Application/Workflows/Services/WorkflowExecutionHandlerFactory.cs | Factory impl |

### Backend source (modified)

| File | Change |
|---|---|
| src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs | Add CustomerChangeRequestCreate constant |
| src/backend/PTKD.Application/Workflows/Services/WorkflowRuntimeService.cs | Call execution handler on PENDING_EXECUTION |
| src/backend/PTKD.Infrastructure/Persistence/PtkdDbContext.cs | Add DbSet for CustomerChangeRequest |
| src/backend/PTKD.Api/Program.cs or DI registration | Register ICustomerProposalService, IWorkflowExecutionHandler, factory |

### Backend tests (new)

| File | Coverage |
|---|---|
| tests/backend/.../CustomerProposalServiceTests.cs | Proposal creation, duplicate check, workflow instance creation |
| tests/backend/.../CreateCustomerExecutionHandlerTests.cs | Execution happy path, idempotency, failure |
| tests/backend/.../CustomerProposalControllerTests.cs | Endpoint integration tests |
| tests/backend/.../WorkflowExecutionHandlerFactoryTests.cs | Factory resolution |

### Frontend source (new)

| File | Purpose |
|---|---|
| src/frontend/src/customers/customerProposalApi.ts | Proposal API functions |
| src/frontend/src/customers/CustomerProposalDetailPage.tsx | Proposal detail view |
| src/frontend/src/customers/CustomerMyProposalsPage.tsx | My proposals list |

### Frontend source (modified)

| File | Change |
|---|---|
| src/frontend/src/customers/CustomerCreatePage.tsx | Add proposal submission path, companyId field, permission-based button |
| src/frontend/src/customers/CustomersPage.tsx | "New Customer" button visible with CUSTOMER_CHANGE_REQUEST_CREATE |
| src/frontend/src/customers/types.ts | Add proposal types |
| src/frontend/src/customers/errorMessages.ts | Add proposal error codes |
| src/frontend/src/App.tsx | Add proposal routes |
| src/frontend/src/components/AuthenticatedShell.tsx | Add "My Proposals" menu item |

### Frontend tests (new/modified)

| File | Change |
|---|---|
| src/frontend/src/customers/customerProposalApi.test.ts | New: endpoint tests |
| src/frontend/src/customers/CustomerProposalDetailPage.test.tsx | New: detail rendering, states |
| src/frontend/src/customers/CustomerMyProposalsPage.test.tsx | New: list rendering, states |
| src/frontend/src/customers/CustomerCreatePage.test.tsx | Modified: proposal path tests |
| src/frontend/src/customers/CustomersPage.test.tsx | Modified: button visibility tests |

### Database (new)

| File | Purpose |
|---|---|
| database/migrations/V0007__create_customer_change_request.sql | Create table |
| database/migrations/U0007__drop_customer_change_request.sql | Rollback |

### Docs (modified)

| File | Change |
|---|---|
| docs/architecture/phase-1b3b4-create-customer-pilot-implementation-plan.md | This document — status update after authorization |

---

## Test Plan

### Backend Unit Tests

- CustomerProposalService.CreateProposalAsync: happy path, duplicate check fails, workflow binding missing, assignees missing, audit event written.
- CreateCustomerExecutionHandler.ExecuteAsync: happy path (customer created), idempotency (already EXECUTED), duplicate at execution (FAILED), afterDataJson written, CorrelationId checked.
- WorkflowExecutionHandlerFactory: resolves handler by ProcessCode, returns null for unknown ProcessCode.
- CustomerProposalValidator: field validation rules match CreateCustomerRequestValidator.

### Backend Integration/API Tests

- POST /customers/proposals: 201 created, 403 without permission, 422 validation errors, 422 no binding, 422 no assignees.
- GET /customers/proposals/:id: 200 found, 404 not found, 403 without permission.
- GET /customers/proposals/my-proposals: 200 with results, 200 empty.
- Full approval flow: create proposal → approve all steps → verify EXECUTED, customer exists.

### Workflow Runtime Integration Tests

- ApproveStep on final step triggers execution handler.
- PENDING_EXECUTION → EXECUTING → EXECUTED transition.
- Execution failure → FAILED status.
- No handler registered for ProcessCode → instance stays PENDING_EXECUTION (graceful, logged).

### Frontend Tests

- CustomerCreatePage: proposal button shown with CUSTOMER_CHANGE_REQUEST_CREATE, direct button shown with CUSTOMER_CREATE_FINAL, both shown with both permissions, companyId field required for proposal.
- CustomerProposalDetailPage: renders proposal metadata, status tags, workflow link, customer link after execution, failure display, no raw payload, 403/404 error states.
- CustomerMyProposalsPage: renders list, empty state, error state.
- customerProposalApi: endpoint path/method verification.
- CustomersPage: "New Customer" button visible with either permission.

### Regression Tests

- Existing customer CRUD: all 42 existing customer tests pass unchanged.
- Existing B2 workflow admin: all existing workflow admin tests pass unchanged.
- Existing B3 My Approvals: all existing workflow runtime tests pass unchanged.
- No test removed or weakened.

### Deferred Behavior Tests

- No generic My Requests UI/API (GAP-1 deferred).
- No action history/timeline UI/API (GAP-2 deferred).
- No reject action UI/API (GAP-3 deferred).
- No CUSTOMER_MASTER_CHANGE proposal support (deferred).

---

## Explicit Deferred Scope

- My Requests UI/API (backend GAP-1 — deferred to separate gap-resolution phase).
- Action history/timeline UI/API (backend GAP-2 — deferred).
- Reject action UI/API (backend GAP-3 — deferred).
- Active instance migration UI.
- Generic workflow instance creation UI.
- CUSTOMER_MASTER_CHANGE pilot (deferred to second pilot).
- Service/Payment/Merge/Card/Plot/ENTITY modules.
- Delegation implementation.
- SLA/reminder implementation.
- Condition evaluation at bind/submit time.
- Production migration/release (requires separate approval).
- New permission codes in permission-catalog.md.
- business-rules.md or acceptance-criteria.md changes.
- Broad workflow engine redesign.
- Manual execution retry endpoint (deferred unless separately authorized).

---

## Risks

1. **CUSTOMER_CHANGE_REQUEST_CREATE missing from PermissionCodes.cs** — exists in permission-catalog.md but not wired to code. Must be added. This is code wiring of an existing catalog entry, not a new permission code. See Blockers.

2. **V0007 migration required** — CustomerChangeRequest table is required for entity-to-instance linkage. Migration is allowed per DEC-1B3B4-03 but adds complexity.

3. **Execution handler failure after final approval** — if duplicate check fails at execution time, the workflow instance enters FAILED state. Manual retry mechanism is deferred. The proposal and instance stay in FAILED state until a future retry mechanism is built.

4. **Duplicate customer detection timing** — CUS-005 requires check before submit AND before execution. A customer could be created between proposal submission and final approval, causing execution failure. This is by design (fail-safe), not a defect.

5. **Direct-create and proposal path confusion** — users with both CUSTOMER_CREATE_FINAL and CUSTOMER_CHANGE_REQUEST_CREATE see both options. UX must clearly distinguish the paths. Risk of user choosing the wrong path.

6. **Payload sensitivity** — ProposalPayloadJson contains personal data (CCCD, Phone, DOB). Safe summary must exclude all sensitive fields. Backend must never return raw payload to frontend.

7. **Workflow/business state divergence** — CustomerChangeRequest status and WorkflowInstance status must stay synchronized. Transaction boundaries must ensure atomic updates.

8. **Approval return/resubmit semantics** — when an approver returns a proposal, the requester can resubmit (new round). The proposal data is unchanged — only the approval workflow restarts. Editing a returned proposal is not supported in B4.

9. **Internal pilot UX limitations** — no My Requests, no action history, no reject. Requester can only see their proposals via the new lightweight MyProposals page, not via the generic workflow system.

---

## Blockers

### BLOCKER-1: CUSTOMER_CHANGE_REQUEST_CREATE not in PermissionCodes.cs

**Status**: Must be resolved during implementation.

CUSTOMER_CHANGE_REQUEST_CREATE exists in `docs/business/permission-catalog.md` (line 26) but does NOT exist in `src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs`. The current PermissionCodes.cs has only 4 customer permissions: CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE.

**Resolution**: Add `CustomerChangeRequestCreate = "CUSTOMER_CHANGE_REQUEST_CREATE"` to PermissionCodes.cs during implementation. This is wiring an existing permission-catalog entry to code — not adding a new permission. The permission catalog already documents this code (module=CUSTOMER, action=PROPOSE_CHANGE, scope=GLOBAL).

**Requires**: Project Owner confirmation that adding to PermissionCodes.cs is within scope (DEC-1B3B4-04 says "not allowed unless separately approved" for permission catalog updates — but this is a code-level wiring, not a catalog update).

### BLOCKER-2: No IWorkflowExecutionHandler interface exists

**Status**: Must be created during implementation.

The WorkflowInstance entity has PENDING_EXECUTION → EXECUTING → EXECUTED → FAILED status lifecycle designed, but no handler interface or dispatch mechanism exists. SetExecuting(), SetExecuted(afterDataJson), SetFailed() methods exist on the entity.

**Resolution**: Create IWorkflowExecutionHandler interface and factory during B4 implementation. Modify WorkflowRuntimeService.ApproveStepAsync to call the handler on PENDING_EXECUTION transition. Register CreateCustomerExecutionHandler for ProcessCode=CREATE_CUSTOMER.

### BLOCKER-3: Execution handler auto-trigger vs manual

**Status**: Resolved by plan acceptance (DEC-1B3B4-12).

Decision: execution handler fires automatically after final approval step. Backend auto-execution matches APR-008/APR-009.

---

## Required Project Owner Implementation Authorization

The Project Owner must explicitly approve each item before implementation begins:

- [ ] Direct-create coexistence model: Option A — alternate proposal path, existing direct-create unchanged.
- [ ] Backend endpoint scope: POST /customers/proposals, GET /customers/proposals/:id, GET /customers/proposals/my-proposals.
- [ ] Execution handler scope: IWorkflowExecutionHandler interface + CreateCustomerExecutionHandler, auto-triggered on PENDING_EXECUTION.
- [ ] Linkage/database strategy: CustomerChangeRequest table (V0007 migration), BusinessEntityType=CustomerChangeRequest.
- [ ] Permission strategy: Add CUSTOMER_CHANGE_REQUEST_CREATE to PermissionCodes.cs (existing catalog entry, no catalog update).
- [ ] Frontend scope: CustomerCreatePage proposal path, CustomerProposalDetailPage, CustomerMyProposalsPage, menu/route changes.
- [ ] Test scope: backend unit + integration + frontend + regression + deferred behavior tests.
- [ ] Deferred scope: My Requests/action history/reject/CUSTOMER_MASTER_CHANGE/production all deferred.

---

## Conclusion

PHASE 1B.3-B4 CREATE_CUSTOMER PILOT IMPLEMENTATION PLAN READY FOR PROJECT OWNER IMPLEMENTATION AUTHORIZATION
