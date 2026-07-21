# Phase 1B.1-F-B — Initial Admin Bootstrap Implementation Plan

## Status

PHASE 1B.1-F-B IMPLEMENTATION ACCEPTED — SEE phase-1b1f-b-project-owner-implementation-acceptance.md

Primary blocker:
- SECURITY_ADMIN_MANAGE exists in PermissionCodes.cs and permission-catalog.md
  but is absent from the V0003 database seed data.
- OD-F-B-06 requires stopping when SECURITY_ADMIN_MANAGE is missing from the
  database, rather than silently using a different permission.

Project Owner corrective direction — Option A selected:
- Create a new V0004/U0004 migration in a separate F-B0 corrective slice.
- Do not amend V0003.
- Do not revise OD-F-B-06 to use a different permission.
- F-B implementation remains blocked until F-B0 is implemented and accepted.

SEE ALSO: phase-1b1f-b-project-owner-plan-acceptance.md

---

## Implementation-spec corrections required before F-B implementation

The following schema discrepancies were discovered during F-B implementation
discovery. F-B implementation must use the actual schema, not the plan prose.

### Correction A — User_Auth_Accounts: no normalized_provider_subject column

The F-B plan (OD-F-B-05) refers to `normalized_provider_subject` as a column
in `User_Auth_Accounts`. The actual V0003 schema does not have this column.
`User_Auth_Accounts` has `provider_subject` only (with a unique constraint on
`(provider_type, provider_subject)`). The unique constraint key is
`UQ_UserAuthAccounts_ProviderSubject` on `(provider_type, provider_subject)`.
F-B implementation must reference `provider_subject` only and must not attempt
to write to a non-existent `normalized_provider_subject` column.

### Correction B — Password_History: FK column is account_id

The F-B plan prose implies the FK column connecting `Password_History` to
`User_Auth_Accounts` might be named `user_auth_account_id`. The actual V0003
schema names this column `account_id` (with FK constraint
`FK_PasswordHistory_Account` referencing `dbo.User_Auth_Accounts(id)`).
F-B implementation must use `account_id`, not `user_auth_account_id`.

### Correction C — Admin_Groups.name is NOT NULL

The `Admin_Groups` table has `name nvarchar(200) NOT NULL` (line 374 of V0003).
OD-F-B-06 specifies creating the ADMIN_SECURITY group but does not explicitly
list all required NOT NULL columns. F-B implementation must populate at minimum:
`group_code`, `name`, `scope_type`, `is_active`.
A suitable `name` value for the bootstrap-created ADMIN_SECURITY group must be
decided during the authorized F-B implementation task (e.g.,
`N'Security Administration'`).

---

## Baseline

- Current accepted HEAD: `fb023cfc7af188d11b6e2c621dd913e25574fd63`
- F-A Audit Writer foundation accepted in commit `38675ff690e0491f2348a52abfa364c63c6f6b2b`.
- F-B Bootstrap not started. No PTKD.Bootstrap project exists.
- V0003 already contains `Security_Bootstrap_State` and `Security_Audit_Events` tables.
- No Phase F-B implementation has been authorized yet.
- This plan is documentation-only. No code was written while producing it.

---

## Purpose

Phase F-B introduces a controlled one-time initial administrator bootstrap path. Without it, a freshly deployed database has no authenticated user holding `SECURITY_ADMIN_MANAGE`, making the entire security administration API permanently inaccessible.

F-B must:
- Create the first System Administrator account through the existing auth model.
- Assign minimal security admin capability through the existing Admin Group model.
- Write an immutable `BOOTSTRAP_ADMIN_CREATED` audit event.
- Mark bootstrap complete using the existing `Security_Bootstrap_State` singleton.
- Reject all subsequent bootstrap attempts durably.
- Never print, log, store, or expose the initial password in any form.
- Run only on explicit operator invocation, never automatically on API startup.

---

## Discovery findings

### 1. Security_Bootstrap_State exact schema (V0003)

