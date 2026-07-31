# Phase 1B.3-B3 Workflow Runtime / My Approvals UI Detailed Plan

## Status

PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

## Baseline

009b3d276b2255c88e8b4a165de5ecfe09927186

## Authorization and Context

| Role | Hash |
|---|---|
| Phase 1B.3-A plan acceptance commit | 54700b1af8c6e831a82fa2d8c90254932f3955a4 |
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |
| B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |

---

## Confirmed Current State

- Workflow Backend Foundation complete (B1).
- Workflow Admin Configuration UI complete (B2).
- Workflow runtime backend primitives exist (7 runtime endpoints in WorkflowRuntimeController).
- Workflow Runtime / My Approvals UI does not exist (no frontend runtime pages found).
- Pilot business process integration does not exist.
- No implementation is authorized by this plan.

---

## B1 Runtime Backend Endpoint Inventory

| # | HTTP | Path | Auth Model | Available |
|---|---|---|---|---|
| 1 | POST | /workflows/instances | Authenticated (self-scoped requester) | Yes |
| 2 | GET | /workflows/instances/:instanceId | RequirePermission(WORKFLOW_VIEW, Global) | Yes |
| 3 | GET | /workflows/my-approvals | Authenticated (self-scoped) | Yes |
| 4 | POST | /workflows/instances/:instanceId/steps/:stepId/approve | Authenticated (assignee check in service) | Yes |
| 5 | POST | /workflows/instances/:instanceId/steps/:stepId/return | Authenticated (assignee check in service) | Yes |
| 6 | POST | /workflows/instances/:instanceId/resubmit | Authenticated (requester check in service) | Yes |
| 7 | POST | /workflows/instances/:instanceId/withdraw | Authenticated (requester check in service) | Yes |
| 8 | POST | /workflows/instances/:instanceId/steps/:stepId/reassign | RequirePermission(WORKFLOW_REASSIGN_PENDING, Company) | Yes |

### Missing Endpoints (Backend Gaps)

