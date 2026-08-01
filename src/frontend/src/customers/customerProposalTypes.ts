export interface CreateCustomerProposalRequest {
  customerCode: string;
  fullName: string;
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
  initialCompanyId?: number | null;
  assignedStaffId?: number | null;
  internalNotes?: string | null;
}

export interface CustomerProposalSummaryDto {
  customerCode: string;
  fullName: string;
  companyId: number | null;
}

export interface CustomerProposalDto {
  id: number;
  processCode: string;
  requesterId: number;
  companyId: number | null;
  requestStatus: string;
  workflowInstanceId: number | null;
  createdCustomerId: number | null;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: string;
  summary: CustomerProposalSummaryDto | null;
}
