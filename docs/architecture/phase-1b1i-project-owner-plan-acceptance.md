# Phase 1B.1-I Project Owner Plan Acceptance

## Status
ACCEPTED — PHASE 1B.1-I PLAN APPROVED FOR IMPLEMENTATION

## Accepted plan commit
9f53f95fd1d68304699c8dec722c73aabd6ebdc0

## Accepted baseline
a8fdbe636e4a57429f8ba2d58652a27349a2989d

## Accepted planning umbrella
Phase 1B.1-I — Security Admin UI / Permission Management

## Accepted implementation scope name
Phase 1B.1-I — Account Management API Hardening

## Reason for backend-first scope

- Discovery confirmed the frontend has no login page, no auth state, no permission-gated routing, and no admin page foundation.
- Backend security admin APIs mostly exist, but Account Management APIs are completely absent.
- Therefore Phase I will complete the backend account management gap first.
- Frontend Security Admin UI is deferred to a later phase unless separately approved.

## Accepted scope

- Backend-only account management API hardening.
- No frontend.
- No Security Admin UI.
- No permission assignment UI.
- No audit viewer UI.
- Add only the minimum required backend account-management capability.
- Use existing security/auth/account patterns and infrastructure.
- Use existing database schema (no migration unless implementation discovery proves otherwise and task stops for approval).

## Accepted account-management endpoints

- View account detail.
- Activate account.
- Disable account.
- Lock account.
- Unlock account.
- Admin password reset.
- Revoke all sessions.

## Accepted permission boundary

- Use `SECURITY_ACCOUNT_MANAGE`.
- Do not create a new permission code.
- Add `PermissionCodes.cs` constant during implementation (approved by DEC-1B-I-04 below).
- Do not modify `permission-catalog.md` unless a real inconsistency is discovered and separately approved.
- Do not modify database seed/migration unless discovery proves the DB row is missing.

## Accepted admin password reset delivery

- Return temporary password in response body once only.
- Never log the temporary password.
- Never store the temporary password in plain text.
- Set `must_change_password = true`.
- Apply existing password policy (minimum/maximum length).
- Apply existing password history rules.
- Apply temporary password expiry where supported by existing account model (`temporary_password_expires_at = now + 24h`).
- Audit event must not contain password material (SEC-005).
- Response-body delivery is accepted only because no email/SMS/secret-delivery channel exists yet.

## Accepted reason requirement

Reason is required in the request body for the following operations:
- lock
- disable
- revoke-all-sessions
- admin password reset

Reason must be audit-safe and must not contain password material or secrets.

## Accepted audit strategy

- All account management mutations must write audit events via the existing transactional audit writer.
- Audit payload must not contain passwords, temporary passwords, tokens, secrets, hashes, raw request payloads, `security_stamp`, or exception details.
- Use existing audit writer patterns (fail-closed, same transaction as the state change).

## Accepted out-of-scope

- Frontend admin console.
- Login UI.
- Must-change-password UI.
- Security Admin UI.
- Permission assignment UI.
- Audit viewer UI.
- Audit export/reporting.
- Audit retention/archive/purge.
- Dynamic Approval Workflow.
- AD/LDAP.
- Bulk import/export.
- Business modules.
- New permission model redesign.
- SIEM integration.
- Production dashboards.

## Accepted decisions

**DEC-1B-I-01 — Phase shape:**
Approved Option A: Backend hardening only. Implement Account Management APIs. No frontend in Phase I.

**DEC-1B-I-02 — MVP screens:**
No frontend screens in Phase I. If a later frontend phase is opened, Login + MustChangePassword foundation should come before admin console pages.

**DEC-1B-I-03 — Admin password reset delivery:**
Approved one-time temporary password returned in response body with strict safeguards. No logging or audit exposure of password material. Response-body delivery is accepted only because no email/SMS/secret-delivery channel exists yet.

**DEC-1B-I-04 — Account management permission code:**
Approved `SECURITY_ACCOUNT_MANAGE`. Add `PermissionCodes.cs` constant during implementation. Do not create a new permission code. Do not create migration unless DB/catalog evidence contradicts discovery.

**DEC-1B-I-05 — Audit viewer UI:**
Defer audit viewer UI. Phase H API remains backend-only.

**DEC-1B-I-06 — Audit target field convention:**
Use `entity_id` convention for target account/user identifier. No migration.

**DEC-1B-I-07 — Reason required:**
Reason is required for lock, disable, revoke-all-sessions, and admin password reset. Reason must be included in audit-safe metadata.

## Implementation authorization

Phase 1B.1-I implementation may begin after this Project Owner plan acceptance is committed.
