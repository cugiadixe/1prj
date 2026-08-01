# Phase 1B.3-B4-A CREATE_CUSTOMER Backend Pilot Implementation Acceptance Review

**Status:** PASSED — READY FOR PROJECT OWNER BACKEND IMPLEMENTATION ACCEPTANCE

**Reviewed implementation:** Phase 1B.3-B4-A — CREATE_CUSTOMER Backend Pilot Foundation
**Implementation commit:** 95eee27ff51003677c89707e1f9358ce0d135a86
**Implementation parent:** a68518d7edaa64d197baa320dfb7318e89b318a9
**Authorization commit:** a68518d7edaa64d197baa320dfb7318e89b318a9
**Implementation plan commit:** f118636cc0184e237273a13894d63d75d84924a0
**B4 plan acceptance commit:** 94912ee14c94240b9be8c50a4c807d3f8b31d0e6

**Exact committed file list:**
- A database/migrations/V0007__create_customer_change_request.sql
- A database/rollbacks/U0007__drop_customer_change_request.sql
- A src/backend/PTKD.Api/Controllers/CustomerProposalController.cs
- M src/backend/PTKD.Api/Program.cs
- M src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs
- M src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs
- A src/backend/PTKD.Application/Customers/DTOs/CustomerProposalDtos.cs
- A src/backend/PTKD.Application/Customers/Services/CreateCustomerExecutionHandler.cs
- A src/backend/PTKD.Application/Customers/Services/CustomerProposalService.cs
- A src/backend/PTKD.Application/Customers/Services/ICustomerProposalService.cs
- A src/backend/PTKD.Application/Customers/Validations/CustomerProposalValidators.cs
- A src/backend/PTKD.Application/Workflows/Services/IWorkflowExecutionHandler.cs
- A src/backend/PTKD.Application/Workflows/Services/WorkflowExecutionHandlerFactory.cs
- M src/backend/PTKD.Application/Workflows/Services/WorkflowRuntimeService.cs
- A src/backend/PTKD.Domain/Entities/CustomerChangeRequest.cs
- M src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs
- A src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerChangeRequestConfiguration.cs
- M tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs
- M tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs
- A tests/backend/PTKD.UnitTests/Customers/CreateCustomerExecutionHandlerTests.cs
- A tests/backend/PTKD.UnitTests/Customers/CustomerChangeRequestTests.cs
- A tests/backend/PTKD.UnitTests/Workflows/WorkflowExecutionHandlerFactoryTests.cs

**Accepted scope findings:**
- CREATE_CUSTOMER only implemented.
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
- My proposals endpoint implemented if included in backend scope.
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

**Accepted deferred scope:**
- No frontend source/tests changed.
- No My Requests.
- No action history/timeline.
- No reject.
- No CUSTOMER_MASTER_CHANGE.
- No Service/Payment/Merge/Card/Plot/ENTITY.
- No production migration/release.
- No business-rules.md change.
- No permission-catalog.md change.
- No acceptance-criteria.md change.

**Test evidence:**
- dotnet build src/backend/PTKD-ERP.sln — 0 errors, 0 warnings.
- dotnet test tests/backend/PTKD.UnitTests/ — 145 passed, 0 failed.
- dotnet test tests/backend/PTKD.IntegrationTests/ — 196 passed, 0 failed.
- dotnet test tests/backend/PTKD.ApiTests/ — 257 passed, 0 failed.
- MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder — passed.
- V0007/U0007 apply/rollback evidence.
- git diff --check — clean.

**Residual risks and follow-up:**
- Frontend proposal/status UX still not implemented; B4-B required.
- My Requests/action history/reject remain deferred.
- CUSTOMER_MASTER_CHANGE remains deferred.
- Reassign UX and user lookup remain future concerns.
- Execution failure retry UX may need frontend/status handling in B4-B.
- Production migration/release remains deferred.
- Future payload changes must preserve metadata-only exposure.

**Conclusion:**
PHASE 1B.3-B4-A CREATE_CUSTOMER BACKEND PILOT IMPLEMENTATION ACCEPTANCE REVIEW PASSED
