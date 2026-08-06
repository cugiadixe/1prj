# Phase 1B.1-O Project Owner Plan Acceptance

Status:
ACCEPTED — IMPLEMENTATION AUTHORIZED

Accepted phase:
Phase 1B.1-O — Audit Viewer UI

Accepted plan commit:
ebae3a2ba3b4de1527f5e8b26ac276176da9183a

Plan acceptance baseline:
ebae3a2ba3b4de1527f5e8b26ac276176da9183a

Previous completed phase:
Phase 1B.1-N COMPLETE

## Approved decisions:

**DEC-1B-O-01 — Phase shape:**
Accepted. Phase O will focus on read-only Audit Viewer UI.

**DEC-1B-O-02 — Authorization gate:**
Accepted. Audit Viewer UI is gated by SECURITY_AUDIT_VIEW GLOBAL.

**DEC-1B-O-03 — Audit domains:**
Accepted. Phase O starts with security audit only, using the existing safe SecurityAuditController backend surface.

**DEC-1B-O-04 — Scope:**
Accepted. Audit Viewer is GLOBAL-only in Phase O.

**DEC-1B-O-05 — Current company context:**
Accepted. Phase O does not depend on current company context. No silent filtering by selected company. Company filtering remains unavailable unless backend safely supports it later.

**DEC-1B-O-06 — Detail view:**
Accepted. Detail view may show only safe, redacted, approved SecurityAuditEventDto fields. No raw payload display.

**DEC-1B-O-07 — Read audit:**
Accepted. No new read audit event is required in Phase O.

**DEC-1B-O-08 — Export:**
Accepted. Export/download is deferred.

**DEC-1B-O-09 — Backend changes:**
Accepted. No backend changes are expected because existing GET /api/v2/security/audit-events is sufficient. If a gap is discovered, stop and report blocker before implementing backend changes.

**DEC-1B-O-10 — Permission catalog:**
Accepted. No new permission code is added. Use existing SECURITY_AUDIT_VIEW.

**DEC-1B-O-11 — Sensitive data:**
Accepted. UI may expose only safe DTO fields. If raw secret/token/password/security stamp/SQL/exception payload exposure is discovered, stop and report blocker.

**DEC-1B-O-12 — Deferred items:**
Accepted. Retention/archive/export/business audit expansion remain deferred.

## Accepted backend basis:
- Use existing SecurityAuditController.
- Use existing GET /api/v2/security/audit-events.
- Use existing SecurityAuditQueryParameters.
- Use existing SecurityAuditEventDto.
- Preserve backend authorization with SECURITY_AUDIT_VIEW GLOBAL.
- Preserve read-only access.
- Do not add schema migration.
- Do not add rollback migration.
- Do not add new production permission code.
- Do not modify PermissionCodes.cs.
- Do not modify permission-catalog.md.

## Accepted frontend scope:
- Add Audit Viewer page under Security/Admin area.
- Gate route/menu with SECURITY_AUDIT_VIEW GLOBAL.
- Show read-only paginated audit event table.
- Use backend-supported filters only:
  - date range
  - actor
  - target
  - event type
  - entity type/id
  - correlation id
- Do not implement company filter unless backend support already exists.
- Show safe audit details only from SecurityAuditEventDto.
- Show sanitized loading/failure states.
- Keep backend as authoritative.

## Accepted out-of-scope:
- Business audit expansion.
- Audit export/download.
- Audit mutation/edit/delete.
- Audit retention/archive management.
- Raw payload display.
- Raw SQL display.
- Raw exception detail display.
- Token/session/password/security stamp exposure.
- Role/Admin Group Management UI.
- Department Baseline Permission UI.
- Approval workflow.
- Business modules.
- Schema migration.
- Rollback migration.
- New production permission code.
- PermissionCodes.cs change.
- permission-catalog.md change.
- Frontend-only authorization enforcement.

## Implementation authorization:
Phase 1B.1-O implementation is authorized under the accepted scope and decisions above.

PHASE 1B.1-O IMPLEMENTATION ACCEPTED — SEE phase-1b1o-project-owner-implementation-acceptance.md
