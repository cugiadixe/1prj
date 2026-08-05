import axiosClient from '../api/axiosClient';
import type {
  CarePackageRequestDto,
  CreateCarePackageRequest,
  ApproveRejectRequest,
  CreatePaymentRequest,
  CarePackagePaymentStatusDto,
} from './types';

const BASE_URL = '/care-packages';

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
}

export const listCarePackageRequests = async (params?: Record<string, any>): Promise<PaginatedResult<CarePackageRequestDto>> => {
  const response = await axiosClient.get<PaginatedResult<CarePackageRequestDto>>(BASE_URL, { params });
  return response.data;
};

export const getCarePackageRequest = async (id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.get<CarePackageRequestDto>(`${BASE_URL}/${id}`);
  return response.data;
};

export const createCarePackageRequest = async (data: CreateCarePackageRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(BASE_URL, data);
  return response.data;
};

export const submitCarePackageRequest = async (id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/submit`);
  return response.data;
};

export const approveCarePackageRequest = async (id: number, data: ApproveRejectRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/approve`, data);
  return response.data;
};

export const rejectCarePackageRequest = async (id: number, data: ApproveRejectRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/reject`, data);
  return response.data;
};

export const createCarePackagePayment = async (id: number, data: CreatePaymentRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/create-payment`, data);
};

export const getCarePackagePaymentStatus = async (id: number): Promise<CarePackagePaymentStatusDto> => {
  const response = await axiosClient.get<CarePackagePaymentStatusDto>(`${BASE_URL}/${id}/payment-status`);
  return response.data;
};

export const activateCarePackageRequest = async (id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/activate`);
  return response.data;
};