```sql
CREATE TABLE dbo.Security_Bootstrap_State (
    id int NOT NULL,
    is_bootstrapped bit NOT NULL DEFAULT (0),
    bootstrapped_at datetime2(3) NULL,
    bootstrapped_by_user_id bigint NULL,  -- FK → dbo.Users(id)
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Security_Bootstrap_State PRIMARY KEY (id),
    CONSTRAINT CK_SecurityBootstrapState_Singleton CHECK (id = 1),
    CONSTRAINT CK_SecurityBootstrapState_Consistency CHECK (
        (is_bootstrapped = 0 AND bootstrapped_at IS NULL AND bootstrapped_by_user_id IS NULL)
        OR (is_bootstrapped = 1 AND bootstrapped_at IS NOT NULL AND bootstrapped_by_user_id IS NOT NULL)
    ),
    CONSTRAINT FK_SecurityBootstrapState_BootstrappedBy
        FOREIGN KEY (bootstrapped_by_user_id) REFERENCES dbo.Users(id)
);
-- Seeded: (1, 0, NULL, NULL)
```

**Finding:** The table is a durable singleton already seeded in unbootstrapped state. The consistency constraint requires `bootstrapped_by_user_id` (FK to Users.id) to be NOT NULL when `is_bootstrapped = 1`. Bootstrap must therefore create the User row first, obtain the user id, then mark the bootstrap state. This FK coupling forces all writes to occur within the same database transaction.

### 2. Security_Bootstrap_State supports one-time durable marker

Yes. The singleton structure, the consistency constraint, and the seeded row collectively enforce a single-row marker. Once `is_bootstrapped = 1` is committed, the constraint and FK make it permanently auditable. Concurrent race is the remaining risk (addressed in OD-F-B-09).

### 3. Tables and entities needed to create the admin account

The bootstrap must write to the following existing tables:

| Table | Purpose |
|---|---|
| `dbo.Users` | The primary user record |
| `dbo.User_Auth_Accounts` | INTERNAL provider auth account with hashed password |
| `dbo.Password_History` | Initial password hash record (for future reuse enforcement) |
| `dbo.Admin_Groups` | ADMIN_SECURITY group — must be created if absent |
| `dbo.Admin_Group_Permissions` | SECURITY_ADMIN_MANAGE → ADMIN_SECURITY mapping — must be created if absent |
| `dbo.User_Admin_Group_Assignments` | Assigns bootstrap user to ADMIN_SECURITY |
| `dbo.Security_Bootstrap_State` | One-time marker update |
| `dbo.Security_Audit_Events` | BOOTSTRAP_ADMIN_CREATED audit (via IAuditWriter) |

**No `UserCompanyAssignment` is required.** `SECURITY_ADMIN_MANAGE` has `data_scope = GLOBAL`, and `Admin_Groups` with `scope_type = GLOBAL` require `company_id IS NULL`. The bootstrap admin does not need a company assignment to exercise GLOBAL security admin capability.

**Employment/company context**: The bootstrap user needs `employment_status = ACTIVE` and `account_status = ACTIVE` to satisfy authentication eligibility (`AuthenticationAccountPolicy.IsLinkedUserEligible`).

### 4. Existing Admin Group seed state

**Critical finding:** V0003 seeds 15 permission codes in `dbo.Permissions` but seeds **zero** Admin Groups and **zero** Admin_Group_Permissions rows. `dbo.Admin_Groups` is empty on a fresh install. Bootstrap cannot assume ADMIN_SECURITY exists. Bootstrap must create or find ADMIN_SECURITY and ensure it holds SECURITY_ADMIN_MANAGE. This must be idempotent on retry (see OD-F-B-09).

### 5. Existing password hasher and password policy

- `AspNetCorePasswordHashService` wraps `PasswordHasher<UserAuthAccount>` (ASP.NET Core Identity).
- `AuthenticationAccountPolicy` enforces: min 8 / max 64 characters; password may not contain the normalized provider_subject; no reuse within last 5 hashes.
- `UserAuthAccount.CreateInternal(userId, providerSubject, passwordHash, utcNow)` creates the account with `MustChangePassword = false`.
- To set `MustChangePassword = true`, the bootstrap must subsequently call `account.ReplacePassword(hash, mustChangePassword: true, temporaryPasswordExpiresAt: utcNow + 24h, ...)`.
- Alternatively, a new `CreateBootstrapInternal(...)` factory on `UserAuthAccount` can be added during implementation to set `MustChangePassword = true` at construction time. **This is an implementation decision for the authorized task.**

