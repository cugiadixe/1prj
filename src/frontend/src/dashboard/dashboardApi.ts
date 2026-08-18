import axiosClient from '../api/axiosClient';

export interface DashboardCountItem {
  key: string;
  count: number;
}
export interface DashboardMonthAmount {
  month: string;
  amount: number;
}
export interface DashboardMonthCount {
  month: string;
  count: number;
}

export interface DashboardSummary {
  // Người dùng có quyền FINANCE_VIEW_REVENUE ở công ty này không. false => totalRevenue = 0 và
  // revenueByMonth rỗng; dashboard thay hai widget doanh thu bằng widget khác.
  canViewRevenue: boolean;
  totalCustomers: number;
  totalGraves: number;
  occupiedGraves: number;
  totalRevenue: number;
  activeCarePackages: number;
  gravesByStatus: DashboardCountItem[];
  gravesByZone: DashboardCountItem[];
  gravesByType: DashboardCountItem[];
  customersByStatus: DashboardCountItem[];
  carePackagesByStatus: DashboardCountItem[];
  servicesByStatus: DashboardCountItem[];
  revenueByMonth: DashboardMonthAmount[];
  carePackagesByMonth: DashboardMonthCount[];
}

export const getDashboardSummary = async (companyId: number): Promise<DashboardSummary> => {
  const res = await axiosClient.get<DashboardSummary>('/dashboard/summary', {
    headers: { 'X-Company-Id': String(companyId) },
  });
  return res.data;
};
