# Phase 1B.9-C Project Owner Frontend Plan Acceptance — Care Package Sales

## Status

ACCEPTED — PHASE 1B.9-C CARE PACKAGE SALES FRONTEND PLAN ACCEPTED

## Project Owner Decision

The Project Owner accepts the Phase 1B.9-C Care Package Sales frontend implementation plan.

This acceptance is based on the accepted B1 backend/data foundation, accepted B2 workflow/payment integration, and the proposed frontend implementation plan.

This acceptance authorizes only:
Phase 1B.9-C Care Package Sales frontend implementation.

This acceptance does not authorize backend changes, database migrations, production migration, release tag, or push.

## Accepted Frontend Plan

Reference:

- Phase 1B.9-C frontend implementation plan commit:
  7974b6952965e2c823a6908a2653245572838a4f

- Phase 1B.9-B2 Project Owner workflow/payment acceptance commit:
  87b783b1f2b64c73fe67aff57016324c543c1003

- Phase 1B.9-B2 workflow/payment implementation commit:
  fd58d92391ece74be9680a8c8aa8504c6c5e2c0a

- Phase 1B.9-B1 backend/data implementation commit:
  c28e7d5b65ac902f80a51c92121352e5ec1fc70c

## Accepted Backend / API Baseline

The Project Owner confirms the accepted backend/API baseline:

- `GET /api/v2/care-packages` — list (paged, company-scoped via X-Company-Id).
- `GET /api/v2/care-packages/{id}` — detail with items.
- `POST /api/v2/care-packages` — create request/draft.
- `POST /api/v2/care-packages/{id}/submit` — submit for approval.
- `POST /api/v2/care-packages/{id}/approve` — approve workflow step.
- `POST /api/v2/care-packages/{id}/reject` — reject workflow step.
- `POST /api/v2/care-packages/{id}/create-payment` — create payment draft.
- `GET /api/v2/care-packages/{id}/payment-status` — read-only payment status.
- `POST /api/v2/care-packages/{id}/activate` — activate after confirmed payment.
- X-Company-Id company scope required on all endpoints.
- Backend authorization remains authoritative.
- Backend-calculated pricing and status.
- Safe 400/403/404/409 error behavior.

## Accepted Frontend Scope

The Project Owner accepts the following frontend scope:

- Route `/care-packages`.
- Route `/care-packages/new`.
- Route `/care-packages/:id`.
- `care-packages/` frontend module following existing `cards/` pattern.
- Types (TypeScript interfaces mirroring backend DTOs).
- API client (9 functions via axiosClient).
- Hooks (9 React Query hooks via @tanstack/react-query).
- Error message helpers (following existing errorMessages.ts pattern).
- List page with table, filters, pagination, permission-gated create button.
- Create page/form with customer, care target, service period, discount, backend-calculated response.
- Detail page with summary, line items, pricing snapshot, workflow/payment status, lifecycle actions.
- Page tests (14 test cases covering rendering, permissions, lifecycle, errors).
- Permission-gated UI actions.
- Lifecycle action display.
- Payment-status display (read-only).
- Backend-calculated totals/status display only.
- Frontend validation/tests.

## Accepted Lifecycle UI Behavior

The Project Owner confirms:

- Submit visible only when draft status AND requiresApproval is true AND user has CARE_PACKAGE_CREATE.
- Approve visible only when PendingApproval AND user has CARE_PACKAGE_APPROVE.
- Reject visible only when PendingApproval AND user has CARE_PACKAGE_REJECT.
- Create Payment visible only when PaymentEligible AND user has CARE_PACKAGE_CREATE_PAYMENT.
- Activate visible only after confirmed payment AND user has CARE_PACKAGE_CREATE.
- Payment Status is read-only display.
- Frontend gates are UX-only.
- Backend remains authoritative for all lifecycle transitions and authorization.

## Accepted Permission-Gated UI

The Project Owner confirms planned permission gates:

