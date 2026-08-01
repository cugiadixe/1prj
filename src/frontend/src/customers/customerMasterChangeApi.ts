import axiosClient from '../api/axiosClient';
import type {
  CreateCustomerMasterChangeRequest,
  CustomerMasterChangeDto,
} from './customerMasterChangeTypes';

const BASE = '/customers';

export async function createCustomerMasterChangeRequest(
  customerId: number,
  request: CreateCustomerMasterChangeRequest,
): Promise<CustomerMasterChangeDto> {
  const { data } = await axiosClient.post<CustomerMasterChangeDto>(
    `${BASE}/${customerId}/change-requests`,
    request,
  );
  return data;
}

export async function getCustomerMasterChangeRequestById(
  requestId: number,
): Promise<CustomerMasterChangeDto> {
  const { data } = await axiosClient.get<CustomerMasterChangeDto>(
    `${BASE}/change-requests/${requestId}`,
  );
  return data;
}

export async function getMyCustomerMasterChangeRequests(): Promise<
  CustomerMasterChangeDto[]
> {
  const { data } = await axiosClient.get<CustomerMasterChangeDto[]>(
    `${BASE}/my-change-requests`,
  );
  return data;
}
