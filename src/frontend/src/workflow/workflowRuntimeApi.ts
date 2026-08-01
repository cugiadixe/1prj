import axiosClient from '../api/axiosClient';
import type {
  ApprovalActionRequest,
  MyApprovalItem,
  ReassignStepRequest,
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
