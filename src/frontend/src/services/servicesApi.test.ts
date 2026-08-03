import { describe, it, expect, vi } from 'vitest';
import axiosClient from '../api/axiosClient';
import { searchServices, getServiceById, createService, renewService, requestPriceOverride } from './servicesApi';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('servicesApi', () => {
  it('searchServices calls correct endpoint', async () => {
    const mockData = { data: { items: [], totalCount: 0 } };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockData);

    const result = await searchServices({ companyId: 1, page: 2 });
    
    expect(axiosClient.get).toHaveBeenCalledWith('/services', {
      params: { companyId: 1, customerId: undefined, status: undefined, page: 2, pageSize: 20 },
    });
    expect(result).toEqual(mockData.data);
  });

  it('getServiceById calls correct endpoint', async () => {
    const mockData = { data: { id: 1, status: 'ACTIVE' } };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockData);

    const result = await getServiceById(1);
    
    expect(axiosClient.get).toHaveBeenCalledWith('/services/1');
    expect(result).toEqual(mockData.data);
  });

  it('createService calls correct endpoint', async () => {
    const mockData = { data: { id: 1 } };
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockData);

    const request = { serviceTypeId: 1, customerId: 1, companyId: 1, validFrom: '2026-01-01' };
    const result = await createService(request);
    
    expect(axiosClient.post).toHaveBeenCalledWith('/services', request);
    expect(result).toEqual(mockData.data);
  });

  it('renewService calls correct endpoint', async () => {
    const mockData = { data: { id: 1 } };
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockData);

    const request = { validFrom: '2027-01-01', rowVersion: 'v1' };
    const result = await renewService(1, request);
    
    expect(axiosClient.post).toHaveBeenCalledWith('/services/1/renew', request);
    expect(result).toEqual(mockData.data);
  });

  it('requestPriceOverride calls correct endpoint', async () => {
    vi.mocked(axiosClient.post).mockResolvedValueOnce({ data: {} });

    const request = { requestedPrice: 1500, reason: 'discount', rowVersion: 'v1' };
    await requestPriceOverride(1, request);
    
    expect(axiosClient.post).toHaveBeenCalledWith('/services/1/request-price-override', request);
  });
});
