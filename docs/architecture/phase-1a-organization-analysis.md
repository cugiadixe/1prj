# PTKD ERP - Phase 1A Organization Analysis

## 1. Confirmed business decisions
1. Users may belong to multiple companies.
2. By default, read-only list screens may aggregate data from all companies the user can access.
3. Except for system administrators, regular users must belong to Headquarters or a subsidiary company.
4. Users may belong to multiple departments.
5. A user must have exactly one primary department per active company.
6. A user may have one primary company for default UI context.
7. Companies have a parent-child hierarchy.
8. Departments have a parent-child hierarchy.
9. Department codes are globally unique.
10. Employee codes are globally unique.
11. Employment status is separate from account status.
12. Organizational transfers must preserve history.
13. `Users.id` uses `bigint IDENTITY`.

## 2. Explicitly deferred decisions
- Roles and Permission assignments (Phase 1B).
- JWT Authentication and Login mechanisms (Phase 1B).
- Company authorization enforcement is deferred until Phase 1B. Phase 1A must not pretend that authorized company scope can be determined without authentication and permission context; Phase 1A only records the structural assignment data.
- Approval workflow configurations and bindings (Phase 2).
- Seeding system administrator users or Headquarters data. Seed data is strictly deferred until actual business values are explicitly supplied in Phase 1A.

## 3. Tables and responsibilities
- **`Companies`**: Stores legal entities and branches in a hierarchical structure. Headquarters is represented as a company record.
- **`Departments`**: Stores logical working groups, strictly scoped to a specific company.
- **`Users`**: Stores employee profiles, employment status, and account login status.
- **`User_Company_Assignments`**: Temporal records managing the lifecycle of a user's membership in a company and identifying the UI default primary company.
- **`User_Department_Assignments`**: Temporal records managing a user's department memberships.
- **`Employment_Histories`**: An append-only business-event log for tracking explicit transfer actions. It must not replace temporal assignment records as the authoritative source of organization membership.

## 4. Full column definitions

### `Companies`
- `id` (bigint IDENTITY, NOT NULL)
- `company_code` (varchar(50), NOT NULL)
- `parent_company_id` (bigint, NULL)
- `name` (nvarchar(200), NOT NULL)
- `tax_code` (varchar(50), NULL)
- `is_active` (bit, NOT NULL)
- `row_version` (rowversion, NOT NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)
- `updated_at` (datetime2(3), NULL)
- `updated_by_user_id` (bigint, NULL)

### `Departments`
- `id` (bigint IDENTITY, NOT NULL)
- `department_code` (varchar(50), NOT NULL)
- `company_id` (bigint, NOT NULL)
- `parent_department_id` (bigint, NULL)
- `name` (nvarchar(200), NOT NULL)
- `is_active` (bit, NOT NULL)
- `row_version` (rowversion, NOT NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)
- `updated_at` (datetime2(3), NULL)
- `updated_by_user_id` (bigint, NULL)

### `Users`
- `id` (bigint IDENTITY, NOT NULL)
- `employee_code` (varchar(50), NOT NULL)
- `full_name` (nvarchar(200), NOT NULL)
- `email` (varchar(200), NULL)
- `employment_status` (varchar(30), NOT NULL)
- `account_status` (varchar(30), NOT NULL)
- `row_version` (rowversion, NOT NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)
- `updated_at` (datetime2(3), NULL)
- `updated_by_user_id` (bigint, NULL)

### `User_Company_Assignments`
- `id` (bigint IDENTITY, NOT NULL)
- `user_id` (bigint, NOT NULL)
- `company_id` (bigint, NOT NULL)
- `is_primary` (bit, NOT NULL)
- `assignment_status` (varchar(30), NOT NULL)
- `effective_from` (datetime2(3), NOT NULL)
- `effective_to` (datetime2(3), NULL)
- `row_version` (rowversion, NOT NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)
- `updated_at` (datetime2(3), NULL)
- `updated_by_user_id` (bigint, NULL)

### `User_Department_Assignments`
- `id` (bigint IDENTITY, NOT NULL)
- `user_id` (bigint, NOT NULL)
- `department_id` (bigint, NOT NULL)
- `user_company_assignment_id` (bigint, NOT NULL)
- `company_id` (bigint, NOT NULL)
- `is_primary_for_company` (bit, NOT NULL)
- `assignment_status` (varchar(30), NOT NULL)
- `effective_from` (datetime2(3), NOT NULL)
- `effective_to` (datetime2(3), NULL)
- `row_version` (rowversion, NOT NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)
- `updated_at` (datetime2(3), NULL)
- `updated_by_user_id` (bigint, NULL)

### `Employment_Histories`
- `id` (bigint IDENTITY, NOT NULL)
- `user_id` (bigint, NOT NULL)
- `from_company_id` (bigint, NULL)
- `to_company_id` (bigint, NULL)
- `from_department_id` (bigint, NULL)
- `to_department_id` (bigint, NULL)
- `from_company_assignment_id` (bigint, NULL)
- `to_company_assignment_id` (bigint, NULL)
- `from_department_assignment_id` (bigint, NULL)
- `to_department_assignment_id` (bigint, NULL)
- `action_type` (varchar(50), NOT NULL)
- `reason` (nvarchar(500), NULL)
- `effective_date` (datetime2(3), NOT NULL)
- `correlation_id` (uniqueidentifier, NULL)
- `created_at` (datetime2(3), NOT NULL)
- `created_by_user_id` (bigint, NULL)

## 5. Primary keys
All tables use a surrogate `id bigint IDENTITY NOT NULL` as the primary key.

## 6. Foreign keys
- `Companies.parent_company_id` -> `Companies.id`
- `Departments.company_id` -> `Companies.id`
- `Departments.(parent_department_id, company_id)` -> `Departments.(id, company_id)` *(Enforces parent department belongs to the same company)*
- `User_Company_Assignments.user_id` -> `Users.id`
- `User_Company_Assignments.company_id` -> `Companies.id`
- `User_Department_Assignments.user_id` -> `Users.id`
- `User_Department_Assignments.(department_id, company_id)` -> `Departments.(id, company_id)` *(Enforces department matches the company context)*
- `User_Department_Assignments.(user_company_assignment_id, user_id, company_id)` -> `User_Company_Assignments.(id, user_id, company_id)` *(Guaranteeing that a department assignment belongs to the same user and company as its referenced company assignment, preventing cross-user mismatch)*
- `Employment_Histories` FKs to `Users.id`, `Companies.id`, `Departments.id`, `User_Company_Assignments.id`, and `User_Department_Assignments.id`.
- All `created_by_user_id` and `updated_by_user_id` fields -> `Users.id` (NO ACTION to prevent cascade).

## 7. Unique constraints
- `Companies`: `UNIQUE(company_code)`
- `Departments`: `UNIQUE(department_code)`
- `Departments`: `UNIQUE(id, company_id)` *(To support the composite foreign keys)*
- `Users`: `UNIQUE(employee_code)`
- `User_Company_Assignments`: `UNIQUE(id, user_id, company_id)` *(To support the composite foreign key from User_Department_Assignments, preventing cross-user mismatch)*

## 8. Check constraints
- `Companies`: `CHECK (parent_company_id <> id)`
- `Departments`: `CHECK (parent_department_id <> id)`
- **Temporal constraints**: `CHECK (effective_to IS NULL OR effective_to >= effective_from)`
- **Consistency constraints**: 
  - `CHECK ((assignment_status = 'ACTIVE' AND effective_to IS NULL) OR (assignment_status = 'CLOSED' AND effective_to IS NOT NULL))`
- Assignments: `CHECK (assignment_status IN ('ACTIVE', 'CLOSED'))`
*(Note: SQL CHECK constraints for `employment_status` and `account_status` are explicitly deferred from V0002. Application logic may validate initial technical values temporarily).*

## 9. Filtered indexes for current and primary assignments
- **At most one active assignment for the same user and company:**
  `CREATE UNIQUE INDEX UQ_User_Company_Active ON User_Company_Assignments(user_id, company_id) WHERE assignment_status = 'ACTIVE'`
- **At most one active primary company per user:**
  `CREATE UNIQUE INDEX UQ_User_Primary_Company ON User_Company_Assignments(user_id) WHERE assignment_status = 'ACTIVE' AND is_primary = 1`
- **At most one active assignment for the same user and department:**
  `CREATE UNIQUE INDEX UQ_User_Dept_Active ON User_Department_Assignments(user_id, department_id) WHERE assignment_status = 'ACTIVE'`
- **At most one active primary department per user per company:**
  `CREATE UNIQUE INDEX UQ_User_Company_Primary_Dept ON User_Department_Assignments(user_id, company_id) WHERE assignment_status = 'ACTIVE' AND is_primary_for_company = 1`

*Important note:* Filtered unique indexes enforce **AT MOST** one active primary assignment, not EXACTLY one. Ensuring "exactly one" active primary department per active company assignment requires transactional application validation.

## 10. Hierarchy validation
- SQL Server `CHECK` constraints prevent direct self-references (`parent_id <> id`). This applies to both `Companies.parent_company_id` and `Departments.parent_department_id`.
- Deep cycles (A -> B -> C -> A) in both Company and Department hierarchies are blocked transactionally by application logic or stored procedures.

## 11. Temporal assignment and transfer transaction behavior
- Historical assignment rows **must not be overwritten**. Closing an assignment sets `effective_to` to the current timestamp, changes status to `CLOSED`, and leaves the row intact. Creating a new assignment creates an entirely new row.
- **Transactional Consistency:**
  - Every active company assignment must have exactly one active primary department assignment.
  - Closing or transferring a primary department must create or select its replacement in the same database transaction.
  - Closing a company assignment must close all active department assignments tied to it in the same transaction.
  - Application logic must validate that a department assignment's effective interval (`effective_from` -> `effective_to`) is fully contained within its parent `User_Company_Assignments` interval.
- **Assigning an Additional Company**:
  - Requires `companyId`, `primaryDepartmentId`, `effectiveFrom`, `isPrimaryCompany`, and `reason`.
  - Must not create a company assignment without a primary department.
  - The transaction must atomically: create the active company assignment, create its active primary department assignment, append an `Employment_Histories` event, and commit or roll back all actions together.
- **Employment_Histories Rules**:
  - The transaction writing `Employment_Histories` must verify: all referenced assignment IDs belong to the same user; referenced company and department IDs match their assignments; from/to assignments match the transfer operation; and the history record is inserted in the same transaction as assignment changes.
  - `Employment_Histories` is purely append-only: no update API, no delete API, and normal application persistence must expose insert only.

## 12. Employment status values
- `WORKING`, `ON_LEAVE`, `RESIGNED`. *(These are initial technical values unless the complete business status catalog has been confirmed. Do not create hard SQL CHECK constraints for them yet).*

## 13. Account status values
- `ACTIVE`, `LOCKED`. *(Initial technical values. Do not create hard SQL CHECK constraints for them yet).*

## 14. Audit metadata
- All tables contain `created_at`, `created_by_user_id`.
- Mutatable tables include `updated_at`, `updated_by_user_id`.
- `Employment_Histories` acts as an explicit business-level audit log for organizational changes, maintaining its own `correlation_id` and `reason` for tracing.

## 15. rowversion behavior
- All mutable tables have a `row_version` (rowversion).
- Updates must include a `target_version` parameter. If the database `row_version` has advanced past the `target_version`, the transaction rolls back and returns a concurrency conflict error.

## 16. API v2 endpoints
- `GET /api/v2/organizations/companies`
- `GET /api/v2/organizations/companies/{id}`
- `POST /api/v2/organizations/companies`
- `PUT /api/v2/organizations/companies/{id}`
- `GET /api/v2/organizations/departments`
- `GET /api/v2/organizations/departments/{id}`
- `POST /api/v2/organizations/departments`
- `PUT /api/v2/organizations/departments/{id}`
- `GET /api/v2/organizations/users`
- `GET /api/v2/organizations/users/{id}`
- `POST /api/v2/organizations/users`
- `PUT /api/v2/organizations/users/{id}`
- `POST /api/v2/organizations/users/{id}/companies` *(Assign an additional company and primary department atomically)*
- `POST /api/v2/organizations/users/{id}/departments` *(Assign an additional department)*
- `PUT /api/v2/organizations/users/{id}/companies/{companyId}/primary` *(Change primary company)*
- `PUT /api/v2/organizations/users/{id}/departments/{departmentId}/primary` *(Change primary department)*
- `PUT /api/v2/organizations/users/{id}/companies/{companyId}/close` *(Close a company assignment and its cascading departments)*
- `POST /api/v2/organizations/users/{id}/transfer` *(Atomic transfer between organizations)*

## 17. Request DTO fields
- **CreateUserRequest**: `employeeCode`, `fullName`, `email`, `employmentStatus`, `accountStatus`, `initialCompanyId`, `initialDepartmentId`, `effectiveFrom`, `reason`.
- **UpdateUserRequest**: *Only profile fields* `employeeCode`, `fullName`, `email`, `employmentStatus`, `accountStatus`, `targetVersion`. *(Must not contain `initialCompanyId` or `initialDepartmentId`. Changes to company/dept must use assignment/transfer transactions).*
- **AssignCompanyRequest**: `companyId`, `primaryDepartmentId`, `effectiveFrom`, `isPrimaryCompany`, `reason`.
- **TransferUserRequest**: `fromCompanyId` (optional), `toCompanyId`, `fromDepartmentId` (optional), `toDepartmentId`, `isPrimaryForCompany`, `effectiveDate`, `reason`, `targetAssignmentVersion`

## 18. Response DTO fields
- **UserDto**: `id`, `employeeCode`, `fullName`, `email`, `employmentStatus`, `accountStatus`, `rowVersion`.
- **AssignmentDto**: `id`, `companyId`, `departmentId`, `userCompanyAssignmentId`, `isPrimary`, `isPrimaryForCompany`, `assignmentStatus`, `effectiveFrom`, `effectiveTo`.
*(Read APIs aggregate authorized companies; every record must expose its `company_id`).*

## 19. Pagination, sorting and filters
- Standard pagination (`pageIndex`, `pageSize`).
- Sorting by `name ASC` or `createdAt DESC`.
- Filtering via query parameters: `?isActive=true&companyId=123&searchTerm=john`.

## 20. Stable business error codes
- `ORG_USER_NOT_FOUND`
- `ORG_COMPANY_NOT_FOUND`
- `ORG_DEPARTMENT_NOT_FOUND`
- `ORG_COMPANY_ASSIGNMENT_REQUIRED`
- `ORG_PRIMARY_DEPARTMENT_REQUIRED`
- `ORG_DEPARTMENT_COMPANY_MISMATCH`
- `ORG_ASSIGNMENT_DATE_OUT_OF_RANGE`
- `ORG_INACTIVE_COMPANY`
- `ORG_INACTIVE_DEPARTMENT`
- `ORG_INVALID_ROW_VERSION`
- `ORG_DEPARTMENT_CYCLE_DETECTED`
- `ORG_TEMPORAL_OVERLAP`
- `ORG_DUPLICATE_EMPLOYEE_CODE`
- `ORG_DUPLICATE_DEPARTMENT_CODE`

## 21. Validation rules
- A user's `employee_code` must be unique system-wide.
- Parent assignments must not introduce a hierarchy cycle.
- The `effective_from` date must not precede an existing closed assignment's timeline for the same user-entity pair.
- Create/Update/Transfer operations require explicit `company_id`.
- The "Primary Company" is solely a UI navigation default and grants zero authorization context.
- Application APIs do not physically delete organization data; records are deactivated or assignments are closed.

## 22. Company-scope and security boundaries
- Read APIs aggregate data from all authorized companies for the caller unless explicitly filtered.
- Company authorization enforcement is purely deferred to Phase 1B authentication/authorization context.

## 23. Minimal frontend screens
- Company Tree Management List.
- Department Tree Management List (filtered by company).
- User Profile Directory.
- User Transfer / Assignment Wizard Modal.

## 24. Deferred frontend screens
- Roles/Permissions Management.
- Login screen.

## 25. V0002 migration plan
1. Create `Users`, `Companies`, `Departments`, `User_Company_Assignments`, `User_Department_Assignments`, `Employment_Histories` **without** the `created_by_user_id` and `updated_by_user_id` foreign keys to avoid circular dependency problems on creation.
2. Add all Primary Keys, Foreign Keys (for hierarchy/companies/departments), Unique constraints, and Check constraints.
3. Apply Filtered Unique Indexes.
4. **Finally**, `ALTER TABLE` to add all `created_by_user_id` and `updated_by_user_id` foreign keys pointing to `Users.id`.
5. No SQL `CHECK` constraints for `employment_status` or `account_status` are added.
6. No seed data is applied.

## 26. U0002 rollback order
1. `ALTER TABLE` to drop all `created_by_user_id` and `updated_by_user_id` foreign keys to allow safe unspooling.
2. Drop tables in strict dependency order:
   - `Employment_Histories`
   - `User_Department_Assignments`
   - `User_Company_Assignments`
   - `Departments` *(Drop before Companies)*
   - `Companies` *(Drop before Users, as it might reference Users in remaining structural ways)*
   - `Users` *(Dropped last, ensuring nothing references it)*
*(Rollback scripts physically drop Phase 1A objects, whereas the application only uses logical/soft close).*

## 27. Unit tests
- Test hierarchy cycle detection logic in isolation for both Company and Department hierarchies.
- Test date overlap logic for temporal assignments before executing DB calls.
- Verify exact 1 active primary department rule logic per company assignment.
- Verify `CreateUserRequest` correctly handles and applies the `effectiveFrom` date.

## 28. Integration tests
- Verify filtered indexes throw exceptions when attempting to add two active primary departments for the same user in a single company.
- Verify `ORG_INVALID_ROW_VERSION` is thrown correctly on concurrency conflicts.
- Verify rejection of a department assignment linked to another user's company assignment.
- Verify rollback occurs cleanly when an atomic assignment (e.g. additional company + primary department) fails midway (e.g., primary department creation fails).

## 29. API tests
- `GET /api/v2/organizations/users` correctly aggregates assignments and parses pagination.
- `POST /api/v2/organizations/users/{id}/transfer` effectively closes the previous assignment (updating `effective_to`), creates a new active assignment, and writes to `Employment_Histories`.
- `POST /api/v2/organizations/users/{id}/companies` successfully creates a company assignment and its primary department assignment atomically.

## 30. Acceptance criteria with stable IDs
- **ORG-001**: System provides API to read aggregated user lists across authorized companies.
- **ORG-002**: Atomic user creation transaction successfully creates the user, active company assignment, primary department assignment, and appends `JOINED` history.
- **ORG-003**: System structurally enforces that a department assignment's `company_id` matches its parent `User_Company_Assignments`, and strictly belongs to the same `user_id`.
- **ORG-004**: System preserves historical assignments; closing an assignment only updates `effective_to` and `assignment_status` without physical deletion.
- **ORG-005**: Closing or transferring a primary department enforces the atomic creation/selection of a replacement primary department in the same transaction.
- **ORG-006**: Closing a company assignment cascades to atomically close all active child department assignments.
- **ORG-007**: Invalid cross-company department hierarchy references, and deep hierarchy cycles (both Companies and Departments), are prevented by database schema constraints and application logic.
- **ORG-008**: Assignment temporal interval overlaps and concurrency conflicts throw stable business errors (`ORG_TEMPORAL_OVERLAP`, `ORG_INVALID_ROW_VERSION`).
- **ORG-009**: U0002 Rollback order executes cleanly without foreign-key drop violations.
- **ORG-010**: Atomic assignment of an additional company and primary department succeeds, or rolls back entirely if any step fails.
- **ORG-011**: Transactions writing to `Employment_Histories` reject any mismatched user, company, or department assignment references.

## 31. Risks and unresolved questions
- **Risk**: Verifying historical timeline overlaps (`effective_from`/`effective_to` overlaps for closed records) requires custom procedural validation because SQL Server unique indexes cannot enforce "no overlap" for ranges across multiple rows natively without triggers.
- **Risk**: Deep hierarchy traversal performance without materialized path/HierarchyId might degrade if company/department trees become exceptionally deep.
