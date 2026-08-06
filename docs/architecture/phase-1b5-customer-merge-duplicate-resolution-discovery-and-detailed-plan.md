# Phase 1B.5 Customer Merge and Duplicate Resolution Discovery and Detailed Plan

## Status

PROPOSED — REQUIRES PROJECT OWNER ACCEPTANCE BEFORE IMPLEMENTATION

## Authorization Source

Reference:
- Post-1B.4 PO next-work decision commit:
  1708778cfd242a0940922b97c5b530e9bc587f3e

State:
- Phase 1B.5 is selected for discovery and detailed planning only.
- This document does not authorize implementation.

## Objective

Define Phase 1B.5 Customer Merge and Duplicate Resolution scope, decisions, architecture, and implementation sequence.

## Source Documents Reviewed

- docs/architecture/post-1b4-project-owner-next-work-decision.md
- docs/architecture/post-1b4-next-work-selection-discovery-and-recommendation.md
- docs/architecture/phase-1b4-project-owner-closure-acceptance.md
- docs/architecture/phase-1b4d-operational-validation-and-closure-acceptance-review.md
- docs/architecture/phase-1b4d-operational-validation-and-closure-report.md
- docs/architecture/phase-1b4c-project-owner-frontend-implementation-acceptance.md
- docs/architecture/phase-1b4b-project-owner-backend-data-implementation-acceptance.md
- docs/architecture/phase-1b4-customer-master-expansion-discovery-and-detailed-plan.md
- PTKD-ERP-Master-Context.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/business/process-catalog.md
- docs/business/PTKD-Specification-v1.1.md

## Confirmed Existing Foundation

- Phase 1B.4 Customer Master Expansion is closed.
- CustomerMasterChange exists.
- CustomerChangeRequest foundation exists.
- customer/profile/company context foundation exists.
- workflow runtime exists.
- backend/frontend/test patterns exist.

## Repository-Supported Business Context

- Shared customer resource across companies is documented (GLOBAL customer-master resources).
- Critical fields controlled by customer data admin group is documented.
- Regular staff cannot directly change critical/common customer data (changes route to GROUP_CUSTOMER_DATA_ADMIN).
- Explicit duplicate/merge references found:
  - `CUSTOMER_MERGE_DUPLICATE` must run duplicate checks and requires approval.
  - Must preview affected services, payments, documents, and company context.
  - Source history is retained; customer is marked `MERGED` and linked via `survivor_customer_id`.
  - Active, non-empty CCCD requires a unique constraint.

## Proposed Phase 1B.5 Scope

- duplicate candidate detection,
- merge request creation,
- merge review/detail,
- approved merge execution boundary,
- canonical target customer selection,
- source customer merged/inactive marker,
- merge audit/history,
- API v2 support,
- frontend workflow,
- tests.

## Explicitly Out of Scope

- production migration,
- release tag/push,
- customer deletion,
- destructive data loss,
- service/payment module changes unless required as read-only impact analysis,
- automatic fuzzy merge without review,
- customer merge implementation in this planning task,
- new business requirements not supported by docs.

## Proposed Data Model

- `CustomerChangeRequest` (Existing table)
  - purpose: Track the merge request details by using process code `CUSTOMER_MERGE_DUPLICATE`.
  - required: Yes.
  - migration impact: None (reuses existing schema).
- `Customer` (Existing table)
  - purpose: Mark source customer as `MERGED` and store canonical ID in `SurvivorCustomerId`.
  - required: Yes.
  - migration impact: Minimal (fields `CustomerStatus` and `SurvivorCustomerId` already exist).
- rowversion/concurrency fields
  - purpose: Ensure source and target records haven't changed while merge request was pending.
  - required: Yes.

## Proposed Business Rules

- merge requires approval or Project Owner decision if not confirmed.
- no hard delete.
- source customer remains traceable.
- canonical customer survives.
- linked history preserved.
- customer data admin / permission requirement.
- duplicate candidates must be reviewed before merge.
- rowversion/concurrency checks.

## Proposed Workflow Design

- CUSTOMER_MERGE workflow process proposal:
  - Drafting phase for finding candidates and defining survivorship rules.
- request / approval / rejection / execution steps modeled in workflow runtime.
- idempotency on execution.
- retry handling on execution failures.
- rejected request no mutation on base tables.
- audit trail captured during execution.

## Proposed API v2 Design

- `GET /api/v2/customers/duplicates` - search endpoint for identifying duplicates.
- `POST /api/v2/customers/merge-requests` - create merge request.
- `GET /api/v2/customers/merge-requests/{id}` - merge request detail including preview of impacted contexts.
- apply execution handler boundary.
- concurrency errors (409 Conflict).
- sanitized errors (400 Bad Request) for invalid target/source references.

## Proposed Frontend Design

- duplicate candidate search/list.
- merge request form.
- before/after survivorship review.
- my merge requests.
- merge request detail.
- admin/review page if needed.
- workflow links.

## Permission and Security Plan

- `CUSTOMER_MERGE_DUPLICATE` (Existing, confirmed)
- `CUSTOMER_MERGE_REQUEST_CREATE` (Proposed)
- `CUSTOMER_MERGE_REQUEST_VIEW` (Proposed)
- `CUSTOMER_MERGE_REQUEST_ADMIN_VIEW` (Proposed)
- `CUSTOMER_MERGE_APPROVE` (Proposed, or workflow permission reuse)
- `CUSTOMER_MERGE_EXECUTE` (Proposed, if needed)

*Note: All new permissions require Project Owner approval.*

- backend authorization authoritative.
- frontend gating convenience only.
- DENY wins.
- no raw sensitive payload exposure.
- append-only audit.

## Database / Migration Strategy

- V0010/U0010 if approved.
- MigrationRollbackTests.
- DB safety with PTKD_TEST_PHASE1A2.
- rowversion/concurrency applied to all affected tables.
- no production migration in implementation phase unless separately approved.

## Test Strategy

- Unit tests for merge logic and preview generation.
- Integration tests mapping workflow state to DB state and testing `SurvivorCustomerId` linking.
- API tests covering endpoint security and 409 concurrency codes.
- migration rollback tests.
- API client,
- duplicate search/list,
- merge form,
- detail/review,
- error mapping,
- permission-gated UI.
- operational validation.

## Risks and Open Questions

- exact survivorship rules for conflicting single-value fields (e.g. differing DOBs or Phones across duplicates).
- merge reversal policy (is a merge strictly irreversible? If an error is made, how is it corrected?).
- required approval flow specific logic (always routes to group, or auto-approved if requested by admin?).
- duplicate matching criteria (strict exact match vs normalized string matching).
- linked records in future service/payment modules (how are foreign keys updated gracefully?).
- permission catalog changes.
- manual data cleanup risk.

## Recommended Implementation Phases

- 1B.5-A Discovery/plan acceptance
- 1B.5-B Backend/data foundation scope and PO acceptance
- 1B.5-C Backend/data implementation
- 1B.5-D Frontend scope/PO acceptance
- 1B.5-E Frontend implementation
- 1B.5-F Operational validation and closure

## Acceptance Criteria for Implementation

- Duplicate candidates must be discoverable and validatable before merge.
- The approval workflow must correctly gate merge execution.
- Merge execution must safely preserve source history by utilizing `SurvivorCustomerId` and `MERGED` status.
- Operational preview of affected entities must be accurate.
- `rowversion` protection must correctly prevent concurrent mutations.

## Project Owner Approval Required

This plan does not authorize implementation.
Implementation may begin only after Project Owner accepts this Phase 1B.5 discovery and detailed plan and explicitly authorizes the next implementation planning scope.
