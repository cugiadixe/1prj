# Phase 1B.0 Project Owner Decision Package

## Consolidated Recommendation Summary

| Decision ID | Recommended result | Exact recommended option | Strength | Blocking | Implementation conditions present | Production conditions present | Project Owner field blank | Status OPEN |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| DEC-1B-001 | APPROVE WITH SPECIFIED CONDITIONS | Separate `User_Auth_Accounts` table | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-002 | APPROVE WITH SPECIFIED CONDITIONS | Canonical password policy (Config-driven) | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-003 | APPROVE WITH SPECIFIED CONDITIONS | 15m Access / 7d Refresh / 30s skew | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-004 | APPROVE WITH SPECIFIED CONDITIONS | 5 failures / 15m lockout | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-005 | APPROVE WITH SPECIFIED CONDITIONS | Strict single-use rotation, no grace period | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-006 | APPROVE WITH SPECIFIED CONDITIONS | Natural `permission_code` PK; ENTITY deferred | MEDIUM | Yes | Yes | No | No | APPROVED |
| DEC-1B-007 | APPROVE WITH SPECIFIED CONDITIONS | Strict bounds validation | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-009 | APPROVE WITH SPECIFIED CONDITIONS | Distinct explicit assignment mapping | MEDIUM | Yes | Yes | No | No | APPROVED |
| DEC-1B-010 | APPROVE WITH SPECIFIED CONDITIONS | Protected secret input; never print secret | HIGH | Yes | Yes | Yes | No | APPROVED |
| DEC-1B-011 | APPROVE WITH SPECIFIED CONDITIONS | Fail closed with sanitized 503 | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-012 | APPROVE WITH SPECIFIED CONDITIONS | HTTP 400, no fallback | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-013 | APPROVE WITH SPECIFIED CONDITIONS | Require ACTIVE/PROBATION | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-014 | APPROVE WITH SPECIFIED CONDITIONS | Dates plus lifecycle state | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-015 | APPROVE WITH SPECIFIED CONDITIONS | Trigger append-only | MEDIUM | Yes | Yes | Yes | No | APPROVED |
| DEC-1B-016 | APPROVE WITH SPECIFIED CONDITIONS | Fine-grained string constants | HIGH | Yes | Yes | No | No | APPROVED |
| DEC-1B-017 | DEFER AS NON-BLOCKING | Defer archiving implementation | HIGH | No | Yes | Yes | No | DEFERRED |
| DEC-1B-018 | APPROVE WITH SPECIFIED CONDITIONS | Same-site, SameSite=Strict, CSRF controls | MEDIUM | Yes | Yes | Yes | No | APPROVED |
| DEC-1B-019 | APPROVE WITH SPECIFIED CONDITIONS | Provider abstraction; 20-min overlap | MEDIUM | Yes | Yes | Yes | No | APPROVED |
| DEC-1B-020 | APPROVE WITH SPECIFIED CONDITIONS | HTTP 403 Forbidden | MEDIUM | Yes | Yes | No | No | APPROVED |
| DEC-1B-021 | APPROVE WITH SPECIFIED CONDITIONS | Distinct `SECURITY_AUDIT_VIEW` | HIGH | Yes | Yes | Yes | No | APPROVED |

## Cross-Decision Consistency Matrix

| Related decisions | Potential conflict | Recommended resolution | Remaining Project Owner choice | Blocks implementation |
| :--- | :--- | :--- | :--- | :--- |
| DEC-1B-001 vs 002 | Authentication identity vs password policy | Apply policy only to local auth accounts, ignore for external providers. | Accept or reject policy boundary. | Yes |
| DEC-1B-002 vs 005 | Password reset vs session revocation | Password reset must revoke all active refresh token families for the user. | Confirm revocation linkage. | Yes |
| DEC-1B-004 vs 020 | Lockout behavior vs HTTP status | Lockout trigger must map cleanly to HTTP 403 without leaking account existence. | Choose HTTP 403 vs 423. | Yes |
| DEC-1B-005 | Refresh rotation vs concurrent refresh | Require single-flight refresh on client. No server-side grace period permitted. | Accept strict single-use vs UX friction. | Yes |
| DEC-1B-006 vs 007 | Permission data_scope vs role scope | Enforce Role scope to equal or be narrower than assigned Permission scopes. | Confirm scope validation strictness. | Yes |
| DEC-1B-006 | ENTITY scope vs Phase 1B boundaries | Exclude ENTITY scope explicitly from Phase 1B.1 to limit complexity. | Include or exclude ENTITY scope. | Yes |
| DEC-1B-012 vs 003 | Company header vs JWT claims | Do not embed Company-Id in JWT; rely entirely on HTTP header. | Choose strict HTTP 400 header requirement. | Yes |
| DEC-1B-013 vs 005 | Employment status vs session invalidation | HR termination must trigger immediate token family revocation. | Accept cross-domain sync requirement. | Yes |
| DEC-1B-014 | Temporal assignments vs locking | Use dates plus lifecycle state (`assignment_status`). | Approve source of truth and deadlock strategy. | Yes |
| DEC-1B-017 vs 015 | Audit immutability vs retention | Prevent deletion via trigger; defer archiving strategy to a separate phase. | Deferral remains non-blocking? | No |
| DEC-1B-016 vs 021 | Permission codes vs audit-view permission | Distinct `SECURITY_AUDIT_VIEW` separates security logs from business audit. | Approve distinct boundary. | Yes |
| DEC-1B-018 | Client topology vs SameSite and CSRF | Assume same-site API topology with `SameSite=Strict`. Explicit CSRF tokens recommended on cookie endpoints. | Accept topology restriction. | Yes |
| DEC-1B-019 vs 003 | Signing key rotation vs token lifetime | Ensure previous key validation covers 20 minutes to cover token lifetime and skew. | Approve key rotation overlap window. | Yes |
| DEC-1B-010 vs 015 | Bootstrap secret delivery vs audit exclusions | Log bootstrap action securely but exclude raw credentials from `SecurityAuditLog`. | Accept CLI secure delivery constraint. | Yes |

