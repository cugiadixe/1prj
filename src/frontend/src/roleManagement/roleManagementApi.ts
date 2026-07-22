import axios from 'axios';

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

export interface CreateRoleRequest {
  roleCode: string;
  name: string;
  description: string | null;
  scopeType: string;
  companyId: number | null;
}

export interface UpdateRoleRequest {
  name: string;
  description: string | null;
  rowVersion: string;
}

export interface DeactivateRoleRequest {
  rowVersion: string;
}

export interface AddRolePermissionsRequest {
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

export const roleManagementApi = {
  getRoles: async (): Promise<RoleDto[]> => {
    const response = await axios.get<RoleDto[]>(`${BASE_URL}/roles`);
    return response.data;
  },

  getRole: async (id: number): Promise<RoleDto> => {
    const response = await axios.get<RoleDto>(`${BASE_URL}/roles/${id}`);
    return response.data;
  },

  createRole: async (request: CreateRoleRequest): Promise<RoleDto> => {
    const response = await axios.post<RoleDto>(`${BASE_URL}/roles`, request);
    return response.data;
  },

  updateRole: async (id: number, request: UpdateRoleRequest): Promise<RoleDto> => {
    const response = await axios.put<RoleDto>(`${BASE_URL}/roles/${id}`, request);
    return response.data;
  },

  deactivateRole: async (id: number, request: DeactivateRoleRequest): Promise<void> => {
    await axios.delete(`${BASE_URL}/roles/${id}`, { data: request });
  },

  addRolePermissions: async (id: number, request: AddRolePermissionsRequest): Promise<void> => {
    await axios.post(`${BASE_URL}/roles/${id}/permissions`, request);
  },

  removeRolePermission: async (id: number, code: string): Promise<void> => {
    await axios.delete(`${BASE_URL}/roles/${id}/permissions/${encodeURIComponent(code)}`);
  },

  getPermissions: async (): Promise<PermissionDto[]> => {
    const response = await axios.get<PermissionDto[]>(`${BASE_URL}/permissions`);
    return response.data;
  },
};
