# Phase 1B.0 - Security Discovery and Decisions

> **Status correction**: The authoritative decision register (`docs/decisions/phase-1b0-open-decisions.md`) now records the 20 active DEC-1B decisions as approved by the Project Owner under the Single-Owner Governance Model; DEC-1B-008 is merged and DEC-1B-017 remains the approved non-blocking deferral. The decision content below is unchanged.

## 1. Repository Baseline
- **.NET Executable:** `C:\Users\adm-bachdh\AppData\Local\Microsoft\dotnet\dotnet.exe`
- **Current Branch:** `feature/phase-1-organization`
- **Latest Commit:** `fe4e9c6 Complete Phase 1A.1 organization database foundation`
- **Git Status:** The repository contains numerous uncommitted files (Phase 1A.2 implementation). Phase 1A.2 is **still uncommitted and untagged**.

## 2. Existing Schema Summary
Please reference the authoritative Phase 1A.2 implementation report (`docs/architecture/phase-1a2-application-api-implementation.md`) and the `V0002` migration for the exact schema source of existing tables (`Users`, `Companies`, `Departments`, `User_Company_Assignments`, `User_Department_Assignments`, `Employment_Histories`).

## 3. The Authentication Model (APPROVED BASELINE)

**[DEC-1B-001, DEC-1B-002, DEC-1B-004, DEC-1B-013] Identity & Password Strategy:**
- Authentication explicitly requires:
  - `Users.account_status = ACTIVE`
  - `Users.employment_status` is an approved login-capable status: `ACTIVE`, `PROBATION`
  - Explictly denied states: `SUSPENDED`, `RESIGNED`, `TERMINATED`, `RETIRED`, `INACTIVE`
  - `User_Auth_Accounts.auth_account_status = ACTIVE`
  - Account is not currently locked
  - Security stamp/session is valid
- Password Hash Strategy: ASP.NET Core PasswordHasher.
- Password Policy: Minimum 8 characters, maximum 64 characters. Cannot contain normalized login name. No reuse of the previous 5 passwords. Temporary password expires after 24 hours. Reset revokes all active sessions. `must_change_password` blocks all non-password business endpoints.
- Lockout: Failed attempts and lockout values are configuration driven (Proposed: 5 fails = 15 minute lockout).

## 4. Core Schema Requirements (APPROVED BASELINE)

**`User_Auth_Accounts` [DEC-1B-001, DEC-1B-002]**
- `id` BIGINT IDENTITY primary key
- `user_id` BIGINT not null (FK to Users, No cascade delete)
- `provider_type` VARCHAR(30) not null
- `provider_subject` VARCHAR(200) not null
- `normalized_provider_subject` VARCHAR(200) not null
- `password_hash` VARCHAR(500) nullable (must be present for INTERNAL, NULL for external)
- `must_change_password` BIT not null
- `temporary_password_expires_at` DATETIME2(3) nullable
- `failed_attempt_count` INT not null
- `lockout_end` DATETIME2(3) nullable
- `auth_account_status` VARCHAR(30) not null (Check constraint: ACTIVE, LOCKED, DISABLED)
- `security_stamp` UNIQUEIDENTIFIER not null
- `row_version` ROWVERSION
- audit columns (`created_at` DATETIME2(3), `created_by_user_id` BIGINT, `updated_at` DATETIME2(3), `updated_by_user_id` BIGINT)
- **Constraints**: `UNIQUE(provider_type, normalized_provider_subject)`.

**`Password_History`**
- `id` BIGINT IDENTITY primary key
- `user_auth_account_id` BIGINT not null (FK to User_Auth_Accounts, No cascade delete)
- `password_hash` VARCHAR(500) not null
- `created_at` DATETIME2(3) not null
- `row_version` ROWVERSION
- **Constraints**: Index on `user_auth_account_id, created_at DESC`.

