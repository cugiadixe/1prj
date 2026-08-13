export interface ApprovalAuthority {
  id: number;
  companyId: number;
  companyName: string | null;
  departmentId: number;
  departmentName: string | null;
  processCode: string | null;
  approverUserId: number;
  approverName: string | null;
  authorityLevel: number;
  minAmount: number | null;
  maxAmount: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  delegatedFromUserId: number | null;
  delegatedFromName: string | null;
  status: string;
  notes: string | null;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateApprovalAuthorityRequest {
  companyId: number;
  departmentId: number;
  processCode?: string | null;
  approverUserId: number;
  authorityLevel: number;
  minAmount?: number | null;
  maxAmount?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  delegatedFromUserId?: number | null;
  notes?: string | null;
}

export interface OrgCompany {
  id: number;
  name: string;
}

export interface OrgDepartment {
  id: number;
  name: string;
  companyId: number;
}

export const AA_STATUS_LABELS: Record<string, string> = {
  ACTIVE: 'Đang hiệu lực',
  CLOSED: 'Đã đóng',
};

export const AA_STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'green',
  CLOSED: 'default',
};

export const AUTHORITY_LEVEL_LABELS: Record<number, string> = {
  1: 'Cấp 1 — Trưởng phòng',
  2: 'Cấp 2 — Giám đốc',
};
