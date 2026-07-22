/**
 * In-memory auth state for Phase 1B.1-J.
 * Access token is never written to localStorage, sessionStorage, or cookies.
 * Refresh token is managed exclusively by the backend via HttpOnly Secure cookie.
 */

import type { CurrentUserPermissionDto } from './authApi';

export interface AuthUser {
  userId: number;
  username: string;
  displayName: string | null;
}

export interface AuthState {
  accessToken: string | null;
  mustChangePassword: boolean;
  user: AuthUser | null;
  isAuthenticated: boolean;
  permissions: CurrentUserPermissionDto[];
}

/**
 * Single in-memory auth store.
 * All consumers read/write through the exported helper functions.
 * No persistence to any browser storage API.
 */
let _state: AuthState = {
  accessToken: null,
  mustChangePassword: false,
  user: null,
  isAuthenticated: false,
  permissions: [],
};

export function getAuthState(): Readonly<AuthState> {
  return _state;
}

export function setAuthState(
  accessToken: string,
  mustChangePassword: boolean,
  user: AuthUser | null,
  permissions: CurrentUserPermissionDto[] = [],
): void {
  _state = {
    accessToken,
    mustChangePassword,
    user,
    isAuthenticated: true,
    permissions,
  };
}

export function clearAuthState(): void {
  _state = {
    accessToken: null,
    mustChangePassword: false,
    user: null,
    isAuthenticated: false,
    permissions: [],
  };
}
