# Phase 1B.0 Stakeholder Review Package

## 1. Purpose of Phase 1B.0
Phase 1B.0 serves to audit the current repository, database migrations, organization model, business rules, and technical decisions, in order to produce the authoritative Phase 1B Authentication and Authorization design decisions before any implementation begins.

## 2. Current phase status
NOT READY FOR PHASE 1B.1 PLANNING

## 3. Documents reviewed
- AGENTS.md
- docs/business/business-rules.md
- docs/business/permission-catalog.md
- docs/business/acceptance-criteria.md
- docs/architecture/technical-decisions-v1.0.md
- docs/architecture/implementation-roadmap-v1.0.md
- docs/architecture/phase-1a2-application-api-implementation.md
- docs/architecture/phase-1b0-security-discovery-decisions.md
- docs/decisions/phase-1b0-open-decisions.md
- docs/reviews/phase-1b0-stakeholder-review-package.md
- docs/reviews/phase-1a2-baseline-closure-checklist.md
- database/migrations/V0001*
- database/migrations/V0002*
- database/rollbacks/U0001*
- database/rollbacks/U0002*

## 4. Authoritative source hierarchy
1. `docs/business/business-rules.md`
   Canonical business rules.
2. `docs/business/permission-catalog.md`
   Canonical permission catalog, permission attributes, roles and admin-group definitions.
3. `docs/business/acceptance-criteria.md`
   Canonical acceptance criteria.
4. `docs/architecture/technical-decisions-v1.0.md`
   Approved project-wide technical constraints.
5. `docs/architecture/implementation-roadmap-v1.0.md`
   Approved implementation phase boundary.
6. `docs/decisions/phase-1b0-open-decisions.md`
   Authoritative source for Phase 1B.0 decision status and approval evidence.
7. `docs/architecture/phase-1b0-security-discovery-decisions.md`
   Proposed technical design constrained by all sources above.

## 5. Summary of the proposed security architecture
The proposed Phase 1B security architecture introduces dedicated tables for authentication (`User_Auth_Accounts`, `Password_History`, `Refresh_Tokens`) and authorization (`Permissions`, `Roles`, `Admin_Groups`, and their respective assignments). It implements a strict `SERIALIZABLE` + `UPDLOCK/HOLDLOCK` mechanism for temporal overlap control. The audit boundary enforces immutability at the database level by restricting the runtime principal to INSERT/SELECT only. Client token storage relies on in-memory access tokens and `HttpOnly`/`Secure` cookies for refresh tokens. First-admin provisioning utilizes a separate controlled bootstrap command decoupled from API startup.

## 6. Decision review matrix

*Note: DEC-1B-008 was merged into DEC-1B-007 and is not an active decision.*

