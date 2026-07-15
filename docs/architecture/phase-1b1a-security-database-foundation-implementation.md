# Phase 1B.1-A Security Database Foundation Implementation

## Authorization Scope
Phase 1B.1-A authorized scope only. Implementation of Phase 1B.1-B through I remains NOT AUTHORIZED.

## Corrected Plan Commit
- **Commit:** 157f979743a1a9e6cf2efb132e022fbe9871fc31 (Note: This is the actual hash from Stage 1 commit)
- **Parent:** 55425c4007b3598400af1d8e1b5a140c8a648fa4

## Repository Baseline
- `PTKD_TEST_PHASE1A2` Database safety conventions
- `net10.0` Target framework

## Exact Files Changed
- `database/migrations/V0003__create_security_schema.sql` (Created)
- `database/rollbacks/U0003__drop_security_schema.sql` (Created)
- `tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs` (Created)

## Exact V0003 Objects Created
- Tables: `Security_Audit_Events`, `User_Auth_Accounts`, `Password_History`, `Refresh_Tokens`, `Permissions`, `Roles`, `Role_Permissions`, `Department_Permissions`, `Admin_Groups`, `Admin_Group_Permissions`, `User_Role_Assignments`, `User_Individual_Permissions`, `User_Admin_Group_Assignments`, `Authorization_Policy_State`, `Security_Bootstrap_State`
- View: `vw_SECURITY_AUDIT_VIEW`
- Trigger: `TR_Security_Audit_Events_PreventUpdateDelete`
- Indexes: Active overlap filtered indexes on assignment tables.
- Constraints: Strict PK/FK, unique constraints, CHECK constraints on dates and status.

## Exact U0003 Safety Behavior
- Refuses execution if V0004+ is applied.
- Refuses execution if DB name does not start with `PTKD_TEST_`.
- Refuses execution if `User_Auth_Accounts` contains material data (populated rows).
- Drops objects in safe reverse-dependency order.

## Database Role/Permission Model
- Relies on application-level execution under a configured login. 
- Trigger explicitly denies `UPDATE` and `DELETE` on `Security_Audit_Events` for all runtime paths.
- `TRUNCATE` is prevented by withholding `ALTER` permissions. 
- Sysadmin limitations are explicitly documented (sysadmin can bypass triggers/truncate).

## Schema Manifest
- **User_Auth_Accounts:** id (bigint PK), user_id (bigint FK), provider_type, provider_subject (UQ pair), password_hash, account_status, failed_login_attempts, lockout_end, row_version.
- **Password_History:** id (PK), account_id (FK), password_hash, created_at.
- **Refresh_Tokens:** id (PK), account_id (FK), token_hash (UQ), family_id, expires_at, is_revoked.
- **Permissions:** permission_code (VARCHAR 100 PK), description.
- **Roles:** id (PK), role_code (UQ), company_id (FK), scope_type, is_active, row_version.
- **Role_Permissions:** role_id (FK), permission_code (FK).
- **Department_Permissions:** department_id (FK), permission_code (FK).
- **User_Role_Assignments:** id (PK), user_id (FK), role_id (FK), assignment_status, effective_from, effective_to. Filtered unique index for active overlap prevention.
- **User_Individual_Permissions:** id (PK), user_id (FK), company_id (FK), permission_code (FK), grant_type (ALLOW/DENY). Filtered unique index.
- **Admin_Groups:** id (PK), group_code (UQ), company_id (FK), scope_type.
- **Admin_Group_Permissions:** admin_group_id (FK), permission_code (FK).
- **User_Admin_Group_Assignments:** id (PK), user_id (FK), admin_group_id (FK), status/dates. Filtered UQ.
- **Authorization_Policy_State:** id (PK), policy_version.
- **Security_Bootstrap_State:** id (PK), is_bootstrapped.
- **Security_Audit_Events:** id (PK), event_type, actor/target, reason, before/after JSON. Append-only via trigger.

## Test Traceability to DEC-1B Decisions
- **DEC-1B-001:** `User_Auth_Accounts` separation covered by `V0003_Executes_ExactlyOnce_And_U0003_RollsBack_Safely`.
- **DEC-1B-014:** `UserRoleAssignments_Cannot_Have_Overlapping_Active_Records` prevents active temporal overlaps.
- **DEC-1B-015:** `AuditEvents_CannotBe_DeletedOrUpdated` asserts INSTEAD OF trigger behavior.
- **DEC-1B-007/009:** `Role_Scope_Constraints_Enforced` validates `company_id` and `scope_type` logic.

## Exact Build Result
```text
The command could not be loaded, possibly because:
  * You intended to execute a .NET application:
      The application 'build' does not exist.
  * You intended to execute a .NET SDK command:
      A compatible .NET SDK was not found.

Requested SDK version: 10.0.301
global.json file: C:\Projects\PTKD-ERP\global.json

Installed SDKs:
8.0.423 [C:\Program Files\dotnet\sdk]
```

## Unit/Integration/API Test Results
**Unit Tests:** Skipped (Build Failed)
**Integration Tests:** Skipped (Build Failed)
**API Tests:** Skipped (Build Failed)
**New Migration Test Results:** Skipped (Build Failed)

## Exact Database Used
`PTKD_TEST_PHASE1A2` (Declared in test fixture)

## Confirmations
- PTKD_DEV was never written: CONFIRMED.
- No package was added: CONFIRMED.
- No API, frontend, JWT or application authorization was implemented: CONFIRMED.

## Known Limitations
- The build environment currently only has .NET SDK 8.0.423 installed, while the `global.json` and `.csproj` files demand .NET 10.0.301. The execution failed at the build verification step.

## Deferred Work for Slices B-I
- Authentication domain, Password hashing, JWT generation, Role/Permission APIs, Frontend interceptors and administration UI remain deferred to Slices B through I.

## Conclusion
PHASE 1B.1-A FAILED — DO NOT CONTINUE TO PHASE 1B.1-B
