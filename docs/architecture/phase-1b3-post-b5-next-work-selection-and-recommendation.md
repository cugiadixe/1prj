# Phase 1B.3 Post-B5 Next-Work Selection and Recommendation

## Status

PROPOSED — AWAITING PROJECT OWNER NEXT-WORK DECISION

## Baseline

- B5-D Project Owner closure acceptance commit:
  0a4149fb233c516210acba197a8b2977cbc39170
- B5-D closure review commit:
  e19e5f1d5d7710e379d722ff90972c3f85725240
- B5-D closure report commit:
  e4b1c2130e5aa9db67cdcae1b00b8f5322f4d74f
- B5-C Project Owner frontend acceptance commit:
  39760a9cbee6fe6f352b4336423b89a8b2149086
- B5-B Project Owner backend acceptance commit:
  c42734e351404d9788b82e2049c92f6de09baf18

## Completed Work Summary

- B5-B backend runtime hardening.
- B5-C frontend runtime hardening.
- B5-D operational validation and closure.
- Phase 1B.3-B5 Workflow Pilot Hardening.

## Selection Criteria

Criteria used to rank next work:
- business value,
- dependency readiness,
- specification completeness,
- permission/security impact,
- data/migration impact,
- testability,
- delivery risk,
- fit after B5 workflow hardening.

## Candidate Options Reviewed

### A. CUSTOMER_MASTER_CHANGE workflow expansion
- Description: Implementing the `CUSTOMER_MASTER_CHANGE` workflow to update existing customer profiles safely.
- Source basis: `docs/business/process-catalog.md`, `docs/business/acceptance-criteria.md` (CUS-04).
- Benefits: Allows staff to request and approve updates to customer data without direct modification rights.
- Dependencies: Fully ready. Relies on the newly hardened B5 workflow runtime.
- Risks: Concurrency risks when multiple updates target the same customer (target-version conflict handling).
- Missing decisions: None.
- Suitability now: Highly suitable. Directly leverages B5 hardening.

### B. Customer merge / CUS-007
- Description: Implementing `CUSTOMER_MERGE_DUPLICATE` workflow to consolidate duplicate profiles.
- Source basis: `docs/business/process-catalog.md`, `docs/business/acceptance-criteria.md`.
- Benefits: Keeps customer data clean and consolidated.
- Dependencies: Requires customer master records to exist.
- Risks: High data complexity. Must re-parent services, payments, and documents.
- Missing decisions: Need detailed rules on how to handle conflicts in merged attributes (e.g., differing addresses).
- Suitability now: Suitable, but complex. Should follow or be grouped with `CUSTOMER_MASTER_CHANGE`.

### C. Service module / service sales / pricing package work
- Description: Service catalog management, `SERVICE_PRICE_OVERRIDE` flow.
- Source basis: `docs/business/process-catalog.md`.
- Benefits: Enables core sales operations.
- Dependencies: Customer master data must be complete first.
- Risks: Pricing and snapshot complexities.
- Missing decisions: `SELL_CARE_PACKAGE` is still marked as INACTIVE pending specification.
- Suitability now: Deferred until Customer module is fully complete.

### D. Payment module / payment transaction flow
- Description: Draft and confirm payments, `ADMIN_PAYMENT` correction workflows.
- Source basis: `docs/business/process-catalog.md`, `docs/business/acceptance-criteria.md` (PAY-03 to PAY-07).
- Benefits: Revenue tracking and accounting.
- Dependencies: Requires both Customer and Service modules to be completed first.
- Risks: Financial reconciliation complexity (PAY-07).
- Missing decisions: None identified, but high inherent risk.
- Suitability now: Deferred due to missing dependencies.

### E. Card flow / grave card print or reprint approval
- Description: `CARD_REPRINT` approval workflow.
- Source basis: `docs/business/process-catalog.md`.
- Benefits: Manages physical credential reprints.
- Dependencies: Requires Customers and Plots.
- Risks: Low.
- Missing decisions: Physical printing integration details.
- Suitability now: Deferred due to missing dependencies.

### F. Plot / cemetery location flow
- Description: `CHANGE_OWNER` approval workflow for plots.
- Source basis: `docs/business/process-catalog.md`.
- Benefits: Manages plot ownership transfers.
- Dependencies: Requires Customer module.
- Risks: Medium.
- Missing decisions: Exact plot schema and state transitions.
- Suitability now: Deferred due to missing dependencies.

