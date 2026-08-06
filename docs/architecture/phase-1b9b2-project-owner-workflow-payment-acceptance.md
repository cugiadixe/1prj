# Phase 1B.9-B2 Project Owner Workflow/Payment Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-B2 CARE PACKAGE SALES WORKFLOW/PAYMENT ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-B2 Care Package Sales workflow/payment integration.

This acceptance is based on the B2 implementation report and the workflow/payment acceptance review.

The acceptance review passed with non-blocking notes and found no blocking issues.

This acceptance authorizes only the next planning task:
Phase 1B.9-C Care Package Sales frontend implementation planning.

This acceptance does not authorize frontend implementation, production migration, release tag, or push.

## Accepted B2 Implementation

Reference:

- Phase 1B.9-B2 workflow/payment acceptance review commit:
  daa38c455522a504a4d9af9c7f24bd12d88b86a9

- Phase 1B.9-B2 workflow/payment implementation commit:
  fd58d92391ece74be9680a8c8aa8504c6c5e2c0a

- Phase 1B.9-B1 Project Owner backend/data acceptance commit:
  3103c4064c190a94531d5ced5ddc23b95acd7708

## Accepted Workflow/Payment Scope

The Project Owner accepts the following B2 workflow/payment scope:

- SELL_CARE_PACKAGE workflow integration.
- Approval-required path: submit initiates workflow, sets PendingApproval, approve/reject facades delegate to WorkflowRuntimeService.
- No-approval path: requests configured without approval skip directly to PaymentEligible.
- Domain state synchronization exclusively via CarePackageExecutionHandler upon successful workflow action completion. WorkflowRuntimeService is the source of truth for step tracking.
- Rejected requests are gracefully blocked from advancing to payment.
- Payment eligibility is verified before payment draft creation.
- Create-payment transitions the request and delegates to IPaymentTransactionService.
- Duplicate payment creation is blocked when a pending/paid transaction exists.
- Payment-status endpoint is read-only.
- Active-status transitions the request to Active when payment is confirmed.
- Payment Foundation constraints are preserved: VND only, full payment only, no partial payment, no refund, no cancellation.
- B2 permission constants added: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT.
- No hard-coded package price; Service Foundation effective-date pricing is the sole financial source.
- B2 backend/domain/API tests covering state engine, authorization scopes, status bounds, and lifecycle transitions.
- B2 implementation report.

## Acceptance Review Summary

The B2 acceptance review (commit daa38c4) passed with non-blocking notes:

- No blockers were found.
- Committed files are within B2 authorization (9 files: 1 report, 6 backend source, 2 test).
- No frontend files were changed.
- No business docs were changed.
- No permission catalog changes were made.
- No production migration/tag/push occurred.
- Validation passed:
  - Build: 0 errors, 9 warnings.
  - UnitTests: 236 passed.
  - IntegrationTests: 203 passed.
  - ApiTests: 308 passed.
  - git diff --check: clean.

## Non-Blocking Notes Accepted

The Project Owner accepts the non-blocking note that B2 added workflow/payment permission constants (CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT) without adding SQL permission seed rows in this slice.

The Project Owner accepts the non-blocking note that database permission seeds and workflow runtime configuration for SELL_CARE_PACKAGE must be addressed before deployment / operational validation.

This acceptance does not modify docs/business/permission-catalog.md.

Any future permission seed, workflow runtime configuration, or deployment readiness work must be handled only under a separately authorized task or later accepted operational validation slice.

## Authorization for Next Step

Authorized next task:
Phase 1B.9-C Care Package Sales frontend implementation planning only.

The next task may create only a frontend implementation plan document.

The next task must produce:

docs/architecture/phase-1b9c-care-package-sales-frontend-implementation-plan.md

The next task must:
- Translate accepted backend/data and workflow/payment scope into frontend planning.
- Define frontend routes.
- Define pages and components.
- Define API client/hooks.
- Define permission-gated UI.
- Define lifecycle action visibility.
- Define backend-calculated price/status display.
- Define error handling.
- Define frontend validation/test strategy.
- Define frontend non-goals.
- Recommend whether frontend implementation may proceed after PO frontend plan acceptance.

Do not authorize:
- Frontend implementation.
- Source code changes.
- Backend implementation.
- Database migrations.
- Business docs changes.
- Permission catalog changes.
- Production migration.
- Release tag.
- Push.
- Dynamic PDF/template generation.
- Generic Payment Print UI.
- Refund.
- Cancellation.
- Partial payment.
- Physical inventory/stamp stock management.

## Required Frontend Plan Output

Future Phase 1B.9-C frontend planning task must produce:

docs/architecture/phase-1b9c-care-package-sales-frontend-implementation-plan.md

It must include:
- Accepted backend/API scope summary.
- Accepted workflow/payment scope summary.
- Proposed frontend routes.
- Proposed frontend page/component structure.
- Proposed API client/hooks/types.
- Permission-gated UI behavior.
- Lifecycle action behavior.
- Backend-calculated pricing/status display.
- Error handling strategy.
- Frontend validation/test plan.
- Risks/dependencies.
- Explicit statement that frontend implementation remains unauthorized until Project Owner frontend plan acceptance.

## Non-Goals

This acceptance task does not:
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

- Phase 1B.9-B2 workflow/payment integration is accepted.
- Phase 1B.9-C frontend implementation has not started.
- Frontend implementation may begin only after frontend implementation plan and Project Owner frontend plan acceptance.
- Local branch may be ahead of origin; no push is authorized.
- Production migration and release tagging require separate explicit authorization.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
