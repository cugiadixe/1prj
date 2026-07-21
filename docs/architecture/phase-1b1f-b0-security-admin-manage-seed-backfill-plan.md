# Phase 1B.1-F-B0 — SECURITY_ADMIN_MANAGE Seed Backfill Plan

## Status

ACCEPTED PLAN — PHASE 1B.1-F-B0 IMPLEMENTATION MAY BE AUTHORIZED AFTER THIS ACCEPTANCE COMMIT

### Accepted conditions

- SECURITY_ADMIN_MANAGE exists in PermissionCodes.cs and permission-catalog.md.
- SECURITY_ADMIN_MANAGE is absent from V0003 database seed data.
- Project Owner rejected amending V0003.
- Project Owner rejected substituting another permission for OD-F-B-06.
- No F-B0 implementation is authorized by this acceptance commit.
- F-B0 implementation still requires a separate authorization prompt.
- F-B Bootstrap remains blocked.

---

## Baseline

- Current accepted HEAD: `e783fd349c2a4a43938715ea8a2c18cb3b3a8389`
- F-B hard stop recorded in commit `e783fd349c2a4a43938715ea8a2c18cb3b3a8389`
  (docs/architecture/phase-1b1f-b-hard-stop-security-admin-manage-seed-gap.md).
- F-B implementation remains blocked until F-B0 plan is accepted,
  F-B0 migration is implemented, tests pass, and F-B0 acceptance is committed.
- F-B0 is a corrective migration slice only.
- No migration has been created yet in this task.

---

## Purpose

Phase F-B0 adds the missing `SECURITY_ADMIN_MANAGE` permission row to the
database migration state, restoring consistency between:

- `PermissionCodes.cs` — which defines `SECURITY_ADMIN_MANAGE` as a constant,
- `permission-catalog.md` — which documents `SECURITY_ADMIN_MANAGE` as a
  canonical permission, and
- the database migration history — which currently does not seed it.

Without this backfill, the accepted F-B bootstrap decision OD-F-B-06 cannot
be implemented: OD-F-B-06 requires stopping if `SECURITY_ADMIN_MANAGE` is
absent from the database rather than silently substituting another permission.

F-B0 adds exactly one permission row. It does not implement bootstrap, does
not create admin groups, does not create users, and does not modify any
application source code.

---

## Discovery findings

### Finding 1 — Permissions table name and schema

Table: `dbo.Permissions`

Primary key: `permission_code varchar(100) NOT NULL`
(natural key — no surrogate identity column)

All columns (from V0003 lines 108–131):

| Column           | Type              | Nullable | Constraint / Default |
|------------------|-------------------|----------|----------------------|
| permission_code  | varchar(100)      | NOT NULL | PK, immutable (trigger) |
| module_code      | varchar(50)       | NOT NULL | — |
| action_code      | varchar(50)       | NOT NULL | — |
| data_scope       | varchar(30)       | NOT NULL | CHECK IN ('GLOBAL','COMPANY') |
| is_sensitive     | bit               | NOT NULL | — |
| requires_reason  | bit               | NOT NULL | — |
| is_delegable     | bit               | NOT NULL | — |
| is_active        | bit               | NOT NULL | — |
| description      | nvarchar(500)     | NULL     | — |
| row_version      | rowversion        | NOT NULL | DB-generated |
| created_at       | datetime2(3)      | NOT NULL | DEFAULT SYSUTCDATETIME() |

Triggers on `dbo.Permissions`:
- `TR_Permissions_PreventDelete` — INSTEAD OF DELETE; blocks all deletion.
- `TR_Permissions_PreventCodeChange` — AFTER UPDATE; blocks updates to
  `permission_code`. Updates to other columns are permitted.

FKs referencing `dbo.Permissions(permission_code)` (child tables):
- `dbo.Role_Permissions.permission_code`
  (`FK_RolePermissions_Permission`)
