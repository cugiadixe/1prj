export interface CreatePaymentDraftRequest {
  customerId: number;
  companyId: number;
  paymentMethod: string;
  paymentDate: string;
  notes?: string;
  items: CreatePaymentItemRequest[];
}

export interface CreatePaymentItemRequest {
  serviceId: number;
  amount: number;
  description?: string;
}

export interface ConfirmPaymentRequest {
  rowVersion: string;
}

export interface CorrectPaymentRequest {
  customerId?: number;
  companyId?: number;
  paymentMethod?: string;
  paymentDate?: string;
  notes?: string;
  items?: CreatePaymentItemRequest[];
  reason: string;
  rowVersion: string;
}

export interface SoftDeletePaymentRequest {
  rowVersion: string;
}

export interface PaymentTransactionItemDto {
  id: number;
  paymentTransactionId: number;
  serviceId: number;
  serviceTypeCode: string;
  serviceCycleNumber: number;
  amount: number;
  description?: string;
  createdAt: string;
}

export interface PaymentTransactionDto {
  id: number;
  billCode: string;
  companyId: number;
  customerId: number;
  paymentMethod: string;
  paymentDate: string;
  totalAmount: number;
  currencyCode: string;
  status: string;
  notes?: string;
  confirmedAt?: string;
  confirmedByUserId?: number;
  createdByUserId: number;
  createdAt: string;
  updatedAt?: string;
  rowVersion: string;
  items: PaymentTransactionItemDto[];
}

export interface PaymentTransactionListDto {
  id: number;
  billCode: string;
  companyId: number;
  customerId: number;
  paymentMethod: string;
  paymentDate: string;
  totalAmount: number;
  status: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface PaymentSearchParams {
  companyId: number;
  customerId?: number;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface ReconciliationPeriodDto {
  id: number;
  companyId: number;
  periodType: string;
  periodDate: string;
  status: string;
  totalAmount: number;
  transactionCount: number;
  preparedByUserId?: number;
  preparedAt?: string;
  confirmedByUserId?: number;
  confirmedAt?: string;
  rowVersion: string;
}

export interface DailyReconciliationReportDto {
  companyId: number;
  date: string;
  period?: ReconciliationPeriodDto;
  payments: PaymentTransactionListDto[];
  totalAmount: number;
  transactionCount: number;
}

export interface DailySummaryDto {
  date: string;
  totalAmount: number;
  transactionCount: number;
  periodStatus?: string;
}

export interface MonthlyReconciliationReportDto {
  companyId: number;
  year: number;
  month: number;
  dailySummaries: DailySummaryDto[];
  monthlyTotalAmount: number;
  monthlyTransactionCount: number;
}

export interface PrepareReconciliationRequest {
  rowVersion: string;
}

export interface ConfirmReconciliationRequest {
  rowVersion: string;
}
