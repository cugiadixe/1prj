# Phase 1B.2-A Customer Module Discovery and Detailed Plan

**Status:** ACCEPTED — SEE phase-1b2a-project-owner-plan-acceptance.md

**Baseline:** a9a368a870d19b6eae903f1041575584402fd089

**Authorization context:**
Phase 1B.2-A discovery and detailed planning only.
No source implementation is authorized.
No backend code changes are authorized.
No frontend implementation is authorized.
No database migration is authorized.
No rollback migration is authorized.
No API v2 implementation is authorized.
No new permission code is authorized.
Workflow/approval implementation is not authorized.
Customer merge implementation is not authorized unless later explicitly approved.
ENTITY remains deferred unless separately approved.

---

## 1. Master context finding

PTKD-ERP-Master-Context.md was not found in the repository. Searched via `git ls-files`, `git grep`, and filesystem glob. Recorded as a documentation gap. This plan uses committed business and architecture documents only.

---

## 2. Source documents reviewed

- `docs/business/business-rules.md` — v1.1 approved baseline
- `docs/business/acceptance-criteria.md` — v1.1 traceable QA/UAT contract
- `docs/business/permission-catalog.md` — v1.1 canonical permissions
- `docs/business/PTKD-Specification-v1.1.md` — primary specification
- `docs/architecture/phase-1b2-next-work-selection-review.md` — accepted
- `docs/architecture/phase-1b2-next-work-selection-project-owner-acceptance.md` — authorization
- `docs/architecture/phase-1b1-security-administration-project-owner-completion-acceptance.md` — security complete
- `docs/architecture/phase-1a2-application-api-implementation.md` — Organization API patterns
- `docs/architecture/phase-1b0-security-discovery-decisions.md` — security schema
- `docs/decisions/phase-1b0-open-decisions.md` — 20 approved security decisions
- `docs/architecture/project-readiness-review.md` — technical stack decisions

---

## 3. Customer business rule discovery

### From business-rules.md

| Rule ID | Summary | First slice? |
|---------|---------|:---:|
| CUS-001 | Ordinary staff cannot directly edit full_name, cccd, dob, phone, contact_address. | Yes |
| CUS-002 | Staff submit CREATE_CUSTOMER or CUSTOMER_MASTER_CHANGE requests for creation/change. | Deferred — requires workflow |
| CUS-003 | Only GROUP_CUSTOMER_DATA_ADMIN may create/update/merge as final operation. | Yes (direct admin only) |
| CUS-004 | Direct admin correction requires reason and field-level before/after audit. | Yes |
| CUS-005 | Duplicate check must run before submit and again before execution. | Yes (pre-create check) |
| CUS-006 | Active non-empty CCCD requires filtered unique index. Phone is duplicate signal only. | Yes |
| CUS-007 | Customer merge must preview affected services, payments, documents, company contexts. Source history retained, marked MERGED. | Deferred |
| CUS-008 | Customer execution is transactional and creates/updates Customer_Company_Context. | Yes |
| CUS-009 | target_version must be rechecked before executing customer change; conflict must not overwrite newer data. | Yes |

### From business-rules.md — data ownership

| Rule ID | Summary | Impact |
|---------|---------|-------|
| DATA-001 | Profiles and Customers are GLOBAL; do not duplicate per company. | Scope = GLOBAL |
| DATA-002 | Company-specific info in Customer_Company_Context, unique by (customer_id, company_id). | Scope = COMPANY |
| DATA-003 | Service, payment, reconciliation scoped by company_id. | Not in first slice |
| DATA-004 | User sees company-scoped data only for companies with effective assignment. | Enforce on Customer_Company_Context |
| DATA-005 | GLOBAL ownership does not grant access to sensitive fields. | CUSTOMER_VIEW_SENSITIVE required |
| DATA-006 | Do not use Customers.total_spent as universal source. Calculate from confirmed payments. | Defer spending view |
| DATA-007 | Do not use Customers.assigned_staff_id as sole source. Use company context/history. | Use Customer_Company_Context.assigned_staff_id |

