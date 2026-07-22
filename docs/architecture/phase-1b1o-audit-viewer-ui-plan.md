# Phase 1B.1-O Audit Viewer UI Plan

Status:
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

Baseline:
d4580c96f397804fa641a886ebdad5c234ef677d

Previous completed phase:
Phase 1B.1-N COMPLETE

## 1. Purpose
Plan the development of the Audit Viewer UI under the Security/Admin area. This phase implements a read-only interface to review security audit events while strictly adhering to security, architectural, and data exposure boundaries.

## 2. Confirmed current state
- The backend `SecurityAuditController` is already implemented.
- `SECURITY_AUDIT_VIEW` permission is correctly registered in the system and `permission-catalog.md`.
- No backend API modifications are necessary to achieve the core requirements.

## 3. Audit backend discovery
- Backend endpoint `GET /api/v2/security/audit-events` exists and supports pagination.
- Security audit and business audit are separate; this phase utilizes the security audit endpoint.
- Audit records are strictly read-only and append-only at the backend level.
- Filtering is supported via `SecurityAuditQueryParameters` for date ranges, actors, targets, event types, entity types/IDs, and correlation IDs.
- `CompanyId` filtering is not supported by the query parameters; filtering is inherently `GLOBAL`.

## 4. Frontend security UI discovery
- The system supports secure routing gated by permissions and scopes.
- Account discovery components and hooks are available for potential reuse in actor/target filtering.

## 5. Proposed backend scope
- Zero backend modifications. 
- No schema migrations.
- No rollback migrations.
- No changes to `PermissionCodes.cs` or `permission-catalog.md`.

## 6. Proposed frontend scope
- Create `AuditViewerPage` under a new route (e.g., `/security/audit`).
- Implement a paginated data table to display security audit events.
- Implement UI filters mapping exactly to `SecurityAuditQueryParameters`.
- Provide a detail view (modal or expandable row) containing only the safe, redacted fields present in `SecurityAuditEventDto`.

## 7. Authorization and permission-gating strategy
- The route and menu link will be strictly gated by `SECURITY_AUDIT_VIEW` at the `GLOBAL` scope.
- The backend natively enforces `[RequirePermission(PermissionCodes.SecurityAuditView, PermissionScope.Global)]`.

## 8. Audit domain strategy
- Phase O focuses exclusively on the Security Audit domain, utilizing the existing secure backend endpoint.

## 9. Filtering and pagination strategy
- Filters will map 1:1 to the backend parameters: `FromUtc`, `ToUtc`, `EventType`, `ActorUserId`, `TargetUserId`, `EntityType`, `EntityId`, and `CorrelationId`.
- Pagination parameters (`Page`, `PageSize`) will be driven by the UI table controls.

## 10. Detail and payload exposure strategy
- The UI will only expose structured properties returned by `SecurityAuditEventDto` (e.g., Reason, Outcome, CorrelationId).
- A detail endpoint `GET /{id}` is not required since the list endpoint DTO contains the full safe payload.

## 11. Sensitive data and redaction strategy
- The backend `SecurityAuditEventDto` is already structurally safe. 
- It does not contain raw SQL, raw exception details, tokens, password hashes, or security stamps. 
- The UI is inherently safe from exposing unredacted payloads because the backend strips them at the API boundary.

## 12. Current company context strategy
- The Audit Viewer UI operates exclusively at the `GLOBAL` level.
- Current company context will not be used to silently filter results, preventing confusion or hidden records for global administrators.

## 13. Audit read-event strategy
- Viewing audit logs via this UI will not generate new read-audit events in Phase O, deferring to existing backend policy.

## 14. Error handling strategy
- Errors will be sanitized; no raw backend exceptions will be exposed.
- Validation problems from the API (e.g., date ranges, pagination limits) will be parsed and displayed as user-friendly toast messages.

## 15. Test strategy
- Add unit tests for the `AuditViewerPage` validating route gating.
- Add tests confirming filters and pagination controls update the API request correctly.
- Add tests ensuring no mutation attempts (POST/PUT/DELETE) are possible from the UI.

## 16. Explicit out-of-scope
- Audit export/download.
- Audit retention/archive management.
- Audit mutation/edit/delete.
- Audit payload redesign.
- Security audit write redesign.
- Business audit write redesign.
- Role/Admin Group Management UI.
- Department Baseline Permission UI.
- Approval workflow.
- Business modules.
- Schema migration.
- Rollback migration.
- New production permission code.
- Permission catalog redesign.

## 17. Required Project Owner decisions

- **DEC-1B-O-01 — Phase shape:** Should Phase O focus on read-only Audit Viewer UI? (Recommended: Yes. Keep it read-only.)
- **DEC-1B-O-02 — Authorization gate:** Which permission gates Audit Viewer UI? (Recommended: SECURITY_AUDIT_VIEW GLOBAL.)
- **DEC-1B-O-03 — Audit domains:** Should Phase O show security audit only, business audit only, or both? (Recommended: Security audit only based on safe existing API.)
- **DEC-1B-O-04 — Scope:** Should Audit Viewer be GLOBAL-only in Phase O? (Recommended: Yes. Company filtering is not supported by the backend.)
- **DEC-1B-O-05 — Current company context:** Should Phase O depend on current company context? (Recommended: No.)
- **DEC-1B-O-06 — Detail view:** Should audit detail payload be shown? (Recommended: Only safe, redacted, approved fields from the DTO.)
- **DEC-1B-O-07 — Read audit:** Should viewing audit logs itself create read audit events? (Recommended: No.)
- **DEC-1B-O-08 — Export:** Should export/download be included? (Recommended: No. Defer export.)
- **DEC-1B-O-09 — Backend changes:** Should backend changes be allowed? (Recommended: No. Existing API is sufficient.)
- **DEC-1B-O-10 — Permission catalog:** Should a new permission code be added? (Recommended: No. Use existing SECURITY_AUDIT_VIEW.)
- **DEC-1B-O-11 — Sensitive data:** What happens if existing audit payload may contain secrets? (Recommended: Not applicable; backend DTO prevents this.)
- **DEC-1B-O-12 — Deferred items:** Should retention/archive/export/business audit expansion remain deferred? (Recommended: Yes.)

## 18. Blockers, if any
None. Existing backend API and permissions fully support a frontend-only implementation.

## 19. Recommended implementation slices
1. **API Client:** Create `auditApi.ts` matching `GET /api/v2/security/audit-events`.
2. **UI Component:** Build `AuditViewerPage.tsx` with Ant Design Table and Filters.
3. **Integration:** Register route and navigation menu item gated by `SECURITY_AUDIT_VIEW`.

## 20. Acceptance criteria
- Audit Viewer UI is gated by SECURITY_AUDIT_VIEW GLOBAL.
- Backend authorization remains authoritative.
- Audit records are read-only from UI.
- No audit mutation/edit/delete UI.
- No raw SQL exposure.
- No raw exception detail exposure.
- No token/session/password/security stamp exposure.
- Detail view shows only safe approved fields.
- Pagination is used.
- Filters are supported only where backend API safely supports them.
- No export/download unless separately approved.
- No schema migration unless separately approved.
- No rollback migration unless separately approved.
- No new permission code unless separately approved.
- No PermissionCodes.cs change unless separately approved.
- No permission-catalog.md change unless separately approved.
- Existing auth, current permissions, current company, account management, permission assignment, and mustChangePassword tests remain passing.
- Frontend tests cover SECURITY_AUDIT_VIEW gating, sanitized errors, read-only behavior, safe detail display, pagination/filter behavior, and absence of mutation controls.
