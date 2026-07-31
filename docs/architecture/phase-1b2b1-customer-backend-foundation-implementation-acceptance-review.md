# Phase 1B.2-B1 Customer Backend Foundation Implementation Acceptance Review

**Status:**
ACCEPTED — SEE phase-1b2b1-project-owner-implementation-acceptance.md

**Implementation commit:**
91828f55924085401ba2bf16a3519b59859dc1d2

**Parent commit:**
6ffa7c8b094ed4cef709af1b22ee5da48e6e993d

**Accepted plan commit:**
7c6a610a1bebdd68a42d90ca2070cace5b90ed17

**Project Owner plan acceptance commit:**
6ffa7c8b094ed4cef709af1b22ee5da48e6e993d

---

## 1. Committed files

23 files changed: 1813 insertions, 5 deletions.

### New files (14)

| File | Lines |
|------|------:|
| database/migrations/V0005__create_customer_schema.sql | 135 |
| database/rollbacks/U0005__drop_customer_schema.sql | 64 |
| src/backend/PTKD.Api/Controllers/CustomersController.cs | 112 |
| src/backend/PTKD.Application/Customers/DTOs/CustomerDtos.cs | 157 |
| src/backend/PTKD.Application/Customers/Services/CustomerService.cs | 447 |
| src/backend/PTKD.Application/Customers/Services/ICustomerService.cs | 17 |
| src/backend/PTKD.Application/Customers/Validations/CustomerValidators.cs | 62 |
| src/backend/PTKD.Domain/Entities/Customer.cs | 49 |
| src/backend/PTKD.Domain/Entities/CustomerCompanyContext.cs | 53 |
| src/backend/PTKD.Domain/Entities/Profile.cs | 98 |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerCompanyContextConfiguration.cs | 33 |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs | 33 |
| src/backend/PTKD.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs | 39 |
| tests/backend/PTKD.ApiTests/CustomerApiTests.cs | 445 |

### Modified files (9)

| File | +/- |
|------|-----|
| src/backend/PTKD.Api/Filters/GlobalExceptionFilter.cs | +1 −1 |
| src/backend/PTKD.Api/Program.cs | +3 |
| src/backend/PTKD.Api/Security/Authorization/PermissionCodes.cs | +8 |
| src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs | +7 |
| src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs | +3 |
| tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs | +3 −3 |
| tests/backend/PTKD.IntegrationTests/MigrationRollbackTests.cs | +8 |
| tests/backend/PTKD.IntegrationTests/SecuritySchemaTests.cs | +4 |
| tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs | +32 −1 |

---

## 2. Accepted implemented scope

### Database

- Profiles table: bigint PK, 17 data fields, filtered unique index on CCCD (active non-null), audit FKs, rowversion.
- Customers table: bigint PK, customer_code unique, profile_id FK, customer_status CHECK (ACTIVE/INACTIVE/MERGED), survivor self-FK, audit FKs, rowversion.
- Customer_Company_Contexts table: bigint PK, composite unique (customer_id, company_id), company_id FK, assigned_staff_id FK, relationship_status CHECK (ACTIVE/INACTIVE), audit FKs, rowversion.
- V0005 migration seeds 4 customer permission codes into dbo.Permissions.
- U0005 rollback with test-only guards (DB_NAME check, SchemaVersions guard, no-later-migration guard, no-FK-reference guard).

### Backend

- Customer search/list with pagination implemented.
- Customer detail with sensitive masking implemented.
- Admin atomic create (Profile + Customer + optional CompanyContext) implemented with Serializable transaction.
- Admin update with required reason and rowVersion concurrency implemented.
- Duplicate detection (CCCD unique, pre-create check endpoint) implemented.
- Audit via SecurityAuditEventRecord + ITransactionalAuditWriter for all mutations implemented.
- 8 API v2 endpoints under /api/v2/customers implemented.
- Controllers delegate to CustomerService application service.
- EF CRUD used for all operations. No Dapper/stored procedures introduced.
- FluentValidation validators for all request DTOs implemented.
- DI registration in Program.cs.

### Concurrency

- Customer entity MarkUpdated() ensures Customer.RowVersion changes on profile updates.
- GlobalExceptionFilter uses ConcurrencyException.ErrorCode instead of hardcoded ORG prefix, preserving existing ORG behavior while supporting CUS error codes.

---

## 3. Permission gate confirmation

