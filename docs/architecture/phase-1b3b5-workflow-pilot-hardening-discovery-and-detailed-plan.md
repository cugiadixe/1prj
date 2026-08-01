# Phase 1B.3-B5 Workflow Pilot Hardening Discovery and Detailed Plan

## Status
PROPOSED — AWAITING PROJECT OWNER APPROVAL

## Baseline
- Current HEAD:
  7b127837ec1f92f46077f64076d0122ea733333d
- Selected next-work decision commit:
  7b127837ec1f92f46077f64076d0122ea733333d
- Post-B4 next-work review commit:
  242dcdbc24acc4626ff8400ed00a0a1197b88fa3
- Final B4 acceptance commit:
  795ed4057881831c8a34efd4dc1cd5eeb0ed46dc

## Planning authorization
- Discovery/planning only.
- No implementation authorized yet.

## Current workflow runtime gap analysis

**My Requests**
- Current availability: Not available in `WorkflowRuntimeController.cs`.
- Existing frontend workaround: The frontend relies on the `customerProposalApi` to show "My Proposals", but a generic `My Requests` view across all workflow types does not exist.
- Needed query/filter behavior: A unified endpoint `GET /api/v2/workflows/my-requests` to show instances created by the actor.
- Expected requester visibility rules: Requesters should see workflow status, current pending step, and final outcome.
- Security concerns: Ensure requesters only see their own instances (or instances within their delegated permissions).

**Action history/timeline**
- Current Workflow_Actions persistence: Assumed tracked in the database, but no API endpoint exists to fetch it.
- Existing API exposure: None.
- Data that can be safely exposed: Action types (Submit, Approve, Return, Withdraw), actor name (if permitted), timestamps, comments.
- Data that must not be exposed: Sensitive internal system execution details or internal comments restricted to admins.
- Frontend timeline needs: A visual timeline showing the path of the request.
- Audit vs user-visible history distinction: Ensure internal audit logs remain separate from business timeline events.

**Reject behavior**
- Current available actions: Approve, Return, Withdraw, Resubmit, Reassign. Reject is missing.
- Difference between return, withdraw, reject, cancel, and deny:
  - Return: Sent back to requester for modifications.
  - Withdraw: Requester cancels the request.
  - Reject: Final denial by an approver, terminating the workflow instance as failed/rejected.
- Whether reject is supported by current backend: No `RejectStep` API or service method exists.
- Required business decision before implementation: Define if a Reject transitions the status to 'Rejected' permanently and whether resubmission is allowed.
- Effect on workflow instance status: Transitions to `Completed_Rejected`.
- Effect on business entity execution: Entity change is aborted, no execution handler runs.

**Execution failure retry UX**
- Current execution handler behavior: Silent failure or unhandled exception if execution fails post-approval.
- How execution failure is represented: Missing explicit state (e.g., `Failed_Execution`).
- Whether retry is backend-supported: No retry endpoint exists.
- Who may retry: Administrators or the final approver.
- Permission requirements: `WorkflowRetryExecution` permission needed.
- Idempotency requirements: Handlers must remain idempotent to support safe retries.
- Safe error display requirements: Do not expose raw exception stack traces to UI.

**Operational validation**
- What should be validated with real users: Ensure Return and Withdraw flows function correctly for edge cases.
- What evidence should be collected: Audit logs, user feedback on UX, correct permission gating.
- How CREATE_CUSTOMER pilot should be checked before wider workflow expansion: Confirm no stuck proposals and correct idempotency in production-like environments.

**User lookup/reassign UX**
- Current reassignment capability: `ReassignStep` exists in API, but frontend lacks a rich lookup component.
- Current frontend UX limitation: Manual ID entry is prone to errors.
- Whether lookup is manual ID-based or needs user search: Needs a Typeahead/Autocomplete search component.
- Permission requirements: User lookup API requires directory read permissions.
- Audit requirements: Reassignments must log the assigner, new assignee, and reason.

**Permission/business/acceptance impact**
- Existing permission codes that can be reused: `WorkflowView`, `WorkflowReassignPending`.
- Potential new permission codes: `WorkflowRetryExecution`, `WorkflowReject`.
- Future updates: `permission-catalog.md`, `business-rules.md`, and `acceptance-criteria.md` will require updates to document Reject, Retry, and My Requests features.

