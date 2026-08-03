# Phase 1B.5-C Customer Merge Frontend Scope and Implementation Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE FRONTEND IMPLEMENTATION

## Authorization Source

Reference:
- Phase 1B.5-B PO backend/data implementation acceptance commit:
  51c94646c2122df20f739dee9de4afe93805cc83

State:
- This document is frontend scope and implementation planning only.
- It does not authorize frontend implementation.

## Objective

Define the frontend scope needed to expose the accepted Phase 1B.5-B Customer Merge backend/data foundation capability, including duplicate search, merge request creation, merge request management, and workflow integration.

## Source Documents Reviewed

- docs/architecture/phase-1b5b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b5b-backend-data-foundation-implementation-acceptance-review.md
- docs/architecture/phase-1b5b-backend-data-foundation-implementation-report.md
- docs/architecture/phase-1b5b-project-owner-backend-data-scope-acceptance.md
- docs/architecture/phase-1b5b-backend-data-foundation-scope-and-implementation-plan.md
- docs/architecture/phase-1b5-project-owner-plan-acceptance.md
- docs/architecture/phase-1b5-customer-merge-duplicate-resolution-discovery-and-detailed-plan.md
- docs/architecture/phase-1b4c-frontend-scope-and-implementation-plan.md (frontend pattern reference)
- docs/architecture/phase-1b4c-frontend-implementation-acceptance-review.md (frontend pattern reference)
- src/backend/PTKD.Api/Controllers/CustomerMergeController.cs (API contract)
- src/backend/PTKD.Application/Customers/DTOs/CustomerMergeDtos.cs (DTO contract)
- src/frontend/src/customers/ (existing customer frontend structure)
- src/frontend/src/workflow/ (existing workflow frontend structure)
- src/frontend/src/App.tsx (route definitions)
- src/frontend/src/components/AuthenticatedShell.tsx (navigation structure)
- src/frontend/src/auth/AuthProvider.tsx (usePermissions hook)
- src/frontend/src/api/axiosClient.ts (HTTP client)

## Accepted Backend Foundation Summary

- CustomerMergeController exists at `/api/v2/customers` with 4 endpoints.
- CustomerMergeDtos exist (CreateCustomerMergeRequestDto, CustomerMergeRequestDto, CustomerMergeCandidateDto).
- Backend API v2 endpoints:
  - `GET /api/v2/customers/duplicates?cccd=&phone=` — duplicate candidate search.
  - `POST /api/v2/customers/merge-requests` — create merge request.
  - `GET /api/v2/customers/merge-requests/{id}` — merge request detail.
  - `GET /api/v2/customers/merge-requests?page=&pageSize=` — list/search merge requests.
- V0010/U0010 migration and rollback accepted.
- Permissions accepted: CUSTOMER_MERGE_REQUEST_CREATE, CUSTOMER_MERGE_REQUEST_VIEW, CUSTOMER_MERGE_REQUEST_ADMIN_VIEW, CUSTOMER_MERGE_EXECUTE.
- Backend tests passed: 158 unit, 196 integration, 267 API.
- Frontend Customer Merge UI remains future work — this plan defines that scope.

## Confirmed Existing Frontend Foundation

- Customer pages exist: CustomersPage, CustomerDetailPage, CustomerCreatePage, CustomerEditPage.
- Customer proposal pages exist: CustomerProposalCreatePage, CustomerMyProposalsPage, CustomerProposalDetailPage.
- Customer master change pages exist: CustomerMasterChangeRequestForm, CustomerMasterChangeRequestsPage, CustomerMasterChangeRequestDetailPage.
- API client pattern: module-specific API files in `src/frontend/src/customers/` using `axiosClient`.
- Type pattern: module-specific `*Types.ts` files alongside API files.
- Error message pattern: `errorMessages.ts` for sanitized error mapping.
- Permission hook: `usePermissions()` returns `{ permissions, hasPermission(code, scope?, companyId?) }`.
- Workflow runtime API exists at `src/frontend/src/workflow/workflowRuntimeApi.ts` with 11 exported functions.
- Existing workflow pages: WorkflowMyApprovalsPage, WorkflowMyRequestsPage, WorkflowInstanceDetailPage.
- Navigation: AuthenticatedShell uses Ant Design Menu with permission-gated items via `hasPermission()`.
- Routes: defined in App.tsx inside AuthenticatedShell wrapper.

