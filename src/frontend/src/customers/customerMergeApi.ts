import axiosClient from '../api/axiosClient';
import type {
  CreateCustomerMergeRequest,
  CustomerMergeRequestDto,
  MergeDuplicateSearchParams,
  MergeRequestListParams,
} from './customerMergeTypes';
import type { DuplicateCheckResult, PagedResult } from './types';

const BASE = '/customers';

export async function findMergeDuplicates(
  params: MergeDuplicateSearchParams,
): Promise<DuplicateCheckResult> {
  const { data } = await axiosClient.get<DuplicateCheckResult>(
    `${BASE}/duplicates`,
    {
      params: {
        cccd: params.cccd || undefined,
        phone: params.phone || undefined,
      },
    },
  );
  return data;
}

export async function createMergeRequest(
  request: CreateCustomerMergeRequest,
): Promise<CustomerMergeRequestDto> {
  const { data } = await axiosClient.post<CustomerMergeRequestDto>(
    `${BASE}/merge-requests`,
    request,
  );
  return data;
}

export async function getMergeRequestById(
  id: string,
): Promise<CustomerMergeRequestDto> {
  const { data } = await axiosClient.get<CustomerMergeRequestDto>(
    `${BASE}/merge-requests/${id}`,
  );
  return data;
}

export async function listMergeRequests(
  params: MergeRequestListParams = {},
): Promise<PagedResult<CustomerMergeRequestDto>> {
  const { data } = await axiosClient.get<
    PagedResult<CustomerMergeRequestDto>
  >(`${BASE}/merge-requests`, {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}
