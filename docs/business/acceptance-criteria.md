# PTKD ERP - Acceptance Criteria

- Version: 1.1
- Purpose: traceable QA/UAT contract for Codex implementation
- Evidence required: automated test where practical, plus migration/build/test logs and manual UAT evidence for UI/notification behavior.

## Authorization

| ID | Acceptance criterion |
|---|---|
| AUTH-01 | PTKD staff receive the correct department baseline permissions after sign-in. |
| AUTH-02 | CASHIER role is effective only for the assigned company. |
| AUTH-03 | An effective individual DENY blocks an action even when department/role ALLOW exists. |
| AUTH-04 | A user in Company A cannot view COMPANY service/payment/document data of Company B. |
| AUTH-05 | GLOBAL customer master can be searched by permission; sensitive fields are correctly masked. |
| AUTH-06 | Permission cache is refreshed/invalidated when department, role or individual permission changes. |

## Customer master

| ID | Acceptance criterion |
|---|---|
| CUS-01 | Ordinary staff cannot directly edit full_name, cccd, dob, phone or contact_address. |
| CUS-02 | Staff submit CREATE_CUSTOMER; only the customer-data administration group performs final creation. |
| CUS-03 | Final duplicate check blocks CREATE_CUSTOMER when active CCCD already exists. |
| CUS-04 | CUSTOMER_MASTER_CHANGE target-version conflict does not overwrite newer data. |
| CUS-05 | Direct administrator correction requires reason and before/after audit. |
| CUS-06 | Customer_Company_Context is unique by customer+company and does not expose internal_notes across companies. |
| CUS-07 | Company spending matches confirmed payments; group total is visible only with the dedicated permission. |

## Payment and reconciliation

| ID | Acceptance criterion |
|---|---|
| PAY-01 | An authorized cashier creates and confirms a valid bill without Approval_Requests. |
| PAY-02 | A user without PAYMENT_CONFIRM cannot confirm a bill. |
| PAY-03 | A bill with no items or mismatched total is rejected. |
| PAY-04 | A cashier cannot edit/delete a payment after CONFIRMED. |
| PAY-05 | ADMIN_PAYMENT may correct allowed company/customer/service/amount/date/method fields with a valid correction package. |
| PAY-06 | Admin cannot change id, bill_code, CONFIRMED status or VND currency. |
| PAY-07 | Company/date correction marks/recalculates all affected old/new day/month periods. |
| PAY-08 | Correction creates before/after audit, reason and notifications to all required recipients. |

## Approval runtime

| ID | Acceptance criterion |
|---|---|
| APR-01 | The requester cannot approve any step of the same request. |
| APR-02 | Standard-price renewal creates no approval; price differing from snapshot requires SERVICE_PRICE_OVERRIDE. |
| APR-03 | Unapproved exceptional price cannot be used to create/confirm a bill. |
| APR-04 | RETURNED can be resubmitted as a new round while preserving the old round history. |
| APR-05 | Two simultaneous valid approver actions result in one successful transaction and one conflict. |
| APR-06 | FAILED execution can retry idempotently with the same payload hash and cannot apply twice. |

## Delegation

| ID | Acceptance criterion |
|---|---|
| DEL-01 | Delegation grants no right before delegate acceptance and Admin activation. |
| DEL-02 | Delegate need not hold an equivalent role but can act only for the delegated permission/scope. |
| DEL-03 | While ACTIVE, primary and delegate see the step; the first valid action closes it for both. |
| DEL-04 | Delegate cannot approve own request and cannot delegate onward. |
| DEL-05 | At effective_to, delegation becomes EXPIRED automatically; primary approver retains original right. |
| DEL-06 | Delegated audit records acted_by, on_behalf_of and delegation_id. |

## Security and audit

| ID | Acceptance criterion |
|---|---|
| SEC-01 | No endpoint relies only on UI visibility for authorization. |
| SEC-02 | Business users cannot update/delete audit or Approval_Actions. |
| SEC-03 | Sensitive data in logs, exports and documents is masked/restricted by permission. |

## Dynamic workflow configuration

| ID | Acceptance criterion |
|---|---|
| WFC-01 | Admin can select only ACTIVE process codes from Business_Process_Catalog and cannot create a process/form. |
| WFC-02 | DRAFT workflow version is editable; PUBLISHED/ACTIVE structure cannot be edited/deleted. |
| WFC-03 | Workflow is sequential; each request round has at most one PENDING step. |
| WFC-04 | Approvers resolve correctly for SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP and DATA_FIELD_USER. |
| WFC-05 | A multi-assignee step closes on the first valid action; later candidates receive conflict. |
| WFC-06 | If any step has no approver, submit is blocked, no request is created, and requester/Admin are notified. |
| WFC-07 | COMPANY binding overrides GLOBAL; GLOBAL is used when no matching COMPANY binding exists. |
| WFC-08 | Publication rejects overlapping bindings for the same process/scope/condition/effective period/priority. |
| WFC-09 | RETURN goes to requester, future steps are CANCELLED, and RESUBMIT increments round while preserving history. |
| WFC-10 | RESUBMIT retains the original workflow_version_id even when a newer version exists. |
| WFC-11 | Publishing a new version/binding does not change steps or assignees of running requests. |
| WFC-12 | Requests submitted after effective_from use the new version; earlier requests keep the previous version. |
| WFC-13 | Reminders fire before/at/after due as configured, are deduplicated, and never auto-escalate. |
| WFC-14 | Overdue step remains PENDING with is_overdue=1 and only valid assignee/delegate may act. |
| WFC-15 | Reassigning PENDING requires WORKFLOW_REASSIGN_PENDING, reason, self-approval check and audit. |
| WFC-16 | Admin cannot configure SQL/JavaScript or fields/operators outside the DEV whitelist. |
| WFC-17 | REJECT by authorized current approver permanently terminates the instance; instance cannot be resubmitted or executed. |
| WFC-18 | RETRY on a FAILED execution instance triggers execution securely without breaking idempotency. |
## Definition of acceptance evidence

For each implemented criterion, the delivery report must include:

1. Rule/criterion IDs.
2. Files changed.
3. Database migration and rollback scripts.
4. API endpoints/DTOs affected.
5. Automated test names and actual result.
6. Build command and actual result.
7. Manual verification steps for UI/notification flows.
8. Remaining risk or reason a criterion is not yet applicable.

## Go-live gate

Go-live is blocked until all applicable AUTH, CUS, PAY, APR, DEL, SEC and WFC criteria pass, and CNTT/PTKD/Kế toán confirm permission mapping, company scope, process catalog, workflow versions/bindings, approver resolution, payment/reconciliation data and reminder behavior.
