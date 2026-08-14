const ERROR_MESSAGES: Record<string, string> = {
  WF_INVALID_ROW_VERSION:
    'This record was modified by another user. Please refresh and try again.',
  WF_DEFINITION_NOT_FOUND: 'Workflow definition not found.',
  WF_VERSION_NOT_FOUND: 'Workflow version not found.',
  WF_STEP_NOT_FOUND: 'Workflow step not found.',
  WF_BINDING_NOT_FOUND: 'Workflow binding not found.',
  WF_DUPLICATE_DEFINITION_CODE: 'This definition code is already in use.',
  WF_VERSION_NOT_DRAFT: 'Only DRAFT versions can be modified.',
  WF_VERSION_NOT_PUBLISHED: 'Only PUBLISHED versions can be activated.',
  WF_VERSION_NOT_ACTIVE: 'Only ACTIVE versions can be retired.',
  WF_NO_STEPS: 'Version must have at least one step before publishing.',
  WF_INSTANCE_NOT_FOUND: 'Workflow instance not found.',
  WF_INSTANCE_NOT_PENDING: 'This instance is no longer pending approval.',
  WF_INSTANCE_NOT_RETURNED: 'Only returned instances can be resubmitted.',
  WF_STEP_NOT_PENDING: 'This step is no longer pending.',
  WF_NOT_ASSIGNEE: 'You are not an assignee for this step.',
  WF_REQUESTER_IS_APPROVER: 'Requester cannot approve their own request.',
  WF_NOT_REQUESTER: 'Only the original requester can perform this action.',
  WF_CANNOT_WITHDRAW: 'This instance cannot be withdrawn in its current state.',
  WF_NO_VALID_BINDING:
    'Quy trình này chưa được khai báo liên kết đang hiệu lực. Vui lòng liên hệ quản trị.',
  WF_NO_ASSIGNEE_FOR_STEP:
    'Chưa xác định được người duyệt cho một bước. Vui lòng kiểm tra cấu hình thẩm quyền phê duyệt.',
  WF_REASON_REQUIRED: 'Reason is required for this action.',
  WF_USER_NOT_FOUND: 'Target user not found or inactive.',
  WF_ALREADY_REJECTED: 'This step has already been rejected.',

  // Các mã bổ sung khi siết an toàn quy trình (Nhóm 0).
  WF_NO_EXECUTION_HANDLER:
    'Quy trình này chưa có bộ xử lý thực thi nên chưa dùng được. Vui lòng báo bộ phận CNTT.',
  WF_BINDING_AMBIGUOUS:
    'Quy trình có nhiều liên kết cùng phạm vi và cùng mức ưu tiên. Đây là lỗi cấu hình — vui lòng đóng bớt liên kết cũ hoặc đặt mức ưu tiên khác nhau.',
  WF_ONLY_REQUESTER_IS_APPROVER:
    'Người đề xuất cũng là người duyệt duy nhất của bước này.',
  WF_INSTANCE_NOT_RETRYABLE:
    'Chỉ chạy lại được hồ sơ đang Thất bại hoặc đang kẹt chờ thực thi.',
  WF_REJECT_COMPENSATION_FAILED:
    'Đã ghi nhận từ chối, nhưng chưa cập nhật được trạng thái hồ sơ nghiệp vụ. Vui lòng báo bộ phận CNTT để xử lý lại.',

  // Gán gói dịch vụ cho khách.
  CCP_COMPANY_CONTEXT_REQUIRED:
    'Chưa xác định công ty làm việc nên không xác định được quy trình phê duyệt. Vui lòng chọn công ty rồi thử lại.',
  CCP_APPROVAL_NOT_CONFIGURED:
    'Quy trình phê duyệt gán gói dịch vụ chưa được cấu hình cho công ty này. Vui lòng liên hệ quản trị.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED =
  'You do not have permission to perform this action.';
export const NOT_FOUND = 'The requested resource was not found.';

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
    if (status === 404) return NOT_FOUND;

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
  return getErrorCode(error) === 'WF_INVALID_ROW_VERSION';
}
