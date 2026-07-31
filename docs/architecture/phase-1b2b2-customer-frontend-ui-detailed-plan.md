# Phase 1B.2-B2 Customer Frontend UI Detailed Plan

**Status:** PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Baseline:** 498991318c7e18f4a9dae11409e90a7a42abc1f4

**Authorization context:**
Planning only.
No source implementation is authorized.
No backend changes are authorized.
No frontend implementation is authorized by this task.
No database/migration/rollback changes are authorized.
No new permission codes are authorized.
Workflow/approval UI remains deferred.
Customer merge UI remains deferred.
Group spending UI remains deferred.
ENTITY scope remains deferred.
Service/Payment dependencies remain deferred.
Export/download remains deferred unless separately approved.

---

## 1. Completed backend foundation summary

Phase 1B.2-B1 is complete (final acceptance: 498991318c7e18f4a9dae11409e90a7a42abc1f4).

Available API v2 Customer endpoints:

| Method | Route | Permission | Purpose |
|--------|-------|-----------|---------|
| GET | /api/v2/customers | CUSTOMER_VIEW_BASIC | Search/list with pagination |
| GET | /api/v2/customers/{id} | CUSTOMER_VIEW_BASIC | Detail; sensitive masked without CUSTOMER_VIEW_SENSITIVE |
| POST | /api/v2/customers | CUSTOMER_CREATE_FINAL | Admin creation (Profile + Customer + optional CompanyContext) |
| PUT | /api/v2/customers/{id} | CUSTOMER_MASTER_UPDATE | Admin update; requires reason + targetVersion |
| GET | /api/v2/customers/{id}/company-contexts | CUSTOMER_VIEW_BASIC | List company contexts |
| POST | /api/v2/customers/{id}/company-contexts | CUSTOMER_CREATE_FINAL | Add company context |
| PUT | /api/v2/customers/{id}/company-contexts/{contextId} | CUSTOMER_MASTER_UPDATE | Update company context; requires targetVersion |
| GET | /api/v2/customers/duplicate-check | CUSTOMER_VIEW_BASIC | Pre-create CCCD/phone check |

Key backend behaviors:
- Sensitive fields (cccd, permanentAddress, phone, contactAddress) are masked by backend when caller lacks CUSTOMER_VIEW_SENSITIVE.
- RowVersion is exposed as Base64 string in `rowVersion` field.
- Update requires `reason` (non-empty, max 500 chars) and `targetVersion` (Base64 rowVersion).
- Concurrency conflict returns 409 with errorCode `CUS_INVALID_ROW_VERSION`.
- Duplicate CCCD returns 409 with errorCode `CUS_DUPLICATE_CCCD`.
- Duplicate customer code returns 409 with errorCode `CUS_DUPLICATE_CUSTOMER_CODE`.
- Validation errors return 400 with standard ProblemDetails.
- 401 for unauthenticated, 403 for insufficient permission.

---

## 2. Source documents reviewed

- `docs/architecture/phase-1b2a-customer-module-discovery-and-detailed-plan.md` — accepted plan
- `docs/architecture/phase-1b2a-project-owner-plan-acceptance.md` — plan approval
- `docs/architecture/phase-1b2b1-project-owner-final-acceptance.md` — B1 complete
- `docs/business/business-rules.md` — CUS-001 through CUS-009
- `docs/business/acceptance-criteria.md` — CUS-01 through CUS-07
- `docs/business/permission-catalog.md` — 7 customer permission codes (4 in first slice)

---

## 3. Existing frontend pattern discovery

### Route registration (App.tsx)
Routes are nested inside `<ProtectedRoute><AuthenticatedShell /></ProtectedRoute>`. Each route is a `<Route path="..." element={<Page />} />`. Comment annotation by phase.

### Menu registration (AuthenticatedShell.tsx)
Permission-gated menu items use `{hasPermission('CODE', 'SCOPE') && (<Menu.Item key="..." data-testid="nav-..."><Link to="...">Label</Link></Menu.Item>)}`.

