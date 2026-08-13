/**
 * Sanitized error messages for Permission Assignment UI — Phase 1B.1-N.
 * Maps backend ProblemDetails error codes to user-friendly messages.
 * No raw backend exception details, SQL text, or stack traces are shown.
 */

const ERROR_MESSAGES: Record<string, string> = {
  PERMISSION_NOT_FOUND: 'Không tìm thấy quyền đã chọn.',
  USER_NOT_FOUND: 'Không tìm thấy người dùng.',
  AUTH_ACCOUNT_NOT_FOUND: 'Không tìm thấy tài khoản.',
  ASSIGNMENT_CONFLICT: 'Đã tồn tại một phân quyền xung đột cho quyền này.',
  ASSIGNMENT_NOT_FOUND: 'Phân quyền không tồn tại hoặc đã được thu hồi.',
  CONCURRENCY_CONFLICT:
    'Phân quyền vừa bị người khác thay đổi. Vui lòng tải lại và thử lại.',
  REASON_REQUIRED: 'Cần nhập lý do cho thao tác này.',
  REASON_TOO_LONG: 'Lý do không được vượt quá 500 ký tự.',
  INVALID_SCOPE_TYPE: 'Phạm vi không hợp lệ.',
  INVALID_GRANT_TYPE: 'Kiểu cấp quyền không hợp lệ.',
  COMPANY_REQUIRED: 'Cần chọn một công ty cho phân quyền theo phạm vi công ty.',
  COMPANY_ASSIGNMENT_MISSING:
    'Người dùng đích chưa được gán vào công ty đã chọn.',
};

export const GENERIC_ERROR = 'Đã xảy ra lỗi. Vui lòng thử lại.';
export const PERMISSION_DENIED_MSG = 'Bạn không có quyền quản lý phân quyền.';

/**
 * Extracts a user-friendly message from an axios error.
 * Never returns raw exception details, SQL, or stack traces.
 */
export function getAssignmentErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          extensions?: Record<string, unknown>;
          detail?: string;
        };
      };
    };

    const status = err?.response?.status;

    if (status === 403) return PERMISSION_DENIED_MSG;
    if (status === 404) return 'Không tìm thấy tài nguyên yêu cầu.';
    if (status === 409) return 'Phát hiện xung đột. Vui lòng tải lại và thử lại.';

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
