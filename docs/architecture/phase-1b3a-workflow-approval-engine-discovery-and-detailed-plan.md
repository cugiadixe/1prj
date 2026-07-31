# Phase 1B.3-A Workflow/Approval Engine Discovery and Detailed Plan

**Status:**
PROPOSED — AWAITING PROJECT OWNER PLAN REVIEW

**Baseline:**
ffae4a919f23ec7d13980cf7ae11351c54c27536

**Branch:**
feature/phase-1-organization

**Date:**
2026-07-31

---

## 1. Authorization

| Reference | Commit |
|-----------|--------|
| Phase 1B.3 selection acceptance | ffae4a919f23ec7d13980cf7ae11351c54c27536 |
| Phase 1B.3 next work selection review | 44e41e000a0fba0115e6aa4dbeb10add11b30a39 |
| Customer first slice acceptance | 2f4c059dd7f5f91aa14f6f5560fc360808049668 |

This document is discovery and detailed planning only. No implementation is authorized.

---

## 2. Confirmed current state

- Security Administration complete.
- Customer first slice complete (B1 backend + B2 frontend).
- No workflow runtime exists in source code.
- No approval UI exists in source code.
- No workflow database tables exist.
- No workflow controllers, services, or domain entities exist.
- No workflow migrations exist.
- The workflow/approval engine is entirely greenfield.
- Next migration version will be V0006.
- No implementation is authorized by this plan document.

---

## 3. Source documents reviewed

| Document | Finding |
|----------|---------|
| business-rules.md v1.1 | WFD-001–WFD-012 (design-time), APR-001–APR-011 (runtime), REM-001–REM-005 (SLA/reminders), DEL-001–DEL-008 (delegation), GOV-001–GOV-008 (governance), SEC-001–SEC-008 (audit) |
| permission-catalog.md v1.1 | 6 workflow permissions already cataloged: WORKFLOW_VIEW, WORKFLOW_CONFIG_MANAGE, WORKFLOW_PUBLISH, WORKFLOW_BIND_PROCESS, WORKFLOW_REASSIGN_PENDING, WORKFLOW_AUDIT_VIEW. Plus DELEGATION_CREATE, DELEGATION_ACTIVATE |
| acceptance-criteria.md v1.1 | WFC-01–WFC-16 (workflow config), APR-01–APR-06 (approval runtime), DEL-01–DEL-06 (delegation) |
| AGENTS.md | Workflow invariants defined at lines 85–96 |
| project-readiness-review.md | Workflow requirements acknowledged; technical decisions documented |
| phase-1b0-open-decisions.md | 20 security decisions approved; no workflow-specific decisions yet |
| PTKD-ERP-Master-Context.md | **Not found** — documented as gap |

---

## 4. Confirmed workflow-related business concerns

Source-supported (from business-rules.md, acceptance-criteria.md, permission-catalog.md):

1. **Dynamic approval flows configurable by admin** — GOV-001, GOV-002, WFD-001–WFD-012, WFC-01–WFC-16.
2. **Ability to create new approval flows and assign to business processes** — WFD-002, WFD-005, WFC-01.
3. **In-progress behavior when configuration changes** — GOV-003, GOV-004, GOV-005, APR-006, APR-007, WFC-10–WFC-12.
4. **Customer workflow deferred from first slice** — CUS-002 requires CREATE_CUSTOMER and CUSTOMER_MASTER_CHANGE request paths.
5. **Service sale approval** — APR-02 references SERVICE_PRICE_OVERRIDE. Permission catalog includes SERVICE_PRICE_OVERRIDE_REQUEST and SERVICE_PRICE_OVERRIDE_APPROVE.
6. **Card reprint approval** — Permission catalog includes CARD_REPRINT_APPROVE (delegable). Supported by existing documentation.
7. **Change owner approval** — Permission catalog includes CHANGE_OWNER_APPROVE (delegable). Supported by existing documentation.
8. **Customer merge** — CUS-007, CUSTOMER_MERGE_DUPLICATE permission. Merge may require approval; not confirmed as mandatory workflow. Mark as candidate.
9. **Delegation** — DEL-001–DEL-008, DELEGATION_CREATE, DELEGATION_ACTIVATE. Tightly coupled to approval steps.
10. **SLA/reminders** — REM-001–REM-005. Part of approval step lifecycle.

Not yet formally confirmed by Project Owner (candidate only):
- Money-related approval flows beyond payment correction (no explicit business rule references additional payment approval beyond PAY-001 normal cashier path).

---

## 5. Business rule coverage matrix

### 5.1 Design-time rules (WFD)

| Rule ID | Summary | First implementation? |
|---------|---------|:---:|
| WFD-001 | SEQUENTIAL only in v1.1 | Yes |
| WFD-002 | Admin creates workflow, DRAFT version, steps, approver rules, conditions, SLA, bindings | Yes |
| WFD-003 | No SQL/JS; DEV-published fields/operators only | Yes |
| WFD-004 | DRAFT editable; PUBLISHED/ACTIVE immutable | Yes |
| WFD-005 | Scope GLOBAL or COMPANY; COMPANY overrides GLOBAL | Yes |
| WFD-006 | Reject overlapping active bindings | Yes |
| WFD-007 | No valid binding blocks submission + notifies | Yes |
| WFD-008 | Resolve all steps + at least one assignee before creating request | Yes |
| WFD-009 | 8 approver sources | Yes (subset — see DEC-1B3A-04) |
| WFD-010 | Multi-assignee: one action closes step | Yes |
| WFD-011 | Requester removal must not leave empty step | Yes |
| WFD-012 | Snapshot + hash stored on request | Yes |

### 5.2 Runtime rules (APR)

