# Post-Phase 1B.5 Next-Work Selection Discovery and Recommendation

## Status

PROPOSED — REQUIRES PROJECT OWNER NEXT-WORK DECISION

## Authorization Source

Reference:
- Phase 1B.5 PO closure acceptance commit:
  22040fb2767ebbb1882c061b212767a257490dc0

State:
- Phase 1B.5 Customer Merge and Duplicate Resolution is closed.
- This document is discovery and recommendation only.
- It does not authorize next-phase implementation.

## Objective

Identify and recommend the next work item after Phase 1B.5.

## Source Documents Reviewed

- docs/architecture/phase-1b5-project-owner-closure-acceptance.md
- docs/architecture/phase-1b5d-operational-validation-and-closure-acceptance-review.md
- docs/architecture/phase-1b5d-operational-validation-and-closure-report.md
- docs/architecture/phase-1b5-customer-merge-duplicate-resolution-discovery-and-detailed-plan.md
- docs/architecture/phase-1b4-project-owner-closure-acceptance.md
- docs/architecture/post-1b4-next-work-selection-discovery-and-recommendation.md
- docs/architecture/post-1b4-project-owner-next-work-decision.md
- docs/business/business-rules.md
- docs/business/acceptance-criteria.md
- docs/business/process-catalog.md
- docs/business/permission-catalog.md
- docs/decisions/phase-1b0-open-decisions.md
- docs/architecture/phase-1b0-security-discovery-decisions.md
- docs/architecture/project-readiness-review.md

Missing sources:
- PTKD-ERP-Master-Context.md: file does not exist at repository root.

## Completed Foundation Summary

### Phase 1A.2 — Application API Implementation
- .NET 10 backend with SQL Server.
- Organization management API v2 (companies, departments, users, assignments).
- React 19 SPA with Vite, Ant Design v6.
- Frontend test foundation (Vitest, RTL).
- axiosClient with baseURL /api/v2.

### Phase 1B.0 — Security Discovery Decisions
- 20 security architecture decisions approved (DEC-1B-001 through DEC-1B-021).
- Authentication, authorization, token lifecycle, permission catalog, audit controls defined.

### Phase 1B.1 — Security Admin
- JWT authentication with refresh token rotation.
- Permission evaluator (IPermissionEvaluator).
- Role/AdminGroup/IndividualPermission management.
- Security audit controls.
- First-admin provisioning.

### Phase 1B.2 — Customer First Slice
- Customer master CRUD.
- Profiles and Customers (GLOBAL).
- Customer_Company_Context (unique by customer+company).
- Duplicate CCCD checking.

### Phase 1B.3 — Workflow/Approval Foundation
- Sequential approval workflow engine.
- Workflow definitions, versions, steps, bindings.
- Approval runtime (APPROVE, REJECT, RETURN, RESUBMIT).
- Execution handler framework.
- SLA/reminder infrastructure.
- Delegation support.
- Frontend workflow admin UI and approval UI.

### Phase 1B.4 — Customer Master Expansion
- CUSTOMER_MASTER_CHANGE process with approval workflow.
- CUSTOMER_UPDATE_FROM_APPROVAL execution handler.
- V0009/U0009 migration.
- Customer change request frontend (form, list, detail).
- Target rowversion / concurrency protection.

### Phase 1B.5 — Customer Merge and Duplicate Resolution
- CUSTOMER_MERGE_DUPLICATE process with approval workflow.
- CUSTOMER_MERGE_FROM_APPROVAL execution handler (CustomerMergeExecutionHandler).
- V0010/U0010 migration.
- Customer_Merge_Requests, Customer_Merge_Request_Candidates, Customer_Merge_History.
- CustomerMergeService with merge execution logic.
- Duplicate search, merge request creation, list, detail frontend.
- Sanitized error handling.

