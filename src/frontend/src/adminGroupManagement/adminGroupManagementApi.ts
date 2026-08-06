import axios from 'axios';

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

export interface CreateAdminGroupRequest {
  groupCode: string;
  name: string;
  description: string | null;
  scopeType: string;
  companyId: number | null;
}

export interface UpdateAdminGroupRequest {
  name: string;
  description: string | null;
  rowVersion: string;
}

export interface DeactivateAdminGroupRequest {
  rowVersion: string;
}

export interface AddAdminGroupPermissionsRequest {
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

const BASE_URL = '/api/v2/security';

export const adminGroupManagementApi = {
  getAdminGroups: async (): Promise<AdminGroupDto[]> => {
    const response = await axios.get<AdminGroupDto[]>(`${BASE_URL}/admin-groups`);
    return response.data;
  },

  getAdminGroup: async (id: number): Promise<AdminGroupDto> => {
    const response = await axios.get<AdminGroupDto>(`${BASE_URL}/admin-groups/${id}`);
    return response.data;
  },

  createAdminGroup: async (request: CreateAdminGroupRequest): Promise<AdminGroupDto> => {
    const response = await axios.post<AdminGroupDto>(`${BASE_URL}/admin-groups`, request);
    return response.data;
  },

  updateAdminGroup: async (id: number, request: UpdateAdminGroupRequest): Promise<AdminGroupDto> => {
    const response = await axios.put<AdminGroupDto>(`${BASE_URL}/admin-groups/${id}`, request);
    return response.data;
  },

  deactivateAdminGroup: async (id: number, request: DeactivateAdminGroupRequest): Promise<void> => {
    await axios.delete(`${BASE_URL}/admin-groups/${id}`, { data: request });
  },

  addAdminGroupPermissions: async (id: number, request: AddAdminGroupPermissionsRequest): Promise<void> => {
    await axios.post(`${BASE_URL}/admin-groups/${id}/permissions`, request);
  },

  removeAdminGroupPermission: async (id: number, code: string): Promise<void> => {
    await axios.delete(`${BASE_URL}/admin-groups/${id}/permissions/${encodeURIComponent(code)}`);
  },

  getPermissions: async (): Promise<PermissionDto[]> => {
    const response = await axios.get<PermissionDto[]>(`${BASE_URL}/permissions`);
    return response.data;
  },
};
