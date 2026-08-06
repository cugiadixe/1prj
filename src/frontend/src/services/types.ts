import type { PagedResult } from '../customers/types';

export type { PagedResult };

export interface ServiceTypeListItem {
  id: number;
  code: string;
  name: string;
  description: string | null;
  standardPrice: number;
  standardPriceCurrency: string;
  cycleDurationMonths: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

export interface ServiceTypeDetail {
  id: number;
  code: string;
  name: string;
  description: string | null;
  standardPrice: number;
  standardPriceCurrency: string;
  cycleDurationMonths: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

export interface CreateServiceTypeRequest {
  code: string;
  name: string;
  description?: string | null;
  standardPrice: number;
  cycleDurationMonths?: number | null;
}

export interface UpdateServiceTypeRequest {
  name: string;
  description?: string | null;
  cycleDurationMonths?: number | null;
  rowVersion: string;
}

export interface ServiceListItem {
  id: number;
  serviceTypeId: number;
  serviceTypeCode: string | null;
  serviceTypeName: string | null;
  customerId: number;
  companyId: number;
  status: string;
  appliedPrice: number;
  standardPriceSnapshot: number;
  isOverridePrice: boolean;
  overrideApprovalRequestId: number | null;
  validFrom: string;
  validTo: string | null;
  cycleNumber: number;
  previousServiceId: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

export interface ServiceDetail {
  id: number;
  serviceTypeId: number;
  serviceTypeCode: string | null;
  serviceTypeName: string | null;
  customerId: number;
  companyId: number;
  status: string;
  appliedPrice: number;
  standardPriceSnapshot: number;
  isOverridePrice: boolean;
  overrideApprovalRequestId: number | null;
  validFrom: string;
  validTo: string | null;
  cycleNumber: number;
  previousServiceId: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
}

export interface CreateServiceRequest {
  serviceTypeId: number;
  customerId: number;
  companyId: number;
  validFrom: string;
  validTo?: string | null;
}

export interface RenewServiceRequest {
  validFrom: string;
  validTo?: string | null;
  rowVersion: string;
}

export interface RequestPriceOverrideRequest {
  requestedPrice: number;
  reason: string;
  rowVersion: string;
}

export interface ServiceTypeSearchParams {
  page?: number;
  pageSize?: number;
}

export interface ServiceSearchParams {
  companyId: number;
  customerId?: number;
  status?: string;
  page?: number;
  pageSize?: number;
}
