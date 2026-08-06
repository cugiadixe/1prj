# Phase 1B.1-B Authentication Account and Password Lifecycle Implementation Plan

Document status:

IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE

## 1. Planning authority and accepted baseline

This document was accepted by the Project Owner as the official Phase 1B.1-B plan. Separate direct written authorization subsequently authorized implementation, testing, documentation, and one implementation commit for Slice B only.

- Repository baseline HEAD: `224116b1f0e45274fb644b78c38b119876de3c83`.
- Accepted Phase 1B.1-A corrective baseline: `efcf950b9c9679a1d6a44198ae3566fe93205a59`.
- Accepted Phase 1B.1-A original parent: `9d313a343fe2b2ccf29379b3a920bab9de4b5a0d`.
- `database/migrations/V0003__create_security_schema.sql` and `database/rollbacks/U0003__drop_security_schema.sql` are accepted and must not be modified by Slice B.
- Phase 1B.1-B implementation is complete and verified; Project Owner implementation acceptance is pending.
- Phase 1B.1-C through I remain **NOT AUTHORIZED**.
- Production migration remains **NOT AUTHORIZED**.
- Planning did not connect to a database and did not run a migration, rollback, build, or test.
- Executable implementation evidence is recorded in `docs/architecture/phase-1b1b-authentication-account-password-implementation.md`.

## 2. Slice B scope

Slice B may propose implementation for only:

- `User_Auth_Accounts` domain behavior and EF Core mapping;
- `(provider_type, provider_subject)` lookup and INTERNAL subject canonicalization;
- nullable password hashes for external providers;
- ASP.NET Core `PasswordHasher<TUser>` behind an application abstraction;
- password length 8–64 and provider-subject exclusion;
- previous-five password-history enforcement;
- 24-hour temporary-password lifetime and `must_change_password` lifecycle;
- five failed attempts, 15-minute lockout, and successful-login reset;
- administrator password reset and unlock domain/application behavior;
- authentication eligibility based on the auth account and linked `Users` row;
- `security_stamp` and `sessions_invalidated_at` contract for Slice C;
- rowversion, transaction, and concurrency behavior;
- sanitized, non-enumerating application results;
- unit and SQL Server integration tests.

## 3. Explicit exclusions

The following remain outside Slice B:

- JWT creation or validation;
- refresh-token behavior, token rotation, or token-family mutation;
- login, refresh, logout, change-password, or security-administration API endpoints;
- cookies, CSRF, frontend, and protected-route behavior;
- permission evaluation, permission cache, company middleware, or `X-Company-Id` handling;
- bootstrap execution;
- the application security-audit writer;
- AD/LDAP, OAuth, OIDC, SSO, or another external-provider implementation;
- modification of V0003 or U0003;
- V0004/U0004 creation or execution;
- MediatR or Dapper;
- package installation.

No Slice B service is to be registered as a reachable API operation. Runtime composition for login belongs to Slice C; administration exposure also depends on the Slice F audit writer.

## 4. Sources and traceability

### 4.1 Governing decisions and rules

| Source | Slice B effect |
|---|---|
| DEC-1B-001 | Separate auth account, provider identity lookup, nullable external-provider hash. |
| DEC-1B-002 | ASP.NET Core PasswordHasher, length 8–64, subject exclusion, previous five, 24-hour temporary password, reset invalidates sessions. |
| DEC-1B-004 | Configuration-backed five-attempt threshold and 15-minute lockout. |
| DEC-1B-013 | Login eligibility requires `Users.account_status = ACTIVE` and `employment_status = ACTIVE or PROBATION`. |
| DEC-1B-020 | Locked-account transport handling must not enumerate accounts. Project Owner decision 5 requires one generic public authentication-failure outcome across nonexistent, bad-password, locked, suspended, and employment-ineligible cases; exact transport reconciliation is deferred to Slice C. |
| GOV-006 | Security behavior must eventually be consistent across service, API, and database boundaries. Slice B supplies the service/domain portion only. |
| GOV-007, GOV-008 | Sensitive administration requires immutable audit. Because the writer is excluded, Slice B administration behavior must not be exposed at runtime before Slice F. |
| AUTH-006 | Account eligibility is a hard rule and cannot be bypassed by an administrator permission. |
| AUTH-012 | User-status changes invalidate authorization state. Slice B defines the account/session contract; later protected-request evaluation remains outside Slice B. |
| SEC-005 | Passwords and hashes must never enter logs, audit JSON, error details, snapshots, or test output. |
| Technical decisions v1.0 | Modular monolith, vertical slices, EF Core for ordinary persistence, no MediatR, internal accounts initially, external authentication behind an abstraction. |

### 4.2 Acceptance-criteria relationship

The released acceptance catalog has no criterion dedicated to password hashing, history, temporary-password expiry, or lockout. Slice B is a prerequisite for `AUTH-01` (permissions after sign-in), but cannot satisfy `AUTH-01` without Slices C, D, and E. `SEC-01` becomes applicable only when endpoints exist. `SEC-03` and business rule `SEC-005` govern secret-free handling, but Slice B has no application audit writer.

Project Owner acceptance of this plan also accepts the Slice B completion/test matrix below as the slice-specific evidence contract; it does not alter the released business acceptance catalog or authorize implementation.

