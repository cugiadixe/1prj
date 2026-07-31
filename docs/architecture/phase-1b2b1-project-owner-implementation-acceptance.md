# Phase 1B.2-B1 Customer Backend Foundation Project Owner Implementation Acceptance

**Status:**
ACCEPTED — PHASE 1B.2-B1 CLOSURE REVIEW PASSED — SEE phase-1b2b1-final-closure-review.md

**Accepted implementation:**
Phase 1B.2-B1 — Customer Backend Foundation

**Accepted implementation commit:**
91828f55924085401ba2bf16a3519b59859dc1d2

**Accepted implementation acceptance review commit:**
458a8ea478a97e2b8b38761054fde4dce851d73a

**Accepted plan commit:**
7c6a610a1bebdd68a42d90ca2070cace5b90ed17

**Accepted Project Owner plan acceptance commit:**
6ffa7c8b094ed4cef709af1b22ee5da48e6e993d

**Acceptance baseline:**
458a8ea478a97e2b8b38761054fde4dce851d73a

---

## Project Owner decision

The Project Owner accepts the Phase 1B.2-B1 Customer Backend Foundation implementation.

---

## Accepted implemented scope

- Customer backend foundation implemented.
- Database migration implemented.
- Rollback implemented.
- Profiles table implemented.
- Customers table implemented.
- Customer_Company_Contexts table implemented.
- rowVersion/concurrency implemented.
- Duplicate detection implemented.
- Sensitive projection/masking implemented.
- Atomic create Profile + Customer + Customer Company Context implemented.
- Admin update with reason and rowVersion implemented.
- Audit behavior implemented.
- API v2 Customer endpoints implemented according to the accepted plan.
- Backend validation remains authoritative.
- Backend authorization remains authoritative.
- EF CRUD used for normal CRUD.
- No Dapper/stored procedures introduced.

---

## Accepted permission gates

- CUSTOMER_VIEW_BASIC.
- CUSTOMER_VIEW_SENSITIVE.
- CUSTOMER_CREATE_FINAL.
- CUSTOMER_MASTER_UPDATE.

---

## Accepted permission/code constraints

- PermissionCodes.cs synchronized only with existing approved customer catalog codes.
- permission-catalog.md unchanged.
- No new permission code names added.

---

## Accepted test evidence

- Build passed with 0 errors.
- Unit tests: 133 passed.
- Integration tests: 196 passed.
- API tests: 257 passed.
- Migration/rollback verification passed through V0005/U0005 test coverage.
- No reruns required.

---

## Accepted validation fixes

- Customer MarkUpdated / rowVersion concurrency fix.
- GlobalExceptionFilter uses ConcurrencyException.ErrorCode instead of hardcoded ORG prefix.
- TestDatabaseFixture updated for V0005 known tables/reset.
- SecuritySchemaTests updated for approved Customer permission codes.
- MigrationRollbackTests updated for V0005/U0005 reverse order.

---

## Accepted deferred scope

- Frontend Customer UI remains deferred.
- Workflow/approval runtime remains deferred.
- Customer merge remains deferred.
- Group spending remains deferred.
- ENTITY scope remains deferred.
- Service module remains deferred.
- Payment/Reconciliation remains deferred.
- Export/download remains deferred.
- Security enhancement backlog remains deferred.

---

## Accepted constraints

- No production auto-migration is authorized.
- V0005 must not be applied to production automatically.
- Frontend implementation requires a separate approved implementation task.
- Workflow/approval requires a separate approved phase.
- Customer merge requires a separate approved phase or explicit future plan acceptance.
- ENTITY scope requires separate approval.

---

## Project Owner acceptance

The Project Owner accepts Phase 1B.2-B1 as implemented under the approved scope.

---

## Next recommended work

Proceed to a closure review for Phase 1B.2-B1, then final acceptance, before starting the frontend Customer UI phase.

PHASE 1B.2-B1 CUSTOMER BACKEND FOUNDATION IMPLEMENTATION ACCEPTED — READY FOR CLOSURE REVIEW
