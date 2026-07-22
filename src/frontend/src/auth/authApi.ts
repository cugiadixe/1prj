/**
 * Auth API client for Phase 1B.1-J.
 * All auth requests go to /api/v2/auth/* using the shared axiosClient.
 * withCredentials=true is required so the browser sends the HttpOnly RefreshToken cookie.
 * Access token is returned from API and held in memory only by the caller.
 * CSRF header is sent on cookie-reliant endpoints (refresh, logout, change-password).
 */

import axiosClient from '../api/axiosClient';
import { CSRF_HEADER_NAME, readCsrfCookie } from './csrf';

export interface LoginRequest {
  Username: string;
  Password: string;
}

export interface LoginUserInfo {
  userId: number;
  username: string;
  displayName: string | null;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  expiresAtUtc: string;
  user: LoginUserInfo;
  mustChangePassword: boolean;
}

export interface ChangePasswordRequest {
  CurrentPassword: string;
  NewPassword: string;
}

function csrfHeaders(): Record<string, string> {
  const token = readCsrfCookie();
  if (!token) return {};
  return { [CSRF_HEADER_NAME]: token };
}

/**
 * POST /api/v2/auth/login
 * Does not require CSRF (login is not cookie-reliant).
 */
export async function apiLogin(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await axiosClient.post<LoginResponse>(
    '/auth/login',
    request,
    { withCredentials: true },
  );
  return data;
}

/**
 * POST /api/v2/auth/refresh
 * Requires RefreshToken cookie (sent automatically by browser due to withCredentials)
 * and X-CSRF-Token header.
 */
export async function apiRefresh(): Promise<LoginResponse> {
  const { data } = await axiosClient.post<LoginResponse>(
    '/auth/refresh',
    {},
    {
      withCredentials: true,
      headers: csrfHeaders(),
    },
  );
  return data;
}

/**
 * POST /api/v2/auth/logout
 * Requires RefreshToken cookie and X-CSRF-Token header.
 */
export async function apiLogout(): Promise<void> {
  await axiosClient.post(
    '/auth/logout',
    {},
    {
      withCredentials: true,
      headers: csrfHeaders(),
    },
  );
}

/**
 * POST /api/v2/auth/change-password
 * Requires Bearer token (via axios interceptor), X-CSRF-Token header.
 */
export async function apiChangePassword(
  request: ChangePasswordRequest,
): Promise<void> {
  await axiosClient.post('/auth/change-password', request, {
    withCredentials: true,
    headers: csrfHeaders(),
  });
}

export interface CurrentUserPermissionDto {
  permissionCode: string;
  scope: string;
  companyId: number | null;
}

export interface CurrentUserPermissionsResponseDto {
  permissions: CurrentUserPermissionDto[];
}

/**
 * GET /api/v2/auth/me/permissions
 * Retrieves the current user's effective permissions.
 * If companyId is provided, returns GLOBAL + COMPANY-scoped permissions for that company.
 */
export async function apiFetchMyPermissions(companyId?: number): Promise<CurrentUserPermissionsResponseDto> {
  const headers: Record<string, string> = {};
  if (companyId !== undefined && companyId !== null) {
    headers['X-Company-Id'] = companyId.toString();
  }

  const { data } = await axiosClient.get<CurrentUserPermissionsResponseDto>(
    '/auth/me/permissions',
    {
      headers,
    }
  );
  return data;
}
