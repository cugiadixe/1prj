import axiosClient from '../api/axiosClient';
import type {
  CompanyLookup,
  CreateCompanyContextRequest,
  CreateCustomerRequest,
  CustomerCompanyContext,
  CustomerDetail,
  CustomerListItem,
  CustomerOverview,
  CustomerSearchParams,
  DuplicateCheckRequest,
  DuplicateCheckResult,
  PagedResult,
  StaffLookup,
  UpdateCompanyContextRequest,
  UpdateCustomerRequest,
} from './types';

const BASE = '/customers';

export async function searchCustomers(
  params: CustomerSearchParams = {},
): Promise<PagedResult<CustomerListItem>> {
  const { data } = await axiosClient.get<PagedResult<CustomerListItem>>(BASE, {
    params: {
      search: params.search || undefined,
      customerStatus: params.customerStatus || undefined,
      lifeStatus: params.lifeStatus || undefined,
      companyId: params.companyId ?? undefined,
      assignedStaffId: params.assignedStaffId ?? undefined,
      unassignedStaff: params.unassignedStaff ? true : undefined,
      tagIds: params.tagIds && params.tagIds.length > 0 ? params.tagIds : undefined,
      ownsGrave: params.ownsGrave === undefined ? undefined : params.ownsGrave,
      notBuried: params.notBuried === undefined ? undefined : params.notBuried,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
    paramsSerializer: { indexes: null },
  });
  return data;
}

export async function getCompanyLookups(): Promise<CompanyLookup[]> {
  const { data } = await axiosClient.get<CompanyLookup[]>(`${BASE}/lookups/companies`);
  return data;
}

export async function getStaffLookups(): Promise<StaffLookup[]> {
  const { data } = await axiosClient.get<StaffLookup[]>(`${BASE}/lookups/staff`);
  return data;
}

export async function getCustomerById(
  id: number,
): Promise<CustomerDetail> {
  const { data } = await axiosClient.get<CustomerDetail>(`${BASE}/${id}`);
  return data;
}

export async function getCustomerOverview(
  id: number,
): Promise<CustomerOverview> {
  const { data } = await axiosClient.get<CustomerOverview>(`${BASE}/${id}/overview`);
  return data;
}

export async function createCustomer(
  request: CreateCustomerRequest,
): Promise<CustomerDetail> {
  const { data } = await axiosClient.post<CustomerDetail>(BASE, request);
  return data;
}

export async function updateCustomer(
  id: number,
  request: UpdateCustomerRequest,
): Promise<CustomerDetail> {
  const { data } = await axiosClient.put<CustomerDetail>(`${BASE}/${id}`, request);
  return data;
}

export async function getCompanyContexts(
  customerId: number,
): Promise<CustomerCompanyContext[]> {
  const { data } = await axiosClient.get<CustomerCompanyContext[]>(
    `${BASE}/${customerId}/company-contexts`,
  );
  return data;
}

export async function createCompanyContext(
  customerId: number,
  request: CreateCompanyContextRequest,
): Promise<CustomerCompanyContext> {
  const { data } = await axiosClient.post<CustomerCompanyContext>(
    `${BASE}/${customerId}/company-contexts`,
    request,
  );
  return data;
}

export async function updateCompanyContext(
  customerId: number,
  contextId: number,
  request: UpdateCompanyContextRequest,
): Promise<CustomerCompanyContext> {
  const { data } = await axiosClient.put<CustomerCompanyContext>(
    `${BASE}/${customerId}/company-contexts/${contextId}`,
    request,
  );
  return data;
}

export async function checkDuplicates(
  params: DuplicateCheckRequest,
): Promise<DuplicateCheckResult> {
  const { data } = await axiosClient.get<DuplicateCheckResult>(
    `${BASE}/duplicate-check`,
    {
      params: {
        cccd: params.cccd || undefined,
        phone: params.phone || undefined,
      },
    },
  );
  return data;
}
