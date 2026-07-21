/**
 * CSRF utility for Phase 1B.1-J.
 * Reads X-CSRF-TOKEN from document.cookie (Path=/, HttpOnly=false per J0 correction).
 * Does NOT read RefreshToken — that remains HttpOnly and is inaccessible to JS.
 */

const CSRF_COOKIE_NAME = 'X-CSRF-TOKEN';
const CSRF_HEADER_NAME = 'X-CSRF-Token';

/**
 * Reads the CSRF token from the X-CSRF-TOKEN cookie.
 * Returns empty string if the cookie is absent (no active session).
 * Does not read access tokens or refresh tokens from cookies.
 */
export function readCsrfCookie(): string {
  const cookies = document.cookie.split(';');
  for (const cookie of cookies) {
    const [name, ...rest] = cookie.trim().split('=');
    if (name === CSRF_COOKIE_NAME) {
      return decodeURIComponent(rest.join('='));
    }
  }
  return '';
}

export { CSRF_HEADER_NAME };
