# Phase 1B.6-C Service Module Foundation Frontend Scope and Implementation Plan

## Status

DRAFT — PENDING PROJECT OWNER FRONTEND SCOPE ACCEPTANCE

## Authorization

- Phase 1B.6-B PO backend/data implementation acceptance: `d93ee668` (ACCEPTED)
- Phase 1B.6-B backend/data implementation commit: `4c49ab00`
- Phase 1B.6 scope acceptance commit: `73a60068`

This document is planning only. Frontend implementation requires separate Project Owner frontend scope acceptance.

## Objective

Define the frontend scope for the Service Module Foundation, covering API client layer, TypeScript types, pages, components, routes, navigation, permission guards, error handling, and tests. This plan maps directly to the backend contract delivered in Phase 1B.6-B.

## Source Documents

- `docs/architecture/phase-1b6b-project-owner-backend-data-implementation-acceptance.md` — Accepted backend scope
- `docs/architecture/phase-1b6b-backend-data-foundation-implementation-report.md` — Implementation report
- `docs/architecture/phase-1b6b-backend-data-foundation-implementation-acceptance-review.md` — Acceptance review
- `docs/architecture/phase-1b6-service-module-foundation-discovery-and-detailed-plan.md` — Discovery plan
- `docs/architecture/phase-1b6-project-owner-scope-acceptance.md` — Phase 1B.6 scope acceptance

## Backend Contract Summary

### Service Type Endpoints (api/v2/service-types)

All endpoints require `SERVICE_TYPE_MANAGE` (GLOBAL).

| Method | Path | Request | Response |
|--------|------|---------|----------|
| GET | `/service-types?page&pageSize` | query params | `PagedResult<ServiceTypeDto>` |
| GET | `/service-types/{id}` | — | `ServiceTypeDto` |
| POST | `/service-types` | `CreateServiceTypeRequest` | `ServiceTypeDto` (201) |
| PUT | `/service-types/{id}` | `UpdateServiceTypeRequest` | `ServiceTypeDto` |
| POST | `/service-types/{id}/deactivate` | `{ rowVersion }` | `ServiceTypeDto` |

### Service Endpoints (api/v2/services)

| Method | Path | Permission | Request | Response |
|--------|------|-----------|---------|----------|
| GET | `/services?companyId&customerId&status&page&pageSize` | SERVICE_VIEW (COMPANY) | query params | `PagedResult<ServiceDto>` |
| GET | `/services/{id}` | SERVICE_VIEW (COMPANY) | — | `ServiceDto` |
| POST | `/services` | SERVICE_CREATE_STANDARD (COMPANY) | `CreateServiceRequest` | `ServiceDto` (201) |
| POST | `/services/{id}/renew` | SERVICE_RENEW_STANDARD (COMPANY) | `RenewServiceRequest` | `ServiceDto` (201) |
| POST | `/services/{id}/request-price-override` | SERVICE_PRICE_OVERRIDE_REQUEST (COMPANY) | `RequestPriceOverrideRequest` | 200 |

### Backend DTOs (camelCase in JSON)

**ServiceTypeDto**: `id`, `code`, `name`, `description`, `standardPrice`, `standardPriceCurrency`, `cycleDurationMonths`, `isActive`, `createdAt`, `updatedAt`, `rowVersion`.

**ServiceDto**: `id`, `serviceTypeId`, `serviceTypeCode`, `serviceTypeName`, `customerId`, `companyId`, `status`, `appliedPrice`, `standardPriceSnapshot`, `isOverridePrice`, `overrideApprovalRequestId`, `validFrom`, `validTo`, `cycleNumber`, `previousServiceId`, `createdAt`, `updatedAt`, `rowVersion`.

### Sanitized Error Responses

- 400 Bad Request: `{ title, detail }` for validation/business rule violations
- 403 Forbidden: `Forbid()` (no body)
- 404 Not Found: `{ title, detail }`
- 409 Conflict: `{ title, detail }` for rowversion concurrency

### Permission Summary

