export interface CarePackageRequestItemDto {
  id: number;
  carePackageRequestId: number;
  graveId: string | null;
  cotCountSnapshot: number;
  servicePeriodStartDate: string;
  servicePeriodEndDate: string;
  unitPriceSnapshot: number;
  lineSubtotal: number;
  notes: string | null;
}

export interface CarePackageRequestDto {
  id: number;
  companyId: number;
  customerId: number;
  customerName: string | null;
  customerCode: string | null;
  serviceName: string | null;
  status: string;
  requiresApproval: boolean;
  workflowInstanceId: number | null;
  serviceId: number | null;
  saleDate: string;
  subtotalAmount: number;
  discountAmount: number;
  discountReason: string | null;
  totalAmount: number;
  paymentTransactionId: number | null;
  previousRequestId: number | null;
  createdAt: string;
  createdByUserId: number;
  updatedAt: string | null;
  updatedByUserId: number | null;
  items: CarePackageRequestItemDto[];
}

export interface CreateCarePackageRequestItem {
  // Mã phần mộ (số) — bắt buộc; số cốt được server lấy tự động từ phần mộ.
  graveId: number;
  servicePeriodStartDate: string;
}

export interface CreateCarePackageRequest {
  customerId: number;
  // Gói chăm sóc chọn từ DANH MỤC dịch vụ (ServiceType có isCarePackage).
  serviceTypeId: number;
  saleDate: string;
  discountAmount: number;
  discountReason?: string;
  item: CreateCarePackageRequestItem;
}

export interface ApproveRejectRequest {
  stepId: number;
  targetVersion: number;
  reason?: string;
  comment?: string;
}

export interface CreatePaymentRequest {
  paymentMethod: string;
}

export interface CarePackagePaymentStatusDto {
  status: string;
}
