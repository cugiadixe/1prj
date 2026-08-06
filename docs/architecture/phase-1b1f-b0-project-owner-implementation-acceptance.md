# Phase 1B.1-F-B0 Project Owner Implementation Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-F-B0 SECURITY_ADMIN_MANAGE SEED BACKFILL COMPLETE

**Accepted implementation commit:**
599e394c36a91b7b8b50fabc9ae6efe301798f97

**Plan acceptance commit:**
f35b6385a931a7201d7f67d3bdc58c789dd0a44e

**Plan commit:**
5fa9c7dfdfc075f2e711171e5f032709414f7fdd

**Hard-stop record commit:**
e783fd349c2a4a43938715ea8a2c18cb3b3a8389

**Accepted corrective slice:**
Phase 1B.1-F-B0 — SECURITY_ADMIN_MANAGE Seed Backfill

**Accepted scope:**
- Added V0004 migration to seed SECURITY_ADMIN_MANAGE into dbo.Permissions.
- Added U0004 rollback for SECURITY_ADMIN_MANAGE.
- Updated MigrationRollbackTests for V0004/U0004 behavior.
- Updated SecuritySchemaTests to verify SECURITY_ADMIN_MANAGE exists and has expected values.
- Preserved V0003/U0003 unchanged.
- Preserved PermissionCodes.cs unchanged.
- Preserved permission-catalog.md unchanged.
- Did not implement F-B Bootstrap.

**Accepted migration behavior:**
- V0004 inserts only SECURITY_ADMIN_MANAGE.
- V0004 does not insert SECURITY_AUDIT_VIEW.
- V0004 does not insert any other permission.
- V0004 uses the actual dbo.Permissions schema and V0003 column mapping.
- V0004 uses catalog-consistent values:
  - permission_code: SECURITY_ADMIN_MANAGE
  - domain/category: SECURITY
  - action/capability: ADMIN_MANAGE
  - scope: GLOBAL
  - sensitive: true / 1
  - delegable: false / 0
  - purpose: manage security administration configuration
- Migration SQL does not depend on PermissionCodes.cs or permission-catalog.md at runtime.

**Accepted rollback behavior:**
- U0004 affects only SECURITY_ADMIN_MANAGE.
- U0004 does not affect SECURITY_AUDIT_VIEW.
- U0004 does not remove existing V0003 permissions.
- U0004 does not drop or alter tables.
- U0004 blocks rollback safely if SECURITY_ADMIN_MANAGE is referenced by assignment tables.
- U0004 uses deactivation/update strategy compatible with delete-prevention behavior.

**Accepted test evidence:**
- Targeted Permission tests: 14 passed, 0 failed.
- Targeted SecuritySchema tests: 36 passed, 0 failed.
- Targeted MigrationRollback tests: 2 passed, 0 failed.
- Targeted DatabaseSafety tests: 17 passed, 0 failed.
- Build: 0 warnings, 0 errors.
- UnitTests: 114 passed, 0 failed.
- IntegrationTests: 158 passed, 0 failed.
- ApiTests: 153 passed, 0 failed.
- DatabaseSafety re-run: 17 passed, 0 failed.

**Explicit exclusions:**
- No F-B Bootstrap implementation.
- No PTKD.Bootstrap project.
- No ADMIN_SECURITY group creation.
- No users/auth accounts created.
- No public bootstrap endpoint.
- No audit endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No V0003/U0003 modification.
- No application code change.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

**Blocker resolution:**
- The OD-F-B-06 blocker is resolved by adding SECURITY_ADMIN_MANAGE to the database migration state.
- F-B Bootstrap implementation may be re-authorized only after this acceptance commit is recorded.
- F-B implementation must still respect the secondary corrections:
  A. User_Auth_Accounts has no normalized_provider_subject column.
  B. Password_History FK column is account_id.
  C. Admin_Groups.name is NOT NULL and must be populated when creating ADMIN_SECURITY.
- F-B implementation must still satisfy transaction-safe BOOTSTRAP_ADMIN_CREATED audit behavior.

**Next step:**
Authorize Phase 1B.1-F-B Initial Admin Bootstrap implementation from the corrected baseline.
