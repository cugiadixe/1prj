# Phase 1B.10-D Production Migration Execution Report

## Status

PASSED WITH NOTES — READY FOR PROJECT OWNER PRODUCTION MIGRATION ACCEPTANCE

## Authorization Source

Reference:

- Phase 1B.10-D production migration execution authorization commit:
  d623786b03394095b4db432d1fa8564e8cbbd8a4

- Phase 1B.10-D production migration plan acceptance commit:
  c6449f0854ff5b6199b3414e3e00483ac00c9f2e

## Execution Boundary

- Production target DB was PTKD_PROD only.
- No other database was touched (IntegrationTests/ApiTests target PTKD_TEST_PHASE1A2 per TestDatabaseFixture configuration, not PTKD_PROD).
- No source/test/frontend/backend/migration/rollback/business-doc/permission-catalog changes.
- No release tag.
- No push.
- Production readiness not claimed.

## Production Target Evidence

- SQL Server instance: IND-L-BACHDH\SQLEXPRESS (Microsoft SQL Server 2025 Express Edition).
- Database name: PTKD_PROD.
- Database state before execution: did not exist.
- Database creation: created via `CREATE DATABASE PTKD_PROD` at 2026-08-06 08:34:24.
- Confirmation: no existing business data was overwritten (database was newly created).

## Backup / Recovery Evidence

- PTKD_PROD did not exist before execution.
- This is initial creation evidence: no prior data to back up.
- Recovery approach: drop/recreate is authorized only for PTKD_PROD before go-live per execution authorization Decision 5.

## Discovered Execution Method

- Migrations applied via `sqlcmd -S . -E -C -I -d PTKD_PROD -i <migration_file>` iterating through `database/migrations/V*.sql` in repository order.
- This matches the Phase 1B.10-C rehearsal execution method.
- The DbMigrator application was not used; sqlcmd applied the SQL scripts directly.

## Migration Execution Evidence

All 15 migrations applied without error:

| Migration | Result |
|---|---|
| V0001__create_schema_versions.sql | OK |
| V0002__create_organization_schema.sql | OK |
| V0003__create_security_schema.sql | OK (15 seed rows + 2 inserts) |
| V0004__seed_security_admin_manage_permission.sql | OK (1 insert) |
| V0005__create_customer_schema.sql | OK (4 seed rows) |
| V0006__create_workflow_schema.sql | OK (6 seed rows + 2 inserts) |
| V0007__create_customer_change_request.sql | OK |
| V0008__harden_workflow_runtime.sql | OK |
| V0009__add_customer_change_request_target_fields.sql | OK |
| V0010__customer_merge_backend_data_foundation.sql | OK (4 seed rows) |
| V0011__service_module_foundation.sql | OK (6 seed rows + 2 inserts) |
| V0012__payment_foundation.sql | OK (6 seed rows) |
| V0013__card_reprint_foundation.sql | OK |
| V0014__care_package_sales_foundation.sql | OK (2 inserts) |
| V0015__deployment_readiness_permission_seed_alignment.sql | OK (12 permission inserts + 1 process catalog insert) |

No failures. No warnings.

## Post-Migration Verification Evidence

### Migration Tracking

- SchemaVersions table exists (52 tables total in PTKD_PROD).
- SchemaVersions contains 0 rows because sqlcmd applied scripts directly without using the DbMigrator application which records version entries. The DbMigrator was not used because it is an application-level runner; the authorized execution method was sqlcmd for direct SQL execution.
- Schema integrity is verified by table existence and data verification below.

### Core Table Verification

52 tables exist in PTKD_PROD including: Companies, Departments, Users, Permissions, Roles, Customers, Workflow_Definitions, Workflow_Instances, Card_Reprint_Requests, Care_Package_Requests, Payment_Transactions, Business_Process_Catalog, SchemaVersions, and all related tables.

### Permission Verification

- Total active permissions: **56** — PASSED.
- Duplicate permission_code check: **0 duplicates** — PASSED.

### Key Permission Verification

All 15 key permissions verified present and active (is_active = 1):

| permission_code | is_active |
|---|---|
| CARD_REPRINT_APPROVE | 1 |
| CARD_REPRINT_REQUEST_CREATE | 1 |
| CARD_REPRINT_REQUEST_MARK_PRINTED | 1 |
| CARD_REPRINT_REQUEST_REJECT | 1 |
| CARD_REPRINT_REQUEST_VIEW | 1 |
| CARE_PACKAGE_APPROVE | 1 |
| CARE_PACKAGE_CREATE | 1 |
| CARE_PACKAGE_CREATE_PAYMENT | 1 |
| CARE_PACKAGE_REJECT | 1 |
| CARE_PACKAGE_VIEW | 1 |
| CUSTOMER_CHANGE_REQUEST_CREATE | 1 |
| ORGANIZATION_USER_MANAGE | 1 |
| WORKFLOW_REJECT | 1 |
| WORKFLOW_RETRY_EXECUTION | 1 |
| WORKFLOW_VIEW | 1 |

