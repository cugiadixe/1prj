# Phase 1B.1-K Project Owner Plan Acceptance

**Status:**
ACCEPTED — PHASE 1B.1-K PLAN REVIEW COMPLETE; IMPLEMENTATION NOT AUTHORIZED

**Accepted plan commit:**
d757315ba6c4ed53b75a270624372a08e34f33ce

**Plan baseline:**
f4dddc03250d69b54b657ff32a1183e2caaed1a0

**Accepted phase:**
Phase 1B.1-K — Security Account Management UI Foundation

**Previous completed phase:**
Phase 1B.1-J — Login UI and MustChangePassword UI Foundation (COMPLETE)

**Backend dependency:**
Phase 1B.1-I — Account Management API Hardening (COMPLETE)

---

## Plan acceptance

- The Phase 1B.1-K plan has been reviewed and accepted.
- The plan correctly identifies 3 blockers (B1, B2, B3) and their resolution paths.
- Blocker B2 (UserId to accountId mapping) is accepted as a HARD BLOCKER for a usable Account Management UI.
- The plan correctly identifies the cross-permission dependency between ORGANIZATION_USER_MANAGE and SECURITY_ACCOUNT_MANAGE.
- The plan correctly identifies the absence of a `my-permissions` endpoint.
- The plan correctly scopes Phase K as frontend-only, consuming existing Phase 1B.1-I APIs.
- The plan correctly defers permission-gated navigation.
- The plan correctly requires confirmation modals and reason input for security-sensitive actions.

## Implementation authorization

**Phase 1B.1-K frontend implementation is NOT authorized.**

Implementation may not begin until:
1. Blocker B2 resolution path is implemented (Phase K0 recommended).
2. A separate implementation authorization document is committed after B2 resolution.

This acceptance authorizes the plan structure, decisions, and scope. It does not authorize code implementation.

---

## Accepted decisions

**DEC-1B-K-01 — Phase shape:**
Accepted: Frontend-only Account Management UI using existing Phase 1B.1-I APIs. Phase K remains planning-only until account discovery (Blocker B2) is resolved. Backend changes only if a blocker is accepted and approved.

---

**DEC-1B-K-02 — Account discovery/navigation:**
Accepted: Option C — Open Phase 1B.1-K0 (Account Management Discovery API) before Phase K implementation begins. K0 adds a SECURITY_ACCOUNT_MANAGE-scoped discovery contract. Phase K then builds full UI on top of a proper discovery API.

Phase K0 scope to be decided:
- `GET /api/v2/security/accounts?search=...` (list/search with pagination), and/or
- `GET /api/v2/security/accounts/by-user/{userId}` (lookup by user ID), and/or
- Another approved SECURITY_ACCOUNT_MANAGE-scoped discovery contract.

Detail-by-known-accountId-only UI (Option A) is NOT accepted as the primary administration interface. If Phase K0 is delayed, the Project Owner may revisit this decision.

---

**DEC-1B-K-03 — Permission-gated UI:**
Accepted: Option A — No permission gating in Phase K. All authenticated users see the Account Management link. Backend enforces 403. Frontend displays "Permission denied" when 403 is returned. No frontend authorization logic is invented. No `my-permissions` endpoint exists.

---

**DEC-1B-K-04 — Security-sensitive action UX:**
Accepted: Confirmation modal plus required reason input for disable, lock, reset password, and revoke sessions. Consistent with backend reason validation (DEC-1B-I-07) and SEC-003.

---

**DEC-1B-K-05 — Temporary password display:**
Accepted: Display temporary password once in modal with copy button after admin reset password. Do not log to console. Do not persist to any storage. Clear from component state on modal close. Consistent with DEC-1B-I-03.

---

**DEC-1B-K-06 — Audit visibility:**
Accepted: No audit history or audit event links in Phase K. Defer to Audit Viewer UI phase.

---

**DEC-1B-K-07 — Backend changes:**
Accepted: No backend changes in Phase K. Use Phase K0 for the account discovery API backend addition. Phase K is frontend-only. Stop and request PO approval before any backend change.

---

## Accepted blockers

| ID | Blocker | Severity | Accepted resolution |
|---|---|---|---|
| B1 | No account list/search endpoint | Medium | Phase K0 adds list endpoint |
| B2 | **HARD BLOCKER.** No UserId to accountId mapping API | **HIGH** | Phase K0 adds SECURITY_ACCOUNT_MANAGE-scoped discovery contract |
| B3 | No `my-permissions` endpoint | Low for Phase K | Defer permission-gated navigation; backend enforces 403 |

---

## Accepted scope boundaries

- Frontend-only (after K0 completes).
- Consumes Phase 1B.1-I Account Management APIs (7 endpoints).
- Consumes Phase K0 Account Management Discovery API (to be implemented).
- No backend changes in Phase K.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No database migration.
- No rollback migration.
- No Security Admin UI beyond Account Management.
- No Permission Assignment UI.
- No Audit Viewer UI.
- No Dynamic Approval Workflow.

---

## Phase K0 authorization

**Phase 1B.1-K0 — Account Management Discovery API is authorized for planning.**

K0 planning may begin immediately. K0 is a small, well-bounded backend addition that:
- Adds SECURITY_ACCOUNT_MANAGE-scoped account discovery endpoints.
- Follows the existing AccountManagementService pattern from Phase 1B.1-I.
- Resolves Blocker B2 before Phase K implementation.

K0 implementation requires its own plan acceptance before code changes begin.

---

## Next steps

1. Plan Phase 1B.1-K0 — Account Management Discovery API.
2. Implement Phase K0 after plan acceptance.
3. After K0 completion, authorize Phase K implementation.

PHASE 1B.1-K PLAN REVIEW ACCEPTED — READY TO PLAN PHASE 1B.1-K0
