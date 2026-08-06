# Phase 1B.8-A Project Owner Blocker Decision Response

## Status

DECIDED — BLOCKERS RESOLVED FOR DETAILED SCOPE ACCEPTANCE REVIEW

## Project Owner Decision

The Project Owner provides decisions for the blocking Phase 1B.8-A Card Reprint / Grave Card Reprint questions.

These decisions resolve the blockers required before detailed scope acceptance review.

This document does not authorize implementation.

## Decision Source

Reference:

- Phase 1B.8-A blocked detailed scope clarification commit:
  612a76dd31200a4fd0c433dfe8276bbe2394c979

- Blocked document:
  docs/architecture/phase-1b8a-card-reprint-open-decisions-and-detailed-scope.md

## Decisions

| ID | Decision | Project Owner Answer | Impact | Implementation Authorized? |
|---|---|---|---|---|
| OD-1B8-001 | Exact New Print vs Reprint Terminology | Use two terms:<br>- Initial Print: first issuance/printing of a grave card for a grave/service record.<br>- Reprint: every print after the Initial Print.<br><br>The system must track print count or enough request history to distinguish Initial Print from Reprint. | Terminology and tracking requirements set. | No. This decision only clarifies terminology. |
| OD-1B8-004 | Fee Model and Whether 50k Is Configurable Price | Reprint fee is 50,000 VND per card/reprint at current business rule.<br><br>The fee must be configurable using the existing service price/effective-date pattern where applicable, not hard-coded in application code.<br><br>Initial Print fee remains unresolved unless existing source documents already define it. If not defined, mark Initial Print fee as out of scope for MVP or require a separate PO decision. | Fee configurability and source set. | No. This decision only clarifies fee model. |
| OD-1B8-005 | Payment Timing Relative to Approval | For Reprint:<br/>1. Request is created.<br/>2. Approval is completed if required.<br/>3. Payment draft/bill is created after approval.<br/>4. Payment must be CONFIRMED before physical print/release completion.<br/><br/>If no approval is required for a specific Initial Print scenario, payment timing must still be clarified separately before Initial Print implementation. | Lifecycle timing set. | No. This decision only clarifies lifecycle timing. |
| OD-1B8-006 | Physical Stamp / Card Custody Handling | Physical stamp/card custody is tracked as status only in Phase 1B.8 MVP.<br/><br/>The system records:<br/>- request approved/rejected,<br/>- payment confirmed,<br/>- card printed,<br/>- card released/handed over,<br/>- actor,<br/>- timestamp,<br/>- note if required.<br/><br/>The system does not manage physical inventory, stamp stock, or advanced custody logistics in Phase 1B.8 MVP. | Status tracking boundary set. | No. This decision only clarifies status tracking boundary. |
| OD-1B8-012 | Print Output / PDF / Template Scope | Phase 1B.8 MVP does not include dynamic PDF/template generation unless already supported by existing infrastructure.<br/><br/>MVP scope may include:<br/>- record request,<br/>- approve/reject,<br/>- payment status,<br/>- mark printed,<br/>- mark released.<br/><br/>Actual printable template/PDF output remains deferred unless a separate PO scope acceptance explicitly authorizes it. | Output boundary set. | No. This decision only clarifies MVP print-output boundary. |
| OD-1B8-013 | Whether Payment Print UI Remains Deferred or Becomes Part of Card Reprint | Generic Payment Print UI remains deferred.<br/><br/>Card Reprint may display payment status and payment reference/link only.<br/><br/>Card Reprint must not implement generic bill/payment printing in Phase 1B.8 unless separately authorized. | Print UI boundary set. | No. This decision only clarifies boundary. |
| OD-1B8-015 | Acceptance Criteria Gaps | Phase 1B.8 MVP acceptance criteria must cover at least:<br/>- create Card Reprint request,<br/>- distinguish Initial Print vs Reprint where supported,<br/>- require configured approval for Reprint when applicable,<br/>- prevent print/release before required approval,<br/>- prevent print/release before CONFIRMED payment for paid Reprint,<br/>- calculate Reprint fee from configurable price/effective-date source,<br/>- create/link payment transaction through Payment Foundation,<br/>- show request status and payment status,<br/>- allow authorized user to mark printed,<br/>- allow authorized user to mark released,<br/>- enforce permission-gated actions,<br/>- record audit trail,<br/>- handle rejection safely,<br/>- show safe errors for 400/403/404/409/500,<br/>- exclude refund/cancellation/partial payment,<br/>- exclude Care Package Sales,<br/>- exclude production migration/tag/push. | MVP AC baseline set. | No. This decision only clarifies acceptance criteria baseline. |

### OD-1B8-001 — Exact New Print vs Reprint Terminology

Project Owner answer:

Use two terms:
- Initial Print: first issuance/printing of a grave card for a grave/service record.
- Reprint: every print after the Initial Print.

The system must track print count or enough request history to distinguish Initial Print from Reprint.