### From business-rules.md — authorization

| Rule ID | Summary |
|---------|---------|
| AUTH-009 | Every endpoint must re-check permission and data scope at the server. |
| GOV-006 | Every business-sensitive operation must be consistent across UI, API/service and database controls. |
| GOV-007 | All material customer-master changes require immutable audit records. |
| SEC-001 | Audit and Approval_Actions are append-only; business users may not update/delete. |
| SEC-004 | CCCD, legal addresses, bank information and identity documents are masked according to permission. |

---

## 4. Customer acceptance criteria discovery

### From acceptance-criteria.md

| ID | Criterion | First slice? |
|----|-----------|:---:|
| CUS-01 | Ordinary staff cannot directly edit full_name, cccd, dob, phone, contact_address. | Yes |
| CUS-02 | Staff submit CREATE_CUSTOMER; only admin group performs final creation. | Deferred — requires workflow |
| CUS-03 | Final duplicate check blocks CREATE_CUSTOMER when active CCCD already exists. | Yes (pre-create check) |
| CUS-04 | CUSTOMER_MASTER_CHANGE target-version conflict does not overwrite newer data. | Yes |
| CUS-05 | Direct administrator correction requires reason and before/after audit. | Yes |
| CUS-06 | Customer_Company_Context is unique by customer+company and does not expose internal_notes across companies. | Yes |
| CUS-07 | Company spending matches confirmed payments; group total visible only with dedicated permission. | Deferred — requires Payment |

### Cross-cutting acceptance criteria applicable

| ID | Criterion |
|----|-----------|
| AUTH-05 | GLOBAL customer master can be searched by permission; sensitive fields masked. |
| SEC-01 | No endpoint relies only on UI visibility for authorization. |
| SEC-03 | Sensitive data in logs, exports and documents is masked/restricted by permission. |

---

## 5. Customer permission catalog discovery

### From permission-catalog.md — all 7 codes already exist

| permission_code | module_code | action_code | data_scope | sensitive | delegable | First slice? |
|----------------|-------------|-------------|------------|:---------:|:---------:|:---:|
| CUSTOMER_VIEW_BASIC | CUSTOMER | VIEW | GLOBAL | No | No | Yes |
| CUSTOMER_VIEW_SENSITIVE | CUSTOMER | VIEW_SENSITIVE | GLOBAL | Yes | No | Yes |
| CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER | PROPOSE_CHANGE | GLOBAL | No | No | Deferred — requires workflow |
| CUSTOMER_CREATE_FINAL | CUSTOMER | CREATE_FINAL | GLOBAL | Yes | No | Yes |
| CUSTOMER_MASTER_UPDATE | CUSTOMER | UPDATE_MASTER | GLOBAL | Yes | No | Yes |
| CUSTOMER_MERGE_DUPLICATE | CUSTOMER | MERGE | GLOBAL | Yes | No | Deferred |
| CUSTOMER_GROUP_FINANCE_VIEW | CUSTOMER | VIEW_GROUP_FINANCE | GLOBAL | Yes | No | Deferred — requires Payment |

All codes are GLOBAL scope. No ENTITY scope required. No new permission codes required for the first slice.

---

## 6. Existing codebase discovery

### Existing Customer code

No Customer backend, frontend, database, or test code exists. The Customer module is entirely new.

### Existing patterns to reuse

**Backend:**
- Domain entities: `private set` properties, `byte[] RowVersion`, audit columns, private EF constructor, public constructor with `CreatedAt = DateTime.UtcNow`, `Update(...)` method.
- Application services: `IOrganizationDbContextFactory` pattern, `ExecutionStrategy` + `Serializable` transactions, `BusinessRuleValidationException` with error codes, `EntityNotFoundException`, `ConcurrencyException` via `RowVersion.FromBase64()`.
- DTOs: plain classes, `RowVersion` as Base64 string, `[Required]` annotations.
- Controllers: `[ApiController]`, `[Authorize]`, `[RequirePermission(...)]` at class level, thin delegation to service.
- EF configurations: `ToTable("TableName")`, snake_case column names, named constraints (`PK_`, `UQ_`, `FK_`, `CK_`).

