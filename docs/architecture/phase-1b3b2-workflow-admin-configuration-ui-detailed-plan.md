# Phase 1B.3-B2 Workflow Admin Configuration UI Detailed Plan

## Status

ACCEPTED — SEE phase-1b3b2-project-owner-plan-acceptance.md

## Baseline

8ccaff5628a5632114ba692f0b430e49b0b4eeb3

## Authorization and Context

| Role | Hash |
|---|---|
| Phase 1B.3-A plan acceptance commit | 54700b1af8c6e831a82fa2d8c90254932f3955a4 |
| B1 implementation commit | f1fafacad81879fa72ca607616e68b34b7024bab |
| B1 final acceptance commit | 8ccaff5628a5632114ba692f0b430e49b0b4eeb3 |

---

## Confirmed Current State

- Workflow Backend Foundation complete (B1).
- V0006/U0006 migration/rollback complete.
- 11 workflow database tables complete.
- Workflow API v2 backend complete (19 configuration endpoints, 7 runtime endpoints).
- PermissionCodes.cs contains six approved workflow constants.
- No Workflow Admin UI exists in the frontend.
- No My Approvals UI exists in the frontend.
- No pilot business process integration exists.
- No implementation is authorized by this plan.

---

## Proposed B2 Scope

### Included in B2

- Workflow admin menu/navigation with permission gates.
- Workflow definitions list/search page.
- Workflow definition create page.
- Workflow definition detail page with edit capability.
- Workflow version list within definition detail.
- Workflow version detail page showing steps, approver rules, and conditions.
- Workflow step configuration (create/edit/delete within DRAFT version).
- Workflow approver rule configuration (create within DRAFT version step).
- Workflow condition configuration (read-only display within version detail).
- Workflow binding/process assignment list and create/edit page.
- Publish/activate/retire version actions.
- Delete DRAFT version action.
- Sanitized error handling following existing frontend patterns.
- rowVersion/concurrency conflict UX with refresh prompt.
- Version freeze warning: clear explanation that active instances use frozen version/snapshot.
- Status labels for DRAFT, PUBLISHED, ACTIVE, RETIRED.

### Explicitly Deferred from B2

- My Approvals inbox (B3).
- Approve/reject/return/resubmit/withdraw runtime action UI (B3).
- Runtime requester UI (B3).
- Workflow Admin active instance migration UI (deferred indefinitely).
- Pilot integration with Customer/Service/Payment/Merge (B4).
- Service module implementation.
- Payment/Reconciliation implementation.
- Customer Merge implementation.
- ENTITY scope implementation.
- Export/download implementation.
- Backend/API/database/migration changes by default.
- Condition editor with full UI (basic read-only display only in B2 — see DEC-1B3B2-04).
- Approver rule deletion (no backend endpoint exists — see DEC-1B3B2-05).
- Condition creation/deletion UI (no backend endpoint for create/delete conditions — see DEC-1B3B2-06).
- Workflow definition deactivation UI (backend has Update with isActive field — may be considered for B2 or deferred).

---

## Proposed Routes

Following existing frontend patterns (React Router v6, nested under authenticated layout):

| Route | Page | Permission |
|---|---|---|
| `/workflow` | WorkflowDefinitionsPage (list/search) | WORKFLOW_VIEW |
| `/workflow/definitions/new` | WorkflowDefinitionCreatePage | WORKFLOW_CONFIG_MANAGE |
| `/workflow/definitions/:definitionId` | WorkflowDefinitionDetailPage | WORKFLOW_VIEW |
| `/workflow/definitions/:definitionId/edit` | WorkflowDefinitionEditPage | WORKFLOW_CONFIG_MANAGE |
| `/workflow/versions/:versionId` | WorkflowVersionDetailPage | WORKFLOW_VIEW |
| `/workflow/bindings` | WorkflowBindingsPage | WORKFLOW_VIEW |

---

## Proposed Pages and Components

### Pages

| Page | Description |
|---|---|
| WorkflowDefinitionsPage | List/search definitions with pagination. Create button gated by WORKFLOW_CONFIG_MANAGE. |
| WorkflowDefinitionCreatePage | Form: definition code, name, description, process code (dropdown from business processes). |
| WorkflowDefinitionDetailPage | Shows definition detail, version list. Edit button gated by WORKFLOW_CONFIG_MANAGE. |
| WorkflowDefinitionEditPage | Edit name, description. Concurrency via TargetVersion. |
| WorkflowVersionDetailPage | Shows version detail with steps, approver rules, conditions. Publish/activate/retire actions gated by WORKFLOW_PUBLISH. Step/rule editing gated by WORKFLOW_CONFIG_MANAGE for DRAFT versions only. |
| WorkflowBindingsPage | List bindings with optional processCode filter. Create/edit gated by WORKFLOW_BIND_PROCESS. |

