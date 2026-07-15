# Biểu mẫu Đánh giá Phase 1B.0 — DBA Lead

## 1. Mục đích tài liệu (Document purpose)
Thu thập các phê duyệt chính thức từ **DBA Lead** đối với các thiết kế bảo mật và kiến trúc của Phase 1B.0.

## 2. Trạng thái dự án hiện tại (Current project status)
- Phase 1A.2: **CLOSED** (Commit: `3ea30fb`, Tag: `phase-1a2-application-api-v1.0`)
- Phase 1B.0 documentation: **COMMITTED** (`37b2d60`)
- Stakeholder decisions: tất cả **OPEN**
- Phase 1B.1: **NOT AUTHORIZED**

## 3. Vai trò người phê duyệt (Reviewer role): DBA Lead

## 4. Hướng dẫn đánh giá (Instructions for completing the review)
1. Đọc đề xuất và các phương án thay thế cho từng quyết định.
2. Điền vào ô **Phản hồi của người đánh giá** đúng một trong các giá trị:
   - APPROVED
   - APPROVED WITH CONDITIONS
   - REJECTED
   - DEFERRED — NON-BLOCKING
   - REMAIN OPEN
3. Nếu APPROVED WITH CONDITIONS: ghi rõ điều kiện cụ thể và có thể kiểm tra được.
4. Mọi quyết định vẫn ở trạng thái OPEN cho đến khi được ghi nhận vào `docs/decisions/phase-1b0-open-decisions.md`.
5. Việc hoàn thành biểu mẫu này **không tự động** cập nhật sổ đăng ký quyết định.
6. **DEC-1B-008** đã được **MERGED INTO DEC-1B-007** và không được đánh giá riêng.
7. Không ai được duyệt ngoài thẩm quyền được giao nếu không có ủy quyền rõ ràng từ Project Owner.
8. Phase 1B.1 không thể bắt đầu khi vẫn còn quyết định blocking chưa được giải quyết.

## 5. Các quyết định dành cho người phê duyệt này (11 quyết định)
- DEC-1B-001 — Login identifier
- DEC-1B-002 — Password policy
- DEC-1B-004 — Lockout values
- DEC-1B-005 — Refresh token schema & family
- DEC-1B-006 — Permission catalog schema
- DEC-1B-007 — Role and Admin-Group scope
- DEC-1B-009 — Admin group model
- DEC-1B-011 — Permission cache failure
- DEC-1B-014 — Temporal locking mechanism
- DEC-1B-015 — Audit database controls
- DEC-1B-017 — Security audit retention/archive

## 6. Bảng quyết định liên chức năng (Cross-functional decisions — all 20 active)

*Một quyết định yêu cầu nhiều người phê duyệt sẽ không được coi là đã phê duyệt cho đến khi tất cả những người phê duyệt cung cấp phản hồi tương thích.*

| Decision ID | Decision owner | Required approvers | Phản hồi BA | Phản hồi DBA | Phản hồi Security | Điều kiện tương thích | Xung đột | Quyết định cuối | Blocking resolved |
|---|---|---|---|---|---|---|---|---|---|
| DEC-1B-001 | Backend Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-002 | Security Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-003 | Security Lead | BA, Security | | | | | | | |
| DEC-1B-004 | Security Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-005 | Backend Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-006 | DBA Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-007 | BA/Product Owner | BA, DBA, Security | | | | | | | |
| DEC-1B-009 | BA/Product Owner | BA, DBA, Security | | | | | | | |
| DEC-1B-010 | Infrastructure Lead | BA, Security | | | | | | | |
| DEC-1B-011 | Backend Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-012 | Backend Lead | BA, Security | | | | | | | |
| DEC-1B-013 | BA/Product Owner | BA, Security | | | | | | | |
| DEC-1B-014 | DBA Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-015 | DBA Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-016 | BA/Product Owner | BA, Security | | | | | | | |
| DEC-1B-017 | DBA Lead | BA, DBA, Security | | | | | | | |
| DEC-1B-018 | Security Lead | BA, Security | | | | | | | |
| DEC-1B-019 | Security Lead | BA, Security | | | | | | | |
| DEC-1B-020 | Backend Lead | BA, Security | | | | | | | |
| DEC-1B-021 | BA/Product Owner | BA, Security | | | | | | | |

## 7. Biểu mẫu phản hồi từng quyết định (Decision response form — 11 sections)

### DEC-1B-001 — Login identifier
- **Decision ID:** DEC-1B-001
- **Chủ đề (Topic):** Login identifier
- **Chủ sở hữu quyết định (Decision owner):** Backend Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Separate `User_Auth_Accounts` table. Columns: `provider_type`, `provider_subject` (unique). Password hash nullable for external providers.
- **Lý do cần quyết định (Why the decision is required):** Users table currently lacks login/authentication mapping. A separate identity model is needed to support authentication without exposing password data through organization APIs.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Add login columns to `Users` table directly.
- **Rủi ro (Risks):** Requires joined queries for authentication. Syncing user deletion with auth accounts adds complexity.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: User_Auth_Accounts (FK to Users). Would require a future V0003 migration if the decision is approved and Phase 1B.1 implementation is separately authorized.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/login, POST /api/v2/auth/refresh, GET /api/v2/auth/me use User_Auth_Accounts.
- **Tác động Security (Security impact):** Separates credential data from business user data. Mitigates password exposure through organization APIs.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: AuthAccounts_LoginName_Is_Unique. API: SecurityStamp_Change_Invalidates_Token.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Cấu trúc bảng User_Auth_Accounts mới (khóa ngoại trỏ đến Users) và ràng buộc UNIQUE(provider_type, provider_subject) có hợp lý và tối ưu không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-002 — Password policy
- **Decision ID:** DEC-1B-002
- **Chủ đề (Topic):** Password policy
- **Chủ sở hữu quyết định (Decision owner):** Security Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** ASP.NET Core PasswordHasher. Min 8, max 64. Temp password lifetime 24h. Reset instantly revokes sessions. No reuse within last 5 passwords. Cannot contain `normalized_provider_subject`. Configuration-driven lockout on 5 fails.
- **Lý do cần quyết định (Why the decision is required):** No authentication exists yet. Password policy parameters must be decided before implementing login.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Rejected / unacceptable security alternative: plaintext password storage, or default Identity policy without history.
- **Rủi ro (Risks):** Minimum length 8 may be weaker than future corporate policy. Password-history storage increases schema and maintenance complexity. Temporary-password expiry can create support requests. Account lockout can be abused for targeted account denial of service.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Password_History (FK to User_Auth_Accounts). Index on (user_auth_account_id, created_at DESC).
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/change-password validates policy. POST /api/v2/security/accounts/{id}/reset sets temporary password.
- **Tác động Security (Security impact):** Password policy and lockout mitigate password guessing and password reuse risks. Temporary password forces immediate change.
- **Tác động Test dự kiến (Proposed test impact if approved):** Unit: PasswordHasher_Uses_AspNet_Implementation, PasswordHistory_Prevents_Reuse_Of_Last_5, TemporaryPassword_Fails_After_Expiry.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Bảng Password_History với index trên (user_auth_account_id, created_at DESC) có ảnh hưởng hiệu năng không? Các SQL types và lengths có chấp nhận được không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-004 — Lockout values
- **Decision ID:** DEC-1B-004
- **Chủ đề (Topic):** Lockout values
- **Chủ sở hữu quyết định (Decision owner):** Security Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** 5 failures = 15 minute lockout. Configuration driven.
- **Lý do cần quyết định (Why the decision is required):** No tracking for failed logins exists. Lockout behavior must be defined to prevent brute-force attacks.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** No lockout policy.
- **Rủi ro (Risks):** Account lockout can be abused for targeted account denial of service. Legitimate users may be locked out during credential rotation.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** Columns `failed_attempt_count`, `lockout_end` in User_Auth_Accounts.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/login returns AUTH_ACCOUNT_LOCKED after threshold. POST /api/v2/security/accounts/{id}/unlock clears lockout.
- **Tác động Security (Security impact):** Mitigates password guessing attacks while introducing account-denial risk. Risk of DoS against individual accounts.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: AccountLockout_Is_Triggered_On_Failures.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Việc cập nhật liên tục failed_attempt_count và lockout_end trong User_Auth_Accounts có gây deadlock hoặc ảnh hưởng hiệu năng không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-005 — Refresh token schema & family
- **Decision ID:** DEC-1B-005
- **Chủ đề (Topic):** Refresh token schema & family
- **Chủ sở hữu quyết định (Decision owner):** Backend Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `session_id` family rotation. Reuse revokes entire family. Store `token_hash` (CHAR(64)) only. No raw token logging. Reused rotated token revokes entire session family.
- **Lý do cần quyết định (Why the decision is required):** No refresh tokens exist. Family rotation and reuse detection strategy must be decided before implementing session management.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Design alternative: Hashed, non-rotating refresh tokens. Rejected / unacceptable security practice: Storing raw refresh tokens.
- **Rủi ro (Risks):** Concurrent requests from the same client can cause race conditions resulting in false-positive token reuse and session revocation.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Refresh_Tokens (FK to User_Auth_Accounts). Indexes on session_id, user_auth_account_id. UNIQUE on token_hash.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/refresh performs family rotation. Reuse detection triggers family revocation.
- **Tác động Security (Security impact):** Detects reuse of an invalidated refresh token, which may indicate token compromise or a concurrent-client race. Family revocation limits damage.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: RefreshToken_Rotation_Updates_Token, RefreshToken_Reuse_Revokes_Session_Family, RefreshToken_Concurrent_Request_Succeeds.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Bảng Refresh_Tokens với UNIQUE(token_hash), indexes trên session_id và user_auth_account_id, và xử lý concurrent refresh có an toàn về mặt DB không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-006 — Permission catalog schema
- **Decision ID:** DEC-1B-006
- **Chủ đề (Topic):** Permission catalog schema
- **Chủ sở hữu quyết định (Decision owner):** DBA Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `permission_code` (VARCHAR(100)) as natural PK. Immutable/no rename. `data_scope` constrained to GLOBAL, COMPANY, ENTITY. Admin cannot invent codes.
- **Lý do cần quyết định (Why the decision is required):** permission-catalog.md lists codes but schema design (natural PK vs surrogate) must be decided before migration.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: `permission_code` field definition, all canonical permissions
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Surrogate `bigint` PK for permissions.
- **Rủi ro (Risks):** Natural primary keys make code renaming a migration concern. A VARCHAR key increases FK/index width compared with a surrogate key. GLOBAL/COMPANY/ENTITY ambiguity may cause incompatible schema design.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Permissions with `permission_code` VARCHAR(100) as PK. Role_Permissions, Department_Permissions junction tables.
- **Tác động API dự kiến (Proposed API impact if approved):** GET /api/v2/security/permissions returns permission list. PUT /api/v2/security/roles/{id}/permissions references permission_code.
- **Tác động Security (Security impact):** Immutable permission codes prevent confusion. Natural PK ensures code-database consistency.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: Permission code uniqueness and FK integrity tests.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. `permission_code` is immutable after release — no rename without mapped migration. 2. Approved `data_scope` values: GLOBAL, COMPANY, ENTITY — stakeholders must confirm whether ENTITY is included in Phase 1B. 3. Roles.role_code VARCHAR(50) NOT NULL UNIQUE must be explicitly approved.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** permission_code VARCHAR(100) có được chấp nhận làm khóa chính tự nhiên (natural PK)? Phê duyệt Roles.role_code VARCHAR(50) NOT NULL UNIQUE?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-007 — Role and Admin-Group scope
- **Decision ID:** DEC-1B-007
- **Chủ đề (Topic):** Role and Admin-Group scope
- **Chủ sở hữu quyết định (Decision owner):** BA/Product Owner
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `scope_type = GLOBAL | COMPANY` for Roles and Admin Groups. GLOBAL requires `company_id` IS NULL. COMPANY requires `company_id` IS NOT NULL. SUPER_ADMIN is explicit mapping (no bypass).
- **Lý do cần quyết định (Why the decision is required):** AUTH-002 references company context. Scope rules for Roles and Admin Groups must be decided to enforce multi-company authorization.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** AUTH-002 (Company role permissions add business permissions within the assigned company), AUTH-007 (COMPANY permission effective only with ACTIVE company assignment)
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: `data_scope` field (GLOBAL, COMPANY), business roles table
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** AUTH-02 (CASHIER role effective only for assigned company), AUTH-04 (Cross-company data denial)
- **Phương án thay thế (Alternatives):** Global-only roles without company scope.
- **Rủi ro (Risks):** Incorrect scope validation can cause cross-company privilege escalation. GLOBAL assignments have a wider blast radius. Conflicting Role and Permission scopes require deterministic validation.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** Column `scope_type` VARCHAR(30) CHECK (GLOBAL, COMPANY) in Roles and Admin_Groups. Proposed: Roles.role_code VARCHAR(50) NOT NULL UNIQUE, Admin_Groups.group_code VARCHAR(50) NOT NULL UNIQUE.
- **Tác động API dự kiến (Proposed API impact if approved):** All security assignment APIs validate scope_type against company_id presence.
- **Tác động Security (Security impact):** Scope enforcement reduces cross-company privilege escalation.
- **Tác động Test dự kiến (Proposed test impact if approved):** Unit: AdminGroup_Scope_Validates_Company_Id. API: ProtectedEndpoint_CrossCompanyDenial_Returns_403.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. GLOBAL requires `company_id` IS NULL. 2. COMPANY requires `company_id` IS NOT NULL. 3. Phase 1B supports only GLOBAL and COMPANY — ENTITY scope exclusion must be confirmed. 4. SUPER_ADMIN must be explicitly mapped, not a bypass.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Cấu trúc schema cho scope_type GLOBAL (company_id NULL) và COMPANY (company_id NOT NULL) với check constraints có tối ưu không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-009 — Admin group model
- **Decision ID:** DEC-1B-009
- **Chủ đề (Topic):** Admin group model
- **Chủ sở hữu quyết định (Decision owner):** BA/Product Owner
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `Admin_Groups`, `Admin_Group_Permissions`, `User_Admin_Group_Assignments`. `company_id` nullable (supports GLOBAL or COMPANY). No hardcoded SUPER_ADMIN bypass; mapped explicitly, enforces hard rules.
- **Lý do cần quyết định (Why the decision is required):** WFD-009 references ADMIN_GROUP as an approver source. The admin group model must be decided before schema creation.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** WFD-009 (ADMIN_GROUP is a supported approver source)
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: Admin groups table (ADMIN_SECURITY, ADMIN_CUSTOMER_DATA, etc.)
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Rejected / unacceptable security alternative: unrestricted SUPER_ADMIN bypass instead of mapped permissions.
- **Rủi ro (Risks):** Managing a separate Admin group hierarchy duplicates some assignment effort. Privilege escalation is possible if Admin group permissions are overly broad.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New tables: Admin_Groups, Admin_Group_Permissions, User_Admin_Group_Assignments. Temporal overlap constraints.
- **Tác động API dự kiến (Proposed API impact if approved):** Admin group CRUD and assignment APIs under /api/v2/security/admin-groups.
- **Tác động Security (Security impact):** Explicit mapping mitigates uncontrolled SUPER_ADMIN bypass of hard invariants.
- **Tác động Test dự kiến (Proposed test impact if approved):** Unit: AdminGroup_Scope_Validates_Company_Id.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Phê duyệt Admin_Groups.group_code VARCHAR(50) NOT NULL UNIQUE? Cấu trúc bảng Admin_Group_Permissions và User_Admin_Group_Assignments có hợp lý không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-011 — Permission cache failure
- **Decision ID:** DEC-1B-011
- **Chủ đề (Topic):** Permission cache failure
- **Chủ sở hữu quyết định (Decision owner):** Backend Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** DB `policy_version` read on every protected request. Cache key includes version. DB read failure must fail closed. Account, session, and company checks occur before cache use.
- **Lý do cần quyết định (Why the decision is required):** AUTH-012 requires permission cache invalidation. The failure mode (fail-open vs fail-closed) must be explicitly decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** AUTH-012 (Permission cache must be invalidated on relevant changes)
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** AUTH-06 (Permission cache refreshed/invalidated on changes)
- **Phương án thay thế (Alternatives):** Redis cache or fail-open.
- **Rủi ro (Risks):** Fail-closed behavior causes system-wide denial of service if the database policy table becomes inaccessible or heavily contented.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Authorization_Policy_State (singleton constraint, policy_version BIGINT).
- **Tác động API dự kiến (Proposed API impact if approved):** All protected endpoints check policy_version. Cache miss returns 403 or 500 (fail-closed).
- **Tác động Security (Security impact):** Fail-closed reduces unauthorized access during cache failures.
- **Tác động Test dự kiến (Proposed test impact if approved):** Unit: PolicyVersion_Read_Invalidates_Old_Cache.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Bảng Authorization_Policy_State dạng singleton (single row constraint) với policy_version BIGINT có ổn định không? Việc bypass cache truy cập DB trực tiếp khi cache lỗi có gây quá tải không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-014 — Temporal locking mechanism
- **Decision ID:** DEC-1B-014
- **Chủ đề (Topic):** Temporal locking mechanism
- **Chủ sở hữu quyết định (Decision owner):** DBA Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Primary: SERIALIZABLE tx; query natural-key range with UPDLOCK and HOLDLOCK; validate half-open overlap; retry only 1205. Defense: SQL overlap trigger & filtered unique index.
- **Lý do cần quyết định (Why the decision is required):** Deterministic temporal overlap prevention is needed for role/permission/admin-group assignments. The isolation level and locking strategy must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** sp_getapplock or READ COMMITTED with optimistic concurrency.
- **Rủi ro (Risks):** SERIALIZABLE isolation significantly increases transaction deadlocks. UPDLOCK/HOLDLOCK reduces concurrency on the assignment tables.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** Overlap triggers (AFTER INSERT, UPDATE) and filtered unique indexes on User_Role_Company, User_Individual_Permissions, User_Admin_Group_Assignments.
- **Tác động API dự kiến (Proposed API impact if approved):** Role/permission/admin-group assignment APIs return 409 on temporal overlap.
- **Tác động Security (Security impact):** Mitigates concurrent duplicate role/permission assignments via SERIALIZABLE isolation.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: UserRoleCompany_Temporal_Overlap_Fails, UserIndividualPerms_Temporal_Overlap_Fails, UserAdminGroup_Temporal_Overlap_Fails.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. DBA must choose temporal state model: A (assignment_status + effective dates) or B (effective dates only). 2. SERIALIZABLE + UPDLOCK/HOLDLOCK explicitly approved. 3. SQL error 1205 is the only retried error. 4. Overlap triggers and filtered unique indexes explicitly approved.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** SERIALIZABLE + UPDLOCK/HOLDLOCK có được phê duyệt không? SQL error 1205 có phải là lỗi duy nhất được retry? Overlap triggers và filtered unique indexes có được phê duyệt không? Chọn mô hình temporal state: A (assignment_status + effective dates) hay B (chỉ effective dates)?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-015 — Audit database controls
- **Decision ID:** DEC-1B-015
- **Chủ đề (Topic):** Audit database controls
- **Chủ sở hữu quyết định (Decision owner):** DBA Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Runtime DB principal has INSERT/SELECT only. No UPDATE/DELETE/TRUNCATE. No cascade delete. Stable DB error. EF/Dapper/SQL immutability tested.
- **Lý do cần quyết định (Why the decision is required):** GOV-007 requires immutable audit. The database-level enforcement mechanism must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** GOV-007 (All material changes require immutable audit records), GOV-008 (No user may erase audit history), SEC-001 (Audit and Approval_Actions are append-only)
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: AUDIT_VIEW
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** SEC-02 (Business users cannot update/delete audit or Approval_Actions)
- **Phương án thay thế (Alternatives):** EF interceptor only (no database-level enforcement).
- **Rủi ro (Risks):** Runtime-principal permissions do not protect against authorized sysadmin changes. Trigger and permission hardening increase deployment and operational complexity. Incorrect DBA grants can bypass the intended runtime boundary.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Security_Audit_Events. Runtime principal limited to INSERT/SELECT. Trigger blocks UPDATE/DELETE.
- **Tác động API dự kiến (Proposed API impact if approved):** GET /api/v2/security/audit returns audit events. No mutation API exists.
- **Tác động Security (Security impact):** Database permissions and triggers enforce append-only behavior for the application runtime principal and ordinary database access paths. They are not a cryptographic guarantee and do not prevent an authorized sysadmin from changing database controls.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: SecurityAuditEvents_Is_AppendOnly, AuditDatabase_Blocks_Update_Delete_Truncate, AuditData_Contains_No_Passwords_Or_Tokens.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Runtime principal restricted to INSERT/SELECT only. 2. UPDATE, DELETE and TRUNCATE explicitly blocked. 3. No cascade delete on audit tables. 4. No password, token, signing key or secret stored in audit data.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Runtime audit principal chỉ INSERT/SELECT có được phê duyệt không? Cấm UPDATE/DELETE/TRUNCATE trên bảng audit có ổn không? Tất cả cột Security_Audit_Events có được chấp nhận không? U0003 phải làm gì với dữ liệu audit/bootstrap?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-017 — Security audit retention/archive
- **Decision ID:** DEC-1B-017
- **Chủ đề (Topic):** Security audit retention/archive
- **Chủ sở hữu quyết định (Decision owner):** DBA Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** DEFERRED — NON-BLOCKING
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Current audit records remain in the database. No purge/archive in Phase 1B. Long-term retention/archive is a separate compliance decision. Phase 1B schema strictly preserves immutable event identity.
- **Lý do cần quyết định (Why the decision is required):** Database size will grow with audit data. Whether to build purge/archive in Phase 1B or defer must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** GOV-007, GOV-008, SEC-001 (Audit immutability rules)
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: AUDIT_VIEW
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** SEC-02 (Business users cannot update/delete audit)
- **Phương án thay thế (Alternatives):** 1-year purge/archive cycle.
- **Rủi ro (Risks):** Audit tables will grow unbounded, potentially degrading database performance and increasing backup size until an archiving strategy is implemented.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No additional schema impact. Preserves existing Security_Audit_Events immutability.
- **Tác động API dự kiến (Proposed API impact if approved):** No API impact. No purge/archive endpoint in Phase 1B.
- **Tác động Security (Security impact):** No immediate security impact. Immutability preserved. Retention deferred.
- **Tác động Test dự kiến (Proposed test impact if approved):** No additional tests. Existing audit immutability tests remain.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Phase 1B implements no purge/archive feature. 2. Existing audit records remain stored. 3. Audit immutability remains enforced. 4. Long-term retention/archive is handled in a later compliance decision.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Không xây purge/archive trong Phase 1B, giữ nguyên dữ liệu audit có được chấp nhận là DEFERRED — NON-BLOCKING không? Ảnh hưởng dung lượng database dài hạn?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

## 8. Điều kiện và câu hỏi mở (Conditions and open questions)

- **DEC-1B-001:** Cấu trúc bảng User_Auth_Accounts mới (khóa ngoại trỏ đến Users) và ràng buộc UNIQUE(provider_type, provider_subject) có hợp lý và tối ưu không?
- **DEC-1B-002:** Bảng Password_History với index trên (user_auth_account_id, created_at DESC) có ảnh hưởng hiệu năng không? Các SQL types và lengths có chấp nhận được không?
- **DEC-1B-004:** Việc cập nhật liên tục failed_attempt_count và lockout_end trong User_Auth_Accounts có gây deadlock hoặc ảnh hưởng hiệu năng không?
- **DEC-1B-005:** Bảng Refresh_Tokens với UNIQUE(token_hash), indexes trên session_id và user_auth_account_id, và xử lý concurrent refresh có an toàn về mặt DB không?
- **DEC-1B-006:** permission_code VARCHAR(100) có được chấp nhận làm khóa chính tự nhiên (natural PK)? Phê duyệt Roles.role_code VARCHAR(50) NOT NULL UNIQUE?
- **DEC-1B-007:** Cấu trúc schema cho scope_type GLOBAL (company_id NULL) và COMPANY (company_id NOT NULL) với check constraints có tối ưu không?
- **DEC-1B-009:** Phê duyệt Admin_Groups.group_code VARCHAR(50) NOT NULL UNIQUE? Cấu trúc bảng Admin_Group_Permissions và User_Admin_Group_Assignments có hợp lý không?
- **DEC-1B-011:** Bảng Authorization_Policy_State dạng singleton (single row constraint) với policy_version BIGINT có ổn định không? Việc bypass cache truy cập DB trực tiếp khi cache lỗi có gây quá tải không?
- **DEC-1B-014:** SERIALIZABLE + UPDLOCK/HOLDLOCK có được phê duyệt không? SQL error 1205 có phải là lỗi duy nhất được retry? Overlap triggers và filtered unique indexes có được phê duyệt không? Chọn mô hình temporal state: A (assignment_status + effective dates) hay B (chỉ effective dates)?
- **DEC-1B-015:** Runtime audit principal chỉ INSERT/SELECT có được phê duyệt không? Cấm UPDATE/DELETE/TRUNCATE trên bảng audit có ổn không? Tất cả cột Security_Audit_Events có được chấp nhận không? U0003 phải làm gì với dữ liệu audit/bootstrap?
- **DEC-1B-017:** Không xây purge/archive trong Phase 1B, giữ nguyên dữ liệu audit có được chấp nhận là DEFERRED — NON-BLOCKING không? Ảnh hưởng dung lượng database dài hạn?

## 9. Khai báo của người phê duyệt (Reviewer declaration)

- **Reviewer name:**
- **Reviewer role:**
- **Review date:**
- **Approval reference:**
- **Overall result:**
- **Outstanding conditions:**
- **Signature or confirmation method:**

"Tôi xác nhận phản hồi của mình chỉ áp dụng cho các quyết định và phạm vi thẩm quyền được liệt kê. Mục không trả lời hoặc sự im lặng không cấu thành phê duyệt."

## 10. Tổng kết đánh giá cuối cùng (Final review summary)
*(Để trống cho đến khi hoàn thành)*