**Database migrations:**
- Versioned SQL files: `V0001__create_schema_versions.sql` through `V0004__seed_security_admin_manage_permission.sql`.
- Conventions: `bigint IDENTITY(1,1)` PKs, `rowversion`, `datetime2(3)`, `varchar` for codes, `nvarchar` for display, snake_case columns, PascalCase table names, CHECK constraints, filtered unique indexes.

**Frontend:**
- Module folders: one folder per module under `src/frontend/src/`.
- API files: DTO interfaces + async functions using `axiosClient`.
- Pages: Ant Design components, React Query for data fetching.
- Auth gating: `hasPermission(code, scope)` from `usePermissions()`.
- Routes: nested inside `<ProtectedRoute><AuthenticatedShell /></ProtectedRoute>`.
- Menu: conditional rendering via `hasPermission(...)`.

---

## 7. Proposed Customer module scope

### First implementation slice (Phase 1B.2-A-impl)

Direct administrator operations only. No workflow runtime.

| Capability | Scope |
|-----------|-------|
| Customer search/list | CUSTOMER_VIEW_BASIC gated |
| Customer detail view | CUSTOMER_VIEW_BASIC + CUSTOMER_VIEW_SENSITIVE for sensitive fields |
| Direct customer creation (admin) | CUSTOMER_CREATE_FINAL gated |
| Direct customer update (admin) | CUSTOMER_MASTER_UPDATE gated |
| Customer_Company_Context management | Company-scoped, enforced per user assignment |
| Duplicate detection (CCCD) | Pre-create and pre-update check |
| Sensitive field masking | CUSTOMER_VIEW_SENSITIVE required to see unmasked |
| Audit trail for changes | Reason + before/after on every mutation |
| Optimistic concurrency | rowversion on all tables |

### What the first slice does NOT include

- CREATE_CUSTOMER workflow/approval (requires workflow runtime).
- CUSTOMER_MASTER_CHANGE workflow/approval (requires workflow runtime).
- CUSTOMER_CHANGE_REQUEST_CREATE permission usage (workflow-dependent).
- Customer merge (CUSTOMER_MERGE_DUPLICATE) — complex, requires preview of services/payments/documents.
- Group spending view (CUSTOMER_GROUP_FINANCE_VIEW) — requires Payment module and vw_Customer_Spending_By_Company.
- Export/download.
- ENTITY scope.
- Service/Payment/Reconciliation modules.

---

## 8. Explicit deferred items

| # | Item | Reason |
|---|------|--------|
| 1 | Workflow/approval runtime for CREATE_CUSTOMER | Workflow module not built |
| 2 | Workflow/approval runtime for CUSTOMER_MASTER_CHANGE | Workflow module not built |
| 3 | CUSTOMER_CHANGE_REQUEST_CREATE permission usage | Requires workflow |
| 4 | Customer merge (CUSTOMER_MERGE_DUPLICATE) | Complex; requires preview of service/payment/document/context; not authorized unless later approved |
| 5 | Group spending (CUSTOMER_GROUP_FINANCE_VIEW) | Requires Payment module and vw_Customer_Spending_By_Company |
| 6 | ENTITY scope | Deferred unless separately approved |
| 7 | Service module | Deferred |
| 8 | Payment/reconciliation | Deferred |
| 9 | Security enhancement backlog | Deferred |
| 10 | Export/download | Deferred unless approved |
| 11 | vw_Customer_Spending_By_Company view | Deferred — requires Payment module |
| 12 | Customers.total_spent field | Deprecated per DATA-006; do not implement |
| 13 | Customers.assigned_staff_id field | Deprecated per DATA-007; use Customer_Company_Context |

---

## 9. Proposed database model

### Table: Profiles

GLOBAL scope. Identity and legal fields for a natural person.