### G. ENTITY permission expansion
- Description: Expanding permission checks to arbitrary entities using a generic or specific implementation.
- Source basis: `docs/business/permission-catalog.md`.
- Benefits: Fine-grained security.
- Dependencies: None.
- Risks: Overcomplicating the permission evaluation engine.
- Missing decisions: Specific business needs for entity-level permissions beyond Company.
- Suitability now: Deferred. Not driven by an immediate business module need.

### H. Safe user lookup/reassign expansion
- Description: Allowing workflows to safely query users and reassign active steps.
- Source basis: B5-D deferred items.
- Benefits: Operational flexibility for stuck workflows.
- Dependencies: Hardened B5 runtime.
- Risks: Bypassing standard approval paths if not constrained.
- Missing decisions: Exactly who can reassign and what constraints apply.
- Suitability now: Suitable as a minor technical expansion, but lower business value than Customer Master completion.

### I. Export/download capability
- Description: `SENSITIVE_EXPORT` workflow for data downloads.
- Source basis: `docs/business/process-catalog.md`.
- Benefits: Secure data extraction.
- Dependencies: Hardened B5 runtime.
- Risks: High security risk (data exfiltration).
- Missing decisions: Which specific grids/tables require this first.
- Suitability now: Deferred until more business data exists to export.

### J. Production readiness / release preparation
- Description: Preparing scripts and environments for production deployment.
- Source basis: B5-D deferred items.
- Benefits: Enables actual business use.
- Dependencies: Minimum viable product features must be completed.
- Risks: High operational risk.
- Missing decisions: Infrastructure layout, hosting environments.
- Suitability now: Deferred. The system lacks core modules (Customer, Service, Payment) to be useful in production.

## Recommendation

Recommended next phase:
Phase 1B.4 — Customer Master Expansion

Recommendation:
Proceed with implementing the `CUSTOMER_MASTER_CHANGE` and `CUSTOMER_MERGE_DUPLICATE` business processes to complete the Customer Master module.

Rationale:
- Directly utilizes the newly hardened B5 workflow engine safely and efficiently.
- Completes the Customer Master domain, which is a hard prerequisite for the subsequent Service and Payment modules.
- Clear acceptance criteria (CUS-04, CUS-05, CUS-06) already exist in the business documents, minimizing unauthorized business invention.
- The `CREATE_CUSTOMER` workflow was successfully piloted in Phase 1B.3; extending it to updates and merges is the most logical progression with manageable technical risk.

Why not the other options now:
- Service / Payment modules: Missing required dependency (Customer Master).
- Production release: Insufficient business value delivered to justify a production launch at this stage.
- Safe user lookup / ENTITY permissions: Lower priority technical expansions compared to core business capabilities.

## Required Project Owner Decisions

- Confirm the target scope (Change and Merge workflows).
- Confirm rules for handling conflicting data during a Customer Merge.
- Approve the creation of a discovery and detailed plan for this phase.

## Proposed Scope of Recommended Next Phase

In-scope:
- Implementation of the `CUSTOMER_MASTER_CHANGE` workflow execution handler.
- Implementation of the `CUSTOMER_MERGE_DUPLICATE` workflow execution handler.
- Backend API endpoints for submitting and processing these requests.
- Frontend UI components for requesting changes and reviewing merge conflicts.

Out-of-scope:
- Service and Payment modules.
- Changes to the core workflow engine schema.
- Production migration.

Deliverables:
- Discovery and detailed plan document.
- Backend implementation and tests.
- Frontend implementation and tests.
- Operational validation report.

Acceptance criteria:
- CUS-04: `CUSTOMER_MASTER_CHANGE` target-version conflict does not overwrite newer data.
- Merge operations correctly attribute existing services/payments/documents to the surviving customer record.

Test strategy:
- Unit tests for execution handlers.
- Integration tests for data concurrency and merge operations.
- API tests for permission enforcement.
- Frontend component tests for UI forms.

Migration strategy:
- Versioned SQL scripts for any new Customer schema requirements. No production migration.

Documentation strategy:
- Update architecture docs with execution handler logic and design decisions.

## Explicit Non-Authorization

This document does not authorize:
- code implementation,
- frontend changes,
- backend changes,
- migration changes,
- business document changes,
- production migration,
- production release,
- release tag,
- push,
- implementation of the recommended next phase.

## Next Step

Project Owner should choose one:
- Accept the recommended next phase.
- Select a different candidate.
- Request more discovery.
- Pause Phase 1B.3 work.

## Conclusion

PHASE 1B.3 POST-B5 NEXT-WORK SELECTION PROPOSED — AWAITING PROJECT OWNER DECISION
