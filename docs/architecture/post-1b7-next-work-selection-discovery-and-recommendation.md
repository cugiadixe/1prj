# Post-1B.7 Next-Work Selection Discovery and Recommendation

## Status

PROPOSED — REQUIRES PROJECT OWNER NEXT-WORK DECISION

## Authorization Source

Reference:
- Phase 1B.7 Project Owner closure acceptance commit:
  fe74ed0a0fb9cff3337e3d8f338d62638848d706

State:
- Phase 1B.7 is closed.
- This document is next-work discovery and recommendation only.
- This document does not select next work as Project Owner.
- This document does not authorize implementation.

## Current Project State

Summarize closed capabilities:
- Customer Master foundation.
- Customer Merge.
- Service Module Foundation.
- Payment / Billing / Collection / Reconciliation Foundation.
- Workflow/approval foundation where relevant.
- Security/admin foundation where relevant.

## Phase 1B.7 Closure Summary

Summarize:
- backend/data accepted.
- frontend accepted.
- operational validation passed.
- Phase 1B.7 closed.
- production release remains deferred.
- Card Reprint remains deferred.
- Care Package Sales remains deferred.
- Payment Print UI remains deferred.
- non-blocking frontend test hardening follow-ups are pending.

## Candidate Options

### Option A — Card Reprint / Grave Card Reprint Workflow

- **Scope Summary:** End-to-end functionality to request, pay for, approve, and execute a card reprint or grave card reprint. Includes the UI/API for reprint request submission, integration with the payment foundation (for fees), and the approval workflow required before physical printing.
- **Source-Supported Business Rationale:** Continuously deferred in previous phases (1B.6 and 1B.7). A core operational capability for the service desk that requires payment processing and approval.
- **Dependency Readiness:** Ready. Payment Foundation (1B.7) is now complete, providing the necessary capability to capture reprint fees. Customer Master (1B.6) is complete.
- **Missing Decisions:** Clarification on the exact workflow required (e.g., does it use the dynamic Workflow Engine or a hardcoded approval state machine?) and whether physical stamp/approval constraints exist for the reprint document itself.
- **Implementation Risk:** Medium. Depends heavily on workflow state transitions and ensuring payment confirmation occurs before or concurrently with approval.
- **Suggested First Gated Task:** Project Owner Card Reprint discovery and detailed plan acceptance.
- **Recommendation:** Select.

### Option B — Care Package Sales

- **Scope Summary:** UI, API, and data modeling for selling Care Packages (`SELL_CARE_PACKAGE`), which is currently marked RESERVED / INACTIVE.
- **Source-Supported Business Rationale:** Care packages are a recurring revenue and service feature that ties deeply into customer lifecycle management.
- **Dependency Readiness:** Ready. Customer Master and Payment Foundation are closed.
- **Missing Decisions:** Business rules around renewal pricing, discount approval constraints, and exact operational workflow for care package activation.
- **Implementation Risk:** Medium-High. The business logic for renewals and discounts can be complex and may require dynamic workflow conditions.
- **Suggested First Gated Task:** Project Owner Care Package Sales discovery and detailed plan acceptance.
- **Recommendation:** Defer until Card Reprint and base operational workflows are hardened.

### Option C — Production Release / Operational Rollout Preparation

- **Scope Summary:** Finalize production infrastructure configuration (IIS/Environment), execute initial production migration of all closed phases (1B.1 through 1B.7), and release to operational users.
- **Source-Supported Business Rationale:** The core foundations (Security, Customer Master, Service, Payment) are now closed and validated in test environments. Releasing unlocks actual business value.
- **Dependency Readiness:** High readiness for the codebase itself, but operational readiness is unclear.
- **Missing Decisions:** Approval of production environment setup, business readiness for rollout without Card Reprint/Care Packages, and data migration plan for legacy systems.
- **Implementation Risk:** High. Rolling out without Card Reprint capabilities may force users to handle reprints out-of-band.
- **Suggested First Gated Task:** Production Readiness Assessment and Runbook Creation.
- **Recommendation:** Defer until Option A is complete to ensure a complete initial feature set for frontline users.

### Option D — Phase 1B.7 Hardening Follow-Ups

- **Scope Summary:** Address deferred non-blocking items from Phase 1B.7: PaymentCreatePage tests, ReconciliationMonthlyPage tests, and Payment Print UI.
- **Source-Supported Business Rationale:** Ensures complete automated coverage and provides the deferred print receipt capabilities.
- **Dependency Readiness:** Ready immediately.
- **Missing Decisions:** Does Payment Print UI require a specific thermal printer layout or standard A4?
- **Implementation Risk:** Low.
- **Suggested First Gated Task:** Hardening implementation execution.
- **Recommendation:** Defer as a standalone phase; fold these follow-ups into the next active feature phase (Option A) to maintain momentum on new value delivery.

## Comparative Assessment

| Option | Business Value | Dependency Readiness | Risk | Missing Decisions | Recommended Disposition |
|---|---|---|---|---|---|
| A. Card Reprint | High | High (Payment done) | Medium | Workflow specifics | **Select** |
| B. Care Package | Medium | High | Medium-High | Pricing/Renewal rules | Defer |
| C. Prod Release | Very High | Medium | High | Business readiness | Defer until Option A |
| D. Hardening | Low | High | Low | Print UI format | Fold into next phase |

## Recommended Next Work

**Recommended option:** Option A — Card Reprint / Grave Card Reprint Workflow

**Rationale:** Card Reprint has been explicitly deferred in multiple past phases because it required a stable Payment and Customer Master foundation. Now that Phase 1B.7 (Payment Foundation) is fully closed, the prerequisites are finally met. It is a highly visible, discrete operational flow that exercises the full stack (Customer -> Request -> Workflow -> Payment).

**Why now:** Completing Card Reprint provides a concrete vertical slice of the workflow and payment integration, serving as the final major puzzle piece before a holistic Production Release (Option C) makes sense for the frontline cashiers and service desk.

**Why alternatives are deferred:** Option B (Care Packages) is more complex and currently inactive. Option C (Production Release) should ideally wait until the service desk has complete core capabilities (like re-issuing cards) to avoid hybrid manual/system workarounds on day one. Option D (Hardening) should be bundled into the Option A effort.

**Expected gated sequence:**
1. Post-1B.7 Next-Work Decision (PO)
2. Phase 1B.8 Card Reprint Discovery and Detailed Plan
3. Phase 1B.8 Backend/Data Scope and Implementation
4. Phase 1B.8 Frontend Scope and Implementation
5. Phase 1B.8 Operational Validation and Closure

## Recommended Next Gate

Recommended next authorized task:
Project Owner post-1B.7 next-work decision.

Do not authorize:
- implementation,
- source code changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- production migration,
- release tag,
- push.

## Risks / Open Questions

- Missing business decisions: Confirmation is needed on whether Card Reprint uses a dynamic configurable workflow (via the planned Workflow Engine) or a hardcoded sequential approval for this phase.
- Deferred Phase 1B.7 follow-ups (tests and Payment Print UI) must not be forgotten; they should be explicitly included in the scope of the next phase if possible.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.

## Non-Goals

Confirm this document does not:
- choose next work as Project Owner.
- implement next work.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- run production migration.
- create release tag.
- push.
