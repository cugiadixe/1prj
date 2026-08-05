# Phase 1B.9-D Care Package Sales Operational Validation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER OPERATIONAL VALIDATION PLAN ACCEPTANCE

## Authorization Source

Reference:
- Phase 1B.9-C Project Owner frontend acceptance commit:
  6dfa4b5cd6dc5af526884f70933f0070d50e251a

## Planning Boundary

- This is operational validation planning only.
- Operational validation execution is not authorized.
- Source changes are not authorized.
- Backend/frontend implementation is not authorized.
- Migrations are not authorized.
- Business docs and permission catalog changes are not authorized.
- Production migration/tag/push are not authorized.

Operational validation execution may begin only after Project Owner operational validation plan acceptance.

## Accepted B1 Backend/Data Scope Summary

Phase 1B.9-B1 delivered:

- V0014/U0014 Care Package Sales foundation migration/rollback.
- CarePackageRequest and CarePackageRequestItem domain entities.
- EF Core configurations with snake_case mappings.
- AppDbContext integration.
- CarePackageRequestDto and CarePackageRequestItemDto.
- CreateCarePackageRequest and CreateCarePackageRequestItem DTOs.
- ICarePackageRequestService and CarePackageRequestService.
- CarePackageRequestsController with `/api/v2/care-packages` list/detail/create endpoints.
- Backend-calculated pricing snapshot foundation via Service Foundation effective-date pricing.
- Company-scope authorization via RequirePermission with X-Company-Id.
- B1 permission constants: CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE.
- Backend unit tests (CarePackageRequestTests) and API tests (CarePackageRequestApiTests).
- Integration tests (MigrationRollbackTests, SecuritySchemaTests).

Accepted by PO at commit 3103c4064c190a94531d5ced5ddc23b95acd7708.

## Accepted B2 Workflow/Payment Scope Summary

Phase 1B.9-B2 delivered:

- SELL_CARE_PACKAGE workflow integration.
- Approval-required path: submit initiates workflow, sets PendingApproval, approve/reject facades delegate to WorkflowRuntimeService.
- No-approval path: configured-price/no-discount requests skip directly to PaymentEligible.
- Domain state synchronization exclusively via CarePackageExecutionHandler upon successful workflow action completion.
- Rejected requests blocked from advancing to payment.
- Payment eligibility verified before payment draft creation.
- Create-payment transitions request and delegates to IPaymentTransactionService.
- Duplicate payment creation blocked when pending/paid transaction exists.
- Payment-status endpoint read-only.
- Active-status transitions request to Active when payment confirmed.
- Payment Foundation constraints: VND only, full payment only, no partial payment, no refund, no cancellation.
- B2 permission constants: CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT.
- Backend unit tests and API tests updated for B2 lifecycle.

Accepted by PO at commit 87b783b1f2b64c73fe67aff57016324c543c1003.

## Accepted C Frontend Scope Summary

Phase 1B.9-C delivered:

- Route `/care-packages` — list page.
- Route `/care-packages/new` — create page.
- Route `/care-packages/:id` — detail page.
- care-packages frontend module (types.ts, carePackageApi.ts, hooks.ts, errorMessages.ts).
- 9 API client functions mapping to 9 backend endpoints.
- 9 React Query hooks.
- List page: table, status filter, permission-gated create button, row navigation.
- Create page: form with customer/service/sale date/grave/cot count/service period/discount fields, backend-calculated response.
- Detail page: summary, line items, pricing snapshots, workflow/payment status, lifecycle action buttons, approve/reject/payment modals.
- Permission-gated UI: CARE_PACKAGE_VIEW, CARE_PACKAGE_CREATE, CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT (COMPANY scope, UX-only).
- Safe error handling: 400/403/404/409.
- 19 frontend tests across 3 test files.

Accepted by PO at commit 6dfa4b5cd6dc5af526884f70933f0070d50e251a.

## Backend Validation Plan

The following commands must be executed during future D operational validation execution:

```bash
dotnet build src/backend/PTKD-ERP.sln
```
Expected: Build succeeded. 0 errors. Warnings acceptable if pre-existing.

