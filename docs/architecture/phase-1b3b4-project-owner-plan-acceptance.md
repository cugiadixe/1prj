# Phase 1B.3-B4 Workflow Pilot Integration Project Owner Plan Acceptance

## Status

ACCEPTED — PHASE 1B.3-B4 WORKFLOW PILOT INTEGRATION PLAN APPROVED

## Accepted Plan

Phase 1B.3-B4 — Workflow Pilot Integration (CREATE_CUSTOMER)

## Plan Acceptance Baseline

93607eb57c4a4aee3f2dd0ecba8a00135f3db87e

## Accepted Plan Commits

| Role | Hash |
|---|---|
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |
| B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |
| B3 final acceptance commit | bd451869b83dd9716422bdcc53d3f628c363232e |
| B4 plan commit | 93607eb57c4a4aee3f2dd0ecba8a00135f3db87e |

---

## Project Owner Decision

The Project Owner accepts the Phase 1B.3-B4 Workflow Pilot Integration plan with CREATE_CUSTOMER as the selected pilot process.

---

## Selected Pilot

CREATE_CUSTOMER — Tạo khách hàng mới.

Rationale:
- ProcessCode CREATE_CUSTOMER is seeded, active, and approval-required in Business_Process_Catalog.
- Customer backend CRUD exists (CustomersController).
- Customer frontend CRUD exists (CustomerCreatePage, CustomerDetailPage, CustomerEditPage).
- Workflow engine is complete (B1 + B2 + B3).
- CUS-002 explicitly requires staff to create CREATE_CUSTOMER requests.
- CUSTOMER_CHANGE_REQUEST_CREATE permission is already cataloged.
- Lowest complexity among available candidates.
- Validates the full workflow engine end-to-end.

---

## Resolved Decisions

### DEC-1B3B4-01: Pilot Process Selection

**Decision**: CREATE_CUSTOMER selected as the pilot process.

CREATE_CUSTOMER only. CUSTOMER_MASTER_CHANGE deferred to a second pilot after CREATE_CUSTOMER succeeds.

### DEC-1B3B4-02: Backend Changes Allowed

**Decision**: Yes — backend changes are allowed for the limited pilot integration.

B4 is NOT frontend-only. Backend integration is required to connect customer creation to the workflow engine. Scope is limited to:
- Proposal submission endpoint or flow.
- Execution handler for approved instances.
- Permission wiring for CUSTOMER_CHANGE_REQUEST_CREATE.

### DEC-1B3B4-03: Database/Migration Allowed

**Decision**: Yes — database/migration changes are allowed if required.

Prefer minimal schema changes. Use existing WorkflowInstance data for linkage where possible. If a new table or column is required for proposal staging or linkage, it is permitted.

### DEC-1B3B4-04: Permission Catalog Updates

**Decision**: Not allowed unless separately approved.

CUSTOMER_CHANGE_REQUEST_CREATE already exists in the permission catalog. Wiring it to PermissionCodes.cs and the proposal endpoint is allowed. Adding new permission codes to permission-catalog.md is not authorized by this acceptance.

### DEC-1B3B4-05: Business-Specific Scope

**Decision**: CREATE_CUSTOMER only.

B4 implementation is scoped exclusively to the CREATE_CUSTOMER business process. No other business processes (CUSTOMER_MASTER_CHANGE, Service, Payment, Card, Plot, Merge) are authorized.

### DEC-1B3B4-06: Workflow Status Derivation

**Decision**: Workflow status must be explicitly derived or stored, no ambiguous state.

Preferred approach: derive workflow status by querying WorkflowInstance by processCode=CREATE_CUSTOMER + businessEntityId (no schema change, single source of truth). If derivation proves inadequate during implementation, a denormalized status column may be proposed with separate approval.

### DEC-1B3B4-07: Payload Summary Strategy

**Decision**: Metadata-only payload summary.

Frontend displays only safe metadata: customer name, customer type, proposed fields list. No raw PayloadJson or BeforeDataJson display. No sensitive data (CCCD, phone, DOB) in summary display. Detailed safe-field rules to be defined during implementation.

### DEC-1B3B4-08: My Requests / Action History / Reject

**Decision**: Deferred.

My Requests (backend GAP-1), action history/timeline (backend GAP-2), and reject action (backend GAP-3) remain deferred to a separate gap-resolution phase. B4 does not resolve these gaps.

### DEC-1B3B4-09: Production Migration

**Decision**: Deferred.

B4 is development/sandbox only. Production migration/release is not authorized by this acceptance and requires separate approval.

### DEC-1B3B4-10: Pilot Visibility

**Decision**: Internal/limited pilot.

B4 is development validation only. Not deployed to production or made user-visible outside development/testing environments.

### DEC-1B3B4-11: Direct-Create Coexistence

**Decision**: Direct-create coexistence must be defined before implementation.

