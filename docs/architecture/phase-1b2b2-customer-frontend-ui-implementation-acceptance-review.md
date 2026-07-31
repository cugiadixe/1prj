# Phase 1B.2-B2 Customer Frontend UI Implementation Acceptance Review

**Status:**
ACCEPTED — PHASE 1B.2-B2 IMPLEMENTATION ACCEPTED — SEE phase-1b2b2-project-owner-implementation-acceptance.md

**Implementation commit:**
9b9ff1900cbbf81d7fbd7a3622f5c1836a2291cd

**Parent commit:**
e3e184d697e029d10acaa4f4c34f4d4172707a31

**Accepted B2 plan commit:**
4459582ebadf8b674167af47b2a7a2964ca51599

**Project Owner B2 plan acceptance commit:**
e3e184d697e029d10acaa4f4c34f4d4172707a31

**B1 final acceptance commit:**
498991318c7e18f4a9dae11409e90a7a42abc1f4

---

## 1. Committed files

16 files changed: 1874 insertions, 0 deletions.

### Modified files (3)

| File | +/- |
|------|-----|
| src/frontend/src/App.tsx | +9 |
| src/frontend/src/components/AuthenticatedShell.test.tsx | +12 |
| src/frontend/src/components/AuthenticatedShell.tsx | +5 |

### New files (13)

| File | Lines |
|------|------:|
| src/frontend/src/customers/types.ts | 138 |
| src/frontend/src/customers/customersApi.ts | 99 |
| src/frontend/src/customers/customersApi.test.ts | 83 |
| src/frontend/src/customers/errorMessages.ts | 75 |
| src/frontend/src/customers/errorMessages.test.ts | 50 |
| src/frontend/src/customers/CustomersPage.tsx | 146 |
| src/frontend/src/customers/CustomersPage.test.tsx | 115 |
| src/frontend/src/customers/CustomerDetailPage.tsx | 192 |
| src/frontend/src/customers/CustomerDetailPage.test.tsx | 151 |
| src/frontend/src/customers/CustomerCreatePage.tsx | 250 |
| src/frontend/src/customers/CustomerCreatePage.test.tsx | 117 |
| src/frontend/src/customers/CustomerEditPage.tsx | 285 |
| src/frontend/src/customers/CustomerEditPage.test.tsx | 147 |

---

## 2. Accepted implemented scope

- Customer Frontend UI implemented.
- Customer list/search page implemented.
- Customer detail page implemented.
- Customer create page/form implemented.
- Customer edit page/form implemented.
- Routes implemented:
  - /customers
  - /customers/new
  - /customers/:customerId
  - /customers/:customerId/edit
- Customer menu/navigation entry implemented gated by CUSTOMER_VIEW_BASIC.
- Customer API client implemented (customersApi.ts) calling 8 backend endpoints.
- TypeScript interfaces (types.ts) mirror backend DTOs exactly.
- Error handling helpers implemented (errorMessages.ts).
- Duplicate warning display implemented as informational on CCCD/phone blur.
- Sensitive masking display implemented — backend-driven, frontend shows as-is with "masked" indicator.
- rowVersion/concurrency handling implemented — 409 shows refresh prompt, no silent overwrite.
- Sanitized error handling implemented — no backend stack/internal details exposed.

---

## 3. Permission gate confirmation

- CUSTOMER_VIEW_BASIC gates menu item, route access, list page, and basic detail page.
- CUSTOMER_VIEW_SENSITIVE gates sensitive field display (backend projection; frontend shows whatever backend returns with mask indicator).
- CUSTOMER_CREATE_FINAL gates create button on list page and add company context button on detail page.
- CUSTOMER_MASTER_UPDATE gates edit button on detail page and edit action on company context rows.
- Backend remains authoritative for all permission enforcement.
- Frontend gates are UX/navigation only and do not replace backend authorization.

---

## 4. Deferred scope confirmation

- No backend source files changed.
- No backend tests changed.
- No database/migration/rollback files changed.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No new permission codes added.
- No workflow/approval UI implemented.
- No customer merge UI implemented.
- No group spending UI implemented.
- No ENTITY scope UI implemented.
- No Service/Payment UI implemented.
- No export/download implemented.
- No security enhancement backlog implemented.
- No frontend-side audit implemented.
- No permission persistence in localStorage/sessionStorage/cookies.

---

## 5. Test evidence

### Frontend lint

```
npx oxlint
3 pre-existing warnings (AuthProvider.tsx, CompanyProvider.tsx fast-refresh only-export-components)
No errors. No new warnings from B2 customer module.
```

### Frontend typecheck

```
npx tsc -b --noEmit
0 errors.
```

### Frontend tests

```
npx vitest run
Test Files  25 passed (25)
     Tests  222 passed (222)
  Duration  70.60s
```

### Backend build

dotnet build skipped — no backend changes were made in this implementation commit.

---

## 6. UX behavior verification

### Sensitive masking
- Detail page displays backend-returned values as-is.
- Masked values (e.g., `****1234`, `***567`, `***`) shown with "masked" tag indicator.
- No client-side masking logic.
- No "unmask" button — permission is checked server-side.
- No sensitive values stored in localStorage/sessionStorage/cookies.
- No sensitive values logged to console.

### Duplicate warning
- Create form triggers duplicate check on CCCD and phone blur.
- Warning displayed as informational banner with match list.
- Warning is closeable.
- No merge action exposed.
- Duplicate CCCD enforced at database level returns 409 on submit — error message displayed.

### rowVersion/concurrency
- Edit form loads customer detail and stores rowVersion in component state.
- Update submits targetVersion = stored rowVersion.
- 409 with CUS_INVALID_ROW_VERSION displays error message + Refresh button.
- No silent overwrite.
- Refresh button reloads customer detail with fresh rowVersion.

### Error handling
- 403 returns sanitized permission denied message.
- 404 returns sanitized not found message.
- Known error codes mapped to user-friendly messages.
- Unknown errors return generic safe message.
- No backend stack traces, SQL text, or internal details exposed.

---

## 7. Risks and follow-up

| # | Risk | Severity |
|---|------|----------|
| 1 | Backend must continue to enforce permissions authoritatively | Medium |
| 2 | Sensitive display must remain aligned with backend projection | Medium |
| 3 | Duplicate warning must not become merge workflow without approval | Medium |
| 4 | Concurrency UX must prevent silent overwrite | Medium |
| 5 | Company context add/edit is partially wired (button disabled) — full modal implementation deferred | Low |
| 6 | Workflow/approval UI remains deferred | Medium |
| 7 | Customer merge UI remains deferred | Medium |
| 8 | Group spending UI remains deferred | Low |
| 9 | ENTITY scope UI remains deferred | Low |
| 10 | Service/Payment UI remains deferred | Low |
| 11 | Export/download remains deferred | Low |

---

## 8. Conclusion

PHASE 1B.2-B2 CUSTOMER FRONTEND UI IMPLEMENTATION ACCEPTANCE REVIEW PASSED