## 5. Repository and package facts

- Backend projects target `net10.0`.
- `PTKD.Application` references FluentValidation `12.1.1` and EF Core `10.0.9`.
- `PTKD.Infrastructure` references EF Core SQL Server `10.0.9`.
- `PTKD.Api` uses `Microsoft.NET.Sdk.Web`, so it already receives the `Microsoft.AspNetCore.App` shared framework.
- `PTKD.Application` and `PTKD.Infrastructure` currently receive only `Microsoft.NETCore.App`; neither has a direct `Microsoft.Extensions.Identity.Core` package nor a `Microsoft.AspNetCore.App` framework reference.
- `PasswordHasher<TUser>` is in `Microsoft.AspNetCore.Identity` and is supplied by `Microsoft.Extensions.Identity.Core`. Its result model includes `Failed`, `Success`, and `SuccessRehashNeeded`.
- `PTKD.UnitTests` currently uses Moq `4.20.72`; `PTKD.ApiTests` contains NSubstitute `6.0.0`. The approved testing standard remains xUnit/NSubstitute, but Slice B does not require a mocking-package change.
- Existing SQL Server tests use the guarded `PTKD_TEST_PHASE1A2` fixture, exact `InitialCatalog` validation, and post-open `SELECT DB_NAME()` verification.
- Existing application services use fresh DbContexts, explicit transactions, EF execution strategies, rowversion, and stable exceptions.

Preferred future dependency approach, if implementation is authorized:

- add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `PTKD.Infrastructure.csproj`;
- keep the ASP.NET Core hasher adapter in Infrastructure;
- add no NuGet package.

This is a proposed project-file/configuration change, not an installed package. Placing the hasher adapter in `PTKD.Api` would avoid that framework reference but would violate the intended infrastructure boundary. Adding `Microsoft.Extensions.Identity.Core` as a NuGet package is not proposed and would require separate package authorization.

References: [global.json SDK selection](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json), [PasswordHasher<TUser>](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0), and [PasswordVerificationResult](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordverificationresult?view=aspnetcore-10.0).

## 6. SDK 10.0.301 versus 10.0.302

Observed during planning:

```text
dotnet --list-sdks
8.0.423
10.0.302

dotnet --version
10.0.302

global.json
version: 10.0.301
rollForward: latestFeature
```

`10.0.302` is valid for this repository because `latestFeature` selects the highest installed SDK with the requested major/minor whose feature band and patch are greater than or equal to `10.0.301`. Both versions are in the `10.0.3xx` feature band, and `10.0.302` is the installed higher patch. The local resolver confirms this by returning `10.0.302` from the repository root.

If a Slice B build were run with the currently installed SDK set and unchanged `global.json`, the exact selected SDK would be `10.0.302`. Because `latestFeature` can select a later installed `10.0.x` feature band, reproducible evidence must always record `dotnet --version`; this plan does not change `global.json`.

## 7. V0003 schema compatibility matrix

| Required behavior | V0003 support | Implementation mapping | Gap/blocker |
|---|---|---|---|
| Auth-account aggregate | Full columns, FK to `Users`, timestamps, actor fields, rowversion | `UserAuthAccount` plus explicit EF configuration | Implementable without schema change |
| Provider lookup | Unique `(provider_type, provider_subject)` and lengths 30/200 | Canonicalize provider type; canonical INTERNAL subject is stored directly in `provider_subject` and used for lookup | Implementable without schema change for INTERNAL |
| Stored normalized subject | Accepted V0003 intentionally has no `normalized_provider_subject` and the schema tests assert its absence | Do not add a shadow/database column; normalize INTERNAL input before storage/query | Implementable without schema change |
| Case-sensitive external subject | Column collation is inherited and not declared by V0003 | External providers must supply their own canonical opaque subject in a later slice | Deferred and non-blocking by Project Owner decision 10. Any V0004/U0004 remains **NOT AUTHORIZED** |
| Nullable external hash | `password_hash varchar(500) NULL` | LOCAL means the accepted `INTERNAL` provider type and requires a hash; external accounts require null and cannot receive a local password reset | Implementable without schema change; DB does not independently enforce provider/hash pairing |
| ASP.NET Core PasswordHasher | Hash column has sufficient length | Infrastructure adapter maps framework verification results | Implementable without schema change |
| Password length 8–64 | No database column stores plaintext | Domain policy validates plaintext before hashing | Implementable without schema change |
| Password excludes provider subject | No plaintext is stored | Compare canonical INTERNAL subject against password using invariant, case-insensitive policy before hashing | Implementable without schema change |
| Previous-five history | Append-only `Password_History`, FK, hash, timestamp, index `(account_id, created_at DESC, id DESC)` | Query latest five previous hashes; retain all rows; append outgoing current hash on actual password replacement | Implementable without schema change |
| Temporary password 24 hours | `must_change_password`, nullable `temporary_password_expires_at`, and consistency check | Set expiry to UTC now + 24 hours and reject at `now >= expiry` | Implementable without schema change |
| Must-change lifecycle | Boolean and expiry columns | Valid credential returns `PasswordChangeRequired`; successful self-change clears flag/expiry | Implementable without schema change; limited-session/API behavior depends on Slice C |
| Five failures/15-minute lockout | Counter, lockout end, ACTIVE/LOCKED/DISABLED check | Atomic UTC state transitions with configured values fixed to approved defaults unless a later configuration source is authorized | Implementable without schema change; lifecycle decisions are approved in section 8 |
| Manual lock versus timed lock | LOCKED plus nullable `lockout_end` | `LOCKED + NULL` means indefinite/manual; `LOCKED + future time` means timed | Implementable without schema change |
| Successful-login reset | Counter, lockout end, status, updated metadata | Reset counter and expired/timed lock state in successful verification transaction | Implementable without schema change |
| Admin reset/unlock | Mutable account columns, actor metadata, rowversion | LOCAL/INTERNAL reset creates a 24-hour temporary password and invalidates sessions; external reset is rejected; unlock clears UTC lockout state and count | Implementable without schema change; runtime exposure blocked on Slice F audit |
| Eligibility | FK to `Users`; V2 has both required status columns | Load linked user and fail closed unless account ACTIVE, user account ACTIVE, and employment ACTIVE/PROBATION; compare approved values case-insensitively because existing data is not DB-canonicalized | Implementable without schema change |
| Session invalidation | `security_stamp` and `sessions_invalidated_at` | Rotate stamp and advance UTC cutoff in the same transaction as password change, LOCAL/INTERNAL admin reset, and auth-account suspension/disable; no no-op implementation | Implementable without schema change; token/session completion depends on Slice C |
| Optimistic concurrency | `row_version rowversion` | `.IsRowVersion().IsConcurrencyToken()` and target-version checks for administration | Implementable without schema change |
| Atomic password/history changes | Both tables and FK exist | One explicit transaction covers lock/read, history insert, account update, stamp change, and commit | Implementable without schema change |
| Application audit | `Security_Audit_Events` exists, but writer is excluded | Slice B may define sanitized audit intent metadata only; no writer and no runtime admin exposure | Dependency on Slice F, not a schema gap |

