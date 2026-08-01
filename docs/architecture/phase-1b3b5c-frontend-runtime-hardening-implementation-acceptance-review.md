# Phase 1B.3-B5-C Frontend Runtime Hardening Implementation Acceptance Review

## Status

PASSED — READY FOR PROJECT OWNER FRONTEND IMPLEMENTATION ACCEPTANCE

## Reviewed Commit

- Implementation commit:
  c11a655cf7f909e1a60f3d3eecbd8db70e8023be
- Parent plan acceptance commit:
  563a009d86a4f5916c105672f07910b62709d012

## Scope Review

Committed files (20 total — 12 modified, 8 added):

| Status | File | In B5-C Scope |
|---|---|---|
| M | src/frontend/src/App.tsx | Yes — route addition |
| M | src/frontend/src/components/AuthenticatedShell.tsx | Yes — nav addition |
| M | src/frontend/src/components/AuthenticatedShell.test.tsx | Yes — nav test |
| M | src/frontend/src/customers/CustomerMyProposalsPage.test.tsx | Yes — authorized test hygiene |
| M | src/frontend/src/customers/CustomerProposalCreatePage.test.tsx | Yes — authorized test hygiene |
| M | src/frontend/src/customers/CustomerProposalDetailPage.test.tsx | Yes — authorized test hygiene |
| M | src/frontend/src/workflow/errorMessages.ts | Yes — new error codes |
| A | src/frontend/src/workflow/WorkflowMyRequestsPage.tsx | Yes — My Requests UI |
| A | src/frontend/src/workflow/WorkflowMyRequestsPage.test.tsx | Yes — My Requests tests |
| A | src/frontend/src/workflow/WorkflowActionHistoryPanel.tsx | Yes — Action History UI |
| A | src/frontend/src/workflow/WorkflowActionHistoryPanel.test.tsx | Yes — Action History tests |
| A | src/frontend/src/workflow/WorkflowRejectDialog.tsx | Yes — Reject UX |
| A | src/frontend/src/workflow/WorkflowRejectDialog.test.tsx | Yes — Reject tests |
| A | src/frontend/src/workflow/WorkflowRetryExecutionButton.tsx | Yes — Retry UX |
| A | src/frontend/src/workflow/WorkflowRetryExecutionButton.test.tsx | Yes — Retry tests |
| M | src/frontend/src/workflow/WorkflowInstanceDetailPage.tsx | Yes — reject/retry/history integration |
| M | src/frontend/src/workflow/WorkflowInstanceDetailPage.test.tsx | Yes — updated tests |
| M | src/frontend/src/workflow/types.ts | Yes — WorkflowActionDto |
| M | src/frontend/src/workflow/workflowRuntimeApi.ts | Yes — 4 API functions |
| M | src/frontend/src/workflow/workflowRuntimeApi.test.ts | Yes — API tests |

All 20 files are within authorized B5-C frontend scope. No backend, migration, rollback, business doc, or PermissionCodes.cs files changed.

## Behavior Review

### A. My Requests UI

- Route `/workflow/my-requests` implemented in App.tsx.
- Navigation entry "My Requests" added in AuthenticatedShell.tsx, no permission gate (matches My Approvals pattern).
- Uses `GET /api/v2/workflows/my-requests` via `getMyRequests()` in workflowRuntimeApi.ts.
- Displays safe metadata only: ID, processCode, businessEntityType, instanceStatus, roundNo, createdAt, updatedAt.
- Loading state: Spin with `data-testid="my-requests-loading"`.
- Empty state: Alert "You have no workflow requests." with `data-testid="my-requests-empty"`.
- Error state: Sanitized error via `getErrorMessage()` with `data-testid="my-requests-error"`.
- Permission denied state: Separate alert with `data-testid="permission-denied"`.
- Row click navigates to `/workflow/instances/${record.id}`.
- Does not display raw PayloadJson.
- Does not display BeforeDataJson.
- Does not display sensitive customer fields.

### B. Action History / Timeline UI

- Component `WorkflowActionHistoryPanel.tsx` implemented.
- Integrated into WorkflowInstanceDetailPage below steps table.
- Uses `GET /api/v2/workflows/instances/{instanceId}/actions` via `getInstanceActions()`.
- Displays safe action fields only: actionType (colored Tag), actedBy (as "User {id}"), reason, comment, createdAt.
- Loading state: Spin with `data-testid="action-history-loading"`.
- Empty state: Alert "No actions recorded." with `data-testid="action-history-empty"`.
- Error state: Sanitized error via `getErrorMessage()` with `data-testid="action-history-error"`.
- Does not display raw payload, BeforeDataJson, stack traces, SQL/internal details, or sensitive customer fields.

### C. Reject UX

- Reject dialog `WorkflowRejectDialog.tsx` implemented with Modal + Form.
- Uses `POST /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject` via `rejectStep()`.
- Reason required (Form validation rule), comment optional. MaxLength 500 on reason.
- Warning presented: "This action is permanent. The request will be rejected and cannot be resubmitted."
- Loading state: `confirmLoading` on Modal.
- Sanitized error handling via `handleError` → `getErrorMessage()`.
- State refresh after success: invalidates `workflow-instance`, `workflow-my-approvals`, and `workflow-instance-actions` queries.
- Frontend visibility gated by: `canReject` (WORKFLOW_REJECT permission) AND `isAssignee` AND `!isRequester` AND step status PENDING.
- Backend remains authoritative — frontend gate is UX only.

### D. Execution Retry UX

