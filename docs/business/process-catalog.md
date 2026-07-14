# PTKD ERP - Business Process Catalog

- Version: 1.1
- Owner: DEV/BA
- Runtime table: `Business_Process_Catalog`
- Admin capability: read/select only; Admin cannot create a process code or execution handler.

## Catalog contract

Each released process must define:

| Field | Required behavior |
|---|---|
| `process_code` | Stable primary key used by API, workflow binding and audit. |
| `process_name` | Vietnamese display name. |
| `module_code` | Owning business module. |
| `entity_type` | Target entity type. |
| `approval_mode` | `REQUIRED`, `CONDITIONAL` or `NONE`. |
| `request_permission` | Permission required to submit. |
| `execution_handler_code` | DEV-owned handler invoked only after approval where applicable. |
| `condition_field_catalog` | Whitelisted fields and types Admin may use. |
| `is_active` | Only ACTIVE processes may receive new bindings/requests. |

## Released baseline processes

| process_code | Vietnamese name | Module | approval_mode | Submit permission | Execution handler code | Key condition fields | Notes |
|---|---|---|---|---|---|---|---|
| CREATE_CUSTOMER | Tạo khách hàng mới | CUSTOMER | REQUIRED | CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER_CREATE_FROM_APPROVAL | `company_id`, customer identity summary | Final execution creates Profiles, Customers and company context after duplicate check. |
| CUSTOMER_MASTER_CHANGE | Thay đổi dữ liệu khách hàng dùng chung | CUSTOMER | REQUIRED | CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER_UPDATE_FROM_APPROVAL | `company_id`, `changed_field_codes`, `is_sensitive_change` | Requires target rowversion and before/after snapshot. |
| CUSTOMER_MERGE_DUPLICATE | Gộp khách hàng trùng | CUSTOMER | REQUIRED/POLICY | CUSTOMER_MERGE_DUPLICATE | CUSTOMER_MERGE_FROM_APPROVAL | `company_count`, `service_count`, `payment_count`, `document_count` | Activation policy must be confirmed before enabling. Source history is retained. |
| CHANGE_OWNER | Thay đổi chủ sở hữu | PLOT | REQUIRED | Process-specific request permission to be seeded | CHANGE_OWNER_FROM_APPROVAL | `company_id`, `plot_id`, `change_reason_code` | Approver commonly resolved by PTKD role/permission; not hard-coded. |
| SERVICE_PRICE_OVERRIDE | Áp dụng giá dịch vụ khác giá tiêu chuẩn | SERVICE | CONDITIONAL | SERVICE_PRICE_OVERRIDE_REQUEST | SERVICE_PRICE_OVERRIDE_FROM_APPROVAL | `company_id`, `standard_price`, `requested_price`, `discount_amount`, `discount_percent`, `service_type` | Required whenever requested price differs from standard snapshot. |
| CARD_REPRINT | In lại thẻ mộ | CARD | CONDITIONAL | Process-specific request permission to be seeded | CARD_REPRINT_FROM_APPROVAL | `company_id`, `previous_print_count`, `reprint_number`, `fee_amount`, `reason_code` | No approval for first issue; workflow may apply from the second print onward. |
| IMPORT_ROLLBACK | Hoàn tác import | IMPORT | REQUIRED/POLICY | IMPORT_ROLLBACK | IMPORT_ROLLBACK_FROM_APPROVAL | `company_id`, `import_log_id`, `affected_record_count`, `has_version_conflict` | Execution must perform version/conflict checks. |
| SENSITIVE_EXPORT | Xuất dữ liệu nhạy cảm | EXPORT | REQUIRED/POLICY | SENSITIVE_EXPORT | AUTHORIZE_SENSITIVE_EXPORT | `company_id`, `export_type`, `record_count`, `purpose_code` | Audit purpose, filters and actual record count. |

## Processes that do not create approval requests

| process_code | Name | approval_mode | Rule |
|---|---|---|---|
| CONFIRM_PAYMENT | Xác nhận thanh toán thông thường | NONE | A CASHIER with PAYMENT_CONFIRM may create and confirm a valid payment; daily reconciliation is the compensating control. |
| RENEW_SERVICE_STANDARD | Gia hạn dịch vụ đúng giá tiêu chuẩn | NONE | No approval when price equals the captured standard-price snapshot. |

## Reserved process pending its functional module specification

| process_code | Name | Status | Reason |
|---|---|---|---|
| SELL_CARE_PACKAGE | Bán gói chăm sóc | RESERVED / INACTIVE | The business need is confirmed, but form fields, entity schema, execution handler and exact approval trigger require the service-sales module specification before activation. |

Codex must not implement or activate a RESERVED process by guessing missing business fields.

## Condition-field whitelist rules

1. A process exposes only fields that DEV has typed and documented.
2. Supported value types should be limited to number, money, percentage, boolean, enum, date/time and stable identifier/reference.
3. Supported operators should be a controlled catalog, for example `EQ`, `NE`, `GT`, `GTE`, `LT`, `LTE`, `IN`, `NOT_IN`, `IS_NULL`, `IS_NOT_NULL`.
4. Admin cannot enter SQL, JavaScript, method names or raw expressions.
5. Publication validates field existence, type/operator compatibility and overlap with active bindings.

## Binding resolution

```text
1. Match active COMPANY bindings for process_code + request.company_id.
2. If none match, evaluate active GLOBAL bindings.
3. Select the lowest numeric priority among matching bindings.
4. Ambiguity is a configuration error; never choose randomly.
5. If approval is required and no binding matches, block submission.
```