| Gap | Description | Impact |
|---|---|---|
| GAP-1 | No GET /workflows/my-requests (requester's own instances) | Cannot show "My Requests" list without WORKFLOW_VIEW permission |
| GAP-2 | No GET /workflows/instances/:instanceId/actions (action history/timeline) | Instance detail page cannot show approval action history |
| GAP-3 | No reject action endpoint | No explicit rejection — return is the closest equivalent |

### Backend Authorization Model Summary

- CreateInstance: any authenticated user; becomes the requester.
- GetInstance: requires WORKFLOW_VIEW (Global) permission.
- GetMyApprovals: self-scoped; returns only assignments for the authenticated user.
- Approve/Return: service-layer checks — must be assignee, must not be requester (for approve), step must be PENDING.
- Resubmit: service-layer check — must be original requester, instance must be RETURNED.
- Withdraw: service-layer check — must be original requester, instance must be PENDING_APPROVAL or RETURNED.
- Reassign: requires WORKFLOW_REASSIGN_PENDING (Company) permission.
- Concurrency: all mutation endpoints use RowVersion/TargetVersion for optimistic concurrency.

---

## Proposed B3 Scope

### Included in B3

- My Approvals menu item and navigation.
- My Approvals inbox/list page (GET /workflows/my-approvals).
- Approval task detail page (navigates from inbox to instance detail via GET /workflows/instances/:instanceId).
- Instance detail page showing instance metadata, status, steps with assignees, and step statuses.
- Approve action UI with reason/comment form and confirmation (POST .../approve).
- Return action UI with required reason, optional comment, and confirmation (POST .../return).
- Resubmit action UI for requester on RETURNED instances (POST .../resubmit).
- Withdraw action UI for requester on PENDING_APPROVAL or RETURNED instances (POST .../withdraw).
- Instance status badges (PENDING_APPROVAL, RETURNED, WITHDRAWN, PENDING_EXECUTION, COMPLETED, CANCELLED).
- Step status badges (PENDING, WAITING, APPROVED, RETURNED, CANCELLED).
- Version/snapshot freeze notice on instance detail.
- Sanitized error handling (403, 404, concurrency, business rule errors).
- Loading/empty/error states.
- Concurrency/refresh UX using RowVersion on approve/return actions.

### Conditionally Included (Depends on Open Decisions)

- My Requests page (DEC-1B3B3-01).
- Action history/timeline display (DEC-1B3B3-02).
- Reassignment UI (DEC-1B3B3-06).
- Payload/metadata display (DEC-1B3B3-07).

### Explicitly Deferred from B3

- Generic workflow instance creation UI (belongs to B4 pilot integration).
- Business-specific workflow start screens (belongs to B4 pilot integration).
- Embedding workflow widgets into business process screens (belongs to B4 pilot integration).
- Active instance migration UI.
- Pilot integration with Customer/Service/Payment/Merge.
- Service module implementation.
- Payment/Reconciliation implementation.
- Customer Merge implementation.
- ENTITY scope.
- Export/download.
- Backend/API/database/migration changes by default.
- PermissionCodes.cs or permission catalog changes by default.
- Workflow Admin Configuration UI changes unless required only for navigation consistency.
- No production migration/release.

---

## Proposed Routes

| Route | Page | Conflict Check |
|---|---|---|
| `/workflow/my-approvals` | WorkflowMyApprovalsPage | No conflict with B2 routes |
| `/workflow/instances/:instanceId` | WorkflowInstanceDetailPage | No conflict with B2 routes |
| `/workflow/my-requests` | WorkflowMyRequestsPage (conditional — DEC-1B3B3-01) | No conflict with B2 routes |

Existing B2 routes remain unchanged:
- `/workflow` — WorkflowDefinitionsPage
- `/workflow/definitions/*` — admin definition/version pages
- `/workflow/bindings` — WorkflowBindingsPage

---

## Proposed Pages and Components

| Component | Description |
|---|---|
| WorkflowMyApprovalsPage | Inbox list of pending approvals for the authenticated user |
| WorkflowInstanceDetailPage | Instance metadata, status, steps table with assignees, action buttons |
| WorkflowMyRequestsPage | Requester's own instances (conditional — requires GAP-1 resolution or WORKFLOW_VIEW) |
| WorkflowApproveModal | Confirmation modal with optional reason/comment, calls approve endpoint |
| WorkflowReturnModal | Confirmation modal with required reason, optional comment, calls return endpoint |
| WorkflowResubmitConfirm | Confirmation dialog for resubmission, calls resubmit endpoint |
| WorkflowWithdrawConfirm | Confirmation dialog for withdrawal, calls withdraw endpoint |
| WorkflowReassignModal | Modal with user selection and required reason (conditional — DEC-1B3B3-06) |
| WorkflowInstanceStatusBadge | Tag component for instance statuses with appropriate colors |
| WorkflowStepStatusBadge | Tag component for step statuses with appropriate colors |
| WorkflowVersionSnapshotNotice | Alert banner explaining frozen version snapshot behavior |
| WorkflowActionTimeline | Timeline of approval actions (conditional — requires GAP-2 resolution) |
| WorkflowPayloadSummary | Safe metadata display for instance payload (conditional — DEC-1B3B3-07) |

---

## API Client Plan

### New Runtime API Functions

| Function | HTTP | Path | Request Type | Response Type |
|---|---|---|---|---|
| getMyApprovals | GET | /workflows/my-approvals | — | MyApprovalItem[] |
| getInstance | GET | /workflows/instances/:id | — | WorkflowInstance |
| approveStep | POST | /workflows/instances/:id/steps/:stepId/approve | ApprovalActionRequest | WorkflowInstance |
| returnStep | POST | /workflows/instances/:id/steps/:stepId/return | ApprovalActionRequest | WorkflowInstance |
| resubmitInstance | POST | /workflows/instances/:id/resubmit | { targetVersion } | WorkflowInstance |
| withdrawInstance | POST | /workflows/instances/:id/withdraw | { targetVersion } | WorkflowInstance |
| reassignStep | POST | /workflows/instances/:id/steps/:stepId/reassign | ReassignStepRequest | WorkflowInstance (conditional — DEC-1B3B3-06) |

### Existing API Functions (reused from B2 workflowApi.ts)

None — B3 runtime API functions are entirely new; no B2 admin functions need modification.

### Endpoints NOT Called by B3

- POST /workflows/instances (instance creation) — deferred to B4 pilot integration.
- No unsupported/fictional endpoints.
- No fake client-only mutation behavior.

### Blockers from Backend Gaps

| Gap | Blocked Feature | Workaround |
|---|---|---|
| GAP-1: No my-requests endpoint | My Requests page | Defer to future phase, or use getInstance if requester knows instance ID |
| GAP-2: No action history endpoint | Action timeline on instance detail | Show steps with status only; no granular action log |
| GAP-3: No reject endpoint | No reject action | Return action is the closest equivalent; UI labels it as "Return" |

---

## Permission and Authorization Strategy

### Runtime Page Access

| Page | Access Control |
|---|---|
| My Approvals | Available to all authenticated users (self-scoped endpoint) |
| Instance Detail | Requires WORKFLOW_VIEW (Global) — backend-enforced |
| My Requests | Self-scoped if GAP-1 is resolved; otherwise requires WORKFLOW_VIEW |

### Action Eligibility

| Action | Who Can Act | Frontend Display Logic |
|---|---|---|
| Approve | Assigned approver (not requester) | Show button if user appears in step.assignees AND user is not instance.requesterId AND step is PENDING |
| Return | Assigned approver | Show button if user appears in step.assignees AND step is PENDING |
| Resubmit | Original requester | Show button if user is instance.requesterId AND instance is RETURNED |
| Withdraw | Original requester | Show button if user is instance.requesterId AND instance is PENDING_APPROVAL or RETURNED |
| Reassign | WORKFLOW_REASSIGN_PENDING holder | Show button if hasPermission('WORKFLOW_REASSIGN_PENDING', 'COMPANY') AND step is PENDING |

### Authorization Principles

- Backend remains authoritative for all actions.
- Frontend button visibility is UX guidance only — backend rejects unauthorized actions with appropriate error codes.
- Frontend must not assume authority from permissions alone for approve/return/resubmit/withdraw.
- Frontend derives action eligibility from backend-provided data (assignee lists, requester ID, status fields).
- DENY-wins behavior is backend-enforced.
- No localStorage/sessionStorage/cookie permission persistence.

---

## UX Strategy

### Status Labels

| Instance Status | Color | Label |
|---|---|---|
| PENDING_APPROVAL | blue | Pending Approval |
| RETURNED | orange | Returned |
| WITHDRAWN | red | Withdrawn |
| PENDING_EXECUTION | cyan | Pending Execution |
| COMPLETED | green | Completed |
| CANCELLED | gray | Cancelled |

| Step Status | Color | Label |
|---|---|---|
| PENDING | blue | Pending |
| WAITING | gray | Waiting |
| APPROVED | green | Approved |
| RETURNED | orange | Returned |
| CANCELLED | gray | Cancelled |

### Error Handling

- Sanitized 403: "You do not have permission to perform this action."
- 404: "Workflow instance not found."
- WF_INVALID_ROW_VERSION (409): Concurrency error with refresh button.
- WF_NOT_ASSIGNEE: "You are not an assignee for this step."
- WF_REQUESTER_IS_APPROVER: "Requester cannot approve their own request."
- WF_NOT_REQUESTER: "Only the original requester can perform this action."
- WF_INSTANCE_NOT_PENDING: "This instance is no longer pending approval."
- WF_STEP_NOT_PENDING: "This step is no longer pending."
- WF_REASON_REQUIRED: "Reason is required for this action."
- Generic error fallback.

### Confirmation Dialogs

- Approve: "Are you sure you want to approve this step?"
- Return: "Are you sure you want to return this request? A reason is required."
- Resubmit: "Are you sure you want to resubmit this request for approval?"
- Withdraw: "Are you sure you want to withdraw this request? This will cancel all pending steps."

### Version/Snapshot Behavior

- Instance detail page shows a notice: "This instance uses a frozen snapshot of workflow version N. Changes to the workflow definition do not affect this instance."
- No active instance route mutation.

### Safe Payload Display

- If payload display is included (DEC-1B3B3-07), show only safe metadata fields (processCode, businessEntityType, businessEntityId, companyId).
- Do not display raw PayloadJson or BeforeDataJson to avoid exposing sensitive data.
- Backend does not return PayloadJson/BeforeDataJson in the instance DTO — this is already safe by design.

---

## Testing Strategy

### Route and Navigation Tests
- My Approvals menu item visible to authenticated users.
- My Approvals menu item navigates to /workflow/my-approvals.
- Instance detail route renders WorkflowInstanceDetailPage.

### My Approvals Inbox Tests
- Renders approval list when data exists.
- Shows empty state when no pending approvals.
- Shows loading state.
- Shows error state on fetch failure.
- Shows 403 permission denied state.
- Each approval item navigates to instance detail.

### Instance Detail Tests
- Renders instance metadata (process, entity type, entity ID, status, round).
- Shows instance status badge with correct color.
- Renders steps table with step statuses and assignees.
- Shows version snapshot notice.
- Shows 404 when instance not found.
- Shows 403 when permission denied.
- Shows error state on fetch failure.

### Approve Action Tests
- Shows approve button when user is assignee and step is PENDING and user is not requester.
- Hides approve button when user is not assignee.
- Hides approve button when user is requester (WF_REQUESTER_IS_APPROVER guard).
- Approve modal shows reason/comment fields.
- Approve calls backend with correct instanceId, stepId, and targetVersion.
- Shows concurrency error with refresh on 409.
- Shows WF_NOT_ASSIGNEE error.

### Return Action Tests
- Shows return button when user is assignee and step is PENDING.
- Return modal requires reason.
- Return calls backend with correct parameters.
- Shows concurrency error with refresh on 409.

### Resubmit Action Tests
- Shows resubmit button when user is requester and instance is RETURNED.
- Hides resubmit button when user is not requester.
- Resubmit calls backend with targetVersion.
- Shows concurrency error with refresh on 409.

### Withdraw Action Tests
- Shows withdraw button when user is requester and instance is PENDING_APPROVAL or RETURNED.
- Hides withdraw button when user is not requester.
- Withdraw calls backend with targetVersion.
- Shows concurrency error with refresh on 409.

### Reassignment Tests (Conditional — DEC-1B3B3-06)
- Shows reassign button when user has WORKFLOW_REASSIGN_PENDING.
- Reassign modal with user selection and required reason.
- Reassign calls backend with newAssigneeUserId and targetVersion.

### Deferred Behavior Tests
- No instance creation UI exists.
- No pilot integration exists.
- No admin migration UI exists.

---

## Open Decisions

### DEC-1B3B3-01: Include My Requests Page?

**Question**: Should B3 include a "My Requests" page showing instances submitted by the authenticated user?

**Analysis**: The backend has no GET /workflows/my-requests endpoint (GAP-1). Without it, a requester would need WORKFLOW_VIEW to use GET /workflows/instances/:id and would need to know their instance IDs.

**Options**:
- A) Defer My Requests to a future phase after GAP-1 backend endpoint is added.
- B) Include My Requests in B3 but require a backend GAP-1 resolution (separate approval needed).
- C) Include a minimal My Requests page using GET /workflows/instances/:id if the requester navigates from elsewhere.

