# Phase 1B.1-E-C Organization API Permission Catalog Decisions and Enforcement Plan

## 1. Status
DRAFT — AWAITING PROJECT OWNER REVIEW

## 2. Baseline
- Current accepted HEAD: 4b7194605adfc224a18037dae6878696ec09fbb6
- E-A accepted foundation.
- E-B accepted Security Admin API migration.
- Organization APIs not yet migrated.

## 3. Purpose
E-C exists to define canonical permission codes and scope decisions for Organization APIs before implementation. No Organization API enforcement may be implemented until these decisions are accepted.

## 4. Current Organization API audit table

| Controller | Action | Method / Route | `[Authorize]` | `[RequirePermission]` | Current effective protection | Reads/Mutates | Probable Scope | Candidate Code | Catalog Gap | Risk if unchanged |
|---|---|---|---|---|---|---|---|---|---|---|
| UsersController | Create | POST /api/v2/organizations/users | No | No | unauthenticated allowed | Mutates | GLOBAL or COMPANY | None | Missing USER_MANAGE | Critical (unauth user creation) |
| UsersController | Update | PUT /api/v2/organizations/users/{id} | No | No | unauthenticated allowed | Mutates | GLOBAL or COMPANY | None | Missing USER_MANAGE | Critical (unauth user modification) |
| UsersController | GetById | GET /api/v2/organizations/users/{id} | No | No | unauthenticated allowed | Reads | GLOBAL or COMPANY | None | Missing USER_VIEW/MANAGE | High (data leak) |
| UsersController | GetAll | GET /api/v2/organizations/users | No | No | unauthenticated allowed | Reads | GLOBAL or COMPANY | None | Missing USER_VIEW/MANAGE | High (data leak) |
| DepartmentsController | Create | POST /api/v2/organizations/departments | No | No | unauthenticated allowed | Mutates | COMPANY | None | Missing DEPT_MANAGE | Critical (unauth dept creation) |
| DepartmentsController | Update | PUT /api/v2/organizations/departments/{id} | No | No | unauthenticated allowed | Mutates | COMPANY | None | Missing DEPT_MANAGE | Critical (unauth dept modification) |
| DepartmentsController | UpdateStatus | PUT /api/v2/organizations/departments/{id}/status | No | No | unauthenticated allowed | Mutates | COMPANY | None | Missing DEPT_MANAGE | Critical (unauth dept modification) |
| DepartmentsController | GetById | GET /api/v2/organizations/departments/{id} | No | No | unauthenticated allowed | Reads | COMPANY | None | Missing DEPT_VIEW/MANAGE | High (data leak) |
| DepartmentsController | GetAll | GET /api/v2/organizations/departments | No | No | unauthenticated allowed | Reads | COMPANY | None | Missing DEPT_VIEW/MANAGE | High (data leak) |
| CompaniesController | Create | POST /api/v2/organizations/companies | No | No | unauthenticated allowed | Mutates | GLOBAL | None | Missing COMP_MANAGE | Critical (unauth comp creation) |
| CompaniesController | Update | PUT /api/v2/organizations/companies/{id} | No | No | unauthenticated allowed | Mutates | GLOBAL | None | Missing COMP_MANAGE | Critical (unauth comp modification) |
| CompaniesController | UpdateStatus | PUT /api/v2/organizations/companies/{id}/status | No | No | unauthenticated allowed | Mutates | GLOBAL | None | Missing COMP_MANAGE | Critical (unauth comp modification) |
| CompaniesController | GetById | GET /api/v2/organizations/companies/{id} | No | No | unauthenticated allowed | Reads | GLOBAL | None | Missing COMP_VIEW/MANAGE | High (data leak) |
| CompaniesController | GetAll | GET /api/v2/organizations/companies | No | No | unauthenticated allowed | Reads | GLOBAL | None | Missing COMP_VIEW/MANAGE | High (data leak) |

