# Phase 1B.8-D Project Owner Operational Validation Plan Acceptance

## Status

ACCEPTED — PHASE 1B.8-D CARD REPRINT OPERATIONAL VALIDATION PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.8-D Card Reprint operational validation plan.

This acceptance is based on the proposed operational validation plan.

This acceptance authorizes only the next execution task:
Phase 1B.8-D Card Reprint operational validation execution.

This acceptance does not authorize source code changes, new backend implementation, new frontend implementation, database migrations, production migration, release tag, or push.

## Accepted Plan

- Phase 1B.8-D operational validation plan commit:
  2c61d0fdbd022fb0396a3201cc58a4bb4ad65e1b

- Phase 1B.8-C Project Owner frontend acceptance commit:
  692553f7465b60ad8ed36bca859a1fd6a86ff1aa

- Phase 1B.8-B2 Project Owner workflow/payment acceptance commit:
  edda862664724dd4c65373a6280bfa1e8881e1e0

- Phase 1B.8-B1 Project Owner backend/data acceptance commit:
  16819c724efeaaf832f7332c93a0d87f22701cf8

## Accepted Validation Scope

- backend build validation.
- backend unit test validation.
- backend integration test validation.
- backend API test validation.
- frontend lint validation.
- frontend build validation.
- frontend full Vitest validation.
- targeted Card Reprint frontend test validation.
- repository whitespace validation.
- Card Reprint happy path validation.
- Card Reprint rejection path validation.
- lifecycle guard validation.
- workflow approval/rejection validation.
- payment draft/status validation.
- print/release guard validation.
- permission validation.
- company-scope validation.
- boundary validation.

## Accepted Scenario Matrix

Happy path:
- create Card Reprint request.
- submit request.
- approve through Workflow Engine.
- create payment draft/bill after approval.
- confirm payment using existing Payment Foundation behavior where supported.
- view payment status.
- mark printed after confirmed payment.
- mark released after printed.
- verify frontend status/action behavior.

Rejection path:
- create request.
- submit request.
- reject through Workflow Engine.
- verify downstream payment/print/release are blocked.

Guard paths:
- payment before approval blocked.
- print before confirmed payment blocked.
- release before printed blocked.
- duplicate payment draft blocked or safely handled.
- cross-company access blocked.
- missing permission blocked.
- missing/inactive CARD_REPRINT service/price config fails safely.
- invalid ID returns safe error.
- invalid lifecycle transition returns safe error.

Boundary paths:
- no refund.
- no cancellation.
- no partial payment.
- no dynamic PDF/template generation.
- no generic Payment Print UI.
- no physical inventory/stamp stock management.
- no Care Package Sales.

## Accepted Automated Validation Commands

Backend:

dotnet build src/backend/PTKD-ERP.sln

dotnet test tests/backend/PTKD.UnitTests/

dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false

dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false

Frontend, from src/frontend:

npm run lint

npm run build

npm run test -- --run

npx vitest run src/cards

Repository:

git diff --check

## Accepted Evidence Requirements

- git baseline.
- automated validation command outputs.
- backend test counts.
- frontend test file/test counts.
- scenario matrix results.
- workflow approval/rejection evidence.
- payment draft/status evidence.
- permission/company-scope evidence.
- lifecycle guard evidence.
- frontend lifecycle/action evidence.
- boundary confirmation.
- known issues and risk classification.
- final pass/fail decision.

## Accepted Pass / Fail Criteria

Pass criteria:
- all automated validation commands pass.
- happy path completes.
- rejection path blocks downstream actions.
- guard paths fail safely.
- permission/company-scope checks pass.
- frontend reflects backend lifecycle correctly.
- no unauthorized behavior appears.
- no tracked files are modified by validation execution except the operational validation report.

Fail criteria:
- any automated validation command fails.
- payment can be created before approval.
- print/release can happen before required guard.
- rejected request can proceed.
- cross-company access succeeds incorrectly.
- hard-coded fee appears in frontend/application.
- refund/cancellation/partial payment behavior appears.
- operational validation requires unauthorized code changes.
- evidence is incomplete.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-D Card Reprint operational validation execution only.

The next task may execute the accepted validation plan and create only the operational validation report.

The next task must produce:

docs/architecture/phase-1b8d-card-reprint-operational-validation-report.md

The report must include:
- git baseline.
- validation environment.
- automated validation evidence.
- scenario matrix results.
- manual validation evidence if executed.
- permission/company-scope evidence.
- workflow/payment evidence.
- boundary confirmation.
- risks/follow-ups.
- final validation decision.

Do not authorize:
- source code changes,
- frontend implementation,
- backend implementation,
- database migrations,
- business docs changes,
- permission catalog changes,
- Care Package Sales,
- production migration,
- release tag,
- push,
- dynamic PDF/template generation,
- generic Payment Print UI,
- refund,
- cancellation,
- partial payment,
- physical inventory/stamp stock management.

## Non-Goals

- run operational validation.
- implement code.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- run production migration.
- create release tag.
- push.

## Notes

- operational validation plan is accepted.
- Phase 1B.8 remains not closed.
- operational validation execution may proceed only within accepted 1B.8-D scope.
- closure review remains a later gate after operational validation execution.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
