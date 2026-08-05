# Phase 1B.10-A Deployment Readiness Remediation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER REMEDIATION PLAN ACCEPTANCE

## Authorization Source

Reference:

- Phase 1B.10 Project Owner scope acceptance commit:
  082827b4ddf6bafd0decb033f8d74ca1a564ccf9

- Phase 1B.10 discovery/scope plan commit:
  632b51328f94b3b60d873b5a7e9e41c61ceb1d9b

## Planning Boundary

- Remediation planning only.
- No source code changes.
- No migration implementation.
- No permission catalog changes.
- No workflow config implementation.
- No validation execution.
- No production migration.
- No release tag.
- No push.
- No production readiness claim.

## Accepted Readiness Findings

- All 9 core Phase 1B feature slices (1B.1–1B.9) are closed and accepted.
- Production migration has been deferred across every phase closure from 1B.6 through 1B.9.
- 14 migrations (V0001–V0014) and 14 rollbacks (U0001–U0014) exist with 1:1 parity.
- No production migration has been executed. No migration rehearsal has been performed.
- Live manual API/UI/lifecycle validation was not executed in any phase.
- 12 permission codes in PermissionCodes.cs lack database seed rows.
- 9 permission codes are missing from docs/business/permission-catalog.md.
- CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE are seeded in V0014.
- SELL_CARE_PACKAGE is code-only — not seeded in Business_Process_Catalog.
- No auto-migration on application startup (Program.cs confirmed).
- Migrations are managed externally via script runner (dbo.SchemaVersions tracking).

## Remediation Objectives

1. Permission seed alignment — seed the 12 missing permission rows in dbo.Permissions.
2. Runtime permission row alignment — ensure all seeded permissions are grantable at runtime.
3. Permission catalog alignment — update docs/business/permission-catalog.md for 9 missing entries (requires PO decision).
4. SELL_CARE_PACKAGE workflow runtime configuration — seed or configure the process catalog entry.
5. Migration rehearsal readiness — plan rehearsal on staging/pre-prod.
6. Live validation readiness — plan live manual API/UI/lifecycle validation.
7. Production migration readiness — plan production execution after rehearsal.

## Permission Gap Inventory

### Codes Requiring Database Seed (V0015)

12 permission codes exist in PermissionCodes.cs but have no INSERT in any migration:

| # | Permission Code | Module | Scope | In Catalog | Phase Origin |
|---|---|---|---|---|---|
| 1 | CARE_PACKAGE_APPROVE | CARE_PACKAGE | COMPANY | No | 1B.9 |
| 2 | CARE_PACKAGE_REJECT | CARE_PACKAGE | COMPANY | No | 1B.9 |
| 3 | CARE_PACKAGE_CREATE_PAYMENT | CARE_PACKAGE | COMPANY | No | 1B.9 |
| 4 | CARD_REPRINT_REQUEST_CREATE | CARD_REPRINT | COMPANY | No | 1B.8 |
| 5 | CARD_REPRINT_REQUEST_VIEW | CARD_REPRINT | COMPANY | No | 1B.8 |
| 6 | CARD_REPRINT_APPROVE | CARD_REPRINT | COMPANY | Yes | 1B.8 |
| 7 | CARD_REPRINT_REQUEST_REJECT | CARD_REPRINT | COMPANY | No | 1B.8 |
| 8 | CARD_REPRINT_REQUEST_MARK_PRINTED | CARD_REPRINT | COMPANY | No | 1B.8 |
| 9 | WORKFLOW_REJECT | WORKFLOW | GLOBAL | Yes | 1B.3 |
| 10 | WORKFLOW_RETRY_EXECUTION | WORKFLOW | GLOBAL | Yes | 1B.3 |
| 11 | ORGANIZATION_USER_MANAGE | ORGANIZATION | GLOBAL | Yes | 1B.1 |
| 12 | CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER | COMPANY | Yes | 1B.4 |

### Codes Already Seeded (No Remediation Needed)