## 5. Permission catalog analysis
- Existing relevant codes: `SECURITY_ADMIN_MANAGE` is the only broad administrative permission currently available.
- `permission-catalog.md` does **not** contain explicit codes for user management, department management, or company management.
- Gaps: To properly secure these endpoints without inventing codes, canonical codes must be added to the catalog for Users, Departments, and Companies.

## 6. Recommended decision options

### Option A — Reuse SECURITY_ADMIN_MANAGE
- Pros: simple, already exists, avoids new codes.
- Cons: overbroad; mixes security-role administration with organization master-data administration.

### Option B — Add separate organization administration permissions
Suggested candidate codes for owner review only:
- `ORGANIZATION_USER_MANAGE`
- `ORGANIZATION_DEPARTMENT_MANAGE`
- `ORGANIZATION_COMPANY_MANAGE`
or a single broader:
- `ORGANIZATION_ADMIN_MANAGE`

*Note: These are proposed codes only and are not approved until Project Owner acceptance.*

### Option C — Hybrid
- `SECURITY_ADMIN_MANAGE` remains for security config.
- Organization master data uses separate organization permission codes.
- Some high-risk user-account/security-linking actions may still require `SECURITY_ADMIN_MANAGE` or dual control in a later phase.

## 7. Scope decision requirements
For each Organization API group, the Project Owner must decide:
- **GLOBAL or COMPANY scope**: Should users/departments/companies be managed globally or within a specific company boundary?
- **Read vs mutation split**: Do we need separate permissions for viewing vs modifying?
- **Company master data**: Is company management GLOBAL-only?
- **User management**: Is user management GLOBAL or company-scoped?
- **Department management**: Is department management company-scoped?
- **Read/Manage split**: Are separate read/manage permissions needed, or is manage-only enough for Phase 1B?

## 8. Proposed E-C decisions to ask Project Owner

**OD-E-C-01:** Decide whether Organization APIs will use `SECURITY_ADMIN_MANAGE` or new organization-specific permission codes.

**OD-E-C-02:** If new codes are approved, decide exact canonical permission codes to add to `permission-catalog.md`.

**OD-E-C-03:** Decide scope for UsersController: GLOBAL, COMPANY, or mixed by action.

**OD-E-C-04:** Decide scope for DepartmentsController: GLOBAL, COMPANY, or mixed by action.

**OD-E-C-05:** Decide scope for CompaniesController: GLOBAL, COMPANY, or mixed by action.

**OD-E-C-06:** Decide whether Phase E-C implementation will only update permission catalog/docs, or also enforce Organization APIs after a separate implementation authorization.

**OD-E-C-07:** Decide whether Organization read endpoints and mutation endpoints share one manage permission in Phase 1B, or require separate read/manage permissions.

**OD-E-C-08:** Confirm no Organization API enforcement implementation is authorized until the above decisions are accepted.

## 9. Recommended implementation slice after acceptance
- First update `permission-catalog.md` and code constants only after owner approval.
- Then, in a separate implementation task, apply `RequirePermission` and `Authorize` to Organization APIs endpoint-by-endpoint.
- Add tests for 401/403/200 and X-Company-Id behavior where company-scoped.

## 10. Explicit exclusions
- No implementation in this plan.
- No application code changes.
- No tests.
- No migration.
- No seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No tag/push.

## 11. Risks
- Leaving Organization APIs insufficiently protected (currently unauthenticated!).
- Inventing non-canonical permission codes.
- Overloading `SECURITY_ADMIN_MANAGE`.
- Choosing wrong GLOBAL vs COMPANY scope.
- Breaking existing Phase 1A API tests.
- Locking out legitimate admin workflows.
- Moving to Phase F before Organization API authorization is decided.

## 12. Acceptance criteria
- Plan accepted by Project Owner.
- OD-E-C decisions recorded.
- No implementation until separate authorization.
