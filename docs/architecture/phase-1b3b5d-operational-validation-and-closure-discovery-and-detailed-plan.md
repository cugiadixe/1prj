# Phase 1B.3-B5-D Operational Validation and Closure Discovery and Detailed Plan

## Status

PROPOSED — AWAITING PROJECT OWNER APPROVAL

## Planning Baseline

| Role | Hash |
|---|---|
| B5-C Project Owner frontend implementation acceptance commit | 39760a9cbee6fe6f352b4336423b89a8b2149086 |
| B5-C frontend implementation commit | c11a655cf7f909e1a60f3d3eecbd8db70e8023be |
| B5-B Project Owner backend implementation acceptance commit | c42734e351404d9788b82e2049c92f6de09baf18 |
| B5-B backend implementation commit | 0394379ca343906bb8560dc0359fb853dc3b658a |
| B5 plan acceptance commit | f13afa48ecfaa8fa190137164b1a49ba70dee06e |

## Purpose

Plan operational validation and closure for Phase 1B.3-B5 after backend (B5-B) and frontend (B5-C) runtime hardening were both accepted. B5-D validates the combined backend + frontend runtime behavior end-to-end, confirms no regressions, and closes the B5 phase.

---

## Accepted B5-B Backend Capabilities

- My Requests backend API: `GET /api/v2/workflows/my-requests` — returns requester's own instances.
- Action History backend API: `GET /api/v2/workflows/instances/{instanceId}/actions` — backend-enforced authorization (requester or assignee only).
- Reject backend support: `POST /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject` — terminal rejection, reason required, assignee + WORKFLOW_REJECT permission enforced.
- Execution Retry backend support: `POST /api/v2/workflows/instances/{instanceId}/retry-execution` — WORKFLOW_RETRY_EXECUTION permission (Global scope), FAILED status required, idempotent.
- V0008/U0008 migration and rollback: WorkflowAction table, WorkflowInstance status columns for REJECTED/FAILED states.
- WORKFLOW_REJECT permission added to PermissionCodes.cs and permission-catalog.md.
- WORKFLOW_RETRY_EXECUTION permission added to PermissionCodes.cs and permission-catalog.md.
- Safe user lookup/reassign deferred.
- Raw PayloadJson/BeforeDataJson not exposed. Sensitive customer fields not exposed.
- Backend test evidence: 145 unit, 196 integration, 261 API tests passed.

## Accepted B5-C Frontend Capabilities

- My Requests UI: Route `/workflow/my-requests`, nav item (no permission gate), table with safe metadata, loading/empty/error states, row click navigates to instance detail.
- Action History / Timeline UI: `WorkflowActionHistoryPanel` on instance detail, displays actionType/actedBy/reason/comment/createdAt, no raw payload.
- Reject UX: `WorkflowRejectDialog` with required reason (maxLength 500), optional comment, permanent warning, confirmation modal.
- Execution Retry UX: `WorkflowRetryExecutionButton` with confirmation modal, visible only when FAILED + WORKFLOW_RETRY_EXECUTION.
- Frontend API client: 4 functions added to `workflowRuntimeApi.ts` (getMyRequests, getInstanceActions, rejectStep, retryExecution).
- Frontend types: `WorkflowActionDto` added to `types.ts`.
- Frontend error codes: `WF_INSTANCE_NOT_FAILED`, `WF_ALREADY_REJECTED` added.
- Frontend permission gating: WORKFLOW_REJECT (assignee + not requester + permission), WORKFLOW_RETRY_EXECUTION (FAILED + permission).
- Route/navigation: App.tsx route, AuthenticatedShell nav item.
- Customer test hygiene: Unused-import removal only in 3 customer test files.
- Frontend test evidence: 44 test files, 371 tests passed. Pre-existing flaky timeout in UserAdminGroupAssignmentsPage.test.tsx observed once, clean on rerun.

---

## B5-D Validation Scope

