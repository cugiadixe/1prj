# Phase 1B.3-B3 Workflow Runtime / My Approvals UI Project Owner Plan Acceptance

## Status

ACCEPTED — PHASE 1B.3-B3 WORKFLOW RUNTIME / MY APPROVALS UI PLAN APPROVED

## Accepted Plan

Phase 1B.3-B3 Workflow Runtime / My Approvals UI Detailed Plan

## Commits

| Role | Hash |
|---|---|
| Accepted plan commit | b3d1ff5740b8909e1ce6a7f198bac6a03483b2ee |
| Accepted B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |
| Accepted B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |
| Accepted Phase 1B.3-A plan acceptance commit | 54700b1af8c6e831a82fa2d8c90254932f3955a4 |

## Acceptance Baseline

b3d1ff5740b8909e1ce6a7f198bac6a03483b2ee

---

## Project Owner Decision

The Project Owner accepts the Phase 1B.3-B3 Workflow Runtime / My Approvals UI Detailed Plan.

## Approved Implementation Direction

Proceed next with Phase 1B.3-B3 Workflow Runtime / My Approvals UI implementation as a frontend-only phase using existing B1 runtime backend endpoints.

---

## Approved B3 Frontend Scope

- My Approvals menu/navigation.
- My Approvals inbox/list.
- Approval task detail.
- Instance detail using existing runtime endpoint support.
- Approve action UI using existing backend endpoint support.
- Return action UI using existing backend endpoint support.
- Resubmit action UI using existing backend endpoint support.
- Withdraw action UI using existing backend endpoint support.
- Reassignment UI only if supported by existing backend endpoint and gated by WORKFLOW_REASSIGN_PENDING.
- Runtime status badges.
- Safe payload/metadata display.
- Version/snapshot freeze notice.
- Sanitized error handling.
- Loading/empty/error states.
- Stale task/concurrency/refresh UX where backend contract supports it.

---

## Approved Endpoint Mapping

- Existing 7 runtime endpoints are sufficient for a frontend-only B3 subset.
- UI actions must map only to existing B1 runtime backend endpoints.
- No unsupported endpoint may be called.
- No fake client-only mutation behavior is allowed.

---

## Accepted Endpoint Gaps and Limitations

- No my-requests endpoint exists.
- No action history endpoint exists.
- No reject endpoint exists.
- My Requests list is deferred unless a backend gap-resolution phase is separately approved.
- Action history/timeline is deferred unless a backend gap-resolution phase is separately approved.
- Reject action is deferred unless a backend gap-resolution phase is separately approved.
- Generic workflow instance creation is deferred.
- Business-specific workflow start screens are deferred to pilot integration.
- Full pilot integration remains deferred to B4.

---

## Approved Permission and Authorization Strategy

- Runtime pages/actions must rely on backend service-layer authorization.
- Frontend must not grant approver/requester eligibility by permission alone.
- Frontend action buttons may be shown based on backend-returned assignment/requester state.
- Backend remains authoritative.
- Frontend gates are UX/navigation only.
- DENY-wins behavior is backend-enforced.
- WORKFLOW_REASSIGN_PENDING is used only for reassignment UI if reassignment is included.
- WORKFLOW_VIEW may be used for shared workflow navigation/instance detail only if consistent with the accepted plan and existing frontend design.

---

## Approved Safe Payload Strategy

- UI must display safe metadata/summary only.
- DTO does not expose PayloadJson/BeforeDataJson.
- Do not display raw sensitive payload JSON.
- Do not log raw sensitive data or secrets.
- Do not persist runtime eligibility or permissions in localStorage/sessionStorage/cookies.

---

## Approved Version/Snapshot Strategy

- Runtime UI must explain that the workflow instance uses the version/snapshot captured at instance creation.
- UI must not imply active instances change route after configuration changes.
- No active instance route mutation.
- No active instance migration UI.

---

## Approved Deferred Scope

- Pilot integration with Customer/Service/Payment/Merge remains deferred.
- Business-specific workflow start screens remain deferred.
- Generic workflow instance creation UI remains deferred unless separately approved.
- My Requests list remains deferred because no endpoint exists.
- Action history/timeline remains deferred because no endpoint exists.
- Reject action remains deferred because no endpoint exists.
- Active instance migration UI remains deferred.
- Service module implementation remains deferred.
- Payment/Reconciliation implementation remains deferred.
- Customer Merge implementation remains deferred.
- ENTITY scope remains deferred.
- Export/download remains deferred.
- Backend/API/database/migration changes are not approved by default.
- PermissionCodes.cs or permission catalog changes are not approved by default.

---

## Accepted UX Behavior

- Loading/empty/error states.
- Safe validation errors.
- Sanitized 403 handling.
- 404 handling.
- Stale task/concurrency refresh handling where applicable.
- Clear workflow instance and step status labels.
- Clear version/snapshot freeze explanation.
- Warnings before approve/return/resubmit/withdraw/reassign actions.
- No silent overwrite.
- No active instance route mutation.
- Safe payload display only.

---

## Accepted Test Strategy

- Route/menu tests.
- Runtime inbox tests.
- Assignment detail tests.
- Instance detail tests if included.
- Approve action tests.
- Return action tests.
- Resubmit action tests.
- Withdraw action tests.
- Reassignment tests if included.
- Permission/authorization UX tests.
- Backend-denial 403 tests.
- Stale task/concurrency refresh tests.
- Safe payload display tests.
- Deferred behavior tests confirming no pilot integration, no My Requests, no action history, no reject, and no admin migration UI.

---

## Accepted Open Decisions

- DEC-1B3B3 decisions are acknowledged.
- Any backend gap resolution requires a separately approved task.
- Any pilot integration requires a separately approved phase.
- Any Service/Payment/Merge/ENTITY/Export implementation requires a separately approved phase.

---

## Explicit Non-Authorization

- This acceptance does not implement code.
- This acceptance does not authorize backend changes.
- This acceptance does not authorize database changes.
- This acceptance does not authorize migrations or rollbacks.
- This acceptance does not authorize PermissionCodes.cs changes.
- This acceptance does not authorize permission-catalog.md changes.
- This acceptance does not authorize business-rules.md or acceptance-criteria.md changes.
- This acceptance does not authorize pilot integration.
- This acceptance does not authorize Service/Payment/Merge/ENTITY/Export implementation.
- This acceptance does not authorize production migration/release.

---

## Project Owner Acceptance

The Project Owner accepts Phase 1B.3-B3 Workflow Runtime / My Approvals UI Detailed Plan.

## Next Recommended Step

Create the approved Phase 1B.3-B3 frontend-only implementation task.
