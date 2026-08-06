# Phase 1B.2 Customer Module First Slice Completion Review

**Status:**
PHASE 1B.2 CUSTOMER MODULE FIRST SLICE ACCEPTED AS FUNCTIONALLY COMPLETE — SEE phase-1b2-customer-module-first-slice-project-owner-completion-acceptance.md

**Review scope:**
Phase 1B.2 — Customer Module First Slice (B1 + B2)

**Review baseline:**
3200d2c92403af94f01d1690b8a03777ad5bb27c

**Branch:**
feature/phase-1-organization

**Date:**
2026-07-31

---

## 1. Phase summary

Phase 1B.2 delivered the Customer Module first slice across two sub-phases:

| Sub-phase | Scope | Final acceptance commit |
|-----------|-------|------------------------|
| B1 — Customer Backend Foundation | Database, API v2, backend logic | 8881ebe264916ee91818b196489c508263e7288e |
| B2 — Customer Frontend UI | React pages, API client, permission gates | 3b743877f81bf5d155dcb1542d9a3d94f03d8af2 |

Both sub-phases completed the full lifecycle: Plan → PO Plan Acceptance → Implementation → Implementation Acceptance Review → PO Implementation Acceptance → Closure Review → PO Final Acceptance.

---

## 2. Completed scope

### 2.1 Backend (B1)

- Database migration V0005 — Profiles, Customers, Customer_Company_Contexts tables with bigint IDENTITY PKs, rowversion, filtered unique index on CCCD.
- Rollback migration U0005.
- 8 API v2 endpoints in CustomersController: search, get-by-id, create, update, company-contexts (list, create, update), duplicate-check.
- `[RequirePermission]` attribute enforcement on all endpoints.
- Atomic create: Profile + Customer + Customer_Company_Context in Serializable transaction.
- Admin update with required reason, targetVersion/rowVersion concurrency check, field-level before/after SecurityAuditEventRecord.
- Sensitive projection/masking via CUSTOMER_VIEW_SENSITIVE permission.
- Duplicate detection on CCCD (filtered unique constraint) and phone (signal only).
- BusinessRuleValidationException for domain errors with typed error codes.

### 2.2 Frontend (B2)

- 4 pages: CustomersPage (list/search), CustomerDetailPage, CustomerCreatePage, CustomerEditPage.
- 4 routes: /customers, /customers/new, /customers/:customerId, /customers/:customerId/edit.
- Customer API client (customersApi.ts) — 8 functions matching backend endpoints.
- TypeScript interfaces (types.ts) mirroring backend DTOs.
- Error handling helpers (errorMessages.ts) with typed error code mapping.
- Permission gates (UX/navigation only, backend authoritative):
  - CUSTOMER_VIEW_BASIC → menu, routes, list, basic detail.
  - CUSTOMER_VIEW_SENSITIVE → sensitive display (backend-driven).
  - CUSTOMER_CREATE_FINAL → create button, add context button.
  - CUSTOMER_MASTER_UPDATE → edit button.
- Sensitive masking display — backend returns masked values, frontend shows with indicator.
- Duplicate warning on CCCD/phone blur — informational only, no merge action.
- rowVersion/concurrency — 409 shows refresh prompt, no silent overwrite.
- Sanitized error handling — no backend internals exposed.

### 2.3 Modified existing files

- App.tsx — 4 customer routes added.
- AuthenticatedShell.tsx — Customer menu item gated by CUSTOMER_VIEW_BASIC.
- AuthenticatedShell.test.tsx — 2 tests for menu visibility.

---

## 3. Business rule coverage

