# Phase 1B.10-A Project Owner Remediation Plan Acceptance — Deployment Readiness

## Status

ACCEPTED — PHASE 1B.10-A DEPLOYMENT READINESS REMEDIATION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.10-A Deployment Readiness Remediation Plan.

This acceptance is based on the remediation plan and its recommended decisions.

This acceptance authorizes only the next implementation task:
Phase 1B.10-B Deployment Readiness Remediation Implementation.

This acceptance does not authorize production migration execution, release tag, push, or production readiness claim.

## Accepted Remediation Plan

Reference:

- Phase 1B.10-A remediation plan commit:
  e97aaebebca8532b4e8cd2a8010b3421b1ec7a4c

- Phase 1B.10 Project Owner scope acceptance commit:
  082827b4ddf6bafd0decb033f8d74ca1a564ccf9

- Phase 1B.10 discovery/scope plan commit:
  632b51328f94b3b60d873b5a7e9e41c61ceb1d9b

## Accepted Readiness Findings

- All 9 core Phase 1B feature slices (1B.1–1B.9) are closed and accepted.
- 14 migrations (V0001–V0014) and 14 rollbacks (U0001–U0014) exist with 1:1 parity.
- No production migration has been executed.
- No live manual API/UI/lifecycle validation has been executed.
- 12 permission codes in PermissionCodes.cs lack database seed rows.
- WORKFLOW_VIEW is already seeded in V0006 (corrected from earlier 13-code count).
- SELL_CARE_PACKAGE is code-only — not seeded in Business_Process_Catalog.
- Runtime permission and workflow alignment are required before deployment readiness.

## Accepted Permission Remediation Scope

The Project Owner accepts the future Phase 1B.10-B implementation scope for:

- Creating a V0015 permission seed alignment migration.
- Creating a matching U0015 rollback.
- Adding all 12 missing permission seed rows identified by the remediation plan:
  - CARE_PACKAGE_APPROVE (COMPANY)
  - CARE_PACKAGE_REJECT (COMPANY)
  - CARE_PACKAGE_CREATE_PAYMENT (COMPANY)
  - CARD_REPRINT_REQUEST_CREATE (COMPANY)
  - CARD_REPRINT_REQUEST_VIEW (COMPANY)
  - CARD_REPRINT_APPROVE (COMPANY)
  - CARD_REPRINT_REQUEST_REJECT (COMPANY)
  - CARD_REPRINT_REQUEST_MARK_PRINTED (COMPANY)
  - WORKFLOW_REJECT (GLOBAL)
  - WORKFLOW_RETRY_EXECUTION (GLOBAL)
  - ORGANIZATION_USER_MANAGE (GLOBAL)
  - CUSTOMER_CHANGE_REQUEST_CREATE (COMPANY)
- Using IF NOT EXISTS guards following existing migration seed patterns (V0003–V0012 style).
- Preserving permission_code immutability (natural key).
- Preserving proper GLOBAL/COMPANY scope as defined in the remediation plan.
- Avoiding duplicate rows.
- Using soft-deactivation rollback (UPDATE SET is_active = 0) where safe.
- Verifying runtime permission rows through tests/queries after migration.

## Accepted Permission Catalog Alignment Scope

The Project Owner accepts permission catalog alignment as part of Phase 1B.10-B.

- The 9 missing permission catalog entries identified by the remediation plan may be added in Phase 1B.10-B:
  - CARE_PACKAGE_VIEW
  - CARE_PACKAGE_CREATE
  - CARE_PACKAGE_APPROVE
  - CARE_PACKAGE_REJECT
  - CARE_PACKAGE_CREATE_PAYMENT
  - CARD_REPRINT_REQUEST_CREATE
  - CARD_REPRINT_REQUEST_VIEW
  - CARD_REPRINT_REQUEST_REJECT
  - CARD_REPRINT_REQUEST_MARK_PRINTED
- No unrelated business docs may be changed.
- No new business requirements may be added.

## Accepted Workflow Remediation Scope

The Project Owner accepts:

- Seeding SELL_CARE_PACKAGE in the future V0015 remediation migration.
- Aligning Business_Process_Catalog for SELL_CARE_PACKAGE.
- Keeping workflow definition/binding as admin UI operational setup unless separately authorized.
- Adding runtime verification for SELL_CARE_PACKAGE readiness.
- Not creating production workflow configuration in this acceptance task.

