# PTKD ERP - Permission Catalog

- Version: 1.1
- Source: `PTKD-Specification-v1.1.docx`, section 4
- Rule: all permission assignments reference this catalog. Do not store uncontrolled action strings in department, role or individual-permission tables.

## Permission fields

| Field | Meaning |
|---|---|
| `permission_code` | Stable primary key; never rename after release without a mapped migration. |
| `module_code` | Functional module. |
| `action_code` | Stable action. |
| `data_scope` | GLOBAL, COMPANY or a catalog entry explicitly supporting both. |
| `is_sensitive` | Extra audit/masking/reason controls may apply. |
| `is_delegable` | May be delegated only for an approval step. |
| `requires_reason` | A reason is required when granting/using the permission as specified. |
| `is_active` | Inactive permissions may not authorize new actions. |

## Canonical permissions

| permission_code | module_code | action_code | data_scope | sensitive | delegable | Purpose |
|---|---|---|---|---:|---:|---|
| CUSTOMER_VIEW_BASIC | CUSTOMER | VIEW | GLOBAL | No | No | Search and view basic customer master. |
| CUSTOMER_VIEW_SENSITIVE | CUSTOMER | VIEW_SENSITIVE | GLOBAL | Yes | No | View unmasked sensitive customer fields/documents. |
| CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER | PROPOSE_CHANGE | GLOBAL | No | No | Submit customer create/change proposals. |
| CUSTOMER_CREATE_FINAL | CUSTOMER | CREATE_FINAL | GLOBAL | Yes | No | Execute approved customer creation. |
| CUSTOMER_MASTER_UPDATE | CUSTOMER | UPDATE_MASTER | GLOBAL | Yes | No | Update customer master as authorized data administrator. |
| CUSTOMER_MERGE_DUPLICATE | CUSTOMER | MERGE | GLOBAL | Yes | No | Merge duplicate customers with preview/audit. |
| CUSTOMER_GROUP_FINANCE_VIEW | CUSTOMER | VIEW_GROUP_FINANCE | GLOBAL | Yes | No | View group-wide customer spending. |
| SERVICE_CREATE_STANDARD | SERVICE | CREATE | COMPANY | No | No | Create a service at standard terms. |
| SERVICE_RENEW_STANDARD | SERVICE | RENEW | COMPANY | No | No | Renew at standard snapshot price. |
| SERVICE_PRICE_OVERRIDE_REQUEST | SERVICE | REQUEST_PRICE | COMPANY | Yes | No | Request non-standard service pricing. |
| SERVICE_PRICE_OVERRIDE_APPROVE | SERVICE | APPROVE_PRICE | COMPANY | Yes | Yes | Approve a non-standard service price. |
| PAYMENT_CREATE_DRAFT | PAYMENT | CREATE_DRAFT | COMPANY | Yes | No | Create draft payment/bill. |
| PAYMENT_CONFIRM | PAYMENT | CONFIRM | COMPANY | Yes | No | Confirm a valid draft payment. |
| PAYMENT_PRINT | PAYMENT | PRINT | COMPANY | Yes | No | Print a confirmed payment/bill. |
| PAYMENT_CORRECT_CONFIRMED | PAYMENT | CORRECT | COMPANY | Yes | No | Correct a confirmed payment under hard invariants. |
| RECONCILIATION_PREPARE | RECONCILIATION | PREPARE | COMPANY | Yes | No | Prepare reconciliation periods/data. |
| RECONCILIATION_CONFIRM | RECONCILIATION | CONFIRM | COMPANY | Yes | No | Confirm reconciliation. |
| CHANGE_OWNER_APPROVE | PLOT | APPROVE_OWNER | COMPANY | Yes | Yes | Approve owner change. |
| CARD_REPRINT_APPROVE | CARD | APPROVE_REPRINT | COMPANY | Yes | Yes | Approve card reprint. |
| DELEGATION_CREATE | APPROVAL | DELEGATE | COMPANY | Yes | No | Create an approval delegation request. |
| DELEGATION_ACTIVATE | APPROVAL | ACTIVATE_DELEGATION | COMPANY | Yes | No | Activate accepted delegation. |
| IMPORT_EXECUTE | IMPORT | EXECUTE | COMPANY | Yes | No | Execute authorized import. |
| IMPORT_ROLLBACK | IMPORT | ROLLBACK | COMPANY | Yes | No | Roll back import under policy/version checks. |
| SENSITIVE_EXPORT | EXPORT | EXPORT_SENSITIVE | COMPANY | Yes | Policy | Export sensitive data with purpose/audit. |
| AUDIT_VIEW | AUDIT | VIEW | GLOBAL/COMPANY | Yes | No | View audit records within granted scope. |
| WORKFLOW_VIEW | WORKFLOW | VIEW | GLOBAL/COMPANY | No | No | View workflow definitions and runtime status. |
| WORKFLOW_CONFIG_MANAGE | WORKFLOW | CONFIGURE | GLOBAL/COMPANY | Yes | No | Create/edit DRAFT workflow configuration. |
| WORKFLOW_PUBLISH | WORKFLOW | PUBLISH | GLOBAL/COMPANY | Yes | No | Publish/activate a validated workflow version. |
| WORKFLOW_BIND_PROCESS | WORKFLOW | BIND_PROCESS | GLOBAL/COMPANY | Yes | No | Bind a version to an existing process and scope. |
| WORKFLOW_REASSIGN_PENDING | WORKFLOW | REASSIGN_PENDING | COMPANY | Yes | No | Reassign a pending step with reason and audit. |
| WORKFLOW_AUDIT_VIEW | WORKFLOW | VIEW_AUDIT | GLOBAL/COMPANY | Yes | No | View workflow configuration/runtime audit. |
| ORGANIZATION_USER_MANAGE | Organization | Manage users | GLOBAL | No | No | Manage Organization Users API access in Phase 1B. |
| ORGANIZATION_DEPARTMENT_MANAGE | Organization | Manage departments | GLOBAL | No | No | Manage Organization Departments API access in Phase 1B. |
| ORGANIZATION_COMPANY_MANAGE | Organization | Manage companies | GLOBAL | No | No | Manage Organization Companies API access in Phase 1B. |
| SECURITY_ADMIN_MANAGE | SECURITY | ADMIN_MANAGE | GLOBAL | Yes | No | Manage security administration configuration (Roles, AdminGroups, Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions). |
| SECURITY_AUDIT_VIEW | SECURITY | AUDIT_VIEW | GLOBAL | Yes | No | Reserved read-only audit view permission. Endpoint enforcement deferred. |