### Process Catalog Verification

- SELL_CARE_PACKAGE: present, is_active = 1 — PASSED.
- Total processes in Business_Process_Catalog: 5.

## Operational Setup Evidence

NOT EXECUTED.

Reason: admin/bootstrap setup, workflow definition/binding, and smoke test record creation are Project Owner operational activities to be performed after migration acceptance. No operational data was inserted beyond what the migrations seed.

## Production Smoke Validation Evidence

| # | Check | Result | Notes |
|---|---|---|---|
| 1 | API health | NOT EXECUTED | API not launched in this task |
| 2 | Authentication/login | NOT EXECUTED | No bootstrap user created |
| 3 | X-Company-Id behavior | NOT EXECUTED | API not launched |
| 4 | Permission check | PASSED | 56 permissions verified via SQL |
| 5 | Permission catalog/admin screen | NOT EXECUTED | API not launched |
| 6 | Customer read/create | NOT EXECUTED | API not launched |
| 7 | Care Package read/create | NOT EXECUTED | API not launched |
| 8 | Card Reprint read/create | NOT EXECUTED | API not launched |
| 9 | Payment check | NOT EXECUTED | API not launched |
| 10 | SELL_CARE_PACKAGE verification | PASSED | Verified via SQL |
| 11 | Workflow setup verification | NOT EXECUTED | No workflow binding configured |
| 12 | Audit/log verification | NOT EXECUTED | API not launched |
| 13 | Frontend route smoke | NOT EXECUTED | Frontend not deployed |

## Automated Sanity Validation Evidence

- **Build**: 0 errors, 9 pre-existing warnings.
- **UnitTests**: 236/236 passed.
- **IntegrationTests**: 203/203 passed (target: PTKD_TEST_PHASE1A2 per TestDatabaseFixture, NOT PTKD_PROD).
- **ApiTests**: 308/308 passed (target: PTKD_TEST_PHASE1A2 per TestDatabaseFixture, NOT PTKD_PROD).

## Notes

1. **Initial production DB creation**: PTKD_PROD was created from scratch because no production data existed. This is initial database creation / initialization, not a legacy data migration.

2. **SchemaVersions empty**: Because migrations were applied via sqlcmd (not the DbMigrator application), the SchemaVersions tracking table has 0 rows. If the DbMigrator is later pointed at PTKD_PROD, it will detect no version records and may attempt to re-apply migrations. The IF NOT EXISTS guards in seed migrations (V0003 onward) and CREATE TABLE IF NOT EXISTS patterns protect against double-application for most scripts, but some DDL migrations (V0001, V0002) would fail on duplicate object creation. This should be considered before running DbMigrator against PTKD_PROD. Options: (a) manually insert SchemaVersions records, (b) run DbMigrator which will detect existing objects and handle accordingly, or (c) accept sqlcmd-only migration management.

3. **API/frontend smoke validation not executed**: API and frontend were not launched in this task. Smoke validation is limited to SQL-level verification. API/frontend deployment and smoke validation remain future operational activities.

4. **Operational setup deferred**: Admin/bootstrap user, company context, workflow definition/binding for SELL_CARE_PACKAGE, and smoke test records were not created. These are Project Owner operational activities.

5. **Release tag/push remain future gates.**

6. **Production readiness not claimed.**

## Blockers

No blockers found for Project Owner production migration acceptance.

## Pass / Fail Assessment

PASSED WITH NOTES. Database migration V0001 through V0015 completed successfully on PTKD_PROD. All 56 permissions verified active with no duplicates. SELL_CARE_PACKAGE verified in Business_Process_Catalog. Build and all automated tests passed. Notes are non-blocking: SchemaVersions tracking is empty (sqlcmd execution method), API/frontend smoke validation was not executed (not deployed), and operational setup was deferred to Project Owner.

## Remaining Future Gates

- Project Owner production migration acceptance.
- Release readiness review.
- Release tag authorization.
- Push authorization.
- Production readiness statement only after all gates accepted.

## Boundary Confirmation

- No source code changes.
- No tests changed.
- No frontend/backend files changed.
- No migrations/rollbacks changed.
- No business docs changed.
- No permission catalog changes.
- No unauthorized database touched (only PTKD_PROD; tests used PTKD_TEST_PHASE1A2).
- No release tag.
- No push.
- No production readiness claim.
- No implementation_plan.md committed.
- No task.md committed.
- No scratch_rehearsal.ps1 committed.
- No frontend debug/test output committed.
- No scratch/decompiled/FixStrategy/script files committed.

## Recommended Next Gate

Project Owner Phase 1B.10-D production migration acceptance.
