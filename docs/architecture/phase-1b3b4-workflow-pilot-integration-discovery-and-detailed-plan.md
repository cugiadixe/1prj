# Phase 1B.3-B4 Workflow Pilot Integration Discovery and Detailed Plan

## Status

PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

## Baseline

bd451869b83dd9716422bdcc53d3f628c363232e

## Authorization and Context

| Role | Hash |
|---|---|
| Phase 1B.3-A plan acceptance commit | 54700b1af8c6e831a82fa2d8c90254932f3955a4 |
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |
| B2 final acceptance commit | 009b3d276b2255c88e8b4a165de5ecfe09927186 |
| B3 final acceptance commit | bd451869b83dd9716422bdcc53d3f628c363232e |

This document is discovery and detailed planning only. No implementation is authorized.

---

## Confirmed Current State

- Workflow Backend Foundation complete (B1) — 8 runtime endpoints, configuration endpoints, workflow schema.
- Workflow Admin Configuration UI complete (B2) — definition/version/binding CRUD.
- Workflow Runtime / My Approvals UI complete (B3) — inbox, detail, approve/return/resubmit/withdraw/reassign.
- Pilot integration not implemented.
- Generic workflow instance creation not implemented in frontend (backend POST /workflows/instances exists).
- Business-specific workflow start screens not implemented.
- Pilot business process undecided by Project Owner.
- No implementation is authorized by this plan.

---

## Business Process Catalog Inventory

The database migration V0006 seeds exactly 2 process codes:

| ProcessCode | ProcessName | IsApprovalRequired | IsActive |
|---|---|---|---|
| CREATE_CUSTOMER | Tạo khách hàng mới | Yes | Yes |
| CUSTOMER_MASTER_CHANGE | Thay đổi thông tin khách hàng | Yes | Yes |

No other process codes exist. Service, Payment, Card Reprint, Change Owner, and Merge process codes are not seeded.

---

## Pilot Candidate Analysis

