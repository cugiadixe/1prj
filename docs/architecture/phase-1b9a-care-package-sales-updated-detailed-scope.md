# Phase 1B.9-A Care Package Sales Updated Detailed Scope

## Status

READY FOR PROJECT OWNER DETAILED SCOPE ACCEPTANCE

## Authorization Source

- Phase 1B.9-A Project Owner blocker decision response commit:
  9d8d168fd6e8a33c30c97eb7b8656361bbd0ec4c

- Phase 1B.9-A blocked detailed scope commit:
  6ac4e7296d53c753be499250674b5a28e909cb3e

## Planning Boundary

- this is updated detailed scope only.
- implementation is not authorized.
- migrations are not authorized.
- permission catalog changes are not authorized.
- production migration/tag/push are not authorized.

## Source Context Reviewed

- `docs/architecture/phase-1b9a-project-owner-blocker-decision-response.md`
- `docs/architecture/phase-1b9a-care-package-sales-open-decisions-and-detailed-scope.md`
- `docs/architecture/phase-1b9-project-owner-scope-acceptance.md`
- `docs/architecture/phase-1b9-care-package-sales-discovery-and-scope-plan.md`
- `docs/architecture/post-phase-1b8-project-owner-next-work-decision.md`
- `docs/architecture/phase-1b8-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b8-card-reprint-closure-review.md`
- `docs/architecture/phase-1b7-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b6-project-owner-closure-acceptance.md`
- `docs/business/business-rules.md`
- `docs/business/process-catalog.md`
- `docs/business/acceptance-criteria.md`
- `docs/business/permission-catalog.md`

*(Unavailable documents: `PTKD-ERP-Master-Context.md` and `docs/architecture/project-readiness-review.md` were unsupported/unverified)*

## Updated Open Decision Matrix

| ID | Topic | Status | Project Owner Decision | Implementation Impact |
| :--- | :--- | :--- | :--- | :--- |
| OD-1B9-001 | Care Package terminology | CONFIRMED | Use "Care Package Sales" / "Gói chăm sóc" for process. Use "Care Package Request" and "Care Package Request Item". | Nomenclature locked. |
| OD-1B9-002 | Sale unit | CONFIRMED | Sale unit is cốt-year. Attach to grave/card/care target with a confirmed cốt count. Customer/Company required at request level. | Relational mapping locked. |
| OD-1B9-003 | Package duration | CONFIRMED | One-year (12 months) packages only in Phase 1B.9. Period start/end dates required. | Validation/calculation rules locked. |
| OD-1B9-004 | Pricing source | CONFIRMED | Must use Service Foundation effective-date pricing (e.g., CARE_PACKAGE service code). Missing/inactive price fails safely. | Service reference locked. |
| OD-1B9-005 | Price calculation | CONFIRMED | line subtotal = unit price per cốt per year × cốt count × 1 year. request total = sum of line subtotals - approved discount. | Calculation formula locked. |
| OD-1B9-006 | Price changes | CONFIRMED | Sale date determines applied price. Snapshots preserved. Historical sales unaffected by later changes. | Snapshot storage required. |
| OD-1B9-007 | Renewal rule | CONFIRMED | Creates new 1-year request. Prevents duplicates/overlaps for same care target. | Status overlap logic required. |
| OD-1B9-008 | Approval trigger | CONFIRMED | Required for discount, price override, changed-price renewal, or configured rule. Not required for configured-price request with no discount. | Workflow handler execution rules locked. |
| OD-1B9-009 | Discount behavior | CONFIRMED | Allowed only with approval. Stored in VND. Reason required. Cannot reduce total below zero. | Amount boundaries and fields locked. |
| OD-1B9-010 | Payment timing | CONFIRMED | Payment draft/bill created only when payment-eligible (no approval needed or approved). | Integration timing locked. |
| OD-1B9-011 | Payment constraints | CONFIRMED | Payment Foundation constraints: VND only, full payment, no refund/partial/cancellation. | Validation boundaries locked. |
| OD-1B9-012 | Reconciliation/reporting | CONFIRMED | Manual reconciliation via Payment Foundation. Data points preserved for reporting. | Data retention locked. |
| OD-1B9-013 | Permissions | CONFIRMED | Use COMPANY-scoped permissions: CARE_PACKAGE_VIEW/CREATE/APPROVE/REJECT/CREATE_PAYMENT/REPORT_VIEW. | Policy structure locked. |
| OD-1B9-014 | Frontend scope | CONFIRMED | List/Create/Detail pages. Permission-gated actions. Backend-calculated totals only. | UI component roadmap locked. |
| OD-1B9-015 | Data model impact | CONFIRMED | Candidates: CarePackageRequests, CarePackageRequestItems. Fields for snapshots, links, totals. | EF Core schema design unblocked. |
| OD-1B9-016 | Migration/rollback needs | CONFIRMED | New migration/rollback pair required post-implementation planning (V0013/U0013). | CI scripts deferred. |
| OD-1B9-017 | Acceptance criteria | CONFIRMED | E2E validation matrix including builds, unit/integration/API/frontend tests, and business logic execution. | Quality gates locked. |
| OD-1B9-018 | Out-of-scope boundaries | CONFIRMED | Explicitly excludes refunds, partial payments, dynamic PDFs, multi-year, discount %, and generic print UI. | Scope bounded. |

