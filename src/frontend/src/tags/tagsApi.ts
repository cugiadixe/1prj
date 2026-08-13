import axiosClient from '../api/axiosClient';
import type { SetEntityTagsRequest, Tag, TagType } from './types';

const BASE = '/tags';

export async function listTags(type: TagType, includeInactive = false): Promise<Tag[]> {
  const { data } = await axiosClient.get<Tag[]>(BASE, { params: { type, includeInactive } });
  return data;
}

export async function createTag(type: TagType, name: string, color?: string): Promise<Tag> {
  const { data } = await axiosClient.post<Tag>(BASE, { tagType: type, name, color: color ?? null });
  return data;
}

export async function updateTag(
  id: number,
  request: { name: string; color?: string | null; isActive: boolean; targetVersion: string },
): Promise<Tag> {
  const { data } = await axiosClient.put<Tag>(`${BASE}/${id}`, request);
  return data;
}

export async function deactivateTag(id: number): Promise<void> {
  await axiosClient.delete(`${BASE}/${id}`);
}

export async function setCustomerTags(customerId: number, request: SetEntityTagsRequest): Promise<Tag[]> {
  const { data } = await axiosClient.put<Tag[]>(`${BASE}/customer/${customerId}`, request);
  return data;
}

export async function setGraveTags(graveId: number, request: SetEntityTagsRequest): Promise<Tag[]> {
  const { data } = await axiosClient.put<Tag[]>(`${BASE}/grave/${graveId}`, request);
  return data;
}
