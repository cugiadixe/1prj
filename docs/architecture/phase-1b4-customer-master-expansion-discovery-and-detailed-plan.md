# Phase 1B.4 Customer Master Expansion Discovery and Detailed Plan

## Status

PROPOSED — AWAITING PROJECT OWNER PLAN APPROVAL
PHASE 1B.4 PLAN ACCEPTED — SEE phase-1b4-project-owner-plan-acceptance.md

## Planning Baseline

- Post-B5 Project Owner next-work decision commit:
  420f76df3d37218c47d98168923b5fa559fc78d9
- Post-B5 next-work selection commit:
  d52701cecb0174b1c2ed023c487b532abbaa0822
- B5-D Project Owner closure acceptance commit:
  0a4149fb233c516210acba197a8b2977cbc39170
- B5-C Project Owner frontend acceptance commit:
  39760a9cbee6fe6f352b4336423b89a8b2149086
- B5-B Project Owner backend acceptance commit:
  c42734e351404d9788b82e2049c92f6de09baf18

## Purpose

The purpose of Phase 1B.4 Customer Master Expansion is to build upon the hardened B5 workflow engine to implement the `CUSTOMER_MASTER_CHANGE` business process. It enables staff to securely propose updates to existing customer master data (including critical/protected fields) through an approval workflow, ensuring strict data governance, concurrency control, and auditability without granting broad direct-update permissions.

## Source Documents Reviewed

- `docs/business/process-catalog.md`
- `docs/business/acceptance-criteria.md`
- `docs/business/business-rules.md`
- `docs/architecture/phase-1b3-post-b5-project-owner-next-work-decision.md`
- `docs/architecture/phase-1b3-post-b5-next-work-selection-and-recommendation.md`

## Confirmed Business Scope

- **Customer master data governance**: Strict separation between ordinary staff (who can only propose changes) and data-admin groups (who hold final approval and update authority).
- **Protected/critical customer fields**: Staff cannot directly edit core fields like `full_name`, `cccd`, `dob`, `phone`, or `contact_address` without approval.
- **Staff proposal vs. data-admin official update model**: Staff submit a `CUSTOMER_MASTER_CHANGE` request containing the desired changes (before/after snapshot); a designated approver (often `GROUP_CUSTOMER_DATA_ADMIN`) reviews and applies the changes.
- **Shared customer data across companies**: Customer master records are shared globally, but `Customer_Company_Context` tracks company-specific relations.
- **Customer creation/change proposal needs**: Requires a reliable mechanism to compare before/after data and capture the target record version.
- **Workflow approval relevance**: `CUSTOMER_MASTER_CHANGE` relies entirely on the robust approval engine hardened in Phase 1B.3.
- **Audit/security expectations**: Full audit trail of the before/after state, sensitive field redaction, and strict permission enforcement.

## Explicit Non-Scope

Phase 1B.4 explicitly does not include:
- Payment module implementation.
- Service module implementation.
- Card print/reprint flow.
- Plot/cemetery location module.
- ENTITY permission expansion.
- Export/download capability.
- Production release.
- Safe user lookup/reassign (unless required only as a documented dependency).
- Customer merge implementation (`CUSTOMER_MERGE_DUPLICATE`), unless separately approved by the Project Owner.
- Any business rule not supported by existing documentation.

## Decisions Already Approved

- The B5 workflow engine is fully mature and serves as the foundation for this expansion.
- `CUSTOMER_MASTER_CHANGE` is the selected next work.
- `CUSTOMER_MASTER_CHANGE` requires a target `rowversion` and before/after snapshot (from `process-catalog.md`).
- Target-version conflict must not overwrite newer data (CUS-04).
- Direct administrator correction requires a reason and before/after audit (CUS-05).

## Decisions Still Missing

- Exact `CUSTOMER_MASTER_CHANGE` workflow trigger conditions.
- Final, confirmed list of protected/critical fields.
- Whether Customer Merge (`CUSTOMER_MERGE_DUPLICATE`) is discovery-only or fully included in 1B.4 implementation.
- Whether duplicate detection on update is a read-only warning or a blocking rule.
- Exact approval flow assignment (e.g., specific rules for `GROUP_CUSTOMER_DATA_ADMIN`).
- Required permission codes beyond the baseline for submitting and approving these changes.
- Exact audit fields required in the history log.
- Specific UI screens required for before/after diff visualization.
- Exact migration boundaries for any new change-request tables.