**`Refresh_Tokens` [DEC-1B-005]**
- `id` BIGINT IDENTITY primary key
- `user_auth_account_id` BIGINT not null (FK to User_Auth_Accounts, No cascade delete)
- `session_id` UNIQUEIDENTIFIER not null
- `token_hash` CHAR(64) not null
- `issued_at` DATETIME2(3) not null
- `expires_at` DATETIME2(3) not null
- `used_at` DATETIME2(3) nullable
- `revoked_at` DATETIME2(3) nullable
- `revoke_reason` VARCHAR(100) nullable
- `replaced_by_token_id` BIGINT nullable (FK to Refresh_Tokens)
- `reuse_detected_at` DATETIME2(3) nullable
- `created_ip_address` VARCHAR(45) nullable
- `created_user_agent` NVARCHAR(500) nullable
- `row_version` ROWVERSION
- **Constraints**: `UNIQUE(token_hash)`. Indexes on `session_id` and `user_auth_account_id`.

**`Permissions` [DEC-1B-006]**
- `permission_code` VARCHAR(100) primary key
- `module_code` VARCHAR(50) not null
- `action_code` VARCHAR(50) not null
- `name` NVARCHAR(200) not null
- `description` NVARCHAR(500) nullable
- `data_scope` VARCHAR(30) not null (Check constraint: GLOBAL, COMPANY, ENTITY)
- `is_sensitive` BIT not null
- `is_delegable` BIT not null
- `requires_reason` BIT not null
- `is_active` BIT not null
- `row_version` ROWVERSION
- audit columns

**`Roles`**
- `id` BIGINT IDENTITY primary key
- `name` NVARCHAR(100) not null
- `description` NVARCHAR(500) nullable
- `scope_type` VARCHAR(30) not null (Check constraint: GLOBAL, COMPANY)
- `is_active` BIT not null
- `row_version` ROWVERSION
- audit columns
- **Constraints**: `UNIQUE(name)`.

**`Role_Permissions`**
- `role_id` BIGINT not null (FK to Roles, No cascade delete)
- `permission_code` VARCHAR(100) not null (FK to Permissions, No cascade delete)
- `created_at` DATETIME2(3) not null
- `created_by_user_id` BIGINT not null
- **Constraints**: PK (`role_id`, `permission_code`).

**`Department_Permissions`**
- `department_id` BIGINT not null (FK to Departments, No cascade delete)
- `permission_code` VARCHAR(100) not null (FK to Permissions, No cascade delete)
- `created_at` DATETIME2(3) not null
- `created_by_user_id` BIGINT not null
- **Constraints**: PK (`department_id`, `permission_code`).

**`Admin_Groups`**
- `id` BIGINT IDENTITY primary key
- `name` NVARCHAR(100) not null
- `description` NVARCHAR(500) nullable
- `scope_type` VARCHAR(30) not null (Check constraint: GLOBAL, COMPANY)
- `is_active` BIT not null
- `row_version` ROWVERSION
- audit columns
- **Constraints**: `UNIQUE(name)`.

**`Admin_Group_Permissions`**
- `admin_group_id` BIGINT not null (FK to Admin_Groups, No cascade delete)
- `permission_code` VARCHAR(100) not null (FK to Permissions, No cascade delete)
- `created_at` DATETIME2(3) not null
- `created_by_user_id` BIGINT not null
- **Constraints**: PK (`admin_group_id`, `permission_code`).

**`User_Role_Company` (Temporal Assignment)**
- `id` BIGINT IDENTITY primary key
- `user_id` BIGINT not null (FK to Users)
- `role_id` BIGINT not null (FK to Roles)
- `company_id` BIGINT nullable (FK to Companies)
- `effective_from` DATETIME2(3) not null
- `effective_to` DATETIME2(3) nullable
- `is_active` BIT not null
- `row_version` ROWVERSION
- audit columns
- **Constraints**: Company scope matches role scope type. Half-open date overlap trigger. Filtered unique index to prevent overlapping active records. No cascade delete.

**`User_Individual_Permissions` (Temporal Assignment)**
- `id` BIGINT IDENTITY primary key
- `user_id` BIGINT not null (FK to Users)
- `permission_code` VARCHAR(100) not null (FK to Permissions)
- `company_id` BIGINT nullable (FK to Companies)
- `effective_from` DATETIME2(3) not null
- `effective_to` DATETIME2(3) nullable
- `is_active` BIT not null
- `is_deny` BIT not null
- `row_version` ROWVERSION
- audit columns
- **Constraints**: Same overlap/temporal requirements as User_Role_Company. No cascade delete.

