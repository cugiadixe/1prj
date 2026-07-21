# Phase 1B.1-H Project Owner Plan Acceptance

**Status**: ACCEPTED — PHASE 1B.1-H PLAN APPROVED FOR IMPLEMENTATION

**Accepted plan commit**: `e8342ec884889f732a81a4b3f6e1c0ef9e21bddd`

**Accepted baseline**: `9d1ba2258ac6e6b7de534aeb4ea6f2db62e85f2c`

**Accepted phase**: Phase 1B.1-H — Security Audit Read / SECURITY_AUDIT_VIEW

## Accepted scope
- Backend-only read API for security audit events.
- Endpoint: `GET /api/v2/security/audit-events`.
- Enforce `SECURITY_AUDIT_VIEW`.
- `GLOBAL` permission scope for Phase H.
- Direct ADO.NET read-only querying by default.
- No Dapper dependency unless separately approved.
- No mutable EF entity for `Security_Audit_Events`.
- Safe response DTOs.
- Pagination with maximum `pageSize` cap.
- Deterministic sorting.
- Safe filters:
  - `fromUtc`
  - `toUtc`
  - `eventType`
  - `actorUserId`
  - `targetUserId`
  - `entityType`
  - `entityId`
  - `correlationId`
  - `page`
  - `pageSize`
- Sanitized 400/403/problem responses.
- Unit, integration, API, and DatabaseSafety tests.

## Accepted data exposure strategy
- MVP response excludes raw JSON state fields:
  - `before_state_json`
  - `after_state_json`
  - `changed_fields`
- These fields may be reconsidered only after explicit redaction rules are approved.
- Metadata fields may be returned if safe.
- No password, token, secret, hash, security stamp, or raw request payload may be exposed.

## Accepted out-of-scope
- Frontend audit viewer.
- Security Admin UI.
- Permission assignment UI.
- CSV/PDF export.
- Audit retention/archive/purge.
- Audit update/delete.
- `SECURITY_AUDIT_VIEW` assignment UI.
- SIEM integration.
- Production dashboards.
- Business modules.
- Schema migration unless separately approved.

## Accepted decisions

> [!IMPORTANT]
> **DEC-1B-H-01 — Endpoint scope:**
> - Implement backend-only `GET /api/v2/security/audit-events`.
> - No frontend in Phase H.

> [!IMPORTANT]
> **DEC-1B-H-02 — Permission boundary:**
> - `SECURITY_AUDIT_VIEW` is GLOBAL-only for Phase H.

> [!IMPORTANT]
> **DEC-1B-H-03 — Query filters:**
> - MVP filters are accepted:
>   - date range
>   - event type
>   - actor user id
>   - target user id
>   - entity type
>   - entity id
>   - correlation id
>   - page
>   - pageSize

> [!IMPORTANT]
> **DEC-1B-H-04 — Response redaction:**
> - MVP excludes `before_state_json`, `after_state_json`, and `changed_fields`.
> - Raw JSON state fields are deferred until explicit redaction rules are approved.

> [!IMPORTANT]
> **DEC-1B-H-05 — Audit the audit-read:**
> - Defer `SECURITY_AUDIT_VIEWED` / audit-read self-auditing for Phase H.
> - Do not create recursive audit volume in MVP.

## Implementation authorization
Phase 1B.1-H implementation may begin after this Project Owner plan acceptance is committed.