| Permission Code | DataScope | Usage |
|---|---|---|
| SERVICE_TYPE_MANAGE | GLOBAL | All service type CRUD |
| SERVICE_VIEW | COMPANY | List/get services (requires companyId) |
| SERVICE_CREATE_STANDARD | COMPANY | Create standard service |
| SERVICE_RENEW_STANDARD | COMPANY | Renew standard service |
| SERVICE_PRICE_OVERRIDE_REQUEST | COMPANY | Request price override |
| SERVICE_PRICE_OVERRIDE_APPROVE | COMPANY | Approve price override (workflow) |

## Frontend Scope Decision

### In Scope (Phase 1B.6-C)

#### A. API Client and Types

**New file: `src/frontend/src/services/types.ts`**

TypeScript interfaces mirroring backend DTOs:

- `ServiceTypeListItem` — mirrors `ServiceTypeDto`
- `ServiceTypeDetail` — mirrors `ServiceTypeDto` (same shape, separate type for future extensibility)
- `CreateServiceTypeRequest`
- `UpdateServiceTypeRequest`
- `ServiceListItem` — mirrors `ServiceDto`
- `ServiceDetail` — mirrors `ServiceDto`
- `CreateServiceRequest`
- `RenewServiceRequest`
- `RequestPriceOverrideRequest`
- `ServiceSearchParams` — `{ companyId: number; customerId?: number; status?: string; page?: number; pageSize?: number }`
- `ServiceTypeSearchParams` — `{ page?: number; pageSize?: number }`

Reuse existing `PagedResult<T>` from `src/frontend/src/customers/types.ts` (extract to shared location or re-export).

**New file: `src/frontend/src/services/serviceTypesApi.ts`**

Pattern: follow `customersApi.ts` — import `axiosClient`, typed async functions, return `data` from response.

Functions:
- `searchServiceTypes(params?: ServiceTypeSearchParams): Promise<PagedResult<ServiceTypeListItem>>`
- `getServiceTypeById(id: number): Promise<ServiceTypeDetail>`
- `createServiceType(request: CreateServiceTypeRequest): Promise<ServiceTypeDetail>`
- `updateServiceType(id: number, request: UpdateServiceTypeRequest): Promise<ServiceTypeDetail>`
- `deactivateServiceType(id: number, rowVersion: string): Promise<ServiceTypeDetail>`

**New file: `src/frontend/src/services/servicesApi.ts`**

Functions:
- `searchServices(params: ServiceSearchParams): Promise<PagedResult<ServiceListItem>>`
- `getServiceById(id: number): Promise<ServiceDetail>`
- `createService(request: CreateServiceRequest): Promise<ServiceDetail>`
- `renewService(id: number, request: RenewServiceRequest): Promise<ServiceDetail>`
- `requestPriceOverride(id: number, request: RequestPriceOverrideRequest): Promise<void>`

**New file: `src/frontend/src/services/errorMessages.ts`**

Pattern: follow `customers/errorMessages.ts`. Map service-specific error codes to user-facing messages.

Error codes to map:
- `SVC_TYPE_DUPLICATE_CODE` — "A service type with this code already exists."
- `SVC_TYPE_NOT_FOUND` — "Service type not found."
- `SVC_NOT_FOUND` — "Service not found."
- `SVC_INVALID_STATUS` — "Service is not in a valid status for this operation."
- `SVC_CUSTOMER_NOT_FOUND` — "Customer not found."
- `SVC_COMPANY_NOT_FOUND` — "Company not found."
- `SVC_CONTEXT_NOT_FOUND` — "Customer does not have a relationship with this company."
- `SVC_CONCURRENCY` — "This record was modified by another user. Please refresh and try again."

Note: Exact error codes depend on backend `InvalidOperationException` messages. These are the anticipated codes; verify during implementation by testing actual API responses.

#### B. Pages and Components

**Service Type Management (GLOBAL admin)**

