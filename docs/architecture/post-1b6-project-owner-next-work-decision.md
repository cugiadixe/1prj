# Post-1B.6 Project Owner Next-Work Decision

## Status

ACCEPTED — POST-1B.6 NEXT WORK SELECTED

## Decision Source

Reference:

- Post-1B.6 next-work recommendation commit:
  853ab3a7fc7ad9229858cea643ecd339c8009ce0

State:
- Phase 1B.6 Service Module Foundation is closed.
- The recommendation document was reviewed.
- This document records Project Owner selection only.
- This document does not authorize implementation.

## Project Owner Decision

The Project Owner selects:

Option A — Payment / Billing / Collection / Reconciliation Foundation

as the next work item after Phase 1B.6.

## Selected Next Work

Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation

## Rationale for Selection

- Payment completes the operational lifecycle from Customer to Service to Payment.
- Closed Phase 1B.6 Service Module Foundation provides the Service dependency needed for Payment.
- Payment is a structural prerequisite for later revenue-generating modules.
- Card Reprint depends on payment collection for reprint fees.
- Care Package Sales depends on payment collection and service lifecycle consistency.
- Deferring Payment would increase dependency risk for later modules.

## Reviewed Alternatives

### Option B — Card Reprint Workflow/Module

Deferred because Payment/Billing foundation should be available before fee-based reprint workflows are implemented.

### Option C — Care Package Sales Workflow/Module

Deferred because Care Package Sales depends on Service and Payment readiness.

### Option D — Production Release and Operational Rollout of 1B.6

Deferred because production migration/release remains outside the current local development gate.

## Scope of Selected Planning Work

The next task should be discovery and detailed planning only for:

- Payment/Billing foundation.
- Collection confirmation.
- One-time full-payment rules.
- No partial payment.
- No refund.
- No cancellation.
- One payment may cover multiple services.
- VND only.
- Manual daily/monthly reconciliation.
- No bank reference code.
- Cashier create and self-confirm rule if still supported by business docs.
- Admin-only edit after confirmed payment.
- Notification after Admin edit.
- Reporting needs.
- Relationship to Service Module Foundation.
- Expected database/API/frontend/workflow boundaries.

Implementation details beyond planning scope are not defined in this decision.

## Boundaries

- Payment implementation is not authorized by this decision.
- Billing implementation is not authorized by this decision.
- Collection implementation is not authorized by this decision.
- Reconciliation implementation is not authorized by this decision.
- Card Reprint implementation is not authorized.
- Care Package Sales implementation is not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.

## Authorization for Next Step

Authorized next task:
Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation discovery and detailed planning only.

Do not authorize:
- implementation,
- database migration creation,
- backend implementation,
- frontend implementation,
- Payment runtime implementation,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

This decision does not:

- modify business requirements,
- modify source code,
- modify tests,
- modify frontend/backend files,
- modify migrations/rollbacks,
- implement Payment,
- implement Card Reprint,
- implement Care Package Sales,
- run production migration,
- create release tag,
- push.

## Notes / Risks

- Local branch may be ahead of origin/main; no push is authorized.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- Production release remains deferred.
- Implementation requires separate planning acceptance after Phase 1B.7 discovery/detailed plan.
- Payment rules must be confirmed from business docs and not invented.
