# Phase 1B.1-D-A Permission Evaluator Foundation Implementation Evidence

Status:
IMPLEMENTED AND VERIFIED — AWAITING PROJECT OWNER ACCEPTANCE

## Commit Baseline

Baseline commit:
f74c3f8b4445dd8b90f3b9b2dbd8b3c7d585cf06

Initial implementation commit:
4a97defb721f41152b2f4fa7116ca9bb37ea0f75

Correction commit:
9b37078ef68ab4187773466eadfc868a408c4cea

Evidence document commit:
TO BE FILLED AFTER COMMIT

## Implemented Scope

Phase 1B.1-D-A implemented the server-side permission evaluator foundation only.

Implemented:
- Permission evaluator contract.
- Authorization database context contract.
- Permission evaluator implementation.
- V0003 authorization entity mappings.
- IMemoryCache-based evaluator caching.
- Fail-closed authorization behavior.
- Unit tests for permission evaluation.
- Integration tests against SQL Server test database.
- ApiTests regression correction for SafeTestWebApplicationFactory.

## Explicit Exclusions

The following were not implemented in Phase 1B.1-D-A:

- No role/admin-group/assignment management APIs.
- No SecurityController production endpoints.
- No middleware enforcement.
- No X-Company-Id enforcement.
- No permission authorization attributes.
- No permission list in JWT.
- No frontend.
- No audit writer.
- No V0004/U0004 migration.
- No production migration.

## Business Rules Implemented

Implemented permission evaluation rules:

- Unknown permission returns DENY.
- Inactive permission returns DENY.
- GLOBAL permission requires no company scope.
- COMPANY permission requires explicit company scope.
- Missing active assignment to requested company returns DENY.
- Individual DENY always wins over all grant sources.
- Admin Group grants are additive only.
- Individual ALLOW grants are additive only.
- Role grants are additive only.
- Department baseline grants are additive only.
- Multiple active department assignments are unioned.
- Expired assignments do not grant.
- Future-dated assignments do not grant.
- Evaluator fails closed on unexpected exception.

## Algorithm Summary

Effective permission formula:

department baseline
+ role grants
+ admin group grants
+ individual allow
- individual deny

DENY precedence:
Individual DENY overrides all other grant sources, including Admin Group, Role, Department baseline, and Individual ALLOW.

## V0003 Tables Used

The evaluator uses V0003 authorization tables only, including:

- Permissions
- Roles
- Role_Permissions
- Admin_Groups
- Admin_Group_Permissions
- User_Role_Assignments
- User_Admin_Group_Assignments
- User_Individual_Permissions
- Department_Permissions
- Authorization_Policy_State

No V0004 migration was created.

## Cache Strategy

The evaluator uses server-side IMemoryCache only.

Cache keys are scoped by:
- user id;
- company id or GLOBAL scope;
- authorization policy version.

Distributed cache is deferred.

## Fail-Closed Behavior

EvaluateAsync returns false when:
- permission is missing;
- permission is inactive;
- scope is invalid;
- company assignment is missing;
- a data access or unexpected exception occurs.

GetEffectivePermissionsAsync returns an empty permission list on unexpected exception.

No internal authorization failure reason is exposed to callers.

## Tests Added or Corrected

Unit test coverage includes:
- Individual DENY precedence.
- Admin Group grant.
- Individual ALLOW grant.
- Role grant.
- Department baseline grant.
- Multi-department union.
- Scope validation.
- Inactive permission behavior.
- Effective date behavior.
- Fail-closed behavior.
- GetEffectivePermissionsAsync union behavior.
- GetEffectivePermissionsAsync deny subtraction.
- GetEffectivePermissionsAsync inactive permission exclusion.
- GetEffectivePermissionsAsync fail-closed behavior.
- GetEffectivePermissionsAsync company scope behavior.

Integration test coverage includes:
- Real V0003 table read.
- Individual DENY precedence against real DB.
- Admin Group grant overridden by individual DENY against real DB.
- Multi-department union against real DB.
- Missing company assignment DENY against real DB.
- Inactive permission does not grant against real DB.
- Effective date boundaries against real DB.
- PTKD_TEST_PHASE1A2 database safety guard.

ApiTests correction:
- Removed destructive ResetToV0003 behavior from SafeTestWebApplicationFactory CreateHost path.
- WithWebHostBuilder-derived factories no longer reset/drop schema during ApiTests.

## Test Results

Build:
- Result: 0 errors.
- Warnings: existing warnings only.

UnitTests:
- Result: 0 failed.
- Exact count: record from final test run.

IntegrationTests:
- Result: 0 failed.
- Exact count: record from final test run.

ApiTests:
- Total: 88.
- Failed: 0.
- Passed: 88.
- Skipped: 0.

DatabaseSafety:
- Total: 17.
- Failed: 0.
- Passed: 17.
- Skipped: 0.

## Database Safety Evidence

- DB-writing tests use PTKD_TEST_PHASE1A2 only.
- InitialCatalog guard is required before writes.
- SELECT DB_NAME() guard expects PTKD_TEST_PHASE1A2.
- PTKD_DEV was not connected.
- No production migration was run.
- No V0004/U0004 exists.
- V0003/U0003 unchanged.

## Remaining Work After D-A

Remaining authorized future work:
- D-B Role/Admin Group/Assignment APIs.

Still not authorized:
- Phase E middleware enforcement.
- Phase F audit/bootstrap.
- Phase 1B.1-E through I.
- Production migration.

## Confirmation

Phase 1B.1-D-A implementation only.

Phase 1B.1-D full acceptance is not yet recorded.

Project Owner acceptance is still pending.

Production migration is not authorized.
