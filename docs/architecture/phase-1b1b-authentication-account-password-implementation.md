# Phase 1B.1-B Authentication Account and Password Lifecycle Implementation

Document status:

ACCEPTED BY PROJECT OWNER

## 1. Baseline commits

- Implementation authorization baseline: `3f50495a06cbb3da2a6e0a41b92d2574b9e8fa7c`.
- Baseline parent: `224116b1f0e45274fb644b78c38b119876de3c83`.
- Accepted Phase 1B.1-A corrective baseline: `efcf950b9c9679a1d6a44198ae3566fe93205a59`.
- Accepted Phase 1B.1-A original parent: `9d313a343fe2b2ccf29379b3a920bab9de4b5a0d`.
- The implementation commit is the single commit containing this evidence document; its Git-assigned hash is reported in the post-commit verification.

## 2. Authorization scope

Direct written Project Owner authorization dated 2026-07-16 authorized implementation, testing, documentation, and one implementation commit for Phase 1B.1-B only.

The implementation is limited to:

- authentication-account domain and EF mapping;
- local/internal password hashing and lifecycle;
- password history;
- temporary-password and must-change state;
- failed-attempt lockout;
- linked-user eligibility;
- persistent security-stamp/session-invalidation state;
- application service results with no HTTP endpoint;
- unit and protected SQL Server integration tests.

Phase 1B.1-C through I, Production migration, JWT, refresh tokens, APIs, controllers, cookies, CSRF, permission evaluation, frontend, bootstrap, audit writing, AD/LDAP, Production secret providers, tags, and pushes remain unauthorized.

## 3. Exact changed files

### Domain

- `src/backend/PTKD.Domain/Entities/UserAuthAccount.cs`
- `src/backend/PTKD.Domain/Entities/PasswordHistory.cs`
- `src/backend/PTKD.Domain/Security/Authentication/AuthenticationAccountPolicy.cs`

### Application

- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IAuthenticationDbContext.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IAuthenticationDbContextFactory.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IPasswordHashService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IProviderSubjectNormalizer.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/ISessionInvalidationService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IUtcClock.cs`
- `src/backend/PTKD.Application/Security/Authentication/Models/AuthenticationAccountCommands.cs`
- `src/backend/PTKD.Application/Security/Authentication/Models/AuthenticationAttemptResult.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/IAuthenticationAccountService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/AuthenticationAccountService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/SecurityStampSessionInvalidationService.cs`

### Infrastructure

- `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs`
- `src/backend/PTKD.Infrastructure/Persistence/AuthenticationDbContextFactory.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/UserAuthAccountConfiguration.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/PasswordHistoryConfiguration.cs`
- `src/backend/PTKD.Infrastructure/Security/Authentication/AspNetCorePasswordHashService.cs`
- `src/backend/PTKD.Infrastructure/Security/Authentication/InternalProviderSubjectNormalizer.cs`
- `src/backend/PTKD.Infrastructure/Time/SystemUtcClock.cs`
- `src/backend/PTKD.Infrastructure/PTKD.Infrastructure.csproj`

### Tests

- `tests/backend/PTKD.UnitTests/Security/Authentication/UserAuthAccountTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/AuthenticationAccountPolicyTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/AuthenticationAccountServiceTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/SecurityStampSessionInvalidationServiceTests.cs`
- `tests/backend/PTKD.IntegrationTests/AuthenticationAccountPersistenceTests.cs`
- `tests/backend/PTKD.IntegrationTests/AuthenticationLifecycleIntegrationTests.cs`

### Documentation

- `docs/architecture/phase-1b1b-authentication-account-password-implementation-plan.md`
- `docs/architecture/phase-1b1b-authentication-account-password-implementation.md`

No other tracked file is changed.

## 4. Domain objects

`UserAuthAccount` maps the accepted account aggregate without duplicating fields into `Users`. It owns:

- provider identity and local/external distinction;
- nullable external `password_hash`;
- ACTIVE, LOCKED, and DISABLED state;
- failed-attempt count and UTC lockout end;
- must-change and UTC temporary-password expiry;
- security stamp and UTC invalidation cutoff;
- creation/update actor metadata;
- rowversion;
- deterministic domain transitions receiving UTC time as input.

`PasswordHistory` is insert-only in application behavior and remains protected by the accepted V0003 append-only trigger. It exposes no update or delete domain operation.

`AuthenticationAccountPolicy` supplies validated defaults and pure rules for length 8–64, history depth 5, 24-hour temporary lifetime, five failures, 15-minute lockout, linked-user eligibility, and UTC enforcement. It adds no uppercase, lowercase, digit, or symbol rule.

## 5. Application services and interfaces

`AuthenticationAccountService` implements only the authorized library/service use cases:

- provider-type/subject lookup;
- local credential verification;
- dummy verification for unknown, external, or missing-hash paths;
- failure recording and lockout evaluation;
- successful-login reset and framework rehash;
- user password change;
- LOCAL/INTERNAL administrator reset;
- administrator unlock;
- auth-account suspension/disable;
- eligibility evaluation;
- security-stamp rotation.

No service is registered as an API route. No controller, DTO transport contract, authentication middleware, or authorization policy was created.

`IAuthenticationDbContext` exposes the accepted entities plus explicitly scoped locked reads, latest-history reads, transaction, execution strategy, and save behavior. It is not a generic repository.

## 6. Infrastructure and EF mappings

`AppDbContext` implements the authentication context and maps `User_Auth_Accounts`, `Password_History`, and the required `Users` relationship. Known-account operations use parameterized EF Core `FromSqlInterpolated` reads with `UPDLOCK, HOLDLOCK`; provider values are not concatenated into SQL.

Mappings match V0003 for:

- exact table and column names;
- `varchar(30)`, `varchar(200)`, and `varchar(500)` limits;
- `datetime2(3)` lifecycle timestamps;
- unique `(provider_type, provider_subject)`;
- deterministic history index order;
- named no-cascade relationships;
- rowversion concurrency.

No EF migration was generated or applied. Application startup still calls neither `Database.Migrate`, `EnsureCreated`, nor `EnsureDeleted`.

## 7. FrameworkReference impact

Compilation before the reference failed only because Infrastructure could not resolve `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>`:

- `CS0234`: `Microsoft.AspNetCore` namespace unavailable;
- two `CS0246` errors for `PasswordHasher<>`.

The authorized change added only:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

to `PTKD.Infrastructure.csproj`. The next build succeeded with zero warnings and zero errors.

## 8. NuGet package confirmation

No `PackageReference` was added, removed, or changed. No package was installed. MediatR and Dapper were not added.

## 9. Password policy implementation

- Minimum: 8 .NET characters.
- Maximum: 64 .NET characters.
- Candidate must not contain the canonical INTERNAL provider subject, using ordinal case-insensitive comparison.
- No character-composition rule exists.
- INTERNAL provider type and subject are trimmed and uppercased invariantly.
- External provider subjects remain exact opaque values; case-sensitive database uniqueness remains deferred.
- External accounts reject local hash creation, verification, change, and reset paths. Even a corrupted external row containing a non-null hash is forced through dummy verification.

`AspNetCorePasswordHashService` wraps the framework `PasswordHasher<UserAuthAccount>` defaults. No custom hash algorithm, reversible encryption, iteration count, or plaintext persistence exists.

## 10. Password-history semantics

The current hash is checked separately. The candidate is then checked against exactly the five latest `Password_History` rows ordered by `created_at DESC, id DESC`.

For a real password replacement:

1. validate policy;
2. reject current-password reuse;
3. read and verify the latest five historical hashes;
4. append the outgoing current hash;
5. write the new framework hash;
6. update account/security state;
7. save and commit atomically.

The sixth-older history password is outside the prohibited window. History rows are retained indefinitely and are never trimmed, updated, or deleted. A temporary password is appended as the outgoing current hash when it is replaced.

`SuccessRehashNeeded` updates the current hash only. It does not append history, rotate the security stamp, or alter temporary-password state.

Task 2B added explicit coverage proving that current password reuse is rejected, each of the five latest history rows is individually rejected, the sixth-older history row is allowed, and a failed self-service password change appends no history row.

## 11. Temporary-password lifecycle

Every LOCAL/INTERNAL administrator reset:

- hashes the supplied protected temporary password;
- appends the outgoing hash;
- sets `must_change_password = true`;
- sets `temporary_password_expires_at = utcNow + 24 hours`;
- clears lockout and failures;
- transitions the account to ACTIVE;
- rotates the security stamp and advances the invalidation cutoff;
- commits all changes atomically.

The credential is valid strictly before expiry and invalid when `utcNow >= temporary_password_expires_at`. Successful mandatory password change clears must-change and expiry state.

No temporary-password generator or delivery mechanism is implemented. No password is returned from an API because Slice B creates no API.

## 12. Lockout state machine

- Attempts 1–4 increment atomically and remain ACTIVE.
- Attempt 5 sets count 5, status LOCKED, and `lockout_end = utcNow + 15 minutes`.
- Active lockout performs password work for non-enumeration but never authorizes and does not extend or increment the window.
- At the first attempt at/after expiry, the service atomically clears the end, resets count to zero, and processes that attempt as attempt one if it fails.
- Successful eligible local authentication resets count and lockout.
- Administrator unlock resets count/end, transitions the locked account to ACTIVE, and preserves password, must-change state, expiry, and security stamp.
- Administrator reset applies the temporary-password transition above.
- Ineligible or disabled accounts never accumulate failures.

All lifecycle times come from `IUtcClock`. Production uses `SystemUtcClock`, which returns `DateTime.UtcNow`; no Slice B code calls `DateTime.Now`.

## 13. Eligibility behavior

Authentication succeeds only when:

- the auth account is ACTIVE and not in active/manual lockout;
- `Users.account_status` equals ACTIVE, case-insensitively;
- `Users.employment_status` equals ACTIVE or PROBATION, case-insensitively;
- a temporary credential is still before expiry.

SUSPENDED, TERMINATED, RETIRED, RESIGNED, INACTIVE, unknown, null-equivalent, and disabled states fail closed. These states do not increment failed attempts and receive the same safe result as unknown accounts and wrong passwords.

## 14. Security-stamp behavior

`SecurityStampSessionInvalidationService` is a real tracked-entity mutation, not a no-op. It changes the non-empty GUID stamp and advances `sessions_invalidated_at` monotonically in the same transaction as:

- successful user password change;
- LOCAL/INTERNAL administrator reset;
- auth-account suspension/disable.

It does not change the stamp for failed authentication, timed-lockout expiry, administrator unlock, or framework-only rehash.

## 15. Explicit Slice C dependency

Slice B persists only the security stamp and invalidation cutoff. There is no token/session store and no fake refresh-token repository. Slice C must:

- include and validate the current security stamp;
- reject refresh/session state issued at or before the cutoff;
- implement actual token/session revocation and transport behavior;
- preserve the one generic authentication-denial contract.

## 16. Transaction boundaries

Each mutable service operation uses:

- one fresh DbContext per execution-strategy attempt;
- one explicit Serializable SQL Server transaction;
- an account-key locked read with `UPDLOCK, HOLDLOCK`;
- one commit after every required account/history/stamp mutation.

Covered operations include failed-attempt update, expired-lockout transition, successful reset, password change, administrator reset, unlock, and disable.

Only the existing SQL Server deadlock error 1205 execution strategy retries, with at most three total attempts. Validation, authentication failure, rowversion conflict, uniqueness conflict, and non-deadlock database errors are not explicitly retried by Slice B.

## 17. Concurrency controls

- `row_version` is mapped as a required concurrency token.
- Administrative/password-change commands require the exact current eight-byte target version.
- A stale version returns `AUTH_ACCOUNT_CONCURRENCY_CONFLICT` without mutation.
- Known-account authentication attempts serialize the account row so concurrent failures cannot lose increments.
- Competing password changes using the same target version yield one success and one deterministic conflict.
- Transaction failure tests prove account hash, history, temporary state, stamp, and rowversion remain unchanged.

## 18. Non-enumeration behavior

The outward application result exposes no internal cause, provider subject, password, hash, or SQL detail. Unknown accounts, unknown providers, external-password paths, missing hashes, bad passwords, active/manual lockout, suspended users, disabled auth accounts, invalid employment status, and expired temporary credentials return `AUTH_INVALID_CREDENTIALS` with no account/user/stamp/version fields.

A real framework verification is performed for a valid INTERNAL hash. A process-local dummy framework hash is verified for unknown, external, and missing-hash paths.

## 19. Build evidence

SDK selected from the repository root:

```text
dotnet --version
10.0.302
```

Final command:

```text
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --warnaserror
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## 20. Unit test evidence

