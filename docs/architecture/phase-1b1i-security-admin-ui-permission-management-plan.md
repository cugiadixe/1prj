# Phase 1B.1-I Security Admin UI and Permission Management Plan

**Status**: PHASE 1B.1-I IMPLEMENTATION ACCEPTED — SEE [phase-1b1i-project-owner-implementation-acceptance.md](phase-1b1i-project-owner-implementation-acceptance.md)

**Baseline**: a8fdbe636e4a57429f8ba2d58652a27349a2989d

**Previous completed phase**: Phase 1B.1-H COMPLETE
(Security Audit Read / SECURITY_AUDIT_VIEW — GET /api/v2/security/audit-events)

---

## 1. Purpose

> **Phase shape note:** The planning umbrella for Phase 1B.1-I is "Security Admin UI / Permission Management." However, discovery found the frontend has no login page, no auth state, no permission-gated routing, and no admin page foundation. Building a meaningful admin UI requires first establishing the authentication layer as a prerequisite. Therefore, the recommended Phase 1B.1-I implementation scope is backend-first Account Management APIs (Option A below). The frontend Security Admin UI should remain a later phase unless the Project Owner explicitly selects Option B or C.
>
> **Recommended implementation name**: Phase 1B.1-I — Account Management API Hardening
> **Planning umbrella**: Security Admin UI / Permission Management

Phase 1B.1-I addresses two related but distinct concerns that remain open after Phase H:

1. **Account Management backend gap** — The security administration API surface is incomplete. All role, admin group, assignment, department permission, individual permission, effective permission, and audit read APIs are implemented. However, the account management API surface (view, activate, disable, lock, unlock, admin password reset, revoke all sessions) does not exist. No `AccountsController` and no `ISecurityAdminService` account methods exist. The permission code `SECURITY_ACCOUNT_MANAGE` is absent from `PermissionCodes.cs`.

2. **Frontend absent** — The frontend is a React placeholder with two pages (Home, SystemHealth). No authentication UI, no login page, no permission-aware routing or context, and no security administration screens exist. The login response does not expose permission codes. Any operator today must call all security administration APIs directly (e.g., via curl or Swagger).

This phase proposes the path to address these two concerns. It offers three option shapes for Project Owner decision before implementation may begin.

---

## 2. Confirmed current state

As of Phase H acceptance (commit a8fdbe6), the codebase contains:

### Backend — what is fully implemented

All APIs use `SECURITY_ADMIN_MANAGE` (GLOBAL scope) for authorization uniformly (not the fine-grained per-resource codes described in the Phase 1B.0 discovery document; the D-B plan OD-D-B-03 accepted this consolidation).

**Permission catalog (read-only):**
- `GET /api/v2/security/permissions` — SECURITY_ADMIN_MANAGE, GLOBAL
- `GET /api/v2/security/permissions/{code}` — SECURITY_ADMIN_MANAGE, GLOBAL

**Roles (CRUD + permission assignment):**
- `GET /api/v2/security/roles` — SECURITY_ADMIN_MANAGE, GLOBAL
- `GET /api/v2/security/roles/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL
- `POST /api/v2/security/roles` — SECURITY_ADMIN_MANAGE, GLOBAL
- `PUT /api/v2/security/roles/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL (with rowversion concurrency)
- `DELETE /api/v2/security/roles/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL (soft deactivate with rowversion)
- `POST /api/v2/security/roles/{id:long}/permissions` — SECURITY_ADMIN_MANAGE, GLOBAL (add permissions to role)
- `DELETE /api/v2/security/roles/{id:long}/permissions/{code}` — SECURITY_ADMIN_MANAGE, GLOBAL

**Admin groups (CRUD + permission assignment):**
- `GET /api/v2/security/admin-groups` — SECURITY_ADMIN_MANAGE, GLOBAL
- `GET /api/v2/security/admin-groups/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL
- `POST /api/v2/security/admin-groups` — SECURITY_ADMIN_MANAGE, GLOBAL
- `PUT /api/v2/security/admin-groups/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL (with rowversion)
- `DELETE /api/v2/security/admin-groups/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL (soft deactivate with rowversion)
- `POST /api/v2/security/admin-groups/{id:long}/permissions` — SECURITY_ADMIN_MANAGE, GLOBAL
- `DELETE /api/v2/security/admin-groups/{id:long}/permissions/{code}` — SECURITY_ADMIN_MANAGE, GLOBAL

