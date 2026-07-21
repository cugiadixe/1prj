# Phase 1B.1-F Project Owner Plan Acceptance

## Status
PHASE 1B.1-F PLAN ACCEPTED — IMPLEMENTATION NOT YET STARTED

## Accepted plan commit
076b656c40bd7cbe437671580f45b9edc4ae6c29

## Current accepted baseline
0e7017f4b9f218bbe6f082a649eab5e046ef13be

## Accepted phase
Phase 1B.1-F — Audit Writer and Initial Admin Bootstrap

## Accepted slice sequencing
- F-A: Audit Writer foundation first.
- F-B: Initial Admin Bootstrap second.
- F-A must be planned, implemented, reviewed, and accepted before F-B implementation begins.
- No combined large implementation commit.

## Accepted discovery findings

- `Security_Audit_Events` table already exists in V0003.
- `Security_Bootstrap_State` table already exists in V0003.
- No C# audit writer/entity/service implementation exists yet.
- No C# bootstrap command/service implementation exists yet.
- Existing tests are schema-level only; application-layer audit/bootstrap tests are pending.
- Active test database remains `PTKD_TEST_PHASE1A2`.
- No new migration is expected unless later implementation inspection proves otherwise.

## Accepted decisions

**OD-F-01:**
Use write-record / append-only model for SecurityAuditEvent. Do not use normal tracked mutable EF entity flow for audit writes.

**OD-F-02:**
Create IAuditWriter contract in the application security boundary, such as Application/Security/Audit, unless implementation inspection finds a more consistent existing location.

**OD-F-03:**
Audit writer uses direct SQL insert into Security_Audit_Events, not normal EF tracked update flow.

**OD-F-04:**
Audit write failure is fail-closed for Phase F security/bootstrap operations. If a required audit event cannot be written, the protected operation must not be treated as successfully completed. Error responses must be sanitized.

**OD-F-05:**
SecurityBootstrapState is mapped through controlled persistence for one-time bootstrap state. It must not be exposed through a public API.

**OD-F-06:**
Initial Admin Bootstrap is delivered as PTKD.Bootstrap console/internal operational project, not as API startup behavior and not as a public API endpoint.

**OD-F-07:**
Bootstrap secret input may use environment variables for Phase F, but plaintext secrets must never be logged, printed, returned, persisted, or included in audit records. No default password is allowed.

**OD-F-08:**
Use BOOTSTRAP_ADMIN_CREATED as the canonical audit event code for successful bootstrap admin creation.

**OD-F-09:**
Continue using PTKD_TEST_PHASE1A2 for Phase F tests.

**OD-F-10:**
Split Phase F implementation:
- F-A: Audit Writer foundation.
- F-B: Initial Admin Bootstrap.
F-A must be planned, implemented, reviewed, and accepted before F-B implementation begins.

**OD-F-11:**
Application runtime uses PTKD_Security_Audit_Runtime role for audit insert behavior according to the existing database security design.

**OD-F-12:**
Explicit exclusions remain:
- No audit read endpoint.
- No SECURITY_AUDIT_VIEW endpoint enforcement in F-A/F-B.
- No new migration unless a later accepted implementation plan proves it is required.
- No PermissionCodes.cs changes.
- No frontend.
- No business module implementation.
- No AD/LDAP.
- No line-ending normalization.

## Explicit non-authorization

- No implementation in this commit.
- No application code changes.
- No tests changed.
- No migration.
- No seed/bootstrap.
- No Audit Writer implementation.
- No Initial Admin Bootstrap implementation.
- No public bootstrap endpoint.
- No audit read endpoint.
- No SECURITY_AUDIT_VIEW enforcement.
- No PermissionCodes.cs change.
- No permission-catalog.md change.
- No frontend.
- No business module implementation.
- No line-ending normalization.
- No production deployment.
- No tag/push.

## Next step
Create a separate Phase 1B.1-F-A Audit Writer foundation implementation authorization after this plan acceptance is committed.
