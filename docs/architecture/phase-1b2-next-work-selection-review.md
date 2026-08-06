# Phase 1B.2 Next Work Selection Review

**Status:** ACCEPTED — SEE phase-1b2-next-work-selection-project-owner-acceptance.md

**Baseline:** 6296935843c75d6633133181925645dd55470205

**Previous status:** PHASE 1B.1 SECURITY ADMINISTRATION ACCEPTED AS FUNCTIONALLY COMPLETE

---

## 1. Purpose

Evaluate candidate next-work directions after Phase 1B.1 Security Administration completion and recommend a single next phase for Project Owner approval. No source implementation is authorized by this review.

---

## 2. Current baseline and status

- HEAD: `6296935843c75d6633133181925645dd55470205`
- Branch: `feature/phase-1-organization`
- Latest commit: Accept Phase 1B.1 security administration completion
- Phase 1B.1 Security Administration: FUNCTIONALLY COMPLETE
- All 10 security administration capabilities delivered and accepted.

---

## 3. Phase 1B.1 Security Administration confirmation

Phase 1B.1 Security Administration is functionally complete. The Project Owner accepted completion under commit `6296935`. All formula components (`DepartmentBaseAllow ∪ RoleCompanyAllow ∪ EffectiveIndividualAllow - EffectiveIndividualDeny`) have dedicated administration and diagnostics UI.

---

## 4. PTKD-ERP-Master-Context.md

**Not found.** Searched via `git ls-files`, `git grep`, and filesystem glob. No master context file exists in the repository.

This is recorded as a **documentation gap**. The review proceeds using existing committed business and architecture documents only.

---

## 5. Completed security capability foundation

| # | Capability | Phase | Gate |
|---|-----------|-------|------|
| 1 | Account Management | 1B.1-K | SECURITY_ACCOUNT_MANAGE GLOBAL |
| 2 | Account Management Discovery / K0 | 1B.1-K0 | Backend |
| 3 | Individual Permission Assignment | 1B.1-N | SECURITY_ADMIN_MANAGE GLOBAL |
| 4 | Security Audit Viewer | 1B.1-O | SECURITY_AUDIT_VIEW GLOBAL |
| 5 | Role Permission Management | 1B.1-P1 | SECURITY_ADMIN_MANAGE GLOBAL |
| 6 | Admin Group Permission Management | 1B.1-P2 | SECURITY_ADMIN_MANAGE GLOBAL |
| 7 | User Role Assignment | 1B.1-Q1 | SECURITY_ADMIN_MANAGE GLOBAL |
| 8 | User Admin Group Membership | 1B.1-Q2 | SECURITY_ADMIN_MANAGE GLOBAL |
| 9 | Department Baseline Permission Management | 1B.1-R | SECURITY_ADMIN_MANAGE GLOBAL |
| 10 | Effective Permission Diagnostics | 1B.1-S | SECURITY_ADMIN_MANAGE GLOBAL |

The authorization infrastructure can now gate any business module using the permission catalog.

---

## 6. Candidate next-work options

| Option | Candidate | Source support |
|--------|-----------|---------------|
| A | Customer module | CUS-001 through CUS-009, CUS-01 through CUS-07, 7 permission codes |
| B | Service module | SERVICE_CREATE_STANDARD through SERVICE_PRICE_OVERRIDE_APPROVE, APR-02 |
| C | Payment module | PAY-001 through PAY-012, PAY-01 through PAY-08, 5 permission codes |
| D | Organization UI continuation | ORGANIZATION_* permission codes already in catalog |
| E | Workflow / approval configuration | WFD-001 through WFD-012, WFC-01 through WFC-16, 7 permission codes |
| F | Security enhancement backlog | 11 deferred items from Phase 1B.1 completion |
| G | Technical readiness / cleanup | No specific acceptance criteria |

---

## 7. Candidate-by-candidate analysis

### A. Customer module

