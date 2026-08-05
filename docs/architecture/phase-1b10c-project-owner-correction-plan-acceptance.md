# Phase 1B.10-C Project Owner Correction Plan Acceptance — Environment Readiness

## Status

ACCEPTED — PHASE 1B.10-C ENVIRONMENT READINESS CORRECTION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.10-C Environment Readiness Correction and Re-Execution Plan.

This acceptance is based on the correction plan created after the failed/blocked Phase 1B.10-C execution.

This acceptance authorizes only the next re-execution task:
Phase 1B.10-C Environment Readiness Correction Re-Execution.

This acceptance does not claim Phase 1B.10-C passed.

This acceptance does not authorize production migration, release tag, push, or production readiness claim.

## Accepted Correction Plan

Reference:

- Phase 1B.10-C environment readiness correction plan commit:
  058a739a392f1f0e47ed5897c7934379c83c2178

- Phase 1B.10-C Project Owner correction decision commit:
  702b174b19f765468feb139a17ece5dc66084afe

- Phase 1B.10-C failed/blocked execution report commit:
  13a0a2ac93389e6c9a21268b65af56a02a2f6348

## Accepted Blockers

- **Dedicated staging/pre-prod server unavailable**: ENVIRONMENT DECISION REQUIRED / ACCESS / CREDENTIALS REQUIRED
- **Sanitized production snapshot unavailable**: DATASET REQUIRED / ACCESS / CREDENTIALS REQUIRED
- **Migration rehearsal not executed**: EXECUTION RETRY REQUIRED
- **Rollback rehearsal not executed**: EXECUTION RETRY REQUIRED
- **Workflow setup verification not executed**: EXECUTION RETRY REQUIRED
- **Live API validation not executed**: EXECUTION RETRY REQUIRED
- **Live frontend/UI validation not executed**: EXECUTION RETRY REQUIRED
- **Integration/API automated sanity validation blocked by test DB state/connectivity issues (e.g., duplicate Users table, missing SchemaVersions)**: TEST DB STATE RESET REQUIRED / NOT A CODE DEFECT BASED ON CURRENT EVIDENCE
- **Production migration remains unauthorized**: OUT OF SCOPE FOR 1B.10-C
- **Release tag/push remain unauthorized**: OUT OF SCOPE FOR 1B.10-C

## Accepted Environment Readiness Plan

- **Dedicated Server:** A dedicated non-production staging/pre-prod SQL Server must be provisioned.
- **Isolation:** The server must be explicitly isolated from Development and Production environments. No production database connection string is permitted.
- **Database Naming Convention:** The database must use a non-production naming convention (e.g., `PTKD_STG` or `PTKD_REHEARSAL`).
- **Database Creation/Restore Capability:** The executor must have the required DB roles to create a clean database or restore a sanitized snapshot.
- **Executor Access:** The executor must have access to run migrations (forward and rollback) against the rehearsal database.
- **API/Frontend Connectivity:** The live validation API and frontend environments must be connected strictly to this dedicated staging database.
- **Secret Handling:** All credentials must be injected securely via environment variables or secret managers. No secrets may be committed to the repository or recorded in documentation.
- **Audit/Log Access:** The executor must have access to SQL logs, application logs, and test output for evidence capture.

## Accepted Dataset and Fallback Plan

- **Path A (Preferred):** Use a sanitized production-like snapshot. Requires verification of restoration and data privacy constraints (no PII leaks).
- **Path B (Fallback):** Use a completely clean rehearsal database with synthetic seed data sufficient for migration and live validation.
- The limitation of using a clean DB (Path B) must be explicitly recorded in the re-execution report if Path A is not technically feasible.

## Accepted Test DB Reset / Initialization Boundary

- **Target DB:** The target test database must be strictly non-production (e.g., `PTKD_TEST`).
- **Baseline:** A clean baseline state is required before the test suite runs (IntegrationTests and ApiTests).
- **Reset Approach:** The project's accepted non-production test DB reset/initialization approach must be executed.
- **Schema verification:** The `dbo.SchemaVersions` table availability must be verified, and the duplicate `Users` table condition must be avoided by safe schema drops/re-creation.
- **Confirmation:** The target test DB name must be confirmed before any reset.
- **Constraints:** No broad destructive commands outside the scoped non-production DB.

