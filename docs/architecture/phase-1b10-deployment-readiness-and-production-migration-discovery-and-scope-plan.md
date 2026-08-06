# Phase 1B.10 Deployment Readiness and Production Migration Discovery / Scope Plan

## Status

PROPOSED — READY FOR PROJECT OWNER SCOPE ACCEPTANCE

## Authorization Source

Reference:

- Post-Phase 1B.9 Project Owner next-work decision commit:
  5ac87435db82dbc77f1e4897366616dca401ba2b

- Phase 1B.9 Project Owner closure acceptance commit:
  9c1494a94afca423e59ef9691c6b58d8bb5cd6b4

## Planning Boundary

- Discovery/scope planning only.
- No implementation.
- No source code changes.
- No database migration implementation.
- No production migration execution.
- No release tag.
- No push.
- No production readiness claim.

## Current Project Status

All core Phase 1B feature slices are closed and accepted:

- Phase 1B.1: Security Admin foundation.
- Phase 1B.2: Customer first slice.
- Phase 1B.3: Workflow/Approval engine foundation.
- Phase 1B.4: Customer Master Expansion.
- Phase 1B.5: Customer Merge.
- Phase 1B.6: Service Module Foundation (V0011/U0011, closed).
- Phase 1B.7: Payment / Billing / Collection / Reconciliation Foundation (V0012/U0012, closed).
- Phase 1B.8: Card Reprint (V0013/U0013, closed).
- Phase 1B.9: Care Package Sales (V0014/U0014, closed with deployment readiness notes).

Production migration has been explicitly deferred and not authorized in every phase closure from 1B.6 through 1B.9. No release tag has been created. No push has been performed. The local branch `feature/phase-1-organization` is ahead of origin.

Phase 1B.10 was selected for deployment readiness and production migration discovery/scope planning.

## Carried-Forward Readiness Blockers

### From Phase 1B.9 Closure

1. **SQL permission seed alignment** — CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT exist as code constants but have no database seed rows (V0014 seeds only CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE).

2. **Runtime permission rows** — The 3 unseeded care package permission codes cannot be granted to users/roles at runtime until seed rows exist.

3. **SELL_CARE_PACKAGE workflow runtime configuration** — No migration seeds this process into Business_Process_Catalog. Must be configured via workflow admin UI before approval-required care package operations function.

4. **Live manual API/UI/lifecycle validation** — Not executed during Phase 1B.9-D due to environment unavailability.

### From Phase 1B.8 Closure

5. **Card Reprint permission seed alignment** — 5 card reprint permission codes (CARD_REPRINT_REQUEST_CREATE, CARD_REPRINT_REQUEST_VIEW, CARD_REPRINT_APPROVE, CARD_REPRINT_REQUEST_REJECT, CARD_REPRINT_REQUEST_MARK_PRINTED) exist as code constants but none are seeded in any migration. Runtime permission gating for Card Reprint will not function without seed rows.

6. **No manual/live E2E verification** for Card Reprint (non-blocking but noted).

### From Phase 1B.7 Closure

7. **PAYMENT_PRINT permission** — Seeded in V0012 but no UI exists. Non-blocking; permission row is present.

8. **Frontend test gaps** — PaymentCreatePage and ReconciliationMonthlyPage frontend tests deferred as non-blocking follow-ups.

### From Phase 1B.6 Closure

9. **Live browser/workflow validation** deferred across all phases.

### Cross-Cutting

10. **No production migration** has ever been executed. All 14 migrations (V0001–V0014) exist locally but have never been applied to a production database.

11. **No release tag** has been created.

12. **No push** has been performed to origin for the feature branch.

## Migration Readiness Inventory

### Migration/Rollback Inventory

14 migrations and 14 rollbacks with 1:1 parity:

| Migration | Rollback | Description |
|-----------|----------|-------------|
| V0001 | U0001 | Initial schema |
| V0002 | U0002 | Schema extension |
| V0003 | U0003 | Organization/security permissions seed |
| V0004 | U0004 | Security admin manage permission |
| V0005 | U0005 | Customer permissions seed |
| V0006 | U0006 | Workflow permissions + process catalog seed |
| V0007 | U0007 | Schema extension |
| V0008 | U0008 | Schema extension |
| V0009 | U0009 | Schema extension |
| V0010 | U0010 | Customer merge permissions seed |
| V0011 | U0011 | Service module permissions + process seed |
| V0012 | U0012 | Payment/reconciliation permissions seed |
| V0013 | U0013 | Card reprint schema |
| V0014 | U0014 | Care package schema + partial permission seed |