| Permission Code | Seeded In | In Catalog | Status |
|---|---|---|---|
| CARE_PACKAGE_VIEW | V0014 | No | SEED OK, CATALOG MISSING |
| CARE_PACKAGE_CREATE | V0014 | No | SEED OK, CATALOG MISSING |
| WORKFLOW_VIEW | V0006 | Yes | FULLY ALIGNED |
| WORKFLOW_CONFIG_MANAGE | V0006 | Yes | FULLY ALIGNED |
| WORKFLOW_PUBLISH | V0006 | Yes | FULLY ALIGNED |
| WORKFLOW_BIND_PROCESS | V0006 | Yes | FULLY ALIGNED |
| WORKFLOW_REASSIGN_PENDING | V0006 | Yes | FULLY ALIGNED |
| WORKFLOW_AUDIT_VIEW | V0006 | Yes | FULLY ALIGNED |
| PAYMENT_PRINT | V0012 | Yes | SEED + CATALOG OK, NO CODE CONSTANT |

### Permission Catalog Gaps

9 codes in PermissionCodes.cs are absent from docs/business/permission-catalog.md:

| # | Permission Code | Has DB Seed | Catalog Status |
|---|---|---|---|
| 1 | CARE_PACKAGE_VIEW | Yes (V0014) | MISSING |
| 2 | CARE_PACKAGE_CREATE | Yes (V0014) | MISSING |
| 3 | CARE_PACKAGE_APPROVE | No | MISSING |
| 4 | CARE_PACKAGE_REJECT | No | MISSING |
| 5 | CARE_PACKAGE_CREATE_PAYMENT | No | MISSING |
| 6 | CARD_REPRINT_REQUEST_CREATE | No | MISSING |
| 7 | CARD_REPRINT_REQUEST_VIEW | No | MISSING |
| 8 | CARD_REPRINT_REQUEST_REJECT | No | MISSING |
| 9 | CARD_REPRINT_REQUEST_MARK_PRINTED | No | MISSING |

Note: CARD_REPRINT_APPROVE is in the catalog but not seeded. WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION, ORGANIZATION_USER_MANAGE, and CUSTOMER_CHANGE_REQUEST_CREATE are in the catalog but not seeded.

### PAYMENT_PRINT Special Case

PAYMENT_PRINT is seeded in V0012 and in the permission catalog but has no PermissionCodes.cs constant. No UI exists for Payment Print. This is non-blocking — the DB row and catalog entry exist. Adding a code constant is deferred until a Payment Print UI is built.

## Permission Seed Remediation Plan

### Recommended Approach

Create a single future migration V0015 to seed all 12 missing permission rows.

Candidate migration file:
`database/migrations/V0015__deployment_readiness_permission_seed_alignment.sql`

Candidate rollback file:
`database/rollbacks/U0015__deployment_readiness_permission_seed_alignment.sql`

### SQL Pattern

Follow the established pattern from V0003–V0012 (not the V0014 variation):

```sql
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope,
     is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('PERMISSION_CODE', 'MODULE', 'ACTION', 'SCOPE', 0, 0, 0, 1, N'Description.');
```

All 9 standard columns must be explicit. Use N-prefixed description strings for consistency. Set `is_active = 1` explicitly.

### Idempotency

Use `IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = '...')` guard before each INSERT to ensure idempotent re-runs. This follows the V0011 pattern.

### Scope Assignment

- CARE_PACKAGE_* codes: `COMPANY` scope (matches CARE_PACKAGE_VIEW/CREATE in V0014).
- CARD_REPRINT_* codes: `COMPANY` scope (matches card reprint business logic — company-scoped operations).
- WORKFLOW_REJECT, WORKFLOW_RETRY_EXECUTION: `GLOBAL` scope (matches other workflow permissions in V0006).
- ORGANIZATION_USER_MANAGE: `GLOBAL` scope (matches other organization permissions in V0003).
- CUSTOMER_CHANGE_REQUEST_CREATE: `COMPANY` scope (matches other customer permissions in V0005).

