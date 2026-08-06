import axiosClient from '../api/axiosClient';
import type {
  ConfirmPaymentRequest,
  CorrectPaymentRequest,
  CreatePaymentDraftRequest,
  PagedResult,
  PaymentSearchParams,
  PaymentTransactionDto,
  PaymentTransactionListDto,
  SoftDeletePaymentRequest,
} from './types';

const BASE = '/payments';

export async function createDraft(
  request: CreatePaymentDraftRequest,
): Promise<PaymentTransactionDto> {
  const { data } = await axiosClient.post<PaymentTransactionDto>(BASE, request);
  return data;
}

export async function confirmPayment(
  id: number,
  request: ConfirmPaymentRequest,
): Promise<PaymentTransactionDto> {
  const { data } = await axiosClient.post<PaymentTransactionDto>(
    `${BASE}/${id}/confirm`,
    request,
  );
  return data;
}

export async function listPayments(
  params: PaymentSearchParams,
): Promise<PagedResult<PaymentTransactionListDto>> {
  const { data } = await axiosClient.get<PagedResult<PaymentTransactionListDto>>(
    BASE,
    {
      params: {
        companyId: params.companyId,
        customerId: params.customerId || undefined,
        status: params.status || undefined,
        dateFrom: params.dateFrom || undefined,
        dateTo: params.dateTo || undefined,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
      },
    },
  );
  return data;
}

export async function getPaymentById(
  id: number,
): Promise<PaymentTransactionDto> {
  const { data } = await axiosClient.get<PaymentTransactionDto>(
    `${BASE}/${id}`,
  );
  return data;
}

export async function correctConfirmed(
  id: number,
  request: CorrectPaymentRequest,
): Promise<PaymentTransactionDto> {
  const { data } = await axiosClient.post<PaymentTransactionDto>(
    `${BASE}/${id}/correct`,
    request,
  );
  return data;
}

export async function softDeleteDraft(
  id: number,
  request: SoftDeletePaymentRequest,
): Promise<void> {
  await axiosClient.delete(`${BASE}/${id}`, { data: request });
}
