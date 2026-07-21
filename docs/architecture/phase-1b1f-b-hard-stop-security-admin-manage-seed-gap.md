# Phase 1B.1-F-B Hard Stop — SECURITY_ADMIN_MANAGE Database Seed Gap

## Status

BLOCKED — CORRECTIVE V0004 SLICE REQUIRED BEFORE F-B IMPLEMENTATION

## Current HEAD

7ec1f5f

## Blocked slice

Phase 1B.1-F-B — Initial Admin Bootstrap

---

## Hard stop summary

- F-B implementation discovery stopped before code was written.
- No application code was written.
- No tests were written.
- No migration was created.
- Working tree remained clean at discovery time.
- SECURITY_ADMIN_MANAGE exists in PermissionCodes.cs and permission-catalog.md
  but is not present in V0003 database seed data.
- OD-F-B-06 (accepted) requires: if SECURITY_ADMIN_MANAGE is missing from the
  database, stop and report rather than silently routing to a different
  permission.

---

## Evidence

### V0003 permission seed — 15 rows, SECURITY_ADMIN_MANAGE absent

V0003 seeds exactly 15 permission codes via a single INSERT INTO dbo.Permissions
block (lines 134–151 of V0003__create_security_schema.sql):

| # | permission_code               | module_code  | data_scope |
|---|-------------------------------|--------------|------------|
|  1 | ORGANIZATION_COMPANY_VIEW    | ORGANIZATION | GLOBAL     |
|  2 | ORGANIZATION_COMPANY_MANAGE  | ORGANIZATION | GLOBAL     |
|  3 | ORGANIZATION_DEPARTMENT_VIEW | ORGANIZATION | GLOBAL     |
|  4 | ORGANIZATION_DEPARTMENT_MANAGE | ORGANIZATION | GLOBAL   |
|  5 | SECURITY_USER_VIEW           | SECURITY     | GLOBAL     |
|  6 | SECURITY_USER_MANAGE         | SECURITY     | GLOBAL     |
|  7 | SECURITY_ASSIGNMENT_MANAGE   | SECURITY     | COMPANY    |
|  8 | SECURITY_ROLE_VIEW           | SECURITY     | GLOBAL     |
|  9 | SECURITY_ROLE_MANAGE         | SECURITY     | GLOBAL     |
| 10 | SECURITY_PERMISSION_VIEW     | SECURITY     | GLOBAL     |
| 11 | SECURITY_PERMISSION_MANAGE   | SECURITY     | GLOBAL     |
| 12 | SECURITY_ACCOUNT_MANAGE      | SECURITY     | GLOBAL     |
| 13 | SECURITY_ADMIN_GROUP_VIEW    | SECURITY     | GLOBAL     |
| 14 | SECURITY_ADMIN_GROUP_MANAGE  | SECURITY     | GLOBAL     |
| 15 | SECURITY_AUDIT_VIEW          | SECURITY     | GLOBAL     |

**SECURITY_ADMIN_MANAGE is absent from this list.**
**SECURITY_AUDIT_VIEW is present (row 15).**

### PermissionCodes.cs — SECURITY_ADMIN_MANAGE present

File: src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs, line 12:

    public const string SecurityAdminManage = "SECURITY_ADMIN_MANAGE";

### permission-catalog.md — SECURITY_ADMIN_MANAGE present

File: docs/business/permission-catalog.md, line 58:

    | SECURITY_ADMIN_MANAGE | SECURITY | ADMIN_MANAGE | GLOBAL | Yes | No |
    Manage security administration configuration (Roles, AdminGroups,
    Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions).

### Accepted decision OD-F-B-06

From phase-1b1f-b-project-owner-plan-acceptance.md:

> Bootstrap creates or uses the ADMIN_SECURITY admin group if missing and
> grants SECURITY_ADMIN_MANAGE through the existing admin group/permission
> assignment model. Do not create new permission codes. If SECURITY_ADMIN_MANAGE
> is missing from the database, stop and report rather than silently using a
> different permission.

---

## Decision options reviewed

### Option A — Create new V0004/U0004 migration (Selected)

- Create V0004 migration to INSERT SECURITY_ADMIN_MANAGE into dbo.Permissions
  using the existing Permissions table schema and the V0003 seed style.
- Create U0004 rollback to DELETE the SECURITY_ADMIN_MANAGE row inserted by
  V0004, conditioned on no FK references existing (safe because no
  Admin_Group_Permissions rows reference it until F-B bootstrap runs).
- Add or update database safety tests verifying SECURITY_ADMIN_MANAGE is
  present after the migration is applied.
- Recommended. Selected by Project Owner.

### Option B — Amend V0003 (Rejected)

- Amend the V0003 migration to include SECURITY_ADMIN_MANAGE in the seed block.
- Rejected. V0003 has been applied outside disposable local tests. Amending an
  applied migration creates a permanent lineage inconsistency between the
  committed migration file and any database on which V0003 has already been
  executed.
- Not selected.