1. `ServiceTypesPage.tsx` — List page with table, pagination. Columns: Code, Name, Standard Price, Cycle (months), Active status, Actions. "Create" button. Filter by active/inactive.
2. `ServiceTypeCreatePage.tsx` — Form: Code, Name, Description, Standard Price, Cycle Duration. Submit → POST, navigate to list on success.
3. `ServiceTypeDetailPage.tsx` — Read-only detail view with Edit and Deactivate actions.
4. `ServiceTypeEditPage.tsx` — Edit form (Name, Description, Cycle Duration — Code and Price not editable via Update). Concurrency via rowVersion.

**Service Management (company-scoped)**

5. `ServicesPage.tsx` — List page filtered by current company (from `useCompany().currentCompanyId`). Columns: Service Type, Customer, Status, Applied Price, Valid From, Valid To, Cycle #. Optional filters: status, customerId.
6. `ServiceDetailPage.tsx` — Read-only detail with Renew and Request Price Override actions (permission-gated).
7. `ServiceCreatePage.tsx` — Form: Service Type (dropdown from active types), Customer (search/select), Valid From, Valid To (optional). CompanyId from current company context.
8. `ServiceRenewDialog.tsx` — Modal/dialog for renewal: Valid From, Valid To (optional). Triggered from ServiceDetailPage.
9. `ServicePriceOverrideDialog.tsx` — Modal/dialog for price override request: Requested Price, Reason. Triggered from ServiceDetailPage.

#### C. Routes and Navigation

**New routes in App.tsx** (inside AuthenticatedShell):

```
services/types                          → ServiceTypesPage
services/types/new                      → ServiceTypeCreatePage
services/types/:serviceTypeId           → ServiceTypeDetailPage
services/types/:serviceTypeId/edit      → ServiceTypeEditPage
services                                → ServicesPage
services/new                            → ServiceCreatePage
services/:serviceId                     → ServiceDetailPage
```

**New navigation items in AuthenticatedShell.tsx**:

```tsx
{hasPermission('SERVICE_TYPE_MANAGE', 'GLOBAL') && (
  <Menu.Item key="service-types" data-testid="nav-service-types">
    <Link to="/services/types">Service Types</Link>
  </Menu.Item>
)}
{hasPermission('SERVICE_VIEW', 'COMPANY') && (
  <Menu.Item key="services" data-testid="nav-services">
    <Link to="/services">Services</Link>
  </Menu.Item>
)}
```

Note: SERVICE_VIEW is COMPANY-scoped. The `hasPermission` call must check against the current company context. Verify `usePermissions().hasPermission` signature supports company-scoped checks, or extend if needed.

#### D. Permission Strategy

- Service Type pages: guard with `SERVICE_TYPE_MANAGE` (GLOBAL). Use `ProtectedRoute` or inline `hasPermission` check consistent with existing patterns.
- Service list/detail: guard with `SERVICE_VIEW` (COMPANY) against `currentCompanyId`.
- Service create button/page: guard with `SERVICE_CREATE_STANDARD` (COMPANY).
- Renew action: guard with `SERVICE_RENEW_STANDARD` (COMPANY). Only show on ACTIVE services.
- Price override action: guard with `SERVICE_PRICE_OVERRIDE_REQUEST` (COMPANY). Only show on ACTIVE services.
- SERVICE_PRICE_OVERRIDE_APPROVE: no frontend UI in this phase (handled by workflow approval UI).

#### E. Error Handling Strategy

Follow `customers/errorMessages.ts` pattern:
- `getErrorMessage(error)` maps status codes and error codes to user messages.
- `isPermissionDenied(error)` checks for 403.
- `isConcurrencyError(error)` checks for 409.
- Display errors via Ant Design `Alert` or `message` components, consistent with customer pages.
- Concurrency errors: prompt user to refresh.

#### F. Test Strategy

**API client tests** (pattern: `customersApi.test.ts`):
- `serviceTypesApi.test.ts` — mock axiosClient, verify each function calls correct endpoint with correct params.
- `servicesApi.test.ts` — same pattern.
- `errorMessages.test.ts` — test error code mapping, permission denied, concurrency.

