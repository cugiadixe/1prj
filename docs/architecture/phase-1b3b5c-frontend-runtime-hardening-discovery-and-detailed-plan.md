# Phase 1B.3-B5-C Frontend Runtime Hardening Discovery and Detailed Plan

## Status

ACCEPTED — SEE phase-1b3b5c-project-owner-plan-acceptance.md

## Planning Baseline

| Role | Hash |
|---|---|
| B5-B Project Owner backend acceptance commit | c42734e351404d9788b82e2049c92f6de09baf18 |
| B5-B backend implementation review commit | 8ac9018429649f7546ae831c83b04060ad41089b |
| B5-B backend implementation commit | 0394379ca343906bb8560dc0359fb853dc3b658a |
| B5-B scope authorization commit | 563503ce88f283d8483e1fc1852acf469427a31b |
| B5 plan acceptance commit | f13afa48ecfaa8fa190137164b1a49ba70dee06e |

## Purpose

Plan the frontend runtime hardening required to expose accepted B5-B backend capabilities safely and consistently in the PTKD ERP web UI. B5-B added four backend APIs (My Requests, Action History, Reject, Execution Retry) and two permissions (WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION). This plan defines the frontend changes needed to surface those capabilities.

---

## Confirmed Backend Capabilities

### My Requests API

- Endpoint: `GET /api/v2/workflows/my-requests`
- Auth: `[Authorize]` (any authenticated user, returns own instances only)
- Response: `WorkflowInstanceDto[]`
- Fields: id, workflowVersionId, processCode, companyId, requesterId, businessEntityType, businessEntityId, instanceStatus, roundNo, rowVersion, createdAt, updatedAt, steps[]

### Action History API

- Endpoint: `GET /api/v2/workflows/instances/{instanceId}/actions`
- Auth: `[Authorize]` (service validates requester or assignee access)
- Response: `WorkflowActionDto[]`
- Fields: id, workflowInstanceStepId, workflowInstanceId, actionType, actedBy, onBehalfOf, reason, comment, createdAt

### Reject API

- Endpoint: `POST /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject`
- Auth: `[Authorize]` (service validates assignee + WORKFLOW_REJECT permission)
- Request: `ApprovalActionRequest` — reason (required), comment (optional), targetVersion (required)
- Response: `WorkflowInstanceDto`
- Semantics: Terminal — instance moves to REJECTED status. Cannot be resubmitted.

### Execution Retry API

- Endpoint: `POST /api/v2/workflows/instances/{instanceId}/retry-execution`
- Auth: `[RequirePermission(WORKFLOW_RETRY_EXECUTION, Global)]`
- Request: No body (instanceId from route)
- Response: `WorkflowInstanceDto`
- Semantics: Only for FAILED instances. Re-runs execution handler. Idempotent.

### Permissions Added in B5-B

- `WORKFLOW_REJECT` — gates reject action (assignee must also have this permission)
- `WORKFLOW_RETRY_EXECUTION` — gates retry action (Global scope, admin-only)

### Deferred

- Safe user lookup/reassign autocomplete UI expansion remains deferred.

---

## Current Frontend State

### Workflow Admin Config

- WorkflowDefinitionsPage, WorkflowVersionDetailPage, WorkflowBindingsPage — full CRUD for definitions, versions, steps, bindings.
- Gated by WORKFLOW_VIEW / WORKFLOW_CONFIG_MANAGE / WORKFLOW_PUBLISH / WORKFLOW_BIND_PROCESS in navigation.

### My Approvals

- MyApprovalsPage — fetches `getMyApprovals()`, renders table (process, entity, step, status, assigned date).
- No permission gate in nav (visible to all authenticated users).
- Links to WorkflowInstanceDetailPage.

### Workflow Instance Detail

- WorkflowInstanceDetailPage — fetches `getInstance(id)`, shows metadata + steps table.
- Actions: Approve, Return, Reassign, Resubmit, Withdraw — each gated by role/status/permission checks.
- Concurrency via rowVersion/targetVersion.
- No action history display.
- No reject action.
- No retry action.
- No REJECTED or FAILED status handling.

### Customer Proposal Pages

- CustomerProposalCreatePage — form with 18 fields, duplicate check.
- CustomerProposalDetailPage — read-only view with safe metadata summary.
- CustomerMyProposalsPage — table of user's proposals.
- Gated by CUSTOMER_CHANGE_REQUEST_CREATE.

### API Client Structure

