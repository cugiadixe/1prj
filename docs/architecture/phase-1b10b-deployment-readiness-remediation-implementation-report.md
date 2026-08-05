# Phase 1B.10-B Deployment Readiness Remediation Implementation Report

## Status

IMPLEMENTED — READY FOR REMEDIATION ACCEPTANCE REVIEW

## Authorization Source

Reference:

- Phase 1B.10-A Project Owner remediation plan acceptance commit:
  5924cd655413d59068a678eace693067843456d9

- Phase 1B.10-A remediation plan commit:
  e97aaebebca8532b4e8cd2a8010b3421b1ec7a4c

## Implementation Boundary

- Implemented only accepted Phase 1B.10-B remediation scope.
- No production migration was executed.
- No staging/pre-prod rehearsal was executed.
- No live validation was executed.
- No release tag was created.
- No push was performed.
- Production readiness is not claimed.

## Implemented Permission Seed Alignment

V0015 migration created: `database/migrations/V0015__deployment_readiness_permission_seed_alignment.sql`

12 accepted permission seed rows included with IF NOT EXISTS guards:

| # | permission_code | module_code | action_code | data_scope |
|---|---|---|---|---|
| 1 | CARE_PACKAGE_APPROVE | SALES | APPROVE | COMPANY |
| 2 | CARE_PACKAGE_REJECT | SALES | REJECT | COMPANY |
| 3 | CARE_PACKAGE_CREATE_PAYMENT | SALES | CREATE_PAYMENT | COMPANY |
| 4 | CARD_REPRINT_REQUEST_CREATE | CARD | REQUEST_CREATE | COMPANY |
| 5 | CARD_REPRINT_REQUEST_VIEW | CARD | REQUEST_VIEW | COMPANY |
| 6 | CARD_REPRINT_APPROVE | CARD | APPROVE_REPRINT | COMPANY |
| 7 | CARD_REPRINT_REQUEST_REJECT | CARD | REQUEST_REJECT | COMPANY |
| 8 | CARD_REPRINT_REQUEST_MARK_PRINTED | CARD | REQUEST_MARK_PRINTED | COMPANY |
| 9 | WORKFLOW_REJECT | WORKFLOW | REJECT | COMPANY |
| 10 | WORKFLOW_RETRY_EXECUTION | WORKFLOW | RETRY_EXECUTION | GLOBAL |
| 11 | ORGANIZATION_USER_MANAGE | ORGANIZATION | USER_MANAGE | GLOBAL |
| 12 | CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER | PROPOSE_CHANGE | GLOBAL |

Notes:
- CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE were not duplicated (already seeded in V0014).
- WORKFLOW_VIEW was not duplicated (already seeded in V0006).
- Care Package permissions use module_code `SALES` matching V0014 convention.
- Card Reprint permissions use module_code `CARD` matching permission catalog convention.
- CARD_REPRINT_APPROVE uses action_code `APPROVE_REPRINT` and `is_sensitive = 1`, `is_delegable = 1` matching the existing permission catalog entry.
- WORKFLOW_REJECT uses data_scope `COMPANY` matching the permission catalog.
- CUSTOMER_CHANGE_REQUEST_CREATE uses action_code `PROPOSE_CHANGE` and scope `GLOBAL` matching the permission catalog.
- All rows use `is_active = 1` explicitly and N-prefixed descriptions following V0003–V0012 convention.

## Implemented Rollback

U0015 rollback created: `database/rollbacks/U0015__deployment_readiness_permission_seed_alignment.sql`

- Uses standard soft-deactivation pattern: `UPDATE dbo.Permissions SET is_active = 0` for all 12 permission codes.
- Soft-deactivates SELL_CARE_PACKAGE in Business_Process_Catalog.
- Removes V0015 from SchemaVersions.
- Respects TR_Permissions_PreventDelete trigger (no DELETE on Permissions).
- Does not use the V0014 hard-delete pattern.

## Implemented Permission Catalog Alignment

Updated: `docs/business/permission-catalog.md`

9 accepted permission catalog entries added:

| # | permission_code | module_code | action_code | data_scope |
|---|---|---|---|---|
| 1 | CARE_PACKAGE_VIEW | SALES | VIEW | COMPANY |
| 2 | CARE_PACKAGE_CREATE | SALES | CREATE | COMPANY |
| 3 | CARE_PACKAGE_APPROVE | SALES | APPROVE | COMPANY |
| 4 | CARE_PACKAGE_REJECT | SALES | REJECT | COMPANY |
| 5 | CARE_PACKAGE_CREATE_PAYMENT | SALES | CREATE_PAYMENT | COMPANY |
| 6 | CARD_REPRINT_REQUEST_CREATE | CARD | REQUEST_CREATE | COMPANY |
| 7 | CARD_REPRINT_REQUEST_VIEW | CARD | REQUEST_VIEW | COMPANY |
| 8 | CARD_REPRINT_REQUEST_REJECT | CARD | REQUEST_REJECT | COMPANY |
| 9 | CARD_REPRINT_REQUEST_MARK_PRINTED | CARD | REQUEST_MARK_PRINTED | COMPANY |

No unrelated business docs were changed. No new business requirements were added.

## Implemented Workflow Catalog Alignment

SELL_CARE_PACKAGE seeded in V0015 via Business_Process_Catalog INSERT:
- process_code: SELL_CARE_PACKAGE
- process_name: Bán gói chăm sóc
- is_approval_required: 1
- is_active: 1

Workflow definition/binding remains admin UI operational setup. No production runtime workflow configuration was performed.

## Tests Added / Updated

### SecuritySchemaTests.cs

Updated `ExpectedPermissionCodes` array from 44 to 56 entries to include all 12 new permission codes from V0015. Array remains alphabetically sorted.

### MigrationRollbackTests.cs

Updated `DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder` test:
- Added `Assert.Contains("Applied V0015", ...)` assertion for forward migration.
- Added `Assert.Equal(1, GetSchemaVersionsCount("V0015"))` assertion.
- Added `Assert.Contains("Skipping V0015", ...)` assertion for idempotent re-run.
- Added U0015 rollback step before U0014 in the rollback sequence.
- Verified V0015 SchemaVersions count drops to 0 after U0015 rollback.

## Validation Evidence

- **Build**: 0 errors, 9 pre-existing warnings.
- **UnitTests**: 236/236 passed.
- **IntegrationTests**: 203/203 passed.
- **ApiTests**: 308/308 passed.
- **git diff --check**: clean (verified pre-commit).

## Remaining Items

Carried forward to future phases:

- Staging/pre-prod migration rehearsal (Phase 1B.10-C).
- Rollback rehearsal (Phase 1B.10-C).
- Live manual API/UI/lifecycle validation (Phase 1B.10-C).
- SELL_CARE_PACKAGE workflow definition/binding admin UI operational setup (Phase 1B.10-C or later).
- Production migration planning/execution (Phase 1B.10-D).
- Release tag/push (Phase 1B.10-E).
- Production readiness claim (not authorized).

## Risks / Notes

1. **V0014 module_code**: V0014 seeds CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE with module_code `SALES`. V0015 follows the same convention for the remaining 3 Care Package codes to maintain consistency. The remediation plan originally proposed `CARE_PACKAGE` as module_code but `SALES` was used to match the existing V0014 seed data.

2. **WORKFLOW_REJECT scope**: The permission catalog lists WORKFLOW_REJECT as `COMPANY` scope. The remediation plan originally proposed `GLOBAL`. V0015 follows the permission catalog (`COMPANY`) as the catalog is the canonical reference.

3. **V0014 rollback inconsistency**: V0014 rollback uses hard-DELETE (disabling trigger) instead of the standard soft-deactivation. V0015 rollback follows the standard pattern. This inconsistency is noted but not remediated in this scope.

4. **PAYMENT_PRINT**: Remains DB-seeded and catalog-present with no PermissionCodes.cs constant. Deferred per accepted plan.

## Recommended Next Gate

Phase 1B.10-B remediation acceptance review.
