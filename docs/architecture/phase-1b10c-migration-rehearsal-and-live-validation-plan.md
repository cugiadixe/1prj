# Phase 1B.10-C Migration Rehearsal and Live Validation Plan

## Status

BLOCKED — PROJECT OWNER DECISIONS REQUIRED

## Authorization Source

Reference:
- Phase 1B.10-B Project Owner remediation acceptance commit:
  450602a5ef679937d4b2c47a4673d7cb2b2663d7

- Phase 1B.10-B remediation implementation commit:
  ad1b94048f262dc7972f1546f564a5dc6ce19b62

## Planning Boundary

Confirm:
- planning only.
- no rehearsal execution.
- no live validation execution.
- no production migration.
- no release tag.
- no push.
- no production readiness claim.
- no code/source/test/migration/business-doc changes.

## Accepted Remediation Baseline

- V0015 permission seed alignment migration exists.
- U0015 rollback exists.
- V0015 seeds 12 accepted missing permission codes.
- V0015 seeds SELL_CARE_PACKAGE in Business_Process_Catalog.
- docs/business/permission-catalog.md was aligned for 9 accepted entries.
- SecuritySchemaTests and MigrationRollbackTests were updated.
- build passed with 0 errors / 9 warnings.
- UnitTests 236/236 passed.
- IntegrationTests 203/203 passed.
- ApiTests 308/308 passed.
- Care Package module_code uses SALES to match V0014.
- WORKFLOW_REJECT uses COMPANY scope to match permission catalog.
- workflow definition/binding remains admin UI operational setup.
- staging/pre-prod rehearsal remains future work.
- live validation remains future work.
- production migration remains future work.

## Staging / Pre-Prod Environment Requirements

- Target environment type: staging/pre-prod or equivalent.
- SQL Server version/compatibility: Must match production standards.
- Connection/secrets handling: Secrets injected safely, no hardcoded credentials.
- Database name and isolation: Must be fully isolated from production, e.g., `PTKD_STG`.
- Backup/restore capability: Ability to restore a production-like backup or a sanitized copy.
- Migration execution: Ability to run V0001 through V0015 migrations.
- Rollback execution: Ability to run U0015 rollback rehearsal safely.
- API and frontend connectivity: Backend APIs accessible to the frontend client for validation.
- Admin access: Rehearsal executor must have database creation, backup, restore, and drop privileges on the rehearsal instance.
- Audit/logging visibility: Logs accessible to capture evidence.
- Isolation: STRICT RULE — No production connection allowed during rehearsal.
- Evidence capture: Required ability to capture logs, queries, and screenshots.

## Migration Rehearsal Plan

1. Pre-flight checks: Verify staging environment isolation and connectivity.
2. Backup/restore verification: Take a baseline backup of the staging database before applying any changes.
3. Clean DB rehearsal path: Apply migrations to an empty database to ensure schema integrity from scratch.
4. Production-like restored database path (if available): Restore the sanitized production snapshot and apply migrations over it.
5. Migration order: Execute migrations sequentially from V0001 through V0015.
6. Check V0015 ordering: Verify that V0015 correctly applies after V0014 without errors.
7. Idempotency checks: Rerun the migration tool to confirm V0015 safely skips via idempotency.
8. Permission row verification: Query `dbo.Permissions` to ensure the 12 newly seeded codes are active, existing Phase 1B permissions are intact, and `CARE_PACKAGE_VIEW`, `CARE_PACKAGE_CREATE`, `WORKFLOW_VIEW` remain active and not duplicated. Check that no unexpected duplicate `permission_code` values exist.
9. Process catalog verification: Query `dbo.Business_Process_Catalog` to verify `SELL_CARE_PACKAGE` exists and is active.
10. Security checks: Verify audit/security constraints still enforce expected behavior.
11. Guardrail: No production auto-update allowed.
12. Operational metrics: Record timing and duration of the migration process.
13. Error handling: Document any failures during the migration.
14. Evidence capture: Capture console logs and query output for the execution report.

