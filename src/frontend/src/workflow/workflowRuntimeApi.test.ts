import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  getMyApprovals,
  getInstance,
  approveStep,
  returnStep,
  resubmitInstance,
  withdrawInstance,
  reassignStep,
} from './workflowRuntimeApi';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

const mockAxios = vi.mocked(axiosClient);

describe('workflowRuntimeApi', () => {
  beforeEach(() => vi.clearAllMocks());

  it('getMyApprovals calls GET /workflows/my-approvals', async () => {
    mockAxios.get.mockResolvedValue({ data: [] });
    await getMyApprovals();
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/my-approvals');
  });

  it('getInstance calls GET /workflows/instances/:id', async () => {
    mockAxios.get.mockResolvedValue({ data: { id: 1 } });
    await getInstance(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/workflows/instances/1');
  });

  it('approveStep calls POST /workflows/instances/:id/steps/:stepId/approve', async () => {
    const req = { reason: 'ok', comment: null, targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await approveStep(1, 2, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/instances/1/steps/2/approve', req);
  });

  it('returnStep calls POST /workflows/instances/:id/steps/:stepId/return', async () => {
    const req = { reason: 'needs fix', comment: null, targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await returnStep(1, 2, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/instances/1/steps/2/return', req);
  });

  it('resubmitInstance calls POST /workflows/instances/:id/resubmit', async () => {
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await resubmitInstance(1, 'AA');
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/instances/1/resubmit', { targetVersion: 'AA' });
  });

  it('withdrawInstance calls POST /workflows/instances/:id/withdraw', async () => {
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await withdrawInstance(1, 'AA');
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/instances/1/withdraw', { targetVersion: 'AA' });
  });

  it('reassignStep calls POST /workflows/instances/:id/steps/:stepId/reassign', async () => {
    const req = { newAssigneeUserId: 5, reason: 'reassign', targetVersion: 'AA' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await reassignStep(1, 2, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/workflows/instances/1/steps/2/reassign', req);
  });

  it('does not expose createInstance function', async () => {
    const mod = await import('./workflowRuntimeApi');
    expect('createInstance' in mod).toBe(false);
  });

  it('does not expose getMyRequests function', async () => {
    const mod = await import('./workflowRuntimeApi');
    expect('getMyRequests' in mod).toBe(false);
  });

  it('does not expose getActionHistory function', async () => {
    const mod = await import('./workflowRuntimeApi');
    expect('getActionHistory' in mod).toBe(false);
  });

  it('does not expose rejectStep function', async () => {
    const mod = await import('./workflowRuntimeApi');
    expect('rejectStep' in mod).toBe(false);
  });
});