Command:

```text
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore
```

Result: 63 passed, 0 failed, 0 skipped, total 63.

## 21. SQL Server integration test evidence

Command:

```text
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore
```

Result: 128 passed, 0 failed, 0 skipped, total 128.

No existing integration-test file was modified or deleted. The executable total above is the actual SDK 10.0.302 discovery result.

## 22. API regression evidence

Command:

```text
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore
```

Result: 60 passed, 0 failed, 0 skipped, total 60.

No authentication API test was added because no authentication endpoint is authorized. No existing API contract or ProblemDetails behavior changed.

## 23. Targeted Slice B evidence

Commands and results:

```text
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Authentication"
Passed: 38, Failed: 0, Skipped: 0, Total: 38.

dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Authentication"
Passed: 25, Failed: 0, Skipped: 0, Total: 25.
```

## 24. Protected database evidence

Every Slice B integration DbContext is created through a test-only guarded factory that:

1. validates `InitialCatalog` before opening;
2. opens only the validated connection;
3. executes `SELECT DB_NAME()`;
4. requires exact `PTKD_TEST_PHASE1A2`.

Independent read-only verification:

```text
sqlcmd -S localhost -d PTKD_TEST_PHASE1A2 -E -C -b -Q "SET NOCOUNT ON; SELECT DB_NAME();"
PTKD_TEST_PHASE1A2
```

