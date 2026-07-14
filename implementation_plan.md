# Phase 1A.1 - Organization Database Foundation

This plan covers implementing the Phase 1A.1 organization schema based on the approved analysis document, and upgrading the `DbMigrator` tool to properly support atomic migrations.

## User Review Required

> [!WARNING]
> Please review this final execution plan. Note the crucial change that `PTKD.DbMigrator` itself will be modified to handle database transactions atomically, rather than putting nested transactions inside the SQL scripts.

## Proposed Changes

### 1. DbMigrator Transaction Upgrades (C#)
`PTKD.DbMigrator/Program.cs` will be modified so that:
- It opens one `SqlTransaction` for each migration file.
- It executes every `GO`-split batch using the same connection and `SqlTransaction`.
- It inserts the `SchemaVersions` record using the *same* `SqlTransaction`.
- It commits the transaction *only after* both the full script and the history insert succeed.
- It rolls back all database changes (tables and history) if any batch or history insert fails.
- It will **never** write or open a write transaction during a `dry-run`.

### 2. Database Scripts

#### [NEW] [V0002__create_organization_schema.sql](file:///c:/Projects/PTKD-ERP/database/migrations/V0002__create_organization_schema.sql)
Creates the organization schema exactly as analyzed:
- **Transaction Safety**: V0002 will rely entirely on the newly upgraded `DbMigrator` `SqlTransaction`. It will **not** contain internal `BEGIN TRAN` or `TRY/CATCH` blocks, preventing issues with `GO` separators.
- `Companies`, `Departments`, `Users`, `User_Company_Assignments`, `User_Department_Assignments`, `Employment_Histories`.
- Temporal constraints `effective_to IS NULL OR effective_to > effective_from`.
- Filtered indexes ensuring AT MOST one active assignment or primary flag.
- Cross-user assignment mismatch prevention via composite constraints and FKs (`User_Company_Assignments: UNIQUE(id, user_id, company_id)`).
- Circular dependency for `created_by_user_id`/`updated_by_user_id` solved by `ALTER TABLE ADD CONSTRAINT` at the end of the script.

#### [NEW] [U0002__drop_organization_schema.sql](file:///c:/Projects/PTKD-ERP/database/rollbacks/U0002__drop_organization_schema.sql)
Rolls back V0002 safely:
- **Pre-check**: Identifies `V0002` using actual `Version` and `ScriptName` columns. Refuses rollback if `V0002` is not recorded. Refuses rollback if any later migration exists (using safe numeric version parsing, `CAST(SUBSTRING(Version, 2, LEN(Version)) AS INT)`).
- Drops all `created_by_user_id` and `updated_by_user_id` foreign keys first.
- Drops tables in reverse order: `Employment_Histories`, `User_Department_Assignments`, `User_Company_Assignments`, `Departments`, `Companies`, `Users`.
- **History Update**: Deletes *only* the exact `V0002` migration record from `SchemaVersions`, preserving `V0001`.
- History changes occur in the same script transaction after objects are dropped successfully.

### 3. Integration Tests

#### Test Execution & Isolation
- Tests will strictly target `PTKD_TEST_PHASE1A`.
- Temporary environment variables (connection strings) will be set per test and cleaned up in a `finally` block to prevent leakage.
- Execution will be separate, sequential, and non-parallel:
  1. `dotnet test ... --filter "FullyQualifiedName~OrganizationSchemaTests"`
  2. `dotnet test ... --filter "FullyQualifiedName~MigrationRollbackTests"`

#### [NEW] [OrganizationSchemaTests.cs](file:///c:/Projects/PTKD-ERP/tests/backend/PTKD.IntegrationTests/OrganizationSchemaTests.cs)
Verifies database constraints using test-managed SQL transactions:
1. Rejection of duplicate `company_code`, `department_code`, `employee_code`.
2. `row_version` changing after an update.
3. No `ON DELETE CASCADE` foreign keys.
4. No seed data (exactly zero rows in all six Phase 1A tables; `SchemaVersions` excluded).
5. All six expected tables exist.
6. All expected filtered indexes exist.
7. Composite FK prevents cross-user/company mismatch.
8. Rejection of cross-company parent department.
9. Rejection of two active primary departments for one user/company.
10. Rejection of `ACTIVE` with non-null `effective_to` / `CLOSED` with null `effective_to`.
11. Rejection of `effective_to <= effective_from`.
12. Rejection of direct self-parent references.
13. Rejection of a department assignment referencing a department from another company.
14. Rejection of two active company assignments for the same user and company.
15. Rejection of two active primary companies for the same user.
16. Rejection of two active assignments for the same user and department.

#### [NEW] [MigrationRollbackTests.cs](file:///c:/Projects/PTKD-ERP/tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs)
A dedicated test class for schema rollback and migrator atomicity:
1. **PTKD_TEST_PHASE1A Safety Preflight**: Asserts the DB name is exactly `PTKD_TEST_PHASE1A`. Refuses to run if unexpected user tables exist, or migrations newer than `V0002` are present.
2. Verifies exactly one `V0002` `SchemaVersions` record after apply.
3. Verifies `V0002` is skipped on a second apply.
4. Transaction rollback when a later migration batch fails.
5. No `SchemaVersions` record when migration execution fails.
6. `U0002` rejection when `V0002` is not recorded.
7. `U0002` rejection when a later numeric migration exists.
8. Applies `U0002` manually and verifies it removes only `V0002` from `SchemaVersions`, preserving `V0001`.
9. Verifies `V0002` successfully reapplies after `U0002`.

### 4. Documentation
#### [NEW] [phase-1a-database-implementation.md](file:///c:/Projects/PTKD-ERP/docs/architecture/phase-1a-database-implementation.md)
Will be created at the end of execution to serve as the completion report, detailing objects created, verification results, and deviations.

## Execution Matrix

### Automated Checks
```powershell
dotnet restore
dotnet build --warnaserror
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --filter "FullyQualifiedName~OrganizationSchemaTests"
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --filter "FullyQualifiedName~MigrationRollbackTests"
```

### Database Migration Verifications
1. `DbMigrator dry-run` to inspect commands.
2. `DbMigrator apply` on `PTKD_DEV`.
3. Verify `V0001` and `V0002` are recorded in `SchemaVersions` on `PTKD_DEV`.
4. Run `DbMigrator apply` again on `PTKD_DEV` and verify `V0002` is skipped.

### PASS/FAIL/BLOCKED Conditions
- **PASS**: All constraint tests pass, rollback/reapply succeeds on `PTKD_TEST_PHASE1A`, migrations apply cleanly to `PTKD_DEV` idempotently. Expected state: 6 tables empty, `SchemaVersions` has `V0001` and `V0002`.
- **FAIL**: Any build error, test failure, missing index/constraint, transaction leakage, or failure of U0002 to preserve history. Expected state: rollback cleanly executed, or explicit failure state documented.
- **BLOCKED**: `PTKD_TEST_PHASE1A` does not exist, contains unexpected tables, or connection fails. Execution halts immediately without dropping or altering external schemas.
