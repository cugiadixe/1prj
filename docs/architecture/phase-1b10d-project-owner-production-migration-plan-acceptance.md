# Phase 1B.10-D Project Owner Production Migration Plan Acceptance

## Status

ACCEPTED WITH EXECUTION DECISIONS REQUIRED — PHASE 1B.10-D PRODUCTION MIGRATION PLAN ACCEPTED

## Project Owner Decision

- The Project Owner accepts the Phase 1B.10-D Production Migration Plan with execution decisions required.
- The accepted plan treats production migration as initial production database creation / initialization because no production business data exists yet.
- This acceptance does not authorize production migration execution.
- This acceptance does not authorize production database create/drop/reset/init.
- This acceptance does not authorize release tag or push.
- This acceptance does not claim production readiness.
- This acceptance authorizes only the next decision/authorization task:
  Phase 1B.10-D Production Migration Execution Decision / Authorization.

## Accepted Plan Source

Reference:

- Phase 1B.10-D production migration plan commit:
  2cd35053bc8623391e30a0507155e681f9bf0644

- Phase 1B.10-D production migration plan parent commit:
  ab011555d67e0c6b742a2fb253042d4cd9058b1a

- Phase 1B.10-C Project Owner re-execution acceptance commit:
  ab011555d67e0c6b742a2fb253042d4cd9058b1a

## Accepted Readiness Baseline

Phase 1B.10-C solo environment readiness was accepted with notes. Accepted evidence:

- V0001 through V0015 forward migration rehearsal passed.
- U0015 rollback rehearsal passed.
- 56 permissions verified.
- CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, WORKFLOW_VIEW verified present.
- SELL_CARE_PACKAGE verified in Business_Process_Catalog.
- Build: 0 errors / 9 warnings.
- UnitTests: 236/236 passed.
- IntegrationTests: 203/203 passed.
- ApiTests: 308/308 passed.
- Repository boundary clean.

Accepted notes: synthetic/minimal data used (no production data exists yet); lower fidelity than sanitized production-like snapshot; manual frontend/UI validation not executed in solo headless context.

## Accepted Plan Type

Initial production database creation / initialization plan.

Not legacy data migration, not data conversion, not production data backfill, not live production upgrade of existing business data.

## Accepted Pre-Flight Checklist

19 pre-flight items accepted including: Project Owner explicit GO, production SQL Server/instance identification, production database name, empty/new DB confirmation, no existing business data overwritten, backup/recovery approach, secrets handled outside repo, migration/rollback executor, application deployment config, admin/bootstrap approach, SELL_CARE_PACKAGE workflow setup, permission catalog alignment (56 permissions), audit/log access, smoke test user, maintenance window, no tag/push until later gates.

## Accepted Execution Sequence

21-step execution sequence accepted: repo freeze at accepted commit, clean git state verification, production target/secrets confirmation, backup/recovery point, create or confirm empty production DB, apply V0001 through V0015 in order, verify SchemaVersions (15 rows), verify core tables, verify 56 permissions, verify V0015 permission rows, verify CARE_PACKAGE_VIEW, verify CARE_PACKAGE_CREATE, verify WORKFLOW_VIEW, verify SELL_CARE_PACKAGE, verify no duplicate permission_code, verify business/security audit constraints, configure operational/bootstrap items, configure SELL_CARE_PACKAGE workflow, run production smoke validation, capture evidence, stop for PO acceptance.

## Accepted Rollback / Recovery Plan

- Backup-first strategy accepted: full backup before migration even if DB is empty/new.
- Restore from backup is primary recovery strategy.
- U0015 available for V0015-specific soft-deactivation only (not full historical rollback).
- Drop/recreate accepted only if DB is empty/new and PO explicitly pre-authorizes before execution.
- No destructive production rollback without explicit PO authorization.
- Post-rollback/restore verification required.

## Accepted Smoke Validation Plan

17 smoke checks accepted: API health, authentication/login, X-Company-Id behavior, permission check, permission catalog/security admin screen, minimal customer/care/card/payment checks where allowed, SELL_CARE_PACKAGE process catalog verification, workflow setup verification if configured, audit/log verification, safe error behavior, frontend route smoke if frontend deployed. Minimal test records only; any test records in production must be PO-authorized and documented.

## Accepted Release / Tag / Push Gates

8 sequential gates accepted: (1) plan acceptance (this document), (2) production migration execution authorization, (3) production migration execution report, (4) PO production migration acceptance, (5) release readiness review, (6) release tag authorization, (7) push authorization, (8) production readiness statement only after all gates accepted. No tag or push in this phase.

## Execution Decisions Still Required

The following decisions must be recorded in the execution authorization document before production migration may proceed:

| # | Decision | Owner |
|---|---|---|
| 1 | Production SQL Server / instance | Project Owner |
| 2 | Production database name | Project Owner |
| 3 | Confirmation DB is empty/new | Project Owner |
| 4 | Whether drop/recreate is allowed if initial migration fails before go-live | Project Owner |
| 5 | Backup/restore approach | Project Owner |
| 6 | Migration executor | Project Owner |
| 7 | Rollback/recovery executor | Project Owner |
| 8 | Maintenance window | Project Owner |
| 9 | Secrets storage approach (without secrets) | Project Owner |
| 10 | Admin/bootstrap setup | Project Owner |
| 11 | Whether production smoke test records may be created | Project Owner |
| 12 | Workflow setup method for SELL_CARE_PACKAGE | Project Owner |
| 13 | API deployment scope | Project Owner |
| 14 | Frontend deployment scope | Project Owner |
| 15 | Release tag/push timing | Project Owner |
| 16 | Final go/no-go owner | Project Owner |

For solo context, Project Owner is owner for all decisions, but actual values must still be recorded before execution.

## Authorization for Next Step

Authorized next task:
Phase 1B.10-D Production Migration Execution Decision / Authorization only.

The next task must produce:

docs/architecture/phase-1b10d-production-migration-execution-authorization.md

The next task may:
- record exact production SQL Server/instance identifier without secrets.
- record exact production database name.
- record whether DB is empty/new.
- record backup/recovery approach.
- record migration executor and rollback/recovery executor.
- record maintenance window.
- record secrets storage approach without secrets.
- record admin/bootstrap setup.
- record production smoke test record policy.
- record SELL_CARE_PACKAGE workflow setup method.
- record API/frontend deployment scope.
- record final go/no-go decision.
- authorize or block production migration execution based on completeness.

The next task must not:
- run production migration.
- connect to production.
- create/drop/reset/init production database.
- run migrations.
- run rollbacks.
- modify source code.
- modify tests.
- modify frontend/backend files.
- modify migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- create release tag.
- push.
- claim production readiness.

## Required Next Output

docs/architecture/phase-1b10d-production-migration-execution-authorization.md

## Non-Goals

- This acceptance does not run production migration.
- This acceptance does not connect to production.
- This acceptance does not create/drop/reset/init production database.
- This acceptance does not run migrations/rollbacks.
- This acceptance does not modify source/tests/frontend/backend/migrations/business docs/permission catalog.
- This acceptance does not create release tag.
- This acceptance does not push.
- This acceptance does not claim production readiness.

## Recommended Next Gate

Phase 1B.10-D Production Migration Execution Decision / Authorization.
