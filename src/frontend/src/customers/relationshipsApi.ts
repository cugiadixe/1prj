import axiosClient from '../api/axiosClient';
import type {
  CreateCustomerRelationshipRequest,
  CustomerRelationship,
  PagedResult,
  RelationshipKind,
  RelationshipListItem,
  RelationshipSearchParams,
} from './types';

const BASE = '/customers';

export async function searchRelationships(
  params: RelationshipSearchParams = {},
): Promise<PagedResult<RelationshipListItem>> {
  const { data } = await axiosClient.get<PagedResult<RelationshipListItem>>(`${BASE}/relationships`, {
    params: {
      search: params.search || undefined,
      kind: params.kind || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}

export async function getRelationshipKinds(): Promise<RelationshipKind[]> {
  const { data } = await axiosClient.get<RelationshipKind[]>(`${BASE}/relationship-kinds`);
  return data;
}

export async function getCustomerRelationships(customerId: number): Promise<CustomerRelationship[]> {
  const { data } = await axiosClient.get<CustomerRelationship[]>(`${BASE}/${customerId}/relationships`);
  return data;
}

export async function createCustomerRelationship(
  customerId: number,
  request: CreateCustomerRelationshipRequest,
): Promise<CustomerRelationship> {
  const { data } = await axiosClient.post<CustomerRelationship>(
    `${BASE}/${customerId}/relationships`,
    request,
  );
  return data;
}

export async function deleteCustomerRelationship(
  customerId: number,
  relationshipId: number,
): Promise<void> {
  await axiosClient.delete(`${BASE}/${customerId}/relationships/${relationshipId}`);
}
