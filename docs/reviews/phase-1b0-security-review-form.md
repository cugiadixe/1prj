# Biểu mẫu Đánh giá Phase 1B.0 — Security / Infrastructure Lead

## 1. Mục đích tài liệu (Document purpose)
Thu thập các phê duyệt chính thức từ **Security / Infrastructure Lead** đối với các thiết kế bảo mật và kiến trúc của Phase 1B.0.

## 2. Trạng thái dự án hiện tại (Current project status)
- Phase 1A.2: **CLOSED** (Commit: `3ea30fb`, Tag: `phase-1a2-application-api-v1.0`)
- Phase 1B.0 documentation: **COMMITTED** (`37b2d60`)
- Stakeholder decisions: tất cả **OPEN**
- Phase 1B.1: **NOT AUTHORIZED**

## 3. Vai trò người phê duyệt (Reviewer role): Security / Infrastructure Lead

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

## 5. Các quyết định dành cho người phê duyệt này (20 quyết định)
- DEC-1B-001 — Login identifier
- DEC-1B-002 — Password policy
- DEC-1B-003 — Token lifetimes
- DEC-1B-004 — Lockout values
- DEC-1B-005 — Refresh token schema & family
- DEC-1B-006 — Permission catalog schema
- DEC-1B-007 — Role and Admin-Group scope
- DEC-1B-009 — Admin group model
- DEC-1B-010 — First-admin provisioning
- DEC-1B-011 — Permission cache failure
- DEC-1B-012 — Current-company missing-header behavior
- DEC-1B-013 — Employment-status values
- DEC-1B-014 — Temporal locking mechanism
- DEC-1B-015 — Audit database controls
- DEC-1B-016 — Exact Organization and Security permission codes
- DEC-1B-017 — Security audit retention/archive
- DEC-1B-018 — Client deployment topology and cookie SameSite behavior
- DEC-1B-019 — Signing-key provider and rotation
- DEC-1B-020 — Account-locked HTTP status
- DEC-1B-021 — Audit-view permission reuse versus SECURITY_AUDIT_VIEW

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

