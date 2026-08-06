# Phase 1B.9-D Project Owner Operational Validation Plan Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-D CARE PACKAGE SALES OPERATIONAL VALIDATION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-D Care Package Sales operational validation plan.

This acceptance is based on the accepted B1 backend/data foundation, accepted B2 workflow/payment integration, accepted C frontend implementation, and the proposed operational validation plan.

This acceptance authorizes only:
Phase 1B.9-D Care Package Sales operational validation execution.

This acceptance does not authorize source code changes, database migrations, production migration, release tag, or push.

## Accepted D Plan

Reference:

- Phase 1B.9-D operational validation plan commit:
  68b8b37fc505713399942e0dfc501bdf2a4837dd

- Phase 1B.9-C Project Owner frontend acceptance commit:
  6dfa4b5cd6dc5af526884f70933f0070d50e251a

- Phase 1B.9-C frontend implementation commit:
  aae57bd1dd3479f757e1a8173061bce5616f5190

- Phase 1B.9-B2 workflow/payment implementation commit:
  fd58d92391ece74be9680a8c8aa8504c6c5e2c0a

- Phase 1B.9-B1 backend/data implementation commit:
  c28e7d5b65ac902f80a51c92121352e5ec1fc70c

## Accepted Validation Scope

The Project Owner accepts the following D validation scope:

- Backend validation commands.
- Frontend validation commands.
- Repository validation commands.
- Manual API validation checklist.
- Manual frontend/UI validation checklist.
- Workflow/payment lifecycle validation scenarios.
- Dependency/risk checklist.
- Pass/fail criteria.
- Future D execution report requirement.

## Accepted Backend Validation Plan

Future D execution must run and record:

```bash
dotnet build src/backend/PTKD-ERP.sln
```

```bash
dotnet test tests/backend/PTKD.UnitTests/
```

```bash
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
```

```bash
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
```

## Accepted Frontend Validation Plan

Future D execution must run and record:

```bash
cd src/frontend && npm run lint
```

```bash
cd src/frontend && npm run build
```

```bash
cd src/frontend && npm run test -- --run
```

```bash
cd src/frontend && npx vitest run src/care-packages
```

## Accepted Repository Validation Plan

Future D execution must run and record:

- git status --short --untracked-files=all
- git diff --name-status
- git diff --numstat
- git diff --cached --name-status
- git diff --check
- git tag --points-at HEAD
- git remote -v

## Accepted Manual API/UI Validation Scope

The Project Owner accepts the following manual validation scope:

- Company scope and X-Company-Id enforcement.
- List/detail/create API operations.
- No-approval path (configured-price/no-discount to PaymentEligible).
- Approval-required path (submit/approve through WorkflowRuntimeService).
- Reject path (reject blocks payment).
- Create-payment path (payment eligibility guard, Payment Foundation delegation).
- Duplicate payment guard (409).
- Payment-status read-only endpoint.
- Active-status transition if supported.
- Safe 400/403/404/409 error responses.
- Frontend list/create/detail UI rendering and navigation.
- Permission-gated action buttons.
- Backend-calculated pricing/status display only.
- No hard-coded frontend price.

## Accepted Workflow / Payment Lifecycle Scenarios

The Project Owner accepts the following future validation scenarios:

- Scenario 1: No-approval sale (Draft to PaymentEligible to Paid to Active).
- Scenario 2: Approval-required sale (Draft to PendingApproval to PaymentEligible to Paid).
- Scenario 3: Rejected approval (PendingApproval to Rejected, payment blocked).
- Scenario 4: Duplicate payment guard (second payment creation returns 409).
- Scenario 5: Company isolation (cross-company access blocked).
- Scenario 6: Permission-gated actions (UI and backend enforcement).

## Accepted Dependency / Risk Checklist

The Project Owner accepts that operational validation must explicitly evaluate and classify:

- SQL permission seed alignment for Care Package permissions (CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT).
- SELL_CARE_PACKAGE workflow runtime configuration.
- Runtime permission rows for CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT.
- Manual ID selector UX follow-up.
- Stale frontend status / backend 409 handling.
- Care target selector/search UX limitation.

These dependencies must be classified in the future D validation report as:
- resolved,
- non-blocking follow-up,
- deployment readiness blocker,
- or correction/decision required.

## Authorization for Next Step

Authorized next task:
Phase 1B.9-D Care Package Sales operational validation execution only.

The next task may execute only the accepted operational validation plan and create the operational validation report.

The next task must produce:

docs/architecture/phase-1b9d-care-package-sales-operational-validation-report.md

The next task may:
- Run accepted backend validation commands.
- Run accepted frontend validation commands.
- Run accepted repository validation commands.
- Perform accepted manual API validation.
- Perform accepted manual frontend/UI validation.
- Perform accepted workflow/payment lifecycle scenario validation.
- Classify dependency/risk findings.
- Create the operational validation report.

The next task must not:
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.
- Fix issues found during validation.
- Hide failures.

If validation finds issues, the execution report must record them and mark status accordingly.

## Required D Execution Report

Future D execution must produce:

docs/architecture/phase-1b9d-care-package-sales-operational-validation-report.md

Required report status options:
- PASSED — READY FOR PROJECT OWNER OPERATIONAL VALIDATION ACCEPTANCE
- PASSED WITH DEPLOYMENT READINESS NOTES — READY FOR PROJECT OWNER OPERATIONAL VALIDATION ACCEPTANCE
- FAILED / BLOCKED — CORRECTION OR DECISION REQUIRED

Required report sections:
- Validation target.
- Backend validation evidence.
- Frontend validation evidence.
- Repository validation evidence.
- Manual API validation evidence.
- Manual UI validation evidence.
- Workflow/payment lifecycle evidence.
- Dependency/risk findings.
- Pass/fail status.
- Blockers.
- Recommended next gate.
- Boundary confirmation.

## Non-Goals

This acceptance task does not:
- Execute operational validation.
- Implement code.
- Modify source code.
- Modify tests.
- Modify frontend/backend files.
- Create migrations/rollbacks.
- Modify business docs.
- Modify permission catalog.
- Run production migration.
- Create release tag.
- Push.

## Notes

- Phase 1B.9-D operational validation plan is accepted.
- Phase 1B.9-D operational validation execution has not started in this acceptance task.
- Execution may begin only in the next D execution task and only within accepted D scope.
- Local branch may be ahead of origin; no push is authorized.
- Production migration and release tagging require separate explicit authorization.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
