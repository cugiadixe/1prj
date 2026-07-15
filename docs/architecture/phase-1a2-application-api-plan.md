# Phase 1A.2 - Organization Domain, Application and API Implementation Plan

## 1. Backend Folder and Project Structure
The backend architecture follows a Modular Monolith built with Vertical Slice Architecture.
- `src/backend/PTKD.Api`: Controller endpoints (`/api/v2/organizations/...`), Swagger configuration, middleware, and ProblemDetails.
- `src/backend/PTKD.Application`: DTOs, Validation rules, Application Services (handling transaction boundaries), and interfaces.
- `src/backend/PTKD.Domain`: Domain Entities, Value Objects, and domain logic (hierarchy cycle detection, overlap validation).
- `src/backend/PTKD.Infrastructure`: EF Core `AppDbContext`, Entity Configurations (mapping to SQL schema), and specific persistence concerns.

## 2. Domain Entities and Value Objects
- **Entities**: `Company`, `Department`, `User`, `UserCompanyAssignment`, `UserDepartmentAssignment`, `EmploymentHistory`.
- **Value Objects**: 
  - `AssignmentTimeline` (EffectiveFrom, EffectiveTo) with overlap and inclusion logic.
  - `RowVersion` (byte[] mapped to/from Base64).
- **Provisional Statuses**: `employment_status` and `account_status` are represented as documented `public const string` values in application validation, not permanent C# enums that imply the catalogs are final.

## 3. Data Access Abstractions and EF Core Strategy
- **DbContext Strategy**: Use exactly one `AppDbContext` in `PTKD.Infrastructure` representing the unified SQL Server database for multi-table transactions. Entity configurations are split by module.
- **Data Access Abstractions**: 
  - Controllers must not access `AppDbContext` directly.
  - Application services rely on an `IOrganizationDbContextFactory` for transactional operations.
  - **Query Behavior**: Read query services use `AsNoTracking` for complex read models and do *not* reuse the isolated transactional DbContext created by `IOrganizationDbContextFactory`.
  - **No Generic Repositories**: Specialized repositories are strictly avoided unless providing tangible, complex behavioral encapsulation. Wrapping EF Core `Add`/`Find`/`Update` in trivial repositories is prohibited.
- **Append-only Enforcement**: Infrastructure intercepts `SaveChanges` to strictly reject any `Modified` or `Deleted` state transitions for `EmploymentHistory` entities.
- **EF Schema Ownership**: Explicitly prohibit `Database.EnsureCreated()`, `Database.Migrate()` during API startup, EF-generated schema migrations, and automatic schema creation. Schemas for local and integration environments must be created exclusively through `PTKD.DbMigrator` using approved SQL migration files.

## 4. SQL Table Name Mapping
- Existing tables are mapped directly: `dbo.Companies`, `dbo.Departments`, `dbo.Users`, `dbo.User_Company_Assignments`, `dbo.User_Department_Assignments`, `dbo.Employment_Histories`.

## 5. Exact Concurrency Strategy
- **Rowversion Representation**: Transmitted exclusively as a Base64 string at the API boundary. Malformed Base64 yields **HTTP 400**. Stale but valid rowversion yields **HTTP 409** (`ORG_INVALID_ROW_VERSION`). Rowversion is a concurrency token, never called a "lock".

**Rowversion Categories**:
- **A. Client-selected records**: Require Base64 rowversion explicitly provided from the API request contract.
- **B. Server-discovered dependent records**: 
  - Are read inside an `IsolationLevel.Serializable` transaction.
  - Are processed in strict deterministic order.
  - EF Core tracks their original rowversions automatically.
  - `DbUpdateConcurrencyException` on any dependent record naturally returns **HTTP 409** with `ORG_INVALID_ROW_VERSION`.
  - The client does *not* submit every dependent rowversion.

- **Ordinary Single-Row CRUD**: Handled via EF Core optimistic concurrency comparing the database `row_version` to the requested `targetVersion`. Every existing row that may be updated must have its rowversion validated.
- **Multi-Row Operations**:
  - Encapsulated in an explicit EF Core transaction using `IsolationLevel.Serializable`.
  - Reads and updates are performed in deterministic order to prevent deadlocks.
  - Explicitly validate rowversions for *every* existing mutable row impacted.