### 6. Existing transaction boundary patterns

- Established pattern (DEC-1B-014): `SERIALIZABLE` transaction with `UPDLOCK`/`HOLDLOCK` on the read before write for temporal assignments.
- `AppDbContext` + EF `Database.BeginTransactionAsync(IsolationLevel.Serializable)` is the existing pattern for concurrent-safe writes.
- `AppendOnlyInterceptor` is registered on `AppDbContext` and blocks EF-tracked UPDATE/DELETE on append-only tables.
- The bootstrap will need to execute a raw SQL `SELECT ... WITH (UPDLOCK, HOLDLOCK)` on the `Security_Bootstrap_State` singleton row before reading `is_bootstrapped`, to serialize concurrent bootstrap attempts at the database level.

### 7. Existing IAuditWriter contract

- `IAuditWriter.WriteAsync(SecurityAuditEventRecord, CancellationToken)` is available via DI.
- `SqlSecurityAuditWriter` uses a **separate `SqlConnection`** from the bootstrap's main EF transaction.
- The audit INSERT commits independently of the main transaction. This creates a narrow consistency gap: if the main transaction commits after the audit write, both are durable. If the main transaction rolls back after the audit write, the audit record is orphaned (bootstrap_admin_created event with no corresponding user). This is documented as an acceptable risk for bootstrap — a false-positive audit event is preferable to a missing audit on successful bootstrap.
- Fail-closed (OD-F-04): if `IAuditWriter.WriteAsync` throws, bootstrap must propagate the exception and the main transaction must be rolled back. The audit write precedes commit.

### 8. Whether bootstrap can be implemented without migration

**Yes.** All required tables exist in V0003: `Security_Bootstrap_State`, `Security_Audit_Events`, `Users`, `User_Auth_Accounts`, `Password_History`, `Admin_Groups`, `Admin_Group_Permissions`, `User_Admin_Group_Assignments`. No new tables, columns, constraints, or migrations are needed for F-B.

**One application-layer gap:** No `SecurityBootstrapState` C# entity class exists in `PTKD.Domain`. No `DbSet<SecurityBootstrapState>` exists on `AppDbContext`. Bootstrap must either:
- Add a minimal `SecurityBootstrapState` entity + EF mapping to AppDbContext (preferred, consistent with existing pattern), or
- Read/update `Security_Bootstrap_State` via raw ADO.NET SQL.

This is an implementation decision. The raw ADO.NET approach avoids modifying AppDbContext but is inconsistent with the EF-first pattern for mutable state tables. The EF entity approach is preferred but requires adding to AppDbContext. **Flagged as OD-F-B-13 below.**

### 9. PTKD.Bootstrap project status

`PTKD.Bootstrap` does **not** exist. The solution (`src/backend/PTKD-ERP.sln`) currently contains:
- PTKD.Api, PTKD.Application, PTKD.Domain, PTKD.Infrastructure, PTKD.Worker, PTKD.DbMigrator
- PTKD.UnitTests, PTKD.IntegrationTests, PTKD.ApiTests

Adding `PTKD.Bootstrap` requires:
1. A new `.csproj` file at `src/backend/PTKD.Bootstrap/PTKD.Bootstrap.csproj`.
2. A new entry in `src/backend/PTKD-ERP.sln`.
3. Project references to `PTKD.Application` and `PTKD.Infrastructure` (does NOT reference `PTKD.Api`).

### 10. Contradictions between accepted F plan and current source/schema