| Rule ID | Summary | First implementation? |
|---------|---------|:---:|
| APR-001 | Requester cannot act on own request steps | Yes |
| APR-002 | One PENDING step at a time | Yes |
| APR-003 | Approve activates next step atomically | Yes |
| APR-004 | Concurrent actions: first wins, others conflict | Yes |
| APR-005 | RETURN → requester, future steps CANCELLED | Yes |
| APR-006 | RESUBMIT increments round_no, recreates steps | Yes |
| APR-007 | Newer version requires withdraw + new request | Yes |
| APR-008 | APPROVAL vs EXECUTION separate statuses | Yes |
| APR-009 | Idempotent execution retry with payload_hash | Yes |
| APR-010 | before/after snapshots are audit, not source | Yes |
| APR-011 | Reassign PENDING requires permission + reason | Yes |

### 5.3 SLA/reminder rules (REM)

| Rule ID | Summary | First implementation? |
|---------|---------|:---:|
| REM-001 | Step may define due/reminder durations | Deferred to later sub-phase |
| REM-002 | Overdue → PENDING + is_overdue=1 | Deferred to later sub-phase |
| REM-003 | No auto-approve/reject/skip/escalate | Yes (invariant) |
| REM-004 | Idempotent reminder logging | Deferred |
| REM-005 | Retryable reminder failures | Deferred |

### 5.4 Delegation rules (DEL)

| Rule ID | Summary | First implementation? |
|---------|---------|:---:|
| DEL-001 | Requires original approver + company + permission + period | Deferred |
| DEL-002 | Delegate accepts + Admin activates | Deferred |
| DEL-003 | No equivalent role needed | Deferred |
| DEL-004 | Additive: primary keeps rights | Deferred |
| DEL-005 | No chaining | Deferred |
| DEL-006 | Delegate cannot approve own request | Deferred |
| DEL-007 | Audit: acted_by, on_behalf_of, delegation_id | Deferred |
| DEL-008 | Auto-expire at effective_to | Deferred |

### 5.5 Governance rules (GOV)

| Rule ID | Relevance |
|---------|-----------|
| GOV-001 | Admin configures only for ACTIVE process_code from Business_Process_Catalog |
| GOV-002 | Admin cannot create new process/form/table/condition-field/resolver/handler |
| GOV-003 | Published/active versions immutable |
| GOV-004 | Running request retains original version/binding/snapshot |
| GOV-005 | New version applies only to requests submitted after effective time |
| GOV-006 | Consistency across UI/API/database |
| GOV-007 | Immutable audit for material changes |
| GOV-008 | No audit erasure; no hard-invariant bypass |

---

## 6. Proposed scope for first implementation phase

### 6.1 Proposed minimal first slice

Split into sub-phases following the established B1/B2 pattern:

**Phase 1B.3-B1 — Workflow Backend Foundation**
- Business_Process_Catalog seed table (DEV-managed, admin read-only).
- Workflow_Definitions, Workflow_Definition_Versions, Workflow_Steps tables.
- Workflow_Step_Approver_Rules table (approver source configuration per step).
- Workflow_Conditions table (whitelisted conditions per version).
- Workflow_Bindings table (version → process + scope + effective period).
- Workflow_Instances (Approval_Requests), Workflow_Instance_Steps, Workflow_Actions tables.
- Version lifecycle: DRAFT → PUBLISHED → ACTIVE → RETIRED.
- Binding overlap rejection.
- Instance creation with snapshot/hash.
- Sequential approval runtime: APPROVE, RETURN, RESUBMIT.
- Approver resolution for initial subset (see DEC-1B3A-04).
- Execution status tracking: PENDING_EXECUTION → EXECUTING → EXECUTED / FAILED.
- Idempotent execution retry.
- Concurrency via rowVersion on steps and instances.
- Audit via SecurityAuditEventRecord for configuration and runtime actions.
- Migration V0006 + rollback U0006.

**Phase 1B.3-B2 — Workflow Admin Configuration UI**
- Workflow definition list/detail pages.
- Version create/edit (DRAFT only).
- Step configuration within version.
- Binding management.
- Publish action.
- Version lifecycle display.
- Permission gates: WORKFLOW_VIEW, WORKFLOW_CONFIG_MANAGE, WORKFLOW_PUBLISH, WORKFLOW_BIND_PROCESS.

**Phase 1B.3-B3 — Workflow Runtime / My Approvals UI**
- My pending approvals inbox.
- Approval action UI (APPROVE, RETURN with reason).
- Request detail with step history.
- RESUBMIT action.
- Permission gates: action-specific per business process permission.
- WORKFLOW_REASSIGN_PENDING for reassignment.

**Phase 1B.3-B4 — Pilot Integration**
- Integrate one pilot business process (see DEC-1B3A-02).
- Business module calls workflow via application service abstraction.
- Execution handler for pilot process.

### 6.2 Explicitly deferred from first implementation

- SLA/reminder processing (REM-001–REM-005) — requires background job infrastructure.
- Delegation (DEL-001–DEL-008) — requires additional tables and UI.
- Condition evaluation engine beyond simple field matching.
- DATA_FIELD_USER approver resolution (requires runtime data access pattern).
- Workflow comments/attachments.
- Active instance migration when definition changes.
- Notification system integration.
- Export/reporting.

---

## 7. Proposed architecture

### 7.1 Domain model

```
Workflow module (vertical slice under PTKD.Application.Workflows):

├── Configuration/
│   ├── DTOs/
│   │   ├── BusinessProcessDto
│   │   ├── WorkflowDefinitionDto, CreateWorkflowDefinitionRequest
│   │   ├── WorkflowVersionDto, CreateWorkflowVersionRequest
│   │   ├── WorkflowStepDto, CreateWorkflowStepRequest, UpdateWorkflowStepRequest
│   │   ├── ApproverRuleDto, CreateApproverRuleRequest
│   │   ├── WorkflowConditionDto
│   │   ├── WorkflowBindingDto, CreateWorkflowBindingRequest
│   │   └── PublishVersionRequest
│   ├── Services/
│   │   ├── IWorkflowConfigurationService
│   │   └── WorkflowConfigurationService
│   └── Validations/
│       ├── CreateWorkflowDefinitionRequestValidator
│       ├── CreateWorkflowStepRequestValidator
│       └── CreateWorkflowBindingRequestValidator
│
├── Runtime/
│   ├── DTOs/
│   │   ├── WorkflowInstanceDto, CreateWorkflowInstanceRequest
│   │   ├── WorkflowInstanceStepDto
│   │   ├── ApprovalActionRequest (action, reason, targetVersion)
│   │   ├── ResubmitRequest
│   │   └── ReassignStepRequest
│   ├── Services/
│   │   ├── IWorkflowRuntimeService
│   │   ├── WorkflowRuntimeService
│   │   ├── IApproverResolver
│   │   ├── ApproverResolver
│   │   ├── IWorkflowExecutionService
│   │   └── WorkflowExecutionService
│   └── Validations/
│       ├── ApprovalActionRequestValidator
│       └── ReassignStepRequestValidator
│
└── Integration/
    ├── IWorkflowIntegrationService  (business modules call this)
    └── WorkflowIntegrationService
```

