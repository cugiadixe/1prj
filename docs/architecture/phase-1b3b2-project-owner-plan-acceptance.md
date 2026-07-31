# Phase 1B.3-B2 Workflow Admin Configuration UI Project Owner Plan Acceptance

## Status

ACCEPTED — PHASE 1B.3-B2 WORKFLOW ADMIN CONFIGURATION UI PLAN APPROVED

## Accepted Plan

Phase 1B.3-B2 Workflow Admin Configuration UI Detailed Plan

## Commits

| Role | Hash |
|---|---|
| Accepted plan commit | c11fd40e795e7b82892d42e9cb02f4c1e7bf8694 |
| Accepted B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |
| Accepted B1 implementation commit | f1fafacad81879fa72ca607616e68b34b7024bab |
| Accepted Phase 1B.3-A plan acceptance commit | 54700b1af8c6e831a82fa2d8c90254932f3955a4 |

## Acceptance Baseline

c11fd40e795e7b82892d42e9cb02f4c1e7bf8694

---

## Project Owner Decision

The Project Owner accepts the Phase 1B.3-B2 Workflow Admin Configuration UI Detailed Plan.

---

## Approved Implementation Direction

Proceed next with Phase 1B.3-B2 Workflow Admin Configuration UI implementation as a frontend-only phase using existing B1 backend endpoints.

---

## Approved B2 Frontend Scope

- Workflow admin menu/navigation.
- Workflow definitions list/search.
- Workflow definition create/edit/detail.
- Workflow version list/detail.
- Workflow step configuration UI.
- Workflow approver rule configuration UI within existing backend endpoint limits.
- Workflow condition configuration UI within existing backend endpoint limits.
- Workflow binding/process assignment UI.
- Publish/activate/retire actions where supported by B1 backend endpoints.
- Sanitized error handling.
- rowVersion/concurrency UX.
- Version freeze warning/education in UI.
- Loading/empty/error states.
- Safe validation display.

---

## Approved Route/Page Direction

Use the routes and pages documented in the accepted B2 plan, including workflow admin routes for definitions, versions, and bindings.

---

## Approved Permission Gates

- WORKFLOW_VIEW for menu/read/list/detail.
- WORKFLOW_CONFIG_MANAGE for create/edit draft/configuration actions.
- WORKFLOW_PUBLISH for publish/activate/retire actions.
- WORKFLOW_BIND_PROCESS for binding/process assignment actions.
- WORKFLOW_AUDIT_VIEW only if audit read UI is included and supported.
- WORKFLOW_REASSIGN_PENDING remains deferred from B2 unless separately approved.
- Backend remains authoritative.
- DENY wins.
- Frontend gates are UX/navigation only.

---

## Accepted Endpoint Mapping

- Existing B1 configuration endpoints are sufficient for a frontend-only B2 subset.
- UI actions must map only to existing B1 backend endpoints.
- No unsupported endpoint may be called.
- No fake client-only mutation behavior is allowed.

---

## Accepted Endpoint Gaps and Limitations

- No DELETE approver rule endpoint exists.
- No POST/DELETE condition endpoint exists.
- Approver rule deletion is deferred unless a backend gap-resolution phase is separately approved.
- Condition create/delete is deferred unless a backend gap-resolution phase is separately approved.
- Condition UI must be read-only or limited to existing supported backend behavior.
- Full condition editor is not approved in B2 unless it can be implemented using existing B1 endpoints without backend changes.

---

## Accepted Deferred Scope

- My Approvals inbox remains deferred.
- Approve/reject/return/resubmit/withdraw runtime action UI remains deferred.
- Runtime requester UI remains deferred.
- Active instance migration UI remains deferred.
- Pilot integration with Customer/Service/Payment/Merge remains deferred.
- Service module implementation remains deferred.
- Payment/Reconciliation implementation remains deferred.
- Customer Merge implementation remains deferred.
- ENTITY scope remains deferred.
- Export/download remains deferred.
- Backend/API/database/migration changes are not approved by default.

---

## Accepted UX Behavior

- Show DRAFT/PUBLISHED/ACTIVE/RETIRED status labels clearly.
- Explain that active workflow instances use frozen version/snapshot.
- Do not imply active instances change route when configuration changes.
- Show warnings before publish/activate/retire.
- Use refresh prompt for 409 concurrency conflicts.
- Do not silently overwrite.
- Do not persist permissions in localStorage/sessionStorage/cookies.
- Do not log raw sensitive data or secrets.

---

## Accepted Test Strategy

- Route/menu tests.
- Permission gate tests.
- API client tests.
- Definition list/detail/create/edit tests.
- Version detail/status action tests.
- Step/rule/condition editor tests within supported backend endpoint limits.
- Binding page tests.
- Error handling tests.
- Concurrency tests.
- Deferred behavior tests confirming no My Approvals/runtime action UI.

---

## Accepted Open Decisions

- DEC-1B3B2 decisions are acknowledged.
- Any backend gap resolution requires a separately approved task.
- Any runtime UI or pilot integration requires a separately approved phase.

---

## Explicit Non-Authorization

- This acceptance does not implement code.
- This acceptance does not authorize backend changes.
- This acceptance does not authorize database changes.
- This acceptance does not authorize migrations or rollbacks.
- This acceptance does not authorize PermissionCodes.cs changes.
- This acceptance does not authorize permission-catalog.md changes.
- This acceptance does not authorize business-rules.md or acceptance-criteria.md changes.
- This acceptance does not authorize My Approvals/runtime UI.
- This acceptance does not authorize Service/Payment/Merge/ENTITY/Export implementation.
- This acceptance does not authorize production migration/release.

---

## Project Owner Acceptance

The Project Owner accepts Phase 1B.3-B2 Workflow Admin Configuration UI Detailed Plan.

---

## Next Recommended Step

Create the approved Phase 1B.3-B2 frontend-only implementation task.

PHASE 1B.3-B2 WORKFLOW ADMIN CONFIGURATION UI PLAN ACCEPTED — READY FOR FRONTEND-ONLY IMPLEMENTATION TASK
