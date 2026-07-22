# Phase 1B.1-K Project Owner Resumption and Implementation Authorization

**Status:**
AUTHORIZED — PHASE 1B.1-K FRONTEND IMPLEMENTATION MAY BEGIN

**Authorized phase:**
Phase 1B.1-K — Security Account Management UI Foundation

**Original Phase K plan commit:**
d757315ba6c4ed53b75a270624372a08e34f33ce

**Original Phase K plan acceptance commit:**
2e5d85cbe1aad8cdde6605db76b2b5bf85b292fd

**K0 final acceptance commit:**
73f7153a4d4cd297d34913a623090a0de9bcc282

**Authorization baseline:**
73f7153a4d4cd297d34913a623090a0de9bcc282

---

## Reason for resumption

- Phase 1B.1-K plan identified B2 UserId-to-accountId mapping as a HIGH / hard blocker.
- Phase 1B.1-K implementation was not authorized until the blocker was resolved.
- Phase 1B.1-K0 implemented and final accepted Account Management discovery APIs.
- B2 is now resolved for frontend implementation purposes.

---

## Accepted K0 dependency

- `GET /api/v2/security/accounts` is available for list/search.
- `GET /api/v2/security/accounts/by-user/{userId:long}` is available for by-user lookup.
- Both use existing `SECURITY_ACCOUNT_MANAGE` at `GLOBAL` scope.
- No new permission was introduced.
- No schema migration was introduced.

---

## Implementation authorization

- Phase 1B.1-K frontend implementation may begin after this authorization is committed.
- Implementation must remain frontend-only.
- Implementation must consume accepted K0 endpoints.
- Implementation must not add backend APIs.
- Implementation must not add migrations or rollbacks.
- Implementation must not modify `PermissionCodes.cs`.
- Implementation must not modify `permission-catalog.md`.
- Implementation must not implement Permission Assignment UI.
- Implementation must not implement Role/Group Management UI.
- Implementation must not implement Audit Viewer UI.
- Implementation must not implement Dynamic Approval Workflow.

---

## Authorized frontend scope

- Account Management route under existing authenticated shell.
- Account list/search screen using `GET /api/v2/security/accounts`.
- Account detail/action screen using existing Phase I Account Management endpoints.
- Optional by-user lookup using `GET /api/v2/security/accounts/by-user/{userId:long}` if needed.
- Account status display.
- Activate account action.
- Disable account action with required reason.
- Lock account action with required reason.
- Unlock account action.
- Admin reset password action with required reason.
- One-time temporary password display after reset.
- Revoke all sessions action with required reason.
- Confirmation modals for security-sensitive actions.
- Sanitized error handling.
- Frontend tests for routing, API calls, reason validation, sanitized errors, temporary password display, and no sensitive persistence.

---

## Permission/navigation decision

- Full frontend permission-gated navigation remains deferred.
- No `/api/v2/auth/my-permissions` endpoint exists.
- Do not invent frontend authorization logic.
- Backend remains the authoritative enforcement layer.
- UI may show the Account Management route within the authenticated shell, but 403 responses must be handled safely and clearly.

---

## Security requirements

- Do not store access tokens in `localStorage`.
- Do not store access tokens in `sessionStorage`.
- Do not store temporary passwords.
- Do not log temporary passwords, access tokens, refresh tokens, CSRF tokens, password inputs, or raw auth payloads.
- Do not read `RefreshToken` from `document.cookie`.
- `document.cookie` may only be used for `X-CSRF-TOKEN` through existing CSRF utility.
- Display temporary password once after reset, then allow user to dismiss/clear it.
- Sanitize all backend errors.

---

## Required tests

- Account list/search route renders for authenticated user.
- Account list/search calls `GET /api/v2/security/accounts`.
- Pagination/search/filter UI behavior works.
- 403 response displays sanitized unauthorized message.
- Account actions call correct endpoints.
- Required reason validation for disable, lock, reset password, and revoke sessions.
- Confirmation modal appears for sensitive actions.
- Reset password displays temporary password once.
- Temporary password is not persisted.
- Logout/auth guard behavior remains intact.
- MustChangePassword route guard behavior remains intact.
- No `localStorage`/`sessionStorage` token persistence.
- No frontend console logging of secrets.

---

## Implementation conclusion

PHASE 1B.1-K FRONTEND IMPLEMENTATION AUTHORIZED — READY TO IMPLEMENT SECURITY ACCOUNT MANAGEMENT UI FOUNDATION

PHASE 1B.1-K IMPLEMENTATION ACCEPTED � SEE phase-1b1k-project-owner-implementation-acceptance.md
