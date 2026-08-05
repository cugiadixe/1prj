# Phase 1B.10-C Environment Readiness Correction and Re-Execution Plan

## Status

READY FOR PROJECT OWNER CORRECTION PLAN ACCEPTANCE

## Authorization Source

Reference:
- Phase 1B.10-C Project Owner correction decision commit:
  702b1744fb74b5030438b4d8dff7a63721327151

- Phase 1B.10-C failed/blocked execution report commit:
  13a0a2ac93389e6c9a21268b65af56a02a2f6348

## Planning Boundary

Confirm:
- correction planning only.
- no rehearsal execution.
- no live validation execution.
- no database reset/drop/recreate.
- no migration execution.
- no production migration.
- no release tag.
- no push.
- no production readiness claim.
- no source/test/backend/frontend/migration/business-doc changes.

## Accepted Blockers

1. **Dedicated staging/pre-prod server unavailable**: ENVIRONMENT DECISION REQUIRED / ACCESS / CREDENTIALS REQUIRED
2. **Sanitized production snapshot unavailable**: DATASET REQUIRED / ACCESS / CREDENTIALS REQUIRED
3. **Migration rehearsal not executed**: EXECUTION RETRY REQUIRED
4. **Rollback rehearsal not executed**: EXECUTION RETRY REQUIRED
5. **Workflow setup verification not executed**: EXECUTION RETRY REQUIRED
6. **Live API validation not executed**: EXECUTION RETRY REQUIRED
7. **Live frontend/UI validation not executed**: EXECUTION RETRY REQUIRED
8. **Integration/API automated sanity validation blocked by test DB state/connectivity issues**: TEST DB STATE RESET REQUIRED / NOT A CODE DEFECT BASED ON CURRENT EVIDENCE
9. **Production migration remains unauthorized**: OUT OF SCOPE FOR 1B.10-C
10. **Release tag/push remain unauthorized**: OUT OF SCOPE FOR 1B.10-C

## Non-Production Environment Readiness Plan

The re-execution strictly requires:
- **Dedicated Server:** A dedicated non-production staging/pre-prod SQL Server must be provisioned.
- **Isolation:** The server must be explicitly isolated from Development and Production environments. No production database connection string is permitted.
- **Database Naming Convention:** The database must use a non-production naming convention (e.g., `PTKD_STG` or `PTKD_REHEARSAL`).
- **Database Creation/Restore Capability:** The executor must have the required DB roles to create a clean database or restore a sanitized snapshot.
- **Executor Access:** The executor must have access to run migrations (forward and rollback) against the rehearsal database.
- **API/Frontend Connectivity:** The live validation API and frontend environments must be connected strictly to this dedicated staging database.
- **Secret Handling:** All credentials must be injected securely via environment variables or secret managers. No secrets may be committed to the repository or recorded in documentation.
- **Audit/Log Access:** The executor must have access to SQL logs, application logs, and test output for evidence capture.
- **Evidence Capture Owner:** QA Lead (per prior decision).
- **Go/No-go Owner:** DevOps or Dev Lead (per prior decision).

## Dataset and Fallback Plan

Two acceptable data paths are defined:

**Path A (Preferred):**
- Use a sanitized production-like snapshot.
- Verify the successful restoration of the snapshot.
- Confirm data privacy constraints are met (no PII leaks).
- Ensure no sensitive data is captured in the execution evidence.

**Path B (Fallback):**
- Use a completely clean rehearsal database.
- Requires explicit Project Owner acceptance that a clean DB is less production-like.
- Requires synthetic seed data sufficient to complete the migration and all live validation scenarios.
- The limitation of using a clean DB must be explicitly recorded in the re-execution report.

Path A should be used by default. Path B is only allowed if Path A is technically infeasible or blocked by data privacy constraints.

## Test DB Reset / Initialization Boundary

To unblock IntegrationTests and ApiTests:
- The target test database must be strictly non-production (e.g., `PTKD_TEST`).
- A clean baseline state is required before the test suite runs.
- The project's accepted non-production test DB reset/initialization approach (e.g., the test DB fixture script or reset tool) must be executed.
- No broad destructive commands (e.g., `DROP DATABASE`) may be used unless explicitly scoped and hardcoded to the non-production test DB name.
- The `dbo.SchemaVersions` table availability must be verified prior to testing.
- The duplicate `Users` table condition must be avoided by ensuring the reset script safely drops and recreates schema entities in the correct order.
- The test DB name must be explicitly confirmed before any reset command is issued.
- Execution evidence must record the DB target name and the reset boundary command invoked, without exposing credentials.

## Workflow Setup Prerequisites

Before live validation can proceed, the following `SELL_CARE_PACKAGE` workflow prerequisites must be met:
- Migration V0015 must have successfully seeded `SELL_CARE_PACKAGE` into `Business_Process_Catalog`.
- The workflow definition and binding must be configured operationally via the admin UI or an accepted operational setup script.
- Approver user, group, and permission mappings must be configured for the workflow.
- The submit, approve, and reject lifecycle states must be available in the configuration.
- Setup must include an expectation/plan for rollback or disabling the workflow post-testing.
- Evidence (e.g., screenshots or query results) is needed to prove the setup is complete.
- Operations Admin sign-off is required for the setup.

## Live Validation Data Prerequisites

The following data must be present in the rehearsal environment prior to live validation:
- Test user accounts provisioned with the required Phase 1B permissions.
- Test user accounts without the required Phase 1B permissions (for negative testing).
- Valid company context / `X-Company-Id`.
- Existing Customer records.
- Existing Grave/Care target records associated with the customers.
- Active Service Catalog items with valid effective-date prices.
- Defined Care Package test cases.
- Defined Card Reprint test cases.
- Valid Payment Foundation setup (VND currency config, etc.).
- Active workflow setup.
- A defined cleanup/reset approach after live validation concludes.
- A plan for capturing audit evidence.
- Strict adherence to data privacy constraints.

## Re-Execution Steps

The re-execution must follow this exact sequence:
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

Execution must be separately authorized after this correction plan is accepted.

## Evidence Requirements

The future re-execution report must include:
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

## Pass / Fail Criteria

- **READY FOR RE-EXECUTION AUTHORIZATION:** The plan defines all environment, dataset, test DB reset, workflow, and data prerequisites, re-execution steps, and evidence requirements, with no execution performed in this task.
- **BLOCKED — ENVIRONMENT DETAILS REQUIRED:** The plan lacks sufficient detail regarding the environment, dataset, or reset boundaries for Project Owner acceptance.

Based on the completion of the sections above, this plan is **READY**.

## Risks

- Environment provisioning delays may further block re-execution.
- Data privacy constraints may prevent the use of a sanitized snapshot, forcing the use of a clean DB and synthetic data, which reduces confidence in production-like validation.
- Test DB reset tools may require updates if the schema state is severely corrupted.

## Non-Goals

Confirm this task does not:
- execute rehearsal.
- execute live validation.
- reset databases.
- run migrations.
- run rollbacks.
- connect to production.
- run production migration.
- create release tag.
- push.
- claim production readiness.
- modify source code, tests, frontend/backend files, migrations/rollbacks, business docs, or permission catalog.

## Recommended Next Gate

Project Owner Phase 1B.10-C correction plan acceptance.
