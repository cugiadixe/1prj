Title:
Phase 1B.1-L Company Scope Blocker Decision

Status:
DECIDED — PHASE 1B.1-L IMPLEMENTATION MAY RESUME WITH SCOPED CONTRACT

Related phase:
Phase 1B.1-L — Current User Permissions API and Frontend Permission Awareness

Baseline:
a9c331cc435c26e53b2eeba98eefc077470cdc55

Related plan commit:
72621f69a45bed406b40f3d4249cc5c2cdaefd0b

Related plan acceptance commit:
a9c331cc435c26e53b2eeba98eefc077470cdc55

Blocker discovered:
- Existing IPermissionEvaluator calculates effective permissions for a specific user and optional companyId.
- COMPANY-scoped permission results require a concrete company context.
- Returning all COMPANY-scoped permissions across all companies would require evaluator redesign or cross-company aggregation.
- That redesign was not approved in the Phase 1B.1-L plan acceptance.
- Implementation correctly stopped before code changes.

Decision:
- Do not redesign PermissionEvaluator in Phase 1B.1-L.
- Do not aggregate permissions across all companies in Phase 1B.1-L.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add a new permission code.
- Do not change PermissionCodes.cs.
- Do not change permission-catalog.md.
- Do not add read audit events.

Accepted endpoint behavior:
GET /api/v2/auth/me/permissions

Without X-Company-Id:
- Return GLOBAL effective permissions only.
- COMPANY-scoped permissions are not returned without a company context.

With valid X-Company-Id:
- Return GLOBAL effective permissions plus COMPANY-scoped effective permissions for the supplied company context.
- Preserve existing company-context validation behavior.
- Preserve DENY-wins behavior.

With invalid or unauthorized X-Company-Id:
- Return sanitized error according to existing company-context/authorization behavior.
- Do not expose internal assignment details.

Accepted response shape remains:
- permissions: array
  - permissionCode
  - scope
  - companyId nullable

Accepted frontend behavior:
- Account Management nav gating uses SECURITY_ACCOUNT_MANAGE GLOBAL only.
- Therefore Phase 1B.1-L frontend can hide/show Account Management using GLOBAL permissions from the endpoint without X-Company-Id.
- COMPANY-scoped UI gating is deferred until a broader current-company UX/context strategy is approved.
- Frontend permission gating remains advisory only.
- Backend remains authoritative for every protected API.
- Deep links must still rely on backend enforcement and sanitized 403 handling.

Accepted exclusions:
- No all-company permission aggregation.
- No evaluator redesign.
- No current-company selector UX.
- No company-switching UX.
- No Permission Assignment UI.
- No Role/Group Management UI.
- No Audit Viewer UI.
- No Dynamic Approval Workflow.

Implementation authorization:
- Phase 1B.1-L implementation may resume using the scoped endpoint contract above.
- Implementation must stop again if schema migration, new permission codes, PermissionCodes.cs changes, permission-catalog.md changes, or evaluator redesign become necessary.

Conclusion:
PHASE 1B.1-L BLOCKER RESOLVED — IMPLEMENTATION MAY RESUME WITH SCOPED CONTRACT