**`User_Admin_Group_Assignments` (Temporal Assignment)**
- `id` BIGINT IDENTITY primary key
- `user_id` BIGINT not null (FK to Users)
- `admin_group_id` BIGINT not null (FK to Admin_Groups)
- `company_id` BIGINT nullable (FK to Companies)
- `effective_from` DATETIME2(3) not null
- `effective_to` DATETIME2(3) nullable
- `is_active` BIT not null
- `row_version` ROWVERSION
- audit columns
- **Constraints**: Scope logic explicitly requires `company_id` to be NULL if Admin_Group `scope_type` = GLOBAL, and NOT NULL if `scope_type` = COMPANY. Same temporal overlap protections. No cascade delete.

**`Authorization_Policy_State`**
- `id` INT not null primary key (Single row constraint)
- `policy_version` BIGINT not null
- `last_invalidated_at` DATETIME2(3) not null
- `invalidated_by_user_id` BIGINT nullable
- `row_version` ROWVERSION

**`Security_Audit_Events`**
- `id` BIGINT IDENTITY primary key
- `event_type` VARCHAR(100) not null
- `actor_user_id` BIGINT nullable
- `actor_ip_address` VARCHAR(45) nullable
- `entity_type` VARCHAR(100) not null
- `entity_id` VARCHAR(100) not null
- `company_id` BIGINT nullable
- `action` VARCHAR(50) not null
- `before_state_json` NVARCHAR(MAX) nullable
- `after_state_json` NVARCHAR(MAX) nullable
- `timestamp` DATETIME2(3) not null
- `correlation_id` UNIQUEIDENTIFIER nullable
- **Constraints**: Runtime principal has INSERT/SELECT only. No UPDATE/DELETE/TRUNCATE. Trigger blocks UPDATE/DELETE. No cascade delete.

**`Security_Bootstrap_State`**
- `id` INT not null primary key (Single row constraint)
- `is_bootstrapped` BIT not null
- `bootstrapped_at` DATETIME2(3) not null
- `row_version` ROWVERSION

### Temporal Overlap Control [DEC-1B-014] (APPROVED BASELINE)
**Primary proposal:**
- Use a `SERIALIZABLE` transaction.
- Query the natural-key date range using `UPDLOCK` and `HOLDLOCK`.
- Validate half-open overlap using: `existing.effective_from < requested.effective_to AND requested.effective_from < existing.effective_to`. Treat NULL effective_to as infinity.
- Use a fresh DbContext and transaction for every retry. Retry only SQL Server error 1205. Use the existing maximum retry convention from Phase 1A.2.
**Defense in depth:**
- SQL `AFTER INSERT, UPDATE` overlap trigger. Filtered unique index preventing more than one open assignment for the same natural key.
- Database stable error mapping. No application-only overlap enforcement.
- Apply consistently to `User_Role_Company`, `User_Individual_Permissions`, `User_Admin_Group_Assignments`.

### Audit Database Controls [DEC-1B-015, DEC-1B-017] (APPROVED BASELINE)
- **Primary enforcement**: Runtime principal has `INSERT` and `SELECT` only. No `UPDATE`, `DELETE`, `TRUNCATE`, or cascade deletes.
- **Defense in depth**: SQL trigger blocks UPDATE and DELETE. EF interceptor provides fail-fast application protection. Dapper and raw SQL paths are tested.
- No password hash, token, signing key or secret in audit data. Stable SQL error mapped to stable application error. Event identity is immutable.
- No purge/archive in Phase 1B. Current audit records remain in the database. Long-term retention/archive is a separate compliance decision.

## 5. JWT and Current-Company Consistency [DEC-1B-003, DEC-1B-012, DEC-1B-018, DEC-1B-019] (APPROVED BASELINE)

**JWT & Client Token Storage:**
- Claims: `sub`, `sid`, `login_name/provider_subject`, `security_stamp`, `iat`, `exp`, `jti`.
- Do not include `current_company_id` as authoritative.
- Access token stored in memory. Refresh token stored in `HttpOnly`, `Secure`, `SameSite` cookie, based on approved deployment topology.

