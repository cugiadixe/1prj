export const CUSTOMER_STATUS_LABELS: Record<string, string> = {
  ACTIVE: 'Đang hoạt động',
  INACTIVE: 'Ngừng giao dịch',
  MERGED: 'Đã gộp',
  DECEASED: 'Đã mất',
};

export const CUSTOMER_STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'green',
  INACTIVE: 'red',
  MERGED: 'default',
  DECEASED: 'volcano',
};

export type { Tag } from '../tags/types';

export interface CustomerListItem {
  id: number;
  customerCode: string;
  fullName: string;
  cccd: string | null;
  phone: string | null;
  customerStatus: string;
  createdAt: string;
  tags?: import('../tags/types').Tag[];
}

export interface ProfileInfo {
  id: number;
  fullName: string;
  cccd: string | null;
  dob: string | null;
  dobPartial: string | null;
  dobPrecision: string | null;
  gender: string | null;
  permanentAddress: string | null;
  cccdIssueDate: string | null;
  cccdIssuePlace: string | null;
  taxCode: string | null;
  phone: string | null;
  contactAddress: string | null;
  deathDateSolar: string | null;
  deathDateLunar: string | null;
  deathPlace: string | null;
  hometown: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface CustomerDetail {
  id: number;
  customerCode: string;
  customerStatus: string;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
  profile: ProfileInfo;
  tags?: import('../tags/types').Tag[];
}

export interface CustomerCompanyContext {
  id: number;
  customerId: number;
  companyId: number;
  companyName: string | null;
  assignedStaffId: number | null;
  assignedStaffName: string | null;
  relationshipStatus: string;
  internalNotes: string | null;
  firstInteractionAt: string | null;
  lastInteractionAt: string | null;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateCustomerRequest {
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

export interface UpdateCustomerRequest {
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
  reason: string;
  targetVersion: string;
}

export interface CreateCompanyContextRequest {
  companyId: number;
  assignedStaffId?: number | null;
  internalNotes?: string | null;
  firstInteractionAt?: string | null;
}

export interface UpdateCompanyContextRequest {
  assignedStaffId?: number | null;
  relationshipStatus: string;
  internalNotes?: string | null;
  lastInteractionAt?: string | null;
  targetVersion: string;
}

export interface DuplicateCheckRequest {
  cccd?: string;
  phone?: string;
}

export interface DuplicateCheckResult {
  hasDuplicates: boolean;
  matches: CustomerListItem[];
}

export interface CustomerSearchParams {
  search?: string;
  customerStatus?: string;
  companyId?: number;
  assignedStaffId?: number;
  unassignedStaff?: boolean;
  tagIds?: number[];
  page?: number;
  pageSize?: number;
}

export interface CompanyLookup {
  id: number;
  name: string;
}

export interface StaffLookup {
  id: number;
  fullName: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
