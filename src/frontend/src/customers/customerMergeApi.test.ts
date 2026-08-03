import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  findMergeDuplicates,
  createMergeRequest,
  getMergeRequestById,
  listMergeRequests,
} from './customerMergeApi';
import type { CreateCustomerMergeRequest, CustomerMergeRequestDto } from './customerMergeTypes';
import type { DuplicateCheckResult, PagedResult } from './types';

vi.mock('../api/axiosClient');

describe('customerMergeApi', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('findMergeDuplicates calls GET /customers/duplicates with params', async () => {
    const result: DuplicateCheckResult = { hasDuplicates: false, matches: [] };
    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: result });

    const response = await findMergeDuplicates({ cccd: '123456789012' });

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/duplicates', {
      params: { cccd: '123456789012', phone: undefined },
    });
    expect(response).toEqual(result);
  });

  it('findMergeDuplicates with phone param', async () => {
    const result: DuplicateCheckResult = {
      hasDuplicates: true,
      matches: [
        {
          id: 1,
          customerCode: 'C001',
          fullName: 'Test',
          cccd: null,
          phone: '0901234567',
          customerStatus: 'ACTIVE',
          createdAt: '2026-01-01T00:00:00Z',
        },
      ],
    };
    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: result });

    const response = await findMergeDuplicates({ phone: '0901234567' });

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/duplicates', {
      params: { cccd: undefined, phone: '0901234567' },
    });
    expect(response).toEqual(result);
  });

  it('createMergeRequest calls POST /customers/merge-requests', async () => {
    const request: CreateCustomerMergeRequest = {
      sourceCustomerId: 1,
      targetCustomerId: 2,
      survivorshipPayload: '{}',
      sourceRowVersionSnapshot: 'abc',
      targetRowVersionSnapshot: 'def',
      candidates: [],
    };
    const responseDto: CustomerMergeRequestDto = {
      id: 'guid-1',
      sourceCustomerId: 1,
      targetCustomerId: 2,
      requesterId: 10,
      requestStatus: 'DRAFT',
      survivorshipPayload: '{}',
      sourceRowVersionSnapshot: 'abc',
      targetRowVersionSnapshot: 'def',
      workflowInstanceId: null,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'rv1',
      candidates: [],
    };

    vi.mocked(axiosClient.post).mockResolvedValueOnce({ data: responseDto });

    const result = await createMergeRequest(request);

    expect(axiosClient.post).toHaveBeenCalledWith(
      '/customers/merge-requests',
      request,
    );
    expect(result).toEqual(responseDto);
  });

  it('getMergeRequestById calls GET /customers/merge-requests/{id}', async () => {
    const responseDto: CustomerMergeRequestDto = {
      id: 'guid-1',
      sourceCustomerId: 1,
      targetCustomerId: 2,
      requesterId: 10,
      requestStatus: 'SUBMITTED',
      survivorshipPayload: '{}',
      sourceRowVersionSnapshot: 'abc',
      targetRowVersionSnapshot: 'def',
      workflowInstanceId: 100,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'rv1',
      candidates: [],
    };

    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: responseDto });

    const result = await getMergeRequestById('guid-1');

    expect(axiosClient.get).toHaveBeenCalledWith(
      '/customers/merge-requests/guid-1',
    );
    expect(result).toEqual(responseDto);
  });

  it('listMergeRequests calls GET /customers/merge-requests with pagination', async () => {
    const responseData: PagedResult<CustomerMergeRequestDto> = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    };

    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: responseData });

    const result = await listMergeRequests({ page: 2, pageSize: 10 });

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/merge-requests', {
      params: { page: 2, pageSize: 10 },
    });
    expect(result).toEqual(responseData);
  });

  it('listMergeRequests uses defaults when no params', async () => {
    const responseData: PagedResult<CustomerMergeRequestDto> = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    };

    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: responseData });

    await listMergeRequests();

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/merge-requests', {
      params: { page: 1, pageSize: 20 },
    });
  });
});