- `dbo.Department_Permissions.permission_code`
  (`FK_DepartmentPermissions_Permission`)
- `dbo.User_Individual_Permissions.permission_code`
  (`FK_UserIndividualPermissions_Permission`)
- `dbo.Admin_Group_Permissions.permission_code`
  (`FK_AdminGroupPermissions_Permission`)

**Impact on V0004 rollback:** After F-B runs, `dbo.Admin_Group_Permissions`
will hold a row referencing `SECURITY_ADMIN_MANAGE`. The U0004 rollback
DELETE will be blocked by `FK_AdminGroupPermissions_Permission`. U0004 must
check for this condition and fail safely. See OD-F-B0-02 and Risks section.

### Finding 2 — Columns required to insert a permission row

A valid INSERT must supply:
- `permission_code` — varchar(100), primary key
- `module_code` — varchar(50)
- `action_code` — varchar(50)
- `data_scope` — varchar(30); must be 'GLOBAL' or 'COMPANY'
- `is_sensitive` — bit
- `requires_reason` — bit
- `is_delegable` — bit
- `is_active` — bit
- `description` — nvarchar(500), nullable but provided in V0003 style

`row_version` and `created_at` are database-generated and must not be
specified in the INSERT column list.

### Finding 3 — SECURITY_ADMIN_MANAGE values from permission-catalog.md

From `docs/business/permission-catalog.md`, line 58:

| Field            | Value |
|------------------|-------|
| permission_code  | `SECURITY_ADMIN_MANAGE` |
| module_code      | `SECURITY` |
| action_code      | `ADMIN_MANAGE` |
| data_scope       | `GLOBAL` |
| sensitive        | Yes → `is_sensitive = 1` |
| delegable        | No → `is_delegable = 0` |
| requires_reason  | Not listed explicitly; consistent with other GLOBAL SECURITY permissions |
| purpose          | Manage security administration configuration (Roles, AdminGroups, Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions). |

**requires_reason mapping:** The permission-catalog.md column is labelled
"delegable" not "requires_reason". Comparing to adjacent V0003 rows of similar
scope:

- `SECURITY_USER_MANAGE`: `is_sensitive=1, requires_reason=1, is_delegable=0`
- `SECURITY_ADMIN_GROUP_MANAGE`: `is_sensitive=1, requires_reason=1, is_delegable=0`

`SECURITY_ADMIN_MANAGE` is classified as `sensitive=Yes, delegable=No`.
`requires_reason=1` is the consistent value for all GLOBAL SECURITY management
permissions in V0003.

**Proposed values for V0004:**

```
permission_code = 'SECURITY_ADMIN_MANAGE'
module_code     = 'SECURITY'
action_code     = 'ADMIN_MANAGE'
data_scope      = 'GLOBAL'
is_sensitive    = 1
requires_reason = 1
is_delegable    = 0
is_active       = 1
description     = N'Manage security administration configuration (Roles,
                    AdminGroups, Permissions, UserAssignments,
                    DepartmentPermissions, EffectivePermissions).'
```

This matches the permission-catalog.md purpose field verbatim.

**Project Owner must confirm `requires_reason = 1` in OD-F-B0-03.**

### Finding 4 — SECURITY_ADMIN_MANAGE is absent from V0003

Confirmed. V0003 (lines 134–151) seeds exactly 15 permission codes via a
single `INSERT INTO dbo.Permissions` block. `SECURITY_ADMIN_MANAGE` does not
appear. The existing rollback U0003 (line 55) hard-codes the expected count
as 15 and hard-codes the exact 15-row set (lines 62–80). Neither SECURITY_ADMIN_MANAGE
appears in U0003 validation.

### Finding 5 — SECURITY_ADMIN_MANAGE present in PermissionCodes.cs

File: `src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs`, line 12:

```csharp
public const string SecurityAdminManage = "SECURITY_ADMIN_MANAGE";
```

No change needed. No PermissionCodes.cs modification is authorized in F-B0.