### Planned Permission Rows

| permission_code | module_code | action_code | data_scope |
|---|---|---|---|
| CARE_PACKAGE_APPROVE | CARE_PACKAGE | APPROVE | COMPANY |
| CARE_PACKAGE_REJECT | CARE_PACKAGE | REJECT | COMPANY |
| CARE_PACKAGE_CREATE_PAYMENT | CARE_PACKAGE | CREATE_PAYMENT | COMPANY |
| CARD_REPRINT_REQUEST_CREATE | CARD_REPRINT | REQUEST_CREATE | COMPANY |
| CARD_REPRINT_REQUEST_VIEW | CARD_REPRINT | REQUEST_VIEW | COMPANY |
| CARD_REPRINT_APPROVE | CARD_REPRINT | APPROVE | COMPANY |
| CARD_REPRINT_REQUEST_REJECT | CARD_REPRINT | REQUEST_REJECT | COMPANY |
| CARD_REPRINT_REQUEST_MARK_PRINTED | CARD_REPRINT | REQUEST_MARK_PRINTED | COMPANY |
| WORKFLOW_REJECT | WORKFLOW | REJECT | GLOBAL |
| WORKFLOW_RETRY_EXECUTION | WORKFLOW | RETRY_EXECUTION | GLOBAL |
| ORGANIZATION_USER_MANAGE | ORGANIZATION | USER_MANAGE | GLOBAL |
| CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER | CHANGE_REQUEST_CREATE | COMPANY |

All rows: `is_sensitive = 0`, `requires_reason = 0`, `is_delegable = 0`, `is_active = 1`.

### Rollback Approach

Follow the standard soft-deactivation pattern (not the V0014 hard-delete pattern):

```sql
UPDATE dbo.Permissions SET is_active = 0
WHERE permission_code IN ('CARE_PACKAGE_APPROVE', ...);

DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0015%';
```

This respects the `TR_Permissions_PreventDelete` trigger. The V0014 rollback's hard-delete approach (disabling trigger, DELETE, re-enabling) is noted as inconsistent — V0015 rollback should not follow that pattern.

### Runtime Verification

After V0015 is applied:
- Query `SELECT * FROM dbo.Permissions WHERE permission_code IN (...)` to confirm all 12 rows exist with `is_active = 1`.
- Verify permissions are grantable to roles/users via the security admin UI.

### Test Validation

- Existing integration tests should continue to pass.
- New or updated tests may verify the 12 new permission rows exist after migration.
- API tests for Card Reprint and Care Package should confirm permission gating functions with the seeded rows.

## Permission Catalog Alignment Plan

### Scope

9 permission codes must be added to docs/business/permission-catalog.md:
- 5 CARE_PACKAGE codes (VIEW, CREATE, APPROVE, REJECT, CREATE_PAYMENT).
- 4 CARD_REPRINT codes (REQUEST_CREATE, REQUEST_VIEW, REQUEST_REJECT, REQUEST_MARK_PRINTED).

CARD_REPRINT_APPROVE is already in the catalog.

### Decision Required

Permission catalog updates require separate authorization (business doc modification). This plan recommends including catalog alignment in the Phase 1B.10-B remediation implementation scope, subject to PO authorization.

### Approach

If authorized, add 9 entries to the permission catalog following the existing format: permission code, module, action, scope, sensitivity, delegation, description. Match values to the V0015 migration seed rows for consistency.

## Workflow Runtime Configuration Remediation Plan

### SELL_CARE_PACKAGE

**Current state**: Code references `SELL_CARE_PACKAGE` as ProcessCode in CarePackageRequestService.cs and CarePackageExecutionHandler.cs. No migration seeds it into dbo.Business_Process_Catalog.

**Recommended approach**: Seed SELL_CARE_PACKAGE via V0015 migration alongside permission seeds.

Planned row:

| process_code | process_name | description | is_approval_required | is_active |
|---|---|---|---|---|
| SELL_CARE_PACKAGE | Bán gói chăm sóc | Care package sales workflow process | 1 | 1 |

