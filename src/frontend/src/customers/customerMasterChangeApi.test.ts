import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  createCustomerMasterChangeRequest,
  getCustomerMasterChangeRequestById,
  getMyCustomerMasterChangeRequests,
} from './customerMasterChangeApi';
import type { CreateCustomerMasterChangeRequest, CustomerMasterChangeDto } from './customerMasterChangeTypes';

vi.mock('../api/axiosClient');

describe('customerMasterChangeApi', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('createCustomerMasterChangeRequest calls POST with correct URL and data', async () => {
    const customerId = 123;
    const request: CreateCustomerMasterChangeRequest = {
      targetCustomerId: 123,
      targetRowVersion: 'v1',
      fullName: 'New Name',
      reason: 'Name change',
    };
    const responseDto: CustomerMasterChangeDto = {
      id: 456,
      processCode: 'CUSTOMER_MASTER_CHANGE',
      requesterId: 1,
      companyId: null,
      requestStatus: 'Draft',
      workflowInstanceId: null,
      targetCustomerId: 123,
      targetCustomerCode: 'KH0000123',
      targetCustomerName: 'Nguyễn Văn A',
      targetRowVersion: 'v1',
      createdAt: '2023-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'v2',
      payload: request,
    };

    vi.mocked(axiosClient.post).mockResolvedValueOnce({ data: responseDto });

    const result = await createCustomerMasterChangeRequest(customerId, request);

    expect(axiosClient.post).toHaveBeenCalledWith(
      '/customers/123/change-requests',
      request,
    );
    expect(result).toEqual(responseDto);
  });

  it('getCustomerMasterChangeRequestById calls GET with correct URL', async () => {
    const responseDto: CustomerMasterChangeDto = {
      id: 456,
      processCode: 'CUSTOMER_MASTER_CHANGE',
      requesterId: 1,
      companyId: null,
      requestStatus: 'Draft',
      workflowInstanceId: null,
      targetCustomerId: 123,
      targetCustomerCode: 'KH0000123',
      targetCustomerName: 'Nguyễn Văn A',
      targetRowVersion: 'v1',
      createdAt: '2023-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 'v2',
      payload: null,
    };

    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: responseDto });

    const result = await getCustomerMasterChangeRequestById(456);

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/change-requests/456');
    expect(result).toEqual(responseDto);
  });

  it('getMyCustomerMasterChangeRequests calls GET with correct URL', async () => {
    const responseDtoList: CustomerMasterChangeDto[] = [
      {
        id: 456,
        processCode: 'CUSTOMER_MASTER_CHANGE',
        requesterId: 1,
        companyId: null,
        requestStatus: 'Draft',
        workflowInstanceId: null,
        targetCustomerId: 123,
        targetCustomerCode: 'KH0000123',
        targetCustomerName: 'Nguyễn Văn A',
        targetRowVersion: 'v1',
        createdAt: '2023-01-01T00:00:00Z',
        updatedAt: null,
        rowVersion: 'v2',
        payload: null,
      },
    ];

    vi.mocked(axiosClient.get).mockResolvedValueOnce({ data: responseDtoList });

    const result = await getMyCustomerMasterChangeRequests();

    expect(axiosClient.get).toHaveBeenCalledWith('/customers/my-change-requests');
    expect(result).toEqual(responseDtoList);
  });
});
