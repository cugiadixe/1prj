import axiosClient from '../api/axiosClient';
import type {
  CreateEmergencyContactRequest,
  CreateGraveOccupantRequest,
  CreateGraveRequest,
  GraveDetail,
  GraveEmergencyContact,
  GraveListItem,
  GraveOccupant,
  GraveSearchParams,
  OwnershipHistoryItem,
  PagedResult,
  TransferOwnershipRequest,
  TransferOwnershipResult,
  UpdateEmergencyContactRequest,
  UpdateGraveOccupantRequest,
  UpdateGraveRequest,
} from './types';

const BASE = '/graves';

export async function searchGraves(
  params: GraveSearchParams = {},
): Promise<PagedResult<GraveListItem>> {
  const { data } = await axiosClient.get<PagedResult<GraveListItem>>(BASE, {
    params: {
      search: params.search || undefined,
      zone: params.zone || undefined,
      status: params.status || undefined,
      graveType: params.graveType || undefined,
      ownerCustomerId: params.ownerCustomerId || undefined,
      tagIds: params.tagIds && params.tagIds.length > 0 ? params.tagIds : undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
    paramsSerializer: { indexes: null },
  });
  return data;
}

export interface GraveAttachmentSummary {
  graveId: number;
  graveCode: string;
  zone: string;
  graveType: string;
  ownerName: string | null;
  cemeteryName: string | null;
  photoCount: number;
  transferDocCount: number;
  otherCount: number;
  totalCount: number;
  lastUploadedAt: string | null;
}

export interface AttachmentSummaryParams {
  search?: string;
  zone?: string;
  category?: string;
  uploadedByUserId?: number;
  uploadedFrom?: string;
  uploadedTo?: string;
  page?: number;
  pageSize?: number;
}

export async function getAttachmentSummary(
  params: AttachmentSummaryParams = {},
): Promise<PagedResult<GraveAttachmentSummary>> {
  const { data } = await axiosClient.get<PagedResult<GraveAttachmentSummary>>(`${BASE}/attachments-summary`, {
    params: {
      search: params.search || undefined,
      zone: params.zone || undefined,
      category: params.category || undefined,
      uploadedByUserId: params.uploadedByUserId || undefined,
      uploadedFrom: params.uploadedFrom || undefined,
      uploadedTo: params.uploadedTo || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}

export interface AttachmentUploader {
  userId: number;
  name: string;
}

export async function getAttachmentUploaders(): Promise<AttachmentUploader[]> {
  const { data } = await axiosClient.get<AttachmentUploader[]>(`${BASE}/attachment-uploaders`);
  return data;
}

export async function getGraveById(id: number): Promise<GraveDetail> {
  const { data } = await axiosClient.get<GraveDetail>(`${BASE}/${id}`);
  return data;
}

export async function createGrave(request: CreateGraveRequest): Promise<GraveDetail> {
  const { data } = await axiosClient.post<GraveDetail>(BASE, request);
  return data;
}

export async function updateGrave(
  id: number,
  request: UpdateGraveRequest,
): Promise<GraveDetail> {
  const { data } = await axiosClient.put<GraveDetail>(`${BASE}/${id}`, request);
  return data;
}

export async function addOccupant(
  graveId: number,
  request: CreateGraveOccupantRequest,
): Promise<GraveOccupant> {
  const { data } = await axiosClient.post<GraveOccupant>(
    `${BASE}/${graveId}/occupants`,
    request,
  );
  return data;
}

export async function updateOccupant(
  graveId: number,
  occupantId: number,
  request: UpdateGraveOccupantRequest,
): Promise<GraveOccupant> {
  const { data } = await axiosClient.put<GraveOccupant>(
    `${BASE}/${graveId}/occupants/${occupantId}`,
    request,
  );
  return data;
}

export async function addEmergencyContact(
  graveId: number,
  request: CreateEmergencyContactRequest,
): Promise<GraveEmergencyContact> {
  const { data } = await axiosClient.post<GraveEmergencyContact>(
    `${BASE}/${graveId}/emergency-contacts`,
    request,
  );
  return data;
}

export async function updateEmergencyContact(
  graveId: number,
  contactId: number,
  request: UpdateEmergencyContactRequest,
): Promise<GraveEmergencyContact> {
  const { data } = await axiosClient.put<GraveEmergencyContact>(
    `${BASE}/${graveId}/emergency-contacts/${contactId}`,
    request,
  );
  return data;
}

export async function removeEmergencyContact(
  graveId: number,
  contactId: number,
): Promise<void> {
  await axiosClient.delete(`${BASE}/${graveId}/emergency-contacts/${contactId}`);
}

export async function transferOwnership(
  graveId: number,
  request: TransferOwnershipRequest,
): Promise<TransferOwnershipResult> {
  const { data } = await axiosClient.post<TransferOwnershipResult>(
    `${BASE}/${graveId}/transfer-owner`,
    request,
  );
  return data;
}

export async function getOwnershipHistory(
  graveId: number,
): Promise<OwnershipHistoryItem[]> {
  const { data } = await axiosClient.get<OwnershipHistoryItem[]>(
    `${BASE}/${graveId}/ownership-history`,
  );
  return data;
}