## Proposed Frontend Scope

### A. Frontend API Client

New file: `src/frontend/src/customers/customerMergeApi.ts`

| Function | Backend Endpoint | Request Type | Response Type | Permission |
|---|---|---|---|---|
| `findDuplicates(params)` | `GET /api/v2/customers/duplicates` | `{ cccd?: string; phone?: string }` | `DuplicateCheckResult[]` | CUSTOMER_MERGE_REQUEST_CREATE |
| `createMergeRequest(request)` | `POST /api/v2/customers/merge-requests` | `CreateCustomerMergeRequest` | `CustomerMergeRequest` | CUSTOMER_MERGE_REQUEST_CREATE |
| `getMergeRequest(id)` | `GET /api/v2/customers/merge-requests/{id}` | `string` (GUID) | `CustomerMergeRequest` | CUSTOMER_MERGE_REQUEST_VIEW / _ADMIN_VIEW |
| `listMergeRequests(page, pageSize)` | `GET /api/v2/customers/merge-requests` | `{ page: number; pageSize: number }` | `PagedResult<CustomerMergeRequest>` | CUSTOMER_MERGE_REQUEST_VIEW / _ADMIN_VIEW |

Error handling:
- 400 → parse `Detail` field for validation errors (source == target, already merged, overlapping company context, invalid target).
- 403 → permission denied, display sanitized message.
- 404 → not found.
- 409 → concurrency/stale rowversion conflict.
- Other → generic sanitized failure.

### B. TypeScript Types

New file: `src/frontend/src/customers/customerMergeTypes.ts`