### API client pattern
- Shared `axiosClient` with `baseURL` = `/api/v2`.
- Typed async functions in `<module>Api.ts`.
- DTO interfaces in `types.ts`.

### Error handling pattern
- `errorMessages.ts` maps backend errorCode strings to user-friendly messages.
- `getErrorMessage(error)` extracts errorCode from ProblemDetails extensions.
- 403 returns generic permission denied message.
- Never exposes raw SQL, stack traces, or internal error details.

### Test pattern
- Vitest + React Testing Library.
- `vi.mock` for API modules.
- `MemoryRouter` + `QueryClientProvider` wrappers.
- `data-testid` attributes for navigation gating tests.
- `hasPermission` mocked via `usePermissions` mock.

### Other patterns
- No localStorage/sessionStorage permission persistence.
- No frontend-side audit.
- React Query for data fetching.
- Ant Design components (Table, Form, Modal, Button, Input, Select).

---

## 4. Proposed frontend scope for B2

### Pages

| Page | Route | Permission gate | Description |
|------|-------|----------------|-------------|
| CustomersPage | /customers | CUSTOMER_VIEW_BASIC GLOBAL | Search/list with pagination, status filter |
| CustomerDetailPage | /customers/:customerId | CUSTOMER_VIEW_BASIC GLOBAL | Read-only detail with Profile, company contexts, masked sensitive fields |
| CustomerCreatePage | /customers/new | CUSTOMER_CREATE_FINAL GLOBAL | Admin creation form |
| CustomerEditPage | /customers/:customerId/edit | CUSTOMER_MASTER_UPDATE GLOBAL | Admin update form with reason and rowVersion |

### Capabilities

- Customer search/list with text search and status filter.
- Customer detail with Profile fields and company context list.
- Sensitive data masking display (backend-driven; frontend shows whatever backend returns).
- Customer create form with duplicate warning before submit.
- Customer edit form with reason field and rowVersion submission.
- Concurrency conflict handling (409 → refresh prompt).
- Sanitized error message display (no raw backend details).
- Company context list on detail page.
- Company context add/edit (inline or modal, within detail page).

---

## 5. Proposed route and menu

### Routes

```
/customers                         — CustomersPage (list/search)
/customers/new                     — CustomerCreatePage
/customers/:customerId             — CustomerDetailPage
/customers/:customerId/edit        — CustomerEditPage
```

### Menu item

```tsx
{hasPermission('CUSTOMER_VIEW_BASIC', 'GLOBAL') && (
  <Menu.Item key="customers" data-testid="nav-customers">
    <Link to="/customers">Customers</Link>
  </Menu.Item>
)}
```

Placement: after the existing security menu items, as a new top-level business module entry.

---

## 6. Proposed permission gates

| Gate | Permission | Scope | Controls |
|------|-----------|-------|----------|
| Menu visibility | CUSTOMER_VIEW_BASIC | GLOBAL | "Customers" menu item shown/hidden |
| Route access | CUSTOMER_VIEW_BASIC | GLOBAL | /customers and /customers/:id accessible |
| Sensitive field display | CUSTOMER_VIEW_SENSITIVE | GLOBAL | Backend returns masked/unmasked; frontend shows as-is |
| Create button/route | CUSTOMER_CREATE_FINAL | GLOBAL | "Create Customer" button visible; /customers/new accessible |
| Edit button/route | CUSTOMER_MASTER_UPDATE | GLOBAL | "Edit" button visible; /customers/:id/edit accessible |
| Company context add | CUSTOMER_CREATE_FINAL | GLOBAL | "Add Company Context" button visible |
| Company context edit | CUSTOMER_MASTER_UPDATE | GLOBAL | "Edit" action visible on company context rows |

Backend remains authoritative for all permission enforcement. Frontend gates are UX-only.

---

## 7. Proposed frontend API client structure

### File: `src/frontend/src/customers/customersApi.ts`