| # | Candidate Process | Business Basis | Module Readiness | Required Backend Changes | Required Frontend Changes | Required DB/Migration | Required Permission Changes | Risks | Recommendation |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **CREATE_CUSTOMER** — Submit new customer for approval before final creation | CUS-002: staff create CREATE_CUSTOMER requests. ProcessCode seeded, active, approval-required. | Customer backend CRUD exists (CustomersController). Customer frontend CRUD exists (create/edit/detail pages). Workflow engine complete. | Moderate: needs integration point where customer creation triggers workflow instance, and execution handler that performs final creation after approval. Current CustomerCreate is direct (CUSTOMER_CREATE_FINAL permission). | Moderate: CustomerCreatePage needs workflow entry point. Customer detail needs workflow status display. | None for workflow schema. Customer table may need a status/workflow_instance_id column, or a separate linkage table. | CUSTOMER_CHANGE_REQUEST_CREATE already cataloged. May need to wire it. | Customer creation flow changes from direct to approval-gated. Existing direct-create path must coexist for authorized data admins. | **Best candidate** — both module and process code exist. |
| 2 | **CUSTOMER_MASTER_CHANGE** — Submit customer data change for approval | CUS-002: staff create CUSTOMER_MASTER_CHANGE requests. ProcessCode seeded, active, approval-required. | Customer backend exists. Customer frontend exists. Workflow engine complete. | Moderate: needs change-proposal capture (before/after data), execution handler for approved changes. More complex than CREATE_CUSTOMER because it must capture field-level diffs. | Moderate: CustomerEditPage needs proposal mode vs direct-edit mode. Needs before/after preview. | May need a change-proposal staging table or use PayloadJson for proposal data. | CUSTOMER_CHANGE_REQUEST_CREATE already cataloged. | More complex than CREATE_CUSTOMER due to field-level diff capture, before/after snapshot, and target_version conflict on execution (CUS-009). | Viable but higher complexity than CREATE_CUSTOMER. |
| 3 | Service price override approval | APR-02, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE in permission catalog. | **Service module not implemented.** No Service controller, service, or entity exists. No process code seeded. | Blocked: entire Service module must be built first. | Blocked: no Service frontend exists. | Blocked: Service schema does not exist. | Process code must be seeded. | Blocked by unimplemented module. | **Deferred** — requires Service module first. |
| 4 | Card reprint approval | CARD_REPRINT_APPROVE in permission catalog (delegable). | **Card/Print module not implemented.** No controller or entity exists. No process code seeded. | Blocked: entire Card module must be built first. | Blocked: no Card frontend exists. | Blocked: Card schema does not exist. | Process code must be seeded. | Blocked by unimplemented module. | **Deferred** — requires Card module first. |
| 5 | Change owner approval | CHANGE_OWNER_APPROVE in permission catalog (delegable). | **Plot/Owner module not implemented.** No controller or entity exists. No process code seeded. | Blocked: entire Plot/Owner module must be built first. | Blocked: no Plot frontend exists. | Blocked: Plot schema does not exist. | Process code must be seeded. | Blocked by unimplemented module. | **Deferred** — requires Plot module first. |
| 6 | Customer merge approval | CUS-007, CUSTOMER_MERGE_DUPLICATE. | Customer backend exists but merge endpoint not implemented. No process code seeded. | Significant: merge logic with preview/audit, source history retention. No merge controller action exists. | Significant: merge UI with preview of affected services/payments/documents/contexts. | Merge may need additional tracking tables. | Process code must be seeded. CUSTOMER_MERGE_DUPLICATE exists in catalog. | Most complex candidate. Merge affects many related entities. Not confirmed as mandatory workflow in business rules. | **Deferred** — too complex for first pilot. |
| 7 | Confirmed payment correction approval | PAY-005, PAYMENT_CORRECT_CONFIRMED. | **Payment module not implemented.** No Payment controller exists. No process code seeded. | Blocked: entire Payment module must be built first. | Blocked: no Payment frontend exists. | Blocked: Payment schema does not exist. | Process code must be seeded. | Blocked by unimplemented module. | **Deferred** — requires Payment module first. |

---

## Recommended Pilot

**CREATE_CUSTOMER is the recommended pilot process.** Rationale:

1. ProcessCode `CREATE_CUSTOMER` already exists in Business_Process_Catalog (seeded, active, approval-required).
2. Customer backend CRUD already exists (CustomersController with search, getById, create, update, company contexts).
3. Customer frontend CRUD already exists (CustomersPage, CustomerCreatePage, CustomerDetailPage, CustomerEditPage).
4. Workflow engine is complete (B1 backend + B2 admin UI + B3 runtime UI).
5. CUS-002 explicitly requires staff to create CREATE_CUSTOMER requests for customer creation.
6. CUSTOMER_CHANGE_REQUEST_CREATE permission is already cataloged.
7. Least complex among available candidates — no field-level diff capture needed (unlike CUSTOMER_MASTER_CHANGE).
8. Validates the entire workflow engine end-to-end: admin configures workflow, staff submits, approver reviews, approved request triggers creation.

CUSTOMER_MASTER_CHANGE is the recommended second pilot after CREATE_CUSTOMER succeeds, but is more complex due to before/after data capture and target_version conflict handling.

**However, this recommendation requires Project Owner decision (DEC-1B3B4-01).**

---

## Proposed B4 Scope

### If CREATE_CUSTOMER pilot is approved:

**Backend integration:**
- New endpoint or modified flow: staff submits customer proposal → creates workflow instance (using existing POST /workflows/instances) with processCode=CREATE_CUSTOMER, businessEntityType=Customer, payload containing proposed customer data.
- Execution handler: when all approval steps pass, executes final customer creation from approved payload.
- Customer proposal may need a staging area (PayloadJson on the workflow instance itself, or a separate proposal table).