- workflowApi.ts: 6 functions (getMyApprovals, getInstance, approveStep, returnStep, resubmitInstance, withdrawInstance).
- customerProposalApi.ts: 3 functions (create, getById, getMyProposals).
- All use shared axios instance with Bearer token injection and 401 retry.

### Permission Infrastructure

- AuthProvider with usePermissions() hook.
- hasPermission(code, scope?, companyId?) check.
- Permissions fetched via GET /api/v2/security/me/permissions after login.
- Frontend gating is UX-only; backend remains authoritative.

### Current Tests

- 40 test files, 345 tests, all passing.
- Vitest + React Testing Library.
- Tests cover routes, API clients, permission visibility, component rendering.

---

## Frontend Gap Analysis

### A. My Requests UI

**Gap**: No route, no navigation item, no page component, no API function.

**Proposed**:
- Route: `/workflow/my-requests`
- Navigation: Add "My Requests" menu item in Workflow section, no permission gate (visible to all authenticated users, same as My Approvals).
- Page: `WorkflowMyRequestsPage.tsx` — table listing user's own workflow instances.
- List fields: ID, processCode, businessEntityType, instanceStatus, roundNo, createdAt, updatedAt.
- Filters: Status filter (all, PENDING_APPROVAL, RETURNED, REJECTED, PENDING_EXECUTION, EXECUTING, EXECUTED, FAILED, WITHDRAWN).
- Empty state: "You have no workflow requests."
- Loading state: Skeleton/spinner.
- Error state: Sanitized error message.
- Row click navigates to `/workflow/instances/:instanceId`.
- Safe metadata only — no payload display.

### B. Action History / Timeline UI

**Gap**: No types, no API function, no UI component.

**Proposed**:
- Component: `WorkflowActionHistoryPanel.tsx` — rendered on WorkflowInstanceDetailPage below the steps table.
- Display fields: actionType, actedBy (user ID — name resolution deferred), reason, comment, createdAt.
- Presentation: Chronological list (oldest first) or reverse-chronological (newest first — PO to decide).
- No raw payload display.
- No sensitive data display.
- Empty state: "No actions recorded."
- Loading state: Spinner.
- Error state: Sanitized error message.
- API call: `getInstanceActions(instanceId)` in workflowApi.ts.

### C. Reject UX

**Gap**: No reject button, no confirmation dialog, no API function.

**Proposed**:
- Button location: WorkflowInstanceDetailPage, per-step actions area.
- Visibility: Step is PENDING AND user is assignee AND user has WORKFLOW_REJECT permission AND user is not the requester.
- Confirmation: Modal dialog with required reason field (min 1 char, max 500 chars) and optional comment field.
- Dialog title: "Reject Step" (PO to confirm wording).
- Warning text: "This action is permanent. The request will be rejected and cannot be resubmitted." (PO to confirm).
- API call: `rejectStep(instanceId, stepId, { reason, comment, targetVersion })`.
- Error handling: Concurrency error → refresh instance. Permission denied → sanitized message. Not assignee → sanitized message.
- Post-reject: Refresh instance detail to show REJECTED status.
- User messaging: Success toast "Step rejected successfully."

### D. Execution Retry UX

**Gap**: No retry button, no confirmation dialog, no API function.

**Proposed**:
- Button location: WorkflowInstanceDetailPage, instance-level action area (not per-step).
- Visibility: Instance status is FAILED AND user has WORKFLOW_RETRY_EXECUTION permission.
- Confirmation: Modal dialog with warning text.
- Dialog title: "Retry Execution" (PO to confirm).
- Warning text: "This will retry the failed execution. The system will attempt to complete the approved action." (PO to confirm).
- API call: `retryExecution(instanceId)` — no request body.
- Loading state: Button disabled with spinner during retry.
- Sanitized failure details: If retry fails again, show sanitized error (no stack traces, no SQL details).
- Post-retry: Refresh instance detail to show updated status (EXECUTED or FAILED).
- User messaging: Success toast "Execution retried successfully." or error toast with sanitized message.

### E. Permission and Navigation

**Frontend gating proposed**:
- WORKFLOW_REJECT: Used to show/hide reject button on instance detail. Fetched via existing usePermissions() infrastructure.
- WORKFLOW_RETRY_EXECUTION: Used to show/hide retry button on instance detail.
- My Requests nav item: No permission gate (same pattern as My Approvals).
- Action history: No separate permission gate (visible if user can view the instance).
- Backend remains authoritative — frontend gates are UX convenience only. Backend validates permissions on every API call.

### F. API Client and Types

