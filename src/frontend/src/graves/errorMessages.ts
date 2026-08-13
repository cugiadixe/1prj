const ERROR_MESSAGES: Record<string, string> = {
  GRAVE_DUPLICATE_CODE: 'Mã mộ này đã tồn tại.',
  GRAVE_NOT_FOUND: 'Không tìm thấy phần mộ.',
  GRAVE_OWNER_NOT_FOUND: 'Không tìm thấy khách hàng (chủ mộ).',
  GRAVE_OCCUPANT_NOT_FOUND: 'Không tìm thấy người an táng.',
  GRAVE_INVALID_ROW_VERSION:
    'Phần mộ đã bị thay đổi bởi người khác. Vui lòng tải lại và thử lại.',
  GRAVE_INVALID_ZONE: 'Khu không hợp lệ (chỉ A–L).',
  GRAVE_INVALID_TYPE: 'Loại mộ không hợp lệ.',
  GRAVE_INVALID_STATUS: 'Trạng thái không hợp lệ.',
};

export const GENERIC_ERROR = 'Đã có lỗi xảy ra. Vui lòng thử lại.';
export const PERMISSION_DENIED = 'Bạn không có quyền thực hiện thao tác này.';

export function getErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: { status?: number; data?: { extensions?: Record<string, unknown> } };
    };
    const status = err?.response?.status;
    if (status === 403) return PERMISSION_DENIED;
    if (status === 404) return ERROR_MESSAGES.GRAVE_NOT_FOUND;

    const extensions = err?.response?.data?.extensions;
    if (extensions && typeof extensions['errorCode'] === 'string') {
      const code = extensions['errorCode'] as string;
      return ERROR_MESSAGES[code] ?? GENERIC_ERROR;
    }
  } catch {
    // ignore
  }
  return GENERIC_ERROR;
}

export function isPermissionDenied(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 403;
  } catch {
    return false;
  }
}
