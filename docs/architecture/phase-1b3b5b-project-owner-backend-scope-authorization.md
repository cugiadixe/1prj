# Phase 1B.3-B5-B Backend Runtime Hardening Project Owner Scope Authorization

## Status
AUTHORIZED — B5-B BACKEND RUNTIME HARDENING SCOPE APPROVED

## Authorization baseline
- Current HEAD:
  f13afa48ecfaa8fa190137164b1a49ba70dee06e
- B5 plan acceptance commit:
  f13afa48ecfaa8fa190137164b1a49ba70dee06e
- B5 plan commit:
  c85a5a95974c97dacf08c70c3dd05cff4778b08e
- B5 next-work decision commit:
  7b127837ec1f92f46077f64076d0122ea733333d

## Blocker summary
B5-B backend implementation was stopped at the scope gate because the accepted plan identified backend hardening items that require explicit Project Owner authorization before code changes.

## Project Owner decision
The Project Owner authorizes B5-B backend runtime hardening implementation within the bounded scope below.

## Authorized backend implementation scope
- My Requests backend API.
- Action history/timeline backend API.
- Reject backend support.
- Execution failure retry backend support.
- User lookup/reassign backend support only as required for reassignment UX and only with safe, permission-scoped response data.
- DTOs and validators required for these backend APIs.
- Application service changes required for these backend APIs.
- Controller endpoints required for these backend APIs.
- Sanitized backend error handling.
- Security audit events required for reject, retry, reassignment, and sensitive workflow actions.
- Backend unit tests.
- Backend integration tests.
- Backend API tests.
- Migration/rollback tests if DB changes are implemented.

## Authorized permission scope
The Project Owner authorizes adding only the minimum permission codes required for B5-B backend runtime hardening.

### Authorized permission code candidates:
- WORKFLOW_REJECT
- WORKFLOW_RETRY_EXECUTION
- Additional user lookup/reassign permission only if the accepted implementation proves the existing WORKFLOW_REASSIGN_PENDING permission is insufficient.

### Rules:
- Reuse existing WORKFLOW_VIEW where sufficient.
- Reuse existing WORKFLOW_REASSIGN_PENDING for reassignment where sufficient.
- Do not add broader admin permissions.
- Do not add unrelated permission codes.
- Any new permission code must be added consistently to:
  - PermissionCodes.cs,
  - permission-catalog.md,
  - related tests/permission catalog verification if present.
- If a proposed permission code differs from the candidates above, stop and request Project Owner approval.

## Authorized business document scope
The Project Owner authorizes minimal updates required for B5-B to:
- business-rules.md,
- permission-catalog.md,
- acceptance-criteria.md.

### Allowed content is limited to:
- My Requests visibility rules.
- User-visible action history/timeline rules.
- Reject semantics and resulting workflow status.
- Execution failure retry ownership and idempotency rules.
- Safe user lookup/reassign rules.
- Safe payload exposure rules.
- Permission catalog entries for approved B5-B permission codes.
- Acceptance criteria for the approved B5-B backend behavior.

Do not change unrelated business requirements.

## Authorized database scope
The Project Owner authorizes DB/migration/rollback changes only if required for the approved B5-B backend behavior.

### Authorized DB change candidates:
- Workflow instance status/state support for Rejected.
- Workflow instance status/state support for ExecutionFailed if required by current schema/code.
- Any minimal indexes needed for My Requests query performance.
- Any minimal columns required for execution retry state if current schema cannot represent it safely.

### Rules:
- Prefer using existing Workflow_Actions and existing workflow instance/step tables if sufficient.
- Do not create a broad new audit system.
- Do not rewrite existing workflow schema.
- Preserve existing B4 CREATE_CUSTOMER data and behavior.
- Add V0008/U0008 only if DB changes are necessary.
- Rollback must be guarded and FK-safe.
- Update migration/rollback tests if DB changes are implemented.

## Authorized API scope
Add only API v2 endpoints required for B5-B backend hardening.

### Candidate endpoints may include:
- GET /api/v2/workflow/instances/my-requests
- GET /api/v2/workflow/instances/{instanceId}/actions
- POST /api/v2/workflow/instances/{instanceId}/steps/{stepId}/reject
- POST /api/v2/workflow/instances/{instanceId}/retry-execution
- GET /api/v2/workflow/reassignable-users or equivalent safe lookup endpoint if required

