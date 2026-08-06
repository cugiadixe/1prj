# Phase 1B.10-D Production Migration Execution Authorization

## Status

AUTHORIZED — PHASE 1B.10-D PRODUCTION MIGRATION EXECUTION AUTHORIZED

## Authorization Source

Reference:

- Phase 1B.10-D Project Owner production migration plan acceptance commit:
  c6449f0854ff5b6199b3414e3e00483ac00c9f2e

- Phase 1B.10-D production migration plan commit:
  2cd35053bc8623391e30a0507155e681f9bf0644

## Authorization Boundary

- Authorization only.
- No production migration executed in this task.
- No production DB created/dropped/reset/initialized in this task.
- No migrations run.
- No rollbacks run.
- No release tag.
- No push.
- No production readiness claim.
- No source/test/frontend/backend/migration/rollback/business-doc/permission-catalog changes.

## Production Execution Decisions

### Decision 1 — Production SQL Server / Instance

Project Owner controlled SQL Server instance.

### Decision 2 — Production Database Name

PTKD_PROD.

### Decision 3 — Production Database State

Empty/new initial production database target.

### Decision 4 — Existing Business Data

No existing production business data is expected. No existing business data will be overwritten.

### Decision 5 — Drop/Recreate Boundary

If initial migration fails before go-live, Project Owner authorizes drop/recreate only for PTKD_PROD. No other database may be dropped/recreated. No destructive action is allowed after go-live without separate explicit authorization.

### Decision 6 — Backup/Recovery

Take backup or recovery point before migration execution, even if DB is empty/new. Record backup evidence in execution report.

### Decision 7 — Migration Executor

Project Owner.

### Decision 8 — Rollback/Recovery Executor

Project Owner.

### Decision 9 — Maintenance Window

Solo-controlled maintenance window. No external coordination required.

### Decision 10 — Secrets Storage

Secrets and connection strings must be handled outside repo. No credentials may be committed. No raw secrets may be printed in reports or chat.

### Decision 11 — Admin/Bootstrap Setup

Project Owner performs admin/bootstrap setup after migration if required. Evidence must be recorded.

### Decision 12 — Production Smoke Test Records

Minimal production smoke test records are allowed only if documented in execution report. No broad synthetic data load is authorized.

### Decision 13 — SELL_CARE_PACKAGE Workflow Setup

Project Owner may perform operational workflow setup after migration if required. Evidence must be recorded.

### Decision 14 — API Deployment Scope

API deployment may be planned after database migration validation. Deployment execution must be recorded in execution report if performed.

### Decision 15 — Frontend Deployment Scope

Frontend deployment may be planned after API smoke validation. Deployment execution must be recorded in execution report if performed.

### Decision 16 — Release Tag / Push Timing

Not authorized by this task. Must remain separate future gate.

### Decision 17 — Final Go/No-Go Owner

Project Owner.

## Authorized Next Step

Authorized next task:
Phase 1B.10-D Production Migration Execution only.

The next task must produce:

docs/architecture/phase-1b10d-production-migration-execution-report.md

The next task may:
- connect to the authorized Project Owner controlled SQL Server instance.
- create or initialize only the authorized production database: PTKD_PROD.
- take backup or recovery point before migration.
- run migrations V0001 through V0015 on PTKD_PROD.
- verify migration tracking.
- verify 56 permissions.
- verify V0015 permission rows.
- verify CARE_PACKAGE_VIEW.
- verify CARE_PACKAGE_CREATE.
- verify WORKFLOW_VIEW.
- verify SELL_CARE_PACKAGE.
- verify no duplicate permission_code.
- perform minimal smoke validation.
- create minimal documented production smoke test records if required.
- perform operational admin/bootstrap setup if required.
- perform SELL_CARE_PACKAGE workflow setup if required.
- capture evidence.
- create the execution report.

The next task may drop/recreate only PTKD_PROD and only if:
- migration fails before go-live,
- the database is still empty/new,
- evidence is recorded,
- and no other database is touched.

The next task must not:
- modify source code.
- modify tests.
- modify frontend/backend files.
- modify migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- touch any database other than PTKD_PROD.
- commit credentials.
- print raw secrets.
- create release tag.
- push.
- claim production readiness.
- hide failures or convert failures into fixes.

## Still Not Authorized

- Release tag.
- Push.
- Production readiness claim.
- Source code fixes during production migration.
- Changing migrations/rollbacks.
- Touching databases other than PTKD_PROD.

## Required Next Output

docs/architecture/phase-1b10d-production-migration-execution-report.md

## Non-Goals

- This task does not execute production migration.
- This task does not connect to production.
- This task does not create/drop/reset/init production DB.
- This task does not run migrations/rollbacks.
- This task does not modify source/tests/frontend/backend/migrations/business docs/permission catalog.
- This task does not create release tag.
- This task does not push.
- This task does not claim production readiness.

## Recommended Next Gate

Phase 1B.10-D Production Migration Execution.
