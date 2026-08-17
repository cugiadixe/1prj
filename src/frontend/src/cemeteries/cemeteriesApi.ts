import axiosClient from '../api/axiosClient';

const BASE_URL = '/cemeteries';

export interface CemeteryDto {
  id: number;
  cemeteryCode: string;
  name: string;
  address: string | null;
  isActive: boolean;
  cardWatermarkCode: string | null;
}

function companyHeaders(companyId: number) {
  return { 'X-Company-Id': companyId.toString() };
}

export const getCemeteries = async (companyId: number): Promise<CemeteryDto[]> => {
  const { data } = await axiosClient.get<CemeteryDto[]>(BASE_URL, { headers: companyHeaders(companyId) });
  return data;
};

export const setCemeteryWatermark = async (companyId: number, id: number, watermarkCode: string | null): Promise<void> => {
  await axiosClient.put(`${BASE_URL}/${id}/watermark`, { watermarkCode }, { headers: companyHeaders(companyId) });
};

/** Danh mục mẫu hoa văn dựng sẵn (giai đoạn 1). */
export const WATERMARK_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Không hoa văn' },
  { value: 'LOTUS', label: 'Hoa sen' },
  { value: 'FRAME_CLASSIC', label: 'Khung cổ' },
  { value: 'DIAGONAL_TEXT', label: 'Chữ chéo' },
];