**Recommendation**: Option A — defer until GAP-1 is resolved.

### DEC-1B3B3-02: Include Action History Timeline?

**Question**: Should B3 include an action history/timeline on the instance detail page?

**Analysis**: The backend has no GET endpoint for WorkflowActions (GAP-2). The WorkflowAction entity stores action type, actor, reason, comment, and timestamp, but this data is not exposed via any API endpoint.

**Options**:
- A) Defer action timeline to a future phase after GAP-2 backend endpoint is added.
- B) Include action timeline in B3 but require a backend GAP-2 resolution (separate approval needed).
- C) Show step status progression only (WAITING → PENDING → APPROVED/RETURNED) without granular action log.

**Recommendation**: Option C for B3 — show step status progression. Defer granular timeline until GAP-2 is resolved.

### DEC-1B3B3-03: Include Instance Creation UI?

**Question**: Should B3 include a generic "Start workflow instance" screen?

**Analysis**: POST /workflows/instances exists but requires processCode, businessEntityType, businessEntityId, payloadJson, and optionally beforeDataJson. These are business-specific fields that require integration with the specific business process (Customer, Service, Payment, etc.). A generic creation form would be impractical without pilot integration context.

**Recommendation**: Defer to B4 pilot integration. Instance creation should originate from within a business process screen, not a standalone workflow form.

