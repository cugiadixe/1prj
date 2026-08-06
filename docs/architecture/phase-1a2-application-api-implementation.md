# Phase 1A.2 Implementation Report

## Summary
The Phase 1A.2 implementation for the Organization Domain Application and API is complete. The application logic, infrastructure, and REST APIs have been successfully developed and thoroughly reconciled against the approved plan. All environment protection, database retry logic, and transaction invariants have been explicitly tested.

## Files Implemented

### Domain
- `src/backend/PTKD.Domain/Entities/Company.cs`
- `src/backend/PTKD.Domain/Entities/Department.cs`
- `src/backend/PTKD.Domain/Entities/User.cs`
- `src/backend/PTKD.Domain/Entities/UserCompanyAssignment.cs`
- `src/backend/PTKD.Domain/Entities/UserDepartmentAssignment.cs`
- `src/backend/PTKD.Domain/Entities/EmploymentHistory.cs`
- `src/backend/PTKD.Domain/ValueObjects/RowVersion.cs`
- `src/backend/PTKD.Domain/ValueObjects/AssignmentTimeline.cs`
- `src/backend/PTKD.Domain/Services/HierarchyCycleDetector.cs`

### Application (DTOs and Validators)
Exact inventory of request types and validators:

**Organizations/Companies**
- `DTOs/CompanyDto.cs`
- `DTOs/CreateCompanyRequest.cs`
- `DTOs/UpdateCompanyRequest.cs`
- `DTOs/UpdateCompanyStatusRequest.cs`
- `Validations/CompanyValidators.cs` (contains CreateCompanyRequestValidator, UpdateCompanyRequestValidator, UpdateCompanyStatusRequestValidator)

**Organizations/Departments**
- `DTOs/DepartmentDto.cs`
- `DTOs/CreateDepartmentRequest.cs`
- `DTOs/UpdateDepartmentRequest.cs`
- `DTOs/UpdateDepartmentStatusRequest.cs`
- `Validations/DepartmentValidators.cs` (contains CreateDepartmentRequestValidator, UpdateDepartmentRequestValidator, UpdateDepartmentStatusRequestValidator)

**Organizations/Users**
- `DTOs/UserDto.cs`
- `DTOs/CreateUserRequest.cs`
- `DTOs/UpdateUserRequest.cs`
- `Validations/UserValidators.cs` (contains CreateUserRequestValidator, UpdateUserRequestValidator)

**Organizations/Assignments**
- `DTOs/AssignCompanyRequest.cs`
- `DTOs/AssignDepartmentRequest.cs`
- `DTOs/ChangePrimaryCompanyRequest.cs`
- `DTOs/ChangePrimaryDepartmentRequest.cs`
- `DTOs/CloseCompanyAssignmentRequest.cs`
- `DTOs/SameCompanyDepartmentTransferRequest.cs`
- `DTOs/CrossCompanyTransferRequest.cs`
- `Validations/AssignmentValidators.cs` (contains all 7 corresponding validators)

### Infrastructure
- `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs`
- `src/backend/PTKD.Infrastructure/Persistence/AppDbContextFactory.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Interceptors/AppendOnlyInterceptor.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Retries/DeadlockRetryPolicy.cs`

### API
- `src/backend/PTKD.API/Program.cs`
- `src/backend/PTKD.API/Controllers/CompaniesController.cs`
- `src/backend/PTKD.API/Controllers/DepartmentsController.cs`
- `src/backend/PTKD.API/Controllers/UsersController.cs`
- `src/backend/PTKD.API/Controllers/UserAssignmentsController.cs`
- `src/backend/PTKD.API/Filters/GlobalExceptionFilter.cs`
- `src/backend/PTKD.API/Filters/ValidationFilter.cs`

## Approved-Versus-Actual Route Comparison