- CUSTOMER_VIEW_BASIC gates GET /api/v2/customers and GET /api/v2/customers/{id}.
- CUSTOMER_VIEW_SENSITIVE gates sensitive field unmasking (checked at service layer via IPermissionEvaluator).
- CUSTOMER_CREATE_FINAL gates POST /api/v2/customers and POST /api/v2/customers/{id}/company-contexts.
- CUSTOMER_MASTER_UPDATE gates PUT /api/v2/customers/{id} and PUT /api/v2/customers/{id}/company-contexts/{contextId}.
- PermissionCodes.cs synchronized only with 4 approved existing catalog codes: CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE.
- permission-catalog.md unchanged (verified via git diff).
- No new permission code names added beyond the approved catalog.

---

## 4. Deferred scope confirmation

- No frontend source files changed.
- No frontend test files changed.
- No workflow/approval runtime implemented.
- No customer merge implemented.
- No group spending implemented.
- No ENTITY scope implemented.
- No Service module dependency implemented.
- No Payment/Reconciliation dependency implemented.
- No export/download implemented.
- No security enhancement backlog implemented.

---

## 5. Test evidence

### Build

```
dotnet build src/backend/PTKD-ERP.sln -c Debug
Build succeeded. 0 Warning(s) 0 Error(s)
```

### Unit tests

```
dotnet test tests/backend/PTKD.UnitTests/PTKD.UnitTests.csproj --no-build -c Debug --verbosity minimal
Passed! - Failed: 0, Passed: 133, Skipped: 0, Total: 133
```

### Integration tests

```
dotnet test tests/backend/PTKD.IntegrationTests/PTKD.IntegrationTests.csproj --no-build -c Debug --verbosity minimal
Passed! - Failed: 0, Passed: 196, Skipped: 0, Total: 196
```

### API tests

```
dotnet test tests/backend/PTKD.ApiTests/PTKD.ApiTests.csproj --no-build -c Debug --verbosity minimal
Passed! - Failed: 0, Passed: 257, Skipped: 0, Total: 257
```

### Migration/rollback verification

- MigrationRollbackTests.DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder covers:
  - V0005 forward migration applies successfully.
  - V0005 is recorded in SchemaVersions with count 1.
  - Second run skips V0005 (idempotent).
  - U0005 rollback drops customer tables and removes V0005 from SchemaVersions.
  - U0004 rollback proceeds after U0005.
- No reruns were required. All suites passed on first run.

---

## 6. Validation fixes made during implementation

| Fix | Description |
|-----|-------------|
| Customer.MarkUpdated() | Added MarkUpdated(userId) to Customer entity so that UpdateCustomerAsync modifies the Customer row, causing its SQL Server rowversion to change and enabling stale-version detection on subsequent updates. |
| GlobalExceptionFilter ErrorCode | Changed from hardcoded `"ORG_INVALID_ROW_VERSION"` to `concurrencyEx.ErrorCode` so that ConcurrencyException carries the correct module-specific error code (CUS_INVALID_ROW_VERSION for customer, ORG_INVALID_ROW_VERSION for organization). |
| TestDatabaseFixture KnownTables | Added "Profiles", "Customers", "Customer_Company_Contexts" to KnownTables whitelist and DropKnownSchema drop order. |
| TestDatabaseFixture ResetToV0005 | Added ResetToV0005() method following existing ResetToV0004() pattern. |
| SafeTestWebApplicationFactory | Updated schema initialization from ResetToV0003() to ResetToV0005() so API tests have customer tables available. |
| SecuritySchemaTests | Added 4 customer permission codes to ExpectedPermissionCodes array in alphabetical order. |
| MigrationRollbackTests | Added V0005 forward assertions and U0005 rollback step before U0004 in dependency order test. |

---

## 7. Risks and follow-up

| # | Item | Status |
|---|------|--------|
| 1 | Frontend Customer UI not yet implemented | Deferred to Phase 1B.2-B2 or later |
| 2 | Workflow approval remains deferred | Requires workflow module |
| 3 | Customer merge remains deferred | Requires preview of services/payments/documents |
| 4 | Group spending remains deferred | Requires Payment module |
| 5 | ENTITY scope remains deferred | Not approved |
| 6 | Service/Payment modules remain deferred | Not in scope |
| 7 | Sensitive masking must be revalidated when frontend is added | Backend masking is authoritative; frontend must not bypass |
| 8 | V0005 migration must not be applied to production automatically | Requires explicit deployment authorization |
| 9 | Company context data isolation (DATA-004) | GET company-contexts currently returns all contexts; company-scoped filtering deferred until user company assignment enforcement is fully specified |

---

## 8. Conclusion

PHASE 1B.2-B1 CUSTOMER BACKEND FOUNDATION IMPLEMENTATION ACCEPTANCE REVIEW PASSED
