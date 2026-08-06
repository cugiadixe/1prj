# Phase 1B.3-B4-A CREATE_CUSTOMER Backend Pilot Project Owner Implementation Acceptance

**Status:** ACCEPTED — PHASE 1B.3-B4-A CREATE_CUSTOMER BACKEND PILOT ACCEPTED

**Accepted implementation:** Phase 1B.3-B4-A — CREATE_CUSTOMER Backend Pilot Foundation

**Accepted backend implementation commit:** 95eee27ff51003677c89707e1f9358ce0d135a86

**Accepted backend implementation acceptance review commit:** 051234ea37f282ba07ec33a95755a57101113577

**Authorization commit:** a68518d7edaa64d197baa320dfb7318e89b318a9

**Implementation plan commit:** f118636cc0184e237273a13894d63d75d84924a0

**Project Owner decision:** The Project Owner accepts the Phase 1B.3-B4-A CREATE_CUSTOMER backend pilot implementation.

**Accepted backend scope:**
- CREATE_CUSTOMER only.
- Backend/database foundation implemented.
- Direct-create coexistence Option A preserved.
- Existing direct customer create unchanged.
- CUSTOMER_CHANGE_REQUEST_CREATE wired in PermissionCodes.cs only.
- permission-catalog.md unchanged.
- CustomerProposalController implemented.
- CustomerProposalService implemented.
- CustomerChangeRequest entity/linkage implemented.
- WorkflowInstance.BusinessEntityType uses CustomerChangeRequest.
- WorkflowInstance.BusinessEntityId uses CustomerChangeRequest ID.
- V0007/U0007 implemented.
- Execution handler implemented.
- Execution handler idempotent.
- APR-008/APR-009 preserved.
- Safe payload metadata-only strategy implemented.
- No frontend implementation included.

**Accepted API/backend behavior:**
- Proposal submit endpoint implemented.
- Proposal status/detail endpoint implemented.
- My proposals endpoint implemented.
- Proposal creates workflow instance for CREATE_CUSTOMER.
- Proposal creates stable BusinessEntityId before workflow instance creation.
- Final approval triggers execution.
- Customer is created only after final approval.
- Created CustomerId is linked back to proposal.
- Duplicate retry does not create duplicate customer.

**Accepted database/migration behavior:**
- V0007 creates Customer_Change_Requests.
- U0007 drops Customer_Change_Requests.
- Migration guards and rollback ordering are correct.
- Test database known-table guard updated safely.
- Customer_Change_Requests is dropped in FK-safe order.
- No production migration/release included.

**Accepted permission behavior:**
- CUSTOMER_CHANGE_REQUEST_CREATE wired.
- No permission-catalog.md change.
- No unrelated permission codes added.
- Backend remains authoritative.
- DENY wins remains backend-enforced.

**Accepted safety behavior:**
- No raw PayloadJson display/logging.
- No BeforeDataJson display/logging.
- No CCCD/identity number in safe summary/audit.
- No phone/address in safe summary/audit.
- No raw proposal JSON in logs.
- No sensitive data logging.
- Sanitized errors preserved.

**Accepted test evidence:**
- dotnet build src/backend/PTKD-ERP.sln — 0 errors, 0 warnings.
- dotnet test tests/backend/PTKD.UnitTests/ — 145 passed, 0 failed.
- dotnet test tests/backend/PTKD.IntegrationTests/ — 196 passed, 0 failed.
- dotnet test tests/backend/PTKD.ApiTests/ — 257 passed, 0 failed.
- MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder — passed.
- V0007/U0007 apply/rollback evidence accepted.
- git diff --check — clean.

**Accepted deferred scope:**
- Frontend proposal/status UX remains not implemented and is deferred to B4-B.
- My Requests remains deferred.
- Action history/timeline remains deferred.
- Reject remains deferred.
- CUSTOMER_MASTER_CHANGE remains deferred.
- Service/Payment/Merge/Card/Plot/ENTITY remain deferred.
- Production migration/release remains deferred.

**Accepted next task:**
Proceed to Phase 1B.3-B4-B CREATE_CUSTOMER frontend proposal/status UX implementation.

**Conclusion:**
PHASE 1B.3-B4-A CREATE_CUSTOMER BACKEND PILOT ACCEPTED
