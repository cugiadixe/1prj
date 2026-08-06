import axiosClient from '../api/axiosClient';

export interface EffectivePermissionsResponse {
  userId: number;
  companyId: number | null;
  permissionCodes: string[];
}

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

export interface UserIndividualPermissionDto {
  id: number;
  userId: number;
  permissionCode: string;
  scopeType: string;
  companyId: number | null;
  grantType: string;
  assignmentStatus: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  reason: string | null;
  rowVersion: string;
}

export interface UserRoleAssignmentDto {
  id: number;
  userId: number;
  roleId: number;
  roleCode: string;
  roleName: string;
  scopeType: string;
  companyId: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  rowVersion: string;
}

export interface UserAdminGroupAssignmentDto {
  id: number;
  userId: number;
  adminGroupId: number;
  groupCode: string;
  groupName: string;
  assignmentStatus: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  rowVersion: string;
}

export interface RoleDto {
  id: number;
  roleCode: string;
  name: string;
  description: string | null;
  scopeType: string;
  companyId: number | null;
  isActive: boolean;
  permissionCodes: string[];
  rowVersion: string;
}

export interface AdminGroupDto {
  id: number;
  groupCode: string;
  name: string;
  description: string | null;
  scopeType: string;
  companyId: number | null;
  isActive: boolean;
  permissionCodes: string[];
  rowVersion: string;
}

const SEC_BASE = '/security';

export const effectivePermissionDiagnosticsApi = {
  fetchEffectivePermissions: async (
    userId: number,
    companyId?: number | null,
  ): Promise<EffectivePermissionsResponse> => {
    const params: Record<string, unknown> = {};
    if (companyId !== undefined && companyId !== null) {
      params.companyId = companyId;
    }
    const { data } = await axiosClient.get<EffectivePermissionsResponse>(
      `${SEC_BASE}/users/${userId}/effective-permissions`,
      { params },
    );
    return data;
  },

  fetchPermissionCatalog: async (): Promise<PermissionDto[]> => {
    const { data } = await axiosClient.get<PermissionDto[]>(
      `${SEC_BASE}/permissions`,
    );
    return data;
  },

  fetchUserIndividualPermissions: async (
    userId: number,
  ): Promise<UserIndividualPermissionDto[]> => {
    const { data } = await axiosClient.get<UserIndividualPermissionDto[]>(
      `${SEC_BASE}/users/${userId}/individual-permissions`,
    );
    return data;
  },

  fetchUserRoleAssignments: async (
    userId: number,
  ): Promise<UserRoleAssignmentDto[]> => {
    const { data } = await axiosClient.get<UserRoleAssignmentDto[]>(
      `${SEC_BASE}/users/${userId}/role-assignments`,
    );
    return data;
  },

  fetchUserAdminGroupAssignments: async (
    userId: number,
  ): Promise<UserAdminGroupAssignmentDto[]> => {
    const { data } = await axiosClient.get<UserAdminGroupAssignmentDto[]>(
      `${SEC_BASE}/users/${userId}/admin-group-assignments`,
    );
    return data;
  },

  fetchRoleDetails: async (roleId: number): Promise<RoleDto> => {
    const { data } = await axiosClient.get<RoleDto>(
      `${SEC_BASE}/roles/${roleId}`,
    );
    return data;
  },

  fetchAdminGroupDetails: async (adminGroupId: number): Promise<AdminGroupDto> => {
    const { data } = await axiosClient.get<AdminGroupDto>(
      `${SEC_BASE}/admin-groups/${adminGroupId}`,
    );
    return data;
  },
};
