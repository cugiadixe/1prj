# Phase 1B.10 Project Owner Scope Acceptance — Deployment Readiness and Production Migration

## Status

ACCEPTED — PHASE 1B.10 DEPLOYMENT READINESS DISCOVERY/SCOPE ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.10 Deployment Readiness and Production Migration discovery/scope plan.

This acceptance is based on the Phase 1B.10 discovery/scope plan.

This acceptance authorizes only the next planning task:
Phase 1B.10-A Deployment Readiness Remediation Planning.

This acceptance does not authorize source code changes, database migration implementation, remediation implementation, production migration execution, release tag, push, or production readiness claim.

## Accepted Scope Plan

Reference:

- Phase 1B.10 discovery/scope plan commit:
  632b51328f94b3b60d873b5a7e9e41c61ceb1d9b

- Post-Phase 1B.9 Project Owner next-work decision commit:
  5ac87435db82dbc77f1e4897366616dca401ba2b

- Phase 1B.9 Project Owner closure acceptance commit:
  9c1494a94afca423e59ef9691c6b58d8bb5cd6b4

## Accepted Findings

- All 9 core Phase 1B feature slices (1B.1–1B.9) are closed and accepted.
- Deployment readiness blockers remain unresolved across multiple closed phases.
- Production migration has been deferred in every phase closure from 1B.6 through 1B.9.
- 14 migrations (V0001–V0014) and 14 rollbacks (U0001–U0014) exist with 1:1 parity, but no rehearsal or production execution has occurred.
- 13 permission codes in PermissionCodes.cs lack database seed rows.
- 9+ permission codes are missing from docs/business/permission-catalog.md.
- CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE are seeded in V0014; CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT are not seeded.
- All 5 Card Reprint permission codes are not seeded in any migration.
- 3 Workflow permission codes (WORKFLOW_VIEW, WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION) are not seeded.
- ORGANIZATION_USER_MANAGE and CUSTOMER_CHANGE_REQUEST_CREATE are not seeded.
- SELL_CARE_PACKAGE workflow process key is code-only with no migration seed in Business_Process_Catalog.
- Live manual API/UI/lifecycle validation has not been executed for any phase due to environment unavailability.
- No release tag has been created. No push has been performed.

## Accepted Readiness Blockers

1. SQL permission seed alignment — 13 permission codes lack database seed rows.
2. Runtime permission rows — 13 codes cannot be granted to users/roles until seeded.
3. Care Package permission gaps — CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT unseeded.
4. Card Reprint permission seed gaps — all 5 Card Reprint codes unseeded (CARD_REPRINT_REQUEST_CREATE, CARD_REPRINT_REQUEST_VIEW, CARD_REPRINT_APPROVE, CARD_REPRINT_REQUEST_REJECT, CARD_REPRINT_REQUEST_MARK_PRINTED).
5. Workflow permission gaps — WORKFLOW_VIEW, WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION unseeded.
6. SELL_CARE_PACKAGE workflow runtime configuration — not seeded in Business_Process_Catalog.
7. Live manual validation gap — no live API/UI/lifecycle validation executed for any phase.
8. Production migration not executed — all 14 migrations are local-only.

## Accepted Scope Options

- Option A: Discovery complete (this plan — accepted).
- Option B: Deployment readiness remediation planning (next authorized scope).
- Option C: Remediation implementation (requires future plan acceptance).
- Option D: Production migration execution (requires future rehearsal and authorization).
- Option E: Release tag and push (requires future separate authorization).

The Project Owner accepts Option B — Deployment Readiness Remediation Planning — as the next authorized scope.

## Accepted Open Decisions

The following open decisions must be addressed in the next planning task:

1. Permission seed migration scope — whether to create V0015 for the 13 missing permission seeds.
2. SELL_CARE_PACKAGE configuration method — migration seed vs manual admin UI configuration.
3. Permission catalog update — whether to update docs/business/permission-catalog.md for 9+ missing entries.
4. PAYMENT_PRINT constant treatment — whether to add PermissionCodes.cs constant or defer until UI is built.
5. Rehearsal environment — which environment is available for migration rehearsal.
6. Live validation environment — whether a live environment is available for manual validation.
7. Readiness acceptance criteria — what constitutes production readiness acceptance.
8. Release tag gates — whether release tag and push remain separate authorization gates.
9. Migration executor — who performs production migration execution.
10. Manual operational setup acceptance — whether any blocker can be accepted as manual operational setup rather than migration seed.

## Authorization for Next Step

Authorized next task:
Phase 1B.10-A Deployment Readiness Remediation Planning only.

The next task may create only the remediation planning document.

The next task must produce:

docs/architecture/phase-1b10a-deployment-readiness-remediation-plan.md

The next task must:
- Define the remediation plan for permission seed gaps (13 unseeded codes).
- Define the remediation plan for runtime permission rows.
- Define the remediation plan for SELL_CARE_PACKAGE workflow runtime configuration.
- Define the remediation plan for workflow permission gaps.
- Define the remediation plan for Card Reprint permission seed gaps.
- Define migration rehearsal and rollback rehearsal approach.
- Define live validation readiness approach.
- Resolve or list Project Owner decisions needed before implementation.
- Define implementation boundaries and sequencing.
- Define pass/fail criteria for future remediation execution.
- Avoid implementing remediation.

The next task must not:
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.
- Claim production readiness.

## Required Next Output

The next task must produce:

docs/architecture/phase-1b10a-deployment-readiness-remediation-plan.md

## Non-Goals

This acceptance task does not:
- Implement code.
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.
- Claim production readiness.

## Recommended Next Gate

Phase 1B.10-A Deployment Readiness Remediation Planning.
