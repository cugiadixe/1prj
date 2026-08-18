import axiosClient from '../api/axiosClient';
import type {
  CreateRelationshipKindRequest,
  RelationshipKindDetail,
  UpdateRelationshipKindRequest,
} from './types';

const BASE = '/relationship-kinds';

export async function getAllRelationshipKinds(): Promise<RelationshipKindDetail[]> {
  const { data } = await axiosClient.get<RelationshipKindDetail[]>(BASE);
  return data;
}

export async function createRelationshipKind(
  request: CreateRelationshipKindRequest,
): Promise<RelationshipKindDetail> {
  const { data } = await axiosClient.post<RelationshipKindDetail>(BASE, request);
  return data;
}

export async function updateRelationshipKind(
  kindCode: string,
  request: UpdateRelationshipKindRequest,
): Promise<void> {
  await axiosClient.put(`${BASE}/${kindCode}`, request);
}

export async function deleteRelationshipKind(kindCode: string): Promise<void> {
  await axiosClient.delete(`${BASE}/${kindCode}`);
}
