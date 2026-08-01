# Phase 1B.3-B3 Workflow Runtime / My Approvals UI Implementation Acceptance Review

## Status

PHASE 1B.3-B3 IMPLEMENTATION ACCEPTED — SEE phase-1b3b3-project-owner-implementation-acceptance.md

## Commits

| Role | Hash |
|---|---|
| Implementation commit | 49182a43886b2647133d027b1a6eb4420470f0cc |
| Implementation parent / B3 plan acceptance commit | 521f53daf0c6feefc09f7cd3bdb90dbd3dafecf0 |
| B3 plan commit | b3d1ff5740b8909e1ce6a7f198bac6a03483b2ee |
| B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |

---

## Committed Files

From `git diff-tree --no-commit-id --name-status -r 49182a43886b2647133d027b1a6eb4420470f0cc`:

```
M	src/frontend/src/App.tsx
M	src/frontend/src/components/AuthenticatedShell.test.tsx
M	src/frontend/src/components/AuthenticatedShell.tsx
A	src/frontend/src/workflow/WorkflowInstanceDetailPage.test.tsx
A	src/frontend/src/workflow/WorkflowInstanceDetailPage.tsx
A	src/frontend/src/workflow/WorkflowMyApprovalsPage.test.tsx
A	src/frontend/src/workflow/WorkflowMyApprovalsPage.tsx
M	src/frontend/src/workflow/errorMessages.ts
M	src/frontend/src/workflow/types.ts
A	src/frontend/src/workflow/workflowRuntimeApi.test.ts
A	src/frontend/src/workflow/workflowRuntimeApi.ts
```

11 frontend files committed: 5 modified, 6 new. No backend, database, migration, rollback, permission catalog, business rule, acceptance criteria, or docs files.

---

## Accepted Implemented Scope

- Workflow Runtime / My Approvals UI implemented.
- My Approvals menu/navigation implemented — visible to all authenticated users without permission gate (self-scoped endpoint).
- My Approvals inbox/list implemented with loading, empty, error, and 403 states.
- Workflow instance detail implemented with metadata, status badges, and steps table.
- Approve action UI implemented with optional reason/comment form and confirmation modal.
- Return action UI implemented with required reason, optional comment, and confirmation modal.
- Resubmit action UI implemented for requester on RETURNED instances with confirmation dialog.
- Withdraw action UI implemented for requester on PENDING_APPROVAL or RETURNED instances with confirmation dialog.
- Reassign action UI implemented where supported by existing backend endpoint, gated by WORKFLOW_REASSIGN_PENDING.
- Runtime status badges implemented (instance: PENDING_APPROVAL, RETURNED, WITHDRAWN, PENDING_EXECUTION, COMPLETED, CANCELLED; step: PENDING, WAITING, APPROVED, RETURNED, CANCELLED).
- Safe payload/metadata display implemented — UI shows only safe metadata fields.
- Version/snapshot freeze notice implemented with data-testid="version-snapshot-notice".
- Sanitized error handling implemented with getErrorMessage(), isConcurrencyError(), and isPermissionDenied().
- Stale task/concurrency refresh UX implemented with concurrency error detection and refresh button.
- Loading/empty/error states implemented on all pages.

---

## Accepted Routes

- `/workflow/my-approvals` — WorkflowMyApprovalsPage.
- `/workflow/instances/:instanceId` — WorkflowInstanceDetailPage.

---

## Accepted API Endpoint Usage

| Function | HTTP | Path | B1 Endpoint |
|---|---|---|---|
| getMyApprovals | GET | /workflows/my-approvals | Yes |
| getInstance | GET | /workflows/instances/:id | Yes |
| approveStep | POST | /workflows/instances/:id/steps/:stepId/approve | Yes |
| returnStep | POST | /workflows/instances/:id/steps/:stepId/return | Yes |
| resubmitInstance | POST | /workflows/instances/:id/resubmit | Yes |
| withdrawInstance | POST | /workflows/instances/:id/withdraw | Yes |
| reassignStep | POST | /workflows/instances/:id/steps/:stepId/reassign | Yes |

Existing B1 runtime endpoints used only. 7 of 8 B1 endpoints mapped (createInstance excluded — no generic creation UI in B3 scope).

---

## Endpoint Limitation Confirmation

- No my-requests endpoint call. No My Requests UI, route, page, or menu item.
- No action history endpoint call. No action history/timeline UI, route, or page.
- No reject endpoint call. No reject action UI.
- No generic/business workflow instance creation UI.
- No fake client-only mutation behavior.
- No unsupported endpoint calls.

---

## Permission and Authorization Confirmation

