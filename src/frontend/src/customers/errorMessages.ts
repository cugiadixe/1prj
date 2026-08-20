const ERROR_MESSAGES: Record<string, string> = {
  CUS_INVALID_ROW_VERSION:
    'Khách hàng này vừa được người khác chỉnh sửa. Vui lòng tải lại và thử lại.',
  CUS_DUPLICATE_CCCD: 'Đã có khách hàng đang hoạt động dùng số CCCD này.',
  CUS_DUPLICATE_CUSTOMER_CODE: 'Mã khách hàng này đã được sử dụng.',
  CUS_DUPLICATE_PHONE: 'Đã có khách hàng đang hoạt động dùng số điện thoại này.',
  CUS_CUSTOMER_NOT_FOUND: 'Không tìm thấy khách hàng.',
  CUS_COMPANY_NOT_FOUND: 'Không tìm thấy công ty hoặc công ty đã ngừng hoạt động.',
  CUS_DUPLICATE_COMPANY_CONTEXT:
    'Khách hàng đã có quan hệ với công ty này.',
  CUS_CONTEXT_NOT_FOUND: 'Không tìm thấy quan hệ công ty.',
};

export const GENERIC_ERROR = 'Đã xảy ra lỗi. Vui lòng thử lại.';
export const PERMISSION_DENIED =
  'Bạn không có quyền thực hiện thao tác này.';
export const CUSTOMER_NOT_FOUND = 'Không tìm thấy khách hàng.';

export function getErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          extensions?: Record<string, unknown>;
        };
      };
    };

    const status = err?.response?.status;

    if (status === 403) return PERMISSION_DENIED;
    if (status === 404) return CUSTOMER_NOT_FOUND;

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

export function getErrorCode(error: unknown): string | null {
  try {
    const err = error as {
      response?: {
        data?: {
          extensions?: Record<string, unknown>;
        };
      };
    };
    const ext = err?.response?.data?.extensions;
    if (ext && typeof ext['errorCode'] === 'string') {
      return ext['errorCode'] as string;
    }
  } catch {
    // ignore
  }
  return null;
}

export function isPermissionDenied(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 403;
  } catch {
    return false;
  }
}

export function isConcurrencyError(error: unknown): boolean {
  return getErrorCode(error) === 'CUS_INVALID_ROW_VERSION';
}
