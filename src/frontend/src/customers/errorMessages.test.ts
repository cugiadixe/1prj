import { describe, it, expect } from 'vitest';
import {
  getErrorMessage,
  getErrorCode,
  isPermissionDenied,
  isConcurrencyError,
  GENERIC_ERROR,
  PERMISSION_DENIED,
  CUSTOMER_NOT_FOUND,
} from './errorMessages';

describe('errorMessages', () => {
  it('returns PERMISSION_DENIED for 403', () => {
    expect(getErrorMessage({ response: { status: 403 } })).toBe(PERMISSION_DENIED);
  });

  it('returns CUSTOMER_NOT_FOUND for 404', () => {
    expect(getErrorMessage({ response: { status: 404 } })).toBe(CUSTOMER_NOT_FOUND);
  });

  it('maps known error codes', () => {
    const err = { response: { status: 409, data: { extensions: { errorCode: 'CUS_INVALID_ROW_VERSION' } } } };
    expect(getErrorMessage(err)).toContain('người khác chỉnh sửa');
  });

  it('returns GENERIC_ERROR for unknown error code', () => {
    const err = { response: { status: 500, data: { extensions: { errorCode: 'UNKNOWN' } } } };
    expect(getErrorMessage(err)).toBe(GENERIC_ERROR);
  });

  it('returns GENERIC_ERROR for non-error', () => {
    expect(getErrorMessage(null)).toBe(GENERIC_ERROR);
  });

  it('extracts error code', () => {
    const err = { response: { data: { extensions: { errorCode: 'CUS_DUPLICATE_CCCD' } } } };
    expect(getErrorCode(err)).toBe('CUS_DUPLICATE_CCCD');
  });

  it('isPermissionDenied detects 403', () => {
    expect(isPermissionDenied({ response: { status: 403 } })).toBe(true);
    expect(isPermissionDenied({ response: { status: 200 } })).toBe(false);
  });

  it('isConcurrencyError detects CUS_INVALID_ROW_VERSION', () => {
    const err = { response: { data: { extensions: { errorCode: 'CUS_INVALID_ROW_VERSION' } } } };
    expect(isConcurrencyError(err)).toBe(true);
    expect(isConcurrencyError({ response: { status: 409 } })).toBe(false);
  });
});