| Approved Route (from Plan) | Actual Route | Controller Method | Status | Reason for Deviation |
|---|---|---|---|---|
| `POST /companies` | `POST /api/v2/organizations/companies` | `CompaniesController.CreateCompany` | MATCH | N/A |
| `PUT /companies/{id}` | `PUT /api/v2/organizations/companies/{id}` | `CompaniesController.UpdateCompany` | MATCH | N/A |
| `PUT /companies/{id}/status` | `PUT /api/v2/organizations/companies/{id}/status` | `CompaniesController.UpdateStatus` | MATCH | N/A |
| `GET /companies` | `GET /api/v2/organizations/companies` | `CompaniesController.ListCompanies` | MATCH | N/A |
| `GET /companies/{id}` | `GET /api/v2/organizations/companies/{id}` | `CompaniesController.GetCompanyById` | MATCH | N/A |
| `POST /departments` | `POST /api/v2/organizations/departments` | `DepartmentsController.CreateDepartment` | MATCH | N/A |
| `PUT /departments/{id}` | `PUT /api/v2/organizations/departments/{id}` | `DepartmentsController.UpdateDepartment` | MATCH | N/A |
| `PUT /departments/{id}/status` | `PUT /api/v2/organizations/departments/{id}/status` | `DepartmentsController.UpdateStatus` | MATCH | N/A |
| `GET /departments` | `GET /api/v2/organizations/departments` | `DepartmentsController.ListDepartments` | MATCH | N/A |
| `GET /departments/{id}` | `GET /api/v2/organizations/departments/{id}` | `DepartmentsController.GetDepartmentById` | MATCH | N/A |
| `POST /users` | `POST /api/v2/organizations/users` | `UsersController.CreateUser` | MATCH | N/A |
| `PUT /users/{id}` | `PUT /api/v2/organizations/users/{id}` | `UsersController.UpdateUser` | MATCH | N/A |
| `GET /users` | `GET /api/v2/organizations/users` | `UsersController.ListUsers` | MATCH | N/A |
| `GET /users/{id}` | `GET /api/v2/organizations/users/{id}` | `UsersController.GetUserById` | MATCH | N/A |
| `POST /users/{id}/companies` | `POST /api/v2/organizations/users/{userId}/companies` | `UserAssignmentsController.AssignCompany` | MATCH | N/A |
| `POST /users/{id}/departments` | `POST /api/v2/organizations/users/{userId}/departments` | `UserAssignmentsController.AssignDepartment` | MATCH | N/A |
| `PUT /users/{userId}/company-assignments/{companyAssignmentId}/primary` | `PUT /api/v2/organizations/users/{userId}/company-assignments/{companyAssignmentId}/primary` | `UserAssignmentsController.ChangePrimaryCompany` | MATCH | N/A |
| `PUT /users/{userId}/department-assignments/{departmentAssignmentId}/primary` | `PUT /api/v2/organizations/users/{userId}/department-assignments/{departmentAssignmentId}/primary` | `UserAssignmentsController.ChangePrimaryDepartment` | MATCH | N/A |
| `PUT /users/{userId}/company-assignments/{companyAssignmentId}/close` | `PUT /api/v2/organizations/users/{userId}/company-assignments/{companyAssignmentId}/close` | `UserAssignmentsController.CloseCompanyAssignment` | MATCH | N/A |
| `POST /users/{userId}/company-assignments/{companyAssignmentId}/transfer/same-company` | `POST /api/v2/organizations/users/{userId}/company-assignments/{companyAssignmentId}/transfer/same-company` | `UserAssignmentsController.TransferSameCompany` | MATCH | N/A |
| `POST /users/{userId}/company-assignments/{sourceCompanyAssignmentId}/transfer/cross-company` | `POST /api/v2/organizations/users/{userId}/company-assignments/{sourceCompanyAssignmentId}/transfer/cross-company` | `UserAssignmentsController.TransferCrossCompany` | MATCH | N/A |

*Note: The actual route inventory perfectly matches the approved plan.*

## Requirement-to-Test Matrix

