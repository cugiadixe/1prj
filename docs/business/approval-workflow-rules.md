# PTKD ERP - Approval Workflow Rules

- Version: 1.1
- Scope: dynamic workflow configuration for DEV-published processes
- Workflow mode: sequential only

## 1. Design-time versus runtime

| Layer | Main objects | Rule |
|---|---|---|
| Design-time | Business_Process_Catalog, Approval_Workflows, Approval_Workflow_Versions, Approval_Workflow_Steps, Approval_Step_Approver_Rules, Approval_Step_Conditions, Approval_Workflow_Bindings, Approval_Reminder_Policies | Only DRAFT configuration is editable. Published/active versions are immutable and audited. |
| Runtime | Approval_Requests, Approval_Request_Steps, Approval_Request_Step_Assignees, Approval_Actions, Approval_Reminder_Logs | Runtime is created from a snapshot and is not changed by later configuration releases. |

Runtime tables must never become the source configuration for a future request.

## 2. Admin boundaries

Admin may:

- Create a workflow identity and DRAFT version.
- Add, remove and reorder sequential steps.
- Select one supported approver rule per configured rule entry.
- Configure whitelisted conditions.
- Configure SLA/reminder policies.
- Bind a validated version to an existing process at GLOBAL or COMPANY scope.
- Retire a version for new requests while retaining history.

Admin may not:

- Create a process/form/table or execution handler.
- Add an unregistered condition field/operator/resolver.
- Enter SQL, JavaScript or arbitrary expression text.
- Edit or delete a PUBLISHED/ACTIVE version.
- Migrate a running request to a new version.

## 3. Version lifecycle

```text
DRAFT -> PUBLISHED -> ACTIVE -> RETIRED
```

| Status | Allowed | Prohibited |
|---|---|---|
| DRAFT | Edit steps/rules/conditions/reminders; validate; delete if never used | Receive new requests |
| PUBLISHED | Set/prepare effective time and binding | Structural edit/delete |
| ACTIVE | Receive new requests through active bindings | Structural edit; mutate old request snapshots |
| RETIRED | Serve history and old running requests | Receive new requests |

A structural change clones the latest version into a new DRAFT.

## 4. Scope and binding precedence

| scope_type | company_id | Precedence | Meaning |
|---|---|---:|---|
| COMPANY | Required | 1 | Applies only to the selected company and overrides GLOBAL. |
| GLOBAL | NULL | 2 | Applies across the Tổng công ty/Tập đoàn tenant. |

Publication must reject overlapping bindings for the same process/scope/effective period/condition/priority. If ambiguity still reaches runtime, block submission and log configuration error.

## 5. Sequential step model

1. On submit, step 1 is `PENDING`; later steps are `WAITING`.
2. At most one step per request round may be `PENDING`.
3. Approval of the current step and activation of the next step occur in one transaction.
4. A step may have multiple resolved assignees, but one successful valid action closes it.
5. `rowversion`/atomic update prevents double action; later attempts return conflict.
6. The requester cannot be a valid assignee/actor for the same request.

### Runtime step states

- `WAITING`: future step.
- `PENDING`: current actionable step.
- `APPROVED`: approved.
- `REJECTED`: rejected and request terminated.
- `RETURNED`: returned to requester.
- `CANCELLED`: unused future step closed by return/reject/withdraw.
- `EXPIRED`: reserved status where an explicit policy uses request expiry; reminder overdue alone does not expire a step.

## 6. Approver resolution

| Rule type | Resolution | Minimum configuration |
|---|---|---|
| SPECIFIC_USER | One explicit active user | `user_id` |
| ROLE | Active users with role in request scope | `role_code` |
| DEPARTMENT | Active users in a department | `department_id` |
| DEPARTMENT_MANAGER | Effective manager of configured/requester department | `department_id` or requester-department source |
| REQUESTER_MANAGER | Effective direct manager of requester | management relationship |
| PERMISSION | Active users with effective permission in company | `permission_code` |
| ADMIN_GROUP | Active members of Admin group in scope | `admin_group_code` |
| DATA_FIELD_USER | User referenced by an approved business payload field | whitelisted `field_code` |

Resolution rules:

1. Resolve all steps before creating the official request.
2. Filter inactive/locked users, invalid company scope and the requester.
3. Store all resolved candidates in `Approval_Request_Step_Assignees`.
4. If any step has no valid assignee, do not create the request and return `APPROVAL_APPROVER_NOT_RESOLVED` with step number/name.
5. Notify the requester and workflow Admin of the configuration failure without leaking unnecessary sensitive payload data.

