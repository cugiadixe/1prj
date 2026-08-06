# Phase 1A.1 - Organization Database Foundation

## Summary of Implemented Behavior
- Created the six foundational organization tables: `Companies`, `Departments`, `Users`, `User_Company_Assignments`, `User_Department_Assignments`, and `Employment_Histories`.
- Added structural constraints: primary keys, `row_version` for optimistic concurrency, foreign keys (including composite foreign keys to ensure cross-user/cross-company isolation), and standard temporal metadata columns (`created_at`, `updated_at`, `created_by_user_id`, `updated_by_user_id`).
- Implemented robust status validations through check constraints ensuring correctness (e.g., `effective_to` must be strictly after `effective_from`, no self-parenting references in hierarchy).
- Implemented filtered unique indexes to ensure a user only has at most one active primary assignment and at most one active assignment per company/department.
- Upgraded `DbMigrator` to fully encapsulate batch execution and migration history insertion inside a single `SqlTransaction`. This ensures atomicity (either everything succeeds, or everything rolls back) and suppresses state corruption on partial failures.
- Added strict schema teardown in `U0002`, including history cleanup and execution preconditions.
- Confirmed that all `.NET 8` temporary workarounds were successfully reverted, placing the environment firmly on `.NET 10.0`.

## Framework and File Details
- **Target Framework**: All 9 projects strictly target `<TargetFramework>net10.0</TargetFramework>` and package references have been restored to their proper versions.
- **Exact Changed Files** (excluding temporary workarounds):
  - `database/migrations/V0002__create_organization_schema.sql` (NEW)
  - `database/rollbacks/U0002__drop_organization_schema.sql` (NEW)
  - `src/backend/PTKD.DbMigrator/Program.cs` (MODIFIED)
  - `tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj` (MODIFIED - appended SqlClient package reference)
  - `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs` (NEW)
  - `tests/backend/PTKD.IntegrationTests/OrganizationSchemaTests.cs` (NEW)
  - `tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs` (NEW)
  - `docs/architecture/phase-1a-database-implementation.md` (NEW)

## Exact Build and Test Commands Run
```bash
$env:PATH = "C:\Users\adm-bachdh\AppData\Local\Microsoft\dotnet\;" + $env:PATH
dotnet restore src/backend/PTKD-ERP.sln
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --warnaserror
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --filter "FullyQualifiedName~OrganizationSchemaTests"
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --filter "FullyQualifiedName~MigrationRollbackTests"
```

## Build and Test Results
- **Build**: Success (`0 Warning(s), 0 Error(s)`) using .NET SDK `10.0.301`.
- **OrganizationSchemaTests**: `Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17, Duration: 957 ms`
- **MigrationRollbackTests**: `Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 9 s`

## Verification and Execution Outputs

### PTKD_DEV Migration Results (Idempotency)
Because `PTKD_DEV` successfully processed the manual DbMigrator execution initially, dry-run and apply strictly skipped re-applying the schema.
**Command**: `dotnet run --project src/backend/PTKD.DbMigrator/PTKD.DbMigrator.csproj -- --dry-run`
**Output**:
```
PTKD DbMigrator started.
--- DRY RUN MODE ---
Using migrations directory: C:\Projects\PTKD-ERP\database\migrations
Connected to database successfully.
Skipping V0001__create_schema_versions.sql (already applied)
Skipping V0002__create_organization_schema.sql (already applied)
PTKD DbMigrator finished.
```
*Applying without `--dry-run` produced the same output.*
**U0002 was strictly forbidden to run on PTKD_DEV.** 

### Schema Status and Row Counts (PTKD_DEV)
- **SchemaVersions**: Contains `V0001` and `V0002` exactly once each.
- **Tables**: `Companies`, `Departments`, `Users`, `User_Company_Assignments`, `User_Department_Assignments`, `Employment_Histories` successfully exist. All tables contain exactly **0 business rows**.
- **ON DELETE CASCADE**: Explicitly confirmed to be 0 across all constraints in integration testing.

### Verified Constraints & Index Names
- **Primary Keys**: `PK_Companies`, `PK_Departments`, `PK_Users`, `PK_UserCompanyAssignments`, `PK_UserDepartmentAssignments`, `PK_EmploymentHistories`
- **Foreign Keys**: `FK_Departments_company_id`, `FK_UserCompanyAssignments_user_id`, `FK_UserCompanyAssignments_company_id`, `FK_UserDepartmentAssignments_company_assignment` (Composite FK), `FK_UserDepartmentAssignments_department_id`
- **Unique Constraints**: `UQ_Companies_company_code`, `UQ_Departments_department_code`, `UQ_Users_employee_code`
- **Filtered Unique Indexes**: `UQ_User_Primary_Company`, `UQ_User_Company_Active`, `UQ_User_Company_Primary_Dept`, `UQ_User_Dept_Active`
- **Check Constraints**: `CK_UserCompanyAssignments_EffectiveDates`, `CK_UserCompanyAssignments_StatusConsistency`

### PTKD_TEST_PHASE1A Rollback and Reapply Results
Executed precisely through the `MigrationRollbackTests` suite:
- `U0002` safely executed as a single transaction block. 
- All Phase 1A tables removed correctly. 
- Exactly the `V0002` SchemaVersions record was cleanly deleted, while `V0001` preserved.
- When `DbMigrator` re-invoked immediately afterwards, `V0002` was successfully reapplied to recreate the schema cleanly. 
- No unexpected tables were left orphaned. 

### DbMigrator Atomicity Verification
Executed in `MigrationRollbackTests.DbMigratorRollsBackWhenScriptFails`:
- Successfully injected a temporary `V9999` migration containing intentional SQL errors (`SELECT * FROM NonExistentTable`) in the second batch.
- Expected behavior witnessed: The earlier `CREATE TABLE` command in `V9999` was cleanly rolled back.
- SchemaVersions did not register `V9999`, confirming complete transactional failure isolation.

## Deviations from the Approved Plan
- `DbMigrator/Program.cs` explicitly updated to prefer `ConnectionStrings__DefaultConnection` environment variable *before* configuration builder defaults (User Secrets) to allow safe execution in the test fixture against `PTKD_TEST_PHASE1A` instead of incorrectly running against `PTKD_DEV`.
- Changed `Main` in `DbMigrator` to return `int` instead of `void`, explicitly returning `1` on failure so CI/CD and the test harnesses observe accurate failure exit codes.

## Conclusion Status
**READY FOR PHASE 1A.2**