**Frontend integration:**
- CustomerCreatePage: new "Submit for Approval" path for staff without CUSTOMER_CREATE_FINAL permission (using CUSTOMER_CHANGE_REQUEST_CREATE instead).
- Customer detail or customer list: show pending workflow status for proposed customers.
- Link from customer workflow status to existing /workflow/instances/:instanceId detail page.
- Use existing My Approvals inbox for approver flow — no new approval UI needed.

**What B4 reuses (no changes needed):**
- Workflow Admin UI (B2) for configuring CREATE_CUSTOMER workflow/binding.
- My Approvals inbox (B3) for approvers to see pending customer creation requests.
- Workflow instance detail (B3) for approve/return/resubmit/withdraw actions.
- Reassign UI (B3) for admin reassignment.

### Explicitly Deferred from B4

- CUSTOMER_MASTER_CHANGE pilot (second pilot, higher complexity).
- My Requests UI — requires backend GAP-1 resolution.
- Action history/timeline UI — requires backend GAP-2 resolution.
- Reject action UI — requires backend GAP-3 resolution.
- Active instance migration UI.
- Service/Payment/Card/Plot/Merge module implementation.
- Delegation implementation.
- SLA/reminder implementation.
- Condition evaluation at bind/submit time.
- Generic workflow instance creation UI.
- Production migration/release unless separately approved.
- New permission codes unless separately approved.
- Broad workflow engine redesign.

---

## API and Data Integration Plan

### Existing Workflow Endpoints Reused

| Endpoint | Usage in B4 |
|---|---|
| POST /workflows/instances | Create workflow instance for customer proposal |
| GET /workflows/instances/:id | View instance detail (existing B3 UI) |
| GET /workflows/my-approvals | Approver inbox (existing B3 UI) |
| POST .../approve | Approve customer creation (existing B3 UI) |
| POST .../return | Return customer creation proposal (existing B3 UI) |
| POST .../resubmit | Resubmit after return (existing B3 UI) |
| POST .../withdraw | Withdraw proposal (existing B3 UI) |
| POST .../reassign | Reassign step (existing B3 UI) |

### Existing Customer Endpoints

| Endpoint | Usage in B4 |
|---|---|
| POST /customers | Final creation — called by execution handler after approval, NOT by staff directly |
| GET /customers/:id | View customer detail with workflow status |
| GET /customers | Search/list with potential workflow status indicator |

### Missing or New Endpoints (Blockers/Open Decisions)

| Gap | Description | Decision |
|---|---|---|
| BLOCKER-1 | No customer proposal submission endpoint — staff currently calls POST /customers directly with CUSTOMER_CREATE_FINAL. B4 needs a proposal path that creates a workflow instance instead. | DEC-1B3B4-02: New endpoint or modified flow? |
| BLOCKER-2 | No execution handler — when approval completes, who/what triggers final customer creation? | DEC-1B3B4-03: Backend auto-execution or manual execution by authorized user? |
| BLOCKER-3 | No customer-to-workflow-instance linkage — how does the customer detail page know a workflow instance exists for a proposed customer? | DEC-1B3B4-04: Link via businessEntityId on WorkflowInstance, or separate tracking? |
| GAP-1 | No GET /workflows/my-requests — requester cannot see their own submissions in a dedicated list | Remains deferred unless Project Owner decides otherwise |
| GAP-2 | No GET /workflows/instances/:id/actions — no action history/timeline | Remains deferred |
| GAP-3 | No reject action endpoint | Remains deferred |

### Payload and Snapshot

- PayloadJson on CreateWorkflowInstanceRequest is backend-owned. Frontend must compose it from the customer proposal form fields.
- BeforeDataJson is null for CREATE_CUSTOMER (no prior data).
- payload_hash and workflow_snapshot_json are computed and stored by the backend.
- Frontend must NOT display raw PayloadJson or BeforeDataJson.
- Safe payload summary: display only safe metadata (customer name, proposed fields summary) — rules TBD by Project Owner.

### Concurrency

- Workflow instance actions use RowVersion — existing B3 UI handles this.
- Customer creation execution must use target_version to prevent double-creation (CUS-009).
- Execution handler must be idempotent (APR-009).

