import axiosClient from '../api/axiosClient';
import type { GraveAttachment } from './types';

const BASE = '/graves';

export async function listAttachments(graveId: number): Promise<GraveAttachment[]> {
  const { data } = await axiosClient.get<GraveAttachment[]>(`${BASE}/${graveId}/attachments`);
  return data;
}

export async function uploadAttachment(
  graveId: number,
  file: File,
  category: string,
  description?: string,
  ownershipHistoryId?: number,
): Promise<GraveAttachment> {
  const form = new FormData();
  form.append('file', file);
  form.append('category', category);
  if (description) form.append('description', description);
  if (ownershipHistoryId != null) form.append('ownershipHistoryId', String(ownershipHistoryId));
  const { data } = await axiosClient.post<GraveAttachment>(
    `${BASE}/${graveId}/attachments`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}

export async function deleteAttachment(graveId: number, attachmentId: number): Promise<void> {
  await axiosClient.delete(`${BASE}/${graveId}/attachments/${attachmentId}`);
}

/**
 * Tải nội dung file qua axios (kèm token) dạng blob, trả về object URL để hiển thị/mở.
 * Nhớ URL.revokeObjectURL khi không dùng nữa.
 */
export async function fetchAttachmentObjectUrl(
  graveId: number,
  attachmentId: number,
  thumbnail: boolean,
): Promise<string> {
  const { data } = await axiosClient.get(
    `${BASE}/${graveId}/attachments/${attachmentId}/content`,
    { params: { thumbnail }, responseType: 'blob' },
  );
  return URL.createObjectURL(data as Blob);
}
