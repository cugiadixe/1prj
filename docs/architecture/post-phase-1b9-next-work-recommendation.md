# Post-Phase 1B.9 Next-Work Recommendation

## Status

PROPOSED — READY FOR PROJECT OWNER NEXT-WORK DECISION

## Authorization Source

Reference:
- Phase 1B.9 Project Owner closure acceptance commit:
  9c1494a94afca423e59ef9691c6b58d8bb5cd6b4

## Current Project Status

Phase 1B.9 Care Package Sales is closed with deployment readiness notes.

Completed and accepted major foundation slices through Phase 1B.9:
- Phase 1B.1: Security Admin foundation.
- Phase 1B.2: Customer first slice (Proposals/Basic APIs).
- Phase 1B.3: Workflow/Approval engine foundation.
- Phase 1B.4: Customer Master Expansion.
- Phase 1B.5: Customer Merge.
- Phase 1B.6: Service Module Foundation.
- Phase 1B.7: Payment / Billing / Collection / Reconciliation Foundation.
- Phase 1B.8: Card Reprint.
- Phase 1B.9: Care Package Sales.

Production readiness is not claimed. Production migration, release tag, and push remain unauthorized.

## Phase 1B.9 Carried-Forward Deployment Readiness Notes

The following deployment readiness blockers are carried forward from Phase 1B.9 closure:

1. **SQL permission seed alignment** — Care Package permission codes (CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT) exist as code constants but database permission seed rows must be confirmed or added before runtime permission gating functions in production.

2. **Runtime permission rows** — All 5 care package permission codes must be grantable to users/roles at runtime. Depends on SQL permission seed alignment.

3. **SELL_CARE_PACKAGE workflow runtime configuration** — Workflow process configuration must be administratively established via workflow admin UI before approval-required path operations function at runtime.

These items do not reopen Phase 1B.9 closure. They block any production/deployment readiness claim until resolved or separately accepted.

## Phase 1B.9 Carried-Forward Non-Blocking Follow-Ups

- Manual ID selector UX for customer/grave.
- Stale frontend status / backend 409 safe handling follow-up.
- Care target selector/search UX improvement.
- Live manual API/UI/lifecycle validation in a suitable environment before deployment readiness.
- No report/export UI in Phase 1B.9.
- No generic Payment Print UI in Phase 1B.9.
- No dynamic PDF/template generation in Phase 1B.9.

## Candidate Next Work Items

### Candidate 1: Phase 1B.10 Deployment Readiness and Production Migration

**Type**: Readiness / operational hardening slice.

**Source basis**: Deployment readiness blockers carried forward from Phase 1B.9 closure (and partially from Phase 1B.8 closure). Production migration has been deferred in every prior phase closure (1B.7, 1B.8, 1B.9). The post-Phase 1B.8 recommendation noted production migration as "viable but should be later" and recommended completing Care Package Sales first — that is now done.

**Value**: Resolves all accumulated deployment blockers across phases 1B.1–1B.9. Unlocks user value by enabling production deployment. Addresses SQL permission seed alignment for Care Package and any other modules. Addresses SELL_CARE_PACKAGE workflow runtime configuration. Enables live manual validation in a production-like environment.

**Dependencies**: All core 1B features (1B.1–1B.9) are complete and accepted. Branch `feature/phase-1-organization` is stable.

**Risks**: Data migration complexity. Environment configuration. Requires business/operational decisions about deployment timeline, data seeding, and user onboarding.

**First expected gate**: Phase 1B.10 deployment readiness discovery/scope planning.

**First output**: `docs/architecture/phase-1b10-deployment-readiness-discovery-and-scope-plan.md`

### Candidate 2: Phase 1B.9-E Care Package Deployment Readiness Resolution

**Type**: Readiness / remediation slice (scoped to Phase 1B.9 blockers only).

**Source basis**: Phase 1B.9 closure deployment readiness notes. Three specific blockers: SQL permission seeds, runtime permission rows, SELL_CARE_PACKAGE workflow config.

**Value**: Resolves Phase 1B.9-specific deployment blockers without addressing broader production migration.

**Dependencies**: Phase 1B.9 closure accepted.

**Risks**: Narrow scope — resolves only Care Package blockers while similar blockers may exist across other modules. May duplicate work if a broader deployment readiness phase follows.

**First expected gate**: Phase 1B.9-E deployment readiness planning.

**First output**: `docs/architecture/phase-1b9e-care-package-deployment-readiness-plan.md`

### Candidate 3: Dynamic PDF / Template Generation

**Type**: Feature slice.

**Source basis**: Deferred in Phase 1B.8 and Phase 1B.9 closure notes. Referenced in process catalog (printable outputs).

**Value**: Medium — operational efficiency for printed documents (receipts, cards, reports).

**Dependencies**: Core modules must be stable. Layout/template business requirements are undefined.

**Risks**: High layout variance. Business requirements need discovery. Template engine selection.

**First expected gate**: Discovery/scope planning.

**First output**: `docs/architecture/phase-1b10-pdf-template-discovery-and-scope-plan.md`

### Candidate 4: Refunds and Cancellations

**Type**: Feature slice.

**Source basis**: Deferred in Phase 1B.7 and Phase 1B.8 closure notes. Payment Foundation currently enforces no-refund/no-cancellation constraints.

**Value**: Medium — operational flexibility for payment corrections beyond the current PAYMENT_CORRECT_CONFIRMED path.

**Dependencies**: Payment Foundation (1B.7). Complex accounting/reconciliation logic. Business rules undefined.