## Confirmed Business Scope

Care Package Request:
- company-scoped.
- customer required.
- one or more request items.
- each item targets a care target with confirmed cốt count.
- cốt count snapshot stored at sale time.
- one-year service period per item.
- sale date determines effective-date price.
- subtotal/discount/total stored for audit and reporting.
- lifecycle supports draft/request, approval where required, payment, active, rejected/blocked states as needed.

Approval:
- only required for discount, price override, changed-price renewal, or configured approval-required rule.
- no approval required for configured-price request with no discount.

Payment:
- created only when request is payment-eligible.
- confirmed payment required before package becomes active.
- no partial/refund/cancellation.

Renewal:
- new request for next period.
- may reference previous request.
- no modification of prior paid/active record.

Reporting:
- preserve data needed for company/customer/care target/cốt count/service period/sale date/unit price/discount/total/payment/status reporting.

## Confirmed Business Rules

- Care Package Sales uses cốt-year sale unit.
- Phase 1B.9 supports one-year packages only.
- price comes from Service Foundation effective-date pricing.
- price formula is unit price per cốt per year × cốt count × 1 year.
- price snapshots must be preserved.
- renewal is a new request for next service period.
- approval required only for discount, price override, changed-price renewal, or later configured approval rule.
- no approval required for configured-price request with no discount.
- payment only after approval when approval is required, or after request becomes payment-eligible when no approval is required.
- Payment Foundation constraints apply.
- reconciliation/reporting data must be preserved.
- frontend must rely on backend-calculated price/status.
- no hard-coded price.

## Accepted Exclusions / Non-Goals

- refund.
- cancellation.
- partial payment.
- dynamic PDF/template generation.
- generic Payment Print UI.
- physical inventory/stamp stock management.
- unrelated services.
- production migration.
- release tag.
- push.
- multi-year.
- partial-year.
- discount percent UI.
- dedicated report/export UI.

## Candidate Backend / Data Model

CarePackageRequests:
- id.
- company_id.
- customer_id.
- status.
- requires_approval.
- workflow_instance_id.
- service_id or service_code reference.
- sale_date.
- subtotal_amount.
- discount_amount.
- discount_reason.
- total_amount.
- payment_transaction_id or payment/bill reference aligned with Payment Foundation.
- previous_request_id for renewal traceability.
- created_by / created_at / updated_at.
- rowversion.
- audit fields.

CarePackageRequestItems:
- id.
- care_package_request_id.
- care target reference supported by existing model.
- cốt count snapshot.
- service period start date.
- service period end date.
- unit price snapshot.
- line subtotal.
- notes.
- audit fields.

*(Note: Fields and rules are confirmed by PO decision. Exact table and column names remain candidate implementation names.)*

## Candidate API Surface