| Permission | UI Effect |
|-----------|-----------|
| CARE_PACKAGE_VIEW | List and detail page access |
| CARE_PACKAGE_CREATE | Create button, create page, submit button |
| CARE_PACKAGE_APPROVE | Approve button when pending approval |
| CARE_PACKAGE_REJECT | Reject button when pending approval |
| CARE_PACKAGE_CREATE_PAYMENT | Create Payment button when payment-eligible |

CARE_PACKAGE_REPORT_VIEW remains a future/reporting candidate and is not accepted for this frontend slice.

Payment confirmation/correction remains outside this frontend slice unless existing Payment UI is linked without new backend scope.

## Accepted Error Handling / Display Rules

The Project Owner confirms:

- Safe handling of 400 validation errors (display backend detail/title).
- Safe handling of 403 missing permission/company access.
- Safe handling of 404 not found.
- Safe handling of 409 invalid lifecycle transition / duplicate payment / payment not eligible.
- Safe handling of missing/inactive service price or workflow configuration failure (display backend message).
- No raw backend internals exposed.
- No frontend hard-coded price.
- No frontend financial source-of-truth calculation.

## Accepted Test / Validation Plan

Future frontend implementation must run and record:

Frontend:
- `cd src/frontend && npm run lint`
- `cd src/frontend && npm run build`
- `cd src/frontend && npm run test -- --run`
- `cd src/frontend && npx vitest run src/care-packages`

Repository:
- `git diff --check`

Backend validation is not required for Phase 1B.9-C frontend implementation unless backend files are changed, which should not happen.

## Accepted Risks / Dependencies

The Project Owner accepts and carries forward:

- SQL permission seed alignment remains deferred before deployment/operational validation.
- SELL_CARE_PACKAGE workflow runtime configuration remains deferred before deployment/operational validation.
- Frontend depends on accepted backend `/api/v2/care-packages` endpoints.
- Frontend depends on company context / X-Company-Id behavior.
- Frontend depends on existing permission context patterns.
- Frontend depends on backend-calculated price/status.
- Stale status/action visibility may produce backend 409 and must be handled safely.
- Care target selector UX may be limited by available backend DTO/display metadata.

## Authorization for Next Step

Authorized next task:
Phase 1B.9-C Care Package Sales frontend implementation only.

The next task may implement only the frontend slice.

Authorized C implementation scope:
- Add frontend routes: `/care-packages`, `/care-packages/new`, `/care-packages/:id`.
- Create care-packages frontend module.
- Create frontend types.
- Create API client.
- Create hooks.
- Create error message helpers.
- Create list page.
- Create create page/form.
- Create detail page.
- Create page/component tests.
- Implement permission-gated UI.
- Implement lifecycle action visibility.
- Display backend-calculated pricing/status.
- Display read-only payment status.
- Handle 400/403/404/409 safely.
- Run frontend validation.
- Create frontend implementation report.

C must not implement:
- Backend changes.
- Database migrations.
- Business docs changes.
- Permission catalog changes.
- Production migration.
- Release tag.
- Push.
- Dynamic PDF/template generation.
- Generic Payment Print UI.
- Dedicated report/export UI.
- Refund.
- Cancellation.
- Partial payment.
- Physical inventory/stamp stock management.
- Multi-year packages.
- Partial-year packages.
- Discount percent UI.
- Frontend hard-coded care package price.
- Frontend financial source-of-truth calculation.

## Required C Implementation Report

The next task must produce:

docs/architecture/phase-1b9c-care-package-sales-frontend-implementation-report.md

The report must include:
- Implemented frontend files.
- Routes added.
- Pages/components summary.
- API client/hooks/types summary.
- Permission-gated UI summary.
- Lifecycle action summary.
- Pricing/status display summary.
- Error handling summary.
- Tests added/updated.
- Validation evidence.
- Boundary confirmation.
- Known risks/follow-ups.
- Explicit statement that backend changes, migrations, production migration, tag, and push were not performed.

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

- Phase 1B.9-C frontend plan is accepted.
- Phase 1B.9-C frontend implementation has not started in this acceptance task.
- Implementation may begin only in the next C task and only within accepted C scope.
- Local branch may be ahead of origin; no push is authorized.
- Production migration and release tagging require separate explicit authorization.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
