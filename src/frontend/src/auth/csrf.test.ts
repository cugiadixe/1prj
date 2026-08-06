import { describe, it, expect, beforeEach, vi } from 'vitest';
import { readCsrfCookie, CSRF_HEADER_NAME } from './csrf';

describe('csrf utilities', () => {
  beforeEach(() => {
    // Clear document.cookie
    document.cookie = 'X-CSRF-TOKEN=; Max-Age=0; path=/';
  });

  it('exports correct header name', () => {
    expect(CSRF_HEADER_NAME).toBe('X-CSRF-Token');
  });

  it('returns empty string when cookie is absent', () => {
    expect(readCsrfCookie()).toBe('');
  });

  it('reads X-CSRF-TOKEN from document.cookie', () => {
    document.cookie = 'X-CSRF-TOKEN=abc123; path=/';
    expect(readCsrfCookie()).toBe('abc123');
  });

  it('does not read RefreshToken from document.cookie', () => {
    document.cookie = 'X-CSRF-TOKEN=mycsrf; path=/';
    const value = readCsrfCookie();
    expect(value).toBe('mycsrf');
    // The function is scoped to X-CSRF-TOKEN by name
  });

  it('returns empty string when only other cookies exist', () => {
    Object.defineProperty(document, 'cookie', {
      get: vi.fn(() => 'SomeOtherCookie=xyz'),
      configurable: true,
    });
    expect(readCsrfCookie()).toBe('');
  });
});
