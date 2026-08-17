import axiosClient from '../api/axiosClient';

const BASE_URL = '/cards';

/** Thẻ mộ (một mộ = một thẻ đang hoạt động). */
export interface CardDto {
  id: number;
  companyId: number;
  graveId: string | null; // mã mộ (grave_code)
  cardNumber: string | null;
  serviceId: number | null;
  printCount: number;
  status: string;
  createdAt: string;
}

export interface CreateCardRequest {
  graveId: number; // id số của phần mộ
  serviceId?: number;
}

function companyHeaders(companyId: number) {
  return { 'X-Company-Id': companyId.toString() };
}

export const getCards = async (companyId: number): Promise<CardDto[]> => {
  const { data } = await axiosClient.get<CardDto[]>(BASE_URL, { headers: companyHeaders(companyId) });
  return data;
};

export const getCard = async (companyId: number, id: number): Promise<CardDto> => {
  const { data } = await axiosClient.get<CardDto>(`${BASE_URL}/${id}`, { headers: companyHeaders(companyId) });
  return data;
};

export const createCard = async (companyId: number, req: CreateCardRequest): Promise<CardDto> => {
  const { data } = await axiosClient.post<CardDto>(BASE_URL, req, { headers: companyHeaders(companyId) });
  return data;
};

/** Tải PDF thẻ mộ (khổ B5 gập đôi, 4 mặt) dạng blob — có kèm token + X-Company-Id. */
export const fetchCardPdf = async (companyId: number, id: number): Promise<Blob> => {
  const { data } = await axiosClient.get(`${BASE_URL}/${id}/document.pdf`, {
    headers: companyHeaders(companyId),
    responseType: 'blob',
  });
  return data as Blob;
};