**Current Company Rules:**
- `X-Company-Id` is the requested COMPANY scope. The server validates an active user-company assignment. JWT does not authorize company access.
- `switch-company` API is NOT part of the proposed contract. (Switch-company tests removed).
- GLOBAL endpoints do not require `X-Company-Id`. COMPANY endpoints require `X-Company-Id`.
- Do not silently fall back to primary company for state-changing endpoints. Missing company context returns `AUTH_CURRENT_COMPANY_REQUIRED` (400 or 403, explicitly decide and document).

**Signing-Key Provider and Rotation:**
- Dev: User secrets. Prod/Staging: Azure Key Vault/injected secret. Min 256-bit (HMAC-SHA256). Uses `kid` for rotation. 24h old-key validation window. Startup fails if missing/unsafe. No committed keys.

## 6. Bootstrap Strategy [DEC-1B-010] (APPROVED BASELINE)
**Production-safe model:**
- Separate controlled bootstrap executable or command (never runs automatically during API startup). Executed only by an authorized operator.
- Reads initial secret from an approved enterprise secret provider or protected deployment input. Never prints password, token or secret.
- Creates the auth account and initial admin-group assignment in one transaction. Sets `must_change_password = 1`. Writes immutable `BOOTSTRAP_ADMIN_CREATED` audit.
- Records a persistent one-time bootstrap marker. Rejects all subsequent bootstrap attempts.
- Does not require the Users table or entire database to be empty. Fails if bootstrap is already completed or an active initial security administrator already exists.

## 7. Permission Evaluator & Cache [DEC-1B-011] (APPROVED BASELINE)
- DB `policy_version` read on every protected request. Cache key includes version. DB read failure must fail closed.
- Account, session, and company checks occur before cache use.
- Immediate permission changes must be effective on the next protected request after the policy-version change.

## 8. Explicit Permission Codes [DEC-1B-016, DEC-1B-021] (APPROVED BASELINE)
- `ORGANIZATION_COMPANY_VIEW`
- `ORGANIZATION_COMPANY_MANAGE`
- `ORGANIZATION_DEPARTMENT_VIEW`
- `ORGANIZATION_DEPARTMENT_MANAGE`
- `SECURITY_USER_VIEW`
- `SECURITY_USER_MANAGE`
- `SECURITY_ASSIGNMENT_MANAGE`
- `SECURITY_ROLE_VIEW`
- `SECURITY_ROLE_MANAGE`
- `SECURITY_PERMISSION_VIEW`
- `SECURITY_PERMISSION_MANAGE`
- `SECURITY_ACCOUNT_MANAGE`
- `SECURITY_ADMIN_GROUP_VIEW`
- `SECURITY_ADMIN_GROUP_MANAGE`
- `SECURITY_AUDIT_VIEW` *(DEC-1B-021 resolved this as a distinct security-administration boundary from canonical `AUDIT_VIEW`.)*

## 9. API and Error Map Consistency (APPROVED BASELINE)

**Error Map (Single Source of HTTP Status Truth):**
- `AUTH_INVALID_CREDENTIALS` = 401
- `AUTH_TOKEN_INVALID` = 401
- `AUTH_TOKEN_EXPIRED` = 401
- `AUTH_REFRESH_TOKEN_INVALID` = 401
- `AUTH_REFRESH_TOKEN_REUSED` = 401
- `AUTH_SESSION_REVOKED` = 401
- `AUTH_SECURITY_STAMP_CHANGED` = 401
- `AUTH_ACCOUNT_DISABLED` = 403
- `AUTH_PASSWORD_CHANGE_REQUIRED` = 403
- `AUTH_ACCOUNT_LOCKED` = 403 or 423 (pending Security approval)
- `AUTH_PERMISSION_DENIED` = 403
- `AUTH_COMPANY_SCOPE_DENIED` = 403
- `AUTH_CURRENT_COMPANY_REQUIRED` = 400 or 403 (explicitly decide and document)
- `AUTH_ROLE_ASSIGNMENT_OVERLAP` = 409
- `AUTH_INDIVIDUAL_PERMISSION_OVERLAP` = 409
- `AUTH_ADMIN_GROUP_ASSIGNMENT_OVERLAP` = 409
- `AUTH_PERMISSION_CATALOG_INACTIVE` = 409 or 422
- `AUTH_BOOTSTRAP_DISABLED` = 409
- `AUTH_UNEXPECTED_DATABASE_ERROR` = 500