Use `IF NOT EXISTS` guard for idempotency, following the V0011 pattern.

Rollback: `UPDATE dbo.Business_Process_Catalog SET is_active = 0 WHERE process_code = 'SELL_CARE_PACKAGE';`

**Runtime workflow configuration**: After the catalog entry exists, workflow definitions (steps, transitions, bindings) must be configured via the workflow admin UI. This is an operational setup task — the migration seeds only the catalog entry, not the full workflow definition.

**Alternative**: Configure SELL_CARE_PACKAGE entirely via workflow admin UI after deployment (no migration seed). This matches the Phase 1B.9 closure review's stated approach. However, seeding the catalog entry via migration ensures the process key exists at deployment time, reducing operational risk.

**Recommendation**: Seed the catalog entry via V0015 migration. Workflow definition/binding configuration remains an operational setup task via admin UI.

### Other Process Keys

CREATE_CUSTOMER, CUSTOMER_MASTER_CHANGE (V0006), SERVICE_PRICE_OVERRIDE, RENEW_SERVICE_STANDARD (V0011) are already seeded. No remediation needed.

Card Reprint does not use a workflow process key — card reprint approval uses direct permission-based approval, not the workflow engine. No workflow catalog remediation needed for Card Reprint.

## Migration Rehearsal and Rollback Rehearsal Plan

### Environment

- Staging or pre-production SQL Server instance required.
- SQL Server version must match production target (version to be confirmed by PO/infra).
- Clean database or production-like database clone.

### Rehearsal Sequence

1. **Backup**: Full database backup before rehearsal.
2. **Forward migration**: Execute V0001 through V0015 (if V0015 is authorized) in order.
3. **Verification after forward migration**:
   - Confirm dbo.SchemaVersions contains entries for V0001–V0015.
   - Confirm dbo.Permissions contains all expected rows (44+ permissions across all migrations).
   - Confirm dbo.Business_Process_Catalog contains all expected entries (5 process keys: CREATE_CUSTOMER, CUSTOMER_MASTER_CHANGE, SERVICE_PRICE_OVERRIDE, RENEW_SERVICE_STANDARD, SELL_CARE_PACKAGE).
   - Spot-check schema tables exist (customers, care_package_requests, card_reprint_requests, etc.).
   - Row count checks on seed tables.
4. **Rollback rehearsal**: Execute U0015 (if exists), then selectively test U0014, U0013 to verify rollback path.
5. **Verification after rollback**: Confirm affected permissions are soft-deactivated. Confirm schema changes are reversed.
6. **Restore**: Restore from backup to confirm recovery path.

### Safety Checks

- No concurrent application connections during rehearsal migration.
- No production auto-update (confirmed: Program.cs does not auto-migrate).
- All migration scripts reviewed before execution.
- Rollback scripts reviewed before execution.

### Go/No-Go Criteria

- All migrations apply without error.
- All expected seed rows exist.
- At least one rollback executes without error.
- Restore from backup succeeds.

### Evidence

- Migration execution log (timestamps, success/failure per script).
- Row count verification queries.
- Rollback execution log.
- Restore verification confirmation.

### Sign-Off

- Technical executor confirms rehearsal passed.
- Project Owner confirms go/no-go for production migration.

## Live Validation Readiness Plan

### Prerequisites

1. **Running backend API**: PTKD.Api running against a database with all migrations (V0001–V0015) applied.
2. **Running frontend**: React SPA connected to the live API.
3. **Authenticated test users**: At least 2 users — one with full permissions (admin), one with limited permissions (to test permission gating).
4. **Company context**: At least 1 company entity. X-Company-Id header must be valid.
5. **Seed data**:
   - At least 1 customer.
   - At least 1 grave/care target.
   - At least 1 active service with effective-date pricing.
   - Payment configuration for VND.