### A. Build and Automated Test Validation

Run all backend and frontend automated checks to confirm no regressions from B5-B + B5-C combined state.

**Backend commands:**
```
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
```

**Expected backend results:**
- Build: 0 errors, 0 warnings.
- Unit tests: 145 passed.
- Integration tests: 196 passed.
- API tests: 261 passed.

**Frontend commands:**
```
cd src/frontend
npx oxlint
npx tsc -b
npx vitest run
```

**Expected frontend results:**
- oxlint: exit 0, 3 pre-existing auth warnings only (non-B5, non-blocking).
- tsc: exit 0, 0 errors.
- vitest: 44 test files, 371 tests passed.

**Repository check:**
```
git diff --check
git status --short --untracked-files=all
```

**Handling pre-existing issues:**
- oxlint `react(only-export-components)` warnings in auth providers: pre-existing, non-blocking, document as known follow-up.
- UserAdminGroupAssignmentsPage.test.tsx flaky timeout: pre-existing, non-B5. If it recurs, rerun once to confirm flaky; document as known follow-up. Do not block B5-D closure.

### B. Migration and Rollback Validation

Validate V0008 apply and U0008 rollback using the existing MigrationRollbackTests.

**Validation approach:**
- `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false --filter "FullyQualifiedName~MigrationRollbackTests"` — confirms V0008 applies and U0008 rolls back in dependency-safe order.
- Test uses PTKD_TEST_PHASE1A2 database only.
- No production migration.
- SchemaVersions state after rollback is verified by the test.

**Expected result:**
- MigrationRollbackTests pass (already included in the 196 integration tests above).

### C. End-to-End Workflow Runtime Validation

Validate the CREATE_CUSTOMER workflow pilot with B5-B + B5-C combined capabilities.

**Automated E2E coverage (already in API tests):**
- WorkflowRuntimeApiTests covers: create instance, my-requests listing, approve/return/resubmit/withdraw lifecycle, reject terminal state, retry execution, action history retrieval, unauthorized access rejection.
- 261 API tests exercise the full backend lifecycle.

**Manual/operational validation checklist:**

1. Start backend locally.
2. Start frontend locally.
3. Log in as requester user.
4. Submit CREATE_CUSTOMER proposal via `/customers/proposals/new`.
5. Confirm proposal appears in My Requests (`/workflow/my-requests`).
6. Log in as approver user.
7. Confirm pending approval appears in My Approvals (`/workflow/my-approvals`).
8. Open instance detail (`/workflow/instances/{id}`).
9. Confirm action history panel renders with safe fields (no payload, no sensitive data).
10. Attempt reject with empty reason — confirm UI validation blocks submission.
11. Reject with valid reason — confirm terminal REJECTED status.
12. Confirm no customer/business entity is created after rejection.
13. Simulate FAILED execution state (requires backend test seed or manual DB update in test environment).
14. Confirm retry button visible only for authorized user (WORKFLOW_RETRY_EXECUTION) and FAILED status.
15. Retry failed execution — confirm status transitions.
16. Confirm retry does not duplicate business entity.
17. Confirm action history updates after reject/retry actions.
18. Confirm sanitized errors for unauthorized/invalid cases (403, concurrency, etc.).
19. Confirm existing My Approvals and instance detail flows are not broken.
20. Confirm customer proposal screens are not broken by test hygiene cleanup.

**Limitation:** Step 13 (FAILED execution simulation) may not be achievable through the frontend alone without a test seed or manual DB intervention. If local test environment lacks failure simulation capability, document as limitation and rely on API test coverage for retry validation.

### D. Security and Permission Validation

Validate security rules across backend + frontend:

- WORKFLOW_REJECT gating: backend enforces assignee + permission; frontend hides button without permission or when not assignee or when requester.
- WORKFLOW_RETRY_EXECUTION gating: backend enforces FAILED status + Global permission; frontend hides button without permission or when not FAILED.
- Unauthorized user cannot access action history for instances they are not requester/assignee of (backend returns 403).
- Frontend hidden buttons are UX convenience only — not relied on for security.
- Raw PayloadJson is not displayed in any frontend component.
- BeforeDataJson is not displayed.
- Sensitive customer fields (CCCD, phone, address, DOB) are not displayed in action history, My Requests, or any summary.
- Stack traces are not displayed — `getErrorMessage()` returns sanitized messages.
- SQL/internal exception details are not displayed.
- Sanitized errors shown in frontend for all error states.

### E. UI Validation

Validate frontend UX behavior:

- My Requests route `/workflow/my-requests` renders correctly.
- My Requests navigation item visible to all authenticated users.
- My Requests loading state (spinner), empty state ("You have no workflow requests."), error state (sanitized message).
- Action History panel loading, empty ("No actions recorded."), error states.
- Reject dialog: required reason validation, permanent warning, modal confirmation.
- Reject success: state refresh, REJECTED status shown.
- Reject failure: sanitized error displayed.
- Retry confirmation modal with warning text.
- Retry success: state refresh, status update.
- Retry failure: sanitized error displayed.
- Existing My Approvals (`/workflow/my-approvals`) not broken.
- Existing instance detail approve/return/resubmit/withdraw not broken.
- Customer proposal create/detail/my-proposals screens not broken by test hygiene cleanup.

### F. Documentation and Closure Validation

Validate documentation completeness:

- B5-B accepted docs present: scope authorization, implementation report, acceptance review, PO acceptance.
- B5-C accepted docs present: plan, plan acceptance, implementation acceptance review, PO acceptance.
- B5-D validation evidence captured in closure report.
- Known deferred items documented.
- No additional business scope introduced by B5-D.

---

## Proposed B5-D Execution Artifacts

Future B5-D execution will produce:

- `docs/architecture/phase-1b3b5d-operational-validation-and-closure-report.md` — validation evidence and results.
- `docs/architecture/phase-1b3b5d-operational-validation-and-closure-acceptance-review.md` — review of closure report.
- `docs/architecture/phase-1b3b5d-project-owner-closure-acceptance.md` — Project Owner final B5 closure.

These documents are not created now. They will be created during B5-D execution after this plan is accepted.

---

## Required Validation Commands

### Backend

```
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
```

### Frontend

```
cd src/frontend
npx oxlint
npx tsc -b
npx vitest run
```

### Repository

```
git diff --check
git status --short --untracked-files=all
```

---

## Manual / Operational Validation Checklist

| # | Step | Expected Result |
|---|---|---|
| 1 | Start backend locally | Server starts without error |
| 2 | Start frontend locally | Dev server starts, renders login |
| 3 | Log in as requester | Dashboard renders with My Requests nav |
| 4 | Submit CREATE_CUSTOMER proposal | Proposal created, redirected |
| 5 | Check My Requests | New request appears with PENDING_APPROVAL |
| 6 | Log in as approver | Dashboard renders with My Approvals nav |
| 7 | Check My Approvals | Pending approval visible |
| 8 | Open instance detail | Metadata, steps, action history render |
| 9 | Verify action history | Safe fields only, no payload/sensitive data |
| 10 | Reject with empty reason | UI validation blocks submission |
| 11 | Reject with valid reason | Terminal REJECTED status, action recorded |
| 12 | Verify no entity created | No customer created after rejection |
| 13 | Simulate FAILED execution | Via test seed or manual DB (limitation noted) |
| 14 | Verify retry button visibility | Only for WORKFLOW_RETRY_EXECUTION + FAILED |
| 15 | Retry failed execution | Status transitions, action recorded |
| 16 | Verify no duplicate entity | Retry does not create duplicate |
| 17 | Verify action history updates | New entries appear after actions |
| 18 | Verify sanitized errors | 403/concurrency/invalid → user-facing message |
| 19 | Verify existing flows | My Approvals, instance detail not broken |
| 20 | Verify customer proposals | Create/detail/my-proposals not broken |