- **Source support:** CUS-001 through CUS-009 (business rules), CUS-01 through CUS-07 (acceptance criteria). 7 permission codes in catalog: CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CHANGE_REQUEST_CREATE, CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE, CUSTOMER_MERGE_DUPLICATE, CUSTOMER_GROUP_FINANCE_VIEW.
- **Enabled by Phase 1B.1:** Permission assignment, role management, department baseline, effective permission diagnostics. The security infrastructure can gate all 7 customer permissions.
- **Backend work required:** Yes. Customer domain entities, Profiles table, Customers table, Customer_Company_Context table. Controllers, services, DTOs, validators.
- **DB migration likely required:** Yes. New tables: Customers, Profiles, Customer_Company_Context. Indexes, constraints, filtered unique index on CCCD (CUS-006).
- **Workflow/approval dependency:** Partial. CUS-002 references CREATE_CUSTOMER and CUSTOMER_MASTER_CHANGE requests which require approval workflow. However, a phased approach can deliver customer CRUD and search first, then add approval integration.
- **ENTITY scope dependency:** No. Customer master is GLOBAL scope. Customer_Company_Context uses COMPANY scope. Both are supported.
- **Payment/reconciliation dependency:** Indirect. CUS-07 references spending from confirmed payments but can be deferred or stubbed.
- **Main risks:** Workflow dependency for create/change requests. Merge operation (CUS-007) is complex. Sensitive data masking (CUS-001, SEC-004) requires careful implementation.
- **Should it be first?** Yes. Strongest acceptance criteria coverage. GLOBAL scope aligns with existing model. Can be phased to avoid workflow dependency initially. Foundational for Service and Payment modules.

### B. Service module

- **Source support:** 4 permission codes (SERVICE_CREATE_STANDARD, SERVICE_RENEW_STANDARD, SERVICE_PRICE_OVERRIDE_REQUEST, SERVICE_PRICE_OVERRIDE_APPROVE). APR-02 references price override approval.
- **Enabled by Phase 1B.1:** Permission gates available. COMPANY scope supported via role assignments.
- **Backend work required:** Yes. Service domain, pricing logic, renewal logic.
- **DB migration likely required:** Yes. Service tables, pricing tables.
- **Workflow/approval dependency:** Strong. SERVICE_PRICE_OVERRIDE requires approval workflow (APR-02, APR-03). Cannot deliver core service pricing without workflow.
- **ENTITY scope dependency:** No.
- **Payment/reconciliation dependency:** Yes. Services are tied to payment items.
- **Main risks:** Requires both Customer module (services reference customers) and Workflow module. Cannot stand alone.
- **Should it be first?** No. Too many upstream dependencies (Customer, Workflow, Payment).

### C. Payment module

- **Source support:** PAY-001 through PAY-012 (business rules), PAY-01 through PAY-08 (acceptance criteria). 5 permission codes.
- **Enabled by Phase 1B.1:** Permission gates and COMPANY-scoped role assignments available.
- **Backend work required:** Yes. Payment domain, reconciliation, correction logic, stored procedures with hard invariants.
- **DB migration likely required:** Yes. Payment tables, reconciliation period tables, audit triggers.
- **Workflow/approval dependency:** Indirect for normal flow (PAY-001: no approval for standard confirmation). Strong for price override (APR-02, APR-03).
- **ENTITY scope dependency:** No.
- **Payment/reconciliation dependency:** Self. This is the payment module.
- **Main risks:** Hard invariants (PAY-004, PAY-006) require database-level enforcement. Correction logic (PAY-005 through PAY-011) is complex. Reconciliation period management adds significant scope. Requires Customer and Service modules upstream.
- **Should it be first?** No. Requires Customer and Service modules. Most complex business logic. Should be last among business modules.

### D. Organization UI continuation

- **Source support:** ORGANIZATION_USER_MANAGE, ORGANIZATION_DEPARTMENT_MANAGE, ORGANIZATION_COMPANY_MANAGE already in permission catalog. Phase 1A.2 backend complete.
- **Enabled by Phase 1B.1:** Fully enabled. Backend APIs exist. Permission gates exist.
- **Backend work required:** Minimal or none. Phase 1A.2 delivered all Organization APIs.
- **DB migration likely required:** No.
- **Workflow/approval dependency:** None.
- **ENTITY scope dependency:** No.
- **Payment/reconciliation dependency:** None.
- **Main risks:** Low complexity. Limited business value beyond admin tooling already partially available.
- **Should it be first?** Possible but low priority. Organization admin is operational tooling, not a business capability. Could be done as a small phase between larger ones.