## 6. Safe Primary-Switch Ordering
To prevent uncontrolled EF Core statement ordering from violating filtered unique indexes during primary assignment swaps, the exact EF Core mutation sequence must be:
1. Validate old and target assignment rowversions.
2. Set the old primary assignment's `is_primary` (or `is_primary_for_company`) flag to `false`.
3. Call `SaveChanges()` (inside the transaction) to clear the slot.
4. Set the target assignment's primary flag to `true`.
5. Application logic explicitly validates the exact-one-primary invariant.
6. Call `SaveChanges()` again.
7. Commit transaction.

## 7. Complete API Contracts and Vertical Slices
All endpoints reside under `/api/v2/organizations/`. No physical delete endpoints are exposed.

### Companies
- `POST /companies` -> `CreateCompanyRequest` (companyCode, parentCompanyId, name, taxCode)
- `PUT /companies/{id}` -> `UpdateCompanyRequest` (companyCode, parentCompanyId, name, taxCode, targetVersion)
- `PUT /companies/{id}/status` -> `UpdateCompanyStatusRequest` (isActive, targetVersion)
- `GET /companies`, `GET /companies/{id}`

### Departments
- `POST /departments` -> `CreateDepartmentRequest` (departmentCode, companyId, parentDepartmentId, name)
- `PUT /departments/{id}` -> `UpdateDepartmentRequest` (departmentCode, parentDepartmentId, name, targetVersion)
  *Note: Department.company_id is immutable after creation. Return a validation error if an internal update attempts to modify it.*
- `PUT /departments/{id}/status` -> `UpdateDepartmentStatusRequest` (isActive, targetVersion)
- `GET /departments`, `GET /departments/{id}`

### Users (Profiles and CRUD)
- `POST /users` -> `CreateUserRequest` (employeeCode, fullName, email, employmentStatus, accountStatus, initialCompanyId, initialDepartmentId, effectiveFrom, reason)
- `PUT /users/{id}` -> `UpdateUserRequest` (employeeCode, fullName, email, employmentStatus, accountStatus, targetVersion)
- `GET /users`, `GET /users/{id}`

### User Assignments and Transfers
- `POST /users/{id}/companies` -> `AssignCompanyRequest` (companyId, primaryDepartmentId, effectiveFrom, reason)
- `POST /users/{id}/departments` -> `AssignDepartmentRequest` (userCompanyAssignmentId, companyAssignmentRowVersion, departmentId, effectiveFrom, reason)
- `PUT /users/{userId}/company-assignments/{companyAssignmentId}/primary` -> `ChangePrimaryCompanyRequest` (targetRowVersion, currentPrimaryAssignmentId, currentPrimaryRowVersion, reason)
- `PUT /users/{userId}/department-assignments/{departmentAssignmentId}/primary` -> `ChangePrimaryDepartmentRequest` (targetRowVersion, currentPrimaryAssignmentId, currentPrimaryRowVersion, reason)
- `PUT /users/{userId}/company-assignments/{companyAssignmentId}/close` -> `CloseCompanyAssignmentRequest` (companyAssignmentRowVersion, replacementPrimaryCompanyAssignmentId, replacementPrimaryCompanyRowVersion, effectiveTo, reason)
- `POST /users/{userId}/company-assignments/{companyAssignmentId}/transfer/same-company` -> `SameCompanyDepartmentTransferRequest` (companyAssignmentRowVersion, sourceDepartmentAssignmentId, sourceDepartmentAssignmentRowVersion, targetDepartmentId, effectiveDate, reason)
- `POST /users/{userId}/company-assignments/{sourceCompanyAssignmentId}/transfer/cross-company` -> `CrossCompanyTransferRequest` (sourceCompanyAssignmentRowVersion, targetCompanyId, targetDepartmentId, makeTargetPrimaryCompany, replacementPrimaryCompanyAssignmentId, replacementPrimaryCompanyRowVersion, effectiveDate, reason)

## 8. Pagination, Sorting, and Filtering
- **Pagination**: Contract requires 1-based `pageIndex`. Default `pageSize` is 20, maximum `pageSize` is 100.
- **Company Endpoints**: 
  - Sort Whitelist: `companyCode`, `name`, `createdAt`. Default: `name ASC`.
  - Filter Whitelist: `isActive` (bool), `parentCompanyId` (long), `searchTerm`.