| Requirement | Test Method Mapping |
|---|---|
| Company CRUD/status/query | `OrganizationApiTests`: `Company_Create_Valid_Returns201`, `Company_Update_Valid_Returns200`, `Company_Status_Update_Returns200`, `Company_List_Returns200`, `Company_GetById_Returns200`, `Company_GetById_Missing_Returns404` |
| Department CRUD/status/query | `OrganizationApiTests`: `Department_Create_Valid_Returns201`, `Department_Update_Valid_Returns200`, `Department_Status_Update_Returns200`, `Department_List_ByCompanyId_Returns200`, `Department_GetById_Returns200` |
| User CRUD/query | `OrganizationApiTests`: `User_Create_Valid_Returns201`, `User_Update_Valid_Returns200`, `User_List_Returns200`, `User_GetById_Returns200` |
| Assign Company | `OrganizationApiTests`: `AssignCompany_Valid_Returns204`, `AssignCompany_InactiveCompany_Rejected`, `AssignCompany_DuplicateActive_Returns409` |
| Assign Department | `OrganizationApiTests.AssignDepartment_Valid_CreatesActiveAssignment`, `OrganizationApiTests.AssignDepartment_WrongRouteUser_Rejected`, `OrganizationApiTests.AssignDepartment_InactiveDepartment_Rejected`, `OrganizationApiTests.AssignDepartment_MismatchCompany_Rejected`, `OrganizationApiTests.AssignDepartment_ClosedCompanyAssignment_Rejected`, `OrganizationApiTests.AssignDepartment_RequiresCompanyAssignmentRowVersion`, `OrganizationApiTests.AssignDepartment_DuplicateActive_Returns409`, `OrganizationApiTests.AssignDepartment_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP`, `OrganizationApiTests.AssignDepartment_CorrectUserCompanyAssignmentId_DepartmentId_EffectiveDate`, `OrganizationApiTests.AssignDepartment_EmploymentHistories_IsWritten`, `OrganizationApiTests.AssignDepartment_Valid_IsAlwaysNonPrimary`, `OrganizationApiTests.AssignDepartment_StaleCompanyAssignmentRowVersion_Returns409` |
| Change Primary Company | `UserAssignmentIntegrationTests.ChangePrimaryCompany_Atomicity_OldPrimaryBecomesFalse_TargetBecomesTrue`, `UserAssignmentIntegrationTests.ChangePrimaryCompany_RejectsTwoActivePrimaryCompanies` |
| Change Primary Department | `UserAssignmentIntegrationTests.ChangePrimaryDepartment_Atomicity_OldPrimaryBecomesFalse_TargetBecomesTrue`, `UserAssignmentIntegrationTests.ChangePrimaryDepartment_RejectsTwoActivePrimaryDepartmentsForOneCompanyAssignment` |
| Close non-primary assignment | `UserAssignmentIntegrationTests.CloseCompanyAssignment_NonPrimary_Succeeds`, `UserAssignmentIntegrationTests.CloseCompanyAssignment_ResultingAssignmentIsNotPrimary` |
| Close primary assignment with replacement | `UserAssignmentIntegrationTests.CloseCompanyAssignment_Primary_WithReplacement_Succeeds` |
| reject closing final active assignment | `OrganizationApiTests.CloseCompanyAssignment_LastActive_Rejected` |
| Same-company transfer | `UserAssignmentIntegrationTests.SameCompanyTransfer_Atomicity_Operations`, `UserAssignmentIntegrationTests.Transfer_SameCompanySourcePrimary`, `UserAssignmentIntegrationTests.Transfer_SameCompanySourceNonPrimary`, `UserAssignmentIntegrationTests.Transfer_SourceAndTargetDepartmentBeingEqual`, `UserAssignmentIntegrationTests.Transfer_TargetDepartmentAlreadyAssigned` |
| Cross-company transfer | `UserAssignmentIntegrationTests.CrossCompanyTransfer_Atomicity_Operations`, `UserAssignmentIntegrationTests.Transfer_CrossCompanySourceNonPrimary`, `UserAssignmentIntegrationTests.Transfer_SourceNonPrimary_MakeTargetPrimaryCompanyTrue_Rejection`, `UserAssignmentIntegrationTests.Transfer_PrimarySourceRequiringReplacement`, `UserAssignmentIntegrationTests.Transfer_ServerDiscoveryOfEveryActiveChildAssignment`, `UserAssignmentIntegrationTests.Transfer_StaleDiscoveredChildRowVersion_CausingCompleteRollback` |
| route assignment ownership | `OrganizationApiTests.Assignment_WrongUserId_Returns404` |
| malformed rowversion | `OrganizationApiTests.MalformedBase64_Returns400_ORG_MALFORMED_ROW_VERSION` |
| stale rowversion | `OrganizationApiTests.StaleRowVersion_Returns409_ORG_INVALID_ROW_VERSION` |
| overlap | `AssignmentTimelineTests.Overlaps_WithOverlap_ReturnsTrue`, `OrganizationApiTests.AssignCompany_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP`, `OrganizationApiTests.AssignDepartment_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP`, `OrganizationApiTests.SameCompanyTransfer_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP`, `OrganizationApiTests.CrossCompanyTransfer_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP` |
| hierarchy cycle | `HierarchyCycleTests.HasCycle_DeepCycle_ReturnsTrue`, `OrganizationApiTests.Company_Update_CycleDetected_Returns400_ORG_HIERARCHY_CYCLE_DETECTED` |
| deactivation dependencies | `OrganizationApiTests.Company_Deactivation_WithActiveChildCompany_Rejected`, `OrganizationApiTests.Company_Deactivation_WithActiveDepartment_Rejected`, `OrganizationApiTests.Company_Deactivation_WithActiveUserCompanyAssignment_Rejected`, `OrganizationApiTests.Department_Deactivation_WithActiveChildDepartment_Rejected`, `OrganizationApiTests.Department_Deactivation_WithActiveUserDepartmentAssignment_Rejected`, `OrganizationApiTests.Department_Deactivation_ActivePrimary_Rejected`, `OrganizationApiTests.Company_Deactivation_Succeeds_AfterDependenciesResolved`, `OrganizationApiTests.Department_Deactivation_Succeeds_AfterDependenciesResolved` |
| append-only Employment_Histories | `TransactionInvariantTests.EmploymentHistory_Insert_Succeeds`, `TransactionInvariantTests.EmploymentHistory_Update_Rejected_ByInterceptor`, `TransactionInvariantTests.EmploymentHistory_Delete_Rejected_ByInterceptor`, `TransactionInvariantTests.EmploymentHistory_CreatedByUserId_RemainsNull_BeforePhase1B`, `TransactionInvariantTests.EmploymentHistory_UpdatedByUserId_RemainsNull_WhereApplicable`, `TransactionInvariantTests.EmploymentHistory_CorrelationId_Persisted`, `TransactionInvariantTests.EmploymentHistory_UnauthenticatedActor_Ignored`, `TransactionInvariantTests.EmploymentHistory_InsertFailure_RollsBackBusinessTransaction` |
| deadlock retry only for SQL error 1205 | `DeadlockRetryPolicyTests.DeadlockRetryPolicy_Only_Retries_SqlException_1205` |
| maximum two retries | `DeadlockRetryPolicyTests.Program_Configures_MaxRetryCount_To_Two` |
| fresh context and transaction per attempt | `RetryContextFactoryTests.Attempt1_And_Retry_Use_Different_DbContext_Instances`, `RetryContextFactoryTests.Each_Attempt_Opens_Fresh_Serializable_Transaction`, `RetryContextFactoryTests.All_Data_Is_Reloaded_Because_Each_Context_Is_New`, `RetryContextFactoryTests.ChangeTracker_State_Not_Reused_Between_Attempts` |
| retry exhaustion mapping to HTTP 503 | `DeadlockRetryPolicyTests.Exhaustion_Maps_To_ORG_TRANSACTION_RETRY_EXHAUSTED` |
| Production/Staging startup protection | `OrganizationApiTests.Production_Startup_Throws_InvalidOperationException`, `OrganizationApiTests.Staging_Startup_Throws_InvalidOperationException` |
| Database safety | `DatabaseSafetyTests.IntegrationTests_Resolve_Exactly_PTKD_TEST_PHASE1A2`, `DatabaseSafetyTests.Tests_Reject_PTKD_DEV_BeforeAnyWrite`, `DatabaseSafetyTests.Tests_Run_NonParallel`, `DatabaseSafetyTests.TemporaryEnvironmentVariables_AreRestored`, `DatabaseSafetyTests.U0002_IsNeverExecutedAgainst_PTKD_DEV`, `DatabaseSafetyTests.TestDatabase_IsNotAutomaticallyCreatedOrDropped`, `DatabaseSafetyTests.DatabaseName_IsCheckedBeforeEveryResetOrSeed`, `DatabaseSafetyTests.EnsureCreated_IsNeverCalled`, `DatabaseSafetyTests.Migrate_IsNeverCalled`, `DatabaseSafetyTests.Migrations_V0001_And_V0002_AppliedByMigrator` |
| ProblemDetails errorCode, correlationId and sanitization | `OrganizationApiTests.ProblemDetails_Contains_ErrorCode_And_CorrelationId`, `OrganizationApiTests.ProblemDetails_Does_Not_Expose_SqlDetails`, `HealthCheckTests.HealthEndpoint_Returns_CorrelationId`, `Response_Echoes_ClientProvided_CorrelationId` |