**workflowApi.ts additions needed**:
- `getMyRequests()` → GET `/workflows/my-requests` → `WorkflowInstance[]`
- `getInstanceActions(instanceId)` → GET `/workflows/instances/{instanceId}/actions` → `WorkflowActionDto[]`
- `rejectStep(instanceId, stepId, request)` → POST `/workflows/instances/{instanceId}/steps/{stepId}/reject` → `WorkflowInstance`
- `retryExecution(instanceId)` → POST `/workflows/instances/{instanceId}/retry-execution` → `WorkflowInstance`

**workflowTypes.ts additions needed**:
- `WorkflowActionDto` type: id, workflowInstanceStepId, workflowInstanceId, actionType, actedBy, onBehalfOf, reason, comment, createdAt.
- Update `WorkflowInstance` instanceStatus union to include `'REJECTED'` and `'FAILED'` if not already present.
- Error handling patterns: Reuse existing axios error handling (concurrency, permission denied, not found).

### G. Tests

**Proposed test coverage**:
- WorkflowMyRequestsPage.test.tsx — renders list, filters by status, handles empty/loading/error, navigates to detail.
- WorkflowActionHistoryPanel.test.tsx — renders action list, handles empty, no sensitive data rendered.
- WorkflowInstanceDetailPage.test.tsx — reject button visibility (permission + assignee + not requester), retry button visibility (permission + FAILED status), reject dialog validation, retry confirmation.
- workflowApi.test.ts — getMyRequests, getInstanceActions, rejectStep, retryExecution endpoint calls.
- AuthenticatedShell.test.tsx — My Requests nav item visibility.
- Regression tests: Existing My Approvals, instance detail, proposal pages must continue passing.

---

## Proposed Frontend Implementation Scope

### Allowed for future B5-C implementation, if approved

- Frontend route/page for My Requests.
- Action history/timeline panel on instance detail.
- Reject modal/action on instance detail.
- Retry action for failed execution on instance detail.
- API client/type updates in workflowApi.ts and workflowTypes.ts.
- Navigation update in AuthenticatedShell.tsx.
- Route update in App.tsx.
- Tests for all new components and interactions.

### Not proposed

- Backend changes.
- Migration/rollback changes.
- Permission catalog changes.
- Business rule changes.
- User lookup/reassign autocomplete expansion.
- Production release.
- Service/Payment/CUSTOMER_MASTER_CHANGE/Merge/Card/Plot/ENTITY modules.
- Broad UI redesign.
- Export/download functionality.

---

## Proposed Files to Change in Future B5-C Implementation

### New files

- src/frontend/src/workflow/WorkflowMyRequestsPage.tsx
- src/frontend/src/workflow/WorkflowMyRequestsPage.test.tsx
- src/frontend/src/workflow/WorkflowActionHistoryPanel.tsx
- src/frontend/src/workflow/WorkflowActionHistoryPanel.test.tsx
- src/frontend/src/workflow/WorkflowRejectDialog.tsx
- src/frontend/src/workflow/WorkflowRejectDialog.test.tsx
- src/frontend/src/workflow/WorkflowRetryExecutionButton.tsx
- src/frontend/src/workflow/WorkflowRetryExecutionButton.test.tsx

### Modified files

- src/frontend/src/App.tsx — add My Requests route
- src/frontend/src/components/AuthenticatedShell.tsx — add My Requests nav item
- src/frontend/src/workflow/workflowApi.ts — add 4 API functions
- src/frontend/src/workflow/workflowTypes.ts — add WorkflowActionDto type, update status union
- src/frontend/src/workflow/WorkflowInstanceDetailPage.tsx — add reject button, retry button, action history panel
- src/frontend/src/workflow/WorkflowInstanceDetailPage.test.tsx — add reject/retry/history tests
- src/frontend/src/components/AuthenticatedShell.test.tsx — add My Requests nav visibility test

---

## API Contract Mapping

| Frontend Feature | Backend Endpoint | Method | Request | Response |
|---|---|---|---|---|
| My Requests UI | /api/v2/workflows/my-requests | GET | — | WorkflowInstanceDto[] |
| Action History UI | /api/v2/workflows/instances/{instanceId}/actions | GET | — | WorkflowActionDto[] |
| Reject UX | /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject | POST | ApprovalActionRequest | WorkflowInstanceDto |
| Retry UX | /api/v2/workflows/instances/{instanceId}/retry-execution | POST | — | WorkflowInstanceDto |

---

## Permission Mapping