| Column | Type | Nullable | Notes |
|--------|------|:--------:|-------|
| id | bigint IDENTITY(1,1) | No | PK |
| full_name | nvarchar(200) | No | |
| cccd | varchar(20) | Yes | Filtered unique index on active non-empty values |
| dob | date | Yes | Full date if known |
| dob_partial | varchar(10) | Yes | Partial date string if full DOB unknown |
| dob_precision | varchar(10) | Yes | FULL, YEAR_MONTH, YEAR, UNKNOWN |
| gender | varchar(10) | Yes | |
| permanent_address | nvarchar(500) | Yes | Sensitive |
| cccd_issue_date | date | Yes | |
| cccd_issue_place | nvarchar(200) | Yes | |
| tax_code | varchar(20) | Yes | |
| phone | varchar(20) | Yes | Duplicate signal, not unique key |
| contact_address | nvarchar(500) | Yes | |
| death_date_solar | date | Yes | |
| death_date_lunar | varchar(20) | Yes | Lunar calendar string |
| death_place | nvarchar(200) | Yes | |
| hometown | nvarchar(200) | Yes | |
| is_active | bit | No | Default 1 |
| created_at | datetime2(3) | No | |
| created_by_user_id | bigint | Yes | FK Users |
| updated_at | datetime2(3) | Yes | |
| updated_by_user_id | bigint | Yes | FK Users |
| row_version | rowversion | No | Optimistic concurrency |

**Constraints:**
- `PK_Profiles` on `id`.
- `UQ_Profiles_cccd_active` — filtered unique index on `cccd` WHERE `cccd IS NOT NULL AND is_active = 1`.
- Audit FKs to Users.

**Open decisions:**
- DEC-1B2A-03: Exact field list — are all fields from the specification required in the first slice, or can death/hometown fields be deferred?
- DEC-1B2A-04: Is `is_active` sufficient for lifecycle, or is a richer status model needed (ACTIVE, MERGED, DECEASED)?

### Table: Customers

GLOBAL scope. Business entity linking to a Profile.

| Column | Type | Nullable | Notes |
|--------|------|:--------:|-------|
| id | bigint IDENTITY(1,1) | No | PK |
| customer_code | nvarchar(50) | No | Unique business identifier |
| profile_id | bigint | No | FK Profiles |
| customer_status | varchar(20) | No | ACTIVE, INACTIVE, MERGED |
| survivor_customer_id | bigint | Yes | FK Customers, set when status = MERGED |
| created_at | datetime2(3) | No | |
| created_by_user_id | bigint | Yes | FK Users |
| updated_at | datetime2(3) | Yes | |
| updated_by_user_id | bigint | Yes | FK Users |
| row_version | rowversion | No | |

**Constraints:**
- `PK_Customers` on `id`.
- `UQ_Customers_customer_code` on `customer_code`.
- `FK_Customers_profile_id` to Profiles.
- `CK_Customers_customer_status` CHECK IN ('ACTIVE', 'INACTIVE', 'MERGED').
- `CK_Customers_survivor_null` CHECK (customer_status != 'MERGED' OR survivor_customer_id IS NOT NULL).
- Audit FKs to Users.

**Open decisions:**
- DEC-1B2A-03: Is `customer_code` auto-generated or user-entered?
- DEC-1B2A-06: MERGED status column is included for future merge support, but merge operation itself is deferred.

### Table: Customer_Company_Contexts

COMPANY scope. Per-company relationship data.

| Column | Type | Nullable | Notes |
|--------|------|:--------:|-------|
| id | bigint IDENTITY(1,1) | No | PK |
| customer_id | bigint | No | FK Customers |
| company_id | bigint | No | FK Companies |
| assigned_staff_id | bigint | Yes | FK Users |
| relationship_status | varchar(20) | No | ACTIVE, INACTIVE |
| internal_notes | nvarchar(2000) | Yes | Company-private, not visible cross-company |
| first_interaction_at | datetime2(3) | Yes | |
| last_interaction_at | datetime2(3) | Yes | |
| created_at | datetime2(3) | No | |
| created_by_user_id | bigint | Yes | FK Users |
| updated_at | datetime2(3) | Yes | |
| updated_by_user_id | bigint | Yes | FK Users |
| row_version | rowversion | No | |

