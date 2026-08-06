/**
 * Maps backend ProblemDetails error codes to user-friendly messages for user role assignments.
 * No raw backend exception details, SQL text, or stack traces are shown.
 */

const ERROR_MESSAGES: Record<string, string> = {
  USER_ROLE_ASSIGNMENT_ALREADY_EXISTS: 'This user already has an active assignment for this role.',
  USER_NOT_FOUND: 'The target user could not be found.',
  ROLE_NOT_FOUND: 'The selected role could not be found.',
  INVALID_LIFECYCLE_DATES: 'Effective from date must be before effective to date.',
  COMPANY_CONTEXT_REQUIRED: 'A specific company must be selected to assign a COMPANY-scoped role.',
  USER_MISSING_COMPANY_CONTEXT: 'The user does not belong to the selected company context.',
  INACTIVE_ROLE: 'Cannot assign an inactive role.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED = 'You do not have permission to manage user role assignments.';
export const NOT_FOUND = 'Resource not found.';

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
    if (status === 404) return NOT_FOUND;
    if (status === 409) return ERROR_MESSAGES['USER_ROLE_ASSIGNMENT_ALREADY_EXISTS'] ?? GENERIC_ERROR;

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
