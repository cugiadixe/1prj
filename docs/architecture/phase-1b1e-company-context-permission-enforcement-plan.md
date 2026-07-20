# Phase 1B.1-E Company Context and Permission Enforcement Plan

## 1. Status
**DRAFT — AWAITING PROJECT OWNER REVIEW**

## 2. Baseline
- **Current accepted HEAD:** `7eb6f427f7dbfbbe2f88460077a824165e162526`
- **Prior Capabilities:** Phase 1B.1-C (Token session foundation, Authentication API, Protected Request Validation) and Phase 1B.1-D (Permission Evaluator Foundation, Security Administration APIs) are fully accepted and implemented.

## 3. Purpose
Phase E must connect authenticated requests to company context and enforce permissions on protected business endpoints. It bridges the gap between the valid JWT identities established in Phase C and the evaluation logic built in Phase D, using `IPermissionEvaluator` to gate API access based on the `X-Company-Id` header and the user's assigned permissions.

## 4. In-Scope
- Company context extraction from the `X-Company-Id` HTTP header.
- Validation of malformed or missing company header where company context is required.
- Standardized HTTP 400 responses for malformed or missing required `X-Company-Id` headers.
- Standardized HTTP 403 responses for authenticated users lacking active permission or company access.
- Endpoint-level permission enforcement using the established permission codes.
- Complete reuse of the existing `IPermissionEvaluator` engine.
- Authorization operates purely on database lookups (cached/optimized later if needed); no permission claims will be embedded in the JWT.
- Exclusions: No frontend integration, no audit writer (Phase F), no bootstrap/seed (Phase F), no production migrations, and no new migrations unless a specific plan blocker proves it necessary.

## 5. Out-of-Scope
- Auth login, refresh, or logout endpoint changes.
- Token or session lifecycle redesign.
- Re-writing the Permission Evaluator or Security Administration APIs.
- Phase F Audit Writer or Initial Admin Bootstrap.
- Frontend implementation (React/Vite).
- Active Directory / LDAP integration.
- Production environment deployments or seed/bootstrap scripts.
- Business module implementation beyond minimal test endpoints necessary to prove the enforcement mechanism.

## 6. Proposed Technical Design
- **Enforcement Mechanism:** Recommend using ASP.NET Core Policy-based authorization (e.g., `IAuthorizationRequirement` and `AuthorizationHandler`) combined with custom attributes (e.g., `[RequirePermission("CODE", Scope = PermissionScope.Company)]`).
- **Endpoint Metadata:** Endpoints will declare their required permissions via attributes. The authorization handler will intercept the request, read the attribute metadata, and execute the check.
- **GLOBAL vs COMPANY Evaluation:** If the endpoint metadata specifies a `COMPANY` scope, the handler mandates the presence and validity of `X-Company-Id`. If `GLOBAL` is specified, the handler ignores the header for that specific check.
- **Exclusion of Non-Company Endpoints:** Endpoints that do not require company context (like GLOBAL security admin endpoints) will explicitly declare `Scope = PermissionScope.Global`.
- **Exclusion of Auth Endpoints:** Public and authentication endpoints (`/api/v2/auth/*`) will be excluded either via `[AllowAnonymous]` or by not applying the custom permission policy.
- **D-B API Interaction:** The D-B Security Administration APIs currently utilize manual in-controller `IPermissionEvaluator` checks. We propose migrating them to the new attribute-based shared enforcement mechanism in Phase 1B.1-E-B to prevent double-enforcement and maintain consistency.
- **Failure Responses:** The authorization handler will interact with the Global Exception Filter or middleware to return sanitized `ProblemDetails` for 400 (Bad Request - missing header) and 403 (Forbidden).
- **Logging Constraints:** Technical failures will be logged to Serilog, but semantic business audit trails will be deferred to Phase F.