## 7. Submit algorithm

```text
Validate process is ACTIVE
-> validate requester permission/company scope
-> select COMPANY binding, else GLOBAL
-> evaluate whitelisted conditions
-> load immutable workflow version
-> resolve every step and assignee
-> reject self-approval/no-assignee/overlap errors
-> calculate workflow snapshot + SHA-256 hash
-> create request, round 1, steps, assignees and SUBMIT action atomically
-> notify step 1 assignees after commit
```

## 8. Return and resubmit

RETURN behavior:

- Request becomes `RETURNED`.
- Current step becomes `RETURNED`.
- Future `WAITING` steps become `CANCELLED`.
- The requester receives the reason and may edit the payload.

RESUBMIT behavior:

- Increment `round_no`.
- Preserve all prior steps/actions.
- Re-run permission, condition and assignee validation.
- Create a new set of runtime steps/assignees.
- Retain the original `workflow_version_id` and version structure.

To use a newer workflow version, withdraw the old request and create a new business request.

## 9. Immutable runtime snapshot

`Approval_Requests` stores at minimum:

- `workflow_id`
- `workflow_version_id`
- `workflow_binding_id`
- `workflow_snapshot`
- `workflow_snapshot_hash`
- `payload_hash`
- `current_round_no`
- `current_step_no`
- request status and execution status
- `correlation_id`

The snapshot includes version identity, scope, step order, approver rules, resolved assignees, conditions and reminder settings needed to explain the running request.

## 10. SLA and reminders

A step may define:

- due duration
- reminder before due
- reminder at due
- repeat interval after overdue

Rules:

- Overdue does not reassign or escalate.
- Overdue does not approve, reject, skip or expire automatically.
- Step remains `PENDING`, with `is_overdue=1`.
- The reminder job is idempotent and records a unique/deduplicated log entry.
- Notification is sent after the transaction that creates the reminder event/log commits.

## 11. Delegation

1. The original approver creates a delegation for a delegable approval permission, company and period.
2. Delegate accepts; status becomes `PENDING_ADMIN`.
3. Admin activates; future start becomes `SCHEDULED`, otherwise `ACTIVE`.
4. While ACTIVE, primary approver and delegate may see matching steps; first valid action wins.
5. Delegation grants only step action, never general entity edit/admin rights.
6. No delegation chaining.
7. Delegated actions record `acted_by`, `on_behalf_of`, `delegation_id`.
8. Expiry automatically removes the additive right without altering completed history or the primary approver's rights.

## 12. Approval versus execution

Approval completion and business execution are separate:

```text
Request status: APPROVED
Execution status: NOT_EXECUTED -> EXECUTING -> EXECUTED | FAILED
```

Execution rules:

- Use the approved payload and matching payload hash.
- Recheck target rowversion where applicable.
- Execute idempotently using correlation/idempotency keys.
- Never execute twice after a timeout/retry.
- Record execution failure and allow controlled retry without re-approval if payload is unchanged.

## 13. Required error codes

| Code | Meaning |
|---|---|
| WORKFLOW_PROCESS_NOT_SUPPORTED | Process is missing/inactive or not released for workflow. |
| WORKFLOW_BINDING_NOT_FOUND | Required approval has no matching active binding. |
| WORKFLOW_BINDING_AMBIGUOUS | More than one equally applicable binding. |
| WORKFLOW_VERSION_IMMUTABLE | Attempt to edit/delete published/active version. |
| WORKFLOW_INVALID_CONDITION | Field/operator/type is not in the process whitelist. |
| APPROVAL_APPROVER_NOT_RESOLVED | A step has no valid assignee. |
| APPROVAL_SELF_ACTION | Requester attempted to act or is the only candidate. |
| APPROVAL_STEP_ALREADY_ACTED | Concurrent/later action on a closed step. |
| APPROVAL_TARGET_VERSION_CONFLICT | Business entity changed after request snapshot. |
| DELEGATION_NOT_ACTIVE | Delegation is not currently effective. |
| DELEGATION_ADMIN_REQUIRED | Accepted delegation is awaiting Admin activation. |

## 14. Audit requirements

Audit all workflow creation, draft edits, validation, publish, activation, retirement, binding changes, submit, assignment, reassignment, approve, reject, return, resubmit, withdraw, reminder, delegation event and execution attempt.

Audit/actions are append-only and include actor, acting-as, request/step/version/binding IDs, company scope, from/to status, reason/note, correlation ID and time.
