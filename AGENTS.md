# PTKD ERP - Codex Working Instructions

## 1. Source of truth

Before analyzing or changing this repository, read the relevant files under `docs/business/`, especially:

- `docs/business/PTKD-Specification-v1.1.md`
- `docs/business/business-rules.md`
- `docs/business/permission-catalog.md`
- `docs/business/process-catalog.md`
- `docs/business/approval-workflow-rules.md`
- `docs/business/acceptance-criteria.md`

The Word document `docs/business/PTKD-Specification-v1.1.docx` is the released business specification. The Markdown files are the working references for implementation.

Do not invent, silently change, or broaden business requirements. When a requirement is missing, contradictory, or unclear, stop and report the issue before implementing it.

## 2. Confirmed technical direction

- Application type: internal web application.
- Backend: ASP.NET Core Web API.
- Frontend: React with TypeScript.
- Database: Microsoft SQL Server.
- API base path: `/api/v2`.
- Current environment: local development with Codex.
- Initial database: `PTKD_DEV`.
- Deployment to IIS or another production host is out of scope until explicitly requested.

## 3. Required workflow for every task

### Before coding

1. Read this file and the business documents related to the task.
2. Inspect the existing repository, database scripts, API contracts, and tests.
3. Identify the impact on:
   - business rules;
   - database schema and data migration;
   - API v2;
   - frontend;
   - authorization and company scope;
   - audit and notifications;
   - automated and manual tests.
4. Present a concise implementation plan when the user requests analysis or approval before coding.
5. Do not modify files when the task explicitly says analysis only.

### During implementation

1. Keep changes inside the approved task scope.
2. Create versioned forward and rollback SQL scripts for every schema change.
3. Implement backend, frontend, and tests required by the vertical slice.
4. Update API documentation and relevant project documentation.
5. Run the relevant build, lint, and test commands that actually exist in the repository.
6. Fix failures caused by the change before reporting completion.

### Completion report

Always report:

- summary of implemented behavior;
- files changed;
- database migration and rollback scripts;
- API endpoints or contracts changed;
- tests added or updated;
- exact build and test commands run;
- actual results, including failures;
- unresolved risks or decisions;
- manual verification steps.

Never claim that work is complete when the relevant build or tests were not run successfully. Clearly state when a command could not be run.

## 4. Mandatory architecture and coding rules

- All public application endpoints must use the `/api/v2` prefix.
- Do not expose database entities directly through API responses. Use request and response DTOs.
- Use stable business error codes and a consistent Problem Details response format.
- Derive the acting user from the authenticated context. Do not trust actor, creator, confirmer, or approver IDs supplied by the client.
- Enforce authorization and `company_id` scope in the backend. Frontend visibility is not a security control.
- Use optimistic concurrency (`rowversion`) where required by the specification.
- Use transactions for approval actions, workflow execution, payment operations, and other atomic business changes.
- Do not hard-code approver user IDs in application source code.
- Do not build administrator-entered workflow conditions as raw SQL, executable C#, or JavaScript.
- Do not add production dependencies without explaining the need and impact.
- Do not place secrets, passwords, tokens, or connection strings in Git.

## 5. Approval workflow invariants

- Administrators may configure approval workflows only for business processes already registered by development.
- Approval steps run sequentially. Only one step is active at a time.
- The requester must never approve any step of the same request, including through delegation.
- If an approver cannot be resolved, block submission and notify the requester. Do not silently route the request to an administrator.
- A returned request goes back to the requester.
- Resubmission creates a new round and preserves the previous round's history.
- Published workflow versions are immutable.
- Running requests retain their original workflow version and snapshot.
- New requests use the currently effective workflow version.
- Company-specific workflow bindings take precedence over global bindings.
- Reminders may be sent before and after a due time, but overdue steps must not auto-escalate, auto-approve, auto-reject, or auto-transfer.
- Approval actions and audit records are append-only.

## 6. Database rules

- Store SQL changes under `database/` using versioned file names.
- Every forward migration must have a corresponding rollback script unless rollback is technically unsafe; in that case, document the recovery procedure.
- Use explicitly named primary keys, foreign keys, unique constraints, default constraints, and indexes.
- Make seed scripts idempotent.
- Do not use application startup to apply unreviewed schema changes automatically.
- Do not run destructive SQL, drop objects, truncate tables, or delete business data without explicit user approval.
- Do not rewrite or remove audit history.
- Validate SQL Server transactions, concurrency, uniqueness, and company scope at the database boundary where required.

## 7. Security and privacy rules

- Mask sensitive customer data unless the effective permission allows access.
- Do not write passwords, tokens, file bytes, permanent signed URLs, or unnecessary personal data to logs or audit JSON.
- Record actor, entity, company scope, action, reason, correlation ID, and relevant before/after fields for sensitive changes.
- Treat payment, customer master, permission administration, workflow publishing, delegation, import/export, and document access as security-sensitive operations.

## 8. Testing requirements

For each implemented requirement:

- map tests to the relevant acceptance criteria code;
- add unit tests for business rules;
- add integration tests for SQL Server behavior and transactions when relevant;
- add API tests for authorization, validation, error codes, and concurrency;
- include negative tests for forbidden company access and self-approval;
- verify migration from a clean development database;
- verify rollback or the documented recovery procedure.

Do not weaken or delete an existing test merely to make a change pass unless the business rule itself was formally changed.

## 9. Scope control

- Do not implement unrelated refactoring during a feature task unless it is required for correctness.
- Do not create a new business process, form, database module, workflow resolver type, or execution handler without an approved requirement.
- Record any proposed change to the approved specification as a separate decision or change request.
- Ask for clarification only when the missing answer materially changes schema, security, financial behavior, or an irreversible action.

## 10. Current repository phase

Until the project skeleton is approved:

- documentation and architecture review are allowed;
- repository scaffolding is allowed only when explicitly requested;
- do not create production deployment configuration;
- do not configure IIS;
- do not connect to or modify production databases;
- do not assume that UAT or production infrastructure already exists.