## Accepted Migration Rehearsal and Rollback Rehearsal Plan

The Project Owner accepts the future rehearsal approach:

- Staging/pre-prod environment required.
- Backup/restore verification before rehearsal.
- V0001–V0015 forward rehearsal.
- Selective rollback rehearsal.
- Smoke checks on seed rows and schema.
- Sign-off and go/no-go criteria.

Rehearsal execution is not authorized in this acceptance task. Rehearsal is deferred to Phase 1B.10-C.

## Accepted Live Validation Readiness Plan

The Project Owner accepts the future live validation approach:

- Running API and frontend environment required.
- Authenticated test users with different permission sets.
- Company context and X-Company-Id setup.
- Seed data for customers, graves/care targets, services, prices.
- Runtime permission rows from V0015.
- SELL_CARE_PACKAGE workflow runtime configuration.
- Payment Foundation setup.
- 12 validation scenarios covering Care Package, Card Reprint, Payment, Customer, Workflow, Security.
- Pass/fail criteria as defined in the remediation plan.

Live validation execution is not authorized in this acceptance task. Validation is deferred to Phase 1B.10-C.

## Accepted Project Owner Decisions

The Project Owner accepts the remediation plan's recommended decisions:

1. **Permission seed migration scope**: Accepted — single V0015 for all 12 codes.
2. **SELL_CARE_PACKAGE configuration method**: Accepted — seed catalog entry in V0015; workflow definition/binding remains admin UI operational setup.
3. **Permission catalog update**: Accepted — include 9 missing entries in Phase 1B.10-B.
4. **PAYMENT_PRINT constant**: Accepted — defer until Payment Print UI is built.
5. **Rehearsal environment**: Deferred to Phase 1B.10-C — PO/infra will confirm availability.
6. **Live validation environment**: Deferred to Phase 1B.10-C — PO/infra will confirm availability.
7. **Readiness acceptance criteria**: Accepted — proposed minimum criteria as defined in the remediation plan.
8. **Release tag and push gates**: Accepted — separate authorization gates.
9. **Production migration executor**: Deferred to Phase 1B.10-D — PO will decide.
10. **Manual operational setup**: Accepted — SELL_CARE_PACKAGE workflow definition/binding accepted as admin UI operational setup.

Decisions 1–3 are resolved and unblock Phase 1B.10-B.
Decisions 5–6 are deferred and block Phase 1B.10-C.
Decisions 7, 9 are deferred and block Phase 1B.10-D.

## Authorization for Next Step

Authorized next task:
Phase 1B.10-B Deployment Readiness Remediation Implementation only.

The next task may implement only the accepted remediation scope.

The next task may:
- Create the accepted V0015 permission seed alignment migration.
- Create the matching U0015 rollback.
- Add accepted missing permission seed rows (12 codes).
- Seed SELL_CARE_PACKAGE catalog entry as accepted.
- Update docs/business/permission-catalog.md only for the accepted 9 missing permission entries.
- Add or update tests needed to verify permission seed and workflow catalog alignment.
- Create the Phase 1B.10-B implementation report.

The next task must produce:

docs/architecture/phase-1b10b-deployment-readiness-remediation-implementation-report.md

The next task must not:
- Execute production migration.
- Run staging/pre-prod rehearsal.
- Execute live validation.
- Create release tag.
- Push.
- Claim production readiness.
- Implement unrelated code changes.
- Modify unrelated business docs.
- Add new business requirements.
- Change existing permission_code meanings.
- Alter Phase 1B business behavior outside the accepted readiness remediation scope.

## Required Next Output

The next task must produce:

docs/architecture/phase-1b10b-deployment-readiness-remediation-implementation-report.md

## Boundaries / Non-Goals

This acceptance task does not:
- Implement code.
- Create migrations.
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Modify docs/business/permission-catalog.md.
- Run migrations.
- Run production migration.
- Execute validation.
- Create release tag.
- Push.
- Claim production readiness.

## Recommended Next Gate

Phase 1B.10-B Deployment Readiness Remediation Implementation.