Conclusion: no required Slice B behavior requires changing V0003. No V0004/U0004 is proposed for Slice B.

## 8. Approved Project Owner decisions

The Project Owner approved this plan and the following decisions by direct written authorization dated 2026-07-16. These decisions make the plan official but do not authorize source implementation.

| # | Approved decision | Required implementation effect |
|---:|---|---|
| 1 | On the first authentication attempt at or after timed-lockout expiry, atomically clear `lockout_end`, set `failed_attempt_count = 0`, and then evaluate the current attempt as the first new attempt. | The expiry transition and current attempt occur in one transaction using UTC. |
| 2 | Administrator unlock clears `lockout_end` and sets `failed_attempt_count = 0`. | Unlock also transitions the locked auth account to ACTIVE, as defined by the accepted lockout state machine. |
| 3 | Administrator reset for a LOCAL account creates a temporary password, sets `must_change_password = true`, expires it after 24 hours, clears lockout, resets failures, and changes `security_stamp` in the same transaction. | `LOCAL` is terminology for the plan's accepted `provider_type = 'INTERNAL'`; no new `LOCAL` database value is introduced. |
| 4 | An external-provider account with `password_hash = NULL` cannot receive a local password reset. | Return a sanitized administrative state conflict; password management remains with the external provider. |
| 5 | Ineligible, suspended, or invalid-employment accounts do not increment `failed_attempt_count`. Public authentication results must not disclose existence, bad password, lockout, suspension, or employment ineligibility. | All listed authentication denials collapse to one generic public application outcome. Restricted internal classification may be supplied only to the later scrubbed audit writer. |
| 6 | `SuccessRehashNeeded` updates `password_hash`, creates no `Password_History` row, and is not a user password change. | Rehash remains atomic and changes rowversion, but not password-history or must-change semantics. |
| 7 | Slice B changes `security_stamp` in the same transaction as password change, LOCAL/INTERNAL administrator reset, and auth-account suspension/disable. No no-op invalidation implementation is permitted. | `ISessionInvalidationService` must mutate the tracked account; Slice C must validate the stamp and complete token/session revocation. |
| 8 | `failed_attempt_count` and `lockout_end` use UTC. | Domain/application time comes from `IUtcClock`; persisted `datetime2(3)` values are UTC. |
| 9 | No password character-class requirement is added. | Only approved length, provider-subject, and previous-five rules apply. |
| 10 | Case-sensitive external-provider subject handling is deferred and non-blocking. | Slice B creates no V0004/U0004; any future collation/schema proposal requires a separate decision. |

The term “account suspension” in decision 7 maps within Slice B to the auth-account suspension/disable transition represented by `auth_account_status = 'DISABLED'`. Changes to the linked Organization `Users` status remain the cross-slice integration dependency described in section 14.

The decision gate is satisfied. Implementation remains blocked until the Project Owner gives separate written Phase 1B.1-B implementation authorization.

## 9. Domain, application, and infrastructure boundaries

### Domain

- `UserAuthAccount` owns account status, failure count, lockout time, must-change state, temporary expiry, security stamp, invalidation cutoff, actor/update metadata, and rowversion.
- `PasswordHistory` is append-only and exposes no update/delete behavior.
- `AuthenticationAccountPolicy` owns constants and pure rules: INTERNAL provider (called LOCAL in the acceptance), allowed statuses, 8–64 length, five-history depth, 24-hour temporary lifetime, five failures, 15-minute UTC lockout, eligibility, and transition guards. It adds no character-class rule.
- Domain methods receive UTC time and new stamps as inputs; they do not call the system clock or EF Core.
- Plaintext passwords and hashes are never returned from domain results or formatted into exception messages.

