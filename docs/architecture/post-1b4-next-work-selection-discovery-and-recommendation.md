# Post-Phase 1B.4 Next-Work Selection Discovery and Recommendation

## Status

PROPOSED — REQUIRES PROJECT OWNER NEXT-WORK DECISION

## Authorization Source

Reference:
- Phase 1B.4 closure acceptance commit:
  fb0635868e72fa2eaeeaa4a1870af917682d9e49

State:
- Phase 1B.4 Customer Master Expansion is closed.
- This document is discovery/recommendation only.
- No implementation is authorized by this document.

## Closed Phase Summary

Summarize Phase 1B.4 outcome:
- backend/data accepted: Completed with V0009/U0009 migration, CustomerMasterChange execution handler, and core API v2 endpoints.
- frontend accepted: Completed with customer change request form, my requests, detail page, and route navigation.
- operational validation accepted: Completed automatically testing concurrency errors, idempotency, and data safety.
- closure accepted: Project Owner closure confirmed, unlocking next phase discovery.

## Candidate Next Work Options

1. **Customer Merge / Duplicate Resolution**
   - **Evidence/Source:** `process-catalog.md` (`CUSTOMER_MERGE_DUPLICATE`), `business-rules.md` (CUS-007).
   - **Business Value:** Provides a safe, auditable pathway to resolve duplicate customer records without orphanizing or losing service, payment, and document history.
   - **Dependencies:** Requires Phase 1B.4 (Customer Master Expansion) which is now complete.
   - **Scope Outline:** `CUSTOMER_MERGE_FROM_APPROVAL` execution handler, UI for merging duplicates, preview logic for affected contexts, and retention of source history.
   - **Risks:** High risk of foreign key violations or data loss if relational data (payments/services) is not correctly reassigned to the target customer.
   - **Blockers/Open Decisions:** Need to confirm whether duplicate merge is purely manual via approval or if automatic deduplication suggestions are required.
   - **Recommendation Status:** Highly Recommended as the immediate follow-up to Phase 1B.4.

2. **Payment and Reconciliation (Payment Module)**
   - **Evidence/Source:** `process-catalog.md` (`CONFIRM_PAYMENT`), `acceptance-criteria.md` (PAY-01 to PAY-08).
   - **Business Value:** Enables authorized cashiers to create, confirm, and correct bills, and supports daily reconciliation.
   - **Dependencies:** Customer Master.
   - **Scope Outline:** Billing schemas, cashier roles, confirmation workflow, and correction flow (`ADMIN_PAYMENT`).
   - **Risks:** High financial and auditing risk.
   - **Blockers/Open Decisions:** None blocking, but logically follows a finalized customer domain.
   - **Recommendation Status:** Deferred.

3. **Service Module**
   - **Evidence/Source:** `process-catalog.md` (`RENEW_SERVICE_STANDARD`, `SERVICE_PRICE_OVERRIDE`, `SELL_CARE_PACKAGE`).
   - **Business Value:** Core operational module for selling and managing services.
   - **Dependencies:** Customer Master, Payment Module.
   - **Scope Outline:** Service price snapshots, overrides, and renewals.
   - **Risks:** Complex business rules regarding standard vs. override pricing.
   - **Blockers/Open Decisions:** `SELL_CARE_PACKAGE` is currently RESERVED / INACTIVE pending a functional module specification.
   - **Recommendation Status:** Deferred.

4. **Card / Printing Flow**
   - **Evidence/Source:** `process-catalog.md` (`CARD_REPRINT`).
   - **Business Value:** Operational printing of service cards.
   - **Dependencies:** Service Module.
   - **Scope Outline:** `CARD_REPRINT_FROM_APPROVAL` handler and print tracking.
   - **Blockers/Open Decisions:** Needs Service Module first.
   - **Recommendation Status:** Deferred.

## Recommended Next Work

Recommended for Project Owner selection:
**Phase 1B.5 Customer Merge and Duplicate Resolution**

## Rationale

Customer Merge / Duplicate Resolution naturally completes the Customer Master domain. With Phase 1B.4 establishing the foundation for change requests, addressing duplicates is the next most critical data integrity capability before moving on to transactional modules like Payments or Services, which will rely heavily on a clean and unified customer identifier.

## Deferred Candidates

- **Payment and Reconciliation:** Deferred because the customer master should be fully robust (including duplicate handling) before linking irreversible financial transactions.
- **Service Module:** Deferred due to pending module specifications (`SELL_CARE_PACKAGE`) and its reliance on the Payment Module.
- **Card / Printing Flow:** Deferred as it relies on the Service Module being active.

## Required Project Owner Decision

The Project Owner must select the next work item before any implementation, planning, or coding begins.

## Proposed Authorization Wording

Recommended next authorized task:
Phase 1B.5 Customer Merge and Duplicate Resolution discovery and detailed planning only.

Implementation requires separate Project Owner approval after discovery and detailed planning are reviewed.

## Boundaries

State:
- no implementation authorized,
- no production migration,
- no release tag,
- no push,
- no business requirement changes,
- no source/test changes.

## Risks / Open Questions

- We must clarify if cross-company customer merges are permitted or if merges are strictly restricted within a single `company_id`.
- We need to outline exactly how `Customer_Company_Context` is handled when two customers being merged both have contexts in the same company.
