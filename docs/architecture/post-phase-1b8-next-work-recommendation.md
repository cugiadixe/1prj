# Post-Phase 1B.8 Next-Work Recommendation

## Status

PROPOSED — READY FOR PROJECT OWNER NEXT-WORK DECISION

## Current Phase Status

Phase 1B.8 Card Reprint is closed.

Reference:
- Phase 1B.8 Project Owner closure acceptance commit:
  53a1361339f6763101856acd3b42fe0a2fe9f3e6

## Completed Work Summary

The following major foundation slices have been completed and accepted through Phase 1B.8:
- **Phase 1B.1**: Security Admin foundation.
- **Phase 1B.2**: Customer first slice (Proposals/Basic APIs).
- **Phase 1B.3**: Workflow/Approval engine foundation.
- **Phase 1B.4**: Customer Master Expansion.
- **Phase 1B.5**: Customer Merge.
- **Phase 1B.6**: Service Module Foundation.
- **Phase 1B.7**: Payment / Billing / Collection / Reconciliation Foundation.
- **Phase 1B.8**: Card Reprint.

## Phase 1B.8 Closure Summary

Phase 1B.8 successfully delivered the full vertical slice for Card Reprint Operations. This included the `CardReprintRequest` backend domain, scoped authorization, dynamic integration with the Phase 1B.3 Workflow Engine (for status delegation), automated draft link generation leveraging the Phase 1B.7 Payment Foundation, and a fully interactive React UI for CRUD and approval lifecycle progressions. Complete test automation across 305 API tests, 203 Integration tests, 226 Unit tests, and 481 Frontend Vitest tests guarantees robust bounds enforcement.

## Remaining Deferrals / Known Non-Goals

Documented deferrals and known non-goals include:
- Care Package Sales workflow and service integrations.
- Dynamic PDF/template generation for printable outputs.
- Generic Payment Print UI components.
- Physical inventory/stamp stock management.
- Refunds, cancellations, and partial payments logic.
- Standalone manual-click UAT environments populated with integrated mock data.
- Production migration, branch merging, and release tagging.

## Candidate Next Work Items

| Candidate | Source/Evidence | Business Value | Dependencies | Risks | Suggested Gate | Recommendation |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Phase 1B.9 Care Package Sales** | Deferred repeatedly in 1B.8 constraints | High (Expands revenue module suite) | 1B.6 Services, 1B.7 Payments | Requires workflow adaptation | Discovery / Scope | **Recommended next** |
| **Phase 1B.10 Production Migration & Release** | Deferred in all Phase closures | Critical (Unlocks user value) | Core 1B features stable | Data migration limits | Discovery / Scope | Viable but should be later |
| **Dynamic PDF / Template Generation** | 1B.8 Deferral list | Medium (Operational efficiency) | Card Reprint / Core modules | High layout variance | Discovery / Scope | Needs discovery first |
| **Refunds & Cancellations** | 1B.7 & 1B.8 Deferral lists | Medium (Operational flexibility) | 1B.7 Payments | Complex accounting logic | Discovery / Scope | Needs discovery first |
| **Physical Inventory Management** | 1B.8 Deferral list | Medium (Operations) | Unknown | Domain complexity | Discovery / Scope | Not recommended now |

## Recommended Next Work

**Recommended phase:**
Phase 1B.9 Care Package Sales

**Recommended first gate:**
Phase 1B.9 discovery/scope planning only

**Rationale:**
Care Package Sales is the most frequently cited functional deferral throughout the 1B.8 module constraints. With the 1B.6 Service module, 1B.3 Workflow module, and 1B.7 Payment foundation now successfully integrated together during Card Reprint, the architectural pattern for delivering composite service-payment-workflow features is proven. Tackling Care Package Sales next capitalizes on this established architectural momentum.

**Prerequisites:**
- Accepted B1, B2, C, D closures for 1B.6, 1B.7, and 1B.8.

**Risks:**
- Service catalog mapping nuances may require workflow model extensions.

**Non-goals for first gate:**
- Implementation, production migration, or tagging.

## Not Recommended Now

- **Physical Inventory / Stamp Stock Management**: Domain boundaries are currently undefined and highly disconnected from the core digital service approvals recently built.
- **Production Migration**: Should be deferred until the final functional slice of the 1B milestone (Care Package Sales) is complete to ensure a unified release.

## Authorization Boundary

This document is a recommendation only.

It does not authorize:
- source code changes,
- backend implementation,
- frontend implementation,
- database migrations,
- business docs changes,
- permission catalog changes,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Required Project Owner Decision

A Project Owner next-work decision is required before any new phase planning or implementation begins.

Recommended decision document:
docs/architecture/post-phase-1b8-project-owner-next-work-decision.md

## Recommended Next Gate

Project Owner post-Phase 1B.8 next-work decision.
