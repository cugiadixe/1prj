# Phase 1B.3 Post-B5 Project Owner Next-Work Decision

## Status

ACCEPTED — PHASE 1B.4 CUSTOMER MASTER EXPANSION SELECTED

## Decision Baseline

- Post-B5 next-work selection and recommendation commit:
  d52701cecb0174b1c2ed023c487b532abbaa0822
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

## Project Owner Decision

The Project Owner accepts the post-B5 recommendation and selects:

Phase 1B.4 — Customer Master Expansion

## Decision Rationale

Summarize from the recommendation document:
- Phase 1B.3-B5 Workflow Pilot Hardening is complete.
- Customer Master Expansion is the most suitable immediate next phase.
- It builds on the accepted workflow runtime hardening safely.
- It continues the Customer domain before moving to Service/Payment.
- It has stronger documented business basis than the larger downstream modules.
- It avoids premature production release or broad module expansion.

## Selected Scope for Phase 1B.4 Discovery and Detailed Planning

Authorize discovery and detailed planning for Customer Master Expansion only.

Planning may analyze:
- CUSTOMER_MASTER_CHANGE workflow expansion.
- Customer data correction/change proposal flow.
- Customer master data governance.
- Critical customer fields and protected fields.
- Permission and approval requirements for customer master changes.
- Customer duplicate/merge readiness, only as discovery unless separately approved.
- Dependencies on completed B5 workflow hardening.
- Database/API/frontend planning boundaries.
- Test strategy and acceptance criteria.
- Open decisions requiring Project Owner approval.

## Explicitly Not Authorized Yet

This decision does not authorize:
- Phase 1B.4 implementation.
- Source code changes.
- Frontend changes.
- Backend changes.
- Test changes.
- Migration changes.
- Rollback changes.
- Database script changes.
- PermissionCodes.cs changes.
- business-rules.md changes.
- permission-catalog.md changes.
- acceptance-criteria.md changes.
- Production migration.
- Production release.
- Release tag.
- Push.
- Service module.
- Payment module.
- Card flow.
- Plot flow.
- ENTITY expansion.
- Export/download.
- Safe user lookup/reassign expansion.
- Broad workflow engine rewrite.
- Broad frontend redesign.

## Required Phase 1B.4 Planning Deliverable

Authorize creation of:

docs/architecture/phase-1b4-customer-master-expansion-discovery-and-detailed-plan.md

The plan must identify:
- confirmed business scope,
- explicit non-scope,
- decisions already approved,
- decisions still missing,
- database impact,
- API v2 impact,
- frontend impact,
- permission/security impact,
- workflow/approval impact,
- migration/rollback strategy if needed,
- automated test strategy,
- manual validation strategy,
- implementation phasing recommendation,
- stop conditions.

## Next Authorized Step

Project Owner authorizes:

Phase 1B.4 — Customer Master Expansion discovery and detailed planning only.

Do not implement Phase 1B.4 until the detailed plan is created, reviewed, and separately accepted by Project Owner.

## Conclusion

PHASE 1B.4 CUSTOMER MASTER EXPANSION SELECTED — READY FOR DISCOVERY AND DETAILED PLANNING
