import { describe, it, expect } from 'vitest';
import {
  getMergeErrorMessage,
  isMergePermissionDenied,
  MERGE_GENERIC_ERROR,
  MERGE_PERMISSION_DENIED,
  MERGE_NOT_FOUND,
  MERGE_CONCURRENCY_ERROR,
} from './customerMergeErrorMessages';

describe('customerMergeErrorMessages', () => {
  it('returns permission denied for 403', () => {
    const error = { response: { status: 403 } };
    expect(getMergeErrorMessage(error)).toBe(MERGE_PERMISSION_DENIED);
  });

  it('returns not found for 404', () => {
    const error = { response: { status: 404 } };
    expect(getMergeErrorMessage(error)).toBe(MERGE_NOT_FOUND);
  });

  it('returns concurrency error for 409', () => {
    const error = { response: { status: 409 } };
    expect(getMergeErrorMessage(error)).toBe(MERGE_CONCURRENCY_ERROR);
  });

  it('maps known validation error detail', () => {
    const error = {
      response: {
        status: 400,
        data: {
          title: 'Validation Error',
          detail: 'Source and target customer cannot be the same.',
        },
      },
    };
    expect(getMergeErrorMessage(error)).toBe(
      'Source and target customer cannot be the same.',
    );
  });

  it('maps already merged error', () => {
    const error = {
      response: {
        status: 400,
        data: {
          title: 'Validation Error',
          detail: 'Cannot merge a customer that is already merged.',
        },
      },
    };
    expect(getMergeErrorMessage(error)).toBe(
      'This customer has already been merged and cannot be merged again.',
    );
  });

  it('maps overlapping company context error', () => {
    const error = {
      response: {
        status: 400,
        data: {
          title: 'Validation Error',
          detail:
            'Cannot automatically merge overlapping company contexts. Manual resolution required.',
        },
      },
    };
    expect(getMergeErrorMessage(error)).toContain('overlapping company relationships');
  });

  it('maps concurrency conflict detail', () => {
    const error = {
      response: {
        status: 400,
        data: {
          title: 'Validation Error',
          detail:
            'Concurrency conflict: Source customer has been modified since the request was created.',
        },
      },
    };
    expect(getMergeErrorMessage(error)).toBe(MERGE_CONCURRENCY_ERROR);
  });

  it('returns generic error for unknown detail', () => {
    const error = {
      response: {
        status: 400,
        data: { title: 'Error', detail: 'Something unknown happened.' },
      },
    };
    expect(getMergeErrorMessage(error)).toBe(MERGE_GENERIC_ERROR);
  });

  it('returns generic error for null/undefined', () => {
    expect(getMergeErrorMessage(null)).toBe(MERGE_GENERIC_ERROR);
    expect(getMergeErrorMessage(undefined)).toBe(MERGE_GENERIC_ERROR);
  });

  it('does not expose raw internal details', () => {
    const error = {
      response: {
        status: 500,
        data: {
          title: 'Internal Server Error',
          detail: 'SQL deadlock detected on table dbo.Customers',
        },
      },
    };
    const message = getMergeErrorMessage(error);
    expect(message).not.toContain('SQL');
    expect(message).not.toContain('deadlock');
    expect(message).not.toContain('dbo');
    expect(message).toBe(MERGE_GENERIC_ERROR);
  });

  describe('isMergePermissionDenied', () => {
    it('returns true for 403', () => {
      expect(isMergePermissionDenied({ response: { status: 403 } })).toBe(
        true,
      );
    });

    it('returns false for other statuses', () => {
      expect(isMergePermissionDenied({ response: { status: 400 } })).toBe(
        false,
      );
    });

    it('returns false for null', () => {
      expect(isMergePermissionDenied(null)).toBe(false);
    });
  });
});