| Item | Plan proposed | Actual source state | Resolution |
|---|---|---|---|
| SecurityAuditEventRecord in PTKD.Domain | Plan proposed Domain | F-A implemented it in PTKD.Application | Accepted in F-A acceptance (OD-F-02 flexibility) |
| IAuditWriter in Application/Security/Audit/Interfaces/ | Plan proposed Interfaces/ subfolder | F-A placed it directly in Application/Security/Audit/ | Accepted in F-A acceptance |
| SecurityBootstrapState in AppDbContext | Not addressed in F-A | No entity or DbSet exists | Must be resolved in F-B implementation |
| Admin_Groups seeded with ADMIN_SECURITY | Not explicitly stated | V0003 seeds zero Admin Groups | Bootstrap must create ADMIN_SECURITY if absent |
| DEC-1B-010: "Creates the auth account and initial admin-group assignment in one transaction" | Single transaction | IAuditWriter uses a separate SqlConnection | Audit write consistency gap accepted and documented |
| phase-1b0 discovery: `Security_Bootstrap_State` columns include `bootstrapped_at DATETIME2(3) NOT NULL` | Implied | Actual schema: `bootstrapped_at DATETIME2(3) NULL` (only NOT NULL via check constraint when is_bootstrapped=1) | No contradiction; source is authoritative |

---

## Proposed Project Owner decisions for F-B

### OD-F-B-01 — Bootstrap delivery

Implement bootstrap as a dedicated internal console project: `src/backend/PTKD.Bootstrap/`.

- Invoked via `dotnet run --project src/backend/PTKD.Bootstrap` (or published executable).
- References `PTKD.Application` and `PTKD.Infrastructure`. Does NOT reference `PTKD.Api`.
- No public API endpoint.
- No API startup auto-run.
- Console output is restricted to sanitized status messages (no secrets, no SQL, no hashes).

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-02 — One-time marker

Use `dbo.Security_Bootstrap_State` (singleton row, id = 1) as the durable one-time bootstrap marker.

- Bootstrap reads the singleton row with `UPDLOCK`/`HOLDLOCK` inside a `SERIALIZABLE` transaction.
- If `is_bootstrapped = 1`: immediately reject with exit code 1, print sanitized message `"Bootstrap has already been completed."`. No state change.
- If `is_bootstrapped = 0`: proceed with bootstrap.
- After all other writes succeed: `UPDATE Security_Bootstrap_State SET is_bootstrapped=1, bootstrapped_at=@now, bootstrapped_by_user_id=@userId WHERE id=1`.
- Commit transaction. The consistency constraint enforces the final state durably.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-03 — Secret input

Bootstrap reads credentials from environment variables only:

- `PTKD_BOOTSTRAP_USERNAME` — the initial admin login name (becomes `provider_subject`).
- `PTKD_BOOTSTRAP_PASSWORD` — the plaintext initial admin password (consumed once, hashed immediately).

Rules:
- No default password. If either variable is absent or empty, bootstrap exits with code 1 and a sanitized error message.
- The password variable is read once, passed to the hasher, and then overwritten with empty/zeroed before any other operation.
- The password value is never written to any log sink, console output, database column, audit field, or exception message.
- No command-line arguments for secrets (visible in process lists).
- No interactive prompts.
- No plaintext secrets in `appsettings.json`, `appsettings.Development.json`, or any committed configuration file.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-04 — Password handling

Use the existing approved password hasher and password policy:

- Validate password against `AuthenticationAccountPolicy` (min 8 / max 64 / not containing normalized username) before hashing.
- Hash with `AspNetCorePasswordHashService.HashPassword(account, password)`.
- Create account with `must_change_password = 1`.
- Set `temporary_password_expires_at = utcNow + 24 hours`.
- Insert one `Password_History` row with the initial hash for future reuse enforcement.

**Implementation note:** `UserAuthAccount.CreateInternal()` sets `MustChangePassword = false`. Implementation must call `account.ReplacePassword(hash, mustChangePassword: true, temporaryPasswordExpiresAt: utcNow + 24h, updatedByUserId: null)` immediately after creation, or add a dedicated `CreateBootstrapInternal(...)` factory. Final decision deferred to implementation.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-05 — Account and provider model

Create the bootstrap admin through the existing user/auth account model:

1. Insert `Users` row: `employee_code = "BOOTSTRAP_ADMIN"`, `full_name` from `PTKD_BOOTSTRAP_USERNAME` (or a configurable display name), `employment_status = "ACTIVE"`, `account_status = "ACTIVE"`.
2. Insert `User_Auth_Accounts` row: `provider_type = "INTERNAL"`, `provider_subject = <normalized PTKD_BOOTSTRAP_USERNAME>`, `password_hash = <hashed>`, `must_change_password = 1`.
3. Insert `Password_History` row for reuse enforcement.
4. Do not bypass `UserAuthAccount.CreateInternal()` factory constraints.
5. `normalized_provider_subject = UPPER(TRIM(provider_subject))` per existing `InternalProviderSubjectNormalizer`.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-06 — Admin permission grant