### Application

- `IAuthenticationDbContext`/factory expose only `UserAuthAccounts`, `PasswordHistories`, linked `Users`, save, transaction, and execution strategy operations required by this slice.
- `IPasswordHashService` hides ASP.NET Core Identity types and maps verification to an application enum: `Failed`, `Succeeded`, `SucceededRehashNeeded`.
- `IProviderSubjectNormalizer` supplies provider-specific canonicalization. Slice B implements INTERNAL only; external identifiers remain exact opaque inputs for later providers.
- `IUtcClock` makes lockout and expiry tests deterministic.
- `ISessionInvalidationService` applies the decision-7 mutation to the tracked account; it performs no separate database work and may not have a no-op implementation.
- `AuthenticationAccountService` owns credential verification, failure/success state, LOCAL/INTERNAL admin reset, external-reset rejection, unlock, auth-account suspension/disable, self-change, transaction orchestration, and sanitized outcomes.
- No controller DTO, claim, cookie, JWT, or refresh-token type is created.

### Infrastructure

- EF configurations map exact accepted names, lengths, defaults, FKs, and rowversion without generating migrations.
- `AspNetCorePasswordHashService` wraps `PasswordHasher<UserAuthAccount>` with framework defaults; no custom algorithm or iteration count is invented.
- A process-local dummy hash, generated once with the same hasher, is used for unknown/external/missing-hash credential checks.
- EF Core parameterization is mandatory. A provider-subject value is never concatenated into SQL.
- The existing `AppDbContext` implements the authentication context interface and applies the new mappings through its existing assembly scan.

## 10. Provider lookup and canonicalization

1. Normalize `provider_type` by trimming and uppercasing invariantly; reject empty or values over 30 characters.
2. For `INTERNAL`, trim and uppercase the subject invariantly, enforce the 200-character storage limit, and store that canonical value in `provider_subject`.
3. Use the same canonical INTERNAL value for lookup and the password-contains-subject rule.
4. For a future external provider, preserve the provider-defined canonical opaque subject; do not apply INTERNAL case folding.
5. Query by both columns. Never fall back to email, employee code, or a different provider.
6. A duplicate key maps to a sanitized account-conflict result in administrative provisioning; it is never exposed by credential verification.

In the Project Owner acceptance, “LOCAL account” means the existing INTERNAL provider defined by this plan. Slice B must not introduce `LOCAL` as an additional `provider_type` value.

## 11. Password policy and history semantics

- Length is checked before hashing: minimum 8, maximum 64 .NET characters. No unapproved character-class rule is added.
- Candidate comparison with the INTERNAL canonical subject is case-insensitive; a candidate containing the full non-empty subject is rejected.
- A change/reset candidate is compared with the current hash and the five most recent `Password_History` rows ordered by `created_at DESC, id DESC`.
- On an actual password replacement, the outgoing current hash is inserted into history before the account hash changes, inside the same transaction.
- Initial password creation has no outgoing hash and creates no history row.
- History rows are never trimmed, updated, or deleted; only the latest five are consulted.
- Reusing the current password is rejected independently of the previous-five query.
- `SuccessRehashNeeded` updates the current hash in place, creates no history row, and is not a password change.
- A successful self-change clears `must_change_password` and `temporary_password_expires_at`, rotates the security stamp, advances the invalidation cutoff, and resets successful-login failure state.
- A temporary password is expired at `utcNow >= temporary_password_expires_at`.
- Every LOCAL/INTERNAL administrator reset creates a 24-hour temporary password, clears lockout and failures, sets must-change, and rotates the stamp atomically.
- An external-provider account whose hash is null rejects local administrator password reset without adding history or mutating account state.
- No character-class, uppercase, lowercase, number, or symbol requirement is added.

## 12. Lockout state machine

The following state machine records the approved Project Owner decisions.

| Current state/event | Guard | Atomic next state |
|---|---|---|
| ACTIVE + failed credential 1–4 | Eligible INTERNAL account | Increment count; remain ACTIVE; no lockout end |
| ACTIVE + fifth failed credential | Eligible INTERNAL account | Count 5; status LOCKED; `lockout_end = utcNow + 15 minutes` |
| LOCKED timed + attempt before end | Password work still performed for non-enumeration | Remain LOCKED; do not extend the window |
| LOCKED timed + first attempt at/after end | Approved decision 1 | Atomically set ACTIVE, count 0, clear end, then evaluate this attempt as attempt 1 |
| LOCKED manual + any credential attempt | `lockout_end IS NULL` | Remain LOCKED until admin unlock/reset |
| ACTIVE + successful credential | Eligible and temporary password not expired | Count 0; clear end; remain ACTIVE; optionally rehash |
| Any account + admin unlock | Valid rowversion; approved decision 2 | ACTIVE, count 0, clear end |
| LOCAL/INTERNAL account + admin reset | Valid rowversion; approved decisions 3 and 7 | ACTIVE, count 0, clear end, temporary hash, must-change, 24-hour expiry, session invalidation |
| External account + local admin reset | `password_hash IS NULL`; approved decision 4 | Reject with no state/history mutation |
| DISABLED, suspended, or linked user ineligible | Hard eligibility rule; approved decision 5 | Generic authentication denial; do not increment failure state |