### Finding 6 — SECURITY_ADMIN_MANAGE present in permission-catalog.md

File: `docs/business/permission-catalog.md`, line 58. Confirmed present.
No change needed. No permission-catalog.md modification is authorized in F-B0.

### Finding 7 — Migration naming convention and next migration number

**Observed pattern:**
- Forward migration files: `database/migrations/V{NNNN}__{description}.sql`
- Rollback files: `database/rollbacks/U{NNNN}__{description}.sql`
- Existing forward migrations: V0001, V0002, V0003
- Existing rollbacks: U0001, U0002, U0003
- Next migration number: **V0004** / **U0004**

**DbMigrator ordering (from PTKD.DbMigrator/Program.cs, line 67):**
```csharp
var files = Directory.GetFiles(migrationsPath, "V*.sql").OrderBy(f => f).ToList();
```
Files are ordered lexicographically by filename. `V0004` sorts after `V0003`,
ensuring correct application order.

**DbMigrator skip logic (line 95–100):** The migrator checks `SchemaVersions`
by `ScriptName`. If the ScriptName of the migration file is already recorded,
the migration is skipped. V0004 will be skipped on re-run once applied.

### Finding 8 — Rollback naming convention

U0003 is the model for F-B0. Key characteristics:

1. Database name guard: `IF DB_NAME() <> N'PTKD_TEST_PHASE1A2' THROW ...`
2. SchemaVersions existence guard.
3. Version recorded guard.
4. No-later-version guard: rejects rollback if a migration with version
   number > the current one is recorded. U0004 must check `> 4`.
5. Data presence guards: refuses rollback if downstream tables contain
   material data.
6. Content validation: verifies exact permission count and set before
   dropping.
7. Wrapped in `SET XACT_ABORT ON; BEGIN TRY; BEGIN TRANSACTION; ... COMMIT;`
   with `BEGIN CATCH; ROLLBACK; THROW; END CATCH`.

U0004 follows the same pattern but for the V0004 migration state.

### Finding 9 — Existing tests relevant to F-B0

**SecuritySchemaTests.cs:**

- `Permissions_UseNaturalPrimaryKey_AndExactImmutableSeedCatalog` (line 234):
  - Queries `SELECT permission_code FROM dbo.Permissions ORDER BY permission_code`.
  - Asserts `Assert.Equal(ExpectedPermissionCodes, actualCodes)` against a
    hard-coded 15-element array.
  - Asserts `Assert.DoesNotContain("ADMIN", actualCodes)` — **this assertion
    will break when V0004 adds `SECURITY_ADMIN_MANAGE`**, since that code
    contains "ADMIN".
  - This test **must** be updated as part of F-B0 implementation to:
    - add `SECURITY_ADMIN_MANAGE` to `ExpectedPermissionCodes` (sorted
      position: after `SECURITY_ADMIN_GROUP_VIEW`, before `SECURITY_ASSIGNMENT_MANAGE`),
    - remove or scope the `DoesNotContain("ADMIN")` assertion.
  - **This is the primary test change required by F-B0.**

- `Migrator_AppliesV0001V0002V0003ExactlyOnce_AndRecordsV0003` (line 54):
  - Verifies Applied/Skipping messages for V0001–V0003.
  - Will need a counterpart test or update for V0004 in F-B0 implementation.

**MigrationRollbackTests.cs:**

- `DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder` (line 17):
  - Hard-codes V0001/V0002/V0003 messages.
  - After F-B0, a corresponding V0004 and U0004 rollback step will need to
    be verified. A new test or test extension is required in the F-B0
    implementation task.

**DatabaseSafetyTests.cs:**

- `FixtureBaseline_ContainsV0001AndV0002ExactlyOnce` (line 108):
  - Only checks V0001 and V0002. No change needed for F-B0.

**U0003 Permissions content validation (lines 62–81):**
- U0003 validates the exact 15-row set before rollback.
- After V0004, U0004 is rolled back first, restoring the 15-row state.
  U0003 can then run against the clean 15-row state. **No U0003 modification
  is required.**

