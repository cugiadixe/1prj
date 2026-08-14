/** Màu & nhãn tiếng Việt cho trạng thái hồ sơ quy trình (WorkflowInstance.instanceStatus). */

export const INSTANCE_STATUS_COLORS: Record<string, string> = {
  PENDING_APPROVAL: 'blue',
  RETURNED: 'orange',
  WITHDRAWN: 'red',
  REJECTED: 'volcano',
  PENDING_EXECUTION: 'cyan',
  EXECUTING: 'geekblue',
  EXECUTED: 'green',
  FAILED: 'magenta',
  COMPLETED: 'green',
  CANCELLED: 'default',
};

export const INSTANCE_STATUS_LABELS: Record<string, string> = {
  PENDING_APPROVAL: 'Chờ duyệt',
  RETURNED: 'Trả lại',
  WITHDRAWN: 'Đã thu hồi',
  REJECTED: 'Từ chối',
  PENDING_EXECUTION: 'Chờ thực thi',
  EXECUTING: 'Đang thực thi',
  EXECUTED: 'Đã thực thi',
  FAILED: 'Thất bại',
  COMPLETED: 'Hoàn tất',
  CANCELLED: 'Đã hủy',
};

/** Nhãn tiếng Việt cho một trạng thái; không khớp thì trả nguyên mã. */
export function instanceStatusLabel(status: string): string {
  return INSTANCE_STATUS_LABELS[status] ?? status;
}