### E. Workflow / approval configuration

- **Source support:** WFD-001 through WFD-012 (design-time), APR-001 through APR-011 (runtime), WFC-01 through WFC-16 (acceptance criteria). 7 permission codes (WORKFLOW_VIEW through WORKFLOW_AUDIT_VIEW).
- **Enabled by Phase 1B.1:** Permission gates available.
- **Backend work required:** Yes. Workflow domain, version management, binding resolution, approver resolution, runtime engine.
- **DB migration likely required:** Yes. Workflow_Definitions, Workflow_Versions, Workflow_Steps, Workflow_Bindings, Approval_Requests, Approval_Actions, Business_Process_Catalog, many more.
- **Workflow/approval dependency:** Self. This is the workflow module.
- **ENTITY scope dependency:** No.
- **Payment/reconciliation dependency:** No.
- **Main risks:** Most architecturally complex module. 16 acceptance criteria. 12 design-time rules + 11 runtime rules + 5 SLA/reminder rules + 8 delegation rules. Requires Business_Process_Catalog to be seeded by DEV (GOV-001). Approver resolution has 8 source types (WFD-009). Building this first delays business module delivery.
- **Should it be first?** No. High complexity, long delivery time, and no direct business value until Customer/Service/Payment modules consume it. Should be built when a business module needs it.

### F. Security enhancement backlog

- **Source support:** 11 deferred items from Phase 1B.1 completion acceptance.
- **Enabled by Phase 1B.1:** N/A — these are enhancements to Phase 1B.1 itself.
- **Backend work required:** Yes for most items (source attribution, authorization matrix, user search, ENTITY scope).
- **DB migration likely required:** Depends on item.
- **Workflow/approval dependency:** Only for workflow approval of security changes (item 10).
- **ENTITY scope dependency:** Item 6 is ENTITY scope itself.
- **Payment/reconciliation dependency:** None.
- **Main risks:** Improves admin experience but does not advance business capability. All items were explicitly accepted as non-blocking by the Project Owner.
- **Should it be first?** No. All items were deferred as non-blocking. Business module delivery should take priority.

### G. Technical readiness / cleanup

- **Source support:** No specific acceptance criteria.
- **Enabled by Phase 1B.1:** N/A.
- **Backend work required:** Depends on scope.
- **DB migration likely required:** No.
- **Workflow/approval dependency:** None.
- **ENTITY scope dependency:** No.
- **Payment/reconciliation dependency:** None.
- **Main risks:** No measurable business value. Delays business module delivery. Project readiness review (existing doc) already identified the technical stack decisions.
- **Should it be first?** No, unless the Project Owner identifies specific technical debt blocking business module work.

---

## 8. Recommended next phase

**Phase 1B.2-A — Customer Module Discovery and Detailed Plan**

---

## 9. Why the Customer module should be next

1. **Strongest acceptance criteria coverage.** 9 business rules (CUS-001 through CUS-009) and 7 acceptance criteria (CUS-01 through CUS-07) provide clear, testable requirements.
2. **GLOBAL scope aligns with existing authorization model.** Customer master is GLOBAL (DATA-001). No ENTITY scope required. COMPANY scope is used only for Customer_Company_Context (DATA-002), which is already supported.
3. **Foundational for downstream modules.** Service, Payment, and Reconciliation all reference customers. Building Customer first unblocks the dependency chain.
4. **Phaseable to avoid workflow dependency.** Customer search, view, and direct administrator operations (CUS-003, CUS-004, CUS-005) can be delivered without approval workflow. CREATE_CUSTOMER and CUSTOMER_MASTER_CHANGE approval integration can follow in a later sub-phase.
5. **Security infrastructure ready.** All 7 customer permission codes are in the catalog. Role and department-based permission assignment can gate customer operations immediately.
6. **Limited cross-cutting risk.** Customer module does not depend on Payment, Service, or Workflow modules for core functionality.
7. **Aligns with project phasing.** The project readiness review (Section 6) lists Customer Master as Phase 2, immediately after Foundation & Auth (Phase 1).

---

## 10. Backend/API impact