## Rollback Rehearsal Plan

1. Backup before rollback: Take a staging database snapshot prior to initiating the rollback.
2. Rollback execution: Execute `U0015` rollback exclusively within the rehearsal environment.
3. Verification of deactivated rows: Query `dbo.Permissions` to confirm V0015-introduced rows are safely soft-deactivated (`is_active = 0`) according to `U0015`.
4. Verification of pre-existing rows: Confirm pre-existing permissions (including `CARE_PACKAGE_VIEW`, `CARE_PACKAGE_CREATE`, `WORKFLOW_VIEW`) remain fully intact and active.
5. Verification of process catalog: Confirm `SELL_CARE_PACKAGE` is properly deactivated in `dbo.Business_Process_Catalog`.
6. Restore-forward recovery path: Verify the process to recover to the state before the rollback, or re-apply V0015 after rollback where appropriate.
7. Evidence capture: Record queries and logs showing the state of permissions before and after rollback.
8. Pass/fail criteria: Rollback is considered successful if all target rows are soft-deactivated while leaving all other schema/data unmodified.

## Workflow Definition / Binding Setup Requirements

To be configured operationally before live validation:
- Process key: `SELL_CARE_PACKAGE`.
- Source: `Business_Process_Catalog` seeded by V0015.
- Admin setup: Workflow definition and binding must be manually created via the admin UI.
- Mapping: Required approver roles, groups, and permission mapping must be established for the workflow.
- Lifecycle expectations: Clear submit, approve, and reject paths must be verified.
- Audit expectations: Actions must be recorded in the audit logs.
- Failure handling: Defined protocol for execution failures or stuck states.
- Evidence: Screenshots showing the workflow configuration and bindings in the admin UI.
- Cleanup: Rollback/disable approach defined for the operational setup if required.
- Sign-off: Setup must be reviewed and signed off by the responsible owner.

## Live Validation Data Requirements

- Authenticated test users.
- Valid company context / `X-Company-Id`.
- Users intentionally provisioned with and without the required Phase 1B permissions.
- Customer records available for modification.
- Grave/care target records associated with customers.
- Active service catalog items with effective-date prices.
- Specific care package test cases to validate new workflows.
- Card reprint test cases.
- Valid payment setup.
- Active workflow runtime setup.
- Permission rows correctly linked to active role/group assignments.
- Defined cleanup/reset approach after live testing.
- Audit evidence capture plan.
- strict adherence to data privacy constraints (use sanitized or mock data).

## Live API Validation Plan

The following API scenarios must be validated:

**Care Package:**
- no-approval care package sale.
- approval-required care package sale.
- rejected approval blocks payment.
- duplicate payment guard.
- payment-status read-only.
- activation after confirmed payment where supported.
- company isolation.
- permission-gated actions.

**Card Reprint:**
- request creation path.
- approval/rejection path if applicable.
- payment eligibility path.
- permission-gated actions.

**Payment:**
- VND only.
- full payment only.
- no partial payment.
- no refund.
- no cancellation.
- one bill cannot be paid multiple times.

**Customer:**
- customer permission and runtime row verification relevant to remediation.

**Workflow:**
- workflow permission and runtime row verification.
- submit/approve/reject lifecycle.

**Security:**
- missing/invalid X-Company-Id.
- unauthorized company.
- missing permission.
- safe 400/403/404/409 errors.
- no raw internals exposed.

## Live Frontend / UI Validation Plan

- login/auth setup.
- company context selection.
- permission-gated navigation.
- Care Package list/create/detail.
- lifecycle buttons and visibility.
- safe stale-status / backend 409 handling.
- Card Reprint UI validation where applicable.
- Payment UI validation where applicable.
- no report/export/PDF/print UI expectation where out of scope.
- manual ID selector UX limitations.
- evidence screenshots/log notes required.

## Evidence Capture Requirements

