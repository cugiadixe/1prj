# Phase 1B.8-C Card Reprint Frontend Implementation Report

## Status

IMPLEMENTED — READY FOR FRONTEND ACCEPTANCE REVIEW

## Authorization Source

- Phase 1B.8-C Project Owner frontend plan acceptance commit:
  13df30614eb58c5ec3fc6dc1314ef8f0b78dcf49

## Implemented Scope

The Card Reprint frontend UI is fully implemented:
- List, Create, and Detail pages are added.
- All forms and UI elements validate inputs and gracefully handle HTTP 400/403/404/409 errors.
- Permission-gated actions restrict viewing, creating, and modifying reprints based on permissions.
- Workflow actions (Submit, Approve, Reject) sync with the backend.
- Payment actions (Create draft, View payment, Check Status) sync with the backend payment foundation.
- Physical actions (Mark Printed, Mark Released) are supported and properly sequenced.
- Tests (17 component/integration tests) are written and pass.

## Files Changed

- `src/frontend/src/cards/CardReprintRequestCreatePage.test.tsx` (New)
- `src/frontend/src/cards/CardReprintRequestCreatePage.tsx` (New)
- `src/frontend/src/cards/CardReprintRequestDetailPage.test.tsx` (New)
- `src/frontend/src/cards/CardReprintRequestDetailPage.tsx` (New)
- `src/frontend/src/cards/CardReprintRequestsPage.test.tsx` (New)
- `src/frontend/src/cards/CardReprintRequestsPage.tsx` (New)
- `src/frontend/src/cards/cardReprintApi.ts` (New)
- `src/frontend/src/cards/errorMessages.ts` (New)
- `src/frontend/src/cards/hooks.ts` (New)
- `src/frontend/src/cards/types.ts` (New)
- `src/frontend/src/App.tsx` (Modified - added routes)
- `docs/architecture/phase-1b8c-card-reprint-frontend-implementation-report.md` (New)

## Routes / Pages Implemented

- `/cards/reprints` -> `CardReprintRequestsPage` (List of requests)
- `/cards/reprints/new` -> `CardReprintRequestCreatePage` (Form to request a reprint)
- `/cards/reprints/:id` -> `CardReprintRequestDetailPage` (Details, workflow, payment actions)

## Components Implemented

- **CardReprintRequestsPage**: Handles data fetching with pagination placeholders, filtering by status, and displaying a table of reprint requests.
- **CardReprintRequestCreatePage**: Uses Ant Design Forms to collect Card ID, Reason Code, and Notes. Sends mutation and handles global error notifications.
- **CardReprintRequestDetailPage**: Shows descriptions with a dynamic status badge. Conditionally renders workflow actions (Submit, Approve, Reject with modals), payment actions (Create Draft, View Payment), and physical handling actions (Mark Printed, Mark Released).

## API Client / Hooks Implemented

- **Hooks**: `useCardReprintRequests`, `useCardReprintRequest`, `useCreateCardReprintRequest`, `useSubmitCardReprintRequest`, `useApproveCardReprintRequest`, `useRejectCardReprintRequest`, `useCreatePaymentForCardReprint`, `useCardReprintPaymentStatus`, `useMarkCardPrinted`, `useMarkCardReleased`.
- **API Client**: `cardReprintApi.ts` maps all 10 endpoints required for Card Reprint with correct types mirroring `CardReprintRequestDto`.

## Permission-Gated UI Evidence

- **List view / Detail view**: Shows "Permission Denied" if backend returns HTTP 403.
- **Create Request / Create Payment / Submit**: Displayed only if `hasPermission('CARD_REPRINT_REQUEST_CREATE', 'GLOBAL')` is true.
- **Approve**: Displayed only if `hasPermission('CARD_REPRINT_APPROVE', 'GLOBAL')` is true.
- **Mark Printed / Mark Released**: Displayed only if `hasPermission('CARD_REPRINT_REQUEST_MARK_PRINTED', 'GLOBAL')` is true.

## Lifecycle / Workflow UI Evidence

- **Submit**: Active only on `DRAFT`.
- **Approve/Reject**: Active only on `PENDING_APPROVAL`. Both use Modal dialogs to safely collect `comment` and `reason`.
- **Rejected**: Terminal state, halts all interaction.

## Payment UI Evidence

- **Create Payment Draft**: Appears only on `APPROVED` status. Invokes backend which generates the draft in the Payment Foundation.
- **Payment Link**: If `paymentTransactionId` is non-null, a "View Payment" deep link navigates to `/payments/:id`.
- **Payment Status**: The frontend implements a read-only polling mechanism `useCardReprintPaymentStatus` to update the local UI when in `PENDING_PAYMENT`.

## Print / Release UI Evidence

- **Mark Printed**: Action button appears on `PAID`. Invokes `/mark-printed` backend route.
- **Mark Released**: Action button appears on `PRINTED`. Invokes `/mark-released` backend route.

## Tests Added / Updated

Added 17 tests across three test suites in `src/frontend/src/cards/`:
- `CardReprintRequestsPage.test.tsx` (6 tests): Renders loading, error, empty, list states, permission gating, and 403 handling.
- `CardReprintRequestCreatePage.test.tsx` (4 tests): Form rendering, validation, successful submission with redirect, and API failure errors.
- `CardReprintRequestDetailPage.test.tsx` (7 tests): Data presentation, conditional action buttons strictly by state (DRAFT vs PENDING_APPROVAL), permission gating, and mutation firing.

## Validation Evidence

- **Lint**: Passed (`npm run lint` -> 0 errors, 3 standard React warnings)
- **TypeScript**: Passed (`npm run build` -> built successfully)
- **Vitest**: Passed (`npx vitest run src/cards` -> 17 passed)
- **git diff --check**: Passed (No trailing whitespace)

## Boundary Confirmation

- No backend implementation.
- No backend files changed.
- No database migrations/rollbacks changed.
- No business docs changed.
- No permission catalog changed.
- No Care Package Sales.
- No operational validation execution.
- No production migration.
- No release tag.
- No push.
- No dynamic PDF/template generation.
- No generic Payment Print UI.
- No refund/cancellation/partial payment.
- No physical inventory/stamp stock management.
- `implementation_plan.md` not committed.
- `task.md` not committed.

## Risks / Follow-Ups

- Operational validation is deferred to Phase 1B.8-D.
- Navigating to `/payments/:id` relies on the existing Payment Foundation UI route structure (`PaymentDetailPage`). If that route changes, this deep link will need an update.