## 25. PTKD_DEV exclusion evidence

- The full IntegrationTests suite passed the existing `Tests_Reject_PTKD_DEV_BeforeAnyWrite` safety test.
- Test connection-string validation accepts only exact `PTKD_TEST_PHASE1A2`.
- Each new factory/context path performs the pre-open catalog guard and post-open `DB_NAME()` check.
- No command in this implementation used a `PTKD_DEV` connection string.
- `PTKD_DEV` was never connected to or written to.

## 26. Known limitations

- Slice B exposes library/application behavior only; no runtime controller or background command invokes it.
- Actual token/session rejection remains Slice C work; only persistent stamp/cutoff state exists.
- Linked Organization `Users` status changes are not modified by this manifest to rotate the auth stamp; later protected requests must re-read eligibility and the integration must be assigned to an authorized later slice.
- Security administration endpoints must not be exposed until Slice F provides immutable, semantically scrubbed audit writing.
- External-provider authentication and case-sensitive provider-subject database uniqueness remain deferred.
- PBKDF2 work occurs while holding a single-account lock; Production load testing remains required.
- Database role membership, privileged-principal boundaries, and Production execution plans remain subject to independent DBA review.

## 27. Migration confirmation

- V0003 was not modified.
- U0003 was not modified.
- No V0004 or U0004 was created.
- No EF migration was generated or applied.
- Tests reset and apply the already accepted V0003 only against `PTKD_TEST_PHASE1A2`.

