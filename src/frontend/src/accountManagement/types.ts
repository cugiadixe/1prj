/**
 * Frontend TypeScript types for Phase 1B.1-K Account Management UI.
 * Matches backend DTOs in PTKD.Application.Security.AccountManagement.DTOs.
 * Do not add sensitive fields (PasswordHash, SecurityStamp, RowVersion, etc.).
 */

// ── AccountSummaryDto — from K0 discovery API ─────────────────────────────────

export interface AccountSummaryDto {
  accountId: number;
  userId: number;
  username: string;
  providerType: string;
  status: AccountStatus;
  mustChangePassword: boolean;
  employeeCode: string;
  fullName: string;
  employmentStatus: string;
  createdAt: string;
  updatedAt: string | null;
}

// ── AccountDetailDto — from Phase I detail endpoint ───────────────────────────

export interface AccountDetailDto {
  id: number;
  userId: number;
  providerType: string;
  username: string;
  status: AccountStatus;
  isInternalProvider: boolean;
  failedAttemptCount: number;
  isManualLock: boolean;
  lockoutEnd: string | null;
  mustChangePassword: boolean;
  temporaryPasswordExpiresAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

// ── Status ─────────────────────────────────────────────────────────────────────

export type AccountStatus = 'ACTIVE' | 'LOCKED' | 'DISABLED' | string;

// ── PagedResult<T> — from list/search API ─────────────────────────────────────

export interface PagedResult<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

// ── Request/response for reason-required actions ───────────────────────────────

export interface AccountReasonRequest {
  reason: string;
}

export interface AdminResetPasswordResponse {
  temporaryPassword: string;
}

// ── ProblemDetails — backend error response shape ─────────────────────────────

export interface ProblemDetails {
  status: number;
  title: string;
  detail: string;
  type?: string;
  extensions?: Record<string, unknown>;
  errors?: Record<string, string[]>;
}

export function extractErrorCode(error: unknown): string | null {
  try {
    const err = error as { response?: { data?: ProblemDetails } };
    const ext = err?.response?.data?.extensions;
    if (ext && typeof ext['errorCode'] === 'string') {
      return ext['errorCode'];
    }
  } catch {
    // ignore
  }
  return null;
}

// ── Account search query params ────────────────────────────────────────────────

export interface AccountSearchParams {
  search?: string;
  status?: string;
  providerType?: string;
  page?: number;
  pageSize?: number;
}