### 7.2 Application services

- **WorkflowConfigurationService** — CRUD for definitions, versions, steps, approver rules, conditions, bindings. Enforces version lifecycle and binding overlap rules.
- **WorkflowRuntimeService** — Instance creation (snapshot + hash + step resolution), approval/return/resubmit actions, step transitions.
- **ApproverResolver** — Resolves candidate approvers by source type (SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP). Excludes requester per APR-001.
- **WorkflowExecutionService** — Manages execution status transitions (PENDING_EXECUTION → EXECUTING → EXECUTED/FAILED). Idempotent retry with payload_hash/correlation_id.
- **WorkflowIntegrationService** — Abstraction for business modules to start workflow, check status, and receive execution callbacks.

### 7.3 API v2

See section 10 for detailed endpoint list.

### 7.4 Persistence

EF Core entities mapped via IWorkflowDbContext interface added to AppDbContext. Configuration via Fluent API in separate configuration classes following existing pattern.

### 7.5 Audit

All configuration changes (create/edit definition, publish, bind) and runtime actions (approve, return, resubmit, reassign, execute) produce SecurityAuditEventRecord entries with:
- EventCode: stable action code (e.g., WORKFLOW_VERSION_PUBLISHED, APPROVAL_ACTION_TAKEN)
- EntityType: workflow entity type
- ChangedFieldsJson, BeforeStateJson, AfterStateJson where applicable
- CorrelationId linking related audit entries
- Reason where mandatory (reassignment, return)

### 7.6 Permissions

Use existing cataloged workflow permissions (already in permission-catalog.md):

| Permission | Purpose in workflow |
|-----------|---------------------|
| WORKFLOW_VIEW | View definitions, versions, bindings, instance status |
| WORKFLOW_CONFIG_MANAGE | Create/edit DRAFT workflow configuration |
| WORKFLOW_PUBLISH | Publish/activate a validated version |
| WORKFLOW_BIND_PROCESS | Bind version to process + scope |
| WORKFLOW_REASSIGN_PENDING | Reassign pending step with reason |
| WORKFLOW_AUDIT_VIEW | View workflow configuration/runtime audit |

Approval action permission is determined by the approver resolution — the user must be a resolved assignee for the specific step. This is not a standalone permission code but a runtime resolution check.

New permission codes may be needed for:
- Business process-specific execution permissions (e.g., execute CREATE_CUSTOMER after approval).
- These are open decisions — see DEC-1B3A-07.

### 7.7 Frontend

See section 11 for route/page proposals.

### 7.8 Tests

See section 16 for testing strategy.

---

## 8. Proposed database design

**PROPOSED ONLY — NO MIGRATION CREATED IN THIS TASK.**

### 8.1 Business_Process_Catalog
Danh mục quy trình nghiệp vụ — DEV-managed, admin read-only.

| Column | Type | Description (Vietnamese) | Notes |
|--------|------|--------------------------|-------|
| process_code | VARCHAR(100) | Mã quy trình nghiệp vụ | PK, immutable |
| process_name | NVARCHAR(500) | Tên quy trình | |
| description | NVARCHAR(2000) | Mô tả | Nullable |
| is_approval_required | BIT | Quy trình bắt buộc phê duyệt | DEFAULT 1 |
| is_active | BIT | Trạng thái hoạt động | DEFAULT 1 |
| created_at | DATETIMEOFFSET | Ngày tạo | |
| updated_at | DATETIMEOFFSET | Ngày cập nhật | |

No rowVersion — DEV-managed seed data, not user-editable.

### 8.2 Workflow_Definitions
Định nghĩa quy trình phê duyệt.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| definition_code | VARCHAR(100) | Mã định nghĩa | UNIQUE |
| definition_name | NVARCHAR(500) | Tên quy trình phê duyệt | |
| description | NVARCHAR(2000) | Mô tả | Nullable |
| process_code | VARCHAR(100) | FK → Business_Process_Catalog | |
| is_active | BIT | Trạng thái hoạt động | DEFAULT 1 |
| created_by | BIGINT | Người tạo FK → Users | |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Index: IX_Workflow_Definitions_ProcessCode on process_code.

### 8.3 Workflow_Definition_Versions
Phiên bản định nghĩa quy trình phê duyệt.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_definition_id | BIGINT | FK → Workflow_Definitions | |
| version_number | INT | Số phiên bản | |
| version_status | VARCHAR(20) | DRAFT / PUBLISHED / ACTIVE / RETIRED | |
| effective_from | DATETIMEOFFSET | Hiệu lực từ | Nullable (set at publish) |
| effective_to | DATETIMEOFFSET | Hiệu lực đến | Nullable |
| published_at | DATETIMEOFFSET | Ngày phát hành | Nullable |
| published_by | BIGINT | Người phát hành | Nullable |
| created_by | BIGINT | Người tạo FK → Users | |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Unique: (workflow_definition_id, version_number).
Index: IX_WDV_Status on (workflow_definition_id, version_status).