### Option C — Revise OD-F-B-06 to use a different existing permission (Rejected)

- Change the accepted decision to use one of the 15 existing permissions
  (e.g., SECURITY_ADMIN_GROUP_MANAGE) instead of SECURITY_ADMIN_MANAGE.
- Rejected. This contradicts the accepted security decision and the
  permission-catalog definition. SECURITY_ADMIN_MANAGE is the specifically
  designated permission for security administration access. Substituting another
  permission would silently introduce a scope mismatch and violate GOV-007.
- Not selected.

---

## Selected corrective direction

Create Phase 1B.1-F-B0 — SECURITY_ADMIN_MANAGE database seed backfill:

- F-B0 is a separate corrective slice that must be planned and accepted before
  its migration is implemented.
- F-B implementation remains blocked until F-B0 is implemented and accepted.

---

## F-B0 proposed scope

### In scope

- Create database/migrations/V0004__seed_security_admin_manage_permission.sql
  to INSERT one row into dbo.Permissions for SECURITY_ADMIN_MANAGE:

      permission_code = 'SECURITY_ADMIN_MANAGE'
      module_code     = 'SECURITY'
      action_code     = 'ADMIN_MANAGE'
      data_scope      = 'GLOBAL'
      is_sensitive    = 1
      requires_reason = 1
      is_delegable    = 0
      is_active       = 1
      description     = N'Manage security administration configuration
                          (Roles, AdminGroups, Permissions, UserAssignments,
                          DepartmentPermissions, EffectivePermissions).'

  The description must match permission-catalog.md exactly.

- Create database/rollbacks/U0004__remove_security_admin_manage_permission.sql
  to DELETE the SECURITY_ADMIN_MANAGE row, guarded by a check that no
  Admin_Group_Permissions row references it (safe rollback only).

- Add or update database safety tests in PTKD.IntegrationTests verifying
  SECURITY_ADMIN_MANAGE exists in dbo.Permissions after V0004 is applied.

- Verify DatabaseSafety tests remain green after V0004 is applied against the
  approved test database.

### Explicitly out of scope for F-B0

- No PermissionCodes.cs change.
- No permission-catalog.md change (unless a discovered inconsistency requires
  a documentation correction; none currently identified).
- No F-B bootstrap implementation.
- No PTKD.Bootstrap project.
- No audit read endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No frontend.
- No business module implementation.
- No additional permissions beyond SECURITY_ADMIN_MANAGE.
- No schema table changes.
- No line-ending normalization.
- No production deployment.
- No tag or push.

---

## Secondary F-B spec corrections required

These corrections are recorded here and in the F-B plan. F-B implementation
must use the actual V0003 schema, not the plan prose where it diverges.

### Correction A — User_Auth_Accounts: no normalized_provider_subject column

The F-B plan (OD-F-B-05, step 5) describes a `normalized_provider_subject`
column in `User_Auth_Accounts`. This column does not exist in V0003.

Actual V0003 columns (relevant subset):
- provider_type varchar(30) NOT NULL
- provider_subject varchar(200) NOT NULL
- Unique constraint: UQ_UserAuthAccounts_ProviderSubject (provider_type, provider_subject)

F-B implementation must write to `provider_subject` only and must not reference
`normalized_provider_subject`.

### Correction B — Password_History: FK column is account_id

The F-B plan prose, in the context of Password_History, implies a column name
`user_auth_account_id`. The actual V0003 schema names this column `account_id`:

    account_id bigint NOT NULL
    CONSTRAINT FK_PasswordHistory_Account
        FOREIGN KEY (account_id) REFERENCES dbo.User_Auth_Accounts(id)

F-B implementation must use `account_id` when inserting into Password_History.

### Correction C — Admin_Groups.name is NOT NULL

Admin_Groups.name is defined as `nvarchar(200) NOT NULL` in V0003 (line 374).
OD-F-B-06 did not explicitly enumerate all required NOT NULL columns when
specifying the ADMIN_SECURITY group creation. F-B implementation must populate
at minimum:

- group_code  = 'ADMIN_SECURITY'
- name        = (a suitable non-null display name, e.g., N'Security Administration')
- scope_type  = 'GLOBAL'
- is_active   = 1

The exact value for `name` must be decided during the authorized F-B
implementation task and should match any future display requirements.

---

## Required next step

Create and accept a Phase 1B.1-F-B0 corrective migration plan.

No F-B implementation may resume until:
1. The F-B0 plan is created and accepted by Project Owner.
2. V0004 migration is implemented and passes all database safety tests.
3. The F-B0 implementation acceptance is committed.

---

## Explicit exclusions

This document is documentation only. The following are explicitly not
authorized:

- No F-B implementation.
- No migration in this documentation task.
- No seed/bootstrap implementation.
- No PTKD.Bootstrap project.
- No public bootstrap endpoint.
- No audit read endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No frontend.
- No business modules.
- No line-ending normalization.
- No production deployment.
- No tag or push.
