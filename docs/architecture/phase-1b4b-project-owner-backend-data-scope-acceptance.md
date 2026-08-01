# Phase 1B.4-B Customer Master Backend/Data Foundation Project Owner Scope Acceptance

## Status

ACCEPTED — PHASE 1B.4-B BACKEND/DATA FOUNDATION SCOPE APPROVED

## Accepted Scope Plan

- Phase 1B.4-B backend/data foundation plan commit:
  8ef86637866b1dfc1dbd64868591576340f627c9
- Phase 1B.4 PO plan acceptance commit:
  06cb36218503f6f4c01a05b05e8fb077a16a767d
- Phase 1B.4 discovery plan commit:
  4dec520d41fc1ad6de9ec4b25a50415b179f2d0c
- Phase 1B.4 selection commit:
  420f76df3d37218c47d98168923b5fa559fc78d9
- B5-D Project Owner closure acceptance commit:
  0a4149fb233c516210acba197a8b2977cbc39170

## Project Owner Decision

The Project Owner accepts the Phase 1B.4-B Customer Master Backend/Data Foundation scope and implementation plan.

## Accepted Backend/Data Implementation Scope

Authorize future implementation only within the accepted backend/data foundation scope.

Accepted implementation areas:
- Extend or use existing Customer_Change_Requests / CustomerChangeRequest foundation as documented.
- Add target customer linkage where required.
- Add target rowversion/concurrency handling where required.
- Implement official customer update boundary after approved workflow.
- Implement before/after value handling with sensitive data protection.
- Link customer master change request to workflow instance.
- Implement CUSTOMER_MASTER_CHANGE / CUSTOMER_UPDATE_FROM_APPROVAL backend workflow execution boundary as documented.
- Implement backend-authoritative permission checks.
- Implement API v2 backend endpoints within the accepted scope.
- Implement migration and rollback only as documented.
- Implement backend unit, integration, API, and migration rollback tests.
- Preserve audit/history and sanitized error behavior.

## Accepted Database Scope

Accept planning basis for:
- Extending existing customer change request schema where required.
- target_customer_id.
- target_row_version.
- workflow_instance_id linkage where required.
- status lifecycle fields where required.
- rowversion/concurrency handling.
- audit metadata.
- indexes and constraints needed for safe backend operation.
- rollback script.
- MigrationRollbackTests coverage.

Final implementation must follow SQL Server migration and rollback conventions already used in the repository.

## Accepted API v2 Scope

Accept backend API planning for:
- create customer master change request,
- list my customer master change requests,
- get customer master change request detail,
- safe before/after diff,
- submit/start workflow where applicable,
- data-admin apply approved change,
- duplicate check only if already supported and bounded as read-only/non-merge,
- sanitized errors,
- backend permission enforcement.

## Accepted Workflow Scope

Accept backend workflow planning for:
- CUSTOMER_MASTER_CHANGE process integration.
- Reuse of B5 workflow runtime hardening.
- My Requests and Action History compatibility.
- Reject terminal semantics.
- Retry semantics for failed execution.
- Double-apply prevention.
- Stale rowversion handling before official update.
- No customer official data mutation on rejected request.

## Accepted Permission/Security Scope

Accept backend planning for permission codes such as:
- CUSTOMER_CHANGE_REQUEST_CREATE
- CUSTOMER_CHANGE_REQUEST_VIEW
- CUSTOMER_CHANGE_REQUEST_ADMIN_VIEW
- CUSTOMER_CHANGE_REQUEST_APPLY
- CUSTOMER_DUPLICATE_CHECK only if implementation confirms it is required and bounded

Confirm:
- Exact names may be finalized during implementation if kept consistent with repository permission conventions.
- Backend authorization remains authoritative.
- Deny-wins rule remains in force.
- Company scope rules remain in force.
- Raw sensitive data must not be exposed.
- Raw before/after JSON must not be dumped into UI/logs.
- SQL/internal exception details must not be exposed.
- Audit/history must be preserved.

## Explicitly Not Authorized

This acceptance does not authorize:
- frontend implementation,
- frontend tests,
- production migration,
- production release,
- release tag,
- push,
- Service module,
- Payment module,
- Card print/reprint flow,
- Plot/cemetery location flow,
- ENTITY expansion,
- export/download,
- safe user lookup/reassign expansion,
- customer merge implementation unless separately approved,
- broad workflow engine rewrite,
- broad frontend redesign,
- unsupported business behavior not present in accepted documents.

## Required Implementation Evidence

Future implementation acceptance must include:
- exact committed file list,
- migration and rollback evidence,
- backend build result,
- unit test result,
- integration test result,
- API test result,
- MigrationRollbackTests result,
- git diff --check result,
- proof no frontend source/tests changed,
- proof no production migration/release/tag/push occurred,
- security/data exposure validation,
- concurrency/idempotency validation,
- rejected request does not alter official customer data,
- approved request can be applied once only.

## Open Decisions Carried Forward

The following must be resolved or explicitly documented during implementation:
- exact protected field list,
- exact workflow definition assignment,
- duplicate detection behavior and whether it blocks submission,
- whether duplicate merge remains deferred,
- exact final permission code names,
- exact audit payload field set,
- before/after redaction model,
- whether data-admin apply is manual or automatic after approval,
- stale rowversion handling,
- manual validation data constraints.

## Next Authorized Step

Project Owner authorizes:

Phase 1B.4-B Backend/Data Foundation Implementation

Implementation is authorized only within the accepted backend/data scope above.

Do not implement frontend in this step.
Do not perform production migration.
Do not tag.
Do not push.

## Conclusion

PHASE 1B.4-B BACKEND/DATA FOUNDATION SCOPE ACCEPTED — READY FOR BACKEND/DATA IMPLEMENTATION
