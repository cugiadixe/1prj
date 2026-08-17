import type { DashboardCountItem } from './dashboardApi';
import type { Datum } from './Charts';

const GRAVE_STATUS: Record<string, string> = {
  EMPTY: 'Còn trống',
  RESERVED: 'Đã đặt',
  OCCUPIED: 'Đã an táng',
  RELOCATED: 'Đã cải táng',
};

const GRAVE_TYPE: Record<string, string> = {
  SINGLE: 'Mộ đơn',
  DOUBLE: 'Mộ đôi',
  FAMILY: 'Mộ gia tộc',
};

const CARE_PACKAGE_STATUS: Record<string, string> = {
  DRAFT: 'Nháp',
  PENDING_APPROVAL: 'Chờ duyệt',
  APPROVED: 'Đã duyệt',
  REJECTED: 'Từ chối',
  PAYMENT_ELIGIBLE: 'Đủ ĐK thanh toán',
  PENDING_PAYMENT: 'Chờ thanh toán',
  PAID: 'Đã thanh toán',
  ACTIVE: 'Đang hiệu lực',
};

const SERVICE_STATUS: Record<string, string> = {
  ACTIVE: 'Đang hiệu lực',
  EXPIRED: 'Hết hạn',
  CANCELLED: 'Đã huỷ',
  PENDING_PRICE_OVERRIDE: 'Chờ duyệt giá',
};

const CUSTOMER_STATUS: Record<string, string> = {
  ACTIVE: 'Đang hoạt động',
  INACTIVE: 'Ngừng',
  DECEASED: 'Đã mất',
  PROSPECT: 'Tiềm năng',
  MERGED: 'Đã gộp',
};

const toData = (items: DashboardCountItem[], map: Record<string, string>): Datum[] =>
  items.map((i) => ({ label: map[i.key] ?? i.key, value: i.count }));

export const graveStatusData = (items: DashboardCountItem[]) => toData(items, GRAVE_STATUS);
export const graveTypeData = (items: DashboardCountItem[]) => toData(items, GRAVE_TYPE);
export const carePackageStatusData = (items: DashboardCountItem[]) => toData(items, CARE_PACKAGE_STATUS);
export const serviceStatusData = (items: DashboardCountItem[]) => toData(items, SERVICE_STATUS);
export const customerStatusData = (items: DashboardCountItem[]) => toData(items, CUSTOMER_STATUS);

// Khu: giữ nguyên nhãn, sắp theo tên tăng dần.
export const zoneData = (items: DashboardCountItem[]): Datum[] =>
  items
    .map((i) => ({ label: i.key, value: i.count }))
    .sort((a, b) => a.label.localeCompare(b.label));

// "2026-03" -> "3/26"
export const monthLabel = (m: string): string => {
  const [y, mo] = m.split('-');
  return `${Number(mo)}/${y.slice(2)}`;
};
