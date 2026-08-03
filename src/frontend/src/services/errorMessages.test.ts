import { describe, it, expect } from 'vitest';
import { getErrorMessage, isPermissionDenied, isConcurrencyError, GENERIC_ERROR, PERMISSION_DENIED, NOT_FOUND } from './errorMessages';

describe('errorMessages', () => {
  it('returns generic error for null or undefined', () => {
    expect(getErrorMessage(null)).toBe(GENERIC_ERROR);
    expect(getErrorMessage(undefined)).toBe(GENERIC_ERROR);
  });

  it('returns permission denied for 403', () => {
    const error = { response: { status: 403 } };
    expect(getErrorMessage(error)).toBe(PERMISSION_DENIED);
    expect(isPermissionDenied(error)).toBe(true);
  });

  it('returns not found for 404', () => {
    const error = { response: { status: 404 } };
    expect(getErrorMessage(error)).toBe(NOT_FOUND);
  });

  it('maps extensions.errorCode correctly', () => {
    const error = {
      response: {
        data: {
          extensions: { errorCode: 'SVC_TYPE_DUPLICATE_CODE' }
        }
      }
    };
    expect(getErrorMessage(error)).toBe('A service type with this code already exists.');
  });

  it('maps title correctly if no extension', () => {
    const error = {
      response: {
        data: {
          title: 'SVC_TYPE_NOT_FOUND'
        }
      }
    };
    expect(getErrorMessage(error)).toBe('Service type not found.');
  });

  it('returns detail if 400 and detail exists without raw exception', () => {
    const error = {
      response: {
        status: 400,
        data: {
          detail: 'Invalid validTo date.'
        }
      }
    };
    expect(getErrorMessage(error)).toBe('Invalid validTo date.');
  });

  it('sanitizes 400 error if detail contains Exception or Sql', () => {
    const error = {
      response: {
        status: 400,
        data: {
          detail: 'System.Exception: Something failed.'
        }
      }
    };
    expect(getErrorMessage(error)).toBe(GENERIC_ERROR);
  });

  it('detects concurrency error', () => {
    const error409 = { response: { status: 409 } };
    expect(isConcurrencyError(error409)).toBe(true);
    expect(getErrorMessage(error409)).toBe('This record was modified by another user. Please refresh and try again.');

    const errorExt = { response: { data: { extensions: { errorCode: 'SVC_CONCURRENCY' } } } };
    expect(isConcurrencyError(errorExt)).toBe(true);
    expect(getErrorMessage(errorExt)).toBe('This record was modified by another user. Please refresh and try again.');
  });
});