| Permission Code | Frontend Use | Scope | Notes |
|---|---|---|---|
| WORKFLOW_REJECT | Show/hide reject button on instance detail | Global | Backend validates assignee + permission |
| WORKFLOW_RETRY_EXECUTION | Show/hide retry button on instance detail | Global | Admin-only |
| WORKFLOW_VIEW | Instance detail access | Global | Existing |
| WORKFLOW_REASSIGN_PENDING | Reassign button | Company | Existing |
| CUSTOMER_CHANGE_REQUEST_CREATE | My Proposals nav | Global | Existing |

Backend authorization remains authoritative. Frontend gating is UX convenience only.

---

## Security and Data Exposure Rules

- No raw PayloadJson display anywhere in the UI.
- No BeforeDataJson display.
- No AfterDataJson display.
- No sensitive customer fields (CCCD, phone, address, DOB) in action history, My Requests list, or any summary display.
- No stack traces shown to users.
- No SQL or internal exception details shown to users.
- Sanitized user-facing error messages only — reuse existing error handling patterns.
- Frontend must not be source of truth for authorization — all permission checks are UX gates only.
- No localStorage/sessionStorage persistence for permissions, approval state, or sensitive data.
- Action history displays actionType, reason, comment, timestamp — no payload content.
- Actor display as user ID only (name resolution deferred to user lookup expansion).

---

## Test Strategy

### Frontend test commands

```
cd src/frontend
npx oxlint
npx tsc -b
npx vitest run
```

### Backend regression command

```
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj -p:ParallelizeTestCollections=false
```

Backend regression should be run if B5-C implementation reveals any API contract assumption issues.

### Expected test outcomes

- All existing 345 tests continue passing.
- New B5-C tests add coverage for My Requests, action history, reject, retry, permission visibility.
- oxlint: clean or only pre-existing warnings unrelated to B5-C.
- tsc: 0 errors.

---

## Recommended B5-C Implementation Phases

### Option A: Single bounded implementation (Recommended)

Implement all B5-C features in one commit:
- API client/types updates.
- My Requests page + route + nav.
- Action history panel on instance detail.
- Reject dialog on instance detail.
- Retry button on instance detail.
- All tests.

Rationale: The four features are tightly coupled (all on the same instance detail page, share the same API client, same permission infrastructure). Splitting into C1-C4 would create intermediate states where instance detail has partial functionality. The total scope is manageable in one commit (~8 new files, ~7 modified files).

### Option B: Split into sub-phases

If PO prefers incremental delivery:
- B5-C1: API client/types and My Requests UI.
- B5-C2: Action History panel.
- B5-C3: Reject and Retry UX.
- B5-C4: Frontend test hardening and closure.

---

## Required Project Owner Decisions

1. Approve route name: `/workflow/my-requests` — or alternative.
2. Approve menu label: "My Requests" — or alternative.
3. Approve whether My Requests appears in main workflow navigation (recommended: yes, alongside My Approvals).
4. Approve action history display order: newest-first or oldest-first.
5. Approve action history display fields: actionType, actedBy (user ID), reason, comment, timestamp.
6. Approve reject confirmation wording and required reason.
7. Approve retry confirmation wording and who sees retry (WORKFLOW_RETRY_EXECUTION holders only).
8. Approve whether B5-C is implemented as one commit (recommended) or split into C1-C4.

---

## Explicit Non-Scope

- Backend implementation.
- Migration/rollback changes.
- Permission catalog changes.
- Business rule changes.
- Production release.
- Service/Payment module.
- CUSTOMER_MASTER_CHANGE module.
- Customer merge module.
- Card flow module.
- Plot flow module.
- ENTITY expansion.
- Export/download functionality.
- Broad frontend redesign.
- User lookup/reassign autocomplete expansion.
- Safe user name resolution (display user names instead of IDs).

---

## Stop Conditions

- Stop if backend API contract is unclear or differs from documented B5-B implementation.
- Stop if frontend requires backend changes beyond what B5-B implemented.
- Stop if permission mapping is unclear or requires new permissions not in B5-B.
- Stop if raw payload or sensitive data would need to be exposed.
- Stop if reject/retry semantics are unclear or differ from documented behavior.
- Stop if existing tests cannot run cleanly.
- Stop if implementation scope expands beyond B5-C into deferred modules.
- Stop if frontend needs to become source of truth for any authorization decision.

---

## Recommendation

Proceed to Project Owner plan acceptance for B5-C. The backend contract is clear, the four features are well-bounded, the existing frontend patterns (API client, permission hooks, instance detail page) provide a solid foundation, and the implementation can be completed in a single commit with full test coverage.

## Conclusion

PHASE 1B.3-B5-C FRONTEND RUNTIME HARDENING PLAN PROPOSED — AWAITING PROJECT OWNER APPROVAL