If steps 13-16 cannot be executed manually due to missing failure simulation in local test environment, document as limitation and rely on automated API test coverage (WorkflowRuntimeApiTests covers retry lifecycle).

---

## Acceptance Criteria for B5-D Closure

B5-D closure may pass only if:

- All required automated checks pass (backend build, unit, integration, API; frontend oxlint, tsc, vitest; git diff --check).
- Migration/rollback validation passes (MigrationRollbackTests).
- Backend + frontend runtime flow is validated (automated tests + manual checklist where feasible).
- Security/data exposure rules are validated (no raw payload, no sensitive fields, sanitized errors, backend-authoritative).
- No new unauthorized scope was introduced during B5-D.
- Known deferred items are documented.
- Any flaky test behavior is documented and classified as pre-existing non-B5.
- Closure report is created with evidence.
- Acceptance review passes.
- Project Owner closure acceptance is recorded.

---

## Deferred Items

- Safe user lookup/reassign expansion remains deferred (action history shows "User {id}" not names).
- Production release remains deferred.
- Service/Payment/CUSTOMER_MASTER_CHANGE/Merge/Card/Plot/ENTITY modules remain deferred.
- Pre-existing auth oxlint warnings (`react(only-export-components)` in AuthProvider.tsx, CompanyProvider.tsx) are non-B5 follow-up.
- Pre-existing flaky UserAdminGroupAssignmentsPage.test.tsx timeout should be monitored in B5-D; if it recurs, classify and document but do not block closure.
- FAILED execution simulation via frontend may be limited by test environment capabilities.

---

## Explicit Non-Scope

- New backend features.
- New frontend features.
- Migrations beyond validation (no new V0009).
- Production release.
- Release tag.
- Push.
- Service module.
- Payment module.
- CUSTOMER_MASTER_CHANGE.
- Customer merge.
- Card flow.
- Plot flow.
- ENTITY expansion.
- Export/download.
- User lookup/reassign expansion.
- Broad workflow engine rewrite.
- Broad frontend redesign.

---

## Required Project Owner Decisions

1. Approve B5-D validation plan.
2. Approve whether B5-D validation may be automated-only, manual-only, or mixed (recommendation: mixed — automated checks required, manual checklist best-effort where local environment supports it).
3. Approve whether manual validation can use local seeded data only (recommendation: yes — no production data access required).
4. Approve handling if pre-existing flaky test recurs (recommendation: rerun once, document as known pre-existing, do not block closure).
5. Approve whether B5-D closure can proceed without production release (recommendation: yes — B5 is internal pilot hardening, production release is separately authorized).
6. Approve whether B5-D should immediately lead to post-B5 next-work selection (recommendation: yes — after B5 closure, proceed to next-work selection review).

---

## Stop Conditions

Stop B5-D execution if:

- Any required automated check fails (and failure is not pre-existing/flaky).
- Migration/rollback validation fails.
- E2E workflow flow cannot be validated.
- Reject/retry behavior deviates from accepted B5-B/B5-C semantics.
- Raw payload or sensitive data exposure is found.
- Frontend requires backend changes.
- Backend requires business rule changes.
- Production release is requested without separate authorization.
- Scope expands beyond operational validation and closure.

---

## Recommendation

Proceed to Project Owner plan acceptance for B5-D. The automated test suites (145 unit + 196 integration + 261 API + 371 frontend = 973 total tests) provide strong validation coverage. Manual checklist supplements automated tests for UI/UX confirmation. B5-D is bounded to validation and closure only — no new features, no production release.

## Conclusion

PHASE 1B.3-B5-D OPERATIONAL VALIDATION AND CLOSURE PLAN PROPOSED — AWAITING PROJECT OWNER APPROVAL
