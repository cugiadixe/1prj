# Phase 1B.3 Next Work Selection Review After Customer First Slice

**Status:**
PHASE 1B.3-A WORKFLOW/APPROVAL ENGINE DISCOVERY SELECTED — SEE phase-1b3-project-owner-selection-acceptance.md

**Baseline:**
2f4c059dd7f5f91aa14f6f5560fc360808049668

**Branch:**
feature/phase-1-organization

**Date:**
2026-07-31

---

## 1. Current completed state

- Phase 1B.1 Security Administration is functionally complete.
- Phase 1B.2 Customer Module first slice is functionally complete.
- Customer backend foundation complete.
- Customer frontend UI complete.
- Customer first slice accepted by Project Owner.

---

## 2. Accepted Customer first-slice reference

| Reference | Commit |
|-----------|--------|
| Customer first slice completion review | 4a3a32a39161a0df775ddbcfff4c6fb7428567a3 |
| Customer first slice Project Owner completion acceptance | 2f4c059dd7f5f91aa14f6f5560fc360808049668 |
| B1 final acceptance | 498991318c7e18f4a9dae11409e90a7a42abc1f4 |
| B2 final acceptance | 3200d2c92403af94f01d1690b8a03777ad5bb27c |

---

## 3. PTKD-ERP-Master-Context.md

**Not found.** No master context file exists in the repository. This is a known documentation gap carried forward from the Phase 1B.2 next work selection review. This review proceeds using existing committed business and architecture documents only.

---

## 4. Open deferred backlog

The following items are not complete and not authorized for implementation:

- Workflow/approval runtime and UI.
- Customer merge.
- Group spending/spending aggregation.
- ENTITY scope.
- Service module integration.
- Payment/Reconciliation integration.
- Export/download.
- Security enhancement backlog.
- Production migration/release approval.

---

## 5. Candidate next work options

### Option A — Service Module Discovery and Detailed Plan

**Purpose:**
- Discover Service/business transaction requirements that depend on Customer.
- Define service package / service sale / care package / operational service scope only from existing business docs.
- Identify database/API/frontend impacts before implementation.

**Why this is a strong next option:**
- Customer first slice gives the master customer foundation.
- Service is likely the next business layer before Payment/Reconciliation.

**Risks:**
- Service rules may require approval workflows.
- Service may introduce payment dependencies.
- Must avoid inventing service requirements.

---

### Option B — Workflow/Approval Engine Discovery and Detailed Plan

**Purpose:**
- Discover dynamic approval configuration for business processes.
- Address previously deferred CUS-002 workflow and future flows such as duplicate handling, service sale approval, card reprint approval, and other approval-driven actions.

**Why this is a strong next option:**
- Multiple future modules may depend on configurable approval.
- Reduces risk of hardcoding approval inside Service/Customer/Payment later.
- Business rules WFD-001 through WFD-012 and APR-001 through APR-011 define substantial workflow requirements already documented.
- Delegation rules DEL-001 through DEL-006 are tightly coupled to approval runtime.
- SLA/reminder rules REM-001 through REM-005 are part of the workflow system.

**Risks:**
- Larger cross-cutting scope.
- Requires careful separation between workflow configuration, runtime, permissions, and audit.
- Must not retrofit into completed Customer first slice without explicit approval.

---

### Option C — Customer Merge Discovery and Detailed Plan

**Purpose:**
- Discover controlled customer duplicate merge process.
- Address CUS-007 only.

**Why this is a narrower next option:**
- Directly follows duplicate detection.

**Risks:**
- Lower business breadth than Service or Workflow.
- Merge may need approval workflow and deep audit.
- Could be better after workflow engine decisions.

---

### Option D — Payment/Reconciliation Discovery and Detailed Plan

**Purpose:**
- Discover payment collection, daily/monthly reconciliation, and accounting handoff scope.

**Why this may be premature:**
- Payment likely depends on Service/Bill/transaction source objects.
- Customer first slice alone may not be enough.

**Risks:**
- Could force incomplete abstractions without Service module defined.

---

### Option E — Production Release / Migration Readiness Review

**Purpose:**
- Review readiness to apply accepted migrations and deploy current completed slices.

**Why this is separate:**
- No production auto-migration is authorized.
- V0005 must not auto-apply to production.

**Risks:**
- Release readiness is operational, not new business functionality.
- May need environment, backup, rollback, and stakeholder approval outside code.

---

## 6. Recommended next selection

**Recommend Option B — Workflow/Approval Engine Discovery and Detailed Plan.**

**Rationale:**
- The user has explicitly raised concern about dynamic approval flows.
- Workflow appears repeatedly as deferred scope across Customer (CUS-002), Service, and Payment modules.
- Service sale, duplicate handling, and future business processes may require approval.
- Designing workflow before Service implementation reduces risk of hardcoding approval logic into future modules.
- Business rules already define substantial workflow requirements (WFD-001–WFD-012, APR-001–APR-011, DEL-001–DEL-006, REM-001–REM-005).
- This should be discovery/planning only first, not implementation.

Option A — Service Module Discovery remains a strong alternative if Project Owner prioritizes immediate business module delivery over cross-cutting workflow foundation.

---

## 7. Recommended next authorized task

Phase 1B.3-A Workflow/Approval Engine Discovery and Detailed Plan

**Scope:**
- Discovery and detailed planning only.
- No source code.
- No migrations.
- No frontend implementation.
- No backend implementation.
- No permission catalog changes unless separately approved after discovery.
- No production migration/release.

---

## 8. Explicit non-authorization

This next work selection review does not authorize implementation.
This review does not authorize Service, Payment, Workflow, Merge, ENTITY, Export, or Production migration implementation.
A separate Project Owner selection acceptance is required.

---

## 9. Risks / blockers

| # | Risk / blocker | Severity |
|---|----------------|----------|
| 1 | Master context may be missing or stale | Medium |
| 2 | Business rules for Service/Payment/Workflow may be incomplete | Medium |
| 3 | Workflow may affect many future modules | High |
| 4 | Approval permissions, audit, and versioning need careful design | High |
| 5 | Existing customer first slice must not be destabilized | Medium |
| 6 | Production migration remains separately controlled | High |

---

## 10. Project Owner decision requested

Select one:

- **Option A** — Service Module Discovery and Detailed Plan.
- **Option B** — Workflow/Approval Engine Discovery and Detailed Plan.
- **Option C** — Customer Merge Discovery and Detailed Plan.
- **Option D** — Payment/Reconciliation Discovery and Detailed Plan.
- **Option E** — Production Release / Migration Readiness Review.
- **Option F** — Other, requiring explicit written scope.

---

## 11. Conclusion

PHASE 1B.3 NEXT WORK SELECTION REVIEW READY FOR PROJECT OWNER DECISION