### Current Test Baseline
- Backend: 158 unit, 196 integration, 267 API tests.
- Frontend: 53 test files, 417 tests.
- Database: V0010 is current migration ceiling. ResetToV0010 in test fixture.

## Remaining Candidate Work

### Candidate: Service Module (Service Catalog, Service Sales, Renewals)

- **Source evidence**: process-catalog.md defines RENEW_SERVICE_STANDARD (approval_mode: NONE, no approval when price equals standard snapshot) and SERVICE_PRICE_OVERRIDE (approval_mode: CONDITIONAL, required when price differs from standard snapshot). business-rules.md does not contain detailed service entity rules. acceptance-criteria.md APR-02 references standard-price renewal and SERVICE_PRICE_OVERRIDE.
- **Business value**: Core operational module. Services are the primary revenue-generating activity. Without services, payments have nothing to bill against, and care packages cannot be sold.
- **Dependencies**: Customer Master (complete), Workflow/Approval (complete for SERVICE_PRICE_OVERRIDE). Payment module is needed for billing confirmed services, but service definition/pricing can be built independently of payment processing.
- **Technical readiness**: Workflow execution handler framework is proven (CUSTOMER_UPDATE_FROM_APPROVAL, CUSTOMER_MERGE_FROM_APPROVAL). SERVICE_PRICE_OVERRIDE_FROM_APPROVAL can follow the same pattern. Service entity schema is not yet defined in the repository.
- **Missing decisions**: Service entity schema, service-type catalog, standard-price snapshot mechanism, service-to-customer-company-context linkage, service cycle definition, renewal logic. These require discovery and planning.
- **Risks**: Complex pricing rules (standard vs override). SELL_CARE_PACKAGE is RESERVED/INACTIVE pending functional module specification — must not be activated by guessing. Service-payment coupling needs careful sequencing.
- **Suitability as immediate next work**: STRONG. Service is the natural next domain after customer master is complete. It enables payment and card modules downstream. The workflow foundation supports SERVICE_PRICE_OVERRIDE approval.

### Candidate: Payment and Reconciliation

- **Source evidence**: process-catalog.md defines CONFIRM_PAYMENT (approval_mode: NONE). business-rules.md PAY-001 through PAY-012. acceptance-criteria.md PAY-01 through PAY-08.
- **Business value**: High. Enables cashiers to create, confirm, and correct bills. Daily/monthly reconciliation supports accounting.
- **Dependencies**: Customer Master (complete), Service Module (needed to bill services). PAY-008 requires service-cycle consistency — payment correction must not pay the same cycle twice. This implies service entities must exist.
- **Technical readiness**: Workflow framework supports ADMIN_PAYMENT correction flow. Payment entity schema is not yet defined. Financial stored procedures (AUTH-010) require database-level validation.
- **Missing decisions**: Payment entity schema, bill code generation, reconciliation period definition, payment-to-service-cycle linkage, correction package format, notification recipients for PAY-011.
- **Risks**: High financial and auditing risk. PAY-004 (confirmed payment immutability) and PAY-006 (hard invariants on correction) require database-level enforcement. Incorrect implementation could cause financial data loss.
- **Suitability as immediate next work**: MODERATE. Logically depends on Service Module for service-cycle billing. Could be started in parallel for payment-only schema, but full billing integration requires services.

### Candidate: Change Owner (Plot Ownership Transfer)

- **Source evidence**: process-catalog.md defines CHANGE_OWNER (approval_mode: REQUIRED). Requires company_id, plot_id, change_reason_code. Handler: CHANGE_OWNER_FROM_APPROVAL.
- **Business value**: Moderate. Enables ownership transfer of cemetery plots. Important for operational completeness but not a dependency for other modules.
- **Dependencies**: Customer Master (complete), Workflow/Approval (complete). Plot/Site/Zone/Block entity schema needed. DATA-008 references Site-Company scope inheritance.
- **Technical readiness**: Workflow execution handler framework supports this pattern. Plot/Site entity schema is not yet defined.
- **Missing decisions**: Plot entity schema, site-company-zone-block-lot-plot hierarchy, ownership transfer rules, what happens to services/payments on transferred plots.
- **Risks**: Moderate. Plot hierarchy introduces new entity domain. Ownership transfer interacts with services and payments if they exist.
- **Suitability as immediate next work**: LOW. Requires plot entity schema which is not yet in the repository. Less business-critical than services or payments.

