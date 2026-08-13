import { describe, it, expect, vi, beforeEach } from 'vitest';
import axiosClient from '../api/axiosClient';
import {
  searchCustomers,
  getCustomerById,
  createCustomer,
  updateCustomer,
  getCompanyContexts,
  createCompanyContext,
  updateCompanyContext,
  checkDuplicates,
  getCompanyLookups,
  getStaffLookups,
} from './customersApi';

vi.mock('../api/axiosClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

const mockAxios = vi.mocked(axiosClient);

describe('customersApi', () => {
  beforeEach(() => vi.clearAllMocks());

  it('searchCustomers calls GET /customers', async () => {
    mockAxios.get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
    const result = await searchCustomers({ search: 'test', page: 2 });
    expect(mockAxios.get).toHaveBeenCalledWith('/customers', {
      params: { search: 'test', customerStatus: undefined, page: 2, pageSize: 20 },
    });
    expect(result.items).toEqual([]);
  });

  it('searchCustomers forwards company/staff filters', async () => {
    mockAxios.get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
    await searchCustomers({ companyId: 3, assignedStaffId: 7 });
    expect(mockAxios.get).toHaveBeenCalledWith('/customers', {
      params: {
        search: undefined,
        customerStatus: undefined,
        companyId: 3,
        assignedStaffId: 7,
        unassignedStaff: undefined,
        page: 1,
        pageSize: 20,
      },
    });
  });

  it('searchCustomers forwards unassignedStaff as true', async () => {
    mockAxios.get.mockResolvedValue({ data: { items: [], totalCount: 0, page: 1, pageSize: 20 } });
    await searchCustomers({ unassignedStaff: true });
    expect(mockAxios.get).toHaveBeenCalledWith('/customers', {
      params: expect.objectContaining({ unassignedStaff: true }),
    });
  });

  it('getCompanyLookups calls GET /customers/lookups/companies', async () => {
    mockAxios.get.mockResolvedValue({ data: [{ id: 1, name: 'PTKD HCM' }] });
    const result = await getCompanyLookups();
    expect(mockAxios.get).toHaveBeenCalledWith('/customers/lookups/companies');
    expect(result).toEqual([{ id: 1, name: 'PTKD HCM' }]);
  });

  it('getStaffLookups calls GET /customers/lookups/staff', async () => {
    mockAxios.get.mockResolvedValue({ data: [{ id: 2, fullName: 'Nguyen Van A' }] });
    const result = await getStaffLookups();
    expect(mockAxios.get).toHaveBeenCalledWith('/customers/lookups/staff');
    expect(result).toEqual([{ id: 2, fullName: 'Nguyen Van A' }]);
  });

  it('getCustomerById calls GET /customers/:id', async () => {
    mockAxios.get.mockResolvedValue({ data: { id: 1 } });
    await getCustomerById(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/customers/1');
  });

  it('createCustomer calls POST /customers', async () => {
    const req = { customerCode: 'C1', fullName: 'Test' };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createCustomer(req);
    expect(mockAxios.post).toHaveBeenCalledWith('/customers', req);
  });

  it('updateCustomer calls PUT /customers/:id', async () => {
    const req = { fullName: 'Updated', reason: 'fix', targetVersion: 'AA' };
    mockAxios.put.mockResolvedValue({ data: { id: 1 } });
    await updateCustomer(1, req as never);
    expect(mockAxios.put).toHaveBeenCalledWith('/customers/1', req);
  });

  it('getCompanyContexts calls GET /customers/:id/company-contexts', async () => {
    mockAxios.get.mockResolvedValue({ data: [] });
    await getCompanyContexts(1);
    expect(mockAxios.get).toHaveBeenCalledWith('/customers/1/company-contexts');
  });

  it('createCompanyContext calls POST /customers/:id/company-contexts', async () => {
    const req = { companyId: 10 };
    mockAxios.post.mockResolvedValue({ data: { id: 1 } });
    await createCompanyContext(1, req);
    expect(mockAxios.post).toHaveBeenCalledWith('/customers/1/company-contexts', req);
  });

  it('updateCompanyContext calls PUT /customers/:id/company-contexts/:contextId', async () => {
    const req = { relationshipStatus: 'ACTIVE', targetVersion: 'BB' };
    mockAxios.put.mockResolvedValue({ data: { id: 1 } });
    await updateCompanyContext(1, 5, req as never);
    expect(mockAxios.put).toHaveBeenCalledWith('/customers/1/company-contexts/5', req);
  });

  it('checkDuplicates calls GET /customers/duplicate-check', async () => {
    mockAxios.get.mockResolvedValue({ data: { hasDuplicates: false, matches: [] } });
    await checkDuplicates({ cccd: '123' });
    expect(mockAxios.get).toHaveBeenCalledWith('/customers/duplicate-check', {
      params: { cccd: '123', phone: undefined },
    });
  });
});