The execution report must include:
- migration command/log output.
- rollback command/log output.
- backup/restore evidence.
- row-count verification.
- permission query results.
- process catalog query results.
- backend/API request/response evidence.
- frontend screenshots or structured notes.
- test user/permission matrix.
- pass/fail table.
- blockers and deviations.
- repository status evidence.
- no production migration evidence.
- no tag/push evidence.

## Pass / Fail Criteria

Possible final statuses for future execution:
- PASSED — READY FOR PROJECT OWNER REHEARSAL/LIVE VALIDATION ACCEPTANCE
- PASSED WITH NOTES — READY FOR PROJECT OWNER REHEARSAL/LIVE VALIDATION ACCEPTANCE
- FAILED / BLOCKED — CORRECTION OR DECISION REQUIRED

Pass requires:
- rehearsal environment available.
- migration rehearsal succeeds.
- rollback rehearsal succeeds or justified rollback boundary is accepted.
- V0015/U0015 behavior verified.
- permission rows verified.
- SELL_CARE_PACKAGE catalog verified.
- workflow definition/binding configured for validation.
- live API validation passes.
- live UI validation passes.
- repository boundary clean.
- no production migration/tag/push.

Blocked if:
- environment unavailable.
- backup/restore cannot be verified.
- migration or rollback fails.
- permission/runtime rows missing.
- workflow cannot be configured.
- live validation cannot run.
- material lifecycle failures occur.
- production migration/tag/push occurs without authorization.

## Project Owner Decisions Required Before Execution

| Decision | Options | Recommendation | Required Before Execution |
|---|---|---|---|
| Exact staging/pre-prod environment | 1. Current Dev DB, 2. Dedicated Staging Server, 3. Cloud Test Environment | Dedicated Staging Server isolated from Dev/Prod | YES |
| Rehearsal data source | 1. Clean DB, 2. Sanitized Prod Snapshot | Sanitized Prod Snapshot | YES |
| Backup/restore owner | 1. DBA Team, 2. DevOps, 3. Dev Lead | DevOps | YES |
| Rehearsal executor | 1. Dev Lead, 2. DevOps, 3. QA Lead | DevOps or Dev Lead | YES |
| Rollback rehearsal boundary | 1. U0015 only, 2. U0015-U0001 full suite | U0015 only | YES |
| Workflow setup owner | 1. Operations Admin, 2. Dev Team, 3. QA Team | Operations Admin | YES |
| Live validation environment | 1. Staging Server, 2. UAT Server | Same as Staging Server | YES |
| Live validation test users | 1. Admin provided accounts, 2. Self-registered mock accounts | Admin provided mock accounts | YES |
| Live validation company and data | 1. Dedicated test company, 2. Global test branch | Dedicated test company | YES |
| Evidence capture owner | 1. QA Lead, 2. Dev Lead | QA Lead | YES |
| Acceptable residual notes | 1. Known minor UX limitations, 2. None | Known minor UX limitations | YES |
| Prod migration planning parallel | 1. Yes, start planning now, 2. No, wait for C acceptance | No, wait for C acceptance | NO |
| Final Prod gates | 1. Combined, 2. Separate explicit authorizations | Separate explicit authorizations | YES |

## Risks

- Environment delays: Staging environment setup may block validation execution.
- Data privacy: Restored snapshots must be properly sanitized to avoid PII leaks in non-production.
- Cross-pollution: Risks if test accounts connect to production databases inadvertently.
- Operational setup: Validation is blocked until the admin UI workflow is configured.

## Non-Goals

Confirm:
- no implementation.
- no source changes.
- no migration changes.
- no business docs changes.
- no permission catalog changes.
- no rehearsal execution.
- no live validation execution.
- no production migration.
- no release tag.
- no push.
- no production readiness claim.

## Recommended Next Gate

Project Owner Phase 1B.10-C open-decision response.

Required next output: docs/architecture/phase-1b10c-project-owner-open-decision-response.md