### Finding 10 — FK/reference constraints affecting rollback

`dbo.Permissions(permission_code)` is referenced by four child tables:
- `dbo.Role_Permissions`
- `dbo.Department_Permissions`
- `dbo.User_Individual_Permissions`
- `dbo.Admin_Group_Permissions`

U0004 must DELETE `SECURITY_ADMIN_MANAGE` from `dbo.Permissions`. This DELETE
will be **blocked by SQL Server foreign key enforcement** if any of the four
child tables contain a row referencing `SECURITY_ADMIN_MANAGE`.

In a clean test database immediately after V0004 (before F-B bootstrap),
no child rows reference `SECURITY_ADMIN_MANAGE`. The rollback is safe.

After F-B bootstrap runs, `dbo.Admin_Group_Permissions` will hold a row
`(ADMIN_SECURITY group id, 'SECURITY_ADMIN_MANAGE')`. Attempting to run
U0004 after a successful F-B bootstrap will be blocked by FK enforcement.

**U0004 rollback design:** U0004 must include an explicit data-presence guard
(following U0003 pattern) that checks for referencing rows in all four child
tables before attempting the DELETE, and throws a descriptive error if any
exist. This is a safe-fail rollback consistent with the U0003 convention.

Additionally, U0004 must guard that U0003 cannot run while V0004 is recorded
(the no-later-version check in U0003 already handles this: if V0004 is in
SchemaVersions, U0003 will be blocked by its version guard on line 27).

**Implication:** U0004 is safe to run on a clean database (before F-B
bootstrap). After F-B bootstrap, U0004 rollback is blocked by FK enforcement;
this is correct behaviour and must be documented.

Note: `TR_Permissions_PreventDelete` fires INSTEAD OF DELETE and throws
an error unconditionally. **This trigger will also block U0004's DELETE.**
U0004 cannot use a plain DELETE. U0004 must use a SQL mechanism that
bypasses the immutability trigger, or the trigger's design must be
reconsidered. This is a **blocker** for U0004 as currently understood.

**Trigger blocker — further analysis:**

The `TR_Permissions_PreventDelete` trigger text (V0003 lines 154–161):

```sql
CREATE TRIGGER dbo.TR_Permissions_PreventDelete
ON dbo.Permissions
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51011, 'Released permission_code values may not be deleted;
                  deactivate the permission instead.', 1;
END;
```

This fires for ALL deletes, including rollback. There is no bypass mechanism
in the current trigger. The trigger message says "deactivate the permission
instead."

