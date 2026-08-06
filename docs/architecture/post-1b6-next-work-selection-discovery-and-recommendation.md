# Post-1B.6 Next-Work Selection Discovery and Recommendation

## Status

PROPOSED — REQUIRES PROJECT OWNER NEXT-WORK SELECTION DECISION

## Authorization Source

Reference:
- Phase 1B.6 Project Owner closure acceptance commit:
  d1914edacb536bd1c5d8f8556e338273f99f4cae

- Phase 1B.6 Service Module Foundation is closed.
- This document is discovery and recommendation only.
- This document does not authorize implementation.

## Source Documents Reviewed

- docs/architecture/phase-1b6-project-owner-closure-acceptance.md
- docs/architecture/phase-1b6d-operational-validation-and-closure-acceptance-review.md
- docs/architecture/phase-1b6d-operational-validation-and-closure-report.md
- docs/architecture/phase-1b6d-project-owner-operational-validation-plan-acceptance.md
- docs/architecture/phase-1b6d-operational-validation-and-closure-plan.md
- docs/architecture/phase-1b6c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b6c-frontend-implementation-acceptance-review.md
- docs/architecture/phase-1b6c-frontend-implementation-report.md
- docs/architecture/phase-1b6c-project-owner-frontend-scope-acceptance.md
- docs/architecture/phase-1b6c-frontend-scope-and-implementation-plan.md
- docs/architecture/phase-1b6b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b6b-backend-data-foundation-implementation-acceptance-review.md
- docs/architecture/phase-1b6b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b6b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b6b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b6-project-owner-scope-acceptance.md
- docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md
- PTKD-ERP-Master-Context.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/architecture/project-readiness-review.md
- docs/architecture/post-1b5-next-work-selection-discovery-and-recommendation.md (referenced prior state)

## Closed Phase 1B.6 Summary

The Phase 1B.6 Service Module Foundation closed the following scope:
- V0011/U0011 database migrations and rollback tests.
- Core tables: `Service_Types`, `Service_Price_History`, `Services`, `Service_History`.
- Backend domain and application services: `ServiceTypeService`, `ServiceService`, `ServicePriceOverrideExecutionHandler`.
- Controllers: `ServiceTypeController`, `ServiceController` (API v2).
- Security: `SERVICE_*` permissions integrated.
- Frontend: API clients, Service Type list/form/detail pages, Service list/create/detail pages.
- UI elements: Price snapshot display, lifecycle/status indicators, and the `SERVICE_PRICE_OVERRIDE` workflow boundary UI.
- Comprehensive route and navigation wiring.
- Exhaustive backend and frontend test coverage.
- Formal operational validation and closure acceptance by the Project Owner.

## Remaining Deferred Business Areas

- Payment / Billing / Collection / Reconciliation.
- Card Reprint.
- Care Package Sales.
- Production migration/release.

## Candidate Next Work Options

### Option A — Payment / Billing / Collection / Reconciliation Foundation
- **Scope summary**: Implement the backend data structures, API endpoints, workflow handlers, and frontend interfaces required to process payments for services, manage billing, and reconcile collections.
- **Business value**: High. Realizes revenue. Completes the business transaction lifecycle for the recently implemented Customer Master (1B.5) and Service (1B.6) modules.
- **Dependency readiness**: High. Service and Customer modules are closed and fully available to be billed against.
- **Technical readiness**: High. The workflow engine and security foundations are mature and capable of handling complex state changes required by financial transactions.
- **Likely backend scope**: Payment entities, transaction logs, receipt generation, `PaymentService`, workflow handlers for payment verification.
- **Likely frontend scope**: Payment collection forms, receipt views, billing history on Customer detail page, payment-gated workflow transitions.
- **Likely database/migration scope**: Substantial. New tables for `Payments`, `Invoices`, `Receipts`, `Ledger_Entries`.
- **Workflow/permission impact**: Requires new `PAYMENT_*` permissions and potentially a `PAYMENT_VERIFICATION` workflow definition.
- **Risks/blockers**: Financial data requires strict concurrency control (rowversion) and precise transaction boundaries to prevent race conditions.
- **Reason to select now**: It is the most critical missing link in the core business flow (Customer -> Service -> Payment).
- **Reason to defer**: High complexity; may require integrating external payment gateways which might not be fully specified yet.