- **Department Endpoints**:
  - Sort Whitelist: `departmentCode`, `name`, `createdAt`. Default: `name ASC`.
  - Filter Whitelist: `isActive`, `companyId` (required), `parentDepartmentId`, `searchTerm`.
- **User Endpoints**:
  - Sort Whitelist: `employeeCode`, `fullName`, `createdAt`. Default: `fullName ASC`.
  - Filter Whitelist: `employmentStatus`, `accountStatus`, `companyId`, `searchTerm`.

## 9. Deactivation Rules
Documented safe behavior:
- **No cascade deactivation.**
- Reject Company deactivation while it has active child companies, active departments, or active user company assignments. (`ORG_COMPANY_HAS_ACTIVE_DEPENDENCIES`)
- Reject Department deactivation while it has active child departments or active user department assignments. (`ORG_DEPARTMENT_HAS_ACTIVE_DEPENDENCIES`)
- Reject deactivation of a department currently used as an active primary department. (`ORG_DEPARTMENT_IS_PRIMARY`)
- Dependencies must be transferred, closed, or deactivated first.

## 10. Retry Policy and DbContext Factory
- Add `IOrganizationDbContextFactory` to produce isolated context instances.
- Transactional application services must use the factory for retryable multi-table operations.
- **Retry Rules**:
  - For every attempt: create a fresh `AppDbContext`, open a new `Serializable` transaction, reload all required records, repeat all validation, perform all mutations, commit or fully dispose the context and transaction.
  - Never reuse EF tracked entities or ChangeTracker state from a failed attempt.
  - Retry only SQL Server deadlock error **1205**.
  - Maximum **2 retries** after initial attempt.
  - Short jittered delay between retries.
  - No retry for non-1205 SQL errors, validation, FK, unique constraint, temporal overlap, or stale rowversion exceptions.
  - Exhausted retries return **HTTP 503** with `ORG_TRANSACTION_RETRY_EXHAUSTED`.

## 11. HTTP Status and ProblemDetails Mappings
ProblemDetails responses always include standard fields plus `extensions` containing `errorCode`, `correlationId`, and `validationErrors`. ProblemDetails must never expose SQL details, constraint internals, connection information, or stack traces. All stable business errors used by operations are explicitly mapped:

- **400 Bad Request**: 
  - `ORG_VALIDATION_FAILED` (General validation failures)
  - `ORG_MALFORMED_ROW_VERSION` (Invalid Base64)
  - `ORG_HIERARCHY_CYCLE_DETECTED` (Parent creates cycle)
  - `ORG_SOURCE_TARGET_DEPARTMENT_SAME` (Source and target being the same in transfer)
- **404 Not Found**: 
  - `ORG_USER_NOT_FOUND`
  - `ORG_COMPANY_NOT_FOUND`
  - `ORG_DEPARTMENT_NOT_FOUND`
  - *(Missing assignment records fallback to 404 if not targeting an active context error)*
- **409 Conflict**: 
  - `ORG_INVALID_ROW_VERSION` (Stale rowversion)
  - `ORG_DUPLICATE_EMPLOYEE_CODE`, `ORG_DUPLICATE_COMPANY_CODE`, `ORG_DUPLICATE_DEPARTMENT_CODE`
  - `ORG_TEMPORAL_OVERLAP` (Temporal overlap with historical assignment)
  - `ORG_COMPANY_ASSIGNMENT_ALREADY_ACTIVE`, `ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE` (Duplicate active assignments)
  - `ORG_DEPARTMENT_ASSIGNMENT_REQUIRED`, `ORG_COMPANY_ASSIGNMENT_REQUIRED` (Required state missing)
  - `ORG_COMPANY_ASSIGNMENT_CLOSED` (Resource state closed)
  - `ORG_INACTIVE_COMPANY`, `ORG_INACTIVE_DEPARTMENT` (Inactive resource state)
  - `ORG_DEPARTMENT_COMPANY_MISMATCH` (Cross-company mismatch)
  - `ORG_USER_REQUIRES_ACTIVE_COMPANY` (Closing final active assignment)
  - `ORG_INVALID_PRIMARY_TRANSFER_REQUEST` (Invalid primary-transfer state)
  - `ORG_COMPANY_HAS_ACTIVE_DEPENDENCIES`, `ORG_DEPARTMENT_HAS_ACTIVE_DEPENDENCIES`, `ORG_DEPARTMENT_IS_PRIMARY` (Deactivation dependency conflict)
  - `ORG_INVALID_EFFECTIVE_DATE` (Date constraints violated)
