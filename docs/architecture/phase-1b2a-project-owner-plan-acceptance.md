# Phase 1B.2-A Customer Module Project Owner Plan Acceptance

**Status:**
ACCEPTED — PHASE 1B.2-A CUSTOMER MODULE PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan:**
Phase 1B.2-A Customer Module Discovery and Detailed Plan

**Accepted plan commit:**
7c6a610a1bebdd68a42d90ca2070cace5b90ed17

**Accepted next work selection acceptance commit:**
a9a368a870d19b6eae903f1041575584402fd089

**Acceptance baseline:**
7c6a610a1bebdd68a42d90ca2070cace5b90ed17

---

## Project Owner decision

The Project Owner accepts the Phase 1B.2-A Customer Module Discovery and Detailed Plan.

---

## Approved implementation direction

Proceed next with the first Customer implementation slice described in the accepted plan.

---

## Approved first implementation slice

- Customer search/list.
- Customer detail with sensitive data masking.
- Admin create Profile + Customer + Customer Company Context.
- Admin update with reason and rowVersion.
- Duplicate detection.
- Audit trail.
- No workflow runtime.
- No customer merge.
- No spending aggregation.
- No Service module dependency.
- No Payment/Reconciliation dependency.
- No ENTITY scope.

---

## Accepted Customer business rule scope

- CUS-001 accepted in first slice.
- CUS-003 accepted in first slice.
- CUS-004 accepted in first slice.
- CUS-005 accepted in first slice.
- CUS-006 accepted in first slice.
- CUS-008 accepted in first slice.
- CUS-009 accepted in first slice.
- CUS-002 workflow remains deferred.
- CUS-007 merge remains deferred.

---

## Accepted Customer acceptance criteria scope

- CUS-01 accepted in first slice where supported by the approved data model.
- CUS-03 accepted in first slice.
- CUS-04 accepted in first slice.
- CUS-05 accepted in first slice.
- CUS-06 accepted in first slice.
- CUS-02 workflow remains deferred.
- CUS-07 spending remains deferred.

---

## Accepted permission gates

Use existing permission catalog codes only:
- CUSTOMER_VIEW_BASIC
- CUSTOMER_VIEW_SENSITIVE
- CUSTOMER_CREATE_FINAL
- CUSTOMER_MASTER_UPDATE

No new permission codes are approved by default.
No PermissionCodes.cs change is approved unless the codebase does not already contain the existing catalog codes and the implementation plan explicitly limits the change to synchronizing existing approved codes.

---

## Accepted database design direction

- Profiles table.
- Customers table.
- Customer_Company_Contexts table.
- rowversion for concurrency.
- Status/soft-delete behavior according to accepted plan.
- Duplicate detection indexes according to accepted plan.
- Migration and rollback required.
- Migration naming should follow existing repository pattern, with V0005/U0005 or next available equivalent verified before implementation.

---

## Accepted backend/API direction

- New Customer module backend is approved for the next implementation phase.
- API v2 Customer endpoints are approved according to the accepted plan.
- Backend validation remains authoritative.
- Backend authorization remains authoritative.
- EF CRUD should be used for normal CRUD.
- Dapper/stored procedures should not be introduced unless a future approved plan identifies a complex sensitive transaction that requires it.
- Mutations requiring concurrency must use rowVersion.
- Customer create/update and sensitive access behavior must be audited according to the accepted plan.

---

## Accepted frontend direction

- Customer route/menu/pages are approved according to the accepted plan.
- Route/menu/action gates must use the approved Customer permission codes.
- Sensitive data masking must be enforced in the UI, while backend remains authoritative.
- Existing frontend route/menu/API/test patterns should be reused.

---

## Accepted test strategy

- Backend unit tests.
- Integration tests.
- API tests.
- Frontend tests.
- Migration/rollback verification.
- Authorization tests.
- Validation tests.
- Concurrency tests.
- Audit behavior tests.

---

## Accepted deferred items

- Workflow/approval runtime remains deferred unless separately approved.
- Customer merge remains deferred unless separately approved.
- Group spending remains deferred unless separately approved.
- ENTITY scope remains deferred unless separately approved.
- Service module remains deferred.
- Payment/Reconciliation remains deferred.
- Security enhancement backlog remains deferred.
- Export/download remains deferred unless separately approved.

---

## Accepted Project Owner decisions

- DEC-1B2A-01 approved — Customer module is the next implementation area.
- DEC-1B2A-02 approved — first implementation slice scope approved as documented.
- DEC-1B2A-03 approved — customer data fields approved as documented, with undocumented fields treated as open decisions.
- DEC-1B2A-04 approved — customer lifecycle/status model approved as documented.
- DEC-1B2A-05 approved — duplicate detection rules approved as documented.
- DEC-1B2A-06 approved — customer merge deferred.
- DEC-1B2A-07 approved — workflow/approval runtime deferred.
- DEC-1B2A-08 approved — ENTITY scope remains deferred.
- DEC-1B2A-09 approved — database table design approved.
- DEC-1B2A-10 approved — migration/rollback strategy approved.
- DEC-1B2A-11 approved — API v2 endpoint set approved.
- DEC-1B2A-12 approved — frontend route/menu/page structure approved.
- DEC-1B2A-13 approved — permission gates use existing permission catalog codes.
- DEC-1B2A-14 approved — no new permission codes by default.
- DEC-1B2A-15 approved — audit/security behavior approved.
- DEC-1B2A-16 approved — test strategy approved.
- DEC-1B2A-17 approved — Service/Payment dependencies deferred from first Customer slice.
- DEC-1B2A-18 approved — implementation may proceed only after this plan acceptance is committed.

---

## Implementation authorization

After this acceptance commit, a separate implementation task may be created for the approved first Customer slice.
This acceptance task itself must not implement source code, tests, migrations, rollbacks, or API/frontend changes.

PHASE 1B.2-A CUSTOMER MODULE PLAN ACCEPTED — READY FOR APPROVED IMPLEMENTATION TASK