### Candidate: Card / Printing Flow (Card Reprint)

- **Source evidence**: process-catalog.md defines CARD_REPRINT (approval_mode: CONDITIONAL). No approval for first issue; workflow from second print. Handler: CARD_REPRINT_FROM_APPROVAL.
- **Business value**: Low-moderate. Operational convenience for reprinting service cards.
- **Dependencies**: Service Module (cards are printed for services).
- **Technical readiness**: Workflow framework supports conditional approval. Card entity schema not defined.
- **Missing decisions**: Card entity definition, print tracking, fee handling for reprints.
- **Risks**: Low. Straightforward workflow-driven process.
- **Suitability as immediate next work**: LOW. Depends on Service Module.

### Candidate: Import Rollback

- **Source evidence**: process-catalog.md defines IMPORT_ROLLBACK (approval_mode: REQUIRED/POLICY). Handler: IMPORT_ROLLBACK_FROM_APPROVAL. Condition fields include affected_record_count and has_version_conflict.
- **Business value**: Low. Operational safety net for data imports.
- **Dependencies**: Import infrastructure not yet defined.
- **Technical readiness**: Workflow framework supports this. Import entity schema not defined.
- **Missing decisions**: Import log schema, rollback scope, version conflict resolution.
- **Risks**: Moderate complexity for version/conflict checks.
- **Suitability as immediate next work**: LOW. Import infrastructure prerequisite not present.

### Candidate: Sensitive Export

- **Source evidence**: process-catalog.md defines SENSITIVE_EXPORT (approval_mode: REQUIRED/POLICY). Handler: AUTHORIZE_SENSITIVE_EXPORT. SEC-006 requires export logging.
- **Business value**: Low-moderate. Compliance/governance requirement.
- **Dependencies**: Core data modules (customers, services, payments) should exist before export controls are meaningful.
- **Technical readiness**: Workflow framework supports approval. Export infrastructure not defined.
- **Missing decisions**: Export types, record selection, purpose codes, format options.
- **Risks**: Low technical risk. Primarily governance/compliance design.
- **Suitability as immediate next work**: LOW. More useful after transactional modules exist.

### Candidate: Production Release Readiness

- **Source evidence**: No release tag or push has been authorized. All phases remain on feature/phase-1-organization branch.
- **Business value**: Enables real users to access the system.
- **Dependencies**: Sufficient functional scope must be complete. Currently: security, customer master, workflow, customer merge are complete but no transactional modules (services, payments).
- **Technical readiness**: CI/CD, deployment pipeline, environment configuration, data migration strategy are not documented in the repository.
- **Missing decisions**: Go-live gate criteria (acceptance-criteria.md requires all AUTH/CUS/PAY/APR/DEL/SEC/WFC criteria to pass). PAY criteria are not yet implemented.
- **Risks**: High. Production deployment without services or payments delivers limited business value.
- **Suitability as immediate next work**: NOT RECOMMENDED. Go-live is blocked until PAY criteria pass per acceptance-criteria.md.

## Recommendation

### Primary Recommendation

**Phase 1B.6 Service Module Foundation (Service Catalog, Standard Pricing, Service Sales)**

Rationale:

1. **Dependency chain**: Services are the prerequisite for Payment (PAY-008 requires service-cycle consistency), Card Reprint (cards are service artifacts), and Care Package Sales (SELL_CARE_PACKAGE). Building services next unblocks the largest number of downstream modules.

