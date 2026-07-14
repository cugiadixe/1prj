# PTKD ERP - Implementation Roadmap v1.0

This roadmap outlines the phased implementation plan for the PTKD ERP system, correcting previous assumptions by establishing the dynamic workflow engine before dependent business modules like Customer Master and Payment.

## Phase 0: Foundation
- **Goals:** Set up the project skeleton, database, and basic development infrastructure.
- **Features included:** Repository scaffolding, basic API setup, frontend Vite setup, database migration runner, core shared utilities.
- **Features excluded:** Any business logic, UI screens, authentication.
- **Dependencies:** None.
- **Deliverables:** Working backend `dotnet run` returning a health check, working frontend `npm run dev`, base SQL script structure.
- **Entry criteria:** Approved technical decisions and architecture documents.
- **Exit criteria:** Both backend and frontend run locally without errors; database `PTKD_DEV` can be created from scripts.
- **Risks:** Setup complexity with custom SQL migration scripts rather than EF Migrations.
- **Suggested task IDs:** TSK-001 (DB Setup), TSK-002 (API Setup), TSK-003 (Frontend Setup).

## Phase 1: Organization and authorization
- **Goals:** Implement the permission engine, user authentication, and organizational structure (Departments, Roles).
- **Features included:** JWT Authentication, Permission evaluation (Department, Role, Individual), Company scope context, API authorization middleware, UI protected routes.
- **Features excluded:** Active Directory integration, Approval delegations (handled in Phase 2).
- **Dependencies:** Phase 0.
- **Deliverables:** Auth API, Permission evaluation engine, User/Role management UI (basic).
- **Entry criteria:** Phase 0 complete.
- **Exit criteria:** Users can log in, receive a JWT, and access endpoints based on effective permissions (allow/deny rules).
- **Risks:** Complexity of combining role permissions and individual deny rules.
- **Suggested task IDs:** TSK-101 (Auth endpoints), TSK-102 (Permission engine), TSK-103 (Role/User schema & seeds).

## Phase 2: Dynamic workflow design-time and runtime
- **Goals:** Build the core dynamic approval workflow engine to support all subsequent business processes.
- **Features included:** Workflow design-time (Versions, Steps, Rules, Conditions, Bindings), Workflow runtime (Requests, Snapshot, Step Assignees, Actions), Delegation management, Reminder policies.
- **Features excluded:** Specific business execution handlers (these belong to their respective phases).
- **Dependencies:** Phase 1 (requires Users, Roles, Permissions for approver resolution).
- **Deliverables:** Workflow configuration API, Workflow runtime API, Admin UI for workflow binding.
- **Entry criteria:** Phase 1 complete, Auth working.
- **Exit criteria:** Admin can create and publish a workflow version and bind it to a process code.
- **Risks:** Concurrency issues with `rowversion` during simultaneous approvals; complex approver resolution logic.
- **Suggested task IDs:** TSK-201 (Workflow schema), TSK-202 (Design-time API), TSK-203 (Approver resolution engine), TSK-204 (Delegation API).

## Phase 3: CARD_REPRINT pilot
- **Goals:** Validate the dynamic workflow engine with a simple, isolated business process before tackling complex modules.
- **Features included:** `CARD_REPRINT` business process, execution handler, basic form UI, approval request submission.
- **Features excluded:** Printing integrations, physical hardware integration.
- **Dependencies:** Phase 2.
- **Deliverables:** Working end-to-end flow for requesting, approving, and executing a card reprint.
- **Entry criteria:** Phase 2 workflow runtime is functional.
- **Exit criteria:** A user can submit a CARD_REPRINT request, an approver can approve it, and the execution handler marks it as EXECUTED.
- **Risks:** Integration gaps between the generic workflow engine and business-specific execution handlers.
- **Suggested task IDs:** TSK-301 (Card Reprint schema & UI), TSK-302 (Card Reprint execution handler).