Assign the bootstrap admin to the `ADMIN_SECURITY` admin group with `SECURITY_ADMIN_MANAGE` permission.

Because V0003 seeds zero Admin Groups, bootstrap must upsert the group and its permission:

1. Check whether an `Admin_Groups` row with `group_code = 'ADMIN_SECURITY'` and `scope_type = 'GLOBAL'` exists.
2. If absent: INSERT `Admin_Groups` (group_code=ADMIN_SECURITY, scope_type=GLOBAL, company_id=NULL, is_active=1, created_by_user_id=NULL).
3. Check whether `Admin_Group_Permissions` (admin_group_id, SECURITY_ADMIN_MANAGE) exists.
4. If absent: INSERT `Admin_Group_Permissions`.
5. INSERT `User_Admin_Group_Assignments` (user_id=<bootstrap user id>, admin_group_id=<ADMIN_SECURITY id>, company_id=NULL, assignment_status=ACTIVE, effective_from=utcNow, effective_to=NULL, created_by_user_id=NULL).

All within the single SERIALIZABLE transaction. No new permission codes created. No direct `User_Individual_Permissions` grant.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-07 — Transactionality

Bootstrap runs all database writes (User, UserAuthAccount, PasswordHistory, AdminGroup, AdminGroupPermission, UserAdminGroupAssignment, Security_Bootstrap_State update) inside a single SERIALIZABLE EF transaction.

- The `Security_Bootstrap_State` row is locked with `SELECT ... WITH (UPDLOCK, HOLDLOCK)` at the start of the transaction.
- If any step throws, the transaction is rolled back. No partial admin state is left.
- The `IAuditWriter` uses a separate `SqlConnection` and commits the audit event independently (see discovery finding 7). The audit write occurs just before committing the main transaction. If the audit write throws `SecurityAuditWriteException`, bootstrap fails closed and the main transaction is rolled back.
- If the main transaction commit fails after the audit write, an orphaned audit event may be present. This is a documented acceptable risk for bootstrap.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-08 — Audit

Bootstrap success emits a sanitized `BOOTSTRAP_ADMIN_CREATED` audit event via `IAuditWriter.WriteAsync`:

```
EventCode     = "BOOTSTRAP_ADMIN_CREATED"
EntityType    = "USER"
EntityId      = <user id as string>
Outcome       = "SUCCESS"
ActorUserId   = NULL  (no authenticated user; bootstrap is the actor)
CorrelationId = Guid.NewGuid()
AfterStateJson = sanitized JSON: { "employee_code": "...", "auth_account_status": "ACTIVE",
                                   "must_change_password": true }
                 — must NOT contain password, password_hash, token, secret, signing_key,
                   private_key, api_key, auth_key, or access_key
```

- `IAuditWriter.ThrowIfContainsSensitiveData()` enforces SEC-005.
- If the audit write fails with `SecurityAuditWriteException`, bootstrap is fail-closed: the main transaction is rolled back, bootstrap exits with code 1.
- Bootstrap failure must not log or print secrets in any error output.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-09 — Idempotency and concurrency

- The `Security_Bootstrap_State` singleton is read with `UPDLOCK`/`HOLDLOCK` inside a `SERIALIZABLE` transaction. This serializes concurrent bootstrap invocations at the database level: only one can hold the lock and see `is_bootstrapped = 0` at a time.
- The first invocation to commit transitions `is_bootstrapped = 0 → 1`. All subsequent invocations that acquire the lock after the commit see `is_bootstrapped = 1` and exit with code 1.
- The unique constraint on `(provider_type, normalized_provider_subject)` in `User_Auth_Accounts` provides a defense-in-depth guard against duplicate account creation if the locking logic is somehow bypassed.
- Bootstrap is not expected to be idempotent for repeated full runs (it is a one-time operation); idempotency of the Admin Group upsert (OD-F-B-06) is a separate concern for partial failure recovery.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-10 — Environment and production safety

