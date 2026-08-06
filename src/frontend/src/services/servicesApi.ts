import axiosClient from '../api/axiosClient';
import type {
  CreateServiceRequest,
  PagedResult,
  RenewServiceRequest,
  RequestPriceOverrideRequest,
  ServiceDetail,
  ServiceListItem,
  ServiceSearchParams,
} from './types';

const BASE = '/services';

export async function searchServices(
  params: ServiceSearchParams,
): Promise<PagedResult<ServiceListItem>> {
  const { data } = await axiosClient.get<PagedResult<ServiceListItem>>(BASE, {
    params: {
      companyId: params.companyId,
      customerId: params.customerId || undefined,
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}

export async function getServiceById(
  id: number,
): Promise<ServiceDetail> {
  const { data } = await axiosClient.get<ServiceDetail>(`${BASE}/${id}`);
  return data;
}

export async function createService(
  request: CreateServiceRequest,
): Promise<ServiceDetail> {
  const { data } = await axiosClient.post<ServiceDetail>(BASE, request);
  return data;
}

export async function renewService(
  id: number,
  request: RenewServiceRequest,
): Promise<ServiceDetail> {
  const { data } = await axiosClient.post<ServiceDetail>(`${BASE}/${id}/renew`, request);
  return data;
}

export async function requestPriceOverride(
  id: number,
  request: RequestPriceOverrideRequest,
): Promise<void> {
  await axiosClient.post(`${BASE}/${id}/request-price-override`, request);
}