**Constraints:**
- `PK_Customer_Company_Contexts` on `id`.
- `UQ_Customer_Company_Contexts_customer_company` on `(customer_id, company_id)`.
- `FK_Customer_Company_Contexts_customer_id` to Customers.
- `FK_Customer_Company_Contexts_company_id` to Companies.
- `FK_Customer_Company_Contexts_assigned_staff_id` to Users.
- `CK_Customer_Company_Contexts_relationship_status` CHECK IN ('ACTIVE', 'INACTIVE').
- Audit FKs to Users.

---

## 10. Proposed migration/rollback strategy

- Migration file: `database/migrations/V0005__create_customer_schema.sql`
- Rollback file: `database/rollbacks/U0005__drop_customer_schema.sql`
- Follow existing conventions from V0002 (Organization schema).
- Create tables in dependency order: Profiles first, then Customers, then Customer_Company_Contexts.
- Add audit FKs via ALTER TABLE at the end to avoid circular dependency with Users.
- Rollback drops tables in reverse order.
- Test database: dedicated test DB (e.g., `PTKD_TEST_PHASE1B2A`), following the existing `PTKD_TEST_PHASE1A2` pattern.

---

## 11. Proposed backend structure

### Domain

```
PTKD.Domain/
  Entities/
    Profile.cs
    Customer.cs
    CustomerCompanyContext.cs
```

Following the existing entity pattern: `private set`, `RowVersion`, audit columns, private EF constructor, public constructor, `Update(...)` method.

### Application

```
PTKD.Application/
  Customers/
    Profiles/
      DTOs/
        ProfileDto.cs
        CreateProfileRequest.cs
        UpdateProfileRequest.cs
      Services/
        IProfileService.cs
        ProfileService.cs
      Validations/
        ProfileValidators.cs
    Customers/
      DTOs/
        CustomerDto.cs
        CreateCustomerRequest.cs
        UpdateCustomerRequest.cs
        CustomerSearchRequest.cs
      Services/
        ICustomerService.cs
        CustomerService.cs
      Validations/
        CustomerValidators.cs
    CompanyContexts/
      DTOs/
        CustomerCompanyContextDto.cs
        CreateCustomerCompanyContextRequest.cs
        UpdateCustomerCompanyContextRequest.cs
      Services/
        ICustomerCompanyContextService.cs
        CustomerCompanyContextService.cs
      Validations/
        CustomerCompanyContextValidators.cs
    Common/
      Interfaces/
        ICustomerDbContext.cs
        ICustomerDbContextFactory.cs
      Services/
        DuplicateDetectionService.cs
```

### Infrastructure

```
PTKD.Infrastructure/
  Persistence/
    Configurations/
      ProfileConfiguration.cs
      CustomerConfiguration.cs
      CustomerCompanyContextConfiguration.cs
    CustomerDbContextFactory.cs  (or extend AppDbContext)
```

**Open decision:** DEC-1B2A-09 — Whether to extend the existing `AppDbContext` with Customer DbSets or create a separate bounded context. Extending `AppDbContext` is simpler and consistent with the Organization module.

---

## 12. Proposed API v2 endpoints

### Customer endpoints