### Components

| Component | Description |
|---|---|
| WorkflowStepEditor | Inline or modal editor for creating/editing steps within a DRAFT version. |
| WorkflowApproverRuleEditor | Inline or modal editor for adding approver rules to a step. |
| WorkflowConditionDisplay | Read-only display of conditions on a version. |
| WorkflowBindingForm | Modal or page form for creating/editing bindings. |
| WorkflowStatusBadge | Renders DRAFT/PUBLISHED/ACTIVE/RETIRED status with appropriate color. |
| WorkflowVersionFreezeNotice | Info banner explaining that active instances use frozen version/snapshot. |

---

## API Client Plan

### Backend Endpoints (from B1 WorkflowConfigurationController)

| HTTP | Path | B2 UI Action | Permission |
|---|---|---|---|
| GET | `/api/v2/workflows/processes` | Load business process dropdown | WORKFLOW_VIEW |
| GET | `/api/v2/workflows/definitions` | List/search definitions | WORKFLOW_VIEW |
| POST | `/api/v2/workflows/definitions` | Create definition | WORKFLOW_CONFIG_MANAGE |
| GET | `/api/v2/workflows/definitions/{id}` | Definition detail | WORKFLOW_VIEW |
| PUT | `/api/v2/workflows/definitions/{id}` | Edit definition | WORKFLOW_CONFIG_MANAGE |
| POST | `/api/v2/workflows/definitions/{definitionId}/versions` | Create new version | WORKFLOW_CONFIG_MANAGE |
| GET | `/api/v2/workflows/definitions/{definitionId}/versions` | List versions | WORKFLOW_VIEW |
| GET | `/api/v2/workflows/versions/{versionId}` | Version detail (steps, conditions) | WORKFLOW_VIEW |
| DELETE | `/api/v2/workflows/versions/{versionId}` | Delete DRAFT version | WORKFLOW_CONFIG_MANAGE |
| POST | `/api/v2/workflows/versions/{versionId}/steps` | Create step | WORKFLOW_CONFIG_MANAGE |
| PUT | `/api/v2/workflows/steps/{stepId}` | Update step | WORKFLOW_CONFIG_MANAGE |
| DELETE | `/api/v2/workflows/steps/{stepId}` | Delete step | WORKFLOW_CONFIG_MANAGE |
| POST | `/api/v2/workflows/steps/{stepId}/approver-rules` | Add approver rule | WORKFLOW_CONFIG_MANAGE |
| POST | `/api/v2/workflows/versions/{versionId}/publish` | Publish version | WORKFLOW_PUBLISH |
| POST | `/api/v2/workflows/versions/{versionId}/activate` | Activate version | WORKFLOW_PUBLISH |
| POST | `/api/v2/workflows/versions/{versionId}/retire` | Retire version | WORKFLOW_PUBLISH |
| GET | `/api/v2/workflows/bindings` | List bindings | WORKFLOW_VIEW |
| POST | `/api/v2/workflows/bindings` | Create binding | WORKFLOW_BIND_PROCESS |
| PUT | `/api/v2/workflows/bindings/{bindingId}` | Update binding | WORKFLOW_BIND_PROCESS |

### Endpoint Gaps Identified

| Gap | Description | Impact |
|---|---|---|
| No DELETE approver rule endpoint | Cannot remove an approver rule after creation | DEC-1B3B2-05 |
| No POST/DELETE condition endpoint | Cannot create or delete conditions via API | DEC-1B3B2-06 |
| No GET definition with versions included | Definition detail returns definition only; versions require separate call | Minor — two API calls needed |
| No workflow audit query endpoint | No dedicated workflow audit read endpoint | DEC-1B3B2-02 — audit UI deferred or uses existing security audit query |

### Proposed API Client Structure

Following existing pattern (`src/frontend/src/customers/customersApi.ts`):

- Create `src/frontend/src/workflow/workflowApi.ts` with typed async functions.
- Use shared `axiosClient` instance.
- Each function returns typed DTOs matching backend response.
- Pages consume via TanStack React Query (`useQuery` / `useMutation`).

---

## Permission Gate Strategy

| Permission | Usage |
|---|---|
| WORKFLOW_VIEW | Menu item visibility, all list/detail/read pages |
| WORKFLOW_CONFIG_MANAGE | Create/edit definition, create/edit/delete version (DRAFT), create/edit/delete step, create approver rule |
| WORKFLOW_PUBLISH | Publish, activate, retire version actions |
| WORKFLOW_BIND_PROCESS | Create/edit binding actions |
| WORKFLOW_AUDIT_VIEW | Audit read UI if included (see DEC-1B3B2-02) |
| WORKFLOW_REASSIGN_PENDING | Not used in B2 — deferred to B3 runtime |