## DEC-1B-001 — Login identifier
- **Decision ID:** DEC-1B-001
- **Topic:** Login identifier
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Separate `User_Auth_Accounts` table. Columns: `provider_type`, `provider_subject` (unique). Password hash nullable for external providers.
- **Actual unresolved question:** Use separate table vs add to Users.
- **Antigravity technical recommendation:** Separate `User_Auth_Accounts` from `Users`. Use a `bigint` surrogate primary key. Implement FK to `Users`. Enforce `UNIQUE(provider_type, provider_subject)`. No cascade delete from `Users`. Password hash nullable for external providers.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Standard separation of identity logic. Supported by existing Phase 1A architecture principles.
- **Business consequences:** Required for seamless future SSO integration.
- **Database consequences:** Requires joining `User_Auth_Accounts` to `Users`.
- **API consequences:** Auth endpoints operate strictly on `User_Auth_Accounts`.
- **Frontend consequences:** None.
- **Security consequences:** Credential data isolated from business APIs.
- **Operational consequences:** Deletions require coordinated transactional updates.
- **Test consequences:** Requires mocked auth accounts in integration tests.
- **Alternatives considered:** Adding login columns directly to `Users`.
- **Why the recommended option is preferred:** Safest for future expansion and separation of concerns.
- **Residual risk after adopting the recommendation:** Join overhead on authentication paths.
- **Risk that requires Project Owner acceptance:** Schema complexity.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Schema must enforce bigint PK, FK without cascade delete, and UNIQUE constraint.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-002 — Password policy
- **Decision ID:** DEC-1B-002
- **Topic:** Password policy
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Minimum length 8. Maximum length 64. Temporary password lifetime 24 hours. No reuse of last 5. Require change on first login. Password must not contain login name. Password reset revokes active sessions.
- **Actual unresolved question:** Approve strict password requirements versus default Identity policy.
- **Antigravity technical recommendation:** Use the canonical proposal exactly. ASP.NET Core PasswordHasher. Min 8, Max 64. 24h temp lifetime. No reuse of last 5. No provider_subject/login name. Reset revokes active sessions. Lockout by DEC-1B-004. Make parameters configuration-driven.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Standard corporate security requirements.
- **Business consequences:** Increased support calls for forgotten passwords.
- **Database consequences:** Requires `PasswordHistory` table.
- **API consequences:** Password reset endpoints must enforce the history and complexity constraints.
- **Frontend consequences:** UI must display password complexity feedback.
- **Security consequences:** Protects against trivial brute-force.
- **Operational consequences:** Routine maintenance of password history.
- **Test consequences:** Integration tests for policy rejection scenarios.
- **Alternatives considered:** Rejected / unacceptable security alternative: plaintext password storage, or default Identity policy without history.
- **Why the recommended option is preferred:** Enforces organizational baseline security without inventing undocumented rules.
- **Residual risk after adopting the recommendation:** Policy weaker than future needs.
- **Risk that requires Project Owner acceptance:** Account lockout abuse potential.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** All numeric parameters must be driven by configuration, not hardcoded. Must invoke session revocation on password reset.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-003 — Token lifetimes
- **Decision ID:** DEC-1B-003
- **Topic:** Token lifetimes
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Access token: 15m. Refresh token: 7d. Zero clock skew.
- **Actual unresolved question:** Token lifetimes and skew settings.
- **Antigravity technical recommendation:** Access token: 15 minutes. Refresh token: 7 days. JWT clock skew: 30 seconds. Zero clock skew increases operational failure risk when clocks differ. Thirty seconds is a small validation tolerance and does not provide replay protection.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Best practices for stateless JWT lifetimes and distributed clock synchronization.
- **Business consequences:** Requires users to login at least every 7 days.
- **Database consequences:** None.
- **API consequences:** Strict validation of `exp` / `nbf`.
- **Frontend consequences:** Must silently handle 401s and refresh via interceptor.
- **Security consequences:** Limits blast radius of stolen access tokens.
- **Operational consequences:** High volume of refresh requests to the authentication server.
- **Test consequences:** Requires mocking clock in tests.
- **Alternatives considered:** Longer access tokens (e.g., 24h).
- **Why the recommended option is preferred:** Immediate session revocation relies on short access token lifetimes.
- **Residual risk after adopting the recommendation:** Refresh load on the server.
- **Risk that requires Project Owner acceptance:** 30s validation tolerance.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Token lifetimes and 30s skew must be configurable defaults.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-004 — Lockout values
- **Decision ID:** DEC-1B-004
- **Topic:** Lockout values
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** 5 failures = 15m lockout (config driven).
- **Actual unresolved question:** Threshold and duration.
- **Antigravity technical recommendation:** 5 failed login attempts. 15-minute lockout. Configuration-driven. Successful authentication resets failed-attempt tracking. Unlock and reset operations are audited. Responses must not reveal whether a username exists.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Standard configuration for enterprise lockouts.
- **Business consequences:** Users may be denied access during high-stress periods.
- **Database consequences:** Requires tracking failed attempts on `User_Auth_Accounts`.
- **API consequences:** Lockout response returned instead of standard unauthorized.
- **Frontend consequences:** Display lockout message to user.
- **Security consequences:** Mitigates brute-force guessing.
- **Operational consequences:** Helpdesk overhead for manual unlocks.
- **Test consequences:** Integration tests required for 5th failure lockout.
- **Alternatives considered:** No lockout, only rate limiting.
- **Why the recommended option is preferred:** Directly stops targeted account guessing.
- **Residual risk after adopting the recommendation:** Targeted DoS against specific user accounts.
- **Risk that requires Project Owner acceptance:** Account-denial risk.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Lockout behavior must be fully configuration-driven and emit audit logs without leaking account existence to clients.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-005 — Refresh token rotation
- **Decision ID:** DEC-1B-005
- **Topic:** Refresh token rotation
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Token-family column tracks chain. Reuse detection revokes family.
- **Actual unresolved question:** Deterministic concurrent-refresh behavior and token-family reuse handling.
- **Antigravity technical recommendation:** Refresh tokens are random opaque secrets. Store only token hashes. Every refresh token is single-use. Rotation is atomic in one database transaction. Reuse of a successfully rotated token revokes the whole family. Frontend must use a single-flight refresh mechanism so only one refresh request is issued at a time. A concurrent losing request receives a sanitized authentication failure. No raw refresh token is logged or stored. Do not permit the same refresh token to succeed repeatedly during a server-side grace period.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** IETF BCP for OAuth 2.0 Browser-Based Apps recommends single-use refresh tokens with rotation and strict revocation.
- **Business consequences:** Transparent session management when successful.
- **Database consequences:** Requires storing token hashes and family IDs.
- **API consequences:** Strict atomic updates on refresh endpoint.
- **Frontend consequences:** Must implement single-flight request concurrency lock on refresh.
- **Security consequences:** Mitigates stolen token replay.
- **Operational consequences:** Requires database cleanup jobs for expired tokens.
- **Test consequences:** Concurrency tests required to ensure strict failure on reuse.
- **Alternatives considered:** Design alternative: Hashed, non-rotating refresh tokens. Rejected: 30s grace period.
- **Why the recommended option is preferred:** Strict single-use guarantees immediate detection of theft.
- **Residual risk after adopting the recommendation:** Race conditions from poorly implemented clients force legitimate users to re-login.
- **Risk that requires Project Owner acceptance:** Legitimate concurrent requests failing.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Strict single-use family revocation in atomic transaction; no server-side grace period.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-006 — Permission schema
- **Decision ID:** DEC-1B-006
- **Topic:** Permission schema
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Data-driven (`Permissions` table). Natural PK (e.g. `USER_VIEW`). Standard data_scope (`GLOBAL`, `COMPANY`, `ENTITY`).
- **Actual unresolved question:** Whether ENTITY scope is excluded from Phase 1B; natural key versus surrogate key.
- **Antigravity technical recommendation:** Preserve canonical natural-key proposal: `permission_code` VARCHAR(100) is the PK, immutable after release. Admin cannot invent codes. Only GLOBAL and COMPANY scopes are implemented in Phase 1B. ENTITY explicitly deferred. Role and assignment tables reference `permission_code`. Surrogate UUIDs are inappropriate because permission codes are a small, controlled, immutable catalog used directly by app policies.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** Standard application RBAC design when codes map directly to application logic.
- **Business consequences:** Row-level permissions deferred to future phases.
- **Database consequences:** VARCHAR PKs on `Permissions` table.
- **API consequences:** Policy constants map directly to DB PKs.
- **Frontend consequences:** None.
- **Security consequences:** Hardcoded policies are intrinsically tied to database assignments.
- **Operational consequences:** None.
- **Test consequences:** Simplified role authorization tests.
- **Alternatives considered:** Surrogate UUIDs.
- **Why the recommended option is preferred:** Codes act as direct constants across the whole stack.
- **Residual risk after adopting the recommendation:** Complex multi-column foreign keys later when ENTITY scope is introduced.
- **Risk that requires Project Owner acceptance:** Row-level constraints missing in MVP.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** `permission_code` must be VARCHAR(100) PK. Code must prevent admin invention of codes and defer ENTITY logic.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-007 — Role and Admin Groups
- **Decision ID:** DEC-1B-007
- **Topic:** Role and Admin Groups
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Explicit Role + Permission mapping. Merged with DEC-1B-008.
- **Actual unresolved question:** Scope strictness validation.
- **Antigravity technical recommendation:** GLOBAL and COMPANY only. GLOBAL requires `company_id` NULL. COMPANY requires `company_id` NOT NULL. No implicit SUPER_ADMIN bypass. Admin groups and their permissions are explicitly mapped. Hard business rules remain server-side and cannot be bypassed by an admin-group assignment. Invalid scope combinations are rejected at API and database boundaries.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Prevents accidental privilege escalation.
- **Business consequences:** Administrators cannot grant overly broad scopes.
- **Database consequences:** Scope validation required in assignment logic.
- **API consequences:** Rejection of invalid assignments.
- **Frontend consequences:** Form validation required for role creation.
- **Security consequences:** Mitigates cross-company escalation.
- **Operational consequences:** None.
- **Test consequences:** Extensive boundary unit tests required.
- **Alternatives considered:** Soft validation warnings.
- **Why the recommended option is preferred:** Essential for strict multi-tenant boundary integrity.
- **Residual risk after adopting the recommendation:** Incorrect scope escalation if logic is flawed.
- **Risk that requires Project Owner acceptance:** Administrator frustration due to rigid rules.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Database and API must strictly enforce company_id NULL/NOT NULL rules and explicitly validate scopes without hardcoded bypass.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-009 — Admin group model
- **Decision ID:** DEC-1B-009
- **Topic:** Admin group model
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Distinct Admin group assignment overriding standard scopes.
- **Actual unresolved question:** Authorization routing for admins.
- **Antigravity technical recommendation:** GLOBAL and COMPANY only. GLOBAL requires `company_id` NULL. COMPANY requires `company_id` NOT NULL. No implicit SUPER_ADMIN bypass. Admin groups and their permissions are explicitly mapped. Hard business rules remain server-side and cannot be bypassed by an admin-group assignment. Invalid scope combinations are rejected at API and database boundaries.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** Explicit mapping is auditable and reduces bypass vulnerabilities.
- **Business consequences:** Clear tracking of high-privilege users.
- **Database consequences:** Specific table or flags for Admin assignments.
- **API consequences:** Authorization policies must check explicit mappings.
- **Frontend consequences:** None.
- **Security consequences:** Mitigates uncontrolled SUPER_ADMIN bypass of hard invariants.
- **Operational consequences:** Overhead in managing a parallel hierarchy.
- **Test consequences:** Tests must explicitly set admin flags.
- **Alternatives considered:** Rejected / unacceptable security alternative: unrestricted SUPER_ADMIN bypass instead of mapped permissions.
- **Why the recommended option is preferred:** Preserves strict auditability.
- **Residual risk after adopting the recommendation:** Permission duplication.
- **Risk that requires Project Owner acceptance:** Duplicates some assignment effort.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Explicit assignment mapping must be implemented for administrative groups.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-010 — First admin
- **Decision ID:** DEC-1B-010
- **Topic:** First admin
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** CLI tool or secured script execution. No default password.
- **Actual unresolved question:** Secret delivery method.
- **Antigravity technical recommendation:** Separate one-time bootstrap command. Secret supplied through protected interactive input or approved secret provider. The command never echoes the secret. `must_change_password` is set. One-time bootstrap marker prevents repeated execution. Bootstrap action is audited without logging credentials. Does not run during normal API startup.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Avoids hardcoded backdoors and secret leakage in deployment pipelines.
- **Business consequences:** Infrastructure engineer required for first setup.
- **Database consequences:** One-time bootstrap marker required.
- **API consequences:** None.
- **Frontend consequences:** None.
- **Security consequences:** Mitigates insecure default admin credentials.
- **Operational consequences:** Complex setup.
- **Test consequences:** None.
- **Alternatives considered:** Rejected: Print password to terminal or logs.
- **Why the recommended option is preferred:** Strongest security posture for production initialization.
- **Residual risk after adopting the recommendation:** Lost initial bootstrap credential requires manual database recovery.
- **Risk that requires Project Owner acceptance:** Initial deployment overhead.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Implement as an isolated offline CLI/tool, never printing the secret, with audit logging and one-time marker.
- **Conditions required before production:** The execution environment must support protected interactive input or secure secret injection.
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-011 — Cache failure
- **Decision ID:** DEC-1B-011
- **Topic:** Cache failure
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Fail-closed policy enforcement.
- **Actual unresolved question:** Behavior under cache failure.
- **Antigravity technical recommendation:** Fail closed. Revalidate account, session, company assignment and policy version. When the backing authorization state cannot be read, deny the protected operation. Return sanitized HTTP 503 Service Unavailable for infrastructure failure, not a misleading permission-denied response. Do not return cached permissions after their policy version is known to be stale.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Fundamental zero-trust security principle.
- **Business consequences:** Outages result in total lockout rather than unauthorized access.
- **Database consequences:** Database hit fallback.
- **API consequences:** Returns HTTP 403 or 503.
- **Frontend consequences:** Handle unexpected 503 globally.
- **Security consequences:** Reduces unauthorized access during cache failures.
- **Operational consequences:** Cache failure causes DB load spikes.
- **Test consequences:** Test fail-closed under mocked exception.
- **Alternatives considered:** Fail-open (unacceptable).
- **Why the recommended option is preferred:** Security over availability in authorization decisions.
- **Residual risk after adopting the recommendation:** System denial of service.
- **Risk that requires Project Owner acceptance:** Outage risk.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Policy evaluation must strictly deny access and return 503 on infrastructure read failures, aggressively invalidating stale caches.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-012 — Company header
- **Decision ID:** DEC-1B-012
- **Topic:** Company header
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Strict `X-Company-Id` header enforcement.
- **Actual unresolved question:** HTTP 400 versus 403; fallback behavior.
- **Antigravity technical recommendation:** `X-Company-Id` required for COMPANY-scoped endpoints. Missing or malformed header: HTTP 400. Valid header but unauthorized company: HTTP 403. No fallback to primary/default company for writes. Company access is revalidated server-side. JWT does not itself grant company access.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Explicit inputs prevent accidental data corruption across tenants.
- **Business consequences:** Clients must explicitly select context.
- **Database consequences:** None.
- **API consequences:** Interceptors must attach header.
- **Frontend consequences:** Local storage state must track active company.
- **Security consequences:** Reduces silent writes to the wrong company.
- **Operational consequences:** None.
- **Test consequences:** API tests for missing header rejection.
- **Alternatives considered:** HTTP 403 or fallback to default user company.
- **Why the recommended option is preferred:** A missing required parameter is a client error (400), not a permission denial (403). Fallbacks cause unpredictable routing.
- **Residual risk after adopting the recommendation:** Strict client requirements.
- **Risk that requires Project Owner acceptance:** API client friction.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** API interceptor must strictly enforce `X-Company-Id` for all COMPANY-scoped requests with exactly 400 or 403 responses.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-013 — Employment status
- **Decision ID:** DEC-1B-013
- **Topic:** Employment status
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Auth explicitly requires `ACTIVE` or `PROBATION`.
- **Actual unresolved question:** Exact requirement for authentication.
- **Antigravity technical recommendation:** Authentication permitted only when `account_status` is ACTIVE. Employment status permitted values for authentication: ACTIVE or PROBATION. Status change to suspended/terminated revokes active refresh-token families and increments security stamp/policy version. Every protected request must still revalidate account/session state according to the approved cache strategy.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Standard offboarding security controls.
- **Business consequences:** Terminated employees immediately lose access upon status update.
- **Database consequences:** None.
- **API consequences:** Auth endpoints check employment status.
- **Frontend consequences:** None.
- **Security consequences:** Mitigates access by terminated/suspended employees.
- **Operational consequences:** Requires tight integration with HR module.
- **Test consequences:** Tests for non-active logins.
- **Alternatives considered:** Ignoring employment status for authentication.
- **Why the recommended option is preferred:** Enforces business logic inherently at the security boundary.
- **Residual risk after adopting the recommendation:** HR sync delays.
- **Risk that requires Project Owner acceptance:** Process delay vulnerabilities.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Auth endpoints must explicitly filter on ACTIVE/PROBATION and trigger immediate revocation on termination.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-014 — Temporal assignments
- **Decision ID:** DEC-1B-014
- **Topic:** Temporal assignments
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** SERIALIZABLE tx + UPDLOCK/HOLDLOCK. Retry SQL error 1205.
- **Actual unresolved question:** assignment_status plus dates vs dates alone; locking strategy.
- **Antigravity technical recommendation:** `effective_from` and `effective_to` define temporal validity. `assignment_status` represents lifecycle state such as ACTIVE, REVOKED or CANCELLED. A record is effective only when both lifecycle state and dates allow it. SERIALIZABLE transaction plus UPDLOCK/HOLDLOCK for overlap checks. Database constraint/trigger as defense in depth. SQL error 1205 retry: maximum 3 attempts with bounded jitter. Stable conflict error after retries are exhausted. Add concurrency integration tests.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Standard SQL Server pattern for preventing overlapping intervals safely without uncontrolled dual truth.
- **Business consequences:** Assures absolute data integrity for roles.
- **Database consequences:** Heavy locking on assignment tables during updates.
- **API consequences:** Occasional slow responses during retries.
- **Frontend consequences:** None.
- **Security consequences:** Mitigates concurrent duplicate role/permission assignments.
- **Operational consequences:** None.
- **Test consequences:** Concurrent deadlock simulation tests required.
- **Alternatives considered:** Uncontrolled dual truth.
- **Why the recommended option is preferred:** Solves temporal race conditions deterministically at the persistence layer.
- **Residual risk after adopting the recommendation:** Deadlock contention.
- **Risk that requires Project Owner acceptance:** Transaction contention.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Implement SERIALIZABLE isolation with UPDLOCK/HOLDLOCK and explicitly mapped 1205 retry policies (max 3 with jitter).
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-015 — Audit controls
- **Decision ID:** DEC-1B-015
- **Topic:** Audit controls
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Triggers enforcing append-only behavior.
- **Actual unresolved question:** Mechanism and limitations.
- **Antigravity technical recommendation:** Runtime principal has INSERT and SELECT only. Explicitly deny UPDATE, DELETE and TRUNCATE through ordinary runtime access paths. No cascade delete. Database trigger or equivalent database control rejects audit mutation attempts. No mutation API. Audit payload excludes passwords, tokens, signing keys and secrets. Document clearly that a privileged sysadmin can still change database controls.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** Standard RDBMS defense-in-depth approach.
- **Business consequences:** Audit logs satisfy basic compliance.
- **Database consequences:** Database triggers required.
- **API consequences:** None.
- **Frontend consequences:** None.
- **Security consequences:** Defends against application-layer SQL injection or logic bugs attempting to erase history.
- **Operational consequences:** Requires manual trigger bypass for legitimate schema refactoring.
- **Test consequences:** Integration tests attempting DELETE must fail.
- **Alternatives considered:** Cryptographic ledger databases.
- **Why the recommended option is preferred:** Feasible within existing MS SQL Server constraints without adding new infrastructure.
- **Residual risk after adopting the recommendation:** Privileged sysadmin bypass. Real operational risks include deployment and permission-management complexity, accidental privilege grants, and trigger maintenance.
- **Risk that requires Project Owner acceptance:** DBA bypass capability.
- **External specialist review recommended:** YES
- **Conditions required before implementation:** Must implement INSTEAD OF UPDATE/DELETE triggers, strict runtime database permissions, and PII/secret scrubbing.
- **Conditions required before production:** DBA must review triggers, schema permissions, and sysadmin bypass operational risks.
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development. Independent expert review is required for production.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-016 — Permission codes
- **Decision ID:** DEC-1B-016
- **Topic:** Permission codes
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Fine-grained discrete codes (e.g., `ORG_USER_VIEW`).
- **Actual unresolved question:** Finalize list of discrete codes.
- **Antigravity technical recommendation:** Recommend adopting the explicit Organization and Security permission codes already proposed in the canonical discovery package. Codes remain repository-controlled and immutable after release.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Principles of least privilege.
- **Business consequences:** Administrators must assign multiple permissions to construct a role.
- **Database consequences:** Larger permission assignment mapping.
- **API consequences:** Granular `[Authorize]` attributes required.
- **Frontend consequences:** Complex conditional rendering.
- **Security consequences:** Restricts access strictly to explicit intents.
- **Operational consequences:** None.
- **Test consequences:** Enumerable tests for each code.
- **Alternatives considered:** Replacing fine-grained logic with one broad ADMIN permission.
- **Why the recommended option is preferred:** Essential for enterprise-grade Role-Based Access Control.
- **Residual risk after adopting the recommendation:** Role assignment complexity.
- **Risk that requires Project Owner acceptance:** Configuration complexity.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Permission codes must map to explicit string constants in code, strictly aligned with the canonical discovery package.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-017 — Audit retention
- **Decision ID:** DEC-1B-017
- **Topic:** Audit retention
- **Current status:** DEFERRED — NON-BLOCKING
- **Blocking classification:** DEFERRED — NON-BLOCKING
- **Current canonical proposal:** Retention logic deferred.
- **Actual unresolved question:** Deferral remains non-blocking; deletion prevention conditions.
- **Antigravity technical recommendation:** DEFER AS NON-BLOCKING.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** MVP scope reduction.
- **Business consequences:** No automated cleanup of audit logs.
- **Database consequences:** Unbounded table growth.
- **API consequences:** None.
- **Frontend consequences:** None.
- **Security consequences:** None.
- **Operational consequences:** Storage requirements will grow continuously.
- **Test consequences:** None.
- **Alternatives considered:** Implementing full cold-storage archiving now.
- **Why the recommended option is preferred:** Focuses engineering effort on blocking Phase 1B features.
- **Residual risk after adopting the recommendation:** Unbounded table growth.
- **Risk that requires Project Owner acceptance:** Storage costs.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** No audit deletion or purge in Phase 1B. DEC-1B-015 controls must be implemented.
- **Conditions required before production:** Monitor audit-table size and backup growth. Define an operational warning threshold. Create a separate compliance/retention decision before production data volume becomes material.
- **Project Owner result:** DEFERRED — NON-BLOCKING
- **Project Owner conditions:** 1. No audit deletion or purge in Phase 1B. 2. DEC-1B-015 append-only controls must be implemented. 3. Monitor audit-table size and backup growth. 4. Create a separate compliance/retention decision before production data volume becomes material.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-018 — Client topology
- **Decision ID:** DEC-1B-018
- **Topic:** Client topology
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** `SameSite=Strict`, `HttpOnly` refresh cookies.
- **Actual unresolved question:** Same-site vs cross-site assumption; CSRF controls.
- **Antigravity technical recommendation:** For internal-auth Phase 1 scope: Frontend and API deployed same-site. Access token kept in memory. Refresh token stored only in HttpOnly, Secure cookie. `SameSite=Strict` for the approved same-site topology. Business APIs continue to use bearer access tokens. CSRF protection is mandatory on cookie-authenticated endpoints such as refresh/logout. If future SSO or cross-site deployment is introduced, reopen this decision.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** Strongest browser defense against CSRF and token theft for same-site applications.
- **Business consequences:** Frontend and API must be hosted on the same registrable domain.
- **Database consequences:** None.
- **API consequences:** API must validate AntiForgery tokens on state-changing cookie requests.
- **Frontend consequences:** Must extract and send CSRF tokens appropriately.
- **Security consequences:** Mitigates CSRF and XSS token exfiltration.
- **Operational consequences:** Deployment topology is restricted.
- **Test consequences:** API tests must pass valid CSRF tokens.
- **Alternatives considered:** `SameSite=None` for cross-origin hosting. Imposing CSRF on bearer-token endpoints.
- **Why the recommended option is preferred:** Provides robust security while limiting friction on non-cookie APIs.
- **Residual risk after adopting the recommendation:** Cross-site restriction.
- **Risk that requires Project Owner acceptance:** Topology restrictions.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Explicit `SameSite=Strict` `HttpOnly` configuration for refresh cookies. Implement antiforgery tokens on cookie-authenticated endpoints.
- **Conditions required before production:** Production hosting must guarantee same-site API topology and HTTPS.
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-019 — Signing keys
- **Decision ID:** DEC-1B-019
- **Topic:** Signing keys
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Azure Key Vault for Prod. `dotnet user-secrets` for Dev.
- **Actual unresolved question:** Exact provider, algorithm, and rotation window.
- **Antigravity technical recommendation:** Development: `dotnet user-secrets`. Staging: environment-specific managed secret store or protected environment injection; never committed configuration. Production: provider abstraction is mandatory. Exact production provider remains an infrastructure deployment choice. Azure Key Vault may be used only when Azure is the approved hosting platform. Cryptography: RS256. RSA key size at least 2048 bits. `kid` included in JWT header. Rotation every 90 days or immediately after suspected compromise. Previous signing key retained for validation for at least: access-token lifetime + twice the approved clock skew (20-minute overlap window). Refresh-token lifetime does not require keeping old JWT signing keys.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** Best practices for JWT asymmetric signing keys.
- **Business consequences:** None.
- **Database consequences:** None.
- **API consequences:** Auth service must fetch/cache keys from provider.
- **Frontend consequences:** None.
- **Security consequences:** External secret provider mitigates key exposure in source control.
- **Operational consequences:** Requires managing rotation pipelines.
- **Test consequences:** Integration tests require mocked secret provider.
- **Alternatives considered:** Symmetric HSM keys, hardcoded keys, assuming Azure-only.
- **Why the recommended option is preferred:** Balances security, provider independence, and cloud-native operability.
- **Residual risk after adopting the recommendation:** External provider outage.
- **Risk that requires Project Owner acceptance:** Outage dependencies.
- **External specialist review recommended:** YES
- **Conditions required before implementation:** Cryptography must use RS256/2048+ bit keys with `kid` headers. Implement provider abstraction and 20-minute key overlap window.
- **Conditions required before production:** Select, provision, and test a real production provider (e.g. Key Vault) and establish rotation pipeline.
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development. Independent expert review is required for production.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-020 — Locked account response
- **Decision ID:** DEC-1B-020
- **Topic:** Locked account response
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Return specific status for lockouts.
- **Actual unresolved question:** HTTP 403 versus 423.
- **Antigravity technical recommendation:** HTTP 403 for the currently allowed 403-versus-423 choice. Same sanitized response shape for invalid credentials and account lockout where practical. No username/account-existence disclosure. Detailed lockout reason is recorded only in restricted audit logs. Frontend displays a generic authentication failure message.
- **Recommendation strength:** MEDIUM
- **Evidence supporting the recommendation:** OWASP guidelines on preventing username enumeration.
- **Business consequences:** Valid users may be confused about why they cannot log in.
- **Database consequences:** None.
- **API consequences:** Standardized 403 response.
- **Frontend consequences:** Display generic authentication failure message.
- **Security consequences:** Mitigates username enumeration.
- **Operational consequences:** Helpdesk calls increase for locked accounts.
- **Test consequences:** Assert 403 for lockout instead of 423.
- **Alternatives considered:** HTTP 423 Locked.
- **Why the recommended option is preferred:** Security against enumeration outweighs UX convenience.
- **Residual risk after adopting the recommendation:** Decreased UX clarity.
- **Risk that requires Project Owner acceptance:** Decreased UX clarity.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Authentication failure handlers must strictly emit identical HTTP 403 sanitized responses for both invalid passwords and locked accounts.
- **Conditions required before production:** 
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** Authentication failure handlers must strictly emit identical HTTP 403 sanitized responses for both invalid passwords and locked accounts. Externally observable responses for invalid credentials and locked accounts must be designed not to reveal account existence or status.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization

## DEC-1B-021 — Security audit permission
- **Decision ID:** DEC-1B-021
- **Topic:** Security audit permission
- **Current status:** APPROVED WITH CONDITIONS
- **Blocking classification:** BLOCKING FOR PHASE 1B.1
- **Current canonical proposal:** Explicitly decide on `AUDIT_VIEW` vs `SECURITY_AUDIT_VIEW`.
- **Actual unresolved question:** Reuse versus separate boundary.
- **Antigravity technical recommendation:** Create distinct `SECURITY_AUDIT_VIEW`. `AUDIT_VIEW` remains for business audit access. `SECURITY_AUDIT_VIEW` protects authentication, authorization, administrative and security-event data. An administrator may receive both permissions through explicit role assignment. Do not grant security-audit access automatically to every holder of `AUDIT_VIEW`.
- **Recommendation strength:** HIGH
- **Evidence supporting the recommendation:** Clearer least-privilege boundaries. Avoids exposing security-sensitive audit data to ordinary business audit viewers.
- **Business consequences:** Distinct permissions required.
- **Database consequences:** None.
- **API consequences:** Security endpoints authorize based on `SECURITY_AUDIT_VIEW`.
- **Frontend consequences:** None.
- **Security consequences:** Stronger segregation of duties.
- **Operational consequences:** None.
- **Test consequences:** Tests map to `SECURITY_AUDIT_VIEW`.
- **Alternatives considered:** Reusing `AUDIT_VIEW`.
- **Why the recommended option is preferred:** Limits sensitive access strictly to security administrators.
- **Residual risk after adopting the recommendation:** Additional administration complexity.
- **Risk that requires Project Owner acceptance:** Lack of strict segregation if roles are overloaded.
- **External specialist review recommended:** NO
- **Conditions required before implementation:** Must define and enforce `SECURITY_AUDIT_VIEW` separately from `AUDIT_VIEW` for all security-sensitive events.
- **Conditions required before production:** Security administrators must be explicitly assigned this new permission.
- **Project Owner result:** APPROVED WITH CONDITIONS
- **Project Owner conditions:** See technical recommendation conditions.
- **Project Owner comments:** Accepted residual risks for internal development.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval
- **Confirmation method:** Direct Prompt Authorization
