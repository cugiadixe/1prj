/**
 * Permission Assignment API client — Phase 1B.1-N.
 *
 * Wraps existing backend security admin endpoints.
 * All requests use the shared axiosClient (Bearer token injected by AuthProvider interceptor).
 * No access token, refresh token, or sensitive data is written to localStorage,
 * sessionStorage, cookies, or console logs.
 *
 * Existing backend endpoints used:
 *   GET    /api/v2/security/permissions                                — permission catalog
 *   GET    /api/v2/security/users/{userId}/individual-permissions      — list assignments
 *   POST   /api/v2/security/users/{userId}/individual-permissions      — grant assignment
 *   DELETE /api/v2/security/users/{userId}/individual-permissions/{id} — deactivate assignment
 *   GET    /api/v2/security/users/{userId}/effective-permissions       — effective permissions
 */

import axiosClient from '../api/axiosClient';

// ── Permission Catalog DTO ────────────────────────────────────────────────────

export interface PermissionDto {
  permissionCode: string;
  moduleCode: string;
  actionCode: string;
  dataScope: string;
  isSensitive: boolean;
  isDelegable: boolean;
  requiresReason: boolean;
  isActive: boolean;
  description: string | null;
}

// ── User Individual Permission DTO ────────────────────────────────────────────

export interface UserIndividualPermissionDto {
  id: number;
  userId: number;
  permissionCode: string;
  scopeType: string;
  companyId: number | null;
  grantType: string;           // "ALLOW" | "DENY"
  assignmentStatus: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  reason: string | null;
  rowVersion: string;
}

// ── Create Request ────────────────────────────────────────────────────────────

export interface CreateUserIndividualPermissionRequest {
  permissionCode: string;
  scopeType: string;           // "GLOBAL" | "COMPANY"
  companyId: number | null;
  grantType: string;           // "ALLOW" | "DENY"
  effectiveFrom: string;
  effectiveTo: string | null;
  reason: string | null;
}

// ── Deactivate Request ────────────────────────────────────────────────────────

export interface DeactivateAssignmentRequest {
  rowVersion: string;
}

// ── Effective Permissions ─────────────────────────────────────────────────────

export interface EffectivePermissionsResponse {
  userId: number;
  companyId: number | null;
  permissionCodes: string[];
}

// ── API Functions ─────────────────────────────────────────────────────────────

/**
 * GET /api/v2/security/permissions
 * Returns the permission catalog.
 * Requires SECURITY_ADMIN_MANAGE GLOBAL (enforced by backend).
 */
export async function fetchPermissionCatalog(): Promise<PermissionDto[]> {
  const { data } = await axiosClient.get<PermissionDto[]>(
    '/security/permissions',
  );
  return data;
}

/**
 * GET /api/v2/security/users/{userId}/individual-permissions
 * Returns all individual permission assignments for the user.
 * Requires SECURITY_ADMIN_MANAGE GLOBAL (enforced by backend).
 */
export async function fetchUserIndividualPermissions(
  userId: number,
): Promise<UserIndividualPermissionDto[]> {
  const { data } = await axiosClient.get<UserIndividualPermissionDto[]>(
    `/security/users/${userId}/individual-permissions`,
  );
  return data;
}

/**
 * POST /api/v2/security/users/{userId}/individual-permissions
 * Grants an individual permission assignment to the user.
 * Returns 201 on creation, 200 on idempotent duplicate.
 * Returns 409 on temporal overlap, 422 on validation error.
 * Requires SECURITY_ADMIN_MANAGE GLOBAL (enforced by backend).
 */
export async function grantIndividualPermission(
  userId: number,
  request: CreateUserIndividualPermissionRequest,
): Promise<UserIndividualPermissionDto> {
  const { data } = await axiosClient.post<UserIndividualPermissionDto>(
    `/security/users/${userId}/individual-permissions`,
    request,
  );
  return data;
}

/**
 * DELETE /api/v2/security/users/{userId}/individual-permissions/{id}
 * Deactivates (soft-deletes) an individual permission assignment.
 * Returns 204 on success.
 * Returns 404 if not found, 409 on concurrency conflict.
 * Requires SECURITY_ADMIN_MANAGE GLOBAL (enforced by backend).
 */
export async function deactivateIndividualPermission(
  userId: number,
  permissionId: number,
  request: DeactivateAssignmentRequest,
): Promise<void> {
  await axiosClient.delete(
    `/security/users/${userId}/individual-permissions/${permissionId}`,
    { data: request },
  );
}

/**
 * GET /api/v2/security/users/{userId}/effective-permissions?companyId={companyId}
 * Returns the final effective permission codes for the user.
 * Requires SECURITY_ADMIN_MANAGE GLOBAL (enforced by backend).
 */
export async function fetchEffectivePermissions(
  userId: number,
  companyId?: number | null,
): Promise<EffectivePermissionsResponse> {
  const params: Record<string, unknown> = {};
  if (companyId !== undefined && companyId !== null) {
    params.companyId = companyId;
  }
  const { data } = await axiosClient.get<EffectivePermissionsResponse>(
    `/security/users/${userId}/effective-permissions`,
    { params },
  );
  return data;
}
