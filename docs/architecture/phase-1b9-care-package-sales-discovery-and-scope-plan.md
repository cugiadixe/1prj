# Phase 1B.9 Care Package Sales Discovery and Scope Plan

## Status

PROPOSED — READY FOR PROJECT OWNER SCOPE REVIEW

## Authorization Source

- Post-Phase 1B.8 Project Owner next-work decision commit:
  21afd45c4719d304ec604809d79820497b3dc1fd

## Planning Boundary

- this is discovery/scope planning only.
- implementation is not authorized.
- migrations are not authorized.
- production migration/tag/push are not authorized.

## Source Context Reviewed

- `docs/architecture/post-phase-1b8-project-owner-next-work-decision.md`
- `docs/architecture/post-phase-1b8-next-work-recommendation.md`
- `docs/architecture/phase-1b8-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b8-card-reprint-closure-review.md`
- `docs/architecture/phase-1b7-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b7d-project-owner-operational-validation-acceptance.md`
- `docs/architecture/phase-1b6-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b5-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b4-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b3-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b2-project-owner-completion.md`
- `docs/business/process-catalog.md`
- `docs/business/business-rules.md`
- `docs/business/acceptance-criteria.md`
- `docs/business/permission-catalog.md`

*(Note: `PTKD-ERP-Master-Context.md` and `docs/architecture/project-readiness-review.md` were recorded as unavailable/unsupported and not extracted.)*

## Source-Supported Business Facts

- The `SELL_CARE_PACKAGE` (Bán gói chăm sóc) workflow process is defined in the `process-catalog.md`.
- It currently holds a `RESERVED / INACTIVE` status.
- It explicitly notes: *The business need is confirmed, but form fields, entity schema, execution handler and exact approval trigger require the service-sales module specification before activation.*

## Completed Foundation Dependencies

- **Customer**: Provides master data for linking purchasers (Customers) to transactions and scopes access by Company.
- **Service**: Provides the underlying service catalog and effective-date pricing structures required to quote and bill for care packages.
- **Workflow**: Provides the execution engine (`WorkflowRuntimeService`), configurable status progression, and dynamic approver resolution.
- **Payment**: Provides draft bill creation tied securely to workflow approvals and strictly limits modifications via Payment Status constraints.
- **Card Reprint**: Validated the complete blueprint (API, EF, Workflow Handler, Payment integration, and React UI) for how an operational service request flows through the ERP.

## Proposed Business Scope

Care Package Sales will track the purchase of "chăm sóc" services by customers. This includes:
- Initiating a `SELL_CARE_PACKAGE` request linked to a specific customer.
- Selecting a predefined service from the catalog.
- Passing through an approval workflow before payment.
- Transitioning to billing and execution upon approval.

*Note: Many specifics of pricing calculations, renewal rules, duration, and discount behaviors are currently undocumented in the active repository context.*

## Open Decisions / Blockers

| ID | Topic | Status | Evidence | Impact | Recommended Resolution |
| :--- | :--- | :--- | :--- | :--- | :--- |
| OD-1B9-001 | Care Package terminology | OPEN | No glossary definitions available | UI/API consistency | Standardize on `CarePackageRequest` and `Bán gói chăm sóc` |
| OD-1B9-002 | Sale unit | BLOCKER | No rule in `business-rules.md` | Data structure (Grave vs Customer link) | PO must define if sale attaches to Customer, Grave, or Cốt |
| OD-1B9-003 | Package duration | OPEN | Undefined | Expiry logic | Clarify if yearly only or variable months |
| OD-1B9-004 | Pricing source | PROPOSED | Service foundation exists | Billing | Use existing `Service` effective-date prices |
| OD-1B9-005 | Price calculation | BLOCKER | Undefined | Billing accuracy | PO must define calculation (e.g. Price x Quantity/Cốt x Duration) |
| OD-1B9-006 | Price changes | PROPOSED | Service foundation history | Reporting | Lock price at Workflow submission snapshot |
| OD-1B9-007 | Renewal rule | OPEN | Undefined | Lifecycle | Clarify if renewals are new workflow requests or extensions |
| OD-1B9-008 | Approval trigger | OPEN | `process-catalog.md` lists pending trigger | Workflow | Define auto-approve thresholds vs mandatory manual checks |
| OD-1B9-009 | Discount behavior | OPEN | Undefined | Payment integration | Determine if discounts require special workflow branches |
| OD-1B9-010 | Payment timing | PROPOSED | Card Reprint blueprint | Payment integration | Payment draft created *after* workflow approval |
| OD-1B9-011 | Payment constraints | PROPOSED | Payment Foundation rules | Billing | Full payment only; no partial, refund, or cancellation |
| OD-1B9-012 | Reconciliation/reporting | OPEN | Undefined | Accounting | Define accounting export requirements for deferred revenue |
| OD-1B9-013 | Permissions | OPEN | `permission-catalog.md` lacks exact constants | Authorization | Define View, Create, and Action constants |
| OD-1B9-014 | Frontend scope | OPEN | Undefined | UI/UX | Map required list, detail, and form fields |
| OD-1B9-015 | Data model impact | PROPOSED | Card Reprint blueprint | Schema | New `CarePackageRequest` entity linking Customer, Service |
| OD-1B9-016 | Migration/rollback needs | PROPOSED | Phase 1 standard | Database | Standard forward/rollback SQL scripts required |
| OD-1B9-017 | Acceptance criteria | BLOCKER | `acceptance-criteria.md` missing | Validation | PO must supply acceptance scenarios |
| OD-1B9-018 | Out-of-scope | PROPOSED | Previous deferrals | Scope control | Exclude dynamic PDF, generic Payment UI, Refunds, Inventory |