6. **Runtime permission rows**: All 12 V0015 permissions must be seeded and grantable.
7. **Workflow runtime config**: SELL_CARE_PACKAGE must be configured (catalog entry + workflow definition/binding via admin UI).
8. **Payment Foundation**: VND-only, full-payment-only constraints active.

### Validation Scenarios

| # | Scenario | Module | Key Checks |
|---|---|---|---|
| 1 | Care package no-approval path | Care Package | Create with configured price, no discount → PaymentEligible → Pay → Active |
| 2 | Care package approval-required path | Care Package | Create with discount → Submit → PendingApproval → Approve → PaymentEligible → Pay → Active |
| 3 | Care package rejection path | Care Package | Create → Submit → Reject → verify blocked from payment |
| 4 | Duplicate payment guard | Care Package | Attempt second payment on already-paid package → blocked |
| 5 | Company isolation | Care Package | Verify packages from company A not visible to company B |
| 6 | Permission-gated actions | Care Package | Verify VIEW/CREATE/APPROVE/REJECT/CREATE_PAYMENT gates |
| 7 | Card reprint lifecycle | Card Reprint | Create request → Approve → Mark printed |
| 8 | Card reprint permission gating | Card Reprint | Verify REQUEST_CREATE/VIEW/APPROVE/REJECT/MARK_PRINTED gates |
| 9 | Payment lifecycle | Payment | Create draft → Confirm → verify status |
| 10 | Customer CRUD | Customer | Create → View → Update → verify |
| 11 | Workflow admin | Workflow | View workflow config → verify bindings |
| 12 | Security admin | Security | Grant/revoke permissions → verify access changes |

### Evidence Capture

- Screenshot or API response log for each scenario.
- Pass/fail per scenario.
- Any unexpected errors documented.

### Pass/Fail Criteria

- All 12 scenarios pass: LIVE VALIDATION PASSED.
- Any critical scenario fails (1–6, 7, 9): LIVE VALIDATION FAILED — remediation required.
- Non-critical scenario fails (10–12): LIVE VALIDATION PASSED WITH NOTES.

### Test Data Reset

- After validation, test data may be cleaned or retained for future reference.
- No production data is affected.

## Future Implementation Sequence

Recommended sequence after PO accepts this remediation plan:

1. **PO accepts Phase 1B.10-A remediation plan** (current gate).

2. **Phase 1B.10-B remediation implementation**:
   - Create V0015/U0015 migration and rollback for 12 permission seeds + SELL_CARE_PACKAGE catalog entry.
   - Update permission catalog (9 entries) if authorized.
   - Run automated tests (build, unit, integration, API, frontend).
   - Acceptance review.
   - PO accepts remediation implementation.

3. **Phase 1B.10-C migration rehearsal and live validation**:
   - Execute migration rehearsal on staging.
   - Execute rollback rehearsal.
   - Execute live validation scenarios.
   - PO accepts rehearsal and validation results.

4. **Phase 1B.10-D production migration execution** (separately authorized):
   - Backup production database.
   - Execute V0001–V0015 on production.
   - Post-migration verification.
   - PO accepts production migration result.

5. **Phase 1B.10-E release tag and push** (separately authorized):
   - Create release tag.
   - Push to origin.
   - PO authorizes release.

Each step requires separate explicit PO authorization. No step may begin until the previous step is accepted.

## Project Owner Decisions Required

### Decision 1: Permission Seed Migration Scope

**Question**: Should V0015 seed all 12 missing permission codes in a single migration?

**Options**:
- A) Single V0015 for all 12 codes (recommended).
- B) Separate migrations per module (V0015 for Care Package, V0016 for Card Reprint, etc.).
- C) Defer some codes if not immediately needed.

**Recommendation**: Option A — single migration is simpler, all codes are needed for runtime permission gating, and the seed pattern is established.

**Required before**: Phase 1B.10-B implementation.

### Decision 2: SELL_CARE_PACKAGE Configuration Method

**Question**: Should SELL_CARE_PACKAGE be seeded via V0015 migration or configured manually via workflow admin UI?

