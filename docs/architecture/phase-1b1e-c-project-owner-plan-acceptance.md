# Phase 1B.1-E-C Project Owner Plan Acceptance

## Status
PHASE 1B.1-E-C PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## Accepted plan commit
66dc7c9637c35bece214a1c5d0ebec91c6e612de

## Current accepted baseline
4b7194605adfc224a18037dae6878696ec09fbb6

## Accepted next slice
Phase 1B.1-E-C — Organization API Permission Catalog Decisions and Enforcement Plan

## Project Owner Decisions

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

## Explicit non-authorization
- No implementation in this commit.
- No application code changes.
- No tests changed.
- No permission-catalog.md update yet.
- No PermissionCodes constants yet.
- No migration.
- No seed/bootstrap.
- No Phase F audit writer.
- No frontend.
- No business module implementation.
- No production deployment.
- No tag/push.

## Next step
E-C implementation requires a separate implementation authorization prompt.
