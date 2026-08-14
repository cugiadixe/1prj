export interface BusinessProcess {
  processCode: string;
  processName: string;
  description: string | null;
  isApprovalRequired: boolean;
  isActive: boolean;
}

export interface WorkflowDefinitionListItem {
  id: number;
  definitionCode: string;
  definitionName: string;
  processCode: string;
  isActive: boolean;
  createdAt: string;
}

export interface WorkflowDefinitionDetail {
  id: number;
  definitionCode: string;
  definitionName: string;
  description: string | null;
  processCode: string;
  isActive: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateWorkflowDefinitionRequest {
  definitionCode: string;
  definitionName: string;
  description?: string | null;
  processCode: string;
}

export interface UpdateWorkflowDefinitionRequest {
  definitionName: string;
  description?: string | null;
  targetVersion: string;
}

export interface WorkflowVersionListItem {
  id: number;
  versionNumber: number;
  versionStatus: string;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  createdAt: string;
}

export interface WorkflowVersionDetail {
  id: number;
  workflowDefinitionId: number;
  versionNumber: number;
  versionStatus: string;
  effectiveFrom: string | null;
  effectiveTo: string | null;
  publishedAt: string | null;
  rowVersion: string;
  createdAt: string;
  steps: WorkflowStep[];
  conditions: WorkflowCondition[];
}

export interface WorkflowStep {
  id: number;
  stepOrder: number;
  stepName: string;
  description: string | null;
  isRequired: boolean;
  dueDurationMinutes: number | null;
  rowVersion: string;
  approverRules: ApproverRule[];
}

export interface CreateWorkflowStepRequest {
  stepName: string;
  stepOrder: number;
  isRequired: boolean;
  description?: string | null;
  dueDurationMinutes?: number | null;
}

export interface UpdateWorkflowStepRequest {
  stepName: string;
  stepOrder: number;
  isRequired: boolean;
  description?: string | null;
  dueDurationMinutes?: number | null;
  targetVersion: string;
}

export interface ApproverRule {
  id: number;
  approverSourceType: string;
  approverSourceValue: string;
  priority: number;
}

export interface CreateApproverRuleRequest {
  approverSourceType: string;
  approverSourceValue: string;
  priority: number;
}

export interface WorkflowCondition {
  id: number;
  fieldCode: string;
  operator: string;
  value: string;
}

export interface PublishVersionRequest {
  effectiveFrom: string;
  effectiveTo?: string | null;
  targetVersion: string;
}

export interface ActivateVersionRequest {
  targetVersion: string;
}

export interface RetireVersionRequest {
  targetVersion: string;
}

export interface WorkflowBindingListItem {
  id: number;
  workflowVersionId: number;
  processCode: string;
  scopeType: string;
  companyId: number | null;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface CreateWorkflowBindingRequest {
  workflowVersionId: number;
  processCode: string;
  scopeType: string;
  companyId?: number | null;
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
}

export interface UpdateWorkflowBindingRequest {
  effectiveFrom: string;
  effectiveTo?: string | null;
  priority: number;
  targetVersion: string;
}

export interface MyApprovalItem {
  instanceId: number;
  stepId: number;
  processCode: string;
  businessEntityType: string;
  businessEntityId: number;
  stepName: string;
  instanceStatus: string;
  assignedAt: string | null;
  requesterId: number;
  requesterName: string | null;
  businessEntityLabel: string | null;
}

export interface WorkflowInstance {
  id: number;
  workflowVersionId: number;
  processCode: string;
  companyId: number | null;
  requesterId: number;
  requesterName: string | null;
  businessEntityLabel: string | null;
  businessEntityType: string;
  businessEntityId: number;
  instanceStatus: string;
  roundNo: number;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
  steps: WorkflowInstanceStep[];
}

export interface WorkflowInstanceStep {
  id: number;
  stepOrder: number;
  stepName: string;
  roundNo: number;
  stepStatus: string;
  assignedAt: string | null;
  completedAt: string | null;
  completedBy: number | null;
  completedByName: string | null;
  rowVersion: string;
  assignees: WorkflowInstanceStepAssignee[];
}

export interface WorkflowInstanceStepAssignee {
  userId: number;
  userName: string | null;
  approverSourceType: string;
}

export interface WorkflowActionDto {
  id: number;
  workflowInstanceStepId: number;
  workflowInstanceId: number;
  actionType: string;
  actedBy: number;
  actedByName: string | null;
  onBehalfOf: number | null;
  onBehalfOfName: string | null;
  reason: string | null;
  comment: string | null;
  createdAt: string;
}

export interface ApprovalActionRequest {
  reason?: string | null;
  comment?: string | null;
  targetVersion: string;
}

export interface ReassignStepRequest {
  newAssigneeUserId: number;
  reason: string;
  targetVersion: string;
}

export interface WorkflowSearchParams {
  processCode?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