| Method | Route | Permission | Scope | rowVersion | Audit | Notes |
|--------|-------|-----------|-------|:----------:|:-----:|-------|
| GET | /api/v2/customers | CUSTOMER_VIEW_BASIC | GLOBAL | No | No | Search/list with pagination |
| GET | /api/v2/customers/{id} | CUSTOMER_VIEW_BASIC | GLOBAL | No | No | Detail; sensitive fields masked unless CUSTOMER_VIEW_SENSITIVE |
| POST | /api/v2/customers | CUSTOMER_CREATE_FINAL | GLOBAL | No | Yes | Direct admin creation (Profile + Customer + Context) |
| PUT | /api/v2/customers/{id} | CUSTOMER_MASTER_UPDATE | GLOBAL | Yes | Yes | Direct admin update; requires reason |
| GET | /api/v2/customers/{id}/company-contexts | CUSTOMER_VIEW_BASIC | GLOBAL | No | No | List company contexts for a customer |
| POST | /api/v2/customers/{id}/company-contexts | CUSTOMER_CREATE_FINAL | GLOBAL | No | Yes | Add company context |
| PUT | /api/v2/customers/{id}/company-contexts/{contextId} | CUSTOMER_MASTER_UPDATE | GLOBAL | Yes | Yes | Update company context |
| GET | /api/v2/customers/duplicate-check | CUSTOMER_VIEW_BASIC | GLOBAL | No | No | Pre-create CCCD/phone check |

### Sensitive field masking

- GET endpoints return masked values for sensitive fields (cccd, permanent_address, phone, contact_address) unless the caller has CUSTOMER_VIEW_SENSITIVE.
- Masking is applied at the service layer before returning DTOs.
- Backend is authoritative for masking; frontend shows whatever the backend returns.

### Company context data isolation

- GET /api/v2/customers/{id}/company-contexts returns only contexts for companies where the calling user has an effective assignment (AUTH-007, DATA-004).
- internal_notes is never returned for companies outside the caller's assignment.

---

## 13. Proposed frontend structure

### Folder

```
src/frontend/src/customers/
  customersApi.ts
  CustomersPage.tsx
  CustomersPage.test.tsx
  CustomerDetailPage.tsx
  CustomerDetailPage.test.tsx
  CustomerCreatePage.tsx  (or modal)
  CustomerCreatePage.test.tsx
```

### Route

```tsx
<Route path="customers" element={<CustomersPage />} />
<Route path="customers/:id" element={<CustomerDetailPage />} />
<Route path="customers/create" element={<CustomerCreatePage />} />
```

### Menu placement

```tsx
{hasPermission('CUSTOMER_VIEW_BASIC', 'GLOBAL') && (
  <Menu.Item key="customers" data-testid="nav-customers">
    <Link to="/customers">Customers</Link>
  </Menu.Item>
)}
```

### Pages

| Page | Description | Permission gate |
|------|-------------|----------------|
| CustomersPage | Search/list with pagination, CCCD/name/phone filters | CUSTOMER_VIEW_BASIC GLOBAL |
| CustomerDetailPage | Read-only detail with masked sensitive fields, company contexts | CUSTOMER_VIEW_BASIC GLOBAL |
| CustomerCreatePage | Admin creation form (Profile + Customer + Context) | CUSTOMER_CREATE_FINAL GLOBAL |
| Edit (inline or separate) | Admin update with reason, rowVersion | CUSTOMER_MASTER_UPDATE GLOBAL |

### Actions gating

| Action | Permission | Notes |
|--------|-----------|-------|
| View customer list | CUSTOMER_VIEW_BASIC GLOBAL | Menu and route visible |
| View customer detail | CUSTOMER_VIEW_BASIC GLOBAL | |
| View sensitive fields | CUSTOMER_VIEW_SENSITIVE GLOBAL | Fields unmasked only if permission present |
| Create customer | CUSTOMER_CREATE_FINAL GLOBAL | Button/route visible only with permission |
| Update customer | CUSTOMER_MASTER_UPDATE GLOBAL | Edit button visible only with permission |
| Merge | CUSTOMER_MERGE_DUPLICATE GLOBAL | Deferred — button hidden |
| View group spending | CUSTOMER_GROUP_FINANCE_VIEW GLOBAL | Deferred — section hidden |

---

## 14. Proposed audit/security behavior

