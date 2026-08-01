# Phase 1B.3-B4 CREATE_CUSTOMER Pilot Project Owner Final Acceptance

## Status
ACCEPTED — PHASE 1B.3-B4 CREATE_CUSTOMER PILOT COMPLETE

## Accepted closure scope
Phase 1B.3-B4 — CREATE_CUSTOMER Workflow Pilot Integration

## Accepted full closure review commit
ee88939869c055eaa73485e404524d31e7fd8429

## Accepted B4-B Project Owner frontend acceptance commit
9e678c0241ac539ee3f576b40e3c3d10edd6568b

## Accepted B4-B frontend implementation acceptance review commit
7205bf394fcfb959dbb0fdf6015ccf474761bf1c

## Accepted B4-B frontend implementation commit
1464dbb5d47f24011e3aa0ec866b2ac7a149d997

## Accepted B4-A Project Owner backend acceptance commit
17e049d0f791dc8e0b1056d18d856a31a82c69f8

## Accepted B4-A backend implementation acceptance review commit
051234ea37f282ba07ec33a95755a57101113577

## Accepted B4-A backend implementation commit
95eee27ff51003677c89707e1f9358ce0d135a86

## B4 implementation authorization commit
a68518d7edaa64d197baa320dfb7318e89b318a9

## B4 CREATE_CUSTOMER implementation plan commit
f118636cc0184e237273a13894d63d75d84924a0

## B4 Project Owner plan acceptance commit
94912ee14c94240b9be8c50a4c807d3f8b31d0e6

## B4 discovery / detailed plan commit
93607eb57c4a4aee3f2dd0ecba8a00135f3db87e

## Project Owner decision
The Project Owner accepts Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot Integration as complete.

## Accepted final B4 scope
- CREATE_CUSTOMER pilot only.
- Workflow-backed customer creation proposal path.
- Existing direct customer create path preserved.
- Direct-create coexistence Option A preserved.
- Backend proposal API implemented.
- CustomerChangeRequest persistence implemented.
- Workflow instance linkage implemented.
- WorkflowInstance.BusinessEntityType uses CustomerChangeRequest.
- WorkflowInstance.BusinessEntityId uses CustomerChangeRequest ID.
- Final approval execution handler implemented.
- Idempotent final customer creation after approval implemented.
- Frontend proposal create/status/my-proposals UX implemented.
- Workflow instance link implemented.
- Existing My Approvals UI reused for approval actions.
- Safe payload metadata-only strategy implemented.
- B4 is internal/limited pilot, not production release.

## Accepted backend evidence
- `dotnet build src/backend/PTKD-ERP.sln` — 0 errors, 0 warnings.
- `dotnet test tests/backend/PTKD.UnitTests/` — 145 passed, 0 failed.
- `dotnet test tests/backend/PTKD.IntegrationTests/` — 196 passed, 0 failed.
- `dotnet test tests/backend/PTKD.ApiTests/` — 257 passed, 0 failed.
- `MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder` — passed.
- V0007/U0007 apply/rollback evidence accepted.
- `git diff --check` — clean.

## Accepted frontend evidence
- `npx oxlint` — passed.
- `npx tsc -b` — passed, 0 errors.
- `npx vitest run` — 40 test files passed, 345 tests passed, 0 failed.
- `git diff --check` — clean.

## Accepted safety and permission findings
- Backend remains authoritative.
- Frontend gates are UX/navigation only.
- DENY wins preserved.
- No super-admin bypass introduced.
- Safe payload metadata-only strategy implemented.
- No raw PayloadJson/BeforeDataJson exposure.
- No sensitive identity/phone/address exposure in proposal summary.
- No sensitive proposal persistence in browser storage.
- permission-catalog.md unchanged.
- business-rules.md unchanged.
- acceptance-criteria.md unchanged.

## Accepted deferred scope
- My Requests UI/API.
- Action history/timeline UI/API.
- Reject UI/API.
- CUSTOMER_MASTER_CHANGE.
- Service module integration.
- Payment module integration.
- Merge flow.
- Card flow.
- Plot flow.
- ENTITY scope expansion.
- Export/download features.
- Production migration.
- Production release.
- Active instance migration.
- Operational execution retry UX hardening.
- User lookup/reassign UX improvements.

## Known residual risks
- B4 pilot still needs operational validation in real usage.
- Execution failure retry UX may require later enhancement.
- My Requests/action history/reject remain deferred and may affect user experience.
- CUSTOMER_MASTER_CHANGE is not covered by this pilot.
- Service/Payment/Merge/Card/Plot integration remains future work.
- Production migration/release remains deferred.
- Future payload expansion must preserve metadata-only display and audit discipline.

## Next authorized task
Proceed to post-B4 next-work selection / roadmap review.
Do not start implementation of a new module until the next work item is explicitly selected and accepted.

## Conclusion
PHASE 1B.3-B4 CREATE_CUSTOMER PILOT COMPLETE