## Candidate Backend / Data Impact

- **Entities**: New `CarePackageRequest` (and potential line items if multi-cốt).
- **Relationships**: FK to Customer, Service, and Workflow/Payment tables.
- **EF Core**: Mapping configurations in `AppDbContext`.
- **API**: New endpoints for CRUD operations under `/api/v2/care-packages`.
- **Security**: Company-scope enforcement and new Permission constants.

## Candidate API Impact

- `POST /api/v2/care-packages`
- `GET /api/v2/care-packages`
- `GET /api/v2/care-packages/{id}`
- Action facades delegating to Workflow Engine (Submit, Approve, Reject).

## Candidate Workflow / Payment Impact

- Activation of `SELL_CARE_PACKAGE` in the workflow engine.
- Implementation of `CarePackageExecutionHandler` to process successful approvals.
- Integration to generate Payment Drafts explicitly tied to the resolved Care Package price.

## Candidate Frontend Impact

- `src/care-packages/CarePackageListPage.tsx`
- `src/care-packages/CarePackageCreatePage.tsx`
- `src/care-packages/CarePackageDetailPage.tsx`
- Action models, status badge mappings, and permission-gated buttons.

## Candidate Permission Impact

- `Permissions.CarePackages.View`
- `Permissions.CarePackages.Create`
- `Permissions.CarePackages.Approve` (managed via dynamic Workflow assignments)

## Candidate Validation Approach

- **Unit Tests**: Domain rules, price calculation logic, status transitions.
- **Integration Tests**: EF Core mappings, workflow execution handler paths.
- **API Tests**: Authorization, validation boundaries, concurrency.
- **Frontend Tests**: Vitest suite for UI component rendering and action state hooks.

## Out of Scope / Non-Goals

- No source implementation in this task.
- No database migration in this task.
- No production migration.
- No release tag.
- No push.
- No refund.
- No cancellation.
- No partial payment.
- No dynamic PDF/template generation unless later accepted.
- No generic Payment Print UI unless later accepted.
- No physical inventory/stamp stock management.
- No unrelated service modules.
- No undocumented business rule changes.

## Proposed Implementation Sequence

1. **Phase 1B.9-A**: Open-decision resolution / detailed scope.
2. **Phase 1B.9-B1**: Backend/data foundation.
3. **Phase 1B.9-B2**: Workflow/payment integration.
4. **Phase 1B.9-C**: Frontend implementation.
5. **Phase 1B.9-D**: Operational validation.
6. **Phase 1B.9 Closure Review / PO Closure Acceptance**.

## Risks / Dependencies

- **Risk**: Significant blockers remain regarding pricing algorithms, renewal logic, and unit of sale (Customer vs. Grave/Cốt).
- **Dependency**: Relies heavily on the existing Service Catalog (1B.6) and Payment Foundation (1B.7) behaving stably.

## Recommended Next Gate

Project Owner Phase 1B.9 scope review / scope acceptance.

No implementation may begin until Project Owner scope acceptance is recorded.