### 8.4 Workflow_Steps
Các bước phê duyệt trong phiên bản.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_version_id | BIGINT | FK → Workflow_Definition_Versions | |
| step_order | INT | Thứ tự bước | |
| step_name | NVARCHAR(500) | Tên bước | |
| description | NVARCHAR(2000) | Mô tả | Nullable |
| is_required | BIT | Bắt buộc | DEFAULT 1 |
| due_duration_minutes | INT | Thời hạn (phút) | Nullable, for future SLA |
| reminder_before_minutes | INT | Nhắc trước (phút) | Nullable, deferred |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Unique: (workflow_version_id, step_order).

### 8.5 Workflow_Step_Approver_Rules
Quy tắc phân công người phê duyệt cho mỗi bước.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_step_id | BIGINT | FK → Workflow_Steps | |
| approver_source_type | VARCHAR(50) | Loại nguồn: SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP, DATA_FIELD_USER | |
| approver_source_value | NVARCHAR(500) | Giá trị: user_id, role_code, dept_id, permission_code, admin_group_code, field_name | |
| priority | INT | Ưu tiên (for ordering) | DEFAULT 0 |
| created_at | DATETIMEOFFSET | | |

### 8.6 Workflow_Conditions
Điều kiện áp dụng phiên bản (whitelisted fields/operators only).

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_version_id | BIGINT | FK → Workflow_Definition_Versions | |
| field_code | VARCHAR(100) | Mã trường điều kiện (DEV whitelist) | |
| operator | VARCHAR(20) | Toán tử: EQ, NEQ, GT, LT, GTE, LTE, IN, CONTAINS | |
| value | NVARCHAR(1000) | Giá trị so sánh | |
| created_at | DATETIMEOFFSET | | |

### 8.7 Workflow_Bindings
Gán phiên bản quy trình cho process + scope + thời kỳ hiệu lực.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_version_id | BIGINT | FK → Workflow_Definition_Versions | |
| process_code | VARCHAR(100) | FK → Business_Process_Catalog | |
| scope_type | VARCHAR(20) | GLOBAL / COMPANY | |
| company_id | BIGINT | FK → Companies | Nullable; required if COMPANY |
| priority | INT | Ưu tiên | DEFAULT 0 |
| effective_from | DATETIMEOFFSET | Hiệu lực từ | |
| effective_to | DATETIMEOFFSET | Hiệu lực đến | Nullable |
| is_active | BIT | | DEFAULT 1 |
| created_by | BIGINT | | |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Overlap prevention: unique filtered index or check constraint on (process_code, scope_type, company_id, priority, effective_from, effective_to) where is_active = 1 and periods overlap — enforced at application layer with SERIALIZABLE transaction per WFD-006.

### 8.8 Workflow_Instances (Approval_Requests)
Yêu cầu phê duyệt — instance of a workflow for a specific business request.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_version_id | BIGINT | FK → Workflow_Definition_Versions | Frozen at creation |
| workflow_binding_id | BIGINT | FK → Workflow_Bindings | Frozen at creation |
| process_code | VARCHAR(100) | Mã quy trình | Denormalized for query |
| company_id | BIGINT | FK → Companies | Nullable if GLOBAL |
| requester_id | BIGINT | FK → Users | |
| business_entity_type | VARCHAR(100) | Loại đối tượng nghiệp vụ | e.g., CUSTOMER, SERVICE |
| business_entity_id | BIGINT | ID đối tượng nghiệp vụ | |
| instance_status | VARCHAR(30) | PENDING_APPROVAL / APPROVED / RETURNED / WITHDRAWN / PENDING_EXECUTION / EXECUTING / EXECUTED / FAILED | |
| round_no | INT | Vòng duyệt | DEFAULT 1 |
| workflow_snapshot_json | NVARCHAR(MAX) | Snapshot quy trình tại thời điểm tạo | |
| payload_json | NVARCHAR(MAX) | Dữ liệu nghiệp vụ gửi duyệt | |
| payload_hash | VARCHAR(128) | Hash payload cho idempotent retry | |
| correlation_id | UNIQUEIDENTIFIER | ID tương quan | |
| before_data_json | NVARCHAR(MAX) | Dữ liệu trước thay đổi | Nullable |
| after_data_json | NVARCHAR(MAX) | Dữ liệu sau thay đổi | Nullable |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Index: IX_WI_Requester on (requester_id, instance_status).
Index: IX_WI_BusinessEntity on (business_entity_type, business_entity_id).
Index: IX_WI_ProcessCompany on (process_code, company_id, instance_status).

### 8.9 Workflow_Instance_Steps
Các bước phê duyệt cụ thể cho instance.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_instance_id | BIGINT | FK → Workflow_Instances | |
| workflow_step_id | BIGINT | FK → Workflow_Steps (original) | Reference only |
| step_order | INT | Thứ tự bước | |
| step_name | NVARCHAR(500) | Tên bước (snapshot) | |
| round_no | INT | Vòng duyệt | |
| step_status | VARCHAR(20) | WAITING / PENDING / APPROVED / RETURNED / CANCELLED | |
| is_overdue | BIT | Quá hạn | DEFAULT 0 |
| assigned_at | DATETIMEOFFSET | Thời điểm phân công | Nullable |
| completed_at | DATETIMEOFFSET | Thời điểm hoàn thành | Nullable |
| completed_by | BIGINT | Người thực hiện | Nullable |
| created_at | DATETIMEOFFSET | | |
| updated_at | DATETIMEOFFSET | | |
| row_version | ROWVERSION | Concurrency | |

Index: IX_WIS_InstanceRound on (workflow_instance_id, round_no, step_order).

### 8.10 Workflow_Instance_Step_Assignees
Danh sách người được phân công cho mỗi bước instance.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_instance_step_id | BIGINT | FK → Workflow_Instance_Steps | |
| user_id | BIGINT | FK → Users | |
| approver_source_type | VARCHAR(50) | Nguồn phân công (snapshot) | |
| is_resolved | BIT | Đã phân giải thành công | DEFAULT 1 |
| created_at | DATETIMEOFFSET | | |

Unique: (workflow_instance_step_id, user_id).

### 8.11 Workflow_Actions
Hành động phê duyệt — append-only.

