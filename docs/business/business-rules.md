# PTKD ERP - Business Rules

- Version: 1.1
- Status: approved baseline
- Canonical source: `PTKD-Specification-v1.1.docx`
- Scope: authorization, customer master, dynamic sequential approval workflow, delegation, payment control, audit and sensitive data.

## How Codex must use this file

1. Cite the rule IDs affected by every implementation plan, migration, API and test.
2. Do not reinterpret a rule silently. Record conflicts in `docs/decisions/` and request a change decision.
3. A UI restriction never replaces API/service authorization or database invariants.
4. Rules marked **hard invariant** must be enforced even for Admin and Super Admin.

## Governance and system boundaries

| Rule ID | Rule |
|---|---|
| GOV-001 | Admin may configure approval workflows only for an ACTIVE `process_code` already published by DEV in `Business_Process_Catalog`. |
| GOV-002 | Admin may not create a new business process, business form, database table, condition field, resolver or execution handler. |
| GOV-003 | Published/active workflow versions are immutable. A structural change requires a new version. |
| GOV-004 | A running request retains its original workflow version, binding, resolved assignees and snapshot. |
| GOV-005 | Only requests submitted after the new version effective time use that version. |
| GOV-006 | Every business-sensitive operation must be consistent across UI, API/service and database controls. |
| GOV-007 | All material permission, workflow, payment and customer-master changes require immutable audit records. |
| GOV-008 | No business user, Admin or Super Admin may erase audit history or bypass hard invariants. |

## Data ownership and company scope

| Rule ID | Rule |
|---|---|
| DATA-001 | `Profiles` and `Customers` are GLOBAL customer-master resources shared across the Tổng công ty; do not duplicate a customer per company. |
| DATA-002 | Company-specific customer information is stored in `Customer_Company_Context`, unique by `(customer_id, company_id)`. |
| DATA-003 | Service, payment, reconciliation, operational documents and approval requests are scoped by `company_id`. |
| DATA-004 | A user may see company-scoped data only for companies where the user has an effective role/permission assignment. |
| DATA-005 | GLOBAL ownership does not automatically grant access to sensitive fields. A separate sensitive-data permission is required. |
| DATA-006 | Do not use `Customers.total_spent` as a universal financial source. Calculate spending from confirmed payments by company. |
| DATA-007 | Do not use `Customers.assigned_staff_id` as the sole staff-assignment source. Use company context/history. |
| DATA-008 | Site determines company scope and the scope is inherited through Zone, Block, Lot and Plot. |

## Authorization

| Rule ID | Rule |
|---|---|
| AUTH-001 | Department permissions are baseline ALLOW permissions. |
| AUTH-002 | Company role permissions add business permissions within the assigned company. |
| AUTH-003 | Effective individual ALLOW permissions add temporary or exceptional access. |
| AUTH-004 | Effective individual DENY overrides department, role and individual ALLOW permissions. |
| AUTH-005 | Delegation adds only the right to act on the matching approval step; it does not grant entity edit or administration rights. |
| AUTH-006 | System hard rules override all soft permissions. |
| AUTH-007 | A COMPANY permission is effective only when the user has an ACTIVE company assignment for that company. |
| AUTH-008 | A user with multiple roles receives the union of ALLOW permissions within the same company, subject to DENY and hard rules. |
| AUTH-009 | Every endpoint must re-check permission and data scope at the server. Hiding a UI button is not authorization. |
| AUTH-010 | Financial stored procedures must validate actor, record state and invariants inside the database transaction. |
| AUTH-011 | `UserPermissions`, if retained, is a calculated cache/view and never the source of authority. |
| AUTH-012 | Permission cache must be invalidated when department, role, individual permission, user status or policy version changes. |

## Customer master

| Rule ID | Rule |
|---|---|
| CUS-001 | Ordinary business staff may search/use customer master but may not directly edit full name, CCCD, date of birth, phone or legal/contact address. |
| CUS-002 | Staff create `CREATE_CUSTOMER` or `CUSTOMER_MASTER_CHANGE` requests for customer-master creation/change. |
| CUS-003 | Only the customer-data administration group may create/update/merge customer master as a final operation. |
| CUS-004 | A direct customer-master correction by an authorized data administrator requires a reason and field-level before/after audit. |
| CUS-005 | Duplicate checking must run before submit and again immediately before execution. |
| CUS-006 | Active non-empty CCCD values require a filtered unique constraint/index. Phone is a duplicate signal, not an absolute unique key. |
| CUS-007 | Customer merge must preview affected services, payments, documents and company contexts. Source history is retained and marked MERGED. |
| CUS-008 | Customer-master execution is transactional and creates/updates the relevant `Customer_Company_Context`. |
| CUS-009 | `target_version` must be rechecked before executing a customer change; conflict must not overwrite newer data. |

## Payment and reconciliation

| Rule ID | Rule |
|---|---|
| PAY-001 | A user with role CASHIER and permission `PAYMENT_CONFIRM` may create and confirm the same valid payment. No approval request is created for normal confirmation. |
| PAY-002 | A payment must contain at least one item. `total_amount` is calculated by the server from item amounts and must be greater than zero. |
| PAY-003 | Confirmation moves payment one-way from DRAFT to CONFIRMED. |
| PAY-004 | After confirmation, the cashier may view/print but may not edit or delete the payment. **Hard invariant.** |
| PAY-005 | Only `ADMIN_PAYMENT` with `PAYMENT_CORRECT_CONFIRMED` may correct a confirmed payment. A non-empty correction reason is mandatory. |
| PAY-006 | Confirmed payment correction may not change `id`, `bill_code`, status from CONFIRMED, or currency from VND. **Hard invariant.** |
| PAY-007 | No cancel, refund or partial-payment state is introduced by this specification. |
| PAY-008 | Payment correction must preserve customer/company/service-cycle consistency and must not pay the same cycle twice. |
| PAY-009 | When company or payment date changes, all affected old/new daily and monthly reconciliation periods must be marked/recalculated. |
| PAY-010 | Payment correction, items, aggregates, reconciliation flags and audit are committed atomically. |
| PAY-011 | After commit, notify the cashier/confirming user, PTKD manager and reconciliation accounting group; include old/new company recipients where applicable. |
| PAY-012 | The client may not supply trusted totals, actor fields or authorization decisions. |

