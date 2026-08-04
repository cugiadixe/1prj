# Phase 1B.9-B Care Package Sales Implementation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER IMPLEMENTATION PLAN ACCEPTANCE

## Authorization Source

- Phase 1B.9-A Project Owner detailed scope acceptance commit:
  4ecf849e8d90b3066c9fa09bc0dab0031ba455b9

## Planning Boundary

- implementation planning only.
- implementation is not authorized.
- migrations are not authorized in this task.
- source changes are not authorized.
- permission catalog changes are not authorized in this task.
- production migration/tag/push are not authorized.

## Accepted Scope Summary

- **Business process name**: Care Package Sales / Gói chăm sóc.
- **Sale unit**: cốt-year.
- **Phase 1B.9 supports**: one-year packages only.
- **Pricing source**: Service Foundation effective-date pricing.
- **Price formula**: unit price per cốt per year × cốt count × 1 year.
- **Request total**: subtotal - approved discount.
- **Snapshots required**: unit price, cốt count, subtotal, discount, total.
- **Sale date**: determines applied effective-date price.
- **Historical records**: are not rewritten by later price changes.
- **Renewal**: creates a new one-year request for the next service period.
- **Duplicate active/paid**: overlapping care packages for the same care target must be prevented.
- **Approval required**: for discount, price override, changed-price renewal, or configured approval-required rule.
- **No approval required**: for configured-price request with no discount.
- **Workflow**: SELL_CARE_PACKAGE workflow is used when approval is required. Workflow Engine remains source of truth.
- **Discount**: is VND amount only, requires reason, cannot reduce total below zero.
- **Payment**: may be created only when payment-eligible. Confirmed payment is required before active status.
- **Payment Foundation constraints apply**: VND only, full payment only, no partial payment, no refund, no cancellation, one bill cannot be paid multiple times.
- **Reconciliation**: manual daily/monthly reconciliation through Payment Foundation.
- **Frontend**: must rely on backend-calculated totals/status.
- **No hard-coded care package price**.

## Proposed Implementation Sequence

1. **Phase 1B.9-B1**: backend/data foundation.
2. **Phase 1B.9-B2**: workflow/payment integration.
3. **Phase 1B.9-C**: frontend implementation.
4. **Phase 1B.9-D**: operational validation.
5. **Phase 1B.9 closure**: closure review / PO closure acceptance.

## Candidate Migration / Rollback Plan

- **Candidate migration**: `V0014__care_package_sales_foundation.sql`
- **Candidate rollback**: `U0014__care_package_sales_foundation.sql`

## Candidate Backend / Data Plan

**Tables:**
- `CarePackageRequests`
- `CarePackageRequestItems`

**Fields (Requests):**
`id`, `company_id`, `customer_id`, `status`, `requires_approval`, `workflow_instance_id`, `service_id` (or `service_code`), `sale_date`, `subtotal_amount`, `discount_amount`, `discount_reason`, `total_amount`, `payment_transaction_id` (aligned with Payment Foundation), `previous_request_id`, `created_by`, `created_at`, `updated_at`, `rowversion`, and repository standard audit fields.

**Fields (Items):**
`id`, `care_package_request_id`, care target reference, `cốt_count_snapshot`, `service_period_start_date`, `service_period_end_date`, `unit_price_snapshot`, `line_subtotal`, `notes`, and audit fields.

**Constraints:**
- Company scope required.
- Customer required.
- One-year period validation.
- Non-negative discount.
- Total cannot be below zero.
- Overlap prevention for active/paid same care target/service period.
- Rowversion.
- No cascade delete that could remove audit/payment evidence.

**Domain / Application:**
- Entities: `CarePackageRequest`, `CarePackageRequestItem`.
- Application service foundation handling list/detail/create endpoints.
- Pricing calculation fetching active price from Service Foundation.

## Candidate API Plan

Base route: `/api/v2/care-packages`

Endpoints:
- `GET /api/v2/care-packages` (list)
- `GET /api/v2/care-packages/{id}` (detail)
- `POST /api/v2/care-packages` (create request/draft)
- `POST /api/v2/care-packages/{id}/submit` (submit to workflow)
- `POST /api/v2/care-packages/{id}/approve` (approve action)
- `POST /api/v2/care-packages/{id}/reject` (reject action)
- `POST /api/v2/care-packages/{id}/create-payment` (create payment draft/bill)
- `GET /api/v2/care-packages/{id}/payment-status` (read-only payment status)
- `POST /api/v2/care-packages/{id}/activate` (activate post-payment)

