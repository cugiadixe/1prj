# Phase 1B.7-D Operational Validation and Closure Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER OPERATIONAL VALIDATION PLAN ACCEPTANCE BEFORE EXECUTION

## Authorization Source

- Phase 1B.7-C PO frontend implementation acceptance commit:
  4bdb6f81e64906f2cf29e083c51379e5db448378

State:
- Phase 1B.7-B backend/data implementation is accepted.
- Phase 1B.7-C frontend implementation is accepted.
- This document is operational validation and closure planning only.
- This document does not authorize operational validation execution.

## Objective

Define the validation plan, evidence expectations, operational checklist, risk checks, and closure readiness criteria for Phase 1B.7 Payment / Billing / Collection / Reconciliation Foundation.

## Source Documents Reviewed

- docs/architecture/phase-1b7c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b7c-frontend-implementation-acceptance-review.md
- docs/architecture/phase-1b7c-frontend-implementation-report.md
- docs/architecture/phase-1b7c-project-owner-frontend-scope-acceptance.md
- docs/architecture/phase-1b7c-frontend-scope-and-implementation-plan.md
- docs/architecture/phase-1b7b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b7b-backend-data-foundation-updated-implementation-acceptance-review.md
- docs/architecture/phase-1b7b-backend-data-foundation-remediation-report.md
- docs/architecture/phase-1b7b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b7b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b7b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b7-project-owner-scope-acceptance.md
- docs/architecture/phase-1b7-payment-foundation-discovery-and-detailed-plan.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- PTKD-ERP-Master-Context.md

## Accepted Implementation Baseline

- V0012/U0012.
- Payment backend/domain/API.
- Reconciliation backend/API.
- payment frontend.
- reconciliation frontend.
- remediation of ReconciliationController Prepare/Confirm authorization order.
- validation evidence from backend and frontend implementation phases.

## Validation Scope

### In Scope

- backend build/tests.
- frontend lint/typecheck/tests.
- targeted payment frontend tests.
- migration/rollback evidence review.
- operational checklist.
- permission/security validation.
- business rule acceptance validation.
- boundary validation.
- closure readiness assessment.

### Out of Scope

- source code changes.
- backend changes.
- frontend changes.
- migrations.
- rollbacks.
- business docs changes.
- Card Reprint.
- Care Package Sales.
- production migration.
- release tag.
- push.

## Backend Validation Plan

Execute the following backend tests to confirm baseline integrity:
- \dotnet build src/backend/PTKD-ERP.sln\
- \dotnet test tests/backend/PTKD.UnitTests/\
- \dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false\
- \dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false\

Expected evidence:
- All commands execute with 0 failures and 0 warnings.
- Migration V0012 applies to PTKD_TEST_PHASE1A2 successfully.
- U0012 rollback behavior is covered by tests.
- Database can reset to target V0012.
- No production database is touched.

## Frontend Validation Plan

Execute the following frontend tests to confirm baseline integrity:
- \
pm run lint\ (in src/frontend)
- \
px tsc -b\ (in src/frontend)
- \
pm run test\ (in src/frontend)
- Targeted Payment frontend tests (e.g. \
pm run test -- payments reconciliation\)

Expected evidence:
- 0 linting errors.
- 0 TypeScript compilation errors.
- Full frontend test suite evidence, not targeted-only evidence.
- No raw SQL/internal/stack trace display in UI components.
- All permission-gated UI elements correctly render or hide based on state/permissions.

## Operational Checklist

| Area | Check | Expected Result | Evidence Type | Required? |
|---|---|---|---|---|
| Payments | view payment list | List renders correctly with safe data | Screenshot/Log | Yes |
| Payments | create draft payment | Draft is created, requires PAYMENT_CREATE_DRAFT | Screenshot/Log | Yes |
| Payments | view payment detail | Detail renders without raw SQL trace | Screenshot/Log | Yes |
| Payments | draft soft-delete | Draft deletes cleanly without "cancel" wording | Screenshot/Log | Yes |
| Payments | confirm payment | Moves to confirmed, requires PAYMENT_CONFIRM | Screenshot/Log | Yes |
| Payments | Admin correction | Allows correction with mandatory reason | Screenshot/Log | Yes |
| Reconciliation | daily reconciliation report | Renders safely | Screenshot/Log | Yes |
| Reconciliation | monthly reconciliation report | Renders safely | Screenshot/Log | Yes |
| Reconciliation | prepare reconciliation | Prepares safely, requires RECONCILIATION_PREPARE | Screenshot/Log | Yes |
| Reconciliation | confirm reconciliation | Confirms safely, requires RECONCILIATION_CONFIRM | Screenshot/Log | Yes |
| Security | permission-denied behavior | 403 renders safe message without raw data | Screenshot/Log | Yes |
| Security | direct URL behavior | Renders safe message on direct unauthorized URL | Screenshot/Log | Yes |
| UX | concurrency behavior | 409 handled with safe refresh message | Screenshot/Log | Yes |
| Boundary | refund/cancellation/partial payment UI | Does not exist | Visual | Yes |
| Boundary | Card Reprint/Care Package UI | Does not exist | Visual | Yes |
| Boundary | automated bank integration UI | Does not exist | Visual | Yes |