| Decision ID | Topic | Current proposal | Reason for the proposal | Alternatives | Security impact | Migration impact | Required approvers | Decision owner role | Exact approval question | Blocking for Phase 1B.1 | Conditions | Approved by | Approval date/reference | Final status |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DEC-1B-001 | Login identifier | Separate `User_Auth_Accounts` table. nullable password_hash. | Decouples business user identity from auth provider strategies. | Add to `Users`. | High | High | BA, DBA, Sec | Backend Lead | Do you approve the `User_Auth_Accounts` identity model? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-002 | Password policy | ASP.NET Core PasswordHasher, min 8 max 64, 24h temp, 5 history block, lockout. | Enforces modern secure defaults natively in .NET. | Plaintext | High | High | BA, DBA, Sec | Security Lead | Do you approve the exact password policy required by the schema? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-003 | Token lifetimes | Access: 15m. Refresh: 7d. Skew: 0s. | Balances security window vs UX. | Longer access tokens. | High | None | BA, Sec | Security Lead | Do you approve the token lifetimes? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-004 | Lockout values | 5 failures = 15m lockout (config driven). | Prevents brute force effectively. | No lockout. | High | Medium | BA, DBA, Sec | Security Lead | Do you approve the lockout threshold and duration? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-005 | Refresh token schema & family | `session_id` family rotation. Store `token_hash` only. Reuse revokes family. | Reduces the impact of refresh-token theft, supports reuse detection, and enables session-family revocation. It does not eliminate token theft risk. | Non-rotating tokens. | High | High | BA, DBA, Sec | Backend Lead | Do you approve the refresh token family rotation behavior? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-006 | Permission catalog schema | `permission_code` (VARCHAR(100)) as natural PK. Immutable/no rename. | Provides stable guarantees for codebase mapping. | Surrogate `bigint`. | Medium | High | BA, DBA, Sec | DBA Lead | Do you approve the permission schema using natural PKs? | BLOCKING FOR PHASE 1B.1 | Stable `role_code` and `group_code`; decide whether ENTITY scope is in Phase 1B; permission codes are migration controlled and immutable after release. | - | - | OPEN |
| DEC-1B-007 | Role & Admin-Group scope | `scope_type = GLOBAL \| COMPANY` for Roles and Admin Groups. No hardcoded bypass. | Single explicit source of truth for authorization scope. | Global only. | High | High | BA, DBA, Sec | BA/Product Owner | Do you approve the role/admin-group scope models? | BLOCKING FOR PHASE 1B.1 | One source of truth for Role/Admin-Group scope; GLOBAL requires company_id NULL; COMPANY requires company_id NOT NULL; no hardcoded SUPER_ADMIN bypass. | - | - | OPEN |
| DEC-1B-009 | Admin group model | `Admin_Groups`, explicit mappings, enforces hard rules. | Aligns admin administration with standard permissions. | Hardcoded bypass. | High | High | BA, DBA, Sec | BA/Product Owner | Do you approve the admin group model? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-010 | First-admin provisioning | Separate bootstrap command, no console print, 1-time marker, sets must_change_password. | Production-safe process decoupled from API startup. | Print to console. | High | Low | BA, Sec | Infrastructure Lead | Do you approve the bootstrap method? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-011 | Permission cache failure | DB `policy_version` read on every request. Read failure fails closed. | Guarantees instant invalidation while surviving cache loss. | Redis. | High | Medium | BA, DBA, Sec | Backend Lead | Do you approve the cache failure behavior? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-012 | Current-company missing-header behavior | `X-Company-Id` required for COMPANY endpoints. Missing returns `AUTH_CURRENT_COMPANY_REQUIRED`. | Enforces strict API intent and context correctness. | Embed in JWT. | High | None | BA, Sec | Backend Lead | Do you approve the strict current-company header requirement? | BLOCKING FOR PHASE 1B.1 | Decide whether missing X-Company-Id returns HTTP 400 or 403; COMPANY write endpoints must not silently fall back to primary company. | - | - | OPEN |
| DEC-1B-013 | Employment-status values | Auth explicitly requires `ACTIVE` or `PROBATION`. | Prevents login by resigned/terminated/suspended users. | Ignore employment. | High | None | BA, Sec | BA/Product Owner | Do you approve the employment-status authentication rule? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-014 | Temporal locking mechanism | SERIALIZABLE tx + UPDLOCK/HOLDLOCK. Retry SQL error 1205. | Deterministically prevents temporal overlaps across clusters. | sp_getapplock | High | High | BA, DBA, Sec | DBA Lead | Do you approve the exact temporal locking mechanism? | BLOCKING FOR PHASE 1B.1 | Select one temporal-status source of truth; no contradictory `is_active`, `assignment_status` and `effective_to` combinations; approve SERIALIZABLE plus UPDLOCK/HOLDLOCK and SQL 1205 retry. | - | - | OPEN |
| DEC-1B-015 | Audit database controls | Runtime principal has INSERT/SELECT only. SQL error stable map. | Database permissions and defensive triggers enforce append-only behavior at the database authorization boundary. This is not a cryptographic guarantee. | EF interceptor only. | High | High | BA, DBA, Sec | DBA Lead | Do you approve the database-level audit immutability? | BLOCKING FOR PHASE 1B.1 | Complete required Security_Audit_Events fields; runtime principal INSERT/SELECT only; UPDATE/DELETE/TRUNCATE blocked; no secrets or raw tokens in audit. | - | - | OPEN |
| DEC-1B-016 | Exact Organization and Security permission codes | Adopt the explicit 15 proposed canonical permission codes. | Provides immediate authorization granularity. | Broad `ADMIN` code. | High | Medium | BA, Sec | BA/Product Owner | Do you approve the exact proposed permission codes needed for Phase 1B.1? | BLOCKING FOR PHASE 1B.1 | None | - | - | OPEN |
| DEC-1B-017 | Security audit retention/archive | No purge/archive in Phase 1B. | Ensures compliance during initial rollout. | 1 year purge. | High | High | BA, DBA, Sec | DBA Lead | Do you approve deferring audit archive/retention features? | DEFERRED — NON-BLOCKING | Phase 1B implements no purge or archive; current audit records remain in the database; audit identity and immutability are preserved; long-term retention/archive is handled by a later compliance decision. | - | - | OPEN |
| DEC-1B-018 | Client deployment topology and cookie SameSite behavior | Access token in memory, refresh in `HttpOnly`/`Secure`/`SameSite` cookie. | Defends against XSS exfiltration of long-lived tokens. | LocalStorage. | High | None | BA, Sec | Security Lead | Do you approve the client token storage topology? | BLOCKING FOR PHASE 1B.1 | Deployment topology must be identified; SameSite behavior must match topology; cookie-based refresh requires approved CSRF controls. | - | - | OPEN |
| DEC-1B-019 | Signing-key provider and rotation | Azure Key Vault/injected secret. `kid` rotation. 24h old window. | Ensures enterprise-grade key rotation and safety. | Hardcoded. | High | Low | BA, Sec | Security Lead | Do you approve the signing-key management strategy? | BLOCKING FOR PHASE 1B.1 | Approved production secret provider must be identified; key rotation, `kid`, current key and previous-key window must be operationally defined. | - | - | OPEN |
| DEC-1B-020 | Account-locked HTTP status | Returns 403 or 423. | Clearly differentiates locked accounts from incorrect passwords. | 400 Bad Request | High | None | BA, Sec | Backend Lead | Do you approve the account-locked HTTP status API contract? | BLOCKING FOR PHASE 1B.1 | Choose exactly one final HTTP status: 403 or 423. | - | - | OPEN |
| DEC-1B-021 | Audit-view permission reuse versus SECURITY_AUDIT_VIEW | Explicitly decide on `AUDIT_VIEW` vs `SECURITY_AUDIT_VIEW`. | Prevents duplicate permission boundary meanings. | - | Medium | None | BA, Sec | BA/Product Owner | Do you approve reusing AUDIT_VIEW or retaining SECURITY_AUDIT_VIEW? | BLOCKING FOR PHASE 1B.1 | Choose AUDIT_VIEW reuse or a clearly distinct SECURITY_AUDIT_VIEW boundary. | - | - | OPEN |

