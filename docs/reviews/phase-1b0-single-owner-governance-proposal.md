# Phase 1B.0 Single-Owner Governance Proposal

**Governance proposal status:**
APPROVED

The existing BA/DBA/Security review forms remain historical supporting documents and are not approval evidence.

## 1. Purpose
This document proposes a formal transition to a single-owner governance model for the PTKD-ERP Phase 1B implementation, adapting the approval process to match the project's actual organizational reality.

## 2. Current governance mismatch
The current documentation baseline and review packages (Phase 1B.0) are structured around a multi-role approval workflow requiring distinct Business Analyst (BA), Database Administrator (DBA), and Security Lead approvals. However, the project currently operates without these separate teams or individuals.

## 3. Why the existing BA/DBA/Security approval model cannot be used
- There is no separate BA, DBA, or Security team.
- Fictitious approvals or impersonations by AI agents violate the project's strict requirement for authentic, accountable human authorization.
- Waiting for non-existent roles blocks Phase 1B.1 authorization indefinitely.

## 4. Proposed single-owner model
The governance model will officially recognize the Project Owner as the single human authority accountable for all business, technical, database, and security decisions. Advisory roles will provide technical recommendations, but cannot grant organizational approval.

## 5. Roles and responsibilities

**Project Owner:**
- Sole final decision authority.
- Approves business behavior.
- Accepts residual technical, security, and operational risk.
- May approve, conditionally approve, reject, or defer decisions.

**ChatGPT — Advisory role:**
- Reviews requirements, architecture, security, and consistency.
- Recommends options and identifies risks.
- Does not provide organizational approval.
- Does not impersonate BA, DBA, Security, or an external auditor.

**Antigravity — Execution and evidence role:**
- Inspects repository state.
- Produces implementation proposals and verification evidence.
- Makes repository changes only after authorization.
- Does not approve its own recommendations.
- Does not claim independent segregation of duties.

## 6. Decision authority
Only the Project Owner holds decision authority. No decision is considered approved until explicitly authorized by the Project Owner.

## 7. Technical recommendation process
Antigravity and ChatGPT will analyze open decisions and provide concrete technical recommendations, identifying the safest or most appropriate choice based on existing canonical rules. These remain recommendations, not approvals.

## 8. Risk-acceptance process
Any residual risk resulting from adopting or rejecting a technical recommendation must be explicitly accepted by the Project Owner.

## 9. Conflict-resolution process
If a recommendation conflicts with project constraints, the Project Owner resolves the conflict by dictating the chosen path or requesting a revised recommendation.

## 10. Evidence requirements
Antigravity will provide verifiable repository evidence (e.g., test outputs, schema diffs, git status) to support recommendations and prove implementation compliance after approval.

## 11. Production-readiness limitations
The absence of independent BA, DBA, and Security reviewers is a known governance limitation. The Project Owner may accept this limitation for internal development. An independent security or database review should be considered before production deployment, especially for authentication, key management, audit controls, and production database permissions.

## 12. Required canonical-document changes after approval
If this proposal is approved, the following actions will be performed in a separate task:
- Update `docs/decisions/phase-1b0-open-decisions.md`.
- Update `docs/architecture/phase-1b0-security-discovery-decisions.md`.
- Update `docs/reviews/phase-1b0-stakeholder-review-package.md`.
- Record the Project Owner as the accountable approver.
- Replace unavailable role approvals with documented advisory review.
- Preserve all decision history.
- Preserve DEC-1B-008 as merged.
- Do not mark a decision approved without the Project Owner's explicit recorded result.
- Do not authorize Phase 1B.1 until all blocking decisions are resolved.
- The existing role-specific review forms will remain as historical/supporting templates and are not evidence of approval.

## 13. Explicit Project Owner approval section
- **Project Owner result:** APPROVED
- **Project Owner conditions:** Must adhere to the verified Phase 1B.0 Project Owner Decision Package conditions.
- **Project Owner comments:** Accepted governance mismatch limitations and residual risks for internal development. Independent specialist review required before production for DEC-1B-015 and DEC-1B-019.
- **Project Owner name:** Đào Hải Bách
- **Decision date:** 2026-07-15
- **Approval reference:** Project Owner Approval (Conversation: 0f48c7a6-f3a1-42d8-92af-ac1cd2e94fe7)
- **Confirmation method:** Direct Prompt Authorization 
