# Phase 1B.9-A Project Owner Blocker Decision Response — Care Package Sales

## Status

DECISIONS RECORDED — READY FOR PHASE 1B.9-A DETAILED SCOPE UPDATE

## Project Owner Decision

The Project Owner provides the blocker decisions required to unblock Phase 1B.9-A Care Package Sales detailed scope.

This response resolves the implementation-critical blockers identified in the blocked detailed scope document.

This response authorizes only the next documentation task:
Phase 1B.9-A detailed scope update.

This response does not authorize implementation, source code changes, database migrations, permission catalog changes, production migration, release tag, or push.

## Decision Basis

- Phase 1B.9-A blocked detailed scope commit:
  6ac4e7296d53c753be499250674b5a28e909cb3e

- Phase 1B.9 Project Owner scope acceptance commit:
  f5e61a09718d55aa9d9287e6d88b4ff35a9adfc7

- Phase 1B.9 discovery/scope plan commit:
  2c9c6955474d899ef7274d880c7ce140f48a32f9

## Resolved Decisions

| ID | Topic | Project Owner Decision | Implementation Impact | Status |
| :--- | :--- | :--- | :--- | :--- |
| OD-1B9-001 | Care Package terminology | Use "Care Package Sales" / "Gói chăm sóc" for process/package. Use "Care Package Request" for sale record and "Care Package Request Item" for targets. | Terminology standardization across UI/API. | DECIDED |
| OD-1B9-002 | Sale unit | Sale unit is cốt-year. One request has items targeting grave/card/care target with confirmed cốt count. Customer and Company required at request level. | Data schema mapping and validation logic. | DECIDED |
| OD-1B9-003 | Package duration | Supports one-year (12 months) packages only. Start/end dates required per item. Renewals create new one-year requests. | Lifecycle calculation and period validation. | DECIDED |
| OD-1B9-004 | Pricing source | Use Service Foundation effective-date pricing (e.g. CARE_PACKAGE service code). Must fail safely if active price is missing. | Price fetching logic. | DECIDED |
| OD-1B9-005 | Price calculation | Line subtotal = unit price per cốt per year × cốt count × 1 year. Request total = sum of line subtotals - approved discount. | Payment draft calculation and storage. | DECIDED |
| OD-1B9-006 | Price changes | Use effective-date pricing at sale date. Snapshots must be stored. Price changes do not rewrite historical requests/bills. | Snapshot persistence. | DECIDED |
| OD-1B9-007 | Renewal rule | New request for next 1-year period. Does not modify prior record. Prevents overlap. No approval if price unchanged/no discount. | Status transition and duplicate prevention. | DECIDED |
| OD-1B9-008 | Approval trigger | Approval required if discount applied, price overridden, renewal price changed, or per configured rule. Uses SELL_CARE_PACKAGE workflow. | Workflow handler execution rules. | DECIDED |
| OD-1B9-009 | Discount behavior | Discount allowed only with approval, stored in VND, reason required, cannot push total < zero. | UI fields and validation limits. | DECIDED |
| OD-1B9-010 | Payment timing | Payment draft created only when payment-eligible (no approval needed, or approved). Must be confirmed before active status. | Lifecycle integration. | DECIDED |
| OD-1B9-011 | Payment constraints | Use Payment Foundation: full payment, VND only, no partial/refund/cancellation. Payment correction via existing admin rules. | Payment boundary enforcement. | DECIDED |
| OD-1B9-012 | Reconciliation/reporting | Participate in existing manual reconciliation. Preserve data for reporting (company, customer, snapshot, discount, total, status). | Data retention mapping. | DECIDED |
| OD-1B9-013 | Permissions | Define COMPANY-scoped permissions: CARE_PACKAGE_VIEW/CREATE/APPROVE/REJECT/CREATE_PAYMENT/REPORT_VIEW. Reuse PAYMENT_CONFIRM. | Application policies. | DECIDED |
| OD-1B9-014 | Frontend scope | List/Create/Detail pages. Permission-gated actions. Backend-calculated totals only. | React UI component map. | DECIDED |
| OD-1B9-015 | Data model impact | Core tables: CarePackageRequests, CarePackageRequestItems. Fields for snapshots, totals, and FK links to Customer/Company/Workflow. | EF Core schema design. | DECIDED |
| OD-1B9-016 | Migration/rollback needs | New migration/rollback pair required post-implementation planning, following V0013/U0013 sequence. | DB scripts required later. | DECIDED |
| OD-1B9-017 | Acceptance criteria | Strict matrix including backend/frontend builds, E2E validation for approvals, pricing overlaps, missing prices, and no-partial rules. | Verification checklist. | DECIDED |
| OD-1B9-018 | Out-of-scope boundaries | Refunds, cancellation, partial payment, dynamic PDF, multi/partial-year packages, discount % UI out of scope. | Scope constraint. | DECIDED |

## Updated Business Rules for Detailed Scope

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

## Authorization for Next Step

Authorized next task:
Phase 1B.9-A Care Package Sales detailed scope update only.

The next task may create only an updated detailed scope document using these Project Owner decisions.

The next task must produce:

docs/architecture/phase-1b9a-care-package-sales-updated-detailed-scope.md

The next task must:
- incorporate these 18 Project Owner decisions.
- convert blocker decisions into confirmed detailed scope.
- define confirmed business scope.
- define accepted exclusions.
- define candidate backend/data scope.
- define candidate API scope.
- define candidate workflow/payment scope.
- define candidate frontend scope.
- define candidate permission model.
- define candidate validation approach.
- recommend whether implementation planning can proceed.

Do not authorize:
- Care Package Sales implementation,
- source code changes,
- backend implementation,
- frontend implementation,
- database migrations,
- business docs changes,
- permission catalog changes,
- production migration,
- release tag,
- push,
- dynamic PDF/template generation,
- generic Payment Print UI,
- refund,
- cancellation,
- partial payment,
- physical inventory/stamp stock management.

## Non-Goals

This response does not:
- implement code.
- modify source code.
- modify tests.
- modify frontend/backend files.
- create migrations/rollbacks.
- modify business docs.
- modify permission catalog.
- run production migration.
- create release tag.
- push.

## Notes

- Phase 1B.9 implementation has not started.
- next task is documentation-only detailed scope update.
- local branch may be ahead of origin; no push is authorized.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
