# Phase 1B.3-B5-C Frontend Runtime Hardening Project Owner Plan Acceptance

## Status

ACCEPTED — PHASE 1B.3-B5-C PLAN APPROVED

## Accepted Plan

- B5-C plan commit:
  d1d718640bff58bec4fd487c98f4e2576328bc85
- B5-B Project Owner backend implementation acceptance commit:
  c42734e351404d9788b82e2049c92f6de09baf18
- B5-B backend implementation commit:
  0394379ca343906bb8560dc0359fb853dc3b658a
- B5-B scope authorization commit:
  563503ce88f283d8483e1fc1852acf469427a31b
- B5 plan acceptance commit:
  f13afa48ecfaa8fa190137164b1a49ba70dee06e

## Project Owner Decision

The Project Owner accepts the Phase 1B.3-B5-C frontend runtime hardening discovery and detailed plan.

## Accepted Frontend Scope

Authorize future B5-C implementation for frontend-only runtime hardening covering:

- My Requests UI.
- Action History / Timeline UI.
- Reject UX.
- Execution Retry UX.
- Frontend API client and type updates.
- Frontend route/navigation updates.
- Frontend permission visibility gating for:
  - WORKFLOW_REJECT
  - WORKFLOW_RETRY_EXECUTION
- Frontend tests for the approved B5-C UI behavior.

## Accepted API Contract Mapping

Confirm B5-C frontend implementation may consume:

- My Requests UI:
  GET /api/v2/workflows/my-requests

- Action History UI:
  GET /api/v2/workflows/instances/{instanceId}/actions

- Reject UX:
  POST /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reject

- Retry UX:
  POST /api/v2/workflows/instances/{instanceId}/retry-execution

## Accepted Security Rules

Confirm:
- Backend authorization remains authoritative.
- Frontend permission gating is usability only, not security.
- Do not display raw PayloadJson.
- Do not display BeforeDataJson.
- Do not display sensitive customer fields.
- Do not display stack traces.
- Do not display SQL/internal exception details.
- Use sanitized user-facing errors only.
- Reject reason/comment is required in UI.
- Retry confirmation must make clear that retry is only for failed execution.
- UI must refresh state after reject/retry actions.

## Accepted Implementation Shape

Accept the plan recommendation for single bounded implementation: B5-C may be implemented as one bounded frontend implementation commit covering API client/types, My Requests page, Action History panel, Reject dialog, Retry button, route/navigation updates, and all tests.

## Authorized Next Step

Authorize:

Phase 1B.3-B5-C Frontend Runtime Hardening Implementation

Implementation authorization:
Project Owner authorizes B5-C frontend runtime hardening implementation only within the accepted frontend scope above.

## Explicitly Not Authorized

B5-C implementation must not include:

- Backend code changes.
- Backend test changes.
- Migration changes.
- Rollback changes.
- Database script changes.
- PermissionCodes.cs changes.
- permission-catalog.md changes.
- business-rules.md changes.
- acceptance-criteria.md changes.
- Production release.
- Service module.
- Payment module.
- CUSTOMER_MASTER_CHANGE.
- Customer merge.
- Card flow.
- Plot flow.
- ENTITY expansion.
- Export/download.
- Broad frontend redesign.
- User lookup/reassign expansion beyond existing accepted/deferred scope.
- Any unrelated business behavior.

## Required B5-C Implementation Evidence

Future B5-C implementation must provide:

- cd src/frontend
- npx oxlint
- npx tsc -b
- npx vitest run

Also run backend API regression if frontend implementation reveals contract uncertainty:

- dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false

## Required B5-C Review Path

- B5-C frontend implementation commit.
- B5-C frontend implementation acceptance review.
- Project Owner B5-C frontend implementation acceptance.
- Then proceed to B5-D operational validation and closure planning.

## Stop Conditions

Stop implementation if:

- frontend requires backend API changes,
- frontend requires PermissionCodes.cs changes,
- frontend requires business doc changes,
- endpoint response DTOs are unclear,
- permission mapping is unclear,
- reject/retry semantics are unclear,
- raw payload or sensitive data would be exposed,
- tests cannot run cleanly,
- implementation scope expands beyond accepted B5-C frontend scope.

## Conclusion

PHASE 1B.3-B5-C PLAN ACCEPTED — READY FOR B5-C FRONTEND RUNTIME HARDENING IMPLEMENTATION
