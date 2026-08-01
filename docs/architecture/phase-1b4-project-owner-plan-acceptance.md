# Phase 1B.4 Customer Master Expansion Project Owner Plan Acceptance

## Status

ACCEPTED — PHASE 1B.4 CUSTOMER MASTER EXPANSION PLAN APPROVED

## Accepted Plan

- Phase 1B.4 discovery and detailed plan commit:
  4dec520d41fc1ad6de9ec4b25a50415b179f2d0c
- Phase 1B.4 Project Owner selection commit:
  420f76df3d37218c47d98168923b5fa559fc78d9
- Post-B5 next-work selection commit:
  d52701cecb0174b1c2ed023c487b532abbaa0822
- B5-D Project Owner closure acceptance commit:
  0a4149fb233c516210acba197a8b2977cbc39170

## Project Owner Decision

The Project Owner accepts the Phase 1B.4 Customer Master Expansion discovery and detailed plan.

## Accepted Planning Scope

Confirm acceptance of the planning scope:
- Customer master data governance.
- Staff proposal model.
- Customer data admin official update model.
- Shared customer data across companies.
- Protected/critical customer fields planning.
- CUSTOMER_MASTER_CHANGE workflow planning.
- Customer proposal lifecycle planning.
- Permission/security planning.
- Database/API/frontend impact planning.
- Workflow/approval impact planning.
- Test and manual validation planning.
- Risk, blocker, and stop-condition planning.

## Accepted Implementation Direction

Authorize the next implementation step only as a bounded Phase 1B.4 backend/data foundation preparation.

Next authorized step:
Phase 1B.4-B — Customer Master Backend/Data Foundation Scope Authorization and Implementation Plan

This next step may prepare a detailed backend/data implementation scope for Project Owner approval.

It may analyze:
- target database tables and migrations,
- rollback strategy,
- domain/application/API boundaries,
- permission codes required,
- workflow registration requirements,
- test coverage requirements,
- risks and stop conditions.

Do not implement backend/data code until that bounded scope is reviewed and accepted.

## Decisions Accepted as Planning Basis

Confirm the following are accepted as planning basis, subject to exact implementation approval later:
- B5 workflow runtime hardening is reusable for Customer Master Expansion.
- Customer master changes should follow proposal/approval governance where protected data is involved.
- Official customer data updates require controlled authority, not unrestricted staff edits.
- Backend authorization must remain authoritative.
- Sensitive before/after data must be protected.
- Audit/history is required.
- Rowversion/concurrency handling must be considered.
- Migration/rollback must be planned before schema changes.

## Open Decisions Carried Forward

Confirm open decisions remain to be finalized before implementation:
- exact CUSTOMER_MASTER_CHANGE trigger and process boundary,
- protected field list confirmation,
- duplicate detection behavior,
- whether customer merge remains discovery-only or enters scope later,
- exact approval flow assignment,
- final permission code names,
- audit payload field set,
- UI scope and screen list,
- migration boundaries,
- manual validation data constraints.

## Explicitly Not Authorized

This acceptance does not authorize:
- Phase 1B.4 implementation.
- Backend source changes.
- Frontend source changes.
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
- Payment module.
- Service module.
- Card print/reprint flow.
- Plot/cemetery location flow.
- ENTITY expansion.
- Export/download.
- Safe user lookup/reassign expansion.
- Customer merge implementation unless separately approved.
- Broad workflow engine rewrite.
- Broad frontend redesign.

## Required Next Deliverable

Authorize creation of:

docs/architecture/phase-1b4b-backend-data-foundation-scope-and-implementation-plan.md

This deliverable must be documentation-only and must define:
- exact backend/data scope,
- exact migration proposal if needed,
- rollback strategy,
- API v2 backend endpoints,
- application service boundaries,
- workflow handler boundaries,
- permission codes,
- security and data exposure rules,
- test strategy,
- acceptance criteria,
- stop conditions,
- implementation file list proposal.

## Next Authorized Step

Project Owner authorizes:

Phase 1B.4-B — Customer Master Backend/Data Foundation Scope Authorization and Implementation Plan

Planning only.

Do not implement Phase 1B.4-B until the backend/data foundation scope and implementation plan is created, reviewed, and separately accepted by Project Owner.

## Conclusion

PHASE 1B.4 CUSTOMER MASTER EXPANSION PLAN ACCEPTED — READY FOR BACKEND/DATA FOUNDATION SCOPE AND IMPLEMENTATION PLANNING
