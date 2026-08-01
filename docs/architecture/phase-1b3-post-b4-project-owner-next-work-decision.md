# Phase 1B.3 Post-B4 Project Owner Next-Work Decision

## Status
ACCEPTED — NEXT WORK SELECTED

## Decision baseline
- Current HEAD:
  242dcdbc24acc4626ff8400ed00a0a1197b88fa3
- Post-B4 next-work selection review commit:
  242dcdbc24acc4626ff8400ed00a0a1197b88fa3
- Final B4 acceptance commit:
  795ed4057881831c8a34efd4dc1cd5eeb0ed46dc
- Latest completed phase:
  Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot Integration

## Project Owner decision
The Project Owner selects Option A — Workflow pilot hardening as the next work item.

## Selected next phase
Phase 1B.3-B5 — Post-B4 Workflow Pilot Hardening Discovery and Detailed Plan

## Authorization
This decision authorizes discovery and detailed planning for Phase 1B.3-B5 only.

## Authorized B5 discovery/planning scope
- Review workflow runtime gaps after B4 pilot.
- Review My Requests requirement.
- Review action history/timeline requirement.
- Review reject behavior requirement.
- Review execution failure retry UX hardening.
- Review operational validation follow-up from CREATE_CUSTOMER pilot.
- Review user lookup/reassign UX improvements.
- Identify whether backend changes are needed.
- Identify whether frontend changes are needed.
- Identify whether database changes are needed.
- Identify whether new permission codes are needed.
- Identify whether business-rules.md updates are needed.
- Identify whether permission-catalog.md updates are needed.
- Identify whether acceptance-criteria.md updates are needed.
- Produce a detailed B5 implementation plan for Project Owner approval.

## Not authorized yet
- Source code implementation.
- Backend implementation.
- Frontend implementation.
- Test implementation.
- Migrations.
- Rollbacks.
- Database scripts.
- PermissionCodes.cs changes.
- permission-catalog.md changes.
- business-rules.md changes.
- acceptance-criteria.md changes.
- Production migration/release.
- Service module implementation.
- Payment module implementation.
- CUSTOMER_MASTER_CHANGE implementation.
- Merge flow implementation.
- Card flow implementation.
- Plot flow implementation.
- ENTITY scope expansion.

## Reason for selecting Option A
- B4 introduced the first workflow-backed business pilot.
- My Requests, action history/timeline, reject, execution retry UX, and operational hardening remain deferred.
- Hardening the workflow pilot before expanding to new business modules reduces risk.
- The next step needs discovery and detailed planning before implementation.

## Options not selected now
- Option B CUSTOMER_MASTER_CHANGE is deferred until workflow pilot hardening decisions are clarified.
- Option C Customer merge / duplicate management is deferred.
- Option D Service module foundation is deferred.
- Option E Payment module foundation is deferred.
- Option F Production migration/release preparation is deferred.
- Other business module expansions are deferred.

## Required B5 deliverable
Create:
docs/architecture/phase-1b3b5-workflow-pilot-hardening-discovery-and-detailed-plan.md

B5 plan must include:
- Current workflow runtime gap analysis.
- My Requests decision proposal.
- Action history/timeline decision proposal.
- Reject behavior decision proposal.
- Execution failure retry UX proposal.
- Operational validation checklist.
- User lookup/reassign UX proposal.
- Backend impact analysis.
- Frontend impact analysis.
- Database impact analysis.
- Permission impact analysis.
- Business rules impact analysis.
- Acceptance criteria impact analysis.
- Proposed implementation phases.
- Required tests.
- Risks and stop conditions.
- Explicit non-scope.
- Project Owner approval checklist.

## Stop conditions
- Stop if source code changes are needed before the B5 plan is accepted.
- Stop if business rules are missing.
- Stop if permission codes are missing.
- Stop if database scope requires approval.
- Stop if workflow runtime behavior is ambiguous.
- Stop if production release is requested without release readiness review.
- Stop if implementation is attempted before Project Owner accepts the B5 plan.

## Conclusion
PHASE 1B.3-B5 WORKFLOW PILOT HARDENING DISCOVERY AND DETAILED PLAN SELECTED