```typescript
// Following accountManagementApi.ts pattern
const BASE = '/customers';

searchCustomers(params) → PagedResult<CustomerListItem>
getCustomerById(id) → CustomerDetail
createCustomer(request) → CustomerDetail
updateCustomer(id, request) → CustomerDetail
getCompanyContexts(customerId) → CustomerCompanyContext[]
createCompanyContext(customerId, request) → CustomerCompanyContext
updateCompanyContext(customerId, contextId, request) → CustomerCompanyContext
checkDuplicates(params) → DuplicateCheckResult
```

### File: `src/frontend/src/customers/types.ts`

TypeScript interfaces mirroring backend DTOs:
- `CustomerListItem` (from CustomerListItemDto)
- `CustomerDetail` (from CustomerDetailDto)
- `ProfileInfo` (from ProfileDto)
- `CustomerCompanyContext` (from CustomerCompanyContextDto)
- `CreateCustomerRequest`
- `UpdateCustomerRequest`
- `CreateCompanyContextRequest`
- `UpdateCompanyContextRequest`
- `DuplicateCheckRequest`
- `DuplicateCheckResult`
- `CustomerSearchParams`
- `PagedResult<T>`

### File: `src/frontend/src/customers/errorMessages.ts`

Error code mappings:
- `CUS_INVALID_ROW_VERSION` → "This customer was modified by another user. Please refresh and try again."
- `CUS_DUPLICATE_CCCD` → "An active customer with this CCCD already exists."
- `CUS_DUPLICATE_CUSTOMER_CODE` → "This customer code is already in use."
- `CUS_CUSTOMER_NOT_FOUND` → "Customer not found."
- `CUS_COMPANY_NOT_FOUND` → "Company not found or inactive."
- `CUS_DUPLICATE_COMPANY_CONTEXT` → "Customer already has a relationship with this company."
- `CUS_CONTEXT_NOT_FOUND` → "Company context not found."

---

## 8. Proposed page/component structure

```
src/frontend/src/customers/
  customersApi.ts              — API client functions
  types.ts                     — TypeScript interfaces
  errorMessages.ts             — Error code → message mapping
  CustomersPage.tsx            — Search/list page
  CustomersPage.test.tsx       — List page tests
  CustomerDetailPage.tsx       — Detail page (read-only + company contexts)
  CustomerDetailPage.test.tsx  — Detail page tests
  CustomerCreatePage.tsx       — Admin create form
  CustomerCreatePage.test.tsx  — Create page tests
  CustomerEditPage.tsx         — Admin edit form (reason + rowVersion)
  CustomerEditPage.test.tsx    — Edit page tests
```

---

## 9. Proposed form and validation behavior

### Create form fields

| Field | Type | Required | Max length | Notes |
|-------|------|:--------:|:----------:|-------|
| customerCode | text input | Yes | 50 | |
| fullName | text input | Yes | 200 | |
| cccd | text input | No | 20 | Triggers duplicate check on blur |
| dob | date picker | No | — | |
| dobPartial | text input | No | 10 | Shown when dobPrecision is not FULL |
| dobPrecision | select | No | — | FULL, YEAR_MONTH, YEAR, UNKNOWN |
| gender | select | No | — | MALE, FEMALE, OTHER |
| permanentAddress | text area | No | 500 | |
| cccdIssueDate | date picker | No | — | |
| cccdIssuePlace | text input | No | 200 | |
| taxCode | text input | No | 20 | |
| phone | text input | No | 20 | Triggers duplicate check on blur |
| contactAddress | text area | No | 500 | |
| deathDateSolar | date picker | No | — | |
| deathDateLunar | text input | No | 20 | |
| deathPlace | text input | No | 200 | |
| hometown | text input | No | 200 | |
| initialCompanyId | company selector | No | — | Select from active companies |
| assignedStaffId | user selector | No | — | Only shown when initialCompanyId selected |
| internalNotes | text area | No | 2000 | Only shown when initialCompanyId selected |