Configuration objects expose the approved values for testability, but the authorized defaults remain exactly 5 attempts, 15 UTC minutes, 24 UTC hours, history depth 5, and length 8–64. Configuration may not silently broaden these values or add character-class rules.

## 13. Authentication eligibility and result flow

Credential verification is an application operation, not an API endpoint:

1. Canonicalize provider and subject.
2. Begin the account transaction and perform a parameterized account lookup with update/hold locking for a known row.
3. Use the real INTERNAL hash or the process-local dummy hash and perform one password verification.
4. For unknown provider/subject, external/missing hash, or bad password, return the generic public invalid-credentials outcome. Mutate a failure counter only for a found eligible INTERNAL account.
5. After a valid password, apply hard eligibility checks: auth account not DISABLED, linked `Users.account_status` ACTIVE, and `employment_status` ACTIVE or PROBATION.
6. Apply manual/timed lockout and temporary-password expiry. Locked, suspended, disabled, and employment-ineligible results collapse to the same generic public outcome and never expose the internal cause.
7. Reset success state and process `SuccessRehashNeeded` without adding history.
8. Return a sanitized success result containing only identifiers, must-change state, rowversion/stamp data required by Slice C, and no hash.

Status values are compared fail-closed and case-insensitively because V2 has no status check constraints and existing tests write both `ACTIVE` and `Active`. Unknown, null, or whitespace variants are ineligible.

## 14. Session-invalidation contract

Project Owner decision 7 is authoritative:

- Password change, LOCAL/INTERNAL administrator reset, and auth-account suspension/disable call `ISessionInvalidationService` before commit.
- The service replaces `security_stamp` with a new non-empty GUID and sets `sessions_invalidated_at` to the later of the current value and supplied UTC time.
- The mutation is made on the same tracked `UserAuthAccount`, so password/history/account/invalidation state is atomic.
- A no-op implementation is prohibited; tests must prove both stamp and cutoff persistence.
- Slice C must copy the current stamp into access-token claims, compare it on protected use, and reject refresh tokens issued at or before the cutoff.
- Slice B does not update `Refresh_Tokens`, issue a token, or create a session.
- Immediate invalidation when an existing Organization `Users` status changes is a cross-slice integration dependency. Slice B defines the callable contract but does not modify the accepted Organization service in this manifest. Before authentication reaches production, the Project Owner must allocate that integration to an authorized slice; Slice C/E must also re-read eligibility and fail closed.

## 15. Transactions, locking, and concurrency

- All account mutations use one explicit SQL Server transaction and one fresh `AppDbContext` per execution-strategy attempt.
- Known-account credential attempts serialize the account row with EF Core parameterized `FromSqlInterpolated` and `UPDLOCK, HOLDLOCK`; no Dapper dependency is required.
- The lock is held through password verification and state mutation to prevent lost failure increments. This contention tradeoff is accepted only for a single account key and must be load-tested before Production.
- Password change/reset locks the account, loads the latest five history rows, validates, inserts history, updates the account, invalidates sessions, and commits atomically.
- `row_version` is an EF concurrency token. Administrative operations require the caller's target rowversion; stale versions map to a stable 409 conflict in the later API.
- Authentication does not accept a client rowversion. It serializes known-account attempts; a conflicting privileged update causes a fresh-context retry or a sanitized infrastructure failure, never a partial mutation.
- The existing SQL 1205 execution strategy remains maximum three total attempts (initial plus two retries). No new general retry policy is introduced.
- Tests must prove that a failed history insert, failed account update, or forced concurrency conflict rolls back every mutation.

## 16. Non-enumeration and secret handling

- Unknown account, unknown provider, external account on the password path, missing hash, wrong password, locked account, suspended account, disabled account, and invalid employment status share the same generic public application outcome.
- A real or dummy framework hash verification is always performed before returning that generic outcome.
- Eligibility and lockout details are internal only. Restricted audit may record a scrubbed internal classification in Slice F, but public code, message, shape, and transport behavior must not identify the cause.
- Slice B defines no HTTP endpoint. Before Slice C is authorized, its plan must select one uniform public HTTP mapping that reconciles DEC-1B-020 with Project Owner decision 5; it may not expose distinct locked/disabled/suspended outcomes.
- Raw passwords are accepted only as method inputs and are never stored outside the hasher call, returned, interpolated, logged, audited, serialized, or included in exceptions.
- Password hashes are persistence-only values; DTOs/results, logs, and Problem Details never contain them.
- Provider subjects are parameterized and omitted from routine technical logs. Correlation IDs and stable outcome codes are sufficient.

## 17. Proposed error and result mappings

Slice B defines application outcomes only. HTTP mapping is documented for Slice C/F and is not implemented here.