### Option B — Card Reprint Workflow/Module
- **Scope summary**: Implement the workflow and UI allowing users to request a replacement/reprint for an existing physical/virtual card.
- **Business value**: Medium. Solves a common customer service operational pain point.
- **Dependency readiness**: High. Customer module is closed.
- **Technical readiness**: High. Workflow engine is ready.
- **Likely backend scope**: Card request entities, workflow handlers for approval/reprint fulfillment.
- **Likely frontend scope**: Card reprint request form, approval UI.
- **Likely database/migration scope**: Low/Medium. New `Card_Requests` or similar tracking table.
- **Workflow/permission impact**: Likely requires a dedicated `CARD_REPRINT_APPROVAL` workflow and `CARD_*` permissions.
- **Risks/blockers**: A card reprint typically incurs a fee. If the Payment module (Option A) is not implemented, the fee collection step would have to be manually bypassed or stubbed.
- **Reason to select now**: Smaller, highly contained scope. Good for a quick win.
- **Reason to defer**: Should ideally follow the Payment module so the reprint fee can be collected natively.

### Option C — Care Package Sales Workflow/Module
- **Scope summary**: Implement the `SELL_CARE_PACKAGE` workflow, inventory tracking, and fulfillment logic.
- **Business value**: Medium-High. Direct revenue generating feature for upselling customers.
- **Dependency readiness**: High (Customer exists).
- **Technical readiness**: High.
- **Likely backend scope**: Care package catalogs, sales orders, fulfillment state machine.
- **Likely frontend scope**: Sales catalog UI, purchase order forms.
- **Likely database/migration scope**: Medium. `Care_Packages`, `Care_Package_Sales`.
- **Workflow/permission impact**: Requires `CARE_PACKAGE_*` permissions and `SELL_CARE_PACKAGE` workflow configuration.
- **Risks/blockers**: Like Card Reprint, Care Package Sales directly require financial transactions. Implementing sales without a Payment module creates an incomplete feature.
- **Reason to select now**: Expands the product portfolio available to customers.
- **Reason to defer**: Strongly depends on the Payment module.

### Option D — Production Release and Operational Rollout of 1B.6
- **Scope summary**: Stop feature development and focus entirely on deploying the current codebase (Customer Master + Service Module) to UAT/Production.
- **Business value**: Medium. Gets software into the hands of users.
- **Dependency readiness**: High.
- **Technical readiness**: Requires infrastructure provisioning and CI/CD pipelines (currently deferred).
- **Likely backend/frontend/db scope**: Purely DevOps/infrastructure focus (IIS configuration, environment variables, production DB migration scripts).
- **Workflow/permission impact**: None.
- **Risks/blockers**: The system lacks Payment functionality. A production release now would mean the business has to collect payments outside the system manually.
- **Reason to select now**: If the business urgently needs to start registering services and is okay with manual offline payments.
- **Reason to defer**: Delivering an ERP without billing capabilities is often unacceptable for standard operations.

## Comparative Assessment

- **Business priority**: Option A (Payment) provides the highest systemic value, acting as the foundation for both Option B and Option C.
- **Dependency readiness**: All options are technically unblocked by Phase 1B.6's closure.
- **Ability to deliver as next vertical slice**: Option A is a massive but necessary vertical slice. Option B is smaller but functionally incomplete without Option A.
- **Risk**: Option A carries the highest implementation risk due to financial accuracy requirements, but avoiding it only compounds technical debt.
- **Alignment with closed Service Module Foundation**: Payment is the immediate next step in the lifecycle of a newly registered Service.
- **Unlocking later work**: Option A completely unlocks Options B and C.

## Recommended Next Work

**Recommended**: Option A — Payment / Billing / Collection / Reconciliation Foundation.

**Explanation**: 
Option A is the structurally correct next step. The core workflow of the ERP is to register a customer, assign a service, and collect payment for that service. With Customer Master (1B.5) and Service Module Foundation (1B.6) complete, the system is primed to process financial transactions. Deferring Payment any longer will bottleneck the implementation of dependent modules like Card Reprint and Care Package Sales, as both of those actions incur fees that must be collected. By selecting Payment now, we establish the financial foundation required for all future revenue-generating workflows, ensuring that subsequent modules can be implemented completely without stubbing billing logic.

## Recommended Next Gate

Recommended next authorized task:
Project Owner selection decision for post-1B.6 next work.

After the Project Owner selects the next work item, authorize discovery and detailed planning only for that selected work.

Do not authorize implementation yet.

## Non-Goals

This document does not:
- implement Payment,
- implement Card Reprint,
- implement Care Package Sales,
- modify source code,
- modify tests,
- modify frontend/backend files,
- modify migrations/rollbacks,
- modify business docs,
- run production migration,
- create release tag,
- push.

## Risks / Notes

- The local branch may be ahead of origin/main; no push is authorized.
- Scratch/decompiled/FixStrategy files remain untracked and must not be staged.
- Production release remains deferred.
- Live browser validation may still be performed later if required.
- Future database migrations must ensure they increment and update test fixture reset targets appropriately beyond V0011.