**Options**:
- A) Seed catalog entry in V0015 migration; workflow definition/binding configured via admin UI (recommended).
- B) Configure entirely via workflow admin UI after deployment (no migration seed).

**Recommendation**: Option A — seeding the catalog entry reduces operational risk. Workflow definition still requires admin UI configuration regardless.

**Required before**: Phase 1B.10-B implementation.

### Decision 3: Permission Catalog Update

**Question**: Should docs/business/permission-catalog.md be updated for the 9 missing entries as part of Phase 1B.10-B?

**Options**:
- A) Yes, include catalog update in Phase 1B.10-B (recommended).
- B) No, defer to a separate task.

**Recommendation**: Option A — keeps code, DB seeds, and documentation aligned. Requires explicit PO authorization for business doc modification.

**Required before**: Phase 1B.10-B implementation.

### Decision 4: PAYMENT_PRINT Code Constant

**Question**: Should a PermissionCodes.cs constant be added for PAYMENT_PRINT?

**Options**:
- A) Add constant now (no UI uses it yet).
- B) Defer until Payment Print UI is built (recommended).

**Recommendation**: Option B — adding an unused constant is unnecessary. The DB seed and catalog entry are sufficient.

**Required before**: Not blocking for Phase 1B.10.

### Decision 5: Migration Rehearsal Environment

**Question**: Which environment is available for migration rehearsal?

**Options**:
- A) Dedicated staging SQL Server instance.
- B) Local SQL Server with production-like configuration.
- C) Docker-based SQL Server for CI/CD rehearsal.

**Recommendation**: Any option is acceptable. PO/infra must confirm availability.

**Required before**: Phase 1B.10-C rehearsal.

### Decision 6: Live Validation Environment

**Question**: Is a live environment available for manual API/UI/lifecycle validation?

**Options**:
- A) Dedicated staging environment with running API + frontend.
- B) Local development environment.
- C) Not yet available — define what's needed.

**Recommendation**: PO/infra must confirm availability. This blocks Phase 1B.10-C.

**Required before**: Phase 1B.10-C validation.

### Decision 7: Readiness Acceptance Criteria

**Question**: What constitutes production readiness acceptance?

**Proposed minimum**:
- All permission seeds applied (V0015).
- SELL_CARE_PACKAGE catalog entry exists.
- Workflow definition configured via admin UI.
- Migration rehearsal passed on staging.
- Rollback rehearsal passed.
- Live validation passed for core scenarios (12 scenarios).
- PO sign-off.

**Recommendation**: Accept the proposed minimum criteria.

**Required before**: Phase 1B.10-D production migration.

### Decision 8: Release Tag and Push Gates

**Question**: Should release tag and push remain separate authorization gates?

**Options**:
- A) Separate gates: tag after migration, push after tag (recommended).
- B) Combined: tag and push in one authorization.

**Recommendation**: Option A — separate gates allow PO to review at each step.

**Required before**: Phase 1B.10-E.

### Decision 9: Production Migration Executor

**Question**: Who performs production migration execution?

**Options**:
- A) Project Owner executes.
- B) Designated technical operator executes, PO authorizes.
- C) Automated pipeline with PO approval gate.

**Recommendation**: PO must decide based on organizational policy.

**Required before**: Phase 1B.10-D.

### Decision 10: Manual Operational Setup Acceptance

**Question**: Can SELL_CARE_PACKAGE workflow definition/binding configuration be accepted as manual operational setup (not automated by migration)?

**Options**:
- A) Yes — catalog entry seeded by migration, definition/binding configured via admin UI post-deployment (recommended).
- B) No — full configuration must be automated.

**Recommendation**: Option A — workflow definitions are inherently runtime-configured via admin UI in the current architecture. Only the catalog entry needs seeding.

**Required before**: Phase 1B.10-C validation.

### Decision Summary

