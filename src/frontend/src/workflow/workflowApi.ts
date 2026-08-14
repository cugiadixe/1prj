import axiosClient from '../api/axiosClient';
import type {
  ActivateVersionRequest,
  BusinessProcess,
  CreateApproverRuleRequest,
  CreateWorkflowBindingRequest,
  CreateWorkflowDefinitionRequest,
  CreateWorkflowStepRequest,
  ApproverRule,
  PagedResult,
  PublishVersionRequest,
  RetireVersionRequest,
  UpdateWorkflowBindingRequest,
  UpdateWorkflowDefinitionRequest,
  UpdateWorkflowStepRequest,
  WorkflowBindingListItem,
  WorkflowDefinitionDetail,
  WorkflowDefinitionListItem,
  WorkflowSearchParams,
  WorkflowStep,
  WorkflowVersionDetail,
  WorkflowVersionListItem,
} from './types';

const BASE = '/workflows';

export async function getBusinessProcesses(): Promise<BusinessProcess[]> {
  const { data } = await axiosClient.get<BusinessProcess[]>(`${BASE}/processes`);
  return data;
}

export async function searchDefinitions(
  params: WorkflowSearchParams = {},
): Promise<PagedResult<WorkflowDefinitionListItem>> {
  const { data } = await axiosClient.get<PagedResult<WorkflowDefinitionListItem>>(
    `${BASE}/definitions`,
    {
      params: {
        processCode: params.processCode || undefined,
        isActive: params.isActive,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
      },
    },
  );
  return data;
}

export async function getDefinitionById(
  id: number,
): Promise<WorkflowDefinitionDetail> {
  const { data } = await axiosClient.get<WorkflowDefinitionDetail>(
    `${BASE}/definitions/${id}`,
  );
  return data;
}

export async function createDefinition(
  request: CreateWorkflowDefinitionRequest,
): Promise<WorkflowDefinitionDetail> {
  const { data } = await axiosClient.post<WorkflowDefinitionDetail>(
    `${BASE}/definitions`,
    request,
  );
  return data;
}

export async function updateDefinition(
  id: number,
  request: UpdateWorkflowDefinitionRequest,
): Promise<WorkflowDefinitionDetail> {
  const { data } = await axiosClient.put<WorkflowDefinitionDetail>(
    `${BASE}/definitions/${id}`,
    request,
  );
  return data;
}

export async function getVersionsByDefinition(
  definitionId: number,
): Promise<WorkflowVersionListItem[]> {
  const { data } = await axiosClient.get<WorkflowVersionListItem[]>(
    `${BASE}/definitions/${definitionId}/versions`,
  );
  return data;
}

export async function createVersion(
  definitionId: number,
): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.post<WorkflowVersionDetail>(
    `${BASE}/definitions/${definitionId}/versions`,
  );
  return data;
}

export async function getVersionById(
  versionId: number,
): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.get<WorkflowVersionDetail>(
    `${BASE}/versions/${versionId}`,
  );
  return data;
}

export async function deleteVersion(versionId: number): Promise<void> {
  await axiosClient.delete(`${BASE}/versions/${versionId}`);
}

export async function createStep(
  versionId: number,
  request: CreateWorkflowStepRequest,
): Promise<WorkflowStep> {
  const { data } = await axiosClient.post<WorkflowStep>(
    `${BASE}/versions/${versionId}/steps`,
    request,
  );
  return data;
}

export async function updateStep(
  stepId: number,
  request: UpdateWorkflowStepRequest,
): Promise<WorkflowStep> {
  const { data } = await axiosClient.put<WorkflowStep>(
    `${BASE}/steps/${stepId}`,
    request,
  );
  return data;
}

export async function deleteStep(stepId: number): Promise<void> {
  await axiosClient.delete(`${BASE}/steps/${stepId}`);
}

export async function createApproverRule(
  stepId: number,
  request: CreateApproverRuleRequest,
): Promise<ApproverRule> {
  const { data } = await axiosClient.post<ApproverRule>(
    `${BASE}/steps/${stepId}/approver-rules`,
    request,
  );
  return data;
}

export async function updateApproverRule(
  ruleId: number,
  request: CreateApproverRuleRequest,
): Promise<ApproverRule> {
  const { data } = await axiosClient.put<ApproverRule>(
    `${BASE}/approver-rules/${ruleId}`,
    request,
  );
  return data;
}

export async function deleteApproverRule(ruleId: number): Promise<void> {
  await axiosClient.delete(`${BASE}/approver-rules/${ruleId}`);
}

/**
 * Nhân bản phiên bản thành bản nháp mới (sao chép bước + luật người duyệt + điều kiện).
 * Không có việc này thì muốn sửa 1 bước phải gõ lại toàn bộ quy trình.
 */
export async function cloneVersion(versionId: number): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.post<WorkflowVersionDetail>(
    `${BASE}/versions/${versionId}/clone`,
  );
  return data;
}

export async function publishVersion(
  versionId: number,
  request: PublishVersionRequest,
): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.post<WorkflowVersionDetail>(
    `${BASE}/versions/${versionId}/publish`,
    request,
  );
  return data;
}

export async function activateVersion(
  versionId: number,
  request: ActivateVersionRequest,
): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.post<WorkflowVersionDetail>(
    `${BASE}/versions/${versionId}/activate`,
    request,
  );
  return data;
}

export async function retireVersion(
  versionId: number,
  request: RetireVersionRequest,
): Promise<WorkflowVersionDetail> {
  const { data } = await axiosClient.post<WorkflowVersionDetail>(
    `${BASE}/versions/${versionId}/retire`,
    request,
  );
  return data;
}

export async function getBindings(
  processCode?: string,
): Promise<WorkflowBindingListItem[]> {
  const { data } = await axiosClient.get<WorkflowBindingListItem[]>(
    `${BASE}/bindings`,
    { params: { processCode: processCode || undefined } },
  );
  return data;
}

export async function createBinding(
  request: CreateWorkflowBindingRequest,
): Promise<WorkflowBindingListItem> {
  const { data } = await axiosClient.post<WorkflowBindingListItem>(
    `${BASE}/bindings`,
    request,
  );
  return data;
}

export async function updateBinding(
  bindingId: number,
  request: UpdateWorkflowBindingRequest,
): Promise<WorkflowBindingListItem> {
  const { data } = await axiosClient.put<WorkflowBindingListItem>(
    `${BASE}/bindings/${bindingId}`,
    request,
  );
  return data;
}

export interface ApproverSourceOption {
  value: string;
  label: string;
  hint: string | null;
}

/** Danh sách lựa chọn cho ô "giá trị nguồn" theo loại nguồn người duyệt. */
export async function getApproverSourceOptions(
  sourceType: string,
): Promise<ApproverSourceOption[]> {
  const { data } = await axiosClient.get<ApproverSourceOption[]>(
    `${BASE}/approver-source-options`,
    { params: { sourceType } },
  );
  return data;
}

/** Các phiên bản đang hiệu lực có thể gán liên kết cho một mã quy trình. */
export async function getBindableVersions(
  processCode: string,
): Promise<ApproverSourceOption[]> {
  const { data } = await axiosClient.get<ApproverSourceOption[]>(
    `${BASE}/bindable-versions`,
    { params: { processCode } },
  );
  return data;
}

/** Ngừng một liên kết quy trình. */
export async function deactivateBinding(
  bindingId: number,
  targetVersion: string,
): Promise<WorkflowBindingListItem> {
  const { data } = await axiosClient.post<WorkflowBindingListItem>(
    `${BASE}/bindings/${bindingId}/deactivate`,
    { targetVersion },
  );
  return data;
}
