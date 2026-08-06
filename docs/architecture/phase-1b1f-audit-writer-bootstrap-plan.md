# Phase 1B.1-F — Audit Writer and Initial Admin Bootstrap Plan

## Status
ACCEPTED PLAN — PHASE 1B.1-F IMPLEMENTATION MAY BE AUTHORIZED BY SLICE

PHASE 1B.1-F-A IMPLEMENTATION ACCEPTED — SEE phase-1b1f-a-project-owner-implementation-acceptance.md
PHASE 1B.1-F FINAL ACCEPTED — SEE phase-1b1f-project-owner-final-acceptance.md

## Accepted baseline
- Phase 1B.1-E closure accepted: commit `0e7017f4b9f218bbe6f082a649eab5e046ef13be`
- Post Phase 1B.1-E backfill accepted: commit `0346ecb032e3847aace1508139d265a6e79e1979`
- Acceptance recorded: commit `2f73584cccf121d501a593987f4a1dd87883d24d`
- Phase F planning commit (this document): TBD

## Purpose

Phase F provides two foundational capabilities that are prerequisites for any live security operation:

1. **Audit Writer** — the C# application-layer service that writes immutable `Security_Audit_Events` records. Every Security Administration endpoint that mutates state must call the Audit Writer. The database schema already enforces append-only at the SQL trigger and database-role level (V0003); Phase F adds the application-layer write path.

2. **Initial Admin Bootstrap** — the controlled, one-time command that provisions the first System Administrator account. Without it, no authenticated user can hold `SECURITY_ADMIN_MANAGE` and the entire security administration API is permanently inaccessible after a fresh database deployment.

Neither component is a business feature. Both are infrastructure pre-conditions for Phase 1B.1-G and all subsequent phases.

---

## Discovery audit

### 1. Existing audit tables/entities/configurations found

**Database layer (V0003 — fully in place):**
- `Security_Audit_Events` table with 17 columns: `id`, `actor_user_id`, `acting_as_user_id`, `target_user_id`, `company_id`, `event_code`, `entity_type`, `entity_id`, `changed_fields`, `before_state_json`, `after_state_json`, `reason`, `correlation_id`, `request_metadata`, `outcome`, `policy_version`, `created_at`
- INSTEAD OF trigger `TR_Security_Audit_Events_AppendOnly` blocking UPDATE and DELETE
- Database role `PTKD_Security_Audit_Runtime` with GRANT SELECT/INSERT, DENY UPDATE/DELETE/ALTER on `Security_Audit_Events`
- JSON validity check constraints on `changed_fields`, `before_state_json`, `after_state_json`, `request_metadata`
- Indexes: `IX_SecurityAuditEvents_CreatedAt` (created_at DESC, id DESC), `IX_SecurityAuditEvents_CorrelationId`

**Application layer:** No C# `SecurityAuditEvent` entity class exists anywhere in `src/backend/`. The V0003 migration header explicitly states: *"Application audit writers introduced in a later authorized slice must sanitize JSON payloads so passwords, password hashes, raw tokens, signing keys, secrets, file bytes, and permanent signed URLs are never persisted here."*

### 2. Existing audit writer/service interfaces found

NONE. No `IAuditWriter`, `ISecurityAuditService`, `ISecurityAuditWriter`, or equivalent interface exists in `src/backend/`. Phase F must define and implement this from scratch.

### 3. Existing audit immutability tests found

Schema-level tests in `tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs`:
- `AuditRuntimeRole_AllowsSelectInsert_AndDeniesUpdateDeleteAlterTruncate`
- `AuditAppendOnlyTrigger_BlocksUpdateAndDeleteForPrivilegedWriter`
- `AuditJsonColumns_RejectInvalidJson` (Theory, 4 column cases)
- `AuditSchema_HasRequiredFieldsNoSecretSpecificFieldsAndNoSqlView`

All are at the SQL schema level. No application-layer audit writer tests exist. Phase F must add application-layer tests (entity mapping, writer contract, writer behavior under failure).

### 4. Existing bootstrap tables/entities/configurations found

**Database layer (V0003 — fully in place):**
- `Security_Bootstrap_State` singleton table: `id INT` (CHECK id = 1), `is_bootstrapped BIT`, `bootstrapped_at DATETIME2(3) NULL`, `bootstrapped_by_user_id BIGINT NULL` (FK → Users), `row_version ROWVERSION`
- Check constraint `CK_SecurityBootstrapState_Consistency` — coherence: is_bootstrapped=0 requires both nullable columns NULL; is_bootstrapped=1 requires both NOT NULL
- Seeded with (1, 0, NULL, NULL) — not-bootstrapped initial state

**Application layer:** No C# `SecurityBootstrapState` entity class exists in `src/backend/`. No `IBootstrapService`, `BootstrapCommand`, or bootstrap-specific interface found.

### 5. Existing bootstrap command/service stubs found

NONE. `Program.cs` calls `.CreateBootstrapLogger()` but this is Serilog's bootstrap-phase startup logging — unrelated to the security Initial Admin Bootstrap.

### 6. Existing tests related to bootstrap found

Schema-level tests in `SecuritySchemaTests.cs`:
- `SingletonStatesAndMutableRows_EnforceSingletonsAndChangeRowVersion` — tests the DB singleton constraint
- `Rollback_BootstrappedStateBlocks` — tests rollback protection when bootstrap state is non-pristine

Application-level test `BootstrapCommand_Runs_Once_Only` (listed in Phase 1B.0 traceability matrix, target database `PTKD_TEST_PHASE1B`) does NOT yet exist. Phase F must implement it.

### 7. Whether Phase F requires a new migration

NO. All required tables (`Security_Audit_Events`, `Security_Bootstrap_State`), constraints, triggers, and the `PTKD_Security_Audit_Runtime` database role already exist in V0003. Phase F adds application code only — no migration, no migration rollback.

### 8. Whether Phase F can reuse existing V0003 schema or needs V0004/U0004

Phase F reuses V0003 entirely. V0004/U0004 is NOT part of Phase F scope. If a future phase requires schema changes they will be separately authorized.

### 9. Current test database name actually used by this repo

`PTKD_TEST_PHASE1A2` — hardcoded in `tests/backend/PTKD.IntegrationTests/TestDatabaseSafety.cs` as `ApprovedDatabaseName` and `DefaultConnectionString`. This database already has V0003 applied and all required tables.

The Phase 1B.0 traceability matrix references `PTKD_TEST_PHASE1B` for Phase 1B application integration tests. This database name is NOT currently approved in `TestDatabaseSafety.cs`. Whether Phase F tests run against `PTKD_TEST_PHASE1A2` (already available) or a new `PTKD_TEST_PHASE1B` approval must be decided in OD-F-09.

### 10. Any contradiction between docs and current source

- **PermissionCodes.cs gap:** Phase 1B.0 decisions list 15 permission codes (SECURITY_USER_VIEW, SECURITY_USER_MANAGE, SECURITY_ROLE_VIEW, etc.) that exist in the V0003 seed and are used by Security Administration controllers — but these are NOT yet reflected as constants in `PermissionCodes.cs` (which holds only 5 constants scoped to Phase 1B.1). This is a known gap; current controllers use the 5 Phase-1B.1 constants. Expanding `PermissionCodes.cs` to cover the full V0003 seed catalog is out of scope for Phase F and requires a separate authorized slice.
- **PTKD_TEST_PHASE1B test database:** The Phase 1B.0 traceability matrix names `PTKD_TEST_PHASE1B` for Phase 1B integration tests. No approval for this database name exists in `TestDatabaseSafety.cs`. Phase F must resolve this in OD-F-09.
- **No other contradictions found.** V0003 schema exactly matches the approved Phase 1B.0 schema decisions. The U0003 rollback confirms the approved 15-permission seed catalog, the Security_Bootstrap_State singleton, and the Authorization_Policy_State singleton.

---

## Owner decisions required

### OD-F-01 — SecurityAuditEvent: domain entity vs write record

**Question:** Should `SecurityAuditEvent` be a tracked EF domain entity in `PTKD.Domain`, or a simple write-record (a plain C# class with no EF tracking behavior)?

**Proposed decision:** Write-record only — a sealed `SecurityAuditEventRecord` class in `PTKD.Domain/Security/Audit/` with all the fields needed to insert one row into `Security_Audit_Events`. No EF `DbSet<SecurityAuditEvent>` on the main `AppDbContext`.

**Rationale:** `Security_Audit_Events` is append-only by invariant. An EF `DbSet` for a tracked entity implies read-back and change tracking, which is not needed for audit writes and creates risk of accidentally staging an Update or Delete. A write-record that maps directly to an INSERT is the minimal, correct representation. Read-back for `SECURITY_AUDIT_VIEW` endpoints is a separate, later concern.

**Impact:** Phase F only adds the write path. Read path is explicitly deferred (SECURITY_AUDIT_VIEW enforcement is already documented as deferred).

---

### OD-F-02 — IAuditWriter interface contract and location

**Question:** Where does `IAuditWriter` live and what is its contract?

**Proposed decision:** Define `IAuditWriter` in `PTKD.Application/Security/Audit/Interfaces/` with a single async method:

```csharp
Task WriteAsync(SecurityAuditEventRecord record, CancellationToken cancellationToken = default);
```

`SecurityAuditEventRecord` carries all columns needed for one `Security_Audit_Events` row. `IAuditWriter` is consumed by Application layer services. Infrastructure implements it.

**Rationale:** Clean architecture — Application layer defines the interface, Infrastructure provides the implementation. Services that need to audit (Authentication, Security Administration) only depend on `IAuditWriter` injected via DI. No cross-layer coupling.

---

### OD-F-03 — Audit writer data access strategy

**Question:** Should the `IAuditWriter` implementation use EF `DbContext`, Dapper, or raw `SqlCommand`?

**Proposed decision:** Direct parameterized SQL INSERT via `SqlCommand` (raw ADO.NET), using a dedicated short-lived `SqlConnection` per write, NOT the shared `AppDbContext`. The implementation lives in `PTKD.Infrastructure/Security/Audit/`.

**Rationale:**
- SEC-001: append-only. Using the shared `AppDbContext` risks the EF change tracker accidentally including audit entities in a broader SaveChanges that could be rolled back or combined with other operations.
- DEC-1B-015: "Dapper and raw SQL paths are tested." Raw ADO.NET is the explicit tested path.
- The `PTKD_Security_Audit_Runtime` database role has only INSERT/SELECT. The audit write path should use a connection that authenticates under (or is granted) this restricted role, enforcing the least-privilege boundary.
- A direct INSERT decoupled from the application transaction lets the audit persist even if the outer transaction rolls back — which may be intentional for failed-operation audit events.

---

### OD-F-04 — Audit writer failure behavior: fail-closed vs fail-open

**Question:** If `IAuditWriter.WriteAsync` throws (e.g., SQL error, transient network), should the calling operation fail (fail-closed) or should the operation complete while the audit failure is logged separately (fail-open)?

**Proposed decision — requires explicit Project Owner decision.**

Two valid positions:

| Position | Behavior | When correct |
|---|---|---|
| **Fail-closed** | Audit write failure surfaces as an exception; operation is rolled back or returns error. | When the business rule says "no audit = no operation" — strictly correct for GOV-007 (all material permission changes require immutable audit). |
| **Fail-open with observability** | Audit write failure is caught, logged to structured error log, increments a metric counter; operation succeeds. | When audit failure is a transient infrastructure failure and the operation is business-critical. Trades some audit completeness for availability. |

**Proposed decision:** Fail-closed for all Security Administration mutations. Reasoning: GOV-007 requires immutable audit records for all material permission/workflow/payment/customer-master changes. A Security Administration action with no audit record violates GOV-007. The sanitized HTTP error returned to the client is 500 with no detail.

**This decision must be confirmed or overridden by Project Owner.** Record as OD-F-04 accepted or modified.

---

### OD-F-05 — SecurityBootstrapState C# entity mapping

**Question:** Should `SecurityBootstrapState` be a tracked EF entity on `AppDbContext` (or a dedicated bootstrap DbContext), or accessed via direct SQL?

**Proposed decision:** Tracked EF entity on a dedicated `IBootstrapDbContext` (minimal DbContext interface in `PTKD.Application/Security/Bootstrap/Interfaces/`) with a single `DbSet<SecurityBootstrapState>`. Infrastructure implementation adds `SecurityBootstrapState` to a context that maps only this one table.

**Rationale:** Bootstrap state is a singleton with read/update behavior (not append-only). EF is appropriate here. Isolating it to a dedicated context prevents accidental cross-context writes with the main application context during bootstrap execution.

---

### OD-F-06 — Bootstrap command delivery mechanism

**Question:** How is the Initial Admin Bootstrap command delivered and invoked?

**Per DEC-1B-010 (APPROVED BASELINE):** "Separate controlled bootstrap executable or command (never runs automatically during API startup). Executed only by an authorized operator."

**Proposed decision:** Implement as a standalone `PTKD.Bootstrap` console project (`src/backend/PTKD.Bootstrap/`), invocable via:
```
dotnet run --project src/backend/PTKD.Bootstrap
```

This project references `PTKD.Application` and `PTKD.Infrastructure` for service registration, but does NOT reference `PTKD.Api`. It cannot be accidentally started by the web API startup. It is an opt-in operator command, not a hosted service.

**Alternatives:**
- PTKD.Worker command (reuse existing worker project) — viable but mixes concerns.
- Separate migration-tool command pattern — viable.

Project Owner must approve the selected delivery mechanism.

---

### OD-F-07 — Bootstrap secret input mechanism

**Question:** How does the bootstrap command receive the initial admin password and username without printing or logging them?

**Per DEC-1B-010:** "Reads initial secret from an approved enterprise secret provider or protected deployment input. Never prints password, token or secret."

**Proposed decision:** Environment variable input only.

- `PTKD_BOOTSTRAP_USERNAME` — the initial admin username (login name / provider_subject)
- `PTKD_BOOTSTRAP_PASSWORD` — the initial admin password (plaintext, consumed once, hashed immediately, then zeroed)

Environment variables are not printed by the bootstrap command. The password value is treated as a `SecureString`-equivalent: consumed, hashed, then the original variable is cleared from process memory. No command-line arguments (command line is visible in process lists). No interactive prompts (non-interactive deployment context).

**Alternatives:** Azure Key Vault reference, protected input file. Deferred to when deployment topology is confirmed.

**Project Owner must confirm acceptable input mechanism for Phase F.**

---

### OD-F-08 — Bootstrap audit event code

**Question:** What `event_code` value is written to `Security_Audit_Events` when bootstrap creates the initial admin account?

**Per DEC-1B-010:** "Writes immutable BOOTSTRAP_ADMIN_CREATED audit."

**Proposed decision:** Confirm `BOOTSTRAP_ADMIN_CREATED` as the canonical `event_code` string. This value is a constant in application code — not a database-seeded permission code. It identifies the bootstrap audit record uniquely and should never be reused for any other event type.

Additional fields in the audit record:
- `entity_type = "USER"` (the bootstrapped User row)
- `entity_id = <user_id as string>`
- `outcome = "SUCCESS"` or `"FAILURE"` (if bootstrap fails mid-transaction)
- `actor_user_id = NULL` (no authenticated user; the bootstrap command is the actor)
- `correlation_id = NEWID()` (generated by bootstrap command)
- `after_state_json` = sanitized new user/account representation (no password hash, no token)

Project Owner must confirm this event code and field conventions.

---

### OD-F-09 — Test database for Phase F integration tests

**Question:** Should Phase F integration tests continue using the existing approved database `PTKD_TEST_PHASE1A2` (already has V0003 schema), or introduce a new approval for `PTKD_TEST_PHASE1B`?

**Current state:** `TestDatabaseSafety.ApprovedDatabaseName = "PTKD_TEST_PHASE1A2"`. The Phase 1B.0 traceability matrix references `PTKD_TEST_PHASE1B` for Phase 1B tests, but this database name is not yet approved in code.

**Proposed decision:** Continue using `PTKD_TEST_PHASE1A2` for Phase F tests. No changes to `TestDatabaseSafety.cs`. All new Phase F integration tests (audit writer, bootstrap command) run against the existing approved database.

**Rationale:** All required tables are already in V0003 which is already applied to `PTKD_TEST_PHASE1A2`. Introducing a new database name requires changes to `TestDatabaseSafety.cs`, a separate approval gate, and a new database to be provisioned. The Phase 1B.0 traceability matrix's use of `PTKD_TEST_PHASE1B` was a planning-time name; the authoritative source is `TestDatabaseSafety.cs`.

**Project Owner must confirm database choice for Phase F tests.**

---

### OD-F-10 — Phase F sub-phase sequencing

**Question:** Should Phase F be implemented as one slice or split into F-A (Audit Writer) and F-B (Bootstrap)?

**Proposed decision:** Split into two sub-phases:

**Phase F-A (Audit Writer):**
- `SecurityAuditEventRecord` write-record class
- `IAuditWriter` interface
- `SqlAuditWriter` infrastructure implementation
- EF entity mapping for `SecurityAuditEvent` read path (optional, only if read path is in F-A scope)
- Unit tests for sanitization behavior
- Integration tests for write path, append-only enforcement at application layer, JSON sanitization

**Phase F-B (Bootstrap):**
- `SecurityBootstrapState` EF entity class and mapping
- `IBootstrapService` application interface
- `BootstrapService` implementation
- `PTKD.Bootstrap` console project
- Integration test: `BootstrapCommand_Runs_Once_Only`
- Integration test: `Bootstrap_Writes_BOOTSTRAP_ADMIN_CREATED_Audit`
- Integration test: `Bootstrap_RejectsIfAlreadyBootstrapped`

**Rationale:** Audit Writer is consumed by Security Administration endpoints that already exist. Bootstrap depends on Audit Writer being available (bootstrap must write a `BOOTSTRAP_ADMIN_CREATED` event). Sequential order is forced by the dependency.

**Project Owner must confirm sub-phase split and sequence.**

---

### OD-F-11 — PTKD_Security_Audit_Runtime role: application runtime membership

**Question:** Should the application runtime SQL principal be a member of `PTKD_Security_Audit_Runtime` (giving it INSERT/SELECT on `Security_Audit_Events`) or should audit writes use a separate connection/principal?

**Context:** V0003 created `PTKD_Security_Audit_Runtime` with:
- GRANT SELECT on `Security_Audit_Events`
- GRANT INSERT on `Security_Audit_Events`
- DENY UPDATE, DELETE, ALTER on `Security_Audit_Events`

The regular application runtime principal (used for all other DB access) may or may not be a member of this role. If it is, the application cannot accidentally UPDATE or DELETE audit rows. If it is not, the audit write path needs a separate SQL login/principal.

**Proposed decision:** Add the application runtime principal to `PTKD_Security_Audit_Runtime` as part of Phase F deployment documentation. The `SqlAuditWriter` uses the standard application connection string. The database role enforces the INSERT/SELECT only boundary at the SQL Server level regardless of what application code attempts.

**Impact:** This is a database deployment configuration concern, not a code change. Phase F produces documentation specifying the required role membership. Actual membership in production is an infrastructure/DBA action.

**Project Owner must confirm this approach.**

---

### OD-F-12 — Phase F explicit scope exclusions

The following items are explicitly NOT in Phase F scope regardless of implementation decisions above:

- No `SECURITY_AUDIT_VIEW` endpoint enforcement (deferred — endpoint does not exist yet)
- No read-path API for `Security_Audit_Events` (no controller, no query service)
- No migration (V0003 is sufficient)
- No frontend or business module changes
- No changes to `PermissionCodes.cs` constants (only 5 constants are authorized for Phase 1B.1)
- No expansion of the V0003 permission seed catalog beyond the 15 codes already seeded
- No production seed data (bootstrap is operator-executed, not automatic seed)
- No archiving, purging, or retention enforcement for `Security_Audit_Events` (deferred per DEC-1B-017)
- No Authorization_Policy_State version bump triggered by bootstrap (bootstrap creates a user but does not change the permission catalog policy version)
- No change to `AuthController` behavior
- No new permission codes
- No changes to existing tests

**Project Owner must confirm this scope boundary.**

---

## Implementation scope (pending OD acceptance)

### Phase F-A — Audit Writer

| Item | Location | Notes |
|---|---|---|
| `SecurityAuditEventRecord` write-record | `PTKD.Domain/Security/Audit/` | Sealed class, all `Security_Audit_Events` columns |
| `IAuditWriter` | `PTKD.Application/Security/Audit/Interfaces/` | Single `WriteAsync` method |
| `SqlAuditWriter` | `PTKD.Infrastructure/Security/Audit/` | Direct parameterized INSERT; sanitizes JSON fields |
| DI registration | `PTKD.Infrastructure` ServiceCollection extension | Singleton or scoped per OD-F-03 decision |
| Unit tests | `PTKD.UnitTests` | Sanitization, field mapping |
| Integration tests | `PTKD.IntegrationTests` | Write round-trip, append-only enforcement, JSON validation at app layer, no-secrets test |

### Phase F-B — Initial Admin Bootstrap

| Item | Location | Notes |
|---|---|---|
| `SecurityBootstrapState` entity | `PTKD.Domain/Security/Bootstrap/` | Singleton entity, id=1 |
| `IBootstrapDbContext` | `PTKD.Application/Security/Bootstrap/Interfaces/` | Single `DbSet<SecurityBootstrapState>` |
| `IBootstrapService` | `PTKD.Application/Security/Bootstrap/Interfaces/` | Single `ExecuteAsync` method |
| `BootstrapService` | `PTKD.Application/Security/Bootstrap/Services/` | Reads state, creates user + account, assigns admin group, writes audit, marks bootstrapped |
| `PTKD.Bootstrap` project | `src/backend/PTKD.Bootstrap/` | Console project; reads env vars; calls IBootstrapService |
| Integration tests | `PTKD.IntegrationTests` | Runs-once-only, audit written, second call rejected, secret not logged |

---

## Security requirements (non-negotiable)

All Phase F implementation must comply with:

- **SEC-001:** Audit records are append-only. No UPDATE or DELETE path exists in the audit writer.
- **SEC-002 / DEC-1B-015:** Audit record includes actor, entity, company scope, action code, changed fields (before/after), reason, correlation ID, timestamp.
- **SEC-005:** Audit JSON must not contain passwords, password hashes, tokens, signing keys, file bytes, or permanent signed URLs. The `SqlAuditWriter` must sanitize JSON before insert.
- **DEC-1B-010:** Bootstrap runs once only. Second invocation is rejected with `AUTH_BOOTSTRAP_DISABLED` (409). Bootstrap never prints the password or any secret. `must_change_password = 1` is set.
- **GOV-007/GOV-008:** All material permission changes require immutable audit records. No user may erase audit history.

---

## Migration impact

No migration required. V0003 already provides all required tables, constraints, triggers, database role, and seed data for Phase F.

Next migration (V0004/U0004) is NOT part of Phase F.

---

## Deferred items

- `SECURITY_AUDIT_VIEW` endpoint enforcement — deferred, no timeline
- Read-path query service for Security_Audit_Events — deferred
- Audit retention/archival — deferred per DEC-1B-017
- Production deployment of PTKD.Bootstrap command and PTKD_Security_Audit_Runtime role membership — infrastructure/DBA action, outside Phase F code scope
- Expansion of PermissionCodes.cs constants to cover the full V0003 15-permission catalog — requires separate authorized slice

---

## Explicit non-authorization

This plan does NOT authorize:
- Implementation of any code
- Migration creation
- Production seed data
- Deployment
- Tag or push

This plan documents proposed decisions for Project Owner review. Implementation may begin only after Project Owner accepts OD-F-01 through OD-F-12 (or explicitly modifies them).

## Recommended next step

Project Owner reviews OD-F-01 through OD-F-12. Accepts, rejects, or modifies each. After acceptance, implementation of Phase F-A (Audit Writer) may begin under a new authorized task.
