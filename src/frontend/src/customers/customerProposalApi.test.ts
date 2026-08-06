import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  createCustomerProposal,
  getCustomerProposalById,
  getMyCustomerProposals,
} from './customerProposalApi';
import type { CreateCustomerProposalRequest } from './customerProposalTypes';

vi.mock('../api/axiosClient');

describe('customerProposalApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('createCustomerProposal calls correct endpoint with payload', async () => {
    const request: CreateCustomerProposalRequest = {
      customerCode: 'CUST-001',
      fullName: 'Test Customer',
    };
    const mockResponse = { data: { id: 1, requestStatus: 'PENDING_APPROVAL' } };
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockResponse);

    const result = await createCustomerProposal(request);

    expect(axiosClient.post).toHaveBeenCalledWith('/customer-proposals', request);
    expect(result).toEqual(mockResponse.data);
  });

  it('getCustomerProposalById calls correct endpoint', async () => {
    const mockResponse = { data: { id: 1, requestStatus: 'PENDING_APPROVAL' } };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockResponse);

    const result = await getCustomerProposalById(1);

    expect(axiosClient.get).toHaveBeenCalledWith('/customer-proposals/1');
    expect(result).toEqual(mockResponse.data);
  });

  it('getMyCustomerProposals calls correct endpoint', async () => {
    const mockResponse = { data: [{ id: 1, requestStatus: 'PENDING_APPROVAL' }] };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockResponse);

    const result = await getMyCustomerProposals();

    expect(axiosClient.get).toHaveBeenCalledWith('/customer-proposals/my-proposals');
    expect(result).toEqual(mockResponse.data);
  });
});