**Risks**: High accounting complexity. Reconciliation impact across all payment-integrated modules.

**First expected gate**: Discovery/scope planning.

### Candidate 5: Report/Export UI

**Type**: Feature slice.

**Source basis**: Deferred as non-blocking follow-up in Phase 1B.9 closure. CARE_PACKAGE_REPORT_VIEW referenced as future/reporting candidate in Phase 1B.9-A detailed scope acceptance.

**Value**: Medium — operational visibility for care package and other module data.

**Dependencies**: Core modules stable. Report requirements need business specification.

**Risks**: Report scope and format undefined.

**First expected gate**: Discovery/scope planning.

## Candidate Ranking

| Rank | Candidate | Rationale |
|------|-----------|-----------|
| 1 | **Phase 1B.10 Deployment Readiness and Production Migration** | All 9 core 1B feature slices are complete. Production migration has been explicitly deferred in every closure since 1B.7. Resolving deployment readiness — including Phase 1B.9 blockers (permission seeds, workflow config) and broader production migration — is now the highest-urgency work. Continuing to defer deployment means all delivered features remain inaccessible to users. This candidate subsumes Candidate 2 (Phase 1B.9-E) by addressing all deployment blockers comprehensively. |
| 2 | Phase 1B.9-E Care Package Deployment Readiness | Valid but narrow. If the Project Owner prefers to resolve only Phase 1B.9 blockers before broader production migration, this is an option. However, it risks duplicating effort if a broader deployment phase follows shortly after. |
| 3 | Dynamic PDF / Template Generation | Valid future candidate but business requirements are undefined. Discovery needed before implementation planning. Lower urgency than deployment readiness. |
| 4 | Refunds and Cancellations | Valid future candidate but high complexity and undefined business rules. Should wait until core modules are deployed and operational feedback is available. |
| 5 | Report/Export UI | Valid future candidate but report specifications are undefined. Lower urgency than deployment readiness. |

## Recommended Next Work

**Recommended phase**: Phase 1B.10 Deployment Readiness and Production Migration

**Recommended first gate**: Phase 1B.10 deployment readiness discovery/scope planning only.

**Why recommended**: All 9 core 1B feature slices (Security Admin, Customer, Workflow, Customer Master Expansion, Customer Merge, Service, Payment, Card Reprint, Care Package Sales) are complete and accepted. Production migration has been explicitly deferred in every phase closure. The accumulated deployment readiness blockers — SQL permission seeds, runtime permission rows, workflow runtime configuration, and production environment preparation — represent the most urgent remaining work. Continuing to build new feature slices while deferring deployment means all delivered value remains inaccessible to users.

**Why now**: Phase 1B.9 Care Package Sales was the last functional slice recommended before production migration in the post-Phase 1B.8 next-work recommendation. With Care Package Sales now complete, the original rationale for deferring production migration ("should be deferred until the final functional slice of the 1B milestone is complete to ensure a unified release") no longer applies.

**First authorized task if selected**: Phase 1B.10 Deployment Readiness and Production Migration discovery/scope planning.

**Required first output if selected**: `docs/architecture/phase-1b10-deployment-readiness-discovery-and-scope-plan.md`

**Boundaries and non-goals for first gate**:
- No implementation.
- No source code changes.
- No database migrations.
- No production migration.
- No release tag.
- No push.
- The discovery/scope plan must identify all deployment readiness blockers across all completed phases (1B.1–1B.9), not only Phase 1B.9 blockers.
- The plan must address permission seed alignment, workflow runtime configuration, data migration, environment preparation, and deployment sequencing.
- The plan must not invent business requirements.

## Not Recommended Now

- **Phase 1B.9-E Care Package Deployment Readiness Resolution**: Narrow scope addresses only Care Package blockers. A broader deployment readiness phase (1B.10) subsumes this work and avoids duplication. Recommended only if the Project Owner explicitly prefers incremental readiness over comprehensive deployment planning.

- **Dynamic PDF / Template Generation**: Business requirements for templates/layouts are undefined. Discovery is needed but is lower urgency than deployment readiness. Recommended after deployment readiness is resolved.

- **Refunds and Cancellations**: High-complexity accounting work with undefined business rules. Should wait until core modules are deployed and operational feedback informs the scope. Recommended after deployment readiness and operational experience.

- **Report/Export UI**: Report specifications are undefined. Lower urgency than deployment readiness. Recommended after deployment readiness is resolved.

## Required Project Owner Decision

The Project Owner must explicitly select the next work item before any new phase/slice begins.

This recommendation does not authorize any implementation, source changes, migrations, or deployment actions.

## Recommended Authorization Wording

If the Project Owner accepts the recommendation:

> The Project Owner selects Phase 1B.10 Deployment Readiness and Production Migration as the next work item after Phase 1B.9 Care Package Sales closure.
>
> This decision authorizes only the next planning task:
> Phase 1B.10 Deployment Readiness and Production Migration discovery/scope planning.
>
> This decision does not authorize implementation, source code changes, database migrations, production migration, release tag, or push.
>
> The next task must produce:
> docs/architecture/phase-1b10-deployment-readiness-discovery-and-scope-plan.md

## Boundaries

- No source code changes.
- No backend/frontend implementation.
- No database migrations.
- No business docs changes.
- No permission catalog changes.
- No production migration.
- No release tag.
- No push.
- No production readiness claim.

## Recommended Next Gate

Project Owner post-Phase 1B.9 next-work decision.
