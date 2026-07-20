# Phase 1B.1-E-C Organization API Permission Catalog Decisions and Enforcement Plan

## 1. Status
ACCEPTED PLAN — PHASE 1B.1-E-C IMPLEMENTATION MAY BE AUTHORIZED SEPARATELY

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

## 8. Recorded Project Owner decisions

**OD-E-C-01:**
Organization APIs use new organization-specific permission codes, not SECURITY_ADMIN_MANAGE.

**OD-E-C-02:**
Approve these canonical permission codes:
- ORGANIZATION_USER_MANAGE
- ORGANIZATION_DEPARTMENT_MANAGE
- ORGANIZATION_COMPANY_MANAGE

**OD-E-C-03:**
UsersController uses ORGANIZATION_USER_MANAGE with PermissionScope.Global for Phase 1B.

**OD-E-C-04:**
DepartmentsController uses ORGANIZATION_DEPARTMENT_MANAGE with PermissionScope.Global for Phase 1B. Company-scoped department enforcement is deferred until entity-company ownership validation is explicitly designed.

**OD-E-C-05:**
CompaniesController uses ORGANIZATION_COMPANY_MANAGE with PermissionScope.Global for Phase 1B.

**OD-E-C-06:**
After E-C plan acceptance, a separate implementation task may update permission-catalog.md, PermissionCodes constants, Organization controllers, and API tests. No seed/bootstrap/migration is authorized.

**OD-E-C-07:**
Read and mutation endpoints share the same manage permission in Phase 1B. Separate read/manage permissions are deferred.

**OD-E-C-08:**
No Organization API enforcement implementation is authorized until these decisions are recorded and accepted.

### Accepted E-C implementation direction:
- Update permission-catalog.md with the approved organization permission codes in a later implementation task.
- Add matching permission constants in code in a later implementation task.
- Apply [Authorize] and [RequirePermission] to UsersController, DepartmentsController, and CompaniesController in a later implementation task.
- Use PermissionScope.Global for all three Organization controller groups in Phase 1B.
- Add API tests proving unauthenticated requests return 401, authenticated users without the required permission return 403, and users with the required permission succeed.
- Do not introduce X-Company-Id requirements for these Organization APIs in Phase 1B.
- Do not create seed/bootstrap/migration in E-C.
- Do not use SECURITY_ADMIN_MANAGE for Organization APIs.
- Do not introduce separate read/manage permissions in Phase 1B.

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