```typescript
interface CreateCustomerMergeRequest {
  sourceCustomerId: number;
  targetCustomerId: number;
  survivorshipPayload: string;
  sourceRowVersionSnapshot: string;
  targetRowVersionSnapshot: string;
  candidates: CustomerMergeCandidate[];
}

interface CustomerMergeCandidate {
  candidateCustomerId: number;
  matchType: string;
  matchConfidence: number | null;
  snapshotPayload: string | null;
}

interface CustomerMergeRequest {
  id: string;
  sourceCustomerId: number;
  targetCustomerId: number;
  requesterId: number;
  requestStatus: string;
  survivorshipPayload: string;
  sourceRowVersionSnapshot: string;
  targetRowVersionSnapshot: string;
  workflowInstanceId: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
  candidates: CustomerMergeCandidate[];
}

interface DuplicateCheckResult {
  customerId: number;
  matchType: string;
  matchConfidence: number | null;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

### C. Screens / Pages

| Page | File | Purpose |
|---|---|---|
| Duplicate Search | `CustomerMergeDuplicateSearchPage.tsx` | Search for duplicate candidates by CCCD or phone |
| Merge Request Form | `CustomerMergeRequestCreatePage.tsx` | Select source/target, review survivorship, submit merge request |
| My Merge Requests | `CustomerMergeRequestsPage.tsx` | List merge requests with status, pagination, link to detail |
| Merge Request Detail | `CustomerMergeRequestDetailPage.tsx` | View merge request details, status, candidates, workflow link |

All pages placed in `src/frontend/src/customers/`.

### D. Navigation and Routing

Proposed routes (added to App.tsx):

| Route | Component | Purpose |
|---|---|---|
| `/customers/merge/search` | CustomerMergeDuplicateSearchPage | Duplicate candidate search |
| `/customers/merge/new` | CustomerMergeRequestCreatePage | Create merge request |
| `/customers/merge-requests` | CustomerMergeRequestsPage | List merge requests |
| `/customers/merge-requests/:id` | CustomerMergeRequestDetailPage | Merge request detail |

Navigation entries (added to AuthenticatedShell.tsx):

| Menu Item | Route | Permission Guard |
|---|---|---|
| Merge Requests | `/customers/merge-requests` | `CUSTOMER_MERGE_REQUEST_VIEW` / `GLOBAL` |
| Find Duplicates | `/customers/merge/search` | `CUSTOMER_MERGE_REQUEST_CREATE` / `GLOBAL` |

Customer detail entry point:
- Optional "Find Duplicates" or "Merge" action button on CustomerDetailPage, gated by CUSTOMER_MERGE_REQUEST_CREATE permission. Implementation detail to be determined.

### E. Permissions

Frontend permission checks (convenience only — backend is authoritative):

| Frontend Gate | Permission Code | Scope | Usage |
|---|---|---|---|
| Show "Find Duplicates" nav/button | CUSTOMER_MERGE_REQUEST_CREATE | GLOBAL | Navigation visibility, search page access |
| Show "Merge Requests" nav | CUSTOMER_MERGE_REQUEST_VIEW | GLOBAL | Navigation visibility |
| Show merge request list page | CUSTOMER_MERGE_REQUEST_VIEW or CUSTOMER_MERGE_REQUEST_ADMIN_VIEW | GLOBAL | Page access |
| Show merge request detail | CUSTOMER_MERGE_REQUEST_VIEW or CUSTOMER_MERGE_REQUEST_ADMIN_VIEW | GLOBAL | Page access |
| Submit merge request form | CUSTOMER_MERGE_REQUEST_CREATE | GLOBAL | Form submit button |

No new permissions are introduced. All codes are already seeded in V0010.

### F. Error Handling

| Error Scenario | Backend Response | Frontend Display |
|---|---|---|
| Source equals target | 400, Detail: "Source and target customer cannot be the same." | Inline form validation error |
| Source already merged | 400, Detail: "Cannot merge a customer that is already merged." | Alert with clear message |
| Target not active | 400, Detail: "Target customer must be active." | Alert with clear message |
| Overlapping company context | 400, Detail: "Cannot automatically merge overlapping company contexts. Manual resolution required." | Alert with specific conflict message |
| Customer not found | 400, Detail: "One or both customers not found." | Alert with message |
| Stale rowversion | 400 or 409 concurrency | Alert: "Data has changed since you started. Please refresh and try again." |
| Permission denied | 403 | Redirect or "You do not have permission" message |
| Merge request not found | 404 | "Merge request not found" with back link |
| Generic server error | 500 | "An unexpected error occurred. Please try again." — no raw details |

All error messages sanitized. No raw JSON payload, SQL, stack traces, or internal exception details displayed.

New file: `src/frontend/src/customers/customerMergeErrorMessages.ts` — maps backend error Detail strings to user-facing messages.

### G. Tests

New test files:

| Test File | Coverage |
|---|---|
| `customerMergeApi.test.ts` | API client functions, error mapping, response parsing |
| `CustomerMergeDuplicateSearchPage.test.tsx` | Search form, result list, empty/loading/error states, permission gating |
| `CustomerMergeRequestCreatePage.test.tsx` | Form validation (source == target), submit success, error display, rowversion transport |
| `CustomerMergeRequestsPage.test.tsx` | List rendering, pagination, status display, empty/loading/error states |
| `CustomerMergeRequestDetailPage.test.tsx` | Detail rendering, status display, candidate list, workflow link, not-found state |
| `customerMergeErrorMessages.test.ts` | Error message mapping, no raw internal error leakage |

Testing approach:
- Vitest + React Testing Library (RTL), consistent with existing test patterns.
- Mock axiosClient for API tests.
- Mock usePermissions for permission gating tests.
- Mock useAuth for authenticated context.
- Verify no raw payload/sensitive data rendered.
- Verify sanitized error messages only.

## Proposed UI Flow

1. **Search duplicates**: User navigates to `/customers/merge/search`. Enters CCCD or phone number. Submits search. Backend returns candidate list.
2. **Review candidates**: Candidate list displays customer IDs, match type, and confidence. User selects a candidate as the source customer to merge.
3. **Create merge request**: User navigates to `/customers/merge/new` (or is redirected with pre-selected source/target). Reviews source vs target customer comparison. Reviews survivorship information. Frontend captures current rowversions for both customers. User submits merge request.
4. **Request enters workflow**: Backend creates merge request in DRAFT status. If workflow binding exists, request enters approval workflow. Status transitions to SUBMITTED.
5. **View merge requests**: User navigates to `/customers/merge-requests` to see their requests with current status. Pagination supported.
6. **View request detail**: User clicks a request to see full details including candidates, status, and workflow instance link. If workflow instance exists, user can navigate to existing workflow instance detail page (`/workflow/instances/{instanceId}`).
7. **Status display**: DRAFT, SUBMITTED, APPROVED, EXECUTED, REJECTED, WITHDRAWN states are displayed with appropriate styling. Rejected/withdrawn states show terminal status clearly. Executed state indicates merge completed.

## Permission and Security Plan

- Backend authorization is authoritative for all customer merge operations.
- Frontend permission gating is convenience only, not a security boundary.
- No raw SQL or internal exception details are displayed.
- No stack traces are exposed.
- No raw PayloadJson, BeforeDataJson, or SurvivorshipPayload JSON is displayed to the user as raw text — it must be parsed and rendered safely if displayed.
- Sensitive customer fields (CCCD, phone, address, DOB) are not included in merge request DTOs from the backend; if they appear in survivorship payload, they must be displayed with appropriate masking.
- No permission catalog changes are made in this frontend phase.
- Error messages are sanitized using the error mapping module.
- DENY-wins behavior is unchanged.

## Explicitly Out of Scope

- Frontend implementation in this planning task.
- Backend changes.
- New migrations or rollbacks.
- Production migration.
- Release tag.
- Push.
- Business docs changes.
- Permission catalog changes.
- PermissionCodes.cs changes.
- Automatic fuzzy merge without review.
- Destructive customer deletion.
- Service/payment/document module implementation.
- Merge execution UI (execution is handled by backend workflow execution handler, not by frontend action).
- Merge reversal UI (reversal policy remains an open question).

## Risks / Open Questions

1. **Survivorship field conflict display**: The exact UX for rendering survivorship payload (which fields the user selected to keep from source vs target) is not defined. The backend stores this as a JSON string. Frontend must parse and display it safely without exposing raw JSON. Recommendation: display as a structured key-value comparison table.

2. **Admin vs requester view**: Whether the merge requests list page shows only the requester's own requests or all requests depends on CUSTOMER_MERGE_REQUEST_VIEW vs CUSTOMER_MERGE_REQUEST_ADMIN_VIEW. Recommendation: single page that filters by backend authorization — the backend already handles this distinction.

3. **Workflow approval integration**: Whether merge request approval reuses the existing My Approvals / WorkflowInstanceDetailPage flow or requires a dedicated merge approval screen. Recommendation: reuse existing workflow pages — the merge request detail page links to the workflow instance via `workflowInstanceId`, and approval happens on the existing WorkflowInstanceDetailPage.

4. **Large duplicate result pagination**: If duplicate search returns many candidates, pagination or virtual scrolling may be needed. The current backend `CheckDuplicatesAsync` return type needs verification for pagination support. Recommendation: implement as simple list initially; add pagination if backend supports it.

5. **Customer detail entry point**: Whether "Find Duplicates" or "Merge" button should appear on CustomerDetailPage. Recommendation: add a small action button or link gated by CUSTOMER_MERGE_REQUEST_CREATE permission; implementation detail.

6. **Future linked-module display**: Service/payment/document impact preview is not available in backend yet. The merge request detail page should have a placeholder section or note that linked-module impact review is a future enhancement.

7. **DuplicateCheckResult type**: The backend `CheckDuplicatesAsync` return type is defined in `ICustomerService`, not in `CustomerMergeDtos.cs`. The exact response shape needs verification during implementation. If it differs, the frontend type must match the actual API response.

## Recommended Implementation Boundaries

If Project Owner accepts this plan, the recommended next task is:

Phase 1B.5-C frontend implementation only.

Implementation boundaries:
- No backend changes unless a blocker is found and separately approved.
- No migration or rollback changes.
- No business doc changes.
- No production migration.
- No release tag.
- No push.

## Recommended Implementation Steps

1. Add TypeScript types (`customerMergeTypes.ts`).
2. Add error message mapping (`customerMergeErrorMessages.ts`) and tests.
3. Add API client (`customerMergeApi.ts`) and tests.
4. Add duplicate search page and tests.
5. Add merge request create page/form and tests.
6. Add merge requests list page and tests.
7. Add merge request detail page and tests.
8. Wire routes in App.tsx.
9. Wire navigation entries in AuthenticatedShell.tsx.
10. Run frontend validation (oxlint, tsc, vitest).
11. Create implementation report.
12. Submit for implementation acceptance review.

## Project Owner Approval Required

This plan does not authorize frontend implementation.
Frontend implementation may begin only after Project Owner accepts this Phase 1B.5-C frontend scope and implementation plan.
