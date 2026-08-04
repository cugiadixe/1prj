# Phase 1B.8-B1 Card Reprint Backend/Data Completion Report

## 1. Summary of Implemented Behavior
The Card Reprint backend foundation (Phase 1B.8-B1) has been completed and verified. 
- Implemented `CardReprintRequestService` to handle creating draft card reprint requests.
- Integrated `CardReprintRequest` and `Card` entities into EF Core's `AppDbContext`.
- Exposed CRUD endpoints (Create, GetAll, GetById) through `CardReprintRequestsController` securely.
- Handled cross-company data authorization boundaries and enforced headers matching payload values.
- Adhered to B1-safe scope: no workflow integration, no payment/billing creation, and no frontend components implemented in this phase.

## 2. Files Changed
- `src/backend/PTKD.Application/Cards/Services/CardReprintRequestService.cs`
- `src/backend/PTKD.Api/Controllers/CardReprintRequestsController.cs`
- `src/backend/PTKD.Application/Common/Interfaces/IOrganizationDbContext.cs`
- `src/backend/PTKD.Infrastructure/Persistence/AppDbContext.cs`
- `src/backend/PTKD.Infrastructure/Persistence/Configurations/CardReprintRequestConfiguration.cs`
- `tests/backend/PTKD.ApiTests/CardReprintRequestApiTests.cs`
- `tests/backend/PTKD.ApiTests/SafeTestWebApplicationFactory.cs`
- `tests/backend/PTKD.IntegrationTests/TestDatabaseFixture.cs`

*(WIP files such as DTOs and Program.cs were also verified)*

## 3. Database Migration and Rollback Scripts
- **Forward**: `database/migrations/V0013__card_reprint_foundation.sql` (Existing file reused)
- **Rollback**: (Existing corresponding script reused, verified through Integration tests).
- Changes to infrastructure: Added `ResetToV0013` in `TestDatabaseFixture.cs` to ensure safe API and integration testing context contains the newly generated schema.

## 4. API Endpoints or Contracts Changed
- `POST /api/v2/card-reprint-requests`
- `GET /api/v2/card-reprint-requests`
- `GET /api/v2/card-reprint-requests/{id}`

*All controllers require `X-Company-Id` header and standard permission claims.*

## 5. Tests Added or Updated
- **API Tests**: `PTKD.ApiTests/CardReprintRequestApiTests.cs` provides complete coverage for endpoints:
  - `Create_ValidRequest_ReturnsCreated`
  - `Create_InvalidCardId_ReturnsNotFound`
  - `Create_MissingCompanyHeader_ReturnsBadRequest`
  - `Create_CrossCompany_ReturnsForbidden`
  - `GetAll_ReturnsOk`
  - `GetById_ExistingRequest_ReturnsOk`
- `SafeTestWebApplicationFactory` was updated to seed the V0013 schema required for `CardReprintRequest` integration testing.

## 6. Exact Build and Test Commands Run
```sh
dotnet build src/backend/PTKD-ERP.sln
dotnet test tests/backend/PTKD.UnitTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.IntegrationTests/ -p:ParallelizeTestCollections=false
dotnet test tests/backend/PTKD.ApiTests/ -p:ParallelizeTestCollections=false
git diff --check
```

## 7. Actual Results
- **Build**: Succeeded (0 Errors, 9 Warnings).
- **Unit Tests**: Passed (219/219).
- **Integration Tests**: Passed (203/203).
- **API Tests**: Passed (6/6).
- **Code Formatting**: No whitespace errors reported by `git diff --check`.

## 8. Unresolved Risks or Decisions
- `SafeTestWebApplicationFactory` currently forces migration to `V0013` schema which is sufficient for Phase 1B.8. We'll need to expand this iteratively in future phases.
- The `CreateCardReprintRequest` body has a `CompanyId`, but the authorization checks `X-Company-Id`. To avoid mismatch, the controller now enforces that they both match before forwarding to the service.

## 9. Manual Verification Steps
- Boot the API server, authorize with an admin JWT.
- Provide `X-Company-Id` in the headers.
- Send a POST request to `/api/v2/card-reprint-requests` with a valid `CardId`. Verify 201 Created response.