- Bootstrap must not run automatically during API startup.
- Bootstrap must require explicit operator invocation (`dotnet run --project src/backend/PTKD.Bootstrap`).
- Bootstrap must fail fast if the database is already bootstrapped, printing a sanitized message.
- No production deployment, scheduling (cron, worker, timer), or automatic execution is authorized by this plan.
- The `PTKD.Bootstrap` project must not be referenced by `PTKD.Api` or `PTKD.Worker`.
- Console output must be restricted to: `"Bootstrap starting."`, `"Bootstrap complete."`, `"Bootstrap has already been completed."`, `"Bootstrap failed. See structured logs."`. No connection strings, SQL, hashes, user ids, or credential material in console output.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-11 — Test database

Continue using `PTKD_TEST_PHASE1A2` for F-B integration tests. No changes to `TestDatabaseSafety.cs`. All bootstrap integration tests run against the existing approved database after `ResetToV0003()`.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-12 — Explicit exclusions

The following are explicitly NOT in F-B scope:

- No public bootstrap endpoint.
- No audit read endpoint.
- No `SECURITY_AUDIT_VIEW` endpoint enforcement.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- No migration (V0003 is sufficient).
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No line-ending normalization.
- No additional user provisioning beyond the bootstrap admin.
- No authorization policy version bump triggered by bootstrap.
- No production deployment.
- No tag or push authorized by this plan.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

### OD-F-B-13 — SecurityBootstrapState entity strategy

V0003 has `Security_Bootstrap_State` but no C# entity or EF mapping exists.

**Proposed:** Add a minimal `SecurityBootstrapState` entity class to `PTKD.Domain` and add `DbSet<SecurityBootstrapState>` to `AppDbContext`. This is consistent with the existing EF-first pattern for mutable singleton state tables (`AuthorizationPolicyState` is an existing example with the same singleton pattern).

**Alternative:** Access via raw ADO.NET (`SqlCommand`) in the bootstrap command only, without touching `AppDbContext`. Avoids modifying the shared context but is inconsistent with the existing pattern.

**Decision owner:** Project Owner.

**Status: PENDING PROJECT OWNER ACCEPTANCE**

---

## Proposed implementation shape

### Project

| Item | Value |
|---|---|
| Project name | `PTKD.Bootstrap` |
| Project path | `src/backend/PTKD.Bootstrap/PTKD.Bootstrap.csproj` |
| Project type | Console application (`net10.0`) |
| References | `PTKD.Application`, `PTKD.Infrastructure` |
| Does NOT reference | `PTKD.Api` |
| Solution entry | Add to `src/backend/PTKD-ERP.sln` |

### Configuration

- `appsettings.json` — connection string (`DefaultConnection`) only. No secrets.
- Credentials via environment variables: `PTKD_BOOTSTRAP_USERNAME`, `PTKD_BOOTSTRAP_PASSWORD`.
- No `appsettings.Development.json` committed with real credentials.

### Service and command class structure

| Class/Interface | Layer | Purpose |
|---|---|---|
| `SecurityBootstrapState` | `PTKD.Domain/Security/Bootstrap/` | EF entity for Security_Bootstrap_State singleton |
| `IBootstrapService` | `PTKD.Application/Security/Bootstrap/` | Application interface: `ExecuteAsync(BootstrapCommand, CancellationToken)` |
| `BootstrapCommand` | `PTKD.Application/Security/Bootstrap/` | Input record: Username, PasswordHash (never plaintext in app layer) |
| `BootstrapService` | `PTKD.Application/Security/Bootstrap/` | Orchestrates transaction: lock → check → create user/account/group/assignment → audit → mark complete |
| `BootstrapEntryPoint` | `PTKD.Bootstrap/` | Console `Program.cs`: reads env vars, validates, hashes password, calls `IBootstrapService`, zeroes password, exits |

### Transaction boundary

