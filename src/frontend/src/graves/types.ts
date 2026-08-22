export const GRAVE_ZONES = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L'] as const;

export const GRAVE_TYPES: Record<string, string> = {
  SINGLE: 'Mộ đơn',
  DOUBLE: 'Mộ đôi',
  FAMILY: 'Mộ gia tộc',
  CREMATION: 'Lưu tro / hỏa táng',
  OTHER: 'Khác',
};

// Loại mộ được XÁC ĐỊNH theo số cốt: 1 = Mộ đơn, 2 = Mộ đôi, ≥3 = Mộ gia tộc.
// Đây là nguồn chân lý phía client; backend cũng suy lại như vậy khi lưu.
export function graveTypeForCotCount(cotCount: number): string {
  const n = Number.isFinite(cotCount) ? cotCount : 1;
  if (n <= 1) return 'SINGLE';
  if (n === 2) return 'DOUBLE';
  return 'FAMILY';
}

// Loại mộ dùng để LỌC danh sách (chỉ 3 loại theo số cốt).
export const GRAVE_TYPE_FILTER: Record<string, string> = {
  SINGLE: 'Mộ đơn',
  DOUBLE: 'Mộ đôi',
  FAMILY: 'Mộ gia tộc',
};

export const GRAVE_STATUSES: Record<string, string> = {
  EMPTY: 'Còn trống',
  RESERVED: 'Đã đặt',
  OCCUPIED: 'Đã an táng',
  RELOCATED: 'Đã cải táng',
};

export const GRAVE_STATUS_COLORS: Record<string, string> = {
  EMPTY: 'default',
  RESERVED: 'blue',
  OCCUPIED: 'green',
  RELOCATED: 'orange',
};

export const GENDERS: Record<string, string> = {
  MALE: 'Nam',
  FEMALE: 'Nữ',
  OTHER: 'Khác',
};

export interface GraveListItem {
  id: number;
  graveCode: string;
  zone: string;
  plotNumber: string;
  graveType: string;
  areaM2: number | null;
  cotCount: number;
  status: string;
  ownerCustomerId: number | null;
  ownerName: string | null;
  occupantCount: number;
  companyId: number | null;
  companyName: string | null;
  createdAt: string;
  tags?: import('../tags/types').Tag[];
}

export interface GraveCompanyLookup {
  id: number;
  name: string;
}

// Lọc theo tương quan số người an táng (cốt ACTIVE) với số cốt.
export const GRAVE_CAPACITY_FILTER: Record<string, string> = {
  UNDER: 'Còn chỗ (ít hơn số cốt)',
  FULL: 'Đã đủ (bằng số cốt)',
  OVER: 'Vượt số cốt',
};

export interface GraveOccupant {
  id: number;
  graveId: number;
  deceasedCustomerId: number | null;
  status: string;                 // ACTIVE / RELOCATED
  relocatedAt: string | null;
  relocationNote: string | null;
  fullName: string;
  gender: string | null;
  dob: string | null;
  deathDateSolar: string | null;
  deathDateLunar: string | null;
  burialDate: string | null;
  hometown: string | null;
  ownerRelationship: string | null;
  deceasedRelationship: string | null;
  notes: string | null;
  rowVersion: string;
}

export interface OccupantCandidate {
  customerId: number;
  customerCode: string;
  fullName: string;
  relationLabel: string;
}

export interface PlaceGraveOccupantRequest {
  deceasedCustomerId: number;
  burialDate?: string | null;
  notes?: string | null;
}

export interface RelocateOccupantRequest {
  relocatedAt?: string | null;
  note?: string | null;
}

export interface AssignableGrave {
  graveId: number;
  graveCode: string;
  zone: string;
  rowVersion: string;
}

export interface OwnerDeathRequest {
  deceasedCustomerId: number;
  deathDateSolar?: string | null;
  heirCustomerId: number;
  reason?: string | null;
}

export interface OwnerDeathResult {
  deceasedCustomerId: number;
  heirCustomerId: number;
  gravesOwned: number;
  gravesTransferred: number;
  occupantsRederived: number;
}