- Backend remains authoritative for all mutation actions.
- Frontend does not grant eligibility by permission alone.
- Action eligibility is derived from backend-returned assignment/requester state:
  - Approve button: visible when user is assignee AND not requester AND step is PENDING.
  - Return button: visible when user is assignee AND step is PENDING.
  - Resubmit button: visible when user is requester AND instance is RETURNED.
  - Withdraw button: visible when user is requester AND instance is PENDING_APPROVAL or RETURNED.
- WORKFLOW_REASSIGN_PENDING gates reassignment UI — `hasPermission('WORKFLOW_REASSIGN_PENDING', 'COMPANY')`.
- Frontend gates are UX/navigation only — backend enforces all authorization decisions.
- DENY wins remains backend-enforced.

---

## Safe Payload and Safety Confirmation

- No raw PayloadJson display anywhere in frontend types, pages, or tests.
- No raw BeforeDataJson display anywhere in frontend types, pages, or tests.
- No raw sensitive data logging.
- No localStorage/sessionStorage/cookie persistence for permissions, runtime eligibility, or approval state.
- No backend stack traces or internal SQL details displayed — sanitized error messages via getErrorMessage().
- WorkflowInstance type does not include PayloadJson or BeforeDataJson fields — safe by design.

---

## UX Confirmation

- Version/snapshot freeze notice implemented — Alert with data-testid="version-snapshot-notice" stating frozen snapshot behavior.
- UI does not imply active instances change route after configuration changes.
- Active instance migration UI not implemented.
- Stale task/concurrency refresh UX implemented — concurrency error detected via isConcurrencyError(), refresh button offered via data-testid="refresh-btn".
- Sanitized 403/404/error handling implemented — permission-denied, instance-error, and my-approvals-error test IDs.
- No silent overwrite behavior introduced.

---

## Test Evidence

### Frontend Lint

Command: `npx oxlint`
Result: Clean. Only pre-existing warnings in AuthProvider.tsx and CompanyProvider.tsx (react/only-export-components) — unrelated to B3.

### Frontend Typecheck

Command: `npx tsc -b`
Result: 0 errors.

### Frontend Tests

Command: `npx vitest run`
Result: 36 test files, 332 tests passed, 0 failed.

### B3-Specific Test Evidence

| Test File | Tests | Status |
|---|---|---|
| workflowRuntimeApi.test.ts | 11 | Passed |
| WorkflowMyApprovalsPage.test.tsx | 5 | Passed |
| WorkflowInstanceDetailPage.test.tsx | 20 | Passed |
| AuthenticatedShell.test.tsx (My Approvals test) | 1 | Passed |
| **Total B3-specific tests** | **37** | **All passed** |

B3 tests cover:
- Runtime API endpoint path/method verification (7 tests).
- Deferred endpoint non-existence (createInstance, getMyRequests, getActionHistory, rejectStep — 4 tests).
- Inbox page rendering, empty state, error state, 403, no my-requests UI (5 tests).
- Instance detail rendering, status tags, version snapshot notice, metadata (5 tests).
- Approve button visibility by assignee/requester/non-assignee state (3 tests).
- Return button visibility by assignee state (1 test).
- Resubmit button visibility by requester/status state (2 tests).
- Withdraw button visibility by requester/status state (2 tests).
- Reassign button gated by WORKFLOW_REASSIGN_PENDING (2 tests).
- 403 permission denied and error states (2 tests).
- Deferred behavior: no action history, no reject button, no raw payload (3 tests).
- My Approvals menu visible to all authenticated users (1 test).

---

## Deferred Scope Confirmation

- No backend source changed in implementation commit.
- No backend tests changed in implementation commit.
- No database/migration/rollback changed in implementation commit.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No business-rules.md change.
- No acceptance-criteria.md change.
- No docs changed in implementation commit.
- No My Requests UI — requires future backend GAP-1 resolution.
- No action history/timeline UI — requires future backend GAP-2 resolution.
- No reject action UI — requires future backend GAP-3 resolution.
- No generic/business workflow instance creation UI.
- No active instance migration UI.
- No pilot integration (Customer, Service, Payment, Merge).
- No Service/Payment/Merge/ENTITY/Export implementation.
- No production migration/release.

---

## Risks and Follow-Up

- My Requests requires future backend gap-resolution phase (GAP-1: no GET /workflows/my-requests endpoint).
- Action history/timeline requires future backend gap-resolution phase (GAP-2: no GET /workflows/instances/:id/actions endpoint).
- Reject action requires future backend gap-resolution phase (GAP-3: no reject action endpoint).
- Future pilot integration remains undecided and deferred to B4 or later.
- Backend remains authoritative — frontend cannot bypass service-layer authorization checks.
- Future workflow runtime UI changes must continue avoiding raw sensitive payload exposure.
- Reassignment UI is functional but requires manual User ID entry — user picker may be a future UX improvement.

---

## Conclusion

PHASE 1B.3-B3 WORKFLOW RUNTIME / MY APPROVALS UI IMPLEMENTATION ACCEPTANCE REVIEW PASSED