- Retry button `WorkflowRetryExecutionButton.tsx` implemented.
- Uses `POST /api/v2/workflows/instances/{instanceId}/retry-execution` via `retryExecution()`.
- Confirmation via `Modal.confirm`: "This will retry the failed execution. The system will attempt to complete the approved action."
- Loading state: `loading` prop on Button.
- Sanitized error handling via `handleError` → `getErrorMessage()`.
- State refresh after success: invalidates `workflow-instance`, `workflow-my-approvals`, and `workflow-instance-actions` queries.
- Frontend visibility gated by: `canRetry` = WORKFLOW_RETRY_EXECUTION permission AND instance status FAILED.
- Backend remains authoritative.

### E. API Client and Type Updates

- workflowRuntimeApi.ts adds 4 functions: `getMyRequests`, `getInstanceActions`, `rejectStep`, `retryExecution`.
- All use correct endpoint paths matching B5-B backend contract.
- types.ts adds `WorkflowActionDto` with safe fields: id, workflowInstanceStepId, workflowInstanceId, actionType, actedBy, onBehalfOf, reason, comment, createdAt.
- No raw payload fields in UI-facing types.
- errorMessages.ts adds 2 error codes: `WF_INSTANCE_NOT_FAILED`, `WF_ALREADY_REJECTED`.
- Existing runtime API tests updated: 4 positive tests added, 3 obsolete negative tests removed.

### F. Route and Navigation Updates

- App.tsx: Route `workflow/my-requests` added with `WorkflowMyRequestsPage` element.
- AuthenticatedShell.tsx: "My Requests" nav item added after "My Approvals", no permission gate, with `data-testid="nav-my-requests"`.

### G. Frontend Permission Gating

- WORKFLOW_REJECT: checked via `hasPermission('WORKFLOW_REJECT', 'GLOBAL')` to compute `canReject`. Combined with `isAssignee && !isRequester` for reject button visibility.
- WORKFLOW_RETRY_EXECUTION: checked via `hasPermission('WORKFLOW_RETRY_EXECUTION', 'GLOBAL')` to compute `canRetry`. Combined with `instanceStatus === 'FAILED'`.
- Backend authorization remains authoritative for all actions.

### H. Safe Payload/Data Exposure

- INSTANCE_STATUS_COLORS maps include REJECTED, FAILED, EXECUTING, EXECUTED.
- STEP_STATUS_COLORS includes REJECTED.
- Metadata display limited to safe fields (processCode, businessEntityType, businessEntityId, companyId, requesterId, roundNo, workflowVersionId, createdAt).
- No PayloadJson, BeforeDataJson, AfterDataJson, or sensitive customer fields rendered anywhere.

## Test Hygiene Review

### CustomerMyProposalsPage.test.tsx

- Removed `import React from 'react';` (unused, JSX transform handles it).
- No other changes. No customer behavior changed.

### CustomerProposalCreatePage.test.tsx

- Removed `import React from 'react';` (unused).
- Removed `import { checkDuplicates } from './customersApi';` (unused — only mock declaration remains, which is correct).
- No other changes. No customer behavior changed.

### CustomerProposalDetailPage.test.tsx

- Removed `import React from 'react';` (unused).
- No other changes. No customer behavior changed.

All three changes are strictly unused-import removal. No customer production source files changed. No customer behavior changed.

## Security and Data Exposure Review

- Raw PayloadJson: NOT displayed anywhere in B5-C implementation.
- BeforeDataJson: NOT displayed anywhere.
- Sensitive customer fields: NOT displayed (no CCCD, phone, address, DOB in any new component).
- Stack traces: NOT displayed — all errors go through `getErrorMessage()` which returns sanitized messages.
- SQL/internal exception details: NOT displayed — error handler maps known codes to user-facing messages, falls back to generic message.
- Frontend gating is usability only — every button visibility check is UX convenience.
- Backend authorization remains authoritative — all API calls go through backend permission validation.

## Test Evidence

### oxlint

```
cd src/frontend && npx oxlint
```

Exit 0. 3 warnings (all pre-existing, non-B5-C):
- src/auth/CompanyProvider.tsx:100:17 — react(only-export-components)
- src/auth/AuthProvider.tsx:36:17 — react(only-export-components)
- src/auth/AuthProvider.tsx:42:17 — react(only-export-components)

No errors. No B5-C warnings.

### tsc

```
cd src/frontend && npx tsc -b
```

Exit 0. No output. 0 errors.

### vitest

```
cd src/frontend && npx vitest run
```

Exit 0. 44 test files, 371 tests passed.

### git diff --check

```
git diff --check
```

Clean. No whitespace violations.

## Risks / Follow-Ups

1. **Pre-existing auth oxlint warnings**: 3 `react(only-export-components)` warnings in auth providers. Non-blocking, pre-existing, unrelated to B5-C.
2. **B5-D operational validation**: Still required after PO acceptance. B5-D covers operational validation and closure planning for the B5 hardening sequence.
3. **Safe user lookup/reassign**: Remains deferred. Action history displays user IDs ("User 42") rather than names. Name resolution deferred to future user lookup expansion.
4. **Status filter on My Requests**: Plan mentioned optional status filter; implementation renders all requests without client-side filtering. Non-blocking — server returns only user's own requests.

## Review Decision

PASSED — B5-C FRONTEND IMPLEMENTATION MAY PROCEED TO PROJECT OWNER ACCEPTANCE

All 20 committed files are within authorized B5-C scope. All four features (My Requests, Action History, Reject, Retry) implemented correctly with proper permission gating, safe data exposure, sanitized errors, and state refresh. Test evidence confirms 371/371 tests passing, 0 tsc errors, 0 oxlint errors. No backend, migration, business doc, or permission catalog changes. Customer test hygiene limited to unused-import removal only.
