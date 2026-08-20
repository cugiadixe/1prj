export interface CreateCustomerMasterChangeRequest {
  targetCustomerId: number;
  targetRowVersion: string;
  fullName?: string | null;
  cccd?: string | null;
  dob?: string | null;
  dobPartial?: string | null;
  dobPrecision?: string | null;
  gender?: string | null;
  permanentAddress?: string | null;
  cccdIssueDate?: string | null;
  cccdIssuePlace?: string | null;
  taxCode?: string | null;
  phone?: string | null;
  contactAddress?: string | null;
  deathDateSolar?: string | null;
  deathDateLunar?: string | null;
  deathPlace?: string | null;
  hometown?: string | null;
  reason: string;
}

export interface CustomerMasterChangeDto {
  id: number;
  processCode: string;
  requesterId: number;
  companyId: number | null;
  requestStatus: string;
  workflowInstanceId: number | null;
  targetCustomerId: number | null;
  targetCustomerCode: string | null;
  targetCustomerName: string | null;
  targetRowVersion: string | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
  payload: CreateCustomerMasterChangeRequest | null;
}