**Page tests** (pattern: `CustomersPage.test.tsx`):
- `ServiceTypesPage.test.tsx` — renders table, handles loading, handles permission denied.
- `ServiceTypeCreatePage.test.tsx` — form submission, validation.
- `ServiceTypeDetailPage.test.tsx` — renders detail, deactivate action.
- `ServiceTypeEditPage.test.tsx` — form pre-population, update, concurrency error.
- `ServicesPage.test.tsx` — renders table with company filter, handles permission denied.
- `ServiceDetailPage.test.tsx` — renders detail, renew/override actions gated by permission.
- `ServiceCreatePage.test.tsx` — form with service type dropdown, customer selection.

Testing approach:
- Use `vitest` + `@testing-library/react`.
- Mock API functions with `vi.mock`.
- Mock `useAuth`/`usePermissions`/`useCompany` providers.
- Use `@tanstack/react-query` test utilities (`QueryClientProvider` wrapper).

## Implementation Sequence

1. **Types and API client** — `services/types.ts`, `services/serviceTypesApi.ts`, `services/servicesApi.ts`, `services/errorMessages.ts`
2. **API client tests** — `services/serviceTypesApi.test.ts`, `services/servicesApi.test.ts`, `services/errorMessages.test.ts`
3. **Service Type pages** — ServiceTypesPage, ServiceTypeCreatePage, ServiceTypeDetailPage, ServiceTypeEditPage
4. **Service Type page tests**
5. **Service pages** — ServicesPage, ServiceDetailPage, ServiceCreatePage, ServiceRenewDialog, ServicePriceOverrideDialog
6. **Service page tests**
7. **Routes and navigation** — App.tsx routes, AuthenticatedShell.tsx nav items
8. **Integration verification** — dev server, manual testing of golden paths

## Risks and Open Questions

### Open Questions

- **OQ-1B6C-001 (PagedResult reuse)**: `PagedResult<T>` is currently defined in `customers/types.ts`. Should it be extracted to a shared types file (e.g., `src/frontend/src/types/common.ts`)? Recommendation: extract during implementation to avoid cross-module import from `customers/`.

- **OQ-1B6C-002 (Company-scoped permission check)**: Verify that `usePermissions().hasPermission(code, scope)` supports COMPANY-scoped checks with the current company context, or whether a `hasCompanyPermission(code, companyId)` variant is needed. The existing navigation uses `hasPermission('CUSTOMER_VIEW_BASIC', 'GLOBAL')` — all current nav checks are GLOBAL. Service module introduces the first COMPANY-scoped nav guard.

- **OQ-1B6C-003 (Error code alignment)**: Backend controllers catch `InvalidOperationException` and return `BadRequest(new { Title, Detail })`. The exact error code mechanism (e.g., `extensions.errorCode` as used by customer endpoints) needs verification — the service controllers may return plain `{ title, detail }` without an `errorCode` extension. Implementation should adapt `errorMessages.ts` to handle both patterns.

- **OQ-1B6C-004 (Customer/Company selectors)**: ServiceCreatePage needs a customer selector and uses current company from `useCompany()`. Determine whether to reuse existing customer search components or build service-specific selectors.

### Risks

- **R-1B6C-001 (Nav menu size)**: AuthenticatedShell Menu is already large (15+ items). Adding 2 more items increases clutter. Consider grouping under a submenu or sidebar in a future UI phase.

- **R-1B6C-002 (Price display)**: StandardPrice and AppliedPrice are `decimal` — frontend must handle VND formatting (no decimal places, thousands separator). Verify consistent formatting approach.

- **R-1B6C-003 (Company context dependency)**: Service pages require `currentCompanyId` from `useCompany()`. If no company is selected, service pages must show a clear message rather than failing silently.

## Recommended Next Gate

Project Owner frontend scope acceptance of this plan before implementation begins.

After acceptance, implementation proceeds as Phase 1B.6-C frontend implementation (separate commit).

## Non-Goals

- Frontend implementation (this is planning only).
- Payment UI.
- Billing/collection/reconciliation UI.
- Card Reprint UI.
- Care Package Sales UI.
- Service type price change UI (price is set at creation; SetStandardPrice is a backend-only admin operation for now).
- SERVICE_PRICE_OVERRIDE_APPROVE UI (handled by existing workflow approval pages).
- Production deployment.
