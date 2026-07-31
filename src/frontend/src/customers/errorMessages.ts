const ERROR_MESSAGES: Record<string, string> = {
  CUS_INVALID_ROW_VERSION:
    'This customer was modified by another user. Please refresh and try again.',
  CUS_DUPLICATE_CCCD: 'An active customer with this CCCD already exists.',
  CUS_DUPLICATE_CUSTOMER_CODE: 'This customer code is already in use.',
  CUS_CUSTOMER_NOT_FOUND: 'Customer not found.',
  CUS_COMPANY_NOT_FOUND: 'Company not found or inactive.',
  CUS_DUPLICATE_COMPANY_CONTEXT:
    'Customer already has a relationship with this company.',
  CUS_CONTEXT_NOT_FOUND: 'Company context not found.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED =
  'You do not have permission to perform this action.';
export const CUSTOMER_NOT_FOUND = 'Customer not found.';

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
