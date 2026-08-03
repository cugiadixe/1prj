import { describe, it, expect, vi } from 'vitest';
import axiosClient from '../api/axiosClient';
import { searchServiceTypes, getServiceTypeById, createServiceType, updateServiceType, deactivateServiceType } from './serviceTypesApi';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('serviceTypesApi', () => {
  it('searchServiceTypes calls correct endpoint', async () => {
    const mockData = { data: { items: [], totalCount: 0 } };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockData);

    const result = await searchServiceTypes({ page: 2 });
    
    expect(axiosClient.get).toHaveBeenCalledWith('/service-types', {
      params: { page: 2, pageSize: 20 },
    });
    expect(result).toEqual(mockData.data);
  });

  it('getServiceTypeById calls correct endpoint', async () => {
    const mockData = { data: { id: 1, name: 'Test' } };
    vi.mocked(axiosClient.get).mockResolvedValueOnce(mockData);

    const result = await getServiceTypeById(1);
    
    expect(axiosClient.get).toHaveBeenCalledWith('/service-types/1');
    expect(result).toEqual(mockData.data);
  });

  it('createServiceType calls correct endpoint', async () => {
    const mockData = { data: { id: 1, name: 'Test' } };
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockData);

    const request = { code: 'T1', name: 'Test', standardPrice: 1000 };
    const result = await createServiceType(request);
    
    expect(axiosClient.post).toHaveBeenCalledWith('/service-types', request);
    expect(result).toEqual(mockData.data);
  });

  it('updateServiceType calls correct endpoint', async () => {
    const mockData = { data: { id: 1, name: 'Updated' } };
    vi.mocked(axiosClient.put).mockResolvedValueOnce(mockData);

    const request = { name: 'Updated', rowVersion: 'v1' };
    const result = await updateServiceType(1, request);
    
    expect(axiosClient.put).toHaveBeenCalledWith('/service-types/1', request);
    expect(result).toEqual(mockData.data);
  });

  it('deactivateServiceType calls correct endpoint', async () => {
    const mockData = { data: { id: 1, isActive: false } };
    vi.mocked(axiosClient.post).mockResolvedValueOnce(mockData);

    const result = await deactivateServiceType(1, 'v1');
    
    expect(axiosClient.post).toHaveBeenCalledWith('/service-types/1/deactivate', { rowVersion: 'v1' });
    expect(result).toEqual(mockData.data);
  });
});
