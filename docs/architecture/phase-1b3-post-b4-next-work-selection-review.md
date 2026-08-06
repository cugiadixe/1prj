# Phase 1B.3 Post-B4 Next-Work Selection / Roadmap Review

## Status
PROPOSED — AWAITING PROJECT OWNER NEXT-WORK SELECTION
PROJECT OWNER SELECTED OPTION A — SEE phase-1b3-post-b4-project-owner-next-work-decision.md

## Current baseline
- Current HEAD:
  795ed4057881831c8a34efd4dc1cd5eeb0ed46dc
- Latest completed phase:
  Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot Integration
- Final B4 acceptance commit:
  795ed4057881831c8a34efd4dc1cd5eeb0ed46dc

## Completed capabilities summary
- Phase 1B.1 Security Admin completed.
- Phase 1B.2 Customer first slice completed.
- Phase 1B.3 Workflow/Approval:
  - B1 backend workflow foundation completed.
  - B2 workflow admin configuration UI completed.
  - B3 runtime/My Approvals UI completed.
  - B4 CREATE_CUSTOMER workflow pilot completed.

For B4:
- Workflow-backed customer creation proposal path exists.
- Existing direct customer create path preserved.
- Backend proposal API exists.
- CustomerChangeRequest persistence exists.
- Workflow instance linkage exists.
- Final approval execution handler exists.
- Idempotent final customer creation after approval exists.
- Frontend proposal create/status/my-proposals UX exists.
- Workflow instance link exists.
- Existing My Approvals is reused for approval actions.
- Safe metadata-only payload strategy is in place.

## Remaining deferred/open work summary
- My Requests UI/API.
- Action history/timeline UI/API.
- Reject UI/API.
- CUSTOMER_MASTER_CHANGE.
- Service module integration.
- Payment module integration.
- Merge flow.
- Card flow.
- Plot flow.
- ENTITY scope expansion.
- Export/download features.
- Production migration.
- Production release.
- Active instance migration.
- Operational execution retry UX hardening.
- User lookup/reassign UX improvements.

## Options matrix

### Option A — Workflow pilot hardening
**Summary:** Enhance the workflow engine and UI with features deferred during the pilot.
**Scope examples:** My Requests, Action history/timeline, Reject, Execution failure retry UX, Operational validation follow-up.
**Business value:** Completes the core workflow runtime experience and makes it robust for wider rollout.
**Dependencies:** Phase 1B.3-B4.
**Risks:** UI/API complexity for rejection and retry paths.
**Blockers:** None.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1B.3-B5 — Post-B4 Workflow Pilot Hardening.
**Out-of-scope boundaries:** New business modules (Service, Payment), CUSTOMER_MASTER_CHANGE.

### Option B — CUSTOMER_MASTER_CHANGE / customer data change workflow
**Summary:** Implement the workflow for changing existing customer master data.
**Scope examples:** Workflow-backed customer master data change request, Data admin review flow, Sensitive customer data governance.
**Business value:** Secures the governance of customer updates.
**Dependencies:** Phase 1B.3-B4.
**Risks:** Complex metadata diffing and safe payload handling for changes.
**Blockers:** Needs detailed discovery on which fields trigger workflow and how diffs are presented.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1B.3-C1 — Customer Master Change Workflow.
**Out-of-scope boundaries:** Service, Payment, Merge, Plot, Card flows.

### Option C — Customer merge / duplicate management
**Summary:** Handle duplicate customers via a merge proposal and audit flow.
**Scope examples:** Duplicate customer handling, Merge proposal and audit flow.
**Business value:** Improves customer data quality and single-view integrity.
**Dependencies:** Phase 1B.2 Customer first slice.
**Risks:** Merging related entities across multiple tables.
**Blockers:** Requires detailed business rules on merge logic.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1B.2-M — Customer Merge Management.
**Out-of-scope boundaries:** Service, Payment, unrelated workflow changes.

### Option D — Service module foundation
**Summary:** Begin implementation of the Service module.
**Scope examples:** Service catalog / service purchase foundation, Later link to workflow where needed.
**Business value:** Expands the system into core operational capabilities.
**Dependencies:** Customer module.
**Risks:** Large surface area.
**Blockers:** Requires full technical discovery and database schema plan.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1C.1 — Service Module Foundation.
**Out-of-scope boundaries:** Workflow enhancements, Payment module.

### Option E — Payment module foundation
**Summary:** Begin implementation of the Payment module.
**Scope examples:** Payment/bill/transaction foundation, Manual reconciliation support later.
**Business value:** Enables financial tracking and reconciliation.
**Dependencies:** Customer and Service modules (likely).
**Risks:** Financial data integrity requirements.
**Blockers:** Requires full technical discovery.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1D.1 — Payment Module Foundation.
**Out-of-scope boundaries:** Service module, workflow enhancements.

### Option F — Production migration/release preparation
**Summary:** Prepare the system for production rollout of the completed phases.
**Scope examples:** Deployment readiness, Migration release controls, Environment checklist.
**Business value:** Delivers the built features to actual users.
**Dependencies:** Completed Phase 1B features.
**Risks:** Environment differences, data migration issues.
**Blockers:** Requires operational validation of B4.
**Can start immediately?** No.
**Discovery/plan required?** Yes.
**Recommended next phase name:** Phase 1B Release Readiness.
**Out-of-scope boundaries:** Any new feature development.

## Recommended next work item
**Recommended option:** Option A — Workflow pilot hardening
**Recommended phase code/name:** Phase 1B.3-B5 — Post-B4 Workflow Pilot Hardening Discovery and Detailed Plan
**Why this option is recommended now:** The B4 CREATE_CUSTOMER pilot implemented a "happy path" workflow. Core features like rejection, action history, and 'My Requests' visibility were deferred. Hardening these features ensures the workflow engine is fully mature and usable before attaching more complex processes like CUSTOMER_MASTER_CHANGE or Service/Payment approvals.
**Why other options are not selected now:** Starting a new massive domain (Service, Payment) or a complex workflow (CUSTOMER_MASTER_CHANGE) on top of an incomplete workflow foundation introduces technical debt and limits the pilot's effectiveness.
**Required decisions before implementation:**
- Exact scope of hardening (e.g., Reject vs Retry UX).
- API designs for history and My Requests.
**Required documents to create next:**
- `docs/architecture/phase-1b3b5-workflow-hardening-discovery-and-detailed-plan.md`
**Stop conditions:**
- Stop if scope expands beyond workflow hardening (e.g., adding CUSTOMER_MASTER_CHANGE).

## Required Project Owner decision
- Select next work option.
- Confirm phase name.
- Confirm whether next step is discovery/plan or implementation.
- Confirm explicit out-of-scope items.
- Confirm whether any permission catalog/business rules/acceptance criteria updates are authorized.

## Recommended stop conditions
- Stop if source code changes are needed before selection.
- Stop if business rules are missing.
- Stop if permission codes are missing.
- Stop if database scope requires approval.
- Stop if workflow process binding is unclear.
- Stop if production release is requested without release readiness review.

## Conclusion
POST-B4 NEXT-WORK SELECTION REVIEW READY FOR PROJECT OWNER DECISION
