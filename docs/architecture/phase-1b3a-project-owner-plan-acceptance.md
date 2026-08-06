# Phase 1B.3-A Workflow/Approval Engine Project Owner Plan Acceptance

**Status:**
ACCEPTED — PHASE 1B.3-A WORKFLOW/APPROVAL ENGINE PLAN APPROVED

**Accepted plan:**
Phase 1B.3-A Workflow/Approval Engine Discovery and Detailed Plan

**Accepted plan commit:**
171e9310ade9e9f5ade7b15d940a8f8de8da99a2

**Accepted Phase 1B.3 selection acceptance commit:**
ffae4a919f23ec7d13980cf7ae11351c54c27536

**Accepted Customer first slice completion acceptance commit:**
2f4c059dd7f5f91aa14f6f5560fc360808049668

**Acceptance baseline:**
171e9310ade9e9f5ade7b15d940a8f8de8da99a2

---

## Project Owner decision

The Project Owner accepts the Phase 1B.3-A Workflow/Approval Engine Discovery and Detailed Plan.

---

## Approved implementation direction

Proceed next with Phase 1B.3-B1 Workflow Backend Foundation under the accepted plan.

---

## Accepted plan findings

- Workflow engine is greenfield.
- No existing workflow runtime exists.
- No existing approval UI exists.
- Workflow/approval was deferred from previous Customer first slice.
- Multiple future modules may depend on workflow.
- Workflow should be designed before Service implementation to reduce hardcoding risk.

---

## Accepted workflow design direction

- Dynamic approval flows configurable by admin.
- Approval flow definitions support versioning.
- Approval flows can be assigned to business process types.
- Process-flow assignment may support company scope as planned.
- Workflow instance must freeze workflow definition version/snapshot at instance creation.
- Workflow definition changes apply only to new workflow instances by default.
- Active instance migration requires explicit admin action and separate audit.
- Existing active instances must not silently change approval route.
- Backend remains authoritative.
- DENY wins.

---

## Accepted proposed design areas

- Domain model.
- Application services.
- API v2.
- Persistence.
- Audit.
- Permissions.
- Frontend.
- Tests.
- Integration strategy.
- In-progress versioning strategy.

---

## Accepted database planning direction

- Proposed database design is accepted as planning direction only.
- Proposed workflow tables remain subject to implementation review.
- Future implementation must include migration and rollback.
- No migration is created or approved in this acceptance commit.
- No production auto-migration is authorized.

---

## Accepted API planning direction

- Separate workflow configuration APIs from runtime approval APIs.
- API v2 must follow existing controller/application service patterns.
- Authorization must remain backend-enforced.
- Sanitized error handling must be preserved.
- Concurrency validation must be applied where required.

---

## Accepted frontend planning direction

- Future workflow admin UI may be planned separately.
- Future My Approvals UI may be planned separately.
- Frontend permission gates are UX/navigation only.
- No approval UI is implemented by this acceptance.

---

## Accepted permission planning direction

- Existing workflow permissions verified in permission-catalog.md may be used where applicable:
  - WORKFLOW_VIEW
  - WORKFLOW_CONFIG_MANAGE
  - WORKFLOW_PUBLISH
  - WORKFLOW_BIND_PROCESS
  - WORKFLOW_REASSIGN_PENDING
  - WORKFLOW_AUDIT_VIEW
- Delegation-related permissions verified in permission-catalog.md may be considered where applicable:
  - DELEGATION_CREATE
  - DELEGATION_ACTIVATE
- No PermissionCodes.cs change is approved in this acceptance task.
- No permission-catalog.md change is approved in this acceptance task.
- Any missing or new workflow permission code requires separate approval before implementation.

---

## Accepted audit direction

- Configuration changes must be audited.
- Runtime approval actions must be audited.
- Workflow audit must link to business entities where applicable.
- No secrets or raw sensitive data should be written to audit.

---

## Accepted testing direction

- Backend unit tests.
- Backend integration/API tests.
- Migration/rollback tests for future implementation.
- Frontend tests for future UI phases.
- Permission and DENY-wins tests.
- Audit tests.
- Version-change and in-progress instance tests.
- Concurrency tests.

---

## Accepted implementation sequencing

- Phase 1B.3-B1 Workflow Backend Foundation.
- Phase 1B.3-B2 Workflow Admin Configuration UI.
- Phase 1B.3-B3 Workflow Runtime / My Approvals UI.
- Phase 1B.3-B4 Pilot Integration with selected business process.

---

## Authorized next task

Phase 1B.3-B1 Workflow Backend Foundation implementation planning or implementation task may be created next under the accepted plan.

---

## Constraints for next implementation

- Stay within accepted B1 backend foundation scope.
- Do not implement Service module.
- Do not implement Payment/Reconciliation.
- Do not implement Customer Merge.
- Do not implement ENTITY scope.
- Do not implement Export/download.
- Do not implement production migration/release.
- Do not modify completed Customer first slice unless explicitly required and separately approved.
- Use existing workflow permissions only if already present in PermissionCodes.cs or add only after explicit approval if missing in code.
- Permission catalog changes are not approved by this acceptance.

---

## Accepted open decisions

- DEC-1B3A-01 through DEC-1B3A-18 are acknowledged.
- Decisions not resolved by this acceptance remain open and must be resolved before or during the specific implementation phase where they become blocking.
- Any pilot business process decision must be explicitly approved before pilot integration.

---

## Explicit non-authorization

- This acceptance does not implement code.
- This acceptance does not authorize frontend implementation.
- This acceptance does not authorize approval UI implementation.
- This acceptance does not authorize Service implementation.
- This acceptance does not authorize Payment/Reconciliation implementation.
- This acceptance does not authorize Customer Merge implementation.
- This acceptance does not authorize ENTITY scope implementation.
- This acceptance does not authorize Export/download implementation.
- This acceptance does not authorize production migration or release.
- This acceptance does not authorize automatic production migration.
- This acceptance does not modify PermissionCodes.cs.
- This acceptance does not modify permission-catalog.md.
- This acceptance does not modify business-rules.md.
- This acceptance does not modify acceptance-criteria.md.

---

## Project Owner acceptance

The Project Owner accepts Phase 1B.3-A Workflow/Approval Engine Discovery and Detailed Plan.

---

## Next recommended step

Create the approved Phase 1B.3-B1 Workflow Backend Foundation task.

PHASE 1B.3-A WORKFLOW/APPROVAL ENGINE PLAN ACCEPTED — READY FOR B1 BACKEND FOUNDATION TASK