| # | Decision | Recommended | Blocking Phase |
|---|---|---|---|
| 1 | Permission seed migration scope | Single V0015 | 1B.10-B |
| 2 | SELL_CARE_PACKAGE method | Seed catalog in V0015 | 1B.10-B |
| 3 | Permission catalog update | Include in 1B.10-B | 1B.10-B |
| 4 | PAYMENT_PRINT constant | Defer | Non-blocking |
| 5 | Rehearsal environment | PO/infra confirms | 1B.10-C |
| 6 | Live validation environment | PO/infra confirms | 1B.10-C |
| 7 | Readiness acceptance criteria | Proposed minimum | 1B.10-D |
| 8 | Release/push gates | Separate | 1B.10-E |
| 9 | Migration executor | PO decides | 1B.10-D |
| 10 | Manual operational setup | Accept for workflow def | 1B.10-C |

Decisions 1–3 block Phase 1B.10-B. Decisions 5–6 block Phase 1B.10-C. Decisions 7, 9 block Phase 1B.10-D.

All decisions have clear recommendations. PO can accept recommendations as part of accepting this plan, or respond to individual decisions separately.

## Risks

1. **Environment unavailability** — If staging/live validation environment is not available, rehearsal and live validation cannot proceed. Blocks Phase 1B.10-C onward.

2. **Permission scope misassignment** — If any permission code's data_scope (COMPANY vs GLOBAL) is incorrect, permission gating will malfunction. Mitigated by following established patterns per module.

3. **Workflow definition complexity** — SELL_CARE_PACKAGE catalog entry seeding does not guarantee a functional workflow. Workflow definition, steps, transitions, and bindings must be configured separately via admin UI. This is an operational step that requires domain knowledge.

4. **V0014 rollback inconsistency** — V0014 rollback uses hard-DELETE (disabling trigger), inconsistent with all other rollbacks that use soft-deactivation. This may cause confusion during rollback rehearsal. Not remediated in V0015 — noted for awareness.

5. **Migration order dependency** — V0015 must execute after V0014. If any prior migration fails, V0015 cannot apply. Mitigated by rehearsal.

6. **Branch divergence** — Local branch is ahead of origin. Push requires authorization and may encounter merge conflicts.

## Pass / Fail Criteria for Future Remediation

### Phase 1B.10-B Remediation Implementation

- PASS: V0015 migration applies without error. All 12 permission rows exist. SELL_CARE_PACKAGE catalog entry exists. All automated tests pass (build, unit, integration, API, frontend). Permission catalog updated (if authorized).
- FAIL: Any migration error. Any missing permission row. Test failures. Catalog inconsistency.

### Phase 1B.10-C Migration Rehearsal

- PASS: V0001–V0015 apply without error on staging. All seed rows verified. At least one rollback executes without error. Restore from backup succeeds.
- FAIL: Any migration error on staging. Missing seed rows. Rollback failure. Restore failure.

### Phase 1B.10-C Live Validation

- PASS: All 12 validation scenarios pass (or non-critical scenarios pass with notes).
- FAIL: Any critical validation scenario fails (care package lifecycle, card reprint lifecycle, payment lifecycle).

### Phase 1B.10-D Production Migration

- PASS: V0001–V0015 apply without error on production. All seed rows verified. Post-migration smoke tests pass. PO sign-off.
- FAIL: Any migration error. Missing seed rows. Smoke test failure.

## Non-Goals

- No implementation in this task.
- No source code changes.
- No migration implementation (V0015 not created).
- No permission catalog changes.
- No workflow config implementation.
- No production migration execution.
- No release tag.
- No push.
- No production readiness claim.
- No new business features.

## Recommended Next Gate

Project Owner Phase 1B.10-A remediation plan acceptance.

The PO may accept the plan and respond to the 10 open decisions (accepting recommendations or choosing alternatives) in a single acceptance document.

Required next output after PO acceptance:

docs/architecture/phase-1b10a-project-owner-remediation-plan-acceptance.md

No Phase 1B.10 implementation, migration, production migration, tag, or push may begin until separately authorized after this plan is accepted.