Implementation Authorized?
No. This decision only clarifies terminology.

### OD-1B8-004 — Fee Model and Whether 50k Is Configurable Price

Project Owner answer:

Reprint fee is 50,000 VND per card/reprint at current business rule.

The fee must be configurable using the existing service price/effective-date pattern where applicable, not hard-coded in application code.

Initial Print fee remains unresolved unless existing source documents already define it. If not defined, mark Initial Print fee as out of scope for MVP or require a separate PO decision.

Implementation Authorized?
No. This decision only clarifies fee model.

### OD-1B8-005 — Payment Timing Relative to Approval

Project Owner answer:

For Reprint:
1. Request is created.
2. Approval is completed if required.
3. Payment draft/bill is created after approval.
4. Payment must be CONFIRMED before physical print/release completion.

If no approval is required for a specific Initial Print scenario, payment timing must still be clarified separately before Initial Print implementation.

Implementation Authorized?
No. This decision only clarifies lifecycle timing.

### OD-1B8-006 — Physical Stamp / Card Custody Handling

Project Owner answer:

Physical stamp/card custody is tracked as status only in Phase 1B.8 MVP.

The system records:
- request approved/rejected,
- payment confirmed,
- card printed,
- card released/handed over,
- actor,
- timestamp,
- note if required.

The system does not manage physical inventory, stamp stock, or advanced custody logistics in Phase 1B.8 MVP.

Implementation Authorized?
No. This decision only clarifies status tracking boundary.

### OD-1B8-012 — Print Output / PDF / Template Scope

Project Owner answer:

Phase 1B.8 MVP does not include dynamic PDF/template generation unless already supported by existing infrastructure.

MVP scope may include:
- record request,
- approve/reject,
- payment status,
- mark printed,
- mark released.

Actual printable template/PDF output remains deferred unless a separate PO scope acceptance explicitly authorizes it.

Implementation Authorized?
No. This decision only clarifies MVP print-output boundary.

### OD-1B8-013 — Whether Payment Print UI Remains Deferred or Becomes Part of Card Reprint

Project Owner answer:

Generic Payment Print UI remains deferred.

Card Reprint may display payment status and payment reference/link only.

Card Reprint must not implement generic bill/payment printing in Phase 1B.8 unless separately authorized.

Implementation Authorized?
No. This decision only clarifies boundary.

### OD-1B8-015 — Acceptance Criteria Gaps

Project Owner answer:

Phase 1B.8 MVP acceptance criteria must cover at least:
- create Card Reprint request,
- distinguish Initial Print vs Reprint where supported,
- require configured approval for Reprint when applicable,
- prevent print/release before required approval,
- prevent print/release before CONFIRMED payment for paid Reprint,
- calculate Reprint fee from configurable price/effective-date source,
- create/link payment transaction through Payment Foundation,
- show request status and payment status,
- allow authorized user to mark printed,
- allow authorized user to mark released,
- enforce permission-gated actions,
- record audit trail,
- handle rejection safely,
- show safe errors for 400/403/404/409/500,
- exclude refund/cancellation/partial payment,
- exclude Care Package Sales,
- exclude production migration/tag/push.

Implementation Authorized?
No. This decision only clarifies acceptance criteria baseline.

## Decisions Still Unresolved or Deferred

The following remain deferred or require later confirmation if not required for Phase 1B.8 MVP:
- Initial Print fee, if not source-supported.
- Generic payment/bill print UI.
- Dynamic PDF/template generation.
- Physical inventory/stamp stock management.
- Care Package Sales.
- Production rollout.

## Updated Implementation Readiness

READY FOR PHASE 1B.8-A DETAILED SCOPE ACCEPTANCE REVIEW

## Authorization for Next Step

Authorized next task:
Phase 1B.8-A detailed scope acceptance review only.

Do not authorize:
- implementation,
- source code changes,
- test changes,
- backend changes,
- frontend changes,
- migration/rollback changes,
- permission catalog changes,
- production migration,
- release tag,
- push.

## Required Next Task Output

Future task must produce:

docs/architecture/phase-1b8a-detailed-scope-acceptance-review.md

The review must verify that the Project Owner blocker decisions are reflected consistently in the detailed scope baseline before any implementation planning is authorized.

## Boundaries

- no implementation is authorized by this decision response.
- no source code changes are authorized.
- no test changes are authorized.
- no backend changes are authorized.
- no frontend changes are authorized.
- no migrations/rollbacks are authorized.
- no permission catalog changes are authorized.
- no production migration is authorized.
- no release tag is authorized.
- no push is authorized.

## Non-Goals

This document does not:

- implement Card Reprint.
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

- Phase 1B.8 remains in planning/decision stage.
- Card Reprint implementation must wait for detailed scope acceptance and Project Owner implementation authorization gates.
- Care Package Sales remains deferred.
- production rollout remains deferred.
- scratch/decompiled/FixStrategy files remain untracked and must not be staged.