```bash
dotnet test tests/backend/PTKD.UnitTests/
```
Expected baseline: 236 passed (from B2 acceptance evidence).

```bash
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
```
Expected baseline: 203 passed (from B2 acceptance evidence).

```bash
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
```
Expected baseline: 308 passed (from B2 acceptance evidence).

The D execution report must record actual results.

## Frontend Validation Plan

The following commands must be executed during future D operational validation execution:

```bash
cd src/frontend && npm run lint
```
Expected: clean, only pre-existing warnings in auth/ files acceptable.

```bash
cd src/frontend && npm run build
```
Expected: Build succeeded.

```bash
cd src/frontend && npm run test -- --run
```
Expected baseline: 71 test files passed, 500 tests passed (from C acceptance evidence).

```bash
cd src/frontend && npx vitest run src/care-packages
```
Expected baseline: 3 test files passed, 19 tests passed (from C acceptance evidence).

The D execution report must record actual results.

## Repository Validation Plan

The following commands must be executed during future D operational validation execution:

```bash
git status --short --untracked-files=all
git diff --name-status
git diff --numstat
git diff --cached --name-status
git diff --check
git tag --points-at HEAD
git remote -v
```

Pass criteria:
- No tracked working-tree modifications after D validation report creation, except the D validation report itself when execution is authorized.
- No staged files unless preparing the authorized D validation report commit.
- Untracked scratch/decompiled/FixStrategy/script/debug files may remain but must not be staged.
- No production migration.
- No tag.
- No push.

## Manual API Validation Checklist

The following must be validated during future D operational validation execution:

### Authentication / Company Scope
- [ ] Authenticated request with valid company context succeeds.
- [ ] Missing X-Company-Id returns safe failure.
- [ ] Unauthorized company returns 403.
- [ ] Cross-company access blocked.

### List / Detail / Create
- [ ] List care package requests returns paginated results.
- [ ] Get detail returns full DTO with items.
- [ ] Create valid care package request succeeds with backend-calculated pricing.
- [ ] Create invalid request returns 400 with safe detail/title.
- [ ] Missing/inactive service price returns safe failure.
- [ ] Backend returns pricing snapshots (unitPriceSnapshot, lineSubtotal, subtotalAmount, totalAmount).
- [ ] Frontend does not calculate authoritative totals.

### No-Approval Path
- [ ] Configured-price/no-discount request becomes PaymentEligible directly.
- [ ] No workflow required for no-approval path.

### Approval-Required Path
- [ ] Discount or approval-required rule triggers approval requirement.
- [ ] Submit creates/uses SELL_CARE_PACKAGE workflow path.
- [ ] Approve through WorkflowRuntimeService succeeds.
- [ ] Reject through WorkflowRuntimeService succeeds.
- [ ] State sync occurs only after successful workflow action.
- [ ] Rejected request cannot create payment.

### Payment Path
- [ ] Create payment only when payment-eligible.
- [ ] Create payment blocked before approval when approval is required.
- [ ] Create payment blocked for rejected request.
- [ ] Duplicate payment creation blocked (409).
- [ ] Payment-status endpoint is read-only.
- [ ] Confirmed payment can support activate to Active status.

### Payment Foundation Constraints
- [ ] VND only.
- [ ] Full payment only.
- [ ] No partial payment.
- [ ] No refund.
- [ ] No cancellation.
- [ ] One bill cannot be paid multiple times.

### Safe Errors
- [ ] 400 invalid input — safe message.
- [ ] 403 missing permission/company access — safe message.
- [ ] 404 not found — safe message.
- [ ] 409 invalid lifecycle transition — safe message.
- [ ] 409 duplicate payment — safe message.
- [ ] 409 not payment-eligible — safe message.
- [ ] No raw backend internals exposed.

## Manual Frontend/UI Validation Checklist

The following must be validated during future D operational validation execution:

### Routes
- [ ] `/care-packages` loads list page.
- [ ] `/care-packages/new` loads create page.
- [ ] `/care-packages/:id` loads detail page.

