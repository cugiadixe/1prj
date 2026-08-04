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

export const getCardReprintRequests = async (params?: Record<string, any>): Promise<PaginatedResult<CardReprintRequestDto>> => {
  const response = await axiosClient.get<PaginatedResult<CardReprintRequestDto>>(BASE_URL, { params });
  return response.data;
};

export const getCardReprintRequest = async (id: number): Promise<CardReprintRequestDto> => {
  const response = await axiosClient.get<CardReprintRequestDto>(`${BASE_URL}/${id}`);
  return response.data;
};

export const createCardReprintRequest = async (data: CreateCardReprintRequest): Promise<CardReprintRequestDto> => {
  const response = await axiosClient.post<CardReprintRequestDto>(BASE_URL, data);
  return response.data;
};

export const submitCardReprintRequest = async (id: number, data: SubmitCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/submit`, data);
};

export const approveCardReprintRequest = async (id: number, data: ApproveCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/approve`, data);
};

export const rejectCardReprintRequest = async (id: number, data: RejectCardReprintRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/reject`, data);
};

export const createPaymentForCardReprint = async (id: number, data: CreateCardReprintPaymentRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/create-payment`, data);
};

export const getCardReprintPaymentStatus = async (id: number): Promise<{ status: string }> => {
  const response = await axiosClient.get<{ status: string }>(`${BASE_URL}/${id}/payment-status`);
  return response.data;
};

export const markCardPrinted = async (id: number, data: MarkCardPrintedRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/mark-printed`, data);
};

export const markCardReleased = async (id: number, data: MarkCardReleasedRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/mark-released`, data);
};