| Rule ID | Rule summary | Coverage | Notes |
|---------|-------------|----------|-------|
| CUS-001 | Ordinary staff cannot directly edit sensitive fields | Covered | Backend enforces CUSTOMER_MASTER_UPDATE; frontend gates edit action |
| CUS-002 | Staff submit CREATE_CUSTOMER/CHANGE requests via workflow | Deferred | Requires workflow/approval runtime — not in first slice |
| CUS-003 | Only admin group may create/update/merge as final operation | Partial | Direct admin create/update implemented; workflow-mediated path deferred |
| CUS-004 | Admin correction requires reason + before/after audit | Covered | Backend requires reason, creates SecurityAuditEventRecord with field diffs |
| CUS-005 | Duplicate check before submit and before execution | Covered | Frontend triggers on blur; backend enforces filtered unique CCCD constraint |
| CUS-006 | Active CCCD filtered unique; phone is signal only | Covered | Database filtered unique index on CCCD; phone used for duplicate warning |
| CUS-007 | Customer merge with preview and history | Deferred | Not in first slice scope |
| CUS-008 | Customer execution is transactional with Company Context | Covered | Atomic Serializable transaction creates Profile + Customer + Context |
| CUS-009 | target_version recheck; no silent overwrite | Covered | Backend checks rowVersion; frontend handles 409 with refresh |
| DATA-001 | Customers are GLOBAL, not per-company | Covered | Customer permissions use PermissionScope.Global |
| DATA-002 | Company-specific info in Customer_Company_Context | Covered | Separate table and endpoints |
| DATA-005 | GLOBAL does not auto-grant sensitive access | Covered | Separate CUSTOMER_VIEW_SENSITIVE permission |
| AUTH-009 | Every endpoint re-checks permission server-side | Covered | `[RequirePermission]` on all endpoints; frontend gates are UX only |

---

## 4. Permission coverage

| Permission | Scope | Backend | Frontend | Status |
|-----------|-------|---------|----------|--------|
| CUSTOMER_VIEW_BASIC | GLOBAL | RequirePermission on search/get | Menu, route, list gate | Complete |
| CUSTOMER_VIEW_SENSITIVE | GLOBAL | Sensitive field projection | Mask indicator display | Complete |
| CUSTOMER_CREATE_FINAL | GLOBAL | RequirePermission on create | Create button gate | Complete |
| CUSTOMER_MASTER_UPDATE | GLOBAL | RequirePermission on update | Edit button gate | Complete |

All permissions existed in PermissionCodes.cs prior to Phase 1B.2. No new permission codes were added. permission-catalog.md was not modified.

---

## 5. Test evidence

### B1 — Backend

| Suite | Result |
|-------|--------|
| dotnet build | 0 errors |
| Unit tests | 133 passed |
| Integration tests | 196 passed |
| API tests | 257 passed |
| Migration/rollback | V0005/U0005 verified |

### B2 — Frontend

| Suite | Result |
|-------|--------|
| oxlint | Passed (3 pre-existing warnings only) |
| tsc -b --noEmit | 0 errors |
| vitest run | 25 files, 222 tests passed |

No backend changes in B2; dotnet build skipped for B2.

---

## 6. Acceptance criteria traceability

| Criterion | Status | Evidence |
|-----------|--------|----------|
| AUTH-05 — GLOBAL customer master searchable; sensitive masked | Met | Backend projection + frontend mask indicator |
| CUS-01 — Ordinary staff cannot edit sensitive fields | Met | CUSTOMER_MASTER_UPDATE gate on edit |
| CUS-02 — Staff submit via workflow | Deferred | Workflow runtime not in scope |
| CUS-03 — Duplicate check blocks on active CCCD | Met | Filtered unique index + pre-create API check |
| CUS-04 — target_version conflict no overwrite | Met | rowVersion enforcement + 409 UX |
| CUS-05 — Admin correction with reason and audit | Met | Required reason + SecurityAuditEventRecord |
| CUS-06 — Company Context unique, no cross-company notes | Met | Unique (customer_id, company_id) + scoped queries |
| SEC-01 — No endpoint relies only on UI visibility | Met | [RequirePermission] on all endpoints |

---

## 7. Deferred backlog