## Phase 4: Customer master
- **Goals:** Implement shared global customer master data and company-specific contexts.
- **Features included:** Profiles, Customers, Customer_Company_Context, `CREATE_CUSTOMER` workflow integration, `CUSTOMER_MASTER_CHANGE` workflow integration, Duplicate checking, Customer Merge (by Admin).
- **Features excluded:** Financial spending aggregates (built in Phase 6).
- **Dependencies:** Phase 2 (for workflow) and Phase 3 (pilot proven).
- **Deliverables:** Customer search UI, Customer profile view, Create/Update request forms, Merge duplicate UI for Admins.
- **Entry criteria:** Workflow engine is stable.
- **Exit criteria:** Staff can propose customer changes; Data Admins can finalize creation and updates via the approval workflow.
- **Risks:** Complex merge logic retaining historical records; duplicate checking performance.
- **Suggested task IDs:** TSK-401 (Customer Schema), TSK-402 (Duplicate Detection), TSK-403 (Create/Change Handlers), TSK-404 (Admin Merge).

## Phase 5: Services
- **Goals:** Manage customer care services, standard renewals, and price overrides.
- **Features included:** Service catalog, `SERVICE_CREATE_STANDARD`, `SERVICE_RENEW_STANDARD`, `SERVICE_PRICE_OVERRIDE` workflow integration.
- **Features excluded:** `SELL_CARE_PACKAGE` (reserved).
- **Dependencies:** Phase 4 (Services depend on Customer Master).
- **Deliverables:** Service management API and UI, Price override approval flow.
- **Entry criteria:** Customer Master is functional.
- **Exit criteria:** Users can create standard services directly or request price overrides through the workflow.
- **Risks:** Ensuring correct snapshots of standard prices during override requests.
- **Suggested task IDs:** TSK-501 (Service Catalog), TSK-502 (Standard Service APIs), TSK-503 (Price Override flow).

## Phase 6: Payment and reconciliation
- **Goals:** Implement robust financial transaction handling, bill confirmation, and admin corrections.
- **Features included:** Payment Drafts, `PAYMENT_CONFIRM` (CASHIER self-confirm), `PAYMENT_CORRECT_CONFIRMED` (Admin correction with cascading reconciliation updates), Daily/Monthly Reconciliation.
- **Features excluded:** Cancellations, partial payments, external payment gateways.
- **Dependencies:** Phase 5 (Payments depend on Services and Customers).
- **Deliverables:** Cashier payment UI, Admin payment correction API/UI, Reconciliation generation.
- **Entry criteria:** Customers and Services are active.
- **Exit criteria:** Cashiers can confirm bills; Admins can securely correct them with audit trails and notifications; Reconciliations balance correctly.
- **Risks:** Extreme complexity in rolling back and recalculating reconciliation periods during payment corrections.
- **Suggested task IDs:** TSK-601 (Payment Schema), TSK-602 (Cashier Draft & Confirm), TSK-603 (Admin Correction SPs), TSK-604 (Reconciliation).

## Phase 7: Reporting, audit and hardening
- **Goals:** Finalize system observability, sensitive data protection, and performance.
- **Features included:** System Audit Logs viewer, Data Masking for sensitive fields, Sensitive Export workflows (`SENSITIVE_EXPORT`), Notifications.
- **Features excluded:** Complex BI dashboards.
- **Dependencies:** All previous phases.
- **Deliverables:** Audit UI, Notification system, Export handlers.
- **Entry criteria:** Core business processes are complete.
- **Exit criteria:** All sensitive actions are audited and immutable; UAT signs off on security and data masking.
- **Risks:** Performance of querying large audit tables; missing audit trails on edge cases.
- **Suggested task IDs:** TSK-701 (Audit UI), TSK-702 (Data Masking Implementation), TSK-703 (Notifications Engine), TSK-704 (Sensitive Export).