## Baseline department permissions

| Department | Baseline ALLOW | Explicitly not baseline |
|---|---|---|
| PTKD / Kinh doanh | Basic customer view, create customer proposal, view assigned burial/service data, create/renew standard service, view company bills | Payment confirmation, price approval, customer-master update, confirmed-payment correction |
| Kế toán | View payments/reports/reconciliation periods within company | Confirmed-payment correction, user management, customer-master update |
| CNTT | Technical status and specifically assigned configuration rights | Sensitive customer/financial data is not implied by IT membership |

## Business roles

| Role | Suggested permission set | Scope |
|---|---|---|
| CASHIER | PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT | COMPANY |
| PTKD_MANAGER | SERVICE_PRICE_OVERRIDE_APPROVE, CHANGE_OWNER_APPROVE, CARD_REPRINT_APPROVE | COMPANY; approval permissions delegable |
| ACCOUNTANT_RECONCILER | RECONCILIATION_CONFIRM plus approved report/export permissions | COMPANY |
| GROUP_CUSTOMER_DATA_ADMIN | CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE, CUSTOMER_MERGE_DUPLICATE | GLOBAL |
| AUDITOR | AUDIT_VIEW plus minimum read permissions | Assigned GLOBAL/COMPANY; read-only |

## Admin groups

| Admin group | Boundary |
|---|---|
| ADMIN_SECURITY | User, role, permission, account lock/unlock, delegation activation. |
| ADMIN_CUSTOMER_DATA | Customer master and duplicate resolution. |
| ADMIN_LOCATION_DATA | Site, Zone, Block, Lot and Plot master data. |
| ADMIN_SERVICE_DATA | Service/package catalog and service data. |
| ADMIN_PAYMENT | Confirmed-payment correction. |
| ADMIN_RECONCILIATION | Reconciliation administration. |
| ADMIN_IMPORT | Import, rollback and conflict handling. |
| ADMIN_DOCUMENT | Document quarantine/versioning. |
| ADMIN_AUDIT | Audit and control reports. |
| ADMIN_SYSTEM_CONFIG | System catalogs and configuration; workflow rights still require the matching WORKFLOW permission. |
| SUPER_ADMIN | Union of Admin groups, still subject to hard invariants and immutable audit. |

## Evaluation order

```text
SOFT_ALLOW = DepartmentBaseAllow
           ∪ RoleCompanyAllow
           ∪ EffectiveIndividualAllow

EFFECTIVE_ALLOW = SOFT_ALLOW - EffectiveIndividualDeny

AUTHORIZED = UserActive
          AND HardRuleAllows(action, entity_status)
          AND permission_code IN EFFECTIVE_ALLOW
          AND DataScopeAllows(user, permission_scope, record.company_id)
```

Delegation is evaluated only while acting on a matching approval step and never expands normal entity permissions.