| # | Item | Reason | Priority |
|---|------|--------|----------|
| 1 | Workflow/approval runtime (CUS-002) | Cross-cutting capability; requires separate design | High |
| 2 | Customer merge (CUS-007) | Complex; requires preview of affected entities | Medium |
| 3 | Group spending display (CUS-07 acceptance) | Requires confirmed payment data | Medium |
| 4 | ENTITY scope permissions | Not required for GLOBAL customer permissions | Low |
| 5 | Service module integration | Separate domain | Medium |
| 6 | Payment/Reconciliation module | Separate domain | Medium |
| 7 | Export/download | Not in first slice | Low |
| 8 | Security enhancement backlog | Separate initiative | Low |
| 9 | Company context add/edit modal | Button disabled; wiring deferred | Low |
| 10 | Production migration/release approval | V0005 must not auto-apply to production | High |

---

## 8. Residual risks

| # | Risk | Severity | Mitigation |
|---|------|----------|------------|
| 1 | Backend must remain sole authority for permissions and data protection | Medium | Architecture enforced; frontend gates are UX only |
| 2 | Workflow absence means only direct admin operations available | Medium | First production users must be authorized admins |
| 3 | Sensitive masking alignment between backend and frontend | Medium | Frontend displays backend-returned values as-is |
| 4 | Duplicate warning must not evolve into merge without approval | Medium | No merge action exposed; merge deferred |
| 5 | Concurrency UX depends on correct rowVersion propagation | Medium | 409 handling tested; refresh prompt implemented |
| 6 | V0005 migration must not be applied to production automatically | High | No auto-migration authorized; requires separate release approval |
| 7 | Customer merge deferred means no MERGED status handling | Low | No merge path exists yet |

---

## 9. Recommended next work options

| Option | Description | Dependencies |
|--------|-------------|-------------|
| A | Phase 1B.3 — Service module (discovery + plan) | Customer module complete (this phase) |
| B | Phase 1B.X — Workflow/approval runtime (cross-cutting) | Unblocks CUS-002 staff-mediated create/change |
| C | Phase 1B.X — Payment/Reconciliation module | Customer module + Service module |
| D | Phase 1B.X — Customer merge capability | Customer module complete |
| E | Company context modal implementation | Customer module complete |

Project Owner selects the next work based on business priority.

**Recommended Project Owner decision:**
Option A — Accept Customer first slice as functionally complete.

No new source implementation is authorized by this completion review.

---

## 10. Commit chain

| Commit | Description |
|--------|-------------|
| 7c6a610 | Plan Phase 1B.2-A customer module |
| 6ffa7c8 | Accept Phase 1B.2-A customer module plan |
| 91828f5 | Implement Phase 1B.2-B1 customer backend foundation |
| 458a8ea | Review Phase 1B.2-B1 implementation acceptance |
| 835b1b6 | Accept Phase 1B.2-B1 implementation |
| 8881ebe | Review Phase 1B.2-B1 closure |
| 4989913 | Record Phase 1B.2-B1 final acceptance |
| 4459582 | Plan Phase 1B.2-B2 customer frontend UI |
| e3e184d | Accept Phase 1B.2-B2 plan |
| 9b9ff19 | Implement Phase 1B.2-B2 customer frontend UI |
| 04b3d88 | Review Phase 1B.2-B2 implementation acceptance |
| 024fb3d | Accept Phase 1B.2-B2 implementation |
| 3b74387 | Review Phase 1B.2-B2 closure |
| 3200d2c | Record Phase 1B.2-B2 final acceptance |

14 commits total. Full lifecycle completed for both sub-phases.

---

## 11. Conclusion

Phase 1B.2 Customer Module First Slice is complete. Both B1 (backend) and B2 (frontend) sub-phases have passed all lifecycle stages through Project Owner final acceptance. Business rules CUS-001, CUS-003–CUS-006, CUS-008–CUS-009 are covered. CUS-002 (workflow-mediated path) and CUS-007 (merge) are explicitly deferred. All acceptance criteria within scope are met. Test suites pass across backend and frontend.

PHASE 1B.2 CUSTOMER MODULE FIRST SLICE COMPLETION REVIEW READY FOR PROJECT OWNER REVIEW

---

**Reviewer:** Claude Opus 4.6 (AI assistant)
**Review date:** 2026-07-31
**Awaiting:** Project Owner review and acceptance