## Accepted Workflow Setup Prerequisites

- Migration V0015 must have successfully seeded `SELL_CARE_PACKAGE` into `Business_Process_Catalog`.
- The workflow definition and binding must be configured operationally via the admin UI or an accepted operational setup script.
- Approver user, group, and permission mappings must be configured for the workflow.
- Evidence (e.g., screenshots or query results) and Operations Admin sign-off are required.

## Accepted Live Validation Data Prerequisites

The following must be present prior to live validation:
- Test user accounts with and without required Phase 1B permissions.
- Valid company context / `X-Company-Id`.
- Existing Customer records.
- Existing Grave/Care target records.
- Active Service Catalog items with valid effective-date prices.
- Defined Care Package, Card Reprint, and Payment test cases.
- Active workflow setup.
- A defined cleanup/reset approach and a plan for capturing audit evidence.

## Accepted Re-Execution Steps

1. Confirm non-production environment connection.
2. Confirm backup/restore (Path A) or clean DB fallback (Path B) has been executed.
3. Confirm test DB clean baseline has been established for automated tests.
4. Run repository pre-flight checks.
5. Run migration rehearsal sequentially from V0001 through V0015.
6. Verify V0015 permission rows and `SELL_CARE_PACKAGE` catalog entry.
7. Run U0015 rollback rehearsal.
8. Verify the rollback boundary (safe deactivation) and the path to reapply V0015.
9. Verify workflow operational setup.
10. Run live API validation scenarios.
11. Run live UI validation scenarios.
12. Run automated sanity validation (Build, UnitTests, IntegrationTests, ApiTests).
13. Capture all evidence.
14. Create the Phase 1B.10-C re-execution report.

## Accepted Evidence Requirements

- Environment confirmation statement.
- DB target confirmation (without secrets).
- Backup or restore logs.
- Migration command and log summary.
- Rollback command and log summary.
- Permission table query results showing V0015 changes.
- `Business_Process_Catalog` query result showing `SELL_CARE_PACKAGE`.
- Duplicate permission checks verifying idempotency.
- Verification of `SchemaVersions` integrity.
- API request/response samples for validation scenarios.
- UI screenshots or structured UI notes.
- Test user and permission matrix used during validation.
- Automated sanity validation output logs.
- Blockers and deviations encountered.
- Git status evidence showing a clean boundary.
- Confirmation of no production migration, tag, or push.

## Authorization for Next Step

Authorized next task:
Phase 1B.10-C Environment Readiness Correction Re-Execution only.

The next task must produce:
docs/architecture/phase-1b10c-environment-readiness-correction-execution-report.md

The next task may:
- confirm the accepted non-production environment.
- confirm the accepted dataset path.
- confirm and use the accepted test DB reset/initialization boundary.
- run accepted migration rehearsal V0001 through V0015.
- run accepted U0015 rollback rehearsal.
- verify V0015 permission rows and SELL_CARE_PACKAGE.
- verify workflow setup.
- execute accepted live API validation.
- execute accepted live UI validation.
- run accepted automated sanity validation.
- capture evidence.
- create the correction execution report.

The next task must not:
- modify source code.
- modify tests.
- modify frontend/backend files.
- modify migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- run production migration.
- connect to production for rehearsal/live validation.
- create release tag.
- push.
- claim production readiness.
- hide failures or convert failures into fixes.

## Required Next Output

docs/architecture/phase-1b10c-environment-readiness-correction-execution-report.md

## Non-Goals

Confirm:
- this acceptance does not execute correction.
- this acceptance does not execute rehearsal.
- this acceptance does not execute live validation.
- this acceptance does not reset databases.
- this acceptance does not run migrations.
- this acceptance does not run production migration.
- this acceptance does not modify source/tests/frontend/backend/migrations/business docs/permission catalog.
- this acceptance does not create release tag.
- this acceptance does not push.
- this acceptance does not claim production readiness.
- this acceptance does not accept Phase 1B.10-C as passed.

## Recommended Next Gate

Phase 1B.10-C Environment Readiness Correction Re-Execution.