```
SERIALIZABLE transaction (AppDbContext):
  1. SELECT id, is_bootstrapped FROM Security_Bootstrap_State WITH (UPDLOCK, HOLDLOCK) WHERE id = 1
  2. If is_bootstrapped = 1 → throw BootstrapAlreadyCompletedException
  3. INSERT Users → obtain user.Id
  4. INSERT User_Auth_Accounts (must_change_password=1)
  5. INSERT Password_History
  6. INSERT or find Admin_Groups (ADMIN_SECURITY, GLOBAL)
  7. INSERT or find Admin_Group_Permissions (ADMIN_SECURITY, SECURITY_ADMIN_MANAGE)
  8. INSERT User_Admin_Group_Assignments (ACTIVE, effective_from=now, no effective_to)
  9. WriteAsync(BOOTSTRAP_ADMIN_CREATED audit) → separate SqlConnection, commits immediately
     If throws SecurityAuditWriteException → abort transaction, exit code 1
 10. UPDATE Security_Bootstrap_State SET is_bootstrapped=1, bootstrapped_at=now, bootstrapped_by_user_id=user.Id
 11. Commit transaction
```

### Success and failure exit codes

| Condition | Exit code | Console output |
|---|---|---|
| Bootstrap complete | 0 | "Bootstrap complete." |
| Already bootstrapped | 1 | "Bootstrap has already been completed." |
| Missing credentials | 1 | "Required environment variables are not set." |
| Password policy violation | 1 | "Password does not meet policy requirements." |
| Audit write failure | 1 | "Bootstrap failed. See structured logs." |
| Database error | 1 | "Bootstrap failed. See structured logs." |

### Sanitized console output

All structured errors go to Serilog configured for the bootstrap project. Console output is limited to the above five messages only.

---

## Proposed tests

### Unit tests (PTKD.UnitTests)

- Password read: `BootstrapEntryPoint` zeroes the environment variable string after hashing — verify via mock.
- Repeated bootstrap detection: `BootstrapService` throws `BootstrapAlreadyCompletedException` when mock returns `is_bootstrapped = 1`.
- Password policy rejection: `BootstrapService` rejects too-short or provider_subject-containing password before any DB write.
- Failure message sanitization: `BootstrapAlreadyCompletedException` and `SecurityAuditWriteException` messages do not contain credential material.

### Integration tests (PTKD.IntegrationTests)

| Test | Description |
|---|---|
| `Bootstrap_Succeeds_OnUnbootstrappedDatabase` | Full bootstrap run on `ResetToV0003()` database returns exit code 0 |
| `Bootstrap_CreatesUser_WithCorrectFields` | After bootstrap, Users table has expected employee_code and statuses |
| `Bootstrap_CreatesAuthAccount_WithHashedPassword_NotPlaintext` | `password_hash` column is a bcrypt/PBKDF2 hash, not the raw input |
| `Bootstrap_CreatesAuthAccount_WithMustChangePassword` | `must_change_password = 1` and `temporary_password_expires_at` is set |
| `Bootstrap_CreatesAdminGroupAssignment_ForAdminSecurity` | `User_Admin_Group_Assignments` row exists with ACTIVE status for the bootstrap user |
| `Bootstrap_WritesBootstrapMarker` | `Security_Bootstrap_State.is_bootstrapped = 1` after completion |
| `Bootstrap_Writes_BOOTSTRAP_ADMIN_CREATED_AuditEvent` | `Security_Audit_Events` has one row with `event_code = BOOTSTRAP_ADMIN_CREATED` |
| `Bootstrap_SecondAttempt_Fails_WithoutModifyingState` | Second call to `IBootstrapService.ExecuteAsync` throws and leaves no new rows |
| `Bootstrap_Concurrent_Attempts_AllowOnlyOneSuccess` | Two parallel tasks; exactly one succeeds; database state is consistent |
| `Bootstrap_AuditWriteFailure_CausesBootstrapToFailClosed` | Mock audit writer throws; main transaction rolls back; no User/Auth rows remain |

### Regression tests

- Existing `PTKD.UnitTests`: 114 tests remain green.
- Existing `PTKD.IntegrationTests`: 157 tests remain green (including all `DatabaseSafety` tests).
- Existing `PTKD.ApiTests`: 153 tests remain green.
- No existing test should be modified to accommodate bootstrap.

---

## Risks and blockers

