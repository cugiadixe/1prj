# Phase 1B.8-D Card Reprint Operational Validation Plan

## Status

PROPOSED — READY FOR PROJECT OWNER OPERATIONAL VALIDATION PLAN ACCEPTANCE

## Authorization Source

Reference:
- Phase 1B.8-C Project Owner frontend acceptance commit:
  692553f7465b60ad8ed36bca859a1fd6a86ff1aa

## Planning Scope

This document represents operational validation planning only. No execution of validation commands, testing, or migration is authorized by this document alone.

## Accepted Implementation Baseline

Reference:
- Phase 1B.8-B1 backend/data acceptance.
- Phase 1B.8-B2 workflow/payment acceptance.
- Phase 1B.8-C frontend acceptance.

## Validation Prerequisites

1.  **Environment**: Validation must run against a stable environment with the accepted frontend and backend baseline code.
2.  **Database**: `PTKD_DEV` database with the `V0013__card_reprint_foundation.sql` migration applied.
3.  **Users/Permissions**:
    - Users representing the Requester with `CARD_REPRINT_REQUEST_CREATE` and `CARD_REPRINT_REQUEST_VIEW`.
    - Users representing the Approver with `CARD_REPRINT_APPROVE` and `CARD_REPRINT_REQUEST_VIEW`.
    - Users representing the Printer/Distributor with `CARD_REPRINT_REQUEST_MARK_PRINTED` and `CARD_REPRINT_REQUEST_VIEW`.
4.  **Company Scope**: Test users spanning at least two distinct companies to verify data isolation.
5.  **Service Config**: A valid active Service Configuration/Price mapped to `CARD_REPRINT` (for 50,000 VND).

## Automated Validation Commands

The operational validation execution must run and verify the following commands from a clean state:

**Backend/Repository Checks**:
```powershell
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
git diff --check
```

**Frontend Checks** (from `src/frontend`):
```bash
npm run lint
npm run build
npm run test -- --run
npx vitest run src/cards
```

## End-to-End Scenario Matrix

