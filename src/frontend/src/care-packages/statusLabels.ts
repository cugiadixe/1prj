// Nhãn trạng thái yêu cầu gói chăm sóc — dịch sang tiếng Việt để hiển thị.
// Giá trị (key) vẫn giữ nguyên tiếng Anh vì backend/API dùng các mã này.
export const carePackageStatusLabels: Record<string, string> = {
  Draft: 'Nháp',
  PendingApproval: 'Chờ duyệt',
  PaymentEligible: 'Đủ điều kiện thanh toán',
  PendingPayment: 'Chờ thanh toán',
  Paid: 'Đã thanh toán',
  Active: 'Đang hiệu lực',
  Rejected: 'Đã từ chối',
};

export const carePackageStatusColors: Record<string, string> = {
  Draft: 'default',
  PendingApproval: 'orange',
  PaymentEligible: 'blue',
  PendingPayment: 'purple',
  Paid: 'cyan',
  Active: 'green',
  Rejected: 'red',
};

export const carePackageStatusLabel = (status: string): string =>
  carePackageStatusLabels[status] ?? status;

// Danh sách trạng thái theo đúng thứ tự vòng đời, dùng cho bộ lọc.
export const carePackageStatusOrder: string[] = [
  'Draft',
  'PendingApproval',
  'PaymentEligible',
  'PendingPayment',
  'Paid',
  'Active',
  'Rejected',
];