### DEC-1B3B3-04: Reject Action?

**Question**: Should B3 include a "Reject" action?

**Analysis**: The backend has no reject endpoint (GAP-3). The "Return" action (POST .../return) returns the instance to the requester for resubmission. There is no terminal rejection.

**Recommendation**: Do not include a reject action. Use "Return" only. If terminal rejection is needed, it requires a separate backend feature phase.

### DEC-1B3B3-05: Approve Self-Guard Display?

**Question**: How should the UI handle the WF_REQUESTER_IS_APPROVER rule?

**Analysis**: The backend throws WF_REQUESTER_IS_APPROVER if the approver is the same user as the requester. The frontend can proactively hide the approve button when the authenticated user's ID matches instance.requesterId, providing a cleaner UX than relying on backend rejection.

**Recommendation**: Proactively hide the approve button when the current user is the requester. Still handle the backend error gracefully as a fallback.

### DEC-1B3B3-06: Include Reassignment UI?

**Question**: Should B3 include step reassignment UI using WORKFLOW_REASSIGN_PENDING?

**Analysis**: The backend endpoint exists (POST .../reassign) and requires WORKFLOW_REASSIGN_PENDING (Company scope). This is an admin operation, not a standard approver action. It adds a new assignee to a pending step.

**Options**:
- A) Include reassignment in B3 — it is a runtime operation and the endpoint exists.
- B) Defer reassignment to a future phase — it is an admin-level runtime operation.

