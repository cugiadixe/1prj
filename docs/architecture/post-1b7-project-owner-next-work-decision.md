# Post-1B.7 Project Owner Next-Work Decision

## Status

ACCEPTED — NEXT WORK SELECTED

## Project Owner Decision

The Project Owner selects Option A — Card Reprint / Grave Card Reprint Workflow as the next work after Phase 1B.7.

This decision is based on the Post-1B.7 Next-Work Selection Discovery and Recommendation.

This decision does not authorize implementation directly.

## Accepted Discovery Source

Reference:

- Post-1B.7 next-work discovery commit:
  db5b60e6e737c0eb83e60dd6e9026bb474b5b75a

- Discovery document:
  docs/architecture/post-1b7-next-work-selection-discovery-and-recommendation.md

## Selected Option

Selected option:

Option A — Card Reprint / Grave Card Reprint Workflow

## Rationale

- Card Reprint / Grave Card Reprint is a long-deferred operational requirement.
- Phase 1B.7 Payment Foundation now provides the required payment dependency for reprint fees.
- Workflow/approval foundation exists and can support approval routing discovery.
- Service and payment foundations make this a suitable next vertical slice.
- Production rollout should remain deferred until at least this visible operational workflow is analyzed and gated.

## Deferred Alternatives

### Option B — Care Package Sales

Deferred because Card Reprint is selected first as the recommended next vertical slice. Care Package Sales remains a viable future candidate.

### Option C — Production Release / Operational Rollout Preparation

Deferred because production release remains gated and should wait until current deferred operational workflows are better understood.

### Option D — Phase 1B.7 Hardening Follow-Ups

Deferred as non-blocking hardening. PaymentCreatePage test, ReconciliationMonthlyPage test, and Payment Print UI remain follow-ups unless later elevated by Project Owner.

## Authorized Next Task

Authorized next task:
Phase 1B.8 Card Reprint / Grave Card Reprint discovery and scope planning only.

Do not authorize:
- implementation,
- source code changes,
- test changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- production migration,
- release tag,
- push.

## Required Phase 1B.8 Discovery Scope

The next discovery/scope planning task must clarify:

- Card Reprint business rules.
- new print vs reprint distinction.
- first print vs second and later print/reprint handling.
- whether reprint fee is fixed/configurable.
- payment dependency and bill/payment flow.
- approval requirement and approval levels.
- physical stamp / physical card handling.
- requester roles.
- approver roles.
- Admin configuration needs.
- customer/grave/site/service references required.
- audit requirements.
- notification requirements.
- permission catalog additions.
- API scope.
- frontend scope.
- database impact.
- test strategy.
- open decisions and blockers.

## Boundaries

Confirm:

- no implementation is authorized by this decision.
- no source code changes are authorized.
- no backend changes are authorized.
- no frontend changes are authorized.
- no migrations/rollbacks are authorized.
- no production migration is authorized.
- no release tag is authorized.
- no push is authorized.
- Card Reprint implementation must wait for discovery, scope acceptance, and Project Owner implementation authorization gates.

## Non-Goals

Confirm this document does not:

- implement Card Reprint.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- run production migration.
- create release tag.
- push.

## Notes

- Phase 1B.7 is closed.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- next task is discovery and scope planning only.