### List Page
- [ ] Rows display with ID, Customer ID, Status, Total Amount, Sale Date, Created At.
- [ ] Status filter works.
- [ ] Create button visible only with CARE_PACKAGE_CREATE permission.
- [ ] Row click navigates to detail.
- [ ] Loading state displays.
- [ ] Empty state displays.
- [ ] Error state displays safely.
- [ ] Permission denied (403) displays safely.

### Create Page
- [ ] Required fields render (Customer ID, Cot Count, Sale Date, Service Period Start).
- [ ] Customer/grave manual ID selector limitation is visible and accepted.
- [ ] Discount amount/reason behavior correct (reason required when amount > 0).
- [ ] Backend validation errors shown safely.
- [ ] Backend-calculated response navigates to detail.
- [ ] No frontend hard-coded price.
- [ ] No discount percent UI.
- [ ] No multi-year/partial-year UI.

### Detail Page
- [ ] Summary panel displays all fields.
- [ ] Line items table displays.
- [ ] Pricing snapshots display from backend values (subtotal, discount, total).
- [ ] Workflow instance link displays when workflowInstanceId exists.
- [ ] Payment status is read-only display.
- [ ] Submit button: visible only when Draft + requiresApproval + CARE_PACKAGE_CREATE.
- [ ] Approve button: visible only when PendingApproval + CARE_PACKAGE_APPROVE.
- [ ] Reject button: visible only when PendingApproval + CARE_PACKAGE_REJECT.
- [ ] Create Payment button: visible only when PaymentEligible + CARE_PACKAGE_CREATE_PAYMENT.
- [ ] Activate button: visible only when Paid + CARE_PACKAGE_CREATE.
- [ ] Stale status / backend 409 handled safely.
- [ ] No Payment Print UI.
- [ ] No report/export UI.
- [ ] No PDF/template UI.

### Permission UI
- [ ] CARE_PACKAGE_VIEW gate works (403 handled).
- [ ] CARE_PACKAGE_CREATE gate works (create button, create page, submit, activate).
- [ ] CARE_PACKAGE_APPROVE gate works (approve button).
- [ ] CARE_PACKAGE_REJECT gate works (reject button).
- [ ] CARE_PACKAGE_CREATE_PAYMENT gate works (create payment button).
- [ ] Frontend gates are UX-only; backend remains authoritative.

## Workflow / Payment Lifecycle Validation Scenarios

The following end-to-end scenarios must be validated during future D operational validation execution:

### Scenario 1 — No-Approval Sale
1. Create configured-price/no-discount request.
2. Verify status becomes PaymentEligible (no workflow).
3. Create payment.
4. Verify payment status via read-only endpoint.
5. Confirm/observe payment state using existing Payment Foundation behavior.
6. Activate if backend supports and preconditions are satisfied.
7. Verify final status is Active.

### Scenario 2 — Approval-Required Sale
1. Create request with discount amount and reason.
2. Verify requiresApproval is true.
3. Submit for approval.
4. Verify status is PendingApproval.
5. Approve.
6. Verify status becomes PaymentEligible.
7. Create payment.
8. Verify payment status.

### Scenario 3 — Rejected Approval
1. Create approval-required request.
2. Submit for approval.
3. Reject with reason.
4. Verify status is Rejected.
5. Verify payment creation is blocked (409).
6. Verify UI hides create-payment button.

### Scenario 4 — Duplicate Payment Guard
1. Create payment-eligible request.
2. Create payment once — succeeds.
3. Attempt create payment again — expect 409.
4. Verify safe error message displayed.

### Scenario 5 — Company Isolation
1. Create request under Company A.
2. Attempt access/action under Company B.
3. Expect 403 or 404 per backend convention.
4. Verify safe UI error message.

### Scenario 6 — Permission-Gated Actions
1. User without CARE_PACKAGE_CREATE cannot see create button or access create page.
2. User without CARE_PACKAGE_APPROVE cannot see approve button.
3. User without CARE_PACKAGE_REJECT cannot see reject button.
4. User without CARE_PACKAGE_CREATE_PAYMENT cannot see create payment button.
5. Backend rejects unauthorized direct API calls with 403.

## Dependency / Risk Checklist

### Blocking Before Deployment / Operational Readiness (Unless Resolved)

