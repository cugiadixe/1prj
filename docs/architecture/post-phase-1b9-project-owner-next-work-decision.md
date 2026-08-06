# Post-Phase 1B.9 Project Owner Next-Work Decision

## Status

SELECTED — PHASE 1B.10 DEPLOYMENT READINESS AND PRODUCTION MIGRATION DISCOVERY/SCOPE PLANNING AUTHORIZED

## Project Owner Decision

The Project Owner selects Phase 1B.10 Deployment Readiness and Production Migration as the next work item after Phase 1B.9 Care Package Sales closure.

This decision is based on the post-Phase 1B.9 next-work recommendation.

This decision authorizes only the next planning task:
Phase 1B.10 Deployment Readiness and Production Migration discovery/scope planning.

This decision does not authorize source code changes, database migrations, production migration execution, release tag, push, or production readiness claim.

## Accepted Recommendation Source

Reference:

- Post-Phase 1B.9 next-work recommendation commit:
  2b51c57e981fde840b4483516628e2e4c2361f45

- Phase 1B.9 Project Owner closure acceptance commit:
  9c1494a94afca423e59ef9691c6b58d8bb5cd6b4

## Selected Next Work

Selected:
Phase 1B.10 Deployment Readiness and Production Migration.

First authorized task:
Phase 1B.10 Deployment Readiness and Production Migration discovery/scope planning only.

Required output:
docs/architecture/phase-1b10-deployment-readiness-and-production-migration-discovery-and-scope-plan.md

## Selection Rationale

- Phase 1B.9 Care Package Sales is closed with deployment readiness notes.
- All core Phase 1B feature slices (1B.1 Security Admin, 1B.2 Customer, 1B.3 Workflow/Approval, 1B.4 Customer Master Expansion, 1B.5 Customer Merge, 1B.6 Service Module, 1B.7 Payment Foundation, 1B.8 Card Reprint, 1B.9 Care Package Sales) are complete and accepted.
- Deployment readiness blockers have accumulated across multiple phase closures without resolution.
- Production migration has been repeatedly deferred in Phase 1B.7, 1B.8, and 1B.9 closures.
- Resolving deployment readiness before new feature work reduces deployment risk and unlocks user value from all completed features.
- The post-Phase 1B.8 recommendation noted production migration should follow the final functional slice; Phase 1B.9 was that final slice and is now closed.

## Carried-Forward Deployment Readiness Items

The following deployment readiness blockers are carried forward from Phase 1B.9 closure:

1. **SQL permission seed alignment** — Care Package permission codes (CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT) exist as code constants but database permission seed rows must be confirmed or added before runtime permission gating functions in production.

2. **Runtime permission rows** — All 5 care package permission codes must be grantable to users/roles at runtime. Depends on SQL permission seed alignment.

3. **SELL_CARE_PACKAGE workflow runtime configuration** — Workflow process configuration must be administratively established via workflow admin UI before approval-required path operations function at runtime.

4. **Live manual API/UI/lifecycle validation** — Not executed during Phase 1B.9-D due to environment unavailability. Must be performed in a suitable environment before deployment readiness.

These items do not reopen Phase 1B.9 closure. They block any production/deployment readiness claim until resolved or separately accepted.

## Boundaries for Phase 1B.10 Discovery/Scope Planning

Authorized:
- Review accepted closures and readiness blockers across all completed phases (1B.1–1B.9).
- Inventory production migration prerequisites.
- Inventory runtime permission seed requirements across all modules.
- Inventory workflow runtime configuration requirements.
- Inventory deployment readiness risks.
- Propose Phase 1B.10 scope.
- Identify decisions required before implementation or migration execution.
- Create discovery/scope plan document.

Not authorized:
- Source code changes.
- Backend/frontend implementation.
- Database migration implementation.
- Running migrations.
- Production migration execution.
- Release tag.
- Push.
- Production readiness claim.
- Business docs changes.
- Permission catalog changes.

## Required First Output

The next task must produce:

docs/architecture/phase-1b10-deployment-readiness-and-production-migration-discovery-and-scope-plan.md

The plan must include:
- Confirmed deployment readiness scope.
- Carried-forward blockers from all completed phases.
- Production migration prerequisites.
- Runtime permission seed inventory.
- Workflow runtime configuration inventory.
- Operational validation / live validation needs.
- Environment assumptions.
- Required Project Owner decisions.
- Non-goals.
- Recommended next gate.

## Non-Goals

This decision task does not:
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

Phase 1B.10 Deployment Readiness and Production Migration discovery/scope planning.