| Column | Type | Description | Notes |
|--------|------|-------------|-------|
| id | BIGINT IDENTITY(1,1) | PK | |
| workflow_instance_step_id | BIGINT | FK → Workflow_Instance_Steps | |
| workflow_instance_id | BIGINT | FK → Workflow_Instances | Denormalized |
| action_type | VARCHAR(20) | APPROVE / RETURN / REASSIGN | |
| acted_by | BIGINT | FK → Users | |
| on_behalf_of | BIGINT | FK → Users | Nullable, for future delegation |
| delegation_id | BIGINT | FK → future Delegations | Nullable |
| reason | NVARCHAR(2000) | Lý do | Required for RETURN, REASSIGN |
| comment | NVARCHAR(4000) | Ghi chú | Nullable |
| correlation_id | UNIQUEIDENTIFIER | ID tương quan | |
| created_at | DATETIMEOFFSET | Thời điểm hành động | |

Append-only. No UPDATE/DELETE.
Index: IX_WA_InstanceStep on (workflow_instance_step_id).
Index: IX_WA_ActedBy on (acted_by, created_at DESC).

### 8.12 Rollback strategy

U0006 must drop all workflow tables in reverse dependency order:
Workflow_Actions → Workflow_Instance_Step_Assignees → Workflow_Instance_Steps → Workflow_Instances → Workflow_Bindings → Workflow_Conditions → Workflow_Step_Approver_Rules → Workflow_Steps → Workflow_Definition_Versions → Workflow_Definitions → Business_Process_Catalog.

### 8.13 Delete policy

- Business_Process_Catalog: no delete (DEV-managed, soft-deactivate via is_active).
- Workflow_Definitions: soft-deactivate via is_active. No hard delete if versions exist.
- Workflow_Definition_Versions: no delete if PUBLISHED/ACTIVE/RETIRED. DRAFT may be deleted if no bindings/instances reference it.
- Workflow_Steps, Approver_Rules, Conditions: cascade with version deletion (DRAFT only).
- Workflow_Bindings: soft-deactivate. No hard delete if instances reference the binding.
- Workflow_Instances, Instance_Steps, Assignees: no delete. Append-only lifecycle.
- Workflow_Actions: no delete. Append-only per SEC-001.

---

## 9. In-progress workflow versioning strategy

**Recommended approach: Freeze at instance creation.**

1. When a Workflow_Instance is created, the current active Workflow_Definition_Version is snapshot into `workflow_snapshot_json` and its `workflow_version_id` is recorded immutably.
2. All steps and assignees are resolved and created at instance creation time.
3. Publishing a new version or changing a binding does NOT affect running instances (GOV-003, GOV-004, WFC-11).
4. Only instances submitted after the new version's `effective_from` use the new version (GOV-005, WFC-12).
5. RESUBMIT retains the original `workflow_version_id` even if a newer version exists (APR-006, WFC-10).
6. To use a newer version, the requester must WITHDRAW the old request and create a new business request (APR-007).
7. Migrating active instances to a new version is NOT supported in the first implementation. If needed later, it requires explicit admin action, separate audit, and a dedicated API with its own approval.

**Justification:** This is the simplest correct approach per the business rules. GOV-003/GOV-004 explicitly require running requests to retain their original version. Active instance migration introduces significant complexity and risk with minimal immediate business value.

---

## 10. Proposed API v2 design

**PROPOSED ONLY — NOT IMPLEMENTED.**

### 10.1 Configuration APIs

| Method | Endpoint | Permission | Purpose |
|--------|----------|-----------|---------|
| GET | /api/v2/workflows/processes | WORKFLOW_VIEW | List active business processes |
| GET | /api/v2/workflows/definitions | WORKFLOW_VIEW | List workflow definitions |
| POST | /api/v2/workflows/definitions | WORKFLOW_CONFIG_MANAGE | Create definition |
| GET | /api/v2/workflows/definitions/{id} | WORKFLOW_VIEW | Get definition detail |
| PUT | /api/v2/workflows/definitions/{id} | WORKFLOW_CONFIG_MANAGE | Update definition |
| POST | /api/v2/workflows/definitions/{id}/versions | WORKFLOW_CONFIG_MANAGE | Create new DRAFT version |
| GET | /api/v2/workflows/definitions/{id}/versions | WORKFLOW_VIEW | List versions |
| GET | /api/v2/workflows/versions/{versionId} | WORKFLOW_VIEW | Get version detail with steps |
| PUT | /api/v2/workflows/versions/{versionId} | WORKFLOW_CONFIG_MANAGE | Update DRAFT version |
| DELETE | /api/v2/workflows/versions/{versionId} | WORKFLOW_CONFIG_MANAGE | Delete DRAFT version (no instances) |
| POST | /api/v2/workflows/versions/{versionId}/steps | WORKFLOW_CONFIG_MANAGE | Add step to DRAFT |
| PUT | /api/v2/workflows/steps/{stepId} | WORKFLOW_CONFIG_MANAGE | Update step in DRAFT |
| DELETE | /api/v2/workflows/steps/{stepId} | WORKFLOW_CONFIG_MANAGE | Delete step in DRAFT |
| POST | /api/v2/workflows/steps/{stepId}/approver-rules | WORKFLOW_CONFIG_MANAGE | Add approver rule |
| POST | /api/v2/workflows/versions/{versionId}/publish | WORKFLOW_PUBLISH | Publish + validate |
| POST | /api/v2/workflows/versions/{versionId}/activate | WORKFLOW_PUBLISH | Activate published version |
| POST | /api/v2/workflows/versions/{versionId}/retire | WORKFLOW_PUBLISH | Retire active version |
| GET | /api/v2/workflows/bindings | WORKFLOW_VIEW | List bindings |
| POST | /api/v2/workflows/bindings | WORKFLOW_BIND_PROCESS | Create binding |
| PUT | /api/v2/workflows/bindings/{bindingId} | WORKFLOW_BIND_PROCESS | Update binding |

### 10.2 Runtime APIs

