# Phase 1B.6-D Operational Validation and Closure Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE EXECUTION

## Authorization Source

Reference:
- Phase 1B.6-C PO frontend implementation acceptance commit:
  d308773298aaca2f7f89d7ac46d8fd8ebf8c97d4

State:
- Phase 1B.6-B backend/data implementation is accepted.
- Phase 1B.6-C frontend implementation is accepted.
- This document plans operational validation and closure only.
- This document does not authorize execution.

## Objective

Define the operational validation and closure approach for Phase 1B.6 Service Module Foundation.

## Source Documents Reviewed

- docs/architecture/phase-1b6c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b6c-frontend-implementation-acceptance-review.md
- docs/architecture/phase-1b6c-frontend-implementation-report.md
- docs/architecture/phase-1b6c-project-owner-frontend-scope-acceptance.md
- docs/architecture/phase-1b6c-frontend-scope-and-implementation-plan.md
- docs/architecture/phase-1b6b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b6b-backend-data-foundation-implementation-acceptance-review.md
- docs/architecture/phase-1b6b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b6b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b6b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b6-project-owner-scope-acceptance.md
- docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md

## Accepted Scope Summary

Summarized accepted Phase 1B.6 scope:
- V0011/U0011.
- Service_Types.
- Service_Price_History.
- Services.
- Service_History.
- ServiceTypeService.
- ServiceService.
- ServicePriceOverrideExecutionHandler.
- service type API v2 controller.
- service API v2 controller.
- SERVICE_* permissions.
- frontend API clients.
- service type pages.
- service pages.
- price snapshot display.
- lifecycle/status display.
- SERVICE_PRICE_OVERRIDE workflow boundary UI.
- route/navigation wiring.
- permission-gated UI.
- tests.

## Automated Backend Validation Plan

- `dotnet build src/backend/PTKD-ERP.sln`
- `dotnet test tests/backend/PTKD.UnitTests/`
- `dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false`
- `dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false`

All commands must pass with 0 failures.

## Automated Frontend Validation Plan

- `npm run lint`
- `npx tsc -b`
- `npm run test`
- Targeted Service Module frontend tests covering the 9 Service Module test files

All commands must pass with 0 errors (fast-refresh warnings are allowed per prior review). Tests must fully pass.

## Database / Migration Validation Plan

- V0011 migration applies cleanly.
- U0011 rollback works.
- MigrationRollbackTests cover V0011/U0011.
- DbMigrator owns SchemaVersions.
- U0011 handles V0011 SchemaVersions cleanup according to repository convention.
- U0011 soft-deactivates SERVICE_* permissions where required.
- TestDatabaseFixture reset target is V0011.
- SafeTestWebApplicationFactory reset target is V0011.
- PTKD_TEST_PHASE1A2 test DB only.
- No production migration.

## Manual / Operational Checklist

1. Service Type list page loads. [PENDING]
2. Service Type detail page loads. [PENDING]
3. Service Type create form works within accepted scope. [PENDING]
4. Service Type edit form works within accepted scope. [PENDING]
5. Service Type GLOBAL permission behavior is safe. [PENDING]
6. Service list page loads. [PENDING]
7. Service detail page loads. [PENDING]
8. Standard service creation form works within accepted scope. [PENDING]
9. Standard service renewal dialog works within accepted scope. [PENDING]
10. Price snapshot displays correctly. [PENDING]
11. Lifecycle/status display is safe. [PENDING]
12. Invalid lifecycle transition is sanitized. [PENDING]
13. Inactive service type is handled safely. [PENDING]
14. Invalid customer/company is sanitized. [PENDING]
15. Permission denied is sanitized. [PENDING]
16. Not found is sanitized. [PENDING]
17. Validation failure is sanitized. [PENDING]
18. Stale rowversion/concurrency error is sanitized. [PENDING]
19. Generic server failure is sanitized. [PENDING]
20. No raw SQL/internal exception is displayed. [PENDING]
21. No stack trace is displayed. [PENDING]
22. No raw sensitive payload is displayed. [PENDING]
23. SERVICE_PRICE_OVERRIDE workflow boundary displays safely. [PENDING]
24. SERVICE_PRICE_OVERRIDE does not expand into unrelated workflow scope. [PENDING]
25. Backend authorization remains authoritative. [PENDING]
26. Frontend gating is convenience only. [PENDING]
27. No Payment UI/API workflow is exposed. [PENDING]
28. No billing/collection/reconciliation workflow is exposed. [PENDING]
29. No Card Reprint UI/workflow is exposed. [PENDING]
30. No SELL_CARE_PACKAGE / Care Package Sales UI/workflow is exposed. [PENDING]
31. V0011 migration evidence is present. [PENDING]
32. U0011 rollback evidence is present. [PENDING]
33. ResetToV0011 test fixture behavior is validated. [PENDING]
34. PTKD_TEST_PHASE1A2 is the only test DB used. [PENDING]

*Result status (PASSED, NOT EXECUTED with reason, FAILED with reason) will be updated upon execution.*

## Security and Data Exposure Validation Plan

Confirm planned validation for:
- backend authorization authoritative,
- frontend gating convenience only,
- no raw SQL/internal exception display,
- no stack traces,
- no raw sensitive payload exposure,
- sanitized errors,
- no Payment scope exposure,
- no Card Reprint scope exposure,
- no Care Package Sales scope exposure.

## Repository Hygiene Validation Plan

- `git diff --check`
- `git status`
- no tracked modifications except report when execution happens
- no staged scratch files
- no tag
- no push
- no production migration
- branch/remotes status recorded

## Closure Criteria

Phase 1B.6 may proceed to closure acceptance review only if:

- backend build passes,
- UnitTests pass,
- IntegrationTests pass,
- ApiTests pass,
- frontend lint passes,
- TypeScript passes,
- full Vitest passes,
- targeted Service Module frontend tests pass,
- git diff --check clean,
- operational checklist has no blocking FAILED item,
- no production migration/tag/push,
- closure report is created.

## Risks / Follow-Ups

- SERVICE_PRICE_OVERRIDE workflow boundary needs operational validation.
- operational browser validation may be limited by available environment.
- Payment remains deferred.
- Card Reprint remains deferred.
- Care Package Sales remains deferred.
- future migrations must update test fixture reset target beyond V0011.
- untracked scratch/decompiled/FixStrategy files remain and must not be staged.
- branch may be ahead of origin/main; no push is authorized.
- production release remains deferred.

## Recommended Next Gate

Project Owner acceptance of this Phase 1B.6-D operational validation and closure plan.

## Project Owner Approval Required

This plan does not authorize operational validation execution.
Operational validation execution may begin only after Project Owner accepts this Phase 1B.6-D plan.