Authorized data admins with CUSTOMER_CREATE_FINAL continue to use the direct creation path (POST /customers). Staff with CUSTOMER_CHANGE_REQUEST_CREATE but without CUSTOMER_CREATE_FINAL use the approval workflow path. Both paths must coexist correctly. The implementation must define and document the coexistence rules before coding begins.

### DEC-1B3B4-12: Execution Handler Required

**Decision**: Yes — execution handler is required.

When all approval steps pass, the backend must auto-execute final customer creation from the approved payload (APR-008/APR-009). The execution handler must be idempotent, handle errors, and use correlation_id tracking.

### DEC-1B3B4-13: Entity-to-Instance Linkage

**Decision**: Required — must be implemented or justified.

The customer detail/list UI must be able to determine if a pending workflow instance exists for a proposed customer. Preferred approach: query WorkflowInstance by processCode + businessEntityType + businessEntityId (no schema change). If this proves insufficient, alternative linkage (column or table) may be proposed during implementation.

---

## Accepted Backend Integration Scope

- Proposal submission path for CREATE_CUSTOMER (endpoint or modified flow).
- Execution handler for auto-creating customer after final approval.
- CUSTOMER_CHANGE_REQUEST_CREATE wired to PermissionCodes.cs and proposal endpoint.
- Idempotent execution (APR-009).
- Duplicate checking at proposal submission and execution (CUS-005).
- target_version conflict handling on execution (CUS-009).
- Backend remains authoritative for all authorization and validation.

## Accepted Frontend Integration Scope

- CustomerCreatePage: "Submit for Approval" path for staff with CUSTOMER_CHANGE_REQUEST_CREATE but without CUSTOMER_CREATE_FINAL.
- Customer detail/list: workflow status display for proposed customers.
- Navigation link from customer workflow status to /workflow/instances/:instanceId.
- Safe metadata summary display (no raw PayloadJson).
- Reuse existing B3 My Approvals inbox and instance detail pages.
- Reuse existing B2 admin UI for workflow configuration.

## Accepted Permission Strategy

- CUSTOMER_CHANGE_REQUEST_CREATE gates proposal submission.
- CUSTOMER_CREATE_FINAL gates direct creation (bypass workflow).
- Workflow approval actions remain service-layer authorized.
- WORKFLOW_REASSIGN_PENDING gates reassignment.
- WORKFLOW_VIEW gates instance detail view.
- Backend remains authoritative. DENY wins.
- No new permission codes added to permission-catalog.md.

## Accepted Safe Payload Strategy

- No raw PayloadJson display.
- No raw BeforeDataJson display.
- Metadata-only summary: customer name, type, proposed fields list.
- No sensitive data (CCCD, phone, DOB) in summary.
- Safe-field rules to be defined during implementation.

## Accepted Version/Snapshot Strategy

- Existing B3 version/snapshot freeze notice applies.
- Workflow instances retain original version at creation time.
- No active instance migration UI.

## Accepted Test Strategy

- Customer proposal submission tests.
- Workflow instance creation from proposal tests.
- Permission gate tests (CUSTOMER_CHANGE_REQUEST_CREATE vs CUSTOMER_CREATE_FINAL).
- Customer detail workflow status display tests.
- Navigation from customer to workflow instance detail tests.
- Safe payload summary tests.
- Concurrency/stale data tests.
- Deferred behavior tests (no My Requests, no action history, no reject).
- Regression tests for B2 admin UI and B3 My Approvals.
- End-to-end flow: propose, approve, customer created.
- Direct-create coexistence tests.

## Accepted Non-Scope

- CUSTOMER_MASTER_CHANGE pilot.
- My Requests UI (backend GAP-1).
- Action history/timeline UI (backend GAP-2).
- Reject action UI (backend GAP-3).
- Active instance migration UI.
- Service/Payment/Card/Plot/Merge module implementation.
- Delegation implementation.
- SLA/reminder implementation.
- Condition evaluation at bind/submit time.
- Generic workflow instance creation UI.
- Production migration/release.
- New permission codes in permission-catalog.md.
- business-rules.md or acceptance-criteria.md changes.
- Broad workflow engine redesign.

---

## Accepted Risks

1. B4 requires backend integration — not frontend-only.
2. Customer proposal payload contains personal data — safe summary rules must be enforced.
3. Proposed customer exists as workflow instance but not yet as Customer record — UI must distinguish.
4. Execution handler must be idempotent with error recovery.
5. Direct-create and workflow paths must coexist.
6. Active instance migration remains deferred.
7. Duplicate checking required at both proposal and execution.
8. Backend remains authoritative.
9. Production release remains separately controlled.

---

## Plan Acceptance Conclusion

Phase 1B.3-B4 Workflow Pilot Integration plan is accepted with CREATE_CUSTOMER as the selected pilot.
Implementation may proceed after this acceptance is committed.