- `GET /api/v2/care-packages` (list)
- `GET /api/v2/care-packages/{id}` (detail)
- `POST /api/v2/care-packages` (create request/draft)
- `POST /api/v2/care-packages/{id}/submit` (submit to workflow only when approval is required)
- `POST /api/v2/care-packages/{id}/approve` (approval facade)
- `POST /api/v2/care-packages/{id}/reject` (rejection facade)
- `POST /api/v2/care-packages/{id}/create-payment` (create payment draft/bill when payment-eligible)
- `GET /api/v2/care-packages/{id}/payment-status` (read-only payment status)
- `POST /api/v2/care-packages/{id}/activate` (activate only after confirmed payment if implementation planning accepts active status)

## Candidate Workflow / Payment Model

Workflow:
- process key: `SELL_CARE_PACKAGE`.
- workflow only used when approval is required.
- approval not required for configured-price/no-discount request.
- WorkflowRuntimeService is source of truth.
- approve/reject facades must delegate to WorkflowRuntimeService.
- domain state sync only after successful workflow action.
- rejected requests cannot proceed to payment.

Payment:
- payment creation only when payment-eligible.
- payment-eligible:
  - no approval required and valid configured price, or
  - approval required and approved.
- Service Foundation effective-date price lookup required.
- missing/inactive price fails safely.
- no hard-coded care package price.
- confirmed payment required before active status.
- Payment Foundation constraints apply.

## Candidate Frontend Scope

- route: `/care-packages`
- route: `/care-packages/new`
- route: `/care-packages/:id`
- list page with filters for status/customer/company/service period/payment status.
- create form with customer, care target/items, service period, discount reason/amount if applicable.
- detail page with request summary, line items, pricing snapshot, workflow status, payment status.
- action UI:
  - submit only when approval is required and request is draft.
  - approve/reject only when pending approval and user has permission.
  - create payment only when payment-eligible.
  - activate only after confirmed payment if accepted.
- permission-gated UI.
- safe handling for 400/403/404/409.
- no frontend hard-coded price.
- frontend must display backend-calculated totals/status.
*(Dedicated report/export UI remains out of scope unless later accepted).*

## Candidate Permission Model

Candidate COMPANY-scoped permission codes:

- `CARE_PACKAGE_VIEW` (list/detail view)
- `CARE_PACKAGE_CREATE` (create request)
- `CARE_PACKAGE_APPROVE` (approve workflow)
- `CARE_PACKAGE_REJECT` (reject workflow)
- `CARE_PACKAGE_CREATE_PAYMENT` (create payment draft/bill from eligible request)
- `CARE_PACKAGE_REPORT_VIEW` (future report/list reporting view, if accepted in implementation planning)

Payment confirmation: reuse existing Payment Foundation permission such as `PAYMENT_CONFIRM`.
Payment correction: reuse existing Payment Foundation correction/admin permission.

## Candidate Validation Approach

Future validation must cover:
- backend build.
- backend unit tests.
- integration tests.
- API tests.
- frontend lint.
- frontend build.
- frontend Vitest.
- pricing effective-date behavior.
- cốt count calculation.
- one-year period validation.
- no partial-year/multi-year behavior.
- discount approval path.
- no-approval configured price path.
- renewal creates new request.
- overlap prevention.
- payment before eligibility blocked.
- payment after eligibility works.
- confirmed payment required before active.
- rejected request cannot proceed.
- missing/inactive service price fails safely.
- company-scope isolation.
- permission 403 handling.
- no refund/cancellation/partial payment.
- no hard-coded price.

## Risks / Dependencies

- **Dependency**: The frontend must heavily rely on backend calculations for total amounts and state transitions. No client-side price generation is permitted.
- **Dependency**: Strict reliance on the Service Foundation for active `CARE_PACKAGE` prices.

## Recommended Implementation Sequence

- Phase 1B.9-B implementation plan.
- Phase 1B.9-B1 backend/data foundation.
- Phase 1B.9-B2 workflow/payment integration.
- Phase 1B.9-C frontend implementation.
- Phase 1B.9-D operational validation.
- Phase 1B.9 closure review and PO closure acceptance.

## Recommended Next Gate

Project Owner Phase 1B.9-A detailed scope acceptance.

No implementation may begin until Project Owner detailed scope acceptance is recorded.
