/**
 * Account Management API client — Phase 1B.1-K.
 *
 * All requests use the shared axiosClient (Bearer token injected by AuthProvider interceptor).
 * No access token, refresh token, or temporary password is written to localStorage,
 * sessionStorage, or console logs.
 *
 * Endpoints:
 *   GET  /api/v2/security/accounts                        — list/search (K0)
 *   GET  /api/v2/security/accounts/by-user/{userId}       — by-user lookup (K0)
 *   GET  /api/v2/security/accounts/{accountId}            — detail (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/activate   — activate (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/disable    — disable with reason (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/lock       — lock with reason (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/unlock     — unlock (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/reset-password   — reset-password with reason (Phase I)
 *   POST /api/v2/security/accounts/{accountId}/revoke-sessions  — revoke sessions with reason (Phase I)
 */

import axiosClient from '../api/axiosClient';
import type {
  AccountDetailDto,
  AccountReasonRequest,
  AccountSearchParams,
  AccountSummaryDto,
  AdminResetPasswordResponse,
  CreateAccountRequest,
  PagedResult,
  UserWithoutAccountDto,
} from './types';

const BASE = '/security/accounts';

// ── Discovery endpoints (K0) ──────────────────────────────────────────────────

/**
 * GET /api/v2/security/accounts
 * Returns a paginated list of account summaries.
 * Requires SECURITY_ACCOUNT_MANAGE (enforced by backend).
 */
export async function searchAccounts(
  params: AccountSearchParams = {},
): Promise<PagedResult<AccountSummaryDto>> {
  const { data } = await axiosClient.get<PagedResult<AccountSummaryDto>>(BASE, {
    params: {
      search: params.search || undefined,
      status: params.status || undefined,
      providerType: params.providerType || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  });
  return data;
}

/**
 * GET /api/v2/security/accounts/by-user/{userId}
 * Returns all auth accounts for the given user.
 * Returns 404 if user does not exist.
 * Returns empty array if user exists but has no auth accounts.
 */
export async function getAccountsByUserId(
  userId: number,
): Promise<AccountSummaryDto[]> {
  const { data } = await axiosClient.get<AccountSummaryDto[]>(
    `${BASE}/by-user/${userId}`,
  );
  return data;
}

export async function getUsersWithoutAccount(): Promise<UserWithoutAccountDto[]> {
  const { data } = await axiosClient.get<UserWithoutAccountDto[]>(
    `${BASE}/users-without-account`,
  );
  return data;
}

export async function createAccount(
  request: CreateAccountRequest,
): Promise<AdminResetPasswordResponse> {
  const { data } = await axiosClient.post<AdminResetPasswordResponse>(BASE, request);
  return data;
}

// ── Phase I detail endpoint ───────────────────────────────────────────────────

/**
 * GET /api/v2/security/accounts/{accountId}
 * Returns AccountDetailDto.
 */
export async function getAccountDetail(
  accountId: number,
): Promise<AccountDetailDto> {
  const { data } = await axiosClient.get<AccountDetailDto>(`${BASE}/${accountId}`);
  return data;
}

// ── Phase I action endpoints ──────────────────────────────────────────────────

/**
 * POST /api/v2/security/accounts/{accountId}/activate
 * Returns 204 on success.
 */
export async function activateAccount(accountId: number): Promise<void> {
  await axiosClient.post(`${BASE}/${accountId}/activate`);
}

/**
 * POST /api/v2/security/accounts/{accountId}/disable
 * Requires reason. Returns 204 on success.
 */
export async function disableAccount(
  accountId: number,
  reason: string,
): Promise<void> {
  const body: AccountReasonRequest = { reason };
  await axiosClient.post(`${BASE}/${accountId}/disable`, body);
}

/**
 * POST /api/v2/security/accounts/{accountId}/lock
 * Requires reason. Returns 204 on success.
 */
export async function lockAccount(
  accountId: number,
  reason: string,
): Promise<void> {
  const body: AccountReasonRequest = { reason };
  await axiosClient.post(`${BASE}/${accountId}/lock`, body);
}

/**
 * POST /api/v2/security/accounts/{accountId}/unlock
 * Returns 204 on success.
 */
export async function unlockAccount(accountId: number): Promise<void> {
  await axiosClient.post(`${BASE}/${accountId}/unlock`);
}

/**
 * POST /api/v2/security/accounts/{accountId}/reset-password
 * Requires reason. Returns AdminResetPasswordResponse containing the temporary password.
 * Caller must display the temporary password once and clear it from state on dismiss.
 * Must NOT log the temporary password to console or write it to any storage.
 */
export async function resetPassword(
  accountId: number,
  reason: string,
): Promise<AdminResetPasswordResponse> {
  const body: AccountReasonRequest = { reason };
  const { data } = await axiosClient.post<AdminResetPasswordResponse>(
    `${BASE}/${accountId}/reset-password`,
    body,
  );
  return data;
}

/**
 * POST /api/v2/security/accounts/{accountId}/revoke-sessions
 * Requires reason. Returns 204 on success.
 */
export async function revokeSessions(
  accountId: number,
  reason: string,
): Promise<void> {
  const body: AccountReasonRequest = { reason };
  await axiosClient.post(`${BASE}/${accountId}/revoke-sessions`, body);
}
