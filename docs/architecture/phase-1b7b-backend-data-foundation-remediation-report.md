# Phase 1B.7-B Payment Backend/Data Foundation Remediation Report

## Status

REMEDIATED — READY FOR UPDATED ACCEPTANCE REVIEW

## Authorization Source

Reference:
- Phase 1B.7-B backend/data acceptance review commit:
  53478395db0f1fcefcc75125c023431ceacc4f2c

## Findings Remediated

### Finding 1: ReconciliationController.Prepare Authorization Bypass — FIXED

**Problem:** The `Prepare` endpoint called `_reconciliationService.PrepareAsync()` (which mutates and persists the reconciliation period state) before checking RECONCILIATION_PREPARE permission. An unauthorized user's request would successfully mutate the period to PREPARED status before receiving 403.

**Fix:** Added read-only `GetPeriodByIdAsync` method to IReconciliationService/ReconciliationService. The controller now loads the period read-only first to obtain companyId, checks RECONCILIATION_PREPARE permission, and only then calls the mutating PrepareAsync. Unauthorized callers receive 403 with no state mutation.

### Finding 2: ReconciliationController.Confirm Authorization Bypass — FIXED

**Problem:** Same pattern as Prepare — `ConfirmAsync()` executed before RECONCILIATION_CONFIRM permission check.

**Fix:** Same approach — read-only lookup for companyId, permission check, then mutating call. Unauthorized callers receive 403 with no state mutation.

### Finding 3: PaymentTransactionController Permission Reuse — DOCUMENTED AS ACCEPTABLE

**Problem:** List, GetById, and SoftDelete endpoints check PAYMENT_CREATE_DRAFT instead of dedicated view/delete permission codes.

**Decision:** This is acceptable scope behavior, not a defect:

1. The accepted permission catalog defines exactly 6 codes. No PAYMENT_VIEW or PAYMENT_DELETE code exists in the catalog.
2. PAYMENT_CREATE_DRAFT is the base permission for cashier payment operations. A cashier who creates drafts naturally needs to list, view, and manage their own drafts.
3. SoftDelete only works on DRAFT status — the domain entity's EnsureNotConfirmed guard prevents deletion of confirmed payments regardless of permission.
4. Confirmed payment mutation is separately gated by PAYMENT_CORRECT_CONFIRMED (admin-only).
5. Adding new permission codes would exceed the accepted scope and require a new migration to seed them.

If finer-grained view/delete permissions are needed in the future, they can be added as part of a permission catalog expansion in a subsequent phase.

## Code Changes

| File | Change |
|------|--------|
| src/backend/PTKD.Api/Controllers/ReconciliationController.cs | Prepare/Confirm: permission check moved before mutation; added read-only period lookup and 404 handling |
| src/backend/PTKD.Application/PaymentManagement/Services/IReconciliationService.cs | Added GetPeriodByIdAsync method |
| src/backend/PTKD.Application/PaymentManagement/Services/ReconciliationService.cs | Implemented GetPeriodByIdAsync (read-only, AsNoTracking) |
| tests/backend/PTKD.ApiTests/ReconciliationApiTests.cs | Added 4 new tests + helper methods for period seeding/verification |

## Security Fix Evidence

Confirmed:
- Mutation no longer happens before permission check in Prepare or Confirm.
- Unauthorized Prepare returns 403 and period remains OPEN (tested: `Prepare_NoPermission_Returns403_AndDoesNotMutateState`).
- Unauthorized Confirm returns 403 and period remains PREPARED (tested: `Confirm_NoPermission_Returns403_AndDoesNotMutateState`).
- Authorized Prepare succeeds and transitions to PREPARED (tested: `Prepare_Authorized_Returns200`).
- Authorized Confirm succeeds and transitions to CONFIRMED (tested: `Confirm_Authorized_Returns200`).
- Backend authorization remains authoritative.
- Sanitized errors remain (no stack traces, no raw SQL, no sensitive payloads).

## Test Evidence

```
dotnet build src/backend/PTKD-ERP.sln
  Build succeeded. 0 errors, 0 warnings.

dotnet test tests/backend/PTKD.UnitTests/
  Passed! 219 passed, 0 failed.

dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
  Passed! 203 passed, 0 failed.

dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
  Passed! 299 passed, 0 failed. (+4 new reconciliation auth tests)

git diff --check
  No whitespace errors.
```

## Boundaries Confirmed

- No frontend implementation.
- No business docs changed.
- No Card Reprint implementation.
- No Care Package Sales implementation.
- No production migration.
- No new migrations or rollbacks (no database defect found).
- No release tag.
- No push.

## Recommended Next Gate

Recommended next authorized task:
Updated Phase 1B.7-B backend/data implementation acceptance review.

Do not authorize Project Owner backend/data implementation acceptance until the updated review passes.