**API Contracts:** *(Note: Every endpoint returns exact statuses from the Error Map. Rowversion applies to all modifications. Transaction and Audit apply to all mutating actions).*
- **Authentication**:
  - `POST /api/v2/auth/login` (No Perm, GLOBAL. Req: LoginRequest. Res: LoginResponse. Tx: Yes. Audit: LOGIN)
  - `POST /api/v2/auth/refresh` (No Perm, GLOBAL. Req: RefreshRequest. Res: TokenResponse. Tx: Yes. Audit: REFRESH)
  - `POST /api/v2/auth/logout` (No Perm, GLOBAL. Req: None. Res: 204. Tx: Yes. Audit: SESSION_REVOKE)
  - `POST /api/v2/auth/change-password` (No Perm, GLOBAL. Req: ChangePasswordRequest. Res: 204. Tx: Yes. Audit: PASSWORD_CHANGE)
  - `GET /api/v2/auth/me` (No Perm, GLOBAL. Req: None. Res: UserProfileResponse)
  - `GET /api/v2/auth/my-permissions` (No Perm, COMPANY/GLOBAL. Req: None. Res: UserPermissionsResponse)
- **Security Administration**:
  - `GET /api/v2/security/permissions` (SECURITY_PERMISSION_VIEW, GLOBAL. Req: None. Res: PermissionListResponse)
  - `GET /api/v2/security/roles` (SECURITY_ROLE_VIEW, GLOBAL. Req: None. Res: RoleListResponse)
  - `GET /api/v2/security/roles/{id}` (SECURITY_ROLE_VIEW, GLOBAL. Req: None. Res: RoleDetailResponse)
  - `POST /api/v2/security/roles` (SECURITY_ROLE_MANAGE, GLOBAL. Req: CreateRoleRequest. Res: RoleDetailResponse. Tx: Yes. Audit: ROLE_CREATE)
  - `PUT /api/v2/security/roles/{id}` (SECURITY_ROLE_MANAGE, GLOBAL. Req: UpdateRoleRequest. Res: RoleDetailResponse. Tx: Yes. Audit: ROLE_UPDATE. Rowversion: Yes)
  - `PUT /api/v2/security/roles/{id}/status` (SECURITY_ROLE_MANAGE, GLOBAL. Req: UpdateStatusRequest. Res: 204. Tx: Yes. Audit: ROLE_STATUS. Rowversion: Yes)
  - `PUT /api/v2/security/roles/{id}/permissions` (SECURITY_ROLE_MANAGE, GLOBAL. Req: ReplacePermissionsRequest. Res: 204. Tx: Yes. Audit: ROLE_PERMS. Rowversion: Yes)
  - `GET /api/v2/security/departments/{id}/permissions` (SECURITY_ROLE_VIEW, GLOBAL. Req: None. Res: PermissionListResponse)
  - `PUT /api/v2/security/departments/{id}/permissions` (SECURITY_ROLE_MANAGE, GLOBAL. Req: ReplacePermissionsRequest. Res: 204. Tx: Yes. Audit: DEPT_PERMS. Rowversion: Yes)
  - `POST /api/v2/security/users/{id}/roles/assign` (SECURITY_ASSIGNMENT_MANAGE, COMPANY/GLOBAL. Req: AssignRoleRequest. Res: 204. Tx: Yes. Audit: USER_ROLE_ASSIGN)
  - `POST /api/v2/security/users/{id}/roles/close` (SECURITY_ASSIGNMENT_MANAGE, COMPANY/GLOBAL. Req: CloseAssignmentRequest. Res: 204. Tx: Yes. Audit: USER_ROLE_CLOSE)
  - `POST /api/v2/security/users/{id}/individual-permissions/grant` (SECURITY_ASSIGNMENT_MANAGE, COMPANY/GLOBAL. Req: GrantPermissionRequest. Res: 204. Tx: Yes. Audit: USER_PERM_GRANT)
  - `POST /api/v2/security/users/{id}/individual-permissions/revoke` (SECURITY_ASSIGNMENT_MANAGE, COMPANY/GLOBAL. Req: RevokePermissionRequest. Res: 204. Tx: Yes. Audit: USER_PERM_REVOKE)
  - `GET /api/v2/security/admin-groups` (SECURITY_ADMIN_GROUP_VIEW, GLOBAL. Req: None. Res: AdminGroupListResponse)
  - `GET /api/v2/security/admin-groups/{id}` (SECURITY_ADMIN_GROUP_VIEW, GLOBAL. Req: None. Res: AdminGroupDetailResponse)
  - `POST /api/v2/security/admin-groups` (SECURITY_ADMIN_GROUP_MANAGE, GLOBAL. Req: CreateAdminGroupRequest. Res: AdminGroupDetailResponse. Tx: Yes. Audit: ADMINGROUP_CREATE)
  - `PUT /api/v2/security/admin-groups/{id}` (SECURITY_ADMIN_GROUP_MANAGE, GLOBAL. Req: UpdateAdminGroupRequest. Res: AdminGroupDetailResponse. Tx: Yes. Audit: ADMINGROUP_UPDATE. Rowversion: Yes)
  - `PUT /api/v2/security/admin-groups/{id}/status` (SECURITY_ADMIN_GROUP_MANAGE, GLOBAL. Req: UpdateStatusRequest. Res: 204. Tx: Yes. Audit: ADMINGROUP_STATUS. Rowversion: Yes)
  - `PUT /api/v2/security/admin-groups/{id}/permissions` (SECURITY_ADMIN_GROUP_MANAGE, GLOBAL. Req: ReplacePermissionsRequest. Res: 204. Tx: Yes. Audit: ADMINGROUP_PERMS. Rowversion: Yes)
  - `POST /api/v2/security/admin-groups/{id}/users/assign` (SECURITY_ACCOUNT_MANAGE, GLOBAL/COMPANY. Req: AssignAdminGroupRequest. Res: 204. Tx: Yes. Audit: USER_ADMINGROUP_ASSIGN)
  - `POST /api/v2/security/admin-groups/{id}/users/close` (SECURITY_ACCOUNT_MANAGE, GLOBAL/COMPANY. Req: CloseAssignmentRequest. Res: 204. Tx: Yes. Audit: USER_ADMINGROUP_CLOSE)
  - `GET /api/v2/security/accounts/{id}` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: None. Res: AccountDetailResponse)
  - `POST /api/v2/security/accounts/{id}/activate` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: ActivateAccountRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_ACTIVATE. Rowversion: Yes)
  - `POST /api/v2/security/accounts/{id}/disable` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: DisableAccountRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_DISABLE. Rowversion: Yes)
  - `POST /api/v2/security/accounts/{id}/lock` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: LockAccountRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_LOCK. Rowversion: Yes)
  - `POST /api/v2/security/accounts/{id}/unlock` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: UnlockAccountRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_UNLOCK. Rowversion: Yes)
  - `POST /api/v2/security/accounts/{id}/reset` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: ResetPasswordRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_RESET. Rowversion: Yes)
  - `POST /api/v2/security/accounts/{id}/revoke-all-sessions` (SECURITY_ACCOUNT_MANAGE, GLOBAL. Req: RevokeSessionsRequest. Res: 204. Tx: Yes. Audit: ACCOUNT_SESSIONS_REVOKED)
  - `GET /api/v2/security/users/{id}/effective-permissions` (SECURITY_ASSIGNMENT_MANAGE, COMPANY. Req: None. Res: EffectivePermissionsResponse)
  - `GET /api/v2/security/audit` (SECURITY_AUDIT_VIEW, GLOBAL/COMPANY. Req: QueryParams. Res: AuditListResponse)
  - `GET /api/v2/security/audit/{id}` (SECURITY_AUDIT_VIEW, GLOBAL/COMPANY. Req: None. Res: AuditDetailResponse)