## Database Safety Evidence
- Database resolved by Integration and API Tests: `PTKD_TEST_PHASE1A2`.
- Migrations run externally by DbMigrator. No `EnsureCreated` or `Migrate` in application startup.

## Fresh DbContext Retry Evidence
Validated by explicit unit tests:
- `Attempt1_And_Retry_Use_Different_DbContext_Instances`
- `Each_Attempt_Opens_Fresh_Serializable_Transaction`
- `All_Data_Is_Reloaded_Because_Each_Context_Is_New`
- `ChangeTracker_State_Not_Reused_Between_Attempts`

## Execution Environment & Build Details
- **SDK Executable**: `C:\Users\adm-bachdh\AppData\Local\Microsoft\dotnet\dotnet.exe`
- **SDK Version**: `10.0.301`

```
dotnet build src/backend/PTKD-ERP.sln --configuration Debug --warnaserror

Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:04.91
```

## Test Execution Results


**PTKD.UnitTests**
- Target framework: net10.0
- Database: None (Mocked)
- Total executed: 25
- Passed: 25
- Failed: 0
- Skipped: 0
- Duration: 962 ms

**PTKD.IntegrationTests**
- Target framework: net10.0
- Database: `PTKD_TEST_PHASE1A2`
- Total executed: 62
- Passed: 62
- Failed: 0
- Skipped: 0
- Duration: 14 s

