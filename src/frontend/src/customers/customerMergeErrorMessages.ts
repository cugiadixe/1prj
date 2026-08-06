export const MERGE_ERROR_MESSAGES: Record<string, string> = {
  'Source and target customer cannot be the same.':
    'Source and target customer cannot be the same.',
  'Cannot merge a customer that is already merged.':
    'This customer has already been merged and cannot be merged again.',
  'Target customer must be active.':
    'The target (survivor) customer must be active.',
  'Cannot automatically merge overlapping company contexts. Manual resolution required.':
    'These customers share overlapping company relationships. Manual resolution is required before merging.',
  'One or both customers not found.': 'One or both customers were not found.',
  'Failed to load saved request': 'Failed to save the merge request. Please try again.',
};

export const MERGE_GENERIC_ERROR =
  'An unexpected error occurred. Please try again.';
export const MERGE_PERMISSION_DENIED =
  'You do not have permission to perform this action.';
export const MERGE_NOT_FOUND = 'Merge request not found.';
export const MERGE_CONCURRENCY_ERROR =
  'Data has changed since you started. Please refresh and try again.';

export function getMergeErrorMessage(error: unknown): string {
  try {
    const err = error as {
      response?: {
        status?: number;
        data?: {
          title?: string;
          detail?: string;
        };
      };
    };

    const status = err?.response?.status;

    if (status === 403) return MERGE_PERMISSION_DENIED;
    if (status === 404) return MERGE_NOT_FOUND;
    if (status === 409) return MERGE_CONCURRENCY_ERROR;

    const detail = err?.response?.data?.detail;
    if (detail && typeof detail === 'string') {
      const mapped = MERGE_ERROR_MESSAGES[detail];
      if (mapped) return mapped;
      if (detail.startsWith('Concurrency conflict')) return MERGE_CONCURRENCY_ERROR;
    }
  } catch {
    // ignore
  }
  return MERGE_GENERIC_ERROR;
}

export function isMergePermissionDenied(error: unknown): boolean {
  try {
    const err = error as { response?: { status?: number } };
    return err?.response?.status === 403;
  } catch {
    return false;
  }
}
