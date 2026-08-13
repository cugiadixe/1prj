import axiosClient from '../api/axiosClient';
import type {
  CardReprintRequestDto,
  CreateCardReprintRequest,
  SubmitCardReprintRequest,
  ApproveCardReprintRequest,
  RejectCardReprintRequest,
  CreateCardReprintPaymentRequest,
  MarkCardPrintedRequest,
  MarkCardReleasedRequest,
} from './types';

const BASE_URL = '/card-reprint-requests';

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
}

function companyHeaders(companyId: number) {
  return { 'X-Company-Id': companyId.toString() };
}

export const getCardReprintRequests = async (companyId: number, params?: Record<string, any>): Promise<PaginatedResult<CardReprintRequestDto>> => {
  const response = await axiosClient.get(BASE_URL, { params, headers: companyHeaders(companyId) });
  const data = response.data;
  if (Array.isArray(data)) {
    return { items: data, totalCount: data.length };
  }
  return data as PaginatedResult<CardReprintRequestDto>;
};

export const getCardReprintRequest = async (companyId: number, id: number): Promise<CardReprintRequestDto> => {
  const response = await axiosClient.get<CardReprintRequestDto>(`${BASE_URL}/${id}`, { headers: companyHeaders(companyId) });
  return response.data;
};

export const createCardReprintRequest = async (companyId: number, data: CreateCardReprintRequest): Promise<CardReprintRequestDto> => {
  const response = await axiosClient.post<CardReprintRequestDto>(BASE_URL, data, { headers: companyHeaders(companyId) });
  return response.data;
};

export const submitCardReprintRequest = async (companyId: number, id: number, data: SubmitCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/submit`, data, { headers: companyHeaders(companyId) });
};

export const approveCardReprintRequest = async (companyId: number, id: number, data: ApproveCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/approve`, data, { headers: companyHeaders(companyId) });
};

export const rejectCardReprintRequest = async (companyId: number, id: number, data: RejectCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/reject`, data, { headers: companyHeaders(companyId) });
};

export const createPaymentForCardReprint = async (companyId: number, id: number, data: CreateCardReprintPaymentRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/create-payment`, data, { headers: companyHeaders(companyId) });
};

export const getCardReprintPaymentStatus = async (companyId: number, id: number): Promise<{ status: string }> => {
  const response = await axiosClient.get<{ status: string }>(`${BASE_URL}/${id}/payment-status`, { headers: companyHeaders(companyId) });
  return response.data;
};

export const markCardPrinted = async (companyId: number, id: number, data: MarkCardPrintedRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/mark-printed`, data, { headers: companyHeaders(companyId) });
};

export const markCardReleased = async (companyId: number, id: number, data: MarkCardReleasedRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/mark-released`, data, { headers: companyHeaders(companyId) });
};