**Options for U0004 rollback strategy:**
- **Option R-A (Soft-delete rollback):** Instead of deleting the row, U0004
  sets `is_active = 0` on `SECURITY_ADMIN_MANAGE`. The trigger allows UPDATE
  to non-`permission_code` columns. This is safe but leaves the row in the
  table (affecting U0003's exact-count guard if run after U0004).
- **Option R-B (Document-only rollback):** Document U0004 as having no
  applicable rollback and provide recovery notes. U0003 must already be
  run before U0004 can be considered. This is technically safe for the test
  database since U0003 blocks on version number if V0004 is recorded.
- **Option R-C (Disable trigger in rollback):** U0004 disables the trigger,
  deletes the row, re-enables it. This is a structural bypass and inconsistent
  with the security posture of the trigger's existence.

**Proposed resolution:** Use Option R-A for the rollback: U0004 sets
`is_active = 0` and records the deactivation in SchemaVersions appropriately.
This is reversible (re-activation via a new migration) and avoids the trigger
bypass. The exact-count guards in U0003 and the V0004 version guard in U0003
prevent improper sequencing.

**Project Owner must confirm U0004 rollback strategy in OD-F-B0-02.**

### Finding 11 — F-B0 can be implemented without touching source code

Yes. `SECURITY_ADMIN_MANAGE` is already defined in `PermissionCodes.cs` and
documented in `permission-catalog.md`. V0004 is a SQL-only migration file.
U0004 is a SQL-only rollback file. No C# source code changes are needed for
F-B0. No PermissionCodes.cs change. No permission-catalog.md change.

The one test file change required (`SecuritySchemaTests.cs`,
`ExpectedPermissionCodes` array and `DoesNotContain` assertion) is a test
update, not a source code change.

### Finding 12 — Contradictions between docs and database migrations

| Item | Documentation | Database (V0003) | Assessment |
|---|---|---|---|
| SECURITY_ADMIN_MANAGE | Present in PermissionCodes.cs and permission-catalog.md | **Absent from V0003 seed** | **Contradiction — this task resolves it** |
| requires_reason for SECURITY_ADMIN_MANAGE | Not stated in permission-catalog.md | Not seeded | Must be inferred; proposed = 1 (consistent with peer permissions) |
| U0003 permission count guard | Hard-codes 15 | V0003 seeds 15 | Consistent after U0004 deactivation scenario; see risk section |
| SecuritySchemaTests.cs ExpectedPermissionCodes | 15 codes; no SECURITY_ADMIN_MANAGE | V0003 has 15 codes | Test will need updating in F-B0 implementation |

No other contradictions between docs and migrations were found.

---

## Proposed Project Owner decisions for F-B0

### OD-F-B0-01 — Corrective migration

Create a new V0004 migration at
`database/migrations/V0004__seed_security_admin_manage_permission.sql`
to INSERT the `SECURITY_ADMIN_MANAGE` row into `dbo.Permissions`.

- Do not amend V0003.
- Do not revise any existing migration.
- V0004 follows the naming and transaction conventions established by V0001–V0003.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-02 — Rollback strategy

Create a matching U0004 rollback at
`database/rollbacks/U0004__deactivate_security_admin_manage_permission.sql`.

Because `TR_Permissions_PreventDelete` blocks all DELETE on `dbo.Permissions`,
U0004 cannot delete the row. The proposed rollback strategy is:

**Option R-A (Recommended):** U0004 sets `is_active = 0` on
`SECURITY_ADMIN_MANAGE`. The UPDATE trigger (`TR_Permissions_PreventCodeChange`)
only blocks updates to `permission_code`; updates to `is_active` are permitted.
U0004 must also guard:
- Database is the approved test database.
- V0004 is recorded in SchemaVersions.
- No later migration is recorded.
- No referencing rows exist in `Admin_Group_Permissions`, `Role_Permissions`,
  `Department_Permissions`, or `User_Individual_Permissions`.
- Remove V0004 from SchemaVersions on success.

The migration `FixtureBaseline_ContainsV0001AndV0002ExactlyOnce` test is
unaffected. U0003's exact-count guard expects 15 rows; after U0004
deactivation the row still exists (count = 16), so U0003 will be blocked.
This is correct: U0003 cannot run while any migration later than V0003 is
applied (its version guard at line 27 blocks it if V0004 is in SchemaVersions;
after U0004 removes V0004 from SchemaVersions, U0003 count guard would see
16 rows and block). **This means U0003 is permanently incompatible with
Option R-A after V0004 is applied on a database that does not subsequently
run U0004.** This is acceptable for the test database workflow where U0004
runs first before U0003.

**Option R-B:** Document U0004 as producing no revertible state on a database
where references exist. Provide recovery notes only. The SchemaVersions
removal and test database reset via `ResetToV0003()` in `TestDatabaseFixture`
cover the integration test use case.

**Project Owner must select Option R-A or R-B in OD-F-B0-02.**

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-03 — Permission values

Use the following column values for the V0004 INSERT, derived from
`permission-catalog.md` and V0003 peer permissions:

| Column          | Value |
|-----------------|-------|
| permission_code | `'SECURITY_ADMIN_MANAGE'` |
| module_code     | `'SECURITY'` |
| action_code     | `'ADMIN_MANAGE'` |
| data_scope      | `'GLOBAL'` |
| is_sensitive    | `1` |
| requires_reason | `1` (proposed; see Finding 3) |
| is_delegable    | `0` |
| is_active       | `1` |
| description     | `N'Manage security administration configuration (Roles, AdminGroups, Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions).'` |

**Project Owner must confirm `requires_reason = 1` or specify an alternative.**

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-04 — Idempotency

The DbMigrator skips a migration if its `ScriptName` is already recorded
in `dbo.SchemaVersions`. V0004 is therefore safe to run through the migration
runner without an `IF NOT EXISTS` guard — the migrator provides the
idempotency guarantee.

In alignment with V0003 style, V0004 does not include an `IF NOT EXISTS` guard.
The migration runner's skip logic is the idempotency mechanism.

**Proposed:** Follow V0003 style — no `IF NOT EXISTS` guard in V0004.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-05 — Tests

F-B0 implementation must update/add the following tests:

**Updated tests (PTKD.IntegrationTests):**

1. `SecuritySchemaTests.ExpectedPermissionCodes` array — add
   `'SECURITY_ADMIN_MANAGE'` in sorted position (between
   `SECURITY_ADMIN_GROUP_VIEW` and `SECURITY_ASSIGNMENT_MANAGE`).
2. `SecuritySchemaTests.Permissions_UseNaturalPrimaryKey_AndExactImmutableSeedCatalog` —
   remove or scope the `Assert.DoesNotContain("ADMIN", actualCodes)` assertion
   (it will fail once `SECURITY_ADMIN_MANAGE` is present).
3. `MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder` —
   extend to include V0004 apply/skip/rollback steps.

**New tests (PTKD.IntegrationTests):**

4. `SecuritySchemaTests.Permissions_V0004_ContainsSecurityAdminManage`:
   After V0004 is applied, verify `SECURITY_ADMIN_MANAGE` exists in
   `dbo.Permissions` with the exact column values specified in OD-F-B0-03.

**Regression:**
- All existing 157 integration tests, 114 unit tests, and 153 API tests
  must remain green after F-B0 implementation.
- DatabaseSafety tests (17) must remain green.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-06 — Scope boundary

F-B0 must not implement bootstrap. F-B0 must not create the ADMIN_SECURITY
admin group. F-B0 must not create users, auth accounts, or any application
business objects. F-B0 must not create audit endpoints. F-B0 must not
enforce SECURITY_AUDIT_VIEW. F-B0 must not modify PermissionCodes.cs or
permission-catalog.md unless implementation inspection proves a documentation
inconsistency that requires a correction.

No documentation inconsistency has been found that requires a change to
PermissionCodes.cs or permission-catalog.md. Both already correctly describe
SECURITY_ADMIN_MANAGE.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B0-07 — F-B unblock condition

F-B implementation remains blocked until all of the following are true:

1. F-B0 plan is accepted (Project Owner acceptance committed).
2. V0004 migration is implemented and committed.
3. U0004 rollback is implemented and committed.
4. All required test updates pass (including `SECURITY_ADMIN_MANAGE` in
   ExpectedPermissionCodes and new existence test).
5. Full test suite (unit, integration, API, DatabaseSafety) passes.
6. F-B0 implementation acceptance is reviewed and committed.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

## Proposed migration shape

### V0004 — Forward migration

**Expected filename:** `database/migrations/V0004__seed_security_admin_manage_permission.sql`

**Table:** `dbo.Permissions`

**Column mapping (INSERT column list — `row_version` and `created_at` omitted):**

```
(permission_code, module_code, action_code, data_scope,
 is_sensitive, requires_reason, is_delegable, is_active, description)
```

**Insert strategy:** Single-row INSERT following V0003 style:

```sql
-- Illustrative only. Do not create migration files in this task.
-- V0004__seed_security_admin_manage_permission.sql
-- Phase 1B.1-F-B0 corrective backfill: SECURITY_ADMIN_MANAGE permission.
-- Resolves gap between PermissionCodes.cs, permission-catalog.md, and V0003.

INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope,
     is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('SECURITY_ADMIN_MANAGE', 'SECURITY', 'ADMIN_MANAGE', 'GLOBAL',
     1, 1, 0, 1,
     N'Manage security administration configuration (Roles, AdminGroups,
       Permissions, UserAssignments, DepartmentPermissions,
       EffectivePermissions).');

INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status)
VALUES ('V0004', 'V0004__seed_security_admin_manage_permission.sql', 'APPLIED');
```

**No V0003 amendment. No existing migration file modification.**

**Note:** The DbMigrator inserts into SchemaVersions automatically. If V0004
runs through the DbMigrator, the SchemaVersions INSERT may be generated by
the migrator. Implementation must follow the exact DbMigrator mechanism used
for V0001–V0003.

### U0004 — Rollback

**Expected filename:** `database/rollbacks/U0004__deactivate_security_admin_manage_permission.sql`

**Rollback strategy (proposed Option R-A):**

```sql
-- Illustrative only. Do not create rollback files in this task.
-- U0004__deactivate_security_admin_manage_permission.sql

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- Database guard
    IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
        THROW 51200, 'U0004 may run only against PTKD_TEST_PHASE1A2.', 1;

    -- Version guards
    IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = N'V0004' ...)
        THROW 51201, 'V0004 is not recorded; cannot roll back.', 1;
    IF EXISTS (SELECT 1 FROM dbo.SchemaVersions
               WHERE TRY_CONVERT(int, SUBSTRING(Version, 2, 20)) > 4)
        THROW 51202, 'A migration later than V0004 is recorded; rollback prohibited.', 1;

    -- FK reference guards (safe-fail before UPDATE)
    IF EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions
               WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51203, 'Rollback blocked: Admin_Group_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Role_Permissions
               WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51204, 'Rollback blocked: Role_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Department_Permissions
               WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51205, 'Rollback blocked: Department_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions
               WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51206, 'Rollback blocked: User_Individual_Permissions references SECURITY_ADMIN_MANAGE.', 1;

    -- Deactivate (no DELETE due to TR_Permissions_PreventDelete trigger)
    UPDATE dbo.Permissions
    SET is_active = 0
    WHERE permission_code = 'SECURITY_ADMIN_MANAGE';

    IF @@ROWCOUNT <> 1
        THROW 51207, 'SECURITY_ADMIN_MANAGE row was not updated exactly once.', 1;

    -- Remove V0004 from SchemaVersions
    DELETE FROM dbo.SchemaVersions
    WHERE Version = N'V0004'
      AND ScriptName = N'V0004__seed_security_admin_manage_permission.sql';

    IF @@ROWCOUNT <> 1
        THROW 51208, 'V0004 SchemaVersions row was not removed exactly once.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
```

**Safety notes for existing references:**
- `TR_Permissions_PreventDelete` blocks DELETE; U0004 uses UPDATE (`is_active=0`).
- FK references block rollback after F-B bootstrap; U0004 guards for this explicitly.
- U0003 cannot run while V0004 is in SchemaVersions (U0003 version guard).
- After U0004 completes, U0003's count guard sees 16 rows (the deactivated
  SECURITY_ADMIN_MANAGE row still exists). U0003 will be blocked. Recovery
  from that state requires a new activation migration, not a U0003 run.