| Scenario Type | Steps / Verification | Expected Result |
| :--- | :--- | :--- |
| **Happy Path** | 1. Create Card Reprint request<br>2. Submit request<br>3. Approve through Workflow Engine<br>4. Create payment draft/bill<br>5. Confirm payment via Payment Foundation (where supported by accepted scope)<br>6. View payment status<br>7. Mark printed<br>8. Mark released<br>9. Verify UI state matches backend | Request flows sequentially from `DRAFT` to `RELEASED`. UI components update accurately per status. |
| **Rejection Path** | 1. Create request<br>2. Submit request<br>3. Reject through Workflow Engine<br>4. Verify no payment/print actions can be taken | Request enters terminal `REJECTED` state. Downstream actions blocked in UI and API. |
| **Guard Paths** | - Create payment draft before approval<br>- Mark printed before confirmed payment<br>- Mark released before printed<br>- Duplicate payment draft creation<br>- Cross-company access (User A queries User B's request)<br>- Action performed without specific permission claim<br>- Missing/inactive CARD_REPRINT service/price config | APIs reject with HTTP 400, 403, 404, or 409 safely. UI handles errors without crashing. |
| **Boundary Paths** | Attempt refund, cancellation, partial payment, or physical inventory management. | Fails safely or unsupported by UI. |

## Manual Validation Checklist

Where automated tests cannot confirm visual flow, manual operational validation requires verifying:
- [ ] User can access Card Reprint list with `CARD_REPRINT_REQUEST_VIEW` permission.
- [ ] User without view permission receives a safe "access denied" state.
- [ ] Create request form successfully submits with a valid card ID, reason code, and notes.
- [ ] `Submit` action button appears only when the request is in `DRAFT`.
- [ ] `Approve` and `Reject` actions appear only in `PENDING_APPROVAL` status and for authorized approvers.
- [ ] `Create Payment` action appears only after the request is `APPROVED`.
- [ ] Payment status is displayed as read-only.
- [ ] `Mark Printed` action appears only after payment is confirmed (`PAID`).
- [ ] `Mark Released` action appears only after the request is `PRINTED`.
- [ ] Rejected requests are locked from proceeding.
- [ ] Cross-company request attempts are completely blocked (list and detail views).
- [ ] The frontend handles 400/403/404/409 responses gracefully via notifications or error boundaries.
- [ ] The frontend does not hard-code the 50,000 VND fee (unless supplied dynamically by the backend).
- [ ] The frontend does not infer `PAID` status locally; it relies entirely on the backend state.

## Evidence Requirements

For the operational validation report, the executor must capture:
- Git baseline output.
- Complete output or summaries of all automated backend and frontend validation commands.
- Results of the end-to-end scenario matrix (Pass/Fail).
- Screenshots (only if the operational validation process explicitly requires them for visual proof).
- Evidence of successful database migration and rollback.
- Evidence of permission/company-scope isolation functioning.
- Evidence of workflow transitions (Approval/Rejection).
- Evidence of payment draft creation and status integration.
- Evidence that lifecycle guards correctly restricted print and release actions.
- Explicit confirmation of boundary boundaries.
- A list of any known issues and their risk classification.

## Pass / Fail Criteria

**Pass Criteria**:
- All automated validation commands pass without unexpected errors.
- The happy path scenario completes successfully end-to-end.
- The rejection path correctly blocks all downstream actions.
- All guard paths fail safely (400/403/404/409).
- Permission and company-scope isolation checks pass.
- Frontend precisely reflects the backend lifecycle at all stages.
- No unauthorized behavior (such as out-of-scope logic) appears.
- No tracked source code files are modified by the validation planning process.

**Fail Criteria**:
- Any automated validation command fails.
- A payment draft can be created before the request is approved.
- Print or release actions can be triggered prematurely.
- A rejected request can proceed further in the lifecycle.
- Cross-company data access succeeds.
- The service fee is found hard-coded in the frontend or application logic rather than configuration.
- Any refund, cancellation, or partial payment behavior is identified.
- The operational validation requires unauthorized source code modifications.
- The captured validation evidence is incomplete.

## Stop Conditions

The operational validation must stop and report failure immediately if:
- Database migration fails or damages `PTKD_DEV`.
- The backend or frontend fails to compile/build.
- The repository state differs from the accepted baseline before validation starts.
- Any critical security or data-isolation boundary is breached.

## Boundary / Non-Goals

The following items are strictly excluded from Phase 1B.8-D scope:
- New backend implementation.
- New frontend implementation.
- Database migrations.
- Care Package Sales.
- Production migration.
- Release tag creation.
- Branch push.
- Dynamic PDF or template generation.
- Generic Payment Print UI.
- Refunds.
- Cancellations.
- Partial payments.
- Physical inventory or stamp stock management.

## Risks / Follow-Ups

- **Service Price Config**: Operational validation depends on a pre-existing or seedable `CARD_REPRINT` service/price configuration.
- **Payment Foundation**: Validation depends on the existing Payment Foundation confirmation behavior to transition the request to `PAID`.
- **Workflow Engine**: Validation depends on the existing Workflow Engine setup for routing approvals.
- **Payment Link Dependency**: Navigating to payment details relies on the existing Payment UI route structure (`/payments/:id`).

## Recommended Validation Sequence

1. Verify environment prerequisites and run database migration.
2. Execute all automated backend tests and checks.
3. Execute all automated frontend tests, build, and lint checks.
4. Perform the manual End-to-End Happy Path scenario.
5. Perform the manual Rejection and Guard path scenarios.
6. Compile the evidence package into the operational validation report.

## Recommended Next Gate

Project Owner Phase 1B.8-D operational validation plan acceptance.

**Note**: Do not authorize operational validation execution until this plan is accepted by the Project Owner.