The Customer module will require:
- New domain entities: Customer, Profile, CustomerCompanyContext.
- New controllers: CustomersController, ProfilesController (or combined).
- New DTOs, validators, services.
- API routes under `/api/v2/customers/` and possibly `/api/v2/profiles/`.
- Permission enforcement on all endpoints using existing authorization infrastructure.
- Sensitive data masking for CCCD, DOB, phone, legal/contact address (SEC-004, CUS-001).

---

## 11. Database/migration impact

The Customer module will require:
- New tables: Customers, Profiles, Customer_Company_Context.
- Filtered unique index on active CCCD (CUS-006).
- `rowversion` columns for optimistic concurrency (CUS-009).
- Audit columns on all tables (GOV-007).
- Possible append-only audit trigger on Customer changes (SEC-001).
- Forward and rollback migration scripts in `database/migrations/` and `database/rollbacks/`.

---

## 12. Permission/catalog impact

All 7 customer permission codes are already in the permission catalog:
- CUSTOMER_VIEW_BASIC
- CUSTOMER_VIEW_SENSITIVE
- CUSTOMER_CHANGE_REQUEST_CREATE
- CUSTOMER_CREATE_FINAL
- CUSTOMER_MASTER_UPDATE
- CUSTOMER_MERGE_DUPLICATE
- CUSTOMER_GROUP_FINANCE_VIEW

No new permission codes are expected for the initial Customer module phase. The catalog should not need modification unless discovery reveals a gap.

---

## 13. Workflow/approval dependency impact

- CUS-002 requires CREATE_CUSTOMER and CUSTOMER_MASTER_CHANGE to go through approval workflow.
- The workflow module does not yet exist.
- **Mitigation:** Phase the Customer module delivery:
  - Sub-phase A: Customer search, view, Customer_Company_Context, direct administrator operations (CUS-003 through CUS-009). No workflow dependency.
  - Sub-phase B: CREATE_CUSTOMER and CUSTOMER_MASTER_CHANGE approval integration. Requires workflow module or can be stubbed with a simplified direct-approval flow.
- The discovery phase should determine the exact phasing boundary.

---

## 14. ENTITY scope dependency impact

No ENTITY scope dependency. Customer master is GLOBAL (DATA-001). Customer_Company_Context uses COMPANY scope (DATA-002). Both are supported by the current authorization model.

---

## 15. Risks/blockers

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Workflow module not yet built; CUS-002 requires approval flow | Medium | Phase customer delivery to defer approval integration |
| 2 | Customer merge (CUS-007) is complex; affects services, payments, documents | Medium | Defer merge to a later sub-phase after basic CRUD |
| 3 | Sensitive data masking (SEC-004) requires careful implementation | Medium | Design masking strategy in discovery phase |
| 4 | No git remote configured; branch is local only | Low | Configure remote before first push |
| 5 | PTKD-ERP-Master-Context.md not found | Low | Proceed with existing docs; create master context if needed |
| 6 | Group spending calculation (CUS-07) depends on confirmed payments | Low | Defer or stub until Payment module exists |
| 7 | Duplicate detection (CUS-005, CUS-006) requires careful index design | Low | Address in discovery phase |

---

## 16. Required Project Owner decisions

1. **Approve Customer module as next phase** — or select an alternative from the candidate list.
2. **Approve phased approach** — deliver customer search/view/admin first, approval integration later.
3. **Decide on customer merge timing** — include in initial phase or defer.
4. **Decide on group spending** — stub, defer, or include with payment dependency.
5. **Confirm discovery scope** — Phase 1B.2-A would be discovery and detailed plan only, not implementation.
6. **Confirm backend/database/API work authorization** — backend, database, and API work authorized only after detailed plan acceptance.
7. **Confirm ENTITY scope remains deferred** — ENTITY scope should not be introduced unless separately approved.

---

## 17. Authorization statement

No source implementation is authorized by this review. This review recommends a next phase for Project Owner approval only. Implementation requires a separately approved plan and implementation phase.

---

## 18. Conclusion

The Customer module has the strongest documented requirements, fewest cross-cutting dependencies, and provides the most foundational business value. It aligns with the project's phasing plan and is fully supported by the completed security administration infrastructure.

NEXT WORK SELECTION REVIEW READY FOR PROJECT OWNER REVIEW
