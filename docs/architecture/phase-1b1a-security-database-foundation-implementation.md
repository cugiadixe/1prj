# Phase 1B.1-A Security Database Foundation Correction and Verification

## Status and authorization boundary

- Original Phase 1B.1-A commit: `9d313a343fe2b2ccf29379b3a920bab9de4b5a0d`.
- The original commit was created without executable build or test verification because the required .NET SDK was unavailable at that time.
- The correction and verification described here were authorized for Phase 1B.1-A only.
- Phase 1B.1-A is verified and corrected but is not self-accepted; Project Owner acceptance remains required.
- Phase 1B.1-B through I remain **NOT AUTHORIZED**.

No authentication service, password service, JWT issuance/validation, authorization middleware, API endpoint, frontend feature, bootstrap executable, MediatR integration, or Dapper integration was implemented.

## Governing decisions and rules

This slice implements the database foundation for DEC-1B-001, DEC-1B-002, DEC-1B-005 through DEC-1B-010, and DEC-1B-013 through DEC-1B-017, with particular traceability to:

- `AUTH-001` through `AUTH-004`: catalog-backed department, role, individual ALLOW, and individual DENY sources.
- `GOV-007`, `GOV-008`, `SEC-001`, `SEC-002`, `SEC-005`: immutable, secret-free security audit foundation.
- Acceptance criteria `AUTH-02`, `AUTH-03`, and `SEC-02` at the database-foundation level. Runtime permission evaluation remains a later, separately authorized slice.

## Defects found in the unverified commit

- Test connection strings could be overridden through the environment without an exact catalog check.
- `MigrationRollbackTests` defaulted to `PTKD_TEST_PHASE1A`, and one API test opened `master`.
- Connections were opened before a central `InitialCatalog` validation and did not consistently verify `SELECT DB_NAME()` before writes.
- V0003 seeded only seven permissions, omitted required permission metadata and state, and did not reject `ENTITY`.
- Authentication, refresh-token, assignment, singleton, temporal, and audit columns/constraints were incomplete.
- Filtered unique indexes were described as complete temporal overlap controls even though they did not prevent general date-range overlap.
- The audit runtime database role was absent, audit JSON was not validated, and the unauthorized SQL view `vw_SECURITY_AUDIT_VIEW` was created.
- U0003 accepted any `PTKD_TEST_%` database, lacked a complete protected-data matrix, and was not fully transactional.
- Security tests relied on xUnit method order, used nonexistent V0002 `Users` columns, manually supplied a rowversion, and did not prove the required runtime role or rollback behavior.

## Corrections made

### Test database safety

`TestDatabaseSafety` now:

1. Parses every test connection string with `SqlConnectionStringBuilder`.
2. Requires `InitialCatalog` to equal `PTKD_TEST_PHASE1A2`, case-insensitively, before a connection is opened.
3. Rejects empty, `PTKD_DEV`, `PTKD_TEST_PHASE1A`, system, production-like, staging, UAT, and every other database name; an environment variable cannot bypass the check.
4. Opens only the validated connection string, executes `SELECT DB_NAME()`, and requires the returned value to equal `PTKD_TEST_PHASE1A2` before reset, migration, rollback, EF-writing integration setup, or API requests can write.
5. Does not create or drop a database and does not call `EnsureCreated`, `EnsureDeleted`, or `Database.Migrate`.

The exact observed value was:

```text
PTKD_TEST_PHASE1A2
```

`PTKD_DEV` rejection tests use a deliberately unreachable server name and succeed at the pre-open `InitialCatalog` guard, proving that the forbidden catalog is rejected without a connection attempt.

### Final V0003 object manifest

Tables:

- `User_Auth_Accounts`
- `Password_History`
- `Refresh_Tokens`
- `Permissions`
- `Roles`
- `Role_Permissions`
- `Department_Permissions`
- `User_Role_Assignments`
- `User_Individual_Permissions`
- `Admin_Groups`
- `Admin_Group_Permissions`
- `User_Admin_Group_Assignments`
- `Authorization_Policy_State`
- `Security_Bootstrap_State`
- `Security_Audit_Events`