**User role assignments:**
- `GET /api/v2/security/users/{userId:long}/role-assignments` — SECURITY_ADMIN_MANAGE, GLOBAL
- `POST /api/v2/security/users/{userId:long}/role-assignments` — SECURITY_ADMIN_MANAGE, GLOBAL (idempotent on exact duplicate)
- `DELETE /api/v2/security/users/{userId:long}/role-assignments/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL (soft deactivate)

**User admin group assignments:**
- `GET /api/v2/security/users/{userId:long}/admin-group-assignments` — SECURITY_ADMIN_MANAGE, GLOBAL
- `POST /api/v2/security/users/{userId:long}/admin-group-assignments` — SECURITY_ADMIN_MANAGE, GLOBAL (idempotent)
- `DELETE /api/v2/security/users/{userId:long}/admin-group-assignments/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL

**User individual permissions:**
- `GET /api/v2/security/users/{userId:long}/individual-permissions` — SECURITY_ADMIN_MANAGE, GLOBAL
- `POST /api/v2/security/users/{userId:long}/individual-permissions` — SECURITY_ADMIN_MANAGE, GLOBAL (ALLOW or DENY, idempotent)
- `DELETE /api/v2/security/users/{userId:long}/individual-permissions/{id:long}` — SECURITY_ADMIN_MANAGE, GLOBAL

**Department baseline permissions:**
- `GET /api/v2/security/departments/{departmentId:long}/permissions` — SECURITY_ADMIN_MANAGE, GLOBAL
- `PUT /api/v2/security/departments/{departmentId:long}/permissions` — SECURITY_ADMIN_MANAGE, GLOBAL (full replace)
- `DELETE /api/v2/security/departments/{departmentId:long}/permissions/{code}` — SECURITY_ADMIN_MANAGE, GLOBAL

**Effective permissions viewer:**
- `GET /api/v2/security/users/{userId:long}/effective-permissions?companyId={companyId}` — SECURITY_ADMIN_MANAGE, GLOBAL

**Audit read (Phase H):**
- `GET /api/v2/security/audit-events` — SECURITY_AUDIT_VIEW, GLOBAL (paged, filtered)

**Infrastructure services available:**
- `IAuditWriter` — fail-closed async audit write interface
- `ITransactionalAuditWriter` — transaction-aware audit write (same connection/tx)
- `ISecurityAdminService` — facade for all D-B operations
- `IPermissionEvaluator` — evaluates effective permissions for a user
- `SecurityControllerHelper` — actor extraction from JWT
- V0003 security schema (all tables: Roles, Admin_Groups, Permissions, User_Role_Company, User_Individual_Permissions, User_Admin_Group_Assignments, Department_Permissions, User_Auth_Accounts, Password_History, Refresh_Tokens, Authorization_Policy_State, Security_Audit_Events, Security_Bootstrap_State)
- V0004 seeds `SECURITY_ADMIN_MANAGE` into dbo.Permissions (gap resolved in Phase F-B0)

### Backend — what is absent

No `AccountsController` exists. No account management methods exist on `ISecurityAdminService`. The following APIs are absent:

| Route | Purpose | Discovery-doc permission |
|---|---|---|
| `GET /api/v2/security/accounts/{id}` | View auth account detail | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/activate` | Activate disabled account | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/disable` | Disable active account | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/lock` | Administratively lock account | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/unlock` | Unlock locked account | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/reset` | Force reset password (admin) | SECURITY_ACCOUNT_MANAGE |
| `POST /api/v2/security/accounts/{id}/revoke-all-sessions` | Revoke all active refresh tokens | SECURITY_ACCOUNT_MANAGE |

Additionally:
- `SECURITY_ACCOUNT_MANAGE` is absent from `PermissionCodes.cs`.
- `SECURITY_ACCOUNT_MANAGE` is present in V0003 seed (row 12 from the Phase F-B hard stop report), so the database row exists.
- Single audit event detail endpoint `GET /api/v2/security/audit-events/{id}` is not implemented.
- No user search/list endpoint under the security namespace (users are accessible via the existing organization UsersController, not the security namespace).

### Frontend — what exists

| Item | Status |
|---|---|
| Framework | React 19 + TypeScript, Vite 8, Ant Design 6, TanStack Query v5, React Router DOM v7, React Hook Form 7, Zod 4 |
| Login/auth page | ABSENT |
| Auth state management (context/store/token storage) | ABSENT |
| Permission-aware navigation or route guards | ABSENT |
| Security administration pages | ABSENT |
| Audit viewer UI | ABSENT |
| Permission-based component visibility | ABSENT |

The App.tsx renders two pages (Home, SystemHealth) with no authentication check. The login response DTO (`LoginResponse`) exposes: `AccessToken`, `TokenType`, `ExpiresIn`, `ExpiresAtUtc`, `User.UserId/Username/DisplayName`, `MustChangePassword`. It does NOT expose permission codes. To drive permission-gated UI, the frontend would need to call `GET /api/v2/auth/my-permissions` after login.

### Migration status

| Migration | Status |
|---|---|
| V0001 schema versions | Applied |
| V0002 organization schema | Applied |
| V0003 security schema | Applied — includes SECURITY_ACCOUNT_MANAGE seed (row 12) |
| V0004 SECURITY_ADMIN_MANAGE backfill | Applied — gap resolved in Phase F-B0 |

No schema migration is required for Phase I based on current inspection. All tables needed for account management already exist in V0003 (`User_Auth_Accounts`, `Password_History`, `Refresh_Tokens`).

---

## 3. Backend API discovery

### APIs that exist

All APIs confirmed by reading source controllers. Every endpoint enforces `SECURITY_ADMIN_MANAGE` at `PermissionScope.Global` (set at controller level via `[RequirePermission]` attribute) except the audit endpoint which enforces `SECURITY_AUDIT_VIEW`.

The `ISecurityAdminService` interface and its `SecurityAdminService` implementation cover: permissions catalog, roles CRUD, admin groups CRUD, user role assignments, user admin group assignments, user individual permissions, department permissions, and effective permissions computation.

The `ISecurityAuditQueryService` and `SqlSecurityAuditQueryService` cover audit read.

### APIs that are missing

**Account Management** — completely absent. No controller, no service interface methods, no PermissionCodes constant.

The Phase 1B.0 discovery document lists `SECURITY_ACCOUNT_MANAGE` as a distinct permission required for account lifecycle operations. This is consistent with the current PermissionCodes.cs gap: the constant `SECURITY_ACCOUNT_MANAGE` does not exist and would need to be added alongside any account management implementation.

**Decision required**: Whether account management APIs use `SECURITY_ACCOUNT_MANAGE` (as originally designed) or are consolidated under `SECURITY_ADMIN_MANAGE` (as all existing D-B APIs use). Both approaches are defensible; the Project Owner must decide.

### Permission code consolidation observation

The D-B implementation (Phase D-B) accepted `SECURITY_ADMIN_MANAGE` as the single management permission (OD-D-B-03). The discovery document originally proposed separate fine-grained codes (`SECURITY_ROLE_VIEW`, `SECURITY_ROLE_MANAGE`, `SECURITY_PERMISSION_VIEW`, `SECURITY_PERMISSION_MANAGE`, etc.). These fine-grained codes exist in V0003's Permissions table and in the permission catalog but are NOT used by any controller today. This is a known, accepted simplification from Phase D-B.

For account management, the existing code pattern suggests using `SECURITY_ADMIN_MANAGE` for consistency, but `SECURITY_ACCOUNT_MANAGE` already exists in the database and the permission catalog.

### Gap relative to Phase 1B.0 spec

The Phase 1B.0 doc listed `PUT /api/v2/security/roles/{id}/status` and `PUT /api/v2/security/admin-groups/{id}/status` as status toggle endpoints. The D-B implementation instead uses `DELETE /{id}` (soft deactivate) with a rowversion body. This is functionally equivalent and the D-B plan was accepted. These are not gaps.

The Phase 1B.0 doc listed `POST /api/v2/security/users/{id}/roles/assign` and `/close`. The D-B implementation uses `POST /users/{userId}/role-assignments` and `DELETE /users/{userId}/role-assignments/{id}`. These are functionally equivalent and accepted.

---

## 4. Frontend discovery

**Framework**: React 19, TypeScript, Vite 8 (build), Ant Design 6 (UI component library), TanStack Query v5 (server state), React Router DOM v7 (routing), React Hook Form 7 (forms), Zod 4 (schema validation).

The stack is appropriate and production-capable for building an admin console. Ant Design 6 provides table, form, modal, menu, and layout primitives needed for a security admin UI.

**What exists**:
- `src/main.tsx` — mounts App inside StrictMode
- `src/App.tsx` — Router + Layout + two menu items (Home, SystemHealth)
- `src/pages/Home.tsx` — static welcome text
- `src/pages/SystemHealth.tsx` — health-check viewer

**What is absent and would be required for any admin UI**:

1. **Auth state management** — No login context, no token storage, no CSRF token handling, no automatic refresh on 401.
2. **Login page** — No `POST /api/v2/auth/login` integration exists in the frontend.
3. **Permission data loading** — After login, `GET /api/v2/auth/my-permissions` must be called to populate a permission set for the current context (company-scoped or global). The login response does not include permission codes.
4. **Route guards** — No protected-route component exists. Any security admin page would be openly accessible once routed, which violates AUTH-009 (server enforces anyway, but the UI would be confusing).
5. **Security admin pages** — No pages for roles, admin groups, assignments, department permissions, effective permissions viewer, or audit viewer.
6. **X-Company-Id header handling** — COMPANY-scoped endpoints require this header. No frontend mechanism exists.

**Assessment**: Building any meaningful security admin UI requires first building authentication (login page, token management, refresh), then permission-gated routing, and then the actual admin screens. This is a significant but well-bounded frontend development effort given the existing stack choice.

---

## 5. Proposed options

### Option A — Backend hardening only: Account Management APIs

Implement only the missing account management API surface:

- Add `SECURITY_ACCOUNT_MANAGE` constant to `PermissionCodes.cs`.
- Add account management methods to `ISecurityAdminService` and `SecurityAdminService`.
- Implement `AccountsController` with:
  - `GET /api/v2/security/accounts/{id}` — view auth account (provider type, status, lockout state, must_change_password, created/updated dates; no password hash or security_stamp)
  - `POST /api/v2/security/accounts/{id}/activate` — set `auth_account_status = ACTIVE`
  - `POST /api/v2/security/accounts/{id}/disable` — set `auth_account_status = DISABLED`
  - `POST /api/v2/security/accounts/{id}/lock` — set `auth_account_status = LOCKED`, set `lockout_end`
  - `POST /api/v2/security/accounts/{id}/unlock` — clear lockout, set `auth_account_status = ACTIVE`
  - `POST /api/v2/security/accounts/{id}/reset` — admin-forced password reset (set `must_change_password = 1`, issue new temp password hash, revoke all sessions, audit)
  - `POST /api/v2/security/accounts/{id}/revoke-all-sessions` — bulk revoke refresh tokens
- All mutations emit audit events via `ITransactionalAuditWriter`.
- All mutations increment `Authorization_Policy_State.policy_version`.
- Unit, integration, and API tests.
- No frontend. No schema migration.

**Scope**: Backend only. Frontend operators continue using API tools. Completes the backend security surface as originally designed.

**Effort estimate**: Medium. The pattern is established from D-B. The main complexity is in the password reset flow (security_stamp rotation, session revocation, temp password management, audit).

**Risk**: Low. Follows existing patterns exactly.

### Option B — Frontend admin console MVP (no new backend APIs)

Build the frontend security admin console on top of the existing APIs (excluding account management which is absent):

- Implement auth state (login page, token storage in memory, refresh token cookie handling, CSRF header management, auto-refresh on 401).
- Implement permission loading from `GET /api/v2/auth/my-permissions`.
- Implement permission-gated routing (protected route component checking SECURITY_ADMIN_MANAGE).
- Implement security admin pages for all existing APIs:
  - Roles list + detail + create/edit/deactivate + permission assignment
  - Admin groups list + detail + create/edit/deactivate + permission assignment
  - Permissions catalog browser (read-only)
  - User assignments (role assignments, admin group assignments, individual permissions) — requires user search
  - Department permissions management — requires department list
  - Effective permissions viewer
  - Audit events viewer (using Phase H audit API)
- No account management UI (backend API absent).
- No new backend code.

**Scope**: Frontend only. Large surface area. Requires user search and department list APIs from the organization controllers.

**Effort estimate**: High. Building a complete auth + admin console from a blank page is substantial even with a good stack.

**Risk**: Medium. Dependencies on organization APIs (user search, department list) that may have gaps. Complex state management for multi-page forms and temporal assignments.

### Option C — Mixed: Account Management backend + Frontend authentication layer

Implement both:

1. Account Management APIs (same as Option A).
2. Frontend authentication layer only (login page, token management, permission loading, protected routing).

This stops short of building all admin screen pages but delivers:
- A working frontend login flow that operators can use.
- All backend APIs complete.
- Foundation for a Phase J that builds out the full admin screen pages.

**Scope**: Backend account management + frontend auth layer. Medium-large.

**Effort estimate**: High.

**Risk**: Medium. Two parallel tracks. Testing the auth layer requires a working login.

---

## 6. Recommended option

**Recommended: Option A — Backend hardening only (Account Management APIs)**

Rationale:

1. The backend is the security enforcement boundary. Completing the account management API surface closes the last gap in the security administration backend that was planned in Phase 1B.0.
2. The frontend is currently not relied upon by any operator for security administration. Operators use API tooling. The frontend gap is real but not blocking.
3. Option A follows the established phase pattern: one well-bounded slice per phase, backend first, test-driven.
4. Option A enables a future Phase J to build the full frontend admin console with a complete and stable API surface beneath it.
5. Account management (lock, unlock, disable, reset password, revoke sessions) is operationally important and currently has no API at all. This is a more urgent gap than absent frontend pages.

If the Project Owner prefers an incremental frontend approach alongside the backend, Option C is the recommended alternative. Option B alone is not recommended because it defers the account management backend gap.

---

## 7. Proposed in-scope (for Option A — recommended)

1. Add `public const string SecurityAccountManage = "SECURITY_ACCOUNT_MANAGE"` to `PermissionCodes.cs` — **only after DEC-1B-I-04 is approved**. Do not add this constant or modify `PermissionCodes.cs` before Project Owner accepts that decision. Do not create a new permission code in the database; the row already exists in V0003.
2. Add account management DTOs to `PTKD.Application.Security.Authorization.DTOs`:
   - `AccountDetailDto` — safe fields only (no password hash, no security_stamp)
   - `ActivateAccountRequest` (rowversion)
   - `DisableAccountRequest` (rowversion)
   - `LockAccountRequest` (lockoutDurationMinutes, rowversion)
   - `UnlockAccountRequest` (rowversion)
   - `AdminResetPasswordRequest` (rowversion; response includes temporary password — see DEC-1B-I-03)
   - `RevokeAllSessionsRequest` (reason)
3. Add account management methods to `ISecurityAdminService`.
4. Implement account management in `SecurityAdminService`.
5. Add `AccountsController` at `api/v2/security/accounts`.
6. All mutations: transactional audit via `ITransactionalAuditWriter`, policy version increment.
7. Unit tests for service logic.
8. Integration tests for account state transitions.
9. API tests for 401/403/404/409/204 response codes.
10. DatabaseSafety test verifying SECURITY_ACCOUNT_MANAGE present in dbo.Permissions (it is in V0003 row 12; test should confirm).

---

## 8. Explicit out-of-scope (for Option A)

- No frontend admin console (unless Project Owner rejects Option A in favour of Option B or C).
- No frontend login page.
- No frontend auth state or permission-gated routing.
- No audit viewer UI.
- No effective permissions viewer UI.
- No schema migration (V0003 already contains all required tables; V0004 already seeds SECURITY_ADMIN_MANAGE).
- No new Permissions table rows (SECURITY_ACCOUNT_MANAGE is already in V0003 seed; no V0005 migration).
- No password policy change.
- No email/SMS OTP or forgot-password flow.
- No SIEM integration.
- No audit export/CSV/PDF.
- No audit retention/archive/purge.
- No Dynamic Approval Workflow.
- No AD/LDAP.
- No bulk import/export.
- No new permission model redesign.
- No production deployment.
- No business module implementation.
- No tag or push (until separately authorized).
- No `GET /api/v2/security/audit-events/{id}` single detail endpoint.
- No user search API (organization UsersController handles user listing).

---

## 9. Permission boundary

| Operation | Required permission | Scope |
|---|---|---|
| View auth account detail | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Activate account | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Disable account | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Lock account | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Unlock account | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Admin reset password | SECURITY_ACCOUNT_MANAGE | GLOBAL |
| Revoke all sessions | SECURITY_ACCOUNT_MANAGE | GLOBAL |

Note: Whether `SECURITY_ACCOUNT_MANAGE` or `SECURITY_ADMIN_MANAGE` is used is a Project Owner decision (DEC-1B-I-04 below). The account management operations are sensitive and the original design intended a separate permission code.

All other existing security administration APIs retain `SECURITY_ADMIN_MANAGE` as already implemented.

---

## 10. UI/UX safety rules

These apply to any future frontend implementation (not to Option A backend-only scope):

1. Disable (not hide) account management action buttons when the actor does not hold `SECURITY_ACCOUNT_MANAGE`. Server re-checks anyway (AUTH-009).
2. Require a typed confirmation ("Type the username to confirm") before any of: disable, lock, admin reset, revoke all sessions.
3. Display a prominent warning banner when viewing an account that has `must_change_password = true`.
4. Display account status (ACTIVE, LOCKED, DISABLED) with color-coded badges.
5. Show lockout expiry time when status is LOCKED.
6. Do not display `password_hash`, `security_stamp`, or any secret field in the UI response even if the API inadvertently exposed them (client must filter).
7. After admin password reset, display the temporary password exactly once in a modal with a "Copy and close" pattern. Do not log it to the browser console.
8. Soft-deactivation of roles and admin groups must show the count of active assignments before confirming.
9. Effective permissions viewer must clearly label which context (GLOBAL or company name) the permissions are computed for.
10. Audit viewer must be read-only with no edit/delete/export controls in this phase.

---

## 11. Audit strategy

The following account management operations must emit audit events via `ITransactionalAuditWriter` in the same database transaction as the state change. This matches the established pattern from Phase F audit writer and Phase G password change.

| Operation | Proposed event_type | entity_type | action |
|---|---|---|---|
| View account (read-only) | None — no audit for reads | — | — |
| Activate account | ACCOUNT_ACTIVATE | USER_AUTH_ACCOUNT | ACTIVATE |
| Disable account | ACCOUNT_DISABLE | USER_AUTH_ACCOUNT | DISABLE |
| Lock account | ACCOUNT_LOCK | USER_AUTH_ACCOUNT | LOCK |
| Unlock account | ACCOUNT_UNLOCK | USER_AUTH_ACCOUNT | UNLOCK |
| Admin password reset | ACCOUNT_PASSWORD_RESET | USER_AUTH_ACCOUNT | RESET |
| Revoke all sessions | ACCOUNT_SESSIONS_REVOKED | USER_AUTH_ACCOUNT | REVOKE_SESSIONS |

Audit record must include:
- `actor_user_id` — the security admin performing the action (from JWT)
- `entity_id` — the target user_auth_account.id as string
- `company_id` — NULL (GLOBAL scope operations)
- No password hash, no temporary password, no refresh token material in audit data (SEC-005)

---

## 12. Test strategy

| Layer | Coverage target |
|---|---|
| Unit | `SecurityAdminService` account management methods: state machine transitions, rowversion conflict, business rule validation (cannot disable own account, cannot lock own account), password reset logic, session revocation |
| Integration | Account state transitions against PTKD_TEST_PHASE1B: activate→disable→activate, lock→unlock, reset issues new temp password and revokes all sessions, revoke-all-sessions clears Refresh_Tokens, audit records are written and are immutable |
| API | 401 unauthenticated, 403 without SECURITY_ACCOUNT_MANAGE, 404 account not found, 409 rowversion conflict, 204 success for all mutations, 200 + AccountDetailDto for GET; test actor must not be able to disable/lock own account |
| DatabaseSafety | SECURITY_ACCOUNT_MANAGE present in dbo.Permissions; test must not run against PTKD_DEV |
| Regression | All 472+ existing tests must remain green |

---

## 13. Security risks

| Risk | Severity | Mitigation |
|---|---|---|
| Admin resets another admin's password and captures temp password | High | Temp password displayed once and not stored in plaintext; audit records who performed reset; requires SECURITY_ACCOUNT_MANAGE which is restricted to the ADMIN_SECURITY group |
| Admin locks self out | Medium | API must reject lock/disable operations where actor_user_id == target account's user_id; unit test covers this |
| Admin revokes own sessions mid-operation | Low | Current request completes because auth check precedes session revocation; next request will require re-login |
| Rowversion race on account state change | Low | All mutations require rowversion; 409 on conflict; same pattern as existing controllers |
| Audit log bypassed on rollback | None | `ITransactionalAuditWriter` writes in the same transaction; if tx rolls back, audit is also rolled back (fail-closed) |
| Temporary password exposed in logs | High | Audit record must not contain the temporary password plaintext; server response must return it in the response body only, not in headers or logs; the API test must verify the audit record contains no password material |
| Account management API accessible without SECURITY_ACCOUNT_MANAGE | High | `[RequirePermission]` attribute on AccountsController at controller level; `PermissionAuthorizationFilter` enforces before handler; integration test must verify 403 without the permission |

---

## 14. Required Project Owner decisions

> **DEC-1B-I-01 — Phase shape:**
> Which option does the Project Owner select?
> - Option A: Backend only — implement Account Management APIs, no frontend.
> - Option B: Frontend only — login page + admin console, no account management backend.
> - Option C: Mixed — Account Management APIs + frontend authentication layer (stops before admin screen pages).
>
> **Recommendation**: Option A. Completes the backend security surface. Frontend is a separate subsequent phase.

> **DEC-1B-I-02 — MVP screens (if Option B or C chosen):**
> If frontend work is in scope, which screens are in Phase I?
> Candidates: (a) Login page + MustChangePassword flow only; (b) Login + roles/admin groups list views; (c) Login + full admin console.
> Recommendation if frontend included: (a) login + MustChangePassword only, to establish the auth layer without the full admin screen complexity.

> **DEC-1B-I-03 — Admin password reset response:**
> When an admin resets a user's password, how is the temporary password delivered?
> - Option A: Returned in the HTTP 200 response body and displayed once in a modal (caller's responsibility to transmit to the target user securely). Not stored in audit.
> - Option B: Sent directly to the target user's email (requires email service not yet implemented).
> - Option C: Admin must provide the new password value in the request body (admin chooses and communicates it out-of-band).
>
> **Recommendation**: Option A. Consistent with the bootstrap pattern (DEC-1B-010). Email is deferred. Option C gives admin direct control but risks weak password choices.
>
> **Required safeguards (apply regardless of which option is chosen):**
> - Temporary password is returned once only; never stored in plaintext in the database, logs, or audit record.
> - `must_change_password` must be set to `true` immediately.
> - Existing password policy (minimum length, maximum length) must apply.
> - Password history rules must apply (cannot reuse last 5 passwords).
> - The audit event for ACCOUNT_PASSWORD_RESET must not contain the temporary password value, old hash, or new hash (SEC-005).
> - Temporary password expiry (`temporary_password_expires_at = now + 24h`) must be set.
> - Response-body delivery (Option A) is accepted only because no email/SMS/secret-delivery channel exists yet. If an email service is added in a future phase, password reset delivery must be re-evaluated.

> **DEC-1B-I-04 — Account management permission code:**
> Should account management APIs use:
> - `SECURITY_ACCOUNT_MANAGE` (the original Phase 1B.0 design, already seeded in V0003, already in permission-catalog.md), or
> - `SECURITY_ADMIN_MANAGE` (the consolidated permission used by all existing D-B APIs, consistent with OD-D-B-03)?
>
> **Recommendation**: `SECURITY_ACCOUNT_MANAGE`. Account lifecycle operations (lock, disable, reset password, revoke sessions) are more sensitive than configuration operations and benefit from a distinct permission boundary. The database row already exists in V0003.
>
> **Constraint**: `PermissionCodes.cs` must not be modified until this decision is accepted. Do not add `SECURITY_ACCOUNT_MANAGE` to `PermissionCodes.cs` or reference it in any controller or service until DEC-1B-I-04 is formally recorded. Do not create a new permission code in the database — the DB row already exists in V0003 and must not be duplicated or modified.

> **DEC-1B-I-05 — Audit viewer UI:**
> Is a frontend audit viewer UI in scope for Phase I?
> The Phase H audit API is implemented. A frontend viewer page would require the frontend auth layer to exist first.
> - Yes (requires frontend work): add audit viewer as a Phase I deliverable.
> - No: defer audit viewer UI to Phase J or later.
>
> **Recommendation**: No. Audit viewer UI is deferred. Option A does not include frontend.

> **DEC-1B-I-06 — Auditing admin changes to admin accounts:**
> The ADMIN_SECURITY admin group members hold `SECURITY_ACCOUNT_MANAGE`. When one admin modifies another admin's account, should the audit record contain a `target_user_id` field or only `entity_id`?
> The current `Security_Audit_Events` schema has `actor_user_id` and `entity_id` (as VARCHAR). A separate `target_user_id` column does not exist.
> - Option A: Use `entity_id = target_account_id.ToString()` and `entity_type = 'USER_AUTH_ACCOUNT'` (current schema, no migration needed).
> - Option B: Add a `target_user_id` column to `Security_Audit_Events` (requires V0005 migration, schema change).
>
> **Recommendation**: Option A. No schema migration needed. The entity_id convention is consistent with existing audit events.

> **DEC-1B-I-07 — Confirmation and safety guards:**
> Must the account management API require a confirmation token or reason string for destructive operations (lock, disable, revoke-all-sessions)?
> - Yes: request body must include a non-empty `reason` field for lock, disable, and revoke-all-sessions.
> - No: no reason required; operations are logged in audit but no reason field in the request.
>
> **Recommendation**: Yes, require `reason` for lock, disable, and revoke-all-sessions. This aligns with `requires_reason = 1` in the SECURITY_ACCOUNT_MANAGE permission catalog entry, SEC-003 (reasons mandatory for sensitive permission changes), and GOV-007. Activate and unlock may also require reason for audit completeness. The request DTOs should include an optional `reason` field at minimum.

---

## 15. Blockers

| Item | Status | Detail |
|---|---|---|
| Schema blocker | None found | V0003 already contains User_Auth_Accounts, Password_History, Refresh_Tokens; no schema migration required |
| Migration blocker | None found | V0004 already seeds SECURITY_ADMIN_MANAGE; SECURITY_ACCOUNT_MANAGE row exists in V0003; no V0005 needed |
| SECURITY_ADMIN_MANAGE seed | Resolved (Phase F-B0) | V0004 applied |
| SECURITY_ACCOUNT_MANAGE seed | Resolved | Row exists in V0003 row 12 |
| Frontend auth prerequisite | Deferred (Option A) | No frontend in Option A scope |
| DEC-1B-I decisions | **Implementation blocked** | Implementation may not begin until Project Owner accepts DEC-1B-I-01 through DEC-1B-I-07. These decisions are not optional — each one gates a concrete implementation choice. |

**Implementation is blocked until Project Owner accepts DEC-1B-I-01 through DEC-1B-I-07.** No source code, tests, or `PermissionCodes.cs` changes may be made until the plan is accepted.

---

## 16. Recommended implementation slices (Option A)

These slices are ordered for incremental review. Each slice must be individually authorized and must not be merged until tests pass.

| Slice | Deliverable |
|---|---|
| I-1 | Add `SECURITY_ACCOUNT_MANAGE` to `PermissionCodes.cs` (gated on DEC-1B-I-04 acceptance). Add `AccountDetailDto`, `ActivateAccountRequest`, `DisableAccountRequest`, `LockAccountRequest`, `UnlockAccountRequest`, `AdminResetPasswordRequest`, `RevokeAllSessionsRequest` DTOs. Add account management method signatures to `ISecurityAdminService`. No implementation. Unit tests compile. |
| I-2 | Implement read: `GetAccountDetailAsync` in `SecurityAdminService`. Return safe fields from `User_Auth_Accounts` (no hash, no stamp). Implement `GET /api/v2/security/accounts/{id}` in `AccountsController`. Unit + API tests for 401, 403, 404, 200. |
| I-3 | Implement account status transitions: activate, disable, unlock. Service + controller. Each mutation: rowversion check, state validation, policy_version increment, transactional audit. Unit + integration + API tests. |
| I-4 | Implement account lock. Requires lockout_end calculation. Service + controller. Mutation: rowversion check, state validation, lockout_end write, policy_version increment, transactional audit. Tests include "cannot lock own account" guard. |
| I-5 | Implement revoke-all-sessions. Service: bulk mark Refresh_Tokens.revoked_at for all active tokens of the target account. Transactional audit. No rowversion needed (targets all tokens, not a single entity). API + integration tests. |
| I-6 | Implement admin password reset. Service: generate temp password, hash, update User_Auth_Accounts (password_hash, must_change_password=1, temporary_password_expires_at=now+24h), rotate security_stamp (invalidates existing JWT sessions), call revoke-all-sessions as part of same transaction. Return plaintext temp password in response (not in audit). Transactional audit with no password material. Unit + integration + API tests including "audit contains no password" assertion. |
| I-7 | DatabaseSafety test: verify SECURITY_ACCOUNT_MANAGE present in dbo.Permissions on PTKD_TEST_PHASE1B. Full regression run: all tests green. |

---

## 17. Acceptance criteria

For Phase 1B.1-I Option A to be considered complete, all of the following must be true:

| ID | Criterion |
|---|---|
| I-AC-01 | `GET /api/v2/security/accounts/{id}` returns 401 when unauthenticated. |
| I-AC-02 | `GET /api/v2/security/accounts/{id}` returns 403 when caller lacks SECURITY_ACCOUNT_MANAGE. |
| I-AC-03 | `GET /api/v2/security/accounts/{id}` returns 200 with `AccountDetailDto` when caller has SECURITY_ACCOUNT_MANAGE. Response contains no `password_hash`, no `security_stamp`. |
| I-AC-04 | `POST /api/v2/security/accounts/{id}/activate` returns 204, sets `auth_account_status = ACTIVE`, increments policy_version, writes ACCOUNT_ACTIVATE audit. |
| I-AC-05 | `POST /api/v2/security/accounts/{id}/disable` returns 204, sets `auth_account_status = DISABLED`, writes ACCOUNT_DISABLE audit. Returns 403 when actor is same user as target. |
| I-AC-06 | `POST /api/v2/security/accounts/{id}/lock` returns 204, sets `auth_account_status = LOCKED`, sets `lockout_end`, writes ACCOUNT_LOCK audit. Returns 403 when actor is same user as target. |
| I-AC-07 | `POST /api/v2/security/accounts/{id}/unlock` returns 204, clears `lockout_end`, sets `auth_account_status = ACTIVE`, writes ACCOUNT_UNLOCK audit. |
| I-AC-08 | `POST /api/v2/security/accounts/{id}/reset` returns 200 with temporary password in response body, sets `must_change_password = 1`, hashes new password, rotates security_stamp, revokes all sessions, writes ACCOUNT_PASSWORD_RESET audit. Audit record contains no password material. |
| I-AC-09 | `POST /api/v2/security/accounts/{id}/revoke-all-sessions` returns 204, marks all active Refresh_Tokens for target as revoked, writes ACCOUNT_SESSIONS_REVOKED audit. |
| I-AC-10 | All mutations return 409 when the supplied rowversion does not match the database rowversion (where rowversion applies). |
| I-AC-11 | All mutations return 404 when the account does not exist. |
| I-AC-12 | DatabaseSafety test confirms SECURITY_ACCOUNT_MANAGE exists in dbo.Permissions on PTKD_TEST_PHASE1B. |
| I-AC-13 | All existing tests (UnitTests, IntegrationTests, ApiTests, DatabaseSafety) remain green. Grand total must be equal to or greater than the Phase H total (472 tests). |
| I-AC-14 | No password hash, temporary password plaintext, or security_stamp appears in any audit record (verified by integration test). |
| I-AC-15 | `PermissionCodes.SecurityAccountManage = "SECURITY_ACCOUNT_MANAGE"` is present in PermissionCodes.cs. |

---

*Document prepared from direct code inspection of HEAD a8fdbe636e4a57429f8ba2d58652a27349a2989d on 2026-07-21.*
*No source code, tests, migrations, or committed documents were modified during the preparation of this plan.*
