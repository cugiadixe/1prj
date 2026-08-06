# Phase 1B.10-D Production Migration Plan

## Status

PROPOSED — READY FOR PROJECT OWNER PRODUCTION MIGRATION PLAN ACCEPTANCE

## Authorization Source

Reference:

- Phase 1B.10-C Project Owner re-execution acceptance commit:
  ab0115598d9ccf5ce754be01369527ec3a1d94ff

- Phase 1B.10-C solo re-execution report commit:
  ff00e205ab04c4558e8071869e5d4a13e51a66a1

## Planning Boundary

- Planning only.
- No production connection.
- No production DB create/drop/reset/init.
- No migrations run.
- No rollbacks run.
- No release tag.
- No push.
- No production readiness claim.
- No source/test/frontend/backend/migration/rollback/business-doc/permission-catalog changes.

## Accepted Readiness Baseline

Phase 1B.10-C solo environment readiness was accepted with notes (commit ab01155). Accepted evidence:

- V0001 through V0015 forward migration rehearsal passed on PTKD_REHEARSAL_PHASE1B10C.
- U0015 rollback rehearsal passed on PTKD_REHEARSAL_ROLLBACK_PHASE1B10C.
- 56 permissions verified.
- CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, WORKFLOW_VIEW verified present.
- SELL_CARE_PACKAGE verified in Business_Process_Catalog (5 processes total).
- Build: 0 errors / 9 warnings.
- UnitTests: 236/236 passed.
- IntegrationTests: 203/203 passed.
- ApiTests: 308/308 passed.
- Repository boundary clean.

Accepted notes:

- Synthetic/minimal data used because no production data exists yet.
- Lower fidelity than sanitized production-like snapshot.
- Manual frontend/UI validation not executed in solo headless context.
- Workflow operational lifecycle relied on automated validation and schema/catalog verification.

## Plan Type

Initial production database creation / initialization plan.

This plan covers the controlled first-time creation of a production database using repo-controlled migrations V0001 through V0015. This is not a legacy data migration, production data conversion, production data backfill, or live production upgrade of existing business data.

- The production target will be created or initialized only after later explicit Project Owner authorization.
- Repo-controlled migrations V0001 through V0015 are the source of truth for schema and seed data.
- V0015 is included because Phase 1B.10-B remediation was accepted.
- U0015 rollback boundary exists only for V0015 and must be understood before execution.
- Solo synthetic validation does not replace final production pre-flight verification.

## Production Pre-Flight Checklist

Before any future production execution, all of the following must be confirmed:

| # | Item | Owner |
|---|---|---|
| 1 | Project Owner explicit GO decision | Project Owner |
| 2 | Production SQL Server instance identified | Project Owner |
| 3 | Production database name identified | Project Owner |
| 4 | Confirm production DB is empty/new or explicitly accepted target | Project Owner |
| 5 | Confirm no existing business data will be overwritten | Project Owner |
| 6 | Backup approach confirmed before migration (even if DB is empty/new) | Project Owner |
| 7 | Restore test or recovery approach confirmed | Project Owner |
| 8 | Connection string / secrets handled outside repo (no secrets in code or chat) | Project Owner |
| 9 | Migration executor identified | Project Owner |
| 10 | Rollback executor identified | Project Owner |
| 11 | Application deployment configuration confirmed (API, frontend if applicable) | Project Owner |
| 12 | Admin / bootstrap user approach confirmed | Project Owner |
| 13 | Expected company / user / bootstrap data plan confirmed | Project Owner |
| 14 | SELL_CARE_PACKAGE workflow definition / binding setup plan confirmed | Project Owner |
| 15 | Permission catalog alignment verified (56 permissions expected) | Project Owner |
| 16 | Audit / log access confirmed | Project Owner |
| 17 | Smoke test user identified | Project Owner |
| 18 | Downtime / maintenance window confirmed (if applicable) | Project Owner |
| 19 | No release tag / push until separate gates | Project Owner |

## Production Migration Execution Sequence

Plan only. Do not execute.

