# Phase 1B.1-F-B Project Owner Plan Acceptance

## Status
PHASE 1B.1-F-B PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## Accepted plan commit
c1bb9491292cb082f9d583fe34cdb9d8abeb9545

## Current accepted baseline
fb023cfc7af188d11b6e2c621dd913e25574fd63

## Accepted phase
Phase 1B.1-F-B — Initial Admin Bootstrap

## Accepted slice sequencing
- F-A (Audit Writer foundation) is complete and accepted.
- F-B (Initial Admin Bootstrap) is the next authorized slice.
- F-B must be planned, implemented, reviewed, and accepted before any subsequent phase begins.
- No combined large implementation commit.

## Accepted decisions

**OD-F-B-01:**
Bootstrap delivered as dedicated internal console project: `src/backend/PTKD.Bootstrap`.
No public API endpoint. No API startup auto-run.

**OD-F-B-02:**
Use `Security_Bootstrap_State` as durable one-time bootstrap marker.
Bootstrap fails if a successful marker already exists.

**OD-F-B-03:**
Bootstrap password/secret comes from protected environment variable or protected command input.
No default password. No plaintext secret in config, logs, console output, database, audit payload, exception message, or test output.

**OD-F-B-04:**
Use existing approved password hasher and password policy.
Because current `UserAuthAccount.CreateInternal()` creates `MustChangePassword = false`, F-B must set
must-change-password by approved model behavior — such as `ReplacePassword()` or an approved factory
or update method — not by unsafe direct bypass.

**OD-F-B-05:**
Create the initial admin through the existing user/auth account model and `User_Auth_Accounts` provider design.
Do not bypass unique constraints or auth-account lifecycle rules.

**OD-F-B-06:**
Bootstrap creates or uses the ADMIN_SECURITY admin group if missing and grants `SECURITY_ADMIN_MANAGE`
through the existing admin group/permission assignment model.
Do not create new permission codes.
If `SECURITY_ADMIN_MANAGE` is missing from the database, stop and report rather than silently using
a different permission.

**OD-F-B-07:**
Bootstrap runs in a SERIALIZABLE transaction with appropriate locking.
No partial successful administrator state is allowed.

**OD-F-B-08:**
Bootstrap success emits `BOOTSTRAP_ADMIN_CREATED` audit event.
Success audit must be transactionally safe with bootstrap completion, or implemented with an approved
equivalent that prevents orphaned success audit.
If the required success audit cannot be written, bootstrap fails closed with a sanitized error.

**OD-F-B-09:**
Concurrent bootstrap execution must allow only one success.
All other concurrent attempts must fail safely and leave no partial admin state.

**OD-F-B-10:**
Bootstrap requires explicit operator invocation.
No startup auto-run. No production deployment or scheduled execution authorized by this plan.

**OD-F-B-11:**
Continue using `PTKD_TEST_PHASE1A2` for F-B tests.

**OD-F-B-12:**
Explicit exclusions remain:
- No public bootstrap endpoint.
- No audit read endpoint.
- No `SECURITY_AUDIT_VIEW` enforcement.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- No migration unless separately accepted.
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No line-ending normalization.

**OD-F-B-13:**
Use controlled persistence for `Security_Bootstrap_State` without schema change.
Preferred implementation: add a minimal EF mapping/entity only if needed for consistency, with raw SQL
locking where required for concurrency.
No migration unless a later accepted implementation review proves the schema is insufficient.

## Key implementation constraint noted by Project Owner

**OD-F-B-08 tightened:** The proposed plan documented an orphaned-audit risk (IAuditWriter commits
on a separate SqlConnection before the main transaction commits). The accepted decision requires the
implementation to eliminate or prevent that orphan — either by enrolling the audit write in the same
transaction, or by using an approved equivalent that guarantees no success audit without a completed
bootstrap. Implementation must resolve this before submitting for acceptance review.

## Explicit non-authorization

- No implementation in this commit.
- No application code changes.
- No tests changed.
- No migration.
- No seed/bootstrap.
- No PTKD.Bootstrap project created.
- No bootstrap endpoint.
- No audit read endpoint.
- No `SECURITY_AUDIT_VIEW` enforcement.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Next step
Create a separate Phase 1B.1-F-B implementation plan and then authorize the implementation task
only after this plan acceptance is committed.
