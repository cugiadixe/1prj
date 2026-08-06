export interface CardReprintRequestDto {
  id: number;
  companyId: number;
  cardId: number;
  requesterId: number;
  requestType: string;
  reprintNumber: number;
  feeAmount: number | null;
  feeCurrency: string | null;
  reasonCode: string | null;
  workflowInstanceId: number | null;
  paymentTransactionId: number | null;
  serviceItemId: number | null;
  status: string;
  notes: string | null;
  printedAt: string | null;
  printedByUserId: number | null;
  releasedAt: string | null;
  releasedByUserId: number | null;
  createdAt: string;
  createdByUserId: number;
  updatedAt: string | null;
  updatedByUserId: number | null;
  rowVersion: string; // Base64
}

export interface CreateCardReprintRequest {
  cardId: number;
  reasonCode?: string;
  notes?: string;
}

export interface SubmitCardReprintRequest {
  rowVersion: string;
}

export interface ApproveCardReprintRequest {
  stepId: number;
  targetVersion: number;
  comment?: string;
}

export interface RejectCardReprintRequest {
  stepId: number;
  targetVersion: number;
  reason: string;
}

export interface CreateCardReprintPaymentRequest {
  rowVersion: string;
}

export interface MarkCardPrintedRequest {
  rowVersion: string;
}

export interface MarkCardReleasedRequest {
  rowVersion: string;
}