## 10. Test Traceability Correction (APPROVED BASELINE)

| Test Method | Test Layer | Business rule IDs | Acceptance criterion IDs | DEC-1B IDs | Expected Result | Database |
|---|---|---|---|---|---|---|
| `Evaluator_Department_ALLOW_Base_Permissions` | Unit | AUTH-001 | AUTH-01 | - | True | - |
| `Evaluator_Role_ALLOW_Company_Permissions` | Unit | AUTH-002, AUTH-008 | AUTH-02 | DEC-1B-007 | True | - |
| `Evaluator_Individual_DENY_Overrides_Role` | Unit | AUTH-004 | AUTH-03 | - | True | - |
| `ProtectedEndpoint_CrossCompanyDenial_Returns_403` | API | AUTH-007, AUTH-009 | AUTH-04 | DEC-1B-012 | 403 | PTKD_TEST_PHASE1B |
| `PolicyVersion_Read_Invalidates_Old_Cache` | Unit | AUTH-012 | AUTH-06 | DEC-1B-011 | Instantly flushed | - |
| `ProtectedEndpoint_Returns_403_When_Unauthorized` | API | AUTH-009 | SEC-01 | - | 403 | PTKD_TEST_PHASE1B |
| `SecurityAuditEvents_Is_AppendOnly` | Integration | SEC-001 | SEC-02 | DEC-1B-015 | Immutable | PTKD_TEST_PHASE1B |
| `ProblemDetails_Is_Sanitized` | API | SEC-004 | - | DEC-1B-015 | Sanitized JSON | PTKD_TEST_PHASE1B |
| `AuditData_Contains_No_Passwords_Or_Tokens` | Integration | SEC-005 | - | DEC-1B-015 | Clean JSON | PTKD_TEST_PHASE1B |
| `Tests_Reject_PTKD_DEV_BeforeAnyWrite` | Integration | DATA-003 | - | - | Rejects execution | PTKD_TEST_PHASE1B |
| `AuthAccounts_LoginName_Is_Unique` | Integration | - | - | DEC-1B-001 | True | PTKD_TEST_PHASE1B |
| `PasswordHasher_Uses_AspNet_Implementation` | Unit | - | - | DEC-1B-002 | Hashed | - |
| `PasswordHistory_Prevents_Reuse_Of_Last_5` | Unit | - | - | DEC-1B-002 | False | - |
| `TemporaryPassword_Fails_After_Expiry` | Unit | - | - | DEC-1B-002 | False | - |
| `AccountLockout_Is_Triggered_On_Failures` | Integration | - | - | DEC-1B-004 | Locked out | PTKD_TEST_PHASE1B |
| `RefreshToken_Rotation_Updates_Token` | API | - | - | DEC-1B-005 | Rotated | PTKD_TEST_PHASE1B |
| `RefreshToken_Reuse_Revokes_Session_Family` | API | - | - | DEC-1B-005 | Revoked | PTKD_TEST_PHASE1B |
| `RefreshToken_Concurrent_Request_Succeeds` | API | - | - | DEC-1B-005 | Deterministic | PTKD_TEST_PHASE1B |
| `SecurityStamp_Change_Invalidates_Token` | API | - | - | DEC-1B-001 | 401 | PTKD_TEST_PHASE1B |
| `BootstrapCommand_Runs_Once_Only` | Integration | - | - | DEC-1B-010 | Rejects 2nd | PTKD_TEST_PHASE1B |
| `AdminGroup_Scope_Validates_Company_Id` | Unit | - | - | DEC-1B-007, DEC-1B-009 | Rejects | - |
| `UserRoleCompany_Temporal_Overlap_Fails` | Integration | - | - | DEC-1B-014 | Fails | PTKD_TEST_PHASE1B |
| `UserIndividualPerms_Temporal_Overlap_Fails` | Integration | - | - | DEC-1B-014 | Fails | PTKD_TEST_PHASE1B |
| `UserAdminGroup_Temporal_Overlap_Fails` | Integration | - | - | DEC-1B-014 | Fails | PTKD_TEST_PHASE1B |
| `AuditDatabase_Blocks_Update_Delete_Truncate` | Integration | SEC-001 | - | DEC-1B-015 | SQL Error | PTKD_TEST_PHASE1B |
| `MigrationRollback_V0003_U0003_Succeeds` | Integration | - | - | - | Applies & Rolls Back | PTKD_TEST_PHASE1B |

*(Note: Test matrix guarantees all 147 Phase 1A.2 tests remain as mandatory regression. No EnsureCreated or EF Migrate is permitted in application startup).*

## 11. Final Status & Blockers

**READY FOR PHASE 1B.1 PLANNING**

**DOCUMENTATION STATUS:**
APPROVED BY PROJECT OWNER

**Blockers preventing implementation:**
None. Phase 1B.0 decisions are explicitly approved by the Project Owner.
