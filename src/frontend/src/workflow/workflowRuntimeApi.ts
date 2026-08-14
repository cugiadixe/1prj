import axiosClient from '../api/axiosClient';
import type {
  ApprovalActionRequest,
  MyApprovalItem,
  ReassignStepRequest,
  WorkflowActionDto,
  WorkflowInstance,
} from './types';

const BASE = '/workflows';

export async function getMyApprovals(): Promise<MyApprovalItem[]> {
  const { data } = await axiosClient.get<MyApprovalItem[]>(`${BASE}/my-approvals`);
  return data;
}

export async function getInstance(instanceId: number): Promise<WorkflowInstance> {
  const { data } = await axiosClient.get<WorkflowInstance>(`${BASE}/instances/${instanceId}`);
  return data;
}

export async function approveStep(
  instanceId: number,
  stepId: number,
  request: ApprovalActionRequest,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/steps/${stepId}/approve`,
    request,
  );
  return data;
}

export async function returnStep(
  instanceId: number,
  stepId: number,
  request: ApprovalActionRequest,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/steps/${stepId}/return`,
    request,
  );
  return data;
}

export async function resubmitInstance(
  instanceId: number,
  targetVersion: string,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/resubmit`,
    { targetVersion },
  );
  return data;
}

export async function withdrawInstance(
  instanceId: number,
  targetVersion: string,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/withdraw`,
    { targetVersion },
  );
  return data;
}

export async function getMyRequests(): Promise<WorkflowInstance[]> {
  const { data } = await axiosClient.get<WorkflowInstance[]>(`${BASE}/my-requests`);
  return data;
}

export async function getInstanceActions(instanceId: number): Promise<WorkflowActionDto[]> {
  const { data } = await axiosClient.get<WorkflowActionDto[]>(`${BASE}/instances/${instanceId}/actions`);
  return data;
}

export async function rejectStep(
  instanceId: number,
  stepId: number,
  request: ApprovalActionRequest,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/steps/${stepId}/reject`,
    request,
  );
  return data;
}

export async function retryExecution(instanceId: number): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/retry-execution`,
  );
  return data;
}

export async function reassignStep(
  instanceId: number,
  stepId: number,
  request: ReassignStepRequest,
): Promise<WorkflowInstance> {
  const { data } = await axiosClient.post<WorkflowInstance>(
    `${BASE}/instances/${instanceId}/steps/${stepId}/reassign`,
    request,
  );
  return data;
}

export interface WorkflowInstanceSearchParams {
  processCode?: string;
  instanceStatus?: string;
  companyId?: number;
  page?: number;
  pageSize?: number;
}

export interface PagedInstances {
  page: number;
  pageSize: number;
  totalCount: number;
  items: WorkflowInstance[];
}

/** Tra cứu hồ sơ toàn hệ thống (quản trị). Cần quyền WORKFLOW_VIEW. */
export async function searchInstances(
  params: WorkflowInstanceSearchParams = {},
): Promise<PagedInstances> {
  const { data } = await axiosClient.get<PagedInstances>(`${BASE}/instances`, {
    params: {
      processCode: params.processCode || undefined,
      instanceStatus: params.instanceStatus || undefined,
      companyId: params.companyId,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}