- **500 Internal Server Error**: 
  - `ORG_UNEXPECTED_DATABASE_ERROR` (Unknown database errors or unmapped constraints)
- **503 Service Unavailable**:
  - `ORG_TRANSACTION_RETRY_EXHAUSTED` (Deadlock retries exhausted)

---

## 12. Multi-Table Transaction Operations

### A. Create User
- **Read/Validate**: Target company active, target dept active, target dept belongs to target company, effectiveFrom valid, employeeCode unique.
- **Insert (Atomically)**: `User`, active primary `UserCompanyAssignment`, active primary `UserDepartmentAssignment`, `EmploymentHistory` (JOINED).

### B. Assign Additional Company
- **Read/Validate**: User exists, target company active, target primary dept active, dept belongs to company. Reject existing active company assignment for same user/company (`ORG_COMPANY_ASSIGNMENT_ALREADY_ACTIVE`), temporal overlap with historical assignment (`ORG_TEMPORAL_OVERLAP`), inactive target company, inactive/cross-company primary department. Do not silently merge/reuse.
- **Insert**: `UserCompanyAssignment`, `UserDepartmentAssignment` (active primary).
- **Rules**: If user has no current primary company, new company becomes primary automatically. If primary exists, new assignment is non-primary. Must not change an existing primary company.
- **History Written**: `ASSIGNED_COMPANY`.

### C. Assign Additional Department
- **Read/Validate**: User company assignment is active; department is active; department belongs to same company. Reject existing active assignment for same user/dept (`ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE`) and temporal overlap.
- **Check Rowversions**: Explicitly provided `companyAssignmentRowVersion`.
- **Insert**: `UserDepartmentAssignment` (always non-primary).
- **History Written**: `ASSIGNED_DEPARTMENT`.

### D. Change Primary Company
- **Read/Validate**: Route parameter dictates target assignment ID. Request dictates current primary ID. Both verified.
- **Update**: Safe swap ordering.
- **History Written**: `CHANGED_PRIMARY_COMPANY`.

### E. Change Primary Department
- **Invariant Rule**: Route parameter identifies existing non-primary department assignment. Request identifies current primary assignment. Both must exist. Return `ORG_DEPARTMENT_ASSIGNMENT_REQUIRED` if caller attempts silent creation.
- **Update**: Safe swap ordering.
- **History Written**: `CHANGED_PRIMARY_DEPARTMENT`.

### F. Close Company Assignment
- **Request Fields**: Route parameter `companyAssignmentId`. Body contains `effectiveTo`, `replacementPrimaryCompanyAssignmentId`, `replacementPrimaryCompanyRowVersion`, and source `companyAssignmentRowVersion`.
- **Validation**:
  - Route `companyAssignmentId` must exist and belong to the route `userId`.
  - `companyAssignmentRowVersion` must belong to that exact assignment.
  - Return `ORG_COMPANY_ASSIGNMENT_REQUIRED` or `ORG_INVALID_ROW_VERSION` if invalid.
- **Rules**: 
  - Do not allow closing the user's final active company assignment. Return `ORG_USER_REQUIRES_ACTIVE_COMPANY`. (User termination is a separate future workflow).
  - A closed company assignment must always have `is_primary = false`.
  - When closing a primary assignment and another active company assignment exists, replacement fields are required. The replacement must belong to the same user, be active, and pass rowversion validation. Safe primary swap occurs *before* the source assignment is closed.
  - When closing a non-primary assignment, replacement fields must be null.
  - Active child department assignments must be discovered and closed by server in the same transaction.
- **Effective Interval Rules**: Half-open interval semantics `[effective_from, effective_to)`. Require `effectiveTo` > source company assignment `effectiveFrom`. Require `effectiveTo` > every active child assignment `effectiveFrom`. Require `effectiveTo` not later than any already-defined upper bound. All source and child rows receive the same logical closure instant. Do *not* replace client `effectiveTo` with server `Now()`.
- **History Written**: `CLOSED_COMPANY`.