2. **Completed foundation enables it**: Customer Master (GLOBAL customers, Customer_Company_Context), Workflow/Approval (sequential approval with execution handlers), and Customer Merge (clean customer data) provide the necessary foundation. SERVICE_PRICE_OVERRIDE can reuse the proven execution handler pattern.

3. **Business value**: Services are the primary revenue-generating domain for PTKD. Without service definitions, the system cannot track what customers have purchased, when renewals are due, or what prices apply. This is the core operational gap.

4. **Manageable scope**: The service module can be phased:
   - 1B.6-A: Service entity schema, service-type catalog, standard pricing (no approval required for standard renewals per RENEW_SERVICE_STANDARD).
   - 1B.6-B: SERVICE_PRICE_OVERRIDE approval workflow integration.
   - Later: SELL_CARE_PACKAGE (currently RESERVED/INACTIVE — must not be activated without functional specification).

5. **Process catalog support**: RENEW_SERVICE_STANDARD and SERVICE_PRICE_OVERRIDE are documented with approval modes, permissions, and condition fields. This provides a clear specification basis.

Expected scope boundary:
- Service entity and service-type catalog design.
- Standard pricing snapshot mechanism.
- Service-to-customer-company-context linkage.
- Service renewal logic (standard price path).
- SERVICE_PRICE_OVERRIDE approval workflow binding.
- SERVICE_PRICE_OVERRIDE_FROM_APPROVAL execution handler.
- Service frontend (list, detail, renewal form).
- SELL_CARE_PACKAGE remains RESERVED/INACTIVE.

Expected first subphase: discovery and detailed planning only.

### Alternatives Considered

1. **Payment and Reconciliation**: Not recommended first because PAY-008 requires service-cycle consistency. Building payment before services would require placeholder service references or would need to be reworked when services are added. The dependency order (Services → Payment) is clearer than (Payment → Services).

2. **Change Owner (Plot Transfer)**: Not recommended first because it introduces a new entity domain (plots/sites) that is not a prerequisite for services or payments. Lower business urgency.

3. **Card Reprint**: Not recommended first because it depends on services.

4. **Production Release**: Not recommended because go-live is blocked until PAY criteria pass per acceptance-criteria.md.

## Proposed Next Gate

Project Owner next-work decision.

## Recommended Authorization Wording

If the recommendation is accepted:

Authorized next task:
Phase 1B.6 Service Module Foundation discovery and detailed planning only.

Implementation requires separate Project Owner scope acceptance.

Do not authorize:
- implementation,
- production migration,
- release tag,
- push.

## Risks / Open Questions

1. **Service entity schema**: Not yet defined in the repository. Discovery must define service types, pricing model, cycle definitions, and customer-company linkage.

2. **SELL_CARE_PACKAGE**: RESERVED/INACTIVE in process-catalog.md. Must not be activated by guessing missing business fields. Requires a separate functional module specification before activation.

3. **Service-to-payment coupling**: The boundary between service creation/renewal and payment billing needs clear definition. Services can be built independently of payment processing, but the interface must be designed for future payment integration.

4. **Standard-price snapshot mechanism**: APR-02 requires that price differing from snapshot triggers SERVICE_PRICE_OVERRIDE. The snapshot capture timing and storage need design.

5. **Site-company scope inheritance**: DATA-008 states site determines company scope inherited through Zone/Block/Lot/Plot. If services are linked to plots/locations, this hierarchy may be needed. Discovery should clarify whether services are purely customer-company scoped or also location-scoped.

6. **Missing PTKD-ERP-Master-Context.md**: This file was referenced but does not exist at the repository root. Discovery should proceed using available business-rules.md, acceptance-criteria.md, and process-catalog.md as primary sources.

## Non-Goals

This document does not:
- modify business rules,
- decide the next phase,
- authorize implementation,
- authorize production migration,
- authorize release tag,
- authorize push.
