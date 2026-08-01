import axiosClient from '../api/axiosClient';
import type {
  CreateCustomerProposalRequest,
  CustomerProposalDto,
} from './customerProposalTypes';

const BASE = '/customer-proposals';

export async function createCustomerProposal(
  request: CreateCustomerProposalRequest,
): Promise<CustomerProposalDto> {
  const { data } = await axiosClient.post<CustomerProposalDto>(BASE, request);
  return data;
}

export async function getCustomerProposalById(
  id: number,
): Promise<CustomerProposalDto> {
  const { data } = await axiosClient.get<CustomerProposalDto>(`${BASE}/${id}`);
  return data;
}

export async function getMyCustomerProposals(): Promise<CustomerProposalDto[]> {
  const { data } = await axiosClient.get<CustomerProposalDto[]>(`${BASE}/my-proposals`);
  return data;
}
