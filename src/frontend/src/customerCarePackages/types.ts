export interface CustomerCarePackage {
  id: number;
  customerId: number;
  customerName: string | null;
  serviceTypeId: number;
  serviceTypeName: string | null;
  cycleDurationMonths: number | null;
  graveId: number | null;
  graveCode: string | null;
  graveCotCount: number | null;
  cotCount: number;
  unitPrice: number;
  totalPrice: number;
  // Cách tính giá của loại dịch vụ: 'PER_COT' (× số cốt) hoặc 'PER_GRAVE' (theo phần mộ, × 1).
  pricingBasis?: string | null;
  startDate: string;
  endDate: string | null;
  status: string;
  notes: string | null;
  workflowInstanceId: number | null;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateCustomerCarePackageRequest {
  customerId: number;
  serviceTypeId: number;
  cotCount: number;
  startDate: string;
  notes?: string | null;
}

export const CCP_STATUS_LABELS: Record<string, string> = {
  PENDING_APPROVAL: 'Chờ duyệt',
  PENDING_GRAVE: 'Chờ gán mộ',
  ACTIVE: 'Đang hiệu lực',
  EXPIRED: 'Hết hạn',
  CANCELLED: 'Đã hủy',
};

export const CCP_STATUS_COLORS: Record<string, string> = {
  PENDING_APPROVAL: 'gold',
  PENDING_GRAVE: 'orange',
  ACTIVE: 'green',
  EXPIRED: 'default',
  CANCELLED: 'red',
};