**PTKD.ApiTests**
- Target framework: net10.0
- Database: `PTKD_TEST_PHASE1A2`
- Total executed: 60
- Passed: 60
- Failed: 0
- Skipped: 0
- Duration: 16 s

## Remaining Deviations and Risks
- None.



### Automated Test Inventory

A total of 147 automated tests passed:
- **Unit Tests:** 25
- **Integration Tests:** 62
- **API Tests:** 60

```text
PTKD.UnitTests -> C:\Projects\PTKD-ERP\tests\backend\PTKD.UnitTests\bin\Debug\net10.0\PTKD.UnitTests.dll
Test run for C:\Projects\PTKD-ERP\tests\backend\PTKD.UnitTests\bin\Debug\net10.0\PTKD.UnitTests.dll (.NETCoreApp,Version=v10.0)
PTKD.UnitTests.RetryContextFactoryTests.Attempt1_And_Retry_Use_Different_DbContext_Instances
PTKD.UnitTests.RetryContextFactoryTests.Each_Attempt_Opens_Fresh_Serializable_Transaction
PTKD.UnitTests.RetryContextFactoryTests.All_Data_Is_Reloaded_Because_Each_Context_Is_New
PTKD.UnitTests.RetryContextFactoryTests.ChangeTracker_State_Not_Reused_Between_Attempts
PTKD.UnitTests.DeadlockRetryPolicyTests.DeadlockRetryPolicy_Only_Retries_SqlException_1205
PTKD.UnitTests.DeadlockRetryPolicyTests.Program_Configures_MaxRetryCount_To_Two
PTKD.UnitTests.DeadlockRetryPolicyTests.Exhaustion_Maps_To_ORG_TRANSACTION_RETRY_EXHAUSTED
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Create_WithValidDates_CreatesSuccessfully
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Create_WithNullEffectiveTo_CreatesSuccessfully
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Create_WithToBeforeFrom_ThrowsArgumentException
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Create_WithToEqualFrom_ThrowsArgumentException
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Contains_Date_ReturnsExpectedResult(fromStr: "2023-01-01", toStr: "2023-12-31", dateStr: "2023-06-01", expected: True)
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Contains_Date_ReturnsExpectedResult(fromStr: "2023-01-01", toStr: "2023-12-31", dateStr: "2023-01-01", expected: True)
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Contains_Date_ReturnsExpectedResult(fromStr: "2023-01-01", toStr: "2023-12-31", dateStr: "2022-12-31", expected: False)
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Contains_Date_ReturnsExpectedResult(fromStr: "2023-01-01", toStr: "2023-12-31", dateStr: "2023-12-31", expected: False)
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Contains_Date_NullTo_ReturnsExpectedResult
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Overlaps_WithOverlap_ReturnsTrue
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Overlaps_NoOverlap_ReturnsFalse
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Overlaps_OneInfinite_ReturnsExpected
PTKD.UnitTests.Domain.ValueObjects.AssignmentTimelineTests.Overlaps_BothInfinite_ReturnsTrue
PTKD.UnitTests.Domain.Entities.HierarchyCycleTests.HasCycle_NullParentId_ReturnsFalse
PTKD.UnitTests.Domain.Entities.HierarchyCycleTests.HasCycle_DirectSelfParent_ReturnsTrue
PTKD.UnitTests.Domain.Entities.HierarchyCycleTests.HasCycle_GrandparentIsSelf_ReturnsTrue
PTKD.UnitTests.Domain.Entities.HierarchyCycleTests.HasCycle_DeepCycle_ReturnsTrue
PTKD.UnitTests.Domain.Entities.HierarchyCycleTests.HasCycle_NoCycle_ReturnsFalse
PTKD.IntegrationTests -> C:\Projects\PTKD-ERP\tests\backend\PTKD.IntegrationTests\bin\Debug\net10.0\PTKD.IntegrationTests.dll
Test run for C:\Projects\PTKD-ERP\tests\backend\PTKD.IntegrationTests\bin\Debug\net10.0\PTKD.IntegrationTests.dll (.NETCoreApp,Version=v10.0)
PTKD.IntegrationTests.DatabaseSafetyTests.IntegrationTests_Resolve_Exactly_PTKD_TEST_PHASE1A2
PTKD.IntegrationTests.DatabaseSafetyTests.Tests_Reject_PTKD_DEV_BeforeAnyWrite
PTKD.IntegrationTests.DatabaseSafetyTests.Tests_Run_NonParallel
PTKD.IntegrationTests.DatabaseSafetyTests.TemporaryEnvironmentVariables_AreRestored
PTKD.IntegrationTests.DatabaseSafetyTests.U0002_IsNeverExecutedAgainst_PTKD_DEV
PTKD.IntegrationTests.DatabaseSafetyTests.TestDatabase_IsNotAutomaticallyCreatedOrDropped
PTKD.IntegrationTests.DatabaseSafetyTests.DatabaseName_IsCheckedBeforeEveryResetOrSeed
PTKD.IntegrationTests.DatabaseSafetyTests.EnsureCreated_IsNeverCalled
PTKD.IntegrationTests.DatabaseSafetyTests.Migrate_IsNeverCalled
PTKD.IntegrationTests.DatabaseSafetyTests.Migrations_V0001_And_V0002_AppliedByMigrator
PTKD.IntegrationTests.MigrationRollbackTests.DbMigratorAtomicityAndIdempotencyAndRollbackFlow
PTKD.IntegrationTests.MigrationRollbackTests.DbMigratorRollsBackWhenScriptFails
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfDuplicateCompanyCode
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfDuplicateDepartmentCode
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfDuplicateEmployeeCode
PTKD.IntegrationTests.OrganizationSchemaTests.RowVersionChangingAfterUpdate
PTKD.IntegrationTests.OrganizationSchemaTests.NoOnDeleteCascadeForeignKeys
PTKD.IntegrationTests.OrganizationSchemaTests.NoSeedData
PTKD.IntegrationTests.OrganizationSchemaTests.AllSixExpectedTablesExist
PTKD.IntegrationTests.OrganizationSchemaTests.AllExpectedFilteredIndexesExist
PTKD.IntegrationTests.OrganizationSchemaTests.CompositeFkPreventsCrossUserCompanyMismatch
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfCrossCompanyParentDepartment
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfTwoActivePrimaryDepartmentsForOneUserCompany
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfActiveWithNonNullEffectiveTo
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfEffectiveToLessThanEffectiveFrom
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfDirectSelfParentReferences
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfTwoActiveCompanyAssignmentsForSameUserAndCompany
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfTwoActivePrimaryCompaniesForSameUser
PTKD.IntegrationTests.OrganizationSchemaTests.RejectionOfTwoActiveAssignmentsForSameUserAndDepartment
PTKD.IntegrationTests.TransactionInvariantTests.CreateUser_Atomicity_EmploymentHistoryInserted
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_Insert_Succeeds
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_Update_Rejected_ByInterceptor
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_Delete_Rejected_ByInterceptor
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_CreatedByUserId_RemainsNull_BeforePhase1B
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_UpdatedByUserId_RemainsNull_WhereApplicable
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_CorrelationId_Persisted
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_UnauthenticatedActor_Ignored
PTKD.IntegrationTests.TransactionInvariantTests.EmploymentHistory_InsertFailure_RollsBackBusinessTransaction
PTKD.IntegrationTests.UserAssignmentIntegrationTests.ChangePrimaryCompany_Atomicity_OldPrimaryBecomesFalse_TargetBecomesTrue
PTKD.IntegrationTests.UserAssignmentIntegrationTests.ChangePrimaryDepartment_Atomicity_OldPrimaryBecomesFalse_TargetBecomesTrue
PTKD.IntegrationTests.UserAssignmentIntegrationTests.ChangePrimaryCompany_RejectsTwoActivePrimaryCompanies
PTKD.IntegrationTests.UserAssignmentIntegrationTests.ChangePrimaryDepartment_RejectsTwoActivePrimaryDepartmentsForOneCompanyAssignment
PTKD.IntegrationTests.UserAssignmentIntegrationTests.CloseCompanyAssignment_NonPrimary_Succeeds
PTKD.IntegrationTests.UserAssignmentIntegrationTests.CloseCompanyAssignment_Primary_WithReplacement_Succeeds
PTKD.IntegrationTests.UserAssignmentIntegrationTests.CloseCompanyAssignment_ResultingAssignmentIsNotPrimary
PTKD.IntegrationTests.UserAssignmentIntegrationTests.CloseCompanyAssignment_Atomicity_AllActiveChildDepartmentsClosed
PTKD.IntegrationTests.UserAssignmentIntegrationTests.SameCompanyTransfer_Atomicity_Operations
PTKD.IntegrationTests.UserAssignmentIntegrationTests.CrossCompanyTransfer_Atomicity_Operations
PTKD.IntegrationTests.UserAssignmentIntegrationTests.AssignmentOperations_IntermediateError_RollsBackAllChanges
PTKD.IntegrationTests.UserAssignmentIntegrationTests.AssignmentOperations_StaleChildRowVersion_RollsBackOperation
PTKD.IntegrationTests.UserAssignmentIntegrationTests.AssignmentHistory_InsertedInSameTransaction
PTKD.IntegrationTests.UserAssignmentIntegrationTests.AssignmentHistory_InsertionFailure_RollsBackEveryBusinessChange
PTKD.IntegrationTests.UserAssignmentIntegrationTests.AssignmentHistory_NoPartialPrimarySwapCloseOrTransferRemains
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_SameCompanySourcePrimary
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_SameCompanySourceNonPrimary
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_SourceAndTargetDepartmentBeingEqual
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_TargetDepartmentAlreadyAssigned
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_CrossCompanySourceNonPrimary
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_SourceNonPrimary_MakeTargetPrimaryCompanyTrue_Rejection
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_PrimarySourceRequiringReplacement
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_ServerDiscoveryOfEveryActiveChildAssignment
PTKD.IntegrationTests.UserAssignmentIntegrationTests.Transfer_StaleDiscoveredChildRowVersion_CausingCompleteRollback
PTKD.ApiTests -> C:\Projects\PTKD-ERP\tests\backend\PTKD.ApiTests\bin\Debug\net10.0\PTKD.ApiTests.dll
Test run for C:\Projects\PTKD-ERP\tests\backend\PTKD.ApiTests\bin\Debug\net10.0\PTKD.ApiTests.dll (.NETCoreApp,Version=v10.0)
PTKD.ApiTests.HealthCheckTests.HealthEndpoint_ReturnsJson_WithStatusField
PTKD.ApiTests.HealthCheckTests.HealthEndpoint_Returns_CorrelationId
PTKD.ApiTests.HealthCheckTests.Response_Contains_CorrelationId_Header
PTKD.ApiTests.HealthCheckTests.Response_Echoes_ClientProvided_CorrelationId
PTKD.ApiTests.OrganizationApiTests.Production_Startup_Throws_InvalidOperationException
PTKD.ApiTests.OrganizationApiTests.Staging_Startup_Throws_InvalidOperationException
PTKD.ApiTests.OrganizationApiTests.Testing_Environment_Routes_Are_Available
PTKD.ApiTests.OrganizationApiTests.Development_Environment_Routes_Are_Available
PTKD.ApiTests.OrganizationApiTests.Company_Create_Valid_Returns201
PTKD.ApiTests.OrganizationApiTests.Company_GetById_Returns200
PTKD.ApiTests.OrganizationApiTests.Company_GetById_Missing_Returns404
PTKD.ApiTests.OrganizationApiTests.Company_List_Returns200
PTKD.ApiTests.OrganizationApiTests.Company_Update_Valid_Returns200
PTKD.ApiTests.OrganizationApiTests.Company_Status_Update_Returns200
PTKD.ApiTests.OrganizationApiTests.Department_Create_Valid_Returns201
PTKD.ApiTests.OrganizationApiTests.Department_GetById_Returns200
PTKD.ApiTests.OrganizationApiTests.Department_List_ByCompanyId_Returns200
PTKD.ApiTests.OrganizationApiTests.Department_Update_Valid_Returns200
PTKD.ApiTests.OrganizationApiTests.Department_Status_Update_Returns200
PTKD.ApiTests.OrganizationApiTests.User_Create_Valid_Returns201
PTKD.ApiTests.OrganizationApiTests.User_GetById_Returns200
PTKD.ApiTests.OrganizationApiTests.User_List_Returns200
PTKD.ApiTests.OrganizationApiTests.User_Update_Valid_Returns200
PTKD.ApiTests.OrganizationApiTests.Validation_EmptyCompanyCode_Returns400_ProblemDetails
PTKD.ApiTests.OrganizationApiTests.MalformedBase64_Returns400_ORG_MALFORMED_ROW_VERSION
PTKD.ApiTests.OrganizationApiTests.StaleRowVersion_Returns409_ORG_INVALID_ROW_VERSION
PTKD.ApiTests.OrganizationApiTests.ProblemDetails_Contains_ErrorCode_And_CorrelationId
PTKD.ApiTests.OrganizationApiTests.ProblemDetails_Does_Not_Expose_SqlDetails
PTKD.ApiTests.OrganizationApiTests.Company_Update_CycleDetected_Returns400_ORG_HIERARCHY_CYCLE_DETECTED
PTKD.ApiTests.OrganizationApiTests.Company_Deactivation_WithActiveDepartments_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignCompany_Valid_Returns204
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_RequiresCompanyAssignmentRowVersion
PTKD.ApiTests.OrganizationApiTests.Assignment_WrongUserId_Returns404
PTKD.ApiTests.OrganizationApiTests.AssignCompany_InactiveCompany_Rejected
PTKD.ApiTests.OrganizationApiTests.CloseCompanyAssignment_LastActive_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignCompany_DuplicateActive_Returns409
PTKD.ApiTests.OrganizationApiTests.ApiTests_Resolve_Exactly_PTKD_TEST_PHASE1A2
PTKD.ApiTests.OrganizationApiTests.Startup_DoesNotCreateSchema_WhenMigrationSchemaIsAbsent
PTKD.ApiTests.OrganizationApiTests.Company_Deactivation_WithActiveChildCompany_Rejected
PTKD.ApiTests.OrganizationApiTests.Company_Deactivation_WithActiveDepartment_Rejected
PTKD.ApiTests.OrganizationApiTests.Company_Deactivation_WithActiveUserCompanyAssignment_Rejected
PTKD.ApiTests.OrganizationApiTests.Department_Deactivation_WithActiveChildDepartment_Rejected
PTKD.ApiTests.OrganizationApiTests.Department_Deactivation_WithActiveUserDepartmentAssignment_Rejected
PTKD.ApiTests.OrganizationApiTests.Department_Deactivation_ActivePrimary_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_Valid_CreatesActiveAssignment
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_Valid_IsAlwaysNonPrimary
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_CorrectUserCompanyAssignmentId_DepartmentId_EffectiveDate
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_EmploymentHistories_IsWritten
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_WrongRouteUser_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_InactiveDepartment_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_MismatchCompany_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_ClosedCompanyAssignment_Rejected
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_DuplicateActive_Returns409
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_StaleCompanyAssignmentRowVersion_Returns409
PTKD.ApiTests.OrganizationApiTests.AssignCompany_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP
PTKD.ApiTests.OrganizationApiTests.AssignDepartment_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP
PTKD.ApiTests.OrganizationApiTests.SameCompanyTransfer_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP
PTKD.ApiTests.OrganizationApiTests.CrossCompanyTransfer_TemporalOverlap_Returns409_ORG_TEMPORAL_OVERLAP
PTKD.ApiTests.OrganizationApiTests.Company_Deactivation_Succeeds_AfterDependenciesResolved
PTKD.ApiTests.OrganizationApiTests.Department_Deactivation_Succeeds_AfterDependenciesResolved
```

READY FOR PHASE 1B