| Risk | Severity | Mitigation |
|---|---|---|
| `must_change_password` initial state requires two-step creation (`CreateInternal` + `ReplacePassword`) | Low | Add `CreateBootstrapInternal` factory or use `ReplacePassword` immediately after creation; implementation choice |
| No ADMIN_SECURITY admin group seeded in V0003 | Medium | Bootstrap creates the group if absent; requires upsert logic with correct FK constraints |
| Audit write (separate `SqlConnection`) commits before main transaction commits | Medium | Document as acceptable risk; audit write placed last before commit; orphaned audit on main rollback is preferable to missing audit on success |
| Concurrent bootstrap race condition | Medium | SERIALIZABLE + UPDLOCK/HOLDLOCK on singleton row; defense-in-depth from unique constraint on auth account |
| Password/secret in Serilog structured log output | High | Validate that env var value never enters any log enricher or error exception message; unit test for sanitization |
| `SecurityBootstrapState` entity not in AppDbContext | Medium | Must be resolved before implementation; OD-F-B-13 decision required |
| Console project requires `.sln` update | Low | Standard `dotnet sln add` command; no architectural risk |
| Bootstrap partial failure leaves orphaned Admin Group rows | Low | Admin Group upsert is idempotent; re-run on fresh `ResetToV0003()` is always clean in tests |
| `bootstrapped_by_user_id` FK requires user row before state update | Medium | Enforced by transaction ordering (user INSERT before state UPDATE); constraint will reject wrong order |

---

## Explicit exclusions

- No F-B code implemented in this task.
- No PTKD.Bootstrap project created.
- No public bootstrap endpoint.
- No audit read endpoint.
- No `SECURITY_AUDIT_VIEW` enforcement.
- No `PermissionCodes.cs` change.
- No `permission-catalog.md` change.
- No new permission code.
- No migration.
- No production seed/bootstrap.
- No JWT permission changes.
- No frontend.
- No business module implementation.
- No SystemController/AuthController/Organization/Security controller behavior change.
- No line-ending normalization.
- No production deployment.
- No tag or push.

---

## Accepted plan — implementation blocked pending F-B0 corrective slice

All OD-F-B decisions (OD-F-B-01 through OD-F-B-13) remain as accepted.
No OD-F-B decision is revised by this hard stop except as noted in the
blocked status section above.

F-B implementation may not resume until:
1. Phase 1B.1-F-B0 corrective migration plan is created and accepted.
2. V0004 migration inserting SECURITY_ADMIN_MANAGE is implemented and accepted.
3. The F-B0 acceptance is committed.

---

## Required Project Owner decisions before implementation

| Decision | Topic | Status |
|---|---|---|
| OD-F-B-01 | Bootstrap delivery as PTKD.Bootstrap console project | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-02 | Security_Bootstrap_State as durable one-time marker with SERIALIZABLE lock | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-03 | Secret input via PTKD_BOOTSTRAP_USERNAME / PTKD_BOOTSTRAP_PASSWORD environment variables only | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-04 | Password handling via existing hasher and policy; must_change_password=1; 24h temporary expiry | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-05 | Account created through existing User + UserAuthAccount model; provider_type=INTERNAL | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-06 | Admin permission via ADMIN_SECURITY group (create if absent) + SECURITY_ADMIN_MANAGE; no UserCompanyAssignment | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-07 | Single SERIALIZABLE transaction for all DB writes; IAuditWriter commits separately; audit write before commit | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-08 | BOOTSTRAP_ADMIN_CREATED audit event; sanitized after_state_json; fail-closed on audit write failure | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-09 | Concurrency safety via UPDLOCK/HOLDLOCK on singleton; defense-in-depth via unique constraint | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-10 | Bootstrap requires explicit operator invocation; no API startup auto-run; sanitized console output only | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-11 | Continue using PTKD_TEST_PHASE1A2 for tests | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-12 | Explicit scope exclusions as listed | PENDING PROJECT OWNER ACCEPTANCE |
| OD-F-B-13 | SecurityBootstrapState entity strategy: EF entity on AppDbContext (preferred) vs raw ADO.NET | PENDING PROJECT OWNER ACCEPTANCE |

---

## Recommended next step

Project Owner reviews OD-F-B-01 through OD-F-B-13. Accepts, rejects, or modifies each decision.

No F-B implementation may begin until this plan acceptance is committed as a separate Project Owner acceptance document.
