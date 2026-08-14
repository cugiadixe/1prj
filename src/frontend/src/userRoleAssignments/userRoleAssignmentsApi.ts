import axiosClient from '../api/axiosClient';

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

export interface CreateUserRoleAssignmentRequest {
  roleId: number;
  effectiveFrom: string;
  effectiveTo: string | null;
}

export interface DeactivateAssignmentRequest {
  rowVersion: string;
}

const BASE_URL = '/security';

export const userRoleAssignmentsApi = {
  getUserRoleAssignments: async (userId: number): Promise<UserRoleAssignmentDto[]> => {
    const response = await axiosClient.get<UserRoleAssignmentDto[]>(`${BASE_URL}/users/${userId}/role-assignments`);
    return response.data;
  },

  assignRoleToUser: async (userId: number, request: CreateUserRoleAssignmentRequest, currentCompanyId?: number): Promise<UserRoleAssignmentDto> => {
    const headers = currentCompanyId ? { 'X-Company-Id': currentCompanyId.toString() } : undefined;
    const response = await axiosClient.post<UserRoleAssignmentDto>(`${BASE_URL}/users/${userId}/role-assignments`, request, { headers });
    return response.data;
  },

  deactivateUserRoleAssignment: async (userId: number, assignmentId: number, request: DeactivateAssignmentRequest, currentCompanyId?: number): Promise<void> => {
    const headers = currentCompanyId ? { 'X-Company-Id': currentCompanyId.toString() } : undefined;
    await axiosClient.delete(`${BASE_URL}/users/${userId}/role-assignments/${assignmentId}`, { data: request, headers });
  }
};
