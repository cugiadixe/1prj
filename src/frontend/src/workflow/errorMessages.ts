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
