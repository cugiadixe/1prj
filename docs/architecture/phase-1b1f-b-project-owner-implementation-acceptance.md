# Phase 1B.1-F-B Project Owner Implementation Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-F-B INITIAL ADMIN BOOTSTRAP COMPLETE

**Accepted implementation commit:**
98387397b973950a7c6691456561ed1bc5eecf71

**Parent / accepted baseline:**
206c2490f2d3ef553940ba9a3e8bac744b9cb42d

**Accepted corrective baseline:**
Phase 1B.1-F-B0 implementation acceptance commit:
206c2490f2d3ef553940ba9a3e8bac744b9cb42d

## Accepted scope:
- Added standalone PTKD.Bootstrap console project.
- Added PTKD.Bootstrap to the backend solution.
- Added test project references to PTKD.Bootstrap.
- Added ResetToV0004() to TestDatabaseFixture.
- Added Bootstrap unit tests.
- Added Bootstrap integration tests.
- Implemented explicit operator-invoked initial admin bootstrap.
- Implemented one-time bootstrap marker behavior.
- Implemented ADMIN_SECURITY group creation/use.
- Implemented SECURITY_ADMIN_MANAGE grant through admin group assignment.
- Implemented BOOTSTRAP_ADMIN_CREATED audit event.
- Did not implement any public API endpoint.

## Accepted bootstrap behavior:
- Explicit invocation only.
- Does not run from API startup.
- Reads protected secrets from environment variables.
- Rejects BOOTSTRAP_ADMIN_PASSWORD through command-line arguments.
- Rejects CONNECTION_STRING through command-line arguments.
- Does not print, log, persist, return, or audit plaintext password.
- Does not print raw database/unhandled exception messages.
- Uses sanitized generic output for database/unhandled failure.
- Uses deterministic exit codes:
  - 0 success
  - 1 validation/configuration failure
  - 2 already bootstrapped
  - 3 database/unhandled failure
- Creates initial user.
- Creates internal User_Auth_Accounts row.
- Does not use normalized_provider_subject.
- Does not use user_auth_account_id.
- Password_History uses account_id.
- must_change_password is true for initial admin.
- Admin_Groups.name is populated.
- Security_Bootstrap_State marker is updated.
- bootstrapped_by_user_id points to the created user.
- Already-bootstrapped state fails safely.
- Missing SECURITY_ADMIN_MANAGE fails safely and does not invent another permission.

## Accepted transaction and audit behavior:
- Bootstrap write sequence runs in one SERIALIZABLE SQL transaction.
- Security_Bootstrap_State check/update is protected by transaction/locking.
- User creation, auth account creation, password history, admin group assignment, permission grant, bootstrap state update, and success audit are atomic.
- BOOTSTRAP_ADMIN_CREATED is inserted using the same SqlConnection and SqlTransaction as bootstrap completion.
- SqlSecurityAuditWriter is not used through a separate independent connection for success audit.
- No orphaned BOOTSTRAP_ADMIN_CREATED success audit is allowed if bootstrap rolls back.
- Audit insert failure fails closed and rolls back bootstrap.
- Existing F-A SqlSecurityAuditWriter behavior remains unchanged.
- No UPDATE/DELETE/TRUNCATE path against Security_Audit_Events is added in bootstrap production code.
- Audit payload excludes plaintext password, connection string, token, secret, stack trace, and SQL details.

## Accepted test evidence:
- Targeted Bootstrap Unit tests: passed, 0 failed.
- Targeted Bootstrap Integration tests: passed, 0 failed.
- Targeted Audit tests: passed, 0 failed.
- Targeted Permission tests: passed, 0 failed.
- Targeted MigrationRollback tests: passed, 0 failed.
- Targeted DatabaseSafety tests: passed, 0 failed.
- Build: 0 errors, 0 warnings.
- UnitTests: 119 passed, 0 failed.
- IntegrationTests: 160 passed, 0 failed.
- ApiTests: 153 passed, 0 failed.
- DatabaseSafety re-run: 17 passed, 0 failed.
- git diff --check: clean.
- No tag.
- No push.

## Explicit exclusions:
- No public bootstrap endpoint.
- No API startup auto-run.
- No API controller changes.
- No audit read endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No migration.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No V0003/U0003 modification.
- No V0004/U0004 modification.
- No frontend.
- No business module implementation.
- No production deployment.
- No tag/push.

## Blocker resolution:
- Phase 1B.1-F-B is accepted.
- The initial admin bootstrap blocker is resolved for Phase 1B.1-F.
- F-A Audit Writer, F-B0 seed backfill, and F-B Initial Admin Bootstrap are now all implementation-accepted.
- Phase 1B.1-F can proceed to final closure review.

## Remaining deferred / out-of-scope items:
- SECURITY_AUDIT_VIEW read/audit viewer remains out of F-B scope.
- Initial Admin Login + Force Password Change end-to-end verification remains a possible next phase.
- Production secret provider / Key Vault operationalization remains future deployment concern if not already separately accepted.
- No production execution of bootstrap was performed by this implementation acceptance.

## Next step:
Perform Phase 1B.1-F final closure review and record final Phase F Project Owner acceptance.
