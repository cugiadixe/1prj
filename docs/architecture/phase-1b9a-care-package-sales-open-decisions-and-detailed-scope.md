# Phase 1B.9-A Care Package Sales Open Decisions and Detailed Scope

## Status

BLOCKED — PROJECT OWNER DECISIONS REQUIRED

## Authorization Source

- Phase 1B.9 Project Owner scope acceptance commit:
  f5e61a09718d55aa9d9287e6d88b4ff35a9adfc7

## Planning Boundary

- this is open-decision resolution / detailed scope only.
- implementation is not authorized.
- migrations are not authorized.
- permission catalog changes are not authorized.
- production migration/tag/push are not authorized.

## Source Context Reviewed

- `docs/architecture/phase-1b9-project-owner-scope-acceptance.md`
- `docs/architecture/phase-1b9-care-package-sales-discovery-and-scope-plan.md`
- `docs/architecture/post-phase-1b8-project-owner-next-work-decision.md`
- `docs/architecture/post-phase-1b8-next-work-recommendation.md`
- `docs/architecture/phase-1b8-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b8-card-reprint-closure-review.md`
- `docs/architecture/phase-1b7-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b6-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b5-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b4-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b3-project-owner-closure-acceptance.md`
- `docs/architecture/phase-1b2-project-owner-completion.md`
- `docs/business/process-catalog.md`
- `docs/business/business-rules.md`
- `docs/business/acceptance-criteria.md`
- `docs/business/permission-catalog.md`

*(Unavailable documents: `PTKD-ERP-Master-Context.md` and `docs/architecture/project-readiness-review.md` were unsupported/unverified)*

## Open Decision Resolution Matrix

| ID | Topic | Status | Resolved Decision or Unresolved Question | Source Evidence | Impact | Required PO Answer |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| OD-1B9-001 | Terminology | OPEN | Unresolved | None | UI consistency | Are we standardizing on "Care Package" and "Gói chăm sóc"? |
| OD-1B9-002 | Sale unit | BLOCKER | Unresolved | `business-rules.md` blank | Data schema | Does the package attach to Customer, Grave, or Cốt? |
| OD-1B9-003 | Duration | BLOCKER | Unresolved | None | Lifecycle | Is duration fixed (e.g. yearly) or variable? |
| OD-1B9-004 | Pricing source | PROPOSED | Use Service module | `phase-1b6` accepted | Billing | N/A (Proposed) |
| OD-1B9-005 | Price calculation | BLOCKER | Unresolved | None | Payment amounts | How is the final price calculated (e.g. Rate x Duration x Cốt)? |
| OD-1B9-006 | Price changes | PROPOSED | Snapshot at workflow submit | `phase-1b6` accepted | Audit | N/A (Proposed) |
| OD-1B9-007 | Renewal rule | BLOCKER | Unresolved | None | Workflow routes | Are renewals treated as brand new requests or extensions? |
| OD-1B9-008 | Approval trigger | BLOCKER | Unresolved | `process-catalog.md` says pending | Workflow | What conditions mandate manual approval vs auto-approval? |
| OD-1B9-009 | Discount behavior | OPEN | Unresolved | None | Billing | Are discounts allowed and who approves them? |
| OD-1B9-010 | Payment timing | PROPOSED | After approval | `phase-1b7` blueprint | Billing | N/A (Proposed) |
| OD-1B9-011 | Payment constraints | PROPOSED | Full payment, no refund | `phase-1b7` blueprint | Billing | N/A (Proposed) |
| OD-1B9-012 | Reconciliation | OPEN | Unresolved | None | Reporting | How is deferred revenue reported? |
| OD-1B9-013 | Permissions | BLOCKER | Unresolved | `permission-catalog.md` blank | AuthZ | What are the exact View/Create/Action permission constants? |
| OD-1B9-014 | Frontend scope | BLOCKER | Unresolved | None | UI/UX | What exact pages/forms are required? |
| OD-1B9-015 | Data model | BLOCKER | Blocked by sale unit | Schema | Schema | N/A |
| OD-1B9-016 | Migration/rollback | PROPOSED | Phase 1 standards | Previous phases | DB CI | N/A (Proposed) |
| OD-1B9-017 | Acceptance criteria | BLOCKER | Unresolved | `acceptance-criteria.md` blank | Testing | Provide BDD acceptance scenarios |
| OD-1B9-018 | Out-of-scope | PROPOSED | Generic Payment UI, Inventory | Previous phases | Scope | N/A (Proposed) |