### Rules:
- Endpoint names may be adjusted to existing controller conventions.
- Do not add frontend endpoints.
- Do not add generic unsafe query endpoints.
- Do not expose raw PayloadJson.
- Do not expose BeforeDataJson.
- Do not expose sensitive customer fields.
- Preserve sanitized errors.

## Reject semantics authorization
The Project Owner authorizes reject as a terminal business decision distinct from return, withdraw, cancel, and deny.

### Required semantics:
- Reject is performed by an authorized current approver.
- Reject requires a non-empty comment/reason.
- Reject terminates the workflow instance.
- Rejected workflow must not proceed to execution.
- Rejected workflow must not create or modify the business entity.
- Reject action must be recorded in Workflow_Actions.
- Reject action must produce security/business audit as appropriate.
- Reject must be visible in safe action history.

## Execution retry authorization
The Project Owner authorizes backend execution retry for failed workflow execution only.

### Required semantics:
- Retry is allowed only after final approval when execution failed.
- Retry must not duplicate business records.
- Retry must preserve idempotency.
- Retry must be permission-protected.
- Retry must record action/audit.
- Retry must show sanitized failure details only.
- Retry must not expose exception stack traces, SQL errors, raw payload, or secrets.

## My Requests authorization
The Project Owner authorizes backend My Requests API.

### Required semantics:
- Current authenticated requester can see their own workflow instances.
- Response must contain safe metadata only.
- No raw payload exposure.
- Include status, process, workflow, submitted time, current step summary if safe, and business entity metadata if safe.
- No cross-user visibility unless already authorized by existing admin/workflow permissions.

## Action history/timeline authorization
The Project Owner authorizes backend user-visible action history.

### Required semantics:
- Use Workflow_Actions as the source where possible.
- Expose only safe user-visible action metadata.
- Distinguish user-visible history from technical/security audit.
- Do not expose raw payload, BeforeDataJson, internal exception details, or sensitive customer fields.
- Include actor display data only if safe and allowed.
- Preserve backend authorization.

## User lookup/reassign authorization
The Project Owner authorizes safe backend support for reassignment lookup only if needed by B5-B.

### Required semantics:
- Reuse existing WORKFLOW_REASSIGN_PENDING permission if sufficient.
- Return minimal user identity fields needed for selection.
- Do not expose sensitive HR/user data.
- Permission-scope the lookup.
- Audit reassignment actions.

## Explicitly not authorized
- Frontend implementation.
- Frontend tests.
- Production migration/release.
- Service module implementation.
- Payment module implementation.
- CUSTOMER_MASTER_CHANGE implementation.
- Customer merge implementation.
- Card flow implementation.
- Plot flow implementation.
- ENTITY scope expansion.
- Export/download features.
- Broad workflow engine rewrite.
- Replacement of existing direct customer create.
- Changing accepted B4 CREATE_CUSTOMER behavior beyond required B5 hardening.
- Any unrelated permission/business/database changes.

## Required B5-B evidence
- dotnet build src/backend/PTKD-ERP.sln
- dotnet test tests/backend/PTKD.UnitTests/
- dotnet test tests/backend/PTKD.IntegrationTests/
- dotnet test tests/backend/PTKD.ApiTests/
- Migration/rollback tests if DB changes are implemented.
- git diff --check clean.
- Exact committed file list.
- Explicit confirmation no frontend changes.

## Stop conditions
- Stop if implementation exceeds this authorized B5-B backend scope.
- Stop if any permission code outside the authorized candidates is needed.
- Stop if DB changes beyond the authorized candidates are needed.
- Stop if reject semantics cannot be implemented safely.
- Stop if retry idempotency cannot be guaranteed.
- Stop if safe action history exposure cannot be guaranteed.
- Stop if user lookup would expose unauthorized user data.
- Stop if frontend changes are required.
- Stop if production release is requested.

## Conclusion
PHASE 1B.3-B5-B BACKEND RUNTIME HARDENING SCOPE AUTHORIZED
