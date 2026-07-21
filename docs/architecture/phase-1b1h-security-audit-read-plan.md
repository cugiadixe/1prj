# Phase 1B.1-H Security Audit Read Plan

**Status**: PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Plan acceptance**: PHASE 1B.1-H PLAN ACCEPTED — SEE [phase-1b1h-project-owner-plan-acceptance.md](phase-1b1h-project-owner-plan-acceptance.md)

**Implementation acceptance**: PHASE 1B.1-H IMPLEMENTATION ACCEPTED — SEE [phase-1b1h-project-owner-implementation-acceptance.md](phase-1b1h-project-owner-implementation-acceptance.md)

**Baseline**: `9d1ba2258ac6e6b7de534aeb4ea6f2db62e85f2c`

**Previous completed phase**: Phase 1B.1-G COMPLETE

## 1. Purpose
Provide a secure, read-only API endpoint for retrieving security audit events from the `Security_Audit_Events` table. This forms the foundation for security monitoring and future administration UI without exposing sensitive state changes or raw secrets.

## 2. Confirmed current state
- `SECURITY_AUDIT_VIEW` permission is already defined in `PermissionCodes.cs`.
- `SECURITY_AUDIT_VIEW` is cataloged in `permission-catalog.md` with `GLOBAL` scope.
- `SECURITY_AUDIT_VIEW` is seeded in the database via `V0003__create_security_schema.sql`.
- `Security_Audit_Events` table exists with comprehensive columns (including exact column names `before_state_json`, `after_state_json`, and `changed_fields`).
- No current API endpoint reads this table.
- No EF entity currently maps to `Security_Audit_Events`.

## 3. Proposed in-scope
- Backend read-only API endpoint for retrieving `Security_Audit_Events`.
- Enforce `SECURITY_AUDIT_VIEW` permission at the `GLOBAL` scope.
- Use direct ADO.NET by default, consistent with existing security audit/bootstrap SQL patterns. Do not introduce Dapper unless Dapper already exists in the repo or Project Owner separately approves adding it.
- Do not map `Security_Audit_Events` as a mutable EF entity. Read-only projection is acceptable.
- Offset-based pagination (`page`, `pageSize`).
- Safe filtering based on standard metadata.
- Safe response DTOs that intentionally omit potentially sensitive JSON state blobs.
- Comprehensive Unit, Integration, and API test coverage.

## 4. Explicit out-of-scope
- frontend (frontend audit viewer)
- export (CSV/PDF)
- retention/archive/purge mechanisms
- update/delete functionality (append-only immutable log)
- SIEM integration
- dashboards (production monitoring)
- Security Admin UI
- permission assignment UI
- schema migration unless separately approved
- business module auditing
- Modifying `PermissionCodes.cs` or `permission-catalog.md`

## 5. Proposed API shape
**Endpoint**: `GET /api/v2/security/audit-events`

**Query Parameters**:
- `fromUtc` (datetime)
- `toUtc` (datetime)
- `eventCode` (string)
- `actorUserId` (long)
- `targetUserId` (long)
- `entityType` (string)
- `entityId` (string)
- `correlationId` (uuid)
- `page` (int, default 1)
- `pageSize` (int, default 50)

**Response Model**:
Paged result containing items with:
- `id`, `created_at`, `actor_user_id`, `acting_as_user_id`, `target_user_id`, `company_id`
- `event_code`, `entity_type`, `entity_id`
- `outcome`, `reason`, `correlation_id`, `policy_version`

## 6. Permission boundary
- `[Authorize]`
- `[RequirePermission(PermissionCodes.SecurityAuditView, PermissionScope.Global)]`
- Access is strictly `GLOBAL` scope.

