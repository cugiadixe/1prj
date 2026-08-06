const ERROR_MESSAGES: Record<string, string> = {
  SVC_TYPE_DUPLICATE_CODE: 'A service type with this code already exists.',
  SVC_TYPE_NOT_FOUND: 'Service type not found.',
  SVC_NOT_FOUND: 'Service not found.',
  SVC_INVALID_STATUS: 'Service is not in a valid status for this operation.',
  SVC_CUSTOMER_NOT_FOUND: 'Customer not found.',
  SVC_COMPANY_NOT_FOUND: 'Company not found.',
  SVC_CONTEXT_NOT_FOUND: 'Customer does not have a relationship with this company.',
  SVC_CONCURRENCY: 'This record was modified by another user. Please refresh and try again.',
};

export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED = 'You do not have permission to perform this action.';
export const NOT_FOUND = 'Record not found.';

export function getErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          title?: string;
          detail?: string;
          extensions?: Record<string, unknown>;
        };
      };
    };

    const status = err?.response?.status;
    if (status === 403) return PERMISSION_DENIED;
    if (status === 404) return NOT_FOUND;
    if (status === 409) return ERROR_MESSAGES['SVC_CONCURRENCY'];

    const data = err?.response?.data;
    if (!data) return GENERIC_ERROR;

    // First try extensions.errorCode
    if (data.extensions && typeof data.extensions['errorCode'] === 'string') {
      const code = data.extensions['errorCode'] as string;
      if (ERROR_MESSAGES[code]) {
        return ERROR_MESSAGES[code];
      }
    }

    // Fallback to title/detail parsing for expected codes if extensions is missing
    // e.g., Backend returns BadRequest(new { Title = "...", Detail = "..." }) without extension
    const title = data.title;
    if (title && ERROR_MESSAGES[title]) {
      return ERROR_MESSAGES[title];
    }
    
    // In case backend puts the business error string in Detail or Title
    // We sanitize and do NOT return raw internal exceptions if it's not mapped
    // But if Title matches a known mapped error, we can use it.
    if (status === 400 && data.detail) {
      // Just returning the detail safely if it's a validation error, but to be safe and sanitized,
      // we could return generic error if it's not a known format. Let's return the detail if it's there
      // because ASP.NET validation (FluentValidation) puts user-friendly messages in detail sometimes,
      // or we just return generic if we want strict sanitization.
      // The requirement says: sanitized handling, no raw SQL, no stack trace.
      // A standard 400 Bad Request detail from our app should be safe.
      // Let's check if we should map standard strings.
      if (data.title === 'Validation Failure') return 'Please check the form for errors.';
      if (data.detail && !data.detail.includes('Exception') && !data.detail.includes('Sql')) {
         return data.detail;
      }
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

export function isConcurrencyError(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number, data?: { extensions?: Record<string, unknown>, title?: string } } };
    if (err?.response?.status === 409) return true;
    
    const ext = err?.response?.data?.extensions;
    if (ext && ext['errorCode'] === 'SVC_CONCURRENCY') return true;
    if (err?.response?.data?.title === 'SVC_CONCURRENCY') return true;
  } catch {
    return false;
  }
  return false;
}
