import axios from 'axios';

export interface CompanyDto {
  id: number;
  companyId: number;
  companyCode: string;
  companyName: string;
  isActive: boolean;
}

export interface DepartmentDto {
  id: number;
  departmentCode: string;
  name: string;
  companyId: number;
  isActive: boolean;
}

export interface DepartmentPermissionDto {
  permissionCode: string;
}

export interface SetDepartmentPermissionsRequest {
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
  name?: string;
  scope?: string;
  status?: string;
}

const SEC_BASE_URL = '/api/v2/security';
const ORG_BASE_URL = '/api/v2/organizations';

export const departmentPermissionsApi = {
  getCompanies: async (): Promise<CompanyDto[]> => {
    const response = await axios.get<CompanyDto[]>(`${ORG_BASE_URL}/companies`);
    return response.data;
  },

  getDepartments: async (companyId?: number): Promise<DepartmentDto[]> => {
    const params = companyId ? { companyId } : {};
    const response = await axios.get<DepartmentDto[]>(`${ORG_BASE_URL}/departments`, { params });
    return response.data;
  },

  getDepartmentPermissions: async (departmentId: number): Promise<DepartmentPermissionDto[]> => {
    const response = await axios.get<DepartmentPermissionDto[]>(`${SEC_BASE_URL}/departments/${departmentId}/permissions`);
    return response.data;
  },

  setDepartmentPermissions: async (departmentId: number, request: SetDepartmentPermissionsRequest): Promise<void> => {
    await axios.put(`${SEC_BASE_URL}/departments/${departmentId}/permissions`, request);
  },

  removeDepartmentPermission: async (departmentId: number, code: string): Promise<void> => {
    await axios.delete(`${SEC_BASE_URL}/departments/${departmentId}/permissions/${encodeURIComponent(code)}`);
  },

  getPermissions: async (): Promise<PermissionDto[]> => {
    const response = await axios.get<PermissionDto[]>(`${SEC_BASE_URL}/permissions`);
    return response.data;
  },
};