## 7. Data exposure/redaction strategy
- **Exposed**: Safe metadata fields (IDs, types, codes, outcomes, dates).
- **Excluded for MVP**: `before_state_json`, `after_state_json`, and `changed_fields` (also request metadata if applicable).
- *Reasoning*: These JSON fields risk containing PII or secrets. MVP response excludes raw JSON state fields. These fields may be reconsidered in a later phase only after explicit redaction rules are approved. Metadata fields may be returned if safe.

## 8. Pagination/filtering strategy
- Default sort: `created_at` DESC. Add a deterministic tie-breaker using the actual stable key column (`id` DESC).
- pageSize must have a maximum cap.
- Invalid page/pageSize/date range returns sanitized 400.
- Offset pagination via standard `page` / `pageSize` queries.
- Filtering via standard WHERE clauses (using parameters to prevent SQL injection).

## 9. Test strategy
- **Unit Tests**: Query request validation (e.g., valid date ranges, page size limits).
- **Integration Tests**: Verify queries translate correctly against the database and return correct filtered/paged subsets.
- **API Tests**: Verify `SECURITY_AUDIT_VIEW` authorization enforcement (403 without permission, 200 with permission).

## 10. Security risks
- **Data Leakage**: Risk of exposing `password_hash`, tokens, or PII if JSON state fields are returned. Mitigated by excluding them entirely in Phase H.
- **Audit Bomb/DoS**: Extremely broad queries (e.g., all events, no filters) crashing the system. Mitigated by strict maximum `pageSize` and mandatory pagination.
- **Recursion/Volume Explosion**: If reading the audit log generates an audit log, automated tools could cause an infinite loop. Mitigated by deferring self-auditing.

## 11. Required Project Owner decisions

> [!IMPORTANT]
> **DEC-1B-H-01 — Endpoint scope:**
> Should Phase H implement backend-only `GET /api/v2/security/audit-events` with no frontend?
> *(Recommendation: Yes, backend only)*

> [!IMPORTANT]
> **DEC-1B-H-02 — Permission boundary:**
> Should `SECURITY_AUDIT_VIEW` be GLOBAL-only for Phase H?
> *(Recommendation: Yes)*

> [!IMPORTANT]
> **DEC-1B-H-03 — Query filters:**
> Are the proposed MVP filters (date range, event type, actor, target, entity, correlation id) sufficient?
> *(Recommendation: Yes)*

> [!IMPORTANT]
> **DEC-1B-H-04 — Response redaction:**
> MVP response excludes raw JSON state fields: `before_state_json`, `after_state_json`, and `changed_fields`. These fields may be reconsidered in a later phase only after explicit redaction rules are approved. Metadata fields may be returned if safe. Do you approve this redaction strategy?
> *(Recommendation: Yes, exclude to guarantee zero secret leakage)*

> [!IMPORTANT]
> **DEC-1B-H-05 — Audit the audit-read:**
> Defer `SECURITY_AUDIT_VIEWED` / audit-read self-auditing for Phase H unless Project Owner explicitly requires it. Do not create recursive audit volume in MVP. Do you approve this deferral?
> *(Recommendation: Defer)*

## 12. Blockers
- None. `SECURITY_AUDIT_VIEW` is fully seeded and the audit table exists.

## 13. Recommended implementation slices
1. **Slice 1 (Core)**: Create `SecurityAuditEventDto`, Query Request Model, and ADO.NET/SQL query handler for the database.
2. **Slice 2 (API)**: Implement `SecurityAuditController.GetAuditEvents`, routing, validation, and endpoint integration.
3. **Slice 3 (Security)**: Apply `RequirePermission` attributes and write API authorization tests.

## 14. Acceptance criteria
- [ ] `GET /api/v2/security/audit-events` is accessible.
- [ ] Endpoint is protected by `SECURITY_AUDIT_VIEW` (Global).
- [ ] Supports filtering by date range, actor, target, entity, correlation ID, and event code.
- [ ] Supports pagination and deterministic sorting.
- [ ] Response explicitly excludes sensitive JSON state fields.
- [ ] Integration and API tests cover all functionality and boundaries.
