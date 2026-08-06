/**
 * Sanitized error messages for Permission Assignment UI — Phase 1B.1-N.
 * Maps backend ProblemDetails error codes to user-friendly messages.
 * No raw backend exception details, SQL text, or stack traces are shown.
 */

const ERROR_MESSAGES: Record<string, string> = {
  PERMISSION_NOT_FOUND: 'The selected permission was not found.',
  USER_NOT_FOUND: 'User not found.',
  AUTH_ACCOUNT_NOT_FOUND: 'Account not found.',
  ASSIGNMENT_CONFLICT: 'A conflicting assignment already exists for this permission.',
  ASSIGNMENT_NOT_FOUND: 'The assignment was not found or has already been deactivated.',
  CONCURRENCY_CONFLICT:
    'The assignment was modified by another user. Please refresh and try again.',
  REASON_REQUIRED: 'A reason is required for this action.',
  REASON_TOO_LONG: 'Reason must not exceed 500 characters.',
  INVALID_SCOPE_TYPE: 'Invalid scope type.',
  INVALID_GRANT_TYPE: 'Invalid grant type.',
  COMPANY_REQUIRED: 'A company must be selected for company-scoped assignments.',
  COMPANY_ASSIGNMENT_MISSING:
    'The target user does not have an active assignment to the selected company.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED_MSG = 'You do not have permission to manage permission assignments.';

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
    if (status === 404) return 'The requested resource was not found.';
    if (status === 409) return 'A conflict was detected. Please refresh and try again.';

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
