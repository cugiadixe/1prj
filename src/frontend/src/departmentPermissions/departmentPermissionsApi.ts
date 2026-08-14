import axiosClient from '../api/axiosClient';

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

// axiosClient đã có baseURL '/api/v2' + interceptor gắn Bearer token (đăng ký ở AuthProvider).
// Trước đây file này dùng `axios` gốc nên request KHÔNG có token → 401/403 → dữ liệu rỗng.
const SEC_BASE_URL = '/security';
const ORG_BASE_URL = '/organizations';

export const departmentPermissionsApi = {
  getCompanies: async (): Promise<CompanyDto[]> => {
    const response = await axiosClient.get<CompanyDto[]>(`${ORG_BASE_URL}/companies`);
    return response.data;
  },

  getDepartments: async (companyId?: number): Promise<DepartmentDto[]> => {
    const params = companyId ? { companyId } : {};
    const response = await axiosClient.get<DepartmentDto[]>(`${ORG_BASE_URL}/departments`, { params });
    return response.data;
  },

  getDepartmentPermissions: async (departmentId: number): Promise<DepartmentPermissionDto[]> => {
    const response = await axiosClient.get<DepartmentPermissionDto[]>(`${SEC_BASE_URL}/departments/${departmentId}/permissions`);
    return response.data;
  },

  setDepartmentPermissions: async (departmentId: number, request: SetDepartmentPermissionsRequest): Promise<void> => {
    await axiosClient.put(`${SEC_BASE_URL}/departments/${departmentId}/permissions`, request);
  },

  removeDepartmentPermission: async (departmentId: number, code: string): Promise<void> => {
    await axiosClient.delete(`${SEC_BASE_URL}/departments/${departmentId}/permissions/${encodeURIComponent(code)}`);
  },

  getPermissions: async (): Promise<PermissionDto[]> => {
    const response = await axiosClient.get<PermissionDto[]>(`${SEC_BASE_URL}/permissions`);
    return response.data;
  },
};