| Step | Action | Verification |
|---|---|---|
| 1 | Freeze repo at accepted commit | `git rev-parse HEAD` matches accepted commit |
| 2 | Confirm no tracked changes | `git status` clean |
| 3 | Confirm production target and secrets | Connection verified, secrets not in repo |
| 4 | Confirm backup / recovery point | Backup evidence captured |
| 5 | Create or confirm empty production DB | DB exists and is empty |
| 6 | Apply V0001 through V0015 in order | Each migration applied without error |
| 7 | Verify SchemaVersions tracking table | 15 rows (V0001–V0015), count = 1 each |
| 8 | Verify core tables exist | Organizations, Companies, Departments, Users, Permissions, Roles, etc. |
| 9 | Verify 56 total permissions | `SELECT COUNT(*) FROM dbo.Permissions WHERE is_active = 1` = 56 |
| 10 | Verify V0015 permission rows | All 12 V0015-seeded permissions present and active |
| 11 | Verify CARE_PACKAGE_VIEW | Present, is_active = 1 |
| 12 | Verify CARE_PACKAGE_CREATE | Present, is_active = 1 |
| 13 | Verify WORKFLOW_VIEW | Present, is_active = 1 |
| 14 | Verify SELL_CARE_PACKAGE | Present in Business_Process_Catalog, is_active = 1 |
| 15 | Verify no duplicate permission_code | `SELECT permission_code, COUNT(*) ... HAVING COUNT(*) > 1` returns 0 rows |
| 16 | Verify business/security audit constraints | TR_Permissions_PreventDelete trigger active, SecurityStamp triggers active |
| 17 | Configure operational / bootstrap items if required | Admin user, company, department as per PO decisions |
| 18 | Configure SELL_CARE_PACKAGE workflow if required | Workflow definition, binding via admin UI or accepted method |
| 19 | Run production smoke validation | See smoke validation plan below |
| 20 | Capture evidence | See evidence requirements below |
| 21 | Stop for PO production migration execution acceptance | No further action without PO GO |

## Rollback / Recovery Plan

### Primary Recovery Strategy

Restore from backup is the primary recovery strategy for production.

### Pre-Execution Backup

- A full database backup must be taken before migration execution begins, even if the DB is empty/new.
- Backup evidence must be captured (backup file path, timestamp, verification).

### Migration Failure Handling

- If migration fails before completion: stop immediately, preserve error logs, do not retry without analysis.
- If production DB is new/empty: acceptable recovery may be drop/recreate, but only if Project Owner explicitly authorizes this approach before execution begins.

### U0015 Rollback Boundary

- U0015 is available for V0015-specific soft-deactivation (sets 12 permissions to is_active = 0, deactivates SELL_CARE_PACKAGE, removes V0015 from SchemaVersions).
- U0015 is not a full rollback for all historical migrations.
- U0015 does not delete permission rows (respects TR_Permissions_PreventDelete).
- Rollback does not replace backup/restore.

### Destructive Rollback Prohibition

- No destructive rollback against production without explicit Project Owner authorization.
- No DROP DATABASE, no TRUNCATE, no DELETE FROM dbo.Permissions without explicit authorization.

### Post-Rollback Verification

- If rollback or restore is performed: verify SchemaVersions state, verify permission counts, verify table integrity, capture evidence.

### Evidence Requirements for Recovery

- Rollback/restore command and output log.
- Post-recovery SchemaVersions state.
- Post-recovery permission count.
- Post-recovery table existence verification.

## Production Smoke Validation Plan

Plan only. Do not execute.

| # | Check | Expected Result |
|---|---|---|
| 1 | API health check | Health endpoint returns 200 |
| 2 | Authentication / login check | Valid credentials return JWT |
| 3 | Company context / X-Company-Id behavior | API accepts valid company header |
| 4 | Permission check | Authenticated user has expected effective permissions |
| 5 | Permission catalog / security admin screen | 56 permissions visible if admin UI available |
| 6 | Customer read (minimal) | Customer list/search returns without error |
| 7 | Customer create (minimal, if allowed) | Create proposal succeeds if test record authorized |
| 8 | Care Package read (minimal, if allowed) | Care package list returns without error |
| 9 | Care Package create (minimal, if allowed) | Create request succeeds if test record authorized |
| 10 | Card Reprint read (minimal, if allowed) | Card reprint request list returns without error |
| 11 | Card Reprint create (minimal, if allowed) | Create request succeeds if test record authorized |
| 12 | Payment (minimal, if allowed) | Payment draft creation succeeds if test record authorized |
| 13 | SELL_CARE_PACKAGE process catalog verification | Process exists in Business_Process_Catalog |
| 14 | Workflow setup verification (if configured) | Workflow binding active for SELL_CARE_PACKAGE |
| 15 | Audit / log verification | Audit records written for smoke test actions |
| 16 | Safe error behavior | Invalid requests return structured error, not stack trace |
| 17 | Frontend route smoke (if frontend deployed) | Login page loads, navigation renders |

Notes:

- Smoke validation should use minimal test records only.
- Any test records created in production must be explicitly allowed and documented by Project Owner.
- No broad synthetic data should be inserted into production unless Project Owner authorizes.

## Release / Tag / Push Gates

Future gates required in sequence:

| # | Gate | Status |
|---|---|---|
| 1 | Phase 1B.10-D production migration plan acceptance | This document (pending) |
| 2 | Production migration execution authorization | Not yet created |
| 3 | Production migration execution report | Not yet created |
| 4 | Project Owner production migration acceptance | Not yet created |
| 5 | Release readiness review | Not yet created |
| 6 | Release tag authorization | Not yet created |
| 7 | Push authorization | Not yet created |
| 8 | Production readiness statement | Only after all gates accepted |