Database role:

- `PTKD_Security_Audit_Runtime`

Triggers:

- `TR_Password_History_AppendOnly`
- `TR_Permissions_PreventDelete`
- `TR_Permissions_PreventCodeChange`
- `TR_User_Role_Assignments_PreventOverlap`
- `TR_User_Individual_Permissions_PreventOverlap`
- `TR_User_Admin_Group_Assignments_PreventOverlap`
- `TR_Security_Audit_Events_AppendOnly`

All V0003 primary keys, foreign keys, unique constraints, check constraints, default constraints, indexes, and triggers are explicitly named. No V0003 foreign key uses cascade delete. V0003 creates no SQL view and specifically does not create `vw_SECURITY_AUDIT_VIEW`.

### Exact permission seed catalog

V0003 seeds exactly these 15 immutable permission codes and no broad `ADMIN` permission:

1. `ORGANIZATION_COMPANY_VIEW`
2. `ORGANIZATION_COMPANY_MANAGE`
3. `ORGANIZATION_DEPARTMENT_VIEW`
4. `ORGANIZATION_DEPARTMENT_MANAGE`
5. `SECURITY_USER_VIEW`
6. `SECURITY_USER_MANAGE`
7. `SECURITY_ASSIGNMENT_MANAGE`
8. `SECURITY_ROLE_VIEW`
9. `SECURITY_ROLE_MANAGE`
10. `SECURITY_PERMISSION_VIEW`
11. `SECURITY_PERMISSION_MANAGE`
12. `SECURITY_ACCOUNT_MANAGE`
13. `SECURITY_ADMIN_GROUP_VIEW`
14. `SECURITY_ADMIN_GROUP_MANAGE`
15. `SECURITY_AUDIT_VIEW`

`permission_code` is the `VARCHAR(100)` natural primary key. `GLOBAL` and `COMPANY` are the only valid `data_scope` values; `ENTITY` is rejected.

### Temporal assignment controls

The database uses half-open `[effective_from, effective_to)` semantics. Each mutable assignment has lifecycle/date checks and rowversion.

Complete range-overlap defense uses multi-row-safe `AFTER INSERT, UPDATE` triggers with `UPDLOCK` and `HOLDLOCK` for:

- user + role;
- user + Admin Group;
- user + permission + scope + company + grant type.

The overlap predicate treats a NULL end as infinity. Adjacent periods are accepted. ALLOW and DENY are independent streams and can coexist for the same user, permission, scope, company, and dates. Filtered unique indexes prevent duplicate open ACTIVE rows as defense-in-depth only; they are not described as complete date-range controls.

### Audit controls

`Security_Audit_Events` contains actor, acting-as, target, company, stable event code, entity, changed fields, selected before/after JSON, reason, correlation ID, request metadata, outcome, policy version, and creation time. Nullable JSON text columns have `ISJSON` checks.

The `PTKD_Security_Audit_Runtime` role was verified with `USER WITHOUT LOGIN` plus `EXECUTE AS USER`:

| Permission | Effective result |
|---|---|
| SELECT | Granted; statement succeeded |
| INSERT | Granted; statement succeeded |
| UPDATE | Denied; statement rejected |
| DELETE | Denied; statement rejected |
| ALTER | Denied; statement rejected |
| TRUNCATE | Unavailable because ALTER is denied; statement rejected |

The append-only trigger separately rejects multi-row UPDATE and DELETE for privileged ordinary paths. A DML trigger does not intercept TRUNCATE. `db_owner` and `sysadmin` remain outside this database-role boundary.

SQL can validate JSON syntax but cannot guarantee semantic secret scrubbing. A later authorized application audit writer must sanitize payloads and exclude passwords, password hashes, raw tokens, signing keys, secrets, file bytes, and permanent signed URLs.

