# Phase 1B.10-D Production Migration Tracking Correction Report

## Status

PASSED — READY FOR PROJECT OWNER PRODUCTION MIGRATION ACCEPTANCE

## Correction Source

Reference:

- Phase 1B.10-D production migration execution report commit:
  aa0ce91e240b69bc1a750333b40ee4458bda1b99

## Correction Boundary

- PTKD_PROD only.
- SchemaVersions tracking correction only.
- No migrations re-run.
- No rollbacks run.
- No schema changes.
- No permission/process catalog/business data changes.
- No source/test/frontend/backend/migration/rollback/business-doc/permission-catalog changes.
- No release tag.
- No push.
- Production readiness not claimed.

## Pre-Correction Finding

- SchemaVersions row count before correction: **0**.
- Reason: Phase 1B.10-D production migration used `sqlcmd` to apply migration SQL scripts directly. The DbMigrator application (which normally inserts SchemaVersions rows after each migration) was not used.
- Risk of leaving it empty: if the DbMigrator is later pointed at PTKD_PROD, it would detect no version records and attempt to re-apply all migrations. V0001/V0002 DDL scripts would fail on duplicate object creation. Seed migrations with IF NOT EXISTS guards (V0003+) would be safe but redundant.

## SchemaVersions Format Evidence

Format confirmed from DbMigrator source at `src/backend/PTKD.DbMigrator/Program.cs:123`:

```sql
INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES (@Ver, @Name, 'APPLIED')
```

- `Version`: filename prefix before `__` (e.g., `V0001`).
- `ScriptName`: full filename (e.g., `V0001__create_schema_versions.sql`).
- `Status`: literal `'APPLIED'`.
- `AppliedAt`: column default `SYSUTCDATETIME()` (auto-populated).
- `Id`: IDENTITY(1,1) auto-increment.

Source: `Program.cs` line 91 (`fileName.Split("__")[0]` for version) and line 123 (insert statement).

## Backup / Recovery Evidence

- Backup created before correction: `C:\temp\PTKD_PROD_pre_tracking_correction.bak`.
- Backup method: `BACKUP DATABASE [PTKD_PROD] TO DISK`.
- Backup result: 594 pages processed successfully.
- No secrets in backup path.

## Pre-Correction Verification

- Table count: 52 — unchanged from migration execution.
- Active permissions: 56 — unchanged.
- Duplicate permission_code: 0 — unchanged.
- SELL_CARE_PACKAGE: present, is_active = 1 — unchanged.

## Correction Execution Evidence

- Transaction used: `BEGIN TRANSACTION` / `COMMIT TRANSACTION`.
- Rows inserted: 15 (V0001 through V0015).
- Only dbo.SchemaVersions was modified.
- No other table was changed.
- No other database was touched.

SQL pattern:
```sql
BEGIN TRANSACTION;
INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status)
VALUES ('V0001', 'V0001__create_schema_versions.sql', 'APPLIED');
-- ... V0002 through V0014 ...
INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status)
VALUES ('V0015', 'V0015__deployment_readiness_permission_seed_alignment.sql', 'APPLIED');
COMMIT TRANSACTION;
```

## Post-Correction Verification

- SchemaVersions row count: **15** — correct.
- Versions present: V0001, V0002, V0003, V0004, V0005, V0006, V0007, V0008, V0009, V0010, V0011, V0012, V0013, V0014, V0015.
- All rows have Status = `APPLIED`.
- All ScriptNames match repository migration filenames exactly.
- Duplicate SchemaVersions entries: **0**.
- Table count: **52** — unchanged.
- Active permissions: **56** — unchanged.
- Duplicate permission_code: **0** — unchanged.
- SELL_CARE_PACKAGE: present, is_active = 1 — unchanged.

## Automated Sanity Validation

- Build: 0 errors, 9 pre-existing warnings.
- UnitTests: 236/236 passed.

IntegrationTests and ApiTests were not re-run against PTKD_PROD. They target PTKD_TEST_PHASE1A2 per TestDatabaseFixture configuration and were already validated in the prior execution task.

## Notes

- Original production migration used sqlcmd, which does not insert SchemaVersions rows.
- This correction aligns PTKD_PROD tracking with the already-applied migrations V0001 through V0015.
- The DbMigrator will now correctly detect all 15 migrations as already applied and skip them.
- Release tag/push remain future gates.
- Production readiness not claimed.

## Blockers

No blockers found.

## Recommended Next Gate

Project Owner Phase 1B.10-D production migration acceptance.