### G. Transfer User
**SameCompanyDepartmentTransferRequest**:
- **Read/Validate**:
  - `sourceDepartmentAssignmentId` must identify an active assignment.
  - `targetDepartmentId` must not equal the source department ID (`ORG_SOURCE_TARGET_DEPARTMENT_SAME`).
  - Target department must be active and belong to the same company (`ORG_DEPARTMENT_COMPANY_MISMATCH`).
  - The user must not already have an active assignment at the target department (`ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE`).
  - The target timeline must not overlap historical assignments for the same user and target department (`ORG_TEMPORAL_OVERLAP`).
  - `effectiveDate` must be strictly greater than the source assignment `effectiveFrom` (`ORG_INVALID_EFFECTIVE_DATE`).
- **Check Rowversions**: `companyAssignmentRowVersion`, `sourceDepartmentAssignmentRowVersion`.
- **Update/Insert**: Retain company assignment. Close the source assignment at `effectiveDate`. Create the target assignment beginning at the same `effectiveDate`. If source is primary, target becomes primary in the same transaction. If source is non-primary, target remains non-primary. Exactly one active primary department must exist for the company assignment at commit. Append history.

**CrossCompanyTransferRequest**:
- **Read/Validate**: Reject target company equal to source company. Reject existing active target company assignment. Reject overlapping target company assignment history. Reject existing active target department assignment. Reject inactive target company or department.
- **Validation Branches**:
  - Destination assignments do not have rowversions before creation.
  - If source company assignment is **primary** and `makeTargetPrimaryCompany` is true: replacement primary fields must be null.
  - If source company assignment is **primary** and `makeTargetPrimaryCompany` is false: replacement primary fields are required.
  - If source company assignment is **not primary**: `makeTargetPrimaryCompany` must be false. The replacement primary fields must be null. The target company assignment is created as non-primary. To make the target primary afterwards, the caller must use the dedicated ChangePrimaryCompany endpoint. Reject an invalid request with HTTP 409 `ORG_INVALID_PRIMARY_TRANSFER_REQUEST`.
  - The replacement assignment must belong to the same user, be active, and pass rowversion validation.
  - A closed company assignment must never remain primary. Exact-one-primary-department must hold at commit.
- **Check Rowversions**: `sourceCompanyAssignmentRowVersion` and replacement primary rowversion. Server-discovered child rowversions automatically checked via `DbUpdateConcurrencyException`.
- **Update/Insert**: Close source company assignment. Close every active child department assignment discovered by the server in deterministic order. Create target company assignment, create target primary dept assignment. Update primary company when required. Append history. Roll back on any validation/rowversion failure.

---

## 13. Security and Environment Protection
Phase 1A.2 APIs entirely lack authentication and company-authorization scopes.
- **API Protection**: ALL organization endpoints (including `GET` read endpoints) must only be available in allowed environments: `Development` and `Testing`.
- **Production/Staging Rules**: In Production, Staging, or any production-like environment, the system must NOT map `/api/v2/organizations/*` endpoints OR it must fail startup when an unsafe organization API configuration is enabled.
- **Fake Authorization**: Do not protect only write endpoints. Organization read APIs contain sensitive employee data. Do not implement fake authorization.
- **Audit Actor Rule**: Until Phase 1B, `created_by_user_id` and `updated_by_user_id` remain `NULL`. Do not trust an actor ID supplied through an unauthenticated request header. `correlation_id` is still written to `Employment_Histories`.

---

## 14. Dedicated Test Database and Matrix
**Database Rules**: Use exactly `PTKD_TEST_PHASE1A2`. Tests must never target `PTKD_DEV`. Verify database name before writing. V0001 and V0002 must be applied through `PTKD.DbMigrator`. Do not use EnsureCreated or Database.Migrate. Do not automatically create or drop the database. Mark execution BLOCKED when the database does not exist. Tests must run non-parallel. Isolate test data using rollback transactions or a deterministic database-reset fixture. Temporary connection-string environment variables must be restored in `finally` blocks. Never execute U0002 against `PTKD_DEV`.