### U0003 protected-data behavior

U0003 requires the exact database `PTKD_TEST_PHASE1A2`, an exact V0003 `SchemaVersions` row, and no numerically later migration. It uses `SET XACT_ABORT ON`, `TRY/CATCH`, and one explicit transaction. The V0003 row is deleted only after every object drop succeeds.

| Protected state | Rollback behavior |
|---|---|
| `User_Auth_Accounts` | Blocked |
| `Password_History` | Blocked |
| `Refresh_Tokens` | Blocked |
| `Role_Permissions` | Blocked |
| `Department_Permissions` | Blocked |
| `User_Role_Assignments` | Blocked |
| `User_Individual_Permissions` | Blocked |
| `Admin_Group_Permissions` | Blocked |
| `User_Admin_Group_Assignments` | Blocked |
| `Security_Audit_Events` | Blocked |
| Any `Roles` row | Blocked |
| Any `Admin_Groups` row | Blocked |
| Permission catalog differs from exact approved seed | Blocked |
| Authorization policy state is non-pristine | Blocked |
| Bootstrap state is bootstrapped or non-pristine | Blocked |
| Missing V0003 record | Blocked |
| V0004 or any numerically later record | Blocked |
| Exact seeds plus pristine policy/bootstrap state only | Allowed |

A forced mid-rollback failure was verified to restore already-dropped triggers and retain every table, the runtime role, and the V0003 record.

## Files corrected

- `database/migrations/V0003__create_security_schema.sql`
- `database/rollbacks/U0003__drop_security_schema.sql`
- `tests/backend/PTKD.IntegrationTests/TestDatabaseSafety.cs`
- `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs`
- `tests/backend/PTKD.IntegrationTests/DatabaseSafetyTests.cs`
- `tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs`
- `tests/backend/PTKD.IntegrationTests/TransactionInvariantTests.cs`
- `tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs`
- `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs`
- `tests/backend/PTKD.ApiTests/OrganizationApiTests.Part2.cs`
- `docs/architecture/phase-1b0-security-discovery-decisions.md`
- `docs/architecture/phase-1b1-authentication-authorization-implementation-plan.md`
- `docs/architecture/phase-1b1a-security-database-foundation-implementation.md`

No project file, package reference, `global.json`, target framework, production configuration, API contract, or frontend file changed.

## Executable evidence

Commands and actual results:

```text
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --warnaserror
Build succeeded. 0 warnings, 0 errors.

dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore
Passed: 25, Failed: 0, Skipped: 0, Total: 25.

dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore
Passed: 104, Failed: 0, Skipped: 0, Total: 104.

dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore
Passed: 60, Failed: 0, Skipped: 0, Total: 60.

dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~SecuritySchemaTests"
Passed: 35, Failed: 0, Skipped: 0, Total: 35.
```

The final delivery gate reruns these exact commands after documentation review. The corrective commit subject is `Verify and correct Phase 1B.1-A security database foundation`; its hash is reported after the non-amended commit is created.

## Remaining limitations and manual verification

- Runtime application membership in `PTKD_Security_Audit_Runtime` is deployment/configuration work outside this slice; only the role boundary itself is created and tested.
- Application-layer permission evaluation, DENY precedence, session invalidation, bootstrap execution, audit sanitization, API authorization, UI visibility, and notifications remain later authorized slices.
- Production DBA review remains required before any production deployment, especially for role membership and privileged-principal boundaries.

Manual DBA review should inspect the named role permissions, confirm no login or permanent user is created, review the overlap-trigger query plans, and independently execute the clean/protected U0003 matrix on a disposable `PTKD_TEST_PHASE1A2` database.

## Conclusion

PHASE 1B.1-A VERIFIED AND CORRECTED — READY FOR PROJECT OWNER ACCEPTANCE
