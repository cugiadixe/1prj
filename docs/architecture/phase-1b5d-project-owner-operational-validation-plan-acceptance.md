# Phase 1B.5-D Project Owner Operational Validation Plan Acceptance

## Status

ACCEPTED — PHASE 1B.5-D OPERATIONAL VALIDATION PLAN APPROVED FOR EXECUTION

## Accepted Plan

The Project Owner accepts:

docs/architecture/phase-1b5d-operational-validation-and-closure-plan.md

Planning commit:
0f5ac174c328c1e90a17589aad856c900e09b69b

## Accepted Validation Scope

The Project Owner accepts only the operational validation and closure execution scope defined in the plan, including:

- Backend build validation (dotnet build).
- Backend UnitTests (dotnet test PTKD.UnitTests).
- Backend IntegrationTests (dotnet test PTKD.IntegrationTests).
- Backend ApiTests (dotnet test PTKD.ApiTests).
- Frontend lint validation (npm run lint).
- Frontend TypeScript validation (npx tsc -b).
- Frontend Vitest validation (npm run test).
- Targeted Customer Merge frontend tests (5 test files, 30 tests).
- Database/migration validation evidence (via automated tests, not production migration).
- Repository hygiene checks (git diff --check, git status, no tag, no push).
- Manual/operational checklist (36 items).
- Closure report creation.

## Accepted Database / Migration Validation Scope

The Project Owner accepts planned database/migration validation including:

- V0010 migration evidence via MigrationRollbackTests and IntegrationTests.
- U0010 rollback evidence via MigrationRollbackTests.
- MigrationRollbackTests coverage for V0010/U0010.
- DbMigrator ownership of SchemaVersions.
- U0010 removal of V0010 SchemaVersions record.
- Soft-deactivation of CUSTOMER_MERGE_* permissions in U0010 rollback (TR_Permissions_PreventDelete blocks hard delete).
- SafeTestWebApplicationFactory / ResetToV0010 evidence.
- PTKD_TEST_PHASE1A2 test DB only.
- No production migration.

## Accepted Manual / Operational Checklist

The Project Owner accepts the plan's 36-item operational checklist, including validation for:

- Duplicate customer search form and result display.
- Candidate list display with customer details.
- Merge request creation with source/target input and comparison.
- Source vs survivor comparison display.
- Survivorship review (no raw JSON display).
- Source equals target blocked.
- Already merged source blocked.
- Invalid/inactive target blocked.
- Overlapping CustomerCompanyContext conflict sanitized error.
- Stale rowversion/concurrency sanitized error.
- Permission denied (403) sanitized.
- Not found (404) sanitized.
- Generic server failure sanitized.
- No raw SQL/internal exception display.
- No stack trace display.
- No raw sensitive payload display.
- Merge request list page with pagination and status tags.
- Merge request detail page with metadata and candidates.
- Workflow/status display and workflow instance link.
- Approved workflow execution applies merge once.
- Rejected/non-approved request does not mutate customer data.
- Retry/idempotency does not double-apply merge.
- Source customer remains traceable.
- Survivor/canonical customer remains active.
- No destructive customer deletion UI.
- No automatic fuzzy merge UI.
- Backend authorization remains authoritative.
- Frontend permission gating is convenience only.

## Boundaries

- Operational validation execution is authorized only after this acceptance commit.
- Source code changes are not authorized.
- Test changes are not authorized.
- Frontend/backend implementation changes are not authorized.
- Migrations/rollbacks are not authorized.
- Business requirement changes are not authorized.
- Production migration is not authorized.
- Release tag is not authorized.
- Push is not authorized.
- Post-1B.5 next-work selection is not authorized yet.

## Execution Evidence Required

Operational validation execution must produce:

- Backend build result.
- UnitTests result.
- IntegrationTests result.
- ApiTests result.
- Frontend lint result.
- TypeScript result.
- Full Vitest result.
- Targeted Customer Merge frontend test result.
- git diff --check result.
- git status result.
- Database/migration validation result.
- Manual/operational checklist result.
- Confirmation no production migration.
- Confirmation no tag.
- Confirmation no push.
- Closure report.

## Project Owner Decision

The Project Owner accepts the Phase 1B.5-D operational validation and closure plan.

## Authorization for Next Step

Authorized next task:
Phase 1B.5-D operational validation and closure execution only.

After execution, a separate Phase 1B.5-D operational validation and closure report, acceptance review, and Project Owner closure acceptance are required before Phase 1B.5 can be closed.

Do not authorize:
- production migration,
- release tag,
- push,
- post-1B.5 next-work selection.
