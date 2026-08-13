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

function companyHeaders(companyId: number) {
  return { 'X-Company-Id': companyId.toString() };
}

export const listCarePackageRequests = async (companyId: number, params?: Record<string, any>): Promise<PaginatedResult<CarePackageRequestDto>> => {
  const response = await axiosClient.get<PaginatedResult<CarePackageRequestDto>>(BASE_URL, { params, headers: companyHeaders(companyId) });
  return response.data;
};

export const getCarePackageRequest = async (companyId: number, id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.get<CarePackageRequestDto>(`${BASE_URL}/${id}`, { headers: companyHeaders(companyId) });
  return response.data;
};

export const createCarePackageRequest = async (companyId: number, data: CreateCarePackageRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(BASE_URL, data, { headers: companyHeaders(companyId) });
  return response.data;
};

export const submitCarePackageRequest = async (companyId: number, id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/submit`, undefined, { headers: companyHeaders(companyId) });
  return response.data;
};

export const approveCarePackageRequest = async (companyId: number, id: number, data: ApproveRejectRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/approve`, data, { headers: companyHeaders(companyId) });
  return response.data;
};

export const rejectCarePackageRequest = async (companyId: number, id: number, data: ApproveRejectRequest): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/reject`, data, { headers: companyHeaders(companyId) });
  return response.data;
};

export const createCarePackagePayment = async (companyId: number, id: number, data: CreatePaymentRequest): Promise<void> => {
  await axiosClient.post(`${BASE_URL}/${id}/create-payment`, data, { headers: companyHeaders(companyId) });
};

export const getCarePackagePaymentStatus = async (companyId: number, id: number): Promise<CarePackagePaymentStatusDto> => {
  const response = await axiosClient.get<CarePackagePaymentStatusDto>(`${BASE_URL}/${id}/payment-status`, { headers: companyHeaders(companyId) });
  return response.data;
};

export const activateCarePackageRequest = async (companyId: number, id: number): Promise<CarePackageRequestDto> => {
  const response = await axiosClient.post<CarePackageRequestDto>(`${BASE_URL}/${id}/activate`, undefined, { headers: companyHeaders(companyId) });
  return response.data;
};