| Method | Endpoint | Permission | Purpose |
|--------|----------|-----------|---------|
| POST | /api/v2/workflows/instances | Business-specific | Create instance (start approval) |
| GET | /api/v2/workflows/instances/{instanceId} | WORKFLOW_VIEW | Get instance detail |
| GET | /api/v2/workflows/my-approvals | Authenticated | List pending approvals for current user |
| POST | /api/v2/workflows/instances/{instanceId}/steps/{stepId}/approve | Resolved assignee | Approve step |
| POST | /api/v2/workflows/instances/{instanceId}/steps/{stepId}/return | Resolved assignee | Return to requester |
| POST | /api/v2/workflows/instances/{instanceId}/resubmit | Requester | Resubmit after return |
| POST | /api/v2/workflows/instances/{instanceId}/withdraw | Requester | Withdraw request |
| POST | /api/v2/workflows/instances/{instanceId}/steps/{stepId}/reassign | WORKFLOW_REASSIGN_PENDING | Reassign pending step |
| POST | /api/v2/workflows/instances/{instanceId}/retry-execution | Business-specific | Retry failed execution |

### 10.3 Authorization model

- Configuration endpoints: `[RequirePermission]` with WORKFLOW_* codes and scope (GLOBAL/COMPANY).
- Runtime approval endpoints: resolved-assignee check at service layer, not via static permission attribute.
- Instance creation: business-module permission (e.g., CUSTOMER_CHANGE_REQUEST_CREATE).
- Company-scoped queries filter by user's effective company assignments.

### 10.4 Concurrency and validation

- rowVersion on versions, bindings, instances, instance steps — 409 on conflict.
- Version lifecycle transitions validated: only DRAFT → PUBLISHED → ACTIVE → RETIRED.
- Binding overlap checked with SERIALIZABLE transaction.
- Step approval uses atomic transition: check step_status = PENDING + user is assignee + rowVersion match, then update in one transaction.
- Approval action + next step activation in same transaction (APR-003).

### 10.5 Error handling

- BusinessRuleValidationException with typed error codes:
  - WF_VERSION_NOT_DRAFT, WF_VERSION_NOT_PUBLISHED, WF_BINDING_OVERLAP
  - WF_NO_VALID_BINDING, WF_NO_ASSIGNEE_FOR_STEP, WF_REQUESTER_IS_APPROVER
  - WF_STEP_NOT_PENDING, WF_INSTANCE_NOT_RETURNED, WF_INVALID_ROW_VERSION
  - WF_EXECUTION_ALREADY_COMPLETED, WF_DUPLICATE_EXECUTION
- 403 for permission denied (sanitized).
- 404 for not found (sanitized).
- No internal details exposed.

---

## 11. Proposed frontend structure

**PROPOSED ONLY — NOT IMPLEMENTED.**

### 11.1 Admin configuration pages

| Route | Page | Permission gate |
|-------|------|----------------|
| /workflows | WorkflowDefinitionsPage (list) | WORKFLOW_VIEW |
| /workflows/new | WorkflowDefinitionCreatePage | WORKFLOW_CONFIG_MANAGE |
| /workflows/:definitionId | WorkflowDefinitionDetailPage | WORKFLOW_VIEW |
| /workflows/:definitionId/versions/:versionId | WorkflowVersionDetailPage | WORKFLOW_VIEW |
| /workflows/:definitionId/versions/:versionId/edit | WorkflowVersionEditPage (DRAFT only) | WORKFLOW_CONFIG_MANAGE |
| /workflows/bindings | WorkflowBindingsPage | WORKFLOW_VIEW |

### 11.2 Runtime / approver pages

| Route | Page | Permission gate |
|-------|------|----------------|
| /my-approvals | MyApprovalsPage (inbox) | Authenticated |
| /my-approvals/:instanceId | ApprovalDetailPage | Authenticated (assignee check) |

### 11.3 Menu structure

- Admin menu section: "Workflow Configuration" gated by WORKFLOW_VIEW.
- User menu section: "My Approvals" visible to all authenticated users (empty if no pending).

### 11.4 Components

- StepEditor — configure steps within a DRAFT version.
- ApproverRuleEditor — configure approver rules per step.
- BindingEditor — configure binding with scope/company/period.
- ApprovalActionPanel — approve/return with reason field.
- StepTimeline — visual step progress display.
- VersionLifecycleBadge — DRAFT/PUBLISHED/ACTIVE/RETIRED status display.

---

## 12. Proposed permission strategy

### 12.1 Existing permissions (no changes needed)

| Permission | Scope | Already in catalog |
|-----------|-------|--------------------|
| WORKFLOW_VIEW | GLOBAL/COMPANY | Yes |
| WORKFLOW_CONFIG_MANAGE | GLOBAL/COMPANY | Yes |
| WORKFLOW_PUBLISH | GLOBAL/COMPANY | Yes |
| WORKFLOW_BIND_PROCESS | GLOBAL/COMPANY | Yes |
| WORKFLOW_REASSIGN_PENDING | COMPANY | Yes |
| WORKFLOW_AUDIT_VIEW | GLOBAL/COMPANY | Yes |
| DELEGATION_CREATE | COMPANY | Yes (deferred) |
| DELEGATION_ACTIVATE | COMPANY | Yes (deferred) |

### 12.2 Possibly new permissions (open decision)

| Proposed code | Purpose | Status |
|--------------|---------|--------|
| CUSTOMER_CHANGE_REQUEST_CREATE | Already in catalog as CUSTOMER_CHANGE_REQUEST_CREATE | Exists — needs PermissionCodes.cs sync |
| Business-process execution permissions | Execute approved request for specific process type | See DEC-1B3A-07 |

### 12.3 Permission enforcement

- Backend remains authoritative.
- Frontend gates are UX only.
- DENY wins per AUTH-004.
- Company scope checked per AUTH-007.
- Approval action is not a static permission — it is resolved dynamically (user must be in instance step assignees).

---

## 13. Integration strategy

### 13.1 Business module → Workflow