### Migration Readiness Gaps

1. **No production migration has been executed** — all 14 migrations are local-only.
2. **V0013 (Card Reprint)** — does not seed any card reprint permission rows. 5 permission codes are code-only constants with no DB seed.
3. **V0014 (Care Package)** — seeds only CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE. Three codes (APPROVE, REJECT, CREATE_PAYMENT) are unseeded.
4. **No migration rehearsal** on staging/pre-production has been performed.
5. **No rollback rehearsal** has been performed.
6. **No backup/restore plan** exists for production migration.
7. **Naming convention** is consistent: `V{NNNN}__{description}.sql` / `U{NNNN}__{description}.sql`.
8. **No environment-specific behavior** observed in migration scripts.
9. **Integration tests** cover migration application but do not rehearse production deployment.

### Workflow Process Seed Status

Seeded processes (in migrations):
- CREATE_CUSTOMER (V0006)
- CUSTOMER_MASTER_CHANGE (V0006)
- SERVICE_PRICE_OVERRIDE (V0011)
- RENEW_SERVICE_STANDARD (V0011)

Not seeded (code-only, requires runtime admin configuration):
- SELL_CARE_PACKAGE — referenced in CarePackageRequestService.cs and CarePackageExecutionHandler.cs but no migration inserts it into Business_Process_Catalog.

## Permission Seed / Runtime Permission Inventory

### Alignment Summary

| Permission Code | In PermissionCodes.cs | Seeded in Migration | In Permission Catalog | Status |
|---|---|---|---|---|
| CARE_PACKAGE_VIEW | Yes | Yes (V0014) | No | CODE + SEED, CATALOG MISSING |
| CARE_PACKAGE_CREATE | Yes | Yes (V0014) | No | CODE + SEED, CATALOG MISSING |
| CARE_PACKAGE_APPROVE | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARE_PACKAGE_REJECT | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARE_PACKAGE_CREATE_PAYMENT | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARD_REPRINT_REQUEST_CREATE | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARD_REPRINT_REQUEST_VIEW | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARD_REPRINT_APPROVE | Yes | No | Yes | CODE + CATALOG, MISSING SEED |
| CARD_REPRINT_REQUEST_REJECT | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| CARD_REPRINT_REQUEST_MARK_PRINTED | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| WORKFLOW_VIEW | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| WORKFLOW_REJECT | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| WORKFLOW_RETRY_EXECUTION | Yes | No | No | CODE ONLY — MISSING SEED + CATALOG |
| ORGANIZATION_USER_MANAGE | Yes | No | Yes | CODE + CATALOG, MISSING SEED |
| CUSTOMER_CHANGE_REQUEST_CREATE | Yes | No | Yes | CODE + CATALOG, MISSING SEED |
| PAYMENT_PRINT | No (not in PermissionCodes.cs) | Yes (V0012) | Yes | SEED + CATALOG, NO CODE CONSTANT |

### Permissions Fully Aligned (code + seed + catalog)

All permissions seeded in V0003–V0006, V0010–V0012 that also appear in both PermissionCodes.cs and the permission catalog are confirmed aligned. These include security, organization, customer, workflow config, customer merge, service, and payment/reconciliation permissions from earlier phases.

### Permission Gaps Requiring Resolution

**13 permission codes** in PermissionCodes.cs lack database seed rows:

- 3 Care Package codes: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT
- 5 Card Reprint codes: CARD_REPRINT_REQUEST_CREATE, CARD_REPRINT_REQUEST_VIEW, CARD_REPRINT_APPROVE, CARD_REPRINT_REQUEST_REJECT, CARD_REPRINT_REQUEST_MARK_PRINTED
- 3 Workflow codes: WORKFLOW_VIEW, WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION
- 1 Organization code: ORGANIZATION_USER_MANAGE
- 1 Customer code: CUSTOMER_CHANGE_REQUEST_CREATE