| Condition | Stable code | Future HTTP | Exposure rule |
|---|---|---:|---|
| Nonexistent, unknown provider, external password path, missing/bad password, locked, suspended, disabled, or employment-ineligible | `AUTH_INVALID_CREDENTIALS` | Deferred to Slice C; one uniform mapping required | Same generic code, body, message, shape, and observable behavior |
| Valid temporary/current credential requiring change | `AUTH_PASSWORD_CHANGE_REQUIRED` | 403 | No business access; Slice C defines limited change flow |
| Expired temporary credential | `AUTH_INVALID_CREDENTIALS` | Same uniform Slice C mapping | Do not reveal expiry/account existence |
| Password outside 8–64 | `AUTH_PASSWORD_LENGTH_INVALID` | 400 | Password value never echoed |
| Password contains canonical subject | `AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT` | 400 | Subject/password never echoed |
| Current or previous-five password reuse | `AUTH_PASSWORD_REUSE` | 409 | No history/hash detail |
| Missing administrative account | `AUTH_ACCOUNT_NOT_FOUND` | 404 | Only a future authorized administration endpoint |
| Stale target rowversion | `AUTH_ACCOUNT_CONCURRENCY_CONFLICT` | 409 | Sanitized conflict detail |
| Invalid account-state transition | `AUTH_ACCOUNT_STATE_CONFLICT` | 409 | Sanitized state conflict |
| Local password reset requested for external account | `AUTH_EXTERNAL_PASSWORD_MANAGED` | 409 | Authorized administration surface only; no provider secret/detail |
| Unmapped SQL/infrastructure failure | `AUTH_UNEXPECTED_DATABASE_ERROR` | 500 | No SQL, table, hash, or account detail |

The Project Owner accepted these proposed Slice B application codes as part of the official plan. Their API transport use remains unimplemented and unauthorized.

## 18. Exact proposed implementation file manifest

No file below may be changed before implementation authorization. V0003/U0003 and every API/frontend file are deliberately absent.

### Proposed new production files

- `src/backend/PTKD.Domain/Entities/UserAuthAccount.cs`
- `src/backend/PTKD.Domain/Entities/PasswordHistory.cs`
- `src/backend/PTKD.Domain/Security/Authentication/AuthenticationAccountPolicy.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IAuthenticationDbContext.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IAuthenticationDbContextFactory.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IPasswordHashService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IProviderSubjectNormalizer.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/ISessionInvalidationService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Interfaces/IUtcClock.cs`
- `src/backend/PTKD.Application/Security/Authentication/Models/AuthenticationAttemptResult.cs`
- `src/backend/PTKD.Application/Security/Authentication/Models/AuthenticationAccountCommands.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/IAuthenticationAccountService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/AuthenticationAccountService.cs`
- `src/backend/PTKD.Application/Security/Authentication/Services/SecurityStampSessionInvalidationService.cs`
- `src/backend/PTKD.Infrastructure/Persistence/AuthenticationDbContextFactory.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/UserAuthAccountConfiguration.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/PasswordHistoryConfiguration.cs`
- `src/backend/PTKD.Infrastructure/Security/Authentication/AspNetCorePasswordHashService.cs`
- `src/backend/PTKD.Infrastructure/Security/Authentication/InternalProviderSubjectNormalizer.cs`
- `src/backend/PTKD.Infrastructure/Time/SystemUtcClock.cs`

### Proposed modified production files

- `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs` — implement the authentication context and add the two DbSets.
- `src/backend/PTKD.Infrastructure/PTKD.Infrastructure.csproj` — add the `Microsoft.AspNetCore.App` framework reference; no package reference.

### Proposed new test files

- `tests/backend/PTKD.UnitTests/Security/Authentication/UserAuthAccountTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/AuthenticationAccountPolicyTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/AuthenticationAccountServiceTests.cs`
- `tests/backend/PTKD.UnitTests/Security/Authentication/SecurityStampSessionInvalidationServiceTests.cs`
- `tests/backend/PTKD.IntegrationTests/AuthenticationAccountPersistenceTests.cs`
- `tests/backend/PTKD.IntegrationTests/AuthenticationLifecycleIntegrationTests.cs`

### Proposed modified test/project files

None. Existing project references and the protected integration fixture are sufficient. If implementation proves otherwise, stop and request a manifest amendment rather than silently broadening scope.

## 19. Unit-test matrix

| Test area | Required cases | Traceability |
|---|---|---|
| Provider identity | INTERNAL type/subject canonicalization; exact two-column lookup inputs; empty/overlength rejected; external subject remains opaque | DEC-1B-001 |
| Hash invariants | INTERNAL requires hash; external allows/requires null; external local reset is rejected without mutation; no result exposes hash | DEC-1B-001, approved decisions 3–4, SEC-005 |
| Length policy | 7 rejected; 8 accepted; 64 accepted; 65 rejected | DEC-1B-002 |
| Subject exclusion | Exact, case variants, embedded subject rejected; unrelated password accepted | DEC-1B-002 |
| History | Current rejected; each latest five rejected; sixth-oldest accepted; initial creation adds none; replacement appends outgoing hash once | DEC-1B-002 |
| Temporary lifecycle | Exactly before expiry accepted; at/after expiry rejected; successful self-change clears must-change/expiry | DEC-1B-002 |
| Lockout | Failures 1–4; fifth locks for 15 minutes; no window extension; manual lock; successful reset | DEC-1B-004 |
| Owner decisions | One test set per approved decision for expiry reset, unlock, LOCAL reset, external-reset rejection, ineligible failures, rehash history, non-no-op invalidation, UTC, no character classes, and deferred external case sensitivity | Approved decisions 1–10 |
| Eligibility | Every account/user/employment status combination; only ACTIVE + ACTIVE + ACTIVE/PROBATION succeeds; unknown casing handled; unknown values fail closed | DEC-1B-013, AUTH-006 |
| Rehash | `SuccessRehashNeeded` updates hash and does not append history | Approved decision 6 |
| Invalidation | Stamp changes, cutoff advances monotonically, operation is idempotent for an older supplied time, reason is non-secret | DEC-1B-002, decision 7 |
| Non-enumeration | Unknown, external-password, missing-hash, wrong-password, locked, suspended, disabled, and employment-ineligible paths return the identical public result and invoke the required real/dummy verification behavior | DEC-1B-020, approved decision 5, SEC-005 |
| Error mapping | Every application failure has the stable proposed code and contains no subject/password/hash/SQL details | AGENTS architecture rules, SEC-005 |