**Guards & Errors:**
- COMPANY scope strict boundaries. No super-admin bypass.
- 400 invalid input.
- 403 missing permission/company access.
- 404 not found.
- 409 invalid lifecycle transition / overlap / duplicate payment.

## Candidate Workflow / Payment Plan

**Workflow:**
- Process key: `SELL_CARE_PACKAGE`.
- Used only when approval is required.
- Approve/reject facade delegates strictly to `WorkflowRuntimeService`.
- Domain state syncs only after successful workflow transition.
- Rejected requests cannot proceed to payment.

**Payment:**
- Payment eligible when: (no approval required & valid price) OR (approval required & approved).
- Confirmed full payment required before active status.
- Strict mapping to Payment Foundation behavior (no partial, no refund).

## Candidate Permission Plan

Candidate COMPANY-scoped permissions:
- `CARE_PACKAGE_VIEW`
- `CARE_PACKAGE_CREATE`
- `CARE_PACKAGE_APPROVE`
- `CARE_PACKAGE_REJECT`
- `CARE_PACKAGE_CREATE_PAYMENT`
- `CARE_PACKAGE_REPORT_VIEW` (future/reporting candidate)

*(Note: Payment confirmation/correction reuses existing Payment Foundation permissions. Permission catalog changes must be explicitly authorized).*

## Candidate Frontend Plan

- Routes: `/care-packages`, `/care-packages/new`, `/care-packages/:id`
- UI: List page (filters), Create form, Detail page (summary, snapshot, workflow/payment status).
- Behavior: Permission-gated actions. Backend calculates all pricing. No hard-coded price or frontend financial source of truth. Safe handling for 4xx errors.

## Test / Validation Plan

**Backend tests:**
- Domain tests for pricing, discount, status, payment eligibility.
- Unit tests for application service guards.
- Integration tests for migration/rollback and persistence.
- API tests for endpoints, authorization, lifecycle errors.
- Effective-date pricing, missing price, overlap prevention tests.

**Frontend tests:**
- List page, create form, detail page.
- Permission-gated actions and lifecycle visibility.
- Error handling (400/403/404/409).
- Backend-calculated price display verification.

**Operational validation:**
- Happy paths (no approval vs approval).
- Rejection and renewal paths.
- Overlap prevention and payment eligibility guards.
- Company/permission boundaries.
- Boundary condition checks (missing service price, no partial payment).

## Risks / Dependencies

- Dependency on Service Foundation effective-date pricing.
- Dependency on existing Payment Foundation bill/payment behavior.
- Dependency on Workflow Engine `SELL_CARE_PACKAGE` process configuration.
- Dependency on care target / cốt count source in existing model.
- Risk of overlap prevention complexity.
- Risk of backend/frontend price mismatch if frontend calculates locally.
- Risk of permission catalog timing.
- Risk of reports needing fields before dedicated reporting UI exists.
- Migration rollback safety.
- Non-production status.

## Out of Scope / Non-Goals

- Implementation in planning task.
- Migrations in planning task.
- Source changes in planning task.
- Production migration.
- Release tag.
- Push.
- Refunds.
- Cancellation.
- Partial payment.
- Dynamic PDF/template generation.
- Generic Payment Print UI.
- Physical inventory/stamp stock management.
- Unrelated service modules.
- Multi-year packages.
- Partial-year packages.
- Discount percent UI.
- Dedicated report/export UI in first implementation slice unless later accepted.
- Undocumented business rule changes.

## Recommended First Implementation Slice

Recommended first implementation slice:
Phase 1B.9-B1 Care Package Sales backend/data foundation only.

**B1 Scope:**
- Migration/rollback.
- Entities/mappings.
- DbContext integration.
- DTOs.
- Application service foundation.
- List/detail/create APIs.
- Pricing snapshot foundation.
- No hard-coded price.
- Basic permissions/constants as authorized.
- Tests for backend/data/API foundation.

**B1 Exclusions:**
- Frontend.
- Full workflow/payment integration unless explicitly included in later B2.
- Production migration.
- Release tag.
- Push.

## Required Next Gate

Project Owner Phase 1B.9-B implementation plan acceptance.

No implementation may begin until Project Owner implementation plan acceptance is recorded.