- [ ] **SQL permission seed alignment**: B2 added permission constants CARE_PACKAGE_APPROVE, CARE_PACKAGE_REJECT, CARE_PACKAGE_CREATE_PAYMENT without SQL seed rows. Database permission seeds must be added or confirmed before runtime permission gating functions in production. B1 constants CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE also require seed confirmation.
- [ ] **SELL_CARE_PACKAGE workflow runtime configuration**: workflow process configuration must be administratively established via workflow admin UI before runtime workflow operations (submit/approve/reject) function. Without this, the approval-required path will fail at runtime.
- [ ] **Runtime permission row confirmation**: all 5 care package permission codes must be grantable to users/roles at runtime.

### Non-Blocking (Must Be Tracked)

- [ ] Manual ID selectors for customer/grave are accepted but may need UX improvement in a future slice.
- [ ] Stale frontend status may trigger backend 409; frontend shows safe error message.
- [ ] Care target display metadata may need future selector/search UX.
- [ ] No report/export UI in 1B.9-C scope.
- [ ] No Payment Print UI in 1B.9-C scope.
- [ ] No production migration/tag/push in D unless separately authorized.

## Pass / Fail Criteria

Future D operational validation execution can PASS only if:

### Backend
- Build passes.
- UnitTests pass.
- IntegrationTests pass.
- ApiTests pass.

### Frontend
- Lint passes or only accepted pre-existing warnings remain.
- Build passes.
- Full Vitest passes.
- Targeted care-packages tests pass.

### Repository
- git diff --check clean.
- No unauthorized tracked modifications.
- No staged scratch files.
- No production migration/tag/push.

### Manual / API / UI
- No-approval lifecycle passes (Scenario 1).
- Approval-required lifecycle passes (Scenario 2).
- Rejected lifecycle blocks payment (Scenario 3).
- Duplicate payment is blocked (Scenario 4).
- Permission-gated UI behaves correctly (Scenario 6).
- Company scope is enforced (Scenario 5).
- Backend-calculated pricing/status displayed correctly.
- No frontend hard-coded price.
- Safe error messages for all failure modes.

### Dependencies
- SQL permission seed alignment must be classified:
  - Resolved before operational readiness, or
  - Explicitly listed as blocker to deployment/operational readiness.
- SELL_CARE_PACKAGE workflow runtime configuration must be classified:
  - Resolved before operational readiness, or
  - Explicitly listed as blocker to deployment/operational readiness.

### Status Values

- **PASSED** — READY FOR PROJECT OWNER OPERATIONAL VALIDATION ACCEPTANCE
- **PASSED WITH DEPLOYMENT READINESS NOTES** — READY FOR PROJECT OWNER OPERATIONAL VALIDATION ACCEPTANCE (when automated validation passes but deployment dependencies remain unresolved)
- **FAILED / BLOCKED** — CORRECTION OR DECISION REQUIRED

## Future D Execution Report Requirement

The future D operational validation execution task must produce:

docs/architecture/phase-1b9d-care-package-sales-operational-validation-report.md

Required sections:
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

This report must not be created in the planning task.

## Out of Scope / Non-Goals

- Operational validation execution in this task.
- Source code changes.
- Frontend/backend implementation.
- Database migrations.
- Business docs changes.
- Permission catalog changes.
- Production migration.
- Release tag.
- Push.
- Refund/cancellation/partial payment.
- PDF/template/print/report UI.
- New business rules.
- Multi-year or partial-year packages.
- Discount percent UI.
- Physical inventory/stamp stock management.

## Recommended Next Gate

Project Owner Phase 1B.9-D operational validation plan acceptance.

No operational validation execution may begin until Project Owner operational validation plan acceptance is recorded.

After PO operational validation plan acceptance, the recommended execution scope is:

Phase 1B.9-D Care Package Sales operational validation execution:
- Run backend validation suite.
- Run frontend validation suite.
- Run repository validation checks.
- Execute manual API validation checklist.
- Execute manual UI validation checklist.
- Execute workflow/payment lifecycle scenarios.
- Assess dependency/risk checklist.
- Produce operational validation report.

Exclusions from D execution:
- Source code changes unless a blocking issue is discovered and separately authorized.
- Production migration.
- Tag/push.
- New feature implementation.