**9 permission codes** in PermissionCodes.cs are absent from docs/business/permission-catalog.md:

- 5 Care Package codes (all)
- 4 Card Reprint codes (all except CARD_REPRINT_APPROVE)
- 3 Workflow codes (VIEW, REJECT, RETRY_EXECUTION)

**1 permission code** seeded in migration with no code constant:

- PAYMENT_PRINT — seeded in V0012, in catalog, but no PermissionCodes.cs constant (no UI exists; non-blocking).

### Decision Required

The Project Owner must decide:
- Whether to add the 13 missing permission seeds via a new migration (V0015).
- Whether to update docs/business/permission-catalog.md for the 9+ missing entries.
- Whether PAYMENT_PRINT needs a code constant or remains DB/catalog-only until a UI is built.

## Workflow Runtime Configuration Inventory

### Process Keys Requiring Runtime Configuration

| Process Key | Code Implementation | Migration Seed | Runtime Config Status | Gap |
|---|---|---|---|---|
| SELL_CARE_PACKAGE | CarePackageRequestService.cs, CarePackageExecutionHandler.cs | Not seeded | Not configured | **BLOCKER** — must be configured via workflow admin UI or seeded via migration before approval-required care package operations function |
| CREATE_CUSTOMER | Used in customer workflow | Seeded (V0006) | Assumed configured | Verify at deployment |
| CUSTOMER_MASTER_CHANGE | Used in customer workflow | Seeded (V0006) | Assumed configured | Verify at deployment |
| SERVICE_PRICE_OVERRIDE | Used in service workflow | Seeded (V0011) | Assumed configured | Verify at deployment |
| RENEW_SERVICE_STANDARD | Used in service workflow | Seeded (V0011) | Assumed configured | Verify at deployment |

### Decision Required

- Whether SELL_CARE_PACKAGE should be seeded via a new migration (V0015) alongside permission seeds, or configured manually via workflow admin UI after deployment.
- Whether seeded processes (CREATE_CUSTOMER, CUSTOMER_MASTER_CHANGE, SERVICE_PRICE_OVERRIDE, RENEW_SERVICE_STANDARD) need verification of their runtime configuration completeness.

## Production Migration Prerequisites

### Environment

1. **Target environment identification** — Production SQL Server instance must be identified. Connection string and credentials must be available to the migration executor (not stored in code or chat).
2. **Staging/pre-production environment** — A staging environment should be available for migration rehearsal before production execution.

### Backup and Recovery

3. **Backup plan** — Full database backup before migration execution. Verified restore capability.
4. **Restore verification** — Test restore from backup before migration to confirm recovery path.
5. **Rollback rehearsal** — Execute rollback sequence (U0014 through U0001 or relevant subset) on staging to verify clean rollback.

### Migration Execution

6. **Migration order** — V0001 through V0014, sequential, in order.
7. **Migration locking/concurrency** — Determine whether migrations require exclusive database access or can run with concurrent application connections.
8. **Expected downtime** — Define whether migration requires planned downtime or can execute online. Decision required from Project Owner.
9. **No production auto-update** — Migrations must be manually triggered. No auto-migration on application startup. Confirm application startup does not auto-apply migrations.

### Post-Migration

10. **Audit/logging** — Migration execution must produce audit trail: who executed, when, which migrations applied, success/failure.
11. **Smoke tests** — Manual API smoke tests after migration: authentication, customer CRUD, workflow, payment, card reprint, care package lifecycle.
12. **Permission verification** — After migration, verify all seeded permission rows exist and are grantable.
13. **Workflow config verification** — After migration, verify seeded process catalog entries exist.

### Sign-Off

14. **Go/no-go criteria** — Backup verified, staging rehearsal passed, rollback rehearsal passed, all migration scripts reviewed.
15. **Rollback criteria** — Any migration failure triggers rollback to last known good state from backup.
16. **Data safety criteria** — No data loss. Migration scripts are additive (schema creation, seed inserts). Destructive operations (if any) must be reviewed.
17. **Sign-off roles** — Project Owner authorizes production migration execution. Technical executor performs migration. Both confirm success.

### Release

18. **Release tag policy** — Release tag creation is a separate gate, authorized only after successful production migration and post-migration verification.
19. **Push/deployment boundary** — Push to origin is a separate gate, authorized only after release tag decision.