- Every customer create/update operation records immutable audit with:
  - Actor (user_id from JWT).
  - Entity type + entity ID.
  - Company ID (for company context operations).
  - Action code (CUSTOMER_CREATE, CUSTOMER_UPDATE, CONTEXT_CREATE, CONTEXT_UPDATE).
  - Changed fields with before/after values.
  - Reason (mandatory for updates per CUS-004).
  - Correlation ID.
  - Timestamp.
- Audit records are append-only (SEC-001).
- Sensitive fields in audit before/after are stored but masked on read per CUSTOMER_VIEW_SENSITIVE (SEC-003).
- No passwords, tokens, or file bytes in audit data (SEC-005).

**Open decision:** DEC-1B2A-15 — Whether to use database triggers (like Phase 1B.0 DEC-1B-015) or application-layer audit in the same transaction. Existing Organization module uses application-layer audit; customer module should follow the same pattern unless a specific trigger-based approach is mandated.

---

## 15. Proposed test strategy

### Backend unit tests

- Profile entity: constructor, update, validation.
- Customer entity: constructor, update, status transitions.
- CustomerCompanyContext entity: constructor, update.
- DuplicateDetectionService: CCCD match, phone signal.
- ProfileValidators, CustomerValidators, CustomerCompanyContextValidators.
- RowVersion handling and concurrency.
- Sensitive field masking logic.

### Integration tests

- CustomerService: CRUD with real database.
- Duplicate detection: CCCD unique constraint enforcement.
- Concurrency: rowversion conflict detection.
- Transaction atomicity: Profile + Customer + Context creation.
- Company context isolation: cross-company data not returned.
- Audit trail: before/after recorded on mutations.

### API tests

- All 8 proposed endpoints: success and error cases.
- Permission enforcement: 403 without required permission.
- Sensitive field masking: masked without CUSTOMER_VIEW_SENSITIVE.
- Duplicate check: 409 on CCCD conflict.
- Concurrency: 409 on stale rowVersion.
- Company context: only user-assigned companies returned.
- ProblemDetails format with errorCode and correlationId.

### Frontend tests

- CustomersPage: renders list, search filters, permission gating.
- CustomerDetailPage: renders detail, masked vs unmasked fields.
- CustomerCreatePage: form validation, submission, error handling.
- Edit flow: reason required, rowVersion submitted.
- Menu visibility: CUSTOMER_VIEW_BASIC gate.
- Action visibility: CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE gates.

### Migration/rollback verification

- V0005 forward migration creates all 3 tables with correct constraints.
- U0005 rollback drops all 3 tables cleanly.
- Test database isolation (dedicated test DB, not PTKD_DEV).

---

## 16. Risks/blockers

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Workflow module not built; CUS-002 staff-initiated creation deferred | Medium | First slice uses direct admin creation only |
| 2 | Customer merge (CUS-007) is complex | Medium | Deferred unless explicitly approved |
| 3 | Group spending (CUS-07) requires Payment module | Low | Deferred |
| 4 | Sensitive data masking design must be correct from the start | Medium | Design masking at service layer; backend authoritative |
| 5 | No git remote configured | Low | Configure before push |
| 6 | PTKD-ERP-Master-Context.md not found | Low | Continue with existing docs |
| 7 | Audit table design — application vs trigger | Low | Follow existing Organization pattern (application-layer) |
| 8 | Profile vs Customer separation may add complexity | Medium | Follows spec exactly; Profile is identity, Customer is business entity |
| 9 | Test database naming convention | Low | Follow existing PTKD_TEST_PHASE1A2 pattern |
| 10 | Scope change from frontend-only (Phase 1B.1) to full-stack | Medium | Explicit PO authorization required before implementation |

---

## 17. Required Project Owner decisions

| Decision ID | Topic | Proposed decision | Alternatives |
|-------------|-------|-------------------|-------------|
| DEC-1B2A-01 | Approve Customer module as next implementation area | Approve | Select alternative module |
| DEC-1B2A-02 | Approve first implementation slice scope | Direct admin operations only; no workflow | Include workflow stub |
| DEC-1B2A-03 | Approve customer data fields | All fields from spec Section 5.1 in Profiles; customer_code + status in Customers | Defer death/hometown fields |
| DEC-1B2A-04 | Approve customer lifecycle/status model | Profiles: is_active. Customers: ACTIVE/INACTIVE/MERGED. Contexts: ACTIVE/INACTIVE | Richer status model |
| DEC-1B2A-05 | Approve duplicate detection rules | Filtered unique index on CCCD; phone as signal only; pre-create check endpoint | Add name/DOB fuzzy match |
| DEC-1B2A-06 | Customer merge scope | Deferred from first slice. MERGED status column included for future use | Include merge in first slice |
| DEC-1B2A-07 | Workflow/approval runtime scope | Deferred. First slice is direct admin only | Build workflow module first |
| DEC-1B2A-08 | ENTITY scope | Remains deferred. All customer permissions are GLOBAL | Introduce ENTITY |
| DEC-1B2A-09 | Database table design | 3 tables (Profiles, Customers, Customer_Company_Contexts) following Organization patterns. Extend AppDbContext | Separate bounded context |
| DEC-1B2A-10 | Migration/rollback strategy | V0005/U0005 following existing pattern. Dedicated test DB | Different versioning |
| DEC-1B2A-11 | API v2 endpoint set | 8 endpoints under /api/v2/customers/ as documented | Different route structure |
| DEC-1B2A-12 | Frontend route/menu/page structure | /customers route, Ant Design, React Query, permission-gated menu | Different structure |
| DEC-1B2A-13 | Permission gates | Use existing catalog codes: CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE | Different gate mapping |
| DEC-1B2A-14 | New permission codes | No new codes required for first slice | Add codes |
| DEC-1B2A-15 | Audit/security behavior | Application-layer audit in transaction, following Organization pattern | Database triggers |
| DEC-1B2A-16 | Test strategy | Unit + Integration + API + Frontend tests, following existing patterns | Different strategy |
| DEC-1B2A-17 | Service/Payment dependencies | Deferred from first Customer slice | Include spending view |
| DEC-1B2A-18 | Implementation timing | No implementation until plan acceptance | Begin immediately |

---

## 18. Implementation phase recommendation

After Project Owner plan acceptance, implement in sub-phases:

**Phase 1B.2-A1 — Customer Database Foundation**
- V0005 migration: Profiles, Customers, Customer_Company_Contexts tables.
- U0005 rollback.
- Database safety tests.
- Migration verification.

**Phase 1B.2-A2 — Customer Backend API**
- Domain entities, application services, DTOs, validators.
- API controllers with permission enforcement.
- Duplicate detection service.
- Sensitive field masking.
- Audit trail on mutations.
- Unit tests, integration tests, API tests.

**Phase 1B.2-A3 — Customer Frontend UI**
- Customer list/search page.
- Customer detail page with masking.
- Customer create page (admin).
- Customer edit with reason and rowVersion.
- Company context management.
- Frontend tests.
- Menu and route gating.

Each sub-phase follows the existing lifecycle: plan acceptance, implementation, implementation acceptance, closure review, final acceptance.

---

## 19. Authorization statement

No source implementation is authorized by this plan until Project Owner plan acceptance. This document is a discovery and detailed plan only.

---

## 20. Conclusion

The Customer module has clear documentation support from business rules (CUS-001 through CUS-009), acceptance criteria (CUS-01 through CUS-07), and permission catalog (7 existing codes). The first implementation slice focuses on direct administrator operations using CUSTOMER_VIEW_BASIC, CUSTOMER_VIEW_SENSITIVE, CUSTOMER_CREATE_FINAL, and CUSTOMER_MASTER_UPDATE — all GLOBAL scope, no ENTITY required. Workflow/approval, merge, and spending are explicitly deferred. The proposed architecture follows existing Organization and Security module patterns throughout backend, database, and frontend.

PHASE 1B.2-A CUSTOMER MODULE DETAILED PLAN READY FOR PROJECT OWNER REVIEW