## 7. Biểu mẫu phản hồi từng quyết định (Decision response form — 20 sections)

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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Mô hình tách biệt User_Auth_Accounts có đáp ứng tiêu chuẩn phân tách dữ liệu bảo mật (credential isolation) không?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Độ dài mật khẩu tối thiểu 8, tối đa 64, lịch sử 5, mật khẩu tạm 24h có đáp ứng yêu cầu an toàn thông tin không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-003 — Token lifetimes
- **Decision ID:** DEC-1B-003
- **Chủ đề (Topic):** Token lifetimes
- **Chủ sở hữu quyết định (Decision owner):** Security Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Access: 15 minutes. Refresh: 7 days. Clock skew: 0s.
- **Lý do cần quyết định (Why the decision is required):** No JWT configuration exists. Token lifetimes affect both security posture and user experience.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Longer access tokens (e.g. 24h).
- **Rủi ro (Risks):** Short access tokens increase refresh frequency, adding load to the authentication service. Zero clock skew removes JWT time tolerance for nbf/exp validation. It does not prevent replay and requires reliable clock synchronization.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. Configuration values only.
- **Tác động API dự kiến (Proposed API impact if approved):** Token endpoint returns access token with 15m expiry. Refresh token cookie with 7d expiry.
- **Tác động Security (Security impact):** Short access token limits damage window. Zero clock skew removes JWT time tolerance for nbf/exp validation. It does not prevent replay and requires reliable clock synchronization.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Token endpoint tests verify expiry times. Unit: JWT clock skew validation.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Access token 15 phút, Refresh token 7 ngày, Clock skew 0s đã tối ưu về bảo mật chưa?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Ngưỡng khóa 5 lần sai trong 15 phút có đủ chống brute-force không? Có rủi ro DoS tài khoản không?
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Hành vi xoay vòng token family và thu hồi toàn bộ khi phát hiện tái sử dụng có chính xác và an toàn không? Kết quả xử lý concurrent refresh (làm mới đồng thời) phải là gì?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Các mã quyền dạng chuỗi cố định có rõ ràng cho việc audit và truy vết không?
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Phân định quyền theo scope GLOBAL/COMPANY có đảm bảo ranh giới an toàn đa công ty (tenant boundary) không?
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Tính toán quyền hiệu lực từ Admin Group có bảo đảm an toàn khi thu hồi quyền không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-010 — First-admin provisioning
- **Decision ID:** DEC-1B-010
- **Chủ đề (Topic):** First-admin provisioning
- **Chủ sở hữu quyết định (Decision owner):** Infrastructure Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER — only when completed by Infrastructure Lead; otherwise REQUIRED APPROVER.
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Separate controlled bootstrap command. Reads from approved secret provider/protected input. NEVER prints password/token/secret. Sets `must_change_password=1`. Immutable audit. One-time marker; rejects repeated attempts. Does not run during API startup.
- **Lý do cần quyết định (Why the decision is required):** A fresh database has no admin user. The provisioning method must be decided to avoid insecure default accounts.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: ADMIN_SECURITY group boundary
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Rejected / unacceptable security alternative: Print the initial password, token or secret to console or logs.
- **Rủi ro (Risks):** CLI-only bootstrap requires infrastructure access, complicating initial setup. Loss of bootstrap credentials before secondary admin creation requires database intervention.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** New table: Security_Bootstrap_State (singleton constraint).
- **Tác động API dự kiến (Proposed API impact if approved):** No API endpoint. Separate CLI/bootstrap command only.
- **Tác động Security (Security impact):** Mitigates insecure default admin credentials. One-time marker reduces repeated bootstrap risk.
- **Tác động Test dự kiến (Proposed test impact if approved):** Integration: BootstrapCommand_Runs_Once_Only.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Người vận hành bootstrap và phương thức phân phối bí mật (secret-delivery method) có an toàn không? Có ngăn chặn bootstrap lặp lại không?
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Chính sách Fail-closed khi cache phân quyền lỗi có đủ an toàn chưa?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-012 — Current-company missing-header behavior
- **Decision ID:** DEC-1B-012
- **Chủ đề (Topic):** Current-company missing-header behavior
- **Chủ sở hữu quyết định (Decision owner):** Backend Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `X-Company-Id` required for COMPANY endpoints. Missing returns `AUTH_CURRENT_COMPANY_REQUIRED` (400 or 403, explicitly decide). JWT does NOT authorize company. `switch-company` removed.
- **Lý do cần quyết định (Why the decision is required):** AUTH-007 requires an active company assignment. The behavior when X-Company-Id is missing must be decided to prevent silent data scope errors.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** AUTH-007 (COMPANY permission effective only with ACTIVE company assignment), AUTH-009 (Every endpoint must re-check permission and data scope at the server)
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** AUTH-04 (Cross-company data denial)
- **Phương án thay thế (Alternatives):** Embed company in JWT.
- **Rủi ro (Risks):** Strict header requirements break simple API clients and require frontend interceptor modifications. Returns ambiguous errors if not clearly documented.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. API-level header validation only.
- **Tác động API dự kiến (Proposed API impact if approved):** COMPANY endpoints return AUTH_CURRENT_COMPANY_REQUIRED (400 or 403) when X-Company-Id missing.
- **Tác động Security (Security impact):** Reduces silent writes to wrong company. Header validation enforces explicit company context.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: ProtectedEndpoint_CrossCompanyDenial_Returns_403, missing-header tests.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Missing X-Company-Id returns one approved HTTP status (400 or 403). 2. COMPANY write operations must never silently fall back to primary company. 3. JWT does NOT authorize company access.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Kiểm tra X-Company-Id có đủ ngăn ngừa leo thang quyền giữa các tenant không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-013 — Employment-status values
- **Decision ID:** DEC-1B-013
- **Chủ đề (Topic):** Employment-status values
- **Chủ sở hữu quyết định (Decision owner):** BA/Product Owner
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Auth requires `Users.account_status` = ACTIVE, AND `Users.employment_status` IN ('ACTIVE', 'PROBATION'). Denies SUSPENDED, RESIGNED, TERMINATED, RETIRED, INACTIVE.
- **Lý do cần quyết định (Why the decision is required):** Users table has both account_status and employment_status. Which employment statuses permit authentication must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Ignore employment status for authentication.
- **Rủi ro (Risks):** Status synchronisation delays between HR systems and the security context can leave active sessions open for terminated employees unless explicitly revoked.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. Application-level status check only.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/login checks employment_status. Denied statuses receive AUTH_INVALID_CREDENTIALS.
- **Tác động Security (Security impact):** Mitigates access by terminated/suspended employees.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Login tests for each employment status value.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Việc liên kết chặt trạng thái nhân sự với quyền đăng nhập và session revocation có hoạt động tức thời không?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** SERIALIZABLE isolation với UPDLOCK/HOLDLOCK có ngăn chặn race conditions đủ an toàn không?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Những trường nào bị cấm lưu trữ trong audit (secrets, password hashes, tokens)? Immutable audit enforcement có đạt chuẩn không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-016 — Exact Organization and Security permission codes
- **Decision ID:** DEC-1B-016
- **Chủ đề (Topic):** Exact Organization and Security permission codes
- **Chủ sở hữu quyết định (Decision owner):** BA/Product Owner
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Adopt explicit ORGANIZATION_COMPANY_VIEW, ORGANIZATION_COMPANY_MANAGE, ORGANIZATION_DEPARTMENT_VIEW, ORGANIZATION_DEPARTMENT_MANAGE, SECURITY_USER_VIEW, SECURITY_USER_MANAGE, SECURITY_ASSIGNMENT_MANAGE, SECURITY_ROLE_VIEW, SECURITY_ROLE_MANAGE, SECURITY_PERMISSION_VIEW, SECURITY_PERMISSION_MANAGE, SECURITY_ACCOUNT_MANAGE, SECURITY_ADMIN_GROUP_VIEW, SECURITY_ADMIN_GROUP_MANAGE, SECURITY_AUDIT_VIEW.
- **Lý do cần quyết định (Why the decision is required):** Phase 1A.2 endpoints lack authorization. Exact permission codes must be decided before implementing authorization checks.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** AUTH-001, AUTH-002, AUTH-003, AUTH-004, AUTH-005, AUTH-006, AUTH-007, AUTH-008, AUTH-009 (Authorization rules reference permission codes)
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: all canonical permission codes; proposed new ORGANIZATION_* and SECURITY_* codes
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** SEC-01 (No endpoint relies only on UI visibility for authorization)
- **Phương án thay thế (Alternatives):** Broad `ADMIN` permission code.
- **Rủi ro (Risks):** A large number of fine-grained permissions increases the complexity of role design and assignment. Frontend must implement complex conditional rendering.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** Seed data in Permissions table for all proposed codes.
- **Tác động API dự kiến (Proposed API impact if approved):** All organization and security endpoints require specific permission codes for authorization.
- **Tác động Security (Security impact):** Fine-grained permission codes enable precise access control.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Authorization checks for each permission code on organization and security endpoints.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** None documented beyond compatible approval from all required approvers.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Cấp phép dựa trên các mã quyền tĩnh ORGANIZATION_* và SECURITY_* có đủ chi tiết và an toàn không?
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
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
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
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Việc hoãn purge/archive nhưng giữ nguyên immutability có ảnh hưởng tới compliance không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-018 — Client deployment topology and cookie SameSite behavior
- **Decision ID:** DEC-1B-018
- **Chủ đề (Topic):** Client deployment topology and cookie SameSite behavior
- **Chủ sở hữu quyết định (Decision owner):** Security Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Access token in memory. Refresh token in `HttpOnly`, `Secure`, `SameSite` cookie, based on approved deployment topology.
- **Lý do cần quyết định (Why the decision is required):** Frontend needs token storage. The client deployment topology and cookie behavior must be decided to balance security and usability.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** SEC-005 (Audit/snapshot must not contain passwords, tokens, file bytes or permanent signed URLs)
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** SEC-03 (Sensitive data masked/restricted by permission)
- **Phương án thay thế (Alternatives):** localStorage (vulnerable to XSS).
- **Rủi ro (Risks):** Incorrect SameSite selection can break login or refresh flows. Cross-site deployment requires additional CSRF controls. XSS can still initiate authenticated actions even when JavaScript cannot directly read the HttpOnly cookie. Cookie behavior depends on browser and deployment topology.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. Client-side storage only.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/login and POST /api/v2/auth/refresh set HttpOnly/Secure/SameSite cookie.
- **Tác động Security (Security impact):** HttpOnly prevents JavaScript from directly reading the refresh-token cookie, but it does not eliminate other effects of XSS. SameSite can reduce CSRF exposure. The selected deployment topology and cookie mode still require explicit, compatible CSRF controls.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Cookie attribute tests (HttpOnly, Secure, SameSite).
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Approved deployment topology (same-site vs cross-site) determines SameSite mode. 2. CSRF controls must be compatible with chosen SameSite mode. 3. Refresh token stored in HttpOnly/Secure cookie only.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** SameSite mode cho deployment topology được phê duyệt và các biện pháp CSRF tương ứng đã đầy đủ chưa?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-019 — Signing-key provider and rotation
- **Decision ID:** DEC-1B-019
- **Chủ đề (Topic):** Signing-key provider and rotation
- **Chủ sở hữu quyết định (Decision owner):** Security Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** OWNER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Dev: User secrets. Prod/Staging: Azure Key Vault/injected secret. Min 256-bit (HMAC-SHA256). Uses `kid` for rotation. 24h old-key window. Startup fails if missing/unsafe. No committed keys.
- **Lý do cần quyết định (Why the decision is required):** Signing keys must be secure. The key source, rotation procedure and validation window must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** SEC-005 (No secrets in audit/snapshot data)
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Rejected / unacceptable security alternative: hardcoded production secrets or committed signing keys.
- **Rủi ro (Risks):** External secret providers introduce an operational dependency. Outages or incorrect rotation can invalidate all active sessions system-wide.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. Configuration/infrastructure only.
- **Tác động API dự kiến (Proposed API impact if approved):** JWT signing uses kid header. Token validation accepts old key within 24h window.
- **Tác động Security (Security impact):** External secret provider mitigates key exposure in source control. Rotation limits key compromise window.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Invalid signature rejection. Integration: Startup failure on missing key.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Signing algorithm and minimum key strength explicitly defined. 2. Secret source specified for Development, Staging and Production. 3. kid and key-rotation procedure documented. 4. Previous-key validation period explicitly set (proposed: 24h). 5. Startup must fail if key is missing or unsafe.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Nguồn bí mật (Secret source) cho Dev/Staging/Production, quy trình xoay vòng kid, khoảng thời gian xác thực khóa cũ (24h), thuật toán ký và độ mạnh khóa tối thiểu (256-bit HMAC-SHA256) có đáp ứng yêu cầu không?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-020 — Account-locked HTTP status
- **Decision ID:** DEC-1B-020
- **Chủ đề (Topic):** Account-locked HTTP status
- **Chủ sở hữu quyết định (Decision owner):** Backend Lead
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** `AUTH_ACCOUNT_LOCKED` returns 403 or 423 (pending Security approval). Option A: HTTP 403. Option B: HTTP 423.
- **Lý do cần quyết định (Why the decision is required):** Frontend needs to distinguish locked accounts from invalid credentials. The HTTP status code must be decided.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** N/A
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** 400 Bad Request (generic).
- **Rủi ro (Risks):** Returning a distinct status code for locked accounts allows attackers to enumerate active usernames through differential responses.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** No schema impact. Error map configuration only.
- **Tác động API dự kiến (Proposed API impact if approved):** POST /api/v2/auth/login returns either 403 or 423 for AUTH_ACCOUNT_LOCKED.
- **Tác động Security (Security impact):** Specific HTTP status enables UX but risks account enumeration.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Test status code returned when account is locked.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Choose exactly one HTTP status: 403 or 423. 2. Decision must consider enumeration risk vs UX clarity.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Nên sử dụng HTTP 403 hay HTTP 423 cho AUTH_ACCOUNT_LOCKED? Rủi ro enumeration là gì?
- **Phản hồi của người đánh giá (Reviewer response):**
  [ CHỌN MỘT: APPROVED | APPROVED WITH CONDITIONS | REJECTED | DEFERRED — NON-BLOCKING | REMAIN OPEN ]