**Unit, SQL Server Integration, and API Tests Must Explicitly Cover**:
- Company create, update, status and query.
- Department create, update, status and query.
- Department `company_id` cannot be changed.
- User create, update and query.
- Assign Company (including existing-active and historical-overlap rejections).
- Assign Department (including existing-active rejection).
- Change Primary Company.
- Change Primary Department.
- Close non-primary Company Assignment.
- Close primary Company Assignment with replacement (replacement validation).
- Closing final active company assignment returns `ORG_USER_REQUIRES_ACTIVE_COMPANY`.
- Same-company transfer (including target-already-active rejection, same-company source/target being equal rejection, target dept historical overlap rejection, source primary transfers primary flag, source non-primary keeps target non-primary).
- Cross-company transfer (including target-already-active rejection, source non-primary + makeTargetPrimary=true rejection, source non-primary transfer creates target as non-primary).
- Server discovers and closes all source child assignments.
- Concurrency failure on a server-discovered child assignment.
- Route assignment does not belong to route user.
- Pagination, filtering and sorting for each list endpoint.
- Full transaction rollback for every multi-table operation.
- Company deactivation with active dependencies.
- Department deactivation with active dependencies.
- Primary department deactivation rejection.
- Route/body assignment identity cannot conflict.
- `effectiveTo` half-open validation logic.
- Fresh DbContext is used on every deadlock retry, failed attempt does not leak tracked state.
- Deadlock 1205 retry succeeds; retries exhausted after 2 retries; non-1205 errors not retried.
- Unknown SqlException returns HTTP 500 with `ORG_UNEXPECTED_DATABASE_ERROR`.
- ProblemDetails does not expose SQL, constraint details, connection information, or stack traces.
- All documented business errors have a stable HTTP and ProblemDetails mapping.
- Organization GET and write routes are not available outside Development/Testing, unsafe prod startup rejected.
- `created_by_user_id` and `updated_by_user_id` remain NULL before Phase 1B.
- `correlation_id` is still stored in `Employment_Histories`.
- `Database.EnsureCreated` is not called, `Database.Migrate` is not called.
- Integration schemas are applied only through `PTKD.DbMigrator`.
- `PTKD_TEST_PHASE1A2` name safety check. Tests refuse `PTKD_DEV`.

---

## 15. Expected File Inventory
The exact implementation will result in creating or modifying the following explicit directories and files:

- **PTKD.Domain**:
  - `Entities/Company.cs`, `Entities/Department.cs`, `Entities/User.cs`, `Entities/UserCompanyAssignment.cs`, `Entities/UserDepartmentAssignment.cs`, `Entities/EmploymentHistory.cs`
  - `ValueObjects/AssignmentTimeline.cs`, `ValueObjects/RowVersion.cs`
- **PTKD.Application**:
  - `Organizations/Companies/DTOs/CreateCompanyRequest.cs`, `UpdateCompanyRequest.cs`, `CompanyDto.cs`
  - `Organizations/Companies/Services/ICompanyService.cs`, `CompanyService.cs`
  - `Organizations/Companies/Validation/CreateCompanyValidator.cs`, `UpdateCompanyValidator.cs`
  - `Organizations/Departments/DTOs/CreateDepartmentRequest.cs`, `UpdateDepartmentRequest.cs`, `DepartmentDto.cs`
  - `Organizations/Departments/Services/IDepartmentService.cs`, `DepartmentService.cs`
  - `Organizations/Departments/Validation/CreateDepartmentValidator.cs`, `UpdateDepartmentValidator.cs`
  - `Organizations/Users/DTOs/CreateUserRequest.cs`, `UpdateUserRequest.cs`, `UserDto.cs`
  - `Organizations/Users/Services/IUserService.cs`, `UserService.cs`
  - `Organizations/Users/Validation/CreateUserValidator.cs`, `UpdateUserValidator.cs`
  - `Organizations/Assignments/DTOs/AssignCompanyRequest.cs`, `AssignDepartmentRequest.cs`, `ChangePrimaryCompanyRequest.cs`, `ChangePrimaryDepartmentRequest.cs`, `CloseCompanyAssignmentRequest.cs`, `SameCompanyDepartmentTransferRequest.cs`, `CrossCompanyTransferRequest.cs`
  - `Organizations/Assignments/Services/IUserAssignmentService.cs`, `UserAssignmentService.cs`
  - `Organizations/Assignments/Validation/CrossCompanyTransferValidator.cs`, `CloseCompanyAssignmentValidator.cs`, `AssignCompanyValidator.cs`, `SameCompanyDepartmentTransferValidator.cs`
  - `Common/Interfaces/IOrganizationDbContext.cs`, `IOrganizationDbContextFactory.cs`