**Database impact**
- Whether existing tables support the hardening items: `WorkflowAction` and `WorkflowInstance` likely support the data models, but enum values for 'Rejected' and 'ExecutionFailed' may need addition.
- Migration risk: Low, mostly enum additions or minor column additions.
- Rollback risk: Standard enum/column rollback.
- Rowversion/concurrency considerations: Concurrency on retry and reject must be strict.

**API v2 impact**
- Proposed endpoints:
  - `GET /api/v2/workflows/my-requests`
  - `GET /api/v2/workflows/instances/{instanceId}/history`
  - `POST /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject`
  - `POST /api/v2/workflows/instances/{instanceId}/retry`
- Error code strategy: Add codes for `ExecutionRetryFailed`, `InvalidRejectState`.

**Frontend impact**
- Routes/pages to add or change: `/workflows/my-requests`, `/workflows/:id/timeline`. Update B3 My Approvals with Reject and Reassign buttons.
- Status/timeline UI: Timeline component.
- Permission-gated navigation: Only show Retry to authorized admins.
- Tests required: Component tests for timeline and new actions.

## Decision proposals

**1. My Requests**
- Problem: Requesters lack a centralized view of their initiated workflows.
- Current state: Only available domain-by-domain (e.g., My Proposals for customers).
- Options:
  1. Build a generic `My Requests` API and UI.
  2. Continue domain-specific views only.
- Recommended option: Option 1.
- Reasoning: A core workflow engine benefit is centralized tracking.
- Impact: New API endpoint and UI page.
- Risks: Performance with many instances.
- Required Project Owner decision: Approve building generic My Requests.

**2. Action history/timeline**
- Problem: Users cannot see the approval path taken.
- Current state: Data is collected but not exposed.
- Options:
  1. Expose full history to all involved users.
  2. Expose sanitized history (no internal comments) to requester/approvers.
- Recommended option: Option 2.
- Reasoning: Balances transparency with security.
- Impact: New API endpoint and timeline UI component.
- Risks: Accidental exposure of sensitive audit data.
- Required Project Owner decision: Approve sanitized history exposure.

**3. Reject behavior**
- Problem: No way to definitively deny a request.
- Current state: Approvers can only Return (requester must act) or ignore.
- Options:
  1. Treat Reject as a permanent terminal state (requires new request to try again).
  2. Allow requester to 'Revive' a rejected request.
- Recommended option: Option 1.
- Reasoning: Simplifies state machine. A new request provides a cleaner audit trail.
- Impact: API additions, state machine updates.
- Risks: Requesters might lose large data entries if rejected.
- Required Project Owner decision: Approve Reject as terminal state.

**4. Execution failure retry UX**
- Problem: If execution fails post-approval, instance is stuck.
- Current state: Silent failure/stuck pending status.
- Options:
  1. Auto-retry.
  2. Manual retry API for admins.
- Recommended option: Option 2.
- Reasoning: Auto-retry is dangerous without idempotency guarantees. Manual retry allows admins to fix underlying issues first.
- Impact: New API, admin UI button.
- Risks: Duplicate entity creation if idempotency fails.
- Required Project Owner decision: Approve manual retry UX for admins.

**5. Operational validation checklist proposal**
- Problem: Need structured validation before exiting the pilot.
- Current state: B4 pilot is accepted but unhardened.
- Recommended option: Execute a formal operational validation checklist (dry runs of Reject, Retry, Return).
- Reasoning: Ensures edge cases are tested in realistic scenarios.
- Required Project Owner decision: Approve operational validation step.

**6. User lookup/reassign UX**
- Problem: Reassigning requires typing an exact User ID.
- Current state: Bare-bones manual input.
- Recommended option: Add a secure user-search autocomplete component.
- Reasoning: Greatly improves usability for approvers.
- Required Project Owner decision: Approve search API usage for reassignment.

**7. Permission update**
- Problem: Missing permissions for new actions.
- Recommended option: Add `WorkflowReject` and `WorkflowRetryExecution`.
- Required Project Owner decision: Approve new permission codes.

**8. Database update**
- Problem: Enums/columns for Reject and Retry states needed.
- Recommended option: Add `Rejected` and `ExecutionFailed` to workflow status enums.
- Required Project Owner decision: Approve database migrations.

**9. API v2 update**
- Problem: Missing endpoints for new features.
- Recommended option: Implement the proposed endpoints for My Requests, History, Reject, and Retry.
- Required Project Owner decision: Approve API surface changes.

