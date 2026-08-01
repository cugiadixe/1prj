# Phase 1B.5-B Project Owner Backend/Data Scope Acceptance

## Status

ACCEPTED — PHASE 1B.5-B BACKEND/DATA FOUNDATION SCOPE APPROVED FOR IMPLEMENTATION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b5b-backend-data-foundation-scope-and-implementation-plan.md

Planning commit:
c629797341df38b5e39c951fd64363f9708b8bfc

## Accepted Backend/Data Scope

The Project Owner accepts only the backend/data foundation implementation scope defined in the plan, including:

- Customer_Merge_Requests persistence.
- Duplicate candidate search boundaries.
- Source and survivor customer linkage.
- Source customer MERGED marker.
- SurvivorCustomerId / CanonicalCustomerId strategy.
- RowVersion/concurrency checks for source and survivor.
- Merge request lifecycle/status tracking.
- Before/after/survivorship snapshot persistence.
- Append-only merge audit/history.
- CUSTOMER_MERGE workflow execution boundary.
- Approved execution handler.
- Idempotency/double-apply prevention.
- Rejected/non-approved request no mutation.
- API v2 backend endpoints proposed in the plan.
- Backend permission enforcement.
- V0010/U0010 migration and rollback implementation.
- MigrationRollbackTests.
- Unit/Integration/API tests.

## Accepted Blocking Decisions

The plan’s blockers are resolved or accepted as implementation constraints:

1. Overlapping CustomerCompanyContext handling:
   - Adopt the plan’s safe backend default:
     block automatic merge when overlapping company contexts conflict,
     return sanitized validation error,
     require manual resolution before merge execution.

2. Permission catalog changes:
   - Approve repo-controlled permission codes proposed in the plan for backend implementation.
   - Final implementation must update permission code/catalog artifacts only if the accepted architecture requires it and must document the exact changes.

3. Future linked modules impact:
   - For Phase 1B.5-B, implement merge foundation without implementing future service/payment/document modules.
   - Preserve source customer identity and survivor linkage so future modules can resolve merged customers safely.
   - Do not cascade destructive reassignment for future modules in this phase.

## Accepted Non-Blocking Open Questions

The following remain non-blocking for backend/data foundation if implemented with safe defaults:

- Exact survivorship for conflicting single-value fields.
- Merge reversal policy.
- Fuzzy matching for names.
- Detailed approval flow configuration.

Implementation must:
- avoid destructive merge,
- preserve traceability,
- require explicit survivor selection,
- require rowversion/concurrency checks,
- use workflow approval before execution,
- expose sanitized errors only.

## Boundaries

- Backend/data implementation is authorized only after this acceptance commit.
- Frontend implementation is not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.
- Business requirement changes are not authorized.
- Automatic fuzzy merge without review is not authorized.
- Destructive customer deletion is not authorized.
- Service/payment/document module implementation is not authorized.

## Implementation Evidence Required

Future backend/data implementation must provide:

- V0010 migration.
- U0010 rollback.
- MigrationRollbackTests coverage.
- backend build pass.
- UnitTests pass.
- IntegrationTests pass.
- ApiTests pass.
- git diff --check clean.
- implementation report.
- no frontend changes.
- no production migration/tag/push.

## Project Owner Decision

The Project Owner accepts the Phase 1B.5-B backend/data foundation scope and implementation plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.5-B backend/data foundation implementation only.

Do not authorize:
- frontend implementation,
- production migration,
- release tag,
- push.

After implementation, a separate Phase 1B.5-B backend/data implementation report and acceptance review are required before Project Owner implementation acceptance.