## Live Validation Readiness Inventory

### Prerequisites for Live Validation

1. **Live API environment** — Running PTKD.Api application connected to a database with all 14 migrations applied.
2. **Live frontend environment** — Running React SPA connected to the live API.
3. **Authenticated test users** — At least 2 users with different permission sets for testing permission gating and workflow approval (requester vs approver).
4. **Company context** — At least 1 company entity with X-Company-Id for company-scoped operations.
5. **Seed data** — Customers, graves/care targets, services with effective-date pricing, payment configuration.
6. **Runtime permission rows** — All 27+ permission codes must be seeded and grantable. The 13 currently unseeded codes must be resolved first.
7. **Workflow runtime config** — SELL_CARE_PACKAGE (and other process keys) must be configured in workflow admin.
8. **Payment Foundation setup** — Payment configuration for VND, full-payment-only constraints.

### Validation Scenarios

- Care package: no-approval path (create, pay, activate).
- Care package: approval-required path (create, submit, approve, pay, activate).
- Care package: rejection path (create, submit, reject).
- Care package: duplicate payment guard.
- Care package: company isolation.
- Care package: permission gating (view, create, approve, reject, create-payment).
- Card reprint: create, approve, mark-printed lifecycle.
- Payment: draft, confirm, correct-confirmed.
- Reconciliation: prepare, confirm.
- Customer: create, view, merge.
- Service: create, renew, price override.
- Workflow: submit, approve, reject, reassign.
- Security admin: user management, role assignment, permission grant.

### Evidence Capture

- Screenshot or log evidence for each scenario.
- Pass/fail recording per scenario.
- Regression check for previously validated automated scenarios.

## Scope Options

### Option A — Discovery Complete (this document)

This document completes the discovery/scope planning gate. No further implementation is performed.

**Next step**: Project Owner scope acceptance, then proceed to Option B.

### Option B — Deployment Readiness Remediation Planning

Plan the specific remediation tasks required to resolve deployment readiness blockers:

- Permission seed migration (V0015) for the 13 unseeded permission codes.
- Permission catalog update for 9+ missing entries.
- SELL_CARE_PACKAGE process catalog seed decision (migration vs manual admin).
- Migration rehearsal plan.
- Live validation plan.

**Output**: Phase 1B.10-A deployment readiness remediation implementation plan.

**Requires**: Project Owner acceptance of this scope plan + open-decision resolution.

### Option C — Deployment Readiness Remediation Implementation

Execute remediation:

- Create V0015/U0015 migration for permission seeds and optionally SELL_CARE_PACKAGE process seed.
- Update permission catalog if authorized.
- Run automated tests.
- Acceptance review.

**Requires**: Accepted remediation plan from Option B.

### Option D — Production Migration Execution

Execute V0001–V0015 (or V0014 if no V0015) on production database.

- Backup, rehearsal, execution, post-migration verification.
- Live validation execution.
- Post-migration acceptance.

**Requires**: Accepted remediation from Option C, staging rehearsal, Project Owner go/no-go authorization.

### Option E — Release Tag and Push

Create release tag and push to origin.

**Requires**: Successful production migration from Option D, post-migration acceptance, Project Owner release authorization.

## Recommended Phase 1B.10 Scope

**Recommended immediate scope after PO acceptance of this plan**: Option B — Deployment Readiness Remediation Planning.

Rationale:
- 13 unseeded permission codes and 1 unseeded workflow process key are concrete blockers that must be resolved before production migration.
- A remediation plan is needed before implementation to confirm scope, decide open questions (migration vs manual config for SELL_CARE_PACKAGE), and get PO acceptance.
- Implementation (Option C) and production migration (Option D) should follow sequentially after the plan is accepted.

**Recommended gate sequence**:

1. PO accepts this discovery/scope plan (current gate).
2. Open-decision resolution (if PO prefers to resolve decisions before planning).
3. Phase 1B.10-A remediation implementation plan.
4. PO accepts remediation plan.
5. Phase 1B.10-B remediation implementation (V0015 migration, catalog update, tests).
6. PO accepts remediation implementation.
7. Phase 1B.10-C production migration planning (environment, rehearsal, backup).
8. PO authorizes production migration execution.
9. Phase 1B.10-D production migration execution and live validation.
10. PO accepts production migration result.
11. Phase 1B.10-E release tag and push (separately authorized).