## Confirmed Business Scope

- **Process Definition**: `SELL_CARE_PACKAGE` is a reserved workflow process designed to handle the sale of care packages.
- **Dependencies**: Care Package Sales must build upon the Customer (Master Data), Service (Catalog & Pricing), Workflow (Execution), and Payment (Drafts & Statuses) foundations.

*(No further detailed scope can be confirmed until blockers are resolved).*

## Unresolved Blockers

- **Sale Unit**: We cannot define the database schema without knowing what entity the Care Package attaches to (Customer vs Grave vs Cốt).
- **Price Calculation**: We cannot implement the pricing logic or Payment Draft creation without the formula.
- **Duration / Renewal**: We cannot define the lifecycle statuses without knowing package duration and expiration behavior.
- **Approval Trigger**: We cannot build the Workflow Handler without knowing when to pause for manual approval.
- **Permissions**: We cannot build API endpoints or UI guards without defined permission constants.
- **Acceptance Criteria**: We cannot validate functionality without Project Owner scenarios.

## Accepted Exclusions / Non-Goals

- No implementation in 1B.9-A.
- No migrations in 1B.9-A.
- No source changes.
- No production migration.
- No release tag.
- No push.
- No refunds.
- No cancellation.
- No partial payment.
- No dynamic PDF/template generation unless later accepted.
- No generic Payment Print UI unless later accepted.
- No physical inventory/stamp stock management.
- No unrelated service modules.
- No undocumented business rule changes.

## Candidate Backend / Data Model

*Status: BLOCKED / PROVISIONAL*

- **CarePackageRequests**: Table to hold the workflow request (Requires PO clarification on FK target).
- **CarePackageRequestItems**: Optional table if multi-cốt is required.
- **Foreign Keys**: CustomerId, ServiceId, PaymentId, WorkflowInstanceId.
- **Fields**: CompanyId (strict isolation), SnapshotPrice, StartDate, EndDate (Requires PO clarification).

## Candidate API Surface

*Status: BLOCKED / PROVISIONAL*

- `GET /api/v2/care-packages`
- `GET /api/v2/care-packages/{id}`
- `POST /api/v2/care-packages`
- `POST /api/v2/care-packages/{id}/actions/submit`
- `POST /api/v2/care-packages/{id}/actions/approve`
- `POST /api/v2/care-packages/{id}/actions/reject`

## Candidate Workflow / Payment Model

*Status: BLOCKED / PROVISIONAL*

- **Workflow**: Activate `SELL_CARE_PACKAGE` in the workflow engine. Blocked on Approval Trigger.
- **Payment**: Payment Draft created post-approval. Constrained by 1B.7 rules (no refund, full payment only). Blocked on Price Calculation.

## Candidate Frontend Scope

*Status: BLOCKED / PROVISIONAL*

- `/care-packages` (List View)
- `/care-packages/create` (Form View)
- `/care-packages/{id}` (Detail / Status / Action View)
- Blocked on field requirements and exact UI states.

## Candidate Permission Model

*Status: BLOCKED / PROVISIONAL*

- `Permissions.CarePackages.View`
- `Permissions.CarePackages.Create`
- `Permissions.CarePackages.Approve` (Dynamic)

## Candidate Validation Approach

*Status: PROVISIONAL*

- **Unit Tests**: Domain calculation and status logic.
- **Integration Tests**: EF Core persistence and Workflow Handler invocation.
- **API Tests**: Authorization and boundary checks.
- **Frontend Tests**: Component rendering and permission guards.
- **Operational Validation**: E2E matrix testing.

## Risks / Dependencies

- **Risk**: Moving to implementation planning without resolving the sale unit and pricing calculation will lead to incorrect schema design and financial calculation errors.
- **Dependency**: Workflow execution requires PO definition of approval triggers to map to the `process-catalog.md`.

## Recommended Implementation Sequence

*Implementation sequence is deferred until Project Owner blocker decisions are recorded.*

## Recommended Next Gate

Project Owner Phase 1B.9-A blocker decision response.

No implementation may begin until the appropriate Project Owner acceptance or decision response is recorded.