## 28. Later-slice authorization

Phase 1B.1-C through Phase 1B.1-I remain **NOT AUTHORIZED**. This implementation does not begin or imply authorization for those slices.

## 29. Production restrictions

- Production migration is **NOT AUTHORIZED**.
- Production secret-provider integration is not implemented.
- No tag or push is authorized.
- Production use is blocked on Slice C session enforcement, Slice F audit integration, DBA review, load testing, and separate Project Owner authorization.

## 30. Project Owner acceptance

PROJECT OWNER ACCEPTANCE — PHASE 1B.1-B IMPLEMENTATION

Tôi, Đào Hải Bách, với vai trò Project Owner dự án PTKD ERP, xác nhận đã
xem xét kết quả triển khai, kiểm thử và evidence review của Phase 1B.1-B.

Phạm vi được chấp nhận:

Phase 1B.1-B — Authentication account and password lifecycle

Các commit được chấp nhận:

1. Implementation commit:
   fdad4e9099283eb1f36271ccb5fd966afaf6742d
   Implement Phase 1B.1-B authentication account and password lifecycle

2. Password-history coverage commit:
   a2e381139bba61ddaf8d9097be7df0e0010d878f
   Add Phase 1B.1-B password history coverage

Tôi chấp nhận các kết quả sau:

1. Authentication account domain/application/infrastructure foundation đã
   được triển khai đúng phạm vi Slice B.

2. Password hashing sử dụng ASP.NET Core PasswordHasher thông qua abstraction,
   không custom hashing, không plaintext password, không reversible password.

3. Password history semantics đúng kế hoạch đã duyệt:
   - kiểm tra current password hash;
   - kiểm tra 5 Password_History rows mới nhất theo created_at DESC, id DESC;
   - tổng cộng 6 giá trị password bị chặn reuse;
   - row thứ 6 cũ hơn được cho phép.

4. Password-history coverage đã được bổ sung sau evidence review:
   - current password bị reject;
   - từng password trong 5 history rows mới nhất bị reject;
   - password ở row thứ 6 cũ hơn được allow;
   - failed self-service password change không append history.

5. Lockout và failed-attempt accounting đã được xác minh:
   - 5 failed attempts dẫn tới lockout 15 phút;
   - concurrent failed attempts không bị lost update;
   - transaction dùng IsolationLevel.Serializable;
   - account-for-update query dùng UPDLOCK, HOLDLOCK;
   - DbUpdateConcurrencyException không retry;
   - retry chỉ áp dụng SQL Server deadlock 1205;
   - tối đa 3 attempts cho deadlock retry.

6. Security stamp behavior được chấp nhận:
   - Slice B chỉ thay đổi security_stamp và sessions_invalidated_at;
   - không triển khai token/session/refresh-token store;
   - không tạo fake/no-op token revocation;
   - Slice C phải hoàn thiện token/session rejection thực sự.

7. External-provider subject case sensitivity vẫn là deferred, non-blocking.
   Không tạo V0004/U0004 trong Phase 1B.1-B.

8. V0003/U0003 không đổi.

9. Không có API, JWT, refresh token, cookie/CSRF, frontend, MediatR, Dapper,
   AD/LDAP, bootstrap, password delivery hoặc application audit writer trong
   Slice B.

10. Production migration chưa được phép.

Tôi chấp nhận Phase 1B.1-B ở trạng thái:

ACCEPTED BY PROJECT OWNER

Các phần sau vẫn giữ nguyên trạng thái:

Phase 1B.1-C through I:
NOT AUTHORIZED

Production migration:
NOT AUTHORIZED

Người phê duyệt: Đào Hải Bách
Vai trò: Project Owner
Ngày phê duyệt: 2026-07-16
Phương thức xác nhận: Direct written authorization
