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

export interface UserCompanyDto {
  companyId: number;
  companyCode: string;
  companyName: string;
  isDefault: boolean;
}

export interface UserCompaniesResponse {
  companies: UserCompanyDto[];
}

/**
 * GET /api/v2/auth/me/companies
 * Retrieves the current user's selectable companies.
 */
export async function apiFetchMyCompanies(): Promise<UserCompaniesResponse> {
  const { data } = await axiosClient.get<UserCompaniesResponse>('/auth/me/companies');
  return data;
}

export interface MyProfileDto {
  userId: number;
  username: string | null;
  fullName: string | null;
  employeeCode: string | null;
  companyName: string | null;
  departmentName: string | null;
}

/**
 * GET /api/v2/auth/me/profile
 * Retrieves the current user's own profile (name, company, department).
 */
export async function apiFetchMyProfile(): Promise<MyProfileDto> {
  const { data } = await axiosClient.get<MyProfileDto>('/auth/me/profile');
  return data;
}

export interface MyActivityEventDto {
  id: number;
  actorUserId: number | null;
  actingAsUserId: number | null;
  targetUserId: number | null;
  companyId: number | null;
  eventCode: string;
  entityType: string;
  entityId: string | null;
  entityLabel: string | null;
  reason: string | null;
  correlationId: string;
  outcome: string;
  policyVersion: number | null;
  createdAt: string;
}

export interface MyActivityResponse {
  page: number;
  pageSize: number;
  totalCount: number;
  items: MyActivityEventDto[];
}

/**
 * GET /api/v2/auth/me/activity
 * Retrieves the current user's own recent activity (audit) events.
 */
export async function apiFetchMyActivity(
  page = 1,
  pageSize = 20,
): Promise<MyActivityResponse> {
  const { data } = await axiosClient.get<MyActivityResponse>('/auth/me/activity', {
    params: { page, pageSize },
  });
  return data;
}