**Recommendation**: Option A — include it since the endpoint exists and it is a runtime action. Gate with WORKFLOW_REASSIGN_PENDING permission check.

### DEC-1B3B3-07: Payload Display Strategy

**Question**: How should instance payload be displayed in the UI?

**Analysis**: The WorkflowInstanceDto returned by GET /workflows/instances/:id does NOT include PayloadJson or BeforeDataJson fields. It only includes: id, workflowVersionId, processCode, companyId, requesterId, businessEntityType, businessEntityId, instanceStatus, roundNo, rowVersion, createdAt, updatedAt, and steps array. Therefore, no sensitive payload data can leak through the current DTO.

**Recommendation**: Display the available metadata fields only (processCode, businessEntityType, businessEntityId, companyId, requesterId). No payload display is needed or possible with the current DTO. This is safe by design.

### DEC-1B3B3-08: My Approvals Menu Gating

**Question**: Should the My Approvals menu item be visible to all authenticated users or gated by a permission?

**Analysis**: GET /workflows/my-approvals is self-scoped and does not require any explicit permission — it returns only the authenticated user's own pending assignments. Unlike admin configuration which requires WORKFLOW_VIEW, this is a personal inbox.

**Options**:
- A) Show My Approvals to all authenticated users (matches self-scoped backend behavior).
- B) Gate My Approvals behind WORKFLOW_VIEW (consistent with existing workflow menu gating).

**Recommendation**: Option A — show to all authenticated users, since the endpoint is self-scoped and does not require WORKFLOW_VIEW.

### DEC-1B3B3-09: Frontend-Only B3 Sufficiency

**Question**: Is a frontend-only B3 sufficient against existing B1 runtime endpoints?

**Analysis**: The core approver workflow (inbox → detail → approve/return) and requester workflow (withdraw on pending, resubmit on returned) are fully supported by existing B1 endpoints. The gaps (GAP-1: my-requests, GAP-2: action history, GAP-3: reject) affect nice-to-have features, not the core flow.

**Recommendation**: Frontend-only B3 is sufficient for the core runtime/approval UI. Backend gaps should be tracked but do not block B3 implementation.

---

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Runtime UI could imply client-side approval authority | High | All action eligibility derived from backend data; backend rejects unauthorized actions |
| Stale assignment/action race conditions | Medium | Concurrency UX with RowVersion and refresh on 409 |
| Approver/requester distinction error | High | Frontend checks assignees array and requesterId from backend DTO; backend is final authority |
| No action history (GAP-2) limits audit visibility | Low | Show step status progression; defer timeline to future phase |
| No My Requests (GAP-1) limits requester self-service | Low | Requester can still withdraw/resubmit from instance detail if they navigate to it |
| Future pilot integration not yet selected | Medium | B3 is generic runtime UI; B4 pilot integration is separate |
| Backend remains authoritative | — | Documented as design constraint |
| Active instance migration remains deferred | Low | No migration UI; no active route mutation |

---

## Recommended Project Owner Decision

The existing B1 runtime endpoints are sufficient for a core B3 frontend-only implementation covering:
- My Approvals inbox (self-scoped endpoint).
- Instance detail (WORKFLOW_VIEW-gated endpoint).
- Approve/return actions (assignee-scoped with concurrency).
- Resubmit/withdraw actions (requester-scoped with concurrency).
- Reassignment (WORKFLOW_REASSIGN_PENDING-gated with concurrency).

**Recommendation**: Approve B3 as frontend-only implementation using existing B1 runtime endpoints. No backend gap-resolution is required for the core flow. GAP-1 (my-requests), GAP-2 (action history), and GAP-3 (reject) should be tracked for future resolution but do not block B3.

---

## Explicit Non-Authorization

- This plan does not authorize implementation.
- No source code changes.
- No test changes.
- No backend changes.
- No frontend changes.
- No migrations.
- No rollbacks.
- No PermissionCodes.cs changes.
- No permission-catalog.md changes.
- No business-rules.md or acceptance-criteria.md changes.
- No Service/Payment/Merge/ENTITY/Export implementation.
- No production migration/release.

---

## Conclusion

PHASE 1B.3-B3 WORKFLOW RUNTIME / MY APPROVALS UI DETAILED PLAN READY FOR PROJECT OWNER REVIEW