## Proposed Functional Scope

- **Customer master change request/proposal**: Allow staff to initiate a `CUSTOMER_MASTER_CHANGE` request, capturing the target customer ID and `rowversion`.
- **Customer master change approval workflow**: Bind the request to the approval engine, routing it to the appropriate data-admin or reviewer.
- **Customer data admin official update action**: Execution handler (`CUSTOMER_UPDATE_FROM_APPROVAL`) that safely applies the approved changes if the `rowversion` matches.
- **My Requests / Action History reuse**: Leverage existing UI components for users to track their change proposals.
- **Rejection and retry behavior reuse**: Utilize B5 logic for handling rejected proposals and retrying failed execution handlers (e.g., concurrency conflicts).
- **Customer proposal detail/history view**: A unified view for reviewers to see the context of the change request.
- **Safe display of before/after values**: A UI mechanism (diff view) to clearly show what fields are being changed.
- **Audit trail**: Comprehensive logging of the final applied update.

## Proposed Database Impact

- **Existing customer tables to inspect**: `Profiles`, `Customers`, `Customer_Company_Context`.
- **New proposal/change tables**: Determine if a dedicated `Customer_Change_Requests` table is needed or if the generic `Approval_Requests` with a JSON payload is sufficient.
- **Workflow instance linkage**: Use `Approval_Requests` linked to the customer entity.
- **Before/after data storage rules**: Store the exact state at the time of proposal (before) and the requested state (after).
- **Sensitive field protection**: Ensure JSON payloads do not leak unmasked sensitive data to unauthorized viewers.
- **Rowversion/concurrency**: Rely on `rowversion` on the `Customers`/`Profiles` tables.
- **Audit strategy**: Utilize standard audit tables for the final applied change.
- **Migration/rollback strategy**: Versioned SQL scripts for schema adjustments.

## Proposed API v2 Impact

- **Create customer master change request**: `POST /api/v2/customers/{id}/change-requests`.
- **List my customer change requests**: Inherited from B5 or specialized endpoint `GET /api/v2/customers/my-change-requests`.
- **Get customer change request detail**: `GET /api/v2/customers/change-requests/{requestId}`.
- **Submit/start workflow**: Triggered implicitly or explicitly.
- **Approve/reject through workflow runtime**: Reuse B5 endpoints.
- **Data-admin finalize/apply approved change**: Internal handler invoked by the approval engine.
- **Duplicate detection endpoint**: Expose an API for frontend warnings if supported.
- **Permission enforcement**: Ensure endpoints validate `CUSTOMER_CHANGE_REQUEST_CREATE` and company boundaries.
- **Sanitized errors**: Return standard Problem Details.

## Proposed Frontend Impact

- **Customer master change proposal screen**: Form allowing staff to edit fields they propose changing.
- **Customer change detail screen**: For reviewers to see the before/after diff.
- **Protected fields UX**: Visual indicators of fields that require approval versus those that do not (if any).
- **Duplicate warning UX**: If supported, warn staff before submitting a proposal that creates a duplicate CCCD.
- **Workflow status display**: Reuse B5 components.
- **Action history reuse**: Reuse B5 components.
- **Admin/data-admin apply screen**: Standard approval action panel.
- **Permission-aware navigation**: Hide links if the user lacks the proposal permission.
- **Sanitized errors**: Graceful error handling for concurrency issues (HTTP 409).

## Proposed Workflow / Approval Impact

- **CUSTOMER_MASTER_CHANGE workflow**: Register this process code and its execution handler.
- **Reuse of B5 workflow hardening**: Full reliance on B5 for routing, step resolution, and history.
- **Pending/approved/rejected/failed status behavior**: Standard B5 state machine.
- **Reject semantics**: A rejected change request is permanently closed and does not alter official customer data.
- **Retry semantics**: If the execution handler fails (e.g., due to a `rowversion` mismatch), it enters a `FAILED` state and can be retried idempotently if the underlying data issue is resolved.
- **Mid-process workflow definition versioning**: Inherit B5 rules (running instances keep their version).
- **Delegation considerations**: Supported via existing B5 delegation logic.

