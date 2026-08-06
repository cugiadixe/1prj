# Phase 1B.9-B1 Project Owner Backend/Data Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-B1 CARE PACKAGE SALES BACKEND/DATA ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-B1 Care Package Sales backend/data foundation.

This acceptance is based on the B1 implementation report and the backend/data acceptance review.

The acceptance review passed with non-blocking notes and found no blocking issues.

This acceptance authorizes only the next implementation slice:
Phase 1B.9-B2 Care Package Sales workflow/payment integration.

This acceptance does not authorize frontend implementation, production migration, release tag, or push.

## Accepted B1 Implementation

Reference:

- Phase 1B.9-B1 backend/data acceptance review commit:
  d606c3b1a309215307de8f7b1b3ec66ae74a544d

- Phase 1B.9-B1 backend/data implementation commit:
  c28e7d5b65ac902f80a51c92121352e5ec1fc70c

- Phase 1B.9-B Project Owner implementation plan acceptance commit:
  e3d8beddd656c4ce2d2846f91e6a3531083b202e

## Accepted Backend/Data Scope

- V0014/U0014 Care Package Sales foundation migration/rollback.
- CarePackageRequest and CarePackageRequestItem domain entities.
- EF mappings and AppDbContext integration.
- application DTOs and service foundation.
- list/detail/create APIs under `/api/v2/care-packages`.
- backend-calculated pricing snapshot foundation.
- company-scope authorization.
- B1 backend/domain/integration/API tests.
- B1 implementation report.

## Acceptance Review Summary

- B1 acceptance review passed with notes.
- no blockers were found.
- validation passed with:
  - build 0 errors, 9 warnings.
  - UnitTests 235 passed.
  - IntegrationTests 203 passed.
  - ApiTests 308 passed.
  - git diff --check clean.
- no frontend files were changed.
- no business docs were changed.
- no permission catalog changes were made.
- no production migration/tag/push occurred.

## Non-Blocking Notes Accepted

The Project Owner accepts the non-blocking note that V0014 seeds `CARE_PACKAGE_VIEW` and `CARE_PACKAGE_CREATE` for B1 backend authorization needs.

This acceptance does not modify `docs/business/permission-catalog.md`.

Any future permission catalog alignment must be handled only under a separately authorized task or implementation slice.

## Authorization for Next Step

Authorized next task:
Phase 1B.9-B2 Care Package Sales workflow/payment integration implementation only.

The next task may implement only the workflow/payment integration slice.

Authorized B2 scope:
- integrate `SELL_CARE_PACKAGE` workflow where approval is required.
- support no-approval path for configured-price/no-discount requests.
- implement submit action for approval-required requests.
- implement approve/reject workflow facades that delegate to WorkflowRuntimeService.
- synchronize domain state only after successful workflow action.
- prevent rejected requests from proceeding to payment.
- implement payment eligibility guards.
- implement create payment draft/bill only when payment-eligible.
- implement read-only payment status endpoint.
- implement active status after confirmed payment if supported by Payment Foundation conventions.
- enforce no hard-coded care package price.
- enforce Payment Foundation constraints:
  - VND only.
  - full payment only.
  - no partial payment.
  - no refund.
  - no cancellation.
  - one bill cannot be paid multiple times.
- add/update backend/domain/integration/API tests for B2.
- create B2 implementation report.

B2 must not implement:
- frontend pages/components.
- Phase 1B.9-C frontend work.
- production migration.
- release tag.
- push.
- dynamic PDF/template generation.
- generic Payment Print UI.
- refund.
- cancellation.
- partial payment.
- physical inventory/stamp stock management.
- multi-year packages.
- partial-year packages.
- discount percent UI.
- dedicated report/export UI.

## Required B2 Implementation Report

The next task must produce:

docs/architecture/phase-1b9b2-care-package-sales-workflow-payment-implementation-report.md

The report must include:
- implemented files.
- workflow integration summary.
- no-approval path summary.
- approve/reject facade summary.
- payment eligibility summary.
- create-payment summary.
- payment-status summary.
- active-status summary if implemented.
- authorization/company-scope summary.
- tests added/updated.
- validation evidence.
- boundary confirmation.
- known risks/follow-ups.
- explicit statement of frontend deferral to Phase 1B.9-C.

## Required B2 Validation

Future B2 implementation must run and record:

Backend:
- dotnet build src/backend/PTKD-ERP.sln
- dotnet test tests/backend/PTKD.UnitTests/
- dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
- dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false

Repository:
- git diff --check

Frontend validation is not required for B2 unless frontend files are changed, which they should not be.

## Non-Goals

- implement code.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- run production migration.
- create release tag.
- push.

## Notes

- Phase 1B.9-B1 backend/data foundation is accepted.
- Phase 1B.9-B2 implementation has not started in this acceptance task.
- implementation may begin only in the next B2 task and only within the accepted B2 scope.
- local branch may be ahead of origin; no push is authorized.
- production migration and release tagging require separate explicit authorization.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