## 7. BA review section
- Which employment statuses may authenticate?
- Are ACTIVE and PROBATION the only login-capable statuses?
- Are the proposed Organization and Security permission codes accepted?
- Should AUDIT_VIEW be reused, or should SECURITY_AUDIT_VIEW exist with a different administration boundary?
- Are GLOBAL and COMPANY the approved Role/Admin Group scopes?
- Is X-Company-Id required for every COMPANY endpoint?
- Should COMPANY requests ever fall back to the primary company?
- Which status-changing actions require a mandatory reason?
- Are Admin Group assignments permitted at both GLOBAL and COMPANY scope?
- Should the current permission catalog support ENTITY scope in Phase 1B?

*(All above decisions require explicit BA/Product Owner approval)*

## 8. DBA review section
- Are permission_code and other stable codes acceptable as natural keys?
- Add stable codes: `Roles.role_code VARCHAR(50) UNIQUE NOT NULL` and `Admin_Groups.group_code VARCHAR(50) UNIQUE NOT NULL`?
- Are all proposed SQL types and lengths acceptable?
- Is SERIALIZABLE plus UPDLOCK/HOLDLOCK approved for temporal writes?
- Is SQL error 1205 the only retried SQL error?
- Are overlap triggers and filtered unique indexes approved?
- Should temporal assignment status be represented by: assignment_status plus effective dates, or calculated only from effective dates? (Remove conflicting dual sources such as is_active versus effective_to).
- Are runtime audit-table permissions sufficient?
- Are UPDATE, DELETE and TRUNCATE protections complete?
- Are Security_Bootstrap_State and Authorization_Policy_State singleton constraints acceptable?
- Are Password_History and Refresh_Tokens indexes sufficient?
- Is U0003 rollback required to preserve or explicitly destroy security audit/bootstrap state?
- Is ENTITY permission scope implementable with the proposed schema?

*(All above decisions require explicit DBA Lead approval)*

## 9. Security/Infrastructure review section
- Password minimum and maximum length
- Password history of five hashes
- Temporary-password lifetime
- Lockout threshold and duration
- Whether AUTH_ACCOUNT_LOCKED returns HTTP 403 or 423
- Access-token and refresh-token lifetimes
- JWT clock skew
- HMAC key strength
- Signing-key source in Development, Staging and Production
- `kid` and key-rotation procedure
- Old-key validation window
- Secure refresh-token cookie behavior
- SameSite behavior for same-site and cross-site deployment
- CSRF protection for cookie-based refresh
- Refresh token rotation and reuse response
- Concurrent refresh deterministic behavior
- Bootstrap operator and approved secret source
- Session revocation after password reset/account disable
- Audit exclusion of passwords, hashes, tokens and secrets

