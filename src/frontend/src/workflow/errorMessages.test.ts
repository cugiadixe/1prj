import { describe, it, expect } from 'vitest';
import {
  getErrorMessage,
  getErrorCode,
  isPermissionDenied,
  isConcurrencyError,
  GENERIC_ERROR,
  PERMISSION_DENIED,
  NOT_FOUND,
} from './errorMessages';

describe('workflow errorMessages', () => {
  it('returns PERMISSION_DENIED for 403', () => {
    expect(getErrorMessage({ response: { status: 403 } })).toBe(PERMISSION_DENIED);
  });

  it('returns NOT_FOUND for 404', () => {
    expect(getErrorMessage({ response: { status: 404 } })).toBe(NOT_FOUND);
  });

  it('maps known error code', () => {
    const err = {
      response: { status: 400, data: { extensions: { errorCode: 'WF_DUPLICATE_DEFINITION_CODE' } } },
    };
    expect(getErrorMessage(err)).toBe('This definition code is already in use.');
  });

  it('returns GENERIC_ERROR for unknown code', () => {
    const err = {
      response: { status: 400, data: { extensions: { errorCode: 'UNKNOWN_CODE' } } },
    };
    expect(getErrorMessage(err)).toBe(GENERIC_ERROR);
  });

  it('returns GENERIC_ERROR for non-error input', () => {
    expect(getErrorMessage(null)).toBe(GENERIC_ERROR);
    expect(getErrorMessage('string')).toBe(GENERIC_ERROR);
  });

  it('getErrorCode extracts error code', () => {
    expect(
      getErrorCode({ response: { data: { extensions: { errorCode: 'WF_TEST' } } } }),
    ).toBe('WF_TEST');
  });

  it('getErrorCode returns null for missing code', () => {
    expect(getErrorCode({})).toBeNull();
  });

  it('isPermissionDenied detects 403', () => {
    expect(isPermissionDenied({ response: { status: 403 } })).toBe(true);
    expect(isPermissionDenied({ response: { status: 200 } })).toBe(false);
    expect(isPermissionDenied(null)).toBe(false);
  });

  it('isConcurrencyError detects WF_INVALID_ROW_VERSION', () => {
    expect(
      isConcurrencyError({
        response: { data: { extensions: { errorCode: 'WF_INVALID_ROW_VERSION' } } },
      }),
    ).toBe(true);
    expect(isConcurrencyError({})).toBe(false);
  });
});
