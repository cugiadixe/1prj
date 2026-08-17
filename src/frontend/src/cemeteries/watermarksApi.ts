import axiosClient from '../api/axiosClient';

const BASE_URL = '/card-watermarks';

export interface CardWatermarkDto {
  id: number;
  name: string;
  contentType: string;
  createdAt: string;
  code: string; // "UPLOAD:{id}"
}

function companyHeaders(companyId: number) {
  return { 'X-Company-Id': companyId.toString() };
}

export const listWatermarks = async (companyId: number): Promise<CardWatermarkDto[]> => {
  const { data } = await axiosClient.get<CardWatermarkDto[]>(BASE_URL, { headers: companyHeaders(companyId) });
  return data;
};

export const uploadWatermark = async (companyId: number, name: string, file: File): Promise<CardWatermarkDto> => {
  const form = new FormData();
  form.append('file', file);
  form.append('name', name);
  const { data } = await axiosClient.post<CardWatermarkDto>(BASE_URL, form, {
    headers: { ...companyHeaders(companyId), 'Content-Type': 'multipart/form-data' },
  });
  return data;
};

export const deleteWatermark = async (companyId: number, id: number): Promise<void> => {
  await axiosClient.delete(`${BASE_URL}/${id}`, { headers: companyHeaders(companyId) });
};

export const fetchWatermarkThumb = async (companyId: number, id: number): Promise<Blob> => {
  const { data } = await axiosClient.get(`${BASE_URL}/${id}/content`, {
    headers: companyHeaders(companyId),
    responseType: 'blob',
  });
  return data as Blob;
};