## Approval workflow design-time

| Rule ID | Rule |
|---|---|
| WFD-001 | Workflow type in v1.1 is SEQUENTIAL only. Parallel, all-approver and minimum-N modes are not supported. |
| WFD-002 | Admin may create a workflow, DRAFT version, ordered steps, approver rules, whitelisted conditions, SLA/reminders and bindings. |
| WFD-003 | Admin may not enter SQL, JavaScript or arbitrary expressions. Conditions use DEV-published fields and operators only. |
| WFD-004 | A DRAFT version may be edited and validated; PUBLISHED/ACTIVE versions may not be structurally edited or deleted. |
| WFD-005 | Workflow scope is GLOBAL or COMPANY. A matching COMPANY binding overrides GLOBAL. |
| WFD-006 | Overlapping active bindings for the same process, scope, condition, priority and effective period must be rejected at publish time. |
| WFD-007 | If a required-approval process has no valid binding, submission is blocked and the requester/Admin workflow are notified. |
| WFD-008 | The system must resolve all steps and at least one valid assignee for every step before creating the request. |
| WFD-009 | Supported approver sources are SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP and DATA_FIELD_USER. |
| WFD-010 | ROLE/DEPARTMENT/PERMISSION/ADMIN_GROUP may resolve multiple assignees, but one valid action closes the single sequential step. |
| WFD-011 | Removing the requester from candidate assignees must not leave a step without an assignee; otherwise submission is blocked. |
| WFD-012 | Workflow snapshot and hash are stored on the request and are the basis for runtime and execution retry. |

## Approval runtime

| Rule ID | Rule |
|---|---|
| APR-001 | The requester may not act on any approval step of the same request, including through delegation. |
| APR-002 | At most one step in a request round is PENDING. Future steps are WAITING. |
| APR-003 | Approving a step activates exactly the next step in the same transaction. |
| APR-004 | Concurrent actions use rowversion/atomic transition; the first commit succeeds and later actions receive conflict. |
| APR-005 | RETURN always returns to the requester, marks the current step RETURNED and future WAITING steps CANCELLED. |
| APR-006 | RESUBMIT increments `round_no`, recreates steps/assignees, keeps prior history and retains the original workflow version. |
| APR-007 | To use a newer workflow version, the requester must withdraw the old request and create a new business request. |
| APR-008 | APPROVAL and EXECUTION statuses are separate. An approved request may be EXECUTING, EXECUTED or FAILED. |
| APR-009 | Execution retry is idempotent and uses the approved payload, `payload_hash` and `correlation_id`; it must not apply twice. |
| APR-010 | `before_data` and `after_data` are review/audit snapshots and do not replace source business tables. |
| APR-011 | A pending-step reassignment requires `WORKFLOW_REASSIGN_PENDING`, reason, self-approval validation and immutable audit. |
| APR-012 | REJECT permanently terminates the instance; it is distinct from RETURN and cannot be resubmitted or executed. |
| APR-013 | RETRY execution is allowed only on FAILED execution instances and must preserve idempotency. |

## SLA and reminders

| Rule ID | Rule |
|---|---|
| REM-001 | Each step may define due duration, reminder-before, reminder-at-due and repeat-after-overdue. |
| REM-002 | An overdue step remains PENDING and is marked `is_overdue=1`. |
| REM-003 | Overdue processing must not auto-approve, auto-reject, skip or escalate to another approver. |
| REM-004 | Reminder processing is idempotent and records deduplication/status in `Approval_Reminder_Logs`. |
| REM-005 | Reminder failures are logged and retryable without duplicate successful notifications. |

## Delegation

| Rule ID | Rule |
|---|---|
| DEL-001 | Delegation requires the original approver, company, delegable approval permission and effective period. |
| DEL-002 | The delegate must accept and an Admin with `DELEGATION_ACTIVATE` must activate before rights become effective. |
| DEL-003 | A delegate does not need an equivalent manager role. |
| DEL-004 | Delegation is additive: the original approver keeps the original right while ACTIVE. |
| DEL-005 | Delegation chaining is prohibited. |
| DEL-006 | The delegate may not approve a request the delegate submitted. |
| DEL-007 | Every delegated action records `acted_by`, `on_behalf_of` and `delegation_id`. |
| DEL-008 | Delegation automatically expires at `effective_to`; pending requests remain available to the primary approver. |

## Audit, notifications and sensitive data

| Rule ID | Rule |
|---|---|
| SEC-001 | Audit and `Approval_Actions` are append-only; business users may not update/delete them. |
| SEC-002 | Audit includes actor/acting-as, entity, company, stable action code, changed fields, selected before/after, reason, correlation ID and time. |
| SEC-003 | Reasons are mandatory for customer-master changes, confirmed-payment corrections and sensitive permission changes. |
| SEC-004 | CCCD, legal addresses, bank information and identity documents are masked according to permission. |
| SEC-005 | Audit/snapshot data must not contain passwords, tokens, file bytes or permanent signed URLs. |
| SEC-006 | Sensitive export logs purpose, filters, record count and actor. |
| SEC-007 | Notifications are created only after the related transaction commits. |
| SEC-008 | Notification links must not expose permanent public file URLs. |