### Edit form

Same profile fields as create, plus:
- `reason` — text input, required, max 500 chars.
- `targetVersion` — hidden field, populated from fetched customer's rowVersion.

### Frontend validation

Frontend validates field lengths and required fields before submission. Backend remains authoritative — frontend validation is UX convenience only.

---

## 10. Proposed masking behavior

- Backend returns masked values when caller lacks CUSTOMER_VIEW_SENSITIVE.
- Frontend displays whatever the backend returns — no client-side masking logic.
- Masked values appear as `****1234` (CCCD), `***567` (phone), `***` (address).
- Detail page shows a visual indicator when fields are masked (e.g., lock icon or "masked" label).
- No "unmask" button — permission is checked server-side; frontend cannot unmask.

---

## 11. Proposed duplicate detection UX

- On create form, when user enters CCCD or phone and leaves the field (blur event), call GET /api/v2/customers/duplicate-check.
- If `hasDuplicates` is true, display a warning banner listing matches (customer code, name, masked CCCD).
- Warning is informational only — user may still submit.
- Duplicate CCCD that is enforced at database level returns 409 on submit; frontend displays the error message.
- No merge action from duplicate warning — merge is deferred.

---

## 12. Proposed rowVersion/concurrency UX

- Detail page fetches customer and stores `rowVersion` in component state.
- Edit form pre-fills from detail and submits `targetVersion` = stored `rowVersion`.
- If backend returns 409 with `CUS_INVALID_ROW_VERSION`:
  - Display message: "This customer was modified by another user. Please refresh and try again."
  - Provide a "Refresh" button that reloads the customer detail.
  - Do not silently overwrite.
- Company context edit follows the same pattern with its own `rowVersion`.

---

## 13. Proposed test strategy

### Route/menu tests (AuthenticatedShell.test.tsx update)
- Customer menu item visible when CUSTOMER_VIEW_BASIC granted.
- Customer menu item hidden when CUSTOMER_VIEW_BASIC not granted.

### CustomersPage tests
- Renders search results table from mocked API.
- Search input filters via API call.
- Status filter works.
- Pagination works.
- Create button visible only with CUSTOMER_CREATE_FINAL.
- Row click navigates to detail.

### CustomerDetailPage tests
- Renders customer detail from mocked API.
- Sensitive fields displayed as returned (masked or unmasked).
- Edit button visible only with CUSTOMER_MASTER_UPDATE.
- Company contexts listed.
- Add company context button visible only with CUSTOMER_CREATE_FINAL.

### CustomerCreatePage tests
- Form renders all fields.
- Required field validation (customerCode, fullName).
- Duplicate check fires on CCCD blur.
- Duplicate warning displayed when matches found.
- Successful create navigates to detail.
- Error messages displayed from backend (duplicate CCCD, duplicate code).

### CustomerEditPage tests
- Form pre-filled from customer detail.
- Reason field required.
- RowVersion submitted as targetVersion.
- Concurrency error (409) displays refresh prompt.
- Successful update navigates to detail.

### API client tests
- Each API function calls correct endpoint.
- Parameters passed correctly.

### Error handling tests
- 403 displays permission denied.
- 409 concurrency displays refresh message.
- 409 duplicate displays duplicate message.
- 400 validation errors display field-level messages.
- Generic errors display safe message.

---

## 14. Explicit deferred items

| # | Item | Reason |
|---|------|--------|
| 1 | Workflow/approval UI | Workflow module not built |
| 2 | Customer merge UI | Deferred — complex, requires preview of cross-module data |
| 3 | Group spending UI | Requires Payment module |
| 4 | ENTITY scope UI | Not approved |
| 5 | Service module UI | Deferred |
| 6 | Payment/Reconciliation UI | Deferred |
| 7 | Export/download | Deferred unless separately approved |
| 8 | Security enhancement backlog | Deferred |
| 9 | Backend/API changes | Not authorized unless separately approved |
| 10 | Company context data isolation by user assignment | Backend enforcement deferred |