*(All above decisions require explicit Security/Infrastructure Lead approval)*

## 10. Cross-functional issues (Mandatory Technical Conditions)

**Condition 1: Stable Role and Admin Group codes**
Proposed: `Roles.role_code VARCHAR(50) NOT NULL UNIQUE`, `Admin_Groups.group_code VARCHAR(50) NOT NULL UNIQUE`. Display names may change. Released codes may not be renamed.

**Condition 2: Complete Security Audit fields**
The proposed `Security_Audit_Events` requirements must include: `id`, `event_type` (or stable `action_code`), `actor_user_id`, `acting_as`, `target_user_id`, `company_id`, `entity_type`, `entity_id`, `before_state_json`, `after_state_json`, `changed_fields`, `reason`, `correlation_id`, `request_metadata`, `outcome`, `policy_version`, `created_at`. No password, password hash, token, signing key or secret may be stored.

**Condition 3: One source of truth for temporal status**
Do not allow `is_active = 1` and `effective_to IS NOT NULL` or other contradictory combinations. Recommend one of:
A. `assignment_status` ACTIVE/CLOSED with consistency checks; or
B. derive effective state only from `effective_from`/`effective_to`.
Require DBA approval for the selected model.

**Condition 4: ENTITY scope**
Do not claim ENTITY scope is implemented unless the schema and authorization evaluator can identify and validate an entity boundary. Require a decision: Phase 1B supports only GLOBAL and COMPANY; or Phase 1B adds a complete ENTITY scope model.

**Condition 5: Test corrections**
ProblemDetails sanitization must not be mapped to SEC-004 unless it actually tests sensitive field masking. Use a technical security requirement or DEC reference instead. Rename the concurrent refresh test to express deterministic behavior. Define the expected winner/reuse/revocation behavior explicitly.

## 11. Blocking versus non-blocking decisions
A decision may be marked NON-BLOCKING or DEFERRED CANDIDATE only when Phase 1B.1 can be designed and implemented without guessing its outcome.

Currently, **19 active decisions are classified as BLOCKING FOR PHASE 1B.1** because they fundamentally dictate the login identity model, schema requirements, database immutability, api contracts, caching behavior, and test definitions.

DEC-1B-017 is classified as **DEFERRED — NON-BLOCKING** because Phase 1B.1 can be built with no purge/archive feature implemented, while preserving audit immutability for later retention/compliance resolution.

## 12. Approval recording instructions
1. Reviewer reads the proposal and alternatives.
2. Reviewer records:
   - APPROVED
   - APPROVED WITH CONDITIONS
   - REJECTED
   - DEFERRED — NON-BLOCKING
3. Reviewer name/role is recorded.
4. Approval date and reference are recorded.
5. Conditions are written explicitly.
6. Discovery document is synchronized with the decision result.
7. No implementation begins while a blocking decision remains OPEN.

*No approval may be inferred from meeting attendance, silence, chat reaction or document access.*

## 13. Phase 1A.2 baseline closure checklist
Please see `docs/reviews/phase-1a2-baseline-closure-checklist.md` for the exact verification sequence required to close the Phase 1A.2 uncommitted baseline.

## 14. Exit criteria for Phase 1B.0
- All blocking decisions approved.
- Phase 1A.2 committed and tagged.

## 15. Prohibited next actions
- V0003/U0003 creation;
- authentication implementation;
- authorization implementation;
- JWT endpoint implementation;
- Security UI implementation;
- automatic commit or tag.

## 16. Final current status

CURRENT DOCUMENTATION STATUS:
READY FOR BA / DBA / SECURITY REVIEW

CURRENT PHASE STATUS:
NOT READY FOR PHASE 1B.1 PLANNING

BLOCKERS:
1. Blocking DEC-1B decisions require real stakeholder decisions.
2. Phase 1A.2 requires verified build/test, commit and tag.
3. Any conditions raised by reviewers must be incorporated into the discovery and decision documents.

AUTHORIZED NEXT ACTIONS:
- stakeholder review;
- approval recording;
- Phase 1A.2 baseline review;
- build and test verification;
- preparation for a user-authorized commit/tag.

PROHIBITED NEXT ACTIONS:
- V0003/U0003 creation;
- authentication implementation;
- authorization implementation;
- JWT endpoint implementation;
- Security UI implementation;
- automatic commit or tag.