- Frontend gates are UX/navigation only following existing `hasPermission()` hook pattern.
- Backend remains authoritative — all API calls enforce permissions server-side.
- DENY-wins behavior is preserved (frontend does not override backend denial).
- No localStorage/sessionStorage/cookie permission persistence.
- No frontend-only authorization assumptions.

---

## UX Strategy

### Loading/Empty/Error States

Following existing patterns from Customer pages:
- Loading: spinner or skeleton while API calls pending.
- Empty: message when no definitions/versions/bindings exist.
- Error: `<Alert type="error">` with sanitized message from `errorMessages.ts`.

### Status Labels

| Status | Color | Description |
|---|---|---|
| DRAFT | Default/gray | Editable, not yet published |
| PUBLISHED | Blue | Published with effective dates, not yet active |
| ACTIVE | Green | Currently active for new instances |
| RETIRED | Red/muted | No longer used for new instances |

### Concurrency Handling

- All update/action requests include `TargetVersion` (base64 rowVersion).
- On concurrency error (backend returns ConcurrencyException), display: "This record was modified by another user. Please refresh and try again."
- Follow existing `isConcurrencyError()` pattern from Customer error handling.

### Version Freeze Warning

- On WorkflowVersionDetailPage and publish/activate actions, display an info banner:
  "Active instances use a frozen snapshot of the workflow version at the time they were created. Changes to this version will only affect new instances."
- On retire action, display confirmation: "Retiring this version means no new instances will use it. Existing active instances will continue with their frozen snapshot."

### Confirmation Dialogs

- Publish: confirm effective dates and that DRAFT becomes PUBLISHED.
- Activate: confirm that this version becomes the active version.
- Retire: confirm that no new instances will use this version.
- Delete DRAFT version: confirm permanent deletion.
- Delete step: confirm step removal.

### 403 Handling

- Follow existing per-page pattern: detect 403 response, show `data-testid="permission-denied"` alert.
- No global 403 page.

### 404 Handling

- Definition/version/binding not found: show "Not found" message with link back to list.

---

## Testing Strategy

Following existing patterns (Vitest + React Testing Library):

### Route/Menu Tests

- Workflow menu item appears only when user has WORKFLOW_VIEW permission.
- Workflow menu item is hidden when WORKFLOW_VIEW is denied.
- All workflow routes render correct pages.

### Permission Gate Tests

- Create definition button visible only with WORKFLOW_CONFIG_MANAGE.
- Edit definition button visible only with WORKFLOW_CONFIG_MANAGE.
- Publish/activate/retire buttons visible only with WORKFLOW_PUBLISH.
- Create/edit binding buttons visible only with WORKFLOW_BIND_PROCESS.
- Step/rule editing controls visible only with WORKFLOW_CONFIG_MANAGE and version in DRAFT status.

### API Client Tests

- Each API function calls correct endpoint with correct parameters.
- Error responses are handled correctly.

### Page Tests

- WorkflowDefinitionsPage: renders list, handles empty/error states, search filtering works.
- WorkflowDefinitionCreatePage: form validation, submit calls API, redirect on success.
- WorkflowDefinitionDetailPage: renders detail with versions, shows edit/version actions.
- WorkflowDefinitionEditPage: form pre-populated, submit with TargetVersion, concurrency error handling.
- WorkflowVersionDetailPage: renders steps/rules/conditions, DRAFT shows editing controls, non-DRAFT hides editing controls.
- WorkflowBindingsPage: renders list, create/edit forms, processCode filter.

### Error Handling Tests

- Generic error renders error alert.
- 403 renders permission denied alert.
- 404 renders not found message.
- Concurrency error renders refresh prompt.

### Deferred Behavior Tests

- No My Approvals menu item or route exists.
- No approve/reject/return runtime action UI exists.

---

## Open Decisions

### DEC-1B3B2-01: Final B2 route structure

Should routes use `/workflow` as the top-level path, or nest under `/admin/workflow` or `/settings/workflow`?

**Recommendation:** Use `/workflow` to match the flat route pattern used by other modules (`/customers`, `/security`).

### DEC-1B3B2-02: Audit read UI in B2

Should B2 include a read-only workflow audit view page? The existing Security Audit Query endpoint may cover workflow events if they are recorded as SecurityAuditEventRecord. No dedicated workflow audit query endpoint exists.