---

## 15. Risks and blockers

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Sensitive data display must align with backend masking | Medium | Frontend shows whatever backend returns; no client-side masking logic |
| 2 | Duplicate warning must not become merge workflow | Medium | Warning is informational only; no merge action |
| 3 | RowVersion handling must avoid silent overwrite | High | 409 → refresh prompt; never auto-retry with new version |
| 4 | Frontend permissions must not replace backend authorization | Medium | Frontend gates are UX-only; backend remains authoritative |
| 5 | No frontend-side audit | Low | All audit is backend-side |
| 6 | No localStorage/sessionStorage/cookie permission persistence | Low | Permissions checked via usePermissions hook only |
| 7 | Company selector in create form needs active company list | Low | Reuse existing organization API if available |
| 8 | User selector for assigned staff needs user list | Low | Reuse existing user search API if available |

---

## 16. Required Project Owner decisions

| Decision ID | Topic | Proposed decision | Alternatives |
|-------------|-------|-------------------|-------------|
| DEC-1B2B2-01 | Approve Customer Frontend UI as next implementation phase | Approve | Select alternative phase |
| DEC-1B2B2-02 | Approve route/menu placement | /customers route, top-level menu item gated by CUSTOMER_VIEW_BASIC | Nested under a "Business" submenu |
| DEC-1B2B2-03 | Approve list/detail/create/edit page scope | 4 pages as documented | Fewer pages (combine create/edit) |
| DEC-1B2B2-04 | Approve sensitive data masking UX | Display backend-returned values as-is with visual indicator | Add client-side masking logic |
| DEC-1B2B2-05 | Approve duplicate warning UX without merge | Informational warning on blur; no merge action | Skip duplicate warning |
| DEC-1B2B2-06 | Approve rowVersion/concurrency UX | 409 → refresh prompt; no silent overwrite | Auto-refresh and re-display |
| DEC-1B2B2-07 | Confirm workflow/approval UI remains deferred | Deferred | Include workflow stub |
| DEC-1B2B2-08 | Confirm customer merge UI remains deferred | Deferred | Include merge preview |
| DEC-1B2B2-09 | Confirm group spending UI remains deferred | Deferred | Include spending section |
| DEC-1B2B2-10 | Confirm ENTITY scope remains deferred | Deferred | Introduce ENTITY |
| DEC-1B2B2-11 | Confirm Service/Payment UI remains deferred | Deferred | Include service linkage |
| DEC-1B2B2-12 | Confirm export/download remains deferred | Deferred | Include export |
| DEC-1B2B2-13 | Approve permission gates using existing customer permission codes | CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE | Different mapping |
| DEC-1B2B2-14 | Confirm no new permission codes | No new codes | Add codes |
| DEC-1B2B2-15 | Approve frontend test strategy | Vitest + RTL, mocked API, permission gate tests | Different strategy |
| DEC-1B2B2-16 | Confirm no implementation until plan acceptance | No implementation until accepted | Begin immediately |

---

## 17. Implementation phase recommendation

After Project Owner plan acceptance, implement as:

**Phase 1B.2-B2 — Customer Frontend UI Implementation**

Recommended implementation order:
1. API client (customersApi.ts, types.ts, errorMessages.ts).
2. CustomersPage (list/search) + tests.
3. CustomerDetailPage (detail + company contexts) + tests.
4. CustomerCreatePage (create form + duplicate warning) + tests.
5. CustomerEditPage (edit form + reason + rowVersion) + tests.
6. Route registration in App.tsx.
7. Menu item in AuthenticatedShell.tsx + navigation gate tests.

---

## 18. Authorization statement

No source implementation is authorized by this plan until Project Owner plan acceptance.

---

## 19. Conclusion

PHASE 1B.2-B2 CUSTOMER FRONTEND UI DETAILED PLAN READY FOR PROJECT OWNER REVIEW
