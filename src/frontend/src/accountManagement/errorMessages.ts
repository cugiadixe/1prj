/**
 * Maps backend ProblemDetails error codes to user-friendly messages.
 * No raw backend exception details, SQL text, or stack traces are shown.
 */

const ERROR_MESSAGES: Record<string, string> = {
  AUTH_ACCOUNT_NOT_FOUND: 'Account not found.',
  AUTH_ACCOUNT_STATE_CONFLICT:
    'This action cannot be performed on the account in its current state.',
  AUTH_EXTERNAL_PASSWORD_MANAGED:
    'Password for this account is managed externally.',
  AUTH_PASSWORD_REUSE:
    'The generated password matches a recently used password. Please try again.',
  AUTH_PASSWORD_LENGTH_INVALID:
    'Password does not meet length requirements. Please try again.',
  AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT:
    'Password contains a disallowed pattern. Please try again.',
  AUTH_ACCOUNT_CONCURRENCY_CONFLICT:
    'The account was modified by another user. Please refresh and try again.',
  REASON_REQUIRED: 'A reason is required for this action.',
  REASON_TOO_LONG: 'Reason must not exceed 500 characters.',
  REASON_CONTAINS_SENSITIVE_TERM: 'Reason must not contain sensitive terms.',
  PAGE_INVALID: 'Invalid page number.',
  PAGE_SIZE_INVALID: 'Invalid page size.',
  USER_NOT_FOUND: 'User not found.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED = 'You do not have permission to manage accounts.';
export const ACCOUNT_NOT_FOUND = 'Account not found.';

/**
 * Extracts a user-friendly message from an axios error.
 * Never returns raw exception details, SQL, or stack traces.
 */
export function getErrorMessage(error: unknown): string {
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

    if (status === 403) return PERMISSION_DENIED;
    if (status === 404) return ACCOUNT_NOT_FOUND;

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

export function isNotFound(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 404;
  } catch {
    return false;
  }
}