**Recommendation:** Defer dedicated workflow audit UI to a later phase. Existing security audit viewer already shows workflow events if the user has SECURITY_AUDIT_VIEW. Adding WORKFLOW_AUDIT_VIEW-gated UI requires a dedicated backend endpoint or filter parameter, which would be a backend change not authorized in B2.

### DEC-1B3B2-03: Publish/activate/retire in B2

Should B2 include publish, activate, and retire version actions?

**Recommendation:** Yes — these are core admin configuration lifecycle actions and backend endpoints exist. Deferring them would leave the admin UI unable to complete the workflow definition lifecycle.

### DEC-1B3B2-04: Condition editor scope

Should B2 include a full condition editor (create/edit/delete), or only read-only display? No backend POST/DELETE condition endpoints exist in B1.

**Recommendation:** Read-only display only in B2. Full condition editor requires backend gap resolution (DEC-1B3B2-06). Conditions are currently only creatable via direct database seeding or future backend work.

### DEC-1B3B2-05: Approver rule deletion

Should B2 support deleting approver rules? No backend DELETE approver rule endpoint exists in B1.

**Recommendation:** Defer approver rule deletion. B2 can create rules but not remove them. If this is blocking, a small backend addition (DELETE `/api/v2/workflows/approver-rules/{ruleId}`) would be needed as a gap-resolution task before or during B2.

### DEC-1B3B2-06: Condition creation/deletion

Should B2 support creating or deleting conditions via the UI? No backend POST/DELETE condition endpoints exist in B1.

**Recommendation:** Defer condition CRUD to a gap-resolution task. B2 shows conditions read-only.

### DEC-1B3B2-07: Company-specific binding UI

Should B2 support creating company-scoped bindings (COMPANY scope with company_id), or only GLOBAL bindings?

**Recommendation:** Support both — the backend already supports COMPANY scope with company_id, and the frontend already has a company selector pattern. The UI should show scope type selection and conditionally show company selector.

### DEC-1B3B2-08: Version comparison

Should B2 include side-by-side version comparison?

**Recommendation:** Defer. Version comparison adds significant UI complexity. B2 should focus on individual version detail.

### DEC-1B3B2-09: Active instance migration UI

Should B2 include any active instance migration UI?

**Recommendation:** Deferred. The accepted plan explicitly states active instance migration requires explicit admin action and separate audit, and the backend does not implement this feature.

### DEC-1B3B2-10: WORKFLOW_REASSIGN_PENDING in B2

Should WORKFLOW_REASSIGN_PENDING be used in any B2 UI?

**Recommendation:** No — reassignment is a runtime action belonging to B3. B2 is configuration-only.

### DEC-1B3B2-11: Backend changes in B2

Are any backend changes allowed in B2?

**Recommendation:** No backend changes by default. If endpoint gaps (DEC-1B3B2-05, DEC-1B3B2-06) are blocking, a separate backend gap-resolution task should be created and approved before or alongside B2 implementation. B2 implementation should be frontend-only.

---

## Risks

| Risk | Mitigation |
|---|---|
| Condition editor blocked by missing backend endpoints | Defer to read-only display; create gap-resolution task if needed |
| Approver rule deletion blocked by missing backend endpoint | Defer deletion; rules can be created but not removed in B2 |
| Publishing/activation mistakes may affect future instances | Confirmation dialogs with clear warnings |
| UI could imply active instances change route | Version freeze notice banner on relevant pages |
| Backend remains authoritative | Frontend gates are UX only; all actions validated server-side |
| Pilot business process not selected | B2 does not depend on pilot selection; pilot is B4 |
| Runtime UI deferred | B2 is configuration-only; no runtime actions exposed |

---

## Recommended Project Owner Decision

All required B1 backend configuration endpoints exist for B2 frontend implementation. Two minor gaps exist:

1. No DELETE approver rule endpoint — approver rules cannot be removed after creation.
2. No POST/DELETE condition endpoints — conditions cannot be created or deleted via UI.

**Recommendation:** Approve B2 as frontend-only implementation with read-only condition display and no approver rule deletion. If these gaps become blocking during future phases, create a separate backend gap-resolution task.

---

## Explicit Non-Authorization

- This plan does not authorize implementation.
- No source code changes.
- No test changes.
- No backend changes.
- No frontend changes.
- No migration changes.
- No rollback changes.
- No PermissionCodes.cs changes.
- No permission-catalog.md changes.
- No business-rules.md or acceptance-criteria.md changes.
- No production migration/release.

---

## Conclusion

PHASE 1B.3-B2 WORKFLOW ADMIN CONFIGURATION UI DETAILED PLAN READY FOR PROJECT OWNER REVIEW