- **Điều kiện của người đánh giá (Reviewer conditions):**
- **Nhận xét của người đánh giá (Reviewer comments):**
- **Tên người đánh giá (Reviewer name):**
- **Vai trò người đánh giá (Reviewer role):**
- **Ngày đánh giá (Review date):**
- **Tài liệu tham chiếu phê duyệt (Approval reference):**
- **Chữ ký hoặc phương thức xác nhận (Signature or confirmation method):**

### DEC-1B-021 — Audit-view permission reuse versus SECURITY_AUDIT_VIEW
- **Decision ID:** DEC-1B-021
- **Chủ đề (Topic):** Audit-view permission reuse versus SECURITY_AUDIT_VIEW
- **Chủ sở hữu quyết định (Decision owner):** BA/Product Owner
- **Thẩm quyền của người đánh giá này (This reviewer's authority):** REQUIRED APPROVER
- **Phân loại chặn (Blocking classification):** BLOCKING FOR PHASE 1B.1
- **Trạng thái hiện tại (Current status):** OPEN
- **Đề xuất hiện tại (Current proposal):** Question: reuse existing AUDIT_VIEW or retain SECURITY_AUDIT_VIEW with a clearly different administration boundary?
- **Lý do cần quyết định (Why the decision is required):** AUDIT_VIEW exists in permission-catalog.md. Whether to reuse it or create a separate SECURITY_AUDIT_VIEW must be decided to avoid duplicate permission meanings.
- **Tham chiếu quy tắc nghiệp vụ (Canonical business-rule references):** N/A — no direct canonical business-rule reference identified
- **Tham chiếu danh mục quyền (Permission-catalog references):** permission-catalog.md: AUDIT_VIEW (existing), proposed SECURITY_AUDIT_VIEW
- **Tham chiếu tiêu chí chấp nhận (Acceptance-criteria references):** N/A — no direct canonical acceptance criterion identified
- **Phương án thay thế (Alternatives):** Allow all admins to view all audit (no boundary).
- **Rủi ro (Risks):** Creating distinct security audit permissions duplicates the assignment burden for administrators who genuinely need cross-system audit visibility.
- **Tác động Schema dự kiến (Proposed schema impact if approved):** Determines whether SECURITY_AUDIT_VIEW is seeded as a separate permission code.
- **Tác động API dự kiến (Proposed API impact if approved):** GET /api/v2/security/audit requires either AUDIT_VIEW or SECURITY_AUDIT_VIEW depending on decision.
- **Tác động Security (Security impact):** Separate boundaries may prevent audit data leakage between security and business audit.
- **Tác động Test dự kiến (Proposed test impact if approved):** API: Test audit endpoint authorization with chosen permission code.
- **Điều kiện phê duyệt bắt buộc (Mandatory approval conditions):** 1. Choose: reuse existing AUDIT_VIEW from permission-catalog.md, or create distinct SECURITY_AUDIT_VIEW. 2. If retained, SECURITY_AUDIT_VIEW must have a clearly different administration boundary from AUDIT_VIEW.
- **Câu hỏi phê duyệt chính xác (Exact approval question):** Phân chia AUDIT_VIEW và SECURITY_AUDIT_VIEW — nên tái sử dụng AUDIT_VIEW hay giữ SECURITY_AUDIT_VIEW riêng biệt? Ranh giới quản trị khác nhau thế nào?
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

- **DEC-1B-001:** Mô hình tách biệt User_Auth_Accounts có đáp ứng tiêu chuẩn phân tách dữ liệu bảo mật (credential isolation) không?
- **DEC-1B-002:** Độ dài mật khẩu tối thiểu 8, tối đa 64, lịch sử 5, mật khẩu tạm 24h có đáp ứng yêu cầu an toàn thông tin không?
- **DEC-1B-003:** Access token 15 phút, Refresh token 7 ngày, Clock skew 0s đã tối ưu về bảo mật chưa?
- **DEC-1B-004:** Ngưỡng khóa 5 lần sai trong 15 phút có đủ chống brute-force không? Có rủi ro DoS tài khoản không?
- **DEC-1B-005:** Hành vi xoay vòng token family và thu hồi toàn bộ khi phát hiện tái sử dụng có chính xác và an toàn không? Kết quả xử lý concurrent refresh (làm mới đồng thời) phải là gì?
- **DEC-1B-006:** Các mã quyền dạng chuỗi cố định có rõ ràng cho việc audit và truy vết không?
- **DEC-1B-007:** Phân định quyền theo scope GLOBAL/COMPANY có đảm bảo ranh giới an toàn đa công ty (tenant boundary) không?
- **DEC-1B-009:** Tính toán quyền hiệu lực từ Admin Group có bảo đảm an toàn khi thu hồi quyền không?
- **DEC-1B-010:** Người vận hành bootstrap và phương thức phân phối bí mật (secret-delivery method) có an toàn không? Có ngăn chặn bootstrap lặp lại không?
- **DEC-1B-011:** Chính sách Fail-closed khi cache phân quyền lỗi có đủ an toàn chưa?
- **DEC-1B-012:** Kiểm tra X-Company-Id có đủ ngăn ngừa leo thang quyền giữa các tenant không?
- **DEC-1B-013:** Việc liên kết chặt trạng thái nhân sự với quyền đăng nhập và session revocation có hoạt động tức thời không?
- **DEC-1B-014:** SERIALIZABLE isolation với UPDLOCK/HOLDLOCK có ngăn chặn race conditions đủ an toàn không?
- **DEC-1B-015:** Những trường nào bị cấm lưu trữ trong audit (secrets, password hashes, tokens)? Immutable audit enforcement có đạt chuẩn không?
- **DEC-1B-016:** Cấp phép dựa trên các mã quyền tĩnh ORGANIZATION_* và SECURITY_* có đủ chi tiết và an toàn không?
- **DEC-1B-017:** Việc hoãn purge/archive nhưng giữ nguyên immutability có ảnh hưởng tới compliance không?
- **DEC-1B-018:** SameSite mode cho deployment topology được phê duyệt và các biện pháp CSRF tương ứng đã đầy đủ chưa?
- **DEC-1B-019:** Nguồn bí mật (Secret source) cho Dev/Staging/Production, quy trình xoay vòng kid, khoảng thời gian xác thực khóa cũ (24h), thuật toán ký và độ mạnh khóa tối thiểu (256-bit HMAC-SHA256) có đáp ứng yêu cầu không?
- **DEC-1B-020:** Nên sử dụng HTTP 403 hay HTTP 423 cho AUTH_ACCOUNT_LOCKED? Rủi ro enumeration là gì?
- **DEC-1B-021:** Phân chia AUDIT_VIEW và SECURITY_AUDIT_VIEW — nên tái sử dụng AUDIT_VIEW hay giữ SECURITY_AUDIT_VIEW riêng biệt? Ranh giới quản trị khác nhau thế nào?

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