export interface GraveEmergencyContact {
  id: number;
  graveId: number;
  priority: number;
  contactCustomerId: number | null;
  contactCode: string | null;
  contactName: string;
  contactPhone: string | null;
  relationshipNote: string | null;
  rowVersion: string;
}

export interface GraveDetail {
  id: number;
  graveCode: string;
  zone: string;
  plotNumber: string;
  rowLabel: string | null;
  colLabel: string | null;
  graveType: string;
  areaM2: number | null;
  cotCount: number;
  status: string;
  ownerCustomerId: number | null;
  ownerName: string | null;
  ownerCode: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  emergencyContactRelationship: string | null;
  notes: string | null;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
  occupants: GraveOccupant[];
  emergencyContacts: GraveEmergencyContact[];
  tags?: import('../tags/types').Tag[];
}

export interface CreateEmergencyContactRequest {
  contactCustomerId: number;
  relationshipNote?: string | null;
}

export interface UpdateEmergencyContactRequest {
  contactCustomerId: number;
  relationshipNote?: string | null;
  targetVersion: string;
}

export interface CreateGraveOccupantRequest {
  fullName: string;
  gender?: string | null;
  dob?: string | null;
  deathDateSolar?: string | null;
  deathDateLunar?: string | null;
  burialDate?: string | null;
  hometown?: string | null;
  ownerRelationship?: string | null;
  deceasedRelationship?: string | null;
  notes?: string | null;
}

export interface CreateGraveRequest {
  graveCode: string;
  zone: string;
  plotNumber: string;
  rowLabel?: string | null;
  colLabel?: string | null;
  graveType: string;
  areaM2?: number | null;
  cotCount: number;
  status: string;
  ownerCustomerId?: number | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  notes?: string | null;
  occupants: CreateGraveOccupantRequest[];
}

export interface UpdateGraveRequest {
  zone: string;
  plotNumber: string;
  rowLabel?: string | null;
  colLabel?: string | null;
  graveType: string;
  areaM2?: number | null;
  cotCount: number;
  status: string;
  ownerCustomerId?: number | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  notes?: string | null;
  targetVersion: string;
}

export interface UpdateGraveOccupantRequest extends CreateGraveOccupantRequest {
  targetVersion: string;
}

export interface GraveSearchParams {
  search?: string;
  zone?: string;
  status?: string;
  graveType?: string;
  ownerCustomerId?: number;
  companyId?: number;
  capacity?: string;
  tagIds?: number[];
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Chuyển quyền sở hữu là việc CHUNG (người còn sống), nhiều lý do — không chỉ do qua đời.
export const TRANSFER_TYPES: Record<string, string> = {
  SALE: 'Sang nhượng / Bán',
  GIFT: 'Cho / Tặng',
  RELOCATION: 'Chuyển công tác / nơi ở',
  INHERITANCE: 'Thừa kế',
  DEATH: 'Chủ mộ qua đời',
  CORRECTION: 'Đính chính',
};

export const TRANSFER_TYPE_COLORS: Record<string, string> = {
  SALE: 'gold',
  GIFT: 'cyan',
  RELOCATION: 'blue',
  INHERITANCE: 'purple',
  DEATH: 'default',
  CORRECTION: 'orange',
};

export interface TransferOwnershipRequest {
  newOwnerCustomerId: number;
  transferType: string;
  reason?: string | null;
  targetVersion: string;
}

export interface TransferOwnershipResult {
  grave: GraveDetail;
  ownershipHistoryId: number;
  occupantsRederived: number;
  occupantsNeedingConfirmation: number;
}

export interface GraveAttachment {
  id: number;
  graveId: number;
  category: string;
  ownershipHistoryId: number | null;
  fileNameOriginal: string;
  contentType: string;
  sizeBytes: number;
  hasThumbnail: boolean;
  isImage: boolean;
  description: string | null;
  createdAt: string;
  createdByUserId: number | null;
  uploadedByName: string | null;
}

export interface OwnershipHistoryItem {
  id: number;
  previousOwnerId: number | null;
  previousOwnerName: string | null;
  newOwnerId: number;
  newOwnerName: string | null;
  transferType: string;
  reason: string | null;
  transferredAt: string;
}