No tag or push in Phase 1B.10-D planning.

## Project Owner Decisions Required Before Execution

| # | Decision | Recommended Option | Required Before Execution |
|---|---|---|---|
| 1 | Production SQL Server instance | Dedicated production SQL Server (not localhost/SQLEXPRESS) | Yes |
| 2 | Production database name | PTKD_PRODUCTION or PO-chosen name | Yes |
| 3 | Whether DB is empty/new | Confirm empty/new (no existing business data) | Yes |
| 4 | Whether drop/recreate is allowed if initial migration fails before go-live | Allow drop/recreate for initial empty DB only, with PO authorization at time of failure | Yes |
| 5 | Backup/restore approach | Full backup before migration, verified restore capability | Yes |
| 6 | Migration executor | Project Owner or PO-designated executor | Yes |
| 7 | Rollback/recovery executor | Same as migration executor | Yes |
| 8 | Maintenance window | Coordinate with deployment schedule; initial setup may not require formal window | Yes |
| 9 | Secrets storage approach | Environment variables or secure configuration provider; never in repo | Yes |
| 10 | Admin / bootstrap setup | Create initial admin user and company via accepted method after migration | Yes |
| 11 | Whether production smoke test records may be created | Allow minimal test records with documented cleanup plan | Yes |
| 12 | Workflow setup method for SELL_CARE_PACKAGE | Admin UI operational setup after migration | Yes |
| 13 | Frontend deployment scope | Deploy frontend alongside API, or API-only initially | Yes |
| 14 | API deployment scope | Deploy API to production host with production connection string | Yes |
| 15 | Release tag/push timing | After production migration acceptance and release readiness review | Yes |
| 16 | Final go/no-go owner | Project Owner | Yes |

## Evidence Requirements

Required future evidence for production migration execution report:

| # | Evidence | Description |
|---|---|---|
| 1 | Git HEAD and status | Commit hash, branch, clean status |
| 2 | Production target identifier | Server/instance name without secrets |
| 3 | Backup evidence | Backup file, timestamp, verification |
| 4 | Migration command/log summary | Each V0001–V0015 applied without error |
| 5 | SchemaVersions rows | 15 rows, count = 1 each |
| 6 | Permission verification | 56 active permissions, no duplicates |
| 7 | Process catalog verification | SELL_CARE_PACKAGE present and active |
| 8 | Rollback/recovery readiness evidence | U0015 available, backup restore tested or confirmed |
| 9 | Smoke validation results | Each check passed/failed with notes |
| 10 | Audit/log evidence | Audit records written for smoke actions |
| 11 | No tag/push evidence | `git tag --points-at HEAD` empty, no remote push |
| 12 | No production readiness claim | Explicit statement |

## Risks

1. **Production target misidentification.** Applying migrations to the wrong server or database would create schema in an unintended location. Mitigation: explicit PO confirmation of target before execution.

2. **Secrets mishandling.** Connection strings or credentials stored in repo, chat, or logs would create a security exposure. Mitigation: secrets handled outside repo via environment variables or secure provider.

3. **Solo synthetic validation does not equal production-like validation.** Phase 1B.10-C used minimal synthetic data on a local instance. Production behavior under real load, network, and concurrent access is not validated. Mitigation: production smoke validation after migration.

4. **Lack of backup/restore evidence.** If backup is skipped or untested, recovery from migration failure becomes difficult. Mitigation: mandatory backup with verification before execution.

5. **Workflow setup missing.** SELL_CARE_PACKAGE is seeded in Business_Process_Catalog but workflow definition/binding requires admin UI operational setup. If skipped, care package approval workflow will not function. Mitigation: PO confirms workflow setup method before go-live.

6. **Production test data pollution.** Smoke test records created in production may persist and affect real business operations. Mitigation: PO authorizes test records with documented cleanup plan.

7. **Tag/push before acceptance.** Premature tag or push would distribute unaccepted state. Mitigation: tag/push remain gated behind separate future authorization.

8. **Production readiness claim too early.** Claiming production readiness before all gates are accepted would misrepresent deployment state. Mitigation: production readiness statement only after all gates accepted.

## Non-Goals

- No production migration execution.
- No production DB connection.
- No production DB reset/drop/create/init.
- No source code changes.
- No test changes.
- No migration/rollback changes.
- No business doc changes.
- No permission catalog changes.
- No release tag.
- No push.
- No production readiness claim.

## Recommended Next Gate

Project Owner Phase 1B.10-D production migration plan acceptance.

Required next output:
docs/architecture/phase-1b10d-project-owner-production-migration-plan-acceptance.md