Business modules (Customer, Service, Payment) call `IWorkflowIntegrationService.StartWorkflowAsync(request)` where request includes:
- process_code (e.g., "CREATE_CUSTOMER", "CUSTOMER_MASTER_CHANGE", "SERVICE_PRICE_OVERRIDE")
- business_entity_type and business_entity_id
- company_id (if COMPANY-scoped)
- requester_id
- payload (business data for review/execution)
- before_data / after_data snapshots

The integration service:
1. Finds the active binding for (process_code, scope, company).
2. Resolves the version and steps.
3. Resolves assignees per step.
4. Validates at least one assignee per step and requester exclusion.
5. Creates the Workflow_Instance with snapshot/hash.
6. Returns instance ID to the business module.

### 13.2 Workflow → Business module (execution)

After final approval, `WorkflowExecutionService` transitions to PENDING_EXECUTION, then calls a registered `IWorkflowExecutionHandler` for the process_code. The handler:
1. Reads the approved payload.
2. Performs the business operation (e.g., create customer, change customer master).
3. Returns success/failure.
4. On success: instance → EXECUTED.
5. On failure: instance → FAILED, retryable with same payload_hash.

Each business module registers its handler via DI. The workflow engine does not contain business logic.

### 13.3 Status exposure

Business modules query workflow status via `IWorkflowIntegrationService.GetInstanceStatusAsync(businessEntityType, businessEntityId)` to show approval progress in their own UI.

### 13.4 Audit linking

Workflow_Actions.correlation_id links to SecurityAuditEventRecord entries on the business entity, providing end-to-end traceability.

---

## 14. Audit strategy

| Event | Audit record | Reason required |
|-------|-------------|:-:|
| Definition created | Yes | No |
| Version created/edited (DRAFT) | Yes | No |
| Version published | Yes | No |
| Version activated | Yes | No |
| Version retired | Yes | No |
| Binding created/updated | Yes | No |
| Instance created | Yes | No |
| Step approved | Yes (via Workflow_Actions) | No |
| Step returned | Yes (via Workflow_Actions) | Yes |
| Step reassigned | Yes (via Workflow_Actions) | Yes |
| Instance resubmitted | Yes | No |
| Instance withdrawn | Yes | No |
| Execution completed | Yes | No |
| Execution failed | Yes | No |

All audit records follow SecurityAuditEventRecord pattern. Workflow_Actions table is append-only per SEC-001. No passwords, tokens, or sensitive URLs in audit per SEC-005.

---

## 15. Concurrency strategy

| Entity | Mechanism | Boundary |
|--------|-----------|----------|
| Workflow_Definition_Versions | rowVersion | Optimistic on edit/publish |
| Workflow_Bindings | rowVersion + SERIALIZABLE tx for overlap check | Prevents duplicate active bindings |
| Workflow_Instances | rowVersion | Optimistic on status transitions |
| Workflow_Instance_Steps | rowVersion | Atomic approval action — first wins |
| Approval action + next step activation | Single SERIALIZABLE transaction | APR-003: approve current + activate next atomically |
| Binding overlap detection | SERIALIZABLE with UPDLOCK/HOLDLOCK per DEC-1B-014 | Same pattern as existing temporal locking |

---

## 16. Testing strategy

### 16.1 Backend unit tests

- Approver resolution for each source type.
- Version lifecycle transitions (valid and invalid).
- Binding overlap detection.
- Requester exclusion from assignees (APR-001).
- Step transition logic (PENDING → APPROVED/RETURNED).
- Round increment on resubmit.
- Payload hash computation and idempotent retry check.
- Snapshot freeze at instance creation.

### 16.2 Backend integration tests

- Full instance lifecycle: create → approve all steps → execute.
- Return and resubmit flow.
- Concurrent approval actions (first wins, second 409).
- Binding with company scope overriding global.
- Version change does not affect running instance.
- Multi-assignee step: one action closes for all.
- Instance creation blocked when no valid binding or empty assignee.

### 16.3 API tests

- Configuration CRUD with permission enforcement.
- Publish validation.
- Binding overlap rejection.
- Runtime approval/return/resubmit/withdraw/reassign.
- 403 for unauthorized users.
- 409 for concurrency conflicts.
- Error codes and sanitized responses.

### 16.4 Migration/rollback tests

- V0006 applies cleanly.
- U0006 rolls back cleanly.
- Re-apply V0006 after rollback.

### 16.5 Frontend tests

- Workflow definition list/create/detail pages.
- Version lifecycle UI.
- Step editor.
- Binding management.
- My approvals inbox.
- Approval action with reason.
- Permission gate visibility.
- Error/403/empty states.

### 16.6 Permission/deny tests

- WORKFLOW_CONFIG_MANAGE required for DRAFT edits.
- WORKFLOW_PUBLISH required for publish/activate.
- WORKFLOW_BIND_PROCESS required for binding.
- DENY on workflow permissions blocks access.
- Non-assignee cannot approve.
- Requester cannot approve own request.

### 16.7 Audit tests

- Configuration changes produce audit records.
- Approval actions produce Workflow_Actions records.
- Workflow_Actions is append-only (no update/delete).
- Audit contains correlation_id linking to business entity.

### 16.8 Version-change / in-progress tests

- New version publish does not change running instance steps.
- Running instance retains original version_id after resubmit.
- New instance uses new version after effective_from.

### 16.9 Concurrency tests

- Simultaneous approval on same step: one succeeds, one 409.
- Simultaneous binding creation: overlap detected.
- Version edit during publish: 409.

---

## 17. Proposed implementation phases

| Phase | Scope | Estimated complexity |
|-------|-------|---------------------|
| 1B.3-B1 | Workflow Backend Foundation — database, configuration services, runtime services, API, audit, tests | High |
| 1B.3-B2 | Workflow Admin Configuration UI — definition/version/step/binding management pages + tests | Medium |
| 1B.3-B3 | Workflow Runtime / My Approvals UI — inbox, approval action, request detail + tests | Medium |
| 1B.3-B4 | Pilot Integration — connect one business process (e.g., CREATE_CUSTOMER) end-to-end + tests | Medium |

