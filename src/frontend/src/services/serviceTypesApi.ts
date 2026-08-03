import axiosClient from '../api/axiosClient';
import type {
  CreateServiceTypeRequest,
  PagedResult,
  ServiceTypeDetail,
  ServiceTypeListItem,
  ServiceTypeSearchParams,
  UpdateServiceTypeRequest,
} from './types';

const BASE = '/service-types';

export async function searchServiceTypes(
  params: ServiceTypeSearchParams = {},
): Promise<PagedResult<ServiceTypeListItem>> {
  const { data } = await axiosClient.get<PagedResult<ServiceTypeListItem>>(BASE, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}

export async function getServiceTypeById(
  id: number,
): Promise<ServiceTypeDetail> {
  const { data } = await axiosClient.get<ServiceTypeDetail>(`${BASE}/${id}`);
  return data;
}

export async function createServiceType(
  request: CreateServiceTypeRequest,
): Promise<ServiceTypeDetail> {
  const { data } = await axiosClient.post<ServiceTypeDetail>(BASE, request);
  return data;
}

export async function updateServiceType(
  id: number,
  request: UpdateServiceTypeRequest,
): Promise<ServiceTypeDetail> {
  const { data } = await axiosClient.put<ServiceTypeDetail>(`${BASE}/${id}`, request);
  return data;
}

export async function deactivateServiceType(
  id: number,
  rowVersion: string,
): Promise<ServiceTypeDetail> {
  const { data } = await axiosClient.post<ServiceTypeDetail>(
    `${BASE}/${id}/deactivate`,
    { rowVersion },
  );
  return data;
}