**No V0003 amendment. No modification to U0003.**

---

## Proposed test strategy

### New test — SECURITY_ADMIN_MANAGE exists after V0004

In `PTKD.IntegrationTests/SecuritySchemaTests.cs` (or a new
`SecurityPermissionCatalogTests.cs`):

```
Permissions_V0004_ContainsSecurityAdminManage:
  - Apply migrations to V0004 baseline.
  - Query: SELECT permission_code, module_code, action_code, data_scope,
           is_sensitive, requires_reason, is_delegable, is_active
    FROM dbo.Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE'.
  - Assert exactly one row.
  - Assert all column values match OD-F-B0-03.
```

### Updated test — ExpectedPermissionCodes

Add `SECURITY_ADMIN_MANAGE` to the `ExpectedPermissionCodes` array in sorted
order:

```
"SECURITY_ACCOUNT_MANAGE",
"SECURITY_ADMIN_GROUP_MANAGE",
"SECURITY_ADMIN_GROUP_VIEW",
"SECURITY_ADMIN_MANAGE",          ← add here
"SECURITY_ASSIGNMENT_MANAGE",
"SECURITY_AUDIT_VIEW",
...
```

Remove or scope the `Assert.DoesNotContain("ADMIN", actualCodes)` assertion.
This assertion was a proxy for "SECURITY_ADMIN_MANAGE should not exist" and
is no longer valid once V0004 adds it.