## 7. Proposed API Behavior
- **Missing/Malformed X-Company-Id (on COMPANY endpoints):** Returns HTTP 400 Bad Request.
- **Valid Company ID, but no active user assignment/access:** Returns HTTP 403 Forbidden.
- **Valid Company ID, but missing required permission:** Returns HTTP 403 Forbidden.
- **GLOBAL Permission Endpoints:** Evaluates permissions at the GLOBAL level. If `X-Company-Id` is provided, it is strictly ignored for the purpose of the global check.
- **Public/Auth Endpoints:** Proceed without permission or company context checks.
- **Security Admin Endpoints:** Will transition from manual controller checks to attribute-based enforcement, returning 403 if the executing user lacks the required security management permissions.

## 8. Testing Strategy
We will implement Unit, Integration, and API tests to guarantee enforcement without regressing existing C/D capabilities:
- **401 Unauthorized:** Missing or invalid JWT on protected endpoints.
- **400 Bad Request:** Missing `X-Company-Id` on COMPANY-scoped endpoints.
- **400 Bad Request:** Malformed `X-Company-Id` header (e.g., non-GUID/integer formatting depending on schema).
- **403 Forbidden:** Valid company ID provided, but the user has no active company assignment.
- **403 Forbidden:** Valid company assignment, but missing the required specific permission code.
- **200 OK:** Successful request when the correct permission and scope are met.
- **GLOBAL Context:** Verifying that a GLOBAL-scoped endpoint succeeds even if `X-Company-Id` is omitted.
- **Auth Exclusion:** Verifying auth endpoints remain reachable without company context.
- **Regression:** Complete run of all existing Unit, Integration, API, and DatabaseSafety tests.

## 9. Open Decisions Requiring Project Owner Approval
**OD-E-01:** Should Phase E enforce permissions through custom attributes/filters on selected endpoints (e.g., `[RequirePermission]`), or via a centralized route-permission registry configured at startup?
**OD-E-02:** Should `X-Company-Id` be strictly required for *all* authenticated non-auth endpoints globally, or only for endpoints explicitly marked as COMPANY-scoped?
**OD-E-03:** Should D-B Security Administration APIs remain manually enforced inside the controller bodies in Phase E, or be fully migrated to the shared attribute enforcement mechanism?
**OD-E-04:** For GLOBAL permission endpoints, if a client accidentally sends an `X-Company-Id` header, should the server ignore it, explicitly reject it (400), or treat it as optional?
**OD-E-05:** What is the initial endpoint set to protect in Phase E? (Options: Existing org APIs only, security APIs only, minimal dummy test endpoints only, or all currently protected endpoints?)
**OD-E-06:** Should a malformed company ID response include only generic 400 Bad Request text, or a specific sanitized error code (e.g., `ERR_MISSING_COMPANY_CONTEXT`)?
**OD-E-07:** Should Phase E introduce reusable permission attributes exactly formatted as `[RequirePermission("CODE", Scope = PermissionScope.COMPANY)]`?
**OD-E-08:** How should endpoints requiring multiple permissions behave? Should we support "any-of", "all-of", or defer multi-permission attribute support until a specific business case requires it?

## 10. Risks
- **Auth Endpoint Lockout:** Accidentally enforcing company context on authentication endpoints, preventing login.
- **Regression:** Breaking existing accepted C/D tests during middleware integration.
- **Double Enforcement:** Inconsistent behavior if manual controller checks conflict with the new shared attribute enforcement.
- **Over-enforcement:** Accidentally blocking endpoints before their final business permission mappings are documented and approved.
- **Missing Audit:** Administrative actions will lack semantic audit trails until Phase F is implemented.

## 11. Recommended Phase Slicing
To minimize risk and isolate testing, we recommend splitting Phase 1B.1-E into two slices:
- **Phase 1B.1-E-A:** Enforcement foundation + minimal dummy test endpoints only (proves the middleware and attributes work).
- **Phase 1B.1-E-B:** Apply enforcement strictly to existing Organization and Security endpoints, migrating them away from manual checks.
- *(Followed by)* **Phase 1B.1-F:** Audit / Bootstrap.

## 12. Acceptance Criteria
- Project Owner review and explicit acceptance of this plan is strictly required before any implementation begins.
- No implementation is performed in the commit containing this plan.
- No application code, tests, or migrations are changed.