Pure domain tests should use no mocking framework. Application-service tests may use small hand-written fakes so no test package change is required.

## 20. SQL Server integration-test matrix

All tests must use `TestDatabaseFixture.ResetToV0003()` and the existing exact database guard. No test may create/drop a database or bypass `SELECT DB_NAME()` verification.

| Test area | Required proof |
|---|---|
| EF mapping | Round-trip every mapped account field; exact table/column names and max lengths; external null hash; linked user lookup |
| Accepted uniqueness | Duplicate `(provider_type, provider_subject)` fails with the accepted constraint; application maps provisioning conflict safely |
| Rowversion | Every account mutation changes rowversion; stale admin reset/unlock loses with a 409-mappable concurrency result |
| History ordering | Latest five are deterministic by `created_at DESC, id DESC`, including equal millisecond timestamps |
| Append-only history | Existing trigger continues to reject UPDATE/DELETE; Slice B performs INSERT only |
| Password-change atomicity | Forced history/account failure leaves account hash, flags, stamp, cutoff, and history unchanged |
| Admin-reset atomicity | LOCAL/INTERNAL hash/history/must-change/UTC expiry/lockout/stamp/actor metadata commit together or all roll back; external reset changes nothing |
| Failure concurrency | Five concurrent/serialized invalid attempts produce no lost increments and exactly the approved lock state |
| Success concurrency | Successful login racing with admin reset/disable cannot overwrite the privileged transition or return stale success |
| Lockout boundaries | Database-persisted millisecond values behave correctly immediately before, at, and after end time |
| Eligibility | Persisted `ACTIVE`, `Active`, PROBATION, suspended, resigned, disabled, and unknown values enforce the fail-closed matrix |
| Rehash persistence | Per approved decision 6, rehash updates current hash and rowversion atomically and does not append history |
| Invalidation persistence | Password change, LOCAL/INTERNAL reset, and auth-account suspension/disable advance stamp and cutoff in the same transaction; a no-op implementation fails; no Refresh_Tokens row is created or changed |
| Secret exclusion | Captured exceptions/results/log test sink contain no plaintext password or stored hash |
| Regression | Existing SecuritySchemaTests still prove accepted V0003 unchanged; all Phase 1A.2 tests remain enabled |

The authorized implementation subsequently added and passed the integration matrix. Exact commands, totals, and protected-database evidence are recorded in `docs/architecture/phase-1b1b-authentication-account-password-implementation.md`.

## 21. Dependencies on later slices

### Slice C

Slice C must:

- expose login/change-password/refresh/logout contracts and transport mappings;
- issue and validate JWTs;
- consume `AuthenticationAttemptResult` without bypassing eligibility or must-change state;
- compare the token security-stamp claim with the current account stamp;
- reject refresh tokens issued at or before `sessions_invalidated_at`;
- define the safe limited mechanism by which a must-change user can change the password;
- select one uniform public HTTP mapping for the generic denial outcome and preserve non-enumerating code/body/timing behavior across nonexistent, bad-password, locked, suspended, and ineligible cases;
- implement token/session persistence without changing Slice B password-history rules.

### Slice F

Slice F must:

- implement semantic-scrubbing security audit writing;
- persist admin reset/unlock/disable/lock and password-change audit events without password/hash content;
- provide audit atomicity for administration before any corresponding API is exposed;
- implement bootstrap separately; bootstrap may call the authorized Slice B password lifecycle but must not duplicate it.

Until Slice F exists, Slice B administration behavior is library/test surface only and must not be reachable from a controller, background command, or bootstrap executable.

## 22. Package and migration impact

- **NuGet packages:** none proposed.
- **Framework reference:** proposed `Microsoft.AspNetCore.App` in `PTKD.Infrastructure.csproj`; requires Slice B implementation authorization.
- **MediatR/Dapper:** none.
- **Forward migration:** none.
- **Rollback migration:** none.
- **V0003/U0003:** no modification.
- **Database execution during planning:** none.
- **API/frontend/configuration runtime files:** none.

Case-sensitive external-provider subject handling is accepted as deferred and non-blocking. If a future provider needs deterministic case-sensitive subject uniqueness, create a separate V0004/U0004 proposal and obtain explicit authorization. Do not retrofit the accepted V0003 baseline.