### Regression tests

- Full migration apply/skip cycle including V0004 via
  `MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder`
  or a new V0004-specific test.
- U0004 rollback tested against a clean post-V0004 database (no bootstrap run).
- Verify that U0003 is blocked while V0004 is in SchemaVersions (the existing
  version guard in U0003 provides this; can be tested implicitly).

### Build and test sequence after F-B0 implementation

```
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests         -- expect 114+ passed, 0 failed
dotnet test tests/backend/PTKD.IntegrationTests  -- expect 158+ passed, 0 failed (1 new)
dotnet test tests/backend/PTKD.ApiTests          -- expect 153 passed, 0 failed
```

No test may be weakened or deleted to make F-B0 pass. The
`DoesNotContain("ADMIN")` removal is a correctness fix, not a weakening.

---

## Risks and blockers

| Risk | Severity | Mitigation |
|---|---|---|
| `TR_Permissions_PreventDelete` blocks U0004 DELETE | **High** | Use `is_active=0` UPDATE instead of DELETE (Option R-A). Project Owner must confirm. |
| U0004 rollback blocked after F-B bootstrap | Medium | FK reference guards in U0004 fail safely with descriptive error. This is correct behaviour. |
| U0003 incompatible after V0004 + Option R-A | Medium | U0003 version guard already blocks it. After U0004, count guard sees 16 rows and blocks U0003. Test database recovery uses `ResetToEmpty()` + full re-apply, not U0003. |
| `DoesNotContain("ADMIN")` test assertion | Medium | Must be updated in F-B0 implementation. If not updated, test will fail immediately after V0004 is applied. |
| Migration order risk | Low | DbMigrator orders by filename lexicographically; `V0004` sorts after `V0003`. No risk. |
| Accidentally amending V0003 | High | Strict scope: create new V0004 only. Do not edit V0003 or U0003. |
| Accidentally changing PermissionCodes.cs | Low | No change needed; constant already exists. Scope control enforced by task rules. |
| Accidentally changing permission-catalog.md | Low | No change needed; row already exists. Scope control enforced by task rules. |
| requires_reason value ambiguity | Low | Proposed as 1 based on peer analysis. Project Owner must confirm in OD-F-B0-03. |
| F-B implementation resuming before F-B0 acceptance | High | OD-F-B0-07 makes F-B unblock condition explicit. F-B plan status is BLOCKED until F-B0 acceptance is recorded. |
| Error code conflicts in U0004 | Low | Error codes 51200–51208 are proposed as new; 51100–51125 are used by U0003. Verify no collision in U0001/U0002. |

---

## Explicit exclusions

- No migration created in this planning task.
- No rollback migration created in this planning task.
- No V0003 amendment.
- No U0003 amendment.
- No F-B bootstrap implementation.
- No PTKD.Bootstrap project.
- No ADMIN_SECURITY admin group creation.
- No user or auth account creation.
- No audit endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag or push.

---

## Accepted Project Owner decisions

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