## Proposed Permission and Security Impact

- **Required permission codes**: `CUSTOMER_CHANGE_REQUEST_CREATE` for submission, `GROUP_CUSTOMER_DATA_ADMIN` (or equivalent) for approval.
- **Data-admin group authority**: Only this group or explicitly assigned approvers can finalize the change.
- **Staff request rights**: Ordinary staff cannot bypass the workflow.
- **Reviewer/approver rights**: Only the assigned assignee/delegate can act.
- **Company scope rules**: Enforce company context where applicable.
- **No super-admin bypass**: Even administrators must use the designated data-admin correction flow (CUS-05) if direct bypass is restricted.
- **Backend-authoritative enforcement**: All checks must happen server-side.
- **Sensitive field redaction**: Protect data at rest and in transit.
- **Audit requirements**: Capture `acted_by`, `correlation_id`, and reason.

## Proposed Test Strategy

- **Backend unit tests**: Domain logic for change request creation and conflict detection.
- **Integration tests**: Database transactions, concurrency (rowversion) testing, and execution handler testing.
- **API tests**: End-to-end endpoint tests for authorization and validation.
- **Frontend tests**: UI logic for diff display and proposal form.
- **Permission/security tests**: Verify staff cannot update directly; unauthorized users cannot propose.
- **Migration/rollback tests**: Test schema upgrades/downgrades if any.
- **Workflow runtime tests**: End-to-end execution of `CUSTOMER_MASTER_CHANGE`.
- **Regression tests**: Ensure existing customer creation screens remain unaffected.
- **Git diff/check hygiene**: Strict branch and commit hygiene.

## Proposed Manual Validation Strategy

- Staff creates a change request altering a protected field (e.g., phone).
- Verify the change is not applied immediately.
- Attempt unauthorized direct API update (should be blocked).
- Approver reviews the before/after diff and approves.
- Data-admin execution handler applies the approved change.
- Verify audit and action history are visible.
- Verify rejected changes do not alter official data.
- Test duplicate warning behavior during proposal (if included).

## Recommended Implementation Phases

- **1B.4-A Detailed implementation plan and PO acceptance**: This current phase.
- **1B.4-B Backend/data foundation**: Schema, models, execution handlers, and API endpoints.
- **1B.4-C Frontend implementation**: UI screens for proposal and diff review.
- **1B.4-D Operational validation and closure**: End-to-end testing and formal sign-off.

## Risks and Blockers

- **Unclear customer field protection rules**: Needs definitive Project Owner clarification.
- **Duplicate/merge scope ambiguity**: Needs a decision on whether merge is in-scope or deferred.
- **Before/after sensitive data exposure risk**: Storing full snapshots might leak sensitive fields if the approval request is accessible to unauthorized users.
- **Workflow definition versioning implications**: Must ensure `CUSTOMER_MASTER_CHANGE` does not expose edge cases in the engine.
- **Audit completeness**: Guaranteeing the final update log correlates precisely with the approval request.
- **Permission-code scope**: Potential gap in existing codes for specific field-level approvals.
- **Migration risk**: Schema changes to accommodate proposal storage.
- **Test data limitations**: Need robust scenarios for concurrency conflicts.

## Stop Conditions

Stop implementation later if:
- An unsupported business requirement is mandated mid-flight.
- Protected field rules remain ambiguous and block development.
- Migration design cannot be safely rolled back.
- Sensitive data exposure is detected in the proposal payload.
- Workflow semantics severely conflict with B5 principles.
- Permissions cannot be reliably enforced backend-side.
- A critical Project Owner decision is missing.

## Recommendation

Recommend whether Phase 1B.4 should proceed to Project Owner plan acceptance. Phase 1B.4 is highly recommended to proceed to Project Owner plan acceptance. It builds naturally on the hardened B5 workflow engine and delivers critical business value by enabling secure, governed updates to central customer data.

## Conclusion

PHASE 1B.4 CUSTOMER MASTER EXPANSION PLAN PROPOSED — AWAITING PROJECT OWNER APPROVAL
