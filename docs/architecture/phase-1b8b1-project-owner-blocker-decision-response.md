# Phase 1B.8-B1 Project Owner Blocker Decision Response

## Status

DECIDED — B1 IMPLEMENTATION BLOCKERS RESOLVED FOR RETRY

## Project Owner Decision

The Project Owner resolves the Phase 1B.8-B1 backend/data implementation blockers.

The previous B1 attempt was correctly stopped because the implementation plan was ambiguous against the actual repository structure and B1/B2 boundary.

This document clarifies B1 retry scope only.

## Blocked Implementation Report

- Blocked B1 implementation report commit:
  11d7e26f8d7b7e61e389e6eb0092c47ab4a5dc11

- Blocked report:
  docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-report.md

## Decisions

| ID | Decision Area | Project Owner Answer | Impact | Implementation Authorized In This Document? |
|---|---|---|---|---|
| B1-BLOCKER-001 | Backend Module Placement | Do not create a new backend project/module named `src/backend/PTKD.CardReprint`. Phase 1B.8-B1 must follow the actual repository structure and existing layered backend conventions. Use the existing backend projects/modules already present in the repository, such as Domain, Application, and API. Exact project/file names must be determined from the current repo structure during implementation. | Update B1 retry implementation to use existing layered modules. Do not invent a new backend project unless the repo already uses that pattern. | No. This document only clarifies the next retry scope. |
| B1-BLOCKER-002 | B1 vs B2 Scope Split | Phase 1B.8-B1 is backend/data foundation only. B1 must defer full integration to B2 for real workflow instance creation/execution, approval action execution through Workflow Engine, payment draft/bill creation, payment confirmation integration, reconciliation integration, and end-to-end workflow/payment lifecycle. B1 may create nullable/link fields for future workflow/payment integration (workflow_instance_id, payment_transaction_id, service_item_id) but must not implement full B2 workflow/payment behavior. | B1 will focus on persistence, API foundation skeletons, and tests. Workflow/payment behaviors are deferred to B2. | No. This document only clarifies the next retry scope. |
| B1-BLOCKER-003 | API Scope in B1 | B1 may include only backend/API foundation that is safe without full B2 integration. Allowed in B1: DTOs, controller skeleton/endpoints only if they persist/read request records and enforce authorization, list/detail/create draft request if no workflow/payment execution is triggered, lifecycle guard methods that prevent printed/released states without future payment confirmation, safe errors, and concurrency handling. Deferred to B2: approve/reject execution, create payment draft, link confirmed payment, mark printed/released if it depends on payment integration not yet implemented. | Implementation retry must explicitly document which endpoints are implemented in B1 and which are deferred to B2. | No. This document only clarifies the next retry scope. |
| B1-BLOCKER-004 | Permission Seed / Catalog Scope | B1 may implement technical permission seed entries only if required for backend/API authorization tests. Do not modify `docs/business/permission-catalog.md` or business docs in B1. Use repository permission seed conventions only. Permission scope should be COMPANY unless existing docs require otherwise. | Backend authorization remains authoritative. Frontend gating is deferred. If B1 does not expose secured endpoints, permission seed changes may be deferred to B2. | No. This document only clarifies the next retry scope. |
| B1-BLOCKER-005 | Migration Strategy | V0013/U0013 are authorized for B1 only if the implementation retry confirms new tables are required. V0013 should be limited to the B1 data foundation. Expected data scope includes Cards or equivalent table, Card_Reprint_Requests, print count/history fields, status, references, audit, and concurrency. U0013 must rollback V0013 completely. No production migration is authorized. | Limits the DB schema changes strictly to what's required for B1. | No. This document only clarifies the next retry scope. |
| B1-BLOCKER-006 | Implementation Retry Boundary | After this blocker decision response is committed, the next authorized task is: Phase 1B.8-B1 backend/data foundation implementation retry only. The retry must not include frontend implementation, full workflow/payment execution, operational validation execution, Care Package Sales, production migration, release tag, push, dynamic PDF/template generation, generic Payment Print UI, refund/cancellation/partial payment, or physical inventory/stamp stock management. | Next task is strictly the B1 implementation retry. | No. This document only clarifies the next retry scope. |

## Corrected B1 Scope

B1 retry may include only:

- V0013/U0013 if required after inspecting current schema.
- Card / Grave Card data foundation if needed.
- Card_Reprint_Requests data foundation if needed.
- domain entities and status model.
- persistence mappings if applicable.
- application DTOs/contracts/services needed for B1.
- minimal safe API foundation if it does not trigger workflow/payment execution.
- technical permission seed only if needed for B1 authorization.
- backend unit/integration/API tests for B1 scope.
- updated implementation report.

## Deferred to B2

The following are deferred to Phase 1B.8-B2:

- real workflow instance creation/execution.
- approve/reject through Workflow Engine.
- payment draft/bill creation.
- payment confirmation integration.
- reconciliation integration.
- full lifecycle execution across workflow and payment.

## Still Deferred Beyond B1/B2 Unless Later Authorized

- frontend implementation.
- dynamic PDF/template generation.
- generic Payment Print UI.
- physical inventory/stamp stock management.
- refund.
- cancellation.
- partial payment.
- Care Package Sales.
- production rollout.
- production migration.
- release tag.
- push.

## Authorization for Next Step

Authorized next task:
Phase 1B.8-B1 backend/data foundation implementation retry only.

The retry must follow the corrected B1 scope in this decision response.

Do not authorize:
- frontend implementation,
- full B2 workflow/payment integration,
- operational validation execution,
- Care Package Sales,
- production migration,
- release tag,
- push.

## Required Next Task Output

The retry must produce either an updated or replacement implementation report:

docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-report.md

or, if preserving the previous blocked report is preferred:

docs/architecture/phase-1b8b1-card-reprint-backend-data-implementation-retry-report.md

The report must clearly distinguish:
- previous blocked attempt,
- corrected B1 retry implementation,
- remaining B2 deferrals.

## Boundaries

- no implementation is performed in this blocker decision task.
- no source code changes are performed.
- no test changes are performed.
- no migrations/rollbacks are created.
- no permission catalog changes are performed.
- no production migration/tag/push.

## Notes

- the previous blocked report remains part of the audit trail.
- B1 retry should not rewrite git history.
- local scratch/decompiled/FixStrategy files must not be staged.
