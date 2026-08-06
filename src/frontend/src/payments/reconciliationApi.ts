import axiosClient from '../api/axiosClient';
import type {
  ConfirmReconciliationRequest,
  DailyReconciliationReportDto,
  MonthlyReconciliationReportDto,
  PrepareReconciliationRequest,
  ReconciliationPeriodDto,
} from './types';

const BASE = '/reconciliation';

export async function getDailyReport(
  companyId: number,
  date: string,
): Promise<DailyReconciliationReportDto> {
  const { data } = await axiosClient.get<DailyReconciliationReportDto>(
    `${BASE}/daily`,
    {
      params: { companyId, date },
    },
  );
  return data;
}

export async function getMonthlyReport(
  companyId: number,
  year: number,
  month: number,
): Promise<MonthlyReconciliationReportDto> {
  const { data } = await axiosClient.get<MonthlyReconciliationReportDto>(
    `${BASE}/monthly`,
    {
      params: { companyId, year, month },
    },
  );
  return data;
}

export async function prepareReconciliation(
  periodId: number,
  request: PrepareReconciliationRequest,
): Promise<ReconciliationPeriodDto> {
  const { data } = await axiosClient.post<ReconciliationPeriodDto>(
    `${BASE}/periods/${periodId}/prepare`,
    request,
  );
  return data;
}

export async function confirmReconciliation(
  periodId: number,
  request: ConfirmReconciliationRequest,
): Promise<ReconciliationPeriodDto> {
  const { data } = await axiosClient.post<ReconciliationPeriodDto>(
    `${BASE}/periods/${periodId}/confirm`,
    request,
  );
  return data;
}