---

## Permission and Authorization Strategy

- Backend remains authoritative for all decisions.
- Staff with CUSTOMER_CHANGE_REQUEST_CREATE can submit proposals — no CUSTOMER_CREATE_FINAL required.
- CUSTOMER_CREATE_FINAL remains required for direct creation by authorized data admins (CUS-003).
- Workflow approval actions remain service-layer authorized (assignee/requester checks).
- WORKFLOW_REASSIGN_PENDING gates reassignment (existing).
- WORKFLOW_VIEW gates instance detail view (existing).
- No frontend-only authorization assumptions.
- DENY wins remains backend-enforced.
- **Open decision**: whether CUSTOMER_CHANGE_REQUEST_CREATE needs to be added to PermissionCodes.cs and wired to the proposal endpoint (DEC-1B3B4-05).

---

## UX Strategy

1. **How user starts workflow**: On CustomerCreatePage, if user has CUSTOMER_CHANGE_REQUEST_CREATE but not CUSTOMER_CREATE_FINAL, show "Submit for Approval" instead of "Create Customer". Fills same form, but submits as workflow instance.
2. **How user sees workflow status**: On CustomerDetailPage or customer list, show workflow status badge if a pending workflow instance exists for this entity.
3. **How user navigates to approval**: Link from customer workflow status to /workflow/instances/:instanceId (existing B3 page).
4. **Version/snapshot freeze**: Existing B3 notice applies — workflow instance retains original version.
5. **Stale/concurrency errors**: Existing B3 concurrency handling applies.
6. **Sensitive payload display**: No raw PayloadJson shown. Safe metadata summary only.
7. **Pending approval state**: Customer list/detail shows "Pending Approval" status. Customer is not created until approved and executed.

---

## Testing Strategy

- Customer proposal submission tests (staff with CUSTOMER_CHANGE_REQUEST_CREATE).
- Workflow instance creation from customer proposal tests.
- Permission gate tests (CUSTOMER_CHANGE_REQUEST_CREATE vs CUSTOMER_CREATE_FINAL paths).
- Customer detail workflow status display tests.
- Navigation from customer to workflow instance detail tests.
- Safe payload summary tests (no raw JSON displayed).
- Version/snapshot freeze notice tests (existing B3 coverage).
- Concurrency/stale data tests (existing B3 coverage).
- Deferred behavior tests (no My Requests, no action history, no reject).
- Regression tests for B2 admin UI and B3 My Approvals routes.
- End-to-end flow test: propose → approve → customer created.

---

## Open Decisions