**10. Frontend update**
- Problem: UI needs to support new API features.
- Recommended option: Build Timeline, My Requests page, and update action buttons.
- Required Project Owner decision: Approve frontend scope.

**11. Test strategy**
- Problem: Ensure hardening does not break B4 features.
- Recommended option: Mandate full regression on CREATE_CUSTOMER alongside new feature tests.
- Required Project Owner decision: Approve test strategy.

## Recommended B5 implementation phases

- **Phase 1B.3-B5-A: Discovery and Detailed Plan**
  - Scope: The current document and PO decisions.
  - Out-of-scope: Implementation.
  - Entry criteria: B4 complete.
  - Exit criteria: PO approval of this plan.

- **Phase 1B.3-B5-B: Backend Runtime Hardening**
  - Scope: My Requests API, action history API, reject API, execution retry API, DTOs, validators, audit, tests.
  - Out-of-scope: Frontend UI.
  - Entry criteria: PO approval of B5-A.
  - Exit criteria: Backend PR approved, tests pass.
  - Required tests: Unit, Integration, API tests.
  - Risks: Breaking existing B4 flows.
  - Stop conditions: DB migration fails.

- **Phase 1B.3-B5-C: Frontend Runtime Hardening**
  - Scope: My Requests UI, timeline UI, reject UX, execution retry UX, reassign lookup UX, tests.
  - Out-of-scope: Backend API changes.
  - Entry criteria: B5-B complete.
  - Exit criteria: Frontend PR approved, tests pass.
  - Required tests: Component tests, API client tests.
  - Risks: State mismatch between UI and API.
  - Stop conditions: Unauthorized data exposure in Timeline.

- **Phase 1B.3-B5-D: Operational Validation and Closure**
  - Scope: Pilot validation checklist, evidence, closure review, PO final acceptance.
  - Out-of-scope: New feature development.
  - Entry criteria: B5-C complete.
  - Exit criteria: PO sign-off.
  - Required tests: Manual operational scenarios.
  - Risks: Late discovery of fundamental workflow flaws.
  - Stop conditions: Unresolved validation failures.

## Required Project Owner decisions
- Approve or reject the B5 plan.
- Approve exact B5-B backend scope.
- Approve exact B5-C frontend scope.
- Decide My Requests scope.
- Decide action history/timeline scope.
- Decide reject behavior.
- Decide execution retry behavior.
- Decide user lookup/reassign UX scope.
- Decide whether new permission codes are authorized.
- Decide whether DB migrations are authorized.
- Decide whether business-rules.md, permission-catalog.md, or acceptance-criteria.md updates are authorized.

## Explicit non-scope
- Service module implementation.
- Payment module implementation.
- CUSTOMER_MASTER_CHANGE implementation unless separately selected later.
- Merge flow implementation.
- Card flow implementation.
- Plot flow implementation.
- ENTITY scope expansion.
- Export/download features.
- Production migration.
- Production release.
- New business module integration.
- Broad workflow engine rewrite.
- Replacing existing direct customer create.
- Changing B4 accepted CREATE_CUSTOMER behavior without PO approval.

## Test strategy
- Backend unit tests.
- Backend integration tests.
- API tests.
- Frontend component tests.
- Frontend API client tests.
- Permission/403 tests.
- Safe payload exposure tests.
- Regression tests for B4 CREATE_CUSTOMER.
- Migration/rollback tests if DB changes are approved.

## Risks
- Runtime hardening can change workflow semantics.
- Reject behavior can conflict with return/withdraw if not defined.
- History/timeline can expose sensitive data if not sanitized.
- Execution retry can create duplicate business records if idempotency is not preserved.
- User lookup can expose unauthorized user data if not permission-scoped.
- DB migration needs approval and rollback plan.
- Frontend status wording can confuse users if workflow/business statuses are not separated.

## Stop conditions
- Stop if implementation is attempted before PO plan approval.
- Stop if business semantics of reject are not approved.
- Stop if action history exposure rules are unclear.
- Stop if execution retry ownership is unclear.
- Stop if permission codes are missing or not authorized.
- Stop if DB migration is needed but not authorized.
- Stop if safe payload exposure cannot be guaranteed.
- Stop if production release is requested without release readiness review.

## Conclusion
PHASE 1B.3-B5 WORKFLOW PILOT HARDENING PLAN PROPOSED FOR PROJECT OWNER APPROVAL
