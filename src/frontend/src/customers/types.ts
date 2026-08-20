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

export interface CustomerCompanyBrief {
  companyId: number;
  companyName: string | null;
  assignedStaffId: number | null;
  assignedStaffName: string | null;
}

export interface OwnedGrave {
  graveId: number;
  graveCode: string;
}

// ─── Bảng điều khiển 360 (mộ sở hữu + mộ được an táng) ───
export interface OverviewGrave {
  graveId: number;
  graveCode: string;
  cemeteryName: string | null;
  zone: string;
  plotNumber: string;
  graveType: string;
  status: string;
  cotCount: number;              // sức chứa
  activeOccupantCount: number;   // số cốt đang an táng
}

export interface BuriedInGrave {
  graveId: number;
  graveCode: string;
  cemeteryName: string | null;
  zone: string;
  graveStatus: string;
  occupantStatus: string;        // ACTIVE / RELOCATED
  burialDate: string | null;
  relocatedAt: string | null;
  deceasedRelationship: string | null;
  ownerCustomerId: number | null;
  ownerName: string | null;
}

export interface CustomerOverview {
  ownedGraves: OverviewGrave[];
  buriedIn: BuriedInGrave[];
  // true nếu người xem KHÔNG có quyền GRAVE_VIEW (dữ liệu mộ để rỗng có chủ đích).
  graveAccessDenied: boolean;
}

/// Tham chiếu gọn tới một phần mộ (dùng trong đan chéo quan hệ).
export interface GraveRef {
  graveId: number;
  graveCode: string;
}

export interface CustomerListItem {
  id: number;
  customerCode: string;
  fullName: string;
  cccd: string | null;
  phone: string | null;
  customerStatus: string;
  // Suy từ customerStatus === 'DECEASED': khách đã mất (đã thành cốt trong mộ). Chỉ endpoint danh
  // sách trả về; optional để các nơi dựng CustomerListItem tổng hợp (vd ô gợi ý) không phải khai.
  isDeceased?: boolean;
  // Công ty phụ trách + nhân viên phụ trách (đã lọc theo phạm vi quyền người xem).
  companies?: CustomerCompanyBrief[];
  // Phần mộ khách đang sở hữu (đã lọc theo phạm vi GRAVE_VIEW).
  ownedGraves?: OwnedGrave[];
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
  // Tạo khách ở tình trạng đã mất → backend đặt CustomerStatus = DECEASED. Mặc định false.
  isDeceased?: boolean;
  // Xác nhận "vẫn tạo dù trùng SĐT". Khi trùng SĐT backend chặn mềm (CUS_DUPLICATE_PHONE); đặt true
  // sau khi người dùng bấm xác nhận để tạo tiếp. SĐT có thể dùng chung nên không khoá cứng như CCCD.
  confirmDuplicatePhone?: boolean;
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
  // 'ALIVE' | 'DECEASED' — lọc tình trạng sống/mất.
  lifeStatus?: string;
  companyId?: number;
  assignedStaffId?: number;
  unassignedStaff?: boolean;
  tagIds?: number[];
  ownsGrave?: boolean;
  // true = chỉ khách CHƯA an táng (chưa là cốt ở mộ nào) — dùng khi chọn người thân đã mất.
  notBuried?: boolean;
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

export interface RelationshipKind {
  kindCode: string;
  label: string;
  inverseCode: string;
  isSymmetric: boolean;
  sortOrder: number;
}

export interface CustomerRelationship {
  id: number;
  fromCustomerId: number;
  otherCustomerId: number;
  otherCustomerCode: string;
  otherCustomerName: string;
  relationKind: string;
  relationLabel: string;
  isDerived: boolean;
  needsConfirmation: boolean;
  note: string | null;
  rowVersion: string;
  // ─── Đan chéo 360: tình trạng + dấu vết phần mộ của người thân ───
  isDeceased?: boolean;
  ownedGraves?: GraveRef[];        // mộ người thân đang sở hữu (đã lọc theo quyền mộ)
  buriedIn?: GraveRef | null;      // nơi người thân được an táng (suất còn hiệu lực)
}

export interface CreateCustomerRelationshipRequest {
  otherCustomerId: number;
  relationKind: string;
  note?: string | null;
}

export interface RelationshipListItem {
  id: number;
  fromCustomerId: number;
  fromCustomerCode: string;
  fromCustomerName: string;
  toCustomerId: number;
  toCustomerCode: string;
  toCustomerName: string;
  relationKind: string;
  relationLabel: string;
  isDerived: boolean;
  needsConfirmation: boolean;
  note: string | null;
}

export interface RelationshipSearchParams {
  search?: string;
  kind?: string;
  page?: number;
  pageSize?: number;
}

export interface RelationshipKindDetail {
  kindCode: string;
  labelMale: string;
  labelFemale: string;
  labelNeutral: string;
  inverseCode: string;
  inverseLabelNeutral: string | null;
  isSymmetric: boolean;
  sortOrder: number;
  isCore: boolean;
  deletable: boolean;
}

export interface RelationshipKindSideInput {
  labelMale: string;
  labelFemale: string;
  labelNeutral: string;
}

export interface CreateRelationshipKindRequest {
  isSymmetric: boolean;
  sideA: RelationshipKindSideInput;
  sideB?: RelationshipKindSideInput | null;
  sortOrder: number;
}

export interface UpdateRelationshipKindRequest {
  labelMale: string;
  labelFemale: string;
  labelNeutral: string;
  sortOrder: number;
}
