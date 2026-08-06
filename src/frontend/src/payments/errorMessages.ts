export const GENERIC_ERROR = 'An error occurred. Please try again.';
export const PERMISSION_DENIED =
  'You do not have permission to perform this action.';
export const NOT_FOUND = 'Record not found.';
export const CONCURRENCY_ERROR =
  'Data has changed since you started. Please refresh and try again.';

export function getErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          detail?: string;
          title?: string;
          extensions?: Record<string, unknown>;
        };
      };
    };

    const status = err?.response?.status;

    if (status === 403) return PERMISSION_DENIED;
    if (status === 404) return NOT_FOUND;
    if (status === 409) return CONCURRENCY_ERROR;

    // Backend typically returns ProblemDetails for 400 Bad Request.
    // PaymentTransactionController uses: new { Title = "Validation Error", Detail = ex.Message }
    if (status === 400 && err?.response?.data?.detail) {
      return err.response.data.detail;
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
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 409;
  } catch {
    return false;
  }
}