- **PTKD.Infrastructure**:
  - `Persistence/AppDbContext.cs`
  - `Persistence/AppDbContextFactory.cs`
  - `Persistence/Interceptors/AppendOnlyInterceptor.cs`
  - `Persistence/Configurations/CompanyConfiguration.cs`, `DepartmentConfiguration.cs`, `UserConfiguration.cs`, `UserCompanyAssignmentConfiguration.cs`, `UserDepartmentAssignmentConfiguration.cs`, `EmploymentHistoryConfiguration.cs`
  - `Persistence/ExecutionStrategy/DeadlockRetryPolicy.cs`
- **PTKD.Api**:
  - `Controllers/Organizations/CompanyController.cs`, `DepartmentController.cs`, `UserController.cs`, `UserAssignmentController.cs`
  - `Middleware/EnvironmentProtectionMiddleware.cs`
  - `Middleware/ProblemDetailsMapping.cs`
- **PTKD.UnitTests**:
  - `Domain/ValueObjects/AssignmentTimelineTests.cs`
  - `Domain/Entities/HierarchyCycleTests.cs`
  - `Application/Organizations/Validation/CrossCompanyTransferValidationTests.cs`
  - `Application/Organizations/Validation/SameCompanyDepartmentTransferValidationTests.cs`
- **PTKD.IntegrationTests**:
  - `Persistence/ConcurrencyTests.cs`
  - `Persistence/DeadlockRetryTests.cs`
  - `Persistence/AppendOnlyEnforcementTests.cs`
  - `Persistence/CompanyConfigurationTests.cs`
  - `Fixtures/DatabaseResetFixture.cs`
- **PTKD.ApiTests**:
  - `Organizations/UserTransferEndpointTests.cs`
  - `Organizations/SecurityEnvironmentTests.cs`
  - `Organizations/CloseCompanyAssignmentTests.cs`

---

## 16. Execution Order and Exit Criteria
1. Define Domain Entities, Value Objects, and append-only rules.
2. Scaffold `AppDbContext`, apply Entity configs without EF migration support.
3. Build DTOs (strictly reflecting requested contracts, Base64 Rowversion handling).
4. Implement Application Services with `IOrganizationDbContextFactory`, isolated `IsolationLevel.Serializable` transactions, safe-swap steps, and deadlock retry policies.
5. Implement Controllers routing endpoints strictly. Apply environment-protection logic for ALL endpoints.
6. Build out extensive `WebApplicationFactory` API tests connected exclusively to `PTKD_TEST_PHASE1A2`.

**Entry Criteria**: 
- Approved Phase 1A.1 database foundation schema (`V0002`).

**Exit Criteria**: 
- `dotnet build --warnaserror`: 0 errors, 0 warnings.
- All unit tests pass.
- All SQL Server integration tests pass.
- All WebApplicationFactory tests pass.
- No V0002 change.
- No EF migration generated.
- Organization endpoints unavailable outside Development/Testing.
- No unauthenticated actor ID accepted from headers.
- No active department assignment remains under a closed company assignment.
- No closed company assignment remains primary.
- Department.company_id cannot be modified through the API.
- User always retains at least one active company assignment.
- No overlapping company or department assignments are created.
- **Cross-company transfer cannot create two primary companies.**
- **Same-company transfer cannot create overlapping department assignments.**
- All assignment endpoints identify assignment rows unambiguously (route identifiers, correct explicit contracts).
- All CRUD and transactional endpoint tests pass.
- **All stable error codes used by services have explicit HTTP mappings.**
- `PTKD_TEST_PHASE1A2` verification passes, no test targets `PTKD_DEV`.
- Retry attempts use completely fresh DbContext instances.
- No unresolved Phase 1A.2 errors.