## 23. Risks and controls

| Risk | Control |
|---|---|
| Lockout can be abused for targeted denial of service | Non-enumeration, no window extension, exact threshold, no failure increments for ineligible accounts, operational monitoring in a later slice |
| Concurrent attempts lose increments | Per-account update/hold lock, transaction, rowversion, concurrency integration tests |
| PBKDF2 work while holding a row lock increases contention | Lock only one known account row; load/concurrency test before Production; no broad table lock |
| Existing V2 status values are not constrained/canonicalized | Case-insensitive approved-value comparison; all unknown values fail closed |
| External subject collation may be incompatible | Accepted deferred/non-blocking; external implementation excluded; future V0004/U0004 requires a separate decision |
| Rehash pollutes history | Approved decision 6 and explicit no-history tests |
| Password/hash leaks through diagnostics | Dedicated sanitized results, dummy verification, no value interpolation, secret-exclusion tests |
| Admin actions lack immutable audit in Slice B | No runtime exposure until Slice F writer and audit atomicity exist |
| Linked Organization `Users` status changes do not yet rotate the auth stamp | Explicit cross-slice integration dependency; Slice B suspension/disable covers the auth account only and protected requests must fail closed on current eligibility |
| Earlier documents distinguish 401 invalid credentials and 403 lockout | Slice B returns one generic public application outcome; separately authorized Slice C must choose a uniform observable HTTP mapping before implementing an endpoint |
| `latestFeature` selects a later installed SDK over time | Record `dotnet --version` in every executable evidence report |

## 24. Entry and completion criteria

### Entry gate for implementation

All are required:

1. **SATISFIED:** Project Owner accepted the plan and decisions 1–10 on 2026-07-16.
2. **SATISFIED:** Project Owner separately authorized Phase 1B.1-B implementation, testing, documentation, the exact file manifest, and one implementation commit.
3. **SATISFIED:** Compilation proved `Microsoft.AspNetCore.App` was required by Infrastructure for `PasswordHasher<TUser>`; the authorized framework reference was added and no NuGet package was added.
4. **SATISFIED:** HEAD and tracked/staged cleanliness were verified immediately before implementation.
5. **SATISFIED:** V0003/U0003 remain byte-for-byte unchanged.

### Completion gate for an authorized future implementation

The future implementation report must show:

- exact SDK selected by `dotnet --version`;
- warning-as-error solution build success;
- all existing and new unit tests passing;
- all existing and new SQL Server integration tests passing against verified `PTKD_TEST_PHASE1A2`;
- focused authentication test filters and exact totals;
- no deleted/weakened tests;
- no API, JWT, refresh-token, cookie, frontend, bootstrap, audit-writer, AD/LDAP, MediatR, Dapper, migration, rollback, or package change;
- changed files exactly match the authorized manifest;
- no password/hash/secret in logs, results, snapshots, or audit payloads;
- unresolved Slice C/F dependencies reported without claiming full authentication completion.

Proposed executable commands for the future authorized gate:

```text
dotnet --version
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --warnaserror
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --configuration Debug --no-restore
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Authentication"
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Authentication"
```

## 25. Project Owner plan acceptance and implementation authorization

- **Project Owner result:** Phase 1B.1-B plan accepted as the official plan; implementation **NOT AUTHORIZED**.
- **Decision 1:** Approved — lazy atomic UTC reset at the first attempt at/after lockout expiry, then process it as attempt 1.
- **Decision 2:** Approved — administrator unlock clears `lockout_end`, resets failures, and transitions the auth account to ACTIVE.
- **Decision 3:** Approved — LOCAL/INTERNAL administrator reset creates a 24-hour temporary password, sets must-change, clears lockout/failures, and changes the stamp atomically.
- **Decision 4:** Approved — external-provider accounts with null hashes cannot receive a local password reset.
- **Decision 5:** Approved — ineligible/suspended/invalid-employment accounts do not increment failures and all listed public authentication denials are generic/non-enumerating.
- **Decision 6:** Approved — `SuccessRehashNeeded` updates the current hash without history and is not a password change.
- **Decision 7:** Approved — password change, LOCAL/INTERNAL reset, and auth-account suspension/disable change `security_stamp` in the same transaction; no no-op invalidator.
- **Decision 8:** Approved — failure and lockout time behavior uses UTC.
- **Decision 9:** Approved — no additional password character-class rule.
- **Decision 10:** Approved — case-sensitive external subject handling is deferred/non-blocking; no V0004/U0004 in Slice B without a separate decision.
- **Project Owner conditions at plan acceptance:** Plan acceptance alone did not authorize source or test changes. A later direct written authorization authorized Slice B implementation, testing, documentation, the framework reference, and one implementation commit only. Migration, API, JWT, frontend, package, Production migration, tag, and push remain unauthorized. Phase 1B.1-C through I remain NOT AUTHORIZED.
- **Project Owner name:** Đào Hải Bách.
- **Role:** Project Owner.
- **Acceptance date:** 2026-07-16.
- **Confirmation method:** Direct written authorization.
- **Implementation authorization:** AUTHORIZED BY SEPARATE DIRECT WRITTEN AUTHORIZATION; implementation is verified and awaiting Project Owner acceptance.

PHASE 1B.1-B IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE
