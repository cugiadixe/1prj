# Phase 1B.1 Authentication & Authorization Implementation Plan

Document status:
PHASE 1B.1-A ACCEPTED BY PROJECT OWNER

Phase 1B.1 implementation:
SLICE A ACCEPTED; SLICES B THROUGH I NOT AUTHORIZED

## 1. Purpose and authorization boundary
This document provides the detailed technical implementation plan for Phase 1B.1, covering the database, backend APIs, and frontend structure for Authentication and Authorization. The Phase 1B.1-A database foundation is the accepted baseline based on corrective commit `efcf950b9c9679a1d6a44198ae3566fe93205a59`. Phase 1B.1-B through I remain strictly **NOT AUTHORIZED** until explicitly approved by the Project Owner.

## 2. Verified Phase 1B.0 decision baseline
This plan strictly enforces the conditions approved by the Project Owner in Phase 1B.0, covering 20 active decisions (with DEC-1B-008 merged into DEC-1B-007, resulting in 21 identifiers in total):
- **Identity:** Separate `User_Auth_Accounts` table. (DEC-1B-001)
- **Passwords:** ASP.NET Core PasswordHasher, Min 8, max 64. Temporary password 24h. History 5. No invented character-class requirement. (DEC-1B-002)
- **Tokens:** Access 15 minutes. Refresh 7 days. Clock skew 30 seconds. (DEC-1B-003)
- **Lockout:** 5 failures, 15-minute lockout. Configuration driven. No account enumeration. (DEC-1B-004)
- **Refresh tokens:** Opaque random refresh secrets. Hash storage only. Atomic single-use rotation. Reuse revokes token family. No server-side grace period. Client single-flight behavior described without assuming an unverified frontend library. (DEC-1B-005)
- **Permissions:** `permission_code` VARCHAR(100) natural primary key. Immutable repository-controlled codes. GLOBAL and COMPANY in Phase 1B. ENTITY deferred. (DEC-1B-006)
- **Admin:** Explicit roles and Admin Groups. GLOBAL requires company_id NULL. COMPANY requires company_id NOT NULL. No hardcoded SUPER_ADMIN bypass. (DEC-1B-007, DEC-1B-009)
- **Merged:** DEC-1B-008 merged into DEC-1B-007.
- **First-admin bootstrap:** Protected bootstrap secret input. Secret is never printed or logged. One-time marker. No API-startup bootstrap. (DEC-1B-010)
- **Cache failure:** Fail closed. Sanitized 503 for authorization-infrastructure failure. No knowingly stale authorization result. (DEC-1B-011)
- **Company context:** Missing/malformed `X-Company-Id` returns 400. Unauthorized company returns 403. No write fallback. JWT does not authorize company access. (DEC-1B-012)
- **Employment status:** `account_status` ACTIVE. `employment_status` ACTIVE or PROBATION. Security/session state invalidated after status change. (DEC-1B-013)
- **Temporal locking:** Effective dates plus lifecycle state. SERIALIZABLE and UPDLOCK/HOLDLOCK. SQL error 1205 maximum three attempts total (initial attempt plus at most two retries) with bounded jitter. (DEC-1B-014)
- **Audit database:** Runtime INSERT/SELECT only for audit. UPDATE/DELETE/TRUNCATE blocked. No cascade delete. No secret/token content. Privileged sysadmin limitation documented. (DEC-1B-015)
- **Codes:** Exact fine-grained permission codes. No broad ADMIN replacement. (DEC-1B-016)
- **Archive:** No purge or archive implementation. No audit deletion in Phase 1B. Monitoring and later retention decision. (DEC-1B-017)
- **Topology:** Same-site deployment assumption. Access token in memory. Refresh cookie HttpOnly, Secure, SameSite=Strict. CSRF controls on cookie-authenticated endpoints. Reopen decision if deployment becomes cross-site. (DEC-1B-018)
- **Signing key:** Development user-secrets. Provider abstraction. RS256, RSA at least 2048. `kid`. 90-day rotation. Previous-key validation proposal: 20 minutes. Production provider remains deployment-specific. (DEC-1B-019)
- **Lockout status:** HTTP 403. Generic, non-enumerating observable response. (DEC-1B-020)
- **Audit View:** Distinct `SECURITY_AUDIT_VIEW`. (DEC-1B-021)

## 3. In-scope behavior
- Schema migrations for all auth and security tables (V0003) and rollback (U0003).
- JWT generation, validation, and single-flight opaque refresh endpoints.
- Role, Admin Group, and individual permission assignment management APIs.
- Mandatory `X-Company-Id` header enforcement (HTTP 400/403).
- Fail-closed database authorization caching (HTTP 503).
- Database-level temporal overlap prevention for assignments.
- Frontend login, refresh interceptor, and basic protected routing.

## 4. Explicitly out-of-scope behavior
- Business features (customer management, document workflows).
- Single Sign-On (SSO) or external OAuth providers.
- `ENTITY` scope authorization logic.
- Audit data archiving or automated purging.
- Azure Key Vault automated provisioning.
- Production environment deployments.
- AD/LDAP implementation (extensibility only).

## 5. Current repository readiness assessment
- **Backend target framework:** net10.0
- **Solution and project names:** `PTKD-ERP.sln`, `PTKD.Api`, `PTKD.Application`, `PTKD.Domain`, `PTKD.Infrastructure`, `PTKD.DbMigrator`
- **Architectural pattern:** Modular Monolith mapped into Vertical Slices. No MediatR. Controllers delegate directly to dedicated Application Services.
- **API project and route conventions:** `/api/v2` namespace.
- **Database migration and rollback folders:** `database/migrations` and `database/rollbacks`.
- **SchemaVersions behavior:** Roundhouse/DbUp style `SchemaVersions` tracking table.
- **Frontend framework:** React 19.2.7 (Vite).
- **Frontend HTTP client:** axios 1.18.1.
- **Frontend router and state-management libraries:** `react-router-dom` 7.18.1, `@tanstack/react-query` 5.101.2.
- **Backend test projects:** `PTKD.UnitTests`, `PTKD.IntegrationTests`, `PTKD.ApiTests`.
- **Backend test standard versus actual:**
  - Approved standard: xUnit, NSubstitute, WebApplicationFactory
  - Actual `PTKD.UnitTests` state: xUnit 2.9.3 and Moq 4.20.72.
  - Actual `PTKD.ApiTests` state: xUnit 2.9.3, NSubstitute 6.0.0 and WebApplicationFactory. NSubstitute is not absent from the repository.
  - Actual `PTKD.IntegrationTests` state: xUnit 2.9.3 with real SQL Server behavior; no mocking framework is used for the security schema tests.
  - Repository drift remains a separate decision: the approved testing standard is unchanged, and Slice 1B.1-A neither replaced Moq nor added or removed a package.
- **Frontend test tools:** vitest, @testing-library/react.
- **Playwright setup:** Approved tool. SEPARATE PACKAGE INSTALLATION AUTHORIZATION REQUIRED.
- **Database test-fixture conventions:** Test containers / SQL Server isolation.
- **Protected test database name:** `PTKD_TEST_PHASE1A2`
- **Existing retry and transaction infrastructure:** Phase 1A.2 already contains explicit Serializable transactions, `IOrganizationDbContextFactory`, a protected database fixture using `PTKD_TEST_PHASE1A2`, `DeadlockRetryPolicy`, and SQL error 1205 retry handling.
- **Existing audit/interceptor infrastructure:** Phase 1A.2 contains `AppendOnlyInterceptor` for `Employment_Histories`. Note: Existing append-only application interception is not the same as the new `Security_Audit_Events` database enforcement. V0003 may create database controls and roles. An EF interceptor for `Security_Audit_Events`, if later required, belongs to a later application slice, not inside V0003.

## 6. Proposed implementation sequence
Implementation will proceed sequentially through Slices A to I. Each slice represents a cohesive, testable block of functionality. Every slice requires separate Project Owner authorization. Slices must be executed in this exact dependency order.

## 7. Database migration and rollback design
**V0003:**
- Creates `User_Auth_Accounts`, `Password_History`, `Refresh_Tokens`, `Permissions`, `Roles`, `Role_Permissions`, `Department_Permissions`, `Admin_Groups`, `Admin_Group_Permissions`, `User_Admin_Group_Assignments`, `User_Role_Assignments`, `User_Individual_Permissions`.
- Creates `Authorization_Policy_State`, `Security_Bootstrap_State`, and append-only `Security_Audit_Events`.
- Enforces strict foreign keys, bigint PKs, natural `permission_code`, and `UNIQUE` constraints.
- Adds an `INSTEAD OF UPDATE, DELETE` trigger for `Security_Audit_Events`; the runtime audit role receives `DENY ALTER`, which prevents `TRUNCATE` for that role. A DML trigger does not intercept `TRUNCATE`.
- Applies multi-row-safe half-open overlap triggers with `UPDLOCK`/`HOLDLOCK`; filtered unique indexes remain defense-in-depth for duplicate current ACTIVE rows and are not complete range-overlap controls.
- Seeds exactly the 15 approved Organization and Security permission codes and creates no SQL audit view.

**U0003:**
- Refuses unsafe rollback if `V0004` or higher exists.
- Requires the exact database `PTKD_TEST_PHASE1A2`. This test authorization does not imply Production execution authorization.
- Removes dependent constraints before tables.
- Drops all tables, triggers, and types created in V0003.
- Restores SchemaVersions correctly.
- Refuses destructive rollback when any protected security, bootstrap, policy, assignment, role, Admin Group, token, password-history, permission-catalog or security-audit state is material or non-pristine.

## 8. Domain and application design
- **Domain:** Entities for `UserAuthAccount`, `Role`, `AdminGroup`, `Permission`.
- **Application:** Command and Query Application Services mapped via Vertical Slices. Explicit input validation via FluentValidation.

## 9. Infrastructure and persistence design
- EF Core is the default for ordinary CRUD.
- Dapper or explicit SQL/stored procedures are approved options for complex or security-sensitive transactional paths involving `UPDLOCK`/`HOLDLOCK`. Dapper is not automatically required for Slice 1B.1-A. Slice 1B.1-A must use SQL migration scripts and the existing `PTKD.DbMigrator`.
- Append-only SQL business/security audit.
- Serilog for technical logging.

## 10. Authentication API design
- `POST /api/v2/auth/login`: Accepts credentials, returns JWT in payload, refresh token in `HttpOnly` `SameSite=Strict` cookie.
- `POST /api/v2/auth/refresh`: Accepts cookie, performs atomic family rotation, returns new JWT and new cookie. Validates CSRF.
- `POST /api/v2/auth/logout`: Revokes family, clears cookies.
- Responses strictly return 403 on lockout/invalid credentials (non-enumerating).

## 11. Refresh-token and session lifecycle
- Refresh tokens are securely generated 256-bit opaque strings.
- Only SHA256 hashes are stored in the database.
- A family identifier links rotated tokens.
- Using an already-used token instantly revokes the entire family.
- No grace periods for concurrent requests; client handles single-flight.

## 12. Authorization evaluation engine
- Evaluates `X-Company-Id` header against the JWT subject's actual assigned scopes in the database.
- Hierarchy: Admin Group -> Explicit Deny -> Individual Grant -> Role Grant.
- Server-side permission and hard-rule enforcement.
- Individual DENY precedence.
- Company context revalidation for protected requests.

## 13. Current-company context handling
- Middleware extracts `X-Company-Id`.
- Missing/invalid header -> HTTP 400.
- Unauthorized for the specific company -> HTTP 403.
- No fallback to primary company for write endpoints.

## 14. Role, permission and Admin Group management
- Strict validation that `GLOBAL` roles enforce `company_id IS NULL` and `COMPANY` roles enforce `company_id IS NOT NULL`.
- Same rules apply to Admin Groups.
- Only users with `SECURITY_ROLE_MANAGE` or `SECURITY_ADMIN_GROUP_MANAGE` can modify these.

## 15. First-admin bootstrap design
- Isolated CLI command (`PTKD.DbMigrator` or dedicated tool).
- Protected bootstrap secret input. Secret is never printed or logged.
- Grants `GLOBAL` Admin Group to a specified identity.
- Sets a one-time execution marker in the database to prevent replay.

## 16. Security audit design
- Dedicated table: `Security_Audit_Events`.
- Records `actor_user_id`, acting-as identity, `target_user_id`, stable `event_code`, entity/company scope, reason, selected `before`/`after` states, request metadata, outcome, policy version, correlation ID, and time (with application payloads scrubbed of secrets).
- Protected by database role permissions plus an `INSTEAD OF UPDATE, DELETE` defense-in-depth trigger. `DENY ALTER` prevents ordinary runtime-role `TRUNCATE`; db_owner and sysadmin remain outside this boundary.
- Future security-audit API access is gated by the distinct application permission code `SECURITY_AUDIT_VIEW`; no SQL view named `vw_SECURITY_AUDIT_VIEW` is created.

## 17. Signing-key provider abstraction
- Abstraction `IJwtSigningKeyProvider`.
- Default local implementation uses `dotnet user-secrets`.
- Keys are RS256, minimum 2048 bits, with `kid`.
- 90-day rotation. Previous-key validation proposal: 20 minutes.

## 18. Frontend authentication/session design
- Axios interceptor to automatically catch 401, trigger single-flight refresh to `/api/v2/auth/refresh` (passing CSRF), and retry.
- Memory-only storage for the access token.

## 19. Frontend authorization and admin screens
- Route guards checking `effective_permissions`.
- Pages for managing Roles, Admin Groups, and Security logs.

## 20. Error and ProblemDetails catalog
- **400:** malformed or invalid request input.
- **401:** missing, invalid or expired authentication.
- **403:** permission/scope denial and approved locked-account behavior.
- **404:** resource not found.
- **409:** stale rowversion, uniqueness, temporal or state conflict (including stale valid rowversion).
- **500:** sanitized unexpected/unmapped server or database failure.
- **503:** authorization infrastructure unavailable or retry exhaustion.
- Every error must use sanitized ProblemDetails and must not disclose account existence, password validity details, raw tokens, signing key information, or database internals.

## 21. Testing strategy
- Traceability mapping to DEC-1B rules.
- Warning-as-error build validation.
- Migration (V0003) and rollback (U0003) tests.
- Database-name guard before writes.
- Exactly-once migration behavior tests.
- Password hashing and history verification.
- Temporary-password expiry testing.
- Password reset session revocation.
- Lockout and non-enumerating responses.
- Token expiry and 30-second skew.
- Atomic refresh rotation.
- Family revocation on token reuse.
- Losing concurrent refresh behavior.
- Effective permission union.
- Individual DENY precedence.
- Hard-rule precedence.
- Company assignment and cross-company denial.
- Missing-header 400 and unauthorized-company 403.
- Stale cache and infrastructure 503.
- Temporal overlap and deadlock retry (1205).
- Audit append-only enforcement.
- Audit secret exclusion.
- Bootstrap one-time behavior.
- Cookie attributes and CSRF.
- Signing kid and old-key overlap.
- `SECURITY_AUDIT_VIEW` application-permission isolation, with no same-named SQL view.
- Complete Phase 1A.2 regression suite execution.

## 22. Security verification strategy
- Assertions ensuring passwords are computationally hashed.
- Assertions ensuring SQL injection protection.
- Assertions confirming database trigger blocks DELETE queries.

## 23. Deployment and configuration strategy
- User Secrets or local protected configuration for Development.
- Provider abstraction for Production secrets.

## 24. Rollback and recovery strategy
- `U0003` script guarantees clean rollback of schema and data (if unpopulated).
- Code changes must be fully reverted via Git commit rollback.

## 25. Implementation slices and commit boundaries

**Phase 1B.1-A: Database security foundation and rollback design.**
- *Prerequisites:* Phase 1B.1 authorization.
- *Files:* `database/migrations/V0003__create_security_schema.sql`, `database/rollbacks/U0003__drop_security_schema.sql`.
- *Impact:* Creates all auth and audit tables, triggers, and overlap indexes.
- *Completion:* V0003 and U0003 were verified against `PTKD_TEST_PHASE1A2` and accepted by the Project Owner as the Phase 1B.1-A baseline.
- *Acceptance:* Đào Hải Bách, Project Owner; direct written authorization dated 2026-07-16; corrective commit `efcf950b9c9679a1d6a44198ae3566fe93205a59` with parent `9d313a343fe2b2ccf29379b3a920bab9de4b5a0d`.
- *Separate authorization required:* YES — satisfied for Phase 1B.1-A only.

**Phase 1B.1-B: Authentication account domain and password lifecycle.**
- *Prerequisites:* Phase 1B.1-A complete.
- *Files:* Auth domain entities, PasswordHasher logic.
- *Impact:* Business logic for passwords, lockouts, temporary passwords.
- *Completion:* Unit tests pass.
- *Separate authorization required:* YES.

**Phase 1B.1-C: Access and refresh token lifecycle.**
- *Prerequisites:* Phase 1B.1-B complete.
- *Files:* JWT generation logic, endpoints, single-flight refresh logic.
- *Impact:* Issues tokens.
- *Completion:* Token rotation and family revocation integration tests pass.
- *Separate authorization required:* YES.

**Phase 1B.1-D: Permission, Role, Department and Admin Group evaluation.**
- *Prerequisites:* Phase 1B.1-A complete.
- *Files:* Permission natural codes, assignment logic, evaluation union engine.
- *Impact:* API endpoints for role management.
- *Completion:* DENY precedence and hierarchical rule tests pass.
- *Separate authorization required:* YES.

**Phase 1B.1-E: Company context and protected endpoint enforcement.**
- *Prerequisites:* Phase 1B.1-D complete.
- *Files:* Middleware to enforce `X-Company-Id` header.
- *Impact:* Middleware blocks requests returning 400/403.
- *Completion:* Integration tests for missing headers and unauthorized scope pass.
- *Separate authorization required:* YES.

**Phase 1B.1-F: Security audit and one-time bootstrap.**
- *Prerequisites:* Phase 1B.1-A complete.
- *Files:* Audit logging interceptor, DB migrator bootstrap script.
- *Impact:* Append-only guarantees, admin creation.
- *Completion:* E2E test of bootstrap one-time marker.
- *Separate authorization required:* YES.

**Phase 1B.1-G: Frontend authentication/session behavior.**
- *Prerequisites:* Phase 1B.1-C complete.
- *Files:* React auth context, axios interceptor.
- *Impact:* Web UI login flows.
- *Completion:* Frontend unit tests pass.
- *Separate authorization required:* YES.

**Phase 1B.1-H: Security administration UI.**
- *Prerequisites:* Phase 1B.1-G complete.
- *Files:* React pages for role management.
- *Impact:* Admin screens.
- *Completion:* UI state tests pass.
- *Separate authorization required:* YES.

**Phase 1B.1-I: Full verification and closure evidence.**
- *Prerequisites:* All previous slices.
- *Files:* Test runners.
- *Impact:* None (read-only verification).
- *Completion:* Phase 1A.2 regression passes, full traceability matrix validated.
- *Separate authorization required:* YES.

## 26. Entry and exit gates
- **Entry Gate:** Project Owner signs this planning document to Authorize Implementation.
- **Exit Gate:** All Slices complete, 100% tests pass, U0003 tested successfully, final security read-only audit.

## 27. Risks and mitigations
- *Risk:* 1205 deadlocks on temporal assignments. *Mitigation:* Explicit jitter retries (max 3 total attempts) and application-layer serialization.
- *Risk:* Frontend refresh loops. *Mitigation:* Strict single-flight locking implemented in Axios interceptor.

## 28. Remaining Production-only prerequisites
- Select, provision, and test a real production secret provider (e.g., Azure Key Vault).
- DBA review of `INSTEAD OF` triggers and database runtime permissions.
- Independent expert review for DEC-1B-015 and DEC-1B-019.
- Same-site HTTPS topology confirmed by infrastructure.
- Monitor audit table size and define an operational warning threshold before compliance data becomes material.

## 29. Exact files expected to be created or modified
- PROPOSED NEW FILE: `database/migrations/V0003__create_security_schema.sql`
- PROPOSED NEW FILE: `database/rollbacks/U0003__drop_security_schema.sql`
- PROPOSED NEW FILE: `src/backend/PTKD.Domain/Entities/UserAuthAccount.cs`
- PROPOSED NEW FILE: `src/backend/PTKD.Domain/Entities/Role.cs`
- PROPOSED NEW FILE: `src/backend/PTKD.Domain/Entities/AdminGroup.cs`
- PROPOSED NEW FILE: `src/backend/PTKD.Domain/Entities/SecurityAuditEvent.cs`
- PROPOSED NEW FILE: `src/backend/PTKD.Application/Security/` (Various Application Services)
- PROPOSED NEW FILE: `src/backend/PTKD.Api/Controllers/AuthController.cs`
- PROPOSED NEW FILE: `src/backend/PTKD.Api/Controllers/SecurityController.cs`
- Existing File: `src/backend/PTKD.Api/Program.cs` (to wire up Auth)
- Existing File: `src/frontend/src/App.tsx` (to add routes)
- PROPOSED NEW FILE: `src/frontend/src/features/auth/` (React components and axios interceptor)
- Existing File: `src/frontend/package.json` (for adding dependencies if authorized)

**SEPARATE PACKAGE AUTHORIZATION REQUIRED:**
- `@playwright/test` (for e2e tests)
- `Dapper` (if explicit SQL transaction locks are chosen over EF Raw SQL in a future slice)

## 30. Project Owner implementation authorization section
- **Project Owner result:** Phase 1B.1-A **ACCEPTED BY PROJECT OWNER**; Phase 1B.1-B through I **NOT AUTHORIZED**.
- **Project Owner conditions:** Acceptance applies only to the verified Phase 1B.1-A baseline. It does not authorize later slices, Production migration, source/API/JWT/frontend changes, packages, tagging, or pushing.
- **Project Owner name:** Đào Hải Bách.
- **Authorization date:** 2026-07-16.
- **Reference:** Direct written authorization accepting corrective commit `efcf950b9c9679a1d6a44198ae3566fe93205a59` with parent `9d313a343fe2b2ccf29379b3a920bab9de4b5a0d`.