## Business Rule / Acceptance Criteria Mapping

| Rule / Criteria | Validation Method | Expected Evidence |
|---|---|---|
| PAY-001 through PAY-012 | Automated Tests & Operational Checklist | Test Pass & Checklist Complete |
| PAY-01 through PAY-08 | Automated Tests & Operational Checklist | Test Pass & Checklist Complete |
| DRAFT to CONFIRMED one-way lifecycle | Operational Checklist / Unit Tests | Checklist Complete |
| one-time full payment | Operational Checklist / Unit Tests | Checklist Complete |
| no partial payment | Boundary verification | Verified out of scope |
| no refund | Boundary verification | Verified out of scope |
| no cancellation | Boundary verification | Verified out of scope |
| one payment may cover multiple services | Operational Checklist / Unit Tests | Checklist Complete |
| one bill/payment cannot be confirmed twice | Concurrency Check / API Tests | Test Pass & Checklist Complete |
| VND-only amount display | Operational Checklist | Checklist Complete |
| Admin-only confirmed-payment correction | Operational Checklist / Unit Tests | Test Pass & Checklist Complete |
| mandatory correction reason | Operational Checklist | Checklist Complete |
| append-only correction history | API Tests / Integration Tests | Test Pass |
| manual reconciliation support | Operational Checklist | Checklist Complete |
| reconciliation period marking after correction | Unit Tests / Integration Tests | Test Pass |
| service-linked payment consistency | Integration Tests | Test Pass |
| sanitized errors | Operational Checklist | Checklist Complete |

## Permission and Security Validation

| Permission / Control | Validation Method | Expected Result |
|---|---|---|
| PAYMENT_CREATE_DRAFT | Frontend / API Tests | Access granted/denied appropriately |
| PAYMENT_CONFIRM | Frontend / API Tests | Access granted/denied appropriately |
| PAYMENT_CORRECT_CONFIRMED | Frontend / API Tests | Access granted/denied appropriately |
| RECONCILIATION_PREPARE | Frontend / API Tests | Access granted/denied appropriately |
| RECONCILIATION_CONFIRM | Frontend / API Tests | Access granted/denied appropriately |
| PAYMENT_PRINT | Boundary verification | Deferred/no UI |
| backend authorization authoritative | API Tests | Unauthenticated/unauthorized API calls fail |
| frontend gating convenience only | Operational Checklist | Elements hide/disable on frontend |
| Prepare/Confirm authorization bypass | API Tests | Remediated, bypass impossible |
| no mutation before authorization | API Tests | Verified in backend |
| no raw internal error exposure | Frontend / API Tests | Verified no stack traces leaked |

## Data / Migration Validation

- V0012 present.
- U0012 present.
- SchemaVersions handling correct.
- reset target V0012 works correctly.
- Applied to PTKD_TEST_PHASE1A2 only.
- no production migration.

## Error Handling Validation

- 400 validation: Handled with safe message.
- 403 forbidden: Handled with safe message.
- 404 not found: Handled with safe message.
- 409 concurrency: Handled with refresh/reload guidance.
- 500 generic server error: Handled with safe generic message.
- no raw SQL/internal exception shown.
- no stack trace shown.
- no sensitive payload display.

## Closure Readiness Criteria

- all required automated validations pass.
- operational checklist complete or justified.
- no blocking defects.
- no unauthorized files changed.
- no uncommitted tracked changes.
- no staged files.
- no production migration/tag/push.
- risks/deferred items documented.
- closure acceptance review can be created only after validation execution.

## Risks / Deferred Items

- operational browser validation may be partially manual/headless depending environment.
- PaymentCreatePage test non-blocking follow-up.
- ReconciliationMonthlyPage test non-blocking follow-up.
- Card Reprint deferred.
- Care Package Sales deferred.
- production release deferred.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.

## Recommended Next Gate

Recommended next authorized task:
Project Owner operational validation plan acceptance for Phase 1B.7-D.

After Project Owner operational validation plan acceptance, authorize Phase 1B.7-D operational validation execution only.

Do not authorize:
- source code changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- Card Reprint implementation,
- Care Package Sales implementation,
- production migration,
- release tag,
- push.

## Non-Goals

Confirm this document does not:
- execute validation.
- modify source code.
- modify tests.
- modify frontend/backend files.
- modify migrations/rollbacks.
- modify business docs.
- implement Card Reprint.
- implement Care Package Sales.
- run production migration.
- create release tag.
- push.