| ID | Decision | Options | Default | Owner |
|---|---|---|---|---|
| DEC-1B3B4-01 | Which pilot business process is selected? | CREATE_CUSTOMER (recommended), CUSTOMER_MASTER_CHANGE, defer B4 | CREATE_CUSTOMER | Project Owner |
| DEC-1B3B4-02 | How does staff submit a customer proposal? | (a) New backend endpoint POST /customers/proposals that internally creates workflow instance, (b) Frontend calls POST /workflows/instances directly with composed payload, (c) Modified POST /customers that detects proposal vs direct-create | Option (a) — cleanest separation | Project Owner |
| DEC-1B3B4-03 | How is approved customer creation executed? | (a) Backend auto-execution when final step approved, (b) Manual execution by authorized user after approval | Option (a) — matches APR-008/APR-009 | Project Owner |
| DEC-1B3B4-04 | How is customer linked to workflow instance? | (a) Query WorkflowInstance by processCode=CREATE_CUSTOMER + businessEntityId, (b) Add workflow_instance_id column to Customers table, (c) Separate linkage table | Option (a) — no schema change, uses existing data | Project Owner |
| DEC-1B3B4-05 | Does B4 require adding CUSTOMER_CHANGE_REQUEST_CREATE to PermissionCodes.cs? | Yes (wire to proposal endpoint), No (use existing permission or open endpoint) | Yes — already in permission catalog, needs code wiring | Project Owner |
| DEC-1B3B4-06 | Does B4 allow backend changes? | Yes (required for integration), No (frontend-only, limited) | Yes — backend integration is essential for meaningful pilot | Project Owner |
| DEC-1B3B4-07 | Does B4 allow database/migration changes? | Yes (if new tables/columns needed), No (use existing schema only) | Depends on DEC-1B3B4-04 | Project Owner |
| DEC-1B3B4-08 | Is workflow status stored on customer or derived from workflow instance? | (a) Derived by querying WorkflowInstance, (b) Denormalized status column on customer | Option (a) — no schema change, single source of truth | Project Owner |
| DEC-1B3B4-09 | Is payload summary metadata-only? | Yes (safe metadata: customer name, type, proposed fields list), No (show specific field values) | Yes — avoid sensitive data exposure | Project Owner |
| DEC-1B3B4-10 | Do My Requests/action history/reject remain deferred in B4? | Yes (deferred), No (resolve backend gaps in B4) | Yes — deferred to separate gap-resolution phase | Project Owner |
| DEC-1B3B4-11 | Does production migration remain deferred? | Yes, No | Yes — B4 is development/sandbox only | Project Owner |
| DEC-1B3B4-12 | Is the pilot internal-only or user-visible? | Internal (development/testing only), User-visible (deployed to staging/production) | Internal — B4 is development validation only | Project Owner |
| DEC-1B3B4-13 | Should B4 also include CUSTOMER_MASTER_CHANGE or only CREATE_CUSTOMER? | CREATE_CUSTOMER only (simpler), Both (more comprehensive) | CREATE_CUSTOMER only — validate engine first, then expand | Project Owner |

---

## Risks

1. **Pilot process depends on backend integration** — B4 cannot be frontend-only like B3. Backend changes are required to connect customer creation to the workflow engine.
2. **Payload may expose sensitive data** — Customer proposal contains personal data (name, CCCD, phone, DOB). PayloadJson must not be displayed raw. Safe summary rules must be defined before implementation.
3. **Business object state and workflow state can diverge** — A proposed customer exists as a workflow instance but not yet as a Customer record. UI must clearly distinguish "proposed" from "created".
4. **Execution handler complexity** — Auto-execution after approval requires idempotent handling (APR-009), error recovery, and correlation_id tracking.
5. **Direct-create path must coexist** — Authorized data admins (GROUP_CUSTOMER_DATA_ADMIN with CUSTOMER_CREATE_FINAL) bypass workflow. Both paths must work correctly.
6. **Active instance migration remains deferred** — If workflow configuration changes, existing instances retain their original version. No migration UI exists.
7. **Duplicate checking interaction** — CUS-005 requires duplicate check before submit AND before execution. Proposal submission and execution both need duplicate checking.
8. **Backend remains authoritative** — All authorization, validation, and execution decisions happen server-side.
9. **Production release remains separately controlled** — B4 implementation does not authorize deployment.

---

## Recommended Project Owner Decision

**Approve B4 as a limited CREATE_CUSTOMER pilot integration** with the following parameters:
- Pilot process: CREATE_CUSTOMER only.
- Backend changes: Yes (proposal endpoint, execution handler, permission wiring).
- Frontend changes: Yes (CustomerCreatePage proposal mode, workflow status display).
- Database changes: Minimal or none (prefer deriving linkage from existing WorkflowInstance data).
- Permission changes: Wire CUSTOMER_CHANGE_REQUEST_CREATE to PermissionCodes.cs.
- Payload strategy: Safe metadata summary only, no raw PayloadJson display.
- My Requests/action history/reject: Remain deferred.
- Production: Development/sandbox only.

If the Project Owner prefers to defer B4 entirely, the alternative is to proceed with another module phase (e.g., Service module, Payment module) and return to pilot integration when more business processes are ready.

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
- No production migration/release.

---

## Conclusion

PHASE 1B.3-B4 WORKFLOW PILOT INTEGRATION DETAILED PLAN READY FOR PROJECT OWNER REVIEW