Each phase follows the established lifecycle: Plan → PO Plan Acceptance → Implementation → Implementation Acceptance Review → PO Implementation Acceptance → Closure Review → PO Final Acceptance.

B1 should be implemented first as B2/B3/B4 depend on it. B2 and B3 could potentially be parallelized or reordered. B4 requires at least B1+B3.

---

## 18. Open decisions

| ID | Topic | Proposed default | Alternatives | Notes |
|----|-------|-----------------|-------------|-------|
| DEC-1B3A-01 | First implementation slice | B1 backend foundation only | B1+B2 combined | B1 alone is testable via API |
| DEC-1B3A-02 | Pilot business process | CREATE_CUSTOMER (CUS-002) | CUSTOMER_MASTER_CHANGE, SERVICE_PRICE_OVERRIDE | CREATE_CUSTOMER is simplest; already has deferred workflow need |
| DEC-1B3A-03 | Workflow versioning behavior | Freeze at instance creation; no active instance migration | Allow admin migration with audit | Freeze is simpler and matches GOV-003/GOV-004 exactly |
| DEC-1B3A-04 | Approver resolution types for first slice | SPECIFIC_USER, ROLE, PERMISSION, ADMIN_GROUP (4 of 8) | All 8 types | Remaining 4 (DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, DATA_FIELD_USER) can follow |
| DEC-1B3A-05 | Sequential only | Yes (per WFD-001 v1.1) | Parallel support | Specification explicitly limits to sequential |
| DEC-1B3A-06 | Company scope for bindings | GLOBAL and COMPANY supported | GLOBAL only | Business rules require both (WFD-005) |
| DEC-1B3A-07 | New permission codes for execution | Defer — use existing business permissions for now | Add per-process execution codes | Existing CUSTOMER_CREATE_FINAL can serve as execution permission for CREATE_CUSTOMER pilot |
| DEC-1B3A-08 | Admin UI scope for B2 | Definition + version + step + binding CRUD | Include condition editor | Condition editor is complex; can follow |
| DEC-1B3A-09 | My approvals UI scope for B3 | Inbox + approve/return + request detail | Include resubmit + withdraw + reassign | Core approve/return is minimum viable |
| DEC-1B3A-10 | Delegation | Deferred to separate phase | Include in B1 schema | Tables can be added; runtime deferred |
| DEC-1B3A-11 | SLA/reminders | Deferred to separate phase | Include in B1 | Requires background job infrastructure |
| DEC-1B3A-12 | Comments/attachments on actions | Optional comment field on Workflow_Actions | Separate attachment table | Comment field is low cost; attachments deferred |
| DEC-1B3A-13 | Cancellation/restart | Withdraw supported; restart = new request | Allow restart of withdrawn | Simpler; matches APR-007 |
| DEC-1B3A-14 | Active instance migration | Not supported in first slice | Admin tool for migration | Can be added later with proper audit |
| DEC-1B3A-15 | Audit retention/export | Append-only; no retention policy in first slice | Archive after N months | Mirrors DEC-1B-017 deferral |
| DEC-1B3A-16 | Production rollout | Requires separate production release approval | Auto-deploy | Consistent with existing V0005 constraint |
| DEC-1B3A-17 | Business_Process_Catalog seed data | DEV-managed SQL seed in V0006; admin cannot add processes | Admin creates processes | GOV-001/GOV-002 prohibit admin-created processes |
| DEC-1B3A-18 | Condition evaluation scope for first slice | Simple field-value matching (EQ, NEQ, IN) | Full expression engine | Start minimal; expand based on actual process needs |

---

## 19. Risks

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Overbuilding workflow engine beyond documented requirements | High | Strict scope per business rules; no invented features |
| 2 | Hardcoding business approval logic in workflow engine | High | Integration via IWorkflowExecutionHandler; workflow has no business module knowledge |
| 3 | Permission complexity from workflow + existing security | Medium | Use existing cataloged permissions; new codes only after PO decision |
| 4 | Audit volume from workflow configuration + runtime | Medium | Append-only; retention deferred |
| 5 | In-progress workflow version changes | Medium | Freeze-at-creation strategy per GOV-003/GOV-004 |
| 6 | Service/Payment dependencies not fully discovered | Medium | Service/Payment modules planned separately; workflow provides generic engine |
| 7 | Production migration control | High | No auto-migration; separate release approval |
| 8 | Approver resolution complexity (8 source types) | Medium | Implement 4 types first (DEC-1B3A-04); add remaining later |
| 9 | Condition evaluation complexity | Medium | Simple field matching first (DEC-1B3A-18) |
| 10 | Background job infrastructure for SLA/reminders | Medium | Defer SLA/reminders until job infrastructure exists |
| 11 | Customer first slice must not be destabilized | Medium | Workflow adds new tables/code; does not modify existing customer code |

---

## 20. Explicit non-authorization

- This plan does not authorize implementation.
- No source code.
- No tests.
- No migrations.
- No rollbacks.
- No PermissionCodes.cs changes.
- No permission-catalog.md changes.
- No production migration.
- No Service/Payment/Merge implementation.

---

## 21. Recommended Project Owner decision

**Recommend approving Phase 1B.3-B1 — Workflow Backend Foundation as the next implementation phase.**

This would deliver:
- Database schema (V0006) for all workflow tables.
- Business_Process_Catalog seed with initial process codes.
- Configuration services for definition/version/step/binding management.
- Runtime services for instance creation and sequential approval.
- Approver resolution for 4 source types.
- Execution status management with idempotent retry.
- API v2 endpoints for configuration and runtime.
- Full test suite.
- Audit integration.
- No frontend UI (deferred to B2/B3).

If approved, the next authorized task would be:
**Phase 1B.3-B1 Workflow Backend Foundation Plan** — a detailed implementation plan for the backend foundation only, following the same lifecycle as Phase 1B.2-B1.

---

## 22. Conclusion

PHASE 1B.3-A WORKFLOW/APPROVAL ENGINE DETAILED PLAN READY FOR PROJECT OWNER REVIEW
