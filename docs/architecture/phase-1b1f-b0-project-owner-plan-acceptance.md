# Phase 1B.1-F-B0 Project Owner Plan Acceptance

## Status
PHASE 1B.1-F-B0 PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## Accepted F-B0 plan commit
5fa9c7dfdfc075f2e711171e5f032709414f7fdd

## Current accepted baseline
e783fd349c2a4a43938715ea8a2c18cb3b3a8389

## Accepted corrective slice
Phase 1B.1-F-B0 — SECURITY_ADMIN_MANAGE Seed Backfill

## Accepted discovery findings
- SECURITY_ADMIN_MANAGE exists in PermissionCodes.cs.
- SECURITY_ADMIN_MANAGE exists in permission-catalog.md.
- SECURITY_ADMIN_MANAGE is absent from V0003 database seed data.
- SECURITY_AUDIT_VIEW is present in V0003.
- F-B Initial Admin Bootstrap is blocked by OD-F-B-06 until SECURITY_ADMIN_MANAGE exists in the database.
- V0003 must not be amended.
- A new V0004/U0004 corrective migration is required.
- F-B implementation remains blocked until F-B0 implementation is accepted.

## Accepted decisions

**OD-F-B0-01 — Corrective migration:**
Create a new V0004 migration to backfill SECURITY_ADMIN_MANAGE into dbo.Permissions.
Do not amend V0003.

**OD-F-B0-02 — Rollback:**
Create matching U0004 rollback.
Rollback must remove only the SECURITY_ADMIN_MANAGE row introduced by V0004 when safe.
If existing references prevent safe deletion, rollback must fail safely or follow the repository rollback convention with clear documentation.

**OD-F-B0-03 — Permission values:**
Use the existing permission-catalog.md row as source of truth:
- permission_code: SECURITY_ADMIN_MANAGE
- category/domain: SECURITY
- action/capability: ADMIN_MANAGE
- scope: GLOBAL
- sensitive: Yes
- delegable: No
- purpose: manage security administration configuration

Map these values to the actual dbo.Permissions table columns discovered from V0003.

**OD-F-B0-04 — Idempotency:**
V0004 should follow the repository migration style.
Use an IF NOT EXISTS guard if consistent with the existing migration convention.
Do not create duplicate permission rows.

**OD-F-B0-05 — Tests:**
Add or update database/schema tests verifying SECURITY_ADMIN_MANAGE exists after migrations are applied.
DatabaseSafety must remain green.

**OD-F-B0-06 — Scope boundary:**
F-B0 must not implement bootstrap.
F-B0 must not create ADMIN_SECURITY admin group.
F-B0 must not create users/auth accounts.
F-B0 must not create audit endpoints.
F-B0 must not enforce SECURITY_AUDIT_VIEW.
F-B0 must not modify PermissionCodes.cs or permission-catalog.md unless implementation discovery proves a documentation inconsistency.

**OD-F-B0-07 — F-B unblock condition:**
F-B implementation remains blocked until:
- F-B0 plan is accepted;
- F-B0 migration is implemented;
- F-B0 tests pass;
- F-B0 implementation is reviewed and accepted.

## Explicit non-authorization
- No implementation in this commit.
- No application code changes.
- No tests changed.
- No migration created.
- No rollback migration created.
- No V0003/U0003 modification.
- No F-B Bootstrap implementation.
- No PTKD.Bootstrap project.
- No ADMIN_SECURITY admin group creation.
- No user/auth account creation.
- No audit endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Next step
Create a separate Phase 1B.1-F-B0 implementation authorization to add V0004/U0004 and related tests.
F-B Bootstrap implementation must not resume until F-B0 implementation is accepted.