## Open Decisions Required

### Decision 1: Permission Seed Migration Scope

Should a new migration (V0015) be created to seed the 13 missing permission codes?

Affected codes:
- CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT
- CARD_REPRINT_REQUEST_CREATE, CARD_REPRINT_REQUEST_VIEW, CARD_REPRINT_APPROVE, CARD_REPRINT_REQUEST_REJECT, CARD_REPRINT_REQUEST_MARK_PRINTED
- WORKFLOW_VIEW, WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION
- ORGANIZATION_USER_MANAGE
- CUSTOMER_CHANGE_REQUEST_CREATE

### Decision 2: SELL_CARE_PACKAGE Configuration Method

Should SELL_CARE_PACKAGE be seeded via V0015 migration (like CREATE_CUSTOMER, SERVICE_PRICE_OVERRIDE) or configured manually via workflow admin UI after deployment?

Trade-off: migration seed ensures availability at deployment; manual config requires admin action post-deployment but matches the closure review's stated approach.

### Decision 3: Permission Catalog Update

Should docs/business/permission-catalog.md be updated to include the 9+ missing permission code entries as part of Phase 1B.10?

This would require separate authorization since business doc changes are not authorized in the current planning task.

### Decision 4: PAYMENT_PRINT Code Constant

PAYMENT_PRINT is seeded in V0012 and in the permission catalog but has no PermissionCodes.cs constant (no UI exists). Should a constant be added, or is this deferred until a Payment Print UI is built?

### Decision 5: Migration Rehearsal Environment

Which environment is available for migration rehearsal (staging, pre-production, local with production-like data)?

### Decision 6: Live Validation Environment

Is a live environment available for manual API/UI/lifecycle validation? What infrastructure is needed?

### Decision 7: Production Readiness Acceptance Criteria

What constitutes production readiness acceptance? Proposed minimum:
- All permission seeds applied.
- All workflow process keys configured.
- Migration rehearsal passed on staging.
- Rollback rehearsal passed.
- Live validation passed for core scenarios.
- Project Owner sign-off.

### Decision 8: Release Tag and Push as Separate Gates

Should release tag creation and push to origin remain separate authorization gates after production migration?

### Decision 9: Production Migration Executor

Who performs production migration execution? Project Owner, technical lead, or designated operator?

### Decision 10: Manual Operational Setup Acceptance

Can any deployment readiness blocker be accepted as a manual operational setup task (e.g., SELL_CARE_PACKAGE configured via admin UI post-deployment) rather than requiring a migration seed?

## Risks

1. **Permission seed gaps** — 13 permission codes cannot be granted at runtime until seeded. Card Reprint and Care Package approval/rejection/payment workflows will not function in production without these seeds.

2. **SELL_CARE_PACKAGE not seeded** — Approval-required care package operations will fail at runtime without workflow process configuration.

3. **No migration rehearsal** — No migration has been tested against a production-like database. Unknown schema conflicts, data issues, or performance impacts.

4. **No rollback rehearsal** — Rollback scripts exist but have not been exercised against a production-like database.

5. **No live validation** — No manual API/UI/lifecycle testing has been performed for any phase. Automated tests provide coverage but cannot confirm runtime behavior in a live environment.

6. **Environment availability** — Staging/production environment readiness is unknown. Migration execution depends on environment access.

7. **Data migration assumptions** — Migrations are additive (schema creation, seed inserts). If production has existing data, migration compatibility must be verified.

8. **Branch divergence** — Local branch is ahead of origin. Push requires authorization and may require merge conflict resolution if origin has changed.

## Non-Goals

- No implementation in this planning task.
- No source code changes.
- No migration implementation.
- No production migration execution.
- No release tag.
- No push.
- No production readiness claim.
- No new business features.
- No unrelated business docs changes.
- No permission catalog changes unless later authorized.
- No refund/cancellation/partial payment implementation.
- No dynamic PDF/template generation.
- No report/export UI.

## Recommended Next Gate

Project Owner Phase 1B.10 discovery/scope acceptance.

No Phase 1B.10 implementation, migration, production migration, tag, or push may begin until separately authorized after this scope plan is accepted.
